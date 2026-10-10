using System.Security.Claims;
using System.Text.Json;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/authentication")]
public sealed class AuthenticationAdministrationController(
    ApplicationDbContext context,
    ITenantContext tenant,
    IEnterpriseProviderRegistry providers,
    IAccessControlService accessControl) : ControllerBase
{
    [HttpGet("providers")]
    [Authorize(Policy = "Permission:AUTHENTICATION.CONFIGURE")]
    public ActionResult<ApiResponse<EnterpriseProviderResponse[]>> GetProviders() => Ok(new ApiResponse<EnterpriseProviderResponse[]>(true,
        providers.Providers.Select(item => new EnterpriseProviderResponse(item.Code, item.DisplayName, item.Kind)).OrderBy(item => item.DisplayName).ToArray()));

    [HttpGet]
    [Authorize(Policy = "Permission:AUTHENTICATION.READ")]
    public async Task<ActionResult<ApiResponse<AuthenticationConfigurationDto?>>> Get(CancellationToken cancellationToken)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        var value = await context.AuthenticationConfigurations.AsNoTracking().Include(item => item.Policy)
            .SingleOrDefaultAsync(item => item.MunicipalityId == municipalityId && item.IsCurrent, cancellationToken);
        return Ok(new ApiResponse<AuthenticationConfigurationDto?>(true, value == null ? null : ToDto(value)));
    }

    [HttpGet("history/page")]
    [Authorize(Policy = "Permission:AUTHENTICATION.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuthenticationConfigurationDto>>>> GetHistoryPage(
        [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        if (request.NormalizedSortBy is not ("version" or "effectivefrom" or "mode" or "displayname"))
            return BadRequest(Fail<PagedResponse<AuthenticationConfigurationDto>>("SortBy must be version, effectiveFrom, mode, or displayName."));

        var query = context.AuthenticationConfigurations.AsNoTracking().Include(item => item.Policy)
            .Where(item => item.MunicipalityId == municipalityId);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.DisplayName.Contains(term)
                || item.ProviderRegistrationCode != null && item.ProviderRegistrationCode.Contains(term));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.VersionNumber),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.VersionNumber),
            ("mode", false) => query.OrderBy(item => item.Mode).ThenByDescending(item => item.VersionNumber),
            ("mode", true) => query.OrderByDescending(item => item.Mode).ThenByDescending(item => item.VersionNumber),
            ("displayname", false) => query.OrderBy(item => item.DisplayName).ThenByDescending(item => item.VersionNumber),
            ("displayname", true) => query.OrderByDescending(item => item.DisplayName).ThenByDescending(item => item.VersionNumber),
            (_, false) => query.OrderBy(item => item.VersionNumber),
            _ => query.OrderByDescending(item => item.VersionNumber)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync(cancellationToken);
        return Ok(new ApiResponse<PagedResponse<AuthenticationConfigurationDto>>(true,
            PagedResponse<AuthenticationConfigurationDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpPut]
    [Authorize(Policy = "Permission:AUTHENTICATION.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<AuthenticationConfigurationDto>>> Save(SaveAuthenticationConfigurationRequest request, CancellationToken cancellationToken)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (!ValidReason(request.Reason)) return BadRequest(Fail<AuthenticationConfigurationDto>("A governance reason between 5 and 500 characters is required."));
        if (!TryValidateProvider(request.Mode, request.ProviderRegistrationCode, out var providerCode, out var providerError))
            return BadRequest(Fail<AuthenticationConfigurationDto>(providerError!));
        if (!ValidPolicy(request.Policy, out var policyError)) return BadRequest(Fail<AuthenticationConfigurationDto>(policyError!));
        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            return BadRequest(Fail<AuthenticationConfigurationDto>("EffectiveTo cannot precede EffectiveFrom."));

        var municipality = await context.Municipalities.SingleAsync(item => item.Id == municipalityId, cancellationToken);
        var previous = await context.AuthenticationConfigurations.Include(item => item.Policy)
            .SingleOrDefaultAsync(item => item.MunicipalityId == municipalityId && item.IsCurrent, cancellationToken);
        var created = previous == null;
        AuthenticationConfiguration entity;
        if (previous == null)
        {
            if (!string.IsNullOrWhiteSpace(request.RowVersion) || !string.IsNullOrWhiteSpace(request.Policy.RowVersion))
                return Conflict(Fail<AuthenticationConfigurationDto>("Authentication configuration does not yet exist; reload and retry."));
            entity = new AuthenticationConfiguration
            {
                MunicipalityId = municipalityId,
                ConfigurationFamilyPublicId = Guid.NewGuid(),
                VersionNumber = 1,
                IsCurrent = true,
                CreatedByUserId = actor,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            if (!SetVersion(previous, request.RowVersion))
                return BadRequest(Fail<AuthenticationConfigurationDto>("A valid configuration RowVersion is required."));
            if (previous.Policy == null)
            {
                if (!string.IsNullOrWhiteSpace(request.Policy.RowVersion))
                    return Conflict(Fail<AuthenticationConfigurationDto>("The stored configuration has no policy row; reload and retry."));
            }
            else if (!SetVersion(previous.Policy, request.Policy.RowVersion))
                return BadRequest(Fail<AuthenticationConfigurationDto>("A valid policy RowVersion is required."));
            if (request.EffectiveFrom <= previous.EffectiveFrom)
                return BadRequest(Fail<AuthenticationConfigurationDto>("A successor authentication configuration must become effective after its predecessor."));

            var now = DateTime.UtcNow;
            previous.IsCurrent = false;
            previous.EffectiveTo = request.EffectiveFrom.AddTicks(-1);
            previous.ModifiedByUserId = actor;
            previous.ModifiedAt = now;
            entity = new AuthenticationConfiguration
            {
                MunicipalityId = municipalityId,
                ConfigurationFamilyPublicId = previous.ConfigurationFamilyPublicId,
                VersionNumber = previous.VersionNumber + 1,
                IsCurrent = true,
                PreviousVersionId = previous.Id,
                CreatedByUserId = actor,
                CreatedAt = now
            };
        }

        entity.Mode = request.Mode;
        entity.ProviderRegistrationCode = providerCode;
        entity.DisplayName = request.DisplayName.Trim();
        entity.IsActive = request.IsActive;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        municipality.AuthenticationMode = request.Mode;
        entity.Policy = new AuthenticationPolicy { MunicipalityId = municipalityId, ModifiedByUserId = actor };
        ApplyPolicy(entity.Policy, request.Policy, actor);
        context.AuthenticationConfigurations.Add(entity);
        AddAudit(municipalityId, actor, "AuthenticationConfiguration", created ? "AuthenticationConfigurationCreated" : "AuthenticationConfigurationVersionCreated", entity.PublicId.ToString(), request.Reason,
            new { entity.ConfigurationFamilyPublicId, entity.VersionNumber, PreviousVersionPublicId = previous?.PublicId, request.Mode, ProviderRegistrationCode = providerCode, request.IsActive, request.EffectiveFrom, request.EffectiveTo });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Ok(new ApiResponse<AuthenticationConfigurationDto>(true, ToDto(entity)));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(Fail<AuthenticationConfigurationDto>("Authentication configuration changed since it was loaded. Refresh and try again."));
        }
    }

    [HttpGet("authenticators")]
    [Authorize(Policy = "Permission:AUTHENTICATION.LINK_IDENTITIES")]
    public ActionResult<ApiResponse<UserAuthenticatorDto[]>> GetAuthenticators() => StatusCode(StatusCodes.Status410Gone,
        Fail<UserAuthenticatorDto[]>("The unbounded authenticator route is retired. Use /api/v1/admin/authentication/authenticators/page."));

    [HttpGet("authenticators/page")]
    [Authorize(Policy = "Permission:AUTHENTICATION.LINK_IDENTITIES")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserAuthenticatorDto>>>> GetAuthenticatorsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool? active = null,
        [FromQuery] string? providerCode = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        if (request.NormalizedSortBy is not ("createdat" or "email" or "provider" or "status" or "linkedat" or "lastauthenticatedat"))
            return BadRequest(Fail<PagedResponse<UserAuthenticatorDto>>("SortBy must be createdAt, email, provider, status, linkedAt, or lastAuthenticatedAt."));

        var canReadUserEmail = await CanAccessMemberAsync("UserEmail", SecurityOperation.Read, municipalityId);
        var canReadExpectedEmail = await CanAccessMemberAsync("ExpectedEmail", SecurityOperation.Read, municipalityId);
        var canReadIssuer = await CanAccessMemberAsync("Issuer", SecurityOperation.Read, municipalityId);
        var canReadSubject = await CanAccessMemberAsync("Subject", SecurityOperation.Read, municipalityId);
        if (request.NormalizedSortBy == "email" && !canReadExpectedEmail)
            return Forbid();

        var query = context.UserAuthenticators.AsNoTracking().Where(item => item.MunicipalityId == municipalityId);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            var provider = providerCode.Trim();
            query = query.Where(item => item.ProviderRegistrationCode == provider);
        }
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.ProviderRegistrationCode.Contains(term)
                || canReadExpectedEmail && item.ExpectedEmail.Contains(term)
                || canReadUserEmail && item.User.Email != null && item.User.Email.Contains(term)
                || canReadIssuer && item.Issuer != null && item.Issuer.Contains(term)
                || canReadSubject && item.Subject != null && item.Subject.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("email", false) => query.OrderBy(item => item.ExpectedEmail).ThenBy(item => item.Id),
            ("email", true) => query.OrderByDescending(item => item.ExpectedEmail).ThenByDescending(item => item.Id),
            ("provider", false) => query.OrderBy(item => item.ProviderRegistrationCode).ThenBy(item => item.ExpectedEmail).ThenBy(item => item.Id),
            ("provider", true) => query.OrderByDescending(item => item.ProviderRegistrationCode).ThenBy(item => item.ExpectedEmail).ThenByDescending(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.ExpectedEmail).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.ExpectedEmail).ThenByDescending(item => item.Id),
            ("linkedat", false) => query.OrderBy(item => item.LinkedAt).ThenBy(item => item.Id),
            ("linkedat", true) => query.OrderByDescending(item => item.LinkedAt).ThenByDescending(item => item.Id),
            ("lastauthenticatedat", false) => query.OrderBy(item => item.LastAuthenticatedAt).ThenBy(item => item.Id),
            ("lastauthenticatedat", true) => query.OrderByDescending(item => item.LastAuthenticatedAt).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Include(item => item.User).ToArrayAsync(cancellationToken);
        return Ok(new ApiResponse<PagedResponse<UserAuthenticatorDto>>(true,
            PagedResponse<UserAuthenticatorDto>.Create(rows.Select(item => ToDto(item, item.User,
                canReadUserEmail, canReadExpectedEmail, canReadIssuer, canReadSubject)), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("authenticators")]
    [Authorize(Policy = "Permission:AUTHENTICATION.LINK_IDENTITIES")]
    public async Task<ActionResult<ApiResponse<UserAuthenticatorDto>>> Provision(ProvisionUserAuthenticatorRequest request, CancellationToken cancellationToken)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (!await CanAccessMemberAsync("ExpectedEmail", SecurityOperation.Update, municipalityId)) return Forbid();
        if ((!string.IsNullOrWhiteSpace(request.Issuer) && !await CanAccessMemberAsync("Issuer", SecurityOperation.Update, municipalityId))
            || (!string.IsNullOrWhiteSpace(request.Subject) && !await CanAccessMemberAsync("Subject", SecurityOperation.Update, municipalityId)))
            return Forbid();
        if (!ValidReason(request.Reason)) return BadRequest(Fail<UserAuthenticatorDto>("A governance reason between 5 and 500 characters is required."));
        if (!providers.TryGet(request.ProviderRegistrationCode.Trim(), out var provider)) return BadRequest(Fail<UserAuthenticatorDto>("The provider is not registered by deployment configuration."));
        var user = await context.Users.SingleOrDefaultAsync(item => item.PublicId == request.UserPublicId && item.MunicipalityId == municipalityId, cancellationToken);
        if (user == null) return NotFound(Fail<UserAuthenticatorDto>("User not found in the current municipality."));
        var email = request.ExpectedEmail.Trim();
        if (email.Length is < 3 or > 320 || !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
            return BadRequest(Fail<UserAuthenticatorDto>("ExpectedEmail must match the user's verified account email."));
        var issuer = NullIfWhiteSpace(request.Issuer)?.TrimEnd('/');
        var subject = NullIfWhiteSpace(request.Subject);
        if ((issuer == null) != (subject == null)) return BadRequest(Fail<UserAuthenticatorDto>("Issuer and Subject must be supplied together."));
        var row = new UserAuthenticator
        {
            MunicipalityId = municipalityId,
            UserId = user.Id,
            ProviderRegistrationCode = provider.Code,
            ExpectedEmail = email,
            Issuer = issuer,
            Subject = subject,
            ExternalIdentityHash = issuer == null ? null : EnterpriseAuthenticationService.HashIdentity(provider.Code, issuer, subject!),
            IsActive = true,
            CreatedByUserId = actor,
            LinkedByUserId = issuer == null ? null : actor,
            LinkedAt = issuer == null ? null : DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.UserAuthenticators.Add(row);
        AddAudit(municipalityId, actor, "UserAuthenticator", "EnterpriseIdentityProvisioned", row.PublicId.ToString(), request.Reason,
            new { user.PublicId, Provider = provider.Code, Prelinked = issuer != null });
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(Fail<UserAuthenticatorDto>("That user or external identity is already linked for this provider.")); }
        return Ok(new ApiResponse<UserAuthenticatorDto>(true, await ToAuthorizedDtoAsync(row, user, municipalityId)));
    }

    [HttpPut("authenticators/{publicId:guid}/status")]
    [Authorize(Policy = "Permission:AUTHENTICATION.LINK_IDENTITIES")]
    public async Task<ActionResult<ApiResponse<UserAuthenticatorDto>>> SetStatus(Guid publicId, SetUserAuthenticatorStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (!ValidReason(request.Reason) || !TryDecode(request.RowVersion, out var version))
            return BadRequest(Fail<UserAuthenticatorDto>("A governance reason and valid RowVersion are required."));
        var row = await context.UserAuthenticators.Include(item => item.User).SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);
        if (row == null) return NotFound(Fail<UserAuthenticatorDto>("Authenticator not found."));
        context.Entry(row).Property(item => item.RowVersion).OriginalValue = version;
        row.IsActive = request.IsActive;
        row.DisabledAt = request.IsActive ? null : DateTime.UtcNow;
        row.DisabledByUserId = request.IsActive ? null : actor;
        AddAudit(municipalityId, actor, "UserAuthenticator", request.IsActive ? "EnterpriseIdentityEnabled" : "EnterpriseIdentityDisabled", row.PublicId.ToString(), request.Reason, null);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<UserAuthenticatorDto>("The authenticator changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<UserAuthenticatorDto>(true, await ToAuthorizedDtoAsync(row, row.User, municipalityId)));
    }

    [HttpGet("events")]
    [Authorize(Policy = "Permission:AUTHENTICATION.VIEW_EVENTS")]
    public ActionResult<ApiResponse<AuthenticationEventDto[]>> GetEvents() => StatusCode(StatusCodes.Status410Gone,
        Fail<AuthenticationEventDto[]>("The fixed-limit authentication-event route is retired. Use /api/v1/admin/authentication/events/page."));

    [HttpGet("events/page")]
    [Authorize(Policy = "Permission:AUTHENTICATION.VIEW_EVENTS")]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuthenticationEventDto>>>> GetEventsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool? success = null,
        [FromQuery] string? providerCode = null,
        [FromQuery] string? eventType = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryTenant(out var municipalityId, out var failure)) return failure!;
        if (request.NormalizedSortBy is not ("createdat" or "occurredat" or "provider" or "eventtype" or "result"))
            return BadRequest(Fail<PagedResponse<AuthenticationEventDto>>("SortBy must be createdAt, occurredAt, provider, eventType, or result."));

        var canReadEventUser = await CanAccessMemberAsync("EventUserId", SecurityOperation.Read, municipalityId);
        var canReadEventIp = await CanAccessMemberAsync("EventIpAddress", SecurityOperation.Read, municipalityId);

        var query = context.AuthenticationEvents.AsNoTracking().Where(item => item.MunicipalityId == municipalityId);
        if (success.HasValue) query = query.Where(item => item.Success == success.Value);
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            var provider = providerCode.Trim();
            query = query.Where(item => item.ProviderCode == provider);
        }
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = eventType.Trim();
            query = query.Where(item => item.EventType == type);
        }
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            var userPublicId = Guid.TryParse(term, out var parsedUserPublicId) ? parsedUserPublicId : (Guid?)null;
            query = query.Where(item => item.ProviderCode.Contains(term)
                || item.EventType.Contains(term)
                || canReadEventUser && userPublicId.HasValue && item.User != null && item.User.PublicId == userPublicId.Value
                || item.FailureCode != null && item.FailureCode.Contains(term)
                || canReadEventIp && item.IpAddress != null && item.IpAddress.Contains(term)
                || item.CorrelationId.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("provider", false) => query.OrderBy(item => item.ProviderCode).ThenByDescending(item => item.OccurredAt).ThenBy(item => item.Id),
            ("provider", true) => query.OrderByDescending(item => item.ProviderCode).ThenByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id),
            ("eventtype", false) => query.OrderBy(item => item.EventType).ThenByDescending(item => item.OccurredAt).ThenBy(item => item.Id),
            ("eventtype", true) => query.OrderByDescending(item => item.EventType).ThenByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id),
            ("result", false) => query.OrderBy(item => item.Success).ThenByDescending(item => item.OccurredAt).ThenBy(item => item.Id),
            ("result", true) => query.OrderByDescending(item => item.Success).ThenByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id)
        };
        var rows = await query.Include(item => item.User).Skip(request.Offset).Take(request.PageSize).ToArrayAsync(cancellationToken);
        return Ok(new ApiResponse<PagedResponse<AuthenticationEventDto>>(true,
            PagedResponse<AuthenticationEventDto>.Create(rows.Select(item => ToDto(item, canReadEventUser, canReadEventIp)), request.Page, request.PageSize, totalCount)));
    }

    private bool TryTenant(out long municipalityId, out ActionResult? failure)
    {
        municipalityId = tenant.MunicipalityId ?? 0;
        failure = municipalityId <= 0 ? BadRequest(Fail<object>("Select a municipality context.")) : null;
        return failure == null;
    }

    private bool TryValidateProvider(AuthenticationMode mode, string? requested, out string? code, out string? error)
    {
        code = NullIfWhiteSpace(requested)?.ToUpperInvariant(); error = null;
        if (mode == AuthenticationMode.Local) { if (code != null) error = "LOCAL mode cannot specify an enterprise provider."; return error == null; }
        if (code == null || !providers.TryGet(code, out var provider)) { error = "The selected enterprise provider is not registered by deployment configuration."; return false; }
        if (!EnterpriseAuthenticationService.ModeAllows(mode, provider.Kind)) { error = "The selected provider kind does not match the authentication mode."; return false; }
        return true;
    }

    private static bool ValidPolicy(AuthenticationPolicyRequest value, out string? error)
    {
        error = null;
        if (value.MinimumPasswordLength is < 12 or > 128 || value.MaximumFailedAttempts is < 1 or > 20
            || value.LockoutMinutes is < 1 or > 1440 || value.SessionIdleTimeoutMinutes is < 5 or > 1440
            || value.SessionAbsoluteTimeoutHours is < 1 or > 720 || value.MaximumConcurrentSessions is < 1 or > 50)
            error = "Authentication policy values are outside the supported security bounds.";
        return error == null;
    }

    private static void ApplyPolicy(AuthenticationPolicy entity, AuthenticationPolicyRequest value, string actor)
    {
        entity.MinimumPasswordLength = value.MinimumPasswordLength; entity.MaximumFailedAttempts = value.MaximumFailedAttempts;
        entity.LockoutMinutes = value.LockoutMinutes; entity.RequireMfaForPrivilegedLocalUsers = value.RequireMfaForPrivilegedLocalUsers;
        entity.RequireMfaForAllLocalUsers = value.RequireMfaForAllLocalUsers; entity.RequireFirstLoginPasswordChange = value.RequireFirstLoginPasswordChange;
        entity.SessionIdleTimeoutMinutes = value.SessionIdleTimeoutMinutes; entity.SessionAbsoluteTimeoutHours = value.SessionAbsoluteTimeoutHours;
        entity.MaximumConcurrentSessions = value.MaximumConcurrentSessions; entity.ModifiedByUserId = actor; entity.ModifiedAt = DateTime.UtcNow;
    }

    private void AddAudit(long municipalityId, string actor, string entityName, string action, string entityId, string reason, object? value) => context.AuditTrails.Add(new AuditTrail
    {
        MunicipalityId = municipalityId, EntityName = entityName, EntityId = entityId, Action = action,
        OldValue = JsonSerializer.Serialize(new { Reason = reason }), NewValue = value == null ? null : JsonSerializer.Serialize(value),
        ChangedBy = actor, ChangedAt = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        CorrelationId = HttpContext.TraceIdentifier, UserAgent = Request.Headers.UserAgent.ToString()
    });

    private bool SetVersion<TEntity>(TEntity entity, string? value) where TEntity : class { if (!TryDecode(value, out var bytes)) return false; context.Entry(entity).Property("RowVersion").OriginalValue = bytes; return true; }
    private static bool TryDecode(string? value, out byte[] bytes) { bytes = []; try { if (string.IsNullOrWhiteSpace(value)) return false; bytes = Convert.FromBase64String(value); return bytes.Length > 0; } catch (FormatException) { return false; } }
    private static bool ValidReason(string? value) => value?.Trim().Length is >= 5 and <= 500;
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private static AuthenticationConfigurationDto ToDto(AuthenticationConfiguration item) => new(item.PublicId, item.ConfigurationFamilyPublicId,
        item.VersionNumber, item.IsCurrent, item.Mode, item.ProviderRegistrationCode, item.DisplayName, item.IsActive,
        item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion), item.Policy == null ? null : ToDto(item.Policy));
    private static AuthenticationPolicyDto ToDto(AuthenticationPolicy item) => new(item.PublicId, item.MinimumPasswordLength, item.MaximumFailedAttempts, item.LockoutMinutes, item.RequireMfaForPrivilegedLocalUsers, item.RequireMfaForAllLocalUsers, item.RequireFirstLoginPasswordChange, item.SessionIdleTimeoutMinutes, item.SessionAbsoluteTimeoutHours, item.MaximumConcurrentSessions, Convert.ToBase64String(item.RowVersion));
    private static UserAuthenticatorDto ToDto(UserAuthenticator item, ApplicationUser user, bool includeUserEmail, bool includeExpectedEmail, bool includeIssuer, bool includeSubject) =>
        new(item.PublicId, user.PublicId, includeUserEmail ? user.Email : null, item.ProviderRegistrationCode,
            includeExpectedEmail ? item.ExpectedEmail : null, includeIssuer ? item.Issuer : null, includeSubject ? item.Subject : null,
            item.IsActive, item.LinkedAt, item.LastAuthenticatedAt, Convert.ToBase64String(item.RowVersion));
    private static AuthenticationEventDto ToDto(AuthenticationEvent item, bool includeUserId, bool includeIpAddress) =>
        new(item.PublicId, includeUserId ? item.User?.PublicId : null, item.ProviderCode, item.EventType, item.Success, item.FailureCode,
            item.OccurredAt, includeIpAddress ? item.IpAddress : null, item.CorrelationId);

    private async Task<UserAuthenticatorDto> ToAuthorizedDtoAsync(UserAuthenticator item, ApplicationUser user, long municipalityId) =>
        ToDto(item, user,
            await CanAccessMemberAsync("UserEmail", SecurityOperation.Read, municipalityId),
            await CanAccessMemberAsync("ExpectedEmail", SecurityOperation.Read, municipalityId),
            await CanAccessMemberAsync("Issuer", SecurityOperation.Read, municipalityId),
            await CanAccessMemberAsync("Subject", SecurityOperation.Read, municipalityId));

    private async Task<bool> CanAccessMemberAsync(string memberCode, SecurityOperation operation, long municipalityId)
    {
        var actorId = tenant.UserId ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return false;
        var actor = await context.Users.SingleOrDefaultAsync(item => item.Id == actorId && item.MunicipalityId == municipalityId);
        if (actor == null) return false;
        var decision = await accessControl.CheckPermissionAsync(actor,
            $"AUTHENTICATION.{memberCode}.{operation.ToString().ToUpperInvariant()}",
            new AccessScopeContext(MunicipalityId: municipalityId));
        return decision.Allowed;
    }
}

public sealed record AuthenticationConfigurationDto(Guid PublicId, Guid ConfigurationFamilyPublicId, int VersionNumber, bool IsCurrent,
    AuthenticationMode Mode, string? ProviderRegistrationCode, string DisplayName, bool IsActive, DateTime EffectiveFrom,
    DateTime? EffectiveTo, string RowVersion, AuthenticationPolicyDto? Policy);
public sealed record AuthenticationPolicyDto(Guid PublicId, int MinimumPasswordLength, int MaximumFailedAttempts, int LockoutMinutes, bool RequireMfaForPrivilegedLocalUsers, bool RequireMfaForAllLocalUsers, bool RequireFirstLoginPasswordChange, int SessionIdleTimeoutMinutes, int SessionAbsoluteTimeoutHours, int MaximumConcurrentSessions, string RowVersion);
public sealed record UserAuthenticatorDto(Guid PublicId, Guid UserPublicId, string? UserEmail, string ProviderRegistrationCode, string? ExpectedEmail, string? Issuer, string? Subject, bool IsActive, DateTime? LinkedAt, DateTime? LastAuthenticatedAt, string RowVersion);
public sealed record AuthenticationEventDto(Guid PublicId, Guid? UserPublicId, string ProviderCode, string EventType, bool Success, string? FailureCode, DateTime OccurredAt, string? IpAddress, string CorrelationId);
public sealed record AuthenticationPolicyRequest(int MinimumPasswordLength, int MaximumFailedAttempts, int LockoutMinutes, bool RequireMfaForPrivilegedLocalUsers, bool RequireMfaForAllLocalUsers, bool RequireFirstLoginPasswordChange, int SessionIdleTimeoutMinutes, int SessionAbsoluteTimeoutHours, int MaximumConcurrentSessions, string? RowVersion);
public sealed record SaveAuthenticationConfigurationRequest(AuthenticationMode Mode, string? ProviderRegistrationCode, string DisplayName, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, AuthenticationPolicyRequest Policy, string Reason, string? RowVersion);
public sealed record ProvisionUserAuthenticatorRequest(Guid UserPublicId, string ProviderRegistrationCode, string ExpectedEmail, string? Issuer, string? Subject, string Reason);
public sealed record SetUserAuthenticatorStatusRequest(bool IsActive, string Reason, string RowVersion);

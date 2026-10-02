using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Auth;

public sealed record EnterpriseIdentityResult(bool Succeeded, ApplicationUser? User, string? FailureCode)
{
    public static EnterpriseIdentityResult Success(ApplicationUser user) => new(true, user, null);
    public static EnterpriseIdentityResult Failure(string code) => new(false, null, code);
}

public interface IEnterpriseAuthenticationService
{
    Task<EnterpriseIdentityResult> ResolveAsync(long municipalityId, string providerCode, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken);
}

public sealed class EnterpriseAuthenticationService(ApplicationDbContext context, IEnterpriseProviderRegistry providers) : IEnterpriseAuthenticationService
{
    public async Task<EnterpriseIdentityResult> ResolveAsync(long municipalityId, string providerCode, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken)
    {
        httpContext.Items[TenantResolutionMiddleware.MunicipalityItem] = municipalityId;
        providerCode = providerCode.Trim().ToUpperInvariant();
        if (!providers.TryGet(providerCode, out var provider)) return EnterpriseIdentityResult.Failure("PROVIDER_NOT_REGISTERED");

        var now = DateTime.UtcNow;
        var configuration = await context.AuthenticationConfigurations.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item => item.MunicipalityId == municipalityId && item.IsActive && item.EffectiveFrom <= now
                && (!item.EffectiveTo.HasValue || item.EffectiveTo > now), cancellationToken);
        if (configuration == null || !string.Equals(configuration.ProviderRegistrationCode, providerCode, StringComparison.OrdinalIgnoreCase)
            || !ModeAllows(configuration.Mode, provider.Kind))
            return await FailAsync(municipalityId, null, providerCode, "CONFIGURATION_DENIED", httpContext, cancellationToken);

        var subjectClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        var issuer = (principal.FindFirstValue("iss") ?? subjectClaim?.Issuer ?? string.Empty).Trim().TrimEnd('/');
        var subject = (subjectClaim?.Value ?? string.Empty).Trim();
        var email = (principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("preferred_username") ?? principal.FindFirstValue("upn") ?? string.Empty).Trim();
        if (issuer.Length == 0 || subject.Length == 0)
            return await FailAsync(municipalityId, null, providerCode, "IDENTITY_CLAIMS_MISSING", httpContext, cancellationToken);

        var identityHash = HashIdentity(providerCode, issuer, subject);
        var authenticator = await context.UserAuthenticators.IgnoreQueryFilters().Include(item => item.User)
            .SingleOrDefaultAsync(item => item.ExternalIdentityHash == identityHash, cancellationToken);
        if (authenticator == null)
        {
            if (email.Length == 0) return await FailAsync(municipalityId, null, providerCode, "EMAIL_CLAIM_MISSING", httpContext, cancellationToken);
            var normalizedEmail = email.ToUpperInvariant();
            var candidates = await context.UserAuthenticators.IgnoreQueryFilters().Include(item => item.User)
                .Where(item => item.MunicipalityId == municipalityId && item.ProviderRegistrationCode == providerCode
                    && item.ExternalIdentityHash == null && item.IsActive && item.ExpectedEmail.ToUpper() == normalizedEmail)
                .Take(2).ToArrayAsync(cancellationToken);
            if (candidates.Length != 1)
                return await FailAsync(municipalityId, null, providerCode, candidates.Length == 0 ? "IDENTITY_NOT_PREPROVISIONED" : "IDENTITY_LINK_AMBIGUOUS", httpContext, cancellationToken);
            authenticator = candidates[0];
            authenticator.Issuer = issuer;
            authenticator.Subject = subject;
            authenticator.ExternalIdentityHash = identityHash;
            authenticator.LinkedAt = now;
            authenticator.LinkedByUserId = authenticator.UserId;
        }

        if (!authenticator.IsActive || authenticator.MunicipalityId != municipalityId || !authenticator.User.IsActive
            || authenticator.User.MunicipalityId != municipalityId)
            return await FailAsync(municipalityId, authenticator.UserId, providerCode, "ACCOUNT_DISABLED_OR_SCOPE_MISMATCH", httpContext, cancellationToken);

        authenticator.LastAuthenticatedAt = now;
        authenticator.User.LastLoginAt = now;
        AddEvent(municipalityId, authenticator.UserId, providerCode, "ExternalSignIn", true, null, httpContext);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return EnterpriseIdentityResult.Failure("IDENTITY_LINK_CONFLICT");
        }
        return EnterpriseIdentityResult.Success(authenticator.User);
    }

    private async Task<EnterpriseIdentityResult> FailAsync(long municipalityId, string? userId, string providerCode, string failureCode, HttpContext httpContext, CancellationToken cancellationToken)
    {
        AddEvent(municipalityId, userId, providerCode, "ExternalSignIn", false, failureCode, httpContext);
        await context.SaveChangesAsync(cancellationToken);
        return EnterpriseIdentityResult.Failure(failureCode);
    }

    private void AddEvent(long municipalityId, string? userId, string providerCode, string eventType, bool success, string? failureCode, HttpContext httpContext) =>
        context.AuthenticationEvents.Add(new AuthenticationEvent
        {
            MunicipalityId = municipalityId,
            UserId = userId,
            ProviderCode = providerCode,
            EventType = eventType,
            Success = success,
            FailureCode = failureCode,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CorrelationId = httpContext.TraceIdentifier,
            OccurredAt = DateTime.UtcNow
        });

    internal static bool ModeAllows(AuthenticationMode mode, string providerKind) => mode == AuthenticationMode.Hybrid
        || mode == AuthenticationMode.MicrosoftEntraId && providerKind == EnterpriseProviderKinds.MicrosoftEntraId
        || mode == AuthenticationMode.ActiveDirectory && providerKind == EnterpriseProviderKinds.ActiveDirectory;

    internal static string HashIdentity(string providerCode, string issuer, string subject) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{providerCode.ToUpperInvariant()}\n{issuer.Trim().TrimEnd('/')}\n{subject.Trim()}")));
}

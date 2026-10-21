using System.Security.Claims;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Security;

public interface ITenantContext
{
    long? MunicipalityId { get; }
    bool IsSystem { get; }
    string? UserId { get; }
}

public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public long? MunicipalityId => accessor.HttpContext?.Items[TenantResolutionMiddleware.MunicipalityItem] as long?;
    public bool IsSystem => accessor.HttpContext == null || (accessor.HttpContext.Items[TenantResolutionMiddleware.SystemItem] as bool? ?? false);
    public string? UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}

public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware>? logger = null)
{
    internal const string MunicipalityItem = "OPMS.MunicipalityId";
    internal const string SystemItem = "OPMS.SystemScope";
    public const string HeaderName = "X-Municipality-Public-Id";
    public const string LegacyHeaderName = "X-Municipality-Id";

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, IAccessControlService accessControl, ApplicationDbContext dbContext)
    {
        // Authentication endpoints explicitly marked anonymous must remain reachable even when
        // the browser carries an expired authentication cookie and a stale tenant selection.
        // Their controllers resolve the relevant account/municipality from the submitted
        // authentication contract, so a previous session must not gate the new sign-in flow.
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) { await next(context); return; }
        var user = await userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
        var access = await accessControl.GetEffectiveAccessAsync(user);
        var system = access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
        var allowed = access.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArray();
        long? requested = null;
        if (context.Request.Headers.TryGetValue(LegacyHeaderName, out var legacyValue)
            && !string.IsNullOrWhiteSpace(legacyValue.ToString()))
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status410Gone,
                $"{LegacyHeaderName} is retired. Use {HeaderName} with the municipality PublicId.", "TENANT_CONTEXT_PRIVATE_KEY_RETIRED");
            return;
        }
        if (context.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            if (!Guid.TryParse(value, out var publicId))
            {
                await ApiProblemDetails.WriteAsync(context, StatusCodes.Status403Forbidden,
                    "The requested municipality context is not authorized.", "TENANT_CONTEXT_DENIED");
                return;
            }
            var resolved = await dbContext.Municipalities.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.PublicId == publicId && item.IsActive)
                .Select(item => (long?)item.Id)
                .SingleOrDefaultAsync(context.RequestAborted);
            if (!resolved.HasValue || (!system && !allowed.Contains(resolved.Value)))
            {
                logger?.LogWarning(
                    "Rejected requested municipality context. System scope: {SystemScope}; active assigned municipalities: {AllowedCount}; municipality resolved: {Resolved}; assignment matched: {AssignmentMatched}",
                    system,
                    allowed.Length,
                    resolved.HasValue,
                    resolved.HasValue && allowed.Contains(resolved.Value));
                await ApiProblemDetails.WriteAsync(context, StatusCodes.Status403Forbidden,
                    "The requested municipality context is not authorized.", "TENANT_CONTEXT_DENIED");
                return;
            }
            requested = resolved.Value;
        }
        else if (allowed.Length == 1) requested = allowed[0];
        context.Items[SystemItem] = system;
        if (requested.HasValue) context.Items[MunicipalityItem] = requested.Value;
        else if (!system) context.Items[MunicipalityItem] = long.MinValue;
        await next(context);
    }
}

using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FTCERP.Host.Infrastructure.Auth;

public static class MfaRequirementPolicy
{
    public const string EnrollmentRequiredClaim = "mfa_enrollment_required";

    public static bool IsEnrollmentRequired(bool twoFactorEnabled, IEnumerable<string> effectivePermissions, IEnumerable<string> requiredPermissions)
    {
        if (twoFactorEnabled) return false;
        var governed = requiredPermissions.Where(code => !string.IsNullOrWhiteSpace(code)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return effectivePermissions.Any(permission => permission == "*" || governed.Contains(permission));
    }
}

public sealed class MfaEnrollmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Request.Path.StartsWithSegments("/api")
            && await RequiresEnrollmentAsync(context)
            && !IsEnrollmentEndpoint(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>(false, null, "MFA_ENROLLMENT_REQUIRED"));
            return;
        }

        await next(context);
    }

    private static async Task<bool> RequiresEnrollmentAsync(HttpContext context)
    {
        if (context.User.HasClaim(MfaRequirementPolicy.EnrollmentRequiredClaim, "true")) return true;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var users = context.RequestServices.GetService<UserManager<ApplicationUser>>();
        var access = context.RequestServices.GetService<IAccessControlService>();
        var settings = context.RequestServices.GetService<IOptions<JwtSettings>>()?.Value;
        if (users == null || access == null || settings == null) return false;
        var user = await users.FindByIdAsync(userId);
        if (user == null || user.TwoFactorEnabled) return false;
        var permissions = (await access.GetEffectiveAccessAsync(user)).EffectivePermissions;
        return MfaRequirementPolicy.IsEnrollmentRequired(false, permissions, settings.MfaRequiredPermissionCodes);
    }

    private static bool IsEnrollmentEndpoint(PathString path) =>
        path.StartsWithSegments("/api/v1/auth/mfa")
        || path.StartsWithSegments("/api/v1/auth/password")
        || path.StartsWithSegments("/api/auth/logout")
        || path.StartsWithSegments("/api/auth/me");
}

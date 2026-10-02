using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FTCERP.Host.Infrastructure.Auth;

public static class PasswordChangePolicy
{
    public const string ChangeRequiredClaim = "password_change_required";
}

public sealed class PasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Request.Path.StartsWithSegments("/api")
            && await RequiresPasswordChangeAsync(context)
            && !IsPasswordEndpoint(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>(false, null, "PASSWORD_CHANGE_REQUIRED"));
            return;
        }

        await next(context);
    }

    private static async Task<bool> RequiresPasswordChangeAsync(HttpContext context)
    {
        if (context.User.HasClaim(PasswordChangePolicy.ChangeRequiredClaim, "true")) return true;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var users = context.RequestServices.GetService<UserManager<ApplicationUser>>();
        return users != null && (await users.FindByIdAsync(userId))?.MustChangePassword == true;
    }

    private static bool IsPasswordEndpoint(PathString path) =>
        path.StartsWithSegments("/api/v1/auth/password")
        || path.StartsWithSegments("/api/auth/logout")
        || path.StartsWithSegments("/api/auth/me");
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace FTCERP.Host.Infrastructure.Auth;

public static class AuthCookiePolicy
{
    public const string AccessCookieName = "opms_access";
    public const string RefreshCookieName = "opms_refresh";
    public const string DevelopmentAccessCookieName = "opms_dev_access";
    public const string DevelopmentRefreshCookieName = "opms_dev_refresh";
    public const string AccessPath = "/api";
    public const string RefreshPath = "/api/auth";

    public static string GetAccessCookieName(IHostEnvironment environment) =>
        environment.IsDevelopment() ? DevelopmentAccessCookieName : AccessCookieName;

    public static string GetRefreshCookieName(IHostEnvironment environment) =>
        environment.IsDevelopment() ? DevelopmentRefreshCookieName : RefreshCookieName;

    public static CookieOptions Create(IHostEnvironment environment, string path, TimeSpan? maxAge = null) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = path,
        MaxAge = maxAge.HasValue
            ? (maxAge.Value > TimeSpan.Zero ? maxAge.Value : TimeSpan.FromSeconds(1))
            : null,
        IsEssential = true
    };
}

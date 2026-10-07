using System.Text.Json;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FTCERP.Tests;

public sealed class AuthCookiePolicyTests
{
    [Fact]
    public void Session_cookies_are_http_only_strict_and_path_scoped()
    {
        var environment = new TestEnvironment { EnvironmentName = Environments.Production };

        var access = AuthCookiePolicy.Create(environment, AuthCookiePolicy.AccessPath, TimeSpan.FromMinutes(15));
        var refresh = AuthCookiePolicy.Create(environment, AuthCookiePolicy.RefreshPath, TimeSpan.FromDays(7));

        Assert.True(access.HttpOnly);
        Assert.True(access.Secure);
        Assert.True(access.IsEssential);
        Assert.Equal(SameSiteMode.Strict, access.SameSite);
        Assert.Equal("/api", access.Path);
        Assert.Equal(TimeSpan.FromMinutes(15), access.MaxAge);
        Assert.Equal("/api/auth", refresh.Path);
    }

    [Fact]
    public void Login_response_does_not_serialize_access_or_refresh_tokens()
    {
        var profile = new UserProfileResponse(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user@example.test", "Test", "User", "Test User", "user@example.test", null, null, null, true, false);
        var response = new LoginResponse(DateTime.UtcNow.AddMinutes(15), profile, [], [], [], false);

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("accessToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Id\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"UserId\":", json, StringComparison.Ordinal);
        Assert.Contains("\"PublicId\":\"00000000-0000-0000-0000-000000000001\"", json, StringComparison.Ordinal);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "FTCERP.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace FTCERP.Tests;

public sealed class AuthSessionTests
{
    [Fact]
    public async Task Sqlite_enforces_unique_session_token_digests()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = User("sqlite-user"); context.Users.Add(user); await context.SaveChangesAsync();
        var now = DateTime.UtcNow;
        context.RefreshTokens.Add(new RefreshToken { UserId = user.Id, Token = new string('A', 64), SessionId = Guid.NewGuid(), ExpiresAt = now.AddHours(1), AbsoluteExpiresAt = now.AddHours(2), LastUsedAt = now, SecurityStamp = user.SecurityStamp!, RowVersion = [1] });
        await context.SaveChangesAsync();
        context.RefreshTokens.Add(new RefreshToken { UserId = user.Id, Token = new string('A', 64), SessionId = Guid.NewGuid(), ExpiresAt = now.AddHours(1), AbsoluteExpiresAt = now.AddHours(2), LastUsedAt = now, SecurityStamp = user.SecurityStamp!, RowVersion = [1] });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Token_generation_creates_hashed_bounded_session_and_access_claims()
    {
        await using var context = NewContext();
        var user = User("user-1");
        context.Users.Add(user); await context.SaveChangesAsync();
        var service = CreateService(context, user);

        var (_, rawRefreshToken, _) = await service.GenerateTokensAsync(user, "127.0.0.1", "Test Browser");

        var session = await context.RefreshTokens.SingleAsync();
        Assert.NotEqual(rawRefreshToken, session.Token);
        Assert.Equal(64, session.Token.Length);
        Assert.Equal("127.0.0.1", session.CreatedByIp);
        Assert.Equal("Test Browser", session.UserAgent);
        Assert.Equal(user.SecurityStamp, session.SecurityStamp);
        Assert.InRange(session.AbsoluteExpiresAt, DateTime.UtcNow.AddHours(23), DateTime.UtcNow.AddHours(25));
        var principal = await service.GetPrincipalFromExpiredTokenAsync((await service.GenerateTokensAsync(user)).AccessToken);
        Assert.NotNull(principal);
        Assert.True(Guid.TryParse(principal!.FindFirstValue("sid"), out _));
        Assert.Equal(user.SecurityStamp, principal!.FindFirstValue("security_stamp"));
    }

    [Fact]
    public async Task Revoked_session_immediately_invalidates_its_access_family()
    {
        await using var context = NewContext();
        var user = User("user-2"); context.Users.Add(user); await context.SaveChangesAsync();
        var service = CreateService(context, user);
        await service.GenerateTokensAsync(user);
        var session = await context.RefreshTokens.SingleAsync();

        Assert.True(await service.ValidateAccessSessionAsync(user.Id, session.SessionId, user.SecurityStamp!, "127.0.0.1"));
        Assert.True(await service.RevokeSessionAsync(user.Id, session.SessionId, "127.0.0.1", "User signed out this device"));
        Assert.False(await service.ValidateAccessSessionAsync(user.Id, session.SessionId, user.SecurityStamp!, "127.0.0.1"));
        Assert.Equal("User signed out this device", (await context.RefreshTokens.SingleAsync()).RevokedReason);
    }

    [Fact]
    public async Task Concurrent_session_limit_revokes_the_oldest_family()
    {
        await using var context = NewContext();
        var user = User("user-3"); context.Users.Add(user); await context.SaveChangesAsync();
        var service = CreateService(context, user, maximumSessions: 2);
        await service.GenerateTokensAsync(user, userAgent: "First");
        await Task.Delay(5);
        await service.GenerateTokensAsync(user, userAgent: "Second");
        await Task.Delay(5);
        await service.GenerateTokensAsync(user, userAgent: "Third");

        var active = await service.GetActiveSessionsAsync(user.Id);
        Assert.Equal(2, active.Length);
        Assert.DoesNotContain(active, item => item.UserAgent == "First");
        Assert.Equal("Concurrent session limit", (await context.RefreshTokens.SingleAsync(item => item.UserAgent == "First")).RevokedReason);
    }

    [Fact]
    public async Task Active_session_page_is_distinct_searchable_and_stably_sorted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var user = User("paged-session-user"); context.Users.Add(user); await context.SaveChangesAsync();
        var service = CreateService(context, user, maximumSessions: 50);
        for (var index = 1; index <= 12; index++)
            await service.GenerateTokensAsync(user, $"10.0.0.{index}", $"Device {index:00}", authenticationMethod: index % 2 == 0 ? "ENTRA" : "LOCAL");

        var secondPage = await service.GetActiveSessionsPageAsync(user.Id, 2, 5, null, "device", false);
        secondPage.TotalCount.Should().Be(12);
        secondPage.Items.Select(item => item.UserAgent).Should().Equal("Device 06", "Device 07", "Device 08", "Device 09", "Device 10");

        var filtered = await service.GetActiveSessionsPageAsync(user.Id, 1, 10, "10.0.0.12", "lastusedat", true);
        filtered.TotalCount.Should().Be(1);
        filtered.Items.Should().ContainSingle(item => item.UserAgent == "Device 12" && item.AuthenticationMethod == "ENTRA");
    }

    [Fact]
    public async Task Session_controller_retires_array_and_returns_authoritative_page()
    {
        var sessionId = Guid.NewGuid();
        var jwt = new Mock<IJwtService>();
        jwt.Setup(service => service.GetActiveSessionsPageAsync("session-user", 2, 10, "browser", "lastusedat", true))
            .ReturnsAsync(new ActiveSessionPage([
                new RefreshToken
                {
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-2),
                    AbsoluteExpiresAt = DateTime.UtcNow.AddHours(2),
                    UserAgent = "Test Browser",
                    AuthenticationMethod = "LOCAL"
                }
            ], 11));
        var controller = new AuthController(null!, null!, jwt.Object, null!, null!, Options.Create(new JwtSettings()), null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, "session-user"),
                        new Claim("sid", sessionId.ToString())
                    ], "test"))
                }
            }
        };

        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetSessions().Result).StatusCode);
        var response = await controller.GetSessionsPage(new PagedQueryRequest
        {
            Page = 2, PageSize = 10, Search = "browser", SortBy = "lastUsedAt", SortDirection = "desc"
        });
        var page = Assert.IsType<ApiResponse<PagedResponse<AuthSessionResponse>>>(Assert.IsType<OkObjectResult>(response.Result).Value).Data!;
        page.TotalCount.Should().Be(11);
        page.TotalPages.Should().Be(2);
        page.Items.Should().ContainSingle(item => item.IsCurrent && item.UserAgent == "Test Browser");
        Assert.IsType<BadRequestObjectResult>((await controller.GetSessionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
    }

    [Fact]
    public async Task Privileged_effective_permission_requires_enrollment_without_hard_coded_role()
    {
        await using var context = NewContext();
        var user = User("privileged-user"); context.Users.Add(user); await context.SaveChangesAsync();
        var service = CreateService(context, user, permissions: ["SECURITY.MANAGE_ROLES"]);

        var token = (await service.GenerateTokensAsync(user)).AccessToken;
        var principal = await service.GetPrincipalFromExpiredTokenAsync(token);

        Assert.Equal("true", principal!.FindFirstValue(MfaRequirementPolicy.EnrollmentRequiredClaim));
        Assert.False(MfaRequirementPolicy.IsEnrollmentRequired(true, ["SECURITY.MANAGE_ROLES"], ["SECURITY.MANAGE_ROLES"]));
        Assert.False(MfaRequirementPolicy.IsEnrollmentRequired(false, ["PERFORMANCE.VIEW"], ["SECURITY.MANAGE_ROLES"]));
    }

    [Fact]
    public async Task Municipality_policy_controls_session_limits_expiry_and_local_mfa_only()
    {
        await using var context = NewContext();
        var user = User("policy-user"); context.Users.Add(user); await context.SaveChangesAsync();
        var policies = new Mock<IAuthenticationPolicyResolver>();
        policies.Setup(service => service.ResolveAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EffectiveAuthenticationPolicy(16, 3, 60, true, true, true, 10, 2, 1));
        var service = CreateService(context, user, policyResolver: policies.Object);

        var local = await service.GenerateTokensAsync(user, authenticationMethod: "LOCAL");
        var localPrincipal = await service.GetPrincipalFromExpiredTokenAsync(local.AccessToken);
        var external = await service.GenerateTokensAsync(user, authenticationMethod: "entra");
        var externalPrincipal = await service.GetPrincipalFromExpiredTokenAsync(external.AccessToken);

        localPrincipal.Should().NotBeNull();
        externalPrincipal.Should().NotBeNull();
        localPrincipal!.FindFirstValue(MfaRequirementPolicy.EnrollmentRequiredClaim).Should().Be("true");
        externalPrincipal!.FindFirstValue(MfaRequirementPolicy.EnrollmentRequiredClaim).Should().BeNull();
        new JwtSecurityTokenHandler().ReadJwtToken(external.AccessToken).Claims.Single(claim => claim.Type == "amr").Value.Should().Be("ENTRA");
        var sessions = await context.RefreshTokens.OrderBy(item => item.CreatedAt).ToArrayAsync();
        sessions[0].RevokedReason.Should().Be("Concurrent session limit");
        sessions[1].AuthenticationMethod.Should().Be("ENTRA");
        sessions[1].AbsoluteExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Enrollment_middleware_blocks_direct_api_calls_but_allows_mfa_endpoints()
    {
        var downstreamCalled = false;
        var middleware = new MfaEnrollmentMiddleware(_ => { downstreamCalled = true; return Task.CompletedTask; });
        var blocked = new DefaultHttpContext();
        blocked.Request.Path = "/api/v1/security/roles";
        blocked.Response.Body = new MemoryStream();
        blocked.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(MfaRequirementPolicy.EnrollmentRequiredClaim, "true")], "test"));

        await middleware.InvokeAsync(blocked);

        Assert.Equal(StatusCodes.Status403Forbidden, blocked.Response.StatusCode);
        Assert.False(downstreamCalled);
        blocked.Response.Body.Position = 0;
        Assert.Contains("MFA_ENROLLMENT_REQUIRED", await new StreamReader(blocked.Response.Body, Encoding.UTF8).ReadToEndAsync());

        var allowed = new DefaultHttpContext();
        allowed.Request.Path = "/api/v1/auth/mfa/status";
        allowed.User = blocked.User;
        await middleware.InvokeAsync(allowed);
        Assert.True(downstreamCalled);
    }

    [Fact]
    public async Task Required_password_change_is_claimed_and_blocks_direct_api_calls()
    {
        await using var context = NewContext();
        var user = User("password-user"); user.MustChangePassword = true;
        context.Users.Add(user); await context.SaveChangesAsync();
        var service = CreateService(context, user);
        var principal = await service.GetPrincipalFromExpiredTokenAsync((await service.GenerateTokensAsync(user)).AccessToken);
        Assert.Equal("true", principal!.FindFirstValue(PasswordChangePolicy.ChangeRequiredClaim));

        var downstreamCalled = false;
        var middleware = new PasswordChangeMiddleware(_ => { downstreamCalled = true; return Task.CompletedTask; });
        var blocked = new DefaultHttpContext { User = principal! };
        blocked.Request.Path = "/api/v1/performance-targets";
        blocked.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(blocked);
        Assert.Equal(StatusCodes.Status403Forbidden, blocked.Response.StatusCode);
        Assert.False(downstreamCalled);

        var allowed = new DefaultHttpContext { User = principal! };
        allowed.Request.Path = "/api/v1/auth/password/change";
        await middleware.InvokeAsync(allowed);
        Assert.True(downstreamCalled);
    }

    private static ApplicationDbContext NewContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@example.test", Email = $"{id}@example.test", FirstName = "Test", LastName = "User", IsActive = true, SecurityStamp = $"stamp-{id}" };
    private static JwtService CreateService(ApplicationDbContext context, ApplicationUser user, int maximumSessions = 5, string[]? permissions = null, IAuthenticationPolicyResolver? policyResolver = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var users = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync([]);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetEffectiveAccessAsync(user)).ReturnsAsync(new EffectiveAccessResult([], permissions ?? [], [], [], [], []));
        var settings = Options.Create(new JwtSettings { Secret = "A-development-test-secret-at-least-32-characters-long", Issuer = "tests", Audience = "tests", ExpiryMinutes = 15, RefreshTokenExpiryDays = 7, SessionIdleTimeoutMinutes = 30, SessionAbsoluteTimeoutHours = 24, MaxConcurrentSessions = maximumSessions, MfaRequiredPermissionCodes = ["SECURITY.MANAGE_ROLES"] });
        return new JwtService(settings, users.Object, context, access.Object, policyResolver);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
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

    private static ApplicationDbContext NewContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@example.test", Email = $"{id}@example.test", FirstName = "Test", LastName = "User", IsActive = true, SecurityStamp = $"stamp-{id}" };
    private static JwtService CreateService(ApplicationDbContext context, ApplicationUser user, int maximumSessions = 5)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var users = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync([]);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetEffectiveAccessAsync(user)).ReturnsAsync(new EffectiveAccessResult([], [], [], [], [], []));
        var settings = Options.Create(new JwtSettings { Secret = "A-development-test-secret-at-least-32-characters-long", Issuer = "tests", Audience = "tests", ExpiryMinutes = 15, RefreshTokenExpiryDays = 7, SessionIdleTimeoutMinutes = 30, SessionAbsoluteTimeoutHours = 24, MaxConcurrentSessions = maximumSessions });
        return new JwtService(settings, users.Object, context, access.Object);
    }
}

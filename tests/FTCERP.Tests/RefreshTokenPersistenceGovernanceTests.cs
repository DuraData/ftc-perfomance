namespace FTCERP.Tests;

public sealed class RefreshTokenPersistenceGovernanceTests
{
    [Fact]
    public async Task Invalid_session_creation_is_rejected_before_sql_execution()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = await SeedUserAsync(context);
        var token = ValidToken(user);
        token.Token = "raw-bearer-value";
        context.RefreshTokens.Add(token);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Refresh-session creation requires a valid immutable identity*");
    }

    [Fact]
    public async Task Session_identity_and_security_context_are_immutable()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var token = await SeedTokenAsync(context);
        token.AuthenticationMethod = "ENTRA";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Refresh-session identity*immutable*");
    }

    [Fact]
    public async Task Session_history_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var token = await SeedTokenAsync(context);
        context.RefreshTokens.Remove(token);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Refresh-session history cannot be hard deleted*");
    }

    [Fact]
    public async Task Activity_touch_must_advance_and_preserves_session_identity()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var token = await SeedTokenAsync(context);
        var originalPublicId = token.PublicId;
        var originalSessionId = token.SessionId;
        var originalDigest = token.Token;
        var originalRowVersion = token.RowVersion.ToArray();
        token.LastUsedAt = token.LastUsedAt.AddMinutes(1);
        token.LastUsedByIp = "10.0.0.2";

        await context.SaveChangesAsync();

        token.PublicId.Should().Be(originalPublicId);
        token.SessionId.Should().Be(originalSessionId);
        token.Token.Should().Be(originalDigest);
        token.RowVersion.Should().NotEqual(originalRowVersion);

        token.LastUsedAt = token.LastUsedAt.AddMinutes(-2);
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Refresh-session activity may only advance monotonically*");
    }

    [Fact]
    public async Task Revocation_is_complete_and_irreversible()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var token = await SeedTokenAsync(context);
        var revokedAt = token.CreatedAt.AddMinutes(5);
        token.RevokedAt = revokedAt;
        token.RevokedByIp = "10.0.0.3";
        token.RevokedReason = "User signed out this device";

        await context.SaveChangesAsync();

        token.RevokedAt.Should().Be(revokedAt);
        token.RevokedReason.Should().Be("User signed out this device");

        token.RevokedAt = revokedAt.AddMinutes(1);
        token.RevokedReason = "Rewritten reason";
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Refresh-session revocation is an irreversible*");
    }

    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser($"session-governor-{Guid.NewGuid():N}");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<RefreshToken> SeedTokenAsync(ApplicationDbContext context)
    {
        var user = await SeedUserAsync(context);
        var token = ValidToken(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        return token;
    }

    private static RefreshToken ValidToken(ApplicationUser user)
    {
        var createdAt = new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc);
        return new RefreshToken
        {
            UserId = user.Id,
            Token = new string('A', 64),
            SessionId = Guid.NewGuid(),
            CreatedAt = createdAt,
            LastUsedAt = createdAt,
            ExpiresAt = createdAt.AddHours(2),
            AbsoluteExpiresAt = createdAt.AddHours(8),
            CreatedByIp = "10.0.0.1",
            LastUsedByIp = "10.0.0.1",
            UserAgent = "Governed test browser",
            SecurityStamp = user.SecurityStamp!,
            AuthenticationMethod = "LOCAL"
        };
    }
}

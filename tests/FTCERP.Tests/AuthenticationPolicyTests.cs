using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace FTCERP.Tests;

public sealed class AuthenticationPolicyTests
{
    [Fact]
    public async Task Resolver_returns_current_municipality_policy_and_safe_defaults()
    {
        await using var context = NewContext();
        var municipality = new Municipality { Code = "POL", Name = "Policy Municipality" };
        var user = User("policy-admin", municipality);
        var configuration = new AuthenticationConfiguration
        {
            Municipality = municipality, Mode = AuthenticationMode.Local, DisplayName = "Local",
            CreatedByUser = user, EffectiveFrom = DateTime.UtcNow.AddMinutes(-5)
        };
        configuration.Policy = new AuthenticationPolicy
        {
            Municipality = municipality, AuthenticationConfiguration = configuration, ModifiedByUser = user,
            MinimumPasswordLength = 18, MaximumFailedAttempts = 3, LockoutMinutes = 45,
            RequireMfaForAllLocalUsers = true, RequireMfaForPrivilegedLocalUsers = false,
            RequireFirstLoginPasswordChange = false, SessionIdleTimeoutMinutes = 12,
            SessionAbsoluteTimeoutHours = 8, MaximumConcurrentSessions = 2
        };
        context.AddRange(municipality, user, configuration);
        await context.SaveChangesAsync();
        var resolver = Resolver(context, requiredLength: 14);

        var stored = await resolver.ResolveAsync(municipality.Id);
        var fallback = await resolver.ResolveAsync(99_999);

        stored.MinimumPasswordLength.Should().Be(18);
        stored.MaximumFailedAttempts.Should().Be(3);
        stored.RequireMfaForAllLocalUsers.Should().BeTrue();
        stored.SessionIdleTimeoutMinutes.Should().Be(12);
        stored.MaximumConcurrentSessions.Should().Be(2);
        fallback.MinimumPasswordLength.Should().Be(14);
        fallback.MaximumFailedAttempts.Should().Be(5);
        fallback.SessionAbsoluteTimeoutHours.Should().Be(24);
    }

    [Fact]
    public async Task Password_validator_uses_resolved_municipality_minimum()
    {
        var policies = new Mock<IAuthenticationPolicyResolver>();
        policies.Setup(service => service.ResolveAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EffectiveAuthenticationPolicy(16, 5, 15, true, false, true, 30, 24, 5));
        var validator = new MunicipalityPasswordPolicyValidator(policies.Object);
        var user = new ApplicationUser { Id = "user", MunicipalityId = 1 };

        var rejected = await validator.ValidateAsync(null!, user, "FifteenChars123");
        var accepted = await validator.ValidateAsync(null!, user, "SixteenChars123!");

        rejected.Succeeded.Should().BeFalse();
        rejected.Errors.Should().ContainSingle(error => error.Code == "MunicipalityPasswordTooShort");
        accepted.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Mfa_policy_applies_only_to_local_authentication()
    {
        var user = new ApplicationUser { TwoFactorEnabled = false };
        var policy = new EffectiveAuthenticationPolicy(12, 5, 15, true, true, true, 30, 24, 5);

        AuthenticationPolicyEnforcement.RequiresLocalMfaEnrollment(user, [], policy, [], "LOCAL").Should().BeTrue();
        AuthenticationPolicyEnforcement.RequiresLocalMfaEnrollment(user, ["SECURITY.MANAGE_ROLES"], policy, ["SECURITY.MANAGE_ROLES"], "ENTRA").Should().BeFalse();
        user.TwoFactorEnabled = true;
        AuthenticationPolicyEnforcement.RequiresLocalMfaEnrollment(user, [], policy, [], "LOCAL").Should().BeFalse();
    }

    private static AuthenticationPolicyResolver Resolver(ApplicationDbContext context, int requiredLength)
    {
        var identity = new IdentityOptions();
        identity.Password.RequiredLength = requiredLength;
        return new AuthenticationPolicyResolver(context, Options.Create(identity), Options.Create(new JwtSettings
        {
            SessionIdleTimeoutMinutes = 30, SessionAbsoluteTimeoutHours = 24, MaxConcurrentSessions = 5
        }));
    }

    private static ApplicationDbContext NewContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ApplicationUser User(string id, Municipality municipality) => new()
    {
        Id = id, UserName = $"{id}@example.test", Email = $"{id}@example.test", FirstName = "Policy", LastName = "Admin",
        IsActive = true, SecurityStamp = $"stamp-{id}", Municipality = municipality
    };
}

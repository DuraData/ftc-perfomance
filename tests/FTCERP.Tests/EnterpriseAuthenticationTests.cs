using System.Security.Claims;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class EnterpriseAuthenticationTests
{
    [Fact]
    public void Provider_registry_rejects_non_https_authority_and_missing_deployment_secret()
    {
        var insecure = () => new EnterpriseProviderRegistry([Provider(authority: "http://identity.example.test")]);
        var missingSecret = () => new EnterpriseProviderRegistry([Provider(clientSecret: "")]);

        insecure.Should().Throw<InvalidOperationException>().WithMessage("*HTTPS*");
        missingSecret.Should().Throw<InvalidOperationException>().WithMessage("*ClientSecret*");
    }

    [Fact]
    public async Task Preprovisioned_identity_is_bound_once_and_authentication_is_audited()
    {
        await using var context = NewContext();
        var (municipality, user) = await SeedConfigurationAsync(context);
        context.UserAuthenticators.Add(new UserAuthenticator
        {
            MunicipalityId = municipality.Id, UserId = user.Id, ProviderRegistrationCode = "ENTRA",
            ExpectedEmail = user.Email!, CreatedByUserId = user.Id
        });
        await context.SaveChangesAsync();
        var service = new EnterpriseAuthenticationService(context, new EnterpriseProviderRegistry([Provider()]));
        var http = new DefaultHttpContext { TraceIdentifier = "enterprise-test" };
        var principal = Principal("https://login.microsoftonline.com/tenant/v2.0", "subject-1", user.Email!);

        var result = await service.ResolveAsync(municipality.Id, "ENTRA", principal, http, default);

        result.Succeeded.Should().BeTrue();
        result.User!.Id.Should().Be(user.Id);
        var authenticator = await context.UserAuthenticators.SingleAsync();
        authenticator.Issuer.Should().Be("https://login.microsoftonline.com/tenant/v2.0");
        authenticator.Subject.Should().Be("subject-1");
        authenticator.ExternalIdentityHash.Should().HaveLength(64);
        authenticator.LinkedAt.Should().NotBeNull();
        (await context.AuthenticationEvents.SingleAsync()).Success.Should().BeTrue();
    }

    [Fact]
    public async Task Unknown_external_identity_is_not_auto_created_or_email_linked()
    {
        await using var context = NewContext();
        var (municipality, user) = await SeedConfigurationAsync(context);
        var service = new EnterpriseAuthenticationService(context, new EnterpriseProviderRegistry([Provider()]));

        var result = await service.ResolveAsync(municipality.Id, "ENTRA",
            Principal("https://login.microsoftonline.com/tenant/v2.0", "unknown", user.Email!), new DefaultHttpContext(), default);

        result.Succeeded.Should().BeFalse();
        result.FailureCode.Should().Be("IDENTITY_NOT_PREPROVISIONED");
        (await context.Users.CountAsync()).Should().Be(1);
        (await context.UserAuthenticators.CountAsync()).Should().Be(0);
        (await context.AuthenticationEvents.SingleAsync()).FailureCode.Should().Be("IDENTITY_NOT_PREPROVISIONED");
    }

    [Fact]
    public async Task Sqlite_enforces_one_external_identity_and_authentication_events_are_append_only()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Code = "M1", Name = "Municipality" };
        var first = User("first", municipality); var second = User("second", municipality);
        context.AddRange(municipality, first, second); await context.SaveChangesAsync();
        var hash = new string('A', 64);
        context.UserAuthenticators.Add(new UserAuthenticator { MunicipalityId = municipality.Id, UserId = first.Id, ProviderRegistrationCode = "ENTRA", ExpectedEmail = first.Email!, ExternalIdentityHash = hash, Issuer = "https://issuer", Subject = "one", CreatedByUserId = first.Id });
        await context.SaveChangesAsync();
        context.UserAuthenticators.Add(new UserAuthenticator { MunicipalityId = municipality.Id, UserId = second.Id, ProviderRegistrationCode = "ENTRA", ExpectedEmail = second.Email!, ExternalIdentityHash = hash, Issuer = "https://issuer", Subject = "one", CreatedByUserId = first.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        var authEvent = new AuthenticationEvent { MunicipalityId = municipality.Id, UserId = first.Id, ProviderCode = "ENTRA", EventType = "ExternalSignIn", Success = true, CorrelationId = "test" };
        context.AuthenticationEvents.Add(authEvent); await context.SaveChangesAsync();
        authEvent.Success = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    private static ApplicationDbContext NewContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Municipality Municipality, ApplicationUser User)> SeedConfigurationAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "M1", Name = "Municipality", AuthenticationMode = AuthenticationMode.MicrosoftEntraId };
        var user = User("user", municipality);
        context.AddRange(municipality, user); await context.SaveChangesAsync();
        context.AuthenticationConfigurations.Add(new AuthenticationConfiguration
        {
            MunicipalityId = municipality.Id, Mode = AuthenticationMode.MicrosoftEntraId, ProviderRegistrationCode = "ENTRA",
            DisplayName = "Work account", CreatedByUserId = user.Id, EffectiveFrom = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();
        return (municipality, user);
    }

    private static ApplicationUser User(string id, Municipality municipality) => new()
    {
        Id = id, UserName = $"{id}@example.test", NormalizedUserName = $"{id}@example.test".ToUpperInvariant(),
        Email = $"{id}@example.test", NormalizedEmail = $"{id}@example.test".ToUpperInvariant(), EmailConfirmed = true,
        FirstName = "Test", LastName = "User", IsActive = true, SecurityStamp = $"stamp-{id}", Municipality = municipality
    };

    private static ClaimsPrincipal Principal(string issuer, string subject, string email) => new(new ClaimsIdentity([
        new Claim("iss", issuer), new Claim(ClaimTypes.NameIdentifier, subject), new Claim(ClaimTypes.Email, email)
    ], "oidc"));

    private static EnterpriseProviderRegistration Provider(string authority = "https://login.microsoftonline.com/tenant/v2.0", string clientSecret = "deployment-secret") => new()
    {
        Code = "ENTRA", Kind = EnterpriseProviderKinds.MicrosoftEntraId, DisplayName = "Work account",
        Authority = authority, ClientId = "client-id", ClientSecret = clientSecret, CallbackPath = "/signin-oidc-entra"
    };
}

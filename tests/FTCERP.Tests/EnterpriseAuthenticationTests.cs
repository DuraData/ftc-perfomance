using System.Security.Claims;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

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

    [Fact]
    public async Task Authentication_administration_pages_filter_tenant_and_status_before_count_and_page()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var tenantA = new Municipality { Code = "AUTH-A", Name = "Authentication A" };
        var tenantB = new Municipality { Code = "AUTH-B", Name = "Authentication B" };
        context.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();
        context.Users.Add(User("authentication-admin", tenantA));

        for (var index = 0; index < 31; index++)
        {
            var user = User($"person-{index:00}", tenantA);
            context.Users.Add(user);
            context.UserAuthenticators.Add(new UserAuthenticator
            {
                MunicipalityId = tenantA.Id,
                User = user,
                ProviderRegistrationCode = index % 2 == 0 ? "ENTRA" : "ADFS",
                ExpectedEmail = user.Email!,
                IsActive = index % 2 == 0,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-index)
            });
            context.AuthenticationEvents.Add(new AuthenticationEvent
            {
                MunicipalityId = tenantA.Id,
                UserId = user.Id,
                ProviderCode = "ENTRA",
                EventType = "ExternalSignIn",
                Success = index % 2 == 0,
                FailureCode = index % 2 == 0 ? null : "DENIED",
                OccurredAt = DateTime.UtcNow.AddMinutes(-index),
                CorrelationId = $"trace-{index:00}"
            });
        }
        var outside = User("outside", tenantB);
        context.Users.Add(outside);
        context.UserAuthenticators.Add(new UserAuthenticator { MunicipalityId = tenantB.Id, User = outside, ProviderRegistrationCode = "ENTRA", ExpectedEmail = outside.Email!, IsActive = true, CreatedByUserId = outside.Id });
        context.AuthenticationEvents.Add(new AuthenticationEvent { MunicipalityId = tenantB.Id, UserId = outside.Id, ProviderCode = "ENTRA", EventType = "ExternalSignIn", Success = false, FailureCode = "DENIED", CorrelationId = "outside" });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "allowed", [], [], []));
        var controller = new AuthenticationAdministrationController(context, new TenantContext(tenantA.Id), new EnterpriseProviderRegistry([Provider()]), access.Object);
        var authenticators = await controller.GetAuthenticatorsPage(new PagedQueryRequest { Page = 2, PageSize = 10, Search = "person", SortBy = "email", SortDirection = "asc" }, true, null);
        var authenticatorPage = Assert.IsType<ApiResponse<PagedResponse<UserAuthenticatorDto>>>(Assert.IsType<OkObjectResult>(authenticators.Result).Value).Data!;
        Assert.Equal(16, authenticatorPage.TotalCount);
        Assert.Equal(6, authenticatorPage.Items.Length);
        Assert.All(authenticatorPage.Items, item => Assert.StartsWith("person-", item.UserEmail));

        var events = await controller.GetEventsPage(new PagedQueryRequest { Page = 2, PageSize = 10, Search = "DENIED", SortBy = "occurredAt", SortDirection = "desc" }, false, null, null);
        var eventPage = Assert.IsType<ApiResponse<PagedResponse<AuthenticationEventDto>>>(Assert.IsType<OkObjectResult>(events.Result).Value).Data!;
        Assert.Equal(15, eventPage.TotalCount);
        Assert.Equal(5, eventPage.Items.Length);
        Assert.All(eventPage.Items, item => Assert.Equal("DENIED", item.FailureCode));

        var invalid = await controller.GetEventsPage(new PagedQueryRequest { SortBy = "unsafe" });
        Assert.IsType<BadRequestObjectResult>(invalid.Result);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetAuthenticators().Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetEvents().Result).StatusCode);
    }

    [Fact]
    public async Task Authentication_administration_masks_denied_members_and_prevents_search_sort_and_write_inference()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Code = "AUTH-PROTECTED", Name = "Protected Authentication" };
        var actor = User("authentication-admin", municipality);
        var linked = User("linked-user", municipality);
        context.AddRange(municipality, actor, linked);
        context.UserAuthenticators.Add(new UserAuthenticator
        {
            Municipality = municipality, User = linked, ProviderRegistrationCode = "ENTRA",
            ExpectedEmail = "identity-secret@example.test", Issuer = "https://issuer.example.test",
            Subject = "external-subject-secret", CreatedByUserId = actor.Id
        });
        context.AuthenticationEvents.Add(new AuthenticationEvent
        {
            Municipality = municipality, User = linked, ProviderCode = "ENTRA", EventType = "ExternalSignIn",
            Success = false, FailureCode = "DENIED", IpAddress = "203.0.113.91", CorrelationId = "protected-event"
        });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "denied", [], [], []));
        var controller = new AuthenticationAdministrationController(context, new TenantContext(municipality.Id), new EnterpriseProviderRegistry([Provider()]), access.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = Http(actor.Id) }
        };

        var pageResult = await controller.GetAuthenticatorsPage(new PagedQueryRequest { SortBy = "createdAt" });
        var page = Assert.IsType<ApiResponse<PagedResponse<UserAuthenticatorDto>>>(Assert.IsType<OkObjectResult>(pageResult.Result).Value).Data!;
        var protectedIdentity = Assert.Single(page.Items);
        Assert.Null(protectedIdentity.UserEmail);
        Assert.Null(protectedIdentity.ExpectedEmail);
        Assert.Null(protectedIdentity.Issuer);
        Assert.Null(protectedIdentity.Subject);

        var emailSort = await controller.GetAuthenticatorsPage(new PagedQueryRequest { SortBy = "email" });
        Assert.IsType<ForbidResult>(emailSort.Result);
        var hiddenSearch = await controller.GetAuthenticatorsPage(new PagedQueryRequest { SortBy = "createdAt", Search = "identity-secret" });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<UserAuthenticatorDto>>>(Assert.IsType<OkObjectResult>(hiddenSearch.Result).Value).Data!.TotalCount);

        var eventResult = await controller.GetEventsPage(new PagedQueryRequest { Search = "203.0.113.91" });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<AuthenticationEventDto>>>(Assert.IsType<OkObjectResult>(eventResult.Result).Value).Data!.TotalCount);
        var eventPageResult = await controller.GetEventsPage(new PagedQueryRequest());
        var protectedEvent = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<AuthenticationEventDto>>>(Assert.IsType<OkObjectResult>(eventPageResult.Result).Value).Data!.Items);
        Assert.Null(protectedEvent.UserId);
        Assert.Null(protectedEvent.IpAddress);

        var write = await controller.Provision(new ProvisionUserAuthenticatorRequest(linked.Id, "ENTRA", linked.Email!, null, null, "Governed link"), default);
        Assert.IsType<ForbidResult>(write.Result);
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

    private static DefaultHttpContext Http(string actorId)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actorId)], "test"));
        return http;
    }

    private static EnterpriseProviderRegistration Provider(string authority = "https://login.microsoftonline.com/tenant/v2.0", string clientSecret = "deployment-secret") => new()
    {
        Code = "ENTRA", Kind = EnterpriseProviderKinds.MicrosoftEntraId, DisplayName = "Work account",
        Authority = authority, ClientId = "client-id", ClientSecret = clientSecret, CallbackPath = "/signin-oidc-entra"
    };

    private sealed class TenantContext(long municipalityId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => "authentication-admin";
    }
}

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
using Microsoft.Extensions.Options;
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
    public async Task Sign_in_options_resolve_account_then_unambiguous_email_domain_without_municipality_input()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "M1", Name = "Municipality One", AuthenticationMode = AuthenticationMode.Hybrid };
        var account = User("known", municipality);
        account.Email = "known@civic.example";
        account.NormalizedEmail = "KNOWN@CIVIC.EXAMPLE";
        account.UserName = account.Email;
        account.NormalizedUserName = account.NormalizedEmail;
        context.AddRange(municipality, account);
        await context.SaveChangesAsync();
        context.AuthenticationConfigurations.Add(new AuthenticationConfiguration
        {
            MunicipalityId = municipality.Id,
            Mode = AuthenticationMode.Hybrid,
            ProviderRegistrationCode = "ENTRA",
            DisplayName = "Work account",
            CreatedByUserId = account.Id,
            EffectiveFrom = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();
        var controller = DiscoveryController(context);

        var known = Extract(await controller.DiscoverOptions(new EnterpriseSignInDiscoveryRequest(account.Email), default));
        known.MunicipalityCode.Should().Be("M1");
        known.LocalEnabled.Should().BeTrue();
        known.Providers.Should().ContainSingle(item => item.Code == "ENTRA" && item.DisplayName == "Work account");

        var domain = Extract(await controller.DiscoverOptions(new EnterpriseSignInDiscoveryRequest("new.person@civic.example"), default));
        domain.MunicipalityCode.Should().Be("M1");

        var secondMunicipality = new Municipality { Code = "M2", Name = "Municipality Two", AuthenticationMode = AuthenticationMode.Local };
        var secondAccount = User("second", secondMunicipality);
        secondAccount.Email = "second@civic.example";
        secondAccount.NormalizedEmail = "SECOND@CIVIC.EXAMPLE";
        context.AddRange(secondMunicipality, secondAccount);
        await context.SaveChangesAsync();

        var ambiguous = Extract(await controller.DiscoverOptions(new EnterpriseSignInDiscoveryRequest("unknown@civic.example"), default));
        ambiguous.MunicipalityCode.Should().BeEmpty();
        ambiguous.Providers.Should().BeEmpty();
        ambiguous.LocalEnabled.Should().BeTrue();
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
    public async Task Authentication_administration_creates_append_preserved_policy_versions_and_bounded_history()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Code = "AUTH-VERSIONS", Name = "Versioned Authentication" };
        var actor = User("authentication-version-admin", municipality);
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        var controller = new AuthenticationAdministrationController(context, new TenantContext(municipality.Id),
            new EnterpriseProviderRegistry([Provider()]), access.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = Http(actor.Id) }
        };
        var firstEffective = DateTime.UtcNow.AddDays(-2);
        var firstResult = await controller.Save(new SaveAuthenticationConfigurationRequest(
            AuthenticationMode.Local, null, "Municipal sign-in", true, firstEffective, null,
            new AuthenticationPolicyRequest(12, 5, 15, true, false, true, 30, 24, 5, null),
            "Initial approved authentication policy", null), default);
        var first = Assert.IsType<ApiResponse<AuthenticationConfigurationDto>>(Assert.IsType<OkObjectResult>(firstResult.Result).Value).Data!;
        Assert.Equal(1, first.VersionNumber);
        Assert.True(first.IsCurrent);

        var secondEffective = DateTime.UtcNow.AddDays(-1);
        var secondResult = await controller.Save(new SaveAuthenticationConfigurationRequest(
            AuthenticationMode.Local, null, "Stronger municipal sign-in", true, secondEffective, null,
            new AuthenticationPolicyRequest(16, 4, 30, true, true, true, 20, 12, 3, first.Policy!.RowVersion),
            "Security committee approved stronger controls", first.RowVersion), default);
        var second = Assert.IsType<ApiResponse<AuthenticationConfigurationDto>>(Assert.IsType<OkObjectResult>(secondResult.Result).Value).Data!;
        Assert.Equal(2, second.VersionNumber);
        Assert.Equal(first.ConfigurationFamilyPublicId, second.ConfigurationFamilyPublicId);
        Assert.True(second.IsCurrent);

        var versions = await context.AuthenticationConfigurations.IgnoreQueryFilters().AsNoTracking().Include(item => item.Policy)
            .OrderBy(item => item.VersionNumber).ToArrayAsync();
        Assert.Equal(2, versions.Length);
        Assert.False(versions[0].IsCurrent);
        Assert.Equal(secondEffective.AddTicks(-1), versions[0].EffectiveTo);
        Assert.Equal(12, versions[0].Policy!.MinimumPasswordLength);
        Assert.Equal(16, versions[1].Policy!.MinimumPasswordLength);
        Assert.Equal(versions[0].Id, versions[1].PreviousVersionId);

        var historyResult = await controller.GetHistoryPage(new PagedQueryRequest { Page = 1, PageSize = 1, SortBy = "version", SortDirection = "desc" });
        var history = Assert.IsType<ApiResponse<PagedResponse<AuthenticationConfigurationDto>>>(Assert.IsType<OkObjectResult>(historyResult.Result).Value).Data!;
        Assert.Equal(2, history.TotalCount);
        Assert.Single(history.Items);
        Assert.Equal(2, history.Items[0].VersionNumber);
        Assert.Equal(2, await context.AuditTrails.CountAsync(item => item.EntityName == "AuthenticationConfiguration"));
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

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "allowed" : "denied", [], [], []));
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
        Assert.Null(protectedEvent.UserPublicId);
        Assert.Null(protectedEvent.IpAddress);

        allowedCodes.Add("AUTHENTICATION.EventUserId.READ");
        var rawKeySearch = await controller.GetEventsPage(new PagedQueryRequest { Search = linked.Id });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<AuthenticationEventDto>>>(
            Assert.IsType<OkObjectResult>(rawKeySearch.Result).Value).Data!.TotalCount);
        var publicIdSearch = await controller.GetEventsPage(new PagedQueryRequest { Search = linked.PublicId.ToString() });
        var publicEvent = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<AuthenticationEventDto>>>(
            Assert.IsType<OkObjectResult>(publicIdSearch.Result).Value).Data!.Items);
        Assert.Equal(linked.PublicId, publicEvent.UserPublicId);
        Assert.Null(typeof(AuthenticationEventDto).GetProperty("UserId"));

        var write = await controller.Provision(new ProvisionUserAuthenticatorRequest(linked.PublicId, "ENTRA", linked.Email!, null, null, "Governed link"), default);
        Assert.IsType<ForbidResult>(write.Result);
    }

    [Fact]
    public async Task Authentication_administration_status_changes_preserve_actor_stamped_audit_history()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Code = "AUTH-STATUS", Name = "Authentication Status" };
        var actor = User("authentication-status-admin", municipality);
        var linked = User("authentication-status-user", municipality);
        var authenticator = new UserAuthenticator
        {
            Municipality = municipality,
            User = linked,
            ProviderRegistrationCode = "ENTRA",
            ExpectedEmail = linked.Email!,
            CreatedByUserId = actor.Id,
            IsActive = true
        };
        context.AddRange(municipality, actor, linked, authenticator);
        await context.SaveChangesAsync();

        var controller = new AuthenticationAdministrationController(context, new TenantContext(municipality.Id),
            new EnterpriseProviderRegistry([Provider()]), Mock.Of<IAccessControlService>())
        {
            ControllerContext = new ControllerContext { HttpContext = Http(actor.Id) }
        };
        var disabledResult = await controller.SetStatus(authenticator.PublicId,
            new SetUserAuthenticatorStatusRequest(false, "Security owner suspended the identity", Convert.ToBase64String(authenticator.RowVersion)), default);
        var disabled = Assert.IsType<ApiResponse<UserAuthenticatorDto>>(Assert.IsType<OkObjectResult>(disabledResult.Result).Value).Data!;
        Assert.False(disabled.IsActive);
        var disabledAudit = await context.AuditTrails.SingleAsync(item => item.Action == "EnterpriseIdentityDisabled");
        Assert.Equal("UserAuthenticator", disabledAudit.EntityName);
        Assert.Equal(actor.Id, disabledAudit.ChangedBy);

        var enabledResult = await controller.SetStatus(authenticator.PublicId,
            new SetUserAuthenticatorStatusRequest(true, "Security owner restored the identity", disabled.RowVersion), default);
        var enabled = Assert.IsType<ApiResponse<UserAuthenticatorDto>>(Assert.IsType<OkObjectResult>(enabledResult.Result).Value).Data!;
        Assert.True(enabled.IsActive);
        Assert.Equal(2, await context.AuditTrails.CountAsync(item => item.EntityName == "UserAuthenticator"));
    }

    private static ApplicationDbContext NewContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static EnterpriseAuthController DiscoveryController(ApplicationDbContext context) => new(
        context,
        new EnterpriseProviderRegistry([Provider()]),
        new Mock<IEnterpriseAuthenticationService>().Object,
        new Mock<IJwtService>().Object,
        Options.Create(new EnterpriseAuthenticationOptions()),
        Options.Create(new JwtSettings()));

    private static EnterpriseSignInOptionsResponse Extract(ActionResult<ApiResponse<EnterpriseSignInOptionsResponse>> result) =>
        Assert.IsType<ApiResponse<EnterpriseSignInOptionsResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;

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

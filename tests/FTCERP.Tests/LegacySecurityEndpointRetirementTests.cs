using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using System.Text.Json;

namespace FTCERP.Tests;

public sealed class LegacySecurityEndpointRetirementTests
{
    [Fact]
    public void Every_action_under_an_unversioned_controller_prefix_is_hidden_from_api_discovery()
    {
        var controllerTypes = typeof(AuthController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttributes(typeof(RouteAttribute), true)
                .Cast<RouteAttribute>()
                .Any(route => (route.Template.Equals("api", StringComparison.OrdinalIgnoreCase)
                        || route.Template.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                    && !route.Template.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var controllerType in controllerTypes)
        {
            var controllerHidden = controllerType.GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Any(setting => setting.IgnoreApi);
            foreach (var method in controllerType.GetMethods().Where(method => method.DeclaringType == controllerType))
            {
                foreach (var route in method.GetCustomAttributes(true).OfType<HttpMethodAttribute>())
                {
                    var template = route.Template ?? string.Empty;
                    if (template.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase)
                        || template.StartsWith("~/api/v1/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var actionHidden = method.GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                        .Cast<ApiExplorerSettingsAttribute>().Any(setting => setting.IgnoreApi);
                    Assert.True(controllerHidden || actionHidden,
                        $"Unversioned action must be hidden from API discovery: {controllerType.Name}.{method.Name} ({template})");
                }
            }
        }
    }

    [Fact]
    public async Task Legacy_role_collection_detail_and_mutations_are_gone()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(301);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var roleA = Role("role-a", 301, "ROLE_A", "Tenant A Role");
        var roleB = Role("role-b", 302, "ROLE_B", "Tenant B Role");
        context.AddRange(
            new Municipality { Id = 301, Code = "M301", Name = "Municipality 301" },
            new Municipality { Id = 302, Code = "M302", Name = "Municipality 302" },
            roleA,
            roleB);
        await context.SaveChangesAsync();
        var controller = new RolesController();

        AssertGone(controller.GetRoles().Result);
        AssertGone(controller.GetRole(roleA.PublicId).Result);
        AssertGone(controller.GetRole(roleB.PublicId).Result);
        AssertGone(controller.GetRolePermissions("role-a").Result);
        AssertGone(controller.GetRolePermissions("role-b").Result);

        AssertGone((await controller.CreateRole()).Result);
        AssertGone((await controller.UpdateRole("role-a")).Result);
        AssertGone((await controller.DeleteRole("role-a")).Result);
        AssertGone((await controller.SetRolePermissions("role-a")).Result);
        Assert.Equal(2, await context.Roles.CountAsync());
        Assert.Equal("Tenant A Role", (await context.Roles.SingleAsync(item => item.Id == "role-a")).Name);
    }

    [Fact]
    public void Unversioned_user_administration_prefix_is_an_explicit_gone_tombstone()
    {
        var controller = new LegacyUsersController();

        AssertGone(controller.Retired().Result);
        AssertGone(controller.Retired("user-id/scopes/page").Result);
    }

    [Fact]
    public void Unversioned_authentication_prefix_is_an_explicit_gone_tombstone()
    {
        var controller = new LegacyAuthController();

        AssertGone(controller.Retired().Result);
        AssertGone(controller.Retired("login").Result);
        AssertGone(controller.Retired("me/profile").Result);
    }

    [Fact]
    public void Unversioned_idp_prefix_has_a_hidden_catch_all_tombstone_and_live_mutations_are_v1_only()
    {
        var controller = new LegacyIdpApiController();
        AssertGone(controller.Retired().Result);
        AssertGone(controller.Retired("comments").Result);
        AssertGone(controller.Retired("outcomes").Result);

        foreach (var action in new[]
                 {
                     nameof(IdpController.CreateOutcome), nameof(IdpController.CreateObjective),
                     nameof(IdpController.CreatePriority), nameof(IdpController.CreateProgramme),
                     nameof(IdpController.CreateProject), nameof(IdpController.CreateKpi),
                     nameof(IdpController.CreateAnnualTarget), nameof(IdpController.CreateAlignmentLink),
                     nameof(IdpController.CreateRiskLink), nameof(IdpController.CreateBudgetSnapshot),
                     nameof(IdpController.CreateComment), nameof(IdpController.CreateTask)
                 })
        {
            var route = typeof(IdpController).GetMethod(action)!
                .GetCustomAttributes(typeof(HttpPostAttribute), true)
                .Cast<HttpPostAttribute>().Single().Template;
            Assert.StartsWith("~/api/v1/idp/", route, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Legacy_permission_definition_mutations_are_gone_and_cannot_rewrite_registry_codes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var permission = new Permission { Module = "Resource", Feature = "USER", Action = "Read", Code = "USER.READ", Description = "Read users" };
        var secondPermission = new Permission { Module = "Resource", Feature = "ROLE", Action = "Read", Code = "ROLE.READ", Description = "Read roles" };
        context.Permissions.AddRange(permission, secondPermission);
        await context.SaveChangesAsync();
        var controller = new PermissionsController();

        AssertGone(controller.GetPermissions().Result);
        AssertGone(controller.GetGrouped().Result);
        AssertGone(controller.GetPermissionsPage().Result);
        AssertGone(controller.CreatePermission().Result);
        AssertGone(controller.UpdatePermission(permission.Id).Result);
        AssertGone(controller.DeletePermission(permission.Id).Result);

        var stored = await context.Permissions.SingleAsync(item => item.Id == permission.Id);
        Assert.Equal("USER.READ", stored.Code);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public void Retired_private_key_security_contracts_are_absent_and_hidden_from_api_discovery()
    {
        var assembly = typeof(PermissionsController).Assembly;
        var removedContracts = new[]
        {
            "FTCERP.Host.API.Responses.PermissionResponse",
            "FTCERP.Host.API.Responses.RolePermissionResponse",
            "FTCERP.Host.API.Responses.UserPermissionOverrideResponse",
            "FTCERP.Host.API.Responses.UserPermissionsResponse",
            "FTCERP.Host.API.Responses.PermissionGroupResponse",
            "FTCERP.Host.API.Responses.DepartmentResponse",
            "FTCERP.Host.API.Responses.UnitResponse",
            "FTCERP.Host.API.Responses.IdpStakeholderEngagementResponse",
            "FTCERP.Host.API.Requests.UpdateRolePermissionsRequest",
            "FTCERP.Host.API.Requests.UpdateUserPermissionOverridesRequest",
            "FTCERP.Host.API.Requests.UpdateUserPermissionOverrideItem",
            "FTCERP.Host.API.Requests.CreateRoleRequest",
            "FTCERP.Host.API.Requests.UpdateRoleRequest",
            "FTCERP.Host.API.Requests.CreatePermissionRequest",
            "FTCERP.Host.API.Requests.UpdatePermissionRequest",
            "FTCERP.Host.API.Requests.CreateDepartmentRequest",
            "FTCERP.Host.API.Requests.UpdateDepartmentRequest",
            "FTCERP.Host.API.Requests.CreateUnitRequest",
            "FTCERP.Host.API.Requests.UpdateUnitRequest",
            "FTCERP.Host.API.Requests.CreateIdpStakeholderEngagementRequest",
            "FTCERP.Host.API.Requests.RegisterRequest"
        };

        foreach (var contract in removedContracts)
        {
            Assert.Null(assembly.GetType(contract));
        }

        Assert.True(typeof(PermissionsController).GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(DepartmentsController).GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(UnitsController).GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        foreach (var action in new[]
                 {
                     nameof(RolesController.GetRoles), nameof(RolesController.GetRole), nameof(RolesController.CreateRole), nameof(RolesController.UpdateRole),
                     nameof(RolesController.DeleteRole), nameof(RolesController.GetRolePermissions), nameof(RolesController.SetRolePermissions)
                 })
        {
            Assert.True(typeof(RolesController).GetMethod(action)!
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        }
        foreach (var action in new[] { nameof(UsersController.GetUserPermissions), nameof(UsersController.SetUserPermissionOverrides) })
        {
            Assert.True(typeof(UsersController).GetMethod(action)!
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        }

        foreach (var action in new[]
                 {
                     nameof(IdpController.UpdatePlan), nameof(IdpController.CreatePlanVersion), nameof(IdpController.GetHierarchy),
                     nameof(IdpController.GetDashboard), nameof(IdpController.GetAlignmentMatrix), nameof(IdpController.GenerateReport),
                     nameof(IdpController.CreateStakeholderEngagement), nameof(IdpController.CompleteTask)
                 })
        {
            Assert.True(typeof(IdpController).GetMethod(action)!
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        }
        Assert.Null(typeof(IdpController).GetMethod(nameof(IdpController.UpdatePlanByPublicId))!
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true).SingleOrDefault());
        Assert.True(typeof(AuthController).GetMethod(nameof(AuthController.Register))!
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(AccessController).GetMethod(nameof(AccessController.GetSystemCoverageAudit))!
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(RoleImplementationAuditController).GetMethod(nameof(RoleImplementationAuditController.GetAudit))!
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        foreach (var action in new[] { nameof(RoleImplementationAuditController.GetLegacyAudit), nameof(RoleImplementationAuditController.GetLegacyAuditPage) })
        {
            Assert.True(typeof(RoleImplementationAuditController).GetMethod(action)!
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        }
        Assert.True(typeof(NavigationController).GetMethod(nameof(NavigationController.GetLegacyMyMenu))!
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(LegacyUsersController)
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(LegacyAuthController)
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(LegacyIdpApiController)
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        Assert.True(typeof(LegacyAccessController)
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        foreach (var action in new[]
                 {
                     nameof(IdpController.GetPlans), nameof(IdpController.GetPlansPage), nameof(IdpController.CreatePlan),
                     nameof(IdpController.CreateDocument), nameof(AuditController.GetLoginLogs),
                     nameof(AuditController.GetSecurityEvents), nameof(AuditController.GetAuditTrails)
                 })
        {
            var controllerType = action.StartsWith("GetLogin", StringComparison.Ordinal)
                || action.StartsWith("GetSecurity", StringComparison.Ordinal)
                || action.StartsWith("GetAudit", StringComparison.Ordinal)
                    ? typeof(AuditController)
                    : typeof(IdpController);
            Assert.True(controllerType.GetMethod(action)!
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        }
        foreach (var action in new[] { nameof(UsersController.GetUsers), nameof(UsersController.GetUserScopes), nameof(UsersController.GetUserAssignments) })
        {
            Assert.True(typeof(UsersController).GetMethod(action)!
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
        }

        foreach (var controllerType in new[] { typeof(OpmsTargetLibraryController), typeof(IpmsTargetLibraryController) })
        {
            foreach (var action in new[] { "GetTemplate", "UpdateTemplate", "ArchiveTemplate", "DuplicateTemplate" })
            {
                var numericAction = controllerType.GetMethods().Single(method => method.Name == action
                    && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(int)));
                Assert.True(numericAction.GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
                    .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
            }
        }
    }

    [Fact]
    public void Retired_registration_cannot_create_a_user_or_assign_a_default_role()
    {
        var controller = new AuthController(null!, null!, null!, null!, null!, Options.Create(new JwtSettings()), null!, null!, null!);

        var result = Assert.IsType<ObjectResult>(controller.Register().Result);

        Assert.Equal(StatusCodes.Status410Gone, result.StatusCode);
        var response = Assert.IsType<ApiResponse<bool>>(result.Value);
        Assert.False(response.Success);
        Assert.Contains("dynamic security administration", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unversioned_performance_prefixes_are_hidden_tombstones_and_current_controllers_are_v1_only()
    {
        var controller = new LegacyPerformanceApiController();
        AssertGone(controller.Retired().Result);

        foreach (var controllerType in new[]
                 {
                     typeof(OpmsTargetsController), typeof(IpmsTargetsController),
                     typeof(OpmsSubmissionsController), typeof(IpmsSubmissionsController),
                     typeof(OpmsTargetLibraryController), typeof(IpmsTargetLibraryController),
                     typeof(NotificationsController)
                 })
        {
            var routes = controllerType.GetCustomAttributes(typeof(RouteAttribute), true)
                .Cast<RouteAttribute>().Select(item => item.Template).ToArray();
            Assert.Single(routes);
            Assert.StartsWith("api/v1/", routes[0], StringComparison.Ordinal);
        }

        Assert.True(typeof(LegacyPerformanceApiController)
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true)
            .Cast<ApiExplorerSettingsAttribute>().Single().IgnoreApi);
    }

    [Fact]
    public void Generated_openapi_document_excludes_retired_private_key_routes_and_keeps_public_id_routes()
    {
        using var factory = new TenantApplicationFactory("swagger-user");
        _ = factory.CreateClient();
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var paths = document.Paths;

        Assert.DoesNotContain(paths.Keys, path => path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase));

        foreach (var retiredPath in new[]
                 {
                     "/api/permissions", "/api/permissions/page", "/api/departments/{id}", "/api/units/{id}",
                     "/api/users", "/api/users/page", "/api/users/{publicId}",
                     "/api/roles/{id}/permissions", "/api/idp/plans/{id}", "/api/idp/plans/{id}/versions",
                     "/api/idp/plans/{id}/hierarchy", "/api/idp/plans/{id}/dashboard",
                     "/api/idp/plans/{id}/alignment-matrix", "/api/idp/plans/{id}/reports/{reportType}",
                     "/api/idp/stakeholder-engagements", "/api/idp/community-sessions", "/api/idp/community-needs",
                     "/api/idp/ward-inputs", "/api/idp/tasks/{id}/complete", "/api/idp/outcomes", "/api/idp/objectives",
                     "/api/idp/priorities", "/api/idp/programmes", "/api/idp/projects", "/api/idp/kpis",
                     "/api/idp/annual-targets", "/api/idp/alignment-links", "/api/idp/risk-links", "/api/idp/budget-snapshots",
                     "/api/idp/comments", "/api/idp/tasks",
                     "/api/access/my-permissions", "/api/access/check", "/api/access/simulate", "/api/access/role-access-matrix/page",
                     "/api/audit/login-logs", "/api/audit/security-events", "/api/audit/trails",
                     "/api/opms-target-library/{id}", "/api/ipms-target-library/{id}",
                     "/api/opms-targets", "/api/ipms-targets", "/api/opms-submissions", "/api/ipms-submissions",
                     "/api/opms-target-library", "/api/ipms-target-library", "/api/notifications",
                     "/api/Auth/register", "/api/auth/login", "/api/auth/refresh-token", "/api/auth/logout", "/api/auth/me", "/api/auth/me/profile",
                     "/api/navigation/my-menu", "/api/v1/access/system-coverage-audit",
                     "/api/role-implementation-audit"
                 })
        {
            Assert.False(paths.ContainsKey(retiredPath), $"Retired private-key path remained in OpenAPI: {retiredPath}");
        }

        foreach (var publicPath in new[]
                 {
                     "/api/v1/security/roles/{rolePublicId}", "/api/v1/navigation/my-menu", "/api/v1/users/page", "/api/v1/users/{publicId}",
                     "/api/v1/auth/login", "/api/v1/auth/refresh-token", "/api/v1/auth/logout", "/api/v1/auth/me", "/api/v1/auth/me/profile",
                     "/api/v1/idp/plans/{planPublicId}",
                     "/api/v1/idp/community-sessions", "/api/v1/idp/community-needs", "/api/v1/idp/ward-inputs",
                     "/api/v1/idp/community-sessions/{sessionPublicId}/stakeholder-engagements",
                     "/api/v1/idp/outcomes", "/api/v1/idp/objectives", "/api/v1/idp/priorities", "/api/v1/idp/programmes",
                     "/api/v1/idp/projects", "/api/v1/idp/kpis", "/api/v1/idp/annual-targets", "/api/v1/idp/alignment-links",
                     "/api/v1/idp/risk-links", "/api/v1/idp/budget-snapshots", "/api/v1/idp/comments", "/api/v1/idp/tasks",
                     "/api/v1/opms-target-library/{publicId}", "/api/v1/ipms-target-library/{publicId}",
                     "/api/v1/opms-targets/{id}", "/api/v1/ipms-targets/{id}",
                     "/api/v1/opms-submissions/{id}", "/api/v1/ipms-submissions/{id}",
                     "/api/v1/notifications/{publicId}/read",
                     "/api/v1/access/system-coverage-audit/page", "/api/v1/security/role-implementation-audit/page"
                 })
        {
            Assert.True(paths.ContainsKey(publicPath), $"Public-ID path missing from OpenAPI: {publicPath}");
        }
    }

    [Fact]
    public async Task Generated_openapi_is_a_complete_versioned_contract_with_exact_anonymous_security_overrides()
    {
        using var factory = new TenantApplicationFactory("openapi-contract-user");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.StartsWith("3.", root.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.True(root.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
        Assert.Contains(root.GetProperty("security").EnumerateArray(), requirement => requirement.TryGetProperty("Bearer", out _));

        var expectedAnonymous = new HashSet<string>(StringComparer.Ordinal)
        {
            "POST /api/v1/auth/login",
            "POST /api/v1/auth/password/forgot",
            "POST /api/v1/auth/password/reset",
            "POST /api/v1/auth/refresh-token",
            "POST /api/v1/auth/enterprise/options",
            "GET /api/v1/auth/enterprise/options/{municipalityCode}",
            "GET /api/v1/auth/enterprise/challenge/{municipalityCode}/{providerCode}",
            "GET /api/v1/auth/enterprise/complete"
        };
        var actualAnonymous = new HashSet<string>(StringComparer.Ordinal);
        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        var operationCount = 0;

        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            Assert.False(path.Name.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                && !path.Name.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase));

            foreach (var method in path.Value.EnumerateObject().Where(item =>
                         item.Name is "get" or "put" or "post" or "delete" or "patch" or "options" or "head" or "trace"))
            {
                operationCount++;
                var operation = method.Value;
                var operationId = operation.GetProperty("operationId").GetString();
                Assert.False(string.IsNullOrWhiteSpace(operationId));
                Assert.True(operationIds.Add(operationId!), $"Duplicate operationId: {operationId}");

                var responses = operation.GetProperty("responses");
                foreach (var status in new[] { "400", "401", "403", "404", "409", "429", "500" })
                    Assert.True(responses.TryGetProperty(status, out _),
                        $"Missing {status} response on {method.Name.ToUpperInvariant()} {path.Name}");

                if (operation.TryGetProperty("security", out var security)
                    && security.ValueKind == JsonValueKind.Array && security.GetArrayLength() == 0)
                    actualAnonymous.Add($"{method.Name.ToUpperInvariant()} {path.Name}");
            }
        }

        Assert.True(operationCount > 400, "The OpenAPI contract must cover the complete production surface.");
        Assert.Equal(expectedAnonymous.Order(), actualAnonymous.Order());
    }

    private static ApplicationRole Role(string id, long municipalityId, string code, string name) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        RoleCode = code,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        IsActive = true,
        EffectiveFrom = DateTime.UtcNow.AddDays(-1),
        ConcurrencyStamp = Guid.NewGuid().ToString()
    };

    private static void AssertGone(ActionResult? result)
    {
        Assert.NotNull(result);
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(410, objectResult.StatusCode);
    }

    private sealed class FixedTenantContext(long? municipalityId, bool isSystem = false) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => "security-admin";
    }
}

using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace FTCERP.Tests;

public sealed class LegacySecurityEndpointRetirementTests
{
    [Fact]
    public async Task Legacy_role_and_permission_collections_and_mutations_are_gone_while_role_detail_remains_tenant_scoped()
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
        var controller = new RolesController(context, tenant);

        AssertGone(controller.GetRoles().Result);
        Assert.IsType<NotFoundObjectResult>((await controller.GetRole(roleB.PublicId)).Result);
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
        Assert.Null(typeof(RolesController).GetMethod(nameof(RolesController.GetRole))!
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), true).SingleOrDefault());
        foreach (var action in new[]
                 {
                     nameof(RolesController.GetRoles), nameof(RolesController.CreateRole), nameof(RolesController.UpdateRole),
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
    public void Generated_openapi_document_excludes_retired_private_key_routes_and_keeps_public_id_routes()
    {
        using var factory = new TenantApplicationFactory("swagger-user");
        _ = factory.CreateClient();
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var paths = document.Paths;

        foreach (var retiredPath in new[]
                 {
                     "/api/permissions", "/api/permissions/page", "/api/departments/{id}", "/api/units/{id}",
                     "/api/roles/{id}/permissions", "/api/idp/plans/{id}", "/api/idp/plans/{id}/versions",
                     "/api/idp/plans/{id}/hierarchy", "/api/idp/plans/{id}/dashboard",
                     "/api/idp/plans/{id}/alignment-matrix", "/api/idp/plans/{id}/reports/{reportType}",
                     "/api/idp/stakeholder-engagements", "/api/idp/tasks/{id}/complete",
                     "/api/opms-target-library/{id}", "/api/ipms-target-library/{id}",
                     "/api/Auth/register", "/api/v1/access/system-coverage-audit",
                     "/api/role-implementation-audit"
                 })
        {
            Assert.False(paths.ContainsKey(retiredPath), $"Retired private-key path remained in OpenAPI: {retiredPath}");
        }

        foreach (var publicPath in new[]
                 {
                     "/api/roles/{publicId}", "/api/v1/idp/plans/{planPublicId}",
                     "/api/v1/idp/community-sessions/{sessionPublicId}/stakeholder-engagements",
                     "/api/v1/opms-target-library/{publicId}", "/api/v1/ipms-target-library/{publicId}",
                     "/api/v1/access/system-coverage-audit/page", "/api/role-implementation-audit/page"
                 })
        {
            Assert.True(paths.ContainsKey(publicPath), $"Public-ID path missing from OpenAPI: {publicPath}");
        }
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

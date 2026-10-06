using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
        context.AddRange(
            new Municipality { Id = 301, Code = "M301", Name = "Municipality 301" },
            new Municipality { Id = 302, Code = "M302", Name = "Municipality 302" },
            Role("role-a", 301, "ROLE_A", "Tenant A Role"),
            Role("role-b", 302, "ROLE_B", "Tenant B Role"));
        await context.SaveChangesAsync();
        var controller = new RolesController(context, tenant);

        AssertGone(controller.GetRoles().Result);
        Assert.IsType<NotFoundObjectResult>((await controller.GetRole("role-b")).Result);
        AssertGone(controller.GetRolePermissions("role-a").Result);
        AssertGone(controller.GetRolePermissions("role-b").Result);

        AssertGone((await controller.CreateRole(new CreateRoleRequest("Bypass", null))).Result);
        AssertGone((await controller.UpdateRole("role-a", new UpdateRoleRequest("Bypass", null))).Result);
        AssertGone((await controller.DeleteRole("role-a")).Result);
        AssertGone((await controller.SetRolePermissions("role-a", new UpdateRolePermissionsRequest([]))).Result);
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
        var controller = new PermissionsController(context);

        AssertGone(controller.GetPermissions().Result);
        AssertGone(controller.GetGrouped().Result);
        var pageResult = await controller.GetPermissionsPage(new PagedQueryRequest { Page = 1, PageSize = 1, Search = "USER", SortBy = "code", SortDirection = "asc" });
        var page = Assert.IsType<ApiResponse<PagedResponse<PermissionResponse>>>(Assert.IsType<OkObjectResult>(pageResult.Result).Value).Data!;
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("USER.READ", Assert.Single(page.Items).Code);
        AssertGone((await controller.CreatePermission(new CreatePermissionRequest("Unsafe", "Unsafe", "Grant", "UNSAFE.GRANT", null, true))).Result);
        AssertGone((await controller.UpdatePermission(permission.Id, new UpdatePermissionRequest("Unsafe", "Unsafe", "Grant", "UNSAFE.GRANT", null, true))).Result);
        AssertGone((await controller.DeletePermission(permission.Id)).Result);

        var stored = await context.Permissions.SingleAsync(item => item.Id == permission.Id);
        Assert.Equal("USER.READ", stored.Code);
        Assert.True(stored.IsActive);
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

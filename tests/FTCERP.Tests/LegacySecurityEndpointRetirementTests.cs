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
    public async Task Legacy_role_reads_are_tenant_scoped_and_all_mutations_are_gone()
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

        var list = await controller.GetRoles();
        var envelope = Assert.IsType<ApiResponse<RoleResponse[]>>(Assert.IsType<OkObjectResult>(list.Result).Value);
        Assert.Equal("role-a", Assert.Single(envelope.Data!).Id);
        Assert.IsType<NotFoundObjectResult>((await controller.GetRole("role-b")).Result);
        Assert.IsType<NotFoundObjectResult>((await controller.GetRolePermissions("role-b")).Result);

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
        context.Permissions.Add(permission);
        await context.SaveChangesAsync();
        var controller = new PermissionsController(context);

        AssertGone((await controller.CreatePermission(new CreatePermissionRequest("Unsafe", "Unsafe", "Grant", "UNSAFE.GRANT", null, true))).Result);
        AssertGone((await controller.UpdatePermission(permission.Id, new UpdatePermissionRequest("Unsafe", "Unsafe", "Grant", "UNSAFE.GRANT", null, true))).Result);
        AssertGone((await controller.DeletePermission(permission.Id)).Result);

        var stored = await context.Permissions.SingleAsync();
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

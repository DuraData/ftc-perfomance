using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FTCERP.Tests;

public sealed class ControlledSeedSecurityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void RequiredSeedSecret_RejectsMissingAndWeakFallbacks(string? value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Password"] = value
        }).Build();

        var action = () => DbInitializer.GetRequiredSeedSecret(configuration, "Admin:Password");

        action.Should().Throw<InvalidOperationException>().WithMessage("*local/deployment secret*");
    }

    [Fact]
    public void RequiredSeedSecret_ReturnsExplicitStrongConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Password"] = "local-only-Strong9!"
        }).Build();

        DbInitializer.GetRequiredSeedSecret(configuration, "Admin:Password").Should().Be("local-only-Strong9!");
    }

    [Fact]
    public async Task Initial_user_security_seed_is_idempotent_and_never_replaces_administrator_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var user = new ApplicationUser
        {
            Id = "seed-user", UserName = "seed-user", NormalizedUserName = "SEED-USER",
            FirstName = "Seed", LastName = "User", SecurityStamp = Guid.NewGuid().ToString()
        };
        context.Users.Add(user);
        context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeType = ScopeType.Self });
        context.UserAssignments.Add(new UserAssignment
        {
            UserId = user.Id, AssignmentType = AssignmentType.TaskAssignee, TaskId = "administrator-task"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await DbInitializer.SeedInitialUserScopesAsync(context, user.Id,
            [new UserScope { ScopeType = ScopeType.System }]);
        await DbInitializer.SeedInitialUserAssignmentsAsync(context, user.Id,
            [new UserAssignment { AssignmentType = AssignmentType.ProjectAssignee, ProjectId = "seed-project" }]);
        await DbInitializer.SeedInitialUserScopesAsync(context, user.Id,
            [new UserScope { ScopeType = ScopeType.System }]);
        await DbInitializer.SeedInitialUserAssignmentsAsync(context, user.Id,
            [new UserAssignment { AssignmentType = AssignmentType.ProjectAssignee, ProjectId = "seed-project" }]);

        Assert.Equal(ScopeType.Self, (await context.UserScopes.SingleAsync()).ScopeType);
        Assert.Equal("administrator-task", (await context.UserAssignments.SingleAsync()).TaskId);

        context.UserAssignments.Remove(await context.UserAssignments.SingleAsync());
        var delete = async () => await context.SaveChangesAsync();
        await delete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Initial_role_permission_seed_never_replaces_existing_dynamic_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var configuredRole = new ApplicationRole { Id = "configured-role", Name = "Configured", NormalizedName = "CONFIGURED", RoleCode = "CONFIGURED" };
        var emptyRole = new ApplicationRole { Id = "empty-role", Name = "Empty", NormalizedName = "EMPTY", RoleCode = "EMPTY" };
        var denied = new Permission { Code = "NAV.DENIED", Module = "Navigation", Feature = "Denied", Action = "View", Kind = SecurityPermissionKind.Navigation, IsActive = true };
        var baseline = new Permission { Code = "NAV.BASELINE", Module = "Navigation", Feature = "Baseline", Action = "View", Kind = SecurityPermissionKind.Navigation, IsActive = true };
        context.AddRange(configuredRole, emptyRole, denied, baseline);
        await context.SaveChangesAsync();
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = configuredRole.Id,
            PermissionId = denied.Id,
            IsAllowed = false,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        await DbInitializer.SeedInitialRolePermissionsAsync(context, configuredRole.Id, [baseline.Id]);
        await DbInitializer.SeedInitialRolePermissionsAsync(context, emptyRole.Id, [baseline.Id]);
        await DbInitializer.SeedInitialRolePermissionsAsync(context, emptyRole.Id, [denied.Id]);

        var configured = await context.RolePermissions.SingleAsync(item => item.RoleId == configuredRole.Id);
        configured.PermissionId.Should().Be(denied.Id);
        configured.IsAllowed.Should().BeFalse();
        var initialized = await context.RolePermissions.SingleAsync(item => item.RoleId == emptyRole.Id);
        initialized.PermissionId.Should().Be(baseline.Id);
        initialized.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Baseline_role_seed_does_not_rewrite_existing_or_disable_dynamic_roles()
    {
        var existing = new ApplicationRole { Id = "super", Name = SecurityModel.SuperAdmin, NormalizedName = "SUPER ADMIN", RoleCode = "SUPER_ADMIN", Description = "Administrator configured", IsSystemRole = false, IsActive = false };
        var dynamicRole = new ApplicationRole { Id = "dynamic", Name = "Municipal Data Steward", NormalizedName = "MUNICIPAL DATA STEWARD", RoleCode = "DATA_STEWARD", IsActive = true };
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(store.Object, null!, null!, null!, null!);
        roleManager.Setup(manager => manager.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string roleName) => string.Equals(roleName, SecurityModel.SuperAdmin, StringComparison.OrdinalIgnoreCase) ? existing : null);
        roleManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);
        roleManager.SetupGet(manager => manager.Roles).Returns(new[] { existing, dynamicRole }.AsQueryable());

        await DbInitializer.SeedRolesAsync(roleManager.Object);

        existing.Description.Should().Be("Administrator configured");
        existing.IsSystemRole.Should().BeFalse();
        existing.IsActive.Should().BeFalse();
        dynamicRole.IsActive.Should().BeTrue();
        roleManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task Security_catalogue_seed_is_additive_and_preserves_administrator_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        await DbInitializer.SeedPermissionsAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);
        var dashboard = await context.Permissions.SingleAsync(item => item.Code == "Dashboard.View");
        var userResource = await context.SecurityResources.SingleAsync(item => item.Code == "USER");
        var emailMember = await context.SecurityMemberDefinitions.SingleAsync(item => item.ResourceCode == "USER" && item.MemberCode == "Email");
        dashboard.Description = "Administrator-defined dashboard description";
        dashboard.IsActive = false;
        userResource.SupportsDelete = false;
        emailMember.IsSensitive = false;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await DbInitializer.SeedPermissionsAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);

        dashboard = await context.Permissions.SingleAsync(item => item.Code == "Dashboard.View");
        dashboard.Description.Should().Be("Administrator-defined dashboard description");
        dashboard.IsActive.Should().BeFalse();
        (await context.SecurityResources.SingleAsync(item => item.Code == "USER")).SupportsDelete.Should().BeFalse();
        (await context.SecurityMemberDefinitions.SingleAsync(item => item.ResourceCode == "USER" && item.MemberCode == "Email")).IsSensitive.Should().BeFalse();
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "seed-test";
    }
}

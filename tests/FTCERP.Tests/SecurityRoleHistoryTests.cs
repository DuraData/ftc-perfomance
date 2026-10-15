using System.Text.Json;

namespace FTCERP.Tests;

public sealed class SecurityRoleHistoryTests
{
    [Fact]
    public async Task Role_administration_requires_and_persists_governance_reasons()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Id = 7, Code = "ROLE-GOV", Name = "Role Governance Municipality" };
        var actor = IdpTestFixture.CreateUser("role-governance-admin");
        actor.MunicipalityId = municipality.Id;
        var systemRole = NewRole("role-governance-system", "SYSTEM_ADMIN");
        var permissions = new[]
        {
            new Permission { Code = "SECURITY.SYSTEM_SCOPE", Module = "Security", Feature = "Role", Action = "System", Kind = SecurityPermissionKind.Action },
            new Permission { Code = "SECURITY.MANAGE_ROLES", Module = "Security", Feature = "Role", Action = "Manage", Kind = SecurityPermissionKind.Action }
        };
        context.AddRange(municipality, actor, systemRole);
        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
        {
            UserId = actor.Id, RoleId = systemRole.Id, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), AssignedAt = DateTime.UtcNow, AssignedBy = actor.Id
        });
        context.RolePermissions.AddRange(permissions.Select(permission => new RolePermission
        {
            RoleId = systemRole.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        }));
        await context.SaveChangesAsync();

        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipality.Id);
        tenant.SetupGet(item => item.IsSystem).Returns(true);
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null!, null!, null!, null!);
        var controller = new SecurityAdministrationController(
            context,
            new AccessControlService(context, IdpTestFixture.CreateUserManagerMock(actor).Object, roleManager.Object, tenant.Object),
            IdpTestFixture.CreateUserManagerMock(actor).Object,
            tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        (await controller.CreateRole(new CreateSecurityRoleRequest("DATA_STEWARD", "Data Steward", null, null, null, "bad"))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        var createResult = await controller.CreateRole(new CreateSecurityRoleRequest(
            "DATA_STEWARD", "Data Steward", "Governed data role", null, null, "Approved data-steward role creation"));
        var created = createResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<SecurityRoleDto>>().Subject.Data!;

        (await controller.UpdateRole(created.PublicId, new UpdateSecurityRoleRequest(
            "Data Steward Updated", "Governed data role", true, created.EffectiveFrom, created.EffectiveTo, created.RowVersion, "bad"))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        var updateResult = await controller.UpdateRole(created.PublicId, new UpdateSecurityRoleRequest(
            "Data Steward Updated", "Updated governed data role", true, created.EffectiveFrom, created.EffectiveTo, created.RowVersion, "Approved role-definition correction"));
        updateResult.Result.Should().BeOfType<OkObjectResult>();

        var audits = await context.AuditTrails.Where(item => item.EntityName == "SecurityRole" && item.EntityId == created.PublicId.ToString()).OrderBy(item => item.ChangedAt).ToArrayAsync();
        audits.Should().HaveCount(2);
        audits.Select(item => item.Reason).Should().Equal("Approved data-steward role creation", "Approved role-definition correction");
    }

    [Fact]
    public async Task Role_rewrite_requires_reasoned_same_transaction_audit_and_preserves_stable_identity()
    {
        var actor = IdpTestFixture.CreateUser("role-history-actor");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(null, actor.Id, true));
        var role = NewRole("governed-role", "GOVERNED_ROLE");
        context.AddRange(role, actor);
        await context.SaveChangesAsync();

        role.Name = "Unaudited rewrite";
        role.NormalizedName = "UNAUDITED REWRITE";
        var unaudited = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        unaudited.Message.Should().Contain("reasoned audit evidence");

        context.ChangeTracker.Clear();
        role = await context.Roles.SingleAsync(item => item.Id == "governed-role");
        var oldValue = new { role.Name, role.Description, role.IsActive, role.EffectiveFrom, role.EffectiveTo };
        role.Name = "Governed Role Updated";
        role.NormalizedName = "GOVERNED ROLE UPDATED";
        role.UpdatedAt = DateTime.UtcNow;
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = "SecurityRole",
            EntityId = role.PublicId.ToString(),
            Action = "Update",
            OldValue = JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive, role.EffectiveFrom, role.EffectiveTo }),
            ChangedBy = actor.Id,
            ChangedAt = role.UpdatedAt.Value,
            Reason = "Approved role display-name correction"
        });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        role = await context.Roles.SingleAsync(item => item.Id == "governed-role");
        role.Description = "Definition rewrite hidden behind permission audit";
        role.UpdatedAt = DateTime.UtcNow;
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = "SecurityRolePermissions",
            EntityId = role.PublicId.ToString(),
            Action = "ReplacePermissions",
            OldValue = "[]",
            NewValue = "[]",
            ChangedBy = actor.Id,
            ChangedAt = role.UpdatedAt.Value,
            Reason = "Attempt role rewrite through permission audit"
        });
        var disguisedRewrite = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        disguisedRewrite.Message.Should().Contain("exact actor-bound");

        context.ChangeTracker.Clear();
        role = await context.Roles.SingleAsync(item => item.Id == "governed-role");
        role.RoleCode = "REWRITTEN_CODE";
        var stableOldValue = new { role.Name, role.Description, role.IsActive, role.EffectiveFrom, role.EffectiveTo };
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = "SecurityRole",
            EntityId = role.PublicId.ToString(),
            Action = "Update",
            OldValue = JsonSerializer.Serialize(stableOldValue),
            NewValue = JsonSerializer.Serialize(stableOldValue),
            ChangedBy = actor.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Attempt stable-code rewrite"
        });
        var identityRewrite = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        identityRewrite.Message.Should().Contain("stable code");
    }

    [Fact]
    public async Task Role_history_rejects_hard_delete()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var role = NewRole("delete-role", "DELETE_ROLE");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        context.Roles.Remove(role);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        exception.Message.Should().Contain("cannot be hard deleted");
    }

    private static ApplicationRole NewRole(string id, string code) => new()
    {
        Id = id,
        PublicId = Guid.NewGuid(),
        RoleCode = code,
        Name = code,
        NormalizedName = code,
        IsActive = true,
        EffectiveFrom = DateTime.UtcNow.AddDays(-1)
    };
}

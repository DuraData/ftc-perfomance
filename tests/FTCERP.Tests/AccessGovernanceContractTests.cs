using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FTCERP.Tests;

public sealed class AccessGovernanceContractTests
{
    [Fact]
    public void Access_governance_is_versioned_and_simulation_contract_exposes_public_identifiers_only()
    {
        var route = Assert.Single(typeof(AccessController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());
        Assert.Equal("api/v1/access", route.Template);

        var properties = typeof(SimulateAccessRequest).GetProperties().ToDictionary(item => item.Name, item => item.PropertyType);
        Assert.Equal(typeof(Guid), properties[nameof(SimulateAccessRequest.UserPublicId)]);
        foreach (var name in new[]
        {
            nameof(SimulateAccessRequest.DepartmentPublicId), nameof(SimulateAccessRequest.UnitPublicId),
            nameof(SimulateAccessRequest.OwnerUserPublicId), nameof(SimulateAccessRequest.DelegatorUserPublicId),
            nameof(SimulateAccessRequest.TargetPublicId), nameof(SimulateAccessRequest.KpiPublicId),
            nameof(SimulateAccessRequest.ProjectPublicId), nameof(SimulateAccessRequest.TaskPublicId)
        })
            Assert.Equal(typeof(Guid?), properties[name]);

        Assert.DoesNotContain(properties, item => item.Key is "DepartmentId" or "UnitId" or "TargetId" or "KpiId" or "ProjectId" or "TaskId" or "Role");
    }

    [Fact]
    public void Legacy_unversioned_access_routes_are_explicitly_retired()
    {
        var controller = new LegacyAccessController();
        AssertGone(controller.GetMyPermissions().Result);
        AssertGone(controller.Check().Result);
        AssertGone(controller.Simulate().Result);
        AssertGone(controller.GetRoleAccessMatrixPage().Result);
        AssertGone(controller.GetRoleAccessMatrix().Result);
        AssertGone(controller.GetSystemCoverageAudit().Result);
    }

    [Fact]
    public async Task Simulation_resolves_public_organization_and_target_ids_inside_the_selected_tenant()
    {
        const long municipalityId = 9701;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = municipalityId, PublicId = Guid.NewGuid(), Code = "SIM-9701", Name = "Simulation Municipality" };
        var department = new Department { PublicId = Guid.NewGuid(), MunicipalityId = municipalityId, Code = "SIM", Name = "Simulation Department", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var subject = IdpTestFixture.CreateUser("simulation-subject", "Simulation", "Subject");
        subject.MunicipalityId = municipalityId;
        var target = new OpmsTarget { PublicId = Guid.NewGuid(), MunicipalityId = municipalityId, Id = "internal-target-key", IndicatorNumber = "SIM-1", TargetName = "Simulation target", KpiDescription = "Simulation KPI" };
        context.AddRange(municipality, department, subject, target);
        await context.SaveChangesAsync();
        var unit = new Unit { PublicId = Guid.NewGuid(), MunicipalityId = municipalityId, DepartmentId = department.Id, Code = "SIM-U", Name = "Simulation Unit", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        context.Units.Add(unit);
        await context.SaveChangesAsync();

        AccessScopeContext? captured = null;
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(subject, "OPMS_KPI.READ", It.IsAny<AccessScopeContext>()))
            .Callback<ApplicationUser, string, AccessScopeContext?>((_, _, scope) => captured = scope)
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", ["OPMS_KPI.READ"], ["UnitScope"], []));
        var controller = new AccessController(access.Object, IdpTestFixture.CreateUserManagerMock(subject).Object, context, tenant.Object);

        var result = await controller.Simulate(new SimulateAccessRequest(
            subject.PublicId, department.PublicId, unit.PublicId, null, null,
            target.PublicId, null, null, null, "OPMS_KPI.READ"));

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(captured);
        Assert.Equal(municipalityId, captured.MunicipalityId);
        Assert.Equal(department.Id, captured.DepartmentId);
        Assert.Equal(unit.Id, captured.UnitId);
        Assert.Equal(target.Id, captured.TargetId);
    }

    [Fact]
    public async Task Simulation_rejects_a_foreign_tenant_record_public_id_before_authorization_evaluation()
    {
        const long municipalityId = 9702;
        var tenant = new Mock<ITenantContext>();
        long? currentMunicipalityId = null;
        tenant.SetupGet(item => item.MunicipalityId).Returns(() => currentMunicipalityId);
        tenant.SetupGet(item => item.IsSystem).Returns(true);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var selected = new Municipality { Id = municipalityId, PublicId = Guid.NewGuid(), Code = "SIM-9702", Name = "Selected Municipality" };
        var foreign = new Municipality { Id = 9703, PublicId = Guid.NewGuid(), Code = "SIM-9703", Name = "Foreign Municipality" };
        var subject = IdpTestFixture.CreateUser("simulation-tenant-subject", "Simulation", "Subject");
        subject.MunicipalityId = municipalityId;
        var foreignTarget = new OpmsTarget { PublicId = Guid.NewGuid(), MunicipalityId = foreign.Id, Id = "foreign-internal-target", IndicatorNumber = "SIM-F", TargetName = "Foreign target", KpiDescription = "Foreign KPI" };
        context.AddRange(selected, foreign, subject, foreignTarget);
        await context.SaveChangesAsync();
        currentMunicipalityId = municipalityId;
        var access = new Mock<IAccessControlService>();
        var controller = new AccessController(access.Object, IdpTestFixture.CreateUserManagerMock(subject).Object, context, tenant.Object);

        var result = await controller.Simulate(new SimulateAccessRequest(
            subject.PublicId, null, null, null, null,
            foreignTarget.PublicId, null, null, null, "OPMS_KPI.READ"));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        access.Verify(item => item.CheckPermissionAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext>()), Times.Never);
    }

    private static void AssertGone(ActionResult? result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, objectResult.StatusCode);
    }
}

using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Security.Claims;

namespace FTCERP.Tests;

public sealed class AccessGovernanceContractTests
{
    [Fact]
    public void Self_permission_bootstrap_requires_authentication_without_a_circular_dynamic_permission()
    {
        Assert.Contains(typeof(AccessController).GetCustomAttributes<AuthorizeAttribute>(),
            attribute => string.IsNullOrWhiteSpace(attribute.Policy));

        var method = typeof(AccessController).GetMethod(nameof(AccessController.GetMyPermissions));
        Assert.NotNull(method);
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
    }

    [Fact]
    public async Task Any_permission_policy_succeeds_when_one_dynamic_permission_is_allowed()
    {
        var user = IdpTestFixture.CreateUser("any-permission-user", "Any", "Permission");
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, "FIRST", null))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "SECOND", null))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", ["SECOND"], [], []));
        var handler = new AnyPermissionHandler(IdpTestFixture.CreateUserManagerMock(user).Object, access.Object);
        var requirement = new AnyPermissionRequirement(["FIRST", "SECOND"]);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id)], "test"));
        var authorization = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(authorization);

        Assert.True(authorization.HasSucceeded);
        access.Verify(item => item.CheckPermissionAsync(user, "FIRST", null), Times.Once);
        access.Verify(item => item.CheckPermissionAsync(user, "SECOND", null), Times.Once);
    }

    [Fact]
    public async Task Governance_screen_policies_keep_direct_api_access_aligned_with_each_visible_screen()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        var policy = await provider.GetPolicyAsync(
            "PermissionAny:SystemAdministration.RoleAccessMatrix.View,RoleImplementationAudit.View");
        var requirement = Assert.Single(policy!.Requirements.OfType<AnyPermissionRequirement>());
        Assert.Equal(
            ["SystemAdministration.RoleAccessMatrix.View", "RoleImplementationAudit.View"],
            requirement.PermissionCodes);

        AssertPolicy<AccessController>(nameof(AccessController.GetRoleAccessMatrixPage),
            "Permission:SystemAdministration.RoleAccessMatrix.View");
        AssertPolicy<AccessController>(nameof(AccessController.GetSystemCoverageAuditPage),
            "Permission:SystemAdministration.SystemCoverageAudit.View");
        AssertPolicy<AccessController>(nameof(AccessController.GetRolePermissionCrudAuditPage),
            "Permission:RoleImplementationAudit.View");
        AssertPolicy<SecurityAdministrationController>(nameof(SecurityAdministrationController.GetPermissionDefinitionsPage),
            "PermissionAny:SECURITY.VIEW,SystemAdministration.PermissionSimulation.View");
        AssertPolicy<OrganizationMastersController>(nameof(OrganizationMastersController.GetDepartmentsPage),
            "PermissionAny:DEPARTMENT.READ,SystemAdministration.PermissionSimulation.View");
        AssertPolicy<OrganizationMastersController>(nameof(OrganizationMastersController.GetUnitsPage),
            "PermissionAny:UNIT.READ,SystemAdministration.PermissionSimulation.View");
    }

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
    public void User_scope_and_operational_assignment_contracts_expose_public_record_identifiers_only()
    {
        AssertPublicOnly(typeof(UserScopeItemRequest));
        AssertPublicOnly(typeof(UserAssignmentItemRequest));
        AssertPublicOnly(typeof(UserScopeResponse));
        AssertPublicOnly(typeof(UserAssignmentResponse));

        foreach (var contract in new[] { typeof(UserScopeItemRequest), typeof(UserScopeResponse) })
        {
            var properties = contract.GetProperties().ToDictionary(item => item.Name, item => item.PropertyType);
            Assert.Equal(typeof(Guid?), properties["DepartmentPublicId"]);
            Assert.Equal(typeof(Guid?), properties["UnitPublicId"]);
        }

        foreach (var contract in new[] { typeof(UserAssignmentItemRequest), typeof(UserAssignmentResponse) })
        {
            var properties = contract.GetProperties().ToDictionary(item => item.Name, item => item.PropertyType);
            Assert.Equal(typeof(Guid?), properties["DelegatorUserPublicId"]);
        }
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

    [Fact]
    public async Task Scope_identity_resolver_round_trips_tenant_owned_idp_kpi_project_and_task_public_ids()
    {
        const long municipalityId = 9704;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = municipalityId, PublicId = Guid.NewGuid(), Code = "SIM-9704", Name = "IDP Scope Municipality" };
        var actor = IdpTestFixture.CreateUser("idp-scope-actor", "IDP", "Actor");
        actor.MunicipalityId = municipalityId;
        var plan = new IdpPlan { PublicId = Guid.NewGuid(), MunicipalityId = municipalityId, MunicipalityName = municipality.Name, PlanCode = "IDP-SCOPE", PlanTitle = "Scope plan", CreatedByUserId = actor.Id };
        var outcome = new IdpStrategicOutcome { PublicId = Guid.NewGuid(), IdpPlan = plan, Code = "OUT", Name = "Outcome" };
        var objective = new IdpStrategicObjective { PublicId = Guid.NewGuid(), IdpStrategicOutcome = outcome, Code = "OBJ", Name = "Objective", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddYears(1) };
        var priority = new IdpDevelopmentPriority { PublicId = Guid.NewGuid(), IdpStrategicObjective = objective, PriorityCode = "PRI", Name = "Priority" };
        var programme = new IdpProgramme { PublicId = Guid.NewGuid(), IdpDevelopmentPriority = priority, ProgrammeCode = "PRG", Name = "Programme" };
        var project = new IdpProject { PublicId = Guid.NewGuid(), IdpProgramme = programme, ProjectCode = "PRJ", ProjectName = "Project", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(6) };
        var kpi = new IdpKpi { PublicId = Guid.NewGuid(), IdpProject = project, KpiCode = "KPI", KpiName = "KPI" };
        var task = new IdpTaskAssignment { PublicId = Guid.NewGuid(), IdpPlan = plan, Title = "Task", AssignedToUserId = actor.Id, AssignedByUserId = actor.Id, DueDate = DateTime.UtcNow.AddDays(5) };
        context.AddRange(municipality, actor, plan, outcome, objective, priority, programme, project, kpi, task);
        await context.SaveChangesAsync();

        var resolved = await SecurityScopeIdentityResolver.ResolveAsync(
            context, municipalityId, null, null, null, kpi.PublicId, project.PublicId, task.PublicId);
        Assert.True(resolved.Succeeded);
        Assert.Equal(kpi.Id.ToString(), resolved.KpiId);
        Assert.Equal(project.Id.ToString(), resolved.ProjectId);
        Assert.Equal(task.Id.ToString(), resolved.TaskId);

        var projected = await SecurityScopeIdentityResolver.ResolvePublicIdsAsync(
            context, municipalityId, [], [resolved.KpiId], [resolved.ProjectId], [resolved.TaskId]);
        Assert.Equal(kpi.PublicId, projected.Kpis[resolved.KpiId!]);
        Assert.Equal(project.PublicId, projected.Projects[resolved.ProjectId!]);
        Assert.Equal(task.PublicId, projected.Tasks[resolved.TaskId!]);
    }

    [Fact]
    public void Submission_http_contracts_expose_public_identity_and_constrain_every_submission_key_route()
    {
        foreach (var responseType in new[] { typeof(OpmsSubmissionResponse), typeof(IpmsSubmissionResponse) })
        {
            Assert.Equal(typeof(Guid), responseType.GetProperty("PublicId")?.PropertyType);
            Assert.Null(responseType.GetProperty("Id"));
        }

        Assert.Null(typeof(WorkflowQueueItemResponse).GetProperty("Id"));
        Assert.Null(typeof(WorkflowQueueItemResponse).GetProperty("TargetId"));
        Assert.Equal(typeof(Guid), typeof(WorkflowQueueItemResponse).GetProperty("PublicId")?.PropertyType);
        Assert.Equal(typeof(Guid), typeof(PoeFileResponse).GetProperty("SubmissionPublicId")?.PropertyType);
        Assert.Null(typeof(PoeFileResponse).GetProperty("SubmissionId"));
        Assert.Equal(typeof(Guid), typeof(PoeFileResponse).GetProperty("PublicId")?.PropertyType);
        Assert.Null(typeof(PoeFileResponse).GetProperty("Id"));

        AssertGuidRoutes(typeof(OpmsSubmissionsController), "{id", "{id:guid}");
        AssertGuidRoutes(typeof(IpmsSubmissionsController), "{id", "{id:guid}");
        AssertGuidRoutes(typeof(WorkflowConfigurationController), "{submissionId", "{submissionId:guid}");
        AssertGuidRoutes(typeof(InternalAuditAssessmentsController), "{submissionId", "{submissionId:guid}");
        AssertGuidRoutes(typeof(OpmsSubmissionsController), "{attachmentId", "{attachmentId:guid}");
        AssertGuidRoutes(typeof(IpmsSubmissionsController), "{attachmentId", "{attachmentId:guid}");
    }

    [Fact]
    public void Tenant_and_organization_directory_contracts_expose_public_identity_only()
    {
        AssertPublicIdentityOnly(typeof(TenantContextDto));
        AssertPublicIdentityOnly(typeof(WardMasterDto));
        AssertPublicIdentityOnly(typeof(VoteNumberMasterDto));
        Assert.Equal("X-Municipality-Public-Id", TenantResolutionMiddleware.HeaderName);
        Assert.Equal("X-Municipality-Id", TenantResolutionMiddleware.LegacyHeaderName);
    }

    private static void AssertGone(ActionResult? result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, objectResult.StatusCode);
    }

    private static void AssertPolicy<TController>(string methodName, string expected)
    {
        var method = typeof(TController).GetMethod(methodName);
        Assert.NotNull(method);
        var authorization = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(expected, authorization.Policy);
    }

    private static void AssertPublicOnly(Type contract)
    {
        var properties = contract.GetProperties().ToDictionary(item => item.Name, item => item.PropertyType);
        foreach (var retired in new[] { "DepartmentId", "UnitId", "TargetId", "KpiId", "ProjectId", "TaskId" })
            Assert.DoesNotContain(retired, properties.Keys);
        foreach (var publicName in new[] { "TargetPublicId", "KpiPublicId", "ProjectPublicId", "TaskPublicId" })
            Assert.Equal(typeof(Guid?), properties[publicName]);
    }

    private static void AssertPublicIdentityOnly(Type contract)
    {
        Assert.Equal(typeof(Guid), contract.GetProperty("PublicId")?.PropertyType);
        Assert.Null(contract.GetProperty("Id"));
    }

    private static void AssertGuidRoutes(Type controllerType, string marker, string constrainedMarker)
    {
        var templates = controllerType.GetMethods()
            .Where(method => method.GetCustomAttribute<ApiExplorerSettingsAttribute>()?.IgnoreApi != true)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => attribute.Template)
            .Where(template => template?.Contains(marker, StringComparison.Ordinal) == true)
            .ToArray();
        Assert.NotEmpty(templates);
        Assert.All(templates, template => Assert.Contains(constrainedMarker, template, StringComparison.Ordinal));
    }
}

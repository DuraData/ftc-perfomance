namespace FTCERP.Tests;

public sealed class IdpAlignmentRiskMemberSecurityTests
{
    [Fact]
    public async Task Alignment_notes_and_risk_mitigation_require_dynamic_member_permissions_and_mask_mutation_responses()
    {
        var tenant = IdpTestFixture.Tenant(704, "idp-governance-member-admin");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        var municipality = new Municipality { Id = 704, Code = "IDP-704", Name = "Governance Municipality" };
        var actor = IdpTestFixture.CreateUser("idp-governance-member-admin");
        actor.MunicipalityId = municipality.Id;
        var plan = new IdpPlan
        {
            Municipality = municipality,
            MunicipalityName = municipality.Name,
            PlanCode = "IDP-2028",
            PlanTitle = "Protected Governance IDP",
            StartFinancialYear = 2028,
            EndFinancialYear = 2033,
            CreatedByUserId = actor.Id
        };
        var outcome = new IdpStrategicOutcome
        {
            IdpPlan = plan,
            Code = "OUT-1",
            Name = "Outcome",
            Description = "Outcome description",
            SortOrder = 1
        };
        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcome = outcome,
            Code = "OBJ-1",
            Name = "Objective",
            Description = "Objective description",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            SortOrder = 1
        };
        context.AddRange(municipality, actor, plan, outcome, objective);
        await context.SaveChangesAsync();

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "allowed" : "denied", [], [], []));
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(item => item.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(actor).Object,
            workflow.Object, actor.Id, tenant, access.Object);

        var deniedAlignment = await controller.CreateAlignmentLink(new CreateIdpAlignmentLinkRequest(
            objective.PublicId, "NationalDevelopmentPlan", "NDP-1", "National reference", "Protected alignment rationale"));
        Assert.IsType<ForbidResult>(deniedAlignment.Result);
        Assert.Empty(await context.IdpAlignmentLinks.ToArrayAsync());

        var deniedRisk = await controller.CreateRiskLink(new CreateIdpRiskLinkRequest(
            objective.PublicId, null, null, "RISK-1", "Delivery risk", "Protected mitigation plan", "High"));
        Assert.IsType<ForbidResult>(deniedRisk.Result);
        Assert.Empty(await context.IdpRiskLinks.ToArrayAsync());

        allowedCodes.Add("IDP_ALIGNMENT.Notes.UPDATE");
        allowedCodes.Add("IDP_RISK_LINK.MitigationPlan.UPDATE");

        var alignment = Extract<IdpAlignmentLinkResponse>((await controller.CreateAlignmentLink(
            new CreateIdpAlignmentLinkRequest(objective.PublicId, "NationalDevelopmentPlan", "NDP-1",
                "National reference", "Protected alignment rationale"))).Result!);
        alignment.Notes.Should().BeNull();
        (await context.IdpAlignmentLinks.SingleAsync()).Notes.Should().Be("Protected alignment rationale");

        var risk = Extract<IdpRiskLinkResponse>((await controller.CreateRiskLink(new CreateIdpRiskLinkRequest(
            objective.PublicId, null, null, "RISK-1", "Delivery risk", "Protected mitigation plan", "High"))).Result!);
        risk.MitigationPlan.Should().BeNull();
        (await context.IdpRiskLinks.SingleAsync()).MitigationPlan.Should().Be("Protected mitigation plan");

        allowedCodes.Add("IDP_ALIGNMENT.Notes.READ");
        allowedCodes.Add("IDP_RISK_LINK.MitigationPlan.READ");

        var visibleAlignment = Extract<IdpAlignmentLinkResponse>((await controller.CreateAlignmentLink(
            new CreateIdpAlignmentLinkRequest(objective.PublicId, "DistrictDevelopmentModel", "DDM-1",
                "District reference", "Visible alignment rationale"))).Result!);
        visibleAlignment.Notes.Should().Be("Visible alignment rationale");

        var visibleRisk = Extract<IdpRiskLinkResponse>((await controller.CreateRiskLink(new CreateIdpRiskLinkRequest(
            objective.PublicId, null, null, "RISK-2", "Capacity risk", "Visible mitigation plan", "Medium"))).Result!);
        visibleRisk.MitigationPlan.Should().Be("Visible mitigation plan");

        allowedCodes.Remove("IDP_ALIGNMENT.Notes.READ");
        allowedCodes.Remove("IDP_RISK_LINK.MitigationPlan.READ");
        var remaskedAlignment = Extract<IdpAlignmentLinkResponse>((await controller.CreateAlignmentLink(
            new CreateIdpAlignmentLinkRequest(objective.PublicId, "SectorPlan", "SEC-1",
                "Sector reference", "Remasked rationale"))).Result!);
        remaskedAlignment.Notes.Should().BeNull();
        var remaskedRisk = Extract<IdpRiskLinkResponse>((await controller.CreateRiskLink(new CreateIdpRiskLinkRequest(
            objective.PublicId, null, null, "RISK-3", "Funding risk", "Remasked mitigation", "Critical"))).Result!);
        remaskedRisk.MitigationPlan.Should().BeNull();

        AuthorizationPolicy(nameof(IdpController.GetAlignmentMatrixPage)).Should().Be("Permission:IDP_ALIGNMENT.READ");
        AuthorizationPolicy(nameof(IdpController.CreateAlignmentLink)).Should().Be("Permission:IDP_ALIGNMENT.CREATE");
        AuthorizationPolicy(nameof(IdpController.CreateRiskLink)).Should().Be("Permission:IDP_RISK_LINK.CREATE");
    }

    private static string? AuthorizationPolicy(string methodName) => typeof(IdpController).GetMethod(methodName)!
        .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
        .Single().Policy;

    private static T Extract<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<ApiResponse<T>>(ok.Value).Data!;
    }
}

namespace FTCERP.Tests;

public sealed class IdpParticipationMemberSecurityTests
{
    [Fact]
    public async Task Participation_writes_responses_and_ward_report_enforce_dynamic_member_permissions()
    {
        var tenant = IdpTestFixture.Tenant(703, "participation-security-admin");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        var municipality = new Municipality { Id = 703, Code = "IDP-703", Name = "Participation Municipality" };
        var actor = IdpTestFixture.CreateUser("participation-security-admin");
        actor.MunicipalityId = municipality.Id;
        var plan = new IdpPlan
        {
            Municipality = municipality, MunicipalityName = municipality.Name, PlanCode = "IDP-2028",
            PlanTitle = "Protected Participation IDP", StartFinancialYear = 2028, EndFinancialYear = 2033,
            CreatedByUserId = actor.Id
        };
        var ward = new Ward { Municipality = municipality, Code = "W01", Name = "Ward 1", IsActive = true };
        context.AddRange(municipality, actor, plan, ward);
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

        var deniedSession = await controller.CreateCommunitySession(new CreateIdpCommunitySessionRequest(
            plan.PublicId, "PublicMeeting", DateTime.UtcNow, "Community Hall", ward.PublicId, 22,
            "private/attendance.pdf", "private/minutes.pdf"));
        Assert.IsType<ForbidResult>(deniedSession.Result);
        Assert.Empty(await context.IdpCommunitySessions.ToArrayAsync());

        foreach (var member in new[] { "AttendanceRegisterPath", "MinutesPath", "CommunityNeedDescription", "ProposedIntervention", "WardPlanSummary", "WardPriorities", "WardProjects" })
            allowedCodes.Add($"IDP_PARTICIPATION.{member}.UPDATE");

        var sessionResponse = Extract<IdpCommunitySessionResponse>((await controller.CreateCommunitySession(
            new CreateIdpCommunitySessionRequest(plan.PublicId, "PublicMeeting", DateTime.UtcNow, "Community Hall",
                ward.PublicId, 22, "private/attendance.pdf", "private/minutes.pdf"))).Result!);
        Assert.Null(sessionResponse.AttendanceRegisterPath);
        Assert.Null(sessionResponse.MinutesPath);

        var needResponse = Extract<IdpCommunityNeedResponse>((await controller.CreateCommunityNeed(
            new CreateIdpCommunityNeedRequest(sessionResponse.PublicId, "Water", "Protected need narrative", "High",
                "Protected intervention"))).Result!);
        Assert.Null(needResponse.Description);
        Assert.Null(needResponse.ProposedIntervention);

        var wardResponse = Extract<IdpWardInputResponse>((await controller.CreateWardInput(
            new CreateIdpWardInputRequest(plan.PublicId, ward.PublicId, "Protected ward plan", "Protected priorities",
                "Protected projects"))).Result!);
        Assert.Null(wardResponse.WardPlanSummary);
        Assert.Null(wardResponse.WardPriorities);
        Assert.Null(wardResponse.WardProjects);

        (await context.IdpCommunitySessions.SingleAsync()).AttendanceRegisterPath.Should().Be("private/attendance.pdf");
        (await context.IdpCommunityNeeds.SingleAsync()).Description.Should().Be("Protected need narrative");
        (await context.IdpWardInputs.SingleAsync()).WardProjects.Should().Be("Protected projects");

        allowedCodes.UnionWith(new[]
        {
            "IDP_PARTICIPATION.READ",
            "IDP_PARTICIPATION.WardPlanSummary.READ", "IDP_PARTICIPATION.WardPriorities.READ",
            "IDP_PARTICIPATION.WardProjects.READ"
        });
        var report = await controller.GenerateReportByPublicId(plan.PublicId, "ward-based", "pdf");
        Assert.IsType<OkObjectResult>(report.Result);

        allowedCodes.Remove("IDP_PARTICIPATION.WardProjects.READ");
        var deniedReport = await controller.GenerateReportByPublicId(plan.PublicId, "ward-based", "pdf");
        Assert.IsType<ForbidResult>(deniedReport.Result);
    }

    private static T Extract<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<ApiResponse<T>>(ok.Value).Data!;
    }
}

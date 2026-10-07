namespace FTCERP.Tests;

public sealed class IdpStakeholderMemberSecurityTests
{
    [Fact]
    public async Task Stakeholder_register_masks_denied_contacts_and_blocks_search_sort_and_direct_write_inference()
    {
        var tenant = IdpTestFixture.Tenant(701, "participation-admin");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        var municipality = new Municipality { Id = 701, Code = "IDP-701", Name = "IDP Municipality" };
        var actor = IdpTestFixture.CreateUser("participation-admin");
        actor.MunicipalityId = municipality.Id;
        var plan = new IdpPlan
        {
            Municipality = municipality, MunicipalityName = municipality.Name, PlanCode = "IDP-2026",
            PlanTitle = "Protected IDP", StartFinancialYear = 2026, EndFinancialYear = 2031, CreatedByUserId = actor.Id
        };
        var session = new IdpCommunitySession
        {
            IdpPlan = plan, ParticipationType = IdpParticipationType.StakeholderEngagement,
            SessionDate = DateTime.UtcNow.AddDays(-1), Venue = "Council Chamber", ParticipantsCount = 8
        };
        context.AddRange(municipality, actor, plan, session);
        context.IdpStakeholderEngagements.Add(new IdpStakeholderEngagement
        {
            IdpCommunitySession = session, StakeholderType = "Business Forum", StakeholderName = "Local Forum",
            ContactPerson = "Protected Person", ContactEmail = "protected.person@example.test", KeyInput = "Local procurement"
        });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "denied", [], [], []));
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(item => item.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(actor).Object,
            workflow.Object, actor.Id, tenant, access.Object);

        var pageResult = await controller.GetStakeholderEngagementsPage(plan.PublicId, new PagedQueryRequest { SortBy = "sessionDate" });
        var page = Assert.IsType<ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>>(
            Assert.IsType<OkObjectResult>(pageResult.Result).Value).Data!;
        var protectedItem = Assert.Single(page.Items);
        Assert.Null(protectedItem.ContactPerson);
        Assert.Null(protectedItem.ContactEmail);
        Assert.Equal(session.PublicId, protectedItem.CommunitySessionPublicId);

        var hiddenSearch = await controller.GetStakeholderEngagementsPage(plan.PublicId,
            new PagedQueryRequest { SortBy = "sessionDate", Search = "protected.person@example.test" });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>>(
            Assert.IsType<OkObjectResult>(hiddenSearch.Result).Value).Data!.TotalCount);
        var protectedSort = await controller.GetStakeholderEngagementsPage(plan.PublicId,
            new PagedQueryRequest { SortBy = "contactEmail" });
        Assert.IsType<ForbidResult>(protectedSort.Result);

        var deniedWrite = await controller.CreateStakeholderEngagementV1(session.PublicId,
            new CreateIdpStakeholderEngagementV1Request("Civil Society", "Residents Association", "Private Contact",
                "private@example.test", "Service reliability"));
        Assert.IsType<ForbidResult>(deniedWrite.Result);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.CreateStakeholderEngagement(
            new CreateIdpStakeholderEngagementRequest(session.Id, "Civil Society", "Residents Association", null, null, null)).Result).StatusCode);
    }

    [Fact]
    public async Task Stakeholder_contact_permissions_can_be_changed_without_recompiling()
    {
        var tenant = IdpTestFixture.Tenant(702, "participation-admin");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        var municipality = new Municipality { Id = 702, Code = "IDP-702", Name = "Dynamic IDP Municipality" };
        var actor = IdpTestFixture.CreateUser("participation-admin");
        actor.MunicipalityId = municipality.Id;
        var plan = new IdpPlan { Municipality = municipality, MunicipalityName = municipality.Name, PlanCode = "IDP-2027", PlanTitle = "Dynamic IDP", StartFinancialYear = 2027, EndFinancialYear = 2032, CreatedByUserId = actor.Id };
        var session = new IdpCommunitySession { IdpPlan = plan, ParticipationType = IdpParticipationType.StakeholderEngagement, SessionDate = DateTime.UtcNow, Venue = "Library", ParticipantsCount = 12 };
        context.AddRange(municipality, actor, plan, session);
        await context.SaveChangesAsync();

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IDP_STAKEHOLDER.ContactPerson.UPDATE", "IDP_STAKEHOLDER.ContactEmail.UPDATE"
        };
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "allowed" : "denied", [], [], []));
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(item => item.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(actor).Object,
            workflow.Object, actor.Id, tenant, access.Object);

        var createdResult = await controller.CreateStakeholderEngagementV1(session.PublicId,
            new CreateIdpStakeholderEngagementV1Request("Youth", "Youth Council", "Nandi Molefe", "nandi@example.test", "Skills development"));
        var created = Assert.IsType<ApiResponse<IdpStakeholderEngagementPageItemResponse>>(
            Assert.IsType<OkObjectResult>(createdResult.Result).Value).Data!;
        Assert.Null(created.ContactPerson);
        Assert.Null(created.ContactEmail);

        allowedCodes.Add("IDP_STAKEHOLDER.ContactPerson.READ");
        allowedCodes.Add("IDP_STAKEHOLDER.ContactEmail.READ");
        var refreshedResult = await controller.GetStakeholderEngagementsPage(plan.PublicId, new PagedQueryRequest { SortBy = "contactEmail" });
        var refreshed = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>>(
            Assert.IsType<OkObjectResult>(refreshedResult.Result).Value).Data!.Items);
        Assert.Equal("Nandi Molefe", refreshed.ContactPerson);
        Assert.Equal("nandi@example.test", refreshed.ContactEmail);
    }
}

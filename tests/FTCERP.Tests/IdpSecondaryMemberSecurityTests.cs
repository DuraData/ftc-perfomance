namespace FTCERP.Tests;

public class IdpSecondaryMemberSecurityTests
{
    [Fact]
    public async Task Dashboard_masks_annual_performance_and_budget_aggregates_until_member_reads_are_granted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("idp-dashboard-reader");
        var graph = await SeedGraphAsync(context, user);
        context.IdpAnnualTargets.AddRange(
            new IdpAnnualTarget { IdpKpiId = graph.Kpi.Id, FinancialYear = 2026, TargetValue = 10, ActualValue = 12, ProgressComment = "protected progress" },
            new IdpAnnualTarget { IdpKpiId = graph.Kpi.Id, FinancialYear = 2027, TargetValue = 10, ActualValue = 8, ProgressComment = "protected variance" });
        context.IdpBudgetSnapshots.Add(new IdpBudgetSnapshot
        {
            IdpProjectId = graph.Project.Id,
            FinancialYear = 2026,
            PlannedBudget = 1000,
            ApprovedBudget = 900,
            ActualExpenditure = 450,
            SourceSystem = "PROTECTED-FMS"
        });
        await context.SaveChangesAsync();

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var controller = Controller(context, user, allowed);

        var denied = Extract<IdpDashboardResponse>((await controller.GetDashboardByPublicId(graph.Plan.PublicId)).Result!);
        denied.KpiAchievementRate.Should().BeNull();
        denied.PlannedBudget.Should().BeNull();
        denied.ApprovedBudget.Should().BeNull();
        denied.ActualExpenditure.Should().BeNull();

        allowed.UnionWith([
            "IDP_INDICATOR.AnnualTargetValue.READ",
            "IDP_INDICATOR.AnnualActualValue.READ",
            "IDP_PROJECT.BudgetSnapshotPlanned.READ",
            "IDP_PROJECT.BudgetSnapshotApproved.READ",
            "IDP_PROJECT.BudgetSnapshotActual.READ"
        ]);

        var visible = Extract<IdpDashboardResponse>((await controller.GetDashboardByPublicId(graph.Plan.PublicId)).Result!);
        visible.KpiAchievementRate.Should().Be(50);
        visible.PlannedBudget.Should().Be(1000);
        visible.ApprovedBudget.Should().Be(900);
        visible.ActualExpenditure.Should().Be(450);
    }

    [Fact]
    public async Task Direct_secondary_writes_require_member_updates_and_mutation_responses_mask_denied_reads()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("idp-collaboration-actor", "Governance", "Actor");
        var assignee = IdpTestFixture.CreateUser("idp-task-assignee", "Task", "Assignee");
        var graph = await SeedGraphAsync(context, actor, assignee);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var controller = Controller(context, actor, allowed, assignee);

        var annualRequest = new CreateIdpAnnualTargetRequest(graph.Kpi.Id, 2026, 100, 75, "protected annual progress");
        (await controller.CreateAnnualTarget(annualRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpAnnualTargets.Should().BeEmpty();

        allowed.UnionWith([
            "IDP_INDICATOR.AnnualTargetValue.UPDATE",
            "IDP_INDICATOR.AnnualActualValue.UPDATE",
            "IDP_INDICATOR.AnnualProgressComment.UPDATE"
        ]);
        var maskedAnnual = Extract<IdpAnnualTargetResponse>((await controller.CreateAnnualTarget(annualRequest)).Result!);
        maskedAnnual.TargetValue.Should().BeNull();
        maskedAnnual.ActualValue.Should().BeNull();
        maskedAnnual.ProgressComment.Should().BeNull();

        var budgetRequest = new CreateIdpBudgetSnapshotRequest(null, graph.Project.Id, 2026, 1000, 900, 450, "PROTECTED-FMS");
        (await controller.CreateBudgetSnapshot(budgetRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpBudgetSnapshots.Should().BeEmpty();
        allowed.UnionWith([
            "IDP_PROJECT.BudgetSnapshotPlanned.UPDATE",
            "IDP_PROJECT.BudgetSnapshotApproved.UPDATE",
            "IDP_PROJECT.BudgetSnapshotActual.UPDATE",
            "IDP_PROJECT.BudgetSnapshotSource.UPDATE"
        ]);
        var maskedBudget = Extract<IdpBudgetSnapshotResponse>((await controller.CreateBudgetSnapshot(budgetRequest)).Result!);
        maskedBudget.PlannedBudget.Should().BeNull();
        maskedBudget.ApprovedBudget.Should().BeNull();
        maskedBudget.ActualExpenditure.Should().BeNull();
        maskedBudget.SourceSystem.Should().BeNull();

        var commentRequest = new CreateIdpCommentRequest(graph.Plan.PublicId, null, "IdpProject", graph.Project.PublicId.ToString(), "protected review comment");
        (await controller.CreateComment(commentRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpCollaborationComments.Should().BeEmpty();
        allowed.Add("IDP_PLAN.CollaborationComment.UPDATE");
        var maskedComment = Extract<IdpCommentResponse>((await controller.CreateComment(commentRequest)).Result!);
        maskedComment.EntityName.Should().BeNull();
        maskedComment.EntityId.Should().BeNull();
        maskedComment.Comment.Should().BeNull();
        maskedComment.CommentedByUserPublicId.Should().BeNull();
        maskedComment.CommentedByName.Should().BeNull();

        var taskRequest = new CreateIdpTaskRequest(graph.Plan.PublicId, null, "Protected task", "Protected task detail", assignee.PublicId, DateTime.UtcNow.AddDays(2));
        (await controller.CreateTask(taskRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpTaskAssignments.Should().BeEmpty();
        allowed.UnionWith(["IDP_PLAN.TaskContent.UPDATE", "IDP_PLAN.TaskAssignee.UPDATE"]);
        var maskedTask = Extract<IdpTaskResponse>((await controller.CreateTask(taskRequest)).Result!);
        maskedTask.Title.Should().BeNull();
        maskedTask.Description.Should().BeNull();
        maskedTask.AssignedToUserPublicId.Should().BeNull();
        maskedTask.AssignedByUserPublicId.Should().BeNull();

        allowed.UnionWith([
            "IDP_PLAN.TaskContent.READ",
            "IDP_PLAN.TaskAssignee.READ",
            "IDP_PLAN.TaskAssigner.READ"
        ]);
        var completed = Extract<IdpTaskResponse>((await controller.CompleteTaskByPublicId(maskedTask.PublicId,
            new CompleteIdpTaskRequest(true, maskedTask.RowVersion, "Reviewed and accepted"))).Result!);
        completed.Title.Should().Be("Protected task");
        completed.Description.Should().Be("Protected task detail");
        completed.AssignedToUserPublicId.Should().Be(assignee.PublicId);
        completed.AssignedToName.Should().Be(assignee.FullName);
        completed.AssignedByUserPublicId.Should().Be(actor.PublicId);
        completed.AssignedByName.Should().Be(actor.FullName);
        completed.AssignedToUserPublicId.ToString().Should().NotBe(assignee.Id);
        completed.AssignedByUserPublicId.ToString().Should().NotBe(actor.Id);
    }

    [Fact]
    public async Task Plan_version_summary_and_creator_require_dynamic_members_without_search_or_internal_identity_leakage()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("idp-version-actor", "Version", "Governor");
        var graph = await SeedGraphAsync(context, actor);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var controller = Controller(context, actor, allowed);
        var request = new CreateIdpPlanVersionRequest(
            "AnnualReview", "Governed annual review", "2026/2027", "PROTECTED-VERSION-SUMMARY");

        (await controller.CreatePlanVersionByPublicId(graph.Plan.PublicId, request)).Result.Should().BeOfType<ForbidResult>();
        context.IdpPlanVersions.Should().BeEmpty();

        allowed.Add("IDP_PLAN.VersionSummary.UPDATE");
        var maskedMutation = Extract<IdpPlanVersionResponse>((await controller.CreatePlanVersionByPublicId(graph.Plan.PublicId, request)).Result!);
        maskedMutation.SummaryOfChanges.Should().BeNull();
        maskedMutation.CreatedByUserPublicId.Should().BeNull();
        maskedMutation.CreatedByName.Should().BeNull();

        var deniedSearch = Extract<PagedResponse<IdpPlanVersionResponse>>((await controller.GetPlanVersionsPage(
            graph.Plan.PublicId, new PagedQueryRequest { Search = "PROTECTED-VERSION-SUMMARY" })).Result!);
        deniedSearch.TotalCount.Should().Be(0);

        allowed.Add("IDP_PLAN.VersionSummary.READ");
        var summaryVisible = Extract<PagedResponse<IdpPlanVersionResponse>>((await controller.GetPlanVersionsPage(
            graph.Plan.PublicId, new PagedQueryRequest { Search = "PROTECTED-VERSION-SUMMARY" })).Result!);
        summaryVisible.Items.Should().ContainSingle();
        summaryVisible.Items[0].SummaryOfChanges.Should().Be("PROTECTED-VERSION-SUMMARY");
        summaryVisible.Items[0].CreatedByUserPublicId.Should().BeNull();

        allowed.Add("IDP_PLAN.VersionCreator.READ");
        var fullyVisible = Extract<PagedResponse<IdpPlanVersionResponse>>((await controller.GetPlanVersionsPage(
            graph.Plan.PublicId, new PagedQueryRequest())).Result!);
        fullyVisible.Items.Should().ContainSingle();
        fullyVisible.Items[0].CreatedByUserPublicId.Should().Be(actor.PublicId);
        fullyVisible.Items[0].CreatedByName.Should().Be(actor.FullName);
        fullyVisible.Items[0].CreatedByUserPublicId.ToString().Should().NotBe(actor.Id);
    }

    [Fact]
    public async Task Objective_programme_and_project_governance_members_require_dynamic_updates_and_mask_mutation_responses()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("idp-hierarchy-governor", "Hierarchy", "Governor");
        var graph = await SeedGraphAsync(context, actor);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var controller = Controller(context, actor, allowed);
        var start = DateTime.UtcNow.Date;

        var deniedPaths = Extract<PagedResponse<IdpHierarchyPathResponse>>((await controller.GetHierarchyPathsPage(
            graph.Plan.PublicId, new PagedQueryRequest())).Result!);
        deniedPaths.Items.Should().ContainSingle();
        deniedPaths.Items[0].ObjectiveStrategicOwnerPublicId.Should().BeNull();
        deniedPaths.Items[0].ObjectiveStrategicOwnerName.Should().BeNull();
        deniedPaths.Items[0].ObjectiveBudgetAllocation.Should().BeNull();
        deniedPaths.Items[0].ProgrammePlannedBudget.Should().BeNull();
        deniedPaths.Items[0].ProgrammeApprovedBudget.Should().BeNull();
        deniedPaths.Items[0].ProgrammeActualExpenditure.Should().BeNull();
        deniedPaths.Items[0].ProjectBudget.Should().BeNull();
        deniedPaths.Items[0].ProjectFundingSource.Should().BeNull();
        var deniedFundingSearch = Extract<PagedResponse<IdpHierarchyPathResponse>>((await controller.GetHierarchyPathsPage(
            graph.Plan.PublicId, new PagedQueryRequest { Search = "PROTECTED-SEED-GRANT" })).Result!);
        deniedFundingSearch.TotalCount.Should().Be(0);

        var objectiveRequest = new CreateIdpStrategicObjectiveRequest(
            graph.Outcome.Id, "OBJ-PROTECTED-1", "Protected objective", "Protected objective detail",
            10, 20, null, actor.Id, start, start.AddYears(1), 123456, 2);
        (await controller.CreateObjective(objectiveRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpStrategicObjectives.Should().ContainSingle();

        allowed.UnionWith([
            "IDP_PLAN.ObjectiveStrategicOwner.UPDATE",
            "IDP_PLAN.ObjectiveBudgetAllocation.UPDATE"
        ]);
        var maskedObjective = Extract<IdpStrategicObjectiveResponse>((await controller.CreateObjective(objectiveRequest)).Result!);
        maskedObjective.StrategicOwnerUserPublicId.Should().BeNull();
        maskedObjective.StrategicOwnerName.Should().BeNull();
        maskedObjective.BudgetAllocation.Should().BeNull();

        allowed.UnionWith([
            "IDP_PLAN.ObjectiveStrategicOwner.READ",
            "IDP_PLAN.ObjectiveBudgetAllocation.READ"
        ]);
        var visibleObjective = Extract<IdpStrategicObjectiveResponse>((await controller.CreateObjective(objectiveRequest with { Code = "OBJ-PROTECTED-2" })).Result!);
        visibleObjective.StrategicOwnerUserPublicId.Should().Be(actor.PublicId);
        visibleObjective.StrategicOwnerName.Should().Be(actor.FullName);
        visibleObjective.StrategicOwnerUserPublicId.ToString().Should().NotBe(actor.Id);
        visibleObjective.BudgetAllocation.Should().Be(123456);

        var programmeRequest = new CreateIdpProgrammeRequest(
            graph.Priority.Id, "PRG-PROTECTED-1", "Protected programme", "Protected programme detail",
            null, 900000, 800000, 700000);
        (await controller.CreateProgramme(programmeRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpProgrammes.Should().ContainSingle();

        allowed.UnionWith([
            "IDP_PROJECT.ProgrammePlannedBudget.UPDATE",
            "IDP_PROJECT.ProgrammeApprovedBudget.UPDATE",
            "IDP_PROJECT.ProgrammeActualExpenditure.UPDATE"
        ]);
        var maskedProgramme = Extract<IdpProgrammeResponse>((await controller.CreateProgramme(programmeRequest)).Result!);
        maskedProgramme.PlannedBudget.Should().BeNull();
        maskedProgramme.ApprovedBudget.Should().BeNull();
        maskedProgramme.ActualExpenditure.Should().BeNull();

        allowed.UnionWith([
            "IDP_PROJECT.ProgrammePlannedBudget.READ",
            "IDP_PROJECT.ProgrammeApprovedBudget.READ",
            "IDP_PROJECT.ProgrammeActualExpenditure.READ"
        ]);
        var visibleProgramme = Extract<IdpProgrammeResponse>((await controller.CreateProgramme(programmeRequest with { ProgrammeCode = "PRG-PROTECTED-2" })).Result!);
        visibleProgramme.PlannedBudget.Should().Be(900000);
        visibleProgramme.ApprovedBudget.Should().Be(800000);
        visibleProgramme.ActualExpenditure.Should().Be(700000);

        var projectRequest = new CreateIdpProjectRequest(
            graph.Programme.Id, "PRJ-PROTECTED-1", "Protected project", "Protected project detail",
            "Capital", null, 654321, "PROTECTED-GRANT", start, start.AddMonths(6), "Planned", null);
        (await controller.CreateProject(projectRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpProjects.Should().ContainSingle();

        allowed.UnionWith([
            "IDP_PROJECT.ProjectBudget.UPDATE",
            "IDP_PROJECT.ProjectFundingSource.UPDATE"
        ]);
        var maskedProject = Extract<IdpProjectResponse>((await controller.CreateProject(projectRequest)).Result!);
        maskedProject.Budget.Should().BeNull();
        maskedProject.FundingSource.Should().BeNull();

        allowed.UnionWith([
            "IDP_PROJECT.ProjectBudget.READ",
            "IDP_PROJECT.ProjectFundingSource.READ"
        ]);
        var visibleProject = Extract<IdpProjectResponse>((await controller.CreateProject(projectRequest with { ProjectCode = "PRJ-PROTECTED-2" })).Result!);
        visibleProject.Budget.Should().Be(654321);
        visibleProject.FundingSource.Should().Be("PROTECTED-GRANT");

        var visiblePaths = Extract<PagedResponse<IdpHierarchyPathResponse>>((await controller.GetHierarchyPathsPage(
            graph.Plan.PublicId, new PagedQueryRequest())).Result!);
        visiblePaths.Items.Should().ContainSingle();
        visiblePaths.Items[0].ObjectiveStrategicOwnerPublicId.Should().Be(actor.PublicId);
        visiblePaths.Items[0].ObjectiveStrategicOwnerName.Should().Be(actor.FullName);
        visiblePaths.Items[0].ObjectiveBudgetAllocation.Should().Be(111111);
        visiblePaths.Items[0].ProgrammePlannedBudget.Should().Be(222222);
        visiblePaths.Items[0].ProgrammeApprovedBudget.Should().Be(200000);
        visiblePaths.Items[0].ProgrammeActualExpenditure.Should().Be(150000);
        visiblePaths.Items[0].ProjectBudget.Should().Be(333333);
        visiblePaths.Items[0].ProjectFundingSource.Should().Be("PROTECTED-SEED-GRANT");
    }

    [Fact]
    public async Task Strategic_objective_owner_cannot_cross_the_selected_municipality()
    {
        const long tenantId = 8101;
        const long foreignTenantId = 8102;
        var actor = IdpTestFixture.CreateUser("idp-tenant-owner", "Tenant", "Owner");
        actor.MunicipalityId = tenantId;
        var foreignOwner = IdpTestFixture.CreateUser("idp-foreign-owner", "Foreign", "Owner");
        foreignOwner.MunicipalityId = foreignTenantId;
        var tenantContext = IdpTestFixture.Tenant(tenantId, actor.Id);
        await using var context = IdpTestFixture.CreateRelationalContext(tenantContext);
        context.Municipalities.AddRange(
            new Municipality { Id = tenantId, Code = "IDP-A", Name = "IDP Municipality A" },
            new Municipality { Id = foreignTenantId, Code = "IDP-B", Name = "IDP Municipality B" });
        var graph = await SeedGraphAsync(context, actor, foreignOwner, tenantId);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IDP_PLAN.ObjectiveStrategicOwner.UPDATE",
            "IDP_PLAN.ObjectiveBudgetAllocation.UPDATE"
        };
        var controller = Controller(context, actor, allowed, foreignOwner, tenantContext);
        var start = DateTime.UtcNow.Date;
        var request = new CreateIdpStrategicObjectiveRequest(
            graph.Outcome.Id, "OBJ-FOREIGN", "Foreign-owned objective", "Must be rejected",
            1, 2, null, foreignOwner.Id, start, start.AddYears(1), 100, 2);

        var rejected = (await controller.CreateObjective(request)).Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        rejected.Value.Should().BeOfType<ApiResponse<IdpStrategicObjectiveResponse>>()
            .Which.Message.Should().Contain("selected municipality");
        context.IdpStrategicObjectives.Should().ContainSingle();

        var accepted = Extract<IdpStrategicObjectiveResponse>((await controller.CreateObjective(
            request with { Code = "OBJ-LOCAL", StrategicOwnerUserId = actor.Id })).Result!);
        accepted.PublicId.Should().NotBeEmpty();
        context.IdpStrategicObjectives.Should().HaveCount(2);
    }

    private static T Extract<T>(ActionResult result) where T : class
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeOfType<ApiResponse<T>>().Subject.Data!;
    }

    private static IdpController Controller(
        ApplicationDbContext context,
        ApplicationUser actor,
        HashSet<string> allowed,
        ApplicationUser? assignee = null,
        ITenantContext? tenantContext = null)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(allowed.Contains(code), allowed.Contains(code) ? "allowed" : "denied", [], [], []));
        var users = new Dictionary<string, ApplicationUser> { [actor.Id] = actor };
        if (assignee != null) users[assignee.Id] = assignee;
        return IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(actor, users).Object,
            Mock.Of<IWorkflowGovernanceService>(), actor.Id, tenantContext, accessControl: access.Object);
    }

    private static async Task<(IdpPlan Plan, IdpStrategicOutcome Outcome, IdpStrategicObjective Objective,
        IdpDevelopmentPriority Priority, IdpProgramme Programme, IdpProject Project, IdpKpi Kpi)> SeedGraphAsync(
        ApplicationDbContext context,
        ApplicationUser actor,
        ApplicationUser? secondUser = null,
        long? municipalityId = null)
    {
        context.Users.Add(actor);
        if (secondUser != null) context.Users.Add(secondUser);
        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "Governed IDP",
            PlanCode = "IDP-GOV",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            MunicipalityId = municipalityId,
            CreatedByUserId = actor.Id
        };
        var outcome = new IdpStrategicOutcome { IdpPlan = plan, Code = "SO1", Name = "Outcome", Description = "Outcome" };
        var objective = new IdpStrategicObjective { IdpStrategicOutcome = outcome, Code = "OBJ1", Name = "Objective", Description = "Objective", StrategicOwnerUserId = actor.Id, BudgetAllocation = 111111, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1) };
        var priority = new IdpDevelopmentPriority { IdpStrategicObjective = objective, PriorityCode = "PRI1", Name = "Priority", Description = "Priority" };
        var programme = new IdpProgramme { IdpDevelopmentPriority = priority, ProgrammeCode = "PRG1", Name = "Programme", Description = "Programme", PlannedBudget = 222222, ApprovedBudget = 200000, ActualExpenditure = 150000 };
        var project = new IdpProject { IdpProgramme = programme, ProjectCode = "PRJ1", ProjectName = "Project", Description = "Project", Category = "Capital", Budget = 333333, FundingSource = "PROTECTED-SEED-GRANT", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1) };
        var kpi = new IdpKpi { IdpProject = project, KpiCode = "KPI1", KpiName = "KPI", Description = "KPI", Formula = "Count", DataSource = "System", ReportingFrequency = "Annual", IndicatorType = IdpKpiIndicatorType.Output };
        context.Add(kpi);
        await context.SaveChangesAsync();
        return (plan, outcome, objective, priority, programme, project, kpi);
    }
}

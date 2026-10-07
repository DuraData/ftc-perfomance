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

        var denied = Extract<IdpDashboardResponse>((await controller.GetDashboard(graph.Plan.Id)).Result!);
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

        var visible = Extract<IdpDashboardResponse>((await controller.GetDashboard(graph.Plan.Id)).Result!);
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

        var commentRequest = new CreateIdpCommentRequest(graph.Plan.Id, null, "IdpProject", graph.Project.PublicId.ToString(), "protected review comment");
        (await controller.CreateComment(commentRequest)).Result.Should().BeOfType<ForbidResult>();
        context.IdpCollaborationComments.Should().BeEmpty();
        allowed.Add("IDP_PLAN.CollaborationComment.UPDATE");
        var maskedComment = Extract<IdpCommentResponse>((await controller.CreateComment(commentRequest)).Result!);
        maskedComment.EntityName.Should().BeNull();
        maskedComment.EntityId.Should().BeNull();
        maskedComment.Comment.Should().BeNull();
        maskedComment.CommentedByUserPublicId.Should().BeNull();
        maskedComment.CommentedByName.Should().BeNull();

        var taskRequest = new CreateIdpTaskRequest(graph.Plan.Id, null, "Protected task", "Protected task detail", assignee.Id, DateTime.UtcNow.AddDays(2));
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

        (await controller.CreatePlanVersion(graph.Plan.Id, request)).Result.Should().BeOfType<ForbidResult>();
        context.IdpPlanVersions.Should().BeEmpty();

        allowed.Add("IDP_PLAN.VersionSummary.UPDATE");
        var maskedMutation = Extract<IdpPlanVersionResponse>((await controller.CreatePlanVersion(graph.Plan.Id, request)).Result!);
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

    private static T Extract<T>(ActionResult result) where T : class
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeOfType<ApiResponse<T>>().Subject.Data!;
    }

    private static IdpController Controller(
        ApplicationDbContext context,
        ApplicationUser actor,
        HashSet<string> allowed,
        ApplicationUser? assignee = null)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(allowed.Contains(code), allowed.Contains(code) ? "allowed" : "denied", [], [], []));
        var users = new Dictionary<string, ApplicationUser> { [actor.Id] = actor };
        if (assignee != null) users[assignee.Id] = assignee;
        return IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(actor, users).Object,
            Mock.Of<IWorkflowGovernanceService>(), actor.Id, accessControl: access.Object);
    }

    private static async Task<(IdpPlan Plan, IdpProject Project, IdpKpi Kpi)> SeedGraphAsync(
        ApplicationDbContext context,
        ApplicationUser actor,
        ApplicationUser? secondUser = null)
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
            CreatedByUserId = actor.Id
        };
        var outcome = new IdpStrategicOutcome { IdpPlan = plan, Code = "SO1", Name = "Outcome", Description = "Outcome" };
        var objective = new IdpStrategicObjective { IdpStrategicOutcome = outcome, Code = "OBJ1", Name = "Objective", Description = "Objective", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1) };
        var priority = new IdpDevelopmentPriority { IdpStrategicObjective = objective, PriorityCode = "PRI1", Name = "Priority", Description = "Priority" };
        var programme = new IdpProgramme { IdpDevelopmentPriority = priority, ProgrammeCode = "PRG1", Name = "Programme", Description = "Programme" };
        var project = new IdpProject { IdpProgramme = programme, ProjectCode = "PRJ1", ProjectName = "Project", Description = "Project", Category = "Capital", FundingSource = "Grant", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1) };
        var kpi = new IdpKpi { IdpProject = project, KpiCode = "KPI1", KpiName = "KPI", Description = "KPI", Formula = "Count", DataSource = "System", ReportingFrequency = "Annual", IndicatorType = IdpKpiIndicatorType.Output };
        context.Add(kpi);
        await context.SaveChangesAsync();
        return (plan, project, kpi);
    }
}

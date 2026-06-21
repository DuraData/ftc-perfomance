namespace FTCERP.Tests;

public class IdpControllerFunctionalityTests
{
    [Fact]
    public async Task CreatePlan_ShouldCreatePlanAndInitialVersionAndAudit()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var request = new CreateIdpPlanRequest("Blue Hills", "Integrated Development Plan", "IDP-2026", 2026, 2031);
        var actionResult = await controller.CreatePlan(request);

        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<IdpPlanSummaryResponse>>().Subject;

        payload.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.PlanCode.Should().Be("IDP-2026");

        (await context.IdpPlans.CountAsync()).Should().Be(1);
        (await context.IdpPlanVersions.CountAsync()).Should().Be(1);

        workflow.Verify(w => w.WriteAuditTrailAsync(
            "IdpPlan",
            It.IsAny<string>(),
            "Create",
            null,
            It.IsAny<object>(),
            user.Id,
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlan_ShouldSetApprovedMetadata_WhenStatusApproved()
    {
        await using var context = IdpTestFixture.CreateContext();
        var creator = IdpTestFixture.CreateUser("creator");
        var approver = IdpTestFixture.CreateUser("approver", "Approver", "User");
        context.Users.AddRange(creator, approver);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "Original",
            PlanCode = "IDP-APPROVE",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = creator.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(approver);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, approver.Id);

        var result = await controller.UpdatePlan(plan.Id, new UpdateIdpPlanRequest("Updated", 2027, 2032, "Approved"));

        result.Result.Should().BeOfType<OkObjectResult>();
        var updated = await context.IdpPlans.SingleAsync();

        updated.PlanTitle.Should().Be("Updated");
        updated.Status.Should().Be(IdpPlanStatus.Approved);
        updated.ApprovedByUserId.Should().Be(approver.Id);
        updated.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePlanVersion_ShouldIncrementVersionAndDeactivatePrevious()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-VERS",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CurrentVersionNumber = 1,
            CreatedByUserId = user.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        context.IdpPlanVersions.Add(new IdpPlanVersion
        {
            IdpPlanId = plan.Id,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original",
            IsActive = true,
            CreatedByUserId = user.Id
        });
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var action = await controller.CreatePlanVersion(plan.Id, new CreateIdpPlanVersionRequest("AnnualReview", "Annual Review", "2026/2027", "Changes"));

        action.Result.Should().BeOfType<OkObjectResult>();

        var versions = await context.IdpPlanVersions.OrderBy(v => v.VersionNumber).ToListAsync();
        versions.Should().HaveCount(2);
        versions[0].IsActive.Should().BeFalse();
        versions[1].IsActive.Should().BeTrue();
        versions[1].VersionNumber.Should().Be(2);

        (await context.IdpPlans.SingleAsync()).CurrentVersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task CreateAndRetrieveHierarchy_ShouldReturnNestedRecords()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        var owner = IdpTestFixture.CreateUser("owner", "Owner", "Planner");
        context.Users.AddRange(user, owner);

        var dept = new Department { Id = 7, Code = "PLN", Name = "Planning", Description = "Planning" };
        context.Departments.Add(dept);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-HIER",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var outcome = new IdpStrategicOutcome { IdpPlanId = plan.Id, Code = "SO1", Name = "Outcome", Description = "Desc", SortOrder = 1 };
        context.IdpStrategicOutcomes.Add(outcome);
        await context.SaveChangesAsync();

        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcomeId = outcome.Id,
            Code = "OBJ1",
            Name = "Objective",
            Description = "Desc",
            BaselineValue = 10,
            TargetValue = 20,
            ResponsibleDepartmentId = dept.Id,
            StrategicOwnerUserId = owner.Id,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            BudgetAllocation = 1000,
            SortOrder = 1
        };
        context.IdpStrategicObjectives.Add(objective);
        await context.SaveChangesAsync();

        var priority = new IdpDevelopmentPriority { IdpStrategicObjectiveId = objective.Id, Name = "Priority", Description = "Desc", SortOrder = 1 };
        context.IdpDevelopmentPriorities.Add(priority);
        await context.SaveChangesAsync();

        var programme = new IdpProgramme { IdpDevelopmentPriorityId = priority.Id, ProgrammeCode = "PRG1", Name = "Programme", Description = "Desc", PlannedBudget = 100, ApprovedBudget = 90, ActualExpenditure = 80, ResponsibleDepartmentId = dept.Id };
        context.IdpProgrammes.Add(programme);
        await context.SaveChangesAsync();

        var project = new IdpProject { IdpProgrammeId = programme.Id, ProjectCode = "PROJ1", ProjectName = "Project", Description = "Desc", Category = "Infrastructure", Budget = 200, FundingSource = "Grant", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(6), Status = IdpProjectStatus.Planned, DepartmentId = dept.Id };
        context.IdpProjects.Add(project);
        await context.SaveChangesAsync();

        var kpi = new IdpKpi { IdpProjectId = project.Id, KpiCode = "KPI1", KpiName = "KPI", Description = "Desc", Formula = "x/y", Baseline = 1, AnnualTarget = 2, FiveYearTarget = 3, ResponsibleDepartmentId = dept.Id, DataSource = "System", ReportingFrequency = "Quarterly", IndicatorType = IdpKpiIndicatorType.Outcome, Circular88Linked = true, TreasuryTidLinked = true };
        context.IdpKpis.Add(kpi);
        await context.SaveChangesAsync();

        context.IdpAnnualTargets.Add(new IdpAnnualTarget { IdpKpiId = kpi.Id, FinancialYear = 2026, TargetValue = 2, ActualValue = 1.8m, ProgressComment = "On track" });
        context.IdpAlignmentLinks.Add(new IdpAlignmentLink { IdpStrategicObjectiveId = objective.Id, FrameworkType = AlignmentFrameworkType.NationalDevelopmentPlan, FrameworkReferenceCode = "NDP-1", FrameworkReferenceTitle = "NDP Ref" });
        context.IdpRiskLinks.Add(new IdpRiskLink { IdpStrategicObjectiveId = objective.Id, RiskReference = "R1", RiskTitle = "Funding risk", RiskLevel = IdpRiskLevel.High });
        context.IdpBudgetSnapshots.Add(new IdpBudgetSnapshot { IdpStrategicObjectiveId = objective.Id, FinancialYear = 2026, PlannedBudget = 1000, ApprovedBudget = 900, ActualExpenditure = 500, SourceSystem = "FMS" });
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var action = await controller.GetHierarchy(plan.Id);
        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<IdpHierarchyResponse>>().Subject;

        payload.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Outcomes.Should().HaveCount(1);
        payload.Data.Objectives.Should().HaveCount(1);
        payload.Data.Projects.Should().HaveCount(1);
        payload.Data.Kpis.Should().HaveCount(1);
        payload.Data.AnnualTargets.Should().HaveCount(1);
        payload.Data.AlignmentLinks.Should().HaveCount(1);
        payload.Data.RiskLinks.Should().HaveCount(1);
        payload.Data.BudgetSnapshots.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnComputedMetrics()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);

        var ward = new Ward { Id = 9, Code = "W9", Name = "Ward 9", Municipality = "Blue Hills", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Wards.Add(ward);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-DASH",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var outcome = new IdpStrategicOutcome { IdpPlanId = plan.Id, Code = "SO1", Name = "Outcome", Description = "Desc", SortOrder = 1 };
        context.IdpStrategicOutcomes.Add(outcome);
        await context.SaveChangesAsync();

        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcomeId = outcome.Id,
            Code = "OBJ1",
            Name = "Objective",
            Description = "Desc",
            BaselineValue = 10,
            TargetValue = 20,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            BudgetAllocation = 1000,
            SortOrder = 1
        };
        context.IdpStrategicObjectives.Add(objective);
        await context.SaveChangesAsync();

        var priority = new IdpDevelopmentPriority { IdpStrategicObjectiveId = objective.Id, Name = "Priority", Description = "Desc", SortOrder = 1 };
        context.IdpDevelopmentPriorities.Add(priority);
        await context.SaveChangesAsync();

        var programme = new IdpProgramme { IdpDevelopmentPriorityId = priority.Id, ProgrammeCode = "PRG1", Name = "Programme", Description = "Desc", PlannedBudget = 100, ApprovedBudget = 100, ActualExpenditure = 60 };
        context.IdpProgrammes.Add(programme);
        await context.SaveChangesAsync();

        var project = new IdpProject { IdpProgrammeId = programme.Id, ProjectCode = "PROJ1", ProjectName = "Project", Description = "Desc", Category = "Infra", Budget = 100, FundingSource = "Grant", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(6), Status = IdpProjectStatus.InProgress };
        context.IdpProjects.Add(project);
        await context.SaveChangesAsync();

        var kpi = new IdpKpi { IdpProjectId = project.Id, KpiCode = "KPI1", KpiName = "KPI", Description = "Desc", Formula = "x", Baseline = 1, AnnualTarget = 10, FiveYearTarget = 50, DataSource = "System", ReportingFrequency = "Quarterly", IndicatorType = IdpKpiIndicatorType.Output };
        context.IdpKpis.Add(kpi);
        await context.SaveChangesAsync();

        context.IdpAnnualTargets.AddRange(
            new IdpAnnualTarget { IdpKpiId = kpi.Id, FinancialYear = 2026, TargetValue = 10, ActualValue = 12 },
            new IdpAnnualTarget { IdpKpiId = kpi.Id, FinancialYear = 2027, TargetValue = 10, ActualValue = 8 });

        context.IdpRiskLinks.Add(new IdpRiskLink { IdpStrategicObjectiveId = objective.Id, RiskReference = "R1", RiskTitle = "Funding Risk", RiskLevel = IdpRiskLevel.Critical });
        context.IdpBudgetSnapshots.Add(new IdpBudgetSnapshot { IdpProjectId = project.Id, FinancialYear = 2026, PlannedBudget = 1000, ApprovedBudget = 900, ActualExpenditure = 450, SourceSystem = "FMS" });

        var session = new IdpCommunitySession
        {
            IdpPlanId = plan.Id,
            ParticipationType = IdpParticipationType.PublicMeeting,
            SessionDate = DateTime.UtcNow,
            Venue = "Hall",
            WardId = ward.Id,
            ParticipantsCount = 100
        };
        context.IdpCommunitySessions.Add(session);
        await context.SaveChangesAsync();

        context.IdpCommunityNeeds.Add(new IdpCommunityNeed
        {
            IdpCommunitySessionId = session.Id,
            IssueCategory = "Water",
            Description = "Need access",
            PriorityLevel = "High"
        });

        context.IdpAlignmentLinks.Add(new IdpAlignmentLink
        {
            IdpStrategicObjectiveId = objective.Id,
            FrameworkType = AlignmentFrameworkType.NationalDevelopmentPlan,
            FrameworkReferenceCode = "NDP-1",
            FrameworkReferenceTitle = "NDP Ref"
        });

        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var action = await controller.GetDashboard(plan.Id);
        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<IdpDashboardResponse>>().Subject;

        payload.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Outcomes.Should().Be(1);
        payload.Data.Objectives.Should().Be(1);
        payload.Data.Projects.Should().Be(1);
        payload.Data.Kpis.Should().Be(1);
        payload.Data.CommunitySessions.Should().Be(1);
        payload.Data.Risks.Should().Be(1);
        payload.Data.TopRiskTitles.Should().Contain("Funding Risk");
        payload.Data.KpiAchievementRate.Should().Be(50);
        payload.Data.WardParticipation.Should().ContainSingle();
        payload.Data.AlignmentMatrix.Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateReport_ShouldSupportPdfExcelWordFormats()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-RPT",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var pdf = await controller.GenerateReport(plan.Id, "annual", "pdf");
        var excel = await controller.GenerateReport(plan.Id, "annual", "excel");
        var word = await controller.GenerateReport(plan.Id, "annual", "word");

        var pdfPayload = ((pdf.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;
        var excelPayload = ((excel.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;
        var wordPayload = ((word.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;

        pdfPayload.Data!.ContentType.Should().Be("application/pdf");
        excelPayload.Data!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        wordPayload.Data!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        pdfPayload.Data.Content.Should().NotBeEmpty();
        excelPayload.Data.Content.Should().NotBeEmpty();
        wordPayload.Data.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTask_AndCompleteTask_ShouldPersistAndNotify()
    {
        await using var context = IdpTestFixture.CreateContext();
        var creator = IdpTestFixture.CreateUser("creator");
        var assignee = IdpTestFixture.CreateUser("assignee", "Assigned", "Person");
        context.Users.AddRange(creator, assignee);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-TASK",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = creator.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var usersById = new Dictionary<string, ApplicationUser>
        {
            [creator.Id] = creator,
            [assignee.Id] = assignee
        };

        var userManager = IdpTestFixture.CreateUserManagerMock(creator, usersById);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, creator.Id);

        var createResult = await controller.CreateTask(new CreateIdpTaskRequest(
            plan.Id,
            null,
            "Review draft IDP",
            "Review and provide comments",
            assignee.Id,
            DateTime.UtcNow.AddDays(7)));

        createResult.Result.Should().BeOfType<OkObjectResult>();
        var task = await context.IdpTaskAssignments.SingleAsync();
        task.IsCompleted.Should().BeFalse();

        workflow.Verify(w => w.CreateNotificationAsync(
            assignee.Id,
            NotificationType.Submission,
            It.IsAny<string>(),
            It.IsAny<string>(),
            "IdpTask",
            task.Id.ToString()), Times.Once);

        var completeResult = await controller.CompleteTask(task.Id, new CompleteIdpTaskRequest(true));
        completeResult.Result.Should().BeOfType<OkObjectResult>();

        var updated = await context.IdpTaskAssignments.SingleAsync();
        updated.IsCompleted.Should().BeTrue();
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDomainRecords_ShouldReturnNotFound_WhenParentsMissing()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var projectResult = await controller.CreateProject(new CreateIdpProjectRequest(
            IdpProgrammeId: -1,
            ProjectCode: "P1",
            ProjectName: "Test",
            Description: "desc",
            Category: "cat",
            DepartmentId: null,
            Budget: 100,
            FundingSource: "Grant",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(1),
            Status: "Planned",
            CommunityNeedReference: null));

        var kpiResult = await controller.CreateKpi(new CreateIdpKpiRequest(
            IdpProjectId: -1,
            KpiCode: "K1",
            KpiName: "KPI",
            Description: "desc",
            Formula: "x",
            Baseline: 1,
            AnnualTarget: 2,
            FiveYearTarget: 3,
            ResponsibleDepartmentId: null,
            DataSource: "sys",
            ReportingFrequency: "Quarterly",
            IndicatorType: "Outcome",
            Circular88Linked: false,
            TreasuryTidLinked: false));

        projectResult.Result.Should().BeOfType<NotFoundObjectResult>();
        kpiResult.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}

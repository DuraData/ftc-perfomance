using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class PerformanceDashboardTests
{
    [Fact]
    public async Task Opms_dashboard_aggregates_only_the_intersection_of_authorized_targets_and_submissions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("opms-dashboard-user");
        context.Users.Add(user);
        context.OpmsTargets.AddRange(
            OpmsTarget("approved"),
            OpmsTarget("overdue"),
            OpmsTarget("approval-queue"),
            OpmsTarget("returned", withdrawn: true),
            OpmsTarget("outside"));
        context.OpmsSubmissions.AddRange(
            OpmsSubmission("approved-submission", "approved", "approved"),
            OpmsSubmission("overdue-submission", "overdue", "submitted", DateTime.UtcNow.AddDays(-1)),
            OpmsSubmission("approval-submission", "approval-queue", "verified"),
            OpmsSubmission("returned-submission", "returned", "returned_for_info"),
            OpmsSubmission("outside-submission", "outside", "approved"));
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(Scope(["approved", "overdue", "approval-queue", "returned"]));
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        var controller = Controller(context, access.Object, user.Id);

        var result = await controller.GetOpms();

        var response = Assert.IsType<ApiResponse<PerformanceDashboardResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(new PerformanceDashboardResponse(
            TotalTargets: 4,
            ActiveTargets: 3,
            CompletedTargets: 1,
            OverdueTargets: 1,
            AtRiskTargets: 1,
            OutstandingTargets: 1,
            DraftSubmissions: 0,
            SubmittedSubmissions: 1,
            ReturnedSubmissions: 1,
            ApprovedSubmissions: 1,
            PendingVerification: 1,
            PendingApproval: 1), response.Data);
    }

    [Fact]
    public async Task Opms_dashboard_uses_current_financial_year_reporting_window_and_governed_workflow_scope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var now = DateTime.UtcNow;
        var user = IdpTestFixture.CreateUser("opms-current-context-user");
        var municipality = new Municipality { Code = "ODASH", Name = "OPMS Dashboard Municipality" };
        var department = new Department { Municipality = municipality, Code = "ROADS", Name = "Roads" };
        var financialYear = new FinancialYear
        {
            Code = "2026/27", Name = "2026/2027 Financial Year", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30)
        };
        var municipalityYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, IsActive = true, EffectiveFrom = financialYear.StartDate
        };
        var q1 = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1, StartDate = financialYear.StartDate, EndDate = new(2026, 9, 30)
        };
        var q2 = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q2", Name = "Quarter 2", PeriodType = ReportingPeriodType.Quarter2,
            Sequence = 2, StartDate = new(2026, 10, 1), EndDate = new(2026, 12, 31)
        };
        context.AddRange(user, municipality, department, financialYear, municipalityYear, q1, q2);
        await context.SaveChangesAsync();

        var targets = new[] { OpmsTarget("opms-achieved"), OpmsTarget("opms-at-risk"), OpmsTarget("opms-draft"), OpmsTarget("opms-verification") };
        foreach (var target in targets)
        {
            target.MunicipalityId = municipality.Id;
            target.DepartmentId = department.Id;
        }
        context.OpmsTargets.AddRange(targets);
        await context.SaveChangesAsync();
        foreach (var target in targets)
        {
            context.PerformancePeriodTargets.AddRange(
                new PerformancePeriodTarget { MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, OpmsTargetId = target.Id, TargetValue = "10", CreatedByUserId = user.Id },
                new PerformancePeriodTarget { MunicipalityId = municipality.Id, ReportingPeriodId = q2.Id, OpmsTargetId = target.Id, TargetValue = "20", CreatedByUserId = user.Id });
        }

        var achievedSubmission = OpmsSubmission("opms-achieved-submission", "opms-achieved", "completed");
        achievedSubmission.MunicipalityId = municipality.Id;
        achievedSubmission.ReportingPeriodId = q1.Id;
        achievedSubmission.BaseState = SubmissionBaseStates.Submitted;
        var atRiskSubmission = OpmsSubmission("opms-at-risk-submission", "opms-at-risk", "verify_rejected");
        atRiskSubmission.MunicipalityId = municipality.Id;
        atRiskSubmission.ReportingPeriodId = q1.Id;
        atRiskSubmission.BaseState = SubmissionBaseStates.Submitted;
        var draftSubmission = OpmsSubmission("opms-draft-submission", "opms-draft", "draft");
        draftSubmission.MunicipalityId = municipality.Id;
        draftSubmission.ReportingPeriodId = q1.Id;
        var verificationSubmission = OpmsSubmission("opms-verification-submission", "opms-verification", "submitted");
        verificationSubmission.MunicipalityId = municipality.Id;
        verificationSubmission.ReportingPeriodId = q1.Id;
        verificationSubmission.BaseState = SubmissionBaseStates.Submitted;
        context.OpmsSubmissions.AddRange(achievedSubmission, atRiskSubmission, draftSubmission, verificationSubmission);

        var workflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id, SubmissionKind = SubmissionKind.Opms,
            Code = "OPMS-DASH", Name = "OPMS dashboard workflow", EffectiveFrom = now.AddDays(-30)
        };
        var verifyStage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id, WorkflowDefinition = workflow, Code = "VERIFY", Name = "Verify", Sequence = 1,
            RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY", IsTerminal = true
        };
        var workflowInstance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinition = workflow, CurrentStage = verifyStage, SubmissionKind = SubmissionKind.Opms,
            SubmissionId = verificationSubmission.Id, State = WorkflowInstanceState.Active
        };
        context.AddRange(
            new ReportingWindow { MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, SubmissionKind = SubmissionKind.Opms, OpensAt = now.AddDays(-2), ClosesAt = now.AddDays(2) },
            new ReportingWindow { MunicipalityId = municipality.Id, ReportingPeriodId = q2.Id, SubmissionKind = SubmissionKind.Opms, OpensAt = now.AddDays(30), ClosesAt = now.AddDays(60) },
            workflow, verifyStage, workflowInstance);
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(Scope(targets.Select(item => item.Id).ToArray()));
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));

        var result = await Controller(context, access.Object, user.Id).GetOpms();

        var response = Assert.IsType<ApiResponse<PerformanceDashboardResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(municipalityYear.PublicId, response.MunicipalityFinancialYearPublicId);
        Assert.Equal("2026/27", response.FinancialYearCode);
        Assert.Equal(q1.PublicId, response.ReportingPeriodPublicId);
        Assert.Equal("Open", response.ReportingWindowState);
        Assert.Equal(4, response.TotalTargets);
        Assert.Equal(1, response.CompletedTargets);
        Assert.Equal(1, response.AtRiskTargets);
        Assert.Equal(1, response.OutstandingTargets);
        Assert.Equal(1, response.PendingVerification);
        var team = Assert.Single(response.TeamBreakdown!);
        Assert.Equal("Roads", team.DepartmentName);
        Assert.Equal((4, 1, 1), (team.TargetCount, team.AchievedCount, team.AtRiskCount));
        Assert.Collection(response.PeriodBreakdown!,
            period => Assert.Equal(("Q1", "Open", 4, 1), (period.Code, period.WindowState, period.SubmissionCount, period.OutstandingCount)),
            period => Assert.Equal(("Q2", "Upcoming", 0, 4), (period.Code, period.WindowState, period.SubmissionCount, period.OutstandingCount)));
    }

    [Fact]
    public async Task Ipms_dashboard_applies_dynamic_scope_and_counts_targets_without_register_downloads()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("ipms-dashboard-user");
        context.Users.Add(user);
        context.IpmsTargets.AddRange(
            IpmsTarget("achieved"),
            IpmsTarget("at-risk"),
            IpmsTarget("no-submission"),
            IpmsTarget("submitted"),
            IpmsTarget("outside"));
        context.IpmsSubmissions.AddRange(
            IpmsSubmission("approved-submission", "achieved", "approved"),
            IpmsSubmission("returned-submission", "at-risk", "verify_rejected"),
            IpmsSubmission("submitted-submission", "submitted", "submitted"),
            IpmsSubmission("outside-submission", "outside", "approved"));
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_KPI.READ"))
            .ReturnsAsync(Scope(["achieved", "at-risk", "no-submission", "submitted"]));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        var controller = Controller(context, access.Object, user.Id);

        var result = await controller.GetIpms();

        var response = Assert.IsType<ApiResponse<PerformanceDashboardResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(new PerformanceDashboardResponse(
            TotalTargets: 4,
            ActiveTargets: 4,
            CompletedTargets: 1,
            OverdueTargets: 0,
            AtRiskTargets: 1,
            OutstandingTargets: 2,
            DraftSubmissions: 0,
            SubmittedSubmissions: 1,
            ReturnedSubmissions: 1,
            ApprovedSubmissions: 1,
            PendingVerification: 1,
            PendingApproval: 0), response.Data);
    }

    [Fact]
    public async Task Ipms_dashboard_uses_current_financial_year_reporting_window_and_governed_workflow_scope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var now = DateTime.UtcNow;
        var user = IdpTestFixture.CreateUser("ipms-current-context-user");
        var municipality = new Municipality { Code = "DASH", Name = "Dashboard Municipality" };
        var department = new Department { Municipality = municipality, Code = "CORP", Name = "Corporate Services" };
        var financialYear = new FinancialYear
        {
            Code = "2026/27", Name = "2026/2027 Financial Year", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30)
        };
        var municipalityYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, IsActive = true, EffectiveFrom = financialYear.StartDate
        };
        var q1 = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1, StartDate = financialYear.StartDate, EndDate = new(2026, 9, 30)
        };
        var q2 = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q2", Name = "Quarter 2", PeriodType = ReportingPeriodType.Quarter2,
            Sequence = 2, StartDate = new(2026, 10, 1), EndDate = new(2026, 12, 31)
        };
        context.AddRange(user, municipality, department, financialYear, municipalityYear, q1, q2);
        await context.SaveChangesAsync();

        var targets = new[] { IpmsTarget("achieved"), IpmsTarget("at-risk"), IpmsTarget("draft"), IpmsTarget("verification") };
        foreach (var target in targets)
        {
            target.MunicipalityId = municipality.Id;
            target.DepartmentId = department.Id;
        }
        context.IpmsTargets.AddRange(targets);
        await context.SaveChangesAsync();
        foreach (var target in targets)
        {
            context.PerformancePeriodTargets.AddRange(
                new PerformancePeriodTarget { MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, IpmsTargetId = target.Id, TargetValue = "10", CreatedByUserId = user.Id },
                new PerformancePeriodTarget { MunicipalityId = municipality.Id, ReportingPeriodId = q2.Id, IpmsTargetId = target.Id, TargetValue = "20", CreatedByUserId = user.Id });
        }

        var achievedSubmission = IpmsSubmission("achieved-submission", "achieved", "completed");
        achievedSubmission.MunicipalityId = municipality.Id;
        achievedSubmission.ReportingPeriodId = q1.Id;
        achievedSubmission.BaseState = SubmissionBaseStates.Submitted;
        var atRiskSubmission = IpmsSubmission("at-risk-submission", "at-risk", "verify_rejected");
        atRiskSubmission.MunicipalityId = municipality.Id;
        atRiskSubmission.ReportingPeriodId = q1.Id;
        atRiskSubmission.BaseState = SubmissionBaseStates.Submitted;
        var draftSubmission = IpmsSubmission("draft-submission", "draft", "draft");
        draftSubmission.MunicipalityId = municipality.Id;
        draftSubmission.ReportingPeriodId = q1.Id;
        var verificationSubmission = IpmsSubmission("verification-submission", "verification", "submitted");
        verificationSubmission.MunicipalityId = municipality.Id;
        verificationSubmission.ReportingPeriodId = q1.Id;
        verificationSubmission.BaseState = SubmissionBaseStates.Submitted;
        context.IpmsSubmissions.AddRange(achievedSubmission, atRiskSubmission, draftSubmission, verificationSubmission);

        var workflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id, SubmissionKind = SubmissionKind.Ipms,
            Code = "IPMS-DASH", Name = "IPMS dashboard workflow", EffectiveFrom = now.AddDays(-30)
        };
        var verifyStage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id, WorkflowDefinition = workflow, Code = "VERIFY", Name = "Verify", Sequence = 1,
            RequiredActionCode = "IPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "IPMS_SUBMISSION.VERIFY", IsTerminal = true
        };
        var workflowInstance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinition = workflow, CurrentStage = verifyStage, SubmissionKind = SubmissionKind.Ipms,
            SubmissionId = verificationSubmission.Id, State = WorkflowInstanceState.Active
        };
        context.AddRange(
            new ReportingWindow { MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, SubmissionKind = SubmissionKind.Ipms, OpensAt = now.AddDays(-2), ClosesAt = now.AddDays(2) },
            new ReportingWindow { MunicipalityId = municipality.Id, ReportingPeriodId = q2.Id, SubmissionKind = SubmissionKind.Ipms, OpensAt = now.AddDays(30), ClosesAt = now.AddDays(60) },
            workflow, verifyStage, workflowInstance);
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_KPI.READ"))
            .ReturnsAsync(Scope(targets.Select(item => item.Id).ToArray()));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));

        var result = await Controller(context, access.Object, user.Id).GetIpms();

        var response = Assert.IsType<ApiResponse<PerformanceDashboardResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(municipalityYear.PublicId, response.MunicipalityFinancialYearPublicId);
        Assert.Equal("2026/27", response.FinancialYearCode);
        Assert.Equal(q1.PublicId, response.ReportingPeriodPublicId);
        Assert.Equal("Open", response.ReportingWindowState);
        Assert.Equal(4, response.TotalTargets);
        Assert.Equal(1, response.CompletedTargets);
        Assert.Equal(1, response.AtRiskTargets);
        Assert.Equal(1, response.OutstandingTargets);
        Assert.Equal(1, response.PendingVerification);
        var team = Assert.Single(response.TeamBreakdown!);
        Assert.Equal("Corporate Services", team.DepartmentName);
        Assert.Equal((4, 1, 1), (team.TargetCount, team.AchievedCount, team.AtRiskCount));
        Assert.Collection(response.PeriodBreakdown!,
            period => Assert.Equal(("Q1", "Open", 4, 1), (period.Code, period.WindowState, period.SubmissionCount, period.OutstandingCount)),
            period => Assert.Equal(("Q2", "Upcoming", 0, 4), (period.Code, period.WindowState, period.SubmissionCount, period.OutstandingCount)));
    }

    private static PerformanceDashboardsController Controller(
        ApplicationDbContext context,
        IAccessControlService access,
        string userId) => new(context, access)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(userId) }
            }
        };

    private static AccessQueryScopeResult Scope(string[] targetIds) =>
        new(true, false, [], [], [], targetIds, [], []);

    private static OpmsTarget OpmsTarget(string id, bool withdrawn = false) => new()
    {
        Id = id,
        IndicatorNumber = id,
        TargetName = id,
        KpiDescription = id,
        AnnualTargetDescription = id,
        IsWithdrawn = withdrawn,
        ReasonForWithdrawal = withdrawn ? "Superseded for dashboard test" : null,
        WithdrawnAt = withdrawn ? DateTime.UtcNow : null,
        WithdrawnByUserId = withdrawn ? "opms-dashboard-user" : null
    };

    private static IpmsTarget IpmsTarget(string id) => new()
    {
        Id = id,
        IndicatorNumber = id,
        TargetName = id,
        KpiDescription = id,
        AnnualTargetDescription = id
    };

    private static OpmsSubmission OpmsSubmission(string id, string targetId, string status, DateTime? dueDate = null) => new()
    {
        Id = id,
        OpmsTargetId = targetId,
        Quarter = "Q1",
        Status = status,
        DueDate = dueDate
    };

    private static IpmsSubmission IpmsSubmission(string id, string targetId, string status) => new()
    {
        Id = id,
        IpmsTargetId = targetId,
        Quarter = "Q1",
        Status = status
    };
}

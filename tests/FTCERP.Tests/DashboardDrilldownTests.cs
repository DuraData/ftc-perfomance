using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class DashboardDrilldownTests
{
    [Fact]
    public async Task Ipms_dashboard_drilldowns_reuse_financial_year_period_outcome_and_dynamic_scope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("dashboard-drilldown-user");
        var municipality = new Municipality { Code = "DRILL", Name = "Drilldown Municipality" };
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/2027", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        var municipalityYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, EffectiveFrom = financialYear.StartDate
        };
        var q1 = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1, StartDate = financialYear.StartDate, EndDate = new(2026, 9, 30)
        };
        context.AddRange(user, municipality, financialYear, municipalityYear, q1);
        await context.SaveChangesAsync();

        var achieved = Target("drill-achieved", "IPMS-ACHIEVED");
        var outstanding = Target("drill-outstanding", "IPMS-OUTSTANDING");
        achieved.MunicipalityId = municipality.Id;
        outstanding.MunicipalityId = municipality.Id;
        context.IpmsTargets.AddRange(achieved, outstanding);
        await context.SaveChangesAsync();
        context.PerformancePeriodTargets.AddRange(
            PeriodTarget(municipality.Id, q1.Id, achieved.Id, user.Id),
            PeriodTarget(municipality.Id, q1.Id, outstanding.Id, user.Id));
        context.IpmsSubmissions.AddRange(
            new IpmsSubmission
            {
                Id = "drill-completed", MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, IpmsTargetId = achieved.Id,
                Quarter = "Q1", Status = "completed", BaseState = SubmissionBaseStates.Submitted
            },
            new IpmsSubmission
            {
                Id = "drill-draft", MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, IpmsTargetId = outstanding.Id,
                Quarter = "Q1", Status = "draft", BaseState = SubmissionBaseStates.InProgress
            });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        access.Setup(service => service.GetEffectiveAccessAsync(user))
            .ReturnsAsync(new EffectiveAccessResult([], [], [], [], [], []));

        var targets = new IpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            new TenantContext(municipality.Id, user.Id),
            new PerformanceUnitEngine())
        { ControllerContext = ControllerContext(user.Id) };
        var submissions = new IpmsSubmissionsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<IEvidenceBlobStorage>(),
            Mock.Of<ISubmissionValueService>(),
            Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(),
            Mock.Of<IEvidenceInspectionService>(),
            Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        { ControllerContext = ControllerContext(user.Id) };

        var achievedResult = await targets.GetTargetsPage(Query(municipalityYear.PublicId, q1.PublicId, "achieved"));
        var achievedPage = Assert.IsType<ApiResponse<PagedResponse<IpmsTargetResponse>>>(Assert.IsType<OkObjectResult>(achievedResult.Result).Value).Data!;
        Assert.Equal(achieved.PublicId, Assert.Single(achievedPage.Items).PublicId);

        var outstandingResult = await targets.GetTargetsPage(Query(municipalityYear.PublicId, q1.PublicId, "outstanding"));
        var outstandingPage = Assert.IsType<ApiResponse<PagedResponse<IpmsTargetResponse>>>(Assert.IsType<OkObjectResult>(outstandingResult.Result).Value).Data!;
        Assert.Equal(outstanding.PublicId, Assert.Single(outstandingPage.Items).PublicId);

        var approvedResult = await submissions.GetSubmissionsPage(Query(municipalityYear.PublicId, q1.PublicId, "approved"));
        var approvedPage = Assert.IsType<ApiResponse<PagedResponse<IpmsSubmissionResponse>>>(Assert.IsType<OkObjectResult>(approvedResult.Result).Value).Data!;
        Assert.Equal("drill-completed", Assert.Single(approvedPage.Items).Id);

        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var deniedTargetIntersection = await submissions.GetSubmissionsPage(Query(municipalityYear.PublicId, q1.PublicId, "approved"));
        var deniedPage = Assert.IsType<ApiResponse<PagedResponse<IpmsSubmissionResponse>>>(Assert.IsType<OkObjectResult>(deniedTargetIntersection.Result).Value).Data!;
        Assert.Empty(deniedPage.Items);
    }

    [Fact]
    public async Task Ipms_dashboard_drilldown_rejects_a_filter_without_a_governed_period()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("dashboard-filter-validation-user");
        context.Add(user);
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        var controller = new IpmsTargetsController(
            context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, Mock.Of<IWorkflowGovernanceService>(),
            new TenantContext(1, user.Id), new PerformanceUnitEngine())
        { ControllerContext = ControllerContext(user.Id) };

        var result = await controller.GetTargetsPage(new PagedQueryRequest { DashboardFilter = "achieved" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        access.Verify(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Opms_dashboard_drilldowns_reuse_financial_year_period_outcome_and_dynamic_scope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("opms-dashboard-drilldown-user");
        var municipality = new Municipality { Code = "ODRILL", Name = "OPMS Drilldown Municipality" };
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/2027", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        var municipalityYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, EffectiveFrom = financialYear.StartDate
        };
        var q1 = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1, StartDate = financialYear.StartDate, EndDate = new(2026, 9, 30)
        };
        context.AddRange(user, municipality, financialYear, municipalityYear, q1);
        await context.SaveChangesAsync();

        var achieved = OpmsTarget("opms-drill-achieved", "OPMS-ACHIEVED", municipality.Id);
        var outstanding = OpmsTarget("opms-drill-outstanding", "OPMS-OUTSTANDING", municipality.Id);
        context.OpmsTargets.AddRange(achieved, outstanding);
        await context.SaveChangesAsync();
        context.PerformancePeriodTargets.AddRange(
            OpmsPeriodTarget(municipality.Id, q1.Id, achieved.Id, user.Id),
            OpmsPeriodTarget(municipality.Id, q1.Id, outstanding.Id, user.Id));
        context.OpmsSubmissions.AddRange(
            new OpmsSubmission
            {
                Id = "opms-drill-completed", MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, OpmsTargetId = achieved.Id,
                Quarter = "Q1", Status = "completed", BaseState = SubmissionBaseStates.Submitted
            },
            new OpmsSubmission
            {
                Id = "opms-drill-draft", MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, OpmsTargetId = outstanding.Id,
                Quarter = "Q1", Status = "draft", BaseState = SubmissionBaseStates.InProgress
            });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        access.Setup(service => service.GetEffectiveAccessAsync(user))
            .ReturnsAsync(new EffectiveAccessResult([], [], [], [], [], []));

        var targets = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            new TenantContext(municipality.Id, user.Id),
            new PerformanceUnitEngine())
        { ControllerContext = ControllerContext(user.Id) };
        var submissions = new OpmsSubmissionsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<IEvidenceBlobStorage>(),
            Mock.Of<ISubmissionValueService>(),
            Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(),
            Mock.Of<IEvidenceInspectionService>(),
            Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        { ControllerContext = ControllerContext(user.Id) };

        var achievedResult = await targets.GetTargetsPage(Query(municipalityYear.PublicId, q1.PublicId, "achieved"));
        var achievedPage = Assert.IsType<ApiResponse<PagedResponse<OpmsTargetResponse>>>(Assert.IsType<OkObjectResult>(achievedResult.Result).Value).Data!;
        Assert.Equal(achieved.PublicId, Assert.Single(achievedPage.Items).PublicId);

        var outstandingResult = await targets.GetTargetsPage(Query(municipalityYear.PublicId, q1.PublicId, "outstanding"));
        var outstandingPage = Assert.IsType<ApiResponse<PagedResponse<OpmsTargetResponse>>>(Assert.IsType<OkObjectResult>(outstandingResult.Result).Value).Data!;
        Assert.Equal(outstanding.PublicId, Assert.Single(outstandingPage.Items).PublicId);

        var approvedResult = await submissions.GetSubmissionsPage(Query(municipalityYear.PublicId, q1.PublicId, "approved"));
        var approvedPage = Assert.IsType<ApiResponse<PagedResponse<OpmsSubmissionResponse>>>(Assert.IsType<OkObjectResult>(approvedResult.Result).Value).Data!;
        Assert.Equal("opms-drill-completed", Assert.Single(approvedPage.Items).Id);

        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var deniedTargetIntersection = await submissions.GetSubmissionsPage(Query(municipalityYear.PublicId, q1.PublicId, "approved"));
        var deniedPage = Assert.IsType<ApiResponse<PagedResponse<OpmsSubmissionResponse>>>(Assert.IsType<OkObjectResult>(deniedTargetIntersection.Result).Value).Data!;
        Assert.Empty(deniedPage.Items);
    }

    [Fact]
    public async Task Opms_dashboard_drilldown_rejects_a_filter_without_a_governed_period()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("opms-dashboard-filter-validation-user");
        context.Add(user);
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        var controller = new OpmsTargetsController(
            context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, Mock.Of<IWorkflowGovernanceService>(),
            new TenantContext(1, user.Id), new PerformanceUnitEngine())
        { ControllerContext = ControllerContext(user.Id) };

        var result = await controller.GetTargetsPage(new PagedQueryRequest { DashboardFilter = "achieved" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        access.Verify(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    private static PagedQueryRequest Query(Guid year, Guid period, string filter) => new()
    {
        Page = 1,
        PageSize = 25,
        MunicipalityFinancialYearPublicId = year,
        ReportingPeriodPublicId = period,
        DashboardFilter = filter
    };

    private static IpmsTarget Target(string id, string indicator) => new()
    {
        Id = id,
        IndicatorNumber = indicator,
        TargetName = indicator,
        KpiDescription = indicator,
        AnnualTargetDescription = indicator
    };

    private static OpmsTarget OpmsTarget(string id, string indicator, long municipalityId) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        IndicatorNumber = indicator,
        TargetName = indicator,
        KpiDescription = indicator,
        AnnualTargetDescription = indicator
    };

    private static PerformancePeriodTarget PeriodTarget(long municipalityId, long reportingPeriodId, string targetId, string userId) => new()
    {
        MunicipalityId = municipalityId,
        ReportingPeriodId = reportingPeriodId,
        IpmsTargetId = targetId,
        TargetValue = "10",
        CreatedByUserId = userId
    };

    private static PerformancePeriodTarget OpmsPeriodTarget(long municipalityId, long reportingPeriodId, string targetId, string userId) => new()
    {
        MunicipalityId = municipalityId,
        ReportingPeriodId = reportingPeriodId,
        OpmsTargetId = targetId,
        TargetValue = "10",
        CreatedByUserId = userId
    };

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(userId) }
    };

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => userId;
    }
}

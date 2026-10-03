using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class NormalizedTargetWriteCutoverTests
{
    [Fact]
    public async Task Ipms_create_uses_the_same_normalized_period_write_path()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = IpmsController(context, seed.User, seed.Municipality.Id);

        var result = await controller.CreateTarget(IpmsRequest(seed.LegacyPeriod.Id));

        var response = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(100m, response.AnnualTarget);
        context.ChangeTracker.Clear();
        Assert.Equal(0m, (await context.IpmsTargets.SingleAsync()).AnnualTarget);
        Assert.Equal(2, await context.PerformancePeriodTargets.CountAsync(item => item.IpmsTargetId == response.Id));
    }

    [Fact]
    public async Task Opms_create_persists_normalized_period_rows_without_writing_legacy_wide_values()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);

        var result = await controller.CreateTarget(Request(seed.LegacyPeriod.Id));

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(100m, response.AnnualTarget);
        Assert.Equal(20m, response.Q1Target);
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.SingleAsync();
        Assert.Equal(0m, stored.AnnualTarget);
        Assert.Null(stored.Q1Target);
        var normalized = await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).OrderBy(item => item.ReportingPeriod.Sequence).ToArrayAsync();
        Assert.Equal(2, normalized.Length);
        Assert.Contains(normalized, item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter1 && item.TargetValue == "20" && item.BudgetValue == 10m);
        Assert.Contains(normalized, item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual && item.TargetValue == "100");
    }

    [Fact]
    public async Task General_target_update_cannot_bypass_governed_period_revision_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(Request(seed.LegacyPeriod.Id))).Result).Value).Data!;
        var changed = Request(seed.LegacyPeriod.Id) with { AnnualTarget = 90m };

        var result = await controller.UpdateTarget(created.Id, changed);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<OpmsTargetResponse>>(conflict.Value);
        Assert.Contains("governed records", envelope.Message);
        Assert.Equal("100", (await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).SingleAsync(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)).TargetValue);
        Assert.Empty(await context.PerformanceTargetRevisions.ToArrayAsync());
    }

    [Fact]
    public async Task General_target_update_keeps_unchanged_normalized_values_and_updates_only_metadata()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(Request(seed.LegacyPeriod.Id))).Result).Value).Data!;

        var result = await controller.UpdateTarget(created.Id, Request(seed.LegacyPeriod.Id) with { TargetName = "Updated metadata" });

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal("Updated metadata", response.TargetName);
        Assert.Equal(100m, response.AnnualTarget);
        Assert.Equal(2, await context.PerformancePeriodTargets.CountAsync());
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.SingleAsync();
        Assert.Equal("Updated metadata", stored.TargetName);
        Assert.Equal(0m, stored.AnnualTarget);
    }

    [Fact]
    public async Task Create_fails_closed_when_governed_municipality_year_is_not_configured()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "NO-YEAR", Name = "No Year" };
        var user = IdpTestFixture.CreateUser("no-year-user");
        var period = new Period { Code = "FY", Name = "Legacy year", FiscalYear = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30), IsActive = true };
        context.AddRange(municipality, user, period);
        await context.SaveChangesAsync();

        var result = await Controller(context, user, municipality.Id).CreateTarget(Request(period.Id));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("No active municipality financial year", Assert.IsType<ApiResponse<OpmsTargetResponse>>(badRequest.Value).Message);
        Assert.Empty(await context.OpmsTargets.ToArrayAsync());
    }

    private static OpmsTargetsController Controller(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context, ApplicationUser user, long municipalityId)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => new AccessDecisionResult(true, "Allowed", [code], [], []));
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(service => service.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.UserId).Returns(user.Id);
        return new OpmsTargetsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, workflow.Object, tenant.Object, new PerformanceUnitEngine())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static IpmsTargetsController IpmsController(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context, ApplicationUser user, long municipalityId)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => new AccessDecisionResult(true, "Allowed", [code], [], []));
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(service => service.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.UserId).Returns(user.Id);
        return new IpmsTargetsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, workflow.Object, tenant.Object, new PerformanceUnitEngine())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static SaveOpmsTargetRequest Request(int periodId) => new(
        null, null, periodId, null, null, null, [], [], [], "OPMS-1", "National KPA", "Municipal KPA", null, null,
        "Objective", "Normalized target", "Description", 0m, null, 100m, "Annual target", null, null, null, 10m,
        "Quantitative", "Output", null, null, null, null, null, false, false, null, "percentage",
        20m, "Q1 target", 10m, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

    private static SaveIpmsTargetRequest IpmsRequest(int periodId) => new(
        SourceTemplateId: null,
        SourceTemplateVersion: null,
        RelatedOpmsTargetId: null,
        PeriodId: periodId,
        DepartmentId: null,
        UnitId: null,
        AssignedUserId: null,
        SupervisorId: null,
        IndicatorNumber: "IPMS-1",
        NationalKpa: "National KPA",
        MunicipalKpa: "Municipal KPA",
        StrategicGoalId: null,
        StrategicObjectiveId: null,
        PerformanceObjective: "Objective",
        TargetName: "Normalized individual target",
        KpiDescription: "Description",
        Baseline: 0m,
        AnnualTarget: 100m,
        AnnualTargetDescription: "Annual target",
        BudgetSourceId: null,
        BudgetTypeId: null,
        UnitOfMeasureId: null,
        Weight: 10m,
        KpiType: "Quantitative",
        IndicatorType: "Output",
        FunctionalArea: null,
        IdpReference: null,
        InternalReference: null,
        IsRevised: false,
        TargetUnitType: "percentage",
        Q1Target: 20m,
        Q1Description: "Q1 target",
        Q1Budget: 10m,
        Q2Target: null,
        Q2Description: null,
        Q2Budget: null,
        MidTermTarget: null,
        MidTermDescription: null,
        MidTermBudget: null,
        Q3Target: null,
        Q3Description: null,
        Q3Budget: null,
        Q3RevisedTarget: null,
        Q4Target: null,
        Q4Description: null,
        Q4Budget: null,
        Q4RevisedTarget: null,
        RevisedAnnualTarget: null,
        RevisedAnnualBudget: null);

    private static async Task<(Municipality Municipality, ApplicationUser User, Period LegacyPeriod)> SeedAsync(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "NORM", Name = "Normalized Municipality" };
        var user = IdpTestFixture.CreateUser("normalized-user");
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30), IsActive = true };
        var legacyPeriod = new Period { Code = "FY", Name = "2026/27", FiscalYear = financialYear.Code, StartDate = financialYear.StartDate, EndDate = financialYear.EndDate, IsActive = true };
        context.AddRange(municipality, user, financialYear, legacyPeriod);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, IsCurrent = true, IsActive = true, EffectiveFrom = financialYear.StartDate };
        context.MunicipalityFinancialYears.Add(municipalityYear);
        await context.SaveChangesAsync();
        context.ReportingPeriods.AddRange(
            new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 9, 30), IsActive = true },
            new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANN", Name = "Annual", PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = financialYear.StartDate, EndDate = financialYear.EndDate, IsActive = true });
        await context.SaveChangesAsync();
        return (municipality, user, legacyPeriod);
    }
}

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
    public void Public_target_contracts_expose_only_canonical_period_values()
    {
        var retired = new[]
        {
            "AnnualTarget", "AnnualTargetDescription", "TargetUnitType", "Q1Target", "Q1Description", "Q1Budget",
            "Q2Target", "Q2Description", "Q2Budget", "MidTermTarget", "MidTermDescription", "MidTermBudget",
            "Q3Target", "Q3Description", "Q3Budget", "Q3RevisedTarget", "Q4Target", "Q4Description", "Q4Budget",
            "Q4RevisedTarget", "RevisedAnnualTarget", "RevisedAnnualBudget"
        };

        foreach (var contract in new[] { typeof(SaveOpmsTargetRequest), typeof(SaveIpmsTargetRequest), typeof(OpmsTargetResponse), typeof(IpmsTargetResponse) })
        {
            Assert.NotNull(contract.GetProperty("PeriodTargets"));
            foreach (var property in retired)
                Assert.Null(contract.GetProperty(property));
        }
        Assert.Null(typeof(SaveOpmsTargetRequest).GetProperty("IsWithdrawn"));
        Assert.Null(typeof(SaveOpmsTargetRequest).GetProperty("ReasonForWithdrawal"));
    }

    [Fact]
    public async Task Ipms_create_uses_the_same_normalized_period_write_path()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = IpmsController(context, seed.User, seed.Municipality.Id);

        var result = await controller.CreateTarget(IpmsRequest(seed.LegacyPeriod.Id, seed.Classifications));

        var response = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == ReportingPeriodType.Annual && item.TargetValue == "100");
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

        var result = await controller.CreateTarget(Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId, seed.Classifications));

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == ReportingPeriodType.Annual && item.TargetValue == "100");
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == ReportingPeriodType.Quarter1 && item.TargetValue == "20");
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.SingleAsync();
        Assert.Equal(seed.SdbipLayer.Id, stored.SdbipLayerId);
        Assert.NotNull(stored.NationalKpaId);
        Assert.NotNull(stored.MunicipalKpaId);
        Assert.NotNull(stored.BackToBasicsPillarId);
        Assert.NotNull(stored.StrategicGoalMasterId);
        Assert.NotNull(stored.StrategicInterventionId);
        Assert.NotNull(stored.StrategicObjectiveMasterId);
        Assert.NotNull(stored.PerformanceObjectiveId);
        Assert.Null(stored.StrategicGoalId);
        Assert.Null(stored.StrategicObjectiveId);
        Assert.Equal(0m, stored.AnnualTarget);
        Assert.Null(stored.Q1Target);
        var normalized = await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).OrderBy(item => item.ReportingPeriod.Sequence).ToArrayAsync();
        Assert.Equal(2, normalized.Length);
        Assert.Contains(normalized, item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter1 && item.TargetValue == "20" && item.BudgetValue == 10m);
        Assert.Contains(normalized, item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual && item.TargetValue == "100");
    }

    [Fact]
    public async Task Canonical_contract_preserves_period_specific_units_and_non_numeric_values()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var request = Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            PeriodTargets =
            [
                new(ReportingPeriodType.Quarter1, PerformanceUnitKind.QualitativeTargets, PerformanceDirection.Exact, "Council approved", null, "Qualitative milestone"),
                new(ReportingPeriodType.Annual, PerformanceUnitKind.Date, PerformanceDirection.LowerIsBetter, "2027-06-30", null, "Completion date")
            ]
        };

        var result = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget(request);

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == ReportingPeriodType.Quarter1 && item.UnitKind == PerformanceUnitKind.QualitativeTargets && item.TargetValue == "Council approved");
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == ReportingPeriodType.Annual && item.UnitKind == PerformanceUnitKind.Date && item.TargetValue == "2027-06-30");
    }

    [Fact]
    public async Task General_target_update_cannot_bypass_governed_period_revision_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;
        var changed = Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId, seed.Classifications) with { PeriodTargets = PeriodTargets("90") };

        var result = await controller.UpdateTarget(created.Id, changed);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<OpmsTargetResponse>>(conflict.Value);
        Assert.Contains("governed records", envelope.Message);
        Assert.Equal("100", (await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).SingleAsync(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)).TargetValue);
        Assert.Empty(await context.PerformanceTargetRevisions.ToArrayAsync());
    }

    [Fact]
    public async Task General_target_update_keeps_governed_values_and_updates_only_non_revision_metadata()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;

        var result = await controller.UpdateTarget(created.Id, Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId, seed.Classifications) with { InternalReference = "Updated metadata" });

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal("Updated metadata", response.InternalReference);
        Assert.Equal("Normalized target", response.TargetName);
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == ReportingPeriodType.Annual && item.TargetValue == "100");
        Assert.Equal(2, await context.PerformancePeriodTargets.CountAsync());
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.SingleAsync();
        Assert.Equal("Updated metadata", stored.InternalReference);
        Assert.Equal("Normalized target", stored.TargetName);
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

    [Fact]
    public async Task Create_rejects_free_text_only_strategic_classification()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);

        var result = await Controller(context, seed.User, seed.Municipality.Id)
            .CreateTarget(Request(seed.LegacyPeriod.Id, seed.SdbipLayer.PublicId));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("governed strategic classifications", Assert.IsType<ApiResponse<OpmsTargetResponse>>(badRequest.Value).Message);
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

    private static SaveOpmsTargetRequest Request(int periodId, Guid? sdbipLayerPublicId = null, ClassificationIds? classifications = null) => new(
        SourceTemplateId: null, SourceTemplateVersion: null, PeriodId: periodId, DepartmentId: null, UnitId: null,
        AssignedUserId: null, WardIds: [], AdditionalAssigneeIds: [], VoteNumberIds: [], IndicatorNumber: "OPMS-1",
        NationalKpa: "National KPA", MunicipalKpa: "Municipal KPA", StrategicGoalId: null, StrategicObjectiveId: null,
        PerformanceObjective: "Objective", TargetName: "Normalized target", KpiDescription: "Description", Baseline: 0m,
        BaselineDescription: null, BudgetSourceId: null, BudgetTypeId: null, UnitOfMeasureId: null, Weight: 10m,
        KpiType: "Quantitative", IndicatorType: "Output", FunctionalArea: null, StandardClassification: null,
        IdpReference: null, InternalReference: null, FmsLink: null, IsRevised: false, PeriodTargets: PeriodTargets())
        {
            SdbipLayerPublicId = sdbipLayerPublicId,
            NationalKpaPublicId = classifications?.NationalKpa,
            MunicipalKpaPublicId = classifications?.MunicipalKpa,
            BackToBasicsPillarPublicId = classifications?.BackToBasicsPillar,
            StrategicGoalPublicId = classifications?.StrategicGoal,
            StrategicInterventionPublicId = classifications?.StrategicIntervention,
            StrategicObjectivePublicId = classifications?.StrategicObjective,
            PerformanceObjectivePublicId = classifications?.PerformanceObjective,
            KpiTypePublicId = classifications?.KpiType,
            IndicatorTypePublicId = classifications?.IndicatorType
        };

    private static SaveIpmsTargetRequest IpmsRequest(int periodId, ClassificationIds classifications) => new(
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
        PeriodTargets: PeriodTargets())
        {
            NationalKpaPublicId = classifications.NationalKpa,
            MunicipalKpaPublicId = classifications.MunicipalKpa,
            BackToBasicsPillarPublicId = classifications.BackToBasicsPillar,
            StrategicGoalPublicId = classifications.StrategicGoal,
            StrategicInterventionPublicId = classifications.StrategicIntervention,
            StrategicObjectivePublicId = classifications.StrategicObjective,
            PerformanceObjectivePublicId = classifications.PerformanceObjective,
            KpiTypePublicId = classifications.KpiType,
            IndicatorTypePublicId = classifications.IndicatorType
        };

    private static SaveTargetPeriodValueRequest[] PeriodTargets(string annual = "100") =>
    [
        new(ReportingPeriodType.Quarter1, PerformanceUnitKind.PercentageBased, PerformanceDirection.HigherIsBetter, "20", 10m, "Q1 target"),
        new(ReportingPeriodType.Annual, PerformanceUnitKind.PercentageBased, PerformanceDirection.HigherIsBetter, annual, null, "Annual target")
    ];

    private sealed record ClassificationIds(Guid NationalKpa, Guid MunicipalKpa, Guid BackToBasicsPillar, Guid StrategicGoal, Guid StrategicIntervention, Guid StrategicObjective, Guid PerformanceObjective, Guid KpiType, Guid IndicatorType);

    private static async Task<(Municipality Municipality, ApplicationUser User, Period LegacyPeriod, SdbipLayer SdbipLayer, ClassificationIds Classifications)> SeedAsync(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context)
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
        var sdbipLayer = new SdbipLayer
        {
            MunicipalityId = municipality.Id,
            MunicipalityFinancialYearId = municipalityYear.Id,
            Code = "TOP",
            Name = "Top Layer SDBIP",
            DisplayOrder = 1,
            IsActive = true
        };
        context.SdbipLayers.Add(sdbipLayer);
        context.ReportingPeriods.AddRange(
            new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 9, 30), IsActive = true },
            new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANN", Name = "Annual", PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = financialYear.StartDate, EndDate = financialYear.EndDate, IsActive = true });
        var nationalKpa = new NationalKpa { Code = "NKPA", Name = "National KPA" };
        var municipalKpa = new MunicipalKpa { MunicipalityId = municipality.Id, Code = "MKPA", Name = "Municipal KPA" };
        var pillar = new BackToBasicsPillar { Code = "B2B", Name = "Back-to-Basics" };
        var goal = new MunicipalStrategicGoal { MunicipalityId = municipality.Id, Code = "GOAL", Name = "Strategic Goal" };
        var intervention = new StrategicIntervention { MunicipalityId = municipality.Id, Code = "INT", Name = "Strategic Intervention" };
        var objective = new MunicipalStrategicObjective { MunicipalityId = municipality.Id, Code = "OBJ", Name = "Strategic Objective" };
        var performanceObjective = new PerformanceObjective { MunicipalityId = municipality.Id, Code = "PERF", Name = "Objective" };
        var kpiType = new GovernedKpiType { MunicipalityId = municipality.Id, Code = "QUANT", Name = "Quantitative" };
        var indicatorType = new GovernedIndicatorType { MunicipalityId = municipality.Id, Code = "OUTPUT", Name = "Output" };
        context.AddRange(nationalKpa, municipalKpa, pillar, goal, intervention, objective, performanceObjective, kpiType, indicatorType);
        await context.SaveChangesAsync();
        return (municipality, user, legacyPeriod, sdbipLayer,
            new(nationalKpa.PublicId, municipalKpa.PublicId, pillar.PublicId, goal.PublicId, intervention.PublicId, objective.PublicId, performanceObjective.PublicId, kpiType.PublicId, indicatorType.PublicId));
    }
}

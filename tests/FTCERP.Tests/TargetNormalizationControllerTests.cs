using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class TargetNormalizationControllerTests
{
    [Fact]
    public async Task Preview_and_execute_reconcile_legacy_target_with_explicit_actor_and_audit_reason()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = await SeedAsync(context);
        var audit = new Mock<IWorkflowGovernanceService>();
        audit.Setup(service => service.WriteAuditTrailAsync(
                "OpmsTarget", target.Id, "NormalizeLegacyPeriodTargets", null, It.IsAny<object>(), user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = Controller(context, municipality.Id, user, audit.Object);

        var previewResult = await controller.Preview(SubmissionKind.Opms);
        var preview = Assert.IsType<ApiResponse<PagedResponse<TargetNormalizationPreviewDto>>>(Assert.IsType<OkObjectResult>(previewResult.Result).Value).Data!;
        Assert.Equal(1, preview.TotalCount);
        Assert.Equal("Ready", Assert.Single(preview.Items).Status);
        Assert.Equal(2, preview.Items[0].MissingPeriodTargets);

        var executeResult = await controller.Execute(new TargetNormalizationRequest(SubmissionKind.Opms, [target.PublicId], "Approved legacy cutover"));
        var executed = Assert.IsType<ApiResponse<TargetNormalizationResultDto>>(Assert.IsType<OkObjectResult>(executeResult.Result).Value).Data!;
        Assert.Equal(2, executed.AddedPeriodTargets);
        Assert.All(await context.PerformancePeriodTargets.ToArrayAsync(), item => Assert.Equal(user.Id, item.CreatedByUserId));
        audit.VerifyAll();

        var afterResult = await controller.Preview(SubmissionKind.Opms);
        var after = Assert.IsType<ApiResponse<PagedResponse<TargetNormalizationPreviewDto>>>(Assert.IsType<OkObjectResult>(afterResult.Result).Value).Data!;
        Assert.Equal("Normalized", Assert.Single(after.Items).Status);
        Assert.Equal(0, after.Items[0].MissingPeriodTargets);
    }

    [Fact]
    public async Task Execute_refuses_legacy_revised_values_without_governance_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = await SeedAsync(context);
        target.RevisedAnnualTarget = 110m;
        await context.SaveChangesAsync();

        var result = await Controller(context, municipality.Id, user, Mock.Of<IWorkflowGovernanceService>())
            .Execute(new TargetNormalizationRequest(SubmissionKind.Opms, [target.PublicId], "Attempted cutover"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("revision endpoint", Assert.IsType<ApiResponse<TargetNormalizationResultDto>>(conflict.Value).Message);
        Assert.Empty(await context.PerformancePeriodTargets.ToArrayAsync());
    }

    private static TargetNormalizationController Controller(
        FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context,
        long municipalityId,
        ApplicationUser user,
        IWorkflowGovernanceService workflow)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.UserId).Returns(user.Id);
        return new TargetNormalizationController(context, tenant.Object, new PerformanceUnitEngine(), IdpTestFixture.CreateUserManagerMock(user).Object, workflow)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static async Task<(Municipality Municipality, ApplicationUser User, OpmsTarget Target)> SeedAsync(
        FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "BACKFILL", Name = "Backfill Municipality" };
        var user = IdpTestFixture.CreateUser("backfill-user");
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
        var target = new OpmsTarget
        {
            MunicipalityId = municipality.Id,
            PeriodId = legacyPeriod.Id,
            IndicatorNumber = "LEGACY-1",
            TargetName = "Legacy target",
            KpiDescription = "Legacy description",
            PerformanceObjective = "Objective",
            NationalKpa = "National",
            MunicipalKpa = "Municipal",
            TargetUnitType = "percentage",
            AnnualTarget = 100m,
            AnnualTargetDescription = "Annual",
            Q1Target = 20m,
            Q1Description = "Quarter 1",
            Q1Budget = 10m
        };
        context.OpmsTargets.Add(target);
        await context.SaveChangesAsync();
        return (municipality, user, target);
    }
}

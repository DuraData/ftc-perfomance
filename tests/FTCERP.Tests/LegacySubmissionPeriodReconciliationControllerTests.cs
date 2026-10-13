using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class LegacySubmissionPeriodReconciliationControllerTests
{
    [Fact]
    public async Task Preview_and_execute_link_only_the_target_scoped_period_and_preserve_the_legacy_quarter()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context);
        var controller = Controller(context, setup);

        var response = await controller.Preview(SubmissionKind.Opms, 1, 25);
        var page = Assert.IsType<ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>>>(
            Assert.IsType<OkObjectResult>(response.Result).Value).Data!;
        var preview = Assert.Single(page.Items);
        Assert.Equal("Ready", preview.Status);
        Assert.Equal("Quarter 1", preview.LegacyQuarter);
        var candidate = Assert.Single(preview.Candidates);
        Assert.Equal(setup.ReportingPeriod.PublicId, candidate.ReportingPeriodPublicId);
        Assert.False(candidate.HasSubmissionConflict);

        var executed = await controller.Execute(new(SubmissionKind.Opms, setup.Submission.PublicId,
            candidate.ReportingPeriodPublicId, "Approved historical period reconciliation.", preview.RowVersion));
        var result = Assert.IsType<ApiResponse<LegacySubmissionPeriodResultDto>>(
            Assert.IsType<OkObjectResult>(executed.Result).Value).Data!;

        context.ChangeTracker.Clear();
        var submission = await context.OpmsSubmissions.SingleAsync();
        Assert.Equal(setup.ReportingPeriod.Id, submission.ReportingPeriodId);
        Assert.Equal("Quarter 1", submission.Quarter);
        var evidence = await context.LegacySubmissionPeriodReconciliations.SingleAsync();
        Assert.Equal(result.ReconciliationPublicId, evidence.PublicId);
        Assert.Equal("Quarter 1", evidence.SourceQuarterSnapshot);
        Assert.Equal(setup.ReportingPeriod.Id, evidence.ReportingPeriodId);
        Assert.Equal(setup.Administrator.Id, evidence.ReconciledByUserId);
        Assert.Equal("Reconcile", (await context.AuditTrails.SingleAsync(x =>
            x.EntityName == nameof(LegacySubmissionPeriodReconciliation))).Action);

        evidence.Reason = "Attempted rewrite";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Execute_rejects_duplicate_logical_submission()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context, includeCanonicalSibling: true);
        var controller = Controller(context, setup);
        var previewResponse = await controller.Preview(SubmissionKind.Opms, 1, 25);
        var preview = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>>>(
            Assert.IsType<OkObjectResult>(previewResponse.Result).Value).Data!.Items);
        Assert.Equal("Blocked", preview.Status);
        Assert.True(Assert.Single(preview.Candidates).HasSubmissionConflict);

        var duplicate = await controller.Execute(new(SubmissionKind.Opms, setup.Submission.PublicId,
            setup.ReportingPeriod.PublicId, "Do not merge ambiguous duplicate submissions.", preview.RowVersion));
        Assert.IsType<ConflictObjectResult>(duplicate.Result);
        Assert.Empty(await context.LegacySubmissionPeriodReconciliations.ToArrayAsync());

    }

    [Fact]
    public async Task Execute_rejects_a_stale_submission_row_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context);
        var stale = await Controller(context, setup).Execute(new(SubmissionKind.Opms, setup.Submission.PublicId,
            setup.ReportingPeriod.PublicId, "Reject a deliberately stale reconciliation attempt.", Convert.ToBase64String(Guid.NewGuid().ToByteArray())));
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.Empty(await context.LegacySubmissionPeriodReconciliations.ToArrayAsync());
    }

    [Fact]
    public async Task Preview_is_tenant_isolated_and_unknown_quarters_remain_blocked()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context);
        setup.Submission.Quarter = "Unverified cycle";
        await context.SaveChangesAsync();
        var other = new Municipality { Code = "PERIOD-OTHER", Name = "Other Municipality" };
        context.Municipalities.Add(other);
        await context.SaveChangesAsync();

        var previewResponse = await Controller(context, setup).Preview(SubmissionKind.Opms, 1, 25);
        var page = Assert.IsType<ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>>>(
            Assert.IsType<OkObjectResult>(previewResponse.Result).Value).Data!;
        Assert.Equal(1, page.TotalCount);
        var preview = Assert.Single(page.Items);
        Assert.Equal("Blocked", preview.Status);
        Assert.Empty(preview.Candidates);

        var tenant = IdpTestFixture.Tenant(other.Id, setup.Administrator.Id);
        var otherPreview = await Controller(context, setup, tenant).Preview(SubmissionKind.Opms, 1, 25);
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>>>(
            Assert.IsType<OkObjectResult>(otherPreview.Result).Value).Data!.TotalCount);
    }

    [Fact]
    public async Task Ipms_submissions_use_the_same_controlled_reconciliation_path()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context);
        var legacyYearId = await context.Periods.Select(x => x.Id).SingleAsync();
        var target = new IpmsTarget
        {
            MunicipalityId = setup.Municipality.Id, PeriodId = legacyYearId, IndicatorNumber = "IPMS-LEGACY-PERIOD-1",
            TargetName = "IPMS legacy period target", KpiDescription = "IPMS legacy period target", PerformanceObjective = "Objective"
        };
        context.IpmsTargets.Add(target);
        await context.SaveChangesAsync();
        context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
        {
            MunicipalityId = setup.Municipality.Id, ReportingPeriodId = setup.ReportingPeriod.Id, IpmsTargetId = target.Id,
            UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = "5", CreatedByUserId = setup.Administrator.Id
        });
        var submission = new IpmsSubmission
        {
            MunicipalityId = setup.Municipality.Id, IpmsTargetId = target.Id, Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress, Status = "Draft", CreatedBy = setup.Administrator.Id
        };
        context.IpmsSubmissions.Add(submission);
        await context.SaveChangesAsync();

        var controller = Controller(context, setup);
        var previewResponse = await controller.Preview(SubmissionKind.Ipms, 1, 25);
        var preview = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>>>(
            Assert.IsType<OkObjectResult>(previewResponse.Result).Value).Data!.Items);
        var selected = Assert.Single(preview.Candidates);
        var executed = await controller.Execute(new(SubmissionKind.Ipms, submission.PublicId,
            selected.ReportingPeriodPublicId, "Approved IPMS historical period reconciliation.", preview.RowVersion));
        Assert.IsType<OkObjectResult>(executed.Result);
        context.ChangeTracker.Clear();
        Assert.Equal(setup.ReportingPeriod.Id, (await context.IpmsSubmissions.SingleAsync()).ReportingPeriodId);
        Assert.Equal(submission.Id, (await context.LegacySubmissionPeriodReconciliations.SingleAsync(x => x.SubmissionKind == SubmissionKind.Ipms)).IpmsSubmissionId);
    }

    private static LegacySubmissionPeriodReconciliationController Controller(
        ApplicationDbContext context, Setup setup, ITenantContext? tenant = null)
    {
        tenant ??= IdpTestFixture.Tenant(setup.Municipality.Id, setup.Administrator.Id);
        return new(context, tenant, IdpTestFixture.CreateUserManagerMock(setup.Administrator).Object,
            new WorkflowGovernanceService(context, tenantContext: tenant))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = IdpTestFixture.CreatePrincipal(setup.Administrator.Id),
                    TraceIdentifier = "legacy-period-reconciliation-test"
                }
            }
        };
    }

    private static async Task<Setup> Seed(ApplicationDbContext context, bool includeCanonicalSibling = false)
    {
        var municipality = new Municipality { Code = $"PER-{Guid.NewGuid():N}"[..12], Name = "Period Reconciliation Municipality" };
        var administrator = IdpTestFixture.CreateUser("period-admin");
        var financialYear = new FinancialYear
        {
            Code = "2040/41", Name = "2040/41", StartDate = new(2040, 7, 1), EndDate = new(2041, 6, 30)
        };
        var legacyYear = new Period
        {
            Code = "FY2040", Name = "2040/41", FiscalYear = financialYear.Code,
            StartDate = financialYear.StartDate, EndDate = financialYear.EndDate
        };
        context.AddRange(municipality, administrator, financialYear, legacyYear);
        await context.SaveChangesAsync();
        administrator.MunicipalityId = municipality.Id;
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id,
            EffectiveFrom = financialYear.StartDate, IsCurrent = true
        };
        context.MunicipalityFinancialYears.Add(municipalityYear);
        await context.SaveChangesAsync();
        var period = new ReportingPeriod
        {
            MunicipalityFinancialYearId = municipalityYear.Id, Code = "Q1", Name = "Quarter 1",
            PeriodType = ReportingPeriodType.Quarter1, Sequence = 1,
            StartDate = financialYear.StartDate, EndDate = new(2040, 9, 30)
        };
        var target = new OpmsTarget
        {
            MunicipalityId = municipality.Id, PeriodId = legacyYear.Id, IndicatorNumber = "LEGACY-PERIOD-1",
            TargetName = "Legacy period target", KpiDescription = "Legacy period target", PerformanceObjective = "Objective"
        };
        context.AddRange(period, target);
        await context.SaveChangesAsync();
        context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
        {
            MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, OpmsTargetId = target.Id,
            UnitKind = PerformanceUnitKind.AbsoluteCount,
            Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = "10", CreatedByUserId = administrator.Id
        });
        var submission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTargetId = target.Id, Quarter = "Quarter 1",
            BaseState = SubmissionBaseStates.InProgress, Status = "Draft", CreatedBy = administrator.Id
        };
        context.OpmsSubmissions.Add(submission);
        if (includeCanonicalSibling)
            context.OpmsSubmissions.Add(new OpmsSubmission
            {
                MunicipalityId = municipality.Id, OpmsTargetId = target.Id, ReportingPeriodId = period.Id,
                Quarter = "Q1", BaseState = SubmissionBaseStates.InProgress, Status = "Draft", CreatedBy = administrator.Id
            });
        await context.SaveChangesAsync();
        return new(municipality, administrator, period, submission);
    }

    private sealed record Setup(Municipality Municipality, ApplicationUser Administrator,
        ReportingPeriod ReportingPeriod, OpmsSubmission Submission);
}

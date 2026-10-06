using System.Text;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class KpiPeriodOrderingTests
{
    [Fact]
    public async Task Definition_revision_is_independent_filtered_append_only_and_rowversion_protected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "FIELD-A", Name = "Field revision municipality" };
        var user = IdpTestFixture.CreateUser("field-revision-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var target = Target("field-target", municipality.Id, "KPI-ORIGINAL", 1, 1);
        target.TargetName = "Original target";
        target.KpiDescription = "Original wording";
        context.OpmsTargets.Add(target);
        await context.SaveChangesAsync();
        var staleVersion = Convert.ToBase64String(target.RowVersion);
        var controller = OpmsController(context, municipality.Id, user);

        var result = await controller.ReviseDefinitionFields(target.Id, new ReviseKpiDefinitionRequest(
            true, "KPI-REVISED", false, null, false, null,
            "Externally approved KPI number", "COUNCIL-FIELD-1", new DateTime(2026, 10, 3), staleVersion));

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal("KPI-ORIGINAL", response.IndicatorNumber);
        Assert.True(response.IsIndicatorNumberRevised);
        Assert.Equal("KPI-REVISED", response.RevisedIndicatorNumber);
        Assert.False(response.IsTargetNameRevised);
        Assert.False(response.IsKpiDescriptionRevised);
        var revision = Assert.Single(await context.KpiFieldRevisions.ToArrayAsync());
        Assert.Equal(nameof(OpmsTarget.IndicatorNumber), revision.FieldName);
        Assert.Equal("KPI-ORIGINAL", revision.OriginalValue);
        Assert.Equal("KPI-REVISED", revision.RevisedValue);

        var fieldHistoryPage = Assert.IsType<ApiResponse<PagedResponse<KpiFieldRevisionResponse>>>(Assert.IsType<OkObjectResult>((await controller.GetFieldRevisionsPage(target.Id, new PagedQueryRequest { PageSize = 10, SortBy = "recordedAt" })).Result).Value).Data!;
        var fieldHistory = fieldHistoryPage.Items;
        Assert.Single(fieldHistory);
        var orderingHistoryPage = Assert.IsType<ApiResponse<PagedResponse<KpiFieldRevisionResponse>>>(Assert.IsType<OkObjectResult>((await controller.GetOrderingRevisionsPage(target.Id, new PagedQueryRequest { PageSize = 10, SortBy = "recordedAt" })).Result).Value).Data!;
        var orderingHistory = orderingHistoryPage.Items;
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetFieldRevisions(target.Id).Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetOrderingRevisions(target.Id).Result).StatusCode);
        Assert.Empty(orderingHistory);

        revision.Reason = "Rewritten";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        var stale = await controller.ReviseDefinitionFields(target.Id, new ReviseKpiDefinitionRequest(
            false, null, true, "Revised name", false, null,
            "Second approved field", "COUNCIL-FIELD-2", new DateTime(2026, 10, 4), staleVersion));
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.Equal(1, await context.KpiFieldRevisions.CountAsync());
    }

    [Fact]
    public async Task Ipms_definition_revision_persists_only_the_independently_flagged_field()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "FIELD-IPMS", Name = "IPMS field revision" };
        var user = IdpTestFixture.CreateUser("ipms-field-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var target = new IpmsTarget
        {
            Id = "ipms-field-target", MunicipalityId = municipality.Id, IndicatorNumber = "I-1",
            TargetName = "Original name", KpiDescription = "Original wording", AnnualTargetDescription = "Annual"
        };
        context.IpmsTargets.Add(target);
        await context.SaveChangesAsync();
        var controller = IpmsController(context, municipality.Id, user);

        var result = await controller.ReviseDefinitionFields(target.Id, new ReviseKpiDefinitionRequest(
            false, null, false, null, true, "Revised individual wording",
            "Approved individual revision", "IPMS-FIELD-1", new DateTime(2026, 10, 3), Convert.ToBase64String(target.RowVersion)));

        var response = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.True(response.IsKpiDescriptionRevised);
        Assert.Equal("Revised individual wording", response.RevisedKpiDescription);
        Assert.False(response.IsIndicatorNumberRevised);
        Assert.False(response.IsTargetNameRevised);
        var revision = Assert.Single(await context.KpiFieldRevisions.ToArrayAsync());
        Assert.Equal(nameof(IpmsTarget.KpiDescription), revision.FieldName);
        Assert.Equal(target.Id, revision.IpmsTargetId);
    }

    [Fact]
    public void Effective_values_use_each_flag_independently_and_only_in_late_periods()
    {
        var target = Target("effective-fields", 1, "KPI-ORIGINAL", 1, 2);
        target.TargetName = "Original name";
        target.KpiDescription = "Original wording";
        target.IsIndicatorNumberRevised = true;
        target.RevisedIndicatorNumber = "KPI-REVISED";
        target.IsTargetNameRevised = false;
        target.RevisedTargetName = "Ignored revised name";
        target.IsKpiDescriptionRevised = true;
        target.RevisedKpiDescription = "Revised wording";
        var q1 = new PerformancePeriodTarget
        {
            ReportingPeriod = new ReportingPeriod { PeriodType = ReportingPeriodType.Quarter1 },
            UnitKind = PerformanceUnitKind.AbsoluteCount, TargetValue = "10", BudgetValue = 100,
            IsTargetRevised = true, RevisedUnitKind = PerformanceUnitKind.PercentageBased, RevisedTargetValue = "75",
            IsBudgetRevised = true, RevisedBudgetValue = 250
        };
        var annual = new PerformancePeriodTarget
        {
            ReportingPeriod = new ReportingPeriod { PeriodType = ReportingPeriodType.Annual },
            UnitKind = q1.UnitKind, TargetValue = q1.TargetValue, BudgetValue = q1.BudgetValue,
            IsTargetRevised = true, RevisedUnitKind = q1.RevisedUnitKind, RevisedTargetValue = q1.RevisedTargetValue,
            IsBudgetRevised = true, RevisedBudgetValue = q1.RevisedBudgetValue
        };

        Assert.Equal("KPI-ORIGINAL", PerformanceRevisionResolver.EffectiveIndicatorNumber(target, ReportingPeriodType.Quarter1));
        Assert.Equal("KPI-REVISED", PerformanceRevisionResolver.EffectiveIndicatorNumber(target, ReportingPeriodType.Annual));
        Assert.Equal("Original name", PerformanceRevisionResolver.EffectiveTargetName(target, ReportingPeriodType.Annual));
        Assert.Equal("Revised wording", PerformanceRevisionResolver.EffectiveKpiDescription(target, ReportingPeriodType.Annual));
        Assert.Equal("10", PerformanceRevisionResolver.EffectiveTargetValue(q1));
        Assert.Equal(PerformanceUnitKind.AbsoluteCount, PerformanceRevisionResolver.EffectiveUnitKind(q1));
        Assert.Equal(100, PerformanceRevisionResolver.EffectiveBudgetValue(q1));
        Assert.Equal("75", PerformanceRevisionResolver.EffectiveTargetValue(annual));
        Assert.Equal(PerformanceUnitKind.PercentageBased, PerformanceRevisionResolver.EffectiveUnitKind(annual));
        Assert.Equal(250, PerformanceRevisionResolver.EffectiveBudgetValue(annual));
    }

    [Fact]
    public async Task Period_revision_preserves_originals_and_rejects_revised_values_for_early_periods()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "FIELD-PERIOD", Name = "Period revision" };
        var user = IdpTestFixture.CreateUser("period-field-user");
        var year = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30), IsActive = true };
        context.AddRange(municipality, user, year);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = year.Id, IsCurrent = true, IsActive = true, EffectiveFrom = year.StartDate };
        context.Add(municipalityYear);
        await context.SaveChangesAsync();
        var q1 = new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = year.StartDate, EndDate = new(2026, 9, 30), IsActive = true };
        var annual = new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANN", Name = "Annual", PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = year.StartDate, EndDate = year.EndDate, IsActive = true };
        var target = Target("period-field-target", municipality.Id, "PERIOD-1", 1, 1);
        context.AddRange(q1, annual, target);
        await context.SaveChangesAsync();
        var q1Value = new PerformancePeriodTarget { MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, OpmsTargetId = target.Id, UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter, TargetValue = "10", BudgetValue = 100, CreatedByUserId = user.Id };
        var annualValue = new PerformancePeriodTarget { MunicipalityId = municipality.Id, ReportingPeriodId = annual.Id, OpmsTargetId = target.Id, UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter, TargetValue = "40", BudgetValue = 400, CreatedByUserId = user.Id };
        context.AddRange(q1Value, annualValue);
        await context.SaveChangesAsync();
        var controller = PeriodController(context, municipality.Id, user);

        var early = await controller.Revise(q1Value.PublicId, new RevisePerformancePeriodTargetRequest(
            PerformanceUnitKind.PercentageBased, PerformanceDirection.HigherIsBetter, "75", 150, null, true,
            "Not applicable early", "EARLY-1", new DateTime(2026, 10, 4), Convert.ToBase64String(q1Value.RowVersion)));
        Assert.IsType<BadRequestObjectResult>(early.Result);

        var late = await controller.Revise(annualValue.PublicId, new RevisePerformancePeriodTargetRequest(
            PerformanceUnitKind.PercentageBased, PerformanceDirection.HigherIsBetter, "75", 500, "Approved annual revision", true,
            "Approved annual target and budget", "ANNUAL-1", new DateTime(2026, 10, 4), Convert.ToBase64String(annualValue.RowVersion)));
        var response = Assert.IsType<ApiResponse<PerformancePeriodTargetDto>>(Assert.IsType<OkObjectResult>(late.Result).Value).Data!;

        Assert.Equal("40", response.OriginalTargetValue);
        Assert.Equal(PerformanceUnitKind.AbsoluteCount, response.OriginalUnitKind);
        Assert.Equal(400, response.OriginalBudgetValue);
        Assert.Equal("75", response.TargetValue);
        Assert.Equal(PerformanceUnitKind.PercentageBased, response.UnitKind);
        Assert.Equal(500, response.BudgetValue);
        Assert.True(response.IsTargetRevised);
        Assert.True(response.IsBudgetRevised);
        Assert.Contains(await context.PerformanceTargetRevisions.ToArrayAsync(), item => item.FieldName == nameof(PerformancePeriodTarget.RevisedTargetValue));
        Assert.Contains(await context.PerformanceTargetRevisions.ToArrayAsync(), item => item.FieldName == nameof(PerformancePeriodTarget.RevisedBudgetValue));
        var history = Assert.IsType<ApiResponse<PagedResponse<PerformanceTargetRevisionDto>>>(Assert.IsType<OkObjectResult>((await controller.RevisionsPage(
            annualValue.PublicId, new PagedQueryRequest { Page = 1, PageSize = 1, Search = "ANNUAL-1", SortBy = "recordedAt", SortDirection = "desc" })).Result).Value).Data!;
        Assert.Equal(await context.PerformanceTargetRevisions.CountAsync(), history.TotalCount);
        Assert.Equal(history.TotalCount, history.TotalPages);
        Assert.True(history.TotalCount > 1);
        Assert.Single(history.Items);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.Revisions(annualValue.PublicId).Result).StatusCode);
        Assert.IsType<BadRequestObjectResult>((await controller.RevisionsPage(annualValue.PublicId, new PagedQueryRequest { SortBy = "unsafe" })).Result);
    }

    [Fact]
    public async Task Ordering_revision_is_field_specific_audited_append_only_and_rowversion_protected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "ORDER-A", Name = "Ordering municipality" };
        var user = IdpTestFixture.CreateUser("ordering-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var target = Target("order-target", municipality.Id, "KPI-1", 3, 9);
        context.OpmsTargets.Add(target);
        await context.SaveChangesAsync();
        var staleVersion = Convert.ToBase64String(target.RowVersion);

        var controller = OpmsController(context, municipality.Id, user);
        var result = await controller.ReviseOrdering(target.Id, new ReviseKpiOrderingRequest(
            4, 1, "Approved SDBIP resequencing", "COUNCIL-2026-10", new DateTime(2026, 10, 1), staleVersion));

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(4, response.OriginalOrderNumber);
        Assert.Equal(1, response.RevisedOrderNumber);
        var revisions = await context.KpiFieldRevisions.OrderBy(item => item.FieldName).ToArrayAsync();
        Assert.Equal(2, revisions.Length);
        Assert.Equal(["OriginalOrderNumber", "RevisedOrderNumber"], revisions.Select(item => item.FieldName));
        Assert.All(revisions, item => Assert.Equal("COUNCIL-2026-10", item.ApprovalReference));

        revisions[0].Reason = "Rewritten";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        var stale = await controller.ReviseOrdering(target.Id, new ReviseKpiOrderingRequest(
            5, 2, "Second approved sequence", "COUNCIL-2026-11", new DateTime(2026, 11, 1), staleVersion));
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.Equal(2, await context.KpiFieldRevisions.CountAsync());
    }

    [Fact]
    public async Task Ipms_ordering_revision_uses_the_same_governed_field_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "ORDER-IPMS", Name = "IPMS ordering" };
        var user = IdpTestFixture.CreateUser("ipms-ordering-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var target = new IpmsTarget
        {
            Id = "ipms-order-target", MunicipalityId = municipality.Id, IndicatorNumber = "IPMS-1", TargetName = "IPMS order",
            KpiDescription = "IPMS order", AnnualTargetDescription = "IPMS order", OriginalOrderNumber = 2, RevisedOrderNumber = 8
        };
        context.IpmsTargets.Add(target);
        await context.SaveChangesAsync();

        var controller = IpmsController(context, municipality.Id, user);
        var result = await controller.ReviseOrdering(target.Id, new ReviseKpiOrderingRequest(
            2, 1, "Approved individual KPI sequence", "IPMS-APPROVAL-1", new DateTime(2026, 10, 2), Convert.ToBase64String(target.RowVersion)));

        var response = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(1, response.RevisedOrderNumber);
        var revision = Assert.Single(await context.KpiFieldRevisions.ToArrayAsync());
        Assert.Equal(nameof(IpmsTarget.RevisedOrderNumber), revision.FieldName);
        Assert.Equal(target.Id, revision.IpmsTargetId);
    }

    [Fact]
    public async Task Effective_order_uses_original_for_q1_and_revised_for_annual_with_stable_tie_breaking()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "ORDER-B", Name = "Report ordering" };
        var user = IdpTestFixture.CreateUser("report-order-user");
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30), IsActive = true };
        context.AddRange(municipality, user, financialYear);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, IsCurrent = true, IsActive = true, EffectiveFrom = financialYear.StartDate };
        context.Add(year);
        await context.SaveChangesAsync();
        var q1 = new ReportingPeriod { MunicipalityFinancialYearId = year.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new(2026, 7, 1), EndDate = new(2026, 9, 30), IsActive = true };
        var annual = new ReportingPeriod { MunicipalityFinancialYearId = year.Id, Code = "ANN", Name = "Annual", PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = financialYear.StartDate, EndDate = financialYear.EndDate, IsActive = true };
        var firstOriginal = Target("target-a", municipality.Id, "ORIGINAL-FIRST", 1, 20, new Guid("00000000-0000-0000-0000-000000000002"));
        var firstRevised = Target("target-b", municipality.Id, "REVISED-FIRST", 20, 1, new Guid("00000000-0000-0000-0000-000000000001"));
        context.AddRange(q1, annual, firstOriginal, firstRevised);
        await context.SaveChangesAsync();
        context.OpmsSubmissions.AddRange(
            Submission("q1-a", municipality.Id, firstOriginal.Id, q1.Id, "Q1"),
            Submission("q1-b", municipality.Id, firstRevised.Id, q1.Id, "Q1"),
            Submission("ann-a", municipality.Id, firstOriginal.Id, annual.Id, "ANN"),
            Submission("ann-b", municipality.Id, firstRevised.Id, annual.Id, "ANN"));
        await context.SaveChangesAsync();

        var controller = ReportController(context, municipality.Id, user);
        var q1Csv = await Export(controller, q1.PublicId);
        var annualCsv = await Export(controller, annual.PublicId);

        Assert.True(q1Csv.IndexOf("ORIGINAL-FIRST", StringComparison.Ordinal) < q1Csv.IndexOf("REVISED-FIRST", StringComparison.Ordinal));
        Assert.True(annualCsv.IndexOf("REVISED-FIRST", StringComparison.Ordinal) < annualCsv.IndexOf("ORIGINAL-FIRST", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Kpi_register_effective_order_requires_a_period_and_switches_order_source()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "ORDER-REGISTER", Name = "Register ordering" };
        var user = IdpTestFixture.CreateUser("register-order-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        context.OpmsTargets.AddRange(
            Target("register-a", municipality.Id, "ORIGINAL-FIRST", 1, 20),
            Target("register-b", municipality.Id, "REVISED-FIRST", 20, 1));
        await context.SaveChangesAsync();
        var controller = OpmsController(context, municipality.Id, user);

        var missingPeriod = await controller.GetTargetsPage(new PagedQueryRequest { SortBy = "effectiveOrder", SortDirection = "asc" });
        Assert.IsType<BadRequestObjectResult>(missingPeriod.Result);
        var q1 = Assert.IsType<ApiResponse<PagedResponse<OpmsTargetResponse>>>(Assert.IsType<OkObjectResult>((await controller.GetTargetsPage(new PagedQueryRequest { SortBy = "effectiveOrder", SortDirection = "asc", ReportingPeriodType = ReportingPeriodType.Quarter1 })).Result).Value).Data!;
        var annual = Assert.IsType<ApiResponse<PagedResponse<OpmsTargetResponse>>>(Assert.IsType<OkObjectResult>((await controller.GetTargetsPage(new PagedQueryRequest { SortBy = "effectiveOrder", SortDirection = "asc", ReportingPeriodType = ReportingPeriodType.Annual })).Result).Value).Data!;

        Assert.Equal(["ORIGINAL-FIRST", "REVISED-FIRST"], q1.Items.Select(item => item.IndicatorNumber));
        Assert.Equal(["REVISED-FIRST", "ORIGINAL-FIRST"], annual.Items.Select(item => item.IndicatorNumber));
    }

    [Fact]
    public async Task Ordering_history_is_tenant_scoped_and_cross_tenant_target_is_not_found()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        ApplicationUser user;

        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "ORDER-C-A", Name = "Tenant A" };
            var tenantB = new Municipality { Code = "ORDER-C-B", Name = "Tenant B" };
            user = IdpTestFixture.CreateUser("tenant-order-user");
            setup.AddRange(tenantA, tenantB, user);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            setup.OpmsTargets.Add(Target("tenant-b-target", tenantB.Id, "TENANT-B", 1, 1));
            await setup.SaveChangesAsync();
        }

        await using var tenantContext = new ApplicationDbContext(options, new TenantContext(tenantAId, user.Id));
        var controller = OpmsController(tenantContext, tenantAId, user);
        var result = await controller.GetOrderingRevisionsPage("tenant-b-target", new PagedQueryRequest());
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Relational_constraints_reject_invalid_order_values_and_ambiguous_revision_ownership()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "ORDER-CONSTRAINT", Name = "Ordering constraints" };
        var user = IdpTestFixture.CreateUser("ordering-constraint-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        context.OpmsTargets.Add(Target("invalid-order", municipality.Id, "INVALID", 0, 1));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        var opms = Target("constraint-opms", municipality.Id, "OPMS", 1, 1);
        var ipms = new IpmsTarget { Id = "constraint-ipms", MunicipalityId = municipality.Id, IndicatorNumber = "IPMS", TargetName = "IPMS", KpiDescription = "IPMS", AnnualTargetDescription = "IPMS" };
        context.AddRange(opms, ipms);
        await context.SaveChangesAsync();
        context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = municipality.Id,
            OpmsTargetId = opms.Id,
            IpmsTargetId = ipms.Id,
            FieldName = nameof(OpmsTarget.RevisedOrderNumber),
            OriginalValue = "1",
            RevisedValue = "2",
            Reason = "Invalid ambiguous owner",
            ApprovalReference = "INVALID",
            EffectiveAt = new DateTime(2026, 10, 1),
            RevisedByUserId = user.Id
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<string> Export(PerformanceReportsController controller, Guid periodPublicId)
    {
        var result = await controller.ExportCsv(SubmissionKind.Opms, periodPublicId);
        return Encoding.UTF8.GetString(Assert.IsType<FileContentResult>(result).FileContents);
    }

    private static OpmsTarget Target(string id, long municipalityId, string indicator, int original, int revised, Guid? publicId = null) => new()
    {
        Id = id,
        PublicId = publicId ?? Guid.NewGuid(),
        MunicipalityId = municipalityId,
        IndicatorNumber = indicator,
        OriginalOrderNumber = original,
        RevisedOrderNumber = revised,
        TargetName = indicator,
        KpiDescription = indicator,
        AnnualTargetDescription = indicator
    };

    private static OpmsSubmission Submission(string id, long municipalityId, string targetId, long reportingPeriodId, string quarter) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        OpmsTargetId = targetId,
        ReportingPeriodId = reportingPeriodId,
        Quarter = quarter,
        Status = SubmissionBaseStates.Submitted,
        BaseState = SubmissionBaseStates.Submitted,
        ActualPerformance = "1"
    };

    private static OpmsTargetsController OpmsController(ApplicationDbContext context, long municipalityId, ApplicationUser user)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => new AccessDecisionResult(true, "Allowed", [code], [], []));
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        var governance = new Mock<IWorkflowGovernanceService>();
        governance.Setup(service => service.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>())).Returns(Task.CompletedTask);
        return new OpmsTargetsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, governance.Object, new TenantContext(municipalityId, user.Id), new PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };
    }

    private static PerformanceReportsController ReportController(ApplicationDbContext context, long municipalityId, ApplicationUser user)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_REPORT.EXPORT"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        return new PerformanceReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, new TenantContext(municipalityId, user.Id), Mock.Of<IWorkflowGovernanceService>())
        {
            ControllerContext = ControllerContext(user.Id)
        };
    }

    private static IpmsTargetsController IpmsController(ApplicationDbContext context, long municipalityId, ApplicationUser user)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => new AccessDecisionResult(true, "Allowed", [code], [], []));
        var governance = new Mock<IWorkflowGovernanceService>();
        governance.Setup(service => service.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>())).Returns(Task.CompletedTask);
        return new IpmsTargetsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, governance.Object, new TenantContext(municipalityId, user.Id), new PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };
    }

    private static PerformancePeriodTargetsController PeriodController(ApplicationDbContext context, long municipalityId, ApplicationUser user)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => new AccessDecisionResult(true, "Allowed", [code], [], []));
        return new PerformancePeriodTargetsController(context, new TenantContext(municipalityId, user.Id), new PerformanceUnitEngine(), access.Object, IdpTestFixture.CreateUserManagerMock(user).Object)
        {
            ControllerContext = ControllerContext(user.Id)
        };
    }

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(userId), TraceIdentifier = "ordering-test" }
    };

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => userId;
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "system";
    }
}

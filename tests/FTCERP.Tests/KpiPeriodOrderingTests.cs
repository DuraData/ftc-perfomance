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
        var result = await controller.GetOrderingRevisions("tenant-b-target");
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

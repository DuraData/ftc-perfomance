using System.Text;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FTCERP.Tests;

public sealed class PerformanceReportMemberSecurityTests
{
    private static readonly AccessQueryScopeResult Granted = new(true, true, [], [], [], [], [], []);
    private static readonly AccessQueryScopeResult Denied = new(false, false, [], [], [], [], [], []);

    [Theory]
    [InlineData("OPMS_REPORT.GENERATE")]
    [InlineData("OPMS_KPI.READ")]
    [InlineData("OPMS_SUBMISSION.READ")]
    [InlineData("OPMS_SUBMISSION.ActualPerformance.READ")]
    [InlineData("OPMS_SUBMISSION.Variance.READ")]
    public async Task Summary_denies_when_any_report_source_or_member_permission_is_missing(string deniedPermission)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("summary-member-user");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var controller = Controller(context, user, code => code == deniedPermission ? Denied : Granted);

        var result = await controller.Summary(SubmissionKind.Opms);

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Theory]
    [InlineData("OPMS_REPORT.EXPORT")]
    [InlineData("OPMS_KPI.READ")]
    [InlineData("OPMS_SUBMISSION.READ")]
    [InlineData("OPMS_SUBMISSION.ActualPerformance.READ")]
    [InlineData("OPMS_SUBMISSION.Variance.READ")]
    public async Task Csv_denies_when_any_report_source_or_member_permission_is_missing(string deniedPermission)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("csv-member-user");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var controller = Controller(context, user, code => code == deniedPermission ? Denied : Granted);

        var result = await controller.ExportCsv(SubmissionKind.Opms);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Report_queries_intersect_member_scope_before_aggregation_and_export()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "REPORT-MEMBER", Name = "Report member municipality" };
        var user = IdpTestFixture.CreateUser("scoped-report-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var allowedDepartment = new Department { MunicipalityId = municipality.Id, Code = "ALLOWED", Name = "Allowed department" };
        var deniedDepartment = new Department { MunicipalityId = municipality.Id, Code = "DENIED", Name = "Denied department" };
        context.AddRange(allowedDepartment, deniedDepartment);
        await context.SaveChangesAsync();
        var allowedTarget = Target("allowed-target", municipality.Id, allowedDepartment.Id, "VISIBLE-INDICATOR");
        var deniedTarget = Target("denied-target", municipality.Id, deniedDepartment.Id, "HIDDEN-INDICATOR");
        context.AddRange(allowedTarget, deniedTarget,
            Submission("allowed-submission", municipality.Id, allowedTarget.Id, "VISIBLE-ACTUAL"),
            Submission("denied-submission", municipality.Id, deniedTarget.Id, "HIDDEN-ACTUAL"));
        await context.SaveChangesAsync();
        var memberScope = new AccessQueryScopeResult(true, false, [allowedDepartment.Id], [], [], [], [], []);
        var controller = Controller(context, user, code => code.EndsWith(".ActualPerformance.READ", StringComparison.Ordinal)
            ? memberScope : Granted, municipality.Id);

        var summaryResult = await controller.Summary(SubmissionKind.Opms);
        var summary = summaryResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PerformanceReportSummaryDto>>().Subject.Data!;
        summary.SubmissionCount.Should().Be(1);
        summary.Departments.Should().ContainSingle(item => item.Department == allowedDepartment.Name);

        var csvResult = await controller.ExportCsv(SubmissionKind.Opms);
        var csv = Encoding.UTF8.GetString(csvResult.Should().BeOfType<FileContentResult>().Subject.FileContents);
        csv.Should().Contain("VISIBLE-INDICATOR").And.Contain("VISIBLE-ACTUAL");
        csv.Should().NotContain("HIDDEN-INDICATOR").And.NotContain("HIDDEN-ACTUAL");
    }

    [Fact]
    public async Task Live_exports_render_canonical_workflow_status_for_opms_and_ipms()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "REPORT-STATUS", Name = "Canonical report municipality" };
        var user = IdpTestFixture.CreateUser("canonical-report-user");
        var year = new FinancialYear { Code = "2037/38", Name = "2037/2038", StartDate = new(2037, 7, 1), EndDate = new(2038, 6, 30) };
        context.AddRange(municipality, user, year);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = year.Id, EffectiveFrom = year.StartDate
        };
        var department = new Department { MunicipalityId = municipality.Id, Code = "STATUS", Name = "Status department" };
        context.AddRange(municipalityYear, department);
        await context.SaveChangesAsync();
        var opmsTarget = Target("canonical-opms-report-target", municipality.Id, department.Id, "CANONICAL-OPMS");
        var ipmsTarget = new IpmsTarget
        {
            Id = "canonical-ipms-report-target", MunicipalityId = municipality.Id, DepartmentId = department.Id,
            IndicatorNumber = "CANONICAL-IPMS", TargetName = "Canonical IPMS", KpiDescription = "Canonical IPMS",
            AnnualTargetDescription = "Canonical IPMS", OriginalOrderNumber = 1, RevisedOrderNumber = 1
        };
        context.AddRange(opmsTarget, ipmsTarget);
        await context.SaveChangesAsync();
        context.AddRange(
            new OpmsSubmission
            {
                Id = "canonical-opms-report-submission", MunicipalityId = municipality.Id, OpmsTargetId = opmsTarget.Id,
                Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "draft", ActualPerformance = "90"
            },
            new IpmsSubmission
            {
                Id = "canonical-ipms-report-submission", MunicipalityId = municipality.Id, IpmsTargetId = ipmsTarget.Id,
                Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "draft", ActualPerformance = "90"
            });
        var opmsWorkflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id, SubmissionKind = SubmissionKind.Opms,
            Code = "OPMS-REPORT-STATUS", Name = "OPMS report status", EffectiveFrom = year.StartDate
        };
        var ipmsWorkflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id, SubmissionKind = SubmissionKind.Ipms,
            Code = "IPMS-REPORT-STATUS", Name = "IPMS report status", EffectiveFrom = year.StartDate
        };
        context.AddRange(opmsWorkflow, ipmsWorkflow,
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id, WorkflowDefinition = opmsWorkflow, SubmissionKind = SubmissionKind.Opms,
                SubmissionId = "canonical-opms-report-submission", State = WorkflowInstanceState.Completed, CompletedAt = DateTime.UtcNow
            },
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id, WorkflowDefinition = ipmsWorkflow, SubmissionKind = SubmissionKind.Ipms,
                SubmissionId = "canonical-ipms-report-submission", State = WorkflowInstanceState.Completed, CompletedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();
        var controller = Controller(context, user, _ => Granted, municipality.Id);

        var opmsCsv = Encoding.UTF8.GetString(Assert.IsType<FileContentResult>(await controller.ExportCsv(SubmissionKind.Opms)).FileContents);
        var ipmsCsv = Encoding.UTF8.GetString(Assert.IsType<FileContentResult>(await controller.ExportCsv(SubmissionKind.Ipms)).FileContents);

        opmsCsv.Should().Contain("CANONICAL-OPMS").And.Contain("\"completed\"").And.NotContain("\"draft\"");
        ipmsCsv.Should().Contain("CANONICAL-IPMS").And.Contain("\"completed\"").And.NotContain("\"draft\"");
    }

    private static PerformanceReportsController Controller(
        ApplicationDbContext context,
        ApplicationUser user,
        Func<string, AccessQueryScopeResult> scope,
        long municipalityId = 1)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.GetQueryScopeAsync(user, It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string code) => scope(code));
        return new PerformanceReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object,
            new TenantContext(municipalityId, user.Id), Mock.Of<IWorkflowGovernanceService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };
    }

    private static OpmsTarget Target(string id, long municipalityId, int departmentId, string indicator) => new()
    {
        Id = id, MunicipalityId = municipalityId, DepartmentId = departmentId, IndicatorNumber = indicator,
        TargetName = indicator, KpiDescription = indicator, AnnualTargetDescription = indicator,
        OriginalOrderNumber = 1, RevisedOrderNumber = 1
    };

    private static OpmsSubmission Submission(string id, long municipalityId, string targetId, string actual) => new()
    {
        Id = id, MunicipalityId = municipalityId, OpmsTargetId = targetId, Quarter = "Q1",
        BaseState = SubmissionBaseStates.Submitted, Status = SubmissionBaseStates.Submitted,
        ActualPerformance = actual, Variance = 2, AchievementPercent = 80, TargetAchieved = true
    };

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => userId;
    }
}

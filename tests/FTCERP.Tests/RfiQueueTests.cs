using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class RfiQueueTests
{
    [Fact]
    public async Task Queue_intersects_dynamic_scopes_masks_members_and_reports_exclusive_actionable_states()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "RFI-QUEUE", Name = "RFI Queue Municipality" };
        var user = IdpTestFixture.CreateUser("rfi-queue-user", "RFI", "Responder");
        user.Municipality = municipality;
        var financialYear = new FinancialYear
        {
            Code = "2030/31", Name = "2030/31", StartDate = new(2030, 7, 1), EndDate = new(2031, 6, 30)
        };
        context.AddRange(municipality, user, financialYear);
        await context.SaveChangesAsync();

        var municipalYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear,
            EffectiveFrom = financialYear.StartDate, IsCurrent = true
        };
        var period = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalYear, Code = "Q2", Name = "Quarter 2",
            PeriodType = ReportingPeriodType.Quarter2, Sequence = 2,
            StartDate = new(2030, 10, 1), EndDate = new(2030, 12, 31)
        };
        var opmsDefinition = Definition(municipality.Id, municipalYear, SubmissionKind.Opms, "RFI-QUEUE-OPMS");
        var ipmsDefinition = Definition(municipality.Id, municipalYear, SubmissionKind.Ipms, "RFI-QUEUE-IPMS");
        var opmsTarget = new OpmsTarget
        {
            Id = "rfi-opms-target", MunicipalityId = municipality.Id, AssignedUserId = user.Id,
            IndicatorNumber = "KPI-WATER-01", TargetName = "Reduce water losses",
            KpiDescription = "Reduce water losses", AnnualTargetDescription = "Ten percent"
        };
        var ipmsTarget = new IpmsTarget
        {
            Id = "rfi-ipms-target", MunicipalityId = municipality.Id, AssignedUserId = user.Id,
            IndicatorNumber = "KPI-SECRET-02", TargetName = "Excluded employee target",
            KpiDescription = "Excluded employee target", AnnualTargetDescription = "Complete"
        };
        context.AddRange(municipalYear, period, opmsDefinition, ipmsDefinition, opmsTarget, ipmsTarget);
        await context.SaveChangesAsync();

        var opmsSubmission = new OpmsSubmission
        {
            Id = "rfi-opms-submission", MunicipalityId = municipality.Id, OpmsTargetId = opmsTarget.Id,
            ReportingPeriodId = period.Id, Quarter = "Q2", Status = "reviewed", BaseState = SubmissionBaseStates.Submitted,
            SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow
        };
        var ipmsSubmission = new IpmsSubmission
        {
            Id = "rfi-ipms-submission", MunicipalityId = municipality.Id, IpmsTargetId = ipmsTarget.Id,
            ReportingPeriodId = period.Id, Quarter = "Q2", Status = "reviewed", BaseState = SubmissionBaseStates.Submitted,
            SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow
        };
        context.AddRange(opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();
        var opmsInstance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinitionId = opmsDefinition.Id,
            SubmissionKind = SubmissionKind.Opms, SubmissionId = opmsSubmission.Id, State = WorkflowInstanceState.Active
        };
        var ipmsInstance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinitionId = ipmsDefinition.Id,
            SubmissionKind = SubmissionKind.Ipms, SubmissionId = ipmsSubmission.Id, State = WorkflowInstanceState.Active
        };
        context.AddRange(opmsInstance, ipmsInstance);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        await InsertRfi(context, municipality.Id, opmsInstance.Id, user.Id, "Open protected question", now.AddHours(-1), now.AddDays(2));
        await InsertRfi(context, municipality.Id, opmsInstance.Id, user.Id, "Responded protected question", now.AddHours(-2), now.AddDays(1), user.Id, now.AddMinutes(-10));
        await InsertRfi(context, municipality.Id, opmsInstance.Id, user.Id, "Overdue protected question", now.AddDays(-5), now.AddDays(-1));
        await InsertRfi(context, municipality.Id, opmsInstance.Id, user.Id, "Closed protected question", now.AddDays(-4), now.AddDays(-2), user.Id, now.AddDays(-3), user.Id, now.AddDays(-2));
        await InsertRfi(context, municipality.Id, ipmsInstance.Id, user.Id, "Excluded question", now.AddHours(-1), now.AddDays(2));

        var opmsScope = Scope([opmsTarget.Id]);
        var ipmsSubmissionScope = Scope([ipmsTarget.Id]);
        var deniedScope = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OPMS_RFI.RESPOND", "OPMS_WORKFLOW.ActionComment.UPDATE", "OPMS_RFI.CLOSE"
        };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ")).ReturnsAsync(opmsScope);
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_RFI.READ")).ReturnsAsync(opmsScope);
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ")).ReturnsAsync(ipmsSubmissionScope);
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_RFI.READ")).ReturnsAsync(deniedScope);
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext>()))
            .ReturnsAsync((ApplicationUser _, string permission, AccessScopeContext? _) => Decision(grants.Contains(permission)));
        var controller = new RfiQueuesController(context, access.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };

        var response = await Get(controller, new RfiQueueQueryRequest
        {
            MunicipalityFinancialYearPublicId = municipalYear.PublicId, Kind = "opms", Period = "Q2",
            Search = "KPI-WATER", SortBy = "status", SortDirection = "asc"
        });

        response.Page.TotalCount.Should().Be(4);
        response.Page.Items.Should().HaveCount(4).And.OnlyContain(item => item.Kind == "opms" && item.Question == null);
        response.Page.Items.Should().NotContain(item => item.TargetName == ipmsTarget.TargetName);
        response.Counts.Should().Be(new RfiQueueCountsResponse(1, 1, 1, 1));
        response.Page.Items.Single(item => item.Status == "open").CanRespond.Should().BeFalse("Response.UPDATE is independently required");
        response.Page.Items.Single(item => item.Status == "responded").CanClose.Should().BeTrue();
        access.Verify(service => service.CheckPermissionAsync(user, It.IsAny<string>(),
            It.Is<AccessScopeContext>(scope => scope.TargetId == opmsSubmission.Id && scope.MunicipalityId == municipality.Id)), Times.AtLeastOnce);

        grants.Add("OPMS_RFI.Question.READ");
        grants.Add("OPMS_RFI.Response.UPDATE");
        var visible = await Get(controller, new RfiQueueQueryRequest { Status = "open" });
        visible.Page.Items.Should().ContainSingle();
        visible.Page.Items[0].Question.Should().Be("Open protected question");
        visible.Page.Items[0].CanRespond.Should().BeTrue();

        var overdue = await Get(controller, new RfiQueueQueryRequest { Status = "overdue" });
        overdue.Page.Items.Should().ContainSingle(item => item.Status == "overdue");
        var protectedSearch = await Get(controller, new RfiQueueQueryRequest { Search = "protected question" });
        protectedSearch.Page.TotalCount.Should().Be(0, "protected narratives are not searchable inference channels");
    }

    private static WorkflowDefinition Definition(long municipalityId, MunicipalityFinancialYear year, SubmissionKind kind, string code) => new()
    {
        MunicipalityId = municipalityId, MunicipalityFinancialYear = year, SubmissionKind = kind,
        Code = code, Name = code, EffectiveFrom = year.EffectiveFrom
    };

    private static AccessQueryScopeResult Scope(string[] targetIds) =>
        new(true, false, [], [], [], targetIds, [], []);

    private static AccessDecisionResult Decision(bool allowed) =>
        new(allowed, allowed ? "Allowed" : "Denied", [], [], []);

    private static async Task<RfiQueueResponse> Get(RfiQueuesController controller, RfiQueueQueryRequest request)
    {
        var result = await controller.Get(request);
        return Assert.IsType<ApiResponse<RfiQueueResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
    }

    private static async Task InsertRfi(
        FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context,
        long municipalityId,
        long instanceId,
        string raisedBy,
        string question,
        DateTime raisedAt,
        DateTime dueAt,
        string? respondedBy = null,
        DateTime? respondedAt = null,
        string? closedBy = null,
        DateTime? closedAt = null)
    {
        var response = respondedAt.HasValue ? "Governed response" : null;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO PerformanceRfis
                (PublicId, MunicipalityId, SubmissionWorkflowInstanceId, Question, RaisedByUserId, RaisedAt,
                 ResponseDueAt, Response, RespondedByUserId, RespondedAt, ClosedByUserId, ClosedAt, RowVersion)
            VALUES
                ({Guid.NewGuid()}, {municipalityId}, {instanceId}, {question}, {raisedBy}, {raisedAt},
                 {dueAt}, {response}, {respondedBy}, {respondedAt}, {closedBy}, {closedAt}, {Guid.NewGuid().ToByteArray()})
            """);
    }
}

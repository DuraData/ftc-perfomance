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
            OutstandingTargets: 0,
            DraftSubmissions: 0,
            SubmittedSubmissions: 1,
            ReturnedSubmissions: 1,
            ApprovedSubmissions: 1,
            PendingVerification: 1,
            PendingApproval: 1), response.Data);
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

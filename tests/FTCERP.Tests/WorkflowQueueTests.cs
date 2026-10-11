using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class WorkflowQueueTests
{
    [Fact]
    public async Task Combined_queue_counts_and_pages_are_filtered_by_dynamic_scope_before_database_paging()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("queue-user", "Queue", "Owner");
        var other = IdpTestFixture.CreateUser("other-user", "Other", "Submitter");
        context.Users.AddRange(user, other);
        context.OpmsTargets.AddRange(OpmsTarget("opms-draft", user.Id), OpmsTarget("opms-verify", other.Id), OpmsTarget("opms-outside", other.Id));
        context.IpmsTargets.AddRange(IpmsTarget("ipms-approved", user.Id), IpmsTarget("ipms-outside", other.Id));
        context.OpmsSubmissions.AddRange(
            OpmsSubmission("own-draft", "opms-draft", "draft", user.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            OpmsSubmission("verify", "opms-verify", "pending_verification", other.Id, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            OpmsSubmission("outside-verify", "opms-outside", "pending_verification", other.Id, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)));
        context.IpmsSubmissions.AddRange(
            IpmsSubmission("own-approved", "ipms-approved", "approved", user.Id, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)),
            IpmsSubmission("outside-approved", "ipms-outside", "approved", other.Id, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        var verificationPublicId = await context.OpmsSubmissions.Where(item => item.Id == "verify").Select(item => item.PublicId).SingleAsync();
        var approvedPublicId = await context.IpmsSubmissions.Where(item => item.Id == "own-approved").Select(item => item.PublicId).SingleAsync();
        var outsideApprovedPublicId = await context.IpmsSubmissions.Where(item => item.Id == "outside-approved").Select(item => item.PublicId).SingleAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"))
            .ReturnsAsync(Scope(["opms-draft", "opms-verify"]));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ"))
            .ReturnsAsync(Scope(["ipms-approved"]));
        var memberPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext>()))
            .ReturnsAsync((ApplicationUser _, string permission, AccessScopeContext? _) =>
                new AccessDecisionResult(memberPermissions.Contains(permission), memberPermissions.Contains(permission) ? "Allowed" : "Denied", [], [], []));
        var controller = new WorkflowQueuesController(context, access.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };

        var verificationResult = await controller.Get(new WorkflowQueueQueryRequest { Queue = "verification", Page = 1, PageSize = 1 });
        var verification = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(verificationResult.Result).Value).Data!;

        Assert.Equal(1, verification.Page.TotalCount);
        Assert.Equal(1, verification.Page.TotalPages);
        var verificationItem = Assert.Single(verification.Page.Items);
        Assert.Equal(verificationPublicId, verificationItem.PublicId);
        Assert.Equal("opms", verificationItem.Kind);
        Assert.Null(verificationItem.SubmittedByUserPublicId);
        Assert.Null(verificationItem.SubmittedByName);
        Assert.Equal(2, verification.Counts.MySubmissions);
        Assert.Equal(1, verification.Counts.Verification);
        Assert.Equal(1, verification.Counts.Pms);
        Assert.Equal(1, verification.Counts.MyDrafts);
        Assert.Equal(1, verification.Counts.ApprovedClosed);

        memberPermissions.Add("OPMS_SUBMISSION.SubmitterIdentity.READ");
        var visibleVerificationResult = await controller.Get(new WorkflowQueueQueryRequest { Queue = "verification", Page = 1, PageSize = 1 });
        var visibleVerification = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(visibleVerificationResult.Result).Value).Data!;
        Assert.Equal(other.PublicId, Assert.Single(visibleVerification.Page.Items).SubmittedByUserPublicId);
        Assert.Equal(other.FullName, Assert.Single(visibleVerification.Page.Items).SubmittedByName);

        memberPermissions.Add("IPMS_SUBMISSION.SubmitterIdentity.READ");
        var approvedResult = await controller.Get(new WorkflowQueueQueryRequest { Queue = "approved-closed" });
        var approved = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(approvedResult.Result).Value).Data!;
        var approvedItem = Assert.Single(approved.Page.Items);
        Assert.Equal(approvedPublicId, approvedItem.PublicId);
        Assert.Equal("ipms", approvedItem.Kind);
        Assert.Equal(user.PublicId, approvedItem.SubmittedByUserPublicId);
        Assert.DoesNotContain(approved.Page.Items, item => item.PublicId == outsideApprovedPublicId);
    }

    private static AccessQueryScopeResult Scope(string[] targetIds) =>
        new(true, false, [], [], [], targetIds, [], []);

    private static OpmsTarget OpmsTarget(string id, string ownerId) => new()
    {
        Id = id,
        AssignedUserId = ownerId,
        IndicatorNumber = id,
        TargetName = id,
        KpiDescription = id,
        AnnualTargetDescription = id
    };

    private static IpmsTarget IpmsTarget(string id, string ownerId) => new()
    {
        Id = id,
        AssignedUserId = ownerId,
        IndicatorNumber = id,
        TargetName = id,
        KpiDescription = id,
        AnnualTargetDescription = id
    };

    private static OpmsSubmission OpmsSubmission(string id, string targetId, string status, string submitterId, DateTime createdAt) => new()
    {
        Id = id,
        OpmsTargetId = targetId,
        Quarter = "Q1",
        Status = status,
        SubmittedByUserId = submitterId,
        CreatedAt = createdAt
    };

    private static IpmsSubmission IpmsSubmission(string id, string targetId, string status, string submitterId, DateTime createdAt) => new()
    {
        Id = id,
        IpmsTargetId = targetId,
        Quarter = "Q1",
        Status = status,
        SubmittedByUserId = submitterId,
        CreatedAt = createdAt
    };
}

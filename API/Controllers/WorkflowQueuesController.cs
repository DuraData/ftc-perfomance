using System.Security.Claims;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/workflow-queues")]
[Authorize]
public sealed class WorkflowQueuesController(
    ApplicationDbContext context,
    IAccessControlService accessControl) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WorkflowQueueResponse>>> Get([FromQuery] WorkflowQueueQueryRequest request)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<WorkflowQueueResponse>(false, null, "User not found"));

        var opmsScope = await accessControl.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ");
        var ipmsScope = await accessControl.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ");
        var records = OpmsRecords(opmsScope).Concat(IpmsRecords(ipmsScope));
        var counts = await Summarize(records, user.Id);
        var filtered = ApplyQueue(records, request.NormalizedQueue, user.Id);
        var totalCount = await filtered.CountAsync();
        var rows = await filtered
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Id)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .ToListAsync();

        var page = PagedResponse<WorkflowQueueItemResponse>.Create(
            rows.Select(item => new WorkflowQueueItemResponse(
                item.Id,
                item.PublicId,
                item.Kind,
                item.TargetId,
                item.TargetPublicId,
                item.TargetName,
                item.IndicatorNumber,
                item.Quarter,
                item.DueDate,
                item.Status,
                item.SubmittedByUserId,
                item.SubmittedByName,
                item.VerifierName,
                item.ApproverName,
                item.CreatedAt)),
            request.Page,
            request.PageSize,
            totalCount);

        return Ok(new ApiResponse<WorkflowQueueResponse>(true, new(request.NormalizedQueue, counts, page)));
    }

    private IQueryable<QueueRecord> OpmsRecords(AccessQueryScopeResult scope)
    {
        var query = context.OpmsSubmissions.AsNoTracking().AsQueryable();
        if (!scope.PermissionGranted) query = query.Where(_ => false);
        else if (!scope.Unrestricted)
            query = query.Where(item =>
                (item.OpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.OpmsTarget.DepartmentId.Value))
                || (item.OpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.OpmsTarget.UnitId.Value))
                || (item.OpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.OpmsTarget.AssignedUserId))
                || scope.TargetIds.Contains(item.OpmsTargetId)
                || scope.KpiIds.Contains(item.OpmsTargetId));

        return query.Select(item => new QueueRecord
        {
            Id = item.Id,
            PublicId = item.PublicId,
            Kind = "opms",
            TargetId = item.OpmsTargetId,
            TargetPublicId = item.OpmsTarget.PublicId,
            TargetName = item.OpmsTarget.TargetName,
            IndicatorNumber = item.OpmsTarget.IndicatorNumber,
            Quarter = item.Quarter,
            DueDate = item.DueDate,
            Status = item.Status,
            SubmittedByUserId = item.SubmittedByUserId,
            SubmittedByName = item.SubmittedByUser == null ? null : item.SubmittedByUser.FirstName + " " + item.SubmittedByUser.LastName,
            AssignedUserId = item.OpmsTarget.AssignedUserId,
            VerifierName = item.VerifierUser == null ? null : item.VerifierUser.FirstName + " " + item.VerifierUser.LastName,
            ApproverName = item.ApproverUser == null ? null : item.ApproverUser.FirstName + " " + item.ApproverUser.LastName,
            CreatedAt = item.CreatedAt
        });
    }

    private IQueryable<QueueRecord> IpmsRecords(AccessQueryScopeResult scope)
    {
        var query = context.IpmsSubmissions.AsNoTracking().AsQueryable();
        if (!scope.PermissionGranted) query = query.Where(_ => false);
        else if (!scope.Unrestricted)
            query = query.Where(item =>
                (item.IpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.IpmsTarget.DepartmentId.Value))
                || (item.IpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.IpmsTarget.UnitId.Value))
                || (item.IpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.IpmsTarget.AssignedUserId))
                || scope.TargetIds.Contains(item.IpmsTargetId)
                || scope.KpiIds.Contains(item.IpmsTargetId));

        return query.Select(item => new QueueRecord
        {
            Id = item.Id,
            PublicId = item.PublicId,
            Kind = "ipms",
            TargetId = item.IpmsTargetId,
            TargetPublicId = item.IpmsTarget.PublicId,
            TargetName = item.IpmsTarget.TargetName,
            IndicatorNumber = item.IpmsTarget.IndicatorNumber,
            Quarter = item.Quarter,
            DueDate = item.DueDate,
            Status = item.Status,
            SubmittedByUserId = item.SubmittedByUserId,
            SubmittedByName = item.SubmittedByUser == null ? null : item.SubmittedByUser.FirstName + " " + item.SubmittedByUser.LastName,
            AssignedUserId = item.IpmsTarget.AssignedUserId,
            VerifierName = item.VerifierUser == null ? null : item.VerifierUser.FirstName + " " + item.VerifierUser.LastName,
            ApproverName = item.ApproverUser == null ? null : item.ApproverUser.FirstName + " " + item.ApproverUser.LastName,
            CreatedAt = item.CreatedAt
        });
    }

    private static IQueryable<QueueRecord> ApplyQueue(IQueryable<QueueRecord> query, string queue, string userId)
    {
        var mine = query.Where(item => item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId));
        return queue switch
        {
            "my-submissions" => mine,
            "verification" => query.Where(item => item.Status == "pending_verification"),
            "approval" => query.Where(item => item.Status == "verified" || item.Status == "pending_approval"),
            "pms" => query.Where(item => item.Status == "approved"),
            "auditor" => query.Where(item => item.Status == "reviewed"),
            "returned" => query.Where(item => item.Status == "returned_for_info" || item.Status == "verify_rejected" || item.Status == "rejected"),
            "my-drafts" => mine.Where(item => item.Status == "draft"),
            "pending-submission" => mine.Where(item => item.Status == "submitted"),
            "my-returned" => mine.Where(item => item.Status == "returned_for_info" || item.Status == "verify_rejected" || item.Status == "rejected"),
            "under-verification" => mine.Where(item => item.Status == "pending_verification"),
            "under-review" => mine.Where(item => item.Status == "reviewed"),
            "under-approval" => mine.Where(item => item.Status == "pending_approval" || item.Status == "verified"),
            "internal-audit-returned" => mine.Where(item => item.Status == "audited"),
            "approved-closed" => mine.Where(item => item.Status == "approved" || item.Status == "completed"),
            _ => query
        };
    }

    private static async Task<WorkflowQueueCountsResponse> Summarize(IQueryable<QueueRecord> query, string userId) =>
        await query.GroupBy(_ => 1).Select(group => new WorkflowQueueCountsResponse(
            group.Count(item => item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)),
            group.Count(item => item.Status == "pending_verification"),
            group.Count(item => item.Status == "verified" || item.Status == "pending_approval"),
            group.Count(item => item.Status == "approved"),
            group.Count(item => item.Status == "reviewed"),
            group.Count(item => item.Status == "returned_for_info" || item.Status == "verify_rejected" || item.Status == "rejected"),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && item.Status == "draft"),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && item.Status == "submitted"),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && (item.Status == "returned_for_info" || item.Status == "verify_rejected" || item.Status == "rejected")),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && item.Status == "pending_verification"),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && item.Status == "reviewed"),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && (item.Status == "pending_approval" || item.Status == "verified")),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && item.Status == "audited"),
            group.Count(item => (item.SubmittedByUserId == userId || (item.SubmittedByUserId == null && item.AssignedUserId == userId)) && (item.Status == "approved" || item.Status == "completed"))))
            .SingleOrDefaultAsync() ?? WorkflowQueueCountsResponse.Empty;

    private async Task<ApplicationUser?> CurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
    }

    private sealed class QueueRecord
    {
        public string Id { get; init; } = string.Empty;
        public Guid PublicId { get; init; }
        public string Kind { get; init; } = string.Empty;
        public string TargetId { get; init; } = string.Empty;
        public Guid TargetPublicId { get; init; }
        public string TargetName { get; init; } = string.Empty;
        public string IndicatorNumber { get; init; } = string.Empty;
        public string Quarter { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? SubmittedByUserId { get; init; }
        public string? SubmittedByName { get; init; }
        public string? AssignedUserId { get; init; }
        public string? VerifierName { get; init; }
        public string? ApproverName { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}

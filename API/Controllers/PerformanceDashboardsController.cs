using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/performance-dashboards")]
[Authorize]
public sealed class PerformanceDashboardsController(
    ApplicationDbContext context,
    IAccessControlService accessControl) : ControllerBase
{
    [HttpGet("opms")]
    public async Task<ActionResult<ApiResponse<PerformanceDashboardResponse>>> GetOpms()
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<PerformanceDashboardResponse>(false, null, "User not found"));

        var targetScope = await accessControl.GetQueryScopeAsync(user, "OPMS_KPI.READ");
        var submissionScope = await accessControl.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ");
        var targets = context.OpmsTargets.AsNoTracking().AsQueryable();
        if (!targetScope.PermissionGranted) targets = targets.Where(_ => false);
        else if (!targetScope.Unrestricted) targets = ApplyTargetScope(targets, targetScope);

        var submissions = context.OpmsSubmissions.AsNoTracking().AsQueryable();
        if (!submissionScope.PermissionGranted) submissions = submissions.Where(_ => false);
        else if (!submissionScope.Unrestricted) submissions = ApplySubmissionScope(submissions, submissionScope);
        submissions = submissions.Where(item => targets.Select(target => target.Id).Contains(item.OpmsTargetId));

        var targetSummary = await targets.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Active = group.Count(item => !item.IsWithdrawn)
        }).SingleOrDefaultAsync();
        var submissionSummary = await Summarize(submissions);
        var completed = await submissions.Where(item => item.Status == "approved").Select(item => item.OpmsTargetId).Distinct().CountAsync();
        var overdue = await submissions.Where(item => item.DueDate < DateTime.UtcNow && item.Status != "approved").Select(item => item.OpmsTargetId).Distinct().CountAsync();

        return Ok(new ApiResponse<PerformanceDashboardResponse>(true, new(
            targetSummary?.Total ?? 0,
            targetSummary?.Active ?? 0,
            completed,
            overdue,
            submissionSummary.ReturnedTargets,
            0,
            submissionSummary.Draft,
            submissionSummary.Submitted,
            submissionSummary.Returned,
            submissionSummary.Approved,
            submissionSummary.PendingVerification,
            submissionSummary.PendingApproval)));
    }

    [HttpGet("ipms")]
    public async Task<ActionResult<ApiResponse<PerformanceDashboardResponse>>> GetIpms()
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<PerformanceDashboardResponse>(false, null, "User not found"));

        var targetScope = await accessControl.GetQueryScopeAsync(user, "IPMS_KPI.READ");
        var submissionScope = await accessControl.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ");
        var targets = context.IpmsTargets.AsNoTracking().AsQueryable();
        if (!targetScope.PermissionGranted) targets = targets.Where(_ => false);
        else if (!targetScope.Unrestricted) targets = ApplyTargetScope(targets, targetScope);

        var submissions = context.IpmsSubmissions.AsNoTracking().AsQueryable();
        if (!submissionScope.PermissionGranted) submissions = submissions.Where(_ => false);
        else if (!submissionScope.Unrestricted) submissions = ApplySubmissionScope(submissions, submissionScope);
        submissions = submissions.Where(item => targets.Select(target => target.Id).Contains(item.IpmsTargetId));

        var targetSummary = await targets.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Active = group.Count(item => !item.IsWithdrawn)
        }).SingleOrDefaultAsync();
        var submissionSummary = await Summarize(submissions);
        var achieved = await submissions.Where(item => item.Status == "approved").Select(item => item.IpmsTargetId).Distinct().CountAsync();
        var atRisk = await submissions.Where(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected").Select(item => item.IpmsTargetId).Distinct().CountAsync();
        var nonOutstanding = submissions.Where(item => item.Status != "draft" && item.Status != "submitted").Select(item => item.IpmsTargetId).Distinct();
        var outstanding = await targets.CountAsync(item => !nonOutstanding.Contains(item.Id));

        return Ok(new ApiResponse<PerformanceDashboardResponse>(true, new(
            targetSummary?.Total ?? 0,
            targetSummary?.Active ?? 0,
            achieved,
            0,
            atRisk,
            outstanding,
            submissionSummary.Draft,
            submissionSummary.Submitted,
            submissionSummary.Returned,
            submissionSummary.Approved,
            submissionSummary.PendingVerification,
            submissionSummary.PendingApproval)));
    }

    private async Task<ApplicationUser?> CurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
    }

    private static IQueryable<OpmsTarget> ApplyTargetScope(IQueryable<OpmsTarget> query, AccessQueryScopeResult scope) =>
        query.Where(item =>
            item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)
            || item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)
            || item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)
            || scope.TargetIds.Contains(item.Id)
            || scope.KpiIds.Contains(item.Id));

    private static IQueryable<IpmsTarget> ApplyTargetScope(IQueryable<IpmsTarget> query, AccessQueryScopeResult scope) =>
        query.Where(item =>
            item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)
            || item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)
            || item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)
            || scope.TargetIds.Contains(item.Id)
            || scope.KpiIds.Contains(item.Id));

    private static IQueryable<OpmsSubmission> ApplySubmissionScope(IQueryable<OpmsSubmission> query, AccessQueryScopeResult scope) =>
        query.Where(item =>
            item.OpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.OpmsTarget.DepartmentId.Value)
            || item.OpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.OpmsTarget.UnitId.Value)
            || item.OpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.OpmsTarget.AssignedUserId)
            || scope.TargetIds.Contains(item.OpmsTargetId)
            || scope.KpiIds.Contains(item.OpmsTargetId));

    private static IQueryable<IpmsSubmission> ApplySubmissionScope(IQueryable<IpmsSubmission> query, AccessQueryScopeResult scope) =>
        query.Where(item =>
            item.IpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.IpmsTarget.DepartmentId.Value)
            || item.IpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.IpmsTarget.UnitId.Value)
            || item.IpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.IpmsTarget.AssignedUserId)
            || scope.TargetIds.Contains(item.IpmsTargetId)
            || scope.KpiIds.Contains(item.IpmsTargetId));

    private static async Task<SubmissionSummary> Summarize(IQueryable<OpmsSubmission> query) =>
        await query.GroupBy(_ => 1).Select(group => new SubmissionSummary(
            group.Count(item => item.Status == "draft"),
            group.Count(item => item.Status == "submitted"),
            group.Count(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected"),
            group.Count(item => item.Status == "approved"),
            group.Count(item => item.Status == "submitted" || item.Status == "pending_verification"),
            group.Count(item => item.Status == "verified" || item.Status == "pending_approval"),
            group.Where(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected").Select(item => item.OpmsTargetId).Distinct().Count()))
            .SingleOrDefaultAsync() ?? SubmissionSummary.Empty;

    private static async Task<SubmissionSummary> Summarize(IQueryable<IpmsSubmission> query) =>
        await query.GroupBy(_ => 1).Select(group => new SubmissionSummary(
            group.Count(item => item.Status == "draft"),
            group.Count(item => item.Status == "submitted"),
            group.Count(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected"),
            group.Count(item => item.Status == "approved"),
            group.Count(item => item.Status == "submitted" || item.Status == "pending_verification"),
            group.Count(item => item.Status == "verified" || item.Status == "pending_approval"),
            group.Where(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected").Select(item => item.IpmsTargetId).Distinct().Count()))
            .SingleOrDefaultAsync() ?? SubmissionSummary.Empty;

    private sealed record SubmissionSummary(int Draft, int Submitted, int Returned, int Approved, int PendingVerification, int PendingApproval, int ReturnedTargets)
    {
        public static SubmissionSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);
    }
}

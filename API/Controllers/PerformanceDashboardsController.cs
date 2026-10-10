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
    public async Task<ActionResult<ApiResponse<PerformanceDashboardResponse>>> GetOpms(
        [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] Guid? reportingPeriodPublicId = null)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<PerformanceDashboardResponse>(false, null, "User not found"));

        var selectedYear = municipalityFinancialYearPublicId.HasValue
            ? await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear)
                .SingleOrDefaultAsync(item => item.PublicId == municipalityFinancialYearPublicId.Value && item.IsActive)
            : await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear)
                .Where(item => item.IsActive && item.IsCurrent)
                .OrderByDescending(item => item.EffectiveFrom)
                .FirstOrDefaultAsync();
        if (municipalityFinancialYearPublicId.HasValue && selectedYear == null)
            return BadRequest(new ApiResponse<PerformanceDashboardResponse>(false, null, "The selected municipality financial year is not active or is outside the current municipality."));

        var periods = selectedYear == null
            ? []
            : await context.ReportingPeriods.AsNoTracking()
                .Where(item => item.MunicipalityFinancialYearId == selectedYear.Id && item.IsActive)
                .OrderBy(item => item.Sequence)
                .ToArrayAsync();
        if (reportingPeriodPublicId.HasValue && periods.All(item => item.PublicId != reportingPeriodPublicId.Value))
            return BadRequest(new ApiResponse<PerformanceDashboardResponse>(false, null, "The selected reporting period does not belong to the selected municipality financial year."));

        var periodIds = periods.Select(item => item.Id).ToArray();
        var windows = periodIds.Length == 0
            ? []
            : await context.ReportingWindows.AsNoTracking()
                .Where(item => item.SubmissionKind == SubmissionKind.Opms && periodIds.Contains(item.ReportingPeriodId))
                .ToArrayAsync();
        var now = DateTime.UtcNow;
        var periodWindows = periods.Select(period => new PeriodWindow(
            period,
            windows.Where(window => window.ReportingPeriodId == period.Id).OrderByDescending(window => window.IsActive).ThenByDescending(window => window.OpensAt).FirstOrDefault()))
            .ToArray();
        var selectedPeriodWindow = reportingPeriodPublicId.HasValue
            ? periodWindows.Single(item => item.Period.PublicId == reportingPeriodPublicId.Value)
            : periodWindows.FirstOrDefault(item => WindowState(item.Window, now) == "Open")
              ?? periodWindows.Where(item => WindowState(item.Window, now) == "Closed").OrderByDescending(item => item.Window?.ClosesAt ?? item.Period.EndDate).FirstOrDefault()
              ?? periodWindows.Where(item => WindowState(item.Window, now) == "Upcoming").OrderBy(item => item.Window?.OpensAt ?? item.Period.StartDate).FirstOrDefault()
              ?? periodWindows.FirstOrDefault();

        var targetScope = await accessControl.GetQueryScopeAsync(user, "OPMS_KPI.READ");
        var submissionScope = await accessControl.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ");
        var targets = context.OpmsTargets.AsNoTracking().AsQueryable();
        if (!targetScope.PermissionGranted) targets = targets.Where(_ => false);
        else if (!targetScope.Unrestricted) targets = ApplyTargetScope(targets, targetScope);
        if (selectedYear != null)
            targets = targets.Where(target => context.PerformancePeriodTargets.Any(periodTarget =>
                periodTarget.OpmsTargetId == target.Id
                && periodTarget.IsActive
                && periodIds.Contains(periodTarget.ReportingPeriodId)));
        var yearTargets = targets;
        if (selectedPeriodWindow != null)
            targets = targets.Where(target => context.PerformancePeriodTargets.Any(periodTarget =>
                periodTarget.OpmsTargetId == target.Id
                && periodTarget.IsActive
                && periodTarget.ReportingPeriodId == selectedPeriodWindow.Period.Id));

        var yearSubmissions = context.OpmsSubmissions.AsNoTracking().AsQueryable();
        if (!submissionScope.PermissionGranted) yearSubmissions = yearSubmissions.Where(_ => false);
        else if (!submissionScope.Unrestricted) yearSubmissions = ApplySubmissionScope(yearSubmissions, submissionScope);
        yearSubmissions = yearSubmissions.Where(item => yearTargets.Select(target => target.Id).Contains(item.OpmsTargetId));
        if (selectedYear != null)
            yearSubmissions = yearSubmissions.Where(item => item.ReportingPeriodId.HasValue && periodIds.Contains(item.ReportingPeriodId.Value));
        var selectedSubmissions = selectedPeriodWindow == null
            ? yearSubmissions
            : yearSubmissions.Where(item => item.ReportingPeriodId == selectedPeriodWindow.Period.Id
                && targets.Select(target => target.Id).Contains(item.OpmsTargetId));

        var targetSummary = await targets.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Active = group.Count(item => !item.IsWithdrawn)
        }).SingleOrDefaultAsync();
        var submissionSummary = await Summarize(selectedSubmissions);
        var achievedTargetIds = selectedSubmissions.Where(item => CompletedStatuses.Contains(item.Status))
            .Select(item => item.OpmsTargetId).Distinct();
        var atRiskTargetIds = selectedSubmissions.Where(item => AtRiskStatuses.Contains(item.Status))
            .Select(item => item.OpmsTargetId).Distinct();
        var achieved = await achievedTargetIds.CountAsync();
        var atRisk = await atRiskTargetIds.CountAsync();
        var overdue = await selectedSubmissions.Where(item => item.DueDate < now && !CompletedStatuses.Contains(item.Status))
            .Select(item => item.OpmsTargetId).Distinct().CountAsync();
        var nonOutstanding = selectedYear == null
            ? selectedSubmissions.Where(item => item.Status != "draft" && item.Status != "submitted").Select(item => item.OpmsTargetId).Distinct()
            : selectedSubmissions.Where(item => item.BaseState == SubmissionBaseStates.Submitted && !item.IsDisabled).Select(item => item.OpmsTargetId).Distinct();
        var outstanding = await targets.CountAsync(item => !item.IsWithdrawn && !nonOutstanding.Contains(item.Id));

        var scopedSubmissionIds = selectedSubmissions.Select(item => item.Id);
        var workflowInstances = context.SubmissionWorkflowInstances.AsNoTracking()
            .Where(item => item.SubmissionKind == SubmissionKind.Opms && scopedSubmissionIds.Contains(item.SubmissionId));
        var workflowSubmissionIds = workflowInstances.Select(item => item.SubmissionId);
        var pendingVerification = await workflowInstances.CountAsync(item => item.State != WorkflowInstanceState.Completed
            && item.CurrentStage != null && item.CurrentStage.RequiredActionCode.Contains("VERIFY"));
        pendingVerification += await selectedSubmissions.CountAsync(item => !workflowSubmissionIds.Contains(item.Id)
            && (item.Status == "submitted" || item.Status == "pending_verification"));
        var pendingApproval = await workflowInstances.CountAsync(item => item.State != WorkflowInstanceState.Completed
            && item.CurrentStage != null && item.CurrentStage.RequiredActionCode.Contains("APPROV"));
        pendingApproval += await selectedSubmissions.CountAsync(item => !workflowSubmissionIds.Contains(item.Id)
            && (item.Status == "verified" || item.Status == "pending_approval"));

        var ratingLabels = await (from rating in context.SubmissionStageRatings.AsNoTracking()
            join instance in workflowInstances on rating.SubmissionWorkflowInstanceId equals instance.Id
            select rating.LabelSnapshot).ToArrayAsync();
        var ratingBreakdown = ratingLabels.GroupBy(label => label)
            .Select(group => new PerformanceDashboardRatingBreakdownResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count).ThenBy(item => item.Label)
            .ToArray();

        var departmentTotals = await targets.Where(item => !item.IsWithdrawn)
            .GroupBy(item => new
            {
                PublicId = item.Department == null ? (Guid?)null : item.Department.PublicId,
                Name = item.Department == null ? "Unassigned" : item.Department.Name
            })
            .Select(group => new { group.Key.PublicId, group.Key.Name, Count = group.Count() })
            .OrderByDescending(item => item.Count).ThenBy(item => item.Name)
            .ToArrayAsync();
        var departmentAchieved = await targets.Where(item => !item.IsWithdrawn && achievedTargetIds.Contains(item.Id))
            .GroupBy(item => new
            {
                PublicId = item.Department == null ? (Guid?)null : item.Department.PublicId,
                Name = item.Department == null ? "Unassigned" : item.Department.Name
            })
            .Select(group => new { group.Key.PublicId, group.Key.Name, Count = group.Count() })
            .ToArrayAsync();
        var departmentAtRisk = await targets.Where(item => !item.IsWithdrawn && atRiskTargetIds.Contains(item.Id))
            .GroupBy(item => new
            {
                PublicId = item.Department == null ? (Guid?)null : item.Department.PublicId,
                Name = item.Department == null ? "Unassigned" : item.Department.Name
            })
            .Select(group => new { group.Key.PublicId, group.Key.Name, Count = group.Count() })
            .ToArrayAsync();
        var departmentBreakdown = departmentTotals.Select(item => new PerformanceDashboardTeamBreakdownResponse(
            item.PublicId,
            item.Name,
            item.Count,
            departmentAchieved.SingleOrDefault(value => value.PublicId == item.PublicId && value.Name == item.Name)?.Count ?? 0,
            departmentAtRisk.SingleOrDefault(value => value.PublicId == item.PublicId && value.Name == item.Name)?.Count ?? 0))
            .ToArray();

        var periodTargetCounts = periodIds.Length == 0
            ? []
            : await context.PerformancePeriodTargets.AsNoTracking()
                .Where(item => item.OpmsTargetId != null && item.IsActive && periodIds.Contains(item.ReportingPeriodId)
                    && yearTargets.Where(target => !target.IsWithdrawn).Select(target => target.Id).Contains(item.OpmsTargetId))
                .GroupBy(item => item.ReportingPeriodId)
                .Select(group => new { ReportingPeriodId = group.Key, Count = group.Select(item => item.OpmsTargetId).Distinct().Count() })
                .ToArrayAsync();
        var periodSubmissionCounts = periodIds.Length == 0
            ? []
            : await yearSubmissions.Where(item => item.ReportingPeriodId.HasValue)
                .GroupBy(item => item.ReportingPeriodId!.Value)
                .Select(group => new
                {
                    ReportingPeriodId = group.Key,
                    Count = group.Count(),
                    Achieved = group.Where(item => CompletedStatuses.Contains(item.Status)).Select(item => item.OpmsTargetId).Distinct().Count(),
                    AtRisk = group.Where(item => AtRiskStatuses.Contains(item.Status)).Select(item => item.OpmsTargetId).Distinct().Count(),
                    NonOutstanding = group.Where(item => item.BaseState == SubmissionBaseStates.Submitted && !item.IsDisabled).Select(item => item.OpmsTargetId).Distinct().Count()
                })
                .ToArrayAsync();
        var periodBreakdown = periodWindows.Select(item =>
        {
            var targetCount = periodTargetCounts.SingleOrDefault(count => count.ReportingPeriodId == item.Period.Id)?.Count ?? 0;
            var submissionCount = periodSubmissionCounts.SingleOrDefault(count => count.ReportingPeriodId == item.Period.Id);
            return new PerformanceDashboardPeriodBreakdownResponse(
                item.Period.PublicId,
                item.Period.Code,
                item.Period.Name,
                item.Period.Sequence,
                WindowState(item.Window, now),
                item.Window?.OpensAt,
                item.Window?.ClosesAt,
                submissionCount?.Count ?? 0,
                submissionCount?.Achieved ?? 0,
                submissionCount?.AtRisk ?? 0,
                Math.Max(0, targetCount - (submissionCount?.NonOutstanding ?? 0)));
        }).ToArray();

        return Ok(new ApiResponse<PerformanceDashboardResponse>(true, new(
            targetSummary?.Total ?? 0,
            targetSummary?.Active ?? 0,
            achieved,
            overdue,
            atRisk,
            outstanding,
            submissionSummary.Draft,
            submissionSummary.Submitted,
            submissionSummary.Returned,
            submissionSummary.Approved,
            pendingVerification,
            pendingApproval,
            selectedYear?.PublicId,
            selectedYear?.FinancialYear.Code,
            selectedYear?.FinancialYear.Name,
            selectedPeriodWindow?.Period.PublicId,
            selectedPeriodWindow?.Period.Code,
            selectedPeriodWindow?.Period.Name,
            selectedPeriodWindow == null ? null : WindowState(selectedPeriodWindow.Window, now),
            selectedPeriodWindow?.Window?.OpensAt,
            selectedPeriodWindow?.Window?.ClosesAt,
            selectedYear == null ? null : ratingBreakdown,
            selectedYear == null ? null : departmentBreakdown,
            selectedYear == null ? null : periodBreakdown)));
    }

    [HttpGet("ipms")]
    public async Task<ActionResult<ApiResponse<PerformanceDashboardResponse>>> GetIpms(
        [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] Guid? reportingPeriodPublicId = null)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<PerformanceDashboardResponse>(false, null, "User not found"));

        var selectedYear = municipalityFinancialYearPublicId.HasValue
            ? await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear)
                .SingleOrDefaultAsync(item => item.PublicId == municipalityFinancialYearPublicId.Value && item.IsActive)
            : await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear)
                .Where(item => item.IsActive && item.IsCurrent)
                .OrderByDescending(item => item.EffectiveFrom)
                .FirstOrDefaultAsync();
        if (municipalityFinancialYearPublicId.HasValue && selectedYear == null)
            return BadRequest(new ApiResponse<PerformanceDashboardResponse>(false, null, "The selected municipality financial year is not active or is outside the current municipality."));

        var periods = selectedYear == null
            ? []
            : await context.ReportingPeriods.AsNoTracking()
                .Where(item => item.MunicipalityFinancialYearId == selectedYear.Id && item.IsActive)
                .OrderBy(item => item.Sequence)
                .ToArrayAsync();
        if (reportingPeriodPublicId.HasValue && periods.All(item => item.PublicId != reportingPeriodPublicId.Value))
            return BadRequest(new ApiResponse<PerformanceDashboardResponse>(false, null, "The selected reporting period does not belong to the selected municipality financial year."));

        var periodIds = periods.Select(item => item.Id).ToArray();
        var windows = periodIds.Length == 0
            ? []
            : await context.ReportingWindows.AsNoTracking()
                .Where(item => item.SubmissionKind == SubmissionKind.Ipms && periodIds.Contains(item.ReportingPeriodId))
                .ToArrayAsync();
        var now = DateTime.UtcNow;
        var periodWindows = periods.Select(period => new PeriodWindow(
            period,
            windows.Where(window => window.ReportingPeriodId == period.Id).OrderByDescending(window => window.IsActive).ThenByDescending(window => window.OpensAt).FirstOrDefault()))
            .ToArray();
        var selectedPeriodWindow = reportingPeriodPublicId.HasValue
            ? periodWindows.Single(item => item.Period.PublicId == reportingPeriodPublicId.Value)
            : periodWindows.FirstOrDefault(item => WindowState(item.Window, now) == "Open")
              ?? periodWindows.Where(item => WindowState(item.Window, now) == "Closed").OrderByDescending(item => item.Window?.ClosesAt ?? item.Period.EndDate).FirstOrDefault()
              ?? periodWindows.Where(item => WindowState(item.Window, now) == "Upcoming").OrderBy(item => item.Window?.OpensAt ?? item.Period.StartDate).FirstOrDefault()
              ?? periodWindows.FirstOrDefault();

        var targetScope = await accessControl.GetQueryScopeAsync(user, "IPMS_KPI.READ");
        var submissionScope = await accessControl.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ");
        var targets = context.IpmsTargets.AsNoTracking().AsQueryable();
        if (!targetScope.PermissionGranted) targets = targets.Where(_ => false);
        else if (!targetScope.Unrestricted) targets = ApplyTargetScope(targets, targetScope);
        if (selectedYear != null)
            targets = targets.Where(target => context.PerformancePeriodTargets.Any(periodTarget =>
                periodTarget.IpmsTargetId == target.Id
                && periodTarget.IsActive
                && periodIds.Contains(periodTarget.ReportingPeriodId)));
        var yearTargets = targets;
        if (selectedPeriodWindow != null)
            targets = targets.Where(target => context.PerformancePeriodTargets.Any(periodTarget =>
                periodTarget.IpmsTargetId == target.Id
                && periodTarget.IsActive
                && periodTarget.ReportingPeriodId == selectedPeriodWindow.Period.Id));

        var yearSubmissions = context.IpmsSubmissions.AsNoTracking().AsQueryable();
        if (!submissionScope.PermissionGranted) yearSubmissions = yearSubmissions.Where(_ => false);
        else if (!submissionScope.Unrestricted) yearSubmissions = ApplySubmissionScope(yearSubmissions, submissionScope);
        yearSubmissions = yearSubmissions.Where(item => yearTargets.Select(target => target.Id).Contains(item.IpmsTargetId));
        if (selectedYear != null)
            yearSubmissions = yearSubmissions.Where(item => item.ReportingPeriodId.HasValue && periodIds.Contains(item.ReportingPeriodId.Value));
        var selectedSubmissions = selectedPeriodWindow == null
            ? yearSubmissions
            : yearSubmissions.Where(item => item.ReportingPeriodId == selectedPeriodWindow.Period.Id
                && targets.Select(target => target.Id).Contains(item.IpmsTargetId));

        var targetSummary = await targets.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Active = group.Count(item => !item.IsWithdrawn)
        }).SingleOrDefaultAsync();
        var submissionSummary = await Summarize(selectedSubmissions);
        var achievedTargetIds = selectedSubmissions.Where(item => CompletedStatuses.Contains(item.Status))
            .Select(item => item.IpmsTargetId).Distinct();
        var atRiskTargetIds = selectedSubmissions.Where(item => AtRiskStatuses.Contains(item.Status))
            .Select(item => item.IpmsTargetId).Distinct();
        var achieved = await achievedTargetIds.CountAsync();
        var atRisk = await atRiskTargetIds.CountAsync();
        var nonOutstanding = selectedYear == null
            ? selectedSubmissions.Where(item => item.Status != "draft" && item.Status != "submitted").Select(item => item.IpmsTargetId).Distinct()
            : selectedSubmissions.Where(item => item.BaseState == SubmissionBaseStates.Submitted && !item.IsDisabled).Select(item => item.IpmsTargetId).Distinct();
        var outstanding = await targets.CountAsync(item => !item.IsWithdrawn && !nonOutstanding.Contains(item.Id));

        var scopedSubmissionIds = selectedSubmissions.Select(item => item.Id);
        var workflowInstances = context.SubmissionWorkflowInstances.AsNoTracking()
            .Where(item => item.SubmissionKind == SubmissionKind.Ipms && scopedSubmissionIds.Contains(item.SubmissionId));
        var workflowSubmissionIds = workflowInstances.Select(item => item.SubmissionId);
        var pendingVerification = await workflowInstances.CountAsync(item => item.State != WorkflowInstanceState.Completed
            && item.CurrentStage != null && item.CurrentStage.RequiredActionCode.Contains("VERIFY"));
        pendingVerification += await selectedSubmissions.CountAsync(item => !workflowSubmissionIds.Contains(item.Id)
            && (item.Status == "submitted" || item.Status == "pending_verification"));
        var pendingApproval = await workflowInstances.CountAsync(item => item.State != WorkflowInstanceState.Completed
            && item.CurrentStage != null && item.CurrentStage.RequiredActionCode.Contains("APPROV"));
        pendingApproval += await selectedSubmissions.CountAsync(item => !workflowSubmissionIds.Contains(item.Id)
            && (item.Status == "verified" || item.Status == "pending_approval"));

        var ratingLabels = await (from rating in context.SubmissionStageRatings.AsNoTracking()
            join instance in workflowInstances on rating.SubmissionWorkflowInstanceId equals instance.Id
            select rating.LabelSnapshot).ToArrayAsync();
        var ratingBreakdown = ratingLabels.GroupBy(label => label)
            .Select(group => new PerformanceDashboardRatingBreakdownResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count).ThenBy(item => item.Label)
            .ToArray();

        var teamTotals = await targets.Where(item => !item.IsWithdrawn)
            .GroupBy(item => new
            {
                PublicId = item.Department == null ? (Guid?)null : item.Department.PublicId,
                Name = item.Department == null ? "Unassigned" : item.Department.Name
            })
            .Select(group => new { group.Key.PublicId, group.Key.Name, Count = group.Count() })
            .OrderByDescending(item => item.Count).ThenBy(item => item.Name)
            .ToArrayAsync();
        var teamAchieved = await targets.Where(item => !item.IsWithdrawn && achievedTargetIds.Contains(item.Id))
            .GroupBy(item => new
            {
                PublicId = item.Department == null ? (Guid?)null : item.Department.PublicId,
                Name = item.Department == null ? "Unassigned" : item.Department.Name
            })
            .Select(group => new { group.Key.PublicId, group.Key.Name, Count = group.Count() })
            .ToArrayAsync();
        var teamAtRisk = await targets.Where(item => !item.IsWithdrawn && atRiskTargetIds.Contains(item.Id))
            .GroupBy(item => new
            {
                PublicId = item.Department == null ? (Guid?)null : item.Department.PublicId,
                Name = item.Department == null ? "Unassigned" : item.Department.Name
            })
            .Select(group => new { group.Key.PublicId, group.Key.Name, Count = group.Count() })
            .ToArrayAsync();
        var teamBreakdown = teamTotals.Select(item => new PerformanceDashboardTeamBreakdownResponse(
            item.PublicId,
            item.Name,
            item.Count,
            teamAchieved.SingleOrDefault(value => value.PublicId == item.PublicId && value.Name == item.Name)?.Count ?? 0,
            teamAtRisk.SingleOrDefault(value => value.PublicId == item.PublicId && value.Name == item.Name)?.Count ?? 0))
            .ToArray();

        var periodTargetCounts = periodIds.Length == 0
            ? []
            : await context.PerformancePeriodTargets.AsNoTracking()
                .Where(item => item.IpmsTargetId != null && item.IsActive && periodIds.Contains(item.ReportingPeriodId)
                    && yearTargets.Where(target => !target.IsWithdrawn).Select(target => target.Id).Contains(item.IpmsTargetId))
                .GroupBy(item => item.ReportingPeriodId)
                .Select(group => new { ReportingPeriodId = group.Key, Count = group.Select(item => item.IpmsTargetId).Distinct().Count() })
                .ToArrayAsync();
        var periodSubmissionCounts = periodIds.Length == 0
            ? []
            : await yearSubmissions.Where(item => item.ReportingPeriodId.HasValue)
                .GroupBy(item => item.ReportingPeriodId!.Value)
                .Select(group => new
                {
                    ReportingPeriodId = group.Key,
                    Count = group.Count(),
                    Achieved = group.Where(item => CompletedStatuses.Contains(item.Status)).Select(item => item.IpmsTargetId).Distinct().Count(),
                    AtRisk = group.Where(item => AtRiskStatuses.Contains(item.Status)).Select(item => item.IpmsTargetId).Distinct().Count(),
                    NonOutstanding = group.Where(item => item.BaseState == SubmissionBaseStates.Submitted && !item.IsDisabled).Select(item => item.IpmsTargetId).Distinct().Count()
                })
                .ToArrayAsync();
        var periodBreakdown = periodWindows.Select(item =>
        {
            var targetCount = periodTargetCounts.SingleOrDefault(count => count.ReportingPeriodId == item.Period.Id)?.Count ?? 0;
            var submissionCount = periodSubmissionCounts.SingleOrDefault(count => count.ReportingPeriodId == item.Period.Id);
            return new PerformanceDashboardPeriodBreakdownResponse(
                item.Period.PublicId,
                item.Period.Code,
                item.Period.Name,
                item.Period.Sequence,
                WindowState(item.Window, now),
                item.Window?.OpensAt,
                item.Window?.ClosesAt,
                submissionCount?.Count ?? 0,
                submissionCount?.Achieved ?? 0,
                submissionCount?.AtRisk ?? 0,
                Math.Max(0, targetCount - (submissionCount?.NonOutstanding ?? 0)));
        }).ToArray();

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
            pendingVerification,
            pendingApproval,
            selectedYear?.PublicId,
            selectedYear?.FinancialYear.Code,
            selectedYear?.FinancialYear.Name,
            selectedPeriodWindow?.Period.PublicId,
            selectedPeriodWindow?.Period.Code,
            selectedPeriodWindow?.Period.Name,
            selectedPeriodWindow == null ? null : WindowState(selectedPeriodWindow.Window, now),
            selectedPeriodWindow?.Window?.OpensAt,
            selectedPeriodWindow?.Window?.ClosesAt,
            selectedYear == null ? null : ratingBreakdown,
            selectedYear == null ? null : teamBreakdown,
            selectedYear == null ? null : periodBreakdown)));
    }

    private static readonly string[] CompletedStatuses = ["completed", "approved"];
    private static readonly string[] AtRiskStatuses = ["returned_for_info", "rejected", "verify_rejected"];

    private static string WindowState(ReportingWindow? window, DateTime now)
    {
        if (window == null) return "Not configured";
        if (!window.IsActive || now > window.ClosesAt) return "Closed";
        if (now < window.OpensAt) return "Upcoming";
        return "Open";
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
            group.Count(item => item.Status == "approved" || item.Status == "completed"),
            group.Count(item => item.Status == "submitted" || item.Status == "pending_verification"),
            group.Count(item => item.Status == "verified" || item.Status == "pending_approval"),
            group.Where(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected").Select(item => item.OpmsTargetId).Distinct().Count()))
            .SingleOrDefaultAsync() ?? SubmissionSummary.Empty;

    private static async Task<SubmissionSummary> Summarize(IQueryable<IpmsSubmission> query) =>
        await query.GroupBy(_ => 1).Select(group => new SubmissionSummary(
            group.Count(item => item.Status == "draft"),
            group.Count(item => item.Status == "submitted"),
            group.Count(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected"),
            group.Count(item => item.Status == "approved" || item.Status == "completed"),
            group.Count(item => item.Status == "submitted" || item.Status == "pending_verification"),
            group.Count(item => item.Status == "verified" || item.Status == "pending_approval"),
            group.Where(item => item.Status == "returned_for_info" || item.Status == "rejected" || item.Status == "verify_rejected").Select(item => item.IpmsTargetId).Distinct().Count()))
            .SingleOrDefaultAsync() ?? SubmissionSummary.Empty;

    private sealed record SubmissionSummary(int Draft, int Submitted, int Returned, int Approved, int PendingVerification, int PendingApproval, int ReturnedTargets)
    {
        public static SubmissionSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);
    }

    private sealed record PeriodWindow(ReportingPeriod Period, ReportingWindow? Window);
}

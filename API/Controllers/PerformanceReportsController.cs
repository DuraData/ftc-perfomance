using System.Security.Claims;
using System.Text;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class PerformanceReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAccessControlService accessControl, ITenantContext tenantContext, IWorkflowGovernanceService governance) : ControllerBase
{
    [HttpGet("performance-summary")]
    public async Task<ActionResult<ApiResponse<PerformanceReportSummaryDto>>> Summary([FromQuery] SubmissionKind kind, [FromQuery] Guid? reportingPeriodPublicId = null)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(Fail<PerformanceReportSummaryDto>("Select a municipality context before reporting."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceReportSummaryDto>("User not found."));
        var permission = kind == SubmissionKind.Opms ? "OPMS_REPORT.GENERATE" : "IPMS_REPORT.GENERATE";
        var scope = await accessControl.GetQueryScopeAsync(user, permission);
        if (!scope.PermissionGranted) return StatusCode(StatusCodes.Status403Forbidden, Fail<PerformanceReportSummaryDto>("Report permission is denied."));
        if (kind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return BadRequest(Fail<PerformanceReportSummaryDto>("Unsupported report type."));

        if (kind == SubmissionKind.Opms)
        {
            var targets = ApplyScope(context.PerformancePeriodTargets.AsNoTracking().Where(x => x.OpmsTargetId != null), scope, true);
            var submissions = ApplyScope(context.OpmsSubmissions.AsNoTracking(), scope);
            if (reportingPeriodPublicId.HasValue) { targets = targets.Where(x => x.ReportingPeriod.PublicId == reportingPeriodPublicId); submissions = submissions.Where(x => x.ReportingPeriod != null && x.ReportingPeriod.PublicId == reportingPeriodPublicId); }
            return Ok(new ApiResponse<PerformanceReportSummaryDto>(true, await CompleteSummary(await BuildOpmsSummary(targets, submissions, reportingPeriodPublicId), user)));
        }
        else
        {
            var targets = ApplyScope(context.PerformancePeriodTargets.AsNoTracking().Where(x => x.IpmsTargetId != null), scope, false);
            var submissions = ApplyScope(context.IpmsSubmissions.AsNoTracking(), scope);
            if (reportingPeriodPublicId.HasValue) { targets = targets.Where(x => x.ReportingPeriod.PublicId == reportingPeriodPublicId); submissions = submissions.Where(x => x.ReportingPeriod != null && x.ReportingPeriod.PublicId == reportingPeriodPublicId); }
            return Ok(new ApiResponse<PerformanceReportSummaryDto>(true, await CompleteSummary(await BuildIpmsSummary(targets, submissions, reportingPeriodPublicId), user)));
        }
    }

    [HttpGet("performance.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] SubmissionKind kind, [FromQuery] Guid? reportingPeriodPublicId = null)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(Fail<object>("Select a municipality context before reporting."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<object>("User not found."));
        var permission = kind == SubmissionKind.Opms ? "OPMS_REPORT.EXPORT" : "IPMS_REPORT.EXPORT";
        var scope = await accessControl.GetQueryScopeAsync(user, permission);
        if (!scope.PermissionGranted) return StatusCode(StatusCodes.Status403Forbidden, Fail<object>("Report export permission is denied."));
        if (kind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return BadRequest(Fail<object>("Unsupported report type."));
        ReportRow[] rows;
        if (kind == SubmissionKind.Opms)
        {
            var query = ApplyScope(context.OpmsSubmissions.AsNoTracking(), scope);
            if (reportingPeriodPublicId.HasValue) query = query.Where(x => x.ReportingPeriod != null && x.ReportingPeriod.PublicId == reportingPeriodPublicId);
            rows = await query
                .OrderBy(x => x.ReportingPeriod != null && (x.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3 || x.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4 || x.ReportingPeriod.PeriodType == ReportingPeriodType.Annual) ? x.OpmsTarget.RevisedOrderNumber : x.OpmsTarget.OriginalOrderNumber)
                .ThenBy(x => x.OpmsTarget.PublicId)
                .Select(x => new ReportRow(x.OpmsTarget.IndicatorNumber, x.OpmsTarget.TargetName, x.OpmsTarget.Department != null ? x.OpmsTarget.Department.Name : "", x.ReportingPeriod != null ? x.ReportingPeriod.Code : x.Quarter, x.ActualPerformance, x.Variance, x.AchievementPercent, x.TargetAchieved, x.Status)).Take(100001).ToArrayAsync();
        }
        else
        {
            var query = ApplyScope(context.IpmsSubmissions.AsNoTracking(), scope);
            if (reportingPeriodPublicId.HasValue) query = query.Where(x => x.ReportingPeriod != null && x.ReportingPeriod.PublicId == reportingPeriodPublicId);
            rows = await query
                .OrderBy(x => x.ReportingPeriod != null && (x.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3 || x.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4 || x.ReportingPeriod.PeriodType == ReportingPeriodType.Annual) ? x.IpmsTarget.RevisedOrderNumber : x.IpmsTarget.OriginalOrderNumber)
                .ThenBy(x => x.IpmsTarget.PublicId)
                .Select(x => new ReportRow(x.IpmsTarget.IndicatorNumber, x.IpmsTarget.TargetName, x.IpmsTarget.Department != null ? x.IpmsTarget.Department.Name : "", x.ReportingPeriod != null ? x.ReportingPeriod.Code : x.Quarter, x.ActualPerformance, x.Variance, x.AchievementPercent, x.TargetAchieved, x.Status)).Take(100001).ToArrayAsync();
        }
        if (rows.Length > 100000) return StatusCode(StatusCodes.Status413PayloadTooLarge, Fail<object>("Export exceeds 100,000 rows; select a reporting period."));
        var csv = new StringBuilder("Indicator,Target,Department,Period,Actual Performance,Variance,Achievement Percent,Target Achieved,Status\r\n");
        foreach (var row in rows) csv.AppendLine(string.Join(',', PerformanceReportCsv.Encode(row.Indicator), PerformanceReportCsv.Encode(row.TargetName), PerformanceReportCsv.Encode(row.Department), PerformanceReportCsv.Encode(row.Period), PerformanceReportCsv.Encode(row.ActualPerformance), PerformanceReportCsv.Encode(row.Variance?.ToString(System.Globalization.CultureInfo.InvariantCulture)), PerformanceReportCsv.Encode(row.AchievementPercent?.ToString(System.Globalization.CultureInfo.InvariantCulture)), PerformanceReportCsv.Encode(row.TargetAchieved?.ToString()), PerformanceReportCsv.Encode(row.Status)));
        governance.QueueAuditTrail("PerformanceReport", kind.ToString(), "ExportCsv", null, new { reportingPeriodPublicId, rowCount = rows.Length, format = "csv" }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        await context.SaveChangesAsync();
        var fileName = $"{kind.ToString().ToLowerInvariant()}-performance-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", fileName);
    }

    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private async Task<PerformanceReportSummaryDto> CompleteSummary(PerformanceReportSummaryDto result, ApplicationUser user)
    {
        governance.QueueAuditTrail("PerformanceReport", result.SubmissionKind.ToString(), "Generate", null, new { result.ReportingPeriodPublicId, result.TargetCount, result.SubmissionCount }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        await context.SaveChangesAsync();
        return result;
    }
    private static IQueryable<PerformancePeriodTarget> ApplyScope(IQueryable<PerformancePeriodTarget> query, AccessQueryScopeResult scope, bool opms) => scope.Unrestricted ? query : opms
        ? query.Where(x => x.OpmsTarget != null && (scope.DepartmentIds.Contains(x.OpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(x.OpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(x.OpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(x.OpmsTargetId!)))
        : query.Where(x => x.IpmsTarget != null && (scope.DepartmentIds.Contains(x.IpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(x.IpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(x.IpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(x.IpmsTargetId!)));
    private static IQueryable<OpmsSubmission> ApplyScope(IQueryable<OpmsSubmission> query, AccessQueryScopeResult scope) => scope.Unrestricted ? query : query.Where(x => scope.DepartmentIds.Contains(x.OpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(x.OpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(x.OpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(x.OpmsTargetId));
    private static IQueryable<IpmsSubmission> ApplyScope(IQueryable<IpmsSubmission> query, AccessQueryScopeResult scope) => scope.Unrestricted ? query : query.Where(x => scope.DepartmentIds.Contains(x.IpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(x.IpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(x.IpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(x.IpmsTargetId));

    private static async Task<PerformanceReportSummaryDto> BuildOpmsSummary(IQueryable<PerformancePeriodTarget> targets, IQueryable<OpmsSubmission> submissions, Guid? period) => BuildSummary(SubmissionKind.Opms, period, await targets.CountAsync(), await submissions.CountAsync(), await submissions.CountAsync(x => x.TargetAchieved == true), await submissions.CountAsync(x => x.TargetAchieved == false), await submissions.CountAsync(x => x.TargetAchieved == null), await submissions.AverageAsync(x => (decimal?)x.AchievementPercent), await submissions.GroupBy(x => x.OpmsTarget.Department != null ? x.OpmsTarget.Department.Name : "Unassigned").Select(x => new DepartmentReportDto(x.Key, x.Count(), x.Count(s => s.TargetAchieved == true), x.Average(s => (decimal?)s.AchievementPercent))).ToArrayAsync());
    private static async Task<PerformanceReportSummaryDto> BuildIpmsSummary(IQueryable<PerformancePeriodTarget> targets, IQueryable<IpmsSubmission> submissions, Guid? period) => BuildSummary(SubmissionKind.Ipms, period, await targets.CountAsync(), await submissions.CountAsync(), await submissions.CountAsync(x => x.TargetAchieved == true), await submissions.CountAsync(x => x.TargetAchieved == false), await submissions.CountAsync(x => x.TargetAchieved == null), await submissions.AverageAsync(x => (decimal?)x.AchievementPercent), await submissions.GroupBy(x => x.IpmsTarget.Department != null ? x.IpmsTarget.Department.Name : "Unassigned").Select(x => new DepartmentReportDto(x.Key, x.Count(), x.Count(s => s.TargetAchieved == true), x.Average(s => (decimal?)s.AchievementPercent))).ToArrayAsync());
    private static PerformanceReportSummaryDto BuildSummary(SubmissionKind kind, Guid? period, int targets, int submissions, int achieved, int atRisk, int pending, decimal? average, DepartmentReportDto[] departments) => new(kind, period, DateTime.UtcNow, targets, submissions, achieved, atRisk, pending, average, departments.OrderBy(x => x.Department).ToArray());
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private sealed record ReportRow(string Indicator, string TargetName, string Department, string Period, string? ActualPerformance, decimal? Variance, decimal? AchievementPercent, bool? TargetAchieved, string Status);
}

public sealed record PerformanceReportSummaryDto(SubmissionKind SubmissionKind, Guid? ReportingPeriodPublicId, DateTime GeneratedAt, int TargetCount, int SubmissionCount, int AchievedCount, int AtRiskCount, int PendingCount, decimal? AverageAchievementPercent, DepartmentReportDto[] Departments);
public sealed record DepartmentReportDto(string Department, int SubmissionCount, int AchievedCount, decimal? AverageAchievementPercent);

public static class PerformanceReportCsv
{
    public static string Encode(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && "=+-@".Contains(safe[0])) safe = "'" + safe;
        return '"' + safe.Replace("\"", "\"\"") + '"';
    }
}

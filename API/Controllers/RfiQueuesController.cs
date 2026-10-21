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
[Route("api/v1/rfi-queues")]
[Authorize]
public sealed class RfiQueuesController(
    ApplicationDbContext context,
    IAccessControlService accessControl) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<RfiQueueResponse>>> Get([FromQuery] RfiQueueQueryRequest request)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<RfiQueueResponse>(false, null, "User not found"));

        var records = OpmsRecords(
                await accessControl.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"),
                await accessControl.GetQueryScopeAsync(user, "OPMS_RFI.READ"))
            .Concat(IpmsRecords(
                await accessControl.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ"),
                await accessControl.GetQueryScopeAsync(user, "IPMS_RFI.READ")));

        var filtered = ApplyContextFilters(records, request);
        var now = DateTime.UtcNow;
        var counts = await Summarize(filtered, now);
        filtered = ApplyStatusFilter(filtered, request.NormalizedStatus, now);
        var totalCount = await filtered.CountAsync();
        var rows = await ApplyOrdering(filtered, request, now)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .ToListAsync();

        var responseRows = new List<RfiQueueItemResponse>(rows.Count);
        foreach (var item in rows)
        {
            var prefix = item.Kind == "opms" ? "OPMS" : "IPMS";
            var rfiResource = $"{prefix}_RFI";
            var recordScope = new AccessScopeContext(
                item.DepartmentId,
                item.UnitId,
                item.AssignedUserId,
                TargetId: item.SubmissionId,
                MunicipalityId: item.MunicipalityId);
            var codes = new[]
            {
                $"{rfiResource}.Question.READ",
                $"{rfiResource}.RESPOND",
                $"{rfiResource}.Response.UPDATE",
                $"{prefix}_WORKFLOW.ActionComment.UPDATE",
                $"{rfiResource}.CLOSE"
            };
            var permissions = await accessControl.CheckPermissionsAsync(user, codes, recordScope);
            var canRespond = !item.RespondedAt.HasValue && !item.ClosedAt.HasValue
                && permissions[codes[1]].Allowed
                && permissions[codes[2]].Allowed
                && permissions[codes[3]].Allowed;
            var canClose = item.RespondedAt.HasValue && !item.ClosedAt.HasValue
                && permissions[codes[4]].Allowed;
            responseRows.Add(new RfiQueueItemResponse(
                item.PublicId,
                item.SubmissionPublicId,
                item.Kind,
                item.TargetPublicId,
                item.TargetName,
                item.IndicatorNumber,
                item.Period,
                item.MunicipalityFinancialYearPublicId,
                permissions[codes[0]].Allowed ? item.Question : null,
                item.RaisedAt,
                item.ResponseDueAt,
                Status(item, now),
                canRespond,
                canClose));
        }

        return Ok(new ApiResponse<RfiQueueResponse>(true, new(
            counts,
            PagedResponse<RfiQueueItemResponse>.Create(responseRows, request.Page, request.PageSize, totalCount))));
    }

    private IQueryable<RfiQueueRecord> OpmsRecords(AccessQueryScopeResult submissionScope, AccessQueryScopeResult rfiScope)
    {
        var submissions = ApplyScope(context.OpmsSubmissions.AsNoTracking(), submissionScope);
        submissions = ApplyScope(submissions, rfiScope);
        return from submission in submissions
               join instance in context.SubmissionWorkflowInstances.AsNoTracking()
                   on submission.Id equals instance.SubmissionId
               join rfi in context.PerformanceRfis.AsNoTracking()
                   on instance.Id equals rfi.SubmissionWorkflowInstanceId
               where instance.SubmissionKind == SubmissionKind.Opms
               select new RfiQueueRecord
               {
                   Id = rfi.Id,
                   PublicId = rfi.PublicId,
                   MunicipalityId = rfi.MunicipalityId,
                   SubmissionId = submission.Id,
                   SubmissionPublicId = submission.PublicId,
                   Kind = "opms",
                   TargetPublicId = submission.OpmsTarget.PublicId,
                   TargetName = submission.ReportingPeriod != null
                       && (submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                       && submission.OpmsTarget.IsTargetNameRevised
                       && submission.OpmsTarget.RevisedTargetName != null
                           ? submission.OpmsTarget.RevisedTargetName
                           : submission.OpmsTarget.TargetName,
                   IndicatorNumber = submission.ReportingPeriod != null
                       && (submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                       && submission.OpmsTarget.IsIndicatorNumberRevised
                       && submission.OpmsTarget.RevisedIndicatorNumber != null
                           ? submission.OpmsTarget.RevisedIndicatorNumber
                           : submission.OpmsTarget.IndicatorNumber,
                   Period = submission.ReportingPeriod == null ? submission.Quarter : submission.ReportingPeriod.Code,
                   MunicipalityFinancialYearPublicId = submission.ReportingPeriod == null
                       ? null
                       : submission.ReportingPeriod.MunicipalityFinancialYear.PublicId,
                   DepartmentId = submission.OpmsTarget.DepartmentId,
                   UnitId = submission.OpmsTarget.UnitId,
                   AssignedUserId = submission.OpmsTarget.AssignedUserId,
                   Question = rfi.Question,
                   RaisedAt = rfi.RaisedAt,
                   ResponseDueAt = rfi.ResponseDueAt,
                   RespondedAt = rfi.RespondedAt,
                   ClosedAt = rfi.ClosedAt
               };
    }

    private IQueryable<RfiQueueRecord> IpmsRecords(AccessQueryScopeResult submissionScope, AccessQueryScopeResult rfiScope)
    {
        var submissions = ApplyScope(context.IpmsSubmissions.AsNoTracking(), submissionScope);
        submissions = ApplyScope(submissions, rfiScope);
        return from submission in submissions
               join instance in context.SubmissionWorkflowInstances.AsNoTracking()
                   on submission.Id equals instance.SubmissionId
               join rfi in context.PerformanceRfis.AsNoTracking()
                   on instance.Id equals rfi.SubmissionWorkflowInstanceId
               where instance.SubmissionKind == SubmissionKind.Ipms
               select new RfiQueueRecord
               {
                   Id = rfi.Id,
                   PublicId = rfi.PublicId,
                   MunicipalityId = rfi.MunicipalityId,
                   SubmissionId = submission.Id,
                   SubmissionPublicId = submission.PublicId,
                   Kind = "ipms",
                   TargetPublicId = submission.IpmsTarget.PublicId,
                   TargetName = submission.ReportingPeriod != null
                       && (submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                       && submission.IpmsTarget.IsTargetNameRevised
                       && submission.IpmsTarget.RevisedTargetName != null
                           ? submission.IpmsTarget.RevisedTargetName
                           : submission.IpmsTarget.TargetName,
                   IndicatorNumber = submission.ReportingPeriod != null
                       && (submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4
                           || submission.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                       && submission.IpmsTarget.IsIndicatorNumberRevised
                       && submission.IpmsTarget.RevisedIndicatorNumber != null
                           ? submission.IpmsTarget.RevisedIndicatorNumber
                           : submission.IpmsTarget.IndicatorNumber,
                   Period = submission.ReportingPeriod == null ? submission.Quarter : submission.ReportingPeriod.Code,
                   MunicipalityFinancialYearPublicId = submission.ReportingPeriod == null
                       ? null
                       : submission.ReportingPeriod.MunicipalityFinancialYear.PublicId,
                   DepartmentId = submission.IpmsTarget.DepartmentId,
                   UnitId = submission.IpmsTarget.UnitId,
                   AssignedUserId = submission.IpmsTarget.AssignedUserId,
                   Question = rfi.Question,
                   RaisedAt = rfi.RaisedAt,
                   ResponseDueAt = rfi.ResponseDueAt,
                   RespondedAt = rfi.RespondedAt,
                   ClosedAt = rfi.ClosedAt
               };
    }

    private static IQueryable<OpmsSubmission> ApplyScope(IQueryable<OpmsSubmission> query, AccessQueryScopeResult scope)
    {
        if (!scope.PermissionGranted) return query.Where(_ => false);
        if (scope.Unrestricted) return query;
        return query.Where(item =>
            (item.OpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.OpmsTarget.DepartmentId.Value))
            || (item.OpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.OpmsTarget.UnitId.Value))
            || (item.OpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.OpmsTarget.AssignedUserId))
            || scope.TargetIds.Contains(item.OpmsTargetId)
            || scope.KpiIds.Contains(item.OpmsTargetId));
    }

    private static IQueryable<IpmsSubmission> ApplyScope(IQueryable<IpmsSubmission> query, AccessQueryScopeResult scope)
    {
        if (!scope.PermissionGranted) return query.Where(_ => false);
        if (scope.Unrestricted) return query;
        return query.Where(item =>
            (item.IpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.IpmsTarget.DepartmentId.Value))
            || (item.IpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.IpmsTarget.UnitId.Value))
            || (item.IpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.IpmsTarget.AssignedUserId))
            || scope.TargetIds.Contains(item.IpmsTargetId)
            || scope.KpiIds.Contains(item.IpmsTargetId));
    }

    private static IQueryable<RfiQueueRecord> ApplyContextFilters(
        IQueryable<RfiQueueRecord> query,
        RfiQueueQueryRequest request)
    {
        if (request.NormalizedSearch is { } search)
            query = query.Where(item => item.TargetName.Contains(search) || item.IndicatorNumber.Contains(search));
        if (request.MunicipalityFinancialYearPublicId.HasValue)
            query = query.Where(item => item.MunicipalityFinancialYearPublicId == request.MunicipalityFinancialYearPublicId.Value);
        if (request.NormalizedKind != "all")
            query = query.Where(item => item.Kind == request.NormalizedKind);
        if (request.NormalizedPeriod is { } period)
            query = query.Where(item => item.Period == period);
        return query;
    }

    private static IQueryable<RfiQueueRecord> ApplyStatusFilter(
        IQueryable<RfiQueueRecord> query,
        string status,
        DateTime now) => status switch
        {
            "open" => query.Where(item => !item.RespondedAt.HasValue && !item.ClosedAt.HasValue && item.ResponseDueAt >= now),
            "responded" => query.Where(item => item.RespondedAt.HasValue && !item.ClosedAt.HasValue && item.ResponseDueAt >= now),
            "overdue" => query.Where(item => !item.ClosedAt.HasValue && item.ResponseDueAt < now),
            "closed" => query.Where(item => item.ClosedAt.HasValue),
            _ => query
        };

    private static IOrderedQueryable<RfiQueueRecord> ApplyOrdering(
        IQueryable<RfiQueueRecord> query,
        RfiQueueQueryRequest request,
        DateTime now)
    {
        var ordered = (request.NormalizedSortBy, request.SortDescending) switch
        {
            ("raisedat", false) => query.OrderBy(item => item.RaisedAt),
            ("raisedat", true) => query.OrderByDescending(item => item.RaisedAt),
            ("targetname", false) => query.OrderBy(item => item.TargetName),
            ("targetname", true) => query.OrderByDescending(item => item.TargetName),
            ("indicatornumber", false) => query.OrderBy(item => item.IndicatorNumber),
            ("indicatornumber", true) => query.OrderByDescending(item => item.IndicatorNumber),
            ("period", false) => query.OrderBy(item => item.Period),
            ("period", true) => query.OrderByDescending(item => item.Period),
            ("status", false) => query.OrderBy(item => item.ClosedAt.HasValue ? 3 : item.ResponseDueAt < now ? 2 : item.RespondedAt.HasValue ? 1 : 0),
            ("status", true) => query.OrderByDescending(item => item.ClosedAt.HasValue ? 3 : item.ResponseDueAt < now ? 2 : item.RespondedAt.HasValue ? 1 : 0),
            ("dueat", true) => query.OrderByDescending(item => item.ResponseDueAt),
            _ => query.OrderBy(item => item.ResponseDueAt)
        };
        return ordered.ThenBy(item => item.Kind).ThenBy(item => item.Id);
    }

    private static async Task<RfiQueueCountsResponse> Summarize(IQueryable<RfiQueueRecord> query, DateTime now) =>
        await query.GroupBy(_ => 1).Select(group => new RfiQueueCountsResponse(
            group.Count(item => !item.RespondedAt.HasValue && !item.ClosedAt.HasValue && item.ResponseDueAt >= now),
            group.Count(item => item.RespondedAt.HasValue && !item.ClosedAt.HasValue && item.ResponseDueAt >= now),
            group.Count(item => !item.ClosedAt.HasValue && item.ResponseDueAt < now),
            group.Count(item => item.ClosedAt.HasValue)))
            .SingleOrDefaultAsync() ?? RfiQueueCountsResponse.Empty;

    private static string Status(RfiQueueRecord item, DateTime now) => item.ClosedAt.HasValue
        ? "closed"
        : item.ResponseDueAt < now
            ? "overdue"
            : item.RespondedAt.HasValue
                ? "responded"
                : "open";

    private async Task<ApplicationUser?> CurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : await context.Users.SingleOrDefaultAsync(item => item.Id == userId);
    }

    private sealed class RfiQueueRecord
    {
        public long Id { get; init; }
        public Guid PublicId { get; init; }
        public long MunicipalityId { get; init; }
        public string SubmissionId { get; init; } = string.Empty;
        public Guid SubmissionPublicId { get; init; }
        public string Kind { get; init; } = string.Empty;
        public Guid TargetPublicId { get; init; }
        public string TargetName { get; init; } = string.Empty;
        public string IndicatorNumber { get; init; } = string.Empty;
        public string Period { get; init; } = string.Empty;
        public Guid? MunicipalityFinancialYearPublicId { get; init; }
        public int? DepartmentId { get; init; }
        public int? UnitId { get; init; }
        public string? AssignedUserId { get; init; }
        public string Question { get; init; } = string.Empty;
        public DateTime RaisedAt { get; init; }
        public DateTime ResponseDueAt { get; init; }
        public DateTime? RespondedAt { get; init; }
        public DateTime? ClosedAt { get; init; }
    }
}

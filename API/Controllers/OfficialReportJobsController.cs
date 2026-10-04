using System.Security.Claims;
using FTCERP.Host.API.Requests;
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
[Route("api/v1/reports/official")]
[Authorize]
public sealed class OfficialReportJobsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IAccessControlService accessControl,
    ITenantContext tenantContext,
    IWorkflowGovernanceService governance) : ControllerBase
{
    [HttpGet("jobs")]
    public async Task<ActionResult<ApiResponse<OfficialReportJobResponse[]>>> Jobs([FromQuery] SubmissionKind kind)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportJobResponse[]>("User not found."));
        if (!await HasReportResourceAccess(user, kind, "READ")) return ForbidResponse<OfficialReportJobResponse[]>("Official report job history requires report, KPI and submission READ permission.");
        var rows = await context.OfficialReportJobs.AsNoTracking()
            .Include(item => item.OfficialReportSchedule).Include(item => item.ReportTemplate)
            .Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Include(item => item.ReportingPeriod).Include(item => item.Department).Include(item => item.Unit)
            .Include(item => item.RequestedByUser).Include(item => item.OfficialReportGeneration).Include(item => item.DistributionOutbox)
            .Where(item => item.ReportTemplate.SubmissionKind == kind).OrderByDescending(item => item.RequestedAt).Take(200).ToArrayAsync();
        var canReadAudit = await Granted(user, "Audit.Trails.View");
        return Ok(new ApiResponse<OfficialReportJobResponse[]>(true, rows.Where(item => item.ReportTemplate.ReportType != OfficialReportType.AuditTrail || canReadAudit).Select(Map).ToArray()));
    }

    [HttpPost("jobs")]
    public async Task<ActionResult<ApiResponse<OfficialReportJobResponse>>> Queue([FromBody] QueueOfficialReportJobRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(Fail<OfficialReportJobResponse>("Select a municipality context before queuing a report."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportJobResponse>("User not found."));
        OfficialReportGeneration? previous = null;
        if (request.PreviousGenerationPublicId.HasValue)
        {
            previous = await context.OfficialReportGenerations.SingleOrDefaultAsync(item => item.PublicId == request.PreviousGenerationPublicId);
            if (previous == null) return BadRequest(Fail<OfficialReportJobResponse>("The previous official generation was not found."));
        }
        var selection = await ResolveSelection(request.TemplatePublicId, request.MunicipalityFinancialYearPublicId, request.ReportingPeriodPublicId, request.DepartmentPublicId, request.UnitPublicId, previous != null);
        if (selection.Error != null) return BadRequest(Fail<OfficialReportJobResponse>(selection.Error));
        var template = selection.Template!;
        if (!await HasReportResourceAccess(user, template.SubmissionKind, "GENERATE")) return ForbidResponse<OfficialReportJobResponse>("Asynchronous generation requires report GENERATE plus KPI and submission READ permission.");
        if (template.ReportType == OfficialReportType.AuditTrail && !await Granted(user, "Audit.Trails.View")) return ForbidResponse<OfficialReportJobResponse>("The audit-trail report also requires audit-trail read permission.");
        if (previous != null)
        {
            if (previous.ReportTemplateId != template.Id || previous.MunicipalityFinancialYearId != selection.Year!.Id || previous.ReportingPeriodId != selection.Period!.Id)
                return BadRequest(Fail<OfficialReportJobResponse>("The previous generation does not belong to the selected template, year and period."));
        }
        if (previous == null && !template.IsCurrent) return Conflict(Fail<OfficialReportJobResponse>("Only the current template can start a new asynchronous report lineage."));
        var job = NewJob(template, selection.Year!, selection.Period!, selection.Department, selection.Unit, user.Id, DateTime.UtcNow, null, previous);
        context.OfficialReportJobs.Add(job);
        governance.QueueAuditTrail(nameof(OfficialReportJob), job.PublicId.ToString(), "Queue", null, new { TemplatePublicId = template.PublicId, FinancialYearPublicId = selection.Year!.PublicId, ReportingPeriodPublicId = selection.Period!.PublicId, PreviousGenerationPublicId = previous?.PublicId }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "Requested durable asynchronous official report generation.");
        await context.SaveChangesAsync();
        await LoadJob(job);
        return Accepted(new ApiResponse<OfficialReportJobResponse>(true, Map(job), "Official report generation was queued."));
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<ApiResponse<OfficialReportScheduleResponse[]>>> Schedules([FromQuery] SubmissionKind kind, [FromQuery] bool includeHistory = false)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportScheduleResponse[]>("User not found."));
        if (!await Granted(user, ConfigurePermission(kind))) return ForbidResponse<OfficialReportScheduleResponse[]>("Official report schedule configuration is denied.");
        var query = context.OfficialReportSchedules.AsNoTracking().IncludeAll().Where(item => item.ReportTemplate.SubmissionKind == kind);
        if (!includeHistory) query = query.Where(item => item.IsCurrent);
        var rows = await query.OrderBy(item => item.Code).ThenByDescending(item => item.VersionNumber).ToArrayAsync();
        return Ok(new ApiResponse<OfficialReportScheduleResponse[]>(true, rows.Select(Map).ToArray()));
    }

    [HttpPost("schedules")]
    public async Task<ActionResult<ApiResponse<OfficialReportScheduleResponse>>> SaveSchedule([FromBody] SaveOfficialReportScheduleRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(Fail<OfficialReportScheduleResponse>("Select a municipality context before configuring report schedules."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportScheduleResponse>("User not found."));
        var selection = await ResolveSelection(request.TemplatePublicId, request.MunicipalityFinancialYearPublicId, request.ReportingPeriodPublicId, request.DepartmentPublicId, request.UnitPublicId);
        if (selection.Error != null) return BadRequest(Fail<OfficialReportScheduleResponse>(selection.Error));
        var template = selection.Template!;
        if (!template.IsCurrent) return Conflict(Fail<OfficialReportScheduleResponse>("Only the current template can be scheduled."));
        if (!await Granted(user, ConfigurePermission(template.SubmissionKind)) || !await HasReportResourceAccess(user, template.SubmissionKind, "GENERATE"))
            return ForbidResponse<OfficialReportScheduleResponse>("Schedule administration requires report CONFIGURE and GENERATE plus KPI and submission READ permission.");
        var validation = await ValidateSchedule(request);
        if (validation != null) return BadRequest(Fail<OfficialReportScheduleResponse>(validation));

        OfficialReportSchedule? previous = null;
        var family = Guid.NewGuid();
        var version = 1;
        if (request.PreviousVersionPublicId.HasValue)
        {
            previous = await context.OfficialReportSchedules.SingleOrDefaultAsync(item => item.PublicId == request.PreviousVersionPublicId);
            if (previous == null || !previous.IsCurrent) return Conflict(Fail<OfficialReportScheduleResponse>("The selected schedule is no longer current."));
            if (!TryRowVersion(request.PreviousVersionRowVersion, out var rowVersion)) return BadRequest(Fail<OfficialReportScheduleResponse>("The prior schedule RowVersion is required."));
            context.Entry(previous).Property(item => item.RowVersion).OriginalValue = rowVersion;
            previous.IsCurrent = false;
            previous.IsActive = false;
            family = previous.ScheduleFamilyPublicId;
            version = previous.VersionNumber + 1;
        }
        else if (await context.OfficialReportSchedules.AnyAsync(item => item.IsCurrent && item.Code == request.Code.Trim().ToUpperInvariant()))
            return Conflict(Fail<OfficialReportScheduleResponse>("A current report schedule with this code already exists."));

        var entity = new OfficialReportSchedule
        {
            MunicipalityId = tenantContext.MunicipalityId.Value, ScheduleFamilyPublicId = family, PreviousVersionId = previous?.Id, VersionNumber = version,
            PreviousVersion = previous,
            ReportTemplateId = template.Id, MunicipalityFinancialYearId = selection.Year!.Id, ReportingPeriodId = selection.Period!.Id,
            DepartmentId = selection.Department?.Id, UnitId = selection.Unit?.Id, Code = request.Code.Trim().ToUpperInvariant(), Name = request.Name.Trim(),
            Cadence = request.Cadence, Interval = request.Interval, NextRunAt = request.IsActive ? request.NextRunAt!.Value.ToUniversalTime() : request.NextRunAt?.ToUniversalTime(),
            EffectiveTo = request.EffectiveTo?.ToUniversalTime(), RecipientKind = request.RecipientKind,
            RecipientValuesCsv = string.Join(',', request.RecipientValues.Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)),
            ChannelsCsv = string.Join(',', request.Channels.Select(item => item.Trim().ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase)),
            IsMandatory = request.IsMandatory, IsActive = request.IsActive, ApprovalReference = request.ApprovalReference.Trim(), Reason = request.Reason.Trim(), CreatedByUserId = user.Id
        };
        context.OfficialReportSchedules.Add(entity);
        governance.QueueAuditTrail(nameof(OfficialReportSchedule), entity.PublicId.ToString(), previous == null ? "Create" : "CreateVersion", null, new { entity.ScheduleFamilyPublicId, entity.Code, entity.VersionNumber, entity.Cadence, entity.NextRunAt, entity.ChannelsCsv, entity.RecipientKind, entity.ApprovalReference }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), entity.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<OfficialReportScheduleResponse>("The schedule changed; refresh before creating another version.")); }
        await context.Entry(entity).Reference(item => item.ReportTemplate).LoadAsync();
        await context.Entry(entity).Reference(item => item.MunicipalityFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync();
        await context.Entry(entity).Reference(item => item.ReportingPeriod).LoadAsync();
        await context.Entry(entity).Reference(item => item.Department).LoadAsync();
        await context.Entry(entity).Reference(item => item.Unit).LoadAsync();
        await context.Entry(entity).Reference(item => item.CreatedByUser).LoadAsync();
        return Ok(new ApiResponse<OfficialReportScheduleResponse>(true, Map(entity)));
    }

    [HttpPost("schedules/{publicId:guid}/run")]
    public async Task<ActionResult<ApiResponse<OfficialReportJobResponse>>> RunSchedule(Guid publicId)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportJobResponse>("User not found."));
        var schedule = await context.OfficialReportSchedules.IncludeAll().SingleOrDefaultAsync(item => item.PublicId == publicId && item.IsCurrent);
        if (schedule == null) return NotFound(Fail<OfficialReportJobResponse>("Current report schedule was not found."));
        if (!await Granted(user, ConfigurePermission(schedule.ReportTemplate.SubmissionKind)) || !await HasReportResourceAccess(user, schedule.ReportTemplate.SubmissionKind, "GENERATE"))
            return ForbidResponse<OfficialReportJobResponse>("Running this schedule is denied.");
        var recipients = await ResolveRecipients(schedule);
        if (recipients.Length == 0) return BadRequest(Fail<OfficialReportJobResponse>("The schedule currently resolves no active tenant recipients."));
        var job = NewJob(schedule.ReportTemplate, schedule.MunicipalityFinancialYear, schedule.ReportingPeriod, schedule.Department, schedule.Unit, user.Id, DateTime.UtcNow, schedule);
        job.RecipientUserIdsCsv = string.Join(',', recipients);
        job.ChannelsCsv = schedule.ChannelsCsv;
        job.IsMandatoryDistribution = schedule.IsMandatory;
        context.OfficialReportJobs.Add(job);
        governance.QueueAuditTrail(nameof(OfficialReportSchedule), schedule.PublicId.ToString(), "RunNow", null, new { job.PublicId, Recipients = recipients.Length }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "Administrator requested immediate scheduled report execution.");
        await context.SaveChangesAsync();
        await LoadJob(job);
        return Accepted(new ApiResponse<OfficialReportJobResponse>(true, Map(job), "Scheduled report generation was queued."));
    }

    [HttpPost("jobs/{publicId:guid}/retry")]
    public async Task<ActionResult<ApiResponse<OfficialReportJobResponse>>> Retry(Guid publicId, [FromBody] RetryOfficialReportJobRequest request)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportJobResponse>("User not found."));
        var job = await context.OfficialReportJobs.Include(item => item.ReportTemplate).SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (job == null) return NotFound(Fail<OfficialReportJobResponse>("Official report job was not found."));
        if (!await Granted(user, ConfigurePermission(job.ReportTemplate.SubmissionKind))) return ForbidResponse<OfficialReportJobResponse>("Retrying failed report jobs is denied.");
        if (job.State != OfficialReportJobState.Failed) return Conflict(Fail<OfficialReportJobResponse>("Only failed jobs can be retried."));
        if (request.Reason.Trim().Length is < 5 or > 1000 || !TryRowVersion(request.RowVersion, out var rowVersion)) return BadRequest(Fail<OfficialReportJobResponse>("A retry reason of 5-1000 characters and valid RowVersion are required."));
        context.Entry(job).Property(item => item.RowVersion).OriginalValue = rowVersion;
        job.State = OfficialReportJobState.Queued; job.AvailableAt = DateTime.UtcNow; job.LastError = null; job.StartedAt = null; job.CompletedAt = null; job.RetryReason = request.Reason.Trim();
        governance.QueueAuditTrail(nameof(OfficialReportJob), job.PublicId.ToString(), "Retry", null, new { job.AttemptCount, job.AvailableAt }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), job.RetryReason);
        try { await context.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<OfficialReportJobResponse>("The job changed; refresh before retrying.")); }
        await LoadJob(job);
        return Ok(new ApiResponse<OfficialReportJobResponse>(true, Map(job), "Official report job queued for retry."));
    }

    private async Task<(OfficialReportTemplate? Template, MunicipalityFinancialYear? Year, ReportingPeriod? Period, Department? Department, Unit? Unit, string? Error)> ResolveSelection(Guid templateId, Guid yearId, Guid periodId, Guid? departmentId, Guid? unitId, bool allowHistoricTemplate = false)
    {
        var template = await context.OfficialReportTemplates.SingleOrDefaultAsync(item => item.PublicId == templateId && item.IsActive);
        if (template == null) return (null, null, null, null, null, "The official report template was not found.");
        var now = DateTime.UtcNow;
        if (!allowHistoricTemplate && (template.EffectiveFrom > now || template.EffectiveTo.HasValue && template.EffectiveTo < now)) return (null, null, null, null, null, "The official report template is not currently effective.");
        var year = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == yearId && item.IsActive);
        if (year == null || template.MunicipalityFinancialYearId.HasValue && template.MunicipalityFinancialYearId != year.Id) return (null, null, null, null, null, "The selected financial year is not valid for this template.");
        var period = await context.ReportingPeriods.SingleOrDefaultAsync(item => item.PublicId == periodId && item.MunicipalityFinancialYearId == year.Id && item.IsActive);
        if (period == null) return (null, null, null, null, null, "The selected reporting period was not found in this financial year.");
        var periodError = ValidatePeriod(template.ReportType, period.PeriodType);
        if (periodError != null) return (null, null, null, null, null, periodError);
        Department? department = null; Unit? unit = null;
        if (departmentId.HasValue) department = await context.Departments.SingleOrDefaultAsync(item => item.PublicId == departmentId && item.IsActive);
        if (departmentId.HasValue && department == null) return (null, null, null, null, null, "The selected department was not found.");
        if (unitId.HasValue) unit = await context.Units.SingleOrDefaultAsync(item => item.PublicId == unitId && item.IsActive);
        if (unitId.HasValue && unit == null) return (null, null, null, null, null, "The selected unit was not found.");
        if (department != null && unit != null && unit.DepartmentId != department.Id) return (null, null, null, null, null, "The selected unit does not belong to the selected department.");
        if (template.ReportType == OfficialReportType.DepartmentalPerformance && department == null) return (null, null, null, null, null, "A department is required for this report class.");
        if (template.ReportType == OfficialReportType.UnitPerformance && unit == null) return (null, null, null, null, null, "A unit is required for this report class.");
        return (template, year, period, department, unit, null);
    }

    private async Task<string?> ValidateSchedule(SaveOfficialReportScheduleRequest request)
    {
        if (request.Code.Trim().Length is < 1 or > 80 || request.Name.Trim().Length is < 1 or > 240) return "Schedule code and name are required and must fit their configured limits.";
        if (!Enum.IsDefined(request.Cadence) || request.Interval is < 1 or > 365) return "Select a supported cadence and an interval from 1 to 365.";
        if (!Enum.IsDefined(request.RecipientKind) || request.RecipientValues.Count == 0) return "Select a supported recipient type and at least one recipient value.";
        if (request.IsActive && !request.NextRunAt.HasValue) return "An active schedule requires its next UTC run time.";
        if (request.EffectiveTo.HasValue && request.NextRunAt.HasValue && request.EffectiveTo < request.NextRunAt) return "Effective-to cannot precede the next run.";
        if (request.ApprovalReference.Trim().Length is < 1 or > 240 || request.Reason.Trim().Length is < 5 or > 1000) return "Approval reference and a reason of 5-1000 characters are required.";
        var channels = request.Channels.Select(item => item.Trim().ToUpperInvariant()).Distinct().ToArray();
        if (channels.Length == 0 || channels.Any(item => item is not ("IN_APP" or "EMAIL" or "SMS"))) return "Channels must contain IN_APP, EMAIL or SMS.";
        var values = request.RecipientValues.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (values.Length == 0) return "At least one recipient value is required.";
        if (request.RecipientKind == OfficialReportRecipientKind.User && await context.Users.CountAsync(item => item.IsActive && values.Contains(item.Id)) != values.Length) return "Every recipient user must be active and belong to this municipality.";
        if (request.RecipientKind == OfficialReportRecipientKind.Role && await context.Roles.CountAsync(item => values.Contains(item.RoleCode)) != values.Length) return "Every recipient role code must exist.";
        return null;
    }

    private async Task<string[]> ResolveRecipients(OfficialReportSchedule schedule)
    {
        var values = Split(schedule.RecipientValuesCsv);
        if (schedule.RecipientKind == OfficialReportRecipientKind.User)
            return await context.Users.Where(item => item.IsActive && values.Contains(item.Id)).Select(item => item.Id).Distinct().ToArrayAsync();
        var now = DateTime.UtcNow;
        return await context.SecurityUserRoleAssignments.Where(item => item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now) && item.User.IsActive && values.Contains(item.Role.RoleCode)).Select(item => item.UserId).Distinct().ToArrayAsync();
    }

    private OfficialReportJob NewJob(OfficialReportTemplate template, MunicipalityFinancialYear year, ReportingPeriod period, Department? department, Unit? unit, string userId, DateTime scheduledFor, OfficialReportSchedule? schedule, OfficialReportGeneration? previous = null) => new()
    {
        MunicipalityId = tenantContext.MunicipalityId!.Value, OfficialReportScheduleId = schedule?.Id, ReportTemplateId = template.Id,
        MunicipalityFinancialYearId = year.Id, ReportingPeriodId = period.Id, DepartmentId = department?.Id, UnitId = unit?.Id,
        PreviousOfficialReportGenerationId = previous?.Id, State = OfficialReportJobState.Queued, ScheduledFor = scheduledFor,
        AvailableAt = DateTime.UtcNow, RequestedByUserId = userId, RequestedAt = DateTime.UtcNow
    };

    private async Task LoadJob(OfficialReportJob job)
    {
        await context.Entry(job).Reference(item => item.OfficialReportSchedule).LoadAsync();
        await context.Entry(job).Reference(item => item.ReportTemplate).LoadAsync();
        await context.Entry(job).Reference(item => item.MunicipalityFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync();
        await context.Entry(job).Reference(item => item.ReportingPeriod).LoadAsync();
        await context.Entry(job).Reference(item => item.Department).LoadAsync();
        await context.Entry(job).Reference(item => item.Unit).LoadAsync();
        await context.Entry(job).Reference(item => item.RequestedByUser).LoadAsync();
        await context.Entry(job).Reference(item => item.OfficialReportGeneration).LoadAsync();
        await context.Entry(job).Reference(item => item.DistributionOutbox).LoadAsync();
    }

    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private async Task<bool> Granted(ApplicationUser user, string permission) => (await accessControl.GetQueryScopeAsync(user, permission)).PermissionGranted;
    private async Task<bool> HasReportResourceAccess(ApplicationUser user, SubmissionKind kind, string reportAction) =>
        await Granted(user, $"{(kind == SubmissionKind.Opms ? "OPMS" : "IPMS")}_REPORT.{reportAction}")
        && await Granted(user, kind == SubmissionKind.Opms ? "OPMS_KPI.READ" : "IPMS_KPI.READ")
        && await Granted(user, kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ");
    private static string ConfigurePermission(SubmissionKind kind) => kind == SubmissionKind.Opms ? "OPMS_REPORT.CONFIGURE" : "IPMS_REPORT.CONFIGURE";
    private static string? ValidatePeriod(OfficialReportType type, ReportingPeriodType periodType) => type switch
    {
        OfficialReportType.QuarterlyPerformance when periodType is not (ReportingPeriodType.Quarter1 or ReportingPeriodType.Quarter2 or ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4) => "A quarterly report requires Q1, Q2, Q3 or Q4.",
        OfficialReportType.MidTermPerformance when periodType != ReportingPeriodType.MidTerm => "A Mid-Term report requires the governed Mid-Term period.",
        OfficialReportType.AnnualPerformance when periodType != ReportingPeriodType.Annual => "An Annual report requires the governed Annual period.",
        _ => null
    };
    private static string[] Split(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static bool TryRowVersion(string? value, out byte[] bytes) { try { bytes = Convert.FromBase64String(value ?? ""); return bytes.Length > 0; } catch (FormatException) { bytes = []; return false; } }
    private static OfficialReportScheduleResponse Map(OfficialReportSchedule item) => new(item.PublicId, item.ScheduleFamilyPublicId, item.VersionNumber, item.PreviousVersion?.PublicId,
        item.ReportTemplate.PublicId, item.ReportTemplate.Name, item.ReportTemplate.ReportType, item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code,
        item.ReportingPeriod.PublicId, item.ReportingPeriod.Code, item.Department?.PublicId, item.Department?.Name, item.Unit?.PublicId, item.Unit?.Name,
        item.Code, item.Name, item.Cadence, item.Interval, item.NextRunAt, item.EffectiveTo, item.RecipientKind, Split(item.RecipientValuesCsv), Split(item.ChannelsCsv), item.IsMandatory,
        item.IsCurrent, item.IsActive, item.ApprovalReference, item.Reason, item.CreatedByUser.UserName ?? item.CreatedByUser.Email ?? item.CreatedByUser.Id, item.CreatedAt, Convert.ToBase64String(item.RowVersion));
    private static OfficialReportJobResponse Map(OfficialReportJob item) => new(item.PublicId, item.OfficialReportSchedule?.PublicId, item.OfficialReportSchedule?.Name, item.State,
        item.ReportTemplate.PublicId, item.ReportTemplate.Name, item.ReportTemplate.ReportType, item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code,
        item.ReportingPeriod.PublicId, item.ReportingPeriod.Code, item.Department?.PublicId, item.Department?.Name, item.Unit?.PublicId, item.Unit?.Name,
        item.ScheduledFor, item.AvailableAt, item.AttemptCount, item.StartedAt, item.CompletedAt, item.LastError,
        item.RequestedByUser.UserName ?? item.RequestedByUser.Email ?? item.RequestedByUser.Id, item.RequestedAt,
        item.OfficialReportGeneration?.PublicId, item.OfficialReportGeneration?.FileName, item.DistributionOutbox?.PublicId,
        Split(item.RecipientUserIdsCsv), Split(item.ChannelsCsv), item.IsMandatoryDistribution, item.RetryReason, Convert.ToBase64String(item.RowVersion));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ObjectResult ForbidResponse<T>(string message) => StatusCode(StatusCodes.Status403Forbidden, Fail<T>(message));
}

internal static class OfficialReportScheduleQueryExtensions
{
    public static IQueryable<OfficialReportSchedule> IncludeAll(this IQueryable<OfficialReportSchedule> query) => query
        .Include(item => item.PreviousVersion).Include(item => item.ReportTemplate)
        .Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
        .Include(item => item.ReportingPeriod).Include(item => item.Department).Include(item => item.Unit).Include(item => item.CreatedByUser);
}

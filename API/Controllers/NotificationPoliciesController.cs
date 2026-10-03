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
[Route("api/v1/notification-policies")]
[Authorize]
public sealed class NotificationPoliciesController(ApplicationDbContext context, ITenantContext tenantContext, INotificationPolicyService policies, IWorkflowGovernanceService governance) : ControllerBase
{
    [HttpGet("preferences/me")]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> GetMyPreferences()
    {
        if (!HasTenant()) return TenantRequired<NotificationPreferenceDto>();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(Fail<NotificationPreferenceDto>("User not found."));
        var entity = await context.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId);
        return Ok(new ApiResponse<NotificationPreferenceDto>(true, entity == null
            ? new(true, false, true, false, null)
            : ToDto(entity)));
    }

    [HttpPut("preferences/me")]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> SaveMyPreferences(SaveNotificationPreferenceRequest request)
    {
        if (!HasTenant()) return TenantRequired<NotificationPreferenceDto>();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(Fail<NotificationPreferenceDto>("User not found."));
        var entity = await context.NotificationPreferences.SingleOrDefaultAsync(x => x.UserId == userId);
        object? before = null;
        if (entity == null)
        {
            entity = new NotificationPreference { MunicipalityId = tenantContext.MunicipalityId!.Value, UserId = userId };
            context.NotificationPreferences.Add(entity);
        }
        else
        {
            before = new { entity.EmailEnabled, entity.SmsEnabled, entity.DailyDigestEnabled, entity.WeeklySummaryEnabled };
            if (string.IsNullOrWhiteSpace(request.RowVersion)) return Conflict(Fail<NotificationPreferenceDto>("Reload preferences before saving."));
            try { context.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
            catch (FormatException) { return BadRequest(Fail<NotificationPreferenceDto>("RowVersion is invalid.")); }
        }
        entity.EmailEnabled = request.EmailEnabled;
        entity.SmsEnabled = request.SmsEnabled;
        entity.DailyDigestEnabled = request.DailyDigestEnabled;
        entity.WeeklySummaryEnabled = request.WeeklySummaryEnabled;
        entity.UpdatedAt = DateTime.UtcNow;
        governance.QueueAuditTrail(nameof(NotificationPreference), entity.PublicId.ToString(), before == null ? "Create" : "Update", before, new { entity.EmailEnabled, entity.SmsEnabled, entity.DailyDigestEnabled, entity.WeeklySummaryEnabled }, userId, null, "User notification preference update; mandatory operational notifications remain enabled.");
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<NotificationPreferenceDto>("Preferences changed; reload before saving.")); }
        return Ok(new ApiResponse<NotificationPreferenceDto>(true, ToDto(entity), "Optional notification preferences saved. Mandatory operational notifications cannot be disabled."));
    }

    [HttpGet]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<NotificationPolicyDto[]>>> GetAll()
    {
        if (!HasTenant()) return TenantRequired<NotificationPolicyDto[]>();
        var values = await context.NotificationConfigurations.AsNoTracking().Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).Include(x => x.ReportingPeriod).Include(x => x.Rules)
            .OrderBy(x => x.Code).ThenByDescending(x => x.Version).ToArrayAsync();
        return Ok(new ApiResponse<NotificationPolicyDto[]>(true, values.Select(ToDto).ToArray()));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<NotificationPolicyDto>>> CreateDraft(SaveNotificationPolicyRequest request)
    {
        if (!HasTenant()) return TenantRequired<NotificationPolicyDto>();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(Fail<NotificationPolicyDto>("User not found."));
        var validation = Validate(request);
        if (validation != null) return BadRequest(Fail<NotificationPolicyDto>(validation));
        var year = await context.MunicipalityFinancialYears.Include(x => x.FinancialYear).SingleOrDefaultAsync(x => x.PublicId == request.MunicipalityFinancialYearPublicId);
        if (year == null) return BadRequest(Fail<NotificationPolicyDto>("Municipality financial year not found."));
        ReportingPeriod? period = null;
        if (request.ReportingPeriodPublicId.HasValue)
        {
            period = await context.ReportingPeriods.SingleOrDefaultAsync(x => x.PublicId == request.ReportingPeriodPublicId.Value && x.MunicipalityFinancialYearId == year.Id);
            if (period == null) return BadRequest(Fail<NotificationPolicyDto>("Reporting period is not in the selected financial year."));
        }
        var previous = request.PreviousVersionPublicId.HasValue ? await context.NotificationConfigurations.SingleOrDefaultAsync(x => x.PublicId == request.PreviousVersionPublicId.Value) : null;
        if (request.PreviousVersionPublicId.HasValue && previous == null) return BadRequest(Fail<NotificationPolicyDto>("Previous policy version not found."));
        var familyId = previous?.FamilyId ?? Guid.NewGuid();
        var version = (await context.NotificationConfigurations.Where(x => x.FamilyId == familyId).MaxAsync(x => (int?)x.Version) ?? 0) + 1;
        var entity = new NotificationConfiguration
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalityFinancialYearId = year.Id, PreviousVersionId = previous?.Id,
            FamilyId = familyId, Version = version, Code = request.Code.Trim().ToUpperInvariant(), Name = request.Name.Trim(), Scope = request.Scope,
            Source = request.Source, SubmissionKind = request.SubmissionKind, WorkflowStageCode = Normalize(request.WorkflowStageCode), ReportingPeriodId = period?.Id,
            Lifecycle = NotificationPolicyLifecycle.Draft, IsMandatory = request.IsMandatory, DeliveryPaused = request.DeliveryPaused,
            ChannelsCsv = NormalizeChannels(request.Channels), TitleTemplate = request.TitleTemplate.Trim(), MessageTemplate = request.MessageTemplate.Trim(),
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, CreatedByUserId = actorId
        };
        entity.ApplicabilityKey = ApplicabilityKey(entity);
        foreach (var rule in request.Rules) entity.Rules.Add(new NotificationScheduleRule
        {
            MunicipalityId = entity.MunicipalityId, Code = rule.Code.Trim().ToUpperInvariant(), WorkingDayOffset = rule.WorkingDayOffset,
            RecipientKind = rule.RecipientKind, RecipientValuesCsv = string.Join(',', rule.RecipientValues.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)), IsActive = true
        });
        context.NotificationConfigurations.Add(entity);
        governance.QueueAuditTrail(nameof(NotificationConfiguration), entity.PublicId.ToString(), "CreateDraft", null, new { entity.Code, entity.Version, entity.Scope, entity.Source, entity.EffectiveFrom, entity.EffectiveTo }, actorId, HttpContext.Connection.RemoteIpAddress?.ToString(), request.Reason);
        await context.SaveChangesAsync();
        entity.MunicipalityFinancialYear = year; entity.ReportingPeriod = period;
        return Ok(new ApiResponse<NotificationPolicyDto>(true, ToDto(entity), "Draft notification policy created. It has no runtime effect until activated."));
    }

    [HttpPost("{publicId:guid}/activate")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<NotificationPolicyDto>>> Activate(Guid publicId, ActivateNotificationPolicyRequest request)
    {
        if (!HasTenant()) return TenantRequired<NotificationPolicyDto>();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(Fail<NotificationPolicyDto>("User not found."));
        if (request.Reason.Trim().Length is < 10 or > 1000) return BadRequest(Fail<NotificationPolicyDto>("An activation reason of 10-1000 characters is required."));
        var entity = await context.NotificationConfigurations.Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).Include(x => x.ReportingPeriod).Include(x => x.Rules).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<NotificationPolicyDto>("Notification policy not found."));
        if (entity.Lifecycle != NotificationPolicyLifecycle.Draft) return Conflict(Fail<NotificationPolicyDto>("Only a draft policy can be activated."));
        try { context.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return BadRequest(Fail<NotificationPolicyDto>("RowVersion is invalid.")); }
        var active = await context.NotificationConfigurations.Where(x => x.Id != entity.Id && x.MunicipalityFinancialYearId == entity.MunicipalityFinancialYearId && x.ApplicabilityKey == entity.ApplicabilityKey && x.Lifecycle == NotificationPolicyLifecycle.Active).ToArrayAsync();
        foreach (var prior in active)
        {
            prior.Lifecycle = NotificationPolicyLifecycle.Superseded;
            prior.EffectiveTo = entity.EffectiveFrom;
            governance.QueueAuditTrail(nameof(NotificationConfiguration), prior.PublicId.ToString(), "Supersede", new { Lifecycle = NotificationPolicyLifecycle.Active }, new { Lifecycle = prior.Lifecycle, prior.EffectiveTo, Successor = entity.PublicId }, actorId, null, request.Reason);
        }
        entity.Lifecycle = NotificationPolicyLifecycle.Active;
        entity.ActivatedAt = DateTime.UtcNow;
        entity.ActivatedByUserId = actorId;
        governance.QueueAuditTrail(nameof(NotificationConfiguration), entity.PublicId.ToString(), "Activate", new { Lifecycle = NotificationPolicyLifecycle.Draft }, new { entity.Lifecycle, entity.ActivatedAt }, actorId, null, request.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<NotificationPolicyDto>("The draft changed; reload before activation.")); }
        return Ok(new ApiResponse<NotificationPolicyDto>(true, ToDto(entity)));
    }

    [HttpPost("{publicId:guid}/copy-to-financial-year")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<NotificationPolicyDto>>> CopyToFinancialYear(Guid publicId, CopyNotificationPolicyRequest request)
    {
        if (!HasTenant()) return TenantRequired<NotificationPolicyDto>();
        var source = await context.NotificationConfigurations.AsNoTracking().Include(x => x.ReportingPeriod).Include(x => x.Rules).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (source == null) return NotFound(Fail<NotificationPolicyDto>("Source notification policy not found."));
        var targetYear = await context.MunicipalityFinancialYears.AsNoTracking().Include(x => x.FinancialYear).Include(x => x.ReportingPeriods).SingleOrDefaultAsync(x => x.PublicId == request.MunicipalityFinancialYearPublicId);
        if (targetYear == null) return BadRequest(Fail<NotificationPolicyDto>("Target municipality financial year not found."));
        Guid? periodPublicId = null;
        if (source.Scope == NotificationPolicyScope.ReportingPeriodOverride)
        {
            var targetPeriod = targetYear.ReportingPeriods.SingleOrDefault(x => x.PeriodType == source.ReportingPeriod!.PeriodType);
            if (targetPeriod == null) return BadRequest(Fail<NotificationPolicyDto>("The target financial year has no equivalent reporting period."));
            periodPublicId = targetPeriod.PublicId;
        }
        var end = source.EffectiveTo.HasValue ? targetYear.FinancialYear.EndDate.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;
        return await CreateDraft(new SaveNotificationPolicyRequest(targetYear.PublicId, source.PublicId, source.Code, source.Name, source.Scope, source.Source, source.SubmissionKind, source.WorkflowStageCode, periodPublicId, source.IsMandatory, source.DeliveryPaused, source.ChannelsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries), source.TitleTemplate, source.MessageTemplate, targetYear.FinancialYear.StartDate, end, source.Rules.Select(x => new SaveNotificationRuleRequest(x.Code, x.WorkingDayOffset, x.RecipientKind, x.RecipientValuesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))).ToArray(), request.Reason));
    }

    [HttpPost("{publicId:guid}/delivery-state")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<NotificationPolicyDto>>> SetDeliveryState(Guid publicId, SetNotificationDeliveryStateRequest request)
    {
        if (!HasTenant()) return TenantRequired<NotificationPolicyDto>();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(Fail<NotificationPolicyDto>("User not found."));
        if (request.Reason.Trim().Length is < 10 or > 1000) return BadRequest(Fail<NotificationPolicyDto>("A reason of 10-1000 characters is required."));
        var entity = await context.NotificationConfigurations.Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).Include(x => x.ReportingPeriod).Include(x => x.Rules).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<NotificationPolicyDto>("Notification policy not found."));
        if (entity.Lifecycle != NotificationPolicyLifecycle.Active) return Conflict(Fail<NotificationPolicyDto>("Only an active policy can be paused or resumed."));
        try { context.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return BadRequest(Fail<NotificationPolicyDto>("RowVersion is invalid.")); }
        var before = entity.DeliveryPaused;
        entity.DeliveryPaused = request.Paused;
        governance.QueueAuditTrail(nameof(NotificationConfiguration), entity.PublicId.ToString(), request.Paused ? "PauseDelivery" : "ResumeDelivery", new { DeliveryPaused = before }, new { entity.DeliveryPaused }, actorId, null, request.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<NotificationPolicyDto>("The policy changed; reload before updating delivery state.")); }
        return Ok(new ApiResponse<NotificationPolicyDto>(true, ToDto(entity), request.Paused ? "Delivery paused; workflow deadlines continue." : "Delivery resumed; due reminders will be caught up."));
    }

    [HttpPost("{publicId:guid}/preview")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<NotificationTemplatePreview>>> Preview(Guid publicId, PreviewNotificationPolicyRequest request)
    {
        if (!HasTenant()) return TenantRequired<NotificationTemplatePreview>();
        var entity = await context.NotificationConfigurations.AsNoTracking().Include(x => x.Municipality).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<NotificationTemplatePreview>("Notification policy not found."));
        return Ok(new ApiResponse<NotificationTemplatePreview>(true, policies.Preview(entity, new(request.Item.Trim(), request.Period.Trim(), entity.Municipality.Name, request.DeadlineAt, request.WorkingDayOffset))));
    }

    [HttpPost("{publicId:guid}/test")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<bool>>> QueueTest(Guid publicId, PreviewNotificationPolicyRequest request)
    {
        if (!HasTenant()) return TenantRequired<bool>();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(Fail<bool>("User not found."));
        var entity = await context.NotificationConfigurations.AsNoTracking().Include(x => x.Municipality).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<bool>("Notification policy not found."));
        var rendered = policies.Preview(entity, new(request.Item.Trim(), request.Period.Trim(), entity.Municipality.Name, request.DeadlineAt, request.WorkingDayOffset));
        context.Notifications.Add(new Notification { MunicipalityId = entity.MunicipalityId, UserId = userId, Type = NotificationType.DeadlineReminder, Title = $"[TEST] {rendered.Title}", Message = rendered.Message, EntityName = nameof(NotificationConfiguration), EntityId = entity.PublicId.ToString() });
        context.BusinessEventOutbox.Add(new BusinessEventOutbox { MunicipalityId = entity.MunicipalityId, EventType = "Notification.PolicyTest", AggregateType = nameof(NotificationConfiguration), AggregateId = entity.PublicId.ToString(), Payload = System.Text.Json.JsonSerializer.Serialize(new { title = $"[TEST] {rendered.Title}", message = rendered.Message, entityName = nameof(NotificationConfiguration), entityId = entity.PublicId.ToString(), recipients = new[] { userId }, channels = rendered.Channels, mandatory = true }) });
        governance.QueueAuditTrail(nameof(NotificationConfiguration), entity.PublicId.ToString(), "QueueTestNotification", null, new { RecipientUserId = userId, rendered.Channels }, userId, null, "Administrator notification policy delivery test.");
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true, "Test notification queued through the normal delivery pipeline."));
    }

    [HttpPost("run-due")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<int>>> RunDue()
    {
        if (!HasTenant()) return TenantRequired<int>();
        var count = await policies.ProcessDueAsync(DateTime.UtcNow, tenantContext.MunicipalityId, HttpContext.RequestAborted);
        return Ok(new ApiResponse<int>(true, count, $"Queued {count} due notification(s)."));
    }

    [HttpGet("holidays")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<WorkingCalendarHolidayDto[]>>> GetHolidays()
    {
        if (!HasTenant()) return TenantRequired<WorkingCalendarHolidayDto[]>();
        var rows = await context.WorkingCalendarHolidays.AsNoTracking().Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).OrderBy(x => x.Date).ToArrayAsync();
        return Ok(new ApiResponse<WorkingCalendarHolidayDto[]>(true, rows.Select(ToDto).ToArray()));
    }

    [HttpPost("holidays")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<WorkingCalendarHolidayDto>>> AddHoliday(SaveWorkingCalendarHolidayRequest request)
    {
        if (!HasTenant()) return TenantRequired<WorkingCalendarHolidayDto>();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(Fail<WorkingCalendarHolidayDto>("User not found."));
        if (request.Name.Trim().Length is < 2 or > 200 || request.Reason.Trim().Length is < 10 or > 1000) return BadRequest(Fail<WorkingCalendarHolidayDto>("Holiday name and a governance reason of 10-1000 characters are required."));
        var year = await context.MunicipalityFinancialYears.Include(x => x.FinancialYear).SingleOrDefaultAsync(x => x.PublicId == request.MunicipalityFinancialYearPublicId);
        if (year == null) return BadRequest(Fail<WorkingCalendarHolidayDto>("Municipality financial year not found."));
        var date = request.Date.Date;
        if (await context.WorkingCalendarHolidays.AnyAsync(x => x.MunicipalityFinancialYearId == year.Id && x.Date == date)) return Conflict(Fail<WorkingCalendarHolidayDto>("That date already exists in this working calendar."));
        var entity = new WorkingCalendarHoliday { MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalityFinancialYearId = year.Id, Date = date, Name = request.Name.Trim(), CreatedByUserId = actorId, MunicipalityFinancialYear = year };
        context.WorkingCalendarHolidays.Add(entity);
        governance.QueueAuditTrail(nameof(WorkingCalendarHoliday), entity.PublicId.ToString(), "Create", null, new { entity.Date, entity.Name, FinancialYear = year.FinancialYear.Code }, actorId, null, request.Reason);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<WorkingCalendarHolidayDto>(true, ToDto(entity)));
    }

    private string? Validate(SaveNotificationPolicyRequest request)
    {
        if (request.Code.Trim().Length is < 2 or > 80 || request.Name.Trim().Length is < 2 or > 200) return "Code and name are required.";
        if (request.Reason.Trim().Length is < 10 or > 1000) return "A governance reason of 10-1000 characters is required.";
        if (request.EffectiveTo.HasValue && request.EffectiveTo <= request.EffectiveFrom) return "EffectiveTo must be after EffectiveFrom.";
        if (request.Scope == NotificationPolicyScope.WorkflowStageDefault && string.IsNullOrWhiteSpace(request.WorkflowStageCode)) return "A stage-default policy requires WorkflowStageCode.";
        if (request.Scope == NotificationPolicyScope.ReportingPeriodOverride && !request.ReportingPeriodPublicId.HasValue) return "A period override requires ReportingPeriodPublicId.";
        if (request.Scope == NotificationPolicyScope.MunicipalityDefault && (request.ReportingPeriodPublicId.HasValue || !string.IsNullOrWhiteSpace(request.WorkflowStageCode))) return "A municipality default cannot specify a stage or period.";
        if (request.Rules.Count == 0 || request.Rules.Any(x => string.IsNullOrWhiteSpace(x.Code)) || request.Rules.Select(x => x.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Rules.Count) return "At least one uniquely coded schedule rule is required.";
        if (request.Rules.Any(x => x.RecipientKind is NotificationRecipientKind.Role or NotificationRecipientKind.User && x.RecipientValues.Count == 0)) return "Role and user recipient rules require recipient values.";
        var templateError = policies.ValidateTemplate(request.TitleTemplate, request.MessageTemplate);
        if (templateError != null) return templateError;
        var channels = request.Channels.Select(x => x.Trim().ToUpperInvariant()).ToArray();
        if (channels.Length == 0 || channels.Any(x => x is not ("IN_APP" or "EMAIL" or "SMS"))) return "Channels must contain IN_APP, EMAIL, or SMS.";
        return null;
    }

    private static string ApplicabilityKey(NotificationConfiguration value) => string.Join('|', value.Code, (int)value.Source, value.SubmissionKind?.ToString() ?? "ALL", (int)value.Scope, value.WorkflowStageCode ?? "-", value.ReportingPeriodId?.ToString() ?? "-");
    private static string NormalizeChannels(IEnumerable<string> values) => string.Join(',', values.Select(x => x.Trim().ToUpperInvariant()).Distinct());
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => Conflict(Fail<T>("Select a municipality context."));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private static NotificationPolicyDto ToDto(NotificationConfiguration x) => new(x.PublicId, x.FamilyId, x.Version, x.Code, x.Name, x.MunicipalityFinancialYear.PublicId, x.MunicipalityFinancialYear.FinancialYear.Code, x.Scope, x.Source, x.SubmissionKind, x.WorkflowStageCode, x.ReportingPeriod?.PublicId, x.ReportingPeriod?.Name, x.Lifecycle, x.IsMandatory, x.DeliveryPaused, x.ChannelsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries), x.TitleTemplate, x.MessageTemplate, x.EffectiveFrom, x.EffectiveTo, x.Rules.OrderBy(r => r.WorkingDayOffset).Select(r => new NotificationScheduleRuleDto(r.PublicId, r.Code, r.WorkingDayOffset, r.RecipientKind, r.RecipientValuesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries), r.IsActive)).ToArray(), Convert.ToBase64String(x.RowVersion));
    private static WorkingCalendarHolidayDto ToDto(WorkingCalendarHoliday x) => new(x.PublicId, x.MunicipalityFinancialYear.PublicId, x.MunicipalityFinancialYear.FinancialYear.Code, x.Date, x.Name, Convert.ToBase64String(x.RowVersion));
    private static NotificationPreferenceDto ToDto(NotificationPreference x) => new(x.EmailEnabled, x.SmsEnabled, x.DailyDigestEnabled, x.WeeklySummaryEnabled, Convert.ToBase64String(x.RowVersion));
}

public sealed record SaveNotificationRuleRequest(string Code, int WorkingDayOffset, NotificationRecipientKind RecipientKind, IReadOnlyList<string> RecipientValues);
public sealed record SaveNotificationPolicyRequest(Guid MunicipalityFinancialYearPublicId, Guid? PreviousVersionPublicId, string Code, string Name, NotificationPolicyScope Scope, NotificationScheduleSource Source, SubmissionKind? SubmissionKind, string? WorkflowStageCode, Guid? ReportingPeriodPublicId, bool IsMandatory, bool DeliveryPaused, IReadOnlyList<string> Channels, string TitleTemplate, string MessageTemplate, DateTime EffectiveFrom, DateTime? EffectiveTo, IReadOnlyList<SaveNotificationRuleRequest> Rules, string Reason);
public sealed record ActivateNotificationPolicyRequest(string RowVersion, string Reason);
public sealed record CopyNotificationPolicyRequest(Guid MunicipalityFinancialYearPublicId, string Reason);
public sealed record SetNotificationDeliveryStateRequest(bool Paused, string RowVersion, string Reason);
public sealed record PreviewNotificationPolicyRequest(string Item, string Period, DateTime DeadlineAt, int WorkingDayOffset);
public sealed record SaveWorkingCalendarHolidayRequest(Guid MunicipalityFinancialYearPublicId, DateTime Date, string Name, string Reason);
public sealed record NotificationScheduleRuleDto(Guid PublicId, string Code, int WorkingDayOffset, NotificationRecipientKind RecipientKind, string[] RecipientValues, bool IsActive);
public sealed record NotificationPolicyDto(Guid PublicId, Guid FamilyId, int Version, string Code, string Name, Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, NotificationPolicyScope Scope, NotificationScheduleSource Source, SubmissionKind? SubmissionKind, string? WorkflowStageCode, Guid? ReportingPeriodPublicId, string? ReportingPeriodName, NotificationPolicyLifecycle Lifecycle, bool IsMandatory, bool DeliveryPaused, string[] Channels, string TitleTemplate, string MessageTemplate, DateTime EffectiveFrom, DateTime? EffectiveTo, NotificationScheduleRuleDto[] Rules, string RowVersion);
public sealed record WorkingCalendarHolidayDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, DateTime Date, string Name, string RowVersion);
public sealed record SaveNotificationPreferenceRequest(bool EmailEnabled, bool SmsEnabled, bool DailyDigestEnabled, bool WeeklySummaryEnabled, string? RowVersion);
public sealed record NotificationPreferenceDto(bool EmailEnabled, bool SmsEnabled, bool DailyDigestEnabled, bool WeeklySummaryEnabled, string? RowVersion);

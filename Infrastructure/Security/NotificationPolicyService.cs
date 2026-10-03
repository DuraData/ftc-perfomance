using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Security;

public interface IWorkingCalendarService
{
    DateTime AddWorkingDays(DateTime value, int workingDays, IReadOnlySet<DateOnly> holidays);
}

public sealed class WorkingCalendarService : IWorkingCalendarService
{
    public DateTime AddWorkingDays(DateTime value, int workingDays, IReadOnlySet<DateOnly> holidays)
    {
        if (workingDays == 0) return value;
        var direction = Math.Sign(workingDays);
        var remaining = Math.Abs(workingDays);
        var result = value;
        while (remaining > 0)
        {
            result = result.AddDays(direction);
            var date = DateOnly.FromDateTime(result);
            if (result.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(date)) continue;
            remaining--;
        }
        return result;
    }
}

public sealed record NotificationTemplateContext(string Item, string Period, string Municipality, DateTime DeadlineAt, int WorkingDayOffset);
public sealed record NotificationTemplatePreview(string Title, string Message, string[] Channels);

public interface INotificationPolicyService
{
    string? ValidateTemplate(string title, string message);
    NotificationTemplatePreview Preview(NotificationConfiguration policy, NotificationTemplateContext context);
    Task<int> ProcessDueAsync(DateTime utcNow, long? municipalityId = null, CancellationToken cancellationToken = default);
}

public sealed class NotificationPolicyService(ApplicationDbContext context, IWorkingCalendarService calendar) : INotificationPolicyService
{
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Item", "Period", "Municipality", "DueDate", "Days"
    };

    public string? ValidateTemplate(string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 240) return "A title template of 1-240 characters is required.";
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 2000) return "A message template of 1-2000 characters is required.";
        foreach (var template in new[] { title, message })
        {
            var index = 0;
            while ((index = template.IndexOf('{', index)) >= 0)
            {
                var end = template.IndexOf('}', index + 1);
                if (end < 0) return "Template contains an unclosed placeholder.";
                var token = template[(index + 1)..end];
                if (!Placeholders.Contains(token)) return $"Template placeholder '{{{token}}}' is not supported.";
                index = end + 1;
            }
        }
        return null;
    }

    public NotificationTemplatePreview Preview(NotificationConfiguration policy, NotificationTemplateContext value)
        => new(Render(policy.TitleTemplate, value), Render(policy.MessageTemplate, value), Channels(policy));

    public async Task<int> ProcessDueAsync(DateTime utcNow, long? municipalityId = null, CancellationToken cancellationToken = default)
    {
        var policies = await context.NotificationConfigurations.IgnoreQueryFilters().AsNoTracking()
            .Include(x => x.Municipality).Include(x => x.Rules)
            .Where(x => (!municipalityId.HasValue || x.MunicipalityId == municipalityId.Value) && x.Lifecycle == NotificationPolicyLifecycle.Active && x.EffectiveFrom <= utcNow && (!x.EffectiveTo.HasValue || x.EffectiveTo > utcNow))
            .ToArrayAsync(cancellationToken);
        var queued = 0;
        foreach (var municipalityGroup in policies.GroupBy(x => x.MunicipalityId))
        {
            var municipalityPolicies = municipalityGroup.ToArray();
            var yearIds = municipalityPolicies.Select(x => x.MunicipalityFinancialYearId).Distinct().ToArray();
            var holidayRows = await context.WorkingCalendarHolidays.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.MunicipalityId == municipalityGroup.Key && yearIds.Contains(x.MunicipalityFinancialYearId))
                .Select(x => new { x.MunicipalityFinancialYearId, x.Date }).ToArrayAsync(cancellationToken);
            var holidays = holidayRows.GroupBy(x => x.MunicipalityFinancialYearId)
                .ToDictionary(x => x.Key, x => (IReadOnlySet<DateOnly>)x.Select(y => DateOnly.FromDateTime(y.Date)).ToHashSet());

            var windowPolicies = municipalityPolicies.Where(x => x.Source == NotificationScheduleSource.ReportingWindow).ToArray();
            if (windowPolicies.Length > 0)
            {
                var windows = await context.ReportingWindows.IgnoreQueryFilters().AsNoTracking().Include(x => x.ReportingPeriod).ThenInclude(x => x.MunicipalityFinancialYear)
                    .Where(x => x.MunicipalityId == municipalityGroup.Key && x.IsActive && yearIds.Contains(x.ReportingPeriod.MunicipalityFinancialYearId)).ToArrayAsync(cancellationToken);
                foreach (var window in windows)
                {
                    foreach (var policy in ResolveMany(windowPolicies, window.ReportingPeriod.MunicipalityFinancialYearId, window.SubmissionKind, null, window.ReportingPeriodId))
                        queued += await MaterializeAsync(policy, nameof(ReportingWindow), window.PublicId.ToString(), window.ClosesAt, window.ReportingPeriod.Name, window.ReportingPeriodId, window.SubmissionKind, null, utcNow, holidays.GetValueOrDefault(policy.MunicipalityFinancialYearId, new HashSet<DateOnly>()), cancellationToken);
                }
            }

            var rfiPolicies = municipalityPolicies.Where(x => x.Source == NotificationScheduleSource.Rfi).ToArray();
            if (rfiPolicies.Length > 0)
            {
                var rfis = await context.PerformanceRfis.IgnoreQueryFilters().AsNoTracking()
                    .Include(x => x.SubmissionWorkflowInstance).ThenInclude(x => x.WorkflowDefinition)
                    .Include(x => x.SubmissionWorkflowInstance).ThenInclude(x => x.CurrentStage)
                    .Where(x => x.MunicipalityId == municipalityGroup.Key && x.ClosedAt == null && x.RespondedAt == null && yearIds.Contains(x.SubmissionWorkflowInstance.WorkflowDefinition.MunicipalityFinancialYearId))
                    .ToArrayAsync(cancellationToken);
                foreach (var rfi in rfis)
                {
                    var instance = rfi.SubmissionWorkflowInstance;
                    var periodId = await ResolveSubmissionPeriodId(instance, cancellationToken);
                    foreach (var policy in ResolveMany(rfiPolicies, instance.WorkflowDefinition.MunicipalityFinancialYearId, instance.SubmissionKind, instance.CurrentStage?.Code, periodId))
                        queued += await MaterializeAsync(policy, nameof(PerformanceRfi), rfi.PublicId.ToString(), rfi.ResponseDueAt, "RFI", periodId, instance.SubmissionKind, instance, utcNow, holidays.GetValueOrDefault(policy.MunicipalityFinancialYearId, new HashSet<DateOnly>()), cancellationToken);
                }
            }
        }
        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync(cancellationToken);
        return queued;
    }

    private static IEnumerable<NotificationConfiguration> ResolveMany(IEnumerable<NotificationConfiguration> policies, long yearId, SubmissionKind kind, string? stageCode, long? periodId)
        => policies.Where(x => x.MunicipalityFinancialYearId == yearId && (!x.SubmissionKind.HasValue || x.SubmissionKind == kind))
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Where(x => x.Scope switch
                {
                    NotificationPolicyScope.ReportingPeriodOverride => periodId.HasValue && x.ReportingPeriodId == periodId,
                    NotificationPolicyScope.WorkflowStageDefault => !string.IsNullOrWhiteSpace(stageCode) && string.Equals(x.WorkflowStageCode, stageCode, StringComparison.OrdinalIgnoreCase),
                    _ => true
                })
                .OrderByDescending(x => x.Scope).ThenByDescending(x => x.Version).FirstOrDefault())
            .Where(x => x != null)!;

    private async Task<int> MaterializeAsync(NotificationConfiguration policy, string sourceType, string sourceId, DateTime deadline, string period, long? reportingPeriodId, SubmissionKind sourceKind, SubmissionWorkflowInstance? instance, DateTime now, IReadOnlySet<DateOnly> holidays, CancellationToken cancellationToken)
    {
        var result = 0;
        foreach (var rule in policy.Rules.Where(x => x.IsActive))
        {
            var scheduledAt = calendar.AddWorkingDays(deadline, rule.WorkingDayOffset, holidays);
            var recipients = await ResolveRecipients(policy, rule, reportingPeriodId, sourceKind, instance, now, cancellationToken);
            foreach (var recipient in recipients)
            {
                var familyRuleKey = $"{policy.MunicipalityId}:{policy.FamilyId:N}:{rule.Code}:{sourceType}:{sourceId}:{recipient}";
                var contextHash = Hash($"{policy.PublicId:N}|{rule.PublicId:N}|{deadline:O}|{scheduledAt:O}|{policy.TitleTemplate}|{policy.MessageTemplate}|{policy.ChannelsCsv}");
                var logicalKey = Hash($"{familyRuleKey}|{contextHash}");
                var scheduled = await context.ScheduledNotifications.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.LogicalKey == logicalKey, cancellationToken);
                if (scheduled != null && scheduled.State != ScheduledNotificationState.Pending) continue;
                if (scheduled == null)
                {
                    var obsolete = await context.ScheduledNotifications.IgnoreQueryFilters()
                        .Where(x => x.MunicipalityId == policy.MunicipalityId && x.SourceType == sourceType && x.SourceId == sourceId && x.RecipientUserId == recipient
                            && x.NotificationConfiguration.FamilyId == policy.FamilyId && x.NotificationScheduleRule.Code == rule.Code && x.State == ScheduledNotificationState.Pending)
                        .ToArrayAsync(cancellationToken);
                    foreach (var item in obsolete) { item.State = ScheduledNotificationState.Superseded; item.StateReason = "Effective policy, template, deadline, or schedule changed before delivery."; }
                    scheduled = new ScheduledNotification
                    {
                        MunicipalityId = policy.MunicipalityId, NotificationConfigurationId = policy.Id, NotificationScheduleRuleId = rule.Id,
                        SourceType = sourceType, SourceId = sourceId, RecipientUserId = recipient, DeadlineAt = deadline, ScheduledAt = scheduledAt,
                        LogicalKey = logicalKey, ContextHash = contextHash, State = ScheduledNotificationState.Pending
                    };
                    context.ScheduledNotifications.Add(scheduled);
                }
                if (scheduledAt > now || policy.DeliveryPaused) continue;

                var templateContext = new NotificationTemplateContext(sourceType == nameof(PerformanceRfi) ? "RFI response" : "performance submission", period, policy.Municipality.Name, deadline, rule.WorkingDayOffset);
                var rendered = Preview(policy, templateContext);
                var notification = new Notification
                {
                    MunicipalityId = policy.MunicipalityId, UserId = recipient,
                    Type = rule.WorkingDayOffset > 0 ? NotificationType.Escalation : NotificationType.DeadlineReminder,
                    Title = rendered.Title, Message = rendered.Message, EntityName = sourceType, EntityId = sourceId, CreatedAt = now
                };
                var outbox = new BusinessEventOutbox
                {
                    MunicipalityId = policy.MunicipalityId, EventType = rule.WorkingDayOffset > 0 ? "Notification.Escalation" : "Notification.DeadlineReminder",
                    AggregateType = sourceType, AggregateId = sourceId, OccurredAt = now, AvailableAt = now,
                    Payload = JsonSerializer.Serialize(new { title = rendered.Title, message = rendered.Message, entityName = sourceType, entityId = sourceId, recipients = new[] { recipient }, channels = rendered.Channels, mandatory = policy.IsMandatory })
                };
                context.Notifications.Add(notification);
                context.BusinessEventOutbox.Add(outbox);
                scheduled.Notification = notification;
                scheduled.BusinessEventOutbox = outbox;
                scheduled.State = ScheduledNotificationState.Queued;
                scheduled.QueuedAt = now;
                result++;
            }
        }
        return result;
    }

    private async Task<string[]> ResolveRecipients(NotificationConfiguration policy, NotificationScheduleRule rule, long? reportingPeriodId, SubmissionKind sourceKind, SubmissionWorkflowInstance? instance, DateTime now, CancellationToken cancellationToken)
    {
        var values = Csv(rule.RecipientValuesCsv);
        IQueryable<string> query;
        if (rule.RecipientKind == NotificationRecipientKind.User)
        {
            query = context.Users.IgnoreQueryFilters().Where(x => x.MunicipalityId == policy.MunicipalityId && x.IsActive && values.Contains(x.Id)).Select(x => x.Id);
        }
        else if (rule.RecipientKind == NotificationRecipientKind.Role)
        {
            query = context.SecurityUserRoleAssignments.IgnoreQueryFilters()
                .Where(x => x.MunicipalityId == policy.MunicipalityId && x.IsActive && x.EffectiveFrom <= now && (!x.EffectiveTo.HasValue || x.EffectiveTo > now) && values.Contains(x.Role.RoleCode) && x.User.IsActive)
                .Select(x => x.UserId);
        }
        else if (instance != null)
        {
            if (instance.SubmissionKind == SubmissionKind.Opms)
                query = context.OpmsSubmissions.IgnoreQueryFilters().Where(x => x.Id == instance.SubmissionId && x.MunicipalityId == policy.MunicipalityId && x.OpmsTarget.AssignedUserId != null).Select(x => x.OpmsTarget.AssignedUserId!);
            else
                query = context.IpmsSubmissions.IgnoreQueryFilters().Where(x => x.Id == instance.SubmissionId && x.MunicipalityId == policy.MunicipalityId && x.IpmsTarget.AssignedUserId != null).Select(x => x.IpmsTarget.AssignedUserId!);
        }
        else if (reportingPeriodId.HasValue && sourceKind == SubmissionKind.Ipms)
        {
            query = context.PerformancePeriodTargets.IgnoreQueryFilters().Where(x => x.MunicipalityId == policy.MunicipalityId && x.ReportingPeriodId == reportingPeriodId && x.IpmsTargetId != null && x.IpmsTarget!.AssignedUserId != null).Select(x => x.IpmsTarget!.AssignedUserId!);
        }
        else if (reportingPeriodId.HasValue)
        {
            query = context.PerformancePeriodTargets.IgnoreQueryFilters().Where(x => x.MunicipalityId == policy.MunicipalityId && x.ReportingPeriodId == reportingPeriodId && x.OpmsTargetId != null && x.OpmsTarget!.AssignedUserId != null).Select(x => x.OpmsTarget!.AssignedUserId!);
        }
        else return [];
        return await query.Distinct().ToArrayAsync(cancellationToken);
    }

    private async Task<long?> ResolveSubmissionPeriodId(SubmissionWorkflowInstance instance, CancellationToken cancellationToken)
        => instance.SubmissionKind == SubmissionKind.Opms
            ? await context.OpmsSubmissions.IgnoreQueryFilters().Where(x => x.Id == instance.SubmissionId).Select(x => x.ReportingPeriodId).SingleOrDefaultAsync(cancellationToken)
            : await context.IpmsSubmissions.IgnoreQueryFilters().Where(x => x.Id == instance.SubmissionId).Select(x => x.ReportingPeriodId).SingleOrDefaultAsync(cancellationToken);

    private static string Render(string template, NotificationTemplateContext value) => template
        .Replace("{Item}", value.Item, StringComparison.OrdinalIgnoreCase)
        .Replace("{Period}", value.Period, StringComparison.OrdinalIgnoreCase)
        .Replace("{Municipality}", value.Municipality, StringComparison.OrdinalIgnoreCase)
        .Replace("{DueDate}", value.DeadlineAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
        .Replace("{Days}", Math.Abs(value.WorkingDayOffset).ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

    private static string[] Channels(NotificationConfiguration policy)
    {
        var channels = Csv(policy.ChannelsCsv).Select(x => x.ToUpperInvariant()).Where(x => x is "IN_APP" or "EMAIL" or "SMS").Distinct().ToList();
        if (policy.IsMandatory && !channels.Contains("IN_APP")) channels.Insert(0, "IN_APP");
        return channels.Count == 0 ? ["IN_APP"] : channels.ToArray();
    }

    private static string[] Csv(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class NotificationScheduleWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<NotificationScheduleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Clamp(configuration.GetValue("Notifications:SchedulerMinutes", 5), 1, 60);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<INotificationPolicyService>().ProcessDueAsync(DateTime.UtcNow, null, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Notification schedule processing failed"); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

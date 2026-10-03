namespace FTCERP.Host.Domain.Entities;

public enum NotificationPolicyScope
{
    MunicipalityDefault = 1,
    WorkflowStageDefault = 2,
    ReportingPeriodOverride = 3
}

public enum NotificationPolicyLifecycle
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Superseded = 4
}

public enum NotificationScheduleSource
{
    ReportingWindow = 1,
    Rfi = 2
}

public enum NotificationRecipientKind
{
    PrimaryAssignee = 1,
    Role = 2,
    User = 3
}

public enum ScheduledNotificationState
{
    Pending = 1,
    Queued = 2,
    Superseded = 3,
    Skipped = 4
}

/// <summary>A versioned notification policy. Activated versions are prospective and never rewrite sent notifications.</summary>
public class NotificationConfiguration
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long? PreviousVersionId { get; set; }
    public int Version { get; set; } = 1;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApplicabilityKey { get; set; } = string.Empty;
    public NotificationPolicyScope Scope { get; set; }
    public NotificationScheduleSource Source { get; set; }
    public SubmissionKind? SubmissionKind { get; set; }
    public string? WorkflowStageCode { get; set; }
    public long? ReportingPeriodId { get; set; }
    public NotificationPolicyLifecycle Lifecycle { get; set; } = NotificationPolicyLifecycle.Draft;
    public bool IsMandatory { get; set; } = true;
    public bool DeliveryPaused { get; set; }
    public string ChannelsCsv { get; set; } = "IN_APP";
    public string TitleTemplate { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ActivatedByUserId { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public NotificationConfiguration? PreviousVersion { get; set; }
    public ReportingPeriod? ReportingPeriod { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? ActivatedByUser { get; set; }
    public ICollection<NotificationScheduleRule> Rules { get; set; } = new List<NotificationScheduleRule>();
}

public class NotificationScheduleRule
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long NotificationConfigurationId { get; set; }
    public string Code { get; set; } = string.Empty;
    /// <summary>Working days relative to the deadline. Negative is before, zero is due day, positive is overdue.</summary>
    public int WorkingDayOffset { get; set; }
    public NotificationRecipientKind RecipientKind { get; set; }
    public string RecipientValuesCsv { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Municipality Municipality { get; set; } = null!;
    public NotificationConfiguration NotificationConfiguration { get; set; } = null!;
}

public class WorkingCalendarHoliday
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

/// <summary>Durable, idempotent materialization of one logical reminder for one recipient.</summary>
public class ScheduledNotification
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long NotificationConfigurationId { get; set; }
    public long NotificationScheduleRuleId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;
    public DateTime DeadlineAt { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string LogicalKey { get; set; } = string.Empty;
    public string ContextHash { get; set; } = string.Empty;
    public ScheduledNotificationState State { get; set; } = ScheduledNotificationState.Pending;
    public DateTime MaterializedAt { get; set; } = DateTime.UtcNow;
    public DateTime? QueuedAt { get; set; }
    public string? NotificationId { get; set; }
    public long? BusinessEventOutboxId { get; set; }
    public string? StateReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public NotificationConfiguration NotificationConfiguration { get; set; } = null!;
    public NotificationScheduleRule NotificationScheduleRule { get; set; } = null!;
    public ApplicationUser RecipientUser { get; set; } = null!;
    public Notification? Notification { get; set; }
    public BusinessEventOutbox? BusinessEventOutbox { get; set; }
}

public class NotificationPreference
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool DailyDigestEnabled { get; set; } = true;
    public bool WeeklySummaryEnabled { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

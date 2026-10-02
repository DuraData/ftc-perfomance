namespace FTCERP.Host.Domain.Entities;

public enum WorkflowInstanceState { Active = 1, Rework = 2, Completed = 3, Cancelled = 4 }
public enum WorkflowActionOutcome { Submit = 1, Approve = 2, Reject = 3, RaiseRfi = 4, RespondRfi = 5, Bypass = 6, Complete = 7 }

public class WorkflowDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ICollection<WorkflowStageDefinition> Stages { get; set; } = new List<WorkflowStageDefinition>();
}

public class WorkflowStageDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long WorkflowDefinitionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string RequiredActionCode { get; set; } = string.Empty;
    public string RequiredPermissionCode { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public bool AllowBypass { get; set; }
    public bool RequireDifferentActorFromSubmitter { get; set; } = true;
    public bool RequireDifferentActorFromPreviousStage { get; set; }
    public bool IsTerminal { get; set; }
    public string? RejectionStageCode { get; set; }
    public bool RequiresRating { get; set; }
    public long? RatingSchemeId { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public RatingScheme? RatingScheme { get; set; }
}

public class SubmissionWorkflowInstance
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long WorkflowDefinitionId { get; set; }
    public long? CurrentStageId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public WorkflowInstanceState State { get; set; } = WorkflowInstanceState.Active;
    public int NextSequence { get; set; } = 1;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public WorkflowStageDefinition? CurrentStage { get; set; }
    public ICollection<SubmissionWorkflowAction> Actions { get; set; } = new List<SubmissionWorkflowAction>();
}

public class SubmissionWorkflowAction
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long SubmissionWorkflowInstanceId { get; set; }
    public int Sequence { get; set; }
    public long? FromStageId { get; set; }
    public long? ToStageId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public WorkflowActionOutcome Outcome { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public decimal? RatingValue { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Municipality Municipality { get; set; } = null!;
    public SubmissionWorkflowInstance SubmissionWorkflowInstance { get; set; } = null!;
    public WorkflowStageDefinition? FromStage { get; set; }
    public WorkflowStageDefinition? ToStage { get; set; }
    public ApplicationUser ActorUser { get; set; } = null!;
}

public class PerformanceRfi
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long SubmissionWorkflowInstanceId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string RaisedByUserId { get; set; } = string.Empty;
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;
    public DateTime ResponseDueAt { get; set; }
    public string? Response { get; set; }
    public string? RespondedByUserId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public SubmissionWorkflowInstance SubmissionWorkflowInstance { get; set; } = null!;
    public ICollection<PerformanceRfiEvidence> EvidenceLinks { get; set; } = new List<PerformanceRfiEvidence>();
}

public enum RfiEvidencePurpose { Question = 1, Response = 2, Closure = 3 }

/// <summary>
/// Append-only provenance linking an RFI action to an existing governed POE record.
/// The POE remains subject to its own signature, malware, retention and download controls.
/// </summary>
public class PerformanceRfiEvidence
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long PerformanceRfiId { get; set; }
    public string PoeFileId { get; set; } = string.Empty;
    public RfiEvidencePurpose Purpose { get; set; }
    public string LinkedByUserId { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public PerformanceRfi PerformanceRfi { get; set; } = null!;
    public PoeFile PoeFile { get; set; } = null!;
    public ApplicationUser LinkedByUser { get; set; } = null!;
}

public class ReportingWindow
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long ReportingPeriodId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
}

public class ReportingWindowException
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long ReportingWindowId { get; set; }
    public string? UserId { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public DateTime ExtendedClosesAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ApprovedByUserId { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public ReportingWindow ReportingWindow { get; set; } = null!;
}

public class RatingScheme
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public ICollection<RatingSchemeValue> Values { get; set; } = new List<RatingSchemeValue>();
}

public class RatingSchemeValue
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long RatingSchemeId { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal? MinimumAchievementPercent { get; set; }
    public decimal? MaximumAchievementPercent { get; set; }
    public int SortOrder { get; set; }
    public Municipality Municipality { get; set; } = null!;
    public RatingScheme RatingScheme { get; set; } = null!;
}

public class SubmissionStageRating
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long SubmissionWorkflowInstanceId { get; set; }
    public long SubmissionWorkflowActionId { get; set; }
    public long WorkflowStageDefinitionId { get; set; }
    public long RatingSchemeId { get; set; }
    public long RatingSchemeValueId { get; set; }
    public decimal Value { get; set; }
    public string LabelSnapshot { get; set; } = string.Empty;
    public decimal? AchievementPercent { get; set; }
    public string? Comment { get; set; }
    public string RatedByUserId { get; set; } = string.Empty;
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }

    public Municipality Municipality { get; set; } = null!;
    public SubmissionWorkflowInstance SubmissionWorkflowInstance { get; set; } = null!;
    public SubmissionWorkflowAction SubmissionWorkflowAction { get; set; } = null!;
    public WorkflowStageDefinition WorkflowStageDefinition { get; set; } = null!;
    public RatingScheme RatingScheme { get; set; } = null!;
    public RatingSchemeValue RatingSchemeValue { get; set; } = null!;
    public ApplicationUser RatedByUser { get; set; } = null!;
}

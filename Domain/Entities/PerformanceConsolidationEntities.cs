using FTCERP.Host.Domain.Services;

namespace FTCERP.Host.Domain.Entities;

public sealed class PerformanceCalculationTypeDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class MunicipalityConsolidationPolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long CalculationTypeId { get; set; }
    public PerformanceCalculationType? ConsolidationRule { get; set; }
    public ConsolidationMissingValuePolicy MissingValuePolicy { get; set; } = ConsolidationMissingValuePolicy.Block;
    public bool AllowDerivedTargetOverride { get; set; }
    public bool DeriveAnnualTarget { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public PerformanceCalculationTypeDefinition CalculationType { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? ModifiedByUser { get; set; }
}

public enum PerformanceSuggestionEventType { Generated = 1, Accepted = 2, Edited = 3 }

public sealed class PerformanceSuggestionEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public string? OpmsSubmissionId { get; set; }
    public string? IpmsSubmissionId { get; set; }
    public long ReportingPeriodId { get; set; }
    public PerformanceSuggestionEventType EventType { get; set; }
    public string? SystemSuggestedActualPerformance { get; set; }
    public string? ActualPerformance { get; set; }
    public bool WasSystemSuggestionEdited { get; set; }
    public long? SuggestionCalculationTypeId { get; set; }
    public PerformanceCalculationType? EffectiveCalculationType { get; set; }
    public string SourcePeriods { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public OpmsSubmission? OpmsSubmission { get; set; }
    public IpmsSubmission? IpmsSubmission { get; set; }
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
    public PerformanceCalculationTypeDefinition? SuggestionCalculationType { get; set; }
    public ApplicationUser ActorUser { get; set; } = null!;
}

public partial class OpmsTarget
{
    public long? CalculationTypeId { get; set; }
    public PerformanceCalculationTypeDefinition? CalculationType { get; set; }
}

public partial class IpmsTarget
{
    public long? CalculationTypeId { get; set; }
    public PerformanceCalculationTypeDefinition? CalculationType { get; set; }
}

public partial class OpmsSubmission
{
    public string? SystemSuggestedActualPerformance { get; set; }
    public bool WasSystemSuggestionEdited { get; set; }
    public long? SuggestionCalculationTypeId { get; set; }
    public DateTime? SuggestionGeneratedDate { get; set; }
    public string? SuggestionEditedByUserId { get; set; }
    public DateTime? SuggestionEditedAt { get; set; }
    public string? SuggestionEditReason { get; set; }
    public PerformanceCalculationTypeDefinition? SuggestionCalculationType { get; set; }
    public ApplicationUser? SuggestionEditedByUser { get; set; }
    public ICollection<PerformanceSuggestionEvent> SuggestionEvents { get; set; } = new List<PerformanceSuggestionEvent>();
}

public partial class IpmsSubmission
{
    public string? SystemSuggestedActualPerformance { get; set; }
    public bool WasSystemSuggestionEdited { get; set; }
    public long? SuggestionCalculationTypeId { get; set; }
    public DateTime? SuggestionGeneratedDate { get; set; }
    public string? SuggestionEditedByUserId { get; set; }
    public DateTime? SuggestionEditedAt { get; set; }
    public string? SuggestionEditReason { get; set; }
    public PerformanceCalculationTypeDefinition? SuggestionCalculationType { get; set; }
    public ApplicationUser? SuggestionEditedByUser { get; set; }
    public ICollection<PerformanceSuggestionEvent> SuggestionEvents { get; set; } = new List<PerformanceSuggestionEvent>();
}

using FTCERP.Host.Domain.Services;

namespace FTCERP.Host.Domain.Entities;

public class PerformancePeriodTarget
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long ReportingPeriodId { get; set; }
    public string? OpmsTargetId { get; set; }
    public string? IpmsTargetId { get; set; }
    public PerformanceUnitKind UnitKind { get; set; }
    public PerformanceDirection Direction { get; set; } = PerformanceDirection.HigherIsBetter;
    public string TargetValue { get; set; } = string.Empty;
    public decimal? BudgetValue { get; set; }
    public string? Description { get; set; }
    public bool IsSystemDerivedTarget { get; set; }
    public string? DerivedFromPeriods { get; set; }
    public long? DerivedCalculationTypeId { get; set; }
    public string? SystemSuggestedTargetValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
    public OpmsTarget? OpmsTarget { get; set; }
    public IpmsTarget? IpmsTarget { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public PerformanceCalculationTypeDefinition? DerivedCalculationType { get; set; }
    public ICollection<PerformanceTargetRevision> Revisions { get; set; } = new List<PerformanceTargetRevision>();
}

public class PerformanceTargetRevision
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long PerformancePeriodTargetId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? RevisedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ApprovalReference { get; set; } = string.Empty;
    public DateTime EffectiveAt { get; set; }
    public string RevisedByUserId { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public Municipality Municipality { get; set; } = null!;
    public PerformancePeriodTarget PerformancePeriodTarget { get; set; } = null!;
    public ApplicationUser RevisedByUser { get; set; } = null!;
}

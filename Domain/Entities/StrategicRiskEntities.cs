namespace FTCERP.Host.Domain.Entities;

/// <summary>
/// Municipality-owned strategic-risk master used by OPMS KPI relationships.
/// Risks are effective-dated by governed municipality financial years and are
/// never hard deleted so historic KPI reporting remains resolvable.
/// </summary>
public sealed class StrategicRisk
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string? RiskReference { get; set; }
    public string RiskTitle { get; set; } = string.Empty;
    public string? RiskDescription { get; set; }
    public string? OwnerUserId { get; set; }
    public long? EffectiveFromMunicipalityFinancialYearId { get; set; }
    public long? EffectiveToMunicipalityFinancialYearId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear? EffectiveFromMunicipalityFinancialYear { get; set; }
    public MunicipalityFinancialYear? EffectiveToMunicipalityFinancialYear { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? UpdatedByUser { get; set; }
    public ApplicationUser? OwnerUser { get; set; }
    public ICollection<OpmsKpiStrategicRisk> KpiLinks { get; set; } = new List<OpmsKpiStrategicRisk>();
    public ICollection<StrategicRiskAssessment> Assessments { get; set; } = new List<StrategicRiskAssessment>();
    public ICollection<StrategicRiskTreatment> Treatments { get; set; } = new List<StrategicRiskTreatment>();
    public ICollection<StrategicRiskReview> Reviews { get; set; } = new List<StrategicRiskReview>();
}

/// <summary>
/// Append-preserving OPMS KPI-to-risk relationship. Unlinking closes the link
/// rather than deleting it; a filtered unique index prevents two active copies.
/// </summary>
public sealed class OpmsKpiStrategicRisk
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public long StrategicRiskId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public string LinkedByUserId { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public string LinkReason { get; set; } = string.Empty;
    public string? UnlinkedByUserId { get; set; }
    public DateTime? UnlinkedAt { get; set; }
    public string? UnlinkReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public OpmsTarget OpmsTarget { get; set; } = null!;
    public StrategicRisk StrategicRisk { get; set; } = null!;
    public ApplicationUser LinkedByUser { get; set; } = null!;
    public ApplicationUser? UnlinkedByUser { get; set; }
}

/// <summary>
/// Immutable point-in-time risk assessment. A new assessment supersedes the
/// previous effective view without rewriting the evidence used historically.
/// </summary>
public sealed class StrategicRiskAssessment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long StrategicRiskId { get; set; }
    public DateTime AssessedAt { get; set; }
    public int InherentLikelihood { get; set; }
    public int InherentImpact { get; set; }
    public int ResidualLikelihood { get; set; }
    public int ResidualImpact { get; set; }
    public string? ControlAssessment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AssessedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Municipality Municipality { get; set; } = null!;
    public StrategicRisk StrategicRisk { get; set; } = null!;
    public ApplicationUser AssessedByUser { get; set; } = null!;
}

/// <summary>
/// Concurrency-protected mitigation action. Its risk, municipality, creator and
/// creation evidence are immutable; lifecycle edits require contextual audit.
/// </summary>
public sealed class StrategicRiskTreatment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long StrategicRiskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = "OPEN";
    public int ProgressPercent { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public StrategicRisk StrategicRisk { get; set; } = null!;
    public ApplicationUser OwnerUser { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? UpdatedByUser { get; set; }
}

/// <summary>Immutable periodic risk-review evidence and the next planned review.</summary>
public sealed class StrategicRiskReview
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long StrategicRiskId { get; set; }
    public DateTime ReviewedAt { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public DateTime? NextReviewAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReviewedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Municipality Municipality { get; set; } = null!;
    public StrategicRisk StrategicRisk { get; set; } = null!;
    public ApplicationUser ReviewedByUser { get; set; } = null!;
}

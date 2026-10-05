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
    public ICollection<OpmsKpiStrategicRisk> KpiLinks { get; set; } = new List<OpmsKpiStrategicRisk>();
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

namespace FTCERP.Host.Domain.Entities;

public enum OfficialReportFormat
{
    Csv = 1,
    Xlsx = 2,
    Docx = 3,
    Pdf = 4
}

/// <summary>
/// An approved municipality report layout. Published versions are immutable; a change creates
/// another row in the same TemplateFamilyPublicId lineage.
/// </summary>
public sealed class OfficialReportTemplate
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid TemplateFamilyPublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long? MunicipalityFinancialYearId { get; set; }
    public long? PreviousVersionId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OfficialReportFormat Format { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string HeadingTemplate { get; set; } = "{FinancialYear} {Period} PERFORMANCE REPORT";
    public string ColumnConfigurationJson { get; set; } = "[]";
    public bool IsCurrent { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string ApprovalReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear? MunicipalityFinancialYear { get; set; }
    public OfficialReportTemplate? PreviousVersion { get; set; }
    public ICollection<OfficialReportTemplate> LaterVersions { get; set; } = new List<OfficialReportTemplate>();
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<OfficialReportGeneration> Generations { get; set; } = new List<OfficialReportGeneration>();
}

/// <summary>An immutable official output and the exact authorization/data snapshot that produced it.</summary>
public sealed class OfficialReportGeneration
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid GenerationFamilyPublicId { get; set; }
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long ReportingPeriodId { get; set; }
    public long ReportTemplateId { get; set; }
    public string EvidenceBlobId { get; set; } = string.Empty;
    public SubmissionKind SubmissionKind { get; set; }
    public int VersionNumber { get; set; }
    public string ScopeJson { get; set; } = "{}";
    public string FilterJson { get; set; } = "{}";
    public string DataVersionReference { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string GeneratedByUserId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
    public OfficialReportTemplate ReportTemplate { get; set; } = null!;
    public EvidenceBlob Blob { get; set; } = null!;
    public ApplicationUser GeneratedByUser { get; set; } = null!;
}

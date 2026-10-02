namespace FTCERP.Host.Domain.Entities;

public class TechnicalIndicatorDescription
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public long? PreviousVersionId { get; set; }
    public string IndicatorDefinition { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public string CollectionMethod { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public string? NumeratorDescription { get; set; }
    public string? DenominatorDescription { get; set; }
    public string? Limitations { get; set; }
    public string? Assumptions { get; set; }
    public string VerificationMethod { get; set; } = string.Empty;
    public long? ResponsibleEmployeeId { get; set; }
    public string? Notes { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public OpmsTarget OpmsTarget { get; set; } = null!;
    public TechnicalIndicatorDescription? PreviousVersion { get; set; }
    public ICollection<TechnicalIndicatorDescription> SuccessorVersions { get; set; } = new List<TechnicalIndicatorDescription>();
    public MunicipalEmployee? ResponsibleEmployee { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<TidSourceDocument> SourceDocuments { get; set; } = new List<TidSourceDocument>();
}

public class TidSourceDocument
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long TechnicalIndicatorDescriptionId { get; set; }
    public string EvidenceBlobId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedByUserId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public TechnicalIndicatorDescription TechnicalIndicatorDescription { get; set; } = null!;
    public EvidenceBlob Blob { get; set; } = null!;
    public ApplicationUser UploadedByUser { get; set; } = null!;
}

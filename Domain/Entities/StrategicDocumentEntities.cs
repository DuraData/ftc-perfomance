namespace FTCERP.Host.Domain.Entities;

public enum StrategicDocumentEventAction
{
    VersionCreated = 1,
    Approved = 2,
    Published = 3,
    Retired = 4,
    MalwareRescanned = 5
}

public class StrategicDocumentType
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowsExternalLinks { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<StrategicDocument> Documents { get; set; } = new List<StrategicDocument>();
}

public class StrategicDocument
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid DocumentFamilyId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long StrategicDocumentTypeId { get; set; }
    public long? PreviousVersionId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string? SdbipLayer { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DocumentDate { get; set; }
    public string? EvidenceBlobId { get; set; }
    public string? FileName { get; set; }
    public string? ExternalUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCurrent { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public string? ApprovalReference { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public StrategicDocumentType DocumentType { get; set; } = null!;
    public StrategicDocument? PreviousVersion { get; set; }
    public ICollection<StrategicDocument> SuccessorVersions { get; set; } = new List<StrategicDocument>();
    public EvidenceBlob? Blob { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? ApprovedByUser { get; set; }
    public ApplicationUser? PublishedByUser { get; set; }
    public ICollection<StrategicDocumentEvent> Events { get; set; } = new List<StrategicDocumentEvent>();
}

public class StrategicDocumentEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long StrategicDocumentId { get; set; }
    public StrategicDocumentEventAction Action { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Municipality Municipality { get; set; } = null!;
    public StrategicDocument StrategicDocument { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}

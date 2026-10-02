namespace FTCERP.Host.Domain.Entities;

public class EvidenceBlob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public bool SignatureVerified { get; set; }
    public string ScanStatus { get; set; } = "SignatureValidated";
    public bool IsQuarantined { get; set; }
    public string? ScannerProvider { get; set; }
    public string? ScannerReference { get; set; }
    public string? ScanDetail { get; set; }
    public DateTime? ScannedAt { get; set; }
    public bool IsContentDeleted { get; set; }
    public DateTime? ContentDeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public ICollection<PoeFile> PoeAssociations { get; set; } = new List<PoeFile>();
    public ICollection<IdpDocument> IdpDocumentAssociations { get; set; } = new List<IdpDocument>();
    public ICollection<TidSourceDocument> TidSourceDocumentAssociations { get; set; } = new List<TidSourceDocument>();
}

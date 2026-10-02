namespace FTCERP.Host.API.Requests;

public sealed record SaveStrategicDocumentTypeRequest(
    string Code,
    string Name,
    string? Description,
    bool AllowsExternalLinks,
    bool IsActive,
    int DisplayOrder,
    string? RowVersion,
    string Reason);

public sealed class CreateStrategicDocumentVersionRequest
{
    public Guid MunicipalityFinancialYearPublicId { get; set; }
    public Guid DocumentTypePublicId { get; set; }
    public Guid? PreviousVersionPublicId { get; set; }
    public string? PreviousVersionRowVersion { get; set; }
    public string? SdbipLayer { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DocumentDate { get; set; }
    public int DisplayOrder { get; set; }
    public string? ExternalUrl { get; set; }
    public IFormFile? File { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record ApproveStrategicDocumentRequest(string RowVersion, string ApprovalReference, string Reason);
public sealed record PublishStrategicDocumentRequest(string RowVersion, DateTime PublicationDate, string Reason);
public sealed record RetireStrategicDocumentRequest(string RowVersion, string Reason);

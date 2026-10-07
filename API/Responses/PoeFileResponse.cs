namespace FTCERP.Host.API.Responses;

public record PoeFileResponse(
    string Id,
    string SubmissionKind,
    string SubmissionId,
    string FileName,
    string? ContentType,
    long SizeInBytes,
    string? UploadedByUserId,
    string? UploadedByName,
    DateTime UploadedAt,
    string Url)
{
    public Guid PublicId { get; init; }
    public Guid EvidenceBlobPublicId { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public bool SignatureVerified { get; init; }
    public string ScanStatus { get; init; } = string.Empty;
    public bool IsQuarantined { get; init; }
    public string? ScannerProvider { get; init; }
    public string? ScannerReference { get; init; }
    public string? ScanDetail { get; init; }
    public DateTime? ScannedAt { get; init; }
    public DateTime? RetainUntil { get; init; }
    public PoeEvidenceAssessmentResponse[] Assessments { get; init; } = [];
    public string RowVersion { get; init; } = string.Empty;
    public PoeEvidenceReplacementResponse? ReplacementOf { get; init; }
    public PoeEvidenceReplacementResponse? ReplacedBy { get; init; }
    public PoeLegalHoldResponse[] LegalHolds { get; init; } = [];
    public bool IsActive { get; init; }
    public PoeDisposalResponse[] Disposals { get; init; } = [];
    public bool IsContentDeleted { get; init; }
}

public record PoeEvidenceAssessmentResponse(Guid PublicId, string Outcome, string? Comment, string? AssessedByUserId, string? AssessedByName, DateTime AssessedAt, string? CorrelationId);
public record PoeEvidenceReplacementResponse(Guid PublicId, Guid SupersededEvidencePublicId, string SupersededFileName, Guid ReplacementEvidencePublicId, string ReplacementFileName, string Reason, string? ReplacedByUserId, string? ReplacedByName, DateTime ReplacedAt, string? CorrelationId);
public record PoeLegalHoldResponse(Guid HoldId, string HoldReference, bool IsActive, string PlacedReason, string? PlacedByUserId, string? PlacedByName, DateTime PlacedAt, string? ReleasedReason, string? ReleasedByUserId, string? ReleasedByName, DateTime? ReleasedAt);
public record PoeDisposalResponse(Guid DisposalId, string Status, string ApprovalReference, string Reason, string? RequestedByUserId, string? RequestedByName, DateTime RequestedAt, DateTime? CompletedAt, DateTime? FailedAt, string? Detail);

public sealed record PoeResponseMemberAccess(
    bool UploadedByUserId,
    bool UploadedByName,
    bool ScannerProvider,
    bool ScannerReference,
    bool ScanDetail,
    bool AssessmentComment,
    bool AssessedByUserId,
    bool AssessedByName,
    bool AssessmentCorrelationId,
    bool ReplacedByUserId,
    bool ReplacedByName,
    bool ReplacementCorrelationId,
    bool LegalHoldActorUserId,
    bool LegalHoldActorName,
    bool DisposalRequestedByUserId,
    bool DisposalRequestedByName,
    bool DisposalDetail)
{
    public static PoeResponseMemberAccess None { get; } = new(
        false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false);
    public static PoeResponseMemberAccess Full { get; } = new(
        true, true, true, true, true, true, true, true, true,
        true, true, true, true, true, true, true, true);
}

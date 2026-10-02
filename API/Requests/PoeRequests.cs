using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Requests;

public sealed record AssessPoeRequest(PoeAssessmentOutcome Outcome, string? Comment);
public sealed record ReplacePoeRequest(Guid ReplacementEvidencePublicId, string Reason, string SupersededRowVersion, string ReplacementRowVersion);
public sealed record PlacePoeLegalHoldRequest(string HoldReference, string Reason);
public sealed record ReleasePoeLegalHoldRequest(string Reason);
public sealed record RequestPoeDisposalRequest(string ApprovalReference, string Reason, string RowVersion);

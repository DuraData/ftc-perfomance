using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Requests;

public sealed record ReconcileLegacyRatingRequest(
    SubmissionKind SubmissionKind,
    LegacyRatingSourceKind SourceKind,
    string SourceKey,
    Guid WorkflowActionPublicId,
    string Reason);

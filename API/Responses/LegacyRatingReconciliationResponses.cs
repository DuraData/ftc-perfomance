using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Responses;

public sealed record LegacyRatingCandidateDto(
    Guid WorkflowActionPublicId,
    string ActionCode,
    string StageCode,
    string StageName,
    Guid RatingSchemePublicId,
    string RatingSchemeCode,
    Guid RatingValuePublicId,
    decimal RatingValue,
    string RatingLabel,
    string ActorName,
    DateTime OccurredAt,
    bool HasMatchingCanonicalRating);

public sealed record LegacyRatingReconciliationPreviewDto(
    SubmissionKind SubmissionKind,
    Guid SubmissionPublicId,
    LegacyRatingSourceKind SourceKind,
    string SourceKey,
    decimal SourceScore,
    string SourceActorName,
    DateTime? SourceOccurredAt,
    string Status,
    string StatusReason,
    IReadOnlyList<LegacyRatingCandidateDto> Candidates,
    Guid? ReconciliationPublicId);

public sealed record LegacyRatingReconciliationResultDto(
    Guid ReconciliationPublicId,
    Guid StageRatingPublicId,
    bool UsedExistingRating);

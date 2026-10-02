using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Services;

public sealed record RfiEvidencePolicyResult(bool Allowed, string? Error)
{
    public static RfiEvidencePolicyResult Permit() => new(true, null);
    public static RfiEvidencePolicyResult Deny(string error) => new(false, error);
}

public static class RfiEvidencePolicy
{
    public const int MaximumLinksPerAction = 20;

    public static RfiEvidencePolicyResult Validate(SubmissionKind kind, string submissionId, IReadOnlyCollection<Guid> requestedPublicIds, IReadOnlyCollection<PoeFile> files)
    {
        if (requestedPublicIds.Count > MaximumLinksPerAction)
            return RfiEvidencePolicyResult.Deny($"An RFI action may reference at most {MaximumLinksPerAction} evidence records.");

        var requested = requestedPublicIds.Where(id => id != Guid.Empty).Distinct().ToHashSet();
        if (requested.Count != requestedPublicIds.Count || files.Count != requested.Count || files.Any(file => !requested.Contains(file.PublicId)))
            return RfiEvidencePolicyResult.Deny("Every referenced evidence record must exist in the selected municipality.");

        if (files.Any(file => file.SubmissionKind != kind || !string.Equals(file.SubmissionId, submissionId, StringComparison.Ordinal)
            || !file.IsActive || file.Blob.IsContentDeleted || !file.Blob.SignatureVerified || file.Blob.IsQuarantined || !string.Equals(file.Blob.ScanStatus, "Clean", StringComparison.Ordinal)))
            return RfiEvidencePolicyResult.Deny("Every referenced evidence record must be active, clean, signature-verified, and belong to this submission.");

        return RfiEvidencePolicyResult.Permit();
    }
}

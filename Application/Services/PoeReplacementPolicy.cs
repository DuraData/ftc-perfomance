using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Services;

public sealed record PoeReplacementPolicyResult(bool Allowed, string? Error);

public static class PoeReplacementPolicy
{
    public static PoeReplacementPolicyResult Validate(PoeFile superseded, PoeFile replacement, SubmissionKind kind, string submissionId, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 1000)
            return new(false, "A replacement reason between 5 and 1,000 characters is required.");
        if (superseded.Id == replacement.Id || superseded.PublicId == replacement.PublicId)
            return new(false, "Evidence cannot replace itself.");
        if (superseded.SubmissionKind != kind || replacement.SubmissionKind != kind
            || !string.Equals(superseded.SubmissionId, submissionId, StringComparison.Ordinal)
            || !string.Equals(replacement.SubmissionId, submissionId, StringComparison.Ordinal))
            return new(false, "Both evidence records must belong to the same submission.");
        if (!superseded.IsActive || !replacement.IsActive)
            return new(false, "Both evidence records must be active before replacement.");
        if (replacement.Blob.IsContentDeleted || !replacement.Blob.SignatureVerified || replacement.Blob.IsQuarantined || !string.Equals(replacement.Blob.ScanStatus, "Clean", StringComparison.Ordinal))
            return new(false, "Replacement evidence must be signature-verified, clean, and released from quarantine.");
        if (superseded.ReplacementsAsOld.Count != 0 || replacement.ReplacementAsNew != null)
            return new(false, "One of the evidence records already participates in a replacement step.");
        return new(true, null);
    }
}

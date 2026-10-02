using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Services;

public sealed record PoeAssessmentPolicyResult(bool Allowed, string? Error);

public static class PoeAssessmentPolicy
{
    public static PoeAssessmentPolicyResult Validate(PoeFile evidence, PoeAssessmentOutcome outcome, string? comment)
    {
        if (!Enum.IsDefined(outcome)) return new(false, "A valid assessment outcome is required.");
        if (comment?.Length > 2000) return new(false, "Assessment comments may not exceed 2,000 characters.");
        if (outcome != PoeAssessmentOutcome.Accepted && string.IsNullOrWhiteSpace(comment))
            return new(false, "Rejected evidence and clarification requests require a comment.");
        if (!evidence.IsActive || evidence.Blob.IsContentDeleted || !evidence.Blob.SignatureVerified || evidence.Blob.IsQuarantined || !string.Equals(evidence.Blob.ScanStatus, "Clean", StringComparison.Ordinal))
            return new(false, "Only active, released, signature-verified evidence can be assessed.");
        return new(true, null);
    }
}

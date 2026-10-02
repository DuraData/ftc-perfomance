using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Services;

public sealed record PoeDisposalDecision(bool Allowed, string? Error)
{
    public static PoeDisposalDecision Permit() => new(true, null);
    public static PoeDisposalDecision Deny(string error) => new(false, error);
}

public static class PoeDisposalPolicy
{
    public static PoeDisposalDecision Validate(PoeFile evidence, string? approvalReference, string? reason, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(approvalReference) || approvalReference.Trim().Length is < 3 or > 200)
            return PoeDisposalDecision.Deny("An approval reference between 3 and 200 characters is required.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 1000)
            return PoeDisposalDecision.Deny("A disposal reason between 5 and 1,000 characters is required.");
        if (evidence.IsActive)
            return PoeDisposalDecision.Deny("Active evidence cannot be disposed; replace or otherwise retire it first.");
        if (!evidence.RetainUntil.HasValue || evidence.RetainUntil.Value > now)
            return PoeDisposalDecision.Deny("The evidence retention period has not expired.");
        if (PoeLegalHoldPolicy.HasAnyActiveHold(evidence.LegalHoldEvents))
            return PoeDisposalDecision.Deny("Evidence under an active legal hold cannot be disposed.");
        if (evidence.DisposalEvents.GroupBy(item => item.DisposalId).Any(group => group.Any(item => item.Action == PoeDisposalAction.Requested) && group.All(item => item.Action != PoeDisposalAction.Failed)))
            return PoeDisposalDecision.Deny("Evidence already has a pending or completed disposal request.");
        return PoeDisposalDecision.Permit();
    }
}

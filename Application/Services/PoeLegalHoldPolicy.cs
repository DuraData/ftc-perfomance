using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Services;

public static class PoeLegalHoldPolicy
{
    public static string? ValidateText(string? reference, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length is < 3 or > 200)
            return "A legal-hold reference between 3 and 200 characters is required.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 1000)
            return "A legal-hold reason between 5 and 1,000 characters is required.";
        return null;
    }

    public static string? ValidateReleaseReason(string? reason) => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 1000
        ? "A release reason between 5 and 1,000 characters is required."
        : null;

    public static bool IsActive(IEnumerable<PoeLegalHoldEvent> events, Guid holdId)
    {
        var history = events.Where(item => item.HoldId == holdId).ToArray();
        return history.Any(item => item.Action == PoeLegalHoldAction.Placed)
            && history.All(item => item.Action != PoeLegalHoldAction.Released);
    }

    public static bool HasAnyActiveHold(IEnumerable<PoeLegalHoldEvent> events) => events
        .GroupBy(item => item.HoldId)
        .Any(group => group.Any(item => item.Action == PoeLegalHoldAction.Placed) && group.All(item => item.Action != PoeLegalHoldAction.Released));
}

using System.Globalization;
using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Domain.Services;

public static class PerformanceRatioRules
{
    public static string? ValidateDefinition(byte segmentCount, string comparisonMethodCode, IReadOnlyCollection<PerformanceRatioSegment> segments)
    {
        if (segmentCount is < 2 or > 4) return "Ratio segment count must be 2, 3, or 4.";
        var active = segments.Where(item => item.IsActive).OrderBy(item => item.SegmentPosition).ToArray();
        if (active.Length != segmentCount) return $"Ratio configuration requires exactly {segmentCount} active segments.";
        if (!active.Select(item => item.SegmentPosition).SequenceEqual(Enumerable.Range(1, segmentCount).Select(value => (byte)value)))
            return "Ratio segment positions must be unique and consecutive from 1.";
        if (active.Any(item => string.IsNullOrWhiteSpace(item.SegmentLabel) || item.SegmentLabel.Trim().Length > 150))
            return "Every ratio segment requires a label of at most 150 characters.";
        if (active.Any(item => item.DecimalPlaces is > 6)) return "Ratio segment decimal places must be between 0 and 6.";
        if (active.Any(item => item.MinValue.HasValue && item.MaxValue.HasValue && item.MinValue > item.MaxValue))
            return "A ratio segment minimum cannot exceed its maximum.";
        if (comparisonMethodCode == "NORMALIZED_RATIO" && segmentCount != 2)
            return "NORMALIZED_RATIO is valid only for a two-segment ratio.";
        return null;
    }

    public static string? ValidateCanonicalValue(string canonicalValue, PerformanceRatioDefinition definition)
    {
        var parts = canonicalValue.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != definition.RatioSegmentCount)
            return $"Actual Performance must contain exactly {definition.RatioSegmentCount} ratio segments.";

        var segments = definition.Segments.Where(item => item.IsActive).OrderBy(item => item.SegmentPosition).ToArray();
        var definitionError = ValidateDefinition(definition.RatioSegmentCount, definition.RatioComparisonMethod.Code, segments);
        if (definitionError != null) return definitionError;

        for (var index = 0; index < parts.Length; index++)
        {
            if (!decimal.TryParse(parts[index], NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value < 0)
                return $"{segments[index].SegmentLabel} must be a non-negative number.";
            if (segments[index].MinValue is { } minimum && value < minimum)
                return $"{segments[index].SegmentLabel} must be at least {minimum.ToString(CultureInfo.InvariantCulture)}.";
            if (segments[index].MaxValue is { } maximum && value > maximum)
                return $"{segments[index].SegmentLabel} must not exceed {maximum.ToString(CultureInfo.InvariantCulture)}.";
            var decimalPlaces = decimal.GetBits(value)[3] >> 16 & 0x7F;
            if (segments[index].DecimalPlaces.HasValue && decimalPlaces > segments[index].DecimalPlaces)
                return $"{segments[index].SegmentLabel} allows at most {segments[index].DecimalPlaces} decimal places.";
        }

        if (definition.RatioComparisonMethod.Code == "NORMALIZED_RATIO" && decimal.Parse(parts[1], CultureInfo.InvariantCulture) == 0)
            return $"{segments[1].SegmentLabel} cannot be zero for normalized ratio comparison.";
        return null;
    }
}

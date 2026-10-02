using System.Globalization;

namespace FTCERP.Host.Domain.Services;

public enum PerformanceUnitKind
{
    None = 0,
    PercentageBased = 1,
    AbsoluteCount = 2,
    Financial = 3,
    TimeBased = 4,
    AreaBased = 5,
    VolumeBased = 6,
    IndexScores = 7,
    Ratios = 8,
    Binary = 9,
    Date = 10,
    ReadinessScale = 11,
    BinaryDetermination = 12,
    QualitativeTargets = 13,
    ZeroBased = 14,
    ReverseCumulative = 15,
    ReverseNonCumulative = 16
}

public enum PerformanceDirection
{
    HigherIsBetter = 1,
    LowerIsBetter = 2,
    Exact = 3
}

public sealed record PerformanceValueResult(bool IsValid, string? CanonicalValue, string? Error)
{
    public static PerformanceValueResult Valid(string value) => new(true, value, null);
    public static PerformanceValueResult Invalid(string error) => new(false, null, error);
}

public sealed record PerformanceCalculationResult(string CanonicalTarget, string CanonicalActual, decimal? Variance, decimal? AchievementPercent, bool Achieved);

public interface IPerformanceUnitEngine
{
    PerformanceValueResult Normalize(PerformanceUnitKind unit, string? value);
    PerformanceCalculationResult Calculate(PerformanceUnitKind unit, string target, string actual, PerformanceDirection? direction = null);
}

public sealed class PerformanceUnitEngine : IPerformanceUnitEngine
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public PerformanceValueResult Normalize(PerformanceUnitKind unit, string? value)
    {
        var input = value?.Trim();
        if (string.IsNullOrWhiteSpace(input)) return PerformanceValueResult.Invalid("A value is required.");

        return unit switch
        {
            PerformanceUnitKind.None or PerformanceUnitKind.QualitativeTargets => PerformanceValueResult.Valid(input),
            PerformanceUnitKind.Date => NormalizeDate(input),
            PerformanceUnitKind.Binary or PerformanceUnitKind.BinaryDetermination => NormalizeBinary(input),
            PerformanceUnitKind.Ratios => NormalizeRatio(input),
            PerformanceUnitKind.AbsoluteCount => NormalizeNumber(input, integer: true, nonNegative: true),
            PerformanceUnitKind.PercentageBased => NormalizePercentage(input),
            PerformanceUnitKind.ReadinessScale => NormalizeReadiness(input),
            PerformanceUnitKind.Financial or PerformanceUnitKind.TimeBased or PerformanceUnitKind.AreaBased or
            PerformanceUnitKind.VolumeBased or PerformanceUnitKind.IndexScores or PerformanceUnitKind.ZeroBased or
            PerformanceUnitKind.ReverseCumulative or PerformanceUnitKind.ReverseNonCumulative => NormalizeNumber(input, integer: false, nonNegative: true),
            _ => PerformanceValueResult.Invalid("Unsupported performance unit.")
        };
    }

    public PerformanceCalculationResult Calculate(PerformanceUnitKind unit, string target, string actual, PerformanceDirection? direction = null)
    {
        var normalizedTarget = Normalize(unit, target);
        var normalizedActual = Normalize(unit, actual);
        if (!normalizedTarget.IsValid) throw new ArgumentException(normalizedTarget.Error, nameof(target));
        if (!normalizedActual.IsValid) throw new ArgumentException(normalizedActual.Error, nameof(actual));
        var targetValue = normalizedTarget.CanonicalValue!;
        var actualValue = normalizedActual.CanonicalValue!;
        var effectiveDirection = direction ?? DefaultDirection(unit);

        if (unit is PerformanceUnitKind.None or PerformanceUnitKind.QualitativeTargets)
        {
            var achievedText = string.Equals(targetValue, actualValue, StringComparison.OrdinalIgnoreCase);
            return new(targetValue, actualValue, null, achievedText ? 100m : 0m, achievedText);
        }
        if (unit is PerformanceUnitKind.Binary or PerformanceUnitKind.BinaryDetermination)
        {
            var achievedBinary = targetValue == actualValue;
            return new(targetValue, actualValue, null, achievedBinary ? 100m : 0m, achievedBinary);
        }
        if (unit == PerformanceUnitKind.Date)
        {
            var targetDate = DateOnly.ParseExact(targetValue, "yyyy-MM-dd", Invariant);
            var actualDate = DateOnly.ParseExact(actualValue, "yyyy-MM-dd", Invariant);
            var difference = actualDate.DayNumber - targetDate.DayNumber;
            var achievedDate = effectiveDirection switch
            {
                PerformanceDirection.HigherIsBetter => actualDate >= targetDate,
                PerformanceDirection.LowerIsBetter => actualDate <= targetDate,
                _ => actualDate == targetDate
            };
            return new(targetValue, actualValue, difference, achievedDate ? 100m : 0m, achievedDate);
        }
        if (unit == PerformanceUnitKind.Ratios)
        {
            var targets = ParseRatio(targetValue);
            var actuals = ParseRatio(actualValue);
            if (targets.Length != actuals.Length) throw new ArgumentException("Actual ratio must have the same number of segments as the target.", nameof(actual));
            var segmentScores = targets.Zip(actuals).Select(pair => Achievement(pair.First, pair.Second, effectiveDirection)).ToArray();
            var ratioAchieved = targets.Zip(actuals).All(pair => Compare(pair.First, pair.Second, effectiveDirection));
            return new(targetValue, actualValue, null, decimal.Round(segmentScores.Average(), 4), ratioAchieved);
        }

        var targetNumber = decimal.Parse(targetValue, NumberStyles.Number, Invariant);
        var actualNumber = decimal.Parse(actualValue, NumberStyles.Number, Invariant);
        return new(targetValue, actualValue, actualNumber - targetNumber, decimal.Round(Achievement(targetNumber, actualNumber, effectiveDirection), 4), Compare(targetNumber, actualNumber, effectiveDirection));
    }

    private static PerformanceDirection DefaultDirection(PerformanceUnitKind unit) => unit switch
    {
        PerformanceUnitKind.ReverseCumulative or PerformanceUnitKind.ReverseNonCumulative or PerformanceUnitKind.TimeBased or PerformanceUnitKind.Date => PerformanceDirection.LowerIsBetter,
        PerformanceUnitKind.Binary or PerformanceUnitKind.BinaryDetermination or PerformanceUnitKind.QualitativeTargets or PerformanceUnitKind.None or PerformanceUnitKind.ZeroBased => PerformanceDirection.Exact,
        _ => PerformanceDirection.HigherIsBetter
    };

    private static bool Compare(decimal target, decimal actual, PerformanceDirection direction) => direction switch
    {
        PerformanceDirection.HigherIsBetter => actual >= target,
        PerformanceDirection.LowerIsBetter => actual <= target,
        _ => actual == target
    };

    private static decimal Achievement(decimal target, decimal actual, PerformanceDirection direction)
    {
        if (direction == PerformanceDirection.Exact) return actual == target ? 100m : 0m;
        if (target == 0m) return Compare(target, actual, direction) ? 100m : 0m;
        if (direction == PerformanceDirection.HigherIsBetter) return actual / target * 100m;
        if (actual <= target) return 100m;
        return actual == 0m ? 100m : target / actual * 100m;
    }

    private static PerformanceValueResult NormalizeDate(string input) => DateOnly.TryParse(input, Invariant, DateTimeStyles.AllowWhiteSpaces, out var date)
        ? PerformanceValueResult.Valid(date.ToString("yyyy-MM-dd", Invariant))
        : PerformanceValueResult.Invalid("Value must be a valid date.");

    private static PerformanceValueResult NormalizeBinary(string input)
    {
        var normalized = input.ToLowerInvariant();
        if (normalized is "true" or "yes" or "1" or "achieved") return PerformanceValueResult.Valid("true");
        if (normalized is "false" or "no" or "0" or "not achieved" or "not_achieved") return PerformanceValueResult.Valid("false");
        return PerformanceValueResult.Invalid("Binary value must be true/false, yes/no, 1/0, or achieved/not achieved.");
    }

    private static PerformanceValueResult NormalizeRatio(string input)
    {
        var parts = input.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length is < 2 or > 4) return PerformanceValueResult.Invalid("Ratio must contain between two and four colon-delimited segments.");
        var values = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            var normalized = NormalizeNumber(part, integer: false, nonNegative: true);
            if (!normalized.IsValid) return PerformanceValueResult.Invalid("Every ratio segment must be a non-negative number.");
            values.Add(normalized.CanonicalValue!);
        }
        return PerformanceValueResult.Valid(string.Join(':', values));
    }

    private static decimal[] ParseRatio(string value) => value.Split(':').Select(part => decimal.Parse(part, NumberStyles.Number, Invariant)).ToArray();

    private static PerformanceValueResult NormalizePercentage(string input)
    {
        var normalized = NormalizeNumber(input.TrimEnd('%').Trim(), integer: false, nonNegative: true);
        if (!normalized.IsValid) return normalized;
        return decimal.Parse(normalized.CanonicalValue!, Invariant) <= 100m ? normalized : PerformanceValueResult.Invalid("Percentage must be between 0 and 100.");
    }

    private static PerformanceValueResult NormalizeReadiness(string input)
    {
        var normalized = NormalizeNumber(input, integer: true, nonNegative: true);
        if (!normalized.IsValid) return normalized;
        var value = int.Parse(normalized.CanonicalValue!, Invariant);
        return value is >= 1 and <= 5 ? normalized : PerformanceValueResult.Invalid("Readiness scale must be an integer from 1 to 5.");
    }

    private static PerformanceValueResult NormalizeNumber(string input, bool integer, bool nonNegative)
    {
        if (!decimal.TryParse(input, NumberStyles.Number, Invariant, out var value)) return PerformanceValueResult.Invalid("Value must be numeric and use invariant decimal notation.");
        if (nonNegative && value < 0m) return PerformanceValueResult.Invalid("Value cannot be negative.");
        if (integer && decimal.Truncate(value) != value) return PerformanceValueResult.Invalid("Value must be a whole number.");
        return PerformanceValueResult.Valid(value.ToString("0.############################", Invariant));
    }
}

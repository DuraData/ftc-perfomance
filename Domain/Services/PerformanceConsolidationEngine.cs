using System.Globalization;

namespace FTCERP.Host.Domain.Services;

public enum PerformanceCalculationType
{
    Sum = 1,
    Average = 2,
    LatestValue = 3,
    Cumulative = 4,
    NonCumulative = 5,
    ReverseCumulative = 6,
    ReverseNonCumulative = 7,
    ZeroBased = 8,
    Manual = 9
}

public enum ConsolidationMissingValuePolicy
{
    Block = 1,
    Ignore = 2,
    TreatAsZero = 3,
    ManualRequired = 4
}

public sealed record PerformanceConsolidationSource(
    string PeriodCode,
    PerformanceUnitKind UnitKind,
    string? Value,
    bool IsSubmitted,
    bool IsEffective);

public sealed record PerformanceConsolidationRequest(
    PerformanceCalculationType CalculationType,
    PerformanceUnitKind DestinationUnitKind,
    IReadOnlyList<string> RequiredPeriodCodes,
    IReadOnlyCollection<PerformanceConsolidationSource> Sources,
    PerformanceCalculationType? ConfiguredRule = null,
    ConsolidationMissingValuePolicy MissingValuePolicy = ConsolidationMissingValuePolicy.Block);

public sealed record PerformanceConsolidationResult(
    bool CanSuggest,
    string? SuggestedValue,
    bool ManualRequired,
    string Code,
    string Explanation,
    PerformanceCalculationType RequestedCalculationType,
    PerformanceCalculationType? EffectiveCalculationType,
    IReadOnlyList<string> SourcePeriods)
{
    public static PerformanceConsolidationResult Unavailable(
        PerformanceConsolidationRequest request,
        string code,
        string explanation,
        PerformanceCalculationType? effectiveCalculationType = null,
        IReadOnlyList<string>? sourcePeriods = null) =>
        new(false, null, true, code, explanation, request.CalculationType, effectiveCalculationType, sourcePeriods ?? Array.Empty<string>());
}

public interface IPerformanceConsolidationEngine
{
    PerformanceConsolidationResult Consolidate(PerformanceConsolidationRequest request);
}

public sealed class PerformanceConsolidationEngine(IPerformanceUnitEngine unitEngine) : IPerformanceConsolidationEngine
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly HashSet<PerformanceUnitKind> NumericUnits =
    [
        PerformanceUnitKind.PercentageBased,
        PerformanceUnitKind.AbsoluteCount,
        PerformanceUnitKind.Financial,
        PerformanceUnitKind.TimeBased,
        PerformanceUnitKind.AreaBased,
        PerformanceUnitKind.VolumeBased,
        PerformanceUnitKind.IndexScores,
        PerformanceUnitKind.ReadinessScale,
        PerformanceUnitKind.ZeroBased,
        PerformanceUnitKind.ReverseCumulative,
        PerformanceUnitKind.ReverseNonCumulative
    ];

    public PerformanceConsolidationResult Consolidate(PerformanceConsolidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RequiredPeriodCodes);
        ArgumentNullException.ThrowIfNull(request.Sources);

        var requiredPeriods = request.RequiredPeriodCodes
            .Select(code => code?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Cast<string>()
            .ToArray();
        if (requiredPeriods.Length == 0 || requiredPeriods.Distinct(StringComparer.OrdinalIgnoreCase).Count() != requiredPeriods.Length)
        {
            return PerformanceConsolidationResult.Unavailable(
                request,
                "INVALID_PERIOD_SET",
                "Consolidation requires a non-empty, unique ordered set of source periods.");
        }

        var effectiveType = ResolveEffectiveType(request);
        if (effectiveType is null)
        {
            var manual = request.CalculationType == PerformanceCalculationType.Manual;
            return PerformanceConsolidationResult.Unavailable(
                request,
                manual ? "MANUAL_CALCULATION" : "CONFIGURED_RULE_REQUIRED",
                manual
                    ? "This calculation type requires an authorised user to capture the consolidated value manually."
                    : $"{Display(request.CalculationType)} requires an explicit municipality consolidation rule; no automatic value was generated.");
        }

        var sourcesByPeriod = new Dictionary<string, PerformanceConsolidationSource>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in request.Sources)
        {
            var code = source.PeriodCode?.Trim();
            if (string.IsNullOrWhiteSpace(code) || !requiredPeriods.Contains(code, StringComparer.OrdinalIgnoreCase)) continue;
            if (!sourcesByPeriod.TryAdd(code, source))
            {
                return PerformanceConsolidationResult.Unavailable(
                    request,
                    "DUPLICATE_SOURCE_PERIOD",
                    $"More than one source value was supplied for period {code}.",
                    effectiveType);
            }
        }

        var values = new List<(string PeriodCode, string CanonicalValue)>();
        var unavailablePeriods = new List<string>();
        foreach (var period in requiredPeriods)
        {
            if (!sourcesByPeriod.TryGetValue(period, out var source) ||
                !source.IsSubmitted ||
                !source.IsEffective ||
                string.IsNullOrWhiteSpace(source.Value))
            {
                unavailablePeriods.Add(period);
                continue;
            }

            if (source.UnitKind != request.DestinationUnitKind)
            {
                return PerformanceConsolidationResult.Unavailable(
                    request,
                    "INCOMPATIBLE_UNITS",
                    $"Period {period} uses {Display(source.UnitKind)}, which cannot be safely consolidated into {Display(request.DestinationUnitKind)} without an explicit conversion rule.",
                    effectiveType,
                    values.Select(value => value.PeriodCode).ToArray());
            }

            var normalized = unitEngine.Normalize(source.UnitKind, source.Value);
            if (!normalized.IsValid)
            {
                return PerformanceConsolidationResult.Unavailable(
                    request,
                    "INVALID_SOURCE_VALUE",
                    $"Period {period} cannot be consolidated: {normalized.Error}",
                    effectiveType,
                    values.Select(value => value.PeriodCode).ToArray());
            }

            values.Add((period, normalized.CanonicalValue!));
        }

        if (unavailablePeriods.Count > 0)
        {
            if (request.MissingValuePolicy is ConsolidationMissingValuePolicy.Block or ConsolidationMissingValuePolicy.ManualRequired)
            {
                return PerformanceConsolidationResult.Unavailable(
                    request,
                    request.MissingValuePolicy == ConsolidationMissingValuePolicy.Block ? "MISSING_SOURCE_VALUES" : "MISSING_VALUES_REQUIRE_MANUAL_CAPTURE",
                    $"No submitted, effective value is available for: {string.Join(", ", unavailablePeriods)}. Missing values were not treated as zero.",
                    effectiveType,
                    values.Select(value => value.PeriodCode).ToArray());
            }

            if (request.MissingValuePolicy == ConsolidationMissingValuePolicy.TreatAsZero)
            {
                var normalizedZero = unitEngine.Normalize(request.DestinationUnitKind, "0");
                if (!normalizedZero.IsValid)
                {
                    return PerformanceConsolidationResult.Unavailable(
                        request,
                        "ZERO_NOT_VALID_FOR_UNIT",
                        $"The configured missing-as-zero policy is incompatible with {Display(request.DestinationUnitKind)}.",
                        effectiveType,
                        values.Select(value => value.PeriodCode).ToArray());
                }

                foreach (var period in unavailablePeriods)
                {
                    values.Add((period, normalizedZero.CanonicalValue!));
                }
                values = values.OrderBy(value => Array.FindIndex(requiredPeriods, period => string.Equals(period, value.PeriodCode, StringComparison.OrdinalIgnoreCase))).ToList();
            }
        }

        if (values.Count == 0)
        {
            return PerformanceConsolidationResult.Unavailable(
                request,
                "NO_SOURCE_VALUES",
                "No submitted, effective source values are available for consolidation.",
                effectiveType);
        }

        string suggestedValue;
        if (effectiveType == PerformanceCalculationType.LatestValue)
        {
            suggestedValue = values[^1].CanonicalValue;
        }
        else
        {
            if (!NumericUnits.Contains(request.DestinationUnitKind))
            {
                return PerformanceConsolidationResult.Unavailable(
                    request,
                    "UNSAFE_UNIT_FOR_MATHEMATICAL_CONSOLIDATION",
                    $"{Display(request.DestinationUnitKind)} values cannot be safely combined using {Display(effectiveType.Value)}.",
                    effectiveType,
                    values.Select(value => value.PeriodCode).ToArray());
            }

            var numbers = values.Select(value => decimal.Parse(value.CanonicalValue, NumberStyles.Number, Invariant)).ToArray();
            var result = effectiveType switch
            {
                PerformanceCalculationType.Sum => numbers.Sum(),
                PerformanceCalculationType.Average => numbers.Average(),
                _ => throw new InvalidOperationException($"Unsupported effective consolidation type {effectiveType}.")
            };
            var normalizedResult = unitEngine.Normalize(request.DestinationUnitKind, result.ToString("0.############################", Invariant));
            if (!normalizedResult.IsValid)
            {
                return PerformanceConsolidationResult.Unavailable(
                    request,
                    "INVALID_CONSOLIDATED_VALUE",
                    $"The calculated value is invalid for {Display(request.DestinationUnitKind)}: {normalizedResult.Error}",
                    effectiveType,
                    values.Select(value => value.PeriodCode).ToArray());
            }
            suggestedValue = normalizedResult.CanonicalValue!;
        }

        return new PerformanceConsolidationResult(
            true,
            suggestedValue,
            false,
            "SUGGESTION_GENERATED",
            $"Generated from {string.Join(", ", values.Select(value => value.PeriodCode))} using {Display(effectiveType.Value)}.",
            request.CalculationType,
            effectiveType,
            values.Select(value => value.PeriodCode).ToArray());
    }

    private static PerformanceCalculationType? ResolveEffectiveType(PerformanceConsolidationRequest request)
    {
        if (request.ConfiguredRule is not null)
        {
            return request.ConfiguredRule is PerformanceCalculationType.Sum or PerformanceCalculationType.Average or PerformanceCalculationType.LatestValue
                ? request.ConfiguredRule
                : null;
        }

        return request.CalculationType switch
        {
            PerformanceCalculationType.Sum => PerformanceCalculationType.Sum,
            PerformanceCalculationType.Average => PerformanceCalculationType.Average,
            PerformanceCalculationType.LatestValue or PerformanceCalculationType.Cumulative or PerformanceCalculationType.ReverseCumulative => PerformanceCalculationType.LatestValue,
            _ => null
        };
    }

    private static string Display<T>(T value) where T : struct, Enum =>
        string.Concat(value.ToString().Select((character, index) => index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString()));
}

namespace FTCERP.Host.Domain.Services;

public sealed record MidTermTargetDerivationRequest(
    string? ApprovedExplicitTarget,
    PerformanceUnitKind DestinationUnitKind,
    PerformanceCalculationType CalculationType,
    IReadOnlyCollection<PerformanceConsolidationSource> QuarterSources,
    PerformanceCalculationType? ConfiguredRule = null,
    ConsolidationMissingValuePolicy MissingValuePolicy = ConsolidationMissingValuePolicy.Block);

public sealed record MidTermTargetDerivationResult(
    bool CanPersistTarget,
    string? TargetValue,
    bool IsSystemDerivedTarget,
    IReadOnlyList<string> DerivedFromPeriods,
    PerformanceCalculationType? DerivedCalculationType,
    string? SystemSuggestedTargetValue,
    bool ManualRequired,
    string Code,
    string Explanation);

public interface IPerformanceTargetDerivationService
{
    MidTermTargetDerivationResult DeriveMidTerm(MidTermTargetDerivationRequest request);
}

public sealed class PerformanceTargetDerivationService(
    IPerformanceUnitEngine unitEngine,
    IPerformanceConsolidationEngine consolidationEngine) : IPerformanceTargetDerivationService
{
    public MidTermTargetDerivationResult DeriveMidTerm(MidTermTargetDerivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.QuarterSources);

        if (!string.IsNullOrWhiteSpace(request.ApprovedExplicitTarget))
        {
            var explicitTarget = unitEngine.Normalize(request.DestinationUnitKind, request.ApprovedExplicitTarget);
            if (!explicitTarget.IsValid)
            {
                return new(false, null, false, [], null, null, false, "INVALID_EXPLICIT_MID_TERM_TARGET",
                    $"The approved Mid-Term target is invalid: {explicitTarget.Error}");
            }

            return new(true, explicitTarget.CanonicalValue, false, [], null, null, false,
                "EXPLICIT_MID_TERM_TARGET", "The approved source Mid-Term target takes precedence over system derivation.");
        }

        var consolidation = consolidationEngine.Consolidate(new PerformanceConsolidationRequest(
            request.CalculationType,
            request.DestinationUnitKind,
            ["Q1", "Q2"],
            request.QuarterSources,
            request.ConfiguredRule,
            request.MissingValuePolicy));

        if (!consolidation.CanSuggest)
        {
            return new(false, null, false, consolidation.SourcePeriods, consolidation.EffectiveCalculationType, null, true,
                consolidation.Code, consolidation.Explanation);
        }

        return new(true, consolidation.SuggestedValue, true, consolidation.SourcePeriods,
            consolidation.EffectiveCalculationType, consolidation.SuggestedValue, false,
            "SYSTEM_DERIVED_MID_TERM_TARGET", consolidation.Explanation);
    }
}

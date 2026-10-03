using FluentAssertions;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Tests;

public sealed class PerformanceConsolidationEngineTests
{
    private readonly PerformanceConsolidationEngine _engine = new(new PerformanceUnitEngine());

    [Theory]
    [InlineData(PerformanceCalculationType.Sum, "50", PerformanceCalculationType.Sum)]
    [InlineData(PerformanceCalculationType.Average, "25", PerformanceCalculationType.Average)]
    [InlineData(PerformanceCalculationType.LatestValue, "30", PerformanceCalculationType.LatestValue)]
    [InlineData(PerformanceCalculationType.Cumulative, "30", PerformanceCalculationType.LatestValue)]
    [InlineData(PerformanceCalculationType.ReverseCumulative, "30", PerformanceCalculationType.LatestValue)]
    public void DefinedRules_ShouldGenerateCanonicalSuggestions(
        PerformanceCalculationType type,
        string expected,
        PerformanceCalculationType effectiveType)
    {
        var result = _engine.Consolidate(Request(
            type,
            PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20.00"),
            Source("Q2", PerformanceUnitKind.Financial, "30")));

        result.CanSuggest.Should().BeTrue();
        result.SuggestedValue.Should().Be(expected);
        result.EffectiveCalculationType.Should().Be(effectiveType);
        result.SourcePeriods.Should().Equal("Q1", "Q2");
    }

    [Theory]
    [InlineData(PerformanceCalculationType.NonCumulative)]
    [InlineData(PerformanceCalculationType.ReverseNonCumulative)]
    [InlineData(PerformanceCalculationType.ZeroBased)]
    [InlineData(PerformanceCalculationType.Manual)]
    public void UndefinedOrManualRules_ShouldRequireManualCapture(PerformanceCalculationType type)
    {
        var result = _engine.Consolidate(Request(type, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20"),
            Source("Q2", PerformanceUnitKind.Financial, "30")));

        result.CanSuggest.Should().BeFalse();
        result.ManualRequired.Should().BeTrue();
        result.SuggestedValue.Should().BeNull();
        result.Code.Should().Be(type == PerformanceCalculationType.Manual ? "MANUAL_CALCULATION" : "CONFIGURED_RULE_REQUIRED");
    }

    [Theory]
    [InlineData(PerformanceCalculationType.NonCumulative)]
    [InlineData(PerformanceCalculationType.ReverseNonCumulative)]
    [InlineData(PerformanceCalculationType.ZeroBased)]
    public void MunicipalityRule_ShouldMakeOtherwiseUndefinedModesExplicit(PerformanceCalculationType type)
    {
        var request = Request(type, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20"),
            Source("Q2", PerformanceUnitKind.Financial, "30")) with
        {
            ConfiguredRule = PerformanceCalculationType.Average
        };

        var result = _engine.Consolidate(request);

        result.CanSuggest.Should().BeTrue();
        result.SuggestedValue.Should().Be("25");
        result.RequestedCalculationType.Should().Be(type);
        result.EffectiveCalculationType.Should().Be(PerformanceCalculationType.Average);
    }

    [Fact]
    public void MissingSource_ShouldNeverSilentlyBecomeZero()
    {
        var result = _engine.Consolidate(Request(PerformanceCalculationType.Sum, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20")));

        result.CanSuggest.Should().BeFalse();
        result.Code.Should().Be("MISSING_SOURCE_VALUES");
        result.Explanation.Should().Contain("Q2").And.Contain("not treated as zero");
    }

    [Fact]
    public void ExplicitMissingPolicies_ShouldIgnoreOrUseZeroDeterministically()
    {
        var baseRequest = Request(PerformanceCalculationType.Average, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20"));

        _engine.Consolidate(baseRequest with { MissingValuePolicy = ConsolidationMissingValuePolicy.Ignore })
            .SuggestedValue.Should().Be("20");
        _engine.Consolidate(baseRequest with { MissingValuePolicy = ConsolidationMissingValuePolicy.TreatAsZero })
            .SuggestedValue.Should().Be("10");
    }

    [Fact]
    public void NonSubmittedOrIneffectiveSource_ShouldBeUnavailable()
    {
        var request = Request(PerformanceCalculationType.Sum, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20", submitted: false),
            Source("Q2", PerformanceUnitKind.Financial, "30", effective: false));

        var result = _engine.Consolidate(request);

        result.CanSuggest.Should().BeFalse();
        result.Code.Should().Be("MISSING_SOURCE_VALUES");
        result.SourcePeriods.Should().BeEmpty();
    }

    [Fact]
    public void DifferingUnits_ShouldRequireConversionInsteadOfGuessing()
    {
        var result = _engine.Consolidate(Request(PerformanceCalculationType.Sum, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "20"),
            Source("Q2", PerformanceUnitKind.PercentageBased, "30")));

        result.CanSuggest.Should().BeFalse();
        result.Code.Should().Be("INCOMPATIBLE_UNITS");
        result.Explanation.Should().Contain("conversion rule");
    }

    [Theory]
    [InlineData(PerformanceUnitKind.Date, "2026-09-30", "2026-12-31")]
    [InlineData(PerformanceUnitKind.Ratios, "1:2", "2:3")]
    [InlineData(PerformanceUnitKind.QualitativeTargets, "Started", "Complete")]
    [InlineData(PerformanceUnitKind.Binary, "no", "yes")]
    public void MathematicalRules_ShouldRejectNonNumericUnits(PerformanceUnitKind unit, string q1, string q2)
    {
        var result = _engine.Consolidate(Request(PerformanceCalculationType.Sum, unit,
            Source("Q1", unit, q1), Source("Q2", unit, q2)));

        result.CanSuggest.Should().BeFalse();
        result.Code.Should().Be("UNSAFE_UNIT_FOR_MATHEMATICAL_CONSOLIDATION");
    }

    [Theory]
    [InlineData(PerformanceUnitKind.Date, "2026-09-30", "2026-12-31", "2026-12-31")]
    [InlineData(PerformanceUnitKind.Ratios, "1:2", "2:3", "2:3")]
    [InlineData(PerformanceUnitKind.QualitativeTargets, "Started", " Complete ", "Complete")]
    [InlineData(PerformanceUnitKind.Binary, "no", "yes", "true")]
    public void LatestValue_ShouldSafelySelectCanonicalNonNumericValue(PerformanceUnitKind unit, string q1, string q2, string expected)
    {
        var result = _engine.Consolidate(Request(PerformanceCalculationType.LatestValue, unit,
            Source("Q1", unit, q1), Source("Q2", unit, q2)));

        result.CanSuggest.Should().BeTrue();
        result.SuggestedValue.Should().Be(expected);
    }

    [Fact]
    public void CalculatedResult_MustRemainValidForDestinationUnit()
    {
        var result = _engine.Consolidate(Request(PerformanceCalculationType.Average, PerformanceUnitKind.AbsoluteCount,
            Source("Q1", PerformanceUnitKind.AbsoluteCount, "1"),
            Source("Q2", PerformanceUnitKind.AbsoluteCount, "2")));

        result.CanSuggest.Should().BeFalse();
        result.Code.Should().Be("INVALID_CONSOLIDATED_VALUE");
    }

    [Fact]
    public void DuplicatePeriod_ShouldFailClosed()
    {
        var result = _engine.Consolidate(Request(PerformanceCalculationType.Sum, PerformanceUnitKind.Financial,
            Source("Q1", PerformanceUnitKind.Financial, "10"),
            Source("q1", PerformanceUnitKind.Financial, "20"),
            Source("Q2", PerformanceUnitKind.Financial, "30")));

        result.CanSuggest.Should().BeFalse();
        result.Code.Should().Be("DUPLICATE_SOURCE_PERIOD");
    }

    private static PerformanceConsolidationRequest Request(
        PerformanceCalculationType type,
        PerformanceUnitKind unit,
        params PerformanceConsolidationSource[] sources) =>
        new(type, unit, ["Q1", "Q2"], sources);

    private static PerformanceConsolidationSource Source(
        string period,
        PerformanceUnitKind unit,
        string? value,
        bool submitted = true,
        bool effective = true) =>
        new(period, unit, value, submitted, effective);
}

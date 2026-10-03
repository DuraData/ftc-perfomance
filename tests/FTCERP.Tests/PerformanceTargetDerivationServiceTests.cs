using FluentAssertions;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Tests;

public sealed class PerformanceTargetDerivationServiceTests
{
    private readonly PerformanceTargetDerivationService _service;

    public PerformanceTargetDerivationServiceTests()
    {
        var units = new PerformanceUnitEngine();
        _service = new PerformanceTargetDerivationService(units, new PerformanceConsolidationEngine(units));
    }

    [Fact]
    public void ApprovedExplicitTarget_ShouldTakePrecedenceWithoutDerivationMetadata()
    {
        var result = _service.DeriveMidTerm(new(
            " 45.00 ",
            PerformanceUnitKind.Financial,
            PerformanceCalculationType.Sum,
            [Source("Q1", "20"), Source("Q2", "30")]));

        result.CanPersistTarget.Should().BeTrue();
        result.TargetValue.Should().Be("45");
        result.IsSystemDerivedTarget.Should().BeFalse();
        result.DerivedFromPeriods.Should().BeEmpty();
        result.DerivedCalculationType.Should().BeNull();
        result.SystemSuggestedTargetValue.Should().BeNull();
        result.Code.Should().Be("EXPLICIT_MID_TERM_TARGET");
    }

    [Fact]
    public void MissingExplicitTarget_ShouldDeriveAndRetainCompleteMetadata()
    {
        var result = _service.DeriveMidTerm(new(
            null,
            PerformanceUnitKind.Financial,
            PerformanceCalculationType.Sum,
            [Source("Q1", "20"), Source("Q2", "30")]));

        result.CanPersistTarget.Should().BeTrue();
        result.TargetValue.Should().Be("50");
        result.IsSystemDerivedTarget.Should().BeTrue();
        result.DerivedFromPeriods.Should().Equal("Q1", "Q2");
        result.DerivedCalculationType.Should().Be(PerformanceCalculationType.Sum);
        result.SystemSuggestedTargetValue.Should().Be("50");
        result.ManualRequired.Should().BeFalse();
        result.Code.Should().Be("SYSTEM_DERIVED_MID_TERM_TARGET");
    }

    [Fact]
    public void UnsafeDerivation_ShouldReturnNoTargetAndRequireManualResolution()
    {
        var result = _service.DeriveMidTerm(new(
            null,
            PerformanceUnitKind.Ratios,
            PerformanceCalculationType.Sum,
            [new("Q1", PerformanceUnitKind.Ratios, "1:2", true, true), new("Q2", PerformanceUnitKind.Ratios, "2:3", true, true)]));

        result.CanPersistTarget.Should().BeFalse();
        result.TargetValue.Should().BeNull();
        result.SystemSuggestedTargetValue.Should().BeNull();
        result.ManualRequired.Should().BeTrue();
        result.Code.Should().Be("UNSAFE_UNIT_FOR_MATHEMATICAL_CONSOLIDATION");
    }

    [Fact]
    public void InvalidExplicitTarget_ShouldNotFallBackToAComputedValue()
    {
        var result = _service.DeriveMidTerm(new(
            "not-a-date",
            PerformanceUnitKind.Date,
            PerformanceCalculationType.LatestValue,
            [new("Q1", PerformanceUnitKind.Date, "2026-08-01", true, true), new("Q2", PerformanceUnitKind.Date, "2026-09-01", true, true)]));

        result.CanPersistTarget.Should().BeFalse();
        result.ManualRequired.Should().BeFalse();
        result.Code.Should().Be("INVALID_EXPLICIT_MID_TERM_TARGET");
    }

    [Fact]
    public void ConfiguredNonCumulativeRule_ShouldBeRecordedAsTheDerivedRule()
    {
        var result = _service.DeriveMidTerm(new(
            null,
            PerformanceUnitKind.Financial,
            PerformanceCalculationType.NonCumulative,
            [Source("Q1", "20"), Source("Q2", "30")],
            PerformanceCalculationType.Average));

        result.TargetValue.Should().Be("25");
        result.DerivedCalculationType.Should().Be(PerformanceCalculationType.Average);
    }

    private static PerformanceConsolidationSource Source(string period, string value) =>
        new(period, PerformanceUnitKind.Financial, value, true, true);
}

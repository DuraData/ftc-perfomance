using FluentAssertions;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Tests;

public sealed class PerformanceUnitEngineTests
{
    private readonly PerformanceUnitEngine _engine = new();

    [Theory]
    [InlineData(PerformanceUnitKind.PercentageBased, "75.00", "75")]
    [InlineData(PerformanceUnitKind.AbsoluteCount, "12", "12")]
    [InlineData(PerformanceUnitKind.Financial, "1200.50", "1200.5")]
    [InlineData(PerformanceUnitKind.AreaBased, "4.250", "4.25")]
    [InlineData(PerformanceUnitKind.VolumeBased, "8.00", "8")]
    [InlineData(PerformanceUnitKind.IndexScores, "3.75", "3.75")]
    [InlineData(PerformanceUnitKind.TimeBased, "30", "30")]
    [InlineData(PerformanceUnitKind.ZeroBased, "0", "0")]
    [InlineData(PerformanceUnitKind.ReverseCumulative, "10", "10")]
    [InlineData(PerformanceUnitKind.ReverseNonCumulative, "5", "5")]
    [InlineData(PerformanceUnitKind.Binary, "Yes", "true")]
    [InlineData(PerformanceUnitKind.BinaryDetermination, "not achieved", "false")]
    [InlineData(PerformanceUnitKind.Date, "2026/10/01", "2026-10-01")]
    [InlineData(PerformanceUnitKind.ReadinessScale, "4", "4")]
    [InlineData(PerformanceUnitKind.QualitativeTargets, "  Complete policy  ", "Complete policy")]
    public void Normalize_ShouldProduceCanonicalValues(PerformanceUnitKind unit, string input, string canonical)
    {
        _engine.Normalize(unit, input).Should().BeEquivalentTo(PerformanceValueResult.Valid(canonical));
    }

    [Theory]
    [InlineData(PerformanceUnitKind.PercentageBased, "101")]
    [InlineData(PerformanceUnitKind.AbsoluteCount, "1.5")]
    [InlineData(PerformanceUnitKind.AbsoluteCount, "-1")]
    [InlineData(PerformanceUnitKind.ReadinessScale, "6")]
    [InlineData(PerformanceUnitKind.Binary, "maybe")]
    [InlineData(PerformanceUnitKind.Date, "not-a-date")]
    [InlineData(PerformanceUnitKind.Ratios, "1")]
    [InlineData(PerformanceUnitKind.Ratios, "1:2:3:4:5")]
    [InlineData(PerformanceUnitKind.Ratios, "1:-2")]
    public void Normalize_ShouldRejectInvalidBoundaries(PerformanceUnitKind unit, string input)
    {
        _engine.Normalize(unit, input).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Ratio_ShouldCanonicalizeAndCalculateEverySegment()
    {
        var normalized = _engine.Normalize(PerformanceUnitKind.Ratios, " 1.0 : 2 : 3.500 ");
        normalized.CanonicalValue.Should().Be("1:2:3.5");
        var result = _engine.Calculate(PerformanceUnitKind.Ratios, "1:2", "1:3");
        result.Achieved.Should().BeTrue();
        result.AchievementPercent.Should().Be(125m);
    }

    [Fact]
    public void ReverseUnit_ShouldTreatLowerActualAsAchieved()
    {
        var result = _engine.Calculate(PerformanceUnitKind.ReverseCumulative, "20", "15");
        result.Achieved.Should().BeTrue();
        result.Variance.Should().Be(-5m);
        result.AchievementPercent.Should().Be(100m);
    }

    [Fact]
    public void ZeroTarget_ShouldNotDivideByZero()
    {
        _engine.Calculate(PerformanceUnitKind.ZeroBased, "0", "0").AchievementPercent.Should().Be(100m);
        _engine.Calculate(PerformanceUnitKind.ZeroBased, "0", "1").AchievementPercent.Should().Be(0m);
    }

    [Fact]
    public void Date_ShouldUseOnOrBeforeSemanticsByDefault()
    {
        _engine.Calculate(PerformanceUnitKind.Date, "2026-10-31", "2026-10-20").Achieved.Should().BeTrue();
        _engine.Calculate(PerformanceUnitKind.Date, "2026-10-31", "2026-11-01").Variance.Should().Be(1m);
    }
}

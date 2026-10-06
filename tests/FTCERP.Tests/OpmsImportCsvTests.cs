using FluentAssertions;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Tests;

public sealed class OpmsImportCsvTests
{
    [Fact]
    public void Wide_header_contains_each_governed_reporting_period()
    {
        var columns = OpmsImportCsv.WideHeader.Split(',');
        columns.Should().OnlyHaveUniqueItems();
        columns.Should().Contain(["Q1_TARGET", "Q2_TARGET", "MID_TERM_TARGET", "Q3_TARGET", "Q4_TARGET", "ANNUAL_TARGET"]);
        columns.Should().Contain(["EXISTING_INDICATOR_NUMBER", "DEPARTMENT_CODE", "UNIT_CODE"]);
        columns.Should().Contain(["BUDGET_TYPE", "BUDGET_SOURCES"]);
        columns.Should().Contain("KPI_UNIT_OF_MEASURE");
    }

    [Theory]
    [InlineData(PerformanceUnitKind.AbsoluteCount, "ABSOLUTE_COUNT")]
    [InlineData(PerformanceUnitKind.PercentageBased, "PERCENTAGE_BASED")]
    [InlineData(PerformanceUnitKind.ReverseNonCumulative, "REVERSE_NON_CUMULATIVE")]
    [InlineData(PerformanceUnitKind.QualitativeTargets, "QUALITATIVE_TARGETS")]
    public void Exported_units_round_trip_through_the_import_vocabulary(PerformanceUnitKind unit, string expected) =>
        OpmsImportCsv.ToTemplateUnit(unit).Should().Be(expected);

    [Theory]
    [InlineData(PerformanceDirection.HigherIsBetter, "HIGHER_IS_BETTER")]
    [InlineData(PerformanceDirection.LowerIsBetter, "LOWER_IS_BETTER")]
    [InlineData(PerformanceDirection.Exact, "EXACT")]
    public void Exported_directions_round_trip_through_the_import_vocabulary(PerformanceDirection direction, string expected) =>
        OpmsImportCsv.ToTemplateDirection(direction).Should().Be(expected);
}

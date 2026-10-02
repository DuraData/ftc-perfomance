using FluentAssertions;
using FTCERP.Host.API.Controllers;

namespace FTCERP.Tests;

public sealed class PerformanceReportCsvTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"https://example.invalid\")", "\"'=HYPERLINK(\"\"https://example.invalid\"\")\"")]
    [InlineData("+1+1", "\"'+1+1\"")]
    [InlineData("-2+3", "\"'-2+3\"")]
    [InlineData("@SUM(A1:A2)", "\"'@SUM(A1:A2)\"")]
    public void Encode_NeutralizesSpreadsheetFormulaPrefixes(string value, string expected)
    {
        PerformanceReportCsv.Encode(value).Should().Be(expected);
    }

    [Fact]
    public void Encode_QuotesEmbeddedDelimitersAndLineBreaks()
    {
        PerformanceReportCsv.Encode("Finance, \"Rates\"\r\nReviewed").Should().Be("\"Finance, \"\"Rates\"\"\r\nReviewed\"");
    }
}

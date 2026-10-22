using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace FTCERP.Tests;

public sealed class SingleComplianceReportGovernanceTests
{
    private const string AuthoritativeReport = "docs/reviews/OPMS_V3_9_Repository_Compliance_Assessment.md";

    [Fact]
    public void Repository_tracks_one_authoritative_compliance_report()
    {
        var root = FindRepositoryRoot();
        var markdownFiles = GetTrackedMarkdownFiles(root);
        var reportLikeFiles = markdownFiles
            .Where(path => File.Exists(Path.Combine(root.FullName, path.Replace('/', Path.DirectorySeparatorChar))))
            .Where(path => Regex.IsMatch(
                Path.GetFileNameWithoutExtension(path),
                "audit|assessment|compliance|report",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        reportLikeFiles.Should().ContainSingle(
            "OPMS V3.9 has one authoritative compliance ledger; parallel audit/report files become stale and contradictory");
        reportLikeFiles.Single().Should().Be(AuthoritativeReport);

        var reportPath = Path.Combine(root.FullName, AuthoritativeReport.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(reportPath).Should().BeTrue("the authoritative compliance report must remain tracked");
        var report = File.ReadAllText(reportPath);
        report.Should()
            .Contain("SOFTWARE IMPLEMENTATION: SUBSTANTIALLY COMPLIANT — EXTERNAL ACCEPTANCE REMAINS.",
                "the report must state the evidence-based software implementation verdict")
            .And.Contain("PRODUCTION ACCEPTANCE: NOT PRODUCTION READY.",
                "external acceptance must remain visibly separate from repository implementation status");
    }

    private static string[] GetTrackedMarkdownFiles(DirectoryInfo root)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root.FullName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("*.md");

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull("git is a declared local quality-gate dependency");
        var output = process!.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, "tracked Markdown inventory must be readable: {0}", error);

        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "FTCERP.Host.csproj")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

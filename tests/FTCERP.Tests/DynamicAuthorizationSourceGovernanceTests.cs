using System.Text.RegularExpressions;

namespace FTCERP.Tests;

public sealed class DynamicAuthorizationSourceGovernanceTests
{
    private static readonly Regex ForbiddenAuthorizationConstruct = new(
        @"\bIsInRole(?:Async)?\s*\(|\bRequireRole\s*\(|\bClaimTypes\.Role\b|\bAuthorize\s*\(\s*Roles\s*=",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BaselineRoleLiteral = new(
        "\"(?:Super Admin|Admin|Client Admin|Auditor General|PMS / Performance Manager|Internal Audit|Reviewer|Approver|Verifier|Submitter)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Production_authorization_is_not_derived_from_role_names_or_role_claims()
    {
        var root = FindRepositoryRoot();
        var productionFiles = new[] { "API", "Application", "Domain", "Infrastructure" }
            .Select(path => new DirectoryInfo(Path.Combine(root.FullName, path)))
            .SelectMany(directory => directory.GetFiles("*.cs", SearchOption.AllDirectories))
            .Where(file => !file.FullName.Contains(
                $"{Path.DirectorySeparatorChar}Infrastructure{Path.DirectorySeparatorChar}Persistence{Path.DirectorySeparatorChar}Seed{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Append(new FileInfo(Path.Combine(root.FullName, "Program.cs")))
            .ToArray();

        var forbiddenConstructs = productionFiles
            .SelectMany(file => FindMatches(file, ForbiddenAuthorizationConstruct))
            .ToArray();
        var baselineRoleLiterals = productionFiles
            .SelectMany(file => FindMatches(file, BaselineRoleLiteral))
            .ToArray();

        forbiddenConstructs.Should().BeEmpty(
            "runtime authorization must use central database-backed permission policies, actions, members and scopes");
        baselineRoleLiterals.Should().BeEmpty(
            "baseline role names are controlled seed data and must not become runtime authorization switches");
    }

    private static IEnumerable<string> FindMatches(FileInfo file, Regex pattern)
    {
        var lines = File.ReadAllLines(file.FullName);
        return lines.SelectMany((line, index) => pattern.Matches(line)
            .Select(match => $"{Path.GetRelativePath(FindRepositoryRoot().FullName, file.FullName)}:{index + 1}: {match.Value}"));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Program.cs"))
                && Directory.Exists(Path.Combine(directory.FullName, "API", "Controllers")))
                return directory;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}

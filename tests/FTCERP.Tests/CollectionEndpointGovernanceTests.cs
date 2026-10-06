using System.Text.RegularExpressions;

namespace FTCERP.Tests;

public sealed class CollectionEndpointGovernanceTests
{
    private static readonly IReadOnlyDictionary<string, string> ReviewedPurposeBoundedArrays =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessController.GetMyPermissions"] = "Complete effective capability snapshot required for client-side presentation; bounded by the governed permission registry.",
            ["AccessController.GetSystemCoverageAudit"] = "One row per role in the fixed system-role catalogue.",
            ["AuthenticationAdministrationController.GetProviders"] = "Finite deployment-configured enterprise identity-provider registry.",
            ["NavigationController.GetMyMenu"] = "Complete authorized navigation hierarchy required to render the current user's menu; bounded by the governed navigation registry.",
            ["PerformanceConsolidationController.GetCalculationTypes"] = "Finite controlled performance-calculation type catalogue.",
            ["PerformanceConsolidationController.GetPolicies"] = "At most one tenant policy per controlled performance-calculation type.",
            ["PerformancePeriodTargetsController.Get"] = "Exactly one KPI's period values; uniqueness and the controlled reporting-period catalogue bound cardinality.",
            ["RoleImplementationAuditController.GetAudit"] = "One audit result per role in the fixed system-role catalogue."
        };

    [Fact]
    public void Successful_raw_array_contracts_are_limited_to_reviewed_purpose_bounded_endpoints()
    {
        var controllerDirectory = FindRepositoryRoot().GetDirectories("API").Single().GetDirectories("Controllers").Single();
        var successfulArrays = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in controllerDirectory.GetFiles("*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file.FullName);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (!line.Contains("Ok(new ApiResponse<", StringComparison.Ordinal) || !line.Contains("[]>", StringComparison.Ordinal))
                    continue;

                var declarationStart = lineIndex;
                while (declarationStart >= 0 && !lines[declarationStart].TrimStart().StartsWith("public ", StringComparison.Ordinal))
                    declarationStart--;
                declarationStart.Should().BeGreaterThanOrEqualTo(0, $"a public action declaration should precede {file.Name}:{lineIndex + 1}");

                var declaration = lines[declarationStart];
                var arrowIndex = declaration.IndexOf("=>", StringComparison.Ordinal);
                if (arrowIndex >= 0) declaration = declaration[..arrowIndex];
                var method = Regex.Matches(declaration, @"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(")
                    .Select(match => match.Groups[1].Value)
                    .LastOrDefault();
                method.Should().NotBeNullOrWhiteSpace($"the action name should be identifiable at {file.Name}:{lineIndex + 1}");
                successfulArrays.Add($"{Path.GetFileNameWithoutExtension(file.Name)}.{method}");
            }
        }

        successfulArrays.Should().BeEquivalentTo(ReviewedPurposeBoundedArrays.Keys,
            "new successful raw-array endpoints must use a page contract or receive an explicit, reviewed finite-cardinality justification");
        ReviewedPurposeBoundedArrays.Values.Should().OnlyContain(reason => reason.Length >= 40);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "API", "Controllers"))) return directory;
        }

        throw new DirectoryNotFoundException("Could not locate API/Controllers from the test output directory.");
    }
}

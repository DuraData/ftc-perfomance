using System.Text.RegularExpressions;

namespace FTCERP.Tests;

public sealed class SecurityMemberSurfaceGovernanceTests
{
    private static readonly Regex MemberDefinitionPattern = new(
        "Member\\(\\\"(?<resource>[^\\\"]+)\\\",\\s*\\\"(?<member>[^\\\"]+)\\\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Every_registered_member_has_a_production_enforcement_or_presentation_surface()
    {
        var root = FindRepositoryRoot();
        var seederPath = Path.Combine(root.FullName, "Infrastructure", "Persistence", "Seed", "SecurityRegistrySeeder.cs");
        var definitions = MemberDefinitionPattern.Matches(File.ReadAllText(seederPath))
            .Select(match => (Resource: match.Groups["resource"].Value, Member: match.Groups["member"].Value))
            .Distinct()
            .ToArray();
        definitions.Should().NotBeEmpty("the database-driven member registry must remain discoverable");

        var productionRoots = new[] { "API", "Application", "Infrastructure", "Domain", Path.Combine("ClientApp", "src") };
        var productionFiles = productionRoots
            .Select(path => new DirectoryInfo(Path.Combine(root.FullName, path)))
            .SelectMany(directory => directory.GetFiles("*", SearchOption.AllDirectories))
            .Where(file => file.Extension is ".cs" or ".ts" or ".tsx")
            .Where(file => !string.Equals(file.FullName, seederPath, StringComparison.OrdinalIgnoreCase))
            .Select(file => File.ReadAllText(file.FullName))
            .ToArray();

        var missing = definitions.Where(definition => !productionFiles.Any(source =>
                source.Contains($"{definition.Resource}.{definition.Member}", StringComparison.OrdinalIgnoreCase)
                || (source.Contains(definition.Resource, StringComparison.OrdinalIgnoreCase)
                    && (source.Contains($"\"{definition.Member}\"", StringComparison.Ordinal)
                        || source.Contains($"'{definition.Member}'", StringComparison.Ordinal)))))
            .Select(definition => $"{definition.Resource}.{definition.Member}")
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "registered members must not be decorative: each requires an exact permission reference or a resource/member handling surface in production code");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Infrastructure", "Persistence", "Seed", "SecurityRegistrySeeder.cs")))
                return directory;
        }

        throw new DirectoryNotFoundException("Could not locate the security registry seeder from the test output directory.");
    }
}

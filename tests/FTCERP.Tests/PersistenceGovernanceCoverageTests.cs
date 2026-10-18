using System.Text.RegularExpressions;

namespace FTCERP.Tests;

public sealed class PersistenceGovernanceCoverageTests
{
    [Fact]
    public void Every_explicit_persistence_projection_is_accounted_for_by_the_save_pipeline_governance_inventory()
    {
        var root = FindRepositoryRoot();
        var persistenceDirectory = Path.Combine(root.FullName, "Infrastructure", "Persistence");
        var source = string.Join(Environment.NewLine,
            Directory.EnumerateFiles(persistenceDirectory, "ApplicationDbContext*.cs", SearchOption.TopDirectoryOnly)
                .Select(File.ReadAllText));

        Regex.Matches(source, @"EnforceAppendOnlyRecords\(\);").Should().HaveCount(2,
            "both synchronous and asynchronous SaveChanges paths must execute persistence governance");
        source.Should().Contain("EnforceStrategicPlanningMasterHistory(strategicMasterEntries);")
            .And.Contain("EnforceStrategicPlanningRelationshipHistory();")
            .And.Contain("entry.Metadata.ClrType == typeof(OpmsSubmission)")
            .And.Contain("entry.Metadata.ClrType == typeof(IpmsSubmission)");

        var directlyGuardedNames = Regex.Matches(source,
                @"(?:ChangeTracker\.Entries|RejectRewrites|Enforce[A-Za-z0-9_]*|Ensure[A-Za-z0-9_]*)<(?<type>[A-Za-z0-9_]+)>")
            .Select(match => match.Groups["type"].Value)
            .ToHashSet(StringComparer.Ordinal);
        directlyGuardedNames.Add(nameof(OpmsSubmission));
        directlyGuardedNames.Add(nameof(IpmsSubmission));

        var dbSetTypes = typeof(ApplicationDbContext).GetProperties()
            .Where(property => property.DeclaringType == typeof(ApplicationDbContext)
                && property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.PropertyType.GenericTypeArguments[0])
            .Distinct()
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        dbSetTypes.Should().HaveCountGreaterThanOrEqualTo(174,
            "the inventory must not silently pass because context discovery stopped working");

        var uncovered = dbSetTypes.Where(type =>
                !directlyGuardedNames.Contains(type.Name)
                && !typeof(StrategicPlanningMasterBase).IsAssignableFrom(type)
                && !typeof(StrategicPlanningRelationshipBase).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToArray();

        uncovered.Should().BeEmpty(
            "every explicit DbSet must remain attached to a reachable history, audit, lifecycle, or immutability guard");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "FTCERP.Host.csproj"))) current = current.Parent;
        return current ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

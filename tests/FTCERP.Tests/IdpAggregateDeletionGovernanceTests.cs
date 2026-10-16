using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class IdpAggregateDeletionGovernanceTests
{
    private static readonly Relationship[] GovernedRelationships =
    [
        new(typeof(IdpPlanVersion), typeof(IdpPlan), nameof(IdpPlanVersion.IdpPlanId), "IdpPlanVersions", "IdpPlans", "TRG_IdpPlans_IdpPlanVersions_RestrictDelete"),
        new(typeof(IdpChangeLog), typeof(IdpPlanVersion), nameof(IdpChangeLog.IdpPlanVersionId), "IdpChangeLogs", "IdpPlanVersions", "TRG_IdpPlanVersions_IdpChangeLogs_RestrictDelete"),
        new(typeof(IdpStrategicOutcome), typeof(IdpPlan), nameof(IdpStrategicOutcome.IdpPlanId), "IdpStrategicOutcomes", "IdpPlans", "TRG_IdpPlans_IdpStrategicOutcomes_RestrictDelete"),
        new(typeof(IdpStrategicObjective), typeof(IdpStrategicOutcome), nameof(IdpStrategicObjective.IdpStrategicOutcomeId), "IdpStrategicObjectives", "IdpStrategicOutcomes", "TRG_IdpStrategicOutcomes_IdpStrategicObjectives_RestrictDelete"),
        new(typeof(IdpDevelopmentPriority), typeof(IdpStrategicObjective), nameof(IdpDevelopmentPriority.IdpStrategicObjectiveId), "IdpDevelopmentPriorities", "IdpStrategicObjectives", "TRG_IdpStrategicObjectives_IdpDevelopmentPriorities_RestrictDelete"),
        new(typeof(IdpProgramme), typeof(IdpDevelopmentPriority), nameof(IdpProgramme.IdpDevelopmentPriorityId), "IdpProgrammes", "IdpDevelopmentPriorities", "TRG_IdpDevelopmentPriorities_IdpProgrammes_RestrictDelete"),
        new(typeof(IdpProject), typeof(IdpProgramme), nameof(IdpProject.IdpProgrammeId), "IdpProjects", "IdpProgrammes", "TRG_IdpProgrammes_IdpProjects_RestrictDelete"),
        new(typeof(IdpKpi), typeof(IdpProject), nameof(IdpKpi.IdpProjectId), "IdpKpis", "IdpProjects", "TRG_IdpProjects_IdpKpis_RestrictDelete"),
        new(typeof(IdpImportRow), typeof(IdpImportBatch), nameof(IdpImportRow.IdpImportBatchId), "IdpImportRows", "IdpImportBatches", "TRG_IdpImportBatches_IdpImportRows_RestrictDelete"),
        new(typeof(IdpAnnualTarget), typeof(IdpKpi), nameof(IdpAnnualTarget.IdpKpiId), "IdpAnnualTargets", "IdpKpis", "TRG_IdpKpis_IdpAnnualTargets_RestrictDelete"),
        new(typeof(IdpAlignmentLink), typeof(IdpStrategicObjective), nameof(IdpAlignmentLink.IdpStrategicObjectiveId), "IdpAlignmentLinks", "IdpStrategicObjectives", "TRG_IdpStrategicObjectives_IdpAlignmentLinks_RestrictDelete"),
        new(typeof(IdpCommunitySession), typeof(IdpPlan), nameof(IdpCommunitySession.IdpPlanId), "IdpCommunitySessions", "IdpPlans", "TRG_IdpPlans_IdpCommunitySessions_RestrictDelete"),
        new(typeof(IdpCommunityNeed), typeof(IdpCommunitySession), nameof(IdpCommunityNeed.IdpCommunitySessionId), "IdpCommunityNeeds", "IdpCommunitySessions", "TRG_IdpCommunitySessions_IdpCommunityNeeds_RestrictDelete"),
        new(typeof(IdpWardInput), typeof(IdpPlan), nameof(IdpWardInput.IdpPlanId), "IdpWardInputs", "IdpPlans", "TRG_IdpPlans_IdpWardInputs_RestrictDelete"),
        new(typeof(IdpStakeholderEngagement), typeof(IdpCommunitySession), nameof(IdpStakeholderEngagement.IdpCommunitySessionId), "IdpStakeholderEngagements", "IdpCommunitySessions", "TRG_IdpCommunitySessions_IdpStakeholderEngagements_RestrictDelete"),
        new(typeof(IdpCollaborationComment), typeof(IdpPlan), nameof(IdpCollaborationComment.IdpPlanId), "IdpCollaborationComments", "IdpPlans", "TRG_IdpPlans_IdpCollaborationComments_RestrictDelete"),
        new(typeof(IdpTaskAssignment), typeof(IdpPlan), nameof(IdpTaskAssignment.IdpPlanId), "IdpTaskAssignments", "IdpPlans", "TRG_IdpPlans_IdpTaskAssignments_RestrictDelete")
    ];

    [Fact]
    public void All_governed_idp_history_relationships_use_restrictive_model_semantics()
    {
        using var context = IdpTestFixture.CreateRelationalContext();

        GovernedRelationships.Should().HaveCount(17);
        foreach (var relationship in GovernedRelationships)
        {
            var dependent = context.Model.FindEntityType(relationship.DependentType);
            dependent.Should().NotBeNull();
            var foreignKey = dependent!.GetForeignKeys().Single(candidate =>
                candidate.PrincipalEntityType.ClrType == relationship.PrincipalType
                && candidate.Properties.Count == 1
                && candidate.Properties[0].Name == relationship.ForeignKey);

            foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict,
                $"{relationship.DependentType.Name}.{relationship.ForeignKey} must not cascade-delete governed history");
        }
    }

    [Fact]
    public async Task Clean_sqlite_schema_enforces_every_governed_relationship_as_restrictive()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        await context.Database.OpenConnectionAsync();

        foreach (var relationship in GovernedRelationships)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"PRAGMA foreign_key_list(\"{relationship.DependentTable}\");";
            await using var reader = await command.ExecuteReaderAsync();
            var found = false;
            while (await reader.ReadAsync())
            {
                if (!string.Equals(reader.GetString(2), relationship.PrincipalTable, StringComparison.Ordinal)
                    || !string.Equals(reader.GetString(3), relationship.ForeignKey, StringComparison.Ordinal))
                    continue;

                reader.GetString(6).Should().Be("RESTRICT",
                    $"{relationship.DependentTable}.{relationship.ForeignKey} must reject direct parent deletion");
                found = true;
            }

            found.Should().BeTrue($"the database must contain {relationship.DependentTable}.{relationship.ForeignKey}");
        }
    }

    [Fact]
    public async Task Sqlite_incremental_upgrade_installs_all_history_guards_and_preserves_existing_rows()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-idp-history-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var seeded = await SeedPlanHistoryAsync(context);
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            const string latestMigration = "20261013152126_V39RestrictiveIdpAggregateHistory";
            foreach (var migration in context.Database.GetMigrations().TakeWhile(item => item != latestMigration))
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            await context.GetService<IMigrator>().MigrateAsync(latestMigration);

            var triggers = new Dictionary<string, string>(StringComparer.Ordinal);
            await context.Database.OpenConnectionAsync();
            await using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'trigger' AND name LIKE 'TRG_Idp%_RestrictDelete' ORDER BY name;";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    triggers.Add(reader.GetString(0), reader.GetString(1));
            }

            triggers.Should().HaveCount(GovernedRelationships.Length);
            foreach (var relationship in GovernedRelationships)
            {
                triggers.Should().ContainKey(relationship.TriggerName);
                var sql = triggers[relationship.TriggerName];
                sql.Should().Contain($"BEFORE DELETE ON \"{relationship.PrincipalTable}\"");
                sql.Should().Contain($"FROM \"{relationship.DependentTable}\"");
                sql.Should().Contain($"\"{relationship.ForeignKey}\" = OLD.\"Id\"");
            }

            (await context.IdpPlans.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.IdpPlanVersions.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.IdpChangeLogs.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM IdpPlanVersions WHERE Id = {seeded.VersionId}"))
                .Should().ThrowAsync<SqliteException>();
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM IdpPlans WHERE Id = {seeded.PlanId}"))
                .Should().ThrowAsync<SqliteException>();

            await using var foreignKeyCheck = context.Database.GetDbConnection().CreateCommand();
            foreignKeyCheck.CommandText = "PRAGMA foreign_key_check;";
            await using var violations = await foreignKeyCheck.ExecuteReaderAsync();
            (await violations.ReadAsync()).Should().BeFalse();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static async Task<(int PlanId, int VersionId)> SeedPlanHistoryAsync(ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser($"idp-history-{Guid.NewGuid():N}");
        var plan = new IdpPlan
        {
            PlanTitle = "Preserved IDP",
            PlanCode = $"IDP-{Guid.NewGuid():N}",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUser = user
        };
        var version = new IdpPlanVersion
        {
            IdpPlan = plan,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original",
            CreatedByUser = user
        };
        var change = new IdpChangeLog
        {
            IdpPlanVersion = version,
            EntityName = nameof(IdpPlan),
            EntityId = plan.PublicId.ToString(),
            ChangeType = "Create",
            AfterValue = "{\"version\":1}",
            ChangedByUser = user
        };
        context.Add(change);
        await context.SaveChangesAsync();
        return (plan.Id, version.Id);
    }

    private sealed record Relationship(
        Type DependentType,
        Type PrincipalType,
        string ForeignKey,
        string DependentTable,
        string PrincipalTable,
        string TriggerName);
}

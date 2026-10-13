using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class PerformanceTargetDeletionGovernanceTests
{
    [Fact]
    public async Task Sqlite_incremental_upgrade_preserves_targets_and_submissions_with_restrictive_foreign_keys()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-target-delete-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var seeded = await SeedAsync(context);
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            const string latestMigration = "20261013141506_V39GovernedPerformanceTargetDeletion";
            foreach (var migration in context.Database.GetMigrations().Where(item => item != latestMigration))
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            await context.GetService<IMigrator>().MigrateAsync(latestMigration);

            (await context.OpmsTargets.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.IpmsTargets.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.OpmsSubmissions.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.IpmsSubmissions.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.LegacySubmissionValueArchives.IgnoreQueryFilters().CountAsync()).Should().Be(2);
            await using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_key_check;";
                await context.Database.OpenConnectionAsync();
                await using var reader = await command.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeFalse("the rebuild must preserve every referencing ledger relationship");
            }
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM OpmsTargets WHERE Id = {seeded.OpmsTarget.Id}"))
                .Should().ThrowAsync<SqliteException>();
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM IpmsTargets WHERE Id = {seeded.IpmsTarget.Id}"))
                .Should().ThrowAsync<SqliteException>();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Opms_target_cannot_be_hard_deleted_through_tracked_persistence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);

        context.OpmsTargets.Remove(seeded.OpmsTarget);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-target history cannot be hard deleted*");
    }

    [Fact]
    public async Task Ipms_target_cannot_be_hard_deleted_through_tracked_persistence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);

        context.IpmsTargets.Remove(seeded.IpmsTarget);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-target history cannot be hard deleted*");
    }

    [Fact]
    public async Task Database_foreign_keys_reject_direct_target_deletion_and_preserve_submissions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);

        await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM OpmsTargets WHERE Id = {seeded.OpmsTarget.Id}"))
            .Should().ThrowAsync<SqliteException>();
        await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM IpmsTargets WHERE Id = {seeded.IpmsTarget.Id}"))
            .Should().ThrowAsync<SqliteException>();

        (await context.OpmsTargets.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.IpmsTargets.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.OpmsSubmissions.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await context.IpmsSubmissions.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    private static async Task<SeededTargets> SeedAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = $"TARGET-DEL-{Guid.NewGuid():N}"[..30], Name = "Target deletion governance" };
        var user = IdpTestFixture.CreateUser($"target-delete-{Guid.NewGuid():N}");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var opmsTarget = new OpmsTarget
        {
            Id = $"opms-delete-{Guid.NewGuid():N}", MunicipalityId = municipality.Id,
            AssignedUserId = user.Id, IndicatorNumber = "OPMS-DELETE", TargetName = "Preserved OPMS target",
            KpiDescription = "Preserved OPMS target", AnnualTargetDescription = "One"
        };
        var ipmsTarget = new IpmsTarget
        {
            Id = $"ipms-delete-{Guid.NewGuid():N}", MunicipalityId = municipality.Id,
            AssignedUserId = user.Id, IndicatorNumber = "IPMS-DELETE", TargetName = "Preserved IPMS target",
            KpiDescription = "Preserved IPMS target", AnnualTargetDescription = "One"
        };
        var opmsSubmission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTarget = opmsTarget, Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress, Status = "Draft", CreatedBy = user.Id
        };
        var ipmsSubmission = new IpmsSubmission
        {
            MunicipalityId = municipality.Id, IpmsTarget = ipmsTarget, Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress, Status = "Draft", CreatedBy = user.Id
        };
        context.AddRange(opmsTarget, ipmsTarget, opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();
        context.LegacySubmissionValueArchives.AddRange(
            new LegacySubmissionValueArchive
            {
                MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms,
                OpmsSubmissionId = opmsSubmission.Id, CanonicalActualPerformance = "10"
            },
            new LegacySubmissionValueArchive
            {
                MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Ipms,
                IpmsSubmissionId = ipmsSubmission.Id, CanonicalActualPerformance = "20"
            });
        await context.SaveChangesAsync();

        return new SeededTargets(opmsTarget, ipmsTarget);
    }

    private sealed record SeededTargets(OpmsTarget OpmsTarget, IpmsTarget IpmsTarget);
}

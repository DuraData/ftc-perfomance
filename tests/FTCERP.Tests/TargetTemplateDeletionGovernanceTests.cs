using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class TargetTemplateDeletionGovernanceTests
{
    [Fact]
    public async Task Sqlite_incremental_upgrade_preserves_template_versions_with_restrictive_foreign_keys()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-template-delete-{Guid.NewGuid():N}.db");
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
            const string latestMigration = "20261013145316_V39GovernedTargetTemplateDeletion";
            foreach (var migration in context.Database.GetMigrations().TakeWhile(item => item != latestMigration))
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            await context.GetService<IMigrator>().MigrateAsync(latestMigration);

            (await context.OpmsTargetTemplates.CountAsync()).Should().Be(1);
            (await context.IpmsTargetTemplates.CountAsync()).Should().Be(1);
            (await context.OpmsTargetTemplateVersions.CountAsync()).Should().Be(1);
            (await context.IpmsTargetTemplateVersions.CountAsync()).Should().Be(1);
            await using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_key_check;";
                await context.Database.OpenConnectionAsync();
                await using var reader = await command.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeFalse("the rebuild must preserve both immutable version histories");
            }
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM OpmsTargetTemplates WHERE Id = {seeded.Opms.Id}"))
                .Should().ThrowAsync<SqliteException>();
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM IpmsTargetTemplates WHERE Id = {seeded.Ipms.Id}"))
                .Should().ThrowAsync<SqliteException>();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Opms_template_cannot_be_hard_deleted_through_tracked_persistence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);
        context.ChangeTracker.Clear();
        context.OpmsTargetTemplates.Remove(await context.OpmsTargetTemplates.SingleAsync(item => item.Id == seeded.Opms.Id));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OPMS target-template history cannot be hard deleted*");
    }

    [Fact]
    public async Task Ipms_template_cannot_be_hard_deleted_through_tracked_persistence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);
        context.ChangeTracker.Clear();
        context.IpmsTargetTemplates.Remove(await context.IpmsTargetTemplates.SingleAsync(item => item.Id == seeded.Ipms.Id));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IPMS target-template history cannot be hard deleted*");
    }

    [Fact]
    public async Task Database_foreign_keys_reject_direct_template_deletion_and_preserve_versions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);

        await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM OpmsTargetTemplates WHERE Id = {seeded.Opms.Id}"))
            .Should().ThrowAsync<SqliteException>();
        await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM IpmsTargetTemplates WHERE Id = {seeded.Ipms.Id}"))
            .Should().ThrowAsync<SqliteException>();

        (await context.OpmsTargetTemplateVersions.CountAsync()).Should().Be(1);
        (await context.IpmsTargetTemplateVersions.CountAsync()).Should().Be(1);
    }

    private static async Task<SeededTemplates> SeedAsync(ApplicationDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var opms = new OpmsTargetTemplate
        {
            TemplateCode = $"OP-{suffix}", TemplateName = "Preserved OPMS template",
            TargetName = "OPMS target", KpiDescription = "OPMS KPI", TargetUnitType = "NUMBER"
        };
        var ipms = new IpmsTargetTemplate
        {
            TemplateCode = $"IP-{suffix}", TemplateName = "Preserved IPMS template",
            TargetName = "IPMS target", KpiDescription = "IPMS KPI", TargetUnitType = "NUMBER"
        };
        opms.Versions.Add(new OpmsTargetTemplateVersion
        {
            Version = 1, SnapshotJson = "{\"version\":1}", CreatedBy = "template-governance-test"
        });
        ipms.Versions.Add(new IpmsTargetTemplateVersion
        {
            Version = 1, SnapshotJson = "{\"version\":1}", CreatedBy = "template-governance-test"
        });
        context.AddRange(opms, ipms);
        await context.SaveChangesAsync();
        return new SeededTemplates(opms, ipms);
    }

    private sealed record SeededTemplates(OpmsTargetTemplate Opms, IpmsTargetTemplate Ipms);
}

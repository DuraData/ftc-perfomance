using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class RolePermissionHistoryTests
{
    [Fact]
    public async Task Permission_replacement_closes_the_predecessor_and_preserves_grant_state_and_scope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var role = new ApplicationRole
        {
            Id = "role-history",
            Name = "ROLE_HISTORY",
            NormalizedName = "ROLE_HISTORY",
            RoleCode = "ROLE_HISTORY",
            IsActive = true
        };
        var permission = new Permission
        {
            Code = "OPMS_KPI.UPDATE",
            Module = "OPMS",
            Feature = "KPI",
            Action = "Update",
            IsActive = true
        };
        context.AddRange(role, permission);
        await context.SaveChangesAsync();

        var original = new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsAllowed = true,
            ScopeType = ScopeType.DepartmentScope,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        context.RolePermissions.Add(original);
        await context.SaveChangesAsync();
        var originalPublicId = original.PublicId;

        original.IsAllowed = false;
        var rewrite = () => context.SaveChangesAsync();
        await rewrite.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*create a successor version*");

        context.ChangeTracker.Clear();
        original = await context.RolePermissions.SingleAsync(item => item.PublicId == originalPublicId);
        var replacedAt = DateTime.UtcNow;
        original.IsActive = false;
        original.EffectiveTo = replacedAt;
        var successor = new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsAllowed = false,
            ScopeType = ScopeType.UnitScope,
            EffectiveFrom = replacedAt
        };
        context.RolePermissions.Add(successor);
        await context.SaveChangesAsync();

        var versions = await context.RolePermissions
            .Where(item => item.RoleId == role.Id && item.PermissionId == permission.Id)
            .OrderBy(item => item.EffectiveFrom)
            .ToArrayAsync();
        versions.Should().HaveCount(2);
        versions[0].PublicId.Should().Be(originalPublicId);
        versions[0].IsAllowed.Should().BeTrue();
        versions[0].ScopeType.Should().Be(ScopeType.DepartmentScope);
        versions[0].IsActive.Should().BeFalse();
        versions[0].EffectiveTo.Should().Be(replacedAt);
        versions[1].PublicId.Should().Be(successor.PublicId);
        versions[1].IsAllowed.Should().BeFalse();
        versions[1].ScopeType.Should().Be(ScopeType.UnitScope);
        versions[1].IsActive.Should().BeTrue();
        versions[1].EffectiveTo.Should().BeNull();

        context.RolePermissions.Remove(versions[0]);
        var deletion = () => context.SaveChangesAsync();
        await deletion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Sqlite_incremental_round_trip_preserves_existing_role_permission_and_installs_history_identity()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-role-permission-history-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var role = new ApplicationRole { Id = "upgrade-role", Name = "UPGRADE_ROLE", NormalizedName = "UPGRADE_ROLE", RoleCode = "UPGRADE_ROLE", IsActive = true };
            var permission = new Permission { Code = "UPGRADE.READ", Module = "Upgrade", Feature = "History", Action = "Read", IsActive = true };
            context.AddRange(role, permission);
            await context.SaveChangesAsync();
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                IsAllowed = false,
                ScopeType = ScopeType.DepartmentScope,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            var migrations = context.Database.GetMigrations().ToArray();
            var targetMigration = migrations.Single(item => item.EndsWith("V39AppendPreservedRolePermissions", StringComparison.Ordinal));
            foreach (var migration in migrations.TakeWhile(item => item != targetMigration).Append(targetMigration))
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            const string predecessor = "20261013161110_V39RestrictiveRemainingAggregateHistory";
            await context.GetService<IMigrator>().MigrateAsync(predecessor);
            await context.GetService<IMigrator>().MigrateAsync(targetMigration);
            context.ChangeTracker.Clear();

            var preserved = await context.RolePermissions.SingleAsync(item => item.RoleId == role.Id && item.PermissionId == permission.Id);
            preserved.Id.Should().BePositive();
            preserved.PublicId.Should().NotBeEmpty();
            preserved.IsAllowed.Should().BeFalse();
            preserved.ScopeType.Should().Be(ScopeType.DepartmentScope);
            await context.Database.OpenConnectionAsync();
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
}

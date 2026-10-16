using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class RemainingCascadeDeletionGovernanceTests
{
    private static readonly Relationship[] GovernedRelationships =
    [
        new(typeof(IdentityRoleClaim<string>), typeof(ApplicationRole), "RoleId", "AspNetRoleClaims", "AspNetRoles", "TRG_AspNetRoles_AspNetRoleClaims_RestrictDelete"),
        new(typeof(IdentityUserClaim<string>), typeof(ApplicationUser), "UserId", "AspNetUserClaims", "AspNetUsers", "TRG_AspNetUsers_AspNetUserClaims_RestrictDelete"),
        new(typeof(IdentityUserLogin<string>), typeof(ApplicationUser), "UserId", "AspNetUserLogins", "AspNetUsers", "TRG_AspNetUsers_AspNetUserLogins_RestrictDelete"),
        new(typeof(IdentityUserRole<string>), typeof(ApplicationRole), "RoleId", "AspNetUserRoles", "AspNetRoles", "TRG_AspNetRoles_AspNetUserRoles_RestrictDelete"),
        new(typeof(IdentityUserRole<string>), typeof(ApplicationUser), "UserId", "AspNetUserRoles", "AspNetUsers", "TRG_AspNetUsers_AspNetUserRoles_RestrictDelete"),
        new(typeof(IdentityUserToken<string>), typeof(ApplicationUser), "UserId", "AspNetUserTokens", "AspNetUsers", "TRG_AspNetUsers_AspNetUserTokens_RestrictDelete"),
        new(typeof(AuthenticationPolicy), typeof(AuthenticationConfiguration), nameof(AuthenticationPolicy.AuthenticationConfigurationId), "AuthenticationPolicies", "AuthenticationConfigurations", "TRG_AuthenticationConfigurations_AuthenticationPolicies_RestrictDelete"),
        new(typeof(LoginAuditLog), typeof(ApplicationUser), nameof(LoginAuditLog.UserId), "LoginAuditLogs", "AspNetUsers", "TRG_AspNetUsers_LoginAuditLogs_RestrictDelete"),
        new(typeof(OpmsImportRow), typeof(OpmsImportBatch), nameof(OpmsImportRow.OpmsImportBatchId), "OpmsImportRows", "OpmsImportBatches", "TRG_OpmsImportBatches_OpmsImportRows_RestrictDelete"),
        new(typeof(RefreshToken), typeof(ApplicationUser), nameof(RefreshToken.UserId), "RefreshTokens", "AspNetUsers", "TRG_AspNetUsers_RefreshTokens_RestrictDelete"),
        new(typeof(RolePermission), typeof(ApplicationRole), nameof(RolePermission.RoleId), "RolePermissions", "AspNetRoles", "TRG_AspNetRoles_RolePermissions_RestrictDelete"),
        new(typeof(RolePermission), typeof(Permission), nameof(RolePermission.PermissionId), "RolePermissions", "Permissions", "TRG_Permissions_RolePermissions_RestrictDelete"),
        new(typeof(StrategicObjective), typeof(StrategicGoal), nameof(StrategicObjective.StrategicGoalId), "StrategicObjectives", "StrategicGoals", "TRG_StrategicGoals_StrategicObjectives_RestrictDelete"),
        new(typeof(Unit), typeof(Department), nameof(Unit.DepartmentId), "Units", "Departments", "TRG_Departments_Units_RestrictDelete"),
        new(typeof(UserAssignment), typeof(ApplicationUser), nameof(UserAssignment.UserId), "UserAssignments", "AspNetUsers", "TRG_AspNetUsers_UserAssignments_RestrictDelete"),
        new(typeof(UserPermissionOverride), typeof(ApplicationUser), nameof(UserPermissionOverride.UserId), "UserPermissionOverrides", "AspNetUsers", "TRG_AspNetUsers_UserPermissionOverrides_RestrictDelete"),
        new(typeof(UserPermissionOverride), typeof(Permission), nameof(UserPermissionOverride.PermissionId), "UserPermissionOverrides", "Permissions", "TRG_Permissions_UserPermissionOverrides_RestrictDelete"),
        new(typeof(UserScope), typeof(ApplicationUser), nameof(UserScope.UserId), "UserScopes", "AspNetUsers", "TRG_AspNetUsers_UserScopes_RestrictDelete")
    ];

    [Fact]
    public void Compiled_model_has_no_database_destructive_delete_relationships()
    {
        using var context = IdpTestFixture.CreateRelationalContext();

        var destructiveRelationships = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => foreignKey.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.SetNull)
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.ClrType.Name}.{string.Join(',', foreignKey.Properties.Select(property => property.Name))}")
            .ToArray();

        destructiveRelationships.Should().BeEmpty("a direct parent delete must never erase or detach governed or identity history");
        GovernedRelationships.Should().HaveCount(18);
        foreach (var relationship in GovernedRelationships)
        {
            var dependent = context.Model.FindEntityType(relationship.DependentType);
            dependent.Should().NotBeNull();
            dependent!.GetForeignKeys().Single(candidate =>
                    candidate.PrincipalEntityType.ClrType == relationship.PrincipalType
                    && candidate.Properties.Count == 1
                    && candidate.Properties[0].Name == relationship.ForeignKey)
                .DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        }
    }

    [Fact]
    public async Task Clean_sqlite_schema_enforces_all_remaining_relationships_as_restrictive()
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

                reader.GetString(6).Should().Be("RESTRICT");
                found = true;
            }

            found.Should().BeTrue($"the database must contain {relationship.DependentTable}.{relationship.ForeignKey}");
        }
    }

    [Fact]
    public async Task Sqlite_incremental_upgrade_installs_every_remaining_delete_guard_and_preserves_rows()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-final-cascade-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var seeded = await SeedRepresentativeRelationshipsAsync(context);
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            const string latestMigration = "20261013161110_V39RestrictiveRemainingAggregateHistory";
            foreach (var migration in context.Database.GetMigrations().TakeWhile(item => item != latestMigration))
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            await context.GetService<IMigrator>().MigrateAsync(latestMigration);

            var triggers = new Dictionary<string, string>(StringComparer.Ordinal);
            await context.Database.OpenConnectionAsync();
            await using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'trigger' AND name LIKE 'TRG_%_RestrictDelete' ORDER BY name;";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    triggers[reader.GetString(0)] = reader.GetString(1);
            }

            foreach (var relationship in GovernedRelationships)
            {
                triggers.Should().ContainKey(relationship.TriggerName);
                var sql = triggers[relationship.TriggerName];
                sql.Should().Contain($"BEFORE DELETE ON \"{relationship.PrincipalTable}\"");
                sql.Should().Contain($"FROM \"{relationship.DependentTable}\"");
                sql.Should().Contain($"\"{relationship.ForeignKey}\" = OLD.\"Id\"");
            }

            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM AspNetUsers WHERE Id = {seeded.UserId}"))
                .Should().ThrowAsync<SqliteException>();
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM Departments WHERE Id = {seeded.DepartmentId}"))
                .Should().ThrowAsync<SqliteException>();
            await FluentActions.Invoking(() => context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM StrategicGoals WHERE Id = {seeded.StrategicGoalId}"))
                .Should().ThrowAsync<SqliteException>();

            (await context.Units.IgnoreQueryFilters().CountAsync()).Should().Be(1);
            (await context.StrategicObjectives.CountAsync()).Should().Be(1);
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

    private static async Task<(string UserId, int DepartmentId, int StrategicGoalId)> SeedRepresentativeRelationshipsAsync(
        ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser($"cascade-{Guid.NewGuid():N}");
        var department = new Department { Code = $"DEP-{Guid.NewGuid():N}", Name = "Preserved department" };
        var unit = new Unit { Department = department, Code = $"UNIT-{Guid.NewGuid():N}", Name = "Preserved unit" };
        var goal = new StrategicGoal { Code = $"GOAL-{Guid.NewGuid():N}", Name = "Preserved goal", IsActive = true };
        var objective = new StrategicObjective { StrategicGoal = goal, Code = $"OBJ-{Guid.NewGuid():N}", Name = "Preserved objective", IsActive = true };
        var claim = new IdentityUserClaim<string> { UserId = user.Id, ClaimType = "history", ClaimValue = "preserve" };
        context.AddRange(user, department, unit, goal, objective, claim);
        await context.SaveChangesAsync();
        return (user.Id, department.Id, goal.Id);
    }

    private sealed record Relationship(
        Type DependentType,
        Type PrincipalType,
        string ForeignKey,
        string DependentTable,
        string PrincipalTable,
        string TriggerName);
}

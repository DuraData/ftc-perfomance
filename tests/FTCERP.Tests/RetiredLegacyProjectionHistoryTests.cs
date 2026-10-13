namespace FTCERP.Tests;

public sealed class RetiredLegacyProjectionHistoryTests
{
    [Fact]
    public void Retired_user_override_has_no_production_mutation_or_client_contract()
    {
        var root = FindRepositoryRoot();
        var productionRoots = new[] { "API", "Application", "Infrastructure" };
        var offenders = productionRoots
            .SelectMany(directory => Directory.EnumerateFiles(Path.Combine(root.FullName, directory), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => File.ReadLines(file).Select((line, index) => (file, line, index)))
            .Where(item => item.line.Contains("new UserPermissionOverride", StringComparison.Ordinal)
                || item.line.Contains("UserPermissionOverrides.Add", StringComparison.Ordinal)
                || item.line.Contains("UserPermissionOverrides.Remove", StringComparison.Ordinal)
                || item.line.Contains("UserPermissionOverrides.Update", StringComparison.Ordinal))
            .Select(item => $"{Path.GetRelativePath(root.FullName, item.file)}:{item.index + 1}: {item.line.Trim()}")
            .ToArray();
        offenders.Should().BeEmpty();

        var clientApi = File.ReadAllText(Path.Combine(root.FullName, "ClientApp", "src", "api", "api.ts"));
        var clientTypes = File.ReadAllText(Path.Combine(root.FullName, "ClientApp", "src", "types", "index.ts"));
        clientApi.Should().NotContain("permission-overrides").And.NotContain("getUserPermissions").And.NotContain("setUserPermissionOverrides");
        clientTypes.Should().NotContain("UserPermissionOverride").And.NotContain("UserPermissions");
    }

    [Fact]
    public async Task Retired_user_permission_overrides_reject_create_rewrite_and_delete()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("legacy-override-user");
        var permission = new Permission
        {
            Code = "OPMS_KPI.READ",
            Module = "Resource",
            Feature = "OPMS_KPI",
            Action = "Read",
            Kind = SecurityPermissionKind.Resource,
            ResourceCode = "OPMS_KPI",
            Operation = SecurityOperation.Read
        };
        context.AddRange(user, permission);
        await context.SaveChangesAsync();

        context.UserPermissionOverrides.Add(new UserPermissionOverride
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsAllowed = false,
            Reason = "Attempt a new direct restriction"
        });
        var create = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        create.Message.Should().Contain("retired and immutable");

        context.ChangeTracker.Clear();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserPermissionOverrides" ("UserId", "PermissionId", "IsAllowed", "Reason")
            VALUES ({user.Id}, {permission.Id}, {false}, {"Preserved pre-retirement restriction"})
            """);

        var legacy = await context.UserPermissionOverrides.SingleAsync();
        legacy.Reason = "Rewrite preserved evidence";
        var rewrite = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        rewrite.Message.Should().Contain("retired and immutable");

        context.ChangeTracker.Clear();
        legacy = await context.UserPermissionOverrides.SingleAsync();
        context.UserPermissionOverrides.Remove(legacy);
        var delete = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        delete.Message.Should().Contain("retired and immutable");
    }

    [Theory]
    [InlineData(nameof(Period))]
    [InlineData(nameof(StrategicGoal))]
    [InlineData(nameof(StrategicObjective))]
    [InlineData(nameof(BudgetSource))]
    [InlineData(nameof(BudgetType))]
    [InlineData(nameof(UnitOfMeasure))]
    public async Task Retired_legacy_lookup_rows_reject_rewrite_and_delete(string entityName)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var entity = await AddLegacyLookupAsync(context, entityName);
        var entityType = entity.GetType();
        var id = (int)context.Entry(entity).Property("Id").CurrentValue!;

        context.Entry(entity).Property("Name").CurrentValue = "Rewritten legacy value";
        var rewrite = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        rewrite.Message.Should().Contain("immutable");

        context.ChangeTracker.Clear();
        var stored = await context.FindAsync(entityType, id);
        stored.Should().NotBeNull();
        context.Remove(stored!);
        var delete = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        delete.Message.Should().Contain("immutable");
    }

    private static async Task<object> AddLegacyLookupAsync(ApplicationDbContext context, string entityName)
    {
        object entity;
        switch (entityName)
        {
            case nameof(Period):
                entity = new Period
                {
                    Code = "LEGACY-FY",
                    Name = "Legacy financial period",
                    FiscalYear = "2025/2026",
                    StartDate = new DateTime(2025, 7, 1),
                    EndDate = new DateTime(2026, 6, 30),
                    IsActive = true
                };
                break;
            case nameof(StrategicGoal):
                entity = new StrategicGoal { Code = "LEGACY-GOAL", Name = "Legacy goal", Description = "Preserved compatibility reference", IsActive = true };
                break;
            case nameof(StrategicObjective):
            {
                var goal = new StrategicGoal { Code = "LEGACY-PARENT", Name = "Legacy parent", Description = "Preserved compatibility reference", IsActive = true };
                entity = new StrategicObjective { Code = "LEGACY-OBJECTIVE", Name = "Legacy objective", StrategicGoal = goal, IsActive = true };
                break;
            }
            case nameof(BudgetSource):
                entity = new BudgetSource { Code = "LEGACY-SOURCE", Name = "Legacy budget source", IsActive = true };
                break;
            case nameof(BudgetType):
                entity = new BudgetType { Code = "LEGACY-TYPE", Name = "Legacy budget type", IsActive = true };
                break;
            case nameof(UnitOfMeasure):
                entity = new UnitOfMeasure { Code = "LEGACY-UOM", Name = "Legacy unit", Symbol = "LU", IsActive = true };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entityName), entityName, "Unknown retired legacy lookup.");
        }

        context.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "FTCERP.Host.csproj"))) current = current.Parent;
        return current ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class SqlServerTenantMasterIntegrationTests
{
    private const string EnvironmentVariable = "OPMS_SQLSERVER_TEST_CONNECTION";
    private const string RequiredServer = @"localhost\SQLEXPRESS";
    private const string RequiredDatabase = "OPMS_IntegrationTests";

    [SqlServerFact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Migrations_constraints_tenant_filters_and_rowversion_execute_on_sql_server()
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvironmentVariable);
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.DataSource, RequiredServer, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(builder.InitialCatalog, RequiredDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"SQL Server integration tests are restricted to {RequiredServer}/{RequiredDatabase}.");

        await EnsureDatabaseAndMigrations(builder);
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var run = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        try
        {
            var system = new FixedTenantContext(null, true, "sql-integration");
            var systemOptions = Options(connection);
            await using (var setup = new ApplicationDbContext(systemOptions, system))
            {
                await setup.Database.UseTransactionAsync(transaction);
                var municipalityA = new Municipality { Code = $"A{run}", Name = $"Tenant A {run}" };
                var municipalityB = new Municipality { Code = $"B{run}", Name = $"Tenant B {run}" };
                setup.Municipalities.AddRange(municipalityA, municipalityB);
                await setup.SaveChangesAsync();
                setup.Departments.AddRange(
                    new Department { MunicipalityId = municipalityA.Id, Code = $"DA{run}", Name = "Tenant A Department" },
                    new Department { MunicipalityId = municipalityB.Id, Code = $"DB{run}", Name = "Tenant B Department" });
                setup.Users.Add(new ApplicationUser
                {
                    Id = "tenant-a-user",
                    MunicipalityId = municipalityA.Id,
                    UserName = $"tenant-a-{run}@example.test",
                    NormalizedUserName = $"TENANT-A-{run}@EXAMPLE.TEST",
                    Email = $"tenant-a-{run}@example.test",
                    NormalizedEmail = $"TENANT-A-{run}@EXAMPLE.TEST",
                    FirstName = "Tenant",
                    LastName = "Administrator",
                    IsActive = true
                });
                setup.MunicipalEmployees.AddRange(
                    new MunicipalEmployee { MunicipalityId = municipalityA.Id, EmployeeNumber = $"EA{run}", FirstName = "Ada", LastName = "A", EffectiveFrom = DateTime.UtcNow.AddYears(-1) },
                    new MunicipalEmployee { MunicipalityId = municipalityB.Id, EmployeeNumber = $"EB{run}", FirstName = "Ben", LastName = "B", EffectiveFrom = DateTime.UtcNow.AddYears(-1) });
                await setup.SaveChangesAsync();

                var tenantA = new FixedTenantContext(municipalityA.Id, false, "tenant-a-user");
                var tenantB = new FixedTenantContext(municipalityB.Id, false, "tenant-b-user");
                await AssertTenantIsolationAndDatabaseConstraints(connection, transaction, tenantA, tenantB, run);
            }
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Dynamic_security_changes_are_effective_scoped_audited_and_concurrency_protected_on_sql_server()
    {
        var builder = RequiredConnection();
        await EnsureDatabaseAndMigrations(builder);
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var run = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var actorId = $"sql-security-{run}";

        try
        {
            long municipalityAId;
            int departmentAId;
            int departmentBId;
            ApplicationUser actor;
            ApplicationRole allowRole;
            ApplicationRole denyRole;
            Permission createPermission;
            Permission readPermission;
            Permission updatePermission;
            Permission deletePermission;
            Permission memberPermission;
            Permission actionPermission;
            RolePermission deleteGrant;

            await using (var setup = new ApplicationDbContext(Options(connection), new FixedTenantContext(null, true, actorId)))
            {
                await setup.Database.UseTransactionAsync(transaction);
                var municipalityA = new Municipality { Code = $"SA{run}", Name = $"Security Tenant A {run}" };
                var municipalityB = new Municipality { Code = $"SB{run}", Name = $"Security Tenant B {run}" };
                setup.Municipalities.AddRange(municipalityA, municipalityB);
                await setup.SaveChangesAsync();
                municipalityAId = municipalityA.Id;

                var departmentA = new Department { MunicipalityId = municipalityA.Id, Code = $"SDA{run}", Name = "Security Department A" };
                var departmentB = new Department { MunicipalityId = municipalityB.Id, Code = $"SDB{run}", Name = "Security Department B" };
                actor = new ApplicationUser
                {
                    Id = actorId,
                    MunicipalityId = municipalityA.Id,
                    UserName = $"{actorId}@example.test",
                    NormalizedUserName = $"{actorId.ToUpperInvariant()}@EXAMPLE.TEST",
                    Email = $"{actorId}@example.test",
                    NormalizedEmail = $"{actorId.ToUpperInvariant()}@EXAMPLE.TEST",
                    FirstName = "SQL",
                    LastName = "Security",
                    IsActive = true
                };
                allowRole = SecurityRole($"sql-allow-{run}", $"SQL_ALLOW_{run}", municipalityA.Id);
                denyRole = SecurityRole($"sql-deny-{run}", $"SQL_DENY_{run}", municipalityA.Id);
                var prefix = $"SQL_{run}";
                createPermission = SecurityPermission($"{prefix}.CREATE", SecurityPermissionKind.Resource, SecurityOperation.Create, resourceCode: prefix);
                readPermission = SecurityPermission($"{prefix}.READ", SecurityPermissionKind.Resource, SecurityOperation.Read, resourceCode: prefix);
                updatePermission = SecurityPermission($"{prefix}.UPDATE", SecurityPermissionKind.Resource, SecurityOperation.Update, resourceCode: prefix);
                deletePermission = SecurityPermission($"{prefix}.DELETE", SecurityPermissionKind.Resource, SecurityOperation.Delete, resourceCode: prefix);
                memberPermission = SecurityPermission($"{prefix}.Sensitive.READ", SecurityPermissionKind.Member, SecurityOperation.Read, resourceCode: prefix, memberCode: "Sensitive");
                actionPermission = SecurityPermission($"{prefix}.APPROVE", SecurityPermissionKind.Action, SecurityOperation.Execute, resourceCode: prefix, actionCode: $"{prefix}.APPROVE");
                var navigationPermission = SecurityPermission($"NAV.{prefix}", SecurityPermissionKind.Navigation, navigationCode: $"NAV.{prefix}");
                setup.AddRange(departmentA, departmentB, actor, allowRole, denyRole, createPermission, readPermission,
                    updatePermission, deletePermission, memberPermission, actionPermission, navigationPermission);
                await setup.SaveChangesAsync();
                departmentAId = departmentA.Id;
                departmentBId = departmentB.Id;

                var now = DateTime.UtcNow;
                var assignment = new SecurityUserRoleAssignment
                {
                    UserId = actor.Id, RoleId = allowRole.Id, MunicipalityId = municipalityA.Id,
                    DepartmentId = departmentA.Id, IsActive = true, EffectiveFrom = now.AddDays(-1),
                    AssignedAt = now, AssignedBy = actor.Id
                };
                var grants = new[]
                {
                    SecurityGrant(allowRole, createPermission),
                    SecurityGrant(allowRole, readPermission, ScopeType.DepartmentScope),
                    SecurityGrant(allowRole, updatePermission),
                    SecurityGrant(allowRole, deletePermission),
                    SecurityGrant(allowRole, memberPermission, ScopeType.DepartmentScope),
                    SecurityGrant(allowRole, actionPermission),
                    SecurityGrant(allowRole, navigationPermission)
                };
                deleteGrant = grants[3];
                setup.SecurityUserRoleAssignments.Add(assignment);
                setup.RolePermissions.AddRange(grants);
                setup.UserScopes.Add(new UserScope
                {
                    UserId = actor.Id, MunicipalityId = municipalityA.Id, ScopeType = ScopeType.DepartmentScope,
                    DepartmentId = departmentA.Id, IsActive = true, EffectiveFrom = now.AddDays(-1)
                });
                setup.SecurityNavigationItems.AddRange(
                    new SecurityNavigationItem
                    {
                        Code = $"NAV.{prefix}.ITEM", Name = "SQL security page", Route = $"/sql-{run.ToLowerInvariant()}",
                        RequiredPermissionCode = navigationPermission.Code, DisplayOrder = 1, IsActive = true
                    },
                    new SecurityNavigationItem
                    {
                        Code = $"NAV.{prefix}.HIDDEN", Name = "Hidden SQL security page", Route = $"/sql-{run.ToLowerInvariant()}-hidden",
                        RequiredPermissionCode = $"NAV.{prefix}.UNGRANTED", DisplayOrder = 2, IsActive = true
                    });
                await setup.SaveChangesAsync();
            }

            var tenant = new FixedTenantContext(municipalityAId, false, actorId);
            await using var accessContext = new ApplicationDbContext(Options(connection), tenant);
            await accessContext.Database.UseTransactionAsync(transaction);
            var service = SecurityService(accessContext, actor, tenant);
            var departmentAScope = new AccessScopeContext(DepartmentId: departmentAId);
            var departmentBScope = new AccessScopeContext(DepartmentId: departmentBId);

            Assert.True((await service.CheckPermissionAsync(actor, createPermission.Code)).Allowed);
            Assert.True((await service.CheckPermissionAsync(actor, readPermission.Code, departmentAScope)).Allowed);
            Assert.False((await service.CheckPermissionAsync(actor, readPermission.Code, departmentBScope)).Allowed);
            Assert.True((await service.CheckPermissionAsync(actor, updatePermission.Code)).Allowed);
            Assert.True((await service.CheckPermissionAsync(actor, deletePermission.Code)).Allowed);
            Assert.True((await service.CheckPermissionAsync(actor, memberPermission.Code, departmentAScope)).Allowed);
            Assert.True((await service.CheckPermissionAsync(actor, actionPermission.Code)).Allowed);
            var menu = await service.GetAuthorizedNavigationAsync(actor);
            Assert.Contains(menu, item => item.Label == "SQL security page");
            Assert.DoesNotContain(menu, item => item.Label == "Hidden SQL security page");
            Assert.False((await service.CheckPermissionAsync(actor, $"NAV.SQL_{run}.UNGRANTED")).Allowed);

            accessContext.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
            {
                UserId = actor.Id, RoleId = denyRole.Id, MunicipalityId = municipalityAId,
                IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), AssignedAt = DateTime.UtcNow, AssignedBy = actor.Id
            });
            accessContext.RolePermissions.Add(SecurityGrant(denyRole, updatePermission, allowed: false));
            await accessContext.SaveChangesAsync();
            Assert.False((await service.CheckPermissionAsync(actor, updatePermission.Code)).Allowed);

            var permissionChangeAt = DateTime.UtcNow;
            var trackedDeleteGrant = await accessContext.RolePermissions.SingleAsync(item => item.Id == deleteGrant.Id);
            trackedDeleteGrant.IsActive = false;
            trackedDeleteGrant.EffectiveTo = permissionChangeAt;
            var trackedRole = await accessContext.Roles.SingleAsync(item => item.Id == allowRole.Id);
            trackedRole.UpdatedAt = permissionChangeAt;
            accessContext.AuditTrails.Add(new AuditTrail
            {
                MunicipalityId = municipalityAId,
                EntityName = "SecurityRolePermissions",
                EntityId = trackedRole.PublicId.ToString(),
                Action = "ReplacePermissions",
                OldValue = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    new { PermissionCode = deletePermission.Code, State = "ALLOW", ScopeType = (string?)null }
                }),
                NewValue = "[]",
                ChangedBy = actor.Id,
                ChangedAt = permissionChangeAt,
                Reason = "SQL Server permission revocation acceptance"
            });
            await accessContext.SaveChangesAsync();
            Assert.False((await service.CheckPermissionAsync(actor, deletePermission.Code)).Allowed);
            Assert.Contains(await accessContext.AuditTrails.AsNoTracking().ToArrayAsync(), item =>
                item.EntityName == "SecurityRolePermissions" && item.Reason == "SQL Server permission revocation acceptance");

            await AssertRolePermissionConcurrency(connection, transaction, tenant, allowRole.Id, createPermission.Id);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Required_foreign_keys_and_logical_uniqueness_are_trusted_on_sql_server()
    {
        var builder = RequiredConnection();
        await EnsureDatabaseAndMigrations(builder);
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var relationships = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT OBJECT_NAME(parent_object_id), OBJECT_NAME(referenced_object_id), is_disabled, is_not_trusted
                FROM sys.foreign_keys;
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Assert.False(reader.GetBoolean(2), $"Disabled foreign key on {reader.GetString(0)} -> {reader.GetString(1)}.");
                Assert.False(reader.GetBoolean(3), $"Untrusted foreign key on {reader.GetString(0)} -> {reader.GetString(1)}.");
                relationships.Add($"{reader.GetString(0)}->{reader.GetString(1)}");
            }
        }

        foreach (var expected in new[]
        {
            "OpmsSubmissions->OpmsTargets", "OpmsSubmissions->ReportingPeriods",
            "IpmsSubmissions->IpmsTargets", "IpmsSubmissions->ReportingPeriods",
            "SubmissionWorkflowActions->SubmissionWorkflowInstances", "PerformanceRfis->SubmissionWorkflowInstances",
            "PerformanceRfiEvidenceLinks->PerformanceRfis", "PerformanceRfiEvidenceLinks->PoeFiles",
            "PoeFiles->EvidenceBlobs", "PoeFiles->OpmsSubmissions", "PoeFiles->IpmsSubmissions",
            "AuditTrails->AspNetUsers", "AuditTrails->Municipalities",
            "IdpPlanVersions->IdpPlans", "TechnicalIndicatorDescriptions->OpmsTargets",
            "TidSourceDocuments->TechnicalIndicatorDescriptions", "TidSourceDocuments->EvidenceBlobs",
            "C88IndicatorReports->C88Indicators", "C88DataElementValues->C88IndicatorReports",
            "RolePermissions->AspNetRoles", "RolePermissions->Permissions",
            "SecurityUserRoleAssignments->AspNetUsers", "SecurityUserRoleAssignments->AspNetRoles",
            "UserScopes->AspNetUsers"
        }) Assert.Contains(expected, relationships);

        var uniqueIndexes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT OBJECT_NAME(object_id), name, filter_definition
                FROM sys.indexes
                WHERE is_unique = 1 AND name IS NOT NULL;
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                uniqueIndexes[$"{reader.GetString(0)}.{reader.GetString(1)}"] = reader.IsDBNull(2) ? null : reader.GetString(2);
        }

        Assert.Equal("([ReportingPeriodId] IS NOT NULL)", uniqueIndexes["OpmsSubmissions.IX_OpmsSubmissions_OpmsTargetId_ReportingPeriodId"]);
        Assert.Equal("([ReportingPeriodId] IS NOT NULL)", uniqueIndexes["IpmsSubmissions.IX_IpmsSubmissions_IpmsTargetId_ReportingPeriodId"]);
        Assert.Equal("([OpmsTargetId] IS NOT NULL)", uniqueIndexes["PerformancePeriodTargets.IX_PerformancePeriodTargets_OpmsTargetId_ReportingPeriodId"]);
        Assert.Equal("([IpmsTargetId] IS NOT NULL)", uniqueIndexes["PerformancePeriodTargets.IX_PerformancePeriodTargets_IpmsTargetId_ReportingPeriodId"]);
    }

    private static async Task AssertTenantIsolationAndDatabaseConstraints(
        SqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        FixedTenantContext tenantA,
        FixedTenantContext tenantB,
        string run)
    {
        await using (var contextA = new ApplicationDbContext(Options(connection), tenantA))
        await using (var contextB = new ApplicationDbContext(Options(connection), tenantB))
        {
            await contextA.Database.UseTransactionAsync(transaction);
            await contextB.Database.UseTransactionAsync(transaction);
            var departmentsA = await contextA.Departments.AsNoTracking().Where(x => x.Code.EndsWith(run)).ToArrayAsync();
            var departmentsB = await contextB.Departments.AsNoTracking().Where(x => x.Code.EndsWith(run)).ToArrayAsync();
            Assert.Single(departmentsA);
            Assert.Single(departmentsB);
            Assert.NotEqual(departmentsA[0].MunicipalityId, departmentsB[0].MunicipalityId);
            Assert.Empty(await contextA.MunicipalEmployees.AsNoTracking().Where(x => x.EmployeeNumber == $"EB{run}").ToArrayAsync());

            contextA.MunicipalEmployees.Add(new MunicipalEmployee
            {
                MunicipalityId = tenantA.MunicipalityId!.Value,
                EmployeeNumber = $"EA{run}",
                FirstName = "Duplicate",
                LastName = "Employee",
                EffectiveFrom = DateTime.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => contextA.SaveChangesAsync());
            contextA.ChangeTracker.Clear();
        }

        await using var firstWriter = new ApplicationDbContext(Options(connection), tenantA);
        await using var staleWriter = new ApplicationDbContext(Options(connection), tenantA);
        await firstWriter.Database.UseTransactionAsync(transaction);
        await staleWriter.Database.UseTransactionAsync(transaction);
        var first = await firstWriter.Departments.SingleAsync(x => x.Code == $"DA{run}");
        var stale = await staleWriter.Departments.SingleAsync(x => x.Code == $"DA{run}");
        Assert.NotEmpty(first.RowVersion);
        var firstBefore = DepartmentSnapshot(first);
        first.Name = "First committed name";
        firstWriter.AuditTrails.Add(DepartmentUpdateAudit(first, firstBefore, "tenant-a-user"));
        await firstWriter.SaveChangesAsync();
        var staleBefore = DepartmentSnapshot(stale);
        stale.Name = "Stale conflicting name";
        staleWriter.AuditTrails.Add(DepartmentUpdateAudit(stale, staleBefore, "tenant-a-user"));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleWriter.SaveChangesAsync());
    }

    private static AuditTrail DepartmentUpdateAudit(Department department, object before, string actor) => new()
    {
        MunicipalityId = department.MunicipalityId,
        EntityName = nameof(Department),
        EntityId = department.PublicId.ToString(),
        Action = "Update",
        OldValue = System.Text.Json.JsonSerializer.Serialize(before),
        NewValue = System.Text.Json.JsonSerializer.Serialize(DepartmentSnapshot(department)),
        ChangedBy = actor,
        ChangedAt = DateTime.UtcNow,
        Reason = "SQL Server RowVersion concurrency acceptance"
    };

    private static object DepartmentSnapshot(Department department) => new
    {
        department.Code,
        department.Name,
        department.Description,
        department.IsActive,
        department.EffectiveFrom,
        department.EffectiveTo
    };

    private static SqlConnectionStringBuilder RequiredConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvironmentVariable);
        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.DataSource, RequiredServer, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(builder.InitialCatalog, RequiredDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"SQL Server integration tests are restricted to {RequiredServer}/{RequiredDatabase}.");
        return builder;
    }

    private static ApplicationRole SecurityRole(string id, string code, long municipalityId) => new()
    {
        Id = id,
        Name = code,
        NormalizedName = code.ToUpperInvariant(),
        RoleCode = code,
        MunicipalityId = municipalityId,
        IsActive = true,
        EffectiveFrom = DateTime.UtcNow.AddDays(-1)
    };

    private static Permission SecurityPermission(string code, SecurityPermissionKind kind,
        SecurityOperation? operation = null, string? resourceCode = null, string? memberCode = null,
        string? navigationCode = null, string? actionCode = null) => new()
    {
        Code = code,
        Module = "SQL Server Acceptance",
        Feature = resourceCode ?? code,
        Action = operation?.ToString() ?? "View",
        Kind = kind,
        ResourceCode = resourceCode,
        Operation = operation,
        MemberCode = memberCode,
        NavigationCode = navigationCode,
        ActionCode = actionCode,
        IsActive = true
    };

    private static RolePermission SecurityGrant(ApplicationRole role, Permission permission,
        ScopeType? scope = null, bool allowed = true) => new()
    {
        RoleId = role.Id,
        PermissionId = permission.Id,
        IsAllowed = allowed,
        ScopeType = scope,
        IsActive = true,
        EffectiveFrom = DateTime.UtcNow.AddDays(-1)
    };

    private static AccessControlService SecurityService(ApplicationDbContext context, ApplicationUser user, ITenantContext tenant)
    {
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null!, null!, null!, null!);
        return new AccessControlService(context, IdpTestFixture.CreateUserManagerMock(user).Object, roleManager.Object, tenant);
    }

    private static async Task AssertRolePermissionConcurrency(SqlConnection connection,
        System.Data.Common.DbTransaction transaction, ITenantContext tenant, string roleId, int permissionId)
    {
        await using var firstWriter = new ApplicationDbContext(Options(connection), tenant);
        await using var staleWriter = new ApplicationDbContext(Options(connection), tenant);
        await firstWriter.Database.UseTransactionAsync(transaction);
        await staleWriter.Database.UseTransactionAsync(transaction);
        var first = await firstWriter.RolePermissions.SingleAsync(item =>
            item.RoleId == roleId && item.PermissionId == permissionId && item.IsActive);
        var stale = await staleWriter.RolePermissions.SingleAsync(item =>
            item.RoleId == roleId && item.PermissionId == permissionId && item.IsActive);
        Assert.NotEmpty(first.RowVersion);
        var closedAt = DateTime.UtcNow;
        first.IsActive = false;
        first.EffectiveTo = closedAt;
        await firstWriter.SaveChangesAsync();
        stale.IsActive = false;
        stale.EffectiveTo = closedAt.AddSeconds(1);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleWriter.SaveChangesAsync());
    }

    private static DbContextOptions<ApplicationDbContext> Options(SqlConnection connection) =>
        new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;

    private static async Task EnsureDatabaseAndMigrations(SqlConnectionStringBuilder target)
    {
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(N'{RequiredDatabase}') IS NULL CREATE DATABASE [{RequiredDatabase}];";
            await command.ExecuteNonQueryAsync();
        }

        await using var migrationContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(target.ConnectionString).Options,
            new FixedTenantContext(null, true, "migration"));
        await migrationContext.Database.MigrateAsync();
    }

    private sealed class FixedTenantContext(long? municipalityId, bool isSystem, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => userId;
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPMS_SQLSERVER_TEST_CONNECTION")))
            Skip = "Set OPMS_SQLSERVER_TEST_CONNECTION to run against localhost\\SQLEXPRESS/OPMS_IntegrationTests.";
    }
}

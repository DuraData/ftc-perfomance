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
        first.Name = "First committed name";
        await firstWriter.SaveChangesAsync();
        stale.Name = "Stale conflicting name";
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

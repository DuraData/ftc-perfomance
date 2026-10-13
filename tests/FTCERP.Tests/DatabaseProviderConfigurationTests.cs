using Microsoft.Extensions.Configuration;

namespace FTCERP.Tests;

public class DatabaseProviderConfigurationTests
{
    [Theory]
    [InlineData("SqlServer", "Server=(local);Database=OPMS;Trusted_Connection=True", "Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData("Sqlite", "Data Source=:memory:", "Microsoft.EntityFrameworkCore.Sqlite")]
    public void UseConfiguredDatabase_SelectsProviderWithoutDomainCodeChanges(
        string provider,
        string connectionString,
        string expectedProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseConfiguredDatabase(configuration);

        using var context = new ApplicationDbContext(optionsBuilder.Options);
        context.Database.ProviderName.Should().Be(expectedProvider);
    }

    [Theory]
    [InlineData("SqlServer", "SqlServerConnection", "Server=(local);Database=OPMS;Trusted_Connection=True", "Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData("Sqlite", "SqliteConnection", "Data Source=:memory:", "Microsoft.EntityFrameworkCore.Sqlite")]
    public void UseConfiguredDatabase_PrefersTheProviderSpecificConnectionString(
        string provider,
        string providerSpecificName,
        string providerSpecificConnectionString,
        string expectedProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["ConnectionStrings:DefaultConnection"] = provider == "Sqlite"
                    ? "Server=(local);Database=WrongForSqlite"
                    : "Data Source=wrong-for-sql-server.db",
                [$"ConnectionStrings:{providerSpecificName}"] = providerSpecificConnectionString
            })
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseConfiguredDatabase(configuration);

        using var context = new ApplicationDbContext(optionsBuilder.Options);
        context.Database.ProviderName.Should().Be(expectedProvider);
        context.Database.GetDbConnection().ConnectionString
            .Should().Contain(provider == "Sqlite" ? ":memory:" : "OPMS")
            .And.NotContain("wrong");
    }

    [Fact]
    public void UseConfiguredDatabase_AllowsAValidExplicitSqliteDefaultToOverrideTheDevelopmentFallback()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=isolated-test.db",
                ["ConnectionStrings:SqliteConnection"] = "Data Source=App_Data/opms-development.db"
            })
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseConfiguredDatabase(configuration);

        using var context = new ApplicationDbContext(optionsBuilder.Options);
        context.Database.GetDbConnection().ConnectionString.Should().Contain("isolated-test.db");
    }

    [Fact]
    public void UseConfiguredDatabase_RejectsUnknownProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Other",
                ["ConnectionStrings:DefaultConnection"] = "unused"
            })
            .Build();

        FluentActions.Invoking(() => new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseConfiguredDatabase(configuration))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported database provider*");
    }

    [Fact]
    public async Task SqliteConfiguration_CreatesTheSharedRelationalModel()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseConfiguredDatabase(configuration);

        await using var context = new ApplicationDbContext(optionsBuilder.Options);
        await context.Database.OpenConnectionAsync();
        (await context.Database.EnsureCreatedAsync()).Should().BeTrue();

        var tables = await context.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('IdpPlans', 'SecurityResources', 'OpmsSubmissions')")
            .ToListAsync();
        tables.Should().BeEquivalentTo("IdpPlans", "SecurityResources", "OpmsSubmissions");
    }

    [Fact]
    public void UseConfiguredDatabase_RequiresConnectionStringForSqlite()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite"
            })
            .Build();

        FluentActions.Invoking(() => new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseConfiguredDatabase(configuration))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*");
    }

    [Fact]
    public void UseConfiguredDatabase_RequiresConnectionStringForSqlServer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SqlServer"
            })
            .Build();

        FluentActions.Invoking(() => new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseConfiguredDatabase(configuration))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*SqlServerConnection*DefaultConnection*");
    }
}

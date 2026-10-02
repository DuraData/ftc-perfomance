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
}

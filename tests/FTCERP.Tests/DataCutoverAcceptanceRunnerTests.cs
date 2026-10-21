using FluentAssertions;
using FTCERP.DataCutoverAcceptance;

namespace FTCERP.Tests;

public sealed class DataCutoverAcceptanceRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"opms-data-cutover-acceptance-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Runner_rehearses_every_historical_cutover_path_and_removes_the_isolated_database()
    {
        Directory.CreateDirectory(_root);
        var database = Path.Combine(_root, "opms-data-cutover-acceptance.db");
        var evidencePath = Path.Combine(_root, "evidence.json");

        var evidence = await DataCutoverAcceptanceRunner.RunAsync(new(
            "sqlite", $"Data Source={database}", ConfirmLocalTestDatabase: true,
            RecreateDatabase: true, EvidencePath: evidencePath));

        evidence.Succeeded.Should().BeTrue();
        evidence.Provider.Should().Be("SQLite");
        evidence.SchemaInitialization.Should().Be("Current EF Core model");
        evidence.MigrationsApplied.Should().BeFalse();
        evidence.Scenarios.Should().HaveCount(4).And.OnlyContain(scenario => scenario.Passed);
        evidence.PersistedEvidence.Should().Contain(new Dictionary<string, int>
        {
            ["tenantOwnershipReconciliations"] = 1,
            ["normalizedPeriodTargets"] = 2,
            ["submissionPeriodReconciliations"] = 1,
            ["ratingReconciliations"] = 1,
            ["canonicalStageRatings"] = 1,
            ["auditTrails"] = 5
        });
        File.Exists(database).Should().BeFalse();
        File.Exists(evidencePath).Should().BeTrue();
        (await File.ReadAllTextAsync(evidencePath)).Should().Contain("\"succeeded\": true");
    }

    [Fact]
    public async Task Safety_validation_rejects_unconfirmed_unrelated_remote_or_argument_exposed_targets()
    {
        var unconfirmed = () => DataCutoverAcceptanceRunner.RunAsync(new(
            "sqlite", $"Data Source={Path.Combine(_root, "opms-data-cutover-acceptance.db")}",
            ConfirmLocalTestDatabase: false, RecreateDatabase: true));
        var unrelatedSqlite = () => DataCutoverAcceptanceRunner.ValidateTarget("sqlite", "Data Source=production.db");
        var remoteSqlServer = () => DataCutoverAcceptanceRunner.ValidateTarget("sqlserver",
            "Server=sql.example.test;Database=OPMS_DataCutoverAcceptance;User Id=sa;Password=not-used");
        var wrongDatabase = () => DataCutoverAcceptanceRunner.ValidateTarget("sqlserver",
            @"Server=localhost\SQLEXPRESS;Database=OPMS_Development;User Id=sa;Password=not-used;TrustServerCertificate=True");
        var secretInArguments = () => DataCutoverAcceptanceOptions.Parse([
            "--provider", "sqlserver", "--connection",
            @"Server=localhost\SQLEXPRESS;Database=OPMS_DataCutoverAcceptance;User Id=sa;Password=not-used",
            "--confirm-local-test-database", "--recreate"]);

        await unconfirmed.Should().ThrowAsync<InvalidOperationException>().WithMessage("*--confirm-local-test-database*");
        unrelatedSqlite.Should().Throw<InvalidOperationException>().WithMessage("*filename must contain*");
        remoteSqlServer.Should().Throw<InvalidOperationException>().WithMessage("*restricted to*");
        wrongDatabase.Should().Throw<InvalidOperationException>().WithMessage("*must start with*");
        secretInArguments.Should().Throw<ArgumentException>().WithMessage("*--connection-env*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

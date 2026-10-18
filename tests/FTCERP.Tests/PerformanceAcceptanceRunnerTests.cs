using FluentAssertions;
using FTCERP.PerformanceAcceptance;

namespace FTCERP.Tests;

public sealed class PerformanceAcceptanceRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"opms-performance-acceptance-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Runner_creates_a_deterministic_representative_workload_and_latency_evidence()
    {
        Directory.CreateDirectory(_root);
        var database = Path.Combine(_root, "opms-performance-acceptance.db");
        var evidencePath = Path.Combine(_root, "evidence.json");

        var evidence = await PerformanceAcceptanceRunner.RunAsync(new(
            "sqlite",
            $"Data Source={database}",
            ConfirmLocalTestDatabase: true,
            RecreateDatabase: true,
            TargetCount: 30,
            Iterations: 3,
            ConcurrentWorkers: 2,
            PageSize: 10,
            QueryP95LimitMilliseconds: 30_000,
            ConcurrentP95LimitMilliseconds: 30_000,
            EvidencePath: evidencePath));

        evidence.Succeeded.Should().BeTrue();
        evidence.Provider.Should().Be("SQLite");
        evidence.TargetCount.Should().Be(30);
        evidence.SubmissionCount.Should().Be(30);
        evidence.Scenarios.Should().HaveCount(5).And.OnlyContain(item => item.Samples == 3 && item.Passed);
        evidence.DatabaseRetained.Should().BeFalse();
        File.Exists(database).Should().BeFalse();
        File.Exists(evidencePath).Should().BeTrue();
        (await File.ReadAllTextAsync(evidencePath)).Should().Contain("\"succeeded\": true");
    }

    [Fact]
    public async Task Safety_validation_rejects_unconfirmed_or_non_isolated_targets()
    {
        var unconfirmed = () => PerformanceAcceptanceRunner.RunAsync(new(
            "sqlite", $"Data Source={Path.Combine(_root, "opms-performance-acceptance.db")}",
            ConfirmLocalTestDatabase: false, RecreateDatabase: true));
        var unrelatedSqlite = () => PerformanceAcceptanceRunner.ValidateTarget("sqlite", "Data Source=production.db");
        var remoteSqlServer = () => PerformanceAcceptanceRunner.ValidateTarget("sqlserver",
            "Server=sql.example.test;Database=OPMS_PerformanceAcceptance;User Id=sa;Password=not-used");
        var wrongDatabase = () => PerformanceAcceptanceRunner.ValidateTarget("sqlserver",
            @"Server=localhost\SQLEXPRESS;Database=OPMS_Development;User Id=sa;Password=not-used;TrustServerCertificate=True");
        var sqlServerSecretInArguments = () => PerformanceAcceptanceOptions.Parse([
            "--provider", "sqlserver", "--connection",
            @"Server=localhost\SQLEXPRESS;Database=OPMS_PerformanceAcceptance;User Id=sa;Password=not-used",
            "--confirm-local-test-database", "--recreate"]);

        await unconfirmed.Should().ThrowAsync<InvalidOperationException>().WithMessage("*--confirm-local-test-database*");
        unrelatedSqlite.Should().Throw<InvalidOperationException>().WithMessage("*filename must contain*");
        remoteSqlServer.Should().Throw<InvalidOperationException>().WithMessage("*restricted to*");
        wrongDatabase.Should().Throw<InvalidOperationException>().WithMessage("*must start with*");
        sqlServerSecretInArguments.Should().Throw<ArgumentException>().WithMessage("*--connection-env*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

using FluentAssertions;
using FTCERP.RecoveryDrill;
using Microsoft.Data.Sqlite;

namespace FTCERP.Tests;

public sealed class SqliteRecoveryDrillTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"opms-recovery-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Drill_creates_verified_backup_and_evidence_then_removes_isolated_restore()
    {
        Directory.CreateDirectory(_root);
        var database = Path.Combine(_root, "opms-integration.db");
        await CreateDatabaseAsync(database);

        var evidence = await SqliteRecoveryDrill.RunAsync(new(
            database, Path.Combine(_root, "evidence"), ConfirmLocalTestDatabase: true));

        evidence.Succeeded.Should().BeTrue();
        evidence.SourceVerification.IntegrityCheck.Should().Be("ok");
        evidence.RestoreVerification.IntegrityCheck.Should().Be("ok");
        evidence.SourceVerification.ForeignKeyViolationCount.Should().Be(0);
        evidence.SourceAndRestoreCountsMatch.Should().BeTrue();
        evidence.BackupSha256.Should().MatchRegex("^[0-9a-f]{64}$");
        File.Exists(evidence.BackupPath).Should().BeTrue();
        File.Exists(evidence.EvidencePath).Should().BeTrue();
        File.Exists(evidence.RestoredDatabase).Should().BeFalse();
        File.Exists($"{evidence.RestoredDatabase}-wal").Should().BeFalse();
        File.Exists($"{evidence.RestoredDatabase}-shm").Should().BeFalse();
        (await File.ReadAllTextAsync(evidence.EvidencePath)).Should().Contain("\"succeeded\": true");
    }

    [Fact]
    public async Task Drill_requires_explicit_confirmation()
    {
        Directory.CreateDirectory(_root);
        var database = Path.Combine(_root, "opms-unconfirmed.db");
        await CreateDatabaseAsync(database);

        var action = () => SqliteRecoveryDrill.RunAsync(new(database, _root, ConfirmLocalTestDatabase: false));

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*--confirm-local-test-database*");
    }

    [Fact]
    public void Validation_rejects_non_opms_and_connection_string_targets()
    {
        Directory.CreateDirectory(_root);
        var unrelated = Path.Combine(_root, "production.db");
        File.WriteAllBytes(unrelated, []);

        var unrelatedAction = () => SqliteRecoveryDrill.ValidateSourcePath(unrelated);
        var connectionStringAction = () => SqliteRecoveryDrill.ValidateSourcePath("Data Source=opms.db;Mode=ReadOnly");

        unrelatedAction.Should().Throw<InvalidOperationException>().WithMessage("*contain 'opms'*");
        connectionStringAction.Should().Throw<InvalidOperationException>().WithMessage("*filesystem path*");
    }

    private static async Task CreateDatabaseAsync(string path)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Foreign Keys=True;Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Municipalities (Id INTEGER PRIMARY KEY, Code TEXT NOT NULL UNIQUE);
            CREATE TABLE OPMSTargets (
                Id INTEGER PRIMARY KEY,
                MunicipalityId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                FOREIGN KEY (MunicipalityId) REFERENCES Municipalities(Id));
            INSERT INTO Municipalities (Id, Code) VALUES (1, 'TEST');
            INSERT INTO OPMSTargets (Id, MunicipalityId, Name) VALUES (1, 1, 'Verified target');
            """;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

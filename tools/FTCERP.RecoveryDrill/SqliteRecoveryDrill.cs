using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace FTCERP.RecoveryDrill;

public sealed record SqliteRecoveryDrillOptions(
    string DatabasePath,
    string OutputDirectory,
    bool ConfirmLocalTestDatabase,
    int RecoveryPointObjectiveMinutes = 15,
    int RecoveryTimeObjectiveMinutes = 60,
    bool KeepRestoredDatabase = false)
{
    public static SqliteRecoveryDrillOptions Parse(IReadOnlyList<string> args)
    {
        string? database = null;
        string? output = null;
        var confirm = false;
        var keep = false;
        var rpo = 15;
        var rto = 60;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            string NextValue()
            {
                if (++index >= args.Count) throw new ArgumentException($"Missing value after {argument}.");
                return args[index];
            }

            switch (argument.ToLowerInvariant())
            {
                case "--database": database = NextValue(); break;
                case "--output": output = NextValue(); break;
                case "--confirm-local-test-database": confirm = true; break;
                case "--keep-restored-database": keep = true; break;
                case "--rpo-minutes": rpo = ParseBoundedMinutes(NextValue(), argument); break;
                case "--rto-minutes": rto = ParseBoundedMinutes(NextValue(), argument); break;
                default: throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(database)) throw new ArgumentException("--database is required.");
        output ??= Path.Combine(Environment.CurrentDirectory, "artifacts", "recovery-drills");
        return new(database, output, confirm, rpo, rto, keep);
    }

    private static int ParseBoundedMinutes(string value, string argument)
    {
        if (!int.TryParse(value, out var parsed) || parsed is < 1 or > 1440)
            throw new ArgumentException($"{argument} must be between 1 and 1440 minutes.");
        return parsed;
    }
}

public sealed record SqliteDatabaseVerification(
    string IntegrityCheck,
    int ForeignKeyViolationCount,
    int TableCount,
    IReadOnlyDictionary<string, long> RowCounts);

public sealed record SqliteRecoveryDrillEvidence(
    int SchemaVersion,
    bool Succeeded,
    string Provider,
    string SourceDatabase,
    string BackupPath,
    string RestoredDatabase,
    string EvidencePath,
    string BackupSha256,
    DateTime StartedAtUtc,
    DateTime BackupCompletedAtUtc,
    DateTime CompletedAtUtc,
    double DurationSeconds,
    double ObservedRecoveryPointAgeSeconds,
    int RecoveryPointObjectiveMinutes,
    int RecoveryTimeObjectiveMinutes,
    bool RecoveryPointObjectiveMet,
    bool RecoveryTimeObjectiveMet,
    bool SourceAndRestoreCountsMatch,
    bool IsolatedRestoreRetained,
    SqliteDatabaseVerification SourceVerification,
    SqliteDatabaseVerification RestoreVerification);

public static class SqliteRecoveryDrill
{
    private static readonly string[] PreferredEvidenceTables =
    [
        "__EFMigrationsHistory", "Municipalities", "AspNetUsers", "AspNetRoles", "Permissions",
        "RolePermissions", "SecurityUserRoleAssignments", "OpmsTargets", "IpmsTargets",
        "PerformancePeriodTargets", "OpmsSubmissions", "IpmsSubmissions", "AuditTrails", "BusinessEventOutbox"
    ];

    public static readonly object ValidationContract = new
    {
        Script = "OPMS SQLite recovery drill",
        SafetyGate = "--confirm-local-test-database",
        AcceptedExtensions = new[] { ".db", ".sqlite", ".sqlite3" },
        RequiredFileNameMarker = "opms",
        Operations = new[]
        {
            "online SQLite backup", "SHA-256 checksum", "isolated restore copy",
            "PRAGMA integrity_check", "PRAGMA foreign_key_check", "row-count reconciliation",
            "RPO/RTO measurement", "isolated restore cleanup", "JSON evidence"
        }
    };

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<SqliteRecoveryDrillEvidence> RunAsync(
        SqliteRecoveryDrillOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!options.ConfirmLocalTestDatabase)
            throw new InvalidOperationException("Pass --confirm-local-test-database to acknowledge the isolated local recovery drill.");

        var sourcePath = ValidateSourcePath(options.DatabasePath);
        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
        var backupPath = Path.Combine(outputDirectory, $"{sourceName}-{stamp}-{suffix}.backup.db");
        var restorePath = Path.Combine(outputDirectory, $"{sourceName}-{stamp}-{suffix}.restored.db");
        var evidencePath = Path.Combine(outputDirectory, $"{sourceName}-{stamp}-{suffix}.json");
        EnsureGeneratedPath(outputDirectory, backupPath);
        EnsureGeneratedPath(outputDirectory, restorePath);
        EnsureGeneratedPath(outputDirectory, evidencePath);

        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var restoreRetained = false;
        try
        {
            await using (var source = Open(sourcePath, SqliteOpenMode.ReadOnly))
            await using (var backup = Open(backupPath, SqliteOpenMode.ReadWriteCreate))
            {
                await source.OpenAsync(cancellationToken);
                await backup.OpenAsync(cancellationToken);
                source.BackupDatabase(backup);
            }

            var backupCompletedAt = DateTime.UtcNow;
            var checksum = await ComputeSha256Async(backupPath, cancellationToken);
            File.Copy(backupPath, restorePath, overwrite: false);

            var sourceVerification = await VerifyAsync(sourcePath, cancellationToken);
            var restoreVerification = await VerifyAsync(restorePath, cancellationToken);
            var countsMatch = sourceVerification.TableCount == restoreVerification.TableCount
                && sourceVerification.RowCounts.OrderBy(pair => pair.Key)
                    .SequenceEqual(restoreVerification.RowCounts.OrderBy(pair => pair.Key));
            if (!countsMatch) throw new InvalidDataException("The restored database row-count evidence does not match the source database.");

            stopwatch.Stop();
            var completedAt = DateTime.UtcNow;
            var recoveryPointAge = (completedAt - backupCompletedAt).TotalSeconds;
            var rpoMet = recoveryPointAge <= TimeSpan.FromMinutes(options.RecoveryPointObjectiveMinutes).TotalSeconds;
            var rtoMet = stopwatch.Elapsed <= TimeSpan.FromMinutes(options.RecoveryTimeObjectiveMinutes);
            if (!rpoMet) throw new InvalidDataException("The recovery drill exceeded the configured recovery point objective.");
            if (!rtoMet) throw new InvalidDataException("The recovery drill exceeded the configured recovery time objective.");

            if (options.KeepRestoredDatabase)
            {
                restoreRetained = true;
            }
            else
            {
                DeleteSqliteFiles(restorePath);
            }

            var evidence = new SqliteRecoveryDrillEvidence(
                1, true, "SQLite", sourcePath, backupPath, restorePath, evidencePath, checksum,
                startedAt, backupCompletedAt, completedAt, Math.Round(stopwatch.Elapsed.TotalSeconds, 3),
                Math.Round(recoveryPointAge, 3), options.RecoveryPointObjectiveMinutes,
                options.RecoveryTimeObjectiveMinutes, rpoMet, rtoMet, countsMatch, restoreRetained,
                sourceVerification, restoreVerification);
            await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(evidence, JsonOptions), cancellationToken);
            return evidence;
        }
        finally
        {
            if (!restoreRetained) DeleteSqliteFiles(restorePath);
        }
    }

    public static string ValidateSourcePath(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("The database path is required.");
        if (databasePath.Contains(';', StringComparison.Ordinal))
            throw new InvalidOperationException("A filesystem path is required; connection strings are not accepted.");
        var fullPath = Path.GetFullPath(databasePath);
        if (new Uri(fullPath).IsUnc) throw new InvalidOperationException("Network database paths are not allowed.");
        if (!File.Exists(fullPath)) throw new FileNotFoundException("The local SQLite database does not exist.", fullPath);
        if (!Path.GetFileName(fullPath).Contains("opms", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The database filename must contain 'opms' so the local test target is positively identified.");
        var extension = Path.GetExtension(fullPath);
        if (extension is not (".db" or ".sqlite" or ".sqlite3"))
            throw new InvalidOperationException("Only .db, .sqlite or .sqlite3 files are accepted.");
        return fullPath;
    }

    private static SqliteConnection Open(string path, SqliteOpenMode mode) => new(new SqliteConnectionStringBuilder
    {
        DataSource = path,
        Mode = mode,
        Cache = SqliteCacheMode.Private,
        ForeignKeys = true,
        Pooling = false
    }.ToString());

    private static async Task<SqliteDatabaseVerification> VerifyAsync(string path, CancellationToken cancellationToken)
    {
        await using var connection = Open(path, SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken);

        var integrityResults = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) integrityResults.Add(reader.GetString(0));
        }
        if (integrityResults.Count != 1 || !string.Equals(integrityResults[0], "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"SQLite integrity_check failed: {string.Join("; ", integrityResults)}");

        var foreignKeyViolations = 0;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) foreignKeyViolations++;
        }
        if (foreignKeyViolations != 0)
            throw new InvalidDataException($"SQLite foreign_key_check reported {foreignKeyViolations} violation(s).");

        var tableNames = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) tableNames.Add(reader.GetString(0));
        }

        var selectedTables = PreferredEvidenceTables.Where(tableNames.Contains).ToList();
        if (selectedTables.Count == 0) selectedTables = tableNames.Take(10).ToList();
        var rowCounts = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in selectedTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM \"{table.Replace("\"", "\"\"")}\";";
            rowCounts[table] = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        }

        return new("ok", foreignKeyViolations, tableNames.Count, rowCounts);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void EnsureGeneratedPath(string outputDirectory, string generatedPath)
    {
        var relative = Path.GetRelativePath(outputDirectory, Path.GetFullPath(generatedPath));
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new InvalidOperationException("Generated recovery artifacts must stay inside the selected output directory.");
    }

    private static void DeleteSqliteFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

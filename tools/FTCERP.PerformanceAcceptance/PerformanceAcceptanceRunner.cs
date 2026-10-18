using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.PerformanceAcceptance;

public sealed record PerformanceAcceptanceOptions(
    string Provider,
    string ConnectionString,
    bool ConfirmLocalTestDatabase,
    bool RecreateDatabase,
    bool KeepDatabase = false,
    int TargetCount = 10_000,
    int Iterations = 30,
    int ConcurrentWorkers = 8,
    int PageSize = 50,
    double QueryP95LimitMilliseconds = 500,
    double ConcurrentP95LimitMilliseconds = 1_500,
    string? EvidencePath = null)
{
    public static PerformanceAcceptanceOptions Parse(IReadOnlyList<string> args)
    {
        string? provider = null;
        string? connection = null;
        var connectionFromEnvironment = false;
        string? evidence = null;
        var confirm = false;
        var recreate = false;
        var keep = false;
        var targets = 10_000;
        var iterations = 30;
        var workers = 8;
        var pageSize = 50;
        var queryP95 = 500d;
        var concurrentP95 = 1_500d;

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
                case "--provider": provider = NextValue(); break;
                case "--connection": connection = NextValue(); break;
                case "--connection-env":
                    {
                        var variable = NextValue();
                        if (string.IsNullOrWhiteSpace(variable)
                            || variable.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
                            throw new ArgumentException("--connection-env requires a valid environment-variable name.");
                        connection = Environment.GetEnvironmentVariable(variable);
                        if (string.IsNullOrWhiteSpace(connection))
                            throw new ArgumentException($"Environment variable '{variable}' is not set.");
                        connectionFromEnvironment = true;
                        break;
                    }
                case "--confirm-local-test-database": confirm = true; break;
                case "--recreate": recreate = true; break;
                case "--keep-database": keep = true; break;
                case "--targets": targets = ParseBoundedInt(NextValue(), argument, 10, 1_000_000); break;
                case "--iterations": iterations = ParseBoundedInt(NextValue(), argument, 3, 10_000); break;
                case "--workers": workers = ParseBoundedInt(NextValue(), argument, 1, 128); break;
                case "--page-size": pageSize = ParseBoundedInt(NextValue(), argument, 1, 200); break;
                case "--query-p95-ms": queryP95 = ParseBoundedDouble(NextValue(), argument, 1, 300_000); break;
                case "--concurrent-p95-ms": concurrentP95 = ParseBoundedDouble(NextValue(), argument, 1, 300_000); break;
                case "--evidence": evidence = NextValue(); break;
                default: throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("--provider is required (sqlite or sqlserver).");
        if (string.IsNullOrWhiteSpace(connection)) throw new ArgumentException("--connection or --connection-env is required.");
        if (string.Equals(provider, "sqlserver", StringComparison.OrdinalIgnoreCase) && !connectionFromEnvironment)
            throw new ArgumentException("SQL Server connection strings must be supplied through --connection-env so credentials are not exposed in process arguments.");
        return new(provider, connection, confirm, recreate, keep, targets, iterations, workers, pageSize,
            queryP95, concurrentP95, evidence);
    }

    private static int ParseBoundedInt(string value, string argument, int minimum, int maximum)
    {
        if (!int.TryParse(value, out var parsed) || parsed < minimum || parsed > maximum)
            throw new ArgumentException($"{argument} must be between {minimum} and {maximum}.");
        return parsed;
    }

    private static double ParseBoundedDouble(string value, string argument, double minimum, double maximum)
    {
        if (!double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            || parsed < minimum || parsed > maximum)
            throw new ArgumentException($"{argument} must be between {minimum} and {maximum}.");
        return parsed;
    }
}

public sealed record PerformanceScenarioEvidence(
    string Name,
    int Samples,
    double MinimumMilliseconds,
    double MedianMilliseconds,
    double P95Milliseconds,
    double P99Milliseconds,
    double MaximumMilliseconds,
    double LimitMilliseconds,
    bool Passed);

public sealed record PerformanceAcceptanceEvidence(
    int SchemaVersion,
    bool Succeeded,
    string Provider,
    string Database,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    double DurationSeconds,
    int TargetCount,
    int SubmissionCount,
    int Iterations,
    int ConcurrentWorkers,
    int PageSize,
    bool DatabaseRetained,
    IReadOnlyList<PerformanceScenarioEvidence> Scenarios,
    string? EvidencePath);

public static class PerformanceAcceptanceRunner
{
    private const string MunicipalityCode = "PERF-ACCEPTANCE";
    private const string SqlServerName = @"localhost\SQLEXPRESS";
    private const string SqlServerDatabasePrefix = "OPMS_PerformanceAcceptance";
    private const string SqliteFileMarker = "opms-performance-acceptance";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static readonly object ValidationContract = new
    {
        Script = "OPMS representative performance acceptance",
        SafetyGate = "--confirm-local-test-database --recreate",
        Providers = new[] { "sqlite", "sqlserver" },
        SqlServer = new { Server = SqlServerName, RequiredDatabasePrefix = SqlServerDatabasePrefix, ConnectionSource = "environment variable only" },
        Sqlite = new { RequiredFileNameMarker = SqliteFileMarker, NetworkPathsAllowed = false },
        Workload = new[] { "target register paging", "workflow queue paging", "dashboard aggregation", "KPI search", "concurrent mixed reads" },
        Output = "redacted JSON latency and row-count evidence"
    };

    public static async Task<PerformanceAcceptanceEvidence> RunAsync(
        PerformanceAcceptanceOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!options.ConfirmLocalTestDatabase || !options.RecreateDatabase)
            throw new InvalidOperationException("Pass --confirm-local-test-database and --recreate to acknowledge recreation of the isolated local acceptance database.");

        var target = ValidateTarget(options.Provider, options.ConnectionString);
        var dbOptions = BuildDbOptions(target.Provider, target.ConnectionString);
        var startedAt = DateTime.UtcNow;
        var total = Stopwatch.StartNew();

        await using (var setup = new ApplicationDbContext(dbOptions))
        {
            await setup.Database.EnsureDeletedAsync(cancellationToken);
            if (target.Provider == "SQL Server")
                await setup.Database.MigrateAsync(cancellationToken);
            else
                await setup.Database.EnsureCreatedAsync(cancellationToken);
        }

        var municipalityId = await SeedAsync(dbOptions, options.TargetCount, cancellationToken);
        var seededCounts = await CountWorkloadAsync(dbOptions, municipalityId, cancellationToken);
        if (seededCounts.Targets != options.TargetCount || seededCounts.Submissions != options.TargetCount)
            throw new InvalidDataException($"The isolated workload count is incomplete: expected {options.TargetCount} targets/submissions but found {seededCounts.Targets}/{seededCounts.Submissions}.");
        var scenarios = new List<PerformanceScenarioEvidence>();

        scenarios.Add(await MeasureAsync("Target register paging", options.Iterations,
            options.QueryP95LimitMilliseconds,
            iteration => TargetRegisterPageAsync(dbOptions, municipalityId, options.TargetCount, options.PageSize, iteration, cancellationToken)));
        scenarios.Add(await MeasureAsync("Workflow queue paging", options.Iterations,
            options.QueryP95LimitMilliseconds,
            iteration => WorkflowQueuePageAsync(dbOptions, municipalityId, options.TargetCount, options.PageSize, iteration, cancellationToken)));
        scenarios.Add(await MeasureAsync("Dashboard aggregation", options.Iterations,
            options.QueryP95LimitMilliseconds,
            _ => DashboardAggregateAsync(dbOptions, municipalityId, cancellationToken)));
        scenarios.Add(await MeasureAsync("KPI search", options.Iterations,
            options.QueryP95LimitMilliseconds,
            iteration => SearchAsync(dbOptions, municipalityId, options.PageSize, iteration, cancellationToken)));
        scenarios.Add(await MeasureConcurrentAsync(dbOptions, municipalityId, options, cancellationToken));

        if (!options.KeepDatabase)
        {
            await using var cleanup = new ApplicationDbContext(dbOptions);
            if (!await cleanup.Database.EnsureDeletedAsync(cancellationToken))
                throw new InvalidOperationException("The isolated performance-acceptance database could not be removed after the run.");
        }

        total.Stop();
        var evidencePath = ResolveEvidencePath(options.EvidencePath);
        var evidence = new PerformanceAcceptanceEvidence(
            1, scenarios.All(item => item.Passed), target.Provider, target.DisplayName,
            startedAt, DateTime.UtcNow, Math.Round(total.Elapsed.TotalSeconds, 3), seededCounts.Targets,
            seededCounts.Submissions, options.Iterations, options.ConcurrentWorkers, options.PageSize,
            options.KeepDatabase, scenarios, evidencePath);

        if (evidencePath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath)!);
            await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(evidence, JsonOptions), cancellationToken);
        }

        return evidence;
    }

    public static (string Provider, string ConnectionString, string DisplayName) ValidateTarget(string provider, string connectionString)
    {
        if (string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
                throw new InvalidOperationException("SQLite performance acceptance requires an isolated file database.");
            var path = Path.GetFullPath(builder.DataSource);
            if (new Uri(path).IsUnc)
                throw new InvalidOperationException("Network SQLite paths are not allowed.");
            if (!Path.GetFileName(path).Contains(SqliteFileMarker, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The SQLite filename must contain '{SqliteFileMarker}'.");
            builder.DataSource = path;
            builder.ForeignKeys = true;
            builder.Pooling = false;
            return ("SQLite", builder.ToString(), path);
        }

        if (string.Equals(provider, "sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (!string.Equals(builder.DataSource, SqlServerName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SQL Server acceptance is restricted to {SqlServerName}.");
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog)
                || !builder.InitialCatalog.StartsWith(SqlServerDatabasePrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The SQL Server database must start with '{SqlServerDatabasePrefix}'.");
            if (builder.IntegratedSecurity)
                throw new InvalidOperationException("The approved local SQL Server acceptance target must use SQL authentication.");
            if (!string.Equals(builder.UserID, "sa", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The approved local SQL Server acceptance target must use the sa test login.");
            return ("SQL Server", builder.ToString(), $"{SqlServerName}/{builder.InitialCatalog}");
        }

        throw new InvalidOperationException("Provider must be sqlite or sqlserver.");
    }

    private static DbContextOptions<ApplicationDbContext> BuildDbOptions(string provider, string connectionString)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (provider == "SQLite") builder.UseSqlite(connectionString);
        else builder.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3));
        return builder.EnableDetailedErrors(false).EnableSensitiveDataLogging(false).Options;
    }

    private static async Task<long> SeedAsync(DbContextOptions<ApplicationDbContext> options, int targetCount,
        CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        var municipality = new Municipality
        {
            PublicId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Code = MunicipalityCode,
            Name = "Isolated Performance Acceptance Municipality",
            EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync(cancellationToken);

        const int batchSize = 500;
        for (var start = 0; start < targetCount; start += batchSize)
        {
            var length = Math.Min(batchSize, targetCount - start);
            for (var offset = 0; offset < length; offset++)
            {
                var index = start + offset;
                var targetId = $"perf-target-{index:D8}";
                context.OpmsTargets.Add(new OpmsTarget
                {
                    Id = targetId,
                    PublicId = DeterministicGuid(1, index),
                    MunicipalityId = municipality.Id,
                    IndicatorNumber = $"KPI-{index:D8}",
                    OriginalOrderNumber = index + 1,
                    RevisedOrderNumber = index + 1,
                    NationalKpa = $"KPA-{index % 5}",
                    MunicipalKpa = $"MKPA-{index % 8}",
                    PerformanceObjective = $"Performance objective {index % 25}",
                    TargetName = $"Representative KPI {index:D8}",
                    KpiDescription = $"Deterministic load-test KPI {index:D8}",
                    AnnualTargetDescription = "100",
                    AnnualTarget = 100,
                    Baseline = 50,
                    Weight = 1,
                    KpiType = "OUTPUT",
                    IndicatorType = "QUANTITATIVE",
                    TargetUnitType = "absolute_count",
                    CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index)
                });
                context.OpmsSubmissions.Add(new OpmsSubmission
                {
                    Id = $"perf-submission-{index:D8}",
                    PublicId = DeterministicGuid(2, index),
                    MunicipalityId = municipality.Id,
                    OpmsTargetId = targetId,
                    Quarter = $"Q{index % 4 + 1}",
                    BaseState = index % 5 == 0 ? SubmissionBaseStates.Submitted : SubmissionBaseStates.InProgress,
                    Status = index % 5 == 0 ? "completed" : "in_progress",
                    ActualPerformance = (50 + index % 51).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    AchievementPercent = 50 + index % 51,
                    TargetAchieved = index % 2 == 0,
                    CreatedOn = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index),
                    CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index)
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        return municipality.Id;
    }

    private static async Task<(int Targets, int Submissions)> CountWorkloadAsync(
        DbContextOptions<ApplicationDbContext> options,
        long municipalityId,
        CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        var targets = await context.OpmsTargets.AsNoTracking()
            .CountAsync(item => item.MunicipalityId == municipalityId, cancellationToken);
        var submissions = await context.OpmsSubmissions.AsNoTracking()
            .CountAsync(item => item.MunicipalityId == municipalityId, cancellationToken);
        return (targets, submissions);
    }

    private static async Task TargetRegisterPageAsync(DbContextOptions<ApplicationDbContext> options, long municipalityId,
        int targetCount, int pageSize, int iteration, CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        var pageCount = Math.Max(1, targetCount / pageSize);
        var skip = iteration % pageCount * pageSize;
        _ = await context.OpmsTargets.AsNoTracking().Where(item => item.MunicipalityId == municipalityId && !item.IsWithdrawn)
            .OrderBy(item => item.OriginalOrderNumber).ThenBy(item => item.Id).Skip(skip).Take(pageSize)
            .Select(item => new { item.PublicId, item.IndicatorNumber, item.TargetName, item.RowVersion })
            .ToArrayAsync(cancellationToken);
    }

    private static async Task WorkflowQueuePageAsync(DbContextOptions<ApplicationDbContext> options, long municipalityId,
        int targetCount, int pageSize, int iteration, CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        var activeCount = Math.Max(1, targetCount * 4 / 5);
        var pageCount = Math.Max(1, activeCount / pageSize);
        var skip = iteration % pageCount * pageSize;
        _ = await context.OpmsSubmissions.AsNoTracking()
            .Where(item => item.MunicipalityId == municipalityId && item.BaseState == SubmissionBaseStates.InProgress && !item.IsDisabled)
            .OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id).Skip(skip).Take(pageSize)
            .Select(item => new { item.PublicId, item.OpmsTargetId, item.ActualPerformance, item.AchievementPercent })
            .ToArrayAsync(cancellationToken);
    }

    private static async Task DashboardAggregateAsync(DbContextOptions<ApplicationDbContext> options, long municipalityId,
        CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        _ = await context.OpmsSubmissions.AsNoTracking().Where(item => item.MunicipalityId == municipalityId && !item.IsDisabled)
            .GroupBy(item => item.BaseState)
            .Select(group => new { State = group.Key, Count = group.Count(), Average = group.Average(item => item.AchievementPercent ?? 0) })
            .ToArrayAsync(cancellationToken);
    }

    private static async Task SearchAsync(DbContextOptions<ApplicationDbContext> options, long municipalityId,
        int pageSize, int iteration, CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        var search = $"KPI-{iteration % 100:D2}";
        _ = await context.OpmsTargets.AsNoTracking()
            .Where(item => item.MunicipalityId == municipalityId && item.IndicatorNumber.Contains(search))
            .OrderBy(item => item.IndicatorNumber).Take(pageSize)
            .Select(item => new { item.PublicId, item.IndicatorNumber, item.TargetName })
            .ToArrayAsync(cancellationToken);
    }

    private static async Task<PerformanceScenarioEvidence> MeasureConcurrentAsync(
        DbContextOptions<ApplicationDbContext> dbOptions,
        long municipalityId,
        PerformanceAcceptanceOptions options,
        CancellationToken cancellationToken)
    {
        await DashboardAggregateAsync(dbOptions, municipalityId, cancellationToken);
        var samples = new List<double>();
        for (var iteration = 0; iteration < options.Iterations; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, options.ConcurrentWorkers).Select(worker => (worker % 4) switch
            {
                0 => TargetRegisterPageAsync(dbOptions, municipalityId, options.TargetCount, options.PageSize, iteration + worker, cancellationToken),
                1 => WorkflowQueuePageAsync(dbOptions, municipalityId, options.TargetCount, options.PageSize, iteration + worker, cancellationToken),
                2 => DashboardAggregateAsync(dbOptions, municipalityId, cancellationToken),
                _ => SearchAsync(dbOptions, municipalityId, options.PageSize, iteration + worker, cancellationToken)
            });
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        return Summarize("Concurrent mixed reads", samples, options.ConcurrentP95LimitMilliseconds);
    }

    private static async Task<PerformanceScenarioEvidence> MeasureAsync(string name, int iterations, double limit,
        Func<int, Task> operation)
    {
        await operation(0);
        var samples = new List<double>(iterations);
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            await operation(iteration);
            stopwatch.Stop();
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }
        return Summarize(name, samples, limit);
    }

    private static PerformanceScenarioEvidence Summarize(string name, List<double> samples, double limit)
    {
        samples.Sort();
        double Percentile(double percentile) => samples[(int)Math.Ceiling(percentile * samples.Count) - 1];
        var p95 = Percentile(0.95);
        return new(name, samples.Count, Math.Round(samples[0], 3), Math.Round(Percentile(0.5), 3),
            Math.Round(p95, 3), Math.Round(Percentile(0.99), 3), Math.Round(samples[^1], 3), limit, p95 <= limit);
    }

    private static Guid DeterministicGuid(int family, int index)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, family);
        BitConverter.TryWriteBytes(bytes[4..], index);
        Encoding.ASCII.GetBytes("OPMSLOAD", bytes[8..]);
        return new Guid(bytes);
    }

    private static string? ResolveEvidencePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var fullPath = Path.GetFullPath(path);
        if (!string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Performance evidence must use a .json file.");
        return fullPath;
    }
}

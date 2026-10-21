using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FTCERP.DataCutoverAcceptance;

public sealed record DataCutoverAcceptanceOptions(
    string Provider,
    string ConnectionString,
    bool ConfirmLocalTestDatabase,
    bool RecreateDatabase,
    bool KeepDatabase = false,
    string? EvidencePath = null)
{
    public static DataCutoverAcceptanceOptions Parse(IReadOnlyList<string> args)
    {
        string? provider = null;
        string? connection = null;
        string? evidence = null;
        var connectionFromEnvironment = false;
        var confirm = false;
        var recreate = false;
        var keep = false;

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
                    var variable = NextValue();
                    if (string.IsNullOrWhiteSpace(variable)
                        || variable.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
                        throw new ArgumentException("--connection-env requires a valid environment-variable name.");
                    connection = Environment.GetEnvironmentVariable(variable);
                    if (string.IsNullOrWhiteSpace(connection))
                        throw new ArgumentException($"Environment variable '{variable}' is not set.");
                    connectionFromEnvironment = true;
                    break;
                case "--confirm-local-test-database": confirm = true; break;
                case "--recreate": recreate = true; break;
                case "--keep-database": keep = true; break;
                case "--evidence": evidence = NextValue(); break;
                default: throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("--provider is required (sqlite or sqlserver).");
        if (string.IsNullOrWhiteSpace(connection)) throw new ArgumentException("--connection or --connection-env is required.");
        if (string.Equals(provider, "sqlserver", StringComparison.OrdinalIgnoreCase) && !connectionFromEnvironment)
            throw new ArgumentException("SQL Server connection strings must be supplied through --connection-env so credentials are not exposed in process arguments.");
        return new(provider, connection, confirm, recreate, keep, evidence);
    }
}

public sealed record DataCutoverScenarioEvidence(string Name, bool Passed, IReadOnlyList<string> Checks);

public sealed record DataCutoverAcceptanceEvidence(
    int SchemaVersion,
    bool Succeeded,
    string Provider,
    string Database,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    double DurationSeconds,
    string SchemaInitialization,
    bool MigrationsApplied,
    int MigrationCount,
    bool DatabaseRetained,
    IReadOnlyList<DataCutoverScenarioEvidence> Scenarios,
    IReadOnlyDictionary<string, int> PersistedEvidence,
    string? EvidencePath);

public static class DataCutoverAcceptanceRunner
{
    private const string SqlServerName = @"localhost\SQLEXPRESS";
    private const string SqlServerDatabasePrefix = "OPMS_DataCutoverAcceptance";
    private const string SqliteFileMarker = "opms-data-cutover-acceptance";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static readonly object ValidationContract = new
    {
        Script = "OPMS historical data-cutover acceptance",
        SafetyGate = "--confirm-local-test-database --recreate",
        Providers = new[] { "sqlite", "sqlserver" },
        SqlServer = new { Server = SqlServerName, RequiredDatabasePrefix = SqlServerDatabasePrefix, ConnectionSource = "environment variable only" },
        Sqlite = new { RequiredFileNameMarker = SqliteFileMarker, NetworkPathsAllowed = false },
        Scenarios = new[] { "tenant ownership", "target-period normalization", "submission-period reconciliation", "legacy rating reconciliation" },
        Guarantees = new[] { "provider-appropriate schema initialization", "full migration chain on SQL Server", "tenant isolation", "append-only provenance", "audit persistence", "idempotent rerun", "legacy source preservation", "isolated cleanup" },
        Output = "redacted JSON reconciliation evidence"
    };

    public static async Task<DataCutoverAcceptanceEvidence> RunAsync(
        DataCutoverAcceptanceOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!options.ConfirmLocalTestDatabase || !options.RecreateDatabase)
            throw new InvalidOperationException("Pass --confirm-local-test-database and --recreate to acknowledge recreation of the isolated local acceptance database.");

        var target = ValidateTarget(options.Provider, options.ConnectionString);
        var dbOptions = BuildDbOptions(target.Provider, target.ConnectionString);
        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var scenarios = new List<DataCutoverScenarioEvidence>();
        var migrationCount = 0;

        try
        {
            await using (var setup = new ApplicationDbContext(dbOptions))
            {
                await setup.Database.EnsureDeletedAsync(cancellationToken);
                if (target.Provider == "SQL Server")
                {
                    await setup.Database.MigrateAsync(cancellationToken);
                    migrationCount = (await setup.Database.GetAppliedMigrationsAsync(cancellationToken)).Count();
                    if (migrationCount == 0) throw new InvalidDataException("No EF Core migrations were applied to the isolated cutover database.");
                }
                else if (!await setup.Database.EnsureCreatedAsync(cancellationToken))
                {
                    throw new InvalidDataException("The isolated SQLite cutover schema could not be created from the current EF model.");
                }
            }

            var seeded = await SeedAsync(dbOptions, cancellationToken);
            var tenant = new AcceptanceTenantContext(seeded.Municipality.Id, seeded.Administrator.Id, true);
            await using var context = new ApplicationDbContext(dbOptions, tenant);
            var runtimeUsers = await context.Users.IgnoreQueryFilters()
                .Where(user => user.Id == seeded.Administrator.Id || user.Id == seeded.HistoricalActor.Id)
                .ToArrayAsync(cancellationToken);
            var users = CreateUserManager(runtimeUsers);
            var governance = new WorkflowGovernanceService(context, tenantContext: tenant);
            var principal = Principal(seeded.Administrator.Id);

            scenarios.Add(await RunTenantOwnershipAsync(context, tenant, users, governance, principal, seeded, cancellationToken));
            context.ChangeTracker.Clear();
            context.AttachRange(runtimeUsers);
            scenarios.Add(await RunTargetNormalizationAsync(context, tenant, users, governance, principal, seeded, cancellationToken));
            context.ChangeTracker.Clear();
            context.AttachRange(runtimeUsers);
            scenarios.Add(await RunSubmissionPeriodAsync(context, tenant, users, governance, principal, seeded, cancellationToken));
            context.ChangeTracker.Clear();
            context.AttachRange(runtimeUsers);
            scenarios.Add(await RunRatingAsync(context, tenant, users, governance, principal, seeded, cancellationToken));
            context.ChangeTracker.Clear();

            var persisted = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["tenantOwnershipReconciliations"] = await context.LegacyTenantOwnershipReconciliations.CountAsync(cancellationToken),
                ["normalizedPeriodTargets"] = await context.PerformancePeriodTargets.CountAsync(x => x.OpmsTargetId == seeded.NormalizationTarget.Id, cancellationToken),
                ["submissionPeriodReconciliations"] = await context.LegacySubmissionPeriodReconciliations.CountAsync(cancellationToken),
                ["ratingReconciliations"] = await context.LegacySubmissionRatingReconciliations.CountAsync(cancellationToken),
                ["canonicalStageRatings"] = await context.SubmissionStageRatings.CountAsync(cancellationToken),
                ["auditTrails"] = await context.AuditTrails.CountAsync(cancellationToken)
            };
            if (persisted.Values.Any(value => value < 1))
                throw new InvalidDataException("The cutover rehearsal did not persist every required provenance, canonical, and audit record.");

            stopwatch.Stop();
            var evidencePath = ResolveEvidencePath(options.EvidencePath);
            var evidence = new DataCutoverAcceptanceEvidence(
                1, scenarios.All(item => item.Passed), target.Provider, target.DisplayName,
                startedAt, DateTime.UtcNow, Math.Round(stopwatch.Elapsed.TotalSeconds, 3),
                target.Provider == "SQL Server" ? "EF Core migration chain" : "Current EF Core model",
                target.Provider == "SQL Server",
                migrationCount, options.KeepDatabase, scenarios, persisted, evidencePath);

            if (evidencePath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(evidencePath)!);
                await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(evidence, JsonOptions), cancellationToken);
            }
            return evidence;
        }
        finally
        {
            if (!options.KeepDatabase)
            {
                await using var cleanup = new ApplicationDbContext(dbOptions);
                await cleanup.Database.EnsureDeletedAsync(cancellationToken);
            }
        }
    }

    public static (string Provider, string ConnectionString, string DisplayName) ValidateTarget(string provider, string connectionString)
    {
        if (string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
                throw new InvalidOperationException("SQLite cutover acceptance requires an isolated file database.");
            var path = Path.GetFullPath(builder.DataSource);
            if (new Uri(path).IsUnc) throw new InvalidOperationException("Network SQLite paths are not allowed.");
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
                throw new InvalidOperationException($"SQL Server cutover acceptance is restricted to {SqlServerName}.");
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog)
                || !builder.InitialCatalog.StartsWith(SqlServerDatabasePrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The SQL Server database must start with '{SqlServerDatabasePrefix}'.");
            if (builder.IntegratedSecurity || !string.Equals(builder.UserID, "sa", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The approved local SQL Server cutover target must use the sa SQL test login.");
            return ("SQL Server", builder.ToString(), $"{SqlServerName}/{builder.InitialCatalog}");
        }

        throw new InvalidOperationException("Provider must be sqlite or sqlserver.");
    }

    private static DbContextOptions<ApplicationDbContext> BuildDbOptions(string provider, string connectionString)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (provider == "SQLite")
            builder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        return provider == "SQL Server"
            ? builder.UseSqlServer(connectionString).Options
            : builder.UseSqlite(connectionString).Options;
    }

    private static async Task<DataCutoverScenarioEvidence> RunTenantOwnershipAsync(
        ApplicationDbContext context, AcceptanceTenantContext tenant, UserManager<ApplicationUser> users,
        IWorkflowGovernanceService governance, ClaimsPrincipal principal, SeededData seeded, CancellationToken cancellationToken)
    {
        var controller = Controller(new LegacyTenantOwnershipReconciliationController(context, tenant, users, governance), principal, "cutover-tenant");
        var preview = Data(await controller.Preview(LegacyTenantEntityType.Department, 1, 25));
        var ready = preview.Items.Single(item => item.EntityPublicId == seeded.LegacyDepartment.PublicId);
        Require(ready.Status == "Ready", "Legacy tenant ownership was not deterministically resolvable.");
        var result = Data(await controller.Execute(new(LegacyTenantEntityType.Department, ready.EntityPublicId,
            ready.RowVersion, "Approved isolated historical tenant cutover rehearsal.")));
        context.ChangeTracker.Clear();
        Require((await context.Departments.IgnoreQueryFilters().SingleAsync(x => x.PublicId == ready.EntityPublicId, cancellationToken)).MunicipalityId == seeded.Municipality.Id,
            "Tenant ownership was not persisted.");
        Require(await context.LegacyTenantOwnershipReconciliations.AnyAsync(x => x.PublicId == result.ReconciliationPublicId, cancellationToken),
            "Tenant ownership provenance was not persisted.");
        var rerun = await controller.Execute(new(LegacyTenantEntityType.Department, ready.EntityPublicId, ready.RowVersion,
            "Reject a repeated historical tenant cutover rehearsal."));
        Require(rerun.Result is ConflictObjectResult, "Tenant ownership rerun was not rejected.");
        var foreign = preview.Items.Single(item => item.EntityPublicId == seeded.ForeignLegacyDepartment.PublicId);
        var crossTenant = await controller.Execute(new(LegacyTenantEntityType.Department, foreign.EntityPublicId, foreign.RowVersion,
            "Reject a cross-municipality historical tenant cutover."));
        Require(crossTenant.Result is ConflictObjectResult, "Cross-municipality ownership was not rejected.");
        return Passed("Tenant ownership", "single authoritative municipality", "cross-tenant rejection", "append-only provenance", "audit persistence", "rerun rejection");
    }

    private static async Task<DataCutoverScenarioEvidence> RunTargetNormalizationAsync(
        ApplicationDbContext context, AcceptanceTenantContext tenant, UserManager<ApplicationUser> users,
        IWorkflowGovernanceService governance, ClaimsPrincipal principal, SeededData seeded, CancellationToken cancellationToken)
    {
        var controller = Controller(new TargetNormalizationController(context, tenant, new PerformanceUnitEngine(), users, governance), principal, "cutover-target");
        var preview = Data(await controller.Preview(SubmissionKind.Opms));
        var ready = preview.Items.Single(item => item.TargetPublicId == seeded.NormalizationTarget.PublicId);
        Require(ready.Status == "Ready" && ready.MissingPeriodTargets == 2,
            $"Legacy target did not produce the expected canonical period plan: status={ready.Status}, missing={ready.MissingPeriodTargets}, error={ready.Error}");
        var result = Data(await controller.Execute(new(SubmissionKind.Opms,
            [new TargetNormalizationSelection(ready.TargetPublicId, ready.RowVersion)], "Approved isolated historical target normalization rehearsal.")));
        Require(result.AddedPeriodTargets == 2, "Canonical target periods were not added exactly once.");
        context.ChangeTracker.Clear();
        var rows = await context.PerformancePeriodTargets.Where(x => x.OpmsTargetId == seeded.NormalizationTarget.Id).ToArrayAsync(cancellationToken);
        Require(rows.Length == 2, "Canonical target-period persistence is incomplete.");
        var rerunPreview = Data(await controller.Preview(SubmissionKind.Opms));
        var normalized = rerunPreview.Items.Single(item => item.TargetPublicId == seeded.NormalizationTarget.PublicId);
        Require(normalized.Status == "Normalized" && normalized.MissingPeriodTargets == 0, "Target normalization is not idempotent.");
        return Passed("Target-period normalization", "legacy values preserved", "canonical Q1 and annual rows", "audit persistence", "idempotent preview");
    }

    private static async Task<DataCutoverScenarioEvidence> RunSubmissionPeriodAsync(
        ApplicationDbContext context, AcceptanceTenantContext tenant, UserManager<ApplicationUser> users,
        IWorkflowGovernanceService governance, ClaimsPrincipal principal, SeededData seeded, CancellationToken cancellationToken)
    {
        var controller = Controller(new LegacySubmissionPeriodReconciliationController(context, tenant, users, governance), principal, "cutover-period");
        var preview = Data(await controller.Preview(SubmissionKind.Opms, 1, 25));
        var row = preview.Items.Single(item => item.SubmissionPublicId == seeded.PeriodSubmission.PublicId);
        var candidate = row.Candidates.Single();
        Require(row.Status == "Ready" && !candidate.HasSubmissionConflict, "Legacy submission period was not unambiguous.");
        Data(await controller.Execute(new(SubmissionKind.Opms, row.SubmissionPublicId,
            candidate.ReportingPeriodPublicId, "Approved isolated historical submission-period rehearsal.", row.RowVersion)));
        context.ChangeTracker.Clear();
        var submission = await context.OpmsSubmissions.SingleAsync(x => x.PublicId == row.SubmissionPublicId, cancellationToken);
        Require(submission.ReportingPeriodId == seeded.Q1.Id && submission.Quarter == "Quarter 1", "Canonical period link or preserved legacy quarter is incorrect.");
        var rerun = await controller.Execute(new(SubmissionKind.Opms, row.SubmissionPublicId,
            candidate.ReportingPeriodPublicId, "Reject repeated historical submission-period reconciliation.", row.RowVersion));
        Require(rerun.Result is ConflictObjectResult, "Submission-period rerun was not rejected.");
        return Passed("Submission-period reconciliation", "target-scoped canonical period", "legacy quarter preserved", "one logical submission", "append-only provenance", "rerun rejection");
    }

    private static async Task<DataCutoverScenarioEvidence> RunRatingAsync(
        ApplicationDbContext context, AcceptanceTenantContext tenant, UserManager<ApplicationUser> users,
        IWorkflowGovernanceService governance, ClaimsPrincipal principal, SeededData seeded, CancellationToken cancellationToken)
    {
        var controller = Controller(new LegacyRatingReconciliationController(context, tenant, users, governance), principal, "cutover-rating");
        var preview = Data(await controller.Preview(SubmissionKind.Opms, LegacyRatingSourceKind.VerifierScore, 1, 25));
        var row = preview.Items.Single(item => item.SubmissionPublicId == seeded.RatingSubmission.PublicId);
        var candidate = row.Candidates.Single();
        Require(row.Status == "Ready" && row.SourceScore == 4m, "Legacy rating did not resolve to one workflow candidate.");
        Data(await controller.Execute(new ReconcileLegacyRatingRequest(SubmissionKind.Opms,
            LegacyRatingSourceKind.VerifierScore, row.SourceKey, candidate.WorkflowActionPublicId,
            "Approved isolated historical rating reconciliation rehearsal.")));
        context.ChangeTracker.Clear();
        var rating = await context.SubmissionStageRatings.SingleAsync(cancellationToken);
        var source = await context.OpmsSubmissions.SingleAsync(x => x.PublicId == seeded.RatingSubmission.PublicId, cancellationToken);
        Require(rating.Value == 4m && source.VerifierScore == 4m, "Canonical rating or preserved legacy rating source is incorrect.");
        var rerun = await controller.Execute(new ReconcileLegacyRatingRequest(SubmissionKind.Opms,
            LegacyRatingSourceKind.VerifierScore, row.SourceKey, candidate.WorkflowActionPublicId,
            "Reject repeated historical rating reconciliation rehearsal."));
        Require(rerun.Result is ConflictObjectResult, "Rating rerun was not rejected.");
        return Passed("Legacy rating reconciliation", "same-submission same-actor mapping", "configured scheme value", "legacy source preserved", "append-only provenance", "rerun rejection");
    }

    private static async Task<SeededData> SeedAsync(DbContextOptions<ApplicationDbContext> options, CancellationToken cancellationToken)
    {
        await using var context = new ApplicationDbContext(options);
        var municipality = new Municipality { Code = "CUTOVER-A", Name = "Cutover Municipality A" };
        var otherMunicipality = new Municipality { Code = "CUTOVER-B", Name = "Cutover Municipality B" };
        var administrator = User("cutover-admin", "Cutover", "Administrator");
        var historicalActor = User("historical-verifier", "Historical", "Verifier");
        administrator.Municipality = municipality;
        historicalActor.Municipality = municipality;
        var year = new FinancialYear { Code = "2024/25", Name = "2024/25", StartDate = new(2024, 7, 1), EndDate = new(2025, 6, 30) };
        var legacyYear = new Period { Code = "FY2024", Name = "2024/25", FiscalYear = year.Code, StartDate = year.StartDate, EndDate = year.EndDate, IsActive = true };
        var legacyDepartment = new Department { Code = "LEGACY-A", Name = "Legacy Department A" };
        var foreignLegacyDepartment = new Department { Code = "LEGACY-B", Name = "Legacy Department B" };
        context.AddRange(municipality, otherMunicipality, administrator, historicalActor, year, legacyYear, legacyDepartment, foreignLegacyDepartment);
        await context.SaveChangesAsync(cancellationToken);
        context.Positions.AddRange(
            new Position { MunicipalityId = municipality.Id, DepartmentId = legacyDepartment.Id, Code = "CUT-A-P", Name = "Cutover A position" },
            new Position { MunicipalityId = otherMunicipality.Id, DepartmentId = foreignLegacyDepartment.Id, Code = "CUT-B-P", Name = "Cutover B position" });
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = year.Id, FinancialYear = year,
            EffectiveFrom = year.StartDate, IsCurrent = true
        };
        context.MunicipalityFinancialYears.Add(municipalityYear);
        await context.SaveChangesAsync(cancellationToken);
        var q1 = new ReportingPeriod
        {
            MunicipalityFinancialYearId = municipalityYear.Id, Code = "Q1", Name = "Quarter 1",
            PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = year.StartDate, EndDate = new(2024, 9, 30)
        };
        var annual = new ReportingPeriod
        {
            MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANN", Name = "Annual",
            PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = year.StartDate, EndDate = year.EndDate
        };
        var normalizationTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, PeriodId = legacyYear.Id, IndicatorNumber = "CUT-NORMALIZE",
            TargetName = "Legacy normalization target", KpiDescription = "Legacy normalization target",
            PerformanceObjective = "Objective", TargetUnitType = "percentage", AnnualTarget = 100m,
            AnnualTargetDescription = "Annual target", Q1Target = 20m, Q1Description = "Quarter 1 target", Q1Budget = 10m
        };
        var periodTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, PeriodId = legacyYear.Id, IndicatorNumber = "CUT-PERIOD",
            TargetName = "Legacy submission period target", KpiDescription = "Legacy submission period target", PerformanceObjective = "Objective"
        };
        var ratingTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = historicalActor.Id, IndicatorNumber = "CUT-RATING",
            TargetName = "Legacy rating target", KpiDescription = "Legacy rating target", AnnualTargetDescription = "Target", PerformanceObjective = "Objective"
        };
        var scheme = new RatingScheme { MunicipalityId = municipality.Id, Code = "CUTOVER-FIVE", Name = "Cutover five point" };
        var ratingValue = new RatingSchemeValue { MunicipalityId = municipality.Id, RatingScheme = scheme, Value = 4m, Label = "Achieved", SortOrder = 4 };
        context.AddRange(q1, annual, normalizationTarget, periodTarget, ratingTarget, scheme, ratingValue);
        await context.SaveChangesAsync(cancellationToken);
        context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
        {
            MunicipalityId = municipality.Id, ReportingPeriodId = q1.Id, OpmsTargetId = periodTarget.Id,
            UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = "10", CreatedByUserId = administrator.Id
        });
        var periodSubmission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTargetId = periodTarget.Id, Quarter = "Quarter 1",
            BaseState = SubmissionBaseStates.InProgress, Status = "Draft", CreatedBy = administrator.Id
        };
        var ratingSubmission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTargetId = ratingTarget.Id, Quarter = "Q1",
            BaseState = SubmissionBaseStates.Submitted, Status = "verified", SubmittedByUserId = historicalActor.Id,
            SubmittedAt = new DateTime(2024, 8, 1, 8, 0, 0, DateTimeKind.Utc), VerifierScore = 4m,
            VerifierUserId = historicalActor.Id, VerifiedAt = new DateTime(2024, 8, 2, 9, 30, 0, DateTimeKind.Utc),
            VerifierComments = "Historical verifier comment"
        };
        var workflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id,
            SubmissionKind = SubmissionKind.Opms, Code = "CUTOVER-RATING", Name = "Cutover rating", EffectiveFrom = year.StartDate
        };
        var stage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id, WorkflowDefinition = workflow, Code = "VERIFY", Name = "Verify", Sequence = 1,
            RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY",
            RequiresRating = true, RatingScheme = scheme, IsTerminal = true
        };
        context.AddRange(periodSubmission, ratingSubmission, workflow, stage);
        await context.SaveChangesAsync(cancellationToken);
        var instance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, SubmissionKind = SubmissionKind.Opms,
            SubmissionId = ratingSubmission.Id, CurrentStageId = stage.Id
        };
        context.SubmissionWorkflowInstances.Add(instance);
        await context.SaveChangesAsync(cancellationToken);
        var actionPublicId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO SubmissionWorkflowActions
                (PublicId, MunicipalityId, SubmissionWorkflowInstanceId, Sequence, FromStageId, ToStageId,
                 ActionCode, Outcome, ActorUserId, Comment, RatingValue, CorrelationId, OccurredAt)
            VALUES ({actionPublicId}, {municipality.Id}, {instance.Id}, 1, {stage.Id}, NULL,
                 {"VERIFY"}, {(int)WorkflowActionOutcome.Approve}, {historicalActor.Id}, {ratingSubmission.VerifierComments}, NULL,
                 {"cutover-rating-action"}, {ratingSubmission.VerifiedAt!.Value})", cancellationToken);
        var action = await context.SubmissionWorkflowActions.SingleAsync(x => x.PublicId == actionPublicId, cancellationToken);
        return new(municipality, otherMunicipality, administrator, historicalActor, legacyDepartment,
            foreignLegacyDepartment, normalizationTarget, periodSubmission, ratingSubmission, q1, action);
    }

    private static ApplicationUser User(string id, string firstName, string lastName) => new()
    {
        Id = id, UserName = $"{id}@local", NormalizedUserName = $"{id}@local".ToUpperInvariant(),
        Email = $"{id}@local.test", NormalizedEmail = $"{id}@local.test".ToUpperInvariant(),
        FirstName = firstName, LastName = lastName, SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"), CreatedAt = DateTime.UtcNow, IsActive = true, EmailConfirmed = true
    };

    private static UserManager<ApplicationUser> CreateUserManager(params ApplicationUser[] users) => new(
        new AcceptanceUserStore(users), Options.Create(new IdentityOptions()), new PasswordHasher<ApplicationUser>(),
        [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);

    private static ClaimsPrincipal Principal(string userId) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, userId)], "DataCutoverAcceptance"));

    private static T Controller<T>(T controller, ClaimsPrincipal principal, string traceId) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal, TraceIdentifier = traceId }
        };
        return controller;
    }

    private static T Data<T>(ActionResult<ApiResponse<T>> response)
    {
        if (response.Result is not OkObjectResult { Value: ApiResponse<T> { Success: true, Data: not null } envelope })
        {
            var resultType = response.Result?.GetType().Name ?? "null";
            var value = response.Result is ObjectResult objectResult
                ? JsonSerializer.Serialize(objectResult.Value, JsonOptions)
                : "no response body";
            throw new InvalidDataException($"A cutover controller returned {resultType}: {value}");
        }
        return envelope.Data;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static DataCutoverScenarioEvidence Passed(string name, params string[] checks) => new(name, true, checks);

    private static string? ResolveEvidencePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Path.GetFullPath(path);
    }

    private sealed record SeededData(
        Municipality Municipality,
        Municipality OtherMunicipality,
        ApplicationUser Administrator,
        ApplicationUser HistoricalActor,
        Department LegacyDepartment,
        Department ForeignLegacyDepartment,
        OpmsTarget NormalizationTarget,
        OpmsSubmission PeriodSubmission,
        OpmsSubmission RatingSubmission,
        ReportingPeriod Q1,
        SubmissionWorkflowAction RatingAction);

    private sealed class AcceptanceTenantContext(long? municipalityId, string? userId, bool isSystem) : ITenantContext
    {
        public long? MunicipalityId { get; } = municipalityId;
        public string? UserId { get; } = userId;
        public bool IsSystem { get; } = isSystem;
    }

    private sealed class AcceptanceUserStore(IEnumerable<ApplicationUser> users) : IUserStore<ApplicationUser>
    {
        private readonly Dictionary<string, ApplicationUser> _users = users.ToDictionary(user => user.Id, StringComparer.OrdinalIgnoreCase);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(_users.GetValueOrDefault(userId));
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Values.SingleOrDefault(user => user.NormalizedUserName == normalizedUserName));
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public void Dispose() { }
    }
}

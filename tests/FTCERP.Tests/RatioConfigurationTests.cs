using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class RatioConfigurationTests
{
    [Fact]
    public async Task Sqlite_migrations_create_governed_ratio_configuration_schema()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-ratio-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "AspNetUsers" ("Id" TEXT NOT NULL PRIMARY KEY);
                CREATE TABLE "Municipalities" ("Id" INTEGER NOT NULL PRIMARY KEY);
                CREATE TABLE "PerformancePeriodTargets" (
                    "Id" INTEGER NOT NULL PRIMARY KEY,
                    "MunicipalityId" INTEGER NOT NULL,
                    "UnitKind" INTEGER NOT NULL,
                    "RevisedUnitKind" INTEGER NULL,
                    "TargetValue" TEXT NOT NULL,
                    "RevisedTargetValue" TEXT NULL,
                    "CreatedByUserId" TEXT NOT NULL
                );
                CREATE TABLE "OpmsUnitDefinitions" ("Id" INTEGER NOT NULL PRIMARY KEY, "ValueDataType" TEXT NOT NULL);
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                INSERT INTO "AspNetUsers" ("Id") VALUES ('ratio-user');
                INSERT INTO "Municipalities" ("Id") VALUES (7);
                INSERT INTO "PerformancePeriodTargets"
                    ("Id", "MunicipalityId", "UnitKind", "RevisedUnitKind", "TargetValue", "RevisedTargetValue", "CreatedByUserId")
                    VALUES (11, 7, 8, NULL, '2:3:5', NULL, 'ratio-user');
                INSERT INTO "OpmsUnitDefinitions" ("Id", "ValueDataType") VALUES (5, 'JSON');
                """);
            var migrations = context.Database.GetMigrations().ToArray();
            var latest = migrations[^1];
            latest.Should().EndWith("V39GovernedRatioConfiguration");
            foreach (var migration in migrations[..^1])
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            await context.GetService<IMigrator>().MigrateAsync(latest);

            (await context.RatioComparisonMethods.CountAsync()).Should().Be(4);
            var backfilled = await context.PerformanceRatioDefinitions.Include(item => item.Segments).SingleAsync();
            backfilled.RatioSegmentCount.Should().Be(3);
            backfilled.Segments.OrderBy(item => item.SegmentPosition).Select(item => item.SegmentLabel)
                .Should().Equal("Segment 1", "Segment 2", "Segment 3");
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA foreign_key_check;";
            await context.Database.OpenConnectionAsync();
            await using var violations = await command.ExecuteReaderAsync();
            (await violations.ReadAsync()).Should().BeFalse();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Ratio_definition_accepts_only_complete_two_to_four_segment_configuration(byte count)
    {
        var segments = Enumerable.Range(1, count).Select(position => new PerformanceRatioSegment
        {
            SegmentPosition = (byte)position,
            SegmentLabel = $"Label {position}",
            DecimalPlaces = 2,
            MinValue = 0,
            IsActive = true
        }).ToArray();

        Assert.Null(PerformanceRatioRules.ValidateDefinition(count, count == 2 ? "NORMALIZED_RATIO" : "MANUAL", segments));
        Assert.NotNull(PerformanceRatioRules.ValidateDefinition(5, "MANUAL", segments));
    }

    [Fact]
    public async Task Ratio_configuration_persists_labels_method_constraints_and_rowversions_relationally()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await AddRatioTarget(context, 3, "EXACT_MATCH", "2:3:5", "Officers", "Population", "Households");

        context.ChangeTracker.Clear();
        var stored = await context.PerformanceRatioDefinitions.Include(item => item.RatioComparisonMethod)
            .Include(item => item.Segments)
            .SingleAsync(item => item.PerformancePeriodTarget.OpmsTargetId == fixture.Target.Id);

        Assert.Equal((byte)3, stored.RatioSegmentCount);
        Assert.Equal("EXACT_MATCH", stored.RatioComparisonMethod.Code);
        Assert.Equal(new[] { "Officers", "Population", "Households" }, stored.Segments.OrderBy(item => item.SegmentPosition).Select(item => item.SegmentLabel));
        Assert.Equal(16, stored.RowVersion.Length);
        Assert.All(stored.Segments, item => Assert.Equal(16, item.RowVersion.Length));

        stored.Segments.Add(new PerformanceRatioSegment
        {
            MunicipalityId = fixture.Municipality.Id,
            SegmentPosition = 3,
            SegmentLabel = "Duplicate",
            IsActive = true
        });
        context.PerformanceTargetRevisions.Add(new PerformanceTargetRevision
        {
            MunicipalityId = fixture.Municipality.Id,
            PerformancePeriodTargetId = stored.PerformancePeriodTargetId,
            FieldName = "RatioDefinition",
            OriginalValue = "3 segments",
            RevisedValue = "duplicate position",
            Reason = "Constraint verification",
            ApprovalReference = "TEST-RATIO-UNIQUE",
            EffectiveAt = DateTime.UtcNow,
            RevisedByUserId = stored.CreatedByUserId
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Submission_resolution_enforces_configured_count_labels_and_zero_denominator()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var three = await AddRatioTarget(context, 3, "EXACT_MATCH", "2:3:5", "Women", "Men", "Youth");
        var normalized = await AddRatioTarget(context, 2, "NORMALIZED_RATIO", "4:3000", "Officers", "Population");
        var service = new SubmissionValueService(context, new PerformanceUnitEngine());

        var mismatch = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveOpmsAsync(three.Target.Id, three.Period.PublicId, "2:3", null));
        Assert.Contains("exactly 3 ratio segments", mismatch.Message);

        var denominator = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveOpmsAsync(normalized.Target.Id, normalized.Period.PublicId, "4:0", null));
        Assert.Contains("Population cannot be zero", denominator.Message);

        var valid = await service.ResolveOpmsAsync(three.Target.Id, three.Period.PublicId, "1:2:3", null);
        Assert.Equal("1:2:3", valid.Calculation!.CanonicalActual);
    }

    private static async Task<(Municipality Municipality, ReportingPeriod Period, OpmsTarget Target)> AddRatioTarget(
        ApplicationDbContext context,
        byte count,
        string methodCode,
        string targetValue,
        params string[] labels)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = IdpTestFixture.CreateUser($"ratio-{suffix}");
        var municipality = new Municipality { Code = $"R{suffix}", Name = $"Ratio {suffix}" };
        var year = new FinancialYear { Code = $"{2030 + count}/{31 + count}", Name = $"Ratio year {suffix}", StartDate = new(2030 + count, 7, 1), EndDate = new(2031 + count, 6, 30) };
        var municipalityYear = new MunicipalityFinancialYear { Municipality = municipality, FinancialYear = year, IsCurrent = true, EffectiveFrom = year.StartDate };
        var period = new ReportingPeriod { MunicipalityFinancialYear = municipalityYear, Code = $"Q1-{suffix}", Name = "Quarter one", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = year.StartDate, EndDate = new(2030 + count, 9, 30) };
        var target = new OpmsTarget { Id = $"ratio-target-{suffix}", Municipality = municipality, IndicatorNumber = $"RATIO-{suffix}", TargetName = "Configured ratio", KpiDescription = "Configured ratio" };
        context.AddRange(user, municipality, year, municipalityYear, period, target);
        await context.SaveChangesAsync();

        var method = await context.RatioComparisonMethods.SingleAsync(item => item.Code == methodCode);
        var periodTarget = new PerformancePeriodTarget
        {
            MunicipalityId = municipality.Id,
            ReportingPeriodId = period.Id,
            OpmsTargetId = target.Id,
            UnitKind = PerformanceUnitKind.Ratios,
            Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = targetValue,
            CreatedByUserId = user.Id,
            RatioDefinition = new PerformanceRatioDefinition
            {
                MunicipalityId = municipality.Id,
                RatioSegmentCount = count,
                RatioComparisonMethodId = method.Id,
                CreatedByUserId = user.Id,
                Segments = labels.Select((label, index) => new PerformanceRatioSegment
                {
                    MunicipalityId = municipality.Id,
                    SegmentPosition = (byte)(index + 1),
                    SegmentLabel = label,
                    DecimalPlaces = 2,
                    MinValue = 0,
                    IsActive = true
                }).ToList()
            }
        };
        context.PerformancePeriodTargets.Add(periodTarget);
        await context.SaveChangesAsync();
        return (municipality, period, target);
    }
}

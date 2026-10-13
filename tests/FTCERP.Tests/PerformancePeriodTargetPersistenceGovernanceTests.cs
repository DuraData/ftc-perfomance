using FTCERP.Host.Domain.Services;

namespace FTCERP.Tests;

public class PerformancePeriodTargetPersistenceGovernanceTests
{
    [Fact]
    public async Task Canonical_period_target_cannot_be_rewritten_without_exact_revision_records()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);
        seeded.Target.IsTargetRevised = true;
        seeded.Target.RevisedTargetValue = "125";

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-period target field '*' requires one exact same-transaction revision record.");
    }

    [Fact]
    public async Task Canonical_period_target_identity_and_original_values_are_immutable_even_with_revision_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);
        seeded.Target.TargetValue = "999";
        context.PerformanceTargetRevisions.Add(Revision(seeded, nameof(PerformancePeriodTarget.TargetValue), "100", "999"));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Canonical performance-period target identity, ownership, period, KPI, original values and creation evidence are immutable.");
    }

    [Fact]
    public async Task Canonical_period_target_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);
        context.PerformancePeriodTargets.Remove(seeded.Target);

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Canonical performance-period target history cannot be hard deleted*");
    }

    [Fact]
    public async Task Exact_atomic_field_revisions_allow_change_and_advance_rowversion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedAsync(context);
        var originalVersion = Convert.ToBase64String(seeded.Target.RowVersion);
        seeded.Target.IsTargetRevised = true;
        seeded.Target.RevisedTargetValue = "125";
        context.PerformanceTargetRevisions.AddRange(
            Revision(seeded, nameof(PerformancePeriodTarget.IsTargetRevised), false.ToString(), true.ToString()),
            Revision(seeded, nameof(PerformancePeriodTarget.RevisedTargetValue), null, "125"));

        await context.SaveChangesAsync();

        Convert.ToBase64String(seeded.Target.RowVersion).Should().NotBe(originalVersion);
        (await context.PerformanceTargetRevisions.CountAsync()).Should().Be(2);
    }

    private static async Task<Seeded> SeedAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "PERIOD-GOV", Name = "Period Governance" };
        var user = IdpTestFixture.CreateUser("period-governance-user");
        var year = new FinancialYear
        {
            Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2027, 6, 30), IsActive = true
        };
        context.AddRange(municipality, user, year);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = year.Id, IsCurrent = true, IsActive = true,
            EffectiveFrom = year.StartDate
        };
        var target = new OpmsTarget
        {
            Id = $"period-governance-{Guid.NewGuid():N}", MunicipalityId = municipality.Id,
            IndicatorNumber = "GOV-1", OriginalOrderNumber = 1, RevisedOrderNumber = 1,
            TargetName = "Governed target", KpiDescription = "Governed target", KpiType = "Output",
            IndicatorType = "Quantitative", Weight = 10
        };
        context.AddRange(municipalityYear, target);
        await context.SaveChangesAsync();
        var period = new ReportingPeriod
        {
            MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANNUAL", Name = "Annual",
            PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = year.StartDate,
            EndDate = year.EndDate, IsActive = true
        };
        context.Add(period);
        await context.SaveChangesAsync();
        var periodTarget = new PerformancePeriodTarget
        {
            MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, OpmsTargetId = target.Id,
            UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = "100", BudgetValue = 1000, CreatedByUserId = user.Id
        };
        context.Add(periodTarget);
        await context.SaveChangesAsync();
        return new Seeded(periodTarget, user);
    }

    private static PerformanceTargetRevision Revision(Seeded seeded, string field, string? original, string? revised) => new()
    {
        MunicipalityId = seeded.Target.MunicipalityId,
        PerformancePeriodTargetId = seeded.Target.Id,
        FieldName = field,
        OriginalValue = original,
        RevisedValue = revised,
        Reason = "Council-approved canonical period target revision",
        ApprovalReference = "COUNCIL-2026-10",
        EffectiveAt = new DateTime(2026, 10, 13),
        RevisedByUserId = seeded.User.Id
    };

    private sealed record Seeded(PerformancePeriodTarget Target, ApplicationUser User);
}

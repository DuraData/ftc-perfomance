using System.Globalization;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

internal sealed record LegacyPeriodTargetValue(
    ReportingPeriodType PeriodType,
    decimal? Value,
    decimal? BudgetValue,
    string? Description);

internal sealed record PlannedPeriodTarget(
    ReportingPeriod ReportingPeriod,
    PerformanceUnitKind UnitKind,
    PerformanceDirection Direction,
    string TargetValue,
    decimal? BudgetValue,
    string? Description);

internal sealed record TargetPeriodPlan(IReadOnlyList<PlannedPeriodTarget> Rows, string? Error)
{
    public bool IsValid => Error == null;
    public static TargetPeriodPlan Invalid(string error) => new([], error);
}

internal static class TargetPeriodCutover
{
    public static async Task HydrateLegacyProjectionAsync(ApplicationDbContext context, IReadOnlyCollection<OpmsTarget> targets)
    {
        if (targets.Count == 0) return;
        var ids = targets.Select(item => item.Id).ToArray();
        var rows = await context.PerformancePeriodTargets.AsNoTracking().Include(item => item.ReportingPeriod)
            .Where(item => item.OpmsTargetId != null && ids.Contains(item.OpmsTargetId) && item.IsActive)
            .ToArrayAsync();
        foreach (var target in targets)
            ApplyProjection(target, rows.Where(item => item.OpmsTargetId == target.Id));
    }

    public static async Task HydrateLegacyProjectionAsync(ApplicationDbContext context, IReadOnlyCollection<IpmsTarget> targets)
    {
        if (targets.Count == 0) return;
        var ids = targets.Select(item => item.Id).ToArray();
        var rows = await context.PerformancePeriodTargets.AsNoTracking().Include(item => item.ReportingPeriod)
            .Where(item => item.IpmsTargetId != null && ids.Contains(item.IpmsTargetId) && item.IsActive)
            .ToArrayAsync();
        foreach (var target in targets)
            ApplyProjection(target, rows.Where(item => item.IpmsTargetId == target.Id));
    }

    public static async Task<TargetPeriodPlan> BuildPlanAsync(
        ApplicationDbContext context,
        IPerformanceUnitEngine unitEngine,
        long? municipalityId,
        int? legacyPeriodId,
        string targetUnitType,
        IEnumerable<LegacyPeriodTargetValue> values)
    {
        if (municipalityId is not > 0)
            return TargetPeriodPlan.Invalid("Select a municipality context before saving performance targets.");
        if (!legacyPeriodId.HasValue)
            return TargetPeriodPlan.Invalid("A financial-year period is required before normalized target values can be saved.");

        var legacyPeriod = await context.Periods.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == legacyPeriodId.Value && item.IsActive);
        if (legacyPeriod == null)
            return TargetPeriodPlan.Invalid("The selected legacy period is missing or inactive.");

        var municipalityYear = await context.MunicipalityFinancialYears
            .AsNoTracking()
            .Include(item => item.FinancialYear)
            .Include(item => item.ReportingPeriods)
            .Where(item => item.MunicipalityId == municipalityId.Value && item.IsActive && item.FinancialYear.Code == legacyPeriod.FiscalYear)
            .OrderByDescending(item => item.IsCurrent)
            .ThenByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync();
        if (municipalityYear == null)
            return TargetPeriodPlan.Invalid($"No active municipality financial year matches '{legacyPeriod.FiscalYear}'. Configure the governed financial-year master before saving targets.");

        if (!TryParseUnitKind(targetUnitType, out var unitKind))
            return TargetPeriodPlan.Invalid($"Target unit type '{targetUnitType}' is not supported by the canonical performance engine.");

        var direction = DefaultDirection(unitKind);
        var rows = new List<PlannedPeriodTarget>();
        foreach (var value in values.Where(item => item.Value.HasValue))
        {
            var matchingPeriods = municipalityYear.ReportingPeriods
                .Where(item => item.IsActive && item.PeriodType == value.PeriodType)
                .OrderBy(item => item.Sequence)
                .ToArray();
            if (matchingPeriods.Length != 1)
                return TargetPeriodPlan.Invalid($"Exactly one active {value.PeriodType} reporting period is required for {municipalityYear.FinancialYear.Code}; found {matchingPeriods.Length}.");

            var rawValue = value.Value!.Value.ToString(CultureInfo.InvariantCulture);
            var normalized = unitEngine.Normalize(unitKind, rawValue);
            if (!normalized.IsValid)
                return TargetPeriodPlan.Invalid($"{value.PeriodType} target is invalid: {normalized.Error}");
            rows.Add(new PlannedPeriodTarget(
                matchingPeriods[0], unitKind, direction, normalized.CanonicalValue!, value.BudgetValue, value.Description?.Trim()));
        }

        return new TargetPeriodPlan(rows, null);
    }

    public static void AddNewRows(
        ApplicationDbContext context,
        TargetPeriodPlan plan,
        long municipalityId,
        string userId,
        string? opmsTargetId,
        string? ipmsTargetId)
    {
        foreach (var row in plan.Rows)
            context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
            {
                MunicipalityId = municipalityId,
                ReportingPeriodId = row.ReportingPeriod.Id,
                OpmsTargetId = opmsTargetId,
                IpmsTargetId = ipmsTargetId,
                UnitKind = row.UnitKind,
                Direction = row.Direction,
                TargetValue = row.TargetValue,
                BudgetValue = row.BudgetValue,
                Description = row.Description,
                CreatedByUserId = userId
            });
    }

    public static async Task<string?> EnsureUnchangedOrAddMissingAsync(
        ApplicationDbContext context,
        TargetPeriodPlan plan,
        long municipalityId,
        string userId,
        string? opmsTargetId,
        string? ipmsTargetId)
    {
        var existing = opmsTargetId != null
            ? await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).Where(item => item.OpmsTargetId == opmsTargetId && item.IsActive).ToArrayAsync()
            : await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).Where(item => item.IpmsTargetId == ipmsTargetId && item.IsActive).ToArrayAsync();

        var removed = existing.FirstOrDefault(item => plan.Rows.All(row => row.ReportingPeriod.Id != item.ReportingPeriodId));
        if (removed != null)
            return $"{removed.ReportingPeriod.Code} target values cannot be removed or moved to another financial year through the general target form. Revise or deactivate them through /api/v1/performance-period-targets/{removed.PublicId}.";

        foreach (var row in plan.Rows)
        {
            var current = existing.SingleOrDefault(item => item.ReportingPeriodId == row.ReportingPeriod.Id);
            if (current == null) continue;

            if (current.UnitKind != row.UnitKind || current.Direction != row.Direction || current.TargetValue != row.TargetValue ||
                current.BudgetValue != row.BudgetValue || current.Description != row.Description)
                return $"{row.ReportingPeriod.Code} target values are governed records. Revise them through /api/v1/performance-period-targets/{current.PublicId} with a reason, approval reference and RowVersion.";
        }

        foreach (var missing in plan.Rows.Where(row => existing.All(item => item.ReportingPeriodId != row.ReportingPeriod.Id)))
            AddNewRows(context, new TargetPeriodPlan([missing], null), municipalityId, userId, opmsTargetId, ipmsTargetId);

        return null;
    }

    public static LegacyPeriodTargetValue[] Values(
        decimal annualTarget,
        string? annualDescription,
        decimal? q1Target, string? q1Description, decimal? q1Budget,
        decimal? q2Target, string? q2Description, decimal? q2Budget,
        decimal? midTermTarget, string? midTermDescription, decimal? midTermBudget,
        decimal? q3Target, string? q3Description, decimal? q3Budget,
        decimal? q4Target, string? q4Description, decimal? q4Budget) =>
    [
        new(ReportingPeriodType.Quarter1, q1Target, q1Budget, q1Description),
        new(ReportingPeriodType.Quarter2, q2Target, q2Budget, q2Description),
        new(ReportingPeriodType.MidTerm, midTermTarget, midTermBudget, midTermDescription),
        new(ReportingPeriodType.Quarter3, q3Target, q3Budget, q3Description),
        new(ReportingPeriodType.Quarter4, q4Target, q4Budget, q4Description),
        new(ReportingPeriodType.Annual, annualTarget, null, annualDescription)
    ];

    public static string? ValidateNoLegacyRevisionValues(decimal? q3RevisedTarget, decimal? q4RevisedTarget, decimal? revisedAnnualTarget, decimal? revisedAnnualBudget) =>
        q3RevisedTarget.HasValue || q4RevisedTarget.HasValue || revisedAnnualTarget.HasValue || revisedAnnualBudget.HasValue
            ? "Legacy revised-target fields are retired. Create the base period target, then use the governed performance-period-target revision endpoint."
            : null;

    private static bool TryParseUnitKind(string value, out PerformanceUnitKind unitKind)
    {
        if (value.Trim().Equals("percentage", StringComparison.OrdinalIgnoreCase))
        {
            unitKind = PerformanceUnitKind.PercentageBased;
            return true;
        }
        var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse(normalized, true, out unitKind)) return true;
        unitKind = value.Trim().ToLowerInvariant() switch
        {
            "qualitative" => PerformanceUnitKind.QualitativeTargets,
            _ => PerformanceUnitKind.None
        };
        return value.Trim().Equals("qualitative", StringComparison.OrdinalIgnoreCase);
    }

    private static PerformanceDirection DefaultDirection(PerformanceUnitKind unitKind) => unitKind switch
    {
        PerformanceUnitKind.ReverseCumulative or PerformanceUnitKind.ReverseNonCumulative or PerformanceUnitKind.TimeBased or PerformanceUnitKind.Date => PerformanceDirection.LowerIsBetter,
        PerformanceUnitKind.Binary or PerformanceUnitKind.BinaryDetermination or PerformanceUnitKind.QualitativeTargets or PerformanceUnitKind.None or PerformanceUnitKind.ZeroBased => PerformanceDirection.Exact,
        _ => PerformanceDirection.HigherIsBetter
    };

    private static void ApplyProjection(OpmsTarget target, IEnumerable<PerformancePeriodTarget> rows)
    {
        foreach (var row in rows)
        {
            var value = DecimalValue(row.TargetValue);
            switch (row.ReportingPeriod.PeriodType)
            {
                case ReportingPeriodType.Quarter1: target.Q1Target = value; target.Q1Budget = row.BudgetValue; target.Q1Description = row.Description; break;
                case ReportingPeriodType.Quarter2: target.Q2Target = value; target.Q2Budget = row.BudgetValue; target.Q2Description = row.Description; break;
                case ReportingPeriodType.MidTerm: target.MidTermTarget = value; target.MidTermBudget = row.BudgetValue; target.MidTermDescription = row.Description; break;
                case ReportingPeriodType.Quarter3: target.Q3Target = value; target.Q3Budget = row.BudgetValue; target.Q3Description = row.Description; break;
                case ReportingPeriodType.Quarter4: target.Q4Target = value; target.Q4Budget = row.BudgetValue; target.Q4Description = row.Description; break;
                case ReportingPeriodType.Annual: target.AnnualTarget = value ?? 0; target.AnnualTargetDescription = row.Description ?? string.Empty; break;
            }
        }
    }

    private static void ApplyProjection(IpmsTarget target, IEnumerable<PerformancePeriodTarget> rows)
    {
        foreach (var row in rows)
        {
            var value = DecimalValue(row.TargetValue);
            switch (row.ReportingPeriod.PeriodType)
            {
                case ReportingPeriodType.Quarter1: target.Q1Target = value; target.Q1Budget = row.BudgetValue; target.Q1Description = row.Description; break;
                case ReportingPeriodType.Quarter2: target.Q2Target = value; target.Q2Budget = row.BudgetValue; target.Q2Description = row.Description; break;
                case ReportingPeriodType.MidTerm: target.MidTermTarget = value; target.MidTermBudget = row.BudgetValue; target.MidTermDescription = row.Description; break;
                case ReportingPeriodType.Quarter3: target.Q3Target = value; target.Q3Budget = row.BudgetValue; target.Q3Description = row.Description; break;
                case ReportingPeriodType.Quarter4: target.Q4Target = value; target.Q4Budget = row.BudgetValue; target.Q4Description = row.Description; break;
                case ReportingPeriodType.Annual: target.AnnualTarget = value ?? 0; target.AnnualTargetDescription = row.Description ?? string.Empty; break;
            }
        }
    }

    private static decimal? DecimalValue(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
}

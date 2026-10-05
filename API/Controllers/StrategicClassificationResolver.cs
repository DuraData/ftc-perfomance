using FTCERP.Host.API.Requests;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

internal sealed record StrategicClassificationSelection(
    Guid? NationalKpaPublicId,
    Guid? MunicipalKpaPublicId,
    Guid? BackToBasicsPillarPublicId,
    Guid? StrategicGoalPublicId,
    Guid? StrategicInterventionPublicId,
    Guid? StrategicObjectivePublicId,
    Guid? PerformanceObjectivePublicId)
{
    public bool HasCanonicalSelection => NationalKpaPublicId.HasValue || MunicipalKpaPublicId.HasValue || BackToBasicsPillarPublicId.HasValue
        || StrategicGoalPublicId.HasValue || StrategicInterventionPublicId.HasValue || StrategicObjectivePublicId.HasValue || PerformanceObjectivePublicId.HasValue;
    public bool IsComplete => NationalKpaPublicId.HasValue && MunicipalKpaPublicId.HasValue && BackToBasicsPillarPublicId.HasValue
        && StrategicGoalPublicId.HasValue && StrategicInterventionPublicId.HasValue && StrategicObjectivePublicId.HasValue && PerformanceObjectivePublicId.HasValue;
}

internal sealed record ResolvedStrategicClassification(
    NationalKpa? NationalKpa,
    MunicipalKpa? MunicipalKpa,
    BackToBasicsPillar? BackToBasicsPillar,
    MunicipalStrategicGoal? StrategicGoal,
    StrategicIntervention? StrategicIntervention,
    MunicipalStrategicObjective? StrategicObjective,
    PerformanceObjective? PerformanceObjective,
    string? Error)
{
    public bool IsValid => Error == null;
}

internal static class StrategicClassificationResolver
{
    public static StrategicClassificationSelection Selection(SaveOpmsTargetRequest request) => new(request.NationalKpaPublicId, request.MunicipalKpaPublicId,
        request.BackToBasicsPillarPublicId, request.StrategicGoalPublicId, request.StrategicInterventionPublicId, request.StrategicObjectivePublicId, request.PerformanceObjectivePublicId);

    public static StrategicClassificationSelection Selection(SaveIpmsTargetRequest request) => new(request.NationalKpaPublicId, request.MunicipalKpaPublicId,
        request.BackToBasicsPillarPublicId, request.StrategicGoalPublicId, request.StrategicInterventionPublicId, request.StrategicObjectivePublicId, request.PerformanceObjectivePublicId);

    public static void Apply(OpmsTarget target, ResolvedStrategicClassification value)
    {
        ApplyIds(target, value);
        target.NationalKpa = value.NationalKpa?.Name ?? string.Empty;
        target.MunicipalKpa = value.MunicipalKpa?.Name ?? string.Empty;
        target.PerformanceObjective = value.PerformanceObjective?.Name ?? string.Empty;
        target.StrategicGoalId = null;
        target.StrategicObjectiveId = null;
    }

    public static void Apply(IpmsTarget target, ResolvedStrategicClassification value)
    {
        ApplyIds(target, value);
        target.NationalKpa = value.NationalKpa?.Name ?? string.Empty;
        target.MunicipalKpa = value.MunicipalKpa?.Name ?? string.Empty;
        target.PerformanceObjective = value.PerformanceObjective?.Name ?? string.Empty;
        target.StrategicGoalId = null;
        target.StrategicObjectiveId = null;
    }

    public static async Task<ResolvedStrategicClassification> ResolveAsync(
        ApplicationDbContext context,
        long municipalityFinancialYearId,
        StrategicClassificationSelection selection,
        long? existingNationalKpaId = null,
        long? existingMunicipalKpaId = null,
        long? existingBackToBasicsPillarId = null,
        long? existingStrategicGoalId = null,
        long? existingStrategicInterventionId = null,
        long? existingStrategicObjectiveId = null,
        long? existingPerformanceObjectiveId = null)
    {
        var year = await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.Id == municipalityFinancialYearId);
        if (year == null) return Invalid("The selected municipality financial year was not found.");

        var nationalKpa = selection.NationalKpaPublicId.HasValue
            ? await context.NationalKpas.SingleOrDefaultAsync(item => item.PublicId == selection.NationalKpaPublicId.Value) : null;
        var backToBasics = selection.BackToBasicsPillarPublicId.HasValue
            ? await context.BackToBasicsPillars.SingleOrDefaultAsync(item => item.PublicId == selection.BackToBasicsPillarPublicId.Value) : null;
        var municipalKpa = selection.MunicipalKpaPublicId.HasValue
            ? await StrategicQuery(context.MunicipalKpas).SingleOrDefaultAsync(item => item.MunicipalityId == year.MunicipalityId && item.PublicId == selection.MunicipalKpaPublicId.Value) : null;
        var goal = selection.StrategicGoalPublicId.HasValue
            ? await StrategicQuery(context.MunicipalStrategicGoals).SingleOrDefaultAsync(item => item.MunicipalityId == year.MunicipalityId && item.PublicId == selection.StrategicGoalPublicId.Value) : null;
        var intervention = selection.StrategicInterventionPublicId.HasValue
            ? await StrategicQuery(context.StrategicInterventions).SingleOrDefaultAsync(item => item.MunicipalityId == year.MunicipalityId && item.PublicId == selection.StrategicInterventionPublicId.Value) : null;
        var objective = selection.StrategicObjectivePublicId.HasValue
            ? await StrategicQuery(context.MunicipalStrategicObjectives).SingleOrDefaultAsync(item => item.MunicipalityId == year.MunicipalityId && item.PublicId == selection.StrategicObjectivePublicId.Value) : null;
        var performanceObjective = selection.PerformanceObjectivePublicId.HasValue
            ? await StrategicQuery(context.PerformanceObjectives).SingleOrDefaultAsync(item => item.MunicipalityId == year.MunicipalityId && item.PublicId == selection.PerformanceObjectivePublicId.Value) : null;

        if (selection.NationalKpaPublicId.HasValue && nationalKpa == null || selection.BackToBasicsPillarPublicId.HasValue && backToBasics == null
            || selection.MunicipalKpaPublicId.HasValue && municipalKpa == null || selection.StrategicGoalPublicId.HasValue && goal == null
            || selection.StrategicInterventionPublicId.HasValue && intervention == null || selection.StrategicObjectivePublicId.HasValue && objective == null
            || selection.PerformanceObjectivePublicId.HasValue && performanceObjective == null)
            return Invalid("One or more strategic classifications were not found in the selected municipality.");

        if (nationalKpa != null && (!nationalKpa.IsActive && nationalKpa.Id != existingNationalKpaId
            || await context.MunicipalityNationalKpas.AnyAsync(item => item.MunicipalityId == year.MunicipalityId && item.NationalKpaId == nationalKpa.Id && !item.IsEnabled)))
            return Invalid("The selected National KPA is not available for new capture in this municipality.");
        if (backToBasics != null && (!backToBasics.IsActive && backToBasics.Id != existingBackToBasicsPillarId
            || await context.MunicipalityBackToBasicsPillars.AnyAsync(item => item.MunicipalityId == year.MunicipalityId && item.BackToBasicsPillarId == backToBasics.Id && !item.IsEnabled)))
            return Invalid("The selected Back-to-Basics pillar is not available for new capture in this municipality.");

        var strategic = new (StrategicPlanningMasterBase? Item, long? ExistingId, string Label)[]
        {
            (municipalKpa, existingMunicipalKpaId, "Municipal KPA"), (goal, existingStrategicGoalId, "Strategic Goal"),
            (intervention, existingStrategicInterventionId, "Strategic Intervention"), (objective, existingStrategicObjectiveId, "Strategic Objective"),
            (performanceObjective, existingPerformanceObjectiveId, "Performance Objective")
        };
        foreach (var value in strategic.Where(value => value.Item != null))
        {
            if (!value.Item!.IsActive && value.Item.Id != value.ExistingId) return Invalid($"The selected {value.Label} is inactive.");
            if (value.Item.Id != value.ExistingId && !AppliesToYear(value.Item, year)) return Invalid($"The selected {value.Label} is not valid for {year.FinancialYear.Code}.");
        }

        var mappingError = await ValidateMappings(context, municipalKpa, goal, intervention, objective, performanceObjective);
        return mappingError == null
            ? new(nationalKpa, municipalKpa, backToBasics, goal, intervention, objective, performanceObjective, null)
            : Invalid(mappingError);
    }

    private static IQueryable<TEntity> StrategicQuery<TEntity>(DbSet<TEntity> set) where TEntity : StrategicPlanningMasterBase =>
        set.Include(item => item.EffectiveFromFinancialYear).ThenInclude(item => item!.FinancialYear)
            .Include(item => item.EffectiveToFinancialYear).ThenInclude(item => item!.FinancialYear);

    private static bool AppliesToYear(StrategicPlanningMasterBase item, MunicipalityFinancialYear year) =>
        (!item.EffectiveFromFinancialYearId.HasValue || item.EffectiveFromFinancialYear!.FinancialYear.StartDate <= year.FinancialYear.StartDate)
        && (!item.EffectiveToFinancialYearId.HasValue || item.EffectiveToFinancialYear!.FinancialYear.EndDate >= year.FinancialYear.EndDate);

    private static async Task<string?> ValidateMappings(ApplicationDbContext context, MunicipalKpa? kpa, MunicipalStrategicGoal? goal,
        StrategicIntervention? intervention, MunicipalStrategicObjective? objective, PerformanceObjective? performanceObjective)
    {
        if (kpa != null && goal != null && await context.MunicipalKpaStrategicGoals.AnyAsync(item => item.MunicipalKpaId == kpa.Id && item.IsActive)
            && !await context.MunicipalKpaStrategicGoals.AnyAsync(item => item.MunicipalKpaId == kpa.Id && item.StrategicGoalId == goal.Id && item.IsActive))
            return "The Strategic Goal is not configured for the selected Municipal KPA.";
        if (goal != null && intervention != null && await context.StrategicGoalInterventions.AnyAsync(item => item.StrategicGoalId == goal.Id && item.IsActive)
            && !await context.StrategicGoalInterventions.AnyAsync(item => item.StrategicGoalId == goal.Id && item.StrategicInterventionId == intervention.Id && item.IsActive))
            return "The Strategic Intervention is not configured for the selected Strategic Goal.";
        if (goal != null && objective != null && await context.StrategicGoalObjectives.AnyAsync(item => item.StrategicGoalId == goal.Id && item.IsActive)
            && !await context.StrategicGoalObjectives.AnyAsync(item => item.StrategicGoalId == goal.Id && item.StrategicObjectiveId == objective.Id && item.IsActive))
            return "The Strategic Objective is not configured for the selected Strategic Goal.";
        if (intervention != null && objective != null && await context.StrategicInterventionObjectives.AnyAsync(item => item.StrategicInterventionId == intervention.Id && item.IsActive)
            && !await context.StrategicInterventionObjectives.AnyAsync(item => item.StrategicInterventionId == intervention.Id && item.StrategicObjectiveId == objective.Id && item.IsActive))
            return "The Strategic Objective is not configured for the selected Strategic Intervention.";
        if (objective != null && performanceObjective != null && await context.StrategicObjectivePerformanceObjectives.AnyAsync(item => item.StrategicObjectiveId == objective.Id && item.IsActive)
            && !await context.StrategicObjectivePerformanceObjectives.AnyAsync(item => item.StrategicObjectiveId == objective.Id && item.PerformanceObjectiveId == performanceObjective.Id && item.IsActive))
            return "The Performance Objective is not configured for the selected Strategic Objective.";
        return null;
    }

    private static ResolvedStrategicClassification Invalid(string error) => new(null, null, null, null, null, null, null, error);

    private static void ApplyIds(OpmsTarget target, ResolvedStrategicClassification value)
    {
        target.NationalKpaId = value.NationalKpa?.Id; target.MunicipalKpaId = value.MunicipalKpa?.Id; target.BackToBasicsPillarId = value.BackToBasicsPillar?.Id;
        target.StrategicGoalMasterId = value.StrategicGoal?.Id; target.StrategicInterventionId = value.StrategicIntervention?.Id;
        target.StrategicObjectiveMasterId = value.StrategicObjective?.Id; target.PerformanceObjectiveId = value.PerformanceObjective?.Id;
    }

    private static void ApplyIds(IpmsTarget target, ResolvedStrategicClassification value)
    {
        target.NationalKpaId = value.NationalKpa?.Id; target.MunicipalKpaId = value.MunicipalKpa?.Id; target.BackToBasicsPillarId = value.BackToBasicsPillar?.Id;
        target.StrategicGoalMasterId = value.StrategicGoal?.Id; target.StrategicInterventionId = value.StrategicIntervention?.Id;
        target.StrategicObjectiveMasterId = value.StrategicObjective?.Id; target.PerformanceObjectiveId = value.PerformanceObjective?.Id;
    }
}

using FTCERP.Host.API.Requests;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

internal sealed record ResolvedBudgetSource(GovernedBudgetSource Source, decimal? Amount);
internal sealed record ResolvedBudgetClassification(GovernedBudgetType? BudgetType, ResolvedBudgetSource[] BudgetSources, string? Error)
{
    public bool IsValid => Error == null;
}

internal static class BudgetClassificationResolver
{
    public static async Task<ResolvedBudgetClassification> ResolveAsync(ApplicationDbContext context, long municipalityFinancialYearId,
        Guid? budgetTypePublicId, SaveKpiBudgetSourceRequest[]? requestedSources, long? existingBudgetTypeId = null,
        IReadOnlyCollection<long>? existingBudgetSourceIds = null)
    {
        var year = await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.Id == municipalityFinancialYearId);
        if (year == null) return Invalid("The selected municipality financial year was not found.");

        GovernedBudgetType? budgetType = null;
        if (budgetTypePublicId.HasValue)
        {
            budgetType = await Query(context.GovernedBudgetTypes).SingleOrDefaultAsync(item =>
                item.MunicipalityId == year.MunicipalityId && item.PublicId == budgetTypePublicId.Value);
            if (budgetType == null) return Invalid("The selected Budget Type was not found in this municipality.");
            var error = ValidateMaster(budgetType, year, budgetType.Id == existingBudgetTypeId, "Budget Type");
            if (error != null) return Invalid(error);
        }

        var inputs = requestedSources ?? [];
        if (inputs.Any(item => item.BudgetSourcePublicId == Guid.Empty || item.Amount < 0))
            return Invalid("Each Budget Source must have a valid public identifier and a non-negative optional amount.");
        if (inputs.GroupBy(item => item.BudgetSourcePublicId).Any(group => group.Count() > 1))
            return Invalid("A Budget Source may be selected only once per KPI.");

        var publicIds = inputs.Select(item => item.BudgetSourcePublicId).ToArray();
        var sources = await Query(context.GovernedBudgetSources).Where(item => item.MunicipalityId == year.MunicipalityId && publicIds.Contains(item.PublicId)).ToArrayAsync();
        if (sources.Length != publicIds.Length) return Invalid("One or more Budget Sources were not found in this municipality.");
        var existingIds = existingBudgetSourceIds ?? [];
        foreach (var source in sources)
        {
            var error = ValidateMaster(source, year, existingIds.Contains(source.Id), "Budget Source");
            if (error != null) return Invalid(error);
        }

        var resolved = inputs.Select(input => new ResolvedBudgetSource(sources.Single(item => item.PublicId == input.BudgetSourcePublicId), input.Amount)).ToArray();
        return new(budgetType, resolved, null);
    }

    public static void Apply(OpmsTarget target, ResolvedBudgetClassification value)
    {
        var municipalityId = target.MunicipalityId ?? throw new InvalidOperationException("A municipality is required before applying budget classifications.");
        target.BudgetTypeMasterId = value.BudgetType?.Id;
        target.BudgetTypeId = null;
        target.BudgetSourceId = null;
        ApplyLinks(target.GovernedBudgetSources, value.BudgetSources,
            resolved => new OpmsKpiBudgetSource { MunicipalityId = municipalityId, OpmsTargetId = target.Id, BudgetSourceId = resolved.Source.Id, Amount = resolved.Amount });
    }

    public static void Apply(IpmsTarget target, ResolvedBudgetClassification value)
    {
        var municipalityId = target.MunicipalityId ?? throw new InvalidOperationException("A municipality is required before applying budget classifications.");
        target.BudgetTypeMasterId = value.BudgetType?.Id;
        target.BudgetTypeId = null;
        target.BudgetSourceId = null;
        ApplyLinks(target.GovernedBudgetSources, value.BudgetSources,
            resolved => new IpmsKpiBudgetSource { MunicipalityId = municipalityId, IpmsTargetId = target.Id, BudgetSourceId = resolved.Source.Id, Amount = resolved.Amount });
    }

    private static void ApplyLinks<TEntity>(ICollection<TEntity> links, ResolvedBudgetSource[] desired, Func<ResolvedBudgetSource, TEntity> factory)
        where TEntity : KpiBudgetSourceBase
    {
        foreach (var existing in links.Where(item => item.IsActive).ToArray())
        {
            var requested = desired.SingleOrDefault(item => item.Source.Id == existing.BudgetSourceId && item.Amount == existing.Amount);
            if (requested == null) existing.IsActive = false;
        }
        foreach (var requested in desired.Where(item => links.All(link => !link.IsActive || link.BudgetSourceId != item.Source.Id || link.Amount != item.Amount)))
            links.Add(factory(requested));
    }

    private static IQueryable<TEntity> Query<TEntity>(DbSet<TEntity> set) where TEntity : StrategicPlanningMasterBase =>
        set.Include(item => item.EffectiveFromFinancialYear).ThenInclude(item => item!.FinancialYear)
            .Include(item => item.EffectiveToFinancialYear).ThenInclude(item => item!.FinancialYear);

    private static string? ValidateMaster(StrategicPlanningMasterBase item, MunicipalityFinancialYear year, bool isExisting, string label)
    {
        if (!item.IsActive && !isExisting) return $"The selected {label} is inactive.";
        if (!isExisting && ((!item.EffectiveFromFinancialYearId.HasValue || item.EffectiveFromFinancialYear!.FinancialYear.StartDate <= year.FinancialYear.StartDate)
            && (!item.EffectiveToFinancialYearId.HasValue || item.EffectiveToFinancialYear!.FinancialYear.EndDate >= year.FinancialYear.EndDate)) == false)
            return $"The selected {label} is not valid for {year.FinancialYear.Code}.";
        return null;
    }

    private static ResolvedBudgetClassification Invalid(string error) => new(null, [], error);
}

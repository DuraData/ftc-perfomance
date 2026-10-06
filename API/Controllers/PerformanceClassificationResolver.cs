using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

internal sealed record ResolvedPerformanceClassification(GovernedKpiType? KpiType, GovernedIndicatorType? IndicatorType,
    GovernedFunctionalArea? FunctionalArea, GovernedStandardClassification? StandardClassification, string? Error)
{
    public bool IsValid => Error == null;
}

internal sealed record ResolvedKpiUnitOfMeasure(GovernedKpiUnitOfMeasure? UnitOfMeasure, string? Error)
{
    public bool IsValid => Error == null;
}

internal static class PerformanceClassificationResolver
{
    public static async Task<ResolvedPerformanceClassification> ResolveAsync(ApplicationDbContext context, long yearId,
        Guid? kpiTypePublicId, Guid? indicatorTypePublicId, Guid? functionalAreaPublicId, Guid? standardClassificationPublicId,
        bool supportsStandardClassification, long? existingKpiTypeId = null, long? existingIndicatorTypeId = null,
        long? existingFunctionalAreaId = null, long? existingStandardClassificationId = null)
    {
        var year = await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.Id == yearId);
        if (year == null) return Invalid("The selected municipality financial year was not found.");
        if (!kpiTypePublicId.HasValue || !indicatorTypePublicId.HasValue) return Invalid("KPI Type and Indicator Type are required governed classifications.");

        var kpiType = await Resolve(context.GovernedKpiTypes, year, kpiTypePublicId, existingKpiTypeId, "KPI Type");
        if (kpiType.Error != null) return Invalid(kpiType.Error);
        var indicatorType = await Resolve(context.GovernedIndicatorTypes, year, indicatorTypePublicId, existingIndicatorTypeId, "Indicator Type");
        if (indicatorType.Error != null) return Invalid(indicatorType.Error);
        var functionalArea = await Resolve(context.GovernedFunctionalAreas, year, functionalAreaPublicId, existingFunctionalAreaId, "Functional Area");
        if (functionalArea.Error != null) return Invalid(functionalArea.Error);
        if (!supportsStandardClassification && standardClassificationPublicId.HasValue) return Invalid("Standard Classification is not supported for IPMS targets.");
        var standard = supportsStandardClassification
            ? await Resolve(context.GovernedStandardClassifications, year, standardClassificationPublicId, existingStandardClassificationId, "Standard Classification")
            : (Item: (GovernedStandardClassification?)null, Error: (string?)null);
        if (standard.Error != null) return Invalid(standard.Error);
        return new(kpiType.Item, indicatorType.Item, functionalArea.Item, standard.Item, null);
    }

    public static void Apply(OpmsTarget target, ResolvedPerformanceClassification value)
    {
        target.KpiTypeMasterId = value.KpiType!.Id; target.KpiType = value.KpiType.Name;
        target.IndicatorTypeMasterId = value.IndicatorType!.Id; target.IndicatorType = value.IndicatorType.Name;
        target.FunctionalAreaMasterId = value.FunctionalArea?.Id; target.FunctionalArea = value.FunctionalArea?.Name;
        target.StandardClassificationMasterId = value.StandardClassification?.Id; target.StandardClassification = value.StandardClassification?.Name;
    }

    public static void Apply(IpmsTarget target, ResolvedPerformanceClassification value)
    {
        target.KpiTypeMasterId = value.KpiType!.Id; target.KpiType = value.KpiType.Name;
        target.IndicatorTypeMasterId = value.IndicatorType!.Id; target.IndicatorType = value.IndicatorType.Name;
        target.FunctionalAreaMasterId = value.FunctionalArea?.Id; target.FunctionalArea = value.FunctionalArea?.Name;
    }

    public static async Task<ResolvedKpiUnitOfMeasure> ResolveUnitAsync(ApplicationDbContext context, long yearId,
        Guid? publicId, long? existingId = null)
    {
        var year = await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.Id == yearId);
        if (year == null) return new(null, "The selected municipality financial year was not found.");
        if (!publicId.HasValue) return new(null, "KPI Unit of Measure is required.");
        var resolved = await Resolve(context.GovernedKpiUnitOfMeasures, year, publicId, existingId, "KPI Unit of Measure");
        return new(resolved.Item, resolved.Error);
    }

    public static void ApplyUnit(OpmsTarget target, ResolvedKpiUnitOfMeasure value)
    {
        target.KpiUnitOfMeasureMasterId = value.UnitOfMeasure!.Id;
        target.UnitOfMeasureId = null;
    }

    public static void ApplyUnit(IpmsTarget target, ResolvedKpiUnitOfMeasure value)
    {
        target.KpiUnitOfMeasureMasterId = value.UnitOfMeasure!.Id;
        target.UnitOfMeasureId = null;
    }

    private static async Task<(T? Item, string? Error)> Resolve<T>(DbSet<T> set, MunicipalityFinancialYear year, Guid? publicId, long? existingId, string label) where T : StrategicPlanningMasterBase
    {
        if (!publicId.HasValue) return (null, null);
        var item = await set.Include(value => value.EffectiveFromFinancialYear).ThenInclude(value => value!.FinancialYear)
            .Include(value => value.EffectiveToFinancialYear).ThenInclude(value => value!.FinancialYear)
            .SingleOrDefaultAsync(value => value.MunicipalityId == year.MunicipalityId && value.PublicId == publicId.Value);
        if (item == null) return (null, $"The selected {label} was not found in this municipality.");
        var historical = item.Id == existingId;
        if (!item.IsActive && !historical) return (null, $"The selected {label} is inactive.");
        if (!historical && ((!item.EffectiveFromFinancialYearId.HasValue || item.EffectiveFromFinancialYear!.FinancialYear.StartDate <= year.FinancialYear.StartDate)
            && (!item.EffectiveToFinancialYearId.HasValue || item.EffectiveToFinancialYear!.FinancialYear.EndDate >= year.FinancialYear.EndDate)) == false)
            return (null, $"The selected {label} is not valid for {year.FinancialYear.Code}.");
        return (item, null);
    }

    private static ResolvedPerformanceClassification Invalid(string error) => new(null, null, null, null, error);
}

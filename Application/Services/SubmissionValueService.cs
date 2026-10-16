using System.Globalization;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Application.Services;

public sealed record SubmissionValueResolution(ReportingPeriod Period, PerformancePeriodTarget Target, PerformanceCalculationResult? Calculation);

public interface ISubmissionValueService
{
    Task<SubmissionValueResolution> ResolveOpmsAsync(string targetId, Guid reportingPeriodPublicId, string? actualPerformance, decimal? legacyActual);
    Task<SubmissionValueResolution> ResolveIpmsAsync(string targetId, Guid reportingPeriodPublicId, string? actualPerformance, decimal? legacyActual);
}

public sealed class SubmissionValueService(ApplicationDbContext context, IPerformanceUnitEngine unitEngine) : ISubmissionValueService
{
    public Task<SubmissionValueResolution> ResolveOpmsAsync(string targetId, Guid reportingPeriodPublicId, string? actualPerformance, decimal? legacyActual) =>
        ResolveAsync(reportingPeriodPublicId, actualPerformance, legacyActual, query => query.Where(item => item.OpmsTargetId == targetId));

    public Task<SubmissionValueResolution> ResolveIpmsAsync(string targetId, Guid reportingPeriodPublicId, string? actualPerformance, decimal? legacyActual) =>
        ResolveAsync(reportingPeriodPublicId, actualPerformance, legacyActual, query => query.Where(item => item.IpmsTargetId == targetId));

    private async Task<SubmissionValueResolution> ResolveAsync(
        Guid reportingPeriodPublicId,
        string? actualPerformance,
        decimal? legacyActual,
        Func<IQueryable<PerformancePeriodTarget>, IQueryable<PerformancePeriodTarget>> targetFilter)
    {
        var target = await targetFilter(context.PerformancePeriodTargets
                .Include(item => item.ReportingPeriod)
                .Include(item => item.RatioDefinition!).ThenInclude(item => item.RatioComparisonMethod)
                .Include(item => item.RatioDefinition!).ThenInclude(item => item.Segments))
            .SingleOrDefaultAsync(item => item.ReportingPeriod.PublicId == reportingPeriodPublicId
                && item.ReportingPeriod.IsActive && item.IsActive)
            ?? throw new InvalidOperationException("An authoritative KPI target value is not configured for this reporting period.");
        var period = target.ReportingPeriod;

        var input = !string.IsNullOrWhiteSpace(actualPerformance)
            ? actualPerformance
            : legacyActual?.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(input)) return new(period, target, null);
        var effectiveUnit = PerformanceRevisionResolver.EffectiveUnitKind(target);
        if (effectiveUnit == PerformanceUnitKind.Ratios)
        {
            if (target.RatioDefinition is not { IsActive: true })
                throw new InvalidOperationException("An active ratio definition is required for this reporting-period target.");
            var normalized = unitEngine.Normalize(effectiveUnit, input);
            if (!normalized.IsValid) throw new InvalidOperationException(normalized.Error);
            var ratioError = PerformanceRatioRules.ValidateCanonicalValue(normalized.CanonicalValue!, target.RatioDefinition);
            if (ratioError != null) throw new InvalidOperationException(ratioError);
        }
        var calculation = unitEngine.Calculate(
            effectiveUnit,
            PerformanceRevisionResolver.EffectiveTargetValue(target),
            input,
            target.Direction);
        return new(period, target, calculation);
    }

}

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
        var target = await targetFilter(context.PerformancePeriodTargets.Include(item => item.ReportingPeriod))
            .SingleOrDefaultAsync(item => item.ReportingPeriod.PublicId == reportingPeriodPublicId
                && item.ReportingPeriod.IsActive && item.IsActive)
            ?? throw new InvalidOperationException("An authoritative KPI target value is not configured for this reporting period.");
        var period = target.ReportingPeriod;

        var input = !string.IsNullOrWhiteSpace(actualPerformance)
            ? actualPerformance
            : legacyActual?.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(input)) return new(period, target, null);
        return new(period, target, unitEngine.Calculate(
            PerformanceRevisionResolver.EffectiveUnitKind(target),
            PerformanceRevisionResolver.EffectiveTargetValue(target),
            input,
            target.Direction));
    }

}

using System.Globalization;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Application.Services;

public sealed record SubmissionValueResolution(ReportingPeriod Period, PerformancePeriodTarget Target, PerformanceCalculationResult? Calculation);

public interface ISubmissionValueService
{
    Task<SubmissionValueResolution> ResolveOpmsAsync(string targetId, string periodCode, string? actualPerformance, decimal? legacyActual);
    Task<SubmissionValueResolution> ResolveIpmsAsync(string targetId, string periodCode, string? actualPerformance, decimal? legacyActual);
}

public sealed class SubmissionValueService(ApplicationDbContext context, IPerformanceUnitEngine unitEngine) : ISubmissionValueService
{
    public Task<SubmissionValueResolution> ResolveOpmsAsync(string targetId, string periodCode, string? actualPerformance, decimal? legacyActual) =>
        ResolveAsync(periodCode, actualPerformance, legacyActual, query => query.Where(item => item.OpmsTargetId == targetId));

    public Task<SubmissionValueResolution> ResolveIpmsAsync(string targetId, string periodCode, string? actualPerformance, decimal? legacyActual) =>
        ResolveAsync(periodCode, actualPerformance, legacyActual, query => query.Where(item => item.IpmsTargetId == targetId));

    private async Task<SubmissionValueResolution> ResolveAsync(
        string periodCode,
        string? actualPerformance,
        decimal? legacyActual,
        Func<IQueryable<PerformancePeriodTarget>, IQueryable<PerformancePeriodTarget>> targetFilter)
    {
        var normalizedCode = NormalizePeriodCode(periodCode);
        var period = await context.ReportingPeriods
            .Include(item => item.MunicipalityFinancialYear)
            .Where(item => item.IsActive && (item.Code.ToUpper() == normalizedCode || item.Name.ToUpper() == periodCode.Trim().ToUpper()))
            .OrderByDescending(item => item.MunicipalityFinancialYear.IsCurrent)
            .ThenByDescending(item => item.MunicipalityFinancialYear.FinancialYearId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("The reporting period is not configured for the selected municipality.");

        var target = await targetFilter(context.PerformancePeriodTargets)
            .SingleOrDefaultAsync(item => item.ReportingPeriodId == period.Id && item.IsActive)
            ?? throw new InvalidOperationException("An authoritative KPI target value is not configured for this reporting period.");

        var input = !string.IsNullOrWhiteSpace(actualPerformance)
            ? actualPerformance
            : legacyActual?.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(input)) return new(period, target, null);
        return new(period, target, unitEngine.Calculate(target.UnitKind, target.TargetValue, input, target.Direction));
    }

    private static string NormalizePeriodCode(string value)
    {
        var normalized = value.Trim().Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        return normalized switch
        {
            "MIDYEAR" or "MIDTERM" => "MID",
            "ANNUAL" or "YEAR" => "ANNUAL",
            _ => normalized
        };
    }
}

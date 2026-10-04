using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class OfficialReportJobHealthCheck(ApplicationDbContext context, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var failed = await context.OfficialReportJobs.IgnoreQueryFilters().CountAsync(item => item.State == OfficialReportJobState.Failed, cancellationToken);
            if (failed > 0) return HealthCheckResult.Degraded("Official report jobs require operator attention.", data: new Dictionary<string, object> { ["failedJobs"] = failed });
            var oldest = await context.OfficialReportJobs.IgnoreQueryFilters()
                .Where(item => item.State == OfficialReportJobState.Queued || item.State == OfficialReportJobState.RetryPending)
                .MinAsync(item => (DateTime?)item.AvailableAt, cancellationToken);
            var maximumAgeMinutes = Math.Clamp(configuration.GetValue("OfficialReports:DegradedAfterMinutes", 15), 1, 1440);
            if (oldest.HasValue && oldest < DateTime.UtcNow.AddMinutes(-maximumAgeMinutes))
                return HealthCheckResult.Degraded("Official report job processing is delayed.", data: new Dictionary<string, object> { ["oldestAvailableAt"] = oldest.Value });
            return HealthCheckResult.Healthy("Official report job processing is current.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Official report job health check failed.", exception);
        }
    }
}

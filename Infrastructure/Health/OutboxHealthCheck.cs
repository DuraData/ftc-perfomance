using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class OutboxHealthCheck(ApplicationDbContext context, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var failed = await context.BusinessEventOutbox.IgnoreQueryFilters().CountAsync(item => item.ProcessedAt == null && item.AttemptCount >= 10, cancellationToken);
            if (failed > 0) return HealthCheckResult.Unhealthy("Outbox contains events that exhausted retries.", data: new Dictionary<string, object> { ["failedEvents"] = failed });
            var oldest = await context.BusinessEventOutbox.IgnoreQueryFilters().Where(item => item.ProcessedAt == null).MinAsync(item => (DateTime?)item.OccurredAt, cancellationToken);
            var maximumAgeMinutes = Math.Clamp(configuration.GetValue("Outbox:DegradedAfterMinutes", 10), 1, 1440);
            if (oldest.HasValue && oldest.Value < now.AddMinutes(-maximumAgeMinutes))
                return HealthCheckResult.Degraded("Outbox processing is delayed.", data: new Dictionary<string, object> { ["oldestPendingAt"] = oldest.Value });
            return HealthCheckResult.Healthy("Outbox processing is current.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Outbox health check failed.", exception);
        }
    }
}

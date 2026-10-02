using FTCERP.Host.Infrastructure.Observability;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class OperationalTelemetryHealthCheck(OperationalTelemetry telemetry, IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = telemetry.Snapshot();
        var minimumRequests = Math.Clamp(configuration.GetValue("Observability:Alerts:MinimumRequests", 20), 1, 100_000);
        var failureRatePercent = snapshot.RequestsCompleted == 0 ? 0 : snapshot.RequestsFailed * 100d / snapshot.RequestsCompleted;
        var failureThreshold = Math.Clamp(configuration.GetValue("Observability:Alerts:FailureRatePercent", 10d), 0.1d, 100d);
        var averageDurationThreshold = Math.Clamp(configuration.GetValue("Observability:Alerts:AverageDurationMilliseconds", 2_000d), 1d, 300_000d);
        var data = new Dictionary<string, object>
        {
            ["requestsCompleted"] = snapshot.RequestsCompleted,
            ["requestsFailed"] = snapshot.RequestsFailed,
            ["requestsActive"] = snapshot.RequestsActive,
            ["failureRatePercent"] = Math.Round(failureRatePercent, 2),
            ["averageDurationMilliseconds"] = Math.Round(snapshot.AverageDurationMilliseconds, 2)
        };

        if (snapshot.RequestsCompleted >= minimumRequests && failureRatePercent >= failureThreshold)
            return Task.FromResult(HealthCheckResult.Degraded("HTTP failure-rate alert threshold exceeded.", data: data));
        if (snapshot.RequestsCompleted >= minimumRequests && snapshot.AverageDurationMilliseconds >= averageDurationThreshold)
            return Task.FromResult(HealthCheckResult.Degraded("HTTP average-duration alert threshold exceeded.", data: data));
        return Task.FromResult(HealthCheckResult.Healthy("Operational request telemetry is within configured thresholds.", data));
    }
}

using FTCERP.Host.Infrastructure.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class NotificationChannelHealthCheck(IConfiguration configuration, IEnumerable<INotificationChannelSender> senders) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var configured = (configuration["Notifications:Channels"] ?? "IN_APP").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(item => item.ToUpperInvariant()).Distinct().ToArray();
        var registered = senders.Select(item => item.Channel).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = configured.Where(item => item != "IN_APP" && !registered.Contains(item)).ToArray();
        if (missing.Length > 0) return Task.FromResult(HealthCheckResult.Unhealthy($"No sender is registered for: {string.Join(", ", missing)}."));
        if (configured.Contains("EMAIL") && !Uri.TryCreate(configuration["Notifications:Email:Endpoint"], UriKind.Absolute, out _))
            return Task.FromResult(HealthCheckResult.Unhealthy("Email notifications are enabled but the provider endpoint is not configured."));
        return Task.FromResult(HealthCheckResult.Healthy($"Configured notification channels: {string.Join(", ", configured)}."));
    }
}

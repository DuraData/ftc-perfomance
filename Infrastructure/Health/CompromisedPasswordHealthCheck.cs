using FTCERP.Host.Infrastructure.Auth;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class CompromisedPasswordHealthCheck(
    IConfiguration configuration,
    ICompromisedPasswordLookup lookup) : IHealthCheck
{
    private const string ProbePassword = "OPMS-Readiness-Probe-Not-A-User-Password-9f3d7a!";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Authentication:PasswordProtection:CompromisedCheckEnabled", false))
            return HealthCheckResult.Healthy("External compromised-password screening is disabled; the local denylist remains active.");
        if (!configuration.GetValue("Authentication:PasswordProtection:FailClosed", false))
            return HealthCheckResult.Degraded("External compromised-password screening is enabled without fail-closed enforcement.");

        var result = await lookup.CheckAsync(ProbePassword, cancellationToken);
        return result.Status == PasswordBreachStatus.Unavailable
            ? HealthCheckResult.Unhealthy("The compromised-password range service is unavailable.")
            : HealthCheckResult.Healthy("The compromised-password range service is available.");
    }
}

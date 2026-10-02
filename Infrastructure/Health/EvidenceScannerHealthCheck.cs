using FTCERP.Host.Infrastructure.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class EvidenceScannerHealthCheck(IEvidenceMalwareScanner scanner) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await scanner.CheckHealthAsync(cancellationToken);
        return result.Available ? HealthCheckResult.Healthy(result.Detail) : HealthCheckResult.Unhealthy(result.Detail);
    }
}

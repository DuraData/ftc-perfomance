using FTCERP.Host.Infrastructure.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public sealed class EvidenceStorageHealthCheck(IEvidenceBlobStorage storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await storage.CheckHealthAsync(cancellationToken);
        var data = new Dictionary<string, object> { ["provider"] = result.Provider };
        return result.Available
            ? HealthCheckResult.Healthy(result.Detail, data)
            : HealthCheckResult.Unhealthy(result.Detail, data: data);
    }
}

using FTCERP.Host.API.Responses;
using FTCERP.Host.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Authorize(Policy = "Permission:SYSTEM_OPERATIONS.READ")]
[Route("api/v1/system-operations")]
public sealed class SystemOperationsController(
    HealthCheckService healthChecks,
    OperationalTelemetry telemetry,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<SystemOperationsOverviewDto>>> GetOverview(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var snapshot = telemetry.Snapshot();
        var generatedAt = DateTime.UtcNow;
        var failureRate = snapshot.RequestsCompleted == 0
            ? 0
            : snapshot.RequestsFailed * 100d / snapshot.RequestsCompleted;

        var response = new SystemOperationsOverviewDto(
            generatedAt,
            snapshot.StartedAt,
            Math.Max(0, (long)(generatedAt - snapshot.StartedAt).TotalSeconds),
            report.Status.ToString(),
            Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            report.Entries.OrderBy(item => item.Key).Select(item => new SystemHealthCheckDto(
                item.Key,
                item.Value.Status.ToString(),
                Math.Round(item.Value.Duration.TotalMilliseconds, 2))).ToArray(),
            new SystemRequestTelemetryDto(
                snapshot.RequestsStarted,
                snapshot.RequestsCompleted,
                snapshot.RequestsFailed,
                snapshot.RequestsActive,
                Math.Round(failureRate, 2),
                Math.Round(snapshot.AverageDurationMilliseconds, 2)),
            new SystemAlertThresholdsDto(
                Math.Clamp(configuration.GetValue("Observability:Alerts:MinimumRequests", 20), 1, 100_000),
                Math.Clamp(configuration.GetValue("Observability:Alerts:FailureRatePercent", 10d), 0.1d, 100d),
                Math.Clamp(configuration.GetValue("Observability:Alerts:AverageDurationMilliseconds", 2_000d), 1d, 300_000d)));

        return Ok(new ApiResponse<SystemOperationsOverviewDto>(true, response));
    }
}

public sealed record SystemOperationsOverviewDto(
    DateTime GeneratedAt,
    DateTime StartedAt,
    long UptimeSeconds,
    string Status,
    double TotalHealthCheckDurationMilliseconds,
    SystemHealthCheckDto[] Checks,
    SystemRequestTelemetryDto Requests,
    SystemAlertThresholdsDto AlertThresholds);

public sealed record SystemHealthCheckDto(string Name, string Status, double DurationMilliseconds);
public sealed record SystemRequestTelemetryDto(
    long Started,
    long Completed,
    long Failed,
    long Active,
    double FailureRatePercent,
    double AverageDurationMilliseconds);
public sealed record SystemAlertThresholdsDto(
    int MinimumRequests,
    double FailureRatePercent,
    double AverageDurationMilliseconds);

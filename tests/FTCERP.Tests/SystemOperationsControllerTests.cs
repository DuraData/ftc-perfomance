using System.Text.Json;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Tests;

public sealed class SystemOperationsControllerTests
{
    [Fact]
    public async Task Overview_RequiresDedicatedPermissionAndExcludesSensitiveHealthDetails()
    {
        var authorization = typeof(SystemOperationsController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        authorization.Policy.Should().Be("Permission:SYSTEM_OPERATIONS.READ");

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(
                HealthStatus.Healthy,
                "Server=private;Password=secret",
                TimeSpan.FromMilliseconds(3),
                null,
                new Dictionary<string, object> { ["connectionString"] = "sensitive" }),
            ["outbox"] = new(
                HealthStatus.Degraded,
                "private queue detail",
                TimeSpan.FromMilliseconds(7),
                null,
                new Dictionary<string, object> { ["lastError"] = "sensitive" })
        };
        using var telemetry = new OperationalTelemetry();
        telemetry.RequestStarted("GET");
        telemetry.RequestCompleted("GET", 500, TimeSpan.FromMilliseconds(25), null);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:Alerts:MinimumRequests"] = "5",
            ["Observability:Alerts:FailureRatePercent"] = "12.5",
            ["Observability:Alerts:AverageDurationMilliseconds"] = "750"
        }).Build();
        var controller = new SystemOperationsController(
            new StubHealthCheckService(new HealthReport(entries, TimeSpan.FromMilliseconds(11))),
            telemetry,
            configuration,
            new StubOperationalAlertStore());

        var action = await controller.GetOverview(CancellationToken.None);

        var response = action.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<SystemOperationsOverviewDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be("Degraded");
        response.Data.Checks.Select(item => item.Name).Should().Equal("database", "outbox");
        response.Data.Requests.FailureRatePercent.Should().Be(100);
        response.Data.AlertThresholds.Should().Be(new SystemAlertThresholdsDto(5, 12.5, 750));
        response.Data.OperationalAlerts.HistoryAvailable.Should().BeTrue();
        response.Data.OperationalAlerts.Items.Should().ContainSingle(item => item.Lifecycle == "Raised");

        var json = JsonSerializer.Serialize(response);
        json.Should().NotContain("Server=private").And.NotContain("Password=secret")
            .And.NotContain("private queue detail").And.NotContain("lastError");
    }

    private sealed class StubHealthCheckService(HealthReport report) : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default) => Task.FromResult(report);
    }

    private sealed class StubOperationalAlertStore : IOperationalAlertStore
    {
        public Task ObserveAsync(HealthReport report, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<OperationalAlertHistory> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default) => Task.FromResult(
            new OperationalAlertHistory(
                true,
                new OperationalAlertDeliveryConfiguration(true, 60, true, ["IN_APP"]),
                [new OperationalAlertHistoryItem(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Raised", "Degraded", ["outbox"], DateTime.UtcNow, 1, 1, 0, 0)]));
    }
}

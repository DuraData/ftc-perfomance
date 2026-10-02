using FTCERP.Host.Infrastructure.Health;
using FTCERP.Host.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace FTCERP.Tests;

public sealed class OperationalReadinessTests
{
    [Fact]
    public async Task RequestTelemetry_RecordsCompletionFailureAndClearsActiveGauge()
    {
        using var telemetry = new OperationalTelemetry();
        var successContext = new DefaultHttpContext();
        successContext.Request.Method = "GET";
        successContext.Response.StatusCode = StatusCodes.Status204NoContent;
        var success = new RequestTelemetryMiddleware(_ => Task.CompletedTask, telemetry, NullLogger<RequestTelemetryMiddleware>.Instance);

        await success.InvokeAsync(successContext);

        var failureContext = new DefaultHttpContext();
        failureContext.Request.Method = "POST";
        var failure = new RequestTelemetryMiddleware(_ => throw new InvalidOperationException("test failure"), telemetry, NullLogger<RequestTelemetryMiddleware>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failure.InvokeAsync(failureContext));

        var snapshot = telemetry.Snapshot();
        snapshot.RequestsStarted.Should().Be(2);
        snapshot.RequestsCompleted.Should().Be(2);
        snapshot.RequestsFailed.Should().Be(1);
        snapshot.RequestsActive.Should().Be(0);
        snapshot.AverageDurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task OperationalHealthCheck_DegradesWhenConfiguredFailureThresholdIsExceeded()
    {
        using var telemetry = new OperationalTelemetry();
        telemetry.RequestStarted("GET");
        telemetry.RequestCompleted("GET", 500, TimeSpan.FromMilliseconds(5), null);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:Alerts:MinimumRequests"] = "1",
            ["Observability:Alerts:FailureRatePercent"] = "50",
            ["Observability:Alerts:AverageDurationMilliseconds"] = "1000"
        }).Build();
        var check = new OperationalTelemetryHealthCheck(telemetry, configuration);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data["failureRatePercent"].Should().Be(100d);
    }

    [Fact]
    public async Task SecurityHeaders_AreAppliedWithoutWeakeningApplicationResponses()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Contain("frame-ancestors 'none'");
        context.Response.Headers["Permissions-Policy"].ToString().Should().Contain("camera=()");
    }

    [Fact]
    public async Task ReadinessResponse_ExposesMachineReadableStatusWithoutSensitiveDetails()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(HealthStatus.Healthy, "private database detail", TimeSpan.FromMilliseconds(3), null, new Dictionary<string, object> { ["secret"] = "hidden" })
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(4));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await HealthResponseWriter.WriteAsync(context, report);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        body.Should().Contain("\"status\":\"Healthy\"");
        body.Should().Contain("\"name\":\"database\"");
        body.Should().NotContain("private database detail");
        body.Should().NotContain("hidden");
    }
}

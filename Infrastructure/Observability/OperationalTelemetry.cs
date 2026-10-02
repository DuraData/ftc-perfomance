using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FTCERP.Host.Infrastructure.Observability;

public sealed record OperationalTelemetrySnapshot(
    DateTime StartedAt,
    long RequestsStarted,
    long RequestsCompleted,
    long RequestsFailed,
    long RequestsActive,
    double AverageDurationMilliseconds);

public sealed class OperationalTelemetry : IDisposable
{
    public const string MeterName = "FTCERP.Host";
    public const string ActivitySourceName = "FTCERP.Host.Operations";

    private readonly DateTime _startedAt = DateTime.UtcNow;
    private readonly Meter _meter = new(MeterName);
    private readonly ActivitySource _activitySource = new(ActivitySourceName);
    private readonly Counter<long> _startedCounter;
    private readonly Counter<long> _completedCounter;
    private readonly Counter<long> _failedCounter;
    private readonly Histogram<double> _durationHistogram;
    private long _started;
    private long _completed;
    private long _failed;
    private long _active;
    private long _durationTicks;

    public OperationalTelemetry()
    {
        _startedCounter = _meter.CreateCounter<long>("opms.http.server.requests.started", unit: "{request}");
        _completedCounter = _meter.CreateCounter<long>("opms.http.server.requests.completed", unit: "{request}");
        _failedCounter = _meter.CreateCounter<long>("opms.http.server.requests.failed", unit: "{request}");
        _durationHistogram = _meter.CreateHistogram<double>("opms.http.server.request.duration", unit: "ms");
        _meter.CreateObservableGauge("opms.http.server.requests.active", () => Interlocked.Read(ref _active), unit: "{request}");
    }

    public Activity? StartRequestActivity(HttpContext context)
    {
        var activity = _activitySource.StartActivity("opms.request", ActivityKind.Internal);
        activity?.SetTag("http.request.method", context.Request.Method);
        activity?.SetTag("opms.correlation_id", context.TraceIdentifier);
        return activity;
    }

    public long RequestStarted(string method)
    {
        var active = Interlocked.Increment(ref _active);
        Interlocked.Increment(ref _started);
        _startedCounter.Add(1, new KeyValuePair<string, object?>("http.request.method", method));
        return active;
    }

    public void RequestCompleted(string method, int statusCode, TimeSpan duration, Exception? exception)
    {
        var failed = exception != null || statusCode >= 500;
        var statusClass = $"{Math.Clamp(statusCode / 100, 1, 5)}xx";
        Interlocked.Decrement(ref _active);
        Interlocked.Increment(ref _completed);
        Interlocked.Add(ref _durationTicks, duration.Ticks);
        _completedCounter.Add(1,
            new KeyValuePair<string, object?>("http.request.method", method),
            new KeyValuePair<string, object?>("http.response.status_class", statusClass));
        _durationHistogram.Record(duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("http.request.method", method),
            new KeyValuePair<string, object?>("http.response.status_class", statusClass));
        if (failed)
        {
            Interlocked.Increment(ref _failed);
            _failedCounter.Add(1,
                new KeyValuePair<string, object?>("http.request.method", method),
                new KeyValuePair<string, object?>("http.response.status_class", statusClass));
        }
    }

    public OperationalTelemetrySnapshot Snapshot()
    {
        var completed = Interlocked.Read(ref _completed);
        var duration = TimeSpan.FromTicks(Interlocked.Read(ref _durationTicks));
        return new OperationalTelemetrySnapshot(
            _startedAt,
            Interlocked.Read(ref _started),
            completed,
            Interlocked.Read(ref _failed),
            Interlocked.Read(ref _active),
            completed == 0 ? 0 : duration.TotalMilliseconds / completed);
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }
}

public sealed class RequestTelemetryMiddleware(RequestDelegate next, OperationalTelemetry telemetry, ILogger<RequestTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        telemetry.RequestStarted(context.Request.Method);
        var started = Stopwatch.GetTimestamp();
        Exception? failure = null;
        using var activity = telemetry.StartRequestActivity(context);
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            failure = exception;
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw;
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(started);
            activity?.SetTag("http.response.status_code", context.Response.StatusCode);
            telemetry.RequestCompleted(context.Request.Method, context.Response.StatusCode, duration, failure);
            logger.LogInformation(
                "HTTP request completed {RequestMethod} {StatusCode} in {ElapsedMilliseconds:F2} ms",
                context.Request.Method,
                failure == null ? context.Response.StatusCode : StatusCodes.Status500InternalServerError,
                duration.TotalMilliseconds);
        }
    }
}

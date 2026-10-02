using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Health;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.OrderBy(item => item.Key).Select(item => new
            {
                name = item.Key,
                status = item.Value.Status.ToString(),
                durationMilliseconds = Math.Round(item.Value.Duration.TotalMilliseconds, 2)
            })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

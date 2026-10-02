using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.Infrastructure.Security;

public sealed class CsrfProtectionMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-OPMS-Request";
    public const string HeaderValue = "same-origin";
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api")
            || SafeMethods.Contains(context.Request.Method)
            || HasBearerToken(context.Request)
            || string.Equals(context.Request.Headers[HeaderName], HeaderValue, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Cross-site request protection failed",
            Detail = $"State-changing cookie-authenticated API requests must include {HeaderName}.",
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = context.TraceIdentifier }
        });
    }

    private static bool HasBearerToken(HttpRequest request) =>
        request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
}

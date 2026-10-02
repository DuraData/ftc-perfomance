using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.Infrastructure.Security;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValid(supplied) ? supplied! : Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 100 && value.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.');
}

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", ApiProblemCodes.AccessDenied),
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent update conflict", ApiProblemCodes.StateConflict),
            ApiValidationException => (StatusCodes.Status400BadRequest, "Invalid request", ApiProblemCodes.BadRequest),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error", ApiProblemCodes.InternalError)
        };
        logger.LogError(exception, "Request failed with status {StatusCode}", status);
        await ApiProblemDetails.WriteAsync(context, status,
            status == StatusCodes.Status500InternalServerError ? "The request could not be completed." : exception.Message,
            code, title: title);
        return true;
    }
}

public sealed class ApiValidationException(string message) : Exception(message);

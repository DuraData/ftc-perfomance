using System.Text.Json;
using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace FTCERP.Host.Infrastructure.Security;

public static class ApiProblemCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BadRequest = "BAD_REQUEST";
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string AccessDenied = "ACCESS_DENIED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ResourceRetired = "RESOURCE_RETIRED";
    public const string StateConflict = "STATE_CONFLICT";
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string InternalError = "INTERNAL_ERROR";
}

public static class ApiProblemDetails
{
    public static IActionResult InvalidModelStateResponse(ActionContext context)
    {
        var errors = context.ModelState.Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(item => item.Key, item => item.Value!.Errors
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The supplied value is invalid." : error.ErrorMessage)
                .ToArray());
        var result = new BadRequestObjectResult(Create(context.HttpContext, StatusCodes.Status400BadRequest,
            "One or more validation errors occurred.", ApiProblemCodes.ValidationFailed, errors));
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    public static ProblemDetails Create(HttpContext context, int status, string? detail = null, string? code = null,
        IDictionary<string, string[]>? errors = null, string? title = null)
    {
        ProblemDetails problem = errors is { Count: > 0 }
            ? new ValidationProblemDetails(errors)
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title ?? ReasonPhrases.GetReasonPhrase(status);
        problem.Detail = detail;
        problem.Instance = context.Request.Path;
        ApplyDefaults(problem, context, code ?? DefaultCode(status, errors is { Count: > 0 }));
        return problem;
    }

    public static void ApplyDefaults(ProblemDetails problem, HttpContext context, string? code = null)
    {
        problem.Status ??= context.Response.StatusCode is >= 400 and <= 599
            ? context.Response.StatusCode
            : StatusCodes.Status500InternalServerError;
        problem.Title ??= ReasonPhrases.GetReasonPhrase(problem.Status.Value);
        problem.Instance ??= context.Request.Path;
        if (code != null || !problem.Extensions.ContainsKey("code"))
            problem.Extensions["code"] = code ?? DefaultCode(problem.Status.Value, problem is ValidationProblemDetails);
        problem.Extensions["correlationId"] = context.TraceIdentifier;
    }

    public static async Task WriteAsync(HttpContext context, int status, string detail, string? code = null,
        IDictionary<string, string[]>? errors = null, string? title = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = Create(context, status, detail, code, errors, title);
        var service = context.RequestServices is { } services ? services.GetService<IProblemDetailsService>() : null;
        if (service != null && await service.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem })) return;
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, problem.GetType(), cancellationToken: context.RequestAborted);
    }

    public static string DefaultCode(int status, bool validation = false) => validation ? ApiProblemCodes.ValidationFailed : status switch
    {
        StatusCodes.Status400BadRequest => ApiProblemCodes.BadRequest,
        StatusCodes.Status401Unauthorized => ApiProblemCodes.AuthenticationRequired,
        StatusCodes.Status403Forbidden => ApiProblemCodes.AccessDenied,
        StatusCodes.Status404NotFound => ApiProblemCodes.ResourceNotFound,
        StatusCodes.Status409Conflict => ApiProblemCodes.StateConflict,
        StatusCodes.Status410Gone => ApiProblemCodes.ResourceRetired,
        StatusCodes.Status413PayloadTooLarge => ApiProblemCodes.PayloadTooLarge,
        StatusCodes.Status415UnsupportedMediaType => ApiProblemCodes.UnsupportedMediaType,
        StatusCodes.Status422UnprocessableEntity => ApiProblemCodes.BusinessRuleViolation,
        StatusCodes.Status429TooManyRequests => ApiProblemCodes.RateLimitExceeded,
        _ when status >= 500 => ApiProblemCodes.InternalError,
        _ => "REQUEST_FAILED"
    };
}

public sealed class ApiFailureProblemDetailsFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: IApiResponse response } result || response.Success) return;
        var status = result.StatusCode ?? context.HttpContext.Response.StatusCode;
        if (status < 400) status = StatusCodes.Status400BadRequest;
        var errors = response.Errors is { Length: > 0 }
            ? new Dictionary<string, string[]> { ["request"] = response.Errors }
            : null;
        result.Value = ApiProblemDetails.Create(context.HttpContext, status, response.Message, errors: errors);
        result.DeclaredType = null;
        result.StatusCode = status;
        result.ContentTypes.Clear();
        result.ContentTypes.Add("application/problem+json");
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}

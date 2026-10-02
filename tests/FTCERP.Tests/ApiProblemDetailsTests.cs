using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FTCERP.Tests;

public sealed class ApiProblemDetailsTests
{
    [Fact]
    public void Failure_filter_converts_legacy_controller_envelope_to_validation_problem_details()
    {
        var http = Context();
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var original = new BadRequestObjectResult(new ApiResponse<object>(false, null, "Request was invalid.", ["Name is required."]));
        var executing = new ResultExecutingContext(action, [], original, new object());

        new ApiFailureProblemDetailsFilter().OnResultExecuting(executing);

        var result = executing.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = result.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Be("Request was invalid.");
        problem.Errors["request"].Should().ContainSingle("Name is required.");
        problem.Extensions["code"].Should().Be(ApiProblemCodes.ValidationFailed);
        problem.Extensions["correlationId"].Should().Be("problem-test");
        result.ContentTypes.Should().ContainSingle("application/problem+json");
    }

    [Fact]
    public void Failure_filter_preserves_success_responses()
    {
        var http = Context();
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var original = new OkObjectResult(new ApiResponse<string>(true, "value"));
        var executing = new ResultExecutingContext(action, [], original, new object());

        new ApiFailureProblemDetailsFilter().OnResultExecuting(executing);

        executing.Result.Should().BeSameAs(original);
        original.Value.Should().BeOfType<ApiResponse<string>>().Which.Data.Should().Be("value");
    }

    [Fact]
    public void Invalid_model_state_uses_field_keyed_validation_problem_details()
    {
        var action = new ActionContext(Context(), new RouteData(), new ActionDescriptor());
        action.ModelState.AddModelError("reason", "Reason is required.");

        var result = ApiProblemDetails.InvalidModelStateResponse(action);

        var problem = result.Should().BeOfType<BadRequestObjectResult>().Which.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors["reason"].Should().ContainSingle("Reason is required.");
        problem.Extensions["code"].Should().Be(ApiProblemCodes.ValidationFailed);
    }

    [Theory]
    [InlineData(400, ApiProblemCodes.BadRequest)]
    [InlineData(401, ApiProblemCodes.AuthenticationRequired)]
    [InlineData(403, ApiProblemCodes.AccessDenied)]
    [InlineData(404, ApiProblemCodes.ResourceNotFound)]
    [InlineData(409, ApiProblemCodes.StateConflict)]
    [InlineData(413, ApiProblemCodes.PayloadTooLarge)]
    [InlineData(415, ApiProblemCodes.UnsupportedMediaType)]
    [InlineData(422, ApiProblemCodes.BusinessRuleViolation)]
    [InlineData(429, ApiProblemCodes.RateLimitExceeded)]
    public void Required_http_statuses_have_stable_problem_codes(int status, string expected)
    {
        ApiProblemDetails.DefaultCode(status).Should().Be(expected);
    }

    [Fact]
    public void Problem_customization_preserves_a_more_specific_middleware_code()
    {
        var problem = ApiProblemDetails.Create(Context(), 400, "Rejected.", "CSRF_VALIDATION_FAILED");

        ApiProblemDetails.ApplyDefaults(problem, Context());

        problem.Extensions["code"].Should().Be("CSRF_VALIDATION_FAILED");
    }

    [Fact]
    public async Task Middleware_problem_writer_emits_rfc7807_content_with_correlation()
    {
        var http = Context();

        await ApiProblemDetails.WriteAsync(http, StatusCodes.Status415UnsupportedMediaType, "Unsupported evidence type.");

        http.Response.ContentType.Should().StartWith("application/problem+json");
        http.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(http.Response.Body);
        document.RootElement.GetProperty("status").GetInt32().Should().Be(415);
        document.RootElement.GetProperty("code").GetString().Should().Be(ApiProblemCodes.UnsupportedMediaType);
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("problem-test");
        document.RootElement.GetProperty("detail").GetString().Should().Be("Unsupported evidence type.");
    }

    [Fact]
    public async Task Unexpected_argument_exceptions_are_sanitized_as_server_errors()
    {
        var http = Context();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(http, new ArgumentException("sensitive infrastructure detail"), default);

        handled.Should().BeTrue();
        http.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        http.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(http.Response.Body);
        document.RootElement.GetProperty("detail").GetString().Should().Be("The request could not be completed.");
        document.RootElement.GetProperty("code").GetString().Should().Be(ApiProblemCodes.InternalError);
        document.RootElement.GetRawText().Should().NotContain("sensitive infrastructure detail");
    }

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "problem-test",
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();
        return context;
    }
}

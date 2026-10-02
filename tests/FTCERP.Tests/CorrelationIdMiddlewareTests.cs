using Microsoft.Extensions.Logging.Abstractions;

namespace FTCERP.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task ValidCallerCorrelationId_IsPreservedAndReturned()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "request-123";
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(next: async http => await http.Response.WriteAsync("ok"), NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.Should().Be("request-123");
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be("request-123");
    }

    [Fact]
    public async Task UnsafeCallerCorrelationId_IsReplaced()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "bad value!";
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(next: async http => await http.Response.WriteAsync("ok"), NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.Should().NotBe("bad value!");
        context.TraceIdentifier.Should().NotBeNullOrWhiteSpace();
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(context.TraceIdentifier);
    }
}

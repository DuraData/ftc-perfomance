namespace FTCERP.Tests;

public sealed class CsrfProtectionTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task SafeApiMethods_DoNotRequireCustomHeader(string method)
    {
        var invoked = false;
        var context = Context(method);
        var middleware = new CsrfProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task CookieAuthenticatedMutation_WithoutSameOriginHeader_IsRejected()
    {
        var invoked = false;
        var context = Context("POST");
        context.Request.Headers.Cookie = "opms_access=opaque";
        var middleware = new CsrfProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        invoked.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.Body.Position = 0;
        (await new StreamReader(context.Response.Body).ReadToEndAsync()).Should().Contain(CsrfProtectionMiddleware.HeaderName);
    }

    [Fact]
    public async Task SameOriginHeader_AllowsCookieAuthenticatedMutation()
    {
        var invoked = false;
        var context = Context("PATCH");
        context.Request.Headers[CsrfProtectionMiddleware.HeaderName] = CsrfProtectionMiddleware.HeaderValue;
        var middleware = new CsrfProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task BearerApiClient_RemainsCompatibleWithoutBrowserHeader()
    {
        var invoked = false;
        var context = Context("DELETE");
        context.Request.Headers.Authorization = "Bearer external-client-token";
        var middleware = new CsrfProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task NonApiMutation_IsNotIntercepted()
    {
        var invoked = false;
        var context = Context("POST", "/client-route");
        var middleware = new CsrfProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        invoked.Should().BeTrue();
    }

    private static DefaultHttpContext Context(string method, string path = "/api/v1/records")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}

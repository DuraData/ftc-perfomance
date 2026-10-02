using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FTCERP.Tests;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task Completed_post_is_replayed_without_executing_the_mutation_twice()
    {
        await using var fixture = await Fixture.CreateAsync();
        var executions = 0;
        var middleware = fixture.Middleware(async context =>
        {
            executions++;
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Location = "/api/v1/items/created";
            await context.Response.WriteAsync("{\"success\":true,\"data\":\"created\"}");
        });

        var first = await fixture.InvokeAsync(middleware, "stable-key-001", "{\"name\":\"One\"}");
        var replay = await fixture.InvokeAsync(middleware, "stable-key-001", "{\"name\":\"One\"}");

        Assert.Equal(1, executions);
        Assert.Equal(StatusCodes.Status201Created, first.StatusCode);
        Assert.Equal(first.Body, replay.Body);
        Assert.Equal("true", replay.Headers[IdempotencyMiddleware.ReplayedHeaderName]);
        Assert.Equal("/api/v1/items/created", replay.Headers["Location"]);
        await using var scope = fixture.Services.CreateAsyncScope();
        var record = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().IdempotencyRequests.SingleAsync();
        Assert.Equal(IdempotencyRequestStates.Completed, record.State);
        Assert.Equal(64, record.RequestHash.Length);
        Assert.Equal(64, record.IdentityHash.Length);
    }

    [Fact]
    public async Task Reusing_a_key_with_different_body_or_query_is_rejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var executions = 0;
        var middleware = fixture.Middleware(async context =>
        {
            executions++;
            await context.Response.WriteAsJsonAsync(new ApiResponse<bool>(true, true));
        });

        await fixture.InvokeAsync(middleware, "stable-key-002", "{\"name\":\"One\"}", "?mode=create");
        var bodyConflict = await fixture.InvokeAsync(middleware, "stable-key-002", "{\"name\":\"Two\"}", "?mode=create");
        var queryConflict = await fixture.InvokeAsync(middleware, "stable-key-002", "{\"name\":\"One\"}", "?mode=replace");

        Assert.Equal(1, executions);
        Assert.Equal(StatusCodes.Status409Conflict, bodyConflict.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, queryConflict.StatusCode);
    }

    [Fact]
    public async Task Missing_or_unsafe_key_is_rejected_before_the_endpoint()
    {
        await using var fixture = await Fixture.CreateAsync();
        var executions = 0;
        var middleware = fixture.Middleware(_ => { executions++; return Task.CompletedTask; });

        var missing = await fixture.InvokeAsync(middleware, null, "{}");
        var unsafeKey = await fixture.InvokeAsync(middleware, "contains spaces", "{}");

        Assert.Equal(0, executions);
        Assert.Equal(StatusCodes.Status400BadRequest, missing.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, unsafeKey.StatusCode);
    }

    [Fact]
    public async Task Non_system_mutation_requires_a_resolved_municipality()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Tenant.MunicipalityIdValue = long.MinValue;
        var executions = 0;
        var middleware = fixture.Middleware(_ => { executions++; return Task.CompletedTask; });

        var response = await fixture.InvokeAsync(middleware, "tenant-key-required", "{}");

        Assert.Equal(0, executions);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Same_key_is_isolated_by_municipality()
    {
        await using var fixture = await Fixture.CreateAsync();
        var executions = 0;
        var middleware = fixture.Middleware(async context =>
        {
            executions++;
            await context.Response.WriteAsJsonAsync(new ApiResponse<int>(true, executions));
        });

        await fixture.InvokeAsync(middleware, "tenant-key-001", "{}");
        fixture.Tenant.MunicipalityIdValue = fixture.SecondMunicipalityId;
        await fixture.InvokeAsync(middleware, "tenant-key-001", "{}");

        Assert.Equal(2, executions);
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await context.IdempotencyRequests.ToArrayAsync());
        Assert.Equal(fixture.SecondMunicipalityId, (await context.IdempotencyRequests.SingleAsync()).MunicipalityId);
    }

    [Fact]
    public async Task Ambiguous_failure_is_recorded_and_same_key_cannot_duplicate_the_mutation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var executions = 0;
        var middleware = fixture.Middleware(_ =>
        {
            executions++;
            throw new InvalidOperationException("Failure after a possible commit");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.InvokeAsync(middleware, "failure-key-001", "{}"));
        var retry = await fixture.InvokeAsync(middleware, "failure-key-001", "{}");

        Assert.Equal(1, executions);
        Assert.Equal(StatusCodes.Status409Conflict, retry.StatusCode);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public ServiceProvider Services { get; }
        public MutableTenantContext Tenant { get; }
        public long SecondMunicipalityId { get; }

        private Fixture(SqliteConnection connection, ServiceProvider services, MutableTenantContext tenant, long secondMunicipalityId)
        {
            _connection = connection;
            Services = services;
            Tenant = tenant;
            SecondMunicipalityId = secondMunicipalityId;
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var tenant = new MutableTenantContext();
            var services = new ServiceCollection()
                .AddSingleton(connection)
                .AddSingleton<ITenantContext>(tenant)
                .AddDbContext<ApplicationDbContext>((provider, options) => options.UseSqlite(provider.GetRequiredService<SqliteConnection>()))
                .BuildServiceProvider();
            await using var scope = services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await database.Database.EnsureCreatedAsync();
            var municipalityA = new Municipality { Code = "IDEM-A", Name = "Idempotency A" };
            var municipalityB = new Municipality { Code = "IDEM-B", Name = "Idempotency B" };
            database.AddRange(municipalityA, municipalityB);
            await database.SaveChangesAsync();
            tenant.MunicipalityIdValue = municipalityA.Id;
            database.Users.Add(IdpTestFixture.CreateUser("idempotency-user"));
            await database.SaveChangesAsync();
            return new Fixture(connection, services, tenant, municipalityB.Id);
        }

        public IdempotencyMiddleware Middleware(RequestDelegate next) => new(
            next,
            Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdempotencyOptions { RetentionHours = 24, MaximumResponseBytes = 1024 * 1024 }),
            NullLogger<IdempotencyMiddleware>.Instance);

        public async Task<ResponseSnapshot> InvokeAsync(IdempotencyMiddleware middleware, string? key, string body, string query = "")
        {
            await using var requestScope = Services.CreateAsyncScope();
            var output = new MemoryStream();
            var context = new DefaultHttpContext
            {
                RequestServices = requestScope.ServiceProvider,
                User = IdpTestFixture.CreatePrincipal("idempotency-user")
            };
            context.TraceIdentifier = Guid.NewGuid().ToString("N");
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/api/v1/items";
            context.Request.QueryString = new QueryString(query);
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Response.Body = output;
            if (key != null) context.Request.Headers[IdempotencyMiddleware.HeaderName] = key;

            await middleware.InvokeAsync(context, Tenant);
            output.Position = 0;
            return new ResponseSnapshot(context.Response.StatusCode, await new StreamReader(output).ReadToEndAsync(),
                context.Response.Headers.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase));
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class MutableTenantContext : ITenantContext
    {
        public long? MunicipalityIdValue { get; set; }
        public long? MunicipalityId => MunicipalityIdValue;
        public bool IsSystem => false;
        public string? UserId => "idempotency-user";
    }

    private sealed record ResponseSnapshot(int StatusCode, string Body, IReadOnlyDictionary<string, string> Headers);
}

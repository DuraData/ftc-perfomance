using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FTCERP.Host.Infrastructure.Security;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";
    public int RetentionHours { get; set; } = 24;
    public int MaximumResponseBytes { get; set; } = 4 * 1024 * 1024;
    public int CleanupMinutes { get; set; } = 60;
}

public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    IOptions<IdempotencyOptions> configuredOptions,
    ILogger<IdempotencyMiddleware> logger)
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    private readonly IdempotencyOptions _options = new()
    {
        RetentionHours = Math.Clamp(configuredOptions.Value.RetentionHours, 1, 168),
        MaximumResponseBytes = Math.Clamp(configuredOptions.Value.MaximumResponseBytes, 64 * 1024, 16 * 1024 * 1024)
    };

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (!ShouldHandle(context))
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var key = context.Request.Headers[HeaderName].ToString().Trim();
        if (!IsValidKey(key))
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                $"{HeaderName} is required for authenticated API POST requests and must contain 8-128 safe characters.");
            return;
        }

        var bodyHash = await HashRequestAsync(context.Request, context.RequestAborted);
        var route = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (route.Length > 600)
        {
            await WriteProblemAsync(context, StatusCodes.Status414UriTooLong, "The request route is too long for idempotent processing.");
            return;
        }
        var municipalityId = tenantContext.MunicipalityId is > 0 ? tenantContext.MunicipalityId : null;
        if (!tenantContext.IsSystem && !municipalityId.HasValue)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "A valid municipality context is required for this mutation.");
            return;
        }
        var scopeKey = municipalityId.HasValue ? $"municipality:{municipalityId.Value}" : "system";
        var identity = new RequestIdentity(scopeKey, municipalityId, userId, context.Request.Method, route, key, bodyHash);

        var acquisition = await AcquireAsync(identity, context.TraceIdentifier, context.RequestAborted);
        if (acquisition.Kind == AcquisitionKind.Conflict)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "The idempotency key was already used with a different request payload.");
            return;
        }
        if (acquisition.Kind == AcquisitionKind.InProgress)
        {
            context.Response.Headers.RetryAfter = "1";
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "A request with this idempotency key is already being processed.");
            return;
        }
        if (acquisition.Kind == AcquisitionKind.Failed)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "The original request ended ambiguously and cannot be retried automatically with this key. Use a new key only after confirming the original operation did not complete.");
            return;
        }
        if (acquisition.Record is { State: IdempotencyRequestStates.Completed } completed)
        {
            await ReplayAsync(context, completed);
            return;
        }

        var originalBody = context.Response.Body;
        await using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await TryMarkFailedAsync(acquisition.Record!.PublicId, exception.GetType().Name);
            logger.LogError(exception, "Idempotent request {IdempotencyRequestId} failed after acquisition.", acquisition.Record.PublicId);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        try
        {
            if (capturedBody.Length > _options.MaximumResponseBytes)
                await MarkFailedAsync(acquisition.Record!.PublicId, "Response exceeded the replay limit.", context.RequestAborted);
            else
                await CompleteAsync(acquisition.Record!.PublicId, context.Response, capturedBody.ToArray(), context.RequestAborted);
        }
        catch (Exception exception)
        {
            await TryMarkFailedAsync(acquisition.Record!.PublicId, exception.GetType().Name);
            logger.LogError(exception, "Could not persist the idempotent response for request {IdempotencyRequestId}.", acquisition.Record.PublicId);
            throw;
        }

        capturedBody.Position = 0;
        await capturedBody.CopyToAsync(originalBody, context.RequestAborted);
    }

    private static bool ShouldHandle(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || context.User.Identity?.IsAuthenticated != true)
            return false;
        if (!context.Request.Path.StartsWithSegments("/api"))
            return false;
        return !context.Request.Path.StartsWithSegments("/api/auth")
            && !context.Request.Path.StartsWithSegments("/api/v1/auth");
    }

    private static bool IsValidKey(string value) => value.Length is >= 8 and <= 128
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');

    private static async Task<string> HashRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(request.QueryString.Value ?? string.Empty));
        hash.AppendData([0]);
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            hash.AppendData(buffer, 0, read);
        request.Body.Position = 0;
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private async Task<Acquisition> AcquireAsync(RequestIdentity identity, string correlationId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existing = await FindAsync(database, identity, cancellationToken);
        if (existing != null && existing.ExpiresAt <= DateTime.UtcNow)
        {
            database.IdempotencyRequests.Remove(existing);
            await database.SaveChangesAsync(cancellationToken);
            existing = null;
        }
        if (existing != null) return Classify(existing, identity.RequestHash);

        var record = new IdempotencyRequest
        {
            MunicipalityId = identity.MunicipalityId,
            ScopeKey = identity.ScopeKey,
            IdentityHash = HashIdentity(identity),
            UserId = identity.UserId,
            Method = identity.Method,
            Route = identity.Route,
            IdempotencyKey = identity.Key,
            RequestHash = identity.RequestHash,
            State = IdempotencyRequestStates.InProgress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(_options.RetentionHours),
            CorrelationId = correlationId
        };
        database.IdempotencyRequests.Add(record);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return new Acquisition(AcquisitionKind.Acquired, record);
        }
        catch (DbUpdateException)
        {
            database.ChangeTracker.Clear();
            existing = await FindAsync(database, identity, cancellationToken);
            if (existing == null) throw;
            return Classify(existing, identity.RequestHash);
        }
    }

    private static Task<IdempotencyRequest?> FindAsync(ApplicationDbContext database, RequestIdentity identity, CancellationToken cancellationToken) =>
        database.IdempotencyRequests.SingleOrDefaultAsync(item => item.IdentityHash == HashIdentity(identity), cancellationToken);

    private static string HashIdentity(RequestIdentity identity) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{identity.ScopeKey}\n{identity.UserId}\n{identity.Method}\n{identity.Route}\n{identity.Key}")));

    private static Acquisition Classify(IdempotencyRequest record, string requestHash)
    {
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(record.RequestHash), Convert.FromHexString(requestHash)))
            return new Acquisition(AcquisitionKind.Conflict, record);
        return record.State switch
        {
            IdempotencyRequestStates.Completed => new Acquisition(AcquisitionKind.Replay, record),
            IdempotencyRequestStates.Failed => new Acquisition(AcquisitionKind.Failed, record),
            _ => new Acquisition(AcquisitionKind.InProgress, record)
        };
    }

    private async Task CompleteAsync(Guid publicId, HttpResponse response, byte[] body, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var record = await database.IdempotencyRequests.SingleAsync(item => item.PublicId == publicId, cancellationToken);
        record.State = IdempotencyRequestStates.Completed;
        record.ResponseStatusCode = response.StatusCode;
        record.ResponseContentType = response.ContentType;
        record.ResponseLocation = response.Headers.Location.ToString().NullIfWhiteSpace();
        record.ResponseETag = response.Headers.ETag.ToString().NullIfWhiteSpace();
        record.ResponseBody = body;
        record.CompletedAt = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(Guid publicId, string detail, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var record = await database.IdempotencyRequests.SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);
        if (record == null) return;
        record.State = IdempotencyRequestStates.Failed;
        record.ResponseStatusCode = StatusCodes.Status500InternalServerError;
        record.ResponseContentType = "application/problem+json";
        record.ResponseBody = System.Text.Encoding.UTF8.GetBytes(detail);
        record.CompletedAt = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task TryMarkFailedAsync(Guid publicId, string detail)
    {
        try
        {
            await MarkFailedAsync(publicId, detail, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Could not mark idempotent request {IdempotencyRequestId} as failed.", publicId);
        }
    }

    private static async Task ReplayAsync(HttpContext context, IdempotencyRequest record)
    {
        context.Response.StatusCode = record.ResponseStatusCode ?? StatusCodes.Status200OK;
        if (!string.IsNullOrWhiteSpace(record.ResponseContentType)) context.Response.ContentType = record.ResponseContentType;
        if (!string.IsNullOrWhiteSpace(record.ResponseLocation)) context.Response.Headers.Location = record.ResponseLocation;
        if (!string.IsNullOrWhiteSpace(record.ResponseETag)) context.Response.Headers.ETag = record.ResponseETag;
        context.Response.Headers[ReplayedHeaderName] = "true";
        if (record.ResponseBody is { Length: > 0 })
            await context.Response.Body.WriteAsync(record.ResponseBody, context.RequestAborted);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = context.TraceIdentifier }
        }, cancellationToken: context.RequestAborted);
    }

    private sealed record RequestIdentity(string ScopeKey, long? MunicipalityId, string UserId, string Method, string Route, string Key, string RequestHash);
    private sealed record Acquisition(AcquisitionKind Kind, IdempotencyRequest? Record);
    private enum AcquisitionKind { Acquired, Replay, Conflict, InProgress, Failed }
}

internal static class IdempotencyStringExtensions
{
    public static string? NullIfWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public sealed class IdempotencyCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<IdempotencyOptions> configuredOptions,
    ILogger<IdempotencyCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Clamp(configuredOptions.Value.CleanupMinutes, 5, 1440));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var deleted = await database.IdempotencyRequests
                    .Where(item => item.ExpiresAt <= DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0) logger.LogInformation("Removed {Count} expired idempotency records.", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Expired idempotency records could not be removed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

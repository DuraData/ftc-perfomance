using System.Text;
using System.Security.Cryptography;

namespace FTCERP.Tests;

public sealed class IdempotencyRequestHistoryTests
{
    [Fact]
    public async Task Request_identity_and_retention_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, request) = Fixture("identity");
        context.AddRange(municipality, user, request);
        await context.SaveChangesAsync();

        request.Route = "/api/v1/forged";

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Idempotency request identity, ownership, request digest, retention and correlation evidence are immutable*");
    }

    [Fact]
    public async Task In_progress_request_rejects_incomplete_terminal_state()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, request) = Fixture("incomplete");
        context.AddRange(municipality, user, request);
        await context.SaveChangesAsync();

        request.State = IdempotencyRequestStates.Completed;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Idempotency request state may advance exactly once from in-progress to a complete persisted response*");
    }

    [Fact]
    public async Task Exact_completed_response_is_persisted_once_and_then_immutable()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, request) = Fixture("complete");
        context.AddRange(municipality, user, request);
        await context.SaveChangesAsync();

        request.State = IdempotencyRequestStates.Completed;
        request.ResponseStatusCode = StatusCodes.Status201Created;
        request.ResponseContentType = "application/json";
        request.ResponseLocation = "/api/v1/items/result";
        request.ResponseETag = "\"version-1\"";
        request.ResponseBody = Encoding.UTF8.GetBytes("{\"success\":true}");
        request.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        request.ResponseBody = Encoding.UTF8.GetBytes("{\"success\":false}");

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Idempotency request state may advance exactly once from in-progress to a complete persisted response*");
    }

    [Fact]
    public async Task Failed_request_requires_exact_problem_response()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, request) = Fixture("failed");
        context.AddRange(municipality, user, request);
        await context.SaveChangesAsync();

        request.State = IdempotencyRequestStates.Failed;
        request.ResponseStatusCode = StatusCodes.Status500InternalServerError;
        request.ResponseContentType = "text/plain";
        request.ResponseBody = Encoding.UTF8.GetBytes("ambiguous failure");
        request.CompletedAt = DateTime.UtcNow;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A failed idempotency request requires its exact terminal problem response evidence*");
    }

    [Fact]
    public async Task Active_request_cannot_be_deleted_but_expired_history_can_be_purged()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, active) = Fixture("active");
        var (_, _, expired) = Fixture("expired", municipality, user, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1));
        context.AddRange(municipality, user, active, expired);
        await context.SaveChangesAsync();

        context.IdempotencyRequests.Remove(active);
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Active idempotency replay history cannot be hard deleted before its retention period expires*");

        context.Entry(active).State = EntityState.Unchanged;
        context.IdempotencyRequests.Remove(expired);
        await context.SaveChangesAsync();

        (await context.IdempotencyRequests.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    private static (Municipality Municipality, ApplicationUser User, IdempotencyRequest Request) Fixture(
        string suffix,
        Municipality? municipality = null,
        ApplicationUser? user = null,
        DateTime? createdAt = null,
        DateTime? expiresAt = null)
    {
        municipality ??= new Municipality
        {
            Id = 999001,
            Code = $"IDEM-{suffix}-{Guid.NewGuid():N}"[..20],
            Name = $"Idempotency {suffix}",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        };
        user ??= new ApplicationUser
        {
            Id = $"idempotency-{suffix}",
            UserName = $"idempotency-{suffix}@example.test",
            NormalizedUserName = $"IDEMPOTENCY-{suffix.ToUpperInvariant()}@EXAMPLE.TEST",
            Email = $"idempotency-{suffix}@example.test",
            NormalizedEmail = $"IDEMPOTENCY-{suffix.ToUpperInvariant()}@EXAMPLE.TEST",
            FirstName = "Idempotency",
            LastName = "Actor",
            Municipality = municipality,
            IsActive = true,
            SecurityStamp = $"stamp-{suffix}"
        };
        var created = createdAt ?? DateTime.UtcNow;
        var scope = $"municipality:{municipality.Id}";
        var method = "POST";
        var route = $"/api/v1/items/{suffix}";
        var key = $"stable-key-{suffix}";
        return (municipality, user, new IdempotencyRequest
        {
            Municipality = municipality,
            User = user,
            ScopeKey = scope,
            IdentityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{scope}\n{user.Id}\n{method}\n{route}\n{key}"))),
            Method = method,
            Route = route,
            IdempotencyKey = key,
            RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"request-{suffix}"))),
            State = IdempotencyRequestStates.InProgress,
            CreatedAt = created,
            ExpiresAt = expiresAt ?? created.AddHours(24),
            CorrelationId = $"correlation-{suffix}"
        });
    }
}

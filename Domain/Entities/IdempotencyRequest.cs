namespace FTCERP.Host.Domain.Entities;

public static class IdempotencyRequestStates
{
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public sealed class IdempotencyRequest
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string ScopeKey { get; set; } = string.Empty;
    public string IdentityHash { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string State { get; set; } = IdempotencyRequestStates.InProgress;
    public int? ResponseStatusCode { get; set; }
    public string? ResponseContentType { get; set; }
    public string? ResponseLocation { get; set; }
    public string? ResponseETag { get; set; }
    public byte[]? ResponseBody { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

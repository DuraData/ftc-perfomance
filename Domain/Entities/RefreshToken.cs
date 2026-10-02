namespace FTCERP.Host.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? LastUsedByIp { get; set; }
    public string? UserAgent { get; set; }
    public string? RevokedReason { get; set; }
    public string SecurityStamp { get; set; } = string.Empty;
    public string AuthenticationMethod { get; set; } = "LOCAL";
    public byte[] RowVersion { get; set; } = [];
}

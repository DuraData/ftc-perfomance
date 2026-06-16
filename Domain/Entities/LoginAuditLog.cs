namespace FTCERP.Host.Domain.Entities;

public class LoginAuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
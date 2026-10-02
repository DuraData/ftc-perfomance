namespace FTCERP.Host.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public int SessionIdleTimeoutMinutes { get; set; } = 30;
    public int SessionAbsoluteTimeoutHours { get; set; } = 24;
    public int MaxConcurrentSessions { get; set; } = 5;
    public string Issuer { get; set; } = "FTCERP";
    public string Audience { get; set; } = "FTCERP";
}

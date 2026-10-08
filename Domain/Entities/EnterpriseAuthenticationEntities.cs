namespace FTCERP.Host.Domain.Entities;

public sealed class AuthenticationConfiguration
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid ConfigurationFamilyPublicId { get; set; } = Guid.NewGuid();
    public int VersionNumber { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public long? PreviousVersionId { get; set; }
    public long MunicipalityId { get; set; }
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Local;
    public string? ProviderRegistrationCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? ModifiedByUser { get; set; }
    public AuthenticationPolicy? Policy { get; set; }
    public AuthenticationConfiguration? PreviousVersion { get; set; }
    public ICollection<AuthenticationConfiguration> SuccessorVersions { get; set; } = new List<AuthenticationConfiguration>();
}

public sealed class AuthenticationPolicy
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long AuthenticationConfigurationId { get; set; }
    public int MinimumPasswordLength { get; set; } = 12;
    public int MaximumFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public bool RequireMfaForPrivilegedLocalUsers { get; set; } = true;
    public bool RequireMfaForAllLocalUsers { get; set; }
    public bool RequireFirstLoginPasswordChange { get; set; } = true;
    public int SessionIdleTimeoutMinutes { get; set; } = 30;
    public int SessionAbsoluteTimeoutHours { get; set; } = 24;
    public int MaximumConcurrentSessions { get; set; } = 5;
    public string ModifiedByUserId { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public AuthenticationConfiguration AuthenticationConfiguration { get; set; } = null!;
    public ApplicationUser ModifiedByUser { get; set; } = null!;
}

public sealed class UserAuthenticator
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProviderRegistrationCode { get; set; } = string.Empty;
    public string ExpectedEmail { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public string? ExternalIdentityHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LinkedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? LinkedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAuthenticatedAt { get; set; }
    public string? DisabledByUserId { get; set; }
    public DateTime? DisabledAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? LinkedByUser { get; set; }
    public ApplicationUser? DisabledByUser { get; set; }
}

public sealed class AuthenticationEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string? UserId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureCode { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality? Municipality { get; set; }
    public ApplicationUser? User { get; set; }
}

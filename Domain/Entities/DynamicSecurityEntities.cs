namespace FTCERP.Host.Domain.Entities;

public enum AuthenticationMode
{
    Local = 1,
    MicrosoftEntraId = 2,
    ActiveDirectory = 3,
    Hybrid = 4
}

public enum SecurityPermissionKind
{
    Resource = 1,
    Navigation = 2,
    Member = 3,
    Action = 4,
    Report = 5
}

public enum SecurityOperation
{
    Create = 1,
    Read = 2,
    Update = 3,
    Delete = 4,
    Export = 5,
    Import = 6,
    Execute = 7
}

public class Municipality
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.Local;
    public bool TidEnabled { get; set; }
    public bool TidAllKpisRequired { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class SecurityResource
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "ENTITY";
    public string? EntityTypeName { get; set; }
    public string? ApiResourceName { get; set; }
    public string? Description { get; set; }
    public bool SupportsCreate { get; set; }
    public bool SupportsRead { get; set; } = true;
    public bool SupportsUpdate { get; set; }
    public bool SupportsDelete { get; set; }
    public bool SupportsExport { get; set; }
    public bool SupportsImport { get; set; }
    public bool SupportsFieldSecurity { get; set; }
    public bool SupportsRecordCriteria { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class SecurityNavigationItem
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int? ParentId { get; set; }
    public SecurityNavigationItem? Parent { get; set; }
    public ICollection<SecurityNavigationItem> Children { get; set; } = new List<SecurityNavigationItem>();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? IconKey { get; set; }
    public int DisplayOrder { get; set; }
    public string? RequiredPermissionCode { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class SecurityActionDefinition
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResourceCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class SecurityMemberDefinition
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string ResourceCode { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public bool IsSystemManaged { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class SecurityUserRoleAssignment
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;
    public long? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string AssignedBy { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? RevokedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

namespace FTCERP.Host.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Feature { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SecurityPermissionKind Kind { get; set; } = SecurityPermissionKind.Action;
    public string? ResourceCode { get; set; }
    public SecurityOperation? Operation { get; set; }
    public string? MemberCode { get; set; }
    public string? NavigationCode { get; set; }
    public string? ActionCode { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermissionOverride> UserPermissionOverrides { get; set; } = new List<UserPermissionOverride>();
}

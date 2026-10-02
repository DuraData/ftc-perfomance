namespace FTCERP.Host.Domain.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsAllowed { get; set; } = true;
    public ScopeType? ScopeType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

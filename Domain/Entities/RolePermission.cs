namespace FTCERP.Host.Domain.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsAllowed { get; set; } = true;
}
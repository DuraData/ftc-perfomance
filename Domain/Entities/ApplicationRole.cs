using Microsoft.AspNetCore.Identity;

namespace FTCERP.Host.Domain.Entities;

public class ApplicationRole : IdentityRole
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<SecurityUserRoleAssignment> UserAssignments { get; set; } = new List<SecurityUserRoleAssignment>();
}

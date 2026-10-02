namespace FTCERP.Host.Domain.Entities;

public class Unit
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public int DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<UserScope> UserScopes { get; set; } = new List<UserScope>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

namespace FTCERP.Host.Domain.Entities;

public class Unit
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Department Department { get; set; } = null!;
    public ICollection<UserScope> UserScopes { get; set; } = new List<UserScope>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

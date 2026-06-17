namespace FTCERP.Host.Domain.Entities;

public class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<UserScope> UserScopes { get; set; } = new List<UserScope>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

namespace FTCERP.Host.Domain.Entities;

public enum ScopeType
{
    InstitutionScope = 1,
    DepartmentScope = 2,
    UnitScope = 3,
    AssignedKpiScope = 4,
    AssignedTargetScope = 5,
    AssignedProjectScope = 6,
    AssignedTaskScope = 7
}

public class UserScope
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ScopeType ScopeType { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public string? TargetId { get; set; }
    public string? KpiId { get; set; }
    public string? ProjectId { get; set; }
    public string? TaskId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Department? Department { get; set; }
    public Unit? Unit { get; set; }
}

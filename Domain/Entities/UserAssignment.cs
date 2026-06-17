namespace FTCERP.Host.Domain.Entities;

public enum AssignmentType
{
    AdditionalApproverAssignment = 1,
    AdditionalVerifierAssignment = 2,
    AdditionalSubmitterAssignment = 3,
    ProjectAssignee = 4,
    TaskAssignee = 5
}

public class UserAssignment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AssignmentType AssignmentType { get; set; }
    public string? TargetId { get; set; }
    public string? KpiId { get; set; }
    public string? ProjectId { get; set; }
    public string? TaskId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}

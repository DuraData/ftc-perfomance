namespace FTCERP.Host.Domain.Entities;

public enum WorkflowActionType
{
    Create = 1,
    Edit = 2,
    Delete = 3,
    Archive = 4,
    Submit = 5,
    Verify = 6,
    VerifyReject = 7,
    Approve = 8,
    Reject = 9,
    Review = 10,
    Audit = 11,
    Score = 12,
    UploadPoe = 13,
    DeletePoe = 14,
    ExtendDueDate = 15,
    PermissionChange = 16
}

public enum NotificationType
{
    Submission = 1,
    Rejection = 2,
    VerifyRejection = 3,
    Approval = 4,
    Rfi = 5,
    InternalAuditRfi = 6,
    OverdueItem = 7,
    DueDateExtension = 8
}

public enum SubmissionKind
{
    Opms = 1,
    Ipms = 2
}

public class OpmsTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? SourceTemplateId { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public int? PeriodId { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public string? AssignedUserId { get; set; }
    public string? WardIds { get; set; }
    public string? AdditionalAssigneeIds { get; set; }
    public string? VoteNumberIds { get; set; }
    public string IndicatorNumber { get; set; } = string.Empty;
    public string NationalKpa { get; set; } = string.Empty;
    public string MunicipalKpa { get; set; } = string.Empty;
    public int? StrategicGoalId { get; set; }
    public int? StrategicObjectiveId { get; set; }
    public string PerformanceObjective { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string KpiDescription { get; set; } = string.Empty;
    public decimal Baseline { get; set; }
    public string? BaselineDescription { get; set; }
    public decimal AnnualTarget { get; set; }
    public string AnnualTargetDescription { get; set; } = string.Empty;
    public int? BudgetSourceId { get; set; }
    public int? BudgetTypeId { get; set; }
    public int? UnitOfMeasureId { get; set; }
    public decimal Weight { get; set; }
    public string KpiType { get; set; } = string.Empty;
    public string IndicatorType { get; set; } = string.Empty;
    public string? FunctionalArea { get; set; }
    public string? StandardClassification { get; set; }
    public string? IdpReference { get; set; }
    public string? InternalReference { get; set; }
    public string? FmsLink { get; set; }
    public bool IsRevised { get; set; }
    public bool IsWithdrawn { get; set; }
    public string? ReasonForWithdrawal { get; set; }
    public string TargetUnitType { get; set; } = "absolute_count";
    public decimal? Q1Target { get; set; }
    public string? Q1Description { get; set; }
    public decimal? Q1Budget { get; set; }
    public decimal? Q2Target { get; set; }
    public string? Q2Description { get; set; }
    public decimal? Q2Budget { get; set; }
    public decimal? MidTermTarget { get; set; }
    public string? MidTermDescription { get; set; }
    public decimal? MidTermBudget { get; set; }
    public decimal? Q3Target { get; set; }
    public string? Q3Description { get; set; }
    public decimal? Q3Budget { get; set; }
    public decimal? Q3RevisedTarget { get; set; }
    public decimal? Q4Target { get; set; }
    public string? Q4Description { get; set; }
    public decimal? Q4Budget { get; set; }
    public decimal? Q4RevisedTarget { get; set; }
    public decimal? RevisedAnnualTarget { get; set; }
    public decimal? RevisedAnnualBudget { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Period? Period { get; set; }
        public Department? Department { get; set; }
        public Unit? Unit { get; set; }
        public ApplicationUser? AssignedUser { get; set; }
        public StrategicGoal? StrategicGoal { get; set; }
        public StrategicObjective? StrategicObjective { get; set; }
        public BudgetSource? BudgetSource { get; set; }
        public BudgetType? BudgetType { get; set; }
        public UnitOfMeasure? UnitOfMeasure { get; set; }
        public ICollection<OpmsSubmission> Submissions { get; set; } = new List<OpmsSubmission>();
}

public class IpmsTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? SourceTemplateId { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public string? RelatedOpmsTargetId { get; set; }
    public int? PeriodId { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public string? AssignedUserId { get; set; }
    public string? SupervisorId { get; set; }
    public string IndicatorNumber { get; set; } = string.Empty;
    public string NationalKpa { get; set; } = string.Empty;
    public string MunicipalKpa { get; set; } = string.Empty;
    public int? StrategicGoalId { get; set; }
    public int? StrategicObjectiveId { get; set; }
    public string PerformanceObjective { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string KpiDescription { get; set; } = string.Empty;
    public decimal Baseline { get; set; }
    public decimal AnnualTarget { get; set; }
    public string AnnualTargetDescription { get; set; } = string.Empty;
    public int? BudgetSourceId { get; set; }
    public int? BudgetTypeId { get; set; }
    public int? UnitOfMeasureId { get; set; }
    public decimal Weight { get; set; }
    public string KpiType { get; set; } = string.Empty;
    public string IndicatorType { get; set; } = string.Empty;
    public string? FunctionalArea { get; set; }
    public string? IdpReference { get; set; }
    public string? InternalReference { get; set; }
    public bool IsRevised { get; set; }
    public string TargetUnitType { get; set; } = "absolute_count";
    public decimal? Q1Target { get; set; }
    public string? Q1Description { get; set; }
    public decimal? Q1Budget { get; set; }
    public decimal? Q2Target { get; set; }
    public string? Q2Description { get; set; }
    public decimal? Q2Budget { get; set; }
    public decimal? MidTermTarget { get; set; }
    public string? MidTermDescription { get; set; }
    public decimal? MidTermBudget { get; set; }
    public decimal? Q3Target { get; set; }
    public string? Q3Description { get; set; }
    public decimal? Q3Budget { get; set; }
    public decimal? Q3RevisedTarget { get; set; }
    public decimal? Q4Target { get; set; }
    public string? Q4Description { get; set; }
    public decimal? Q4Budget { get; set; }
    public decimal? Q4RevisedTarget { get; set; }
    public decimal? RevisedAnnualTarget { get; set; }
    public decimal? RevisedAnnualBudget { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Period? Period { get; set; }
        public Department? Department { get; set; }
        public Unit? Unit { get; set; }
        public ApplicationUser? AssignedUser { get; set; }
        public StrategicGoal? StrategicGoal { get; set; }
        public StrategicObjective? StrategicObjective { get; set; }
        public BudgetSource? BudgetSource { get; set; }
        public BudgetType? BudgetType { get; set; }
        public UnitOfMeasure? UnitOfMeasure { get; set; }
        public OpmsTarget? RelatedOpmsTarget { get; set; }
        public ICollection<IpmsSubmission> Submissions { get; set; } = new List<IpmsSubmission>();
}

public class OpmsSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OpmsTargetId { get; set; } = string.Empty;
    public string Quarter { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SubmitterStatus { get; set; } = "Draft";
    public string VerifierStatus { get; set; } = "Pending";
    public string ApproverStatus { get; set; } = "Pending";
    public string PmsStatus { get; set; } = "Pending";
    public string AuditorStatus { get; set; } = "Pending";
    public decimal? Actual { get; set; }
    public string? ActualDescription { get; set; }
    public string? ActualPerformanceDescription { get; set; }
    public decimal? ActualExpenditure { get; set; }
    public decimal? Variance { get; set; }
    public string? VarianceReason { get; set; }
    public string? CorrectiveMeasure { get; set; }
    public decimal? SubmitterScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByUserId { get; set; }
    public string? VerifierUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifierComments { get; set; }
    public string? VerifierComment { get; set; }
    public decimal? VerifierScore { get; set; }
    public string? ApproverUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComments { get; set; }
    public string? ApproverComment { get; set; }
    public decimal? ApproverScore { get; set; }
    public string? PmsOfficerUserId { get; set; }
    public DateTime? PmsReviewedAt { get; set; }
    public string? PmsComments { get; set; }
    public string? PmsComment { get; set; }
    public string? PmsRecommendation { get; set; }
    public decimal? PmsScore { get; set; }
    public DateTime? PmsResponseDueDate { get; set; }
    public string? PmsRfiComment { get; set; }
    public string? AuditorUserId { get; set; }
    public DateTime? AuditedAt { get; set; }
    public string? AuditorComments { get; set; }
    public string? AuditorComment { get; set; }
    public string? AuditorRecommendation { get; set; }
    public decimal? AuditorScore { get; set; }
    public DateTime? AuditorResponseDueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public int? DueDateExtendedDays { get; set; }
    public string? PoeType { get; set; }
    public bool IsDisabled { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? OrganisationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OpmsTarget OpmsTarget { get; set; } = null!;
    public ApplicationUser? SubmittedByUser { get; set; }
    public ApplicationUser? VerifierUser { get; set; }
    public ApplicationUser? ApproverUser { get; set; }
    public ApplicationUser? PmsOfficerUser { get; set; }
    public ApplicationUser? AuditorUser { get; set; }
    public ICollection<PoeFile> PoeFiles { get; set; } = new List<PoeFile>();
    public ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();
    public ICollection<SubmissionScore> Scores { get; set; } = new List<SubmissionScore>();
}

public class IpmsSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string IpmsTargetId { get; set; } = string.Empty;
    public string Quarter { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SubmitterStatus { get; set; } = "Draft";
    public string VerifierStatus { get; set; } = "Pending";
    public string ApproverStatus { get; set; } = "Pending";
    public string PmsStatus { get; set; } = "Pending";
    public string AuditorStatus { get; set; } = "Pending";
    public decimal? Actual { get; set; }
    public string? ActualDescription { get; set; }
    public string? ActualPerformanceDescription { get; set; }
    public decimal? ActualExpenditure { get; set; }
    public decimal? Variance { get; set; }
    public string? VarianceReason { get; set; }
    public string? CorrectiveMeasure { get; set; }
    public decimal? SubmitterScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByUserId { get; set; }
    public string? VerifierUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifierComments { get; set; }
    public string? VerifierComment { get; set; }
    public decimal? VerifierScore { get; set; }
    public string? ApproverUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComments { get; set; }
    public string? ApproverComment { get; set; }
    public decimal? ApproverScore { get; set; }
    public string? PmsOfficerUserId { get; set; }
    public DateTime? PmsReviewedAt { get; set; }
    public string? PmsComments { get; set; }
    public string? PmsComment { get; set; }
    public string? PmsRecommendation { get; set; }
    public decimal? PmsScore { get; set; }
    public DateTime? PmsResponseDueDate { get; set; }
    public string? PmsRfiComment { get; set; }
    public string? AuditorUserId { get; set; }
    public DateTime? AuditedAt { get; set; }
    public string? AuditorComments { get; set; }
    public string? AuditorComment { get; set; }
    public string? AuditorRecommendation { get; set; }
    public decimal? AuditorScore { get; set; }
    public DateTime? AuditorResponseDueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public int? DueDateExtendedDays { get; set; }
    public string? PoeType { get; set; }
    public bool IsDisabled { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? OrganisationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IpmsTarget IpmsTarget { get; set; } = null!;
    public ApplicationUser? SubmittedByUser { get; set; }
    public ApplicationUser? VerifierUser { get; set; }
    public ApplicationUser? ApproverUser { get; set; }
    public ApplicationUser? PmsOfficerUser { get; set; }
    public ApplicationUser? AuditorUser { get; set; }
    public ICollection<PoeFile> PoeFiles { get; set; } = new List<PoeFile>();
    public ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();
    public ICollection<SubmissionScore> Scores { get; set; } = new List<SubmissionScore>();
}

public class PoeFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser UploadedByUser { get; set; } = null!;
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}

public class AuditTrail
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }

    public ApplicationUser? ChangedByUser { get; set; }
}

public class DueDateExtension
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public DateTime OriginalDueDate { get; set; }
    public DateTime ExtendedDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ApprovedByUserId { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser ApprovedByUser { get; set; } = null!;
}

public class ReviewComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string CommentedByUserId { get; set; } = string.Empty;
    public DateTime CommentedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser CommentedByUser { get; set; } = null!;
}

public class AuditFinding
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public string Finding { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser CreatedByUser { get; set; } = null!;
}

public class SubmissionScore
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Notes { get; set; }
    public string ScoredByUserId { get; set; } = string.Empty;
    public DateTime ScoredAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser ScoredByUser { get; set; } = null!;
}

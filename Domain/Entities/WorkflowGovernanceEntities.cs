using System.ComponentModel.DataAnnotations.Schema;

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
    DueDateExtension = 8,
    DeadlineReminder = 9,
    Escalation = 10
}

public enum SubmissionKind
{
    Opms = 1,
    Ipms = 2
}

public static class SubmissionBaseStates
{
    public const string InProgress = "IN_PROGRESS";
    public const string Submitted = "SUBMITTED";

    public static string Normalize(string? state) => state?.Trim().ToUpperInvariant() switch
    {
        InProgress or "DRAFT" or "VERIFY_REJECTED" or "REJECTED" or null or "" => InProgress,
        _ => Submitted
    };
}

public partial class OpmsTarget
{
    [NotMapped]
    public IReadOnlyList<PerformancePeriodTarget> CanonicalPeriodTargets { get; set; } = [];

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string? SourceTemplateId { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public int? PeriodId { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public string? AssignedUserId { get; set; }
    [Column("WardIds")] public string? LegacyWardIds { get; set; }
    [Column("AdditionalAssigneeIds")] public string? LegacyAdditionalAssigneeIds { get; set; }
    [Column("VoteNumberIds")] public string? LegacyVoteNumberIds { get; set; }
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
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawnByUserId { get; set; }
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
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
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
        public ICollection<OpmsTargetWard> Wards { get; set; } = new List<OpmsTargetWard>();
        public ICollection<OpmsTargetAdditionalAssignee> AdditionalAssignees { get; set; } = new List<OpmsTargetAdditionalAssignee>();
        public ICollection<OpmsTargetVoteNumber> VoteNumbers { get; set; } = new List<OpmsTargetVoteNumber>();
        public ICollection<TechnicalIndicatorDescription> TechnicalIndicatorDescriptions { get; set; } = new List<TechnicalIndicatorDescription>();
}

public class OpmsTargetWard
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public int WardId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Municipality? Municipality { get; set; }
    public OpmsTarget Target { get; set; } = null!;
    public Ward Ward { get; set; } = null!;
}

public class OpmsTargetAdditionalAssignee
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Municipality? Municipality { get; set; }
    public OpmsTarget Target { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

public class OpmsTargetVoteNumber
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public int VoteNumberId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Municipality? Municipality { get; set; }
    public OpmsTarget Target { get; set; } = null!;
    public VoteNumber VoteNumber { get; set; } = null!;
}

public partial class IpmsTarget
{
    [NotMapped]
    public IReadOnlyList<PerformancePeriodTarget> CanonicalPeriodTargets { get; set; } = [];

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
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
    public bool IsWithdrawn { get; set; }
    public string? ReasonForWithdrawal { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawnByUserId { get; set; }
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
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
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

public partial class OpmsSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public long? ReportingPeriodId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public string Quarter { get; set; } = string.Empty;
    public string BaseState { get; set; } = SubmissionBaseStates.InProgress;
    public string Status { get; set; } = string.Empty;
    public string SubmitterStatus { get; set; } = "Draft";
    public string VerifierStatus { get; set; } = "Pending";
    public string ApproverStatus { get; set; } = "Pending";
    public string PmsStatus { get; set; } = "Pending";
    public string AuditorStatus { get; set; } = "Pending";
    public string? ActualPerformance { get; set; }
    public decimal? AchievementPercent { get; set; }
    public bool? TargetAchieved { get; set; }
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
    public string? WithdrawalReason { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawnByUserId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? OrganisationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public ReportingPeriod? ReportingPeriod { get; set; }
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

public partial class IpmsSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public long? ReportingPeriodId { get; set; }
    public string IpmsTargetId { get; set; } = string.Empty;
    public string Quarter { get; set; } = string.Empty;
    public string BaseState { get; set; } = SubmissionBaseStates.InProgress;
    public string Status { get; set; } = string.Empty;
    public string SubmitterStatus { get; set; } = "Draft";
    public string VerifierStatus { get; set; } = "Pending";
    public string ApproverStatus { get; set; } = "Pending";
    public string PmsStatus { get; set; } = "Pending";
    public string AuditorStatus { get; set; } = "Pending";
    public string? ActualPerformance { get; set; }
    public decimal? AchievementPercent { get; set; }
    public bool? TargetAchieved { get; set; }
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
    public string? WithdrawalReason { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawnByUserId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? OrganisationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public ReportingPeriod? ReportingPeriod { get; set; }
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

public enum GovernedLifecycleAction
{
    Withdrawn = 1
}

public class GovernedRecordLifecycleEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public GovernedLifecycleAction Action { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}

public class PoeFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string EvidenceBlobId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? RetainUntil { get; set; }
    public string? SupersedesPoeFileId { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public EvidenceBlob Blob { get; set; } = null!;
    public ApplicationUser UploadedByUser { get; set; } = null!;
    public ICollection<PoeEvidenceAssessment> Assessments { get; set; } = new List<PoeEvidenceAssessment>();
    public PoeEvidenceReplacement? ReplacementAsNew { get; set; }
    public ICollection<PoeEvidenceReplacement> ReplacementsAsOld { get; set; } = new List<PoeEvidenceReplacement>();
    public ICollection<PoeLegalHoldEvent> LegalHoldEvents { get; set; } = new List<PoeLegalHoldEvent>();
    public ICollection<PoeDisposalEvent> DisposalEvents { get; set; } = new List<PoeDisposalEvent>();
}

public enum PoeAssessmentOutcome { Accepted = 1, Rejected = 2, NeedsClarification = 3 }

public class PoeEvidenceAssessment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string PoeFileId { get; set; } = string.Empty;
    public PoeAssessmentOutcome Outcome { get; set; }
    public string? Comment { get; set; }
    public string AssessedByUserId { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public PoeFile PoeFile { get; set; } = null!;
    public ApplicationUser AssessedByUser { get; set; } = null!;
}

public class PoeEvidenceReplacement
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string SupersededPoeFileId { get; set; } = string.Empty;
    public string ReplacementPoeFileId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ReplacedByUserId { get; set; } = string.Empty;
    public DateTime ReplacedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public PoeFile SupersededPoeFile { get; set; } = null!;
    public PoeFile ReplacementPoeFile { get; set; } = null!;
    public ApplicationUser ReplacedByUser { get; set; } = null!;
}

public enum PoeLegalHoldAction { Placed = 1, Released = 2 }

public class PoeLegalHoldEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid HoldId { get; set; }
    public long MunicipalityId { get; set; }
    public string PoeFileId { get; set; } = string.Empty;
    public PoeLegalHoldAction Action { get; set; }
    public string HoldReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public PoeFile PoeFile { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}

public enum PoeDisposalAction { Requested = 1, Completed = 2, Failed = 3 }

public class PoeDisposalEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid DisposalId { get; set; }
    public long MunicipalityId { get; set; }
    public string PoeFileId { get; set; } = string.Empty;
    public PoeDisposalAction Action { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ApprovalReference { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public Municipality Municipality { get; set; } = null!;
    public PoeFile PoeFile { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Municipality? Municipality { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

public class AuditTrail
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string? Reason { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }

    public Municipality? Municipality { get; set; }
    public ApplicationUser? ChangedByUser { get; set; }
}

public class BusinessEventOutbox
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality? Municipality { get; set; }
    public ICollection<NotificationDeliveryAttempt> DeliveryAttempts { get; set; } = new List<NotificationDeliveryAttempt>();
}

public class NotificationDeliveryAttempt
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long? MunicipalityId { get; set; }
    public long BusinessEventOutboxId { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string Channel { get; set; } = "IN_APP";
    public string Status { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public string? Error { get; set; }
    public string? Provider { get; set; }
    public string? ProviderReference { get; set; }
    public string? ResponseDetail { get; set; }

    public Municipality? Municipality { get; set; }
    public BusinessEventOutbox BusinessEventOutbox { get; set; } = null!;
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

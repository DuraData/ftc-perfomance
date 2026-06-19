namespace FTCERP.Host.Domain.Entities;

public enum IdpPlanStatus
{
    Draft = 1,
    InReview = 2,
    Recommended = 3,
    Approved = 4,
    Published = 5,
    Archived = 6
}

public enum IdpVersionType
{
    Original = 1,
    AnnualReview = 2,
    Revised = 3,
    Amendment = 4
}

public enum IdpProjectStatus
{
    Planned = 1,
    InProgress = 2,
    Delayed = 3,
    Completed = 4,
    Cancelled = 5
}

public enum IdpKpiIndicatorType
{
    Strategic = 1,
    Outcome = 2,
    Output = 3,
    Impact = 4,
    Circular88 = 5,
    TreasuryTid = 6
}

public enum AlignmentFrameworkType
{
    NationalDevelopmentPlan = 1,
    ProvincialGrowthStrategy = 2,
    DistrictDevelopmentModel = 3,
    SectorPlan = 4,
    MunicipalGoal = 5,
    Circular88 = 6,
    TreasuryTid = 7
}

public enum IdpParticipationType
{
    PublicMeeting = 1,
    WardConsultation = 2,
    StakeholderEngagement = 3
}

public enum IdpRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum IdpDocumentCategory
{
    SignedIdp = 1,
    CouncilResolution = 2,
    Policy = 3,
    Framework = 4,
    Circular = 5,
    Guideline = 6,
    ParticipationEvidence = 7,
    PoeEvidence = 8,
    Governance = 9
}

public class IdpPlan
{
    public int Id { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public string PlanTitle { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public int StartFinancialYear { get; set; }
    public int EndFinancialYear { get; set; }
    public IdpPlanStatus Status { get; set; } = IdpPlanStatus.Draft;
    public int CurrentVersionNumber { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
    public ICollection<IdpPlanVersion> Versions { get; set; } = new List<IdpPlanVersion>();
    public ICollection<IdpStrategicOutcome> StrategicOutcomes { get; set; } = new List<IdpStrategicOutcome>();
    public ICollection<IdpCommunitySession> CommunitySessions { get; set; } = new List<IdpCommunitySession>();
    public ICollection<IdpDocument> Documents { get; set; } = new List<IdpDocument>();
}

public class IdpPlanVersion
{
    public int Id { get; set; }
    public int IdpPlanId { get; set; }
    public int VersionNumber { get; set; }
    public IdpVersionType VersionType { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public string? ReviewYear { get; set; }
    public string? SummaryOfChanges { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;

    public IdpPlan IdpPlan { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<IdpChangeLog> ChangeLogs { get; set; } = new List<IdpChangeLog>();
}

public class IdpChangeLog
{
    public long Id { get; set; }
    public int IdpPlanVersionId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public IdpPlanVersion IdpPlanVersion { get; set; } = null!;
    public ApplicationUser? ChangedByUser { get; set; }
}

public class IdpStrategicOutcome
{
    public int Id { get; set; }
    public int IdpPlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public IdpPlan IdpPlan { get; set; } = null!;
    public ICollection<IdpStrategicObjective> StrategicObjectives { get; set; } = new List<IdpStrategicObjective>();
}

public class IdpStrategicObjective
{
    public int Id { get; set; }
    public int IdpStrategicOutcomeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaselineValue { get; set; }
    public decimal TargetValue { get; set; }
    public int? ResponsibleDepartmentId { get; set; }
    public string? StrategicOwnerUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal BudgetAllocation { get; set; }
    public int SortOrder { get; set; }

    public IdpStrategicOutcome IdpStrategicOutcome { get; set; } = null!;
    public Department? ResponsibleDepartment { get; set; }
    public ApplicationUser? StrategicOwnerUser { get; set; }
    public ICollection<IdpDevelopmentPriority> DevelopmentPriorities { get; set; } = new List<IdpDevelopmentPriority>();
    public ICollection<IdpAlignmentLink> AlignmentLinks { get; set; } = new List<IdpAlignmentLink>();
    public ICollection<IdpRiskLink> RiskLinks { get; set; } = new List<IdpRiskLink>();
    public ICollection<IdpBudgetSnapshot> BudgetSnapshots { get; set; } = new List<IdpBudgetSnapshot>();
}

public class IdpDevelopmentPriority
{
    public int Id { get; set; }
    public int IdpStrategicObjectiveId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public IdpStrategicObjective IdpStrategicObjective { get; set; } = null!;
    public ICollection<IdpProgramme> Programmes { get; set; } = new List<IdpProgramme>();
}

public class IdpProgramme
{
    public int Id { get; set; }
    public int IdpDevelopmentPriorityId { get; set; }
    public string ProgrammeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ResponsibleDepartmentId { get; set; }
    public decimal PlannedBudget { get; set; }
    public decimal ApprovedBudget { get; set; }
    public decimal ActualExpenditure { get; set; }

    public IdpDevelopmentPriority IdpDevelopmentPriority { get; set; } = null!;
    public Department? ResponsibleDepartment { get; set; }
    public ICollection<IdpProject> Projects { get; set; } = new List<IdpProject>();
}

public class IdpProject
{
    public int Id { get; set; }
    public int IdpProgrammeId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public decimal Budget { get; set; }
    public string FundingSource { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IdpProjectStatus Status { get; set; } = IdpProjectStatus.Planned;
    public string? CommunityNeedReference { get; set; }

    public IdpProgramme IdpProgramme { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<IdpKpi> Kpis { get; set; } = new List<IdpKpi>();
    public ICollection<IdpBudgetSnapshot> BudgetSnapshots { get; set; } = new List<IdpBudgetSnapshot>();
    public ICollection<IdpRiskLink> RiskLinks { get; set; } = new List<IdpRiskLink>();
}

public class IdpKpi
{
    public int Id { get; set; }
    public int IdpProjectId { get; set; }
    public string KpiCode { get; set; } = string.Empty;
    public string KpiName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public decimal Baseline { get; set; }
    public decimal AnnualTarget { get; set; }
    public decimal FiveYearTarget { get; set; }
    public int? ResponsibleDepartmentId { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public string ReportingFrequency { get; set; } = string.Empty;
    public IdpKpiIndicatorType IndicatorType { get; set; } = IdpKpiIndicatorType.Strategic;
    public bool Circular88Linked { get; set; }
    public bool TreasuryTidLinked { get; set; }

    public IdpProject IdpProject { get; set; } = null!;
    public Department? ResponsibleDepartment { get; set; }
    public ICollection<IdpAnnualTarget> AnnualTargets { get; set; } = new List<IdpAnnualTarget>();
    public ICollection<IdpRiskLink> RiskLinks { get; set; } = new List<IdpRiskLink>();
}

public class IdpAnnualTarget
{
    public int Id { get; set; }
    public int IdpKpiId { get; set; }
    public int FinancialYear { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public string? ProgressComment { get; set; }

    public IdpKpi IdpKpi { get; set; } = null!;
}

public class IdpAlignmentLink
{
    public long Id { get; set; }
    public int IdpStrategicObjectiveId { get; set; }
    public AlignmentFrameworkType FrameworkType { get; set; }
    public string FrameworkReferenceCode { get; set; } = string.Empty;
    public string FrameworkReferenceTitle { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public IdpStrategicObjective IdpStrategicObjective { get; set; } = null!;
}

public class IdpCommunitySession
{
    public int Id { get; set; }
    public int IdpPlanId { get; set; }
    public IdpParticipationType ParticipationType { get; set; } = IdpParticipationType.PublicMeeting;
    public DateTime SessionDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public int? WardId { get; set; }
    public int ParticipantsCount { get; set; }
    public string? AttendanceRegisterPath { get; set; }
    public string? MinutesPath { get; set; }

    public IdpPlan IdpPlan { get; set; } = null!;
    public Ward? Ward { get; set; }
    public ICollection<IdpCommunityNeed> CommunityNeeds { get; set; } = new List<IdpCommunityNeed>();
    public ICollection<IdpStakeholderEngagement> StakeholderEngagements { get; set; } = new List<IdpStakeholderEngagement>();
}

public class IdpCommunityNeed
{
    public int Id { get; set; }
    public int IdpCommunitySessionId { get; set; }
    public string IssueCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PriorityLevel { get; set; } = "Medium";
    public string? ProposedIntervention { get; set; }

    public IdpCommunitySession IdpCommunitySession { get; set; } = null!;
}

public class IdpWardInput
{
    public int Id { get; set; }
    public int IdpPlanId { get; set; }
    public int WardId { get; set; }
    public string WardPlanSummary { get; set; } = string.Empty;
    public string WardPriorities { get; set; } = string.Empty;
    public string WardProjects { get; set; } = string.Empty;

    public IdpPlan IdpPlan { get; set; } = null!;
    public Ward Ward { get; set; } = null!;
}

public class IdpStakeholderEngagement
{
    public int Id { get; set; }
    public int IdpCommunitySessionId { get; set; }
    public string StakeholderType { get; set; } = string.Empty;
    public string StakeholderName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? KeyInput { get; set; }

    public IdpCommunitySession IdpCommunitySession { get; set; } = null!;
}

public class IdpRiskLink
{
    public long Id { get; set; }
    public int? IdpStrategicObjectiveId { get; set; }
    public int? IdpProjectId { get; set; }
    public int? IdpKpiId { get; set; }
    public string RiskReference { get; set; } = string.Empty;
    public string RiskTitle { get; set; } = string.Empty;
    public string? MitigationPlan { get; set; }
    public IdpRiskLevel RiskLevel { get; set; } = IdpRiskLevel.Medium;

    public IdpStrategicObjective? IdpStrategicObjective { get; set; }
    public IdpProject? IdpProject { get; set; }
    public IdpKpi? IdpKpi { get; set; }
}

public class IdpBudgetSnapshot
{
    public long Id { get; set; }
    public int? IdpStrategicObjectiveId { get; set; }
    public int? IdpProjectId { get; set; }
    public int FinancialYear { get; set; }
    public decimal PlannedBudget { get; set; }
    public decimal ApprovedBudget { get; set; }
    public decimal ActualExpenditure { get; set; }
    public string SourceSystem { get; set; } = "FMS";
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public IdpStrategicObjective? IdpStrategicObjective { get; set; }
    public IdpProject? IdpProject { get; set; }
}

public class IdpDocument
{
    public int Id { get; set; }
    public int IdpPlanId { get; set; }
    public int? IdpPlanVersionId { get; set; }
    public IdpDocumentCategory Category { get; set; } = IdpDocumentCategory.Governance;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public int VersionNumber { get; set; } = 1;
    public bool IsApproved { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedByUserId { get; set; } = string.Empty;

    public IdpPlan IdpPlan { get; set; } = null!;
    public IdpPlanVersion? IdpPlanVersion { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
}

public class IdpCollaborationComment
{
    public long Id { get; set; }
    public int IdpPlanId { get; set; }
    public int? IdpPlanVersionId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string CommentedByUserId { get; set; } = string.Empty;
    public DateTime CommentedAt { get; set; } = DateTime.UtcNow;

    public IdpPlan IdpPlan { get; set; } = null!;
    public IdpPlanVersion? IdpPlanVersion { get; set; }
    public ApplicationUser? CommentedByUser { get; set; }
}

public class IdpTaskAssignment
{
    public long Id { get; set; }
    public int IdpPlanId { get; set; }
    public int? IdpPlanVersionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssignedToUserId { get; set; } = string.Empty;
    public string AssignedByUserId { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public IdpPlan IdpPlan { get; set; } = null!;
    public IdpPlanVersion? IdpPlanVersion { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public ApplicationUser? AssignedByUser { get; set; }
}

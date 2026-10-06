using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Host.API.Requests;

public record LoginRequest(string Email, string Password, string? TwoFactorCode = null, string? RecoveryCode = null);

public record EnableMfaRequest(string Code);

public record DisableMfaRequest(string Password, string? Code = null, string? RecoveryCode = null);

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string? PhoneNumber);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record CreateUserRequest(string FirstName, string LastName, string Email, string Password, string? PhoneNumber);

public record UpdateUserRequest(string FirstName, string LastName, string? PhoneNumber, bool IsActive);

public record CreateRoleRequest(string Name, string? Description);

public record UpdateRoleRequest(string Name, string? Description);

public record UpdateRolePermissionsRequest(int[] PermissionIds);

public record UpdateUserPermissionOverridesRequest(UpdateUserPermissionOverrideItem[] Overrides);

public record UpdateUserPermissionOverrideItem(int PermissionId, bool IsAllowed, string? Reason);

public record AssignUserRolesRequest(string[] RoleIds);

public record UserScopeItemRequest(string ScopeType, int? DepartmentId, int? UnitId, string? TargetId, string? KpiId, string? ProjectId, string? TaskId);

public record UpdateUserScopesRequest(UserScopeItemRequest[] Scopes);

public record UserAssignmentItemRequest(
    string AssignmentType,
    string? DelegatorUserId,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string? TargetId,
    string? KpiId,
    string? ProjectId,
    string? TaskId);

public record UpdateUserAssignmentsRequest(UserAssignmentItemRequest[] Assignments);

public record CreateDepartmentRequest(string Code, string Name, string? Description);

public record UpdateDepartmentRequest(string Code, string Name, string? Description);

public record CreateUnitRequest(int DepartmentId, string Code, string Name);

public record UpdateUnitRequest(int DepartmentId, string Code, string Name);

public record CreatePermissionRequest(string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record UpdatePermissionRequest(string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record CheckPermissionRequest(string PermissionCode);

public record SimulateAccessRequest(
    string? UserId,
    string? Role,
    int? DepartmentId,
    int? UnitId,
    string? OwnerUserId,
    string? DelegatorUserId,
    string? TargetId,
    string? KpiId,
    string? ProjectId,
    string? TaskId,
    string PermissionCode)
{
    public Guid? DepartmentPublicId { get; init; }
    public Guid? UnitPublicId { get; init; }
}

public record SaveOpmsTargetTemplateRequest(
    string TemplateCode,
    string TemplateName,
    string IndicatorNumber,
    string TargetName,
    string KpiDescription,
    decimal Baseline,
    decimal AnnualTarget,
    string? AnnualTargetDescription,
    string TargetUnitType,
    string? UnitOfMeasure,
    string? NationalKpa,
    string? MunicipalKpa,
    string? StrategicGoal,
    string? StrategicObjective,
    string? PerformanceObjective,
    string? Outcome,
    string? Output,
    string? PriorityIssue,
    string? BudgetSource,
    string? BudgetType,
    decimal Weight,
    string? KpiType,
    string? IndicatorType,
    string? FunctionalArea,
    string? StandardClassification,
    string? IdpReference,
    string? InternalReference,
    string? FmsLink,
    string? DefaultQuarterlyTargetsJson,
    string? DefaultBudgetInformation,
    string? DefaultPoeRequirements,
    bool IsActive);

public record SaveIpmsTargetTemplateRequest(
    string TemplateCode,
    string TemplateName,
    string TargetName,
    string KpiDescription,
    string? PerformanceArea,
    string? EmployeeLevel,
    string? JobGrade,
    string TargetUnitType,
    string? UnitOfMeasure,
    decimal AnnualTarget,
    string? AnnualTargetDescription,
    decimal Weight,
    string? DefaultRatingMethod,
    string? DefaultScoreScale,
    string? DefaultPoeRequirements,
    string? DefaultTaskTemplatesJson,
    bool LinkedOpmsTargetRequired,
    string? FunctionalArea,
    bool IsActive);

public record SaveOpmsTargetRequest(
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    int? PeriodId,
    int? DepartmentId,
    int? UnitId,
    string? AssignedUserId,
    int[]? WardIds,
    string[]? AdditionalAssigneeIds,
    int[]? VoteNumberIds,
    string IndicatorNumber,
    string NationalKpa,
    string MunicipalKpa,
    int? StrategicGoalId,
    int? StrategicObjectiveId,
    string PerformanceObjective,
    string TargetName,
    string KpiDescription,
    decimal Baseline,
    string? BaselineDescription,
    int? BudgetSourceId,
    int? BudgetTypeId,
    int? UnitOfMeasureId,
    decimal Weight,
    string KpiType,
    string IndicatorType,
    string? FunctionalArea,
    string? StandardClassification,
    string? IdpReference,
    string? InternalReference,
    string? FmsLink,
    bool IsRevised,
    SaveTargetPeriodValueRequest[] PeriodTargets)
{
    public int OriginalOrderNumber { get; init; } = 1;
    public Guid? SdbipLayerPublicId { get; init; }
    public Guid? DepartmentPublicId { get; init; }
    public Guid? UnitPublicId { get; init; }
    public Guid? NationalKpaPublicId { get; init; }
    public Guid? MunicipalKpaPublicId { get; init; }
    public Guid? BackToBasicsPillarPublicId { get; init; }
    public Guid? StrategicGoalPublicId { get; init; }
    public Guid? StrategicInterventionPublicId { get; init; }
    public Guid? StrategicObjectivePublicId { get; init; }
    public Guid? PerformanceObjectivePublicId { get; init; }
    public Guid? BudgetTypePublicId { get; init; }
    public SaveKpiBudgetSourceRequest[] BudgetSources { get; init; } = [];
    public Guid? KpiTypePublicId { get; init; }
    public Guid? IndicatorTypePublicId { get; init; }
    public Guid? FunctionalAreaPublicId { get; init; }
    public Guid? StandardClassificationPublicId { get; init; }
    public Guid? KpiUnitOfMeasurePublicId { get; init; }
}

public sealed record SaveKpiBudgetSourceRequest(Guid BudgetSourcePublicId, decimal? Amount);

public record SaveIpmsTargetRequest(
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    string? RelatedOpmsTargetId,
    int? PeriodId,
    int? DepartmentId,
    int? UnitId,
    string? AssignedUserId,
    string? SupervisorId,
    string IndicatorNumber,
    string NationalKpa,
    string MunicipalKpa,
    int? StrategicGoalId,
    int? StrategicObjectiveId,
    string PerformanceObjective,
    string TargetName,
    string KpiDescription,
    decimal Baseline,
    int? BudgetSourceId,
    int? BudgetTypeId,
    int? UnitOfMeasureId,
    decimal Weight,
    string KpiType,
    string IndicatorType,
    string? FunctionalArea,
    string? IdpReference,
    string? InternalReference,
    bool IsRevised,
    SaveTargetPeriodValueRequest[] PeriodTargets)
{
    public int OriginalOrderNumber { get; init; } = 1;
    public Guid? DepartmentPublicId { get; init; }
    public Guid? UnitPublicId { get; init; }
    public Guid? NationalKpaPublicId { get; init; }
    public Guid? MunicipalKpaPublicId { get; init; }
    public Guid? BackToBasicsPillarPublicId { get; init; }
    public Guid? StrategicGoalPublicId { get; init; }
    public Guid? StrategicInterventionPublicId { get; init; }
    public Guid? StrategicObjectivePublicId { get; init; }
    public Guid? PerformanceObjectivePublicId { get; init; }
    public Guid? BudgetTypePublicId { get; init; }
    public SaveKpiBudgetSourceRequest[] BudgetSources { get; init; } = [];
    public Guid? KpiTypePublicId { get; init; }
    public Guid? IndicatorTypePublicId { get; init; }
    public Guid? FunctionalAreaPublicId { get; init; }
    public Guid? KpiUnitOfMeasurePublicId { get; init; }
}

public record SaveTargetPeriodValueRequest(
    ReportingPeriodType PeriodType,
    PerformanceUnitKind UnitKind,
    PerformanceDirection Direction,
    string TargetValue,
    decimal? BudgetValue,
    string? Description)
{
    public Guid? OpmsUnitPublicId { get; init; }
    public Guid? PerformanceDirectionPublicId { get; init; }
}

public record SaveOpmsSubmissionRequest(
    string OpmsTargetId,
    string Quarter,
    string? ActualPerformance,
    decimal? ActualExpenditure,
    string? VarianceReason,
    string? CorrectiveMeasure,
    decimal? SubmitterScore,
    string? PoeType,
    DateTime? DueDate = null,
    DateTime? ExtendedDueDate = null);

public record SaveIpmsSubmissionRequest(
    string IpmsTargetId,
    string Quarter,
    string? ActualPerformance,
    decimal? ActualExpenditure,
    string? VarianceReason,
    string? CorrectiveMeasure,
    decimal? SubmitterScore,
    string? PoeType,
    DateTime? DueDate = null,
    DateTime? ExtendedDueDate = null);

public record SubmissionWorkflowActionRequest(
    string? Comment,
    decimal? Score = null,
    string? Recommendation = null,
    DateTime? ResponseDueDate = null,
    string? RfiComment = null);

public record DueDateExtensionRequest(DateTime ExtendedDueDate, string Reason, int? ExtendedByDays = null);

public record WithdrawGovernedRecordRequest(string Reason, string RowVersion);

public sealed record ReviseKpiOrderingRequest(
    int OriginalOrderNumber,
    int RevisedOrderNumber,
    string Reason,
    string ApprovalReference,
    DateTime EffectiveAt,
    string RowVersion);

public sealed record ReviseKpiDefinitionRequest(
    bool IsIndicatorNumberRevised,
    string? RevisedIndicatorNumber,
    bool IsTargetNameRevised,
    string? RevisedTargetName,
    bool IsKpiDescriptionRevised,
    string? RevisedKpiDescription,
    string Reason,
    string ApprovalReference,
    DateTime EffectiveAt,
    string RowVersion);

public sealed record SaveConsolidatedActualRequest(string ActualPerformance, string? EditReason, string RowVersion);

public record CreateIdpPlanRequest(
    string MunicipalityName,
    string PlanTitle,
    string PlanCode,
    int StartFinancialYear,
    int EndFinancialYear,
    Guid? PredecessorPlanPublicId = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    string? PublicationReference = null);

public record UpdateIdpPlanRequest(
    string PlanTitle,
    int StartFinancialYear,
    int EndFinancialYear,
    string Status,
    string? RowVersion = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    string? PublicationReference = null);

public record CreateIdpPlanVersionRequest(
    string VersionType,
    string VersionLabel,
    string? ReviewYear,
    string? SummaryOfChanges,
    DateTime? EffectiveFrom = null,
    string? PublicationReference = null);

public record CreateIdpStrategicOutcomeRequest(
    int IdpPlanId,
    string Code,
    string Name,
    string Description,
    int SortOrder);

public record CreateIdpStrategicObjectiveRequest(
    int IdpStrategicOutcomeId,
    string Code,
    string Name,
    string Description,
    decimal BaselineValue,
    decimal TargetValue,
    int? ResponsibleDepartmentId,
    string? StrategicOwnerUserId,
    DateTime StartDate,
    DateTime EndDate,
    decimal BudgetAllocation,
    int SortOrder);

public record CreateIdpDevelopmentPriorityRequest(
    int IdpStrategicObjectiveId,
    string Name,
    string Description,
    int SortOrder,
    string? PriorityCode = null);

public record CreateIdpProgrammeRequest(
    int IdpDevelopmentPriorityId,
    string ProgrammeCode,
    string Name,
    string Description,
    int? ResponsibleDepartmentId,
    decimal PlannedBudget,
    decimal ApprovedBudget,
    decimal ActualExpenditure);

public record CreateIdpProjectRequest(
    int IdpProgrammeId,
    string ProjectCode,
    string ProjectName,
    string Description,
    string Category,
    int? DepartmentId,
    decimal Budget,
    string FundingSource,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    string? CommunityNeedReference);

public record CreateIdpKpiRequest(
    int IdpProjectId,
    string KpiCode,
    string KpiName,
    string Description,
    string Formula,
    decimal Baseline,
    decimal AnnualTarget,
    decimal FiveYearTarget,
    int? ResponsibleDepartmentId,
    string DataSource,
    string ReportingFrequency,
    string IndicatorType,
    bool Circular88Linked,
    bool TreasuryTidLinked);

public record StageIdpKpiImportRequest(
    Guid ClientRequestId,
    string SourceFileName,
    IdpKpiImportRowRequest[] Rows);

public record IdpKpiImportRowRequest(
    int SourceRowNumber,
    string ProjectCode,
    string KpiCode,
    string KpiName,
    string Description,
    string Formula,
    decimal Baseline,
    decimal AnnualTarget,
    decimal FiveYearTarget,
    string? ResponsibleDepartmentCode,
    string DataSource,
    string ReportingFrequency,
    string IndicatorType,
    bool Circular88Linked,
    bool TreasuryTidLinked);

public record StageIdpHierarchyImportRequest(
    Guid ClientRequestId,
    string SourceFileName,
    IdpHierarchyImportRowRequest[] Rows);

public record IdpHierarchyImportRowRequest(
    int SourceRowNumber,
    string OutcomeCode,
    string OutcomeName,
    string OutcomeDescription,
    int OutcomeSortOrder,
    string ObjectiveCode,
    string ObjectiveName,
    string ObjectiveDescription,
    decimal ObjectiveBaseline,
    decimal ObjectiveTarget,
    string? ObjectiveDepartmentCode,
    DateTime ObjectiveStartDate,
    DateTime ObjectiveEndDate,
    decimal ObjectiveBudget,
    int ObjectiveSortOrder,
    string PriorityCode,
    string PriorityName,
    string PriorityDescription,
    int PrioritySortOrder,
    string ProgrammeCode,
    string ProgrammeName,
    string ProgrammeDescription,
    string? ProgrammeDepartmentCode,
    decimal ProgrammePlannedBudget,
    decimal ProgrammeApprovedBudget,
    decimal ProgrammeActualExpenditure,
    string ProjectCode,
    string ProjectName,
    string ProjectDescription,
    string ProjectCategory,
    string? ProjectDepartmentCode,
    decimal ProjectBudget,
    string ProjectFundingSource,
    DateTime ProjectStartDate,
    DateTime ProjectEndDate,
    string ProjectStatus,
    string? CommunityNeedReference);

public record CommitIdpImportRequest(string RowVersion, string Reason);

public record CreateIdpAnnualTargetRequest(
    int IdpKpiId,
    int FinancialYear,
    decimal TargetValue,
    decimal? ActualValue,
    string? ProgressComment);

public record CreateIdpAlignmentLinkRequest(
    int IdpStrategicObjectiveId,
    string FrameworkType,
    string FrameworkReferenceCode,
    string FrameworkReferenceTitle,
    string? Notes);

public record CreateIdpCommunitySessionRequest(
    int IdpPlanId,
    string ParticipationType,
    DateTime SessionDate,
    string Venue,
    int? WardId,
    int ParticipantsCount,
    string? AttendanceRegisterPath,
    string? MinutesPath);

public record CreateIdpCommunityNeedRequest(
    int IdpCommunitySessionId,
    string IssueCategory,
    string Description,
    string PriorityLevel,
    string? ProposedIntervention);

public record CreateIdpWardInputRequest(
    int IdpPlanId,
    int WardId,
    string WardPlanSummary,
    string WardPriorities,
    string WardProjects);

public record CreateIdpStakeholderEngagementRequest(
    int IdpCommunitySessionId,
    string StakeholderType,
    string StakeholderName,
    string? ContactPerson,
    string? ContactEmail,
    string? KeyInput);

public record CreateIdpRiskLinkRequest(
    int? IdpStrategicObjectiveId,
    int? IdpProjectId,
    int? IdpKpiId,
    string RiskReference,
    string RiskTitle,
    string? MitigationPlan,
    string RiskLevel);

public record CreateIdpBudgetSnapshotRequest(
    int? IdpStrategicObjectiveId,
    int? IdpProjectId,
    int FinancialYear,
    decimal PlannedBudget,
    decimal ApprovedBudget,
    decimal ActualExpenditure,
    string SourceSystem);

public record CreateIdpDocumentRequest(
    int IdpPlanId,
    int? IdpPlanVersionId,
    string Category,
    string Title,
    string FileName,
    string StoragePath,
    string? ContentType,
    long SizeInBytes,
    int VersionNumber,
    bool IsApproved);

public record CreateIdpCommentRequest(
    int IdpPlanId,
    int? IdpPlanVersionId,
    string EntityName,
    string EntityId,
    string Comment);

public record CreateIdpTaskRequest(
    int IdpPlanId,
    int? IdpPlanVersionId,
    string Title,
    string Description,
    string AssignedToUserId,
    DateTime DueDate);

public record CompleteIdpTaskRequest(bool IsCompleted);

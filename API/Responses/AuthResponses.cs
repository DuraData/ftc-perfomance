using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Host.API.Responses;

public interface IApiResponse
{
    bool Success { get; }
    string? Message { get; }
    string[]? Errors { get; }
}

public record ApiResponse<T>(bool Success, T? Data, string? Message = null, string[]? Errors = null) : IApiResponse;

public record LoginResponse(DateTime ExpiresAt, UserProfileResponse User, string[] Roles, string[] Permissions, MenuItemResponse[] Menu, bool MfaEnrollmentRequired = false);

public record MfaStatusResponse(bool IsEnabled, bool EnrollmentRequired, int RecoveryCodesLeft);

public record MfaSetupResponse(string SharedKey, string AuthenticatorUri);

public record MfaEnableResponse(string[] RecoveryCodes);

public record UserProfileResponse(Guid PublicId, string UserName, string FirstName, string LastName, string FullName, string Email, string? PhoneNumber, string? Department, string? Position, bool IsActive, bool MustChangePassword);

public record RoleResponse(Guid PublicId, string Name, string? Description, bool IsSystemRole, bool IsActive);

public record PermissionResponse(int Id, string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record MenuItemResponse(string Label, string? Path, string? Icon, MenuItemResponse[]? Children, bool IsDivider, string? Code = null);

public record LoginAuditLogResponse(Guid PublicId, Guid? UserPublicId, string? Email, string? IpAddress, string? UserAgent, bool Success, string? FailureReason, DateTime LoggedAt);

public record UserResponse(Guid PublicId, string UserName, string FirstName, string LastName, string FullName, string? Email, string? PhoneNumber, string? Department, string? Position, bool IsActive, bool MustChangePassword, DateTime? LastLoginAt)
{
    public string RowVersion { get; init; } = string.Empty;
}

public record UserDetailResponse(UserResponse User, RoleResponse[] Roles);

public record DemoUserResponse(string Role, string FullName, string Department, string Position, string Email, string UserName, string Password);

public record RolePermissionResponse(int PermissionId, string Code, bool IsAllowed);

public record UserPermissionOverrideResponse(int PermissionId, string Code, bool IsAllowed, string? Reason);

public record UserPermissionsResponse(string[] FromRoles, UserPermissionOverrideResponse[] Overrides, string[] Effective);

public record PermissionGroupResponse(string Module, string Feature, PermissionResponse[] Permissions);

public record DepartmentResponse(int Id, string Code, string Name, string? Description)
{
    public Guid PublicId { get; init; }
}

public record UnitResponse(int Id, int DepartmentId, string DepartmentName, string Code, string Name)
{
    public Guid PublicId { get; init; }
}

public record UserScopeResponse(
    Guid PublicId,
    string ScopeType,
    int? DepartmentId,
    string? DepartmentName,
    int? UnitId,
    string? UnitName,
    string? TargetId,
    string? KpiId,
    string? ProjectId,
    string? TaskId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    string RowVersion);

public record UserAssignmentResponse(
    Guid PublicId,
    string AssignmentType,
    Guid? DelegatorUserPublicId,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string? TargetId,
    string? KpiId,
    string? ProjectId,
    string? TaskId,
    string RowVersion);

public record RoleImplementationAuditResponse(
    string Role,
    bool Dashboard,
    bool Menus,
    bool Crud,
    bool ScopeFiltering,
    bool Notifications,
    bool Reports,
    bool AuditTrail,
    bool Complete);

public record AccessSimulationResponse(
    bool Allowed,
    string Reason,
    string[] EffectivePermissions,
    string[] MatchedScopes,
    string[] MatchedAssignments);

public record RoleAccessMatrixResponse(
    string Role,
    string[] Permissions,
    string[] Scope,
    string[] Menus,
    string[] AllowedActions,
    string[] Reports,
    string? TestUser);

public record SystemCoverageAuditResponse(
    string Role,
    bool SeededUser,
    bool Dashboard,
    bool Menu,
    bool Permissions,
    bool ScopeFiltering,
    bool Crud,
    bool WorkflowActions,
    bool Reports,
    bool AuditTrail,
    bool Notifications);

public record OpmsTargetTemplateResponse(
    Guid PublicId,
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
    bool IsActive,
    bool IsArchived,
    int Version,
    string? CreatedBy,
    DateTime CreatedDate,
    string RowVersion);

public record IpmsTargetTemplateResponse(
    Guid PublicId,
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
    bool IsActive,
    bool IsArchived,
    int Version,
    string? CreatedBy,
    DateTime CreatedDate,
    string RowVersion);

public record OpmsTargetResponse(
    string Id,
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    int? PeriodId,
    int? DepartmentId,
    string? DepartmentName,
    int? UnitId,
    string? UnitName,
    Guid? AssignedUserPublicId,
    string? AssignedUserName,
    int[] WardIds,
    Guid[] AdditionalAssigneePublicIds,
    int[] VoteNumberIds,
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
    bool IsWithdrawn,
    string? ReasonForWithdrawal,
    TargetPeriodValueResponse[] PeriodTargets,
    DateTime CreatedAt)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public int OriginalOrderNumber { get; init; }
    public int RevisedOrderNumber { get; init; }
    public bool IsIndicatorNumberRevised { get; init; }
    public string? RevisedIndicatorNumber { get; init; }
    public bool IsTargetNameRevised { get; init; }
    public string? RevisedTargetName { get; init; }
    public bool IsKpiDescriptionRevised { get; init; }
    public string? RevisedKpiDescription { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public Guid? WithdrawnByUserPublicId { get; init; }
    public string? WithdrawnByName { get; init; }
    public Guid? SdbipLayerPublicId { get; init; }
    public Guid? MunicipalityFinancialYearPublicId { get; init; }
    public string? MunicipalityFinancialYearName { get; init; }
    public string? SdbipLayerCode { get; init; }
    public string? SdbipLayerName { get; init; }
    public Guid? DepartmentPublicId { get; init; }
    public Guid? UnitPublicId { get; init; }
    public Guid? NationalKpaPublicId { get; init; }
    public Guid? MunicipalKpaPublicId { get; init; }
    public Guid? BackToBasicsPillarPublicId { get; init; }
    public Guid? StrategicGoalPublicId { get; init; }
    public string? StrategicGoalCode { get; init; }
    public string? StrategicGoalName { get; init; }
    public Guid? StrategicInterventionPublicId { get; init; }
    public Guid? StrategicObjectivePublicId { get; init; }
    public string? StrategicObjectiveCode { get; init; }
    public string? StrategicObjectiveName { get; init; }
    public Guid? PerformanceObjectivePublicId { get; init; }
    public string? PerformanceObjectiveCode { get; init; }
    public string? PerformanceObjectiveName { get; init; }
    public string? BackToBasicsPillar { get; init; }
    public string? StrategicIntervention { get; init; }
    public Guid? BudgetTypePublicId { get; init; }
    public string? BudgetTypeName { get; init; }
    public KpiBudgetSourceResponse[] BudgetSources { get; init; } = [];
    public Guid? KpiTypePublicId { get; init; }
    public Guid? IndicatorTypePublicId { get; init; }
    public Guid? FunctionalAreaPublicId { get; init; }
    public Guid? StandardClassificationPublicId { get; init; }
    public Guid? KpiUnitOfMeasurePublicId { get; init; }
    public string? KpiUnitOfMeasureName { get; init; }
    public string? KpiUnitOfMeasureSymbol { get; init; }
    public OpmsTargetVoteNumberResponse[] VoteNumbers { get; init; } = [];
}

public sealed record KpiBudgetSourceResponse(Guid PublicId, Guid BudgetSourcePublicId, string Code, string Name, decimal? Amount);
public sealed record OpmsTargetVoteNumberResponse(int Id, Guid PublicId, string Code, string Number, string Name, decimal Amount);

public record IpmsTargetResponse(
    string Id,
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    string? RelatedOpmsTargetId,
    int? PeriodId,
    int? DepartmentId,
    string? DepartmentName,
    int? UnitId,
    string? UnitName,
    Guid? AssignedUserPublicId,
    string? AssignedUserName,
    Guid? SupervisorPublicId,
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
    TargetPeriodValueResponse[] PeriodTargets,
    DateTime CreatedAt)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public int OriginalOrderNumber { get; init; }
    public int RevisedOrderNumber { get; init; }
    public bool IsIndicatorNumberRevised { get; init; }
    public string? RevisedIndicatorNumber { get; init; }
    public bool IsTargetNameRevised { get; init; }
    public string? RevisedTargetName { get; init; }
    public bool IsKpiDescriptionRevised { get; init; }
    public string? RevisedKpiDescription { get; init; }
    public bool IsWithdrawn { get; init; }
    public string? ReasonForWithdrawal { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public Guid? WithdrawnByUserPublicId { get; init; }
    public string? WithdrawnByName { get; init; }
    public Guid? DepartmentPublicId { get; init; }
    public Guid? MunicipalityFinancialYearPublicId { get; init; }
    public Guid? UnitPublicId { get; init; }
    public Guid? NationalKpaPublicId { get; init; }
    public Guid? MunicipalKpaPublicId { get; init; }
    public Guid? BackToBasicsPillarPublicId { get; init; }
    public Guid? StrategicGoalPublicId { get; init; }
    public Guid? StrategicInterventionPublicId { get; init; }
    public Guid? StrategicObjectivePublicId { get; init; }
    public Guid? PerformanceObjectivePublicId { get; init; }
    public string? BackToBasicsPillar { get; init; }
    public string? StrategicIntervention { get; init; }
    public Guid? BudgetTypePublicId { get; init; }
    public string? BudgetTypeName { get; init; }
    public KpiBudgetSourceResponse[] BudgetSources { get; init; } = [];
    public Guid? KpiTypePublicId { get; init; }
    public Guid? IndicatorTypePublicId { get; init; }
    public Guid? FunctionalAreaPublicId { get; init; }
    public Guid? KpiUnitOfMeasurePublicId { get; init; }
    public string? KpiUnitOfMeasureName { get; init; }
    public string? KpiUnitOfMeasureSymbol { get; init; }
}

public record TargetPeriodValueResponse(
    Guid PublicId,
    Guid ReportingPeriodPublicId,
    string PeriodCode,
    ReportingPeriodType PeriodType,
    PerformanceUnitKind UnitKind,
    PerformanceDirection Direction,
    string? TargetValue,
    decimal? BudgetValue,
    string? Description,
    bool IsActive,
    string RowVersion)
{
    public PerformanceUnitKind OriginalUnitKind { get; init; }
    public string? OriginalTargetValue { get; init; }
    public decimal? OriginalBudgetValue { get; init; }
    public bool IsTargetRevised { get; init; }
    public PerformanceUnitKind? RevisedUnitKind { get; init; }
    public string? RevisedTargetValue { get; init; }
    public bool IsBudgetRevised { get; init; }
    public decimal? RevisedBudgetValue { get; init; }
}

public sealed record KpiFieldRevisionResponse(
    Guid PublicId,
    string FieldName,
    string? OriginalValue,
    string? RevisedValue,
    string? Reason,
    string? ApprovalReference,
    DateTime EffectiveAt,
    Guid? RevisedByUserPublicId,
    string? RevisedByName,
    DateTime RecordedAt);

public sealed record KpiRevisionMemberAccess(
    bool OriginalValue,
    bool RevisedValue,
    bool Reason,
    bool ApprovalReference,
    bool Actor);

public sealed record PeriodTargetMemberAccess(bool TargetValue, bool BudgetValue, bool Description)
{
    public static PeriodTargetMemberAccess Full { get; } = new(true, true, true);
    public static PeriodTargetMemberAccess None { get; } = new(false, false, false);
}

public sealed record KpiLifecycleMemberAccess(bool WithdrawalReason, bool WithdrawalActor)
{
    public static KpiLifecycleMemberAccess Full { get; } = new(true, true);
    public static KpiLifecycleMemberAccess None { get; } = new(false, false);
}

public record OpmsSubmissionResponse(
    string Id,
    string OpmsTargetId,
    string TargetName,
    string TargetIndicatorNumber,
    string Quarter,
    string Status,
    string SubmitterStatus,
    string VerifierStatus,
    string ApproverStatus,
    string PmsStatus,
    string AuditorStatus,
    string? ActualPerformance,
    decimal? ActualExpenditure,
    decimal? Variance,
    string? VarianceReason,
    string? CorrectiveMeasure,
    decimal? SubmitterScore,
    DateTime? SubmittedAt,
    Guid? SubmittedByUserPublicId,
    string? SubmittedByName,
    Guid? VerifierUserPublicId,
    string? VerifierName,
    DateTime? VerifiedAt,
    string? VerifierComments,
    string? VerifierComment,
    decimal? VerifierScore,
    Guid? ApproverUserPublicId,
    string? ApproverName,
    DateTime? ApprovedAt,
    string? ApproverComments,
    string? ApproverComment,
    decimal? ApproverScore,
    Guid? PmsOfficerUserPublicId,
    string? PmsOfficerName,
    DateTime? PmsReviewedAt,
    string? PmsComments,
    string? PmsComment,
    string? PmsRecommendation,
    decimal? PmsScore,
    DateTime? PmsResponseDueDate,
    string? PmsRfiComment,
    Guid? AuditorUserPublicId,
    string? AuditorName,
    DateTime? AuditedAt,
    string? AuditorComments,
    string? AuditorComment,
    string? AuditorRecommendation,
    decimal? AuditorScore,
    DateTime? AuditorResponseDueDate,
    DateTime? DueDate,
    DateTime? ExtendedDueDate,
    int? DueDateExtendedDays,
    string? PoeType,
    bool IsDisabled,
    string? CreatedBy,
    DateTime CreatedOn,
    string? UpdatedBy,
    DateTime? UpdatedOn,
    string? OrganisationId,
    DateTime CreatedAt)
{
    public string BaseState { get; init; } = "IN_PROGRESS";
    public Guid? ReportingPeriodPublicId { get; init; }
    public Guid? TargetDepartmentPublicId { get; init; }
    public string? TargetDepartmentName { get; init; }
    public Guid? TargetUnitPublicId { get; init; }
    public string? TargetUnitName { get; init; }
    public string? TargetFinancialYearName { get; init; }
    public string? TargetUnitOfMeasureName { get; init; }
    public string? TargetUnitOfMeasureSymbol { get; init; }
    public string? TargetUnitType { get; init; }
    public string? SystemSuggestedActualPerformance { get; init; }
    public bool WasSystemSuggestionEdited { get; init; }
    public DateTime? SuggestionGeneratedDate { get; init; }
    public Guid? SuggestionEditedByUserPublicId { get; init; }
    public string? SuggestionEditedByName { get; init; }
    public DateTime? SuggestionEditedAt { get; init; }
    public string? SuggestionEditReason { get; init; }
    public decimal? AchievementPercent { get; init; }
    public bool? TargetAchieved { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string? WithdrawalReason { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public Guid? WithdrawnByUserPublicId { get; init; }
    public string? WithdrawnByName { get; init; }
}

public record IpmsSubmissionResponse(
    string Id,
    string IpmsTargetId,
    string TargetName,
    string TargetIndicatorNumber,
    string Quarter,
    string Status,
    string SubmitterStatus,
    string VerifierStatus,
    string ApproverStatus,
    string PmsStatus,
    string AuditorStatus,
    string? ActualPerformance,
    decimal? ActualExpenditure,
    decimal? Variance,
    string? VarianceReason,
    string? CorrectiveMeasure,
    decimal? SubmitterScore,
    DateTime? SubmittedAt,
    Guid? SubmittedByUserPublicId,
    string? SubmittedByName,
    Guid? VerifierUserPublicId,
    string? VerifierName,
    DateTime? VerifiedAt,
    string? VerifierComments,
    string? VerifierComment,
    decimal? VerifierScore,
    Guid? ApproverUserPublicId,
    string? ApproverName,
    DateTime? ApprovedAt,
    string? ApproverComments,
    string? ApproverComment,
    decimal? ApproverScore,
    Guid? PmsOfficerUserPublicId,
    string? PmsOfficerName,
    DateTime? PmsReviewedAt,
    string? PmsComments,
    string? PmsComment,
    string? PmsRecommendation,
    decimal? PmsScore,
    DateTime? PmsResponseDueDate,
    string? PmsRfiComment,
    Guid? AuditorUserPublicId,
    string? AuditorName,
    DateTime? AuditedAt,
    string? AuditorComments,
    string? AuditorComment,
    string? AuditorRecommendation,
    decimal? AuditorScore,
    DateTime? AuditorResponseDueDate,
    DateTime? DueDate,
    DateTime? ExtendedDueDate,
    int? DueDateExtendedDays,
    string? PoeType,
    bool IsDisabled,
    string? CreatedBy,
    DateTime CreatedOn,
    string? UpdatedBy,
    DateTime? UpdatedOn,
    string? OrganisationId,
    DateTime CreatedAt)
{
    public string BaseState { get; init; } = "IN_PROGRESS";
    public Guid? ReportingPeriodPublicId { get; init; }
    public Guid? TargetDepartmentPublicId { get; init; }
    public string? TargetDepartmentName { get; init; }
    public Guid? TargetUnitPublicId { get; init; }
    public string? TargetUnitName { get; init; }
    public string? TargetFinancialYearName { get; init; }
    public string? TargetUnitOfMeasureName { get; init; }
    public string? TargetUnitOfMeasureSymbol { get; init; }
    public string? TargetUnitType { get; init; }
    public string? SystemSuggestedActualPerformance { get; init; }
    public bool WasSystemSuggestionEdited { get; init; }
    public DateTime? SuggestionGeneratedDate { get; init; }
    public Guid? SuggestionEditedByUserPublicId { get; init; }
    public string? SuggestionEditedByName { get; init; }
    public DateTime? SuggestionEditedAt { get; init; }
    public string? SuggestionEditReason { get; init; }
    public decimal? AchievementPercent { get; init; }
    public bool? TargetAchieved { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string? WithdrawalReason { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public Guid? WithdrawnByUserPublicId { get; init; }
    public string? WithdrawnByName { get; init; }
}

public sealed record PerformanceSuggestionEventResponse(
    Guid PublicId,
    string EventType,
    string? SystemSuggestedActualPerformance,
    string? ActualPerformance,
    bool WasSystemSuggestionEdited,
    string? EffectiveCalculationType,
    string[] SourcePeriods,
    Guid? ActorUserPublicId,
    string? ActorName,
    string? Reason,
    DateTime OccurredAt,
    string? CorrelationId);

public record NotificationResponse(
    Guid PublicId,
    Guid RecipientUserPublicId,
    string RecipientName,
    string Type,
    string Title,
    string Message,
    string? EntityName,
    bool IsRead,
    DateTime CreatedAt);

public sealed record NotificationPageResponse(
    NotificationResponse[] Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int UnreadCount)
{
    public static NotificationPageResponse Create(IEnumerable<NotificationResponse> items, int page, int pageSize, int totalCount, int unreadCount) =>
        new(items.ToArray(), page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize), unreadCount);
}

public record AuditTrailEntryResponse(
    Guid PublicId,
    long? MunicipalityId,
    string EntityName,
    string? EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    string? ChangedBy,
    DateTime ChangedAt,
    string? IpAddress,
    string? CorrelationId,
    string? Reason,
    string? UserAgent,
    string? SessionId);

public record IdpPlanSummaryResponse(
    int Id,
    Guid PublicId,
    string MunicipalityName,
    string PlanTitle,
    string PlanCode,
    int StartFinancialYear,
    int EndFinancialYear,
    string Status,
    int CurrentVersionNumber,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    string RowVersion,
    Guid PlanFamilyId,
    Guid? PredecessorPlanPublicId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime? PublishedAt,
    string? PublicationReference);

public record IdpPlanVersionResponse(
    int Id,
    Guid PublicId,
    int IdpPlanId,
    Guid? PredecessorVersionPublicId,
    int VersionNumber,
    string VersionType,
    string VersionLabel,
    string? ReviewYear,
    string? SummaryOfChanges,
    bool IsActive,
    DateTime CreatedAt,
    Guid? CreatedByUserPublicId,
    string? CreatedByName,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime? PublishedAt,
    string? PublicationReference,
    string RowVersion);

public record IdpHierarchyPathResponse(
    Guid IdpPlanPublicId,
    Guid OutcomePublicId,
    string OutcomeCode,
    string OutcomeName,
    Guid ObjectivePublicId,
    string ObjectiveCode,
    string ObjectiveName,
    Guid? ObjectiveStrategicOwnerPublicId,
    string? ObjectiveStrategicOwnerName,
    decimal? ObjectiveBudgetAllocation,
    Guid PriorityPublicId,
    string PriorityCode,
    string PriorityName,
    Guid ProgrammePublicId,
    string ProgrammeCode,
    string ProgrammeName,
    decimal? ProgrammePlannedBudget,
    decimal? ProgrammeApprovedBudget,
    decimal? ProgrammeActualExpenditure,
    Guid ProjectPublicId,
    string ProjectCode,
    string ProjectName,
    decimal? ProjectBudget,
    string? ProjectFundingSource,
    Guid KpiPublicId,
    string KpiCode,
    string KpiName);

public record IdpStrategicOutcomeResponse(int Id, int IdpPlanId, string Code, string Name, string Description, int SortOrder)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record IdpStrategicObjectiveResponse(
    int Id,
    int IdpStrategicOutcomeId,
    string Code,
    string Name,
    string Description,
    decimal BaselineValue,
    decimal TargetValue,
    int? ResponsibleDepartmentId,
    string? ResponsibleDepartmentName,
    Guid? StrategicOwnerUserPublicId,
    string? StrategicOwnerName,
    DateTime StartDate,
    DateTime EndDate,
    decimal? BudgetAllocation,
    int SortOrder)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record IdpDevelopmentPriorityResponse(int Id, int IdpStrategicObjectiveId, string Name, string Description, int SortOrder)
{
    public Guid PublicId { get; init; }
    public string PriorityCode { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
}

public record IdpProgrammeResponse(
    int Id,
    int IdpDevelopmentPriorityId,
    string ProgrammeCode,
    string Name,
    string Description,
    int? ResponsibleDepartmentId,
    string? ResponsibleDepartmentName,
    decimal? PlannedBudget,
    decimal? ApprovedBudget,
    decimal? ActualExpenditure)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record IdpProjectResponse(
    int Id,
    int IdpProgrammeId,
    string ProjectCode,
    string ProjectName,
    string Description,
    string Category,
    int? DepartmentId,
    string? DepartmentName,
    decimal? Budget,
    string? FundingSource,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    string? CommunityNeedReference)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record IdpKpiResponse(
    int Id,
    int IdpProjectId,
    string KpiCode,
    string KpiName,
    string Description,
    string Formula,
    decimal Baseline,
    decimal AnnualTarget,
    decimal FiveYearTarget,
    int? ResponsibleDepartmentId,
    string? ResponsibleDepartmentName,
    string DataSource,
    string ReportingFrequency,
    string IndicatorType,
    bool Circular88Linked,
    bool TreasuryTidLinked,
    Guid PublicId,
    string RowVersion);

public record IdpImportBatchResponse(
    Guid PublicId,
    Guid? ClientRequestId,
    Guid IdpPlanPublicId,
    string ImportType,
    string? SourceFileName,
    string? SourceSha256,
    string Status,
    int TotalRows,
    int NewRows,
    int UnchangedRows,
    int ChangedRows,
    int InvalidRows,
    Guid? CreatedByUserPublicId,
    string? CreatedByName,
    DateTime CreatedAt,
    Guid? CommittedByUserPublicId,
    string? CommittedByName,
    DateTime? CommittedAt,
    string RowVersion,
    IdpImportRowResponse[] Rows);

public record IdpImportBatchSummaryResponse(
    Guid PublicId,
    Guid? ClientRequestId,
    Guid IdpPlanPublicId,
    string ImportType,
    string? SourceFileName,
    string? SourceSha256,
    string Status,
    int TotalRows,
    int NewRows,
    int UnchangedRows,
    int ChangedRows,
    int InvalidRows,
    Guid? CreatedByUserPublicId,
    string? CreatedByName,
    DateTime CreatedAt,
    Guid? CommittedByUserPublicId,
    string? CommittedByName,
    DateTime? CommittedAt,
    string RowVersion);

public record IdpImportRowResponse(
    Guid PublicId,
    int SourceRowNumber,
    string Reference,
    string Status,
    string? ExistingValueJson,
    string? NormalizedJson,
    string? ErrorCode,
    string? ErrorField,
    string? SuppliedValue,
    string? ErrorMessage);

public record IdpAnnualTargetResponse(Guid PublicId, int IdpKpiId, int FinancialYear, decimal? TargetValue, decimal? ActualValue, string? ProgressComment, string RowVersion);

public record IdpAlignmentLinkResponse(
    Guid PublicId,
    int IdpStrategicObjectiveId,
    string FrameworkType,
    string FrameworkReferenceCode,
    string FrameworkReferenceTitle,
    string? Notes,
    string RowVersion);

public record IdpCommunitySessionResponse(
    Guid PublicId,
    int IdpPlanId,
    string ParticipationType,
    DateTime SessionDate,
    string Venue,
    int? WardId,
    string? WardName,
    int ParticipantsCount,
    string? AttendanceRegisterPath,
    string? MinutesPath,
    string RowVersion);

public record IdpCommunityNeedResponse(Guid PublicId, int IdpCommunitySessionId, string IssueCategory, string Description, string PriorityLevel, string? ProposedIntervention, string RowVersion);

public record IdpWardInputResponse(Guid PublicId, int IdpPlanId, int WardId, string WardName, string WardPlanSummary, string WardPriorities, string WardProjects, string RowVersion);

public record IdpStakeholderEngagementResponse(
    Guid PublicId,
    int IdpCommunitySessionId,
    string StakeholderType,
    string StakeholderName,
    string? ContactPerson,
    string? ContactEmail,
    string? KeyInput,
    string RowVersion);

public record IdpStakeholderEngagementPageItemResponse(
    Guid PublicId,
    Guid CommunitySessionPublicId,
    DateTime SessionDate,
    string Venue,
    string StakeholderType,
    string StakeholderName,
    string? ContactPerson,
    string? ContactEmail,
    string? KeyInput,
    string RowVersion);

public record IdpRiskLinkResponse(
    Guid PublicId,
    int? IdpStrategicObjectiveId,
    int? IdpProjectId,
    int? IdpKpiId,
    string RiskReference,
    string RiskTitle,
    string? MitigationPlan,
    string RiskLevel,
    string RowVersion);

public record IdpBudgetSnapshotResponse(
    Guid PublicId,
    int? IdpStrategicObjectiveId,
    int? IdpProjectId,
    int FinancialYear,
    decimal? PlannedBudget,
    decimal? ApprovedBudget,
    decimal? ActualExpenditure,
    string? SourceSystem,
    DateTime CapturedAt);

public record IdpDocumentResponse(
    Guid PublicId,
    Guid IdpPlanPublicId,
    int? PlanVersionNumber,
    string Category,
    string Title,
    string FileName,
    string DownloadUrl,
    string? ContentType,
    long SizeInBytes,
    int VersionNumber,
    bool IsApproved,
    DateTime UploadedAt,
    Guid? UploadedByUserPublicId,
    string? UploadedByName,
    string Sha256,
    bool SignatureVerified,
    string ScanStatus,
    bool IsQuarantined,
    string? ScannerProvider,
    string? ScannerReference,
    string? ScanDetail,
    DateTime? ScannedAt,
    DateTime? RetainUntil,
    Guid EvidenceBlobPublicId,
    bool IsContentDeleted,
    string RowVersion);

public record IdpCommentResponse(
    Guid PublicId,
    int IdpPlanId,
    int? IdpPlanVersionId,
    string? EntityName,
    string? EntityId,
    string? Comment,
    Guid? CommentedByUserPublicId,
    string? CommentedByName,
    DateTime CommentedAt);

public record IdpTaskResponse(
    Guid PublicId,
    int IdpPlanId,
    int? IdpPlanVersionId,
    string? Title,
    string? Description,
    Guid? AssignedToUserPublicId,
    string? AssignedToName,
    Guid? AssignedByUserPublicId,
    string? AssignedByName,
    DateTime DueDate,
    bool IsCompleted,
    DateTime? CompletedAt,
    string RowVersion);

public record IdpDashboardResponse(
    Guid PlanPublicId,
    string PlanTitle,
    int Outcomes,
    int Objectives,
    int Projects,
    int Kpis,
    int CommunitySessions,
    int Risks,
    decimal? PlannedBudget,
    decimal? ApprovedBudget,
    decimal? ActualExpenditure,
    decimal? KpiAchievementRate,
    string[] TopRiskTitles,
    IdpWardParticipationResponse[] WardParticipation,
    int AlignmentCount);

public record IdpWardParticipationResponse(int WardId, string WardName, int MeetingCount, int ParticipantsCount, int NeedsCaptured);

public record IdpAlignmentMatrixItemResponse(
    string StrategicOutcomeCode,
    string StrategicOutcomeName,
    string ObjectiveCode,
    string ObjectiveName,
    string FrameworkType,
    string FrameworkReferenceCode,
    string FrameworkReferenceTitle);

public record IdpReportDocumentResponse(string ReportName, string ContentType, string FileName, string ContentBase64, long SizeInBytes, string Sha256);

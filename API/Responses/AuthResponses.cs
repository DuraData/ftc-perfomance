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

public record UserProfileResponse(string Id, string UserName, string FirstName, string LastName, string FullName, string Email, string? PhoneNumber, string? Department, string? Position, bool IsActive, bool MustChangePassword);

public record RoleResponse(string Id, string Name, string? Description, bool IsSystemRole, bool IsActive);

public record PermissionResponse(int Id, string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record MenuItemResponse(string Label, string? Path, string? Icon, MenuItemResponse[]? Children, bool IsDivider, string? Code = null);

public record LoginAuditLogResponse(int Id, string? UserId, string Email, string? IpAddress, string? UserAgent, bool Success, string? FailureReason, DateTime LoggedAt);

public record UserResponse(string Id, string UserName, string FirstName, string LastName, string FullName, string? Email, string? PhoneNumber, string? Department, string? Position, bool IsActive, bool MustChangePassword, DateTime? LastLoginAt)
{
    public Guid PublicId { get; init; }
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

public record UserScopeResponse(int Id, string ScopeType, int? DepartmentId, string? DepartmentName, int? UnitId, string? UnitName, string? TargetId, string? KpiId, string? ProjectId, string? TaskId);

public record UserAssignmentResponse(
    int Id,
    string AssignmentType,
    string? DelegatorUserId,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string? TargetId,
    string? KpiId,
    string? ProjectId,
    string? TaskId);

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
    int Id,
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
    DateTime CreatedDate);

public record IpmsTargetTemplateResponse(
    int Id,
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
    DateTime CreatedDate);

public record OpmsTargetResponse(
    string Id,
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    int? PeriodId,
    int? DepartmentId,
    string? DepartmentName,
    int? UnitId,
    string? UnitName,
    string? AssignedUserId,
    string? AssignedUserName,
    int[] WardIds,
    string[] AdditionalAssigneeIds,
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
    public DateTime? WithdrawnAt { get; init; }
    public string? WithdrawnByUserId { get; init; }
}

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
    string? AssignedUserId,
    string? AssignedUserName,
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
    TargetPeriodValueResponse[] PeriodTargets,
    DateTime CreatedAt)
{
    public Guid PublicId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public bool IsWithdrawn { get; init; }
    public string? ReasonForWithdrawal { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public string? WithdrawnByUserId { get; init; }
}

public record TargetPeriodValueResponse(
    Guid PublicId,
    Guid ReportingPeriodPublicId,
    string PeriodCode,
    ReportingPeriodType PeriodType,
    PerformanceUnitKind UnitKind,
    PerformanceDirection Direction,
    string TargetValue,
    decimal? BudgetValue,
    string? Description,
    bool IsActive,
    string RowVersion);

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
    string? SubmittedByUserId,
    string? SubmittedByName,
    string? VerifierUserId,
    string? VerifierName,
    DateTime? VerifiedAt,
    string? VerifierComments,
    string? VerifierComment,
    decimal? VerifierScore,
    string? ApproverUserId,
    string? ApproverName,
    DateTime? ApprovedAt,
    string? ApproverComments,
    string? ApproverComment,
    decimal? ApproverScore,
    string? PmsOfficerUserId,
    string? PmsOfficerName,
    DateTime? PmsReviewedAt,
    string? PmsComments,
    string? PmsComment,
    string? PmsRecommendation,
    decimal? PmsScore,
    DateTime? PmsResponseDueDate,
    string? PmsRfiComment,
    string? AuditorUserId,
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
    public string? SystemSuggestedActualPerformance { get; init; }
    public bool WasSystemSuggestionEdited { get; init; }
    public DateTime? SuggestionGeneratedDate { get; init; }
    public string? SuggestionEditedByUserId { get; init; }
    public DateTime? SuggestionEditedAt { get; init; }
    public string? SuggestionEditReason { get; init; }
    public decimal? AchievementPercent { get; init; }
    public bool? TargetAchieved { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string? WithdrawalReason { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public string? WithdrawnByUserId { get; init; }
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
    string? SubmittedByUserId,
    string? SubmittedByName,
    string? VerifierUserId,
    string? VerifierName,
    DateTime? VerifiedAt,
    string? VerifierComments,
    string? VerifierComment,
    decimal? VerifierScore,
    string? ApproverUserId,
    string? ApproverName,
    DateTime? ApprovedAt,
    string? ApproverComments,
    string? ApproverComment,
    decimal? ApproverScore,
    string? PmsOfficerUserId,
    string? PmsOfficerName,
    DateTime? PmsReviewedAt,
    string? PmsComments,
    string? PmsComment,
    string? PmsRecommendation,
    decimal? PmsScore,
    DateTime? PmsResponseDueDate,
    string? PmsRfiComment,
    string? AuditorUserId,
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
    public string? SystemSuggestedActualPerformance { get; init; }
    public bool WasSystemSuggestionEdited { get; init; }
    public DateTime? SuggestionGeneratedDate { get; init; }
    public string? SuggestionEditedByUserId { get; init; }
    public DateTime? SuggestionEditedAt { get; init; }
    public string? SuggestionEditReason { get; init; }
    public decimal? AchievementPercent { get; init; }
    public bool? TargetAchieved { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string? WithdrawalReason { get; init; }
    public DateTime? WithdrawnAt { get; init; }
    public string? WithdrawnByUserId { get; init; }
}

public sealed record PerformanceSuggestionEventResponse(
    Guid PublicId,
    string EventType,
    string? SystemSuggestedActualPerformance,
    string? ActualPerformance,
    bool WasSystemSuggestionEdited,
    string? EffectiveCalculationType,
    string[] SourcePeriods,
    string ActorUserId,
    string? Reason,
    DateTime OccurredAt,
    string CorrelationId);

public record NotificationResponse(
    string Id,
    string UserId,
    string Type,
    string Title,
    string Message,
    string? EntityName,
    string? EntityId,
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
    long Id,
    Guid PublicId,
    long? MunicipalityId,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    string ChangedBy,
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
    string CreatedByUserId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime? PublishedAt,
    string? PublicationReference,
    string RowVersion);

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
    string? StrategicOwnerUserId,
    string? StrategicOwnerName,
    DateTime StartDate,
    DateTime EndDate,
    decimal BudgetAllocation,
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
    decimal PlannedBudget,
    decimal ApprovedBudget,
    decimal ActualExpenditure)
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
    decimal Budget,
    string FundingSource,
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
    Guid ClientRequestId,
    Guid IdpPlanPublicId,
    string ImportType,
    string SourceFileName,
    string SourceSha256,
    string Status,
    int TotalRows,
    int NewRows,
    int UnchangedRows,
    int ChangedRows,
    int InvalidRows,
    string CreatedByUserId,
    DateTime CreatedAt,
    string? CommittedByUserId,
    DateTime? CommittedAt,
    string RowVersion,
    IdpImportRowResponse[] Rows);

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

public record IdpAnnualTargetResponse(int Id, int IdpKpiId, int FinancialYear, decimal TargetValue, decimal? ActualValue, string? ProgressComment);

public record IdpAlignmentLinkResponse(
    long Id,
    int IdpStrategicObjectiveId,
    string FrameworkType,
    string FrameworkReferenceCode,
    string FrameworkReferenceTitle,
    string? Notes);

public record IdpCommunitySessionResponse(
    int Id,
    int IdpPlanId,
    string ParticipationType,
    DateTime SessionDate,
    string Venue,
    int? WardId,
    string? WardName,
    int ParticipantsCount,
    string? AttendanceRegisterPath,
    string? MinutesPath);

public record IdpCommunityNeedResponse(int Id, int IdpCommunitySessionId, string IssueCategory, string Description, string PriorityLevel, string? ProposedIntervention);

public record IdpWardInputResponse(int Id, int IdpPlanId, int WardId, string WardName, string WardPlanSummary, string WardPriorities, string WardProjects);

public record IdpStakeholderEngagementResponse(
    int Id,
    int IdpCommunitySessionId,
    string StakeholderType,
    string StakeholderName,
    string? ContactPerson,
    string? ContactEmail,
    string? KeyInput);

public record IdpRiskLinkResponse(
    long Id,
    int? IdpStrategicObjectiveId,
    int? IdpProjectId,
    int? IdpKpiId,
    string RiskReference,
    string RiskTitle,
    string? MitigationPlan,
    string RiskLevel);

public record IdpBudgetSnapshotResponse(
    long Id,
    int? IdpStrategicObjectiveId,
    int? IdpProjectId,
    int FinancialYear,
    decimal PlannedBudget,
    decimal ApprovedBudget,
    decimal ActualExpenditure,
    string SourceSystem,
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
    string UploadedByUserId,
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
    long Id,
    int IdpPlanId,
    int? IdpPlanVersionId,
    string EntityName,
    string EntityId,
    string Comment,
    string CommentedByUserId,
    string? CommentedByName,
    DateTime CommentedAt);

public record IdpTaskResponse(
    long Id,
    int IdpPlanId,
    int? IdpPlanVersionId,
    string Title,
    string Description,
    string AssignedToUserId,
    string? AssignedToName,
    string AssignedByUserId,
    string? AssignedByName,
    DateTime DueDate,
    bool IsCompleted,
    DateTime? CompletedAt);

public record IdpHierarchyResponse(
    IdpPlanSummaryResponse Plan,
    IdpPlanVersionResponse[] Versions,
    IdpStrategicOutcomeResponse[] Outcomes,
    IdpStrategicObjectiveResponse[] Objectives,
    IdpDevelopmentPriorityResponse[] Priorities,
    IdpProgrammeResponse[] Programmes,
    IdpProjectResponse[] Projects,
    IdpKpiResponse[] Kpis,
    IdpAnnualTargetResponse[] AnnualTargets,
    IdpAlignmentLinkResponse[] AlignmentLinks,
    IdpRiskLinkResponse[] RiskLinks,
    IdpBudgetSnapshotResponse[] BudgetSnapshots);

public record IdpDashboardResponse(
    int PlanId,
    string PlanTitle,
    int Outcomes,
    int Objectives,
    int Projects,
    int Kpis,
    int CommunitySessions,
    int Risks,
    decimal PlannedBudget,
    decimal ApprovedBudget,
    decimal ActualExpenditure,
    decimal KpiAchievementRate,
    string[] TopRiskTitles,
    IdpWardParticipationResponse[] WardParticipation,
    IdpAlignmentMatrixItemResponse[] AlignmentMatrix);

public record IdpWardParticipationResponse(int WardId, string WardName, int MeetingCount, int ParticipantsCount, int NeedsCaptured);

public record IdpAlignmentMatrixItemResponse(
    string StrategicOutcomeCode,
    string StrategicOutcomeName,
    string ObjectiveCode,
    string ObjectiveName,
    string FrameworkType,
    string FrameworkReferenceCode,
    string FrameworkReferenceTitle);

public record IdpReportDocumentResponse(string ReportName, string ContentType, string FileName, byte[] Content);

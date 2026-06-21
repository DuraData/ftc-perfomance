namespace FTCERP.Host.API.Requests;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

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
    string PermissionCode);

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
    string? WardIds,
    string? AdditionalAssigneeIds,
    string? VoteNumberIds,
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
    decimal AnnualTarget,
    string AnnualTargetDescription,
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
    string TargetUnitType,
    decimal? Q1Target,
    string? Q1Description,
    decimal? Q1Budget,
    decimal? Q2Target,
    string? Q2Description,
    decimal? Q2Budget,
    decimal? MidTermTarget,
    string? MidTermDescription,
    decimal? MidTermBudget,
    decimal? Q3Target,
    string? Q3Description,
    decimal? Q3Budget,
    decimal? Q3RevisedTarget,
    decimal? Q4Target,
    string? Q4Description,
    decimal? Q4Budget,
    decimal? Q4RevisedTarget,
    decimal? RevisedAnnualTarget,
    decimal? RevisedAnnualBudget);

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
    decimal AnnualTarget,
    string AnnualTargetDescription,
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
    string TargetUnitType,
    decimal? Q1Target,
    string? Q1Description,
    decimal? Q1Budget,
    decimal? Q2Target,
    string? Q2Description,
    decimal? Q2Budget,
    decimal? MidTermTarget,
    string? MidTermDescription,
    decimal? MidTermBudget,
    decimal? Q3Target,
    string? Q3Description,
    decimal? Q3Budget,
    decimal? Q3RevisedTarget,
    decimal? Q4Target,
    string? Q4Description,
    decimal? Q4Budget,
    decimal? Q4RevisedTarget,
    decimal? RevisedAnnualTarget,
    decimal? RevisedAnnualBudget);

public record SaveOpmsSubmissionRequest(
    string OpmsTargetId,
    string Quarter,
    decimal? Actual,
    string? ActualDescription,
    string? ActualPerformanceDescription,
    decimal? ActualExpenditure,
    decimal? Variance,
    string? VarianceReason,
    string? CorrectiveMeasure,
    decimal? SubmitterScore,
    string? PoeType,
    DateTime? DueDate,
    DateTime? ExtendedDueDate);

public record SaveIpmsSubmissionRequest(
    string IpmsTargetId,
    string Quarter,
    decimal? Actual,
    string? ActualDescription,
    string? ActualPerformanceDescription,
    decimal? ActualExpenditure,
    decimal? Variance,
    string? VarianceReason,
    string? CorrectiveMeasure,
    decimal? SubmitterScore,
    string? PoeType,
    DateTime? DueDate,
    DateTime? ExtendedDueDate);

public record SubmissionWorkflowActionRequest(
    string? Comment,
    decimal? Score = null,
    string? Recommendation = null,
    DateTime? ResponseDueDate = null,
    string? RfiComment = null);

public record DueDateExtensionRequest(DateTime ExtendedDueDate, string Reason, int? ExtendedByDays = null);

public record CreateIdpPlanRequest(
    string MunicipalityName,
    string PlanTitle,
    string PlanCode,
    int StartFinancialYear,
    int EndFinancialYear);

public record UpdateIdpPlanRequest(
    string PlanTitle,
    int StartFinancialYear,
    int EndFinancialYear,
    string Status);

public record CreateIdpPlanVersionRequest(
    string VersionType,
    string VersionLabel,
    string? ReviewYear,
    string? SummaryOfChanges);

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
    int SortOrder);

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

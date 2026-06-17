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

public record UserAssignmentItemRequest(string AssignmentType, string? TargetId, string? KpiId, string? ProjectId, string? TaskId);

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
    string IndicatorNumber,
    string TargetName,
    string KpiDescription,
    int? DepartmentId,
    int? UnitId,
    string? AssignedUserId,
    string? KpiId,
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    decimal Baseline,
    decimal AnnualTarget,
    decimal Weight,
    bool IsArchived);

public record SaveIpmsTargetRequest(
    string IndicatorNumber,
    string TargetName,
    string KpiDescription,
    int? DepartmentId,
    int? UnitId,
    string? AssignedUserId,
    string? RelatedOpmsTargetId,
    string? KpiId,
    string? SourceTemplateId,
    int? SourceTemplateVersion,
    decimal AnnualTarget,
    decimal Weight,
    bool IsArchived);

public record SaveOpmsSubmissionRequest(
    string OpmsTargetId,
    string Quarter,
    decimal? Actual,
    string? ActualDescription,
    string? VarianceReason,
    string? CorrectiveMeasure,
    DateTime? DueDate);

public record SaveIpmsSubmissionRequest(
    string IpmsTargetId,
    string Quarter,
    decimal? Actual,
    string? ActualDescription,
    string? VarianceReason,
    string? CorrectiveMeasure,
    DateTime? DueDate);

public record SubmissionWorkflowActionRequest(string? Comment, decimal? Score = null);

public record DueDateExtensionRequest(DateTime ExtendedDueDate, string Reason);

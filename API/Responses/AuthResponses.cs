namespace FTCERP.Host.API.Responses;

public record ApiResponse<T>(bool Success, T? Data, string? Message = null, string[]? Errors = null);

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserProfileResponse User, string[] Roles, string[] Permissions, MenuItemResponse[] Menu);

public record UserProfileResponse(string Id, string UserName, string FirstName, string LastName, string FullName, string Email, string? PhoneNumber, string? Department, string? Position, bool IsActive, bool MustChangePassword);

public record RoleResponse(string Id, string Name, string? Description, bool IsSystemRole, bool IsActive);

public record PermissionResponse(int Id, string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record MenuItemResponse(string Label, string? Path, string? Icon, MenuItemResponse[]? Children, bool IsDivider);

public record LoginAuditLogResponse(int Id, string? UserId, string Email, string? IpAddress, string? UserAgent, bool Success, string? FailureReason, DateTime LoggedAt);

public record UserResponse(string Id, string UserName, string FirstName, string LastName, string FullName, string Email, string? PhoneNumber, string? Department, string? Position, bool IsActive, bool MustChangePassword, DateTime? LastLoginAt);

public record UserDetailResponse(UserResponse User, RoleResponse[] Roles);

public record DemoUserResponse(string Role, string FullName, string Department, string Position, string Email, string UserName, string Password);

public record RolePermissionResponse(int PermissionId, string Code, bool IsAllowed);

public record UserPermissionOverrideResponse(int PermissionId, string Code, bool IsAllowed, string? Reason);

public record UserPermissionsResponse(string[] FromRoles, UserPermissionOverrideResponse[] Overrides, string[] Effective);

public record PermissionGroupResponse(string Module, string Feature, PermissionResponse[] Permissions);

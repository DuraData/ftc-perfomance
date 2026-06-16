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

public record CreatePermissionRequest(string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record UpdatePermissionRequest(string Module, string Feature, string Action, string Code, string? Description, bool IsActive);

public record CheckPermissionRequest(string PermissionCode);

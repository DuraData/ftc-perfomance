using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IAccessControlService _accessControlService;
    private readonly Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IPasswordResetNotifier _passwordResetNotifier;
    private readonly IAuthenticationPolicyResolver _authenticationPolicies;
    private readonly ITenantContext _tenantContext;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IAccessControlService accessControlService,
        Infrastructure.Persistence.ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IPasswordResetNotifier passwordResetNotifier,
        IAuthenticationPolicyResolver authenticationPolicies,
        ITenantContext tenantContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _accessControlService = accessControlService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _passwordResetNotifier = passwordResetNotifier;
        _authenticationPolicies = authenticationPolicies;
        _tenantContext = tenantContext;
    }

    [HttpPost("login")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        user ??= await _userManager.FindByNameAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            await RecordAuthenticationEventAsync(user?.Id, request.Email, false, user == null ? "Unknown account" : "Disabled account");
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid credentials"));
        }

        if (!await IsLocalSignInEnabledAsync(user))
        {
            await RecordAuthenticationEventAsync(user.Id, user.Email ?? request.Email, false, "Local sign-in disabled");
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid credentials"));
        }

        var authenticationPolicy = await _authenticationPolicies.ResolveAsync(user);
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        var dynamicallyLocked = await EnforceDynamicLockoutAsync(user, result, authenticationPolicy);
        if (result.RequiresTwoFactor)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode) && string.IsNullOrWhiteSpace(request.RecoveryCode))
                return StatusCode(StatusCodes.Status428PreconditionRequired, new ApiResponse<LoginResponse>(false, null, "MFA_REQUIRED"));

            var secondFactorValid = !string.IsNullOrWhiteSpace(request.RecoveryCode)
                ? (await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, NormalizeCode(request.RecoveryCode))).Succeeded
                : await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.TwoFactorCode));
            if (!secondFactorValid)
            {
                await RecordAuthenticationEventAsync(user.Id, user.Email ?? request.Email, false, "Invalid MFA code");
                return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid authentication code"));
            }
        }
        else if (!result.Succeeded)
        {
            await RecordAuthenticationEventAsync(user.Id, user.Email ?? request.Email, false, result.IsLockedOut || dynamicallyLocked ? "Account locked" : "Invalid credentials");
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid credentials"));
        }

        await RecordAuthenticationEventAsync(user.Id, user.Email ?? request.Email, true, null);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var (accessToken, refreshToken, expiresAt) = await _jwtService.GenerateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        SetSessionCookies(accessToken, refreshToken, expiresAt);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToList();
        var menu = await _accessControlService.GetAuthorizedNavigationAsync(user);

        var userProfile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        var enrollmentRequired = AuthenticationPolicyEnforcement.RequiresLocalMfaEnrollment(user, permissions, authenticationPolicy, _jwtSettings.MfaRequiredPermissionCodes, "LOCAL");
        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu, enrollmentRequired)));
    }

    [HttpPost("register")]
    [Authorize(Policy = "Permission:Admin.Users.Manage")]
    public async Task<ActionResult<ApiResponse<bool>>> Register([FromBody] RegisterRequest request)
    {
        if (!_tenantContext.MunicipalityId.HasValue || _tenantContext.MunicipalityId <= 0)
            return BadRequest(new ApiResponse<bool>(false, false, "Select a municipality context before creating a user."));
        var authenticationPolicy = await _authenticationPolicies.ResolveAsync(_tenantContext.MunicipalityId);
        var user = new ApplicationUser
        {
            MunicipalityId = _tenantContext.MunicipalityId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            MustChangePassword = authenticationPolicy.RequireFirstLoginPasswordChange,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to register user", result.Errors.Select(e => e.Description).ToArray()));

        await _userManager.AddToRoleAsync(user, SecurityModel.Submitter);
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(role => role.Name == SecurityModel.Submitter && role.IsActive
            && (role.MunicipalityId == null || role.MunicipalityId == _tenantContext.MunicipalityId));
        if (defaultRole != null)
        {
            _context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
            {
                UserId = user.Id,
                RoleId = defaultRole.Id,
                MunicipalityId = _tenantContext.MunicipalityId,
                EffectiveFrom = DateTime.UtcNow,
                AssignedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "BOOTSTRAP",
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        return Ok(new ApiResponse<bool>(true, true, "User registered successfully"));
    }

    [HttpPost("/api/v1/auth/password/forgot")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        const string message = "If an active account matches that email address, password reset instructions will be sent.";
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Ok(new ApiResponse<bool>(true, true, message));

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive || !user.EmailConfirmed)
            return Ok(new ApiResponse<bool>(true, true, message));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var delivery = await _passwordResetNotifier.SendAsync(user, token, HttpContext.TraceIdentifier, cancellationToken);
        AddAuthenticationAudit(user, "PasswordResetRequested", new { delivery.Delivered, delivery.Provider });
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new ApiResponse<bool>(true, true, message));
    }

    [HttpPost("/api/v1/auth/password/reset")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new ApiResponse<bool>(false, false, "The password reset link is invalid or has expired."));

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null || !user.IsActive || !user.EmailConfirmed)
            return BadRequest(new ApiResponse<bool>(false, false, "The password reset link is invalid or has expired."));

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var reset = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!reset.Succeeded)
        {
            await transaction.RollbackAsync();
            var invalidToken = reset.Errors.Any(error => string.Equals(error.Code, "InvalidToken", StringComparison.OrdinalIgnoreCase));
            return BadRequest(new ApiResponse<bool>(false, false,
                invalidToken ? "The password reset link is invalid or has expired." : "The new password does not satisfy the password policy.",
                invalidToken ? null : reset.Errors.Select(error => error.Description).ToArray()));
        }

        user.MustChangePassword = false;
        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiResponse<bool>(false, false, "The password could not be updated.", updated.Errors.Select(error => error.Description).ToArray()));
        }
        await _userManager.UpdateSecurityStampAsync(user);
        await _jwtService.RevokeAllSessionsAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "Password reset");
        AddAuthenticationAudit(user, "PasswordResetCompleted", null);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        ClearSessionCookies();
        return Ok(new ApiResponse<bool>(true, true, "Password reset completed. Sign in with the new password."));
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken()
    {
        var rawRefreshToken = Request.Cookies[AuthCookiePolicy.RefreshCookieName];
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        var existingRefreshToken = await _jwtService.GetRefreshTokenAsync(rawRefreshToken);
        if (existingRefreshToken == null)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        var user = await _userManager.FindByIdAsync(existingRefreshToken.UserId);
        if (user == null || !user.IsActive)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        var now = DateTime.UtcNow;
        var authenticationPolicy = await _authenticationPolicies.ResolveAsync(user);
        var idleCutoff = now.AddMinutes(-authenticationPolicy.SessionIdleTimeoutMinutes);
        if (existingRefreshToken.RevokedAt.HasValue || existingRefreshToken.ExpiresAt <= now || existingRefreshToken.AbsoluteExpiresAt <= now || existingRefreshToken.LastUsedAt <= idleCutoff || existingRefreshToken.SecurityStamp != (user.SecurityStamp ?? string.Empty))
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        await using var rotation = await _context.Database.BeginTransactionAsync();
        await _jwtService.RevokeRefreshTokenAsync(rawRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), "Rotated");
        var (accessToken, refreshToken, expiresAt) = await _jwtService.GenerateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), existingRefreshToken.SessionId, existingRefreshToken.AbsoluteExpiresAt, existingRefreshToken.AuthenticationMethod);
        await rotation.CommitAsync();
        SetSessionCookies(accessToken, refreshToken, expiresAt);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToList();
        var menu = await _accessControlService.GetAuthorizedNavigationAsync(user);

        var userProfile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        var enrollmentRequired = AuthenticationPolicyEnforcement.RequiresLocalMfaEnrollment(user, permissions, authenticationPolicy, _jwtSettings.MfaRequiredPermissionCodes, existingRefreshToken.AuthenticationMethod);
        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu, enrollmentRequired)));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionValue = User.FindFirstValue("sid");
        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(sessionValue, out var sessionId))
        {
            await _jwtService.RevokeSessionAsync(userId, sessionId, HttpContext.Connection.RemoteIpAddress?.ToString(), "User logout");
        }
        var rawRefreshToken = Request.Cookies[AuthCookiePolicy.RefreshCookieName];
        if (!string.IsNullOrWhiteSpace(rawRefreshToken) && !Guid.TryParse(sessionValue, out _))
        {
            await _jwtService.RevokeRefreshTokenAsync(rawRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), "User logout");
        }
        ClearSessionCookies();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [Authorize]
    [HttpGet("/api/v1/auth/sessions")]
    public async Task<ActionResult<ApiResponse<AuthSessionResponse[]>>> GetSessions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var current = User.FindFirstValue("sid");
        var rows = await _jwtService.GetActiveSessionsAsync(userId);
        var data = rows.Select(item => new AuthSessionResponse(item.SessionId, item.CreatedAt, item.LastUsedAt, item.AbsoluteExpiresAt, item.CreatedByIp, item.LastUsedByIp, item.UserAgent, item.AuthenticationMethod, item.SessionId.ToString() == current)).ToArray();
        return Ok(new ApiResponse<AuthSessionResponse[]>(true, data));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/sessions/{sessionId:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeSession(Guid sessionId, RevokeAuthSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var reason = request.Reason.Trim();
        if (reason.Length is < 5 or > 500) return BadRequest(new ApiResponse<bool>(false, false, "A reason between 5 and 500 characters is required."));
        var revoked = await _jwtService.RevokeSessionAsync(userId, sessionId, HttpContext.Connection.RemoteIpAddress?.ToString(), reason);
        if (!revoked) return NotFound(new ApiResponse<bool>(false, false, "Active session not found."));
        if (User.FindFirstValue("sid") == sessionId.ToString()) ClearSessionCookies();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/sessions/revoke-all")]
    public async Task<ActionResult<ApiResponse<int>>> RevokeAllSessions(RevokeAuthSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var reason = request.Reason.Trim();
        if (reason.Length is < 5 or > 500) return BadRequest(new ApiResponse<int>(false, 0, "A reason between 5 and 500 characters is required."));
        var count = await _jwtService.RevokeAllSessionsAsync(userId, HttpContext.Connection.RemoteIpAddress?.ToString(), reason);
        ClearSessionCookies();
        return Ok(new ApiResponse<int>(true, count));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/password/change")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(ChangePasswordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new ApiResponse<bool>(false, false, "Current and new passwords are required."));
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var changed = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changed.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiResponse<bool>(false, false, "Password could not be changed.", changed.Errors.Select(error => error.Description).ToArray()));
        }
        user.MustChangePassword = false;
        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiResponse<bool>(false, false, "Password state could not be updated.", updated.Errors.Select(error => error.Description).ToArray()));
        }
        await _userManager.UpdateSecurityStampAsync(user);
        AddAuthenticationAudit(user, "PasswordChange", null);
        await _jwtService.RevokeAllSessionsAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "Password changed");
        await transaction.CommitAsync();
        ClearSessionCookies();
        return Ok(new ApiResponse<bool>(true, true, "Password changed. Sign in again."));
    }

    [Authorize]
    [HttpGet("/api/v1/auth/mfa/status")]
    public async Task<ActionResult<ApiResponse<MfaStatusResponse>>> GetMfaStatus()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var recoveryCodesLeft = user.TwoFactorEnabled ? await _userManager.CountRecoveryCodesAsync(user) : 0;
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions;
        var enrollmentRequired = MfaRequirementPolicy.IsEnrollmentRequired(user.TwoFactorEnabled, permissions, _jwtSettings.MfaRequiredPermissionCodes);
        return Ok(new ApiResponse<MfaStatusResponse>(true, new MfaStatusResponse(user.TwoFactorEnabled, enrollmentRequired, recoveryCodesLeft)));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/mfa/setup")]
    public async Task<ActionResult<ApiResponse<MfaSetupResponse>>> SetupMfa()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (user.TwoFactorEnabled) return Conflict(new ApiResponse<MfaSetupResponse>(false, null, "MFA is already enabled."));
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            var reset = await _userManager.ResetAuthenticatorKeyAsync(user);
            if (!reset.Succeeded) return BadRequest(new ApiResponse<MfaSetupResponse>(false, null, "Authenticator setup could not be started.", reset.Errors.Select(error => error.Description).ToArray()));
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        if (string.IsNullOrWhiteSpace(key)) return StatusCode(500, new ApiResponse<MfaSetupResponse>(false, null, "Authenticator setup could not be started."));
        var issuer = Uri.EscapeDataString(_jwtSettings.Issuer);
        var account = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id);
        var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6";
        return Ok(new ApiResponse<MfaSetupResponse>(true, new MfaSetupResponse(FormatKey(key), uri)));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/mfa/enable")]
    public async Task<ActionResult<ApiResponse<MfaEnableResponse>>> EnableMfa(EnableMfaRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (user.TwoFactorEnabled) return Conflict(new ApiResponse<MfaEnableResponse>(false, null, "MFA is already enabled."));
        if (!await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.Code)))
        {
            await RecordAuthenticationEventAsync(user.Id, user.Email ?? user.Id, false, "MFA enrollment verification failed");
            return BadRequest(new ApiResponse<MfaEnableResponse>(false, null, "The authenticator code is invalid."));
        }

        var enabled = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enabled.Succeeded) return BadRequest(new ApiResponse<MfaEnableResponse>(false, null, "MFA could not be enabled.", enabled.Errors.Select(error => error.Description).ToArray()));
        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))?.ToArray() ?? [];
        await _userManager.UpdateSecurityStampAsync(user);
        AddAuthenticationAudit(user, "MfaEnable", new { RecoveryCodeCount = recoveryCodes.Length });
        await _jwtService.RevokeAllSessionsAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "MFA enabled");
        ClearSessionCookies();
        return Ok(new ApiResponse<MfaEnableResponse>(true, new MfaEnableResponse(recoveryCodes), "MFA enabled. Save the recovery codes and sign in again."));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/mfa/disable")]
    public async Task<ActionResult<ApiResponse<bool>>> DisableMfa(DisableMfaRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (!user.TwoFactorEnabled) return BadRequest(new ApiResponse<bool>(false, false, "MFA is not enabled."));
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new ApiResponse<bool>(false, false, "Password is required."));
        if (!await _userManager.CheckPasswordAsync(user, request.Password)) return Unauthorized(new ApiResponse<bool>(false, false, "Password verification failed."));
        var secondFactorValid = !string.IsNullOrWhiteSpace(request.RecoveryCode)
            ? (await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, NormalizeCode(request.RecoveryCode))).Succeeded
            : await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.Code));
        if (!secondFactorValid) return BadRequest(new ApiResponse<bool>(false, false, "The authentication code is invalid."));

        var disabled = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disabled.Succeeded) return BadRequest(new ApiResponse<bool>(false, false, "MFA could not be disabled.", disabled.Errors.Select(error => error.Description).ToArray()));
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        AddAuthenticationAudit(user, "MfaDisable", null);
        await _jwtService.RevokeAllSessionsAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "MFA disabled");
        ClearSessionCookies();
        return Ok(new ApiResponse<bool>(true, true, "MFA disabled. Sign in again."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return Unauthorized();

        var profile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        return Ok(new ApiResponse<UserProfileResponse>(true, profile));
    }

    private void SetSessionCookies(string accessToken, string refreshToken, DateTime accessExpiresAt)
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        Response.Cookies.Append(AuthCookiePolicy.AccessCookieName, accessToken, AuthCookiePolicy.Create(environment, AuthCookiePolicy.AccessPath, accessExpiresAt - DateTime.UtcNow));
        Response.Cookies.Append(AuthCookiePolicy.RefreshCookieName, refreshToken, AuthCookiePolicy.Create(
            environment,
            AuthCookiePolicy.RefreshPath,
            TimeSpan.FromDays(Math.Clamp(_jwtSettings.RefreshTokenExpiryDays, 1, 90))));
    }

    private void ClearSessionCookies()
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        Response.Cookies.Delete(AuthCookiePolicy.AccessCookieName, AuthCookiePolicy.Create(environment, AuthCookiePolicy.AccessPath));
        Response.Cookies.Delete(AuthCookiePolicy.RefreshCookieName, AuthCookiePolicy.Create(environment, AuthCookiePolicy.RefreshPath));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private void AddAuthenticationAudit(ApplicationUser user, string action, object? value)
    {
        _context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = user.MunicipalityId,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = action,
            NewValue = value == null ? null : JsonSerializer.Serialize(value),
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = HttpContext.TraceIdentifier,
            UserAgent = Request.Headers.UserAgent.ToString()
        });
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);

    private static string FormatKey(string key) => string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4).Select(index => key.Substring(index * 4, Math.Min(4, key.Length - index * 4)))).ToLowerInvariant();

    private async Task RecordAuthenticationEventAsync(string? userId, string email, bool success, string? failureReason)
    {
        _context.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserId = userId,
            Email = email,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Success = success,
            FailureReason = failureReason,
            LoggedAt = DateTime.UtcNow
        });
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var municipalityId = await _context.Users.IgnoreQueryFilters().Where(item => item.Id == userId).Select(item => item.MunicipalityId).SingleOrDefaultAsync();
            if (municipalityId.HasValue)
            {
                HttpContext.Items[TenantResolutionMiddleware.MunicipalityItem] = municipalityId.Value;
                _context.AuthenticationEvents.Add(new AuthenticationEvent
                {
                    MunicipalityId = municipalityId,
                    UserId = userId,
                    ProviderCode = "LOCAL",
                    EventType = "LocalSignIn",
                    Success = success,
                    FailureCode = failureReason,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    CorrelationId = HttpContext.TraceIdentifier,
                    OccurredAt = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task<bool> IsLocalSignInEnabledAsync(ApplicationUser user)
    {
        if (!user.MunicipalityId.HasValue) return true;
        var now = DateTime.UtcNow;
        var configuredMode = await _context.AuthenticationConfigurations.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.MunicipalityId == user.MunicipalityId && item.IsActive && item.EffectiveFrom <= now
                && (!item.EffectiveTo.HasValue || item.EffectiveTo > now))
            .Select(item => (AuthenticationMode?)item.Mode).SingleOrDefaultAsync();
        var mode = configuredMode ?? await _context.Municipalities.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Id == user.MunicipalityId).Select(item => item.AuthenticationMode).SingleAsync();
        return mode is AuthenticationMode.Local or AuthenticationMode.Hybrid;
    }

    private async Task<bool> EnforceDynamicLockoutAsync(ApplicationUser user, Microsoft.AspNetCore.Identity.SignInResult result, EffectiveAuthenticationPolicy policy)
    {
        if (result.Succeeded || result.RequiresTwoFactor || !user.LockoutEnabled || result.IsLockedOut) return result.IsLockedOut;
        if (user.AccessFailedCount < policy.MaximumFailedAttempts) return false;
        var locked = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(policy.LockoutMinutes));
        if (!locked.Succeeded) return false;
        await _userManager.ResetAccessFailedCountAsync(user);
        return true;
    }
}

public sealed record AuthSessionResponse(Guid SessionId, DateTime CreatedAt, DateTime LastUsedAt, DateTime AbsoluteExpiresAt, string? CreatedByIp, string? LastUsedByIp, string? UserAgent, string AuthenticationMethod, bool IsCurrent);
public sealed record RevokeAuthSessionRequest(string Reason);

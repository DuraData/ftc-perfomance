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
    private const string RefreshCookieName = "opms_refresh";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IAccessControlService _accessControlService;
    private readonly Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IAccessControlService accessControlService,
        Infrastructure.Persistence.ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _accessControlService = accessControlService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
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

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
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
            await RecordAuthenticationEventAsync(user.Id, user.Email ?? request.Email, false, result.IsLockedOut ? "Account locked" : "Invalid credentials");
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid credentials"));
        }

        await RecordAuthenticationEventAsync(user.Id, user.Email ?? request.Email, true, null);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var (accessToken, refreshToken, expiresAt) = await _jwtService.GenerateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        SetRefreshCookie(refreshToken);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToList();
        var menu = await _accessControlService.GetAuthorizedNavigationAsync(user);

        var userProfile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        var enrollmentRequired = MfaRequirementPolicy.IsEnrollmentRequired(user.TwoFactorEnabled, permissions, _jwtSettings.MfaRequiredPermissionCodes);
        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            accessToken, string.Empty, expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu, enrollmentRequired)));
    }

    [HttpPost("register")]
    [Authorize(Policy = "Permission:Admin.Users.Manage")]
    public async Task<ActionResult<ApiResponse<bool>>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            MustChangePassword = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to register user", result.Errors.Select(e => e.Description).ToArray()));

        await _userManager.AddToRoleAsync(user, SecurityModel.Submitter);
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(role => role.Name == SecurityModel.Submitter && role.IsActive);
        if (defaultRole != null)
        {
            _context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
            {
                UserId = user.Id,
                RoleId = defaultRole.Id,
                MunicipalityId = defaultRole.MunicipalityId,
                EffectiveFrom = DateTime.UtcNow,
                AssignedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "BOOTSTRAP",
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });
            await _context.SaveChangesAsync();
        }

        return Ok(new ApiResponse<bool>(true, true, "User registered successfully"));
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var principal = await _jwtService.GetPrincipalFromExpiredTokenAsync(request.AccessToken);
        if (principal == null)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid token"));

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid token"));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid token"));

        var rawRefreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        var existingRefreshToken = await _jwtService.GetRefreshTokenAsync(rawRefreshToken);
        var now = DateTime.UtcNow;
        var idleCutoff = now.AddMinutes(-Math.Clamp(_jwtSettings.SessionIdleTimeoutMinutes, 5, 24 * 60));
        if (existingRefreshToken == null || existingRefreshToken.UserId != userId || existingRefreshToken.RevokedAt.HasValue || existingRefreshToken.ExpiresAt <= now || existingRefreshToken.AbsoluteExpiresAt <= now || existingRefreshToken.LastUsedAt <= idleCutoff || existingRefreshToken.SecurityStamp != (user.SecurityStamp ?? string.Empty))
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        await using var rotation = await _context.Database.BeginTransactionAsync();
        await _jwtService.RevokeRefreshTokenAsync(rawRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), "Rotated");
        var (accessToken, refreshToken, expiresAt) = await _jwtService.GenerateTokensAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), existingRefreshToken.SessionId, existingRefreshToken.AbsoluteExpiresAt);
        await rotation.CommitAsync();
        SetRefreshCookie(refreshToken);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToList();
        var menu = await _accessControlService.GetAuthorizedNavigationAsync(user);

        var userProfile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        var enrollmentRequired = MfaRequirementPolicy.IsEnrollmentRequired(user.TwoFactorEnabled, permissions, _jwtSettings.MfaRequiredPermissionCodes);
        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            accessToken, string.Empty, expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu, enrollmentRequired)));
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
        var rawRefreshToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrWhiteSpace(rawRefreshToken) && !Guid.TryParse(sessionValue, out _))
        {
            await _jwtService.RevokeRefreshTokenAsync(rawRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), "User logout");
        }
        Response.Cookies.Delete(RefreshCookieName);
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
        var data = rows.Select(item => new AuthSessionResponse(item.SessionId, item.CreatedAt, item.LastUsedAt, item.AbsoluteExpiresAt, item.CreatedByIp, item.LastUsedByIp, item.UserAgent, item.SessionId.ToString() == current)).ToArray();
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
        if (User.FindFirstValue("sid") == sessionId.ToString()) Response.Cookies.Delete(RefreshCookieName);
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
        Response.Cookies.Delete(RefreshCookieName);
        return Ok(new ApiResponse<int>(true, count));
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
        AddMfaAudit(user, "Enable", new { RecoveryCodeCount = recoveryCodes.Length });
        await _jwtService.RevokeAllSessionsAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "MFA enabled");
        Response.Cookies.Delete(RefreshCookieName);
        return Ok(new ApiResponse<MfaEnableResponse>(true, new MfaEnableResponse(recoveryCodes), "MFA enabled. Save the recovery codes and sign in again."));
    }

    [Authorize]
    [HttpPost("/api/v1/auth/mfa/disable")]
    public async Task<ActionResult<ApiResponse<bool>>> DisableMfa(DisableMfaRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (!user.TwoFactorEnabled) return BadRequest(new ApiResponse<bool>(false, false, "MFA is not enabled."));
        if (!await _userManager.CheckPasswordAsync(user, request.Password)) return Unauthorized(new ApiResponse<bool>(false, false, "Password verification failed."));
        var secondFactorValid = !string.IsNullOrWhiteSpace(request.RecoveryCode)
            ? (await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, NormalizeCode(request.RecoveryCode))).Succeeded
            : await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.Code));
        if (!secondFactorValid) return BadRequest(new ApiResponse<bool>(false, false, "The authentication code is invalid."));

        var disabled = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disabled.Succeeded) return BadRequest(new ApiResponse<bool>(false, false, "MFA could not be disabled.", disabled.Errors.Select(error => error.Description).ToArray()));
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        AddMfaAudit(user, "Disable", null);
        await _jwtService.RevokeAllSessionsAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(), "MFA disabled");
        Response.Cookies.Delete(RefreshCookieName);
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

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(7),
            IsEssential = true
        });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private void AddMfaAudit(ApplicationUser user, string action, object? value)
    {
        _context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = user.MunicipalityId,
            EntityName = "UserMfa",
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
        await _context.SaveChangesAsync();
    }
}

public sealed record AuthSessionResponse(Guid SessionId, DateTime CreatedAt, DateTime LastUsedAt, DateTime AbsoluteExpiresAt, string? CreatedByIp, string? LastUsedByIp, string? UserAgent, bool IsCurrent);
public sealed record RevokeAuthSessionRequest(string Reason);

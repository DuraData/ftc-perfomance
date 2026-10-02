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
using System.Security.Claims;

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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IAccessControlService accessControlService,
        Infrastructure.Persistence.ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _accessControlService = accessControlService;
        _context = context;
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
        if (!result.Succeeded)
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

        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            accessToken, string.Empty, expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu)));
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
        var settings = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value;
        var idleCutoff = now.AddMinutes(-Math.Clamp(settings.SessionIdleTimeoutMinutes, 5, 24 * 60));
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

        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            accessToken, string.Empty, expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu)));
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

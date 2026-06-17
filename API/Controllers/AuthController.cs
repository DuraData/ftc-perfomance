using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        Infrastructure.Persistence.ApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        user ??= await _userManager.FindByNameAsync(request.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid credentials"));

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid credentials"));

        // Log the login attempt
        _context.LoginAuditLogs.Add(new LoginAuditLog
        {
            UserId = user.Id,
            Email = user.Email ?? request.Email,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Success = true,
            LoggedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var (accessToken, refreshToken, expiresAt) = await _jwtService.GenerateTokensAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user);
        var menu = GenerateMenu(permissions, roles);

        var userProfile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            accessToken, refreshToken, expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu)));
    }

    [HttpPost("register")]
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

        await _userManager.AddToRoleAsync(user, "EPMS User");

        return Ok(new ApiResponse<bool>(true, true, "User registered successfully"));
    }

    [HttpPost("refresh-token")]
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

        var existingRefreshToken = await _jwtService.GetRefreshTokenAsync(request.RefreshToken);
        if (existingRefreshToken == null || existingRefreshToken.UserId != userId || existingRefreshToken.RevokedAt.HasValue || existingRefreshToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Invalid refresh token"));

        var (accessToken, refreshToken, expiresAt) = await _jwtService.GenerateTokensAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user);
        var menu = GenerateMenu(permissions, roles);

        var userProfile = new UserProfileResponse(
            user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!,
            user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword);

        return Ok(new ApiResponse<LoginResponse>(true, new LoginResponse(
            accessToken, refreshToken, expiresAt, userProfile, roles.ToArray(), permissions.ToArray(), menu)));
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

    [HttpGet("demo-users")]
    public ActionResult<ApiResponse<DemoUserResponse[]>> DemoUsers()
    {
        var demoUsers = DbInitializer.GetDemoUserResponses(_configuration).ToArray();
        return Ok(new ApiResponse<DemoUserResponse[]>(true, demoUsers));
    }

    private async Task<List<string>> GetUserPermissionsAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (NavigationController.IsSystemAdministrator(roles))
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .Select(p => p.Code)
                .Distinct()
                .ToListAsync();
        }
        var roleIds = await _context.Roles.Where(r => roles.Contains(r.Name!)).Select(r => r.Id).ToListAsync();

        var rolePermissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsAllowed)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        var userOverrides = await _context.UserPermissionOverrides
            .Where(upo => upo.UserId == user.Id)
            .ToListAsync();

        var finalPermissions = new HashSet<string>(rolePermissions);
        foreach (var overrideItem in userOverrides)
        {
            if (overrideItem.IsAllowed)
                finalPermissions.Add(overrideItem.Permission.Code);
            else
                finalPermissions.Remove(overrideItem.Permission.Code);
        }

        return finalPermissions.ToList();
    }

    private MenuItemResponse[] GenerateMenu(List<string> permissions, IEnumerable<string> roles)
    {
        var set = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fullAccess = NavigationController.IsSystemAdministrator(roles);
        return NavigationController.BuildMenu(set, fullAccess);
    }
}

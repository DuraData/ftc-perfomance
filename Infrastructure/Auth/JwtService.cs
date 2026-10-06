using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FTCERP.Host.Infrastructure.Auth;

public interface IJwtService
{
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(ApplicationUser user, string? ipAddress = null, string? userAgent = null, Guid? sessionId = null, DateTime? absoluteExpiresAt = null, string authenticationMethod = "LOCAL");
    Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress, string reason = "Logout");
    Task<RefreshToken[]> GetActiveSessionsAsync(string userId);
    Task<ActiveSessionPage> GetActiveSessionsPageAsync(string userId, int page, int pageSize, string? search, string sortBy, bool descending);
    Task<bool> ValidateAccessSessionAsync(string userId, Guid sessionId, string securityStamp, string? ipAddress);
    Task<bool> RevokeSessionAsync(string userId, Guid sessionId, string? ipAddress, string reason);
    Task<int> RevokeAllSessionsAsync(string userId, string? ipAddress, string reason);
}

public sealed record ActiveSessionPage(RefreshToken[] Items, int TotalCount);

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly IAccessControlService _accessControlService;
    private readonly IAuthenticationPolicyResolver? _policyResolver;

    public JwtService(IOptions<JwtSettings> jwtSettings, UserManager<ApplicationUser> userManager, Infrastructure.Persistence.ApplicationDbContext context, IAccessControlService accessControlService, IAuthenticationPolicyResolver? policyResolver = null)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _context = context;
        _accessControlService = accessControlService;
        _policyResolver = policyResolver;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(ApplicationUser user, string? ipAddress = null, string? userAgent = null, Guid? sessionId = null, DateTime? absoluteExpiresAt = null, string authenticationMethod = "LOCAL")
    {
        var now = DateTime.UtcNow;
        var policy = await ResolvePolicyAsync(user);
        authenticationMethod = NormalizeAuthenticationMethod(authenticationMethod);
        var currentSessionId = sessionId ?? Guid.NewGuid();
        var absoluteExpiry = absoluteExpiresAt ?? now.AddHours(policy.SessionAbsoluteTimeoutHours);
        if (!sessionId.HasValue) await EnforceConcurrentSessionLimitAsync(user.Id, now, policy.MaximumConcurrentSessions);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("sid", currentSessionId.ToString()),
            new("security_stamp", user.SecurityStamp ?? string.Empty),
            new("amr", authenticationMethod)
        };

        if (user.MustChangePassword)
            claims.Add(new Claim(PasswordChangePolicy.ChangeRequiredClaim, bool.TrueString.ToLowerInvariant()));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(perm => new Claim("Permission", perm)));
        if (AuthenticationPolicyEnforcement.RequiresLocalMfaEnrollment(user, permissions, policy, _jwtSettings.MfaRequiredPermissionCodes, authenticationMethod))
            claims.Add(new Claim(MfaRequirementPolicy.EnrollmentRequiredClaim, bool.TrueString.ToLowerInvariant()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = now.AddMinutes(_jwtSettings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        // Persist only a one-way digest. The raw bearer value is returned once to the client.
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = HashRefreshToken(refreshToken),
            SessionId = currentSessionId,
            ExpiresAt = new[] { now.AddDays(_jwtSettings.RefreshTokenExpiryDays), absoluteExpiry }.Min(),
            AbsoluteExpiresAt = absoluteExpiry,
            CreatedAt = now,
            LastUsedAt = now,
            CreatedByIp = ipAddress,
            LastUsedByIp = ipAddress,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 1024)],
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            AuthenticationMethod = authenticationMethod
        });
        await _context.SaveChangesAsync();

        return (accessToken, refreshToken, expiresAt);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Allow expired tokens
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return Task.FromResult<ClaimsPrincipal?>(null);

            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var digest = HashRefreshToken(token);
        return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == digest);
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress, string reason = "Logout")
    {
        var digest = HashRefreshToken(token);
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == digest);
        if (refreshToken != null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.RevokedReason = reason;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<RefreshToken[]> GetActiveSessionsAsync(string userId)
    {
        var page = await GetActiveSessionsPageAsync(userId, 1, 100, null, "lastusedat", true);
        return page.Items;
    }

    public async Task<ActiveSessionPage> GetActiveSessionsPageAsync(string userId, int page, int pageSize, string? search, string sortBy, bool descending)
    {
        var now = DateTime.UtcNow;
        var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId);
        if (user == null) return new ActiveSessionPage([], 0);
        var policy = await ResolvePolicyAsync(user);
        var idleCutoff = now.AddMinutes(-policy.SessionIdleTimeoutMinutes);
        var query = _context.RefreshTokens.AsNoTracking()
            .Where(item => item.UserId == userId && !item.RevokedAt.HasValue && item.ExpiresAt > now && item.AbsoluteExpiresAt > now && item.LastUsedAt > idleCutoff)
            .Where(item => !_context.RefreshTokens.Any(other => other.UserId == userId
                && other.SessionId == item.SessionId
                && !other.RevokedAt.HasValue
                && other.ExpiresAt > now
                && other.AbsoluteExpiresAt > now
                && other.LastUsedAt > idleCutoff
                && (other.LastUsedAt > item.LastUsedAt || other.LastUsedAt == item.LastUsedAt && other.Id > item.Id)));
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(item => (item.UserAgent != null && item.UserAgent.Contains(term))
                || item.AuthenticationMethod.Contains(term)
                || (item.CreatedByIp != null && item.CreatedByIp.Contains(term))
                || (item.LastUsedByIp != null && item.LastUsedByIp.Contains(term)));
        var totalCount = await query.CountAsync();
        query = (sortBy, descending) switch
        {
            ("createdat", false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.SessionId),
            ("createdat", true) => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.SessionId),
            ("expiresat", false) => query.OrderBy(item => item.AbsoluteExpiresAt).ThenBy(item => item.SessionId),
            ("expiresat", true) => query.OrderByDescending(item => item.AbsoluteExpiresAt).ThenBy(item => item.SessionId),
            ("device", false) => query.OrderBy(item => item.UserAgent).ThenBy(item => item.SessionId),
            ("device", true) => query.OrderByDescending(item => item.UserAgent).ThenBy(item => item.SessionId),
            ("method", false) => query.OrderBy(item => item.AuthenticationMethod).ThenBy(item => item.SessionId),
            ("method", true) => query.OrderByDescending(item => item.AuthenticationMethod).ThenBy(item => item.SessionId),
            ("lastusedat", false) => query.OrderBy(item => item.LastUsedAt).ThenBy(item => item.SessionId),
            _ => query.OrderByDescending(item => item.LastUsedAt).ThenBy(item => item.SessionId)
        };
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
        return new ActiveSessionPage(items, totalCount);
    }

    public async Task<bool> ValidateAccessSessionAsync(string userId, Guid sessionId, string securityStamp, string? ipAddress)
    {
        var now = DateTime.UtcNow;
        var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId);
        if (user == null || !user.IsActive || user.SecurityStamp != securityStamp) return false;
        var policy = await ResolvePolicyAsync(user);
        var idleCutoff = now.AddMinutes(-policy.SessionIdleTimeoutMinutes);
        var session = await _context.RefreshTokens
            .Where(item => item.UserId == userId && item.SessionId == sessionId && !item.RevokedAt.HasValue)
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync();
        if (session == null || session.ExpiresAt <= now || session.AbsoluteExpiresAt <= now || session.LastUsedAt <= idleCutoff || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(session.SecurityStamp), Encoding.UTF8.GetBytes(securityStamp)))
        {
            if (session != null)
            {
                session.RevokedAt = now; session.RevokedByIp = ipAddress; session.RevokedReason = "Session validation failed";
                await _context.SaveChangesAsync();
            }
            return false;
        }
        if (session.LastUsedAt <= now.AddMinutes(-1))
        {
            session.LastUsedAt = now; session.LastUsedByIp = ipAddress;
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> RevokeSessionAsync(string userId, Guid sessionId, string? ipAddress, string reason)
    {
        var rows = await _context.RefreshTokens.Where(item => item.UserId == userId && item.SessionId == sessionId && !item.RevokedAt.HasValue).ToArrayAsync();
        if (rows.Length == 0) return false;
        var now = DateTime.UtcNow;
        foreach (var row in rows) { row.RevokedAt = now; row.RevokedByIp = ipAddress; row.RevokedReason = reason; }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> RevokeAllSessionsAsync(string userId, string? ipAddress, string reason)
    {
        var rows = await _context.RefreshTokens.Where(item => item.UserId == userId && !item.RevokedAt.HasValue).ToArrayAsync();
        var now = DateTime.UtcNow;
        foreach (var row in rows) { row.RevokedAt = now; row.RevokedByIp = ipAddress; row.RevokedReason = reason; }
        if (rows.Length > 0) await _context.SaveChangesAsync();
        return rows.Length;
    }

    private async Task EnforceConcurrentSessionLimitAsync(string userId, DateTime now, int maximum)
    {
        var active = await _context.RefreshTokens.Where(item => item.UserId == userId && !item.RevokedAt.HasValue && item.ExpiresAt > now && item.AbsoluteExpiresAt > now).OrderBy(item => item.CreatedAt).ToArrayAsync();
        var sessions = active.GroupBy(item => item.SessionId).OrderBy(group => group.Min(item => item.CreatedAt)).ToArray();
        foreach (var group in sessions.Take(Math.Max(0, sessions.Length - maximum + 1)))
            foreach (var row in group) { row.RevokedAt = now; row.RevokedReason = "Concurrent session limit"; }
    }

    private Task<EffectiveAuthenticationPolicy> ResolvePolicyAsync(ApplicationUser user) => _policyResolver?.ResolveAsync(user)
        ?? Task.FromResult(new EffectiveAuthenticationPolicy(12, 5, 15, true, false, true,
            Math.Clamp(_jwtSettings.SessionIdleTimeoutMinutes, 5, 1440), Math.Clamp(_jwtSettings.SessionAbsoluteTimeoutHours, 1, 720), Math.Clamp(_jwtSettings.MaxConcurrentSessions, 1, 50)));

    private static string NormalizeAuthenticationMethod(string value)
    {
        value = value.Trim().ToUpperInvariant();
        return value.Length is >= 2 and <= 40 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-') ? value : "EXTERNAL";
    }

    private static string HashRefreshToken(string token)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(digest);
    }
}

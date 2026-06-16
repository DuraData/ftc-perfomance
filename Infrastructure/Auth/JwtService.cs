using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FTCERP.Host.Infrastructure.Auth;

public interface IJwtService
{
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(ApplicationUser user);
    Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress);
}

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Infrastructure.Persistence.ApplicationDbContext _context;

    public JwtService(IOptions<JwtSettings> jwtSettings, UserManager<ApplicationUser> userManager, Infrastructure.Persistence.ApplicationDbContext context)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _context = context;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionCodesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(perm => new Claim("Permission", perm)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        // Save refresh token
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
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
        return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<string[]> GetUserPermissionCodesAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any(r => string.Equals(r, "Super Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .Select(p => p.Code)
                .Distinct()
                .ToArrayAsync();
        }
        var roleIds = await _context.Roles.Where(r => roles.Contains(r.Name!)).Select(r => r.Id).ToListAsync();

        // Get role permissions
        var rolePermissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsAllowed)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        // Get user overrides
        var userOverrides = await _context.UserPermissionOverrides
            .Where(upo => upo.UserId == user.Id)
            .ToListAsync();

        // Combine them
        var finalPermissions = new HashSet<string>(rolePermissions);
        foreach (var overrideItem in userOverrides)
        {
            if (overrideItem.IsAllowed)
                finalPermissions.Add(overrideItem.Permission.Code);
            else
                finalPermissions.Remove(overrideItem.Permission.Code);
        }

        return finalPermissions.ToArray();
    }
}

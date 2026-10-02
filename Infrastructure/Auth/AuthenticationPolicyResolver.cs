using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FTCERP.Host.Infrastructure.Auth;

public sealed record EffectiveAuthenticationPolicy(
    int MinimumPasswordLength,
    int MaximumFailedAttempts,
    int LockoutMinutes,
    bool RequireMfaForPrivilegedLocalUsers,
    bool RequireMfaForAllLocalUsers,
    bool RequireFirstLoginPasswordChange,
    int SessionIdleTimeoutMinutes,
    int SessionAbsoluteTimeoutHours,
    int MaximumConcurrentSessions);

public interface IAuthenticationPolicyResolver
{
    Task<EffectiveAuthenticationPolicy> ResolveAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<EffectiveAuthenticationPolicy> ResolveAsync(long? municipalityId, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationPolicyResolver(
    ApplicationDbContext context,
    IOptions<IdentityOptions> identityOptions,
    IOptions<JwtSettings> jwtSettings) : IAuthenticationPolicyResolver
{
    public Task<EffectiveAuthenticationPolicy> ResolveAsync(ApplicationUser user, CancellationToken cancellationToken = default) =>
        ResolveAsync(user.MunicipalityId, cancellationToken);

    public async Task<EffectiveAuthenticationPolicy> ResolveAsync(long? municipalityId, CancellationToken cancellationToken = default)
    {
        if (municipalityId.HasValue)
        {
            var now = DateTime.UtcNow;
            var stored = await context.AuthenticationPolicies.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId
                    && item.AuthenticationConfiguration.IsActive
                    && item.AuthenticationConfiguration.EffectiveFrom <= now
                    && (!item.AuthenticationConfiguration.EffectiveTo.HasValue || item.AuthenticationConfiguration.EffectiveTo > now))
                .Select(item => new EffectiveAuthenticationPolicy(
                    item.MinimumPasswordLength, item.MaximumFailedAttempts, item.LockoutMinutes,
                    item.RequireMfaForPrivilegedLocalUsers, item.RequireMfaForAllLocalUsers,
                    item.RequireFirstLoginPasswordChange, item.SessionIdleTimeoutMinutes,
                    item.SessionAbsoluteTimeoutHours, item.MaximumConcurrentSessions))
                .SingleOrDefaultAsync(cancellationToken);
            if (stored != null) return stored;
        }

        var identity = identityOptions.Value;
        var jwt = jwtSettings.Value;
        return new EffectiveAuthenticationPolicy(
            Math.Clamp(identity.Password.RequiredLength, 12, 128),
            5,
            15,
            true,
            false,
            true,
            Math.Clamp(jwt.SessionIdleTimeoutMinutes, 5, 1440),
            Math.Clamp(jwt.SessionAbsoluteTimeoutHours, 1, 720),
            Math.Clamp(jwt.MaxConcurrentSessions, 1, 50));
    }
}

public sealed class MunicipalityPasswordPolicyValidator(IAuthenticationPolicyResolver policies) : IPasswordValidator<ApplicationUser>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        var policy = await policies.ResolveAsync(user);
        if ((password?.Length ?? 0) >= policy.MinimumPasswordLength) return IdentityResult.Success;
        return IdentityResult.Failed(new IdentityError
        {
            Code = "MunicipalityPasswordTooShort",
            Description = $"Passwords for this municipality must be at least {policy.MinimumPasswordLength} characters."
        });
    }
}

public static class AuthenticationPolicyEnforcement
{
    public static bool RequiresLocalMfaEnrollment(ApplicationUser user, IEnumerable<string> effectivePermissions,
        EffectiveAuthenticationPolicy policy, IEnumerable<string> privilegedPermissions, string authenticationMethod)
    {
        if (!string.Equals(authenticationMethod, "LOCAL", StringComparison.OrdinalIgnoreCase) || user.TwoFactorEnabled) return false;
        if (policy.RequireMfaForAllLocalUsers) return true;
        return policy.RequireMfaForPrivilegedLocalUsers
            && MfaRequirementPolicy.IsEnrollmentRequired(false, effectivePermissions, privilegedPermissions);
    }
}

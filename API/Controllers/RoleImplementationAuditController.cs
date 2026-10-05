using FTCERP.Host.API.Responses;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/role-implementation-audit")]
[Authorize(Policy = "Permission:RoleImplementationAudit.View")]
public class RoleImplementationAuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<Domain.Entities.ApplicationRole> _roleManager;
    private readonly ITenantContext _tenantContext;

    public RoleImplementationAuditController(
        ApplicationDbContext context,
        RoleManager<Domain.Entities.ApplicationRole> roleManager,
        ITenantContext tenantContext)
    {
        _context = context;
        _roleManager = roleManager;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RoleImplementationAuditResponse[]>>> GetAudit()
    {
        var now = DateTime.UtcNow;
        var roleQuery = _roleManager.Roles.AsNoTracking().Where(role => role.IsActive);
        if (_tenantContext.MunicipalityId is long municipalityId)
            roleQuery = roleQuery.Where(role => role.MunicipalityId == municipalityId || !role.MunicipalityId.HasValue);
        var roles = await roleQuery.ToArrayAsync();
        var roleIds = roles.Select(role => role.Id).ToArray();
        var permissionRules = await _context.RolePermissions.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId) && item.IsActive && item.EffectiveFrom <= now
                && (!item.EffectiveTo.HasValue || item.EffectiveTo > now) && item.Permission.IsActive)
            .Select(item => new { item.RoleId, item.Permission.Code, item.IsAllowed })
            .ToArrayAsync();
        var rolePermissions = permissionRules.GroupBy(item => item.RoleId).ToDictionary(group => group.Key, group =>
        {
            var denied = group.Where(item => !item.IsAllowed).Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return group.Where(item => item.IsAllowed && !denied.Contains(item.Code)).Select(item => item.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        });
        var assignmentQuery = _context.SecurityUserRoleAssignments.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId) && item.IsActive && item.EffectiveFrom <= now
                && (!item.EffectiveTo.HasValue || item.EffectiveTo > now) && !item.RevokedAt.HasValue);
        var scopeQuery = _context.UserScopes.AsNoTracking()
            .Where(item => item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now));
        if (_tenantContext.MunicipalityId is long assignmentMunicipalityId)
        {
            assignmentQuery = assignmentQuery.Where(item => item.MunicipalityId == assignmentMunicipalityId);
            scopeQuery = scopeQuery.Where(item => !item.MunicipalityId.HasValue || item.MunicipalityId == assignmentMunicipalityId);
        }
        var assignments = await assignmentQuery.ToArrayAsync();
        var assignedUserIds = assignments.Select(item => item.UserId).Distinct().ToArray();
        var userScopes = await scopeQuery.Where(item => assignedUserIds.Contains(item.UserId)).ToArrayAsync();
        var navigation = await _context.SecurityNavigationItems.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToArrayAsync();
        var rolesByName = roles.Where(role => role.Name != null).ToDictionary(role => role.Name!, StringComparer.OrdinalIgnoreCase);

        var results = new List<RoleImplementationAuditResponse>();

        foreach (var roleName in SecurityModel.OrderedRoles)
        {
            if (!rolesByName.TryGetValue(roleName, out var role))
            {
                continue;
            }

            var permissions = rolePermissions.TryGetValue(role.Id, out var codes)
                ? codes
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var menus = AccessControlService.BuildAuthorizedNavigation(navigation, permissions);
            var roleAssignments = assignments.Where(link => link.RoleId == role.Id).ToArray();
            var roleUserIds = roleAssignments.Select(link => link.UserId).ToHashSet();
            var hasScopeRows = roleAssignments.Any(link => link.MunicipalityId.HasValue || link.DepartmentId.HasValue || link.UnitId.HasValue)
                || userScopes.Any(scope => roleUserIds.Contains(scope.UserId));

            var actual = new RoleImplementationAuditResponse(
                roleName,
                Dashboard: permissions.Contains("Dashboard.View"),
                Menus: menus.Length > 0,
                Crud: permissions.Any(code => code.EndsWith(".Manage", StringComparison.OrdinalIgnoreCase) || code.EndsWith(".Create", StringComparison.OrdinalIgnoreCase) || code.EndsWith(".Edit", StringComparison.OrdinalIgnoreCase) || code.EndsWith(".Delete", StringComparison.OrdinalIgnoreCase)),
                ScopeFiltering: hasScopeRows,
                Notifications: permissions.Any(code => code.StartsWith("Notifications.", StringComparison.OrdinalIgnoreCase)),
                Reports: permissions.Any(code => code.StartsWith("Reports.", StringComparison.OrdinalIgnoreCase) || code.StartsWith("Audit.Reports.", StringComparison.OrdinalIgnoreCase)),
                AuditTrail: permissions.Any(code => code.StartsWith("Audit.", StringComparison.OrdinalIgnoreCase) || code.Equals("VersionLogs.View", StringComparison.OrdinalIgnoreCase)),
                Complete: false);

            var expected = GetExpectedMatrix(roleName);
            results.Add(actual with
            {
                Complete =
                    actual.Dashboard == expected.Dashboard &&
                    actual.Menus == expected.Menus &&
                    actual.Crud == expected.Crud &&
                    actual.ScopeFiltering == expected.ScopeFiltering &&
                    actual.Notifications == expected.Notifications &&
                    actual.Reports == expected.Reports &&
                    actual.AuditTrail == expected.AuditTrail
            });
        }

        return Ok(new ApiResponse<RoleImplementationAuditResponse[]>(true, results.ToArray()));
    }

    private static RoleImplementationAuditResponse GetExpectedMatrix(string roleName)
    {
        return roleName switch
        {
            var value when string.Equals(value, SecurityModel.SuperAdmin, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, true),
            var value when string.Equals(value, SecurityModel.Admin, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, false, false),
            var value when string.Equals(value, SecurityModel.ClientAdmin, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, false, true, false, true, false, true, true, false),
            var value when string.Equals(value, SecurityModel.AuditorGeneral, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, false, true, false, true, true, false),
            var value when string.Equals(value, SecurityModel.PmsPerformanceManager, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            var value when string.Equals(value, SecurityModel.InternalAudit, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, false, true, true, false),
            var value when string.Equals(value, SecurityModel.Reviewer, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, false, true, false, true, true, false),
            var value when string.Equals(value, SecurityModel.Approver, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            var value when string.Equals(value, SecurityModel.Verifier, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            _ => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, false, false, false)
        };
    }
}

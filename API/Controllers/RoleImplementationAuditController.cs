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

    public RoleImplementationAuditController(ApplicationDbContext context, RoleManager<Domain.Entities.ApplicationRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RoleImplementationAuditResponse[]>>> GetAudit()
    {
        var rolePermissions = await _context.RolePermissions
            .AsNoTracking()
            .Include(item => item.Permission)
            .GroupBy(item => item.RoleId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.Permission.Code).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var userRoles = await _context.UserRoles.AsNoTracking().ToListAsync();
        var userScopes = await _context.UserScopes.AsNoTracking().ToListAsync();

        var results = new List<RoleImplementationAuditResponse>();

        foreach (var roleName in SecurityModel.OrderedRoles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                continue;
            }

            var permissions = rolePermissions.TryGetValue(role.Id, out var codes)
                ? codes
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var menus = NavigationController.BuildMenu(permissions, SecurityModel.IsSuperAdmin([roleName]));
            var roleUserIds = userRoles.Where(link => link.RoleId == role.Id).Select(link => link.UserId).ToHashSet();
            var hasScopeRows = userScopes.Any(scope => roleUserIds.Contains(scope.UserId));

            var actual = new RoleImplementationAuditResponse(
                roleName,
                Dashboard: permissions.Contains("Dashboard.View"),
                Menus: menus.Length > 0,
                Crud: permissions.Any(code => code.EndsWith(".Manage", StringComparison.OrdinalIgnoreCase) || code.EndsWith(".Create", StringComparison.OrdinalIgnoreCase) || code.EndsWith(".Edit", StringComparison.OrdinalIgnoreCase) || code.EndsWith(".Delete", StringComparison.OrdinalIgnoreCase)),
                ScopeFiltering: SecurityModel.IsSuperAdmin([roleName]) || hasScopeRows,
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
            var value when string.Equals(value, SecurityModel.MunicipalManager, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            var value when string.Equals(value, SecurityModel.InternalAudit, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, false, true, true, false),
            var value when string.Equals(value, SecurityModel.Reviewer, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, false, true, false, true, true, false),
            var value when string.Equals(value, SecurityModel.KpiApprover, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            var value when string.Equals(value, SecurityModel.HeadOfDepartment, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            var value when string.Equals(value, SecurityModel.DeputyHeadOfDepartment, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            var value when string.Equals(value, SecurityModel.Verifier, StringComparison.OrdinalIgnoreCase)
                => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, true, true, false),
            _ => new RoleImplementationAuditResponse(roleName, true, true, true, true, true, false, false, false)
        };
    }
}

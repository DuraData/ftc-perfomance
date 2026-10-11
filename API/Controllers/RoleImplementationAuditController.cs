using FTCERP.Host.API.Requests;
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
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<RoleImplementationAuditResponse[]>> GetAudit() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleImplementationAuditResponse[]>(false, null,
            "This unbounded fixed-role audit is retired. Use /api/role-implementation-audit/page."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<RoleImplementationAuditResponse>>>> GetAuditPage([FromQuery] PagedQueryRequest request)
    {
        if (request.NormalizedSortBy is not ("name" or "code" or "createdat"))
            return BadRequest(new ApiResponse<PagedResponse<RoleImplementationAuditResponse>>(false, null, "SortBy must be name, code, or createdAt."));

        var now = DateTime.UtcNow;
        var roleQuery = _roleManager.Roles.AsNoTracking().Where(role => role.IsActive && role.Name != null
            && role.EffectiveFrom <= now && (!role.EffectiveTo.HasValue || role.EffectiveTo > now));
        if (_tenantContext.MunicipalityId is long municipalityId)
            roleQuery = roleQuery.Where(role => role.MunicipalityId == municipalityId || !role.MunicipalityId.HasValue);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            roleQuery = roleQuery.Where(role => (role.Name != null && role.Name.ToLower().Contains(search))
                || role.RoleCode.ToLower().Contains(search)
                || (role.Description != null && role.Description.ToLower().Contains(search)));
        }
        var totalCount = await roleQuery.CountAsync();
        roleQuery = request.NormalizedSortBy switch
        {
            "code" => request.Descending ? roleQuery.OrderByDescending(role => role.RoleCode).ThenBy(role => role.Id) : roleQuery.OrderBy(role => role.RoleCode).ThenBy(role => role.Id),
            "createdat" => request.Descending ? roleQuery.OrderByDescending(role => role.CreatedAt).ThenBy(role => role.Id) : roleQuery.OrderBy(role => role.CreatedAt).ThenBy(role => role.Id),
            _ => request.Descending ? roleQuery.OrderByDescending(role => role.Name).ThenBy(role => role.Id) : roleQuery.OrderBy(role => role.Name).ThenBy(role => role.Id)
        };
        var roles = await roleQuery.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var roleIds = roles.Select(role => role.Id).ToArray();
        var permissionRules = await _context.RolePermissions.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId) && item.IsActive && item.EffectiveFrom <= now
                && (!item.EffectiveTo.HasValue || item.EffectiveTo > now) && item.Permission.IsActive)
            .Select(item => new { item.RoleId, item.Permission.Code, item.IsAllowed })
            .ToArrayAsync();
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
        var results = roles.Select(role =>
        {
            var rules = permissionRules.Where(item => item.RoleId == role.Id).ToArray();
            var denied = rules.Where(item => !item.IsAllowed).Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var permissions = rules.Where(item => item.IsAllowed && !denied.Contains(item.Code)).Select(item => item.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var menus = AccessControlService.BuildAuthorizedNavigation(navigation, permissions);
            var roleAssignments = assignments.Where(link => link.RoleId == role.Id).ToArray();
            var roleUserIds = roleAssignments.Select(link => link.UserId).ToHashSet();
            var hasScopeRows = roleAssignments.Any(link => link.MunicipalityId.HasValue || link.DepartmentId.HasValue || link.UnitId.HasValue)
                || userScopes.Any(scope => roleUserIds.Contains(scope.UserId));
            var allowedCount = permissions.Count;
            var deniedCount = denied.Count;

            return new RoleImplementationAuditResponse(
                role.PublicId,
                role.RoleCode,
                role.Name!,
                Dashboard: permissions.Any(code => code.Equals("Dashboard.View", StringComparison.OrdinalIgnoreCase)
                    || code.Equals("DASHBOARD.READ", StringComparison.OrdinalIgnoreCase)
                    || code.StartsWith("NAV.DASHBOARD", StringComparison.OrdinalIgnoreCase)),
                Menus: menus.Length > 0,
                Crud: permissions.Any(code => HasSuffix(code, "MANAGE", "CREATE", "EDIT", "UPDATE", "DELETE", "ARCHIVE")),
                ScopeFiltering: hasScopeRows,
                Notifications: permissions.Any(code => code.StartsWith("Notifications.", StringComparison.OrdinalIgnoreCase)),
                Reports: permissions.Any(code => code.StartsWith("Reports.", StringComparison.OrdinalIgnoreCase)
                    || code.StartsWith("Report.", StringComparison.OrdinalIgnoreCase)
                    || code.StartsWith("OfficialReport.", StringComparison.OrdinalIgnoreCase)
                    || code.StartsWith("Audit.Reports.", StringComparison.OrdinalIgnoreCase)),
                AuditTrail: permissions.Any(code => code.StartsWith("Audit.", StringComparison.OrdinalIgnoreCase) || code.Equals("VersionLogs.View", StringComparison.OrdinalIgnoreCase)),
                AllowedPermissionCount: allowedCount,
                DeniedPermissionCount: deniedCount,
                ActiveAssignmentCount: roleAssignments.Length,
                Complete: allowedCount + deniedCount > 0 && roleAssignments.Length > 0);
        }).ToArray();

        return Ok(new ApiResponse<PagedResponse<RoleImplementationAuditResponse>>(true,
            PagedResponse<RoleImplementationAuditResponse>.Create(results, request.Page, request.PageSize, totalCount)));
    }

    private static bool HasSuffix(string code, params string[] suffixes) =>
        suffixes.Any(suffix => code.EndsWith('.' + suffix, StringComparison.OrdinalIgnoreCase));
}

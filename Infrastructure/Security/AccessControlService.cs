using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Security;

public interface IAccessControlService
{
    Task<EffectiveAccessResult> GetEffectiveAccessAsync(ApplicationUser user);
    Task<AccessDecisionResult> CheckPermissionAsync(ApplicationUser user, string permissionCode, AccessScopeContext? scope = null);
    Task<RoleAccessMatrixResponse[]> BuildRoleAccessMatrixAsync();
    Task<SystemCoverageAuditResponse[]> BuildSystemCoverageAuditAsync();
}

public sealed record AccessScopeContext(
    int? DepartmentId = null,
    int? UnitId = null,
    string? TargetId = null,
    string? KpiId = null,
    string? ProjectId = null,
    string? TaskId = null);

public sealed record EffectiveAccessResult(
    string[] Roles,
    string[] EffectivePermissions,
    UserScope[] Scopes,
    UserAssignment[] Assignments);

public sealed record AccessDecisionResult(
    bool Allowed,
    string Reason,
    string[] EffectivePermissions,
    string[] MatchedScopes,
    string[] MatchedAssignments);

public class AccessControlService : IAccessControlService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AccessControlService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<EffectiveAccessResult> GetEffectiveAccessAsync(ApplicationUser user)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        if (SecurityModel.IsSuperAdmin(roles))
        {
            return new EffectiveAccessResult(
                roles,
                await _context.Permissions.Where(p => p.IsActive).Select(p => p.Code).Distinct().OrderBy(code => code).ToArrayAsync(),
                await _context.UserScopes.Where(scope => scope.UserId == user.Id).ToArrayAsync(),
                await _context.UserAssignments.Where(assignment => assignment.UserId == user.Id).ToArrayAsync());
        }

        var roleIds = await _context.Roles
            .Where(role => roles.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync();

        var effective = await _context.RolePermissions
            .Where(link => roleIds.Contains(link.RoleId) && link.IsAllowed)
            .Select(link => link.Permission.Code)
            .Distinct()
            .ToListAsync();

        var overrides = await _context.UserPermissionOverrides
            .Where(overrideItem => overrideItem.UserId == user.Id)
            .Select(overrideItem => new { overrideItem.Permission.Code, overrideItem.IsAllowed })
            .ToListAsync();

        var permissionSet = new HashSet<string>(effective, StringComparer.OrdinalIgnoreCase);
        foreach (var item in overrides)
        {
            if (item.IsAllowed)
            {
                permissionSet.Add(item.Code);
            }
            else
            {
                permissionSet.Remove(item.Code);
            }
        }

        var scopes = await _context.UserScopes
            .Where(scope => scope.UserId == user.Id)
            .OrderBy(scope => scope.ScopeType)
            .ToArrayAsync();

        var assignments = await _context.UserAssignments
            .Where(assignment => assignment.UserId == user.Id)
            .OrderBy(assignment => assignment.AssignmentType)
            .ToArrayAsync();

        return new EffectiveAccessResult(
            roles,
            permissionSet.OrderBy(code => code).ToArray(),
            scopes,
            assignments);
    }

    public async Task<AccessDecisionResult> CheckPermissionAsync(ApplicationUser user, string permissionCode, AccessScopeContext? scope = null)
    {
        var access = await GetEffectiveAccessAsync(user);
        if (SecurityModel.IsSuperAdmin(access.Roles))
        {
            return new AccessDecisionResult(true, "Super Admin has unrestricted access.", access.EffectivePermissions, ["InstitutionScope"], Array.Empty<string>());
        }

        if (!access.EffectivePermissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
        {
            return new AccessDecisionResult(false, $"Missing permission '{permissionCode}'.", access.EffectivePermissions, Array.Empty<string>(), Array.Empty<string>());
        }

        if (scope == null)
        {
            return new AccessDecisionResult(true, $"Permission '{permissionCode}' granted.", access.EffectivePermissions, Array.Empty<string>(), Array.Empty<string>());
        }

        var matchedScopes = access.Scopes
            .Where(current => ScopeMatches(current, scope))
            .Select(current => current.ScopeType.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var matchedAssignments = access.Assignments
            .Where(current => AssignmentMatches(current, scope))
            .Select(current => current.AssignmentType.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allowWithoutScope = access.Scopes.Length == 0 && access.Assignments.Length == 0;
        var allowed = allowWithoutScope || matchedScopes.Length > 0 || matchedAssignments.Length > 0;
        var reason = allowed
            ? $"Permission '{permissionCode}' granted within current scope."
            : $"Permission '{permissionCode}' exists but the requested record is outside the user's scope or assignment.";

        return new AccessDecisionResult(allowed, reason, access.EffectivePermissions, matchedScopes, matchedAssignments);
    }

    public async Task<RoleAccessMatrixResponse[]> BuildRoleAccessMatrixAsync()
    {
        var rolePermissions = await _context.RolePermissions
            .AsNoTracking()
            .Include(link => link.Permission)
            .GroupBy(link => link.RoleId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(link => link.Permission.Code).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToArray());

        var roles = await _roleManager.Roles
            .AsNoTracking()
            .Where(role => role.IsActive)
            .OrderBy(role => role.Name)
            .ToListAsync();

        var userRoles = await _context.UserRoles.AsNoTracking().ToListAsync();
        var users = await _context.Users.AsNoTracking().ToListAsync();
        var scopes = await _context.UserScopes.AsNoTracking().Include(scope => scope.Department).Include(scope => scope.Unit).ToListAsync();

        return roles.Select(role =>
        {
            var permissionCodes = SecurityModel.IsSuperAdmin([role.Name!])
                ? Array.Empty<string>()
                : rolePermissions.GetValueOrDefault(role.Id, Array.Empty<string>());
            var testUserId = userRoles.FirstOrDefault(link => link.RoleId == role.Id)?.UserId;
            var testUser = testUserId != null ? users.FirstOrDefault(user => user.Id == testUserId) : null;
            var testScopes = testUserId != null
                ? scopes.Where(scope => scope.UserId == testUserId).Select(FormatScope).ToArray()
                : Array.Empty<string>();

            return new RoleAccessMatrixResponse(
                role.Name!,
                SecurityModel.IsSuperAdmin([role.Name!]) ? ["*"] : permissionCodes,
                testScopes,
                NavigationController.BuildMenu(permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase), SecurityModel.IsSuperAdmin([role.Name!])).Select(item => item.Label).ToArray(),
                BuildAllowedActions(permissionCodes, SecurityModel.IsSuperAdmin([role.Name!])),
                BuildAllowedReports(permissionCodes, SecurityModel.IsSuperAdmin([role.Name!])),
                testUser != null ? $"{testUser.FirstName} {testUser.LastName}" : null);
        }).ToArray();
    }

    public async Task<SystemCoverageAuditResponse[]> BuildSystemCoverageAuditAsync()
    {
        var matrix = await BuildRoleAccessMatrixAsync();
        var rolesByName = await _roleManager.Roles.AsNoTracking().ToDictionaryAsync(role => role.Name!, StringComparer.OrdinalIgnoreCase);
        var userRoles = await _context.UserRoles.AsNoTracking().ToListAsync();
        var userScopes = await _context.UserScopes.AsNoTracking().ToListAsync();

        return SecurityModel.OrderedRoles.Select(roleName =>
        {
            rolesByName.TryGetValue(roleName, out var role);
            var links = role == null ? [] : userRoles.Where(link => link.RoleId == role.Id).ToArray();
            var row = matrix.FirstOrDefault(item => string.Equals(item.Role, roleName, StringComparison.OrdinalIgnoreCase));
            var hasPermissions = row != null && row.Permissions.Length > 0;
            var hasScopeFiltering = SecurityModel.IsSuperAdmin([roleName]) || links.Any(link => userScopes.Any(scope => scope.UserId == link.UserId));
            return new SystemCoverageAuditResponse(
                roleName,
                SeededUser: links.Length > 0,
                Dashboard: true,
                Menu: row != null && row.Menus.Length > 0,
                Permissions: hasPermissions,
                ScopeFiltering: hasScopeFiltering,
                Crud: row != null && row.AllowedActions.Any(action => action.Contains("Create", StringComparison.OrdinalIgnoreCase) || action.Contains("Edit", StringComparison.OrdinalIgnoreCase) || action.Contains("Delete", StringComparison.OrdinalIgnoreCase) || action.Contains("Manage", StringComparison.OrdinalIgnoreCase)),
                WorkflowActions: row != null && row.AllowedActions.Any(action => action.Contains("Submit", StringComparison.OrdinalIgnoreCase) || action.Contains("Verify", StringComparison.OrdinalIgnoreCase) || action.Contains("Approve", StringComparison.OrdinalIgnoreCase) || action.Contains("Review", StringComparison.OrdinalIgnoreCase) || action.Contains("Audit", StringComparison.OrdinalIgnoreCase)),
                Reports: row != null && row.Reports.Length > 0,
                AuditTrail: row != null && row.Permissions.Any(permission => permission.StartsWith("Audit.", StringComparison.OrdinalIgnoreCase) || permission.Contains("Trail", StringComparison.OrdinalIgnoreCase) || permission == "*"),
                Notifications: row != null && row.Permissions.Any(permission => permission.StartsWith("Notifications.", StringComparison.OrdinalIgnoreCase) || permission == "*"));
        }).ToArray();
    }

    private static bool ScopeMatches(UserScope current, AccessScopeContext requested)
    {
        return current.ScopeType switch
        {
            ScopeType.InstitutionScope => true,
            ScopeType.DepartmentScope => current.DepartmentId.HasValue && requested.DepartmentId == current.DepartmentId,
            ScopeType.UnitScope => current.UnitId.HasValue && requested.UnitId == current.UnitId,
            ScopeType.AssignedTargetScope => !string.IsNullOrWhiteSpace(current.TargetId) && string.Equals(current.TargetId, requested.TargetId, StringComparison.OrdinalIgnoreCase),
            ScopeType.AssignedKpiScope => !string.IsNullOrWhiteSpace(current.KpiId) && string.Equals(current.KpiId, requested.KpiId, StringComparison.OrdinalIgnoreCase),
            ScopeType.AssignedProjectScope => !string.IsNullOrWhiteSpace(current.ProjectId) && string.Equals(current.ProjectId, requested.ProjectId, StringComparison.OrdinalIgnoreCase),
            ScopeType.AssignedTaskScope => !string.IsNullOrWhiteSpace(current.TaskId) && string.Equals(current.TaskId, requested.TaskId, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool AssignmentMatches(UserAssignment current, AccessScopeContext requested)
    {
        return current.AssignmentType switch
        {
            AssignmentType.AdditionalApproverAssignment => IdMatches(current.TargetId, requested.TargetId) || IdMatches(current.KpiId, requested.KpiId),
            AssignmentType.AdditionalVerifierAssignment => IdMatches(current.TargetId, requested.TargetId) || IdMatches(current.KpiId, requested.KpiId),
            AssignmentType.AdditionalSubmitterAssignment => IdMatches(current.TargetId, requested.TargetId) || IdMatches(current.KpiId, requested.KpiId),
            (AssignmentType)4 => IdMatches(current.ProjectId, requested.ProjectId),
            (AssignmentType)5 => IdMatches(current.TaskId, requested.TaskId),
            _ => false
        };
    }

    private static bool IdMatches(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string FormatScope(UserScope scope)
    {
        return scope.ScopeType switch
        {
            ScopeType.InstitutionScope => "Institution",
            ScopeType.DepartmentScope => $"Department:{scope.Department?.Name ?? scope.DepartmentId?.ToString() ?? "-"}",
            ScopeType.UnitScope => $"Unit:{scope.Unit?.Name ?? scope.UnitId?.ToString() ?? "-"}",
            ScopeType.AssignedTargetScope => $"Target:{scope.TargetId}",
            ScopeType.AssignedKpiScope => $"KPI:{scope.KpiId}",
            ScopeType.AssignedProjectScope => $"Project:{scope.ProjectId}",
            ScopeType.AssignedTaskScope => $"Task:{scope.TaskId}",
            _ => scope.ScopeType.ToString()
        };
    }

    private static string[] BuildAllowedActions(IEnumerable<string> permissions, bool fullAccess)
    {
        if (fullAccess)
        {
            return ["View", "Create", "Edit", "Delete", "Archive", "Submit", "Verify", "VerifyReject", "Approve", "Reject", "Review", "Audit", "Score", "UploadPOE", "ValidatePOE", "ExtendDueDate", "TriggerNotification", "GenerateReport", "Export", "Manage"];
        }

        return permissions
            .Select(code => code.Split('.').Last())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToArray();
    }

    private static string[] BuildAllowedReports(IEnumerable<string> permissions, bool fullAccess)
    {
        if (fullAccess)
        {
            return ["All Reports"];
        }

        return permissions
            .Where(code => code.StartsWith("Reports.", StringComparison.OrdinalIgnoreCase) || code.StartsWith("Audit.Reports.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(code => code)
            .ToArray();
    }
}

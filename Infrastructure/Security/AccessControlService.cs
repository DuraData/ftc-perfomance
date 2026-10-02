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
    Task<AccessQueryScopeResult> GetQueryScopeAsync(ApplicationUser user, string permissionCode);
    Task<MenuItemResponse[]> GetAuthorizedNavigationAsync(ApplicationUser user);
    Task<RoleAccessMatrixResponse[]> BuildRoleAccessMatrixAsync();
    Task<SystemCoverageAuditResponse[]> BuildSystemCoverageAuditAsync();
}

public sealed record AccessScopeContext(
    int? DepartmentId = null,
    int? UnitId = null,
    string? OwnerUserId = null,
    string? DelegatorUserId = null,
    string? TargetId = null,
    string? KpiId = null,
    string? ProjectId = null,
    string? TaskId = null,
    long? MunicipalityId = null);

public sealed record EffectivePermissionRule(string Code, ScopeType? ScopeType);

public sealed record EffectiveAccessResult(
    string[] Roles,
    string[] EffectivePermissions,
    UserScope[] Scopes,
    UserAssignment[] Assignments,
    EffectivePermissionRule[] PermissionRules,
    SecurityUserRoleAssignment[] RoleAssignments);

public sealed record AccessDecisionResult(
    bool Allowed,
    string Reason,
    string[] EffectivePermissions,
    string[] MatchedScopes,
    string[] MatchedAssignments);

public sealed record AccessQueryScopeResult(bool PermissionGranted, bool Unrestricted, int[] DepartmentIds, int[] UnitIds, string[] OwnerUserIds, string[] TargetIds, string[] KpiIds, long[] MunicipalityIds);

public class AccessControlService : IAccessControlService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantContext? _tenantContext;

    public AccessControlService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITenantContext? tenantContext = null)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantContext = tenantContext;
    }

    public async Task<EffectiveAccessResult> GetEffectiveAccessAsync(ApplicationUser user)
    {
        var now = DateTime.UtcNow;
        var municipalityId = _tenantContext?.MunicipalityId;
        var assignmentQuery = _context.SecurityUserRoleAssignments
            .Where(link => link.UserId == user.Id && link.IsActive && link.EffectiveFrom <= now && (!link.EffectiveTo.HasValue || link.EffectiveTo > now) && !link.RevokedAt.HasValue)
            .AsNoTracking();
        if (municipalityId.HasValue) assignmentQuery = assignmentQuery.Where(link => link.MunicipalityId == municipalityId.Value);
        var roleAssignments = await assignmentQuery.ToArrayAsync();
        var roleIds = roleAssignments.Select(link => link.RoleId).Distinct().ToArray();
        var roles = await _context.Roles.Where(role => roleIds.Contains(role.Id) && role.IsActive && role.EffectiveFrom <= now && (!role.EffectiveTo.HasValue || role.EffectiveTo > now)).Select(role => role.Name!).ToArrayAsync();

        var roleRules = await _context.RolePermissions
            .Where(link => roleIds.Contains(link.RoleId) && link.IsActive && link.EffectiveFrom <= now && (!link.EffectiveTo.HasValue || link.EffectiveTo > now) && link.Permission.IsActive)
            .Select(link => new { link.Permission.Code, link.IsAllowed, link.ScopeType })
            .ToListAsync();

        var denied = roleRules.Where(item => !item.IsAllowed).Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effective = roleRules.Where(item => item.IsAllowed && !denied.Contains(item.Code)).Select(item => item.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var effectiveRules = roleRules.Where(item => item.IsAllowed && !denied.Contains(item.Code))
            .Select(item => new EffectivePermissionRule(item.Code, item.ScopeType)).ToList();

        var overrides = await _context.UserPermissionOverrides
            .Where(overrideItem => overrideItem.UserId == user.Id)
            .Select(overrideItem => new { overrideItem.Permission.Code, overrideItem.IsAllowed })
            .ToListAsync();

        var permissionSet = new HashSet<string>(effective, StringComparer.OrdinalIgnoreCase);
        foreach (var item in overrides)
        {
            // Legacy user overrides are restriction-only. Grants belong to audited,
            // tenant-scoped role assignments and may not bypass role DENY rules.
            if (!item.IsAllowed)
            {
                permissionSet.Remove(item.Code);
                effectiveRules.RemoveAll(rule => string.Equals(rule.Code, item.Code, StringComparison.OrdinalIgnoreCase));
            }
        }

        var scopes = await _context.UserScopes
            .Where(scope => scope.UserId == user.Id && scope.IsActive && scope.EffectiveFrom <= now && (!scope.EffectiveTo.HasValue || scope.EffectiveTo > now))
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
            assignments,
            effectiveRules.ToArray(),
            roleAssignments);
    }

    public async Task<AccessDecisionResult> CheckPermissionAsync(ApplicationUser user, string permissionCode, AccessScopeContext? scope = null)
    {
        var access = await GetEffectiveAccessAsync(user);
        if (!access.EffectivePermissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
        {
            return new AccessDecisionResult(false, $"Missing permission '{permissionCode}'.", access.EffectivePermissions, Array.Empty<string>(), Array.Empty<string>());
        }

        if (scope == null)
        {
            return new AccessDecisionResult(true, $"Permission '{permissionCode}' granted.", access.EffectivePermissions, Array.Empty<string>(), Array.Empty<string>());
        }

        var permissionRules = access.PermissionRules.Where(rule => string.Equals(rule.Code, permissionCode, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (permissionRules.Any(rule => rule.ScopeType == null))
        {
            return new AccessDecisionResult(true, $"Permission '{permissionCode}' granted without a record restriction.", access.EffectivePermissions, ["Unrestricted"], Array.Empty<string>());
        }
        var permittedScopeTypes = permissionRules.Where(rule => rule.ScopeType.HasValue).Select(rule => rule.ScopeType!.Value).ToHashSet();

        var matchedScopes = access.Scopes
            .Where(current => permittedScopeTypes.Contains(current.ScopeType) && ScopeMatches(current, scope))
            .Select(current => current.ScopeType.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roleAssignmentScopeMatches = access.RoleAssignments.Any(assignment =>
            (permittedScopeTypes.Contains(ScopeType.DepartmentScope) && assignment.DepartmentId.HasValue && assignment.DepartmentId == scope.DepartmentId)
            || (permittedScopeTypes.Contains(ScopeType.UnitScope) && assignment.UnitId.HasValue && assignment.UnitId == scope.UnitId)
            || (permittedScopeTypes.Contains(ScopeType.InstitutionScope) && assignment.MunicipalityId.HasValue && assignment.MunicipalityId == scope.MunicipalityId));
        if (permittedScopeTypes.Contains(ScopeType.Self) && string.Equals(scope.OwnerUserId, user.Id, StringComparison.OrdinalIgnoreCase))
            matchedScopes = matchedScopes.Append(nameof(ScopeType.Self)).ToArray();
        if (permittedScopeTypes.Contains(ScopeType.System))
            matchedScopes = matchedScopes.Append(nameof(ScopeType.System)).ToArray();
        if (roleAssignmentScopeMatches)
            matchedScopes = matchedScopes.Append("RoleAssignmentScope").Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var matchedAssignments = access.Assignments
            .Where(current => AssignmentMatches(current, scope, user.Id))
            .Select(current => current.AssignmentType.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var managerHierarchyMatch = await ManagerHierarchyMatchesAsync(user.Id, scope.OwnerUserId);
        if (managerHierarchyMatch)
        {
            matchedAssignments = matchedAssignments
                .Concat(["ManagerHierarchy"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var assignmentScopePermitted = permittedScopeTypes.Any(type => type is ScopeType.AssignedTargetScope or ScopeType.AssignedKpiScope or ScopeType.AssignedProjectScope or ScopeType.AssignedTaskScope);
        if (!assignmentScopePermitted) matchedAssignments = [];
        var allowed = matchedScopes.Length > 0 || matchedAssignments.Length > 0 || (managerHierarchyMatch && permittedScopeTypes.Contains(ScopeType.Self));
        var reason = allowed
            ? $"Permission '{permissionCode}' granted within current scope."
            : $"Permission '{permissionCode}' exists but the requested record is outside the user's scope or assignment.";

        return new AccessDecisionResult(allowed, reason, access.EffectivePermissions, matchedScopes, matchedAssignments);
    }

    public async Task<AccessQueryScopeResult> GetQueryScopeAsync(ApplicationUser user, string permissionCode)
    {
        var access = await GetEffectiveAccessAsync(user);
        if (!access.EffectivePermissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
            return new(false, false, [], [], [], [], [], []);
        var rules = access.PermissionRules.Where(rule => string.Equals(rule.Code, permissionCode, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (rules.Any(rule => rule.ScopeType == null || rule.ScopeType == ScopeType.System))
            return new(true, true, [], [], [], [], [], []);

        var allowedTypes = rules.Where(rule => rule.ScopeType.HasValue).Select(rule => rule.ScopeType!.Value).ToHashSet();
        var departments = access.Scopes.Where(scope => allowedTypes.Contains(ScopeType.DepartmentScope) && scope.ScopeType == ScopeType.DepartmentScope && scope.DepartmentId.HasValue).Select(scope => scope.DepartmentId!.Value)
            .Concat(access.RoleAssignments.Where(item => allowedTypes.Contains(ScopeType.DepartmentScope) && item.DepartmentId.HasValue).Select(item => item.DepartmentId!.Value)).Distinct().ToArray();
        var units = access.Scopes.Where(scope => allowedTypes.Contains(ScopeType.UnitScope) && scope.ScopeType == ScopeType.UnitScope && scope.UnitId.HasValue).Select(scope => scope.UnitId!.Value)
            .Concat(access.RoleAssignments.Where(item => allowedTypes.Contains(ScopeType.UnitScope) && item.UnitId.HasValue).Select(item => item.UnitId!.Value)).Distinct().ToArray();
        var municipalities = access.Scopes.Where(scope => allowedTypes.Contains(ScopeType.InstitutionScope) && scope.ScopeType == ScopeType.InstitutionScope && scope.MunicipalityId.HasValue).Select(scope => scope.MunicipalityId!.Value)
            .Concat(access.RoleAssignments.Where(item => allowedTypes.Contains(ScopeType.InstitutionScope) && item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value)).Distinct().ToArray();
        var owners = allowedTypes.Contains(ScopeType.Self) ? new[] { user.Id } : [];
        var targetIds = access.Scopes.Where(scope => allowedTypes.Contains(ScopeType.AssignedTargetScope) && scope.ScopeType == ScopeType.AssignedTargetScope && scope.TargetId != null).Select(scope => scope.TargetId!)
            .Concat(access.Assignments.Where(item => allowedTypes.Contains(ScopeType.AssignedTargetScope) && item.IsActive && item.TargetId != null).Select(item => item.TargetId!)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var kpiIds = access.Scopes.Where(scope => allowedTypes.Contains(ScopeType.AssignedKpiScope) && scope.ScopeType == ScopeType.AssignedKpiScope && scope.KpiId != null).Select(scope => scope.KpiId!)
            .Concat(access.Assignments.Where(item => allowedTypes.Contains(ScopeType.AssignedKpiScope) && item.IsActive && item.KpiId != null).Select(item => item.KpiId!)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return new(true, false, departments, units, owners, targetIds, kpiIds, municipalities);
    }

    public async Task<MenuItemResponse[]> GetAuthorizedNavigationAsync(ApplicationUser user)
    {
        var access = await GetEffectiveAccessAsync(user);
        var permissionSet = access.EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var definitions = await _context.SecurityNavigationItems.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync();
        return BuildAuthorizedNavigation(definitions, permissionSet);
    }

    internal static MenuItemResponse[] BuildAuthorizedNavigation(IReadOnlyCollection<SecurityNavigationItem> definitions, ISet<string> permissionSet)
    {
        var byParent = definitions.Where(item => item.ParentId.HasValue).GroupBy(item => item.ParentId!.Value).ToDictionary(group => group.Key, group => group.ToArray());
        var roots = definitions.Where(item => !item.ParentId.HasValue).ToArray();

        MenuItemResponse? Build(SecurityNavigationItem item, HashSet<int> path)
        {
            if (!path.Add(item.Id)) return null;
            var children = byParent.GetValueOrDefault(item.Id, [])
                .Select(child => Build(child, new HashSet<int>(path)))
                .Where(child => child != null)
                .Cast<MenuItemResponse>()
                .ToArray();
            var directlyAllowed = !string.IsNullOrWhiteSpace(item.RequiredPermissionCode) && permissionSet.Contains(item.RequiredPermissionCode);
            if (!directlyAllowed && children.Length == 0) return null;
            return new MenuItemResponse(item.Name, directlyAllowed ? item.Route : null, item.IconKey, children.Length == 0 ? null : children, false, item.Code);
        }

        return roots
            .Select(item => Build(item, []))
            .Where(item => item != null)
            .Cast<MenuItemResponse>()
            .ToArray();
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
        var navigation = await _context.SecurityNavigationItems.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToArrayAsync();

        return roles.Select(role =>
        {
            var permissionCodes = rolePermissions.GetValueOrDefault(role.Id, Array.Empty<string>());
            var testUserId = userRoles.FirstOrDefault(link => link.RoleId == role.Id)?.UserId;
            var testUser = testUserId != null ? users.FirstOrDefault(user => user.Id == testUserId) : null;
            var testScopes = testUserId != null
                ? scopes.Where(scope => scope.UserId == testUserId).Select(FormatScope).ToArray()
                : Array.Empty<string>();

            return new RoleAccessMatrixResponse(
                role.Name!,
                permissionCodes,
                testScopes,
                BuildAuthorizedNavigation(navigation, permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)).Select(item => item.Label).ToArray(),
                BuildAllowedActions(permissionCodes, false),
                BuildAllowedReports(permissionCodes, false),
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
            var hasScopeFiltering = links.Any(link => userScopes.Any(scope => scope.UserId == link.UserId));
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
            ScopeType.Self => !string.IsNullOrWhiteSpace(requested.OwnerUserId) && string.Equals(requested.OwnerUserId, current.UserId, StringComparison.OrdinalIgnoreCase),
            ScopeType.InstitutionScope => current.MunicipalityId.HasValue && requested.MunicipalityId == current.MunicipalityId,
            ScopeType.System => true,
            ScopeType.DepartmentScope => current.DepartmentId.HasValue && requested.DepartmentId == current.DepartmentId,
            ScopeType.UnitScope => current.UnitId.HasValue && requested.UnitId == current.UnitId,
            ScopeType.AssignedTargetScope => !string.IsNullOrWhiteSpace(current.TargetId) && string.Equals(current.TargetId, requested.TargetId, StringComparison.OrdinalIgnoreCase),
            ScopeType.AssignedKpiScope => !string.IsNullOrWhiteSpace(current.KpiId) && string.Equals(current.KpiId, requested.KpiId, StringComparison.OrdinalIgnoreCase),
            ScopeType.AssignedProjectScope => !string.IsNullOrWhiteSpace(current.ProjectId) && string.Equals(current.ProjectId, requested.ProjectId, StringComparison.OrdinalIgnoreCase),
            ScopeType.AssignedTaskScope => !string.IsNullOrWhiteSpace(current.TaskId) && string.Equals(current.TaskId, requested.TaskId, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool AssignmentMatches(UserAssignment current, AccessScopeContext requested, string currentUserId)
    {
        if (!current.IsActive)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        if (current.ValidFromUtc.HasValue && current.ValidFromUtc.Value > now)
        {
            return false;
        }

        if (current.ValidToUtc.HasValue && current.ValidToUtc.Value < now)
        {
            return false;
        }

        return current.AssignmentType switch
        {
            AssignmentType.AdditionalApproverAssignment => IdMatches(current.TargetId, requested.TargetId) || IdMatches(current.KpiId, requested.KpiId),
            AssignmentType.AdditionalVerifierAssignment => IdMatches(current.TargetId, requested.TargetId) || IdMatches(current.KpiId, requested.KpiId),
            AssignmentType.AdditionalSubmitterAssignment => IdMatches(current.TargetId, requested.TargetId) || IdMatches(current.KpiId, requested.KpiId),
            AssignmentType.ProjectAssignee => IdMatches(current.ProjectId, requested.ProjectId),
            AssignmentType.TaskAssignee => IdMatches(current.TaskId, requested.TaskId),
            AssignmentType.DelegatedAssignment => DelegationMatches(current, requested, currentUserId),
            _ => false
        };
    }

    private static bool DelegationMatches(UserAssignment current, AccessScopeContext requested, string currentUserId)
    {
        if (!string.Equals(current.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(current.DelegatorUserId)
            && !string.IsNullOrWhiteSpace(requested.DelegatorUserId)
            && !string.Equals(current.DelegatorUserId, requested.DelegatorUserId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IdMatches(current.TargetId, requested.TargetId)
            || IdMatches(current.KpiId, requested.KpiId)
            || IdMatches(current.ProjectId, requested.ProjectId)
            || IdMatches(current.TaskId, requested.TaskId)
            || (!string.IsNullOrWhiteSpace(requested.OwnerUserId) && string.Equals(requested.OwnerUserId, current.DelegatorUserId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> ManagerHierarchyMatchesAsync(string currentUserId, string? ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return false;
        }

        if (string.Equals(currentUserId, ownerUserId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Traverse the manager chain from the target owner toward the root.
        var traversed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cursor = ownerUserId;
        while (!string.IsNullOrWhiteSpace(cursor) && traversed.Add(cursor))
        {
            var managerId = await _context.Users
                .AsNoTracking()
                .Where(item => item.Id == cursor)
                .Select(item => item.ManagerUserId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(managerId))
            {
                return false;
            }

            if (string.Equals(managerId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            cursor = managerId;
        }

        return false;
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

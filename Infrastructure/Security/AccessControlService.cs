using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
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
    Task<PagedResponse<RoleAccessMatrixResponse>> BuildRoleAccessMatrixPageAsync(PagedQueryRequest request);
    Task<PagedResponse<SystemCoverageAuditResponse>> BuildSystemCoverageAuditPageAsync(PagedQueryRequest request);
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

public static class AccessControlBatchExtensions
{
    public static async Task<IReadOnlyDictionary<string, AccessDecisionResult>> CheckPermissionsAsync(
        this IAccessControlService service,
        ApplicationUser user,
        IEnumerable<string> permissionCodes,
        AccessScopeContext? scope = null)
    {
        var codes = permissionCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (service is AccessControlService concrete)
            return await concrete.CheckPermissionsBatchAsync(user, codes, scope);

        var decisions = new Dictionary<string, AccessDecisionResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
            decisions[code] = await service.CheckPermissionAsync(user, code, scope)
                ?? new AccessDecisionResult(false, $"Missing decision for permission '{code}'.", [], [], []);
        return decisions;
    }
}

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
        if (municipalityId.HasValue)
            assignmentQuery = assignmentQuery.Where(link => !link.MunicipalityId.HasValue || link.MunicipalityId == municipalityId.Value);
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
            .Where(assignment => assignment.UserId == user.Id
                && assignment.IsActive
                && (!assignment.ValidFromUtc.HasValue || assignment.ValidFromUtc <= now)
                && (!assignment.ValidToUtc.HasValue || assignment.ValidToUtc > now))
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
        var managerHierarchyMatch = scope != null && await ManagerHierarchyMatchesAsync(user.Id, scope.OwnerUserId);
        return EvaluatePermission(user, permissionCode, scope, access, managerHierarchyMatch);
    }

    public async Task<IReadOnlyDictionary<string, AccessDecisionResult>> CheckPermissionsBatchAsync(
        ApplicationUser user,
        IEnumerable<string> permissionCodes,
        AccessScopeContext? scope = null)
    {
        var access = await GetEffectiveAccessAsync(user);
        var managerHierarchyMatch = scope != null && await ManagerHierarchyMatchesAsync(user.Id, scope.OwnerUserId);
        return permissionCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(
            code => code,
            code => EvaluatePermission(user, code, scope, access, managerHierarchyMatch),
            StringComparer.OrdinalIgnoreCase);
    }

    private static AccessDecisionResult EvaluatePermission(
        ApplicationUser user,
        string permissionCode,
        AccessScopeContext? scope,
        EffectiveAccessResult access,
        bool managerHierarchyMatch)
    {
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

    public async Task<PagedResponse<RoleAccessMatrixResponse>> BuildRoleAccessMatrixPageAsync(PagedQueryRequest request)
    {
        var now = DateTime.UtcNow;
        var query = _roleManager.Roles.AsNoTracking().Where(role => role.IsActive && role.EffectiveFrom <= now
            && (!role.EffectiveTo.HasValue || role.EffectiveTo > now));
        if (_tenantContext?.MunicipalityId is long municipalityId)
            query = query.Where(role => role.MunicipalityId == municipalityId || !role.MunicipalityId.HasValue);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            query = query.Where(role => (role.Name != null && role.Name.ToLower().Contains(search))
                || role.RoleCode.ToLower().Contains(search)
                || (role.Description != null && role.Description.ToLower().Contains(search)));
        }
        var totalCount = await query.CountAsync();
        query = request.NormalizedSortBy switch
        {
            "code" => request.Descending ? query.OrderByDescending(role => role.RoleCode).ThenBy(role => role.Id) : query.OrderBy(role => role.RoleCode).ThenBy(role => role.Id),
            "createdat" => request.Descending ? query.OrderByDescending(role => role.CreatedAt).ThenBy(role => role.Id) : query.OrderBy(role => role.CreatedAt).ThenBy(role => role.Id),
            _ => request.Descending ? query.OrderByDescending(role => role.Name).ThenBy(role => role.Id) : query.OrderBy(role => role.Name).ThenBy(role => role.Id)
        };
        var roles = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var rows = await BuildRoleAccessRowsAsync(roles);
        return PagedResponse<RoleAccessMatrixResponse>.Create(rows, request.Page, request.PageSize, totalCount);
    }

    private async Task<RoleAccessMatrixResponse[]> BuildRoleAccessRowsAsync(ApplicationRole[] roles)
    {
        if (roles.Length == 0) return [];
        var now = DateTime.UtcNow;
        var roleIds = roles.Select(role => role.Id).ToArray();
        var rules = await _context.RolePermissions.AsNoTracking()
            .Where(link => roleIds.Contains(link.RoleId) && link.IsActive && link.EffectiveFrom <= now
                && (!link.EffectiveTo.HasValue || link.EffectiveTo > now) && link.Permission.IsActive)
            .Select(link => new { link.RoleId, link.Permission.Code, link.IsAllowed }).ToArrayAsync();
        var rolePermissions = rules.GroupBy(link => link.RoleId).ToDictionary(group => group.Key, group =>
        {
            var denied = group.Where(link => !link.IsAllowed).Select(link => link.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return group.Where(link => link.IsAllowed && !denied.Contains(link.Code)).Select(link => link.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToArray();
        });
        var assignmentQuery = _context.SecurityUserRoleAssignments.AsNoTracking()
            .Where(link => roleIds.Contains(link.RoleId) && link.IsActive && link.EffectiveFrom <= now
                && (!link.EffectiveTo.HasValue || link.EffectiveTo > now) && !link.RevokedAt.HasValue);
        if (_tenantContext?.MunicipalityId is long municipalityId)
            assignmentQuery = assignmentQuery.Where(link => link.MunicipalityId == municipalityId);
        var assignments = await assignmentQuery.Include(link => link.User).OrderBy(link => link.AssignedAt).ToArrayAsync();
        var assignedUserIds = assignments.Select(link => link.UserId).Distinct().ToArray();
        var scopeQuery = _context.UserScopes.AsNoTracking()
            .Where(scope => assignedUserIds.Contains(scope.UserId) && scope.IsActive && scope.EffectiveFrom <= now
                && (!scope.EffectiveTo.HasValue || scope.EffectiveTo > now));
        if (_tenantContext?.MunicipalityId is long scopeMunicipalityId)
            scopeQuery = scopeQuery.Where(scope => !scope.MunicipalityId.HasValue || scope.MunicipalityId == scopeMunicipalityId);
        var scopes = await scopeQuery.Include(scope => scope.Department).Include(scope => scope.Unit).ToArrayAsync();
        var navigation = await _context.SecurityNavigationItems.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToArrayAsync();

        return roles.Select(role =>
        {
            var permissionCodes = rolePermissions.GetValueOrDefault(role.Id, Array.Empty<string>());
            var testAssignment = assignments.FirstOrDefault(link => link.RoleId == role.Id);
            var testUserId = testAssignment?.UserId;
            var testUser = testAssignment?.User;
            var testScopes = testUserId != null
                ? scopes.Where(scope => scope.UserId == testUserId).Select(FormatScope)
                    .Concat(testAssignment == null ? [] : FormatAssignmentScopes(testAssignment)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : Array.Empty<string>();

            return new RoleAccessMatrixResponse(
                role.PublicId,
                role.RoleCode,
                role.Name!,
                permissionCodes,
                testScopes,
                BuildAuthorizedNavigation(navigation, permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)).Select(item => item.Label).ToArray(),
                BuildAllowedActions(permissionCodes, false),
                BuildAllowedReports(permissionCodes, false),
                testUser != null ? $"{testUser.FirstName} {testUser.LastName}" : null);
        }).ToArray();
    }

    public async Task<PagedResponse<SystemCoverageAuditResponse>> BuildSystemCoverageAuditPageAsync(PagedQueryRequest request)
    {
        var now = DateTime.UtcNow;
        var roleQuery = _roleManager.Roles.AsNoTracking().Where(role => role.IsActive && role.Name != null
            && role.EffectiveFrom <= now && (!role.EffectiveTo.HasValue || role.EffectiveTo > now));
        if (_tenantContext?.MunicipalityId is long roleMunicipalityId)
            roleQuery = roleQuery.Where(role => role.MunicipalityId == roleMunicipalityId || !role.MunicipalityId.HasValue);
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
        var visibleRoles = await roleQuery.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var matrix = await BuildRoleAccessRowsAsync(visibleRoles);
        var visibleRoleIds = visibleRoles.Select(role => role.Id).ToArray();
        var assignmentQuery = _context.SecurityUserRoleAssignments.AsNoTracking()
            .Where(link => visibleRoleIds.Contains(link.RoleId) && link.IsActive && link.EffectiveFrom <= now
                && (!link.EffectiveTo.HasValue || link.EffectiveTo > now) && !link.RevokedAt.HasValue)
            .AsQueryable();
        var scopeQuery = _context.UserScopes.AsNoTracking()
            .Where(scope => scope.IsActive && scope.EffectiveFrom <= now && (!scope.EffectiveTo.HasValue || scope.EffectiveTo > now))
            .AsQueryable();
        if (_tenantContext?.MunicipalityId is long municipalityId)
        {
            assignmentQuery = assignmentQuery.Where(link => link.MunicipalityId == municipalityId);
            scopeQuery = scopeQuery.Where(scope => !scope.MunicipalityId.HasValue || scope.MunicipalityId == municipalityId);
        }
        var userRoles = await assignmentQuery.ToListAsync();
        var assignedUserIds = userRoles.Select(link => link.UserId).Distinct().ToArray();
        var userScopes = await scopeQuery.Where(scope => assignedUserIds.Contains(scope.UserId)).ToListAsync();

        var rows = visibleRoles.Select(role =>
        {
            var links = userRoles.Where(link => link.RoleId == role.Id).ToArray();
            var row = matrix.Single(item => item.RolePublicId == role.PublicId);
            var hasPermissions = row != null && row.Permissions.Length > 0;
            var hasScopeFiltering = links.Any(link => link.MunicipalityId.HasValue || link.DepartmentId.HasValue || link.UnitId.HasValue
                || userScopes.Any(scope => scope.UserId == link.UserId));
            return new SystemCoverageAuditResponse(
                role.PublicId,
                role.RoleCode,
                role.Name!,
                SeededUser: links.Length > 0,
                Dashboard: row != null && row.Permissions.Any(permission => permission.Equals("Dashboard.View", StringComparison.OrdinalIgnoreCase) || permission == "*"),
                Menu: row != null && row.Menus.Length > 0,
                Permissions: hasPermissions,
                ScopeFiltering: hasScopeFiltering,
                Crud: row != null && row.AllowedActions.Any(action => action.Contains("Create", StringComparison.OrdinalIgnoreCase) || action.Contains("Edit", StringComparison.OrdinalIgnoreCase) || action.Contains("Delete", StringComparison.OrdinalIgnoreCase) || action.Contains("Manage", StringComparison.OrdinalIgnoreCase)),
                WorkflowActions: row != null && row.AllowedActions.Any(action => action.Contains("Submit", StringComparison.OrdinalIgnoreCase) || action.Contains("Verify", StringComparison.OrdinalIgnoreCase) || action.Contains("Approve", StringComparison.OrdinalIgnoreCase) || action.Contains("Review", StringComparison.OrdinalIgnoreCase) || action.Contains("Audit", StringComparison.OrdinalIgnoreCase)),
                Reports: row != null && row.Reports.Length > 0,
                AuditTrail: row != null && row.Permissions.Any(permission => permission.StartsWith("Audit.", StringComparison.OrdinalIgnoreCase) || permission.Contains("Trail", StringComparison.OrdinalIgnoreCase) || permission == "*"),
                Notifications: row != null && row.Permissions.Any(permission => permission.StartsWith("Notifications.", StringComparison.OrdinalIgnoreCase) || permission == "*"));
        }).ToArray();
        return PagedResponse<SystemCoverageAuditResponse>.Create(rows, request.Page, request.PageSize, totalCount);
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

    private static IEnumerable<string> FormatAssignmentScopes(SecurityUserRoleAssignment assignment)
    {
        if (assignment.MunicipalityId.HasValue) yield return $"Municipality:{assignment.MunicipalityId}";
        if (assignment.DepartmentId.HasValue) yield return $"Department:{assignment.DepartmentId}";
        if (assignment.UnitId.HasValue) yield return $"Unit:{assignment.UnitId}";
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

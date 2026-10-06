using System.Security.Claims;
using System.Text.Json;
using FTCERP.Host.API.Responses;
using FTCERP.Host.API.Requests;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/security")]
[Authorize]
public sealed class SecurityAdministrationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAccessControlService _accessControl;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenantContext;

    public SecurityAdministrationController(ApplicationDbContext context, IAccessControlService accessControl, UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
    {
        _context = context;
        _accessControl = accessControl;
        _userManager = userManager;
        _tenantContext = tenantContext;
    }

    [HttpGet("roles")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public ActionResult<ApiResponse<SecurityRoleDto[]>> GetRoles() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityRoleDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/security/roles/page."));

    [HttpGet("roles/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityRoleDto>>>> GetRolesPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool includeInactive = false)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<PagedResponse<SecurityRoleDto>>(false, null, "User not found"));
        if (!SecurityRoleSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityRoleDto>>(false, null,
                "SortBy must be createdAt, name, code, status, or effectiveFrom."));
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        var system = access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
        var municipalities = access.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArray();
        var query = _context.Roles.AsNoTracking().AsQueryable();
        if (!system) query = query.Where(item => item.MunicipalityId.HasValue && municipalities.Contains(item.MunicipalityId.Value));
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.RoleCode.Contains(term) || (item.Name != null && item.Name.Contains(term))
                || (item.Description != null && item.Description.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            ("code", false) => query.OrderBy(item => item.RoleCode).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.RoleCode).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
        };
        var roles = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<SecurityRoleDto>>(true,
            PagedResponse<SecurityRoleDto>.Create(roles.Select(ToRoleDto), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityRoleSortFields = ["createdat", "name", "code", "status", "effectivefrom"];

    [HttpPost("roles")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_ROLES")]
    public async Task<ActionResult<ApiResponse<SecurityRoleDto>>> CreateRole([FromBody] CreateSecurityRoleRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityRoleDto>(false, null, "User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityRoleDto>(false, null, "Select a municipality context before creating a role"));
        var code = request.RoleCode.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (!Regex.IsMatch(code, "^[A-Z][A-Z0-9_]{2,63}$")) return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "Role code must be 3-64 uppercase letters, digits or underscores and start with a letter"));
        if (name.Length is < 3 or > 128) return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "Role name must be 3-128 characters"));
        var municipalityId = _tenantContext.MunicipalityId.Value;
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        var canAdministerTenant = access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase)
            || access.RoleAssignments.Any(item => item.MunicipalityId == municipalityId);
        if (!canAdministerTenant) return Forbid();
        if (await _context.Roles.AnyAsync(item => item.MunicipalityId == municipalityId && item.RoleCode == code))
            return Conflict(new ApiResponse<SecurityRoleDto>(false, null, "Role code already exists in this municipality"));

        var now = DateTime.UtcNow;
        var role = new ApplicationRole { MunicipalityId = municipalityId, RoleCode = code, Name = name, NormalizedName = name.ToUpperInvariant(), Description = request.Description?.Trim(), IsSystemRole = false, IsActive = true, EffectiveFrom = request.EffectiveFrom ?? now, EffectiveTo = request.EffectiveTo, CreatedAt = now };
        if (role.EffectiveTo.HasValue && role.EffectiveTo <= role.EffectiveFrom) return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "EffectiveTo must be later than EffectiveFrom"));
        _context.Roles.Add(role);
        _context.AuditTrails.Add(new AuditTrail { EntityName = "SecurityRole", EntityId = role.PublicId.ToString(), Action = "Create", NewValue = JsonSerializer.Serialize(new { role.RoleCode, role.Name, role.Description, role.MunicipalityId, role.EffectiveFrom, role.EffectiveTo }), ChangedBy = actor.Id, ChangedAt = now, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<SecurityRoleDto>(true, ToRoleDto(role)));
    }

    [HttpPut("roles/{roleId}")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_ROLES")]
    public async Task<ActionResult<ApiResponse<SecurityRoleDto>>> UpdateRole(string roleId, [FromBody] UpdateSecurityRoleRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityRoleDto>(false, null, "User not found"));
        var role = await _context.Roles.FirstOrDefaultAsync(item => item.Id == roleId);
        if (role == null) return NotFound(new ApiResponse<SecurityRoleDto>(false, null, "Role not found"));
        if (!await CanAdministerRoleAsync(actor, role)) return Forbid();
        if (role.IsSystemRole) return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "System roles cannot be changed through tenant administration"));
        byte[] rowVersion;
        try { rowVersion = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "RowVersion is invalid")); }
        var name = request.Name.Trim();
        if (name.Length is < 3 or > 128) return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "Role name must be 3-128 characters"));
        if (request.EffectiveTo.HasValue && request.EffectiveTo <= request.EffectiveFrom) return BadRequest(new ApiResponse<SecurityRoleDto>(false, null, "EffectiveTo must be later than EffectiveFrom"));
        _context.Entry(role).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var oldValue = new { role.Name, role.Description, role.IsActive, role.EffectiveFrom, role.EffectiveTo };
        role.Name = name;
        role.NormalizedName = name.ToUpperInvariant();
        role.Description = request.Description?.Trim();
        role.IsActive = request.IsActive;
        role.EffectiveFrom = request.EffectiveFrom;
        role.EffectiveTo = request.IsActive ? request.EffectiveTo : request.EffectiveTo ?? DateTime.UtcNow;
        role.UpdatedAt = DateTime.UtcNow;
        _context.AuditTrails.Add(new AuditTrail { EntityName = "SecurityRole", EntityId = role.PublicId.ToString(), Action = "Update", OldValue = JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(new { role.Name, role.Description, role.IsActive, role.EffectiveFrom, role.EffectiveTo }), ChangedBy = actor.Id, ChangedAt = role.UpdatedAt.Value, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<SecurityRoleDto>(false, null, "The role changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<SecurityRoleDto>(true, ToRoleDto(role)));
    }

    [HttpGet("users")]
    [Authorize(Policy = "Permission:SECURITY.VIEW_EFFECTIVE")]
    public ActionResult<ApiResponse<SecurityUserDto[]>> GetUsers() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityUserDto[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/security/users/page."));

    [HttpGet("users/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW_EFFECTIVE")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityUserDto>>>> GetUsersPage([FromQuery] PagedQueryRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<PagedResponse<SecurityUserDto>>(false, null, "User not found"));
        if (!SecurityUserSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityUserDto>>(false, null, "SortBy must be createdAt, name, email, or status."));
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        var system = access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
        var municipalities = access.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArray();
        var query = _context.Users.AsNoTracking().AsQueryable();
        var now = DateTime.UtcNow;
        if (!system)
            query = query.Where(user => _context.SecurityUserRoleAssignments.Any(link => link.UserId == user.Id && link.IsActive && !link.RevokedAt.HasValue
                && link.EffectiveFrom <= now && (!link.EffectiveTo.HasValue || link.EffectiveTo > now)
                && link.MunicipalityId.HasValue && municipalities.Contains(link.MunicipalityId.Value)));
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.FirstName.Contains(term) || item.LastName.Contains(term)
                || (item.Email != null && item.Email.Contains(term)) || (item.UserName != null && item.UserName.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.FirstName).ThenBy(item => item.LastName).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.FirstName).ThenByDescending(item => item.LastName).ThenBy(item => item.Id),
            ("email", false) => query.OrderBy(item => item.Email).ThenBy(item => item.Id),
            ("email", true) => query.OrderByDescending(item => item.Email).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.FirstName).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.FirstName).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
        };
        var users = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var rows = users.Select(item => new SecurityUserDto(item.Id, item.FullName, item.Email ?? item.UserName ?? item.Id));
        return Ok(new ApiResponse<PagedResponse<SecurityUserDto>>(true,
            PagedResponse<SecurityUserDto>.Create(rows, request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityUserSortFields = ["createdat", "name", "email", "status"];

    [HttpGet("users/{userId}/roles")]
    [Authorize(Policy = "Permission:SECURITY.ASSIGN_ROLES")]
    public async Task<ActionResult<ApiResponse<UserRoleSecurityConfigurationDto>>> GetUserRoles(string userId)
    {
        var actor = await GetCurrentUserAsync();
        var user = await _userManager.FindByIdAsync(userId);
        if (actor == null) return Unauthorized(new ApiResponse<UserRoleSecurityConfigurationDto>(false, null, "User not found"));
        if (user == null) return NotFound(new ApiResponse<UserRoleSecurityConfigurationDto>(false, null, "User not found"));
        if (!await CanAdministerUserAsync(actor, userId)) return Forbid();
        var now = DateTime.UtcNow;
        var assignmentEntities = await _context.SecurityUserRoleAssignments.AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive && !item.RevokedAt.HasValue && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now))
            .OrderBy(item => item.Role.Name)
            .Include(item => item.Role)
            .ToArrayAsync();
        var departmentIds = assignmentEntities.Where(item => item.DepartmentId.HasValue).Select(item => item.DepartmentId!.Value).Distinct().ToArray();
        var unitIds = assignmentEntities.Where(item => item.UnitId.HasValue).Select(item => item.UnitId!.Value).Distinct().ToArray();
        var departments = await _context.Departments.AsNoTracking().Where(item => departmentIds.Contains(item.Id))
            .Select(item => new { item.Id, item.PublicId, item.Name }).ToDictionaryAsync(item => item.Id);
        var units = await _context.Units.AsNoTracking().Where(item => unitIds.Contains(item.Id))
            .Select(item => new { item.Id, item.PublicId, item.Name }).ToDictionaryAsync(item => item.Id);
        var assignments = assignmentEntities.Select(item =>
        {
            var department = item.DepartmentId.HasValue && departments.TryGetValue(item.DepartmentId.Value, out var foundDepartment) ? foundDepartment : null;
            var unit = item.UnitId.HasValue && units.TryGetValue(item.UnitId.Value, out var foundUnit) ? foundUnit : null;
            return new UserRoleAssignmentDto(item.Id, item.RoleId, item.Role.Name ?? item.Role.RoleCode, item.MunicipalityId, item.DepartmentId, item.UnitId, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion), department?.PublicId, department?.Name, unit?.PublicId, unit?.Name);
        }).ToArray();
        return Ok(new ApiResponse<UserRoleSecurityConfigurationDto>(true, new UserRoleSecurityConfigurationDto(user.Id, user.FullName, assignments)));
    }

    [HttpPut("users/{userId}/roles")]
    [Authorize(Policy = "Permission:SECURITY.ASSIGN_ROLES")]
    public async Task<ActionResult<ApiResponse<bool>>> PutUserRoles(string userId, [FromBody] UpdateUserRoleSecurityRequest request)
    {
        var actor = await GetCurrentUserAsync();
        var user = await _userManager.FindByIdAsync(userId);
        if (actor == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));
        if (!await CanAdministerUserAsync(actor, userId)) return Forbid();
        if (request.Assignments.Select(item => item.RoleId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Assignments.Length)
            return BadRequest(new ApiResponse<bool>(false, false, "A role may be assigned only once in the active configuration"));

        var actorAccess = await _accessControl.GetEffectiveAccessAsync(actor);
        var actorPermissions = actorAccess.EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isSystem = actorPermissions.Contains("SECURITY.SYSTEM_SCOPE");
        var actorMunicipalities = actorAccess.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).ToHashSet();
        var requestedRoleIds = request.Assignments.Select(item => item.RoleId).ToArray();
        var roles = await _context.Roles.Where(item => requestedRoleIds.Contains(item.Id) && item.IsActive).ToArrayAsync();
        if (roles.Length != requestedRoleIds.Length) return BadRequest(new ApiResponse<bool>(false, false, "One or more roles are invalid or inactive"));
        if (!isSystem && roles.Any(role => !role.MunicipalityId.HasValue || !actorMunicipalities.Contains(role.MunicipalityId.Value))) return Forbid();
        if (request.Assignments.Any(item => item.EffectiveTo.HasValue && item.EffectiveTo <= (item.EffectiveFrom ?? DateTime.UtcNow)))
            return BadRequest(new ApiResponse<bool>(false, false, "Every assignment EffectiveTo must be later than EffectiveFrom"));
        var resolvedScopes = new Dictionary<string, (int? DepartmentId, int? UnitId)>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in request.Assignments)
        {
            var role = roles.Single(value => value.Id == item.RoleId);
            var municipalityId = item.MunicipalityId ?? role.MunicipalityId;
            if (!municipalityId.HasValue) return BadRequest(new ApiResponse<bool>(false, false, "Every assignment requires a municipality"));
            if (_tenantContext.MunicipalityId is > 0 && municipalityId != _tenantContext.MunicipalityId)
                return BadRequest(new ApiResponse<bool>(false, false, "Assignment municipality must match the selected context"));
            var departmentId = item.DepartmentId;
            if (item.DepartmentPublicId.HasValue)
            {
                var resolvedDepartmentId = await _context.Departments.Where(value => value.PublicId == item.DepartmentPublicId && value.MunicipalityId == municipalityId && value.IsActive).Select(value => (int?)value.Id).SingleOrDefaultAsync();
                if (!resolvedDepartmentId.HasValue || (departmentId.HasValue && departmentId != resolvedDepartmentId))
                    return BadRequest(new ApiResponse<bool>(false, false, "One or more assignment departments are invalid or inactive"));
                departmentId = resolvedDepartmentId;
            }
            else if (departmentId.HasValue && !await _context.Departments.AnyAsync(value => value.Id == departmentId && value.MunicipalityId == municipalityId && value.IsActive))
                return BadRequest(new ApiResponse<bool>(false, false, "One or more assignment departments are invalid or inactive"));

            var unitId = item.UnitId;
            if (item.UnitPublicId.HasValue)
            {
                var resolvedUnit = await _context.Units.Where(value => value.PublicId == item.UnitPublicId && value.MunicipalityId == municipalityId && value.IsActive).Select(value => new { value.Id, value.DepartmentId }).SingleOrDefaultAsync();
                if (resolvedUnit == null || (unitId.HasValue && unitId != resolvedUnit.Id))
                    return BadRequest(new ApiResponse<bool>(false, false, "One or more assignment units are invalid or inactive"));
                unitId = resolvedUnit.Id;
                if (departmentId.HasValue && departmentId != resolvedUnit.DepartmentId)
                    return BadRequest(new ApiResponse<bool>(false, false, "Assignment unit must belong to its selected department"));
            }
            else if (unitId.HasValue)
            {
                var unitDepartmentId = await _context.Units.Where(value => value.Id == unitId && value.MunicipalityId == municipalityId && value.IsActive).Select(value => (int?)value.DepartmentId).SingleOrDefaultAsync();
                if (!unitDepartmentId.HasValue) return BadRequest(new ApiResponse<bool>(false, false, "One or more assignment units are invalid or inactive"));
                if (departmentId.HasValue && departmentId != unitDepartmentId)
                    return BadRequest(new ApiResponse<bool>(false, false, "Assignment unit must belong to its selected department"));
            }
            resolvedScopes[item.RoleId] = (departmentId, unitId);
        }
        var grantedCodes = await _context.RolePermissions.AsNoTracking().Where(item => requestedRoleIds.Contains(item.RoleId) && item.IsActive && item.IsAllowed).Select(item => item.Permission.Code).Distinct().ToArrayAsync();
        if (grantedCodes.Any(code => !actorPermissions.Contains(code))) return Forbid();

        var now = DateTime.UtcNow;
        var current = await _context.SecurityUserRoleAssignments.Where(item => item.UserId == userId && item.IsActive && !item.RevokedAt.HasValue).ToArrayAsync();
        if (request.ExpectedAssignments.Length != current.Length || request.ExpectedAssignments.Select(item => item.AssignmentId).Order().SequenceEqual(current.Select(item => item.Id).Order()) == false)
            return Conflict(new ApiResponse<bool>(false, false, "Role assignments changed since they were loaded. Refresh and try again."));
        foreach (var expected in request.ExpectedAssignments)
        {
            var assignment = current.Single(item => item.Id == expected.AssignmentId);
            try { _context.Entry(assignment).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(expected.RowVersion); }
            catch (FormatException) { return BadRequest(new ApiResponse<bool>(false, false, "An assignment RowVersion is invalid")); }
        }

        var oldValue = current.Select(item => new { item.Id, item.RoleId, item.MunicipalityId, item.DepartmentId, item.UnitId, item.EffectiveFrom, item.EffectiveTo }).ToArray();
        foreach (var assignment in current)
        {
            assignment.IsActive = false;
            assignment.EffectiveTo = now;
            assignment.RevokedAt = now;
            assignment.RevokedBy = actor.Id;
        }
        foreach (var item in request.Assignments)
        {
            var role = roles.Single(value => value.Id == item.RoleId);
            var municipalityId = item.MunicipalityId ?? role.MunicipalityId;
            if (!isSystem && (!municipalityId.HasValue || !actorMunicipalities.Contains(municipalityId.Value))) return Forbid();
            var resolvedScope = resolvedScopes[item.RoleId];
            _context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment { UserId = userId, RoleId = role.Id, MunicipalityId = municipalityId, DepartmentId = resolvedScope.DepartmentId, UnitId = resolvedScope.UnitId, EffectiveFrom = item.EffectiveFrom ?? now, EffectiveTo = item.EffectiveTo, AssignedBy = actor.Id, AssignedAt = now, IsActive = true });
        }
        var identityLinks = await _context.UserRoles.Where(item => item.UserId == userId).ToArrayAsync();
        _context.UserRoles.RemoveRange(identityLinks);
        _context.UserRoles.AddRange(requestedRoleIds.Select(roleId => new IdentityUserRole<string> { UserId = userId, RoleId = roleId }));
        _context.AuditTrails.Add(new AuditTrail { EntityName = "SecurityUserRoleAssignment", EntityId = userId, Action = "ReplaceRoleAssignments", OldValue = JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(request.Assignments), ChangedBy = actor.Id, ChangedAt = now, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<bool>(false, false, "Role assignments changed since they were loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("resources")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public ActionResult<ApiResponse<SecurityResourceDto[]>> GetResources() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityResourceDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/security/resources/page."));

    [HttpGet("resources/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityResourceDto>>>> GetResourcesPage([FromQuery] PagedQueryRequest request)
    {
        var sortBy = request.SortBy == null ? "code" : request.NormalizedSortBy;
        if (!SecurityResourceSortFields.Contains(sortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityResourceDto>>(false, null,
                "SortBy must be name, code, type, or status."));
        var query = _context.SecurityResources.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term)
                || item.ResourceType.Contains(term) || (item.Description != null && item.Description.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            ("type", false) => query.OrderBy(item => item.ResourceType).ThenBy(item => item.Name).ThenBy(item => item.Id),
            ("type", true) => query.OrderByDescending(item => item.ResourceType).ThenByDescending(item => item.Name).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
            _ => query.OrderBy(item => item.Code).ThenBy(item => item.Id)
        };
        var items = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<SecurityResourceDto>>(true,
            PagedResponse<SecurityResourceDto>.Create(items.Select(ToResourceDto), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityResourceSortFields = ["name", "code", "type", "status"];

    [HttpPost("resources")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_PERMISSIONS")]
    public async Task<ActionResult<ApiResponse<SecurityResourceDto>>> CreateResource([FromBody] CreateSecurityResourceRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityResourceDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityResourceDto>(false, null, "Select a municipality context to record the registry audit."));
        var validation = ValidateResourceInput(request.Code, request.Name, request.ResourceType, request.Description, request.Reason,
            request.SupportsCreate, request.SupportsRead, request.SupportsUpdate, request.SupportsDelete, request.SupportsExport, request.SupportsImport);
        if (validation != null) return BadRequest(new ApiResponse<SecurityResourceDto>(false, null, validation));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.SecurityResources.AnyAsync(item => item.Code == code)) return Conflict(new ApiResponse<SecurityResourceDto>(false, null, "Resource code already exists."));

        var entity = new SecurityResource
        {
            Code = code, Name = request.Name.Trim(), ResourceType = request.ResourceType.Trim().ToUpperInvariant(), Description = NullIfWhiteSpace(request.Description),
            SupportsCreate = request.SupportsCreate, SupportsRead = request.SupportsRead, SupportsUpdate = request.SupportsUpdate,
            SupportsDelete = request.SupportsDelete, SupportsExport = request.SupportsExport, SupportsImport = request.SupportsImport,
            SupportsFieldSecurity = request.SupportsFieldSecurity, SupportsRecordCriteria = request.SupportsRecordCriteria, IsActive = true
        };
        _context.SecurityResources.Add(entity);
        await SyncResourcePermissionsAsync(entity);
        _context.AuditTrails.Add(NewRegistryAudit(actor, nameof(SecurityResource), entity.PublicId, "Create", null, ResourceAuditValue(entity), request.Reason));
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<SecurityResourceDto>(true, ToResourceDto(entity)));
    }

    [HttpPut("resources/{publicId:guid}")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_PERMISSIONS")]
    public async Task<ActionResult<ApiResponse<SecurityResourceDto>>> UpdateResource(Guid publicId, [FromBody] UpdateSecurityResourceRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityResourceDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityResourceDto>(false, null, "Select a municipality context to record the registry audit."));
        var entity = await _context.SecurityResources.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(new ApiResponse<SecurityResourceDto>(false, null, "Security resource not found."));
        var validation = ValidateResourceInput(entity.Code, request.Name, request.ResourceType, request.Description, request.Reason,
            request.SupportsCreate, request.SupportsRead, request.SupportsUpdate, request.SupportsDelete, request.SupportsExport, request.SupportsImport);
        if (validation != null) return BadRequest(new ApiResponse<SecurityResourceDto>(false, null, validation));
        if (!TryDecodeRowVersion(request.RowVersion, out var rowVersion)) return BadRequest(new ApiResponse<SecurityResourceDto>(false, null, "RowVersion is invalid."));
        _context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var oldValue = ResourceAuditValue(entity);
        entity.Name = request.Name.Trim();
        entity.ResourceType = request.ResourceType.Trim().ToUpperInvariant();
        entity.Description = NullIfWhiteSpace(request.Description);
        entity.SupportsCreate = request.SupportsCreate; entity.SupportsRead = request.SupportsRead; entity.SupportsUpdate = request.SupportsUpdate;
        entity.SupportsDelete = request.SupportsDelete; entity.SupportsExport = request.SupportsExport; entity.SupportsImport = request.SupportsImport;
        entity.SupportsFieldSecurity = request.SupportsFieldSecurity; entity.SupportsRecordCriteria = request.SupportsRecordCriteria; entity.IsActive = request.IsActive;
        await SyncResourcePermissionsAsync(entity);
        await SyncChildPermissionsAsync(entity.Code, entity.IsActive, entity.SupportsFieldSecurity);
        _context.AuditTrails.Add(NewRegistryAudit(actor, nameof(SecurityResource), entity.PublicId, "Update", oldValue, ResourceAuditValue(entity), request.Reason));
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<SecurityResourceDto>(false, null, "The security resource changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<SecurityResourceDto>(true, ToResourceDto(entity)));
    }

    [HttpGet("actions")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public ActionResult<ApiResponse<SecurityActionDto[]>> GetActions() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityActionDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/security/actions/page."));

    [HttpGet("actions/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityActionDto>>>> GetActionsPage([FromQuery] PagedQueryRequest request)
    {
        var sortBy = request.SortBy == null ? "code" : request.NormalizedSortBy;
        if (!SecurityActionSortFields.Contains(sortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityActionDto>>(false, null,
                "SortBy must be name, code, resource, or status."));
        var query = _context.SecurityActionDefinitions.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term)
                || item.ResourceCode.Contains(term) || (item.Description != null && item.Description.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            ("resource", false) => query.OrderBy(item => item.ResourceCode).ThenBy(item => item.Name).ThenBy(item => item.Id),
            ("resource", true) => query.OrderByDescending(item => item.ResourceCode).ThenByDescending(item => item.Name).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
            _ => query.OrderBy(item => item.Code).ThenBy(item => item.Id)
        };
        var items = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<SecurityActionDto>>(true,
            PagedResponse<SecurityActionDto>.Create(items.Select(ToActionDto), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityActionSortFields = ["name", "code", "resource", "status"];

    [HttpPost("actions")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_PERMISSIONS")]
    public async Task<ActionResult<ApiResponse<SecurityActionDto>>> CreateAction([FromBody] CreateSecurityActionRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityActionDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityActionDto>(false, null, "Select a municipality context to record the registry audit."));
        var validation = ValidateActionInput(request.Code, request.Name, request.ResourceCode, request.Description, request.Reason);
        if (validation != null) return BadRequest(new ApiResponse<SecurityActionDto>(false, null, validation));
        var code = request.Code.Trim().ToUpperInvariant();
        var resourceCode = request.ResourceCode.Trim().ToUpperInvariant();
        if (!await _context.SecurityResources.AnyAsync(item => item.Code == resourceCode && item.IsActive)) return BadRequest(new ApiResponse<SecurityActionDto>(false, null, "The action must reference an active registered resource."));
        if (await _context.SecurityActionDefinitions.AnyAsync(item => item.Code == code)) return Conflict(new ApiResponse<SecurityActionDto>(false, null, "Action code already exists."));
        var conflictingPermission = await _context.Permissions.AnyAsync(item => item.Code == code && (item.Kind != SecurityPermissionKind.Action || item.ActionCode != code));
        if (conflictingPermission) return Conflict(new ApiResponse<SecurityActionDto>(false, null, "Action code conflicts with another registered permission."));

        var entity = new SecurityActionDefinition { Code = code, Name = request.Name.Trim(), ResourceCode = resourceCode, Description = NullIfWhiteSpace(request.Description), IsActive = true };
        _context.SecurityActionDefinitions.Add(entity);
        await SyncActionPermissionAsync(entity, true);
        _context.AuditTrails.Add(NewRegistryAudit(actor, nameof(SecurityActionDefinition), entity.PublicId, "Create", null, ActionAuditValue(entity), request.Reason));
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<SecurityActionDto>(true, ToActionDto(entity)));
    }

    [HttpPut("actions/{publicId:guid}")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_PERMISSIONS")]
    public async Task<ActionResult<ApiResponse<SecurityActionDto>>> UpdateAction(Guid publicId, [FromBody] UpdateSecurityActionRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityActionDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityActionDto>(false, null, "Select a municipality context to record the registry audit."));
        var entity = await _context.SecurityActionDefinitions.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(new ApiResponse<SecurityActionDto>(false, null, "Security action not found."));
        var validation = ValidateActionInput(entity.Code, request.Name, entity.ResourceCode, request.Description, request.Reason);
        if (validation != null) return BadRequest(new ApiResponse<SecurityActionDto>(false, null, validation));
        if (!TryDecodeRowVersion(request.RowVersion, out var rowVersion)) return BadRequest(new ApiResponse<SecurityActionDto>(false, null, "RowVersion is invalid."));
        var resourceActive = await _context.SecurityResources.AnyAsync(item => item.Code == entity.ResourceCode && item.IsActive);
        if (request.IsActive && !resourceActive) return BadRequest(new ApiResponse<SecurityActionDto>(false, null, "An action cannot be activated while its resource is inactive."));
        _context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var oldValue = ActionAuditValue(entity);
        entity.Name = request.Name.Trim(); entity.Description = NullIfWhiteSpace(request.Description); entity.IsActive = request.IsActive;
        await SyncActionPermissionAsync(entity, resourceActive);
        _context.AuditTrails.Add(NewRegistryAudit(actor, nameof(SecurityActionDefinition), entity.PublicId, "Update", oldValue, ActionAuditValue(entity), request.Reason));
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<SecurityActionDto>(false, null, "The security action changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<SecurityActionDto>(true, ToActionDto(entity)));
    }

    [HttpGet("navigation/registry")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public ActionResult<ApiResponse<SecurityNavigationDto[]>> GetNavigationRegistry() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityNavigationDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/security/navigation/registry/page."));

    [HttpGet("navigation/registry/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityNavigationDto>>>> GetNavigationRegistryPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool? active = null)
    {
        var sortBy = request.SortBy == null ? "order" : request.NormalizedSortBy;
        if (!SecurityNavigationSortFields.Contains(sortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityNavigationDto>>(false, null,
                "SortBy must be order, name, code, parent, route, or status."));
        var query = _context.SecurityNavigationItems.AsNoTracking().Include(item => item.Parent).AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term)
                || (item.Route != null && item.Route.Contains(term)) || (item.RequiredPermissionCode != null && item.RequiredPermissionCode.Contains(term))
                || (item.Parent != null && (item.Parent.Code.Contains(term) || item.Parent.Name.Contains(term))));
        }
        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
            ("parent", false) => query.OrderBy(item => item.Parent == null ? string.Empty : item.Parent.Name).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Id),
            ("parent", true) => query.OrderByDescending(item => item.Parent == null ? string.Empty : item.Parent.Name).ThenByDescending(item => item.DisplayOrder).ThenBy(item => item.Id),
            ("route", false) => query.OrderBy(item => item.Route).ThenBy(item => item.Id),
            ("route", true) => query.OrderByDescending(item => item.Route).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("order", true) => query.OrderByDescending(item => item.ParentId).ThenByDescending(item => item.DisplayOrder).ThenByDescending(item => item.Name).ThenBy(item => item.Id),
            _ => query.OrderBy(item => item.ParentId).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.Id)
        };
        var items = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<SecurityNavigationDto>>(true,
            PagedResponse<SecurityNavigationDto>.Create(items.Select(ToNavigationDto), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityNavigationSortFields = ["order", "name", "code", "parent", "route", "status"];

    [HttpPost("navigation/registry")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_NAVIGATION")]
    public async Task<ActionResult<ApiResponse<SecurityNavigationDto>>> CreateNavigationItem([FromBody] CreateSecurityNavigationRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityNavigationDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityNavigationDto>(false, null, "Select a municipality context to record the registry audit."));
        var validation = ValidateNavigationInput(request.Code, request.Name, request.Route, request.DisplayOrder, request.Reason);
        if (validation != null) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, validation));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.SecurityNavigationItems.AnyAsync(item => item.Code == code)) return Conflict(new ApiResponse<SecurityNavigationDto>(false, null, "Navigation code already exists."));
        var parent = await ResolveNavigationParentAsync(request.ParentPublicId);
        if (request.ParentPublicId.HasValue && parent == null) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, "Parent navigation item was not found or is inactive."));
        if (!await IsRegisteredPermissionAsync(request.RequiredPermissionCode)) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, "Required permission code is not active or registered."));

        var entity = new SecurityNavigationItem
        {
            Code = code,
            Name = request.Name.Trim(),
            Route = NormalizeRoute(request.Route),
            IconKey = NullIfWhiteSpace(request.IconKey),
            DisplayOrder = request.DisplayOrder,
            RequiredPermissionCode = NullIfWhiteSpace(request.RequiredPermissionCode)?.ToUpperInvariant(),
            Parent = parent,
            ParentId = parent?.Id,
            IsActive = true
        };
        _context.SecurityNavigationItems.Add(entity);
        _context.AuditTrails.Add(NewNavigationAudit(actor, entity, "Create", null, new { entity.Code, entity.Name, request.ParentPublicId, entity.Route, entity.IconKey, entity.DisplayOrder, entity.RequiredPermissionCode, entity.IsActive }, request.Reason));
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<SecurityNavigationDto>(true, ToNavigationDto(entity)));
    }

    [HttpPut("navigation/registry/{publicId:guid}")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_NAVIGATION")]
    public async Task<ActionResult<ApiResponse<SecurityNavigationDto>>> UpdateNavigationItem(Guid publicId, [FromBody] UpdateSecurityNavigationRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityNavigationDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityNavigationDto>(false, null, "Select a municipality context to record the registry audit."));
        var entity = await _context.SecurityNavigationItems.Include(item => item.Parent).FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(new ApiResponse<SecurityNavigationDto>(false, null, "Navigation item not found."));
        var validation = ValidateNavigationInput(entity.Code, request.Name, request.Route, request.DisplayOrder, request.Reason);
        if (validation != null) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, validation));
        byte[] rowVersion;
        try { rowVersion = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, "RowVersion is invalid.")); }
        var parent = await ResolveNavigationParentAsync(request.ParentPublicId);
        if (request.ParentPublicId.HasValue && parent == null) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, "Parent navigation item was not found or is inactive."));
        if (parent?.Id == entity.Id || (parent != null && await WouldCreateNavigationCycleAsync(entity.Id, parent))) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, "Navigation hierarchy cannot contain a cycle."));
        if (!request.IsActive && await _context.SecurityNavigationItems.AnyAsync(item => item.ParentId == entity.Id && item.IsActive)) return Conflict(new ApiResponse<SecurityNavigationDto>(false, null, "Deactivate or move active child items first."));
        if (!await IsRegisteredPermissionAsync(request.RequiredPermissionCode)) return BadRequest(new ApiResponse<SecurityNavigationDto>(false, null, "Required permission code is not active or registered."));

        _context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var oldValue = new { entity.Name, ParentPublicId = entity.Parent?.PublicId, entity.Route, entity.IconKey, entity.DisplayOrder, entity.RequiredPermissionCode, entity.IsActive };
        entity.Name = request.Name.Trim();
        entity.Route = NormalizeRoute(request.Route);
        entity.IconKey = NullIfWhiteSpace(request.IconKey);
        entity.DisplayOrder = request.DisplayOrder;
        entity.RequiredPermissionCode = NullIfWhiteSpace(request.RequiredPermissionCode)?.ToUpperInvariant();
        entity.Parent = parent;
        entity.ParentId = parent?.Id;
        entity.IsActive = request.IsActive;
        var newValue = new { entity.Name, ParentPublicId = parent?.PublicId, entity.Route, entity.IconKey, entity.DisplayOrder, entity.RequiredPermissionCode, entity.IsActive };
        _context.AuditTrails.Add(NewNavigationAudit(actor, entity, "Update", oldValue, newValue, request.Reason));
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<SecurityNavigationDto>(false, null, "The navigation item changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<SecurityNavigationDto>(true, ToNavigationDto(entity)));
    }

    [HttpGet("members")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public ActionResult<ApiResponse<SecurityMemberDto[]>> GetMembers() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityMemberDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/security/members/page."));

    [HttpGet("members/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityMemberDto>>>> GetMembersPage([FromQuery] PagedQueryRequest request)
    {
        var sortBy = request.SortBy == null ? "resource" : request.NormalizedSortBy;
        if (!SecurityMemberSortFields.Contains(sortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityMemberDto>>(false, null,
                "SortBy must be name, code, resource, sensitive, or status."));
        var query = _context.SecurityMemberDefinitions.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.ResourceCode.Contains(term) || item.MemberCode.Contains(term) || item.DisplayName.Contains(term));
        }
        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.DisplayName).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.DisplayName).ThenBy(item => item.Id),
            ("resource", false) => query.OrderBy(item => item.ResourceCode).ThenBy(item => item.DisplayName).ThenBy(item => item.Id),
            ("resource", true) => query.OrderByDescending(item => item.ResourceCode).ThenByDescending(item => item.DisplayName).ThenBy(item => item.Id),
            ("sensitive", false) => query.OrderBy(item => item.IsSensitive).ThenBy(item => item.ResourceCode).ThenBy(item => item.Id),
            ("sensitive", true) => query.OrderByDescending(item => item.IsSensitive).ThenBy(item => item.ResourceCode).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.ResourceCode).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.ResourceCode).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.MemberCode).ThenBy(item => item.Id),
            _ => query.OrderBy(item => item.MemberCode).ThenBy(item => item.Id)
        };
        var items = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<SecurityMemberDto>>(true,
            PagedResponse<SecurityMemberDto>.Create(items.Select(ToMemberDto), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityMemberSortFields = ["name", "code", "resource", "sensitive", "status"];

    [HttpPut("members/{publicId:guid}")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_PERMISSIONS")]
    public async Task<ActionResult<ApiResponse<SecurityMemberDto>>> UpdateMember(Guid publicId, [FromBody] UpdateSecurityMemberRequest request)
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityMemberDto>(false, null, "User not found"));
        if (!await CanManageGlobalRegistryAsync(actor)) return Forbid();
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<SecurityMemberDto>(false, null, "Select a municipality context to record the registry audit."));
        var entity = await _context.SecurityMemberDefinitions.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(new ApiResponse<SecurityMemberDto>(false, null, "Security member not found."));
        if (request.DisplayName.Trim().Length is < 2 or > 120) return BadRequest(new ApiResponse<SecurityMemberDto>(false, null, "Member display name must be 2-120 characters."));
        if (request.Reason.Trim().Length is < 5 or > 500) return BadRequest(new ApiResponse<SecurityMemberDto>(false, null, "An audit reason of 5-500 characters is required."));
        if (!TryDecodeRowVersion(request.RowVersion, out var rowVersion)) return BadRequest(new ApiResponse<SecurityMemberDto>(false, null, "RowVersion is invalid."));
        var resourceActive = await _context.SecurityResources.AnyAsync(item => item.Code == entity.ResourceCode && item.IsActive && item.SupportsFieldSecurity);
        if (request.IsActive && !resourceActive) return BadRequest(new ApiResponse<SecurityMemberDto>(false, null, "A member cannot be activated unless its resource is active and supports field security."));
        _context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var oldValue = MemberAuditValue(entity);
        entity.DisplayName = request.DisplayName.Trim(); entity.IsSensitive = request.IsSensitive; entity.IsActive = request.IsActive;
        await SyncMemberPermissionsAsync(entity, resourceActive);
        _context.AuditTrails.Add(NewRegistryAudit(actor, nameof(SecurityMemberDefinition), entity.PublicId, "Update", oldValue, MemberAuditValue(entity), request.Reason));
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<SecurityMemberDto>(false, null, "The security member changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<SecurityMemberDto>(true, ToMemberDto(entity)));
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public ActionResult<ApiResponse<SecurityPermissionDefinitionDto[]>> GetPermissionDefinitions() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<SecurityPermissionDefinitionDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/security/permissions/page."));

    [HttpGet("permissions/page")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SecurityPermissionDefinitionDto>>>> GetPermissionDefinitionsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? kinds = null)
    {
        var sortBy = request.SortBy == null ? "code" : request.NormalizedSortBy;
        if (!SecurityPermissionDefinitionSortFields.Contains(sortBy))
            return BadRequest(new ApiResponse<PagedResponse<SecurityPermissionDefinitionDto>>(false, null,
                "SortBy must be code, kind, or resource."));
        var requestedKinds = new List<SecurityPermissionKind>();
        foreach (var value in (kinds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<SecurityPermissionKind>(value, true, out var parsedKind))
                return BadRequest(new ApiResponse<PagedResponse<SecurityPermissionDefinitionDto>>(false, null,
                    "Kinds must contain only Resource, Navigation, Member, Action, or Report."));
            if (!requestedKinds.Contains(parsedKind)) requestedKinds.Add(parsedKind);
        }
        var query = _context.Permissions.AsNoTracking().Where(item => item.IsActive);
        if (requestedKinds.Count > 0) query = query.Where(item => requestedKinds.Contains(item.Kind));
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || (item.Description != null && item.Description.Contains(term))
                || (item.ResourceCode != null && item.ResourceCode.Contains(term)) || (item.MemberCode != null && item.MemberCode.Contains(term))
                || (item.NavigationCode != null && item.NavigationCode.Contains(term)) || (item.ActionCode != null && item.ActionCode.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("kind", false) => query.OrderBy(item => item.Kind).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("kind", true) => query.OrderByDescending(item => item.Kind).ThenByDescending(item => item.Code).ThenBy(item => item.Id),
            ("resource", false) => query.OrderBy(item => item.ResourceCode).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("resource", true) => query.OrderByDescending(item => item.ResourceCode).ThenByDescending(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
            _ => query.OrderBy(item => item.Code).ThenBy(item => item.Id)
        };
        var definitions = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var items = definitions.Select(ToPermissionDefinitionDto);
        return Ok(new ApiResponse<PagedResponse<SecurityPermissionDefinitionDto>>(true,
            PagedResponse<SecurityPermissionDefinitionDto>.Create(items, request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SecurityPermissionDefinitionSortFields = ["code", "kind", "resource"];

    [HttpGet("roles/{roleId}/permissions")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<RoleSecurityConfigurationDto>>> GetRolePermissions(string roleId)
    {
        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(item => item.Id == roleId);
        if (role == null) return NotFound(new ApiResponse<RoleSecurityConfigurationDto>(false, null, "Role not found"));
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<RoleSecurityConfigurationDto>(false, null, "User not found"));
        if (!await CanAdministerRoleAsync(actor, role)) return Forbid();
        var permissionRows = await _context.RolePermissions.AsNoTracking().Where(item => item.RoleId == roleId && item.IsActive)
            .Include(item => item.Permission)
            .OrderBy(item => item.Permission.Code)
            .ToArrayAsync();
        var rows = permissionRows.Select(item => new RoleSecurityPermissionDto(item.Permission.Code, item.Permission.Kind.ToString(), item.Permission.ResourceCode, item.Permission.MemberCode, item.Permission.NavigationCode, item.Permission.ActionCode, item.IsAllowed ? "ALLOW" : "DENY", item.ScopeType.HasValue ? item.ScopeType.ToString() : null, Convert.ToBase64String(item.RowVersion))).ToArray();
        return Ok(new ApiResponse<RoleSecurityConfigurationDto>(true, new RoleSecurityConfigurationDto(role.Id, role.PublicId, role.Name ?? role.RoleCode, Convert.ToBase64String(role.RowVersion), rows)));
    }

    [HttpPut("roles/{roleId}/permissions")]
    [Authorize(Policy = "Permission:SECURITY.MANAGE_PERMISSIONS")]
    public async Task<ActionResult<ApiResponse<bool>>> PutRolePermissions(string roleId, [FromBody] UpdateRoleSecurityRequest request)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(item => item.Id == roleId && item.IsActive);
        if (role == null) return NotFound(new ApiResponse<bool>(false, false, "Role not found"));
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        if (!await CanAdministerRoleAsync(actor, role)) return Forbid();
        var actorAccess = await _accessControl.GetEffectiveAccessAsync(actor);
        var actorPermissions = actorAccess.EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        byte[] expectedRoleVersion;
        try { expectedRoleVersion = Convert.FromBase64String(request.RoleRowVersion); }
        catch (FormatException) { return BadRequest(new ApiResponse<bool>(false, false, "RoleRowVersion is invalid")); }
        _context.Entry(role).Property(item => item.RowVersion).OriginalValue = expectedRoleVersion;
        var requestedCodes = request.Permissions.Select(item => item.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var definitions = await _context.Permissions.Where(item => requestedCodes.Contains(item.Code)).ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase);
        if (definitions.Count != requestedCodes.Length) return BadRequest(new ApiResponse<bool>(false, false, "One or more permission codes are not registered"));
        if (requestedCodes.Any(code => !actorPermissions.Contains(code))) return Forbid();
        if (request.Permissions.Any(item => string.Equals(item.ScopeType, nameof(ScopeType.System), StringComparison.OrdinalIgnoreCase)) && !actorPermissions.Contains("SECURITY.SYSTEM_SCOPE")) return Forbid();

        if (request.Permissions.Any(item => !item.State.Equals("ALLOW", StringComparison.OrdinalIgnoreCase) && !item.State.Equals("DENY", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new ApiResponse<bool>(false, false, "Permission state must be ALLOW or DENY"));
        if (request.Permissions.Any(item => !string.IsNullOrWhiteSpace(item.ScopeType) && !Enum.TryParse<ScopeType>(item.ScopeType, true, out _)))
            return BadRequest(new ApiResponse<bool>(false, false, "One or more scope values are invalid"));

        var now = DateTime.UtcNow;
        var current = await _context.RolePermissions.Where(item => item.RoleId == roleId).ToListAsync();
        var oldValue = current.Where(item => item.IsActive).Select(item => new { item.PermissionId, item.IsAllowed, Scope = item.ScopeType == null ? null : item.ScopeType.ToString() }).ToArray();
        foreach (var row in current)
        {
            row.IsActive = false;
            row.EffectiveTo = now;
        }
        foreach (var item in request.Permissions)
        {
            var definition = definitions[item.PermissionCode];
            var existing = current.FirstOrDefault(row => row.PermissionId == definition.Id);
            ScopeType? scope = string.IsNullOrWhiteSpace(item.ScopeType) ? null : Enum.Parse<ScopeType>(item.ScopeType, true);
            if (existing == null)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = definition.Id, IsAllowed = item.State.Equals("ALLOW", StringComparison.OrdinalIgnoreCase), ScopeType = scope, IsActive = true, EffectiveFrom = now });
            }
            else
            {
                existing.IsAllowed = item.State.Equals("ALLOW", StringComparison.OrdinalIgnoreCase);
                existing.ScopeType = scope;
                existing.IsActive = true;
                existing.EffectiveFrom = now;
                existing.EffectiveTo = null;
            }
        }
        role.UpdatedAt = now;
        _context.AuditTrails.Add(new AuditTrail
        {
            EntityName = "SecurityRolePermissions",
            EntityId = role.PublicId.ToString(),
            Action = "ReplacePermissions",
            OldValue = JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(request.Permissions),
            ChangedBy = actor.Id,
            ChangedAt = now,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<bool>(false, false, "The role security configuration changed since it was loaded. Refresh and try again."));
        }
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("effective-permissions/{userId}")]
    [Authorize(Policy = "Permission:SECURITY.VIEW_EFFECTIVE")]
    public async Task<ActionResult<ApiResponse<EffectiveSecurityDto>>> GetEffectivePermissions(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new ApiResponse<EffectiveSecurityDto>(false, null, "User not found"));
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<EffectiveSecurityDto>(false, null, "User not found"));
        var actorAccess = await _accessControl.GetEffectiveAccessAsync(actor);
        var targetAccess = await _accessControl.GetEffectiveAccessAsync(user);
        var actorMunicipalities = actorAccess.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).ToHashSet();
        var targetMunicipalities = targetAccess.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).ToHashSet();
        var isSystem = actorAccess.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
        if (!isSystem && (targetMunicipalities.Count == 0 || !targetMunicipalities.IsSubsetOf(actorMunicipalities))) return Forbid();
        var access = targetAccess;
        return Ok(new ApiResponse<EffectiveSecurityDto>(true, new EffectiveSecurityDto(user.Id, access.Roles, access.EffectivePermissions, access.Scopes.Select(item => item.ScopeType.ToString()).ToArray(), access.Assignments.Select(item => item.AssignmentType.ToString()).ToArray())));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : await _userManager.FindByIdAsync(userId);
    }

    private static SecurityRoleDto ToRoleDto(ApplicationRole item) => new(item.Id, item.PublicId, item.RoleCode, item.Name ?? item.RoleCode, item.Description, item.MunicipalityId, item.IsSystemRole, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion));
    private static SecurityNavigationDto ToNavigationDto(SecurityNavigationItem item) => new(item.PublicId, item.Code, item.Parent?.PublicId, item.Name, item.Route, item.IconKey, item.DisplayOrder, item.RequiredPermissionCode, item.IsActive, Convert.ToBase64String(item.RowVersion), item.Parent?.Code, item.Parent?.Name);

    private async Task<bool> CanManageGlobalRegistryAsync(ApplicationUser actor)
    {
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        return access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
    }

    private Task<SecurityNavigationItem?> ResolveNavigationParentAsync(Guid? publicId) => publicId.HasValue
        ? _context.SecurityNavigationItems.FirstOrDefaultAsync(item => item.PublicId == publicId.Value && item.IsActive)
        : Task.FromResult<SecurityNavigationItem?>(null);

    private async Task<bool> WouldCreateNavigationCycleAsync(int itemId, SecurityNavigationItem candidateParent)
    {
        var cursor = candidateParent;
        while (true)
        {
            if (cursor.Id == itemId) return true;
            if (!cursor.ParentId.HasValue) return false;
            var next = await _context.SecurityNavigationItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == cursor.ParentId.Value);
            if (next == null) return false;
            cursor = next;
        }
    }

    private Task<bool> IsRegisteredPermissionAsync(string? permissionCode) => string.IsNullOrWhiteSpace(permissionCode)
        ? Task.FromResult(true)
        : _context.Permissions.AnyAsync(item => item.Code == permissionCode.Trim().ToUpperInvariant() && item.IsActive);

    private AuditTrail NewNavigationAudit(ApplicationUser actor, SecurityNavigationItem item, string action, object? oldValue, object newValue, string reason) => new()
    {
        EntityName = nameof(SecurityNavigationItem), EntityId = item.PublicId.ToString(), Action = action,
        OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(newValue),
        ChangedBy = actor.Id, ChangedAt = DateTime.UtcNow, Reason = reason.Trim(), CorrelationId = HttpContext.TraceIdentifier,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString()
    };

    private static string? ValidateNavigationInput(string code, string name, string? route, int displayOrder, string reason)
    {
        if (!Regex.IsMatch(code.Trim().ToUpperInvariant(), "^[A-Z][A-Z0-9_.]{2,127}$")) return "Navigation code must be 3-128 uppercase letters, digits, dots or underscores and start with a letter.";
        if (name.Trim().Length is < 2 or > 120) return "Navigation name must be 2-120 characters.";
        if (displayOrder is < 0 or > 100000) return "Display order must be between 0 and 100000.";
        if (!string.IsNullOrWhiteSpace(route) && (!route.Trim().StartsWith('/') || route.Trim().StartsWith("//", StringComparison.Ordinal))) return "Route must be an application-relative path beginning with one slash.";
        if (reason.Trim().Length is < 5 or > 500) return "An audit reason of 5-500 characters is required.";
        return null;
    }

    private static string? NormalizeRoute(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SecurityResourceDto ToResourceDto(SecurityResource item) => new(item.PublicId, item.Code, item.Name, item.ResourceType, item.Description,
        item.SupportsCreate, item.SupportsRead, item.SupportsUpdate, item.SupportsDelete, item.SupportsExport, item.SupportsImport,
        item.SupportsFieldSecurity, item.SupportsRecordCriteria, item.IsActive, Convert.ToBase64String(item.RowVersion));

    private static SecurityActionDto ToActionDto(SecurityActionDefinition item) => new(item.PublicId, item.Code, item.Name, item.ResourceCode,
        item.Description, item.IsActive, Convert.ToBase64String(item.RowVersion));

    private static SecurityMemberDto ToMemberDto(SecurityMemberDefinition item) => new(item.PublicId, item.ResourceCode, item.MemberCode,
        item.DisplayName, item.IsSensitive, item.IsSystemManaged, item.IsActive, Convert.ToBase64String(item.RowVersion));

    private static SecurityPermissionDefinitionDto ToPermissionDefinitionDto(Permission item) => new(item.Code, item.Description,
        item.Kind.ToString(), item.ResourceCode, item.Operation?.ToString(), item.MemberCode, item.NavigationCode, item.ActionCode);

    private static object ResourceAuditValue(SecurityResource item) => new
    {
        item.Code, item.Name, item.ResourceType, item.Description, item.SupportsCreate, item.SupportsRead, item.SupportsUpdate,
        item.SupportsDelete, item.SupportsExport, item.SupportsImport, item.SupportsFieldSecurity, item.SupportsRecordCriteria, item.IsActive
    };

    private static object ActionAuditValue(SecurityActionDefinition item) => new { item.Code, item.Name, item.ResourceCode, item.Description, item.IsActive };
    private static object MemberAuditValue(SecurityMemberDefinition item) => new { item.ResourceCode, item.MemberCode, item.DisplayName, item.IsSensitive, item.IsSystemManaged, item.IsActive };

    private AuditTrail NewRegistryAudit(ApplicationUser actor, string entityName, Guid publicId, string action, object? oldValue, object newValue, string reason) => new()
    {
        EntityName = entityName, EntityId = publicId.ToString(), Action = action,
        OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(newValue),
        ChangedBy = actor.Id, ChangedAt = DateTime.UtcNow, Reason = reason.Trim(), CorrelationId = HttpContext.TraceIdentifier,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString()
    };

    private static string? ValidateResourceInput(string code, string name, string resourceType, string? description, string reason,
        bool create, bool read, bool update, bool delete, bool export, bool import)
    {
        if (!Regex.IsMatch(code.Trim().ToUpperInvariant(), "^[A-Z][A-Z0-9_]{2,63}$")) return "Resource code must be 3-64 uppercase letters, digits or underscores and start with a letter.";
        if (name.Trim().Length is < 2 or > 120) return "Resource name must be 2-120 characters.";
        if (!RegistryResourceTypes.Contains(resourceType.Trim().ToUpperInvariant())) return "Resource type must be ENTITY, REPORT, WORKFLOW, or SERVICE.";
        if (description?.Trim().Length > 500) return "Resource description cannot exceed 500 characters.";
        if (!(create || read || update || delete || export || import)) return "A resource must support at least one operation.";
        if (reason.Trim().Length is < 5 or > 500) return "An audit reason of 5-500 characters is required.";
        return null;
    }

    private static string? ValidateActionInput(string code, string name, string resourceCode, string? description, string reason)
    {
        if (!Regex.IsMatch(code.Trim().ToUpperInvariant(), "^[A-Z][A-Z0-9_]*(\\.[A-Z][A-Z0-9_]*)+$")) return "Action code must contain uppercase dot-separated stable segments.";
        if (!Regex.IsMatch(resourceCode.Trim().ToUpperInvariant(), "^[A-Z][A-Z0-9_]{2,63}$")) return "Resource code is invalid.";
        if (name.Trim().Length is < 2 or > 120) return "Action name must be 2-120 characters.";
        if (description?.Trim().Length > 500) return "Action description cannot exceed 500 characters.";
        if (reason.Trim().Length is < 5 or > 500) return "An audit reason of 5-500 characters is required.";
        return null;
    }

    private static bool TryDecodeRowVersion(string value, out byte[] rowVersion)
    {
        try { rowVersion = Convert.FromBase64String(value); return rowVersion.Length > 0; }
        catch (FormatException) { rowVersion = []; return false; }
    }

    private async Task SyncResourcePermissionsAsync(SecurityResource resource)
    {
        var operations = new Dictionary<SecurityOperation, bool>
        {
            [SecurityOperation.Create] = resource.SupportsCreate, [SecurityOperation.Read] = resource.SupportsRead,
            [SecurityOperation.Update] = resource.SupportsUpdate, [SecurityOperation.Delete] = resource.SupportsDelete,
            [SecurityOperation.Export] = resource.SupportsExport, [SecurityOperation.Import] = resource.SupportsImport
        };
        var codes = operations.Keys.ToDictionary(operation => operation, operation => $"{resource.Code}.{operation.ToString().ToUpperInvariant()}");
        var existing = await _context.Permissions.Where(item => codes.Values.Contains(item.Code)).ToListAsync();
        foreach (var (operation, supported) in operations)
        {
            var code = codes[operation];
            var permission = existing.FirstOrDefault(item => item.Code == code);
            if (permission != null && permission.Kind != SecurityPermissionKind.Resource) continue;
            if (permission == null)
            {
                permission = new Permission { Code = code, Module = "Resource", Feature = resource.Code, Action = operation.ToString(), Kind = SecurityPermissionKind.Resource, ResourceCode = resource.Code, Operation = operation };
                _context.Permissions.Add(permission);
            }
            permission.Description = $"{operation} {resource.Name}";
            permission.IsActive = resource.IsActive && supported;
        }
    }

    private async Task SyncChildPermissionsAsync(string resourceCode, bool resourceActive, bool supportsFieldSecurity)
    {
        var actions = await _context.SecurityActionDefinitions.Where(item => item.ResourceCode == resourceCode).ToArrayAsync();
        foreach (var action in actions) await SyncActionPermissionAsync(action, resourceActive);
        var members = await _context.SecurityMemberDefinitions.Where(item => item.ResourceCode == resourceCode).ToArrayAsync();
        foreach (var member in members) await SyncMemberPermissionsAsync(member, resourceActive && supportsFieldSecurity);
    }

    private async Task SyncActionPermissionAsync(SecurityActionDefinition action, bool resourceActive)
    {
        var permission = await _context.Permissions.FirstOrDefaultAsync(item => item.Code == action.Code);
        if (permission == null)
        {
            permission = new Permission { Code = action.Code, Module = "Action", Feature = action.ResourceCode, Action = action.Code.Split('.').Last(), Kind = SecurityPermissionKind.Action, ResourceCode = action.ResourceCode, Operation = SecurityOperation.Execute, ActionCode = action.Code };
            _context.Permissions.Add(permission);
        }
        if (permission.Kind != SecurityPermissionKind.Action || (!string.IsNullOrWhiteSpace(permission.ActionCode) && permission.ActionCode != action.Code)) return;
        permission.Module = "Action"; permission.Feature = action.ResourceCode; permission.ResourceCode = action.ResourceCode;
        permission.Operation = SecurityOperation.Execute; permission.ActionCode = action.Code; permission.Description = action.Description ?? action.Name;
        permission.IsActive = resourceActive && action.IsActive;
    }

    private async Task SyncMemberPermissionsAsync(SecurityMemberDefinition member, bool resourceActive)
    {
        foreach (var operation in new[] { SecurityOperation.Read, SecurityOperation.Update })
        {
            var code = $"{member.ResourceCode}.{member.MemberCode}.{operation.ToString().ToUpperInvariant()}";
            var permission = await _context.Permissions.FirstOrDefaultAsync(item => item.Code == code);
            if (permission == null)
            {
                permission = new Permission { Code = code, Module = "Member", Feature = member.ResourceCode, Action = operation.ToString(), Kind = SecurityPermissionKind.Member, ResourceCode = member.ResourceCode, MemberCode = member.MemberCode, Operation = operation };
                _context.Permissions.Add(permission);
            }
            permission.Description = $"{operation} {member.DisplayName}";
            permission.IsActive = resourceActive && member.IsActive && (operation == SecurityOperation.Read || !member.IsSystemManaged);
        }
    }

    private static readonly HashSet<string> RegistryResourceTypes = ["ENTITY", "REPORT", "WORKFLOW", "SERVICE"];

    private async Task<bool> CanAdministerRoleAsync(ApplicationUser actor, ApplicationRole role)
    {
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        if (access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase)) return true;
        if (!role.MunicipalityId.HasValue) return false;
        return access.RoleAssignments.Any(item => item.MunicipalityId == role.MunicipalityId);
    }

    private async Task<bool> CanAdministerUserAsync(ApplicationUser actor, string userId)
    {
        var actorAccess = await _accessControl.GetEffectiveAccessAsync(actor);
        if (actorAccess.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase)) return true;
        var actorMunicipalities = actorAccess.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).ToHashSet();
        var targetMunicipalities = await _context.SecurityUserRoleAssignments.AsNoTracking().Where(item => item.UserId == userId && item.IsActive && item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArrayAsync();
        return targetMunicipalities.Length > 0 && targetMunicipalities.All(actorMunicipalities.Contains);
    }
}

public sealed record SecurityResourceDto(Guid PublicId, string Code, string Name, string Type, string? Description, bool CanCreate, bool CanRead, bool CanUpdate, bool CanDelete, bool CanExport, bool CanImport, bool SupportsMembers, bool SupportsCriteria, bool IsActive, string RowVersion);
public sealed record SecurityRoleDto(string Id, Guid PublicId, string RoleCode, string Name, string? Description, long? MunicipalityId, bool IsSystemRole, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SecurityUserDto(string Id, string FullName, string Email);
public sealed record UserRoleAssignmentDto(long Id, string RoleId, string RoleName, long? MunicipalityId, int? DepartmentId, int? UnitId, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion, Guid? DepartmentPublicId, string? DepartmentName, Guid? UnitPublicId, string? UnitName);
public sealed record UserRoleSecurityConfigurationDto(string UserId, string UserName, UserRoleAssignmentDto[] Assignments);
public sealed record ExpectedUserRoleAssignment(long AssignmentId, string RowVersion);
public sealed record UpdateUserRoleAssignment(string RoleId, long? MunicipalityId, int? DepartmentId, int? UnitId, DateTime? EffectiveFrom, DateTime? EffectiveTo, Guid? DepartmentPublicId = null, Guid? UnitPublicId = null);
public sealed record UpdateUserRoleSecurityRequest(ExpectedUserRoleAssignment[] ExpectedAssignments, UpdateUserRoleAssignment[] Assignments);
public sealed record SecurityActionDto(Guid PublicId, string Code, string Name, string ResourceCode, string? Description, bool IsActive, string RowVersion);
public sealed record SecurityNavigationDto(Guid PublicId, string Code, Guid? ParentPublicId, string Name, string? Route, string? IconKey, int DisplayOrder, string? RequiredPermissionCode, bool IsActive, string RowVersion, string? ParentCode = null, string? ParentName = null);
public sealed record SecurityMemberDto(Guid PublicId, string ResourceCode, string MemberCode, string DisplayName, bool IsSensitive, bool IsSystemManaged, bool IsActive, string RowVersion);
public sealed record SecurityPermissionDefinitionDto(string Code, string? Description, string Kind, string? ResourceCode, string? Operation, string? MemberCode, string? NavigationCode, string? ActionCode);
public sealed record RoleSecurityPermissionDto(string PermissionCode, string Kind, string? ResourceCode, string? MemberCode, string? NavigationCode, string? ActionCode, string State, string? ScopeType, string RowVersion);
public sealed record RoleSecurityConfigurationDto(string RoleId, Guid PublicId, string Name, string RoleRowVersion, RoleSecurityPermissionDto[] Permissions);
public sealed record UpdateRoleSecurityRequest(string RoleRowVersion, UpdateRoleSecurityItem[] Permissions);
public sealed record UpdateRoleSecurityItem(string PermissionCode, string State, string? ScopeType);
public sealed record EffectiveSecurityDto(string UserId, string[] Roles, string[] Permissions, string[] Scopes, string[] Assignments);
public sealed record CreateSecurityRoleRequest(string RoleCode, string Name, string? Description, DateTime? EffectiveFrom, DateTime? EffectiveTo);
public sealed record UpdateSecurityRoleRequest(string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record CreateSecurityNavigationRequest(string Code, Guid? ParentPublicId, string Name, string? Route, string? IconKey, int DisplayOrder, string? RequiredPermissionCode, string Reason);
public sealed record UpdateSecurityNavigationRequest(Guid? ParentPublicId, string Name, string? Route, string? IconKey, int DisplayOrder, string? RequiredPermissionCode, bool IsActive, string RowVersion, string Reason);
public sealed record CreateSecurityResourceRequest(string Code, string Name, string ResourceType, string? Description, bool SupportsCreate, bool SupportsRead, bool SupportsUpdate, bool SupportsDelete, bool SupportsExport, bool SupportsImport, bool SupportsFieldSecurity, bool SupportsRecordCriteria, string Reason);
public sealed record UpdateSecurityResourceRequest(string Name, string ResourceType, string? Description, bool SupportsCreate, bool SupportsRead, bool SupportsUpdate, bool SupportsDelete, bool SupportsExport, bool SupportsImport, bool SupportsFieldSecurity, bool SupportsRecordCriteria, bool IsActive, string RowVersion, string Reason);
public sealed record CreateSecurityActionRequest(string Code, string Name, string ResourceCode, string? Description, string Reason);
public sealed record UpdateSecurityActionRequest(string Name, string? Description, bool IsActive, string RowVersion, string Reason);
public sealed record UpdateSecurityMemberRequest(string DisplayName, bool IsSensitive, bool IsActive, string RowVersion, string Reason);

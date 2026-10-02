using System.Security.Claims;
using System.Text.Json;
using FTCERP.Host.API.Responses;
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
    public async Task<ActionResult<ApiResponse<SecurityRoleDto[]>>> GetRoles()
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityRoleDto[]>(false, null, "User not found"));
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        var system = access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
        var municipalities = access.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArray();
        var query = _context.Roles.AsNoTracking().Where(item => item.IsActive);
        if (!system) query = query.Where(item => item.MunicipalityId.HasValue && municipalities.Contains(item.MunicipalityId.Value));
        var roles = await query.OrderBy(item => item.Name).ToArrayAsync();
        return Ok(new ApiResponse<SecurityRoleDto[]>(true, roles.Select(ToRoleDto).ToArray()));
    }

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
    public async Task<ActionResult<ApiResponse<SecurityUserDto[]>>> GetUsers()
    {
        var actor = await GetCurrentUserAsync();
        if (actor == null) return Unauthorized(new ApiResponse<SecurityUserDto[]>(false, null, "User not found"));
        var access = await _accessControl.GetEffectiveAccessAsync(actor);
        var system = access.EffectivePermissions.Contains("SECURITY.SYSTEM_SCOPE", StringComparer.OrdinalIgnoreCase);
        var municipalities = access.RoleAssignments.Where(item => item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArray();
        var query = _context.Users.AsNoTracking().Where(item => item.IsActive);
        if (!system)
            query = query.Where(user => _context.SecurityUserRoleAssignments.Any(link => link.UserId == user.Id && link.IsActive && link.MunicipalityId.HasValue && municipalities.Contains(link.MunicipalityId.Value)));
        var users = await query.OrderBy(item => item.FirstName).ThenBy(item => item.LastName).ToArrayAsync();
        return Ok(new ApiResponse<SecurityUserDto[]>(true, users.Select(item => new SecurityUserDto(item.Id, item.FullName, item.Email ?? item.UserName ?? item.Id)).ToArray()));
    }

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
        var assignments = assignmentEntities.Select(item => new UserRoleAssignmentDto(item.Id, item.RoleId, item.Role.Name ?? item.Role.RoleCode, item.MunicipalityId, item.DepartmentId, item.UnitId, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArray();
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
        foreach (var item in request.Assignments)
        {
            var role = roles.Single(value => value.Id == item.RoleId);
            var municipalityId = item.MunicipalityId ?? role.MunicipalityId;
            if (!municipalityId.HasValue) return BadRequest(new ApiResponse<bool>(false, false, "Every assignment requires a municipality"));
            if (_tenantContext.MunicipalityId is > 0 && municipalityId != _tenantContext.MunicipalityId)
                return BadRequest(new ApiResponse<bool>(false, false, "Assignment municipality must match the selected context"));
            if (item.DepartmentId.HasValue && !await _context.Departments.AnyAsync(value => value.Id == item.DepartmentId && value.IsActive))
                return BadRequest(new ApiResponse<bool>(false, false, "One or more assignment departments are invalid or inactive"));
            if (item.UnitId.HasValue)
            {
                var unitDepartmentId = await _context.Units.Where(value => value.Id == item.UnitId && value.IsActive).Select(value => (int?)value.DepartmentId).SingleOrDefaultAsync();
                if (!unitDepartmentId.HasValue) return BadRequest(new ApiResponse<bool>(false, false, "One or more assignment units are invalid or inactive"));
                if (item.DepartmentId.HasValue && item.DepartmentId != unitDepartmentId)
                    return BadRequest(new ApiResponse<bool>(false, false, "Assignment unit must belong to its selected department"));
            }
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
            _context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment { UserId = userId, RoleId = role.Id, MunicipalityId = municipalityId, DepartmentId = item.DepartmentId, UnitId = item.UnitId, EffectiveFrom = item.EffectiveFrom ?? now, EffectiveTo = item.EffectiveTo, AssignedBy = actor.Id, AssignedAt = now, IsActive = true });
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
    public async Task<ActionResult<ApiResponse<SecurityResourceDto[]>>> GetResources()
    {
        var items = await _context.SecurityResources.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name).ToArrayAsync();
        return Ok(new ApiResponse<SecurityResourceDto[]>(true, items.Select(item => new SecurityResourceDto(item.Code, item.Name, item.ResourceType, item.SupportsCreate, item.SupportsRead, item.SupportsUpdate, item.SupportsDelete, item.SupportsExport, item.SupportsImport, item.SupportsFieldSecurity, item.SupportsRecordCriteria, Convert.ToBase64String(item.RowVersion))).ToArray()));
    }

    [HttpGet("actions")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<SecurityActionDto[]>>> GetActions()
    {
        var items = await _context.SecurityActionDefinitions.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.ResourceCode).ThenBy(item => item.Name).ToArrayAsync();
        return Ok(new ApiResponse<SecurityActionDto[]>(true, items.Select(item => new SecurityActionDto(item.Code, item.Name, item.ResourceCode, item.Description, Convert.ToBase64String(item.RowVersion))).ToArray()));
    }

    [HttpGet("navigation/registry")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<SecurityNavigationDto[]>>> GetNavigationRegistry()
    {
        var items = await _context.SecurityNavigationItems.AsNoTracking().Include(item => item.Parent).OrderBy(item => item.ParentId).ThenBy(item => item.DisplayOrder).ToArrayAsync();
        return Ok(new ApiResponse<SecurityNavigationDto[]>(true, items.Select(ToNavigationDto).ToArray()));
    }

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
    public async Task<ActionResult<ApiResponse<SecurityMemberDto[]>>> GetMembers()
    {
        var items = await _context.SecurityMemberDefinitions.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.ResourceCode).ThenBy(item => item.DisplayName)
            .Select(item => new SecurityMemberDto(item.ResourceCode, item.MemberCode, item.DisplayName, item.IsSensitive, item.IsSystemManaged))
            .ToArrayAsync();
        return Ok(new ApiResponse<SecurityMemberDto[]>(true, items));
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "Permission:SECURITY.VIEW")]
    public async Task<ActionResult<ApiResponse<SecurityPermissionDefinitionDto[]>>> GetPermissionDefinitions()
    {
        var definitions = await _context.Permissions.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.Kind).ThenBy(item => item.ResourceCode).ThenBy(item => item.Code)
            .ToArrayAsync();
        var items = definitions.Select(item => new SecurityPermissionDefinitionDto(item.Code, item.Description, item.Kind.ToString(), item.ResourceCode, item.Operation?.ToString(), item.MemberCode, item.NavigationCode, item.ActionCode)).ToArray();
        return Ok(new ApiResponse<SecurityPermissionDefinitionDto[]>(true, items));
    }

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
    private static SecurityNavigationDto ToNavigationDto(SecurityNavigationItem item) => new(item.PublicId, item.Code, item.Parent?.PublicId, item.Name, item.Route, item.IconKey, item.DisplayOrder, item.RequiredPermissionCode, item.IsActive, Convert.ToBase64String(item.RowVersion));

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

public sealed record SecurityResourceDto(string Code, string Name, string Type, bool CanCreate, bool CanRead, bool CanUpdate, bool CanDelete, bool CanExport, bool CanImport, bool SupportsMembers, bool SupportsCriteria, string RowVersion);
public sealed record SecurityRoleDto(string Id, Guid PublicId, string RoleCode, string Name, string? Description, long? MunicipalityId, bool IsSystemRole, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SecurityUserDto(string Id, string FullName, string Email);
public sealed record UserRoleAssignmentDto(long Id, string RoleId, string RoleName, long? MunicipalityId, int? DepartmentId, int? UnitId, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record UserRoleSecurityConfigurationDto(string UserId, string UserName, UserRoleAssignmentDto[] Assignments);
public sealed record ExpectedUserRoleAssignment(long AssignmentId, string RowVersion);
public sealed record UpdateUserRoleAssignment(string RoleId, long? MunicipalityId, int? DepartmentId, int? UnitId, DateTime? EffectiveFrom, DateTime? EffectiveTo);
public sealed record UpdateUserRoleSecurityRequest(ExpectedUserRoleAssignment[] ExpectedAssignments, UpdateUserRoleAssignment[] Assignments);
public sealed record SecurityActionDto(string Code, string Name, string ResourceCode, string? Description, string RowVersion);
public sealed record SecurityNavigationDto(Guid PublicId, string Code, Guid? ParentPublicId, string Name, string? Route, string? IconKey, int DisplayOrder, string? RequiredPermissionCode, bool IsActive, string RowVersion);
public sealed record SecurityMemberDto(string ResourceCode, string MemberCode, string DisplayName, bool IsSensitive, bool IsSystemManaged);
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

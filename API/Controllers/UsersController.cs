using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAccessControlService _accessControl;
    private readonly ITenantContext _tenantContext;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IAccessControlService accessControl, ITenantContext tenantContext)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _accessControl = accessControl;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public ActionResult<ApiResponse<UserDetailResponse[]>> GetUsers() =>
        StatusCode(StatusCodes.Status410Gone, Fail<UserDetailResponse[]>(
            "This fixed-limit route is retired. Use /api/users/page."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDetailResponse>>>> GetUsersPage([FromQuery] PagedQueryRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<PagedResponse<UserDetailResponse>>("User not found"));
        if (!await IsAllowedAsync(actor, "USER.READ")) return Forbid();
        var canReadEmail = await IsAllowedAsync(actor, "USER.Email.READ");
        var canReadPhone = await IsAllowedAsync(actor, "USER.PhoneNumber.READ");
        if (!UserSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<UserDetailResponse>>("SortBy must be createdAt, name, email, or status."));
        if (!canReadEmail && request.NormalizedSortBy == "email")
            return Forbid();

        var query = TenantUsers().AsNoTracking();
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = canReadEmail
                ? query.Where(item => item.FirstName.Contains(term) || item.LastName.Contains(term)
                    || (item.Email != null && item.Email.Contains(term)))
                : query.Where(item => item.FirstName.Contains(term) || item.LastName.Contains(term));
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
        var details = await ToUserDetailsAsync(users, canReadEmail, canReadPhone);
        return Ok(new ApiResponse<PagedResponse<UserDetailResponse>>(true,
            PagedResponse<UserDetailResponse>.Create(details, request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetUser(string id)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<UserDetailResponse>("User not found"));
        if (!await IsAllowedAsync(actor, "USER.READ")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<UserDetailResponse>(false, null, "User not found"));

        var roles = await _userManager.GetRolesAsync(user);
        var roleEntities = await _context.Roles.AsNoTracking().Where(r => roles.Contains(r.Name!)).ToListAsync();

        var roleResponses = roleEntities.Select(r => new RoleResponse(r.Id, r.Name!, r.Description, r.IsSystemRole, r.IsActive)).ToArray();
        var userResponse = ToResponse(user, await IsAllowedAsync(actor, "USER.Email.READ"), await IsAllowedAsync(actor, "USER.PhoneNumber.READ"));

        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, roleResponses)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<UserDetailResponse>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<UserDetailResponse>("Select a municipality context before creating a user"));
        if (!await IsAllowedAsync(actor, "USER.CREATE") || !await IsAllowedAsync(actor, "USER.Email.UPDATE")) return Forbid();
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !await IsAllowedAsync(actor, "USER.PhoneNumber.UPDATE")) return Forbid();
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null) return Conflict(new ApiResponse<UserDetailResponse>(false, null, "Email already exists"));

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            MunicipalityId = _tenantContext.MunicipalityId,
            IsActive = true,
            MustChangePassword = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<UserDetailResponse>(false, null, "Failed to create user", result.Errors.Select(e => e.Description).ToArray()));
        }

        QueueAudit(user, "Create", null, new { user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        var userResponse = ToResponse(user, await IsAllowedAsync(actor, "USER.Email.READ"), await IsAllowedAsync(actor, "USER.PhoneNumber.READ"));
        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, Array.Empty<RoleResponse>())));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<UserDetailResponse>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<UserDetailResponse>("Select a municipality context before updating a user"));
        if (!await IsAllowedAsync(actor, "USER.UPDATE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<UserDetailResponse>(false, null, "User not found"));
        if (!string.Equals(user.PhoneNumber?.Trim(), request.PhoneNumber?.Trim(), StringComparison.Ordinal)
            && !await IsAllowedAsync(actor, "USER.PhoneNumber.UPDATE")) return Forbid();

        var before = new { user.FirstName, user.LastName, user.PhoneNumber, user.IsActive };
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<UserDetailResponse>(false, null, "Failed to update user", result.Errors.Select(e => e.Description).ToArray()));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var roleEntities = await _context.Roles.AsNoTracking().Where(r => roles.Contains(r.Name!)).ToListAsync();
        var roleResponses = roleEntities.Select(r => new RoleResponse(r.Id, r.Name!, r.Description, r.IsSystemRole, r.IsActive)).ToArray();

        QueueAudit(user, "Update", before, new { user.FirstName, user.LastName, user.PhoneNumber, user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        var userResponse = ToResponse(user, await IsAllowedAsync(actor, "USER.Email.READ"), await IsAllowedAsync(actor, "USER.PhoneNumber.READ"));
        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, roleResponses)));
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> Activate(string id)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before enabling a user"));
        if (!await IsAllowedAsync(actor, "USER.ENABLE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var before = new { user.IsActive };
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        QueueAudit(user, "Enable", before, new { user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(string id)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before disabling a user"));
        if (!await IsAllowedAsync(actor, "USER.DISABLE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var before = new { user.IsActive };
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        QueueAudit(user, "Disable", before, new { user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(string id)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before deleting a user"));
        if (!await IsAllowedAsync(actor, "USER.DELETE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var before = new { user.IsActive };
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to disable user", result.Errors.Select(e => e.Description).ToArray()));
        }

        QueueAudit(user, "Delete", before, new { user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id}/roles")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserRoles(string id, [FromBody] AssignUserRolesRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before assigning roles"));
        if (!await IsAllowedAsync(actor, "ROLE.ASSIGN")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var roles = await _context.Roles.Where(r => request.RoleIds.Contains(r.Id) && r.MunicipalityId == _tenantContext.MunicipalityId).ToListAsync();
        if (roles.Count != request.RoleIds.Distinct(StringComparer.OrdinalIgnoreCase).Count()) return Forbid();
        var roleNames = roles.Select(r => r.Name!).ToArray();

        var existingRoleNames = await _userManager.GetRolesAsync(user);
        var tenantRoleNames = await _context.Roles.AsNoTracking()
            .Where(item => item.MunicipalityId == _tenantContext.MunicipalityId && item.Name != null)
            .Select(item => item.Name!)
            .ToArrayAsync();
        var remove = existingRoleNames.Intersect(tenantRoleNames, StringComparer.OrdinalIgnoreCase).Except(roleNames, StringComparer.OrdinalIgnoreCase).ToArray();
        var add = roleNames.Except(existingRoleNames, StringComparer.OrdinalIgnoreCase).ToArray();

        if (remove.Length > 0) await _userManager.RemoveFromRolesAsync(user, remove);
        if (add.Length > 0) await _userManager.AddToRolesAsync(user, add);

        var now = DateTime.UtcNow;
        var actorId = actor.Id;
        var currentAssignments = await _context.SecurityUserRoleAssignments
            .Where(item => item.UserId == id && item.MunicipalityId == _tenantContext.MunicipalityId && item.IsActive && !item.RevokedAt.HasValue)
            .ToListAsync();
        foreach (var assignment in currentAssignments.Where(item => !request.RoleIds.Contains(item.RoleId)))
        {
            assignment.IsActive = false;
            assignment.EffectiveTo = now;
            assignment.RevokedAt = now;
            assignment.RevokedBy = actorId;
        }
        var activeRoleIds = currentAssignments.Where(item => item.IsActive).Select(item => item.RoleId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles.Where(item => !activeRoleIds.Contains(item.Id)))
        {
            _context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
            {
                UserId = id,
                RoleId = role.Id,
                MunicipalityId = role.MunicipalityId,
                EffectiveFrom = now,
                AssignedAt = now,
                AssignedBy = actorId,
                IsActive = true
            });
        }
        QueueAudit(user, "AssignRoles", new { RoleIds = currentAssignments.Where(item => item.IsActive).Select(item => item.RoleId).ToArray() }, new { RoleIds = request.RoleIds }, actorId);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveUserRole(string id, string roleId)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before removing roles"));
        if (!await IsAllowedAsync(actor, "ROLE.ASSIGN")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound(new ApiResponse<bool>(false, false, "Role not found"));
        if (!(_tenantContext.IsSystem && !_tenantContext.MunicipalityId.HasValue) && role.MunicipalityId != _tenantContext.MunicipalityId) return Forbid();

        await _userManager.RemoveFromRoleAsync(user, role.Name!);
        var now = DateTime.UtcNow;
        var actorId = actor.Id;
        var assignments = await _context.SecurityUserRoleAssignments.Where(item => item.UserId == id && item.RoleId == roleId
            && item.MunicipalityId == _tenantContext.MunicipalityId && item.IsActive).ToListAsync();
        foreach (var assignment in assignments)
        {
            assignment.IsActive = false;
            assignment.EffectiveTo = now;
            assignment.RevokedAt = now;
            assignment.RevokedBy = actorId;
        }
        QueueAudit(user, "RemoveRole", new { RoleId = roleId }, new { Removed = true }, actorId);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/scopes")]
    public ActionResult<ApiResponse<UserScopeResponse[]>> GetUserScopes(string id) =>
        StatusCode(StatusCodes.Status410Gone, Fail<UserScopeResponse[]>(
            $"This unbounded route is retired. Use /api/users/{id}/scopes/page."));

    [HttpGet("{id}/scopes/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserScopeResponse>>>> GetUserScopesPage(string id, [FromQuery] PagedQueryRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<PagedResponse<UserScopeResponse>>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.VIEW_EFFECTIVE")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(Fail<PagedResponse<UserScopeResponse>>("User not found"));
        if (!UserScopeSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<UserScopeResponse>>("SortBy must be createdAt, effectiveFrom, type, department, or unit."));

        var query = _context.UserScopes
            .AsNoTracking()
            .Where(scope => scope.UserId == id)
            .Include(scope => scope.Department)
            .Include(scope => scope.Unit)
            .AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            var hasType = Enum.TryParse<ScopeType>(term, true, out var scopeType);
            query = query.Where(scope => (hasType && scope.ScopeType == scopeType)
                || (scope.Department != null && scope.Department.Name.Contains(term))
                || (scope.Unit != null && scope.Unit.Name.Contains(term))
                || (scope.TargetId != null && scope.TargetId.Contains(term))
                || (scope.KpiId != null && scope.KpiId.Contains(term))
                || (scope.ProjectId != null && scope.ProjectId.Contains(term))
                || (scope.TaskId != null && scope.TaskId.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("effectivefrom", false) => query.OrderBy(scope => scope.EffectiveFrom).ThenBy(scope => scope.Id),
            ("effectivefrom", true) => query.OrderByDescending(scope => scope.EffectiveFrom).ThenBy(scope => scope.Id),
            ("type", false) => query.OrderBy(scope => scope.ScopeType).ThenBy(scope => scope.Id),
            ("type", true) => query.OrderByDescending(scope => scope.ScopeType).ThenBy(scope => scope.Id),
            ("department", false) => query.OrderBy(scope => scope.Department != null ? scope.Department.Name : null).ThenBy(scope => scope.Id),
            ("department", true) => query.OrderByDescending(scope => scope.Department != null ? scope.Department.Name : null).ThenBy(scope => scope.Id),
            ("unit", false) => query.OrderBy(scope => scope.Unit != null ? scope.Unit.Name : null).ThenBy(scope => scope.Id),
            ("unit", true) => query.OrderByDescending(scope => scope.Unit != null ? scope.Unit.Name : null).ThenBy(scope => scope.Id),
            (_, false) => query.OrderBy(scope => scope.CreatedAt).ThenBy(scope => scope.Id),
            _ => query.OrderByDescending(scope => scope.CreatedAt).ThenBy(scope => scope.Id)
        };
        var scopes = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(scope => new UserScopeResponse(
                scope.Id,
                scope.ScopeType.ToString(),
                scope.DepartmentId,
                scope.Department != null ? scope.Department.Name : null,
                scope.UnitId,
                scope.Unit != null ? scope.Unit.Name : null,
                scope.TargetId,
                scope.KpiId,
                scope.ProjectId,
                scope.TaskId))
            .ToArrayAsync();

        return Ok(new ApiResponse<PagedResponse<UserScopeResponse>>(true,
            PagedResponse<UserScopeResponse>.Create(scopes, request.Page, request.PageSize, totalCount)));
    }

    [HttpPut("{id}/scopes")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserScopes(string id, [FromBody] UpdateUserScopesRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before assigning scopes"));
        if (!await IsAllowedAsync(actor, "SECURITY.ASSIGN_ROLES")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var invalidScope = request.Scopes.FirstOrDefault(scope => !Enum.TryParse<ScopeType>(scope.ScopeType, true, out _));
        if (invalidScope != null)
        {
            return BadRequest(new ApiResponse<bool>(false, false, $"Invalid scope type '{invalidScope.ScopeType}'"));
        }
        var departmentIds = request.Scopes.Where(item => item.DepartmentId.HasValue).Select(item => item.DepartmentId!.Value).Distinct().ToArray();
        var unitIds = request.Scopes.Where(item => item.UnitId.HasValue).Select(item => item.UnitId!.Value).Distinct().ToArray();
        if (await _context.Departments.CountAsync(item => departmentIds.Contains(item.Id)) != departmentIds.Length
            || await _context.Units.CountAsync(item => unitIds.Contains(item.Id)) != unitIds.Length)
            return Forbid();

        var now = DateTime.UtcNow;
        var existing = await _context.UserScopes.Where(scope => scope.UserId == id && scope.IsActive).ToListAsync();
        foreach (var item in existing)
        {
            item.IsActive = false;
            item.EffectiveTo = now;
        }

        foreach (var scope in request.Scopes)
        {
            _context.UserScopes.Add(new UserScope
            {
                UserId = id,
                MunicipalityId = _tenantContext.MunicipalityId,
                ScopeType = Enum.Parse<ScopeType>(scope.ScopeType, true),
                DepartmentId = scope.DepartmentId,
                UnitId = scope.UnitId,
                TargetId = scope.TargetId,
                KpiId = scope.KpiId,
                ProjectId = scope.ProjectId,
                TaskId = scope.TaskId,
                EffectiveFrom = now,
                IsActive = true
            });
        }

        QueueAudit(user, "UpdateScopes", new { ActiveScopeCount = existing.Count }, new { Scopes = request.Scopes }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/assignments")]
    public ActionResult<ApiResponse<UserAssignmentResponse[]>> GetUserAssignments(string id) =>
        StatusCode(StatusCodes.Status410Gone, Fail<UserAssignmentResponse[]>(
            $"This unbounded route is retired. Use /api/users/{id}/assignments/page."));

    [HttpGet("{id}/assignments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserAssignmentResponse>>>> GetUserAssignmentsPage(string id, [FromQuery] PagedQueryRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<PagedResponse<UserAssignmentResponse>>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.VIEW_EFFECTIVE")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(Fail<PagedResponse<UserAssignmentResponse>>("User not found"));
        if (!UserAssignmentSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<UserAssignmentResponse>>("SortBy must be createdAt, validFrom, validTo, type, or status."));

        var query = _context.UserAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == id);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            var hasType = Enum.TryParse<AssignmentType>(term, true, out var assignmentType);
            query = query.Where(assignment => (hasType && assignment.AssignmentType == assignmentType)
                || (assignment.DelegatorUserId != null && assignment.DelegatorUserId.Contains(term))
                || (assignment.TargetId != null && assignment.TargetId.Contains(term))
                || (assignment.KpiId != null && assignment.KpiId.Contains(term))
                || (assignment.ProjectId != null && assignment.ProjectId.Contains(term))
                || (assignment.TaskId != null && assignment.TaskId.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("validfrom", false) => query.OrderBy(item => item.ValidFromUtc).ThenBy(item => item.Id),
            ("validfrom", true) => query.OrderByDescending(item => item.ValidFromUtc).ThenBy(item => item.Id),
            ("validto", false) => query.OrderBy(item => item.ValidToUtc).ThenBy(item => item.Id),
            ("validto", true) => query.OrderByDescending(item => item.ValidToUtc).ThenBy(item => item.Id),
            ("type", false) => query.OrderBy(item => item.AssignmentType).ThenBy(item => item.Id),
            ("type", true) => query.OrderByDescending(item => item.AssignmentType).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
        };
        var assignments = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(assignment => new UserAssignmentResponse(
                assignment.Id,
                assignment.AssignmentType.ToString(),
                assignment.DelegatorUserId,
                assignment.IsActive,
                assignment.ValidFromUtc,
                assignment.ValidToUtc,
                assignment.TargetId,
                assignment.KpiId,
                assignment.ProjectId,
                assignment.TaskId))
            .ToArrayAsync();

        return Ok(new ApiResponse<PagedResponse<UserAssignmentResponse>>(true,
            PagedResponse<UserAssignmentResponse>.Create(assignments, request.Page, request.PageSize, totalCount)));
    }

    [HttpPut("{id}/assignments")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserAssignments(string id, [FromBody] UpdateUserAssignmentsRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before assigning responsibilities"));
        if (!await IsAllowedAsync(actor, "SECURITY.ASSIGN_ROLES")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var invalidAssignment = request.Assignments.FirstOrDefault(assignment => !Enum.TryParse<AssignmentType>(assignment.AssignmentType, true, out _));
        if (invalidAssignment != null)
        {
            return BadRequest(new ApiResponse<bool>(false, false, $"Invalid assignment type '{invalidAssignment.AssignmentType}'"));
        }

        var existing = await _context.UserAssignments.Where(assignment => assignment.UserId == id).ToListAsync();
        _context.UserAssignments.RemoveRange(existing);

        foreach (var assignment in request.Assignments)
        {
            _context.UserAssignments.Add(new UserAssignment
            {
                UserId = id,
                AssignmentType = Enum.Parse<AssignmentType>(assignment.AssignmentType, true),
                DelegatorUserId = assignment.DelegatorUserId,
                IsActive = assignment.IsActive,
                ValidFromUtc = assignment.ValidFromUtc,
                ValidToUtc = assignment.ValidToUtc,
                TargetId = assignment.TargetId,
                KpiId = assignment.KpiId,
                ProjectId = assignment.ProjectId,
                TaskId = assignment.TaskId
            });
        }

        QueueAudit(user, "UpdateAssignments", new { AssignmentCount = existing.Count }, new { Assignments = request.Assignments }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<UserPermissionsResponse>>> GetUserPermissions(string id)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<UserPermissionsResponse>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.VIEW_EFFECTIVE")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (user == null) return NotFound(new ApiResponse<UserPermissionsResponse>(false, null, "User not found"));

        var roleNames = await _userManager.GetRolesAsync(user);
        var roleIds = await _context.Roles.Where(r => roleNames.Contains(r.Name!)).Select(r => r.Id).ToListAsync();

        var fromRoles = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsAllowed)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        var overrides = await _context.UserPermissionOverrides
            .Where(o => o.UserId == user.Id)
            .Select(o => new { o.PermissionId, o.Permission.Code, o.IsAllowed, o.Reason })
            .ToListAsync();

        var effective = new HashSet<string>(fromRoles, StringComparer.OrdinalIgnoreCase);
        foreach (var o in overrides)
        {
            if (!o.IsAllowed) effective.Remove(o.Code);
        }

        var overrideResponses = overrides
            .Select(o => new UserPermissionOverrideResponse(o.PermissionId, o.Code, o.IsAllowed, o.Reason))
            .ToArray();

        return Ok(new ApiResponse<UserPermissionsResponse>(true, new UserPermissionsResponse(fromRoles.ToArray(), overrideResponses, effective.OrderBy(x => x).ToArray())));
    }

    [HttpPut("{id}/permission-overrides")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserPermissionOverrides(string id, [FromBody] UpdateUserPermissionOverridesRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.MANAGE_PERMISSIONS")) return Forbid();
        if (!await TenantUsers().AsNoTracking().AnyAsync(item => item.Id == id)) return NotFound(Fail<bool>("User not found"));
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Direct user permission grants are disabled. Use tenant-scoped role assignments in /api/v1/security."));
    }

    private IQueryable<ApplicationUser> TenantUsers()
    {
        var query = _context.Users.AsQueryable();
        if (_tenantContext.MunicipalityId is > 0) return query.Where(item => item.MunicipalityId == _tenantContext.MunicipalityId);
        return _tenantContext.IsSystem ? query : query.Where(_ => false);
    }

    private static readonly HashSet<string> UserSortFields = ["createdat", "name", "email", "status"];
    private static readonly HashSet<string> UserScopeSortFields = ["createdat", "effectivefrom", "type", "department", "unit"];
    private static readonly HashSet<string> UserAssignmentSortFields = ["createdat", "validfrom", "validto", "type", "status"];

    private async Task<UserDetailResponse[]> ToUserDetailsAsync(ApplicationUser[] users, bool canReadEmail, bool canReadPhone)
    {
        if (users.Length == 0) return [];
        var userIds = users.Select(item => item.Id).ToArray();
        var links = await _context.UserRoles.AsNoTracking().Where(item => userIds.Contains(item.UserId)).ToArrayAsync();
        var roleIds = links.Select(item => item.RoleId).Distinct().ToArray();
        var rolesById = await _context.Roles.AsNoTracking().Where(item => roleIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        var roleIdsByUser = links.GroupBy(item => item.UserId).ToDictionary(group => group.Key, group => group.Select(item => item.RoleId).ToArray());
        return users.Select(user =>
        {
            var roles = roleIdsByUser.GetValueOrDefault(user.Id, [])
                .Select(roleId => rolesById.GetValueOrDefault(roleId))
                .Where(role => role != null)
                .OrderBy(role => role!.Name)
                .Select(role => new RoleResponse(role!.Id, role.Name!, role.Description, role.IsSystemRole, role.IsActive))
                .ToArray();
            return new UserDetailResponse(ToResponse(user, canReadEmail, canReadPhone), roles);
        }).ToArray();
    }

    private async Task<ApplicationUser?> GetCurrentActorAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? _tenantContext.UserId;
        return string.IsNullOrWhiteSpace(id) ? null : await _userManager.FindByIdAsync(id);
    }

    private async Task<bool> IsAllowedAsync(ApplicationUser actor, string permissionCode)
    {
        var decision = await _accessControl.CheckPermissionAsync(actor, permissionCode,
            new AccessScopeContext(MunicipalityId: _tenantContext.MunicipalityId));
        return decision.Allowed;
    }

    private static UserResponse ToResponse(ApplicationUser user, bool includeEmail, bool includePhone) =>
        new(user.Id, includeEmail ? user.UserName ?? user.Email ?? user.Id : user.Id, user.FirstName, user.LastName,
            user.FullName, includeEmail ? user.Email : null, includePhone ? user.PhoneNumber : null, user.Department,
            user.Position, user.IsActive, user.MustChangePassword, user.LastLoginAt) { PublicId = user.PublicId };

    private void QueueAudit(ApplicationUser user, string action, object? oldValue, object? newValue, string actorId) =>
        _context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = user.MunicipalityId,
            EntityName = nameof(ApplicationUser),
            EntityId = user.PublicId.ToString(),
            Action = action,
            OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue == null ? null : JsonSerializer.Serialize(newValue),
            ChangedBy = actorId,
            ChangedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CorrelationId = HttpContext.TraceIdentifier,
            Reason = "User administration change"
        });

    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
}

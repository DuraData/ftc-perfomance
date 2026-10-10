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

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetUser(Guid publicId)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<UserDetailResponse>("User not found"));
        if (!await IsAllowedAsync(actor, "USER.READ")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<UserDetailResponse>(false, null, "User not found"));

        var roles = await _userManager.GetRolesAsync(user);
        var roleEntities = await _context.Roles.AsNoTracking().Where(r => roles.Contains(r.Name!)).ToListAsync();

        var roleResponses = roleEntities.Select(r => new RoleResponse(r.PublicId, r.Name!, r.Description, r.IsSystemRole, r.IsActive)).ToArray();
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

    [HttpPut("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> UpdateUser(Guid publicId, [FromBody] UpdateUserRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<UserDetailResponse>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<UserDetailResponse>("Select a municipality context before updating a user"));
        if (!await IsAllowedAsync(actor, "USER.UPDATE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
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
        var roleResponses = roleEntities.Select(r => new RoleResponse(r.PublicId, r.Name!, r.Description, r.IsSystemRole, r.IsActive)).ToArray();

        QueueAudit(user, "Update", before, new { user.FirstName, user.LastName, user.PhoneNumber, user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        var userResponse = ToResponse(user, await IsAllowedAsync(actor, "USER.Email.READ"), await IsAllowedAsync(actor, "USER.PhoneNumber.READ"));
        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, roleResponses)));
    }

    [HttpPatch("{publicId:guid}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> Activate(Guid publicId)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before enabling a user"));
        if (!await IsAllowedAsync(actor, "USER.ENABLE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var before = new { user.IsActive };
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        QueueAudit(user, "Enable", before, new { user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPatch("{publicId:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(Guid publicId)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before disabling a user"));
        if (!await IsAllowedAsync(actor, "USER.DISABLE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var before = new { user.IsActive };
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        QueueAudit(user, "Disable", before, new { user.IsActive }, actor.Id);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpDelete("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid publicId)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before deleting a user"));
        if (!await IsAllowedAsync(actor, "USER.DELETE")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
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

    [HttpPost("{publicId:guid}/roles")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserRoles(Guid publicId, [FromBody] AssignUserRolesRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before assigning roles"));
        if (!await IsAllowedAsync(actor, "ROLE.ASSIGN")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var requestedRolePublicIds = request.RolePublicIds.Distinct().ToArray();
        var roles = await _context.Roles.Where(r => requestedRolePublicIds.Contains(r.PublicId) && r.MunicipalityId == _tenantContext.MunicipalityId).ToListAsync();
        if (roles.Count != requestedRolePublicIds.Length) return Forbid();
        var requestedRoleIds = roles.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
            .Where(item => item.UserId == user.Id && item.MunicipalityId == _tenantContext.MunicipalityId && item.IsActive && !item.RevokedAt.HasValue)
            .ToListAsync();
        var currentRoleIds = currentAssignments.Select(item => item.RoleId).Distinct().ToArray();
        var currentRolePublicIds = await _context.Roles.AsNoTracking()
            .Where(item => currentRoleIds.Contains(item.Id))
            .Select(item => item.PublicId)
            .ToArrayAsync();
        foreach (var assignment in currentAssignments.Where(item => !requestedRoleIds.Contains(item.RoleId)))
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
                UserId = user.Id,
                RoleId = role.Id,
                MunicipalityId = role.MunicipalityId,
                EffectiveFrom = now,
                AssignedAt = now,
                AssignedBy = actorId,
                IsActive = true
            });
        }
        QueueAudit(user, "AssignRoles",
            new { RolePublicIds = currentRolePublicIds },
            new { RolePublicIds = requestedRolePublicIds }, actorId);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpDelete("{publicId:guid}/roles/{rolePublicId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveUserRole(Guid publicId, Guid rolePublicId)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before removing roles"));
        if (!await IsAllowedAsync(actor, "ROLE.ASSIGN")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var role = await _context.Roles.SingleOrDefaultAsync(item => item.PublicId == rolePublicId);
        if (role == null) return NotFound(new ApiResponse<bool>(false, false, "Role not found"));
        if (!(_tenantContext.IsSystem && !_tenantContext.MunicipalityId.HasValue) && role.MunicipalityId != _tenantContext.MunicipalityId) return Forbid();

        await _userManager.RemoveFromRoleAsync(user, role.Name!);
        var now = DateTime.UtcNow;
        var actorId = actor.Id;
        var assignments = await _context.SecurityUserRoleAssignments.Where(item => item.UserId == user.Id && item.RoleId == role.Id
            && item.MunicipalityId == _tenantContext.MunicipalityId && item.IsActive).ToListAsync();
        foreach (var assignment in assignments)
        {
            assignment.IsActive = false;
            assignment.EffectiveTo = now;
            assignment.RevokedAt = now;
            assignment.RevokedBy = actorId;
        }
        QueueAudit(user, "RemoveRole", new { RolePublicId = role.PublicId }, new { Removed = true }, actorId);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{publicId:guid}/scopes")]
    public ActionResult<ApiResponse<UserScopeResponse[]>> GetUserScopes(Guid publicId) =>
        StatusCode(StatusCodes.Status410Gone, Fail<UserScopeResponse[]>(
            $"This unbounded route is retired. Use /api/users/{publicId}/scopes/page."));

    [HttpGet("{publicId:guid}/scopes/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserScopeResponse>>>> GetUserScopesPage(Guid publicId, [FromQuery] PagedQueryRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<PagedResponse<UserScopeResponse>>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.VIEW_EFFECTIVE")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(Fail<PagedResponse<UserScopeResponse>>("User not found"));
        if (!UserScopeSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<UserScopeResponse>>("SortBy must be createdAt, effectiveFrom, type, department, or unit."));

        var query = _context.UserScopes
            .AsNoTracking()
            .Where(scope => scope.UserId == user.Id)
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
                scope.PublicId,
                scope.ScopeType.ToString(),
                scope.Department != null ? scope.Department.PublicId : null,
                scope.Department != null ? scope.Department.Name : null,
                scope.Unit != null ? scope.Unit.PublicId : null,
                scope.Unit != null ? scope.Unit.Name : null,
                scope.TargetId,
                scope.KpiId,
                scope.ProjectId,
                scope.TaskId,
                scope.EffectiveFrom,
                scope.EffectiveTo,
                scope.IsActive,
                Convert.ToBase64String(scope.RowVersion)))
            .ToArrayAsync();

        return Ok(new ApiResponse<PagedResponse<UserScopeResponse>>(true,
            PagedResponse<UserScopeResponse>.Create(scopes, request.Page, request.PageSize, totalCount)));
    }

    [HttpPut("{publicId:guid}/scopes")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserScopes(Guid publicId, [FromBody] UpdateUserScopesRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before assigning scopes"));
        if (!await IsAllowedAsync(actor, "SECURITY.ASSIGN_ROLES")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));
        if (!TryDecodeRowVersion(request.RowVersion, out var expectedVersion))
            return BadRequest(Fail<bool>("RowVersion must be a valid non-empty base64 concurrency token."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 5 or > 500)
            return BadRequest(Fail<bool>("Reason must contain between 5 and 500 characters."));
        if (request.Scopes == null) return BadRequest(Fail<bool>("Scopes are required."));

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
        var existing = await _context.UserScopes.Where(scope => scope.UserId == user.Id && scope.IsActive).ToListAsync();
        foreach (var item in existing)
        {
            item.IsActive = false;
            item.EffectiveTo = now;
        }

        foreach (var scope in request.Scopes)
        {
            _context.UserScopes.Add(new UserScope
            {
                UserId = user.Id,
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

        _context.Entry(user).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        user.UpdatedAt = now;
        user.UpdatedBy = actor.Id;
        QueueAudit(user, "UpdateScopes", new { ActiveScopeCount = existing.Count }, new { Scopes = request.Scopes }, actor.Id, reason);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(Fail<bool>("The user scopes changed since they were loaded. Refresh and try again."));
        }
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{publicId:guid}/assignments")]
    public ActionResult<ApiResponse<UserAssignmentResponse[]>> GetUserAssignments(Guid publicId) =>
        StatusCode(StatusCodes.Status410Gone, Fail<UserAssignmentResponse[]>(
            $"This unbounded route is retired. Use /api/users/{publicId}/assignments/page."));

    [HttpGet("{publicId:guid}/assignments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserAssignmentResponse>>>> GetUserAssignmentsPage(Guid publicId, [FromQuery] PagedQueryRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<PagedResponse<UserAssignmentResponse>>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.VIEW_EFFECTIVE")) return Forbid();
        var user = await TenantUsers().AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(Fail<PagedResponse<UserAssignmentResponse>>("User not found"));
        if (!UserAssignmentSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<UserAssignmentResponse>>("SortBy must be createdAt, validFrom, validTo, type, or status."));

        var query = _context.UserAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == user.Id);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            var hasType = Enum.TryParse<AssignmentType>(term, true, out var assignmentType);
            var hasDelegator = Guid.TryParse(term, out var delegatorPublicId);
            query = query.Where(assignment => (hasType && assignment.AssignmentType == assignmentType)
                || (hasDelegator && assignment.DelegatorUserId != null
                    && _context.Users.Any(delegator => delegator.Id == assignment.DelegatorUserId && delegator.PublicId == delegatorPublicId))
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
                assignment.PublicId,
                assignment.AssignmentType.ToString(),
                assignment.DelegatorUserId == null ? null : _context.Users
                    .Where(delegator => delegator.Id == assignment.DelegatorUserId)
                    .Select(delegator => (Guid?)delegator.PublicId)
                    .SingleOrDefault(),
                assignment.IsActive,
                assignment.ValidFromUtc,
                assignment.ValidToUtc,
                assignment.TargetId,
                assignment.KpiId,
                assignment.ProjectId,
                assignment.TaskId,
                Convert.ToBase64String(assignment.RowVersion)))
            .ToArrayAsync();

        return Ok(new ApiResponse<PagedResponse<UserAssignmentResponse>>(true,
            PagedResponse<UserAssignmentResponse>.Create(assignments, request.Page, request.PageSize, totalCount)));
    }

    [HttpPut("{publicId:guid}/assignments")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserAssignments(Guid publicId, [FromBody] UpdateUserAssignmentsRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (_tenantContext.MunicipalityId is not > 0) return Conflict(Fail<bool>("Select a municipality context before assigning responsibilities"));
        if (!await IsAllowedAsync(actor, "SECURITY.ASSIGN_ROLES")) return Forbid();
        var user = await TenantUsers().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));
        if (!TryDecodeRowVersion(request.RowVersion, out var expectedVersion))
            return BadRequest(Fail<bool>("RowVersion must be a valid non-empty base64 concurrency token."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 5 or > 500)
            return BadRequest(Fail<bool>("Reason must contain between 5 and 500 characters."));
        if (request.Assignments == null)
            return BadRequest(Fail<bool>("Assignments are required."));

        var invalidAssignment = request.Assignments.FirstOrDefault(assignment => !Enum.TryParse<AssignmentType>(assignment.AssignmentType, true, out _));
        if (invalidAssignment != null)
        {
            return BadRequest(new ApiResponse<bool>(false, false, $"Invalid assignment type '{invalidAssignment.AssignmentType}'"));
        }

        foreach (var assignment in request.Assignments)
        {
            var type = Enum.Parse<AssignmentType>(assignment.AssignmentType, true);
            if (assignment.ValidFromUtc.HasValue && assignment.ValidToUtc.HasValue && assignment.ValidToUtc <= assignment.ValidFromUtc)
                return BadRequest(Fail<bool>("Assignment ValidToUtc must be later than ValidFromUtc."));
            var hasTarget = !string.IsNullOrWhiteSpace(assignment.TargetId) || !string.IsNullOrWhiteSpace(assignment.KpiId);
            var validSelector = type switch
            {
                AssignmentType.AdditionalApproverAssignment or AssignmentType.AdditionalVerifierAssignment or AssignmentType.AdditionalSubmitterAssignment => hasTarget,
                AssignmentType.ProjectAssignee => !string.IsNullOrWhiteSpace(assignment.ProjectId),
                AssignmentType.TaskAssignee => !string.IsNullOrWhiteSpace(assignment.TaskId),
                AssignmentType.DelegatedAssignment => assignment.DelegatorUserPublicId.HasValue
                    && (hasTarget || !string.IsNullOrWhiteSpace(assignment.ProjectId) || !string.IsNullOrWhiteSpace(assignment.TaskId)),
                _ => false
            };
            if (!validSelector)
                return BadRequest(Fail<bool>($"Assignment type '{type}' requires its corresponding record selector."));
            if (type == AssignmentType.DelegatedAssignment && assignment.DelegatorUserPublicId == user.PublicId)
                return BadRequest(Fail<bool>("A user cannot delegate an assignment to themselves."));
        }

        var delegatorPublicIds = request.Assignments
            .Where(item => item.DelegatorUserPublicId.HasValue)
            .Select(item => item.DelegatorUserPublicId!.Value)
            .Distinct()
            .ToArray();
        var delegatorIds = new Dictionary<Guid, string>();
        if (delegatorPublicIds.Length > 0)
        {
            delegatorIds = await TenantUsers().AsNoTracking()
                .Where(item => delegatorPublicIds.Contains(item.PublicId) && item.IsActive)
                .ToDictionaryAsync(item => item.PublicId, item => item.Id);
            if (delegatorIds.Count != delegatorPublicIds.Length)
                return BadRequest(Fail<bool>("Every delegator must be an active user in the selected municipality."));
        }

        var duplicate = request.Assignments
            .GroupBy(AssignmentKey, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null) return BadRequest(Fail<bool>("Duplicate operational assignments are not permitted."));

        var existing = await _context.UserAssignments.Where(assignment => assignment.UserId == user.Id).ToListAsync();
        var previouslyActiveCount = existing.Count(item => item.IsActive);
        var now = DateTime.UtcNow;
        foreach (var assignment in existing.Where(item => item.IsActive))
        {
            assignment.IsActive = false;
            if ((!assignment.ValidFromUtc.HasValue || assignment.ValidFromUtc <= now)
                && (!assignment.ValidToUtc.HasValue || assignment.ValidToUtc > now))
                assignment.ValidToUtc = now;
        }

        foreach (var assignment in request.Assignments)
        {
            _context.UserAssignments.Add(new UserAssignment
            {
                UserId = user.Id,
                AssignmentType = Enum.Parse<AssignmentType>(assignment.AssignmentType, true),
                DelegatorUserId = assignment.DelegatorUserPublicId.HasValue
                    ? delegatorIds[assignment.DelegatorUserPublicId.Value]
                    : null,
                IsActive = assignment.IsActive,
                ValidFromUtc = assignment.ValidFromUtc,
                ValidToUtc = assignment.ValidToUtc,
                TargetId = NullIfWhiteSpace(assignment.TargetId),
                KpiId = NullIfWhiteSpace(assignment.KpiId),
                ProjectId = NullIfWhiteSpace(assignment.ProjectId),
                TaskId = NullIfWhiteSpace(assignment.TaskId)
            });
        }

        _context.Entry(user).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        user.UpdatedAt = now;
        user.UpdatedBy = actor.Id;
        QueueAudit(user, "UpdateAssignments", new { ActiveAssignmentCount = previouslyActiveCount }, new { Assignments = request.Assignments }, actor.Id, reason);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(Fail<bool>("The user assignments changed since they were loaded. Refresh and try again."));
        }
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{publicId:guid}/permissions")]
    public ActionResult<ApiResponse<UserPermissionsResponse>> GetUserPermissions(Guid publicId)
    {
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<UserPermissionsResponse>(false, null,
            "This legacy user-permission projection is retired. Use /api/v1/security/effective-permissions/{userPublicId}; effective access is derived from tenant-scoped role assignments."));
    }

    [HttpPut("{publicId:guid}/permission-overrides")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserPermissionOverrides(Guid publicId, [FromBody] UpdateUserPermissionOverridesRequest request)
    {
        var actor = await GetCurrentActorAsync();
        if (actor == null) return Unauthorized(Fail<bool>("User not found"));
        if (!await IsAllowedAsync(actor, "SECURITY.MANAGE_PERMISSIONS")) return Forbid();
        if (!await TenantUsers().AsNoTracking().AnyAsync(item => item.PublicId == publicId)) return NotFound(Fail<bool>("User not found"));
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
                .Select(role => new RoleResponse(role!.PublicId, role.Name!, role.Description, role.IsSystemRole, role.IsActive))
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
        new(user.PublicId, includeEmail ? user.UserName ?? user.Email ?? user.PublicId.ToString() : user.PublicId.ToString(), user.FirstName, user.LastName,
            user.FullName, includeEmail ? user.Email : null, includePhone ? user.PhoneNumber : null, user.Department,
            user.Position, user.IsActive, user.MustChangePassword, user.LastLoginAt)
        { RowVersion = Convert.ToBase64String(user.RowVersion) };

    private void QueueAudit(ApplicationUser user, string action, object? oldValue, object? newValue, string actorId, string reason = "User administration change") =>
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
            Reason = reason
        });

    private static string AssignmentKey(UserAssignmentItemRequest assignment) => string.Join('|',
        assignment.AssignmentType.Trim(), assignment.DelegatorUserPublicId, assignment.TargetId?.Trim(), assignment.KpiId?.Trim(),
        assignment.ProjectId?.Trim(), assignment.TaskId?.Trim(), assignment.ValidFromUtc?.ToUniversalTime().Ticks,
        assignment.ValidToUtc?.ToUniversalTime().Ticks, assignment.IsActive);

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException) { return false; }
    }

    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
}

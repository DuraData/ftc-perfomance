using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "Permission:Admin.Users.Manage")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDetailResponse[]>>> GetUsers()
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
        var rolesById = await _context.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id);
        var userRoleLinks = await _context.UserRoles.AsNoTracking().ToListAsync();

        var result = users.Select(u =>
        {
            var userRoles = userRoleLinks.Where(ur => ur.UserId == u.Id)
                .Select(ur => rolesById.TryGetValue(ur.RoleId, out var role) ? role : null)
                .Where(r => r != null)
                .Select(r => new RoleResponse(r!.Id, r.Name!, r.Description, r.IsSystemRole, r.IsActive))
                .ToArray();

            var userResponse = new UserResponse(u.Id, u.UserName ?? u.Email!, u.FirstName, u.LastName, u.FullName, u.Email!, u.PhoneNumber, u.Department, u.Position, u.IsActive, u.MustChangePassword, u.LastLoginAt);
            return new UserDetailResponse(userResponse, userRoles);
        }).ToArray();

        return Ok(new ApiResponse<UserDetailResponse[]>(true, result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<UserDetailResponse>(false, null, "User not found"));

        var roles = await _userManager.GetRolesAsync(user);
        var roleEntities = await _context.Roles.AsNoTracking().Where(r => roles.Contains(r.Name!)).ToListAsync();

        var roleResponses = roleEntities.Select(r => new RoleResponse(r.Id, r.Name!, r.Description, r.IsSystemRole, r.IsActive)).ToArray();
        var userResponse = new UserResponse(user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!, user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword, user.LastLoginAt);

        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, roleResponses)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null) return Conflict(new ApiResponse<UserDetailResponse>(false, null, "Email already exists"));

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
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

        var userResponse = new UserResponse(user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!, user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword, user.LastLoginAt);
        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, Array.Empty<RoleResponse>())));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<UserDetailResponse>(false, null, "User not found"));

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

        var userResponse = new UserResponse(user.Id, user.UserName ?? user.Email!, user.FirstName, user.LastName, user.FullName, user.Email!, user.PhoneNumber, user.Department, user.Position, user.IsActive, user.MustChangePassword, user.LastLoginAt);
        return Ok(new ApiResponse<UserDetailResponse>(true, new UserDetailResponse(userResponse, roleResponses)));
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> Activate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to delete user", result.Errors.Select(e => e.Description).ToArray()));
        }

        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id}/roles")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserRoles(string id, [FromBody] AssignUserRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var roles = await _context.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync();
        var roleNames = roles.Select(r => r.Name!).ToArray();

        var existingRoleNames = await _userManager.GetRolesAsync(user);
        var remove = existingRoleNames.Except(roleNames).ToArray();
        var add = roleNames.Except(existingRoleNames).ToArray();

        if (remove.Length > 0) await _userManager.RemoveFromRolesAsync(user, remove);
        if (add.Length > 0) await _userManager.AddToRolesAsync(user, add);

        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveUserRole(string id, string roleId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound(new ApiResponse<bool>(false, false, "Role not found"));

        await _userManager.RemoveFromRoleAsync(user, role.Name!);
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/scopes")]
    public async Task<ActionResult<ApiResponse<UserScopeResponse[]>>> GetUserScopes(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<UserScopeResponse[]>(false, null, "User not found"));

        var scopes = await _context.UserScopes
            .AsNoTracking()
            .Where(scope => scope.UserId == id)
            .Include(scope => scope.Department)
            .Include(scope => scope.Unit)
            .OrderBy(scope => scope.ScopeType)
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

        return Ok(new ApiResponse<UserScopeResponse[]>(true, scopes));
    }

    [HttpPut("{id}/scopes")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserScopes(string id, [FromBody] UpdateUserScopesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var invalidScope = request.Scopes.FirstOrDefault(scope => !Enum.TryParse<ScopeType>(scope.ScopeType, true, out _));
        if (invalidScope != null)
        {
            return BadRequest(new ApiResponse<bool>(false, false, $"Invalid scope type '{invalidScope.ScopeType}'"));
        }

        var existing = await _context.UserScopes.Where(scope => scope.UserId == id).ToListAsync();
        _context.UserScopes.RemoveRange(existing);

        foreach (var scope in request.Scopes)
        {
            _context.UserScopes.Add(new UserScope
            {
                UserId = id,
                ScopeType = Enum.Parse<ScopeType>(scope.ScopeType, true),
                DepartmentId = scope.DepartmentId,
                UnitId = scope.UnitId,
                TargetId = scope.TargetId,
                KpiId = scope.KpiId,
                ProjectId = scope.ProjectId,
                TaskId = scope.TaskId
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/assignments")]
    public async Task<ActionResult<ApiResponse<UserAssignmentResponse[]>>> GetUserAssignments(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<UserAssignmentResponse[]>(false, null, "User not found"));

        var assignments = await _context.UserAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == id)
            .OrderBy(assignment => assignment.AssignmentType)
            .Select(assignment => new UserAssignmentResponse(
                assignment.Id,
                assignment.AssignmentType.ToString(),
                assignment.TargetId,
                assignment.KpiId,
                assignment.ProjectId,
                assignment.TaskId))
            .ToArrayAsync();

        return Ok(new ApiResponse<UserAssignmentResponse[]>(true, assignments));
    }

    [HttpPut("{id}/assignments")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserAssignments(string id, [FromBody] UpdateUserAssignmentsRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
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
                TargetId = assignment.TargetId,
                KpiId = assignment.KpiId,
                ProjectId = assignment.ProjectId,
                TaskId = assignment.TaskId
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<UserPermissionsResponse>>> GetUserPermissions(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
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
            if (o.IsAllowed) effective.Add(o.Code);
            else effective.Remove(o.Code);
        }

        var overrideResponses = overrides
            .Select(o => new UserPermissionOverrideResponse(o.PermissionId, o.Code, o.IsAllowed, o.Reason))
            .ToArray();

        return Ok(new ApiResponse<UserPermissionsResponse>(true, new UserPermissionsResponse(fromRoles.ToArray(), overrideResponses, effective.OrderBy(x => x).ToArray())));
    }

    [HttpPut("{id}/permission-overrides")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserPermissionOverrides(string id, [FromBody] UpdateUserPermissionOverridesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new ApiResponse<bool>(false, false, "User not found"));

        var existing = await _context.UserPermissionOverrides.Where(o => o.UserId == id).ToListAsync();
        _context.UserPermissionOverrides.RemoveRange(existing);

        foreach (var o in request.Overrides)
        {
            _context.UserPermissionOverrides.Add(new UserPermissionOverride
            {
                UserId = id,
                PermissionId = o.PermissionId,
                IsAllowed = o.IsAllowed,
                Reason = o.Reason
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }
}

using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "Permission:SECURITY.MANAGE_ROLES")]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RolesController(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RoleResponse[]>>> GetRoles()
    {
        var roles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        var result = roles.Select(r => new RoleResponse(r.Id, r.Name!, r.Description, r.IsSystemRole, r.IsActive)).ToArray();
        return Ok(new ApiResponse<RoleResponse[]>(true, result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound(new ApiResponse<RoleResponse>(false, null, "Role not found"));
        return Ok(new ApiResponse<RoleResponse>(true, new RoleResponse(role.Id, role.Name!, role.Description, role.IsSystemRole, role.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var existing = await _roleManager.FindByNameAsync(request.Name);
        if (existing != null) return Conflict(new ApiResponse<RoleResponse>(false, null, "Role already exists"));

        var role = new ApplicationRole
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpperInvariant(),
            Description = request.Description,
            RoleCode = string.Concat(request.Name.Trim().ToUpperInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '_')),
            IsSystemRole = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<RoleResponse>(false, null, "Failed to create role", result.Errors.Select(e => e.Description).ToArray()));
        }

        return Ok(new ApiResponse<RoleResponse>(true, new RoleResponse(role.Id, role.Name!, role.Description, role.IsSystemRole, role.IsActive)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound(new ApiResponse<RoleResponse>(false, null, "Role not found"));

        if (role.IsSystemRole)
        {
            return BadRequest(new ApiResponse<RoleResponse>(false, null, "System roles cannot be edited"));
        }

        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpperInvariant();
        role.Description = request.Description;
        role.UpdatedAt = DateTime.UtcNow;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<RoleResponse>(false, null, "Failed to update role", result.Errors.Select(e => e.Description).ToArray()));
        }

        return Ok(new ApiResponse<RoleResponse>(true, new RoleResponse(role.Id, role.Name!, role.Description, role.IsSystemRole, role.IsActive)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound(new ApiResponse<bool>(false, false, "Role not found"));

        if (role.IsSystemRole)
        {
            return BadRequest(new ApiResponse<bool>(false, false, "System roles cannot be deleted"));
        }

        role.IsActive = false;
        role.EffectiveTo = DateTime.UtcNow;
        role.UpdatedAt = DateTime.UtcNow;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to disable role", result.Errors.Select(e => e.Description).ToArray()));
        }

        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<RolePermissionResponse[]>>> GetRolePermissions(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound(new ApiResponse<RolePermissionResponse[]>(false, null, "Role not found"));

        var permissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .OrderBy(rp => rp.Permission.Code)
            .Select(rp => new RolePermissionResponse(rp.PermissionId, rp.Permission.Code, rp.IsAllowed))
            .ToArrayAsync();

        return Ok(new ApiResponse<RolePermissionResponse[]>(true, permissions));
    }

    [HttpPut("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<bool>>> SetRolePermissions(string id, [FromBody] UpdateRolePermissionsRequest request)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Use PUT /api/v1/security/roles/{roleId}/permissions with RowVersion. The legacy mutation contract is disabled."));
    }
}

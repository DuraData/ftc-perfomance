using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "Permission:Admin.Permissions.Manage")]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermissionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PermissionResponse[]>>> GetPermissions()
    {
        var permissions = await _context.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Feature).ThenBy(p => p.Action)
            .Select(p => new PermissionResponse(p.Id, p.Module, p.Feature, p.Action, p.Code, p.Description, p.IsActive))
            .ToArrayAsync();

        return Ok(new ApiResponse<PermissionResponse[]>(true, permissions));
    }

    [HttpGet("grouped")]
    public async Task<ActionResult<ApiResponse<PermissionGroupResponse[]>>> GetGrouped()
    {
        var permissions = await _context.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Feature).ThenBy(p => p.Action)
            .ToListAsync();

        var grouped = permissions
            .GroupBy(p => new { p.Module, p.Feature })
            .Select(g => new PermissionGroupResponse(
                g.Key.Module,
                g.Key.Feature,
                g.Select(p => new PermissionResponse(p.Id, p.Module, p.Feature, p.Action, p.Code, p.Description, p.IsActive)).ToArray()
            ))
            .OrderBy(g => g.Module).ThenBy(g => g.Feature)
            .ToArray();

        return Ok(new ApiResponse<PermissionGroupResponse[]>(true, grouped));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var exists = await _context.Permissions.AnyAsync(p => p.Code == request.Code);
        if (exists) return Conflict(new ApiResponse<PermissionResponse>(false, null, "Permission code already exists"));

        var entity = new Permission
        {
            Module = request.Module,
            Feature = request.Feature,
            Action = request.Action,
            Code = request.Code,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Permissions.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<PermissionResponse>(true, new PermissionResponse(entity.Id, entity.Module, entity.Feature, entity.Action, entity.Code, entity.Description, entity.IsActive)));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> UpdatePermission(int id, [FromBody] UpdatePermissionRequest request)
    {
        var entity = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        if (entity == null) return NotFound(new ApiResponse<PermissionResponse>(false, null, "Permission not found"));

        var codeConflict = await _context.Permissions.AnyAsync(p => p.Id != id && p.Code == request.Code);
        if (codeConflict) return Conflict(new ApiResponse<PermissionResponse>(false, null, "Permission code already exists"));

        entity.Module = request.Module;
        entity.Feature = request.Feature;
        entity.Action = request.Action;
        entity.Code = request.Code;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<PermissionResponse>(true, new PermissionResponse(entity.Id, entity.Module, entity.Feature, entity.Action, entity.Code, entity.Description, entity.IsActive)));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePermission(int id)
    {
        var entity = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        if (entity == null) return NotFound(new ApiResponse<bool>(false, false, "Permission not found"));

        var roleLinks = await _context.RolePermissions.Where(rp => rp.PermissionId == id).ToListAsync();
        var userLinks = await _context.UserPermissionOverrides.Where(up => up.PermissionId == id).ToListAsync();
        _context.RolePermissions.RemoveRange(roleLinks);
        _context.UserPermissionOverrides.RemoveRange(userLinks);
        _context.Permissions.Remove(entity);

        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }
}


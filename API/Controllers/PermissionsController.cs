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
[Authorize(Policy = "Permission:SECURITY.VIEW")]
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
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<PermissionResponse>(false, null,
            "Permission definitions are controlled by the versioned security registry. The legacy mutation contract is disabled."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PermissionResponse>>> UpdatePermission(int id, [FromBody] UpdatePermissionRequest request)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<PermissionResponse>(false, null,
            "Permission definitions are controlled by the versioned security registry. The legacy mutation contract is disabled."));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePermission(int id)
    {
        await Task.CompletedTask;
        return StatusCode(
            StatusCodes.Status410Gone,
            new ApiResponse<bool>(false, false, "Permission definitions are controlled by the versioned security registry. The legacy mutation contract is disabled."));
    }
}

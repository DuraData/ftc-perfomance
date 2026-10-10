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
    public ActionResult<ApiResponse<PermissionResponse[]>> GetPermissions() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<PermissionResponse[]>(false, null,
            "This legacy permission catalogue is retired. Use /api/v1/security/permissions/page; permission code is the authoritative public identity."));

    [HttpGet("page")]
    public ActionResult<ApiResponse<PagedResponse<PermissionResponse>>> GetPermissionsPage([FromQuery] PagedQueryRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<PagedResponse<PermissionResponse>>(false, null,
            "This legacy permission catalogue is retired. Use /api/v1/security/permissions/page; permission code is the authoritative public identity."));

    [HttpGet("grouped")]
    public ActionResult<ApiResponse<PermissionGroupResponse[]>> GetGrouped() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<PermissionGroupResponse[]>(false, null,
            "This unbounded legacy route is retired. Use /api/v1/security/permissions/page with a kind filter."));

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

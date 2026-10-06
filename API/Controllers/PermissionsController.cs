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
            "This unbounded route is retired. Use /api/permissions/page."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PermissionResponse>>>> GetPermissionsPage([FromQuery] PagedQueryRequest request)
    {
        if (!PermissionSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<PermissionResponse>>(false, null,
                "SortBy must be createdAt, module, feature, action, code, or status."));
        var query = _context.Permissions.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Module.Contains(term) || item.Feature.Contains(term)
                || item.Action.Contains(term) || item.Code.Contains(term)
                || (item.Description != null && item.Description.Contains(term)));
        }
        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("module", false) => query.OrderBy(item => item.Module).ThenBy(item => item.Feature).ThenBy(item => item.Action).ThenBy(item => item.Id),
            ("module", true) => query.OrderByDescending(item => item.Module).ThenByDescending(item => item.Feature).ThenByDescending(item => item.Action).ThenBy(item => item.Id),
            ("feature", false) => query.OrderBy(item => item.Feature).ThenBy(item => item.Action).ThenBy(item => item.Id),
            ("feature", true) => query.OrderByDescending(item => item.Feature).ThenByDescending(item => item.Action).ThenBy(item => item.Id),
            ("action", false) => query.OrderBy(item => item.Action).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("action", true) => query.OrderByDescending(item => item.Action).ThenByDescending(item => item.Code).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
            _ => query.OrderBy(item => item.Code).ThenBy(item => item.Id)
        };
        var permissions = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(item => new PermissionResponse(item.Id, item.Module, item.Feature, item.Action, item.Code, item.Description, item.IsActive))
            .ToArrayAsync();

        return Ok(new ApiResponse<PagedResponse<PermissionResponse>>(true,
            PagedResponse<PermissionResponse>.Create(permissions, request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> PermissionSortFields = ["createdat", "module", "feature", "action", "code", "status"];

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

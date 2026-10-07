using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "Permission:SECURITY.VIEW")]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public RolesController(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public ActionResult<ApiResponse<RoleResponse[]>> GetRoles() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse[]>(false, null,
            "This unbounded legacy route is retired. Use /api/v1/security/roles/page."));

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole(Guid publicId)
    {
        var role = await TenantRoles().AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (role == null) return NotFound(new ApiResponse<RoleResponse>(false, null, "Role not found"));
        return Ok(new ApiResponse<RoleResponse>(true, new RoleResponse(role.PublicId, role.Name!, role.Description, role.IsSystemRole, role.IsActive)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse>(false, null,
            "Use POST /api/v1/security/roles. The legacy role mutation contract is disabled."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse>(false, null,
            "Use PUT /api/v1/security/roles/{roleId} with RowVersion. The legacy role mutation contract is disabled."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(string id)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false,
            "Use PUT /api/v1/security/roles/{roleId} with RowVersion to deactivate a role. The legacy mutation contract is disabled."));
    }

    [HttpGet("{id}/permissions")]
    public ActionResult<ApiResponse<RolePermissionResponse[]>> GetRolePermissions(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<RolePermissionResponse[]>(false, null,
            "This legacy role-permission collection is retired. Use /api/v1/security/roles/{roleId}/permissions with /api/v1/security/permissions/page."));

    [HttpPut("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<bool>>> SetRolePermissions(string id, [FromBody] UpdateRolePermissionsRequest request)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Use PUT /api/v1/security/roles/{roleId}/permissions with RowVersion. The legacy mutation contract is disabled."));
    }

    private IQueryable<ApplicationRole> TenantRoles()
    {
        var query = _context.Roles.AsQueryable();
        if (_tenantContext.MunicipalityId is > 0) return query.Where(item => item.MunicipalityId == _tenantContext.MunicipalityId);
        return _tenantContext.IsSystem ? query : query.Where(_ => false);
    }
}

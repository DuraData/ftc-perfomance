using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "Permission:SECURITY.VIEW")]
public class RolesController : ControllerBase
{
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<RoleResponse[]>> GetRoles() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse[]>(false, null,
            "This unbounded legacy route is retired. Use /api/v1/security/roles/page."));

    [HttpGet("{publicId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<RoleResponse>> GetRole(Guid publicId) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse>(false, null,
            "This unversioned role-detail route is retired. Use GET /api/v1/security/roles/{rolePublicId}."));

    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole()
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse>(false, null,
            "Use POST /api/v1/security/roles. The legacy role mutation contract is disabled."));
    }

    [HttpPut("{id}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole(string id)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<RoleResponse>(false, null,
            "Use PUT /api/v1/security/roles/{roleId} with RowVersion. The legacy role mutation contract is disabled."));
    }

    [HttpDelete("{id}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(string id)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false,
            "Use PUT /api/v1/security/roles/{roleId} with RowVersion to deactivate a role. The legacy mutation contract is disabled."));
    }

    [HttpGet("{id}/permissions")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<object>> GetRolePermissions(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null,
            "This legacy role-permission collection is retired. Use /api/v1/security/roles/{roleId}/permissions with /api/v1/security/permissions/page."));

    [HttpPut("{id}/permissions")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<bool>>> SetRolePermissions(string id)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Use PUT /api/v1/security/roles/{roleId}/permissions with RowVersion. The legacy mutation contract is disabled."));
    }
}

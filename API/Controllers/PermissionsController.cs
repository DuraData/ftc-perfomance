using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "Permission:SECURITY.VIEW")]
[ApiExplorerSettings(IgnoreApi = true)]
public class PermissionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> GetPermissions() =>
        Gone(
            "This legacy permission catalogue is retired. Use /api/v1/security/permissions/page; permission code is the authoritative public identity.");

    [HttpGet("page")]
    public ActionResult<ApiResponse<object>> GetPermissionsPage() =>
        Gone(
            "This legacy permission catalogue is retired. Use /api/v1/security/permissions/page; permission code is the authoritative public identity.");

    [HttpGet("grouped")]
    public ActionResult<ApiResponse<object>> GetGrouped() =>
        Gone(
            "This unbounded legacy route is retired. Use /api/v1/security/permissions/page with a kind filter.");

    [HttpPost]
    public ActionResult<ApiResponse<object>> CreatePermission() =>
        Gone("Permission definitions are controlled by the versioned security registry. The legacy mutation contract is disabled.");

    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<object>> UpdatePermission(int id) =>
        Gone("Permission definitions are controlled by the versioned security registry. The legacy mutation contract is disabled.");

    [HttpDelete("{id:int}")]
    public ActionResult<ApiResponse<object>> DeletePermission(int id) =>
        Gone("Permission definitions are controlled by the versioned security registry. The legacy mutation contract is disabled.");

    private ObjectResult Gone(string message) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null, message));
}

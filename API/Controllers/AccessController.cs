using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/access")]
[Authorize]
public class AccessController : ControllerBase
{
    [HttpGet("my-permissions")]
    [Authorize(Policy = "Permission:Access.MyPermissions.View")]
    public ActionResult<ApiResponse<string[]>> GetMyPermissions()
    {
        var permissions = User.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        return Ok(new ApiResponse<string[]>(true, permissions));
    }

    [HttpPost("check")]
    public ActionResult<ApiResponse<bool>> Check([FromBody] CheckPermissionRequest request)
    {
        var has = User.IsInRole("Super Admin") || User.Claims.Any(c =>
            c.Type == "Permission" &&
            string.Equals(c.Value, request.PermissionCode, StringComparison.OrdinalIgnoreCase));

        return Ok(new ApiResponse<bool>(true, has));
    }
}


using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

/// <summary>
/// Explicit tombstones for the pre-v1 user-administration prefix. Keeping the
/// compatibility routes separate prevents future UsersController actions from
/// silently becoming available without an API version.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class LegacyUsersController : ControllerBase
{
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    [Route("users")]
    [Route("users/{**path}")]
    public ActionResult<ApiResponse<object>> Retired(string? path = null)
    {
        _ = path;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(
            false,
            null,
            "This unversioned user-administration route is retired. Use the equivalent /api/v1/users route."));
    }
}

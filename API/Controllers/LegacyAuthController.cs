using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

/// <summary>
/// Explicit tombstones for the pre-v1 local-authentication prefix. Authentication
/// remains available without a municipality code through /api/v1/auth; keeping
/// this catch-all separate prevents future actions from silently exposing an
/// unversioned security contract.
/// </summary>
[ApiController]
[Route("api")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class LegacyAuthController : ControllerBase
{
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    [Route("auth")]
    [Route("auth/{**path}")]
    public ActionResult<ApiResponse<object>> Retired(string? path = null)
    {
        _ = path;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(
            false,
            null,
            "This unversioned authentication route is retired. Use the equivalent /api/v1/auth route."));
    }
}

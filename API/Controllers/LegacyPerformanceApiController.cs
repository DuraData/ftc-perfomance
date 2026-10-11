using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

/// <summary>
/// Explicit tombstones for pre-v1 performance API prefixes. Keeping these routes
/// separate prevents an unversioned alias from silently returning when a current
/// controller gains a new action.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class LegacyPerformanceApiController : ControllerBase
{
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    [Route("opms-targets")]
    [Route("opms-targets/{**path}")]
    [Route("ipms-targets")]
    [Route("ipms-targets/{**path}")]
    [Route("opms-submissions")]
    [Route("opms-submissions/{**path}")]
    [Route("ipms-submissions")]
    [Route("ipms-submissions/{**path}")]
    [Route("opms-target-library")]
    [Route("opms-target-library/{**path}")]
    [Route("ipms-target-library")]
    [Route("ipms-target-library/{**path}")]
    [Route("notifications")]
    [Route("notifications/{**path}")]
    public ActionResult<ApiResponse<object>> Retired(string? path = null)
    {
        _ = path;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(
            false,
            null,
            "This unversioned compatibility route is retired. Use the equivalent /api/v1 route."));
    }
}

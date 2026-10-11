using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

/// <summary>
/// Catch-all tombstones for IDP actions that pre-date the v1 contract. Specific
/// retirement actions on IdpController remain authoritative where they provide
/// more precise replacement guidance; this fallback prevents any former live
/// hierarchy or collaboration route from reaching the SPA fallback.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class LegacyIdpApiController : ControllerBase
{
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    [Route("idp")]
    [Route("idp/{**path}")]
    public ActionResult<ApiResponse<object>> Retired(string? path = null)
    {
        _ = path;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(
            false,
            null,
            "This unversioned IDP route is retired. Use the equivalent /api/v1/idp route."));
    }
}

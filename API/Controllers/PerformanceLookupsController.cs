using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/performance-lookups")]
[Authorize]
public sealed class PerformanceLookupsController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get() => StatusCode(StatusCodes.Status410Gone,
        new ApiResponse<object>(false, null,
            "This unbounded private-key catalogue is retired. Use the bounded tenant calendar and strategic-planning master page routes."));
}

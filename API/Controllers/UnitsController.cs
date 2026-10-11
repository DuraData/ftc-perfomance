using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class UnitsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public ActionResult<ApiResponse<object>> GetUnits() =>
        Gone(
            "This unbounded compatibility route is retired. Use /api/v1/masters/units/page.");

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public ActionResult<ApiResponse<object>> GetUnit(int id)
    {
        _ = id;
        return Gone("This integer-ID compatibility route is retired. Use /api/v1/masters/units/page with public identifiers.");
    }

    [HttpPost]
    [Authorize(Policy = "Permission:UNIT.CREATE")]
    public ActionResult<ApiResponse<object>> CreateUnit() =>
        Gone("Use POST /api/v1/masters/units with public identifiers, tenant context, and governance reason.");

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.UPDATE")]
    public ActionResult<ApiResponse<object>> UpdateUnit(int id) =>
        Gone("Use PUT /api/v1/masters/units/{publicId} with RowVersion and governance reason.");

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.UPDATE")]
    public ActionResult<ApiResponse<object>> DeleteUnit(int id) =>
        Gone("Units are retired through PUT /api/v1/masters/units/{publicId}; they are never deleted.");

    private ObjectResult Gone(string message) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null, message));
}

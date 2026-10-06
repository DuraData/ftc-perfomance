using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public class UnitsController : ControllerBase
{
    public UnitsController(ApplicationDbContext context)
    {
        _ = context;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public ActionResult<ApiResponse<UnitResponse[]>> GetUnits() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<UnitResponse[]>(false, null,
            "This unbounded compatibility route is retired. Use /api/v1/masters/units/page."));

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public ActionResult<ApiResponse<UnitResponse>> GetUnit(int id)
    {
        _ = id;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<UnitResponse>(false, null,
            "This integer-ID compatibility route is retired. Use /api/v1/masters/units/page with public identifiers."));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:UNIT.CREATE")]
    public ActionResult<ApiResponse<UnitResponse>> CreateUnit([FromBody] CreateUnitRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<UnitResponse>(false, null, "Use POST /api/v1/masters/units with public identifiers, tenant context, and governance reason."));

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.UPDATE")]
    public ActionResult<ApiResponse<UnitResponse>> UpdateUnit(int id, [FromBody] UpdateUnitRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<UnitResponse>(false, null, "Use PUT /api/v1/masters/units/{publicId} with RowVersion and governance reason."));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.UPDATE")]
    public ActionResult<ApiResponse<bool>> DeleteUnit(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Units are retired through PUT /api/v1/masters/units/{publicId}; they are never deleted."));
}

using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UnitsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public async Task<ActionResult<ApiResponse<UnitResponse[]>>> GetUnits()
    {
        var units = await _context.Units
            .AsNoTracking()
            .Include(unit => unit.Department)
            .OrderBy(unit => unit.Department.Name)
            .ThenBy(unit => unit.Name)
            .Select(unit => new UnitResponse(unit.Id, unit.DepartmentId, unit.Department.Name, unit.Code, unit.Name) { PublicId = unit.PublicId })
            .ToArrayAsync();

        return Ok(new ApiResponse<UnitResponse[]>(true, units));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public async Task<ActionResult<ApiResponse<UnitResponse>>> GetUnit(int id)
    {
        var unit = await _context.Units
            .AsNoTracking()
            .Include(item => item.Department)
            .Where(item => item.Id == id)
            .Select(item => new UnitResponse(item.Id, item.DepartmentId, item.Department.Name, item.Code, item.Name) { PublicId = item.PublicId })
            .FirstOrDefaultAsync();

        return unit == null
            ? NotFound(new ApiResponse<UnitResponse>(false, null, "Unit not found"))
            : Ok(new ApiResponse<UnitResponse>(true, unit));
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

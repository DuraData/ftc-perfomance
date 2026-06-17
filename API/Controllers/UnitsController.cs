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
    [Authorize(Policy = "Permission:Units.View")]
    public async Task<ActionResult<ApiResponse<UnitResponse[]>>> GetUnits()
    {
        var units = await _context.Units
            .AsNoTracking()
            .Include(unit => unit.Department)
            .OrderBy(unit => unit.Department.Name)
            .ThenBy(unit => unit.Name)
            .Select(unit => new UnitResponse(unit.Id, unit.DepartmentId, unit.Department.Name, unit.Code, unit.Name))
            .ToArrayAsync();

        return Ok(new ApiResponse<UnitResponse[]>(true, units));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Units.View")]
    public async Task<ActionResult<ApiResponse<UnitResponse>>> GetUnit(int id)
    {
        var unit = await _context.Units
            .AsNoTracking()
            .Include(item => item.Department)
            .Where(item => item.Id == id)
            .Select(item => new UnitResponse(item.Id, item.DepartmentId, item.Department.Name, item.Code, item.Name))
            .FirstOrDefaultAsync();

        return unit == null
            ? NotFound(new ApiResponse<UnitResponse>(false, null, "Unit not found"))
            : Ok(new ApiResponse<UnitResponse>(true, unit));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Units.Manage")]
    public async Task<ActionResult<ApiResponse<UnitResponse>>> CreateUnit([FromBody] CreateUnitRequest request)
    {
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
        {
            return BadRequest(new ApiResponse<UnitResponse>(false, null, "Department not found"));
        }

        var exists = await _context.Units.AnyAsync(unit => unit.DepartmentId == request.DepartmentId && unit.Code == request.Code);
        if (exists)
        {
            return Conflict(new ApiResponse<UnitResponse>(false, null, "Unit code already exists in the department"));
        }

        var unit = new Unit
        {
            DepartmentId = request.DepartmentId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim()
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<UnitResponse>(true, new UnitResponse(unit.Id, unit.DepartmentId, department.Name, unit.Code, unit.Name)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Units.Manage")]
    public async Task<ActionResult<ApiResponse<UnitResponse>>> UpdateUnit(int id, [FromBody] UpdateUnitRequest request)
    {
        var unit = await _context.Units.FindAsync(id);
        if (unit == null)
        {
            return NotFound(new ApiResponse<UnitResponse>(false, null, "Unit not found"));
        }

        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
        {
            return BadRequest(new ApiResponse<UnitResponse>(false, null, "Department not found"));
        }

        var exists = await _context.Units.AnyAsync(item => item.Id != id && item.DepartmentId == request.DepartmentId && item.Code == request.Code);
        if (exists)
        {
            return Conflict(new ApiResponse<UnitResponse>(false, null, "Unit code already exists in the department"));
        }

        unit.DepartmentId = request.DepartmentId;
        unit.Code = request.Code.Trim();
        unit.Name = request.Name.Trim();
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<UnitResponse>(true, new UnitResponse(unit.Id, unit.DepartmentId, department.Name, unit.Code, unit.Name)));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Units.Manage")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUnit(int id)
    {
        var unit = await _context.Units.FindAsync(id);
        if (unit == null)
        {
            return NotFound(new ApiResponse<bool>(false, false, "Unit not found"));
        }

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }
}

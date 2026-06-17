using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Departments.View")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse[]>>> GetDepartments()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentResponse(department.Id, department.Code, department.Name, department.Description))
            .ToArrayAsync();

        return Ok(new ApiResponse<DepartmentResponse[]>(true, departments));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Departments.View")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new DepartmentResponse(item.Id, item.Code, item.Name, item.Description))
            .FirstOrDefaultAsync();

        return department == null
            ? NotFound(new ApiResponse<DepartmentResponse>(false, null, "Department not found"))
            : Ok(new ApiResponse<DepartmentResponse>(true, department));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Departments.Manage")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var exists = await _context.Departments.AnyAsync(department => department.Code == request.Code);
        if (exists)
        {
            return Conflict(new ApiResponse<DepartmentResponse>(false, null, "Department code already exists"));
        }

        var department = new Department
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<DepartmentResponse>(true, new DepartmentResponse(department.Id, department.Code, department.Name, department.Description)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Departments.Manage")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> UpdateDepartment(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound(new ApiResponse<DepartmentResponse>(false, null, "Department not found"));
        }

        var codeInUse = await _context.Departments.AnyAsync(item => item.Id != id && item.Code == request.Code);
        if (codeInUse)
        {
            return Conflict(new ApiResponse<DepartmentResponse>(false, null, "Department code already exists"));
        }

        department.Code = request.Code.Trim();
        department.Name = request.Name.Trim();
        department.Description = request.Description?.Trim();
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<DepartmentResponse>(true, new DepartmentResponse(department.Id, department.Code, department.Name, department.Description)));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Departments.Manage")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDepartment(int id)
    {
        var department = await _context.Departments.Include(item => item.Units).FirstOrDefaultAsync(item => item.Id == id);
        if (department == null)
        {
            return NotFound(new ApiResponse<bool>(false, false, "Department not found"));
        }

        if (department.Units.Count > 0)
        {
            return BadRequest(new ApiResponse<bool>(false, false, "Delete units first before deleting the department"));
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }
}

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
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse[]>>> GetDepartments()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentResponse(department.Id, department.Code, department.Name, department.Description) { PublicId = department.PublicId })
            .ToArrayAsync();

        return Ok(new ApiResponse<DepartmentResponse[]>(true, departments));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public async Task<ActionResult<ApiResponse<DepartmentResponse>>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new DepartmentResponse(item.Id, item.Code, item.Name, item.Description) { PublicId = item.PublicId })
            .FirstOrDefaultAsync();

        return department == null
            ? NotFound(new ApiResponse<DepartmentResponse>(false, null, "Department not found"))
            : Ok(new ApiResponse<DepartmentResponse>(true, department));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:DEPARTMENT.CREATE")]
    public ActionResult<ApiResponse<DepartmentResponse>> CreateDepartment([FromBody] CreateDepartmentRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<DepartmentResponse>(false, null, "Use POST /api/v1/masters/departments with tenant context, governance reason, and effective dates."));

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.UPDATE")]
    public ActionResult<ApiResponse<DepartmentResponse>> UpdateDepartment(int id, [FromBody] UpdateDepartmentRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<DepartmentResponse>(false, null, "Use PUT /api/v1/masters/departments/{publicId} with RowVersion and governance reason."));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.UPDATE")]
    public ActionResult<ApiResponse<bool>> DeleteDepartment(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Departments are retired through PUT /api/v1/masters/departments/{publicId}; they are never deleted."));
}

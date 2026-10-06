using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    public DepartmentsController(ApplicationDbContext context)
    {
        _ = context;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public ActionResult<ApiResponse<DepartmentResponse[]>> GetDepartments() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<DepartmentResponse[]>(false, null,
            "This unbounded compatibility route is retired. Use /api/v1/masters/departments/page."));

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public ActionResult<ApiResponse<DepartmentResponse>> GetDepartment(int id)
    {
        _ = id;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<DepartmentResponse>(false, null,
            "This integer-ID compatibility route is retired. Use /api/v1/masters/departments/page with public identifiers."));
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

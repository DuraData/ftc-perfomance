using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class DepartmentsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public ActionResult<ApiResponse<object>> GetDepartments() =>
        Gone(
            "This unbounded compatibility route is retired. Use /api/v1/masters/departments/page.");

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public ActionResult<ApiResponse<object>> GetDepartment(int id)
    {
        _ = id;
        return Gone("This integer-ID compatibility route is retired. Use /api/v1/masters/departments/page with public identifiers.");
    }

    [HttpPost]
    [Authorize(Policy = "Permission:DEPARTMENT.CREATE")]
    public ActionResult<ApiResponse<object>> CreateDepartment() =>
        Gone("Use POST /api/v1/masters/departments with tenant context, governance reason, and effective dates.");

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.UPDATE")]
    public ActionResult<ApiResponse<object>> UpdateDepartment(int id) =>
        Gone("Use PUT /api/v1/masters/departments/{publicId} with RowVersion and governance reason.");

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:DEPARTMENT.UPDATE")]
    public ActionResult<ApiResponse<object>> DeleteDepartment(int id) =>
        Gone("Departments are retired through PUT /api/v1/masters/departments/{publicId}; they are never deleted.");

    private ObjectResult Gone(string message) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null, message));
}

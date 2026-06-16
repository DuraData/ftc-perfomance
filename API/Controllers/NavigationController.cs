using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/navigation")]
[Authorize]
public class NavigationController : ControllerBase
{
    [HttpGet("my-menu")]
    public ActionResult<ApiResponse<MenuItemResponse[]>> GetMyMenu()
    {
        var menu = BuildMenu();
        return Ok(new ApiResponse<MenuItemResponse[]>(true, menu));
    }

    internal static MenuItemResponse[] BuildMenu()
    {
        var items = new List<MenuItemResponse>
        {
            new("Dashboard", "/dashboard", "dashboard", null, false),
        };

        items.Add(new MenuItemResponse("Performance Management", null, "target", new[]
        {
            new MenuItemResponse("OPMS Targets", "/opms/targets", "target", null, false),
            new MenuItemResponse("OPMS Submissions", "/opms/submissions", "target", null, false),
            new MenuItemResponse("Vote Numbers", "/opms/vote-numbers", "layers", null, false),
            new MenuItemResponse("IPMS Targets", "/ipms/targets", "target", null, false),
            new MenuItemResponse("IPMS Submissions", "/ipms/submissions", "target", null, false),
            new MenuItemResponse("KPI Library", "/kpi-library", "library", null, false),
        }, false));

        items.Add(new MenuItemResponse("Workflow", null, "workflow", new[]
        {
            new MenuItemResponse("My Queue", "/workflow/my-queue", "workflow", null, false),
            new MenuItemResponse("Verification", "/workflow/verification", "workflow", null, false),
            new MenuItemResponse("Approval", "/workflow/approval", "workflow", null, false),
            new MenuItemResponse("PMS Review", "/workflow/pms-review", "workflow", null, false),
            new MenuItemResponse("Auditor Review", "/workflow/auditor-review", "workflow", null, false),
        }, false));

        items.Add(new MenuItemResponse("HR", null, "users", new[]
        {
            new MenuItemResponse("Employees", "/hr/employees", "users", null, false),
            new MenuItemResponse("Departments", "/hr/departments", "users", null, false),
            new MenuItemResponse("Units", "/hr/units", "users", null, false),
            new MenuItemResponse("Positions", "/hr/positions", "users", null, false),
            new MenuItemResponse("Contacts", "/hr/contacts", "users", null, false),
            new MenuItemResponse("Resumes", "/hr/resumes", "users", null, false),
        }, false));

        items.Add(new MenuItemResponse("Projects & Tasks", null, "projects", new[]
        {
            new MenuItemResponse("Tasks", "/tasks", "layers", null, false),
        }, false));

        items.Add(new MenuItemResponse("Location", null, "globe", new[]
        {
            new MenuItemResponse("Countries", "/location/countries", "globe", null, false),
            new MenuItemResponse("Provinces", "/location/provinces", "globe", null, false),
            new MenuItemResponse("Cities", "/location/cities", "globe", null, false),
            new MenuItemResponse("Suburbs", "/location/suburbs", "globe", null, false),
            new MenuItemResponse("Addresses", "/location/addresses", "globe", null, false),
        }, false));

        items.Add(new MenuItemResponse("Reports", "/reports", "reports", null, false));

        items.Add(new MenuItemResponse("Divider", null, null, null, true));

        items.Add(new MenuItemResponse("System Administration", null, "settings", new[]
        {
            new MenuItemResponse("Users", "/system-administration/users", "users", null, false),
            new MenuItemResponse("Roles", "/system-administration/roles", "users-group", null, false),
            new MenuItemResponse("Permissions", "/system-administration/permissions", "key", null, false),
            new MenuItemResponse("Audit Logs", "/system-administration/audit-logs", "history", null, false),
        }, false));

        items.Add(new MenuItemResponse("Configuration", null, "settings", new[]
        {
            new MenuItemResponse("Periods", "/admin/periods", "settings", null, false),
            new MenuItemResponse("Organisations", "/admin/organisations", "settings", null, false),
            new MenuItemResponse("Approval Setup", "/admin/approval-setup", "settings", null, false),
            new MenuItemResponse("Lookup Tables", "/admin/lookups", "settings", null, false),
            new MenuItemResponse("Budget Types", "/admin/budget-types", "settings", null, false),
            new MenuItemResponse("Strategic Goals", "/admin/strategic-goals", "settings", null, false),
            new MenuItemResponse("Strategic Objectives", "/admin/strategic-objectives", "settings", null, false),
            new MenuItemResponse("Units of Measure", "/admin/units-measure", "settings", null, false),
            new MenuItemResponse("KPAs", "/admin/kpas", "settings", null, false),
            new MenuItemResponse("Municipal KPAs", "/admin/municipal-kpas", "settings", null, false),
            new MenuItemResponse("Departmental Objectives", "/admin/departmental-objectives", "settings", null, false),
            new MenuItemResponse("Outputs", "/admin/outputs", "settings", null, false),
            new MenuItemResponse("Performance Objectives", "/admin/performance-objectives", "settings", null, false),
            new MenuItemResponse("Priority Issues", "/admin/priority-issues", "settings", null, false),
            new MenuItemResponse("Occupations", "/admin/occupations", "settings", null, false),
            new MenuItemResponse("Industries", "/admin/industries", "settings", null, false),
        }, false));

        items.Add(new MenuItemResponse("Settings", "/settings", "settings", null, false));

        return items.ToArray();
    }
}

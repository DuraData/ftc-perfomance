using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/navigation")]
[Authorize]
public class NavigationController : ControllerBase
{
    [HttpGet("my-menu")]
    public ActionResult<ApiResponse<MenuItemResponse[]>> GetMyMenu()
    {
        var permissions = User.Claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        var fullAccess = IsSystemAdministrator(roles);
        var menu = BuildMenu(permissions, fullAccess);
        return Ok(new ApiResponse<MenuItemResponse[]>(true, menu));
    }

    internal static bool IsSystemAdministrator(IEnumerable<string> roles)
    {
        var systemAdminRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Super Admin",
            "System Admin",
            "System Administrator",
            "EPMS Admin",
            "ICT Admin",
            "ICT Sub-Admin"
        };

        return roles.Any(systemAdminRoles.Contains);
    }

    internal static MenuItemResponse[] BuildMenu(ISet<string> permissions, bool fullAccess)
    {
        static bool HasPermission(ISet<string> currentPermissions, params string[] codes) =>
            codes.Any(code => currentPermissions.Contains(code));

        static bool HasModuleAccess(ISet<string> currentPermissions, string module) =>
            currentPermissions.Any(code => code.StartsWith($"{module}.", StringComparison.OrdinalIgnoreCase));

        var items = new List<MenuItemResponse>
        {
            new("Dashboard", "/dashboard", "dashboard", null, false),
        };

        var performanceChildren = new List<MenuItemResponse>();
        if (fullAccess || HasModuleAccess(permissions, "OPMS"))
        {
            performanceChildren.Add(new MenuItemResponse("OPMS Targets", "/opms/targets", "target", null, false));
            performanceChildren.Add(new MenuItemResponse("OPMS Submissions", "/opms/submissions", "target", null, false));
            performanceChildren.Add(new MenuItemResponse("Vote Numbers", "/opms/vote-numbers", "layers", null, false));
        }

        if (fullAccess || HasModuleAccess(permissions, "IPMS"))
        {
            performanceChildren.Add(new MenuItemResponse("IPMS Targets", "/ipms/targets", "target", null, false));
            performanceChildren.Add(new MenuItemResponse("IPMS Submissions", "/ipms/submissions", "target", null, false));
        }

        if (fullAccess || performanceChildren.Count > 0)
        {
            performanceChildren.Add(new MenuItemResponse("KPI Library", "/kpi-library", "library", null, false));
            items.Add(new MenuItemResponse("Performance Management", null, "target", performanceChildren.ToArray(), false));
        }

        var workflowChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "OPMS.Submit", "IPMS.Submit", "OPMS.Verify", "IPMS.Verify", "OPMS.Approve", "IPMS.Approve", "Audit.View"))
        {
            workflowChildren.Add(new MenuItemResponse("My Queue", "/workflow/my-queue", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "OPMS.Verify", "IPMS.Verify"))
        {
            workflowChildren.Add(new MenuItemResponse("Verification", "/workflow/verification", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "OPMS.Approve", "IPMS.Approve"))
        {
            workflowChildren.Add(new MenuItemResponse("Approval", "/workflow/approval", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "OPMS.View", "IPMS.View", "Reports.View"))
        {
            workflowChildren.Add(new MenuItemResponse("PMS Review", "/workflow/pms-review", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Audit.View"))
        {
            workflowChildren.Add(new MenuItemResponse("Auditor Review", "/workflow/auditor-review", "workflow", null, false));
        }
        if (workflowChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Workflow", null, "workflow", workflowChildren.ToArray(), false));
        }

        if (fullAccess)
        {
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
        }

        if (fullAccess || permissions.Contains("Reports.View"))
            items.Add(new MenuItemResponse("Reports", "/reports", "reports", null, false));

        var adminChildren = new List<MenuItemResponse>();
        if (fullAccess || permissions.Contains("Admin.Users.Manage"))
            adminChildren.Add(new MenuItemResponse("Users", "/system-administration/users", "users", null, false));
        if (fullAccess || permissions.Contains("Admin.Roles.Manage"))
            adminChildren.Add(new MenuItemResponse("Roles", "/system-administration/roles", "users-group", null, false));
        if (fullAccess || permissions.Contains("Admin.Permissions.Manage"))
            adminChildren.Add(new MenuItemResponse("Permissions", "/system-administration/permissions", "key", null, false));
        if (fullAccess || permissions.Contains("Audit.LoginLogs.View"))
            adminChildren.Add(new MenuItemResponse("Audit Logs", "/system-administration/audit-logs", "history", null, false));

        if (adminChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Divider", null, null, null, true));
            items.Add(new MenuItemResponse("System Administration", null, "settings", adminChildren.ToArray(), false));
        }

        if (fullAccess)
        {
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
        }

        return items.ToArray();
    }
}

using FTCERP.Host.API.Responses;
using FTCERP.Host.Infrastructure.Security;
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
        var fullAccess = SecurityModel.IsSuperAdmin(roles);

        if (!fullAccess && roles.Any(role => string.Equals(role, SecurityModel.Submitter, StringComparison.OrdinalIgnoreCase)))
        {
            var submitterMenu = BuildSubmitterMenu(permissions);
            return Ok(new ApiResponse<MenuItemResponse[]>(true, submitterMenu));
        }

        var menu = BuildMenu(permissions, fullAccess);
        return Ok(new ApiResponse<MenuItemResponse[]>(true, menu));
    }

    private static MenuItemResponse[] BuildSubmitterMenu(ISet<string> permissions)
    {
        static bool HasPermission(ISet<string> currentPermissions, params string[] codes) =>
            codes.Any(code => currentPermissions.Contains(code));

        var items = new List<MenuItemResponse>
        {
            new("Dashboard", null, "dashboard", [
                new MenuItemResponse("My Performance Dashboard", "/dashboard", "dashboard", null, false),
                new MenuItemResponse("IDP Dashboard", "/idp/dashboard", "dashboard", null, false)
            ], false)
        };

        if (HasPermission(permissions, "IDP.Plan.View", "IDP.Dashboard.View", "IDP.Alignment.View", "IDP.Participation.View", "IDP.Reports.Generate"))
        {
            items.Add(new MenuItemResponse("IDP", null, "map", [
                new MenuItemResponse("IDP Overview", "/idp/overview", "map", null, false),
                new MenuItemResponse("Strategic Objectives", "/idp/strategic-objectives", "target", null, false),
                new MenuItemResponse("IDP Projects", "/idp/projects", "layers", null, false),
                new MenuItemResponse("IDP KPIs", "/idp/kpis", "bar-chart", null, false),
                new MenuItemResponse("IDP Reports", "/idp/reports", "reports", null, false)
            ], false));
        }

        if (HasPermission(permissions, "IDP.Risk.Manage"))
        {
            items.Add(new MenuItemResponse("Risk Management", null, "shield-alert", [
                new MenuItemResponse("Risk Dashboard", "/risk/dashboard", "shield-alert", null, false),
                new MenuItemResponse("Risk Register", "/risk/register", "clipboard-list", null, false),
                new MenuItemResponse("Assessments", "/risk/assessments", "file-search", null, false),
                new MenuItemResponse("Treatment Plans", "/risk/treatment-plans", "heart-pulse", null, false),
                new MenuItemResponse("Reviews", "/risk/reviews", "refresh-cw", null, false),
                new MenuItemResponse("Heatmap", "/risk/heatmap", "thermometer", null, false),
                new MenuItemResponse("Reports", "/risk/reports", "reports", null, false)
            ], false));
        }

        if (HasPermission(permissions, "OPMS.Targets.View", "OPMS.Submissions.View", "OPMS.Submissions.Create"))
        {
            items.Add(new MenuItemResponse("OPMS", null, "target", [
                new MenuItemResponse("OPMS Dashboard", "/opms/dashboard", "dashboard", null, false),
                new MenuItemResponse("My OPMS Targets", "/opms/targets", "target", null, false),
                new MenuItemResponse("My OPMS Submissions", "/opms/submissions", "file-text", null, false)
            ], false));
        }

        if (HasPermission(permissions, "IPMS.Targets.View", "IPMS.Submissions.View", "IPMS.Submissions.Create"))
        {
            items.Add(new MenuItemResponse("IPMS", null, "target", [
                new MenuItemResponse("IPMS Dashboard", "/ipms/dashboard", "dashboard", null, false),
                new MenuItemResponse("My IPMS Targets", "/ipms/targets", "target", null, false),
                new MenuItemResponse("My IPMS Submissions", "/ipms/submissions", "file-text", null, false)
            ], false));
        }

        if (HasPermission(permissions, "Workflow.Submit.View"))
        {
            items.Add(new MenuItemResponse("Workflow Queues", null, "workflow", [
                new MenuItemResponse("My Drafts", "/workflow/my-drafts", "workflow", null, false),
                new MenuItemResponse("Pending Submission", "/workflow/pending-submission", "workflow", null, false),
                new MenuItemResponse("Returned Submissions", "/workflow/returned-submissions", "workflow", null, false),
                new MenuItemResponse("Under Verification", "/workflow/under-verification", "workflow", null, false),
                new MenuItemResponse("Under Review", "/workflow/under-review", "workflow", null, false),
                new MenuItemResponse("Under Approval", "/workflow/under-approval", "workflow", null, false),
                new MenuItemResponse("Internal Audit Returned", "/workflow/internal-audit-returned", "workflow", null, false),
                new MenuItemResponse("Approved / Closed", "/workflow/approved-closed", "workflow", null, false)
            ], false));
        }

        if (HasPermission(permissions, "Reports.View", "IDP.Reports.Generate"))
        {
            items.Add(new MenuItemResponse("Reports", null, "reports", [
                new MenuItemResponse("My OPMS Performance Report", "/reports/opms-performance", "reports", null, false),
                new MenuItemResponse("My IPMS Performance Report", "/reports/ipms-performance", "reports", null, false),
                new MenuItemResponse("IDP Summary Report", "/reports/idp-summary", "reports", null, false),
                new MenuItemResponse("My Submission Status Report", "/reports/submission-status", "reports", null, false),
                new MenuItemResponse("My Evidence Register", "/reports/evidence-register", "reports", null, false),
                new MenuItemResponse("My Returned Submissions Report", "/reports/returned-submissions", "reports", null, false),
                new MenuItemResponse("My Overdue Submissions Report", "/reports/overdue-submissions", "reports", null, false)
            ], false));
        }

        if (HasPermission(permissions, "Notifications.View"))
        {
            items.Add(new MenuItemResponse("Notifications", "/notifications", "bell", null, false));
        }

        items.Add(new MenuItemResponse("My Profile", "/my-profile", "user", null, false));

        return items.ToArray();
    }

    internal static MenuItemResponse[] BuildMenu(ISet<string> permissions, bool fullAccess)
    {
        static bool HasPermission(ISet<string> currentPermissions, params string[] codes) =>
            codes.Any(code => currentPermissions.Contains(code));

        var items = new List<MenuItemResponse>
        {
            new("Dashboard", "/dashboard", "dashboard", null, false)
        };

        var opmsChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "OPMS.Library.View", "OPMS.Library.Create", "OPMS.Library.Edit", "OPMS.Library.UseTemplate"))
        {
            opmsChildren.Add(new MenuItemResponse("OPMS Target Library", "/opms/library", "library", null, false));
        }
        if (fullAccess || HasPermission(permissions, "OPMS.View", "Targets.View", "Targets.Manage"))
        {
            opmsChildren.Add(new MenuItemResponse("OPMS Targets", "/opms/targets", "target", null, false));
            opmsChildren.Add(new MenuItemResponse("OPMS Submissions", "/opms/submissions", "target", null, false));
            opmsChildren.Add(new MenuItemResponse("Vote Numbers", "/opms/vote-numbers", "layers", null, false));
        }
        if (opmsChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("OPMS", null, "target", opmsChildren.ToArray(), false));
        }

        var ipmsChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "IPMS.Library.View", "IPMS.Library.Create", "IPMS.Library.Edit", "IPMS.Library.UseTemplate"))
        {
            ipmsChildren.Add(new MenuItemResponse("IPMS Target Library", "/ipms/library", "library", null, false));
        }
        if (fullAccess || HasPermission(permissions, "IPMS.View", "Targets.View", "Targets.Manage"))
        {
            ipmsChildren.Add(new MenuItemResponse("IPMS Targets", "/ipms/targets", "target", null, false));
            ipmsChildren.Add(new MenuItemResponse("IPMS Submissions", "/ipms/submissions", "target", null, false));
        }
        if (ipmsChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("IPMS", null, "target", ipmsChildren.ToArray(), false));
        }

        var idpChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "IDP.Dashboard.View"))
        {
            idpChildren.Add(new MenuItemResponse("IDP Dashboard", "/idp/dashboard", "dashboard", null, false));
        }
        if (fullAccess || HasPermission(permissions, "IDP.Plan.View", "IDP.Plan.Manage"))
        {
            idpChildren.Add(new MenuItemResponse("IDP Plans", "/idp/plans", "target", null, false));
        }
        if (fullAccess || HasPermission(permissions, "IDP.Hierarchy.Manage"))
        {
            idpChildren.Add(new MenuItemResponse("Planning Hierarchy", "/idp/hierarchy", "target", null, false));
        }
        if (fullAccess || HasPermission(permissions, "IDP.Participation.View", "IDP.Participation.Manage"))
        {
            idpChildren.Add(new MenuItemResponse("Community Participation", "/idp/community", "users", null, false));
        }
        if (fullAccess || HasPermission(permissions, "IDP.Alignment.View", "IDP.Alignment.Manage"))
        {
            idpChildren.Add(new MenuItemResponse("Alignment Matrix", "/idp/alignment", "layers", null, false));
        }
        if (fullAccess || HasPermission(permissions, "IDP.Reports.Generate"))
        {
            idpChildren.Add(new MenuItemResponse("IDP Reports", "/idp/reports", "reports", null, false));
        }
        if (idpChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("IDP", null, "target", idpChildren.ToArray(), false));
        }

        var riskChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "IDP.Risk.Manage"))
        {
            riskChildren.Add(new MenuItemResponse("Risk Dashboard", "/risk/dashboard", "shield-alert", null, false));
            riskChildren.Add(new MenuItemResponse("Risk Register", "/risk/register", "clipboard-list", null, false));
            riskChildren.Add(new MenuItemResponse("Assessments", "/risk/assessments", "file-search", null, false));
            riskChildren.Add(new MenuItemResponse("Treatment Plans", "/risk/treatment-plans", "heart-pulse", null, false));
            riskChildren.Add(new MenuItemResponse("Reviews", "/risk/reviews", "refresh-cw", null, false));
            riskChildren.Add(new MenuItemResponse("Heatmap", "/risk/heatmap", "thermometer", null, false));
            riskChildren.Add(new MenuItemResponse("Reports", "/risk/reports", "reports", null, false));
        }
        if (riskChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Risk Management", null, "shield-alert", riskChildren.ToArray(), false));
        }

        var workflowChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "Workflow.Submit.View", "Workflow.Verify.View", "Workflow.Review.View", "Workflow.Approve.View", "Workflow.Audit.View"))
        {
            workflowChildren.Add(new MenuItemResponse("My Queue", "/workflow/my-queue", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Workflow.Verify.View"))
        {
            workflowChildren.Add(new MenuItemResponse("Verification", "/workflow/verification", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Workflow.Review.View"))
        {
            workflowChildren.Add(new MenuItemResponse("Review", "/workflow/pms-review", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Workflow.Approve.View"))
        {
            workflowChildren.Add(new MenuItemResponse("Approval", "/workflow/approval", "workflow", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Workflow.Audit.View"))
        {
            workflowChildren.Add(new MenuItemResponse("Auditor Review", "/workflow/auditor-review", "workflow", null, false));
        }
        if (workflowChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Workflow", null, "workflow", workflowChildren.ToArray(), false));
        }

        var organizationChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "Departments.View", "Departments.Manage"))
        {
            organizationChildren.Add(new MenuItemResponse("Departments", "/hr/departments", "users", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Units.View", "Units.Manage"))
        {
            organizationChildren.Add(new MenuItemResponse("Units", "/hr/units", "users", null, false));
        }
        if (fullAccess || HasPermission(permissions, "Admin.Users.Manage", "UserDirectory.View"))
        {
            organizationChildren.Add(new MenuItemResponse("Employees", "/hr/employees", "users", null, false));
        }
        if (organizationChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Organisation", null, "users", organizationChildren.ToArray(), false));
        }

        if (fullAccess || HasPermission(permissions, "Reports.View", "Reports.Department.View", "Reports.Approval.View", "Reports.Verification.View", "Reports.InternalAudit.View", "Audit.Reports.View"))
            items.Add(new MenuItemResponse("Reports", "/reports", "reports", null, false));

        var adminChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "Admin.Users.Manage"))
            adminChildren.Add(new MenuItemResponse("Users", "/system-administration/users", "users", null, false));
        if (fullAccess || HasPermission(permissions, "Admin.Roles.Manage"))
            adminChildren.Add(new MenuItemResponse("Roles", "/system-administration/roles", "users-group", null, false));
        if (fullAccess || HasPermission(permissions, "Admin.Permissions.Manage"))
            adminChildren.Add(new MenuItemResponse("Permissions", "/system-administration/permissions", "key", null, false));
        if (fullAccess || HasPermission(permissions, "Departments.Manage"))
            adminChildren.Add(new MenuItemResponse("Department Management", "/hr/departments", "users", null, false));
        if (fullAccess || HasPermission(permissions, "Units.Manage"))
            adminChildren.Add(new MenuItemResponse("Unit Management", "/hr/units", "users", null, false));
        if (fullAccess || HasPermission(permissions, "Audit.LoginLogs.View", "Audit.Logs.View"))
            adminChildren.Add(new MenuItemResponse("Audit Logs", "/system-administration/audit-logs", "history", null, false));
        if (fullAccess || HasPermission(permissions, "RoleImplementationAudit.View"))
            adminChildren.Add(new MenuItemResponse("Role Implementation Audit", "/system-administration/role-implementation-audit", "history", null, false));
        if (fullAccess || HasPermission(permissions, "SystemAdministration.RoleAccessMatrix.View"))
            adminChildren.Add(new MenuItemResponse("Role Access Matrix", "/system-administration/role-access-matrix", "history", null, false));
        if (fullAccess || HasPermission(permissions, "SystemAdministration.PermissionSimulation.View"))
            adminChildren.Add(new MenuItemResponse("Permission Simulation", "/system-administration/permission-simulation", "history", null, false));
        if (fullAccess || HasPermission(permissions, "SystemAdministration.SystemCoverageAudit.View"))
            adminChildren.Add(new MenuItemResponse("System Coverage Audit", "/system-administration/system-coverage-audit", "history", null, false));

        if (adminChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Divider", null, null, null, true));
            items.Add(new MenuItemResponse("System Administration", null, "settings", adminChildren.ToArray(), false));
        }

        var configurationChildren = new List<MenuItemResponse>();
        if (fullAccess || HasPermission(permissions, "Configuration.Manage", "Configuration.View"))
        {
            configurationChildren.Add(new MenuItemResponse("Periods", "/admin/periods", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("Approval Setup", "/admin/approval-setup", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("Lookup Tables", "/admin/lookups", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("Budget Types", "/admin/budget-types", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("Strategic Goals", "/admin/strategic-goals", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("Strategic Objectives", "/admin/strategic-objectives", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("Units of Measure", "/admin/units-measure", "settings", null, false));
            configurationChildren.Add(new MenuItemResponse("KPAs", "/admin/kpas", "settings", null, false));
        }
        if (configurationChildren.Count > 0)
        {
            items.Add(new MenuItemResponse("Configuration", null, "settings", configurationChildren.ToArray(), false));
        }

        if (fullAccess || HasPermission(permissions, "Notifications.View", "Notifications.Manage"))
        {
            items.Add(new MenuItemResponse("Notifications", "/settings", "settings", null, false));
        }

        return items.ToArray();
    }
}

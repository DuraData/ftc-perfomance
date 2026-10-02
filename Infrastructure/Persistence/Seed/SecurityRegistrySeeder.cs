using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence.Seed;

public static class SecurityRegistrySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var resources = new[]
        {
            Resource("OPMS_KPI", "OPMS KPI", true, true, true, false, true, true),
            Resource("OPMS_SUBMISSION", "OPMS Submission", true, true, true, false, true, true),
            Resource("OPMS_POE", "OPMS Evidence", true, true, true, false, true, true),
            Resource("OPMS_RFI", "OPMS Request for Information", true, true, true, false, true, true),
            Resource("OPMS_REPORT", "OPMS Report", false, true, false, false, true, true),
            Resource("IPMS_REPORT", "IPMS Report", false, true, false, false, true, true),
            Resource("OPMS_WORKFLOW", "OPMS Workflow", false, true, false, false, false, true),
            Resource("IPMS_KPI", "IPMS KPI", true, true, true, false, true, true),
            Resource("IPMS_SUBMISSION", "IPMS Submission", true, true, true, false, true, true),
            Resource("IPMS_POE", "IPMS Evidence", true, true, true, false, true, true),
            Resource("IPMS_RFI", "IPMS Request for Information", true, true, true, false, true, true),
            Resource("IPMS_WORKFLOW", "IPMS Workflow", false, true, false, false, false, true),
            Resource("MUNICIPALITY", "Municipality", true, true, true, false, true, true),
            Resource("DEPARTMENT", "Department", true, true, true, false, true, true),
            Resource("UNIT", "Unit", true, true, true, false, true, true),
            Resource("POSITION", "Position", true, true, true, false, true, true),
            Resource("EMPLOYEE", "Municipal Employee", true, true, true, false, true, true),
            Resource("USER", "User Account", true, true, true, false, true, true),
            Resource("ROLE", "Security Role", true, true, true, false, true, true),
            Resource("FINANCIAL_YEAR", "Financial Year", true, true, true, false, true, true),
            Resource("REPORTING_PERIOD", "Reporting Period", true, true, true, false, true, true),
            Resource("EMPLOYEE_ASSIGNMENT", "Employee Assignment", true, true, true, false, true, true),
            Resource("IDP_PLAN", "IDP Plan", true, true, true, false, true, true),
            Resource("IDP_PROJECT", "IDP Project", true, true, true, false, true, true),
            Resource("IDP_INDICATOR", "IDP Indicator", true, true, true, false, true, true),
            Resource("TID", "Technical Indicator Definition", true, true, true, false, true, true),
            Resource("C88_INDICATOR", "Circular 88 Indicator", true, true, true, false, true, true),
            Resource("C88_REPORT", "Circular 88 Report", true, true, true, false, true, true)
        };
        var existingResourceCodes = await context.SecurityResources.Select(item => item.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.SecurityResources.AddRange(resources.Where(item => !existingResourceCodes.Contains(item.Code)));

        var actions = new[]
        {
            Action("OPMS_KPI.ACTIVATE", "Activate KPI", "OPMS_KPI"), Action("OPMS_KPI.WITHDRAW", "Withdraw KPI", "OPMS_KPI"), Action("OPMS_KPI.REVISE", "Revise KPI", "OPMS_KPI"),
            Action("OPMS_SUBMISSION.SAVE", "Save Submission", "OPMS_SUBMISSION"), Action("OPMS_SUBMISSION.SUBMIT", "Submit", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.VERIFY", "Verify", "OPMS_SUBMISSION"), Action("OPMS_SUBMISSION.VERIFY_REJECT", "Reject Verification", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.APPROVE", "Approve", "OPMS_SUBMISSION"), Action("OPMS_SUBMISSION.REJECT", "Reject Approval", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.EXTEND_DUE_DATE", "Extend Due Date", "OPMS_SUBMISSION"),
            Action("OPMS_WORKFLOW.PMS_REVIEW", "PMS Review", "OPMS_WORKFLOW"), Action("OPMS_WORKFLOW.INTERNAL_AUDIT", "Internal Audit Assess", "OPMS_WORKFLOW"),
            Action("OPMS_RFI.RAISE", "Raise RFI", "OPMS_RFI"), Action("OPMS_RFI.RESPOND", "Respond to RFI", "OPMS_RFI"), Action("OPMS_RFI.CLOSE", "Close RFI", "OPMS_RFI"),
            Action("OPMS_POE.UPLOAD", "Upload Evidence", "OPMS_POE"), Action("OPMS_POE.REPLACE", "Replace Evidence", "OPMS_POE"), Action("OPMS_POE.ASSESS", "Assess Evidence", "OPMS_POE"), Action("OPMS_POE.PLACE_HOLD", "Place Evidence Legal Hold", "OPMS_POE"), Action("OPMS_POE.RELEASE_HOLD", "Release Evidence Legal Hold", "OPMS_POE"), Action("OPMS_POE.DISPOSE", "Dispose Retained Evidence", "OPMS_POE"),
            Action("OPMS_REPORT.GENERATE", "Generate Report", "OPMS_REPORT"), Action("OPMS_REPORT.EXPORT", "Export Report", "OPMS_REPORT"),
            Action("IPMS_REPORT.GENERATE", "Generate Report", "IPMS_REPORT"), Action("IPMS_REPORT.EXPORT", "Export Report", "IPMS_REPORT"),
            Action("IPMS_SUBMISSION.SUBMIT", "Submit", "IPMS_SUBMISSION"), Action("IPMS_SUBMISSION.VERIFY", "Verify", "IPMS_SUBMISSION"),
            Action("IPMS_SUBMISSION.VERIFY_REJECT", "Reject Verification", "IPMS_SUBMISSION"), Action("IPMS_SUBMISSION.APPROVE", "Approve", "IPMS_SUBMISSION"),
            Action("IPMS_SUBMISSION.REJECT", "Reject Approval", "IPMS_SUBMISSION"), Action("IPMS_SUBMISSION.EXTEND_DUE_DATE", "Extend Due Date", "IPMS_SUBMISSION"),
            Action("IPMS_WORKFLOW.PMS_REVIEW", "PMS Review", "IPMS_WORKFLOW"), Action("IPMS_WORKFLOW.INTERNAL_AUDIT", "Internal Audit Assess", "IPMS_WORKFLOW"),
            Action("IPMS_RFI.RAISE", "Raise RFI", "IPMS_RFI"), Action("IPMS_RFI.RESPOND", "Respond to RFI", "IPMS_RFI"), Action("IPMS_RFI.CLOSE", "Close RFI", "IPMS_RFI"),
            Action("IPMS_POE.UPLOAD", "Upload Evidence", "IPMS_POE"), Action("IPMS_POE.REPLACE", "Replace Evidence", "IPMS_POE"), Action("IPMS_POE.ASSESS", "Assess Evidence", "IPMS_POE"), Action("IPMS_POE.PLACE_HOLD", "Place Evidence Legal Hold", "IPMS_POE"), Action("IPMS_POE.RELEASE_HOLD", "Release Evidence Legal Hold", "IPMS_POE"), Action("IPMS_POE.DISPOSE", "Dispose Retained Evidence", "IPMS_POE"),
            Action("USER.ENABLE", "Enable User", "USER"), Action("USER.DISABLE", "Disable User", "USER"), Action("ROLE.ASSIGN", "Assign Role", "ROLE"),
            Action("SECURITY.VIEW", "View Security Configuration", "ROLE"), Action("SECURITY.MANAGE_ROLES", "Manage Roles", "ROLE"),
            Action("SECURITY.ASSIGN_ROLES", "Assign Roles", "ROLE"), Action("SECURITY.MANAGE_PERMISSIONS", "Manage Permissions", "ROLE"),
            Action("SECURITY.MANAGE_NAVIGATION", "Manage Navigation", "ROLE"), Action("SECURITY.VIEW_EFFECTIVE", "View Effective Permissions", "ROLE"),
            Action("SECURITY.SYSTEM_SCOPE", "Administer System Scope", "ROLE"),
            Action("WORKFLOW.CONFIGURE", "Configure Workflow and Reporting Windows", "OPMS_WORKFLOW"),
            Action("C88_REPORT.SUBMIT", "Submit C88 Report", "C88_REPORT"), Action("C88_REPORT.VERIFY", "Verify C88 Report", "C88_REPORT"), Action("C88_REPORT.FINAL_SUBMIT", "Final Submit C88 Report", "C88_REPORT")
        };
        var existingActionCodes = await context.SecurityActionDefinitions.Select(item => item.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.SecurityActionDefinitions.AddRange(actions.Where(item => !existingActionCodes.Contains(item.Code)));

        await context.SaveChangesAsync();
        await SeedNavigationAsync(context);
        await SeedMembersAsync(context);
        await SeedPermissionsAsync(context, resources, actions);
        await MigrateLegacyRolePermissionsAsync(context);
    }

    private static async Task SeedMembersAsync(ApplicationDbContext context)
    {
        var members = new[]
        {
            Member("OPMS_SUBMISSION", "ActualPerformance", "Actual Performance"),
            Member("OPMS_SUBMISSION", "Variance", "Variance", systemManaged: true),
            Member("OPMS_SUBMISSION", "SubmittedDate", "Submitted Date", systemManaged: true),
            Member("OPMS_SUBMISSION", "InternalAuditObservation", "Internal Audit Observation", sensitive: true),
            Member("IPMS_SUBMISSION", "ActualPerformance", "Actual Performance"),
            Member("IPMS_SUBMISSION", "Variance", "Variance", systemManaged: true),
            Member("IPMS_SUBMISSION", "SubmittedDate", "Submitted Date", systemManaged: true),
            Member("IPMS_SUBMISSION", "InternalAuditObservation", "Internal Audit Observation", sensitive: true),
            Member("EMPLOYEE", "SalaryReference", "Salary Reference", sensitive: true),
            Member("EMPLOYEE", "EmailAddress", "Email Address")
        };
        var existing = await context.SecurityMemberDefinitions
            .Select(item => item.ResourceCode + "|" + item.MemberCode)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.SecurityMemberDefinitions.AddRange(members.Where(item => !existing.Contains(item.ResourceCode + "|" + item.MemberCode)));
        await context.SaveChangesAsync();
    }

    public static async Task BackfillAssignmentsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;
        var existing = await context.SecurityUserRoleAssignments.Where(item => item.IsActive).Select(item => new { item.UserId, item.RoleId }).ToListAsync();
        var keys = existing.Select(item => $"{item.UserId}|{item.RoleId}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var identityAssignments = await context.UserRoles.AsNoTracking().ToListAsync();
        foreach (var item in identityAssignments.Where(item => !keys.Contains($"{item.UserId}|{item.RoleId}")))
        {
            context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment { UserId = item.UserId, RoleId = item.RoleId, EffectiveFrom = now, AssignedAt = now, AssignedBy = "BOOTSTRAP", IsActive = true });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedNavigationAsync(ApplicationDbContext context)
    {
        var roots = new[]
        {
            Nav("NAV.DASHBOARD", "Dashboard", "/dashboard", "dashboard", 10, "NAV.DASHBOARD"),
            Nav("NAV.SDBIP", "SDBIP / OPMS", null, "target", 20, null), Nav("NAV.IPMS", "IPMS", null, "target", 30, null),
            Nav("NAV.WORKFLOW", "My Workflow", null, "workflow", 40, null), Nav("NAV.POE", "POE", "/opms/submissions", "file-text", 50, "NAV.POE"),
            Nav("NAV.REPORTS", "Reports", "/reports", "reports", 60, "NAV.REPORTS"), Nav("NAV.IDP", "IDP", null, "map", 70, null),
            Nav("NAV.RISK", "Risk Management", null, "shield-alert", 80, null), Nav("NAV.C88", "Circular 88", null, "layers", 90, null),
            Nav("NAV.ORGANISATION", "Organisation", null, "users", 100, null), Nav("NAV.CONFIGURATION", "Configuration", null, "settings", 110, null),
            Nav("NAV.ADMIN", "Administration", null, "settings", 120, null), Nav("NAV.NOTIFICATIONS", "Notifications", "/notifications", "bell", 130, "NAV.NOTIFICATIONS"),
            Nav("NAV.PROFILE", "My Profile", "/my-profile", "user", 140, "NAV.PROFILE")
        };
        var existingCodes = await context.SecurityNavigationItems.Select(item => item.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.SecurityNavigationItems.AddRange(roots.Where(item => !existingCodes.Contains(item.Code)));
        await context.SaveChangesAsync();

        var rootIds = await context.SecurityNavigationItems.Where(item => roots.Select(root => root.Code).Contains(item.Code)).ToDictionaryAsync(item => item.Code, item => item.Id);
        var children = new[]
        {
            Nav("NAV.SDBIP.REGISTER", "SDBIP Register", "/opms/targets", "target", 10, "NAV.SDBIP.REGISTER", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.CAPTURE", "Performance Reporting", "/opms/submissions", "file-text", 20, "NAV.SDBIP.CAPTURE", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.LIBRARY", "OPMS Target Library", "/opms/library", "library", 30, "NAV.SDBIP.LIBRARY", rootIds["NAV.SDBIP"]),
            Nav("NAV.IPMS.DASHBOARD", "IPMS Dashboard", "/ipms/dashboard", "dashboard", 10, "NAV.IPMS.DASHBOARD", rootIds["NAV.IPMS"]),
            Nav("NAV.IPMS.REGISTER", "IPMS Targets", "/ipms/targets", "target", 20, "NAV.IPMS.REGISTER", rootIds["NAV.IPMS"]),
            Nav("NAV.IPMS.CAPTURE", "IPMS Submissions", "/ipms/submissions", "file-text", 30, "NAV.IPMS.CAPTURE", rootIds["NAV.IPMS"]),
            Nav("NAV.IPMS.LIBRARY", "IPMS Target Library", "/ipms/library", "library", 40, "NAV.IPMS.LIBRARY", rootIds["NAV.IPMS"]),
            Nav("NAV.WORKFLOW.MY_QUEUE", "My Queue", "/workflow/my-queue", "workflow", 10, "NAV.WORKFLOW.MY_QUEUE", rootIds["NAV.WORKFLOW"]),
            Nav("NAV.WORKFLOW.VERIFY", "Verification", "/workflow/verification", "workflow", 20, "NAV.WORKFLOW.VERIFY", rootIds["NAV.WORKFLOW"]),
            Nav("NAV.WORKFLOW.REVIEW", "PMS Review", "/workflow/pms-review", "workflow", 30, "NAV.WORKFLOW.REVIEW", rootIds["NAV.WORKFLOW"]),
            Nav("NAV.WORKFLOW.APPROVE", "Approval", "/workflow/approval", "workflow", 40, "NAV.WORKFLOW.APPROVE", rootIds["NAV.WORKFLOW"]),
            Nav("NAV.WORKFLOW.AUDIT", "Internal Audit", "/workflow/auditor-review", "workflow", 50, "NAV.WORKFLOW.AUDIT", rootIds["NAV.WORKFLOW"]),
            Nav("NAV.IDP.OVERVIEW", "IDP Overview", "/idp/dashboard", "dashboard", 10, "NAV.IDP.OVERVIEW", rootIds["NAV.IDP"]),
            Nav("NAV.IDP.PLANS", "IDP Plans", "/idp/plans", "map", 20, "NAV.IDP.PLANS", rootIds["NAV.IDP"]),
            Nav("NAV.IDP.HIERARCHY", "Planning Hierarchy", "/idp/hierarchy", "layers", 30, "NAV.IDP.HIERARCHY", rootIds["NAV.IDP"]),
            Nav("NAV.IDP.PARTICIPATION", "Community Participation", "/idp/community", "users", 40, "NAV.IDP.PARTICIPATION", rootIds["NAV.IDP"]),
            Nav("NAV.IDP.ALIGNMENT", "Alignment Matrix", "/idp/alignment", "layers", 50, "NAV.IDP.ALIGNMENT", rootIds["NAV.IDP"]),
            Nav("NAV.IDP.REPORTS", "IDP Reports", "/idp/reports", "reports", 60, "NAV.IDP.REPORTS", rootIds["NAV.IDP"]),
            Nav("NAV.RISK.DASHBOARD", "Risk Dashboard", "/risk/dashboard", "shield-alert", 10, "NAV.RISK.DASHBOARD", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.REGISTER", "Risk Register", "/risk/register", "clipboard-list", 20, "NAV.RISK.REGISTER", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.ASSESSMENTS", "Risk Assessments", "/risk/assessments", "file-search", 30, "NAV.RISK.ASSESSMENTS", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.TREATMENTS", "Treatment Plans", "/risk/treatment-plans", "heart-pulse", 40, "NAV.RISK.TREATMENTS", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.REPORTS", "Risk Reports", "/risk/reports", "reports", 50, "NAV.RISK.REPORTS", rootIds["NAV.RISK"]),
            Nav("NAV.C88.PLANNING", "C88 Planning", "/c88/planning", "layers", 10, "NAV.C88.PLANNING", rootIds["NAV.C88"]),
            Nav("NAV.ORGANISATION.EMPLOYEES", "Employees", "/hr/employees", "users", 10, "NAV.ORGANISATION.EMPLOYEES", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.ORGANISATION.DEPARTMENTS", "Departments", "/hr/departments", "users", 20, "NAV.ORGANISATION.DEPARTMENTS", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.ORGANISATION.UNITS", "Units", "/hr/units", "users", 30, "NAV.ORGANISATION.UNITS", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.CONFIGURATION.PERIODS", "Periods", "/admin/periods", "calendar", 10, "NAV.CONFIGURATION.PERIODS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.WORKFLOW", "Workflow Governance", "/admin/approval-setup", "workflow", 20, "NAV.CONFIGURATION.WORKFLOW", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.LOOKUPS", "Lookup Tables", "/admin/lookups", "settings", 30, "NAV.CONFIGURATION.LOOKUPS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.ADMIN.USERS", "Users", "/system-administration/users", "users", 10, "NAV.ADMIN.USERS", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.ROLES", "Roles", "/system-administration/roles", "users-group", 20, "NAV.ADMIN.ROLES", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.SECURITY", "Security", "/system-administration/security", "key", 30, "NAV.ADMIN.SECURITY", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.AUDIT", "Audit Logs", "/system-administration/audit-logs", "history", 40, "NAV.ADMIN.AUDIT", rootIds["NAV.ADMIN"])
        };
        existingCodes = await context.SecurityNavigationItems.Select(item => item.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.SecurityNavigationItems.AddRange(children.Where(item => !existingCodes.Contains(item.Code)));
        await context.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context, IEnumerable<SecurityResource> resources, IEnumerable<SecurityActionDefinition> actions)
    {
        var permissions = new List<Permission>();
        foreach (var resource in resources)
        {
            if (resource.SupportsCreate) permissions.Add(Permission(resource.Code, SecurityOperation.Create));
            if (resource.SupportsRead) permissions.Add(Permission(resource.Code, SecurityOperation.Read));
            if (resource.SupportsUpdate) permissions.Add(Permission(resource.Code, SecurityOperation.Update));
            if (resource.SupportsDelete) permissions.Add(Permission(resource.Code, SecurityOperation.Delete));
            if (resource.SupportsExport) permissions.Add(Permission(resource.Code, SecurityOperation.Export));
            if (resource.SupportsImport) permissions.Add(Permission(resource.Code, SecurityOperation.Import));
        }
        permissions.AddRange(actions.Select(item => new Permission { Module = "Security", Feature = item.ResourceCode, Action = "Execute", Code = item.Code, Description = item.Name, Kind = SecurityPermissionKind.Action, ResourceCode = item.ResourceCode, Operation = SecurityOperation.Execute, ActionCode = item.Code }));
        var navItems = await context.SecurityNavigationItems.AsNoTracking().Where(item => item.RequiredPermissionCode != null).ToListAsync();
        permissions.AddRange(navItems.Select(item => new Permission { Module = "Navigation", Feature = item.Name, Action = "View", Code = item.RequiredPermissionCode!, Description = $"View {item.Name} navigation", Kind = SecurityPermissionKind.Navigation, NavigationCode = item.Code }));
        var members = await context.SecurityMemberDefinitions.AsNoTracking().Where(item => item.IsActive).ToListAsync();
        foreach (var member in members)
        {
            permissions.Add(MemberPermission(member, SecurityOperation.Read));
            if (!member.IsSystemManaged) permissions.Add(MemberPermission(member, SecurityOperation.Update));
        }
        var existing = await context.Permissions.Select(item => item.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.Permissions.AddRange(permissions.GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).Where(item => !existing.Contains(item.Code)));
        await context.SaveChangesAsync();
    }

    private static async Task MigrateLegacyRolePermissionsAsync(ApplicationDbContext context)
    {
        var mappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard.View"] = ["NAV.DASHBOARD", "NAV.PROFILE"],
            ["OPMS.Targets.View"] = ["OPMS_KPI.READ"], ["OPMS.Targets.Create"] = ["OPMS_KPI.CREATE"], ["OPMS.Targets.Edit"] = ["OPMS_KPI.UPDATE"], ["OPMS.Targets.Delete"] = ["OPMS_KPI.DELETE"],
            ["OPMS.View"] = ["NAV.SDBIP.REGISTER"], ["OPMS.Library.View"] = ["NAV.SDBIP.LIBRARY"],
            ["OPMS.Submissions.View"] = ["OPMS_SUBMISSION.READ"], ["OPMS.Submissions.Create"] = ["OPMS_SUBMISSION.CREATE"], ["OPMS.Submissions.Edit"] = ["OPMS_SUBMISSION.UPDATE"], ["OPMS.Submissions.Delete"] = ["OPMS_SUBMISSION.DELETE"],
            ["Workflow.Submit.View"] = ["NAV.SDBIP.CAPTURE", "NAV.WORKFLOW.MY_QUEUE"], ["Workflow.Verify.View"] = ["NAV.WORKFLOW.VERIFY"], ["Workflow.Review.View"] = ["NAV.WORKFLOW.REVIEW"], ["Workflow.Approve.View"] = ["NAV.WORKFLOW.APPROVE"], ["Workflow.Audit.View"] = ["NAV.WORKFLOW.AUDIT"],
            ["OPMS.Submissions.Submit"] = ["OPMS_SUBMISSION.SUBMIT"], ["OPMS.Submissions.Verify"] = ["OPMS_SUBMISSION.VERIFY"], ["OPMS.Submissions.VerifyReject"] = ["OPMS_SUBMISSION.VERIFY_REJECT"],
            ["OPMS.Submissions.Approve"] = ["OPMS_SUBMISSION.APPROVE"], ["OPMS.Submissions.Reject"] = ["OPMS_SUBMISSION.REJECT"], ["OPMS.Submissions.Review"] = ["OPMS_WORKFLOW.PMS_REVIEW"],
            ["OPMS.Submissions.Audit"] = ["OPMS_WORKFLOW.INTERNAL_AUDIT", "OPMS_SUBMISSION.InternalAuditObservation.READ", "OPMS_SUBMISSION.InternalAuditObservation.UPDATE", "OPMS_POE.ASSESS", "OPMS_POE.PLACE_HOLD", "OPMS_POE.RELEASE_HOLD", "OPMS_POE.DISPOSE"],
            ["OPMS.Submissions.Score"] = ["OPMS_WORKFLOW.PMS_REVIEW"], ["OPMS.Submissions.ExtendDueDate"] = ["OPMS_SUBMISSION.EXTEND_DUE_DATE"], ["OPMS.POE.Upload"] = ["OPMS_POE.UPLOAD", "OPMS_POE.REPLACE"],
            ["Actuals.View"] = ["OPMS_SUBMISSION.ActualPerformance.READ", "OPMS_SUBMISSION.Variance.READ", "OPMS_SUBMISSION.SubmittedDate.READ"],
            ["Actuals.Edit"] = ["OPMS_SUBMISSION.ActualPerformance.UPDATE"], ["Actuals.Submit"] = ["OPMS_SUBMISSION.ActualPerformance.UPDATE"],
            ["Departments.View"] = ["DEPARTMENT.READ", "NAV.ORGANISATION.DEPARTMENTS"], ["Departments.Manage"] = ["DEPARTMENT.CREATE", "DEPARTMENT.UPDATE"],
            ["Units.View"] = ["UNIT.READ", "NAV.ORGANISATION.UNITS"], ["Units.Manage"] = ["UNIT.CREATE", "UNIT.UPDATE"],
            ["IPMS.Targets.View"] = ["IPMS_KPI.READ"], ["IPMS.Targets.Create"] = ["IPMS_KPI.CREATE"], ["IPMS.Targets.Edit"] = ["IPMS_KPI.UPDATE"],
            ["IPMS.View"] = ["NAV.IPMS.DASHBOARD", "NAV.IPMS.REGISTER"], ["IPMS.Library.View"] = ["NAV.IPMS.LIBRARY"],
            ["IPMS.Submissions.View"] = ["IPMS_SUBMISSION.READ"], ["IPMS.Submissions.Create"] = ["IPMS_SUBMISSION.CREATE"], ["IPMS.Submissions.Edit"] = ["IPMS_SUBMISSION.UPDATE"],
            ["IDP.Dashboard.View"] = ["NAV.IDP.OVERVIEW"], ["IDP.Plan.View"] = ["NAV.IDP.PLANS"], ["IDP.Hierarchy.Manage"] = ["NAV.IDP.HIERARCHY"], ["IDP.Participation.View"] = ["NAV.IDP.PARTICIPATION"], ["IDP.Alignment.View"] = ["NAV.IDP.ALIGNMENT"], ["IDP.Reports.Generate"] = ["NAV.IDP.REPORTS"], ["IDP.Risk.Manage"] = ["NAV.RISK.DASHBOARD", "NAV.RISK.REGISTER", "NAV.RISK.ASSESSMENTS", "NAV.RISK.TREATMENTS", "NAV.RISK.REPORTS"],
            ["IPMS.Submissions.Submit"] = ["IPMS_SUBMISSION.SUBMIT"], ["IPMS.Submissions.Verify"] = ["IPMS_SUBMISSION.VERIFY"], ["IPMS.Submissions.VerifyReject"] = ["IPMS_SUBMISSION.VERIFY_REJECT"],
            ["IPMS.Submissions.Approve"] = ["IPMS_SUBMISSION.APPROVE"], ["IPMS.Submissions.Reject"] = ["IPMS_SUBMISSION.REJECT"], ["IPMS.Submissions.Review"] = ["IPMS_WORKFLOW.PMS_REVIEW"],
            ["IPMS.Submissions.Audit"] = ["IPMS_WORKFLOW.INTERNAL_AUDIT", "IPMS_SUBMISSION.InternalAuditObservation.READ", "IPMS_SUBMISSION.InternalAuditObservation.UPDATE", "IPMS_POE.ASSESS", "IPMS_POE.PLACE_HOLD", "IPMS_POE.RELEASE_HOLD", "IPMS_POE.DISPOSE"],
            ["IPMS.Submissions.ExtendDueDate"] = ["IPMS_SUBMISSION.EXTEND_DUE_DATE"], ["IPMS.POE.Upload"] = ["IPMS_POE.UPLOAD", "IPMS_POE.REPLACE"],
            ["Configuration.Manage"] = ["WORKFLOW.CONFIGURE", "NAV.CONFIGURATION.PERIODS", "NAV.CONFIGURATION.WORKFLOW", "NAV.CONFIGURATION.LOOKUPS"],
            ["UserDirectory.View"] = ["NAV.ORGANISATION.EMPLOYEES"],
            ["Admin.Users.Manage"] = ["NAV.ADMIN.USERS"], ["Admin.Roles.Manage"] = ["NAV.ADMIN.ROLES"], ["Admin.Permissions.Manage"] = ["NAV.ADMIN.SECURITY"], ["Audit.Logs.View"] = ["NAV.ADMIN.AUDIT"],
            ["Notifications.View"] = ["NAV.NOTIFICATIONS"],
            ["Reports.View"] = ["OPMS_REPORT.READ", "IPMS_REPORT.READ", "NAV.REPORTS"],
            ["Reports.Generate"] = ["OPMS_REPORT.GENERATE", "IPMS_REPORT.GENERATE"],
            ["Reports.Export"] = ["OPMS_REPORT.EXPORT", "IPMS_REPORT.EXPORT"]
        };
        var codes = mappings.Keys.Concat(mappings.Values.SelectMany(value => value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var definitions = await context.Permissions.Where(item => codes.Contains(item.Code)).ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase);
        var current = await context.RolePermissions.AsNoTracking().Where(item => definitions.Values.Select(permission => permission.Id).Contains(item.PermissionId)).ToListAsync();
        var existingKeys = current.Select(item => item.RoleId + "|" + item.PermissionId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var legacy in current.Where(item => mappings.ContainsKey(definitions.Values.First(permission => permission.Id == item.PermissionId).Code)))
        {
            var legacyCode = definitions.Values.First(permission => permission.Id == legacy.PermissionId).Code;
            foreach (var targetCode in mappings[legacyCode])
            {
                if (!definitions.TryGetValue(targetCode, out var target)) continue;
                var key = legacy.RoleId + "|" + target.Id;
                if (!existingKeys.Add(key)) continue;
                context.RolePermissions.Add(new RolePermission { RoleId = legacy.RoleId, PermissionId = target.Id, IsAllowed = legacy.IsAllowed, ScopeType = legacy.ScopeType, IsActive = legacy.IsActive, EffectiveFrom = legacy.EffectiveFrom, EffectiveTo = legacy.EffectiveTo });
            }
        }
        await context.SaveChangesAsync();
    }

    private static SecurityResource Resource(string code, string name, bool create, bool read, bool update, bool delete, bool field, bool criteria) =>
        new() { Code = code, Name = name, SupportsCreate = create, SupportsRead = read, SupportsUpdate = update, SupportsDelete = delete, SupportsExport = read, SupportsImport = create, SupportsFieldSecurity = field, SupportsRecordCriteria = criteria };
    private static SecurityActionDefinition Action(string code, string name, string resource) => new() { Code = code, Name = name, ResourceCode = resource };
    private static SecurityMemberDefinition Member(string resource, string code, string name, bool sensitive = false, bool systemManaged = false) => new() { ResourceCode = resource, MemberCode = code, DisplayName = name, IsSensitive = sensitive, IsSystemManaged = systemManaged };
    private static SecurityNavigationItem Nav(string code, string name, string? route, string icon, int order, string? permission, int? parent = null) => new() { Code = code, Name = name, Route = route, IconKey = icon, DisplayOrder = order, RequiredPermissionCode = permission, ParentId = parent };
    private static Permission Permission(string resource, SecurityOperation operation) => new() { Module = "Resource", Feature = resource, Action = operation.ToString(), Code = $"{resource}.{operation.ToString().ToUpperInvariant()}", Description = $"{operation} {resource}", Kind = SecurityPermissionKind.Resource, ResourceCode = resource, Operation = operation };
    private static Permission MemberPermission(SecurityMemberDefinition member, SecurityOperation operation) => new() { Module = "Member", Feature = member.ResourceCode, Action = operation.ToString(), Code = $"{member.ResourceCode}.{member.MemberCode}.{operation.ToString().ToUpperInvariant()}", Description = $"{operation} {member.DisplayName}", Kind = SecurityPermissionKind.Member, ResourceCode = member.ResourceCode, MemberCode = member.MemberCode, Operation = operation };
}

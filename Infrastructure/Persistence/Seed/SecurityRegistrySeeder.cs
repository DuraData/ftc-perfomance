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
            Resource("WARD", "Ward", true, true, true, false, true, true),
            Resource("VOTE_NUMBER", "Vote Number", true, true, true, false, true, true),
            Resource("EMPLOYEE", "Municipal Employee", true, true, true, false, true, true),
            Resource("USER", "User Account", true, true, true, true, true, true),
            Resource("ROLE", "Security Role", true, true, true, false, true, true),
            Resource("FINANCIAL_YEAR", "Financial Year", true, true, true, false, true, true),
            Resource("REPORTING_PERIOD", "Reporting Period", true, true, true, false, true, true),
            Resource("SDBIP_LAYER", "SDBIP Layer", true, true, true, false, true, true),
            Resource("EMPLOYEE_ASSIGNMENT", "Employee Assignment", true, true, true, false, true, true),
            Resource("IDP_PLAN", "IDP Plan", true, true, true, false, true, true),
            Resource("IDP_DOCUMENT", "IDP Document", true, true, true, false, true, true),
            Resource("IDP_PROJECT", "IDP Project", true, true, true, false, true, true),
            Resource("IDP_INDICATOR", "IDP Indicator", true, true, true, false, true, true),
            Resource("IDP_STAKEHOLDER", "IDP Stakeholder Engagement", true, true, true, false, true, true),
            Resource("TID", "Technical Indicator Definition", true, true, true, false, true, true),
            Resource("STRATEGIC_DOCUMENT", "Strategic Document", true, true, true, false, true, true),
            Resource("STRATEGIC_RISK", "Strategic Risk", true, true, true, false, false, true),
            Resource("NATIONAL_KPA", "National Key Performance Area", true, true, true, false, true, false),
            Resource("BACK_TO_BASICS_PILLAR", "Back-to-Basics Pillar", true, true, true, false, true, false),
            Resource("MUNICIPAL_KPA", "Municipal Key Performance Area", true, true, true, false, true, true),
            Resource("STRATEGIC_GOAL", "Strategic Goal", true, true, true, false, true, true),
            Resource("STRATEGIC_INTERVENTION", "Strategic Intervention", true, true, true, false, true, true),
            Resource("STRATEGIC_OBJECTIVE", "Strategic Objective", true, true, true, false, true, true),
            Resource("PERFORMANCE_OBJECTIVE", "Performance Objective", true, true, true, false, true, true),
            Resource("BUDGET_SOURCE", "Budget Source", true, true, true, false, true, true),
            Resource("BUDGET_TYPE", "Budget Type", true, true, true, false, true, true),
            Resource("KPI_TYPE", "KPI Type", true, true, true, false, true, true),
            Resource("INDICATOR_TYPE", "Indicator Type", true, true, true, false, true, true),
            Resource("FUNCTIONAL_AREA", "Functional Area", true, true, true, false, true, true),
            Resource("STANDARD_CLASSIFICATION", "Standard Classification", true, true, true, false, true, true),
            Resource("KPI_UNIT_OF_MEASURE", "KPI Unit of Measure", true, true, true, false, true, true),
            Resource("STRATEGIC_HIERARCHY", "Strategic Planning Relationships", true, true, true, false, false, true),
            Resource("AUTHENTICATION", "Authentication Configuration", true, true, true, false, true, true),
            Resource("LOGIN_AUDIT", "Login Audit History", false, true, false, false, true, true),
            Resource("AUDIT_TRAIL", "Business and Security Audit Trail", false, true, false, false, true, true),
            Resource("NOTIFICATION_DELIVERY", "Notification Delivery Operations", false, true, false, false, true, true),
            Resource("C88_INDICATOR", "Circular 88 Indicator", true, true, true, false, true, true),
            Resource("C88_REPORT", "Circular 88 Report", true, true, true, false, true, true)
        };
        var existingResources = await context.SecurityResources.ToListAsync();
        var existingResourceCodes = existingResources.Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        context.SecurityResources.AddRange(resources.Where(item => !existingResourceCodes.Contains(item.Code)));
        var existingUserResource = existingResources.SingleOrDefault(item => string.Equals(item.Code, "USER", StringComparison.OrdinalIgnoreCase));
        if (existingUserResource != null) existingUserResource.SupportsDelete = true;
        var existingAuthenticationResource = existingResources.SingleOrDefault(item => string.Equals(item.Code, "AUTHENTICATION", StringComparison.OrdinalIgnoreCase));
        if (existingAuthenticationResource != null) existingAuthenticationResource.SupportsFieldSecurity = true;
        var existingLoginAuditResource = existingResources.SingleOrDefault(item => string.Equals(item.Code, "LOGIN_AUDIT", StringComparison.OrdinalIgnoreCase));
        if (existingLoginAuditResource != null) existingLoginAuditResource.SupportsFieldSecurity = true;
        var existingAuditTrailResource = existingResources.SingleOrDefault(item => string.Equals(item.Code, "AUDIT_TRAIL", StringComparison.OrdinalIgnoreCase));
        if (existingAuditTrailResource != null) existingAuditTrailResource.SupportsFieldSecurity = true;
        var existingNotificationDeliveryResource = existingResources.SingleOrDefault(item => string.Equals(item.Code, "NOTIFICATION_DELIVERY", StringComparison.OrdinalIgnoreCase));
        if (existingNotificationDeliveryResource != null) existingNotificationDeliveryResource.SupportsFieldSecurity = true;

        var actions = new[]
        {
            Action("OPMS_KPI.ACTIVATE", "Activate KPI", "OPMS_KPI"), Action("OPMS_KPI.WITHDRAW", "Withdraw KPI", "OPMS_KPI"), Action("OPMS_KPI.REVISE", "Revise KPI", "OPMS_KPI"),
            Action("OPMS_KPI.IMPORT", "Import and reconcile SDBIP KPIs", "OPMS_KPI"),
            Action("OPMS_KPI.NORMALIZE_LEGACY", "Normalize Legacy Target Values", "OPMS_KPI"),
            Action("OPMS_KPI.CONFIGURE_CONSOLIDATION", "Configure Performance Consolidation", "OPMS_KPI"),
            Action("IPMS_KPI.WITHDRAW", "Withdraw KPI", "IPMS_KPI"), Action("IPMS_KPI.REVISE", "Revise KPI", "IPMS_KPI"),
            Action("OPMS_SUBMISSION.SAVE", "Save Submission", "OPMS_SUBMISSION"), Action("OPMS_SUBMISSION.SUBMIT", "Submit", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.WITHDRAW", "Withdraw Submission", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.VERIFY", "Verify", "OPMS_SUBMISSION"), Action("OPMS_SUBMISSION.VERIFY_REJECT", "Reject Verification", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.APPROVE", "Approve", "OPMS_SUBMISSION"), Action("OPMS_SUBMISSION.REJECT", "Reject Approval", "OPMS_SUBMISSION"),
            Action("OPMS_SUBMISSION.EXTEND_DUE_DATE", "Extend Due Date", "OPMS_SUBMISSION"),
            Action("OPMS_WORKFLOW.PMS_REVIEW", "PMS Review", "OPMS_WORKFLOW"), Action("OPMS_WORKFLOW.INTERNAL_AUDIT", "Internal Audit Assess", "OPMS_WORKFLOW"),
            Action("OPMS_RFI.RAISE", "Raise RFI", "OPMS_RFI"), Action("OPMS_RFI.RESPOND", "Respond to RFI", "OPMS_RFI"), Action("OPMS_RFI.CLOSE", "Close RFI", "OPMS_RFI"),
            Action("OPMS_POE.UPLOAD", "Upload Evidence", "OPMS_POE"), Action("OPMS_POE.REPLACE", "Replace Evidence", "OPMS_POE"), Action("OPMS_POE.ASSESS", "Assess Evidence", "OPMS_POE"), Action("OPMS_POE.PLACE_HOLD", "Place Evidence Legal Hold", "OPMS_POE"), Action("OPMS_POE.RELEASE_HOLD", "Release Evidence Legal Hold", "OPMS_POE"), Action("OPMS_POE.DISPOSE", "Dispose Retained Evidence", "OPMS_POE"),
            Action("OPMS_REPORT.GENERATE", "Generate Report", "OPMS_REPORT"), Action("OPMS_REPORT.EXPORT", "Export Report", "OPMS_REPORT"), Action("OPMS_REPORT.CONFIGURE", "Configure Official Report Templates", "OPMS_REPORT"),
            Action("IPMS_REPORT.GENERATE", "Generate Report", "IPMS_REPORT"), Action("IPMS_REPORT.EXPORT", "Export Report", "IPMS_REPORT"), Action("IPMS_REPORT.CONFIGURE", "Configure Official Report Templates", "IPMS_REPORT"),
            Action("IPMS_SUBMISSION.SUBMIT", "Submit", "IPMS_SUBMISSION"), Action("IPMS_SUBMISSION.WITHDRAW", "Withdraw Submission", "IPMS_SUBMISSION"), Action("IPMS_SUBMISSION.VERIFY", "Verify", "IPMS_SUBMISSION"),
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
            Action("AUTHENTICATION.CONFIGURE", "Configure Authentication", "AUTHENTICATION"),
            Action("AUTHENTICATION.LINK_IDENTITIES", "Link Enterprise Identities", "AUTHENTICATION"),
            Action("AUTHENTICATION.VIEW_EVENTS", "View Authentication Events", "AUTHENTICATION"),
            Action("WORKFLOW.CONFIGURE", "Configure Workflow and Reporting Windows", "OPMS_WORKFLOW"),
            Action("NOTIFICATION_DELIVERY.RETRY", "Retry Notification Delivery", "NOTIFICATION_DELIVERY"),
            Action("TID.CONFIGURE", "Configure TID Policy", "TID"), Action("TID.UPLOAD_SOURCE", "Upload TID Source Document", "TID"),
            Action("IDP_DOCUMENT.RESCAN", "Rescan IDP Document", "IDP_DOCUMENT"),
            Action("STRATEGIC_DOCUMENT.MANAGE_TYPES", "Manage Strategic Document Types", "STRATEGIC_DOCUMENT"),
            Action("STRATEGIC_DOCUMENT.APPROVE", "Approve Strategic Document", "STRATEGIC_DOCUMENT"),
            Action("STRATEGIC_DOCUMENT.PUBLISH", "Publish Strategic Document", "STRATEGIC_DOCUMENT"),
            Action("STRATEGIC_DOCUMENT.RETIRE", "Retire Strategic Document", "STRATEGIC_DOCUMENT"),
            Action("STRATEGIC_DOCUMENT.RESCAN", "Rescan Strategic Document", "STRATEGIC_DOCUMENT"),
            Action("STRATEGIC_RISK.LINK_KPI", "Link Strategic Risk to KPI", "STRATEGIC_RISK"),
            Action("STRATEGIC_RISK.UNLINK_KPI", "Unlink Strategic Risk from KPI", "STRATEGIC_RISK"),
            Action("C88_INDICATOR.CONFIGURE", "Configure Circular 88", "C88_INDICATOR"), Action("C88_INDICATOR.MANAGE_CATALOGUE", "Manage Circular 88 Catalogue", "C88_INDICATOR"),
            Action("C88_INDICATOR.MANAGE_ASSIGNMENTS", "Manage Circular 88 Assignments", "C88_INDICATOR"), Action("C88_INDICATOR.MANAGE_WORKFLOW", "Manage Circular 88 Workflow", "C88_INDICATOR"),
            Action("C88_INDICATOR.MANAGE_MAPPING", "Manage OPMS to Circular 88 Mappings", "C88_INDICATOR"),
            Action("C88_REPORT.SUBMIT", "Submit C88 Report", "C88_REPORT"), Action("C88_REPORT.VERIFY", "Verify C88 Report", "C88_REPORT"),
            Action("C88_REPORT.RETURN", "Return C88 Report", "C88_REPORT"), Action("C88_REPORT.FINAL_SUBMIT", "Final Submit C88 Report", "C88_REPORT")
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
            Member("OPMS_SUBMISSION", "VarianceReason", "Variance Reason"),
            Member("OPMS_SUBMISSION", "CorrectiveMeasure", "Corrective Measure"),
            Member("OPMS_SUBMISSION", "SubmittedDate", "Submitted Date", systemManaged: true),
            Member("OPMS_SUBMISSION", "InternalAuditObservation", "Internal Audit Observation", sensitive: true),
            Member("IPMS_SUBMISSION", "ActualPerformance", "Actual Performance"),
            Member("IPMS_SUBMISSION", "Variance", "Variance", systemManaged: true),
            Member("IPMS_SUBMISSION", "VarianceReason", "Variance Reason"),
            Member("IPMS_SUBMISSION", "CorrectiveMeasure", "Corrective Measure"),
            Member("IPMS_SUBMISSION", "SubmittedDate", "Submitted Date", systemManaged: true),
            Member("IPMS_SUBMISSION", "InternalAuditObservation", "Internal Audit Observation", sensitive: true),
            Member("OPMS_POE", "UploadedByUserId", "Evidence Uploader Identity", sensitive: true, systemManaged: true),
            Member("OPMS_POE", "UploadedByName", "Evidence Uploader Name", sensitive: true, systemManaged: true),
            Member("OPMS_POE", "ScannerProvider", "Evidence Scanner Provider", sensitive: true, systemManaged: true),
            Member("OPMS_POE", "ScannerReference", "Evidence Scanner Reference", sensitive: true, systemManaged: true),
            Member("OPMS_POE", "ScanDetail", "Evidence Scanner Detail", sensitive: true, systemManaged: true),
            Member("IPMS_POE", "UploadedByUserId", "Evidence Uploader Identity", sensitive: true, systemManaged: true),
            Member("IPMS_POE", "UploadedByName", "Evidence Uploader Name", sensitive: true, systemManaged: true),
            Member("IPMS_POE", "ScannerProvider", "Evidence Scanner Provider", sensitive: true, systemManaged: true),
            Member("IPMS_POE", "ScannerReference", "Evidence Scanner Reference", sensitive: true, systemManaged: true),
            Member("IPMS_POE", "ScanDetail", "Evidence Scanner Detail", sensitive: true, systemManaged: true),
            Member("IDP_DOCUMENT", "UploadedByUserId", "Document Uploader Identity", sensitive: true, systemManaged: true),
            Member("IDP_DOCUMENT", "UploadedByName", "Document Uploader Name", sensitive: true, systemManaged: true),
            Member("IDP_DOCUMENT", "ScannerProvider", "Document Scanner Provider", sensitive: true, systemManaged: true),
            Member("IDP_DOCUMENT", "ScannerReference", "Document Scanner Reference", sensitive: true, systemManaged: true),
            Member("IDP_DOCUMENT", "ScanDetail", "Document Scanner Detail", sensitive: true, systemManaged: true),
            Member("EMPLOYEE", "EmployeeNumber", "Employee Number", sensitive: true),
            Member("EMPLOYEE", "SalaryReference", "Salary Reference", sensitive: true),
            Member("EMPLOYEE", "EmailAddress", "Email Address", sensitive: true),
            Member("EMPLOYEE", "IdentityUserId", "Linked Login", sensitive: true),
            Member("USER", "Email", "Email Address", sensitive: true),
            Member("USER", "PhoneNumber", "Phone Number", sensitive: true),
            Member("AUTHENTICATION", "UserEmail", "Linked User Email", sensitive: true, systemManaged: true),
            Member("AUTHENTICATION", "ExpectedEmail", "Expected Enterprise Email", sensitive: true),
            Member("AUTHENTICATION", "Issuer", "External Identity Issuer", sensitive: true),
            Member("AUTHENTICATION", "Subject", "External Identity Subject", sensitive: true),
            Member("AUTHENTICATION", "EventUserId", "Authentication Event User", sensitive: true, systemManaged: true),
            Member("AUTHENTICATION", "EventIpAddress", "Authentication Event IP Address", sensitive: true, systemManaged: true),
            Member("IDP_STAKEHOLDER", "ContactPerson", "Stakeholder Contact Person", sensitive: true),
            Member("IDP_STAKEHOLDER", "ContactEmail", "Stakeholder Contact Email", sensitive: true),
            Member("LOGIN_AUDIT", "UserId", "Login User Identity", sensitive: true, systemManaged: true),
            Member("LOGIN_AUDIT", "Email", "Login Email Address", sensitive: true, systemManaged: true),
            Member("LOGIN_AUDIT", "IpAddress", "Login IP Address", sensitive: true, systemManaged: true),
            Member("LOGIN_AUDIT", "UserAgent", "Login User Agent", sensitive: true, systemManaged: true),
            Member("LOGIN_AUDIT", "FailureReason", "Login Failure Detail", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "EntityId", "Audited Entity Identity", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "OldValue", "Previous Value Snapshot", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "NewValue", "New Value Snapshot", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "ChangedBy", "Audit Actor Identity", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "IpAddress", "Audit IP Address", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "CorrelationId", "Audit Correlation Identity", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "Reason", "Audit Governance Reason", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "UserAgent", "Audit User Agent", sensitive: true, systemManaged: true),
            Member("AUDIT_TRAIL", "SessionId", "Audit Session Identity", sensitive: true, systemManaged: true),
            Member("NOTIFICATION_DELIVERY", "AggregateId", "Notification Record Identity", sensitive: true, systemManaged: true),
            Member("NOTIFICATION_DELIVERY", "LastError", "Notification Queue Failure Detail", sensitive: true, systemManaged: true),
            Member("NOTIFICATION_DELIVERY", "RecipientUserId", "Notification Recipient Identity", sensitive: true, systemManaged: true),
            Member("NOTIFICATION_DELIVERY", "ProviderReference", "Provider Receipt Reference", sensitive: true, systemManaged: true),
            Member("NOTIFICATION_DELIVERY", "Error", "Delivery Failure Detail", sensitive: true, systemManaged: true),
            Member("NOTIFICATION_DELIVERY", "ResponseDetail", "Provider Response Detail", sensitive: true, systemManaged: true)
        };
        var existingMembers = await context.SecurityMemberDefinitions.ToListAsync();
        var existing = existingMembers.Select(item => item.ResourceCode + "|" + item.MemberCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        context.SecurityMemberDefinitions.AddRange(members.Where(item => !existing.Contains(item.ResourceCode + "|" + item.MemberCode)));
        foreach (var definition in existingMembers.Where(item =>
                     string.Equals(item.ResourceCode, "USER", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "AUTHENTICATION", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "LOGIN_AUDIT", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "AUDIT_TRAIL", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "NOTIFICATION_DELIVERY", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "OPMS_POE", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "IPMS_POE", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "IDP_DOCUMENT", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "IDP_STAKEHOLDER", StringComparison.OrdinalIgnoreCase)
                        && new[] { "ContactPerson", "ContactEmail" }.Contains(item.MemberCode, StringComparer.OrdinalIgnoreCase)
                     || string.Equals(item.ResourceCode, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
                        && new[] { "EmployeeNumber", "SalaryReference", "EmailAddress", "IdentityUserId" }
                            .Contains(item.MemberCode, StringComparer.OrdinalIgnoreCase)))
            definition.IsSensitive = true;
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
            Nav("NAV.REPORTS", "Reports", "/reports", "reports", 60, "NAV.REPORTS"),
            Nav("NAV.STRATEGIC_DOCUMENTS", "Strategic Documents", "/strategic-documents", "file-text", 65, "NAV.STRATEGIC_DOCUMENTS"),
            Nav("NAV.IDP", "IDP", null, "map", 70, null),
            Nav("NAV.RISK", "Risk Management", null, "shield-alert", 80, null), Nav("NAV.C88", "Circular 88", null, "layers", 90, null),
            Nav("NAV.ORGANISATION", "Organisation", null, "users", 100, null), Nav("NAV.CONFIGURATION", "Configuration", null, "settings", 110, null),
            Nav("NAV.ADMIN", "Administration", null, "settings", 120, null), Nav("NAV.NOTIFICATIONS", "Notifications", "/notifications", "bell", 130, "NAV.NOTIFICATIONS"),
            Nav("NAV.PROFILE", "My Profile", "/my-profile", "user", 140, "NAV.PROFILE"),
            Nav("NAV.SETTINGS", "Settings", "/settings", "settings", 150, "NAV.SETTINGS")
        };
        var existingCodes = await context.SecurityNavigationItems.Select(item => item.Code).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        context.SecurityNavigationItems.AddRange(roots.Where(item => !existingCodes.Contains(item.Code)));
        await context.SaveChangesAsync();

        var rootIds = await context.SecurityNavigationItems.Where(item => roots.Select(root => root.Code).Contains(item.Code)).ToDictionaryAsync(item => item.Code, item => item.Id);
        var children = new[]
        {
            Nav("NAV.SDBIP.REGISTER", "SDBIP Register", "/opms/targets", "target", 10, "NAV.SDBIP.REGISTER", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.IMPORT", "SDBIP Import", "/opms/import", "file-text", 15, "NAV.SDBIP.IMPORT", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.CAPTURE", "Performance Reporting", "/opms/submissions", "file-text", 20, "NAV.SDBIP.CAPTURE", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.LIBRARY", "OPMS Target Library", "/opms/library", "library", 30, "NAV.SDBIP.LIBRARY", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.VOTE_NUMBERS", "Vote Numbers", "/opms/vote-numbers", "layers", 40, "NAV.SDBIP.VOTE_NUMBERS", rootIds["NAV.SDBIP"]),
            Nav("NAV.SDBIP.TIDS", "Technical Indicator Descriptions", "/opms/tids", "file-search", 50, "NAV.SDBIP.TIDS", rootIds["NAV.SDBIP"]),
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
            Nav("NAV.IDP.DOCUMENTS", "IDP Documents", "/idp/documents", "file-text", 60, "NAV.IDP.DOCUMENTS", rootIds["NAV.IDP"]),
            Nav("NAV.IDP.REPORTS", "IDP Reports", "/idp/reports", "reports", 70, "NAV.IDP.REPORTS", rootIds["NAV.IDP"]),
            Nav("NAV.RISK.DASHBOARD", "Risk Dashboard", "/risk/dashboard", "shield-alert", 10, "NAV.RISK.DASHBOARD", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.REGISTER", "Risk Register", "/risk/register", "clipboard-list", 20, "NAV.RISK.REGISTER", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.ASSESSMENTS", "Risk Assessments", "/risk/assessments", "file-search", 30, "NAV.RISK.ASSESSMENTS", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.TREATMENTS", "Treatment Plans", "/risk/treatment-plans", "heart-pulse", 40, "NAV.RISK.TREATMENTS", rootIds["NAV.RISK"]),
            Nav("NAV.RISK.REPORTS", "Risk Reports", "/risk/reports", "reports", 50, "NAV.RISK.REPORTS", rootIds["NAV.RISK"]),
            Nav("NAV.C88.PLANNING", "C88 Planning", "/c88/planning", "layers", 10, "NAV.C88.PLANNING", rootIds["NAV.C88"]),
            Nav("NAV.C88.REPORTING", "C88 Reporting", "/c88/reporting", "file-text", 20, "NAV.C88.REPORTING", rootIds["NAV.C88"]),
            Nav("NAV.C88.COMPLIANCE", "C88 Compliance", "/c88/compliance", "check-circle", 30, "NAV.C88.COMPLIANCE", rootIds["NAV.C88"]),
            Nav("NAV.C88.MAPPING", "C88 Mapping", "/c88/mapping", "link", 40, "NAV.C88.MAPPING", rootIds["NAV.C88"]),
            Nav("NAV.C88.REPORTS", "C88 Reports", "/c88/reports", "reports", 50, "NAV.C88.REPORTS", rootIds["NAV.C88"]),
            Nav("NAV.ORGANISATION.EMPLOYEES", "Employees", "/hr/employees", "users", 10, "NAV.ORGANISATION.EMPLOYEES", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.ORGANISATION.DEPARTMENTS", "Departments", "/hr/departments", "users", 20, "NAV.ORGANISATION.DEPARTMENTS", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.ORGANISATION.UNITS", "Units", "/hr/units", "users", 30, "NAV.ORGANISATION.UNITS", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.ORGANISATION.POSITIONS", "Positions", "/hr/positions", "users", 40, "NAV.ORGANISATION.POSITIONS", rootIds["NAV.ORGANISATION"]),
            Nav("NAV.CONFIGURATION.PERIODS", "Periods", "/admin/periods", "calendar", 10, "NAV.CONFIGURATION.PERIODS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.WORKFLOW", "Workflow Governance", "/admin/approval-setup", "workflow", 20, "NAV.CONFIGURATION.WORKFLOW", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.LOOKUPS", "Lookup Tables", "/admin/lookups", "settings", 30, "NAV.CONFIGURATION.LOOKUPS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.WARDS", "Wards", "/admin/wards", "map", 40, "NAV.CONFIGURATION.WARDS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.NATIONAL_KPAS", "National KPAs", "/admin/kpas", "layers", 50, "NAV.CONFIGURATION.NATIONAL_KPAS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.BACK_TO_BASICS", "Back-to-Basics Pillars", "/admin/back-to-basics-pillars", "layers", 60, "NAV.CONFIGURATION.BACK_TO_BASICS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.MUNICIPAL_KPAS", "Municipal KPAs", "/admin/municipal-kpas", "layers", 70, "NAV.CONFIGURATION.MUNICIPAL_KPAS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.STRATEGIC_GOALS", "Strategic Goals", "/admin/strategic-goals", "target", 80, "NAV.CONFIGURATION.STRATEGIC_GOALS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.STRATEGIC_INTERVENTIONS", "Strategic Interventions", "/admin/strategic-interventions", "workflow", 90, "NAV.CONFIGURATION.STRATEGIC_INTERVENTIONS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.STRATEGIC_OBJECTIVES", "Strategic Objectives", "/admin/strategic-objectives", "target", 100, "NAV.CONFIGURATION.STRATEGIC_OBJECTIVES", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.PERFORMANCE_OBJECTIVES", "Performance Objectives", "/admin/performance-objectives", "target", 110, "NAV.CONFIGURATION.PERFORMANCE_OBJECTIVES", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.BUDGET_SOURCES", "Budget Sources", "/admin/budget-sources", "coins", 120, "NAV.CONFIGURATION.BUDGET_SOURCES", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.BUDGET_TYPES", "Budget Types", "/admin/budget-types", "wallet", 130, "NAV.CONFIGURATION.BUDGET_TYPES", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.KPI_TYPES", "KPI Types", "/admin/kpi-types", "list", 140, "NAV.CONFIGURATION.KPI_TYPES", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.INDICATOR_TYPES", "Indicator Types", "/admin/indicator-types", "list", 150, "NAV.CONFIGURATION.INDICATOR_TYPES", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.FUNCTIONAL_AREAS", "Functional Areas", "/admin/functional-areas", "layers", 160, "NAV.CONFIGURATION.FUNCTIONAL_AREAS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.STANDARD_CLASSIFICATIONS", "Standard Classifications", "/admin/standard-classifications", "layers", 170, "NAV.CONFIGURATION.STANDARD_CLASSIFICATIONS", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.CONFIGURATION.KPI_UNITS_OF_MEASURE", "KPI Units of Measure", "/admin/units-measure", "ruler", 180, "NAV.CONFIGURATION.KPI_UNITS_OF_MEASURE", rootIds["NAV.CONFIGURATION"]),
            Nav("NAV.ADMIN.USERS", "Users", "/system-administration/users", "users", 10, "NAV.ADMIN.USERS", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.ROLES", "Roles", "/system-administration/roles", "users-group", 20, "NAV.ADMIN.ROLES", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.SECURITY", "Security", "/system-administration/security", "key", 30, "NAV.ADMIN.SECURITY", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.AUTHENTICATION", "Authentication", "/system-administration/authentication", "shield", 40, "AUTHENTICATION.READ", rootIds["NAV.ADMIN"]),
            Nav("NAV.ADMIN.AUDIT", "Audit Logs", "/system-administration/audit-logs", "history", 50, "NAV.ADMIN.AUDIT", rootIds["NAV.ADMIN"])
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
            ["Dashboard.View"] = ["NAV.DASHBOARD", "NAV.PROFILE", "NAV.SETTINGS"],
            ["OPMS.Targets.View"] = ["OPMS_KPI.READ", "SDBIP_LAYER.READ", "WARD.READ", "VOTE_NUMBER.READ"], ["OPMS.Targets.Create"] = ["OPMS_KPI.CREATE", "OPMS_KPI.IMPORT"], ["OPMS.Targets.Edit"] = ["OPMS_KPI.UPDATE"], ["OPMS.Targets.Delete"] = ["OPMS_KPI.WITHDRAW"],
            ["OPMS.View"] = ["NAV.SDBIP.REGISTER", "NAV.SDBIP.IMPORT", "NAV.SDBIP.VOTE_NUMBERS", "SDBIP_LAYER.READ", "VOTE_NUMBER.READ", "TID.READ", "NAV.SDBIP.TIDS", "STRATEGIC_DOCUMENT.READ", "NAV.STRATEGIC_DOCUMENTS"], ["OPMS.Library.View"] = ["NAV.SDBIP.LIBRARY"],
            ["OPMS.Submissions.View"] = ["OPMS_SUBMISSION.READ", "OPMS_SUBMISSION.VarianceReason.READ", "OPMS_SUBMISSION.CorrectiveMeasure.READ"], ["OPMS.Submissions.Create"] = ["OPMS_SUBMISSION.CREATE"], ["OPMS.Submissions.Edit"] = ["OPMS_SUBMISSION.UPDATE", "OPMS_SUBMISSION.VarianceReason.UPDATE", "OPMS_SUBMISSION.CorrectiveMeasure.UPDATE"], ["OPMS.Submissions.Delete"] = ["OPMS_SUBMISSION.WITHDRAW"],
            ["Workflow.Submit.View"] = ["NAV.SDBIP.CAPTURE", "NAV.WORKFLOW.MY_QUEUE"], ["Workflow.Verify.View"] = ["NAV.WORKFLOW.VERIFY"], ["Workflow.Review.View"] = ["NAV.WORKFLOW.REVIEW"], ["Workflow.Approve.View"] = ["NAV.WORKFLOW.APPROVE"], ["Workflow.Audit.View"] = ["NAV.WORKFLOW.AUDIT"],
            ["OPMS.Submissions.Submit"] = ["OPMS_SUBMISSION.SUBMIT"], ["OPMS.Submissions.Verify"] = ["OPMS_SUBMISSION.VERIFY"], ["OPMS.Submissions.VerifyReject"] = ["OPMS_SUBMISSION.VERIFY_REJECT"],
            ["OPMS.Submissions.Approve"] = ["OPMS_SUBMISSION.APPROVE"], ["OPMS.Submissions.Reject"] = ["OPMS_SUBMISSION.REJECT"], ["OPMS.Submissions.Review"] = ["OPMS_WORKFLOW.PMS_REVIEW"],
            ["OPMS.Submissions.Audit"] = ["OPMS_WORKFLOW.INTERNAL_AUDIT", "OPMS_SUBMISSION.InternalAuditObservation.READ", "OPMS_SUBMISSION.InternalAuditObservation.UPDATE", "OPMS_POE.ASSESS", "OPMS_POE.PLACE_HOLD", "OPMS_POE.RELEASE_HOLD", "OPMS_POE.DISPOSE"],
            ["OPMS.Submissions.Score"] = ["OPMS_WORKFLOW.PMS_REVIEW"], ["OPMS.Submissions.ExtendDueDate"] = ["OPMS_SUBMISSION.EXTEND_DUE_DATE"], ["OPMS.POE.Upload"] = ["OPMS_POE.UPLOAD", "OPMS_POE.REPLACE"],
            ["Actuals.View"] = ["OPMS_SUBMISSION.ActualPerformance.READ", "OPMS_SUBMISSION.Variance.READ", "OPMS_SUBMISSION.SubmittedDate.READ"],
            ["Actuals.Edit"] = ["OPMS_SUBMISSION.ActualPerformance.UPDATE"], ["Actuals.Submit"] = ["OPMS_SUBMISSION.ActualPerformance.UPDATE"],
            ["Departments.View"] = ["DEPARTMENT.READ", "NAV.ORGANISATION.DEPARTMENTS"], ["Departments.Manage"] = ["DEPARTMENT.CREATE", "DEPARTMENT.UPDATE"],
            ["Units.View"] = ["UNIT.READ", "NAV.ORGANISATION.UNITS", "POSITION.READ", "NAV.ORGANISATION.POSITIONS"], ["Units.Manage"] = ["UNIT.CREATE", "UNIT.UPDATE", "POSITION.CREATE", "POSITION.UPDATE"],
            ["IPMS.Targets.View"] = ["IPMS_KPI.READ"], ["IPMS.Targets.Create"] = ["IPMS_KPI.CREATE"], ["IPMS.Targets.Edit"] = ["IPMS_KPI.UPDATE"], ["IPMS.Targets.Delete"] = ["IPMS_KPI.WITHDRAW"],
            ["IPMS.View"] = ["NAV.IPMS.DASHBOARD", "NAV.IPMS.REGISTER"], ["IPMS.Library.View"] = ["NAV.IPMS.LIBRARY"],
            ["IPMS.Submissions.View"] = ["IPMS_SUBMISSION.READ", "IPMS_SUBMISSION.VarianceReason.READ", "IPMS_SUBMISSION.CorrectiveMeasure.READ"], ["IPMS.Submissions.Create"] = ["IPMS_SUBMISSION.CREATE"], ["IPMS.Submissions.Edit"] = ["IPMS_SUBMISSION.UPDATE", "IPMS_SUBMISSION.VarianceReason.UPDATE", "IPMS_SUBMISSION.CorrectiveMeasure.UPDATE"], ["IPMS.Submissions.Delete"] = ["IPMS_SUBMISSION.WITHDRAW"],
            ["IDP.Dashboard.View"] = ["NAV.IDP.OVERVIEW"], ["IDP.Plan.View"] = ["NAV.IDP.PLANS"], ["IDP.Hierarchy.Manage"] = ["NAV.IDP.HIERARCHY", "IDP_PLAN.IMPORT"], ["IDP.Participation.View"] = ["NAV.IDP.PARTICIPATION", "IDP_STAKEHOLDER.READ", "IDP_STAKEHOLDER.ContactPerson.READ", "IDP_STAKEHOLDER.ContactEmail.READ"], ["IDP.Participation.Manage"] = ["IDP_STAKEHOLDER.CREATE", "IDP_STAKEHOLDER.READ", "IDP_STAKEHOLDER.UPDATE", "IDP_STAKEHOLDER.ContactPerson.READ", "IDP_STAKEHOLDER.ContactPerson.UPDATE", "IDP_STAKEHOLDER.ContactEmail.READ", "IDP_STAKEHOLDER.ContactEmail.UPDATE"], ["IDP.Alignment.View"] = ["NAV.IDP.ALIGNMENT"], ["IDP.Documents.Manage"] = ["NAV.IDP.DOCUMENTS", "IDP_DOCUMENT.READ", "IDP_DOCUMENT.CREATE", "IDP_DOCUMENT.UPDATE", "IDP_DOCUMENT.RESCAN", "IDP_DOCUMENT.UploadedByUserId.READ", "IDP_DOCUMENT.UploadedByName.READ", "IDP_DOCUMENT.ScannerProvider.READ", "IDP_DOCUMENT.ScannerReference.READ", "IDP_DOCUMENT.ScanDetail.READ"], ["IDP.Reports.Generate"] = ["NAV.IDP.REPORTS"], ["IDP.Risk.Manage"] = ["NAV.RISK.DASHBOARD", "NAV.RISK.REGISTER", "NAV.RISK.ASSESSMENTS", "NAV.RISK.TREATMENTS", "NAV.RISK.REPORTS", "FINANCIAL_YEAR.READ", "STRATEGIC_RISK.READ", "STRATEGIC_RISK.CREATE", "STRATEGIC_RISK.UPDATE", "STRATEGIC_RISK.LINK_KPI", "STRATEGIC_RISK.UNLINK_KPI"],
            ["IPMS.Submissions.Submit"] = ["IPMS_SUBMISSION.SUBMIT"], ["IPMS.Submissions.Verify"] = ["IPMS_SUBMISSION.VERIFY"], ["IPMS.Submissions.VerifyReject"] = ["IPMS_SUBMISSION.VERIFY_REJECT"],
            ["IPMS.Submissions.Approve"] = ["IPMS_SUBMISSION.APPROVE"], ["IPMS.Submissions.Reject"] = ["IPMS_SUBMISSION.REJECT"], ["IPMS.Submissions.Review"] = ["IPMS_WORKFLOW.PMS_REVIEW"],
            ["IPMS.Submissions.Audit"] = ["IPMS_WORKFLOW.INTERNAL_AUDIT", "IPMS_SUBMISSION.InternalAuditObservation.READ", "IPMS_SUBMISSION.InternalAuditObservation.UPDATE", "IPMS_POE.ASSESS", "IPMS_POE.PLACE_HOLD", "IPMS_POE.RELEASE_HOLD", "IPMS_POE.DISPOSE"],
            ["IPMS.Submissions.ExtendDueDate"] = ["IPMS_SUBMISSION.EXTEND_DUE_DATE"], ["IPMS.POE.Upload"] = ["IPMS_POE.UPLOAD", "IPMS_POE.REPLACE"],
            ["Configuration.Manage"] = ["WORKFLOW.CONFIGURE", "OPMS_REPORT.CONFIGURE", "IPMS_REPORT.CONFIGURE", "OPMS_KPI.NORMALIZE_LEGACY", "SDBIP_LAYER.READ", "SDBIP_LAYER.CREATE", "SDBIP_LAYER.UPDATE", "TID.READ", "TID.CREATE", "TID.UPDATE", "TID.CONFIGURE", "TID.UPLOAD_SOURCE", "NAV.SDBIP.TIDS", "STRATEGIC_DOCUMENT.READ", "STRATEGIC_DOCUMENT.CREATE", "STRATEGIC_DOCUMENT.UPDATE", "STRATEGIC_DOCUMENT.MANAGE_TYPES", "STRATEGIC_DOCUMENT.APPROVE", "STRATEGIC_DOCUMENT.PUBLISH", "STRATEGIC_DOCUMENT.RETIRE", "STRATEGIC_DOCUMENT.RESCAN", "NAV.STRATEGIC_DOCUMENTS", "STRATEGIC_RISK.READ", "STRATEGIC_RISK.CREATE", "STRATEGIC_RISK.UPDATE", "STRATEGIC_RISK.LINK_KPI", "STRATEGIC_RISK.UNLINK_KPI", "NAV.RISK.DASHBOARD", "NAV.RISK.REGISTER", "NATIONAL_KPA.READ", "NATIONAL_KPA.CREATE", "NATIONAL_KPA.UPDATE", "BACK_TO_BASICS_PILLAR.READ", "BACK_TO_BASICS_PILLAR.CREATE", "BACK_TO_BASICS_PILLAR.UPDATE", "MUNICIPAL_KPA.READ", "MUNICIPAL_KPA.CREATE", "MUNICIPAL_KPA.UPDATE", "STRATEGIC_GOAL.READ", "STRATEGIC_GOAL.CREATE", "STRATEGIC_GOAL.UPDATE", "STRATEGIC_INTERVENTION.READ", "STRATEGIC_INTERVENTION.CREATE", "STRATEGIC_INTERVENTION.UPDATE", "STRATEGIC_OBJECTIVE.READ", "STRATEGIC_OBJECTIVE.CREATE", "STRATEGIC_OBJECTIVE.UPDATE", "PERFORMANCE_OBJECTIVE.READ", "PERFORMANCE_OBJECTIVE.CREATE", "PERFORMANCE_OBJECTIVE.UPDATE", "BUDGET_SOURCE.READ", "BUDGET_SOURCE.CREATE", "BUDGET_SOURCE.UPDATE", "BUDGET_TYPE.READ", "BUDGET_TYPE.CREATE", "BUDGET_TYPE.UPDATE", "STRATEGIC_HIERARCHY.READ", "STRATEGIC_HIERARCHY.CREATE", "STRATEGIC_HIERARCHY.UPDATE", "C88_INDICATOR.READ", "C88_INDICATOR.CREATE", "C88_INDICATOR.UPDATE", "C88_INDICATOR.CONFIGURE", "C88_INDICATOR.MANAGE_CATALOGUE", "C88_INDICATOR.MANAGE_ASSIGNMENTS", "C88_INDICATOR.MANAGE_WORKFLOW", "C88_INDICATOR.MANAGE_MAPPING", "C88_REPORT.READ", "C88_REPORT.CREATE", "C88_REPORT.UPDATE", "C88_REPORT.SUBMIT", "C88_REPORT.VERIFY", "C88_REPORT.RETURN", "C88_REPORT.FINAL_SUBMIT", "NAV.C88.PLANNING", "NAV.C88.REPORTING", "NAV.C88.COMPLIANCE", "NAV.C88.MAPPING", "NAV.C88.REPORTS", "WARD.READ", "WARD.CREATE", "WARD.UPDATE", "VOTE_NUMBER.READ", "VOTE_NUMBER.CREATE", "VOTE_NUMBER.UPDATE", "NAV.CONFIGURATION.PERIODS", "NAV.CONFIGURATION.WORKFLOW", "NAV.CONFIGURATION.LOOKUPS", "NAV.CONFIGURATION.WARDS", "NAV.CONFIGURATION.NATIONAL_KPAS", "NAV.CONFIGURATION.BACK_TO_BASICS", "NAV.CONFIGURATION.MUNICIPAL_KPAS", "NAV.CONFIGURATION.STRATEGIC_GOALS", "NAV.CONFIGURATION.STRATEGIC_INTERVENTIONS", "NAV.CONFIGURATION.STRATEGIC_OBJECTIVES", "NAV.CONFIGURATION.PERFORMANCE_OBJECTIVES", "NAV.CONFIGURATION.BUDGET_SOURCES", "NAV.CONFIGURATION.BUDGET_TYPES", "NAV.SDBIP.VOTE_NUMBERS"],
            ["UserDirectory.View"] = ["NAV.ORGANISATION.EMPLOYEES", "EMPLOYEE.READ", "EMPLOYEE.EmployeeNumber.READ", "EMPLOYEE.IdentityUserId.READ"],
            ["Admin.Users.Manage"] = ["NAV.ADMIN.USERS", "USER.CREATE", "USER.READ", "USER.UPDATE", "USER.DELETE", "USER.Email.READ", "USER.Email.UPDATE", "USER.PhoneNumber.READ", "USER.PhoneNumber.UPDATE", "USER.ENABLE", "USER.DISABLE", "EMPLOYEE.EmployeeNumber.READ", "EMPLOYEE.EmployeeNumber.UPDATE", "EMPLOYEE.EmailAddress.READ", "EMPLOYEE.EmailAddress.UPDATE", "EMPLOYEE.IdentityUserId.READ", "EMPLOYEE.IdentityUserId.UPDATE", "ROLE.ASSIGN", "SECURITY.VIEW_EFFECTIVE", "SECURITY.ASSIGN_ROLES"],
            ["Admin.Roles.Manage"] = ["NAV.ADMIN.ROLES", "SECURITY.VIEW", "SECURITY.MANAGE_ROLES", "SECURITY.ASSIGN_ROLES", "SECURITY.VIEW_EFFECTIVE"],
            ["Admin.Permissions.Manage"] = ["NAV.ADMIN.SECURITY", "SECURITY.VIEW", "SECURITY.MANAGE_PERMISSIONS", "SECURITY.MANAGE_NAVIGATION", "SECURITY.VIEW_EFFECTIVE"], ["Audit.Logs.View"] = ["NAV.ADMIN.AUDIT", "AUDIT_TRAIL.READ", "AUDIT_TRAIL.EntityId.READ", "AUDIT_TRAIL.OldValue.READ", "AUDIT_TRAIL.NewValue.READ", "AUDIT_TRAIL.ChangedBy.READ", "AUDIT_TRAIL.IpAddress.READ", "AUDIT_TRAIL.CorrelationId.READ", "AUDIT_TRAIL.Reason.READ", "AUDIT_TRAIL.UserAgent.READ", "AUDIT_TRAIL.SessionId.READ"],
            ["AUTHENTICATION.LINK_IDENTITIES"] = ["AUTHENTICATION.UserEmail.READ", "AUTHENTICATION.ExpectedEmail.READ", "AUTHENTICATION.ExpectedEmail.UPDATE", "AUTHENTICATION.Issuer.READ", "AUTHENTICATION.Issuer.UPDATE", "AUTHENTICATION.Subject.READ", "AUTHENTICATION.Subject.UPDATE"],
            ["AUTHENTICATION.VIEW_EVENTS"] = ["AUTHENTICATION.EventUserId.READ", "AUTHENTICATION.EventIpAddress.READ"],
            ["Audit.LoginLogs.View"] = ["LOGIN_AUDIT.READ", "LOGIN_AUDIT.UserId.READ", "LOGIN_AUDIT.Email.READ", "LOGIN_AUDIT.IpAddress.READ", "LOGIN_AUDIT.UserAgent.READ", "LOGIN_AUDIT.FailureReason.READ"],
            ["Audit.Trails.View"] = ["AUDIT_TRAIL.READ", "AUDIT_TRAIL.EntityId.READ", "AUDIT_TRAIL.OldValue.READ", "AUDIT_TRAIL.NewValue.READ", "AUDIT_TRAIL.ChangedBy.READ", "AUDIT_TRAIL.IpAddress.READ", "AUDIT_TRAIL.CorrelationId.READ", "AUDIT_TRAIL.Reason.READ", "AUDIT_TRAIL.UserAgent.READ", "AUDIT_TRAIL.SessionId.READ"],
            ["Notifications.View"] = ["NAV.NOTIFICATIONS"],
            ["Reports.View"] = ["OPMS_REPORT.READ", "IPMS_REPORT.READ", "NAV.REPORTS"],
            ["Reports.Generate"] = ["OPMS_REPORT.GENERATE", "IPMS_REPORT.GENERATE"],
            ["Reports.Export"] = ["OPMS_REPORT.EXPORT", "IPMS_REPORT.EXPORT"],
            ["IDP.Kpi.Manage"] = ["IDP_INDICATOR.CREATE", "IDP_INDICATOR.READ", "IDP_INDICATOR.UPDATE", "IDP_INDICATOR.IMPORT"]
        };
        mappings["Configuration.Manage"] = [.. mappings["Configuration.Manage"],
            "NOTIFICATION_DELIVERY.READ", "NOTIFICATION_DELIVERY.RETRY",
            "NOTIFICATION_DELIVERY.AggregateId.READ", "NOTIFICATION_DELIVERY.LastError.READ",
            "NOTIFICATION_DELIVERY.RecipientUserId.READ", "NOTIFICATION_DELIVERY.ProviderReference.READ",
            "NOTIFICATION_DELIVERY.Error.READ", "NOTIFICATION_DELIVERY.ResponseDetail.READ",
            "KPI_TYPE.READ", "KPI_TYPE.CREATE", "KPI_TYPE.UPDATE",
            "INDICATOR_TYPE.READ", "INDICATOR_TYPE.CREATE", "INDICATOR_TYPE.UPDATE",
            "FUNCTIONAL_AREA.READ", "FUNCTIONAL_AREA.CREATE", "FUNCTIONAL_AREA.UPDATE",
            "STANDARD_CLASSIFICATION.READ", "STANDARD_CLASSIFICATION.CREATE", "STANDARD_CLASSIFICATION.UPDATE",
            "KPI_UNIT_OF_MEASURE.READ", "KPI_UNIT_OF_MEASURE.CREATE", "KPI_UNIT_OF_MEASURE.UPDATE",
            "NAV.CONFIGURATION.KPI_TYPES", "NAV.CONFIGURATION.INDICATOR_TYPES",
            "NAV.CONFIGURATION.FUNCTIONAL_AREAS", "NAV.CONFIGURATION.STANDARD_CLASSIFICATIONS", "NAV.CONFIGURATION.KPI_UNITS_OF_MEASURE"];
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

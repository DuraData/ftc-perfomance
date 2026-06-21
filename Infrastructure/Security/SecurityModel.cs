namespace FTCERP.Host.Infrastructure.Security;

public static class SecurityModel
{
    public const string SuperAdmin = "Super Admin";
    public const string Admin = "Admin";
    public const string ClientAdmin = "Client Admin";
    public const string AuditorGeneral = "Auditor General";
    public const string PmsPerformanceManager = "PMS / Performance Manager";
    public const string InternalAudit = "Internal Audit";
    public const string Reviewer = "Reviewer";
    public const string Approver = "Approver";
    public const string Verifier = "Verifier";
    public const string Submitter = "Submitter";

    public static readonly string[] OrderedRoles =
    [
        SuperAdmin,
        Admin,
        ClientAdmin,
        AuditorGeneral,
        PmsPerformanceManager,
        InternalAudit,
        Reviewer,
        Approver,
        Verifier,
        Submitter
    ];

    public static bool IsSuperAdmin(IEnumerable<string> roles)
    {
        return roles.Any(role => string.Equals(role, SuperAdmin, StringComparison.OrdinalIgnoreCase));
    }
}

namespace FTCERP.Host.Infrastructure.Persistence.Seed;

/// <summary>
/// Names used only to create the optional controlled baseline role catalogue.
/// Runtime authorization must resolve database permissions through the central
/// access-control service and must never infer access from one of these names.
/// </summary>
internal static class BaselineSecurityRoleCatalog
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

    public static bool ReceivesInitialFullPermissionCatalogue(string roleName)
    {
        return string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase);
    }
}

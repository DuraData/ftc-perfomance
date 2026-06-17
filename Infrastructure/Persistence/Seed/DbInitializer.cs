using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IConfiguration configuration)
    {
        await context.Database.MigrateAsync();

        // Seed Permissions (idempotent)
        {
            var modules = new[]
            {
                "Dashboard", "IDP", "OPMS", "IPMS", "Projects", "Tasks", "POE", "Reports", "Governance", "Audit", "Notifications", "Integrations", "Helpdesk", "SystemAdministration"
            };
            var actions = new[] { "View", "Create", "Edit", "Delete", "Approve", "Submit", "Verify", "Export", "Manage" };

            foreach (var module in modules)
            {
                foreach (var action in actions)
                {
                    var code = $"{module}.{action}";
                    var exists = await context.Permissions.AnyAsync(p => p.Code == code);
                    if (exists) continue;

                    context.Permissions.Add(new Permission
                    {
                        Module = module,
                        Feature = "All",
                        Action = action,
                        Code = code,
                        Description = $"Allows {action} access to {module} module"
                    });
                }
            }

            var extraPermissions = new[]
            {
                new Permission { Module = "Admin", Feature = "Users", Action = "Manage", Code = "Admin.Users.Manage", Description = "Manage users" },
                new Permission { Module = "Admin", Feature = "Roles", Action = "Manage", Code = "Admin.Roles.Manage", Description = "Manage roles" },
                new Permission { Module = "Admin", Feature = "Permissions", Action = "Manage", Code = "Admin.Permissions.Manage", Description = "Manage permissions" },
                new Permission { Module = "Audit", Feature = "LoginLogs", Action = "View", Code = "Audit.LoginLogs.View", Description = "View login audit logs" },
                new Permission { Module = "Navigation", Feature = "Menu", Action = "View", Code = "Navigation.MyMenu.View", Description = "View my menu" },
                new Permission { Module = "Access", Feature = "Permissions", Action = "View", Code = "Access.MyPermissions.View", Description = "View my permissions" },
            };

            foreach (var p in extraPermissions)
            {
                var exists = await context.Permissions.AnyAsync(x => x.Code == p.Code);
                if (!exists)
                {
                    context.Permissions.Add(p);
                }
            }

            await context.SaveChangesAsync();
        }

        // Seed Roles (idempotent)
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Super Admin", NormalizedName = "SUPER ADMIN", Description = "Full system access", IsSystemRole = true },
                new ApplicationRole { Name = "System Admin", NormalizedName = "SYSTEM ADMIN", Description = "System administrator with unrestricted access", IsSystemRole = true },
                new ApplicationRole { Name = "ICT Admin", NormalizedName = "ICT ADMIN", Description = "IT administration role", IsSystemRole = true },
                new ApplicationRole { Name = "ICT Sub-Admin", NormalizedName = "ICT SUB-ADMIN", Description = "Limited IT administration", IsSystemRole = true },
                new ApplicationRole { Name = "EPMS Admin", NormalizedName = "EPMS ADMIN", Description = "EPMS system administrator", IsSystemRole = false },
                new ApplicationRole { Name = "EPMS User", NormalizedName = "EPMS USER", Description = "Regular EPMS user", IsSystemRole = false },
                new ApplicationRole { Name = "OPMS Admin", NormalizedName = "OPMS ADMIN", Description = "OPMS module administrator", IsSystemRole = false },
                new ApplicationRole { Name = "IPMS Admin", NormalizedName = "IPMS ADMIN", Description = "IPMS module administrator", IsSystemRole = false },
                new ApplicationRole { Name = "Audit Admin", NormalizedName = "AUDIT ADMIN", Description = "Audit module administrator", IsSystemRole = false },
                new ApplicationRole { Name = "AGSA Viewer", NormalizedName = "AGSA VIEWER", Description = "Read-only access for AGSA", IsSystemRole = false },
                new ApplicationRole { Name = "Department Manager", NormalizedName = "DEPARTMENT MANAGER", Description = "Department manager role", IsSystemRole = false },
                new ApplicationRole { Name = "Submitter", NormalizedName = "SUBMITTER", Description = "Submission creator", IsSystemRole = false },
                new ApplicationRole { Name = "Verifier", NormalizedName = "VERIFIER", Description = "Submission verifier", IsSystemRole = false },
                new ApplicationRole { Name = "Approver", NormalizedName = "APPROVER", Description = "Submission approver", IsSystemRole = false },
                new ApplicationRole { Name = "Auditor", NormalizedName = "AUDITOR", Description = "Auditor role", IsSystemRole = false },
                new ApplicationRole { Name = "Executive Viewer", NormalizedName = "EXECUTIVE VIEWER", Description = "Executive read-only access", IsSystemRole = false }
            };

            foreach (var role in roles)
            {
                var existingRole = await roleManager.FindByNameAsync(role.Name!);
                if (existingRole == null)
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        await SeedRolePermissionsAsync(context, roleManager);

        // Seed default admin user and ensure it uses System Admin
        {
            const string defaultRoleName = "System Admin";
            const string legacyRoleName = "Super Admin";

            var adminEmail = configuration["Admin:Email"] ?? "superadmin@example.com";
            var adminPassword = configuration["Admin:Password"] ?? "P@ssw0rd123!";

            var defaultUser = await userManager.FindByEmailAsync(adminEmail);
            if (defaultUser == null)
            {
                defaultUser = new ApplicationUser
                {
                    UserName = configuration["Admin:UserName"] ?? adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    Department = "Office of the Municipal Manager",
                    Position = "System Administrator",
                    PhoneNumber = "0825550001",
                    IsActive = true,
                    MustChangePassword = false,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(defaultUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    return;
                }
            }
            else
            {
                defaultUser.UserName = configuration["Admin:UserName"] ?? defaultUser.UserName ?? adminEmail;
                defaultUser.Department = "Office of the Municipal Manager";
                defaultUser.Position = "System Administrator";
                defaultUser.PhoneNumber ??= "0825550001";
                defaultUser.IsActive = true;
                defaultUser.EmailConfirmed = true;
                await userManager.UpdateAsync(defaultUser);
            }

            await EnsurePasswordAsync(userManager, defaultUser, adminPassword);

            var assignedRoles = await userManager.GetRolesAsync(defaultUser);
            if (!assignedRoles.Contains(defaultRoleName, StringComparer.OrdinalIgnoreCase))
            {
                await userManager.AddToRoleAsync(defaultUser, defaultRoleName);
            }

            var legacyRoles = assignedRoles
                .Where(role => string.Equals(role, legacyRoleName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (legacyRoles.Length > 0)
            {
                await userManager.RemoveFromRolesAsync(defaultUser, legacyRoles);
            }
        }

        await SeedDemoUsersAsync(userManager, configuration);
    }

    internal static DemoUserResponse[] GetDemoUserResponses(IConfiguration configuration)
    {
        var password = GetDemoPassword(configuration);
        return GetDemoUserSeeds()
            .Select(seed => new DemoUserResponse(
                seed.Role,
                $"{seed.FirstName} {seed.LastName}",
                seed.Department,
                seed.Position,
                seed.Email,
                seed.UserName,
                password))
            .ToArray();
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
    {
        var permissionsByCode = await context.Permissions
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

        var superAdminRole = await roleManager.FindByNameAsync("Super Admin");
        if (superAdminRole != null)
        {
            await SyncRolePermissionsAsync(context, superAdminRole.Id, permissionsByCode.Values);
        }

        foreach (var mapping in BuildRolePermissionMap())
        {
            var role = await roleManager.FindByNameAsync(mapping.Key);
            if (role == null)
            {
                continue;
            }

            var permissionIds = mapping.Value
                .Where(permissionsByCode.ContainsKey)
                .Select(code => permissionsByCode[code])
                .Distinct()
                .ToArray();

            await SyncRolePermissionsAsync(context, role.Id, permissionIds);
        }
    }

    private static async Task SyncRolePermissionsAsync(ApplicationDbContext context, string roleId, IEnumerable<int> desiredPermissionIds)
    {
        var desired = desiredPermissionIds.ToHashSet();
        var existing = await context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();

        var toRemove = existing.Where(rp => !desired.Contains(rp.PermissionId)).ToList();
        if (toRemove.Count > 0)
        {
            context.RolePermissions.RemoveRange(toRemove);
        }

        var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var permissionId in desired.Except(existingIds))
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsAllowed = true
            });
        }

        await context.SaveChangesAsync();
    }

    private static Dictionary<string, string[]> BuildRolePermissionMap()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["EPMS User"] = BaseAccess("OPMS.View", "IPMS.View", "Reports.View", "OPMS.Submit", "IPMS.Submit"),
            ["OPMS Admin"] = BaseAccess("OPMS.View", "OPMS.Create", "OPMS.Edit", "OPMS.Delete", "OPMS.Submit", "OPMS.Verify", "OPMS.Approve", "Reports.View"),
            ["IPMS Admin"] = BaseAccess("IPMS.View", "IPMS.Create", "IPMS.Edit", "IPMS.Delete", "IPMS.Submit", "IPMS.Verify", "IPMS.Approve", "Reports.View"),
            ["Audit Admin"] = BaseAccess("Audit.View", "Audit.Verify", "Audit.Export", "Reports.View", "Audit.LoginLogs.View"),
            ["AGSA Viewer"] = BaseAccess("Audit.View", "Reports.View"),
            ["Department Manager"] = BaseAccess("OPMS.View", "IPMS.View", "Reports.View", "OPMS.Approve", "IPMS.Approve"),
            ["Submitter"] = BaseAccess("OPMS.View", "IPMS.View", "OPMS.Submit", "IPMS.Submit"),
            ["Verifier"] = BaseAccess("OPMS.View", "IPMS.View", "OPMS.Verify", "IPMS.Verify"),
            ["Approver"] = BaseAccess("OPMS.View", "IPMS.View", "Reports.View", "OPMS.Approve", "IPMS.Approve"),
            ["Auditor"] = BaseAccess("Audit.View", "Audit.Export", "Reports.View"),
            ["Executive Viewer"] = BaseAccess("Reports.View")
        };
    }

    private static string[] BaseAccess(params string[] permissionCodes)
    {
        return new[]
        {
            "Dashboard.View",
            "Navigation.MyMenu.View",
            "Access.MyPermissions.View"
        }.Concat(permissionCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static async Task SeedDemoUsersAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var defaultPassword = GetDemoPassword(configuration);
        var demoRoles = GetDemoUserSeeds().Select(x => x.Role).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in GetDemoUserSeeds())
        {
            var user = await userManager.FindByEmailAsync(seed.Email) ?? await userManager.FindByNameAsync(seed.UserName);
            var created = false;

            if (user == null)
            {
                user = new ApplicationUser();
                created = true;
            }

            user.UserName = seed.UserName;
            user.Email = seed.Email;
            user.FirstName = seed.FirstName;
            user.LastName = seed.LastName;
            user.PhoneNumber = seed.PhoneNumber;
            user.Department = seed.Department;
            user.Position = seed.Position;
            user.IsActive = true;
            user.MustChangePassword = false;
            user.EmailConfirmed = true;

            if (created)
            {
                var createResult = await userManager.CreateAsync(user, defaultPassword);
                if (!createResult.Succeeded)
                {
                    continue;
                }
            }
            else
            {
                await userManager.UpdateAsync(user);
                await EnsurePasswordAsync(userManager, user, defaultPassword);
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles
                .Where(role => demoRoles.Contains(role) && !string.Equals(role, seed.Role, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (rolesToRemove.Length > 0)
            {
                await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (!currentRoles.Contains(seed.Role, StringComparer.OrdinalIgnoreCase))
            {
                await userManager.AddToRoleAsync(user, seed.Role);
            }
        }
    }

    private static async Task EnsurePasswordAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string password)
    {
        var hasPassword = await userManager.HasPasswordAsync(user);
        if (!hasPassword)
        {
            await userManager.AddPasswordAsync(user, password);
            return;
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.ResetPasswordAsync(user, resetToken, password);
    }

    private static string GetDemoPassword(IConfiguration configuration)
    {
        return configuration["DemoUsers:DefaultPassword"]
            ?? configuration["Admin:Password"]
            ?? "P@ssw0rd123!";
    }

    private static IReadOnlyList<DemoUserSeed> GetDemoUserSeeds()
    {
        return
        [
            new("Super Admin", "Lethabo", "Khumalo", "lethabo.khumalo", "lethabo.khumalo@bluehills.gov.za", "0825550101", "Office of the Municipal Manager", "Enterprise Systems Administrator"),
            new("Super Admin", "Annelie", "Bothma", "annelie.bothma", "annelie.bothma@bluehills.gov.za", "0825550102", "Corporate Services", "Digital Governance Lead"),
            new("Super Admin", "Sipho", "Nene", "sipho.nene", "sipho.nene@bluehills.gov.za", "0825550103", "ICT Services", "Infrastructure Platforms Manager"),

            new("ICT Admin", "Thandiwe", "Maseko", "thandiwe.maseko", "thandiwe.maseko@bluehills.gov.za", "0825550201", "ICT Services", "ICT Systems Manager"),
            new("ICT Admin", "Kabelo", "Mahlangu", "kabelo.mahlangu", "kabelo.mahlangu@bluehills.gov.za", "0825550202", "ICT Services", "Applications Manager"),
            new("ICT Admin", "Melissa", "Jacobs", "melissa.jacobs", "melissa.jacobs@bluehills.gov.za", "0825550203", "ICT Services", "Cybersecurity Manager"),

            new("ICT Sub-Admin", "Sibusiso", "Mthembu", "sibusiso.mthembu", "sibusiso.mthembu@bluehills.gov.za", "0825550301", "ICT Services", "Systems Support Officer"),
            new("ICT Sub-Admin", "Lerato", "Pienaar", "lerato.pienaar", "lerato.pienaar@bluehills.gov.za", "0825550302", "ICT Services", "Database Support Analyst"),
            new("ICT Sub-Admin", "Aisha", "Daniels", "aisha.daniels", "aisha.daniels@bluehills.gov.za", "0825550303", "ICT Services", "Endpoint Support Specialist"),

            new("EPMS Admin", "Noluthando", "Gama", "noluthando.gama", "noluthando.gama@bluehills.gov.za", "0825550401", "Strategic Planning and Performance", "Performance Management Administrator"),
            new("EPMS Admin", "Brian", "Sekgobela", "brian.sekgobela", "brian.sekgobela@bluehills.gov.za", "0825550402", "Strategic Planning and Performance", "Planning Systems Coordinator"),
            new("EPMS Admin", "Candice", "Petersen", "candice.petersen", "candice.petersen@bluehills.gov.za", "0825550403", "Office of the Municipal Manager", "Integrated Performance Administrator"),

            new("EPMS User", "Mandla", "Dube", "mandla.dube", "mandla.dube@bluehills.gov.za", "0825550501", "Strategic Planning and Performance", "Performance Management Officer"),
            new("EPMS User", "Zanele", "Mokoena", "zanele.mokoena", "zanele.mokoena@bluehills.gov.za", "0825550502", "Strategic Planning and Performance", "Monitoring and Evaluation Officer"),
            new("EPMS User", "Gerhard", "Swanepoel", "gerhard.swanepoel", "gerhard.swanepoel@bluehills.gov.za", "0825550503", "Budget and Treasury Office", "Performance Reporting Officer"),

            new("OPMS Admin", "Nomvula", "Mkhize", "nomvula.mkhize", "nomvula.mkhize@bluehills.gov.za", "0825550601", "Strategic Planning and Performance", "OPMS Coordinator"),
            new("OPMS Admin", "Francois", "du Plessis", "francois.duplessis", "francois.duplessis@bluehills.gov.za", "0825550602", "Infrastructure and Technical Services", "Senior OPMS Administrator"),
            new("OPMS Admin", "Boitumelo", "Radebe", "boitumelo.radebe", "boitumelo.radebe@bluehills.gov.za", "0825550603", "Community Services", "Operations Performance Coordinator"),

            new("IPMS Admin", "Priya", "Naidoo", "priya.naidoo", "priya.naidoo@bluehills.gov.za", "0825550701", "Strategic Planning and Performance", "IPMS Coordinator"),
            new("IPMS Admin", "Themba", "Zulu", "themba.zulu", "themba.zulu@bluehills.gov.za", "0825550702", "Infrastructure and Technical Services", "Infrastructure Performance Administrator"),
            new("IPMS Admin", "Megan", "van Wyk", "megan.vanwyk", "megan.vanwyk@bluehills.gov.za", "0825550703", "Budget and Treasury Office", "Capital Programme Performance Officer"),

            new("Audit Admin", "Azwi", "Netshifhefhe", "azwi.netshifhefhe", "azwi.netshifhefhe@bluehills.gov.za", "0825550801", "Internal Audit and Risk", "Internal Audit Manager"),
            new("Audit Admin", "Riaan", "Pretorius", "riaan.pretorius", "riaan.pretorius@bluehills.gov.za", "0825550802", "Internal Audit and Risk", "Risk and Assurance Manager"),
            new("Audit Admin", "Fatima", "Ismail", "fatima.ismail", "fatima.ismail@bluehills.gov.za", "0825550803", "Internal Audit and Risk", "Audit Quality Manager"),

            new("AGSA Viewer", "Koketso", "Molefe", "koketso.molefe", "koketso.molefe@bluehills.gov.za", "0825550901", "Office of the Auditor-General Liaison", "AGSA Liaison Officer"),
            new("AGSA Viewer", "Johan", "Steyn", "johan.steyn", "johan.steyn@bluehills.gov.za", "0825550902", "Office of the Auditor-General Liaison", "Audit Coordination Analyst"),
            new("AGSA Viewer", "Nosipho", "Mbatha", "nosipho.mbatha", "nosipho.mbatha@bluehills.gov.za", "0825550903", "Office of the Auditor-General Liaison", "External Audit Support Officer"),

            new("Department Manager", "Thabo", "Mokoena", "thabo.mokoena", "thabo.mokoena@bluehills.gov.za", "0825551001", "Infrastructure and Technical Services", "Department Manager"),
            new("Department Manager", "Lindiwe", "Mahlangu", "lindiwe.mahlangu", "lindiwe.mahlangu@bluehills.gov.za", "0825551002", "Community Services", "Department Manager"),
            new("Department Manager", "Ahmed", "Hassan", "ahmed.hassan", "ahmed.hassan@bluehills.gov.za", "0825551003", "Budget and Treasury Office", "Department Manager"),

            new("Submitter", "Nomsa", "Dlamini", "nomsa.dlamini", "nomsa.dlamini@bluehills.gov.za", "0825551101", "Infrastructure and Technical Services", "Senior Performance Officer"),
            new("Submitter", "Keitumetse", "Mabaso", "keitumetse.mabaso", "keitumetse.mabaso@bluehills.gov.za", "0825551102", "Community Services", "Performance Information Officer"),
            new("Submitter", "Bradley", "Peters", "bradley.peters", "bradley.peters@bluehills.gov.za", "0825551103", "Corporate Services", "Performance Reporting Clerk"),

            new("Verifier", "Ayabonga", "Cele", "ayabonga.cele", "ayabonga.cele@bluehills.gov.za", "0825551201", "Strategic Planning and Performance", "Monitoring and Evaluation Officer"),
            new("Verifier", "Reneilwe", "Mogale", "reneilwe.mogale", "reneilwe.mogale@bluehills.gov.za", "0825551202", "Budget and Treasury Office", "Data Quality Verifier"),
            new("Verifier", "Willem", "Nel", "willem.nel", "willem.nel@bluehills.gov.za", "0825551203", "Infrastructure and Technical Services", "Senior Verification Officer"),

            new("Approver", "Mpho", "Madonsela", "mpho.madonsela", "mpho.madonsela@bluehills.gov.za", "0825551301", "Office of the Municipal Manager", "Director: Strategy and Performance"),
            new("Approver", "Helena", "Visagie", "helena.visagie", "helena.visagie@bluehills.gov.za", "0825551302", "Budget and Treasury Office", "Director: Financial Governance"),
            new("Approver", "Sizwe", "Nxumalo", "sizwe.nxumalo", "sizwe.nxumalo@bluehills.gov.za", "0825551303", "Infrastructure and Technical Services", "Director: Technical Services"),

            new("Auditor", "David", "Khumalo", "david.khumalo", "david.khumalo@bluehills.gov.za", "0825551401", "Internal Audit and Risk", "Internal Auditor"),
            new("Auditor", "Mariska", "Coetzee", "mariska.coetzee", "mariska.coetzee@bluehills.gov.za", "0825551402", "Internal Audit and Risk", "Senior Audit Analyst"),
            new("Auditor", "Tshepiso", "Ratau", "tshepiso.ratau", "tshepiso.ratau@bluehills.gov.za", "0825551403", "Governance and Risk", "Compliance Auditor"),

            new("Executive Viewer", "Maria", "Fernandez", "maria.fernandez", "maria.fernandez@bluehills.gov.za", "0825551501", "Office of the Executive Mayor", "Executive Support Analyst"),
            new("Executive Viewer", "Bongani", "Mabena", "bongani.mabena", "bongani.mabena@bluehills.gov.za", "0825551502", "Office of the Speaker", "Council Performance Analyst"),
            new("Executive Viewer", "Tracey", "Adams", "tracey.adams", "tracey.adams@bluehills.gov.za", "0825551503", "Office of the Chief Financial Officer", "Executive Reporting Specialist")
        ];
    }

    private sealed record DemoUserSeed(
        string Role,
        string FirstName,
        string LastName,
        string UserName,
        string Email,
        string PhoneNumber,
        string Department,
        string Position);
}

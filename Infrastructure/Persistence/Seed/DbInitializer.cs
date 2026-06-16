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
        if (!context.Roles.Any())
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Super Admin", NormalizedName = "SUPER ADMIN", Description = "Full system access", IsSystemRole = true },
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
                await roleManager.CreateAsync(role);
            }
        }

        // Ensure Super Admin always has all permissions (idempotent)
        {
            var superAdminRole = await roleManager.FindByNameAsync("Super Admin");
            if (superAdminRole != null)
            {
                var allPermissionIds = await context.Permissions.Select(p => p.Id).ToListAsync();
                var existing = await context.RolePermissions
                    .Where(rp => rp.RoleId == superAdminRole.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var missing = allPermissionIds.Except(existing).ToList();
                foreach (var permissionId in missing)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = superAdminRole.Id,
                        PermissionId = permissionId,
                        IsAllowed = true
                    });
                }

                if (missing.Count > 0)
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        // Seed Super Admin User
        if (!context.Users.Any())
        {
            var adminEmail = configuration["Admin:Email"] ?? "superadmin@example.com";
            var adminPassword = configuration["Admin:Password"] ?? "P@ssw0rd123!";

            var superAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Super",
                LastName = "Admin",
                IsActive = true,
                MustChangePassword = false,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(superAdmin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "Super Admin");
            }
        }
    }
}

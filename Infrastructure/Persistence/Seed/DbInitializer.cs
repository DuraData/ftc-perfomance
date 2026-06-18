using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IConfiguration configuration)
    {
        await context.Database.MigrateAsync();

        await SeedPermissionsAsync(context);
        await SeedDepartmentsAndUnitsAsync(context);
        await SeedRolesAsync(roleManager);
        await SeedRolePermissionsAsync(context, roleManager);
        await SeedDefaultAdminUserAsync(context, userManager, configuration);
        await SeedDemoUsersAsync(context, userManager, configuration);
        await SeedLookupTablesAsync(context);
        await SeedTargetsAndSubmissionsAsync(context, userManager);
    }

    internal static DemoUserResponse[] GetDemoUserResponses(IConfiguration configuration)
    {
        var password = GetDemoPassword(configuration);
        var departments = GetDepartmentSeeds().ToDictionary(d => d.Code, d => d.Name, StringComparer.OrdinalIgnoreCase);
        return GetDemoUserSeeds()
            .Select(seed => new DemoUserResponse(
                seed.Role,
                $"{seed.FirstName} {seed.LastName}",
                departments[seed.DepartmentCode],
                seed.Position,
                seed.Email,
                seed.UserName,
                password))
            .ToArray();
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        var permissions = BuildPermissionCatalog();
        var existing = await context.Permissions.AsNoTracking().ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions)
        {
            if (!existing.TryGetValue(permission.Code, out var current))
            {
                context.Permissions.Add(permission);
                continue;
            }

            permission.Id = current.Id;
            context.Permissions.Update(permission);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDepartmentsAndUnitsAsync(ApplicationDbContext context)
    {
        var existingDepartments = await context.Departments.Include(d => d.Units).ToListAsync();
        var byCode = existingDepartments.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var departmentSeed in GetDepartmentSeeds())
        {
            if (!byCode.TryGetValue(departmentSeed.Code, out var department))
            {
                department = new Department
                {
                    Code = departmentSeed.Code,
                    Name = departmentSeed.Name,
                    Description = departmentSeed.Description
                };
                context.Departments.Add(department);
                byCode[departmentSeed.Code] = department;
            }
            else
            {
                department.Name = departmentSeed.Name;
                department.Description = departmentSeed.Description;
            }
        }

        await context.SaveChangesAsync();

        var departments = await context.Departments.Include(d => d.Units).ToListAsync();
        foreach (var departmentSeed in GetDepartmentSeeds())
        {
            var department = departments.Single(d => d.Code == departmentSeed.Code);
            var unitsByCode = department.Units.ToDictionary(u => u.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var unitSeed in departmentSeed.Units)
            {
                if (!unitsByCode.TryGetValue(unitSeed.Code, out var unit))
                {
                    context.Units.Add(new Unit
                    {
                        DepartmentId = department.Id,
                        Code = unitSeed.Code,
                        Name = unitSeed.Name
                    });
                }
                else
                {
                    unit.Name = unitSeed.Name;
                    unit.DepartmentId = department.Id;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var roleSeed in GetRoleSeeds())
        {
            var existingRole = await roleManager.FindByNameAsync(roleSeed.Name);
            if (existingRole == null)
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleSeed.Name,
                    NormalizedName = roleSeed.Name.ToUpperInvariant(),
                    Description = roleSeed.Description,
                    IsSystemRole = roleSeed.IsSystemRole,
                    IsActive = true
                });
                continue;
            }

            existingRole.Description = roleSeed.Description;
            existingRole.IsSystemRole = roleSeed.IsSystemRole;
            existingRole.IsActive = true;
            await roleManager.UpdateAsync(existingRole);
        }

        var validRoles = SecurityModel.OrderedRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allRoles = roleManager.Roles.ToList();
        foreach (var role in allRoles.Where(role => !validRoles.Contains(role.Name!)))
        {
            role.IsActive = false;
            await roleManager.UpdateAsync(role);
        }
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
    {
        var permissionsByCode = await context.Permissions
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in SecurityModel.OrderedRoles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                continue;
            }

            IEnumerable<int> permissionIds;
            if (SecurityModel.IsSuperAdmin([roleName]))
            {
                permissionIds = permissionsByCode.Values;
            }
            else
            {
                permissionIds = BuildRolePermissionMap()[roleName]
                    .Where(permissionsByCode.ContainsKey)
                    .Select(code => permissionsByCode[code])
                    .Distinct();
            }

            await SyncRolePermissionsAsync(context, role.Id, permissionIds);
        }
    }

    private static async Task SeedDefaultAdminUserAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var adminEmail = configuration["Admin:Email"] ?? "superadmin@example.com";
        var adminPassword = configuration["Admin:Password"] ?? "P@ssw0rd123!";
        var omm = await context.Departments.FirstAsync(d => d.Code == "OMM");
        var executiveUnit = await context.Units.FirstAsync(u => u.Code == "OMM-EXEC");

        var defaultUser = await userManager.FindByEmailAsync(adminEmail);
        if (defaultUser == null)
        {
            defaultUser = new ApplicationUser
            {
                UserName = configuration["Admin:UserName"] ?? adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                Department = omm.Name,
                DepartmentId = omm.Id,
                UnitId = executiveUnit.Id,
                Position = "Super Administrator",
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
            defaultUser.Department = omm.Name;
            defaultUser.DepartmentId = omm.Id;
            defaultUser.UnitId = executiveUnit.Id;
            defaultUser.Position = "Super Administrator";
            defaultUser.PhoneNumber ??= "0825550001";
            defaultUser.IsActive = true;
            defaultUser.EmailConfirmed = true;
            await userManager.UpdateAsync(defaultUser);
        }

        await EnsurePasswordAsync(userManager, defaultUser, adminPassword);
        await SyncUserRolesAsync(userManager, defaultUser, [SecurityModel.SuperAdmin]);
        await ReplaceUserScopesAsync(context, defaultUser.Id, [new UserScope { ScopeType = ScopeType.InstitutionScope }]);
        await ReplaceUserAssignmentsAsync(context, defaultUser.Id, Array.Empty<UserAssignment>());
    }

    private static async Task SeedDemoUsersAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var defaultPassword = GetDemoPassword(configuration);
        var departments = await context.Departments.AsNoTracking().ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var units = await context.Units.AsNoTracking().ToDictionaryAsync(u => u.Code, StringComparer.OrdinalIgnoreCase);
        var validRoleNames = SecurityModel.OrderedRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in GetDemoUserSeeds())
        {
            var user = await userManager.FindByEmailAsync(seed.Email) ?? await userManager.FindByNameAsync(seed.UserName);
            var created = false;
            var department = departments[seed.DepartmentCode];
            var unit = seed.UnitCode != null && units.TryGetValue(seed.UnitCode, out var foundUnit) ? foundUnit : null;

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
            user.Department = department.Name;
            user.DepartmentId = department.Id;
            user.UnitId = unit?.Id;
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
            var rolesToRemove = currentRoles.Where(role => validRoleNames.Contains(role) && !string.Equals(role, seed.Role, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (rolesToRemove.Length > 0)
            {
                await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }
            if (!currentRoles.Contains(seed.Role, StringComparer.OrdinalIgnoreCase))
            {
                await userManager.AddToRoleAsync(user, seed.Role);
            }

            await ReplaceUserScopesAsync(context, user.Id, seed.Scopes.Select(scope => BuildUserScope(scope, departments, units)).ToArray());
            await ReplaceUserAssignmentsAsync(context, user.Id, seed.Assignments.Select(BuildUserAssignment).ToArray());
        }
    }

    private static UserScope BuildUserScope(DemoScopeSeed seed, IReadOnlyDictionary<string, Department> departments, IReadOnlyDictionary<string, Unit> units)
    {
        return new UserScope
        {
            ScopeType = seed.ScopeType,
            DepartmentId = seed.DepartmentCode != null ? departments[seed.DepartmentCode].Id : null,
            UnitId = seed.UnitCode != null && units.TryGetValue(seed.UnitCode, out var unit) ? unit.Id : null,
            TargetId = seed.TargetId,
            KpiId = seed.KpiId,
            ProjectId = seed.ProjectId,
            TaskId = seed.TaskId
        };
    }

    private static UserAssignment BuildUserAssignment(DemoAssignmentSeed seed)
    {
        return new UserAssignment
        {
            AssignmentType = seed.AssignmentType,
            TargetId = seed.TargetId,
            KpiId = seed.KpiId,
            ProjectId = seed.ProjectId,
            TaskId = seed.TaskId
        };
    }

    private static async Task ReplaceUserScopesAsync(ApplicationDbContext context, string userId, IEnumerable<UserScope> scopes)
    {
        var existing = await context.UserScopes.Where(scope => scope.UserId == userId).ToListAsync();
        context.UserScopes.RemoveRange(existing);
        foreach (var scope in scopes)
        {
            scope.UserId = userId;
            context.UserScopes.Add(scope);
        }
        await context.SaveChangesAsync();
    }

    private static async Task ReplaceUserAssignmentsAsync(ApplicationDbContext context, string userId, IEnumerable<UserAssignment> assignments)
    {
        var existing = await context.UserAssignments.Where(assignment => assignment.UserId == userId).ToListAsync();
        context.UserAssignments.RemoveRange(existing);
        foreach (var assignment in assignments)
        {
            assignment.UserId = userId;
            context.UserAssignments.Add(assignment);
        }
        await context.SaveChangesAsync();
    }

    private static async Task SyncUserRolesAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, IEnumerable<string> roleNames)
    {
        var desiredRoles = roleNames.ToArray();
        var currentRoles = await userManager.GetRolesAsync(user);
        var remove = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        var add = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        if (remove.Length > 0)
        {
            await userManager.RemoveFromRolesAsync(user, remove);
        }
        if (add.Length > 0)
        {
            await userManager.AddToRolesAsync(user, add);
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

    private static Permission[] BuildPermissionCatalog()
    {
        return
        [
            Permission("Dashboard", "Overview", "View", "Dashboard.View", "View role-based dashboards"),
            Permission("Navigation", "Menu", "View", "Navigation.MyMenu.View", "View the dynamic menu"),
            Permission("Access", "Permissions", "View", "Access.MyPermissions.View", "View my permissions"),
            Permission("Admin", "Users", "Manage", "Admin.Users.Manage", "Manage users"),
            Permission("Admin", "Roles", "Manage", "Admin.Roles.Manage", "Manage roles"),
            Permission("Admin", "Permissions", "Manage", "Admin.Permissions.Manage", "Manage permission definitions"),
            Permission("Directory", "Users", "View", "UserDirectory.View", "View the user directory"),
            Permission("Departments", "Administration", "View", "Departments.View", "View departments"),
            Permission("Departments", "Administration", "Manage", "Departments.Manage", "Manage departments"),
            Permission("Units", "Administration", "View", "Units.View", "View units"),
            Permission("Units", "Administration", "Manage", "Units.Manage", "Manage units"),
            Permission("Configuration", "System", "View", "Configuration.View", "View setup data"),
            Permission("Configuration", "System", "Manage", "Configuration.Manage", "Manage setup data"),
            Permission("Notifications", "System", "View", "Notifications.View", "View notifications"),
            Permission("Notifications", "System", "Manage", "Notifications.Manage", "Manage notifications"),
            Permission("Implementation", "Roles", "View", "RoleImplementationAudit.View", "View role implementation audit"),
            Permission("SystemAdministration", "RoleAccessMatrix", "View", "SystemAdministration.RoleAccessMatrix.View", "View role access matrix"),
            Permission("SystemAdministration", "PermissionSimulation", "View", "SystemAdministration.PermissionSimulation.View", "View permission simulation"),
            Permission("SystemAdministration", "SystemCoverageAudit", "View", "SystemAdministration.SystemCoverageAudit.View", "View system coverage audit"),
            Permission("Audit", "LoginLogs", "View", "Audit.LoginLogs.View", "View login audit logs"),
            Permission("Audit", "Platform", "View", "Audit.Logs.View", "View audit trail logs"),
            Permission("Audit", "Reports", "View", "Audit.Reports.View", "View audit reports"),
            Permission("Audit", "Trail", "View", "Audit.Trail.View", "View record-level audit trails"),
            Permission("Backup", "Logs", "View", "BackupLogs.View", "View backup logs"),
            Permission("DueDateExtensions", "Workflow", "Manage", "DueDateExtensions.Manage", "Manage due date extensions"),
            Permission("SystemAdministration", "AuditTrail", "View", "SystemAdministration.AuditTrail.View", "View audit trail records"),
            Permission("Notifications", "Workflow", "Trigger", "Notifications.TriggerNotification", "Trigger workflow notifications"),
            Permission("OPMS", "Library", "View", "OPMS.Library.View", "View OPMS target library"),
            Permission("OPMS", "Library", "Create", "OPMS.Library.Create", "Create OPMS target templates"),
            Permission("OPMS", "Library", "Edit", "OPMS.Library.Edit", "Edit OPMS target templates"),
            Permission("OPMS", "Library", "Delete", "OPMS.Library.Delete", "Archive or delete OPMS target templates"),
            Permission("OPMS", "Library", "Duplicate", "OPMS.Library.Duplicate", "Duplicate OPMS target templates"),
            Permission("OPMS", "Library", "UseTemplate", "OPMS.Library.UseTemplate", "Create OPMS targets from library templates"),
            Permission("OPMS", "Targets", "View", "OPMS.Targets.View", "View OPMS targets"),
            Permission("OPMS", "Targets", "Create", "OPMS.Targets.Create", "Create OPMS targets"),
            Permission("OPMS", "Targets", "Edit", "OPMS.Targets.Edit", "Edit OPMS targets"),
            Permission("OPMS", "Targets", "Delete", "OPMS.Targets.Delete", "Delete OPMS targets"),
            Permission("OPMS", "Targets", "Archive", "OPMS.Targets.Archive", "Archive OPMS targets"),
            Permission("OPMS", "Submissions", "View", "OPMS.Submissions.View", "View OPMS submissions"),
            Permission("OPMS", "Submissions", "Create", "OPMS.Submissions.Create", "Create OPMS submissions"),
            Permission("OPMS", "Submissions", "Edit", "OPMS.Submissions.Edit", "Edit OPMS submissions"),
            Permission("OPMS", "Submissions", "Delete", "OPMS.Submissions.Delete", "Delete OPMS submissions"),
            Permission("OPMS", "Submissions", "Submit", "OPMS.Submissions.Submit", "Submit OPMS submissions"),
            Permission("OPMS", "Submissions", "Verify", "OPMS.Submissions.Verify", "Verify OPMS submissions"),
            Permission("OPMS", "Submissions", "VerifyReject", "OPMS.Submissions.VerifyReject", "Verify reject OPMS submissions"),
            Permission("OPMS", "Submissions", "Approve", "OPMS.Submissions.Approve", "Approve OPMS submissions"),
            Permission("OPMS", "Submissions", "Reject", "OPMS.Submissions.Reject", "Reject OPMS submissions"),
            Permission("OPMS", "Submissions", "Review", "OPMS.Submissions.Review", "Review OPMS submissions"),
            Permission("OPMS", "Submissions", "Audit", "OPMS.Submissions.Audit", "Audit OPMS submissions"),
            Permission("OPMS", "Submissions", "Score", "OPMS.Submissions.Score", "Score OPMS submissions"),
            Permission("OPMS", "POE", "Upload", "OPMS.POE.Upload", "Upload OPMS POE files"),
            Permission("OPMS", "Submissions", "ExtendDueDate", "OPMS.Submissions.ExtendDueDate", "Extend OPMS submission due dates"),
            Permission("IPMS", "Library", "View", "IPMS.Library.View", "View IPMS target library"),
            Permission("IPMS", "Library", "Create", "IPMS.Library.Create", "Create IPMS target templates"),
            Permission("IPMS", "Library", "Edit", "IPMS.Library.Edit", "Edit IPMS target templates"),
            Permission("IPMS", "Library", "Delete", "IPMS.Library.Delete", "Archive or delete IPMS target templates"),
            Permission("IPMS", "Library", "Duplicate", "IPMS.Library.Duplicate", "Duplicate IPMS target templates"),
            Permission("IPMS", "Library", "UseTemplate", "IPMS.Library.UseTemplate", "Create IPMS targets from library templates"),
            Permission("IPMS", "Targets", "View", "IPMS.Targets.View", "View IPMS targets"),
            Permission("IPMS", "Targets", "Create", "IPMS.Targets.Create", "Create IPMS targets"),
            Permission("IPMS", "Targets", "Edit", "IPMS.Targets.Edit", "Edit IPMS targets"),
            Permission("IPMS", "Targets", "Delete", "IPMS.Targets.Delete", "Delete IPMS targets"),
            Permission("IPMS", "Targets", "Archive", "IPMS.Targets.Archive", "Archive IPMS targets"),
            Permission("IPMS", "Submissions", "View", "IPMS.Submissions.View", "View IPMS submissions"),
            Permission("IPMS", "Submissions", "Create", "IPMS.Submissions.Create", "Create IPMS submissions"),
            Permission("IPMS", "Submissions", "Edit", "IPMS.Submissions.Edit", "Edit IPMS submissions"),
            Permission("IPMS", "Submissions", "Delete", "IPMS.Submissions.Delete", "Delete IPMS submissions"),
            Permission("IPMS", "Submissions", "Submit", "IPMS.Submissions.Submit", "Submit IPMS submissions"),
            Permission("IPMS", "Submissions", "Verify", "IPMS.Submissions.Verify", "Verify IPMS submissions"),
            Permission("IPMS", "Submissions", "VerifyReject", "IPMS.Submissions.VerifyReject", "Verify reject IPMS submissions"),
            Permission("IPMS", "Submissions", "Approve", "IPMS.Submissions.Approve", "Approve IPMS submissions"),
            Permission("IPMS", "Submissions", "Reject", "IPMS.Submissions.Reject", "Reject IPMS submissions"),
            Permission("IPMS", "Submissions", "Review", "IPMS.Submissions.Review", "Review IPMS submissions"),
            Permission("IPMS", "Submissions", "Audit", "IPMS.Submissions.Audit", "Audit IPMS submissions"),
            Permission("IPMS", "Submissions", "Score", "IPMS.Submissions.Score", "Score IPMS submissions"),
            Permission("IPMS", "POE", "Upload", "IPMS.POE.Upload", "Upload IPMS POE files"),
            Permission("IPMS", "Submissions", "ExtendDueDate", "IPMS.Submissions.ExtendDueDate", "Extend IPMS submission due dates"),
            Permission("Targets", "Planning", "View", "Targets.View", "View targets"),
            Permission("Targets", "Planning", "Manage", "Targets.Manage", "Manage targets"),
            Permission("OPMS", "Targets", "View", "OPMS.View", "View OPMS"),
            Permission("OPMS", "Targets", "Create", "OPMS.Create", "Create OPMS targets"),
            Permission("OPMS", "Targets", "Edit", "OPMS.Edit", "Edit OPMS targets"),
            Permission("OPMS", "Targets", "Delete", "OPMS.Delete", "Delete OPMS targets"),
            Permission("OPMS", "Workflow", "Submit", "OPMS.Submit", "Submit OPMS items"),
            Permission("OPMS", "Workflow", "Verify", "OPMS.Verify", "Verify OPMS items"),
            Permission("OPMS", "Workflow", "Approve", "OPMS.Approve", "Approve OPMS items"),
            Permission("IPMS", "Targets", "View", "IPMS.View", "View IPMS"),
            Permission("IPMS", "Targets", "Create", "IPMS.Create", "Create IPMS targets"),
            Permission("IPMS", "Targets", "Edit", "IPMS.Edit", "Edit IPMS targets"),
            Permission("IPMS", "Targets", "Delete", "IPMS.Delete", "Delete IPMS targets"),
            Permission("IPMS", "Workflow", "Submit", "IPMS.Submit", "Submit IPMS items"),
            Permission("IPMS", "Workflow", "Verify", "IPMS.Verify", "Verify IPMS items"),
            Permission("IPMS", "Workflow", "Approve", "IPMS.Approve", "Approve IPMS items"),
            Permission("Workflow", "Submission", "View", "Workflow.Submit.View", "View submitter work queues"),
            Permission("Workflow", "Verification", "View", "Workflow.Verify.View", "View verification queues"),
            Permission("Workflow", "Review", "View", "Workflow.Review.View", "View review queues"),
            Permission("Workflow", "Approval", "View", "Workflow.Approve.View", "View approval queues"),
            Permission("Workflow", "Audit", "View", "Workflow.Audit.View", "View audit queues"),
            Permission("Actuals", "Performance", "View", "Actuals.View", "View performance actuals"),
            Permission("Actuals", "Performance", "Submit", "Actuals.Submit", "Submit performance actuals"),
            Permission("Actuals", "Performance", "Edit", "Actuals.Edit", "Edit submitted actuals"),
            Permission("Scores", "Performance", "View", "Scores.View", "View scores"),
            Permission("Scores", "Performance", "Edit", "Scores.Edit", "Edit scores"),
            Permission("Comments", "Performance", "Add", "Comments.Add", "Add review comments"),
            Permission("POE", "Evidence", "View", "POE.View", "View proofs of evidence"),
            Permission("POE", "Evidence", "Upload", "POE.Upload", "Upload proofs of evidence"),
            Permission("Audit", "Trails", "View", "Audit.Trails.View", "View audit trail entries"),
            Permission("Findings", "Audit", "Manage", "Findings.Manage", "Manage internal audit findings"),
            Permission("Recommendations", "Audit", "Manage", "Recommendations.Manage", "Manage internal audit recommendations"),
            Permission("Reports", "Institution", "View", "Reports.View", "View reports"),
            Permission("Reports", "Institution", "Generate", "Reports.Generate", "Generate reports"),
            Permission("Reports", "Department", "View", "Reports.Department.View", "View department reports"),
            Permission("Reports", "Approvals", "View", "Reports.Approval.View", "View approval reports"),
            Permission("Reports", "Verification", "View", "Reports.Verification.View", "View verification reports"),
            Permission("Reports", "InternalAudit", "View", "Reports.InternalAudit.View", "View internal audit reports"),
            Permission("Reports", "Institution", "Summary", "Reports.Institution.View", "View institutional performance reports"),
            Permission("VersionLogs", "Audit", "View", "VersionLogs.View", "View version history logs")
        ];
    }

    private static Dictionary<string, string[]> BuildRolePermissionMap()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [SecurityModel.Admin] = BaseAccess(
                "Admin.Users.Manage",
                "UserDirectory.View",
                "Departments.View", "Departments.Manage",
                "Units.View", "Units.Manage",
                "Configuration.View", "Configuration.Manage",
                "Notifications.View", "Notifications.Manage",
                "Targets.View", "Targets.Manage",
                "OPMS.View", "OPMS.Create", "OPMS.Edit", "OPMS.Delete",
                "OPMS.Targets.View", "OPMS.Targets.Create", "OPMS.Targets.Edit", "OPMS.Targets.Delete", "OPMS.Targets.Archive",
                "OPMS.Submissions.View", "OPMS.Submissions.Create", "OPMS.Submissions.Edit", "OPMS.Submissions.Delete", "OPMS.Submissions.Submit", "OPMS.Submissions.Verify", "OPMS.Submissions.VerifyReject", "OPMS.Submissions.Approve", "OPMS.Submissions.Reject", "OPMS.Submissions.Review", "OPMS.Submissions.Audit", "OPMS.Submissions.Score", "OPMS.Submissions.ExtendDueDate",
                "OPMS.POE.Upload",
                "IPMS.View", "IPMS.Create", "IPMS.Edit", "IPMS.Delete",
                "IPMS.Targets.View", "IPMS.Targets.Create", "IPMS.Targets.Edit", "IPMS.Targets.Delete", "IPMS.Targets.Archive",
                "IPMS.Submissions.View", "IPMS.Submissions.Create", "IPMS.Submissions.Edit", "IPMS.Submissions.Delete", "IPMS.Submissions.Submit", "IPMS.Submissions.Verify", "IPMS.Submissions.VerifyReject", "IPMS.Submissions.Approve", "IPMS.Submissions.Reject", "IPMS.Submissions.Review", "IPMS.Submissions.Audit", "IPMS.Submissions.Score", "IPMS.Submissions.ExtendDueDate",
                "IPMS.POE.Upload",
                "OPMS.Library.View", "OPMS.Library.Create", "OPMS.Library.Edit", "OPMS.Library.Delete", "OPMS.Library.Duplicate", "OPMS.Library.UseTemplate",
                "IPMS.Library.View", "IPMS.Library.Create", "IPMS.Library.Edit", "IPMS.Library.Delete", "IPMS.Library.Duplicate", "IPMS.Library.UseTemplate",
                "Reports.View", "Reports.Generate",
                "RoleImplementationAudit.View",
                "SystemAdministration.RoleAccessMatrix.View",
                "SystemAdministration.PermissionSimulation.View",
                "SystemAdministration.SystemCoverageAudit.View",
                "SystemAdministration.AuditTrail.View"),
            [SecurityModel.ClientAdmin] = BaseAccess("UserDirectory.View", "Audit.Reports.View", "Reports.View", "SystemAdministration.AuditTrail.View"),
            [SecurityModel.AuditorGeneral] = BaseAccess(
                "Targets.View", "OPMS.View", "IPMS.View", "OPMS.Targets.View", "IPMS.Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Library.View", "IPMS.Library.View",
                "Actuals.View", "Scores.View", "POE.View",
                "Workflow.Audit.View", "Reports.View", "Reports.Institution.View",
                "Reports.InternalAudit.View", "Audit.Logs.View", "Audit.Reports.View", "Audit.Trails.View",
                "VersionLogs.View", "SystemAdministration.AuditTrail.View"),
            [SecurityModel.MunicipalManager] = BaseAccess(
                "Targets.View", "OPMS.View", "IPMS.View", "OPMS.Targets.View", "OPMS.Submissions.View", "OPMS.Submissions.Submit", "OPMS.Submissions.Approve", "OPMS.Submissions.Reject", "OPMS.Submissions.Score", "Actuals.View", "Actuals.Submit",
                "Workflow.Submit.View", "Workflow.Approve.View", "Scores.Edit",
                "POE.View", "POE.Upload", "OPMS.POE.Upload", "Notifications.View", "Notifications.TriggerNotification", "Reports.Institution.View", "Reports.Generate"),
            [SecurityModel.InternalAudit] = BaseAccess(
                "Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Audit", "IPMS.Submissions.Audit", "OPMS.Submissions.Score", "IPMS.Submissions.Score", "Actuals.View", "POE.View", "Workflow.Audit.View",
                "Findings.Manage", "Recommendations.Manage", "Scores.Edit",
                "Reports.InternalAudit.View", "Audit.Reports.View", "Audit.Logs.View",
                "VersionLogs.View", "Notifications.TriggerNotification", "Audit.Trails.View", "SystemAdministration.AuditTrail.View"),
            [SecurityModel.Reviewer] = BaseAccess(
                "Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Review", "IPMS.Submissions.Review", "OPMS.Submissions.Score", "IPMS.Submissions.Score", "Actuals.View", "POE.View", "Workflow.Review.View",
                "Comments.Add", "Scores.Edit", "Reports.View", "Notifications.TriggerNotification"),
            [SecurityModel.KpiApprover] = BaseAccess(
                "Targets.View", "OPMS.Library.View", "IPMS.Library.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Approve", "IPMS.Submissions.Approve", "OPMS.Submissions.Reject", "IPMS.Submissions.Reject", "OPMS.Submissions.Score", "IPMS.Submissions.Score", "Actuals.View", "POE.View",
                "Workflow.Approve.View", "Scores.Edit", "Notifications.View",
                "Reports.Approval.View"),
            [SecurityModel.HeadOfDepartment] = BaseAccess(
                "Targets.View", "OPMS.Targets.View", "IPMS.Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Edit", "IPMS.Submissions.Edit", "OPMS.Submissions.Approve", "IPMS.Submissions.Approve", "OPMS.Submissions.Reject", "IPMS.Submissions.Reject", "OPMS.Submissions.Score", "IPMS.Submissions.Score", "Actuals.View", "Actuals.Edit", "POE.View", "POE.Upload", "OPMS.POE.Upload", "IPMS.POE.Upload",
                "Workflow.Approve.View", "Comments.Add", "Notifications.View",
                "Reports.Department.View", "Audit.Trail.View", "Notifications.TriggerNotification", "SystemAdministration.AuditTrail.View"),
            [SecurityModel.DeputyHeadOfDepartment] = BaseAccess(
                "Targets.View", "OPMS.Targets.View", "IPMS.Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Edit", "IPMS.Submissions.Edit", "OPMS.Submissions.Approve", "IPMS.Submissions.Approve", "OPMS.Submissions.Reject", "IPMS.Submissions.Reject", "OPMS.Submissions.Score", "IPMS.Submissions.Score", "Actuals.View", "Actuals.Edit", "POE.View", "POE.Upload", "OPMS.POE.Upload", "IPMS.POE.Upload",
                "Workflow.Approve.View", "Comments.Add", "Notifications.View",
                "Reports.Department.View", "Audit.Trail.View", "SystemAdministration.AuditTrail.View"),
            [SecurityModel.Verifier] = BaseAccess(
                "Targets.View", "OPMS.Targets.View", "IPMS.Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Edit", "IPMS.Submissions.Edit", "OPMS.Submissions.Verify", "IPMS.Submissions.Verify", "OPMS.Submissions.VerifyReject", "IPMS.Submissions.VerifyReject", "Actuals.View", "Actuals.Edit", "POE.View", "POE.Upload", "OPMS.POE.Upload", "IPMS.POE.Upload",
                "Workflow.Verify.View", "Comments.Add", "Notifications.View",
                "Reports.Verification.View", "SystemAdministration.AuditTrail.View"),
            [SecurityModel.Submitter] = BaseAccess(
                "Targets.View", "OPMS.Targets.View", "IPMS.Targets.View", "OPMS.Submissions.View", "IPMS.Submissions.View", "OPMS.Submissions.Submit", "IPMS.Submissions.Submit", "OPMS.Submissions.Score", "IPMS.Submissions.Score", "Actuals.View", "Actuals.Submit", "POE.View", "POE.Upload", "OPMS.POE.Upload", "IPMS.POE.Upload",
                "Workflow.Submit.View", "Scores.Edit", "Notifications.View")
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

    private static Permission Permission(string module, string feature, string action, string code, string description)
    {
        return new Permission
        {
            Module = module,
            Feature = feature,
            Action = action,
            Code = code,
            Description = description,
            IsActive = true
        };
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

    private static IReadOnlyList<RoleSeed> GetRoleSeeds()
    {
        return
        [
            new(SecurityModel.SuperAdmin, "Platform-wide administration across all modules and security functions.", true),
            new(SecurityModel.Admin, "Operational administration for users, departments, units, setup data, and plans.", true),
            new(SecurityModel.ClientAdmin, "Read-only client administration focused on user directory and audit reporting.", true),
            new(SecurityModel.AuditorGeneral, "Read-only oversight across the institution for external assurance.", true),
            new(SecurityModel.MunicipalManager, "Institution-wide performance review and approval role.", false),
            new(SecurityModel.InternalAudit, "Institution-wide internal audit role.", false),
            new(SecurityModel.Reviewer, "Institution-wide performance review role.", false),
            new(SecurityModel.KpiApprover, "Approver role scoped by institution, department, or assigned KPI.", false),
            new(SecurityModel.HeadOfDepartment, "Department-level performance authority.", false),
            new(SecurityModel.DeputyHeadOfDepartment, "Department-level delegated authority.", false),
            new(SecurityModel.Verifier, "Verification role scoped by department, unit, or assigned KPI.", false),
            new(SecurityModel.Submitter, "Submission role scoped by department, unit, or assigned KPI.", false)
        ];
    }

    private static IReadOnlyList<DepartmentSeed> GetDepartmentSeeds()
    {
        return
        [
            new("OMM", "Office of the Municipal Manager", "Executive leadership and institutional performance oversight",
            [
                new UnitSeed("OMM-EXEC", "Executive Support"),
                new UnitSeed("OMM-PMO", "Performance Management Office")
            ]),
            new("SPP", "Strategic Planning and Performance", "Institutional planning, KPI coordination, and performance review",
            [
                new UnitSeed("SPP-PLAN", "Strategic Planning"),
                new UnitSeed("SPP-ME", "Monitoring and Evaluation")
            ]),
            new("BTO", "Budget and Treasury Office", "Budget, treasury, and financial governance",
            [
                new UnitSeed("BTO-FIN", "Financial Reporting"),
                new UnitSeed("BTO-SCM", "Supply Chain Management")
            ]),
            new("ITS", "Infrastructure and Technical Services", "Infrastructure delivery and technical maintenance",
            [
                new UnitSeed("ITS-ROADS", "Roads and Stormwater"),
                new UnitSeed("ITS-ELEC", "Electricity and Energy")
            ]),
            new("COM", "Community Services", "Community operations and public amenities",
            [
                new UnitSeed("COM-WASTE", "Waste Management"),
                new UnitSeed("COM-PARKS", "Parks and Recreation")
            ]),
            new("IAR", "Internal Audit and Risk", "Internal audit, assurance, and risk oversight",
            [
                new UnitSeed("IAR-AUD", "Internal Audit"),
                new UnitSeed("IAR-RISK", "Risk Management")
            ]),
            new("ICT", "ICT Services", "Enterprise systems, infrastructure, and digital support",
            [
                new UnitSeed("ICT-OPS", "Infrastructure Operations"),
                new UnitSeed("ICT-APP", "Business Applications")
            ]),
            new("CORP", "Corporate Services", "Corporate governance and support services",
            [
                new UnitSeed("CORP-HR", "Human Resources"),
                new UnitSeed("CORP-LEGAL", "Legal Services")
            ]),
            new("WAT", "Water Services", "Water services delivery and network operations",
            [
                new UnitSeed("WAT-PROD", "Water Production"),
                new UnitSeed("WAT-DIST", "Water Distribution")
            ])
        ];
    }

    private static IReadOnlyList<DemoUserSeed> GetDemoUserSeeds()
    {
        return
        [
            new(SecurityModel.SuperAdmin, "Lethabo", "Khumalo", "lethabo.khumalo", "lethabo.khumalo@bluehills.gov.za", "0825550101", "OMM", "OMM-EXEC", "Enterprise Systems Director", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.SuperAdmin, "Annelie", "Bothma", "annelie.bothma", "annelie.bothma@bluehills.gov.za", "0825550102", "ICT", "ICT-APP", "Digital Governance Lead", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.SuperAdmin, "Sipho", "Nene", "sipho.nene", "sipho.nene@bluehills.gov.za", "0825550103", "CORP", "CORP-HR", "Platform Security Executive", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.Admin, "Thandiwe", "Maseko", "thandiwe.maseko", "thandiwe.maseko@bluehills.gov.za", "0825550201", "SPP", "SPP-PLAN", "Enterprise Admin Manager", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.Admin, "Kabelo", "Mahlangu", "kabelo.mahlangu", "kabelo.mahlangu@bluehills.gov.za", "0825550202", "CORP", "CORP-HR", "Setup Data Administrator", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.Admin, "Melissa", "Jacobs", "melissa.jacobs", "melissa.jacobs@bluehills.gov.za", "0825550203", "ICT", "ICT-OPS", "Notifications Administrator", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.ClientAdmin, "Koketso", "Molefe", "koketso.molefe", "koketso.molefe@bluehills.gov.za", "0825550301", "OMM", "OMM-EXEC", "Client Liaison Administrator", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.ClientAdmin, "Johan", "Steyn", "johan.steyn", "johan.steyn@bluehills.gov.za", "0825550302", "CORP", "CORP-LEGAL", "Audit Reporting Coordinator", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.ClientAdmin, "Nosipho", "Mbatha", "nosipho.mbatha", "nosipho.mbatha@bluehills.gov.za", "0825550303", "BTO", "BTO-FIN", "Client Assurance Analyst", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.AuditorGeneral, "Maria", "Fernandez", "maria.fernandez", "maria.fernandez@bluehills.gov.za", "0825550401", "OMM", "OMM-EXEC", "External Audit Lead", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.AuditorGeneral, "Bongani", "Mabena", "bongani.mabena", "bongani.mabena@bluehills.gov.za", "0825550402", "IAR", "IAR-AUD", "Assurance Specialist", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.AuditorGeneral, "Tracey", "Adams", "tracey.adams", "tracey.adams@bluehills.gov.za", "0825550403", "CORP", "CORP-LEGAL", "Audit Oversight Analyst", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.MunicipalManager, "Mandla", "Dube", "mandla.dube", "mandla.dube@bluehills.gov.za", "0825550501", "OMM", "OMM-EXEC", "Municipal Manager", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.MunicipalManager, "Zanele", "Mokoena", "zanele.mokoena", "zanele.mokoena@bluehills.gov.za", "0825550502", "OMM", "OMM-PMO", "Acting Municipal Manager", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.MunicipalManager, "Gerhard", "Swanepoel", "gerhard.swanepoel", "gerhard.swanepoel@bluehills.gov.za", "0825550503", "SPP", "SPP-PLAN", "Institutional Performance Sponsor", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.InternalAudit, "Azwi", "Netshifhefhe", "azwi.netshifhefhe", "azwi.netshifhefhe@bluehills.gov.za", "0825550601", "IAR", "IAR-AUD", "Internal Audit Manager", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.InternalAudit, "Riaan", "Pretorius", "riaan.pretorius", "riaan.pretorius@bluehills.gov.za", "0825550602", "IAR", "IAR-RISK", "Risk and Assurance Manager", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.InternalAudit, "Fatima", "Ismail", "fatima.ismail", "fatima.ismail@bluehills.gov.za", "0825550603", "IAR", "IAR-AUD", "Audit Quality Lead", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.Reviewer, "Nomvula", "Mkhize", "nomvula.mkhize", "nomvula.mkhize@bluehills.gov.za", "0825550701", "SPP", "SPP-ME", "Senior Performance Reviewer", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.Reviewer, "Francois", "du Plessis", "francois.duplessis", "francois.duplessis@bluehills.gov.za", "0825550702", "BTO", "BTO-FIN", "Financial Reviewer", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),
            new(SecurityModel.Reviewer, "Boitumelo", "Radebe", "boitumelo.radebe", "boitumelo.radebe@bluehills.gov.za", "0825550703", "OMM", "OMM-PMO", "Institutional Review Officer", [new DemoScopeSeed(ScopeType.InstitutionScope)], []),

            new(SecurityModel.KpiApprover, "Priya", "Naidoo", "priya.naidoo", "priya.naidoo@bluehills.gov.za", "0825550801", "SPP", "SPP-PLAN", "KPI Approval Lead", [new DemoScopeSeed(ScopeType.InstitutionScope)], [new DemoAssignmentSeed(AssignmentType.AdditionalApproverAssignment, KpiId: "KPI-INST-001")]),
            new(SecurityModel.KpiApprover, "Themba", "Zulu", "themba.zulu", "themba.zulu@bluehills.gov.za", "0825550802", "ITS", "ITS-ROADS", "Technical KPI Approver", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "ITS"), new DemoScopeSeed(ScopeType.AssignedKpiScope, DepartmentCode: "ITS", KpiId: "KPI-ITS-014")], [new DemoAssignmentSeed(AssignmentType.AdditionalApproverAssignment, KpiId: "KPI-ITS-014")]),
            new(SecurityModel.KpiApprover, "Megan", "van Wyk", "megan.vanwyk", "megan.vanwyk@bluehills.gov.za", "0825550803", "WAT", "WAT-DIST", "Water KPI Approver", [new DemoScopeSeed(ScopeType.AssignedKpiScope, DepartmentCode: "WAT", UnitCode: "WAT-DIST", KpiId: "KPI-WAT-021")], [new DemoAssignmentSeed(AssignmentType.AdditionalApproverAssignment, KpiId: "KPI-WAT-021")]),

            new(SecurityModel.HeadOfDepartment, "Thabo", "Mokoena", "thabo.mokoena", "thabo.mokoena@bluehills.gov.za", "0825550901", "ITS", "ITS-ROADS", "Head of Infrastructure and Technical Services", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "ITS")], []),
            new(SecurityModel.HeadOfDepartment, "Lindiwe", "Mahlangu", "lindiwe.mahlangu", "lindiwe.mahlangu@bluehills.gov.za", "0825550902", "COM", "COM-WASTE", "Head of Community Services", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "COM")], []),
            new(SecurityModel.HeadOfDepartment, "Ahmed", "Hassan", "ahmed.hassan", "ahmed.hassan@bluehills.gov.za", "0825550903", "BTO", "BTO-FIN", "Chief Financial Officer", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "BTO")], []),

            new(SecurityModel.DeputyHeadOfDepartment, "Nomsa", "Dlamini", "nomsa.dlamini", "nomsa.dlamini@bluehills.gov.za", "0825551001", "ITS", "ITS-ELEC", "Deputy Director Technical Services", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "ITS")], []),
            new(SecurityModel.DeputyHeadOfDepartment, "Keitumetse", "Mabaso", "keitumetse.mabaso", "keitumetse.mabaso@bluehills.gov.za", "0825551002", "COM", "COM-PARKS", "Deputy Director Community Services", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "COM")], []),
            new(SecurityModel.DeputyHeadOfDepartment, "Bradley", "Peters", "bradley.peters", "bradley.peters@bluehills.gov.za", "0825551003", "WAT", "WAT-PROD", "Deputy Director Water Services", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "WAT")], []),

            new(SecurityModel.Verifier, "Ayabonga", "Cele", "ayabonga.cele", "ayabonga.cele@bluehills.gov.za", "0825551101", "SPP", "SPP-ME", "Institutional Verifier", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "SPP"), new DemoScopeSeed(ScopeType.AssignedKpiScope, DepartmentCode: "SPP", KpiId: "KPI-SPP-008")], [new DemoAssignmentSeed(AssignmentType.AdditionalVerifierAssignment, KpiId: "KPI-SPP-008")]),
            new(SecurityModel.Verifier, "Reneilwe", "Mogale", "reneilwe.mogale", "reneilwe.mogale@bluehills.gov.za", "0825551102", "BTO", "BTO-SCM", "Financial Data Verifier", [new DemoScopeSeed(ScopeType.UnitScope, DepartmentCode: "BTO", UnitCode: "BTO-SCM")], [new DemoAssignmentSeed(AssignmentType.AdditionalVerifierAssignment, TargetId: "TGT-BTO-003")]),
            new(SecurityModel.Verifier, "Willem", "Nel", "willem.nel", "willem.nel@bluehills.gov.za", "0825551103", "ITS", "ITS-ROADS", "Technical Verifier", [new DemoScopeSeed(ScopeType.AssignedKpiScope, DepartmentCode: "ITS", UnitCode: "ITS-ROADS", KpiId: "KPI-ITS-014")], [new DemoAssignmentSeed(AssignmentType.AdditionalVerifierAssignment, KpiId: "KPI-ITS-014")]),

            new(SecurityModel.Submitter, "Mpho", "Madonsela", "mpho.madonsela", "mpho.madonsela@bluehills.gov.za", "0825551201", "SPP", "SPP-PLAN", "Institutional Submitter", [new DemoScopeSeed(ScopeType.DepartmentScope, DepartmentCode: "SPP")], [new DemoAssignmentSeed(AssignmentType.AdditionalSubmitterAssignment, TargetId: "TGT-SPP-001")]),
            new(SecurityModel.Submitter, "Helena", "Visagie", "helena.visagie", "helena.visagie@bluehills.gov.za", "0825551202", "WAT", "WAT-DIST", "Water Performance Submitter", [new DemoScopeSeed(ScopeType.UnitScope, DepartmentCode: "WAT", UnitCode: "WAT-DIST")], [new DemoAssignmentSeed(AssignmentType.AdditionalSubmitterAssignment, KpiId: "KPI-WAT-021")]),
            new(SecurityModel.Submitter, "Sizwe", "Nxumalo", "sizwe.nxumalo", "sizwe.nxumalo@bluehills.gov.za", "0825551203", "ITS", "ITS-ELEC", "Infrastructure Submitter", [new DemoScopeSeed(ScopeType.AssignedKpiScope, DepartmentCode: "ITS", UnitCode: "ITS-ELEC", KpiId: "KPI-ITS-019")], [new DemoAssignmentSeed(AssignmentType.AdditionalSubmitterAssignment, KpiId: "KPI-ITS-019")])
        ];
    }

    private sealed record RoleSeed(string Name, string Description, bool IsSystemRole);

    private sealed record DepartmentSeed(string Code, string Name, string Description, IReadOnlyList<UnitSeed> Units);

    private sealed record UnitSeed(string Code, string Name);

    private sealed record DemoScopeSeed(
        ScopeType ScopeType,
        string? DepartmentCode = null,
        string? UnitCode = null,
        string? TargetId = null,
        string? KpiId = null,
        string? ProjectId = null,
        string? TaskId = null);

    private sealed record DemoAssignmentSeed(
        AssignmentType AssignmentType,
        string? TargetId = null,
        string? KpiId = null,
        string? ProjectId = null,
        string? TaskId = null);

    private sealed record DemoUserSeed(
        string Role,
        string FirstName,
        string LastName,
        string UserName,
        string Email,
        string PhoneNumber,
        string DepartmentCode,
        string? UnitCode,
        string Position,
        IReadOnlyList<DemoScopeSeed> Scopes,
        IReadOnlyList<DemoAssignmentSeed> Assignments);

    private static async Task SeedLookupTablesAsync(ApplicationDbContext context)
    {
        // Seed Periods
        var existingPeriods = await context.Periods.ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var periodSeed in GetPeriodSeeds())
        {
            if (!existingPeriods.TryGetValue(periodSeed.Code, out var period))
            {
                period = new Period
                {
                    Code = periodSeed.Code,
                    Name = periodSeed.Name,
                    StartDate = periodSeed.StartDate,
                    EndDate = periodSeed.EndDate,
                    FiscalYear = periodSeed.FiscalYear,
                    IsActive = periodSeed.IsActive
                };
                context.Periods.Add(period);
            }
            else
            {
                period.Code = periodSeed.Code;
                period.Name = periodSeed.Name;
                period.StartDate = periodSeed.StartDate;
                period.EndDate = periodSeed.EndDate;
                period.FiscalYear = periodSeed.FiscalYear;
                period.IsActive = periodSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Strategic Goals
        var existingGoals = await context.StrategicGoals.ToDictionaryAsync(g => g.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var goalSeed in GetStrategicGoalSeeds())
        {
            if (!existingGoals.TryGetValue(goalSeed.Code, out var goal))
            {
                goal = new StrategicGoal
                {
                    Name = goalSeed.Name,
                    Code = goalSeed.Code,
                    Description = goalSeed.Description,
                    IsActive = goalSeed.IsActive
                };
                context.StrategicGoals.Add(goal);
            }
            else
            {
                goal.Name = goalSeed.Name;
                goal.Description = goalSeed.Description;
                goal.IsActive = goalSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Strategic Objectives
        var goals = await context.StrategicGoals.ToDictionaryAsync(g => g.Code, StringComparer.OrdinalIgnoreCase);
        var existingObjectives = await context.StrategicObjectives.ToDictionaryAsync(o => o.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var objSeed in GetStrategicObjectiveSeeds())
        {
            if (!existingObjectives.TryGetValue(objSeed.Code, out var obj))
            {
                obj = new StrategicObjective
                {
                    Name = objSeed.Name,
                    Code = objSeed.Code,
                    StrategicGoalId = goals[objSeed.StrategicGoalCode].Id,
                    IsActive = objSeed.IsActive
                };
                context.StrategicObjectives.Add(obj);
            }
            else
            {
                obj.Name = objSeed.Name;
                obj.StrategicGoalId = goals[objSeed.StrategicGoalCode].Id;
                obj.IsActive = objSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Budget Sources
        var existingBudgetSources = await context.BudgetSources.ToDictionaryAsync(b => b.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var sourceSeed in GetBudgetSourceSeeds())
        {
            if (!existingBudgetSources.TryGetValue(sourceSeed.Code, out var source))
            {
                source = new BudgetSource
                {
                    Name = sourceSeed.Name,
                    Code = sourceSeed.Code,
                    IsActive = sourceSeed.IsActive
                };
                context.BudgetSources.Add(source);
            }
            else
            {
                source.Name = sourceSeed.Name;
                source.IsActive = sourceSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Budget Types
        var existingBudgetTypes = await context.BudgetTypes.ToDictionaryAsync(b => b.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var typeSeed in GetBudgetTypeSeeds())
        {
            if (!existingBudgetTypes.TryGetValue(typeSeed.Code, out var type))
            {
                type = new BudgetType
                {
                    Name = typeSeed.Name,
                    Code = typeSeed.Code,
                    IsActive = typeSeed.IsActive
                };
                context.BudgetTypes.Add(type);
            }
            else
            {
                type.Name = typeSeed.Name;
                type.IsActive = typeSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Units of Measure
        var existingUoms = await context.UnitOfMeasures.ToDictionaryAsync(u => u.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var uomSeed in GetUnitOfMeasureSeeds())
        {
            if (!existingUoms.TryGetValue(uomSeed.Code, out var uom))
            {
                uom = new UnitOfMeasure
                {
                    Name = uomSeed.Name,
                    Code = uomSeed.Code,
                    Symbol = uomSeed.Symbol,
                    IsActive = uomSeed.IsActive
                };
                context.UnitOfMeasures.Add(uom);
            }
            else
            {
                uom.Name = uomSeed.Name;
                uom.Symbol = uomSeed.Symbol;
                uom.IsActive = uomSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Wards
        var existingWards = await context.Wards.ToDictionaryAsync(w => w.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var wardSeed in GetWardSeeds())
        {
            if (!existingWards.TryGetValue(wardSeed.Code, out var ward))
            {
                ward = new Ward
                {
                    Name = wardSeed.Name,
                    Code = wardSeed.Code,
                    Municipality = wardSeed.Municipality,
                    IsActive = wardSeed.IsActive
                };
                context.Wards.Add(ward);
            }
            else
            {
                ward.Name = wardSeed.Name;
                ward.Municipality = wardSeed.Municipality;
                ward.IsActive = wardSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();

        // Seed Vote Numbers
        var departments = await context.Departments.ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var existingVotes = await context.VoteNumbers.ToDictionaryAsync(v => v.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var voteSeed in GetVoteNumberSeeds())
        {
            if (!existingVotes.TryGetValue(voteSeed.Code, out var vote))
            {
                vote = new VoteNumber
                {
                    Code = voteSeed.Code,
                    Number = voteSeed.Code,
                    Name = voteSeed.Name,
                    DepartmentId = voteSeed.DepartmentCode != null ? departments[voteSeed.DepartmentCode].Id : null,
                    Amount = voteSeed.Amount,
                    IsActive = voteSeed.IsActive
                };
                context.VoteNumbers.Add(vote);
            }
            else
            {
                vote.Number = voteSeed.Code;
                vote.Name = voteSeed.Name;
                vote.DepartmentId = voteSeed.DepartmentCode != null ? departments[voteSeed.DepartmentCode].Id : null;
                vote.Amount = voteSeed.Amount;
                vote.IsActive = voteSeed.IsActive;
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedTargetsAndSubmissionsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Get all lookup data
        var periods = await context.Periods.ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var departments = await context.Departments.ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var units = await context.Units.ToDictionaryAsync(u => $"{u.DepartmentId}-{u.Code}", StringComparer.OrdinalIgnoreCase);
        var goals = await context.StrategicGoals.ToDictionaryAsync(g => g.Code, StringComparer.OrdinalIgnoreCase);
        var objectives = await context.StrategicObjectives.ToDictionaryAsync(o => o.Code, StringComparer.OrdinalIgnoreCase);
        var budgetSources = await context.BudgetSources.ToDictionaryAsync(b => b.Code, StringComparer.OrdinalIgnoreCase);
        var budgetTypes = await context.BudgetTypes.ToDictionaryAsync(b => b.Code, StringComparer.OrdinalIgnoreCase);
        var uoms = await context.UnitOfMeasures.ToDictionaryAsync(u => u.Code, StringComparer.OrdinalIgnoreCase);
        var users = await userManager.Users.ToDictionaryAsync(u => u.UserName!, StringComparer.OrdinalIgnoreCase);

        // Seed OPMS Targets
        var existingOpmsTargets = await context.OpmsTargets.ToDictionaryAsync(t => t.IndicatorNumber, StringComparer.OrdinalIgnoreCase);
        foreach (var targetSeed in GetOpmsTargetSeeds())
        {
            if (!existingOpmsTargets.TryGetValue(targetSeed.IndicatorNumber, out var target))
            {
                target = new OpmsTarget
                {
                    IndicatorNumber = targetSeed.IndicatorNumber,
                    PeriodId = periods[targetSeed.PeriodCode].Id,
                    DepartmentId = targetSeed.DepartmentCode != null ? departments[targetSeed.DepartmentCode].Id : null,
                    UnitId = targetSeed.UnitCode != null ? units[$"{departments[targetSeed.DepartmentCode!].Id}-{targetSeed.UnitCode}"].Id : null,
                    NationalKpa = targetSeed.NationalKpa,
                    MunicipalKpa = targetSeed.MunicipalKpa,
                    StrategicGoalId = targetSeed.StrategicGoalCode != null ? goals[targetSeed.StrategicGoalCode].Id : null,
                    StrategicObjectiveId = targetSeed.StrategicObjectiveCode != null ? objectives[targetSeed.StrategicObjectiveCode].Id : null,
                    PerformanceObjective = targetSeed.PerformanceObjective,
                    TargetName = targetSeed.TargetName,
                    KpiDescription = targetSeed.KpiDescription,
                    Baseline = targetSeed.Baseline,
                    AnnualTarget = targetSeed.AnnualTarget,
                    AnnualTargetDescription = targetSeed.AnnualTargetDescription,
                    BudgetSourceId = targetSeed.BudgetSourceCode != null ? budgetSources[targetSeed.BudgetSourceCode].Id : null,
                    BudgetTypeId = targetSeed.BudgetTypeCode != null ? budgetTypes[targetSeed.BudgetTypeCode].Id : null,
                    UnitOfMeasureId = targetSeed.UnitOfMeasureCode != null ? uoms[targetSeed.UnitOfMeasureCode].Id : null,
                    Weight = targetSeed.Weight,
                    KpiType = targetSeed.KpiType,
                    IndicatorType = targetSeed.IndicatorType,
                    FunctionalArea = targetSeed.FunctionalArea,
                    Q1Target = targetSeed.Q1Target,
                    Q2Target = targetSeed.Q2Target,
                    MidTermTarget = targetSeed.MidTermTarget,
                    Q3Target = targetSeed.Q3Target,
                    Q4Target = targetSeed.Q4Target,
                    TargetUnitType = targetSeed.TargetUnitType,
                    IsRevised = false,
                    IsWithdrawn = false
                };
                context.OpmsTargets.Add(target);
            }
        }
        await context.SaveChangesAsync();

        // Seed IPMS Targets
        var opmsTargets = await context.OpmsTargets.ToDictionaryAsync(t => t.IndicatorNumber, StringComparer.OrdinalIgnoreCase);
        var existingIpmsTargets = await context.IpmsTargets.ToDictionaryAsync(t => t.IndicatorNumber, StringComparer.OrdinalIgnoreCase);
        foreach (var targetSeed in GetIpmsTargetSeeds())
        {
            if (!existingIpmsTargets.TryGetValue(targetSeed.IndicatorNumber, out var target))
            {
                target = new IpmsTarget
                {
                    IndicatorNumber = targetSeed.IndicatorNumber,
                    PeriodId = periods[targetSeed.PeriodCode].Id,
                    DepartmentId = targetSeed.DepartmentCode != null ? departments[targetSeed.DepartmentCode].Id : null,
                    UnitId = targetSeed.UnitCode != null ? units[$"{departments[targetSeed.DepartmentCode!].Id}-{targetSeed.UnitCode}"].Id : null,
                    RelatedOpmsTargetId = targetSeed.RelatedOpmsIndicatorNumber != null ? opmsTargets[targetSeed.RelatedOpmsIndicatorNumber].Id : null,
                    NationalKpa = targetSeed.NationalKpa,
                    MunicipalKpa = targetSeed.MunicipalKpa,
                    StrategicGoalId = targetSeed.StrategicGoalCode != null ? goals[targetSeed.StrategicGoalCode].Id : null,
                    StrategicObjectiveId = targetSeed.StrategicObjectiveCode != null ? objectives[targetSeed.StrategicObjectiveCode].Id : null,
                    PerformanceObjective = targetSeed.PerformanceObjective,
                    TargetName = targetSeed.TargetName,
                    KpiDescription = targetSeed.KpiDescription,
                    Baseline = targetSeed.Baseline,
                    AnnualTarget = targetSeed.AnnualTarget,
                    AnnualTargetDescription = targetSeed.AnnualTargetDescription,
                    BudgetSourceId = targetSeed.BudgetSourceCode != null ? budgetSources[targetSeed.BudgetSourceCode].Id : null,
                    BudgetTypeId = targetSeed.BudgetTypeCode != null ? budgetTypes[targetSeed.BudgetTypeCode].Id : null,
                    UnitOfMeasureId = targetSeed.UnitOfMeasureCode != null ? uoms[targetSeed.UnitOfMeasureCode].Id : null,
                    Weight = targetSeed.Weight,
                    KpiType = targetSeed.KpiType,
                    IndicatorType = targetSeed.IndicatorType,
                    FunctionalArea = targetSeed.FunctionalArea,
                    Q1Target = targetSeed.Q1Target,
                    Q2Target = targetSeed.Q2Target,
                    MidTermTarget = targetSeed.MidTermTarget,
                    Q3Target = targetSeed.Q3Target,
                    Q4Target = targetSeed.Q4Target,
                    TargetUnitType = targetSeed.TargetUnitType,
                    IsRevised = false
                };
                context.IpmsTargets.Add(target);
            }
        }
        await context.SaveChangesAsync();

        // Seed OPMS Submissions
        var opmsTargetsById = await context.OpmsTargets.ToDictionaryAsync(t => t.Id);
        foreach (var submissionSeed in GetOpmsSubmissionSeeds())
        {
            var target = opmsTargetsById.Values.First(t => t.IndicatorNumber == submissionSeed.OpmsIndicatorNumber);
            var existing = await context.OpmsSubmissions.FirstOrDefaultAsync(s => s.OpmsTargetId == target.Id && s.Quarter == submissionSeed.Quarter);
            if (existing == null)
            {
                existing = new OpmsSubmission
                {
                    OpmsTargetId = target.Id,
                    Quarter = submissionSeed.Quarter,
                    Status = submissionSeed.Status,
                    Actual = submissionSeed.Actual,
                    ActualDescription = submissionSeed.ActualDescription,
                    ActualExpenditure = submissionSeed.ActualExpenditure,
                    Variance = submissionSeed.Variance,
                    VarianceReason = submissionSeed.VarianceReason,
                    CorrectiveMeasure = submissionSeed.CorrectiveMeasure,
                    SubmitterScore = submissionSeed.SubmitterScore,
                    SubmittedAt = submissionSeed.SubmittedAt,
                    SubmittedByUserId = submissionSeed.SubmitterUserName != null ? users[submissionSeed.SubmitterUserName].Id : null,
                    VerifierUserId = submissionSeed.VerifierUserName != null ? users[submissionSeed.VerifierUserName].Id : null,
                    VerifiedAt = submissionSeed.VerifiedAt,
                    VerifierComments = submissionSeed.VerifierComments,
                    ApproverUserId = submissionSeed.ApproverUserName != null ? users[submissionSeed.ApproverUserName].Id : null,
                    ApprovedAt = submissionSeed.ApprovedAt,
                    ApproverComments = submissionSeed.ApproverComments,
                    PmsOfficerUserId = submissionSeed.PmsOfficerUserName != null ? users[submissionSeed.PmsOfficerUserName].Id : null,
                    PmsReviewedAt = submissionSeed.PmsReviewedAt,
                    PmsComments = submissionSeed.PmsComments,
                    AuditorUserId = submissionSeed.AuditorUserName != null ? users[submissionSeed.AuditorUserName].Id : null,
                    AuditedAt = submissionSeed.AuditedAt,
                    AuditorComments = submissionSeed.AuditorComments,
                    DueDate = submissionSeed.DueDate,
                    ExtendedDueDate = submissionSeed.ExtendedDueDate
                };
                context.OpmsSubmissions.Add(existing);
            }
        }
        await context.SaveChangesAsync();

        // Seed IPMS Submissions
        var ipmsTargetsById = await context.IpmsTargets.ToDictionaryAsync(t => t.Id);
        foreach (var submissionSeed in GetIpmsSubmissionSeeds())
        {
            var target = ipmsTargetsById.Values.First(t => t.IndicatorNumber == submissionSeed.IpmsIndicatorNumber);
            var existing = await context.IpmsSubmissions.FirstOrDefaultAsync(s => s.IpmsTargetId == target.Id && s.Quarter == submissionSeed.Quarter);
            if (existing == null)
            {
                existing = new IpmsSubmission
                {
                    IpmsTargetId = target.Id,
                    Quarter = submissionSeed.Quarter,
                    Status = submissionSeed.Status,
                    Actual = submissionSeed.Actual,
                    ActualDescription = submissionSeed.ActualDescription,
                    ActualExpenditure = submissionSeed.ActualExpenditure,
                    Variance = submissionSeed.Variance,
                    VarianceReason = submissionSeed.VarianceReason,
                    CorrectiveMeasure = submissionSeed.CorrectiveMeasure,
                    SubmitterScore = submissionSeed.SubmitterScore,
                    SubmittedAt = submissionSeed.SubmittedAt,
                    SubmittedByUserId = submissionSeed.SubmitterUserName != null ? users[submissionSeed.SubmitterUserName].Id : null,
                    VerifierUserId = submissionSeed.VerifierUserName != null ? users[submissionSeed.VerifierUserName].Id : null,
                    VerifiedAt = submissionSeed.VerifiedAt,
                    VerifierComments = submissionSeed.VerifierComments,
                    ApproverUserId = submissionSeed.ApproverUserName != null ? users[submissionSeed.ApproverUserName].Id : null,
                    ApprovedAt = submissionSeed.ApprovedAt,
                    ApproverComments = submissionSeed.ApproverComments,
                    PmsOfficerUserId = submissionSeed.PmsOfficerUserName != null ? users[submissionSeed.PmsOfficerUserName].Id : null,
                    PmsReviewedAt = submissionSeed.PmsReviewedAt,
                    PmsComments = submissionSeed.PmsComments,
                    AuditorUserId = submissionSeed.AuditorUserName != null ? users[submissionSeed.AuditorUserName].Id : null,
                    AuditedAt = submissionSeed.AuditedAt,
                    AuditorComments = submissionSeed.AuditorComments,
                    DueDate = submissionSeed.DueDate,
                    ExtendedDueDate = submissionSeed.ExtendedDueDate
                };
                context.IpmsSubmissions.Add(existing);
            }
        }
        await context.SaveChangesAsync();
    }

    private static IReadOnlyList<PeriodSeed> GetPeriodSeeds()
    {
        return
        [
            new("2024/25", "2024/2025 Financial Year", new DateTime(2024, 7, 1), new DateTime(2025, 6, 30), "2024/2025", true),
            new("2023/24", "2023/2024 Financial Year", new DateTime(2023, 7, 1), new DateTime(2024, 6, 30), "2023/2024", false)
        ];
    }

    private static IReadOnlyList<StrategicGoalSeed> GetStrategicGoalSeeds()
    {
        return
        [
            new("SG1", "Good Governance", "Enhance good governance and accountability", true),
            new("SG2", "Service Delivery Excellence", "Deliver quality services to all communities", true),
            new("SG3", "Economic Development", "Promote local economic development", true),
            new("SG4", "Financial Sustainability", "Ensure long-term financial sustainability", true),
            new("SG5", "Environmental Sustainability", "Protect and preserve the environment", true)
        ];
    }

    private static IReadOnlyList<StrategicObjectiveSeed> GetStrategicObjectiveSeeds()
    {
        return
        [
            new("SO1.1", "Improve Road Infrastructure", "SG2", true),
            new("SO1.2", "Enhance Water Service Delivery", "SG2", true),
            new("SO2.1", "Increase Revenue Collection", "SG4", true),
            new("SO1.3", "Maintain Clean Audit Status", "SG1", true)
        ];
    }

    private static IReadOnlyList<BudgetSourceSeed> GetBudgetSourceSeeds()
    {
        return
        [
            new("EQS", "Equitable Share", true),
            new("MIG", "Municipal Infrastructure Grant", true),
            new("PR", "Property Rates", true),
            new("SC", "Service Charges", true),
            new("INT", "Internal Funds", true)
        ];
    }

    private static IReadOnlyList<BudgetTypeSeed> GetBudgetTypeSeeds()
    {
        return
        [
            new("CAPEX", "Capital Expenditure", true),
            new("OPEX", "Operating Expenditure", true),
            new("MAINT", "Maintenance", true),
            new("REP", "Repairs", true)
        ];
    }

    private static IReadOnlyList<UnitOfMeasureSeed> GetUnitOfMeasureSeeds()
    {
        return
        [
            new("PCT", "Percentage", "%", true),
            new("NUM", "Number", "#", true),
            new("KM", "Kilometers", "km", true),
            new("ZAR", "Rands", "R", true),
            new("HA", "Hectares", "ha", true),
            new("CONN", "Number of Connections", null, true),
            new("DAYS", "Days", "days", true),
            new("SCORE", "Score", null, true),
            new("RATE", "Rating", null, true)
        ];
    }

    private static IReadOnlyList<WardSeed> GetWardSeeds()
    {
        return
        [
            new("W1", "Ward 1", "Blue Hills Municipality", true),
            new("W2", "Ward 2", "Blue Hills Municipality", true),
            new("W3", "Ward 3", "Blue Hills Municipality", true),
            new("W4", "Ward 4", "Blue Hills Municipality", true),
            new("W5", "Ward 5", "Blue Hills Municipality", true)
        ];
    }

    private static IReadOnlyList<VoteNumberSeed> GetVoteNumberSeeds()
    {
        return
        [
            new("V001", "Roads Infrastructure", "ITS", 25000000, true),
            new("V002", "Water Infrastructure", "ITS", 35000000, true),
            new("V003", "Electricity Infrastructure", "ITS", 45000000, true),
            new("V004", "Community Facilities", "COM", 15000000, true),
            new("V005", "Administrative Services", "CORP", 8000000, true)
        ];
    }

    private static IReadOnlyList<OpmsTargetSeed> GetOpmsTargetSeeds()
    {
        return
        [
            new(
                "INFRA-001", "2024/25", "ITS", "ITS-ROADS",
                "Basic Service Delivery", "Infrastructure Development",
                "SG2", "SO1.1",
                "Maintain road network in good condition",
                "Kilometers of roads maintained",
                "Number of kilometers of roads maintained in good condition within the municipal area",
                450, 520, "Maintain 520 km of roads in good condition",
                "MIG", "MAINT", "KM", 15,
                "Quantitative", "Output", "Roads Maintenance",
                115, 125, 250, 135, 145, "absolute_count"),
            new(
                "INFRA-002", "2024/25", "ITS", "ITS-ELEC",
                "Basic Service Delivery", "Water Services",
                "SG2", "SO1.2",
                "Provide reliable water supply",
                "Number of new water connections",
                "Number of new water connections installed in underserved areas",
                0, 2500, "Install 2500 new water connections",
                "MIG", "CAPEX", "CONN", 20,
                "Quantitative", "Output", "Water & Sanitation",
                600, 625, 1250, 650, 625, "absolute_count"),
            new(
                "FIN-001", "2024/25", "BTO", null,
                "Financial Viability", "Revenue Management",
                "SG4", "SO2.1",
                "Maximize revenue collection",
                "Revenue collection rate",
                "Percentage of billed revenue collected within the financial year",
                85, 95, "Achieve 95% revenue collection rate",
                "PR", "OPEX", "PCT", 25,
                "Quantitative", "Outcome", "Revenue Management",
                90, 92, 93, 94, 95, "percentage")
        ];
    }

    private static IReadOnlyList<IpmsTargetSeed> GetIpmsTargetSeeds()
    {
        return
        [
            new(
                "IPMS-INFRA-001", "2024/25", "ITS", "ITS-ROADS",
                "INFRA-001",
                "Basic Service Delivery", "Infrastructure Development",
                "SG2", "SO1.1",
                "Complete road maintenance projects on time",
                "Road maintenance project completion rate",
                "Percentage of road maintenance projects completed within scheduled timeframes",
                70, 90, "Achieve 90% project completion rate",
                "MIG", "MAINT", "PCT", 15,
                "Quantitative", "Output", "Roads Maintenance",
                75, 80, 85, 88, 90, "percentage"),
            new(
                "IPMS-INFRA-002", "2024/25", "ITS", "ITS-ELEC",
                "INFRA-002",
                "Basic Service Delivery", "Water Services",
                "SG2", "SO1.2",
                "Reduce water losses",
                "Water loss reduction",
                "Percentage reduction in non-revenue water",
                35, 20, "Reduce non-revenue water to 20%",
                "MIG", "MAINT", "PCT", 20,
                "Quantitative", "Outcome", "Water & Sanitation",
                32, 28, 25, 22, 20, "reverse_non_cumulative")
        ];
    }

    private static IReadOnlyList<OpmsSubmissionSeed> GetOpmsSubmissionSeeds()
    {
        return
        [
            new(
                OpmsIndicatorNumber: "INFRA-001", Quarter: "Q1", Status: "approved",
                Actual: 118, ActualDescription: "Maintained total of 118 km in Q1 through routine maintenance program", ActualExpenditure: 5500000,
                Variance: 2.6m, VarianceReason: "Favorable weather conditions enabled additional maintenance", CorrectiveMeasure: null,
                SubmitterScore: 85m,
                SubmitterUserName: "mpho.madonsela", SubmittedAt: new DateTime(2024, 9, 28),
                VerifierUserName: "ayabonga.cele", VerifiedAt: new DateTime(2024, 10, 2), VerifierComments: "Evidence aligns to project registers",
                ApproverUserName: "thabo.mokoena", ApprovedAt: new DateTime(2024, 10, 5), ApproverComments: "Approved",
                PmsOfficerUserName: "thandiwe.maseko", PmsReviewedAt: null, PmsComments: null,
                AuditorUserName: null, AuditedAt: null, AuditorComments: null,
                DueDate: new DateTime(2024, 9, 30), ExtendedDueDate: null
            ),
            new(
                OpmsIndicatorNumber: "INFRA-001", Quarter: "Q2", Status: "pending_verification",
                Actual: 120, ActualDescription: "Maintained 120 km including emergency repairs", ActualExpenditure: 5800000,
                Variance: -4.0m, VarianceReason: "Heavy rainfall caused delays in scheduled maintenance", CorrectiveMeasure: "Additional crews deployed for acceleration",
                SubmitterScore: 70m,
                SubmitterUserName: "mpho.madonsela", SubmittedAt: new DateTime(2024, 12, 29),
                VerifierUserName: null, VerifiedAt: null, VerifierComments: null,
                ApproverUserName: null, ApprovedAt: null, ApproverComments: null,
                PmsOfficerUserName: null, PmsReviewedAt: null, PmsComments: null,
                AuditorUserName: null, AuditedAt: null, AuditorComments: null,
                DueDate: new DateTime(2024, 12, 31), ExtendedDueDate: null
            ),
            new(
                OpmsIndicatorNumber: "INFRA-002", Quarter: "Q1", Status: "verified",
                Actual: 580, ActualDescription: "Installed 580 new connections in informal settlements", ActualExpenditure: 3200000,
                Variance: -3.33m, VarianceReason: "Material supply delays", CorrectiveMeasure: "Alternative supplier sourced",
                SubmitterScore: 75m,
                SubmitterUserName: "helena.visagie", SubmittedAt: new DateTime(2024, 9, 28),
                VerifierUserName: "reneilwe.mogale", VerifiedAt: new DateTime(2024, 10, 1), VerifierComments: "Verified",
                ApproverUserName: null, ApprovedAt: null, ApproverComments: null,
                PmsOfficerUserName: null, PmsReviewedAt: null, PmsComments: null,
                AuditorUserName: null, AuditedAt: null, AuditorComments: null,
                DueDate: new DateTime(2024, 9, 30), ExtendedDueDate: null
            ),
            new(
                OpmsIndicatorNumber: "FIN-001", Quarter: "Q1", Status: "submitted",
                Actual: 88, ActualDescription: "Revenue collection achieved through enhanced follow-up", ActualExpenditure: null,
                Variance: -2.2m, VarianceReason: "Extended payment terms for drought-affected businesses", CorrectiveMeasure: null,
                SubmitterScore: 80m,
                SubmitterUserName: "lindiwe.mahlangu", SubmittedAt: new DateTime(2024, 9, 28),
                VerifierUserName: null, VerifiedAt: null, VerifierComments: null,
                ApproverUserName: null, ApprovedAt: null, ApproverComments: null,
                PmsOfficerUserName: null, PmsReviewedAt: null, PmsComments: null,
                AuditorUserName: null, AuditedAt: null, AuditorComments: null,
                DueDate: new DateTime(2024, 9, 30), ExtendedDueDate: null
            )
        ];
    }

    private static IReadOnlyList<IpmsSubmissionSeed> GetIpmsSubmissionSeeds()
    {
        return
        [
            new(
                IpmsIndicatorNumber: "IPMS-INFRA-002", Quarter: "Q3", Status: "pending_verification",
                Actual: 19.4m, ActualDescription: "Non-revenue water reduced to 19.4% following completion of the leak-detection program across zones 3 and 4.", ActualExpenditure: 46200000,
                Variance: 0.4m, VarianceReason: "Marginally above the 19% quarterly target due to a delayed valve replacement.", CorrectiveMeasure: "Expedite outstanding valve replacements in Q4 and intensify pressure management.",
                SubmitterScore: 85m,
                SubmitterUserName: "sizwe.nxumalo", SubmittedAt: new DateTime(2025, 4, 28),
                VerifierUserName: "willem.nel", VerifiedAt: new DateTime(2025, 5, 1, 9, 20, 0), VerifierComments: "Evidence aligns to meter readings and engineering reports.",
                ApproverUserName: null, ApprovedAt: null, ApproverComments: null,
                PmsOfficerUserName: "lethabo.khumalo", PmsReviewedAt: null, PmsComments: "Awaiting verification closure before moderation.",
                AuditorUserName: null, AuditedAt: null, AuditorComments: null,
                DueDate: new DateTime(2025, 4, 30), ExtendedDueDate: new DateTime(2025, 5, 15)
            ),
            new(
                IpmsIndicatorNumber: "IPMS-INFRA-001", Quarter: "Q2", Status: "verified",
                Actual: 83, ActualDescription: "Road maintenance projects progressed slower than planned after storm damage reprioritisation.", ActualExpenditure: 12400000,
                Variance: -5, VarianceReason: "Emergency reinstatement works reduced scheduled completion output.", CorrectiveMeasure: "Reallocate the contractor team and shift non-critical works into the next period.",
                SubmitterScore: 75m,
                SubmitterUserName: "nomsa.dlamini", SubmittedAt: new DateTime(2024, 12, 29),
                VerifierUserName: "ayabonga.cele", VerifiedAt: new DateTime(2025, 1, 5, 8, 30, 0), VerifierComments: "Verified against project registers and site photos.",
                ApproverUserName: null, ApprovedAt: null, ApproverComments: null,
                PmsOfficerUserName: null, PmsReviewedAt: null, PmsComments: null,
                AuditorUserName: null, AuditedAt: null, AuditorComments: null,
                DueDate: new DateTime(2024, 12, 31), ExtendedDueDate: null
            )
        ];
    }

    private sealed record PeriodSeed(string Code, string Name, DateTime StartDate, DateTime EndDate, string FiscalYear, bool IsActive);
    private sealed record StrategicGoalSeed(string Code, string Name, string Description, bool IsActive);
    private sealed record StrategicObjectiveSeed(string Code, string Name, string StrategicGoalCode, bool IsActive);
    private sealed record BudgetSourceSeed(string Code, string Name, bool IsActive);
    private sealed record BudgetTypeSeed(string Code, string Name, bool IsActive);
    private sealed record UnitOfMeasureSeed(string Code, string Name, string? Symbol, bool IsActive);
    private sealed record WardSeed(string Code, string Name, string Municipality, bool IsActive);
    private sealed record VoteNumberSeed(string Code, string Name, string? DepartmentCode, decimal Amount, bool IsActive);
    private sealed record OpmsTargetSeed(
        string IndicatorNumber, string PeriodCode, string? DepartmentCode, string? UnitCode,
        string NationalKpa, string MunicipalKpa,
        string? StrategicGoalCode, string? StrategicObjectiveCode,
        string PerformanceObjective, string TargetName, string KpiDescription,
        decimal Baseline, decimal AnnualTarget, string AnnualTargetDescription,
        string? BudgetSourceCode, string? BudgetTypeCode, string? UnitOfMeasureCode,
        decimal Weight, string KpiType, string IndicatorType, string? FunctionalArea,
        decimal? Q1Target, decimal? Q2Target, decimal? MidTermTarget, decimal? Q3Target, decimal? Q4Target,
        string TargetUnitType);
    private sealed record IpmsTargetSeed(
        string IndicatorNumber, string PeriodCode, string? DepartmentCode, string? UnitCode,
        string? RelatedOpmsIndicatorNumber,
        string NationalKpa, string MunicipalKpa,
        string? StrategicGoalCode, string? StrategicObjectiveCode,
        string PerformanceObjective, string TargetName, string KpiDescription,
        decimal Baseline, decimal AnnualTarget, string AnnualTargetDescription,
        string? BudgetSourceCode, string? BudgetTypeCode, string? UnitOfMeasureCode,
        decimal Weight, string KpiType, string IndicatorType, string? FunctionalArea,
        decimal? Q1Target, decimal? Q2Target, decimal? MidTermTarget, decimal? Q3Target, decimal? Q4Target,
        string TargetUnitType);
    private sealed record OpmsSubmissionSeed(
        string OpmsIndicatorNumber, string Quarter, string Status,
        decimal? Actual, string? ActualDescription, decimal? ActualExpenditure,
        decimal? Variance, string? VarianceReason, string? CorrectiveMeasure,
        decimal? SubmitterScore,
        string? SubmitterUserName, DateTime? SubmittedAt,
        string? VerifierUserName, DateTime? VerifiedAt, string? VerifierComments,
        string? ApproverUserName, DateTime? ApprovedAt, string? ApproverComments,
        string? PmsOfficerUserName, DateTime? PmsReviewedAt, string? PmsComments,
        string? AuditorUserName, DateTime? AuditedAt, string? AuditorComments,
        DateTime? DueDate, DateTime? ExtendedDueDate = null);
    private sealed record IpmsSubmissionSeed(
        string IpmsIndicatorNumber, string Quarter, string Status,
        decimal? Actual, string? ActualDescription, decimal? ActualExpenditure,
        decimal? Variance, string? VarianceReason, string? CorrectiveMeasure,
        decimal? SubmitterScore,
        string? SubmitterUserName, DateTime? SubmittedAt,
        string? VerifierUserName, DateTime? VerifiedAt, string? VerifierComments,
        string? ApproverUserName, DateTime? ApprovedAt, string? ApproverComments,
        string? PmsOfficerUserName, DateTime? PmsReviewedAt, string? PmsComments,
        string? AuditorUserName, DateTime? AuditedAt, string? AuditorComments,
        DateTime? DueDate, DateTime? ExtendedDueDate = null);
}

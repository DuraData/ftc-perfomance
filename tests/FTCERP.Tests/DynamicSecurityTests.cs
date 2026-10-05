namespace FTCERP.Tests;

public class DynamicSecurityTests
{
    [Fact]
    public async Task Security_user_directory_pages_only_effective_authorized_municipalities()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("directory-admin", "Directory", "Admin"); actor.MunicipalityId = 7;
        var local = IdpTestFixture.CreateUser("local-user", "Local", "User"); local.MunicipalityId = 7;
        var expired = IdpTestFixture.CreateUser("expired-user", "Expired", "User"); expired.MunicipalityId = 7;
        var foreign = IdpTestFixture.CreateUser("foreign-user", "Foreign", "User"); foreign.MunicipalityId = 8;
        var municipalityA = new Municipality { Id = 7, Code = "M007", Name = "Municipality 7" };
        var municipalityB = new Municipality { Id = 8, Code = "M008", Name = "Municipality 8" };
        var localRole = Role("local-role", "LOCAL_SECURITY"); localRole.MunicipalityId = 7;
        var foreignRole = Role("foreign-role", "FOREIGN_SECURITY"); foreignRole.MunicipalityId = 8;
        context.AddRange(municipalityA, municipalityB, actor, local, expired, foreign, localRole, foreignRole);
        await context.SaveChangesAsync();
        var now = DateTime.UtcNow;
        context.SecurityUserRoleAssignments.AddRange(
            new SecurityUserRoleAssignment { UserId = actor.Id, RoleId = localRole.Id, MunicipalityId = 7, IsActive = true, EffectiveFrom = now.AddDays(-1), AssignedAt = now, AssignedBy = actor.Id },
            new SecurityUserRoleAssignment { UserId = local.Id, RoleId = localRole.Id, MunicipalityId = 7, IsActive = true, EffectiveFrom = now.AddDays(-1), AssignedAt = now, AssignedBy = actor.Id },
            new SecurityUserRoleAssignment { UserId = expired.Id, RoleId = localRole.Id, MunicipalityId = 7, IsActive = true, EffectiveFrom = now.AddDays(-2), EffectiveTo = now.AddDays(-1), AssignedAt = now.AddDays(-2), AssignedBy = actor.Id },
            new SecurityUserRoleAssignment { UserId = foreign.Id, RoleId = foreignRole.Id, MunicipalityId = 8, IsActive = true, EffectiveFrom = now.AddDays(-1), AssignedAt = now, AssignedBy = actor.Id });
        await context.SaveChangesAsync();
        var tenant = new Mock<ITenantContext>(); tenant.SetupGet(item => item.MunicipalityId).Returns(7); tenant.SetupGet(item => item.IsSystem).Returns(false);
        var controller = new SecurityAdministrationController(context, CreateService(context, actor), IdpTestFixture.CreateUserManagerMock(actor).Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        var result = await controller.GetUsersPage(new PagedQueryRequest { Page = 1, PageSize = 1, SortBy = "name", SortDirection = "asc" });

        var payload = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityUserDto>>>().Subject.Data!;
        payload.TotalCount.Should().Be(2);
        payload.Items.Should().ContainSingle();
        payload.TotalPages.Should().Be(2);
        payload.Items.Should().NotContain(item => item.Id == foreign.Id || item.Id == expired.Id);
    }

    [Fact]
    public async Task DynamicallyConfiguredKpiViewerAndDepartmentSubmitter_EnforceMenusCrudAndScope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var viewer = IdpTestFixture.CreateUser("dynamic-viewer");
        var submitter = IdpTestFixture.CreateUser("dynamic-submitter");
        var viewerRole = Role("dynamic-viewer-role", "KPI_VIEWER");
        var submitterRole = Role("dynamic-submitter-role", "DEPARTMENT_SUBMITTER");
        var read = new Permission
        {
            Code = "OPMS_KPI.READ",
            Module = "Resource",
            Feature = "OPMS_KPI",
            Action = "Read",
            Kind = SecurityPermissionKind.Resource,
            ResourceCode = "OPMS_KPI",
            Operation = SecurityOperation.Read
        };
        var update = new Permission
        {
            Code = "OPMS_KPI.UPDATE",
            Module = "Resource",
            Feature = "OPMS_KPI",
            Action = "Update",
            Kind = SecurityPermissionKind.Resource,
            ResourceCode = "OPMS_KPI",
            Operation = SecurityOperation.Update
        };
        var navigation = new Permission
        {
            Code = "NAV.SDBIP",
            Module = "Navigation",
            Feature = "SDBIP",
            Action = "View",
            Kind = SecurityPermissionKind.Navigation,
            NavigationCode = "NAV.SDBIP"
        };
        var department = new Department { Id = 10, Code = "FIN", Name = "Finance", IsActive = true };
        context.AddRange(viewer, submitter, viewerRole, submitterRole, read, update, navigation, department);
        await context.SaveChangesAsync();

        context.SecurityUserRoleAssignments.AddRange(Assignment(viewer, viewerRole), Assignment(submitter, submitterRole));
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = viewerRole.Id, PermissionId = read.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = viewerRole.Id, PermissionId = navigation.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = submitterRole.Id, PermissionId = read.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ScopeType = ScopeType.DepartmentScope },
            new RolePermission { RoleId = submitterRole.Id, PermissionId = update.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ScopeType = ScopeType.DepartmentScope },
            new RolePermission { RoleId = submitterRole.Id, PermissionId = navigation.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        context.UserScopes.Add(new UserScope { UserId = submitter.Id, ScopeType = ScopeType.DepartmentScope, DepartmentId = department.Id, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        context.SecurityNavigationItems.Add(new SecurityNavigationItem { Code = "NAV.SDBIP.ITEM", Name = "SDBIP", Route = "/opms", RequiredPermissionCode = navigation.Code, DisplayOrder = 1 });
        await context.SaveChangesAsync();

        var viewerSecurity = CreateService(context, viewer);
        (await viewerSecurity.CheckPermissionAsync(viewer, read.Code)).Allowed.Should().BeTrue();
        (await viewerSecurity.CheckPermissionAsync(viewer, update.Code, new AccessScopeContext(DepartmentId: department.Id))).Allowed.Should().BeFalse();
        (await viewerSecurity.GetAuthorizedNavigationAsync(viewer)).Should().ContainSingle(item => item.Path == "/opms");

        var submitterSecurity = CreateService(context, submitter);
        (await submitterSecurity.CheckPermissionAsync(submitter, update.Code, new AccessScopeContext(DepartmentId: department.Id))).Allowed.Should().BeTrue();
        (await submitterSecurity.CheckPermissionAsync(submitter, update.Code, new AccessScopeContext(DepartmentId: 11))).Allowed.Should().BeFalse();
        (await submitterSecurity.GetAuthorizedNavigationAsync(submitter)).Should().ContainSingle(item => item.Path == "/opms");
    }

    [Fact]
    public async Task NoApplicablePermission_IsDeniedByDefault()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context, user);

        var decision = await service.CheckPermissionAsync(user, "OPMS_KPI.READ");

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain("Missing permission");
    }

    [Fact]
    public async Task ExplicitDeny_OverridesAllowAcrossMultipleRoles()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var allowRole = Role("allow", "KPI_VIEWER");
        var denyRole = Role("deny", "KPI_RESTRICTED");
        var permission = new Permission { Code = "OPMS_KPI.READ", Module = "Resource", Feature = "OPMS_KPI", Action = "Read", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Read };
        context.AddRange(user, allowRole, denyRole, permission);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.AddRange(Assignment(user, allowRole), Assignment(user, denyRole));
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = allowRole.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = denyRole.Id, PermissionId = permission.Id, IsAllowed = false, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();
        var service = CreateService(context, user);

        var decision = await service.CheckPermissionAsync(user, permission.Code);

        decision.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task ExplicitRoleDeny_CannotBeBypassedByUserAllowOverride()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var role = Role("deny", "KPI_RESTRICTED");
        var permission = new Permission { Code = "OPMS_KPI.UPDATE", Module = "Resource", Feature = "OPMS_KPI", Action = "Update", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Update };
        context.AddRange(user, role, permission);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsAllowed = false, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        context.UserPermissionOverrides.Add(new UserPermissionOverride { UserId = user.Id, PermissionId = permission.Id, IsAllowed = true, Reason = "Must not override an explicit deny" });
        await context.SaveChangesAsync();

        var decision = await CreateService(context, user).CheckPermissionAsync(user, permission.Code);

        decision.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task LegacyUserAllowOverride_CannotCreatePermissionWithoutRoleGrant()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var permission = new Permission { Code = "SECURITY.SYSTEM_SCOPE", Module = "Security", Feature = "Role", Action = "System", Kind = SecurityPermissionKind.Action };
        context.AddRange(user, permission);
        await context.SaveChangesAsync();
        context.UserPermissionOverrides.Add(new UserPermissionOverride { UserId = user.Id, PermissionId = permission.Id, IsAllowed = true, Reason = "Legacy unsafe grant" });
        await context.SaveChangesAsync();

        var decision = await CreateService(context, user).CheckPermissionAsync(user, permission.Code);

        decision.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task RecordPermission_RequiresMatchingScope()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var role = Role("submitter", "DEPARTMENT_SUBMITTER");
        var permission = new Permission { Code = "OPMS_SUBMISSION.READ", Module = "Resource", Feature = "OPMS_SUBMISSION", Action = "Read", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_SUBMISSION", Operation = SecurityOperation.Read };
        var department = new Department { Id = 10, Code = "FIN", Name = "Finance", IsActive = true };
        context.AddRange(user, role, permission, department);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ScopeType = ScopeType.DepartmentScope });
        context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeType = ScopeType.DepartmentScope, DepartmentId = 10, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();
        var service = CreateService(context, user);

        var allowed = await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(DepartmentId: 10));
        var denied = await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(DepartmentId: 11));

        allowed.Allowed.Should().BeTrue();
        denied.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionScope_CannotBeSatisfiedByDifferentUserScopeType()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var role = Role("scoped", "SCOPED_READER");
        var permission = new Permission { Code = "OPMS_KPI.READ", Module = "Resource", Feature = "OPMS_KPI", Action = "Read", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Read };
        var department = new Department { Id = 10, Code = "FIN", Name = "Finance", IsActive = true };
        var unit = new Unit { Id = 20, DepartmentId = department.Id, Code = "REV", Name = "Revenue", IsActive = true };
        context.AddRange(user, role, permission, department, unit);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ScopeType = ScopeType.DepartmentScope });
        context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeType = ScopeType.UnitScope, UnitId = 20, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        var decision = await CreateService(context, user).CheckPermissionAsync(user, permission.Code, new AccessScopeContext(DepartmentId: 10, UnitId: 20));

        decision.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task EffectiveDatedRoleAssignmentScope_ConstrainsRecordAccess()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var role = Role("department-role", "DEPARTMENT_READER");
        var permission = new Permission { Code = "OPMS_KPI.READ", Module = "Resource", Feature = "OPMS_KPI", Action = "Read", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Read };
        context.AddRange(user, role, permission);
        await context.SaveChangesAsync();
        var assignment = Assignment(user, role);
        assignment.DepartmentId = 10;
        context.SecurityUserRoleAssignments.Add(assignment);
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ScopeType = ScopeType.DepartmentScope });
        await context.SaveChangesAsync();
        var service = CreateService(context, user);

        var allowed = await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(DepartmentId: 10));
        var denied = await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(DepartmentId: 11));

        allowed.Allowed.Should().BeTrue();
        denied.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task Navigation_ExcludesUnauthorizedLeavesAndEmptyParents()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var role = Role("viewer", "KPI_VIEWER");
        var permission = new Permission { Code = "NAV.DASHBOARD", Module = "Navigation", Feature = "Dashboard", Action = "View", Kind = SecurityPermissionKind.Navigation, NavigationCode = "NAV.DASHBOARD" };
        context.AddRange(user, role, permission);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        var admin = new SecurityNavigationItem { Code = "NAV.ADMIN", Name = "Administration", DisplayOrder = 2 };
        context.SecurityNavigationItems.AddRange(new SecurityNavigationItem { Code = "NAV.DASHBOARD.ITEM", Name = "Dashboard", Route = "/dashboard", RequiredPermissionCode = "NAV.DASHBOARD", DisplayOrder = 1 }, admin);
        await context.SaveChangesAsync();
        context.SecurityNavigationItems.Add(new SecurityNavigationItem { Code = "NAV.ADMIN.USERS", Name = "Users", Route = "/users", ParentId = admin.Id, RequiredPermissionCode = "NAV.ADMIN.USERS", DisplayOrder = 1 });
        await context.SaveChangesAsync();
        var service = CreateService(context, user);

        var menu = await service.GetAuthorizedNavigationAsync(user);

        menu.Should().ContainSingle(item => item.Label == "Dashboard");
        menu.Should().NotContain(item => item.Label == "Administration");
    }

    [Fact]
    public async Task NavigationRegistry_CreateUsesPublicHierarchyAndRejectsCycles()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("system-admin");
        var role = Role("system-role", "SYSTEM_ADMIN");
        var systemPermission = new Permission { Code = "SECURITY.SYSTEM_SCOPE", Module = "Security", Feature = "Role", Action = "System", Kind = SecurityPermissionKind.Action };
        context.AddRange(user, role, systemPermission);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = systemPermission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        var parent = new SecurityNavigationItem { Code = "NAV.TEST", Name = "Test root", DisplayOrder = 10 };
        context.SecurityNavigationItems.Add(parent);
        await context.SaveChangesAsync();

        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(7);
        tenant.SetupGet(item => item.IsSystem).Returns(true);
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = new SecurityAdministrationController(context, CreateService(context, user), userManager.Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };

        var createdResult = await controller.CreateNavigationItem(new CreateSecurityNavigationRequest(
            "NAV.TEST.CHILD", parent.PublicId, "Child", "/test/child", "List", 1, null, "Add governed test route"));
        var created = createdResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<SecurityNavigationDto>>().Subject.Data!;
        created.ParentPublicId.Should().Be(parent.PublicId);
        created.PublicId.Should().NotBeEmpty();

        var cycleResult = await controller.UpdateNavigationItem(parent.PublicId, new UpdateSecurityNavigationRequest(
            created.PublicId, parent.Name, null, null, parent.DisplayOrder, null, true, Convert.ToBase64String(parent.RowVersion), "Attempt invalid cyclic move"));
        cycleResult.Result.Should().BeOfType<BadRequestObjectResult>();
        (await context.AuditTrails.SingleAsync()).Reason.Should().Be("Add governed test route");
    }

    [Fact]
    public async Task RoleAssignment_RejectsUnitOutsideSelectedDepartment()
    {
        await using var context = IdpTestFixture.CreateContext();
        var actor = IdpTestFixture.CreateUser("system-admin");
        var target = IdpTestFixture.CreateUser("target-user");
        var systemRole = Role("system-role", "SYSTEM_ADMIN");
        var tenantRole = Role("tenant-role", "DEPARTMENT_REVIEWER"); tenantRole.MunicipalityId = 7;
        var systemPermission = new Permission { Code = "SECURITY.SYSTEM_SCOPE", Module = "Security", Feature = "Role", Action = "System", Kind = SecurityPermissionKind.Action };
        var department = new Department { Id = 10, MunicipalityId = 7, Code = "FIN", Name = "Finance", IsActive = true };
        var otherDepartment = new Department { Id = 11, MunicipalityId = 7, Code = "CORP", Name = "Corporate", IsActive = true };
        var unit = new Unit { Id = 20, MunicipalityId = 7, DepartmentId = otherDepartment.Id, Code = "LEGAL", Name = "Legal", IsActive = true };
        context.AddRange(actor, target, systemRole, tenantRole, systemPermission, department, otherDepartment, unit);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(actor, systemRole));
        context.RolePermissions.Add(new RolePermission { RoleId = systemRole.Id, PermissionId = systemPermission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        var tenant = new Mock<ITenantContext>(); tenant.SetupGet(item => item.MunicipalityId).Returns(7); tenant.SetupGet(item => item.IsSystem).Returns(true);
        var users = new Dictionary<string, ApplicationUser> { [actor.Id] = actor, [target.Id] = target };
        var userManager = IdpTestFixture.CreateUserManagerMock(actor, users);
        var controller = new SecurityAdministrationController(context, CreateService(context, actor), userManager.Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        var result = await controller.PutUserRoles(target.Id, new UpdateUserRoleSecurityRequest([], [new UpdateUserRoleAssignment(tenantRole.Id, 7, department.Id, unit.Id, DateTime.UtcNow, null)]));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        context.SecurityUserRoleAssignments.Should().ContainSingle(item => item.UserId == actor.Id);
    }

    [Fact]
    public async Task RoleAssignment_ResolvesTenantScopedPublicOrganizationIds_and_projects_them_back()
    {
        await using var context = IdpTestFixture.CreateContext();
        var actor = IdpTestFixture.CreateUser("public-scope-admin");
        var target = IdpTestFixture.CreateUser("public-scope-target");
        var systemRole = Role("public-system-role", "SYSTEM_ADMIN");
        var tenantRole = Role("public-tenant-role", "UNIT_REVIEWER"); tenantRole.MunicipalityId = 7;
        var systemPermission = new Permission { Code = "SECURITY.SYSTEM_SCOPE", Module = "Security", Feature = "Role", Action = "System", Kind = SecurityPermissionKind.Action };
        var department = new Department { MunicipalityId = 7, Code = "FIN", Name = "Finance", IsActive = true };
        var unit = new Unit { MunicipalityId = 7, Department = department, Code = "REV", Name = "Revenue", IsActive = true };
        context.AddRange(actor, target, systemRole, tenantRole, systemPermission, department, unit);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(actor, systemRole));
        context.RolePermissions.Add(new RolePermission { RoleId = systemRole.Id, PermissionId = systemPermission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        var tenant = new Mock<ITenantContext>(); tenant.SetupGet(item => item.MunicipalityId).Returns(7); tenant.SetupGet(item => item.IsSystem).Returns(true);
        var users = new Dictionary<string, ApplicationUser> { [actor.Id] = actor, [target.Id] = target };
        var controller = new SecurityAdministrationController(context, CreateService(context, actor), IdpTestFixture.CreateUserManagerMock(actor, users).Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        var savedResult = await controller.PutUserRoles(target.Id, new UpdateUserRoleSecurityRequest([], [
            new UpdateUserRoleAssignment(tenantRole.Id, 7, null, null, DateTime.UtcNow.AddMinutes(-1), null, department.PublicId, unit.PublicId)
        ]));

        savedResult.Result.Should().BeOfType<OkObjectResult>();
        var stored = await context.SecurityUserRoleAssignments.SingleAsync(item => item.UserId == target.Id && item.IsActive);
        stored.DepartmentId.Should().Be(department.Id);
        stored.UnitId.Should().Be(unit.Id);

        var loadedResult = await controller.GetUserRoles(target.Id);
        var loaded = loadedResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<UserRoleSecurityConfigurationDto>>().Subject.Data!;
        var assignment = loaded.Assignments.Should().ContainSingle().Subject;
        assignment.DepartmentPublicId.Should().Be(department.PublicId);
        assignment.DepartmentName.Should().Be("Finance");
        assignment.UnitPublicId.Should().Be(unit.PublicId);
        assignment.UnitName.Should().Be("Revenue");
    }

    private static AccessControlService CreateService(ApplicationDbContext context, ApplicationUser user)
    {
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null!, null!, null!, null!);
        return new AccessControlService(context, userManager.Object, roleManager.Object);
    }

    private static ApplicationRole Role(string id, string code) => new() { Id = id, Name = code, NormalizedName = code, RoleCode = code, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
    private static SecurityUserRoleAssignment Assignment(ApplicationUser user, ApplicationRole role) => new() { UserId = user.Id, RoleId = role.Id, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), AssignedAt = DateTime.UtcNow, AssignedBy = "test" };
}

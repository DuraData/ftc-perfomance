namespace FTCERP.Tests;

public class DynamicSecurityTests
{
    [Fact]
    public async Task Role_access_matrix_pages_tenant_roles_and_uses_effective_dynamic_assignments()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(7);
        tenant.SetupGet(item => item.IsSystem).Returns(false);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = 7, Code = "MATRIX-7", Name = "Matrix Municipality" };
        var foreignMunicipality = new Municipality { Id = 8, Code = "MATRIX-8", Name = "Foreign Matrix Municipality" };
        var user = IdpTestFixture.CreateUser("matrix-user", "Matrix", "User"); user.Municipality = municipality;
        var foreignUser = IdpTestFixture.CreateUser("matrix-foreign", "Foreign", "User"); foreignUser.Municipality = foreignMunicipality;
        var roles = Enumerable.Range(0, 11).Select(index =>
        {
            var role = Role($"matrix-role-{index:00}", $"MATCH_{index:00}");
            role.Name = $"Match Role {index:00}"; role.NormalizedName = role.Name.ToUpperInvariant(); role.Municipality = municipality; role.CreatedAt = DateTime.UtcNow.AddMinutes(index);
            return role;
        }).ToArray();
        var foreignRole = Role("matrix-role-foreign", "MATCH_FOREIGN"); foreignRole.Name = "Match Foreign Role"; foreignRole.Municipality = foreignMunicipality;
        var allowed = new Permission { Code = "OPMS_KPI.READ", Module = "Resource", Feature = "OPMS_KPI", Action = "Read", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Read, IsActive = true };
        var denied = new Permission { Code = "OPMS_KPI.DELETE", Module = "Resource", Feature = "OPMS_KPI", Action = "Delete", Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Delete, IsActive = true };
        context.AddRange(municipality, foreignMunicipality, user, foreignUser);
        context.Roles.AddRange(roles.Append(foreignRole));
        context.Permissions.AddRange(allowed, denied);
        await context.SaveChangesAsync();
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roles[3].Id, PermissionId = allowed.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = roles[3].Id, PermissionId = denied.Id, IsAllowed = false, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        context.SecurityUserRoleAssignments.AddRange(
            new SecurityUserRoleAssignment { UserId = user.Id, RoleId = roles[3].Id, MunicipalityId = municipality.Id, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), AssignedAt = DateTime.UtcNow, AssignedBy = user.Id },
            new SecurityUserRoleAssignment { UserId = foreignUser.Id, RoleId = foreignRole.Id, MunicipalityId = foreignMunicipality.Id, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), AssignedAt = DateTime.UtcNow, AssignedBy = foreignUser.Id });
        await context.SaveChangesAsync();
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null!, null!, null!, null!);
        roleManager.SetupGet(manager => manager.Roles).Returns(context.Roles);
        var service = new AccessControlService(context, IdpTestFixture.CreateUserManagerMock(user).Object, roleManager.Object, tenant.Object);

        var page = await service.BuildRoleAccessMatrixPageAsync(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "code", SortDirection = "asc" });

        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.Role).Should().Equal("Match Role 03", "Match Role 04", "Match Role 05");
        page.Items[0].Permissions.Should().Contain("OPMS_KPI.READ").And.NotContain("OPMS_KPI.DELETE");
        page.Items[0].TestUser.Should().Be("Matrix User");
        page.Items[0].Scope.Should().Contain("Municipality:7");
        page.Items.Should().NotContain(item => item.Role == "Match Foreign Role");

        var controller = new AccessController(service, IdpTestFixture.CreateUserManagerMock(user).Object, context, tenant.Object);
        (await controller.GetRoleAccessMatrixPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetRoleAccessMatrix().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task Role_implementation_audit_uses_current_tenant_dynamic_security_evidence()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(7);
        tenant.SetupGet(item => item.IsSystem).Returns(false);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = 7, Code = "AUDIT-7", Name = "Audit Municipality" };
        var user = IdpTestFixture.CreateUser("audit-user", "Audit", "Reviewer"); user.Municipality = municipality;
        var role = Role("audit-reviewer", SecurityModel.Reviewer); role.Municipality = municipality;
        var uncoveredRole = Role("audit-approver", SecurityModel.Approver); uncoveredRole.Municipality = municipality;
        var dashboard = new Permission { Code = "Dashboard.View", Module = "Dashboard", Feature = "Dashboard", Action = "View", Kind = SecurityPermissionKind.Action, IsActive = true };
        var reports = new Permission { Code = "Reports.View", Module = "Reports", Feature = "Reports", Action = "View", Kind = SecurityPermissionKind.Action, IsActive = true };
        var audit = new Permission { Code = "Audit.Trails.View", Module = "Audit", Feature = "Trails", Action = "View", Kind = SecurityPermissionKind.Action, IsActive = true };
        var notifications = new Permission { Code = "Notifications.View", Module = "Notifications", Feature = "Notifications", Action = "View", Kind = SecurityPermissionKind.Action, IsActive = true };
        var expiredCrud = new Permission { Code = "OPMS.Create", Module = "OPMS", Feature = "OPMS", Action = "Create", Kind = SecurityPermissionKind.Action, IsActive = true };
        context.AddRange(municipality, user, role, uncoveredRole, dashboard, reports, audit, notifications, expiredCrud);
        await context.SaveChangesAsync();
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = dashboard.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = role.Id, PermissionId = reports.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = role.Id, PermissionId = audit.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = role.Id, PermissionId = notifications.Id, IsAllowed = false, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = role.Id, PermissionId = expiredCrud.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-2), EffectiveTo = DateTime.UtcNow.AddDays(-1) });
        context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
        {
            UserId = user.Id, RoleId = role.Id, MunicipalityId = municipality.Id, IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1), AssignedAt = DateTime.UtcNow, AssignedBy = user.Id
        });
        context.SecurityNavigationItems.Add(new SecurityNavigationItem
        {
            Code = "NAV.REVIEWER.DASHBOARD", Name = "Reviewer Dashboard", Route = "/dashboard",
            RequiredPermissionCode = dashboard.Code, DisplayOrder = 1, IsActive = true
        });
        await context.SaveChangesAsync();
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null!, null!, null!, null!);
        roleManager.SetupGet(manager => manager.Roles).Returns(context.Roles);
        var controller = new RoleImplementationAuditController(context, roleManager.Object, tenant.Object);

        var result = await controller.GetAudit();

        var payload = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<RoleImplementationAuditResponse[]>>().Subject.Data!;
        var reviewer = payload.Should().ContainSingle(item => item.Role == SecurityModel.Reviewer).Subject;
        reviewer.Dashboard.Should().BeTrue();
        reviewer.Menus.Should().BeTrue();
        reviewer.ScopeFiltering.Should().BeTrue();
        reviewer.Crud.Should().BeFalse("the only CRUD grant is expired");
        reviewer.Notifications.Should().BeFalse("the current notification rule is an explicit deny");
        reviewer.Reports.Should().BeTrue();
        reviewer.AuditTrail.Should().BeTrue();
        reviewer.Complete.Should().BeTrue();

        var service = new AccessControlService(context, IdpTestFixture.CreateUserManagerMock(user).Object, roleManager.Object, tenant.Object);
        var coverage = await service.BuildSystemCoverageAuditAsync();
        var reviewerCoverage = coverage.Should().ContainSingle(item => item.Role == SecurityModel.Reviewer).Subject;
        reviewerCoverage.SeededUser.Should().BeTrue();
        reviewerCoverage.Dashboard.Should().BeTrue();
        reviewerCoverage.Menu.Should().BeTrue();
        reviewerCoverage.ScopeFiltering.Should().BeTrue();
        reviewerCoverage.Crud.Should().BeFalse();
        reviewerCoverage.Reports.Should().BeTrue();
        reviewerCoverage.AuditTrail.Should().BeTrue();
        reviewerCoverage.Notifications.Should().BeFalse();
        coverage.Should().ContainSingle(item => item.Role == SecurityModel.Approver).Which.Dashboard.Should().BeFalse();
    }

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
        controller.GetUsers().Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task Security_role_directory_pages_authorized_municipalities_and_supports_inactive_administration()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("role-directory-admin", "Role", "Admin"); actor.MunicipalityId = 7;
        var municipalityA = new Municipality { Id = 7, Code = "M007", Name = "Municipality 7" };
        var municipalityB = new Municipality { Id = 8, Code = "M008", Name = "Municipality 8" };
        var actorRole = Role("role-directory-owner", "ROLE_DIRECTORY_OWNER"); actorRole.MunicipalityId = 7; actorRole.Name = "Directory Owner";
        var localRole = Role("role-directory-local", "LOCAL_REVIEWER"); localRole.MunicipalityId = 7; localRole.Name = "Local Reviewer";
        var inactiveRole = Role("role-directory-inactive", "FORMER_REVIEWER"); inactiveRole.MunicipalityId = 7; inactiveRole.Name = "Former Reviewer"; inactiveRole.IsActive = false;
        var foreignRole = Role("role-directory-foreign", "FOREIGN_REVIEWER"); foreignRole.MunicipalityId = 8; foreignRole.Name = "Foreign Reviewer";
        context.AddRange(municipalityA, municipalityB, actor, actorRole, localRole, inactiveRole, foreignRole);
        await context.SaveChangesAsync();
        var now = DateTime.UtcNow;
        context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
        {
            UserId = actor.Id, RoleId = actorRole.Id, MunicipalityId = 7, IsActive = true,
            EffectiveFrom = now.AddDays(-1), AssignedAt = now, AssignedBy = actor.Id
        });
        await context.SaveChangesAsync();
        var tenant = new Mock<ITenantContext>(); tenant.SetupGet(item => item.MunicipalityId).Returns(7); tenant.SetupGet(item => item.IsSystem).Returns(false);
        var controller = new SecurityAdministrationController(context, CreateService(context, actor), IdpTestFixture.CreateUserManagerMock(actor).Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        var activeResult = await controller.GetRolesPage(new PagedQueryRequest { Page = 1, PageSize = 1, SortBy = "name", SortDirection = "asc" });
        var active = activeResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityRoleDto>>>().Subject.Data!;
        active.TotalCount.Should().Be(2);
        active.Items.Should().ContainSingle();
        active.TotalPages.Should().Be(2);

        var inactiveResult = await controller.GetRolesPage(new PagedQueryRequest { Search = "FORMER", SortBy = "code", SortDirection = "asc" }, includeInactive: true);
        var inactive = inactiveResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityRoleDto>>>().Subject.Data!;
        inactive.Items.Should().ContainSingle(item => item.Id == inactiveRole.Id);
        inactive.Items.Should().NotContain(item => item.Id == foreignRole.Id);
        controller.GetRoles().Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
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
    public async Task Batched_member_decisions_preserve_single_permission_scope_and_default_deny_semantics()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser();
        var role = Role("batch-reader", "BATCH_READER");
        var department = new Department { Id = 10, Code = "BATCH", Name = "Batch Department", IsActive = true };
        var identity = new Permission { Code = "OPMS_SUBMISSION.SubmitterIdentity.READ", Module = "Member", Feature = "OPMS_SUBMISSION", Action = "Read", Kind = SecurityPermissionKind.Member, ResourceCode = "OPMS_SUBMISSION", MemberCode = "SubmitterIdentity", Operation = SecurityOperation.Read };
        var comment = new Permission { Code = "OPMS_SUBMISSION.VerifierComment.READ", Module = "Member", Feature = "OPMS_SUBMISSION", Action = "Read", Kind = SecurityPermissionKind.Member, ResourceCode = "OPMS_SUBMISSION", MemberCode = "VerifierComment", Operation = SecurityOperation.Read };
        context.AddRange(user, role, department, identity, comment);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = identity.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ScopeType = ScopeType.DepartmentScope },
            new RolePermission { RoleId = role.Id, PermissionId = comment.Id, IsAllowed = false, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeType = ScopeType.DepartmentScope, DepartmentId = 10, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();
        var service = CreateService(context, user);
        var scope = new AccessScopeContext(DepartmentId: 10);

        var batch = await service.CheckPermissionsBatchAsync(user,
            [identity.Code, comment.Code, "OPMS_SUBMISSION.WithdrawalReason.READ"], scope);

        batch[identity.Code].Allowed.Should().BeTrue();
        (await service.CheckPermissionAsync(user, identity.Code, scope)).Allowed.Should().BeTrue();
        batch[comment.Code].Allowed.Should().BeFalse();
        (await service.CheckPermissionAsync(user, comment.Code, scope)).Allowed.Should().BeFalse();
        batch["OPMS_SUBMISSION.WithdrawalReason.READ"].Allowed.Should().BeFalse();
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
    public async Task OperationalAssignments_GrantQueryScopeOnlyWhileActiveAndEffective()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var now = DateTime.UtcNow;
        var user = IdpTestFixture.CreateUser();
        var role = Role("assigned-target-reader", "ASSIGNED_TARGET_READER");
        var permission = new Permission
        {
            Code = "OPMS_KPI.READ", Module = "Resource", Feature = "OPMS_KPI", Action = "Read",
            Kind = SecurityPermissionKind.Resource, ResourceCode = "OPMS_KPI", Operation = SecurityOperation.Read
        };
        context.AddRange(user, role, permission);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id, PermissionId = permission.Id, IsAllowed = true, IsActive = true,
            EffectiveFrom = now.AddDays(-1), ScopeType = ScopeType.AssignedTargetScope
        });
        context.UserAssignments.AddRange(
            new UserAssignment { UserId = user.Id, AssignmentType = AssignmentType.AdditionalSubmitterAssignment, TargetId = "current-target", IsActive = true, ValidFromUtc = now.AddHours(-1), ValidToUtc = now.AddHours(1) },
            new UserAssignment { UserId = user.Id, AssignmentType = AssignmentType.AdditionalSubmitterAssignment, TargetId = "expired-target", IsActive = true, ValidFromUtc = now.AddDays(-2), ValidToUtc = now.AddDays(-1) },
            new UserAssignment { UserId = user.Id, AssignmentType = AssignmentType.AdditionalSubmitterAssignment, TargetId = "future-target", IsActive = true, ValidFromUtc = now.AddDays(1) },
            new UserAssignment { UserId = user.Id, AssignmentType = AssignmentType.AdditionalSubmitterAssignment, TargetId = "inactive-target", IsActive = false, ValidFromUtc = now.AddDays(-1) });
        await context.SaveChangesAsync();

        var service = CreateService(context, user);
        var access = await service.GetEffectiveAccessAsync(user);
        var queryScope = await service.GetQueryScopeAsync(user, permission.Code);

        access.Assignments.Should().ContainSingle(item => item.TargetId == "current-target");
        queryScope.TargetIds.Should().Equal("current-target");
        (await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(TargetId: "current-target"))).Allowed.Should().BeTrue();
        (await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(TargetId: "expired-target"))).Allowed.Should().BeFalse();
        (await service.CheckPermissionAsync(user, permission.Code, new AccessScopeContext(TargetId: "future-target"))).Allowed.Should().BeFalse();
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
        var pageResult = await controller.GetNavigationRegistryPage(new PagedQueryRequest { Page = 1, PageSize = 1, Search = "Child", SortBy = "parent", SortDirection = "asc" }, true);
        pageResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityNavigationDto>>>()
            .Subject.Data!.Items.Should().ContainSingle(item => item.PublicId == created.PublicId && item.ParentCode == parent.Code && item.ParentName == parent.Name);
        (await controller.GetNavigationRegistryPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetNavigationRegistry().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);

        var cycleResult = await controller.UpdateNavigationItem(parent.PublicId, new UpdateSecurityNavigationRequest(
            created.PublicId, parent.Name, null, null, parent.DisplayOrder, null, true, Convert.ToBase64String(parent.RowVersion), "Attempt invalid cyclic move"));
        cycleResult.Result.Should().BeOfType<BadRequestObjectResult>();
        (await context.AuditTrails.SingleAsync()).Reason.Should().Be("Add governed test route");
    }

    [Fact]
    public async Task SecurityRegistry_DefinitionsAreVersionedAuditedAndSynchronizeEffectivePermissions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("registry-system-admin");
        var role = Role("registry-system-role", "SYSTEM_ADMIN");
        var systemPermission = new Permission { Code = "SECURITY.SYSTEM_SCOPE", Module = "Security", Feature = "Role", Action = "System", Kind = SecurityPermissionKind.Action };
        context.AddRange(user, role, systemPermission);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(Assignment(user, role));
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = systemPermission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        var tenant = new Mock<ITenantContext>(); tenant.SetupGet(item => item.MunicipalityId).Returns(7); tenant.SetupGet(item => item.IsSystem).Returns(true);
        var controller = new SecurityAdministrationController(context, CreateService(context, user), IdpTestFixture.CreateUserManagerMock(user).Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };

        var resourceResult = await controller.CreateResource(new CreateSecurityResourceRequest(
            "TEST_CASE", "Test Case", "ENTITY", "Governed test resource", true, true, true, false, false, false, true, true, "Register test resource"));
        var resource = resourceResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<SecurityResourceDto>>().Subject.Data!;
        resource.PublicId.Should().NotBeEmpty();
        (await context.Permissions.Where(item => item.ResourceCode == "TEST_CASE" && item.Kind == SecurityPermissionKind.Resource && item.IsActive).Select(item => item.Code).ToArrayAsync())
            .Should().BeEquivalentTo("TEST_CASE.CREATE", "TEST_CASE.READ", "TEST_CASE.UPDATE");

        var actionResult = await controller.CreateAction(new CreateSecurityActionRequest("TEST_CASE.COMPLETE", "Complete Test Case", "TEST_CASE", null, "Register completion action"));
        var action = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<SecurityActionDto>>().Subject.Data!;
        action.PublicId.Should().NotBeEmpty();
        (await context.Permissions.SingleAsync(item => item.Code == "TEST_CASE.COMPLETE")).IsActive.Should().BeTrue();

        var memberEntity = new SecurityMemberDefinition { ResourceCode = "TEST_CASE", MemberCode = "ProtectedValue", DisplayName = "Protected Value" };
        context.SecurityMemberDefinitions.Add(memberEntity);
        await context.SaveChangesAsync();
        var memberResult = await controller.UpdateMember(memberEntity.PublicId, new UpdateSecurityMemberRequest(
            "Protected Value", true, true, Convert.ToBase64String(memberEntity.RowVersion), "Classify protected member"));
        memberResult.Result.Should().BeOfType<OkObjectResult>();
        (await context.Permissions.Where(item => item.ResourceCode == "TEST_CASE" && item.MemberCode == "ProtectedValue" && item.IsActive).CountAsync()).Should().Be(2);

        var resourcePageResult = await controller.GetResourcesPage(new PagedQueryRequest { Page = 1, PageSize = 1, Search = "TEST_CASE", SortBy = "code", SortDirection = "asc" });
        resourcePageResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityResourceDto>>>()
            .Subject.Data!.Items.Should().ContainSingle(item => item.PublicId == resource.PublicId);
        var actionPageResult = await controller.GetActionsPage(new PagedQueryRequest { Page = 1, PageSize = 1, Search = "COMPLETE", SortBy = "resource", SortDirection = "asc" });
        actionPageResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityActionDto>>>()
            .Subject.Data!.Items.Should().ContainSingle(item => item.PublicId == action.PublicId);
        var memberPageResult = await controller.GetMembersPage(new PagedQueryRequest { Page = 1, PageSize = 1, Search = "Protected", SortBy = "resource", SortDirection = "asc" });
        memberPageResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityMemberDto>>>()
            .Subject.Data!.Items.Should().ContainSingle(item => item.PublicId == memberEntity.PublicId);
        var definitionPageResult = await controller.GetPermissionDefinitionsPage(new PagedQueryRequest { Page = 1, PageSize = 1, Search = "TEST_CASE", SortBy = "code", SortDirection = "asc" }, "Resource");
        definitionPageResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<SecurityPermissionDefinitionDto>>>()
            .Subject.Data!.Items.Should().ContainSingle(item => item.Kind == "Resource" && item.Code.StartsWith("TEST_CASE."));
        (await controller.GetResourcesPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetActionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetMembersPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetPermissionDefinitionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetPermissionDefinitionsPage(new PagedQueryRequest(), "Resource,unsafe")).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetResources().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        controller.GetActions().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        controller.GetMembers().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        controller.GetPermissionDefinitions().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);

        var updateResult = await controller.UpdateResource(resource.PublicId, new UpdateSecurityResourceRequest(
            resource.Name, resource.Type, resource.Description, resource.CanCreate, resource.CanRead, resource.CanUpdate, resource.CanDelete,
            resource.CanExport, resource.CanImport, resource.SupportsMembers, resource.SupportsCriteria, false, resource.RowVersion, "Deactivate test resource"));
        var updated = updateResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<SecurityResourceDto>>().Subject.Data!;
        updated.IsActive.Should().BeFalse();
        (await context.Permissions.Where(item => item.ResourceCode == "TEST_CASE" && item.IsActive).CountAsync()).Should().Be(0);
        (await context.AuditTrails.Where(item => item.EntityName == nameof(SecurityResource) || item.EntityName == nameof(SecurityActionDefinition) || item.EntityName == nameof(SecurityMemberDefinition)).CountAsync()).Should().Be(4);

        var staleResult = await controller.UpdateResource(resource.PublicId, new UpdateSecurityResourceRequest(
            resource.Name, resource.Type, resource.Description, resource.CanCreate, resource.CanRead, resource.CanUpdate, resource.CanDelete,
            resource.CanExport, resource.CanImport, resource.SupportsMembers, resource.SupportsCriteria, true, resource.RowVersion, "Attempt stale registry update"));
        staleResult.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task SecurityRegistry_TenantAdministratorCannotMutateGlobalDefinitions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("tenant-security-admin");
        var role = Role("tenant-security-role", "TENANT_SECURITY_ADMIN"); role.MunicipalityId = 7;
        var managePermission = new Permission { Code = "SECURITY.MANAGE_PERMISSIONS", Module = "Security", Feature = "Role", Action = "Manage", Kind = SecurityPermissionKind.Action };
        context.AddRange(new Municipality { Id = 7, Code = "TST", Name = "Test Municipality" }, user, role, managePermission);
        await context.SaveChangesAsync();
        var assignment = Assignment(user, role); assignment.MunicipalityId = 7;
        context.SecurityUserRoleAssignments.Add(assignment);
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = managePermission.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();
        var tenant = new Mock<ITenantContext>(); tenant.SetupGet(item => item.MunicipalityId).Returns(7);
        var controller = new SecurityAdministrationController(context, CreateService(context, user), IdpTestFixture.CreateUserManagerMock(user).Object, tenant.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };

        var result = await controller.CreateResource(new CreateSecurityResourceRequest(
            "ESCALATION", "Escalation", "ENTITY", null, false, true, false, false, false, false, false, true, "Attempt global mutation"));

        result.Result.Should().BeOfType<ForbidResult>();
        (await context.SecurityResources.AnyAsync(item => item.Code == "ESCALATION")).Should().BeFalse();
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
        assignment.PublicId.Should().Be(stored.PublicId);
        assignment.PublicId.Should().NotBe(Guid.Empty);
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

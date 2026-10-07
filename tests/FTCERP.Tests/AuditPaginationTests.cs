namespace FTCERP.Tests;

public sealed class AuditPaginationTests
{
    [Fact]
    public async Task Audit_pages_apply_tenant_scope_before_search_count_sort_and_page()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        string auditorAId;

        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "AUD-A", Name = "Audit A" };
            var tenantB = new Municipality { Code = "AUD-B", Name = "Audit B" };
            setup.AddRange(tenantA, tenantB);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            tenantBId = tenantB.Id;
            var auditorA = IdpTestFixture.CreateUser("auditor-a"); auditorA.MunicipalityId = tenantAId;
            var plannerA = IdpTestFixture.CreateUser("planner-a"); plannerA.MunicipalityId = tenantAId;
            var auditorB = IdpTestFixture.CreateUser("auditor-b"); auditorB.MunicipalityId = tenantBId;
            setup.AddRange(auditorA, plannerA, auditorB);
            await setup.SaveChangesAsync();
            auditorAId = auditorA.Id;

            setup.LoginAuditLogs.AddRange(
                Login(tenantAId, "anna@example.test", false, "Account locked", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
                Login(tenantAId, "alex@example.test", false, "Invalid credentials", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
                Login(tenantAId, "amy@example.test", true, null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                Login(tenantBId, "other@example.test", false, "Account locked", new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)));
            setup.AuditTrails.AddRange(
                Trail(tenantAId, "OpmsSubmission", "a-1", "Approve", "auditor-a", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
                Trail(tenantAId, "OpmsTarget", "a-2", "Update", "planner-a", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
                Trail(tenantBId, "OpmsSubmission", "b-1", "Approve", "auditor-b", new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)));
            await setup.SaveChangesAsync();
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TenantContext(tenantAId));
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "allowed", [], [], []));
        var controller = new AuditController(tenantAContext, access.Object, new TenantContext(tenantAId))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(auditorAId) } }
        };

        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetLoginLogs().Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetSecurityEvents().Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetAuditTrails().Result).StatusCode);

        var loginResult = await controller.GetLoginLogsPage(new PagedQueryRequest
        {
            Page = 1, PageSize = 1, Search = "a", SortBy = "email", SortDirection = "asc"
        }, failuresOnly: true);
        var loginPage = Assert.IsType<ApiResponse<PagedResponse<LoginAuditLogResponse>>>(Assert.IsType<OkObjectResult>(loginResult.Result).Value).Data!;
        loginPage.TotalCount.Should().Be(2);
        loginPage.TotalPages.Should().Be(2);
        loginPage.Items.Should().ContainSingle().Which.Email.Should().Be("alex@example.test");
        loginPage.Items.Should().OnlyContain(item => item.PublicId != Guid.Empty);
        loginPage.Items.Should().NotContain(item => item.Email == "other@example.test");

        var trailResult = await controller.GetAuditTrailsPage(new PagedQueryRequest
        {
            Page = 1, PageSize = 10, Search = "Approve", SortBy = "createdAt", SortDirection = "desc"
        }, entityName: "OpmsSubmission");
        var trailPage = Assert.IsType<ApiResponse<PagedResponse<AuditTrailEntryResponse>>>(Assert.IsType<OkObjectResult>(trailResult.Result).Value).Data!;
        trailPage.TotalCount.Should().Be(1);
        trailPage.Items.Should().ContainSingle().Which.EntityId.Should().Be("a-1");
        trailPage.Items.Should().NotContain(item => item.EntityId == "b-1");
    }

    [Fact]
    public async Task Login_audit_member_permissions_mask_and_prevent_sensitive_query_inference_dynamically()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var tenant = new Municipality { Code = "LOG-MEMBER", Name = "Login Member Municipality" };
        var actor = IdpTestFixture.CreateUser("login-auditor");
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        context.Municipalities.Add(tenant);
        await context.SaveChangesAsync();
        actor.MunicipalityId = tenant.Id;
        context.Users.Add(actor);
        var protectedLogin = Login(tenant.Id, "protected@example.test", false, "Account locked", DateTime.UtcNow);
        protectedLogin.UserId = actor.Id;
        context.LoginAuditLogs.Add(protectedLogin);
        await context.SaveChangesAsync();

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "allowed" : "denied", [], [], []));
        var controller = new AuditController(context, access.Object, new TenantContext(tenant.Id))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        var deniedResult = await controller.GetLoginLogsPage(new PagedQueryRequest { SortBy = "createdAt" });
        var deniedItem = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<LoginAuditLogResponse>>>(
            Assert.IsType<OkObjectResult>(deniedResult.Result).Value).Data!.Items);
        Assert.Null(deniedItem.UserId);
        Assert.Null(deniedItem.Email);
        Assert.Null(deniedItem.IpAddress);
        Assert.Null(deniedItem.UserAgent);
        Assert.Null(deniedItem.FailureReason);

        var hiddenSearch = await controller.GetLoginLogsPage(new PagedQueryRequest { SortBy = "createdAt", Search = "protected@example.test" });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<LoginAuditLogResponse>>>(
            Assert.IsType<OkObjectResult>(hiddenSearch.Result).Value).Data!.TotalCount);
        Assert.IsType<ForbidResult>((await controller.GetLoginLogsPage(new PagedQueryRequest { SortBy = "email" })).Result);

        allowedCodes.Add("LOGIN_AUDIT.Email.READ");
        allowedCodes.Add("LOGIN_AUDIT.IpAddress.READ");
        allowedCodes.Add("LOGIN_AUDIT.FailureReason.READ");
        var allowedResult = await controller.GetLoginLogsPage(new PagedQueryRequest { SortBy = "email", Search = "protected@example.test" });
        var allowedItem = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<LoginAuditLogResponse>>>(
            Assert.IsType<OkObjectResult>(allowedResult.Result).Value).Data!.Items);
        Assert.Equal("protected@example.test", allowedItem.Email);
        Assert.Equal("127.0.0.1", allowedItem.IpAddress);
        Assert.Equal("Account locked", allowedItem.FailureReason);
        Assert.Null(allowedItem.UserAgent);
    }

    [Fact]
    public async Task Audit_trail_member_permissions_mask_and_prevent_sensitive_query_inference_dynamically()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var tenant = new Municipality { Code = "AUD-MEMBER", Name = "Audit Member Municipality" };
        var actor = IdpTestFixture.CreateUser("audit-member-reader");
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        context.Municipalities.Add(tenant);
        await context.SaveChangesAsync();
        actor.MunicipalityId = tenant.Id;
        context.Users.Add(actor);
        var protectedTrail = Trail(tenant.Id, "ProtectedEntity", "protected-entity-id", "Update", actor.Id, DateTime.UtcNow);
        protectedTrail.OldValue = "{\"secret\":\"before\"}";
        protectedTrail.NewValue = "{\"secret\":\"after\"}";
        protectedTrail.IpAddress = "192.0.2.10";
        protectedTrail.CorrelationId = "protected-correlation";
        protectedTrail.Reason = "protected-reason";
        protectedTrail.UserAgent = "protected-agent";
        protectedTrail.SessionId = "protected-session";
        context.AuditTrails.Add(protectedTrail);
        await context.SaveChangesAsync();

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "allowed" : "denied", [], [], []));
        var controller = new AuditController(context, access.Object, new TenantContext(tenant.Id))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(actor.Id) } }
        };

        var deniedResult = await controller.GetAuditTrailsPage(new PagedQueryRequest { SortBy = "createdAt" });
        var deniedItem = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<AuditTrailEntryResponse>>>(
            Assert.IsType<OkObjectResult>(deniedResult.Result).Value).Data!.Items);
        Assert.Null(deniedItem.EntityId);
        Assert.Null(deniedItem.OldValue);
        Assert.Null(deniedItem.NewValue);
        Assert.Null(deniedItem.ChangedBy);
        Assert.Null(deniedItem.IpAddress);
        Assert.Null(deniedItem.CorrelationId);
        Assert.Null(deniedItem.Reason);
        Assert.Null(deniedItem.UserAgent);
        Assert.Null(deniedItem.SessionId);

        foreach (var hiddenValue in new[] { "protected-entity-id", actor.Id, "protected-reason", "protected-correlation" })
        {
            var hiddenSearch = await controller.GetAuditTrailsPage(new PagedQueryRequest { SortBy = "createdAt", Search = hiddenValue });
            Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<AuditTrailEntryResponse>>>(
                Assert.IsType<OkObjectResult>(hiddenSearch.Result).Value).Data!.TotalCount);
        }
        Assert.IsType<ForbidResult>((await controller.GetAuditTrailsPage(new PagedQueryRequest { SortBy = "changedBy" })).Result);
        Assert.IsType<ForbidResult>((await controller.GetAuditTrailsPage(new PagedQueryRequest { SortBy = "createdAt" }, entityId: "protected-entity-id")).Result);

        foreach (var member in new[] { "EntityId", "OldValue", "NewValue", "ChangedBy", "IpAddress", "CorrelationId", "Reason", "UserAgent", "SessionId" })
            allowedCodes.Add($"AUDIT_TRAIL.{member}.READ");

        var allowedResult = await controller.GetAuditTrailsPage(
            new PagedQueryRequest { SortBy = "changedBy", Search = actor.Id }, entityId: "protected-entity-id");
        var allowedItem = Assert.Single(Assert.IsType<ApiResponse<PagedResponse<AuditTrailEntryResponse>>>(
            Assert.IsType<OkObjectResult>(allowedResult.Result).Value).Data!.Items);
        Assert.Equal("protected-entity-id", allowedItem.EntityId);
        Assert.Equal("{\"secret\":\"before\"}", allowedItem.OldValue);
        Assert.Equal("{\"secret\":\"after\"}", allowedItem.NewValue);
        Assert.Equal(actor.Id, allowedItem.ChangedBy);
        Assert.Equal("192.0.2.10", allowedItem.IpAddress);
        Assert.Equal("protected-correlation", allowedItem.CorrelationId);
        Assert.Equal("protected-reason", allowedItem.Reason);
        Assert.Equal("protected-agent", allowedItem.UserAgent);
        Assert.Equal("protected-session", allowedItem.SessionId);
    }

    [Fact]
    public async Task Login_audit_history_is_tenant_filtered_and_append_only()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "LOG-A", Name = "Login A" };
            var tenantB = new Municipality { Code = "LOG-B", Name = "Login B" };
            setup.AddRange(tenantA, tenantB);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            tenantBId = tenantB.Id;
            setup.LoginAuditLogs.AddRange(Login(tenantAId, "a@example.test", true, null, DateTime.UtcNow), Login(tenantBId, "b@example.test", true, null, DateTime.UtcNow));
            await setup.SaveChangesAsync();
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TenantContext(tenantAId));
        var visible = await tenantAContext.LoginAuditLogs.SingleAsync();
        visible.Email.Should().Be("a@example.test");
        visible.Email = "rewritten@example.test";
        var rewrite = () => tenantAContext.SaveChangesAsync();
        await rewrite.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Login audit history is append-only*");
    }

    private static LoginAuditLog Login(long municipalityId, string email, bool success, string? failure, DateTime occurredAt) => new()
    {
        MunicipalityId = municipalityId,
        Email = email,
        Success = success,
        FailureReason = failure,
        IpAddress = "127.0.0.1",
        UserAgent = "test-agent",
        LoggedAt = occurredAt
    };

    private static AuditTrail Trail(long municipalityId, string entityName, string entityId, string action, string actor, DateTime occurredAt) => new()
    {
        MunicipalityId = municipalityId,
        EntityName = entityName,
        EntityId = entityId,
        Action = action,
        ChangedBy = actor,
        ChangedAt = occurredAt,
        CorrelationId = $"correlation-{entityId}"
    };

    private sealed class TenantContext(long municipalityId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => "audit-test";
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "system";
    }
}

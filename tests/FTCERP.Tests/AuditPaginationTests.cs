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
        var controller = new AuditController(tenantAContext);

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

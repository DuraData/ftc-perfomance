namespace FTCERP.Tests;

public sealed class GovernedWithdrawalTests
{
    [Fact]
    public async Task Opms_target_withdrawal_preserves_record_and_appends_immutable_tenant_event()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WD-A", Name = "Withdrawal Municipality" };
        var user = IdpTestFixture.CreateUser("withdraw-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var target = new OpmsTarget
        {
            Id = "opms-withdraw-1",
            MunicipalityId = municipality.Id,
            IndicatorNumber = "1.1",
            TargetName = "Retained target",
            KpiDescription = "Governed target",
            AnnualTargetDescription = "One"
        };
        context.OpmsTargets.Add(target);
        await context.SaveChangesAsync();
        var expectedVersion = Convert.ToBase64String(target.RowVersion);

        var access = AllowAccess("OPMS_KPI.WITHDRAW");
        var audit = new Mock<IWorkflowGovernanceService>();
        audit.Setup(service => service.WriteAuditTrailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            audit.Object,
            new TenantContext(municipality.Id, user.Id),
            new FTCERP.Host.Domain.Services.PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.WithdrawTarget(target.Id, new WithdrawGovernedRecordRequest("  Superseded by the approved plan  ", expectedVersion));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<OpmsTargetResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.True(payload.Data!.IsWithdrawn);
        Assert.Equal("Superseded by the approved plan", payload.Data.ReasonForWithdrawal);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.RowVersion));

        context.ChangeTracker.Clear();
        var retained = await context.OpmsTargets.SingleAsync(item => item.Id == target.Id);
        Assert.True(retained.IsWithdrawn);
        Assert.Equal(user.Id, retained.WithdrawnByUserId);
        var lifecycle = await context.GovernedRecordLifecycleEvents.SingleAsync();
        Assert.Equal(GovernedLifecycleAction.Withdrawn, lifecycle.Action);
        Assert.Equal(target.Id, lifecycle.AggregateId);
        Assert.Equal(municipality.Id, lifecycle.MunicipalityId);

        lifecycle.Reason = "Rewritten history";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        audit.Verify(service => service.WriteAuditTrailAsync(
            "OpmsTarget", target.Id, "Withdraw", It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>()), Times.Once);

        var delete = controller.DeleteTarget(target.Id);
        var gone = Assert.IsType<ObjectResult>(delete.Result);
        Assert.Equal(StatusCodes.Status410Gone, gone.StatusCode);
    }

    [Fact]
    public async Task Opms_target_withdrawal_rejects_stale_row_version_without_writing_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WD-B", Name = "Concurrency Municipality" };
        var user = IdpTestFixture.CreateUser("concurrency-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var target = new OpmsTarget
        {
            Id = "opms-withdraw-stale",
            MunicipalityId = municipality.Id,
            IndicatorNumber = "1.2",
            TargetName = "Concurrent target",
            KpiDescription = "Concurrency target",
            AnnualTargetDescription = "One"
        };
        context.Add(target);
        await context.SaveChangesAsync();

        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            AllowAccess("OPMS_KPI.WITHDRAW").Object,
            Mock.Of<IWorkflowGovernanceService>(),
            new TenantContext(municipality.Id, user.Id),
            new FTCERP.Host.Domain.Services.PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var staleVersion = Convert.ToBase64String(BitConverter.GetBytes(long.MaxValue));
        var result = await controller.WithdrawTarget(target.Id, new WithdrawGovernedRecordRequest("Stale request", staleVersion));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        context.ChangeTracker.Clear();
        Assert.False((await context.OpmsTargets.SingleAsync()).IsWithdrawn);
        Assert.Empty(await context.GovernedRecordLifecycleEvents.ToArrayAsync());
    }

    [Fact]
    public async Task Relational_constraints_require_withdrawal_metadata_and_filter_lifecycle_by_tenant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        var user = IdpTestFixture.CreateUser("tenant-lifecycle-user");

        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "LIFE-A", Name = "Lifecycle A" };
            var tenantB = new Municipality { Code = "LIFE-B", Name = "Lifecycle B" };
            setup.AddRange(tenantA, tenantB, user);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            tenantBId = tenantB.Id;

            setup.OpmsTargets.Add(new OpmsTarget
            {
                MunicipalityId = tenantAId,
                IndicatorNumber = "invalid",
                TargetName = "Missing metadata",
                KpiDescription = "Invalid withdrawn target",
                AnnualTargetDescription = "One",
                IsWithdrawn = true
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => setup.SaveChangesAsync());
            setup.ChangeTracker.Clear();

            setup.GovernedRecordLifecycleEvents.AddRange(
                Lifecycle(tenantAId, user.Id, "a"),
                Lifecycle(tenantBId, user.Id, "b"));
            await setup.SaveChangesAsync();
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TenantContext(tenantAId, user.Id));
        var visible = await tenantAContext.GovernedRecordLifecycleEvents.AsNoTracking().ToArrayAsync();
        Assert.Single(visible);
        Assert.Equal("a", visible[0].AggregateId);
    }

    private static GovernedRecordLifecycleEvent Lifecycle(long municipalityId, string userId, string aggregateId) => new()
    {
        MunicipalityId = municipalityId,
        AggregateType = "OpmsTarget",
        AggregateId = aggregateId,
        Action = GovernedLifecycleAction.Withdrawn,
        Reason = "Governed reason",
        ActorUserId = userId,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    private static Mock<IAccessControlService> AllowAccess(string code)
    {
        var mock = new Mock<IAccessControlService>();
        mock.Setup(service => service.CheckPermissionAsync(
                It.IsAny<ApplicationUser>(), code, It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [code], [], []));
        return mock;
    }

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = IdpTestFixture.CreatePrincipal(userId),
            TraceIdentifier = "withdrawal-test"
        }
    };

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => userId;
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "system";
    }
}

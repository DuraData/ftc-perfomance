using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class LegacyTenantOwnershipReconciliationControllerTests
{
    [Fact]
    public async Task Preview_and_execute_require_one_authoritative_municipality_and_append_evidence()
    {
        await using var database = await TestDatabase.Create();
        var setup = await database.Seed();
        await using var context = database.Context(new Tenant(setup.MunicipalityA.Id, setup.Actor.Id, true));
        var controller = Controller(context, setup, new Tenant(setup.MunicipalityA.Id, setup.Actor.Id, true));

        var response = await controller.Preview(LegacyTenantEntityType.Department, 1, 25);
        var page = Assert.IsType<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>>(
            Assert.IsType<OkObjectResult>(response.Result).Value).Data!;
        Assert.Equal(4, page.TotalCount);
        var ready = Assert.Single(page.Items, x => x.EntityPublicId == setup.ReadyDepartment.PublicId);
        Assert.Equal("Ready", ready.Status);
        Assert.Contains("position.department", ready.SourceEvidence);
        Assert.Equal("Ambiguous", Assert.Single(page.Items, x => x.EntityPublicId == setup.AmbiguousDepartment.PublicId).Status);
        Assert.Equal("Blocked", Assert.Single(page.Items, x => x.EntityPublicId == setup.BlockedDepartment.PublicId).Status);

        var executed = await controller.Execute(new(LegacyTenantEntityType.Department, ready.EntityPublicId,
            ready.RowVersion, "Approved tenant-root reconciliation CR-301."));
        var result = Assert.IsType<ApiResponse<LegacyTenantOwnershipResultDto>>(
            Assert.IsType<OkObjectResult>(executed.Result).Value).Data!;

        context.ChangeTracker.Clear();
        Assert.Equal(setup.MunicipalityA.Id, (await context.Departments.IgnoreQueryFilters()
            .SingleAsync(x => x.PublicId == setup.ReadyDepartment.PublicId)).MunicipalityId);
        var evidence = await context.LegacyTenantOwnershipReconciliations.SingleAsync();
        Assert.Equal(result.ReconciliationPublicId, evidence.PublicId);
        Assert.Equal(setup.ReadyDepartment.PublicId, evidence.EntityPublicId);
        Assert.Equal(setup.Actor.Id, evidence.ReconciledByUserId);
        Assert.Contains("position.department", evidence.SourceEvidence);
        Assert.Equal("Reconcile", (await context.AuditTrails.SingleAsync(x =>
            x.EntityName == nameof(LegacyTenantOwnershipReconciliation))).Action);

        evidence.Reason = "Attempted rewrite";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Execute_rejects_stale_versions_foreign_evidence_and_non_system_contexts()
    {
        await using var database = await TestDatabase.Create();
        var setup = await database.Seed();
        var systemTenant = new Tenant(setup.MunicipalityA.Id, setup.Actor.Id, true);
        await using var context = database.Context(systemTenant);
        var controller = Controller(context, setup, systemTenant);

        var stale = await controller.Execute(new(LegacyTenantEntityType.Department, setup.ReadyDepartment.PublicId,
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "Reject deliberately stale ownership evidence."));
        Assert.IsType<ConflictObjectResult>(stale.Result);

        var foreign = await controller.Execute(new(LegacyTenantEntityType.Department, setup.ForeignDepartment.PublicId,
            Convert.ToBase64String(setup.ForeignDepartment.RowVersion), "Reject ownership proven for another municipality."));
        Assert.IsType<ConflictObjectResult>(foreign.Result);

        var ordinaryTenant = new Tenant(setup.MunicipalityA.Id, setup.Actor.Id, false);
        var denied = await Controller(context, setup, ordinaryTenant).Preview(LegacyTenantEntityType.Department, 1, 25);
        Assert.IsType<ObjectResult>(denied.Result);
        Assert.Equal(StatusCodes.Status409Conflict, ((ObjectResult)denied.Result!).StatusCode);
        Assert.Empty(await context.LegacyTenantOwnershipReconciliations.ToArrayAsync());
    }

    [Theory]
    [InlineData(LegacyTenantEntityType.Unit)]
    [InlineData(LegacyTenantEntityType.OpmsTarget)]
    [InlineData(LegacyTenantEntityType.IpmsTarget)]
    [InlineData(LegacyTenantEntityType.OpmsSubmission)]
    [InlineData(LegacyTenantEntityType.IpmsSubmission)]
    public async Task Every_supported_register_derives_ownership_from_its_authoritative_parent(LegacyTenantEntityType type)
    {
        await using var database = await TestDatabase.Create();
        var setup = await database.Seed(includeOperationalChain: true);
        var tenant = new Tenant(setup.MunicipalityA.Id, setup.Actor.Id, true);
        await using var context = database.Context(tenant);
        var response = await Controller(context, setup, tenant).Preview(type, 1, 25);
        var page = Assert.IsType<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>>(
            Assert.IsType<OkObjectResult>(response.Result).Value).Data!;
        Assert.Contains(page.Items, item => item.Status == "Ready");
    }

    private static LegacyTenantOwnershipReconciliationController Controller(ApplicationDbContext context, Setup setup, ITenantContext tenant) =>
        new(context, tenant, IdpTestFixture.CreateUserManagerMock(setup.Actor).Object,
            new WorkflowGovernanceService(context, tenantContext: tenant))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = IdpTestFixture.CreatePrincipal(setup.Actor.Id),
                    TraceIdentifier = "tenant-ownership-reconciliation-test"
                }
            }
        };

    private sealed record Tenant(long? MunicipalityId, string? UserId, bool IsSystem) : ITenantContext;

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private TestDatabase(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<TestDatabase> Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new(connection, options);
        }

        public ApplicationDbContext Context(ITenantContext? tenant = null) => new(_options, tenant);

        public async Task<Setup> Seed(bool includeOperationalChain = false)
        {
            await using var context = Context();
            var municipalityA = new Municipality { Code = "TEN-A", Name = "Tenant A" };
            var municipalityB = new Municipality { Code = "TEN-B", Name = "Tenant B" };
            var actor = IdpTestFixture.CreateUser("tenant-root-admin");
            var ready = new Department { Code = "READY", Name = "Ready department" };
            var ambiguous = new Department { Code = "AMB", Name = "Ambiguous department" };
            var blocked = new Department { Code = "BLOCK", Name = "Blocked department" };
            var foreign = new Department { Code = "FOREIGN", Name = "Foreign department" };
            context.AddRange(municipalityA, municipalityB, actor, ready, ambiguous, blocked, foreign);
            await context.SaveChangesAsync();
            actor.MunicipalityId = municipalityA.Id;
            context.Positions.AddRange(
                new Position { MunicipalityId = municipalityA.Id, DepartmentId = ready.Id, Code = "READY-P", Name = "Ready position" },
                new Position { MunicipalityId = municipalityA.Id, DepartmentId = ambiguous.Id, Code = "AMB-A", Name = "Ambiguous A" },
                new Position { MunicipalityId = municipalityB.Id, DepartmentId = ambiguous.Id, Code = "AMB-B", Name = "Ambiguous B" },
                new Position { MunicipalityId = municipalityB.Id, DepartmentId = foreign.Id, Code = "FOREIGN-P", Name = "Foreign position" });
            await context.SaveChangesAsync();

            if (includeOperationalChain)
            {
                var ownedDepartment = new Department { MunicipalityId = municipalityA.Id, Code = "OWNED", Name = "Owned department" };
                context.Departments.Add(ownedDepartment);
                await context.SaveChangesAsync();
                var legacyUnit = new Unit { DepartmentId = ownedDepartment.Id, Code = "LEG-U", Name = "Legacy unit" };
                context.Units.Add(legacyUnit);
                await context.SaveChangesAsync();
                var opms = new OpmsTarget { DepartmentId = ownedDepartment.Id, IndicatorNumber = "OPMS-LEG", TargetName = "OPMS legacy", KpiDescription = "Legacy", PerformanceObjective = "Objective" };
                var ipms = new IpmsTarget { DepartmentId = ownedDepartment.Id, IndicatorNumber = "IPMS-LEG", TargetName = "IPMS legacy", KpiDescription = "Legacy", PerformanceObjective = "Objective" };
                var ownedOpms = new OpmsTarget { MunicipalityId = municipalityA.Id, DepartmentId = ownedDepartment.Id, IndicatorNumber = "OPMS-OWNED", TargetName = "OPMS owned", KpiDescription = "Owned", PerformanceObjective = "Objective" };
                var ownedIpms = new IpmsTarget { MunicipalityId = municipalityA.Id, DepartmentId = ownedDepartment.Id, IndicatorNumber = "IPMS-OWNED", TargetName = "IPMS owned", KpiDescription = "Owned", PerformanceObjective = "Objective" };
                context.AddRange(opms, ipms, ownedOpms, ownedIpms);
                await context.SaveChangesAsync();
                context.AddRange(
                    new OpmsSubmission { OpmsTargetId = ownedOpms.Id, Quarter = "Q1", Status = "Draft", BaseState = SubmissionBaseStates.InProgress },
                    new IpmsSubmission { IpmsTargetId = ownedIpms.Id, Quarter = "Q1", Status = "Draft", BaseState = SubmissionBaseStates.InProgress });
                await context.SaveChangesAsync();
            }
            return new(municipalityA, municipalityB, actor, ready, ambiguous, blocked, foreign);
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }

    private sealed record Setup(Municipality MunicipalityA, Municipality MunicipalityB, ApplicationUser Actor,
        Department ReadyDepartment, Department AmbiguousDepartment, Department BlockedDepartment, Department ForeignDepartment);
}

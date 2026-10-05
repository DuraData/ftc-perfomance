using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class StrategicPlanningMastersControllerTests
{
    [Fact]
    public async Task EffectiveDatedMasterIsAuditedAndFilteredByFinancialYear()
    {
        await using var fixture = await Fixture.CreateAsync(); await using var context = fixture.Context(fixture.MunicipalityA.Id); var controller = Controller(context, fixture.MunicipalityA.Id);
        var created = await controller.CreateMunicipalKpa(new("MKPA-1", "Reliable services", null, fixture.Year2025.PublicId, fixture.Year2025.PublicId, 10, true, "Council adopted strategic master"));
        var dto = Assert.IsType<OkObjectResult>(created.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!;
        Assert.Equal("2025/26", dto.EffectiveFromFinancialYearCode);
        Assert.Single((Assert.IsType<OkObjectResult>((await controller.GetMunicipalKpas(new() { MunicipalityFinancialYearPublicId = fixture.Year2025.PublicId })).Result).Value.As<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>().Data!).Items);
        Assert.Empty((Assert.IsType<OkObjectResult>((await controller.GetMunicipalKpas(new() { MunicipalityFinancialYearPublicId = fixture.Year2026.PublicId })).Result).Value.As<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>().Data!).Items);
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityId == dto.PublicId.ToString() && item.Action == "Create");
    }

    [Fact]
    public async Task TenantBoundaryPreventsCrossMunicipalityRelationship()
    {
        await using var fixture = await Fixture.CreateAsync();
        Guid foreignGoal;
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
        {
            var result = await Controller(tenantB, fixture.MunicipalityB.Id).CreateStrategicGoal(new(null, "Tenant B goal", null, null, null, 10, true, "Tenant B strategic planning setup"));
            foreignGoal = Assert.IsType<OkObjectResult>(result.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!.PublicId;
        }
        await using var tenantA = fixture.Context(fixture.MunicipalityA.Id); var controller = Controller(tenantA, fixture.MunicipalityA.Id);
        var kpaResult = await controller.CreateMunicipalKpa(new(null, "Tenant A KPA", null, null, null, 10, true, "Tenant A strategic planning setup"));
        var kpa = Assert.IsType<OkObjectResult>(kpaResult.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!;
        var link = await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, foreignGoal, "Attempt prohibited cross tenant link"));
        Assert.IsType<NotFoundObjectResult>(link.Result);
    }

    [Fact]
    public async Task ConfigurableRelationshipCanBeCreatedAndSoftDisabledWithConcurrency()
    {
        await using var fixture = await Fixture.CreateAsync(); await using var context = fixture.Context(fixture.MunicipalityA.Id); var controller = Controller(context, fixture.MunicipalityA.Id);
        var kpa = Data(await controller.CreateMunicipalKpa(new(null, "KPA", null, null, null, 1, true, "Create KPA relationship parent")));
        var goal = Data(await controller.CreateStrategicGoal(new(null, "Goal", null, null, null, 1, true, "Create goal relationship child")));
        var linked = await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Council approved optional relationship"));
        var relationship = Assert.IsType<OkObjectResult>(linked.Result).Value.As<ApiResponse<StrategicPlanningRelationshipDto>>().Data!;
        var disabled = await controller.DisableRelationship(relationship.PublicId, new("Relationship superseded by council", relationship.RowVersion));
        var disabledDto = Assert.IsType<OkObjectResult>(disabled.Result).Value.As<ApiResponse<StrategicPlanningRelationshipDto>>().Data!;
        Assert.False((await context.MunicipalKpaStrategicGoals.SingleAsync()).IsActive);
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityId == relationship.PublicId.ToString() && item.Action == "DisableRelationship");
        Assert.IsType<ConflictObjectResult>((await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Attempt unsafe relationship reactivation"))).Result);
        Assert.IsType<OkObjectResult>((await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Council approved relationship reactivation", disabledDto.RowVersion))).Result);
    }

    [Fact]
    public async Task StaleMasterRowVersionReturnsConflict()
    {
        await using var fixture = await Fixture.CreateAsync(); Guid id; string stale;
        await using (var context = fixture.Context(fixture.MunicipalityA.Id)) { var dto = Data(await Controller(context, fixture.MunicipalityA.Id).CreateStrategicObjective(new("SO", "Objective", null, null, null, 1, true, "Create objective for concurrency"))); id = dto.PublicId; stale = dto.RowVersion; }
        await using (var writer = fixture.Context(fixture.MunicipalityA.Id)) { var entity = await writer.MunicipalStrategicObjectives.SingleAsync(); entity.Name = "Changed elsewhere"; await writer.SaveChangesAsync(); }
        await using (var context = fixture.Context(fixture.MunicipalityA.Id)) { var result = await Controller(context, fixture.MunicipalityA.Id).UpdateStrategicObjective(id, new("SO", "Stale", null, null, null, 1, true, "Attempt stale objective update", stale)); Assert.IsType<ConflictObjectResult>(result.Result); }
    }

    [Fact]
    public async Task SecurityRegistrySeedsAllMastersHierarchyAndNavigation()
    {
        await using var fixture = await Fixture.CreateAsync(); await using var context = fixture.SystemContext(); await SecurityRegistrySeeder.SeedAsync(context);
        var resources = await context.SecurityResources.Select(item => item.Code).ToArrayAsync();
        Assert.Contains("MUNICIPAL_KPA", resources); Assert.Contains("STRATEGIC_GOAL", resources); Assert.Contains("STRATEGIC_INTERVENTION", resources); Assert.Contains("STRATEGIC_OBJECTIVE", resources); Assert.Contains("PERFORMANCE_OBJECTIVE", resources); Assert.Contains("STRATEGIC_HIERARCHY", resources);
        Assert.Contains(await context.SecurityNavigationItems.ToArrayAsync(), item => item.Route == "/admin/strategic-interventions" && item.RequiredPermissionCode == "NAV.CONFIGURATION.STRATEGIC_INTERVENTIONS");
    }

    [Theory]
    [InlineData(nameof(StrategicPlanningMastersController.CreateMunicipalKpa), "Permission:MUNICIPAL_KPA.CREATE")]
    [InlineData(nameof(StrategicPlanningMastersController.UpdateStrategicGoal), "Permission:STRATEGIC_GOAL.UPDATE")]
    [InlineData(nameof(StrategicPlanningMastersController.GetStrategicInterventions), "Permission:STRATEGIC_INTERVENTION.READ")]
    [InlineData(nameof(StrategicPlanningMastersController.LinkGoalToObjective), "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public void EndpointsCarryDynamicPermissionPolicies(string methodName, string policy)
    {
        var method = typeof(StrategicPlanningMastersController).GetMethods().Single(item => item.Name == methodName);
        Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), item => item.Policy == policy);
    }

    private static StrategicPlanningMasterDto Data(Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> task) => Data(task.GetAwaiter().GetResult());
    private static StrategicPlanningMasterDto Data(ActionResult<ApiResponse<StrategicPlanningMasterDto>> result) => Assert.IsType<OkObjectResult>(result.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!;
    private static StrategicPlanningMastersController Controller(ApplicationDbContext context, long municipalityId) { var controller = new StrategicPlanningMastersController(context, new TestTenantContext(municipalityId, "planner-1", false)); controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; return controller; }

    private sealed class Fixture(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options, Municipality municipalityA, Municipality municipalityB, MunicipalityFinancialYear year2025, MunicipalityFinancialYear year2026) : IAsyncDisposable
    {
        public Municipality MunicipalityA { get; } = municipalityA; public Municipality MunicipalityB { get; } = municipalityB; public MunicipalityFinancialYear Year2025 { get; } = year2025; public MunicipalityFinancialYear Year2026 { get; } = year2026;
        public ApplicationDbContext Context(long municipalityId) => new(options, new TestTenantContext(municipalityId, "planner-1", false)); public ApplicationDbContext SystemContext() => new(options, new TestTenantContext(null, "setup", true));
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var municipalityA = new Municipality { Code = "SPA", Name = "Strategic A" }; var municipalityB = new Municipality { Code = "SPB", Name = "Strategic B" };
            var financial2025 = new FinancialYear { Code = "2025/26", Name = "2025/26", StartDate = new(2025, 7, 1), EndDate = new(2026, 6, 30) }; var financial2026 = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
            var year2025 = new MunicipalityFinancialYear { Municipality = municipalityA, FinancialYear = financial2025, EffectiveFrom = financial2025.StartDate, IsActive = true, IsCurrent = true }; var year2026 = new MunicipalityFinancialYear { Municipality = municipalityA, FinancialYear = financial2026, EffectiveFrom = financial2026.StartDate, IsActive = true };
            await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "setup", true))) { await setup.Database.EnsureCreatedAsync(); setup.AddRange(municipalityA, municipalityB, financial2025, financial2026, year2025, year2026); await setup.SaveChangesAsync(); setup.Users.Add(new ApplicationUser { Id = "planner-1", UserName = "planner@test", NormalizedUserName = "PLANNER@TEST", FirstName = "Plan", LastName = "User", MunicipalityId = municipalityA.Id, IsActive = true }); await setup.SaveChangesAsync(); }
            return new(connection, options, municipalityA, municipalityB, year2025, year2026);
        }
        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }

    private sealed class TestTenantContext(long? municipalityId, string userId, bool isSystem) : ITenantContext { public long? MunicipalityId { get; } = municipalityId; public string? UserId { get; } = userId; public bool IsSystem { get; } = isSystem; }
}

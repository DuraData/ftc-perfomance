using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
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

public sealed class GlobalStrategicReferencesControllerTests
{
    [Fact]
    public async Task MunicipalityAvailabilityIsIsolatedAndDefaultsToEnabled()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var tenantA = fixture.Context(fixture.MunicipalityA.Id))
        {
            var controller = Controller(tenantA, fixture.MunicipalityA.Id);
            var update = await controller.SetNationalKpaAvailability(fixture.NationalKpa.PublicId, new(false, "Not applicable for this municipality"));
            Assert.IsType<OkObjectResult>(update.Result);
            Assert.False(Assert.IsType<OkObjectResult>(update.Result).Value.As<ApiResponse<GlobalStrategicReferenceDto>>().Data!.IsEnabledForMunicipality);
        }

        await using (var tenantA = fixture.Context(fixture.MunicipalityA.Id))
        {
            var controller = Controller(tenantA, fixture.MunicipalityA.Id);
            var response = Assert.IsType<OkObjectResult>((await controller.GetNationalKpasPage(new PagedQueryRequest { SortBy = "displayOrder", SortDirection = "asc" })).Result);
            Assert.False(response.Value.As<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>>().Data!.Items.Single().IsEnabledForMunicipality);
            var available = Assert.IsType<OkObjectResult>((await controller.GetNationalKpasPage(new PagedQueryRequest(), active: true, enabledForMunicipality: true)).Result);
            Assert.Empty(available.Value.As<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>>().Data!.Items);
            Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetNationalKpas().Result).StatusCode);
            Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetBackToBasicsPillars().Result).StatusCode);
            Assert.Empty(Assert.IsType<OkObjectResult>((await controller.GetBackToBasicsPillarsPage(new PagedQueryRequest())).Result).Value.As<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>>().Data!.Items);
            Assert.IsType<BadRequestObjectResult>((await controller.GetNationalKpasPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
        }
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
        {
            var response = Assert.IsType<OkObjectResult>((await Controller(tenantB, fixture.MunicipalityB.Id).GetNationalKpasPage(new PagedQueryRequest())).Result);
            var dto = response.Value.As<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>>().Data!.Items.Single();
            Assert.True(dto.IsEnabledForMunicipality);
            Assert.Null(dto.AvailabilityPublicId);
        }
    }

    [Fact]
    public async Task GlobalMutationRequiresSystemScopeAndProducesAuditEvidence()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var context = fixture.Context(fixture.MunicipalityA.Id);
        var denied = await Controller(context, fixture.MunicipalityA.Id, isSystem: false).CreateNationalKpa(new("LED", "Local Economic Development", null, 20, true, "Council approved catalogue"));
        Assert.IsType<ObjectResult>(denied.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(denied.Result).StatusCode);

        var allowed = await Controller(context, fixture.MunicipalityA.Id, isSystem: true).CreateNationalKpa(new("LED", "Local Economic Development", null, 20, true, "Council approved catalogue"));
        Assert.IsType<OkObjectResult>(allowed.Result);
        var entity = await context.NationalKpas.SingleAsync(item => item.Code == "LED");
        var audit = await context.AuditTrails.SingleAsync(item => item.EntityId == entity.PublicId.ToString());
        Assert.Equal("Create", audit.Action);
        Assert.Equal("Council approved catalogue", audit.Reason);
    }

    [Fact]
    public async Task StaleGlobalRowVersionReturnsConflict()
    {
        await using var fixture = await Fixture.CreateAsync();
        string staleVersion;
        await using (var first = fixture.Context(fixture.MunicipalityA.Id))
        {
            staleVersion = Convert.ToBase64String((await first.NationalKpas.SingleAsync()).RowVersion);
        }
        await using (var writer = fixture.Context(fixture.MunicipalityA.Id))
        {
            var entity = await writer.NationalKpas.SingleAsync();
            entity.Name = "Updated elsewhere";
            await writer.SaveChangesAsync();
        }
        await using (var stale = fixture.Context(fixture.MunicipalityA.Id))
        {
            var result = await Controller(stale, fixture.MunicipalityA.Id).UpdateNationalKpa(fixture.NationalKpa.PublicId, new("BSD", "Stale edit", null, 10, true, "Attempt stale catalogue edit", staleVersion));
            Assert.IsType<ConflictObjectResult>(result.Result);
        }
    }

    [Theory]
    [InlineData(nameof(GlobalStrategicReferencesController.GetNationalKpasPage), "Permission:NATIONAL_KPA.READ")]
    [InlineData(nameof(GlobalStrategicReferencesController.CreateNationalKpa), "Permission:NATIONAL_KPA.CREATE")]
    [InlineData(nameof(GlobalStrategicReferencesController.UpdateNationalKpa), "Permission:NATIONAL_KPA.UPDATE")]
    [InlineData(nameof(GlobalStrategicReferencesController.GetBackToBasicsPillarsPage), "Permission:BACK_TO_BASICS_PILLAR.READ")]
    public void EndpointsCarryDynamicPermissionPolicies(string methodName, string policy)
    {
        var method = typeof(GlobalStrategicReferencesController).GetMethods().Single(item => item.Name == methodName);
        Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), item => item.Policy == policy);
    }

    [Fact]
    public async Task SecurityRegistrySeedsIndependentResourcesAndNavigation()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var context = fixture.SystemContext();
        await SecurityRegistrySeeder.SeedAsync(context);
        Assert.Contains(await context.SecurityResources.ToArrayAsync(), item => item.Code == "NATIONAL_KPA" && item.SupportsCreate && item.SupportsUpdate);
        Assert.Contains(await context.SecurityResources.ToArrayAsync(), item => item.Code == "BACK_TO_BASICS_PILLAR" && item.SupportsCreate && item.SupportsUpdate);
        Assert.Contains(await context.Permissions.ToArrayAsync(), item => item.Code == "NATIONAL_KPA.READ" && item.Kind == SecurityPermissionKind.Resource);
        Assert.Contains(await context.Permissions.ToArrayAsync(), item => item.Code == "NAV.CONFIGURATION.NATIONAL_KPAS" && item.Kind == SecurityPermissionKind.Navigation);
        Assert.Contains(await context.SecurityNavigationItems.ToArrayAsync(), item => item.Route == "/admin/back-to-basics-pillars" && item.RequiredPermissionCode == "NAV.CONFIGURATION.BACK_TO_BASICS");
    }

    private static GlobalStrategicReferencesController Controller(ApplicationDbContext context, long municipalityId, bool isSystem = true)
    {
        var controller = new GlobalStrategicReferencesController(context, new TestTenantContext(municipalityId, "user-1", isSystem));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private sealed class Fixture(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options, Municipality municipalityA, Municipality municipalityB, NationalKpa nationalKpa) : IAsyncDisposable
    {
        public Municipality MunicipalityA { get; } = municipalityA;
        public Municipality MunicipalityB { get; } = municipalityB;
        public NationalKpa NationalKpa { get; } = nationalKpa;
        public ApplicationDbContext Context(long municipalityId) => new(options, new TestTenantContext(municipalityId, "user-1", true));
        public ApplicationDbContext SystemContext() => new(options, new TestTenantContext(null, "user-1", true));

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var municipalityA = new Municipality { Code = "A", Name = "Municipality A" };
            var municipalityB = new Municipality { Code = "B", Name = "Municipality B" };
            var nationalKpa = new NationalKpa { Code = "BSD", Name = "Basic Service Delivery", DisplayOrder = 10 };
            await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "setup", true)))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.AddRange(municipalityA, municipalityB, nationalKpa);
                await setup.SaveChangesAsync();
                setup.Users.Add(new ApplicationUser { Id = "user-1", UserName = "governance@example.test", NormalizedUserName = "GOVERNANCE@EXAMPLE.TEST", Email = "governance@example.test", NormalizedEmail = "GOVERNANCE@EXAMPLE.TEST", FirstName = "Governance", LastName = "User", MunicipalityId = municipalityA.Id, IsActive = true });
                await setup.SaveChangesAsync();
            }
            return new Fixture(connection, options, municipalityA, municipalityB, nationalKpa);
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }

    private sealed class TestTenantContext(long? municipalityId, string userId, bool isSystem) : ITenantContext
    {
        public long? MunicipalityId { get; } = municipalityId;
        public string? UserId { get; } = userId;
        public bool IsSystem { get; } = isSystem;
    }
}

internal static class StrategicReferenceTestCasting
{
    public static T As<T>(this object? value) => Assert.IsType<T>(value);
}

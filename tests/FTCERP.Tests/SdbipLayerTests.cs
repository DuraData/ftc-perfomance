using System.Security.Claims;
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

public sealed class SdbipLayerTests
{
    [Fact]
    public async Task Layers_are_tenant_year_scoped_versioned_audited_and_protected_from_active_kpi_deactivation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var system = new TestTenantContext(null, "system", true);
        Guid yearAPublicId;
        long tenantAId;
        long tenantBId;
        await using (var setup = new ApplicationDbContext(options, system))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "LAY-A", Name = "Layer Tenant A" };
            var tenantB = new Municipality { Code = "LAY-B", Name = "Layer Tenant B" };
            var year = new FinancialYear { Code = "2030/31", Name = "2030/31", StartDate = new DateTime(2030, 7, 1), EndDate = new DateTime(2031, 6, 30) };
            setup.AddRange(tenantA, tenantB, year);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id; tenantBId = tenantB.Id;
            var linkA = new MunicipalityFinancialYear { MunicipalityId = tenantA.Id, FinancialYearId = year.Id, FinancialYear = year, IsCurrent = true, EffectiveFrom = year.StartDate };
            var linkB = new MunicipalityFinancialYear { MunicipalityId = tenantB.Id, FinancialYearId = year.Id, FinancialYear = year, IsCurrent = true, EffectiveFrom = year.StartDate };
            setup.AddRange(linkA, linkB, User("layer-admin-a", tenantA.Id), User("layer-admin-b", tenantB.Id));
            await setup.SaveChangesAsync();
            yearAPublicId = linkA.PublicId;
        }

        await using var contextA = new ApplicationDbContext(options, new TestTenantContext(tenantAId, "layer-admin-a"));
        var controllerA = Controller(contextA, tenantAId, "layer-admin-a");
        var create = await controllerA.CreateSdbipLayer(new SaveSdbipLayerRequest(yearAPublicId, "TOP", "Top Layer SDBIP", "Approved strategic layer", 1, "Initial approved layer configuration"));
        var created = Assert.IsType<ApiResponse<SdbipLayerDto>>(Assert.IsType<OkObjectResult>(create.Result).Value).Data!;
        Assert.Equal("TOP", created.Code);
        Assert.Single(await contextA.AuditTrails.Where(item => item.EntityName == nameof(SdbipLayer) && item.Action == "Create").ToArrayAsync());

        var stale = created.RowVersion;
        var update = await controllerA.UpdateSdbipLayer(created.PublicId, new UpdateSdbipLayerRequest("TOP", "Top Layer Service Delivery and Budget Implementation Plan", "Approved renamed layer", 2, true, "Council approved the layer display name", created.RowVersion));
        var updated = Assert.IsType<ApiResponse<SdbipLayerDto>>(Assert.IsType<OkObjectResult>(update.Result).Value).Data!;
        Assert.Equal(2, updated.DisplayOrder);
        var staleUpdate = await controllerA.UpdateSdbipLayer(created.PublicId, new UpdateSdbipLayerRequest("TOP", "Stale name", null, 3, true, "Attempt an obsolete concurrent update", stale));
        Assert.IsType<ConflictObjectResult>(staleUpdate.Result);

        await using (var protectedContext = new ApplicationDbContext(options, new TestTenantContext(tenantAId, "layer-admin-a")))
        {
            var layer = await protectedContext.SdbipLayers.SingleAsync();
            protectedContext.OpmsTargets.Add(new OpmsTarget { MunicipalityId = tenantAId, SdbipLayerId = layer.Id, IndicatorNumber = "KPI-1", TargetName = "Target", KpiDescription = "Measure", NationalKpa = "KPA", MunicipalKpa = "MKPA", PerformanceObjective = "Objective", OriginalOrderNumber = 1, RevisedOrderNumber = 1 });
            await protectedContext.SaveChangesAsync();
            var deactivate = await Controller(protectedContext, tenantAId, "layer-admin-a").UpdateSdbipLayer(created.PublicId, new UpdateSdbipLayerRequest(layer.Code, layer.Name, layer.Description, layer.DisplayOrder, false, "Retire this configured SDBIP layer", Convert.ToBase64String(layer.RowVersion)));
            Assert.IsType<ConflictObjectResult>(deactivate.Result);
        }

        await using var contextB = new ApplicationDbContext(options, new TestTenantContext(tenantBId, "layer-admin-b"));
        Assert.Empty(await contextB.SdbipLayers.ToArrayAsync());
        Assert.Empty(Assert.IsType<ApiResponse<SdbipLayerDto[]>>(Assert.IsType<OkObjectResult>((await Controller(contextB, tenantBId, "layer-admin-b").GetSdbipLayers()).Result).Value).Data!);
    }

    [Fact]
    public async Task Database_rejects_cross_tenant_and_duplicate_year_layer_codes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var tenant = new TestTenantContext(901, "layer-admin");
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Id = 901, Code = "M901", Name = "Municipality 901" };
        var year = new FinancialYear { Code = "2031/32", Name = "2031/32", StartDate = new DateTime(2031, 7, 1), EndDate = new DateTime(2032, 6, 30) };
        var link = new MunicipalityFinancialYear { MunicipalityId = 901, FinancialYear = year, IsCurrent = true, EffectiveFrom = year.StartDate };
        context.AddRange(municipality, year, link);
        await context.SaveChangesAsync();
        context.SdbipLayers.Add(new SdbipLayer { MunicipalityId = 901, MunicipalityFinancialYearId = link.Id, Code = "OPS", Name = "Operational", DisplayOrder = 1 });
        await context.SaveChangesAsync();
        context.SdbipLayers.Add(new SdbipLayer { MunicipalityId = 901, MunicipalityFinancialYearId = link.Id, Code = "OPS", Name = "Duplicate", DisplayOrder = 2 });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        var secondMunicipality = new Municipality { Id = 902, Code = "M902", Name = "Municipality 902" };
        context.Municipalities.Add(secondMunicipality);
        await context.SaveChangesAsync();
        await using var systemContext = new ApplicationDbContext(options, new TestTenantContext(null, "system", true));
        systemContext.SdbipLayers.Add(new SdbipLayer { MunicipalityId = 902, MunicipalityFinancialYearId = link.Id, Code = "CROSS", Name = "Cross tenant", DisplayOrder = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => systemContext.SaveChangesAsync());
    }

    private static ApplicationUser User(string id, long municipalityId) => new() { Id = id, UserName = $"{id}@example.test", NormalizedUserName = $"{id.ToUpperInvariant()}@EXAMPLE.TEST", Email = $"{id}@example.test", NormalizedEmail = $"{id.ToUpperInvariant()}@EXAMPLE.TEST", MunicipalityId = municipalityId, IsActive = true };
    private static TenantMastersController Controller(ApplicationDbContext context, long municipalityId, string userId)
    {
        var http = new DefaultHttpContext { TraceIdentifier = $"layer-test-{Guid.NewGuid():N}" };
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        return new TenantMastersController(context, new TestTenantContext(municipalityId, userId)) { ControllerContext = new ControllerContext { HttpContext = http } };
    }
    private sealed class TestTenantContext(long? municipalityId, string userId, bool isSystem = false) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => userId;
    }
}

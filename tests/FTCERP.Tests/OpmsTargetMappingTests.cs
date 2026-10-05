using FTCERP.Host.API.Controllers;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class OpmsTargetMappingTests
{
    [Fact]
    public async Task Organization_scope_resolves_public_ids_rejects_cross_tenant_values_and_projects_public_ids()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;

        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var tenantA = new Municipality { Code = "SCOPE-A", Name = "Scope A" };
        var tenantB = new Municipality { Code = "SCOPE-B", Name = "Scope B" };
        context.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var departmentA = new Department { MunicipalityId = tenantA.Id, PublicId = Guid.NewGuid(), Code = "FIN", Name = "Finance", IsActive = true };
        var departmentB = new Department { MunicipalityId = tenantB.Id, PublicId = Guid.NewGuid(), Code = "CORP", Name = "Corporate", IsActive = true };
        context.AddRange(departmentA, departmentB);
        await context.SaveChangesAsync();
        var unitA = new Unit { MunicipalityId = tenantA.Id, PublicId = Guid.NewGuid(), DepartmentId = departmentA.Id, Code = "REV", Name = "Revenue", IsActive = true };
        context.Add(unitA);
        await context.SaveChangesAsync();

        var resolved = await PerformanceApiSupport.ResolveOrganizationScopeAsync(
            context, tenantA.Id, null, null, departmentA.PublicId, unitA.PublicId);

        Assert.Null(resolved.Error);
        Assert.Equal(departmentA.Id, resolved.DepartmentId);
        Assert.Equal(unitA.Id, resolved.UnitId);

        var rejected = await PerformanceApiSupport.ResolveOrganizationScopeAsync(
            context, tenantA.Id, null, null, departmentB.PublicId, null);

        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.DepartmentId);
        Assert.Null(rejected.UnitId);

        var response = new OpmsTarget
        {
            MunicipalityId = tenantA.Id,
            IndicatorNumber = "1",
            TargetName = "Public organization scope",
            KpiDescription = "Scope mapping",
            AnnualTargetDescription = "One",
            Department = departmentA,
            DepartmentId = departmentA.Id,
            Unit = unitA,
            UnitId = unitA.Id
        }.ToResponse();

        Assert.Equal(departmentA.PublicId, response.DepartmentPublicId);
        Assert.Equal(unitA.PublicId, response.UnitPublicId);
    }

    [Fact]
    public async Task Sqlite_enforces_relational_target_mapping_uniqueness_and_tenant_isolation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        int wardId;
        int voteId;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "MAP-A", Name = "Mapping A" };
            var municipalityB = new Municipality { Code = "MAP-B", Name = "Mapping B" };
            setup.AddRange(tenantA, municipalityB); await setup.SaveChangesAsync(); tenantAId = tenantA.Id; tenantBId = municipalityB.Id;
            var department = new Department { MunicipalityId = tenantAId, Code = "FIN", Name = "Finance" };
            var ward = new Ward { MunicipalityId = tenantAId, Code = "W1", Name = "Ward 1", LegacyMunicipality = tenantA.Name, IsActive = true };
            setup.AddRange(department, ward); await setup.SaveChangesAsync(); wardId = ward.Id;
            var vote = new VoteNumber { MunicipalityId = tenantAId, Code = "V1", Number = "001", Name = "Operations", DepartmentId = department.Id, IsActive = true };
            setup.Add(vote); await setup.SaveChangesAsync(); voteId = vote.Id;
        }

        await using (var tenantA = new ApplicationDbContext(options, new TenantContext(tenantAId, "mapping-user")))
        {
            var target = new OpmsTarget { IndicatorNumber = "1", TargetName = "Mapped target", KpiDescription = "Mapping", AnnualTargetDescription = "One", MunicipalityId = tenantAId };
            tenantA.OpmsTargets.Add(target);
            tenantA.OpmsTargetWards.Add(new OpmsTargetWard { MunicipalityId = tenantAId, OpmsTargetId = target.Id, WardId = wardId });
            tenantA.OpmsTargetVoteNumbers.Add(new OpmsTargetVoteNumber { MunicipalityId = tenantAId, OpmsTargetId = target.Id, VoteNumberId = voteId });
            await tenantA.SaveChangesAsync();

            tenantA.OpmsTargetWards.Add(new OpmsTargetWard { MunicipalityId = tenantAId, OpmsTargetId = target.Id, WardId = wardId });
            await Assert.ThrowsAsync<DbUpdateException>(() => tenantA.SaveChangesAsync());
            tenantA.ChangeTracker.Clear();
            var loaded = await tenantA.OpmsTargets.Include(item => item.Wards).Include(item => item.VoteNumbers).SingleAsync();
            var response = loaded.ToResponse();
            Assert.Equal([wardId], response.WardIds);
            Assert.Equal([voteId], response.VoteNumberIds);
        }

        await using var tenantB = new ApplicationDbContext(options, new TenantContext(tenantBId, "other-user"));
        Assert.Empty(await tenantB.OpmsTargetWards.ToArrayAsync());
        Assert.Empty(await tenantB.OpmsTargetVoteNumbers.ToArrayAsync());
    }

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext { public long? MunicipalityId => municipalityId; public bool IsSystem => false; public string? UserId => userId; }
    private sealed class SystemTenantContext : ITenantContext { public long? MunicipalityId => null; public bool IsSystem => true; public string? UserId => "system"; }
}

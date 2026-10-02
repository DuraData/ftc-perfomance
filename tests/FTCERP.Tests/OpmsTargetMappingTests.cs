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

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

public sealed class TenantCalendarGovernanceTests
{
    [Fact]
    public async Task System_financial_year_update_keeps_audit_system_scoped_when_a_municipality_is_selected()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        const long municipalityId = 740;
        const string actorId = "system-calendar-governor";
        Guid yearPublicId;

        await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "system", true)))
        {
            await setup.Database.EnsureCreatedAsync();
            var municipality = new Municipality { Id = municipalityId, Code = "CAL-740", Name = "System Calendar Municipality" };
            var actor = User(actorId, municipalityId);
            var year = new FinancialYear { Code = "2030/31", Name = "2030/31", StartDate = new DateTime(2030, 7, 1), EndDate = new DateTime(2031, 6, 30) };
            setup.AddRange(municipality, actor, year);
            await setup.SaveChangesAsync();
            yearPublicId = year.PublicId;
        }

        var tenant = new TestTenantContext(municipalityId, actorId, true);
        await using var context = new ApplicationDbContext(options, tenant);
        var controller = Controller(context, tenant, actorId);
        var yearToUpdate = await context.FinancialYears.SingleAsync(item => item.PublicId == yearPublicId);
        var update = await controller.UpdateFinancialYear(yearToUpdate.PublicId,
            new SaveFinancialYearRequest(yearToUpdate.Code, "2030/31 Municipal Financial Year", yearToUpdate.StartDate, yearToUpdate.EndDate,
                "Clarify the governed global financial year name", true, Convert.ToBase64String(yearToUpdate.RowVersion)));

        Assert.IsType<OkObjectResult>(update.Result);
        var audit = Assert.Single(await context.AuditTrails.IgnoreQueryFilters()
            .Where(item => item.EntityName == nameof(FinancialYear) && item.Action == "Update").ToArrayAsync());
        Assert.Null(audit.MunicipalityId);
        Assert.Equal("Clarify the governed global financial year name", audit.Reason);
        Assert.Contains("\"Name\":\"2030/31\"", audit.OldValue);
        Assert.Contains("\"Name\":\"2030/31 Municipal Financial Year\"", audit.NewValue);
    }

    [Fact]
    public async Task Promoting_current_year_and_updating_period_append_reasoned_before_after_audits()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        const long municipalityId = 741;
        const string actorId = "calendar-governor";
        Guid nextYearPublicId;
        Guid periodPublicId;

        await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "system", true)))
        {
            await setup.Database.EnsureCreatedAsync();
            var municipality = new Municipality { Id = municipalityId, Code = "CAL-741", Name = "Calendar Municipality" };
            var actor = User(actorId, municipalityId);
            var firstYear = new FinancialYear { Code = "2031/32", Name = "2031/32", StartDate = new DateTime(2031, 7, 1), EndDate = new DateTime(2032, 6, 30) };
            var secondYear = new FinancialYear { Code = "2032/33", Name = "2032/33", StartDate = new DateTime(2032, 7, 1), EndDate = new DateTime(2033, 6, 30) };
            setup.AddRange(municipality, actor, firstYear, secondYear);
            await setup.SaveChangesAsync();
            var current = new MunicipalityFinancialYear { MunicipalityId = municipalityId, FinancialYear = firstYear, FinancialYearId = firstYear.Id, IsCurrent = true, EffectiveFrom = firstYear.StartDate };
            var next = new MunicipalityFinancialYear { MunicipalityId = municipalityId, FinancialYear = secondYear, FinancialYearId = secondYear.Id, EffectiveFrom = secondYear.StartDate };
            setup.AddRange(current, next);
            await setup.SaveChangesAsync();
            var initialPeriod = new ReportingPeriod
            {
                MunicipalityFinancialYear = next, MunicipalityFinancialYearId = next.Id, Code = "Q1", Name = "Quarter 1",
                PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = secondYear.StartDate, EndDate = new DateTime(2032, 9, 30)
            };
            setup.ReportingPeriods.Add(initialPeriod);
            await setup.SaveChangesAsync();
            nextYearPublicId = next.PublicId;
            periodPublicId = initialPeriod.PublicId;
        }

        await using var context = new ApplicationDbContext(options, new TestTenantContext(municipalityId, actorId));
        var controller = Controller(context, municipalityId, actorId);
        var nextYear = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleAsync(item => item.PublicId == nextYearPublicId);
        var promote = await controller.UpdateMunicipalityFinancialYear(nextYear.PublicId,
            new UpdateMunicipalityFinancialYearRequest(true, true, nextYear.EffectiveFrom, null,
                "Council approved the next reporting year", Convert.ToBase64String(nextYear.RowVersion)));

        Assert.IsType<OkObjectResult>(promote.Result);
        var years = await context.MunicipalityFinancialYears.OrderBy(item => item.EffectiveFrom).ToArrayAsync();
        Assert.False(years[0].IsCurrent);
        Assert.True(years[1].IsCurrent);
        var yearAudits = await context.AuditTrails.Where(item => item.EntityName == nameof(MunicipalityFinancialYear)).ToArrayAsync();
        Assert.Equal(2, yearAudits.Length);
        Assert.All(yearAudits, audit => Assert.Equal("Council approved the next reporting year", audit.Reason));
        Assert.Contains(yearAudits, audit => audit.OldValue!.Contains("\"IsCurrent\":true") && audit.NewValue!.Contains("\"IsCurrent\":false"));
        Assert.Contains(yearAudits, audit => audit.OldValue!.Contains("\"IsCurrent\":false") && audit.NewValue!.Contains("\"IsCurrent\":true"));

        var period = await context.ReportingPeriods.Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .SingleAsync(item => item.PublicId == periodPublicId);
        var update = await controller.UpdateReportingPeriod(period.PublicId,
            new UpdateReportingPeriodRequest("First Quarter", ReportingPeriodType.Quarter1, 1, period.StartDate, period.EndDate, true,
                "Align the approved reporting-period label", Convert.ToBase64String(period.RowVersion)));

        Assert.IsType<OkObjectResult>(update.Result);
        var periodAudit = Assert.Single(await context.AuditTrails.Where(item => item.EntityName == nameof(ReportingPeriod)).ToArrayAsync());
        Assert.Equal("Align the approved reporting-period label", periodAudit.Reason);
        Assert.Contains("\"Name\":\"Quarter 1\"", periodAudit.OldValue);
        Assert.Contains("\"Name\":\"First Quarter\"", periodAudit.NewValue);
    }

    [Fact]
    public async Task Persistence_rejects_unaudited_rewrite_and_hard_delete_of_calendar_masters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        const long municipalityId = 742;
        await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "system", true)))
        {
            await setup.Database.EnsureCreatedAsync();
            var municipality = new Municipality { Id = municipalityId, Code = "CAL-742", Name = "Protected Calendar Municipality" };
            var year = new FinancialYear { Code = "2033/34", Name = "2033/34", StartDate = new DateTime(2033, 7, 1), EndDate = new DateTime(2034, 6, 30) };
            setup.AddRange(municipality, year);
            await setup.SaveChangesAsync();
            var municipalYear = new MunicipalityFinancialYear { MunicipalityId = municipalityId, FinancialYear = year, FinancialYearId = year.Id, IsCurrent = true, EffectiveFrom = year.StartDate };
            setup.MunicipalityFinancialYears.Add(municipalYear);
            await setup.SaveChangesAsync();
            setup.ReportingPeriods.Add(new ReportingPeriod
            {
                MunicipalityFinancialYear = municipalYear, MunicipalityFinancialYearId = municipalYear.Id, Code = "Q1", Name = "Quarter 1",
                PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = year.StartDate, EndDate = new DateTime(2033, 9, 30)
            });
            setup.SdbipLayers.Add(new SdbipLayer
            {
                MunicipalityId = municipalityId, MunicipalityFinancialYear = municipalYear, MunicipalityFinancialYearId = municipalYear.Id,
                Code = "TOP", Name = "Top Layer", DisplayOrder = 1
            });
            await setup.SaveChangesAsync();
        }

        await using var context = new ApplicationDbContext(options, new TestTenantContext(municipalityId, "calendar-governor"));
        var period = await context.ReportingPeriods.SingleAsync();
        period.Name = "Unaudited rewrite";
        var rewrite = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("same-transaction reasoned before/after audit evidence", rewrite.Message);

        context.ChangeTracker.Clear();
        var layer = await context.SdbipLayers.SingleAsync();
        context.SdbipLayers.Remove(layer);
        var deletion = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("cannot be hard deleted", deletion.Message);
    }

    private static TenantMastersController Controller(ApplicationDbContext context, long municipalityId, string actorId)
        => Controller(context, new TestTenantContext(municipalityId, actorId), actorId);

    private static TenantMastersController Controller(ApplicationDbContext context, ITenantContext tenantContext, string actorId)
    {
        var http = new DefaultHttpContext { TraceIdentifier = $"calendar-governance-{Guid.NewGuid():N}" };
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actorId)], "test"));
        return new TenantMastersController(context, tenantContext)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static ApplicationUser User(string id, long municipalityId) => new()
    {
        Id = id, PublicId = Guid.NewGuid(), UserName = $"{id}@example.test", NormalizedUserName = $"{id.ToUpperInvariant()}@EXAMPLE.TEST",
        Email = $"{id}@example.test", NormalizedEmail = $"{id.ToUpperInvariant()}@EXAMPLE.TEST", MunicipalityId = municipalityId, IsActive = true
    };

    private sealed class TestTenantContext(long? municipalityId, string userId, bool isSystem = false) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => userId;
    }
}

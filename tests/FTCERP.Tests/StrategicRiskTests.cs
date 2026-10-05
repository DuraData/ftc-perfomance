using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class StrategicRiskTests
{
    [Fact]
    public async Task Security_registry_seeds_risk_permissions_and_actions_idempotently()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);

        var codes = await context.Permissions.Where(item => item.Code.StartsWith("STRATEGIC_RISK"))
            .Select(item => item.Code).OrderBy(item => item).ToArrayAsync();
        codes.Should().Equal("STRATEGIC_RISK.CREATE", "STRATEGIC_RISK.EXPORT", "STRATEGIC_RISK.IMPORT", "STRATEGIC_RISK.LINK_KPI",
            "STRATEGIC_RISK.READ", "STRATEGIC_RISK.UNLINK_KPI", "STRATEGIC_RISK.UPDATE");
    }

    [Fact]
    public async Task Risk_master_is_effective_dated_audited_and_concurrency_protected()
    {
        const long municipalityId = 9101;
        var user = IdpTestFixture.CreateUser("risk-admin");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityId, user.Id));
        var seed = await SeedAsync(context, municipalityId, user);
        var audit = new Mock<IWorkflowGovernanceService>();
        var controller = Controller(context, user, municipalityId, audit);

        var created = Data(await controller.Create(new SaveStrategicRiskRequest(
            "sr-01", "Water service interruption", "Loss of bulk supply", seed.Year.PublicId, seed.Year.PublicId,
            true, "Risk committee resolution", null)));

        created.RiskReference.Should().Be("SR-01");
        created.EffectiveFromFinancialYear.Should().Be("2026/27");
        created.RowVersion.Should().NotBeNullOrWhiteSpace();
        var updated = Data(await controller.Update(created.PublicId, new SaveStrategicRiskRequest(
            "sr-01", "Bulk water interruption", "Updated assessment", seed.Year.PublicId, seed.Year.PublicId,
            true, "Approved wording", created.RowVersion)));
        updated.RiskTitle.Should().Be("Bulk water interruption");

        var stale = await controller.Update(created.PublicId, new SaveStrategicRiskRequest(
            "sr-01", "Stale edit", null, seed.Year.PublicId, seed.Year.PublicId,
            true, "Stale update", created.RowVersion));
        stale.Result.Should().BeOfType<ConflictObjectResult>();
        audit.Verify(service => service.QueueAuditTrail(nameof(StrategicRisk), created.PublicId.ToString(), "Create",
            null, It.IsAny<object>(), user.Id, It.IsAny<string?>(), "Risk committee resolution"), Times.Once);
        audit.Verify(service => service.QueueAuditTrail(nameof(StrategicRisk), created.PublicId.ToString(), "Update",
            It.IsAny<object>(), It.IsAny<object>(), user.Id, It.IsAny<string?>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Kpi_links_support_multiple_risks_one_primary_soft_unlink_and_relink()
    {
        const long municipalityId = 9102;
        var user = IdpTestFixture.CreateUser("risk-linker");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityId, user.Id));
        var seed = await SeedAsync(context, municipalityId, user);
        var controller = Controller(context, user, municipalityId);
        var riskOne = Data(await controller.Create(Request("SR-A", "Flood risk", seed.Year.PublicId)));
        var riskTwo = Data(await controller.Create(Request("SR-B", "Energy risk", seed.Year.PublicId)));

        var first = Data(await controller.LinkKpi(new LinkStrategicRiskRequest(riskOne.PublicId, seed.Target.PublicId, true, "Primary exposure")));
        var second = Data(await controller.LinkKpi(new LinkStrategicRiskRequest(riskTwo.PublicId, seed.Target.PublicId, true, "More material exposure")));
        second.IsPrimary.Should().BeTrue();
        (await context.OpmsKpiStrategicRisks.SingleAsync(item => item.PublicId == first.PublicId)).IsPrimary.Should().BeFalse();

        var duplicate = await controller.LinkKpi(new LinkStrategicRiskRequest(riskTwo.PublicId, seed.Target.PublicId, false, "Duplicate"));
        duplicate.Result.Should().BeOfType<ConflictObjectResult>();
        var unlinked = Data(await controller.UnlinkKpi(second.PublicId, new UnlinkStrategicRiskRequest("Mitigation completed", second.RowVersion)));
        unlinked.IsActive.Should().BeFalse();
        unlinked.UnlinkReason.Should().Be("Mitigation completed");

        var relinked = Data(await controller.LinkKpi(new LinkStrategicRiskRequest(riskTwo.PublicId, seed.Target.PublicId, false, "Risk re-emerged")));
        relinked.IsActive.Should().BeTrue();
        (await context.OpmsKpiStrategicRisks.CountAsync(item => item.StrategicRiskId ==
            context.StrategicRisks.Single(risk => risk.PublicId == riskTwo.PublicId).Id)).Should().Be(2);
        var history = Data(await controller.GetLinksPage(new PagedQueryRequest
        {
            Page = 1, PageSize = 20, SortBy = "linkedAt", SortDirection = "asc"
        }, includeInactive: true));
        history.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Tenant_filter_financial_year_validation_and_direct_permissions_protect_links()
    {
        const long municipalityA = 9103;
        const long municipalityB = 9104;
        var user = IdpTestFixture.CreateUser("risk-tenant-user");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityA, user.Id));
        var a = await SeedAsync(context, municipalityA, user);
        var bMunicipality = new Municipality { Id = municipalityB, Code = "RISK-B", Name = "Risk B" };
        var bRisk = new StrategicRisk
        {
            MunicipalityId = municipalityB, Municipality = bMunicipality, RiskReference = "B-1", RiskTitle = "Other tenant risk",
            CreatedByUserId = user.Id
        };
        context.AddRange(bMunicipality, bRisk);
        await context.SaveChangesAsync();
        var allowed = Controller(context, user, municipalityA);

        var crossTenant = await allowed.LinkKpi(new LinkStrategicRiskRequest(bRisk.PublicId, a.Target.PublicId, false, "Cross tenant attempt"));
        crossTenant.Result.Should().BeOfType<NotFoundObjectResult>();

        var expiredYear = new FinancialYear
        {
            Code = "2024/25", Name = "2024/25", StartDate = new(2024, 7, 1), EndDate = new(2025, 6, 30), IsActive = true
        };
        var expiredMunicipalYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipalityA, FinancialYear = expiredYear, IsActive = true, EffectiveFrom = expiredYear.StartDate
        };
        context.AddRange(expiredYear, expiredMunicipalYear);
        await context.SaveChangesAsync();
        var expired = Data(await allowed.Create(Request("OLD", "Expired risk", expiredMunicipalYear.PublicId)));
        var invalidYear = await allowed.LinkKpi(new LinkStrategicRiskRequest(expired.PublicId, a.Target.PublicId, false, "Invalid year"));
        invalidYear.Result.Should().BeOfType<BadRequestObjectResult>();

        var denied = Controller(context, user, municipalityA, allow: false);
        var directCall = await denied.LinkKpi(new LinkStrategicRiskRequest(expired.PublicId, a.Target.PublicId, false, "Denied direct call"));
        directCall.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private static SaveStrategicRiskRequest Request(string reference, string title, Guid yearPublicId) =>
        new(reference, title, null, yearPublicId, yearPublicId, true, "Approved risk administration", null);

    private static StrategicRisksController Controller(
        ApplicationDbContext context,
        ApplicationUser user,
        long municipalityId,
        Mock<IWorkflowGovernanceService>? audit = null,
        bool allow = true)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(allow, allow ? "Allowed" : "Denied", allow ? [code] : [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(allow, allow, [], [], [], [], [], []));
        return new StrategicRisksController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object,
            (audit ?? new Mock<IWorkflowGovernanceService>()).Object, IdpTestFixture.Tenant(municipalityId, user.Id))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };
    }

    private static async Task<(MunicipalityFinancialYear Year, OpmsTarget Target)> SeedAsync(
        ApplicationDbContext context,
        long municipalityId,
        ApplicationUser user)
    {
        user.MunicipalityId = municipalityId;
        var municipality = new Municipality { Id = municipalityId, Code = $"RISK-{municipalityId}", Name = $"Risk Municipality {municipalityId}" };
        var financialYear = new FinancialYear
        {
            Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30), IsActive = true
        };
        var year = new MunicipalityFinancialYear
        {
            MunicipalityId = municipalityId, Municipality = municipality, FinancialYear = financialYear,
            IsActive = true, IsCurrent = true, EffectiveFrom = financialYear.StartDate
        };
        var layer = new SdbipLayer
        {
            MunicipalityId = municipalityId, Municipality = municipality, MunicipalityFinancialYear = year,
            Code = "TOP", Name = "Top Layer", IsActive = true
        };
        var target = new OpmsTarget
        {
            Id = $"risk-target-{municipalityId}", MunicipalityId = municipalityId, SdbipLayer = layer,
            IndicatorNumber = "KPI-1", TargetName = "Maintain reliable services", KpiDescription = "Service reliability",
            AnnualTargetDescription = "Meet annual target"
        };
        context.AddRange(user, municipality, financialYear, year, layer, target);
        await context.SaveChangesAsync();
        return (year, target);
    }

    private static T Data<T>(ActionResult<ApiResponse<T>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<ApiResponse<T>>(ok.Value).Data!;
    }
}

using System.Security.Claims;
using FluentAssertions;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FTCERP.Tests;

public sealed class TargetLibraryPagingTests
{
    [Fact]
    public async Task Opms_library_page_applies_all_filters_before_authoritative_count_and_stable_page()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("opms-library-reader");
        context.Users.Add(user);
        context.OpmsTargetTemplates.AddRange(Enumerable.Range(1, 31).Select(index => new OpmsTargetTemplate
        {
            TemplateCode = $"OP-{index:00}", TemplateName = $"Water template {index:00}", IndicatorNumber = $"KPI-{index:00}",
            TargetName = $"Water target {index:00}", KpiDescription = "Water delivery", TargetUnitType = "percentage",
            NationalKpa = "Water", MunicipalKpa = index % 2 == 0 ? "Services" : "Water", FunctionalArea = "Operations",
            KpiType = "Outcome", IsActive = index != 31, IsArchived = index == 31, Version = index % 3 + 1,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index)
        }));
        await context.SaveChangesAsync();
        var controller = OpmsController(context, user);

        var result = await controller.GetTemplatesPage(
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "Water", SortBy = "templateCode", SortDirection = "asc" },
            new TargetLibraryFilterRequest { Status = "active", PrimaryArea = "Water", FunctionalArea = "Operations", Classification = "Outcome", TargetUnitType = "percentage" });

        var page = Assert.IsType<ApiResponse<PagedResponse<OpmsTargetTemplateResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        page.TotalCount.Should().Be(30);
        page.TotalPages.Should().Be(3);
        page.Items.Should().HaveCount(10);
        page.Items[0].TemplateCode.Should().Be("OP-11");

        var facetsResult = await controller.GetTemplateFacets();
        var facets = Assert.IsType<ApiResponse<TargetLibraryFacetsResponse>>(
            Assert.IsType<OkObjectResult>(facetsResult.Result).Value).Data!;
        facets.PrimaryAreas.Should().Contain(["Services", "Water"]);
        facets.FunctionalAreas.Should().Contain("Operations");
        facets.Classifications.Should().Contain("Outcome");
        facets.TargetUnitTypes.Should().Contain("percentage");
        facets.Versions.Should().BeEquivalentTo([3, 2, 1]);
    }

    [Fact]
    public async Task Ipms_library_page_is_searchable_filterable_and_stably_paged()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("ipms-library-reader");
        context.Users.Add(user);
        context.IpmsTargetTemplates.AddRange(Enumerable.Range(1, 26).Select(index => new IpmsTargetTemplate
        {
            TemplateCode = $"IP-{index:00}", TemplateName = $"Manager template {index:00}", TargetName = $"Manager target {index:00}",
            KpiDescription = "Individual performance", PerformanceArea = "Leadership", EmployeeLevel = "Manager",
            FunctionalArea = "Corporate", TargetUnitType = "percentage", IsActive = true, Version = 2,
            CreatedDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index)
        }));
        await context.SaveChangesAsync();
        var controller = IpmsController(context, user);

        var result = await controller.GetTemplatesPage(
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "Manager", SortBy = "templateCode", SortDirection = "asc" },
            new TargetLibraryFilterRequest { Status = "active", PrimaryArea = "Leadership", FunctionalArea = "Corporate", Classification = "Manager", TargetUnitType = "percentage", Version = 2 });

        var page = Assert.IsType<ApiResponse<PagedResponse<IpmsTargetTemplateResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        page.TotalCount.Should().Be(26);
        page.TotalPages.Should().Be(3);
        page.Items.Should().HaveCount(10);
        page.Items[0].TemplateCode.Should().Be("IP-11");
    }

    [Fact]
    public async Task Target_library_pages_reject_unknown_sort_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("library-reader");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new PagedQueryRequest { SortBy = "raw-sql" };
        var filter = new TargetLibraryFilterRequest();
        (await OpmsController(context, user).GetTemplatesPage(request, filter)).Result.Should().BeOfType<BadRequestObjectResult>();
        (await IpmsController(context, user).GetTemplatesPage(request, filter)).Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Compatibility_target_library_reads_are_capped_at_one_hundred_rows()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("compatibility-library-reader");
        context.Users.Add(user);
        context.OpmsTargetTemplates.AddRange(Enumerable.Range(1, 101).Select(index => new OpmsTargetTemplate
        {
            TemplateCode = $"OP-C-{index:000}", TemplateName = $"OPMS {index:000}", IndicatorNumber = $"K-{index:000}",
            TargetName = $"Target {index:000}", KpiDescription = "Description", TargetUnitType = "percentage"
        }));
        context.IpmsTargetTemplates.AddRange(Enumerable.Range(1, 101).Select(index => new IpmsTargetTemplate
        {
            TemplateCode = $"IP-C-{index:000}", TemplateName = $"IPMS {index:000}", TargetName = $"Target {index:000}",
            KpiDescription = "Description", TargetUnitType = "percentage"
        }));
        await context.SaveChangesAsync();

        var opms = Assert.IsType<ApiResponse<OpmsTargetTemplateResponse[]>>(
            Assert.IsType<OkObjectResult>((await OpmsController(context, user).GetTemplates()).Result).Value).Data!;
        var ipms = Assert.IsType<ApiResponse<IpmsTargetTemplateResponse[]>>(
            Assert.IsType<OkObjectResult>((await IpmsController(context, user).GetTemplates()).Result).Value).Data!;
        opms.Should().HaveCount(100);
        ipms.Should().HaveCount(100);
    }

    private static OpmsTargetLibraryController OpmsController(ApplicationDbContext context, ApplicationUser user)
    {
        var access = Access(user);
        return new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, Mock.Of<IWorkflowGovernanceService>())
        {
            ControllerContext = Context(user.Id)
        };
    }

    private static IpmsTargetLibraryController IpmsController(ApplicationDbContext context, ApplicationUser user)
    {
        var access = Access(user);
        return new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, Mock.Of<IWorkflowGovernanceService>())
        {
            ControllerContext = Context(user.Id)
        };
    }

    private static Mock<IAccessControlService> Access(ApplicationUser user)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        return access;
    }

    private static ControllerContext Context(string userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"))
        }
    };
}

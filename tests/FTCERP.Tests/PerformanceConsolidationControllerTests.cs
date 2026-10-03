using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class PerformanceConsolidationControllerTests
{
    [Fact]
    public async Task Authorized_admin_can_create_and_update_tenant_policy_with_audit_and_rowversion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user) = await SeedAsync(context);
        var audit = new Mock<IWorkflowGovernanceService>();
        audit.Setup(service => service.WriteAuditTrailAsync(
                nameof(MunicipalityConsolidationPolicy), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<object?>(), user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = Controller(context, municipality.Id, user, true, audit.Object);
        var type = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "NON_CUMULATIVE");

        var createdResult = await controller.SavePolicy(type.PublicId,
            new(PerformanceCalculationType.Average, ConsolidationMissingValuePolicy.Ignore, true, false, true, "Approved council policy", null));
        var created = Assert.IsType<ApiResponse<ConsolidationPolicyDto>>(Assert.IsType<OkObjectResult>(createdResult.Result).Value).Data!;
        created.ConsolidationRule.Should().Be(PerformanceCalculationType.Average);
        created.RowVersion.Should().NotBeNullOrWhiteSpace();

        var updatedResult = await controller.SavePolicy(type.PublicId,
            new(PerformanceCalculationType.LatestValue, ConsolidationMissingValuePolicy.Block, false, true, true, "Annual policy approved", created.RowVersion));
        var updated = Assert.IsType<ApiResponse<ConsolidationPolicyDto>>(Assert.IsType<OkObjectResult>(updatedResult.Result).Value).Data!;
        updated.DeriveAnnualTarget.Should().BeTrue();
        updated.ConsolidationRule.Should().Be(PerformanceCalculationType.LatestValue);
        audit.Verify(service => service.WriteAuditTrailAsync(nameof(MunicipalityConsolidationPolicy), It.IsAny<string>(), "Create", null, It.IsAny<object>(), user.Id, It.IsAny<string?>()), Times.Once);
        audit.Verify(service => service.WriteAuditTrailAsync(nameof(MunicipalityConsolidationPolicy), It.IsAny<string>(), "Edit", It.IsAny<object>(), It.IsAny<object>(), user.Id, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Policy_catalogue_returns_all_types_and_tenant_policy_projection()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user) = await SeedAsync(context);
        var controller = Controller(context, municipality.Id, user, true, Mock.Of<IWorkflowGovernanceService>());
        var type = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "SUM");
        await controller.SavePolicy(type.PublicId, new(null, ConsolidationMissingValuePolicy.Block, false, false, true, "Use standard sum", null));

        var result = await controller.GetPolicies();
        var rows = Assert.IsType<ApiResponse<ConsolidationPolicyDto[]>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        rows.Should().HaveCount(9);
        rows.Single(item => item.CalculationTypeCode == "SUM").PolicyPublicId.Should().NotBeNull();
        rows.Single(item => item.CalculationTypeCode == "MANUAL").PolicyPublicId.Should().BeNull();
    }

    [Theory]
    [InlineData(PerformanceCalculationType.Cumulative)]
    [InlineData(PerformanceCalculationType.NonCumulative)]
    [InlineData(PerformanceCalculationType.Manual)]
    public async Task Unsafe_override_is_rejected(PerformanceCalculationType rule)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user) = await SeedAsync(context);
        var type = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "ZERO_BASED");

        var result = await Controller(context, municipality.Id, user, true, Mock.Of<IWorkflowGovernanceService>()).SavePolicy(
            type.PublicId, new(rule, ConsolidationMissingValuePolicy.Block, false, false, true, "Unsafe override", null));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        context.MunicipalityConsolidationPolicies.Should().BeEmpty();
    }

    [Fact]
    public async Task Direct_call_without_dynamic_permission_is_forbidden()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user) = await SeedAsync(context);
        var type = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "AVERAGE");

        var result = await Controller(context, municipality.Id, user, false, Mock.Of<IWorkflowGovernanceService>()).SavePolicy(
            type.PublicId, new(null, ConsolidationMissingValuePolicy.Block, false, false, true, "Denied", null));

        Assert.IsType<ForbidResult>(result.Result);
        context.MunicipalityConsolidationPolicies.Should().BeEmpty();
    }

    private static PerformanceConsolidationController Controller(
        ApplicationDbContext context, long municipalityId, ApplicationUser user, bool allowed, IWorkflowGovernanceService audit)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.UserId).Returns(user.Id);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(allowed, allowed ? "Allowed" : "Denied", [], [], []));
        return new PerformanceConsolidationController(context, tenant.Object, access.Object, IdpTestFixture.CreateUserManagerMock(user).Object, audit)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static async Task<(Municipality Municipality, ApplicationUser User)> SeedAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "POLICY", Name = "Policy Municipality" };
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();
        var user = IdpTestFixture.CreateUser("policy-admin");
        user.MunicipalityId = municipality.Id;
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (municipality, user);
    }
}

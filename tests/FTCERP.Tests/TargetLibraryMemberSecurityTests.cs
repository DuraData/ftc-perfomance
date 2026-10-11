using System.Security.Claims;
using FluentAssertions;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FTCERP.Tests;

public sealed class TargetLibraryMemberSecurityTests
{
    [Fact]
    public async Task Target_template_member_registry_is_idempotent_and_complete()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);

        var resources = context.SecurityResources.Where(item => item.Code.EndsWith("_TARGET_TEMPLATE")).ToArray();
        resources.Select(item => item.Code).Should().BeEquivalentTo(["OPMS_TARGET_TEMPLATE", "IPMS_TARGET_TEMPLATE"]);
        var members = context.SecurityMemberDefinitions.Where(item => item.ResourceCode.EndsWith("_TARGET_TEMPLATE")).ToArray();
        members.Should().HaveCount(9).And.OnlyContain(item => item.IsSensitive && !item.IsSystemManaged);
        context.Permissions.Count(item => item.ResourceCode != null && item.ResourceCode.EndsWith("_TARGET_TEMPLATE")
            && item.Kind == SecurityPermissionKind.Member).Should().Be(18);
    }

    [Fact]
    public async Task Opms_template_members_mask_and_change_effectively_without_restart()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("opms-template-member-reader");
        var template = OpmsTemplate();
        context.AddRange(user, template);
        await context.SaveChangesAsync();
        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var controller = OpmsController(context, user, grants);

        var denied = Data(await controller.GetTemplate(template.PublicId));
        denied.InternalReference.Should().BeNull();
        denied.FmsLink.Should().BeNull();
        denied.DefaultQuarterlyTargetsJson.Should().BeNull();
        denied.DefaultBudgetInformation.Should().BeNull();
        denied.DefaultPoeRequirements.Should().BeNull();
        denied.TemplateName.Should().Be(template.TemplateName);

        grants.Add("OPMS_TARGET_TEMPLATE.InternalReference.READ");
        var allowed = Data(await controller.GetTemplate(template.PublicId));
        allowed.InternalReference.Should().Be("INTERNAL-SECRET");
        allowed.FmsLink.Should().BeNull();

        grants.Remove("OPMS_TARGET_TEMPLATE.InternalReference.READ");
        Data(await controller.GetTemplate(template.PublicId)).InternalReference.Should().BeNull();
    }

    [Fact]
    public async Task Opms_template_protected_write_and_duplicate_fail_closed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("opms-template-member-writer");
        var template = OpmsTemplate();
        context.AddRange(user, template);
        await context.SaveChangesAsync();
        var grants = ReadGrants("OPMS_TARGET_TEMPLATE", "InternalReference", "FmsLink", "DefaultQuarterlyTargets", "DefaultBudgetInformation", "DefaultPoeRequirements");
        var controller = OpmsController(context, user, grants);

        (await controller.CreateTemplate(OpmsRequest("OP-DENIED", internalReference: "WRITE-SECRET"))).Result
            .Should().BeOfType<ForbidResult>();
        (await controller.DuplicateTemplate(template.PublicId)).Result.Should().BeOfType<ForbidResult>();

        grants.Add("OPMS_TARGET_TEMPLATE.InternalReference.UPDATE");
        var created = Data(await controller.CreateTemplate(OpmsRequest("OP-ALLOWED", internalReference: "WRITE-SECRET")));
        created.InternalReference.Should().Be("WRITE-SECRET");
    }

    [Fact]
    public async Task Ipms_template_members_mask_and_protected_writes_fail_closed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("ipms-template-member-reader");
        var template = IpmsTemplate();
        context.AddRange(user, template);
        await context.SaveChangesAsync();
        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var controller = IpmsController(context, user, grants);

        var denied = Data(await controller.GetTemplate(template.PublicId));
        denied.JobGrade.Should().BeNull();
        denied.DefaultScoreScale.Should().BeNull();
        denied.DefaultPoeRequirements.Should().BeNull();
        denied.DefaultTaskTemplatesJson.Should().BeNull();

        (await controller.CreateTemplate(IpmsRequest("IP-DENIED", jobGrade: "GRADE-SECRET"))).Result
            .Should().BeOfType<ForbidResult>();
        grants.Add("IPMS_TARGET_TEMPLATE.JobGrade.READ");
        grants.Add("IPMS_TARGET_TEMPLATE.JobGrade.UPDATE");
        var created = Data(await controller.CreateTemplate(IpmsRequest("IP-ALLOWED", jobGrade: "GRADE-SECRET")));
        created.JobGrade.Should().Be("GRADE-SECRET");
        grants.Remove("IPMS_TARGET_TEMPLATE.JobGrade.READ");
        Data(await controller.GetTemplate(created.PublicId)).JobGrade.Should().BeNull();
        (await controller.DuplicateTemplate(template.PublicId)).Result.Should().BeOfType<ForbidResult>();
    }

    private static HashSet<string> ReadGrants(string resource, params string[] members) =>
        members.Select(member => $"{resource}.{member}.READ").ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static OpmsTargetLibraryController OpmsController(ApplicationDbContext context, ApplicationUser user, HashSet<string> grants)
    {
        var access = Access(user, grants, "OPMS_TARGET_TEMPLATE");
        return new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, Mock.Of<IWorkflowGovernanceService>())
        { ControllerContext = ControllerContext(user.Id) };
    }

    private static IpmsTargetLibraryController IpmsController(ApplicationDbContext context, ApplicationUser user, HashSet<string> grants)
    {
        var access = Access(user, grants, "IPMS_TARGET_TEMPLATE");
        return new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, Mock.Of<IWorkflowGovernanceService>())
        { ControllerContext = ControllerContext(user.Id) };
    }

    private static Mock<IAccessControlService> Access(ApplicationUser user, HashSet<string> grants, string protectedResource)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                code.StartsWith(protectedResource + ".", StringComparison.OrdinalIgnoreCase) && !grants.Contains(code)
                    ? new AccessDecisionResult(false, "Denied", [], [], [])
                    : new AccessDecisionResult(true, "Allowed", [], [], []));
        return access;
    }

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"))
        }
    };

    private static OpmsTargetTemplateResponse Data(ActionResult<ApiResponse<OpmsTargetTemplateResponse>> result) =>
        result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<OpmsTargetTemplateResponse>>().Subject.Data!;

    private static IpmsTargetTemplateResponse Data(ActionResult<ApiResponse<IpmsTargetTemplateResponse>> result) =>
        result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<IpmsTargetTemplateResponse>>().Subject.Data!;

    private static OpmsTargetTemplate OpmsTemplate() => new()
    {
        TemplateCode = "OP-SECRET", TemplateName = "OPMS protected template", IndicatorNumber = "KPI-1",
        TargetName = "Target", KpiDescription = "Description", TargetUnitType = "percentage",
        InternalReference = "INTERNAL-SECRET", FmsLink = "FMS-SECRET",
        DefaultQuarterlyTargetsJson = "[{\"quarter\":\"Q1\",\"target\":10}]",
        DefaultBudgetInformation = "BUDGET-SECRET", DefaultPoeRequirements = "POE-SECRET", IsActive = true
    };

    private static IpmsTargetTemplate IpmsTemplate() => new()
    {
        TemplateCode = "IP-SECRET", TemplateName = "IPMS protected template", TargetName = "Target",
        KpiDescription = "Description", TargetUnitType = "percentage", JobGrade = "GRADE-SECRET",
        DefaultScoreScale = "SCORE-SECRET", DefaultPoeRequirements = "POE-SECRET",
        DefaultTaskTemplatesJson = "[\"TASK-SECRET\"]", IsActive = true
    };

    private static SaveOpmsTargetTemplateRequest OpmsRequest(string code, string? internalReference = null) => new(
        code, code, "KPI-1", "Target", "Description", 0, 100, null, "percentage", "%", null, null,
        null, null, null, null, null, null, null, null, 100, null, null, null, null, null,
        internalReference, null, null, null, null, true);

    private static SaveIpmsTargetTemplateRequest IpmsRequest(string code, string? jobGrade = null) => new(
        code, code, "Target", "Description", null, null, jobGrade, "percentage", "%", 100, null, 100,
        null, null, null, null, false, null, true);
}

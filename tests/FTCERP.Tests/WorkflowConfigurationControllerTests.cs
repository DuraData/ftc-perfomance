using FTCERP.Host.Application.Services;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class WorkflowConfigurationControllerTests
{
    [Fact]
    public async Task RetireDefinition_UsesRowVersionAndWritesReasonedAuditWithoutDeletingDefinition()
    {
        await using var context = IdpTestFixture.CreateContext();
        var definition = Definition(1, 101, true, "Original");
        context.AddRange(definition.MunicipalityFinancialYear, definition);
        await context.SaveChangesAsync();
        var controller = Controller(context);

        var result = await controller.RetireDefinition(definition.PublicId, new RetireWorkflowDefinitionRequest("Annual governance retirement", DateTime.UtcNow, Convert.ToBase64String(definition.RowVersion)));

        result.Result.Should().BeOfType<OkObjectResult>();
        definition.IsActive.Should().BeFalse();
        definition.EffectiveTo.Should().NotBeNull();
        context.WorkflowDefinitions.IgnoreQueryFilters().Should().ContainSingle(item => item.PublicId == definition.PublicId);
        var audit = await context.AuditTrails.IgnoreQueryFilters().SingleAsync(item => item.EntityId == definition.PublicId.ToString());
        audit.Action.Should().Be("Retire");
        audit.NewValue.Should().Contain("Annual governance retirement");
    }

    [Fact]
    public async Task CompareDefinitions_ReturnsOnlySameLineageStageDifferences()
    {
        await using var context = IdpTestFixture.CreateContext();
        var prior = Definition(1, 201, false, "Submit");
        var current = Definition(2, 202, true, "Capture and submit");
        current.MunicipalityFinancialYear = prior.MunicipalityFinancialYear;
        current.MunicipalityFinancialYearId = prior.MunicipalityFinancialYearId;
        context.AddRange(prior.MunicipalityFinancialYear, prior, current);
        await context.SaveChangesAsync();
        var controller = Controller(context);

        var result = await controller.CompareDefinitions(prior.PublicId, current.PublicId);

        var response = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<WorkflowDefinitionComparisonDto>>().Subject;
        response.Data!.From.Version.Should().Be(1);
        response.Data.To.Version.Should().Be(2);
        response.Data.StageDifferences.Single().ChangedFields.Should().Contain("Name");
    }

    private static WorkflowConfigurationController Controller(ApplicationDbContext context)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(7);
        var user = IdpTestFixture.CreateUser("workflow-admin");
        return new WorkflowConfigurationController(
            context,
            tenant.Object,
            Mock.Of<IAccessControlService>(),
            Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(),
            new WorkflowGovernanceService(context),
            IdpTestFixture.CreateUserManagerMock(user).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static WorkflowDefinition Definition(int version, long id, bool active, string stageName)
    {
        var year = new MunicipalityFinancialYear { Id = 500, PublicId = Guid.NewGuid(), MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        var definition = new WorkflowDefinition { Id = id, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "DEFAULT", Name = "Default", Version = version, IsActive = active, EffectiveFrom = DateTime.UtcNow.AddDays(-10 + version) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = id * 10, MunicipalityId = 7, Code = "SUBMIT", Name = stageName, Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT", IsTerminal = true });
        return definition;
    }
}

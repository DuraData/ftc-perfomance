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

    [Fact]
    public async Task RfiPage_AppliesScopeAndFiltersBeforeCount_AndRetiresLegacyArray()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "RFI-PAGE", Name = "RFI Page Municipality" };
        var user = IdpTestFixture.CreateUser("rfi-reader");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var financialYear = new FinancialYear { Code = "2032/33", Name = "2032/33", StartDate = new(2032, 7, 1), EndDate = new(2033, 6, 30) };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear, EffectiveFrom = financialYear.StartDate };
        var target = new OpmsTarget { MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "RFI-KPI", TargetName = "RFI target", KpiDescription = "RFI target", AnnualTargetDescription = "Target", PerformanceObjective = "Objective" };
        context.AddRange(year, target);
        await context.SaveChangesAsync();
        var submission = new OpmsSubmission { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, OpmsTarget = target, Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "reviewed", SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow };
        var workflow = new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "RFI", Name = "RFI workflow", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        context.AddRange(submission, workflow);
        await context.SaveChangesAsync();
        var instance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id };
        var otherInstance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = "other-submission" };
        context.AddRange(instance, otherInstance);
        await context.SaveChangesAsync();
        var raisedAt = new DateTime(2032, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 31; index++)
        {
            context.PerformanceRfis.Add(new PerformanceRfi
            {
                MunicipalityId = municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                Question = $"match-{index:00}",
                RaisedByUserId = user.Id,
                RaisedAt = raisedAt.AddMinutes(index),
                ResponseDueAt = raisedAt.AddDays(5),
                Response = index % 2 == 0 ? null : "answered",
                RespondedByUserId = index % 2 == 0 ? null : user.Id,
                RespondedAt = index % 2 == 0 ? null : raisedAt.AddMinutes(index + 1)
            });
        }
        context.PerformanceRfis.Add(new PerformanceRfi { MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = otherInstance.Id, SubmissionWorkflowInstance = otherInstance, Question = "match-outside", RaisedByUserId = user.Id, RaisedAt = raisedAt, ResponseDueAt = raisedAt.AddDays(5) });
        await context.SaveChangesAsync();

        var controller = Controller(context, municipality.Id, user);
        var action = await controller.GetRfisPage(SubmissionKind.Opms, submission.Id,
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "raisedAt", SortDirection = "asc" }, "open");
        var page = action.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<PerformanceRfiDto>>>().Subject.Data!;

        page.TotalCount.Should().Be(16);
        page.Items.Should().HaveCount(6);
        page.Items.First().Question.Should().Be("match-20");
        page.Items.Should().NotContain(item => item.Question == "match-outside");
        (await controller.GetRfisPage(SubmissionKind.Opms, submission.Id, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetRfis(SubmissionKind.Opms, submission.Id)).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task WorkflowEvidencePages_ApplySubmissionScopeBeforeCount_AndRetireLegacyArrays()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WF-EVIDENCE", Name = "Workflow Evidence Municipality" };
        var user = IdpTestFixture.CreateUser("workflow-reader");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var financialYear = new FinancialYear { Code = "2033/34", Name = "2033/34", StartDate = new(2033, 7, 1), EndDate = new(2034, 6, 30) };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear, EffectiveFrom = financialYear.StartDate };
        var target = new OpmsTarget { MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "WF-KPI", TargetName = "Workflow target", KpiDescription = "Workflow target", AnnualTargetDescription = "Target", PerformanceObjective = "Objective" };
        var scheme = new RatingScheme { MunicipalityId = municipality.Id, Code = "FIVE", Name = "Five point" };
        var value = new RatingSchemeValue { MunicipalityId = municipality.Id, RatingScheme = scheme, Value = 4, Label = "Exceeded", SortOrder = 4 };
        context.AddRange(year, target, scheme, value);
        await context.SaveChangesAsync();
        var submission = new OpmsSubmission { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, OpmsTarget = target, Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "reviewed", SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow };
        var workflow = new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "EVIDENCE", Name = "Evidence workflow", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var stage = new WorkflowStageDefinition { MunicipalityId = municipality.Id, WorkflowDefinition = workflow, Code = "REVIEW", Name = "Review", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.REVIEW", RequiredPermissionCode = "OPMS_SUBMISSION.READ", IsTerminal = true, RequiresRating = true, RatingScheme = scheme };
        context.AddRange(submission, workflow, stage);
        await context.SaveChangesAsync();
        var instance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id };
        var otherInstance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = "other-submission" };
        context.AddRange(instance, otherInstance);
        await context.SaveChangesAsync();
        var occurredAt = new DateTime(2033, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var actions = new List<SubmissionWorkflowAction>();
        for (var index = 0; index < 21; index++)
        {
            var action = new SubmissionWorkflowAction
            {
                MunicipalityId = municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                Sequence = index + 1,
                ActionCode = $"MATCH-ACTION-{index:00}",
                Outcome = WorkflowActionOutcome.Approve,
                ActorUserId = user.Id,
                Comment = $"match-comment-{index:00}",
                OccurredAt = occurredAt.AddMinutes(index)
            };
            actions.Add(action);
            context.SubmissionWorkflowActions.Add(action);
        }
        var outsideAction = new SubmissionWorkflowAction { MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = otherInstance.Id, SubmissionWorkflowInstance = otherInstance, Sequence = 1, ActionCode = "MATCH-OUTSIDE", Outcome = WorkflowActionOutcome.Approve, ActorUserId = user.Id, OccurredAt = occurredAt };
        context.SubmissionWorkflowActions.Add(outsideAction);
        await context.SaveChangesAsync();
        for (var index = 0; index < actions.Count; index++)
        {
            context.SubmissionStageRatings.Add(new SubmissionStageRating
            {
                MunicipalityId = municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                SubmissionWorkflowActionId = actions[index].Id,
                SubmissionWorkflowAction = actions[index],
                WorkflowStageDefinitionId = stage.Id,
                WorkflowStageDefinition = stage,
                RatingSchemeId = scheme.Id,
                RatingScheme = scheme,
                RatingSchemeValueId = value.Id,
                RatingSchemeValue = value,
                Value = value.Value,
                LabelSnapshot = $"match-label-{index:00}",
                RatedByUserId = user.Id,
                RatedAt = occurredAt.AddMinutes(index)
            });
        }
        context.SubmissionStageRatings.Add(new SubmissionStageRating { MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = otherInstance.Id, SubmissionWorkflowInstance = otherInstance, SubmissionWorkflowActionId = outsideAction.Id, SubmissionWorkflowAction = outsideAction, WorkflowStageDefinitionId = stage.Id, WorkflowStageDefinition = stage, RatingSchemeId = scheme.Id, RatingScheme = scheme, RatingSchemeValueId = value.Id, RatingSchemeValue = value, Value = value.Value, LabelSnapshot = "match-outside", RatedByUserId = user.Id, RatedAt = occurredAt });
        await context.SaveChangesAsync();

        var controller = Controller(context, municipality.Id, user);
        var actionResult = await controller.HistoryPage(SubmissionKind.Opms, submission.Id,
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "occurredAt", SortDirection = "asc" });
        var actionPage = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        actionPage.TotalCount.Should().Be(21);
        actionPage.Items.Should().HaveCount(10);
        actionPage.Items.First().ActionCode.Should().Be("MATCH-ACTION-10");
        actionPage.Items.Should().NotContain(item => item.ActionCode == "MATCH-OUTSIDE");

        var ratingResult = await controller.RatingHistoryPage(SubmissionKind.Opms, submission.Id,
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "ratedAt", SortDirection = "desc" });
        var ratingPage = ratingResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        ratingPage.TotalCount.Should().Be(21);
        ratingPage.Items.Should().HaveCount(10);
        ratingPage.Items.First().Label.Should().Be("match-label-10");
        ratingPage.Items.Should().NotContain(item => item.Label == "match-outside");

        (await controller.HistoryPage(SubmissionKind.Opms, submission.Id, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.RatingHistoryPage(SubmissionKind.Opms, submission.Id, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.History(SubmissionKind.Opms, submission.Id)).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        (await controller.RatingHistory(SubmissionKind.Opms, submission.Id)).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static WorkflowConfigurationController Controller(ApplicationDbContext context, long municipalityId = 7, ApplicationUser? suppliedUser = null)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        var user = suppliedUser ?? IdpTestFixture.CreateUser("workflow-admin");
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        return new WorkflowConfigurationController(
            context,
            tenant.Object,
            access.Object,
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

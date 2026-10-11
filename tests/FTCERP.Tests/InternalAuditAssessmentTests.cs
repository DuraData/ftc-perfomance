using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class InternalAuditAssessmentTests
{
    [Fact]
    public async Task Model_selection_is_versioned_audited_and_concurrency_protected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed, allowed: true);

        var firstResult = await controller.Configure(new(seed.Year.PublicId, InternalAuditAssessmentModel.Detailed, DateTime.UtcNow.AddMinutes(-5), "Initial approved audit methodology", null));
        var first = Data<InternalAuditConfigurationDto>(firstResult.Result!);
        var staleResult = await controller.Configure(new(seed.Year.PublicId, InternalAuditAssessmentModel.SatisfactoryNotSatisfactory, DateTime.UtcNow.AddMinutes(-2), "Council approved simplified methodology", null));
        staleResult.Result.Should().BeOfType<ConflictObjectResult>();

        var secondResult = await controller.Configure(new(seed.Year.PublicId, InternalAuditAssessmentModel.SatisfactoryNotSatisfactory, DateTime.UtcNow.AddMinutes(-2), "Council approved simplified methodology", first.RowVersion));
        var second = Data<InternalAuditConfigurationDto>(secondResult.Result!);

        second.Version.Should().Be(2);
        second.IsCurrent.Should().BeTrue();
        var versions = await context.InternalAuditAssessmentConfigurations.OrderBy(item => item.Version).ToArrayAsync();
        versions.Should().HaveCount(2);
        versions[0].IsCurrent.Should().BeFalse();
        versions[0].EffectiveTo.Should().Be(second.EffectiveFrom);
        (await context.AuditTrails.CountAsync(item => item.EntityName == nameof(InternalAuditAssessmentConfiguration))).Should().Be(2);
    }

    [Fact]
    public async Task Not_satisfactory_creates_scoped_rfi_and_reassessment_appends_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.SatisfactoryNotSatisfactory);
        var controller = Controller(context, seed, allowed: true);

        var adverseResult = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.NotSatisfactory, "Invoices do not reconcile; upload the signed reconciliation.", null, null, null, null, DateTime.UtcNow.AddDays(3), null));
        var adverse = Data<InternalAuditAssessmentDto>(adverseResult.Result!);
        adverse.RfiPublicId.Should().NotBeNull();
        (await context.PerformanceRfis.SingleAsync()).ResponseDueAt.Should().BeAfter(DateTime.UtcNow);

        var reassessmentResult = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Satisfactory, "The signed reconciliation now supports the reported actual.", null, null, null, null, null, adverse.PublicId));
        var reassessment = Data<InternalAuditAssessmentDto>(reassessmentResult.Result!);

        reassessment.PreviousAssessmentPublicId.Should().Be(adverse.PublicId);
        (await context.InternalAuditAssessments.OrderBy(item => item.AssessedAt).Select(item => item.Outcome).ToArrayAsync())
            .Should().Equal(InternalAuditAssessmentOutcome.NotSatisfactory, InternalAuditAssessmentOutcome.Satisfactory);
        (await context.PerformanceRfis.CountAsync()).Should().Be(1);
        (await context.SubmissionWorkflowActions.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Simplified_model_enforces_exact_fields_and_due_date_rule()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.SatisfactoryNotSatisfactory);
        var controller = Controller(context, seed, allowed: true);

        var detailedFields = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Satisfactory, "Supported.", "Extra comment", null, null, null, null, null));
        detailedFields.Result.Should().BeOfType<BadRequestObjectResult>();
        var missingDueDate = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.NotSatisfactory, "Support is missing.", null, null, null, null, null, null));
        missingDueDate.Result.Should().BeOfType<BadRequestObjectResult>();
        context.InternalAuditAssessments.Should().BeEmpty();
        context.PerformanceRfis.Should().BeEmpty();
    }

    [Fact]
    public async Task Direct_assessment_call_is_denied_without_dynamic_permission()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.Detailed);

        var result = await Controller(context, seed, allowed: false).Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Achieved, "Evidence supports the reported performance.", null, null, null, 4, null, null));

        result.Result.Should().BeOfType<ForbidResult>();
        context.InternalAuditAssessments.Should().BeEmpty();
    }

    [Fact]
    public async Task Detailed_model_uses_the_configured_audit_stage_rating_scheme()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.Detailed);
        var workflow = await context.WorkflowDefinitions.SingleAsync();
        var scheme = new RatingScheme { MunicipalityId = seed.Municipality.Id, Code = "IA", Name = "IA rating" };
        scheme.Values.Add(new RatingSchemeValue { MunicipalityId = seed.Municipality.Id, Value = 4, Label = "Supported", SortOrder = 1 });
        var stage = new WorkflowStageDefinition
        {
            MunicipalityId = seed.Municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow,
            Code = "AUDIT", Name = "Internal Audit", Sequence = 1, RequiredActionCode = "OPMS_WORKFLOW.INTERNAL_AUDIT",
            RequiredPermissionCode = "OPMS_WORKFLOW.INTERNAL_AUDIT", IsTerminal = true, RequiresRating = true, RatingScheme = scheme
        };
        context.AddRange(scheme, stage);
        await context.SaveChangesAsync();
        var controller = Controller(context, seed, allowed: true);

        var invalid = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Achieved, "The evidence supports achievement.", null, null, null, 3, null, null));
        invalid.Result.Should().BeOfType<BadRequestObjectResult>();
        var valid = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Achieved, "The evidence supports achievement.", null, null, null, 4, null, null));

        valid.Result.Should().BeOfType<OkObjectResult>();
        var rating = await context.SubmissionStageRatings.SingleAsync();
        rating.Value.Should().Be(4);
        rating.LabelSnapshot.Should().Be("Supported");
    }

    [Fact]
    public async Task Assessment_rows_cannot_be_changed_or_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.Detailed);
        await Controller(context, seed, allowed: true).Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Achieved, "Evidence supports the reported performance.", null, null, null, 4, null, null));
        var assessment = await context.InternalAuditAssessments.SingleAsync();
        assessment.DetailedObservation = "Changed";

        var action = () => context.SaveChangesAsync();
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task ConfigurationPage_FiltersBeforeCount_PagesDeterministically_AndRetiresLegacyArray()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var createdAt = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            context.InternalAuditAssessmentConfigurations.Add(new InternalAuditAssessmentConfiguration
            {
                MunicipalityId = seed.Municipality.Id,
                MunicipalityFinancialYearId = seed.Year.Id,
                MunicipalityFinancialYear = seed.Year,
                Model = index % 2 == 0 ? InternalAuditAssessmentModel.Detailed : InternalAuditAssessmentModel.SatisfactoryNotSatisfactory,
                Version = index + 1,
                IsCurrent = index == 10,
                EffectiveFrom = createdAt.AddDays(index),
                EffectiveTo = index == 10 ? null : createdAt.AddDays(index + 1),
                Reason = $"match governance reason {index:00}",
                CreatedByUserId = seed.User.Id,
                CreatedByUser = seed.User,
                CreatedAt = createdAt.AddMinutes(index)
            });
        }
        context.InternalAuditAssessmentConfigurations.Add(new InternalAuditAssessmentConfiguration { MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYearId = seed.Year.Id, MunicipalityFinancialYear = seed.Year, Model = InternalAuditAssessmentModel.Detailed, Version = 12, IsCurrent = false, EffectiveFrom = createdAt.AddDays(12), EffectiveTo = createdAt.AddDays(13), Reason = "outside governance reason", CreatedByUserId = seed.User.Id, CreatedByUser = seed.User, CreatedAt = createdAt.AddDays(12) });
        await context.SaveChangesAsync();
        var controller = Controller(context, seed, allowed: true);

        var result = await controller.ConfigurationsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "version", SortDirection = "asc" });
        var page = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<InternalAuditConfigurationDto>>>().Subject.Data!;

        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.Version).Should().Equal(4, 5, 6);
        (await controller.ConfigurationsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.Configurations().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task Assessment_history_filters_before_count_pages_stably_and_bootstrap_returns_only_latest()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.SatisfactoryNotSatisfactory);
        var instance = await context.SubmissionWorkflowInstances.SingleAsync();
        var configuration = await context.InternalAuditAssessmentConfigurations.SingleAsync();
        InternalAuditAssessment? previous = null;
        var assessedAt = new DateTime(2035, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            var assessment = new InternalAuditAssessment
            {
                MunicipalityId = seed.Municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                ConfigurationId = configuration.Id,
                Configuration = configuration,
                PreviousAssessmentId = previous?.Id,
                PreviousAssessment = previous,
                Outcome = InternalAuditAssessmentOutcome.Satisfactory,
                DetailedObservation = $"match assessment {index:00}",
                AssessedByUserId = seed.User.Id,
                AssessedByUser = seed.User,
                AssessedAt = assessedAt.AddMinutes(index),
                CorrelationId = $"assessment-{index:00}"
            };
            context.InternalAuditAssessments.Add(assessment);
            await context.SaveChangesAsync();
            previous = assessment;
        }
        context.InternalAuditAssessments.Add(new InternalAuditAssessment
        {
            MunicipalityId = seed.Municipality.Id, SubmissionWorkflowInstanceId = instance.Id, SubmissionWorkflowInstance = instance,
            ConfigurationId = configuration.Id, Configuration = configuration, PreviousAssessmentId = previous!.Id, PreviousAssessment = previous,
            Outcome = InternalAuditAssessmentOutcome.NotSatisfactory, DetailedObservation = "outside history search",
            AssessedByUserId = seed.User.Id, AssessedByUser = seed.User, AssessedAt = assessedAt.AddMinutes(20), CorrelationId = "outside"
        });
        await context.SaveChangesAsync();
        var controller = Controller(context, seed, allowed: true);

        var result = await controller.AssessmentsPage(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "assessedAt", SortDirection = "asc" });
        var page = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<InternalAuditAssessmentDto>>>().Subject.Data!;
        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.DetailedObservation).Should().Equal("match assessment 03", "match assessment 04", "match assessment 05");
        (await controller.AssessmentsPage(SubmissionKind.Opms, seed.Submission.PublicId.ToString(), new PagedQueryRequest { SortBy = "unsafe" }))
            .Result.Should().BeOfType<BadRequestObjectResult>();

        var bootstrap = Data<InternalAuditSubmissionDto>((await controller.Submission(SubmissionKind.Opms, seed.Submission.PublicId.ToString())).Result!);
        bootstrap.LatestAssessment.Should().NotBeNull();
        bootstrap.LatestAssessment!.DetailedObservation.Should().Be("outside history search");
    }

    [Fact]
    public async Task Assessment_members_are_masked_non_inferable_and_write_protected_by_dynamic_permissions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, InternalAuditAssessmentModel.Detailed);
        var instance = await context.SubmissionWorkflowInstances.SingleAsync();
        var configuration = await context.InternalAuditAssessmentConfigurations.SingleAsync();
        var rfi = new PerformanceRfi
        {
            MunicipalityId = seed.Municipality.Id, SubmissionWorkflowInstanceId = instance.Id, SubmissionWorkflowInstance = instance,
            Question = "secret RFI", RaisedByUserId = seed.User.Id, RaisedAt = DateTime.UtcNow, ResponseDueAt = DateTime.UtcNow.AddDays(3)
        };
        context.InternalAuditAssessments.Add(new InternalAuditAssessment
        {
            MunicipalityId = seed.Municipality.Id, SubmissionWorkflowInstanceId = instance.Id, SubmissionWorkflowInstance = instance,
            ConfigurationId = configuration.Id, Configuration = configuration, Outcome = InternalAuditAssessmentOutcome.NotAchieved,
            DetailedObservation = "secret observation", Comment = "secret comment", Findings = "secret finding",
            Recommendation = "secret recommendation", Score = 2, AssessedByUserId = seed.User.Id, AssessedByUser = seed.User,
            AssessedAt = DateTime.UtcNow, CorrelationId = "secret-correlation", PerformanceRfi = rfi
        });
        await context.SaveChangesAsync();
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_SUBMISSION.READ" };
        var controller = Controller(context, seed, permissions.Contains);

        var masked = Data<PagedResponse<InternalAuditAssessmentDto>>((await controller.AssessmentsPage(
            SubmissionKind.Opms, seed.Submission.PublicId.ToString(), new PagedQueryRequest())).Result!);
        masked.Items.Should().ContainSingle();
        masked.Items[0].DetailedObservation.Should().BeNull();
        masked.Items[0].Comment.Should().BeNull();
        masked.Items[0].Findings.Should().BeNull();
        masked.Items[0].Recommendation.Should().BeNull();
        masked.Items[0].Score.Should().BeNull();
        masked.Items[0].AssessedByUserPublicId.Should().BeNull();
        masked.Items[0].AssessedByName.Should().BeNull();
        masked.Items[0].RfiPublicId.Should().BeNull();
        masked.Items[0].RfiResponseDueAt.Should().BeNull();

        var hiddenSearch = Data<PagedResponse<InternalAuditAssessmentDto>>((await controller.AssessmentsPage(
            SubmissionKind.Opms, seed.Submission.PublicId.ToString(), new PagedQueryRequest { Search = "secret" })).Result!);
        hiddenSearch.TotalCount.Should().Be(0);
        (await controller.AssessmentsPage(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new PagedQueryRequest { SortBy = "assessedBy" })).Result.Should().BeOfType<ForbidResult>();

        permissions.UnionWith(new[]
        {
            "OPMS_SUBMISSION.InternalAuditObservation.READ", "OPMS_SUBMISSION.InternalAuditComment.READ",
            "OPMS_SUBMISSION.InternalAuditFindings.READ", "OPMS_SUBMISSION.InternalAuditRecommendation.READ",
            "OPMS_SUBMISSION.InternalAuditScore.READ", "OPMS_SUBMISSION.InternalAuditAssessedBy.READ",
            "OPMS_SUBMISSION.InternalAuditRfi.READ"
        });
        var visible = Data<PagedResponse<InternalAuditAssessmentDto>>((await controller.AssessmentsPage(
            SubmissionKind.Opms, seed.Submission.PublicId.ToString(), new PagedQueryRequest { Search = "secret" })).Result!);
        visible.TotalCount.Should().Be(1);
        visible.Items[0].DetailedObservation.Should().Be("secret observation");
        visible.Items[0].Findings.Should().Be("secret finding");
        visible.Items[0].AssessedByUserPublicId.Should().Be(seed.User.PublicId);
        visible.Items[0].RfiPublicId.Should().Be(rfi.PublicId);
        visible.Items[0].RfiResponseDueAt.Should().Be(rfi.ResponseDueAt);

        var rawKeySearch = Data<PagedResponse<InternalAuditAssessmentDto>>((await controller.AssessmentsPage(
            SubmissionKind.Opms, seed.Submission.PublicId.ToString(), new PagedQueryRequest { Search = seed.User.Id })).Result!);
        rawKeySearch.TotalCount.Should().Be(0);
        var publicIdSearch = Data<PagedResponse<InternalAuditAssessmentDto>>((await controller.AssessmentsPage(
            SubmissionKind.Opms, seed.Submission.PublicId.ToString(), new PagedQueryRequest { Search = seed.User.PublicId.ToString() })).Result!);
        publicIdSearch.TotalCount.Should().Be(1);
        typeof(InternalAuditAssessmentDto).GetProperty("AssessedByUserId").Should().BeNull();

        permissions.Add("OPMS_WORKFLOW.INTERNAL_AUDIT");
        var deniedWrite = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Achieved, "new protected observation", null, null, null, null, null, null));
        deniedWrite.Result.Should().BeOfType<ForbidResult>();
        permissions.Add("OPMS_SUBMISSION.InternalAuditObservation.UPDATE");
        var allowedWrite = await controller.Assess(SubmissionKind.Opms, seed.Submission.PublicId.ToString(),
            new(InternalAuditAssessmentOutcome.Achieved, "new protected observation", null, null, null, null, null,
                (await context.InternalAuditAssessments.OrderByDescending(item => item.AssessedAt).FirstAsync()).PublicId));
        allowedWrite.Result.Should().BeOfType<OkObjectResult>();
    }

    private static T Data<T>(IActionResult result) where T : class =>
        Assert.IsType<ApiResponse<T>>(Assert.IsType<OkObjectResult>(result).Value).Data!;

    private static InternalAuditAssessmentsController Controller(ApplicationDbContext context, Seed seed, bool allowed)
        => Controller(context, seed, _ => allowed);

    private static InternalAuditAssessmentsController Controller(ApplicationDbContext context, Seed seed, Func<string, bool> allowed)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(seed.Municipality.Id);
        tenant.SetupGet(item => item.UserId).Returns(seed.User.Id);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(seed.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string permission, AccessScopeContext? _) =>
            {
                var granted = allowed(permission);
                return new AccessDecisionResult(granted, granted ? "Allowed" : "Denied", [], [], []);
            });
        return new(context, tenant.Object, access.Object, new WorkflowGovernanceService(context), IdpTestFixture.CreateUserManagerMock(seed.User).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(seed.User.Id), TraceIdentifier = "ia-test" } }
        };
    }

    private static async Task<Seed> SeedAsync(ApplicationDbContext context, InternalAuditAssessmentModel? model = null)
    {
        var municipality = new Municipality { Code = $"IA-{Guid.NewGuid():N}"[..20], Name = "Internal Audit Municipality" };
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();
        var user = IdpTestFixture.CreateUser($"auditor-{Guid.NewGuid():N}");
        user.MunicipalityId = municipality.Id;
        context.Users.Add(user);
        var financialYear = new FinancialYear { Code = $"FY-{Guid.NewGuid():N}"[..20], Name = "2026/2027", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear, IsCurrent = true, EffectiveFrom = financialYear.StartDate };
        context.MunicipalityFinancialYears.Add(year);
        var target = new OpmsTarget { MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "KPI-IA", TargetName = "Audit target", KpiDescription = "Audit target", AnnualTargetDescription = "Target", PerformanceObjective = "Objective" };
        context.OpmsTargets.Add(target);
        await context.SaveChangesAsync();
        var submission = new OpmsSubmission { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, OpmsTarget = target, Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "reviewed", SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow };
        context.OpmsSubmissions.Add(submission);
        var workflow = new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "DEFAULT", Name = "Default", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        context.WorkflowDefinitions.Add(workflow);
        await context.SaveChangesAsync();
        var instance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id };
        context.SubmissionWorkflowInstances.Add(instance);
        if (model.HasValue)
            context.InternalAuditAssessmentConfigurations.Add(new InternalAuditAssessmentConfiguration { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, Model = model.Value, EffectiveFrom = DateTime.UtcNow.AddHours(-1), Reason = "Approved test configuration", CreatedByUserId = user.Id, CreatedByUser = user });
        await context.SaveChangesAsync();
        return new(municipality, year, user, submission);
    }

    private sealed record Seed(Municipality Municipality, MunicipalityFinancialYear Year, ApplicationUser User, OpmsSubmission Submission);
}

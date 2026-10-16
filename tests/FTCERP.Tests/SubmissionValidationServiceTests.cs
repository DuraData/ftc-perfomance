using FTCERP.Host.Application.Services;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FTCERP.Tests;

public sealed class SubmissionValidationServiceTests
{
    [Fact]
    public async Task Controller_rejects_invalid_submit_before_workflow_preparation_or_state_mutation()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, ReportingPeriodType.Quarter1);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(seed.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        var workflow = new Mock<IConfigurableWorkflowService>();
        var validation = new Mock<ISubmissionValidationService>();
        validation.Setup(item => item.ValidateAsync(It.IsAny<OpmsSubmission>())).ReturnsAsync(new SubmissionValidationResult(
            [new SubmissionValidationError("VarianceReason", "Variance reason is required.")], false));
        var controller = new OpmsSubmissionsController(
            context, IdpTestFixture.CreateUserManagerMock(seed.User).Object, access.Object,
            Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(),
            workflow.Object, Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(),
            Mock.Of<IEvidenceMalwareScanner>(), Mock.Of<IPerformanceSuggestionService>(), validation.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(seed.User.Id) } }
        };

        var action = await controller.Submit(seed.Destination.PublicId.ToString(), new SubmissionWorkflowActionRequest(null));

        var badRequest = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ApiResponse<OpmsSubmissionResponse>>().Subject.Errors.Should().ContainSingle().Which.Should().Contain("VarianceReason");
        workflow.Verify(item => item.PrepareActionAsync(It.IsAny<SubmissionKind>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkflowActionOutcome>(), It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<string>()), Times.Never);
        seed.Destination.BaseState.Should().Be(SubmissionBaseStates.InProgress);
        seed.Destination.SubmittedAt.Should().BeNull();
        (await context.SubmissionWorkflowActions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Submit_boundary_reports_every_missing_mandatory_field_without_changing_the_draft()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, ReportingPeriodType.Quarter1);
        seed.Destination.ActualPerformance = null;
        seed.Destination.VarianceReason = null;
        seed.Destination.CorrectiveMeasure = null;
        var service = new SubmissionValidationService(context);

        var result = await service.ValidateAsync(seed.Destination);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(item => item.Field).Should().Contain(["ActualPerformance", "VarianceReason", "CorrectiveMeasure"]);
        seed.Destination.BaseState.Should().Be(SubmissionBaseStates.InProgress);
        seed.Destination.SubmittedAt.Should().BeNull();
        seed.Destination.SubmittedByUserId.Should().BeNull();
        (await context.SubmissionWorkflowActions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Required_poe_accepts_only_a_current_signature_validated_clean_blob()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, ReportingPeriodType.Quarter1, requiresPoe: true);
        var service = new SubmissionValidationService(context);

        (await service.ValidateAsync(seed.Destination)).Errors.Should().ContainSingle(item => item.Field == "Evidence");
        await AddEvidenceAsync(context, seed, seed.Destination.Id, "ScannerUnavailable", quarantined: true);
        (await service.ValidateAsync(seed.Destination)).Errors.Should().ContainSingle(item => item.Field == "Evidence");

        await AddEvidenceAsync(context, seed, seed.Destination.Id, "Clean", quarantined: false);
        var accepted = await service.ValidateAsync(seed.Destination);

        accepted.RequiresPoe.Should().BeTrue();
        accepted.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(ReportingPeriodType.MidTerm, ReportingPeriodType.Quarter1)]
    [InlineData(ReportingPeriodType.Annual, ReportingPeriodType.Quarter4)]
    public async Task Consolidation_period_inherits_clean_evidence_from_submitted_source_quarters(
        ReportingPeriodType destinationType, ReportingPeriodType sourceType)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, destinationType, requiresPoe: true, sourceType);
        await AddEvidenceAsync(context, seed, seed.Source!.Id, "Clean", quarantined: false);

        var result = await new SubmissionValidationService(context).ValidateAsync(seed.Destination);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Latest_effective_workflow_rating_wins_without_an_older_version_leaking_rules()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, ReportingPeriodType.Quarter1);
        AddWorkflow(context, seed, version: 1, requiresPoe: true);
        AddWorkflow(context, seed, version: 2, requiresPoe: false);
        await context.SaveChangesAsync();
        var candidate = RatingCandidate(seed.Destination, 75m);

        var result = await new SubmissionValidationService(context).ValidateAsync(candidate);
        result.RequiresPoe.Should().BeFalse();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Latest_rating_outcome_can_make_poe_mandatory_at_submit_time()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, ReportingPeriodType.Quarter1);
        AddWorkflow(context, seed, version: 1, requiresPoe: true);
        await context.SaveChangesAsync();
        var candidate = RatingCandidate(seed.Destination, 75m);

        var result = await new SubmissionValidationService(context).ValidateAsync(candidate);

        result.RequiresPoe.Should().BeTrue();
        result.Errors.Should().ContainSingle(item => item.Field == "Evidence");
    }

    private static async Task<Seed> SeedAsync(
        ApplicationDbContext context,
        ReportingPeriodType destinationType,
        bool requiresPoe = false,
        ReportingPeriodType? sourceType = null)
    {
        var municipality = new Municipality { Code = $"M-{Guid.NewGuid():N}"[..12], Name = "Validation Municipality" };
        var financialYear = new FinancialYear { Code = $"FY-{Guid.NewGuid():N}"[..12], Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        var municipalityYear = new MunicipalityFinancialYear { Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, EffectiveFrom = financialYear.StartDate };
        var destinationPeriod = Period(municipalityYear, destinationType, 10);
        var target = new OpmsTarget { Municipality = municipality, IndicatorNumber = "SUBMIT-1", TargetName = "Submission validation", KpiDescription = "Submission validation KPI", AnnualTargetDescription = "100", PerformanceObjective = "Validate" };
        var user = IdpTestFixture.CreateUser($"validator-{Guid.NewGuid():N}");
        user.Municipality = municipality;
        var destination = new OpmsSubmission
        {
            Municipality = municipality, OpmsTarget = target, ReportingPeriod = destinationPeriod,
            BaseState = SubmissionBaseStates.InProgress, Status = SubmissionBaseStates.InProgress,
            ActualPerformance = "75", VarianceReason = "Performance variance explained.",
            CorrectiveMeasure = "Corrective measurement recorded.", RequiresPoe = requiresPoe
        };
        OpmsSubmission? source = null;
        if (sourceType.HasValue)
        {
            var sourcePeriod = Period(municipalityYear, sourceType.Value, 1);
            source = new OpmsSubmission
            {
                Municipality = municipality, OpmsTarget = target, ReportingPeriod = sourcePeriod,
                BaseState = SubmissionBaseStates.Submitted, Status = SubmissionBaseStates.Submitted,
                ActualPerformance = "50", VarianceReason = "Source variance explained.", CorrectiveMeasure = "Source corrective measurement."
            };
            context.OpmsSubmissions.Add(source);
        }
        context.AddRange(municipality, financialYear, municipalityYear, destinationPeriod, target, user, destination);
        await context.SaveChangesAsync();
        return new(municipality, municipalityYear, target, user, destination, source);
    }

    private static ReportingPeriod Period(MunicipalityFinancialYear year, ReportingPeriodType type, int sequence) => new()
    {
        MunicipalityFinancialYear = year, Code = type.ToString(), Name = type.ToString(), PeriodType = type,
        Sequence = sequence, StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30), IsActive = true
    };

    private static OpmsSubmission RatingCandidate(OpmsSubmission source, decimal achievementPercent) => new()
    {
        Id = source.Id, OpmsTargetId = source.OpmsTargetId, ReportingPeriodId = source.ReportingPeriodId,
        BaseState = source.BaseState, ActualPerformance = source.ActualPerformance,
        VarianceReason = source.VarianceReason, CorrectiveMeasure = source.CorrectiveMeasure,
        RequiresPoe = source.RequiresPoe, AchievementPercent = achievementPercent
    };

    private static async Task AddEvidenceAsync(ApplicationDbContext context, Seed seed, string submissionId, string scanStatus, bool quarantined)
    {
        context.PoeFiles.Add(new PoeFile
        {
            MunicipalityId = seed.Municipality.Id, SubmissionKind = SubmissionKind.Opms, SubmissionId = submissionId,
            FileName = $"{Guid.NewGuid():N}.pdf", UploadedByUserId = seed.User.Id,
            Blob = new EvidenceBlob { MunicipalityId = seed.Municipality.Id, StorageKey = $"tests/{Guid.NewGuid():N}.pdf", ContentType = "application/pdf", SizeInBytes = 10, Sha256 = new string('a', 64), SignatureVerified = true, ScanStatus = scanStatus, IsQuarantined = quarantined }
        });
        await context.SaveChangesAsync();
    }

    private static void AddWorkflow(ApplicationDbContext context, Seed seed, int version, bool requiresPoe)
    {
        var rating = new RatingScheme { MunicipalityId = seed.Municipality.Id, Code = $"R-{version}", Name = $"Rating {version}" };
        rating.Values.Add(new RatingSchemeValue { MunicipalityId = seed.Municipality.Id, Value = 1, Label = "Range", MinimumAchievementPercent = 0, MaximumAchievementPercent = 100, RequiresPoe = requiresPoe, SortOrder = 1 });
        var definition = new WorkflowDefinition { MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYearId = seed.Year.Id, SubmissionKind = SubmissionKind.Opms, Code = $"WF-{version}", Name = $"Workflow {version}", Version = version, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { MunicipalityId = seed.Municipality.Id, Code = "REVIEW", Name = "Review", Sequence = 1, RequiredActionCode = "REVIEW", RequiredPermissionCode = "OPMS_WORKFLOW.REVIEW", RatingScheme = rating, RequiresRating = true });
        context.Add(definition);
    }

    private sealed record Seed(Municipality Municipality, MunicipalityFinancialYear Year, OpmsTarget Target, ApplicationUser User, OpmsSubmission Destination, OpmsSubmission? Source);
}

using FTCERP.Host.Application.Services;
using Microsoft.Extensions.Configuration;

namespace FTCERP.Tests;

public sealed class ConfigurableWorkflowTests
{
    [Fact]
    public async Task Workflow_DeniesActionWhenNoEffectiveDefinitionExists()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Id = 7, Code = "WF-NONE", Name = "Workflow Test Municipality" };
        var financialYear = new FinancialYear { Id = 901, Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) };
        var year = new MunicipalityFinancialYear { Id = 901, Municipality = municipality, FinancialYear = financialYear, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = 902, MunicipalityFinancialYearId = year.Id, Code = "Q-NONE", Name = "Unconfigured period", StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) };
        context.AddRange(municipality, financialYear, year, period);
        await context.SaveChangesAsync();

        var result = await new ConfigurableWorkflowService(context).PrepareActionAsync(
            SubmissionKind.Opms, "submission-unconfigured", period.Id, "submitter", "submitter",
            "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit, null, null, "trace-unconfigured");

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain("No effective active workflow definition");
        context.SubmissionWorkflowInstances.Should().BeEmpty();
        context.SubmissionWorkflowActions.Should().BeEmpty();
    }

    [Fact]
    public async Task Workflow_AdvancesByConfiguration_AndEnforcesSubmitterSeparation()
    {
        await using var context = IdpTestFixture.CreateContext();
        var year = new MunicipalityFinancialYear { Id = 1, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = 10, MunicipalityFinancialYearId = 1, Code = "Q1", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var scheme = new RatingScheme { Id = 30, MunicipalityId = 7, Code = "FIVE_POINT", Name = "Five point scale" };
        scheme.Values.Add(new RatingSchemeValue { Id = 31, MunicipalityId = 7, Value = 4, Label = "Exceeded", SortOrder = 4 });
        var definition = new WorkflowDefinition { Id = 20, MunicipalityId = 7, MunicipalityFinancialYearId = 1, SubmissionKind = SubmissionKind.Opms, Code = "DEFAULT", Name = "Default", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = 21, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT", RequireDifferentActorFromSubmitter = false });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 22, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY", RequireDifferentActorFromSubmitter = true, IsTerminal = true, RequiresRating = true, RatingSchemeId = scheme.Id, RatingScheme = scheme });
        context.AddRange(year, period, scheme, definition);
        await context.SaveChangesAsync();
        var service = new ConfigurableWorkflowService(context);

        var submitted = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-1", period.Id, "submitter", "submitter", "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit, null, null, "trace-1");
        submitted.Allowed.Should().BeTrue();
        submitted.Instance!.CurrentStageId.Should().Be(22);
        await context.SaveChangesAsync();

        var submitterProjection = await service.GetProjectionAsync(SubmissionKind.Opms, "submission-1", period.Id, "submitter", "submitter");
        submitterProjection.Stages.Select(stage => stage.Name).Should().Equal("Submit", "Verify");
        submitterProjection.CurrentStageCode.Should().Be("VERIFY");
        submitterProjection.ActorEligible.Should().BeFalse();
        (await service.GetProjectionAsync(SubmissionKind.Opms, "submission-1", period.Id, "submitter", "verifier"))
            .ActorEligible.Should().BeTrue();

        var selfVerify = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-1", period.Id, "submitter", "submitter", "OPMS_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve, null, null, "trace-2");
        selfVerify.Allowed.Should().BeFalse();
        selfVerify.Reason.Should().Contain("Segregation");

        var missingRating = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-1", period.Id, "submitter", "verifier", "OPMS_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve, null, null, "trace-2b");
        missingRating.Allowed.Should().BeFalse();
        missingRating.Reason.Should().Contain("requires");

        var verified = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-1", period.Id, "submitter", "verifier", "OPMS_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve, "Verified", 4, "trace-3");
        verified.Allowed.Should().BeTrue();
        verified.Instance!.State.Should().Be(WorkflowInstanceState.Completed);
        verified.Action!.Sequence.Should().Be(2);
        await context.SaveChangesAsync();

        var rating = await context.SubmissionStageRatings.SingleAsync();
        rating.Value.Should().Be(4);
        rating.LabelSnapshot.Should().Be("Exceeded");
        rating.CorrelationId.Should().Be("trace-3");
        rating.Value = 3;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Theory]
    [InlineData(SubmissionKind.Opms)]
    [InlineData(SubmissionKind.Ipms)]
    public async Task Workflow_DerivesRequiredRatingFromTheRecordedAchievementBand(SubmissionKind kind)
    {
        await using var context = IdpTestFixture.CreateContext();
        var year = new MunicipalityFinancialYear { Id = 401, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = 410, MunicipalityFinancialYearId = year.Id, Code = "Q1-AUTO", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var scheme = new RatingScheme { Id = 430, MunicipalityId = 7, Code = "AUTO", Name = "Achievement bands" };
        scheme.Values.Add(new RatingSchemeValue { Id = 431, MunicipalityId = 7, Value = 2, Label = "Below target", MinimumAchievementPercent = 0, MaximumAchievementPercent = 79.9999m, SortOrder = 1 });
        scheme.Values.Add(new RatingSchemeValue { Id = 432, MunicipalityId = 7, Value = 4, Label = "Achieved", MinimumAchievementPercent = 80, MaximumAchievementPercent = 100, SortOrder = 2 });
        var prefix = kind == SubmissionKind.Opms ? "OPMS" : "IPMS";
        var definition = new WorkflowDefinition { Id = 420, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = kind, Code = "AUTO", Name = "Automatic rating", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = 421, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = $"{prefix}_SUBMISSION.SUBMIT", RequiredPermissionCode = $"{prefix}_SUBMISSION.SUBMIT", RequireDifferentActorFromSubmitter = false });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 422, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = $"{prefix}_SUBMISSION.VERIFY", RequiredPermissionCode = $"{prefix}_SUBMISSION.VERIFY", RequireDifferentActorFromSubmitter = true, IsTerminal = true, RequiresRating = true, RatingScheme = scheme });
        object submission = kind == SubmissionKind.Opms
            ? new OpmsSubmission { Id = "submission-auto-rating", MunicipalityId = 7, AchievementPercent = 87.5m }
            : new IpmsSubmission { Id = "submission-auto-rating", MunicipalityId = 7, AchievementPercent = 87.5m };
        context.AddRange(year, period, scheme, definition, submission);
        await context.SaveChangesAsync();
        var service = new ConfigurableWorkflowService(context);

        (await service.PrepareActionAsync(kind, "submission-auto-rating", period.Id, "submitter", "submitter", $"{prefix}_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit, null, null, "trace-auto-submit")).Allowed.Should().BeTrue();
        await context.SaveChangesAsync();
        var verified = await service.PrepareActionAsync(kind, "submission-auto-rating", period.Id, "submitter", "verifier", $"{prefix}_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve, "Verified", null, "trace-auto-rating");

        verified.Allowed.Should().BeTrue();
        verified.Action!.RatingValue.Should().Be(4);
        await context.SaveChangesAsync();
        var rating = await context.SubmissionStageRatings.SingleAsync();
        rating.Value.Should().Be(4);
        rating.LabelSnapshot.Should().Be("Achieved");
        rating.AchievementPercent.Should().Be(87.5m);
        rating.CorrelationId.Should().Be("trace-auto-rating");
    }

    [Theory]
    [InlineData(false, "does not match")]
    [InlineData(true, "multiple bands")]
    public async Task Workflow_FailsClosedWhenAchievementBandsCannotSelectOneRating(bool overlappingBands, string expectedReason)
    {
        await using var context = IdpTestFixture.CreateContext();
        var suffix = overlappingBands ? 500 : 600;
        var year = new MunicipalityFinancialYear { Id = suffix + 1, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = suffix + 2, MunicipalityFinancialYearId = year.Id, Code = $"Q1-{suffix}", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var scheme = new RatingScheme { Id = suffix + 3, MunicipalityId = 7, Code = $"BANDS-{suffix}", Name = "Invalid runtime bands" };
        scheme.Values.Add(new RatingSchemeValue { Id = suffix + 4, MunicipalityId = 7, Value = 2, Label = "First", MinimumAchievementPercent = overlappingBands ? 60 : 0, MaximumAchievementPercent = overlappingBands ? 90 : 40, SortOrder = 1 });
        scheme.Values.Add(new RatingSchemeValue { Id = suffix + 5, MunicipalityId = 7, Value = 4, Label = "Second", MinimumAchievementPercent = overlappingBands ? 70 : 90, MaximumAchievementPercent = 100, SortOrder = 2 });
        var definition = new WorkflowDefinition { Id = suffix + 6, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms, Code = $"WF-{suffix}", Name = "Fail closed rating", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = suffix + 7, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY", RequireDifferentActorFromSubmitter = true, IsTerminal = true, RequiresRating = true, RatingScheme = scheme });
        var submissionId = $"submission-invalid-band-{suffix}";
        context.AddRange(year, period, scheme, definition, new OpmsSubmission { Id = submissionId, MunicipalityId = 7, AchievementPercent = 75m });
        await context.SaveChangesAsync();

        var result = await new ConfigurableWorkflowService(context).PrepareActionAsync(
            SubmissionKind.Opms, submissionId, period.Id, "submitter", "verifier",
            "OPMS_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve, null, null, $"trace-{suffix}");

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain(expectedReason);
        context.SubmissionWorkflowActions.Should().BeEmpty();
        context.SubmissionStageRatings.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistingWorkflowInstance_RemainsPinnedToItsOriginalDefinitionVersion()
    {
        await using var context = IdpTestFixture.CreateContext();
        var year = new MunicipalityFinancialYear { Id = 101, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-20) };
        var period = new ReportingPeriod { Id = 110, MunicipalityFinancialYearId = year.Id, Code = "Q1-PIN", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var original = new WorkflowDefinition { Id = 120, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms, Code = "PINNED", Name = "Original", Version = 1, IsActive = false, EffectiveFrom = DateTime.UtcNow.AddDays(-10), EffectiveTo = DateTime.UtcNow.AddDays(-1) };
        original.Stages.Add(new WorkflowStageDefinition { Id = 121, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT", RequireDifferentActorFromSubmitter = false });
        original.Stages.Add(new WorkflowStageDefinition { Id = 122, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY", RequireDifferentActorFromSubmitter = false });
        original.Stages.Add(new WorkflowStageDefinition { Id = 123, MunicipalityId = 7, Code = "APPROVE", Name = "Approve", Sequence = 3, RequiredActionCode = "OPMS_SUBMISSION.APPROVE", RequiredPermissionCode = "OPMS_SUBMISSION.APPROVE", RequireDifferentActorFromSubmitter = false, IsTerminal = true });
        var replacement = new WorkflowDefinition { Id = 220, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms, Code = "PINNED", Name = "Replacement", Version = 2, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        replacement.Stages.Add(new WorkflowStageDefinition { Id = 221, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT" });
        replacement.Stages.Add(new WorkflowStageDefinition { Id = 222, MunicipalityId = 7, Code = "EXEC_REVIEW", Name = "Executive review", Sequence = 2, RequiredActionCode = "OPMS_SUBMISSION.APPROVE", RequiredPermissionCode = "OPMS_SUBMISSION.APPROVE", IsTerminal = true });
        var instance = new SubmissionWorkflowInstance { Id = 130, MunicipalityId = 7, WorkflowDefinition = original, WorkflowDefinitionId = original.Id, CurrentStage = original.Stages.Single(stage => stage.Id == 122), CurrentStageId = 122, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-pinned" };
        context.AddRange(year, period, original, replacement, instance);
        await context.SaveChangesAsync();

        var result = await new ConfigurableWorkflowService(context).PrepareActionAsync(SubmissionKind.Opms, instance.SubmissionId, period.Id, "submitter", "verifier", "OPMS_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve, "Verified", null, "trace-pinned");

        result.Allowed.Should().BeTrue();
        result.Action!.ToStageId.Should().Be(123);
        result.Instance!.WorkflowDefinitionId.Should().Be(original.Id);
    }

    [Fact]
    public async Task Workflow_AllowsGovernedRejectCounterpart_AndReturnsToConfiguredReworkStage()
    {
        await using var context = IdpTestFixture.CreateContext();
        var year = new MunicipalityFinancialYear { Id = 301, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = 310, MunicipalityFinancialYearId = year.Id, Code = "Q1-REJECT", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var definition = new WorkflowDefinition { Id = 320, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms, Code = "REWORK", Name = "Rework", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = 321, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT" });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 322, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY", RejectionStageCode = "SUBMIT", RequireDifferentActorFromSubmitter = true });
        context.AddRange(year, period, definition);
        await context.SaveChangesAsync();
        var service = new ConfigurableWorkflowService(context);

        (await service.PrepareActionAsync(SubmissionKind.Opms, "submission-rework", period.Id, "submitter", "submitter", "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit, "Ready", null, "trace-submit")).Allowed.Should().BeTrue();
        await context.SaveChangesAsync();
        var rejected = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-rework", period.Id, "submitter", "verifier", "OPMS_SUBMISSION.VERIFY_REJECT", WorkflowActionOutcome.Reject, "Evidence is incomplete", null, "trace-reject");

        rejected.Allowed.Should().BeTrue();
        rejected.Instance!.State.Should().Be(WorkflowInstanceState.Rework);
        rejected.Instance.CurrentStageId.Should().Be(321);
        rejected.Action!.ActionCode.Should().Be("OPMS_SUBMISSION.VERIFY_REJECT");
        rejected.Action.ToStageId.Should().Be(321);
        ConfigurableWorkflowService.IsActionAllowedAtStage("IPMS_SUBMISSION.APPROVE", "IPMS_SUBMISSION.REJECT", WorkflowActionOutcome.Reject).Should().BeTrue();
        ConfigurableWorkflowService.IsActionAllowedAtStage("IPMS_SUBMISSION.VERIFY", "IPMS_SUBMISSION.APPROVE", WorkflowActionOutcome.Approve).Should().BeFalse();
    }

    [Fact]
    public async Task Workflow_ApproverCanBypassConfiguredOptionalVerifierWithOneAuditableAction()
    {
        await using var context = IdpTestFixture.CreateContext();
        var year = new MunicipalityFinancialYear { Id = 701, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = 710, MunicipalityFinancialYearId = year.Id, Code = "Q1-BYPASS", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var definition = new WorkflowDefinition { Id = 720, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms, Code = "BYPASS", Name = "Optional verification", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = 721, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT" });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 722, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY", IsOptional = true, AllowBypass = true });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 723, MunicipalityId = 7, Code = "APPROVE", Name = "Approve", Sequence = 3, RequiredActionCode = "OPMS_SUBMISSION.APPROVE", RequiredPermissionCode = "OPMS_SUBMISSION.APPROVE", IsTerminal = true, RequireDifferentActorFromSubmitter = true });
        context.AddRange(year, period, definition);
        await context.SaveChangesAsync();
        var service = new ConfigurableWorkflowService(context);

        (await service.PrepareActionAsync(SubmissionKind.Opms, "submission-bypass", period.Id, "submitter", "submitter", "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit, null, null, "trace-submit")).Allowed.Should().BeTrue();
        await context.SaveChangesAsync();
        (await service.GetProjectionAsync(SubmissionKind.Opms, "submission-bypass", period.Id, "submitter", "approver"))
            .ActorEligibleForBypass.Should().BeTrue();
        (await service.GetProjectionAsync(SubmissionKind.Opms, "submission-bypass", period.Id, "submitter", "submitter"))
            .ActorEligibleForBypass.Should().BeFalse();
        var bypassed = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-bypass", period.Id, "submitter", "approver", "OPMS_SUBMISSION.APPROVE_BYPASS", WorkflowActionOutcome.Bypass, "Approved without verifier delay", null, "trace-bypass");

        bypassed.Allowed.Should().BeTrue();
        bypassed.Instance!.State.Should().Be(WorkflowInstanceState.Completed);
        bypassed.Action!.FromStageId.Should().Be(722);
        bypassed.Action.ToStageId.Should().BeNull();
        bypassed.Action.ActionCode.Should().Be("OPMS_SUBMISSION.APPROVE_BYPASS");
        bypassed.Action.Outcome.Should().Be(WorkflowActionOutcome.Bypass);
    }

    [Fact]
    public async Task Workflow_ReopenAndResubmitPreserveStageAndAppendDistinctActions()
    {
        await using var context = IdpTestFixture.CreateContext();
        var year = new MunicipalityFinancialYear { Id = 801, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var period = new ReportingPeriod { Id = 810, MunicipalityFinancialYearId = year.Id, Code = "Q1-CORRECT", Name = "Quarter 1", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10) };
        var definition = new WorkflowDefinition { Id = 820, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms, Code = "CORRECT", Name = "Correction", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = 821, MunicipalityId = 7, Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT" });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 822, MunicipalityId = 7, Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY" });
        definition.Stages.Add(new WorkflowStageDefinition { Id = 823, MunicipalityId = 7, Code = "APPROVE", Name = "Approve", Sequence = 3, RequiredActionCode = "OPMS_SUBMISSION.APPROVE", RequiredPermissionCode = "OPMS_SUBMISSION.APPROVE", IsTerminal = true });
        context.AddRange(year, period, definition);
        await context.SaveChangesAsync();
        var service = new ConfigurableWorkflowService(context);

        (await service.PrepareActionAsync(SubmissionKind.Opms, "submission-correct", period.Id, "submitter", "submitter", "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit, null, null, "trace-submit")).Allowed.Should().BeTrue();
        await context.SaveChangesAsync();
        var reopened = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-correct", period.Id, "submitter", "approver", "OPMS_SUBMISSION.CORRECT", WorkflowActionOutcome.Reopen, "Correct the actual value", null, "trace-reopen");
        reopened.Allowed.Should().BeTrue();
        reopened.Instance!.State.Should().Be(WorkflowInstanceState.Rework);
        reopened.Instance.CurrentStageId.Should().Be(822);
        await context.SaveChangesAsync();

        var resubmitted = await service.PrepareActionAsync(SubmissionKind.Opms, "submission-correct", period.Id, "submitter", "submitter", "OPMS_SUBMISSION.RESUBMIT", WorkflowActionOutcome.Resubmit, "Corrected", null, "trace-resubmit");
        resubmitted.Allowed.Should().BeTrue(resubmitted.Reason);
        resubmitted.Instance!.State.Should().Be(WorkflowInstanceState.Active);
        resubmitted.Instance.CurrentStageId.Should().Be(822);
        resubmitted.Action!.Outcome.Should().Be(WorkflowActionOutcome.Resubmit);
        resubmitted.Instance.Actions.Append(resubmitted.Action).Select(action => action.ActionCode)
            .Should().ContainInOrder("OPMS_SUBMISSION.SUBMIT", "OPMS_SUBMISSION.CORRECT", "OPMS_SUBMISSION.RESUBMIT");
    }

    [Fact]
    public void WorkflowComparison_ReportsAddedRemovedAndModifiedStages()
    {
        var from = new WorkflowDefinition();
        from.Stages.Add(new WorkflowStageDefinition { Code = "SUBMIT", Name = "Submit", Sequence = 1, RequiredActionCode = "SUBMIT", RequiredPermissionCode = "SUBMIT" });
        from.Stages.Add(new WorkflowStageDefinition { Code = "VERIFY", Name = "Verify", Sequence = 2, RequiredActionCode = "VERIFY", RequiredPermissionCode = "VERIFY" });
        var to = new WorkflowDefinition();
        to.Stages.Add(new WorkflowStageDefinition { Code = "SUBMIT", Name = "Capture and submit", Sequence = 1, RequiredActionCode = "SUBMIT", RequiredPermissionCode = "SUBMIT" });
        to.Stages.Add(new WorkflowStageDefinition { Code = "APPROVE", Name = "Approve", Sequence = 2, RequiredActionCode = "APPROVE", RequiredPermissionCode = "APPROVE", IsTerminal = true });

        var result = WorkflowDefinitionComparer.Compare(from, to);

        result.Single(item => item.StageCode == "SUBMIT").Should().Match<WorkflowStageDifference>(item => item.Change == "Modified" && item.ChangedFields.Contains("Name"));
        result.Single(item => item.StageCode == "VERIFY").Change.Should().Be("Removed");
        result.Single(item => item.StageCode == "APPROVE").Change.Should().Be("Added");
    }

    [Fact]
    public async Task ReportingWindow_EnforcesBeforeOpenAfterAndScopedExtensionDecisions()
    {
        await using var context = IdpTestFixture.CreateContext();
        var now = DateTime.UtcNow;
        var window = new ReportingWindow { Id = 1, MunicipalityId = 7, ReportingPeriodId = 10, SubmissionKind = SubmissionKind.Opms, OpensAt = now.AddDays(-10), ClosesAt = now.AddDays(-1) };
        context.ReportingWindows.Add(window);
        context.ReportingWindowExceptions.Add(new ReportingWindowException { MunicipalityId = 7, ReportingWindowId = 1, UserId = "allowed-user", ExtendedClosesAt = now.AddDays(2), Reason = "Approved extension", ApprovedByUserId = "manager" });
        context.ReportingWindowExceptions.Add(new ReportingWindowException { MunicipalityId = 7, ReportingWindowId = 1, DepartmentId = 50, ExtendedClosesAt = now.AddDays(3), Reason = "Approved department extension", ApprovedByUserId = "manager" });
        await context.SaveChangesAsync();
        var service = new ReportingWindowService(context);

        var before = await service.CheckAsync(SubmissionKind.Opms, 10, "other-user", null, null, now.AddDays(-11));
        before.Allowed.Should().BeFalse();
        before.Reason.Should().Contain("Reporting opens at");

        var during = await service.CheckAsync(SubmissionKind.Opms, 10, "other-user", null, null, now.AddDays(-5));
        during.Allowed.Should().BeTrue();
        during.Reason.Should().Be("Reporting window is open.");

        var after = await service.CheckAsync(SubmissionKind.Opms, 10, "other-user", null, null, now);
        after.Allowed.Should().BeFalse();
        after.Reason.Should().Contain("Reporting closed at");

        var userExtension = await service.CheckAsync(SubmissionKind.Opms, 10, "allowed-user", null, null, now);
        userExtension.Allowed.Should().BeTrue();
        userExtension.EffectiveClose.Should().Be(now.AddDays(2));

        var departmentExtension = await service.CheckAsync(SubmissionKind.Opms, 10, "department-user", 50, null, now.AddDays(2.5));
        departmentExtension.Allowed.Should().BeTrue();
        departmentExtension.EffectiveClose.Should().Be(now.AddDays(3));

        (await service.CheckAsync(SubmissionKind.Opms, 10, "allowed-user", null, null, now.AddDays(2.5))).Allowed.Should().BeFalse();
        (await service.CheckAsync(SubmissionKind.Opms, 10, "department-user", 50, null, now.AddDays(3.5))).Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task RfiEvidenceProvenance_IsAppendOnly()
    {
        await using var context = IdpTestFixture.CreateContext();
        var link = new PerformanceRfiEvidence
        {
            Id = 1,
            MunicipalityId = 7,
            PerformanceRfiId = 42,
            PoeFileId = "poe-1",
            Purpose = RfiEvidencePurpose.Response,
            LinkedByUserId = "responder",
            CorrelationId = "trace-rfi-evidence"
        };
        context.Attach(link);

        link.Purpose = RfiEvidencePurpose.Question;
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public void RfiEvidencePolicy_RejectsQuarantineAndCrossSubmissionLinks()
    {
        var requested = Guid.NewGuid();
        var quarantined = new PoeFile { PublicId = requested, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-1", IsActive = true, Blob = new EvidenceBlob { SignatureVerified = true, IsQuarantined = true, ScanStatus = "ThreatDetected" } };
        RfiEvidencePolicy.Validate(SubmissionKind.Opms, "submission-1", [requested], [quarantined]).Allowed.Should().BeFalse();

        var otherSubmission = new PoeFile { PublicId = requested, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-2", IsActive = true, Blob = new EvidenceBlob { SignatureVerified = true, ScanStatus = "Clean" } };
        RfiEvidencePolicy.Validate(SubmissionKind.Opms, "submission-1", [requested], [otherSubmission]).Allowed.Should().BeFalse();

        var clean = new PoeFile { PublicId = requested, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-1", IsActive = true, Blob = new EvidenceBlob { SignatureVerified = true, ScanStatus = "Clean" } };
        RfiEvidencePolicy.Validate(SubmissionKind.Opms, "submission-1", [requested], [clean]).Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task PoeAssessments_RequireReleasedEvidence_AndRemainAppendOnly()
    {
        var evidence = new PoeFile { Id = "poe-1", IsActive = true, Blob = new EvidenceBlob { SignatureVerified = true, ScanStatus = "Clean" } };
        PoeAssessmentPolicy.Validate(evidence, PoeAssessmentOutcome.Rejected, null).Allowed.Should().BeFalse();
        evidence.Blob.IsQuarantined = true;
        PoeAssessmentPolicy.Validate(evidence, PoeAssessmentOutcome.Accepted, null).Allowed.Should().BeFalse();
        evidence.Blob.IsQuarantined = false;
        PoeAssessmentPolicy.Validate(evidence, PoeAssessmentOutcome.NeedsClarification, "Provide signed minutes.").Allowed.Should().BeTrue();

        await using var context = IdpTestFixture.CreateContext();
        evidence.MunicipalityId = 7;
        evidence.SubmissionKind = SubmissionKind.Opms;
        evidence.SubmissionId = "submission-1";
        evidence.FileName = "evidence.pdf";
        evidence.UploadedByUserId = "auditor";
        context.PoeFiles.Add(evidence);
        var assessment = new PoeEvidenceAssessment { MunicipalityId = 7, PoeFileId = evidence.Id, Outcome = PoeAssessmentOutcome.Accepted, AssessedByUserId = "auditor", CorrelationId = "trace-assessment" };
        context.PoeEvidenceAssessments.Add(assessment);
        await context.SaveChangesAsync();
        assessment.Comment = "Mutation attempt";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task PoeReplacementPolicy_RequiresSameSubmissionCleanSuccessor_AndLedgerIsAppendOnly()
    {
        var oldEvidence = new PoeFile { Id = "old", PublicId = Guid.NewGuid(), SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-1", IsActive = true };
        var replacement = new PoeFile { Id = "new", PublicId = Guid.NewGuid(), SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-1", IsActive = true, Blob = new EvidenceBlob { SignatureVerified = true, ScanStatus = "Clean" } };
        PoeReplacementPolicy.Validate(oldEvidence, replacement, SubmissionKind.Opms, "submission-1", "Corrected signed version").Allowed.Should().BeTrue();
        replacement.Blob.IsQuarantined = true;
        PoeReplacementPolicy.Validate(oldEvidence, replacement, SubmissionKind.Opms, "submission-1", "Corrected signed version").Allowed.Should().BeFalse();
        replacement.Blob.IsQuarantined = false; replacement.SubmissionId = "submission-2";
        PoeReplacementPolicy.Validate(oldEvidence, replacement, SubmissionKind.Opms, "submission-1", "Corrected signed version").Allowed.Should().BeFalse();

        await using var context = IdpTestFixture.CreateContext();
        oldEvidence.MunicipalityId = 7;
        oldEvidence.IsActive = false;
        oldEvidence.FileName = "old.pdf";
        oldEvidence.UploadedByUserId = "owner";
        replacement.MunicipalityId = 7;
        replacement.SubmissionId = oldEvidence.SubmissionId;
        replacement.SupersedesPoeFileId = oldEvidence.Id;
        replacement.FileName = "new.pdf";
        replacement.UploadedByUserId = "owner";
        context.PoeFiles.AddRange(oldEvidence, replacement);
        var ledger = new PoeEvidenceReplacement { MunicipalityId = 7, SupersededPoeFileId = "old", ReplacementPoeFileId = "new", Reason = "Corrected signed version", ReplacedByUserId = "owner", CorrelationId = "trace-replacement" };
        context.PoeEvidenceReplacements.Add(ledger);
        await context.SaveChangesAsync();
        ledger.Reason = "Mutation attempt";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task PoeLegalHoldEvents_DeriveActiveState_AndRemainAppendOnly()
    {
        var holdId = Guid.NewGuid();
        var placed = new PoeLegalHoldEvent { HoldId = holdId, MunicipalityId = 7, PoeFileId = "poe-1", Action = PoeLegalHoldAction.Placed, HoldReference = "CASE-2026-1", Reason = "Pending investigation", ActorUserId = "legal", CorrelationId = "trace-hold" };
        PoeLegalHoldPolicy.IsActive([placed], holdId).Should().BeTrue();
        PoeLegalHoldPolicy.HasAnyActiveHold([placed]).Should().BeTrue();
        var released = new PoeLegalHoldEvent { HoldId = holdId, MunicipalityId = 7, PoeFileId = "poe-1", Action = PoeLegalHoldAction.Released, HoldReference = placed.HoldReference, Reason = "Matter concluded", ActorUserId = "legal", CorrelationId = "trace-release" };
        PoeLegalHoldPolicy.IsActive([placed, released], holdId).Should().BeFalse();
        PoeLegalHoldPolicy.HasAnyActiveHold([placed, released]).Should().BeFalse();

        await using var context = IdpTestFixture.CreateContext();
        context.PoeFiles.Add(new PoeFile
        {
            Id = placed.PoeFileId, MunicipalityId = placed.MunicipalityId, SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "submission-1", FileName = "held.pdf", UploadedByUserId = placed.ActorUserId
        });
        context.PoeLegalHoldEvents.Add(placed);
        await context.SaveChangesAsync();
        placed.Reason = "Mutation attempt";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task PoeDisposal_RequiresExpiredInactiveUnheldEvidence_AndLedgerIsAppendOnly()
    {
        var now = DateTime.UtcNow;
        var evidence = new PoeFile { Id = "poe-retired", IsActive = true, RetainUntil = now.AddDays(-1) };
        PoeDisposalPolicy.Validate(evidence, "COUNCIL-2026-42", "Retention period completed", now).Allowed.Should().BeFalse();
        evidence.IsActive = false; evidence.RetainUntil = now.AddDays(1);
        PoeDisposalPolicy.Validate(evidence, "COUNCIL-2026-42", "Retention period completed", now).Allowed.Should().BeFalse();
        evidence.RetainUntil = now.AddDays(-1);
        var holdId = Guid.NewGuid();
        evidence.LegalHoldEvents.Add(new PoeLegalHoldEvent { HoldId = holdId, Action = PoeLegalHoldAction.Placed });
        PoeDisposalPolicy.Validate(evidence, "COUNCIL-2026-42", "Retention period completed", now).Allowed.Should().BeFalse();
        evidence.LegalHoldEvents.Add(new PoeLegalHoldEvent { HoldId = holdId, Action = PoeLegalHoldAction.Released });
        PoeDisposalPolicy.Validate(evidence, "COUNCIL-2026-42", "Retention period completed", now).Allowed.Should().BeTrue();
        var priorDisposalId = Guid.NewGuid();
        evidence.DisposalEvents.Add(new PoeDisposalEvent { DisposalId = priorDisposalId, Action = PoeDisposalAction.Requested });
        PoeDisposalPolicy.Validate(evidence, "COUNCIL-2026-42", "Retention period completed", now).Allowed.Should().BeFalse();
        evidence.DisposalEvents.Add(new PoeDisposalEvent { DisposalId = priorDisposalId, Action = PoeDisposalAction.Failed });
        PoeDisposalPolicy.Validate(evidence, "COUNCIL-2026-42", "Retention period completed", now).Allowed.Should().BeTrue();

        await using var context = IdpTestFixture.CreateContext();
        var disposal = new PoeDisposalEvent { DisposalId = Guid.NewGuid(), MunicipalityId = 7, PoeFileId = evidence.Id, Action = PoeDisposalAction.Requested, ApprovalReference = "COUNCIL-2026-42", Reason = "Retention period completed", ActorUserId = "records-officer", CorrelationId = "trace-disposal" };
        context.PoeFiles.Add(new PoeFile
        {
            Id = disposal.PoeFileId, MunicipalityId = disposal.MunicipalityId, SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "submission-1", FileName = "disposed.pdf", UploadedByUserId = disposal.ActorUserId,
            IsActive = false, RetainUntil = now.AddDays(-1)
        });
        context.PoeDisposalEvents.Add(disposal);
        await context.SaveChangesAsync();
        disposal.Reason = "Mutation attempt";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task PoeEvidenceStorage_RejectsTraversal_AndDeletesIdempotently()
    {
        var root = Path.Combine(Path.GetTempPath(), "poe-storage-" + Guid.NewGuid().ToString("N"));
        try
        {
            var secure = Path.Combine(root, "secure-files", "poe", "opms", "submission-1");
            Directory.CreateDirectory(secure);
            var path = Path.Combine(secure, "proof.pdf");
            await File.WriteAllTextAsync(path, "test evidence");
            var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            environment.SetupGet(item => item.ContentRootPath).Returns(root);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["EvidenceStorage:LocalRoot"] = "secure-files" })
                .Build();
            var storage = new FileSystemEvidenceBlobStorage(environment.Object, configuration);

            (await storage.StoreAsync(Path.Combine("poe", "opms", "submission-1", "proof-2.pdf"), "stored evidence"u8.ToArray(), CancellationToken.None)).Succeeded.Should().BeTrue();
            (await storage.ReadAsync(Path.Combine("poe", "opms", "submission-1", "proof-2.pdf"), CancellationToken.None)).Content.Should().Equal("stored evidence"u8.ToArray());
            (await storage.DisposeAsync(Path.Combine("poe", "opms", "submission-1", "proof.pdf"), CancellationToken.None)).Succeeded.Should().BeTrue();
            File.Exists(path).Should().BeFalse();
            (await storage.DisposeAsync(Path.Combine("poe", "opms", "submission-1", "proof.pdf"), CancellationToken.None)).Succeeded.Should().BeTrue();
            (await storage.DisposeAsync(Path.Combine("..", "outside.pdf"), CancellationToken.None)).Succeeded.Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}

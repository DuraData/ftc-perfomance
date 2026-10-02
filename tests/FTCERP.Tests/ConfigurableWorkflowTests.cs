using FTCERP.Host.Application.Services;
using Microsoft.Extensions.Configuration;

namespace FTCERP.Tests;

public sealed class ConfigurableWorkflowTests
{
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
    public async Task ReportingWindow_UsesScopedExtensionWithoutOpeningForOtherUsers()
    {
        await using var context = IdpTestFixture.CreateContext();
        var now = DateTime.UtcNow;
        var window = new ReportingWindow { Id = 1, MunicipalityId = 7, ReportingPeriodId = 10, SubmissionKind = SubmissionKind.Opms, OpensAt = now.AddDays(-10), ClosesAt = now.AddDays(-1) };
        context.ReportingWindows.Add(window);
        context.ReportingWindowExceptions.Add(new ReportingWindowException { MunicipalityId = 7, ReportingWindowId = 1, UserId = "allowed-user", ExtendedClosesAt = now.AddDays(2), Reason = "Approved extension", ApprovedByUserId = "manager" });
        await context.SaveChangesAsync();
        var service = new ReportingWindowService(context);

        (await service.CheckAsync(SubmissionKind.Opms, 10, "allowed-user", null, null, now)).Allowed.Should().BeTrue();
        (await service.CheckAsync(SubmissionKind.Opms, 10, "other-user", null, null, now)).Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task RfiEvidenceProvenance_IsAppendOnly()
    {
        await using var context = IdpTestFixture.CreateContext();
        var link = new PerformanceRfiEvidence
        {
            MunicipalityId = 7,
            PerformanceRfiId = 42,
            PoeFileId = "poe-1",
            Purpose = RfiEvidencePurpose.Response,
            LinkedByUserId = "responder",
            CorrelationId = "trace-rfi-evidence"
        };
        context.PerformanceRfiEvidenceLinks.Add(link);
        await context.SaveChangesAsync();

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

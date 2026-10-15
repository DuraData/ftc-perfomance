namespace FTCERP.Tests;

public sealed class LegacyRatingReconciliationControllerTests
{
    [Fact]
    public async Task Preview_and_execute_do_not_cross_the_selected_municipality_boundary()
    {
        long? selectedMunicipalityId = null;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(value => value.MunicipalityId).Returns(() => selectedMunicipalityId);
        tenant.SetupGet(value => value.UserId).Returns("rating-admin");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var setup = await Seed(context, onMunicipalityCreated: id => selectedMunicipalityId = id);
        selectedMunicipalityId = null;
        var otherMunicipality = new Municipality { Code = "RATE-OTHER", Name = "Other Rating Municipality" };
        context.Municipalities.Add(otherMunicipality);
        await context.SaveChangesAsync();
        selectedMunicipalityId = otherMunicipality.Id;
        var otherActor = IdpTestFixture.CreateUser("other-tenant-verifier");
        otherActor.MunicipalityId = otherMunicipality.Id;
        var otherTarget = new OpmsTarget
        {
            MunicipalityId = otherMunicipality.Id,
            AssignedUserId = otherActor.Id,
            IndicatorNumber = "OTHER-RATING-KPI",
            TargetName = "Other tenant target",
            KpiDescription = "Other tenant target",
            AnnualTargetDescription = "Target",
            PerformanceObjective = "Objective"
        };
        context.AddRange(otherActor, otherTarget);
        await context.SaveChangesAsync();
        var otherSubmission = new OpmsSubmission
        {
            MunicipalityId = otherMunicipality.Id,
            OpmsTargetId = otherTarget.Id,
            Quarter = "Q1",
            BaseState = SubmissionBaseStates.Submitted,
            Status = "verified",
            SubmittedByUserId = otherActor.Id,
            SubmittedAt = DateTime.UtcNow,
            VerifierScore = 4m,
            VerifierUserId = otherActor.Id,
            VerifiedAt = DateTime.UtcNow
        };
        context.OpmsSubmissions.Add(otherSubmission);
        await context.SaveChangesAsync();
        selectedMunicipalityId = setup.Municipality.Id;
        var controller = Controller(context, setup, tenant.Object);

        var preview = await controller.Preview(SubmissionKind.Opms, LegacyRatingSourceKind.VerifierScore, 1, 25);
        var page = preview.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<LegacyRatingReconciliationPreviewDto>>>().Subject.Data!;
        page.TotalCount.Should().Be(1);
        page.Items.Should().OnlyContain(item => item.SubmissionPublicId == setup.Submission.PublicId);

        var crossTenant = await controller.Execute(new(
            SubmissionKind.Opms,
            LegacyRatingSourceKind.VerifierScore,
            $"OPMS:{otherSubmission.PublicId:N}:{LegacyRatingSourceKind.VerifierScore}",
            setup.Action.PublicId,
            "Attempt a forbidden cross-municipality mapping."));
        crossTenant.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Preview_requires_an_explicit_exact_same_actor_workflow_candidate()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context, includeConflictingAction: true);
        var controller = Controller(context, setup);

        var response = await controller.Preview(
            SubmissionKind.Opms, LegacyRatingSourceKind.VerifierScore, page: 1, pageSize: 25);

        var page = response.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<LegacyRatingReconciliationPreviewDto>>>().Subject.Data!;
        page.TotalCount.Should().Be(1);
        var preview = page.Items.Should().ContainSingle().Subject;
        preview.Status.Should().Be("Ready");
        preview.SourceScore.Should().Be(4m);
        preview.Candidates.Should().ContainSingle()
            .Which.WorkflowActionPublicId.Should().Be(setup.Action.PublicId);
        preview.Candidates.Should().NotContain(candidate => candidate.WorkflowActionPublicId == setup.ConflictingAction!.PublicId);
        (await context.LegacySubmissionRatingReconciliations.CountAsync()).Should().Be(0);
        (await context.SubmissionStageRatings.CountAsync()).Should().Be(0);

        var missingSelection = await controller.Execute(new(
            SubmissionKind.Opms,
            LegacyRatingSourceKind.VerifierScore,
            preview.SourceKey,
            Guid.Empty,
            "No workflow action has been selected for mapping."));
        missingSelection.Result.Should().BeOfType<BadRequestObjectResult>();
        (await context.LegacySubmissionRatingReconciliations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Execute_creates_canonical_rating_append_only_provenance_and_atomic_audit_without_mutating_legacy_source()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context);
        var controller = Controller(context, setup);
        var sourceKey = $"OPMS:{setup.Submission.PublicId:N}:{LegacyRatingSourceKind.VerifierScore}";

        var response = await controller.Execute(new(
            SubmissionKind.Opms,
            LegacyRatingSourceKind.VerifierScore,
            sourceKey,
            setup.Action.PublicId,
            "Preserve and reconcile verified historical evidence."));

        var result = response.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<LegacyRatingReconciliationResultDto>>().Subject.Data!;
        result.UsedExistingRating.Should().BeFalse();

        context.ChangeTracker.Clear();
        var rating = await context.SubmissionStageRatings.SingleAsync();
        rating.PublicId.Should().Be(result.StageRatingPublicId);
        rating.Value.Should().Be(4m);
        rating.RatingSchemeValueId.Should().Be(setup.RatingValue.Id);
        rating.SubmissionWorkflowActionId.Should().Be(setup.Action.Id);
        rating.AchievementPercent.Should().BeNull("historical achievement must not be invented");
        rating.Comment.Should().Be("Historical verifier comment");
        rating.RatedByUserId.Should().Be(setup.Actor.Id);
        rating.RatedAt.Should().Be(setup.Action.OccurredAt);

        var reconciliation = await context.LegacySubmissionRatingReconciliations.SingleAsync();
        reconciliation.PublicId.Should().Be(result.ReconciliationPublicId);
        reconciliation.SourceScore.Should().Be(4m);
        reconciliation.SourceActorUserIdSnapshot.Should().Be(setup.Actor.Id);
        reconciliation.SubmissionWorkflowActionId.Should().Be(setup.Action.Id);
        reconciliation.SubmissionStageRatingId.Should().Be(rating.Id);
        reconciliation.UsedExistingRating.Should().BeFalse();
        (await context.AuditTrails.SingleAsync(item => item.EntityName == nameof(LegacySubmissionRatingReconciliation)))
            .Action.Should().Be("Reconcile");
        (await context.OpmsSubmissions.SingleAsync()).VerifierScore.Should().Be(4m);

        reconciliation.Reason = "Attempted rewrite of immutable provenance";
        var update = async () => await context.SaveChangesAsync();
        await update.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*rating reconciliation evidence is append-only*");
    }

    [Fact]
    public async Task Execute_links_matching_existing_rating_and_rejects_duplicate_or_wrong_actor_mapping()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await Seed(context, includeWrongActorAction: true);
        context.ChangeTracker.Clear();
        var existingPublicId = Guid.NewGuid();
        var generic = new SubmissionScore
        {
            MunicipalityId = setup.Municipality.Id,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = setup.Submission.Id,
            Score = 4m,
            Notes = "Historical generic score",
            ScoredByUserId = setup.Actor.Id,
            ScoredAt = setup.Action.OccurredAt
        };
        new WorkflowGovernanceService(context,
                tenantContext: IdpTestFixture.Tenant(setup.Municipality.Id, setup.Actor.Id))
            .QueueAuditTrail(nameof(OpmsSubmission), setup.Submission.Id, "Score", null,
                new { generic.Score, generic.Notes }, setup.Actor.Id, "127.0.0.1", "Seed preserved historical score evidence.");
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO SubmissionStageRatings
                (PublicId, MunicipalityId, SubmissionWorkflowInstanceId, SubmissionWorkflowActionId,
                 WorkflowStageDefinitionId, RatingSchemeId, RatingSchemeValueId, Value, LabelSnapshot,
                 AchievementPercent, Comment, RatedByUserId, RatedAt, CorrelationId)
            VALUES ({existingPublicId}, {setup.Municipality.Id}, {setup.Instance.Id}, {setup.Action.Id},
                 {setup.Stage.Id}, {setup.Scheme.Id}, {setup.RatingValue.Id}, {4m}, {setup.RatingValue.Label},
                 NULL, NULL, {setup.Actor.Id}, {setup.Action.OccurredAt}, {"historic-existing-rating"})");
        var existing = await context.SubmissionStageRatings.SingleAsync(item => item.PublicId == existingPublicId);
        var submissionKind = (int)generic.SubmissionKind;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO SubmissionScores
                (Id, MunicipalityId, SubmissionKind, SubmissionId, Score, Notes, ScoredByUserId, ScoredAt)
            VALUES
                ({generic.Id}, {generic.MunicipalityId}, {submissionKind}, {generic.SubmissionId}, {generic.Score},
                 {generic.Notes}, {generic.ScoredByUserId}, {generic.ScoredAt})
            """);
        var controller = Controller(context, setup);
        var reason = "Link the exact pre-existing canonical rating evidence.";

        var wrongActor = await controller.Execute(new(
            SubmissionKind.Opms, LegacyRatingSourceKind.GenericSubmissionScore,
            $"GENERIC:{generic.Id}", setup.WrongActorAction!.PublicId, reason));
        wrongActor.Result.Should().BeOfType<ConflictObjectResult>();
        (await context.LegacySubmissionRatingReconciliations.CountAsync()).Should().Be(0);

        var linked = await controller.Execute(new(
            SubmissionKind.Opms, LegacyRatingSourceKind.GenericSubmissionScore,
            $"GENERIC:{generic.Id}", setup.Action.PublicId, reason));
        if (linked.Result is ConflictObjectResult { Value: ApiResponse<LegacyRatingReconciliationResultDto> failure })
            Assert.Fail(failure.Message ?? "Reconciliation unexpectedly conflicted.");
        var data = linked.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<LegacyRatingReconciliationResultDto>>().Subject.Data!;
        data.UsedExistingRating.Should().BeTrue();
        data.StageRatingPublicId.Should().Be(existing.PublicId);
        (await context.SubmissionStageRatings.CountAsync()).Should().Be(1);

        var duplicate = await controller.Execute(new(
            SubmissionKind.Opms, LegacyRatingSourceKind.GenericSubmissionScore,
            $"GENERIC:{generic.Id}", setup.Action.PublicId, reason));
        duplicate.Result.Should().BeOfType<ConflictObjectResult>();
    }

    private static LegacyRatingReconciliationController Controller(
        ApplicationDbContext context,
        Setup setup,
        ITenantContext? configuredTenant = null)
    {
        var tenant = configuredTenant ?? IdpTestFixture.Tenant(setup.Municipality.Id, setup.Administrator.Id);
        var users = IdpTestFixture.CreateUserManagerMock(setup.Administrator);
        users.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(setup.Administrator);
        return new LegacyRatingReconciliationController(
            context,
            tenant,
            users.Object,
            new WorkflowGovernanceService(context, tenantContext: tenant))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = IdpTestFixture.CreatePrincipal(setup.Administrator.Id),
                    TraceIdentifier = "rating-reconciliation-test"
                }
            }
        };
    }

    private static async Task<Setup> Seed(
        ApplicationDbContext context,
        bool includeConflictingAction = false,
        bool includeWrongActorAction = false,
        Action<long>? onMunicipalityCreated = null)
    {
        var municipality = new Municipality { Code = $"RATE-{Guid.NewGuid():N}"[..12], Name = "Rating Reconciliation Municipality" };
        var administrator = IdpTestFixture.CreateUser("rating-admin");
        var actor = IdpTestFixture.CreateUser("historical-verifier", "Historical", "Verifier");
        var otherActor = IdpTestFixture.CreateUser("unrelated-reviewer", "Unrelated", "Reviewer");
        var financialYear = new FinancialYear
        {
            Code = "2024/25", Name = "2024/25", StartDate = new(2024, 7, 1), EndDate = new(2025, 6, 30)
        };
        administrator.Municipality = municipality;
        actor.Municipality = municipality;
        otherActor.Municipality = municipality;
        context.AddRange(municipality, administrator, actor, otherActor, financialYear);
        await context.SaveChangesAsync();
        onMunicipalityCreated?.Invoke(municipality.Id);

        var year = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id,
            FinancialYearId = financialYear.Id,
            FinancialYear = financialYear,
            EffectiveFrom = financialYear.StartDate
        };
        var target = new OpmsTarget
        {
            MunicipalityId = municipality.Id,
            AssignedUserId = actor.Id,
            IndicatorNumber = "LEGACY-RATING-KPI",
            TargetName = "Legacy rating target",
            KpiDescription = "Legacy rating target",
            AnnualTargetDescription = "Target",
            PerformanceObjective = "Objective"
        };
        var scheme = new RatingScheme { MunicipalityId = municipality.Id, Code = "LEGACY-FIVE", Name = "Legacy five point" };
        var ratingValue = new RatingSchemeValue
        {
            MunicipalityId = municipality.Id,
            RatingScheme = scheme,
            Value = 4m,
            Label = "Achieved",
            SortOrder = 4
        };
        context.AddRange(year, target, scheme, ratingValue);
        await context.SaveChangesAsync();

        var submission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id,
            OpmsTargetId = target.Id,
            OpmsTarget = target,
            Quarter = "Q1",
            BaseState = SubmissionBaseStates.Submitted,
            Status = "verified",
            SubmittedByUserId = actor.Id,
            SubmittedAt = new DateTime(2024, 8, 1, 8, 0, 0, DateTimeKind.Utc),
            VerifierScore = 4m,
            VerifierUserId = actor.Id,
            VerifiedAt = new DateTime(2024, 8, 2, 9, 30, 0, DateTimeKind.Utc),
            VerifierComments = "Historical verifier comment"
        };
        var workflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id,
            MunicipalityFinancialYearId = year.Id,
            MunicipalityFinancialYear = year,
            SubmissionKind = SubmissionKind.Opms,
            Code = "LEGACY-RATING",
            Name = "Legacy rating reconciliation",
            EffectiveFrom = financialYear.StartDate
        };
        var stage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id,
            WorkflowDefinition = workflow,
            Code = "VERIFY",
            Name = "Verify",
            Sequence = 1,
            RequiredActionCode = "OPMS_SUBMISSION.VERIFY",
            RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY",
            RequiresRating = true,
            RatingScheme = scheme,
            IsTerminal = true
        };
        context.AddRange(submission, workflow, stage);
        await context.SaveChangesAsync();

        var instance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id,
            WorkflowDefinitionId = workflow.Id,
            WorkflowDefinition = workflow,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = submission.Id,
            CurrentStageId = stage.Id,
            CurrentStage = stage
        };
        context.SubmissionWorkflowInstances.Add(instance);
        await context.SaveChangesAsync();
        var occurredAt = submission.VerifiedAt!.Value;
        WorkflowStageDefinition? otherStage = null;
        if (includeConflictingAction)
        {
            var otherScheme = new RatingScheme { MunicipalityId = municipality.Id, Code = "OTHER", Name = "Other scheme" };
            otherScheme.Values.Add(new RatingSchemeValue { MunicipalityId = municipality.Id, Value = 3m, Label = "Other", SortOrder = 1 });
            otherStage = new WorkflowStageDefinition
            {
                MunicipalityId = municipality.Id,
                WorkflowDefinition = workflow,
                Code = "OTHER",
                Name = "Other",
                Sequence = 2,
                RequiredActionCode = "OPMS_SUBMISSION.APPROVE",
                RequiredPermissionCode = "OPMS_SUBMISSION.APPROVE",
                RequiresRating = true,
                RatingScheme = otherScheme
            };
            context.AddRange(otherScheme, otherStage);
            await context.SaveChangesAsync();
        }

        var actionPublicId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO SubmissionWorkflowActions
                (PublicId, MunicipalityId, SubmissionWorkflowInstanceId, Sequence, FromStageId, ToStageId,
                 ActionCode, Outcome, ActorUserId, Comment, RatingValue, CorrelationId, OccurredAt)
            VALUES ({actionPublicId}, {municipality.Id}, {instance.Id}, 1, {stage.Id}, NULL,
                 {"VERIFY"}, {(int)WorkflowActionOutcome.Approve}, {actor.Id}, {submission.VerifierComments}, NULL,
                 {"historic-legacy-rating"}, {occurredAt})");
        Guid? conflictingPublicId = null;
        if (otherStage != null)
        {
            conflictingPublicId = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO SubmissionWorkflowActions
                    (PublicId, MunicipalityId, SubmissionWorkflowInstanceId, Sequence, FromStageId, ToStageId,
                     ActionCode, Outcome, ActorUserId, Comment, RatingValue, CorrelationId, OccurredAt)
                VALUES ({conflictingPublicId.Value}, {municipality.Id}, {instance.Id}, 2, {otherStage.Id}, NULL,
                     {"OTHER"}, {(int)WorkflowActionOutcome.Approve}, {actor.Id}, NULL, NULL,
                     {"historic-conflicting-rating"}, {occurredAt.AddMinutes(1)})");
        }
        Guid? wrongActorPublicId = null;
        if (includeWrongActorAction)
        {
            wrongActorPublicId = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO SubmissionWorkflowActions
                    (PublicId, MunicipalityId, SubmissionWorkflowInstanceId, Sequence, FromStageId, ToStageId,
                     ActionCode, Outcome, ActorUserId, Comment, RatingValue, CorrelationId, OccurredAt)
                VALUES ({wrongActorPublicId.Value}, {municipality.Id}, {instance.Id}, 3, {stage.Id}, NULL,
                     {"VERIFY-WRONG-ACTOR"}, {(int)WorkflowActionOutcome.Approve}, {otherActor.Id}, NULL, NULL,
                     {"historic-wrong-actor-rating"}, {occurredAt.AddMinutes(2)})");
        }
        var action = await context.SubmissionWorkflowActions.SingleAsync(item => item.PublicId == actionPublicId);
        var conflictingAction = conflictingPublicId.HasValue
            ? await context.SubmissionWorkflowActions.SingleAsync(item => item.PublicId == conflictingPublicId.Value) : null;
        var wrongActorAction = wrongActorPublicId.HasValue
            ? await context.SubmissionWorkflowActions.SingleAsync(item => item.PublicId == wrongActorPublicId.Value) : null;
        return new(municipality, administrator, actor, submission, workflow, stage, scheme, ratingValue,
            instance, action, conflictingAction, wrongActorAction);
    }

    private sealed record Setup(
        Municipality Municipality,
        ApplicationUser Administrator,
        ApplicationUser Actor,
        OpmsSubmission Submission,
        WorkflowDefinition Workflow,
        WorkflowStageDefinition Stage,
        RatingScheme Scheme,
        RatingSchemeValue RatingValue,
        SubmissionWorkflowInstance Instance,
        SubmissionWorkflowAction Action,
        SubmissionWorkflowAction? ConflictingAction,
        SubmissionWorkflowAction? WrongActorAction);
}

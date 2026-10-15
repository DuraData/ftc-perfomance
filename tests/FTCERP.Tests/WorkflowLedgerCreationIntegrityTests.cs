namespace FTCERP.Tests;

public sealed class WorkflowLedgerCreationIntegrityTests
{
    [Fact]
    public async Task Canonical_action_and_stage_rating_are_appended_with_the_parent_sequence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "valid");
        var action = NewAction(fixture);
        var rating = NewRating(fixture, action);
        fixture.Instance.NextSequence++;
        context.AddRange(action, rating);

        await context.SaveChangesAsync();

        (await context.SubmissionWorkflowActions.SingleAsync()).PublicId.Should().Be(action.PublicId);
        (await context.SubmissionStageRatings.SingleAsync()).PublicId.Should().Be(rating.PublicId);
    }

    [Fact]
    public async Task Cross_tenant_action_actor_is_rejected_at_the_persistence_boundary()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "foreign-actor");
        var action = NewAction(fixture);
        action.ActorUserId = fixture.ForeignActor.Id;
        action.ActorUser = fixture.ForeignActor;
        action.RatingValue = null;
        fixture.Instance.NextSequence++;
        context.Add(action);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Workflow action creation requires an exact tracked lifecycle advance, canonical actor, tenant, stage, sequence, timestamp and correlation identity.*");
    }

    [Fact]
    public async Task Action_rating_value_without_one_canonical_rating_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "missing-rating");
        var action = NewAction(fixture);
        fixture.Instance.NextSequence++;
        context.Add(action);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A workflow action rating value requires exactly one canonical stage-rating record in the same unit of work.*");
    }

    [Theory]
    [InlineData("label")]
    [InlineData("actor")]
    [InlineData("time")]
    public async Task Counterfeit_stage_rating_snapshot_actor_or_time_is_rejected(string defect)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, defect);
        var action = NewAction(fixture);
        var rating = NewRating(fixture, action);
        if (defect == "label") rating.LabelSnapshot = "Counterfeit label";
        if (defect == "actor")
        {
            rating.RatedByUserId = fixture.ForeignActor.Id;
            rating.RatedByUser = fixture.ForeignActor;
        }
        if (defect == "time") rating.RatedAt = action.OccurredAt.AddSeconds(1);
        fixture.Instance.NextSequence++;
        context.AddRange(action, rating);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Stage-rating creation requires one exact canonical workflow action, stage, scheme, value, actor, tenant and immutable snapshot.*");
    }

    [Fact]
    public async Task Legacy_reconciliation_cannot_forge_the_preserved_source_snapshot()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "forged-source");
        var action = NewAction(fixture);
        var rating = NewRating(fixture, action);
        fixture.Instance.NextSequence++;
        context.AddRange(action, rating);
        await context.SaveChangesAsync();

        var sourceId = Guid.NewGuid().ToString("N");
        var scoredAt = action.OccurredAt.AddMinutes(-1);
        var submissionKind = (int)SubmissionKind.Opms;
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO SubmissionScores
                (Id, MunicipalityId, SubmissionKind, SubmissionId, Score, Notes, ScoredByUserId, ScoredAt)
            VALUES ({sourceId}, {fixture.Municipality.Id}, {submissionKind}, {fixture.Instance.SubmissionId},
                {fixture.Value.Value}, {"Preserved source comment"}, {fixture.Actor.Id}, {scoredAt})");

        var reconciledAt = DateTime.UtcNow;
        var reconciliation = new LegacySubmissionRatingReconciliation
        {
            MunicipalityId = fixture.Municipality.Id,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = fixture.Instance.SubmissionId,
            SourceKind = LegacyRatingSourceKind.GenericSubmissionScore,
            SourceKey = $"GENERIC:{sourceId}",
            SourceScore = fixture.Value.Value,
            SourceActorUserIdSnapshot = fixture.Actor.Id,
            SourceOccurredAtSnapshot = scoredAt,
            SourceCommentSnapshot = "Counterfeit source comment",
            SubmissionWorkflowInstanceId = fixture.Instance.Id,
            SubmissionWorkflowInstance = fixture.Instance,
            SubmissionWorkflowActionId = action.Id,
            SubmissionWorkflowAction = action,
            WorkflowStageDefinitionId = fixture.Stage.Id,
            WorkflowStageDefinition = fixture.Stage,
            RatingSchemeId = fixture.Scheme.Id,
            RatingScheme = fixture.Scheme,
            RatingSchemeValueId = fixture.Value.Id,
            RatingSchemeValue = fixture.Value,
            SubmissionStageRatingId = rating.Id,
            SubmissionStageRating = rating,
            UsedExistingRating = true,
            ReconciledByUserId = fixture.Actor.Id,
            ReconciledByUser = fixture.Actor,
            ReconciledAt = reconciledAt,
            Reason = "Attempt to forge source provenance.",
            CorrelationId = "forged-source-reconciliation"
        };
        context.LegacySubmissionRatingReconciliations.Add(reconciliation);
        new WorkflowGovernanceService(context, tenantContext: IdpTestFixture.Tenant(fixture.Municipality.Id, fixture.Actor.Id))
            .QueueAuditTrailAt(nameof(LegacySubmissionRatingReconciliation), reconciliation.PublicId.ToString(), "Reconcile",
                null, new { reconciliation.SourceKey }, fixture.Actor.Id, "127.0.0.1", reconciliation.Reason, reconciledAt);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Legacy rating reconciliation creation requires exact canonical source, workflow, rating, actor, tenant and provenance relationships.*");
    }

    private static SubmissionWorkflowAction NewAction(Fixture fixture)
    {
        var occurredAt = DateTime.UtcNow;
        return new SubmissionWorkflowAction
        {
            MunicipalityId = fixture.Municipality.Id,
            SubmissionWorkflowInstanceId = fixture.Instance.Id,
            SubmissionWorkflowInstance = fixture.Instance,
            Sequence = fixture.Instance.NextSequence,
            FromStageId = fixture.Stage.Id,
            FromStage = fixture.Stage,
            ToStageId = fixture.Stage.Id,
            ToStage = fixture.Stage,
            ActionCode = "VERIFY",
            Outcome = WorkflowActionOutcome.Approve,
            ActorUserId = fixture.Actor.Id,
            ActorUser = fixture.Actor,
            RatingValue = fixture.Value.Value,
            CorrelationId = "workflow-ledger-test",
            OccurredAt = occurredAt
        };
    }

    private static SubmissionStageRating NewRating(Fixture fixture, SubmissionWorkflowAction action) => new()
    {
        MunicipalityId = fixture.Municipality.Id,
        SubmissionWorkflowInstanceId = fixture.Instance.Id,
        SubmissionWorkflowInstance = fixture.Instance,
        SubmissionWorkflowAction = action,
        WorkflowStageDefinitionId = fixture.Stage.Id,
        WorkflowStageDefinition = fixture.Stage,
        RatingSchemeId = fixture.Scheme.Id,
        RatingScheme = fixture.Scheme,
        RatingSchemeValueId = fixture.Value.Id,
        RatingSchemeValue = fixture.Value,
        Value = fixture.Value.Value,
        LabelSnapshot = fixture.Value.Label,
        RatedByUserId = fixture.Actor.Id,
        RatedByUser = fixture.Actor,
        RatedAt = action.OccurredAt,
        CorrelationId = action.CorrelationId
    };

    private static async Task<Fixture> SeedAsync(ApplicationDbContext context, string suffix)
    {
        var municipality = new Municipality { Code = $"WF-{suffix}", Name = $"Workflow {suffix}" };
        var foreignMunicipality = new Municipality { Code = $"WF-X-{suffix}", Name = $"Foreign workflow {suffix}" };
        var actor = IdpTestFixture.CreateUser($"workflow-actor-{suffix}");
        actor.Municipality = municipality;
        var foreignActor = IdpTestFixture.CreateUser($"foreign-workflow-actor-{suffix}");
        foreignActor.Municipality = foreignMunicipality;
        var financialYear = new FinancialYear
        {
            Code = $"40{suffix.Length:D2}/41{suffix.Length:D2}", Name = $"Workflow year {suffix}",
            StartDate = new DateTime(2040, 7, 1), EndDate = new DateTime(2041, 6, 30)
        };
        var year = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, EffectiveFrom = financialYear.StartDate
        };
        var scheme = new RatingScheme { Municipality = municipality, Code = $"S-{suffix}", Name = "Governed rating" };
        var value = new RatingSchemeValue
        {
            Municipality = municipality, RatingScheme = scheme, Value = 4m, Label = "Achieved", SortOrder = 1
        };
        var definition = new WorkflowDefinition
        {
            Municipality = municipality, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms,
            Code = $"D-{suffix}", Name = "Governed workflow", EffectiveFrom = financialYear.StartDate
        };
        var stage = new WorkflowStageDefinition
        {
            Municipality = municipality, WorkflowDefinition = definition, Code = "VERIFY", Name = "Verify", Sequence = 1,
            RequiredActionCode = "OPMS_SUBMISSION.VERIFY", RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY",
            RequiresRating = true, RatingScheme = scheme
        };
        var instance = new SubmissionWorkflowInstance
        {
            Municipality = municipality, WorkflowDefinition = definition, CurrentStage = stage,
            SubmissionKind = SubmissionKind.Opms, SubmissionId = $"submission-{suffix}"
        };
        context.AddRange(municipality, foreignMunicipality, actor, foreignActor, financialYear, year, scheme, value, definition, stage, instance);
        await context.SaveChangesAsync();
        return new(municipality, actor, foreignActor, scheme, value, stage, instance);
    }

    private sealed record Fixture(
        Municipality Municipality,
        ApplicationUser Actor,
        ApplicationUser ForeignActor,
        RatingScheme Scheme,
        RatingSchemeValue Value,
        WorkflowStageDefinition Stage,
        SubmissionWorkflowInstance Instance);
}

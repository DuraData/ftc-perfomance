using System.Text.Json;

namespace FTCERP.Tests;

public sealed class PerformanceSubmissionRootHistoryTests
{
    [Fact]
    public async Task Submission_history_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("delete");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        context.OpmsSubmissions.Remove(fixture.Submission);

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-submission history cannot be hard deleted; withdraw the submission through its governed lifecycle instead.*");
    }

    [Fact]
    public async Task Submission_identity_target_and_creation_evidence_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("identity");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        fixture.Submission.Quarter = "FORGED";

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-submission identity, tenant, target, legacy period label and creation evidence are immutable after creation.*");
    }

    [Fact]
    public async Task Draft_value_change_requires_same_transaction_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("audit-required");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        fixture.Submission.ActualPerformance = "42";
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = DateTime.UtcNow;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsSubmission Edit requires one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Exact_draft_value_audit_allows_the_governed_change()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("audited-edit");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        var updatedOn = DateTime.UtcNow;
        fixture.Submission.ActualPerformance = "42";
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = updatedOn;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = fixture.Municipality.Id,
            EntityName = nameof(OpmsSubmission),
            EntityId = fixture.Submission.Id,
            Action = "Edit",
            OldValue = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                [nameof(OpmsSubmission.PublicId)] = fixture.Submission.PublicId,
                [nameof(OpmsSubmission.ActualPerformance)] = null,
                ["UpdatedByUserPublicId"] = null,
                [nameof(OpmsSubmission.UpdatedOn)] = null
            }),
            NewValue = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                [nameof(OpmsSubmission.PublicId)] = fixture.Submission.PublicId,
                [nameof(OpmsSubmission.ActualPerformance)] = "42",
                ["UpdatedByUserPublicId"] = fixture.User.PublicId,
                [nameof(OpmsSubmission.UpdatedOn)] = updatedOn
            }),
            ChangedBy = fixture.User.Id,
            Reason = "Governed draft correction"
        });

        await context.SaveChangesAsync();
        fixture.Submission.ActualPerformance.Should().Be("42");
    }

    [Fact]
    public async Task Forged_draft_value_snapshot_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("forged-edit");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        var updatedOn = DateTime.UtcNow;
        fixture.Submission.ActualPerformance = "42";
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = updatedOn;
        context.AuditTrails.Add(EditAudit(fixture, fixture.User.Id, updatedOn, "99"));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsSubmission Edit requires one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Forged_but_persisted_submission_actor_is_rejected_for_an_authenticated_tenant()
    {
        const long municipalityId = 982101;
        var expectedActor = IdpTestFixture.CreateUser("submission-expected-actor");
        var forgedActor = IdpTestFixture.CreateUser("submission-forged-actor");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityId, expectedActor.Id));
        var municipality = new Municipality { Id = municipalityId, Code = "SUB-ACTOR", Name = "Submission actor municipality" };
        expectedActor.Municipality = municipality;
        forgedActor.Municipality = municipality;
        var target = new OpmsTarget
        {
            Id = "submission-actor-target", Municipality = municipality, IndicatorNumber = "KPI-1",
            TargetName = "Governed target", KpiDescription = "Governed wording", CreatedAt = DateTime.UtcNow
        };
        var submission = new OpmsSubmission
        {
            Id = "submission-actor-bound", Municipality = municipality, OpmsTarget = target, Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress, Status = SubmissionBaseStates.InProgress,
            SubmittedByUser = expectedActor, CreatedBy = expectedActor.Id, CreatedOn = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        var fixture = new SubmissionFixture(municipality, expectedActor, target, submission);
        context.AddRange(municipality, expectedActor, forgedActor, target, submission);
        await context.SaveChangesAsync();

        var updatedOn = DateTime.UtcNow;
        submission.ActualPerformance = "42";
        submission.UpdatedBy = expectedActor.Id;
        submission.UpdatedOn = updatedOn;
        context.AuditTrails.Add(EditAudit(fixture, forgedActor.Id, updatedOn, "42"));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsSubmission Edit requires one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Due_date_extension_rejects_audit_reason_or_snapshot_not_bound_to_the_ledger()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("forged-extension");
        var originalDueDate = DateTime.UtcNow.Date.AddDays(5);
        fixture.Submission.DueDate = originalDueDate;
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        var updatedOn = DateTime.UtcNow;
        var extendedDueDate = originalDueDate.AddDays(3);
        fixture.Submission.DueDate = extendedDueDate;
        fixture.Submission.ExtendedDueDate = extendedDueDate;
        fixture.Submission.DueDateExtendedDays = 3;
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = updatedOn;
        context.DueDateExtensions.Add(new DueDateExtension
        {
            MunicipalityId = fixture.Municipality.Id,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = fixture.Submission.Id,
            OriginalDueDate = originalDueDate,
            ExtendedDueDate = extendedDueDate,
            Reason = "Approved extension",
            ApprovedByUserId = fixture.User.Id,
            ApprovedAt = updatedOn
        });
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = fixture.Municipality.Id,
            EntityName = nameof(OpmsSubmission),
            EntityId = fixture.Submission.Id,
            Action = "ExtendDueDate",
            OldValue = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                [nameof(OpmsSubmission.PublicId)] = fixture.Submission.PublicId,
                [nameof(OpmsSubmission.DueDate)] = originalDueDate,
                [nameof(OpmsSubmission.DueDateExtendedDays)] = null,
                [nameof(OpmsSubmission.ExtendedDueDate)] = null,
                ["UpdatedByUserPublicId"] = null,
                [nameof(OpmsSubmission.UpdatedOn)] = null
            }),
            NewValue = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                [nameof(OpmsSubmission.PublicId)] = fixture.Submission.PublicId,
                [nameof(OpmsSubmission.DueDate)] = extendedDueDate,
                [nameof(OpmsSubmission.DueDateExtendedDays)] = 3,
                [nameof(OpmsSubmission.ExtendedDueDate)] = extendedDueDate,
                ["UpdatedByUserPublicId"] = fixture.User.PublicId,
                [nameof(OpmsSubmission.UpdatedOn)] = updatedOn
            }),
            ChangedBy = fixture.User.Id,
            Reason = "Different reason"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsSubmission ExtendDueDate requires one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Generic_edit_audit_cannot_forge_a_workflow_projection()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("forged-workflow");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        fixture.Submission.Status = "approved";
        fixture.Submission.ApproverUserId = fixture.User.Id;
        fixture.Submission.ApprovedAt = DateTime.UtcNow;
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = DateTime.UtcNow;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = fixture.Municipality.Id,
            EntityName = nameof(OpmsSubmission),
            EntityId = fixture.Submission.Id,
            Action = "Edit",
            OldValue = "{\"status\":\"IN_PROGRESS\"}",
            NewValue = "{\"status\":\"approved\"}",
            ChangedBy = fixture.User.Id,
            Reason = "Forged approval"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Submission workflow, actor, decision, score, status and terminal projections may change only beside their exact append-only workflow action.*");
    }

    [Fact]
    public async Task Forged_submission_withdrawal_evidence_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("forged-withdrawal");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();
        var occurredAt = DateTime.UtcNow;
        ApplyWithdrawal(fixture, occurredAt, "Approved withdrawal");
        context.GovernedRecordLifecycleEvents.Add(Lifecycle(fixture, occurredAt, "Different reason"));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-submission withdrawal requires exact same-transaction tenant, reason, actor, time and lifecycle evidence.*");
    }

    [Fact]
    public async Task Exact_withdrawal_is_one_way_and_makes_submission_immutable()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("withdrawal");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();
        var occurredAt = DateTime.UtcNow;
        const string reason = "Approved withdrawal";
        ApplyWithdrawal(fixture, occurredAt, reason);
        context.GovernedRecordLifecycleEvents.Add(Lifecycle(fixture, occurredAt, reason));
        await context.SaveChangesAsync();

        fixture.Submission.ActualPerformance = "99";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A withdrawn performance submission is immutable.*");
    }

    private static SubmissionFixture Fixture(string suffix)
    {
        var municipality = new Municipality
        {
            Code = $"SUB-{suffix}-{Guid.NewGuid():N}"[..20],
            Name = $"Submission root {suffix}",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        };
        var user = IdpTestFixture.CreateUser($"submission-root-{suffix}");
        user.Municipality = municipality;
        var target = new OpmsTarget
        {
            Id = $"submission-target-{suffix}",
            Municipality = municipality,
            IndicatorNumber = "KPI-1",
            TargetName = "Governed target",
            KpiDescription = "Governed wording",
            CreatedAt = DateTime.UtcNow
        };
        var createdAt = DateTime.UtcNow;
        var submission = new OpmsSubmission
        {
            Id = $"submission-root-{suffix}",
            Municipality = municipality,
            OpmsTarget = target,
            Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress,
            Status = SubmissionBaseStates.InProgress,
            SubmittedByUser = user,
            CreatedBy = user.Id,
            CreatedOn = createdAt,
            CreatedAt = createdAt
        };
        return new(municipality, user, target, submission);
    }

    private static void ApplyWithdrawal(SubmissionFixture fixture, DateTime occurredAt, string reason)
    {
        fixture.Submission.IsDisabled = true;
        fixture.Submission.WithdrawalReason = reason;
        fixture.Submission.WithdrawnAt = occurredAt;
        fixture.Submission.WithdrawnByUserId = fixture.User.Id;
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = occurredAt;
    }

    private static GovernedRecordLifecycleEvent Lifecycle(SubmissionFixture fixture, DateTime occurredAt, string reason) => new()
    {
        MunicipalityId = fixture.Municipality.Id,
        AggregateType = nameof(OpmsSubmission),
        AggregateId = fixture.Submission.Id,
        Action = GovernedLifecycleAction.Withdrawn,
        Reason = reason,
        ActorUserId = fixture.User.Id,
        OccurredAt = occurredAt,
        CorrelationId = "submission-root-test"
    };

    private static AuditTrail EditAudit(SubmissionFixture fixture, string auditActor, DateTime updatedOn, string newActual) => new()
    {
        MunicipalityId = fixture.Municipality.Id,
        EntityName = nameof(OpmsSubmission),
        EntityId = fixture.Submission.Id,
        Action = "Edit",
        OldValue = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [nameof(OpmsSubmission.PublicId)] = fixture.Submission.PublicId,
            [nameof(OpmsSubmission.ActualPerformance)] = null,
            ["UpdatedByUserPublicId"] = null,
            [nameof(OpmsSubmission.UpdatedOn)] = null
        }),
        NewValue = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [nameof(OpmsSubmission.PublicId)] = fixture.Submission.PublicId,
            [nameof(OpmsSubmission.ActualPerformance)] = newActual,
            ["UpdatedByUserPublicId"] = fixture.User.PublicId,
            [nameof(OpmsSubmission.UpdatedOn)] = updatedOn
        }),
        ChangedBy = auditActor,
        Reason = "Governed draft correction"
    };

    private sealed record SubmissionFixture(
        Municipality Municipality,
        ApplicationUser User,
        OpmsTarget Target,
        OpmsSubmission Submission);
}

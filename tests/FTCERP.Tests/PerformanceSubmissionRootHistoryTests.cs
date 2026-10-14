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
            .WithMessage("OpmsSubmission changes require same-transaction reasoned before/after audit evidence.*");
    }

    [Fact]
    public async Task Exact_draft_value_audit_allows_the_governed_change()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = Fixture("audited-edit");
        context.AddRange(fixture.Municipality, fixture.User, fixture.Target, fixture.Submission);
        await context.SaveChangesAsync();

        fixture.Submission.ActualPerformance = "42";
        fixture.Submission.UpdatedBy = fixture.User.Id;
        fixture.Submission.UpdatedOn = DateTime.UtcNow;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = fixture.Municipality.Id,
            EntityName = nameof(OpmsSubmission),
            EntityId = fixture.Submission.Id,
            Action = "Edit",
            OldValue = "{\"actualPerformance\":null}",
            NewValue = "{\"actualPerformance\":\"42\"}",
            ChangedBy = fixture.User.Id,
            Reason = "Governed draft correction"
        });

        await context.SaveChangesAsync();
        fixture.Submission.ActualPerformance.Should().Be("42");
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

    private sealed record SubmissionFixture(
        Municipality Municipality,
        ApplicationUser User,
        OpmsTarget Target,
        OpmsSubmission Submission);
}

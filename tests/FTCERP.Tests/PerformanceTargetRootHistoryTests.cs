namespace FTCERP.Tests;

public sealed class PerformanceTargetRootHistoryTests
{
    [Fact]
    public async Task Governed_originals_and_tenant_identity_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("identity");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.IndicatorNumber = "FORGED";

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-target identity, tenant, governed originals and retired wide-period projection are immutable after creation.*");
    }

    [Fact]
    public async Task Ordinary_target_edit_requires_same_transaction_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("audit");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.Baseline = 12;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsTarget changes require same-transaction reasoned before/after audit evidence.*");
    }

    [Fact]
    public async Task Forged_withdrawal_evidence_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("forged-withdrawal");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();
        var occurredAt = DateTime.UtcNow;
        target.IsWithdrawn = true;
        target.ReasonForWithdrawal = "Approved retirement";
        target.WithdrawnAt = occurredAt;
        target.WithdrawnByUserId = user.Id;
        context.GovernedRecordLifecycleEvents.Add(Lifecycle(target, municipality.Id, user.Id, occurredAt, "Different reason"));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-target withdrawal requires exact same-transaction tenant, reason, actor, time and lifecycle evidence.*");
    }

    [Fact]
    public async Task Exact_withdrawal_is_one_way_and_makes_target_immutable()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("withdrawal");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();
        var occurredAt = DateTime.UtcNow;
        const string reason = "Approved retirement";
        target.IsWithdrawn = true;
        target.ReasonForWithdrawal = reason;
        target.WithdrawnAt = occurredAt;
        target.WithdrawnByUserId = user.Id;
        context.GovernedRecordLifecycleEvents.Add(Lifecycle(target, municipality.Id, user.Id, occurredAt, reason));
        await context.SaveChangesAsync();

        target.Baseline = 99;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A withdrawn performance target is immutable.*");
    }

    [Fact]
    public async Task Definition_change_requires_exact_field_history_and_target_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("revision");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.IsTargetNameRevised = true;
        target.RevisedTargetName = "Revised governed target";
        target.IsRevised = true;
        context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = municipality.Id,
            OpmsTargetId = target.Id,
            FieldName = nameof(OpmsTarget.TargetName),
            OriginalValue = target.TargetName,
            RevisedValue = target.RevisedTargetName,
            Reason = "Council-approved revision",
            ApprovalReference = "COUNCIL-2026-10",
            EffectiveAt = DateTime.UtcNow,
            RevisedByUserId = user.Id
        });
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = nameof(OpmsTarget),
            EntityId = target.Id,
            Action = "ReviseDefinitionFields",
            OldValue = "{\"targetName\":\"Original target\"}",
            NewValue = "{\"targetName\":\"Revised governed target\"}",
            ChangedBy = user.Id,
            Reason = "Council-approved revision"
        });

        await context.SaveChangesAsync();
        (await context.KpiFieldRevisions.CountAsync()).Should().Be(1);
    }

    private static (Municipality Municipality, ApplicationUser User, OpmsTarget Target) Fixture(string suffix)
    {
        var municipality = new Municipality
        {
            Code = $"TARGET-{suffix}-{Guid.NewGuid():N}"[..20],
            Name = $"Target root {suffix}",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        };
        var user = IdpTestFixture.CreateUser($"target-root-{suffix}");
        user.Municipality = municipality;
        var target = new OpmsTarget
        {
            Id = $"target-root-{suffix}",
            Municipality = municipality,
            IndicatorNumber = "KPI-1",
            TargetName = "Original target",
            KpiDescription = "Original KPI wording",
            AnnualTargetDescription = "Retired compatibility value",
            OriginalOrderNumber = 1,
            RevisedOrderNumber = 1,
            CreatedAt = DateTime.UtcNow
        };
        return (municipality, user, target);
    }

    private static GovernedRecordLifecycleEvent Lifecycle(
        OpmsTarget target, long municipalityId, string userId, DateTime occurredAt, string reason) => new()
    {
        MunicipalityId = municipalityId,
        AggregateType = nameof(OpmsTarget),
        AggregateId = target.Id,
        Action = GovernedLifecycleAction.Withdrawn,
        Reason = reason,
        ActorUserId = userId,
        OccurredAt = occurredAt,
        CorrelationId = "target-root-test"
    };
}

using System.Text.Json;

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
            .WithMessage("OpmsTarget changes require one exact actor-bound same-transaction before/after audit.*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ordinary_target_edit_requires_exact_public_identity_and_value_snapshots(bool ipms)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality
        {
            Code = $"TARGET-EXACT-{Guid.NewGuid():N}"[..20],
            Name = "Exact target audit",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        };
        var user = IdpTestFixture.CreateUser($"target-exact-{ipms}");
        user.Municipality = municipality;
        object target = ipms
            ? new IpmsTarget { Id = "ipms-exact", Municipality = municipality, IndicatorNumber = "KPI-1", TargetName = "Original", KpiDescription = "Original", AnnualTargetDescription = "Legacy", CreatedAt = DateTime.UtcNow }
            : new OpmsTarget { Id = "opms-exact", Municipality = municipality, IndicatorNumber = "KPI-1", TargetName = "Original", KpiDescription = "Original", AnnualTargetDescription = "Legacy", CreatedAt = DateTime.UtcNow };
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        var publicId = ipms ? ((IpmsTarget)target).PublicId : ((OpmsTarget)target).PublicId;
        var entityId = ipms ? ((IpmsTarget)target).Id : ((OpmsTarget)target).Id;
        if (ipms) ((IpmsTarget)target).Baseline = 12m;
        else ((OpmsTarget)target).Baseline = 12m;
        context.AuditTrails.Add(EditAudit(ipms ? nameof(IpmsTarget) : nameof(OpmsTarget), entityId,
            municipality.Id, user.Id,
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = publicId, [nameof(OpmsTarget.Baseline)] = 0m },
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = publicId, [nameof(OpmsTarget.Baseline)] = 12m }));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Forged_target_snapshot_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("forged-audit");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.Baseline = 12m;
        context.AuditTrails.Add(EditAudit(nameof(OpmsTarget), target.Id, municipality.Id, user.Id,
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = target.PublicId, [nameof(OpmsTarget.Baseline)] = 0m },
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = target.PublicId, [nameof(OpmsTarget.Baseline)] = 99m }));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsTarget changes require one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Forged_but_persisted_target_actor_is_rejected_for_an_authenticated_tenant()
    {
        const long municipalityId = 982001;
        var expectedActor = IdpTestFixture.CreateUser("target-expected-actor");
        var forgedActor = IdpTestFixture.CreateUser("target-forged-actor");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityId, expectedActor.Id));
        var municipality = new Municipality
        {
            Id = municipalityId,
            Code = "TARGET-ACTOR",
            Name = "Target actor municipality",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        };
        expectedActor.Municipality = municipality;
        forgedActor.Municipality = municipality;
        var target = new OpmsTarget
        {
            Id = "target-actor-bound",
            Municipality = municipality,
            IndicatorNumber = "KPI-1",
            TargetName = "Original",
            KpiDescription = "Original",
            AnnualTargetDescription = "Legacy",
            CreatedAt = DateTime.UtcNow
        };
        context.AddRange(municipality, expectedActor, forgedActor, target);
        await context.SaveChangesAsync();

        target.Baseline = 12m;
        context.AuditTrails.Add(EditAudit(nameof(OpmsTarget), target.Id, municipality.Id, forgedActor.Id,
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = target.PublicId, [nameof(OpmsTarget.Baseline)] = 0m },
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = target.PublicId, [nameof(OpmsTarget.Baseline)] = 12m }));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpmsTarget changes require one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Target_reference_changes_are_audited_by_stable_public_identity()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, originalAssignee, target) = Fixture("public-reference");
        var revisedAssignee = IdpTestFixture.CreateUser("target-revised-assignee");
        revisedAssignee.Municipality = municipality;
        target.AssignedUser = originalAssignee;
        context.AddRange(municipality, originalAssignee, revisedAssignee, target);
        await context.SaveChangesAsync();

        target.AssignedUserId = revisedAssignee.Id;
        context.AuditTrails.Add(EditAudit(nameof(OpmsTarget), target.Id, municipality.Id, originalAssignee.Id,
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = target.PublicId, ["AssignedUserPublicId"] = originalAssignee.PublicId },
            new Dictionary<string, object?> { [nameof(OpmsTarget.PublicId)] = target.PublicId, ["AssignedUserPublicId"] = revisedAssignee.PublicId }));

        await context.SaveChangesAsync();
        var audit = await context.AuditTrails.SingleAsync(item => item.EntityName == nameof(OpmsTarget));
        audit.OldValue.Should().Contain("AssignedUserPublicId").And.NotContain("AssignedUserId");
        audit.NewValue.Should().Contain(revisedAssignee.PublicId.ToString()).And.NotContain(revisedAssignee.Id);
    }

    [Fact]
    public async Task Legacy_classification_keys_are_immutable()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("legacy-key");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.BudgetTypeId = 42;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Performance-target identity, tenant, governed originals and retired wide-period projection are immutable after creation.*");
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
        var effectiveAt = DateTime.UtcNow;
        const string reason = "Council-approved revision";
        const string approvalReference = "COUNCIL-2026-10";
        context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = municipality.Id,
            OpmsTargetId = target.Id,
            FieldName = nameof(OpmsTarget.TargetName),
            OriginalValue = target.TargetName,
            RevisedValue = target.RevisedTargetName,
            Reason = reason,
            ApprovalReference = approvalReference,
            EffectiveAt = effectiveAt,
            RevisedByUserId = user.Id
        });
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = nameof(OpmsTarget),
            EntityId = target.Id,
            Action = "ReviseDefinitionFields",
            OldValue = JsonSerializer.Serialize(new { target.IndicatorNumber, target.TargetName, target.KpiDescription }),
            NewValue = JsonSerializer.Serialize(new
            {
                target.IndicatorNumber,
                TargetName = target.RevisedTargetName,
                target.KpiDescription,
                Reason = reason,
                ApprovalReference = approvalReference,
                EffectiveAt = effectiveAt
            }),
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        });

        await context.SaveChangesAsync();
        (await context.KpiFieldRevisions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Definition_change_rejects_an_audit_with_unrelated_snapshots()
    {
        var (municipality, user, target) = Fixture("revision-audit-bypass");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(null, user.Id, true));
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.IsTargetNameRevised = true;
        target.RevisedTargetName = "Revised governed target";
        target.IsRevised = true;
        var effectiveAt = DateTime.UtcNow;
        context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = municipality.Id,
            OpmsTargetId = target.Id,
            FieldName = nameof(OpmsTarget.TargetName),
            OriginalValue = target.TargetName,
            RevisedValue = target.RevisedTargetName,
            Reason = "Council-approved revision",
            ApprovalReference = "COUNCIL-2026-10",
            EffectiveAt = effectiveAt,
            RevisedByUserId = user.Id
        });
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = nameof(OpmsTarget),
            EntityId = target.Id,
            Action = "ReviseDefinitionFields",
            OldValue = "{}",
            NewValue = "{}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Council-approved revision"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("KPI revisions require one exact actor-bound same-transaction before/after target audit.");
    }

    [Fact]
    public async Task Definition_change_rejects_a_future_dated_exact_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (municipality, user, target) = Fixture("revision-future-audit");
        context.AddRange(municipality, user, target);
        await context.SaveChangesAsync();

        target.IsTargetNameRevised = true;
        target.RevisedTargetName = "Revised governed target";
        target.IsRevised = true;
        var effectiveAt = DateTime.UtcNow;
        const string reason = "Council-approved revision";
        const string approvalReference = "COUNCIL-2026-10";
        context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = municipality.Id,
            OpmsTargetId = target.Id,
            FieldName = nameof(OpmsTarget.TargetName),
            OriginalValue = target.TargetName,
            RevisedValue = target.RevisedTargetName,
            Reason = reason,
            ApprovalReference = approvalReference,
            EffectiveAt = effectiveAt,
            RevisedByUserId = user.Id
        });
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = nameof(OpmsTarget),
            EntityId = target.Id,
            Action = "ReviseDefinitionFields",
            OldValue = JsonSerializer.Serialize(new { target.IndicatorNumber, target.TargetName, target.KpiDescription }),
            NewValue = JsonSerializer.Serialize(new
            {
                target.IndicatorNumber,
                TargetName = target.RevisedTargetName,
                target.KpiDescription,
                Reason = reason,
                ApprovalReference = approvalReference,
                EffectiveAt = effectiveAt
            }),
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow.AddHours(1),
            Reason = reason
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("KPI revisions require one exact actor-bound same-transaction before/after target audit.");
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

    private static AuditTrail EditAudit(string entityName, string entityId, long municipalityId, string userId,
        IReadOnlyDictionary<string, object?> before, IReadOnlyDictionary<string, object?> after) => new()
    {
        MunicipalityId = municipalityId,
        EntityName = entityName,
        EntityId = entityId,
        Action = "Edit",
        OldValue = JsonSerializer.Serialize(before),
        NewValue = JsonSerializer.Serialize(after),
        ChangedBy = userId,
        Reason = "Approved target edit"
    };
}

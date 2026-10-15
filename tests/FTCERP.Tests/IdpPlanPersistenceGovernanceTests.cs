using System.Text.Json;

namespace FTCERP.Tests;

public sealed class IdpPlanPersistenceGovernanceTests
{
    [Fact]
    public async Task Plan_identity_rewrite_and_hard_delete_are_rejected_at_the_persistence_boundary()
    {
        await using var rewriteContext = IdpTestFixture.CreateRelationalContext();
        var (plan, _) = await SeedPlanAsync(rewriteContext);
        plan.PlanCode = "IDP-REWRITTEN";
        QueuePlanAudit(rewriteContext, plan, "Attempted identity rewrite");

        await FluentActions.Invoking(() => rewriteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*identity are immutable*");

        await using var deleteContext = IdpTestFixture.CreateRelationalContext();
        var (deletable, _) = await SeedPlanAsync(deleteContext);
        deleteContext.ChangeTracker.Clear();
        deleteContext.IdpPlans.Remove(await deleteContext.IdpPlans
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == deletable.Id));

        await FluentActions.Invoking(() => deleteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Plan_business_change_requires_reasoned_before_and_after_audit_in_the_same_unit_of_work()
    {
        await using var deniedContext = IdpTestFixture.CreateRelationalContext();
        var (deniedPlan, _) = await SeedPlanAsync(deniedContext);
        deniedPlan.PlanTitle = "Unaudited title";

        await FluentActions.Invoking(() => deniedContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IdpPlan Update requires one exact actor-bound same-transaction before/after audit.*");

        await using var acceptedContext = IdpTestFixture.CreateRelationalContext();
        var (acceptedPlan, _) = await SeedPlanAsync(acceptedContext);
        acceptedPlan.PlanTitle = "Audited title";
        QueuePlanAudit(acceptedContext, acceptedPlan, "Council corrected the plan title");

        await acceptedContext.SaveChangesAsync();

        (await acceptedContext.IdpPlans.SingleAsync()).PlanTitle.Should().Be("Audited title");
        (await acceptedContext.AuditTrails.SingleAsync()).Reason.Should().Be("Council corrected the plan title");
    }

    [Fact]
    public async Task Plan_lifecycle_cannot_move_backwards_or_rewrite_approval_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (plan, user) = await SeedPlanAsync(context, IdpPlanStatus.Approved);
        var approvedAt = plan.ApprovedAt;
        plan.Status = IdpPlanStatus.Draft;
        plan.ApprovedAt = approvedAt!.Value.AddMinutes(1);
        QueuePlanAudit(context, plan, "Attempted lifecycle reversal");

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*lifecycle may only advance*");

        user.Id.Should().Be(plan.ApprovedByUserId);
    }

    [Fact]
    public async Task Plan_update_rejects_forged_snapshot_and_forged_persisted_actor()
    {
        const long municipalityId = 982201;
        var expectedActor = IdpTestFixture.CreateUser("idp-plan-expected-actor");
        var forgedActor = IdpTestFixture.CreateUser("idp-plan-forged-actor");
        expectedActor.MunicipalityId = municipalityId;
        forgedActor.MunicipalityId = municipalityId;
        await using var context = IdpTestFixture.CreateRelationalContext(
            IdpTestFixture.Tenant(municipalityId, expectedActor.Id));
        var municipality = new Municipality { Id = municipalityId, Code = "IDP-EXACT", Name = "IDP exact municipality" };
        var plan = new IdpPlan
        {
            Municipality = municipality, MunicipalityName = municipality.Name, PlanTitle = "Governed IDP",
            PlanCode = "IDP-EXACT", StartFinancialYear = 2026, EndFinancialYear = 2031,
            EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = expectedActor.Id
        };
        context.AddRange(municipality, expectedActor, forgedActor, plan);
        await context.SaveChangesAsync();

        plan.PlanTitle = "Corrected title";
        var snapshots = context.BuildIdpPlanAuditSnapshots(plan);
        var forgedAfter = snapshots.After.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        forgedAfter[nameof(IdpPlan.PlanTitle)] = "Unrelated title";
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipalityId, EntityName = nameof(IdpPlan), EntityId = plan.PublicId.ToString(),
            Action = "Update", OldValue = JsonSerializer.Serialize(snapshots.Before),
            NewValue = JsonSerializer.Serialize(forgedAfter), ChangedBy = expectedActor.Id, Reason = "Forged snapshot"
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IdpPlan Update requires one exact actor-bound same-transaction before/after audit.*");

        context.ChangeTracker.Clear();
        plan = await context.IdpPlans.SingleAsync(item => item.PlanCode == "IDP-EXACT");
        plan.PlanTitle = "Another corrected title";
        snapshots = context.BuildIdpPlanAuditSnapshots(plan);
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipalityId, EntityName = nameof(IdpPlan), EntityId = plan.PublicId.ToString(),
            Action = "Update", OldValue = JsonSerializer.Serialize(snapshots.Before),
            NewValue = JsonSerializer.Serialize(snapshots.After), ChangedBy = forgedActor.Id, Reason = "Forged actor"
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IdpPlan Update requires one exact actor-bound same-transaction before/after audit.*");
    }

    [Fact]
    public async Task Plan_version_counter_cannot_advance_without_an_exact_reason_audited_successor()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (plan, _) = await SeedPlanAsync(context);
        plan.CurrentVersionNumber = 2;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*one exact actor-bound predecessor/successor audit*");
    }

    [Fact]
    public async Task Plan_version_successor_rejects_a_correctly_labelled_but_non_exact_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (plan, user) = await SeedPlanAsync(context);
        var predecessor = await context.IdpPlanVersions.SingleAsync();
        var effectiveFrom = predecessor.EffectiveFrom.AddYears(1);
        var successor = new IdpPlanVersion
        {
            IdpPlan = plan,
            PredecessorVersion = predecessor,
            VersionNumber = 2,
            VersionType = IdpVersionType.AnnualReview,
            VersionLabel = "Annual review",
            SummaryOfChanges = "Council-approved annual review",
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = user.Id
        };
        predecessor.IsActive = false;
        predecessor.EffectiveTo = effectiveFrom;
        plan.CurrentVersionNumber = 2;
        context.IdpPlanVersions.Add(successor);
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = nameof(IdpPlanVersion), EntityId = successor.PublicId.ToString(), Action = "Create",
            OldValue = "{}", NewValue = "{}", ChangedBy = user.Id, Reason = "Disguised empty version evidence"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exact actor-bound predecessor/successor audit*");
    }

    [Fact]
    public async Task Exact_successor_closure_plan_increment_and_audit_persist_atomically()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (plan, user) = await SeedPlanAsync(context);
        var predecessor = await context.IdpPlanVersions.SingleAsync();
        var successorEffectiveFrom = predecessor.EffectiveFrom.AddYears(1);
        var createdAt = DateTime.UtcNow;
        var successor = new IdpPlanVersion
        {
            IdpPlan = plan,
            PredecessorVersion = predecessor,
            VersionNumber = 2,
            VersionType = IdpVersionType.AnnualReview,
            VersionLabel = "Annual review",
            SummaryOfChanges = "Council-approved annual review",
            EffectiveFrom = successorEffectiveFrom,
            CreatedAt = createdAt,
            CreatedByUserId = user.Id,
            IsActive = true
        };
        predecessor.IsActive = false;
        predecessor.EffectiveTo = successorEffectiveFrom;
        plan.CurrentVersionNumber = 2;
        context.IdpPlanVersions.Add(successor);
        const string reason = "Council approved the annual review";
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = nameof(IdpPlanVersion),
            EntityId = successor.PublicId.ToString(),
            Action = "Create",
            OldValue = JsonSerializer.Serialize(new IdpPlanVersionAuditSnapshot(
                plan.PublicId, predecessor.PublicId, null, predecessor.VersionNumber, predecessor.VersionType,
                predecessor.VersionLabel, predecessor.ReviewYear, predecessor.SummaryOfChanges, true,
                predecessor.CreatedAt, user.PublicId, predecessor.EffectiveFrom, null, predecessor.PublishedAt,
                predecessor.PublicationReference, null)),
            NewValue = JsonSerializer.Serialize(new IdpPlanVersionAuditSnapshot(
                plan.PublicId, successor.PublicId, predecessor.PublicId, successor.VersionNumber, successor.VersionType,
                successor.VersionLabel, successor.ReviewYear, successor.SummaryOfChanges, successor.IsActive,
                successor.CreatedAt, user.PublicId, successor.EffectiveFrom, successor.EffectiveTo, successor.PublishedAt,
                successor.PublicationReference, reason)),
            ChangedBy = user.Id,
            ChangedAt = createdAt,
            Reason = reason
        });

        await context.SaveChangesAsync();

        var versions = await context.IdpPlanVersions.OrderBy(item => item.VersionNumber).ToListAsync();
        versions.Should().HaveCount(2);
        versions[0].IsActive.Should().BeFalse();
        versions[0].EffectiveTo.Should().Be(successorEffectiveFrom);
        versions[1].PredecessorVersionId.Should().Be(versions[0].Id);
        (await context.IdpPlans.SingleAsync()).CurrentVersionNumber.Should().Be(2);
        (await context.AuditTrails.SingleAsync()).EntityId.Should().Be(versions[1].PublicId.ToString());
    }

    [Fact]
    public async Task Plan_version_successor_rejects_audit_time_that_differs_from_created_at()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (plan, user) = await SeedPlanAsync(context);
        var predecessor = await context.IdpPlanVersions.SingleAsync();
        var effectiveFrom = predecessor.EffectiveFrom.AddYears(1);
        var createdAt = DateTime.UtcNow;
        var successor = new IdpPlanVersion
        {
            IdpPlan = plan,
            PredecessorVersion = predecessor,
            VersionNumber = 2,
            VersionType = IdpVersionType.AnnualReview,
            VersionLabel = "Timestamp-governed annual review",
            SummaryOfChanges = "Prove exact successor audit time correlation",
            EffectiveFrom = effectiveFrom,
            CreatedAt = createdAt,
            CreatedByUserId = user.Id,
            IsActive = true
        };
        predecessor.IsActive = false;
        predecessor.EffectiveTo = effectiveFrom;
        plan.CurrentVersionNumber = 2;
        context.IdpPlanVersions.Add(successor);
        const string reason = "Reject mismatched successor audit time";
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = nameof(IdpPlanVersion),
            EntityId = successor.PublicId.ToString(),
            Action = "Create",
            OldValue = JsonSerializer.Serialize(new IdpPlanVersionAuditSnapshot(
                plan.PublicId, predecessor.PublicId, null, predecessor.VersionNumber, predecessor.VersionType,
                predecessor.VersionLabel, predecessor.ReviewYear, predecessor.SummaryOfChanges, true,
                predecessor.CreatedAt, user.PublicId, predecessor.EffectiveFrom, null, predecessor.PublishedAt,
                predecessor.PublicationReference, null)),
            NewValue = JsonSerializer.Serialize(new IdpPlanVersionAuditSnapshot(
                plan.PublicId, successor.PublicId, predecessor.PublicId, successor.VersionNumber, successor.VersionType,
                successor.VersionLabel, successor.ReviewYear, successor.SummaryOfChanges, successor.IsActive,
                successor.CreatedAt, user.PublicId, successor.EffectiveFrom, successor.EffectiveTo, successor.PublishedAt,
                successor.PublicationReference, reason)),
            ChangedBy = user.Id,
            ChangedAt = createdAt.AddSeconds(1),
            Reason = reason
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exact actor-bound predecessor/successor audit*");
    }

    [Fact]
    public async Task Successor_creation_rejects_a_stale_plan_rowversion_without_partial_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var (plan, user) = await SeedPlanAsync(context);
        var staleRowVersion = Convert.ToBase64String(plan.RowVersion);
        plan.PlanTitle = "Concurrent title";
        QueuePlanAudit(context, plan, "Concurrent administrator update");
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "allowed", [], [], []));
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            new WorkflowGovernanceService(context),
            user.Id,
            accessControl: access.Object);
        var result = await controller.CreatePlanVersionByPublicId(plan.PublicId, new CreateIdpPlanVersionRequest(
            "AnnualReview", "Stale annual review", "2026/2027", "Must not persist",
            EffectiveFrom: plan.EffectiveFrom.AddYears(1), RowVersion: staleRowVersion,
            Reason: "Attempted stale annual review"));

        result.Result.Should().BeOfType<ConflictObjectResult>();
        context.ChangeTracker.Clear();
        (await context.IdpPlans.SingleAsync()).CurrentVersionNumber.Should().Be(1);
        (await context.IdpPlanVersions.CountAsync()).Should().Be(1);
        (await context.AuditTrails.CountAsync()).Should().Be(1);
    }

    private static async Task<(IdpPlan Plan, ApplicationUser User)> SeedPlanAsync(
        ApplicationDbContext context,
        IdpPlanStatus status = IdpPlanStatus.Draft)
    {
        var user = IdpTestFixture.CreateUser($"idp-plan-governor-{Guid.NewGuid():N}");
        var effectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "Governed IDP",
            PlanCode = "IDP-2026",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            Status = status,
            CurrentVersionNumber = 1,
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = user.Id,
            ApprovedAt = (int)status >= (int)IdpPlanStatus.Approved ? effectiveFrom.AddDays(1) : null,
            ApprovedByUserId = (int)status >= (int)IdpPlanStatus.Approved ? user.Id : null
        };
        context.Users.Add(user);
        context.IdpPlans.Add(plan);
        context.IdpPlanVersions.Add(new IdpPlanVersion
        {
            IdpPlan = plan,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original",
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = user.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();
        return (plan, user);
    }

    private static void QueuePlanAudit(ApplicationDbContext context, IdpPlan plan, string reason)
    {
        var snapshots = context.BuildIdpPlanAuditSnapshots(plan);
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = plan.MunicipalityId,
            EntityName = nameof(IdpPlan),
            EntityId = plan.PublicId.ToString(),
            Action = "Update",
            OldValue = JsonSerializer.Serialize(snapshots.Before),
            NewValue = JsonSerializer.Serialize(snapshots.After),
            ChangedBy = plan.CreatedByUserId,
            Reason = reason
        });
    }
}

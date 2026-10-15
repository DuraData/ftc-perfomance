namespace FTCERP.Tests;

public class IdpSecondaryGovernanceTests
{
    private static readonly Type[] PublicIdentityTypes =
    [
        typeof(IdpChangeLog),
        typeof(IdpAnnualTarget),
        typeof(IdpAlignmentLink),
        typeof(IdpCommunitySession),
        typeof(IdpCommunityNeed),
        typeof(IdpWardInput),
        typeof(IdpStakeholderEngagement),
        typeof(IdpRiskLink),
        typeof(IdpBudgetSnapshot),
        typeof(IdpCollaborationComment),
        typeof(IdpTaskAssignment)
    ];

    private static readonly Type[] MutableTypes =
    [
        typeof(IdpAnnualTarget),
        typeof(IdpAlignmentLink),
        typeof(IdpCommunitySession),
        typeof(IdpCommunityNeed),
        typeof(IdpWardInput),
        typeof(IdpStakeholderEngagement),
        typeof(IdpRiskLink),
        typeof(IdpTaskAssignment)
    ];

    [Fact]
    public void Secondary_records_have_unique_public_identity_and_mutable_records_have_concurrency_tokens()
    {
        using var context = IdpTestFixture.CreateRelationalContext();

        foreach (var clrType in PublicIdentityTypes)
        {
            var entityType = context.Model.FindEntityType(clrType);
            entityType.Should().NotBeNull();
            var publicId = entityType!.FindProperty("PublicId");
            publicId.Should().NotBeNull($"{clrType.Name} must expose stable public identity");
            entityType.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Count == 1 && index.Properties[0] == publicId);

            var first = (Guid)clrType.GetProperty("PublicId")!.GetValue(Activator.CreateInstance(clrType)!)!;
            var second = (Guid)clrType.GetProperty("PublicId")!.GetValue(Activator.CreateInstance(clrType)!)!;
            first.Should().NotBeEmpty();
            second.Should().NotBe(first);
        }

        foreach (var clrType in MutableTypes)
        {
            var rowVersion = context.Model.FindEntityType(clrType)!.FindProperty("RowVersion");
            rowVersion.Should().NotBeNull($"{clrType.Name} is mutable and requires optimistic concurrency");
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }
    }

    public static TheoryData<object> AppendOnlyRecords => new()
    {
        new IdpChangeLog(),
        new IdpBudgetSnapshot(),
        new IdpCollaborationComment()
    };

    public static TheoryData<object> AppendPreservedOperationalRecords => new()
    {
        new IdpAnnualTarget(),
        new IdpAlignmentLink(),
        new IdpCommunitySession(),
        new IdpCommunityNeed(),
        new IdpWardInput(),
        new IdpStakeholderEngagement(),
        new IdpRiskLink()
    };

    public static TheoryData<object> CoreDefinitionRecords => new()
    {
        new IdpStrategicOutcome(),
        new IdpStrategicObjective(),
        new IdpDevelopmentPriority(),
        new IdpProgramme(),
        new IdpProject(),
        new IdpKpi()
    };

    [Theory]
    [MemberData(nameof(AppendOnlyRecords))]
    public async Task Idp_history_records_cannot_be_rewritten(object entity)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(entity).State = EntityState.Modified;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP change, budget-snapshot, and collaboration-comment history is append-only*");
    }

    [Fact]
    public async Task Idp_task_history_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new IdpTaskAssignment()).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP task history cannot be hard deleted*");
    }

    [Theory]
    [MemberData(nameof(AppendPreservedOperationalRecords))]
    public async Task Idp_secondary_operational_records_cannot_be_rewritten(object entity)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(entity).State = EntityState.Modified;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP annual-target, alignment, participation, stakeholder, and risk-link records are append-only*");
    }

    [Theory]
    [MemberData(nameof(AppendPreservedOperationalRecords))]
    public async Task Idp_secondary_operational_records_cannot_be_hard_deleted(object entity)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(entity).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP annual-target, alignment, participation, stakeholder, and risk-link records are append-only*");
    }

    [Theory]
    [MemberData(nameof(CoreDefinitionRecords))]
    public async Task Idp_core_definition_records_cannot_be_hard_deleted(object entity)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(entity).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP hierarchy and KPI definition history cannot be hard deleted*");
    }

    [Fact]
    public async Task Idp_core_definition_rewrite_requires_one_governed_import_commit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var project = new IdpProject
        {
            Id = 991,
            PublicId = Guid.NewGuid(),
            IdpProgrammeId = 881,
            ProjectCode = "GOVERNED-PROJECT",
            ProjectName = "Governed project"
        };
        context.Attach(project);
        project.Budget = 125000;
        context.Entry(project).Property(item => item.Budget).IsModified = true;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*may be rewritten only by one governed import commit*");
    }

    [Fact]
    public async Task Idp_task_completion_requires_an_exact_atomic_audit_and_preserves_task_evidence()
    {
        const long municipalityId = 8201;
        var actor = IdpTestFixture.CreateUser("idp-task-governance");
        actor.MunicipalityId = municipalityId;
        var tenant = IdpTestFixture.Tenant(municipalityId, actor.Id);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        context.Municipalities.Add(new Municipality
        {
            Id = municipalityId,
            Code = "IDP-TASK-GOV",
            Name = "Governed municipality"
        });
        context.Users.Add(actor);
        var plan = new IdpPlan
        {
            MunicipalityId = municipalityId,
            MunicipalityName = "Governed municipality",
            PlanCode = "IDP-GOVERNED-TASK",
            PlanTitle = "Governed task plan",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = actor.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();
        var task = new IdpTaskAssignment
        {
            IdpPlanId = plan.Id,
            Title = "Review plan",
            Description = "Review the plan evidence",
            AssignedToUserId = actor.Id,
            AssignedByUserId = actor.Id,
            DueDate = DateTime.UtcNow.AddDays(3)
        };
        context.IdpTaskAssignments.Add(task);
        await context.SaveChangesAsync();

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*one exact actor-bound same-transaction before/after audit*");

        context.ChangeTracker.Clear();
        task = await context.IdpTaskAssignments.SingleAsync(item => item.PublicId == task.PublicId);
        var before = new { task.IsCompleted, task.CompletedAt };
        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        new WorkflowGovernanceService(context, tenantContext: tenant).QueueAuditTrail(
            "IdpTask", task.PublicId.ToString(), "Complete", before,
            new { task.IsCompleted, task.CompletedAt }, actor.Id, null, "Reviewed and accepted");
        await context.SaveChangesAsync();

        var audit = await context.AuditTrails.SingleAsync(item => item.EntityName == "IdpTask"
            && item.EntityId == task.PublicId.ToString());
        audit.Action.Should().Be("Complete");
        audit.Reason.Should().Be("Reviewed and accepted");
        audit.OldValue.Should().NotBeNull();
        audit.NewValue.Should().NotBeNull();

        context.ChangeTracker.Clear();
        task = await context.IdpTaskAssignments.SingleAsync(item => item.PublicId == task.PublicId);
        before = new { task.IsCompleted, task.CompletedAt };
        task.IsCompleted = false;
        task.CompletedAt = null;
        new WorkflowGovernanceService(context, tenantContext: tenant).QueueAuditTrail(
            "IdpTask", task.PublicId.ToString(), "Reopen", before,
            new { task.IsCompleted, task.CompletedAt }, "forged-task-actor", null, "Returned for correction");
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*one exact actor-bound same-transaction before/after audit*");

        context.ChangeTracker.Clear();
        task = await context.IdpTaskAssignments.SingleAsync(item => item.PublicId == task.PublicId);
        var forgedBefore = new { task.IsCompleted, CompletedAt = (DateTime?)DateTime.UnixEpoch };
        task.IsCompleted = false;
        task.CompletedAt = null;
        new WorkflowGovernanceService(context, tenantContext: tenant).QueueAuditTrail(
            "IdpTask", task.PublicId.ToString(), "Reopen", forgedBefore,
            new { task.IsCompleted, task.CompletedAt }, actor.Id, null, "Returned for correction");
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*one exact actor-bound same-transaction before/after audit*");

        context.ChangeTracker.Clear();
        task = await context.IdpTaskAssignments.SingleAsync(item => item.PublicId == task.PublicId);
        task.PublicId = Guid.NewGuid();
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*identity, assignment, content and due-date evidence are immutable*");

        context.ChangeTracker.Clear();
        task = await context.IdpTaskAssignments.SingleAsync();
        task.Title = "Rewritten title";
        new WorkflowGovernanceService(context, tenantContext: tenant).QueueAuditTrail(
            "IdpTask", task.PublicId.ToString(), "Complete", new { Title = "Review plan" },
            new { task.Title }, actor.Id, null, "Attempted rewrite");
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP task identity, assignment, content and due-date evidence are immutable*");
    }
}

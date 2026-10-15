namespace FTCERP.Tests;

public sealed class InternalAuditAndRfiCreationIntegrityTests
{
    [Fact]
    public async Task Internal_audit_configuration_requires_exact_reasoned_audit_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "config-no-audit");
        context.InternalAuditAssessmentConfigurations.Add(NewConfiguration(fixture, DateTime.UtcNow));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Internal Audit configuration creation requires one chronological tenant/year version with a canonical actor and exact same-time reasoned audit evidence.*");
    }

    [Fact]
    public async Task Canonical_configuration_and_assessment_are_persisted_with_exact_workflow_and_audit_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "valid-assessment");
        var configuration = await AddConfigurationAsync(context, fixture);
        var assessedAt = DateTime.UtcNow;
        var assessment = new InternalAuditAssessment
        {
            MunicipalityId = fixture.Municipality.Id, SubmissionWorkflowInstanceId = fixture.Instance.Id,
            SubmissionWorkflowInstance = fixture.Instance, ConfigurationId = configuration.Id, Configuration = configuration,
            Outcome = InternalAuditAssessmentOutcome.Achieved, DetailedObservation = "Canonical evidence supports achievement.",
            AssessedByUserId = fixture.Actor.Id, AssessedByUser = fixture.Actor, AssessedAt = assessedAt,
            CorrelationId = "audit-valid-assessment"
        };
        AddWorkflowAction(context, fixture, "AUDIT_ACCEPTED", WorkflowActionOutcome.Complete,
            assessment.DetailedObservation, assessedAt, assessment.CorrelationId);
        context.AuditTrails.Add(NewAudit(fixture, nameof(InternalAuditAssessment), assessment.PublicId, "Assess", assessedAt,
            new { Model = configuration.Model, Outcome = assessment.Outcome, assessment.DetailedObservation, PreviousAssessmentPublicId = (Guid?)null },
            correlationId: assessment.CorrelationId));
        context.InternalAuditAssessments.Add(assessment);

        await context.SaveChangesAsync();

        (await context.InternalAuditAssessments.SingleAsync()).PublicId.Should().Be(assessment.PublicId);
    }

    [Fact]
    public async Task Internal_audit_assessment_without_its_exact_workflow_action_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "assessment-no-action");
        var configuration = await AddConfigurationAsync(context, fixture);
        var assessedAt = DateTime.UtcNow;
        var assessment = new InternalAuditAssessment
        {
            MunicipalityId = fixture.Municipality.Id, SubmissionWorkflowInstanceId = fixture.Instance.Id,
            SubmissionWorkflowInstance = fixture.Instance, ConfigurationId = configuration.Id, Configuration = configuration,
            Outcome = InternalAuditAssessmentOutcome.Achieved, DetailedObservation = "The action is deliberately absent.",
            AssessedByUserId = fixture.Actor.Id, AssessedAt = assessedAt, CorrelationId = "audit-no-action"
        };
        context.InternalAuditAssessments.Add(assessment);
        context.AuditTrails.Add(NewAudit(fixture, nameof(InternalAuditAssessment), assessment.PublicId, "Assess", assessedAt,
            new { Model = configuration.Model, Outcome = assessment.Outcome, assessment.DetailedObservation, PreviousAssessmentPublicId = (Guid?)null },
            correlationId: assessment.CorrelationId));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Internal Audit assessment creation requires the effective tenant/year model, exact history predecessor, canonical actor, workflow action, adverse-outcome RFI and same-time audit evidence.*");
    }

    [Fact]
    public async Task Adverse_assessment_without_its_exact_rfi_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "assessment-no-rfi");
        var configuration = await AddConfigurationAsync(context, fixture);
        var assessedAt = DateTime.UtcNow;
        var assessment = new InternalAuditAssessment
        {
            MunicipalityId = fixture.Municipality.Id, SubmissionWorkflowInstanceId = fixture.Instance.Id,
            SubmissionWorkflowInstance = fixture.Instance, ConfigurationId = configuration.Id, Configuration = configuration,
            Outcome = InternalAuditAssessmentOutcome.NotAchieved, DetailedObservation = "The adverse assessment omits its RFI.",
            AssessedByUserId = fixture.Actor.Id, AssessedAt = assessedAt, CorrelationId = "audit-no-rfi"
        };
        AddWorkflowAction(context, fixture, "AUDIT_REJECTED", WorkflowActionOutcome.Complete,
            assessment.DetailedObservation, assessedAt, assessment.CorrelationId);
        context.InternalAuditAssessments.Add(assessment);
        context.AuditTrails.Add(NewAudit(fixture, nameof(InternalAuditAssessment), assessment.PublicId, "Assess", assessedAt,
            new { Model = configuration.Model, Outcome = assessment.Outcome, assessment.DetailedObservation, PreviousAssessmentPublicId = (Guid?)null },
            correlationId: assessment.CorrelationId));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Internal Audit assessment creation requires the effective tenant/year model, exact history predecessor, canonical actor, workflow action, adverse-outcome RFI and same-time audit evidence.*");
    }

    [Fact]
    public async Task Rfi_creation_without_exact_action_and_audit_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "rfi-no-action");
        context.PerformanceRfis.Add(NewRfi(fixture, DateTime.UtcNow));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RFI lifecycle creation requires canonical tenant, instance and actor evidence plus one exact same-time workflow action and audit record.*");
    }

    [Fact]
    public async Task Rfi_response_without_exact_action_and_audit_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "rfi-response");
        var rfi = NewRfi(fixture, DateTime.UtcNow);
        AddRfiCreationEvidence(context, fixture, rfi);
        context.PerformanceRfis.Add(rfi);
        await context.SaveChangesAsync();

        rfi.Response = "A response without its canonical workflow evidence.";
        rfi.RespondedByUserId = fixture.Actor.Id;
        rfi.RespondedAt = DateTime.UtcNow;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RFI lifecycle creation requires canonical tenant, instance and actor evidence plus one exact same-time workflow action and audit record.*");
    }

    private static async Task<Fixture> SeedAsync(ApplicationDbContext context, string suffix)
    {
        var municipality = new Municipality { Code = $"IAI-{suffix}"[..Math.Min(20, $"IAI-{suffix}".Length)], Name = $"Integrity {suffix}" };
        var actor = IdpTestFixture.CreateUser($"integrity-{suffix}");
        actor.Municipality = municipality;
        var year = new FinancialYear
        {
            Code = $"I{Guid.NewGuid():N}"[..20], Name = "Integrity year",
            StartDate = DateTime.UtcNow.Date.AddYears(-1), EndDate = DateTime.UtcNow.Date.AddYears(1)
        };
        var municipalYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = year, EffectiveFrom = year.StartDate
        };
        var definition = new WorkflowDefinition
        {
            Municipality = municipality, MunicipalityFinancialYear = municipalYear,
            SubmissionKind = SubmissionKind.Opms, Code = $"WF-{suffix}", Name = "Integrity workflow",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        var instance = new SubmissionWorkflowInstance
        {
            Municipality = municipality, WorkflowDefinition = definition,
            SubmissionKind = SubmissionKind.Opms, SubmissionId = $"submission-{suffix}"
        };
        context.AddRange(municipality, actor, year, municipalYear, definition, instance);
        await context.SaveChangesAsync();
        return new(municipality, actor, municipalYear, instance);
    }

    private static InternalAuditAssessmentConfiguration NewConfiguration(Fixture fixture, DateTime createdAt) => new()
    {
        MunicipalityId = fixture.Municipality.Id, MunicipalityFinancialYearId = fixture.Year.Id,
        MunicipalityFinancialYear = fixture.Year, Model = InternalAuditAssessmentModel.Detailed,
        EffectiveFrom = createdAt.AddMinutes(-1), Reason = "Approved integrity-test methodology",
        CreatedByUserId = fixture.Actor.Id, CreatedByUser = fixture.Actor, CreatedAt = createdAt
    };

    private static async Task<InternalAuditAssessmentConfiguration> AddConfigurationAsync(ApplicationDbContext context, Fixture fixture)
    {
        var createdAt = DateTime.UtcNow;
        var configuration = NewConfiguration(fixture, createdAt);
        context.InternalAuditAssessmentConfigurations.Add(configuration);
        context.AuditTrails.Add(NewAudit(fixture, nameof(InternalAuditAssessmentConfiguration), configuration.PublicId,
            "SelectModel", createdAt, new { configuration.Model, configuration.Version, configuration.EffectiveFrom, configuration.Reason }, configuration.Reason));
        await context.SaveChangesAsync();
        return configuration;
    }

    private static PerformanceRfi NewRfi(Fixture fixture, DateTime raisedAt) => new()
    {
        MunicipalityId = fixture.Municipality.Id, SubmissionWorkflowInstanceId = fixture.Instance.Id,
        SubmissionWorkflowInstance = fixture.Instance, Question = "Provide the signed supporting schedule.",
        RaisedByUserId = fixture.Actor.Id, RaisedAt = raisedAt, ResponseDueAt = raisedAt.AddDays(3)
    };

    private static void AddRfiCreationEvidence(ApplicationDbContext context, Fixture fixture, PerformanceRfi rfi)
    {
        AddWorkflowAction(context, fixture, "OPMS_RFI.RAISE", WorkflowActionOutcome.RaiseRfi,
            rfi.Question, rfi.RaisedAt, "rfi-creation");
        context.AuditTrails.Add(NewAudit(fixture, nameof(PerformanceRfi), rfi.PublicId, "OPMS_RFI.RAISE",
            rfi.RaisedAt, new { rfi.Question, rfi.ResponseDueAt }, correlationId: "rfi-creation"));
    }

    private static void AddWorkflowAction(ApplicationDbContext context, Fixture fixture, string code,
        WorkflowActionOutcome outcome, string? comment, DateTime occurredAt, string correlationId)
    {
        context.SubmissionWorkflowActions.Add(new SubmissionWorkflowAction
        {
            MunicipalityId = fixture.Municipality.Id, SubmissionWorkflowInstance = fixture.Instance,
            Sequence = fixture.Instance.NextSequence++, FromStageId = fixture.Instance.CurrentStageId,
            ToStageId = fixture.Instance.CurrentStageId, ActionCode = code, Outcome = outcome,
            ActorUserId = fixture.Actor.Id, Comment = comment, CorrelationId = correlationId, OccurredAt = occurredAt
        });
    }

    private static AuditTrail NewAudit(Fixture fixture, string entityName, Guid entityId, string action,
        DateTime changedAt, object newValue, string? reason = null, string? correlationId = null) => new()
    {
        MunicipalityId = fixture.Municipality.Id, EntityName = entityName, EntityId = entityId.ToString(),
        Action = action, NewValue = System.Text.Json.JsonSerializer.Serialize(newValue),
        ChangedBy = fixture.Actor.Id, ChangedAt = changedAt, Reason = reason, CorrelationId = correlationId
    };

    private sealed record Fixture(Municipality Municipality, ApplicationUser Actor,
        MunicipalityFinancialYear Year, SubmissionWorkflowInstance Instance);
}

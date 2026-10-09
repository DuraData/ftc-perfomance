namespace FTCERP.Tests;

public sealed class GovernedLedgerImmutabilityTests
{
    public static TheoryData<object, EntityState> ImmutableLedgerMutations
    {
        get
        {
            var rows = new object[]
            {
                new OpmsTargetTemplateVersion(),
                new IpmsTargetTemplateVersion(),
                new DueDateExtension(),
                new ReviewComment(),
                new AuditFinding(),
                new SubmissionScore()
            };
            var cases = new TheoryData<object, EntityState>();
            foreach (var row in rows)
            {
                cases.Add(row, EntityState.Modified);
                cases.Add(Activator.CreateInstance(row.GetType())!, EntityState.Deleted);
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(ImmutableLedgerMutations))]
    public async Task Snapshot_and_submission_ledgers_cannot_be_rewritten_or_deleted(object row, EntityState state)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(row).State = state;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task Idp_plan_version_business_content_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var version = new IdpPlanVersion
        {
            Id = 41,
            VersionLabel = "Original council version",
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10)
        };
        context.Attach(version);
        version.VersionLabel = "Rewritten version";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-preserved*");
    }

    [Fact]
    public async Task Idp_plan_versions_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new IdpPlanVersion { Id = 42 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IDP plan-version history is append-only*");
    }

    public static TheoryData<object, string> AssignmentBusinessFieldRewrites => new()
    {
        { new UserScope { Id = 51, IsActive = true }, nameof(UserScope.ScopeType) },
        { new UserAssignment { Id = 52, IsActive = true }, nameof(UserAssignment.AssignmentType) },
        { new SecurityUserRoleAssignment { Id = 53, IsActive = true }, nameof(SecurityUserRoleAssignment.RoleId) },
        { new EmployeeAssignment { Id = 54, IsActive = true }, nameof(EmployeeAssignment.PositionName) }
    };

    [Theory]
    [MemberData(nameof(AssignmentBusinessFieldRewrites))]
    public async Task Effective_dated_assignment_business_fields_cannot_be_rewritten(object row, string propertyName)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Attach(row);
        context.Entry(row).Property(propertyName).IsModified = true;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-preserved*");
    }

    public static TheoryData<object> AssignmentDeletionCases => new()
    {
        new UserScope { Id = 61 },
        new UserAssignment { Id = 62 },
        new SecurityUserRoleAssignment { Id = 63 },
        new EmployeeAssignment { Id = 64 }
    };

    [Theory]
    [MemberData(nameof(AssignmentDeletionCases))]
    public async Task Effective_dated_assignment_history_cannot_be_deleted(object row)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(row).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    public static TheoryData<object> Circular88CatalogueDefinitions => new()
    {
        new C88CatalogueItem { Id = 71 },
        new C88Indicator { Id = 72 },
        new C88DataElement { Id = 73 },
        new C88IndicatorApplicability { Id = 74 },
        new C88ComplianceQuestion { Id = 75 }
    };

    [Theory]
    [MemberData(nameof(Circular88CatalogueDefinitions))]
    public async Task Circular88_catalogue_definitions_cannot_be_rewritten(object row)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(row).State = EntityState.Modified;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*catalogue definitions are append-only*");
    }

    [Fact]
    public async Task Published_Circular88_catalogue_edition_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var edition = new C88CatalogueVersion { Id = 76, IsPublished = true };
        context.Attach(edition);
        edition.Name = "Retrospectively rewritten edition";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*catalogue editions are immutable*");
    }

    public static TheoryData<object, string> NotificationBusinessFieldRewrites => new()
    {
        { new BusinessEventOutbox { Id = 81 }, nameof(BusinessEventOutbox.Payload) },
        { new NotificationDeliveryAttempt { Id = 82 }, nameof(NotificationDeliveryAttempt.RecipientUserId) }
    };

    [Theory]
    [MemberData(nameof(NotificationBusinessFieldRewrites))]
    public async Task Notification_ledger_identity_and_payload_cannot_be_rewritten(object row, string propertyName)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Attach(row);
        context.Entry(row).Property(propertyName).IsModified = true;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    public static TheoryData<object> NotificationDeletionCases => new()
    {
        new BusinessEventOutbox { Id = 83 },
        new NotificationDeliveryAttempt { Id = 84 }
    };

    [Theory]
    [MemberData(nameof(NotificationDeletionCases))]
    public async Task Notification_delivery_history_cannot_be_deleted(object row)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(row).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Official_report_job_request_identity_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var job = new OfficialReportJob { Id = 91 };
        context.Attach(job);
        context.Entry(job).Property(nameof(OfficialReportJob.ReportTemplateId)).IsModified = true;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*request identity is immutable*");
    }

    [Fact]
    public async Task Official_report_job_history_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new OfficialReportJob { Id = 92 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Official_report_schedule_configuration_requires_a_successor_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var schedule = new OfficialReportSchedule { Id = 93, IsCurrent = true, IsActive = true };
        context.Attach(schedule);
        schedule.Code = "RETROSPECTIVE-REWRITE";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*successor version*");
    }

    [Fact]
    public async Task Official_report_schedule_recurrence_cannot_move_backwards()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var originalNext = DateTime.UtcNow.AddDays(10);
        var schedule = new OfficialReportSchedule { Id = 94, IsCurrent = true, IsActive = true, NextRunAt = originalNext };
        context.Attach(schedule);
        schedule.NextRunAt = originalNext.AddDays(-1);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be reversed or moved backwards*");
    }

    [Fact]
    public async Task Notification_policy_definition_requires_a_successor_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var policy = new NotificationConfiguration { Id = 101, Lifecycle = NotificationPolicyLifecycle.Active };
        context.Attach(policy);
        policy.TitleTemplate = "Retrospectively rewritten template";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*successor version*");
    }

    [Fact]
    public async Task Working_calendar_history_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var holiday = new WorkingCalendarHoliday { Id = 102, Name = "Original holiday" };
        context.Attach(holiday);
        holiday.Name = "Rewritten holiday";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*working-calendar history*");
    }

    [Fact]
    public async Task Scheduled_notification_identity_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var scheduled = new ScheduledNotification { Id = 103, State = ScheduledNotificationState.Pending };
        context.Attach(scheduled);
        scheduled.RecipientUserId = "different-recipient";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*identity are immutable*");
    }

    [Fact]
    public async Task Scheduled_notification_lifecycle_cannot_be_reversed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var scheduled = new ScheduledNotification { Id = 104, State = ScheduledNotificationState.Queued, QueuedAt = DateTime.UtcNow };
        context.Attach(scheduled);
        scheduled.State = ScheduledNotificationState.Pending;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only advance once*");
    }

    [Fact]
    public async Task Internal_audit_configuration_definition_requires_a_successor_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var configuration = new InternalAuditAssessmentConfiguration
        {
            Id = 111,
            IsCurrent = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            Model = InternalAuditAssessmentModel.Detailed
        };
        context.Attach(configuration);
        configuration.Model = InternalAuditAssessmentModel.SatisfactoryNotSatisfactory;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-preserved*");
    }

    [Fact]
    public async Task Workflow_definition_content_requires_a_successor_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var definition = new WorkflowDefinition { Id = 112, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-10), Code = "ORIGINAL" };
        context.Attach(definition);
        definition.Code = "REWRITTEN";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*successor version*");
    }

    [Fact]
    public async Task Workflow_stage_definition_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var stage = new WorkflowStageDefinition { Id = 113, Code = "REVIEW", Sequence = 1 };
        context.Attach(stage);
        stage.RequiredPermissionCode = "BYPASS.PERMISSION";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*stage-definition versions are append-only*");
    }

    [Fact]
    public async Task Workflow_definition_history_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new WorkflowDefinition { Id = 114 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Reporting_window_definition_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var window = new ReportingWindow { Id = 121, OpensAt = DateTime.UtcNow.AddDays(-1), ClosesAt = DateTime.UtcNow.AddDays(1) };
        context.Attach(window);
        window.ClosesAt = window.ClosesAt.AddDays(10);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Reporting-window history is append-only*");
    }

    [Fact]
    public async Task Reporting_window_exception_approval_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new ReportingWindowException { Id = 122 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exception approvals are append-only*");
    }

    [Fact]
    public async Task Rating_scheme_definition_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var scheme = new RatingScheme { Id = 123, Code = "FIVE_POINT", Name = "Five point scale" };
        context.Attach(scheme);
        scheme.Name = "Rewritten scale";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rating-scheme definitions are append-only*");
    }

    [Fact]
    public async Task Rating_scheme_value_band_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new RatingSchemeValue { Id = 124 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*value bands are append-only*");
    }

    [Fact]
    public async Task Rfi_question_and_due_date_evidence_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var rfi = new PerformanceRfi
        {
            Id = 125,
            Question = "Original governed question",
            RaisedAt = DateTime.UtcNow.AddDays(-1),
            ResponseDueAt = DateTime.UtcNow.AddDays(2)
        };
        context.Attach(rfi);
        rfi.Question = "Retrospectively rewritten question";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*question, raiser and due-date evidence are immutable*");
    }

    [Fact]
    public async Task Rfi_history_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new PerformanceRfi { Id = 126 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RFI history cannot be hard deleted*");
    }

    [Fact]
    public async Task Rfi_response_cannot_be_rewritten_or_cleared()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var respondedAt = DateTime.UtcNow.AddHours(-1);
        var rfi = new PerformanceRfi
        {
            Id = 127,
            Question = "Governed question",
            RaisedAt = respondedAt.AddHours(-1),
            ResponseDueAt = respondedAt.AddDays(1),
            Response = "Original response",
            RespondedByUserId = "original-responder",
            RespondedAt = respondedAt
        };
        context.Attach(rfi);
        rfi.Response = "Rewritten response";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*recorded exactly once*");
    }

    [Fact]
    public async Task Rfi_closure_cannot_be_reversed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var respondedAt = DateTime.UtcNow.AddHours(-2);
        var rfi = new PerformanceRfi
        {
            Id = 128,
            Question = "Governed question",
            RaisedAt = respondedAt.AddHours(-1),
            ResponseDueAt = respondedAt.AddDays(1),
            Response = "Governed response",
            RespondedByUserId = "responder",
            RespondedAt = respondedAt,
            ClosedByUserId = "closer",
            ClosedAt = respondedAt.AddHours(1)
        };
        context.Attach(rfi);
        rfi.ClosedByUserId = null;
        rfi.ClosedAt = null;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*closed exactly once after response*");
    }

    [Fact]
    public async Task Workflow_instance_pinned_definition_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var instance = new SubmissionWorkflowInstance
        {
            Id = 129,
            WorkflowDefinitionId = 10,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "submission-129",
            CurrentStageId = 20,
            NextSequence = 3
        };
        context.Attach(instance);
        instance.WorkflowDefinitionId = 11;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pinned definition are immutable*");
    }

    [Fact]
    public async Task Workflow_instance_history_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new SubmissionWorkflowInstance { Id = 130 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*workflow-instance history cannot be hard deleted*");
    }

    [Fact]
    public async Task Workflow_instance_sequence_cannot_advance_without_matching_actions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var instance = new SubmissionWorkflowInstance
        {
            Id = 131,
            WorkflowDefinitionId = 10,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "submission-131",
            CurrentStageId = 20,
            NextSequence = 3
        };
        context.Attach(instance);
        instance.NextSequence = 8;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exact append-only actions*");
    }

    [Fact]
    public async Task Terminal_workflow_instance_cannot_be_reopened()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var completedAt = DateTime.UtcNow.AddHours(-1);
        var instance = new SubmissionWorkflowInstance
        {
            Id = 132,
            WorkflowDefinitionId = 10,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "submission-132",
            State = WorkflowInstanceState.Completed,
            CurrentStageId = null,
            CompletedAt = completedAt,
            NextSequence = 3
        };
        context.Attach(instance);
        instance.State = WorkflowInstanceState.Active;
        instance.CurrentStageId = 20;
        instance.CompletedAt = null;
        instance.NextSequence = 4;
        context.SubmissionWorkflowActions.Add(new SubmissionWorkflowAction
        {
            MunicipalityId = instance.MunicipalityId,
            SubmissionWorkflowInstanceId = instance.Id,
            SubmissionWorkflowInstance = instance,
            Sequence = 3,
            FromStageId = null,
            ToStageId = 20,
            ActionCode = "REOPEN",
            ActorUserId = "actor"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*terminal workflow instance cannot be reopened*");
    }

    [Fact]
    public async Task Official_report_template_definition_requires_a_successor_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var template = new OfficialReportTemplate
        {
            Id = 133,
            TemplateFamilyPublicId = Guid.NewGuid(),
            Code = "QUARTERLY",
            Name = "Approved quarterly report",
            EffectiveFrom = DateTime.UtcNow.AddDays(-10)
        };
        context.Attach(template);
        template.Name = "Retrospectively rewritten report";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*definition changes require a successor version*");
    }

    [Fact]
    public async Task Official_report_template_history_cannot_be_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.Entry(new OfficialReportTemplate { Id = 134 }).State = EntityState.Deleted;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*template-version history cannot be hard deleted*");
    }

    [Fact]
    public async Task Official_report_template_cannot_be_closed_without_its_exact_successor()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var template = new OfficialReportTemplate
        {
            Id = 135,
            TemplateFamilyPublicId = Guid.NewGuid(),
            VersionNumber = 1,
            IsCurrent = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10)
        };
        context.Attach(template);
        template.IsCurrent = false;
        template.EffectiveTo = DateTime.UtcNow.AddDays(-1);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exact chronological successor version*");
    }

    [Fact]
    public async Task Authentication_configuration_definition_requires_a_successor_version()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var configuration = new AuthenticationConfiguration
        {
            Id = 136,
            ConfigurationFamilyPublicId = Guid.NewGuid(),
            VersionNumber = 1,
            IsCurrent = true,
            MunicipalityId = 1,
            Mode = AuthenticationMode.Local,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            CreatedByUserId = "actor"
        };
        context.Attach(configuration);
        configuration.Mode = AuthenticationMode.Hybrid;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*definition changes require a successor version*");
    }

    [Fact]
    public async Task Authentication_policy_history_cannot_be_rewritten_or_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var policy = new AuthenticationPolicy
        {
            Id = 137,
            MunicipalityId = 1,
            AuthenticationConfigurationId = 136,
            MinimumPasswordLength = 12,
            ModifiedByUserId = "actor"
        };
        context.Attach(policy);
        policy.MinimumPasswordLength = 18;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Authentication policy history is append-only*");
    }

    [Fact]
    public async Task Authentication_configuration_cannot_be_closed_without_its_exact_successor()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var configuration = new AuthenticationConfiguration
        {
            Id = 138,
            ConfigurationFamilyPublicId = Guid.NewGuid(),
            VersionNumber = 1,
            IsCurrent = true,
            MunicipalityId = 1,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            CreatedByUserId = "actor"
        };
        context.Attach(configuration);
        configuration.IsCurrent = false;
        configuration.EffectiveTo = DateTime.UtcNow.AddTicks(-1);
        configuration.ModifiedByUserId = "actor";
        configuration.ModifiedAt = DateTime.UtcNow;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exact chronological, actor-stamped successor version*");
    }

    [Fact]
    public async Task Enterprise_authenticator_identity_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var authenticator = new UserAuthenticator
        {
            Id = 139,
            MunicipalityId = 1,
            UserId = "user",
            ProviderRegistrationCode = "ENTRA",
            ExpectedEmail = "original@example.test",
            CreatedByUserId = "actor"
        };
        context.Attach(authenticator);
        authenticator.ExpectedEmail = "replacement@example.test";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expected email and creation evidence are immutable*");
    }

    [Fact]
    public async Task Enterprise_authenticator_last_use_requires_append_only_success_event()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var authenticator = new UserAuthenticator
        {
            Id = 140,
            MunicipalityId = 1,
            UserId = "user",
            ProviderRegistrationCode = "ENTRA",
            ExpectedEmail = "user@example.test",
            CreatedByUserId = "actor",
            LastAuthenticatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Attach(authenticator);
        authenticator.LastAuthenticatedAt = DateTime.UtcNow;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only successful authentication event*");
    }

    [Fact]
    public async Task Enterprise_authenticator_status_requires_governance_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var authenticator = new UserAuthenticator
        {
            Id = 141,
            MunicipalityId = 1,
            UserId = "user",
            ProviderRegistrationCode = "ENTRA",
            ExpectedEmail = "user@example.test",
            CreatedByUserId = "actor",
            IsActive = true
        };
        context.Attach(authenticator);
        authenticator.IsActive = false;
        authenticator.DisabledByUserId = "actor";
        authenticator.DisabledAt = DateTime.UtcNow;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only governance audit evidence*");
    }
}

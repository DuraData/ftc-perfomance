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
}

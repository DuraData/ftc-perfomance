namespace FTCERP.Tests;

public class TenantIsolationTests
{
    [Fact]
    public async Task TenantQueryFilter_ExcludesOtherMunicipalitiesAndUnassignedLegacyRows()
    {
        var database = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options;
        await using (var system = new ApplicationDbContext(options))
        {
            system.Municipalities.AddRange(new Municipality { Id = 1, Code = "A", Name = "A" }, new Municipality { Id = 2, Code = "B", Name = "B" });
            system.Departments.AddRange(
                new Department { Id = 10, MunicipalityId = 1, Code = "FIN", Name = "Finance" },
                new Department { Id = 20, MunicipalityId = 2, Code = "FIN", Name = "Finance" },
                new Department { Id = 30, Code = "LEGACY", Name = "Needs reconciliation" });
            system.PerformancePeriodTargets.AddRange(
                new PerformancePeriodTarget { Id = 1, MunicipalityId = 1, ReportingPeriodId = 1, OpmsTargetId = "A", TargetValue = "10", CreatedByUserId = "user" },
                new PerformancePeriodTarget { Id = 2, MunicipalityId = 2, ReportingPeriodId = 2, OpmsTargetId = "B", TargetValue = "20", CreatedByUserId = "user" });
            system.EvidenceBlobs.AddRange(
                new EvidenceBlob { Id = "blob-a", MunicipalityId = 1, StorageKey = "poe/a.pdf", Sha256 = new string('a', 64) },
                new EvidenceBlob { Id = "blob-b", MunicipalityId = 2, StorageKey = "poe/b.pdf", Sha256 = new string('b', 64) },
                new EvidenceBlob { Id = "blob-legacy", StorageKey = "poe/legacy.pdf", Sha256 = new string('c', 64) });
            await system.SaveChangesAsync();
        }

        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(1));
        var rows = await tenant.Departments.AsNoTracking().ToArrayAsync();

        rows.Should().ContainSingle(item => item.Id == 10);
        rows.Should().NotContain(item => item.Id == 20 || item.Id == 30);
        (await tenant.PerformancePeriodTargets.CountAsync()).Should().Be(1);
        (await tenant.EvidenceBlobs.Select(item => item.Id).ToArrayAsync()).Should().Equal("blob-a");
    }

    [Fact]
    public async Task TenantWriteGuard_RejectsCrossMunicipalityInsert()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(1));
        tenant.Departments.Add(new Department { MunicipalityId = 2, Code = "BAD", Name = "Cross tenant" });

        var action = () => tenant.SaveChangesAsync();

        await action.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Cross-municipality*");
    }

    [Fact]
    public async Task TenantWriteGuard_StampsNewTenantOwnedRows()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(7));
        var department = new Department { Code = "CORP", Name = "Corporate Services" };
        tenant.Departments.Add(department);

        await tenant.SaveChangesAsync();

        department.MunicipalityId.Should().Be(7);
    }

    [Fact]
    public async Task EvidenceBlob_CanBackMultipleAssociations_WithoutDuplicatingPhysicalMetadata()
    {
        await using var context = IdpTestFixture.CreateContext();
        var blob = new EvidenceBlob { Id = "blob-shared", MunicipalityId = 7, StorageKey = "poe/shared.pdf", Sha256 = new string('d', 64), SignatureVerified = true, ScanStatus = "Clean" };
        context.PoeFiles.AddRange(
            new PoeFile { Id = "association-1", MunicipalityId = 7, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-1", FileName = "first.pdf", Blob = blob, UploadedByUserId = "user" },
            new PoeFile { Id = "association-2", MunicipalityId = 7, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-2", FileName = "second.pdf", Blob = blob, UploadedByUserId = "user" });
        await context.SaveChangesAsync();

        (await context.EvidenceBlobs.CountAsync()).Should().Be(1);
        var associations = await context.PoeFiles.Include(item => item.Blob).OrderBy(item => item.Id).ToArrayAsync();
        associations.Should().HaveCount(2);
        associations.Select(item => item.EvidenceBlobId).Distinct().Should().Equal("blob-shared");
        associations.Should().OnlyContain(item => item.Blob.Sha256 == new string('d', 64));
    }

    [Fact]
    public async Task PerformanceTargetRevisionHistory_IsAppendOnly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(7));
        var revision = new PerformanceTargetRevision
        {
            MunicipalityId = 7,
            PerformancePeriodTargetId = 10,
            FieldName = "TargetValue",
            OriginalValue = "10",
            RevisedValue = "12",
            Reason = "Approved adjustment",
            ApprovalReference = "COUNCIL-2026-10",
            EffectiveAt = DateTime.UtcNow,
            RevisedByUserId = "user"
        };
        tenant.PerformanceTargetRevisions.Add(revision);
        await tenant.SaveChangesAsync();
        revision.Reason = "Changed after approval";

        var action = () => tenant.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task AuditHistory_IsTenantStampedAndAppendOnly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(7));
        var audit = new AuditTrail { EntityName = "OpmsSubmission", EntityId = "submission-1", Action = "Submit", ChangedBy = "user" };
        tenant.AuditTrails.Add(audit);
        await tenant.SaveChangesAsync();

        audit.MunicipalityId.Should().Be(7);
        audit.Action = "Changed";
        var action = () => tenant.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task QueuedNotification_StagesTenantNotificationAndOutboxTogether()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(7));
        var governance = new WorkflowGovernanceService(tenant);

        governance.QueueWorkflowNotifications(["recipient"], NotificationType.Approval, "Approved", "Submission approved.", "OpmsSubmission", "submission-1");
        await tenant.SaveChangesAsync();

        var notification = await tenant.Notifications.SingleAsync();
        var outbox = await tenant.BusinessEventOutbox.SingleAsync();
        notification.MunicipalityId.Should().Be(7);
        outbox.MunicipalityId.Should().Be(7);
        outbox.EventType.Should().Be("Notification.Approval");
        outbox.Payload.Should().Contain("recipient");
    }

    [Fact]
    public async Task IdpAggregateFilters_ExcludeOtherTenantsAndLegacyRows()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var system = new ApplicationDbContext(options))
        {
            var planA = new IdpPlan { Id = 1, MunicipalityId = 1, PlanCode = "IDP", PlanTitle = "A", CreatedByUserId = "user" };
            var planB = new IdpPlan { Id = 2, MunicipalityId = 2, PlanCode = "IDP", PlanTitle = "B", CreatedByUserId = "user" };
            var legacy = new IdpPlan { Id = 3, PlanCode = "LEGACY", PlanTitle = "Legacy", CreatedByUserId = "user" };
            system.IdpPlans.AddRange(planA, planB, legacy);
            system.IdpStrategicOutcomes.AddRange(
                new IdpStrategicOutcome { Id = 10, IdpPlan = planA, Code = "A", Name = "A" },
                new IdpStrategicOutcome { Id = 20, IdpPlan = planB, Code = "B", Name = "B" },
                new IdpStrategicOutcome { Id = 30, IdpPlan = legacy, Code = "L", Name = "L" });
            await system.SaveChangesAsync();
        }

        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(1));
        (await tenant.IdpPlans.Select(x => x.Id).ToArrayAsync()).Should().Equal(1);
        (await tenant.IdpStrategicOutcomes.Select(x => x.Id).ToArrayAsync()).Should().Equal(10);
    }

    [Fact]
    public async Task MissingTenantSelection_CannotStampSentinelTenant()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var tenant = new ApplicationDbContext(options, new TestTenantContext(long.MinValue));
        tenant.IdpPlans.Add(new IdpPlan { PlanCode = "IDP", PlanTitle = "Invalid", CreatedByUserId = "user" });

        var action = () => tenant.SaveChangesAsync();

        await action.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*municipality context*");
    }

    private sealed class TestTenantContext(long municipalityId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => "tenant-test";
    }
}

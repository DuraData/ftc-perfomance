namespace FTCERP.Tests;

using System.Text.Json;

public sealed class SubmissionGovernanceTenantIsolationTests
{
    [Fact]
    public async Task Score_and_due_date_ledgers_require_same_transaction_parent_audits()
    {
        const long municipalityId = 9799;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.IsSystem).Returns(false);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = municipalityId, Code = "LEDGER-AUDIT", Name = "Ledger Audit Municipality" };
        var user = IdpTestFixture.CreateUser("ledger-audit-user");
        user.MunicipalityId = municipalityId;
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        context.SubmissionScores.Add(new SubmissionScore
        {
            MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-score",
            Score = 4, ScoredByUserId = user.Id
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*same-transaction reasoned before/after audit evidence*");

        context.ChangeTracker.Clear();
        context.DueDateExtensions.Add(new DueDateExtension
        {
            MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Ipms, SubmissionId = "submission-extension",
            OriginalDueDate = DateTime.UtcNow.Date, ExtendedDueDate = DateTime.UtcNow.Date.AddDays(2),
            Reason = "Governed extension", ApprovedByUserId = user.Id
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*same-transaction reasoned before/after audit evidence*");
    }

    [Fact]
    public async Task Tenant_write_guard_stamps_unscoped_ledger_rows_and_rejects_cross_municipality_rows()
    {
        const long municipalityId = 9800;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.IsSystem).Returns(false);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = municipalityId, Code = "LEDGER-WRITE", Name = "Ledger Write Municipality" };
        var user = IdpTestFixture.CreateUser("ledger-write-user");
        user.MunicipalityId = municipalityId;
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        AddLedgerRows(context, null, user.Id, "selected-submission");
        await context.SaveChangesAsync();

        context.ChangeTracker.Entries<DueDateExtension>().Single().Entity.MunicipalityId.Should().Be(municipalityId);
        context.ChangeTracker.Entries<ReviewComment>().Single().Entity.MunicipalityId.Should().Be(municipalityId);
        context.ChangeTracker.Entries<AuditFinding>().Single().Entity.MunicipalityId.Should().Be(municipalityId);
        context.ChangeTracker.Entries<SubmissionScore>().Single().Entity.MunicipalityId.Should().Be(municipalityId);

        context.DueDateExtensions.Add(new DueDateExtension
        {
            MunicipalityId = municipalityId + 1,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "foreign-submission",
            OriginalDueDate = DateTime.UtcNow,
            ExtendedDueDate = DateTime.UtcNow.AddDays(1),
            Reason = "Cross-tenant attempt",
            ApprovedByUserId = user.Id
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Cross-municipality writes*");
    }

    [Fact]
    public async Task Append_only_submission_governance_ledgers_are_filtered_by_selected_municipality()
    {
        long? selectedMunicipalityId = null;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(() => selectedMunicipalityId);
        tenant.SetupGet(item => item.IsSystem).Returns(true);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);

        var municipalityA = new Municipality { Id = 9801, Code = "LEDGER-A", Name = "Ledger Municipality A" };
        var municipalityB = new Municipality { Id = 9802, Code = "LEDGER-B", Name = "Ledger Municipality B" };
        var userA = IdpTestFixture.CreateUser("ledger-user-a");
        var userB = IdpTestFixture.CreateUser("ledger-user-b");
        userA.MunicipalityId = municipalityA.Id;
        userB.MunicipalityId = municipalityB.Id;
        context.AddRange(municipalityA, municipalityB, userA, userB);
        await context.SaveChangesAsync();

        AddLedgerRows(context, municipalityA.Id, userA.Id, "submission-a");
        AddLedgerRows(context, municipalityB.Id, userB.Id, "submission-b");
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        selectedMunicipalityId = municipalityA.Id;
        (await context.DueDateExtensions.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-a");
        (await context.ReviewComments.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-a");
        (await context.AuditFindings.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-a");
        (await context.SubmissionScores.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-a");

        selectedMunicipalityId = municipalityB.Id;
        (await context.DueDateExtensions.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-b");
        (await context.ReviewComments.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-b");
        (await context.AuditFindings.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-b");
        (await context.SubmissionScores.Select(item => item.SubmissionId).ToArrayAsync()).Should().Equal("submission-b");
    }

    private static void AddLedgerRows(ApplicationDbContext context, long? municipalityId, string userId, string submissionId)
    {
        var now = DateTime.UtcNow;
        context.AddRange(
            new DueDateExtension
            {
                MunicipalityId = municipalityId,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = submissionId,
                OriginalDueDate = now,
                ExtendedDueDate = now.AddDays(1),
                Reason = "Governed extension",
                ApprovedByUserId = userId,
                ApprovedAt = now
            },
            new ReviewComment
            {
                MunicipalityId = municipalityId,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = submissionId,
                Comment = "Governed review",
                CommentedByUserId = userId,
                CommentedAt = now
            },
            new AuditFinding
            {
                MunicipalityId = municipalityId,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = submissionId,
                Finding = "Governed finding",
                CreatedByUserId = userId,
                CreatedAt = now
            },
            new SubmissionScore
            {
                MunicipalityId = municipalityId,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = submissionId,
                Score = 3,
                ScoredByUserId = userId,
                ScoredAt = now
            },
            new AuditTrail
            {
                MunicipalityId = municipalityId,
                EntityName = nameof(OpmsSubmission),
                EntityId = submissionId,
                Action = "ExtendDueDate",
                OldValue = JsonSerializer.Serialize(new { DueDate = now }),
                NewValue = JsonSerializer.Serialize(new { DueDate = now.AddDays(1), Reason = "Governed extension" }),
                Reason = "Governed extension",
                ChangedBy = userId,
                ChangedAt = now
            },
            new AuditTrail
            {
                MunicipalityId = municipalityId,
                EntityName = nameof(OpmsSubmission),
                EntityId = submissionId,
                Action = "Score",
                NewValue = JsonSerializer.Serialize(new { Score = 3 }),
                Reason = "Governed score",
                ChangedBy = userId,
                ChangedAt = now
            });
    }
}

namespace FTCERP.Tests;

public sealed class SubmissionGovernanceTenantIsolationTests
{
    [Fact]
    public async Task Retired_score_and_orphan_due_date_ledgers_are_rejected()
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
            .WithMessage("*standalone submission-score ledger is retired*");

        context.ChangeTracker.Clear();
        context.DueDateExtensions.Add(new DueDateExtension
        {
            MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Ipms, SubmissionId = "submission-extension",
            OriginalDueDate = DateTime.UtcNow.Date, ExtendedDueDate = DateTime.UtcNow.Date.AddDays(2),
            Reason = "Governed extension", ApprovedByUserId = user.Id
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only alongside its exact governed parent-submission mutation*");
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

        context.ChangeTracker.Entries<ReviewComment>().Single().Entity.MunicipalityId.Should().Be(municipalityId);
        context.ChangeTracker.Entries<AuditFinding>().Single().Entity.MunicipalityId.Should().Be(municipalityId);

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
        await InsertHistoricalRetiredLedgersAsync(context, municipalityA.Id, userA.Id, "submission-a");
        await InsertHistoricalRetiredLedgersAsync(context, municipalityB.Id, userB.Id, "submission-b");
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
            });
    }

    private static async Task InsertHistoricalRetiredLedgersAsync(
        ApplicationDbContext context,
        long municipalityId,
        string userId,
        string submissionId)
    {
        var now = DateTime.UtcNow;
        var extensionId = Guid.NewGuid().ToString("N");
        var scoreId = Guid.NewGuid().ToString("N");
        var kind = (int)SubmissionKind.Opms;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO DueDateExtensions
                (Id, MunicipalityId, SubmissionKind, SubmissionId, OriginalDueDate, ExtendedDueDate, Reason, ApprovedByUserId, ApprovedAt)
            VALUES
                ({extensionId}, {municipalityId}, {kind}, {submissionId}, {now}, {now.AddDays(1)}, {"Preserved historical extension"}, {userId}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO SubmissionScores
                (Id, MunicipalityId, SubmissionKind, SubmissionId, Score, Notes, ScoredByUserId, ScoredAt)
            VALUES
                ({scoreId}, {municipalityId}, {kind}, {submissionId}, {3m}, {"Preserved historical score"}, {userId}, {now})
            """);
    }
}

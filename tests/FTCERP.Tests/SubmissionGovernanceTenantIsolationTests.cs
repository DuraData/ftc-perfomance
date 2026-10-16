namespace FTCERP.Tests;

public sealed class SubmissionGovernanceTenantIsolationTests
{
    [Fact]
    public async Task Review_comment_accepts_a_persisted_system_actor_in_the_selected_tenant()
    {
        const long municipalityId = 9798;
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.IsSystem).Returns(true);
        await using var context = IdpTestFixture.CreateRelationalContext(tenant.Object);
        var municipality = new Municipality { Id = municipalityId, Code = "LEDGER-PERSISTED-ACTOR", Name = "Persisted Actor Municipality" };
        var actor = IdpTestFixture.CreateUser("ledger-persisted-actor");
        tenant.SetupGet(item => item.UserId).Returns(actor.Id);
        var target = new IpmsTarget
        {
            Id = "persisted-actor-target", Municipality = municipality, IndicatorNumber = "IPMS-LEDGER-1",
            TargetName = "Persisted actor target", KpiDescription = "Valid review-comment persistence"
        };
        var submission = new IpmsSubmission
        {
            Id = "persisted-actor-submission", Municipality = municipality, IpmsTarget = target,
            Quarter = "ANNUAL", BaseState = SubmissionBaseStates.InProgress, Status = SubmissionBaseStates.InProgress,
            CreatedBy = actor.Id, CreatedAt = DateTime.UtcNow
        };
        context.AddRange(municipality, actor, target, submission);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedSubmission = await context.IpmsSubmissions.SingleAsync(item => item.Id == submission.Id);
        context.Entry(persistedSubmission).Property(item => item.RowVersion).IsModified = true;
        context.ReviewComments.Add(new ReviewComment
        {
            MunicipalityId = municipalityId,
            SubmissionKind = SubmissionKind.Ipms,
            SubmissionId = persistedSubmission.Id,
            Comment = "Governed submission comment",
            CommentedByUserId = actor.Id,
            CommentedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        (await context.ReviewComments.SingleAsync()).Comment.Should().Be("Governed submission comment");
    }

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
            .WithMessage("*exact governed parent-submission mutation*");

        context.ChangeTracker.Clear();
        context.AuditFindings.Add(new AuditFinding
        {
            MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Opms, SubmissionId = "submission-finding",
            Finding = "Retired standalone finding", CreatedByUserId = user.Id
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*standalone audit-finding ledger is retired*");
    }

    [Fact]
    public async Task Tenant_write_guard_rejects_cross_municipality_submission_governance_rows()
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

        context.ReviewComments.Add(new ReviewComment
        {
            MunicipalityId = municipalityId + 1,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "foreign-submission",
            Comment = "Cross-tenant attempt",
            CommentedByUserId = user.Id
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

        await InsertHistoricalSubmissionGovernanceLedgersAsync(context, municipalityA.Id, userA.Id, "submission-a");
        await InsertHistoricalSubmissionGovernanceLedgersAsync(context, municipalityB.Id, userB.Id, "submission-b");
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

    private static async Task InsertHistoricalSubmissionGovernanceLedgersAsync(
        ApplicationDbContext context,
        long municipalityId,
        string userId,
        string submissionId)
    {
        var now = DateTime.UtcNow;
        var extensionId = Guid.NewGuid().ToString("N");
        var reviewId = Guid.NewGuid().ToString("N");
        var findingId = Guid.NewGuid().ToString("N");
        var scoreId = Guid.NewGuid().ToString("N");
        var kind = (int)SubmissionKind.Opms;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO DueDateExtensions
                (Id, MunicipalityId, SubmissionKind, SubmissionId, OriginalDueDate, ExtendedDueDate, Reason, ApprovedByUserId, ApprovedAt)
            VALUES
                ({extensionId}, {municipalityId}, {kind}, {submissionId}, {now}, {now.AddDays(1)}, {"Preserved historical extension"}, {userId}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ReviewComments
                (Id, MunicipalityId, SubmissionKind, SubmissionId, Comment, CommentedByUserId, CommentedAt)
            VALUES
                ({reviewId}, {municipalityId}, {kind}, {submissionId}, {"Preserved historical review"}, {userId}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO AuditFindings
                (Id, MunicipalityId, SubmissionKind, SubmissionId, Finding, Recommendation, CreatedByUserId, CreatedAt)
            VALUES
                ({findingId}, {municipalityId}, {kind}, {submissionId}, {"Preserved historical finding"}, {"Preserved recommendation"}, {userId}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO SubmissionScores
                (Id, MunicipalityId, SubmissionKind, SubmissionId, Score, Notes, ScoredByUserId, ScoredAt)
            VALUES
                ({scoreId}, {municipalityId}, {kind}, {submissionId}, {3m}, {"Preserved historical score"}, {userId}, {now})
            """);
    }
}

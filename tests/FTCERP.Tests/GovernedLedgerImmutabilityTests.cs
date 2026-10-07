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
}

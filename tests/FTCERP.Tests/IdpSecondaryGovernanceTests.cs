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
}

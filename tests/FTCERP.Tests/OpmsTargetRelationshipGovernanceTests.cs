namespace FTCERP.Tests;

public sealed class OpmsTargetRelationshipGovernanceTests
{
    [Fact]
    public async Task Relationship_creation_requires_link_actor_provenance()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeRelationships: false);
        context.OpmsTargetWards.Add(new OpmsTargetWard
        {
            MunicipalityId = seed.Municipality.Id,
            OpmsTargetId = seed.Target.Id,
            WardId = seed.Ward.Id
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A target relationship requires immutable tenant, target, actor and link-time provenance*");
    }

    [Theory]
    [InlineData(RelationshipKind.Ward)]
    [InlineData(RelationshipKind.Assignee)]
    [InlineData(RelationshipKind.VoteNumber)]
    public async Task Relationship_identity_rewrite_and_hard_delete_are_rejected(RelationshipKind kind)
    {
        await using var rewriteContext = IdpTestFixture.CreateRelationalContext();
        var rewriteSeed = await SeedAsync(rewriteContext);
        var relationship = Select(rewriteSeed, kind);
        relationship.OpmsTargetId = Guid.NewGuid().ToString("N");

        await FluentActions.Invoking(() => rewriteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Target relationship identity*immutable*");

        await using var deleteContext = IdpTestFixture.CreateRelationalContext();
        var deleteSeed = await SeedAsync(deleteContext);
        deleteContext.Remove(Select(deleteSeed, kind));

        await FluentActions.Invoking(() => deleteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Target relationship history cannot be hard deleted*");
    }

    [Fact]
    public async Task Exact_unlink_transition_is_allowed_and_advances_each_rowversion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var rows = new IOpmsTargetRelationship[] { seed.WardLink, seed.AssigneeLink, seed.VoteLink };
        var versions = rows.ToDictionary(row => row.PublicId, row => row.RowVersion.ToArray());
        var unlinkedAt = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.IsActive = false;
            row.UnlinkedAt = unlinkedAt;
            row.UnlinkedByUserId = seed.Actor.Id;
            row.UnlinkReason = "Governed responsibility realignment";
        }

        await context.SaveChangesAsync();

        rows.Should().OnlyContain(row => !row.IsActive
            && row.UnlinkedAt == unlinkedAt
            && row.UnlinkedByUserId == seed.Actor.Id
            && row.UnlinkReason == "Governed responsibility realignment");
        rows.Should().OnlyContain(row => !row.RowVersion.SequenceEqual(versions[row.PublicId]));
    }

    [Fact]
    public async Task Unlinked_relationship_cannot_be_reactivated_or_have_its_reason_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var row = seed.WardLink;
        row.IsActive = false;
        row.UnlinkedAt = DateTime.UtcNow;
        row.UnlinkedByUserId = seed.Actor.Id;
        row.UnlinkReason = "Approved ward reassignment";
        await context.SaveChangesAsync();

        row.UnlinkReason = "Rewritten explanation";
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A target relationship may transition exactly once*");

        row.IsActive = true;
        row.UnlinkedAt = null;
        row.UnlinkedByUserId = null;
        row.UnlinkReason = null;
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A target relationship may transition exactly once*");
    }

    [Fact]
    public async Task Relinking_appends_a_new_active_row_and_preserves_the_inactive_predecessor()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        seed.WardLink.IsActive = false;
        seed.WardLink.UnlinkedAt = DateTime.UtcNow;
        seed.WardLink.UnlinkedByUserId = seed.Actor.Id;
        seed.WardLink.UnlinkReason = "Temporary ward removal";
        await context.SaveChangesAsync();

        var successor = new OpmsTargetWard
        {
            MunicipalityId = seed.Municipality.Id,
            OpmsTargetId = seed.Target.Id,
            WardId = seed.Ward.Id,
            LinkedByUserId = seed.Actor.Id
        };
        context.OpmsTargetWards.Add(successor);
        await context.SaveChangesAsync();

        var history = await context.OpmsTargetWards.AsNoTracking().OrderBy(row => row.LinkedAt).ToArrayAsync();
        history.Should().HaveCount(2);
        history.Should().ContainSingle(row => row.IsActive && row.PublicId == successor.PublicId);
        history.Should().ContainSingle(row => !row.IsActive && row.UnlinkReason == "Temporary ward removal");
    }

    private static IOpmsTargetRelationship Select(Seed seed, RelationshipKind kind) => kind switch
    {
        RelationshipKind.Ward => seed.WardLink,
        RelationshipKind.Assignee => seed.AssigneeLink,
        RelationshipKind.VoteNumber => seed.VoteLink,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static async Task<Seed> SeedAsync(ApplicationDbContext context, bool includeRelationships = true)
    {
        var municipality = new Municipality { Code = $"REL-{Guid.NewGuid():N}"[..20], Name = "Relationship Governance" };
        var actor = IdpTestFixture.CreateUser($"relationship-actor-{Guid.NewGuid():N}");
        actor.Municipality = municipality;
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();

        var department = new Department { MunicipalityId = municipality.Id, Code = "GOV", Name = "Governance", IsActive = true };
        var ward = new Ward { MunicipalityId = municipality.Id, Code = "W01", Name = "Ward 1", IsActive = true };
        var target = new OpmsTarget
        {
            MunicipalityId = municipality.Id,
            IndicatorNumber = "REL-1",
            TargetName = "Governed relationships",
            KpiDescription = "Append-preserved relationship history",
            AnnualTargetDescription = "One"
        };
        context.AddRange(department, ward, target);
        await context.SaveChangesAsync();
        var vote = new VoteNumber
        {
            MunicipalityId = municipality.Id,
            DepartmentId = department.Id,
            Code = "V01",
            Number = "001",
            Name = "Governance Vote",
            IsActive = true
        };
        context.VoteNumbers.Add(vote);
        await context.SaveChangesAsync();

        var wardLink = new OpmsTargetWard { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, WardId = ward.Id, LinkedByUserId = actor.Id };
        var assigneeLink = new OpmsTargetAdditionalAssignee { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, UserId = actor.Id, LinkedByUserId = actor.Id };
        var voteLink = new OpmsTargetVoteNumber { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, VoteNumberId = vote.Id, LinkedByUserId = actor.Id };
        if (includeRelationships)
        {
            context.AddRange(wardLink, assigneeLink, voteLink);
            await context.SaveChangesAsync();
        }
        return new(municipality, actor, target, ward, wardLink, assigneeLink, voteLink);
    }

    public enum RelationshipKind { Ward, Assignee, VoteNumber }

    private sealed record Seed(Municipality Municipality, ApplicationUser Actor, OpmsTarget Target, Ward Ward,
        OpmsTargetWard WardLink, OpmsTargetAdditionalAssignee AssigneeLink, OpmsTargetVoteNumber VoteLink);
}

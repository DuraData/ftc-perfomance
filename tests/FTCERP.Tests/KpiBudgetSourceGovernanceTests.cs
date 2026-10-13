namespace FTCERP.Tests;

public sealed class KpiBudgetSourceGovernanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Allocation_creation_requires_actor_provenance(bool ipms)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        context.Add(NewAllocation(seed, ipms, includeActor: false));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A KPI budget-source allocation requires immutable tenant, target, source, amount, actor and allocation-time provenance*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Allocation_identity_rewrite_and_hard_delete_are_rejected(bool ipms)
    {
        await using var rewriteContext = IdpTestFixture.CreateRelationalContext();
        var rewriteSeed = await SeedAsync(rewriteContext);
        var allocation = NewAllocation(rewriteSeed, ipms);
        rewriteContext.Add(allocation);
        await rewriteContext.SaveChangesAsync();
        allocation.Amount = 999m;

        await FluentActions.Invoking(() => rewriteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("KPI budget-source allocation identity*immutable*");

        await using var deleteContext = IdpTestFixture.CreateRelationalContext();
        var deleteSeed = await SeedAsync(deleteContext);
        var deleted = NewAllocation(deleteSeed, ipms);
        deleteContext.Add(deleted);
        await deleteContext.SaveChangesAsync();
        deleteContext.Remove(deleted);

        await FluentActions.Invoking(() => deleteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("KPI budget-source allocation history cannot be hard deleted*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exact_deactivation_is_one_way_and_advances_rowversion(bool ipms)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var allocation = NewAllocation(seed, ipms);
        context.Add(allocation);
        await context.SaveChangesAsync();
        var version = allocation.RowVersion.ToArray();
        var deactivatedAt = DateTime.UtcNow;
        allocation.IsActive = false;
        allocation.DeactivatedAt = deactivatedAt;
        allocation.DeactivatedByUserId = seed.Actor.Id;
        allocation.DeactivationReason = "Approved funding-source reallocation";

        await context.SaveChangesAsync();

        allocation.IsActive.Should().BeFalse();
        allocation.DeactivatedAt.Should().Be(deactivatedAt);
        allocation.DeactivatedByUserId.Should().Be(seed.Actor.Id);
        allocation.RowVersion.Should().NotEqual(version);

        allocation.DeactivationReason = "Rewritten reason";
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A KPI budget-source allocation may deactivate exactly once*");
    }

    [Fact]
    public async Task Reallocation_appends_successor_and_preserves_predecessor()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var predecessor = (OpmsKpiBudgetSource)NewAllocation(seed, ipms: false);
        context.Add(predecessor);
        await context.SaveChangesAsync();
        predecessor.IsActive = false;
        predecessor.DeactivatedAt = DateTime.UtcNow;
        predecessor.DeactivatedByUserId = seed.Actor.Id;
        predecessor.DeactivationReason = "Approved allocation amount revision";
        var successor = new OpmsKpiBudgetSource
        {
            MunicipalityId = seed.Municipality.Id,
            OpmsTargetId = seed.OpmsTarget.Id,
            BudgetSourceId = seed.Source.Id,
            Amount = 200m,
            AllocatedByUserId = seed.Actor.Id
        };
        context.Add(successor);
        await context.SaveChangesAsync();

        var history = await context.OpmsKpiBudgetSources.AsNoTracking().OrderBy(item => item.CreatedAt).ToArrayAsync();
        history.Should().HaveCount(2);
        history.Should().ContainSingle(item => !item.IsActive && item.Amount == 100m && item.DeactivationReason == "Approved allocation amount revision");
        history.Should().ContainSingle(item => item.IsActive && item.Amount == 200m && item.AllocatedByUserId == seed.Actor.Id);
    }

    private static KpiBudgetSourceBase NewAllocation(Seed seed, bool ipms, bool includeActor = true) => ipms
        ? new IpmsKpiBudgetSource
        {
            MunicipalityId = seed.Municipality.Id,
            IpmsTargetId = seed.IpmsTarget.Id,
            BudgetSourceId = seed.Source.Id,
            Amount = 100m,
            AllocatedByUserId = includeActor ? seed.Actor.Id : null
        }
        : new OpmsKpiBudgetSource
        {
            MunicipalityId = seed.Municipality.Id,
            OpmsTargetId = seed.OpmsTarget.Id,
            BudgetSourceId = seed.Source.Id,
            Amount = 100m,
            AllocatedByUserId = includeActor ? seed.Actor.Id : null
        };

    private static async Task<Seed> SeedAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = $"BUD-{Guid.NewGuid():N}"[..20], Name = "Budget Governance" };
        var actor = IdpTestFixture.CreateUser($"budget-actor-{Guid.NewGuid():N}");
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();
        actor.MunicipalityId = municipality.Id;
        var source = new GovernedBudgetSource { MunicipalityId = municipality.Id, Code = "MIG", Name = "Infrastructure Grant" };
        var opmsTarget = new OpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "OPMS-BUD", TargetName = "OPMS Budget", KpiDescription = "OPMS governed allocation" };
        var ipmsTarget = new IpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "IPMS-BUD", TargetName = "IPMS Budget", KpiDescription = "IPMS governed allocation" };
        context.AddRange(source, opmsTarget, ipmsTarget);
        await context.SaveChangesAsync();
        return new(municipality, actor, source, opmsTarget, ipmsTarget);
    }

    private sealed record Seed(Municipality Municipality, ApplicationUser Actor, GovernedBudgetSource Source,
        OpmsTarget OpmsTarget, IpmsTarget IpmsTarget);
}

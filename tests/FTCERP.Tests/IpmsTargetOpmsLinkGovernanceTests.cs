using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;

namespace FTCERP.Tests;

public sealed class IpmsTargetOpmsLinkGovernanceTests
{
    [Fact]
    public async Task Sqlite_upgrade_backfills_existing_tenant_owned_pointer_as_active_history()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-ipms-link-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                var municipality = new Municipality { Code = "LINK-UPGRADE", Name = "Link Upgrade Municipality" };
                var opms = new OpmsTarget { Municipality = municipality, IndicatorNumber = "OPMS-LEGACY", TargetName = "Legacy OPMS", KpiDescription = "Legacy related target" };
                var ipms = new IpmsTarget { Municipality = municipality, IndicatorNumber = "IPMS-LEGACY", TargetName = "Legacy IPMS", KpiDescription = "Legacy pointer" };
                context.AddRange(municipality, opms, ipms);
                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"IpmsTargets\" SET \"RelatedOpmsTargetId\" = {0} WHERE \"Id\" = {1}", opms.Id, ipms.Id);
                context.ChangeTracker.Clear();

                await context.Database.ExecuteSqlRawAsync("DROP TABLE \"IPMS_TargetOpmsLinks\"");
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    )
                    """);
                const string latestMigration = "20261013123035_V39AppendPreservedIpmsOpmsTargetLinks";
                foreach (var migration in context.Database.GetMigrations().Where(item => item != latestMigration))
                    await context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                        migration, "10.0.0");

                var migrator = context.GetService<IMigrator>();
                await migrator.MigrateAsync(latestMigration);

                var link = await context.IpmsTargetOpmsLinks.AsNoTracking().SingleAsync();
                link.MunicipalityId.Should().Be(municipality.Id);
                link.IpmsTargetId.Should().Be(ipms.Id);
                link.OpmsTargetId.Should().Be(opms.Id);
                link.LinkedByUserId.Should().BeNull("legacy actor identity must not be invented");
                link.IsActive.Should().BeTrue();
                link.RowVersion.Should().NotBeEmpty();
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Link_creation_requires_actor_provenance()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        seed.IpmsTarget.RelatedOpmsTargetId = seed.FirstOpmsTarget.Id;
        context.IpmsTargetOpmsLinks.Add(NewLink(seed, includeActor: false));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An IPMS-to-OPMS target link requires immutable tenant, targets, actor and link-time provenance*");
    }

    [Fact]
    public async Task Link_cannot_be_appended_without_the_matching_parent_pointer_change()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        context.IpmsTargetOpmsLinks.Add(NewLink(seed));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An IPMS-to-OPMS target link must be appended with the matching parent pointer*");
    }

    [Fact]
    public async Task Link_cannot_cross_municipality_boundaries_even_when_private_keys_are_supplied_directly()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var otherMunicipality = new Municipality { Code = $"IOL-X-{Guid.NewGuid():N}"[..20], Name = "Other Municipality" };
        context.Municipalities.Add(otherMunicipality);
        await context.SaveChangesAsync();
        var foreignTarget = new OpmsTarget
        {
            MunicipalityId = otherMunicipality.Id,
            IndicatorNumber = "OPMS-FOREIGN",
            TargetName = "Foreign OPMS",
            KpiDescription = "Must not be linked"
        };
        context.OpmsTargets.Add(foreignTarget);
        await context.SaveChangesAsync();
        seed.IpmsTarget.RelatedOpmsTargetId = foreignTarget.Id;
        context.IpmsTargetOpmsLinks.Add(NewLink(seed, foreignTarget));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("IPMS-to-OPMS target links cannot cross municipality boundaries.");
    }

    [Fact]
    public async Task Link_identity_rewrite_and_hard_delete_are_rejected()
    {
        await using var rewriteContext = IdpTestFixture.CreateRelationalContext();
        var rewriteSeed = await SeedAsync(rewriteContext, includeLink: true);
        rewriteSeed.Link!.OpmsTargetId = rewriteSeed.SecondOpmsTarget.Id;

        await FluentActions.Invoking(() => rewriteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IPMS-to-OPMS target-link identity*immutable*");

        await using var deleteContext = IdpTestFixture.CreateRelationalContext();
        var deleteSeed = await SeedAsync(deleteContext, includeLink: true);
        deleteContext.IpmsTargetOpmsLinks.Remove(deleteSeed.Link!);

        await FluentActions.Invoking(() => deleteContext.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IPMS-to-OPMS target-link history cannot be hard deleted*");
    }

    [Fact]
    public async Task Unlink_requires_the_matching_parent_pointer_change()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeLink: true);
        Close(seed);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An IPMS-to-OPMS target unlink must update the matching parent pointer*");
    }

    [Fact]
    public async Task Exact_unlink_and_successor_append_preserve_history_and_advance_rowversions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeLink: true);
        var linkVersion = seed.Link!.RowVersion.ToArray();
        var targetVersion = seed.IpmsTarget.RowVersion.ToArray();
        Close(seed);
        seed.IpmsTarget.RelatedOpmsTargetId = seed.SecondOpmsTarget.Id;
        var successor = NewLink(seed, seed.SecondOpmsTarget);
        context.IpmsTargetOpmsLinks.Add(successor);

        await context.SaveChangesAsync();

        seed.Link.IsActive.Should().BeFalse();
        seed.Link.UnlinkReason.Should().Be("Approved realignment");
        seed.Link.RowVersion.Should().NotEqual(linkVersion);
        seed.IpmsTarget.RowVersion.Should().NotEqual(targetVersion);
        var history = await context.IpmsTargetOpmsLinks.AsNoTracking().OrderBy(item => item.LinkedAt).ToArrayAsync();
        history.Should().HaveCount(2);
        history.Should().ContainSingle(item => !item.IsActive && item.OpmsTargetId == seed.FirstOpmsTarget.Id);
        history.Should().ContainSingle(item => item.IsActive && item.OpmsTargetId == seed.SecondOpmsTarget.Id);

        seed.Link.UnlinkReason = "Rewritten explanation";
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An IPMS-to-OPMS target link may transition exactly once*");
    }

    [Fact]
    public async Task Direct_parent_pointer_rewrite_without_link_history_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeLink: true);
        seed.IpmsTarget.RelatedOpmsTargetId = seed.SecondOpmsTarget.Id;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Changing an IPMS target's OPMS pointer requires closing its exact active relationship*");
    }

    private static void Close(Seed seed)
    {
        seed.Link!.IsActive = false;
        seed.Link.UnlinkedAt = DateTime.UtcNow;
        seed.Link.UnlinkedByUserId = seed.Actor.Id;
        seed.Link.UnlinkReason = "Approved realignment";
    }

    private static IpmsTargetOpmsLink NewLink(Seed seed, OpmsTarget? target = null, bool includeActor = true) => new()
    {
        MunicipalityId = seed.Municipality.Id,
        IpmsTargetId = seed.IpmsTarget.Id,
        OpmsTargetId = (target ?? seed.FirstOpmsTarget).Id,
        LinkedByUserId = includeActor ? seed.Actor.Id : null
    };

    private static async Task<Seed> SeedAsync(ApplicationDbContext context, bool includeLink = false)
    {
        var municipality = new Municipality { Code = $"IOL-{Guid.NewGuid():N}"[..20], Name = "IPMS Link Governance" };
        var actor = IdpTestFixture.CreateUser($"ipms-link-actor-{Guid.NewGuid():N}");
        actor.Municipality = municipality;
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();
        var first = new OpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "OPMS-LINK-1", TargetName = "First OPMS", KpiDescription = "First link target" };
        var second = new OpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "OPMS-LINK-2", TargetName = "Second OPMS", KpiDescription = "Second link target" };
        var ipms = new IpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "IPMS-LINK", TargetName = "Linked IPMS", KpiDescription = "Append-preserved link" };
        context.AddRange(first, second, ipms);
        await context.SaveChangesAsync();

        IpmsTargetOpmsLink? link = null;
        if (includeLink)
        {
            ipms.RelatedOpmsTargetId = first.Id;
            link = new IpmsTargetOpmsLink
            {
                MunicipalityId = municipality.Id,
                IpmsTargetId = ipms.Id,
                OpmsTargetId = first.Id,
                LinkedByUserId = actor.Id
            };
            context.IpmsTargetOpmsLinks.Add(link);
            await context.SaveChangesAsync();
        }

        return new(municipality, actor, first, second, ipms, link);
    }

    private sealed record Seed(Municipality Municipality, ApplicationUser Actor, OpmsTarget FirstOpmsTarget,
        OpmsTarget SecondOpmsTarget, IpmsTarget IpmsTarget, IpmsTargetOpmsLink? Link);
}

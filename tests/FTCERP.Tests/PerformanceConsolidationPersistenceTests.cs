using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class PerformanceConsolidationPersistenceTests
{
    [Fact]
    public async Task Relational_model_seeds_all_types_and_enforces_tenant_policy_uniqueness_and_rowversion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long municipalityId;

        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var definitions = await setup.PerformanceCalculationTypes.OrderBy(item => item.Id).ToArrayAsync();
            definitions.Select(item => item.Code).Should().Equal(
                "SUM", "AVERAGE", "LATEST_VALUE", "CUMULATIVE", "NON_CUMULATIVE",
                "REVERSE_CUMULATIVE", "REVERSE_NON_CUMULATIVE", "ZERO_BASED", "MANUAL");

            var municipality = new Municipality { Code = "CONSOLIDATE", Name = "Consolidation Municipality" };
            setup.Municipalities.Add(municipality);
            await setup.SaveChangesAsync();
            municipalityId = municipality.Id;
            var user = IdpTestFixture.CreateUser("consolidation-admin");
            user.MunicipalityId = municipalityId;
            setup.Users.Add(user);
            await setup.SaveChangesAsync();

            var policy = new MunicipalityConsolidationPolicy
            {
                MunicipalityId = municipalityId,
                CalculationTypeId = definitions.Single(item => item.Code == "NON_CUMULATIVE").Id,
                ConsolidationRule = PerformanceCalculationType.Average,
                MissingValuePolicy = ConsolidationMissingValuePolicy.Ignore,
                AllowDerivedTargetOverride = true,
                CreatedByUserId = user.Id
            };
            setup.MunicipalityConsolidationPolicies.Add(policy);
            await setup.SaveChangesAsync();
            policy.RowVersion.Should().NotBeEmpty();
        }

        await using var tenant = new ApplicationDbContext(options, new TenantContext(municipalityId, "consolidation-admin"));
        var visible = await tenant.MunicipalityConsolidationPolicies.SingleAsync();
        visible.ConsolidationRule.Should().Be(PerformanceCalculationType.Average);
        var calculationTypePublicId = await tenant.PerformanceCalculationTypes
            .Where(item => item.Id == visible.CalculationTypeId).Select(item => item.PublicId).SingleAsync();

        await using var stale = new ApplicationDbContext(options, new TenantContext(municipalityId, "consolidation-admin"));
        var stalePolicy = await stale.MunicipalityConsolidationPolicies.SingleAsync();
        var visibleBefore = PolicySnapshot(visible, calculationTypePublicId);
        visible.DeriveAnnualTarget = true;
        visible.ModifiedByUserId = "consolidation-admin";
        visible.ModifiedAt = DateTime.UtcNow;
        new WorkflowGovernanceService(tenant, tenantContext: new TenantContext(municipalityId, "consolidation-admin"))
            .QueueAuditTrailAt(nameof(MunicipalityConsolidationPolicy), visible.PublicId.ToString(), "Edit", visibleBefore,
                PolicySnapshot(visible, calculationTypePublicId), "consolidation-admin", null, "Enable annual derivation", visible.ModifiedAt!.Value);
        await tenant.SaveChangesAsync();
        var staleBefore = PolicySnapshot(stalePolicy, calculationTypePublicId);
        stalePolicy.AllowDerivedTargetOverride = false;
        stalePolicy.ModifiedByUserId = "consolidation-admin";
        stalePolicy.ModifiedAt = DateTime.UtcNow;
        new WorkflowGovernanceService(stale, tenantContext: new TenantContext(municipalityId, "consolidation-admin"))
            .QueueAuditTrailAt(nameof(MunicipalityConsolidationPolicy), stalePolicy.PublicId.ToString(), "Edit", staleBefore,
                PolicySnapshot(stalePolicy, calculationTypePublicId), "consolidation-admin", null, "Concurrent policy correction", stalePolicy.ModifiedAt!.Value);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());

        await using var duplicate = new ApplicationDbContext(options, new TenantContext(municipalityId, "consolidation-admin"));
        var existing = await duplicate.MunicipalityConsolidationPolicies.AsNoTracking().SingleAsync();
        duplicate.MunicipalityConsolidationPolicies.Add(new MunicipalityConsolidationPolicy
        {
            MunicipalityId = municipalityId,
            CalculationTypeId = existing.CalculationTypeId,
            ConsolidationRule = PerformanceCalculationType.LatestValue,
            CreatedByUserId = "consolidation-admin"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
    }

    [Fact]
    public async Task Tenant_filter_hides_other_municipality_policies()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long firstId;
        long secondId;

        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var first = new Municipality { Code = "POL-A", Name = "Policy A" };
            var second = new Municipality { Code = "POL-B", Name = "Policy B" };
            setup.AddRange(first, second);
            await setup.SaveChangesAsync();
            firstId = first.Id;
            secondId = second.Id;
            var firstUser = IdpTestFixture.CreateUser("policy-a"); firstUser.MunicipalityId = firstId;
            var secondUser = IdpTestFixture.CreateUser("policy-b"); secondUser.MunicipalityId = secondId;
            setup.AddRange(firstUser, secondUser);
            await setup.SaveChangesAsync();
            setup.MunicipalityConsolidationPolicies.AddRange(
                new() { MunicipalityId = firstId, CalculationTypeId = 1, CreatedByUserId = firstUser.Id },
                new() { MunicipalityId = secondId, CalculationTypeId = 1, CreatedByUserId = secondUser.Id });
            await setup.SaveChangesAsync();
        }

        await using var firstTenant = new ApplicationDbContext(options, new TenantContext(firstId, "policy-a"));
        await using var secondTenant = new ApplicationDbContext(options, new TenantContext(secondId, "policy-b"));
        (await firstTenant.MunicipalityConsolidationPolicies.SingleAsync()).MunicipalityId.Should().Be(firstId);
        (await secondTenant.MunicipalityConsolidationPolicies.SingleAsync()).MunicipalityId.Should().Be(secondId);
    }

    [Fact]
    public async Task Suggestion_history_is_append_only_and_submission_values_are_separate_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var opms = context.Model.FindEntityType(typeof(OpmsSubmission))!;
        opms.FindProperty(nameof(OpmsSubmission.SystemSuggestedActualPerformance)).Should().NotBeNull();
        opms.FindProperty(nameof(OpmsSubmission.ActualPerformance)).Should().NotBeNull();
        opms.FindProperty(nameof(OpmsSubmission.WasSystemSuggestionEdited)).Should().NotBeNull();
        opms.FindProperty(nameof(OpmsSubmission.SuggestionCalculationTypeId)).Should().NotBeNull();
        opms.FindProperty(nameof(OpmsSubmission.SuggestionGeneratedDate)).Should().NotBeNull();

        var history = new PerformanceSuggestionEvent { Id = 42 };
        context.Attach(history);
        context.Entry(history).State = EntityState.Modified;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        exception.Message.Should().Contain("suggestion history is append-only");
    }

    [Fact]
    public async Task CanonicalCalculationUnitAndDirectionDefinitions_RejectRuntimeRewritesAndDeletes()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();

        var calculation = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "SUM");
        calculation.Description = "Runtime rewrite";
        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*calculation type definitions are migration-managed and immutable*");

        context.ChangeTracker.Clear();
        var unit = await context.OpmsUnitDefinitions.SingleAsync(item => item.Code == "PERCENT");
        context.OpmsUnitDefinitions.Remove(unit);
        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*OPMS unit definitions are migration-managed and immutable*");

        context.ChangeTracker.Clear();
        var direction = await context.PerformanceDirectionDefinitions.SingleAsync(item => item.Code == "TARGET_OR_HIGHER");
        direction.EngineDirection = PerformanceDirection.LowerIsBetter;
        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*performance-direction definitions are migration-managed and immutable*");
    }

    [Fact]
    public async Task MunicipalityConsolidationPolicy_RejectsUnauditedRewriteAndHardDelete()
    {
        const long municipalityId = 6511;
        var user = IdpTestFixture.CreateUser("consolidation-governance");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityId, user.Id));
        var municipality = new Municipality { Id = municipalityId, Code = "CON-GOV", Name = "Consolidation Governance" };
        user.MunicipalityId = municipalityId;
        var policy = new MunicipalityConsolidationPolicy
        {
            MunicipalityId = municipalityId,
            CalculationTypeId = 1,
            CreatedByUserId = user.Id
        };
        context.AddRange(user, municipality, policy);
        await context.SaveChangesAsync();

        policy.ModifiedByUserId = user.Id;
        policy.ModifiedAt = DateTime.UtcNow;
        policy.DeriveAnnualTarget = true;
        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*one exact same-transaction reasoned before/after audit*");

        context.ChangeTracker.Clear();
        context.MunicipalityConsolidationPolicies.Remove(
            await context.MunicipalityConsolidationPolicies.SingleAsync(item => item.PublicId == policy.PublicId));
        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task MunicipalityConsolidationPolicy_RejectsIdentityAndForgedCalculationTypeAudits()
    {
        const long municipalityId = 6512;
        var user = IdpTestFixture.CreateUser("consolidation-root-governor");
        await using var context = IdpTestFixture.CreateRelationalContext(IdpTestFixture.Tenant(municipalityId, user.Id));
        var municipality = new Municipality { Id = municipalityId, Code = "CON-ROOT", Name = "Consolidation Root" };
        user.MunicipalityId = municipalityId;
        var definition = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "AVERAGE");
        var policy = new MunicipalityConsolidationPolicy
        {
            MunicipalityId = municipalityId,
            CalculationTypeId = definition.Id,
            ConsolidationRule = PerformanceCalculationType.Average,
            MissingValuePolicy = ConsolidationMissingValuePolicy.Ignore,
            CreatedByUserId = user.Id
        };
        context.AddRange(user, municipality, policy);
        await context.SaveChangesAsync();

        policy.PublicId = Guid.NewGuid();
        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*identity, calculation type, municipality ownership and creation evidence are immutable*");

        context.ChangeTracker.Clear();
        policy = await context.MunicipalityConsolidationPolicies.SingleAsync();
        var forgedCalculationTypePublicId = Guid.NewGuid();
        var before = PolicySnapshot(policy, forgedCalculationTypePublicId);
        policy.DeriveAnnualTarget = true;
        policy.ModifiedByUserId = user.Id;
        policy.ModifiedAt = DateTime.UtcNow;
        new WorkflowGovernanceService(context, tenantContext: IdpTestFixture.Tenant(municipalityId, user.Id))
            .QueueAuditTrail(nameof(MunicipalityConsolidationPolicy), policy.PublicId.ToString(), "Edit", before,
                PolicySnapshot(policy, forgedCalculationTypePublicId), user.Id, null, "Approved consolidation policy correction");

        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*one exact same-transaction reasoned before/after audit*");
        forgedCalculationTypePublicId.Should().NotBe(definition.PublicId);
    }

    [Fact]
    public async Task MunicipalityConsolidationPolicy_RejectsExactAuditAndMetadataFromADifferentActor()
    {
        const long municipalityId = 6513;
        var actor = IdpTestFixture.CreateUser("consolidation-context-actor");
        var forgedActor = IdpTestFixture.CreateUser("consolidation-forged-actor");
        actor.MunicipalityId = municipalityId;
        forgedActor.MunicipalityId = municipalityId;
        await using var context = IdpTestFixture.CreateRelationalContext(new TenantContext(municipalityId, actor.Id));
        var municipality = new Municipality { Id = municipalityId, Code = "CON-ACTOR", Name = "Consolidation Actor Governance" };
        var definition = await context.PerformanceCalculationTypes.SingleAsync(item => item.Code == "AVERAGE");
        var policy = new MunicipalityConsolidationPolicy
        {
            MunicipalityId = municipalityId,
            CalculationTypeId = definition.Id,
            ConsolidationRule = PerformanceCalculationType.Average,
            MissingValuePolicy = ConsolidationMissingValuePolicy.Ignore,
            CreatedByUserId = actor.Id
        };
        context.AddRange(municipality, actor, forgedActor, policy);
        await context.SaveChangesAsync();

        var before = PolicySnapshot(policy, definition.PublicId);
        policy.DeriveAnnualTarget = true;
        policy.ModifiedByUserId = forgedActor.Id;
        policy.ModifiedAt = DateTime.UtcNow;
        new WorkflowGovernanceService(context, tenantContext: new TenantContext(municipalityId, actor.Id))
            .QueueAuditTrailAt(nameof(MunicipalityConsolidationPolicy), policy.PublicId.ToString(), "Edit", before,
                PolicySnapshot(policy, definition.PublicId), forgedActor.Id, null,
                "Exact snapshots cannot authorize another actor", policy.ModifiedAt!.Value);

        (await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*complete update actor and timestamp evidence*");
    }

    private static object PolicySnapshot(MunicipalityConsolidationPolicy policy, Guid calculationTypePublicId) => new
    {
        CalculationTypePublicId = calculationTypePublicId,
        policy.ConsolidationRule,
        policy.MissingValuePolicy,
        policy.AllowDerivedTargetOverride,
        policy.DeriveAnnualTarget,
        policy.IsActive
    };

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "system";
    }

    private sealed class TenantContext(long municipalityId, string actorUserId) : ITenantContext
    {
        public long? MunicipalityId { get; } = municipalityId;
        public bool IsSystem => false;
        public string? UserId { get; } = actorUserId;
    }
}

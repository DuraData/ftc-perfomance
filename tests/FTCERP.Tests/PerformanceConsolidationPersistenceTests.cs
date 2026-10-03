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

        await using var stale = new ApplicationDbContext(options, new TenantContext(municipalityId, "consolidation-admin"));
        var stalePolicy = await stale.MunicipalityConsolidationPolicies.SingleAsync();
        visible.DeriveAnnualTarget = true;
        await tenant.SaveChangesAsync();
        stalePolicy.AllowDerivedTargetOverride = false;
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

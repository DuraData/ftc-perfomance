using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FTCERP.Tests;

public sealed class ControlledSeedSecurityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void RequiredSeedSecret_RejectsMissingAndWeakFallbacks(string? value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Password"] = value
        }).Build();

        var action = () => DbInitializer.GetRequiredSeedSecret(configuration, "Admin:Password");

        action.Should().Throw<InvalidOperationException>().WithMessage("*local/deployment secret*");
    }

    [Fact]
    public void RequiredSeedSecret_ReturnsExplicitStrongConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Password"] = "local-only-Strong9!"
        }).Build();

        DbInitializer.GetRequiredSeedSecret(configuration, "Admin:Password").Should().Be("local-only-Strong9!");
    }

    [Fact]
    public async Task Initial_user_security_seed_is_idempotent_and_never_replaces_administrator_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var user = new ApplicationUser
        {
            Id = "seed-user", UserName = "seed-user", NormalizedUserName = "SEED-USER",
            FirstName = "Seed", LastName = "User", SecurityStamp = Guid.NewGuid().ToString()
        };
        context.Users.Add(user);
        context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeType = ScopeType.Self });
        context.UserAssignments.Add(new UserAssignment
        {
            UserId = user.Id, AssignmentType = AssignmentType.TaskAssignee, TaskId = "administrator-task"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await DbInitializer.SeedInitialUserScopesAsync(context, user.Id,
            [new UserScope { ScopeType = ScopeType.System }]);
        await DbInitializer.SeedInitialUserAssignmentsAsync(context, user.Id,
            [new UserAssignment { AssignmentType = AssignmentType.ProjectAssignee, ProjectId = "seed-project" }]);
        await DbInitializer.SeedInitialUserScopesAsync(context, user.Id,
            [new UserScope { ScopeType = ScopeType.System }]);
        await DbInitializer.SeedInitialUserAssignmentsAsync(context, user.Id,
            [new UserAssignment { AssignmentType = AssignmentType.ProjectAssignee, ProjectId = "seed-project" }]);

        Assert.Equal(ScopeType.Self, (await context.UserScopes.SingleAsync()).ScopeType);
        Assert.Equal("administrator-task", (await context.UserAssignments.SingleAsync()).TaskId);

        context.UserAssignments.Remove(await context.UserAssignments.SingleAsync());
        var delete = async () => await context.SaveChangesAsync();
        await delete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "seed-test";
    }
}

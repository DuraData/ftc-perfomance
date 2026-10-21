using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

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

    [Fact]
    public async Task Department_seed_is_idempotent_and_ignores_tenant_owned_code_collisions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        var municipality = new Municipality { Code = "TENANT", Name = "Tenant Municipality" };
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();
        var tenantDepartment = new Department
        {
            MunicipalityId = municipality.Id,
            Code = "ITS",
            Name = "Administrator-owned Technical Directorate",
            Description = "Must not be rewritten by startup seeding"
        };
        context.Departments.Add(tenantDepartment);
        await context.SaveChangesAsync();
        context.Units.Add(new Unit
        {
            MunicipalityId = municipality.Id,
            DepartmentId = tenantDepartment.Id,
            Code = "ITS-ROADS",
            Name = "Administrator-owned Roads Unit"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await DbInitializer.SeedDepartmentsAndUnitsAsync(context);
        await context.Departments.IgnoreQueryFilters()
            .Where(item => item.MunicipalityId == null && item.Code == "ITS")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator-renamed Platform Directorate")
                .SetProperty(item => item.Description, "Administrator-owned platform description")
                .SetProperty(item => item.IsActive, false));
        await context.Units.IgnoreQueryFilters()
            .Where(item => item.MunicipalityId == null && item.Code == "ITS-ROADS")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator-renamed Platform Roads Unit")
                .SetProperty(item => item.IsActive, false));
        context.ChangeTracker.Clear();

        await DbInitializer.SeedDepartmentsAndUnitsAsync(context);
        await DbInitializer.SeedDepartmentsAndUnitsAsync(context);
        context.ChangeTracker.Clear();

        var tenantResult = await context.Departments.IgnoreQueryFilters()
            .SingleAsync(item => item.MunicipalityId == municipality.Id && item.Code == "ITS");
        tenantResult.Name.Should().Be("Administrator-owned Technical Directorate");
        tenantResult.Description.Should().Be("Must not be rewritten by startup seeding");
        (await context.Units.IgnoreQueryFilters()
            .SingleAsync(item => item.MunicipalityId == municipality.Id && item.Code == "ITS-ROADS"))
            .Name.Should().Be("Administrator-owned Roads Unit");
        var platformDepartmentResult = await context.Departments.IgnoreQueryFilters()
            .SingleAsync(item => item.MunicipalityId == null && item.Code == "ITS");
        platformDepartmentResult.Name.Should().Be("Administrator-renamed Platform Directorate");
        platformDepartmentResult.Description.Should().Be("Administrator-owned platform description");
        platformDepartmentResult.IsActive.Should().BeFalse();
        var platformUnitResult = await context.Units.IgnoreQueryFilters()
            .SingleAsync(item => item.MunicipalityId == null && item.Code == "ITS-ROADS");
        platformUnitResult.Name.Should().Be("Administrator-renamed Platform Roads Unit");
        platformUnitResult.IsActive.Should().BeFalse();
        (await context.Departments.IgnoreQueryFilters()
            .CountAsync(item => item.MunicipalityId == null && item.Code == "ITS")).Should().Be(1);
        (await context.Units.IgnoreQueryFilters()
            .CountAsync(item => item.MunicipalityId == null && item.Code == "ITS-ROADS")).Should().Be(1);
    }

    [Fact]
    public async Task Platform_target_seed_lookup_ignores_same_indicator_numbers_owned_by_municipalities()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        var municipality = new Municipality { Code = "TENANT", Name = "Tenant Municipality" };
        var period = new Period
        {
            Code = "2026/27",
            Name = "2026/2027 Financial Year",
            FiscalYear = "2026/2027",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2027, 6, 30)
        };
        context.AddRange(municipality, period);
        await context.SaveChangesAsync();

        context.OpmsTargets.AddRange(
            new OpmsTarget { IndicatorNumber = "FIN-001", TargetName = "Platform target", PeriodId = period.Id },
            new OpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "FIN-001", TargetName = "Tenant target", PeriodId = period.Id });
        context.IpmsTargets.AddRange(
            new IpmsTarget { IndicatorNumber = "EMP-001", TargetName = "Platform employee target", PeriodId = period.Id },
            new IpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "EMP-001", TargetName = "Tenant employee target", PeriodId = period.Id });
        await context.SaveChangesAsync();

        var opmsTargets = await DbInitializer.LoadPlatformOpmsTargetsAsync(context);
        var ipmsTargets = await DbInitializer.LoadPlatformIpmsTargetsAsync(context);

        opmsTargets.Should().ContainSingle().Which.Value.MunicipalityId.Should().BeNull();
        ipmsTargets.Should().ContainSingle().Which.Value.MunicipalityId.Should().BeNull();
    }

    [Fact]
    public async Task Lookup_seed_is_additive_and_preserves_administrator_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        var municipality = new Municipality { Code = "BLUE-HILLS", Name = "Blue Hills Municipality" };
        var financialYear = new FinancialYear
        {
            Code = "2024/25",
            Name = "2024/2025",
            StartDate = new DateTime(2024, 7, 1),
            EndDate = new DateTime(2025, 6, 30)
        };
        context.AddRange(municipality, financialYear);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id,
            FinancialYearId = financialYear.Id,
            IsCurrent = true,
            IsActive = true,
            EffectiveFrom = financialYear.StartDate
        };
        var departments = new[]
        {
            new Department { MunicipalityId = municipality.Id, Code = "ITS", Name = "Technical Services" },
            new Department { MunicipalityId = municipality.Id, Code = "COM", Name = "Community Services" },
            new Department { MunicipalityId = municipality.Id, Code = "CORP", Name = "Corporate Services" }
        };
        context.Add(municipalityYear);
        context.AddRange(departments);
        await context.SaveChangesAsync();
        var legacyVote = new VoteNumber
        {
            MunicipalityId = municipality.Id,
            MunicipalityFinancialYearId = null,
            Code = "V001",
            Number = "LEGACY-VOTE",
            Name = "Administrator-owned Historical Vote",
            DepartmentId = departments.Single(item => item.Code == "ITS").Id,
            Amount = 765432.10m,
            IsActive = false
        };
        context.VoteNumbers.Add(legacyVote);
        await context.SaveChangesAsync();

        await DbInitializer.SeedLookupTablesAsync(context);

        var alternateGoalId = await context.StrategicGoals.Where(item => item.Code == "SG1").Select(item => item.Id).SingleAsync();
        var alternateDepartmentId = departments.Single(item => item.Code == "COM").Id;
        await context.Periods.Where(item => item.Code == "2024/25")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Financial Period")
                .SetProperty(item => item.StartDate, new DateTime(2024, 8, 1))
                .SetProperty(item => item.EndDate, new DateTime(2025, 5, 31))
                .SetProperty(item => item.FiscalYear, "ADMIN-FY")
                .SetProperty(item => item.IsActive, false));
        await context.StrategicGoals.Where(item => item.Code == "SG2")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Strategic Goal")
                .SetProperty(item => item.Description, "Administrator goal description")
                .SetProperty(item => item.IsActive, false));
        await context.StrategicObjectives.Where(item => item.Code == "SO1.1")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Strategic Objective")
                .SetProperty(item => item.StrategicGoalId, alternateGoalId)
                .SetProperty(item => item.IsActive, false));
        await context.BudgetSources.Where(item => item.Code == "MIG")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Funding Source")
                .SetProperty(item => item.IsActive, false));
        await context.BudgetTypes.Where(item => item.Code == "CAPEX")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Budget Classification")
                .SetProperty(item => item.IsActive, false));
        await context.UnitOfMeasures.Where(item => item.Code == "KM")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Distance Unit")
                .SetProperty(item => item.Symbol, "admin-km")
                .SetProperty(item => item.IsActive, false));
        await context.Wards.IgnoreQueryFilters()
            .Where(item => item.MunicipalityId == municipality.Id && item.Code == "W1")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Name, "Administrator Ward One")
                .SetProperty(item => item.LegacyMunicipality, "Administrator Municipality Label")
                .SetProperty(item => item.IsActive, false));
        await context.VoteNumbers.IgnoreQueryFilters()
            .Where(item => item.MunicipalityId == municipality.Id &&
                           item.MunicipalityFinancialYearId == municipalityYear.Id &&
                           item.Code == "V001")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Number, "ADMIN-VOTE")
                .SetProperty(item => item.Name, "Administrator Roads Vote")
                .SetProperty(item => item.DepartmentId, alternateDepartmentId)
                .SetProperty(item => item.Amount, 123456.78m)
                .SetProperty(item => item.IsActive, false));
        context.ChangeTracker.Clear();

        await DbInitializer.SeedLookupTablesAsync(context);
        await DbInitializer.SeedLookupTablesAsync(context);
        context.ChangeTracker.Clear();

        var period = await context.Periods.SingleAsync(item => item.Code == "2024/25");
        period.Name.Should().Be("Administrator Financial Period");
        period.StartDate.Should().Be(new DateTime(2024, 8, 1));
        period.EndDate.Should().Be(new DateTime(2025, 5, 31));
        period.FiscalYear.Should().Be("ADMIN-FY");
        period.IsActive.Should().BeFalse();
        var goal = await context.StrategicGoals.SingleAsync(item => item.Code == "SG2");
        goal.Name.Should().Be("Administrator Strategic Goal");
        goal.Description.Should().Be("Administrator goal description");
        goal.IsActive.Should().BeFalse();
        var objective = await context.StrategicObjectives.SingleAsync(item => item.Code == "SO1.1");
        objective.Name.Should().Be("Administrator Strategic Objective");
        objective.StrategicGoalId.Should().Be(alternateGoalId);
        objective.IsActive.Should().BeFalse();
        (await context.BudgetSources.SingleAsync(item => item.Code == "MIG")).Should().Match<BudgetSource>(item => item.Name == "Administrator Funding Source" && !item.IsActive);
        (await context.BudgetTypes.SingleAsync(item => item.Code == "CAPEX")).Should().Match<BudgetType>(item => item.Name == "Administrator Budget Classification" && !item.IsActive);
        var unitResult = await context.UnitOfMeasures.SingleAsync(item => item.Code == "KM");
        unitResult.Name.Should().Be("Administrator Distance Unit");
        unitResult.Symbol.Should().Be("admin-km");
        unitResult.IsActive.Should().BeFalse();
        var wardResult = await context.Wards.IgnoreQueryFilters().SingleAsync(item => item.MunicipalityId == municipality.Id && item.Code == "W1");
        wardResult.Name.Should().Be("Administrator Ward One");
        wardResult.LegacyMunicipality.Should().Be("Administrator Municipality Label");
        wardResult.IsActive.Should().BeFalse();
        var voteResult = await context.VoteNumbers.IgnoreQueryFilters().SingleAsync(item =>
            item.MunicipalityId == municipality.Id &&
            item.MunicipalityFinancialYearId == municipalityYear.Id &&
            item.Code == "V001");
        voteResult.Number.Should().Be("ADMIN-VOTE");
        voteResult.Name.Should().Be("Administrator Roads Vote");
        voteResult.DepartmentId.Should().Be(departments.Single(item => item.Code == "COM").Id);
        voteResult.Amount.Should().Be(123456.78m);
        voteResult.IsActive.Should().BeFalse();
        var historicalVoteResult = await context.VoteNumbers.IgnoreQueryFilters().SingleAsync(item => item.Id == legacyVote.Id);
        historicalVoteResult.MunicipalityFinancialYearId.Should().BeNull();
        historicalVoteResult.Number.Should().Be("LEGACY-VOTE");
        historicalVoteResult.Name.Should().Be("Administrator-owned Historical Vote");
        historicalVoteResult.Amount.Should().Be(765432.10m);
        historicalVoteResult.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Initial_role_permission_seed_never_replaces_existing_dynamic_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var configuredRole = new ApplicationRole { Id = "configured-role", Name = "Configured", NormalizedName = "CONFIGURED", RoleCode = "CONFIGURED" };
        var emptyRole = new ApplicationRole { Id = "empty-role", Name = "Empty", NormalizedName = "EMPTY", RoleCode = "EMPTY" };
        var denied = new Permission { Code = "NAV.DENIED", Module = "Navigation", Feature = "Denied", Action = "View", Kind = SecurityPermissionKind.Navigation, IsActive = true };
        var baseline = new Permission { Code = "NAV.BASELINE", Module = "Navigation", Feature = "Baseline", Action = "View", Kind = SecurityPermissionKind.Navigation, IsActive = true };
        context.AddRange(configuredRole, emptyRole, denied, baseline);
        await context.SaveChangesAsync();
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = configuredRole.Id,
            PermissionId = denied.Id,
            IsAllowed = false,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        await DbInitializer.SeedInitialRolePermissionsAsync(context, configuredRole.Id, [baseline.Id]);
        await DbInitializer.SeedInitialRolePermissionsAsync(context, emptyRole.Id, [baseline.Id]);
        await DbInitializer.SeedInitialRolePermissionsAsync(context, emptyRole.Id, [denied.Id]);

        var configured = await context.RolePermissions.SingleAsync(item => item.RoleId == configuredRole.Id);
        configured.PermissionId.Should().Be(denied.Id);
        configured.IsAllowed.Should().BeFalse();
        var initialized = await context.RolePermissions.SingleAsync(item => item.RoleId == emptyRole.Id);
        initialized.PermissionId.Should().Be(baseline.Id);
        initialized.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Baseline_role_seed_does_not_rewrite_existing_or_disable_dynamic_roles()
    {
        var existing = new ApplicationRole { Id = "super", Name = BaselineSecurityRoleCatalog.SuperAdmin, NormalizedName = "SUPER ADMIN", RoleCode = "SUPER_ADMIN", Description = "Administrator configured", IsSystemRole = false, IsActive = false };
        var dynamicRole = new ApplicationRole { Id = "dynamic", Name = "Municipal Data Steward", NormalizedName = "MUNICIPAL DATA STEWARD", RoleCode = "DATA_STEWARD", IsActive = true };
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(store.Object, null!, null!, null!, null!);
        roleManager.Setup(manager => manager.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string roleName) => string.Equals(roleName, BaselineSecurityRoleCatalog.SuperAdmin, StringComparison.OrdinalIgnoreCase) ? existing : null);
        roleManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);
        roleManager.SetupGet(manager => manager.Roles).Returns(new[] { existing, dynamicRole }.AsQueryable());

        await DbInitializer.SeedRolesAsync(roleManager.Object);

        existing.Description.Should().Be("Administrator configured");
        existing.IsSystemRole.Should().BeFalse();
        existing.IsActive.Should().BeFalse();
        dynamicRole.IsActive.Should().BeTrue();
        roleManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task Security_catalogue_seed_is_additive_and_preserves_administrator_configuration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        await DbInitializer.SeedPermissionsAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);
        var userResource = await context.SecurityResources.SingleAsync(item => item.Code == "USER");
        var emailMember = await context.SecurityMemberDefinitions.SingleAsync(item => item.ResourceCode == "USER" && item.MemberCode == "Email");
        var actor = IdpTestFixture.CreateUser("catalogue-governance-actor");
        context.Users.Add(actor);
        await context.SaveChangesAsync();
        await context.Permissions.Where(item => item.Code == "Dashboard.View").ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Description, "Administrator-defined dashboard description")
            .SetProperty(item => item.IsActive, false));
        var resourceBefore = RegistrySnapshot(userResource);
        var memberBefore = RegistrySnapshot(emailMember);
        userResource.SupportsDelete = false;
        emailMember.IsSensitive = false;
        context.AuditTrails.AddRange(
            RegistryAudit(nameof(SecurityResource), userResource.PublicId, actor.Id, "Preserve administrator resource configuration", resourceBefore, RegistrySnapshot(userResource)),
            RegistryAudit(nameof(SecurityMemberDefinition), emailMember.PublicId, actor.Id, "Preserve administrator member configuration", memberBefore, RegistrySnapshot(emailMember)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await DbInitializer.SeedPermissionsAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);

        var dashboard = await context.Permissions.SingleAsync(item => item.Code == "Dashboard.View");
        dashboard.Description.Should().Be("Administrator-defined dashboard description");
        dashboard.IsActive.Should().BeFalse();
        (await context.SecurityResources.SingleAsync(item => item.Code == "USER")).SupportsDelete.Should().BeFalse();
        (await context.SecurityMemberDefinitions.SingleAsync(item => item.ResourceCode == "USER" && item.MemberCode == "Email")).IsSensitive.Should().BeFalse();
    }

    private static object RegistrySnapshot(object entity) => entity switch
    {
        SecurityResource resource => new
        {
            resource.Code, resource.Name, resource.ResourceType, resource.Description, resource.SupportsCreate,
            resource.SupportsRead, resource.SupportsUpdate, resource.SupportsDelete, resource.SupportsExport,
            resource.SupportsImport, resource.SupportsFieldSecurity, resource.SupportsRecordCriteria, resource.IsActive
        },
        SecurityMemberDefinition member => new
        {
            member.ResourceCode, member.MemberCode, member.DisplayName, member.IsSensitive, member.IsSystemManaged, member.IsActive
        },
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static AuditTrail RegistryAudit(string entityName, Guid publicId, string actorId, string reason, object oldValue, object newValue) => new()
    {
        EntityName = entityName,
        EntityId = publicId.ToString(),
        Action = "Update",
        OldValue = System.Text.Json.JsonSerializer.Serialize(oldValue),
        NewValue = System.Text.Json.JsonSerializer.Serialize(newValue),
        ChangedBy = actorId,
        ChangedAt = DateTime.UtcNow,
        Reason = reason
    };

    [Fact]
    public async Task Security_catalogue_legacy_member_correction_is_atomic_and_audited()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var actor = IdpTestFixture.CreateUser("catalogue-upgrade-actor");
        var opms = new SecurityMemberDefinition
        {
            ResourceCode = "OPMS_SUBMISSION", MemberCode = "SuggestionReason", DisplayName = "Suggestion Reason",
            IsSensitive = true, IsSystemManaged = true
        };
        var ipms = new SecurityMemberDefinition
        {
            ResourceCode = "IPMS_SUBMISSION", MemberCode = "SuggestionReason", DisplayName = "Suggestion Reason",
            IsSensitive = true, IsSystemManaged = true
        };
        context.AddRange(actor, opms, ipms);
        await context.SaveChangesAsync();

        await SecurityRegistrySeeder.SeedAsync(context);

        opms.IsSystemManaged.Should().BeFalse();
        ipms.IsSystemManaged.Should().BeFalse();
        var audits = await context.AuditTrails
            .Where(item => item.EntityName == nameof(SecurityMemberDefinition) && item.Action == "Update")
            .ToArrayAsync();
        audits.Should().HaveCount(2).And.OnlyContain(item => item.ChangedBy == actor.Id
            && item.OldValue!.Contains("\"IsSystemManaged\":true")
            && item.NewValue!.Contains("\"IsSystemManaged\":false")
            && !string.IsNullOrWhiteSpace(item.Reason));
        (await context.Permissions.CountAsync(item => item.Code.EndsWith("SuggestionReason.UPDATE"))).Should().Be(2);
    }

    [Fact]
    public async Task Security_catalogue_registers_live_kpi_reference_and_budget_amount_members_idempotently()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);

        var expected = new[]
        {
            ("OPMS_KPI", "InternalReference"), ("OPMS_KPI", "FmsLink"), ("OPMS_KPI", "BudgetSourceAmounts"),
            ("IPMS_KPI", "InternalReference"), ("IPMS_KPI", "BudgetSourceAmounts")
        };
        foreach (var (resource, member) in expected)
        {
            var definition = await context.SecurityMemberDefinitions.SingleAsync(item => item.ResourceCode == resource && item.MemberCode == member);
            definition.IsSensitive.Should().BeTrue();
            (await context.Permissions.CountAsync(item => item.Code == $"{resource}.{member}.READ")).Should().Be(1);
            (await context.Permissions.CountAsync(item => item.Code == $"{resource}.{member}.UPDATE")).Should().Be(1);
        }
    }

    [Fact]
    public async Task Security_catalogue_seed_registers_opms_dashboard_and_migrates_legacy_and_modern_opms_grants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        await DbInitializer.SeedPermissionsAsync(context);
        var legacyPermission = await context.Permissions.SingleAsync(item => item.Code == "OPMS.View");
        var role = new ApplicationRole
        {
            Id = "opms-viewer",
            Name = "OPMS Viewer",
            NormalizedName = "OPMS VIEWER",
            RoleCode = "OPMS_VIEWER"
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionId = legacyPermission.Id,
            IsAllowed = true,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var navigation = await context.SecurityNavigationItems.SingleAsync(item => item.Code == "NAV.SDBIP.DASHBOARD");
        navigation.Route.Should().Be("/opms/dashboard");
        navigation.RequiredPermissionCode.Should().Be("NAV.SDBIP.DASHBOARD");
        (await context.SecurityNavigationItems.SingleAsync(item => item.Id == navigation.ParentId)).Code.Should().Be("NAV.SDBIP");

        var permission = await context.Permissions.SingleAsync(item => item.Code == "NAV.SDBIP.DASHBOARD");
        permission.Kind.Should().Be(SecurityPermissionKind.Navigation);
        permission.NavigationCode.Should().Be("NAV.SDBIP.DASHBOARD");
        (await context.RolePermissions.SingleAsync(item => item.RoleId == role.Id && item.PermissionId == permission.Id)).IsAllowed.Should().BeTrue();

        var revisionsNavigation = await context.SecurityNavigationItems.SingleAsync(item => item.Code == "NAV.SDBIP.REVISIONS");
        revisionsNavigation.Name.Should().Be("Revision Management");
        revisionsNavigation.Route.Should().Be("/opms/revisions");
        revisionsNavigation.RequiredPermissionCode.Should().Be("NAV.SDBIP.REVISIONS");
        revisionsNavigation.ParentId.Should().Be(navigation.ParentId);
        var revisionsPermission = await context.Permissions.SingleAsync(item => item.Code == "NAV.SDBIP.REVISIONS");
        revisionsPermission.Kind.Should().Be(SecurityPermissionKind.Navigation);
        (await context.RolePermissions.SingleAsync(item => item.RoleId == role.Id && item.PermissionId == revisionsPermission.Id)).IsAllowed.Should().BeTrue();

        var registerPermission = await context.Permissions.SingleAsync(item => item.Code == "NAV.SDBIP.REGISTER");
        var modernRole = new ApplicationRole
        {
            Id = "modern-opms-viewer",
            Name = "Modern OPMS Viewer",
            NormalizedName = "MODERN OPMS VIEWER",
            RoleCode = "MODERN_OPMS_VIEWER"
        };
        context.Roles.Add(modernRole);
        await context.SaveChangesAsync();
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = modernRole.Id,
            PermissionId = registerPermission.Id,
            IsAllowed = true,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        (await context.SecurityNavigationItems.CountAsync(item => item.Code == "NAV.SDBIP.DASHBOARD")).Should().Be(1);
        (await context.SecurityNavigationItems.CountAsync(item => item.Code == "NAV.SDBIP.REVISIONS")).Should().Be(1);
        (await context.RolePermissions.CountAsync(item => item.RoleId == role.Id && item.PermissionId == permission.Id)).Should().Be(1);
        (await context.RolePermissions.CountAsync(item => item.RoleId == role.Id && item.PermissionId == revisionsPermission.Id)).Should().Be(1);
        (await context.RolePermissions.SingleAsync(item => item.RoleId == modernRole.Id && item.PermissionId == permission.Id)).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Dashboard_access_additively_grants_the_active_financial_year_directory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedPermissionsAsync(context);

        var role = new ApplicationRole
        {
            Id = "financial-year-context-user", Name = "Financial Year Context User",
            NormalizedName = "FINANCIAL YEAR CONTEXT USER", RoleCode = "FINANCIAL_YEAR_CONTEXT_USER"
        };
        var dashboard = await context.Permissions.SingleAsync(item => item.Code == "Dashboard.View");
        context.Roles.Add(role);
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id, PermissionId = dashboard.Id, IsAllowed = true, IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var financialYearRead = await context.Permissions.SingleAsync(item => item.Code == "FINANCIAL_YEAR.READ");
        var grants = await context.RolePermissions
            .Where(item => item.RoleId == role.Id && item.PermissionId == financialYearRead.Id)
            .ToListAsync();
        grants.Should().ContainSingle();
        grants[0].IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Security_catalogue_seed_registers_and_grants_governance_audit_navigation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();

        await DbInitializer.SeedPermissionsAsync(context);
        var legacyPermission = await context.Permissions.SingleAsync(item => item.Code == "Admin.Permissions.Manage");
        var role = new ApplicationRole
        {
            Id = "governance-auditor",
            Name = "Governance Auditor",
            NormalizedName = "GOVERNANCE AUDITOR",
            RoleCode = "GOVERNANCE_AUDITOR"
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionId = legacyPermission.Id,
            IsAllowed = true,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var expected = new Dictionary<string, (string Route, string Permission)>
        {
            ["NAV.ADMIN.PERMISSIONS"] = ("/system-administration/permissions", "NAV.ADMIN.PERMISSIONS"),
            ["NAV.ADMIN.ROLE_IMPLEMENTATION_AUDIT"] = ("/system-administration/role-implementation-audit", "RoleImplementationAudit.View"),
            ["NAV.ADMIN.ROLE_ACCESS_MATRIX"] = ("/system-administration/role-access-matrix", "SystemAdministration.RoleAccessMatrix.View"),
            ["NAV.ADMIN.PERMISSION_SIMULATION"] = ("/system-administration/permission-simulation", "SystemAdministration.PermissionSimulation.View"),
            ["NAV.ADMIN.SYSTEM_COVERAGE_AUDIT"] = ("/system-administration/system-coverage-audit", "SystemAdministration.SystemCoverageAudit.View"),
            ["NAV.ADMIN.ROLE_PERMISSION_CRUD_AUDIT"] = ("/system-administration/role-permission-crud-audit", "RoleImplementationAudit.View"),
            ["NAV.ADMIN.OPERATIONS"] = ("/system-administration/operations", "SYSTEM_OPERATIONS.READ")
        };

        foreach (var (code, definition) in expected)
        {
            var navigation = await context.SecurityNavigationItems.SingleAsync(item => item.Code == code);
            navigation.Route.Should().Be(definition.Route);
            navigation.RequiredPermissionCode.Should().Be(definition.Permission);
            (await context.SecurityNavigationItems.SingleAsync(item => item.Id == navigation.ParentId)).Code.Should().Be("NAV.ADMIN");

            var requiredPermission = await context.Permissions.SingleAsync(item => item.Code == definition.Permission);
            (await context.RolePermissions.SingleAsync(item => item.RoleId == role.Id && item.PermissionId == requiredPermission.Id)).IsAllowed.Should().BeTrue();
        }

        var operationsPermission = await context.Permissions.SingleAsync(item => item.Code == "SYSTEM_OPERATIONS.READ");
        operationsPermission.Kind.Should().Be(SecurityPermissionKind.Resource);
        operationsPermission.ResourceCode.Should().Be("SYSTEM_OPERATIONS");
        operationsPermission.Operation.Should().Be(SecurityOperation.Read);
    }

    [Fact]
    public async Task Security_catalogue_seed_registers_rfi_queue_and_additively_migrates_submission_view_grants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedPermissionsAsync(context);

        var role = new ApplicationRole
        {
            Id = "rfi-queue-reviewer", Name = "RFI Queue Reviewer", NormalizedName = "RFI QUEUE REVIEWER",
            RoleCode = "RFI_QUEUE_REVIEWER"
        };
        var opmsView = await context.Permissions.SingleAsync(item => item.Code == "OPMS.Submissions.View");
        var ipmsView = await context.Permissions.SingleAsync(item => item.Code == "IPMS.Submissions.View");
        context.Roles.Add(role);
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = opmsView.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new RolePermission { RoleId = role.Id, PermissionId = ipmsView.Id, IsAllowed = true, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var navigation = await context.SecurityNavigationItems.SingleAsync(item => item.Code == "NAV.WORKFLOW.RFI");
        navigation.Route.Should().Be("/workflow/rfis");
        navigation.RequiredPermissionCode.Should().Be("NAV.WORKFLOW.RFI");
        navigation.DisplayOrder.Should().Be(60);
        (await context.SecurityNavigationItems.SingleAsync(item => item.Id == navigation.ParentId)).Code.Should().Be("NAV.WORKFLOW");
        var permission = await context.Permissions.SingleAsync(item => item.Code == "NAV.WORKFLOW.RFI");
        (await context.RolePermissions.CountAsync(item => item.RoleId == role.Id && item.PermissionId == permission.Id)).Should().Be(1);
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => null;
    }
}

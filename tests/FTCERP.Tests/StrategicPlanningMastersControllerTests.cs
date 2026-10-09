using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class StrategicPlanningMastersControllerTests
{
    [Fact]
    public async Task EffectiveDatedMasterIsAuditedAndFilteredByFinancialYear()
    {
        await using var fixture = await Fixture.CreateAsync(); await using var context = fixture.Context(fixture.MunicipalityA.Id); var controller = Controller(context, fixture.MunicipalityA.Id);
        var created = await controller.CreateMunicipalKpa(new("MKPA-1", "Reliable services", null, fixture.Year2025.PublicId, fixture.Year2025.PublicId, 10, true, "Council adopted strategic master"));
        var dto = Assert.IsType<OkObjectResult>(created.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!;
        Assert.Equal("2025/26", dto.EffectiveFromFinancialYearCode);
        Assert.Single((Assert.IsType<OkObjectResult>((await controller.GetMunicipalKpas(new() { MunicipalityFinancialYearPublicId = fixture.Year2025.PublicId })).Result).Value.As<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>().Data!).Items);
        Assert.Empty((Assert.IsType<OkObjectResult>((await controller.GetMunicipalKpas(new() { MunicipalityFinancialYearPublicId = fixture.Year2026.PublicId })).Result).Value.As<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>().Data!).Items);
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityId == dto.PublicId.ToString() && item.Action == "Create");
    }

    [Fact]
    public async Task TenantBoundaryPreventsCrossMunicipalityRelationship()
    {
        await using var fixture = await Fixture.CreateAsync();
        Guid foreignGoal;
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
        {
            var result = await Controller(tenantB, fixture.MunicipalityB.Id).CreateStrategicGoal(new(null, "Tenant B goal", null, null, null, 10, true, "Tenant B strategic planning setup"));
            foreignGoal = Assert.IsType<OkObjectResult>(result.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!.PublicId;
        }
        await using var tenantA = fixture.Context(fixture.MunicipalityA.Id); var controller = Controller(tenantA, fixture.MunicipalityA.Id);
        var kpaResult = await controller.CreateMunicipalKpa(new(null, "Tenant A KPA", null, null, null, 10, true, "Tenant A strategic planning setup"));
        var kpa = Assert.IsType<OkObjectResult>(kpaResult.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!;
        var link = await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, foreignGoal, "Attempt prohibited cross tenant link"));
        Assert.IsType<NotFoundObjectResult>(link.Result);
    }

    [Fact]
    public async Task ConfigurableRelationshipCanBeCreatedAndSoftDisabledWithConcurrency()
    {
        await using var fixture = await Fixture.CreateAsync(); await using var context = fixture.Context(fixture.MunicipalityA.Id); var controller = Controller(context, fixture.MunicipalityA.Id);
        var kpa = Data(await controller.CreateMunicipalKpa(new(null, "KPA", null, null, null, 1, true, "Create KPA relationship parent")));
        var goal = Data(await controller.CreateStrategicGoal(new(null, "Goal", null, null, null, 1, true, "Create goal relationship child")));
        var linked = await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Council approved optional relationship"));
        var relationship = Assert.IsType<OkObjectResult>(linked.Result).Value.As<ApiResponse<StrategicPlanningRelationshipDto>>().Data!;
        var pageResult = await controller.GetRelationshipsPage(
            new PagedQueryRequest { Page = 1, PageSize = 1, SortBy = "parentName", SortDirection = "asc" },
            "municipal-kpa-strategic-goal", true, kpa.PublicId, goal.PublicId);
        var page = Assert.IsType<OkObjectResult>(pageResult.Result).Value.As<ApiResponse<PagedResponse<StrategicPlanningRelationshipDto>>>().Data!;
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(relationship.PublicId, Assert.Single(page.Items).PublicId);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetRelationships().Result).StatusCode);
        var disabled = await controller.DisableRelationship(relationship.PublicId, new("Relationship superseded by council", relationship.RowVersion));
        var disabledDto = Assert.IsType<OkObjectResult>(disabled.Result).Value.As<ApiResponse<StrategicPlanningRelationshipDto>>().Data!;
        Assert.False((await context.MunicipalKpaStrategicGoals.SingleAsync()).IsActive);
        var inactiveResult = await controller.GetRelationshipsPage(new PagedQueryRequest(), "municipal-kpa-strategic-goal", true, kpa.PublicId, goal.PublicId);
        Assert.False(Assert.Single(Assert.IsType<OkObjectResult>(inactiveResult.Result).Value.As<ApiResponse<PagedResponse<StrategicPlanningRelationshipDto>>>().Data!.Items).IsActive);
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityId == relationship.PublicId.ToString() && item.Action == "DisableRelationship");
        Assert.IsType<ConflictObjectResult>((await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Attempt unsafe relationship reactivation"))).Result);
        Assert.IsType<OkObjectResult>((await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Council approved relationship reactivation", disabledDto.RowVersion))).Result);
    }

    [Fact]
    public async Task StaleMasterRowVersionReturnsConflict()
    {
        await using var fixture = await Fixture.CreateAsync(); Guid id; string stale;
        await using (var context = fixture.Context(fixture.MunicipalityA.Id)) { var dto = Data(await Controller(context, fixture.MunicipalityA.Id).CreateStrategicObjective(new("SO", "Objective", null, null, null, 1, true, "Create objective for concurrency"))); id = dto.PublicId; stale = dto.RowVersion; }
        await using (var writer = fixture.Context(fixture.MunicipalityA.Id))
        {
            var result = await Controller(writer, fixture.MunicipalityA.Id).UpdateStrategicObjective(id,
                new("SO", "Changed elsewhere", null, null, null, 1, true, "Approved concurrent objective correction", stale));
            Assert.IsType<OkObjectResult>(result.Result);
        }
        await using (var context = fixture.Context(fixture.MunicipalityA.Id)) { var result = await Controller(context, fixture.MunicipalityA.Id).UpdateStrategicObjective(id, new("SO", "Stale", null, null, null, 1, true, "Attempt stale objective update", stale)); Assert.IsType<ConflictObjectResult>(result.Result); }
    }

    [Fact]
    public async Task CanonicalResolverEnforcesTenantYearAvailabilityAndConfiguredHierarchy()
    {
        await using var fixture = await Fixture.CreateAsync();
        Guid foreignGoal;
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
        {
            foreignGoal = Data(await Controller(tenantB, fixture.MunicipalityB.Id).CreateStrategicGoal(
                new("B-GOAL", "Tenant B goal", null, null, null, 1, true, "Create foreign strategic goal"))).PublicId;
        }

        Guid disabledNationalKpa;
        await using (var setup = fixture.SystemContext())
        {
            var national = new NationalKpa { Code = "NKPA-DISABLED", Name = "Disabled locally" };
            setup.NationalKpas.Add(national);
            await setup.SaveChangesAsync();
            setup.MunicipalityNationalKpas.Add(new MunicipalityNationalKpa
            {
                MunicipalityId = fixture.MunicipalityA.Id,
                NationalKpaId = national.Id,
                IsEnabled = false
            });
            await setup.SaveChangesAsync();
            disabledNationalKpa = national.PublicId;
        }

        await using var context = fixture.Context(fixture.MunicipalityA.Id);
        var controller = Controller(context, fixture.MunicipalityA.Id);
        var kpa = Data(await controller.CreateMunicipalKpa(new("KPA-A", "Municipal KPA A", null, null, null, 1, true, "Create canonical KPA")));
        var allowedGoal = Data(await controller.CreateStrategicGoal(new("GOAL-A", "Allowed goal", null, null, null, 1, true, "Create allowed goal")));
        var otherGoal = Data(await controller.CreateStrategicGoal(new("GOAL-B", "Other goal", null, null, null, 2, true, "Create other goal")));
        var expiredObjective = Data(await controller.CreateStrategicObjective(new("OBJ-OLD", "Historic objective", null,
            fixture.Year2025.PublicId, fixture.Year2025.PublicId, 1, true, "Create historic objective")));
        await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, allowedGoal.PublicId, "Configure the permitted hierarchy"));

        var foreign = await StrategicClassificationResolver.ResolveAsync(context, fixture.Year2026.Id,
            new(null, null, null, foreignGoal, null, null, null));
        Assert.False(foreign.IsValid);
        Assert.Contains("selected municipality", foreign.Error, StringComparison.OrdinalIgnoreCase);

        var disabled = await StrategicClassificationResolver.ResolveAsync(context, fixture.Year2026.Id,
            new(disabledNationalKpa, null, null, null, null, null, null));
        Assert.False(disabled.IsValid);
        Assert.Contains("not available", disabled.Error, StringComparison.OrdinalIgnoreCase);

        var mismatch = await StrategicClassificationResolver.ResolveAsync(context, fixture.Year2026.Id,
            new(null, kpa.PublicId, null, otherGoal.PublicId, null, null, null));
        Assert.False(mismatch.IsValid);
        Assert.Contains("not configured", mismatch.Error, StringComparison.OrdinalIgnoreCase);

        var expired = await StrategicClassificationResolver.ResolveAsync(context, fixture.Year2026.Id,
            new(null, null, null, null, null, expiredObjective.PublicId, null));
        Assert.False(expired.IsValid);
        Assert.Contains("not valid", expired.Error, StringComparison.OrdinalIgnoreCase);

        var historic = await StrategicClassificationResolver.ResolveAsync(context, fixture.Year2026.Id,
            new(null, null, null, null, null, expiredObjective.PublicId, null), existingStrategicObjectiveId:
            await context.MunicipalStrategicObjectives.Where(item => item.PublicId == expiredObjective.PublicId).Select(item => item.Id).SingleAsync());
        Assert.True(historic.IsValid);

        var valid = await StrategicClassificationResolver.ResolveAsync(context, fixture.Year2026.Id,
            new(null, kpa.PublicId, null, allowedGoal.PublicId, null, null, null));
        Assert.True(valid.IsValid);
        var target = new OpmsTarget { NationalKpa = "untrusted", MunicipalKpa = "untrusted", PerformanceObjective = "untrusted" };
        StrategicClassificationResolver.Apply(target, valid);
        Assert.Equal(kpa.Name, target.MunicipalKpa);
        Assert.Equal(valid.MunicipalKpa!.Id, target.MunicipalKpaId);
        Assert.Equal(valid.StrategicGoal!.Id, target.StrategicGoalMasterId);
        Assert.Null(target.StrategicGoalId);
    }

    [Fact]
    public async Task CataloguePagesAreBoundedAvailableEffectiveAndHierarchyFiltered()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var setup = fixture.SystemContext())
        {
            var available = new NationalKpa { Code = "NKPA-AVAILABLE", Name = "Available", DisplayOrder = 1 };
            var disabled = new NationalKpa { Code = "NKPA-DISABLED", Name = "Disabled", DisplayOrder = 2 };
            var secondAvailable = new NationalKpa { Code = "NKPA-SECOND", Name = "Second available", DisplayOrder = 3 };
            setup.NationalKpas.AddRange(available, disabled, secondAvailable);
            await setup.SaveChangesAsync();
            setup.MunicipalityNationalKpas.Add(new MunicipalityNationalKpa { MunicipalityId = fixture.MunicipalityA.Id, NationalKpaId = disabled.Id, IsEnabled = false });
            await setup.SaveChangesAsync();
        }

        await using var context = fixture.Context(fixture.MunicipalityA.Id);
        var controller = Controller(context, fixture.MunicipalityA.Id);
        var kpa = Data(await controller.CreateMunicipalKpa(new("KPA-CURRENT", "Current KPA", null, fixture.Year2026.PublicId, null, 1, true, "Create current catalogue KPA")));
        var unmappedKpa = Data(await controller.CreateMunicipalKpa(new("KPA-UNMAPPED", "Unmapped KPA", null, fixture.Year2026.PublicId, null, 4, true, "Create catalogue KPA without mappings")));
        _ = Data(await controller.CreateMunicipalKpa(new("KPA-EXPIRED", "Expired KPA", null, fixture.Year2025.PublicId, fixture.Year2025.PublicId, 2, true, "Create expired catalogue KPA")));
        _ = Data(await controller.CreateMunicipalKpa(new("KPA-INACTIVE", "Inactive KPA", null, null, null, 3, false, "Create inactive catalogue KPA")));
        var goal = Data(await controller.CreateStrategicGoal(new("GOAL-CURRENT", "Current goal", null, null, null, 1, true, "Create catalogue goal")));
        await controller.LinkMunicipalKpaToGoal(new(kpa.PublicId, goal.PublicId, "Configure catalogue relationship"));

        var legacy = controller.GetOpmsCatalogue(fixture.Year2026.PublicId);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(legacy.Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetIpmsCatalogue(fixture.Year2026.PublicId).Result).StatusCode);
        var nationalKpas = await CataloguePage(controller, fixture.Year2026.PublicId, "national-kpas", pageSize: 1);
        Assert.Equal(2, nationalKpas.TotalCount);
        Assert.Equal(2, nationalKpas.TotalPages);
        Assert.Collection(nationalKpas.Items, item => Assert.Equal("NKPA-AVAILABLE", item.Code));
        var municipalKpas = await CataloguePage(controller, fixture.Year2026.PublicId, "municipal-kpas", search: "CURRENT");
        Assert.Collection(municipalKpas.Items, item => Assert.Equal(kpa.PublicId, item.PublicId));
        var linkedGoals = await CataloguePage(controller, fixture.Year2026.PublicId, "strategic-goals",
            parentPublicId: kpa.PublicId, relationshipType: "municipal-kpa-strategic-goal");
        Assert.Collection(linkedGoals.Items, item => Assert.Equal(goal.PublicId, item.PublicId));
        var unconfiguredGoals = await CataloguePage(controller, fixture.Year2026.PublicId, "strategic-goals",
            parentPublicId: unmappedKpa.PublicId, relationshipType: "municipal-kpa-strategic-goal");
        Assert.Collection(unconfiguredGoals.Items, item => Assert.Equal(goal.PublicId, item.PublicId));
        var invalidRelationship = await controller.GetOpmsCataloguePage(new PagedQueryRequest(), fixture.Year2026.PublicId,
            "strategic-goals", kpa.PublicId, "strategic-intervention-objective");
        Assert.IsType<BadRequestObjectResult>(invalidRelationship.Result);
    }

    [Fact]
    public async Task GovernedBudgetMastersAreTenantScopedEffectiveDatedAndSupportMultipleSources()
    {
        await using var fixture = await Fixture.CreateAsync();
        Guid foreignSource;
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
        {
            foreignSource = Data(await Controller(tenantB, fixture.MunicipalityB.Id).CreateBudgetSource(
                new("FOREIGN", "Foreign grant", null, null, null, 10, true, "Create tenant B budget source"))).PublicId;
        }

        await using var context = fixture.Context(fixture.MunicipalityA.Id);
        var controller = Controller(context, fixture.MunicipalityA.Id);
        var sourceA = Data(await controller.CreateBudgetSource(new("MIG", "Infrastructure grant", null, null, null, 10, true, "Create first governed budget source")));
        var sourceB = Data(await controller.CreateBudgetSource(new("OWN", "Own revenue", null, fixture.Year2026.PublicId, null, 20, true, "Create second governed budget source")));
        var historicSource = Data(await controller.CreateBudgetSource(new("OLD", "Historic grant", null, fixture.Year2025.PublicId, fixture.Year2025.PublicId, 30, true, "Create historic governed budget source")));
        var budgetType = Data(await controller.CreateBudgetType(new("CAPEX", "Capital expenditure", null, null, null, 10, true, "Create governed budget type")));

        var budgetSources = await CataloguePage(controller, fixture.Year2026.PublicId, "budget-sources");
        var budgetTypes = await CataloguePage(controller, fixture.Year2026.PublicId, "budget-types");
        Assert.Equal([sourceA.PublicId, sourceB.PublicId], budgetSources.Items.Select(item => item.PublicId));
        Assert.Collection(budgetTypes.Items, item => Assert.Equal(budgetType.PublicId, item.PublicId));

        var foreign = await BudgetClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, budgetType.PublicId,
            [new(foreignSource, 10m)]);
        Assert.False(foreign.IsValid);
        Assert.Contains("this municipality", foreign.Error, StringComparison.OrdinalIgnoreCase);

        var expired = await BudgetClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, budgetType.PublicId,
            [new(historicSource.PublicId, 10m)]);
        Assert.False(expired.IsValid);
        Assert.Contains("not valid", expired.Error, StringComparison.OrdinalIgnoreCase);

        var duplicate = await BudgetClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, budgetType.PublicId,
            [new(sourceA.PublicId, 10m), new(sourceA.PublicId, 20m)]);
        Assert.False(duplicate.IsValid);

        var resolved = await BudgetClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, budgetType.PublicId,
            [new(sourceA.PublicId, 125.50m), new(sourceB.PublicId, null)]);
        Assert.True(resolved.IsValid);
        var target = new OpmsTarget { Id = "OPMS-BUDGET-1", MunicipalityId = fixture.MunicipalityA.Id };
        BudgetClassificationResolver.Apply(target, resolved);
        Assert.Equal(budgetType.PublicId, resolved.BudgetType!.PublicId);
        Assert.Equal(2, target.GovernedBudgetSources.Count(item => item.IsActive));
        Assert.Contains(target.GovernedBudgetSources, item => item.BudgetSourceId == resolved.BudgetSources[0].Source.Id && item.Amount == 125.50m);

        var changed = await BudgetClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, null,
            [new(sourceA.PublicId, 200m)], resolved.BudgetType!.Id, resolved.BudgetSources.Select(item => item.Source.Id).ToArray());
        BudgetClassificationResolver.Apply(target, changed);
        Assert.Null(target.BudgetTypeMasterId);
        Assert.Single(target.GovernedBudgetSources, item => item.IsActive);
        Assert.Equal(2, target.GovernedBudgetSources.Count(item => !item.IsActive));
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityName == nameof(GovernedBudgetSource));
    }

    [Fact]
    public async Task GovernedPerformanceClassificationsAreTenantScopedEffectiveDatedAndCanonical()
    {
        await using var fixture = await Fixture.CreateAsync();
        Guid foreignKpiType;
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
            foreignKpiType = Data(await Controller(tenantB, fixture.MunicipalityB.Id).CreateKpiType(
                new("FOREIGN", "Foreign type", null, null, null, 10, true, "Create foreign KPI type master"))).PublicId;

        await using var context = fixture.Context(fixture.MunicipalityA.Id);
        var controller = Controller(context, fixture.MunicipalityA.Id);
        var kpiType = Data(await controller.CreateKpiType(new("OUTPUT", "Output", null, null, null, 10, true, "Create governed KPI type")));
        var indicatorType = Data(await controller.CreateIndicatorType(new("QUANT", "Quantitative", null, null, null, 10, true, "Create governed indicator type")));
        var functionalArea = Data(await controller.CreateFunctionalArea(new("TECH", "Technical Services", null, fixture.Year2026.PublicId, null, 10, true, "Create governed functional area")));
        var expiredStandard = Data(await controller.CreateStandardClassification(new("OLD", "Historic standard", null, fixture.Year2025.PublicId, fixture.Year2025.PublicId, 10, true, "Create historic standard classification")));

        Assert.Collection((await CataloguePage(controller, fixture.Year2026.PublicId, "kpi-types")).Items, item => Assert.Equal(kpiType.PublicId, item.PublicId));
        Assert.Collection((await CataloguePage(controller, fixture.Year2026.PublicId, "indicator-types")).Items, item => Assert.Equal(indicatorType.PublicId, item.PublicId));
        Assert.Collection((await CataloguePage(controller, fixture.Year2026.PublicId, "functional-areas")).Items, item => Assert.Equal(functionalArea.PublicId, item.PublicId));
        Assert.Empty((await CataloguePage(controller, fixture.Year2026.PublicId, "standard-classifications")).Items);

        var foreign = await PerformanceClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, foreignKpiType, indicatorType.PublicId, null, null, true);
        Assert.False(foreign.IsValid);
        var expired = await PerformanceClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, kpiType.PublicId, indicatorType.PublicId, functionalArea.PublicId, expiredStandard.PublicId, true);
        Assert.False(expired.IsValid);
        var resolved = await PerformanceClassificationResolver.ResolveAsync(context, fixture.Year2026.Id, kpiType.PublicId, indicatorType.PublicId, functionalArea.PublicId, null, true);
        Assert.True(resolved.IsValid);
        var target = new OpmsTarget { MunicipalityId = fixture.MunicipalityA.Id, KpiType = "untrusted", IndicatorType = "untrusted" };
        PerformanceClassificationResolver.Apply(target, resolved);
        Assert.Equal(kpiType.Name, target.KpiType);
        Assert.Equal(indicatorType.Name, target.IndicatorType);
        Assert.Equal(functionalArea.Name, target.FunctionalArea);
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityId == kpiType.PublicId.ToString());
    }

    [Fact]
    public async Task GovernedKpiUnitsOfMeasurePreserveSymbolTenantEffectiveDatesAuditAndConcurrency()
    {
        await using var fixture = await Fixture.CreateAsync();
        Guid foreignPublicId;
        await using (var tenantB = fixture.Context(fixture.MunicipalityB.Id))
            foreignPublicId = Data(await Controller(tenantB, fixture.MunicipalityB.Id).CreateKpiUnitOfMeasure(
                new("FOREIGN", "Foreign unit", null, null, null, 1, true, "Create foreign unit", Symbol: "F"))).PublicId;

        Guid currentPublicId; string staleVersion;
        await using (var context = fixture.Context(fixture.MunicipalityA.Id))
        {
            var controller = Controller(context, fixture.MunicipalityA.Id);
            var current = Data(await controller.CreateKpiUnitOfMeasure(new("COUNT", "Count", "Items counted",
                fixture.Year2026.PublicId, null, 1, true, "Create governed unit", Symbol: "#")));
            _ = Data(await controller.CreateKpiUnitOfMeasure(new("OLD", "Historic unit", null,
                fixture.Year2025.PublicId, fixture.Year2025.PublicId, 2, true, "Create historic unit", Symbol: "H")));
            currentPublicId = current.PublicId; staleVersion = current.RowVersion;
            Assert.Equal("#", current.Symbol);

            var units = await CataloguePage(controller, fixture.Year2026.PublicId, "kpi-units-of-measure");
            Assert.Collection(units.Items, item => { Assert.Equal(current.PublicId, item.PublicId); Assert.Equal("#", item.Symbol); });

            var foreign = await PerformanceClassificationResolver.ResolveUnitAsync(context, fixture.Year2026.Id, foreignPublicId);
            Assert.False(foreign.IsValid); Assert.Contains("this municipality", foreign.Error, StringComparison.OrdinalIgnoreCase);
            var historic = await context.GovernedKpiUnitOfMeasures.SingleAsync(item => item.Code == "OLD");
            var expired = await PerformanceClassificationResolver.ResolveUnitAsync(context, fixture.Year2026.Id, historic.PublicId);
            Assert.False(expired.IsValid); Assert.Contains("not valid", expired.Error, StringComparison.OrdinalIgnoreCase);
            var resolved = await PerformanceClassificationResolver.ResolveUnitAsync(context, fixture.Year2026.Id, current.PublicId);
            Assert.True(resolved.IsValid);
            var target = new OpmsTarget { MunicipalityId = fixture.MunicipalityA.Id, UnitOfMeasureId = 7 };
            PerformanceClassificationResolver.ApplyUnit(target, resolved);
            Assert.Equal(resolved.UnitOfMeasure!.Id, target.KpiUnitOfMeasureMasterId); Assert.Null(target.UnitOfMeasureId);
            Assert.Contains(await context.AuditTrails.ToArrayAsync(), item => item.EntityId == current.PublicId.ToString() && item.Action == "Create");
        }

        await using (var writer = fixture.Context(fixture.MunicipalityA.Id))
        {
            var result = await Controller(writer, fixture.MunicipalityA.Id).UpdateKpiUnitOfMeasure(currentPublicId,
                new("COUNT", "Count", "Items counted", fixture.Year2026.PublicId, null, 1, true,
                    "Approved concurrent unit symbol correction", staleVersion, "items"));
            Assert.IsType<OkObjectResult>(result.Result);
        }
        await using (var stale = fixture.Context(fixture.MunicipalityA.Id))
        {
            var result = await Controller(stale, fixture.MunicipalityA.Id).UpdateKpiUnitOfMeasure(currentPublicId,
                new("COUNT", "Count", "Items counted", fixture.Year2026.PublicId, null, 1, true, "Attempt stale unit update", staleVersion, "#"));
            Assert.IsType<ConflictObjectResult>(result.Result);
        }
    }

    [Fact]
    public async Task SecurityRegistrySeedsAllMastersHierarchyAndNavigation()
    {
        await using var fixture = await Fixture.CreateAsync(); await using var context = fixture.SystemContext(); await SecurityRegistrySeeder.SeedAsync(context);
        var resources = await context.SecurityResources.Select(item => item.Code).ToArrayAsync();
        Assert.Contains("MUNICIPAL_KPA", resources); Assert.Contains("STRATEGIC_GOAL", resources); Assert.Contains("STRATEGIC_INTERVENTION", resources); Assert.Contains("STRATEGIC_OBJECTIVE", resources); Assert.Contains("PERFORMANCE_OBJECTIVE", resources); Assert.Contains("STRATEGIC_HIERARCHY", resources); Assert.Contains("BUDGET_SOURCE", resources); Assert.Contains("BUDGET_TYPE", resources); Assert.Contains("KPI_UNIT_OF_MEASURE", resources);
        Assert.Contains(await context.SecurityNavigationItems.ToArrayAsync(), item => item.Route == "/admin/strategic-interventions" && item.RequiredPermissionCode == "NAV.CONFIGURATION.STRATEGIC_INTERVENTIONS");
        Assert.Contains(await context.SecurityNavigationItems.ToArrayAsync(), item => item.Route == "/admin/budget-sources" && item.RequiredPermissionCode == "NAV.CONFIGURATION.BUDGET_SOURCES");
        Assert.Contains(await context.SecurityNavigationItems.ToArrayAsync(), item => item.Route == "/admin/units-measure" && item.RequiredPermissionCode == "NAV.CONFIGURATION.KPI_UNITS_OF_MEASURE");
    }

    [Fact]
    public async Task Strategic_planning_masters_reject_unaudited_rewrites_and_hard_deletes()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var setup = fixture.SystemContext())
        {
            setup.AddRange(
                new MunicipalKpa { MunicipalityId = fixture.MunicipalityA.Id, Code = "KPA", Name = "Municipal KPA" },
                new MunicipalStrategicGoal { MunicipalityId = fixture.MunicipalityA.Id, Code = "GOAL", Name = "Strategic goal" },
                new StrategicIntervention { MunicipalityId = fixture.MunicipalityA.Id, Code = "INT", Name = "Strategic intervention" },
                new MunicipalStrategicObjective { MunicipalityId = fixture.MunicipalityA.Id, Code = "OBJ", Name = "Strategic objective" },
                new PerformanceObjective { MunicipalityId = fixture.MunicipalityA.Id, Code = "PERF", Name = "Performance objective" },
                new GovernedBudgetSource { MunicipalityId = fixture.MunicipalityA.Id, Code = "BS", Name = "Budget source" },
                new GovernedBudgetType { MunicipalityId = fixture.MunicipalityA.Id, Code = "BT", Name = "Budget type" },
                new GovernedKpiType { MunicipalityId = fixture.MunicipalityA.Id, Code = "KT", Name = "KPI type" },
                new GovernedIndicatorType { MunicipalityId = fixture.MunicipalityA.Id, Code = "IT", Name = "Indicator type" },
                new GovernedFunctionalArea { MunicipalityId = fixture.MunicipalityA.Id, Code = "FA", Name = "Functional area" },
                new GovernedStandardClassification { MunicipalityId = fixture.MunicipalityA.Id, Code = "SC", Name = "Standard classification" },
                new GovernedKpiUnitOfMeasure { MunicipalityId = fixture.MunicipalityA.Id, Code = "UOM", Name = "Unit", Symbol = "#" });
            await setup.SaveChangesAsync();
        }

        await using var context = fixture.Context(fixture.MunicipalityA.Id);
        await AssertUnauditedRewriteRejected(context, context.MunicipalKpas);
        await AssertUnauditedRewriteRejected(context, context.MunicipalStrategicGoals);
        await AssertUnauditedRewriteRejected(context, context.StrategicInterventions);
        await AssertUnauditedRewriteRejected(context, context.MunicipalStrategicObjectives);
        await AssertUnauditedRewriteRejected(context, context.PerformanceObjectives);
        await AssertUnauditedRewriteRejected(context, context.GovernedBudgetSources);
        await AssertUnauditedRewriteRejected(context, context.GovernedBudgetTypes);
        await AssertUnauditedRewriteRejected(context, context.GovernedKpiTypes);
        await AssertUnauditedRewriteRejected(context, context.GovernedIndicatorTypes);
        await AssertUnauditedRewriteRejected(context, context.GovernedFunctionalAreas);
        await AssertUnauditedRewriteRejected(context, context.GovernedStandardClassifications);
        await AssertUnauditedRewriteRejected(context, context.GovernedKpiUnitOfMeasures);

        await AssertHardDeleteRejected(context, context.MunicipalKpas);
        await AssertHardDeleteRejected(context, context.MunicipalStrategicGoals);
        await AssertHardDeleteRejected(context, context.StrategicInterventions);
        await AssertHardDeleteRejected(context, context.MunicipalStrategicObjectives);
        await AssertHardDeleteRejected(context, context.PerformanceObjectives);
        await AssertHardDeleteRejected(context, context.GovernedBudgetSources);
        await AssertHardDeleteRejected(context, context.GovernedBudgetTypes);
        await AssertHardDeleteRejected(context, context.GovernedKpiTypes);
        await AssertHardDeleteRejected(context, context.GovernedIndicatorTypes);
        await AssertHardDeleteRejected(context, context.GovernedFunctionalAreas);
        await AssertHardDeleteRejected(context, context.GovernedStandardClassifications);
        await AssertHardDeleteRejected(context, context.GovernedKpiUnitOfMeasures);
    }

    [Theory]
    [InlineData(nameof(StrategicPlanningMastersController.CreateMunicipalKpa), "Permission:MUNICIPAL_KPA.CREATE")]
    [InlineData(nameof(StrategicPlanningMastersController.UpdateStrategicGoal), "Permission:STRATEGIC_GOAL.UPDATE")]
    [InlineData(nameof(StrategicPlanningMastersController.GetStrategicInterventions), "Permission:STRATEGIC_INTERVENTION.READ")]
    [InlineData(nameof(StrategicPlanningMastersController.CreateBudgetSource), "Permission:BUDGET_SOURCE.CREATE")]
    [InlineData(nameof(StrategicPlanningMastersController.UpdateBudgetType), "Permission:BUDGET_TYPE.UPDATE")]
    [InlineData(nameof(StrategicPlanningMastersController.CreateKpiUnitOfMeasure), "Permission:KPI_UNIT_OF_MEASURE.CREATE")]
    [InlineData(nameof(StrategicPlanningMastersController.GetOpmsCataloguePage), "Permission:OPMS_KPI.READ")]
    [InlineData(nameof(StrategicPlanningMastersController.GetIpmsCataloguePage), "Permission:IPMS_KPI.READ")]
    [InlineData(nameof(StrategicPlanningMastersController.GetRelationshipsPage), "Permission:STRATEGIC_HIERARCHY.READ")]
    [InlineData(nameof(StrategicPlanningMastersController.LinkGoalToObjective), "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public void EndpointsCarryDynamicPermissionPolicies(string methodName, string policy)
    {
        var method = typeof(StrategicPlanningMastersController).GetMethods().Single(item => item.Name == methodName);
        Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), item => item.Policy == policy);
    }

    private static async Task<PagedResponse<StrategicCatalogueItemDto>> CataloguePage(
        StrategicPlanningMastersController controller,
        Guid municipalityFinancialYearPublicId,
        string classificationKind,
        string? search = null,
        int pageSize = 25,
        Guid? parentPublicId = null,
        string? relationshipType = null)
    {
        var result = await controller.GetOpmsCataloguePage(
            new PagedQueryRequest { Page = 1, PageSize = pageSize, Search = search, SortBy = "displayOrder", SortDirection = "asc" },
            municipalityFinancialYearPublicId, classificationKind, parentPublicId, relationshipType);
        return Assert.IsType<OkObjectResult>(result.Result).Value.As<ApiResponse<PagedResponse<StrategicCatalogueItemDto>>>().Data!;
    }

    private static StrategicPlanningMasterDto Data(Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> task) => Data(task.GetAwaiter().GetResult());
    private static StrategicPlanningMasterDto Data(ActionResult<ApiResponse<StrategicPlanningMasterDto>> result) => Assert.IsType<OkObjectResult>(result.Result).Value.As<ApiResponse<StrategicPlanningMasterDto>>().Data!;
    private static async Task AssertUnauditedRewriteRejected<TEntity>(ApplicationDbContext context, DbSet<TEntity> set) where TEntity : StrategicPlanningMasterBase
    {
        var entity = await set.SingleAsync();
        entity.Name += " silently rewritten";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("same-transaction reasoned before/after audit evidence", exception.Message);
        context.ChangeTracker.Clear();
    }
    private static async Task AssertHardDeleteRejected<TEntity>(ApplicationDbContext context, DbSet<TEntity> set) where TEntity : StrategicPlanningMasterBase
    {
        var entity = await set.SingleAsync();
        set.Remove(entity);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("cannot be hard deleted", exception.Message);
        context.ChangeTracker.Clear();
    }
    private static StrategicPlanningMastersController Controller(ApplicationDbContext context, long municipalityId) { var controller = new StrategicPlanningMastersController(context, new TestTenantContext(municipalityId, "planner-1", false)); controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; return controller; }

    private sealed class Fixture(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options, Municipality municipalityA, Municipality municipalityB, MunicipalityFinancialYear year2025, MunicipalityFinancialYear year2026) : IAsyncDisposable
    {
        public Municipality MunicipalityA { get; } = municipalityA; public Municipality MunicipalityB { get; } = municipalityB; public MunicipalityFinancialYear Year2025 { get; } = year2025; public MunicipalityFinancialYear Year2026 { get; } = year2026;
        public ApplicationDbContext Context(long municipalityId) => new(options, new TestTenantContext(municipalityId, "planner-1", false)); public ApplicationDbContext SystemContext() => new(options, new TestTenantContext(null, "setup", true));
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var municipalityA = new Municipality { Code = "SPA", Name = "Strategic A" }; var municipalityB = new Municipality { Code = "SPB", Name = "Strategic B" };
            var financial2025 = new FinancialYear { Code = "2025/26", Name = "2025/26", StartDate = new(2025, 7, 1), EndDate = new(2026, 6, 30) }; var financial2026 = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
            var year2025 = new MunicipalityFinancialYear { Municipality = municipalityA, FinancialYear = financial2025, EffectiveFrom = financial2025.StartDate, IsActive = true, IsCurrent = true }; var year2026 = new MunicipalityFinancialYear { Municipality = municipalityA, FinancialYear = financial2026, EffectiveFrom = financial2026.StartDate, IsActive = true };
            await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "setup", true))) { await setup.Database.EnsureCreatedAsync(); setup.AddRange(municipalityA, municipalityB, financial2025, financial2026, year2025, year2026); await setup.SaveChangesAsync(); setup.Users.Add(new ApplicationUser { Id = "planner-1", UserName = "planner@test", NormalizedUserName = "PLANNER@TEST", FirstName = "Plan", LastName = "User", MunicipalityId = municipalityA.Id, IsActive = true }); await setup.SaveChangesAsync(); }
            return new(connection, options, municipalityA, municipalityB, year2025, year2026);
        }
        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }

    private sealed class TestTenantContext(long? municipalityId, string userId, bool isSystem) : ITenantContext { public long? MunicipalityId { get; } = municipalityId; public string? UserId { get; } = userId; public bool IsSystem { get; } = isSystem; }
}

using FluentAssertions;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;
using System.Text.Json;

namespace FTCERP.Tests;

public sealed class OpmsImportControllerTests
{
    [Fact]
    public async Task Import_batch_identity_commit_transition_and_history_are_persistence_governed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var controller = Controller(context, setup);
        var staged = Payload(await controller.Stage(setup.Layer.PublicId, new StageOpmsImportRequest(
            Guid.NewGuid(), "governed.csv", [Row(2, "KPI-GOVERNED", setup.Department.Code)])));

        var batch = await context.OpmsImportBatches.SingleAsync(item => item.PublicId == staged.PublicId);
        batch.SourceFileName = "rewritten.csv";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*identity, source, reconciliation counts and creator evidence are immutable*");

        context.ChangeTracker.Clear();
        batch = await context.OpmsImportBatches.SingleAsync(item => item.PublicId == staged.PublicId);
        batch.Status = OpmsImportBatchStatus.Committed;
        batch.CommittedAt = DateTime.UtcNow;
        batch.CommittedByUserId = setup.User.Id;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*same-transaction reasoned before/after audit evidence*");

        context.ChangeTracker.Clear();
        batch = await context.OpmsImportBatches.SingleAsync(item => item.PublicId == staged.PublicId);
        context.OpmsImportBatches.Remove(batch);
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Invalid_row_blocks_the_complete_batch_without_business_writes()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var controller = Controller(context, setup);
        var response = Payload(await controller.Stage(setup.Layer.PublicId, new StageOpmsImportRequest(Guid.NewGuid(), "sdbip.csv",
        [
            Row(2, "KPI-NEW", setup.Department.Code),
            Row(3, "KPI-BAD", "UNKNOWN")
        ])));

        response.NewRows.Should().Be(1); response.InvalidRows.Should().Be(1);
        response.Rows.Single(x => x.Reference == "KPI-BAD").ErrorCode.Should().Be("DEPARTMENT_NOT_FOUND");
        var commit = await controller.Commit(response.PublicId, new CommitOpmsImportRequest("Approved", null, null, response.RowVersion));
        commit.Result.Should().BeOfType<BadRequestObjectResult>();
        (await context.OpmsTargets.CountAsync()).Should().Be(0);
        (await context.OpmsImportBatches.SingleAsync()).Status.Should().Be(OpmsImportBatchStatus.Staged);
    }

    [Fact]
    public async Task Valid_stage_is_idempotent_and_commit_creates_canonical_period_rows_atomically()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context); var controller = Controller(context, setup);
        var request = new StageOpmsImportRequest(Guid.NewGuid(), "approved.csv", [Row(2, "KPI-NEW", setup.Department.Code)]);
        var staged = Payload(await controller.Stage(setup.Layer.PublicId, request));
        var repeated = Payload(await controller.Stage(setup.Layer.PublicId, request));
        repeated.PublicId.Should().Be(staged.PublicId);

        var committed = Payload(await controller.Commit(staged.PublicId, new CommitOpmsImportRequest("Council approved", null, null, staged.RowVersion)));
        committed.Status.Should().Be("Committed");
        var target = await context.OpmsTargets.SingleAsync();
        target.SdbipLayerId.Should().Be(setup.Layer.Id);
        target.NationalKpaId.Should().Be(setup.NationalKpa.Id);
        target.MunicipalKpaId.Should().Be(setup.MunicipalKpa.Id);
        target.BackToBasicsPillarId.Should().Be(setup.BackToBasicsPillar.Id);
        target.StrategicGoalMasterId.Should().Be(setup.StrategicGoal.Id);
        target.StrategicInterventionId.Should().Be(setup.StrategicIntervention.Id);
        target.StrategicObjectiveMasterId.Should().Be(setup.StrategicObjective.Id);
        target.PerformanceObjectiveId.Should().Be(setup.PerformanceObjective.Id);
        target.AnnualTarget.Should().Be(0m, "new imports must not mirror the canonical annual value into the retired wide column");
        target.AnnualTargetDescription.Should().BeEmpty("new imports must not write retired wide target descriptions");
        target.Q1Target.Should().BeNull();
        target.Q2Target.Should().BeNull();
        target.MidTermTarget.Should().BeNull();
        target.Q3Target.Should().BeNull();
        target.Q4Target.Should().BeNull();
        var canonical = await context.PerformancePeriodTargets.Include(x => x.ReportingPeriod).SingleAsync();
        canonical.OpmsTargetId.Should().Be(target.Id); canonical.ReportingPeriod.PeriodType.Should().Be(ReportingPeriodType.Annual);
        canonical.UnitKind.Should().Be(PerformanceUnitKind.AbsoluteCount); canonical.TargetValue.Should().Be("100");
        (await context.OpmsImportRows.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Revision_import_governs_unit_and_direction_only_changes_with_exact_field_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var controller = Controller(context, setup);
        var initial = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "initial.csv", [Row(2, "KPI-UNIT", setup.Department.Code)])));
        _ = Payload(await controller.Commit(initial.PublicId,
            new CommitOpmsImportRequest("Approve initial KPI", null, null, initial.RowVersion)));

        var revisedRow = Row(2, "KPI-UNIT", setup.Department.Code) with
        {
            PeriodTargets =
            [
                new SaveTargetPeriodValueRequest(ReportingPeriodType.Annual, PerformanceUnitKind.PercentageBased,
                    PerformanceDirection.LowerIsBetter, "100", 1000, "Annual")
            ]
        };
        var revision = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "unit-direction-revision.csv", [revisedRow])));
        revision.ChangedRows.Should().Be(1);

        _ = Payload(await controller.Commit(revision.PublicId,
            new CommitOpmsImportRequest("Council approved unit and direction revision", "COUNCIL-UNIT-1",
                new DateTime(2026, 10, 13), revision.RowVersion)));

        var stored = await context.PerformancePeriodTargets.SingleAsync();
        stored.IsTargetRevised.Should().BeTrue();
        stored.RevisedTargetValue.Should().Be("100");
        stored.RevisedUnitKind.Should().Be(PerformanceUnitKind.PercentageBased);
        stored.RevisedOpmsUnitId.Should().NotBeNull().And.NotBe(stored.OpmsUnitId);
        stored.Direction.Should().Be(PerformanceDirection.LowerIsBetter);
        var fields = await context.PerformanceTargetRevisions.Where(item => item.PerformancePeriodTargetId == stored.Id)
            .Select(item => item.FieldName).ToArrayAsync();
        fields.Should().BeEquivalentTo(nameof(PerformancePeriodTarget.IsTargetRevised),
            nameof(PerformancePeriodTarget.RevisedTargetValue), nameof(PerformancePeriodTarget.RevisedUnitKind),
            nameof(PerformancePeriodTarget.RevisedOpmsUnitId), nameof(PerformancePeriodTarget.Direction),
            nameof(PerformancePeriodTarget.PerformanceDirectionId));

        var directionOnlyRow = revisedRow with
        {
            PeriodTargets =
            [
                new SaveTargetPeriodValueRequest(ReportingPeriodType.Annual, PerformanceUnitKind.PercentageBased,
                    PerformanceDirection.Exact, "100", 1000, "Annual")
            ]
        };
        var directionRevision = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "direction-only-revision.csv", [directionOnlyRow])));
        directionRevision.ChangedRows.Should().Be(1);
        _ = Payload(await controller.Commit(directionRevision.PublicId,
            new CommitOpmsImportRequest("Council approved direction-only revision", "COUNCIL-DIRECTION-2",
                new DateTime(2026, 10, 14), directionRevision.RowVersion)));

        stored.Direction.Should().Be(PerformanceDirection.Exact);
        var directionFields = await context.PerformanceTargetRevisions
            .Where(item => item.PerformancePeriodTargetId == stored.Id && item.ApprovalReference == "COUNCIL-DIRECTION-2")
            .Select(item => item.FieldName).ToArrayAsync();
        directionFields.Should().BeEquivalentTo(nameof(PerformancePeriodTarget.Direction),
            nameof(PerformancePeriodTarget.PerformanceDirectionId));
    }

    [Fact]
    public async Task Import_resolves_and_persists_governed_budget_type_and_multiple_sources()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context); var controller = Controller(context, setup);
        var row = Row(2, "KPI-BUDGET", setup.Department.Code) with
        {
            BudgetType = setup.BudgetType.Code,
            BudgetSources = $"{setup.BudgetSourceA.Code}:125.50;{setup.BudgetSourceB.Code}"
        };
        var staged = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "budget.csv", [row])));
        staged.InvalidRows.Should().Be(0);
        _ = Payload(await controller.Commit(staged.PublicId, new CommitOpmsImportRequest("Council approved budget import", null, null, staged.RowVersion)));

        var target = await context.OpmsTargets.Include(item => item.GovernedBudgetSources).SingleAsync();
        target.BudgetTypeMasterId.Should().Be(setup.BudgetType.Id);
        target.BudgetSourceId.Should().BeNull(); target.BudgetTypeId.Should().BeNull();
        target.GovernedBudgetSources.Should().HaveCount(2);
        target.GovernedBudgetSources.Should().Contain(item => item.BudgetSourceId == setup.BudgetSourceA.Id && item.Amount == 125.50m);
        target.GovernedBudgetSources.Should().Contain(item => item.BudgetSourceId == setup.BudgetSourceB.Id && item.Amount == null);
    }

    [Fact]
    public async Task Import_resolves_and_persists_all_governed_performance_classifications()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context); var controller = Controller(context, setup);
        var row = Row(2, "KPI-CLASSIFIED", setup.Department.Code) with
        {
            KpiType = setup.KpiType.Code!,
            IndicatorType = setup.IndicatorType.Code!,
            FunctionalArea = setup.FunctionalArea.Code,
            StandardClassification = setup.StandardClassification.Code
        };

        var staged = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "classified.csv", [row])));
        staged.InvalidRows.Should().Be(0);
        _ = Payload(await controller.Commit(staged.PublicId, new CommitOpmsImportRequest("Approved classified import", null, null, staged.RowVersion)));

        var target = await context.OpmsTargets.SingleAsync();
        target.KpiTypeMasterId.Should().Be(setup.KpiType.Id);
        target.IndicatorTypeMasterId.Should().Be(setup.IndicatorType.Id);
        target.FunctionalAreaMasterId.Should().Be(setup.FunctionalArea.Id);
        target.StandardClassificationMasterId.Should().Be(setup.StandardClassification.Id);
        target.KpiUnitOfMeasureMasterId.Should().Be(setup.KpiUnitOfMeasure.Id);
        target.UnitOfMeasureId.Should().BeNull();
        target.FunctionalArea.Should().Be(setup.FunctionalArea.Name);
        target.StandardClassification.Should().Be(setup.StandardClassification.Name);
    }

    [Fact]
    public async Task Import_cannot_resolve_a_strategic_master_from_another_municipality_even_when_query_filters_are_bypassed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var foreignMunicipality = new Municipality { Code = "FOREIGN", Name = "Foreign Municipality", IsActive = true };
        context.Add(foreignMunicipality); await context.SaveChangesAsync();
        context.Add(new MunicipalStrategicGoal { MunicipalityId = foreignMunicipality.Id, Code = "FOREIGN-GOAL", Name = "Foreign Goal" });
        await context.SaveChangesAsync();

        var row = Row(2, "KPI-CROSS-TENANT", setup.Department.Code) with { StrategicGoal = "FOREIGN-GOAL" };
        var staged = Payload(await Controller(context, setup).Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "cross-tenant.csv", [row])));

        staged.InvalidRows.Should().Be(1);
        staged.Rows.Single().ErrorCode.Should().Be("CLASSIFICATION_NOT_FOUND");
        staged.Rows.Single().ErrorField.Should().Be(nameof(OpmsImportRowRequest.StrategicGoal));
    }

    [Fact]
    public async Task Import_enforces_configured_strategic_hierarchy_relationships()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var configuredGoal = new MunicipalStrategicGoal { MunicipalityId = setup.Municipality.Id, Code = "ONLY-GOAL", Name = "Only configured goal" };
        context.Add(configuredGoal); await context.SaveChangesAsync();
        context.Add(new MunicipalKpaStrategicGoal
        {
            MunicipalityId = setup.Municipality.Id,
            MunicipalKpaId = setup.MunicipalKpa.Id,
            StrategicGoalId = configuredGoal.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var staged = Payload(await Controller(context, setup).Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "invalid-hierarchy.csv", [Row(2, "KPI-HIERARCHY", setup.Department.Code)])));

        staged.InvalidRows.Should().Be(1);
        staged.Rows.Single().ErrorCode.Should().Be("INVALID_STRATEGIC_CLASSIFICATION");
        staged.Rows.Single().ErrorMessage.Should().Contain("Strategic Goal is not configured");
    }

    [Fact]
    public async Task Import_history_masks_members_prevents_query_inference_and_uses_public_actor_identity()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_KPI.IMPORT" };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowed.Contains(code) && scope?.MunicipalityId == setup.Municipality.Id,
                    allowed.Contains(code) ? "Allowed" : "Denied", [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(setup.User, It.IsAny<string>()))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], [setup.Municipality.Id]));
        var controller = Controller(context, setup, access.Object);

        var staged = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "SECRET-OPMS-IMPORT.csv", [Row(2, "SECRET-KPI", "UNKNOWN")])));
        staged.ClientRequestId.Should().BeNull();
        staged.SourceFileName.Should().BeNull();
        staged.SourceSha256.Should().BeNull();
        staged.CreatedByUserPublicId.Should().BeNull();
        staged.CreatedByName.Should().BeNull();
        staged.Rows.Should().ContainSingle();
        staged.Rows[0].ExistingValueJson.Should().BeNull();
        staged.Rows[0].NormalizedJson.Should().BeNull();
        staged.Rows[0].SuppliedValue.Should().BeNull();
        staged.Rows[0].ErrorCode.Should().BeNull();
        staged.Rows[0].ErrorField.Should().BeNull();
        staged.Rows[0].ErrorMessage.Should().BeNull();

        var hiddenAction = await controller.GetBatchesPage(setup.Layer.PublicId,
            new PagedQueryRequest { Search = "SECRET-OPMS-IMPORT" });
        var hidden = ((hiddenAction.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<OpmsImportBatchSummaryResponse>>)!.Data!;
        hidden.TotalCount.Should().Be(0);
        (await controller.GetBatchesPage(setup.Layer.PublicId, new PagedQueryRequest { SortBy = "fileName" })).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        foreach (var member in new[] { "ImportClientRequestId", "ImportSourceFileName", "ImportSourceHash", "ImportActor", "ImportRowPayload", "ImportErrorDetail" })
            allowed.Add($"OPMS_KPI.{member}.READ");

        var visible = Payload(await controller.GetBatch(staged.PublicId));
        visible.ClientRequestId.Should().NotBeNull();
        visible.SourceFileName.Should().Be("SECRET-OPMS-IMPORT.csv");
        visible.SourceSha256.Should().HaveLength(64);
        visible.CreatedByUserPublicId.Should().Be(setup.User.PublicId);
        visible.CreatedByName.Should().Be(setup.User.FullName);
        visible.Rows[0].ErrorCode.Should().Be("DEPARTMENT_NOT_FOUND");
        visible.Rows[0].ErrorMessage.Should().NotBeNullOrWhiteSpace();

        var visibleAction = await controller.GetBatchesPage(setup.Layer.PublicId,
            new PagedQueryRequest { Search = "SECRET-OPMS-IMPORT", SortBy = "fileName" });
        var visiblePage = ((visibleAction.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<OpmsImportBatchSummaryResponse>>)!.Data!;
        visiblePage.Items.Should().ContainSingle().Which.CreatedByUserPublicId.Should().Be(setup.User.PublicId);
    }

    [Fact]
    public void Template_and_export_require_their_distinct_dynamic_permissions()
    {
        Policy(nameof(OpmsImportsController.Template)).Should().Be("Permission:OPMS_KPI.IMPORT");
        Policy(nameof(OpmsImportsController.Export)).Should().Be("Permission:OPMS_KPI.EXPORT");
        Policy(nameof(OpmsImportsController.Stage)).Should().Be("Permission:OPMS_KPI.IMPORT");
        Policy(nameof(OpmsImportsController.Commit)).Should().Be("Permission:OPMS_KPI.IMPORT");
        Policy(nameof(OpmsImportsController.GetBatchesPage)).Should().Be("Permission:OPMS_KPI.IMPORT");
        Policy(nameof(OpmsImportsController.GetBatch)).Should().Be("Permission:OPMS_KPI.IMPORT");
    }

    [Fact]
    public async Task Export_emits_effective_revised_values_in_the_importable_wide_contract()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        await SeedExportTargetAsync(context, setup);

        var result = await Controller(context, setup).Export(setup.Layer.PublicId);
        var file = result.Should().BeOfType<FileContentResult>().Subject;
        var csv = Encoding.UTF8.GetString(file.FileContents).TrimStart('\uFEFF');
        csv.Should().Contain("EXISTING_INDICATOR_NUMBER,INDICATOR_NUMBER");
        csv.Should().Contain("\"KPI-REV\",\"KPI-REV\",\"3\"");
        csv.Should().Contain("\"'=Revised target\"");
        csv.Should().Contain("\"CAPEX\",\"MIG:125.5\"");
        csv.Should().Contain("\"Output\",\"Quantitative\",\"TECHNICAL\",\"SERVICE\",\"COUNT\"");
        csv.Should().Contain("\"125\",\"ABSOLUTE_COUNT\",\"HIGHER_IS_BETTER\",\"1250.0\"");
    }

    [Fact]
    public async Task Export_masks_sensitive_period_and_budget_source_values_until_exact_read_grants_exist()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        await SeedExportTargetAsync(context, setup);
        var readable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(setup.User, It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string code) => code == "OPMS_KPI.EXPORT" || readable.Contains(code)
                ? code == "OPMS_KPI.EXPORT"
                    ? new AccessQueryScopeResult(true, true, [], [], [], [], [], [setup.Municipality.Id])
                    : new AccessQueryScopeResult(true, false, [setup.Department.Id], [], [], [], [], [])
                : new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var controller = Controller(context, setup, access.Object);

        var hiddenFile = (await controller.Export(setup.Layer.PublicId)).Should().BeOfType<FileContentResult>().Subject;
        var hidden = Encoding.UTF8.GetString(hiddenFile.FileContents).TrimStart('\uFEFF');
        hidden.Should().Contain("\"CAPEX\",\"MIG\"");
        hidden.Should().NotContain("MIG:125.5");
        hidden.Should().NotContain("\"125\",\"ABSOLUTE_COUNT\",\"HIGHER_IS_BETTER\",\"1250.0\"");

        foreach (var member in new[] { "PeriodTargetValue", "PeriodBudgetValue", "PeriodDescription", "BudgetSourceAmounts" })
            readable.Add($"OPMS_KPI.{member}.READ");

        var visibleFile = (await controller.Export(setup.Layer.PublicId)).Should().BeOfType<FileContentResult>().Subject;
        var visible = Encoding.UTF8.GetString(visibleFile.FileContents).TrimStart('\uFEFF');
        visible.Should().Contain("\"CAPEX\",\"MIG:125.5\"");
        visible.Should().Contain("\"125\",\"ABSOLUTE_COUNT\",\"HIGHER_IS_BETTER\",\"1250.0\"");
    }

    [Fact]
    public async Task Import_preview_composes_row_payload_and_sensitive_member_read_permissions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_KPI.IMPORT", "OPMS_KPI.ImportRowPayload.READ" };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowed.Contains(code) && scope?.MunicipalityId == setup.Municipality.Id, code, [], [], []));
        var controller = Controller(context, setup, access.Object);
        var row = Row(2, "KPI-PREVIEW", setup.Department.Code) with
        {
            BudgetType = setup.BudgetType.Code,
            BudgetSources = $"{setup.BudgetSourceA.Code}:125.50"
        };

        var staged = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "preview.csv", [row])));
        using (var hidden = JsonDocument.Parse(staged.Rows.Single().NormalizedJson!))
        {
            hidden.RootElement.GetProperty("budgetSources")[0].GetProperty("amount").ValueKind.Should().Be(JsonValueKind.Null);
            var period = hidden.RootElement.GetProperty("periodTargets")[0];
            period.GetProperty("targetValue").GetString().Should().BeEmpty();
            period.GetProperty("budgetValue").ValueKind.Should().Be(JsonValueKind.Null);
            period.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        }

        foreach (var member in new[] { "PeriodTargetValue", "PeriodBudgetValue", "PeriodDescription", "BudgetSourceAmounts" })
            allowed.Add($"OPMS_KPI.{member}.READ");
        var visibleBatch = Payload(await controller.GetBatch(staged.PublicId));
        using var visible = JsonDocument.Parse(visibleBatch.Rows.Single().NormalizedJson!);
        visible.RootElement.GetProperty("budgetSources")[0].GetProperty("amount").GetDecimal().Should().Be(125.50m);
        var visiblePeriod = visible.RootElement.GetProperty("periodTargets")[0];
        visiblePeriod.GetProperty("targetValue").GetString().Should().Be("100");
        visiblePeriod.GetProperty("budgetValue").GetDecimal().Should().Be(1000m);
        visiblePeriod.GetProperty("description").GetString().Should().Be("Annual");
    }

    [Fact]
    public async Task Import_commit_denies_sensitive_writes_atomically_and_dynamic_grants_take_effect()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var direct = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_KPI.IMPORT" };
        var scoped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(direct.Contains(code) && scope?.MunicipalityId == setup.Municipality.Id, code, [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(setup.User, It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string code) => scoped.Contains(code)
                ? new AccessQueryScopeResult(true, false, [setup.Department.Id], [], [], [], [], [])
                : new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var controller = Controller(context, setup, access.Object);
        var row = Row(2, "KPI-GOVERNED", setup.Department.Code) with
        {
            BudgetType = setup.BudgetType.Code,
            BudgetSources = $"{setup.BudgetSourceA.Code}:125.50"
        };
        var staged = Payload(await controller.Stage(setup.Layer.PublicId,
            new StageOpmsImportRequest(Guid.NewGuid(), "governed.csv", [row])));

        var denied = await controller.Commit(staged.PublicId,
            new CommitOpmsImportRequest("Governed import", null, null, staged.RowVersion));
        denied.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await context.OpmsTargets.CountAsync()).Should().Be(0);
        (await context.PerformancePeriodTargets.CountAsync()).Should().Be(0);

        foreach (var member in new[] { "PeriodTargetValue", "PeriodBudgetValue", "PeriodDescription", "BudgetSourceAmounts" })
            scoped.Add($"OPMS_KPI.{member}.UPDATE");
        _ = Payload(await controller.Commit(staged.PublicId,
            new CommitOpmsImportRequest("Governed import", null, null, staged.RowVersion)));
        var saved = await context.OpmsTargets.Include(item => item.GovernedBudgetSources).SingleAsync();
        saved.GovernedBudgetSources.Single().Amount.Should().Be(125.50m);
        (await context.PerformancePeriodTargets.SingleAsync()).TargetValue.Should().Be("100");
    }

    private static string? Policy(string method) => typeof(OpmsImportsController).GetMethod(method)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;

    private static async Task SeedExportTargetAsync(ApplicationDbContext context, Setup setup)
    {
        var target = new OpmsTarget
        {
            MunicipalityId = setup.Municipality.Id, SdbipLayerId = setup.Layer.Id, PeriodId = setup.LegacyPeriod.Id,
            DepartmentId = setup.Department.Id, IndicatorNumber = "KPI-1", IsIndicatorNumberRevised = true, RevisedIndicatorNumber = "KPI-REV",
            OriginalOrderNumber = 1, RevisedOrderNumber = 3, TargetName = "Original", IsTargetNameRevised = true, RevisedTargetName = "=Revised target",
            KpiDescription = "Original wording", NationalKpa = "Basic Services", MunicipalKpa = "Service Delivery", PerformanceObjective = "Improve access",
            NationalKpaId = setup.NationalKpa.Id, MunicipalKpaId = setup.MunicipalKpa.Id, BackToBasicsPillarId = setup.BackToBasicsPillar.Id,
            StrategicGoalMasterId = setup.StrategicGoal.Id, StrategicInterventionId = setup.StrategicIntervention.Id,
            StrategicObjectiveMasterId = setup.StrategicObjective.Id, PerformanceObjectiveId = setup.PerformanceObjective.Id,
            BudgetTypeMasterId = setup.BudgetType.Id, KpiTypeMasterId = setup.KpiType.Id, IndicatorTypeMasterId = setup.IndicatorType.Id,
            FunctionalAreaMasterId = setup.FunctionalArea.Id, StandardClassificationMasterId = setup.StandardClassification.Id,
            KpiUnitOfMeasureMasterId = setup.KpiUnitOfMeasure.Id,
            KpiType = "Output", IndicatorType = "Quantitative", AnnualTargetDescription = "Annual", Weight = 10
        };
        target.GovernedBudgetSources.Add(new OpmsKpiBudgetSource { MunicipalityId = setup.Municipality.Id, BudgetSourceId = setup.BudgetSourceA.Id, Amount = 125.50m });
        context.Add(target);
        await context.SaveChangesAsync();
        context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
        {
            MunicipalityId = setup.Municipality.Id, ReportingPeriodId = setup.Annual.Id, OpmsTargetId = target.Id,
            UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = "100", IsTargetRevised = true, RevisedTargetValue = "125", RevisedUnitKind = PerformanceUnitKind.AbsoluteCount,
            BudgetValue = 1000, IsBudgetRevised = true, RevisedBudgetValue = 1250, Description = "Annual", CreatedByUserId = setup.User.Id
        });
        await context.SaveChangesAsync();
    }

    private static OpmsImportRowRequest Row(int sourceRow, string indicator, string department) => new(sourceRow, indicator, 1,
        "Water connections", "Households connected", department, null, "Basic Services", "Service Delivery", "Improve access",
        "Strategic Goal", "Strategic Intervention", "Strategic Objective", "Performance Objective",
        0, 10, "Output", "Quantitative",
        [new SaveTargetPeriodValueRequest(ReportingPeriodType.Annual, PerformanceUnitKind.AbsoluteCount, PerformanceDirection.HigherIsBetter, "100", 1000, "Annual")],
        KpiUnitOfMeasure: "COUNT");

    private static OpmsImportsController Controller(ApplicationDbContext context, Setup setup, IAccessControlService? configuredAccess = null)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(x => x.GetQueryScopeAsync(setup.User, It.IsAny<string>())).ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], [setup.Municipality.Id]));
        access.Setup(x => x.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string _, AccessScopeContext? scope) => new AccessDecisionResult(scope?.MunicipalityId == setup.Municipality.Id, "Allowed", [], [], []));
        return new OpmsImportsController(context, IdpTestFixture.CreateUserManagerMock(setup.User).Object,
            new Tenant(setup.Municipality.Id, setup.User.Id), new PerformanceUnitEngine(),
            new WorkflowGovernanceService(context, tenantContext: new Tenant(setup.Municipality.Id, setup.User.Id)), configuredAccess ?? access.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(setup.User.Id) } } };
    }

    private static OpmsImportBatchResponse Payload(ActionResult<ApiResponse<OpmsImportBatchResponse>> action)
    {
        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeOfType<ApiResponse<OpmsImportBatchResponse>>().Subject.Data!;
    }

    private static async Task<Setup> SeedAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "IMPORT", Name = "Import Municipality", IsActive = true };
        var user = IdpTestFixture.CreateUser("opms-importer"); context.AddRange(municipality, user); await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var year = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30), IsActive = true };
        var department = new Department { MunicipalityId = municipality.Id, Code = "TECH", Name = "Technical Services", IsActive = true };
        var legacy = new Period { Code = "FY26", Name = "2026/27", FiscalYear = year.Code, StartDate = year.StartDate, EndDate = year.EndDate, IsActive = true };
        context.AddRange(year, department, legacy); await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = year.Id, IsCurrent = true, IsActive = true, EffectiveFrom = year.StartDate };
        context.Add(municipalityYear); await context.SaveChangesAsync();
        var nationalKpa = new NationalKpa { Code = "Basic Services", Name = "Basic Services" };
        var municipalKpa = new MunicipalKpa { MunicipalityId = municipality.Id, Code = "Service Delivery", Name = "Service Delivery" };
        var backToBasics = new BackToBasicsPillar { Code = "Improve access", Name = "Improve access" };
        var strategicGoal = new MunicipalStrategicGoal { MunicipalityId = municipality.Id, Code = "Strategic Goal", Name = "Strategic Goal" };
        var strategicIntervention = new StrategicIntervention { MunicipalityId = municipality.Id, Code = "Strategic Intervention", Name = "Strategic Intervention" };
        var strategicObjective = new MunicipalStrategicObjective { MunicipalityId = municipality.Id, Code = "Strategic Objective", Name = "Strategic Objective" };
        var performanceObjective = new PerformanceObjective { MunicipalityId = municipality.Id, Code = "Performance Objective", Name = "Performance Objective" };
        var budgetType = new GovernedBudgetType { MunicipalityId = municipality.Id, Code = "CAPEX", Name = "Capital expenditure" };
        var budgetSourceA = new GovernedBudgetSource { MunicipalityId = municipality.Id, Code = "MIG", Name = "Infrastructure grant" };
        var budgetSourceB = new GovernedBudgetSource { MunicipalityId = municipality.Id, Code = "OWN", Name = "Own revenue" };
        var kpiType = new GovernedKpiType { MunicipalityId = municipality.Id, Code = "Output", Name = "Output" };
        var indicatorType = new GovernedIndicatorType { MunicipalityId = municipality.Id, Code = "Quantitative", Name = "Quantitative" };
        var functionalArea = new GovernedFunctionalArea { MunicipalityId = municipality.Id, Code = "TECHNICAL", Name = "Technical Services" };
        var standardClassification = new GovernedStandardClassification { MunicipalityId = municipality.Id, Code = "SERVICE", Name = "Service Delivery" };
        var kpiUnitOfMeasure = new GovernedKpiUnitOfMeasure { MunicipalityId = municipality.Id, Code = "COUNT", Name = "Count", Symbol = "#" };
        context.AddRange(nationalKpa, municipalKpa, backToBasics, strategicGoal, strategicIntervention, strategicObjective, performanceObjective, budgetType, budgetSourceA, budgetSourceB, kpiType, indicatorType, functionalArea, standardClassification, kpiUnitOfMeasure);
        await context.SaveChangesAsync();
        var reportingPeriod = new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANNUAL", Name = "Annual", PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = year.StartDate, EndDate = year.EndDate, IsActive = true };
        var layer = new SdbipLayer { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id, Code = "TOP", Name = "Top Layer", IsActive = true };
        context.AddRange(reportingPeriod, layer); await context.SaveChangesAsync();
        return new Setup(municipality, user, department, layer, reportingPeriod, legacy, nationalKpa, municipalKpa, backToBasics,
            strategicGoal, strategicIntervention, strategicObjective, performanceObjective, budgetType, budgetSourceA, budgetSourceB,
            kpiType, indicatorType, functionalArea, standardClassification, kpiUnitOfMeasure);
    }

    private sealed record Setup(Municipality Municipality, ApplicationUser User, Department Department, SdbipLayer Layer, ReportingPeriod Annual, Period LegacyPeriod,
        NationalKpa NationalKpa, MunicipalKpa MunicipalKpa, BackToBasicsPillar BackToBasicsPillar, MunicipalStrategicGoal StrategicGoal,
        StrategicIntervention StrategicIntervention, MunicipalStrategicObjective StrategicObjective, PerformanceObjective PerformanceObjective,
        GovernedBudgetType BudgetType, GovernedBudgetSource BudgetSourceA, GovernedBudgetSource BudgetSourceB,
        GovernedKpiType KpiType, GovernedIndicatorType IndicatorType, GovernedFunctionalArea FunctionalArea,
        GovernedStandardClassification StandardClassification, GovernedKpiUnitOfMeasure KpiUnitOfMeasure);
    private sealed class Tenant(long municipalityId, string userId) : ITenantContext { public long? MunicipalityId => municipalityId; public bool IsSystem => false; public string? UserId => userId; }
}

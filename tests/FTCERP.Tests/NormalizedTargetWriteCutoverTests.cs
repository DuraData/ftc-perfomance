using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FTCERP.Tests;

public sealed class NormalizedTargetWriteCutoverTests
{
    [Fact]
    public void Target_reads_options_and_submission_links_expose_public_ids_only()
    {
        var privateTargetProperties = new[]
        {
            "Id", "SourceTemplateId", "PeriodId", "DepartmentId", "UnitId", "WardIds", "VoteNumberIds",
            "StrategicGoalId", "StrategicObjectiveId", "BudgetSourceId", "BudgetTypeId", "UnitOfMeasureId",
            "RelatedOpmsTargetId"
        };

        foreach (var contract in new[] { typeof(OpmsTargetResponse), typeof(IpmsTargetResponse) })
        {
            Assert.Equal(typeof(Guid), contract.GetProperty("PublicId")!.PropertyType);
            foreach (var property in privateTargetProperties)
                Assert.Null(contract.GetProperty(property));
        }

        Assert.Equal(typeof(Guid), typeof(PerformanceTargetOptionResponse).GetProperty("PublicId")!.PropertyType);
        Assert.Equal(typeof(Guid?), typeof(PerformanceTargetOptionResponse).GetProperty("DepartmentPublicId")!.PropertyType);
        Assert.Null(typeof(PerformanceTargetOptionResponse).GetProperty("Id"));
        Assert.Null(typeof(PerformanceTargetOptionResponse).GetProperty("DepartmentId"));
        Assert.Null(typeof(OpmsTargetVoteNumberResponse).GetProperty("Id"));

        Assert.Equal(typeof(Guid), typeof(SaveOpmsSubmissionRequest).GetProperty("OpmsTargetPublicId")!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(SaveIpmsSubmissionRequest).GetProperty("IpmsTargetPublicId")!.PropertyType);
        Assert.Null(typeof(SaveOpmsSubmissionRequest).GetProperty("OpmsTargetId"));
        Assert.Null(typeof(SaveIpmsSubmissionRequest).GetProperty("IpmsTargetId"));
        Assert.Equal(typeof(Guid), typeof(OpmsSubmissionResponse).GetProperty("OpmsTargetPublicId")!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(IpmsSubmissionResponse).GetProperty("IpmsTargetPublicId")!.PropertyType);
        Assert.Null(typeof(OpmsSubmissionResponse).GetProperty("OpmsTargetId"));
        Assert.Null(typeof(IpmsSubmissionResponse).GetProperty("IpmsTargetId"));
    }

    [Fact]
    public void Target_detail_mutation_and_history_routes_require_public_guids()
    {
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.GetTarget), "{id:guid}");
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.UpdateTarget), "{id:guid}");
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.ReviseOrdering), "{id:guid}/ordering");
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.ReviseDefinitionFields), "{id:guid}/field-revisions");
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.GetOrderingRevisionsPage), "{id:guid}/ordering-revisions/page");
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.GetFieldRevisionsPage), "{id:guid}/field-revisions/page");
        AssertGuidRoute<OpmsTargetsController>(nameof(OpmsTargetsController.WithdrawTarget), "{id:guid}/withdraw");

        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.GetTarget), "{id:guid}");
        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.UpdateTarget), "{id:guid}");
        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.ReviseOrdering), "{id:guid}/ordering");
        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.ReviseDefinitionFields), "{id:guid}/field-revisions");
        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.GetOrderingRevisionsPage), "{id:guid}/ordering-revisions/page");
        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.GetFieldRevisionsPage), "{id:guid}/field-revisions/page");
        AssertGuidRoute<IpmsTargetsController>(nameof(IpmsTargetsController.WithdrawTarget), "{id:guid}/withdraw");
    }

    [Fact]
    public void Public_target_contracts_expose_only_canonical_period_values()
    {
        var retired = new[]
        {
            "AnnualTarget", "AnnualTargetDescription", "TargetUnitType", "Q1Target", "Q1Description", "Q1Budget",
            "Q2Target", "Q2Description", "Q2Budget", "MidTermTarget", "MidTermDescription", "MidTermBudget",
            "Q3Target", "Q3Description", "Q3Budget", "Q3RevisedTarget", "Q4Target", "Q4Description", "Q4Budget",
            "Q4RevisedTarget", "RevisedAnnualTarget", "RevisedAnnualBudget"
        };

        foreach (var contract in new[] { typeof(SaveOpmsTargetRequest), typeof(SaveIpmsTargetRequest), typeof(OpmsTargetResponse), typeof(IpmsTargetResponse) })
        {
            Assert.NotNull(contract.GetProperty("PeriodTargets"));
            foreach (var property in retired)
                Assert.Null(contract.GetProperty(property));
        }
        Assert.Null(typeof(SaveOpmsTargetRequest).GetProperty("IsWithdrawn"));
        Assert.Null(typeof(SaveOpmsTargetRequest).GetProperty("ReasonForWithdrawal"));

        Assert.Equal(typeof(Guid), typeof(SaveOpmsTargetRequest).GetProperty("MunicipalityFinancialYearPublicId")!.PropertyType);
        Assert.Equal(typeof(Guid[]), typeof(SaveOpmsTargetRequest).GetProperty("WardPublicIds")!.PropertyType);
        Assert.Equal(typeof(Guid[]), typeof(SaveOpmsTargetRequest).GetProperty("VoteNumberPublicIds")!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(SaveIpmsTargetRequest).GetProperty("MunicipalityFinancialYearPublicId")!.PropertyType);
        Assert.Equal(typeof(Guid?), typeof(SaveIpmsTargetRequest).GetProperty("RelatedOpmsTargetPublicId")!.PropertyType);

        var retiredWriteProperties = new[]
        {
            "SourceTemplateId", "PeriodId", "DepartmentId", "UnitId", "WardIds", "VoteNumberIds",
            "StrategicGoalId", "StrategicObjectiveId", "BudgetSourceId", "BudgetTypeId", "UnitOfMeasureId",
            "RelatedOpmsTargetId"
        };
        foreach (var contract in new[] { typeof(SaveOpmsTargetRequest), typeof(SaveIpmsTargetRequest) })
            foreach (var property in retiredWriteProperties)
                Assert.Null(contract.GetProperty(property));
    }

    [Fact]
    public async Task Ipms_create_uses_the_same_normalized_period_write_path()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = IpmsController(context, seed.User, seed.Municipality.Id);

        var result = await controller.CreateTarget(IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications));

        var response = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == (int)ReportingPeriodType.Annual && item.TargetValue == "100");
        context.ChangeTracker.Clear();
        var stored = await context.IpmsTargets.SingleAsync();
        Assert.Equal(0m, stored.AnnualTarget);
        Assert.Equal("absolute_count", stored.TargetUnitType);
        Assert.Equal(response.PublicId, stored.PublicId);
        Assert.Equal(2, await context.PerformancePeriodTargets.CountAsync(item => item.IpmsTargetId == stored.Id));
    }

    [Fact]
    public async Task Opms_create_persists_normalized_period_rows_without_writing_legacy_wide_values()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);

        var result = await controller.CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications));

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == (int)ReportingPeriodType.Annual && item.TargetValue == "100");
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == (int)ReportingPeriodType.Quarter1 && item.TargetValue == "20");
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.SingleAsync();
        Assert.Equal(seed.SdbipLayer.Id, stored.SdbipLayerId);
        Assert.NotNull(stored.NationalKpaId);
        Assert.NotNull(stored.MunicipalKpaId);
        Assert.NotNull(stored.BackToBasicsPillarId);
        Assert.NotNull(stored.StrategicGoalMasterId);
        Assert.NotNull(stored.StrategicInterventionId);
        Assert.NotNull(stored.StrategicObjectiveMasterId);
        Assert.NotNull(stored.PerformanceObjectiveId);
        Assert.Null(stored.StrategicGoalId);
        Assert.Null(stored.StrategicObjectiveId);
        Assert.Equal(0m, stored.AnnualTarget);
        Assert.Equal("absolute_count", stored.TargetUnitType);
        Assert.Null(stored.Q1Target);
        var normalized = await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).Include(item => item.OpmsUnit).Include(item => item.PerformanceDirectionDefinition).OrderBy(item => item.ReportingPeriod.Sequence).ToArrayAsync();
        Assert.Equal(2, normalized.Length);
        Assert.All(normalized, item =>
        {
            Assert.NotNull(item.OpmsUnitId);
            Assert.Equal("PERCENT", item.OpmsUnit!.Code);
            Assert.NotNull(item.PerformanceDirectionId);
            Assert.Equal("TARGET_OR_HIGHER", item.PerformanceDirectionDefinition!.Code);
        });
        Assert.Contains(normalized, item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter1 && item.TargetValue == "20" && item.BudgetValue == 10m);
        Assert.Contains(normalized, item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual && item.TargetValue == "100");
    }

    [Fact]
    public async Task Canonical_contract_preserves_period_specific_units_and_non_numeric_values()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var request = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            PeriodTargets =
            [
                new(ReportingPeriodType.Quarter1, PerformanceUnitKind.QualitativeTargets, PerformanceDirection.Exact, "Council approved", null, "Qualitative milestone"),
                new(ReportingPeriodType.Annual, PerformanceUnitKind.Date, PerformanceDirection.LowerIsBetter, "2027-06-30", null, "Completion date")
            ]
        };

        var result = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget(request);

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == (int)ReportingPeriodType.Quarter1 && item.UnitKind == (int)PerformanceUnitKind.QualitativeTargets && item.TargetValue == "Council approved");
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == (int)ReportingPeriodType.Annual && item.UnitKind == (int)PerformanceUnitKind.Date && item.TargetValue == "2027-06-30");
    }

    [Fact]
    public async Task Canonical_master_ids_drive_period_unit_and_direction_instead_of_legacy_enums()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var date = await context.OpmsUnitDefinitions.SingleAsync(item => item.Code == "DATE");
        var onOrBefore = await context.PerformanceDirectionDefinitions.SingleAsync(item => item.Code == "ON_OR_BEFORE_DATE");
        var request = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            PeriodTargets =
            [
                new SaveTargetPeriodValueRequest(ReportingPeriodType.Annual, PerformanceUnitKind.AbsoluteCount,
                    PerformanceDirection.HigherIsBetter, "2027-06-30", null, "Completion date")
                {
                    OpmsUnitPublicId = date.PublicId,
                    PerformanceDirectionPublicId = onOrBefore.PublicId
                }
            ]
        };

        var result = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget(request);

        Assert.IsType<OkObjectResult>(result.Result);
        var stored = await context.PerformancePeriodTargets.Include(item => item.OpmsUnit).Include(item => item.PerformanceDirectionDefinition).SingleAsync();
        Assert.Equal(date.Id, stored.OpmsUnitId);
        Assert.Equal(PerformanceUnitKind.Date, stored.UnitKind);
        Assert.Equal(onOrBefore.Id, stored.PerformanceDirectionId);
        Assert.Equal(PerformanceDirection.LowerIsBetter, stored.Direction);
        Assert.Equal("2027-06-30", stored.TargetValue);
    }

    [Fact]
    public async Task General_target_update_cannot_bypass_governed_period_revision_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;
        var changed = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            RowVersion = created.RowVersion,
            PeriodTargets = PeriodTargets("90")
        };

        var result = await controller.UpdateTarget(created.PublicId, changed);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<OpmsTargetResponse>>(conflict.Value);
        Assert.Contains("governed records", envelope.Message);
        Assert.Equal("100", (await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).SingleAsync(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)).TargetValue);
        Assert.Empty(await context.PerformanceTargetRevisions.ToArrayAsync());
    }

    [Fact]
    public async Task General_target_update_keeps_governed_values_and_updates_only_non_revision_metadata()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;

        var result = await controller.UpdateTarget(created.PublicId, Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            RowVersion = created.RowVersion,
            InternalReference = "Updated metadata"
        });

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal("Updated metadata", response.InternalReference);
        Assert.Equal("Normalized target", response.TargetName);
        Assert.Contains(response.PeriodTargets, item => item.PeriodType == (int)ReportingPeriodType.Annual && item.TargetValue == "100");
        Assert.Equal(2, await context.PerformancePeriodTargets.CountAsync());
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.SingleAsync();
        Assert.Equal("Updated metadata", stored.InternalReference);
        Assert.Equal("Normalized target", stored.TargetName);
        Assert.Equal(0m, stored.AnnualTarget);
    }

    [Fact]
    public async Task Opms_and_ipms_target_mutations_persist_atomic_audits_and_reject_stale_edits()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var opms = Controller(context, seed.User, seed.Municipality.Id, persistGovernance: true);
        var ipms = IpmsController(context, seed.User, seed.Municipality.Id, persistGovernance: true);

        var opmsCreated = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.CreateTarget(
            Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;
        var ipmsCreated = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipms.CreateTarget(
            IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications))).Result).Value).Data!;

        var opmsEdited = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.UpdateTarget(opmsCreated.PublicId,
            Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
            { RowVersion = opmsCreated.RowVersion, InternalReference = "Atomic OPMS metadata" })).Result).Value).Data!;
        var ipmsEdited = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipms.UpdateTarget(ipmsCreated.PublicId,
            IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications) with
            { RowVersion = ipmsCreated.RowVersion, InternalReference = "Atomic IPMS metadata" })).Result).Value).Data!;

        var effectiveAt = new DateTime(2030, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var opmsOrdered = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.ReviseOrdering(opmsCreated.PublicId,
            new ReviseKpiOrderingRequest(2, 3, "Approved OPMS ordering", "OPMS-ORDER-1", effectiveAt, opmsEdited.RowVersion))).Result).Value).Data!;
        var ipmsOrdered = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipms.ReviseOrdering(ipmsCreated.PublicId,
            new ReviseKpiOrderingRequest(2, 3, "Approved IPMS ordering", "IPMS-ORDER-1", effectiveAt, ipmsEdited.RowVersion))).Result).Value).Data!;

        Assert.IsType<OkObjectResult>((await opms.ReviseDefinitionFields(opmsCreated.PublicId,
            new ReviseKpiDefinitionRequest(false, null, true, "Governed OPMS target", false, null,
                "Approved OPMS wording", "OPMS-WORD-1", effectiveAt, opmsOrdered.RowVersion))).Result);
        Assert.IsType<OkObjectResult>((await ipms.ReviseDefinitionFields(ipmsCreated.PublicId,
            new ReviseKpiDefinitionRequest(false, null, true, "Governed IPMS target", false, null,
                "Approved IPMS wording", "IPMS-WORD-1", effectiveAt, ipmsOrdered.RowVersion))).Result);

        var audits = await context.AuditTrails
            .Where(item => item.EntityName == nameof(OpmsTarget) || item.EntityName == nameof(IpmsTarget))
            .ToArrayAsync();
        Assert.Equal(8, audits.Length);
        Assert.All(new[] { "Create", "Edit", "ReviseOrdering", "ReviseDefinitionFields" },
            action => Assert.Equal(2, audits.Count(item => item.Action == action)));
        Assert.All(audits, item =>
        {
            Assert.Equal(seed.Municipality.Id, item.MunicipalityId);
            Assert.False(string.IsNullOrWhiteSpace(item.Reason));
        });
        Assert.Contains(audits, item => item.Action == "Edit" && item.NewValue!.Contains("Atomic OPMS metadata"));
        Assert.Contains(audits, item => item.Action == "Edit" && item.NewValue!.Contains("Atomic IPMS metadata"));

        var stale = await opms.UpdateTarget(opmsCreated.PublicId,
            Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
            { RowVersion = opmsCreated.RowVersion, InternalReference = "Stale overwrite" });
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.Equal(8, await context.AuditTrails.CountAsync(item => item.EntityName == nameof(OpmsTarget) || item.EntityName == nameof(IpmsTarget)));
    }

    [Fact]
    public async Task Opms_sensitive_members_are_masked_and_preserved_when_their_dynamic_grants_are_removed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await Controller(context, seed.User, seed.Municipality.Id)
            .CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;
        var source = new GovernedBudgetSource { MunicipalityId = seed.Municipality.Id, Code = "OWN", Name = "Own revenue" };
        context.GovernedBudgetSources.Add(source);
        await context.SaveChangesAsync();
        var entity = await context.OpmsTargets.SingleAsync(item => item.PublicId == created.PublicId);
        entity.InternalReference = "INTERNAL-SECRET";
        entity.FmsLink = "FMS-SECRET";
        entity.GovernedBudgetSources.Add(new OpmsKpiBudgetSource { MunicipalityId = seed.Municipality.Id, BudgetSourceId = source.Id, Amount = 125.50m, AllocatedByUserId = seed.User.Id });
        context.AuditTrails.Add(TargetEditAudit(nameof(OpmsTarget), entity.Id, seed.Municipality.Id, seed.User.Id, "Seed protected OPMS members.",
            new { entity.PublicId, InternalReference = (string?)null, FmsLink = (string?)null },
            new { entity.PublicId, entity.InternalReference, entity.FmsLink }));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var denied = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OPMS_KPI.InternalReference.READ", "OPMS_KPI.FmsLink.READ", "OPMS_KPI.BudgetSourceAmounts.READ",
            "OPMS_KPI.InternalReference.UPDATE", "OPMS_KPI.FmsLink.UPDATE", "OPMS_KPI.BudgetSourceAmounts.UPDATE"
        };
        var controller = Controller(context, seed.User, seed.Municipality.Id, denied);
        var currentVersion = Convert.ToBase64String((await context.OpmsTargets.AsNoTracking()
            .SingleAsync(item => item.PublicId == created.PublicId)).RowVersion);
        var request = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            RowVersion = currentVersion,
            InternalReference = "HOSTILE-REPLACEMENT",
            FmsLink = "HOSTILE-FMS",
            InternalReferenceSpecified = false,
            FmsLinkSpecified = false,
            BudgetSourceAmountsSpecified = false,
            BudgetSources = [new SaveKpiBudgetSourceRequest(source.PublicId, 999m)]
        };

        var response = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.UpdateTarget(created.PublicId, request)).Result).Value).Data!;

        Assert.Null(response.InternalReference);
        Assert.Null(response.FmsLink);
        Assert.Null(Assert.Single(response.BudgetSources).Amount);
        context.ChangeTracker.Clear();
        var stored = await context.OpmsTargets.Include(item => item.GovernedBudgetSources).SingleAsync(item => item.PublicId == created.PublicId);
        Assert.Equal("INTERNAL-SECRET", stored.InternalReference);
        Assert.Equal("FMS-SECRET", stored.FmsLink);
        Assert.Equal(125.50m, Assert.Single(stored.GovernedBudgetSources, item => item.IsActive).Amount);

        var forbidden = await controller.UpdateTarget(created.PublicId, request with { InternalReferenceSpecified = true });
        Assert.IsType<ForbidResult>(forbidden.Result);
    }

    [Fact]
    public async Task Ipms_sensitive_members_are_masked_and_preserved_when_their_dynamic_grants_are_removed()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var created = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await IpmsController(context, seed.User, seed.Municipality.Id)
            .CreateTarget(IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications))).Result).Value).Data!;
        var source = new GovernedBudgetSource { MunicipalityId = seed.Municipality.Id, Code = "GRANT", Name = "Conditional grant" };
        context.GovernedBudgetSources.Add(source);
        await context.SaveChangesAsync();
        var entity = await context.IpmsTargets.SingleAsync(item => item.PublicId == created.PublicId);
        entity.InternalReference = "EMPLOYEE-SECRET";
        entity.GovernedBudgetSources.Add(new IpmsKpiBudgetSource { MunicipalityId = seed.Municipality.Id, BudgetSourceId = source.Id, Amount = 75m, AllocatedByUserId = seed.User.Id });
        context.AuditTrails.Add(TargetEditAudit(nameof(IpmsTarget), entity.Id, seed.Municipality.Id, seed.User.Id, "Seed protected IPMS members.",
            new { entity.PublicId, InternalReference = (string?)null },
            new { entity.PublicId, entity.InternalReference }));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var denied = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IPMS_KPI.InternalReference.READ", "IPMS_KPI.BudgetSourceAmounts.READ",
            "IPMS_KPI.InternalReference.UPDATE", "IPMS_KPI.BudgetSourceAmounts.UPDATE"
        };
        var controller = IpmsController(context, seed.User, seed.Municipality.Id, denied);
        var currentVersion = Convert.ToBase64String((await context.IpmsTargets.AsNoTracking()
            .SingleAsync(item => item.PublicId == created.PublicId)).RowVersion);
        var request = IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications) with
        {
            RowVersion = currentVersion,
            InternalReference = "HOSTILE-REPLACEMENT",
            InternalReferenceSpecified = false,
            BudgetSourceAmountsSpecified = false,
            BudgetSources = [new SaveKpiBudgetSourceRequest(source.PublicId, 999m)]
        };

        var response = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.UpdateTarget(created.PublicId, request)).Result).Value).Data!;

        Assert.Null(response.InternalReference);
        Assert.Null(Assert.Single(response.BudgetSources).Amount);
        context.ChangeTracker.Clear();
        var stored = await context.IpmsTargets.Include(item => item.GovernedBudgetSources).SingleAsync(item => item.PublicId == created.PublicId);
        Assert.Equal("EMPLOYEE-SECRET", stored.InternalReference);
        Assert.Equal(75m, Assert.Single(stored.GovernedBudgetSources, item => item.IsActive).Amount);

        var forbidden = await controller.UpdateTarget(created.PublicId, request with { BudgetSourceAmountsSpecified = true });
        Assert.IsType<ForbidResult>(forbidden.Result);
    }

    [Fact]
    public async Task Create_fails_closed_when_governed_municipality_year_is_not_configured()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "NO-YEAR", Name = "No Year" };
        var user = IdpTestFixture.CreateUser("no-year-user");
        var period = new Period { Code = "FY", Name = "Legacy year", FiscalYear = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30), IsActive = true };
        context.AddRange(municipality, user, period);
        await context.SaveChangesAsync();

        var result = await Controller(context, user, municipality.Id).CreateTarget(Request(Guid.NewGuid()));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("municipality financial year", Assert.IsType<ApiResponse<OpmsTargetResponse>>(badRequest.Value).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.OpmsTargets.ToArrayAsync());
    }

    [Fact]
    public async Task Create_rejects_free_text_only_strategic_classification()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);

        var result = await Controller(context, seed.User, seed.Municipality.Id)
            .CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("governed strategic classifications", Assert.IsType<ApiResponse<OpmsTargetResponse>>(badRequest.Value).Message);
        Assert.Empty(await context.OpmsTargets.ToArrayAsync());
    }

    [Fact]
    public async Task Public_template_and_related_target_ids_resolve_to_private_keys_without_leaking_into_writes()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var opmsTemplate = new OpmsTargetTemplate { TemplateCode = "OPMS-T", TemplateName = "OPMS template", Version = 2 };
        var ipmsTemplate = new IpmsTargetTemplate { TemplateCode = "IPMS-T", TemplateName = "IPMS template", Version = 3 };
        context.AddRange(opmsTemplate, ipmsTemplate);
        await context.SaveChangesAsync();

        var opmsRequest = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            SourceTemplatePublicId = opmsTemplate.PublicId,
            SourceTemplateVersion = opmsTemplate.Version
        };
        var opmsResult = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget(opmsRequest);
        var opmsResponse = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(opmsResult.Result).Value).Data!;
        Assert.Equal(opmsTemplate.PublicId, opmsResponse.SourceTemplatePublicId);

        var ipmsRequest = IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications) with
        {
            SourceTemplatePublicId = ipmsTemplate.PublicId,
            SourceTemplateVersion = ipmsTemplate.Version,
            RelatedOpmsTargetPublicId = opmsResponse.PublicId
        };
        var ipmsResult = await IpmsController(context, seed.User, seed.Municipality.Id).CreateTarget(ipmsRequest);
        var ipmsResponse = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>(ipmsResult.Result).Value).Data!;
        Assert.Equal(ipmsTemplate.PublicId, ipmsResponse.SourceTemplatePublicId);
        Assert.Equal(opmsResponse.PublicId, ipmsResponse.RelatedOpmsTargetPublicId);

        context.ChangeTracker.Clear();
        Assert.Equal(opmsTemplate.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), (await context.OpmsTargets.SingleAsync()).SourceTemplateId);
        var storedIpms = await context.IpmsTargets.SingleAsync();
        Assert.Equal(ipmsTemplate.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), storedIpms.SourceTemplateId);
        Assert.Equal((await context.OpmsTargets.SingleAsync()).Id, storedIpms.RelatedOpmsTargetId);
        var link = await context.IpmsTargetOpmsLinks.SingleAsync();
        Assert.Equal(storedIpms.Id, link.IpmsTargetId);
        Assert.Equal(storedIpms.RelatedOpmsTargetId, link.OpmsTargetId);
        Assert.Equal(seed.Municipality.Id, link.MunicipalityId);
        Assert.Equal(seed.User.Id, link.LinkedByUserId);
        Assert.True(link.IsActive);
    }

    [Fact]
    public async Task Ipms_related_target_changes_require_reason_and_append_preserved_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var opms = Controller(context, seed.User, seed.Municipality.Id);
        var first = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.CreateTarget(
            Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications))).Result).Value).Data!;
        var secondRequest = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            IndicatorNumber = "OPMS-2",
            TargetName = "Replacement related target"
        };
        var second = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.CreateTarget(secondRequest)).Result).Value).Data!;
        var ipmsController = IpmsController(context, seed.User, seed.Municipality.Id);
        var createRequest = IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications) with
        {
            RelatedOpmsTargetPublicId = first.PublicId
        };
        var created = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipmsController.CreateTarget(createRequest)).Result).Value).Data!;
        Assert.Single(await context.IpmsTargetOpmsLinks.AsNoTracking().ToArrayAsync());

        var replacement = createRequest with
        {
            RowVersion = created.RowVersion,
            RelatedOpmsTargetPublicId = second.PublicId
        };
        var missingReason = await ipmsController.UpdateTarget(created.PublicId, replacement);
        Assert.Contains("change reason", Assert.IsType<ApiResponse<IpmsTargetResponse>>(
            Assert.IsType<BadRequestObjectResult>(missingReason.Result).Value).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.IpmsTargetOpmsLinks.AsNoTracking().ToArrayAsync());

        var changed = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipmsController.UpdateTarget(
            created.PublicId, replacement with { RelatedOpmsTargetChangeReason = "Approved performance alignment" })).Result).Value).Data!;
        Assert.NotEqual(created.RowVersion, changed.RowVersion);
        var replacementHistory = await context.IpmsTargetOpmsLinks.AsNoTracking().Include(item => item.OpmsTarget).OrderBy(item => item.LinkedAt).ToArrayAsync();
        Assert.Equal(2, replacementHistory.Length);
        Assert.Contains(replacementHistory, item => item.OpmsTarget.PublicId == first.PublicId && !item.IsActive
            && item.UnlinkReason == "Approved performance alignment" && item.UnlinkedByUserId == seed.User.Id);
        Assert.Contains(replacementHistory, item => item.OpmsTarget.PublicId == second.PublicId && item.IsActive
            && item.LinkedByUserId == seed.User.Id);

        var remove = createRequest with { RowVersion = changed.RowVersion, RelatedOpmsTargetPublicId = null };
        var missingRemovalReason = await ipmsController.UpdateTarget(created.PublicId, remove);
        Assert.IsType<BadRequestObjectResult>(missingRemovalReason.Result);
        var removed = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipmsController.UpdateTarget(
            created.PublicId, remove with { RelatedOpmsTargetChangeReason = "Approved responsibility separation" })).Result).Value).Data!;
        Assert.Null(removed.RelatedOpmsTargetPublicId);
        Assert.Equal(2, await context.IpmsTargetOpmsLinks.CountAsync());
        Assert.Empty(await context.IpmsTargetOpmsLinks.Where(item => item.IsActive).ToArrayAsync());

        var relinked = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipmsController.UpdateTarget(
            created.PublicId, createRequest with
            {
                RowVersion = removed.RowVersion,
                RelatedOpmsTargetChangeReason = "Approved restoration"
            })).Result).Value).Data!;
        Assert.Equal(first.PublicId, relinked.RelatedOpmsTargetPublicId);
        var finalHistory = await context.IpmsTargetOpmsLinks.AsNoTracking().Include(item => item.OpmsTarget).ToArrayAsync();
        Assert.Equal(3, finalHistory.Length);
        Assert.Single(finalHistory, item => item.IsActive && item.OpmsTarget.PublicId == first.PublicId);
    }

    [Fact]
    public async Task Ipms_related_target_public_id_cannot_cross_the_selected_municipality()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var foreignMunicipality = new Municipality { Code = "FOREIGN", Name = "Foreign Municipality" };
        context.Municipalities.Add(foreignMunicipality);
        await context.SaveChangesAsync();
        var foreignTarget = new OpmsTarget
        {
            MunicipalityId = foreignMunicipality.Id,
            IndicatorNumber = "FOREIGN-1",
            TargetName = "Foreign target",
            KpiDescription = "Foreign target",
            PerformanceObjective = "Foreign objective"
        };
        context.OpmsTargets.Add(foreignTarget);
        await context.SaveChangesAsync();

        var result = await IpmsController(context, seed.User, seed.Municipality.Id)
            .CreateTarget(IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications) with { RelatedOpmsTargetPublicId = foreignTarget.PublicId });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("outside the selected municipality", Assert.IsType<ApiResponse<IpmsTargetResponse>>(badRequest.Value).Message);
        Assert.Empty(await context.IpmsTargets.ToArrayAsync());
    }

    [Fact]
    public async Task Opms_vote_numbers_must_match_the_target_municipality_financial_year()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var targetYear = await context.MunicipalityFinancialYears.SingleAsync();
        var department = new Department { MunicipalityId = seed.Municipality.Id, Code = "FIN", Name = "Finance" };
        var otherFinancialYear = new FinancialYear { Code = "2027/28", Name = "2027/28", StartDate = new DateTime(2027, 7, 1), EndDate = new DateTime(2028, 6, 30), IsActive = true };
        context.AddRange(department, otherFinancialYear);
        await context.SaveChangesAsync();
        var otherMunicipalityYear = new MunicipalityFinancialYear { MunicipalityId = seed.Municipality.Id, FinancialYearId = otherFinancialYear.Id, IsActive = true, EffectiveFrom = otherFinancialYear.StartDate };
        context.MunicipalityFinancialYears.Add(otherMunicipalityYear);
        await context.SaveChangesAsync();
        var foreignMunicipality = new Municipality { Code = "OTHER", Name = "Other Municipality" };
        context.Municipalities.Add(foreignMunicipality);
        await context.SaveChangesAsync();
        var currentWard = new Ward { MunicipalityId = seed.Municipality.Id, Code = "W01", Name = "Ward 1", IsActive = true };
        var foreignWard = new Ward { MunicipalityId = foreignMunicipality.Id, Code = "W99", Name = "Ward 99", IsActive = true };
        var currentVote = new VoteNumber { MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYearId = targetYear.Id, DepartmentId = department.Id, Code = "CUR", Number = "001", Name = "Current vote", IsActive = true };
        var futureVote = new VoteNumber { MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYearId = otherMunicipalityYear.Id, DepartmentId = department.Id, Code = "FUT", Number = "002", Name = "Future vote", IsActive = true };
        context.AddRange(currentWard, foreignWard, currentVote, futureVote);
        await context.SaveChangesAsync();

        var accepted = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with { WardPublicIds = [currentWard.PublicId], VoteNumberPublicIds = [currentVote.PublicId] });
        var acceptedResponse = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(accepted.Result).Value).Data!;
        Assert.Equal([currentWard.PublicId], acceptedResponse.WardPublicIds);
        Assert.Equal([currentVote.PublicId], acceptedResponse.VoteNumberPublicIds);
        Assert.Equal(currentVote.Id, (await context.OpmsTargetVoteNumbers.SingleAsync()).VoteNumberId);

        var rejected = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget((Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with { IndicatorNumber = "OPMS-2", VoteNumberPublicIds = [futureVote.PublicId] }));
        var badRequest = Assert.IsType<BadRequestObjectResult>(rejected.Result);
        Assert.Contains("selected municipality financial year", Assert.IsType<ApiResponse<OpmsTargetResponse>>(badRequest.Value).Message);

        var foreignWardResult = await Controller(context, seed.User, seed.Municipality.Id).CreateTarget(Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with { IndicatorNumber = "OPMS-3", WardPublicIds = [foreignWard.PublicId] });
        var foreignWardBadRequest = Assert.IsType<BadRequestObjectResult>(foreignWardResult.Result);
        Assert.Contains("selected municipality", Assert.IsType<ApiResponse<OpmsTargetResponse>>(foreignWardBadRequest.Value).Message);
    }

    [Fact]
    public async Task Relationship_only_updates_require_a_reason_append_history_and_advance_target_concurrency()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var secondAssignee = IdpTestFixture.CreateUser($"relationship-assignee-{Guid.NewGuid():N}");
        secondAssignee.MunicipalityId = seed.Municipality.Id;
        var department = new Department { MunicipalityId = seed.Municipality.Id, Code = "REL", Name = "Relationship Governance" };
        var firstWard = new Ward { MunicipalityId = seed.Municipality.Id, Code = "W01", Name = "Ward 1", IsActive = true };
        var secondWard = new Ward { MunicipalityId = seed.Municipality.Id, Code = "W02", Name = "Ward 2", IsActive = true };
        context.AddRange(secondAssignee, department, firstWard, secondWard);
        await context.SaveChangesAsync();
        var firstVote = new VoteNumber { MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYearId = seed.MunicipalityYear.Id, DepartmentId = department.Id, Code = "V01", Number = "001", Name = "First vote", IsActive = true };
        var secondVote = new VoteNumber { MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYearId = seed.MunicipalityYear.Id, DepartmentId = department.Id, Code = "V02", Number = "002", Name = "Second vote", IsActive = true };
        context.AddRange(firstVote, secondVote);
        await context.SaveChangesAsync();

        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var createRequest = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            WardPublicIds = [firstWard.PublicId],
            AdditionalAssigneePublicIds = [seed.User.PublicId],
            VoteNumberPublicIds = [firstVote.PublicId]
        };
        var created = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await controller.CreateTarget(createRequest)).Result).Value).Data!;

        var replacement = createRequest with
        {
            RowVersion = created.RowVersion,
            WardPublicIds = [secondWard.PublicId],
            AdditionalAssigneePublicIds = [secondAssignee.PublicId],
            VoteNumberPublicIds = [secondVote.PublicId]
        };
        var missingReason = await controller.UpdateTarget(created.PublicId, replacement);
        var badRequest = Assert.IsType<BadRequestObjectResult>(missingReason.Result);
        Assert.Contains("relationship change reason", Assert.IsType<ApiResponse<OpmsTargetResponse>>(badRequest.Value).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.OpmsTargetWards.Where(row => row.IsActive).ToArrayAsync());

        var changed = await controller.UpdateTarget(created.PublicId, replacement with { RelationshipChangeReason = "Approved service-delivery responsibility change" });
        var changedResponse = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(changed.Result).Value).Data!;
        Assert.NotEqual(created.RowVersion, changedResponse.RowVersion);
        Assert.Equal([secondWard.PublicId], changedResponse.WardPublicIds);
        Assert.Equal([secondAssignee.PublicId], changedResponse.AdditionalAssigneePublicIds);
        Assert.Equal([secondVote.PublicId], changedResponse.VoteNumberPublicIds);

        context.ChangeTracker.Clear();
        var wardHistory = await context.OpmsTargetWards.OrderBy(row => row.LinkedAt).ToArrayAsync();
        var assigneeHistory = await context.OpmsTargetAdditionalAssignees.OrderBy(row => row.LinkedAt).ToArrayAsync();
        var voteHistory = await context.OpmsTargetVoteNumbers.OrderBy(row => row.LinkedAt).ToArrayAsync();
        Assert.Equal(2, wardHistory.Length);
        Assert.Equal(2, assigneeHistory.Length);
        Assert.Equal(2, voteHistory.Length);
        Assert.All(new IOpmsTargetRelationship[] { wardHistory[0], assigneeHistory[0], voteHistory[0] }, row =>
        {
            Assert.False(row.IsActive);
            Assert.Equal(seed.User.Id, row.UnlinkedByUserId);
            Assert.Equal("Approved service-delivery responsibility change", row.UnlinkReason);
            Assert.NotNull(row.UnlinkedAt);
        });
        Assert.All(new IOpmsTargetRelationship[] { wardHistory[1], assigneeHistory[1], voteHistory[1] }, row =>
        {
            Assert.True(row.IsActive);
            Assert.Equal(seed.User.Id, row.LinkedByUserId);
        });

        var relinked = await controller.UpdateTarget(created.PublicId, createRequest with
        {
            RowVersion = changedResponse.RowVersion,
            RelationshipChangeReason = "Approved restoration of the original responsibility"
        });
        var relinkedResponse = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>(relinked.Result).Value).Data!;
        Assert.Equal([firstWard.PublicId], relinkedResponse.WardPublicIds);
        Assert.Equal(3, await context.OpmsTargetWards.CountAsync());
        Assert.Equal(3, await context.OpmsTargetAdditionalAssignees.CountAsync());
        Assert.Equal(3, await context.OpmsTargetVoteNumbers.CountAsync());
    }

    [Fact]
    public async Task Opms_and_ipms_budget_changes_require_reason_and_append_preserved_allocations()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var originalType = new GovernedBudgetType { MunicipalityId = seed.Municipality.Id, Code = "CAPEX", Name = "Capital" };
        var replacementType = new GovernedBudgetType { MunicipalityId = seed.Municipality.Id, Code = "OPEX", Name = "Operating" };
        var originalSource = new GovernedBudgetSource { MunicipalityId = seed.Municipality.Id, Code = "MIG", Name = "Infrastructure Grant" };
        var replacementSource = new GovernedBudgetSource { MunicipalityId = seed.Municipality.Id, Code = "OWN", Name = "Own Revenue" };
        context.AddRange(originalType, replacementType, originalSource, replacementSource);
        await context.SaveChangesAsync();

        var opms = Controller(context, seed.User, seed.Municipality.Id);
        var ipms = IpmsController(context, seed.User, seed.Municipality.Id);
        var opmsCreate = Request(seed.MunicipalityYear.PublicId, seed.SdbipLayer.PublicId, seed.Classifications) with
        {
            BudgetTypePublicId = originalType.PublicId,
            BudgetSources = [new(originalSource.PublicId, 100m)]
        };
        var ipmsCreate = IpmsRequest(seed.MunicipalityYear.PublicId, seed.Classifications) with
        {
            BudgetTypePublicId = originalType.PublicId,
            BudgetSources = [new(originalSource.PublicId, 100m)]
        };
        var opmsCreated = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.CreateTarget(opmsCreate)).Result).Value).Data!;
        var ipmsCreated = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipms.CreateTarget(ipmsCreate)).Result).Value).Data!;

        var opmsReplacement = opmsCreate with
        {
            RowVersion = opmsCreated.RowVersion,
            BudgetTypePublicId = replacementType.PublicId,
            BudgetSources = [new(replacementSource.PublicId, 250m)]
        };
        var ipmsReplacement = ipmsCreate with
        {
            RowVersion = ipmsCreated.RowVersion,
            BudgetTypePublicId = replacementType.PublicId,
            BudgetSources = [new(replacementSource.PublicId, 250m)]
        };
        Assert.Contains("budget-classification change reason",
            Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<BadRequestObjectResult>((await opms.UpdateTarget(opmsCreated.PublicId, opmsReplacement)).Result).Value).Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("budget-classification change reason",
            Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<BadRequestObjectResult>((await ipms.UpdateTarget(ipmsCreated.PublicId, ipmsReplacement)).Result).Value).Message,
            StringComparison.OrdinalIgnoreCase);

        const string reason = "Approved annual funding reclassification";
        var opmsChanged = Assert.IsType<ApiResponse<OpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await opms.UpdateTarget(opmsCreated.PublicId,
            opmsReplacement with { BudgetClassificationChangeReason = reason })).Result).Value).Data!;
        var ipmsChanged = Assert.IsType<ApiResponse<IpmsTargetResponse>>(Assert.IsType<OkObjectResult>((await ipms.UpdateTarget(ipmsCreated.PublicId,
            ipmsReplacement with { BudgetClassificationChangeReason = reason })).Result).Value).Data!;
        Assert.NotEqual(opmsCreated.RowVersion, opmsChanged.RowVersion);
        Assert.NotEqual(ipmsCreated.RowVersion, ipmsChanged.RowVersion);

        context.ChangeTracker.Clear();
        var opmsHistory = await context.OpmsKpiBudgetSources.OrderBy(item => item.CreatedAt).ToArrayAsync();
        var ipmsHistory = await context.IpmsKpiBudgetSources.OrderBy(item => item.CreatedAt).ToArrayAsync();
        Assert.Equal(2, opmsHistory.Length);
        Assert.Equal(2, ipmsHistory.Length);
        Assert.All(new KpiBudgetSourceBase[] { opmsHistory[0], ipmsHistory[0] }, item =>
        {
            Assert.False(item.IsActive);
            Assert.Equal(seed.User.Id, item.DeactivatedByUserId);
            Assert.Equal(reason, item.DeactivationReason);
            Assert.NotNull(item.DeactivatedAt);
        });
        Assert.All(new KpiBudgetSourceBase[] { opmsHistory[1], ipmsHistory[1] }, item =>
        {
            Assert.True(item.IsActive);
            Assert.Equal(seed.User.Id, item.AllocatedByUserId);
            Assert.Equal(250m, item.Amount);
        });
        Assert.NotNull((await context.IpmsTargets.AsNoTracking().SingleAsync(item => item.PublicId == ipmsCreated.PublicId)).UpdatedAt);
    }

    private static OpmsTargetsController Controller(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context, ApplicationUser user, long municipalityId,
        ISet<string>? deniedPermissions = null, bool persistGovernance = true)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => deniedPermissions?.Contains(code) == true
                ? new AccessDecisionResult(false, "Denied", [], [code], [])
                : new AccessDecisionResult(true, "Allowed", [code], [], []));
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.UserId).Returns(user.Id);
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(service => service.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        IWorkflowGovernanceService governance = persistGovernance
            ? new WorkflowGovernanceService(context, tenantContext: tenant.Object)
            : workflow.Object;
        return new OpmsTargetsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, governance, tenant.Object, new PerformanceUnitEngine())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static AuditTrail TargetEditAudit(string entityName, string entityId, long municipalityId, string userId, string reason,
        object oldValue, object newValue) => new()
    {
        MunicipalityId = municipalityId,
        EntityName = entityName,
        EntityId = entityId,
        Action = "Edit",
        OldValue = JsonSerializer.Serialize(oldValue),
        NewValue = JsonSerializer.Serialize(newValue),
        ChangedBy = userId,
        Reason = reason
    };

    private static void AssertGuidRoute<TController>(string methodName, string expectedTemplate)
    {
        var methods = typeof(TController).GetMethods().Where(method => method.Name == methodName).ToArray();
        Assert.NotEmpty(methods);
        var template = methods
            .SelectMany(method => method.GetCustomAttributes(inherit: false))
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .Single(value => value == expectedTemplate);
        Assert.Equal(expectedTemplate, template);
    }

    private static IpmsTargetsController IpmsController(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context, ApplicationUser user, long municipalityId,
        ISet<string>? deniedPermissions = null, bool persistGovernance = true)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => deniedPermissions?.Contains(code) == true
                ? new AccessDecisionResult(false, "Denied", [], [code], [])
                : new AccessDecisionResult(true, "Allowed", [code], [], []));
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        tenant.SetupGet(item => item.UserId).Returns(user.Id);
        var workflow = new Mock<IWorkflowGovernanceService>();
        workflow.Setup(service => service.WriteAuditTrailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        IWorkflowGovernanceService governance = persistGovernance
            ? new WorkflowGovernanceService(context, tenantContext: tenant.Object)
            : workflow.Object;
        return new IpmsTargetsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, governance, tenant.Object, new PerformanceUnitEngine())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static SaveOpmsTargetRequest Request(Guid municipalityFinancialYearPublicId, Guid? sdbipLayerPublicId = null, ClassificationIds? classifications = null) => new()
        {
            MunicipalityFinancialYearPublicId = municipalityFinancialYearPublicId,
            SdbipLayerPublicId = sdbipLayerPublicId,
            IndicatorNumber = "OPMS-1",
            NationalKpa = "National KPA",
            MunicipalKpa = "Municipal KPA",
            PerformanceObjective = "Objective",
            TargetName = "Normalized target",
            KpiDescription = "Description",
            Weight = 10m,
            KpiType = "Quantitative",
            IndicatorType = "Output",
            PeriodTargets = PeriodTargets(),
            NationalKpaPublicId = classifications?.NationalKpa,
            MunicipalKpaPublicId = classifications?.MunicipalKpa,
            BackToBasicsPillarPublicId = classifications?.BackToBasicsPillar,
            StrategicGoalPublicId = classifications?.StrategicGoal,
            StrategicInterventionPublicId = classifications?.StrategicIntervention,
            StrategicObjectivePublicId = classifications?.StrategicObjective,
            PerformanceObjectivePublicId = classifications?.PerformanceObjective,
            KpiTypePublicId = classifications?.KpiType,
            IndicatorTypePublicId = classifications?.IndicatorType,
            KpiUnitOfMeasurePublicId = classifications?.KpiUnitOfMeasure
        };

    private static SaveIpmsTargetRequest IpmsRequest(Guid municipalityFinancialYearPublicId, ClassificationIds classifications) => new()
        {
            MunicipalityFinancialYearPublicId = municipalityFinancialYearPublicId,
            IndicatorNumber = "IPMS-1",
            NationalKpa = "National KPA",
            MunicipalKpa = "Municipal KPA",
            PerformanceObjective = "Objective",
            TargetName = "Normalized individual target",
            KpiDescription = "Description",
            Weight = 10m,
            KpiType = "Quantitative",
            IndicatorType = "Output",
            PeriodTargets = PeriodTargets(),
            NationalKpaPublicId = classifications.NationalKpa,
            MunicipalKpaPublicId = classifications.MunicipalKpa,
            BackToBasicsPillarPublicId = classifications.BackToBasicsPillar,
            StrategicGoalPublicId = classifications.StrategicGoal,
            StrategicInterventionPublicId = classifications.StrategicIntervention,
            StrategicObjectivePublicId = classifications.StrategicObjective,
            PerformanceObjectivePublicId = classifications.PerformanceObjective,
            KpiTypePublicId = classifications.KpiType,
            IndicatorTypePublicId = classifications.IndicatorType,
            KpiUnitOfMeasurePublicId = classifications.KpiUnitOfMeasure
        };

    private static SaveTargetPeriodValueRequest[] PeriodTargets(string annual = "100") =>
    [
        new(ReportingPeriodType.Quarter1, PerformanceUnitKind.PercentageBased, PerformanceDirection.HigherIsBetter, "20", 10m, "Q1 target"),
        new(ReportingPeriodType.Annual, PerformanceUnitKind.PercentageBased, PerformanceDirection.HigherIsBetter, annual, null, "Annual target")
    ];

    private sealed record ClassificationIds(Guid NationalKpa, Guid MunicipalKpa, Guid BackToBasicsPillar, Guid StrategicGoal, Guid StrategicIntervention, Guid StrategicObjective, Guid PerformanceObjective, Guid KpiType, Guid IndicatorType, Guid KpiUnitOfMeasure);

    private static async Task<(Municipality Municipality, ApplicationUser User, Period LegacyPeriod, MunicipalityFinancialYear MunicipalityYear, SdbipLayer SdbipLayer, ClassificationIds Classifications)> SeedAsync(FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "NORM", Name = "Normalized Municipality" };
        var user = IdpTestFixture.CreateUser("normalized-user");
        user.Municipality = municipality;
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30), IsActive = true };
        var legacyPeriod = new Period { Code = "FY", Name = "2026/27", FiscalYear = financialYear.Code, StartDate = financialYear.StartDate, EndDate = financialYear.EndDate, IsActive = true };
        context.AddRange(municipality, user, financialYear, legacyPeriod);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, IsCurrent = true, IsActive = true, EffectiveFrom = financialYear.StartDate };
        context.MunicipalityFinancialYears.Add(municipalityYear);
        await context.SaveChangesAsync();
        var sdbipLayer = new SdbipLayer
        {
            MunicipalityId = municipality.Id,
            MunicipalityFinancialYearId = municipalityYear.Id,
            Code = "TOP",
            Name = "Top Layer SDBIP",
            DisplayOrder = 1,
            IsActive = true
        };
        context.SdbipLayers.Add(sdbipLayer);
        context.ReportingPeriods.AddRange(
            new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 9, 30), IsActive = true },
            new ReportingPeriod { MunicipalityFinancialYearId = municipalityYear.Id, Code = "ANN", Name = "Annual", PeriodType = ReportingPeriodType.Annual, Sequence = 6, StartDate = financialYear.StartDate, EndDate = financialYear.EndDate, IsActive = true });
        var nationalKpa = new NationalKpa { Code = "NKPA", Name = "National KPA" };
        var municipalKpa = new MunicipalKpa { MunicipalityId = municipality.Id, Code = "MKPA", Name = "Municipal KPA" };
        var pillar = new BackToBasicsPillar { Code = "B2B", Name = "Back-to-Basics" };
        var goal = new MunicipalStrategicGoal { MunicipalityId = municipality.Id, Code = "GOAL", Name = "Strategic Goal" };
        var intervention = new StrategicIntervention { MunicipalityId = municipality.Id, Code = "INT", Name = "Strategic Intervention" };
        var objective = new MunicipalStrategicObjective { MunicipalityId = municipality.Id, Code = "OBJ", Name = "Strategic Objective" };
        var performanceObjective = new PerformanceObjective { MunicipalityId = municipality.Id, Code = "PERF", Name = "Objective" };
        var kpiType = new GovernedKpiType { MunicipalityId = municipality.Id, Code = "QUANT", Name = "Quantitative" };
        var indicatorType = new GovernedIndicatorType { MunicipalityId = municipality.Id, Code = "OUTPUT", Name = "Output" };
        var kpiUnitOfMeasure = new GovernedKpiUnitOfMeasure { MunicipalityId = municipality.Id, Code = "COUNT", Name = "Count", Symbol = "#" };
        context.AddRange(nationalKpa, municipalKpa, pillar, goal, intervention, objective, performanceObjective, kpiType, indicatorType, kpiUnitOfMeasure);
        await context.SaveChangesAsync();
        return (municipality, user, legacyPeriod, municipalityYear, sdbipLayer,
            new(nationalKpa.PublicId, municipalKpa.PublicId, pillar.PublicId, goal.PublicId, intervention.PublicId, objective.PublicId, performanceObjective.PublicId, kpiType.PublicId, indicatorType.PublicId, kpiUnitOfMeasure.PublicId));
    }
}

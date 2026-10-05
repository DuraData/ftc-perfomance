using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController, Route("api/v1/opms/imports"), Authorize]
public sealed class OpmsImportsController(
    ApplicationDbContext context, UserManager<ApplicationUser> userManager, ITenantContext tenantContext,
    IPerformanceUnitEngine unitEngine, IWorkflowGovernanceService workflow, IAccessControlService accessControl) : ControllerBase
{
    private const int MaximumRows = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    [HttpGet("template.csv"), Authorize(Policy = "Permission:OPMS_KPI.IMPORT")]
    public IActionResult Template() => CsvFile(OpmsImportCsv.WideHeader + "\r\n", "opms-sdbip-import-template.csv");

    [HttpGet("layers/{layerPublicId:guid}/export.csv"), Authorize(Policy = "Permission:OPMS_KPI.EXPORT")]
    public async Task<IActionResult> Export(Guid layerPublicId)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<object>(false, null, "User not found."));
        if (tenantContext.MunicipalityId is not > 0) return BadRequest(new ApiResponse<object>(false, null, "A municipality context is required."));
        var municipalityId = tenantContext.MunicipalityId.Value;
        var layer = await context.SdbipLayers.AsNoTracking().SingleOrDefaultAsync(x => x.MunicipalityId == municipalityId && x.PublicId == layerPublicId && x.IsActive);
        if (layer == null) return NotFound(new ApiResponse<object>(false, null, "Active SDBIP layer not found."));
        var scope = await accessControl.GetQueryScopeAsync(user, "OPMS_KPI.EXPORT");
        if (!scope.PermissionGranted) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>(false, null, "SDBIP export permission is denied."));
        var query = context.OpmsTargets.AsNoTracking().Include(x => x.Department).Include(x => x.Unit)
            .Include(x => x.NationalKpaReference).Include(x => x.MunicipalKpaReference)
            .Include(x => x.BackToBasicsPillarReference).Include(x => x.StrategicGoalMaster)
            .Include(x => x.StrategicInterventionReference).Include(x => x.StrategicObjectiveMaster)
            .Include(x => x.PerformanceObjectiveReference)
            .Where(x => x.SdbipLayerId == layer.Id && !x.IsWithdrawn);
        if (!scope.Unrestricted)
            query = query.Where(x => (x.DepartmentId.HasValue && scope.DepartmentIds.Contains(x.DepartmentId.Value)) ||
                (x.UnitId.HasValue && scope.UnitIds.Contains(x.UnitId.Value)) ||
                (x.AssignedUserId != null && scope.OwnerUserIds.Contains(x.AssignedUserId)) || scope.TargetIds.Contains(x.Id) || scope.KpiIds.Contains(x.Id));
        var targets = await query.OrderBy(x => x.RevisedOrderNumber).ThenBy(x => x.PublicId).Take(100001).ToArrayAsync();
        if (targets.Length > 100000) return StatusCode(StatusCodes.Status413PayloadTooLarge, new ApiResponse<object>(false, null, "Export exceeds 100,000 KPIs."));
        var ids = targets.Select(x => x.Id).ToArray();
        var periods = await context.PerformancePeriodTargets.AsNoTracking().Include(x => x.ReportingPeriod)
            .Where(x => x.OpmsTargetId != null && ids.Contains(x.OpmsTargetId) && x.IsActive).ToArrayAsync();
        var csv = new StringBuilder(OpmsImportCsv.WideHeader).Append("\r\n");
        foreach (var target in targets)
        {
            var effectiveIndicator = target.IsIndicatorNumberRevised && target.RevisedIndicatorNumber != null ? target.RevisedIndicatorNumber : target.IndicatorNumber;
            var effectiveName = target.IsTargetNameRevised && target.RevisedTargetName != null ? target.RevisedTargetName : target.TargetName;
            var effectiveWording = target.IsKpiDescriptionRevised && target.RevisedKpiDescription != null ? target.RevisedKpiDescription : target.KpiDescription;
            var values = new List<string?> { effectiveIndicator, effectiveIndicator, target.RevisedOrderNumber.ToString(CultureInfo.InvariantCulture), effectiveName, effectiveWording,
                target.Department?.Code, target.Unit?.Code, target.NationalKpaReference?.Code, target.MunicipalKpaReference?.Code ?? target.MunicipalKpaReference?.Name,
                target.BackToBasicsPillarReference?.Code, target.StrategicGoalMaster?.Code ?? target.StrategicGoalMaster?.Name,
                target.StrategicInterventionReference?.Code ?? target.StrategicInterventionReference?.Name,
                target.StrategicObjectiveMaster?.Code ?? target.StrategicObjectiveMaster?.Name,
                target.PerformanceObjectiveReference?.Code ?? target.PerformanceObjectiveReference?.Name,
                target.Baseline.ToString(CultureInfo.InvariantCulture), target.Weight.ToString(CultureInfo.InvariantCulture), target.KpiType, target.IndicatorType };
            foreach (var periodType in new[] { ReportingPeriodType.Quarter1, ReportingPeriodType.Quarter2, ReportingPeriodType.MidTerm, ReportingPeriodType.Quarter3, ReportingPeriodType.Quarter4, ReportingPeriodType.Annual })
            {
                var period = periods.SingleOrDefault(x => x.OpmsTargetId == target.Id && x.ReportingPeriod.PeriodType == periodType);
                var revisable = periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual;
                values.Add(period == null ? null : revisable && period.IsTargetRevised ? period.RevisedTargetValue : period.TargetValue);
                values.Add(period == null ? null : OpmsImportCsv.ToTemplateUnit(revisable ? period.RevisedUnitKind ?? period.UnitKind : period.UnitKind));
                values.Add(period == null ? null : OpmsImportCsv.ToTemplateDirection(period.Direction));
                values.Add(period == null ? null : (revisable && period.IsBudgetRevised ? period.RevisedBudgetValue : period.BudgetValue)?.ToString(CultureInfo.InvariantCulture));
                values.Add(period?.Description);
            }
            csv.AppendJoin(',', values.Select(PerformanceReportCsv.Encode)).Append("\r\n");
        }
        workflow.QueueAuditTrail("OpmsImportBatch", layer.PublicId.ToString(), "ExportWideTemplate", null, new { rowCount = targets.Length }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await context.SaveChangesAsync();
        return CsvFile(csv.ToString(), $"sdbip-{SafeFileName(layer.Code)}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpPost("layers/{layerPublicId:guid}/stage"), Authorize(Policy = "Permission:OPMS_KPI.IMPORT")]
    public async Task<ActionResult<ApiResponse<OpmsImportBatchResponse>>> Stage(Guid layerPublicId, StageOpmsImportRequest request)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<OpmsImportBatchResponse>(false, null, "User not found."));
        if (request.ClientRequestId == Guid.Empty) return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "ClientRequestId is required."));
        if (request.Rows.Length is < 1 or > MaximumRows) return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, $"An import must contain between 1 and {MaximumRows} rows."));
        var fileName = Path.GetFileName(request.SourceFileName ?? "").Trim();
        if (fileName.Length is 0 or > 260) return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "A valid source file name is required."));
        if (tenantContext.MunicipalityId is not > 0) return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "A municipality context is required."));
        var municipalityId = tenantContext.MunicipalityId.Value;
        var layer = await context.SdbipLayers.Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear)
            .SingleOrDefaultAsync(x => x.MunicipalityId == municipalityId && x.PublicId == layerPublicId && x.IsActive);
        if (layer == null) return NotFound(new ApiResponse<OpmsImportBatchResponse>(false, null, "Active SDBIP layer not found."));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.Rows, JsonOptions)))).ToLowerInvariant();
        var existingBatch = await context.OpmsImportBatches.Include(x => x.SdbipLayer).Include(x => x.Rows)
            .SingleOrDefaultAsync(x => x.ClientRequestId == request.ClientRequestId);
        if (existingBatch != null)
            return existingBatch.SourceSha256 != hash
                ? Conflict(new ApiResponse<OpmsImportBatchResponse>(false, null, "ClientRequestId was already used for different content."))
                : Ok(new ApiResponse<OpmsImportBatchResponse>(true, ToResponse(existingBatch), "Existing staged result returned."));

        var candidates = await BuildCandidates(layer, request.Rows);
        var batch = new OpmsImportBatch
        {
            ClientRequestId = request.ClientRequestId, MunicipalityId = layer.MunicipalityId, SdbipLayerId = layer.Id,
            SourceFileName = fileName, SourceSha256 = hash, Status = OpmsImportBatchStatus.Staged,
            TotalRows = candidates.Count, NewRows = candidates.Count(x => x.Status == OpmsImportRowStatus.New),
            UnchangedRows = candidates.Count(x => x.Status == OpmsImportRowStatus.Unchanged),
            ChangedRows = candidates.Count(x => x.Status == OpmsImportRowStatus.Changed),
            InvalidRows = candidates.Count(x => x.Status == OpmsImportRowStatus.Invalid), CreatedByUserId = user.Id,
            Rows = candidates
        };
        context.Add(batch);
        await context.SaveChangesAsync();
        batch.SdbipLayer = layer;
        await workflow.WriteAuditTrailAsync("OpmsImportBatch", batch.PublicId.ToString(), "Stage", null,
            new { batch.SourceFileName, batch.SourceSha256, batch.TotalRows, batch.NewRows, batch.UnchangedRows, batch.ChangedRows, batch.InvalidRows },
            user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsImportBatchResponse>(true, ToResponse(batch)));
    }

    [HttpPost("{batchPublicId:guid}/commit"), Authorize(Policy = "Permission:OPMS_KPI.IMPORT")]
    public async Task<ActionResult<ApiResponse<OpmsImportBatchResponse>>> Commit(Guid batchPublicId, CommitOpmsImportRequest request)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(new ApiResponse<OpmsImportBatchResponse>(false, null, "User not found."));
        if (tenantContext.MunicipalityId is not > 0) return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "A municipality context is required."));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "A commit reason of at most 1000 characters is required."));
        var municipalityId = tenantContext.MunicipalityId.Value;
        var batch = await context.OpmsImportBatches.Include(x => x.SdbipLayer).ThenInclude(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear)
            .Include(x => x.Rows).SingleOrDefaultAsync(x => x.MunicipalityId == municipalityId && x.PublicId == batchPublicId);
        if (batch == null) return NotFound(new ApiResponse<OpmsImportBatchResponse>(false, null, "OPMS import batch not found."));
        if (batch.Status != OpmsImportBatchStatus.Staged) return Conflict(new ApiResponse<OpmsImportBatchResponse>(false, null, "Only a staged import can be committed."));
        if (batch.InvalidRows > 0) return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "The complete batch must be valid before commit."));
        if (batch.ChangedRows > 0 && (string.IsNullOrWhiteSpace(request.ApprovalReference) || !request.EffectiveAt.HasValue))
            return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "Changed rows require an approval reference and effective date."));
        byte[] version;
        try { version = Convert.FromBase64String(request.RowVersion); } catch { return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "Invalid RowVersion.")); }
        if (!version.SequenceEqual(batch.RowVersion)) return Conflict(new ApiResponse<OpmsImportBatchResponse>(false, null, "Import preview changed. Stage a new preview."));
        var payloads = batch.Rows.OrderBy(x => x.SourceRowNumber).Select(x => JsonSerializer.Deserialize<OpmsImportRowRequest>(x.PayloadJson, JsonOptions)!).ToArray();
        var fresh = await BuildCandidates(batch.SdbipLayer, payloads);
        if (fresh.Count != batch.Rows.Count || fresh.Zip(batch.Rows.OrderBy(x => x.SourceRowNumber)).Any(x => x.First.Status != x.Second.Status || !VersionsEqual(x.First.ExpectedEntityRowVersion, x.Second.ExpectedEntityRowVersion)))
            return Conflict(new ApiResponse<OpmsImportBatchResponse>(false, null, "A target changed after preview. Stage a new preview."));

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            foreach (var candidate in fresh.Where(x => x.Status == OpmsImportRowStatus.New))
            {
                var dto = JsonSerializer.Deserialize<NormalizedRow>(candidate.NormalizedJson!, JsonOptions)!;
                var target = new OpmsTarget
                {
                    MunicipalityId = batch.MunicipalityId, SdbipLayerId = batch.SdbipLayerId, PeriodId = dto.PeriodId,
                    DepartmentId = dto.DepartmentId, UnitId = dto.UnitId, IndicatorNumber = dto.IndicatorNumber,
                    OriginalOrderNumber = dto.OrderNumber, RevisedOrderNumber = dto.OrderNumber, TargetName = dto.TargetName,
                    KpiDescription = dto.KpiDescription, Baseline = dto.Baseline, Weight = dto.Weight,
                    KpiType = dto.KpiType, IndicatorType = dto.IndicatorType, AnnualTargetDescription = "Imported canonical annual target",
                    AnnualTarget = decimal.TryParse(dto.PeriodTargets.Single(x => x.PeriodType == ReportingPeriodType.Annual).TargetValue, out var annual) ? annual : 0,
                    TargetUnitType = dto.PeriodTargets.Single(x => x.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString(), CreatedAt = DateTime.UtcNow
                };
                var classification = await StrategicClassificationResolver.ResolveAsync(context, batch.SdbipLayer.MunicipalityFinancialYearId,
                    dto.ClassificationSelection);
                if (!classification.IsValid) throw new DbUpdateConcurrencyException(classification.Error ?? "Canonical strategic classification validation failed.");
                StrategicClassificationResolver.Apply(target, classification);
                context.OpmsTargets.Add(target);
                var plan = await TargetPeriodCutover.BuildPlanAsync(context, unitEngine, batch.MunicipalityId, dto.PeriodId, dto.PeriodTargets);
                if (!plan.IsValid) throw new DbUpdateConcurrencyException(plan.Error ?? "Canonical period validation failed.");
                TargetPeriodCutover.AddNewRows(context, plan, batch.MunicipalityId, user.Id, target.Id, null);
            }
            foreach (var candidate in fresh.Where(x => x.Status == OpmsImportRowStatus.Changed))
            {
                var dto = JsonSerializer.Deserialize<NormalizedRow>(candidate.NormalizedJson!, JsonOptions)!;
                var source = JsonSerializer.Deserialize<OpmsImportRowRequest>(candidate.PayloadJson, JsonOptions)!;
                var matchReference = string.IsNullOrWhiteSpace(source.ExistingIndicatorNumber) ? source.IndicatorNumber.Trim() : source.ExistingIndicatorNumber.Trim();
                var layerTargets = await context.OpmsTargets.Where(x => x.SdbipLayerId == batch.SdbipLayerId).ToArrayAsync();
                var target = layerTargets.Single(x => string.Equals(x.IndicatorNumber.Trim(), matchReference, StringComparison.OrdinalIgnoreCase) || string.Equals(x.RevisedIndicatorNumber?.Trim(), matchReference, StringComparison.OrdinalIgnoreCase));
                context.Entry(target).Property(x => x.RowVersion).OriginalValue = candidate.ExpectedEntityRowVersion!;
                AddFieldRevision(target, "IndicatorNumber", target.IsIndicatorNumberRevised ? target.RevisedIndicatorNumber : target.IndicatorNumber, dto.IndicatorNumber, request, user.Id);
                if (!string.Equals(target.IsIndicatorNumberRevised ? target.RevisedIndicatorNumber : target.IndicatorNumber, dto.IndicatorNumber, StringComparison.Ordinal)) { target.IsIndicatorNumberRevised = true; target.RevisedIndicatorNumber = dto.IndicatorNumber; }
                AddFieldRevision(target, "TargetName", target.IsTargetNameRevised ? target.RevisedTargetName : target.TargetName, dto.TargetName, request, user.Id);
                if (!string.Equals(target.IsTargetNameRevised ? target.RevisedTargetName : target.TargetName, dto.TargetName, StringComparison.Ordinal)) { target.IsTargetNameRevised = true; target.RevisedTargetName = dto.TargetName; }
                AddFieldRevision(target, "KpiDescription", target.IsKpiDescriptionRevised ? target.RevisedKpiDescription : target.KpiDescription, dto.KpiDescription, request, user.Id);
                if (!string.Equals(target.IsKpiDescriptionRevised ? target.RevisedKpiDescription : target.KpiDescription, dto.KpiDescription, StringComparison.Ordinal)) { target.IsKpiDescriptionRevised = true; target.RevisedKpiDescription = dto.KpiDescription; }
                AddFieldRevision(target, "OrderNumber", target.RevisedOrderNumber.ToString(), dto.OrderNumber.ToString(), request, user.Id);
                target.RevisedOrderNumber = dto.OrderNumber;
                target.IsRevised = true;
                var plan = await TargetPeriodCutover.BuildPlanAsync(context, unitEngine, batch.MunicipalityId, dto.PeriodId, dto.PeriodTargets);
                var currentRows = await context.PerformancePeriodTargets.Include(x => x.ReportingPeriod).Where(x => x.OpmsTargetId == target.Id && x.IsActive).ToArrayAsync();
                foreach (var desired in plan.Rows.Where(x => x.ReportingPeriod.PeriodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual))
                {
                    var current = currentRows.Single(x => x.ReportingPeriodId == desired.ReportingPeriod.Id);
                    var currentValue = current.IsTargetRevised ? current.RevisedTargetValue : current.TargetValue;
                    if (currentValue != desired.TargetValue)
                    {
                        context.PerformanceTargetRevisions.Add(new PerformanceTargetRevision { MunicipalityId = batch.MunicipalityId, PerformancePeriodTargetId = current.Id, FieldName = "TargetValue", OriginalValue = currentValue, RevisedValue = desired.TargetValue, Reason = request.Reason.Trim(), ApprovalReference = request.ApprovalReference!.Trim(), EffectiveAt = request.EffectiveAt!.Value, RevisedByUserId = user.Id });
                        current.IsTargetRevised = true; current.RevisedTargetValue = desired.TargetValue; current.RevisedUnitKind = desired.UnitKind;
                    }
                    var currentBudget = current.IsBudgetRevised ? current.RevisedBudgetValue : current.BudgetValue;
                    if (currentBudget != desired.BudgetValue)
                    {
                        context.PerformanceTargetRevisions.Add(new PerformanceTargetRevision { MunicipalityId = batch.MunicipalityId, PerformancePeriodTargetId = current.Id, FieldName = "BudgetValue", OriginalValue = currentBudget?.ToString(), RevisedValue = desired.BudgetValue?.ToString(), Reason = request.Reason.Trim(), ApprovalReference = request.ApprovalReference!.Trim(), EffectiveAt = request.EffectiveAt!.Value, RevisedByUserId = user.Id });
                        current.IsBudgetRevised = true; current.RevisedBudgetValue = desired.BudgetValue;
                    }
                }
            }
            batch.Status = OpmsImportBatchStatus.Committed; batch.CommittedAt = DateTime.UtcNow; batch.CommittedByUserId = user.Id;
            context.Entry(batch).Property(x => x.RowVersion).OriginalValue = version;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<OpmsImportBatchResponse>(false, null, "The import preview changed before commit. Stage a new preview."));
        }
        await workflow.WriteAuditTrailAsync("OpmsImportBatch", batch.PublicId.ToString(), "Commit", new { Status = "Staged" },
            new { Status = "Committed", Reason = request.Reason.Trim() }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsImportBatchResponse>(true, ToResponse(batch)));
    }

    private async Task<List<OpmsImportRow>> BuildCandidates(SdbipLayer layer, OpmsImportRowRequest[] rows)
    {
        var result = new List<OpmsImportRow>();
        var duplicateRows = rows.GroupBy(x => x.IndicatorNumber.Trim(), StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicateSourceRows = rows.GroupBy(x => x.SourceRowNumber).Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet();
        var departments = await context.Departments.Where(x => x.MunicipalityId == layer.MunicipalityId && x.IsActive).ToArrayAsync();
        var units = await context.Units.Where(x => x.MunicipalityId == layer.MunicipalityId && x.IsActive).ToArrayAsync();
        var period = await context.Periods.SingleOrDefaultAsync(x => x.IsActive && x.FiscalYear == layer.MunicipalityFinancialYear.FinancialYear.Code);
        var existing = await context.OpmsTargets.Where(x => x.MunicipalityId == layer.MunicipalityId && x.SdbipLayerId == layer.Id).ToArrayAsync();
        var nationalKpas = await context.NationalKpas.AsNoTracking().ToArrayAsync();
        var municipalKpas = await context.MunicipalKpas.AsNoTracking().Where(x => x.MunicipalityId == layer.MunicipalityId).ToArrayAsync();
        var backToBasicsPillars = await context.BackToBasicsPillars.AsNoTracking().ToArrayAsync();
        var strategicGoals = await context.MunicipalStrategicGoals.AsNoTracking().Where(x => x.MunicipalityId == layer.MunicipalityId).ToArrayAsync();
        var strategicInterventions = await context.StrategicInterventions.AsNoTracking().Where(x => x.MunicipalityId == layer.MunicipalityId).ToArrayAsync();
        var strategicObjectives = await context.MunicipalStrategicObjectives.AsNoTracking().Where(x => x.MunicipalityId == layer.MunicipalityId).ToArrayAsync();
        var performanceObjectives = await context.PerformanceObjectives.AsNoTracking().Where(x => x.MunicipalityId == layer.MunicipalityId).ToArrayAsync();
        foreach (var source in rows.OrderBy(x => x.SourceRowNumber))
        {
            OpmsImportRow Invalid(string code, string field, string? value, string message, string? periodName = null) => new()
            { SourceRowNumber = source.SourceRowNumber, Reference = source.IndicatorNumber?.Trim() ?? "", Status = OpmsImportRowStatus.Invalid,
              PayloadJson = JsonSerializer.Serialize(source, JsonOptions), ErrorCode = code, ErrorField = field, ErrorPeriod = periodName,
              SuppliedValue = value?.Length > 1000 ? value[..1000] : value, ErrorMessage = message };
            if (source.SourceRowNumber < 2) { result.Add(Invalid("INVALID_SOURCE_ROW", nameof(source.SourceRowNumber), source.SourceRowNumber.ToString(), "Spreadsheet source row must be 2 or greater.")); continue; }
            if (duplicateSourceRows.Contains(source.SourceRowNumber)) { result.Add(Invalid("DUPLICATE_SOURCE_ROW", nameof(source.SourceRowNumber), source.SourceRowNumber.ToString(), "The batch contains this spreadsheet source row more than once.")); continue; }
            var indicator = source.IndicatorNumber?.Trim() ?? "";
            if (indicator.Length is 0 or > 120) { result.Add(Invalid("INVALID_INDICATOR", nameof(source.IndicatorNumber), source.IndicatorNumber, "IndicatorNumber must contain 1 to 120 characters.")); continue; }
            if (duplicateRows.Contains(indicator)) { result.Add(Invalid("DUPLICATE_INDICATOR", nameof(source.IndicatorNumber), indicator, "The batch contains this KPI reference more than once.")); continue; }
            var matchReference = string.IsNullOrWhiteSpace(source.ExistingIndicatorNumber) ? indicator : source.ExistingIndicatorNumber.Trim();
            var match = existing.SingleOrDefault(x => string.Equals(x.IndicatorNumber.Trim(), matchReference, StringComparison.OrdinalIgnoreCase) || string.Equals(x.RevisedIndicatorNumber?.Trim(), matchReference, StringComparison.OrdinalIgnoreCase));
            if (period == null) { result.Add(Invalid("FINANCIAL_YEAR_NOT_MAPPED", "FinancialYear", layer.MunicipalityFinancialYear.FinancialYear.Code, "No active legacy period maps to the layer financial year.")); continue; }
            var department = departments.SingleOrDefault(x => string.Equals(x.Code.Trim(), source.DepartmentCode?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (department == null) { result.Add(Invalid("DEPARTMENT_NOT_FOUND", nameof(source.DepartmentCode), source.DepartmentCode, "DepartmentCode must identify one active tenant department.")); continue; }
            Unit? unit = null;
            if (!string.IsNullOrWhiteSpace(source.UnitCode)) unit = units.SingleOrDefault(x => x.DepartmentId == department.Id && string.Equals(x.Code.Trim(), source.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(source.UnitCode) && unit == null) { result.Add(Invalid("UNIT_NOT_FOUND", nameof(source.UnitCode), source.UnitCode, "UnitCode must identify one active unit in the selected department.")); continue; }
            if (match != null && !HasCompleteCanonicalClassification(match))
            { result.Add(Invalid("CLASSIFICATION_NOT_RECONCILED", "StrategicClassification", matchReference, "The existing KPI has legacy strategic classifications. Reconcile it before using revision import.")); continue; }
            var references = new[]
            {
                MatchReference(source.NationalKpa, nationalKpas, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.NationalKpa), "National KPA"),
                MatchReference(source.MunicipalKpa, municipalKpas, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.MunicipalKpa), "Municipal KPA"),
                MatchReference(source.BackToBasicsPillar, backToBasicsPillars, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.BackToBasicsPillar), "Back-to-Basics pillar"),
                MatchReference(source.StrategicGoal, strategicGoals, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.StrategicGoal), "Strategic Goal"),
                MatchReference(source.StrategicIntervention, strategicInterventions, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.StrategicIntervention), "Strategic Intervention"),
                MatchReference(source.StrategicObjective, strategicObjectives, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.StrategicObjective), "Strategic Objective"),
                MatchReference(source.PerformanceObjective, performanceObjectives, item => item.Code, item => item.Name, item => item.PublicId, nameof(source.PerformanceObjective), "Performance Objective")
            };
            var invalidReference = references.FirstOrDefault(item => item.ErrorCode != null);
            if (invalidReference != null)
            { result.Add(Invalid(invalidReference.ErrorCode!, invalidReference.Field, invalidReference.Value, invalidReference.ErrorMessage!)); continue; }
            var selection = new StrategicClassificationSelection(references[0].PublicId, references[1].PublicId, references[2].PublicId,
                references[3].PublicId, references[4].PublicId, references[5].PublicId, references[6].PublicId);
            var classification = await StrategicClassificationResolver.ResolveAsync(context, layer.MunicipalityFinancialYearId, selection,
                match?.NationalKpaId, match?.MunicipalKpaId, match?.BackToBasicsPillarId, match?.StrategicGoalMasterId,
                match?.StrategicInterventionId, match?.StrategicObjectiveMasterId, match?.PerformanceObjectiveId);
            if (!classification.IsValid)
            { result.Add(Invalid("INVALID_STRATEGIC_CLASSIFICATION", "StrategicClassification", null, classification.Error!)); continue; }
            var definitionError = OpmsTargetDefinitionPolicy.Validate(indicator, source.OrderNumber, source.TargetName, source.KpiDescription,
                classification.NationalKpa!.Name, classification.MunicipalKpa!.Name, classification.PerformanceObjective!.Name, source.Weight, source.KpiType, source.IndicatorType);
            if (definitionError != null) { result.Add(Invalid("INVALID_KPI", "KPI", indicator, definitionError)); continue; }
            var plan = await TargetPeriodCutover.BuildPlanAsync(context, unitEngine, layer.MunicipalityId, period.Id, source.PeriodTargets);
            if (!plan.IsValid) { result.Add(Invalid("INVALID_PERIOD_TARGET", "PeriodTargets", null, plan.Error!, DetectPeriod(plan.Error!))); continue; }
            var normalized = new NormalizedRow(indicator, source.OrderNumber, source.TargetName.Trim(), source.KpiDescription.Trim(), department.Id, unit?.Id,
                selection, classification.NationalKpa!.Name, classification.MunicipalKpa!.Name, classification.PerformanceObjective!.Name, source.Baseline, source.Weight,
                source.KpiType.Trim(), source.IndicatorType.Trim(), period.Id, source.PeriodTargets);
            var normalizedJson = JsonSerializer.Serialize(normalized, JsonOptions);
            var status = OpmsImportRowStatus.New;
            if (match != null)
            {
                if (match.DepartmentId != normalized.DepartmentId || match.UnitId != normalized.UnitId || match.PeriodId != normalized.PeriodId ||
                    match.NationalKpaId != classification.NationalKpa!.Id || match.MunicipalKpaId != classification.MunicipalKpa!.Id ||
                    match.BackToBasicsPillarId != classification.BackToBasicsPillar!.Id || match.StrategicGoalMasterId != classification.StrategicGoal!.Id ||
                    match.StrategicInterventionId != classification.StrategicIntervention!.Id || match.StrategicObjectiveMasterId != classification.StrategicObjective!.Id ||
                    match.PerformanceObjectiveId != classification.PerformanceObjective!.Id ||
                    match.Baseline != normalized.Baseline || match.Weight != normalized.Weight || match.KpiType != normalized.KpiType || match.IndicatorType != normalized.IndicatorType)
                { result.Add(Invalid("ORIGINAL_FIELD_CHANGE", "KPI", indicator, "The import attempts to change governed original fields. Only KPI number, name, wording, order, Q3, Q4 and Annual targets/budgets may be revised.")); continue; }
                var currentPeriods = await context.PerformancePeriodTargets.Include(x => x.ReportingPeriod).Where(x => x.OpmsTargetId == match.Id && x.IsActive).ToArrayAsync();
                var missingPeriod = plan.Rows.FirstOrDefault(desired => currentPeriods.All(current => current.ReportingPeriodId != desired.ReportingPeriod.Id));
                if (missingPeriod != null) { result.Add(Invalid("PERIOD_NOT_RECONCILED", "PeriodTargets", missingPeriod.TargetValue, $"{missingPeriod.ReportingPeriod.PeriodType} has no governed current row. Reconcile legacy targets before revision import.", missingPeriod.ReportingPeriod.PeriodType.ToString())); continue; }
                var forbidden = plan.Rows.FirstOrDefault(desired => desired.ReportingPeriod.PeriodType is ReportingPeriodType.Quarter1 or ReportingPeriodType.Quarter2 or ReportingPeriodType.MidTerm && currentPeriods.Any(current => current.ReportingPeriodId == desired.ReportingPeriod.Id && ((current.IsTargetRevised ? current.RevisedTargetValue : current.TargetValue) != desired.TargetValue || current.UnitKind != desired.UnitKind || (current.IsBudgetRevised ? current.RevisedBudgetValue : current.BudgetValue) != desired.BudgetValue)));
                if (forbidden != null) { result.Add(Invalid("ORIGINAL_PERIOD_CHANGE", "PeriodTargets", forbidden.TargetValue, $"{forbidden.ReportingPeriod.PeriodType} is an original period and cannot be changed by a revision import.", forbidden.ReportingPeriod.PeriodType.ToString())); continue; }
                var periodChanged = plan.Rows.Any(desired => currentPeriods.Any(current => current.ReportingPeriodId == desired.ReportingPeriod.Id && ((current.IsTargetRevised ? current.RevisedTargetValue : current.TargetValue) != desired.TargetValue || (current.RevisedUnitKind ?? current.UnitKind) != desired.UnitKind || (current.IsBudgetRevised ? current.RevisedBudgetValue : current.BudgetValue) != desired.BudgetValue)));
                status = Equivalent(match, normalized) && !periodChanged ? OpmsImportRowStatus.Unchanged : OpmsImportRowStatus.Changed;
            }
            result.Add(new OpmsImportRow { SourceRowNumber = source.SourceRowNumber, Reference = indicator, Status = status,
                PayloadJson = JsonSerializer.Serialize(source, JsonOptions), NormalizedJson = normalizedJson,
                ExistingValueJson = match == null ? null : JsonSerializer.Serialize(new { match.IndicatorNumber, match.TargetName, match.KpiDescription, match.OriginalOrderNumber }, JsonOptions),
                ExpectedEntityRowVersion = match?.RowVersion });
        }
        return result;
    }

    private void AddFieldRevision(OpmsTarget target, string field, string? original, string? revised, CommitOpmsImportRequest request, string userId)
    {
        if (string.Equals(original, revised, StringComparison.Ordinal)) return;
        context.KpiFieldRevisions.Add(new KpiFieldRevision { MunicipalityId = target.MunicipalityId!.Value, OpmsTargetId = target.Id, FieldName = field, OriginalValue = original, RevisedValue = revised, Reason = request.Reason.Trim(), ApprovalReference = request.ApprovalReference!.Trim(), EffectiveAt = request.EffectiveAt!.Value, RevisedByUserId = userId });
    }
    private static bool Equivalent(OpmsTarget x, NormalizedRow y) =>
        (x.IsIndicatorNumberRevised ? x.RevisedIndicatorNumber : x.IndicatorNumber) == y.IndicatorNumber &&
        (x.IsTargetNameRevised ? x.RevisedTargetName : x.TargetName) == y.TargetName &&
        (x.IsKpiDescriptionRevised ? x.RevisedKpiDescription : x.KpiDescription) == y.KpiDescription && x.RevisedOrderNumber == y.OrderNumber;
    private static bool HasCompleteCanonicalClassification(OpmsTarget target) => target.NationalKpaId.HasValue && target.MunicipalKpaId.HasValue
        && target.BackToBasicsPillarId.HasValue && target.StrategicGoalMasterId.HasValue && target.StrategicInterventionId.HasValue
        && target.StrategicObjectiveMasterId.HasValue && target.PerformanceObjectiveId.HasValue;
    private static ReferenceMatch MatchReference<T>(string? value, IEnumerable<T> values, Func<T, string?> code, Func<T, string> name,
        Func<T, Guid> publicId, string field, string label)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return new(null, "CLASSIFICATION_REQUIRED", field, value, $"{label} is required and must identify a governed master by code or exact name.");
        var matches = values.Where(item => string.Equals(code(item)?.Trim(), normalized, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name(item).Trim(), normalized, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length switch
        {
            1 => new(publicId(matches[0]), null, field, normalized, null),
            0 => new(null, "CLASSIFICATION_NOT_FOUND", field, normalized, $"{label} must identify one governed master in the selected municipality by code or exact name."),
            _ => new(null, "CLASSIFICATION_AMBIGUOUS", field, normalized, $"{label} matches more than one governed master. Supply its unique code.")
        };
    }
    private static string? DetectPeriod(string error) => Enum.GetNames<ReportingPeriodType>().FirstOrDefault(error.Contains);
    private Task<ApplicationUser?> CurrentUser() { var id = PerformanceApiSupport.GetCurrentUserId(User); return string.IsNullOrWhiteSpace(id) ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(id); }
    private static bool VersionsEqual(byte[]? a, byte[]? b) => a == null ? b == null : b != null && a.SequenceEqual(b);
    private FileContentResult CsvFile(string value, string fileName) => File(new UTF8Encoding(true).GetBytes(value), "text/csv; charset=utf-8", fileName);
    private static string SafeFileName(string value) => string.Concat(value.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')).ToLowerInvariant();
    private static OpmsImportBatchResponse ToResponse(OpmsImportBatch x) => new(x.PublicId, x.ClientRequestId, x.SdbipLayer.PublicId, x.SourceFileName, x.SourceSha256, x.Status.ToString(), x.TotalRows, x.NewRows, x.UnchangedRows, x.ChangedRows, x.InvalidRows, x.CreatedAt, x.CommittedAt, Convert.ToBase64String(x.RowVersion), x.Rows.OrderBy(r => r.SourceRowNumber).Select(r => new OpmsImportRowResponse(r.PublicId, r.SourceRowNumber, r.Reference, r.Status.ToString(), r.ExistingValueJson, r.NormalizedJson, r.ErrorCode, r.ErrorPeriod, r.ErrorField, r.SuppliedValue, r.ErrorMessage)).ToArray());
    private sealed record ReferenceMatch(Guid? PublicId, string? ErrorCode, string Field, string? Value, string? ErrorMessage);
    private sealed record NormalizedRow(string IndicatorNumber, int OrderNumber, string TargetName, string KpiDescription, int DepartmentId, int? UnitId,
        StrategicClassificationSelection ClassificationSelection, string NationalKpa, string MunicipalKpa, string PerformanceObjective,
        decimal Baseline, decimal Weight, string KpiType, string IndicatorType, int PeriodId, SaveTargetPeriodValueRequest[] PeriodTargets);
}

internal static class OpmsImportCsv
{
    internal const string WideHeader = "EXISTING_INDICATOR_NUMBER,INDICATOR_NUMBER,ORDER_NUMBER,TARGET_NAME,KPI_DESCRIPTION,DEPARTMENT_CODE,UNIT_CODE,NATIONAL_KPA,MUNICIPAL_KPA,BACK_TO_BASICS_PILLAR,STRATEGIC_GOAL,STRATEGIC_INTERVENTION,STRATEGIC_OBJECTIVE,PERFORMANCE_OBJECTIVE,BASELINE,WEIGHT,KPI_TYPE,INDICATOR_TYPE,Q1_TARGET,Q1_UNIT,Q1_DIRECTION,Q1_BUDGET,Q1_DESCRIPTION,Q2_TARGET,Q2_UNIT,Q2_DIRECTION,Q2_BUDGET,Q2_DESCRIPTION,MID_TERM_TARGET,MID_TERM_UNIT,MID_TERM_DIRECTION,MID_TERM_BUDGET,MID_TERM_DESCRIPTION,Q3_TARGET,Q3_UNIT,Q3_DIRECTION,Q3_BUDGET,Q3_DESCRIPTION,Q4_TARGET,Q4_UNIT,Q4_DIRECTION,Q4_BUDGET,Q4_DESCRIPTION,ANNUAL_TARGET,ANNUAL_UNIT,ANNUAL_DIRECTION,ANNUAL_BUDGET,ANNUAL_DESCRIPTION";
    internal static string ToTemplateUnit(PerformanceUnitKind value) => ToSnakeCase(value.ToString());
    internal static string ToTemplateDirection(PerformanceDirection value) => ToSnakeCase(value.ToString());
    private static string ToSnakeCase(string value) => string.Concat(value.Select((character, index) => index > 0 && char.IsUpper(character) ? "_" + character : character.ToString())).ToUpperInvariant();
}

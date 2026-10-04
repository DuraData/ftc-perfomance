using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    IPerformanceUnitEngine unitEngine, IWorkflowGovernanceService workflow) : ControllerBase
{
    private const int MaximumRows = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        var layer = await context.SdbipLayers.Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear)
            .SingleOrDefaultAsync(x => x.PublicId == layerPublicId && x.IsActive);
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
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            return BadRequest(new ApiResponse<OpmsImportBatchResponse>(false, null, "A commit reason of at most 1000 characters is required."));
        var batch = await context.OpmsImportBatches.Include(x => x.SdbipLayer).ThenInclude(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear)
            .Include(x => x.Rows).SingleOrDefaultAsync(x => x.PublicId == batchPublicId);
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
                    KpiDescription = dto.KpiDescription, NationalKpa = dto.NationalKpa, MunicipalKpa = dto.MunicipalKpa,
                    PerformanceObjective = dto.PerformanceObjective, Baseline = dto.Baseline, Weight = dto.Weight,
                    KpiType = dto.KpiType, IndicatorType = dto.IndicatorType, AnnualTargetDescription = "Imported canonical annual target",
                    AnnualTarget = decimal.TryParse(dto.PeriodTargets.Single(x => x.PeriodType == ReportingPeriodType.Annual).TargetValue, out var annual) ? annual : 0,
                    TargetUnitType = dto.PeriodTargets.Single(x => x.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString(), CreatedAt = DateTime.UtcNow
                };
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
        var departments = await context.Departments.Where(x => x.IsActive).ToArrayAsync();
        var units = await context.Units.Where(x => x.IsActive).ToArrayAsync();
        var period = await context.Periods.SingleOrDefaultAsync(x => x.IsActive && x.FiscalYear == layer.MunicipalityFinancialYear.FinancialYear.Code);
        var existing = await context.OpmsTargets.Where(x => x.SdbipLayerId == layer.Id).ToArrayAsync();
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
            if (period == null) { result.Add(Invalid("FINANCIAL_YEAR_NOT_MAPPED", "FinancialYear", layer.MunicipalityFinancialYear.FinancialYear.Code, "No active legacy period maps to the layer financial year.")); continue; }
            var department = departments.SingleOrDefault(x => string.Equals(x.Code.Trim(), source.DepartmentCode?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (department == null) { result.Add(Invalid("DEPARTMENT_NOT_FOUND", nameof(source.DepartmentCode), source.DepartmentCode, "DepartmentCode must identify one active tenant department.")); continue; }
            Unit? unit = null;
            if (!string.IsNullOrWhiteSpace(source.UnitCode)) unit = units.SingleOrDefault(x => x.DepartmentId == department.Id && string.Equals(x.Code.Trim(), source.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(source.UnitCode) && unit == null) { result.Add(Invalid("UNIT_NOT_FOUND", nameof(source.UnitCode), source.UnitCode, "UnitCode must identify one active unit in the selected department.")); continue; }
            var definitionError = OpmsTargetDefinitionPolicy.Validate(indicator, source.OrderNumber, source.TargetName, source.KpiDescription,
                source.NationalKpa, source.MunicipalKpa, source.PerformanceObjective, source.Weight, source.KpiType, source.IndicatorType);
            if (definitionError != null) { result.Add(Invalid("INVALID_KPI", "KPI", indicator, definitionError)); continue; }
            var plan = await TargetPeriodCutover.BuildPlanAsync(context, unitEngine, layer.MunicipalityId, period.Id, source.PeriodTargets);
            if (!plan.IsValid) { result.Add(Invalid("INVALID_PERIOD_TARGET", "PeriodTargets", null, plan.Error!, DetectPeriod(plan.Error!))); continue; }
            var normalized = new NormalizedRow(indicator, source.OrderNumber, source.TargetName.Trim(), source.KpiDescription.Trim(), department.Id, unit?.Id,
                source.NationalKpa.Trim(), source.MunicipalKpa.Trim(), source.PerformanceObjective.Trim(), source.Baseline, source.Weight,
                source.KpiType.Trim(), source.IndicatorType.Trim(), period.Id, source.PeriodTargets);
            var matchReference = string.IsNullOrWhiteSpace(source.ExistingIndicatorNumber) ? indicator : source.ExistingIndicatorNumber.Trim();
            var match = existing.SingleOrDefault(x => string.Equals(x.IndicatorNumber.Trim(), matchReference, StringComparison.OrdinalIgnoreCase) || string.Equals(x.RevisedIndicatorNumber?.Trim(), matchReference, StringComparison.OrdinalIgnoreCase));
            var normalizedJson = JsonSerializer.Serialize(normalized, JsonOptions);
            var status = OpmsImportRowStatus.New;
            if (match != null)
            {
                if (match.DepartmentId != normalized.DepartmentId || match.UnitId != normalized.UnitId || match.PeriodId != normalized.PeriodId ||
                    match.NationalKpa != normalized.NationalKpa || match.MunicipalKpa != normalized.MunicipalKpa || match.PerformanceObjective != normalized.PerformanceObjective ||
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
    private static string? DetectPeriod(string error) => Enum.GetNames<ReportingPeriodType>().FirstOrDefault(error.Contains);
    private Task<ApplicationUser?> CurrentUser() { var id = PerformanceApiSupport.GetCurrentUserId(User); return string.IsNullOrWhiteSpace(id) ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(id); }
    private static bool VersionsEqual(byte[]? a, byte[]? b) => a == null ? b == null : b != null && a.SequenceEqual(b);
    private static OpmsImportBatchResponse ToResponse(OpmsImportBatch x) => new(x.PublicId, x.ClientRequestId, x.SdbipLayer.PublicId, x.SourceFileName, x.SourceSha256, x.Status.ToString(), x.TotalRows, x.NewRows, x.UnchangedRows, x.ChangedRows, x.InvalidRows, x.CreatedAt, x.CommittedAt, Convert.ToBase64String(x.RowVersion), x.Rows.OrderBy(r => r.SourceRowNumber).Select(r => new OpmsImportRowResponse(r.PublicId, r.SourceRowNumber, r.Reference, r.Status.ToString(), r.ExistingValueJson, r.NormalizedJson, r.ErrorCode, r.ErrorPeriod, r.ErrorField, r.SuppliedValue, r.ErrorMessage)).ToArray());
    private sealed record NormalizedRow(string IndicatorNumber, int OrderNumber, string TargetName, string KpiDescription, int DepartmentId, int? UnitId, string NationalKpa, string MunicipalKpa, string PerformanceObjective, decimal Baseline, decimal Weight, string KpiType, string IndicatorType, int PeriodId, SaveTargetPeriodValueRequest[] PeriodTargets);
}

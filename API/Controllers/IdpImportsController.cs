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

[ApiController]
[Route("api/v1/idp")]
[Authorize]
public class IdpImportsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IWorkflowGovernanceService workflowGovernanceService) : ControllerBase
{
    private const int MaximumRows = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("plans/{planPublicId:guid}/imports")]
    [Authorize(Policy = "Permission:IDP_INDICATOR.IMPORT")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse[]>>> GetBatches(Guid planPublicId)
    {
        var batches = await context.IdpImportBatches
            .AsNoTracking()
            .Include(batch => batch.IdpPlan)
            .Include(batch => batch.Rows)
            .Where(batch => batch.IdpPlan.PublicId == planPublicId)
            .OrderByDescending(batch => batch.CreatedAt)
            .Take(100)
            .ToListAsync();
        return Ok(new ApiResponse<IdpImportBatchResponse[]>(true, batches.Select(ToResponse).ToArray()));
    }

    [HttpGet("imports/{batchPublicId:guid}")]
    [Authorize(Policy = "Permission:IDP_INDICATOR.IMPORT")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> GetBatch(Guid batchPublicId)
    {
        var batch = await context.IdpImportBatches
            .AsNoTracking()
            .Include(item => item.IdpPlan)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.PublicId == batchPublicId);
        return batch == null
            ? NotFound(new ApiResponse<IdpImportBatchResponse>(false, null, "IDP import batch not found."))
            : Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch)));
    }

    [HttpPost("plans/{planPublicId:guid}/imports/kpis/stage")]
    [Authorize(Policy = "Permission:IDP_INDICATOR.IMPORT")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> StageKpis(
        Guid planPublicId,
        [FromBody] StageIdpKpiImportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpImportBatchResponse>(false, null, "User not found."));
        if (request.ClientRequestId == Guid.Empty)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "ClientRequestId is required for idempotency."));
        if (request.Rows is not { Length: > 0 } || request.Rows.Length > MaximumRows)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, $"An import must contain between 1 and {MaximumRows} rows."));

        var plan = await context.IdpPlans.SingleOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null) return NotFound(new ApiResponse<IdpImportBatchResponse>(false, null, "IDP plan not found."));
        if (!plan.MunicipalityId.HasValue)
            return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "The IDP plan must be reconciled to a municipality before importing."));

        var sourceFileName = Path.GetFileName(request.SourceFileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourceFileName) || sourceFileName.Length > 260)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "A valid source file name is required."));
        var sourceHash = ComputeHash(request.Rows);

        var existingBatch = await context.IdpImportBatches
            .Include(item => item.IdpPlan)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.MunicipalityId == plan.MunicipalityId.Value && item.ClientRequestId == request.ClientRequestId);
        if (existingBatch != null)
        {
            if (!string.Equals(existingBatch.SourceSha256, sourceHash, StringComparison.OrdinalIgnoreCase))
                return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "ClientRequestId was already used for different import content."));
            return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(existingBatch), "Existing staged result returned."));
        }

        var candidates = await BuildCandidatesAsync(plan.Id, request.Rows, trackExisting: false);
        var batch = new IdpImportBatch
        {
            ClientRequestId = request.ClientRequestId,
            MunicipalityId = plan.MunicipalityId.Value,
            IdpPlanId = plan.Id,
            ImportType = "KPI",
            SourceFileName = sourceFileName,
            SourceSha256 = sourceHash,
            Status = IdpImportBatchStatus.Staged,
            TotalRows = candidates.Count,
            NewRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.New),
            UnchangedRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.Unchanged),
            ChangedRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.Changed),
            InvalidRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.Invalid),
            CreatedByUserId = user.Id,
            Rows = candidates.Select(item => item.Row).ToList()
        };
        context.IdpImportBatches.Add(batch);
        await context.SaveChangesAsync();
        batch.IdpPlan = plan;

        await workflowGovernanceService.WriteAuditTrailAsync(
            "IdpImportBatch", batch.PublicId.ToString(), "Stage", null,
            new { batch.SourceFileName, batch.SourceSha256, batch.TotalRows, batch.NewRows, batch.UnchangedRows, batch.ChangedRows, batch.InvalidRows },
            user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch)));
    }

    [HttpPost("imports/{batchPublicId:guid}/commit")]
    [Authorize(Policy = "Permission:IDP_INDICATOR.IMPORT")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> Commit(
        Guid batchPublicId,
        [FromBody] CommitIdpImportRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpImportBatchResponse>(false, null, "User not found."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "A commit reason is required."));
        if (request.Reason.Trim().Length > 1000)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "The commit reason cannot exceed 1000 characters."));

        var batch = await context.IdpImportBatches
            .Include(item => item.IdpPlan)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.PublicId == batchPublicId);
        if (batch == null) return NotFound(new ApiResponse<IdpImportBatchResponse>(false, null, "IDP import batch not found."));
        if (batch.Status != IdpImportBatchStatus.Staged)
            return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "Only a staged import can be committed."));
        if (batch.InvalidRows > 0)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "The complete batch must be valid before commit."));
        byte[] expectedBatchVersion;
        try { expectedBatchVersion = Convert.FromBase64String(request.RowVersion ?? string.Empty); }
        catch (FormatException) { return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "Invalid RowVersion.")); }
        if (expectedBatchVersion.Length == 0)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "RowVersion is required."));
        context.Entry(batch).Property(item => item.RowVersion).OriginalValue = expectedBatchVersion;

        var requests = batch.Rows.OrderBy(item => item.SourceRowNumber)
            .Select(item => JsonSerializer.Deserialize<IdpKpiImportRowRequest>(item.PayloadJson, JsonOptions)!)
            .ToArray();
        var current = await BuildCandidatesAsync(batch.IdpPlanId, requests, trackExisting: true);
        var staged = batch.Rows.ToDictionary(item => item.SourceRowNumber);
        foreach (var candidate in current)
        {
            var original = staged[candidate.Row.SourceRowNumber];
            if (candidate.Row.Status == IdpImportRowStatus.Invalid
                || candidate.Row.Status != original.Status
                || !string.Equals(candidate.Row.NormalizedJson, original.NormalizedJson, StringComparison.Ordinal)
                || !VersionsEqual(candidate.Row.ExpectedEntityRowVersion, original.ExpectedEntityRowVersion))
                return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, $"Source row {candidate.Row.SourceRowNumber} changed since reconciliation. Stage a new preview."));
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            foreach (var candidate in current)
            {
                if (candidate.Row.Status == IdpImportRowStatus.Unchanged) continue;
                var entity = candidate.Existing ?? new IdpKpi();
                if (candidate.Existing != null && candidate.Row.ExpectedEntityRowVersion != null)
                    context.Entry(candidate.Existing).Property(item => item.RowVersion).OriginalValue = candidate.Row.ExpectedEntityRowVersion;
                IdpKpiDefinitionPolicy.Apply(entity, candidate.Definition!);
                if (candidate.Existing == null) context.IdpKpis.Add(entity);
            }

            batch.Status = IdpImportBatchStatus.Committed;
            batch.CommittedAt = DateTime.UtcNow;
            batch.CommittedByUserId = user.Id;
            await context.SaveChangesAsync();
            await workflowGovernanceService.WriteAuditTrailAsync(
                "IdpImportBatch", batch.PublicId.ToString(), "Commit",
                new { Status = IdpImportBatchStatus.Staged.ToString() },
                new { Status = batch.Status.ToString(), Reason = request.Reason.Trim(), batch.NewRows, batch.UnchangedRows, batch.ChangedRows },
                user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "The import preview or a target KPI changed before commit. Stage a new preview."));
        }

        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch)));
    }

    private async Task<List<ImportCandidate>> BuildCandidatesAsync(
        int planId,
        IReadOnlyCollection<IdpKpiImportRowRequest> requests,
        bool trackExisting)
    {
        IQueryable<IdpProject> projectQuery = context.IdpProjects
            .Include(item => item.Kpis)
            .Where(item => item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == planId);
        IQueryable<Department> departmentQuery = context.Departments.Where(item => item.IsActive);
        if (!trackExisting)
        {
            projectQuery = projectQuery.AsNoTracking();
            departmentQuery = departmentQuery.AsNoTracking();
        }

        var projects = await projectQuery.ToListAsync();
        var departments = await departmentQuery.ToListAsync();
        var duplicateSourceRows = requests.GroupBy(item => item.SourceRowNumber).Where(group => group.Key <= 0 || group.Count() > 1).Select(group => group.Key).ToHashSet();
        var duplicateBusinessKeys = requests
            .GroupBy(item => $"{NormalizeCode(item.ProjectCode)}|{NormalizeCode(item.KpiCode)}")
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<ImportCandidate>(requests.Count);

        foreach (var request in requests.OrderBy(item => item.SourceRowNumber))
        {
            var payload = JsonSerializer.Serialize(request, JsonOptions);
            var reference = $"{NormalizeCode(request.ProjectCode)}/{NormalizeCode(request.KpiCode)}";
            if (duplicateSourceRows.Contains(request.SourceRowNumber))
            {
                candidates.Add(Invalid(request, payload, reference, "DUPLICATE_SOURCE_ROW", nameof(request.SourceRowNumber), request.SourceRowNumber.ToString(), "Source row numbers must be positive and unique."));
                continue;
            }
            var businessKey = $"{NormalizeCode(request.ProjectCode)}|{NormalizeCode(request.KpiCode)}";
            if (duplicateBusinessKeys.Contains(businessKey))
            {
                candidates.Add(Invalid(request, payload, reference, "DUPLICATE_KPI", nameof(request.KpiCode), request.KpiCode, "The same project/KPI code appears more than once in this batch."));
                continue;
            }

            var projectMatches = projects.Where(item => string.Equals(item.ProjectCode.Trim(), request.ProjectCode?.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            if (projectMatches.Length != 1)
            {
                var code = projectMatches.Length == 0 ? "PROJECT_NOT_FOUND" : "AMBIGUOUS_PROJECT";
                candidates.Add(Invalid(request, payload, reference, code, nameof(request.ProjectCode), request.ProjectCode, "ProjectCode must identify exactly one project in this IDP plan."));
                continue;
            }

            int? departmentId = null;
            if (!string.IsNullOrWhiteSpace(request.ResponsibleDepartmentCode))
            {
                var departmentMatches = departments.Where(item => string.Equals(item.Code.Trim(), request.ResponsibleDepartmentCode.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
                if (departmentMatches.Length != 1)
                {
                    candidates.Add(Invalid(request, payload, reference, "DEPARTMENT_NOT_FOUND", nameof(request.ResponsibleDepartmentCode), request.ResponsibleDepartmentCode, "ResponsibleDepartmentCode must identify one active department in the selected municipality."));
                    continue;
                }
                departmentId = departmentMatches[0].Id;
            }

            var input = new IdpKpiDefinitionInput(
                projectMatches[0].Id, request.KpiCode, request.KpiName, request.Description, request.Formula,
                request.Baseline, request.AnnualTarget, request.FiveYearTarget, departmentId,
                request.DataSource, request.ReportingFrequency, request.IndicatorType,
                request.Circular88Linked, request.TreasuryTidLinked);
            if (!IdpKpiDefinitionPolicy.TryNormalize(input, out var definition, out var issue))
            {
                candidates.Add(Invalid(request, payload, reference, issue!.Code, issue.Field, issue.SuppliedValue, issue.Message));
                continue;
            }

            var existing = projectMatches[0].Kpis.SingleOrDefault(item => string.Equals(item.KpiCode, definition!.KpiCode, StringComparison.OrdinalIgnoreCase));
            var status = existing == null
                ? IdpImportRowStatus.New
                : IdpKpiDefinitionPolicy.IsEquivalent(existing, definition!) ? IdpImportRowStatus.Unchanged : IdpImportRowStatus.Changed;
            var row = new IdpImportRow
            {
                SourceRowNumber = request.SourceRowNumber,
                Reference = reference,
                Status = status,
                PayloadJson = payload,
                NormalizedJson = JsonSerializer.Serialize(definition, JsonOptions),
                ExistingValueJson = existing == null ? null : JsonSerializer.Serialize(Snapshot(existing), JsonOptions),
                ExpectedEntityRowVersion = existing?.RowVersion.ToArray()
            };
            candidates.Add(new(row, definition, existing));
        }

        return candidates;
    }

    private static ImportCandidate Invalid(
        IdpKpiImportRowRequest request,
        string payload,
        string reference,
        string code,
        string field,
        string? supplied,
        string message) => new(new IdpImportRow
        {
            SourceRowNumber = request.SourceRowNumber,
            Reference = reference,
            Status = IdpImportRowStatus.Invalid,
            PayloadJson = payload,
            ErrorCode = code,
            ErrorField = field,
            SuppliedValue = supplied?.Length > 1000 ? supplied[..1000] : supplied,
            ErrorMessage = message
        }, null, null);

    private static object Snapshot(IdpKpi item) => new
    {
        item.PublicId,
        item.IdpProjectId,
        item.KpiCode,
        item.KpiName,
        item.Description,
        item.Formula,
        item.Baseline,
        item.AnnualTarget,
        item.FiveYearTarget,
        item.ResponsibleDepartmentId,
        item.DataSource,
        item.ReportingFrequency,
        IndicatorType = item.IndicatorType.ToString(),
        item.Circular88Linked,
        item.TreasuryTidLinked
    };

    private static IdpImportBatchResponse ToResponse(IdpImportBatch batch) => new(
        batch.PublicId,
        batch.ClientRequestId,
        batch.IdpPlan.PublicId,
        batch.ImportType,
        batch.SourceFileName,
        batch.SourceSha256,
        batch.Status.ToString(),
        batch.TotalRows,
        batch.NewRows,
        batch.UnchangedRows,
        batch.ChangedRows,
        batch.InvalidRows,
        batch.CreatedByUserId,
        batch.CreatedAt,
        batch.CommittedByUserId,
        batch.CommittedAt,
        Convert.ToBase64String(batch.RowVersion),
        batch.Rows.OrderBy(item => item.SourceRowNumber).Select(item => new IdpImportRowResponse(
            item.PublicId, item.SourceRowNumber, item.Reference, item.Status.ToString(), item.ExistingValueJson,
            item.NormalizedJson, item.ErrorCode, item.ErrorField, item.SuppliedValue, item.ErrorMessage)).ToArray());

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(userId);
    }

    private static string ComputeHash(IdpKpiImportRowRequest[] rows) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows, JsonOptions)))).ToLowerInvariant();

    private static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    private static bool VersionsEqual(byte[]? left, byte[]? right) => left == null ? right == null : right != null && left.SequenceEqual(right);

    private sealed record ImportCandidate(IdpImportRow Row, NormalizedIdpKpiDefinition? Definition, IdpKpi? Existing);
}

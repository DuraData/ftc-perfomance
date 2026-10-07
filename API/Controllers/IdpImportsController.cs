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
    IAccessControlService accessControlService,
    IWorkflowGovernanceService workflowGovernanceService) : ControllerBase
{
    private const int MaximumRows = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("plans/{planPublicId:guid}/imports/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>>> GetBatchesPage(
        Guid planPublicId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? status = null,
        [FromQuery] string? importType = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, "User not found."));
        var plan = await context.IdpPlans.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null) return NotFound(new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, "IDP plan not found."));
        var access = await CanReadImportsAsync(user, plan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, access.Reason));
        var memberAccess = await GetImportMemberAccessAsync(user, plan);

        var normalizedStatus = status?.Trim();
        IdpImportBatchStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            if (!Enum.TryParse<IdpImportBatchStatus>(normalizedStatus, true, out var value) || !Enum.IsDefined(value))
                return BadRequest(new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, "Status must be Staged, Committed, or Cancelled."));
            parsedStatus = value;
        }

        var normalizedImportType = importType?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(normalizedImportType) && normalizedImportType is not ("KPI" or "HIERARCHY"))
            return BadRequest(new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, "ImportType must be KPI or HIERARCHY."));
        if (!ImportBatchSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, "SortBy must be createdAt, fileName, status, importType, totalRows, or committedAt."));
        if (request.NormalizedSortBy == "filename" && !memberAccess.SourceFileName)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(false, null, "File-name sorting requires ImportSourceFileName READ permission."));

        var query = context.IdpImportBatches
            .AsNoTracking()
            .Include(batch => batch.IdpPlan)
            .Include(batch => batch.CreatedByUser)
            .Include(batch => batch.CommittedByUser)
            .Where(batch => batch.IdpPlanId == plan.Id)
            .AsQueryable();
        if (parsedStatus.HasValue) query = query.Where(batch => batch.Status == parsedStatus.Value);
        if (!string.IsNullOrEmpty(normalizedImportType)) query = query.Where(batch => batch.ImportType == normalizedImportType);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(batch => memberAccess.SourceFileName && batch.SourceFileName.Contains(request.NormalizedSearch)
                || memberAccess.SourceHash && batch.SourceSha256.Contains(request.NormalizedSearch)
                || memberAccess.Actor && batch.CreatedByUser != null && (batch.CreatedByUser.FirstName.Contains(request.NormalizedSearch) || batch.CreatedByUser.LastName.Contains(request.NormalizedSearch))
                || memberAccess.Actor && batch.CommittedByUser != null && (batch.CommittedByUser.FirstName.Contains(request.NormalizedSearch) || batch.CommittedByUser.LastName.Contains(request.NormalizedSearch)));

        var totalCount = await query.CountAsync();
        var batches = await ApplyImportBatchOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>(true,
            PagedResponse<IdpImportBatchSummaryResponse>.Create(batches.Select(item => ToSummaryResponse(item, memberAccess)), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("plans/{planPublicId:guid}/imports")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse[]>>> GetBatches(Guid planPublicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpImportBatchResponse[]>(false, null, "User not found."));
        var plan = await context.IdpPlans.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null) return NotFound(new ApiResponse<IdpImportBatchResponse[]>(false, null, "IDP plan not found."));
        var access = await CanReadImportsAsync(user, plan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<IdpImportBatchResponse[]>(false, null, access.Reason));
        return StatusCode(StatusCodes.Status410Gone,
            new ApiResponse<IdpImportBatchResponse[]>(false, null, "This unbounded route is retired. Use /api/v1/idp/plans/{planPublicId}/imports/page."));
    }

    private static readonly HashSet<string> ImportBatchSortFields =
        ["createdat", "filename", "status", "importtype", "totalrows", "committedat"];

    private static IOrderedQueryable<IdpImportBatch> ApplyImportBatchOrdering(
        IQueryable<IdpImportBatch> query,
        string sortBy,
        bool descending) => (sortBy, descending) switch
        {
            ("filename", false) => query.OrderBy(item => item.SourceFileName).ThenBy(item => item.Id),
            ("filename", true) => query.OrderByDescending(item => item.SourceFileName).ThenByDescending(item => item.Id),
            ("status", false) => query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id),
            ("importtype", false) => query.OrderBy(item => item.ImportType).ThenBy(item => item.Id),
            ("importtype", true) => query.OrderByDescending(item => item.ImportType).ThenByDescending(item => item.Id),
            ("totalrows", false) => query.OrderBy(item => item.TotalRows).ThenBy(item => item.Id),
            ("totalrows", true) => query.OrderByDescending(item => item.TotalRows).ThenByDescending(item => item.Id),
            ("committedat", false) => query.OrderBy(item => item.CommittedAt).ThenBy(item => item.Id),
            ("committedat", true) => query.OrderByDescending(item => item.CommittedAt).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };

    [HttpGet("imports/{batchPublicId:guid}")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> GetBatch(Guid batchPublicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpImportBatchResponse>(false, null, "User not found."));
        var batch = await context.IdpImportBatches
            .AsNoTracking()
            .Include(item => item.IdpPlan)
            .Include(item => item.CreatedByUser)
            .Include(item => item.CommittedByUser)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.PublicId == batchPublicId);
        if (batch == null) return NotFound(new ApiResponse<IdpImportBatchResponse>(false, null, "IDP import batch not found."));
        var access = await CanReadImportsAsync(user, batch.IdpPlan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<IdpImportBatchResponse>(false, null, access.Reason));
        var memberAccess = await GetImportMemberAccessAsync(user, batch.IdpPlan);
        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch, memberAccess)));
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
        var access = await CheckPlanPermissionAsync(user, "IDP_INDICATOR.IMPORT", plan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IdpImportBatchResponse>(false, null, access.Reason));
        var memberAccess = await GetImportMemberAccessAsync(user, plan);

        var sourceFileName = Path.GetFileName(request.SourceFileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourceFileName) || sourceFileName.Length > 260)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "A valid source file name is required."));
        var sourceHash = ComputeHash(request.Rows);

        var existingBatch = await context.IdpImportBatches
            .Include(item => item.IdpPlan)
            .Include(item => item.CreatedByUser)
            .Include(item => item.CommittedByUser)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.MunicipalityId == plan.MunicipalityId.Value && item.ClientRequestId == request.ClientRequestId);
        if (existingBatch != null)
        {
            if (!string.Equals(existingBatch.SourceSha256, sourceHash, StringComparison.OrdinalIgnoreCase))
                return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "ClientRequestId was already used for different import content."));
            return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(existingBatch, memberAccess), "Existing staged result returned."));
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
            CreatedByUser = user,
            Rows = candidates.Select(item => item.Row).ToList()
        };
        context.IdpImportBatches.Add(batch);
        await context.SaveChangesAsync();
        batch.IdpPlan = plan;

        await workflowGovernanceService.WriteAuditTrailAsync(
            "IdpImportBatch", batch.PublicId.ToString(), "Stage", null,
            new { batch.SourceFileName, batch.SourceSha256, batch.TotalRows, batch.NewRows, batch.UnchangedRows, batch.ChangedRows, batch.InvalidRows },
            user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch, memberAccess)));
    }

    [HttpPost("plans/{planPublicId:guid}/imports/hierarchy/stage")]
    [Authorize(Policy = "Permission:IDP_PLAN.IMPORT")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> StageHierarchy(
        Guid planPublicId,
        [FromBody] StageIdpHierarchyImportRequest request)
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
        var access = await CheckPlanPermissionAsync(user, "IDP_PLAN.IMPORT", plan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IdpImportBatchResponse>(false, null, access.Reason));
        var memberAccess = await GetImportMemberAccessAsync(user, plan);

        var sourceFileName = Path.GetFileName(request.SourceFileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourceFileName) || sourceFileName.Length > 260)
            return BadRequest(new ApiResponse<IdpImportBatchResponse>(false, null, "A valid source file name is required."));
        var sourceHash = ComputeHash(request.Rows);

        var existingBatch = await context.IdpImportBatches
            .Include(item => item.IdpPlan)
            .Include(item => item.CreatedByUser)
            .Include(item => item.CommittedByUser)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.MunicipalityId == plan.MunicipalityId.Value && item.ClientRequestId == request.ClientRequestId);
        if (existingBatch != null)
        {
            if (!string.Equals(existingBatch.SourceSha256, sourceHash, StringComparison.OrdinalIgnoreCase))
                return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "ClientRequestId was already used for different import content."));
            return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(existingBatch, memberAccess), "Existing staged result returned."));
        }

        var candidates = await BuildHierarchyCandidatesAsync(plan, request.Rows, trackExisting: false);
        var batch = new IdpImportBatch
        {
            ClientRequestId = request.ClientRequestId,
            MunicipalityId = plan.MunicipalityId.Value,
            IdpPlanId = plan.Id,
            ImportType = "HIERARCHY",
            SourceFileName = sourceFileName,
            SourceSha256 = sourceHash,
            Status = IdpImportBatchStatus.Staged,
            TotalRows = candidates.Count,
            NewRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.New),
            UnchangedRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.Unchanged),
            ChangedRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.Changed),
            InvalidRows = candidates.Count(item => item.Row.Status == IdpImportRowStatus.Invalid),
            CreatedByUserId = user.Id,
            CreatedByUser = user,
            Rows = candidates.Select(item => item.Row).ToList()
        };
        context.IdpImportBatches.Add(batch);
        await context.SaveChangesAsync();
        batch.IdpPlan = plan;

        await workflowGovernanceService.WriteAuditTrailAsync(
            "IdpImportBatch", batch.PublicId.ToString(), "StageHierarchy", null,
            new { batch.SourceFileName, batch.SourceSha256, batch.TotalRows, batch.NewRows, batch.UnchangedRows, batch.ChangedRows, batch.InvalidRows },
            user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch, memberAccess)));
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
            .Include(item => item.CreatedByUser)
            .Include(item => item.CommittedByUser)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.PublicId == batchPublicId);
        if (batch == null) return NotFound(new ApiResponse<IdpImportBatchResponse>(false, null, "IDP import batch not found."));
        var access = await CheckPlanPermissionAsync(user, "IDP_INDICATOR.IMPORT", batch.IdpPlan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IdpImportBatchResponse>(false, null, access.Reason));
        var memberAccess = await GetImportMemberAccessAsync(user, batch.IdpPlan);
        if (string.Equals(batch.ImportType, "HIERARCHY", StringComparison.OrdinalIgnoreCase))
            return Forbid();
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

        if (!string.Equals(batch.ImportType, "KPI", StringComparison.OrdinalIgnoreCase))
            return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "The import type is not supported by this application version."));

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
            batch.CommittedByUser = user;
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

        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch, memberAccess)));
    }

    [HttpPost("imports/{batchPublicId:guid}/commit-hierarchy")]
    [Authorize(Policy = "Permission:IDP_PLAN.IMPORT")]
    public async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> CommitHierarchy(
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
            .Include(item => item.CreatedByUser)
            .Include(item => item.CommittedByUser)
            .Include(item => item.Rows)
            .SingleOrDefaultAsync(item => item.PublicId == batchPublicId);
        if (batch == null) return NotFound(new ApiResponse<IdpImportBatchResponse>(false, null, "IDP import batch not found."));
        var access = await CheckPlanPermissionAsync(user, "IDP_PLAN.IMPORT", batch.IdpPlan);
        if (!access.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IdpImportBatchResponse>(false, null, access.Reason));
        var memberAccess = await GetImportMemberAccessAsync(user, batch.IdpPlan);
        if (!string.Equals(batch.ImportType, "HIERARCHY", StringComparison.OrdinalIgnoreCase))
            return Forbid();
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

        return await CommitHierarchyAsync(batch, request, user, memberAccess);
    }

    private async Task<ActionResult<ApiResponse<IdpImportBatchResponse>>> CommitHierarchyAsync(
        IdpImportBatch batch,
        CommitIdpImportRequest request,
        ApplicationUser user,
        IdpImportMemberAccess memberAccess)
    {
        var requests = batch.Rows.OrderBy(item => item.SourceRowNumber)
            .Select(item => JsonSerializer.Deserialize<IdpHierarchyImportRowRequest>(item.PayloadJson, JsonOptions)!)
            .ToArray();
        var current = await BuildHierarchyCandidatesAsync(batch.IdpPlan, requests, trackExisting: true);
        var staged = batch.Rows.ToDictionary(item => item.SourceRowNumber);
        foreach (var candidate in current)
        {
            var original = staged[candidate.Row.SourceRowNumber];
            if (candidate.Row.Status == IdpImportRowStatus.Invalid
                || candidate.Row.Status != original.Status
                || !string.Equals(candidate.Row.NormalizedJson, original.NormalizedJson, StringComparison.Ordinal)
                || !string.Equals(candidate.Row.ExistingValueJson, original.ExistingValueJson, StringComparison.Ordinal)
                || !VersionsEqual(candidate.Row.ExpectedEntityRowVersion, original.ExpectedEntityRowVersion))
                return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, $"Source row {candidate.Row.SourceRowNumber} changed since reconciliation. Stage a new preview."));
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await ApplyHierarchyDefinitionsAsync(batch.IdpPlanId, current
                .Where(item => item.Row.Status is IdpImportRowStatus.New or IdpImportRowStatus.Changed)
                .Select(item => item.Definition!)
                .ToArray());
            batch.Status = IdpImportBatchStatus.Committed;
            batch.CommittedAt = DateTime.UtcNow;
            batch.CommittedByUserId = user.Id;
            batch.CommittedByUser = user;
            await context.SaveChangesAsync();
            await workflowGovernanceService.WriteAuditTrailAsync(
                "IdpImportBatch", batch.PublicId.ToString(), "CommitHierarchy",
                new { Status = IdpImportBatchStatus.Staged.ToString() },
                new { Status = batch.Status.ToString(), Reason = request.Reason.Trim(), batch.NewRows, batch.UnchangedRows, batch.ChangedRows },
                user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "The import preview or an IDP hierarchy record changed before commit. Stage a new preview."));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<IdpImportBatchResponse>(false, null, "The hierarchy changed or now conflicts with an existing business key. Stage a new preview."));
        }

        return Ok(new ApiResponse<IdpImportBatchResponse>(true, ToResponse(batch, memberAccess)));
    }

    private async Task<List<HierarchyImportCandidate>> BuildHierarchyCandidatesAsync(
        IdpPlan plan,
        IReadOnlyCollection<IdpHierarchyImportRowRequest> requests,
        bool trackExisting)
    {
        IQueryable<IdpStrategicOutcome> hierarchyQuery = context.IdpStrategicOutcomes
            .Include(item => item.StrategicObjectives)
                .ThenInclude(item => item.DevelopmentPriorities)
                    .ThenInclude(item => item.Programmes)
                        .ThenInclude(item => item.Projects)
            .Where(item => item.IdpPlanId == plan.Id);
        IQueryable<Department> departmentQuery = context.Departments.Where(item => item.IsActive);
        if (!trackExisting)
        {
            hierarchyQuery = hierarchyQuery.AsNoTracking();
            departmentQuery = departmentQuery.AsNoTracking();
        }

        var outcomes = await hierarchyQuery.ToListAsync();
        var departments = await departmentQuery.ToListAsync();
        var duplicateSourceRows = requests.GroupBy(item => item.SourceRowNumber)
            .Where(group => group.Key <= 0 || group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var duplicateProjects = requests.GroupBy(HierarchyProjectKey, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var prepared = new List<PreparedHierarchyRow>(requests.Count);
        foreach (var request in requests.OrderBy(item => item.SourceRowNumber))
        {
            var payload = JsonSerializer.Serialize(request, JsonOptions);
            var reference = HierarchyProjectKey(request).Replace('|', '/');
            if (duplicateSourceRows.Contains(request.SourceRowNumber))
            {
                prepared.Add(new(request, payload, reference, null,
                    new ValidationIssue("DUPLICATE_SOURCE_ROW", nameof(request.SourceRowNumber), request.SourceRowNumber.ToString(), "Source row numbers must be positive and unique.")));
                continue;
            }
            if (duplicateProjects.Contains(HierarchyProjectKey(request)))
            {
                prepared.Add(new(request, payload, reference, null,
                    new ValidationIssue("DUPLICATE_PROJECT", nameof(request.ProjectCode), request.ProjectCode, "The same hierarchy project path appears more than once in this batch.")));
                continue;
            }
            if (!TryNormalizeHierarchy(plan, request, departments, out var definition, out var issue))
            {
                prepared.Add(new(request, payload, reference, null, issue));
                continue;
            }
            prepared.Add(new(request, payload, reference, definition, null));
        }

        var conflictingDefinitions = prepared.Where(item => item.Definition != null)
            .SelectMany(item => DefinitionSignatures(item.Definition!))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(item => item.Value).Distinct(StringComparer.Ordinal).Skip(1).Any())
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = new List<HierarchyImportCandidate>(prepared.Count);
        foreach (var item in prepared)
        {
            if (item.Issue != null)
            {
                candidates.Add(HierarchyInvalid(item, item.Issue));
                continue;
            }
            var definition = item.Definition!;
            var conflict = DefinitionSignatures(definition).FirstOrDefault(signature => conflictingDefinitions.Contains(signature.Key));
            if (conflict != default)
            {
                candidates.Add(HierarchyInvalid(item, new ValidationIssue(
                    "CONFLICTING_PARENT_DEFINITION", "Hierarchy", item.Reference,
                    "Rows sharing a hierarchy code path must supply identical parent definitions.")));
                continue;
            }

            var outcome = outcomes.SingleOrDefault(entity => CodeEquals(entity.Code, definition.Outcome.Code));
            var objective = outcome?.StrategicObjectives.SingleOrDefault(entity => CodeEquals(entity.Code, definition.Objective.Code));
            var priority = objective?.DevelopmentPriorities.SingleOrDefault(entity => CodeEquals(entity.PriorityCode, definition.Priority.Code));
            var programme = priority?.Programmes.SingleOrDefault(entity => CodeEquals(entity.ProgrammeCode, definition.Programme.Code));
            var project = programme?.Projects.SingleOrDefault(entity => CodeEquals(entity.ProjectCode, definition.Project.Code));
            var snapshot = new HierarchySnapshot(
                outcome == null ? null : OutcomeDefinition.From(outcome),
                objective == null ? null : ObjectiveDefinition.From(objective),
                priority == null ? null : PriorityDefinition.From(priority),
                programme == null ? null : ProgrammeDefinition.From(programme),
                project == null ? null : ProjectDefinition.From(project));
            var allExist = outcome != null && objective != null && priority != null && programme != null && project != null;
            var existingPathMatches = (snapshot.Outcome == null || snapshot.Outcome == definition.Outcome)
                && (snapshot.Objective == null || snapshot.Objective == definition.Objective)
                && (snapshot.Priority == null || snapshot.Priority == definition.Priority)
                && (snapshot.Programme == null || snapshot.Programme == definition.Programme)
                && (snapshot.Project == null || snapshot.Project == definition.Project);
            var status = allExist && snapshot.Equals(HierarchySnapshot.From(definition))
                ? IdpImportRowStatus.Unchanged
                : !existingPathMatches ? IdpImportRowStatus.Changed : IdpImportRowStatus.New;
            candidates.Add(new HierarchyImportCandidate(new IdpImportRow
            {
                SourceRowNumber = item.Request.SourceRowNumber,
                Reference = item.Reference,
                Status = status,
                PayloadJson = item.Payload,
                NormalizedJson = JsonSerializer.Serialize(definition, JsonOptions),
                ExistingValueJson = JsonSerializer.Serialize(snapshot, JsonOptions),
                ExpectedEntityRowVersion = HierarchyVersionDigest(outcome, objective, priority, programme, project)
            }, definition));
        }
        return candidates;
    }

    private async Task ApplyHierarchyDefinitionsAsync(int planId, IReadOnlyCollection<HierarchyDefinition> definitions)
    {
        var outcomes = await context.IdpStrategicOutcomes
            .Include(item => item.StrategicObjectives)
                .ThenInclude(item => item.DevelopmentPriorities)
                    .ThenInclude(item => item.Programmes)
                        .ThenInclude(item => item.Projects)
            .Where(item => item.IdpPlanId == planId)
            .ToListAsync();

        foreach (var definition in definitions)
        {
            var outcome = outcomes.SingleOrDefault(item => CodeEquals(item.Code, definition.Outcome.Code));
            if (outcome == null)
            {
                outcome = new IdpStrategicOutcome { IdpPlanId = planId };
                outcomes.Add(outcome);
                context.IdpStrategicOutcomes.Add(outcome);
            }
            definition.Outcome.Apply(outcome);

            var objective = outcome.StrategicObjectives.SingleOrDefault(item => CodeEquals(item.Code, definition.Objective.Code));
            if (objective == null)
            {
                objective = new IdpStrategicObjective { IdpStrategicOutcome = outcome };
                outcome.StrategicObjectives.Add(objective);
                context.IdpStrategicObjectives.Add(objective);
            }
            definition.Objective.Apply(objective);

            var priority = objective.DevelopmentPriorities.SingleOrDefault(item => CodeEquals(item.PriorityCode, definition.Priority.Code));
            if (priority == null)
            {
                priority = new IdpDevelopmentPriority { IdpStrategicObjective = objective };
                objective.DevelopmentPriorities.Add(priority);
                context.IdpDevelopmentPriorities.Add(priority);
            }
            definition.Priority.Apply(priority);

            var programme = priority.Programmes.SingleOrDefault(item => CodeEquals(item.ProgrammeCode, definition.Programme.Code));
            if (programme == null)
            {
                programme = new IdpProgramme { IdpDevelopmentPriority = priority };
                priority.Programmes.Add(programme);
                context.IdpProgrammes.Add(programme);
            }
            definition.Programme.Apply(programme);

            var project = programme.Projects.SingleOrDefault(item => CodeEquals(item.ProjectCode, definition.Project.Code));
            if (project == null)
            {
                project = new IdpProject { IdpProgramme = programme };
                programme.Projects.Add(project);
                context.IdpProjects.Add(project);
            }
            definition.Project.Apply(project);
        }
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

    private static IdpImportBatchResponse ToResponse(IdpImportBatch batch, IdpImportMemberAccess access) => new(
        batch.PublicId,
        access.ClientRequestId ? batch.ClientRequestId : null,
        batch.IdpPlan.PublicId,
        batch.ImportType,
        access.SourceFileName ? batch.SourceFileName : null,
        access.SourceHash ? batch.SourceSha256 : null,
        batch.Status.ToString(),
        batch.TotalRows,
        batch.NewRows,
        batch.UnchangedRows,
        batch.ChangedRows,
        batch.InvalidRows,
        access.Actor ? batch.CreatedByUser?.PublicId : null,
        access.Actor ? batch.CreatedByUser?.FullName : null,
        batch.CreatedAt,
        access.Actor ? batch.CommittedByUser?.PublicId : null,
        access.Actor ? batch.CommittedByUser?.FullName : null,
        batch.CommittedAt,
        Convert.ToBase64String(batch.RowVersion),
        batch.Rows.OrderBy(item => item.SourceRowNumber).Select(item => new IdpImportRowResponse(
             item.PublicId, item.SourceRowNumber, item.Reference, item.Status.ToString(),
             access.RowPayload ? item.ExistingValueJson : null, access.RowPayload ? item.NormalizedJson : null,
             access.ErrorDetail ? item.ErrorCode : null, access.ErrorDetail ? item.ErrorField : null,
             access.RowPayload ? item.SuppliedValue : null, access.ErrorDetail ? item.ErrorMessage : null)).ToArray());

    private static IdpImportBatchSummaryResponse ToSummaryResponse(IdpImportBatch batch, IdpImportMemberAccess access) => new(
        batch.PublicId,
        access.ClientRequestId ? batch.ClientRequestId : null,
        batch.IdpPlan.PublicId,
        batch.ImportType,
        access.SourceFileName ? batch.SourceFileName : null,
        access.SourceHash ? batch.SourceSha256 : null,
        batch.Status.ToString(),
        batch.TotalRows,
        batch.NewRows,
        batch.UnchangedRows,
        batch.ChangedRows,
        batch.InvalidRows,
        access.Actor ? batch.CreatedByUser?.PublicId : null,
        access.Actor ? batch.CreatedByUser?.FullName : null,
        batch.CreatedAt,
        access.Actor ? batch.CommittedByUser?.PublicId : null,
        access.Actor ? batch.CommittedByUser?.FullName : null,
        batch.CommittedAt,
        Convert.ToBase64String(batch.RowVersion));

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(userId);
    }

    private Task<AccessDecisionResult> CheckPlanPermissionAsync(ApplicationUser user, string permission, IdpPlan plan) =>
        accessControlService.CheckPermissionAsync(user, permission,
            new AccessScopeContext(MunicipalityId: plan.MunicipalityId));

    private async Task<AccessDecisionResult> CanReadImportsAsync(ApplicationUser user, IdpPlan plan)
    {
        var indicatorAccess = await CheckPlanPermissionAsync(user, "IDP_INDICATOR.IMPORT", plan);
        return indicatorAccess.Allowed
            ? indicatorAccess
            : await CheckPlanPermissionAsync(user, "IDP_PLAN.IMPORT", plan);
    }

    private async Task<IdpImportMemberAccess> GetImportMemberAccessAsync(ApplicationUser user, IdpPlan plan)
    {
        async Task<bool> Read(string member) =>
            (await CheckPlanPermissionAsync(user, $"IDP_PLAN.{member}.READ", plan)).Allowed;
        return new IdpImportMemberAccess(
            await Read("ImportClientRequestId"), await Read("ImportSourceFileName"),
            await Read("ImportSourceHash"), await Read("ImportActor"),
            await Read("ImportRowPayload"), await Read("ImportErrorDetail"));
    }

    private sealed record IdpImportMemberAccess(
        bool ClientRequestId,
        bool SourceFileName,
        bool SourceHash,
        bool Actor,
        bool RowPayload,
        bool ErrorDetail);

    private static string ComputeHash(IdpKpiImportRowRequest[] rows) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows, JsonOptions)))).ToLowerInvariant();

    private static string ComputeHash(IdpHierarchyImportRowRequest[] rows) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows, JsonOptions)))).ToLowerInvariant();

    private static bool TryNormalizeHierarchy(
        IdpPlan plan,
        IdpHierarchyImportRowRequest request,
        IReadOnlyCollection<Department> departments,
        out HierarchyDefinition? definition,
        out ValidationIssue? issue)
    {
        definition = null;
        issue = null;
        if (!TryRequiredCode(request.OutcomeCode, nameof(request.OutcomeCode), out var outcomeCode, out issue)
            || !TryRequired(request.OutcomeName, nameof(request.OutcomeName), 240, out var outcomeName, out issue)
            || !TryRequired(request.OutcomeDescription, nameof(request.OutcomeDescription), 4000, out var outcomeDescription, out issue)
            || !TryRequiredCode(request.ObjectiveCode, nameof(request.ObjectiveCode), out var objectiveCode, out issue)
            || !TryRequired(request.ObjectiveName, nameof(request.ObjectiveName), 240, out var objectiveName, out issue)
            || !TryRequired(request.ObjectiveDescription, nameof(request.ObjectiveDescription), 4000, out var objectiveDescription, out issue)
            || !TryRequiredCode(request.PriorityCode, nameof(request.PriorityCode), out var priorityCode, out issue)
            || !TryRequired(request.PriorityName, nameof(request.PriorityName), 240, out var priorityName, out issue)
            || !TryRequired(request.PriorityDescription, nameof(request.PriorityDescription), 4000, out var priorityDescription, out issue)
            || !TryRequiredCode(request.ProgrammeCode, nameof(request.ProgrammeCode), out var programmeCode, out issue)
            || !TryRequired(request.ProgrammeName, nameof(request.ProgrammeName), 240, out var programmeName, out issue)
            || !TryRequired(request.ProgrammeDescription, nameof(request.ProgrammeDescription), 4000, out var programmeDescription, out issue)
            || !TryRequiredCode(request.ProjectCode, nameof(request.ProjectCode), out var projectCode, out issue)
            || !TryRequired(request.ProjectName, nameof(request.ProjectName), 240, out var projectName, out issue)
            || !TryRequired(request.ProjectDescription, nameof(request.ProjectDescription), 4000, out var projectDescription, out issue)
            || !TryRequired(request.ProjectCategory, nameof(request.ProjectCategory), 240, out var projectCategory, out issue)
            || !TryRequired(request.ProjectFundingSource, nameof(request.ProjectFundingSource), 240, out var fundingSource, out issue))
            return false;

        if (request.OutcomeSortOrder < 0 || request.ObjectiveSortOrder < 0 || request.PrioritySortOrder < 0)
        {
            issue = new ValidationIssue("INVALID_SORT_ORDER", "SortOrder", null, "Hierarchy sort orders cannot be negative.");
            return false;
        }
        if (request.ObjectiveStartDate >= request.ObjectiveEndDate)
        {
            issue = new ValidationIssue("INVALID_OBJECTIVE_DATES", nameof(request.ObjectiveEndDate), request.ObjectiveEndDate.ToString("O"), "Objective EndDate must be after StartDate.");
            return false;
        }
        if (request.ProjectStartDate >= request.ProjectEndDate)
        {
            issue = new ValidationIssue("INVALID_PROJECT_DATES", nameof(request.ProjectEndDate), request.ProjectEndDate.ToString("O"), "Project EndDate must be after StartDate.");
            return false;
        }
        if (request.ObjectiveBudget < 0 || request.ProgrammePlannedBudget < 0 || request.ProgrammeApprovedBudget < 0
            || request.ProgrammeActualExpenditure < 0 || request.ProjectBudget < 0)
        {
            issue = new ValidationIssue("NEGATIVE_BUDGET", "Budget", null, "Hierarchy budget and expenditure values cannot be negative.");
            return false;
        }
        if (!Enum.TryParse<IdpProjectStatus>(request.ProjectStatus?.Trim(), true, out var projectStatus)
            || !Enum.IsDefined(projectStatus))
        {
            issue = new ValidationIssue("INVALID_PROJECT_STATUS", nameof(request.ProjectStatus), request.ProjectStatus, "ProjectStatus is not recognized.");
            return false;
        }
        if (!TryDepartment(request.ObjectiveDepartmentCode, departments, nameof(request.ObjectiveDepartmentCode), out var objectiveDepartmentId, out issue)
            || !TryDepartment(request.ProgrammeDepartmentCode, departments, nameof(request.ProgrammeDepartmentCode), out var programmeDepartmentId, out issue)
            || !TryDepartment(request.ProjectDepartmentCode, departments, nameof(request.ProjectDepartmentCode), out var projectDepartmentId, out issue))
            return false;

        var objectiveStart = AsUtc(request.ObjectiveStartDate);
        var objectiveEnd = AsUtc(request.ObjectiveEndDate);
        var projectStart = AsUtc(request.ProjectStartDate);
        var projectEnd = AsUtc(request.ProjectEndDate);
        if (objectiveStart < plan.EffectiveFrom || (plan.EffectiveTo.HasValue && objectiveEnd > plan.EffectiveTo.Value))
        {
            issue = new ValidationIssue("OBJECTIVE_OUTSIDE_PLAN", nameof(request.ObjectiveStartDate), request.ObjectiveStartDate.ToString("O"), "Objective dates must fall inside the plan effective period.");
            return false;
        }
        if (projectStart < plan.EffectiveFrom || (plan.EffectiveTo.HasValue && projectEnd > plan.EffectiveTo.Value))
        {
            issue = new ValidationIssue("PROJECT_OUTSIDE_PLAN", nameof(request.ProjectStartDate), request.ProjectStartDate.ToString("O"), "Project dates must fall inside the plan effective period.");
            return false;
        }

        definition = new HierarchyDefinition(
            new OutcomeDefinition(outcomeCode, outcomeName, outcomeDescription, request.OutcomeSortOrder),
            new ObjectiveDefinition(objectiveCode, objectiveName, objectiveDescription, request.ObjectiveBaseline, request.ObjectiveTarget,
                objectiveDepartmentId, objectiveStart, objectiveEnd, request.ObjectiveBudget, request.ObjectiveSortOrder),
            new PriorityDefinition(priorityCode, priorityName, priorityDescription, request.PrioritySortOrder),
            new ProgrammeDefinition(programmeCode, programmeName, programmeDescription, programmeDepartmentId,
                request.ProgrammePlannedBudget, request.ProgrammeApprovedBudget, request.ProgrammeActualExpenditure),
            new ProjectDefinition(projectCode, projectName, projectDescription, projectCategory, projectDepartmentId,
                request.ProjectBudget, fundingSource, projectStart, projectEnd, projectStatus,
                string.IsNullOrWhiteSpace(request.CommunityNeedReference) ? null : request.CommunityNeedReference.Trim()));
        return true;
    }

    private static bool TryRequiredCode(string? value, string field, out string normalized, out ValidationIssue? issue)
    {
        if (!TryRequired(value, field, 80, out normalized, out issue)) return false;
        normalized = normalized.ToUpperInvariant();
        if (normalized.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or '/')))
        {
            issue = new ValidationIssue("INVALID_CODE", field, value, $"{field} may contain letters, digits, hyphens, underscores, dots, and slashes only.");
            return false;
        }
        return true;
    }

    private static bool TryRequired(string? value, string field, int maximumLength, out string normalized, out ValidationIssue? issue)
    {
        normalized = value?.Trim() ?? string.Empty;
        issue = null;
        if (normalized.Length is 0 || normalized.Length > maximumLength)
        {
            issue = new ValidationIssue("INVALID_TEXT", field, value, $"{field} must contain between 1 and {maximumLength} characters.");
            return false;
        }
        return true;
    }

    private static bool TryDepartment(string? code, IReadOnlyCollection<Department> departments, string field, out int? id, out ValidationIssue? issue)
    {
        id = null;
        issue = null;
        if (string.IsNullOrWhiteSpace(code)) return true;
        var matches = departments.Where(item => CodeEquals(item.Code, code)).ToArray();
        if (matches.Length != 1)
        {
            issue = new ValidationIssue("DEPARTMENT_NOT_FOUND", field, code, $"{field} must identify one active department in the selected municipality.");
            return false;
        }
        id = matches[0].Id;
        return true;
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string HierarchyProjectKey(IdpHierarchyImportRowRequest request) => string.Join('|',
        NormalizeCode(request.OutcomeCode), NormalizeCode(request.ObjectiveCode), NormalizeCode(request.PriorityCode),
        NormalizeCode(request.ProgrammeCode), NormalizeCode(request.ProjectCode));

    private static IEnumerable<(string Key, string Value)> DefinitionSignatures(HierarchyDefinition definition)
    {
        var outcomeKey = $"O|{definition.Outcome.Code}";
        var objectiveKey = $"{outcomeKey}|{definition.Objective.Code}";
        var priorityKey = $"{objectiveKey}|{definition.Priority.Code}";
        var programmeKey = $"{priorityKey}|{definition.Programme.Code}";
        yield return (outcomeKey, JsonSerializer.Serialize(definition.Outcome, JsonOptions));
        yield return (objectiveKey, JsonSerializer.Serialize(definition.Objective, JsonOptions));
        yield return (priorityKey, JsonSerializer.Serialize(definition.Priority, JsonOptions));
        yield return (programmeKey, JsonSerializer.Serialize(definition.Programme, JsonOptions));
    }

    private static HierarchyImportCandidate HierarchyInvalid(PreparedHierarchyRow item, ValidationIssue issue) =>
        new(new IdpImportRow
        {
            SourceRowNumber = item.Request.SourceRowNumber,
            Reference = item.Reference,
            Status = IdpImportRowStatus.Invalid,
            PayloadJson = item.Payload,
            ErrorCode = issue.Code,
            ErrorField = issue.Field,
            SuppliedValue = issue.SuppliedValue?.Length > 1000 ? issue.SuppliedValue[..1000] : issue.SuppliedValue,
            ErrorMessage = issue.Message
        }, null);

    private static byte[]? HierarchyVersionDigest(params object?[] entities)
    {
        var versions = entities.Where(item => item != null).Select(item => item switch
        {
            IdpStrategicOutcome value => $"O:{value.PublicId}:{Convert.ToBase64String(value.RowVersion)}",
            IdpStrategicObjective value => $"SO:{value.PublicId}:{Convert.ToBase64String(value.RowVersion)}",
            IdpDevelopmentPriority value => $"D:{value.PublicId}:{Convert.ToBase64String(value.RowVersion)}",
            IdpProgramme value => $"P:{value.PublicId}:{Convert.ToBase64String(value.RowVersion)}",
            IdpProject value => $"J:{value.PublicId}:{Convert.ToBase64String(value.RowVersion)}",
            _ => string.Empty
        }).ToArray();
        return versions.Length == 0 ? null : SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', versions)));
    }

    private static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    private static bool CodeEquals(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static bool VersionsEqual(byte[]? left, byte[]? right) => left == null ? right == null : right != null && left.SequenceEqual(right);

    private sealed record ImportCandidate(IdpImportRow Row, NormalizedIdpKpiDefinition? Definition, IdpKpi? Existing);
    private sealed record HierarchyImportCandidate(IdpImportRow Row, HierarchyDefinition? Definition);
    private sealed record PreparedHierarchyRow(IdpHierarchyImportRowRequest Request, string Payload, string Reference, HierarchyDefinition? Definition, ValidationIssue? Issue);
    private sealed record ValidationIssue(string Code, string Field, string? SuppliedValue, string Message);
    private sealed record HierarchyDefinition(OutcomeDefinition Outcome, ObjectiveDefinition Objective, PriorityDefinition Priority, ProgrammeDefinition Programme, ProjectDefinition Project);
    private sealed record HierarchySnapshot(OutcomeDefinition? Outcome, ObjectiveDefinition? Objective, PriorityDefinition? Priority, ProgrammeDefinition? Programme, ProjectDefinition? Project)
    {
        public static HierarchySnapshot From(HierarchyDefinition value) => new(value.Outcome, value.Objective, value.Priority, value.Programme, value.Project);
    }

    private sealed record OutcomeDefinition(string Code, string Name, string Description, int SortOrder)
    {
        public static OutcomeDefinition From(IdpStrategicOutcome value) => new(value.Code, value.Name, value.Description, value.SortOrder);
        public void Apply(IdpStrategicOutcome value) { value.Code = Code; value.Name = Name; value.Description = Description; value.SortOrder = SortOrder; }
    }

    private sealed record ObjectiveDefinition(string Code, string Name, string Description, decimal Baseline, decimal Target, int? DepartmentId, DateTime StartDate, DateTime EndDate, decimal Budget, int SortOrder)
    {
        public static ObjectiveDefinition From(IdpStrategicObjective value) => new(value.Code, value.Name, value.Description, value.BaselineValue, value.TargetValue, value.ResponsibleDepartmentId, value.StartDate, value.EndDate, value.BudgetAllocation, value.SortOrder);
        public void Apply(IdpStrategicObjective value) { value.Code = Code; value.Name = Name; value.Description = Description; value.BaselineValue = Baseline; value.TargetValue = Target; value.ResponsibleDepartmentId = DepartmentId; value.StartDate = StartDate; value.EndDate = EndDate; value.BudgetAllocation = Budget; value.SortOrder = SortOrder; }
    }

    private sealed record PriorityDefinition(string Code, string Name, string Description, int SortOrder)
    {
        public static PriorityDefinition From(IdpDevelopmentPriority value) => new(value.PriorityCode, value.Name, value.Description, value.SortOrder);
        public void Apply(IdpDevelopmentPriority value) { value.PriorityCode = Code; value.Name = Name; value.Description = Description; value.SortOrder = SortOrder; }
    }

    private sealed record ProgrammeDefinition(string Code, string Name, string Description, int? DepartmentId, decimal PlannedBudget, decimal ApprovedBudget, decimal ActualExpenditure)
    {
        public static ProgrammeDefinition From(IdpProgramme value) => new(value.ProgrammeCode, value.Name, value.Description, value.ResponsibleDepartmentId, value.PlannedBudget, value.ApprovedBudget, value.ActualExpenditure);
        public void Apply(IdpProgramme value) { value.ProgrammeCode = Code; value.Name = Name; value.Description = Description; value.ResponsibleDepartmentId = DepartmentId; value.PlannedBudget = PlannedBudget; value.ApprovedBudget = ApprovedBudget; value.ActualExpenditure = ActualExpenditure; }
    }

    private sealed record ProjectDefinition(string Code, string Name, string Description, string Category, int? DepartmentId, decimal Budget, string FundingSource, DateTime StartDate, DateTime EndDate, IdpProjectStatus Status, string? CommunityNeedReference)
    {
        public static ProjectDefinition From(IdpProject value) => new(value.ProjectCode, value.ProjectName, value.Description, value.Category, value.DepartmentId, value.Budget, value.FundingSource, value.StartDate, value.EndDate, value.Status, value.CommunityNeedReference);
        public void Apply(IdpProject value) { value.ProjectCode = Code; value.ProjectName = Name; value.Description = Description; value.Category = Category; value.DepartmentId = DepartmentId; value.Budget = Budget; value.FundingSource = FundingSource; value.StartDate = StartDate; value.EndDate = EndDate; value.Status = Status; value.CommunityNeedReference = CommunityNeedReference; }
    }
}

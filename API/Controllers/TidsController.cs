using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/tids")]
[Authorize]
public class TidsController : ControllerBase
{
    private const long MaximumDocumentBytes = 25L * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> AllowedDocumentTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"]
    };

    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IAccessControlService accessControl;
    private readonly IWorkflowGovernanceService workflow;
    private readonly ITenantContext tenantContext;
    private readonly IEvidenceBlobStorage storage;
    private readonly IEvidenceInspectionService inspection;
    private readonly IEvidenceMalwareScanner scanner;

    public TidsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccessControlService accessControl,
        IWorkflowGovernanceService workflow,
        ITenantContext tenantContext,
        IEvidenceBlobStorage storage,
        IEvidenceInspectionService inspection,
        IEvidenceMalwareScanner scanner)
    {
        this.context = context;
        this.userManager = userManager;
        this.accessControl = accessControl;
        this.workflow = workflow;
        this.tenantContext = tenantContext;
        this.storage = storage;
        this.inspection = inspection;
        this.scanner = scanner;
    }

    [HttpGet("configuration")]
    public async Task<ActionResult<ApiResponse<TidConfigurationResponse>>> GetConfiguration()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TidConfigurationResponse>(false, null, "User not found."));
        var decision = await accessControl.CheckPermissionAsync(user, "TID.READ");
        if (!decision.Allowed) return Forbidden<TidConfigurationResponse>(decision.Reason);
        var municipality = await CurrentMunicipalityAsync();
        if (municipality == null) return NotFound(new ApiResponse<TidConfigurationResponse>(false, null, "Municipality context not found."));
        return Ok(new ApiResponse<TidConfigurationResponse>(true, await BuildConfigurationAsync(municipality, user)));
    }

    [HttpPut("configuration")]
    public async Task<ActionResult<ApiResponse<TidConfigurationResponse>>> UpdateConfiguration([FromBody] UpdateTidConfigurationRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TidConfigurationResponse>(false, null, "User not found."));
        if (!TryReason(request.Reason, out var reason, out var reasonError)) return BadRequest(new ApiResponse<TidConfigurationResponse>(false, null, reasonError));
        if (!TryVersion(request.RowVersion, out var expectedVersion)) return BadRequest(new ApiResponse<TidConfigurationResponse>(false, null, "A valid RowVersion is required."));
        var municipality = await CurrentMunicipalityAsync();
        if (municipality == null) return NotFound(new ApiResponse<TidConfigurationResponse>(false, null, "Municipality context not found."));
        var decision = await accessControl.CheckPermissionAsync(
            user,
            "TID.CONFIGURE",
            new AccessScopeContext(MunicipalityId: municipality.Id));
        if (!decision.Allowed) return Forbidden<TidConfigurationResponse>(decision.Reason);
        var before = new { municipality.TidEnabled, municipality.TidAllKpisRequired };
        context.Entry(municipality).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        municipality.TidEnabled = request.TidEnabled;
        municipality.TidAllKpisRequired = request.TidEnabled && request.AllKpisRequired;
        workflow.QueueAuditTrail("TidConfiguration", municipality.PublicId.ToString(), "Update", before,
            new { municipality.TidEnabled, municipality.TidAllKpisRequired, Reason = reason }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return Conflict(new ApiResponse<TidConfigurationResponse>(false, null, "The TID configuration changed before this update. Reload and retry."));
        }
        return Ok(new ApiResponse<TidConfigurationResponse>(true, await BuildConfigurationAsync(municipality, user)));
    }

    [HttpGet]
    public ActionResult<ApiResponse<TidRegisterItemResponse[]>> GetRegister([FromQuery] string? search = null)
    {
        _ = search;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<TidRegisterItemResponse[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/tids/page."));
    }

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TidRegisterItemResponse>>>> GetRegisterPage([FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<TidRegisterItemResponse>>(false, null, "User not found."));
        if (!TidRegisterSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<TidRegisterItemResponse>>(false, null, "SortBy must be createdAt, indicatorNumber, targetName, department, or unit."));
        var municipality = await CurrentMunicipalityAsync();
        if (municipality == null || !municipality.TidEnabled)
            return Ok(new ApiResponse<PagedResponse<TidRegisterItemResponse>>(true, PagedResponse<TidRegisterItemResponse>.Empty(request.Page, request.PageSize)));
        var scope = await accessControl.GetQueryScopeAsync(user, "TID.READ");
        if (!scope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<TidRegisterItemResponse>>(true, PagedResponse<TidRegisterItemResponse>.Empty(request.Page, request.PageSize)));

        var query = ApplyScope(context.OpmsTargets.AsNoTracking().Include(item => item.Department).Include(item => item.Unit), scope)
            .Where(item => !item.IsWithdrawn);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || item.TargetName.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var targets = await ApplyTidRegisterOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var targetIds = targets.Select(item => item.Id).ToArray();
        var tids = await context.TechnicalIndicatorDescriptions.AsNoTracking()
            .Include(item => item.OpmsTarget)
            .Include(item => item.ResponsibleEmployee)
            .Include(item => item.CreatedByUser)
            .Include(item => item.SourceDocuments).ThenInclude(item => item.Blob)
            .Include(item => item.SourceDocuments).ThenInclude(item => item.UploadedByUser)
            .Where(item => targetIds.Contains(item.OpmsTargetId) && item.IsCurrent)
            .ToDictionaryAsync(item => item.OpmsTargetId);
        var items = new List<TidRegisterItemResponse>(targets.Length);
        foreach (var target in targets)
        {
            TidVersionResponse? current = null;
            if (tids.TryGetValue(target.Id, out var tid))
                current = ToResponse(tid, await GetMemberAccessAsync(user, target));
            items.Add(new TidRegisterItemResponse(target.PublicId, target.IndicatorNumber, target.TargetName,
                target.Department?.Name, target.Unit?.Name, municipality.TidAllKpisRequired, current));
        }
        return Ok(new ApiResponse<PagedResponse<TidRegisterItemResponse>>(true,
            PagedResponse<TidRegisterItemResponse>.Create(items, request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> TidRegisterSortFields =
        ["createdat", "indicatornumber", "targetname", "department", "unit"];

    private static IOrderedQueryable<OpmsTarget> ApplyTidRegisterOrdering(IQueryable<OpmsTarget> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("indicatornumber", false) => query.OrderBy(item => item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", true) => query.OrderByDescending(item => item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("targetname", false) => query.OrderBy(item => item.TargetName).ThenBy(item => item.PublicId),
            ("targetname", true) => query.OrderByDescending(item => item.TargetName).ThenBy(item => item.PublicId),
            ("department", false) => query.OrderBy(item => item.Department == null ? null : item.Department.Name).ThenBy(item => item.PublicId),
            ("department", true) => query.OrderByDescending(item => item.Department == null ? null : item.Department.Name).ThenBy(item => item.PublicId),
            ("unit", false) => query.OrderBy(item => item.Unit == null ? null : item.Unit.Name).ThenBy(item => item.PublicId),
            ("unit", true) => query.OrderByDescending(item => item.Unit == null ? null : item.Unit.Name).ThenBy(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.PublicId)
        };

    [HttpGet("targets/{targetPublicId:guid}")]
    public ActionResult<ApiResponse<TidVersionResponse[]>> GetHistory(Guid targetPublicId) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<TidVersionResponse[]>(false, null,
            $"This unbounded TID version-history route is retired. Use /api/v1/tids/targets/{targetPublicId}/versions/page."));

    [HttpGet("targets/{targetPublicId:guid}/versions/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TidVersionResponse>>>> GetHistoryPage(
        Guid targetPublicId,
        [FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<TidVersionResponse>>(false, null, "User not found."));
        var target = await context.OpmsTargets.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == targetPublicId);
        if (target == null) return NotFound(new ApiResponse<PagedResponse<TidVersionResponse>>(false, null, "OPMS KPI not found."));
        var decision = await accessControl.CheckPermissionAsync(user, "TID.READ", Scope(target));
        if (!decision.Allowed) return Forbidden<PagedResponse<TidVersionResponse>>(decision.Reason);
        var sortBy = request.SortBy == null ? "versionnumber" : request.NormalizedSortBy;
        if (sortBy is not ("versionnumber" or "effectivefrom" or "createdat" or "responsibleemployee"))
            return BadRequest(new ApiResponse<PagedResponse<TidVersionResponse>>(false, null,
                "SortBy must be versionNumber, effectiveFrom, createdAt, or responsibleEmployee."));
        var query = context.TechnicalIndicatorDescriptions.AsNoTracking()
            .Include(item => item.OpmsTarget)
            .Include(item => item.PreviousVersion)
            .Include(item => item.ResponsibleEmployee)
            .Include(item => item.CreatedByUser)
            .Include(item => item.SourceDocuments).ThenInclude(item => item.Blob)
            .Include(item => item.SourceDocuments).ThenInclude(item => item.UploadedByUser)
            .Where(item => item.OpmsTargetId == target.Id);
        var memberAccess = await GetMemberAccessAsync(user, target);
        if (request.NormalizedSearch.Length > 0)
        {
            var creatorPublicId = Guid.TryParse(request.NormalizedSearch, out var parsedCreatorPublicId)
                ? parsedCreatorPublicId
                : (Guid?)null;
            query = query.Where(item => item.IndicatorDefinition.Contains(request.NormalizedSearch)
                || item.Purpose.Contains(request.NormalizedSearch)
                || item.DataSource.Contains(request.NormalizedSearch)
                || item.CollectionMethod.Contains(request.NormalizedSearch)
                || item.CalculationMethod.Contains(request.NormalizedSearch)
                || item.VerificationMethod.Contains(request.NormalizedSearch)
                || (item.Notes != null && item.Notes.Contains(request.NormalizedSearch))
                || (memberAccess.CreatedByUserId && ((creatorPublicId.HasValue && item.CreatedByUser.PublicId == creatorPublicId.Value)
                    || item.CreatedByUser.FirstName.Contains(request.NormalizedSearch)
                    || item.CreatedByUser.LastName.Contains(request.NormalizedSearch)))
                || (item.ResponsibleEmployee != null && (item.ResponsibleEmployee.FirstName.Contains(request.NormalizedSearch)
                    || item.ResponsibleEmployee.LastName.Contains(request.NormalizedSearch))));
        }
        var totalCount = await query.CountAsync();
        var ordered = (sortBy, request.Descending) switch
        {
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.PublicId),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.PublicId),
            ("createdat", false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.PublicId),
            ("createdat", true) => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.PublicId),
            ("responsibleemployee", false) => query.OrderBy(item => item.ResponsibleEmployee == null ? null : item.ResponsibleEmployee.LastName)
                .ThenBy(item => item.ResponsibleEmployee == null ? null : item.ResponsibleEmployee.FirstName).ThenBy(item => item.PublicId),
            ("responsibleemployee", true) => query.OrderByDescending(item => item.ResponsibleEmployee == null ? null : item.ResponsibleEmployee.LastName)
                .ThenByDescending(item => item.ResponsibleEmployee == null ? null : item.ResponsibleEmployee.FirstName).ThenByDescending(item => item.PublicId),
            ("versionnumber", false) => query.OrderBy(item => item.VersionNumber).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.VersionNumber).ThenByDescending(item => item.PublicId)
        };
        var versions = await ordered.Skip(request.Offset).Take(request.PageSize)
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<TidVersionResponse>>(true,
            PagedResponse<TidVersionResponse>.Create(versions.Select(item => ToResponse(item, memberAccess)), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("targets/{targetPublicId:guid}/versions")]
    public async Task<ActionResult<ApiResponse<TidVersionResponse>>> CreateVersion(Guid targetPublicId, [FromBody] CreateTidVersionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TidVersionResponse>(false, null, "User not found."));
        var municipality = await CurrentMunicipalityAsync();
        if (municipality == null) return NotFound(new ApiResponse<TidVersionResponse>(false, null, "Municipality context not found."));
        if (!municipality.TidEnabled) return Conflict(new ApiResponse<TidVersionResponse>(false, null, "TID is disabled for this municipality."));
        var target = await context.OpmsTargets.Include(item => item.Department).Include(item => item.Unit).SingleOrDefaultAsync(item => item.PublicId == targetPublicId);
        if (target == null) return NotFound(new ApiResponse<TidVersionResponse>(false, null, "OPMS KPI not found."));
        if (target.IsWithdrawn) return Conflict(new ApiResponse<TidVersionResponse>(false, null, "A withdrawn KPI cannot receive a new TID version."));
        var current = await context.TechnicalIndicatorDescriptions.SingleOrDefaultAsync(item => item.OpmsTargetId == target.Id && item.IsCurrent);
        var permission = current == null ? "TID.CREATE" : "TID.UPDATE";
        var decision = await accessControl.CheckPermissionAsync(user, permission, Scope(target));
        if (!decision.Allowed) return Forbidden<TidVersionResponse>(decision.Reason);
        if (!TryNormalize(request, out var normalized, out var validationError))
            return BadRequest(new ApiResponse<TidVersionResponse>(false, null, validationError));
        if (current != null && normalized.EffectiveFrom <= current.EffectiveFrom)
            return BadRequest(new ApiResponse<TidVersionResponse>(false, null, "A successor version must start after the current version."));
        if (current != null)
        {
            if (!TryVersion(request.PreviousVersionRowVersion, out var expectedVersion))
                return BadRequest(new ApiResponse<TidVersionResponse>(false, null, "The current version RowVersion is required."));
            context.Entry(current).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        }

        MunicipalEmployee? employee = null;
        if (request.ResponsibleEmployeePublicId.HasValue)
        {
            employee = await context.MunicipalEmployees.SingleOrDefaultAsync(item => item.PublicId == request.ResponsibleEmployeePublicId.Value && item.IsActive);
            if (employee == null) return BadRequest(new ApiResponse<TidVersionResponse>(false, null, "Responsible employee must be an active employee in the selected municipality."));
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (current != null)
            {
                current.IsCurrent = false;
                current.EffectiveTo = normalized.EffectiveFrom.AddTicks(-1);
            }
            var entity = new TechnicalIndicatorDescription
            {
                MunicipalityId = municipality.Id,
                OpmsTargetId = target.Id,
                VersionNumber = (current?.VersionNumber ?? 0) + 1,
                PreviousVersionId = current?.Id,
                IndicatorDefinition = normalized.IndicatorDefinition,
                Purpose = normalized.Purpose,
                DataSource = normalized.DataSource,
                CollectionMethod = normalized.CollectionMethod,
                CalculationMethod = normalized.CalculationMethod,
                NumeratorDescription = normalized.NumeratorDescription,
                DenominatorDescription = normalized.DenominatorDescription,
                Limitations = normalized.Limitations,
                Assumptions = normalized.Assumptions,
                VerificationMethod = normalized.VerificationMethod,
                ResponsibleEmployeeId = employee?.Id,
                Notes = normalized.Notes,
                EffectiveFrom = normalized.EffectiveFrom,
                CreatedByUserId = user.Id,
                CreatedByUser = user
            };
            context.TechnicalIndicatorDescriptions.Add(entity);
            workflow.QueueAuditTrail("TechnicalIndicatorDescription", entity.PublicId.ToString(), "CreateVersion", null,
                new { target.PublicId, entity.VersionNumber, PreviousVersionPublicId = current?.PublicId, Reason = normalized.Reason }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            entity.OpmsTarget = target;
            entity.PreviousVersion = current;
            entity.ResponsibleEmployee = employee;
            return Ok(new ApiResponse<TidVersionResponse>(true, ToResponse(entity, await GetMemberAccessAsync(user, target))));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
            return Conflict(new ApiResponse<TidVersionResponse>(false, null, "The current TID version changed before the successor was saved. Reload and retry."));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
            return Conflict(new ApiResponse<TidVersionResponse>(false, null, "A current TID version already exists or the version lineage changed. Reload and retry."));
        }
    }

    [HttpPost("{tidPublicId:guid}/documents")]
    [RequestSizeLimit(MaximumDocumentBytes)]
    public async Task<ActionResult<ApiResponse<TidSourceDocumentResponse>>> UploadSourceDocument(
        Guid tidPublicId, [FromForm] UploadTidSourceDocumentRequest request)
    {
        var file = request.File;
        var title = request.Title;
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TidSourceDocumentResponse>(false, null, "User not found."));
        if (file == null || file.Length == 0) return BadRequest(new ApiResponse<TidSourceDocumentResponse>(false, null, "File is required."));
        if (file.Length > MaximumDocumentBytes) return StatusCode(StatusCodes.Status413PayloadTooLarge, new ApiResponse<TidSourceDocumentResponse>(false, null, "File exceeds the 25 MB limit."));
        if (!TryText(title, nameof(title), 240, out var normalizedTitle, out var titleError)) return BadRequest(new ApiResponse<TidSourceDocumentResponse>(false, null, titleError));
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedDocumentTypes.TryGetValue(extension, out var types) || !types.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ApiResponse<TidSourceDocumentResponse>(false, null, "Unsupported source-document type."));
        var tid = await context.TechnicalIndicatorDescriptions.Include(item => item.OpmsTarget).SingleOrDefaultAsync(item => item.PublicId == tidPublicId);
        if (tid == null) return NotFound(new ApiResponse<TidSourceDocumentResponse>(false, null, "TID version not found."));
        var decision = await accessControl.CheckPermissionAsync(user, "TID.UPLOAD_SOURCE", Scope(tid.OpmsTarget));
        if (!decision.Allowed) return Forbidden<TidSourceDocumentResponse>(decision.Reason);

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, HttpContext.RequestAborted);
        var content = buffer.ToArray();
        var inspected = inspection.Inspect(content, extension);
        if (!inspected.SignatureValid)
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ApiResponse<TidSourceDocumentResponse>(false, null, inspected.Error));
        var scan = await scanner.ScanAsync(content, file.FileName, file.ContentType, HttpContext.RequestAborted);
        var storageKey = Path.Combine("tid", tid.PublicId.ToString("N"), Guid.NewGuid().ToString("N") + extension.ToLowerInvariant());
        var stored = await storage.StoreAsync(storageKey, content, HttpContext.RequestAborted);
        if (!stored.Succeeded) return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<TidSourceDocumentResponse>(false, null, "Source-document storage is unavailable: " + stored.Detail));
        var entity = new TidSourceDocument
        {
            MunicipalityId = tid.MunicipalityId,
            TechnicalIndicatorDescriptionId = tid.Id,
            Title = normalizedTitle,
            FileName = Path.GetFileName(file.FileName),
            UploadedByUserId = user.Id,
            UploadedByUser = user,
            Blob = new EvidenceBlob
            {
                MunicipalityId = tid.MunicipalityId, StorageKey = storageKey, ContentType = file.ContentType, SizeInBytes = content.LongLength,
                Sha256 = inspected.Sha256, SignatureVerified = true, ScanStatus = scan.Status, IsQuarantined = !scan.IsClean,
                ScannerProvider = scan.Provider, ScannerReference = scan.ProviderReference, ScanDetail = scan.Detail, ScannedAt = DateTime.UtcNow
            }
        };
        context.TidSourceDocuments.Add(entity);
        workflow.QueueAuditTrail("TidSourceDocument", entity.PublicId.ToString(), "Upload", null,
            new { tid.PublicId, entity.Title, entity.FileName, inspected.Sha256, scan.Status }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch
        {
            await storage.DisposeAsync(storageKey, CancellationToken.None);
            throw;
        }
        entity.TechnicalIndicatorDescription = tid;
        var memberAccess = await GetSourceDocumentMemberAccessAsync(user, tid.OpmsTarget);
        return Ok(new ApiResponse<TidSourceDocumentResponse>(true, ToResponse(entity, memberAccess), scan.IsClean ? "Source document uploaded." : "Source document quarantined pending a clean scan."));
    }

    [HttpGet("{tidPublicId:guid}/documents/{documentPublicId:guid}/content")]
    public async Task<IActionResult> DownloadSourceDocument(Guid tidPublicId, Guid documentPublicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var document = await context.TidSourceDocuments.AsNoTracking()
            .Include(item => item.TechnicalIndicatorDescription).ThenInclude(item => item.OpmsTarget)
            .Include(item => item.Blob)
            .SingleOrDefaultAsync(item => item.PublicId == documentPublicId && item.TechnicalIndicatorDescription.PublicId == tidPublicId);
        if (document == null || document.Blob.IsContentDeleted || document.Blob.IsQuarantined || !document.Blob.SignatureVerified || document.Blob.ScanStatus != "Clean") return NotFound();
        var decision = await accessControl.CheckPermissionAsync(user, "TID.READ", Scope(document.TechnicalIndicatorDescription.OpmsTarget));
        if (!decision.Allowed) return Forbid();
        var stored = await storage.ReadAsync(document.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found) return stored.Available ? NotFound() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        return File(stored.Content, document.Blob.ContentType ?? "application/octet-stream", document.FileName, enableRangeProcessing: true);
    }

    [HttpPost("{tidPublicId:guid}/documents/{documentPublicId:guid}/rescan")]
    public async Task<ActionResult<ApiResponse<TidSourceDocumentResponse>>> RescanSourceDocument(Guid tidPublicId, Guid documentPublicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TidSourceDocumentResponse>(false, null, "User not found."));
        var document = await context.TidSourceDocuments
            .Include(item => item.TechnicalIndicatorDescription).ThenInclude(item => item.OpmsTarget)
            .Include(item => item.Blob)
            .Include(item => item.UploadedByUser)
            .SingleOrDefaultAsync(item => item.PublicId == documentPublicId && item.TechnicalIndicatorDescription.PublicId == tidPublicId);
        if (document == null) return NotFound(new ApiResponse<TidSourceDocumentResponse>(false, null, "TID source document not found."));
        if (document.Blob.IsContentDeleted) return Conflict(new ApiResponse<TidSourceDocumentResponse>(false, null, "Disposed content cannot be rescanned."));
        var decision = await accessControl.CheckPermissionAsync(user, "TID.RESCAN_SOURCE", Scope(document.TechnicalIndicatorDescription.OpmsTarget));
        if (!decision.Allowed) return Forbidden<TidSourceDocumentResponse>(decision.Reason);
        var stored = await storage.ReadAsync(document.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found) return stored.Available
            ? NotFound(new ApiResponse<TidSourceDocumentResponse>(false, null, "Stored source content not found."))
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<TidSourceDocumentResponse>(false, null, "Source-document storage is unavailable: " + stored.Detail));
        var before = new { document.Blob.ScanStatus, document.Blob.IsQuarantined, document.Blob.ScannerReference };
        var scan = await scanner.ScanAsync(stored.Content, document.FileName, document.Blob.ContentType ?? "application/octet-stream", HttpContext.RequestAborted);
        document.Blob.ScanStatus = scan.Status;
        document.Blob.IsQuarantined = !scan.IsClean;
        document.Blob.ScannerProvider = scan.Provider;
        document.Blob.ScannerReference = scan.ProviderReference;
        document.Blob.ScanDetail = scan.Detail;
        document.Blob.ScannedAt = DateTime.UtcNow;
        workflow.QueueAuditTrail("TidSourceDocument", document.PublicId.ToString(), "MalwareRescan", before,
            new { scan.Status, document.Blob.IsQuarantined, scan.ProviderReference }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await context.SaveChangesAsync();
        var memberAccess = await GetSourceDocumentMemberAccessAsync(user, document.TechnicalIndicatorDescription.OpmsTarget);
        return Ok(new ApiResponse<TidSourceDocumentResponse>(true, ToResponse(document, memberAccess), scan.IsClean ? "Source document released after a clean scan." : "Source document remains quarantined."));
    }

    private async Task<TidConfigurationResponse> BuildConfigurationAsync(Municipality municipality, ApplicationUser user)
    {
        var scope = await accessControl.GetQueryScopeAsync(user, "TID.READ");
        if (!scope.PermissionGranted) return new(municipality.PublicId, municipality.TidEnabled, municipality.TidAllKpisRequired, 0, 0, 0, Convert.ToBase64String(municipality.RowVersion));
        var targets = ApplyScope(context.OpmsTargets.AsNoTracking().Where(item => !item.IsWithdrawn), scope);
        var targetCount = await targets.CountAsync();
        var currentCount = await targets.CountAsync(target => target.TechnicalIndicatorDescriptions.Any(tid => tid.IsCurrent));
        return new(municipality.PublicId, municipality.TidEnabled, municipality.TidAllKpisRequired, targetCount, currentCount,
            municipality.TidEnabled && municipality.TidAllKpisRequired ? targetCount - currentCount : 0, Convert.ToBase64String(municipality.RowVersion));
    }

    private static IQueryable<OpmsTarget> ApplyScope(IQueryable<OpmsTarget> query, AccessQueryScopeResult scope) => scope.Unrestricted
        ? query
        : query.Where(item => (item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value))
            || (item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value))
            || (item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId))
            || scope.TargetIds.Contains(item.Id) || scope.KpiIds.Contains(item.Id));

    private static AccessScopeContext Scope(OpmsTarget target) => new(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, KpiId: target.Id, MunicipalityId: target.MunicipalityId);

    private async Task<DocumentMetadataMemberAccess> GetSourceDocumentMemberAccessAsync(ApplicationUser user, OpmsTarget target)
    {
        async Task<bool> CanReadAsync(string memberCode)
        {
            var decision = await accessControl.CheckPermissionAsync(user, $"TID.Source{memberCode}.READ", Scope(target));
            return decision.Allowed;
        }

        return new DocumentMetadataMemberAccess(
            await CanReadAsync("UploadedByUserId"),
            await CanReadAsync("UploadedByName"),
            await CanReadAsync("ScannerProvider"),
            await CanReadAsync("ScannerReference"),
            await CanReadAsync("ScanDetail"));
    }

    private async Task<TidMemberAccess> GetMemberAccessAsync(ApplicationUser user, OpmsTarget target)
    {
        var creator = await accessControl.CheckPermissionAsync(user, "TID.CreatedByUserId.READ", Scope(target));
        return new TidMemberAccess(creator.Allowed, await GetSourceDocumentMemberAccessAsync(user, target));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? null : await userManager.FindByIdAsync(userId);
    }

    private Task<Municipality?> CurrentMunicipalityAsync() => tenantContext.MunicipalityId.HasValue
        ? context.Municipalities.SingleOrDefaultAsync(item => item.Id == tenantContext.MunicipalityId.Value && item.IsActive)
        : Task.FromResult<Municipality?>(null);

    private ActionResult<ApiResponse<T>> Forbidden<T>(string reason) => StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<T>(false, default, reason));

    private static bool TryVersion(string? value, out byte[] version)
    {
        version = [];
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { version = Convert.FromBase64String(value); return version.Length > 0; }
        catch (FormatException) { return false; }
    }

    private static bool TryReason(string? value, out string normalized, out string? error) => TryText(value, "Reason", 1000, out normalized, out error);

    private static bool TryText(string? value, string field, int maximum, out string normalized, out string? error)
    {
        normalized = value?.Trim() ?? string.Empty;
        error = normalized.Length is 0 or > 4000 || normalized.Length > maximum ? $"{field} must contain between 1 and {maximum} characters." : null;
        return error == null;
    }

    private static bool TryOptional(string? value, string field, int maximum, out string? normalized, out string? error)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        error = normalized?.Length > maximum ? $"{field} cannot exceed {maximum} characters." : null;
        return error == null;
    }

    private static bool TryNormalize(CreateTidVersionRequest request, out NormalizedTid value, out string? error)
    {
        value = default!;
        error = null;
        if (!TryText(request.IndicatorDefinition, nameof(request.IndicatorDefinition), 4000, out var definition, out error)
            || !TryText(request.Purpose, nameof(request.Purpose), 4000, out var purpose, out error)
            || !TryText(request.DataSource, nameof(request.DataSource), 2000, out var dataSource, out error)
            || !TryText(request.CollectionMethod, nameof(request.CollectionMethod), 4000, out var collection, out error)
            || !TryText(request.CalculationMethod, nameof(request.CalculationMethod), 4000, out var calculation, out error)
            || !TryText(request.VerificationMethod, nameof(request.VerificationMethod), 4000, out var verification, out error)
            || !TryReason(request.Reason, out var reason, out error)) return false;
        if (!TryOptional(request.NumeratorDescription, nameof(request.NumeratorDescription), 2000, out var numerator, out error)
            || !TryOptional(request.DenominatorDescription, nameof(request.DenominatorDescription), 2000, out var denominator, out error)
            || !TryOptional(request.Limitations, nameof(request.Limitations), 4000, out var limitations, out error)
            || !TryOptional(request.Assumptions, nameof(request.Assumptions), 4000, out var assumptions, out error)
            || !TryOptional(request.Notes, nameof(request.Notes), 4000, out var notes, out error)) return false;
        if ((numerator == null) != (denominator == null))
        {
            error = "Numerator and denominator descriptions must either both be supplied or both be omitted.";
            return false;
        }
        if (request.EffectiveFrom == default)
        {
            error = "EffectiveFrom is required.";
            return false;
        }
        value = new(definition, purpose, dataSource, collection, calculation, numerator, denominator,
            limitations, assumptions, verification, notes, request.EffectiveFrom.Kind switch
            {
                DateTimeKind.Utc => request.EffectiveFrom,
                DateTimeKind.Local => request.EffectiveFrom.ToUniversalTime(),
                _ => DateTime.SpecifyKind(request.EffectiveFrom, DateTimeKind.Utc)
            }, reason);
        return true;
    }

    private static TidVersionResponse ToResponse(TechnicalIndicatorDescription item, TidMemberAccess memberAccess) => new(
        item.PublicId, item.OpmsTarget.PublicId, item.VersionNumber, item.PreviousVersion?.PublicId,
        item.IndicatorDefinition, item.Purpose, item.DataSource, item.CollectionMethod, item.CalculationMethod,
        item.NumeratorDescription, item.DenominatorDescription, item.Limitations, item.Assumptions, item.VerificationMethod,
        item.ResponsibleEmployee?.PublicId, item.ResponsibleEmployee == null ? null : $"{item.ResponsibleEmployee.FirstName} {item.ResponsibleEmployee.LastName}".Trim(),
        item.Notes, item.EffectiveFrom, item.EffectiveTo, item.IsCurrent, item.CreatedAt,
        memberAccess.CreatedByUserId ? item.CreatedByUser.PublicId : null,
        memberAccess.CreatedByUserId ? item.CreatedByUser.FullName : null,
        Convert.ToBase64String(item.RowVersion), item.SourceDocuments.OrderByDescending(document => document.UploadedAt)
            .Select(document => ToResponse(document, memberAccess.SourceDocument)).ToArray());

    private static TidSourceDocumentResponse ToResponse(TidSourceDocument item, DocumentMetadataMemberAccess memberAccess) => new(
        item.PublicId, item.Title, item.FileName, item.Blob.ContentType, item.Blob.SizeInBytes, item.Blob.Sha256,
        item.Blob.ScanStatus, item.Blob.IsQuarantined, item.UploadedAt,
        memberAccess.UploadedByUserId ? item.UploadedByUser.PublicId : null,
        memberAccess.UploadedByName ? item.UploadedByUser?.FullName : null,
        memberAccess.ScannerProvider ? item.Blob.ScannerProvider : null,
        memberAccess.ScannerReference ? item.Blob.ScannerReference : null,
        memberAccess.ScanDetail ? item.Blob.ScanDetail : null,
        $"/api/v1/tids/{item.TechnicalIndicatorDescription.PublicId}/documents/{item.PublicId}/content");

    private sealed record NormalizedTid(
        string IndicatorDefinition, string Purpose, string DataSource, string CollectionMethod, string CalculationMethod,
        string? NumeratorDescription, string? DenominatorDescription, string? Limitations, string? Assumptions,
        string VerificationMethod, string? Notes, DateTime EffectiveFrom, string Reason);
}

using System.Text.Json;
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
[Route("api/v1/strategic-documents")]
[Authorize]
public sealed class StrategicDocumentsController : ControllerBase
{
    private const long MaximumDocumentBytes = 25L * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> AllowedFileTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
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

    public StrategicDocumentsController(
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

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentTypeResponse[]>>> GetTypes([FromQuery] bool includeInactive = false)
    {
        var session = await SessionAsync<StrategicDocumentTypeResponse[]>("STRATEGIC_DOCUMENT.READ");
        if (session.Error != null) return session.Error;
        var canManage = (await accessControl.CheckPermissionAsync(session.User!, "STRATEGIC_DOCUMENT.MANAGE_TYPES", MunicipalityScope())).Allowed;
        var query = context.StrategicDocumentTypes.AsNoTracking();
        if (!includeInactive || !canManage) query = query.Where(item => item.IsActive);
        var rows = await query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ToArrayAsync();
        return Ok(new ApiResponse<StrategicDocumentTypeResponse[]>(true, rows.Select(ToResponse).ToArray()));
    }

    [HttpPost("types")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentTypeResponse>>> CreateType([FromBody] SaveStrategicDocumentTypeRequest request)
    {
        var session = await SessionAsync<StrategicDocumentTypeResponse>("STRATEGIC_DOCUMENT.MANAGE_TYPES");
        if (session.Error != null) return session.Error;
        if (!TryNormalizeType(request, out var normalized, out var error)) return BadRequest(Fail<StrategicDocumentTypeResponse>(error!));
        if (await context.StrategicDocumentTypes.AnyAsync(item => item.Code == normalized.Code))
            return Conflict(Fail<StrategicDocumentTypeResponse>("A strategic-document type with this code already exists."));
        var entity = new StrategicDocumentType
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            Code = normalized.Code,
            Name = normalized.Name,
            Description = normalized.Description,
            AllowsExternalLinks = request.AllowsExternalLinks,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedByUserId = session.User!.Id
        };
        context.StrategicDocumentTypes.Add(entity);
        workflow.QueueAuditTrail(nameof(StrategicDocumentType), entity.PublicId.ToString(), "Create", null,
            new { entity.Code, entity.Name, entity.AllowsExternalLinks, entity.IsActive, entity.DisplayOrder, Reason = normalized.Reason },
            session.User.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(Fail<StrategicDocumentTypeResponse>("A strategic-document type with this code already exists.")); }
        return Ok(new ApiResponse<StrategicDocumentTypeResponse>(true, ToResponse(entity)));
    }

    [HttpPut("types/{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentTypeResponse>>> UpdateType(Guid publicId, [FromBody] SaveStrategicDocumentTypeRequest request)
    {
        var session = await SessionAsync<StrategicDocumentTypeResponse>("STRATEGIC_DOCUMENT.MANAGE_TYPES");
        if (session.Error != null) return session.Error;
        if (!TryNormalizeType(request, out var normalized, out var error)) return BadRequest(Fail<StrategicDocumentTypeResponse>(error!));
        if (!TryVersion(request.RowVersion, out var expectedVersion)) return BadRequest(Fail<StrategicDocumentTypeResponse>("A valid RowVersion is required."));
        var entity = await context.StrategicDocumentTypes.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<StrategicDocumentTypeResponse>("Strategic-document type not found."));
        if (await context.StrategicDocumentTypes.AnyAsync(item => item.Id != entity.Id && item.Code == normalized.Code))
            return Conflict(Fail<StrategicDocumentTypeResponse>("A strategic-document type with this code already exists."));
        var before = new { entity.Code, entity.Name, entity.Description, entity.AllowsExternalLinks, entity.IsActive, entity.DisplayOrder };
        context.Entry(entity).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        entity.Code = normalized.Code;
        entity.Name = normalized.Name;
        entity.Description = normalized.Description;
        entity.AllowsExternalLinks = request.AllowsExternalLinks;
        entity.IsActive = request.IsActive;
        entity.DisplayOrder = request.DisplayOrder;
        workflow.QueueAuditTrail(nameof(StrategicDocumentType), entity.PublicId.ToString(), "Update", before,
            new { entity.Code, entity.Name, entity.Description, entity.AllowsExternalLinks, entity.IsActive, entity.DisplayOrder, Reason = normalized.Reason },
            session.User!.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicDocumentTypeResponse>("The document type changed before this update. Reload and retry.")); }
        catch (DbUpdateException) { return Conflict(Fail<StrategicDocumentTypeResponse>("A strategic-document type with this code already exists.")); }
        return Ok(new ApiResponse<StrategicDocumentTypeResponse>(true, ToResponse(entity)));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse[]>>> GetDocuments(
        [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] bool includeHistory = false,
        [FromQuery] string? search = null)
    {
        var session = await SessionAsync<StrategicDocumentResponse[]>("STRATEGIC_DOCUMENT.READ");
        if (session.Error != null) return session.Error;
        var manager = (await accessControl.CheckPermissionAsync(session.User!, "STRATEGIC_DOCUMENT.UPDATE", MunicipalityScope())).Allowed;
        if (includeHistory && !manager) return Forbidden<StrategicDocumentResponse[]>("Document history requires strategic-document administration permission.");
        var now = DateTime.UtcNow;
        var query = DocumentQuery();
        if (manager && !includeHistory) query = query.Where(item => item.IsCurrent);
        if (!manager)
            query = query.Where(item => item.IsActive && item.IsApproved && item.IsPublished
                && item.DocumentType.IsActive && item.PublishedAt <= now
                && (item.EvidenceBlobId == null
                    ? item.ExternalUrl != null && item.DocumentType.AllowsExternalLinks
                    : item.Blob != null && item.Blob.SignatureVerified && !item.Blob.IsQuarantined && item.Blob.ScanStatus == "Clean" && !item.Blob.IsContentDeleted)
                && !context.StrategicDocuments.Any(later => later.DocumentFamilyId == item.DocumentFamilyId && later.VersionNumber > item.VersionNumber
                    && later.IsActive && later.IsApproved && later.IsPublished && later.DocumentType.IsActive && later.PublishedAt <= now
                    && (later.EvidenceBlobId == null
                        ? later.ExternalUrl != null && later.DocumentType.AllowsExternalLinks
                        : later.Blob != null && later.Blob.SignatureVerified && !later.Blob.IsQuarantined && later.Blob.ScanStatus == "Clean" && !later.Blob.IsContentDeleted)));
        if (municipalityFinancialYearPublicId.HasValue)
            query = query.Where(item => item.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Title.Contains(term) || item.DocumentType.Name.Contains(term) || item.DocumentType.Code.Contains(term));
        }
        var rows = await query.OrderByDescending(item => item.MunicipalityFinancialYear.FinancialYear.StartDate)
            .ThenBy(item => item.DisplayOrder).ThenBy(item => item.Title).ThenByDescending(item => item.VersionNumber).Take(500).ToArrayAsync();
        return Ok(new ApiResponse<StrategicDocumentResponse[]>(true, rows.Select(item => ToResponse(item, manager)).ToArray()));
    }

    [HttpGet("families/{familyId:guid}/versions")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse[]>>> GetVersionHistory(Guid familyId)
    {
        var session = await SessionAsync<StrategicDocumentResponse[]>("STRATEGIC_DOCUMENT.UPDATE");
        if (session.Error != null) return session.Error;
        var rows = await DocumentQuery().Where(item => item.DocumentFamilyId == familyId).OrderByDescending(item => item.VersionNumber).ToArrayAsync();
        if (rows.Length == 0) return NotFound(Fail<StrategicDocumentResponse[]>("Strategic-document family not found."));
        return Ok(new ApiResponse<StrategicDocumentResponse[]>(true, rows.Select(item => ToResponse(item, true)).ToArray()));
    }

    [HttpPost("versions")]
    [RequestSizeLimit(MaximumDocumentBytes)]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse>>> CreateVersion([FromForm] CreateStrategicDocumentVersionRequest request)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized(Fail<StrategicDocumentResponse>("User not found."));
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest(Fail<StrategicDocumentResponse>("Municipality context is required."));
        var permission = request.PreviousVersionPublicId.HasValue ? "STRATEGIC_DOCUMENT.UPDATE" : "STRATEGIC_DOCUMENT.CREATE";
        var decision = await accessControl.CheckPermissionAsync(user, permission, MunicipalityScope());
        if (!decision.Allowed) return Forbidden<StrategicDocumentResponse>(decision.Reason);
        if (!TryNormalizeDocument(request, out var normalized, out var error)) return BadRequest(Fail<StrategicDocumentResponse>(error!));
        var year = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId && item.IsActive);
        if (year == null) return BadRequest(Fail<StrategicDocumentResponse>("Municipality financial year not found or inactive."));
        var type = await context.StrategicDocumentTypes.SingleOrDefaultAsync(item => item.PublicId == request.DocumentTypePublicId && item.IsActive);
        if (type == null) return BadRequest(Fail<StrategicDocumentResponse>("Strategic-document type not found or inactive."));

        StrategicDocument? previous = null;
        if (request.PreviousVersionPublicId.HasValue)
        {
            previous = await context.StrategicDocuments.SingleOrDefaultAsync(item => item.PublicId == request.PreviousVersionPublicId.Value && item.IsCurrent);
            if (previous == null) return Conflict(Fail<StrategicDocumentResponse>("The selected current version no longer exists. Reload and retry."));
            if (previous.MunicipalityFinancialYearId != year.Id || previous.StrategicDocumentTypeId != type.Id)
                return BadRequest(Fail<StrategicDocumentResponse>("A successor must remain in the same municipality financial year and controlled document type. Start a new document family for a different context."));
            if (!TryVersion(request.PreviousVersionRowVersion, out var expectedVersion)) return BadRequest(Fail<StrategicDocumentResponse>("The current version RowVersion is required."));
            context.Entry(previous).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        }

        if (request.File == null && normalized.ExternalUrl == null || request.File != null && normalized.ExternalUrl != null)
            return BadRequest(Fail<StrategicDocumentResponse>("Supply exactly one managed file or approved external link."));
        if (normalized.ExternalUrl != null && !type.AllowsExternalLinks)
            return BadRequest(Fail<StrategicDocumentResponse>("The selected document type does not allow external links."));

        EvidenceBlob? blob = null;
        string? storageKey = null;
        string? fileName = null;
        var familyId = previous?.DocumentFamilyId ?? Guid.NewGuid();
        if (request.File != null)
        {
            if (request.File.Length == 0) return BadRequest(Fail<StrategicDocumentResponse>("The uploaded file is empty."));
            if (request.File.Length > MaximumDocumentBytes) return StatusCode(StatusCodes.Status413PayloadTooLarge, Fail<StrategicDocumentResponse>("File exceeds the 25 MB limit."));
            var extension = Path.GetExtension(request.File.FileName);
            if (!AllowedFileTypes.TryGetValue(extension, out var contentTypes) || !contentTypes.Contains(request.File.ContentType, StringComparer.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, Fail<StrategicDocumentResponse>("Unsupported strategic-document file type."));
            await using var stream = request.File.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, HttpContext.RequestAborted);
            var content = buffer.ToArray();
            var inspected = inspection.Inspect(content, extension);
            if (!inspected.SignatureValid) return StatusCode(StatusCodes.Status415UnsupportedMediaType, Fail<StrategicDocumentResponse>(inspected.Error ?? "File signature is invalid."));
            var scan = await scanner.ScanAsync(content, request.File.FileName, request.File.ContentType, HttpContext.RequestAborted);
            storageKey = Path.Combine("strategic-documents", familyId.ToString("N"), Guid.NewGuid().ToString("N") + extension.ToLowerInvariant());
            var stored = await storage.StoreAsync(storageKey, content, HttpContext.RequestAborted);
            if (!stored.Succeeded) return StatusCode(StatusCodes.Status503ServiceUnavailable, Fail<StrategicDocumentResponse>("Strategic-document storage is unavailable: " + stored.Detail));
            fileName = Path.GetFileName(request.File.FileName);
            blob = new EvidenceBlob
            {
                MunicipalityId = tenantContext.MunicipalityId!.Value,
                StorageKey = storageKey,
                ContentType = request.File.ContentType,
                SizeInBytes = content.LongLength,
                Sha256 = inspected.Sha256,
                SignatureVerified = true,
                ScanStatus = scan.Status,
                IsQuarantined = !scan.IsClean,
                ScannerProvider = scan.Provider,
                ScannerReference = scan.ProviderReference,
                ScanDetail = scan.Detail,
                ScannedAt = DateTime.UtcNow
            };
        }

        var entity = new StrategicDocument
        {
            DocumentFamilyId = familyId,
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            MunicipalityFinancialYearId = year.Id,
            StrategicDocumentTypeId = type.Id,
            PreviousVersionId = previous?.Id,
            VersionNumber = (previous?.VersionNumber ?? 0) + 1,
            SdbipLayer = normalized.SdbipLayer,
            Title = normalized.Title,
            Description = normalized.Description,
            DocumentDate = normalized.DocumentDate,
            Blob = blob,
            FileName = fileName,
            ExternalUrl = normalized.ExternalUrl,
            DisplayOrder = request.DisplayOrder,
            CreatedByUserId = user.Id
        };
        entity.Events.Add(NewEvent(entity, StrategicDocumentEventAction.VersionCreated, normalized.Reason, user.Id));
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (previous != null) previous.IsCurrent = false;
            context.StrategicDocuments.Add(entity);
            workflow.QueueAuditTrail(nameof(StrategicDocument), entity.PublicId.ToString(), "CreateVersion", null,
                new { entity.DocumentFamilyId, entity.VersionNumber, PreviousVersionPublicId = previous?.PublicId, year.PublicId, TypePublicId = type.PublicId, entity.Title, entity.FileName, entity.ExternalUrl, Reason = normalized.Reason },
                user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            if (storageKey != null) await storage.DisposeAsync(storageKey, CancellationToken.None);
            return Conflict(Fail<StrategicDocumentResponse>("The current document version changed before the successor was saved. Reload and retry."));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            if (storageKey != null) await storage.DisposeAsync(storageKey, CancellationToken.None);
            return Conflict(Fail<StrategicDocumentResponse>("The document lineage or selected references changed. Reload and retry."));
        }
        entity.MunicipalityFinancialYear = year;
        entity.DocumentType = type;
        entity.PreviousVersion = previous;
        return Ok(new ApiResponse<StrategicDocumentResponse>(true, ToResponse(entity, true), blob?.IsQuarantined == true ? "Version created; its file is quarantined pending a clean scan." : "Strategic-document version created."));
    }

    [HttpPost("{publicId:guid}/approve")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse>>> Approve(Guid publicId, [FromBody] ApproveStrategicDocumentRequest request)
    {
        var loaded = await LoadForCommand(publicId, "STRATEGIC_DOCUMENT.APPROVE", request.RowVersion);
        if (loaded.Error != null) return loaded.Error;
        if (!TryText(request.ApprovalReference, "ApprovalReference", 240, out var reference, out var error) || !TryReason(request.Reason, out var reason, out error))
            return BadRequest(Fail<StrategicDocumentResponse>(error!));
        var entity = loaded.Document!;
        if (!entity.IsCurrent || !entity.IsActive) return Conflict(Fail<StrategicDocumentResponse>("Only the active current version can be approved."));
        if (entity.IsApproved) return Conflict(Fail<StrategicDocumentResponse>("This version is already approved."));
        if (!ContentReady(entity)) return Conflict(Fail<StrategicDocumentResponse>("The managed file must pass signature and malware checks before approval."));
        entity.IsApproved = true;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = loaded.User!.Id;
        entity.ApprovalReference = reference;
        entity.Events.Add(NewEvent(entity, StrategicDocumentEventAction.Approved, reason, loaded.User.Id));
        workflow.QueueAuditTrail(nameof(StrategicDocument), entity.PublicId.ToString(), "Approve", null, new { ApprovalReference = reference, Reason = reason }, loaded.User.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return await SaveCommand(entity, "The document changed before approval. Reload and retry.");
    }

    [HttpPost("{publicId:guid}/publish")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse>>> Publish(Guid publicId, [FromBody] PublishStrategicDocumentRequest request)
    {
        var loaded = await LoadForCommand(publicId, "STRATEGIC_DOCUMENT.PUBLISH", request.RowVersion);
        if (loaded.Error != null) return loaded.Error;
        if (!TryReason(request.Reason, out var reason, out var error)) return BadRequest(Fail<StrategicDocumentResponse>(error!));
        if (request.PublicationDate == default) return BadRequest(Fail<StrategicDocumentResponse>("PublicationDate is required."));
        var entity = loaded.Document!;
        if (!entity.IsCurrent || !entity.IsActive || !entity.IsApproved) return Conflict(Fail<StrategicDocumentResponse>("Only an active, approved current version can be published."));
        if (entity.IsPublished) return Conflict(Fail<StrategicDocumentResponse>("This version is already published."));
        if (!ContentReady(entity)) return Conflict(Fail<StrategicDocumentResponse>("The managed file must pass signature and malware checks before publication."));
        entity.IsPublished = true;
        entity.PublicationDate = NormalizeUtc(request.PublicationDate);
        entity.PublishedAt = DateTime.UtcNow;
        entity.PublishedByUserId = loaded.User!.Id;
        entity.Events.Add(NewEvent(entity, StrategicDocumentEventAction.Published, reason, loaded.User.Id));
        workflow.QueueAuditTrail(nameof(StrategicDocument), entity.PublicId.ToString(), "Publish", null, new { entity.PublicationDate, Reason = reason }, loaded.User.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return await SaveCommand(entity, "The document changed before publication. Reload and retry.");
    }

    [HttpPost("{publicId:guid}/retire")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse>>> Retire(Guid publicId, [FromBody] RetireStrategicDocumentRequest request)
    {
        var loaded = await LoadForCommand(publicId, "STRATEGIC_DOCUMENT.RETIRE", request.RowVersion);
        if (loaded.Error != null) return loaded.Error;
        if (!TryReason(request.Reason, out var reason, out var error)) return BadRequest(Fail<StrategicDocumentResponse>(error!));
        var entity = loaded.Document!;
        if (!entity.IsActive) return Conflict(Fail<StrategicDocumentResponse>("This version is already retired."));
        entity.IsActive = false;
        entity.Events.Add(NewEvent(entity, StrategicDocumentEventAction.Retired, reason, loaded.User!.Id));
        workflow.QueueAuditTrail(nameof(StrategicDocument), entity.PublicId.ToString(), "Retire", null, new { Reason = reason }, loaded.User.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return await SaveCommand(entity, "The document changed before retirement. Reload and retry.");
    }

    [HttpPost("{publicId:guid}/rescan")]
    public async Task<ActionResult<ApiResponse<StrategicDocumentResponse>>> Rescan(Guid publicId)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized(Fail<StrategicDocumentResponse>("User not found."));
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest(Fail<StrategicDocumentResponse>("Municipality context is required."));
        var decision = await accessControl.CheckPermissionAsync(user, "STRATEGIC_DOCUMENT.RESCAN", MunicipalityScope());
        if (!decision.Allowed) return Forbidden<StrategicDocumentResponse>(decision.Reason);
        var entity = await DocumentQuery(tracking: true).SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<StrategicDocumentResponse>("Strategic document not found."));
        if (entity.Blob == null || entity.Blob.IsContentDeleted) return Conflict(Fail<StrategicDocumentResponse>("This document has no available managed file to rescan."));
        var stored = await storage.ReadAsync(entity.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found) return stored.Available ? NotFound(Fail<StrategicDocumentResponse>("Stored document content not found.")) : StatusCode(StatusCodes.Status503ServiceUnavailable, Fail<StrategicDocumentResponse>("Strategic-document storage is unavailable: " + stored.Detail));
        var before = new { entity.Blob.ScanStatus, entity.Blob.IsQuarantined, entity.Blob.ScannerReference };
        var scan = await scanner.ScanAsync(stored.Content, entity.FileName ?? "strategic-document", entity.Blob.ContentType ?? "application/octet-stream", HttpContext.RequestAborted);
        entity.Blob.ScanStatus = scan.Status;
        entity.Blob.IsQuarantined = !scan.IsClean;
        entity.Blob.ScannerProvider = scan.Provider;
        entity.Blob.ScannerReference = scan.ProviderReference;
        entity.Blob.ScanDetail = scan.Detail;
        entity.Blob.ScannedAt = DateTime.UtcNow;
        entity.Events.Add(NewEvent(entity, StrategicDocumentEventAction.MalwareRescanned, scan.Detail ?? scan.Status, user.Id));
        workflow.QueueAuditTrail(nameof(StrategicDocument), entity.PublicId.ToString(), "MalwareRescan", before,
            new { scan.Status, entity.Blob.IsQuarantined, scan.ProviderReference }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<StrategicDocumentResponse>(true, ToResponse(entity, true), scan.IsClean ? "Document released after a clean scan." : "Document remains quarantined."));
    }

    [HttpGet("{publicId:guid}/content")]
    public async Task<IActionResult> Download(Guid publicId)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized();
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest();
        var read = await accessControl.CheckPermissionAsync(user, "STRATEGIC_DOCUMENT.READ", MunicipalityScope());
        if (!read.Allowed) return Forbid();
        var entity = await DocumentQuery().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity?.Blob == null || !ContentReady(entity)) return NotFound();
        var manager = (await accessControl.CheckPermissionAsync(user, "STRATEGIC_DOCUMENT.UPDATE", MunicipalityScope())).Allowed;
        if (!manager && (!entity.IsActive || !entity.IsApproved || !entity.IsPublished || !entity.DocumentType.IsActive || entity.PublishedAt > DateTime.UtcNow
            || await context.StrategicDocuments.AnyAsync(later => later.DocumentFamilyId == entity.DocumentFamilyId && later.VersionNumber > entity.VersionNumber
                && later.IsActive && later.IsApproved && later.IsPublished && later.DocumentType.IsActive && later.PublishedAt <= DateTime.UtcNow
                && (later.EvidenceBlobId == null
                    ? later.ExternalUrl != null && later.DocumentType.AllowsExternalLinks
                    : later.Blob != null && later.Blob.SignatureVerified && !later.Blob.IsQuarantined && later.Blob.ScanStatus == "Clean" && !later.Blob.IsContentDeleted)))) return NotFound();
        var stored = await storage.ReadAsync(entity.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found) return stored.Available ? NotFound() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        return File(stored.Content, entity.Blob.ContentType ?? "application/octet-stream", entity.FileName ?? entity.Title, enableRangeProcessing: true);
    }

    private IQueryable<StrategicDocument> DocumentQuery(bool tracking = false)
    {
        var query = tracking ? context.StrategicDocuments.AsQueryable() : context.StrategicDocuments.AsNoTracking();
        return query.Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Include(item => item.DocumentType).Include(item => item.PreviousVersion).Include(item => item.Blob)
            .Include(item => item.Events);
    }

    private async Task<(ApplicationUser? User, StrategicDocument? Document, ActionResult<ApiResponse<StrategicDocumentResponse>>? Error)> LoadForCommand(Guid publicId, string permission, string rowVersion)
    {
        var user = await CurrentUserAsync();
        if (user == null) return (null, null, Unauthorized(Fail<StrategicDocumentResponse>("User not found.")));
        if (!tenantContext.MunicipalityId.HasValue) return (user, null, BadRequest(Fail<StrategicDocumentResponse>("Municipality context is required.")));
        var decision = await accessControl.CheckPermissionAsync(user, permission, MunicipalityScope());
        if (!decision.Allowed) return (user, null, Forbidden<StrategicDocumentResponse>(decision.Reason));
        if (!TryVersion(rowVersion, out var expectedVersion)) return (user, null, BadRequest(Fail<StrategicDocumentResponse>("A valid RowVersion is required.")));
        var entity = await DocumentQuery(tracking: true).SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return (user, null, NotFound(Fail<StrategicDocumentResponse>("Strategic document not found.")));
        context.Entry(entity).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        return (user, entity, null);
    }

    private async Task<ActionResult<ApiResponse<StrategicDocumentResponse>>> SaveCommand(StrategicDocument entity, string concurrencyMessage)
    {
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return Conflict(Fail<StrategicDocumentResponse>(concurrencyMessage));
        }
        return Ok(new ApiResponse<StrategicDocumentResponse>(true, ToResponse(entity, true)));
    }

    private async Task<(ApplicationUser? User, ActionResult<ApiResponse<T>>? Error)> SessionAsync<T>(string permission)
    {
        var user = await CurrentUserAsync();
        if (user == null) return (null, Unauthorized(new ApiResponse<T>(false, default, "User not found.")));
        if (!tenantContext.MunicipalityId.HasValue) return (user, BadRequest(new ApiResponse<T>(false, default, "Municipality context is required.")));
        var decision = await accessControl.CheckPermissionAsync(user, permission, MunicipalityScope());
        return decision.Allowed ? (user, null) : (user, Forbidden<T>(decision.Reason));
    }

    private Task<ApplicationUser?> CurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(userId);
    }

    private AccessScopeContext MunicipalityScope() => new(MunicipalityId: tenantContext.MunicipalityId);

    private static StrategicDocumentEvent NewEvent(StrategicDocument document, StrategicDocumentEventAction action, string reason, string actorUserId) => new()
    {
        MunicipalityId = document.MunicipalityId,
        Action = action,
        Reason = reason,
        ActorUserId = actorUserId,
        SnapshotJson = JsonSerializer.Serialize(new
        {
            document.PublicId, document.DocumentFamilyId, document.VersionNumber, document.Title, document.IsCurrent,
            document.IsActive, document.IsApproved, document.IsPublished, document.DocumentDate, document.PublicationDate
        })
    };

    private static bool ContentReady(StrategicDocument document) => document.Blob == null
        ? document.ExternalUrl != null && document.DocumentType.AllowsExternalLinks
        : document.Blob.SignatureVerified && !document.Blob.IsQuarantined && document.Blob.ScanStatus == "Clean" && !document.Blob.IsContentDeleted;

    private static StrategicDocumentTypeResponse ToResponse(StrategicDocumentType item) => new(
        item.PublicId, item.Code, item.Name, item.Description, item.AllowsExternalLinks, item.IsActive, item.DisplayOrder, Convert.ToBase64String(item.RowVersion));

    private static StrategicDocumentResponse ToResponse(StrategicDocument item, bool includeAdministration) => new(
        item.PublicId, item.DocumentFamilyId, item.PreviousVersion?.PublicId, item.VersionNumber,
        item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code, item.MunicipalityFinancialYear.FinancialYear.Name,
        item.DocumentType.PublicId, item.DocumentType.Code, item.DocumentType.Name, item.SdbipLayer, item.Title, item.Description,
        item.DocumentDate, item.DisplayOrder, item.IsCurrent, item.IsActive, item.IsApproved, item.ApprovedAt, item.ApprovedByUserId,
        item.ApprovalReference, item.IsPublished, item.PublicationDate, item.PublishedAt, item.PublishedByUserId, item.CreatedAt,
        item.CreatedByUserId, item.FileName, item.Blob?.ContentType, item.Blob?.SizeInBytes, item.Blob?.Sha256, item.Blob?.ScanStatus,
        item.Blob?.IsQuarantined ?? false, item.ExternalUrl, item.Blob == null ? null : $"/api/v1/strategic-documents/{item.PublicId}/content",
        Convert.ToBase64String(item.RowVersion), includeAdministration ? item.Events.OrderBy(eventItem => eventItem.OccurredAt)
            .Select(eventItem => new StrategicDocumentEventResponse(eventItem.PublicId, eventItem.Action.ToString(), eventItem.Reason, eventItem.ActorUserId, eventItem.OccurredAt)).ToArray() : []);

    private static bool TryNormalizeType(SaveStrategicDocumentTypeRequest request, out NormalizedType value, out string? error)
    {
        value = default!;
        error = null;
        var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length is 0 or > 80 || code.Any(character => !char.IsLetterOrDigit(character) && character is not '_' and not '-' and not '.'))
        { error = "Code must contain 1-80 letters, numbers, underscores, hyphens, or periods."; return false; }
        if (!TryText(request.Name, "Name", 160, out var name, out error)
            || !TryOptional(request.Description, "Description", 1000, out var description, out error)
            || !TryReason(request.Reason, out var reason, out error)) return false;
        if (request.DisplayOrder < 0) { error = "DisplayOrder cannot be negative."; return false; }
        value = new(code, name, description, reason);
        return true;
    }

    private static bool TryNormalizeDocument(CreateStrategicDocumentVersionRequest request, out NormalizedDocument value, out string? error)
    {
        value = default!;
        error = null;
        if (!TryText(request.Title, "Title", 240, out var title, out error)
            || !TryOptional(request.Description, "Description", 4000, out var description, out error)
            || !TryOptional(request.SdbipLayer, "SdbipLayer", 120, out var layer, out error)
            || !TryReason(request.Reason, out var reason, out error)) return false;
        if (request.DocumentDate == default) { error = "DocumentDate is required."; return false; }
        if (request.DisplayOrder < 0) { error = "DisplayOrder cannot be negative."; return false; }
        string? externalUrl = null;
        if (!string.IsNullOrWhiteSpace(request.ExternalUrl))
        {
            var candidate = request.ExternalUrl.Trim();
            if (candidate.Length > 2048 || !Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            { error = "ExternalUrl must be an absolute HTTPS URL without embedded credentials."; return false; }
            externalUrl = uri.AbsoluteUri;
        }
        value = new(title, description, layer, NormalizeUtc(request.DocumentDate), externalUrl, reason);
        return true;
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static bool TryText(string? value, string field, int maximum, out string normalized, out string? error)
    {
        normalized = value?.Trim() ?? string.Empty;
        error = normalized.Length is 0 || normalized.Length > maximum ? $"{field} must contain between 1 and {maximum} characters." : null;
        return error == null;
    }

    private static bool TryOptional(string? value, string field, int maximum, out string? normalized, out string? error)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        error = normalized?.Length > maximum ? $"{field} cannot exceed {maximum} characters." : null;
        return error == null;
    }

    private static bool TryReason(string? value, out string normalized, out string? error) => TryText(value, "Reason", 1000, out normalized, out error);

    private static bool TryVersion(string? value, out byte[] version)
    {
        version = [];
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { version = Convert.FromBase64String(value); return version.Length > 0; }
        catch (FormatException) { return false; }
    }

    private ActionResult<ApiResponse<T>> Forbidden<T>(string reason) => StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<T>(false, default, reason));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);

    private sealed record NormalizedType(string Code, string Name, string? Description, string Reason);
    private sealed record NormalizedDocument(string Title, string? Description, string? SdbipLayer, DateTime DocumentDate, string? ExternalUrl, string Reason);
}

using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Reporting;
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
[Route("api/idp")]
[Authorize]
public class IdpController : ControllerBase
{
    private const long MaximumDocumentBytes = 25 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> AllowedDocumentTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;
    private readonly ITenantContext? _tenantContext;
    private readonly IEvidenceBlobStorage? _evidenceStorage;
    private readonly IEvidenceInspectionService? _evidenceInspection;
    private readonly IEvidenceMalwareScanner? _malwareScanner;
    private readonly IAccessControlService? _accessControl;

    public IdpController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWorkflowGovernanceService workflowGovernanceService,
        ITenantContext? tenantContext = null,
        IEvidenceBlobStorage? evidenceStorage = null,
        IEvidenceInspectionService? evidenceInspection = null,
        IEvidenceMalwareScanner? malwareScanner = null,
        IAccessControlService? accessControl = null)
    {
        _context = context;
        _userManager = userManager;
        _workflowGovernanceService = workflowGovernanceService;
        _tenantContext = tenantContext;
        _evidenceStorage = evidenceStorage;
        _evidenceInspection = evidenceInspection;
        _malwareScanner = malwareScanner;
        _accessControl = accessControl;
    }

    [HttpGet("plans")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public ActionResult<ApiResponse<IdpPlanSummaryResponse[]>> GetPlans() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpPlanSummaryResponse[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/idp/plans/page."));

    [HttpGet("plans/page")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public ActionResult<ApiResponse<PagedResponse<IdpPlanSummaryResponse>>> GetPlansPage([FromQuery] PagedQueryRequest request)
    {
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<PagedResponse<IdpPlanSummaryResponse>>(false, null,
            "This unversioned plan register is retired. Use /api/v1/idp/plans/page."));
    }

    [HttpGet("~/api/v1/idp/plans/page")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpPlanSummaryResponse>>>> GetPlansPageV1([FromQuery] PagedQueryRequest request)
    {
        if (request.NormalizedSortBy is not ("createdat" or "plancode" or "plantitle" or "status" or "effectivefrom" or "startfinancialyear"))
            return BadRequest(new ApiResponse<PagedResponse<IdpPlanSummaryResponse>>(false, null,
                "SortBy must be createdAt, planCode, planTitle, status, effectiveFrom, or startFinancialYear."));

        IQueryable<IdpPlan> query = _context.IdpPlans.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.NormalizedSearch))
        {
            var search = request.NormalizedSearch;
            query = query.Where(plan =>
                plan.PlanCode.Contains(search) ||
                plan.PlanTitle.Contains(search) ||
                plan.MunicipalityName.Contains(search) ||
                (plan.PublicationReference != null && plan.PublicationReference.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("plancode", false) => query.OrderBy(plan => plan.PlanCode).ThenBy(plan => plan.Id),
            ("plancode", true) => query.OrderByDescending(plan => plan.PlanCode).ThenByDescending(plan => plan.Id),
            ("plantitle", false) => query.OrderBy(plan => plan.PlanTitle).ThenBy(plan => plan.Id),
            ("plantitle", true) => query.OrderByDescending(plan => plan.PlanTitle).ThenByDescending(plan => plan.Id),
            ("status", false) => query.OrderBy(plan => plan.Status).ThenBy(plan => plan.Id),
            ("status", true) => query.OrderByDescending(plan => plan.Status).ThenByDescending(plan => plan.Id),
            ("effectivefrom", false) => query.OrderBy(plan => plan.EffectiveFrom).ThenBy(plan => plan.Id),
            ("effectivefrom", true) => query.OrderByDescending(plan => plan.EffectiveFrom).ThenByDescending(plan => plan.Id),
            ("startfinancialyear", false) => query.OrderBy(plan => plan.StartFinancialYear).ThenBy(plan => plan.Id),
            ("startfinancialyear", true) => query.OrderByDescending(plan => plan.StartFinancialYear).ThenByDescending(plan => plan.Id),
            (_, false) => query.OrderBy(plan => plan.CreatedAt).ThenBy(plan => plan.Id),
            _ => query.OrderByDescending(plan => plan.CreatedAt).ThenByDescending(plan => plan.Id)
        };

        var rows = await query
            .Include(plan => plan.PredecessorPlan)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .ToArrayAsync();

        return Ok(new ApiResponse<PagedResponse<IdpPlanSummaryResponse>>(true,
            PagedResponse<IdpPlanSummaryResponse>.Create(rows.Select(ToSummaryResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("plans")]
    [Authorize(Policy = "Permission:IDP.Plan.Manage")]
    public ActionResult<ApiResponse<IdpPlanSummaryResponse>> CreatePlan([FromBody] CreateIdpPlanRequest request)
    {
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpPlanSummaryResponse>(false, null,
            "This unversioned plan route is retired. Use POST /api/v1/idp/plans."));
    }

    [HttpPost("~/api/v1/idp/plans")]
    [Authorize(Policy = "Permission:IDP.Plan.Manage")]
    public async Task<ActionResult<ApiResponse<IdpPlanSummaryResponse>>> CreatePlanV1([FromBody] CreateIdpPlanRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new ApiResponse<IdpPlanSummaryResponse>(false, null, "User not found"));
        }

        var municipalityName = request.MunicipalityName.Trim();
        if (_tenantContext?.MunicipalityId is > 0 and not long.MinValue)
        {
            municipalityName = await _context.Municipalities.Where(x => x.Id == _tenantContext.MunicipalityId.Value && x.IsActive).Select(x => x.Name).SingleOrDefaultAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(municipalityName)) return Conflict(new ApiResponse<IdpPlanSummaryResponse>(false, null, "Selected municipality is not active."));
        }

        var effectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow;
        if (request.EndFinancialYear < request.StartFinancialYear || request.EffectiveTo.HasValue && request.EffectiveTo <= effectiveFrom)
            return BadRequest(new ApiResponse<IdpPlanSummaryResponse>(false, null, "The financial-year and effective-date ranges must end after they begin."));

        IdpPlan? predecessor = null;
        if (request.PredecessorPlanPublicId.HasValue)
        {
            predecessor = await _context.IdpPlans.FirstOrDefaultAsync(plan => plan.PublicId == request.PredecessorPlanPublicId.Value);
            if (predecessor == null) return BadRequest(new ApiResponse<IdpPlanSummaryResponse>(false, null, "The predecessor plan was not found in the selected municipality."));
            if (effectiveFrom < predecessor.EffectiveFrom) return BadRequest(new ApiResponse<IdpPlanSummaryResponse>(false, null, "A successor plan cannot become effective before its predecessor."));
        }

        var entity = new IdpPlan
        {
            PlanFamilyId = predecessor?.PlanFamilyId ?? Guid.NewGuid(),
            PredecessorPlan = predecessor,
            MunicipalityName = municipalityName,
            PlanTitle = request.PlanTitle.Trim(),
            PlanCode = request.PlanCode.Trim(),
            StartFinancialYear = request.StartFinancialYear,
            EndFinancialYear = request.EndFinancialYear,
            Status = IdpPlanStatus.Draft,
            CurrentVersionNumber = 1,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = request.EffectiveTo,
            PublicationReference = NormalizeOptional(request.PublicationReference),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        };

        var version = new IdpPlanVersion
        {
            IdpPlan = entity,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original Approved IDP",
            ReviewYear = null,
            SummaryOfChanges = "Original approved five-year IDP version.",
            EffectiveFrom = effectiveFrom,
            PublicationReference = NormalizeOptional(request.PublicationReference),
            PublishedAt = string.IsNullOrWhiteSpace(request.PublicationReference) ? null : DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            IsActive = true
        };

        _context.IdpPlans.Add(entity);
        _context.IdpPlanVersions.Add(version);
        _workflowGovernanceService.QueueAuditTrail(
            "IdpPlan",
            entity.PublicId.ToString(),
            "Create",
            null,
            ToSummaryResponse(entity),
            user.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<IdpPlanSummaryResponse>(true, ToSummaryResponse(entity)));
    }

    [HttpPut("plans/{id:int}")]
    [Authorize(Policy = "Permission:IDP.Plan.Manage")]
    public ActionResult<ApiResponse<IdpPlanSummaryResponse>> UpdatePlan(int id, [FromBody] UpdateIdpPlanRequest request)
    {
        _ = id;
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpPlanSummaryResponse>(false, null,
            "This numeric-ID plan route is retired. Use PUT /api/v1/idp/plans/{planPublicId}."));
    }

    [HttpPut("~/api/v1/idp/plans/{planPublicId:guid}")]
    [Authorize(Policy = "Permission:IDP.Plan.Manage")]
    public async Task<ActionResult<ApiResponse<IdpPlanSummaryResponse>>> UpdatePlanByPublicId(Guid planPublicId, [FromBody] UpdateIdpPlanRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new ApiResponse<IdpPlanSummaryResponse>(false, null, "User not found"));
        }

        var entity = await _context.IdpPlans.Include(plan => plan.PredecessorPlan).FirstOrDefaultAsync(plan => plan.PublicId == planPublicId);
        if (entity == null)
        {
            return NotFound(new ApiResponse<IdpPlanSummaryResponse>(false, null, "IDP plan not found"));
        }

        var before = ToSummaryResponse(entity);
        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            try { _context.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
            catch (FormatException) { return BadRequest(new ApiResponse<IdpPlanSummaryResponse>(false, null, "Invalid RowVersion.")); }
        }
        entity.PlanTitle = request.PlanTitle.Trim();
        var effectiveFrom = request.EffectiveFrom ?? entity.EffectiveFrom;
        var effectiveTo = request.EffectiveTo;
        if (request.EndFinancialYear < request.StartFinancialYear || effectiveTo.HasValue && effectiveTo <= effectiveFrom)
            return BadRequest(new ApiResponse<IdpPlanSummaryResponse>(false, null, "The financial-year and effective-date ranges must end after they begin."));
        entity.StartFinancialYear = request.StartFinancialYear;
        entity.EndFinancialYear = request.EndFinancialYear;
        entity.EffectiveFrom = effectiveFrom;
        entity.EffectiveTo = effectiveTo;
        entity.PublicationReference = NormalizeOptional(request.PublicationReference) ?? entity.PublicationReference;
        if (TryParseEnum(request.Status, out IdpPlanStatus status))
        {
            if (status == IdpPlanStatus.Published && string.IsNullOrWhiteSpace(entity.PublicationReference))
                return BadRequest(new ApiResponse<IdpPlanSummaryResponse>(false, null, "A publication reference is required before publishing an IDP plan."));
            entity.Status = status;
            if (status == IdpPlanStatus.Approved || status == IdpPlanStatus.Published)
            {
                entity.ApprovedAt = DateTime.UtcNow;
                entity.ApprovedByUserId = user.Id;
            }
            if (status == IdpPlanStatus.Published) entity.PublishedAt ??= DateTime.UtcNow;
        }

        _workflowGovernanceService.QueueAuditTrail(
            "IdpPlan",
            entity.PublicId.ToString(),
            "Update",
            before,
            ToSummaryResponse(entity),
            user.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<IdpPlanSummaryResponse>(false, null, "IDP plan was changed by another user.")); }

        return Ok(new ApiResponse<IdpPlanSummaryResponse>(true, ToSummaryResponse(entity)));
    }

    [HttpPost("plans/{id:int}/versions")]
    [Authorize(Policy = "Permission:IDP.Version.Manage")]
    public ActionResult<ApiResponse<IdpPlanVersionResponse>> CreatePlanVersion(int id, [FromBody] CreateIdpPlanVersionRequest request)
    {
        _ = id;
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpPlanVersionResponse>(false, null,
            "This numeric-ID version route is retired. Use POST /api/v1/idp/plans/{planPublicId}/versions."));
    }

    [HttpPost("~/api/v1/idp/plans/{planPublicId:guid}/versions")]
    [Authorize(Policy = "Permission:IDP.Version.Manage")]
    public async Task<ActionResult<ApiResponse<IdpPlanVersionResponse>>> CreatePlanVersionByPublicId(Guid planPublicId, [FromBody] CreateIdpPlanVersionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new ApiResponse<IdpPlanVersionResponse>(false, null, "User not found"));
        }

        var plan = await _context.IdpPlans.FirstOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpPlanVersionResponse>(false, null, "IDP plan not found"));
        }

        var versionScope = Scope(targetId: plan.PublicId);
        if (!string.IsNullOrWhiteSpace(request.SummaryOfChanges)
            && await MemberUpdateDenialAsync(user, "IDP_PLAN", versionScope, ["VersionSummary"]) is not null)
            return Forbid();

        if (!TryParseEnum(request.VersionType, out IdpVersionType versionType))
        {
            return BadRequest(new ApiResponse<IdpPlanVersionResponse>(false, null, "Invalid version type"));
        }

        var effectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow;
        if (effectiveFrom < plan.EffectiveFrom || plan.EffectiveTo.HasValue && effectiveFrom >= plan.EffectiveTo)
            return BadRequest(new ApiResponse<IdpPlanVersionResponse>(false, null, "The version effective date must fall within the plan effective period."));
        var previousActive = await _context.IdpPlanVersions.Where(item => item.IdpPlanId == plan.Id && item.IsActive).OrderByDescending(item => item.VersionNumber).ToListAsync();
        var predecessor = previousActive.FirstOrDefault();
        if (predecessor != null && effectiveFrom <= predecessor.EffectiveFrom)
            return BadRequest(new ApiResponse<IdpPlanVersionResponse>(false, null, "A successor version must become effective after its predecessor."));

        var nextVersion = plan.CurrentVersionNumber + 1;
        var entity = new IdpPlanVersion
        {
            IdpPlanId = plan.Id,
            PredecessorVersion = predecessor,
            VersionNumber = nextVersion,
            VersionType = versionType,
            VersionLabel = request.VersionLabel.Trim(),
            ReviewYear = request.ReviewYear,
            SummaryOfChanges = request.SummaryOfChanges,
            EffectiveFrom = effectiveFrom,
            PublicationReference = NormalizeOptional(request.PublicationReference),
            PublishedAt = string.IsNullOrWhiteSpace(request.PublicationReference) ? null : DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            CreatedByUser = user
        };

        foreach (var existing in previousActive)
        {
            existing.IsActive = false;
            existing.EffectiveTo = effectiveFrom;
        }

        _context.IdpPlanVersions.Add(entity);
        plan.CurrentVersionNumber = nextVersion;
        await _context.SaveChangesAsync();

        await _workflowGovernanceService.WriteAuditTrailAsync(
            "IdpPlanVersion",
            entity.Id.ToString(),
            "Create",
            null,
            ToVersionResponse(entity, IdpPlanVersionMemberAccess.Full),
            user.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpPlanVersionResponse>(true,
            ToVersionResponse(entity, await GetPlanVersionMemberAccessAsync(user, versionScope))));
    }

    [HttpGet("plans/{id:int}/hierarchy")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public ActionResult<ApiResponse<object>> GetHierarchy(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null,
            "This unbounded numeric-ID hierarchy route is retired. Use /api/v1/idp/plans/{planPublicId}/hierarchy-paths/page and /api/v1/idp/plans/{planPublicId}/versions/page."));

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/versions/page")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpPlanVersionResponse>>>> GetPlanVersionsPage(
        Guid planPublicId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool? active = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized(new ApiResponse<PagedResponse<IdpPlanVersionResponse>>(false, null, "User not found"));

        if (request.NormalizedSortBy is not ("createdat" or "versionnumber" or "versionlabel" or "versiontype" or "effectivefrom"))
            return BadRequest(new ApiResponse<PagedResponse<IdpPlanVersionResponse>>(false, null,
                "SortBy must be createdAt, versionNumber, versionLabel, versionType, or effectiveFrom."));

        var planId = await _context.IdpPlans.AsNoTracking()
            .Where(plan => plan.PublicId == planPublicId)
            .Select(plan => (int?)plan.Id)
            .SingleOrDefaultAsync();
        if (!planId.HasValue)
            return NotFound(new ApiResponse<PagedResponse<IdpPlanVersionResponse>>(false, null, "IDP plan not found."));

        var memberAccess = await GetPlanVersionMemberAccessAsync(user, Scope(targetId: planPublicId));

        IQueryable<IdpPlanVersion> query = _context.IdpPlanVersions.AsNoTracking()
            .Where(version => version.IdpPlanId == planId.Value);
        if (active.HasValue) query = query.Where(version => version.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(request.NormalizedSearch))
        {
            var search = request.NormalizedSearch;
            query = query.Where(version =>
                version.VersionLabel.Contains(search) ||
                (version.ReviewYear != null && version.ReviewYear.Contains(search)) ||
                (memberAccess.Summary && version.SummaryOfChanges != null && version.SummaryOfChanges.Contains(search)) ||
                (version.PublicationReference != null && version.PublicationReference.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("versionnumber", false) => query.OrderBy(version => version.VersionNumber).ThenBy(version => version.Id),
            ("versionnumber", true) => query.OrderByDescending(version => version.VersionNumber).ThenByDescending(version => version.Id),
            ("versionlabel", false) => query.OrderBy(version => version.VersionLabel).ThenBy(version => version.Id),
            ("versionlabel", true) => query.OrderByDescending(version => version.VersionLabel).ThenByDescending(version => version.Id),
            ("versiontype", false) => query.OrderBy(version => version.VersionType).ThenBy(version => version.Id),
            ("versiontype", true) => query.OrderByDescending(version => version.VersionType).ThenByDescending(version => version.Id),
            ("effectivefrom", false) => query.OrderBy(version => version.EffectiveFrom).ThenBy(version => version.Id),
            ("effectivefrom", true) => query.OrderByDescending(version => version.EffectiveFrom).ThenByDescending(version => version.Id),
            (_, false) => query.OrderBy(version => version.CreatedAt).ThenBy(version => version.Id),
            _ => query.OrderByDescending(version => version.CreatedAt).ThenByDescending(version => version.Id)
        };

        var rows = await query.Include(version => version.PredecessorVersion)
            .Include(version => version.CreatedByUser)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<IdpPlanVersionResponse>>(true,
            PagedResponse<IdpPlanVersionResponse>.Create(rows.Select(version => ToVersionResponse(version, memberAccess)), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/hierarchy-paths/page")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpHierarchyPathResponse>>>> GetHierarchyPathsPage(
        Guid planPublicId,
        [FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized(new ApiResponse<PagedResponse<IdpHierarchyPathResponse>>(false, null, "User not found"));

        if (request.NormalizedSortBy is not ("createdat" or "outcome" or "objective" or "priority" or "programme" or "project" or "kpi"))
            return BadRequest(new ApiResponse<PagedResponse<IdpHierarchyPathResponse>>(false, null,
                "SortBy must be outcome, objective, priority, programme, project, or kpi."));

        var planId = await _context.IdpPlans.AsNoTracking()
            .Where(plan => plan.PublicId == planPublicId)
            .Select(plan => (int?)plan.Id)
            .SingleOrDefaultAsync();
        if (!planId.HasValue)
            return NotFound(new ApiResponse<PagedResponse<IdpHierarchyPathResponse>>(false, null, "IDP plan not found."));

        IQueryable<IdpKpi> query = _context.IdpKpis.AsNoTracking()
            .Where(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == planId.Value);
        if (!string.IsNullOrWhiteSpace(request.NormalizedSearch))
        {
            var search = request.NormalizedSearch;
            query = query.Where(kpi =>
                kpi.KpiCode.Contains(search) || kpi.KpiName.Contains(search) ||
                kpi.IdpProject.ProjectCode.Contains(search) || kpi.IdpProject.ProjectName.Contains(search) ||
                kpi.IdpProject.IdpProgramme.ProgrammeCode.Contains(search) || kpi.IdpProject.IdpProgramme.Name.Contains(search) ||
                kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.PriorityCode.Contains(search) || kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.Name.Contains(search) ||
                kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Code.Contains(search) || kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Name.Contains(search) ||
                kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.Code.Contains(search) ||
                kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("objective", false) => query.OrderBy(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Code).ThenBy(kpi => kpi.KpiCode).ThenBy(kpi => kpi.Id),
            ("objective", true) => query.OrderByDescending(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Code).ThenByDescending(kpi => kpi.KpiCode).ThenByDescending(kpi => kpi.Id),
            ("priority", false) => query.OrderBy(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.PriorityCode).ThenBy(kpi => kpi.KpiCode).ThenBy(kpi => kpi.Id),
            ("priority", true) => query.OrderByDescending(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.PriorityCode).ThenByDescending(kpi => kpi.KpiCode).ThenByDescending(kpi => kpi.Id),
            ("programme", false) => query.OrderBy(kpi => kpi.IdpProject.IdpProgramme.ProgrammeCode).ThenBy(kpi => kpi.KpiCode).ThenBy(kpi => kpi.Id),
            ("programme", true) => query.OrderByDescending(kpi => kpi.IdpProject.IdpProgramme.ProgrammeCode).ThenByDescending(kpi => kpi.KpiCode).ThenByDescending(kpi => kpi.Id),
            ("project", false) => query.OrderBy(kpi => kpi.IdpProject.ProjectCode).ThenBy(kpi => kpi.KpiCode).ThenBy(kpi => kpi.Id),
            ("project", true) => query.OrderByDescending(kpi => kpi.IdpProject.ProjectCode).ThenByDescending(kpi => kpi.KpiCode).ThenByDescending(kpi => kpi.Id),
            ("kpi", false) => query.OrderBy(kpi => kpi.KpiCode).ThenBy(kpi => kpi.Id),
            ("kpi", true) => query.OrderByDescending(kpi => kpi.KpiCode).ThenByDescending(kpi => kpi.Id),
            (_, false) => query.OrderBy(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.SortOrder)
                .ThenBy(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.SortOrder)
                .ThenBy(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.SortOrder)
                .ThenBy(kpi => kpi.KpiCode).ThenBy(kpi => kpi.Id),
            _ => query.OrderByDescending(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.SortOrder)
                .ThenByDescending(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.SortOrder)
                .ThenByDescending(kpi => kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.SortOrder)
                .ThenByDescending(kpi => kpi.KpiCode).ThenByDescending(kpi => kpi.Id)
        };

        var rows = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(kpi => new IdpHierarchyPathRow
            {
                IdpPlanPublicId = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.PublicId,
                OutcomePublicId = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.PublicId,
                OutcomeCode = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.Code,
                OutcomeName = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.Name,
                ObjectivePublicId = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.PublicId,
                ObjectiveCode = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Code,
                ObjectiveName = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Name,
                ObjectiveStrategicOwnerPublicId = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.StrategicOwnerUser == null
                    ? null : kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.StrategicOwnerUser!.PublicId,
                ObjectiveStrategicOwnerName = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.StrategicOwnerUser == null
                    ? null : kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.StrategicOwnerUser!.FirstName + " " + kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.StrategicOwnerUser!.LastName,
                ObjectiveBudgetAllocation = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.BudgetAllocation,
                PriorityPublicId = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.PublicId,
                PriorityCode = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.PriorityCode,
                PriorityName = kpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.Name,
                ProgrammePublicId = kpi.IdpProject.IdpProgramme.PublicId,
                ProgrammeCode = kpi.IdpProject.IdpProgramme.ProgrammeCode,
                ProgrammeName = kpi.IdpProject.IdpProgramme.Name,
                ProgrammePlannedBudget = kpi.IdpProject.IdpProgramme.PlannedBudget,
                ProgrammeApprovedBudget = kpi.IdpProject.IdpProgramme.ApprovedBudget,
                ProgrammeActualExpenditure = kpi.IdpProject.IdpProgramme.ActualExpenditure,
                ProjectPublicId = kpi.IdpProject.PublicId,
                ProjectCode = kpi.IdpProject.ProjectCode,
                ProjectName = kpi.IdpProject.ProjectName,
                ProjectBudget = kpi.IdpProject.Budget,
                ProjectFundingSource = kpi.IdpProject.FundingSource,
                KpiPublicId = kpi.PublicId,
                KpiCode = kpi.KpiCode,
                KpiName = kpi.KpiName
            }).ToArrayAsync();

        var planScope = Scope(targetId: planPublicId);
        var objectiveAccess = await GetObjectiveMemberAccessAsync(user, planScope);
        var programmeAccess = await GetProgrammeMemberAccessAsync(user, planScope);
        var projectAccessById = new Dictionary<Guid, IdpProjectMemberAccess>();
        var responses = new List<IdpHierarchyPathResponse>(rows.Length);
        foreach (var row in rows)
        {
            if (!projectAccessById.TryGetValue(row.ProjectPublicId, out var projectAccess))
            {
                projectAccess = await GetProjectMemberAccessAsync(user,
                    Scope(targetId: row.IdpPlanPublicId, projectId: row.ProjectPublicId));
                projectAccessById[row.ProjectPublicId] = projectAccess;
            }
            responses.Add(row.ToResponse(objectiveAccess, programmeAccess, projectAccess));
        }

        return Ok(new ApiResponse<PagedResponse<IdpHierarchyPathResponse>>(true,
            PagedResponse<IdpHierarchyPathResponse>.Create(responses, request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("plans/{id:int}/dashboard")]
    [Authorize(Policy = "Permission:IDP.Dashboard.View")]
    public ActionResult<ApiResponse<IdpDashboardResponse>> GetDashboard(int id)
    {
        _ = id;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpDashboardResponse>(false, null,
            "This numeric-ID dashboard route is retired. Use GET /api/v1/idp/plans/{planPublicId}/dashboard."));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/dashboard")]
    [Authorize(Policy = "Permission:IDP.Dashboard.View")]
    public async Task<ActionResult<ApiResponse<IdpDashboardResponse>>> GetDashboardByPublicId(Guid planPublicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpDashboardResponse>(false, null, "User not found"));
        var plan = await _context.IdpPlans.AsNoTracking().FirstOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpDashboardResponse>(false, null, "IDP plan not found"));
        }
        var id = plan.Id;

        var outcomes = await _context.IdpStrategicOutcomes.CountAsync(item => item.IdpPlanId == id);

        var objectiveIds = await _context.IdpStrategicObjectives
            .Where(item => item.IdpStrategicOutcome.IdpPlanId == id)
            .Select(item => item.Id)
            .ToArrayAsync();

        var projectIds = await _context.IdpProjects
            .Where(item => item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == id)
            .Select(item => item.Id)
            .ToArrayAsync();

        var kpiIds = await _context.IdpKpis
            .Where(item => projectIds.Contains(item.IdpProjectId))
            .Select(item => item.Id)
            .ToArrayAsync();

        var annualAccess = await GetAnnualTargetMemberAccessAsync(user);
        decimal? kpiAchievementRate = null;
        if (annualAccess.TargetValue && annualAccess.ActualValue)
        {
            var annualTargets = await _context.IdpAnnualTargets
                .Where(item => kpiIds.Contains(item.IdpKpiId))
                .Select(item => new { item.TargetValue, item.ActualValue })
                .ToArrayAsync();
            var achievedCount = annualTargets.Count(item => item.ActualValue.HasValue && item.ActualValue.Value >= item.TargetValue);
            kpiAchievementRate = annualTargets.Length == 0 ? 0m : decimal.Round((decimal)achievedCount / annualTargets.Length * 100m, 2);
        }

        var budgetAccess = await GetBudgetMemberAccessAsync(user);
        var budgetQuery = _context.IdpBudgetSnapshots
            .Where(item => (item.IdpStrategicObjectiveId.HasValue && objectiveIds.Contains(item.IdpStrategicObjectiveId.Value))
                || (item.IdpProjectId.HasValue && projectIds.Contains(item.IdpProjectId.Value)));
        decimal? plannedBudget = budgetAccess.PlannedBudget ? await budgetQuery.SumAsync(item => item.PlannedBudget) : null;
        decimal? approvedBudget = budgetAccess.ApprovedBudget ? await budgetQuery.SumAsync(item => item.ApprovedBudget) : null;
        decimal? actualExpenditure = budgetAccess.ActualExpenditure ? await budgetQuery.SumAsync(item => item.ActualExpenditure) : null;

        var topRiskTitles = await _context.IdpRiskLinks
            .Where(item => (item.IdpStrategicObjectiveId.HasValue && objectiveIds.Contains(item.IdpStrategicObjectiveId.Value))
                || (item.IdpProjectId.HasValue && projectIds.Contains(item.IdpProjectId.Value))
                || (item.IdpKpiId.HasValue && kpiIds.Contains(item.IdpKpiId.Value)))
            .OrderByDescending(item => item.RiskLevel)
            .Select(item => item.RiskTitle)
            .Distinct()
            .Take(5)
            .ToArrayAsync();

        var sessions = await _context.IdpCommunitySessions
            .AsNoTracking()
            .Where(item => item.IdpPlanId == id && item.WardId.HasValue)
            .Select(item => new
            {
                item.Id,
                item.WardId,
                WardName = item.Ward != null ? item.Ward.Name : "Unknown Ward",
                item.ParticipantsCount
            })
            .ToListAsync();

        var sessionIds = sessions.Select(item => item.Id).ToArray();
        var needsBySession = await _context.IdpCommunityNeeds
            .AsNoTracking()
            .Where(item => sessionIds.Contains(item.IdpCommunitySessionId))
            .GroupBy(item => item.IdpCommunitySessionId)
            .Select(group => new { SessionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.SessionId, item => item.Count);

        var wardParticipation = sessions
            .GroupBy(item => new { item.WardId, item.WardName })
            .Select(group => new IdpWardParticipationResponse(
                group.Key.WardId ?? 0,
                group.Key.WardName,
                group.Count(),
                group.Sum(item => item.ParticipantsCount),
                group.Sum(item => needsBySession.TryGetValue(item.Id, out var count) ? count : 0)))
            .OrderByDescending(item => item.ParticipantsCount)
            .ToArray();

        var alignmentCount = await _context.IdpAlignmentLinks
            .CountAsync(item => item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == id);

        var response = new IdpDashboardResponse(
            plan.PublicId,
            plan.PlanTitle,
            outcomes,
            objectiveIds.Length,
            projectIds.Length,
            kpiIds.Length,
            await _context.IdpCommunitySessions.CountAsync(item => item.IdpPlanId == id),
            topRiskTitles.Length,
            plannedBudget,
            approvedBudget,
            actualExpenditure,
            kpiAchievementRate,
            topRiskTitles,
            wardParticipation,
            alignmentCount);

        return Ok(new ApiResponse<IdpDashboardResponse>(true, response));
    }

    [HttpGet("plans/{id:int}/alignment-matrix")]
    [Authorize(Policy = "Permission:IDP.Alignment.View")]
    public ActionResult<ApiResponse<IdpAlignmentMatrixItemResponse[]>> GetAlignmentMatrix(int id)
    {
        _ = id;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpAlignmentMatrixItemResponse[]>(false, null,
            "This unbounded numeric-ID route is retired. Use /api/v1/idp/plans/{planPublicId}/alignment-matrix/page."));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/alignment-matrix/page")]
    [Authorize(Policy = "Permission:IDP.Alignment.View")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>>> GetAlignmentMatrixPage(
        Guid planPublicId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? frameworkType = null)
    {
        var sortBy = request.SortBy == null ? "objective" : request.NormalizedSortBy;
        if (sortBy is not ("outcome" or "objective" or "framework" or "reference"))
            return BadRequest(new ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>(false, null,
                "SortBy must be outcome, objective, framework, or reference."));
        AlignmentFrameworkType? frameworkFilter = null;
        if (!string.IsNullOrWhiteSpace(frameworkType))
        {
            if (!TryParseEnum(frameworkType, out AlignmentFrameworkType parsedFramework))
                return BadRequest(new ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>(false, null,
                    "FrameworkType is invalid."));
            frameworkFilter = parsedFramework;
        }
        if (!await _context.IdpPlans.AsNoTracking().AnyAsync(item => item.PublicId == planPublicId))
            return NotFound(new ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>(false, null, "IDP plan not found."));

        var query = _context.IdpAlignmentLinks.AsNoTracking()
            .Where(item => item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.PublicId == planPublicId);
        if (frameworkFilter.HasValue) query = query.Where(item => item.FrameworkType == frameworkFilter.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(item => item.IdpStrategicObjective.IdpStrategicOutcome.Code.Contains(search)
                || item.IdpStrategicObjective.IdpStrategicOutcome.Name.Contains(search)
                || item.IdpStrategicObjective.Code.Contains(search)
                || item.IdpStrategicObjective.Name.Contains(search)
                || item.FrameworkReferenceCode.Contains(search)
                || item.FrameworkReferenceTitle.Contains(search));
        }

        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("outcome", false) => query.OrderBy(item => item.IdpStrategicObjective.IdpStrategicOutcome.Code).ThenBy(item => item.Id),
            ("outcome", true) => query.OrderByDescending(item => item.IdpStrategicObjective.IdpStrategicOutcome.Code).ThenBy(item => item.Id),
            ("framework", false) => query.OrderBy(item => item.FrameworkType).ThenBy(item => item.Id),
            ("framework", true) => query.OrderByDescending(item => item.FrameworkType).ThenBy(item => item.Id),
            ("reference", false) => query.OrderBy(item => item.FrameworkReferenceCode).ThenBy(item => item.Id),
            ("reference", true) => query.OrderByDescending(item => item.FrameworkReferenceCode).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.IdpStrategicObjective.Code).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.IdpStrategicObjective.Code).ThenBy(item => item.Id)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(item => new IdpAlignmentMatrixItemResponse(
                item.IdpStrategicObjective.IdpStrategicOutcome.Code,
                item.IdpStrategicObjective.IdpStrategicOutcome.Name,
                item.IdpStrategicObjective.Code,
                item.IdpStrategicObjective.Name,
                item.FrameworkType.ToString(),
                item.FrameworkReferenceCode,
                item.FrameworkReferenceTitle))
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>(true,
            PagedResponse<IdpAlignmentMatrixItemResponse>.Create(rows, request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("plans/{id:int}/reports/{reportType}")]
    [Authorize(Policy = "Permission:IDP.Reports.Generate")]
    public ActionResult<ApiResponse<IdpReportDocumentResponse>> GenerateReport(int id, string reportType, [FromQuery] string format = "pdf")
    {
        _ = id;
        _ = reportType;
        _ = format;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpReportDocumentResponse>(false, null,
            "This numeric-ID report route is retired. Use GET /api/v1/idp/plans/{planPublicId}/reports/{reportType}."));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/reports/{reportType}")]
    [Authorize(Policy = "Permission:IDP.Reports.Generate")]
    public async Task<ActionResult<ApiResponse<IdpReportDocumentResponse>>> GenerateReportByPublicId(Guid planPublicId, string reportType, [FromQuery] string format = "pdf")
    {
        var plan = await _context.IdpPlans.AsNoTracking().FirstOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpReportDocumentResponse>(false, null, "IDP plan not found"));
        }

        var normalizedReportType = (reportType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedReportType is not ("annual" or "five-year" or "ward-based" or "provincial-submission" or "national-submission"))
            return BadRequest(new ApiResponse<IdpReportDocumentResponse>(false, null,
                "ReportType must be annual, five-year, ward-based, provincial-submission, or national-submission."));

        var normalizedFormat = (format ?? "pdf").Trim().ToLowerInvariant();
        var documentFormat = normalizedFormat switch
        {
            "pdf" => OfficialReportFormat.Pdf,
            "xlsx" or "excel" => OfficialReportFormat.Xlsx,
            "docx" or "word" => OfficialReportFormat.Docx,
            _ => (OfficialReportFormat?)null
        };
        if (!documentFormat.HasValue)
            return BadRequest(new ApiResponse<IdpReportDocumentResponse>(false, null, "Format must be pdf, xlsx/excel, or docx/word."));

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpReportDocumentResponse>(false, null, "User not found"));
        var canReadTarget = await CanAccessMemberAsync(user, "IDP_INDICATOR", "AnnualTargetValue", SecurityOperation.Read, Scope());
        var canReadBudget = await CanAccessMemberAsync(user, "IDP_PROJECT", "ProjectBudget", SecurityOperation.Read, Scope());
        var columns = new[]
        {
            new TabularDocumentColumn("outcome", "Strategic Outcome"),
            new TabularDocumentColumn("objective", "Strategic Objective"),
            new TabularDocumentColumn("project", "Project"),
            new TabularDocumentColumn("item", normalizedReportType == "ward-based" ? "Ward Input" : "KPI / Alignment"),
            new TabularDocumentColumn("target", "Target / Reference"),
            new TabularDocumentColumn("budget", "Budget"),
            new TabularDocumentColumn("status", "Status")
        };
        var rows = await BuildIdpReportRows(plan.Id, normalizedReportType, canReadTarget, canReadBudget);

        var reportName = $"{normalizedReportType.ToUpperInvariant()} - {plan.PlanCode}";
        var rendered = OfficialReportRenderer.RenderTable(new TabularDocumentRenderRequest(
            plan.MunicipalityName,
            $"{plan.StartFinancialYear}/{plan.EndFinancialYear}",
            normalizedReportType,
            $"{plan.MunicipalityName} · {plan.PlanTitle} · {normalizedReportType.ToUpperInvariant()}",
            documentFormat.Value,
            columns,
            rows));
        var safePlanCode = string.Concat(plan.PlanCode.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_'));
        var fileName = $"{safePlanCode}_{normalizedReportType}.{rendered.Extension}";

        var response = new IdpReportDocumentResponse(reportName, rendered.ContentType, fileName,
            Convert.ToBase64String(rendered.Content), rendered.Content.LongLength, rendered.Sha256);
        return Ok(new ApiResponse<IdpReportDocumentResponse>(true, response));
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> BuildIdpReportRows(
        int planId, string reportType, bool canReadTarget, bool canReadBudget)
    {
        if (reportType == "ward-based")
        {
            var wardRows = await _context.IdpWardInputs.AsNoTracking()
                .Where(item => item.IdpPlanId == planId)
                .OrderBy(item => item.WardId)
                .Select(item => new { item.WardId, item.WardPlanSummary, item.WardPriorities, item.WardProjects })
                .ToArrayAsync();
            return wardRows.Select(item => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["outcome"] = $"Ward {item.WardId}", ["objective"] = item.WardPriorities,
                ["project"] = item.WardProjects, ["item"] = item.WardPlanSummary,
                ["target"] = string.Empty, ["budget"] = string.Empty, ["status"] = "Captured"
            }).ToArray();
        }

        if (reportType is "provincial-submission" or "national-submission")
        {
            var alignmentRows = await _context.IdpAlignmentLinks.AsNoTracking()
                .Where(item => item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == planId)
                .OrderBy(item => item.IdpStrategicObjective.IdpStrategicOutcome.SortOrder)
                .ThenBy(item => item.IdpStrategicObjective.SortOrder).ThenBy(item => item.FrameworkReferenceCode)
                .Select(item => new
                {
                    Outcome = item.IdpStrategicObjective.IdpStrategicOutcome.Code + " - " + item.IdpStrategicObjective.IdpStrategicOutcome.Name,
                    Objective = item.IdpStrategicObjective.Code + " - " + item.IdpStrategicObjective.Name,
                    Framework = item.FrameworkType.ToString(), item.FrameworkReferenceCode, item.FrameworkReferenceTitle
                }).ToArrayAsync();
            return alignmentRows.Select(item => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["outcome"] = item.Outcome, ["objective"] = item.Objective, ["project"] = item.Framework,
                ["item"] = item.FrameworkReferenceTitle, ["target"] = item.FrameworkReferenceCode,
                ["budget"] = string.Empty, ["status"] = "Aligned"
            }).ToArray();
        }

        var kpiRows = await _context.IdpKpis.AsNoTracking()
            .Where(item => item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == planId)
            .OrderBy(item => item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.SortOrder)
            .ThenBy(item => item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.SortOrder)
            .ThenBy(item => item.KpiCode)
            .Select(item => new
            {
                Outcome = item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.Code + " - " + item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.Name,
                Objective = item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Code + " - " + item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.Name,
                Project = item.IdpProject.ProjectCode + " - " + item.IdpProject.ProjectName,
                Kpi = item.KpiCode + " - " + item.KpiName,
                item.AnnualTarget, item.FiveYearTarget, item.IdpProject.Budget, Status = item.IdpProject.Status.ToString()
            }).ToArrayAsync();
        return kpiRows.Select(item => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["outcome"] = item.Outcome, ["objective"] = item.Objective, ["project"] = item.Project, ["item"] = item.Kpi,
            ["target"] = canReadTarget ? (reportType == "five-year" ? item.FiveYearTarget : item.AnnualTarget).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) : string.Empty,
            ["budget"] = canReadBudget ? item.Budget.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : string.Empty,
            ["status"] = item.Status
        }).ToArray();
    }

    [HttpPost("outcomes")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpStrategicOutcomeResponse>>> CreateOutcome([FromBody] CreateIdpStrategicOutcomeRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpStrategicOutcomeResponse>(false, null, "User not found"));

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpStrategicOutcomeResponse>(false, null, "IDP plan not found"));

        var entity = new IdpStrategicOutcome
        {
            IdpPlanId = request.IdpPlanId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            SortOrder = request.SortOrder
        };

        _context.IdpStrategicOutcomes.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpStrategicOutcome", entity.PublicId.ToString(), "Create", null, ToOutcomeResponse(entity));

        return Ok(new ApiResponse<IdpStrategicOutcomeResponse>(true, ToOutcomeResponse(entity)));
    }

    [HttpPost("objectives")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpStrategicObjectiveResponse>>> CreateObjective([FromBody] CreateIdpStrategicObjectiveRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpStrategicObjectiveResponse>(false, null, "User not found"));

        var outcome = await _context.IdpStrategicOutcomes
            .Include(item => item.IdpPlan)
            .FirstOrDefaultAsync(item => item.Id == request.IdpStrategicOutcomeId);
        if (outcome == null) return NotFound(new ApiResponse<IdpStrategicObjectiveResponse>(false, null, "Strategic outcome not found"));

        var objectiveScope = Scope(targetId: outcome.IdpPlan.PublicId);
        var protectedMembers = string.IsNullOrWhiteSpace(request.StrategicOwnerUserId)
            ? new[] { "ObjectiveBudgetAllocation" }
            : new[] { "ObjectiveStrategicOwner", "ObjectiveBudgetAllocation" };
        if (await MemberUpdateDenialAsync(user, "IDP_PLAN", objectiveScope, protectedMembers) is not null)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(request.StrategicOwnerUserId))
        {
            var strategicOwner = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.StrategicOwnerUserId && item.IsActive);
            if (strategicOwner == null)
                return BadRequest(new ApiResponse<IdpStrategicObjectiveResponse>(false, null, "Strategic owner was not found or is inactive."));
            if (_tenantContext is { IsSystem: false, MunicipalityId: > 0 }
                && strategicOwner.MunicipalityId != _tenantContext.MunicipalityId)
                return BadRequest(new ApiResponse<IdpStrategicObjectiveResponse>(false, null, "Strategic owner must belong to the selected municipality."));
        }

        var entity = new IdpStrategicObjective
        {
            IdpStrategicOutcomeId = request.IdpStrategicOutcomeId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            BaselineValue = request.BaselineValue,
            TargetValue = request.TargetValue,
            ResponsibleDepartmentId = request.ResponsibleDepartmentId,
            StrategicOwnerUserId = request.StrategicOwnerUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BudgetAllocation = request.BudgetAllocation,
            SortOrder = request.SortOrder
        };

        _context.IdpStrategicObjectives.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpStrategicObjectives
            .Include(item => item.ResponsibleDepartment)
            .Include(item => item.StrategicOwnerUser)
            .FirstAsync(item => item.Id == entity.Id);

        await WriteIdpAudit(user.Id, "IdpStrategicObjective", entity.PublicId.ToString(), "Create", null,
            ToObjectiveResponse(entity, IdpObjectiveMemberAccess.Full));
        return Ok(new ApiResponse<IdpStrategicObjectiveResponse>(true,
            ToObjectiveResponse(entity, await GetObjectiveMemberAccessAsync(user, objectiveScope))));
    }

    [HttpPost("priorities")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpDevelopmentPriorityResponse>>> CreatePriority([FromBody] CreateIdpDevelopmentPriorityRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpDevelopmentPriorityResponse>(false, null, "User not found"));
        if (!await _context.IdpStrategicObjectives.AnyAsync(item => item.Id == request.IdpStrategicObjectiveId))
            return NotFound(new ApiResponse<IdpDevelopmentPriorityResponse>(false, null, "Strategic objective not found"));

        var entity = new IdpDevelopmentPriority
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            PriorityCode = string.IsNullOrWhiteSpace(request.PriorityCode)
                ? $"PRIORITY-{Guid.NewGuid():N}"[..17].ToUpperInvariant()
                : request.PriorityCode.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            SortOrder = request.SortOrder
        };
        _context.IdpDevelopmentPriorities.Add(entity);
        await _context.SaveChangesAsync();
        var response = ToPriorityResponse(entity);
        await WriteIdpAudit(user.Id, "IdpDevelopmentPriority", entity.PublicId.ToString(), "Create", null, response);
        return Ok(new ApiResponse<IdpDevelopmentPriorityResponse>(true, response));
    }

    [HttpPost("programmes")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpProgrammeResponse>>> CreateProgramme([FromBody] CreateIdpProgrammeRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpProgrammeResponse>(false, null, "User not found"));

        var priority = await _context.IdpDevelopmentPriorities.AsNoTracking()
            .Include(item => item.IdpStrategicObjective).ThenInclude(item => item.IdpStrategicOutcome).ThenInclude(item => item.IdpPlan)
            .FirstOrDefaultAsync(item => item.Id == request.IdpDevelopmentPriorityId);
        if (priority == null) return NotFound(new ApiResponse<IdpProgrammeResponse>(false, null, "Development priority not found"));

        var programmeScope = Scope(targetId: priority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.PublicId);
        if (await MemberUpdateDenialAsync(user, "IDP_PROJECT", programmeScope,
            ["ProgrammePlannedBudget", "ProgrammeApprovedBudget", "ProgrammeActualExpenditure"]) is not null)
            return Forbid();

        var entity = new IdpProgramme
        {
            IdpDevelopmentPriorityId = request.IdpDevelopmentPriorityId,
            ProgrammeCode = request.ProgrammeCode.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            ResponsibleDepartmentId = request.ResponsibleDepartmentId,
            PlannedBudget = request.PlannedBudget,
            ApprovedBudget = request.ApprovedBudget,
            ActualExpenditure = request.ActualExpenditure
        };

        _context.IdpProgrammes.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpProgrammes.Include(item => item.ResponsibleDepartment).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpProgramme", entity.PublicId.ToString(), "Create", null,
            ToProgrammeResponse(entity, IdpProgrammeMemberAccess.Full));
        return Ok(new ApiResponse<IdpProgrammeResponse>(true,
            ToProgrammeResponse(entity, await GetProgrammeMemberAccessAsync(user, programmeScope))));
    }

    [HttpPost("projects")]
    [Authorize(Policy = "Permission:IDP.Project.Manage")]
    public async Task<ActionResult<ApiResponse<IdpProjectResponse>>> CreateProject([FromBody] CreateIdpProjectRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpProjectResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.Status, out IdpProjectStatus status))
        {
            return BadRequest(new ApiResponse<IdpProjectResponse>(false, null, "Invalid project status"));
        }

        var programme = await _context.IdpProgrammes.AsNoTracking()
            .Include(item => item.IdpDevelopmentPriority).ThenInclude(item => item.IdpStrategicObjective)
            .ThenInclude(item => item.IdpStrategicOutcome).ThenInclude(item => item.IdpPlan)
            .FirstOrDefaultAsync(item => item.Id == request.IdpProgrammeId);
        if (programme == null) return NotFound(new ApiResponse<IdpProjectResponse>(false, null, "Programme not found"));

        var projectScope = Scope(targetId: programme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.PublicId);
        if (await MemberUpdateDenialAsync(user, "IDP_PROJECT", projectScope,
            ["ProjectBudget", "ProjectFundingSource"]) is not null)
            return Forbid();

        var entity = new IdpProject
        {
            IdpProgrammeId = request.IdpProgrammeId,
            ProjectCode = request.ProjectCode.Trim(),
            ProjectName = request.ProjectName.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            DepartmentId = request.DepartmentId,
            Budget = request.Budget,
            FundingSource = request.FundingSource.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = status,
            CommunityNeedReference = request.CommunityNeedReference
        };

        _context.IdpProjects.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpProjects.Include(item => item.Department).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpProject", entity.PublicId.ToString(), "Create", null,
            ToProjectResponse(entity, IdpProjectMemberAccess.Full));
        projectScope = projectScope with { ProjectId = entity.PublicId.ToString() };
        return Ok(new ApiResponse<IdpProjectResponse>(true,
            ToProjectResponse(entity, await GetProjectMemberAccessAsync(user, projectScope))));
    }

    [HttpPost("kpis")]
    [Authorize(Policy = "Permission:IDP_INDICATOR.CREATE")]
    public async Task<ActionResult<ApiResponse<IdpKpiResponse>>> CreateKpi([FromBody] CreateIdpKpiRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpKpiResponse>(false, null, "User not found"));

        var projectExists = await _context.IdpProjects.AnyAsync(item => item.Id == request.IdpProjectId);
        if (!projectExists) return NotFound(new ApiResponse<IdpKpiResponse>(false, null, "Project not found"));
        if (request.ResponsibleDepartmentId.HasValue
            && !await _context.Departments.AnyAsync(item => item.Id == request.ResponsibleDepartmentId.Value && item.IsActive))
            return BadRequest(new ApiResponse<IdpKpiResponse>(false, null, "Responsible department was not found in the selected municipality."));

        var input = new IdpKpiDefinitionInput(
            request.IdpProjectId, request.KpiCode, request.KpiName, request.Description, request.Formula,
            request.Baseline, request.AnnualTarget, request.FiveYearTarget, request.ResponsibleDepartmentId,
            request.DataSource, request.ReportingFrequency, request.IndicatorType,
            request.Circular88Linked, request.TreasuryTidLinked);
        if (!IdpKpiDefinitionPolicy.TryNormalize(input, out var definition, out var issue))
            return BadRequest(new ApiResponse<IdpKpiResponse>(false, null, issue!.Message));

        var entity = new IdpKpi();
        IdpKpiDefinitionPolicy.Apply(entity, definition!);

        _context.IdpKpis.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpKpis.Include(item => item.ResponsibleDepartment).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpKpi", entity.PublicId.ToString(), "Create", null, ToKpiResponse(entity));
        return Ok(new ApiResponse<IdpKpiResponse>(true, ToKpiResponse(entity)));
    }

    [HttpPost("annual-targets")]
    [Authorize(Policy = "Permission:IDP_INDICATOR.UPDATE")]
    public async Task<ActionResult<ApiResponse<IdpAnnualTargetResponse>>> CreateAnnualTarget([FromBody] CreateIdpAnnualTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpAnnualTargetResponse>(false, null, "User not found"));

        var kpi = await _context.IdpKpis.AsNoTracking()
            .Where(item => item.Id == request.IdpKpiId)
            .Select(item => new { item.PublicId, ProjectPublicId = item.IdpProject.PublicId })
            .SingleOrDefaultAsync();
        if (kpi == null) return NotFound(new ApiResponse<IdpAnnualTargetResponse>(false, null, "KPI not found"));

        var requiredMembers = new List<string> { "AnnualTargetValue" };
        if (request.ActualValue.HasValue) requiredMembers.Add("AnnualActualValue");
        if (!string.IsNullOrWhiteSpace(request.ProgressComment)) requiredMembers.Add("AnnualProgressComment");
        if (await MemberUpdateDenialAsync(user, "IDP_INDICATOR", Scope(kpiId: kpi.PublicId, projectId: kpi.ProjectPublicId), requiredMembers) is not null)
            return Forbid();

        var entity = new IdpAnnualTarget
        {
            IdpKpiId = request.IdpKpiId,
            FinancialYear = request.FinancialYear,
            TargetValue = request.TargetValue,
            ActualValue = request.ActualValue,
            ProgressComment = request.ProgressComment
        };

        _context.IdpAnnualTargets.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpAnnualTarget", entity.PublicId.ToString(), "Create", null, ToAnnualTargetResponse(entity, IdpAnnualTargetMemberAccess.Full));

        return Ok(new ApiResponse<IdpAnnualTargetResponse>(true, ToAnnualTargetResponse(entity, await GetAnnualTargetMemberAccessAsync(user, Scope(kpiId: kpi.PublicId, projectId: kpi.ProjectPublicId)))));
    }

    [HttpPost("alignment-links")]
    [Authorize(Policy = "Permission:IDP.Alignment.Manage")]
    public async Task<ActionResult<ApiResponse<IdpAlignmentLinkResponse>>> CreateAlignmentLink([FromBody] CreateIdpAlignmentLinkRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpAlignmentLinkResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.FrameworkType, out AlignmentFrameworkType frameworkType))
        {
            return BadRequest(new ApiResponse<IdpAlignmentLinkResponse>(false, null, "Invalid framework type"));
        }

        var objectiveExists = await _context.IdpStrategicObjectives.AnyAsync(item => item.Id == request.IdpStrategicObjectiveId);
        if (!objectiveExists) return NotFound(new ApiResponse<IdpAlignmentLinkResponse>(false, null, "Strategic objective not found"));

        var entity = new IdpAlignmentLink
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            FrameworkType = frameworkType,
            FrameworkReferenceCode = request.FrameworkReferenceCode.Trim(),
            FrameworkReferenceTitle = request.FrameworkReferenceTitle.Trim(),
            Notes = request.Notes
        };

        _context.IdpAlignmentLinks.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpAlignmentLink", entity.PublicId.ToString(), "Create", null, ToAlignmentResponse(entity));

        return Ok(new ApiResponse<IdpAlignmentLinkResponse>(true, ToAlignmentResponse(entity)));
    }

    [HttpPost("community-sessions")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpCommunitySessionResponse>>> CreateCommunitySession([FromBody] CreateIdpCommunitySessionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpCommunitySessionResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.ParticipationType, out IdpParticipationType participationType))
        {
            return BadRequest(new ApiResponse<IdpCommunitySessionResponse>(false, null, "Invalid participation type"));
        }

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpCommunitySessionResponse>(false, null, "IDP plan not found"));

        var entity = new IdpCommunitySession
        {
            IdpPlanId = request.IdpPlanId,
            ParticipationType = participationType,
            SessionDate = request.SessionDate,
            Venue = request.Venue.Trim(),
            WardId = request.WardId,
            ParticipantsCount = request.ParticipantsCount,
            AttendanceRegisterPath = request.AttendanceRegisterPath,
            MinutesPath = request.MinutesPath
        };

        _context.IdpCommunitySessions.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpCommunitySessions.Include(item => item.Ward).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpCommunitySession", entity.PublicId.ToString(), "Create", null, ToCommunitySessionResponse(entity));

        return Ok(new ApiResponse<IdpCommunitySessionResponse>(true, ToCommunitySessionResponse(entity)));
    }

    [HttpPost("community-needs")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpCommunityNeedResponse>>> CreateCommunityNeed([FromBody] CreateIdpCommunityNeedRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpCommunityNeedResponse>(false, null, "User not found"));

        var sessionExists = await _context.IdpCommunitySessions.AnyAsync(item => item.Id == request.IdpCommunitySessionId);
        if (!sessionExists) return NotFound(new ApiResponse<IdpCommunityNeedResponse>(false, null, "Community session not found"));

        var entity = new IdpCommunityNeed
        {
            IdpCommunitySessionId = request.IdpCommunitySessionId,
            IssueCategory = request.IssueCategory.Trim(),
            Description = request.Description.Trim(),
            PriorityLevel = request.PriorityLevel.Trim(),
            ProposedIntervention = request.ProposedIntervention
        };

        _context.IdpCommunityNeeds.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpCommunityNeed", entity.PublicId.ToString(), "Create", null, ToCommunityNeedResponse(entity));

        return Ok(new ApiResponse<IdpCommunityNeedResponse>(true, ToCommunityNeedResponse(entity)));
    }

    [HttpPost("ward-inputs")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpWardInputResponse>>> CreateWardInput([FromBody] CreateIdpWardInputRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpWardInputResponse>(false, null, "User not found"));

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpWardInputResponse>(false, null, "IDP plan not found"));

        var ward = await _context.Wards.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.WardId);
        if (ward == null) return NotFound(new ApiResponse<IdpWardInputResponse>(false, null, "Ward not found"));

        var entity = new IdpWardInput
        {
            IdpPlanId = request.IdpPlanId,
            WardId = request.WardId,
            WardPlanSummary = request.WardPlanSummary.Trim(),
            WardPriorities = request.WardPriorities.Trim(),
            WardProjects = request.WardProjects.Trim()
        };

        _context.IdpWardInputs.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpWardInput", entity.PublicId.ToString(), "Create", null, ToWardInputResponse(entity, ward.Name));

        return Ok(new ApiResponse<IdpWardInputResponse>(true, ToWardInputResponse(entity, ward.Name)));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/stakeholder-engagements/page")]
    [Authorize(Policy = "Permission:IDP_STAKEHOLDER.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>>> GetStakeholderEngagementsPage(
        Guid planPublicId,
        [FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>(false, null, "User not found"));
        if (request.NormalizedSortBy is not ("sessiondate" or "stakeholdername" or "stakeholdertype" or "contactperson" or "contactemail"))
            return BadRequest(new ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>(false, null,
                "SortBy must be sessionDate, stakeholderName, stakeholderType, contactPerson, or contactEmail."));

        var canReadContactPerson = await CanAccessStakeholderMemberAsync(user, "ContactPerson", SecurityOperation.Read);
        var canReadContactEmail = await CanAccessStakeholderMemberAsync(user, "ContactEmail", SecurityOperation.Read);
        if ((request.NormalizedSortBy == "contactperson" && !canReadContactPerson)
            || (request.NormalizedSortBy == "contactemail" && !canReadContactEmail))
            return Forbid();

        var planExists = await _context.IdpPlans.AsNoTracking().AnyAsync(item => item.PublicId == planPublicId);
        if (!planExists) return NotFound(new ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>(false, null, "IDP plan not found"));

        var query = _context.IdpStakeholderEngagements.AsNoTracking()
            .Where(item => item.IdpCommunitySession.IdpPlan.PublicId == planPublicId);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.StakeholderType.Contains(term)
                || item.StakeholderName.Contains(term)
                || item.KeyInput != null && item.KeyInput.Contains(term)
                || canReadContactPerson && item.ContactPerson != null && item.ContactPerson.Contains(term)
                || canReadContactEmail && item.ContactEmail != null && item.ContactEmail.Contains(term));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("stakeholdername", false) => query.OrderBy(item => item.StakeholderName).ThenBy(item => item.Id),
            ("stakeholdername", true) => query.OrderByDescending(item => item.StakeholderName).ThenByDescending(item => item.Id),
            ("stakeholdertype", false) => query.OrderBy(item => item.StakeholderType).ThenBy(item => item.StakeholderName).ThenBy(item => item.Id),
            ("stakeholdertype", true) => query.OrderByDescending(item => item.StakeholderType).ThenBy(item => item.StakeholderName).ThenByDescending(item => item.Id),
            ("contactperson", false) => query.OrderBy(item => item.ContactPerson).ThenBy(item => item.Id),
            ("contactperson", true) => query.OrderByDescending(item => item.ContactPerson).ThenByDescending(item => item.Id),
            ("contactemail", false) => query.OrderBy(item => item.ContactEmail).ThenBy(item => item.Id),
            ("contactemail", true) => query.OrderByDescending(item => item.ContactEmail).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.IdpCommunitySession.SessionDate).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.IdpCommunitySession.SessionDate).ThenByDescending(item => item.Id)
        };
        var rows = await query.Include(item => item.IdpCommunitySession)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var response = PagedResponse<IdpStakeholderEngagementPageItemResponse>.Create(rows.Select(item =>
            ToStakeholderPageResponse(item, canReadContactPerson, canReadContactEmail)), request.Page, request.PageSize, totalCount);
        return Ok(new ApiResponse<PagedResponse<IdpStakeholderEngagementPageItemResponse>>(true, response));
    }

    [HttpPost("stakeholder-engagements")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public ActionResult<ApiResponse<IdpStakeholderEngagementResponse>> CreateStakeholderEngagement([FromBody] CreateIdpStakeholderEngagementRequest request)
    {
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpStakeholderEngagementResponse>(false, null,
            "This numeric-session route is retired. Use POST /api/v1/idp/community-sessions/{sessionPublicId}/stakeholder-engagements."));
    }

    [HttpPost("~/api/v1/idp/community-sessions/{sessionPublicId:guid}/stakeholder-engagements")]
    [Authorize(Policy = "Permission:IDP_STAKEHOLDER.CREATE")]
    public async Task<ActionResult<ApiResponse<IdpStakeholderEngagementPageItemResponse>>> CreateStakeholderEngagementV1(
        Guid sessionPublicId,
        [FromBody] CreateIdpStakeholderEngagementV1Request request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpStakeholderEngagementPageItemResponse>(false, null, "User not found"));
        if ((!string.IsNullOrWhiteSpace(request.ContactPerson)
                && !await CanAccessStakeholderMemberAsync(user, "ContactPerson", SecurityOperation.Update))
            || (!string.IsNullOrWhiteSpace(request.ContactEmail)
                && !await CanAccessStakeholderMemberAsync(user, "ContactEmail", SecurityOperation.Update)))
            return Forbid();

        var session = await _context.IdpCommunitySessions.FirstOrDefaultAsync(item => item.PublicId == sessionPublicId);
        if (session == null) return NotFound(new ApiResponse<IdpStakeholderEngagementPageItemResponse>(false, null, "Community session not found"));

        var entity = new IdpStakeholderEngagement
        {
            IdpCommunitySessionId = session.Id,
            StakeholderType = request.StakeholderType.Trim(),
            StakeholderName = request.StakeholderName.Trim(),
            ContactPerson = NormalizeOptional(request.ContactPerson),
            ContactEmail = NormalizeOptional(request.ContactEmail),
            KeyInput = NormalizeOptional(request.KeyInput)
        };

        _context.IdpStakeholderEngagements.Add(entity);
        await _context.SaveChangesAsync();
        entity.IdpCommunitySession = session;
        await WriteIdpAudit(user.Id, "IdpStakeholderEngagement", entity.PublicId.ToString(), "Create", null,
            new { entity.PublicId, SessionPublicId = session.PublicId, entity.StakeholderType, entity.StakeholderName });

        var canReadContactPerson = await CanAccessStakeholderMemberAsync(user, "ContactPerson", SecurityOperation.Read);
        var canReadContactEmail = await CanAccessStakeholderMemberAsync(user, "ContactEmail", SecurityOperation.Read);
        return Ok(new ApiResponse<IdpStakeholderEngagementPageItemResponse>(true,
            ToStakeholderPageResponse(entity, canReadContactPerson, canReadContactEmail)));
    }

    [HttpPost("risk-links")]
    [Authorize(Policy = "Permission:IDP.Risk.Manage")]
    public async Task<ActionResult<ApiResponse<IdpRiskLinkResponse>>> CreateRiskLink([FromBody] CreateIdpRiskLinkRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpRiskLinkResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.RiskLevel, out IdpRiskLevel riskLevel))
        {
            return BadRequest(new ApiResponse<IdpRiskLinkResponse>(false, null, "Invalid risk level"));
        }

        var entity = new IdpRiskLink
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            IdpProjectId = request.IdpProjectId,
            IdpKpiId = request.IdpKpiId,
            RiskReference = request.RiskReference.Trim(),
            RiskTitle = request.RiskTitle.Trim(),
            MitigationPlan = request.MitigationPlan,
            RiskLevel = riskLevel
        };

        _context.IdpRiskLinks.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpRiskLink", entity.PublicId.ToString(), "Create", null, ToRiskResponse(entity));

        return Ok(new ApiResponse<IdpRiskLinkResponse>(true, ToRiskResponse(entity)));
    }

    [HttpPost("budget-snapshots")]
    [Authorize(Policy = "Permission:IDP.Budget.View")]
    public async Task<ActionResult<ApiResponse<IdpBudgetSnapshotResponse>>> CreateBudgetSnapshot([FromBody] CreateIdpBudgetSnapshotRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpBudgetSnapshotResponse>(false, null, "User not found"));

        var scope = await ResolveBudgetScopeAsync(request.IdpStrategicObjectiveId, request.IdpProjectId);
        if (scope == null) return NotFound(new ApiResponse<IdpBudgetSnapshotResponse>(false, null, "The IDP objective or project was not found."));
        if (await MemberUpdateDenialAsync(user, "IDP_PROJECT", scope,
                ["BudgetSnapshotPlanned", "BudgetSnapshotApproved", "BudgetSnapshotActual", "BudgetSnapshotSource"]) is not null)
            return Forbid();

        var entity = new IdpBudgetSnapshot
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            IdpProjectId = request.IdpProjectId,
            FinancialYear = request.FinancialYear,
            PlannedBudget = request.PlannedBudget,
            ApprovedBudget = request.ApprovedBudget,
            ActualExpenditure = request.ActualExpenditure,
            SourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "FMS" : request.SourceSystem.Trim(),
            CapturedAt = DateTime.UtcNow
        };

        _context.IdpBudgetSnapshots.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpBudgetSnapshot", entity.PublicId.ToString(), "Create", null, ToBudgetSnapshotResponse(entity, IdpBudgetMemberAccess.Full));

        return Ok(new ApiResponse<IdpBudgetSnapshotResponse>(true, ToBudgetSnapshotResponse(entity, await GetBudgetMemberAccessAsync(user, scope))));
    }

    [HttpPost("documents")]
    [Authorize(Policy = "Permission:IDP_DOCUMENT.CREATE")]
    public ActionResult<ApiResponse<IdpDocumentResponse>> CreateDocument([FromBody] CreateIdpDocumentRequest request)
    {
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpDocumentResponse>(false, null,
            "Client-supplied document metadata and storage paths are no longer accepted. Use the versioned multipart IDP document endpoint."));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/documents")]
    [Authorize(Policy = "Permission:IDP_DOCUMENT.READ")]
    public ActionResult<ApiResponse<IdpDocumentResponse[]>> GetDocuments(Guid planPublicId)
    {
        _ = planPublicId;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpDocumentResponse[]>(false, null,
            "This unbounded route is retired. Use /api/v1/idp/plans/{planPublicId}/documents/page."));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/documents/page")]
    [Authorize(Policy = "Permission:IDP_DOCUMENT.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IdpDocumentResponse>>>> GetDocumentsPage(
        Guid planPublicId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? category = null,
        [FromQuery] string? scanStatus = null,
        [FromQuery] bool? quarantined = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<IdpDocumentResponse>>(false, null, "User not found."));
        if (request.NormalizedSortBy is not ("createdat" or "uploadedat" or "title" or "filename" or "category" or "scanstatus" or "versionnumber"))
            return BadRequest(new ApiResponse<PagedResponse<IdpDocumentResponse>>(false, null,
                "SortBy must be createdAt, uploadedAt, title, fileName, category, scanStatus, or versionNumber."));
        if (!string.IsNullOrWhiteSpace(category) && !TryParseEnum(category, out IdpDocumentCategory _))
            return BadRequest(new ApiResponse<PagedResponse<IdpDocumentResponse>>(false, null, "Invalid document category."));

        var plan = await _context.IdpPlans.AsNoTracking()
            .Where(item => item.PublicId == planPublicId)
            .Select(item => new { item.MunicipalityId })
            .FirstOrDefaultAsync();
        if (plan == null)
            return NotFound(new ApiResponse<PagedResponse<IdpDocumentResponse>>(false, null, "IDP plan not found."));
        if (!plan.MunicipalityId.HasValue)
            return NotFound(new ApiResponse<PagedResponse<IdpDocumentResponse>>(false, null, "IDP plan is not assigned to a municipality."));

        IQueryable<IdpDocument> query = _context.IdpDocuments
            .AsNoTracking()
            .Include(item => item.IdpPlan)
            .Include(item => item.IdpPlanVersion)
            .Include(item => item.UploadedByUser)
            .Include(item => item.Blob)
            .Where(item => item.IdpPlan.PublicId == planPublicId && item.IsActive);

        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(item => item.Title.Contains(search) || item.FileName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(category) && TryParseEnum(category, out IdpDocumentCategory parsedCategory))
            query = query.Where(item => item.Category == parsedCategory);
        if (!string.IsNullOrWhiteSpace(scanStatus))
        {
            var normalizedScanStatus = scanStatus.Trim();
            query = query.Where(item => item.Blob.ScanStatus == normalizedScanStatus);
        }
        if (quarantined.HasValue)
            query = query.Where(item => item.Blob.IsQuarantined == quarantined.Value);

        var totalCount = await query.CountAsync();
        var descending = request.Descending;
        query = request.NormalizedSortBy switch
        {
            "title" => descending ? query.OrderByDescending(item => item.Title).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.Title).ThenBy(item => item.PublicId),
            "filename" => descending ? query.OrderByDescending(item => item.FileName).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.FileName).ThenBy(item => item.PublicId),
            "category" => descending ? query.OrderByDescending(item => item.Category).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.Category).ThenBy(item => item.PublicId),
            "scanstatus" => descending ? query.OrderByDescending(item => item.Blob.ScanStatus).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.Blob.ScanStatus).ThenBy(item => item.PublicId),
            "versionnumber" => descending ? query.OrderByDescending(item => item.VersionNumber).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.VersionNumber).ThenBy(item => item.PublicId),
            _ => descending ? query.OrderByDescending(item => item.UploadedAt).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.UploadedAt).ThenBy(item => item.PublicId)
        };
        var documents = await query.Skip(request.Offset).Take(request.PageSize).ToListAsync();
        var memberAccess = await GetDocumentMemberAccessAsync(user, plan.MunicipalityId.Value);

        return Ok(new ApiResponse<PagedResponse<IdpDocumentResponse>>(true,
            PagedResponse<IdpDocumentResponse>.Create(documents.Select(item => ToDocumentResponse(item, memberAccess)), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("~/api/v1/idp/plans/{planPublicId:guid}/documents")]
    [Authorize(Policy = "Permission:IDP_DOCUMENT.CREATE")]
    [RequestSizeLimit(MaximumDocumentBytes)]
    public async Task<ActionResult<ApiResponse<IdpDocumentResponse>>> UploadDocument(
        Guid planPublicId,
        [FromForm] IFormFile file,
        [FromForm] string category,
        [FromForm] string title,
        [FromForm] int? planVersionNumber = null)
    {
        if (_evidenceStorage == null || _evidenceInspection == null || _malwareScanner == null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<IdpDocumentResponse>(false, null, "Governed document storage is unavailable."));
        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse<IdpDocumentResponse>(false, null, "File is required."));
        if (file.Length > MaximumDocumentBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ApiResponse<IdpDocumentResponse>(false, null, "File exceeds the 25 MB document limit."));
        if (!TryParseEnum(category, out IdpDocumentCategory parsedCategory))
            return BadRequest(new ApiResponse<IdpDocumentResponse>(false, null, "Invalid document category."));
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new ApiResponse<IdpDocumentResponse>(false, null, "Document title is required."));

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedDocumentTypes.TryGetValue(extension, out var contentTypes) || !contentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ApiResponse<IdpDocumentResponse>(false, null, "Unsupported document file type."));

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpDocumentResponse>(false, null, "User not found."));
        var plan = await _context.IdpPlans.Include(item => item.Versions).FirstOrDefaultAsync(item => item.PublicId == planPublicId);
        if (plan == null) return NotFound(new ApiResponse<IdpDocumentResponse>(false, null, "IDP plan not found."));
        if (!plan.MunicipalityId.HasValue)
            return NotFound(new ApiResponse<IdpDocumentResponse>(false, null, "IDP plan is not assigned to a municipality."));

        IdpPlanVersion? planVersion = null;
        if (planVersionNumber.HasValue)
        {
            planVersion = plan.Versions.SingleOrDefault(item => item.VersionNumber == planVersionNumber.Value);
            if (planVersion == null) return BadRequest(new ApiResponse<IdpDocumentResponse>(false, null, "The selected version does not belong to this IDP plan."));
        }

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, HttpContext.RequestAborted);
        var content = buffer.ToArray();
        var inspection = _evidenceInspection.Inspect(content, extension);
        if (!inspection.SignatureValid)
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ApiResponse<IdpDocumentResponse>(false, null, inspection.Error));
        var malwareScan = await _malwareScanner.ScanAsync(content, file.FileName, file.ContentType, HttpContext.RequestAborted);

        var versionNumber = (await _context.IdpDocuments
            .Where(item => item.IdpPlanId == plan.Id && item.Category == parsedCategory && item.Title == title.Trim())
            .MaxAsync(item => (int?)item.VersionNumber) ?? 0) + 1;
        var relativeDirectory = Path.Combine("idp", planPublicId.ToString("N"));
        var relativePath = Path.Combine(relativeDirectory, $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        var stored = await _evidenceStorage.StoreAsync(relativePath, content, HttpContext.RequestAborted);
        if (!stored.Succeeded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<IdpDocumentResponse>(false, null, "Evidence storage is unavailable: " + stored.Detail));
        var now = DateTime.UtcNow;
        var entity = new IdpDocument
        {
            IdpPlan = plan,
            IdpPlanVersion = planVersion,
            Category = parsedCategory,
            Title = title.Trim(),
            FileName = Path.GetFileName(file.FileName),
            Blob = new EvidenceBlob { MunicipalityId = plan.MunicipalityId, StorageKey = relativePath, ContentType = file.ContentType, SizeInBytes = content.LongLength, Sha256 = inspection.Sha256, SignatureVerified = inspection.SignatureValid, ScanStatus = malwareScan.Status, IsQuarantined = !malwareScan.IsClean, ScannerProvider = malwareScan.Provider, ScannerReference = malwareScan.ProviderReference, ScanDetail = malwareScan.Detail, ScannedAt = now },
            RetainUntil = now.AddYears(7),
            VersionNumber = versionNumber,
            IsApproved = false,
            UploadedAt = now,
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };

        _context.IdpDocuments.Add(entity);
        try { await _context.SaveChangesAsync(); }
        catch
        {
            await _evidenceStorage.DisposeAsync(relativePath, CancellationToken.None);
            throw;
        }
        var auditResponse = ToDocumentResponse(entity, DocumentMetadataMemberAccess.Full);
        await WriteIdpAudit(user.Id, "IdpDocument", entity.PublicId.ToString(), "Upload", null, auditResponse);
        var response = ToDocumentResponse(entity, await GetDocumentMemberAccessAsync(user, plan.MunicipalityId.Value));
        return Ok(new ApiResponse<IdpDocumentResponse>(true, response,
            malwareScan.IsClean ? "Document uploaded and released after a clean scan." : "Document uploaded and quarantined pending a clean scan."));
    }

    [HttpGet("~/api/v1/idp/plans/{planPublicId:guid}/documents/{documentPublicId:guid}/content")]
    [Authorize(Policy = "Permission:IDP_DOCUMENT.READ")]
    public async Task<IActionResult> DownloadDocument(Guid planPublicId, Guid documentPublicId)
    {
        if (_evidenceStorage == null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        var document = await _context.IdpDocuments.AsNoTracking().Include(item => item.Blob).FirstOrDefaultAsync(item =>
            item.PublicId == documentPublicId && item.IdpPlan.PublicId == planPublicId && item.IsActive &&
            !item.Blob.IsContentDeleted && !item.Blob.IsQuarantined && item.Blob.SignatureVerified && item.Blob.ScanStatus == "Clean");
        if (document == null) return NotFound();
        var stored = await _evidenceStorage.ReadAsync(document.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found) return stored.Available ? NotFound() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        return File(stored.Content, document.Blob.ContentType ?? "application/octet-stream", document.FileName, enableRangeProcessing: true);
    }

    [HttpPost("~/api/v1/idp/plans/{planPublicId:guid}/documents/{documentPublicId:guid}/rescan")]
    [Authorize(Policy = "Permission:IDP_DOCUMENT.RESCAN")]
    public async Task<ActionResult<ApiResponse<IdpDocumentResponse>>> RescanDocument(Guid planPublicId, Guid documentPublicId)
    {
        if (_evidenceStorage == null || _malwareScanner == null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<IdpDocumentResponse>(false, null, "Governed document scanning is unavailable."));
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpDocumentResponse>(false, null, "User not found."));
        var document = await _context.IdpDocuments
            .Include(item => item.IdpPlan)
            .Include(item => item.IdpPlanVersion)
            .Include(item => item.UploadedByUser)
            .Include(item => item.Blob)
            .FirstOrDefaultAsync(item => item.PublicId == documentPublicId && item.IdpPlan.PublicId == planPublicId && item.IsActive);
        if (document == null) return NotFound(new ApiResponse<IdpDocumentResponse>(false, null, "IDP document not found."));
        if (!document.IdpPlan.MunicipalityId.HasValue)
            return NotFound(new ApiResponse<IdpDocumentResponse>(false, null, "IDP plan is not assigned to a municipality."));
        if (document.Blob.IsContentDeleted) return Conflict(new ApiResponse<IdpDocumentResponse>(false, null, "Disposed document content cannot be rescanned."));
        var stored = await _evidenceStorage.ReadAsync(document.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found)
            return stored.Available
                ? NotFound(new ApiResponse<IdpDocumentResponse>(false, null, "Stored document content not found."))
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<IdpDocumentResponse>(false, null, "Evidence storage is unavailable: " + stored.Detail));

        var before = new { document.Blob.ScanStatus, document.Blob.IsQuarantined, document.Blob.ScannerReference };
        var scan = await _malwareScanner.ScanAsync(stored.Content, document.FileName, document.Blob.ContentType ?? "application/octet-stream", HttpContext.RequestAborted);
        document.Blob.ScanStatus = scan.Status;
        document.Blob.IsQuarantined = !scan.IsClean;
        document.Blob.ScannerProvider = scan.Provider;
        document.Blob.ScannerReference = scan.ProviderReference;
        document.Blob.ScanDetail = scan.Detail;
        document.Blob.ScannedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var auditResponse = ToDocumentResponse(document, DocumentMetadataMemberAccess.Full);
        await WriteIdpAudit(user.Id, "IdpDocument", document.PublicId.ToString(), "MalwareRescan", before, auditResponse);
        var response = ToDocumentResponse(document, await GetDocumentMemberAccessAsync(user, document.IdpPlan.MunicipalityId.Value));
        return Ok(new ApiResponse<IdpDocumentResponse>(true, response, scan.IsClean ? "Document released after a clean scan." : "Document remains quarantined."));
    }

    [HttpPost("comments")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public async Task<ActionResult<ApiResponse<IdpCommentResponse>>> CreateComment([FromBody] CreateIdpCommentRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpCommentResponse>(false, null, "User not found"));

        var plan = await _context.IdpPlans.AsNoTracking().Where(item => item.Id == request.IdpPlanId)
            .Select(item => new { item.PublicId, item.MunicipalityId }).SingleOrDefaultAsync();
        if (plan == null) return NotFound(new ApiResponse<IdpCommentResponse>(false, null, "IDP plan not found"));
        var scope = Scope(targetId: plan.PublicId);
        if (await MemberUpdateDenialAsync(user, "IDP_PLAN", scope, ["CollaborationComment"]) is not null)
            return Forbid();

        var entity = new IdpCollaborationComment
        {
            IdpPlanId = request.IdpPlanId,
            IdpPlanVersionId = request.IdpPlanVersionId,
            EntityName = request.EntityName.Trim(),
            EntityId = request.EntityId.Trim(),
            Comment = request.Comment.Trim(),
            CommentedByUserId = user.Id,
            CommentedAt = DateTime.UtcNow
        };

        _context.IdpCollaborationComments.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpComment", entity.PublicId.ToString(), "Create", null,
            ToCommentResponse(entity, user, IdpCollaborationMemberAccess.Full));

        return Ok(new ApiResponse<IdpCommentResponse>(true,
            ToCommentResponse(entity, user, await GetCollaborationMemberAccessAsync(user, scope))));
    }

    [HttpPost("tasks")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public async Task<ActionResult<ApiResponse<IdpTaskResponse>>> CreateTask([FromBody] CreateIdpTaskRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpTaskResponse>(false, null, "User not found"));

        var plan = await _context.IdpPlans.AsNoTracking().Where(item => item.Id == request.IdpPlanId)
            .Select(item => new { item.PublicId, item.MunicipalityId }).SingleOrDefaultAsync();
        if (plan == null) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "IDP plan not found"));
        var scope = Scope(targetId: plan.PublicId);
        if (await MemberUpdateDenialAsync(user, "IDP_PLAN", scope, ["TaskContent", "TaskAssignee"]) is not null)
            return Forbid();

        var assignee = await _userManager.FindByIdAsync(request.AssignedToUserId);
        if (assignee == null) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "Assignee not found"));
        if (plan.MunicipalityId.HasValue && assignee.MunicipalityId != plan.MunicipalityId.Value)
        {
            var now = DateTime.UtcNow;
            var hasTenantAssignment = await _context.SecurityUserRoleAssignments.AsNoTracking().AnyAsync(item =>
                item.UserId == assignee.Id && item.MunicipalityId == plan.MunicipalityId.Value && item.IsActive
                && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now));
            if (!hasTenantAssignment) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "Assignee not found"));
        }

        var entity = new IdpTaskAssignment
        {
            IdpPlanId = request.IdpPlanId,
            IdpPlanVersionId = request.IdpPlanVersionId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = user.Id,
            DueDate = request.DueDate,
            IsCompleted = false
        };

        _context.IdpTaskAssignments.Add(entity);
        await _context.SaveChangesAsync();

        await _workflowGovernanceService.CreateNotificationAsync(
            assignee.Id,
            NotificationType.Submission,
            "IDP task assigned",
            "You were assigned an IDP collaboration task.",
            "IdpTask",
            entity.PublicId.ToString());

        await WriteIdpAudit(user.Id, "IdpTask", entity.PublicId.ToString(), "Create", null,
            ToTaskResponse(entity, assignee, user, IdpCollaborationMemberAccess.Full));
        return Ok(new ApiResponse<IdpTaskResponse>(true,
            ToTaskResponse(entity, assignee, user, await GetCollaborationMemberAccessAsync(user, scope))));
    }

    [HttpPatch("tasks/{id:long}/complete")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public ActionResult<ApiResponse<IdpTaskResponse>> CompleteTask(long id, [FromBody] CompleteIdpTaskRequest request)
    {
        _ = id;
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<IdpTaskResponse>(false, null,
            "This numeric-ID task route is retired. Use /api/v1/idp/tasks/{taskPublicId}/completion."));
    }

    [HttpPatch("~/api/v1/idp/tasks/{taskPublicId:guid}/completion")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public async Task<ActionResult<ApiResponse<IdpTaskResponse>>> CompleteTaskByPublicId(Guid taskPublicId, [FromBody] CompleteIdpTaskRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpTaskResponse>(false, null, "User not found"));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length is < 5 or > 500)
            return BadRequest(new ApiResponse<IdpTaskResponse>(false, null, "A completion reason between 5 and 500 characters is required."));

        var entity = await _context.IdpTaskAssignments.Include(item => item.IdpPlan).FirstOrDefaultAsync(item => item.PublicId == taskPublicId);
        if (entity == null) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "Task not found"));
        if (string.IsNullOrWhiteSpace(request.RowVersion))
            return BadRequest(new ApiResponse<IdpTaskResponse>(false, null, "A valid RowVersion is required."));
        try
        {
            _context.Entry(entity).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return BadRequest(new ApiResponse<IdpTaskResponse>(false, null, "A valid RowVersion is required."));
        }

        var before = ToTaskResponse(entity, null, null, IdpCollaborationMemberAccess.Full);
        entity.IsCompleted = request.IsCompleted;
        entity.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<IdpTaskResponse>(false, null, "The task changed since it was loaded. Refresh and try again."));
        }

        var assignee = await _userManager.FindByIdAsync(entity.AssignedToUserId);
        var assigner = await _userManager.FindByIdAsync(entity.AssignedByUserId);

        var fullResponse = ToTaskResponse(entity, assignee, assigner, IdpCollaborationMemberAccess.Full);
        await WriteIdpAudit(user.Id, "IdpTask", entity.PublicId.ToString(), request.IsCompleted ? "Complete" : "Reopen",
            new { Task = before, Reason = request.Reason.Trim() }, fullResponse);
        var memberAccess = await GetCollaborationMemberAccessAsync(user, Scope(targetId: entity.IdpPlan.PublicId, taskId: entity.PublicId));
        return Ok(new ApiResponse<IdpTaskResponse>(true, ToTaskResponse(entity, assignee, assigner, memberAccess)));
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private async Task<bool> CanAccessStakeholderMemberAsync(ApplicationUser user, string memberCode, SecurityOperation operation)
    {
        return await CanAccessMemberAsync(user, "IDP_STAKEHOLDER", memberCode, operation, Scope());
    }

    private AccessScopeContext Scope(Guid? targetId = null, Guid? kpiId = null, Guid? projectId = null, Guid? taskId = null) =>
        new(TargetId: targetId?.ToString(), KpiId: kpiId?.ToString(), ProjectId: projectId?.ToString(),
            TaskId: taskId?.ToString(), MunicipalityId: _tenantContext?.MunicipalityId);

    private async Task<bool> CanAccessMemberAsync(
        ApplicationUser user,
        string resourceCode,
        string memberCode,
        SecurityOperation operation,
        AccessScopeContext scope)
    {
        if (_accessControl == null) return false;
        var decision = await _accessControl.CheckPermissionAsync(user,
            $"{resourceCode}.{memberCode}.{operation.ToString().ToUpperInvariant()}", scope);
        return decision.Allowed;
    }

    private async Task<string?> MemberUpdateDenialAsync(
        ApplicationUser user,
        string resourceCode,
        AccessScopeContext scope,
        IReadOnlyCollection<string> memberCodes)
    {
        foreach (var memberCode in memberCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_accessControl == null) return $"{memberCode} update permission is required.";
            var decision = await _accessControl.CheckPermissionAsync(user, $"{resourceCode}.{memberCode}.UPDATE", scope);
            if (!decision.Allowed) return decision.Reason ?? $"{memberCode} update permission is required.";
        }
        return null;
    }

    private async Task<IdpAnnualTargetMemberAccess> GetAnnualTargetMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_INDICATOR", "AnnualTargetValue", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_INDICATOR", "AnnualActualValue", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_INDICATOR", "AnnualProgressComment", SecurityOperation.Read, scope ?? Scope()));

    private async Task<IdpBudgetMemberAccess> GetBudgetMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_PROJECT", "BudgetSnapshotPlanned", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PROJECT", "BudgetSnapshotApproved", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PROJECT", "BudgetSnapshotActual", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PROJECT", "BudgetSnapshotSource", SecurityOperation.Read, scope ?? Scope()));

    private async Task<IdpCollaborationMemberAccess> GetCollaborationMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_PLAN", "CollaborationComment", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PLAN", "CollaborationActor", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PLAN", "TaskContent", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PLAN", "TaskAssignee", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PLAN", "TaskAssigner", SecurityOperation.Read, scope ?? Scope()));

    private async Task<IdpPlanVersionMemberAccess> GetPlanVersionMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_PLAN", "VersionSummary", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PLAN", "VersionCreator", SecurityOperation.Read, scope ?? Scope()));

    private async Task<IdpObjectiveMemberAccess> GetObjectiveMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_PLAN", "ObjectiveStrategicOwner", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PLAN", "ObjectiveBudgetAllocation", SecurityOperation.Read, scope ?? Scope()));

    private async Task<IdpProgrammeMemberAccess> GetProgrammeMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_PROJECT", "ProgrammePlannedBudget", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PROJECT", "ProgrammeApprovedBudget", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PROJECT", "ProgrammeActualExpenditure", SecurityOperation.Read, scope ?? Scope()));

    private async Task<IdpProjectMemberAccess> GetProjectMemberAccessAsync(ApplicationUser user, AccessScopeContext? scope = null) =>
        new(
            await CanAccessMemberAsync(user, "IDP_PROJECT", "ProjectBudget", SecurityOperation.Read, scope ?? Scope()),
            await CanAccessMemberAsync(user, "IDP_PROJECT", "ProjectFundingSource", SecurityOperation.Read, scope ?? Scope()));

    private async Task<AccessScopeContext?> ResolveBudgetScopeAsync(int? objectiveId, int? projectId)
    {
        if (objectiveId.HasValue == projectId.HasValue) return null;
        if (projectId.HasValue)
        {
            var publicId = await _context.IdpProjects.AsNoTracking().Where(item => item.Id == projectId.Value)
                .Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync();
            return publicId.HasValue ? Scope(projectId: publicId.Value) : null;
        }

        var objectivePublicId = await _context.IdpStrategicObjectives.AsNoTracking().Where(item => item.Id == objectiveId!.Value)
            .Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync();
        return objectivePublicId.HasValue ? Scope(targetId: objectivePublicId.Value) : null;
    }

    private async Task<DocumentMetadataMemberAccess> GetDocumentMemberAccessAsync(ApplicationUser user, long municipalityId)
    {
        if (_accessControl == null) return new DocumentMetadataMemberAccess(false, false, false, false, false);
        async Task<bool> CanReadAsync(string memberCode)
        {
            var decision = await _accessControl.CheckPermissionAsync(user, $"IDP_DOCUMENT.{memberCode}.READ",
                new AccessScopeContext(MunicipalityId: municipalityId));
            return decision.Allowed;
        }

        return new DocumentMetadataMemberAccess(
            await CanReadAsync("UploadedByUserId"),
            await CanReadAsync("UploadedByName"),
            await CanReadAsync("ScannerProvider"),
            await CanReadAsync("ScannerReference"),
            await CanReadAsync("ScanDetail"));
    }

    private Task WriteIdpAudit(string changedBy, string entityName, string entityId, string action, object? before, object? after)
    {
        return _workflowGovernanceService.WriteAuditTrailAsync(entityName, entityId, action, before, after, changedBy, PerformanceApiSupport.GetIpAddress(HttpContext));
    }

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed) where TEnum : struct, Enum
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);

        foreach (var name in Enum.GetNames(typeof(TEnum)))
        {
            var candidate = name.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
            {
                parsed = Enum.Parse<TEnum>(name, true);
                return true;
            }
        }

        parsed = default;
        return false;
    }

    private static IdpPlanSummaryResponse ToSummaryResponse(IdpPlan plan) =>
        new(plan.Id, plan.PublicId, plan.MunicipalityName, plan.PlanTitle, plan.PlanCode, plan.StartFinancialYear, plan.EndFinancialYear, plan.Status.ToString(), plan.CurrentVersionNumber, plan.CreatedAt, plan.ApprovedAt, Convert.ToBase64String(plan.RowVersion), plan.PlanFamilyId, plan.PredecessorPlan?.PublicId, plan.EffectiveFrom, plan.EffectiveTo, plan.PublishedAt, plan.PublicationReference);

    private static IdpPlanVersionResponse ToVersionResponse(IdpPlanVersion version, IdpPlanVersionMemberAccess access) =>
        new(version.Id, version.PublicId, version.IdpPlanId, version.PredecessorVersion?.PublicId, version.VersionNumber, version.VersionType.ToString(), version.VersionLabel, version.ReviewYear,
            access.Summary ? version.SummaryOfChanges : null, version.IsActive, version.CreatedAt,
            access.Creator ? version.CreatedByUser?.PublicId : null, access.Creator ? version.CreatedByUser?.FullName : null,
            version.EffectiveFrom, version.EffectiveTo, version.PublishedAt, version.PublicationReference, Convert.ToBase64String(version.RowVersion));

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IdpStrategicOutcomeResponse ToOutcomeResponse(IdpStrategicOutcome outcome) =>
        new(outcome.Id, outcome.IdpPlanId, outcome.Code, outcome.Name, outcome.Description, outcome.SortOrder)
        {
            PublicId = outcome.PublicId,
            RowVersion = Convert.ToBase64String(outcome.RowVersion)
        };

    private static IdpStrategicObjectiveResponse ToObjectiveResponse(IdpStrategicObjective objective, IdpObjectiveMemberAccess access) =>
        new(
            objective.Id,
            objective.IdpStrategicOutcomeId,
            objective.Code,
            objective.Name,
            objective.Description,
            objective.BaselineValue,
            objective.TargetValue,
            objective.ResponsibleDepartmentId,
            objective.ResponsibleDepartment?.Name,
            access.StrategicOwner ? objective.StrategicOwnerUser?.PublicId : null,
            access.StrategicOwner ? objective.StrategicOwnerUser?.FullName : null,
            objective.StartDate,
            objective.EndDate,
            access.BudgetAllocation ? objective.BudgetAllocation : null,
            objective.SortOrder)
        {
            PublicId = objective.PublicId,
            RowVersion = Convert.ToBase64String(objective.RowVersion)
        };

    private static IdpDevelopmentPriorityResponse ToPriorityResponse(IdpDevelopmentPriority priority) =>
        new(priority.Id, priority.IdpStrategicObjectiveId, priority.Name, priority.Description, priority.SortOrder)
        {
            PublicId = priority.PublicId,
            PriorityCode = priority.PriorityCode,
            RowVersion = Convert.ToBase64String(priority.RowVersion)
        };

    private static IdpProgrammeResponse ToProgrammeResponse(IdpProgramme programme, IdpProgrammeMemberAccess access) =>
        new(
            programme.Id,
            programme.IdpDevelopmentPriorityId,
            programme.ProgrammeCode,
            programme.Name,
            programme.Description,
            programme.ResponsibleDepartmentId,
            programme.ResponsibleDepartment?.Name,
            access.PlannedBudget ? programme.PlannedBudget : null,
            access.ApprovedBudget ? programme.ApprovedBudget : null,
            access.ActualExpenditure ? programme.ActualExpenditure : null)
        {
            PublicId = programme.PublicId,
            RowVersion = Convert.ToBase64String(programme.RowVersion)
        };

    private static IdpProjectResponse ToProjectResponse(IdpProject project, IdpProjectMemberAccess access) =>
        new(
            project.Id,
            project.IdpProgrammeId,
            project.ProjectCode,
            project.ProjectName,
            project.Description,
            project.Category,
            project.DepartmentId,
            project.Department?.Name,
            access.Budget ? project.Budget : null,
            access.FundingSource ? project.FundingSource : null,
            project.StartDate,
            project.EndDate,
            project.Status.ToString(),
            project.CommunityNeedReference)
        {
            PublicId = project.PublicId,
            RowVersion = Convert.ToBase64String(project.RowVersion)
        };

    private static IdpKpiResponse ToKpiResponse(IdpKpi kpi) =>
        new(
            kpi.Id,
            kpi.IdpProjectId,
            kpi.KpiCode,
            kpi.KpiName,
            kpi.Description,
            kpi.Formula,
            kpi.Baseline,
            kpi.AnnualTarget,
            kpi.FiveYearTarget,
            kpi.ResponsibleDepartmentId,
            kpi.ResponsibleDepartment?.Name,
            kpi.DataSource,
            kpi.ReportingFrequency,
            kpi.IndicatorType.ToString(),
            kpi.Circular88Linked,
            kpi.TreasuryTidLinked,
            kpi.PublicId,
            Convert.ToBase64String(kpi.RowVersion));

    private static IdpAnnualTargetResponse ToAnnualTargetResponse(IdpAnnualTarget annualTarget, IdpAnnualTargetMemberAccess access) =>
        new(annualTarget.PublicId, annualTarget.IdpKpiId, annualTarget.FinancialYear,
            access.TargetValue ? annualTarget.TargetValue : null,
            access.ActualValue ? annualTarget.ActualValue : null,
            access.ProgressComment ? annualTarget.ProgressComment : null,
            Convert.ToBase64String(annualTarget.RowVersion));

    private static IdpAlignmentLinkResponse ToAlignmentResponse(IdpAlignmentLink link) =>
        new(link.PublicId, link.IdpStrategicObjectiveId, link.FrameworkType.ToString(), link.FrameworkReferenceCode, link.FrameworkReferenceTitle, link.Notes, Convert.ToBase64String(link.RowVersion));

    private static IdpCommunitySessionResponse ToCommunitySessionResponse(IdpCommunitySession session) =>
        new(session.PublicId, session.IdpPlanId, session.ParticipationType.ToString(), session.SessionDate, session.Venue, session.WardId, session.Ward?.Name, session.ParticipantsCount, session.AttendanceRegisterPath, session.MinutesPath, Convert.ToBase64String(session.RowVersion));

    private static IdpCommunityNeedResponse ToCommunityNeedResponse(IdpCommunityNeed need) =>
        new(need.PublicId, need.IdpCommunitySessionId, need.IssueCategory, need.Description, need.PriorityLevel, need.ProposedIntervention, Convert.ToBase64String(need.RowVersion));

    private static IdpWardInputResponse ToWardInputResponse(IdpWardInput wardInput, string wardName) =>
        new(wardInput.PublicId, wardInput.IdpPlanId, wardInput.WardId, wardName, wardInput.WardPlanSummary, wardInput.WardPriorities, wardInput.WardProjects, Convert.ToBase64String(wardInput.RowVersion));

    private static IdpStakeholderEngagementResponse ToStakeholderResponse(IdpStakeholderEngagement stakeholder) =>
        new(stakeholder.PublicId, stakeholder.IdpCommunitySessionId, stakeholder.StakeholderType, stakeholder.StakeholderName, stakeholder.ContactPerson, stakeholder.ContactEmail, stakeholder.KeyInput, Convert.ToBase64String(stakeholder.RowVersion));

    private static IdpStakeholderEngagementPageItemResponse ToStakeholderPageResponse(
        IdpStakeholderEngagement stakeholder,
        bool includeContactPerson,
        bool includeContactEmail) =>
        new(stakeholder.PublicId, stakeholder.IdpCommunitySession.PublicId, stakeholder.IdpCommunitySession.SessionDate,
            stakeholder.IdpCommunitySession.Venue, stakeholder.StakeholderType, stakeholder.StakeholderName,
            includeContactPerson ? stakeholder.ContactPerson : null,
            includeContactEmail ? stakeholder.ContactEmail : null,
            stakeholder.KeyInput, Convert.ToBase64String(stakeholder.RowVersion));

    private static IdpRiskLinkResponse ToRiskResponse(IdpRiskLink risk) =>
        new(risk.PublicId, risk.IdpStrategicObjectiveId, risk.IdpProjectId, risk.IdpKpiId, risk.RiskReference, risk.RiskTitle, risk.MitigationPlan, risk.RiskLevel.ToString(), Convert.ToBase64String(risk.RowVersion));

    private static IdpBudgetSnapshotResponse ToBudgetSnapshotResponse(IdpBudgetSnapshot snapshot, IdpBudgetMemberAccess access) =>
        new(snapshot.PublicId, snapshot.IdpStrategicObjectiveId, snapshot.IdpProjectId, snapshot.FinancialYear,
            access.PlannedBudget ? snapshot.PlannedBudget : null,
            access.ApprovedBudget ? snapshot.ApprovedBudget : null,
            access.ActualExpenditure ? snapshot.ActualExpenditure : null,
            access.SourceSystem ? snapshot.SourceSystem : null,
            snapshot.CapturedAt);

    private IdpDocumentResponse ToDocumentResponse(IdpDocument document, DocumentMetadataMemberAccess? memberAccess = null)
    {
        memberAccess ??= DocumentMetadataMemberAccess.Full;
        var downloadUrl = document.IsActive && !document.Blob.IsContentDeleted && !document.Blob.IsQuarantined && document.Blob.SignatureVerified && document.Blob.ScanStatus == "Clean"
            ? $"{Request.Scheme}://{Request.Host}/api/v1/idp/plans/{document.IdpPlan.PublicId}/documents/{document.PublicId}/content"
            : string.Empty;
        return new(
            document.PublicId,
            document.IdpPlan.PublicId,
            document.IdpPlanVersion?.VersionNumber,
            document.Category.ToString(),
            document.Title,
            document.FileName,
            downloadUrl,
            document.Blob.ContentType,
            document.Blob.SizeInBytes,
            document.VersionNumber,
            document.IsApproved,
            document.UploadedAt,
            memberAccess.UploadedByUserId ? document.UploadedByUser?.PublicId : null,
            memberAccess.UploadedByName ? document.UploadedByUser?.FullName : null,
            document.Blob.Sha256,
            document.Blob.SignatureVerified,
            document.Blob.ScanStatus,
            document.Blob.IsQuarantined,
            memberAccess.ScannerProvider ? document.Blob.ScannerProvider : null,
            memberAccess.ScannerReference ? document.Blob.ScannerReference : null,
            memberAccess.ScanDetail ? document.Blob.ScanDetail : null,
            document.Blob.ScannedAt,
            document.RetainUntil,
            document.Blob.PublicId,
            document.Blob.IsContentDeleted,
            Convert.ToBase64String(document.RowVersion ?? []));
    }

    private static IdpCommentResponse ToCommentResponse(IdpCollaborationComment comment, ApplicationUser? commentedBy, IdpCollaborationMemberAccess access) =>
        new(comment.PublicId, comment.IdpPlanId, comment.IdpPlanVersionId,
            access.Comment ? comment.EntityName : null,
            access.Comment ? comment.EntityId : null,
            access.Comment ? comment.Comment : null,
            access.CommentActor ? commentedBy?.PublicId : null,
            access.CommentActor ? commentedBy?.FullName : null,
            comment.CommentedAt);

    private static IdpTaskResponse ToTaskResponse(IdpTaskAssignment task, ApplicationUser? assignedTo, ApplicationUser? assignedBy, IdpCollaborationMemberAccess access) =>
        new(task.PublicId, task.IdpPlanId, task.IdpPlanVersionId,
            access.TaskContent ? task.Title : null,
            access.TaskContent ? task.Description : null,
            access.TaskAssignee ? assignedTo?.PublicId : null,
            access.TaskAssignee ? assignedTo?.FullName : null,
            access.TaskAssigner ? assignedBy?.PublicId : null,
            access.TaskAssigner ? assignedBy?.FullName : null,
            task.DueDate, task.IsCompleted, task.CompletedAt, Convert.ToBase64String(task.RowVersion));

    private sealed record IdpAnnualTargetMemberAccess(bool TargetValue, bool ActualValue, bool ProgressComment)
    {
        public static IdpAnnualTargetMemberAccess Full { get; } = new(true, true, true);
    }

    private sealed record IdpBudgetMemberAccess(bool PlannedBudget, bool ApprovedBudget, bool ActualExpenditure, bool SourceSystem)
    {
        public static IdpBudgetMemberAccess Full { get; } = new(true, true, true, true);
    }

    private sealed record IdpCollaborationMemberAccess(bool Comment, bool CommentActor, bool TaskContent, bool TaskAssignee, bool TaskAssigner)
    {
        public static IdpCollaborationMemberAccess Full { get; } = new(true, true, true, true, true);
    }

    private sealed record IdpPlanVersionMemberAccess(bool Summary, bool Creator)
    {
        public static IdpPlanVersionMemberAccess Full { get; } = new(true, true);
    }

    private sealed record IdpObjectiveMemberAccess(bool StrategicOwner, bool BudgetAllocation)
    {
        public static IdpObjectiveMemberAccess Full { get; } = new(true, true);
    }

    private sealed record IdpProgrammeMemberAccess(bool PlannedBudget, bool ApprovedBudget, bool ActualExpenditure)
    {
        public static IdpProgrammeMemberAccess Full { get; } = new(true, true, true);
    }

    private sealed record IdpProjectMemberAccess(bool Budget, bool FundingSource)
    {
        public static IdpProjectMemberAccess Full { get; } = new(true, true);
    }

    private sealed class IdpHierarchyPathRow
    {
        public Guid IdpPlanPublicId { get; init; }
        public Guid OutcomePublicId { get; init; }
        public string OutcomeCode { get; init; } = string.Empty;
        public string OutcomeName { get; init; } = string.Empty;
        public Guid ObjectivePublicId { get; init; }
        public string ObjectiveCode { get; init; } = string.Empty;
        public string ObjectiveName { get; init; } = string.Empty;
        public Guid? ObjectiveStrategicOwnerPublicId { get; init; }
        public string? ObjectiveStrategicOwnerName { get; init; }
        public decimal ObjectiveBudgetAllocation { get; init; }
        public Guid PriorityPublicId { get; init; }
        public string PriorityCode { get; init; } = string.Empty;
        public string PriorityName { get; init; } = string.Empty;
        public Guid ProgrammePublicId { get; init; }
        public string ProgrammeCode { get; init; } = string.Empty;
        public string ProgrammeName { get; init; } = string.Empty;
        public decimal ProgrammePlannedBudget { get; init; }
        public decimal ProgrammeApprovedBudget { get; init; }
        public decimal ProgrammeActualExpenditure { get; init; }
        public Guid ProjectPublicId { get; init; }
        public string ProjectCode { get; init; } = string.Empty;
        public string ProjectName { get; init; } = string.Empty;
        public decimal ProjectBudget { get; init; }
        public string ProjectFundingSource { get; init; } = string.Empty;
        public Guid KpiPublicId { get; init; }
        public string KpiCode { get; init; } = string.Empty;
        public string KpiName { get; init; } = string.Empty;

        public IdpHierarchyPathResponse ToResponse(
            IdpObjectiveMemberAccess objectiveAccess,
            IdpProgrammeMemberAccess programmeAccess,
            IdpProjectMemberAccess projectAccess) => new(
            IdpPlanPublicId,
            OutcomePublicId,
            OutcomeCode,
            OutcomeName,
            ObjectivePublicId,
            ObjectiveCode,
            ObjectiveName,
            objectiveAccess.StrategicOwner ? ObjectiveStrategicOwnerPublicId : null,
            objectiveAccess.StrategicOwner ? ObjectiveStrategicOwnerName : null,
            objectiveAccess.BudgetAllocation ? ObjectiveBudgetAllocation : null,
            PriorityPublicId,
            PriorityCode,
            PriorityName,
            ProgrammePublicId,
            ProgrammeCode,
            ProgrammeName,
            programmeAccess.PlannedBudget ? ProgrammePlannedBudget : null,
            programmeAccess.ApprovedBudget ? ProgrammeApprovedBudget : null,
            programmeAccess.ActualExpenditure ? ProgrammeActualExpenditure : null,
            ProjectPublicId,
            ProjectCode,
            ProjectName,
            projectAccess.Budget ? ProjectBudget : null,
            projectAccess.FundingSource ? ProjectFundingSource : null,
            KpiPublicId,
            KpiCode,
            KpiName);
    }
}

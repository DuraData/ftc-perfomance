using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/ipms-target-library")]
[Route("api/v1/ipms-target-library")]
[Authorize]
public class IpmsTargetLibraryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;

    public IpmsTargetLibraryController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccessControlService accessControlService,
        IWorkflowGovernanceService workflowGovernanceService)
    {
        _context = context;
        _userManager = userManager;
        _accessControlService = accessControlService;
        _workflowGovernanceService = workflowGovernanceService;
    }

    [HttpGet]
    public ActionResult<ApiResponse<IpmsTargetTemplateResponse[]>> GetTemplates() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<IpmsTargetTemplateResponse[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/ipms-target-library/page."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IpmsTargetTemplateResponse>>>> GetTemplatesPage(
        [FromQuery] PagedQueryRequest page,
        [FromQuery] TargetLibraryFilterRequest filter)
    {
        if (page.NormalizedSortBy is not ("createdat" or "templatecode" or "templatename" or "targetname" or "version"))
            return BadRequest(new ApiResponse<PagedResponse<IpmsTargetTemplateResponse>>(false, null,
                "SortBy must be createdAt, templateCode, templateName, targetName, or version."));
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<IpmsTargetTemplateResponse>>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<PagedResponse<IpmsTargetTemplateResponse>>(false, null, decision.Reason));

        var query = ApplyFilters(_context.IpmsTargetTemplates.AsNoTracking(), page, filter);
        var totalCount = await query.CountAsync();
        query = (page.NormalizedSortBy, page.Descending) switch
        {
            ("templatecode", false) => query.OrderBy(item => item.TemplateCode).ThenBy(item => item.Id),
            ("templatecode", true) => query.OrderByDescending(item => item.TemplateCode).ThenByDescending(item => item.Id),
            ("templatename", false) => query.OrderBy(item => item.TemplateName).ThenBy(item => item.Id),
            ("templatename", true) => query.OrderByDescending(item => item.TemplateName).ThenByDescending(item => item.Id),
            ("targetname", false) => query.OrderBy(item => item.TargetName).ThenBy(item => item.Id),
            ("targetname", true) => query.OrderByDescending(item => item.TargetName).ThenByDescending(item => item.Id),
            ("version", false) => query.OrderBy(item => item.Version).ThenBy(item => item.TemplateCode).ThenBy(item => item.Id),
            ("version", true) => query.OrderByDescending(item => item.Version).ThenBy(item => item.TemplateCode).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedDate).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedDate).ThenByDescending(item => item.Id)
        };
        var templates = await query.Skip(page.Offset).Take(page.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<IpmsTargetTemplateResponse>>(true,
            PagedResponse<IpmsTargetTemplateResponse>.Create(templates.Select(item => item.ToResponse()), page.Page, page.PageSize, totalCount)));
    }

    [HttpGet("facets")]
    public async Task<ActionResult<ApiResponse<TargetLibraryFacetsResponse>>> GetTemplateFacets()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TargetLibraryFacetsResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<TargetLibraryFacetsResponse>(false, null, decision.Reason));

        var query = _context.IpmsTargetTemplates.AsNoTracking();
        var primaryAreas = await query.Where(item => item.PerformanceArea != null && item.PerformanceArea != "")
            .Select(item => item.PerformanceArea!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var functionalAreas = await query.Where(item => item.FunctionalArea != null && item.FunctionalArea != "")
            .Select(item => item.FunctionalArea!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var classifications = await query.Where(item => item.EmployeeLevel != null && item.EmployeeLevel != "")
            .Select(item => item.EmployeeLevel!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var unitTypes = await query.Select(item => item.TargetUnitType).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var versions = await query.Select(item => item.Version).Distinct().OrderByDescending(item => item).Take(100).ToArrayAsync();
        return Ok(new ApiResponse<TargetLibraryFacetsResponse>(true,
            new(primaryAreas, functionalAreas, classifications, unitTypes, versions)));
    }

    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<IpmsTargetTemplateResponse>> GetTemplate(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<IpmsTargetTemplateResponse>(false, null,
            "Integer template identifiers are retired. Use GET /api/v1/ipms-target-library/{publicId}."));

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> GetTemplate(Guid publicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.IpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.PublicId == publicId);
        return template == null
            ? NotFound(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "IPMS target template not found"))
            : Ok(new ApiResponse<IpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> CreateTemplate([FromBody] SaveIpmsTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Create");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var exists = await _context.IpmsTargetTemplates.AnyAsync(item => item.TemplateCode == request.TemplateCode);
        if (exists) return Conflict(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "Template code already exists"));

        var template = Apply(new IpmsTargetTemplate(), request, user.UserName ?? user.Email ?? user.Id);
        _context.IpmsTargetTemplates.Add(template);
        await _context.SaveChangesAsync();
        await AddVersionAsync(template, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", template.PublicId.ToString(), "Create", null, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<IpmsTargetTemplateResponse>> UpdateTemplate(int id, [FromBody] SaveIpmsTargetTemplateRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<IpmsTargetTemplateResponse>(false, null,
            "Integer template identifiers are retired. Use PUT /api/v1/ipms-target-library/{publicId} with RowVersion."));

    [HttpPut("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> UpdateTemplate(Guid publicId, [FromBody] SaveIpmsTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Edit");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.IpmsTargetTemplates.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (template == null) return NotFound(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "IPMS target template not found"));
        if (!TrySetExpectedVersion(template, request.RowVersion))
            return BadRequest(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "A valid RowVersion is required."));
        var codeConflict = await _context.IpmsTargetTemplates.AnyAsync(item => item.PublicId != publicId && item.TemplateCode == request.TemplateCode);
        if (codeConflict) return Conflict(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "Template code already exists"));

        var oldValue = template.ToResponse();
        template = Apply(template, request, user.UserName ?? user.Email ?? user.Id);
        template.Version += 1;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "The IPMS target template changed. Reload and retry."));
        }
        await AddVersionAsync(template, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", template.PublicId.ToString(), "Edit", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpDelete("{id:int}")]
    public ActionResult<ApiResponse<bool>> ArchiveTemplate(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false,
            "Integer archive routes are retired. Use POST /api/v1/ipms-target-library/{publicId}/archive with RowVersion."));

    [HttpPost("{publicId:guid}/archive")]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveTemplate(Guid publicId, [FromBody] ArchiveTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Delete");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var template = await _context.IpmsTargetTemplates.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (template == null) return NotFound(new ApiResponse<bool>(false, false, "IPMS target template not found"));
        if (!TrySetExpectedVersion(template, request.RowVersion))
            return BadRequest(new ApiResponse<bool>(false, false, "A valid RowVersion is required."));

        var oldValue = template.ToResponse();
        template.IsArchived = true;
        template.IsActive = false;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<bool>(false, false, "The IPMS target template changed. Reload and retry."));
        }
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", template.PublicId.ToString(), "Archive", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id:int}/duplicate")]
    public ActionResult<ApiResponse<IpmsTargetTemplateResponse>> DuplicateTemplate(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<IpmsTargetTemplateResponse>(false, null,
            "Integer template identifiers are retired. Use POST /api/v1/ipms-target-library/{publicId}/duplicate."));

    [HttpPost("{publicId:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> DuplicateTemplate(Guid publicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Duplicate");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.IpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (template == null) return NotFound(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "IPMS target template not found"));

        var duplicate = new IpmsTargetTemplate
        {
            TemplateCode = $"{template.TemplateCode}-COPY",
            TemplateName = $"{template.TemplateName} (Copy)",
            TargetName = template.TargetName,
            KpiDescription = template.KpiDescription,
            PerformanceArea = template.PerformanceArea,
            EmployeeLevel = template.EmployeeLevel,
            JobGrade = template.JobGrade,
            TargetUnitType = template.TargetUnitType,
            UnitOfMeasure = template.UnitOfMeasure,
            AnnualTarget = template.AnnualTarget,
            AnnualTargetDescription = template.AnnualTargetDescription,
            Weight = template.Weight,
            DefaultRatingMethod = template.DefaultRatingMethod,
            DefaultScoreScale = template.DefaultScoreScale,
            DefaultPoeRequirements = template.DefaultPoeRequirements,
            DefaultTaskTemplatesJson = template.DefaultTaskTemplatesJson,
            LinkedOpmsTargetRequired = template.LinkedOpmsTargetRequired,
            FunctionalArea = template.FunctionalArea,
            IsActive = true,
            IsArchived = false,
            Version = 1,
            CreatedBy = user.UserName ?? user.Email ?? user.Id,
            CreatedDate = DateTime.UtcNow
        };

        _context.IpmsTargetTemplates.Add(duplicate);
        await _context.SaveChangesAsync();
        await AddVersionAsync(duplicate, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", duplicate.PublicId.ToString(), "Duplicate", template.ToResponse(), duplicate.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsTargetTemplateResponse>(true, duplicate.ToResponse()));
    }

    private static IpmsTargetTemplate Apply(IpmsTargetTemplate entity, SaveIpmsTargetTemplateRequest request, string actor)
    {
        entity.TemplateCode = request.TemplateCode.Trim();
        entity.TemplateName = request.TemplateName.Trim();
        entity.TargetName = request.TargetName.Trim();
        entity.KpiDescription = request.KpiDescription.Trim();
        entity.PerformanceArea = request.PerformanceArea?.Trim();
        entity.EmployeeLevel = request.EmployeeLevel?.Trim();
        entity.JobGrade = request.JobGrade?.Trim();
        entity.TargetUnitType = request.TargetUnitType.Trim();
        entity.UnitOfMeasure = request.UnitOfMeasure?.Trim();
        entity.AnnualTarget = request.AnnualTarget;
        entity.AnnualTargetDescription = request.AnnualTargetDescription?.Trim();
        entity.Weight = request.Weight;
        entity.DefaultRatingMethod = request.DefaultRatingMethod?.Trim();
        entity.DefaultScoreScale = request.DefaultScoreScale?.Trim();
        entity.DefaultPoeRequirements = request.DefaultPoeRequirements?.Trim();
        entity.DefaultTaskTemplatesJson = request.DefaultTaskTemplatesJson;
        entity.LinkedOpmsTargetRequired = request.LinkedOpmsTargetRequired;
        entity.FunctionalArea = request.FunctionalArea?.Trim();
        entity.IsActive = request.IsActive;
        entity.IsArchived = !request.IsActive;
        entity.CreatedBy ??= actor;
        return entity;
    }

    private static IQueryable<IpmsTargetTemplate> ApplyFilters(
        IQueryable<IpmsTargetTemplate> query,
        PagedQueryRequest page,
        TargetLibraryFilterRequest filter)
    {
        if (page.NormalizedSearch.Length > 0)
        {
            var search = page.NormalizedSearch;
            query = query.Where(item => item.TemplateCode.Contains(search) || item.TemplateName.Contains(search)
                || item.TargetName.Contains(search) || item.KpiDescription.Contains(search));
        }
        query = filter.NormalizedStatus switch
        {
            "active" => query.Where(item => item.IsActive && !item.IsArchived),
            "archived" => query.Where(item => item.IsArchived),
            _ => query
        };
        if (filter.NormalizedPrimaryArea.Length > 0)
        {
            var value = filter.NormalizedPrimaryArea;
            query = query.Where(item => item.PerformanceArea == value);
        }
        if (filter.NormalizedFunctionalArea.Length > 0)
        {
            var value = filter.NormalizedFunctionalArea;
            query = query.Where(item => item.FunctionalArea == value);
        }
        if (filter.NormalizedClassification.Length > 0)
        {
            var value = filter.NormalizedClassification;
            query = query.Where(item => item.EmployeeLevel == value);
        }
        if (filter.NormalizedTargetUnitType.Length > 0)
        {
            var value = filter.NormalizedTargetUnitType;
            query = query.Where(item => item.TargetUnitType == value);
        }
        if (filter.Version.HasValue) query = query.Where(item => item.Version == filter.Version.Value);
        return query;
    }

    private async Task AddVersionAsync(IpmsTargetTemplate template, string actor)
    {
        _context.IpmsTargetTemplateVersions.Add(new IpmsTargetTemplateVersion
        {
            IpmsTargetTemplateId = template.Id,
            Version = template.Version,
            SnapshotJson = JsonSerializer.Serialize(template.ToResponse()),
            CreatedBy = actor,
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private bool TrySetExpectedVersion(IpmsTargetTemplate template, string? encoded)
    {
        try
        {
            var expected = Convert.FromBase64String(encoded ?? string.Empty);
            if (expected.Length == 0) return false;
            _context.Entry(template).Property(item => item.RowVersion).OriginalValue = expected;
            return true;
        }
        catch (FormatException) { return false; }
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }
}

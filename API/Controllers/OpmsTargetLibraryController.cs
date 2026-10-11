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
[Route("api/v1/opms-target-library")]
[Authorize]
public class OpmsTargetLibraryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;

    public OpmsTargetLibraryController(
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
    public ActionResult<ApiResponse<OpmsTargetTemplateResponse[]>> GetTemplates() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<OpmsTargetTemplateResponse[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/opms-target-library/page."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<OpmsTargetTemplateResponse>>>> GetTemplatesPage(
        [FromQuery] PagedQueryRequest page,
        [FromQuery] TargetLibraryFilterRequest filter)
    {
        if (page.NormalizedSortBy is not ("createdat" or "templatecode" or "templatename" or "targetname" or "version"))
            return BadRequest(new ApiResponse<PagedResponse<OpmsTargetTemplateResponse>>(false, null,
                "SortBy must be createdAt, templateCode, templateName, targetName, or version."));
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<OpmsTargetTemplateResponse>>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<PagedResponse<OpmsTargetTemplateResponse>>(false, null, decision.Reason));

        var query = ApplyFilters(_context.OpmsTargetTemplates.AsNoTracking(), page, filter);
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
        return Ok(new ApiResponse<PagedResponse<OpmsTargetTemplateResponse>>(true,
            PagedResponse<OpmsTargetTemplateResponse>.Create(templates.Select(item => item.ToResponse()), page.Page, page.PageSize, totalCount)));
    }

    [HttpGet("facets")]
    public async Task<ActionResult<ApiResponse<TargetLibraryFacetsResponse>>> GetTemplateFacets()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<TargetLibraryFacetsResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponse<TargetLibraryFacetsResponse>(false, null, decision.Reason));

        var query = _context.OpmsTargetTemplates.AsNoTracking();
        var national = await query.Where(item => item.NationalKpa != null && item.NationalKpa != "")
            .Select(item => item.NationalKpa!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var municipal = await query.Where(item => item.MunicipalKpa != null && item.MunicipalKpa != "")
            .Select(item => item.MunicipalKpa!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var functionalAreas = await query.Where(item => item.FunctionalArea != null && item.FunctionalArea != "")
            .Select(item => item.FunctionalArea!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var classifications = await query.Where(item => item.KpiType != null && item.KpiType != "")
            .Select(item => item.KpiType!).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var unitTypes = await query.Select(item => item.TargetUnitType).Distinct().OrderBy(item => item).Take(100).ToArrayAsync();
        var versions = await query.Select(item => item.Version).Distinct().OrderByDescending(item => item).Take(100).ToArrayAsync();
        return Ok(new ApiResponse<TargetLibraryFacetsResponse>(true, new(
            national.Concat(municipal).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item).Take(100).ToArray(),
            functionalAreas, classifications, unitTypes, versions)));
    }

    [HttpGet("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<OpmsTargetTemplateResponse>> GetTemplate(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<OpmsTargetTemplateResponse>(false, null,
            "Integer template identifiers are retired. Use GET /api/v1/opms-target-library/{publicId}."));

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> GetTemplate(Guid publicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.OpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.PublicId == publicId);
        return template == null
            ? NotFound(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "OPMS target template not found"))
            : Ok(new ApiResponse<OpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> CreateTemplate([FromBody] SaveOpmsTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Create");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var exists = await _context.OpmsTargetTemplates.AnyAsync(item => item.TemplateCode == request.TemplateCode);
        if (exists) return Conflict(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "Template code already exists"));

        var template = Apply(new OpmsTargetTemplate(), request, user.UserName ?? user.Email ?? user.Id);
        _context.OpmsTargetTemplates.Add(template);
        await _context.SaveChangesAsync();
        await AddVersionAsync(template, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", template.PublicId.ToString(), "Create", null, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<OpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpPut("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<OpmsTargetTemplateResponse>> UpdateTemplate(int id, [FromBody] SaveOpmsTargetTemplateRequest request) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<OpmsTargetTemplateResponse>(false, null,
            "Integer template identifiers are retired. Use PUT /api/v1/opms-target-library/{publicId} with RowVersion."));

    [HttpPut("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> UpdateTemplate(Guid publicId, [FromBody] SaveOpmsTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Edit");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.OpmsTargetTemplates.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (template == null) return NotFound(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "OPMS target template not found"));
        if (!TrySetExpectedVersion(template, request.RowVersion))
            return BadRequest(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "A valid RowVersion is required."));
        var codeConflict = await _context.OpmsTargetTemplates.AnyAsync(item => item.PublicId != publicId && item.TemplateCode == request.TemplateCode);
        if (codeConflict) return Conflict(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "Template code already exists"));

        var oldValue = template.ToResponse();
        template = Apply(template, request, user.UserName ?? user.Email ?? user.Id);
        template.Version += 1;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "The OPMS target template changed. Reload and retry."));
        }
        await AddVersionAsync(template, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", template.PublicId.ToString(), "Edit", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<OpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpDelete("{id:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<bool>> ArchiveTemplate(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false,
            "Integer archive routes are retired. Use POST /api/v1/opms-target-library/{publicId}/archive with RowVersion."));

    [HttpPost("{publicId:guid}/archive")]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveTemplate(Guid publicId, [FromBody] ArchiveTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Delete");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var template = await _context.OpmsTargetTemplates.FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (template == null) return NotFound(new ApiResponse<bool>(false, false, "OPMS target template not found"));
        if (!TrySetExpectedVersion(template, request.RowVersion))
            return BadRequest(new ApiResponse<bool>(false, false, "A valid RowVersion is required."));

        var oldValue = template.ToResponse();
        template.IsArchived = true;
        template.IsActive = false;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<bool>(false, false, "The OPMS target template changed. Reload and retry."));
        }
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", template.PublicId.ToString(), "Archive", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id:int}/duplicate")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<OpmsTargetTemplateResponse>> DuplicateTemplate(int id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<OpmsTargetTemplateResponse>(false, null,
            "Integer template identifiers are retired. Use POST /api/v1/opms-target-library/{publicId}/duplicate."));

    [HttpPost("{publicId:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> DuplicateTemplate(Guid publicId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Duplicate");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.OpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.PublicId == publicId);
        if (template == null) return NotFound(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "OPMS target template not found"));

        var duplicate = new OpmsTargetTemplate
        {
            TemplateCode = $"{template.TemplateCode}-COPY",
            TemplateName = $"{template.TemplateName} (Copy)",
            IndicatorNumber = template.IndicatorNumber,
            TargetName = template.TargetName,
            KpiDescription = template.KpiDescription,
            Baseline = template.Baseline,
            AnnualTarget = template.AnnualTarget,
            AnnualTargetDescription = template.AnnualTargetDescription,
            TargetUnitType = template.TargetUnitType,
            UnitOfMeasure = template.UnitOfMeasure,
            NationalKpa = template.NationalKpa,
            MunicipalKpa = template.MunicipalKpa,
            StrategicGoal = template.StrategicGoal,
            StrategicObjective = template.StrategicObjective,
            PerformanceObjective = template.PerformanceObjective,
            Outcome = template.Outcome,
            Output = template.Output,
            PriorityIssue = template.PriorityIssue,
            BudgetSource = template.BudgetSource,
            BudgetType = template.BudgetType,
            Weight = template.Weight,
            KpiType = template.KpiType,
            IndicatorType = template.IndicatorType,
            FunctionalArea = template.FunctionalArea,
            StandardClassification = template.StandardClassification,
            IdpReference = template.IdpReference,
            InternalReference = template.InternalReference,
            FmsLink = template.FmsLink,
            DefaultQuarterlyTargetsJson = template.DefaultQuarterlyTargetsJson,
            DefaultBudgetInformation = template.DefaultBudgetInformation,
            DefaultPoeRequirements = template.DefaultPoeRequirements,
            IsActive = true,
            IsArchived = false,
            Version = 1,
            CreatedBy = user.UserName ?? user.Email ?? user.Id,
            CreatedDate = DateTime.UtcNow
        };

        _context.OpmsTargetTemplates.Add(duplicate);
        await _context.SaveChangesAsync();
        await AddVersionAsync(duplicate, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", duplicate.PublicId.ToString(), "Duplicate", template.ToResponse(), duplicate.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsTargetTemplateResponse>(true, duplicate.ToResponse()));
    }

    private static OpmsTargetTemplate Apply(OpmsTargetTemplate entity, SaveOpmsTargetTemplateRequest request, string actor)
    {
        entity.TemplateCode = request.TemplateCode.Trim();
        entity.TemplateName = request.TemplateName.Trim();
        entity.IndicatorNumber = request.IndicatorNumber.Trim();
        entity.TargetName = request.TargetName.Trim();
        entity.KpiDescription = request.KpiDescription.Trim();
        entity.Baseline = request.Baseline;
        entity.AnnualTarget = request.AnnualTarget;
        entity.AnnualTargetDescription = request.AnnualTargetDescription?.Trim();
        entity.TargetUnitType = request.TargetUnitType.Trim();
        entity.UnitOfMeasure = request.UnitOfMeasure?.Trim();
        entity.NationalKpa = request.NationalKpa?.Trim();
        entity.MunicipalKpa = request.MunicipalKpa?.Trim();
        entity.StrategicGoal = request.StrategicGoal?.Trim();
        entity.StrategicObjective = request.StrategicObjective?.Trim();
        entity.PerformanceObjective = request.PerformanceObjective?.Trim();
        entity.Outcome = request.Outcome?.Trim();
        entity.Output = request.Output?.Trim();
        entity.PriorityIssue = request.PriorityIssue?.Trim();
        entity.BudgetSource = request.BudgetSource?.Trim();
        entity.BudgetType = request.BudgetType?.Trim();
        entity.Weight = request.Weight;
        entity.KpiType = request.KpiType?.Trim();
        entity.IndicatorType = request.IndicatorType?.Trim();
        entity.FunctionalArea = request.FunctionalArea?.Trim();
        entity.StandardClassification = request.StandardClassification?.Trim();
        entity.IdpReference = request.IdpReference?.Trim();
        entity.InternalReference = request.InternalReference?.Trim();
        entity.FmsLink = request.FmsLink?.Trim();
        entity.DefaultQuarterlyTargetsJson = request.DefaultQuarterlyTargetsJson;
        entity.DefaultBudgetInformation = request.DefaultBudgetInformation?.Trim();
        entity.DefaultPoeRequirements = request.DefaultPoeRequirements?.Trim();
        entity.IsActive = request.IsActive;
        entity.IsArchived = !request.IsActive;
        entity.CreatedBy ??= actor;
        return entity;
    }

    private static IQueryable<OpmsTargetTemplate> ApplyFilters(
        IQueryable<OpmsTargetTemplate> query,
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
            query = query.Where(item => item.NationalKpa == value || item.MunicipalKpa == value);
        }
        if (filter.NormalizedFunctionalArea.Length > 0)
        {
            var value = filter.NormalizedFunctionalArea;
            query = query.Where(item => item.FunctionalArea == value);
        }
        if (filter.NormalizedClassification.Length > 0)
        {
            var value = filter.NormalizedClassification;
            query = query.Where(item => item.KpiType == value);
        }
        if (filter.NormalizedTargetUnitType.Length > 0)
        {
            var value = filter.NormalizedTargetUnitType;
            query = query.Where(item => item.TargetUnitType == value);
        }
        if (filter.Version.HasValue) query = query.Where(item => item.Version == filter.Version.Value);
        return query;
    }

    private async Task AddVersionAsync(OpmsTargetTemplate template, string actor)
    {
        _context.OpmsTargetTemplateVersions.Add(new OpmsTargetTemplateVersion
        {
            OpmsTargetTemplateId = template.Id,
            Version = template.Version,
            SnapshotJson = JsonSerializer.Serialize(template.ToResponse()),
            CreatedBy = actor,
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private bool TrySetExpectedVersion(OpmsTargetTemplate template, string? encoded)
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

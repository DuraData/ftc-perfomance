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
[Route("api/opms-target-library")]
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
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse[]>>> GetTemplates()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse[]>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse[]>(false, null, decision.Reason));

        var templates = await _context.OpmsTargetTemplates
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedDate)
            .ToArrayAsync();

        return Ok(new ApiResponse<OpmsTargetTemplateResponse[]>(true, templates.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> GetTemplate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.OpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
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
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", template.Id.ToString(), "Create", null, template, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<OpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> UpdateTemplate(int id, [FromBody] SaveOpmsTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Edit");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.OpmsTargetTemplates.FirstOrDefaultAsync(item => item.Id == id);
        if (template == null) return NotFound(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "OPMS target template not found"));
        var codeConflict = await _context.OpmsTargetTemplates.AnyAsync(item => item.Id != id && item.TemplateCode == request.TemplateCode);
        if (codeConflict) return Conflict(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "Template code already exists"));

        var oldValue = template.ToResponse();
        template = Apply(template, request, user.UserName ?? user.Email ?? user.Id);
        template.Version += 1;
        await _context.SaveChangesAsync();
        await AddVersionAsync(template, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", template.Id.ToString(), "Edit", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<OpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveTemplate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Delete");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var template = await _context.OpmsTargetTemplates.FirstOrDefaultAsync(item => item.Id == id);
        if (template == null) return NotFound(new ApiResponse<bool>(false, false, "OPMS target template not found"));

        var oldValue = template.ToResponse();
        template.IsArchived = true;
        template.IsActive = false;
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", template.Id.ToString(), "Archive", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<ApiResponse<OpmsTargetTemplateResponse>>> DuplicateTemplate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Library.Duplicate");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.OpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
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
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTargetTemplate", duplicate.Id.ToString(), "Duplicate", template.ToResponse(), duplicate.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
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

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }
}

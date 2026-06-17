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
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse[]>>> GetTemplates()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse[]>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse[]>(false, null, decision.Reason));

        var templates = await _context.IpmsTargetTemplates.AsNoTracking().OrderByDescending(item => item.CreatedDate).ToArrayAsync();
        return Ok(new ApiResponse<IpmsTargetTemplateResponse[]>(true, templates.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> GetTemplate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.View");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.IpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
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
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", template.Id.ToString(), "Create", null, template, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> UpdateTemplate(int id, [FromBody] SaveIpmsTargetTemplateRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Edit");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.IpmsTargetTemplates.FirstOrDefaultAsync(item => item.Id == id);
        if (template == null) return NotFound(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "IPMS target template not found"));
        var codeConflict = await _context.IpmsTargetTemplates.AnyAsync(item => item.Id != id && item.TemplateCode == request.TemplateCode);
        if (codeConflict) return Conflict(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "Template code already exists"));

        var oldValue = template.ToResponse();
        template = Apply(template, request, user.UserName ?? user.Email ?? user.Id);
        template.Version += 1;
        await _context.SaveChangesAsync();
        await AddVersionAsync(template, user.UserName ?? user.Email ?? user.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", template.Id.ToString(), "Edit", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IpmsTargetTemplateResponse>(true, template.ToResponse()));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveTemplate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Delete");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var template = await _context.IpmsTargetTemplates.FirstOrDefaultAsync(item => item.Id == id);
        if (template == null) return NotFound(new ApiResponse<bool>(false, false, "IPMS target template not found"));

        var oldValue = template.ToResponse();
        template.IsArchived = true;
        template.IsActive = false;
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", template.Id.ToString(), "Archive", oldValue, template.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<ApiResponse<IpmsTargetTemplateResponse>>> DuplicateTemplate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetTemplateResponse>(false, null, "User not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Library.Duplicate");
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetTemplateResponse>(false, null, decision.Reason));

        var template = await _context.IpmsTargetTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
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
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTargetTemplate", duplicate.Id.ToString(), "Duplicate", template.ToResponse(), duplicate.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
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

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }
}

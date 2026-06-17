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
[Route("api/opms-targets")]
[Authorize]
public class OpmsTargetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;

    public OpmsTargetsController(
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
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse[]>>> GetTargets()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse[]>(false, null, "User not found"));

        var targets = await _context.OpmsTargets
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var visible = new List<OpmsTargetResponse>();
        foreach (var target in targets)
        {
            var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Targets.View", BuildScope(target));
            if (decision.Allowed)
            {
                visible.Add(target.ToResponse());
            }
        }

        return Ok(new ApiResponse<OpmsTargetResponse[]>(true, visible.ToArray()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse>>> GetTarget(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse>(false, null, "User not found"));

        var target = await FindTargetAsync(id);
        if (target == null) return NotFound(new ApiResponse<OpmsTargetResponse>(false, null, "OPMS target not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Targets.View", BuildScope(target));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));

        return Ok(new ApiResponse<OpmsTargetResponse>(true, target.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse>>> CreateTarget([FromBody] SaveOpmsTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse>(false, null, "User not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Targets.Create", new AccessScopeContext(request.DepartmentId, request.UnitId, null, request.KpiId));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));

        var entity = new OpmsTarget
        {
            IndicatorNumber = request.IndicatorNumber.Trim(),
            TargetName = request.TargetName.Trim(),
            KpiDescription = request.KpiDescription.Trim(),
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            AssignedUserId = request.AssignedUserId,
            KpiId = request.KpiId,
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            Baseline = request.Baseline,
            AnnualTarget = request.AnnualTarget,
            Weight = request.Weight,
            IsArchived = request.IsArchived,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpmsTargets.Add(entity);
        await _context.SaveChangesAsync();
        entity = await FindTargetAsync(entity.Id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTarget", entity.Id, "Create", null, entity.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        if (!string.IsNullOrWhiteSpace(entity.AssignedUserId))
        {
            await _workflowGovernanceService.CreateNotificationAsync(entity.AssignedUserId, NotificationType.Submission, "OPMS target assigned", $"You have been assigned OPMS target '{entity.TargetName}'.", "OpmsTarget", entity.Id);
        }

        return Ok(new ApiResponse<OpmsTargetResponse>(true, entity.ToResponse()));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse>>> UpdateTarget(string id, [FromBody] SaveOpmsTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse>(false, null, "User not found"));

        var entity = await _context.OpmsTargets.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsTargetResponse>(false, null, "OPMS target not found"));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Targets.Edit", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));

        entity.IndicatorNumber = request.IndicatorNumber.Trim();
        entity.TargetName = request.TargetName.Trim();
        entity.KpiDescription = request.KpiDescription.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.UnitId = request.UnitId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.KpiId = request.KpiId;
        entity.SourceTemplateId = request.SourceTemplateId;
        entity.SourceTemplateVersion = request.SourceTemplateVersion;
        entity.Baseline = request.Baseline;
        entity.AnnualTarget = request.AnnualTarget;
        entity.Weight = request.Weight;
        entity.IsArchived = request.IsArchived;
        await _context.SaveChangesAsync();

        var after = await FindTargetAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTarget", id, "Edit", before?.ToResponse(), after.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsTargetResponse>(true, after.ToResponse()));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTarget(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));

        var entity = await _context.OpmsTargets.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<bool>(false, false, "OPMS target not found"));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Targets.Delete", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        _context.OpmsTargets.Remove(entity);
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTarget", id, "Delete", before?.ToResponse(), null, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private Task<OpmsTarget?> FindTargetAsync(string id)
    {
        return _context.OpmsTargets
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    private static AccessScopeContext BuildScope(OpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.Id, target.KpiId);
}

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
[Route("api/ipms-targets")]
[Authorize]
public class IpmsTargetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;

    public IpmsTargetsController(
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
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse[]>>> GetTargets()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse[]>(false, null, "User not found"));

        var targets = await _context.IpmsTargets
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var visible = new List<IpmsTargetResponse>();
        foreach (var target in targets)
        {
            var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Targets.View", BuildScope(target));
            if (decision.Allowed)
            {
                visible.Add(target.ToResponse());
            }
        }

        return Ok(new ApiResponse<IpmsTargetResponse[]>(true, visible.ToArray()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> GetTarget(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));

        var target = await FindTargetAsync(id);
        if (target == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Targets.View", BuildScope(target));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));

        return Ok(new ApiResponse<IpmsTargetResponse>(true, target.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> CreateTarget([FromBody] SaveIpmsTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Targets.Create", new AccessScopeContext(request.DepartmentId, request.UnitId, null, request.KpiId));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));

        var entity = new IpmsTarget
        {
            IndicatorNumber = request.IndicatorNumber.Trim(),
            TargetName = request.TargetName.Trim(),
            KpiDescription = request.KpiDescription.Trim(),
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            AssignedUserId = request.AssignedUserId,
            RelatedOpmsTargetId = request.RelatedOpmsTargetId,
            KpiId = request.KpiId,
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            AnnualTarget = request.AnnualTarget,
            Weight = request.Weight,
            IsArchived = request.IsArchived,
            CreatedAt = DateTime.UtcNow
        };

        _context.IpmsTargets.Add(entity);
        await _context.SaveChangesAsync();
        entity = await FindTargetAsync(entity.Id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", entity.Id, "Create", null, entity.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        if (!string.IsNullOrWhiteSpace(entity.AssignedUserId))
        {
            await _workflowGovernanceService.CreateNotificationAsync(entity.AssignedUserId, NotificationType.Submission, "IPMS target assigned", $"You have been assigned IPMS target '{entity.TargetName}'.", "IpmsTarget", entity.Id);
        }

        return Ok(new ApiResponse<IpmsTargetResponse>(true, entity.ToResponse()));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> UpdateTarget(string id, [FromBody] SaveIpmsTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));

        var entity = await _context.IpmsTargets.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Targets.Edit", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));

        entity.IndicatorNumber = request.IndicatorNumber.Trim();
        entity.TargetName = request.TargetName.Trim();
        entity.KpiDescription = request.KpiDescription.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.UnitId = request.UnitId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.RelatedOpmsTargetId = request.RelatedOpmsTargetId;
        entity.KpiId = request.KpiId;
        entity.SourceTemplateId = request.SourceTemplateId;
        entity.SourceTemplateVersion = request.SourceTemplateVersion;
        entity.AnnualTarget = request.AnnualTarget;
        entity.Weight = request.Weight;
        entity.IsArchived = request.IsArchived;
        await _context.SaveChangesAsync();

        var after = await FindTargetAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", id, "Edit", before?.ToResponse(), after.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsTargetResponse>(true, after.ToResponse()));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTarget(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));

        var entity = await _context.IpmsTargets.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<bool>(false, false, "IPMS target not found"));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Targets.Delete", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        _context.IpmsTargets.Remove(entity);
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", id, "Delete", before?.ToResponse(), null, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private Task<IpmsTarget?> FindTargetAsync(string id)
    {
        return _context.IpmsTargets
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    private static AccessScopeContext BuildScope(IpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.Id, target.KpiId);
}

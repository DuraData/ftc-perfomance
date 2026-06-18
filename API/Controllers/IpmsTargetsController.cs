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

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS.Targets.Create", new AccessScopeContext(request.DepartmentId, request.UnitId, null, null));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));

        var entity = new IpmsTarget
        {
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            RelatedOpmsTargetId = request.RelatedOpmsTargetId,
            PeriodId = request.PeriodId,
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            AssignedUserId = request.AssignedUserId,
            SupervisorId = request.SupervisorId,
            IndicatorNumber = request.IndicatorNumber.Trim(),
            NationalKpa = request.NationalKpa,
            MunicipalKpa = request.MunicipalKpa,
            StrategicGoalId = request.StrategicGoalId,
            StrategicObjectiveId = request.StrategicObjectiveId,
            PerformanceObjective = request.PerformanceObjective,
            TargetName = request.TargetName.Trim(),
            KpiDescription = request.KpiDescription.Trim(),
            Baseline = request.Baseline,
            AnnualTarget = request.AnnualTarget,
            AnnualTargetDescription = request.AnnualTargetDescription,
            BudgetSourceId = request.BudgetSourceId,
            BudgetTypeId = request.BudgetTypeId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            Weight = request.Weight,
            KpiType = request.KpiType,
            IndicatorType = request.IndicatorType,
            FunctionalArea = request.FunctionalArea,
            IdpReference = request.IdpReference,
            InternalReference = request.InternalReference,
            IsRevised = request.IsRevised,
            TargetUnitType = request.TargetUnitType,
            Q1Target = request.Q1Target,
            Q1Description = request.Q1Description,
            Q1Budget = request.Q1Budget,
            Q2Target = request.Q2Target,
            Q2Description = request.Q2Description,
            Q2Budget = request.Q2Budget,
            MidTermTarget = request.MidTermTarget,
            MidTermDescription = request.MidTermDescription,
            MidTermBudget = request.MidTermBudget,
            Q3Target = request.Q3Target,
            Q3Description = request.Q3Description,
            Q3Budget = request.Q3Budget,
            Q3RevisedTarget = request.Q3RevisedTarget,
            Q4Target = request.Q4Target,
            Q4Description = request.Q4Description,
            Q4Budget = request.Q4Budget,
            Q4RevisedTarget = request.Q4RevisedTarget,
            RevisedAnnualTarget = request.RevisedAnnualTarget,
            RevisedAnnualBudget = request.RevisedAnnualBudget,
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

        entity.SourceTemplateId = request.SourceTemplateId;
        entity.SourceTemplateVersion = request.SourceTemplateVersion;
        entity.RelatedOpmsTargetId = request.RelatedOpmsTargetId;
        entity.PeriodId = request.PeriodId;
        entity.DepartmentId = request.DepartmentId;
        entity.UnitId = request.UnitId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.SupervisorId = request.SupervisorId;
        entity.IndicatorNumber = request.IndicatorNumber.Trim();
        entity.NationalKpa = request.NationalKpa;
        entity.MunicipalKpa = request.MunicipalKpa;
        entity.StrategicGoalId = request.StrategicGoalId;
        entity.StrategicObjectiveId = request.StrategicObjectiveId;
        entity.PerformanceObjective = request.PerformanceObjective;
        entity.TargetName = request.TargetName.Trim();
        entity.KpiDescription = request.KpiDescription.Trim();
        entity.Baseline = request.Baseline;
        entity.AnnualTarget = request.AnnualTarget;
        entity.AnnualTargetDescription = request.AnnualTargetDescription;
        entity.BudgetSourceId = request.BudgetSourceId;
        entity.BudgetTypeId = request.BudgetTypeId;
        entity.UnitOfMeasureId = request.UnitOfMeasureId;
        entity.Weight = request.Weight;
        entity.KpiType = request.KpiType;
        entity.IndicatorType = request.IndicatorType;
        entity.FunctionalArea = request.FunctionalArea;
        entity.IdpReference = request.IdpReference;
        entity.InternalReference = request.InternalReference;
        entity.IsRevised = request.IsRevised;
        entity.TargetUnitType = request.TargetUnitType;
        entity.Q1Target = request.Q1Target;
        entity.Q1Description = request.Q1Description;
        entity.Q1Budget = request.Q1Budget;
        entity.Q2Target = request.Q2Target;
        entity.Q2Description = request.Q2Description;
        entity.Q2Budget = request.Q2Budget;
        entity.MidTermTarget = request.MidTermTarget;
        entity.MidTermDescription = request.MidTermDescription;
        entity.MidTermBudget = request.MidTermBudget;
        entity.Q3Target = request.Q3Target;
        entity.Q3Description = request.Q3Description;
        entity.Q3Budget = request.Q3Budget;
        entity.Q3RevisedTarget = request.Q3RevisedTarget;
        entity.Q4Target = request.Q4Target;
        entity.Q4Description = request.Q4Description;
        entity.Q4Budget = request.Q4Budget;
        entity.Q4RevisedTarget = request.Q4RevisedTarget;
        entity.RevisedAnnualTarget = request.RevisedAnnualTarget;
        entity.RevisedAnnualBudget = request.RevisedAnnualBudget;
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
        new(target.DepartmentId, target.UnitId, target.Id, null);
}

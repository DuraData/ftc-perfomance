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

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Targets.Create", new AccessScopeContext(request.DepartmentId, request.UnitId, null, null));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));

        var entity = new OpmsTarget
        {
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            PeriodId = request.PeriodId,
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            AssignedUserId = request.AssignedUserId,
            WardIds = request.WardIds,
            AdditionalAssigneeIds = request.AdditionalAssigneeIds,
            VoteNumberIds = request.VoteNumberIds,
            IndicatorNumber = request.IndicatorNumber.Trim(),
            NationalKpa = request.NationalKpa,
            MunicipalKpa = request.MunicipalKpa,
            StrategicGoalId = request.StrategicGoalId,
            StrategicObjectiveId = request.StrategicObjectiveId,
            PerformanceObjective = request.PerformanceObjective,
            TargetName = request.TargetName.Trim(),
            KpiDescription = request.KpiDescription.Trim(),
            Baseline = request.Baseline,
            BaselineDescription = request.BaselineDescription,
            AnnualTarget = request.AnnualTarget,
            AnnualTargetDescription = request.AnnualTargetDescription,
            BudgetSourceId = request.BudgetSourceId,
            BudgetTypeId = request.BudgetTypeId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            Weight = request.Weight,
            KpiType = request.KpiType,
            IndicatorType = request.IndicatorType,
            FunctionalArea = request.FunctionalArea,
            StandardClassification = request.StandardClassification,
            IdpReference = request.IdpReference,
            InternalReference = request.InternalReference,
            FmsLink = request.FmsLink,
            IsRevised = request.IsRevised,
            IsWithdrawn = request.IsWithdrawn,
            ReasonForWithdrawal = request.ReasonForWithdrawal,
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

        entity.SourceTemplateId = request.SourceTemplateId;
        entity.SourceTemplateVersion = request.SourceTemplateVersion;
        entity.PeriodId = request.PeriodId;
        entity.DepartmentId = request.DepartmentId;
        entity.UnitId = request.UnitId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.WardIds = request.WardIds;
        entity.AdditionalAssigneeIds = request.AdditionalAssigneeIds;
        entity.VoteNumberIds = request.VoteNumberIds;
        entity.IndicatorNumber = request.IndicatorNumber.Trim();
        entity.NationalKpa = request.NationalKpa;
        entity.MunicipalKpa = request.MunicipalKpa;
        entity.StrategicGoalId = request.StrategicGoalId;
        entity.StrategicObjectiveId = request.StrategicObjectiveId;
        entity.PerformanceObjective = request.PerformanceObjective;
        entity.TargetName = request.TargetName.Trim();
        entity.KpiDescription = request.KpiDescription.Trim();
        entity.Baseline = request.Baseline;
        entity.BaselineDescription = request.BaselineDescription;
        entity.AnnualTarget = request.AnnualTarget;
        entity.AnnualTargetDescription = request.AnnualTargetDescription;
        entity.BudgetSourceId = request.BudgetSourceId;
        entity.BudgetTypeId = request.BudgetTypeId;
        entity.UnitOfMeasureId = request.UnitOfMeasureId;
        entity.Weight = request.Weight;
        entity.KpiType = request.KpiType;
        entity.IndicatorType = request.IndicatorType;
        entity.FunctionalArea = request.FunctionalArea;
        entity.StandardClassification = request.StandardClassification;
        entity.IdpReference = request.IdpReference;
        entity.InternalReference = request.InternalReference;
        entity.FmsLink = request.FmsLink;
        entity.IsRevised = request.IsRevised;
        entity.IsWithdrawn = request.IsWithdrawn;
        entity.ReasonForWithdrawal = request.ReasonForWithdrawal;
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
        new(target.DepartmentId, target.UnitId, target.Id, null);
}

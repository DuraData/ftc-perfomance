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
[Route("api/v1/opms-targets")]
[Authorize]
public class OpmsTargetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;
    private readonly ITenantContext _tenantContext;

    public OpmsTargetsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccessControlService accessControlService,
        IWorkflowGovernanceService workflowGovernanceService,
        ITenantContext tenantContext)
    {
        _context = context;
        _userManager = userManager;
        _accessControlService = accessControlService;
        _workflowGovernanceService = workflowGovernanceService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse[]>>> GetTargets()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse[]>(false, null, "User not found"));

        var scope = await _accessControlService.GetQueryScopeAsync(user, "OPMS_KPI.READ");
        if (!scope.PermissionGranted) return Ok(new ApiResponse<OpmsTargetResponse[]>(true, []));
        var query = _context.OpmsTargets
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .Include(item => item.Wards)
            .Include(item => item.AdditionalAssignees)
            .Include(item => item.VoteNumbers)
            .AsQueryable();
        if (!scope.Unrestricted)
            query = query.Where(item => (item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)) || (item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)) || (item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)) || scope.TargetIds.Contains(item.Id) || scope.KpiIds.Contains(item.Id));
        var targets = await query.OrderByDescending(item => item.CreatedAt).ToListAsync();
        return Ok(new ApiResponse<OpmsTargetResponse[]>(true, targets.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse>>> GetTarget(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse>(false, null, "User not found"));

        var target = await FindTargetAsync(id);
        if (target == null) return NotFound(new ApiResponse<OpmsTargetResponse>(false, null, "OPMS target not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS_KPI.READ", BuildScope(target));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));

        return Ok(new ApiResponse<OpmsTargetResponse>(true, target.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse>>> CreateTarget([FromBody] SaveOpmsTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse>(false, null, "User not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS_KPI.CREATE", new AccessScopeContext(request.DepartmentId, request.UnitId, null, null));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));
        var mappingError = await ValidateMappingsAsync(request);
        if (mappingError != null) return BadRequest(new ApiResponse<OpmsTargetResponse>(false, null, mappingError));

        var entity = new OpmsTarget
        {
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            PeriodId = request.PeriodId,
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            AssignedUserId = request.AssignedUserId,
            MunicipalityId = _tenantContext.MunicipalityId,
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
        ApplyMappings(entity, request);

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

        var entity = await _context.OpmsTargets
            .Include(item => item.Wards)
            .Include(item => item.AdditionalAssignees)
            .Include(item => item.VoteNumbers)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsTargetResponse>(false, null, "OPMS target not found"));
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<OpmsTargetResponse>(false, null, "A withdrawn OPMS target is immutable."));

        var before = (await FindTargetAsync(id))?.ToResponse();
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS_KPI.UPDATE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));
        var mappingError = await ValidateMappingsAsync(request);
        if (mappingError != null) return BadRequest(new ApiResponse<OpmsTargetResponse>(false, null, mappingError));

        entity.SourceTemplateId = request.SourceTemplateId;
        entity.SourceTemplateVersion = request.SourceTemplateVersion;
        entity.PeriodId = request.PeriodId;
        entity.DepartmentId = request.DepartmentId;
        entity.UnitId = request.UnitId;
        entity.AssignedUserId = request.AssignedUserId;
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
        _context.OpmsTargetWards.RemoveRange(entity.Wards);
        _context.OpmsTargetAdditionalAssignees.RemoveRange(entity.AdditionalAssignees);
        _context.OpmsTargetVoteNumbers.RemoveRange(entity.VoteNumbers);
        ApplyMappings(entity, request);
        await _context.SaveChangesAsync();

        var after = await FindTargetAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTarget", id, "Edit", before, after.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsTargetResponse>(true, after.ToResponse()));
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<bool>> DeleteTarget(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Governed targets are never deleted. Use POST /api/v1/opms-targets/{id}/withdraw with a reason and RowVersion."));

    [HttpPost("{id}/withdraw")]
    public async Task<ActionResult<ApiResponse<OpmsTargetResponse>>> WithdrawTarget(string id, [FromBody] WithdrawGovernedRecordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsTargetResponse>(false, null, "User not found"));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            return BadRequest(new ApiResponse<OpmsTargetResponse>(false, null, "A withdrawal reason between 1 and 1000 characters is required."));

        var entity = await _context.OpmsTargets.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsTargetResponse>(false, null, "OPMS target not found"));
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<OpmsTargetResponse>(false, null, "The OPMS target is already withdrawn."));
        if (!entity.MunicipalityId.HasValue) return Conflict(new ApiResponse<OpmsTargetResponse>(false, null, "The OPMS target must be reconciled to a municipality before withdrawal."));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS_KPI.WITHDRAW", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsTargetResponse>(false, null, decision.Reason));
        if (!TrySetExpectedVersion(entity, request.RowVersion))
            return BadRequest(new ApiResponse<OpmsTargetResponse>(false, null, "A valid RowVersion is required."));

        var occurredAt = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
        entity.IsWithdrawn = true;
        entity.ReasonForWithdrawal = reason;
        entity.WithdrawnAt = occurredAt;
        entity.WithdrawnByUserId = user.Id;
        _context.GovernedRecordLifecycleEvents.Add(new GovernedRecordLifecycleEvent
        {
            MunicipalityId = entity.MunicipalityId.Value,
            AggregateType = "OpmsTarget",
            AggregateId = entity.Id,
            Action = GovernedLifecycleAction.Withdrawn,
            Reason = reason,
            ActorUserId = user.Id,
            OccurredAt = occurredAt,
            CorrelationId = HttpContext.TraceIdentifier
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsTarget", id, "Withdraw", before?.ToResponse(), entity.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<OpmsTargetResponse>(false, null, "The OPMS target changed before withdrawal. Refresh and try again."));
        }
        var response = await FindTargetAsync(id) ?? entity;
        return Ok(new ApiResponse<OpmsTargetResponse>(true, response.ToResponse()));
    }

    private bool TrySetExpectedVersion(OpmsTarget entity, string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value ?? string.Empty);
            if (bytes.Length is not (sizeof(long) or 16)) return false;
            _context.Entry(entity).Property(item => item.RowVersion).OriginalValue = bytes;
            return true;
        }
        catch (FormatException) { return false; }
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
            .Include(item => item.Wards)
            .Include(item => item.AdditionalAssignees)
            .Include(item => item.VoteNumbers)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    private async Task<string?> ValidateMappingsAsync(SaveOpmsTargetRequest request)
    {
        var tenantId = _tenantContext.MunicipalityId;
        if (!tenantId.HasValue || tenantId == long.MinValue) return "A municipality context is required.";
        var wardIds = (request.WardIds ?? []).Distinct().ToArray();
        var assigneeIds = (request.AdditionalAssigneeIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var voteIds = (request.VoteNumberIds ?? []).Distinct().ToArray();
        if (wardIds.Length > 100 || assigneeIds.Length > 100 || voteIds.Length > 100) return "At most 100 wards, additional assignees, and vote numbers may be linked.";
        var wards = await _context.Wards.AsNoTracking().Where(item => wardIds.Contains(item.Id) && item.IsActive).ToArrayAsync();
        if (wards.Length != wardIds.Length || wards.Any(item => item.MunicipalityId != tenantId.Value))
            return "Every ward must be active and belong to the selected municipality.";

        var votes = await _context.VoteNumbers.AsNoTracking().Include(item => item.Department).Where(item => voteIds.Contains(item.Id) && item.IsActive).ToArrayAsync();
        if (votes.Length != voteIds.Length || votes.Any(item => item.MunicipalityId != tenantId.Value || item.Department.MunicipalityId != tenantId.Value))
            return "Every vote number must be active and belong to a department in the selected municipality.";

        var users = await _context.Users.AsNoTracking().Where(item => assigneeIds.Contains(item.Id) && item.IsActive).Select(item => new { item.Id, item.MunicipalityId }).ToArrayAsync();
        var now = DateTime.UtcNow;
        var assignedIds = await _context.SecurityUserRoleAssignments.AsNoTracking().Where(item => assigneeIds.Contains(item.UserId) && item.MunicipalityId == tenantId.Value && item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now)).Select(item => item.UserId).Distinct().ToArrayAsync();
        if (users.Length != assigneeIds.Length || users.Any(item => item.MunicipalityId != tenantId.Value && !assignedIds.Contains(item.Id, StringComparer.OrdinalIgnoreCase)))
            return "Every additional assignee must be active and assigned within the selected municipality.";
        return null;
    }

    private void ApplyMappings(OpmsTarget target, SaveOpmsTargetRequest request)
    {
        var tenantId = _tenantContext.MunicipalityId;
        target.Wards = (request.WardIds ?? []).Distinct().Select(id => new OpmsTargetWard { MunicipalityId = tenantId, OpmsTargetId = target.Id, WardId = id }).ToList();
        target.AdditionalAssignees = (request.AdditionalAssigneeIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Select(id => new OpmsTargetAdditionalAssignee { MunicipalityId = tenantId, OpmsTargetId = target.Id, UserId = id }).ToList();
        target.VoteNumbers = (request.VoteNumberIds ?? []).Distinct().Select(id => new OpmsTargetVoteNumber { MunicipalityId = tenantId, OpmsTargetId = target.Id, VoteNumberId = id }).ToList();
    }

    private static AccessScopeContext BuildScope(OpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, MunicipalityId: target.MunicipalityId);
}

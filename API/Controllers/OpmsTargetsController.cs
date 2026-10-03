using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
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
    private readonly IPerformanceUnitEngine _unitEngine;

    public OpmsTargetsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccessControlService accessControlService,
        IWorkflowGovernanceService workflowGovernanceService,
        ITenantContext tenantContext,
        IPerformanceUnitEngine unitEngine)
    {
        _context = context;
        _userManager = userManager;
        _accessControlService = accessControlService;
        _workflowGovernanceService = workflowGovernanceService;
        _tenantContext = tenantContext;
        _unitEngine = unitEngine;
    }

    [HttpGet]
    public ActionResult<ApiResponse<object>> GetTargets() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null, "The unbounded OPMS target collection is retired. Use /api/v1/opms-targets/page for registers or /api/v1/opms-targets/options for selectors."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<OpmsTargetResponse>>>> GetTargetsPage([FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<OpmsTargetResponse>>(false, null, "User not found"));
        if (!TargetSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<OpmsTargetResponse>>(false, null, "SortBy must be createdAt, indicatorNumber, or targetName."));

        var scope = await _accessControlService.GetQueryScopeAsync(user, "OPMS_KPI.READ");
        if (!scope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<OpmsTargetResponse>>(true, PagedResponse<OpmsTargetResponse>.Empty(request.Page, request.PageSize)));

        var query = _context.OpmsTargets.AsNoTracking().AsQueryable();
        if (!scope.Unrestricted)
            query = query.Where(item => (item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)) || (item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)) || (item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)) || scope.TargetIds.Contains(item.Id) || scope.KpiIds.Contains(item.Id));
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || item.TargetName.Contains(request.NormalizedSearch) || item.KpiDescription.Contains(request.NormalizedSearch));

        var totalCount = await query.CountAsync();
        query = ApplyTargetOrdering(query, request.NormalizedSortBy, request.Descending);
        var items = await query
            .Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.Department).Include(item => item.Unit).Include(item => item.AssignedUser)
            .Include(item => item.Wards).Include(item => item.AdditionalAssignees).Include(item => item.VoteNumbers)
            .AsSplitQuery()
            .ToListAsync();
        await TargetPeriodCutover.HydrateCanonicalRowsAsync(_context, items);
        return Ok(new ApiResponse<PagedResponse<OpmsTargetResponse>>(true,
            PagedResponse<OpmsTargetResponse>.Create(items.Select(item => item.ToResponse()), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> TargetSortFields = ["createdat", "indicatornumber", "targetname"];

    private static IQueryable<OpmsTarget> ApplyTargetOrdering(IQueryable<OpmsTarget> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("indicatornumber", false) => query.OrderBy(item => item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", true) => query.OrderByDescending(item => item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("targetname", false) => query.OrderBy(item => item.TargetName).ThenBy(item => item.PublicId),
            ("targetname", true) => query.OrderByDescending(item => item.TargetName).ThenBy(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.PublicId)
        };

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>>> GetTargetOptions([FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(false, null, "User not found"));
        if (!TargetSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(false, null, "SortBy must be createdAt, indicatorNumber, or targetName."));

        var scope = await _accessControlService.GetQueryScopeAsync(user, "OPMS_KPI.READ");
        if (!scope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(true, PagedResponse<PerformanceTargetOptionResponse>.Empty(request.Page, request.PageSize)));

        var query = _context.OpmsTargets.AsNoTracking().Where(item => !item.IsWithdrawn);
        if (!scope.Unrestricted)
            query = query.Where(item => (item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)) || (item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)) || (item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)) || scope.TargetIds.Contains(item.Id) || scope.KpiIds.Contains(item.Id));
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || item.TargetName.Contains(request.NormalizedSearch) || item.KpiDescription.Contains(request.NormalizedSearch));

        var totalCount = await query.CountAsync();
        var items = await ApplyTargetOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize)
            .Select(item => new PerformanceTargetOptionResponse(item.Id, item.PublicId, item.IndicatorNumber, item.TargetName, item.DepartmentId, item.Department != null ? item.Department.Name : null))
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(true,
            PagedResponse<PerformanceTargetOptionResponse>.Create(items, request.Page, request.PageSize, totalCount)));
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
        var periodPlan = await TargetPeriodCutover.BuildPlanAsync(_context, _unitEngine, _tenantContext.MunicipalityId, request.PeriodId, request.PeriodTargets);
        if (!periodPlan.IsValid) return BadRequest(new ApiResponse<OpmsTargetResponse>(false, null, periodPlan.Error));

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
            TargetUnitType = periodPlan.Rows.Single(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        ApplyMappings(entity, request);

        _context.OpmsTargets.Add(entity);
        TargetPeriodCutover.AddNewRows(_context, periodPlan, _tenantContext.MunicipalityId!.Value, user.Id, entity.Id, null);
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
        var periodPlan = await TargetPeriodCutover.BuildPlanAsync(_context, _unitEngine, entity.MunicipalityId, request.PeriodId, request.PeriodTargets);
        if (!periodPlan.IsValid) return BadRequest(new ApiResponse<OpmsTargetResponse>(false, null, periodPlan.Error));
        var periodChangeError = await TargetPeriodCutover.EnsureUnchangedOrAddMissingAsync(_context, periodPlan, entity.MunicipalityId!.Value, user.Id, entity.Id, null);
        if (periodChangeError != null) return Conflict(new ApiResponse<OpmsTargetResponse>(false, null, periodChangeError));

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
        entity.TargetUnitType = periodPlan.Rows.Single(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString();
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
        entity.CanonicalPeriodTargets = before?.CanonicalPeriodTargets ?? [];
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

    private async Task<OpmsTarget?> FindTargetAsync(string id)
    {
        var target = await _context.OpmsTargets
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .Include(item => item.Wards)
            .Include(item => item.AdditionalAssignees)
            .Include(item => item.VoteNumbers)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (target != null) await TargetPeriodCutover.HydrateCanonicalRowsAsync(_context, [target]);
        return target;
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

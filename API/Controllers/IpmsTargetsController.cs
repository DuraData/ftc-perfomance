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
[Route("api/ipms-targets")]
[Route("api/v1/ipms-targets")]
[Authorize]
public class IpmsTargetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;
    private readonly ITenantContext _tenantContext;
    private readonly IPerformanceUnitEngine _unitEngine;

    public IpmsTargetsController(
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
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null, "The unbounded IPMS target collection is retired. Use /api/v1/ipms-targets/page for registers or /api/v1/ipms-targets/options for selectors."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IpmsTargetResponse>>>> GetTargetsPage([FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<IpmsTargetResponse>>(false, null, "User not found"));
        if (!TargetSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<IpmsTargetResponse>>(false, null, "SortBy must be createdAt, indicatorNumber, targetName, or effectiveOrder."));
        if (request.NormalizedSortBy == "effectiveorder" && !request.ReportingPeriodType.HasValue)
            return BadRequest(new ApiResponse<PagedResponse<IpmsTargetResponse>>(false, null, "ReportingPeriodType is required for effectiveOrder sorting."));

        var scope = await _accessControlService.GetQueryScopeAsync(user, "IPMS_KPI.READ");
        if (!scope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<IpmsTargetResponse>>(true, PagedResponse<IpmsTargetResponse>.Empty(request.Page, request.PageSize)));
        var query = _context.IpmsTargets.AsNoTracking().AsQueryable();
        if (!scope.Unrestricted)
            query = query.Where(item => (item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)) || (item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)) || (item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)) || scope.TargetIds.Contains(item.Id) || scope.KpiIds.Contains(item.Id));
        if (request.RelatedOpmsTargetPublicId.HasValue)
            query = query.Where(item => item.RelatedOpmsTarget != null && item.RelatedOpmsTarget.PublicId == request.RelatedOpmsTargetPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || item.TargetName.Contains(request.NormalizedSearch) || item.KpiDescription.Contains(request.NormalizedSearch));

        var totalCount = await query.CountAsync();
        query = ApplyTargetOrdering(query, request.NormalizedSortBy, request.Descending, request.ReportingPeriodType);
        var items = await query.Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.Department).Include(item => item.Unit).Include(item => item.AssignedUser)
            .AsSplitQuery().ToListAsync();
        await TargetPeriodCutover.HydrateCanonicalRowsAsync(_context, items);
        return Ok(new ApiResponse<PagedResponse<IpmsTargetResponse>>(true,
            PagedResponse<IpmsTargetResponse>.Create(items.Select(item => item.ToResponse()), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> TargetSortFields = ["createdat", "indicatornumber", "targetname", "effectiveorder"];

    private static IQueryable<IpmsTarget> ApplyTargetOrdering(IQueryable<IpmsTarget> query, string sortBy, bool descending, ReportingPeriodType? periodType) =>
        (sortBy, descending) switch
        {
            ("effectiveorder", false) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderBy(item => item.RevisedOrderNumber).ThenBy(item => item.PublicId),
            ("effectiveorder", true) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderByDescending(item => item.RevisedOrderNumber).ThenBy(item => item.PublicId),
            ("effectiveorder", false) => query.OrderBy(item => item.OriginalOrderNumber).ThenBy(item => item.PublicId),
            ("effectiveorder", true) => query.OrderByDescending(item => item.OriginalOrderNumber).ThenBy(item => item.PublicId),
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
            return BadRequest(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(false, null, "SortBy must be createdAt, indicatorNumber, targetName, or effectiveOrder."));
        if (request.NormalizedSortBy == "effectiveorder" && !request.ReportingPeriodType.HasValue)
            return BadRequest(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(false, null, "ReportingPeriodType is required for effectiveOrder sorting."));

        var scope = await _accessControlService.GetQueryScopeAsync(user, "IPMS_KPI.READ");
        if (!scope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(true, PagedResponse<PerformanceTargetOptionResponse>.Empty(request.Page, request.PageSize)));

        var query = _context.IpmsTargets.AsNoTracking().Where(item => !item.IsWithdrawn);
        if (!scope.Unrestricted)
            query = query.Where(item => (item.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.DepartmentId.Value)) || (item.UnitId.HasValue && scope.UnitIds.Contains(item.UnitId.Value)) || (item.AssignedUserId != null && scope.OwnerUserIds.Contains(item.AssignedUserId)) || scope.TargetIds.Contains(item.Id) || scope.KpiIds.Contains(item.Id));
        if (request.RelatedOpmsTargetPublicId.HasValue)
            query = query.Where(item => item.RelatedOpmsTarget != null && item.RelatedOpmsTarget.PublicId == request.RelatedOpmsTargetPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || item.TargetName.Contains(request.NormalizedSearch) || item.KpiDescription.Contains(request.NormalizedSearch));

        var totalCount = await query.CountAsync();
        var items = await ApplyTargetOrdering(query, request.NormalizedSortBy, request.Descending, request.ReportingPeriodType)
            .Skip(request.Offset).Take(request.PageSize)
            .Select(item => new PerformanceTargetOptionResponse(item.Id, item.PublicId, item.IndicatorNumber, item.TargetName, item.DepartmentId, item.Department != null ? item.Department.Name : null, item.RelatedOpmsTarget != null ? item.RelatedOpmsTarget.PublicId : null))
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>(true,
            PagedResponse<PerformanceTargetOptionResponse>.Create(items, request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> GetTarget(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));

        var target = await FindTargetAsync(id);
        if (target == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.READ", BuildScope(target));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));

        return Ok(new ApiResponse<IpmsTargetResponse>(true, target.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> CreateTarget([FromBody] SaveIpmsTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.CREATE", new AccessScopeContext(request.DepartmentId, request.UnitId, user.Id));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        if (request.OriginalOrderNumber <= 0)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "Original order number must be a positive integer."));
        var periodPlan = await TargetPeriodCutover.BuildPlanAsync(_context, _unitEngine, _tenantContext.MunicipalityId, request.PeriodId, request.PeriodTargets);
        if (!periodPlan.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, periodPlan.Error));

        var entity = new IpmsTarget
        {
            MunicipalityId = _tenantContext.MunicipalityId,
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            RelatedOpmsTargetId = request.RelatedOpmsTargetId,
            PeriodId = request.PeriodId,
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            AssignedUserId = request.AssignedUserId,
            SupervisorId = request.SupervisorId,
            IndicatorNumber = request.IndicatorNumber.Trim(),
            OriginalOrderNumber = request.OriginalOrderNumber,
            RevisedOrderNumber = request.OriginalOrderNumber,
            NationalKpa = request.NationalKpa,
            MunicipalKpa = request.MunicipalKpa,
            StrategicGoalId = request.StrategicGoalId,
            StrategicObjectiveId = request.StrategicObjectiveId,
            PerformanceObjective = request.PerformanceObjective,
            TargetName = request.TargetName.Trim(),
            KpiDescription = request.KpiDescription.Trim(),
            Baseline = request.Baseline,
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
            TargetUnitType = periodPlan.Rows.Single(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.IpmsTargets.Add(entity);
        TargetPeriodCutover.AddNewRows(_context, periodPlan, _tenantContext.MunicipalityId!.Value, user.Id, null, entity.Id);
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
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "A withdrawn IPMS target is immutable."));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.UPDATE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        var periodPlan = await TargetPeriodCutover.BuildPlanAsync(_context, _unitEngine, entity.MunicipalityId, request.PeriodId, request.PeriodTargets);
        if (!periodPlan.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, periodPlan.Error));
        var periodChangeError = await TargetPeriodCutover.EnsureUnchangedOrAddMissingAsync(_context, periodPlan, entity.MunicipalityId!.Value, user.Id, null, entity.Id);
        if (periodChangeError != null) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, periodChangeError));

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
        entity.TargetUnitType = periodPlan.Rows.Single(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString();
        await _context.SaveChangesAsync();

        var after = await FindTargetAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", id, "Edit", before?.ToResponse(), after.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsTargetResponse>(true, after.ToResponse()));
    }

    [HttpPut("{id}/ordering")]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> ReviseOrdering(string id, [FromBody] ReviseKpiOrderingRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));
        if (request.OriginalOrderNumber <= 0 || request.RevisedOrderNumber <= 0)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "Original and revised order numbers must be positive integers."));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 2000 || string.IsNullOrWhiteSpace(request.ApprovalReference) || request.ApprovalReference.Trim().Length > 500 || request.EffectiveAt == default)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "Reason, approval reference and effective date are required."));

        var entity = await _context.IpmsTargets.SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "A withdrawn IPMS target is immutable."));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.UPDATE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        if (!TrySetExpectedVersion(entity, request.RowVersion))
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "A valid RowVersion is required."));
        if (entity.OriginalOrderNumber == request.OriginalOrderNumber && entity.RevisedOrderNumber == request.RevisedOrderNumber)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "At least one order number must change."));

        var before = new { entity.OriginalOrderNumber, entity.RevisedOrderNumber };
        AddOrderRevision(entity, nameof(entity.OriginalOrderNumber), entity.OriginalOrderNumber, request.OriginalOrderNumber, request, user.Id);
        AddOrderRevision(entity, nameof(entity.RevisedOrderNumber), entity.RevisedOrderNumber, request.RevisedOrderNumber, request, user.Id);
        entity.OriginalOrderNumber = request.OriginalOrderNumber;
        entity.RevisedOrderNumber = request.RevisedOrderNumber;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "The KPI ordering changed since it was loaded. Refresh and try again.")); }

        var after = await FindTargetAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", id, "ReviseOrdering", before, new { after.OriginalOrderNumber, after.RevisedOrderNumber, request.Reason, request.ApprovalReference, request.EffectiveAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsTargetResponse>(true, after.ToResponse()));
    }

    [HttpGet("{id}/ordering-revisions")]
    public async Task<ActionResult<ApiResponse<KpiFieldRevisionResponse[]>>> GetOrderingRevisions(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<KpiFieldRevisionResponse[]>(false, null, "User not found"));
        var entity = await _context.IpmsTargets.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<KpiFieldRevisionResponse[]>(false, null, "IPMS target not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.READ", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<KpiFieldRevisionResponse[]>(false, null, decision.Reason));
        var rows = await _context.KpiFieldRevisions.AsNoTracking().Where(item => item.IpmsTargetId == entity.Id)
            .OrderBy(item => item.RecordedAt).ThenBy(item => item.Id)
            .Select(item => new KpiFieldRevisionResponse(item.PublicId, item.FieldName, item.OriginalValue, item.RevisedValue, item.Reason, item.ApprovalReference, item.EffectiveAt, item.RevisedByUserId, item.RecordedAt)).ToArrayAsync();
        return Ok(new ApiResponse<KpiFieldRevisionResponse[]>(true, rows));
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<bool>> DeleteTarget(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Governed targets are never deleted. Use POST /api/v1/ipms-targets/{id}/withdraw with a reason and RowVersion."));

    [HttpPost("{id}/withdraw")]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> WithdrawTarget(string id, [FromBody] WithdrawGovernedRecordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "A withdrawal reason between 1 and 1000 characters is required."));

        var entity = await _context.IpmsTargets.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "The IPMS target is already withdrawn."));
        if (!entity.MunicipalityId.HasValue) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "The IPMS target must be reconciled to a municipality before withdrawal."));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.WITHDRAW", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        if (!TrySetExpectedVersion(entity, request.RowVersion))
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "A valid RowVersion is required."));

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
            AggregateType = "IpmsTarget",
            AggregateId = entity.Id,
            Action = GovernedLifecycleAction.Withdrawn,
            Reason = reason,
            ActorUserId = user.Id,
            OccurredAt = occurredAt,
            CorrelationId = HttpContext.TraceIdentifier
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", id, "Withdraw", before?.ToResponse(), entity.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "The IPMS target changed before withdrawal. Refresh and try again."));
        }
        var response = await FindTargetAsync(id) ?? entity;
        return Ok(new ApiResponse<IpmsTargetResponse>(true, response.ToResponse()));
    }

    private bool TrySetExpectedVersion(IpmsTarget entity, string value)
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

    private void AddOrderRevision(IpmsTarget entity, string fieldName, int originalValue, int revisedValue, ReviseKpiOrderingRequest request, string userId)
    {
        if (originalValue == revisedValue) return;
        _context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = entity.MunicipalityId!.Value,
            IpmsTargetId = entity.Id,
            FieldName = fieldName,
            OriginalValue = originalValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RevisedValue = revisedValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Reason = request.Reason.Trim(),
            ApprovalReference = request.ApprovalReference.Trim(),
            EffectiveAt = request.EffectiveAt,
            RevisedByUserId = userId
        });
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private async Task<IpmsTarget?> FindTargetAsync(string id)
    {
        var target = await _context.IpmsTargets
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Unit)
            .Include(item => item.AssignedUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (target != null) await TargetPeriodCutover.HydrateCanonicalRowsAsync(_context, [target]);
        return target;
    }

    private static AccessScopeContext BuildScope(IpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, MunicipalityId: target.MunicipalityId);
}

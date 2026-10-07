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
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || (item.RevisedIndicatorNumber != null && item.RevisedIndicatorNumber.Contains(request.NormalizedSearch)) || item.TargetName.Contains(request.NormalizedSearch) || (item.RevisedTargetName != null && item.RevisedTargetName.Contains(request.NormalizedSearch)) || item.KpiDescription.Contains(request.NormalizedSearch) || (item.RevisedKpiDescription != null && item.RevisedKpiDescription.Contains(request.NormalizedSearch)));

        var totalCount = await query.CountAsync();
        query = ApplyTargetOrdering(query, request.NormalizedSortBy, request.Descending, request.ReportingPeriodType);
        var items = await query.Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.Department).Include(item => item.Unit).Include(item => item.AssignedUser)
            .Include(item => item.NationalKpaReference).Include(item => item.MunicipalKpaReference).Include(item => item.BackToBasicsPillarReference)
            .Include(item => item.StrategicGoalMaster).Include(item => item.StrategicInterventionReference)
            .Include(item => item.StrategicObjectiveMaster).Include(item => item.PerformanceObjectiveReference)
            .Include(item => item.BudgetTypeMaster).Include(item => item.GovernedBudgetSources).ThenInclude(item => item.BudgetSource)
            .Include(item => item.KpiTypeMaster).Include(item => item.IndicatorTypeMaster).Include(item => item.FunctionalAreaMaster).Include(item => item.KpiUnitOfMeasureMaster)
            .AsSplitQuery().ToListAsync();
        await TargetPeriodCutover.HydrateCanonicalRowsAsync(_context, items);
        return Ok(new ApiResponse<PagedResponse<IpmsTargetResponse>>(true,
            PagedResponse<IpmsTargetResponse>.Create(items.Select(item => item.ToResponse(request.ReportingPeriodType)), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> TargetSortFields = ["createdat", "indicatornumber", "targetname", "effectiveorder"];

    private static IQueryable<IpmsTarget> ApplyTargetOrdering(IQueryable<IpmsTarget> query, string sortBy, bool descending, ReportingPeriodType? periodType) =>
        (sortBy, descending) switch
        {
            ("effectiveorder", false) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderBy(item => item.RevisedOrderNumber).ThenBy(item => item.PublicId),
            ("effectiveorder", true) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderByDescending(item => item.RevisedOrderNumber).ThenBy(item => item.PublicId),
            ("effectiveorder", false) => query.OrderBy(item => item.OriginalOrderNumber).ThenBy(item => item.PublicId),
            ("effectiveorder", true) => query.OrderByDescending(item => item.OriginalOrderNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", false) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderBy(item => item.IsIndicatorNumberRevised ? item.RevisedIndicatorNumber ?? item.IndicatorNumber : item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", true) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderByDescending(item => item.IsIndicatorNumberRevised ? item.RevisedIndicatorNumber ?? item.IndicatorNumber : item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", false) => query.OrderBy(item => item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", true) => query.OrderByDescending(item => item.IndicatorNumber).ThenBy(item => item.PublicId),
            ("targetname", false) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderBy(item => item.IsTargetNameRevised ? item.RevisedTargetName ?? item.TargetName : item.TargetName).ThenBy(item => item.PublicId),
            ("targetname", true) when periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual => query.OrderByDescending(item => item.IsTargetNameRevised ? item.RevisedTargetName ?? item.TargetName : item.TargetName).ThenBy(item => item.PublicId),
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
            query = query.Where(item => item.IndicatorNumber.Contains(request.NormalizedSearch) || (item.RevisedIndicatorNumber != null && item.RevisedIndicatorNumber.Contains(request.NormalizedSearch)) || item.TargetName.Contains(request.NormalizedSearch) || (item.RevisedTargetName != null && item.RevisedTargetName.Contains(request.NormalizedSearch)) || item.KpiDescription.Contains(request.NormalizedSearch) || (item.RevisedKpiDescription != null && item.RevisedKpiDescription.Contains(request.NormalizedSearch)));

        var totalCount = await query.CountAsync();
        var useRevised = request.ReportingPeriodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual;
        var items = await ApplyTargetOrdering(query, request.NormalizedSortBy, request.Descending, request.ReportingPeriodType)
            .Skip(request.Offset).Take(request.PageSize)
            .Select(item => new PerformanceTargetOptionResponse(item.Id, item.PublicId,
                useRevised && item.IsIndicatorNumberRevised && item.RevisedIndicatorNumber != null ? item.RevisedIndicatorNumber : item.IndicatorNumber,
                useRevised && item.IsTargetNameRevised && item.RevisedTargetName != null ? item.RevisedTargetName : item.TargetName,
                item.DepartmentId, item.Department != null ? item.Department.Name : null, item.RelatedOpmsTarget != null ? item.RelatedOpmsTarget.PublicId : null))
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

        var organization = await PerformanceApiSupport.ResolveOrganizationScopeAsync(_context, _tenantContext.MunicipalityId, request.DepartmentId, request.UnitId, request.DepartmentPublicId, request.UnitPublicId);
        if (organization.Error != null) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, organization.Error));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.CREATE", new AccessScopeContext(organization.DepartmentId, organization.UnitId, user.Id));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        if (request.OriginalOrderNumber <= 0)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "Original order number must be a positive integer."));
        var periodPlan = await TargetPeriodCutover.BuildPlanAsync(_context, _unitEngine, _tenantContext.MunicipalityId, request.PeriodId, request.PeriodTargets);
        if (!periodPlan.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, periodPlan.Error));
        var strategicSelection = StrategicClassificationResolver.Selection(request);
        if (!strategicSelection.IsComplete) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "All governed strategic classifications are required."));
        var strategicClassification = await StrategicClassificationResolver.ResolveAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId, strategicSelection);
        if (!strategicClassification.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, strategicClassification.Error));
        var budgetClassification = await BudgetClassificationResolver.ResolveAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId,
            request.BudgetTypePublicId, request.BudgetSources);
        if (!budgetClassification.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, budgetClassification.Error));
        var performanceClassification = await PerformanceClassificationResolver.ResolveAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId,
            request.KpiTypePublicId, request.IndicatorTypePublicId, request.FunctionalAreaPublicId, null, false);
        if (!performanceClassification.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, performanceClassification.Error));
        var kpiUnit = await PerformanceClassificationResolver.ResolveUnitAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId,
            request.KpiUnitOfMeasurePublicId);
        if (!kpiUnit.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, kpiUnit.Error));

        var entity = new IpmsTarget
        {
            MunicipalityId = _tenantContext.MunicipalityId,
            SourceTemplateId = request.SourceTemplateId,
            SourceTemplateVersion = request.SourceTemplateVersion,
            RelatedOpmsTargetId = request.RelatedOpmsTargetId,
            PeriodId = request.PeriodId,
            DepartmentId = organization.DepartmentId,
            UnitId = organization.UnitId,
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
            UnitOfMeasureId = request.UnitOfMeasureId,
            Weight = request.Weight,
            KpiType = request.KpiType,
            IndicatorType = request.IndicatorType,
            FunctionalArea = request.FunctionalArea,
            IdpReference = request.IdpReference,
            InternalReference = request.InternalReference,
            IsRevised = false,
            TargetUnitType = periodPlan.Rows.Single(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        StrategicClassificationResolver.Apply(entity, strategicClassification);
        BudgetClassificationResolver.Apply(entity, budgetClassification);
        PerformanceClassificationResolver.Apply(entity, performanceClassification);
        PerformanceClassificationResolver.ApplyUnit(entity, kpiUnit);

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

        var entity = await _context.IpmsTargets.Include(item => item.GovernedBudgetSources).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "A withdrawn IPMS target is immutable."));

        var before = await FindTargetAsync(id);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.UPDATE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        var organization = await PerformanceApiSupport.ResolveOrganizationScopeAsync(_context, entity.MunicipalityId, request.DepartmentId, request.UnitId, request.DepartmentPublicId, request.UnitPublicId);
        if (organization.Error != null) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, organization.Error));
        var periodPlan = await TargetPeriodCutover.BuildPlanAsync(_context, _unitEngine, entity.MunicipalityId, request.PeriodId, request.PeriodTargets);
        if (!periodPlan.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, periodPlan.Error));
        var strategicSelection = StrategicClassificationResolver.Selection(request);
        if (!strategicSelection.IsComplete) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "All governed strategic classifications are required."));
        var strategicClassification = await StrategicClassificationResolver.ResolveAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId, strategicSelection,
            entity.NationalKpaId, entity.MunicipalKpaId, entity.BackToBasicsPillarId, entity.StrategicGoalMasterId,
            entity.StrategicInterventionId, entity.StrategicObjectiveMasterId, entity.PerformanceObjectiveId);
        if (!strategicClassification.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, strategicClassification.Error));
        var budgetClassification = await BudgetClassificationResolver.ResolveAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId,
            request.BudgetTypePublicId, request.BudgetSources, entity.BudgetTypeMasterId,
            entity.GovernedBudgetSources.Where(item => item.IsActive).Select(item => item.BudgetSourceId).ToArray());
        if (!budgetClassification.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, budgetClassification.Error));
        var performanceClassification = await PerformanceClassificationResolver.ResolveAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId,
            request.KpiTypePublicId, request.IndicatorTypePublicId, request.FunctionalAreaPublicId, null, false,
            entity.KpiTypeMasterId, entity.IndicatorTypeMasterId, entity.FunctionalAreaMasterId);
        if (!performanceClassification.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, performanceClassification.Error));
        var kpiUnit = await PerformanceClassificationResolver.ResolveUnitAsync(_context, periodPlan.Rows[0].ReportingPeriod.MunicipalityFinancialYearId,
            request.KpiUnitOfMeasurePublicId, entity.KpiUnitOfMeasureMasterId);
        if (!kpiUnit.IsValid) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, kpiUnit.Error));
        var periodChangeError = await TargetPeriodCutover.EnsureUnchangedOrAddMissingAsync(_context, periodPlan, entity.MunicipalityId!.Value, user.Id, null, entity.Id);
        if (periodChangeError != null) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, periodChangeError));
        if (!string.Equals(entity.IndicatorNumber, request.IndicatorNumber.Trim(), StringComparison.Ordinal) ||
            !string.Equals(entity.TargetName, request.TargetName.Trim(), StringComparison.Ordinal) ||
            !string.Equals(entity.KpiDescription, request.KpiDescription.Trim(), StringComparison.Ordinal))
            return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "KPI number, target name and KPI wording are governed originals. Record approved changes through the field-revision endpoint."));

        entity.SourceTemplateId = request.SourceTemplateId;
        entity.SourceTemplateVersion = request.SourceTemplateVersion;
        entity.RelatedOpmsTargetId = request.RelatedOpmsTargetId;
        entity.PeriodId = request.PeriodId;
        entity.DepartmentId = organization.DepartmentId;
        entity.UnitId = organization.UnitId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.SupervisorId = request.SupervisorId;
        entity.NationalKpa = request.NationalKpa;
        entity.MunicipalKpa = request.MunicipalKpa;
        entity.StrategicGoalId = request.StrategicGoalId;
        entity.StrategicObjectiveId = request.StrategicObjectiveId;
        entity.PerformanceObjective = request.PerformanceObjective;
        entity.Baseline = request.Baseline;
        entity.UnitOfMeasureId = request.UnitOfMeasureId;
        entity.Weight = request.Weight;
        entity.KpiType = request.KpiType;
        entity.IndicatorType = request.IndicatorType;
        entity.FunctionalArea = request.FunctionalArea;
        entity.IdpReference = request.IdpReference;
        entity.InternalReference = request.InternalReference;
        entity.TargetUnitType = periodPlan.Rows.Single(item => item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual).UnitKind.ToString();
        StrategicClassificationResolver.Apply(entity, strategicClassification);
        BudgetClassificationResolver.Apply(entity, budgetClassification);
        PerformanceClassificationResolver.Apply(entity, performanceClassification);
        PerformanceClassificationResolver.ApplyUnit(entity, kpiUnit);
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
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.REVISE", BuildScope(entity));
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

    [HttpPut("{id}/field-revisions")]
    public async Task<ActionResult<ApiResponse<IpmsTargetResponse>>> ReviseDefinitionFields(string id, [FromBody] ReviseKpiDefinitionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsTargetResponse>(false, null, "User not found"));
        var validation = ValidateDefinitionRevision(request);
        if (validation != null) return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, validation));

        var entity = await _context.IpmsTargets.SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsTargetResponse>(false, null, "IPMS target not found"));
        if (entity.IsWithdrawn) return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "A withdrawn IPMS target is immutable."));
        if ((request.IsIndicatorNumberRevised && string.Equals(request.RevisedIndicatorNumber!.Trim(), entity.IndicatorNumber, StringComparison.Ordinal))
            || (request.IsTargetNameRevised && string.Equals(request.RevisedTargetName!.Trim(), entity.TargetName, StringComparison.Ordinal))
            || (request.IsKpiDescriptionRevised && string.Equals(request.RevisedKpiDescription!.Trim(), entity.KpiDescription, StringComparison.Ordinal)))
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "Each flagged revised value must differ from its original value."));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.REVISE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsTargetResponse>(false, null, decision.Reason));
        if (!TrySetExpectedVersion(entity, request.RowVersion))
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "A valid RowVersion is required."));

        var revisedIndicator = request.IsIndicatorNumberRevised ? request.RevisedIndicatorNumber!.Trim() : null;
        var revisedName = request.IsTargetNameRevised ? request.RevisedTargetName!.Trim() : null;
        var revisedKpi = request.IsKpiDescriptionRevised ? request.RevisedKpiDescription!.Trim() : null;
        var oldIndicator = entity.IsIndicatorNumberRevised ? entity.RevisedIndicatorNumber : entity.IndicatorNumber;
        var oldName = entity.IsTargetNameRevised ? entity.RevisedTargetName : entity.TargetName;
        var oldKpi = entity.IsKpiDescriptionRevised ? entity.RevisedKpiDescription : entity.KpiDescription;
        var newIndicator = revisedIndicator ?? entity.IndicatorNumber;
        var newName = revisedName ?? entity.TargetName;
        var newKpi = revisedKpi ?? entity.KpiDescription;
        if (oldIndicator == newIndicator && oldName == newName && oldKpi == newKpi)
            return BadRequest(new ApiResponse<IpmsTargetResponse>(false, null, "At least one field-specific revision must change."));

        AddDefinitionRevision(entity, nameof(entity.IndicatorNumber), oldIndicator, newIndicator, request, user.Id);
        AddDefinitionRevision(entity, nameof(entity.TargetName), oldName, newName, request, user.Id);
        AddDefinitionRevision(entity, nameof(entity.KpiDescription), oldKpi, newKpi, request, user.Id);
        entity.IsIndicatorNumberRevised = request.IsIndicatorNumberRevised;
        entity.RevisedIndicatorNumber = revisedIndicator;
        entity.IsTargetNameRevised = request.IsTargetNameRevised;
        entity.RevisedTargetName = revisedName;
        entity.IsKpiDescriptionRevised = request.IsKpiDescriptionRevised;
        entity.RevisedKpiDescription = revisedKpi;
        entity.IsRevised = request.IsIndicatorNumberRevised || request.IsTargetNameRevised || request.IsKpiDescriptionRevised;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<IpmsTargetResponse>(false, null, "The KPI definition changed since it was loaded. Refresh and try again.")); }

        var after = await FindTargetAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsTarget", id, "ReviseDefinitionFields", new { IndicatorNumber = oldIndicator, TargetName = oldName, KpiDescription = oldKpi }, new { IndicatorNumber = newIndicator, TargetName = newName, KpiDescription = newKpi, request.Reason, request.ApprovalReference, request.EffectiveAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsTargetResponse>(true, after.ToResponse()));
    }

    [HttpGet("{id}/ordering-revisions")]
    public ActionResult<ApiResponse<KpiFieldRevisionResponse[]>> GetOrderingRevisions(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<KpiFieldRevisionResponse[]>(false, null,
            $"This unbounded ordering-revision route is retired. Use /api/v1/ipms-targets/{id}/ordering-revisions/page."));

    [HttpGet("{id}/ordering-revisions/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<KpiFieldRevisionResponse>>>> GetOrderingRevisionsPage(string id, [FromQuery] PagedQueryRequest request)
        => await GetRevisionsPage(id, [nameof(IpmsTarget.OriginalOrderNumber), nameof(IpmsTarget.RevisedOrderNumber)], request);

    [HttpGet("{id}/field-revisions")]
    public ActionResult<ApiResponse<KpiFieldRevisionResponse[]>> GetFieldRevisions(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<KpiFieldRevisionResponse[]>(false, null,
            $"This unbounded field-revision route is retired. Use /api/v1/ipms-targets/{id}/field-revisions/page."));

    [HttpGet("{id}/field-revisions/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<KpiFieldRevisionResponse>>>> GetFieldRevisionsPage(string id, [FromQuery] PagedQueryRequest request)
        => await GetRevisionsPage(id, [nameof(IpmsTarget.IndicatorNumber), nameof(IpmsTarget.TargetName), nameof(IpmsTarget.KpiDescription)], request);

    private async Task<ActionResult<ApiResponse<PagedResponse<KpiFieldRevisionResponse>>>> GetRevisionsPage(string id, string[] fieldNames, PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<KpiFieldRevisionResponse>>(false, null, "User not found"));
        var entity = await _context.IpmsTargets.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<PagedResponse<KpiFieldRevisionResponse>>(false, null, "IPMS target not found"));
        var scope = BuildScope(entity);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_KPI.READ", scope);
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PagedResponse<KpiFieldRevisionResponse>>(false, null, decision.Reason));
        async Task<bool> CanRead(string member) => (await _accessControlService.CheckPermissionAsync(user, $"IPMS_KPI.{member}.READ", scope)).Allowed;
        var memberAccess = new KpiRevisionMemberAccess(
            await CanRead("RevisionOriginalValue"), await CanRead("RevisionRevisedValue"), await CanRead("RevisionReason"),
            await CanRead("RevisionApprovalReference"), await CanRead("RevisionActor"));
        if (request.NormalizedSortBy is not ("createdat" or "recordedat" or "effectiveat" or "fieldname" or "revisedby" or "revisedbyuserid"))
            return BadRequest(new ApiResponse<PagedResponse<KpiFieldRevisionResponse>>(false, null, "SortBy must be recordedAt, effectiveAt, fieldName, or revisedBy."));
        if (request.NormalizedSortBy is "revisedby" or "revisedbyuserid" && !memberAccess.Actor)
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PagedResponse<KpiFieldRevisionResponse>>(false, null, "Revision actor sorting requires RevisionActor READ permission."));
        var query = _context.KpiFieldRevisions.AsNoTracking().Where(item => item.IpmsTargetId == entity.Id && fieldNames.Contains(item.FieldName));
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.FieldName.Contains(request.NormalizedSearch)
                || memberAccess.Reason && item.Reason.Contains(request.NormalizedSearch)
                || memberAccess.ApprovalReference && item.ApprovalReference.Contains(request.NormalizedSearch)
                || memberAccess.Actor && (item.RevisedByUser.FirstName.Contains(request.NormalizedSearch) || item.RevisedByUser.LastName.Contains(request.NormalizedSearch))
                || memberAccess.OriginalValue && item.OriginalValue != null && item.OriginalValue.Contains(request.NormalizedSearch)
                || memberAccess.RevisedValue && item.RevisedValue != null && item.RevisedValue.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("effectiveat", false) => query.OrderBy(item => item.EffectiveAt).ThenBy(item => item.PublicId),
            ("effectiveat", true) => query.OrderByDescending(item => item.EffectiveAt).ThenByDescending(item => item.PublicId),
            ("fieldname", false) => query.OrderBy(item => item.FieldName).ThenBy(item => item.PublicId),
            ("fieldname", true) => query.OrderByDescending(item => item.FieldName).ThenByDescending(item => item.PublicId),
            ("revisedby" or "revisedbyuserid", false) => query.OrderBy(item => item.RevisedByUser.LastName).ThenBy(item => item.RevisedByUser.FirstName).ThenBy(item => item.RevisedByUser.PublicId).ThenBy(item => item.PublicId),
            ("revisedby" or "revisedbyuserid", true) => query.OrderByDescending(item => item.RevisedByUser.LastName).ThenByDescending(item => item.RevisedByUser.FirstName).ThenByDescending(item => item.RevisedByUser.PublicId).ThenByDescending(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.RecordedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.RecordedAt).ThenByDescending(item => item.PublicId)
        };
        var items = await ordered.Skip(request.Offset).Take(request.PageSize).Include(item => item.RevisedByUser).ToArrayAsync();
        var rows = items.Select(item => new KpiFieldRevisionResponse(item.PublicId, item.FieldName,
            memberAccess.OriginalValue ? item.OriginalValue : null, memberAccess.RevisedValue ? item.RevisedValue : null,
            memberAccess.Reason ? item.Reason : null, memberAccess.ApprovalReference ? item.ApprovalReference : null,
            item.EffectiveAt, memberAccess.Actor ? item.RevisedByUser.PublicId : null,
            memberAccess.Actor ? item.RevisedByUser.FullName : null, item.RecordedAt)).ToArray();
        return Ok(new ApiResponse<PagedResponse<KpiFieldRevisionResponse>>(true,
            PagedResponse<KpiFieldRevisionResponse>.Create(rows, request.Page, request.PageSize, totalCount)));
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

    private void AddDefinitionRevision(IpmsTarget entity, string fieldName, string? originalValue, string? revisedValue, ReviseKpiDefinitionRequest request, string userId)
    {
        if (string.Equals(originalValue, revisedValue, StringComparison.Ordinal)) return;
        _context.KpiFieldRevisions.Add(new KpiFieldRevision
        {
            MunicipalityId = entity.MunicipalityId!.Value,
            IpmsTargetId = entity.Id,
            FieldName = fieldName,
            OriginalValue = originalValue,
            RevisedValue = revisedValue,
            Reason = request.Reason.Trim(),
            ApprovalReference = request.ApprovalReference.Trim(),
            EffectiveAt = request.EffectiveAt,
            RevisedByUserId = userId
        });
    }

    private static string? ValidateDefinitionRevision(ReviseKpiDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 2000 || string.IsNullOrWhiteSpace(request.ApprovalReference) || request.ApprovalReference.Trim().Length > 500 || request.EffectiveAt == default)
            return "Reason, approval reference and effective date are required.";
        if (request.IsIndicatorNumberRevised && (string.IsNullOrWhiteSpace(request.RevisedIndicatorNumber) || request.RevisedIndicatorNumber.Trim().Length > 100))
            return "A revised KPI number of at most 100 characters is required when its revision flag is active.";
        if (request.IsTargetNameRevised && (string.IsNullOrWhiteSpace(request.RevisedTargetName) || request.RevisedTargetName.Trim().Length > 1000))
            return "A revised target name of at most 1000 characters is required when its revision flag is active.";
        if (request.IsKpiDescriptionRevised && (string.IsNullOrWhiteSpace(request.RevisedKpiDescription) || request.RevisedKpiDescription.Trim().Length > 2000))
            return "Revised KPI wording of at most 2000 characters is required when its revision flag is active.";
        return null;
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
            .Include(item => item.NationalKpaReference)
            .Include(item => item.MunicipalKpaReference)
            .Include(item => item.BackToBasicsPillarReference)
            .Include(item => item.StrategicGoalMaster)
            .Include(item => item.StrategicInterventionReference)
            .Include(item => item.StrategicObjectiveMaster)
            .Include(item => item.PerformanceObjectiveReference)
            .Include(item => item.BudgetTypeMaster)
            .Include(item => item.KpiTypeMaster).Include(item => item.IndicatorTypeMaster).Include(item => item.FunctionalAreaMaster).Include(item => item.KpiUnitOfMeasureMaster)
            .Include(item => item.GovernedBudgetSources).ThenInclude(item => item.BudgetSource)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (target != null) await TargetPeriodCutover.HydrateCanonicalRowsAsync(_context, [target]);
        return target;
    }

    private static AccessScopeContext BuildScope(IpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, MunicipalityId: target.MunicipalityId);
}

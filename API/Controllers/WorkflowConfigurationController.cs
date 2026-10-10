using System.Security.Claims;
using System.Text.Json;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/workflow")]
[Authorize]
public sealed class WorkflowConfigurationController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    IAccessControlService accessControl,
    IConfigurableWorkflowService workflows,
    IReportingWindowService windows,
    IWorkflowGovernanceService governance,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("definitions/page")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<PagedResponse<WorkflowDefinitionDto>>>> GetDefinitionsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] SubmissionKind? submissionKind = null,
        [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<WorkflowDefinitionDto>>();
        if (!WorkflowDefinitionSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<WorkflowDefinitionDto>>("SortBy must be createdAt, code, name, version, effectiveFrom, or financialYear."));
        var query = context.WorkflowDefinitions.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(search) || x.Name.ToLower().Contains(search)
                || x.MunicipalityFinancialYear.FinancialYear.Code.ToLower().Contains(search));
        }
        if (submissionKind.HasValue) query = query.Where(x => x.SubmissionKind == submissionKind.Value);
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        var totalCount = await query.CountAsync();
        var entities = await ApplyWorkflowDefinitionOrdering(query, request.NormalizedSortBy, request.Descending)
            .Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear)
            .Include(x => x.Stages).ThenInclude(x => x.RatingScheme)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<WorkflowDefinitionDto>>(true,
            PagedResponse<WorkflowDefinitionDto>.Create(entities.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("definitions")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public ActionResult<ApiResponse<WorkflowDefinitionDto[]>> GetDefinitions()
    {
        if (!HasTenant()) return TenantRequired<WorkflowDefinitionDto[]>();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<WorkflowDefinitionDto[]>("This unbounded route is retired. Use the /definitions/page endpoint."));
    }

    [HttpPost("definitions")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> CreateDefinition(SaveWorkflowDefinitionRequest request)
    {
        if (!HasTenant()) return TenantRequired<WorkflowDefinitionDto>();
        var actor = await CurrentUser();
        if (actor == null) return Unauthorized(Fail<WorkflowDefinitionDto>("User not found."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(reason) || reason.Length < 10 || reason.Length > 1000)
            return BadRequest(Fail<WorkflowDefinitionDto>("Code, name, and a governance reason of 10 to 1000 characters are required."));
        if (request.Stages.Count == 0 || request.Stages.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.RequiredActionCode) || string.IsNullOrWhiteSpace(x.RequiredPermissionCode) || x.Sequence < 1) || request.Stages.Select(x => x.Code.Trim().ToUpperInvariant()).Distinct().Count() != request.Stages.Count || request.Stages.Select(x => x.Sequence).Distinct().Count() != request.Stages.Count)
            return BadRequest(Fail<WorkflowDefinitionDto>("Workflow requires stages with unique codes and sequences."));
        var stageCodes = request.Stages.Select(x => x.Code.Trim().ToUpperInvariant()).ToHashSet();
        if (request.Stages.Any(x => !string.IsNullOrWhiteSpace(x.RejectionStageCode) && (!stageCodes.Contains(x.RejectionStageCode.Trim().ToUpperInvariant()) || request.Stages.Single(s => string.Equals(s.Code.Trim(), x.RejectionStageCode.Trim(), StringComparison.OrdinalIgnoreCase)).Sequence >= x.Sequence)))
            return BadRequest(Fail<WorkflowDefinitionDto>("A rejection stage must reference an earlier stage in the same workflow."));
        if (request.Stages.Count(x => x.IsTerminal) != 1 || !request.Stages.OrderBy(x => x.Sequence).Last().IsTerminal)
            return BadRequest(Fail<WorkflowDefinitionDto>("Workflow requires exactly one terminal stage and it must be last."));
        if (request.Stages.Any(x => x.RequiresRating && !x.RatingSchemePublicId.HasValue))
            return BadRequest(Fail<WorkflowDefinitionDto>("Every rating-required stage must reference an active rating scheme."));
        var year = await context.MunicipalityFinancialYears.Include(x => x.FinancialYear).SingleOrDefaultAsync(x => x.PublicId == request.MunicipalityFinancialYearPublicId);
        if (year == null) return BadRequest(Fail<WorkflowDefinitionDto>("Municipality financial year not found."));
        var permissionCodes = request.Stages.Select(x => x.RequiredPermissionCode.Trim().ToUpperInvariant()).Distinct().ToArray();
        var actionCodes = request.Stages.Select(x => x.RequiredActionCode.Trim().ToUpperInvariant()).Distinct().ToArray();
        var registeredActions = await context.SecurityActionDefinitions.Where(x => actionCodes.Contains(x.Code) && x.IsActive).Select(x => x.Code).ToArrayAsync();
        if (registeredActions.Length != actionCodes.Length) return BadRequest(Fail<WorkflowDefinitionDto>("Every workflow stage must reference an active registered action."));
        var registeredPermissions = await context.Permissions.Where(x => permissionCodes.Contains(x.Code) && x.IsActive).Select(x => new { x.Code, x.ActionCode }).ToArrayAsync();
        if (registeredPermissions.Length != permissionCodes.Length) return BadRequest(Fail<WorkflowDefinitionDto>("Every workflow stage must reference an active registered permission."));
        var permissionActions = registeredPermissions.ToDictionary(x => x.Code, x => x.ActionCode, StringComparer.OrdinalIgnoreCase);
        if (request.Stages.Any(x => !permissionActions.TryGetValue(x.RequiredPermissionCode.Trim(), out var permissionAction) || !string.Equals(permissionAction, x.RequiredActionCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            return BadRequest(Fail<WorkflowDefinitionDto>("Each workflow permission must authorize its configured action."));
        var ratingSchemePublicIds = request.Stages.Where(x => x.RatingSchemePublicId.HasValue).Select(x => x.RatingSchemePublicId!.Value).Distinct().ToArray();
        var ratingSchemes = await context.RatingSchemes.Include(x => x.Values).Where(x => ratingSchemePublicIds.Contains(x.PublicId) && x.IsActive).ToDictionaryAsync(x => x.PublicId);
        if (ratingSchemes.Count != ratingSchemePublicIds.Length)
            return BadRequest(Fail<WorkflowDefinitionDto>("Every referenced rating scheme must be active in the selected municipality."));
        var code = request.Code.Trim().ToUpperInvariant();
        var version = (await context.WorkflowDefinitions.Where(x => x.MunicipalityFinancialYearId == year.Id && x.SubmissionKind == request.SubmissionKind && x.Code == code).MaxAsync(x => (int?)x.Version) ?? 0) + 1;
        WorkflowDefinition[] previous = [];
        var previousSnapshots = new Dictionary<Guid, object>();
        if (request.IsActive)
        {
            previous = await context.WorkflowDefinitions.Include(x => x.MunicipalityFinancialYear).Include(x => x.Stages).ThenInclude(x => x.RatingScheme).Where(x => x.MunicipalityFinancialYearId == year.Id && x.SubmissionKind == request.SubmissionKind && x.Code == code && x.IsActive && (!x.EffectiveTo.HasValue || x.EffectiveTo > request.EffectiveFrom)).ToArrayAsync();
            if (previous.Any(item => request.EffectiveFrom <= item.EffectiveFrom))
                return BadRequest(Fail<WorkflowDefinitionDto>("A replacement version must become effective after every active predecessor."));
            foreach (var item in previous)
            {
                previousSnapshots[item.PublicId] = new { item.Version, item.IsActive, item.EffectiveTo };
                item.EffectiveTo = request.EffectiveFrom;
            }
        }
        var entity = new WorkflowDefinition { MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = request.SubmissionKind, Code = code, Name = request.Name.Trim(), Version = version, IsActive = request.IsActive, EffectiveFrom = request.EffectiveFrom };
        foreach (var stage in request.Stages.OrderBy(x => x.Sequence)) entity.Stages.Add(new WorkflowStageDefinition
        {
            MunicipalityId = tenantContext.MunicipalityId.Value, Code = stage.Code.Trim().ToUpperInvariant(), Name = stage.Name.Trim(), Sequence = stage.Sequence,
            RequiredActionCode = stage.RequiredActionCode.Trim().ToUpperInvariant(), RequiredPermissionCode = stage.RequiredPermissionCode.Trim().ToUpperInvariant(), IsOptional = stage.IsOptional,
            AllowBypass = stage.AllowBypass, RequireDifferentActorFromSubmitter = stage.RequireDifferentActorFromSubmitter,
            RequireDifferentActorFromPreviousStage = stage.RequireDifferentActorFromPreviousStage, IsTerminal = stage.IsTerminal,
            RejectionStageCode = stage.RejectionStageCode?.Trim().ToUpperInvariant(), RequiresRating = stage.RequiresRating,
            RatingSchemeId = stage.RatingSchemePublicId.HasValue ? ratingSchemes[stage.RatingSchemePublicId.Value].Id : null,
            RatingScheme = stage.RatingSchemePublicId.HasValue ? ratingSchemes[stage.RatingSchemePublicId.Value] : null
        });
        context.WorkflowDefinitions.Add(entity);
        foreach (var item in previous)
            governance.QueueAuditTrail("WorkflowDefinition", item.PublicId.ToString(), "Supersede", previousSnapshots[item.PublicId], new { item.Version, item.IsActive, item.EffectiveTo, Reason = reason, SuccessorPublicId = entity.PublicId }, actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        governance.QueueAuditTrail("WorkflowDefinition", entity.PublicId.ToString(), "CreateVersion", null, new { entity.Code, entity.Name, entity.Version, entity.IsActive, entity.EffectiveFrom, Reason = reason }, actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<WorkflowDefinitionDto>(true, ToDto(entity)));
    }

    [HttpGet("definitions/compare")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionComparisonDto>>> CompareDefinitions([FromQuery] Guid from, [FromQuery] Guid to)
    {
        if (!HasTenant()) return TenantRequired<WorkflowDefinitionComparisonDto>();
        if (from == Guid.Empty || to == Guid.Empty || from == to) return BadRequest(Fail<WorkflowDefinitionComparisonDto>("Select two different workflow versions."));
        var ids = new[] { from, to };
        var definitions = await context.WorkflowDefinitions.IgnoreQueryFilters().AsNoTracking()
            .Include(x => x.MunicipalityFinancialYear)
            .Include(x => x.Stages).ThenInclude(x => x.RatingScheme)
            .Where(x => x.MunicipalityId == tenantContext.MunicipalityId!.Value && ids.Contains(x.PublicId)).ToArrayAsync();
        if (definitions.Length != 2) return NotFound(Fail<WorkflowDefinitionComparisonDto>("One or both workflow versions were not found in the selected municipality."));
        var prior = definitions.Single(x => x.PublicId == from);
        var current = definitions.Single(x => x.PublicId == to);
        if (prior.MunicipalityFinancialYearId != current.MunicipalityFinancialYearId || prior.SubmissionKind != current.SubmissionKind || !string.Equals(prior.Code, current.Code, StringComparison.OrdinalIgnoreCase))
            return BadRequest(Fail<WorkflowDefinitionComparisonDto>("Workflow comparison requires versions from the same code, financial year, and submission type."));
        var changes = WorkflowDefinitionComparer.Compare(prior, current)
            .Select(change => new WorkflowStageDifferenceDto(change.Change, change.StageCode, change.FromSequence, change.ToSequence, change.ChangedFields)).ToArray();
        return Ok(new ApiResponse<WorkflowDefinitionComparisonDto>(true, new(ToDto(prior), ToDto(current), changes)));
    }

    [HttpPost("definitions/{publicId:guid}/retire")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> RetireDefinition(Guid publicId, RetireWorkflowDefinitionRequest request)
    {
        if (!HasTenant()) return TenantRequired<WorkflowDefinitionDto>();
        var actor = await CurrentUser();
        if (actor == null) return Unauthorized(Fail<WorkflowDefinitionDto>("User not found."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10 || reason.Length > 1000)
            return BadRequest(Fail<WorkflowDefinitionDto>("A retirement reason of 10 to 1000 characters is required."));
        var entity = await context.WorkflowDefinitions.IgnoreQueryFilters().Include(x => x.MunicipalityFinancialYear).Include(x => x.Stages).ThenInclude(x => x.RatingScheme).SingleOrDefaultAsync(x => x.MunicipalityId == tenantContext.MunicipalityId!.Value && x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<WorkflowDefinitionDto>("Workflow definition not found."));
        if (!entity.IsActive) return Conflict(Fail<WorkflowDefinitionDto>("Workflow definition is already inactive."));
        DateTime effectiveTo = request.EffectiveTo ?? DateTime.UtcNow;
        if (effectiveTo < entity.EffectiveFrom) return BadRequest(Fail<WorkflowDefinitionDto>("Retirement cannot predate the workflow's effective start."));
        try { context.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return BadRequest(Fail<WorkflowDefinitionDto>("A valid RowVersion is required.")); }
        var before = new { entity.IsActive, entity.EffectiveTo, entity.Version };
        entity.IsActive = false;
        entity.EffectiveTo = effectiveTo;
        governance.QueueAuditTrail("WorkflowDefinition", entity.PublicId.ToString(), "Retire", before, new { entity.IsActive, entity.EffectiveTo, entity.Version, Reason = reason }, actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<WorkflowDefinitionDto>("The workflow definition changed; reload before retiring it.")); }
        return Ok(new ApiResponse<WorkflowDefinitionDto>(true, ToDto(entity), "Workflow retired. Existing instances remain pinned to this version."));
    }

    [HttpGet("reporting-windows/page")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportingWindowDto>>>> GetWindowsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] SubmissionKind? submissionKind = null,
        [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<ReportingWindowDto>>();
        if (!ReportingWindowSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<ReportingWindowDto>>("SortBy must be opensAt, closesAt, period, or submissionKind."));
        var query = context.ReportingWindows.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            query = query.Where(x => x.ReportingPeriod.Code.ToLower().Contains(search) || x.ReportingPeriod.Name.ToLower().Contains(search));
        }
        if (submissionKind.HasValue) query = query.Where(x => x.SubmissionKind == submissionKind.Value);
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        var totalCount = await query.CountAsync();
        var entities = await ApplyReportingWindowOrdering(query, request.NormalizedSortBy, request.Descending)
            .Include(x => x.ReportingPeriod)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<ReportingWindowDto>>(true,
            PagedResponse<ReportingWindowDto>.Create(entities.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("reporting-windows")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public ActionResult<ApiResponse<ReportingWindowDto[]>> GetWindows()
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowDto[]>();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<ReportingWindowDto[]>("This unbounded route is retired. Use the /reporting-windows/page endpoint."));
    }

    [HttpPost("reporting-windows")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<ReportingWindowDto>>> CreateWindow(SaveReportingWindowRequest request)
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowDto>();
        var actor = await CurrentUser();
        if (actor == null) return Unauthorized(Fail<ReportingWindowDto>("User not found."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 10 or > 1000)
            return BadRequest(Fail<ReportingWindowDto>("A governance reason of 10 to 1000 characters is required."));
        if (request.OpensAt.Kind == DateTimeKind.Unspecified || request.ClosesAt.Kind == DateTimeKind.Unspecified || request.ClosesAt <= request.OpensAt)
            return BadRequest(Fail<ReportingWindowDto>("Timezone-aware opening and closing times are required, and close must be after open."));
        var period = await context.ReportingPeriods.SingleOrDefaultAsync(x => x.PublicId == request.ReportingPeriodPublicId);
        if (period == null) return BadRequest(Fail<ReportingWindowDto>("Reporting period not found."));
        if (await context.ReportingWindows.AnyAsync(x => x.ReportingPeriodId == period.Id && x.SubmissionKind == request.SubmissionKind)) return Conflict(Fail<ReportingWindowDto>("A reporting window already exists for this period and submission type."));
        var entity = new ReportingWindow { MunicipalityId = tenantContext.MunicipalityId!.Value, ReportingPeriodId = period.Id, ReportingPeriod = period, SubmissionKind = request.SubmissionKind, OpensAt = request.OpensAt.ToUniversalTime(), ClosesAt = request.ClosesAt.ToUniversalTime() };
        context.ReportingWindows.Add(entity);
        governance.QueueAuditTrail(nameof(ReportingWindow), entity.PublicId.ToString(), "Create", null,
            new { ReportingPeriodPublicId = period.PublicId, entity.SubmissionKind, entity.OpensAt, entity.ClosesAt },
            actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext), reason);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<ReportingWindowDto>(true, ToDto(entity)));
    }

    [HttpGet("reporting-windows/{windowPublicId:guid}/exceptions/page")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportingWindowExceptionDto>>>> GetWindowExceptionsPage(
        Guid windowPublicId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? scope = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<ReportingWindowExceptionDto>>();
        var window = await context.ReportingWindows.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == windowPublicId);
        if (window == null) return NotFound(Fail<PagedResponse<ReportingWindowExceptionDto>>("Reporting window not found."));
        var actor = await CurrentUser();
        if (actor == null) return Unauthorized(Fail<PagedResponse<ReportingWindowExceptionDto>>("User not found."));
        var memberAccess = await ReadWindowExceptionMembersAsync(actor, window.SubmissionKind);
        if (!ReportingWindowExceptionSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<ReportingWindowExceptionDto>>("SortBy must be approvedAt, extendedClosesAt, or scope."));
        var normalizedScope = scope?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedScope is not ("" or "user" or "department" or "unit"))
            return BadRequest(Fail<PagedResponse<ReportingWindowExceptionDto>>("Scope must be user, department, or unit."));
        if ((request.NormalizedSortBy == "scope" || normalizedScope.Length > 0) && !memberAccess.ScopeRead) return Forbid();
        var query = context.ReportingWindowExceptions.AsNoTracking().Where(x => x.ReportingWindowId == window.Id);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            query = query.Where(x => (memberAccess.ReasonRead && x.Reason.ToLower().Contains(search))
                || (memberAccess.ScopeRead && x.UserId != null && x.UserId.ToLower().Contains(search))
                || (memberAccess.ApprovedByRead && x.ApprovedByUserId.ToLower().Contains(search)));
        }
        query = normalizedScope switch
        {
            "user" => query.Where(x => x.UserId != null),
            "department" => query.Where(x => x.DepartmentId.HasValue),
            "unit" => query.Where(x => x.UnitId.HasValue),
            _ => query
        };
        var totalCount = await query.CountAsync();
        var rows = await ApplyReportingWindowExceptionOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var responseRows = await ToWindowExceptionDtosAsync(rows, memberAccess);
        return Ok(new ApiResponse<PagedResponse<ReportingWindowExceptionDto>>(true,
            PagedResponse<ReportingWindowExceptionDto>.Create(responseRows, request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("reporting-windows/{windowPublicId:guid}/exceptions")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<ReportingWindowExceptionDto[]>>> GetWindowExceptions(Guid windowPublicId)
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowExceptionDto[]>();
        var window = await context.ReportingWindows.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == windowPublicId);
        if (window == null) return NotFound(Fail<ReportingWindowExceptionDto[]>("Reporting window not found."));
        return StatusCode(StatusCodes.Status410Gone,
            Fail<ReportingWindowExceptionDto[]>("This unbounded route is retired. Use the /exceptions/page endpoint."));
    }

    [HttpPost("reporting-windows/{windowPublicId:guid}/exceptions")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<ReportingWindowExceptionDto>>> CreateWindowException(Guid windowPublicId, SaveReportingWindowExceptionRequest request)
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowExceptionDto>();
        var actor = await CurrentUser();
        if (actor == null) return Unauthorized(Fail<ReportingWindowExceptionDto>("User not found."));
        var window = await context.ReportingWindows.SingleOrDefaultAsync(x => x.PublicId == windowPublicId);
        if (window == null) return NotFound(Fail<ReportingWindowExceptionDto>("Reporting window not found."));
        var memberAccess = await ReadWindowExceptionMembersAsync(actor, window.SubmissionKind);
        if (!memberAccess.ScopeUpdate || !memberAccess.ReasonUpdate) return Forbid();
        if (request.ExtendedClosesAt <= window.ClosesAt || string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail<ReportingWindowExceptionDto>("An exception requires a reason and a close time after the normal window."));
        var scopeCount = (request.UserPublicId.HasValue ? 1 : 0) + (request.DepartmentPublicId.HasValue ? 1 : 0) + (request.UnitPublicId.HasValue ? 1 : 0);
        if (scopeCount != 1) return BadRequest(Fail<ReportingWindowExceptionDto>("Select exactly one user, department, or unit scope."));
        string? userId = null; int? departmentId = null; int? unitId = null;
        if (request.UserPublicId.HasValue)
        {
            userId = await context.Users.Where(x => x.PublicId == request.UserPublicId && x.IsActive && context.SecurityUserRoleAssignments.Any(a => a.UserId == x.Id && a.MunicipalityId == tenantContext.MunicipalityId && a.IsActive)).Select(x => x.Id).SingleOrDefaultAsync();
            if (userId == null) return BadRequest(Fail<ReportingWindowExceptionDto>("Scoped user is not active in this municipality."));
        }
        if (request.DepartmentPublicId.HasValue)
        {
            departmentId = await context.Departments.Where(x => x.PublicId == request.DepartmentPublicId && x.IsActive).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            if (!departmentId.HasValue) return BadRequest(Fail<ReportingWindowExceptionDto>("Department not found."));
        }
        if (request.UnitPublicId.HasValue)
        {
            unitId = await context.Units.Where(x => x.PublicId == request.UnitPublicId && x.IsActive).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            if (!unitId.HasValue) return BadRequest(Fail<ReportingWindowExceptionDto>("Unit not found."));
        }
        var entity = new ReportingWindowException { MunicipalityId = tenantContext.MunicipalityId!.Value, ReportingWindowId = window.Id, ReportingWindow = window, UserId = userId, DepartmentId = departmentId, UnitId = unitId, ExtendedClosesAt = request.ExtendedClosesAt, Reason = request.Reason.Trim(), ApprovedByUserId = actor.Id };
        context.ReportingWindowExceptions.Add(entity);
        context.AuditTrails.Add(new AuditTrail { EntityName = nameof(ReportingWindowException), EntityId = entity.PublicId.ToString(), Action = "Create", NewValue = JsonSerializer.Serialize(request), ChangedBy = actor.Id, Reason = entity.Reason, CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
        await context.SaveChangesAsync();
        var response = (await ToWindowExceptionDtosAsync([entity], memberAccess))[0];
        return Ok(new ApiResponse<ReportingWindowExceptionDto>(true, response));
    }

    [HttpGet("rating-schemes/page")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<PagedResponse<RatingSchemeDto>>>> GetRatingSchemesPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<RatingSchemeDto>>();
        if (!RatingSchemeSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<RatingSchemeDto>>("SortBy must be code, name, or createdAt."));
        var query = context.RatingSchemes.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(search) || x.Name.ToLower().Contains(search)
                || x.Values.Any(value => value.Label.ToLower().Contains(search)));
        }
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        var totalCount = await query.CountAsync();
        var rows = await ApplyRatingSchemeOrdering(query, request.NormalizedSortBy, request.Descending)
            .Include(x => x.Values)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<RatingSchemeDto>>(true,
            PagedResponse<RatingSchemeDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("rating-schemes")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public ActionResult<ApiResponse<RatingSchemeDto[]>> GetRatingSchemes()
    {
        if (!HasTenant()) return TenantRequired<RatingSchemeDto[]>();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<RatingSchemeDto[]>("This unbounded route is retired. Use the /rating-schemes/page endpoint."));
    }

    [HttpPost("rating-schemes")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<RatingSchemeDto>>> CreateRatingScheme(SaveRatingSchemeRequest request)
    {
        if (!HasTenant()) return TenantRequired<RatingSchemeDto>();
        var actor = await CurrentUser();
        if (actor == null) return Unauthorized(Fail<RatingSchemeDto>("User not found."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 10 or > 1000)
            return BadRequest(Fail<RatingSchemeDto>("A governance reason of 10 to 1000 characters is required."));
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || request.Values.Count == 0) return BadRequest(Fail<RatingSchemeDto>("Code, name, and rating values are required."));
        if (request.Values.Select(x => x.Value).Distinct().Count() != request.Values.Count || request.Values.Select(x => x.SortOrder).Distinct().Count() != request.Values.Count) return BadRequest(Fail<RatingSchemeDto>("Rating values and sort orders must be unique."));
        if (request.Values.Any(x => x.MinimumAchievementPercent.HasValue != x.MaximumAchievementPercent.HasValue || x.MinimumAchievementPercent > x.MaximumAchievementPercent)) return BadRequest(Fail<RatingSchemeDto>("Rating achievement ranges must have valid minimum and maximum values."));
        var ranges = request.Values.Where(x => x.MinimumAchievementPercent.HasValue).OrderBy(x => x.MinimumAchievementPercent).ToArray();
        if (ranges.Zip(ranges.Skip(1), (left, right) => left.MaximumAchievementPercent >= right.MinimumAchievementPercent).Any(overlap => overlap)) return BadRequest(Fail<RatingSchemeDto>("Rating achievement ranges must not overlap."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.RatingSchemes.AnyAsync(x => x.Code == code)) return Conflict(Fail<RatingSchemeDto>("Rating scheme code already exists."));
        var entity = new RatingScheme { MunicipalityId = tenantContext.MunicipalityId!.Value, Code = code, Name = request.Name.Trim(), IsActive = true };
        foreach (var value in request.Values.OrderBy(x => x.SortOrder)) entity.Values.Add(new RatingSchemeValue { MunicipalityId = tenantContext.MunicipalityId.Value, Value = value.Value, Label = value.Label.Trim(), MinimumAchievementPercent = value.MinimumAchievementPercent, MaximumAchievementPercent = value.MaximumAchievementPercent, SortOrder = value.SortOrder });
        context.RatingSchemes.Add(entity);
        governance.QueueAuditTrail(nameof(RatingScheme), entity.PublicId.ToString(), "Create", null,
            new { entity.Code, entity.Name, entity.IsActive, Values = entity.Values.OrderBy(x => x.SortOrder).Select(x => new { x.PublicId, x.Value, x.Label, x.MinimumAchievementPercent, x.MaximumAchievementPercent, x.SortOrder }).ToArray() },
            actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext), reason);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<RatingSchemeDto>(true, ToDto(entity)));
    }

    [HttpPost("submissions/{kind}/{submissionId}/actions")]
    public async Task<ActionResult<ApiResponse<WorkflowActionDto>>> Act(SubmissionKind kind, string submissionId, WorkflowActionRequest request)
    {
        if (!HasTenant()) return TenantRequired<WorkflowActionDto>();
        if (kind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return BadRequest(Fail<WorkflowActionDto>("Unsupported submission type."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<WorkflowActionDto>("User not found."));
        long? periodId; string? submitter; int? department; int? unit; string? owner;
        if (kind == SubmissionKind.Opms)
        {
            var submission = await context.OpmsSubmissions.Include(x => x.OpmsTarget).SingleOrDefaultAsync(x => x.Id == submissionId);
            if (submission == null) return NotFound(Fail<WorkflowActionDto>("OPMS submission not found."));
            periodId = submission.ReportingPeriodId; submitter = submission.SubmittedByUserId ?? submission.CreatedBy ?? user.Id; department = submission.OpmsTarget.DepartmentId; unit = submission.OpmsTarget.UnitId; owner = submission.OpmsTarget.AssignedUserId;
        }
        else
        {
            var submission = await context.IpmsSubmissions.Include(x => x.IpmsTarget).SingleOrDefaultAsync(x => x.Id == submissionId);
            if (submission == null) return NotFound(Fail<WorkflowActionDto>("IPMS submission not found."));
            periodId = submission.ReportingPeriodId; submitter = submission.SubmittedByUserId ?? submission.CreatedBy ?? user.Id; department = submission.IpmsTarget.DepartmentId; unit = submission.IpmsTarget.UnitId; owner = submission.IpmsTarget.AssignedUserId;
        }
        if (!periodId.HasValue) return Conflict(Fail<WorkflowActionDto>("Submission has not been reconciled to a reporting period."));
        var scope = new AccessScopeContext(department, unit, owner, TargetId: submissionId, MunicipalityId: tenantContext.MunicipalityId);
        var permission = await accessControl.CheckPermissionAsync(user, request.ActionCode, scope);
        if (!permission.Allowed) return StatusCode(StatusCodes.Status403Forbidden, Fail<WorkflowActionDto>(permission.Reason));
        if (request.Outcome == WorkflowActionOutcome.Submit)
        {
            var window = await windows.CheckAsync(kind, periodId.Value, user.Id, department, unit, DateTime.UtcNow);
            if (!window.Allowed) return Conflict(Fail<WorkflowActionDto>(window.Reason));
        }
        var transition = await workflows.PrepareActionAsync(kind, submissionId, periodId.Value, submitter!, user.Id, request.ActionCode.Trim(), request.Outcome, request.Comment, request.RatingValue, HttpContext.TraceIdentifier);
        if (!transition.Allowed || transition.Action == null) return Conflict(Fail<WorkflowActionDto>(transition.Reason));
        context.AuditTrails.Add(new AuditTrail { EntityName = kind + "Submission", EntityId = submissionId, Action = request.ActionCode.Trim(), NewValue = JsonSerializer.Serialize(new { request.Outcome, request.Comment, request.RatingValue, transition.Reason }), ChangedBy = user.Id, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        await context.SaveChangesAsync();
        var members = await ReadWorkflowMembersAsync(user, kind, scope);
        return Ok(new ApiResponse<WorkflowActionDto>(true, ToDto(transition.Action, members, user), transition.Reason));
    }

    [HttpGet("submissions/{kind}/{submissionId}/actions/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<WorkflowActionDto>>>> HistoryPage(
        SubmissionKind kind,
        string submissionId,
        [FromQuery] PagedQueryRequest request)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<WorkflowActionDto>>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<PagedResponse<WorkflowActionDto>>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PagedResponse<WorkflowActionDto>>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        if (!WorkflowActionSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<WorkflowActionDto>>("SortBy must be occurredAt, sequence, actionCode, or actor."));
        var members = await ReadWorkflowMembersAsync(user, kind, access.Scope);
        if (request.NormalizedSortBy == "actor" && !members.ActionActorUserId) return Forbid();
        if (access.Instance == null)
            return Ok(new ApiResponse<PagedResponse<WorkflowActionDto>>(true, PagedResponse<WorkflowActionDto>.Empty(request.Page, request.PageSize)));

        var query = context.SubmissionWorkflowActions.AsNoTracking()
            .Include(x => x.ActorUser)
            .Where(x => x.SubmissionWorkflowInstanceId == access.Instance.Id);
        if (request.NormalizedSearch.Length > 0)
        {
            var actorPublicId = Guid.TryParse(request.NormalizedSearch, out var parsedActorPublicId)
                ? parsedActorPublicId
                : (Guid?)null;
            query = query.Where(x => x.ActionCode.Contains(request.NormalizedSearch)
                || (members.ActionActorUserId && ((actorPublicId.HasValue && x.ActorUser.PublicId == actorPublicId.Value)
                    || x.ActorUser.FirstName.Contains(request.NormalizedSearch)
                    || x.ActorUser.LastName.Contains(request.NormalizedSearch)))
                || (members.ActionComment && x.Comment != null && x.Comment.Contains(request.NormalizedSearch)));
        }
        var totalCount = await query.CountAsync();
        var rows = await ApplyWorkflowActionOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<WorkflowActionDto>>(true,
            PagedResponse<WorkflowActionDto>.Create(rows.Select(x => ToDto(x, members, x.ActorUser)), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("submissions/{kind}/{submissionId}/actions")]
    public async Task<ActionResult<ApiResponse<WorkflowActionDto[]>>> History(SubmissionKind kind, string submissionId)
    {
        if (!HasTenant()) return TenantRequired<WorkflowActionDto[]>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<WorkflowActionDto[]>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<WorkflowActionDto[]>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<WorkflowActionDto[]>("This unbounded route is retired. Use the /actions/page endpoint."));
    }

    [HttpGet("submissions/{kind}/{submissionId}/ratings/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StageRatingDto>>>> RatingHistoryPage(
        SubmissionKind kind,
        string submissionId,
        [FromQuery] PagedQueryRequest request)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<StageRatingDto>>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<PagedResponse<StageRatingDto>>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PagedResponse<StageRatingDto>>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        if (!StageRatingSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<StageRatingDto>>("SortBy must be ratedAt, stageCode, ratingScheme, value, or actor."));
        var members = await ReadWorkflowMembersAsync(user, kind, access.Scope);
        if (request.NormalizedSortBy == "value" && !members.StageRatingValue) return Forbid();
        if (request.NormalizedSortBy == "actor" && !members.StageRatingRatedByUserId) return Forbid();
        if (access.Instance == null)
            return Ok(new ApiResponse<PagedResponse<StageRatingDto>>(true, PagedResponse<StageRatingDto>.Empty(request.Page, request.PageSize)));

        var query = context.SubmissionStageRatings.AsNoTracking()
            .Where(x => x.SubmissionWorkflowInstanceId == access.Instance.Id);
        if (request.NormalizedSearch.Length > 0)
        {
            var actorPublicId = Guid.TryParse(request.NormalizedSearch, out var parsedActorPublicId)
                ? parsedActorPublicId
                : (Guid?)null;
            query = query.Where(x => x.WorkflowStageDefinition.Code.Contains(request.NormalizedSearch)
                || x.RatingScheme.Code.Contains(request.NormalizedSearch)
                || (members.StageRatingValue && x.LabelSnapshot.Contains(request.NormalizedSearch))
                || (members.StageRatingRatedByUserId && actorPublicId.HasValue && x.RatedByUser.PublicId == actorPublicId.Value)
                || (members.StageRatingRatedByName && (x.RatedByUser.FirstName.Contains(request.NormalizedSearch)
                    || x.RatedByUser.LastName.Contains(request.NormalizedSearch)))
                || (members.StageRatingComment && x.Comment != null && x.Comment.Contains(request.NormalizedSearch)));
        }
        var totalCount = await query.CountAsync();
        var rows = await ApplyStageRatingOrdering(query, request.NormalizedSortBy, request.Descending)
            .Include(x => x.SubmissionWorkflowAction)
            .Include(x => x.WorkflowStageDefinition)
            .Include(x => x.RatingScheme)
            .Include(x => x.RatingSchemeValue)
            .Include(x => x.RatedByUser)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StageRatingDto>>(true,
            PagedResponse<StageRatingDto>.Create(rows.Select(x => ToDto(x, members)), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("submissions/{kind}/{submissionId}/ratings")]
    public async Task<ActionResult<ApiResponse<StageRatingDto[]>>> RatingHistory(SubmissionKind kind, string submissionId)
    {
        if (!HasTenant()) return TenantRequired<StageRatingDto[]>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<StageRatingDto[]>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<StageRatingDto[]>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<StageRatingDto[]>("This unbounded route is retired. Use the /ratings/page endpoint."));
    }

    private static readonly HashSet<string> WorkflowActionSortFields = ["createdat", "occurredat", "sequence", "actioncode", "actor"];
    private static readonly HashSet<string> StageRatingSortFields = ["createdat", "ratedat", "stagecode", "ratingscheme", "value", "actor"];
    private static readonly HashSet<string> WorkflowDefinitionSortFields = ["createdat", "code", "name", "version", "effectivefrom", "financialyear"];
    private static readonly HashSet<string> ReportingWindowSortFields = ["createdat", "opensat", "closesat", "period", "submissionkind"];
    private static readonly HashSet<string> ReportingWindowExceptionSortFields = ["createdat", "approvedat", "extendedclosesat", "scope"];
    private static readonly HashSet<string> RatingSchemeSortFields = ["createdat", "code", "name"];

    private static IOrderedQueryable<WorkflowDefinition> ApplyWorkflowDefinitionOrdering(
        IQueryable<WorkflowDefinition> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("code", false) => query.OrderBy(x => x.Code).ThenBy(x => x.Version).ThenBy(x => x.Id),
            ("code", true) => query.OrderByDescending(x => x.Code).ThenByDescending(x => x.Version).ThenByDescending(x => x.Id),
            ("name", false) => query.OrderBy(x => x.Name).ThenBy(x => x.Id),
            ("name", true) => query.OrderByDescending(x => x.Name).ThenByDescending(x => x.Id),
            ("version", false) => query.OrderBy(x => x.Version).ThenBy(x => x.Id),
            ("version", true) => query.OrderByDescending(x => x.Version).ThenByDescending(x => x.Id),
            ("financialyear", false) => query.OrderBy(x => x.MunicipalityFinancialYear.FinancialYear.Code).ThenBy(x => x.Id),
            ("financialyear", true) => query.OrderByDescending(x => x.MunicipalityFinancialYear.FinancialYear.Code).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id)
        };

    private static IOrderedQueryable<ReportingWindow> ApplyReportingWindowOrdering(
        IQueryable<ReportingWindow> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("closesat", false) => query.OrderBy(x => x.ClosesAt).ThenBy(x => x.Id),
            ("closesat", true) => query.OrderByDescending(x => x.ClosesAt).ThenByDescending(x => x.Id),
            ("period", false) => query.OrderBy(x => x.ReportingPeriod.Code).ThenBy(x => x.Id),
            ("period", true) => query.OrderByDescending(x => x.ReportingPeriod.Code).ThenByDescending(x => x.Id),
            ("submissionkind", false) => query.OrderBy(x => x.SubmissionKind).ThenBy(x => x.Id),
            ("submissionkind", true) => query.OrderByDescending(x => x.SubmissionKind).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.OpensAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.OpensAt).ThenByDescending(x => x.Id)
        };

    private static IOrderedQueryable<RatingScheme> ApplyRatingSchemeOrdering(
        IQueryable<RatingScheme> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("name", false) => query.OrderBy(x => x.Name).ThenBy(x => x.Id),
            ("name", true) => query.OrderByDescending(x => x.Name).ThenByDescending(x => x.Id),
            ("code", false) => query.OrderBy(x => x.Code).ThenBy(x => x.Id),
            ("code", true) => query.OrderByDescending(x => x.Code).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Id)
        };

    private static IOrderedQueryable<ReportingWindowException> ApplyReportingWindowExceptionOrdering(
        IQueryable<ReportingWindowException> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("extendedclosesat", false) => query.OrderBy(x => x.ExtendedClosesAt).ThenBy(x => x.Id),
            ("extendedclosesat", true) => query.OrderByDescending(x => x.ExtendedClosesAt).ThenByDescending(x => x.Id),
            ("scope", false) => query.OrderBy(x => x.UserId != null ? 1 : x.DepartmentId.HasValue ? 2 : 3).ThenBy(x => x.Id),
            ("scope", true) => query.OrderByDescending(x => x.UserId != null ? 1 : x.DepartmentId.HasValue ? 2 : 3).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.ApprovedAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.ApprovedAt).ThenByDescending(x => x.Id)
        };

    private static IOrderedQueryable<SubmissionWorkflowAction> ApplyWorkflowActionOrdering(
        IQueryable<SubmissionWorkflowAction> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("sequence", false) => query.OrderBy(x => x.Sequence).ThenBy(x => x.Id),
            ("sequence", true) => query.OrderByDescending(x => x.Sequence).ThenByDescending(x => x.Id),
            ("actioncode", false) => query.OrderBy(x => x.ActionCode).ThenBy(x => x.Id),
            ("actioncode", true) => query.OrderByDescending(x => x.ActionCode).ThenByDescending(x => x.Id),
            ("actor", false) => query.OrderBy(x => x.ActorUser.PublicId).ThenBy(x => x.Id),
            ("actor", true) => query.OrderByDescending(x => x.ActorUser.PublicId).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.OccurredAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
        };

    private static IOrderedQueryable<SubmissionStageRating> ApplyStageRatingOrdering(
        IQueryable<SubmissionStageRating> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("stagecode", false) => query.OrderBy(x => x.WorkflowStageDefinition.Code).ThenBy(x => x.Id),
            ("stagecode", true) => query.OrderByDescending(x => x.WorkflowStageDefinition.Code).ThenByDescending(x => x.Id),
            ("ratingscheme", false) => query.OrderBy(x => x.RatingScheme.Code).ThenBy(x => x.Id),
            ("ratingscheme", true) => query.OrderByDescending(x => x.RatingScheme.Code).ThenByDescending(x => x.Id),
            ("value", false) => query.OrderBy(x => x.Value).ThenBy(x => x.Id),
            ("value", true) => query.OrderByDescending(x => x.Value).ThenByDescending(x => x.Id),
            ("actor", false) => query.OrderBy(x => x.RatedByUser.PublicId).ThenBy(x => x.Id),
            ("actor", true) => query.OrderByDescending(x => x.RatedByUser.PublicId).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.RatedAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.RatedAt).ThenByDescending(x => x.Id)
        };

    [HttpGet("submissions/{kind}/{submissionId}/rfis/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PerformanceRfiDto>>>> GetRfisPage(
        SubmissionKind kind,
        string submissionId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? status = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<PerformanceRfiDto>>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<PagedResponse<PerformanceRfiDto>>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PagedResponse<PerformanceRfiDto>>("User not found."));
        var resource = kind == SubmissionKind.Opms ? "OPMS_RFI" : "IPMS_RFI";
        var readCode = $"{resource}.READ";
        if (!(await accessControl.CheckPermissionAsync(user, readCode, access.Scope)).Allowed) return Forbid();
        var members = await ReadRfiMembersAsync(user, kind, access.Scope);
        if (!RfiSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<PerformanceRfiDto>>("SortBy must be raisedAt, dueAt, or status."));
        var normalizedStatus = status?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedStatus is not ("" or "open" or "responded" or "closed" or "overdue"))
            return BadRequest(Fail<PagedResponse<PerformanceRfiDto>>("Status must be open, responded, closed, or overdue."));
        if (access.Instance == null)
            return Ok(new ApiResponse<PagedResponse<PerformanceRfiDto>>(true, PagedResponse<PerformanceRfiDto>.Empty(request.Page, request.PageSize)));

        var query = context.PerformanceRfis.AsNoTracking()
            .Where(x => x.SubmissionWorkflowInstanceId == access.Instance.Id);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(x =>
                (members.QuestionRead && x.Question.Contains(search))
                || (members.ResponseRead && x.Response != null && x.Response.Contains(search))
                || (members.RaisedByRead && context.Users.Any(userRow => userRow.Id == x.RaisedByUserId
                    && (userRow.FirstName.Contains(search) || userRow.LastName.Contains(search))))
                || (members.RespondedByRead && x.RespondedByUserId != null && context.Users.Any(userRow => userRow.Id == x.RespondedByUserId
                    && (userRow.FirstName.Contains(search) || userRow.LastName.Contains(search))))
                || (members.ClosedByRead && x.ClosedByUserId != null && context.Users.Any(userRow => userRow.Id == x.ClosedByUserId
                    && (userRow.FirstName.Contains(search) || userRow.LastName.Contains(search))))
                || (members.EvidenceMetadataRead && x.EvidenceLinks.Any(link => link.PoeFile.FileName.Contains(search)
                    || link.PoeFile.Blob.Sha256.Contains(search)))
                || (members.EvidenceLinkedByRead && x.EvidenceLinks.Any(link => context.Users.Any(userRow => userRow.Id == link.LinkedByUserId
                    && (userRow.FirstName.Contains(search) || userRow.LastName.Contains(search))))));
        }
        query = normalizedStatus switch
        {
            "open" => query.Where(x => !x.RespondedAt.HasValue && !x.ClosedAt.HasValue),
            "responded" => query.Where(x => x.RespondedAt.HasValue && !x.ClosedAt.HasValue),
            "closed" => query.Where(x => x.ClosedAt.HasValue),
            "overdue" => query.Where(x => !x.ClosedAt.HasValue && x.ResponseDueAt < DateTime.UtcNow),
            _ => query
        };

        var totalCount = await query.CountAsync();
        var rows = await ApplyRfiOrdering(query, request.NormalizedSortBy, request.Descending)
            .Include(x => x.EvidenceLinks).ThenInclude(x => x.PoeFile).ThenInclude(x => x.Blob)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        var dtos = await ToRfiDtosAsync(rows, members);
        return Ok(new ApiResponse<PagedResponse<PerformanceRfiDto>>(true,
            PagedResponse<PerformanceRfiDto>.Create(dtos, request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("submissions/{kind}/{submissionId}/rfis")]
    public async Task<ActionResult<ApiResponse<PerformanceRfiDto[]>>> GetRfis(SubmissionKind kind, string submissionId)
    {
        if (!HasTenant()) return TenantRequired<PerformanceRfiDto[]>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<PerformanceRfiDto[]>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceRfiDto[]>("User not found."));
        var readCode = kind == SubmissionKind.Opms ? "OPMS_RFI.READ" : "IPMS_RFI.READ";
        if (!(await accessControl.CheckPermissionAsync(user, readCode, access.Scope)).Allowed) return Forbid();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<PerformanceRfiDto[]>("This unbounded route is retired. Use the /rfis/page endpoint."));
    }

    private static readonly HashSet<string> RfiSortFields = ["raisedat", "dueat", "status"];

    private static IOrderedQueryable<PerformanceRfi> ApplyRfiOrdering(
        IQueryable<PerformanceRfi> query,
        string sortBy,
        bool descending) => (sortBy, descending) switch
        {
            ("dueat", false) => query.OrderBy(x => x.ResponseDueAt).ThenBy(x => x.Id),
            ("dueat", true) => query.OrderByDescending(x => x.ResponseDueAt).ThenByDescending(x => x.Id),
            ("status", false) => query.OrderBy(x => x.ClosedAt.HasValue).ThenBy(x => x.RespondedAt.HasValue).ThenBy(x => x.Id),
            ("status", true) => query.OrderByDescending(x => x.ClosedAt.HasValue).ThenByDescending(x => x.RespondedAt.HasValue).ThenByDescending(x => x.Id),
            (_, false) => query.OrderBy(x => x.RaisedAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.RaisedAt).ThenByDescending(x => x.Id)
        };

    [HttpPost("submissions/{kind}/{submissionId}/rfis")]
    public async Task<ActionResult<ApiResponse<PerformanceRfiDto>>> RaiseRfi(SubmissionKind kind, string submissionId, RaisePerformanceRfiRequest request)
    {
        if (!HasTenant()) return TenantRequired<PerformanceRfiDto>();
        if (string.IsNullOrWhiteSpace(request.Question) || request.ResponseDueAt <= DateTime.UtcNow) return BadRequest(Fail<PerformanceRfiDto>("Question and a future response due time are required."));
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<PerformanceRfiDto>("Submission not found."));
        if (access.Instance == null) return Conflict(Fail<PerformanceRfiDto>("The submission has no configured workflow instance."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceRfiDto>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_RFI.RAISE" : "IPMS_RFI.RAISE";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        var members = await ReadRfiMembersAsync(user, kind, access.Scope);
        if (!members.QuestionUpdate) return Forbid();
        var entity = new PerformanceRfi { MunicipalityId = tenantContext.MunicipalityId!.Value, SubmissionWorkflowInstanceId = access.Instance.Id, SubmissionWorkflowInstance = access.Instance, Question = request.Question.Trim(), RaisedByUserId = user.Id, ResponseDueAt = request.ResponseDueAt };
        var evidenceError = await LinkEvidenceAsync(entity, kind, submissionId, RfiEvidencePurpose.Question, request.EvidencePublicIds, user.Id);
        if (evidenceError != null) return BadRequest(Fail<PerformanceRfiDto>(evidenceError));
        context.PerformanceRfis.Add(entity);
        AddRfiAction(access.Instance, permissionCode, WorkflowActionOutcome.RaiseRfi, user.Id, entity.Question);
        context.AuditTrails.Add(NewRfiAudit(entity, permissionCode, user, new { entity.Question, entity.ResponseDueAt }));
        governance.QueueWorkflowNotifications(access.Recipients, NotificationType.Rfi, "Performance RFI raised", $"An RFI was raised for {kind} submission '{submissionId}'.", kind + "Submission", submissionId);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<PerformanceRfiDto>(true, (await ToRfiDtosAsync([entity], members)).Single()));
    }

    [HttpPost("rfis/{publicId:guid}/respond")]
    public async Task<ActionResult<ApiResponse<PerformanceRfiDto>>> RespondRfi(Guid publicId, RespondPerformanceRfiRequest request)
    {
        if (!HasTenant()) return TenantRequired<PerformanceRfiDto>();
        if (string.IsNullOrWhiteSpace(request.Response)) return BadRequest(Fail<PerformanceRfiDto>("Response is required."));
        var rfi = await context.PerformanceRfis.Include(x => x.SubmissionWorkflowInstance).Include(x => x.EvidenceLinks).ThenInclude(x => x.PoeFile).ThenInclude(x => x.Blob).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (rfi == null) return NotFound(Fail<PerformanceRfiDto>("RFI not found."));
        if (rfi.RespondedAt.HasValue || rfi.ClosedAt.HasValue) return Conflict(Fail<PerformanceRfiDto>("RFI has already been responded to or closed."));
        var access = await LoadSubmissionAccess(rfi.SubmissionWorkflowInstance.SubmissionKind, rfi.SubmissionWorkflowInstance.SubmissionId);
        if (access == null) return NotFound(Fail<PerformanceRfiDto>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceRfiDto>("User not found."));
        var permissionCode = rfi.SubmissionWorkflowInstance.SubmissionKind == SubmissionKind.Opms ? "OPMS_RFI.RESPOND" : "IPMS_RFI.RESPOND";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        var members = await ReadRfiMembersAsync(user, rfi.SubmissionWorkflowInstance.SubmissionKind, access.Scope);
        if (!members.ResponseUpdate) return Forbid();
        if (!TrySetRowVersion(rfi, request.RowVersion)) return BadRequest(Fail<PerformanceRfiDto>("A valid RowVersion is required."));
        var evidenceError = await LinkEvidenceAsync(rfi, rfi.SubmissionWorkflowInstance.SubmissionKind, rfi.SubmissionWorkflowInstance.SubmissionId, RfiEvidencePurpose.Response, request.EvidencePublicIds, user.Id);
        if (evidenceError != null) return BadRequest(Fail<PerformanceRfiDto>(evidenceError));
        rfi.Response = request.Response.Trim(); rfi.RespondedByUserId = user.Id; rfi.RespondedAt = DateTime.UtcNow;
        AddRfiAction(rfi.SubmissionWorkflowInstance, permissionCode, WorkflowActionOutcome.RespondRfi, user.Id, rfi.Response);
        context.AuditTrails.Add(NewRfiAudit(rfi, permissionCode, user, new { rfi.Response }));
        governance.QueueWorkflowNotifications(access.Recipients, NotificationType.Rfi, "Performance RFI response", $"An RFI response was submitted for '{rfi.SubmissionWorkflowInstance.SubmissionId}'.", rfi.SubmissionWorkflowInstance.SubmissionKind + "Submission", rfi.SubmissionWorkflowInstance.SubmissionId);
        return await SaveRfi(rfi, members);
    }

    [HttpPost("rfis/{publicId:guid}/close")]
    public async Task<ActionResult<ApiResponse<PerformanceRfiDto>>> CloseRfi(Guid publicId, ClosePerformanceRfiRequest request)
    {
        if (!HasTenant()) return TenantRequired<PerformanceRfiDto>();
        var rfi = await context.PerformanceRfis.Include(x => x.SubmissionWorkflowInstance).Include(x => x.EvidenceLinks).ThenInclude(x => x.PoeFile).ThenInclude(x => x.Blob).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (rfi == null) return NotFound(Fail<PerformanceRfiDto>("RFI not found."));
        if (!rfi.RespondedAt.HasValue) return Conflict(Fail<PerformanceRfiDto>("RFI must be responded to before closure."));
        if (rfi.ClosedAt.HasValue) return Conflict(Fail<PerformanceRfiDto>("RFI is already closed."));
        var access = await LoadSubmissionAccess(rfi.SubmissionWorkflowInstance.SubmissionKind, rfi.SubmissionWorkflowInstance.SubmissionId);
        if (access == null) return NotFound(Fail<PerformanceRfiDto>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceRfiDto>("User not found."));
        var permissionCode = rfi.SubmissionWorkflowInstance.SubmissionKind == SubmissionKind.Opms ? "OPMS_RFI.CLOSE" : "IPMS_RFI.CLOSE";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, access.Scope)).Allowed) return Forbid();
        var members = await ReadRfiMembersAsync(user, rfi.SubmissionWorkflowInstance.SubmissionKind, access.Scope);
        if (!TrySetRowVersion(rfi, request.RowVersion)) return BadRequest(Fail<PerformanceRfiDto>("A valid RowVersion is required."));
        rfi.ClosedByUserId = user.Id; rfi.ClosedAt = DateTime.UtcNow;
        AddRfiAction(rfi.SubmissionWorkflowInstance, permissionCode, WorkflowActionOutcome.Complete, user.Id, request.Comment?.Trim());
        context.AuditTrails.Add(NewRfiAudit(rfi, permissionCode, user, new { request.Comment }));
        governance.QueueWorkflowNotifications(access.Recipients, NotificationType.Rfi, "Performance RFI closed", $"An RFI was closed for '{rfi.SubmissionWorkflowInstance.SubmissionId}'.", rfi.SubmissionWorkflowInstance.SubmissionKind + "Submission", rfi.SubmissionWorkflowInstance.SubmissionId);
        return await SaveRfi(rfi, members);
    }

    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private async Task<SubmissionAccess?> LoadSubmissionAccess(SubmissionKind kind, string submissionId)
    {
        if (kind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return null;
        int? department; int? unit; string? owner; string? submitter;
        if (kind == SubmissionKind.Opms)
        {
            var row = await context.OpmsSubmissions.AsNoTracking().Include(x => x.OpmsTarget).SingleOrDefaultAsync(x => x.Id == submissionId);
            if (row == null) return null;
            department = row.OpmsTarget.DepartmentId; unit = row.OpmsTarget.UnitId; owner = row.OpmsTarget.AssignedUserId; submitter = row.SubmittedByUserId ?? row.CreatedBy;
        }
        else
        {
            var row = await context.IpmsSubmissions.AsNoTracking().Include(x => x.IpmsTarget).SingleOrDefaultAsync(x => x.Id == submissionId);
            if (row == null) return null;
            department = row.IpmsTarget.DepartmentId; unit = row.IpmsTarget.UnitId; owner = row.IpmsTarget.AssignedUserId; submitter = row.SubmittedByUserId ?? row.CreatedBy;
        }
        var instance = await context.SubmissionWorkflowInstances.Include(x => x.CurrentStage).SingleOrDefaultAsync(x => x.SubmissionKind == kind && x.SubmissionId == submissionId);
        return new SubmissionAccess(new AccessScopeContext(department, unit, owner, TargetId: submissionId, MunicipalityId: tenantContext.MunicipalityId), instance, new[] { owner, submitter }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
    private void AddRfiAction(SubmissionWorkflowInstance instance, string actionCode, WorkflowActionOutcome outcome, string actorId, string? comment)
    {
        context.SubmissionWorkflowActions.Add(new SubmissionWorkflowAction { MunicipalityId = tenantContext.MunicipalityId!.Value, SubmissionWorkflowInstanceId = instance.Id, SubmissionWorkflowInstance = instance, Sequence = instance.NextSequence++, FromStageId = instance.CurrentStageId, ToStageId = instance.CurrentStageId, ActionCode = actionCode, Outcome = outcome, ActorUserId = actorId, Comment = comment, CorrelationId = HttpContext.TraceIdentifier });
    }
    private AuditTrail NewRfiAudit(PerformanceRfi rfi, string action, ApplicationUser user, object value) => new() { EntityName = nameof(PerformanceRfi), EntityId = rfi.PublicId.ToString(), Action = action, NewValue = JsonSerializer.Serialize(value), ChangedBy = user.Id, CorrelationId = HttpContext.TraceIdentifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() };
    private bool TrySetRowVersion(PerformanceRfi rfi, string rowVersion) { try { context.Entry(rfi).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion); return true; } catch (FormatException) { return false; } }
    private async Task<ActionResult<ApiResponse<PerformanceRfiDto>>> SaveRfi(PerformanceRfi rfi, RfiMemberAccess members) { try { await context.SaveChangesAsync(); return Ok(new ApiResponse<PerformanceRfiDto>(true, (await ToRfiDtosAsync([rfi], members)).Single())); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<PerformanceRfiDto>("RFI was changed by another user.")); } }
    private async Task<string?> LinkEvidenceAsync(PerformanceRfi rfi, SubmissionKind kind, string submissionId, RfiEvidencePurpose purpose, IReadOnlyCollection<Guid>? publicIds, string userId)
    {
        var ids = (publicIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0) return null;
        var files = await context.PoeFiles.Include(x => x.Blob).Where(x => ids.Contains(x.PublicId)).ToArrayAsync();
        var policy = RfiEvidencePolicy.Validate(kind, submissionId, ids, files);
        if (!policy.Allowed) return policy.Error;
        var existing = rfi.EvidenceLinks.Select(x => new { x.PoeFileId, x.Purpose }).ToHashSet();
        foreach (var file in files.Where(file => !existing.Contains(new { PoeFileId = file.Id, Purpose = purpose })))
            rfi.EvidenceLinks.Add(new PerformanceRfiEvidence { MunicipalityId = tenantContext.MunicipalityId!.Value, PoeFileId = file.Id, PoeFile = file, Purpose = purpose, LinkedByUserId = userId, LinkedAt = DateTime.UtcNow, CorrelationId = HttpContext.TraceIdentifier });
        return null;
    }
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before using workflow."));
    private static WorkflowDefinitionDto ToDto(WorkflowDefinition x) => new(x.PublicId, x.MunicipalityFinancialYear.PublicId, x.SubmissionKind, x.Code, x.Name, x.Version, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion), x.Stages.OrderBy(s => s.Sequence).Select(s => new WorkflowStageDto(s.PublicId, s.Code, s.Name, s.Sequence, s.RequiredActionCode, s.RequiredPermissionCode, s.IsOptional, s.AllowBypass, s.RequireDifferentActorFromSubmitter, s.RequireDifferentActorFromPreviousStage, s.IsTerminal, s.RejectionStageCode, s.RequiresRating, s.RatingScheme?.PublicId, s.RatingScheme?.Code)).ToArray());
    private static ReportingWindowDto ToDto(ReportingWindow x) => new(x.PublicId, x.ReportingPeriod.PublicId, x.ReportingPeriod.Code, x.SubmissionKind, x.OpensAt, x.ClosesAt, x.IsActive, Convert.ToBase64String(x.RowVersion));
    private async Task<WindowExceptionMemberAccess> ReadWindowExceptionMembersAsync(ApplicationUser user, SubmissionKind kind)
    {
        var resource = kind == SubmissionKind.Opms ? "OPMS_WORKFLOW" : "IPMS_WORKFLOW";
        var scope = new AccessScopeContext(MunicipalityId: tenantContext.MunicipalityId);
        async Task<bool> Allowed(string member, string operation) =>
            (await accessControl.CheckPermissionAsync(user, $"{resource}.{member}.{operation}", scope)).Allowed;
        return new WindowExceptionMemberAccess(
            await Allowed("WindowExceptionScope", "READ"), await Allowed("WindowExceptionReason", "READ"),
            await Allowed("WindowExceptionApprovedBy", "READ"), await Allowed("WindowExceptionScope", "UPDATE"),
            await Allowed("WindowExceptionReason", "UPDATE"));
    }
    private async Task<ReportingWindowExceptionDto[]> ToWindowExceptionDtosAsync(
        IReadOnlyCollection<ReportingWindowException> rows,
        WindowExceptionMemberAccess members)
    {
        var userIds = members.ScopeRead ? rows.Where(x => x.UserId != null).Select(x => x.UserId!).Distinct().ToArray() : [];
        var departmentIds = members.ScopeRead ? rows.Where(x => x.DepartmentId.HasValue).Select(x => x.DepartmentId!.Value).Distinct().ToArray() : [];
        var unitIds = members.ScopeRead ? rows.Where(x => x.UnitId.HasValue).Select(x => x.UnitId!.Value).Distinct().ToArray() : [];
        var approverIds = members.ApprovedByRead ? rows.Select(x => x.ApprovedByUserId).Distinct().ToArray() : [];
        var users = userIds.Length == 0 ? [] : await context.Users.AsNoTracking().Where(x => userIds.Contains(x.Id)).Select(x => new { x.Id, x.PublicId, x.FirstName, x.LastName }).ToArrayAsync();
        var departments = departmentIds.Length == 0 ? [] : await context.Departments.AsNoTracking().Where(x => departmentIds.Contains(x.Id)).Select(x => new { x.Id, x.PublicId, x.Name }).ToArrayAsync();
        var units = unitIds.Length == 0 ? [] : await context.Units.AsNoTracking().Where(x => unitIds.Contains(x.Id)).Select(x => new { x.Id, x.PublicId, x.Name }).ToArrayAsync();
        var approvers = approverIds.Length == 0 ? [] : await context.Users.AsNoTracking().Where(x => approverIds.Contains(x.Id)).Select(x => new { x.Id, x.PublicId, x.FirstName, x.LastName }).ToArrayAsync();
        return rows.Select(x =>
        {
            var scopeType = x.UserId != null ? "User" : x.DepartmentId.HasValue ? "Department" : "Unit";
            Guid? scopePublicId = null; string? scopeName = null;
            if (members.ScopeRead && x.UserId != null)
            {
                var user = users.SingleOrDefault(item => item.Id == x.UserId);
                scopePublicId = user?.PublicId; scopeName = user == null ? null : $"{user.FirstName} {user.LastName}".Trim();
            }
            else if (members.ScopeRead && x.DepartmentId.HasValue)
            {
                var department = departments.SingleOrDefault(item => item.Id == x.DepartmentId.Value);
                scopePublicId = department?.PublicId; scopeName = department?.Name;
            }
            else if (members.ScopeRead && x.UnitId.HasValue)
            {
                var unit = units.SingleOrDefault(item => item.Id == x.UnitId.Value);
                scopePublicId = unit?.PublicId; scopeName = unit?.Name;
            }
            var approver = members.ApprovedByRead ? approvers.SingleOrDefault(item => item.Id == x.ApprovedByUserId) : null;
            return new ReportingWindowExceptionDto(
                x.PublicId, scopeType, scopePublicId, scopeName, x.ExtendedClosesAt,
                members.ReasonRead ? x.Reason : null,
                approver?.PublicId, approver == null ? null : $"{approver.FirstName} {approver.LastName}".Trim(),
                x.ApprovedAt, Convert.ToBase64String(x.RowVersion));
        }).ToArray();
    }
    private static RatingSchemeDto ToDto(RatingScheme x) => new(x.PublicId, x.Code, x.Name, x.IsActive, Convert.ToBase64String(x.RowVersion), x.Values.OrderBy(v => v.SortOrder).Select(v => new RatingValueDto(v.PublicId, v.Value, v.Label, v.MinimumAchievementPercent, v.MaximumAchievementPercent, v.SortOrder)).ToArray());
    private async Task<WorkflowMemberAccess> ReadWorkflowMembersAsync(ApplicationUser user, SubmissionKind kind, AccessScopeContext scope)
    {
        var resource = kind == SubmissionKind.Opms ? "OPMS_WORKFLOW" : "IPMS_WORKFLOW";
        async Task<bool> Read(string member) => (await accessControl.CheckPermissionAsync(user, $"{resource}.{member}.READ", scope)).Allowed;
        return new WorkflowMemberAccess(
            await Read("ActionActorUserId"), await Read("ActionComment"), await Read("ActionRatingValue"),
            await Read("StageRatingValue"), await Read("StageRatingAchievementPercent"), await Read("StageRatingComment"),
            await Read("StageRatingRatedByUserId"), await Read("StageRatingRatedByName"));
    }
    private static WorkflowActionDto ToDto(SubmissionWorkflowAction x, WorkflowMemberAccess members, ApplicationUser actor) => new(
        x.PublicId, x.Sequence, x.ActionCode, x.Outcome,
        members.ActionActorUserId ? actor.PublicId : null,
        members.ActionActorUserId ? actor.FullName : null,
        members.ActionComment ? x.Comment : null,
        members.ActionRatingValue ? x.RatingValue : null,
        x.OccurredAt);
    private static StageRatingDto ToDto(SubmissionStageRating x, WorkflowMemberAccess members) => new(
        x.PublicId, x.SubmissionWorkflowAction.PublicId, x.WorkflowStageDefinition.Code, x.RatingScheme.PublicId, x.RatingScheme.Code,
        members.StageRatingValue ? x.RatingSchemeValue.PublicId : null,
        members.StageRatingValue ? x.Value : null,
        members.StageRatingValue ? x.LabelSnapshot : null,
        members.StageRatingAchievementPercent ? x.AchievementPercent : null,
        members.StageRatingComment ? x.Comment : null,
        members.StageRatingRatedByUserId ? x.RatedByUser.PublicId : null,
        members.StageRatingRatedByName ? x.RatedByUser.FullName : null,
        x.RatedAt);
    private async Task<RfiMemberAccess> ReadRfiMembersAsync(ApplicationUser user, SubmissionKind kind, AccessScopeContext scope)
    {
        var resource = kind == SubmissionKind.Opms ? "OPMS_RFI" : "IPMS_RFI";
        async Task<bool> Allowed(string member, string operation) =>
            (await accessControl.CheckPermissionAsync(user, $"{resource}.{member}.{operation}", scope)).Allowed;
        return new RfiMemberAccess(
            await Allowed("Question", "READ"), await Allowed("Question", "UPDATE"), await Allowed("RaisedBy", "READ"),
            await Allowed("Response", "READ"), await Allowed("Response", "UPDATE"), await Allowed("RespondedBy", "READ"),
            await Allowed("ClosedBy", "READ"), await Allowed("EvidenceMetadata", "READ"), await Allowed("EvidenceLinkedBy", "READ"));
    }
    private async Task<PerformanceRfiDto[]> ToRfiDtosAsync(IReadOnlyCollection<PerformanceRfi> rows, RfiMemberAccess members)
    {
        var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (members.RaisedByRead) foreach (var id in rows.Select(x => x.RaisedByUserId)) userIds.Add(id);
        if (members.RespondedByRead) foreach (var id in rows.Select(x => x.RespondedByUserId).Where(x => x != null)) userIds.Add(id!);
        if (members.ClosedByRead) foreach (var id in rows.Select(x => x.ClosedByUserId).Where(x => x != null)) userIds.Add(id!);
        if (members.EvidenceLinkedByRead) foreach (var id in rows.SelectMany(x => x.EvidenceLinks).Select(x => x.LinkedByUserId)) userIds.Add(id);
        var users = userIds.Count == 0 ? [] : await context.Users.AsNoTracking().Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.PublicId, x.FirstName, x.LastName }).ToArrayAsync();
        (Guid? PublicId, string? Name) Identity(string? id, bool readable)
        {
            if (!readable || string.IsNullOrWhiteSpace(id)) return (null, null);
            var identity = users.SingleOrDefault(x => x.Id == id);
            return identity == null ? (null, null) : (identity.PublicId, $"{identity.FirstName} {identity.LastName}".Trim());
        }
        return rows.Select(x =>
        {
            var raisedBy = Identity(x.RaisedByUserId, members.RaisedByRead);
            var respondedBy = Identity(x.RespondedByUserId, members.RespondedByRead);
            var closedBy = Identity(x.ClosedByUserId, members.ClosedByRead);
            return new PerformanceRfiDto(
                x.PublicId, members.QuestionRead ? x.Question : null, raisedBy.PublicId, raisedBy.Name,
                x.RaisedAt, x.ResponseDueAt, members.ResponseRead ? x.Response : null,
                respondedBy.PublicId, respondedBy.Name, x.RespondedAt, closedBy.PublicId, closedBy.Name, x.ClosedAt,
                Convert.ToBase64String(x.RowVersion),
                x.EvidenceLinks.OrderBy(link => link.LinkedAt).Select(link =>
                {
                    var linkedBy = Identity(link.LinkedByUserId, members.EvidenceLinkedByRead);
                    return new RfiEvidenceDto(
                        link.PublicId, link.PoeFile.PublicId, link.Purpose,
                        members.EvidenceMetadataRead ? link.PoeFile.FileName : null,
                        members.EvidenceMetadataRead ? link.PoeFile.Blob.ContentType : null,
                        members.EvidenceMetadataRead ? link.PoeFile.Blob.SizeInBytes : null,
                        members.EvidenceMetadataRead ? link.PoeFile.Blob.Sha256 : null,
                        linkedBy.PublicId, linkedBy.Name, link.LinkedAt,
                        members.EvidenceMetadataRead ? link.PoeFile.ToResponse(HttpContext).Url : null);
                }).ToArray());
        }).ToArray();
    }
    private sealed record SubmissionAccess(AccessScopeContext Scope, SubmissionWorkflowInstance? Instance, string[] Recipients);
    private sealed record WorkflowMemberAccess(
        bool ActionActorUserId, bool ActionComment, bool ActionRatingValue,
        bool StageRatingValue, bool StageRatingAchievementPercent, bool StageRatingComment,
        bool StageRatingRatedByUserId, bool StageRatingRatedByName);
    private sealed record WindowExceptionMemberAccess(
        bool ScopeRead, bool ReasonRead, bool ApprovedByRead, bool ScopeUpdate, bool ReasonUpdate);
    private sealed record RfiMemberAccess(
        bool QuestionRead, bool QuestionUpdate, bool RaisedByRead,
        bool ResponseRead, bool ResponseUpdate, bool RespondedByRead, bool ClosedByRead,
        bool EvidenceMetadataRead, bool EvidenceLinkedByRead);
}

public sealed record SaveWorkflowStageRequest(string Code, string Name, int Sequence, string RequiredActionCode, string RequiredPermissionCode, bool IsOptional, bool AllowBypass, bool RequireDifferentActorFromSubmitter, bool RequireDifferentActorFromPreviousStage, bool IsTerminal, string? RejectionStageCode, bool RequiresRating, Guid? RatingSchemePublicId);
public sealed record SaveWorkflowDefinitionRequest(Guid MunicipalityFinancialYearPublicId, SubmissionKind SubmissionKind, string Code, string Name, bool IsActive, DateTime EffectiveFrom, IReadOnlyList<SaveWorkflowStageRequest> Stages, string? Reason = null);
public sealed record WorkflowStageDto(Guid PublicId, string Code, string Name, int Sequence, string RequiredActionCode, string RequiredPermissionCode, bool IsOptional, bool AllowBypass, bool RequireDifferentActorFromSubmitter, bool RequireDifferentActorFromPreviousStage, bool IsTerminal, string? RejectionStageCode, bool RequiresRating, Guid? RatingSchemePublicId, string? RatingSchemeCode);
public sealed record WorkflowDefinitionDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, SubmissionKind SubmissionKind, string Code, string Name, int Version, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion, WorkflowStageDto[] Stages);
public sealed record RetireWorkflowDefinitionRequest(string Reason, DateTime? EffectiveTo, string RowVersion);
public sealed record WorkflowStageDifferenceDto(string Change, string StageCode, int? FromSequence, int? ToSequence, string[] ChangedFields);
public sealed record WorkflowDefinitionComparisonDto(WorkflowDefinitionDto From, WorkflowDefinitionDto To, WorkflowStageDifferenceDto[] StageDifferences);
public sealed record SaveReportingWindowRequest(Guid ReportingPeriodPublicId, SubmissionKind SubmissionKind, DateTime OpensAt, DateTime ClosesAt, string Reason);
public sealed record ReportingWindowDto(Guid PublicId, Guid ReportingPeriodPublicId, string PeriodCode, SubmissionKind SubmissionKind, DateTime OpensAt, DateTime ClosesAt, bool IsActive, string RowVersion);
public sealed record SaveReportingWindowExceptionRequest(Guid? UserPublicId, Guid? DepartmentPublicId, Guid? UnitPublicId, DateTime ExtendedClosesAt, string Reason);
public sealed record ReportingWindowExceptionDto(Guid PublicId, string ScopeType, Guid? ScopePublicId, string? ScopeName, DateTime ExtendedClosesAt, string? Reason, Guid? ApprovedByUserPublicId, string? ApprovedByName, DateTime ApprovedAt, string RowVersion);
public sealed record SaveRatingValueRequest(decimal Value, string Label, decimal? MinimumAchievementPercent, decimal? MaximumAchievementPercent, int SortOrder);
public sealed record SaveRatingSchemeRequest(string Code, string Name, IReadOnlyList<SaveRatingValueRequest> Values, string Reason);
public sealed record RatingValueDto(Guid PublicId, decimal Value, string Label, decimal? MinimumAchievementPercent, decimal? MaximumAchievementPercent, int SortOrder);
public sealed record RatingSchemeDto(Guid PublicId, string Code, string Name, bool IsActive, string RowVersion, RatingValueDto[] Values);
public sealed record WorkflowActionRequest(string ActionCode, WorkflowActionOutcome Outcome, string? Comment, decimal? RatingValue);
public sealed record WorkflowActionDto(Guid PublicId, int Sequence, string ActionCode, WorkflowActionOutcome Outcome, Guid? ActorUserPublicId, string? ActorName, string? Comment, decimal? RatingValue, DateTime OccurredAt);
public sealed record StageRatingDto(Guid PublicId, Guid WorkflowActionPublicId, string StageCode, Guid RatingSchemePublicId, string RatingSchemeCode, Guid? RatingValuePublicId, decimal? Value, string? Label, decimal? AchievementPercent, string? Comment, Guid? RatedByUserPublicId, string? RatedByName, DateTime RatedAt);
public sealed record RaisePerformanceRfiRequest(string Question, DateTime ResponseDueAt, IReadOnlyCollection<Guid>? EvidencePublicIds = null);
public sealed record RespondPerformanceRfiRequest(string Response, string RowVersion, IReadOnlyCollection<Guid>? EvidencePublicIds = null);
public sealed record ClosePerformanceRfiRequest(string? Comment, string RowVersion);
public sealed record RfiEvidenceDto(Guid PublicId, Guid EvidencePublicId, RfiEvidencePurpose Purpose, string? FileName, string? ContentType, long? SizeInBytes, string? Sha256, Guid? LinkedByUserPublicId, string? LinkedByName, DateTime LinkedAt, string? Url);
public sealed record PerformanceRfiDto(Guid PublicId, string? Question, Guid? RaisedByUserPublicId, string? RaisedByName, DateTime RaisedAt, DateTime ResponseDueAt, string? Response, Guid? RespondedByUserPublicId, string? RespondedByName, DateTime? RespondedAt, Guid? ClosedByUserPublicId, string? ClosedByName, DateTime? ClosedAt, string RowVersion, RfiEvidenceDto[] Evidence);

using System.Security.Claims;
using System.Text.Json;
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
    [HttpGet("definitions")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto[]>>> GetDefinitions()
    {
        if (!HasTenant()) return TenantRequired<WorkflowDefinitionDto[]>();
        var entities = await context.WorkflowDefinitions.AsNoTracking().Include(x => x.MunicipalityFinancialYear).Include(x => x.Stages).ThenInclude(x => x.RatingScheme).OrderByDescending(x => x.Version).ToArrayAsync();
        return Ok(new ApiResponse<WorkflowDefinitionDto[]>(true, entities.Select(ToDto).ToArray()));
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

    [HttpGet("reporting-windows")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<ReportingWindowDto[]>>> GetWindows()
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowDto[]>();
        var entities = await context.ReportingWindows.AsNoTracking().Include(x => x.ReportingPeriod).OrderByDescending(x => x.OpensAt).ToArrayAsync();
        return Ok(new ApiResponse<ReportingWindowDto[]>(true, entities.Select(ToDto).ToArray()));
    }

    [HttpPost("reporting-windows")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<ReportingWindowDto>>> CreateWindow(SaveReportingWindowRequest request)
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowDto>();
        if (request.ClosesAt <= request.OpensAt) return BadRequest(Fail<ReportingWindowDto>("Window close must be after open."));
        var period = await context.ReportingPeriods.SingleOrDefaultAsync(x => x.PublicId == request.ReportingPeriodPublicId);
        if (period == null) return BadRequest(Fail<ReportingWindowDto>("Reporting period not found."));
        if (await context.ReportingWindows.AnyAsync(x => x.ReportingPeriodId == period.Id && x.SubmissionKind == request.SubmissionKind)) return Conflict(Fail<ReportingWindowDto>("A reporting window already exists for this period and submission type."));
        var entity = new ReportingWindow { MunicipalityId = tenantContext.MunicipalityId!.Value, ReportingPeriodId = period.Id, ReportingPeriod = period, SubmissionKind = request.SubmissionKind, OpensAt = request.OpensAt, ClosesAt = request.ClosesAt };
        context.ReportingWindows.Add(entity); await context.SaveChangesAsync();
        return Ok(new ApiResponse<ReportingWindowDto>(true, ToDto(entity)));
    }

    [HttpGet("reporting-windows/{windowPublicId:guid}/exceptions")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<ReportingWindowExceptionDto[]>>> GetWindowExceptions(Guid windowPublicId)
    {
        if (!HasTenant()) return TenantRequired<ReportingWindowExceptionDto[]>();
        var window = await context.ReportingWindows.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == windowPublicId);
        if (window == null) return NotFound(Fail<ReportingWindowExceptionDto[]>("Reporting window not found."));
        var rows = await context.ReportingWindowExceptions.AsNoTracking().Where(x => x.ReportingWindowId == window.Id).OrderByDescending(x => x.ApprovedAt).ToArrayAsync();
        return Ok(new ApiResponse<ReportingWindowExceptionDto[]>(true, rows.Select(ToDto).ToArray()));
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
        return Ok(new ApiResponse<ReportingWindowExceptionDto>(true, ToDto(entity)));
    }

    [HttpGet("rating-schemes")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<RatingSchemeDto[]>>> GetRatingSchemes()
    {
        if (!HasTenant()) return TenantRequired<RatingSchemeDto[]>();
        var rows = await context.RatingSchemes.AsNoTracking().Include(x => x.Values).OrderBy(x => x.Code).ToArrayAsync();
        return Ok(new ApiResponse<RatingSchemeDto[]>(true, rows.Select(ToDto).ToArray()));
    }

    [HttpPost("rating-schemes")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<RatingSchemeDto>>> CreateRatingScheme(SaveRatingSchemeRequest request)
    {
        if (!HasTenant()) return TenantRequired<RatingSchemeDto>();
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || request.Values.Count == 0) return BadRequest(Fail<RatingSchemeDto>("Code, name, and rating values are required."));
        if (request.Values.Select(x => x.Value).Distinct().Count() != request.Values.Count || request.Values.Select(x => x.SortOrder).Distinct().Count() != request.Values.Count) return BadRequest(Fail<RatingSchemeDto>("Rating values and sort orders must be unique."));
        if (request.Values.Any(x => x.MinimumAchievementPercent.HasValue != x.MaximumAchievementPercent.HasValue || x.MinimumAchievementPercent > x.MaximumAchievementPercent)) return BadRequest(Fail<RatingSchemeDto>("Rating achievement ranges must have valid minimum and maximum values."));
        var ranges = request.Values.Where(x => x.MinimumAchievementPercent.HasValue).OrderBy(x => x.MinimumAchievementPercent).ToArray();
        if (ranges.Zip(ranges.Skip(1), (left, right) => left.MaximumAchievementPercent >= right.MinimumAchievementPercent).Any(overlap => overlap)) return BadRequest(Fail<RatingSchemeDto>("Rating achievement ranges must not overlap."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.RatingSchemes.AnyAsync(x => x.Code == code)) return Conflict(Fail<RatingSchemeDto>("Rating scheme code already exists."));
        var entity = new RatingScheme { MunicipalityId = tenantContext.MunicipalityId!.Value, Code = code, Name = request.Name.Trim(), IsActive = true };
        foreach (var value in request.Values.OrderBy(x => x.SortOrder)) entity.Values.Add(new RatingSchemeValue { MunicipalityId = tenantContext.MunicipalityId.Value, Value = value.Value, Label = value.Label.Trim(), MinimumAchievementPercent = value.MinimumAchievementPercent, MaximumAchievementPercent = value.MaximumAchievementPercent, SortOrder = value.SortOrder });
        context.RatingSchemes.Add(entity); await context.SaveChangesAsync();
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
        var permission = await accessControl.CheckPermissionAsync(user, request.ActionCode, new AccessScopeContext(department, unit, owner, TargetId: submissionId, MunicipalityId: tenantContext.MunicipalityId));
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
        return Ok(new ApiResponse<WorkflowActionDto>(true, ToDto(transition.Action), transition.Reason));
    }

    [HttpGet("submissions/{kind}/{submissionId}/actions")]
    public async Task<ActionResult<ApiResponse<WorkflowActionDto[]>>> History(SubmissionKind kind, string submissionId)
    {
        if (!HasTenant()) return TenantRequired<WorkflowActionDto[]>();
        if (kind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return BadRequest(Fail<WorkflowActionDto[]>("Unsupported submission type."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<WorkflowActionDto[]>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        int? department; int? unit; string? owner;
        if (kind == SubmissionKind.Opms)
        {
            var submission = await context.OpmsSubmissions.AsNoTracking().Include(x => x.OpmsTarget).SingleOrDefaultAsync(x => x.Id == submissionId);
            if (submission == null) return NotFound(Fail<WorkflowActionDto[]>("OPMS submission not found."));
            department = submission.OpmsTarget.DepartmentId; unit = submission.OpmsTarget.UnitId; owner = submission.OpmsTarget.AssignedUserId;
        }
        else
        {
            var submission = await context.IpmsSubmissions.AsNoTracking().Include(x => x.IpmsTarget).SingleOrDefaultAsync(x => x.Id == submissionId);
            if (submission == null) return NotFound(Fail<WorkflowActionDto[]>("IPMS submission not found."));
            department = submission.IpmsTarget.DepartmentId; unit = submission.IpmsTarget.UnitId; owner = submission.IpmsTarget.AssignedUserId;
        }
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, new AccessScopeContext(department, unit, owner, TargetId: submissionId, MunicipalityId: tenantContext.MunicipalityId))).Allowed) return Forbid();
        var rows = await context.SubmissionWorkflowActions.AsNoTracking().Where(x => x.SubmissionWorkflowInstance.SubmissionKind == kind && x.SubmissionWorkflowInstance.SubmissionId == submissionId).OrderBy(x => x.Sequence).ToArrayAsync();
        return Ok(new ApiResponse<WorkflowActionDto[]>(true, rows.Select(ToDto).ToArray()));
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
        var rows = await context.SubmissionStageRatings.AsNoTracking()
            .Include(x => x.SubmissionWorkflowAction)
            .Include(x => x.WorkflowStageDefinition)
            .Include(x => x.RatingScheme)
            .Include(x => x.RatingSchemeValue)
            .Include(x => x.RatedByUser)
            .Where(x => x.SubmissionWorkflowInstance.SubmissionKind == kind && x.SubmissionWorkflowInstance.SubmissionId == submissionId)
            .OrderBy(x => x.RatedAt)
            .ToArrayAsync();
        return Ok(new ApiResponse<StageRatingDto[]>(true, rows.Select(ToDto).ToArray()));
    }

    [HttpGet("submissions/{kind}/{submissionId}/rfis")]
    public async Task<ActionResult<ApiResponse<PerformanceRfiDto[]>>> GetRfis(SubmissionKind kind, string submissionId)
    {
        if (!HasTenant()) return TenantRequired<PerformanceRfiDto[]>();
        var access = await LoadSubmissionAccess(kind, submissionId);
        if (access == null) return NotFound(Fail<PerformanceRfiDto[]>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceRfiDto[]>("User not found."));
        var readCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, readCode, access.Scope)).Allowed) return Forbid();
        if (access.Instance == null) return Ok(new ApiResponse<PerformanceRfiDto[]>(true, []));
        var rows = await context.PerformanceRfis.AsNoTracking()
            .Include(x => x.EvidenceLinks).ThenInclude(x => x.PoeFile).ThenInclude(x => x.Blob)
            .Where(x => x.SubmissionWorkflowInstanceId == access.Instance.Id)
            .OrderByDescending(x => x.RaisedAt).ToArrayAsync();
        return Ok(new ApiResponse<PerformanceRfiDto[]>(true, rows.Select(ToRfiDto).ToArray()));
    }

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
        var entity = new PerformanceRfi { MunicipalityId = tenantContext.MunicipalityId!.Value, SubmissionWorkflowInstanceId = access.Instance.Id, SubmissionWorkflowInstance = access.Instance, Question = request.Question.Trim(), RaisedByUserId = user.Id, ResponseDueAt = request.ResponseDueAt };
        var evidenceError = await LinkEvidenceAsync(entity, kind, submissionId, RfiEvidencePurpose.Question, request.EvidencePublicIds, user.Id);
        if (evidenceError != null) return BadRequest(Fail<PerformanceRfiDto>(evidenceError));
        context.PerformanceRfis.Add(entity);
        AddRfiAction(access.Instance, permissionCode, WorkflowActionOutcome.RaiseRfi, user.Id, entity.Question);
        context.AuditTrails.Add(NewRfiAudit(entity, permissionCode, user, new { entity.Question, entity.ResponseDueAt }));
        governance.QueueWorkflowNotifications(access.Recipients, NotificationType.Rfi, "Performance RFI raised", $"An RFI was raised for {kind} submission '{submissionId}'.", kind + "Submission", submissionId);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<PerformanceRfiDto>(true, ToRfiDto(entity)));
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
        if (!TrySetRowVersion(rfi, request.RowVersion)) return BadRequest(Fail<PerformanceRfiDto>("A valid RowVersion is required."));
        var evidenceError = await LinkEvidenceAsync(rfi, rfi.SubmissionWorkflowInstance.SubmissionKind, rfi.SubmissionWorkflowInstance.SubmissionId, RfiEvidencePurpose.Response, request.EvidencePublicIds, user.Id);
        if (evidenceError != null) return BadRequest(Fail<PerformanceRfiDto>(evidenceError));
        rfi.Response = request.Response.Trim(); rfi.RespondedByUserId = user.Id; rfi.RespondedAt = DateTime.UtcNow;
        AddRfiAction(rfi.SubmissionWorkflowInstance, permissionCode, WorkflowActionOutcome.RespondRfi, user.Id, rfi.Response);
        context.AuditTrails.Add(NewRfiAudit(rfi, permissionCode, user, new { rfi.Response }));
        governance.QueueWorkflowNotifications(access.Recipients, NotificationType.Rfi, "Performance RFI response", $"An RFI response was submitted for '{rfi.SubmissionWorkflowInstance.SubmissionId}'.", rfi.SubmissionWorkflowInstance.SubmissionKind + "Submission", rfi.SubmissionWorkflowInstance.SubmissionId);
        return await SaveRfi(rfi);
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
        if (!TrySetRowVersion(rfi, request.RowVersion)) return BadRequest(Fail<PerformanceRfiDto>("A valid RowVersion is required."));
        rfi.ClosedByUserId = user.Id; rfi.ClosedAt = DateTime.UtcNow;
        AddRfiAction(rfi.SubmissionWorkflowInstance, permissionCode, WorkflowActionOutcome.Complete, user.Id, request.Comment?.Trim());
        context.AuditTrails.Add(NewRfiAudit(rfi, permissionCode, user, new { request.Comment }));
        governance.QueueWorkflowNotifications(access.Recipients, NotificationType.Rfi, "Performance RFI closed", $"An RFI was closed for '{rfi.SubmissionWorkflowInstance.SubmissionId}'.", rfi.SubmissionWorkflowInstance.SubmissionKind + "Submission", rfi.SubmissionWorkflowInstance.SubmissionId);
        return await SaveRfi(rfi);
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
    private async Task<ActionResult<ApiResponse<PerformanceRfiDto>>> SaveRfi(PerformanceRfi rfi) { try { await context.SaveChangesAsync(); return Ok(new ApiResponse<PerformanceRfiDto>(true, ToRfiDto(rfi))); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<PerformanceRfiDto>("RFI was changed by another user.")); } }
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
    private static ReportingWindowExceptionDto ToDto(ReportingWindowException x) => new(x.PublicId, x.UserId, x.DepartmentId, x.UnitId, x.ExtendedClosesAt, x.Reason, x.ApprovedByUserId, x.ApprovedAt, Convert.ToBase64String(x.RowVersion));
    private static RatingSchemeDto ToDto(RatingScheme x) => new(x.PublicId, x.Code, x.Name, x.IsActive, Convert.ToBase64String(x.RowVersion), x.Values.OrderBy(v => v.SortOrder).Select(v => new RatingValueDto(v.PublicId, v.Value, v.Label, v.MinimumAchievementPercent, v.MaximumAchievementPercent, v.SortOrder)).ToArray());
    private static WorkflowActionDto ToDto(SubmissionWorkflowAction x) => new(x.PublicId, x.Sequence, x.ActionCode, x.Outcome, x.ActorUserId, x.Comment, x.RatingValue, x.OccurredAt);
    private static StageRatingDto ToDto(SubmissionStageRating x) => new(x.PublicId, x.SubmissionWorkflowAction.PublicId, x.WorkflowStageDefinition.Code, x.RatingScheme.PublicId, x.RatingScheme.Code, x.RatingSchemeValue.PublicId, x.Value, x.LabelSnapshot, x.AchievementPercent, x.Comment, x.RatedByUserId, x.RatedByUser.FullName, x.RatedAt);
    private PerformanceRfiDto ToRfiDto(PerformanceRfi x) => new(x.PublicId, x.Question, x.RaisedByUserId, x.RaisedAt, x.ResponseDueAt, x.Response, x.RespondedByUserId, x.RespondedAt, x.ClosedByUserId, x.ClosedAt, Convert.ToBase64String(x.RowVersion), x.EvidenceLinks.OrderBy(link => link.LinkedAt).Select(link => new RfiEvidenceDto(link.PublicId, link.PoeFile.PublicId, link.Purpose, link.PoeFile.FileName, link.PoeFile.Blob.ContentType, link.PoeFile.Blob.SizeInBytes, link.PoeFile.Blob.Sha256, link.LinkedByUserId, link.LinkedAt, link.PoeFile.ToResponse(HttpContext).Url)).ToArray());
    private sealed record SubmissionAccess(AccessScopeContext Scope, SubmissionWorkflowInstance? Instance, string[] Recipients);
}

public sealed record SaveWorkflowStageRequest(string Code, string Name, int Sequence, string RequiredActionCode, string RequiredPermissionCode, bool IsOptional, bool AllowBypass, bool RequireDifferentActorFromSubmitter, bool RequireDifferentActorFromPreviousStage, bool IsTerminal, string? RejectionStageCode, bool RequiresRating, Guid? RatingSchemePublicId);
public sealed record SaveWorkflowDefinitionRequest(Guid MunicipalityFinancialYearPublicId, SubmissionKind SubmissionKind, string Code, string Name, bool IsActive, DateTime EffectiveFrom, IReadOnlyList<SaveWorkflowStageRequest> Stages, string? Reason = null);
public sealed record WorkflowStageDto(Guid PublicId, string Code, string Name, int Sequence, string RequiredActionCode, string RequiredPermissionCode, bool IsOptional, bool AllowBypass, bool RequireDifferentActorFromSubmitter, bool RequireDifferentActorFromPreviousStage, bool IsTerminal, string? RejectionStageCode, bool RequiresRating, Guid? RatingSchemePublicId, string? RatingSchemeCode);
public sealed record WorkflowDefinitionDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, SubmissionKind SubmissionKind, string Code, string Name, int Version, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion, WorkflowStageDto[] Stages);
public sealed record RetireWorkflowDefinitionRequest(string Reason, DateTime? EffectiveTo, string RowVersion);
public sealed record WorkflowStageDifferenceDto(string Change, string StageCode, int? FromSequence, int? ToSequence, string[] ChangedFields);
public sealed record WorkflowDefinitionComparisonDto(WorkflowDefinitionDto From, WorkflowDefinitionDto To, WorkflowStageDifferenceDto[] StageDifferences);
public sealed record SaveReportingWindowRequest(Guid ReportingPeriodPublicId, SubmissionKind SubmissionKind, DateTime OpensAt, DateTime ClosesAt);
public sealed record ReportingWindowDto(Guid PublicId, Guid ReportingPeriodPublicId, string PeriodCode, SubmissionKind SubmissionKind, DateTime OpensAt, DateTime ClosesAt, bool IsActive, string RowVersion);
public sealed record SaveReportingWindowExceptionRequest(Guid? UserPublicId, Guid? DepartmentPublicId, Guid? UnitPublicId, DateTime ExtendedClosesAt, string Reason);
public sealed record ReportingWindowExceptionDto(Guid PublicId, string? UserId, int? DepartmentId, int? UnitId, DateTime ExtendedClosesAt, string Reason, string ApprovedByUserId, DateTime ApprovedAt, string RowVersion);
public sealed record SaveRatingValueRequest(decimal Value, string Label, decimal? MinimumAchievementPercent, decimal? MaximumAchievementPercent, int SortOrder);
public sealed record SaveRatingSchemeRequest(string Code, string Name, IReadOnlyList<SaveRatingValueRequest> Values);
public sealed record RatingValueDto(Guid PublicId, decimal Value, string Label, decimal? MinimumAchievementPercent, decimal? MaximumAchievementPercent, int SortOrder);
public sealed record RatingSchemeDto(Guid PublicId, string Code, string Name, bool IsActive, string RowVersion, RatingValueDto[] Values);
public sealed record WorkflowActionRequest(string ActionCode, WorkflowActionOutcome Outcome, string? Comment, decimal? RatingValue);
public sealed record WorkflowActionDto(Guid PublicId, int Sequence, string ActionCode, WorkflowActionOutcome Outcome, string ActorUserId, string? Comment, decimal? RatingValue, DateTime OccurredAt);
public sealed record StageRatingDto(Guid PublicId, Guid WorkflowActionPublicId, string StageCode, Guid RatingSchemePublicId, string RatingSchemeCode, Guid RatingValuePublicId, decimal Value, string Label, decimal? AchievementPercent, string? Comment, string RatedByUserId, string? RatedByName, DateTime RatedAt);
public sealed record RaisePerformanceRfiRequest(string Question, DateTime ResponseDueAt, IReadOnlyCollection<Guid>? EvidencePublicIds = null);
public sealed record RespondPerformanceRfiRequest(string Response, string RowVersion, IReadOnlyCollection<Guid>? EvidencePublicIds = null);
public sealed record ClosePerformanceRfiRequest(string? Comment, string RowVersion);
public sealed record RfiEvidenceDto(Guid PublicId, Guid EvidencePublicId, RfiEvidencePurpose Purpose, string FileName, string? ContentType, long SizeInBytes, string Sha256, string LinkedByUserId, DateTime LinkedAt, string Url);
public sealed record PerformanceRfiDto(Guid PublicId, string Question, string RaisedByUserId, DateTime RaisedAt, DateTime ResponseDueAt, string? Response, string? RespondedByUserId, DateTime? RespondedAt, string? ClosedByUserId, DateTime? ClosedAt, string RowVersion, RfiEvidenceDto[] Evidence);

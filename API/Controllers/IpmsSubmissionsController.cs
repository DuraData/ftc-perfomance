using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using FTCERP.Host.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/ipms-submissions")]
[Route("api/v1/ipms-submissions")]
[Authorize]
public class IpmsSubmissionsController : ControllerBase
{
    private const long MaximumEvidenceBytes = 25 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> AllowedEvidenceTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;
    private readonly IEvidenceBlobStorage _evidenceStorage;
    private readonly ISubmissionValueService _submissionValues;
    private readonly IConfigurableWorkflowService _configurableWorkflow;
    private readonly IReportingWindowService _reportingWindows;
    private readonly IEvidenceInspectionService _evidenceInspection;
    private readonly IEvidenceMalwareScanner _malwareScanner;
    private readonly IPerformanceSuggestionService _performanceSuggestions;

    public IpmsSubmissionsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccessControlService accessControlService,
        IWorkflowGovernanceService workflowGovernanceService,
        IEvidenceBlobStorage evidenceStorage,
        ISubmissionValueService submissionValues,
        IConfigurableWorkflowService configurableWorkflow,
        IReportingWindowService reportingWindows,
        IEvidenceInspectionService evidenceInspection,
        IEvidenceMalwareScanner malwareScanner,
        IPerformanceSuggestionService performanceSuggestions)
    {
        _context = context;
        _userManager = userManager;
        _accessControlService = accessControlService;
        _workflowGovernanceService = workflowGovernanceService;
        _evidenceStorage = evidenceStorage;
        _submissionValues = submissionValues;
        _configurableWorkflow = configurableWorkflow;
        _reportingWindows = reportingWindows;
        _evidenceInspection = evidenceInspection;
        _malwareScanner = malwareScanner;
        _performanceSuggestions = performanceSuggestions;
    }

    [HttpGet]
    public ActionResult<ApiResponse<object>> GetSubmissions() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null, "The unbounded IPMS submission collection is retired. Use /api/v1/ipms-submissions/page."));

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<IpmsSubmissionResponse>>>> GetSubmissionsPage([FromQuery] PagedQueryRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<IpmsSubmissionResponse>>(false, null, "User not found"));
        if (!SubmissionSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<IpmsSubmissionResponse>>(false, null, "SortBy must be createdAt, status, quarter, or indicatorNumber."));

        var scope = await _accessControlService.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ");
        if (!scope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<IpmsSubmissionResponse>>(true, PagedResponse<IpmsSubmissionResponse>.Empty(request.Page, request.PageSize)));
        var query = _context.IpmsSubmissions.AsNoTracking().AsQueryable();
        if (!scope.Unrestricted)
            query = query.Where(item => (item.IpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.IpmsTarget.DepartmentId.Value)) || (item.IpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.IpmsTarget.UnitId.Value)) || (item.IpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.IpmsTarget.AssignedUserId)) || scope.TargetIds.Contains(item.IpmsTargetId) || scope.KpiIds.Contains(item.IpmsTargetId));
        if (request.TargetPublicId.HasValue)
            query = query.Where(item => item.IpmsTarget.PublicId == request.TargetPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item =>
                item.IpmsTarget.IndicatorNumber.Contains(request.NormalizedSearch)
                || item.IpmsTarget.TargetName.Contains(request.NormalizedSearch)
                || (item.ReportingPeriod != null
                    && (item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3 || item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4 || item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                    && ((item.IpmsTarget.IsIndicatorNumberRevised && item.IpmsTarget.RevisedIndicatorNumber != null && item.IpmsTarget.RevisedIndicatorNumber.Contains(request.NormalizedSearch))
                        || (item.IpmsTarget.IsTargetNameRevised && item.IpmsTarget.RevisedTargetName != null && item.IpmsTarget.RevisedTargetName.Contains(request.NormalizedSearch))))
                || item.Status.Contains(request.NormalizedSearch) || item.Quarter.Contains(request.NormalizedSearch));

        var totalCount = await query.CountAsync();
        query = ApplySubmissionOrdering(query, request.NormalizedSortBy, request.Descending);
        var items = await query.Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.IpmsTarget).ThenInclude(target => target.Department)
            .Include(item => item.IpmsTarget).ThenInclude(target => target.Unit)
            .Include(item => item.SubmittedByUser).Include(item => item.ReportingPeriod)
            .AsSplitQuery().ToListAsync();
        var memberPermissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Ok(new ApiResponse<PagedResponse<IpmsSubmissionResponse>>(true,
            PagedResponse<IpmsSubmissionResponse>.Create(items.Select(item => ToAuthorizedResponse(item, memberPermissions)), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> SubmissionSortFields = ["createdat", "status", "quarter", "indicatornumber"];

    private static IQueryable<IpmsSubmission> ApplySubmissionOrdering(IQueryable<IpmsSubmission> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("status", false) => query.OrderBy(item => item.Status).ThenBy(item => item.PublicId),
            ("status", true) => query.OrderByDescending(item => item.Status).ThenBy(item => item.PublicId),
            ("quarter", false) => query.OrderBy(item => item.Quarter).ThenBy(item => item.PublicId),
            ("quarter", true) => query.OrderByDescending(item => item.Quarter).ThenBy(item => item.PublicId),
            ("indicatornumber", false) => query.OrderBy(item => item.ReportingPeriod != null
                && (item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3 || item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4 || item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                && item.IpmsTarget.IsIndicatorNumberRevised && item.IpmsTarget.RevisedIndicatorNumber != null ? item.IpmsTarget.RevisedIndicatorNumber : item.IpmsTarget.IndicatorNumber).ThenBy(item => item.PublicId),
            ("indicatornumber", true) => query.OrderByDescending(item => item.ReportingPeriod != null
                && (item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter3 || item.ReportingPeriod.PeriodType == ReportingPeriodType.Quarter4 || item.ReportingPeriod.PeriodType == ReportingPeriodType.Annual)
                && item.IpmsTarget.IsIndicatorNumberRevised && item.IpmsTarget.RevisedIndicatorNumber != null ? item.IpmsTarget.RevisedIndicatorNumber : item.IpmsTarget.IndicatorNumber).ThenBy(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.PublicId)
        };

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> GetSubmission(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));

        var item = await FindSubmissionAsync(id);
        if (item == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.READ", BuildScope(item));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));

        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, await ToAuthorizedResponseAsync(item, user)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> CreateSubmission([FromBody] SaveIpmsSubmissionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));

        var target = await _context.IpmsTargets.FirstOrDefaultAsync(item => item.Id == request.IpmsTargetId);
        if (target == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS target not found"));
        if (target.IsWithdrawn) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "A submission cannot be created for a withdrawn IPMS target."));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.CREATE", BuildScope(target));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));
        var memberError = await ValidateMemberUpdatesAsync(user, request, null);
        if (memberError != null) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, memberError));
        SubmissionValueResolution resolved;
        try { resolved = await _submissionValues.ResolveIpmsAsync(target.Id, request.Quarter, request.ActualPerformance, null); }
        catch (ArgumentException exception) { return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, exception.Message)); }
        catch (InvalidOperationException exception) { return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, exception.Message)); }
        var existingLogical = await _context.IpmsSubmissions.FirstOrDefaultAsync(item => item.IpmsTargetId == target.Id && item.ReportingPeriodId == resolved.Period.Id);
        if (existingLogical != null)
        {
            var existingLoaded = await FindSubmissionAsync(existingLogical.Id) ?? existingLogical;
            return Ok(new ApiResponse<IpmsSubmissionResponse>(true, await ToAuthorizedResponseAsync(existingLoaded, user), "Existing logical submission returned."));
        }

        var entity = new IpmsSubmission
        {
            IpmsTargetId = request.IpmsTargetId,
            MunicipalityId = target.MunicipalityId,
            ReportingPeriodId = resolved.Period.Id,
            ReportingPeriod = resolved.Period,
            Quarter = request.Quarter.Trim(),
            BaseState = SubmissionBaseStates.InProgress,
            Status = SubmissionBaseStates.InProgress,
            SubmitterStatus = "In Progress",
            VerifierStatus = "Pending",
            ApproverStatus = "Pending",
            PmsStatus = "Pending",
            AuditorStatus = "Pending",
            ActualPerformance = resolved.Calculation?.CanonicalActual,
            AchievementPercent = resolved.Calculation?.AchievementPercent,
            TargetAchieved = resolved.Calculation?.Achieved,
            ActualExpenditure = null,
            Variance = resolved.Calculation?.Variance,
            VarianceReason = request.VarianceReason?.Trim(),
            CorrectiveMeasure = request.CorrectiveMeasure?.Trim(),
            SubmitterScore = request.SubmitterScore,
            PoeType = request.PoeType?.Trim(),
            SubmittedByUserId = user.Id,
            CreatedBy = user.Id,
            CreatedOn = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.IpmsSubmissions.Add(entity);
        await _context.SaveChangesAsync();
        var created = await FindSubmissionAsync(entity.Id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmission", created.Id, "Create", null, created.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, await ToAuthorizedResponseAsync(created, user)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> UpdateSubmission(string id, [FromBody] SaveIpmsSubmissionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS submission not found"));
        if (entity.IsDisabled) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "A withdrawn IPMS submission is immutable."));

        if (!CanMutateInProgressSubmission(user.Id, entity.BaseState, entity.SubmittedByUserId, entity.IpmsTarget.AssignedUserId, out var mutationReason))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, mutationReason));
        }

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.UPDATE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));
        var memberError = await ValidateMemberUpdatesAsync(user, request, entity);
        if (memberError != null) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, memberError));
        if (!string.Equals(request.IpmsTargetId, entity.IpmsTargetId, StringComparison.Ordinal)) return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "A submission cannot be moved to another KPI."));
        SubmissionValueResolution resolved;
        try { resolved = await _submissionValues.ResolveIpmsAsync(entity.IpmsTargetId, request.Quarter, request.ActualPerformance, null); }
        catch (ArgumentException exception) { return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, exception.Message)); }
        catch (InvalidOperationException exception) { return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, exception.Message)); }
        if (entity.ReportingPeriodId.HasValue && entity.ReportingPeriodId != resolved.Period.Id) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "A submission cannot be moved to another reporting period."));

        var before = await FindSubmissionAsync(id);
        entity.Quarter = request.Quarter.Trim();
        entity.ReportingPeriodId = resolved.Period.Id;
        entity.ActualPerformance = resolved.Calculation?.CanonicalActual;
        entity.AchievementPercent = resolved.Calculation?.AchievementPercent;
        entity.TargetAchieved = resolved.Calculation?.Achieved;
        entity.ActualExpenditure = null;
        entity.Variance = resolved.Calculation?.Variance;
        entity.VarianceReason = request.VarianceReason?.Trim();
        entity.CorrectiveMeasure = request.CorrectiveMeasure?.Trim();
        entity.SubmitterScore = request.SubmitterScore;
        entity.PoeType = request.PoeType?.Trim();
        entity.UpdatedBy = user.Id;
        entity.UpdatedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var after = await FindSubmissionAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmission", id, "Edit", before?.ToResponse(), after.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, await ToAuthorizedResponseAsync(after, user)));
    }

    [HttpPost("{id}/consolidation-suggestion")]
    public async Task<ActionResult<ApiResponse<PerformanceSuggestionResult>>> GenerateConsolidationSuggestion(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PerformanceSuggestionResult>(false, null, "User not found"));
        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<PerformanceSuggestionResult>(false, null, "IPMS submission not found"));
        if (!CanMutateInProgressSubmission(user.Id, entity.BaseState, entity.SubmittedByUserId, entity.IpmsTarget.AssignedUserId, out var mutationReason))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PerformanceSuggestionResult>(false, null, mutationReason));
        var denial = await ConsolidationPermissionDenialAsync(user, entity, update: true);
        if (denial != null) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PerformanceSuggestionResult>(false, null, denial));

        var result = await _performanceSuggestions.GenerateIpmsAsync(id, user.Id, HttpContext.TraceIdentifier);
        if (result.Code == "CONCURRENCY_CONFLICT") return Conflict(new ApiResponse<PerformanceSuggestionResult>(false, result, result.Explanation));
        return result.Generated || result.Code == "SUGGESTION_ALREADY_GENERATED"
            ? Ok(new ApiResponse<PerformanceSuggestionResult>(true, result))
            : UnprocessableEntity(new ApiResponse<PerformanceSuggestionResult>(false, result, result.Explanation));
    }

    [HttpPut("{id}/consolidated-actual")]
    public async Task<ActionResult<ApiResponse<PerformanceSuggestionResult>>> SaveConsolidatedActual(string id, [FromBody] SaveConsolidatedActualRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PerformanceSuggestionResult>(false, null, "User not found"));
        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<PerformanceSuggestionResult>(false, null, "IPMS submission not found"));
        if (!CanMutateInProgressSubmission(user.Id, entity.BaseState, entity.SubmittedByUserId, entity.IpmsTarget.AssignedUserId, out var mutationReason))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PerformanceSuggestionResult>(false, null, mutationReason));
        var denial = await ConsolidationPermissionDenialAsync(user, entity, update: true);
        if (denial != null) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PerformanceSuggestionResult>(false, null, denial));

        var result = await _performanceSuggestions.RecordFinalIpmsActualAsync(id, request.ActualPerformance, request.EditReason, request.RowVersion, user.Id, HttpContext.TraceIdentifier);
        if (result.Code == "CONCURRENCY_CONFLICT") return Conflict(new ApiResponse<PerformanceSuggestionResult>(false, result, result.Explanation));
        return result.ManualRequired
            ? UnprocessableEntity(new ApiResponse<PerformanceSuggestionResult>(false, result, result.Explanation))
            : Ok(new ApiResponse<PerformanceSuggestionResult>(true, result));
    }

    [HttpGet("{id}/consolidation-history")]
    public ActionResult<ApiResponse<PerformanceSuggestionEventResponse[]>> GetConsolidationHistory(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<PerformanceSuggestionEventResponse[]>(false, null,
            $"This unbounded consolidation history route is retired. Use /api/v1/ipms-submissions/{id}/consolidation-history/page."));

    [HttpGet("{id}/consolidation-history/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>>> GetConsolidationHistoryPage(
        string id,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? eventType = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(false, null, "User not found"));
        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).SingleOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(false, null, "IPMS submission not found"));
        var denial = await ConsolidationPermissionDenialAsync(user, entity, update: false);
        if (denial != null) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(false, null, denial));
        var memberAccess = await GetConsolidationHistoryMemberAccessAsync(user, BuildScope(entity));
        if (request.NormalizedSortBy is not ("createdat" or "occurredat" or "eventtype" or "actor" or "actoruserid"))
            return BadRequest(new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(false, null, "SortBy must be occurredAt, eventType, or actor."));
        if (request.NormalizedSortBy is "actor" or "actoruserid" && !memberAccess.Actor)
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(false, null, "Suggestion actor sorting requires member read permission."));

        var query = _context.PerformanceSuggestionEvents.AsNoTracking().Include(item => item.ActorUser).Where(item => item.IpmsSubmissionId == id);
        if (request.NormalizedSearch.Length > 0)
        {
            var actorPublicId = Guid.TryParse(request.NormalizedSearch, out var parsedActorPublicId) ? parsedActorPublicId : (Guid?)null;
            query = query.Where(item => (memberAccess.Reason && item.Reason != null && item.Reason.Contains(request.NormalizedSearch))
                || (memberAccess.Actor && ((actorPublicId.HasValue && item.ActorUser.PublicId == actorPublicId.Value)
                    || item.ActorUser.FirstName.Contains(request.NormalizedSearch) || item.ActorUser.LastName.Contains(request.NormalizedSearch)))
                || (memberAccess.CorrelationId && item.CorrelationId.Contains(request.NormalizedSearch))
                || (item.SystemSuggestedActualPerformance != null && item.SystemSuggestedActualPerformance.Contains(request.NormalizedSearch))
                || (item.ActualPerformance != null && item.ActualPerformance.Contains(request.NormalizedSearch)));
        }
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            if (!Enum.TryParse<PerformanceSuggestionEventType>(eventType.Trim(), true, out var parsedEventType))
                return BadRequest(new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(false, null, "EventType must be Generated, Accepted, or Edited."));
            query = query.Where(item => item.EventType == parsedEventType);
        }

        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("eventtype", false) => query.OrderBy(item => item.EventType).ThenBy(item => item.PublicId),
            ("eventtype", true) => query.OrderByDescending(item => item.EventType).ThenByDescending(item => item.PublicId),
            ("actor" or "actoruserid", false) => query.OrderBy(item => item.ActorUser.FirstName).ThenBy(item => item.ActorUser.LastName).ThenBy(item => item.PublicId),
            ("actor" or "actoruserid", true) => query.OrderByDescending(item => item.ActorUser.FirstName).ThenByDescending(item => item.ActorUser.LastName).ThenByDescending(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.OccurredAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.PublicId)
        };
        var persistedEvents = await ordered.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var events = persistedEvents.Select(item => new PerformanceSuggestionEventResponse(item.PublicId, item.EventType.ToString(), item.SystemSuggestedActualPerformance,
            item.ActualPerformance, item.WasSystemSuggestionEdited, item.EffectiveCalculationType?.ToString(),
            item.SourcePeriods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            memberAccess.Actor ? item.ActorUser.PublicId : null,
            memberAccess.Actor ? item.ActorUser.FullName : null,
            memberAccess.Reason ? item.Reason : null, item.OccurredAt,
            memberAccess.CorrelationId ? item.CorrelationId : null)).ToArray();
        return Ok(new ApiResponse<PagedResponse<PerformanceSuggestionEventResponse>>(true,
            PagedResponse<PerformanceSuggestionEventResponse>.Create(events, request.Page, request.PageSize, totalCount)));
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<bool>> DeleteSubmission(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<bool>(false, false, "Governed submissions are never deleted. Use POST /api/v1/ipms-submissions/{id}/withdraw with a reason and RowVersion."));

    [HttpPost("{id}/withdraw")]
    public async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> WithdrawSubmission(string id, [FromBody] WithdrawGovernedRecordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "A withdrawal reason between 1 and 1000 characters is required."));

        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS submission not found"));
        if (entity.IsDisabled) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "The IPMS submission is already withdrawn."));
        if (!entity.MunicipalityId.HasValue) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "The IPMS submission must be reconciled to a municipality before withdrawal."));

        if (!CanMutateInProgressSubmission(user.Id, entity.BaseState, entity.SubmittedByUserId, entity.IpmsTarget.AssignedUserId, out var mutationReason))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, mutationReason));
        }

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.WITHDRAW", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));
        if (!TrySetExpectedVersion(entity, request.RowVersion))
            return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "A valid RowVersion is required."));

        var before = await FindSubmissionAsync(id);
        var occurredAt = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
        entity.IsDisabled = true;
        entity.WithdrawalReason = reason;
        entity.WithdrawnAt = occurredAt;
        entity.WithdrawnByUserId = user.Id;
        entity.UpdatedBy = user.Id;
        entity.UpdatedOn = occurredAt;
        _context.GovernedRecordLifecycleEvents.Add(new GovernedRecordLifecycleEvent
        {
            MunicipalityId = entity.MunicipalityId.Value,
            AggregateType = "IpmsSubmission",
            AggregateId = entity.Id,
            Action = GovernedLifecycleAction.Withdrawn,
            Reason = reason,
            ActorUserId = user.Id,
            OccurredAt = occurredAt,
            CorrelationId = HttpContext.TraceIdentifier
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmission", id, "Withdraw", before?.ToResponse(), entity.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "The IPMS submission changed before withdrawal. Refresh and try again."));
        }
        var response = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, await ToAuthorizedResponseAsync(response, user)));
    }

    private bool TrySetExpectedVersion(IpmsSubmission entity, string value)
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

    [HttpGet("{id}/attachments")]
    public ActionResult<ApiResponse<PoeFileResponse[]>> GetAttachments(string id) =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<PoeFileResponse[]>(false, null,
            $"This unbounded evidence route is retired. Use /api/v1/ipms-submissions/{id}/attachments/page."));

    [HttpGet("{id}/attachments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PoeFileResponse>>>> GetAttachmentsPage(
        string id,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? scanStatus = null,
        [FromQuery] bool? quarantined = null,
        [FromQuery] bool? active = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PagedResponse<PoeFileResponse>>(false, null, "User not found"));

        var submission = await _context.IpmsSubmissions
            .AsNoTracking()
            .Include(item => item.IpmsTarget)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PagedResponse<PoeFileResponse>>(false, null, "IPMS submission not found"));

        var scope = BuildScope(submission);
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.READ", scope);
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PagedResponse<PoeFileResponse>>(false, null, decision.Reason));

        var query = _context.PoeFiles
            .AsNoTrackingWithIdentityResolution()
            .Where(item => item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id);

        if (!string.IsNullOrWhiteSpace(request.NormalizedSearch))
            query = query.Where(item => item.FileName.Contains(request.NormalizedSearch));
        if (!string.IsNullOrWhiteSpace(scanStatus))
            query = query.Where(item => item.Blob.ScanStatus == scanStatus.Trim());
        if (quarantined.HasValue)
            query = query.Where(item => item.Blob.IsQuarantined == quarantined.Value);
        if (active.HasValue)
            query = query.Where(item => item.IsActive == active.Value);

        var totalCount = await query.CountAsync();
        var ordered = request.NormalizedSortBy switch
        {
            "uploadedat" or "createdat" => request.Descending ? query.OrderByDescending(item => item.UploadedAt).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.UploadedAt).ThenBy(item => item.PublicId),
            "filename" => request.Descending ? query.OrderByDescending(item => item.FileName).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.FileName).ThenBy(item => item.PublicId),
            "filesize" => request.Descending ? query.OrderByDescending(item => item.Blob.SizeInBytes).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.Blob.SizeInBytes).ThenBy(item => item.PublicId),
            "scanstatus" => request.Descending ? query.OrderByDescending(item => item.Blob.ScanStatus).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.Blob.ScanStatus).ThenBy(item => item.PublicId),
            "retainuntil" => request.Descending ? query.OrderByDescending(item => item.RetainUntil).ThenByDescending(item => item.PublicId) : query.OrderBy(item => item.RetainUntil).ThenBy(item => item.PublicId),
            _ => null
        };
        if (ordered == null)
            return BadRequest(new ApiResponse<PagedResponse<PoeFileResponse>>(false, null, "SortBy must be uploadedAt, fileName, fileSize, scanStatus, or retainUntil."));

        var files = await ordered.IncludePoeGovernance().Skip(request.Offset).Take(request.PageSize).ToListAsync();
        var memberAccess = await GetPoeMemberAccessAsync(user, scope);

        return Ok(new ApiResponse<PagedResponse<PoeFileResponse>>(true,
            PagedResponse<PoeFileResponse>.Create(files.Select(item => item.ToResponse(HttpContext, memberAccess)), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(MaximumEvidenceBytes)]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> UploadAttachment(string id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, "File is required"));
        if (file.Length > MaximumEvidenceBytes) return StatusCode(StatusCodes.Status413PayloadTooLarge, new ApiResponse<PoeFileResponse>(false, null, "File exceeds the 25 MB evidence limit"));
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedEvidenceTypes.TryGetValue(extension, out var contentTypes) || !contentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ApiResponse<PoeFileResponse>(false, null, "Unsupported evidence file type"));

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));

        var submission = await _context.IpmsSubmissions
            .Include(item => item.IpmsTarget)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        if (submission.IsDisabled) return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Evidence cannot be added to a withdrawn IPMS submission."));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.UPLOAD", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        var content = buffer.ToArray();
        var inspection = _evidenceInspection.Inspect(content, extension);
        if (!inspection.SignatureValid) return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ApiResponse<PoeFileResponse>(false, null, inspection.Error));
        var malwareScan = await _malwareScanner.ScanAsync(content, file.FileName, file.ContentType, HttpContext.RequestAborted);

        var relativeDirectory = Path.Combine("poe", "ipms", id);
        var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativePath = Path.Combine(relativeDirectory, safeFileName);
        var stored = await _evidenceStorage.StoreAsync(relativePath, content, HttpContext.RequestAborted);
        if (!stored.Succeeded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<PoeFileResponse>(false, null, "Evidence storage is unavailable: " + stored.Detail));

        var entity = new PoeFile
        {
            SubmissionKind = SubmissionKind.Ipms,
            SubmissionId = id,
            MunicipalityId = submission.MunicipalityId,
            FileName = file.FileName,
            Blob = new EvidenceBlob { MunicipalityId = submission.MunicipalityId, StorageKey = relativePath, ContentType = file.ContentType, SizeInBytes = file.Length, Sha256 = inspection.Sha256, SignatureVerified = inspection.SignatureValid, ScanStatus = malwareScan.Status, IsQuarantined = !malwareScan.IsClean, ScannerProvider = malwareScan.Provider, ScannerReference = malwareScan.ProviderReference, ScanDetail = malwareScan.Detail, ScannedAt = DateTime.UtcNow },
            RetainUntil = DateTime.UtcNow.AddYears(7),
            UploadedByUserId = user.Id,
            UploadedAt = DateTime.UtcNow
        };

        _context.PoeFiles.Add(entity);
        try { await _context.SaveChangesAsync(); }
        catch
        {
            await _evidenceStorage.DisposeAsync(relativePath, CancellationToken.None);
            throw;
        }

        var created = await _context.PoeFiles.IncludePoeGovernance().FirstAsync(item => item.Id == entity.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", entity.Id, "Upload", null, created.ToResponse(HttpContext, PoeResponseMemberAccess.Full), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await _workflowGovernanceService.CreateWorkflowNotificationsAsync(GetRelevantUserIds(submission), NotificationType.Submission, "IPMS evidence uploaded", $"A POE file was uploaded for IPMS submission '{id}'.", "IpmsSubmission", id);
        return Ok(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(created, user, BuildScope(submission))));
    }

    [HttpGet("{id}/attachments/{attachmentId}/content")]
    public async Task<IActionResult> DownloadAttachment(string id, string attachmentId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var submission = await _context.IpmsSubmissions.AsNoTracking().Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound();
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.READ", BuildScope(submission));
        if (!decision.Allowed) return Forbid();
        var evidence = await _context.PoeFiles.AsNoTracking().Include(item => item.Blob).FirstOrDefaultAsync(item => item.Id == attachmentId && item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id && item.IsActive && !item.Blob.IsContentDeleted && !item.Blob.IsQuarantined && item.Blob.SignatureVerified && item.Blob.ScanStatus == "Clean");
        if (evidence == null) return NotFound();
        var stored = await _evidenceStorage.ReadAsync(evidence.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found) return stored.Available ? NotFound() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        return File(stored.Content, evidence.Blob.ContentType ?? "application/octet-stream", evidence.FileName, enableRangeProcessing: true);
    }

    [HttpPost("{id}/attachments/{attachmentId}/rescan")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> RescanAttachment(string id, string attachmentId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));
        var submission = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.UPLOAD", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));
        var evidence = await _context.PoeFiles.IncludePoeGovernance().FirstOrDefaultAsync(item => item.Id == attachmentId && item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id && item.IsActive);
        if (evidence == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Attachment not found"));
        if (evidence.Blob.IsContentDeleted) return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Disposed evidence content cannot be rescanned"));
        var stored = await _evidenceStorage.ReadAsync(evidence.Blob.StorageKey, HttpContext.RequestAborted);
        if (!stored.Found)
            return stored.Available
                ? NotFound(new ApiResponse<PoeFileResponse>(false, null, "Stored evidence content not found"))
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<PoeFileResponse>(false, null, "Evidence storage is unavailable: " + stored.Detail));
        var before = new { evidence.Blob.ScanStatus, evidence.Blob.IsQuarantined, evidence.Blob.ScannerReference };
        var scan = await _malwareScanner.ScanAsync(stored.Content, evidence.FileName, evidence.Blob.ContentType ?? "application/octet-stream", HttpContext.RequestAborted);
        evidence.Blob.ScanStatus = scan.Status; evidence.Blob.IsQuarantined = !scan.IsClean; evidence.Blob.ScannerProvider = scan.Provider; evidence.Blob.ScannerReference = scan.ProviderReference; evidence.Blob.ScanDetail = scan.Detail; evidence.Blob.ScannedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", evidence.Id, "MalwareRescan", before, new { evidence.Blob.ScanStatus, evidence.Blob.IsQuarantined, evidence.Blob.ScannerReference }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(evidence, user, BuildScope(submission)), scan.IsClean ? "Evidence released after a clean scan." : "Evidence remains quarantined."));
    }

    [HttpPost("{id}/attachments/{attachmentId}/assessments")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> AssessAttachment(string id, string attachmentId, AssessPoeRequest request)
    {
        var comment = request.Comment?.Trim();
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));
        var submission = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.ASSESS", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));
        var evidence = await _context.PoeFiles.IncludePoeGovernance()
            .FirstOrDefaultAsync(item => item.Id == attachmentId && item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id && item.IsActive);
        if (evidence == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Attachment not found"));
        var policy = PoeAssessmentPolicy.Validate(evidence, request.Outcome, comment);
        if (!policy.Allowed) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, policy.Error));
        var assessment = new PoeEvidenceAssessment { MunicipalityId = submission.MunicipalityId!.Value, PoeFileId = evidence.Id, PoeFile = evidence, Outcome = request.Outcome, Comment = comment, AssessedByUserId = user.Id, AssessedByUser = user, AssessedAt = DateTime.UtcNow, CorrelationId = HttpContext.TraceIdentifier };
        evidence.Assessments.Add(assessment);
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", evidence.Id, "Assess:" + request.Outcome, null, new { assessment.PublicId, assessment.Outcome, assessment.Comment, assessment.AssessedAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(evidence, user, BuildScope(submission)), "Evidence assessment recorded."));
    }

    [HttpPost("{id}/attachments/{attachmentId}/replace")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> ReplaceAttachment(string id, string attachmentId, ReplacePoeRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));
        var submission = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        if (submission.IsDisabled) return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Evidence cannot be replaced on a withdrawn IPMS submission."));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.REPLACE", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));
        var rows = await _context.PoeFiles.IncludePoeGovernance().Where(item => item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id && (item.Id == attachmentId || item.PublicId == request.ReplacementEvidencePublicId)).ToArrayAsync();
        var superseded = rows.SingleOrDefault(item => item.Id == attachmentId);
        var replacement = rows.SingleOrDefault(item => item.PublicId == request.ReplacementEvidencePublicId);
        if (superseded == null || replacement == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Both the superseded and replacement evidence records are required"));
        var policy = PoeReplacementPolicy.Validate(superseded, replacement, SubmissionKind.Ipms, id, request.Reason);
        if (!policy.Allowed) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, policy.Error));
        if (!TrySetPoeRowVersion(superseded, request.SupersededRowVersion) || !TrySetPoeRowVersion(replacement, request.ReplacementRowVersion))
            return BadRequest(new ApiResponse<PoeFileResponse>(false, null, "Valid row versions are required for both evidence records"));
        var ledger = new PoeEvidenceReplacement { MunicipalityId = submission.MunicipalityId!.Value, SupersededPoeFileId = superseded.Id, SupersededPoeFile = superseded, ReplacementPoeFileId = replacement.Id, ReplacementPoeFile = replacement, Reason = request.Reason.Trim(), ReplacedByUserId = user.Id, ReplacedByUser = user, ReplacedAt = DateTime.UtcNow, CorrelationId = HttpContext.TraceIdentifier };
        superseded.IsActive = false; replacement.SupersedesPoeFileId = superseded.Id; superseded.ReplacementsAsOld.Add(ledger); replacement.ReplacementAsNew = ledger;
        _context.PoeEvidenceReplacements.Add(ledger);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Evidence changed before replacement could be recorded")); }
        catch (DbUpdateException) { return Conflict(new ApiResponse<PoeFileResponse>(false, null, "One of these evidence records already participates in a replacement")); }
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", superseded.Id, "Replace", new { superseded.PublicId, superseded.FileName }, new { ledger.PublicId, ReplacementPublicId = replacement.PublicId, ledger.Reason, ledger.ReplacedAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(superseded, user, BuildScope(submission)), "Evidence replacement recorded; the prior record remains retained in immutable history."));
    }

    [HttpPost("{id}/attachments/{attachmentId}/legal-holds")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> PlaceLegalHold(string id, string attachmentId, PlacePoeLegalHoldRequest request)
    {
        var error = PoeLegalHoldPolicy.ValidateText(request.HoldReference, request.Reason);
        if (error != null) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, error));
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));
        var submission = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.PLACE_HOLD", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));
        var evidence = await _context.PoeFiles.IncludePoeGovernance().FirstOrDefaultAsync(item => item.Id == attachmentId && item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id);
        if (evidence == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Attachment not found"));
        if (evidence.DisposalEvents.Any(item => item.Action == PoeDisposalAction.Completed)) return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Disposed evidence can no longer be placed under legal hold"));
        if (evidence.LegalHoldEvents.GroupBy(item => item.HoldId).Any(group => group.First().HoldReference == request.HoldReference.Trim() && group.All(item => item.Action != PoeLegalHoldAction.Released)))
            return Conflict(new ApiResponse<PoeFileResponse>(false, null, "An active legal hold with this reference already exists for the evidence"));
        var hold = new PoeLegalHoldEvent { HoldId = Guid.NewGuid(), MunicipalityId = submission.MunicipalityId!.Value, PoeFileId = evidence.Id, PoeFile = evidence, Action = PoeLegalHoldAction.Placed, HoldReference = request.HoldReference.Trim(), Reason = request.Reason.Trim(), ActorUserId = user.Id, ActorUser = user, OccurredAt = DateTime.UtcNow, CorrelationId = HttpContext.TraceIdentifier };
        evidence.LegalHoldEvents.Add(hold); _context.PoeLegalHoldEvents.Add(hold); await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", evidence.Id, "LegalHoldPlaced", null, new { hold.HoldId, hold.HoldReference, hold.Reason, hold.OccurredAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(evidence, user, BuildScope(submission)), "Legal hold placed."));
    }

    [HttpPost("{id}/attachments/{attachmentId}/legal-holds/{holdId:guid}/release")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> ReleaseLegalHold(string id, string attachmentId, Guid holdId, ReleasePoeLegalHoldRequest request)
    {
        var error = PoeLegalHoldPolicy.ValidateReleaseReason(request.Reason);
        if (error != null) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, error));
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));
        var submission = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.RELEASE_HOLD", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));
        var evidence = await _context.PoeFiles.IncludePoeGovernance().FirstOrDefaultAsync(item => item.Id == attachmentId && item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id);
        if (evidence == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Attachment not found"));
        var placed = evidence.LegalHoldEvents.FirstOrDefault(item => item.HoldId == holdId && item.Action == PoeLegalHoldAction.Placed);
        if (placed == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Legal hold not found"));
        if (!PoeLegalHoldPolicy.IsActive(evidence.LegalHoldEvents, holdId)) return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Legal hold has already been released"));
        var release = new PoeLegalHoldEvent { HoldId = holdId, MunicipalityId = submission.MunicipalityId!.Value, PoeFileId = evidence.Id, PoeFile = evidence, Action = PoeLegalHoldAction.Released, HoldReference = placed.HoldReference, Reason = request.Reason.Trim(), ActorUserId = user.Id, ActorUser = user, OccurredAt = DateTime.UtcNow, CorrelationId = HttpContext.TraceIdentifier };
        evidence.LegalHoldEvents.Add(release); _context.PoeLegalHoldEvents.Add(release);
        try { await _context.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Legal hold was released concurrently")); }
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", evidence.Id, "LegalHoldReleased", new { holdId, placed.HoldReference }, new { release.Reason, release.OccurredAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(evidence, user, BuildScope(submission)), "Legal hold released."));
    }

    [HttpPost("{id}/attachments/{attachmentId}/disposals")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> RequestDisposal(string id, string attachmentId, RequestPoeDisposalRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));
        var submission = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "IPMS submission not found"));
        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.DISPOSE", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));
        var evidence = await _context.PoeFiles.IncludePoeGovernance().FirstOrDefaultAsync(item => item.Id == attachmentId && item.SubmissionKind == SubmissionKind.Ipms && item.SubmissionId == id);
        if (evidence == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "Attachment not found"));
        var policy = PoeDisposalPolicy.Validate(evidence, request.ApprovalReference, request.Reason, DateTime.UtcNow);
        if (!policy.Allowed) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, policy.Error));
        if (!PoeRowVersionMatches(evidence, request.RowVersion)) return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Evidence changed before disposal could be requested"));
        var disposal = new PoeDisposalEvent { DisposalId = Guid.NewGuid(), MunicipalityId = submission.MunicipalityId!.Value, PoeFileId = evidence.Id, PoeFile = evidence, Action = PoeDisposalAction.Requested, ApprovalReference = request.ApprovalReference.Trim(), Reason = request.Reason.Trim(), ActorUserId = user.Id, ActorUser = user, OccurredAt = DateTime.UtcNow, CorrelationId = HttpContext.TraceIdentifier };
        evidence.DisposalEvents.Add(disposal); _context.PoeDisposalEvents.Add(disposal);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<PoeFileResponse>(false, null, "Evidence changed before disposal could be requested")); }
        catch (DbUpdateException) { return Conflict(new ApiResponse<PoeFileResponse>(false, null, "A disposal request was recorded concurrently")); }
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmissionAttachment", evidence.Id, "DisposalRequested", null, new { disposal.DisposalId, disposal.ApprovalReference, disposal.Reason, disposal.OccurredAt }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Accepted(new ApiResponse<PoeFileResponse>(true, await ToAuthorizedPoeResponseAsync(evidence, user, BuildScope(submission)), "Evidence disposal was queued for controlled storage processing."));
    }

    private bool TrySetPoeRowVersion(PoeFile file, string value)
    {
        try { var bytes = Convert.FromBase64String(value); if (bytes.Length != 8) return false; _context.Entry(file).Property(item => item.RowVersion).OriginalValue = bytes; return true; }
        catch (FormatException) { return false; }
    }

    private static bool PoeRowVersionMatches(PoeFile file, string value)
    {
        try { var bytes = Convert.FromBase64String(value); return bytes.Length == 8 && file.RowVersion.SequenceEqual(bytes); }
        catch (FormatException) { return false; }
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAttachment(string id, string attachmentId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));

        var submission = await _context.IpmsSubmissions
            .Include(item => item.IpmsTarget)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<bool>(false, false, "IPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_POE.UPLOAD", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var file = await _context.PoeFiles.Include(item => item.UploadedByUser).FirstOrDefaultAsync(item =>
            item.Id == attachmentId &&
            item.SubmissionKind == SubmissionKind.Ipms &&
            item.SubmissionId == id);
        if (file == null) return NotFound(new ApiResponse<bool>(false, false, "Attachment not found"));

        return Conflict(new ApiResponse<bool>(false, false, "Evidence is an auditable record and cannot be hard-deleted; use the governed replacement workflow"));
    }

    [HttpPost("{id}/submit")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Submit(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_SUBMISSION.SUBMIT", "submitted", "Submit", NotificationType.Submission, request);

    [HttpPost("{id}/verify")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Verify(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_SUBMISSION.VERIFY", "verified", "Verify", NotificationType.Approval, request);

    [HttpPost("{id}/verify-reject")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> VerifyReject(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_SUBMISSION.VERIFY_REJECT", "verify_rejected", "VerifyReject", NotificationType.VerifyRejection, request);

    [HttpPost("{id}/approve")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Approve(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_SUBMISSION.APPROVE", "approved", "Approve", NotificationType.Approval, request);

    [HttpPost("{id}/reject")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Reject(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_SUBMISSION.REJECT", "rejected", "Reject", NotificationType.Rejection, request);

    [HttpPost("{id}/review")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Review(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_WORKFLOW.PMS_REVIEW", "reviewed", "Review", NotificationType.Rfi, request);

    [HttpPost("{id}/audit")]
    public Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Audit(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "IPMS_WORKFLOW.INTERNAL_AUDIT", "audited", "Audit", NotificationType.InternalAuditRfi, request);

    [HttpPost("{id}/score")]
    public async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> Score(string id, [FromBody] SubmissionWorkflowActionRequest request)
    {
        if (request.Score == null) return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "Score is required"));

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS submission not found"));
        if (entity.IsDisabled) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "A withdrawn IPMS submission cannot be scored."));

        if (string.Equals(SubmissionBaseStates.Normalize(entity.BaseState), SubmissionBaseStates.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "Scoring is not allowed while the submission is still in draft state."));
        }

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_WORKFLOW.PMS_REVIEW", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));

        _context.SubmissionScores.Add(new SubmissionScore
        {
            SubmissionKind = SubmissionKind.Ipms,
            SubmissionId = id,
            Score = request.Score.Value,
            Notes = request.Comment,
            ScoredByUserId = user.Id,
            ScoredAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmission", id, "Score", null, new { request.Score, request.Comment }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        var response = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, response.ToResponse()));
    }

    [HttpPost("{id}/extend-due-date")]
    public async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> ExtendDueDate(string id, [FromBody] DueDateExtensionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.IpmsSubmissions.Include(item => item.IpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS submission not found"));
        if (entity.IsDisabled) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "A withdrawn IPMS submission cannot receive a due-date extension."));

        var currentStatus = NormalizeStatus(entity.Status);
        if (string.Equals(currentStatus, "audited", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "Due date cannot be extended after audit has been completed."));
        }

        var decision = await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.EXTEND_DUE_DATE", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));

        var previousDueDate = entity.DueDate ?? DateTime.UtcNow;
        entity.DueDate = request.ExtendedDueDate;
        entity.ExtendedDueDate = request.ExtendedDueDate;
        entity.DueDateExtendedDays = request.ExtendedByDays ?? (int)Math.Max(0, (request.ExtendedDueDate.Date - previousDueDate.Date).TotalDays);
        entity.UpdatedBy = user.Id;
        entity.UpdatedOn = DateTime.UtcNow;
        _context.DueDateExtensions.Add(new DueDateExtension
        {
            SubmissionKind = SubmissionKind.Ipms,
            SubmissionId = id,
            OriginalDueDate = previousDueDate,
            ExtendedDueDate = request.ExtendedDueDate,
            Reason = request.Reason,
            ApprovedByUserId = user.Id,
            ApprovedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("IpmsSubmission", id, "ExtendDueDate", new { DueDate = previousDueDate }, new { DueDate = request.ExtendedDueDate, request.Reason }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await _workflowGovernanceService.CreateWorkflowNotificationsAsync(GetRelevantUserIds(entity), NotificationType.DueDateExtension, "IPMS due date extended", $"The due date for IPMS submission '{entity.Id}' was extended.", "IpmsSubmission", id);

        var response = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, response.ToResponse()));
    }

    private async Task<ActionResult<ApiResponse<IpmsSubmissionResponse>>> ApplyWorkflowAction(string id, string permissionCode, string status, string action, NotificationType notificationType, SubmissionWorkflowActionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.IpmsSubmissions
            .Include(item => item.IpmsTarget)
            .Include(item => item.SubmittedByUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IpmsSubmissionResponse>(false, null, "IPMS submission not found"));
        if (entity.IsDisabled) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "A withdrawn IPMS submission cannot transition workflow."));

        var decision = await _accessControlService.CheckPermissionAsync(user, permissionCode, BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<IpmsSubmissionResponse>(false, null, decision.Reason));
        if (string.Equals(permissionCode, "IPMS_SUBMISSION.SUBMIT", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(entity.ActualPerformance))
            return BadRequest(new ApiResponse<IpmsSubmissionResponse>(false, null, "Authoritative actual performance is required before submission."));
        var configurable = entity.ReportingPeriodId.HasValue && await HasConfiguredWorkflowAsync(entity.ReportingPeriodId.Value);
        if (!configurable)
            return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, "No effective configured workflow exists for this municipality, financial year, and submission type."));
        {
            if (string.Equals(permissionCode, "IPMS_SUBMISSION.SUBMIT", StringComparison.OrdinalIgnoreCase))
            {
                var window = await _reportingWindows.CheckAsync(SubmissionKind.Ipms, entity.ReportingPeriodId!.Value, user.Id, entity.IpmsTarget.DepartmentId, entity.IpmsTarget.UnitId, DateTime.UtcNow);
                if (!window.Allowed) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, window.Reason));
            }
            var outcome = status is "rejected" or "verify_rejected" ? WorkflowActionOutcome.Reject : status == "submitted" ? WorkflowActionOutcome.Submit : WorkflowActionOutcome.Approve;
            var transition = await _configurableWorkflow.PrepareActionAsync(SubmissionKind.Ipms, entity.Id, entity.ReportingPeriodId!.Value, entity.SubmittedByUserId ?? entity.CreatedBy ?? user.Id, user.Id, permissionCode, outcome, request.Comment, request.Score, HttpContext.TraceIdentifier);
            if (!transition.Allowed) return Conflict(new ApiResponse<IpmsSubmissionResponse>(false, null, transition.Reason));
            _context.AuditTrails.Add(new AuditTrail { EntityName = "IpmsSubmission", EntityId = id, Action = permissionCode, NewValue = System.Text.Json.JsonSerializer.Serialize(new { status, request.Comment, request.Score, transition.Reason }), ChangedBy = user.Id, IpAddress = PerformanceApiSupport.GetIpAddress(HttpContext) });
        }

        var before = await FindSubmissionAsync(id);
        entity.Status = status;
        if (string.Equals(status, "submitted", StringComparison.OrdinalIgnoreCase)) entity.BaseState = SubmissionBaseStates.Submitted;
        else if (status is "rejected" or "verify_rejected") entity.BaseState = SubmissionBaseStates.InProgress;
        entity.UpdatedBy = user.Id;
        entity.UpdatedOn = DateTime.UtcNow;
        if (string.Equals(status, "submitted", StringComparison.OrdinalIgnoreCase))
        {
            entity.SubmittedAt = DateTime.UtcNow;
            entity.SubmittedByUserId = user.Id;
            entity.SubmitterStatus = "Submitted";
            entity.VerifierStatus = "Pending";
            entity.ApproverStatus = "Pending";
            entity.PmsStatus = "Pending";
            entity.AuditorStatus = "Pending";
        }
        else if (string.Equals(status, "verified", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "verify_rejected", StringComparison.OrdinalIgnoreCase))
        {
            entity.VerifierUserId = user.Id;
            entity.VerifiedAt = DateTime.UtcNow;
            entity.VerifierComments = request.Comment;
            entity.VerifierComment = request.Comment;
            entity.VerifierScore = request.Score;
            entity.VerifierStatus = string.Equals(status, "verified", StringComparison.OrdinalIgnoreCase) ? "Verified" : "Rejected";
            entity.SubmitterStatus = string.Equals(status, "verified", StringComparison.OrdinalIgnoreCase) ? "Verified" : "Needs Rework";
        }
        else if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            entity.ApproverUserId = user.Id;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApproverComments = request.Comment;
            entity.ApproverComment = request.Comment;
            entity.ApproverScore = request.Score;
            entity.ApproverStatus = string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : "Rejected";
            entity.SubmitterStatus = string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : "Needs Rework";
        }
        else if (string.Equals(status, "audited", StringComparison.OrdinalIgnoreCase))
        {
            entity.AuditorUserId = user.Id;
            entity.AuditedAt = DateTime.UtcNow;
            entity.AuditorComments = request.Comment;
            entity.AuditorComment = request.Comment;
            entity.AuditorRecommendation = request.Recommendation;
            entity.AuditorScore = request.Score;
            entity.AuditorResponseDueDate = request.ResponseDueDate;
            entity.AuditorStatus = "Audited";
            entity.SubmitterStatus = "Respond To Audit";
        }
        else if (string.Equals(status, "reviewed", StringComparison.OrdinalIgnoreCase))
        {
            entity.PmsOfficerUserId = user.Id;
            entity.PmsReviewedAt = DateTime.UtcNow;
            entity.PmsComments = request.Comment;
            entity.PmsComment = request.Comment;
            entity.PmsRecommendation = request.Recommendation;
            entity.PmsScore = request.Score;
            entity.PmsResponseDueDate = request.ResponseDueDate;
            entity.PmsRfiComment = request.RfiComment;
            entity.PmsStatus = "Reviewed";
            entity.SubmitterStatus = "Respond To PMS";
        }
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            _context.ReviewComments.Add(new ReviewComment
            {
                SubmissionKind = SubmissionKind.Ipms,
                SubmissionId = id,
                Comment = request.Comment,
                CommentedByUserId = user.Id,
                CommentedAt = DateTime.UtcNow
            });
        }

        _workflowGovernanceService.QueueWorkflowNotifications(GetRelevantUserIds(entity), notificationType, $"IPMS submission {action}", $"IPMS submission '{entity.Id}' was marked as {status}.", "IpmsSubmission", id);
        await _context.SaveChangesAsync();

        var after = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<IpmsSubmissionResponse>(true, after.ToResponse()));
    }

    private IEnumerable<string> GetRelevantUserIds(IpmsSubmission submission)
    {
        return new[] { submission.SubmittedByUserId, submission.IpmsTarget.AssignedUserId }.Where(id => !string.IsNullOrWhiteSpace(id))!;
    }

    private async Task<bool> HasConfiguredWorkflowAsync(long reportingPeriodId)
    {
        var municipalityYearId = await _context.ReportingPeriods.Where(item => item.Id == reportingPeriodId).Select(item => item.MunicipalityFinancialYearId).SingleAsync();
        return await _context.WorkflowDefinitions.AnyAsync(item => item.MunicipalityFinancialYearId == municipalityYearId && item.SubmissionKind == SubmissionKind.Ipms && item.IsActive);
    }

    private static bool CanMutateInProgressSubmission(string actorUserId, string? baseState, string? submittedByUserId, string? assignedUserId, out string reason)
    {
        if (!IsSubmissionOwner(actorUserId, submittedByUserId, assignedUserId))
        {
            reason = "Only the submission owner can edit or delete this submission.";
            return false;
        }

        var normalizedState = SubmissionBaseStates.Normalize(baseState);
        if (!string.Equals(normalizedState, SubmissionBaseStates.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"Submissions in '{normalizedState}' base state cannot be edited.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsSubmissionOwner(string actorUserId, string? submittedByUserId, string? assignedUserId)
    {
        return (!string.IsNullOrWhiteSpace(submittedByUserId)
                && string.Equals(submittedByUserId, actorUserId, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(assignedUserId)
                && string.Equals(assignedUserId, actorUserId, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "draft"
            : status.Trim().ToLowerInvariant();
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private Task<IpmsSubmission?> FindSubmissionAsync(string id)
    {
        return _context.IpmsSubmissions
            .Include(item => item.IpmsTarget).ThenInclude(target => target.Department)
            .Include(item => item.ReportingPeriod)
            .Include(item => item.IpmsTarget).ThenInclude(target => target.Unit)
            .Include(item => item.SubmittedByUser)
            .Include(item => item.VerifierUser)
            .Include(item => item.ApproverUser)
            .Include(item => item.PmsOfficerUser)
            .Include(item => item.AuditorUser)
            .Include(item => item.SuggestionEditedByUser)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    private async Task<string?> ValidateMemberUpdatesAsync(ApplicationUser user, SaveIpmsSubmissionRequest request, IpmsSubmission? existing)
    {
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualChanged = existing == null
            ? !string.IsNullOrWhiteSpace(request.ActualPerformance) || request.ActualExpenditure.HasValue
            : request.ActualPerformance?.Trim() != existing.ActualPerformance || request.ActualExpenditure != existing.ActualExpenditure;
        if (actualChanged && !permissions.Contains("IPMS_SUBMISSION.ActualPerformance.UPDATE")) return "Actual Performance is protected by member-level security.";
        var varianceReasonChanged = existing == null
            ? !string.IsNullOrWhiteSpace(request.VarianceReason)
            : request.VarianceReason?.Trim() != existing.VarianceReason;
        if (varianceReasonChanged && !permissions.Contains("IPMS_SUBMISSION.VarianceReason.UPDATE"))
            return "Variance Reason is protected by member-level security.";
        var correctiveMeasureChanged = existing == null
            ? !string.IsNullOrWhiteSpace(request.CorrectiveMeasure)
            : request.CorrectiveMeasure?.Trim() != existing.CorrectiveMeasure;
        if (correctiveMeasureChanged && !permissions.Contains("IPMS_SUBMISSION.CorrectiveMeasure.UPDATE"))
            return "Corrective Measure is protected by member-level security.";
        return null;
    }

    private async Task<IpmsSubmissionResponse> ToAuthorizedResponseAsync(IpmsSubmission submission, ApplicationUser user)
    {
        var permissions = (await _accessControlService.GetEffectiveAccessAsync(user)).EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var response = ToAuthorizedResponse(submission, permissions);
        var scope = BuildScope(submission);
        var actorAllowed = (await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.SuggestionActor.READ", scope)).Allowed;
        var reasonAllowed = (await _accessControlService.CheckPermissionAsync(user, "IPMS_SUBMISSION.SuggestionReason.READ", scope)).Allowed;
        return response with
        {
            SuggestionEditedByUserPublicId = actorAllowed ? submission.SuggestionEditedByUser?.PublicId : null,
            SuggestionEditedByName = actorAllowed ? submission.SuggestionEditedByUser?.FullName : null,
            SuggestionEditReason = reasonAllowed ? submission.SuggestionEditReason : null
        };
    }

    private async Task<PoeFileResponse> ToAuthorizedPoeResponseAsync(PoeFile file, ApplicationUser user, AccessScopeContext scope)
        => file.ToResponse(HttpContext, await GetPoeMemberAccessAsync(user, scope));

    private async Task<PoeResponseMemberAccess> GetPoeMemberAccessAsync(ApplicationUser user, AccessScopeContext scope)
    {
        async Task<bool> CanReadAsync(string memberCode)
            => (await _accessControlService.CheckPermissionAsync(user, $"IPMS_POE.{memberCode}.READ", scope))?.Allowed == true;

        return new PoeResponseMemberAccess(
            await CanReadAsync("UploadedByUserId"),
            await CanReadAsync("UploadedByName"),
            await CanReadAsync("ScannerProvider"),
            await CanReadAsync("ScannerReference"),
            await CanReadAsync("ScanDetail"),
            await CanReadAsync("AssessmentComment"),
            await CanReadAsync("AssessedByUserId"),
            await CanReadAsync("AssessedByName"),
            await CanReadAsync("AssessmentCorrelationId"),
            await CanReadAsync("ReplacedByUserId"),
            await CanReadAsync("ReplacedByName"),
            await CanReadAsync("ReplacementCorrelationId"),
            await CanReadAsync("LegalHoldActorUserId"),
            await CanReadAsync("LegalHoldActorName"),
            await CanReadAsync("DisposalRequestedByUserId"),
            await CanReadAsync("DisposalRequestedByName"),
            await CanReadAsync("DisposalDetail"));
    }

    private async Task<string?> ConsolidationPermissionDenialAsync(ApplicationUser user, IpmsSubmission submission, bool update)
    {
        var scope = BuildScope(submission);
        var entityDecision = await _accessControlService.CheckPermissionAsync(user, update ? "IPMS_SUBMISSION.UPDATE" : "IPMS_SUBMISSION.READ", scope);
        if (!entityDecision.Allowed) return entityDecision.Reason;
        var memberDecision = await _accessControlService.CheckPermissionAsync(user,
            update ? "IPMS_SUBMISSION.ActualPerformance.UPDATE" : "IPMS_SUBMISSION.ActualPerformance.READ", scope);
        return memberDecision.Allowed ? null : memberDecision.Reason;
    }

    private async Task<(bool Actor, bool Reason, bool CorrelationId)> GetConsolidationHistoryMemberAccessAsync(
        ApplicationUser user,
        AccessScopeContext scope)
    {
        async Task<bool> CanReadAsync(string memberCode) =>
            (await _accessControlService.CheckPermissionAsync(user, $"IPMS_SUBMISSION.{memberCode}.READ", scope)).Allowed;

        return (await CanReadAsync("SuggestionActor"), await CanReadAsync("SuggestionReason"),
            await CanReadAsync("SuggestionCorrelationId"));
    }

    private static IpmsSubmissionResponse ToAuthorizedResponse(IpmsSubmission submission, HashSet<string> permissions)
    {
        var response = submission.ToResponse();
        if (!permissions.Contains("IPMS_SUBMISSION.ActualPerformance.READ"))
            response = response with { ActualPerformance = null, ActualExpenditure = null, SystemSuggestedActualPerformance = null, WasSystemSuggestionEdited = false, SuggestionGeneratedDate = null, SuggestionEditedByUserPublicId = null, SuggestionEditedByName = null, SuggestionEditedAt = null, SuggestionEditReason = null, AchievementPercent = null, TargetAchieved = null };
        if (!permissions.Contains("IPMS_SUBMISSION.Variance.READ")) response = response with { Variance = null };
        if (!permissions.Contains("IPMS_SUBMISSION.VarianceReason.READ")) response = response with { VarianceReason = null };
        if (!permissions.Contains("IPMS_SUBMISSION.CorrectiveMeasure.READ")) response = response with { CorrectiveMeasure = null };
        if (!permissions.Contains("IPMS_SUBMISSION.SubmittedDate.READ")) response = response with { SubmittedAt = null };
        if (!permissions.Contains("IPMS_SUBMISSION.InternalAuditObservation.READ"))
            response = response with { AuditedAt = null, AuditorComments = null, AuditorComment = null, AuditorRecommendation = null, AuditorScore = null, AuditorResponseDueDate = null };
        return response;
    }

    private static AccessScopeContext BuildScope(IpmsSubmission submission) => BuildScope(submission.IpmsTarget);

    private static AccessScopeContext BuildScope(IpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, MunicipalityId: target.MunicipalityId);
}

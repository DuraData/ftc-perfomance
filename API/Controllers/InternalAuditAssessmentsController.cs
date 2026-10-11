using System.Text.Json;
using System.Security.Claims;
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
[Route("api/v1/internal-audit")]
[Authorize]
public sealed class InternalAuditAssessmentsController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    IAccessControlService accessControl,
    IWorkflowGovernanceService governance,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("configurations/page")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<PagedResponse<InternalAuditConfigurationDto>>>> ConfigurationsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] InternalAuditAssessmentModel? model = null,
        [FromQuery] bool? current = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<InternalAuditConfigurationDto>>();
        if (!ConfigurationSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<InternalAuditConfigurationDto>>("SortBy must be createdAt, financialYear, model, version, or effectiveFrom."));
        var query = context.InternalAuditAssessmentConfigurations.AsNoTracking().AsQueryable();
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch.ToLowerInvariant();
            query = query.Where(item => item.Reason.ToLower().Contains(search)
                || item.MunicipalityFinancialYear.FinancialYear.Code.ToLower().Contains(search));
        }
        if (model.HasValue) query = query.Where(item => item.Model == model.Value);
        if (current.HasValue) query = query.Where(item => item.IsCurrent == current.Value);
        var totalCount = await query.CountAsync();
        var rows = await ApplyConfigurationOrdering(query, request.NormalizedSortBy, request.Descending)
            .Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<InternalAuditConfigurationDto>>(true,
            PagedResponse<InternalAuditConfigurationDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("configurations")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public ActionResult<ApiResponse<InternalAuditConfigurationDto[]>> Configurations()
    {
        if (!HasTenant()) return TenantRequired<InternalAuditConfigurationDto[]>();
        return StatusCode(StatusCodes.Status410Gone,
            Fail<InternalAuditConfigurationDto[]>("This unbounded route is retired. Use the /configurations/page endpoint."));
    }

    private static readonly HashSet<string> ConfigurationSortFields = ["createdat", "financialyear", "model", "version", "effectivefrom"];

    private static IOrderedQueryable<InternalAuditAssessmentConfiguration> ApplyConfigurationOrdering(
        IQueryable<InternalAuditAssessmentConfiguration> query, string sortBy, bool descending) => (sortBy, descending) switch
        {
            ("financialyear", false) => query.OrderBy(item => item.MunicipalityFinancialYear.FinancialYear.Code).ThenBy(item => item.Id),
            ("financialyear", true) => query.OrderByDescending(item => item.MunicipalityFinancialYear.FinancialYear.Code).ThenByDescending(item => item.Id),
            ("model", false) => query.OrderBy(item => item.Model).ThenBy(item => item.Id),
            ("model", true) => query.OrderByDescending(item => item.Model).ThenByDescending(item => item.Id),
            ("version", false) => query.OrderBy(item => item.Version).ThenBy(item => item.Id),
            ("version", true) => query.OrderByDescending(item => item.Version).ThenByDescending(item => item.Id),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };

    [HttpPost("configurations")]
    [Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
    public async Task<ActionResult<ApiResponse<InternalAuditConfigurationDto>>> Configure(SaveInternalAuditConfigurationRequest request)
    {
        if (!HasTenant()) return TenantRequired<InternalAuditConfigurationDto>();
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<InternalAuditConfigurationDto>("User not found."));
        if (!Enum.IsDefined(request.Model) || request.EffectiveFrom.Kind == DateTimeKind.Unspecified)
            return BadRequest(Fail<InternalAuditConfigurationDto>("A valid model and timezone-aware effective date are required."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length is < 10 or > 1000)
            return BadRequest(Fail<InternalAuditConfigurationDto>("A governance reason of 10 to 1000 characters is required."));

        var year = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId);
        if (year == null) return BadRequest(Fail<InternalAuditConfigurationDto>("Municipality financial year not found."));
        var current = await context.InternalAuditAssessmentConfigurations
            .SingleOrDefaultAsync(item => item.MunicipalityFinancialYearId == year.Id && item.IsCurrent);
        if (current != null)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentRowVersion))
                return Conflict(Fail<InternalAuditConfigurationDto>("Reload the current configuration before replacing it."));
            if (!TrySetRowVersion(current, request.CurrentRowVersion))
                return BadRequest(Fail<InternalAuditConfigurationDto>("A valid current RowVersion is required."));
            if (request.EffectiveFrom <= current.EffectiveFrom)
                return BadRequest(Fail<InternalAuditConfigurationDto>("A replacement must become effective after the current version."));
            current.IsCurrent = false;
            current.EffectiveTo = request.EffectiveFrom;
        }

        var entity = new InternalAuditAssessmentConfiguration
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            MunicipalityFinancialYearId = year.Id,
            MunicipalityFinancialYear = year,
            Model = request.Model,
            Version = (current?.Version ?? 0) + 1,
            EffectiveFrom = request.EffectiveFrom.ToUniversalTime(),
            Reason = reason,
            CreatedByUserId = user.Id
        };
        context.InternalAuditAssessmentConfigurations.Add(entity);
        governance.QueueAuditTrail(nameof(InternalAuditAssessmentConfiguration), entity.PublicId.ToString(), "SelectModel",
            current == null ? null : new { current.PublicId, current.Model, current.Version },
            new { entity.Model, entity.Version, entity.EffectiveFrom, Reason = reason }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<InternalAuditConfigurationDto>("The model configuration changed; reload and try again.")); }
        catch (DbUpdateException) { return Conflict(Fail<InternalAuditConfigurationDto>("Another model version was selected; reload and try again.")); }
        return Ok(new ApiResponse<InternalAuditConfigurationDto>(true, ToDto(entity)));
    }

    [HttpGet("submissions/{kind}/{submissionId:guid}")]
    public async Task<ActionResult<ApiResponse<InternalAuditSubmissionDto>>> Submission(SubmissionKind kind, string submissionId)
    {
        if (!HasTenant()) return TenantRequired<InternalAuditSubmissionDto>();
        submissionId = await ResolveSubmissionIdAsync(kind, submissionId) ?? string.Empty;
        if (submissionId.Length == 0) return NotFound(Fail<InternalAuditSubmissionDto>("Submission not found."));
        var loaded = await LoadSubmission(kind, submissionId);
        if (loaded == null) return NotFound(Fail<InternalAuditSubmissionDto>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<InternalAuditSubmissionDto>("User not found."));
        var readCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, readCode, loaded.Scope)).Allowed) return Forbid();
        if (loaded.Instance == null) return Conflict(Fail<InternalAuditSubmissionDto>("The submission has no configured workflow instance."));
        var configuration = await EffectiveConfiguration(loaded.Instance.WorkflowDefinition.MunicipalityFinancialYearId);
        if (configuration == null) return Conflict(Fail<InternalAuditSubmissionDto>("No Internal Audit assessment model is configured for this financial year."));
        var memberAccess = await GetMemberAccess(user, kind, loaded.Scope);
        var latestAssessment = await LatestAssessment(loaded.Instance.Id, memberAccess);
        return Ok(new ApiResponse<InternalAuditSubmissionDto>(true, new(ToDto(configuration), latestAssessment)));
    }

    [HttpGet("submissions/{kind}/{submissionId:guid}/assessments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<InternalAuditAssessmentDto>>>> AssessmentsPage(
        SubmissionKind kind, string submissionId, [FromQuery] PagedQueryRequest request)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<InternalAuditAssessmentDto>>();
        submissionId = await ResolveSubmissionIdAsync(kind, submissionId) ?? string.Empty;
        if (submissionId.Length == 0) return NotFound(Fail<PagedResponse<InternalAuditAssessmentDto>>("Submission not found."));
        var loaded = await LoadSubmission(kind, submissionId);
        if (loaded == null) return NotFound(Fail<PagedResponse<InternalAuditAssessmentDto>>("Submission not found."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PagedResponse<InternalAuditAssessmentDto>>("User not found."));
        var readCode = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ";
        if (!(await accessControl.CheckPermissionAsync(user, readCode, loaded.Scope)).Allowed) return Forbid();
        if (loaded.Instance == null) return Conflict(Fail<PagedResponse<InternalAuditAssessmentDto>>("The submission has no configured workflow instance."));
        if (request.NormalizedSortBy is not ("createdat" or "assessedat" or "outcome" or "assessedby"))
            return BadRequest(Fail<PagedResponse<InternalAuditAssessmentDto>>("SortBy must be assessedAt, outcome, or assessedBy."));
        var memberAccess = await GetMemberAccess(user, kind, loaded.Scope);
        if (request.NormalizedSortBy == "assessedby" && !memberAccess.AssessedByRead) return Forbid();

        var query = context.InternalAuditAssessments.AsNoTracking()
            .Where(item => item.SubmissionWorkflowInstanceId == loaded.Instance.Id);
        if (request.NormalizedSearch.Length > 0)
        {
            var actorPublicId = Guid.TryParse(request.NormalizedSearch, out var parsedActorPublicId) ? parsedActorPublicId : (Guid?)null;
            query = query.Where(item => memberAccess.ObservationRead && item.DetailedObservation.Contains(request.NormalizedSearch)
                || memberAccess.CommentRead && item.Comment != null && item.Comment.Contains(request.NormalizedSearch)
                || memberAccess.FindingsRead && item.Findings != null && item.Findings.Contains(request.NormalizedSearch)
                || memberAccess.RecommendationRead && item.Recommendation != null && item.Recommendation.Contains(request.NormalizedSearch)
                || memberAccess.AssessedByRead && item.AssessedByUser != null && ((actorPublicId.HasValue && item.AssessedByUser.PublicId == actorPublicId.Value)
                    || item.AssessedByUser.FirstName.Contains(request.NormalizedSearch)
                    || item.AssessedByUser.LastName.Contains(request.NormalizedSearch)));
        }
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("outcome", false) => query.OrderBy(item => item.Outcome).ThenBy(item => item.Id),
            ("outcome", true) => query.OrderByDescending(item => item.Outcome).ThenByDescending(item => item.Id),
            ("assessedby", false) => query.OrderBy(item => item.AssessedByUser.LastName).ThenBy(item => item.AssessedByUser.FirstName).ThenBy(item => item.Id),
            ("assessedby", true) => query.OrderByDescending(item => item.AssessedByUser.LastName).ThenByDescending(item => item.AssessedByUser.FirstName).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.AssessedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.AssessedAt).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.Configuration).Include(item => item.AssessedByUser)
            .Include(item => item.PreviousAssessment).Include(item => item.PerformanceRfi)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<InternalAuditAssessmentDto>>(true,
            PagedResponse<InternalAuditAssessmentDto>.Create(rows.Select(item => ToDto(item, memberAccess)), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("submissions/{kind}/{submissionId:guid}/assessments")]
    public async Task<ActionResult<ApiResponse<InternalAuditAssessmentDto>>> Assess(SubmissionKind kind, string submissionId, SaveInternalAuditAssessmentRequest request)
    {
        if (!HasTenant()) return TenantRequired<InternalAuditAssessmentDto>();
        submissionId = await ResolveSubmissionIdAsync(kind, submissionId) ?? string.Empty;
        if (submissionId.Length == 0) return NotFound(Fail<InternalAuditAssessmentDto>("Submission not found."));
        var loaded = await LoadSubmission(kind, submissionId);
        if (loaded == null) return NotFound(Fail<InternalAuditAssessmentDto>("Submission not found."));
        if (loaded.Instance == null) return Conflict(Fail<InternalAuditAssessmentDto>("The submission has no configured workflow instance."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<InternalAuditAssessmentDto>("User not found."));
        var permissionCode = kind == SubmissionKind.Opms ? "OPMS_WORKFLOW.INTERNAL_AUDIT" : "IPMS_WORKFLOW.INTERNAL_AUDIT";
        if (!(await accessControl.CheckPermissionAsync(user, permissionCode, loaded.Scope)).Allowed) return Forbid();
        var memberAccess = await GetMemberAccess(user, kind, loaded.Scope);
        if (!memberAccess.ObservationUpdate
            || request.Comment != null && !memberAccess.CommentUpdate
            || request.Findings != null && !memberAccess.FindingsUpdate
            || request.Recommendation != null && !memberAccess.RecommendationUpdate
            || request.Score.HasValue && !memberAccess.ScoreUpdate)
            return Forbid();

        var configuration = await EffectiveConfiguration(loaded.Instance.WorkflowDefinition.MunicipalityFinancialYearId);
        if (configuration == null) return Conflict(Fail<InternalAuditAssessmentDto>("No Internal Audit assessment model is configured for this financial year."));
        var validation = Validate(configuration.Model, request);
        if (validation != null) return BadRequest(Fail<InternalAuditAssessmentDto>(validation));
        var auditStage = loaded.Instance.WorkflowDefinition.Stages.FirstOrDefault(item => item.IsActive && (item.Code.Equals("AUDIT", StringComparison.OrdinalIgnoreCase) || item.Code.Contains("INTERNAL_AUDIT", StringComparison.OrdinalIgnoreCase)));
        RatingSchemeValue? ratingValue = null;
        if (configuration.Model == InternalAuditAssessmentModel.Detailed && auditStage?.RequiresRating == true)
        {
            if (!request.Score.HasValue) return BadRequest(Fail<InternalAuditAssessmentDto>("The configured Internal Audit stage requires a rating."));
            ratingValue = auditStage.RatingScheme?.Values.SingleOrDefault(item => item.Value == request.Score.Value);
            if (ratingValue == null) return BadRequest(Fail<InternalAuditAssessmentDto>("Select a value from the configured Internal Audit rating scheme."));
        }

        var previous = await context.InternalAuditAssessments
            .Where(item => item.SubmissionWorkflowInstanceId == loaded.Instance.Id)
            .OrderByDescending(item => item.AssessedAt).ThenByDescending(item => item.Id).FirstOrDefaultAsync();
        if (previous == null && request.PreviousAssessmentPublicId.HasValue)
            return Conflict(Fail<InternalAuditAssessmentDto>("The supplied prior assessment does not exist."));
        if (previous != null && request.PreviousAssessmentPublicId != previous.PublicId)
            return Conflict(Fail<InternalAuditAssessmentDto>("The assessment history changed; reload before reassessing."));

        var now = DateTime.UtcNow;
        var entity = new InternalAuditAssessment
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            SubmissionWorkflowInstanceId = loaded.Instance.Id,
            SubmissionWorkflowInstance = loaded.Instance,
            ConfigurationId = configuration.Id,
            PreviousAssessmentId = previous?.Id,
            PreviousAssessment = previous,
            Outcome = request.Outcome,
            DetailedObservation = request.DetailedObservation.Trim(),
            Comment = Clean(request.Comment),
            Findings = Clean(request.Findings),
            Recommendation = Clean(request.Recommendation),
            Score = request.Score,
            AssessedByUserId = user.Id,
            AssessedByUser = user,
            AssessedAt = now,
            CorrelationId = HttpContext.TraceIdentifier
        };

        var createsRfi = request.Outcome is InternalAuditAssessmentOutcome.NotAchieved or InternalAuditAssessmentOutcome.NotSatisfactory;
        if (createsRfi)
        {
            var rfi = new PerformanceRfi
            {
                MunicipalityId = tenantContext.MunicipalityId!.Value,
                SubmissionWorkflowInstanceId = loaded.Instance.Id,
                SubmissionWorkflowInstance = loaded.Instance,
                Question = entity.DetailedObservation,
                RaisedByUserId = user.Id,
                RaisedAt = now,
                ResponseDueAt = request.ResponseDueAt!.Value.ToUniversalTime()
            };
            context.PerformanceRfis.Add(rfi);
            entity.PerformanceRfi = rfi;
            context.SubmissionWorkflowActions.Add(NewAction(loaded.Instance, permissionCode, WorkflowActionOutcome.RaiseRfi, user.Id, entity.DetailedObservation));
            governance.QueueWorkflowNotifications(loaded.Recipients, NotificationType.InternalAuditRfi, "Internal Audit RFI raised", $"Internal Audit raised an RFI for {kind} submission '{submissionId}'.", kind + "Submission", submissionId);
        }
        context.InternalAuditAssessments.Add(entity);
        var auditAction = NewAction(loaded.Instance, request.Outcome is InternalAuditAssessmentOutcome.Achieved or InternalAuditAssessmentOutcome.Satisfactory ? "AUDIT_ACCEPTED" : "AUDIT_REJECTED", WorkflowActionOutcome.Complete, user.Id, entity.DetailedObservation);
        auditAction.RatingValue = request.Score;
        context.SubmissionWorkflowActions.Add(auditAction);
        if (ratingValue != null && auditStage?.RatingScheme != null)
            context.SubmissionStageRatings.Add(new SubmissionStageRating
            {
                MunicipalityId = tenantContext.MunicipalityId!.Value, SubmissionWorkflowInstanceId = loaded.Instance.Id, SubmissionWorkflowInstance = loaded.Instance,
                SubmissionWorkflowAction = auditAction, WorkflowStageDefinitionId = auditStage.Id, WorkflowStageDefinition = auditStage,
                RatingSchemeId = auditStage.RatingScheme.Id, RatingScheme = auditStage.RatingScheme, RatingSchemeValueId = ratingValue.Id, RatingSchemeValue = ratingValue,
                Value = ratingValue.Value, LabelSnapshot = ratingValue.Label, Comment = entity.Comment, RatedByUserId = user.Id, RatedByUser = user,
                RatedAt = now, CorrelationId = HttpContext.TraceIdentifier
            });
        governance.QueueAuditTrail(nameof(InternalAuditAssessment), entity.PublicId.ToString(), previous == null ? "Assess" : "Reassess", null,
            new { configuration.Model, request.Outcome, entity.DetailedObservation, entity.Comment, entity.Findings, entity.Recommendation, entity.Score, request.ResponseDueAt, PreviousAssessmentPublicId = previous?.PublicId },
            user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(Fail<InternalAuditAssessmentDto>("The assessment could not be appended because the workflow changed.")); }
        return Ok(new ApiResponse<InternalAuditAssessmentDto>(true, ToDto(entity, configuration.Model, memberAccess)));
    }

    private static string? Validate(InternalAuditAssessmentModel model, SaveInternalAuditAssessmentRequest request)
    {
        var observation = request.DetailedObservation?.Trim();
        if (string.IsNullOrWhiteSpace(observation) || observation.Length > 4000)
            return "IA Detailed Observation is required and must not exceed 4000 characters.";
        if (request.Comment?.Length > 2000 || request.Findings?.Length > 4000 || request.Recommendation?.Length > 4000)
            return "Internal Audit narrative fields exceed their configured limits.";
        if (model == InternalAuditAssessmentModel.SatisfactoryNotSatisfactory)
        {
            if (request.Outcome is not (InternalAuditAssessmentOutcome.Satisfactory or InternalAuditAssessmentOutcome.NotSatisfactory))
                return "This municipality uses the Satisfactory / Not Satisfactory assessment model.";
            if (request.Score.HasValue || !string.IsNullOrWhiteSpace(request.Comment) || !string.IsNullOrWhiteSpace(request.Findings) || !string.IsNullOrWhiteSpace(request.Recommendation))
                return "The Satisfactory / Not Satisfactory model contains exactly Status and Detailed Observation.";
        }
        else if (request.Outcome is not (InternalAuditAssessmentOutcome.Achieved or InternalAuditAssessmentOutcome.NotAchieved))
            return "This municipality uses the Detailed Internal Audit assessment model.";
        if (request.Outcome is InternalAuditAssessmentOutcome.NotAchieved or InternalAuditAssessmentOutcome.NotSatisfactory)
        {
            if (!request.ResponseDueAt.HasValue || request.ResponseDueAt.Value.ToUniversalTime() <= DateTime.UtcNow)
                return "A future IA RFI Due Date is required for an adverse assessment.";
        }
        else if (request.ResponseDueAt.HasValue)
            return "An IA RFI Due Date is only valid when an Internal Audit RFI is raised.";
        return null;
    }

    private async Task<InternalAuditAssessmentConfiguration?> EffectiveConfiguration(long yearId) =>
        await context.InternalAuditAssessmentConfigurations.AsNoTracking().Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Where(item => item.MunicipalityFinancialYearId == yearId && item.IsCurrent && item.EffectiveFrom <= DateTime.UtcNow && (!item.EffectiveTo.HasValue || item.EffectiveTo > DateTime.UtcNow))
            .OrderByDescending(item => item.Version).FirstOrDefaultAsync();

    private async Task<InternalAuditAssessmentDto?> LatestAssessment(long instanceId, InternalAuditMemberAccess memberAccess)
    {
        var row = await context.InternalAuditAssessments.AsNoTracking().Include(item => item.Configuration).Include(item => item.AssessedByUser)
            .Include(item => item.PreviousAssessment).Include(item => item.PerformanceRfi)
            .Where(item => item.SubmissionWorkflowInstanceId == instanceId)
            .OrderByDescending(item => item.AssessedAt).ThenByDescending(item => item.Id).FirstOrDefaultAsync();
        return row == null ? null : ToDto(row, memberAccess);
    }

    private async Task<string?> ResolveSubmissionIdAsync(SubmissionKind kind, string publicId)
    {
        if (!Guid.TryParse(publicId, out var parsed)) return null;
        return kind switch
        {
            SubmissionKind.Opms => await context.OpmsSubmissions.AsNoTracking().Where(item => item.PublicId == parsed).Select(item => item.Id).SingleOrDefaultAsync(),
            SubmissionKind.Ipms => await context.IpmsSubmissions.AsNoTracking().Where(item => item.PublicId == parsed).Select(item => item.Id).SingleOrDefaultAsync(),
            _ => null
        };
    }

    private async Task<SubmissionAccess?> LoadSubmission(SubmissionKind kind, string submissionId)
    {
        int? department; int? unit; string? owner; string? submitter;
        if (kind == SubmissionKind.Opms)
        {
            var row = await context.OpmsSubmissions.AsNoTracking().Include(item => item.OpmsTarget).SingleOrDefaultAsync(item => item.Id == submissionId);
            if (row == null) return null;
            department = row.OpmsTarget.DepartmentId; unit = row.OpmsTarget.UnitId; owner = row.OpmsTarget.AssignedUserId; submitter = row.SubmittedByUserId ?? row.CreatedBy;
        }
        else if (kind == SubmissionKind.Ipms)
        {
            var row = await context.IpmsSubmissions.AsNoTracking().Include(item => item.IpmsTarget).SingleOrDefaultAsync(item => item.Id == submissionId);
            if (row == null) return null;
            department = row.IpmsTarget.DepartmentId; unit = row.IpmsTarget.UnitId; owner = row.IpmsTarget.AssignedUserId; submitter = row.SubmittedByUserId ?? row.CreatedBy;
        }
        else return null;
        var instance = await context.SubmissionWorkflowInstances
            .Include(item => item.WorkflowDefinition).ThenInclude(item => item.Stages).ThenInclude(item => item.RatingScheme).ThenInclude(item => item!.Values)
            .SingleOrDefaultAsync(item => item.SubmissionKind == kind && item.SubmissionId == submissionId);
        return new(new(department, unit, owner, TargetId: submissionId, MunicipalityId: tenantContext.MunicipalityId), instance,
            new[] { owner, submitter }.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private SubmissionWorkflowAction NewAction(SubmissionWorkflowInstance instance, string code, WorkflowActionOutcome outcome, string userId, string comment) => new()
    {
        MunicipalityId = tenantContext.MunicipalityId!.Value, SubmissionWorkflowInstanceId = instance.Id, SubmissionWorkflowInstance = instance,
        Sequence = instance.NextSequence++, FromStageId = instance.CurrentStageId, ToStageId = instance.CurrentStageId, ActionCode = code,
        Outcome = outcome, ActorUserId = userId, Comment = comment, CorrelationId = HttpContext.TraceIdentifier
    };

    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private async Task<InternalAuditMemberAccess> GetMemberAccess(ApplicationUser user, SubmissionKind kind, AccessScopeContext scope)
    {
        var resource = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION" : "IPMS_SUBMISSION";
        async Task<bool> Allowed(string member, string operation) =>
            (await accessControl.CheckPermissionAsync(user, $"{resource}.{member}.{operation}", scope)).Allowed;
        return new(
            await Allowed("InternalAuditObservation", "READ"), await Allowed("InternalAuditObservation", "UPDATE"),
            await Allowed("InternalAuditComment", "READ"), await Allowed("InternalAuditComment", "UPDATE"),
            await Allowed("InternalAuditFindings", "READ"), await Allowed("InternalAuditFindings", "UPDATE"),
            await Allowed("InternalAuditRecommendation", "READ"), await Allowed("InternalAuditRecommendation", "UPDATE"),
            await Allowed("InternalAuditScore", "READ"), await Allowed("InternalAuditScore", "UPDATE"),
            await Allowed("InternalAuditAssessedBy", "READ"), await Allowed("InternalAuditRfi", "READ"));
    }
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private bool TrySetRowVersion(InternalAuditAssessmentConfiguration entity, string encoded) { try { context.Entry(entity).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(encoded); return true; } catch (FormatException) { return false; } }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before using Internal Audit."));
    private static InternalAuditConfigurationDto ToDto(InternalAuditAssessmentConfiguration item) => new(item.PublicId, item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code, item.Model, item.Version, item.IsCurrent, item.EffectiveFrom, item.EffectiveTo, item.Reason, Convert.ToBase64String(item.RowVersion));
    private static InternalAuditAssessmentDto ToDto(InternalAuditAssessment item, InternalAuditMemberAccess access) => ToDto(item, item.Configuration.Model, access);
    private static InternalAuditAssessmentDto ToDto(InternalAuditAssessment item, InternalAuditAssessmentModel model, InternalAuditMemberAccess access) => new(item.PublicId, model, item.Outcome,
        access.ObservationRead ? item.DetailedObservation : null,
        access.CommentRead ? item.Comment : null,
        access.FindingsRead ? item.Findings : null,
        access.RecommendationRead ? item.Recommendation : null,
        access.ScoreRead ? item.Score : null,
        access.AssessedByRead ? item.AssessedByUser?.PublicId : null,
        access.AssessedByRead ? item.AssessedByUser?.FullName : null,
        item.AssessedAt, item.PreviousAssessment?.PublicId,
        access.RfiRead ? item.PerformanceRfi?.PublicId : null,
        access.RfiRead ? item.PerformanceRfi?.ResponseDueAt : null);
    private sealed record SubmissionAccess(AccessScopeContext Scope, SubmissionWorkflowInstance? Instance, string[] Recipients);
    private sealed record InternalAuditMemberAccess(bool ObservationRead, bool ObservationUpdate, bool CommentRead, bool CommentUpdate,
        bool FindingsRead, bool FindingsUpdate, bool RecommendationRead, bool RecommendationUpdate, bool ScoreRead, bool ScoreUpdate,
        bool AssessedByRead, bool RfiRead);
}

public sealed record SaveInternalAuditConfigurationRequest(Guid MunicipalityFinancialYearPublicId, InternalAuditAssessmentModel Model, DateTime EffectiveFrom, string Reason, string? CurrentRowVersion);
public sealed record InternalAuditConfigurationDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, InternalAuditAssessmentModel Model, int Version, bool IsCurrent, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string RowVersion);
public sealed record SaveInternalAuditAssessmentRequest(InternalAuditAssessmentOutcome Outcome, string DetailedObservation, string? Comment, string? Findings, string? Recommendation, decimal? Score, DateTime? ResponseDueAt, Guid? PreviousAssessmentPublicId);
public sealed record InternalAuditAssessmentDto(Guid PublicId, InternalAuditAssessmentModel Model, InternalAuditAssessmentOutcome Outcome, string? DetailedObservation, string? Comment, string? Findings, string? Recommendation, decimal? Score, Guid? AssessedByUserPublicId, string? AssessedByName, DateTime AssessedAt, Guid? PreviousAssessmentPublicId, Guid? RfiPublicId, DateTime? RfiResponseDueAt);
public sealed record InternalAuditSubmissionDto(InternalAuditConfigurationDto Configuration, InternalAuditAssessmentDto? LatestAssessment);

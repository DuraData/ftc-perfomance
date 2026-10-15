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
[Route("api/v1/strategic-risks")]
[Authorize]
public sealed class StrategicRiskOperationsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IAccessControlService accessControl,
    IWorkflowGovernanceService workflow,
    ITenantContext tenantContext) : ControllerBase
{
    private static readonly HashSet<string> TreatmentStates = ["OPEN", "IN_PROGRESS", "COMPLETED", "CANCELLED"];

    [HttpGet("owners/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicRiskOwnerOptionDto>>>> GetOwnersPage([FromQuery] PagedQueryRequest request)
    {
        var session = await SessionAsync<PagedResponse<StrategicRiskOwnerOptionDto>>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        if (!await CanMemberAsync(session.User!, "OwnerUserId", "READ"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<PagedResponse<StrategicRiskOwnerOptionDto>>("Strategic-risk owner read permission is required."));
        if (request.NormalizedSortBy is not ("fullname" or "email"))
            return BadRequest(Fail<PagedResponse<StrategicRiskOwnerOptionDto>>("SortBy must be fullName or email."));
        var query = context.Users.AsNoTracking().Where(item => item.MunicipalityId == tenantContext.MunicipalityId && item.IsActive);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.FirstName.Contains(request.NormalizedSearch) || item.LastName.Contains(request.NormalizedSearch)
                || item.Email!.Contains(request.NormalizedSearch));
        var total = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("email", false) => query.OrderBy(item => item.Email).ThenBy(item => item.PublicId),
            ("email", true) => query.OrderByDescending(item => item.Email).ThenBy(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.FirstName).ThenBy(item => item.LastName).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.FirstName).ThenByDescending(item => item.LastName).ThenBy(item => item.PublicId)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(item => new StrategicRiskOwnerOptionDto(item.PublicId, (item.FirstName + " " + item.LastName).Trim(), item.Email!)).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicRiskOwnerOptionDto>>(true,
            PagedResponse<StrategicRiskOwnerOptionDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpGet("assessments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicRiskAssessmentDto>>>> GetAssessmentsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? strategicRiskPublicId = null)
    {
        var session = await SessionAsync<PagedResponse<StrategicRiskAssessmentDto>>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        if (request.NormalizedSortBy is not ("assessedat" or "risktitle" or "inherent" or "residual"))
            return BadRequest(Fail<PagedResponse<StrategicRiskAssessmentDto>>("SortBy must be assessedAt, riskTitle, inherent, or residual."));
        var canReadControls = await CanMemberAsync(session.User!, "AssessmentControls", "READ");
        var query = context.StrategicRiskAssessments.AsNoTracking()
            .Include(item => item.StrategicRisk).Include(item => item.AssessedByUser).AsQueryable();
        if (strategicRiskPublicId.HasValue) query = query.Where(item => item.StrategicRisk.PublicId == strategicRiskPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.StrategicRisk.RiskTitle.Contains(request.NormalizedSearch)
                || (item.StrategicRisk.RiskReference != null && item.StrategicRisk.RiskReference.Contains(request.NormalizedSearch))
                || (canReadControls && item.ControlAssessment != null && item.ControlAssessment.Contains(request.NormalizedSearch)));
        var total = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("risktitle", false) => query.OrderBy(item => item.StrategicRisk.RiskTitle).ThenByDescending(item => item.AssessedAt),
            ("risktitle", true) => query.OrderByDescending(item => item.StrategicRisk.RiskTitle).ThenByDescending(item => item.AssessedAt),
            ("inherent", false) => query.OrderBy(item => item.InherentLikelihood * item.InherentImpact).ThenByDescending(item => item.AssessedAt),
            ("inherent", true) => query.OrderByDescending(item => item.InherentLikelihood * item.InherentImpact).ThenByDescending(item => item.AssessedAt),
            ("residual", false) => query.OrderBy(item => item.ResidualLikelihood * item.ResidualImpact).ThenByDescending(item => item.AssessedAt),
            ("residual", true) => query.OrderByDescending(item => item.ResidualLikelihood * item.ResidualImpact).ThenByDescending(item => item.AssessedAt),
            (_, false) => query.OrderBy(item => item.AssessedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.AssessedAt).ThenBy(item => item.PublicId)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicRiskAssessmentDto>>(true,
            PagedResponse<StrategicRiskAssessmentDto>.Create(rows.Select(item => AssessmentDto(item, canReadControls)), request.Page, request.PageSize, total)));
    }

    [HttpPost("assessments")]
    public async Task<ActionResult<ApiResponse<StrategicRiskAssessmentDto>>> RecordAssessment([FromBody] RecordStrategicRiskAssessmentRequest request)
    {
        var session = await SessionAsync<StrategicRiskAssessmentDto>("STRATEGIC_RISK.RECORD_ASSESSMENT");
        if (session.Error != null) return session.Error;
        var controls = request.ControlAssessment?.Trim();
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (!ValidScore(request.InherentLikelihood) || !ValidScore(request.InherentImpact)
            || !ValidScore(request.ResidualLikelihood) || !ValidScore(request.ResidualImpact))
            return BadRequest(Fail<StrategicRiskAssessmentDto>("Likelihood and impact values must be between 1 and 5."));
        if (request.AssessedAt == default || request.AssessedAt > DateTime.UtcNow.AddMinutes(5))
            return BadRequest(Fail<StrategicRiskAssessmentDto>("Assessment date is required and cannot be in the future."));
        if (reason.Length is < 10 or > 1000 || controls?.Length > 4000)
            return BadRequest(Fail<StrategicRiskAssessmentDto>("A substantive reason is required and assessment text must fit the allowed lengths."));
        if (controls != null && !await CanMemberAsync(session.User!, "AssessmentControls", "UPDATE"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskAssessmentDto>("Assessment-control update permission is required."));
        var risk = await context.StrategicRisks.SingleOrDefaultAsync(item => item.PublicId == request.StrategicRiskPublicId);
        if (risk == null) return NotFound(Fail<StrategicRiskAssessmentDto>("Strategic risk not found."));
        var entity = new StrategicRiskAssessment
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, StrategicRiskId = risk.Id,
            AssessedAt = request.AssessedAt.ToUniversalTime(), InherentLikelihood = request.InherentLikelihood,
            InherentImpact = request.InherentImpact, ResidualLikelihood = request.ResidualLikelihood,
            ResidualImpact = request.ResidualImpact, ControlAssessment = controls, Reason = reason,
            AssessedByUserId = session.User!.Id
        };
        context.StrategicRiskAssessments.Add(entity);
        workflow.QueueAuditTrail(nameof(StrategicRiskAssessment), entity.PublicId.ToString(), "RecordAssessment", null,
            new { risk.PublicId, entity.AssessedAt, entity.InherentLikelihood, entity.InherentImpact, entity.ResidualLikelihood, entity.ResidualImpact, entity.ControlAssessment },
            session.User.Id, IpAddress(), reason);
        await context.SaveChangesAsync();
        entity.StrategicRisk = risk; entity.AssessedByUser = session.User;
        return Ok(new ApiResponse<StrategicRiskAssessmentDto>(true, AssessmentDto(entity, await CanMemberAsync(session.User, "AssessmentControls", "READ"))));
    }

    [HttpGet("treatments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicRiskTreatmentDto>>>> GetTreatmentsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? strategicRiskPublicId = null, [FromQuery] string? status = null,
        [FromQuery] bool overdueOnly = false)
    {
        var session = await SessionAsync<PagedResponse<StrategicRiskTreatmentDto>>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        if (request.NormalizedSortBy is not ("dueat" or "risktitle" or "title" or "status" or "progress"))
            return BadRequest(Fail<PagedResponse<StrategicRiskTreatmentDto>>("SortBy must be dueAt, riskTitle, title, status, or progress."));
        var normalizedStatus = status?.Trim().ToUpperInvariant();
        if (normalizedStatus is { Length: > 0 } && !TreatmentStates.Contains(normalizedStatus))
            return BadRequest(Fail<PagedResponse<StrategicRiskTreatmentDto>>("Unknown treatment status."));
        var canReadDescription = await CanMemberAsync(session.User!, "TreatmentDescription", "READ");
        var canReadOwner = await CanMemberAsync(session.User!, "OwnerUserId", "READ");
        var query = context.StrategicRiskTreatments.AsNoTracking()
            .Include(item => item.StrategicRisk).Include(item => item.OwnerUser).AsQueryable();
        if (strategicRiskPublicId.HasValue) query = query.Where(item => item.StrategicRisk.PublicId == strategicRiskPublicId.Value);
        if (normalizedStatus is { Length: > 0 }) query = query.Where(item => item.Status == normalizedStatus);
        if (overdueOnly) query = query.Where(item => item.DueAt < DateTime.UtcNow && item.Status != "COMPLETED" && item.Status != "CANCELLED");
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Title.Contains(request.NormalizedSearch)
                || item.StrategicRisk.RiskTitle.Contains(request.NormalizedSearch)
                || (canReadOwner && (item.OwnerUser.FirstName.Contains(request.NormalizedSearch) || item.OwnerUser.LastName.Contains(request.NormalizedSearch)))
                || (canReadDescription && item.Description != null && item.Description.Contains(request.NormalizedSearch)));
        var total = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("risktitle", false) => query.OrderBy(item => item.StrategicRisk.RiskTitle).ThenBy(item => item.DueAt),
            ("risktitle", true) => query.OrderByDescending(item => item.StrategicRisk.RiskTitle).ThenBy(item => item.DueAt),
            ("title", false) => query.OrderBy(item => item.Title).ThenBy(item => item.DueAt),
            ("title", true) => query.OrderByDescending(item => item.Title).ThenBy(item => item.DueAt),
            ("status", false) => query.OrderBy(item => item.Status).ThenBy(item => item.DueAt),
            ("status", true) => query.OrderByDescending(item => item.Status).ThenBy(item => item.DueAt),
            ("progress", false) => query.OrderBy(item => item.ProgressPercent).ThenBy(item => item.DueAt),
            ("progress", true) => query.OrderByDescending(item => item.ProgressPercent).ThenBy(item => item.DueAt),
            (_, false) => query.OrderBy(item => item.DueAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.DueAt).ThenBy(item => item.PublicId)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicRiskTreatmentDto>>(true,
            PagedResponse<StrategicRiskTreatmentDto>.Create(rows.Select(item => TreatmentDto(item, canReadDescription, canReadOwner)), request.Page, request.PageSize, total)));
    }

    [HttpPost("treatments")]
    public async Task<ActionResult<ApiResponse<StrategicRiskTreatmentDto>>> CreateTreatment([FromBody] SaveStrategicRiskTreatmentRequest request)
    {
        var session = await SessionAsync<StrategicRiskTreatmentDto>("STRATEGIC_RISK.MANAGE_TREATMENT");
        if (session.Error != null) return session.Error;
        var validated = ValidateTreatment(request, false);
        if (validated.Error != null) return BadRequest(Fail<StrategicRiskTreatmentDto>(validated.Error));
        if (validated.Description != null && !await CanMemberAsync(session.User!, "TreatmentDescription", "UPDATE"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskTreatmentDto>("Treatment-description update permission is required."));
        if (!await CanMemberAsync(session.User!, "OwnerUserId", "UPDATE"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskTreatmentDto>("Treatment-owner update permission is required."));
        var risk = await context.StrategicRisks.SingleOrDefaultAsync(item => item.PublicId == request.StrategicRiskPublicId);
        var owner = await TenantUserAsync(request.OwnerUserPublicId);
        if (risk == null || owner == null) return BadRequest(Fail<StrategicRiskTreatmentDto>("Strategic risk and active municipality owner are required."));
        var entity = new StrategicRiskTreatment
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, StrategicRiskId = risk.Id, Title = validated.Title,
            Description = validated.Description, OwnerUserId = owner.Id, DueAt = request.DueAt.ToUniversalTime(),
            Status = validated.Status, ProgressPercent = request.ProgressPercent, CreatedByUserId = session.User!.Id
        };
        context.StrategicRiskTreatments.Add(entity);
        workflow.QueueAuditTrail(nameof(StrategicRiskTreatment), entity.PublicId.ToString(), "Create", null,
            new { risk.PublicId, entity.Title, entity.Description, OwnerUserPublicId = owner.PublicId, entity.DueAt, entity.Status, entity.ProgressPercent },
            session.User.Id, IpAddress(), validated.Reason);
        await context.SaveChangesAsync();
        entity.StrategicRisk = risk; entity.OwnerUser = owner;
        return Ok(new ApiResponse<StrategicRiskTreatmentDto>(true, TreatmentDto(entity,
            await CanMemberAsync(session.User, "TreatmentDescription", "READ"), await CanMemberAsync(session.User, "OwnerUserId", "READ"))));
    }

    [HttpPut("treatments/{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<StrategicRiskTreatmentDto>>> UpdateTreatment(Guid publicId, [FromBody] SaveStrategicRiskTreatmentRequest request)
    {
        var session = await SessionAsync<StrategicRiskTreatmentDto>("STRATEGIC_RISK.MANAGE_TREATMENT");
        if (session.Error != null) return session.Error;
        var validated = ValidateTreatment(request, true);
        if (validated.Error != null) return BadRequest(Fail<StrategicRiskTreatmentDto>(validated.Error));
        if (!TryVersion(request.RowVersion, out var version)) return BadRequest(Fail<StrategicRiskTreatmentDto>("A valid RowVersion is required."));
        var entity = await context.StrategicRiskTreatments.Include(item => item.StrategicRisk).SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<StrategicRiskTreatmentDto>("Risk treatment not found."));
        if (entity.Status is "COMPLETED" or "CANCELLED") return Conflict(Fail<StrategicRiskTreatmentDto>("A terminal treatment is immutable; create a successor action if more work is required."));
        if (request.StrategicRiskPublicId != entity.StrategicRisk.PublicId) return BadRequest(Fail<StrategicRiskTreatmentDto>("A treatment cannot be moved to another risk."));
        var owner = await TenantUserAsync(request.OwnerUserPublicId);
        if (owner == null) return BadRequest(Fail<StrategicRiskTreatmentDto>("An active municipality owner is required."));
        if (!string.Equals(entity.OwnerUserId, owner.Id, StringComparison.Ordinal)
            && !await CanMemberAsync(session.User!, "OwnerUserId", "UPDATE"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskTreatmentDto>("Treatment-owner update permission is required."));
        if (!string.Equals(entity.Description, validated.Description, StringComparison.Ordinal)
            && !await CanMemberAsync(session.User!, "TreatmentDescription", "UPDATE"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskTreatmentDto>("Treatment-description update permission is required."));
        var previousOwnerPublicId = await context.Users.Where(item => item.Id == entity.OwnerUserId)
            .Select(item => item.PublicId).SingleAsync();
        var before = new
        {
            entity.Title, entity.Description, OwnerUserPublicId = previousOwnerPublicId,
            entity.DueAt, entity.Status, entity.ProgressPercent
        };
        context.Entry(entity).Property(item => item.RowVersion).OriginalValue = version;
        entity.Title = validated.Title; entity.Description = validated.Description; entity.OwnerUserId = owner.Id;
        entity.DueAt = request.DueAt.ToUniversalTime(); entity.Status = validated.Status; entity.ProgressPercent = request.ProgressPercent;
        entity.UpdatedByUserId = session.User!.Id; entity.UpdatedAt = DateTime.UtcNow;
        workflow.QueueAuditTrail(nameof(StrategicRiskTreatment), entity.PublicId.ToString(), "Update", before,
            new { entity.Title, entity.Description, OwnerUserPublicId = owner.PublicId, entity.DueAt, entity.Status, entity.ProgressPercent },
            session.User.Id, IpAddress(), validated.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicRiskTreatmentDto>("The risk treatment changed before this update. Reload and retry.")); }
        entity.OwnerUser = owner;
        return Ok(new ApiResponse<StrategicRiskTreatmentDto>(true, TreatmentDto(entity,
            await CanMemberAsync(session.User, "TreatmentDescription", "READ"), await CanMemberAsync(session.User, "OwnerUserId", "READ"))));
    }

    [HttpGet("reviews/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicRiskReviewDto>>>> GetReviewsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? strategicRiskPublicId = null)
    {
        var session = await SessionAsync<PagedResponse<StrategicRiskReviewDto>>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        if (request.NormalizedSortBy is not ("reviewedat" or "nextreviewat" or "risktitle"))
            return BadRequest(Fail<PagedResponse<StrategicRiskReviewDto>>("SortBy must be reviewedAt, nextReviewAt, or riskTitle."));
        var canReadOutcome = await CanMemberAsync(session.User!, "ReviewOutcome", "READ");
        var query = context.StrategicRiskReviews.AsNoTracking().Include(item => item.StrategicRisk).Include(item => item.ReviewedByUser).AsQueryable();
        if (strategicRiskPublicId.HasValue) query = query.Where(item => item.StrategicRisk.PublicId == strategicRiskPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.StrategicRisk.RiskTitle.Contains(request.NormalizedSearch)
                || (canReadOutcome && item.Outcome.Contains(request.NormalizedSearch)));
        var total = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("nextreviewat", false) => query.OrderBy(item => item.NextReviewAt).ThenByDescending(item => item.ReviewedAt),
            ("nextreviewat", true) => query.OrderByDescending(item => item.NextReviewAt).ThenByDescending(item => item.ReviewedAt),
            ("risktitle", false) => query.OrderBy(item => item.StrategicRisk.RiskTitle).ThenByDescending(item => item.ReviewedAt),
            ("risktitle", true) => query.OrderByDescending(item => item.StrategicRisk.RiskTitle).ThenByDescending(item => item.ReviewedAt),
            (_, false) => query.OrderBy(item => item.ReviewedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.ReviewedAt).ThenBy(item => item.PublicId)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicRiskReviewDto>>(true,
            PagedResponse<StrategicRiskReviewDto>.Create(rows.Select(item => ReviewDto(item, canReadOutcome)), request.Page, request.PageSize, total)));
    }

    [HttpPost("reviews")]
    public async Task<ActionResult<ApiResponse<StrategicRiskReviewDto>>> RecordReview([FromBody] RecordStrategicRiskReviewRequest request)
    {
        var session = await SessionAsync<StrategicRiskReviewDto>("STRATEGIC_RISK.RECORD_REVIEW");
        if (session.Error != null) return session.Error;
        var outcome = request.Outcome?.Trim() ?? string.Empty;
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (request.ReviewedAt == default || request.ReviewedAt > DateTime.UtcNow.AddMinutes(5)
            || request.NextReviewAt.HasValue && request.NextReviewAt.Value <= request.ReviewedAt)
            return BadRequest(Fail<StrategicRiskReviewDto>("Review date is required, cannot be in the future, and the next review must be later."));
        if (outcome.Length is < 3 or > 4000 || reason.Length is < 10 or > 1000)
            return BadRequest(Fail<StrategicRiskReviewDto>("A review outcome and substantive governance reason are required."));
        if (!await CanMemberAsync(session.User!, "ReviewOutcome", "UPDATE"))
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskReviewDto>("Review-outcome update permission is required."));
        var risk = await context.StrategicRisks.SingleOrDefaultAsync(item => item.PublicId == request.StrategicRiskPublicId);
        if (risk == null) return NotFound(Fail<StrategicRiskReviewDto>("Strategic risk not found."));
        var entity = new StrategicRiskReview
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, StrategicRiskId = risk.Id,
            ReviewedAt = request.ReviewedAt.ToUniversalTime(), Outcome = outcome,
            NextReviewAt = request.NextReviewAt?.ToUniversalTime(), Reason = reason, ReviewedByUserId = session.User!.Id
        };
        context.StrategicRiskReviews.Add(entity);
        workflow.QueueAuditTrail(nameof(StrategicRiskReview), entity.PublicId.ToString(), "RecordReview", null,
            new { risk.PublicId, entity.ReviewedAt, entity.Outcome, entity.NextReviewAt }, session.User.Id, IpAddress(), reason);
        await context.SaveChangesAsync();
        entity.StrategicRisk = risk; entity.ReviewedByUser = session.User;
        return Ok(new ApiResponse<StrategicRiskReviewDto>(true, ReviewDto(entity, await CanMemberAsync(session.User, "ReviewOutcome", "READ"))));
    }

    [HttpGet("heatmap")]
    public async Task<ActionResult<ApiResponse<StrategicRiskHeatmapDto>>> GetHeatmap()
    {
        var session = await SessionAsync<StrategicRiskHeatmapDto>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        var latest = await context.StrategicRiskAssessments.AsNoTracking()
            .Where(item => item.StrategicRisk.IsActive && !context.StrategicRiskAssessments.Any(other =>
                other.StrategicRiskId == item.StrategicRiskId && (other.AssessedAt > item.AssessedAt
                    || other.AssessedAt == item.AssessedAt && other.Id > item.Id)))
            .Select(item => new { item.ResidualLikelihood, item.ResidualImpact, item.StrategicRiskId }).ToArrayAsync();
        var cells = latest.GroupBy(item => new { item.ResidualLikelihood, item.ResidualImpact })
            .Select(group => new StrategicRiskHeatmapCellDto(group.Key.ResidualLikelihood, group.Key.ResidualImpact, group.Count()))
            .OrderBy(item => item.Likelihood).ThenBy(item => item.Impact).ToArray();
        var active = await context.StrategicRisks.CountAsync(item => item.IsActive);
        var openTreatments = await context.StrategicRiskTreatments.CountAsync(item => item.Status == "OPEN" || item.Status == "IN_PROGRESS");
        var overdueTreatments = await context.StrategicRiskTreatments.CountAsync(item => item.DueAt < DateTime.UtcNow && item.Status != "COMPLETED" && item.Status != "CANCELLED");
        return Ok(new ApiResponse<StrategicRiskHeatmapDto>(true,
            new(cells, active, latest.Select(item => item.StrategicRiskId).Distinct().Count(), active - latest.Select(item => item.StrategicRiskId).Distinct().Count(), openTreatments, overdueTreatments)));
    }

    private async Task<(ApplicationUser? User, ActionResult? Error)> SessionAsync<T>(string permission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId == null ? null : await userManager.FindByIdAsync(userId);
        if (user == null) return (null, Unauthorized(Fail<T>("User not found.")));
        if (!tenantContext.MunicipalityId.HasValue) return (user, BadRequest(Fail<T>("Municipality context is required.")));
        var decision = await accessControl.CheckPermissionAsync(user, permission, MunicipalityScope());
        return decision.Allowed ? (user, null) : (user, StatusCode(StatusCodes.Status403Forbidden, Fail<T>(decision.Reason)));
    }

    private async Task<bool> CanMemberAsync(ApplicationUser user, string member, string operation) =>
        (await accessControl.CheckPermissionAsync(user, $"STRATEGIC_RISK.{member}.{operation}", MunicipalityScope())).Allowed;

    private Task<ApplicationUser?> TenantUserAsync(Guid publicId) => context.Users.SingleOrDefaultAsync(item =>
        item.PublicId == publicId && item.MunicipalityId == tenantContext.MunicipalityId && item.IsActive);

    private static bool ValidScore(int value) => value is >= 1 and <= 5;

    private static (string Title, string? Description, string Status, string Reason, string? Error) ValidateTreatment(
        SaveStrategicRiskTreatmentRequest request, bool update)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        var description = request.Description?.Trim();
        var status = request.Status?.Trim().ToUpperInvariant() ?? string.Empty;
        var reason = request.Reason?.Trim() ?? string.Empty;
        var error = title.Length is < 3 or > 500 ? "Treatment title must contain 3 to 500 characters."
            : description?.Length > 4000 ? "Treatment description cannot exceed 4000 characters."
            : request.DueAt == default ? "Treatment due date is required."
            : !TreatmentStates.Contains(status) ? "Unknown treatment status."
            : request.ProgressPercent is < 0 or > 100 ? "Treatment progress must be between 0 and 100."
            : status == "COMPLETED" && request.ProgressPercent != 100 ? "Completed treatments must record 100 percent progress."
            : !update && (status is "COMPLETED" or "CANCELLED") ? "A new treatment must start open or in progress."
            : reason.Length is < 10 or > 1000 ? "A substantive governance reason is required."
            : null;
        return (title, description, status, reason, error);
    }

    private static bool TryVersion(string? value, out byte[] version)
    {
        try { version = Convert.FromBase64String(value ?? string.Empty); return version.Length > 0; }
        catch (FormatException) { version = []; return false; }
    }

    private static StrategicRiskAssessmentDto AssessmentDto(StrategicRiskAssessment item, bool controls) => new(
        item.PublicId, item.StrategicRisk.PublicId, item.StrategicRisk.RiskReference, item.StrategicRisk.RiskTitle,
        item.AssessedAt, item.InherentLikelihood, item.InherentImpact, item.InherentLikelihood * item.InherentImpact,
        item.ResidualLikelihood, item.ResidualImpact, item.ResidualLikelihood * item.ResidualImpact,
        controls ? item.ControlAssessment : null, item.AssessedByUser.PublicId,
        $"{item.AssessedByUser.FirstName} {item.AssessedByUser.LastName}".Trim(), item.CreatedAt);

    private static StrategicRiskTreatmentDto TreatmentDto(StrategicRiskTreatment item, bool description, bool owner) => new(
        item.PublicId, item.StrategicRisk.PublicId, item.StrategicRisk.RiskReference, item.StrategicRisk.RiskTitle,
        item.Title, description ? item.Description : null, owner ? item.OwnerUser.PublicId : null,
        owner ? $"{item.OwnerUser.FirstName} {item.OwnerUser.LastName}".Trim() : null, item.DueAt, item.Status,
        item.ProgressPercent, item.CreatedAt, item.UpdatedAt, Convert.ToBase64String(item.RowVersion));

    private static StrategicRiskReviewDto ReviewDto(StrategicRiskReview item, bool outcome) => new(
        item.PublicId, item.StrategicRisk.PublicId, item.StrategicRisk.RiskReference, item.StrategicRisk.RiskTitle,
        item.ReviewedAt, outcome ? item.Outcome : null, item.NextReviewAt, item.ReviewedByUser.PublicId,
        $"{item.ReviewedByUser.FirstName} {item.ReviewedByUser.LastName}".Trim(), item.CreatedAt);

    private AccessScopeContext MunicipalityScope() => new(MunicipalityId: tenantContext.MunicipalityId);
    private string? IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
}

public sealed record RecordStrategicRiskAssessmentRequest(Guid StrategicRiskPublicId, DateTime AssessedAt,
    int InherentLikelihood, int InherentImpact, int ResidualLikelihood, int ResidualImpact,
    string? ControlAssessment, string Reason);
public sealed record StrategicRiskAssessmentDto(Guid PublicId, Guid StrategicRiskPublicId, string? RiskReference,
    string RiskTitle, DateTime AssessedAt, int InherentLikelihood, int InherentImpact, int InherentScore,
    int ResidualLikelihood, int ResidualImpact, int ResidualScore, string? ControlAssessment,
    Guid AssessedByUserPublicId, string AssessedByName, DateTime CreatedAt);
public sealed record SaveStrategicRiskTreatmentRequest(Guid StrategicRiskPublicId, string Title, string? Description,
    Guid OwnerUserPublicId, DateTime DueAt, string Status, int ProgressPercent, string Reason, string? RowVersion);
public sealed record StrategicRiskTreatmentDto(Guid PublicId, Guid StrategicRiskPublicId, string? RiskReference,
    string RiskTitle, string Title, string? Description, Guid? OwnerUserPublicId, string? OwnerName,
    DateTime DueAt, string Status, int ProgressPercent, DateTime CreatedAt, DateTime? UpdatedAt, string RowVersion);
public sealed record RecordStrategicRiskReviewRequest(Guid StrategicRiskPublicId, DateTime ReviewedAt,
    string Outcome, DateTime? NextReviewAt, string Reason);
public sealed record StrategicRiskReviewDto(Guid PublicId, Guid StrategicRiskPublicId, string? RiskReference,
    string RiskTitle, DateTime ReviewedAt, string? Outcome, DateTime? NextReviewAt,
    Guid ReviewedByUserPublicId, string ReviewedByName, DateTime CreatedAt);
public sealed record StrategicRiskHeatmapCellDto(int Likelihood, int Impact, int Count);
public sealed record StrategicRiskHeatmapDto(IReadOnlyCollection<StrategicRiskHeatmapCellDto> Cells,
    int ActiveRisks, int AssessedRisks, int UnassessedRisks, int OpenTreatments, int OverdueTreatments);
public sealed record StrategicRiskOwnerOptionDto(Guid PublicId, string FullName, string Email);

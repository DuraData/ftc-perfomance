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
[Route("api/v1/workflow/rating-reconciliation")]
[Authorize]
[Authorize(Policy = "Permission:WORKFLOW.RECONCILE_LEGACY_RATINGS")]
public sealed class LegacyRatingReconciliationController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IWorkflowGovernanceService governance) : ControllerBase
{
    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LegacyRatingReconciliationPreviewDto>>>> Preview(
        [FromQuery] SubmissionKind submissionKind,
        [FromQuery] LegacyRatingSourceKind sourceKind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (!tenantContext.MunicipalityId.HasValue)
            return BadRequest(Fail<PagedResponse<LegacyRatingReconciliationPreviewDto>>("A municipality context is required."));
        if (submissionKind is not (SubmissionKind.Opms or SubmissionKind.Ipms) || !Enum.IsDefined(sourceKind))
            return BadRequest(Fail<PagedResponse<LegacyRatingReconciliationPreviewDto>>("A supported submission and legacy score source kind are required."));
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var loaded = await LoadSources(submissionKind, sourceKind, page, pageSize);
        var results = new List<LegacyRatingReconciliationPreviewDto>(loaded.Sources.Count);
        foreach (var source in loaded.Sources)
            results.Add(await Analyze(source));

        return Ok(new ApiResponse<PagedResponse<LegacyRatingReconciliationPreviewDto>>(true,
            PagedResponse<LegacyRatingReconciliationPreviewDto>.Create(results, page, pageSize, loaded.TotalCount)));
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<LegacyRatingReconciliationResultDto>>> Execute(ReconcileLegacyRatingRequest request)
    {
        if (!tenantContext.MunicipalityId.HasValue)
            return BadRequest(Fail<LegacyRatingReconciliationResultDto>("A municipality context is required."));
        if (request.SubmissionKind is not (SubmissionKind.Opms or SubmissionKind.Ipms)
            || !Enum.IsDefined(request.SourceKind)
            || string.IsNullOrWhiteSpace(request.SourceKey)
            || request.SourceKey.Length > 550
            || request.WorkflowActionPublicId == Guid.Empty)
            return BadRequest(Fail<LegacyRatingReconciliationResultDto>("A supported submission, source key, source kind and explicit workflow action are required."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10 || reason.Length > 1000)
            return BadRequest(Fail<LegacyRatingReconciliationResultDto>("A reconciliation reason of 10 to 1000 characters is required."));
        var actor = await userManager.GetUserAsync(User);
        if (actor == null) return Unauthorized(Fail<LegacyRatingReconciliationResultDto>("User not found."));
        var source = await LoadSource(request.SubmissionKind, request.SourceKind, request.SourceKey);
        if (source == null) return NotFound(Fail<LegacyRatingReconciliationResultDto>("The legacy rating source was not found in the selected municipality."));
        if (await context.LegacySubmissionRatingReconciliations.AnyAsync(x => x.SourceKey == source.SourceKey))
            return Conflict(Fail<LegacyRatingReconciliationResultDto>("This legacy rating source has already been reconciled."));

        var action = await context.SubmissionWorkflowActions
            .Include(x => x.ActorUser)
            .Include(x => x.SubmissionWorkflowInstance)
            .Include(x => x.FromStage).ThenInclude(x => x!.RatingScheme).ThenInclude(x => x!.Values)
            .SingleOrDefaultAsync(x => x.PublicId == request.WorkflowActionPublicId);
        if (action?.FromStage?.RatingScheme == null
            || action.MunicipalityId != tenantContext.MunicipalityId
            || action.SubmissionWorkflowInstance.SubmissionKind != source.SubmissionKind
            || !string.Equals(action.SubmissionWorkflowInstance.SubmissionId, source.SubmissionId, StringComparison.Ordinal)
            || !string.Equals(action.ActorUserId, source.ActorUserId, StringComparison.Ordinal))
            return Conflict(Fail<LegacyRatingReconciliationResultDto>("The selected action is not a same-submission, same-actor configured rating candidate."));
        var matchingValues = action.FromStage.RatingScheme.Values.Where(x => x.Value == source.Score).Take(2).ToArray();
        if (matchingValues.Length != 1)
            return Conflict(Fail<LegacyRatingReconciliationResultDto>("The legacy score does not identify exactly one configured value in the selected action's stage rating scheme."));
        var value = matchingValues[0];

        var existingRating = await context.SubmissionStageRatings.SingleOrDefaultAsync(x => x.SubmissionWorkflowActionId == action.Id);
        if (existingRating != null && (existingRating.RatingSchemeValueId != value.Id || existingRating.Value != source.Score))
            return Conflict(Fail<LegacyRatingReconciliationResultDto>("The selected action already has different canonical rating evidence."));

        await using var transaction = await context.Database.BeginTransactionAsync();
        var usedExisting = existingRating != null;
        var rating = existingRating ?? new SubmissionStageRating
        {
            MunicipalityId = tenantContext.MunicipalityId.Value,
            SubmissionWorkflowInstanceId = action.SubmissionWorkflowInstanceId,
            SubmissionWorkflowInstance = action.SubmissionWorkflowInstance,
            SubmissionWorkflowActionId = action.Id,
            SubmissionWorkflowAction = action,
            WorkflowStageDefinitionId = action.FromStage.Id,
            WorkflowStageDefinition = action.FromStage,
            RatingSchemeId = action.FromStage.RatingScheme.Id,
            RatingScheme = action.FromStage.RatingScheme,
            RatingSchemeValueId = value.Id,
            RatingSchemeValue = value,
            Value = value.Value,
            LabelSnapshot = value.Label,
            AchievementPercent = null,
            Comment = source.Comment,
            RatedByUserId = action.ActorUserId,
            RatedByUser = action.ActorUser,
            RatedAt = action.OccurredAt,
            CorrelationId = $"legacy-rating:{source.SourceKey}"[..Math.Min(100, $"legacy-rating:{source.SourceKey}".Length)]
        };
        if (!usedExisting) context.SubmissionStageRatings.Add(rating);

        var reconciliation = new LegacySubmissionRatingReconciliation
        {
            MunicipalityId = tenantContext.MunicipalityId.Value,
            SubmissionKind = source.SubmissionKind,
            SubmissionId = source.SubmissionId,
            SourceKind = source.SourceKind,
            SourceKey = source.SourceKey,
            SourceScore = source.Score,
            SourceActorUserIdSnapshot = source.ActorUserId,
            SourceOccurredAtSnapshot = source.OccurredAt,
            SourceCommentSnapshot = source.Comment,
            SubmissionWorkflowInstanceId = action.SubmissionWorkflowInstanceId,
            SubmissionWorkflowInstance = action.SubmissionWorkflowInstance,
            SubmissionWorkflowActionId = action.Id,
            SubmissionWorkflowAction = action,
            WorkflowStageDefinitionId = action.FromStage.Id,
            WorkflowStageDefinition = action.FromStage,
            RatingSchemeId = action.FromStage.RatingScheme.Id,
            RatingScheme = action.FromStage.RatingScheme,
            RatingSchemeValueId = value.Id,
            RatingSchemeValue = value,
            SubmissionStageRating = rating,
            UsedExistingRating = usedExisting,
            ReconciledByUserId = actor.Id,
            ReconciledAt = DateTime.UtcNow,
            Reason = reason,
            CorrelationId = HttpContext.TraceIdentifier[..Math.Min(100, HttpContext.TraceIdentifier.Length)]
        };
        context.LegacySubmissionRatingReconciliations.Add(reconciliation);
        governance.QueueAuditTrailAt(nameof(LegacySubmissionRatingReconciliation), reconciliation.PublicId.ToString(), "Reconcile",
            null, new { source.SubmissionPublicId, source.SourceKind, source.SourceKey, source.Score, WorkflowActionPublicId = action.PublicId, StagePublicId = action.FromStage.PublicId, RatingValuePublicId = value.PublicId, UsedExistingRating = usedExisting },
            actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext), reason, reconciliation.ReconciledAt);
        try
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(Fail<LegacyRatingReconciliationResultDto>("The source or workflow rating was reconciled concurrently. Refresh the preview."));
        }

        return Ok(new ApiResponse<LegacyRatingReconciliationResultDto>(true,
            new(reconciliation.PublicId, rating.PublicId, usedExisting)));
    }

    private async Task<LegacyRatingReconciliationPreviewDto> Analyze(LegacySource source)
    {
        var reconciled = await context.LegacySubmissionRatingReconciliations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SourceKey == source.SourceKey);
        if (reconciled != null)
            return Preview(source, "Reconciled", "The source is linked to immutable canonical rating evidence.", [], reconciled.PublicId);

        var instance = await context.SubmissionWorkflowInstances.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubmissionKind == source.SubmissionKind && x.SubmissionId == source.SubmissionId);
        if (instance == null)
            return Preview(source, "Blocked", "No configured workflow instance exists for this historical submission.", [], null);

        var actions = await context.SubmissionWorkflowActions.AsNoTracking()
            .Where(x => x.SubmissionWorkflowInstanceId == instance.Id && x.ActorUserId == source.ActorUserId && x.FromStageId != null)
            .Include(x => x.ActorUser)
            .Include(x => x.FromStage).ThenInclude(x => x!.RatingScheme).ThenInclude(x => x!.Values)
            .OrderBy(x => x.Sequence).ToArrayAsync();
        var actionIds = actions.Select(x => x.Id).ToArray();
        var ratings = await context.SubmissionStageRatings.AsNoTracking()
            .Where(x => actionIds.Contains(x.SubmissionWorkflowActionId))
            .ToDictionaryAsync(x => x.SubmissionWorkflowActionId);
        var candidates = actions.Select(action => Candidate(action, source, ratings.GetValueOrDefault(action.Id)))
            .Where(candidate => candidate != null).Select(candidate => candidate!).ToArray();
        if (candidates.Length == 0)
            return Preview(source, "Blocked", "No same-submission, same-actor workflow action has a stage scheme containing this exact value without conflicting canonical evidence.", [], null);
        return candidates.Length == 1
            ? Preview(source, "Ready", "One exact configured action candidate is available; execution still requires explicit administrator confirmation and reason.", candidates, null)
            : Preview(source, "RequiresMapping", "Multiple exact configured action candidates exist; an administrator must select the evidenced action explicitly.", candidates, null);
    }

    private static LegacyRatingCandidateDto? Candidate(SubmissionWorkflowAction action, LegacySource source, SubmissionStageRating? rating)
    {
        var stage = action.FromStage;
        var scheme = stage?.RatingScheme;
        var values = scheme?.Values.Where(x => x.Value == source.Score).Take(2).ToArray() ?? [];
        if (stage == null || scheme == null || values.Length != 1) return null;
        var value = values[0];
        if (rating != null && (rating.RatingSchemeValueId != value.Id || rating.Value != source.Score)) return null;
        return new(action.PublicId, action.ActionCode, stage.Code, stage.Name, scheme.PublicId, scheme.Code,
            value.PublicId, value.Value, value.Label, DisplayName(action.ActorUser), action.OccurredAt, rating != null);
    }

    private async Task<(int TotalCount, IReadOnlyList<LegacySource> Sources)> LoadSources(
        SubmissionKind submissionKind, LegacyRatingSourceKind sourceKind, int page, int pageSize)
    {
        if (sourceKind == LegacyRatingSourceKind.GenericSubmissionScore)
        {
            var query = context.SubmissionScores.AsNoTracking().Where(x => x.SubmissionKind == submissionKind);
            query = submissionKind == SubmissionKind.Opms
                ? query.Where(x => context.OpmsSubmissions.Any(s => s.Id == x.SubmissionId))
                : query.Where(x => context.IpmsSubmissions.Any(s => s.Id == x.SubmissionId));
            var total = await query.CountAsync();
            var scores = await query.Include(x => x.ScoredByUser).OrderBy(x => x.ScoredAt).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
            var submissionIds = scores.Select(x => x.SubmissionId).Distinct().ToArray();
            var publicIds = submissionKind == SubmissionKind.Opms
                ? await context.OpmsSubmissions.AsNoTracking().Where(x => submissionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.PublicId)
                : await context.IpmsSubmissions.AsNoTracking().Where(x => submissionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.PublicId);
            return (total, scores.Select(x => new LegacySource(submissionKind, publicIds[x.SubmissionId], x.SubmissionId,
                sourceKind, $"GENERIC:{x.Id}", x.Score, x.ScoredByUserId, DisplayName(x.ScoredByUser), x.ScoredAt, x.Notes)).ToArray());
        }

        return submissionKind == SubmissionKind.Opms
            ? await LoadEmbeddedOpms(sourceKind, page, pageSize)
            : await LoadEmbeddedIpms(sourceKind, page, pageSize);
    }

    private async Task<(int TotalCount, IReadOnlyList<LegacySource> Sources)> LoadEmbeddedOpms(LegacyRatingSourceKind sourceKind, int page, int pageSize)
    {
        IQueryable<OpmsSubmission> query = sourceKind switch
        {
            LegacyRatingSourceKind.SubmitterScore => context.OpmsSubmissions.Where(x => x.SubmitterScore != null),
            LegacyRatingSourceKind.VerifierScore => context.OpmsSubmissions.Where(x => x.VerifierScore != null),
            LegacyRatingSourceKind.ApproverScore => context.OpmsSubmissions.Where(x => x.ApproverScore != null),
            LegacyRatingSourceKind.PmsScore => context.OpmsSubmissions.Where(x => x.PmsScore != null),
            LegacyRatingSourceKind.AuditorScore => context.OpmsSubmissions.Where(x => x.AuditorScore != null),
            _ => context.OpmsSubmissions.Where(_ => false)
        };
        var total = await query.CountAsync();
        var rows = await query.AsNoTracking().OrderBy(x => x.PublicId).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
        return (total, await AttachActorNames(rows.Select(x => Embedded(x, sourceKind)!).ToArray()));
    }

    private async Task<(int TotalCount, IReadOnlyList<LegacySource> Sources)> LoadEmbeddedIpms(LegacyRatingSourceKind sourceKind, int page, int pageSize)
    {
        IQueryable<IpmsSubmission> query = sourceKind switch
        {
            LegacyRatingSourceKind.SubmitterScore => context.IpmsSubmissions.Where(x => x.SubmitterScore != null),
            LegacyRatingSourceKind.VerifierScore => context.IpmsSubmissions.Where(x => x.VerifierScore != null),
            LegacyRatingSourceKind.ApproverScore => context.IpmsSubmissions.Where(x => x.ApproverScore != null),
            LegacyRatingSourceKind.PmsScore => context.IpmsSubmissions.Where(x => x.PmsScore != null),
            LegacyRatingSourceKind.AuditorScore => context.IpmsSubmissions.Where(x => x.AuditorScore != null),
            _ => context.IpmsSubmissions.Where(_ => false)
        };
        var total = await query.CountAsync();
        var rows = await query.AsNoTracking().OrderBy(x => x.PublicId).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
        return (total, await AttachActorNames(rows.Select(x => Embedded(x, sourceKind)!).ToArray()));
    }

    private async Task<IReadOnlyList<LegacySource>> AttachActorNames(LegacySource[] sources)
    {
        var ids = sources.Select(x => x.ActorUserId).Where(x => x.Length > 0).Distinct().ToArray();
        var names = await context.Users.AsNoTracking().Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => (x.FirstName + " " + x.LastName).Trim());
        return sources.Select(x => x with { ActorName = names.GetValueOrDefault(x.ActorUserId, "Unavailable historical actor") }).ToArray();
    }

    private async Task<LegacySource?> LoadSource(SubmissionKind submissionKind, LegacyRatingSourceKind sourceKind, string sourceKey)
    {
        if (sourceKind == LegacyRatingSourceKind.GenericSubmissionScore && sourceKey.StartsWith("GENERIC:", StringComparison.Ordinal))
        {
            var id = sourceKey[8..];
            var score = await context.SubmissionScores.AsNoTracking().Include(x => x.ScoredByUser)
                .SingleOrDefaultAsync(x => x.Id == id && x.SubmissionKind == submissionKind);
            if (score == null) return null;
            var publicId = submissionKind == SubmissionKind.Opms
                ? await context.OpmsSubmissions.Where(x => x.Id == score.SubmissionId).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync()
                : await context.IpmsSubmissions.Where(x => x.Id == score.SubmissionId).Select(x => (Guid?)x.PublicId).SingleOrDefaultAsync();
            return publicId.HasValue ? new(submissionKind, publicId.Value, score.SubmissionId, sourceKind, sourceKey,
                score.Score, score.ScoredByUserId, DisplayName(score.ScoredByUser), score.ScoredAt, score.Notes) : null;
        }

        var parts = sourceKey.Split(':');
        if (parts.Length != 3 || !Guid.TryParseExact(parts[1], "N", out var submissionPublicId)
            || !string.Equals(parts[0], submissionKind.ToString(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[2], sourceKind.ToString(), StringComparison.Ordinal)) return null;
        LegacySource? source;
        if (submissionKind == SubmissionKind.Opms)
        {
            var submission = await context.OpmsSubmissions.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == submissionPublicId);
            source = submission == null ? null : Embedded(submission, sourceKind);
        }
        else
        {
            var submission = await context.IpmsSubmissions.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == submissionPublicId);
            source = submission == null ? null : Embedded(submission, sourceKind);
        }
        if (source == null) return null;
        return (await AttachActorNames([source])).Single();
    }

    private static LegacySource? Embedded(OpmsSubmission x, LegacyRatingSourceKind kind)
    {
        var value = EmbeddedValues(kind, x.SubmitterScore, x.SubmittedByUserId, x.SubmittedAt, null,
            x.VerifierScore, x.VerifierUserId, x.VerifiedAt, x.VerifierComments,
            x.ApproverScore, x.ApproverUserId, x.ApprovedAt, x.ApproverComments,
            x.PmsScore, x.PmsOfficerUserId, x.PmsReviewedAt, x.PmsComments,
            x.AuditorScore, x.AuditorUserId, x.AuditedAt, x.AuditorComments);
        return value.Score.HasValue
            ? new(SubmissionKind.Opms, x.PublicId, x.Id, kind, $"OPMS:{x.PublicId:N}:{kind}", value.Score.Value, value.Actor ?? string.Empty, string.Empty, value.OccurredAt, value.Comment)
            : null;
    }

    private static LegacySource? Embedded(IpmsSubmission x, LegacyRatingSourceKind kind)
    {
        var value = EmbeddedValues(kind, x.SubmitterScore, x.SubmittedByUserId, x.SubmittedAt, null,
            x.VerifierScore, x.VerifierUserId, x.VerifiedAt, x.VerifierComments,
            x.ApproverScore, x.ApproverUserId, x.ApprovedAt, x.ApproverComments,
            x.PmsScore, x.PmsOfficerUserId, x.PmsReviewedAt, x.PmsComments,
            x.AuditorScore, x.AuditorUserId, x.AuditedAt, x.AuditorComments);
        return value.Score.HasValue
            ? new(SubmissionKind.Ipms, x.PublicId, x.Id, kind, $"IPMS:{x.PublicId:N}:{kind}", value.Score.Value, value.Actor ?? string.Empty, string.Empty, value.OccurredAt, value.Comment)
            : null;
    }

    private static (decimal? Score, string? Actor, DateTime? OccurredAt, string? Comment) EmbeddedValues(
        LegacyRatingSourceKind kind,
        decimal? submitterScore, string? submitter, DateTime? submittedAt, string? submitterComment,
        decimal? verifierScore, string? verifier, DateTime? verifiedAt, string? verifierComment,
        decimal? approverScore, string? approver, DateTime? approvedAt, string? approverComment,
        decimal? pmsScore, string? pms, DateTime? pmsAt, string? pmsComment,
        decimal? auditorScore, string? auditor, DateTime? auditedAt, string? auditorComment) => kind switch
    {
        LegacyRatingSourceKind.SubmitterScore => (submitterScore ?? 0, submitter, submittedAt, submitterComment),
        LegacyRatingSourceKind.VerifierScore => (verifierScore ?? 0, verifier, verifiedAt, verifierComment),
        LegacyRatingSourceKind.ApproverScore => (approverScore ?? 0, approver, approvedAt, approverComment),
        LegacyRatingSourceKind.PmsScore => (pmsScore ?? 0, pms, pmsAt, pmsComment),
        LegacyRatingSourceKind.AuditorScore => (auditorScore ?? 0, auditor, auditedAt, auditorComment),
        _ => (null, null, null, null)
    };

    private static LegacyRatingReconciliationPreviewDto Preview(LegacySource source, string status, string reason,
        IReadOnlyList<LegacyRatingCandidateDto> candidates, Guid? reconciliationPublicId) =>
        new(source.SubmissionKind, source.SubmissionPublicId, source.SourceKind, source.SourceKey, source.Score,
            source.ActorName, source.OccurredAt, status, reason, candidates, reconciliationPublicId);

    private static string DisplayName(ApplicationUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return name.Length > 0 ? name : "Historical actor";
    }

    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);

    private sealed record LegacySource(
        SubmissionKind SubmissionKind,
        Guid SubmissionPublicId,
        string SubmissionId,
        LegacyRatingSourceKind SourceKind,
        string SourceKey,
        decimal Score,
        string ActorUserId,
        string ActorName,
        DateTime? OccurredAt,
        string? Comment);
}

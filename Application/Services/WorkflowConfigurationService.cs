using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Application.Services;

public sealed record ReportingWindowDecision(bool Allowed, string Reason, DateTime? EffectiveClose);

public interface IReportingWindowService
{
    Task<ReportingWindowDecision> CheckAsync(SubmissionKind kind, long reportingPeriodId, string userId, int? departmentId, int? unitId, DateTime at);
}

public sealed class ReportingWindowService(ApplicationDbContext context) : IReportingWindowService
{
    public async Task<ReportingWindowDecision> CheckAsync(SubmissionKind kind, long reportingPeriodId, string userId, int? departmentId, int? unitId, DateTime at)
    {
        var window = await context.ReportingWindows.AsNoTracking().SingleOrDefaultAsync(x => x.SubmissionKind == kind && x.ReportingPeriodId == reportingPeriodId && x.IsActive);
        if (window == null) return new(false, "No active reporting window is configured for this period.", null);
        var exceptionClose = await context.ReportingWindowExceptions.AsNoTracking()
            .Where(x => x.ReportingWindowId == window.Id &&
                ((x.UserId != null && x.UserId == userId) || (x.DepartmentId.HasValue && x.DepartmentId == departmentId) || (x.UnitId.HasValue && x.UnitId == unitId)))
            .MaxAsync(x => (DateTime?)x.ExtendedClosesAt);
        var close = exceptionClose.HasValue && exceptionClose > window.ClosesAt ? exceptionClose.Value : window.ClosesAt;
        if (at < window.OpensAt) return new(false, $"Reporting opens at {window.OpensAt:O}.", close);
        if (at > close) return new(false, $"Reporting closed at {close:O}.", close);
        return new(true, "Reporting window is open.", close);
    }
}

public sealed record WorkflowTransitionResult(bool Allowed, string Reason, SubmissionWorkflowInstance? Instance, SubmissionWorkflowAction? Action);
public sealed record WorkflowStageProjection(
    string Code,
    string Name,
    int Sequence,
    bool IsTerminal,
    string RequiredActionCode,
    bool IsOptional,
    bool AllowBypass);
public sealed record SubmissionWorkflowProjection(
    IReadOnlyList<WorkflowStageProjection> Stages,
    string? CurrentStageCode,
    bool Completed,
    bool ActorEligible,
    bool ActorEligibleForBypass,
    WorkflowInstanceState? State);

public interface IConfigurableWorkflowService
{
    Task<WorkflowTransitionResult> PrepareActionAsync(SubmissionKind kind, string submissionId, long reportingPeriodId, string submitterUserId, string actorUserId, string actionCode, WorkflowActionOutcome outcome, string? comment, decimal? rating, string? correlationId);
    Task<SubmissionWorkflowProjection> GetProjectionAsync(SubmissionKind kind, string submissionId, long reportingPeriodId, string submitterUserId, string actorUserId);
}

public sealed class ConfigurableWorkflowService(ApplicationDbContext context) : IConfigurableWorkflowService
{
    public async Task<SubmissionWorkflowProjection> GetProjectionAsync(SubmissionKind kind, string submissionId, long reportingPeriodId, string submitterUserId, string actorUserId)
    {
        var period = await context.ReportingPeriods.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportingPeriodId);
        if (period == null) return new([], null, false, false, false, null);
        var instance = await context.SubmissionWorkflowInstances.AsNoTracking()
            .Include(x => x.CurrentStage)
            .Include(x => x.Actions)
            .Include(x => x.WorkflowDefinition).ThenInclude(x => x.Stages)
            .SingleOrDefaultAsync(x => x.SubmissionKind == kind && x.SubmissionId == submissionId);
        var definition = instance?.WorkflowDefinition ?? await context.WorkflowDefinitions.AsNoTracking()
            .Include(x => x.Stages)
            .Where(x => x.MunicipalityFinancialYearId == period.MunicipalityFinancialYearId && x.SubmissionKind == kind && x.IsActive
                && x.EffectiveFrom <= DateTime.UtcNow && (!x.EffectiveTo.HasValue || x.EffectiveTo > DateTime.UtcNow))
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();
        if (definition == null) return new([], null, false, false, false, null);
        var stages = definition.Stages.Where(x => x.IsActive).OrderBy(x => x.Sequence).ToArray();
        var currentStage = instance?.CurrentStage ?? (instance == null ? stages.FirstOrDefault() : null);
        var previousActor = instance?.Actions.OrderByDescending(x => x.Sequence).Select(x => x.ActorUserId).FirstOrDefault();
        var actorEligible = currentStage != null
            && !(currentStage.RequireDifferentActorFromSubmitter && string.Equals(actorUserId, submitterUserId, StringComparison.OrdinalIgnoreCase))
            && !(currentStage.RequireDifferentActorFromPreviousStage && string.Equals(actorUserId, previousActor, StringComparison.OrdinalIgnoreCase));
        var bypassStage = currentStage is { IsOptional: true, AllowBypass: true }
            ? stages.FirstOrDefault(x => x.Sequence > currentStage.Sequence)
            : null;
        var actorEligibleForBypass = bypassStage != null
            && bypassStage.RequiredActionCode.EndsWith(".APPROVE", StringComparison.OrdinalIgnoreCase)
            && !(bypassStage.RequireDifferentActorFromSubmitter && string.Equals(actorUserId, submitterUserId, StringComparison.OrdinalIgnoreCase))
            && !(bypassStage.RequireDifferentActorFromPreviousStage && string.Equals(actorUserId, previousActor, StringComparison.OrdinalIgnoreCase));
        return new(
            stages.Select(x => new WorkflowStageProjection(
                x.Code, x.Name, x.Sequence, x.IsTerminal, x.RequiredActionCode, x.IsOptional, x.AllowBypass)).ToArray(),
            currentStage?.Code,
            instance?.State == WorkflowInstanceState.Completed,
            actorEligible,
            actorEligibleForBypass,
            instance?.State);
    }

    public async Task<WorkflowTransitionResult> PrepareActionAsync(SubmissionKind kind, string submissionId, long reportingPeriodId, string submitterUserId, string actorUserId, string actionCode, WorkflowActionOutcome outcome, string? comment, decimal? rating, string? correlationId)
    {
        var period = await context.ReportingPeriods.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportingPeriodId);
        if (period == null) return new(false, "Reporting period not found.", null, null);
        var instance = await context.SubmissionWorkflowInstances
            .Include(x => x.CurrentStage)
            .Include(x => x.Actions)
            .Include(x => x.WorkflowDefinition).ThenInclude(x => x.Stages).ThenInclude(x => x.RatingScheme).ThenInclude(x => x!.Values)
            .SingleOrDefaultAsync(x => x.SubmissionKind == kind && x.SubmissionId == submissionId);
        var definition = instance?.WorkflowDefinition ?? await context.WorkflowDefinitions
            .Include(x => x.Stages)
            .ThenInclude(x => x.RatingScheme)
            .ThenInclude(x => x!.Values)
            .Where(x => x.MunicipalityFinancialYearId == period.MunicipalityFinancialYearId && x.SubmissionKind == kind && x.IsActive && x.EffectiveFrom <= DateTime.UtcNow && (!x.EffectiveTo.HasValue || x.EffectiveTo > DateTime.UtcNow))
            .OrderByDescending(x => x.Version).FirstOrDefaultAsync();
        if (definition == null) return new(false, "No effective active workflow definition is configured for this municipality, year, and submission type.", null, null);
        var stages = definition.Stages.Where(x => x.IsActive).OrderBy(x => x.Sequence).ToArray();
        if (stages.Length == 0) return new(false, "The selected workflow version has no active stages.", instance, null);
        if (instance == null)
        {
            instance = new SubmissionWorkflowInstance { MunicipalityId = definition.MunicipalityId, WorkflowDefinitionId = definition.Id, WorkflowDefinition = definition, CurrentStageId = stages[0].Id, CurrentStage = stages[0], SubmissionKind = kind, SubmissionId = submissionId };
            context.SubmissionWorkflowInstances.Add(instance);
        }
        if (instance.State is WorkflowInstanceState.Completed or WorkflowInstanceState.Cancelled) return new(false, "Workflow is already closed.", instance, null);
        var stage = instance.CurrentStage ?? stages.SingleOrDefault(x => x.Id == instance.CurrentStageId);
        if (stage == null) return new(false, "Current workflow stage is invalid.", instance, null);
        var bypass = outcome == WorkflowActionOutcome.Bypass;
        var reopen = outcome == WorkflowActionOutcome.Reopen;
        var resubmit = outcome == WorkflowActionOutcome.Resubmit;
        var actionPrefix = kind == SubmissionKind.Opms ? "OPMS_SUBMISSION." : "IPMS_SUBMISSION.";
        var expectedBypassCode = $"{actionPrefix}APPROVE_BYPASS";
        var expectedReopenCode = $"{actionPrefix}CORRECT";
        var expectedResubmitCode = $"{actionPrefix}RESUBMIT";
        if (bypass && (!stage.IsOptional || !stage.AllowBypass || !string.Equals(actionCode, expectedBypassCode, StringComparison.OrdinalIgnoreCase)))
            return new(false, $"Stage '{stage.Code}' does not allow the requested approver bypass.", instance, null);
        if (reopen && (!string.Equals(actionCode, expectedReopenCode, StringComparison.OrdinalIgnoreCase)
            || instance.State != WorkflowInstanceState.Active))
            return new(false, "Only an active submitted workflow can be reopened for correction.", instance, null);
        if (resubmit && (!string.Equals(actionCode, expectedResubmitCode, StringComparison.OrdinalIgnoreCase) || instance.State != WorkflowInstanceState.Rework))
            return new(false, "Only a submission in rework can be resubmitted.", instance, null);
        if (!bypass && !reopen && !resubmit && !IsActionAllowedAtStage(stage.RequiredActionCode, actionCode, outcome))
            return new(false, $"Action '{actionCode}' is not valid at stage '{stage.Code}'.", instance, null);
        var decisionStage = stage;
        if (bypass)
        {
            decisionStage = stages.FirstOrDefault(x => x.Sequence > stage.Sequence);
            if (decisionStage == null || !decisionStage.RequiredActionCode.EndsWith(".APPROVE", StringComparison.OrdinalIgnoreCase))
                return new(false, "An optional stage can be bypassed only into its immediately following approval stage.", instance, null);
        }
        if (!reopen && !resubmit && decisionStage.RequireDifferentActorFromSubmitter
            && string.Equals(actorUserId, submitterUserId, StringComparison.OrdinalIgnoreCase)
            && outcome != WorkflowActionOutcome.Submit)
            return new(false, "Segregation of duties prevents the submitter from acting at this stage.", instance, null);
        var previousActor = instance.Actions.OrderByDescending(x => x.Sequence).Select(x => x.ActorUserId).FirstOrDefault();
        if (!reopen && !resubmit && decisionStage.RequireDifferentActorFromPreviousStage
            && string.Equals(previousActor, actorUserId, StringComparison.OrdinalIgnoreCase))
            return new(false, "Segregation of duties requires a different actor from the previous stage.", instance, null);
        decimal? achievementPercent = null;
        if (decisionStage.RequiresRating || rating.HasValue)
            achievementPercent = await GetAchievementPercentAsync(kind, submissionId);
        RatingSchemeValue? selectedRating = null;
        if (decisionStage.RequiresRating && !rating.HasValue)
        {
            var automatic = ResolveAchievementRating(decisionStage, achievementPercent);
            if (automatic.Error != null)
                return new(false, automatic.Error, instance, null);
            selectedRating = automatic.Value!;
            rating = selectedRating.Value;
        }
        else if (rating.HasValue)
        {
            if (decisionStage.RatingScheme == null || !decisionStage.RatingScheme.IsActive)
                return new(false, $"Stage '{decisionStage.Code}' is not assigned an active rating scheme.", instance, null);
            selectedRating = decisionStage.RatingScheme.Values.SingleOrDefault(x => x.Value == rating.Value);
            if (selectedRating == null)
                return new(false, $"Rating '{rating.Value}' is not a configured value in scheme '{decisionStage.RatingScheme.Code}'.", instance, null);
        }

        WorkflowStageDefinition? next;
        if (reopen) next = stage;
        else if (resubmit && !stage.RequiredActionCode.EndsWith(".SUBMIT", StringComparison.OrdinalIgnoreCase)) next = stage;
        else if (outcome == WorkflowActionOutcome.Reject && !string.IsNullOrWhiteSpace(stage.RejectionStageCode)) next = stages.SingleOrDefault(x => string.Equals(x.Code, stage.RejectionStageCode, StringComparison.OrdinalIgnoreCase));
        else next = stages.FirstOrDefault(x => x.Sequence > decisionStage.Sequence);
        var completed = !reopen && !resubmit && (decisionStage.IsTerminal || next == null || outcome == WorkflowActionOutcome.Complete);
        var action = new SubmissionWorkflowAction
        {
            MunicipalityId = definition.MunicipalityId, SubmissionWorkflowInstance = instance, Sequence = instance.NextSequence++, FromStageId = stage.Id,
            ToStageId = completed ? null : next!.Id, ActionCode = actionCode, Outcome = outcome, ActorUserId = actorUserId,
            Comment = comment?.Trim(), RatingValue = rating, CorrelationId = correlationId, OccurredAt = DateTime.UtcNow
        };
        context.SubmissionWorkflowActions.Add(action);
        if (selectedRating != null)
        {
            context.SubmissionStageRatings.Add(new SubmissionStageRating
            {
                MunicipalityId = definition.MunicipalityId,
                SubmissionWorkflowInstance = instance,
                SubmissionWorkflowAction = action,
                WorkflowStageDefinitionId = decisionStage.Id,
                WorkflowStageDefinition = decisionStage,
                RatingSchemeId = decisionStage.RatingScheme!.Id,
                RatingScheme = decisionStage.RatingScheme,
                RatingSchemeValueId = selectedRating.Id,
                RatingSchemeValue = selectedRating,
                Value = selectedRating.Value,
                LabelSnapshot = selectedRating.Label,
                AchievementPercent = achievementPercent,
                Comment = comment?.Trim(),
                RatedByUserId = actorUserId,
                RatedAt = action.OccurredAt,
                CorrelationId = correlationId
            });
        }
        instance.CurrentStageId = completed ? null : next!.Id;
        instance.CurrentStage = completed ? null : next;
        instance.State = completed ? WorkflowInstanceState.Completed
            : outcome is WorkflowActionOutcome.Reject or WorkflowActionOutcome.Reopen ? WorkflowInstanceState.Rework
            : WorkflowInstanceState.Active;
        if (completed) instance.CompletedAt = action.OccurredAt;
        return new(true, completed ? "Workflow completed." : $"Workflow advanced to '{next!.Code}'.", instance, action);
    }

    private async Task<decimal?> GetAchievementPercentAsync(SubmissionKind kind, string submissionId) => kind switch
    {
        SubmissionKind.Opms => await context.OpmsSubmissions.AsNoTracking()
            .Where(item => item.Id == submissionId)
            .Select(item => item.AchievementPercent)
            .SingleOrDefaultAsync(),
        SubmissionKind.Ipms => await context.IpmsSubmissions.AsNoTracking()
            .Where(item => item.Id == submissionId)
            .Select(item => item.AchievementPercent)
            .SingleOrDefaultAsync(),
        _ => null
    };

    internal static (RatingSchemeValue? Value, string? Error) ResolveAchievementRating(WorkflowStageDefinition stage, decimal? achievementPercent)
    {
        if (stage.RatingScheme == null || !stage.RatingScheme.IsActive)
            return (null, $"Stage '{stage.Code}' is not assigned an active rating scheme.");
        if (!achievementPercent.HasValue)
            return (null, $"Stage '{stage.Code}' requires a configured rating because the submission has no recorded achievement percentage.");

        var matchingBands = stage.RatingScheme.Values
            .Where(value => value.MinimumAchievementPercent.HasValue
                && value.MaximumAchievementPercent.HasValue
                && achievementPercent.Value >= value.MinimumAchievementPercent.Value
                && achievementPercent.Value <= value.MaximumAchievementPercent.Value)
            .ToArray();
        if (matchingBands.Length == 0)
            return (null, $"Achievement {achievementPercent.Value:0.####}% does not match a configured band in scheme '{stage.RatingScheme.Code}'.");
        if (matchingBands.Length > 1)
            return (null, $"Achievement {achievementPercent.Value:0.####}% matches multiple bands in scheme '{stage.RatingScheme.Code}'. Correct the rating configuration before continuing.");
        return (matchingBands[0], null);
    }

    internal static bool IsActionAllowedAtStage(string requiredActionCode, string actionCode, WorkflowActionOutcome outcome)
    {
        if (string.Equals(requiredActionCode, actionCode, StringComparison.OrdinalIgnoreCase)) return true;
        if (outcome != WorkflowActionOutcome.Reject) return false;

        var separator = requiredActionCode.LastIndexOf('.');
        if (separator < 0) return false;
        var prefix = requiredActionCode[..(separator + 1)];
        var requiredVerb = requiredActionCode[(separator + 1)..];
        return requiredVerb.ToUpperInvariant() switch
        {
            "VERIFY" => string.Equals(actionCode, $"{prefix}VERIFY_REJECT", StringComparison.OrdinalIgnoreCase),
            "APPROVE" => string.Equals(actionCode, $"{prefix}REJECT", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}

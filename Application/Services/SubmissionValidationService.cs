using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Application.Services;

public sealed record SubmissionValidationError(string Field, string Message);

public sealed record SubmissionValidationResult(IReadOnlyList<SubmissionValidationError> Errors, bool RequiresPoe)
{
    public bool IsValid => Errors.Count == 0;
}

public interface ISubmissionValidationService
{
    Task<SubmissionValidationResult> ValidateAsync(OpmsSubmission submission);
    Task<SubmissionValidationResult> ValidateAsync(IpmsSubmission submission);
}

/// <summary>
/// Authoritative submit-boundary validation. Draft saves deliberately remain permissive;
/// this service is called before a workflow action is prepared so a failed validation
/// cannot create a SUBMITTED action or mutate the base submission state.
/// </summary>
public sealed class SubmissionValidationService(ApplicationDbContext context) : ISubmissionValidationService
{
    public Task<SubmissionValidationResult> ValidateAsync(OpmsSubmission submission) => ValidateAsync(
        SubmissionKind.Opms, submission.Id, submission.OpmsTargetId, submission.ReportingPeriodId,
        submission.BaseState, submission.ActualPerformance, submission.VarianceReason,
        submission.CorrectiveMeasure, submission.RequiresPoe, submission.AchievementPercent);

    public Task<SubmissionValidationResult> ValidateAsync(IpmsSubmission submission) => ValidateAsync(
        SubmissionKind.Ipms, submission.Id, submission.IpmsTargetId, submission.ReportingPeriodId,
        submission.BaseState, submission.ActualPerformance, submission.VarianceReason,
        submission.CorrectiveMeasure, submission.RequiresPoe, submission.AchievementPercent);

    private async Task<SubmissionValidationResult> ValidateAsync(
        SubmissionKind kind,
        string submissionId,
        string targetId,
        long? reportingPeriodId,
        string baseState,
        string? actualPerformance,
        string? varianceReason,
        string? correctiveMeasure,
        bool snapshotRequiresPoe,
        decimal? achievementPercent)
    {
        var errors = new List<SubmissionValidationError>();
        if (!string.Equals(baseState, SubmissionBaseStates.InProgress, StringComparison.Ordinal))
            errors.Add(new("BaseState", "Only an IN_PROGRESS submission can be submitted."));
        if (!reportingPeriodId.HasValue)
            errors.Add(new("ReportingPeriod", "A governed reporting period is required."));
        if (string.IsNullOrWhiteSpace(actualPerformance))
            errors.Add(new("ActualPerformance", "Authoritative actual performance is required."));
        if (string.IsNullOrWhiteSpace(varianceReason))
            errors.Add(new("VarianceReason", "Variance reason is required for every applicable reporting submission."));
        else if (varianceReason.Trim().Length > 4000)
            errors.Add(new("VarianceReason", "Variance reason may not exceed 4000 characters."));
        if (string.IsNullOrWhiteSpace(correctiveMeasure))
            errors.Add(new("CorrectiveMeasure", "Corrective measurement is required for every applicable reporting submission."));
        else if (correctiveMeasure.Trim().Length > 4000)
            errors.Add(new("CorrectiveMeasure", "Corrective measurement may not exceed 4000 characters."));

        var ratingRequiresPoe = reportingPeriodId.HasValue
            && await RatingOutcomeRequiresPoeAsync(kind, reportingPeriodId.Value, achievementPercent);
        var requiresPoe = snapshotRequiresPoe || ratingRequiresPoe;
        if (requiresPoe && reportingPeriodId.HasValue
            && !await HasCurrentValidEvidenceAsync(kind, submissionId, targetId, reportingPeriodId.Value))
        {
            errors.Add(new("Evidence", "At least one current, signature-validated and clean supporting-evidence upload is required."));
        }

        return new(errors, requiresPoe);
    }

    private async Task<bool> RatingOutcomeRequiresPoeAsync(
        SubmissionKind kind, long reportingPeriodId, decimal? achievementPercent)
    {
        if (!achievementPercent.HasValue) return false;
        var period = await context.ReportingPeriods.AsNoTracking()
            .Where(item => item.Id == reportingPeriodId)
            .Select(item => new { item.MunicipalityFinancialYearId })
            .SingleOrDefaultAsync();
        if (period == null) return false;
        var now = DateTime.UtcNow;
        var definitionId = await context.WorkflowDefinitions.AsNoTracking()
            .Where(definition => definition.MunicipalityFinancialYearId == period.MunicipalityFinancialYearId
                && definition.SubmissionKind == kind && definition.IsActive
                && definition.EffectiveFrom <= now && (!definition.EffectiveTo.HasValue || definition.EffectiveTo > now))
            .OrderByDescending(definition => definition.Version)
            .Select(definition => (long?)definition.Id)
            .FirstOrDefaultAsync();
        if (!definitionId.HasValue) return false;

        var ratingSchemeIds = context.WorkflowStageDefinitions.AsNoTracking()
            .Where(stage => stage.WorkflowDefinitionId == definitionId.Value
                && stage.IsActive && stage.RatingSchemeId.HasValue)
            .Select(stage => stage.RatingSchemeId!.Value);
        return await context.RatingSchemeValues.AsNoTracking()
            .Where(value => ratingSchemeIds.Contains(value.RatingSchemeId))
            .AnyAsync(value => value.RequiresPoe
                && value.MinimumAchievementPercent.HasValue && value.MaximumAchievementPercent.HasValue
                && achievementPercent.Value >= value.MinimumAchievementPercent.Value
                && achievementPercent.Value <= value.MaximumAchievementPercent.Value);
    }

    private async Task<bool> HasCurrentValidEvidenceAsync(
        SubmissionKind kind, string submissionId, string targetId, long reportingPeriodId)
    {
        var submissionIds = new List<string> { submissionId };
        var period = await context.ReportingPeriods.AsNoTracking().SingleOrDefaultAsync(item => item.Id == reportingPeriodId);
        if (period?.PeriodType is ReportingPeriodType.MidTerm or ReportingPeriodType.Annual)
        {
            var sourceTypes = period.PeriodType == ReportingPeriodType.MidTerm
                ? new[] { ReportingPeriodType.Quarter1, ReportingPeriodType.Quarter2 }
                : new[] { ReportingPeriodType.Quarter1, ReportingPeriodType.Quarter2, ReportingPeriodType.Quarter3, ReportingPeriodType.Quarter4 };
            var sourcePeriodIds = await context.ReportingPeriods.AsNoTracking()
                .Where(item => item.MunicipalityFinancialYearId == period.MunicipalityFinancialYearId
                    && sourceTypes.Contains(item.PeriodType) && item.IsActive)
                .Select(item => item.Id).ToArrayAsync();
            var sourceIds = kind == SubmissionKind.Opms
                ? await context.OpmsSubmissions.AsNoTracking()
                    .Where(item => item.OpmsTargetId == targetId && item.ReportingPeriodId.HasValue
                        && sourcePeriodIds.Contains(item.ReportingPeriodId.Value)
                        && item.BaseState == SubmissionBaseStates.Submitted && !item.IsDisabled)
                    .Select(item => item.Id).ToArrayAsync()
                : await context.IpmsSubmissions.AsNoTracking()
                    .Where(item => item.IpmsTargetId == targetId && item.ReportingPeriodId.HasValue
                        && sourcePeriodIds.Contains(item.ReportingPeriodId.Value)
                        && item.BaseState == SubmissionBaseStates.Submitted && !item.IsDisabled)
                    .Select(item => item.Id).ToArrayAsync();
            submissionIds.AddRange(sourceIds);
        }

        return await context.PoeFiles.AsNoTracking().AnyAsync(file =>
            file.SubmissionKind == kind && submissionIds.Contains(file.SubmissionId) && file.IsActive
            && file.Blob.SignatureVerified && file.Blob.ScanStatus == "Clean"
            && !file.Blob.IsQuarantined && !file.Blob.IsContentDeleted);
    }
}

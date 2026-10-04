using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Application.Services;

public sealed record PerformanceSuggestionResult(
    bool Generated,
    bool ManualRequired,
    string Code,
    string Explanation,
    string? SystemSuggestedActualPerformance,
    string? ActualPerformance,
    bool WasSystemSuggestionEdited,
    IReadOnlyList<string> SourcePeriods);

public interface IPerformanceSuggestionService
{
    Task<PerformanceSuggestionResult> GenerateOpmsAsync(string submissionId, string actorUserId, string correlationId);
    Task<PerformanceSuggestionResult> GenerateIpmsAsync(string submissionId, string actorUserId, string correlationId);
    Task<PerformanceSuggestionResult> RecordFinalOpmsActualAsync(string submissionId, string actualPerformance, string? editReason, string rowVersion, string actorUserId, string correlationId);
    Task<PerformanceSuggestionResult> RecordFinalIpmsActualAsync(string submissionId, string actualPerformance, string? editReason, string rowVersion, string actorUserId, string correlationId);
}

public sealed class PerformanceSuggestionService(
    ApplicationDbContext context,
    IPerformanceConsolidationEngine consolidationEngine,
    IPerformanceUnitEngine unitEngine) : IPerformanceSuggestionService
{
    public async Task<PerformanceSuggestionResult> GenerateOpmsAsync(string submissionId, string actorUserId, string correlationId)
    {
        var current = await context.OpmsSubmissions.Include(item => item.OpmsTarget).Include(item => item.ReportingPeriod)
            .SingleOrDefaultAsync(item => item.Id == submissionId);
        if (current == null) return Missing("OPMS submission not found.");
        return await GenerateAsync(current, SubmissionKind.Opms, current.OpmsTargetId, current.OpmsTarget.CalculationTypeId,
            current.ReportingPeriodId, actorUserId, correlationId);
    }

    public async Task<PerformanceSuggestionResult> GenerateIpmsAsync(string submissionId, string actorUserId, string correlationId)
    {
        var current = await context.IpmsSubmissions.Include(item => item.IpmsTarget).Include(item => item.ReportingPeriod)
            .SingleOrDefaultAsync(item => item.Id == submissionId);
        if (current == null) return Missing("IPMS submission not found.");
        return await GenerateAsync(current, SubmissionKind.Ipms, current.IpmsTargetId, current.IpmsTarget.CalculationTypeId,
            current.ReportingPeriodId, actorUserId, correlationId);
    }

    public async Task<PerformanceSuggestionResult> RecordFinalOpmsActualAsync(string submissionId, string actualPerformance, string? editReason, string rowVersion, string actorUserId, string correlationId)
    {
        var current = await context.OpmsSubmissions.Include(item => item.ReportingPeriod).SingleOrDefaultAsync(item => item.Id == submissionId);
        if (current == null) return Missing("OPMS submission not found.");
        return await RecordFinalAsync(current, SubmissionKind.Opms, current.OpmsTargetId, current.ReportingPeriodId,
            actualPerformance, editReason, rowVersion, actorUserId, correlationId);
    }

    public async Task<PerformanceSuggestionResult> RecordFinalIpmsActualAsync(string submissionId, string actualPerformance, string? editReason, string rowVersion, string actorUserId, string correlationId)
    {
        var current = await context.IpmsSubmissions.Include(item => item.ReportingPeriod).SingleOrDefaultAsync(item => item.Id == submissionId);
        if (current == null) return Missing("IPMS submission not found.");
        return await RecordFinalAsync(current, SubmissionKind.Ipms, current.IpmsTargetId, current.ReportingPeriodId,
            actualPerformance, editReason, rowVersion, actorUserId, correlationId);
    }

    private async Task<PerformanceSuggestionResult> GenerateAsync(
        object current,
        SubmissionKind kind,
        string targetId,
        long? calculationTypeId,
        long? reportingPeriodId,
        string actorUserId,
        string correlationId)
    {
        if (GetIsDisabled(current)) return Manual("SUBMISSION_WITHDRAWN", "A withdrawn submission cannot receive a consolidation suggestion.");
        if (!string.Equals(SubmissionBaseStates.Normalize(GetBaseState(current)), SubmissionBaseStates.InProgress, StringComparison.OrdinalIgnoreCase))
            return Manual("SUBMISSION_NOT_EDITABLE", "A consolidation suggestion can be generated only while the submission is in progress.");
        var municipalityId = GetMunicipalityId(current);
        if (municipalityId is not > 0) return Manual("MUNICIPALITY_REQUIRED", "The submission must be reconciled to a municipality before generating a suggestion.");
        if (reportingPeriodId is not > 0) return Manual("REPORTING_PERIOD_REQUIRED", "A governed reporting period is required before generating a suggestion.");
        var period = GetPeriod(current);
        if (period == null) return Manual("REPORTING_PERIOD_REQUIRED", "The governed reporting period could not be resolved.");
        var requiredTypes = period.PeriodType switch
        {
            ReportingPeriodType.MidTerm => new[] { ReportingPeriodType.Quarter1, ReportingPeriodType.Quarter2 },
            ReportingPeriodType.Annual => new[] { ReportingPeriodType.Quarter1, ReportingPeriodType.Quarter2, ReportingPeriodType.Quarter3, ReportingPeriodType.Quarter4 },
            _ => null
        };
        if (requiredTypes == null) return Manual("NOT_A_CONSOLIDATION_PERIOD", "Suggestions are generated only for Mid-Term or Annual reporting periods.");
        if (GetSuggestion(current) != null)
            return Existing(current, "SUGGESTION_ALREADY_GENERATED", "The original system suggestion is retained and was not regenerated.");
        if (calculationTypeId is not > 0) return Manual("CALCULATION_TYPE_REQUIRED", "Configure a KPI calculation type before generating a suggestion.");

        var definition = await context.PerformanceCalculationTypes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == calculationTypeId.Value && item.IsActive);
        if (definition == null || !TryCalculationType(definition.Code, out var calculationType))
            return Manual("CALCULATION_TYPE_REQUIRED", "The configured KPI calculation type is missing, inactive or unsupported.");
        var policy = await context.MunicipalityConsolidationPolicies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MunicipalityId == municipalityId.Value && item.CalculationTypeId == definition.Id && item.IsActive);
        var sourcePeriods = await context.ReportingPeriods.AsNoTracking()
            .Where(item => item.MunicipalityFinancialYearId == period.MunicipalityFinancialYearId && requiredTypes.Contains(item.PeriodType) && item.IsActive)
            .OrderBy(item => item.Sequence).ToArrayAsync();
        if (sourcePeriods.Length != requiredTypes.Length || requiredTypes.Any(type => sourcePeriods.Count(item => item.PeriodType == type) != 1))
            return Manual("SOURCE_PERIOD_CONFIGURATION_INVALID", "The financial year must contain exactly one active governed reporting period for every required source quarter.");
        sourcePeriods = requiredTypes.Select(type => sourcePeriods.Single(item => item.PeriodType == type)).ToArray();
        var periodIds = sourcePeriods.Select(item => item.Id).Append(period.Id).ToArray();
        var targets = kind == SubmissionKind.Opms
            ? await context.PerformancePeriodTargets.AsNoTracking().Include(item => item.ReportingPeriod).Where(item => item.OpmsTargetId == targetId && periodIds.Contains(item.ReportingPeriodId) && item.IsActive).ToArrayAsync()
            : await context.PerformancePeriodTargets.AsNoTracking().Include(item => item.ReportingPeriod).Where(item => item.IpmsTargetId == targetId && periodIds.Contains(item.ReportingPeriodId) && item.IsActive).ToArrayAsync();
        var destinationTarget = targets.SingleOrDefault(item => item.ReportingPeriodId == period.Id);
        if (destinationTarget == null) return Manual("DESTINATION_TARGET_REQUIRED", "The consolidation period has no active governed period target.");

        var actuals = kind == SubmissionKind.Opms
            ? await context.OpmsSubmissions.AsNoTracking().Where(item => item.OpmsTargetId == targetId && item.ReportingPeriodId.HasValue && periodIds.Contains(item.ReportingPeriodId.Value) && item.BaseState == SubmissionBaseStates.Submitted && item.SubmittedAt.HasValue && !item.IsDisabled && item.ActualPerformance != null)
                .Select(item => new SourceActual(item.ReportingPeriodId!.Value, item.ActualPerformance!)).ToArrayAsync()
            : await context.IpmsSubmissions.AsNoTracking().Where(item => item.IpmsTargetId == targetId && item.ReportingPeriodId.HasValue && periodIds.Contains(item.ReportingPeriodId.Value) && item.BaseState == SubmissionBaseStates.Submitted && item.SubmittedAt.HasValue && !item.IsDisabled && item.ActualPerformance != null)
                .Select(item => new SourceActual(item.ReportingPeriodId!.Value, item.ActualPerformance!)).ToArrayAsync();
        var sources = sourcePeriods.Select(sourcePeriod =>
        {
            var sourceTarget = targets.SingleOrDefault(item => item.ReportingPeriodId == sourcePeriod.Id);
            var actual = actuals.SingleOrDefault(item => item.ReportingPeriodId == sourcePeriod.Id);
            return sourceTarget == null || actual == null
                ? null
                : new PerformanceConsolidationSource(sourcePeriod.Code, PerformanceRevisionResolver.EffectiveUnitKind(sourceTarget), actual.Value, true, true);
        }).Where(item => item != null).Cast<PerformanceConsolidationSource>().ToArray();
        var consolidation = consolidationEngine.Consolidate(new PerformanceConsolidationRequest(
            calculationType, PerformanceRevisionResolver.EffectiveUnitKind(destinationTarget), sourcePeriods.Select(item => item.Code).ToArray(), sources,
            policy?.ConsolidationRule, policy?.MissingValuePolicy ?? ConsolidationMissingValuePolicy.Block));
        if (!consolidation.CanSuggest)
            return new(false, true, consolidation.Code, consolidation.Explanation, null, GetActual(current), false, consolidation.SourcePeriods);

        var now = DateTime.UtcNow;
        var existingActual = GetActual(current);
        var edited = !string.IsNullOrWhiteSpace(existingActual) && !string.Equals(existingActual, consolidation.SuggestedValue, StringComparison.Ordinal);
        var finalActual = existingActual ?? consolidation.SuggestedValue!;
        PerformanceCalculationResult calculation;
        try { calculation = unitEngine.Calculate(PerformanceRevisionResolver.EffectiveUnitKind(destinationTarget), PerformanceRevisionResolver.EffectiveTargetValue(destinationTarget), finalActual, destinationTarget.Direction); }
        catch (ArgumentException exception) { return Manual("INVALID_DESTINATION_TARGET", exception.Message); }
        SetGenerated(current, consolidation.SuggestedValue!, calculation, edited, definition.Id, now, actorUserId,
            edited ? actorUserId : null, edited ? now : null, edited ? "Existing actual retained when the system suggestion was generated." : null);
        context.PerformanceSuggestionEvents.Add(Event(current, kind, period.Id, PerformanceSuggestionEventType.Generated,
            consolidation.SuggestedValue, calculation.CanonicalActual, edited, definition.Id,
            consolidation.EffectiveCalculationType, consolidation.SourcePeriods, actorUserId, edited ? "Existing actual retained when the system suggestion was generated." : null, now, correlationId));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Manual("CONCURRENCY_CONFLICT", "The submission changed while the suggestion was generated. Refresh and try again."); }
        return new(true, false, "SUGGESTION_GENERATED", consolidation.Explanation, consolidation.SuggestedValue,
            GetActual(current), edited, consolidation.SourcePeriods);
    }

    private async Task<PerformanceSuggestionResult> RecordFinalAsync(
        object current,
        SubmissionKind kind,
        string targetId,
        long? reportingPeriodId,
        string actualPerformance,
        string? editReason,
        string rowVersion,
        string actorUserId,
        string correlationId)
    {
        if (GetIsDisabled(current)) return Manual("SUBMISSION_WITHDRAWN", "A withdrawn submission cannot receive a consolidated actual.");
        if (!string.Equals(SubmissionBaseStates.Normalize(GetBaseState(current)), SubmissionBaseStates.InProgress, StringComparison.OrdinalIgnoreCase))
            return Manual("SUBMISSION_NOT_EDITABLE", "A consolidated actual can be changed only while the submission is in progress.");
        var suggestion = GetSuggestion(current);
        if (suggestion == null) return Manual("SUGGESTION_REQUIRED", "Generate the system suggestion before accepting or editing the consolidated actual.");
        if (reportingPeriodId is not > 0) return Manual("REPORTING_PERIOD_REQUIRED", "A governed reporting period is required.");
        if (!TrySetVersion(current, rowVersion)) return Manual("ROW_VERSION_REQUIRED", "A valid RowVersion is required.");
        var target = kind == SubmissionKind.Opms
            ? await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).SingleOrDefaultAsync(item => item.OpmsTargetId == targetId && item.ReportingPeriodId == reportingPeriodId.Value && item.IsActive)
            : await context.PerformancePeriodTargets.Include(item => item.ReportingPeriod).SingleOrDefaultAsync(item => item.IpmsTargetId == targetId && item.ReportingPeriodId == reportingPeriodId.Value && item.IsActive);
        if (target == null) return Manual("DESTINATION_TARGET_REQUIRED", "The consolidation period has no active governed period target.");
        var effectiveUnit = PerformanceRevisionResolver.EffectiveUnitKind(target);
        var normalized = unitEngine.Normalize(effectiveUnit, actualPerformance);
        if (!normalized.IsValid) return Manual("INVALID_ACTUAL_PERFORMANCE", normalized.Error!);
        var edited = !string.Equals(suggestion, normalized.CanonicalValue, StringComparison.Ordinal);
        if (edited && string.IsNullOrWhiteSpace(editReason)) return Manual("EDIT_REASON_REQUIRED", "A reason is required when changing the system suggestion.");
        if (edited && editReason!.Trim().Length > 1000) return Manual("EDIT_REASON_TOO_LONG", "The edit reason cannot exceed 1000 characters.");

        var now = DateTime.UtcNow;
        PerformanceCalculationResult calculation;
        try { calculation = unitEngine.Calculate(effectiveUnit, PerformanceRevisionResolver.EffectiveTargetValue(target), normalized.CanonicalValue!, target.Direction); }
        catch (ArgumentException exception) { return Manual("INVALID_DESTINATION_TARGET", exception.Message); }
        SetFinal(current, calculation, edited, actorUserId, now, edited ? actorUserId : null, edited ? now : null, edited ? editReason!.Trim() : null);
        var submissionId = GetId(current);
        var sourcePeriods = await context.PerformanceSuggestionEvents.AsNoTracking()
            .Where(item => kind == SubmissionKind.Opms ? item.OpmsSubmissionId == submissionId : item.IpmsSubmissionId == submissionId)
            .OrderBy(item => item.OccurredAt).Select(item => item.SourcePeriods).FirstOrDefaultAsync() ?? string.Empty;
        var calculationTypeId = GetSuggestionCalculationTypeId(current);
        context.PerformanceSuggestionEvents.Add(Event(current, kind, reportingPeriodId.Value,
            edited ? PerformanceSuggestionEventType.Edited : PerformanceSuggestionEventType.Accepted,
            suggestion, normalized.CanonicalValue, edited, calculationTypeId, null,
            sourcePeriods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), actorUserId,
            edited ? editReason!.Trim() : null, now, correlationId));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Manual("CONCURRENCY_CONFLICT", "The submission changed since it was loaded. Refresh and try again."); }
        return new(false, false, edited ? "SUGGESTION_EDITED" : "SUGGESTION_ACCEPTED",
            edited ? "The final actual was saved without overwriting the original system suggestion." : "The system suggestion was accepted as the final actual.",
            suggestion, normalized.CanonicalValue, edited,
            sourcePeriods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private bool TrySetVersion(object entity, string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            // SQL Server rowversion is 8 bytes; the provider-neutral SQLite development profile uses a 16-byte GUID token.
            if (bytes.Length is not (8 or 16)) return false;
            context.Entry(entity).Property("RowVersion").OriginalValue = bytes;
            return true;
        }
        catch (FormatException) { return false; }
    }

    private static ReportingPeriod? GetPeriod(object value) => value switch { OpmsSubmission item => item.ReportingPeriod, IpmsSubmission item => item.ReportingPeriod, _ => null };
    private static string GetId(object value) => value switch { OpmsSubmission item => item.Id, IpmsSubmission item => item.Id, _ => string.Empty };
    private static string? GetSuggestion(object value) => value switch { OpmsSubmission item => item.SystemSuggestedActualPerformance, IpmsSubmission item => item.SystemSuggestedActualPerformance, _ => null };
    private static string? GetActual(object value) => value switch { OpmsSubmission item => item.ActualPerformance, IpmsSubmission item => item.ActualPerformance, _ => null };
    private static long? GetMunicipalityId(object value) => value switch { OpmsSubmission item => item.MunicipalityId, IpmsSubmission item => item.MunicipalityId, _ => null };
    private static bool GetIsDisabled(object value) => value switch { OpmsSubmission item => item.IsDisabled, IpmsSubmission item => item.IsDisabled, _ => true };
    private static string? GetBaseState(object value) => value switch { OpmsSubmission item => item.BaseState, IpmsSubmission item => item.BaseState, _ => null };
    private static long? GetSuggestionCalculationTypeId(object value) => value switch { OpmsSubmission item => item.SuggestionCalculationTypeId, IpmsSubmission item => item.SuggestionCalculationTypeId, _ => null };

    private static void SetGenerated(object value, string suggestion, PerformanceCalculationResult calculation, bool edited, long typeId, DateTime generatedAt, string actor, string? editor, DateTime? editedAt, string? reason)
    {
        switch (value)
        {
            case OpmsSubmission item: item.SystemSuggestedActualPerformance = suggestion; ApplyCalculation(item, calculation); item.WasSystemSuggestionEdited = edited; item.SuggestionCalculationTypeId = typeId; item.SuggestionGeneratedDate = generatedAt; item.SuggestionEditedByUserId = editor; item.SuggestionEditedAt = editedAt; item.SuggestionEditReason = reason; item.UpdatedBy = actor; item.UpdatedOn = generatedAt; break;
            case IpmsSubmission item: item.SystemSuggestedActualPerformance = suggestion; ApplyCalculation(item, calculation); item.WasSystemSuggestionEdited = edited; item.SuggestionCalculationTypeId = typeId; item.SuggestionGeneratedDate = generatedAt; item.SuggestionEditedByUserId = editor; item.SuggestionEditedAt = editedAt; item.SuggestionEditReason = reason; item.UpdatedBy = actor; item.UpdatedOn = generatedAt; break;
        }
    }

    private static void SetFinal(object value, PerformanceCalculationResult calculation, bool edited, string actor, DateTime updatedAt, string? editor, DateTime? editedAt, string? reason)
    {
        switch (value)
        {
            case OpmsSubmission item: ApplyCalculation(item, calculation); item.WasSystemSuggestionEdited = edited; item.SuggestionEditedByUserId = editor; item.SuggestionEditedAt = editedAt; item.SuggestionEditReason = reason; item.UpdatedBy = actor; item.UpdatedOn = updatedAt; break;
            case IpmsSubmission item: ApplyCalculation(item, calculation); item.WasSystemSuggestionEdited = edited; item.SuggestionEditedByUserId = editor; item.SuggestionEditedAt = editedAt; item.SuggestionEditReason = reason; item.UpdatedBy = actor; item.UpdatedOn = updatedAt; break;
        }
    }

    private static void ApplyCalculation(OpmsSubmission item, PerformanceCalculationResult calculation)
    {
        item.ActualPerformance = calculation.CanonicalActual;
        item.Variance = calculation.Variance;
        item.AchievementPercent = calculation.AchievementPercent;
        item.TargetAchieved = calculation.Achieved;
    }

    private static void ApplyCalculation(IpmsSubmission item, PerformanceCalculationResult calculation)
    {
        item.ActualPerformance = calculation.CanonicalActual;
        item.Variance = calculation.Variance;
        item.AchievementPercent = calculation.AchievementPercent;
        item.TargetAchieved = calculation.Achieved;
    }

    private static PerformanceSuggestionEvent Event(object value, SubmissionKind kind, long reportingPeriodId,
        PerformanceSuggestionEventType eventType, string? suggestion, string? actual, bool edited, long? calculationTypeId,
        PerformanceCalculationType? effectiveType, IReadOnlyList<string> sourcePeriods, string actor, string? reason, DateTime occurredAt, string correlationId) =>
        new()
        {
            MunicipalityId = value switch { OpmsSubmission item => item.MunicipalityId!.Value, IpmsSubmission item => item.MunicipalityId!.Value, _ => 0 },
            SubmissionKind = kind,
            OpmsSubmissionId = kind == SubmissionKind.Opms ? GetId(value) : null,
            IpmsSubmissionId = kind == SubmissionKind.Ipms ? GetId(value) : null,
            ReportingPeriodId = reportingPeriodId,
            EventType = eventType,
            SystemSuggestedActualPerformance = suggestion,
            ActualPerformance = actual,
            WasSystemSuggestionEdited = edited,
            SuggestionCalculationTypeId = calculationTypeId,
            EffectiveCalculationType = effectiveType,
            SourcePeriods = string.Join(',', sourcePeriods),
            ActorUserId = actor,
            Reason = reason,
            OccurredAt = occurredAt,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId[..Math.Min(100, correlationId.Length)]
        };

    private static bool TryCalculationType(string code, out PerformanceCalculationType value) =>
        Enum.TryParse(code.Replace("_", string.Empty, StringComparison.Ordinal), true, out value);
    private static PerformanceSuggestionResult Missing(string explanation) => new(false, true, "NOT_FOUND", explanation, null, null, false, []);
    private static PerformanceSuggestionResult Manual(string code, string explanation) => new(false, true, code, explanation, null, null, false, []);
    private static PerformanceSuggestionResult Existing(object value, string code, string explanation) => new(false, false, code, explanation, GetSuggestion(value), GetActual(value), value switch { OpmsSubmission item => item.WasSystemSuggestionEdited, IpmsSubmission item => item.WasSystemSuggestionEdited, _ => false }, []);
    private sealed record SourceActual(long ReportingPeriodId, string Value);
}

using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

public static class PerformanceApiSupport
{
    internal static string ResolveWorkflowStatus(string requestedStatus, SubmissionWorkflowInstance? instance) =>
        instance?.State == WorkflowInstanceState.Completed ? "completed" : requestedStatus;

    internal static async Task HydrateFinancialYearsAsync(ApplicationDbContext context, IEnumerable<ReportingPeriod?> reportingPeriods)
    {
        var periods = reportingPeriods.Where(period => period != null).Cast<ReportingPeriod>().DistinctBy(period => period.Id).ToArray();
        if (periods.Length == 0) return;

        var municipalityFinancialYearIds = periods.Select(period => period.MunicipalityFinancialYearId).Distinct().ToArray();
        var years = await context.MunicipalityFinancialYears
            .Include(item => item.FinancialYear)
            .Where(item => municipalityFinancialYearIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        foreach (var period in periods)
            if (years.TryGetValue(period.MunicipalityFinancialYearId, out var year)) period.MunicipalityFinancialYear = year;
    }

    public static IQueryable<PoeFile> IncludePoeGovernance(this IQueryable<PoeFile> query) => query
        .Include(item => item.Blob)
        .Include(item => item.UploadedByUser)
        .Include(item => item.Assessments).ThenInclude(item => item.AssessedByUser)
        .Include(item => item.ReplacementAsNew).ThenInclude(item => item!.SupersededPoeFile)
        .Include(item => item.ReplacementAsNew).ThenInclude(item => item!.ReplacedByUser)
        .Include(item => item.ReplacementsAsOld).ThenInclude(item => item.ReplacementPoeFile)
        .Include(item => item.ReplacementsAsOld).ThenInclude(item => item.ReplacedByUser)
        .Include(item => item.LegalHoldEvents).ThenInclude(item => item.ActorUser)
        .Include(item => item.DisposalEvents).ThenInclude(item => item.ActorUser);

    public static string? GetCurrentUserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static async Task<(int? DepartmentId, int? UnitId, string? Error)> ResolveOrganizationScopeAsync(
        ApplicationDbContext context,
        long? municipalityId,
        int? departmentId,
        int? unitId,
        Guid? departmentPublicId,
        Guid? unitPublicId)
    {
        if (!municipalityId.HasValue || municipalityId == long.MinValue)
            return (null, null, "A municipality context is required.");

        if (departmentPublicId.HasValue)
        {
            var resolved = await context.Departments.Where(item => item.PublicId == departmentPublicId && item.MunicipalityId == municipalityId && item.IsActive)
                .Select(item => (int?)item.Id).SingleOrDefaultAsync();
            if (!resolved.HasValue || (departmentId.HasValue && departmentId != resolved))
                return (null, null, "The selected department is invalid, inactive, or outside the municipality.");
            departmentId = resolved;
        }
        else if (departmentId.HasValue && !await context.Departments.AnyAsync(item => item.Id == departmentId && item.MunicipalityId == municipalityId && item.IsActive))
            return (null, null, "The selected department is invalid, inactive, or outside the municipality.");

        if (unitPublicId.HasValue)
        {
            var resolved = await context.Units.Where(item => item.PublicId == unitPublicId && item.MunicipalityId == municipalityId && item.IsActive)
                .Select(item => new { item.Id, item.DepartmentId }).SingleOrDefaultAsync();
            if (resolved == null || (unitId.HasValue && unitId != resolved.Id))
                return (null, null, "The selected unit is invalid, inactive, or outside the municipality.");
            unitId = resolved.Id;
            if (departmentId.HasValue && departmentId != resolved.DepartmentId)
                return (null, null, "The selected unit does not belong to the selected department.");
            departmentId ??= resolved.DepartmentId;
        }
        else if (unitId.HasValue)
        {
            var resolvedDepartmentId = await context.Units.Where(item => item.Id == unitId && item.MunicipalityId == municipalityId && item.IsActive)
                .Select(item => (int?)item.DepartmentId).SingleOrDefaultAsync();
            if (!resolvedDepartmentId.HasValue)
                return (null, null, "The selected unit is invalid, inactive, or outside the municipality.");
            if (departmentId.HasValue && departmentId != resolvedDepartmentId)
                return (null, null, "The selected unit does not belong to the selected department.");
            departmentId ??= resolvedDepartmentId;
        }

        return (departmentId, unitId, null);
    }

    public static string? GetIpAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString();

    public static string BuildProtectedFileUrl(HttpContext context, PoeFile file)
    {
        var kind = file.SubmissionKind == SubmissionKind.Opms ? "opms-submissions" : "ipms-submissions";
        return $"{context.Request.Scheme}://{context.Request.Host}/api/{kind}/{file.SubmissionId}/attachments/{file.Id}/content";
    }

    public static OpmsTargetTemplateResponse ToResponse(this OpmsTargetTemplate template) =>
        new(
            template.PublicId,
            template.TemplateCode,
            template.TemplateName,
            template.IndicatorNumber,
            template.TargetName,
            template.KpiDescription,
            template.Baseline,
            template.AnnualTarget,
            template.AnnualTargetDescription,
            template.TargetUnitType,
            template.UnitOfMeasure,
            template.NationalKpa,
            template.MunicipalKpa,
            template.StrategicGoal,
            template.StrategicObjective,
            template.PerformanceObjective,
            template.Outcome,
            template.Output,
            template.PriorityIssue,
            template.BudgetSource,
            template.BudgetType,
            template.Weight,
            template.KpiType,
            template.IndicatorType,
            template.FunctionalArea,
            template.StandardClassification,
            template.IdpReference,
            template.InternalReference,
            template.FmsLink,
            template.DefaultQuarterlyTargetsJson,
            template.DefaultBudgetInformation,
            template.DefaultPoeRequirements,
            template.IsActive,
            template.IsArchived,
            template.Version,
            template.CreatedBy,
            template.CreatedDate,
            Convert.ToBase64String(template.RowVersion));

    public static IpmsTargetTemplateResponse ToResponse(this IpmsTargetTemplate template) =>
        new(
            template.PublicId,
            template.TemplateCode,
            template.TemplateName,
            template.TargetName,
            template.KpiDescription,
            template.PerformanceArea,
            template.EmployeeLevel,
            template.JobGrade,
            template.TargetUnitType,
            template.UnitOfMeasure,
            template.AnnualTarget,
            template.AnnualTargetDescription,
            template.Weight,
            template.DefaultRatingMethod,
            template.DefaultScoreScale,
            template.DefaultPoeRequirements,
            template.DefaultTaskTemplatesJson,
            template.LinkedOpmsTargetRequired,
            template.FunctionalArea,
            template.IsActive,
            template.IsArchived,
            template.Version,
            template.CreatedBy,
            template.CreatedDate,
            Convert.ToBase64String(template.RowVersion));

    public static OpmsTargetResponse ToResponse(this OpmsTarget target) => target.ToResponse(PeriodTargetMemberAccess.Full);

    public static OpmsTargetResponse ToResponse(this OpmsTarget target, PeriodTargetMemberAccess memberAccess) =>
        target.ToResponse(memberAccess, KpiLifecycleMemberAccess.Full);

    public static OpmsTargetResponse ToResponse(this OpmsTarget target, PeriodTargetMemberAccess memberAccess, KpiLifecycleMemberAccess lifecycleAccess) =>
        new(
            target.Id,
            target.SourceTemplateId,
            target.SourceTemplateVersion,
            target.PeriodId,
            target.DepartmentId,
            target.Department?.Name,
            target.UnitId,
            target.Unit?.Name,
            target.AssignedUser?.PublicId,
            target.AssignedUser != null ? target.AssignedUser.FullName : null,
            target.Wards.Select(item => item.WardId).OrderBy(id => id).ToArray(),
            target.AdditionalAssignees.Select(item => item.User.PublicId).OrderBy(id => id).ToArray(),
            target.VoteNumbers.Select(item => item.VoteNumberId).OrderBy(id => id).ToArray(),
            target.IndicatorNumber,
            target.NationalKpa,
            target.MunicipalKpa,
            target.StrategicGoalId,
            target.StrategicObjectiveId,
            target.PerformanceObjective,
            target.TargetName,
            target.KpiDescription,
            target.Baseline,
            target.BaselineDescription,
            target.BudgetSourceId,
            target.BudgetTypeId,
            target.UnitOfMeasureId,
            target.Weight,
            target.KpiType,
            target.IndicatorType,
            target.FunctionalArea,
            target.StandardClassification,
            target.IdpReference,
            target.InternalReference,
            target.FmsLink,
            target.IsRevised,
            target.IsWithdrawn,
            lifecycleAccess.WithdrawalReason ? target.ReasonForWithdrawal : null,
            target.CanonicalPeriodTargets.Select(item => ToResponse(item, memberAccess)).ToArray(),
            target.CreatedAt)
        {
            PublicId = target.PublicId,
            RowVersion = Convert.ToBase64String(target.RowVersion),
            OriginalOrderNumber = target.OriginalOrderNumber,
            RevisedOrderNumber = target.RevisedOrderNumber,
            IsIndicatorNumberRevised = target.IsIndicatorNumberRevised,
            RevisedIndicatorNumber = target.RevisedIndicatorNumber,
            IsTargetNameRevised = target.IsTargetNameRevised,
            RevisedTargetName = target.RevisedTargetName,
            IsKpiDescriptionRevised = target.IsKpiDescriptionRevised,
            RevisedKpiDescription = target.RevisedKpiDescription,
            WithdrawnAt = target.WithdrawnAt,
            SdbipLayerPublicId = target.SdbipLayer?.PublicId,
            MunicipalityFinancialYearPublicId = target.CanonicalPeriodTargets.FirstOrDefault()?.ReportingPeriod.MunicipalityFinancialYear.PublicId,
            MunicipalityFinancialYearName = target.CanonicalPeriodTargets.FirstOrDefault()?.ReportingPeriod.MunicipalityFinancialYear.FinancialYear.Name,
            SdbipLayerCode = target.SdbipLayer?.Code,
            SdbipLayerName = target.SdbipLayer?.Name,
            DepartmentPublicId = target.Department?.PublicId,
            UnitPublicId = target.Unit?.PublicId,
            NationalKpaPublicId = target.NationalKpaReference?.PublicId,
            MunicipalKpaPublicId = target.MunicipalKpaReference?.PublicId,
            BackToBasicsPillarPublicId = target.BackToBasicsPillarReference?.PublicId,
            StrategicGoalPublicId = target.StrategicGoalMaster?.PublicId,
            StrategicGoalCode = target.StrategicGoalMaster?.Code,
            StrategicGoalName = target.StrategicGoalMaster?.Name,
            StrategicInterventionPublicId = target.StrategicInterventionReference?.PublicId,
            StrategicObjectivePublicId = target.StrategicObjectiveMaster?.PublicId,
            StrategicObjectiveCode = target.StrategicObjectiveMaster?.Code,
            StrategicObjectiveName = target.StrategicObjectiveMaster?.Name,
            PerformanceObjectivePublicId = target.PerformanceObjectiveReference?.PublicId,
            PerformanceObjectiveCode = target.PerformanceObjectiveReference?.Code,
            PerformanceObjectiveName = target.PerformanceObjectiveReference?.Name,
            BackToBasicsPillar = target.BackToBasicsPillarReference?.Name,
            StrategicIntervention = target.StrategicInterventionReference?.Name,
            BudgetTypePublicId = target.BudgetTypeMaster?.PublicId,
            BudgetTypeName = target.BudgetTypeMaster?.Name,
            BudgetSources = target.GovernedBudgetSources.Where(item => item.IsActive).OrderBy(item => item.BudgetSource.DisplayOrder).ThenBy(item => item.BudgetSource.Name)
                .Select(item => new KpiBudgetSourceResponse(item.PublicId, item.BudgetSource.PublicId, item.BudgetSource.Code ?? string.Empty, item.BudgetSource.Name, item.Amount)).ToArray(),
            KpiTypePublicId = target.KpiTypeMaster?.PublicId,
            IndicatorTypePublicId = target.IndicatorTypeMaster?.PublicId,
            FunctionalAreaPublicId = target.FunctionalAreaMaster?.PublicId,
            StandardClassificationPublicId = target.StandardClassificationMaster?.PublicId,
            KpiUnitOfMeasurePublicId = target.KpiUnitOfMeasureMaster?.PublicId,
            KpiUnitOfMeasureName = target.KpiUnitOfMeasureMaster?.Name,
            KpiUnitOfMeasureSymbol = target.KpiUnitOfMeasureMaster?.Symbol,
            VoteNumbers = target.VoteNumbers
                .Where(item => item.VoteNumber != null)
                .OrderBy(item => item.VoteNumber.Number)
                .Select(item => new OpmsTargetVoteNumberResponse(item.VoteNumber.Id, item.VoteNumber.PublicId, item.VoteNumber.Code, item.VoteNumber.Number, item.VoteNumber.Name, item.VoteNumber.Amount))
                .ToArray()
        };

    public static OpmsTargetResponse ToResponse(this OpmsTarget target, ReportingPeriodType? periodType) =>
        target.ToResponse(periodType, PeriodTargetMemberAccess.Full);

    public static OpmsTargetResponse ToResponse(this OpmsTarget target, ReportingPeriodType? periodType, PeriodTargetMemberAccess memberAccess) =>
        target.ToResponse(periodType, memberAccess, KpiLifecycleMemberAccess.Full);

    public static OpmsTargetResponse ToResponse(this OpmsTarget target, ReportingPeriodType? periodType, PeriodTargetMemberAccess memberAccess, KpiLifecycleMemberAccess lifecycleAccess) =>
        !periodType.HasValue ? target.ToResponse(memberAccess, lifecycleAccess) : target.ToResponse(memberAccess, lifecycleAccess) with
        {
            IndicatorNumber = PerformanceRevisionResolver.EffectiveIndicatorNumber(target, periodType.Value),
            TargetName = PerformanceRevisionResolver.EffectiveTargetName(target, periodType.Value),
            KpiDescription = PerformanceRevisionResolver.EffectiveKpiDescription(target, periodType.Value)
        };

    public static IpmsTargetResponse ToResponse(this IpmsTarget target, Guid? supervisorPublicId = null) =>
        target.ToResponse(supervisorPublicId, PeriodTargetMemberAccess.Full);

    public static IpmsTargetResponse ToResponse(this IpmsTarget target, Guid? supervisorPublicId, PeriodTargetMemberAccess memberAccess) =>
        target.ToResponse(supervisorPublicId, memberAccess, KpiLifecycleMemberAccess.Full);

    public static IpmsTargetResponse ToResponse(this IpmsTarget target, Guid? supervisorPublicId, PeriodTargetMemberAccess memberAccess, KpiLifecycleMemberAccess lifecycleAccess) =>
        new(
            target.Id,
            target.SourceTemplateId,
            target.SourceTemplateVersion,
            target.RelatedOpmsTargetId,
            target.PeriodId,
            target.DepartmentId,
            target.Department?.Name,
            target.UnitId,
            target.Unit?.Name,
            target.AssignedUser?.PublicId,
            target.AssignedUser != null ? target.AssignedUser.FullName : null,
            supervisorPublicId,
            target.IndicatorNumber,
            target.NationalKpa,
            target.MunicipalKpa,
            target.StrategicGoalId,
            target.StrategicObjectiveId,
            target.PerformanceObjective,
            target.TargetName,
            target.KpiDescription,
            target.Baseline,
            target.BudgetSourceId,
            target.BudgetTypeId,
            target.UnitOfMeasureId,
            target.Weight,
            target.KpiType,
            target.IndicatorType,
            target.FunctionalArea,
            target.IdpReference,
            target.InternalReference,
            target.IsRevised,
            target.CanonicalPeriodTargets.Select(item => ToResponse(item, memberAccess)).ToArray(),
            target.CreatedAt)
        {
            PublicId = target.PublicId,
            RowVersion = Convert.ToBase64String(target.RowVersion),
            OriginalOrderNumber = target.OriginalOrderNumber,
            RevisedOrderNumber = target.RevisedOrderNumber,
            IsIndicatorNumberRevised = target.IsIndicatorNumberRevised,
            RevisedIndicatorNumber = target.RevisedIndicatorNumber,
            IsTargetNameRevised = target.IsTargetNameRevised,
            RevisedTargetName = target.RevisedTargetName,
            IsKpiDescriptionRevised = target.IsKpiDescriptionRevised,
            RevisedKpiDescription = target.RevisedKpiDescription,
            IsWithdrawn = target.IsWithdrawn,
            ReasonForWithdrawal = lifecycleAccess.WithdrawalReason ? target.ReasonForWithdrawal : null,
            WithdrawnAt = target.WithdrawnAt,
            DepartmentPublicId = target.Department?.PublicId,
            MunicipalityFinancialYearPublicId = target.CanonicalPeriodTargets.FirstOrDefault()?.ReportingPeriod.MunicipalityFinancialYear.PublicId,
            UnitPublicId = target.Unit?.PublicId,
            NationalKpaPublicId = target.NationalKpaReference?.PublicId,
            MunicipalKpaPublicId = target.MunicipalKpaReference?.PublicId,
            BackToBasicsPillarPublicId = target.BackToBasicsPillarReference?.PublicId,
            StrategicGoalPublicId = target.StrategicGoalMaster?.PublicId,
            StrategicInterventionPublicId = target.StrategicInterventionReference?.PublicId,
            StrategicObjectivePublicId = target.StrategicObjectiveMaster?.PublicId,
            PerformanceObjectivePublicId = target.PerformanceObjectiveReference?.PublicId,
            BackToBasicsPillar = target.BackToBasicsPillarReference?.Name,
            StrategicIntervention = target.StrategicInterventionReference?.Name,
            BudgetTypePublicId = target.BudgetTypeMaster?.PublicId,
            BudgetTypeName = target.BudgetTypeMaster?.Name,
            BudgetSources = target.GovernedBudgetSources.Where(item => item.IsActive).OrderBy(item => item.BudgetSource.DisplayOrder).ThenBy(item => item.BudgetSource.Name)
                .Select(item => new KpiBudgetSourceResponse(item.PublicId, item.BudgetSource.PublicId, item.BudgetSource.Code ?? string.Empty, item.BudgetSource.Name, item.Amount)).ToArray(),
            KpiTypePublicId = target.KpiTypeMaster?.PublicId,
            IndicatorTypePublicId = target.IndicatorTypeMaster?.PublicId,
            FunctionalAreaPublicId = target.FunctionalAreaMaster?.PublicId,
            KpiUnitOfMeasurePublicId = target.KpiUnitOfMeasureMaster?.PublicId,
            KpiUnitOfMeasureName = target.KpiUnitOfMeasureMaster?.Name,
            KpiUnitOfMeasureSymbol = target.KpiUnitOfMeasureMaster?.Symbol
        };

    public static IpmsTargetResponse ToResponse(this IpmsTarget target, ReportingPeriodType? periodType, Guid? supervisorPublicId = null) =>
        target.ToResponse(periodType, supervisorPublicId, PeriodTargetMemberAccess.Full);

    public static IpmsTargetResponse ToResponse(this IpmsTarget target, ReportingPeriodType? periodType, Guid? supervisorPublicId, PeriodTargetMemberAccess memberAccess) =>
        target.ToResponse(periodType, supervisorPublicId, memberAccess, KpiLifecycleMemberAccess.Full);

    public static IpmsTargetResponse ToResponse(this IpmsTarget target, ReportingPeriodType? periodType, Guid? supervisorPublicId, PeriodTargetMemberAccess memberAccess, KpiLifecycleMemberAccess lifecycleAccess) =>
        !periodType.HasValue ? target.ToResponse(supervisorPublicId, memberAccess, lifecycleAccess) : target.ToResponse(supervisorPublicId, memberAccess, lifecycleAccess) with
        {
            IndicatorNumber = PerformanceRevisionResolver.EffectiveIndicatorNumber(target, periodType.Value),
            TargetName = PerformanceRevisionResolver.EffectiveTargetName(target, periodType.Value),
            KpiDescription = PerformanceRevisionResolver.EffectiveKpiDescription(target, periodType.Value)
        };

    private static TargetPeriodValueResponse ToResponse(PerformancePeriodTarget target, PeriodTargetMemberAccess memberAccess) =>
        new(
            target.PublicId,
            target.ReportingPeriod.PublicId,
            target.ReportingPeriod.Code,
            target.ReportingPeriod.PeriodType,
            PerformanceRevisionResolver.EffectiveUnitKind(target),
            target.Direction,
            memberAccess.TargetValue ? PerformanceRevisionResolver.EffectiveTargetValue(target) : null,
            memberAccess.BudgetValue ? PerformanceRevisionResolver.EffectiveBudgetValue(target) : null,
            memberAccess.Description ? target.Description : null,
            target.IsActive,
            Convert.ToBase64String(target.RowVersion))
        {
            OriginalUnitKind = target.UnitKind,
            OriginalTargetValue = memberAccess.TargetValue ? target.TargetValue : null,
            OriginalBudgetValue = memberAccess.BudgetValue ? target.BudgetValue : null,
            IsTargetRevised = target.IsTargetRevised,
            RevisedUnitKind = target.RevisedUnitKind,
            RevisedTargetValue = memberAccess.TargetValue ? target.RevisedTargetValue : null,
            IsBudgetRevised = target.IsBudgetRevised,
            RevisedBudgetValue = memberAccess.BudgetValue ? target.RevisedBudgetValue : null
        };

    public static OpmsSubmissionResponse ToResponse(this OpmsSubmission submission) =>
        new(
            submission.Id,
            submission.OpmsTargetId,
            PerformanceRevisionResolver.EffectiveTargetName(submission.OpmsTarget, PerformanceRevisionResolver.ResolvePeriodType(submission.ReportingPeriod?.PeriodType, submission.Quarter)),
            PerformanceRevisionResolver.EffectiveIndicatorNumber(submission.OpmsTarget, PerformanceRevisionResolver.ResolvePeriodType(submission.ReportingPeriod?.PeriodType, submission.Quarter)),
            submission.ReportingPeriod?.Code ?? submission.Quarter,
            submission.Status,
            submission.SubmitterStatus,
            submission.VerifierStatus,
            submission.ApproverStatus,
            submission.PmsStatus,
            submission.AuditorStatus,
            submission.ActualPerformance,
            submission.ActualExpenditure,
            submission.Variance,
            submission.VarianceReason,
            submission.CorrectiveMeasure,
            submission.SubmitterScore,
            submission.SubmittedAt,
            submission.SubmittedByUser?.PublicId,
            submission.SubmittedByUser != null ? submission.SubmittedByUser.FullName : null,
            submission.VerifierUser?.PublicId,
            submission.VerifierUser != null ? submission.VerifierUser.FullName : null,
            submission.VerifiedAt,
            submission.VerifierComments,
            submission.VerifierComment,
            submission.VerifierScore,
            submission.ApproverUser?.PublicId,
            submission.ApproverUser != null ? submission.ApproverUser.FullName : null,
            submission.ApprovedAt,
            submission.ApproverComments,
            submission.ApproverComment,
            submission.ApproverScore,
            submission.PmsOfficerUser?.PublicId,
            submission.PmsOfficerUser != null ? submission.PmsOfficerUser.FullName : null,
            submission.PmsReviewedAt,
            submission.PmsComments,
            submission.PmsComment,
            submission.PmsRecommendation,
            submission.PmsScore,
            submission.PmsResponseDueDate,
            submission.PmsRfiComment,
            submission.AuditorUser?.PublicId,
            submission.AuditorUser != null ? submission.AuditorUser.FullName : null,
            submission.AuditedAt,
            submission.AuditorComments,
            submission.AuditorComment,
            submission.AuditorRecommendation,
            submission.AuditorScore,
            submission.AuditorResponseDueDate,
            submission.DueDate,
            submission.ExtendedDueDate,
            submission.DueDateExtendedDays,
            submission.PoeType,
            submission.IsDisabled,
            submission.CreatedBy,
            submission.CreatedOn,
            submission.UpdatedBy,
            submission.UpdatedOn,
            submission.OrganisationId,
            submission.CreatedAt)
        {
            BaseState = submission.BaseState,
            ReportingPeriodPublicId = submission.ReportingPeriod?.PublicId,
            TargetDepartmentPublicId = submission.OpmsTarget.Department?.PublicId,
            TargetDepartmentName = submission.OpmsTarget.Department?.Name,
            TargetUnitPublicId = submission.OpmsTarget.Unit?.PublicId,
            TargetUnitName = submission.OpmsTarget.Unit?.Name,
            TargetFinancialYearName = submission.ReportingPeriod?.MunicipalityFinancialYear?.FinancialYear?.Name,
            TargetUnitOfMeasureName = submission.OpmsTarget.UnitOfMeasure?.Name,
            TargetUnitOfMeasureSymbol = submission.OpmsTarget.UnitOfMeasure?.Symbol,
            TargetUnitType = EffectiveTargetUnitType(submission.OpmsTarget.CanonicalPeriodTargets, submission.ReportingPeriodId),
            SystemSuggestedActualPerformance = submission.SystemSuggestedActualPerformance,
            WasSystemSuggestionEdited = submission.WasSystemSuggestionEdited,
            SuggestionGeneratedDate = submission.SuggestionGeneratedDate,
            SuggestionEditedByUserPublicId = submission.SuggestionEditedByUser?.PublicId,
            SuggestionEditedByName = submission.SuggestionEditedByUser == null ? null : submission.SuggestionEditedByUser.FullName,
            SuggestionEditedAt = submission.SuggestionEditedAt,
            SuggestionEditReason = submission.SuggestionEditReason,
            AchievementPercent = submission.AchievementPercent,
            TargetAchieved = submission.TargetAchieved,
            RowVersion = Convert.ToBase64String(submission.RowVersion),
            WithdrawalReason = submission.WithdrawalReason,
            WithdrawnAt = submission.WithdrawnAt,
            WithdrawnByUserPublicId = null,
            WithdrawnByName = null
        };

    public static IpmsSubmissionResponse ToResponse(this IpmsSubmission submission) =>
        new(
            submission.Id,
            submission.IpmsTargetId,
            PerformanceRevisionResolver.EffectiveTargetName(submission.IpmsTarget, PerformanceRevisionResolver.ResolvePeriodType(submission.ReportingPeriod?.PeriodType, submission.Quarter)),
            PerformanceRevisionResolver.EffectiveIndicatorNumber(submission.IpmsTarget, PerformanceRevisionResolver.ResolvePeriodType(submission.ReportingPeriod?.PeriodType, submission.Quarter)),
            submission.ReportingPeriod?.Code ?? submission.Quarter,
            submission.Status,
            submission.SubmitterStatus,
            submission.VerifierStatus,
            submission.ApproverStatus,
            submission.PmsStatus,
            submission.AuditorStatus,
            submission.ActualPerformance,
            submission.ActualExpenditure,
            submission.Variance,
            submission.VarianceReason,
            submission.CorrectiveMeasure,
            submission.SubmitterScore,
            submission.SubmittedAt,
            submission.SubmittedByUser?.PublicId,
            submission.SubmittedByUser != null ? submission.SubmittedByUser.FullName : null,
            submission.VerifierUser?.PublicId,
            submission.VerifierUser != null ? submission.VerifierUser.FullName : null,
            submission.VerifiedAt,
            submission.VerifierComments,
            submission.VerifierComment,
            submission.VerifierScore,
            submission.ApproverUser?.PublicId,
            submission.ApproverUser != null ? submission.ApproverUser.FullName : null,
            submission.ApprovedAt,
            submission.ApproverComments,
            submission.ApproverComment,
            submission.ApproverScore,
            submission.PmsOfficerUser?.PublicId,
            submission.PmsOfficerUser != null ? submission.PmsOfficerUser.FullName : null,
            submission.PmsReviewedAt,
            submission.PmsComments,
            submission.PmsComment,
            submission.PmsRecommendation,
            submission.PmsScore,
            submission.PmsResponseDueDate,
            submission.PmsRfiComment,
            submission.AuditorUser?.PublicId,
            submission.AuditorUser != null ? submission.AuditorUser.FullName : null,
            submission.AuditedAt,
            submission.AuditorComments,
            submission.AuditorComment,
            submission.AuditorRecommendation,
            submission.AuditorScore,
            submission.AuditorResponseDueDate,
            submission.DueDate,
            submission.ExtendedDueDate,
            submission.DueDateExtendedDays,
            submission.PoeType,
            submission.IsDisabled,
            submission.CreatedBy,
            submission.CreatedOn,
            submission.UpdatedBy,
            submission.UpdatedOn,
            submission.OrganisationId,
            submission.CreatedAt)
        {
            BaseState = submission.BaseState,
            ReportingPeriodPublicId = submission.ReportingPeriod?.PublicId,
            TargetDepartmentPublicId = submission.IpmsTarget.Department?.PublicId,
            TargetDepartmentName = submission.IpmsTarget.Department?.Name,
            TargetUnitPublicId = submission.IpmsTarget.Unit?.PublicId,
            TargetUnitName = submission.IpmsTarget.Unit?.Name,
            TargetFinancialYearName = submission.ReportingPeriod?.MunicipalityFinancialYear?.FinancialYear?.Name,
            TargetUnitOfMeasureName = submission.IpmsTarget.UnitOfMeasure?.Name,
            TargetUnitOfMeasureSymbol = submission.IpmsTarget.UnitOfMeasure?.Symbol,
            TargetUnitType = EffectiveTargetUnitType(submission.IpmsTarget.CanonicalPeriodTargets, submission.ReportingPeriodId),
            SystemSuggestedActualPerformance = submission.SystemSuggestedActualPerformance,
            WasSystemSuggestionEdited = submission.WasSystemSuggestionEdited,
            SuggestionGeneratedDate = submission.SuggestionGeneratedDate,
            SuggestionEditedByUserPublicId = submission.SuggestionEditedByUser?.PublicId,
            SuggestionEditedByName = submission.SuggestionEditedByUser == null ? null : submission.SuggestionEditedByUser.FullName,
            SuggestionEditedAt = submission.SuggestionEditedAt,
            SuggestionEditReason = submission.SuggestionEditReason,
            AchievementPercent = submission.AchievementPercent,
            TargetAchieved = submission.TargetAchieved,
            RowVersion = Convert.ToBase64String(submission.RowVersion),
            WithdrawalReason = submission.WithdrawalReason,
            WithdrawnAt = submission.WithdrawnAt,
            WithdrawnByUserPublicId = null,
            WithdrawnByName = null
        };

    private static string? EffectiveTargetUnitType(
        IReadOnlyList<PerformancePeriodTarget> periodTargets,
        long? reportingPeriodId)
    {
        if (!reportingPeriodId.HasValue) return null;
        var periodTarget = periodTargets.SingleOrDefault(item => item.ReportingPeriodId == reportingPeriodId.Value && item.IsActive);
        return periodTarget == null ? null : PerformanceRevisionResolver.EffectiveUnitKind(periodTarget).ToString();
    }

    public static NotificationResponse ToResponse(this Notification notification) =>
        new(
            notification.PublicId,
            notification.User.PublicId,
            notification.User.FullName,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.EntityName,
            notification.IsRead,
            notification.CreatedAt);

    public static AuditTrailEntryResponse ToResponse(this AuditTrail audit) =>
        new(
            audit.PublicId,
            audit.MunicipalityId,
            audit.EntityName,
            audit.EntityId,
            audit.Action,
            audit.OldValue,
            audit.NewValue,
            audit.ChangedBy,
            audit.ChangedAt,
            audit.IpAddress,
            audit.CorrelationId,
            audit.Reason,
            audit.UserAgent,
            audit.SessionId);

    public static PoeFileResponse ToResponse(this PoeFile file, HttpContext context, PoeResponseMemberAccess? memberAccess = null)
    {
        memberAccess ??= PoeResponseMemberAccess.None;
        return new(
            file.Id,
            file.SubmissionKind.ToString(),
            file.SubmissionId,
            file.FileName,
            file.Blob.ContentType,
            file.Blob.SizeInBytes,
            memberAccess.UploadedByUserId ? file.UploadedByUser?.PublicId : null,
            memberAccess.UploadedByName ? file.UploadedByUser?.FullName : null,
            file.UploadedAt,
            file.IsActive && !file.Blob.IsContentDeleted && file.Blob.ScanStatus == "Clean" && !file.Blob.IsQuarantined ? BuildProtectedFileUrl(context, file) : string.Empty)
        {
            PublicId = file.PublicId,
            EvidenceBlobPublicId = file.Blob.PublicId,
            Sha256 = file.Blob.Sha256,
            SignatureVerified = file.Blob.SignatureVerified,
            ScanStatus = file.Blob.ScanStatus,
            IsQuarantined = file.Blob.IsQuarantined,
            ScannerProvider = memberAccess.ScannerProvider ? file.Blob.ScannerProvider : null,
            ScannerReference = memberAccess.ScannerReference ? file.Blob.ScannerReference : null,
            ScanDetail = memberAccess.ScanDetail ? file.Blob.ScanDetail : null,
            ScannedAt = file.Blob.ScannedAt,
            RetainUntil = file.RetainUntil,
            Assessments = file.Assessments.OrderBy(item => item.AssessedAt).Select(item => new PoeEvidenceAssessmentResponse(
                item.PublicId, item.Outcome.ToString(), memberAccess.AssessmentComment ? item.Comment : null,
                memberAccess.AssessedByUserId ? item.AssessedByUser?.PublicId : null,
                memberAccess.AssessedByName ? item.AssessedByUser?.FullName : null,
                item.AssessedAt, memberAccess.AssessmentCorrelationId ? item.CorrelationId : null)).ToArray(),
            RowVersion = Convert.ToBase64String(file.RowVersion),
            ReplacementOf = file.ReplacementAsNew == null ? null : ToReplacementResponse(file.ReplacementAsNew, memberAccess),
            ReplacedBy = file.ReplacementsAsOld.OrderByDescending(item => item.ReplacedAt).Select(item => ToReplacementResponse(item, memberAccess)).FirstOrDefault()
            , LegalHolds = file.LegalHoldEvents.GroupBy(item => item.HoldId).Select(group => ToLegalHoldResponse(group.OrderBy(item => item.OccurredAt).ToArray(), memberAccess)).OrderByDescending(item => item.PlacedAt).ToArray(),
            IsActive = file.IsActive,
            Disposals = file.DisposalEvents.GroupBy(item => item.DisposalId).Select(group => ToDisposalResponse(group.OrderBy(item => item.OccurredAt).ToArray(), memberAccess)).OrderByDescending(item => item.RequestedAt).ToArray()
            , IsContentDeleted = file.Blob.IsContentDeleted
        };
    }

    private static PoeEvidenceReplacementResponse ToReplacementResponse(PoeEvidenceReplacement item, PoeResponseMemberAccess memberAccess) => new(
        item.PublicId, item.SupersededPoeFile.PublicId, item.SupersededPoeFile.FileName,
        item.ReplacementPoeFile.PublicId, item.ReplacementPoeFile.FileName, item.Reason,
        memberAccess.ReplacedByUserId ? item.ReplacedByUser?.PublicId : null,
        memberAccess.ReplacedByName ? item.ReplacedByUser?.FullName : null,
        item.ReplacedAt, memberAccess.ReplacementCorrelationId ? item.CorrelationId : null);
    private static PoeLegalHoldResponse ToLegalHoldResponse(PoeLegalHoldEvent[] events, PoeResponseMemberAccess memberAccess)
    {
        var placed = events.First(item => item.Action == PoeLegalHoldAction.Placed);
        var released = events.LastOrDefault(item => item.Action == PoeLegalHoldAction.Released);
        return new(placed.HoldId, placed.HoldReference, released == null, placed.Reason,
            memberAccess.LegalHoldActorUserId ? placed.ActorUser?.PublicId : null,
            memberAccess.LegalHoldActorName ? placed.ActorUser?.FullName : null,
            placed.OccurredAt, released?.Reason,
            memberAccess.LegalHoldActorUserId ? released?.ActorUser?.PublicId : null,
            memberAccess.LegalHoldActorName ? released?.ActorUser?.FullName : null,
            released?.OccurredAt);
    }
    private static PoeDisposalResponse ToDisposalResponse(PoeDisposalEvent[] events, PoeResponseMemberAccess memberAccess)
    {
        var requested = events.First(item => item.Action == PoeDisposalAction.Requested);
        var completed = events.LastOrDefault(item => item.Action == PoeDisposalAction.Completed);
        var failed = events.LastOrDefault(item => item.Action == PoeDisposalAction.Failed);
        var status = completed != null ? "Completed" : failed != null ? "Failed" : "Pending";
        return new(requested.DisposalId, status, requested.ApprovalReference, requested.Reason,
            memberAccess.DisposalRequestedByUserId ? requested.ActorUser?.PublicId : null,
            memberAccess.DisposalRequestedByName ? requested.ActorUser?.FullName : null,
            requested.OccurredAt, completed?.OccurredAt, failed?.OccurredAt,
            memberAccess.DisposalDetail ? completed?.Detail ?? failed?.Detail : null);
    }
}

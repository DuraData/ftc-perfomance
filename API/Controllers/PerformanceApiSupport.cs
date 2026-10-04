using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

public static class PerformanceApiSupport
{
    public static IQueryable<PoeFile> IncludePoeGovernance(this IQueryable<PoeFile> query) => query
        .Include(item => item.Blob)
        .Include(item => item.UploadedByUser)
        .Include(item => item.Assessments).ThenInclude(item => item.AssessedByUser)
        .Include(item => item.ReplacementAsNew).ThenInclude(item => item!.SupersededPoeFile)
        .Include(item => item.ReplacementAsNew).ThenInclude(item => item!.ReplacementPoeFile)
        .Include(item => item.ReplacementAsNew).ThenInclude(item => item!.ReplacedByUser)
        .Include(item => item.ReplacementsAsOld).ThenInclude(item => item.ReplacementPoeFile)
        .Include(item => item.ReplacementsAsOld).ThenInclude(item => item.SupersededPoeFile)
        .Include(item => item.ReplacementsAsOld).ThenInclude(item => item.ReplacedByUser)
        .Include(item => item.LegalHoldEvents).ThenInclude(item => item.ActorUser)
        .Include(item => item.DisposalEvents).ThenInclude(item => item.ActorUser);

    public static string? GetCurrentUserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetIpAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString();

    public static string BuildProtectedFileUrl(HttpContext context, PoeFile file)
    {
        var kind = file.SubmissionKind == SubmissionKind.Opms ? "opms-submissions" : "ipms-submissions";
        return $"{context.Request.Scheme}://{context.Request.Host}/api/{kind}/{file.SubmissionId}/attachments/{file.Id}/content";
    }

    public static OpmsTargetTemplateResponse ToResponse(this OpmsTargetTemplate template) =>
        new(
            template.Id,
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
            template.CreatedDate);

    public static IpmsTargetTemplateResponse ToResponse(this IpmsTargetTemplate template) =>
        new(
            template.Id,
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
            template.CreatedDate);

    public static OpmsTargetResponse ToResponse(this OpmsTarget target) =>
        new(
            target.Id,
            target.SourceTemplateId,
            target.SourceTemplateVersion,
            target.PeriodId,
            target.DepartmentId,
            target.Department?.Name,
            target.UnitId,
            target.Unit?.Name,
            target.AssignedUserId,
            target.AssignedUser != null ? target.AssignedUser.FullName : null,
            target.Wards.Select(item => item.WardId).OrderBy(id => id).ToArray(),
            target.AdditionalAssignees.Select(item => item.UserId).OrderBy(id => id).ToArray(),
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
            target.ReasonForWithdrawal,
            target.CanonicalPeriodTargets.Select(ToResponse).ToArray(),
            target.CreatedAt)
        {
            PublicId = target.PublicId,
            RowVersion = Convert.ToBase64String(target.RowVersion),
            OriginalOrderNumber = target.OriginalOrderNumber,
            RevisedOrderNumber = target.RevisedOrderNumber,
            WithdrawnAt = target.WithdrawnAt,
            WithdrawnByUserId = target.WithdrawnByUserId
        };

    public static IpmsTargetResponse ToResponse(this IpmsTarget target) =>
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
            target.AssignedUserId,
            target.AssignedUser != null ? target.AssignedUser.FullName : null,
            target.SupervisorId,
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
            target.CanonicalPeriodTargets.Select(ToResponse).ToArray(),
            target.CreatedAt)
        {
            PublicId = target.PublicId,
            RowVersion = Convert.ToBase64String(target.RowVersion),
            OriginalOrderNumber = target.OriginalOrderNumber,
            RevisedOrderNumber = target.RevisedOrderNumber,
            IsWithdrawn = target.IsWithdrawn,
            ReasonForWithdrawal = target.ReasonForWithdrawal,
            WithdrawnAt = target.WithdrawnAt,
            WithdrawnByUserId = target.WithdrawnByUserId
        };

    private static TargetPeriodValueResponse ToResponse(PerformancePeriodTarget target) =>
        new(
            target.PublicId,
            target.ReportingPeriod.PublicId,
            target.ReportingPeriod.Code,
            target.ReportingPeriod.PeriodType,
            target.UnitKind,
            target.Direction,
            target.TargetValue,
            target.BudgetValue,
            target.Description,
            target.IsActive,
            Convert.ToBase64String(target.RowVersion));

    public static OpmsSubmissionResponse ToResponse(this OpmsSubmission submission) =>
        new(
            submission.Id,
            submission.OpmsTargetId,
            submission.OpmsTarget.TargetName,
            submission.OpmsTarget.IndicatorNumber,
            submission.Quarter,
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
            submission.SubmittedByUserId,
            submission.SubmittedByUser != null ? submission.SubmittedByUser.FullName : null,
            submission.VerifierUserId,
            submission.VerifierUser != null ? submission.VerifierUser.FullName : null,
            submission.VerifiedAt,
            submission.VerifierComments,
            submission.VerifierComment,
            submission.VerifierScore,
            submission.ApproverUserId,
            submission.ApproverUser != null ? submission.ApproverUser.FullName : null,
            submission.ApprovedAt,
            submission.ApproverComments,
            submission.ApproverComment,
            submission.ApproverScore,
            submission.PmsOfficerUserId,
            submission.PmsOfficerUser != null ? submission.PmsOfficerUser.FullName : null,
            submission.PmsReviewedAt,
            submission.PmsComments,
            submission.PmsComment,
            submission.PmsRecommendation,
            submission.PmsScore,
            submission.PmsResponseDueDate,
            submission.PmsRfiComment,
            submission.AuditorUserId,
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
            SystemSuggestedActualPerformance = submission.SystemSuggestedActualPerformance,
            WasSystemSuggestionEdited = submission.WasSystemSuggestionEdited,
            SuggestionGeneratedDate = submission.SuggestionGeneratedDate,
            SuggestionEditedByUserId = submission.SuggestionEditedByUserId,
            SuggestionEditedAt = submission.SuggestionEditedAt,
            SuggestionEditReason = submission.SuggestionEditReason,
            AchievementPercent = submission.AchievementPercent,
            TargetAchieved = submission.TargetAchieved,
            RowVersion = Convert.ToBase64String(submission.RowVersion),
            WithdrawalReason = submission.WithdrawalReason,
            WithdrawnAt = submission.WithdrawnAt,
            WithdrawnByUserId = submission.WithdrawnByUserId
        };

    public static IpmsSubmissionResponse ToResponse(this IpmsSubmission submission) =>
        new(
            submission.Id,
            submission.IpmsTargetId,
            submission.IpmsTarget.TargetName,
            submission.IpmsTarget.IndicatorNumber,
            submission.Quarter,
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
            submission.SubmittedByUserId,
            submission.SubmittedByUser != null ? submission.SubmittedByUser.FullName : null,
            submission.VerifierUserId,
            submission.VerifierUser != null ? submission.VerifierUser.FullName : null,
            submission.VerifiedAt,
            submission.VerifierComments,
            submission.VerifierComment,
            submission.VerifierScore,
            submission.ApproverUserId,
            submission.ApproverUser != null ? submission.ApproverUser.FullName : null,
            submission.ApprovedAt,
            submission.ApproverComments,
            submission.ApproverComment,
            submission.ApproverScore,
            submission.PmsOfficerUserId,
            submission.PmsOfficerUser != null ? submission.PmsOfficerUser.FullName : null,
            submission.PmsReviewedAt,
            submission.PmsComments,
            submission.PmsComment,
            submission.PmsRecommendation,
            submission.PmsScore,
            submission.PmsResponseDueDate,
            submission.PmsRfiComment,
            submission.AuditorUserId,
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
            SystemSuggestedActualPerformance = submission.SystemSuggestedActualPerformance,
            WasSystemSuggestionEdited = submission.WasSystemSuggestionEdited,
            SuggestionGeneratedDate = submission.SuggestionGeneratedDate,
            SuggestionEditedByUserId = submission.SuggestionEditedByUserId,
            SuggestionEditedAt = submission.SuggestionEditedAt,
            SuggestionEditReason = submission.SuggestionEditReason,
            AchievementPercent = submission.AchievementPercent,
            TargetAchieved = submission.TargetAchieved,
            RowVersion = Convert.ToBase64String(submission.RowVersion),
            WithdrawalReason = submission.WithdrawalReason,
            WithdrawnAt = submission.WithdrawnAt,
            WithdrawnByUserId = submission.WithdrawnByUserId
        };

    public static NotificationResponse ToResponse(this Notification notification) =>
        new(
            notification.Id,
            notification.UserId,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.EntityName,
            notification.EntityId,
            notification.IsRead,
            notification.CreatedAt);

    public static AuditTrailEntryResponse ToResponse(this AuditTrail audit) =>
        new(
            audit.Id,
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

    public static PoeFileResponse ToResponse(this PoeFile file, HttpContext context) =>
        new(
            file.Id,
            file.SubmissionKind.ToString(),
            file.SubmissionId,
            file.FileName,
            file.Blob.ContentType,
            file.Blob.SizeInBytes,
            file.UploadedByUserId,
            file.UploadedByUser?.FullName,
            file.UploadedAt,
            file.IsActive && !file.Blob.IsContentDeleted && file.Blob.ScanStatus == "Clean" && !file.Blob.IsQuarantined ? BuildProtectedFileUrl(context, file) : string.Empty)
        {
            PublicId = file.PublicId,
            EvidenceBlobPublicId = file.Blob.PublicId,
            Sha256 = file.Blob.Sha256,
            SignatureVerified = file.Blob.SignatureVerified,
            ScanStatus = file.Blob.ScanStatus,
            IsQuarantined = file.Blob.IsQuarantined,
            ScannerProvider = file.Blob.ScannerProvider,
            ScannerReference = file.Blob.ScannerReference,
            ScanDetail = file.Blob.ScanDetail,
            ScannedAt = file.Blob.ScannedAt,
            RetainUntil = file.RetainUntil,
            Assessments = file.Assessments.OrderBy(item => item.AssessedAt).Select(item => new PoeEvidenceAssessmentResponse(item.PublicId, item.Outcome.ToString(), item.Comment, item.AssessedByUserId, item.AssessedByUser?.FullName, item.AssessedAt, item.CorrelationId)).ToArray(),
            RowVersion = Convert.ToBase64String(file.RowVersion),
            ReplacementOf = file.ReplacementAsNew == null ? null : ToReplacementResponse(file.ReplacementAsNew),
            ReplacedBy = file.ReplacementsAsOld.OrderByDescending(item => item.ReplacedAt).Select(ToReplacementResponse).FirstOrDefault()
            , LegalHolds = file.LegalHoldEvents.GroupBy(item => item.HoldId).Select(group => ToLegalHoldResponse(group.OrderBy(item => item.OccurredAt).ToArray())).OrderByDescending(item => item.PlacedAt).ToArray(),
            IsActive = file.IsActive,
            Disposals = file.DisposalEvents.GroupBy(item => item.DisposalId).Select(group => ToDisposalResponse(group.OrderBy(item => item.OccurredAt).ToArray())).OrderByDescending(item => item.RequestedAt).ToArray()
            , IsContentDeleted = file.Blob.IsContentDeleted
        };

    private static PoeEvidenceReplacementResponse ToReplacementResponse(PoeEvidenceReplacement item) => new(item.PublicId, item.SupersededPoeFile.PublicId, item.SupersededPoeFile.FileName, item.ReplacementPoeFile.PublicId, item.ReplacementPoeFile.FileName, item.Reason, item.ReplacedByUserId, item.ReplacedByUser?.FullName, item.ReplacedAt, item.CorrelationId);
    private static PoeLegalHoldResponse ToLegalHoldResponse(PoeLegalHoldEvent[] events)
    {
        var placed = events.First(item => item.Action == PoeLegalHoldAction.Placed);
        var released = events.LastOrDefault(item => item.Action == PoeLegalHoldAction.Released);
        return new(placed.HoldId, placed.HoldReference, released == null, placed.Reason, placed.ActorUserId, placed.ActorUser?.FullName, placed.OccurredAt, released?.Reason, released?.ActorUserId, released?.ActorUser?.FullName, released?.OccurredAt);
    }
    private static PoeDisposalResponse ToDisposalResponse(PoeDisposalEvent[] events)
    {
        var requested = events.First(item => item.Action == PoeDisposalAction.Requested);
        var completed = events.LastOrDefault(item => item.Action == PoeDisposalAction.Completed);
        var failed = events.LastOrDefault(item => item.Action == PoeDisposalAction.Failed);
        var status = completed != null ? "Completed" : failed != null ? "Failed" : "Pending";
        return new(requested.DisposalId, status, requested.ApprovalReference, requested.Reason, requested.ActorUserId, requested.ActorUser?.FullName, requested.OccurredAt, completed?.OccurredAt, failed?.OccurredAt, completed?.Detail ?? failed?.Detail);
    }
}

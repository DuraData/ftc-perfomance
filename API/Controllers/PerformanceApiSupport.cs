using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

internal static class PerformanceApiSupport
{
    public static string? GetCurrentUserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetIpAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString();

    public static string BuildPublicFileUrl(HttpContext context, string storagePath)
    {
        var normalized = storagePath.Replace("\\", "/").TrimStart('/');
        return $"{context.Request.Scheme}://{context.Request.Host}/{normalized}";
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
            target.IndicatorNumber,
            target.TargetName,
            target.KpiDescription,
            target.DepartmentId,
            target.Department?.Name,
            target.UnitId,
            target.Unit?.Name,
            target.AssignedUserId,
            target.AssignedUser != null ? target.AssignedUser.FullName : null,
            target.KpiId,
            target.SourceTemplateId,
            target.SourceTemplateVersion,
            target.Baseline,
            target.AnnualTarget,
            target.Weight,
            target.IsArchived,
            target.CreatedAt);

    public static IpmsTargetResponse ToResponse(this IpmsTarget target) =>
        new(
            target.Id,
            target.IndicatorNumber,
            target.TargetName,
            target.KpiDescription,
            target.DepartmentId,
            target.Department?.Name,
            target.UnitId,
            target.Unit?.Name,
            target.AssignedUserId,
            target.AssignedUser != null ? target.AssignedUser.FullName : null,
            target.RelatedOpmsTargetId,
            target.KpiId,
            target.SourceTemplateId,
            target.SourceTemplateVersion,
            target.AnnualTarget,
            target.Weight,
            target.IsArchived,
            target.CreatedAt);

    public static OpmsSubmissionResponse ToResponse(this OpmsSubmission submission) =>
        new(
            submission.Id,
            submission.OpmsTargetId,
            submission.OpmsTarget.TargetName,
            submission.Quarter,
            submission.Status,
            submission.Actual,
            submission.ActualDescription,
            submission.VarianceReason,
            submission.CorrectiveMeasure,
            submission.SubmittedAt,
            submission.SubmittedByUserId,
            submission.SubmittedByUser != null ? submission.SubmittedByUser.FullName : null,
            submission.DueDate,
            submission.CreatedAt);

    public static IpmsSubmissionResponse ToResponse(this IpmsSubmission submission) =>
        new(
            submission.Id,
            submission.IpmsTargetId,
            submission.IpmsTarget.TargetName,
            submission.Quarter,
            submission.Status,
            submission.Actual,
            submission.ActualDescription,
            submission.VarianceReason,
            submission.CorrectiveMeasure,
            submission.SubmittedAt,
            submission.SubmittedByUserId,
            submission.SubmittedByUser != null ? submission.SubmittedByUser.FullName : null,
            submission.DueDate,
            submission.CreatedAt);

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
            audit.EntityName,
            audit.EntityId,
            audit.Action,
            audit.OldValue,
            audit.NewValue,
            audit.ChangedBy,
            audit.ChangedAt,
            audit.IpAddress);

    public static PoeFileResponse ToResponse(this PoeFile file, HttpContext context) =>
        new(
            file.Id,
            file.SubmissionKind.ToString(),
            file.SubmissionId,
            file.FileName,
            file.ContentType,
            file.SizeInBytes,
            file.UploadedByUserId,
            file.UploadedByUser?.FullName,
            file.UploadedAt,
            BuildPublicFileUrl(context, file.StoragePath));
}

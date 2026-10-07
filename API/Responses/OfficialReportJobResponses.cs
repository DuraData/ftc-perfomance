using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Responses;

public sealed record OfficialReportScheduleResponse(
    Guid PublicId, Guid ScheduleFamilyPublicId, int VersionNumber, Guid? PreviousVersionPublicId,
    Guid TemplatePublicId, string TemplateName, OfficialReportType ReportType,
    Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, Guid ReportingPeriodPublicId, string ReportingPeriodCode,
    Guid? DepartmentPublicId, string? DepartmentName, Guid? UnitPublicId, string? UnitName,
    string Code, string Name, OfficialReportScheduleCadence Cadence, int Interval, DateTime? NextRunAt, DateTime? EffectiveTo,
    OfficialReportRecipientKind RecipientKind, string[] RecipientValues, string[] Channels, bool IsMandatory,
    bool IsCurrent, bool IsActive, string ApprovalReference, string Reason, string? CreatedBy, DateTime CreatedAt, string RowVersion);

public sealed record OfficialReportJobResponse(
    Guid PublicId, Guid? SchedulePublicId, string? ScheduleName, OfficialReportJobState State,
    Guid TemplatePublicId, string TemplateName, OfficialReportType ReportType,
    Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, Guid ReportingPeriodPublicId, string ReportingPeriodCode,
    Guid? DepartmentPublicId, string? DepartmentName, Guid? UnitPublicId, string? UnitName,
    DateTime ScheduledFor, DateTime AvailableAt, int AttemptCount, DateTime? StartedAt, DateTime? CompletedAt, string? LastError,
    string? RequestedBy, DateTime RequestedAt, Guid? GenerationPublicId, string? FileName, Guid? DistributionOutboxPublicId,
    string[] RecipientUserIds, string[] Channels, bool IsMandatoryDistribution, string? RetryReason, string RowVersion);

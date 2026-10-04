using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Requests;

public sealed record QueueOfficialReportJobRequest(
    Guid TemplatePublicId,
    Guid MunicipalityFinancialYearPublicId,
    Guid ReportingPeriodPublicId,
    Guid? PreviousGenerationPublicId = null,
    Guid? DepartmentPublicId = null,
    Guid? UnitPublicId = null);

public sealed record SaveOfficialReportScheduleRequest(
    Guid? PreviousVersionPublicId,
    string? PreviousVersionRowVersion,
    Guid TemplatePublicId,
    Guid MunicipalityFinancialYearPublicId,
    Guid ReportingPeriodPublicId,
    Guid? DepartmentPublicId,
    Guid? UnitPublicId,
    string Code,
    string Name,
    OfficialReportScheduleCadence Cadence,
    int Interval,
    DateTime? NextRunAt,
    DateTime? EffectiveTo,
    OfficialReportRecipientKind RecipientKind,
    IReadOnlyList<string> RecipientValues,
    IReadOnlyList<string> Channels,
    bool IsMandatory,
    bool IsActive,
    string ApprovalReference,
    string Reason);

public sealed record RetryOfficialReportJobRequest(string Reason, string RowVersion);

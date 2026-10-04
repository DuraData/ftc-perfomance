using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Requests;

public sealed record SaveOfficialReportTemplateRequest(
    Guid? PreviousVersionPublicId,
    string? PreviousVersionRowVersion,
    Guid? MunicipalityFinancialYearPublicId,
    SubmissionKind SubmissionKind,
    OfficialReportType ReportType,
    string Code,
    string Name,
    OfficialReportFormat Format,
    string HeadingTemplate,
    string[] Columns,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string ApprovalReference,
    string Reason);

public sealed record GenerateOfficialReportRequest(
    Guid TemplatePublicId,
    Guid MunicipalityFinancialYearPublicId,
    Guid ReportingPeriodPublicId,
    Guid? PreviousGenerationPublicId,
    Guid? DepartmentPublicId = null,
    Guid? UnitPublicId = null);

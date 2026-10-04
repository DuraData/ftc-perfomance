using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Responses;

public sealed record OfficialReportTemplateResponse(
    Guid PublicId, Guid TemplateFamilyPublicId, Guid? MunicipalityFinancialYearPublicId, string? FinancialYearCode,
    SubmissionKind SubmissionKind, OfficialReportType ReportType, string Code, string Name, OfficialReportFormat Format, int VersionNumber,
    string HeadingTemplate, string[] Columns, bool IsCurrent, bool IsActive, DateTime EffectiveFrom,
    DateTime? EffectiveTo, string ApprovalReference, string Reason, DateTime CreatedAt, string RowVersion);

public sealed record OfficialReportGenerationResponse(
    Guid PublicId, Guid GenerationFamilyPublicId, int VersionNumber, Guid TemplatePublicId, string TemplateCode,
    string TemplateName, int TemplateVersion, OfficialReportFormat Format, SubmissionKind SubmissionKind, OfficialReportType ReportType,
    Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, Guid ReportingPeriodPublicId,
    string ReportingPeriodCode, string ScopeJson, string FilterJson, string DataVersionReference, string FileName,
    string ContentType, long SizeInBytes, string Sha256, int RowCount, string GeneratedBy, DateTime GeneratedAt,
    string DownloadUrl);

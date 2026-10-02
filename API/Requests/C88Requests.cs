using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Requests;

public sealed record SaveC88CatalogueVersionRequest(string Code, string Name, DateTime EditionDate, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPublished, bool IsActive, string Reason, string? RowVersion);
public sealed record SaveC88CatalogueItemRequest(Guid CatalogueVersionPublicId, C88CatalogueItemKind Kind, string Code, string Name, string? Description, Guid? ParentItemPublicId, int DisplayOrder, bool IsActive, string Reason, string? RowVersion);
public sealed record C88DataElementInput(string Code, string Name, string? Description, C88ValueType ValueType, bool IsRequired, int Sequence);
public sealed record C88ApplicabilityInput(Guid MunicipalCategoryPublicId, Guid? ReadinessTierPublicId, bool IsApplicable, string? Notes);
public sealed record SaveC88IndicatorRequest(Guid CatalogueVersionPublicId, string Code, string Name, string Definition, string OfficialTechnicalIndicatorDescription, Guid? SectorPublicId, Guid? OutcomePublicId, Guid? IndicatorTypePublicId, C88ValueType ValueType, C88ControlledCalculationOperator CalculationOperator, string? OfficialFormulaText, bool RequiresBaseline, bool RequiresMediumTermTarget, bool RequiresAnnualTarget, bool IsActive, C88DataElementInput[] DataElements, C88ApplicabilityInput[] Applicability, string Reason);
public sealed record SaveC88ComplianceQuestionRequest(Guid CatalogueVersionPublicId, Guid ReportTypePublicId, Guid ResponseTypePublicId, string Code, string Prompt, bool IsRequired, int Sequence, bool IsActive, string Reason);
public sealed record ConfigureC88Request(Guid MunicipalityFinancialYearPublicId, Guid CatalogueVersionPublicId, bool IsEnabled, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion);
public sealed record SaveC88IndicatorPlanRequest(Guid ConfigurationPublicId, Guid IndicatorPublicId, string? BaselineValue, string? MediumTermTarget, string? AnnualTarget, string? MissingDataExplanation, DateTime? EstimatedAvailability, string Reason, string? RowVersion);
public sealed record SaveC88ReportingCalendarRequest(Guid ConfigurationPublicId, Guid ReportTypePublicId, Guid? ReportingPeriodPublicId, string Code, string Name, DateTime OpensAt, DateTime ClosesAt, DateTime DueAt, bool IsActive, string Reason, string? RowVersion);
public sealed record SaveC88AssignmentRequest(Guid ConfigurationPublicId, Guid IndicatorPublicId, Guid EmployeePublicId, C88AssignmentRole Role, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsActive, string Reason, string? RowVersion);
public sealed record C88WorkflowStageInput(int Sequence, C88WorkflowStageKind Kind, string Name, C88AssignmentRole RequiredRole, bool IsActive);
public sealed record SaveC88WorkflowRequest(Guid ConfigurationPublicId, DateTime EffectiveFrom, DateTime? EffectiveTo, C88WorkflowStageInput[] Stages, string Reason, Guid? PreviousWorkflowPublicId, string? PreviousWorkflowRowVersion);
public sealed record SaveC88MappingRequest(Guid ConfigurationPublicId, Guid IndicatorPublicId, Guid OpmsTargetPublicId, C88MappingType MappingType, string Reason, bool IsActive, string? RowVersion);
public sealed record C88DataElementValueInput(Guid DataElementPublicId, string? Value, string? MissingDataExplanation, DateTime? EstimatedAvailability);
public sealed record C88ComplianceResponseInput(Guid QuestionPublicId, string? Response, string? Comment);
public sealed record CreateC88ReportVersionRequest(Guid ConfigurationPublicId, Guid CalendarPublicId, Guid IndicatorPublicId, Guid? PreviousReportPublicId, string? PreviousReportRowVersion, string? MissingDataExplanation, DateTime? EstimatedAvailability, C88DataElementValueInput[] DataElementValues, C88ComplianceResponseInput[] ComplianceResponses, string Reason);
public sealed record C88WorkflowCommandRequest(string RowVersion, string Reason);

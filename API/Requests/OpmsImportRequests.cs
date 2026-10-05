using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.API.Requests;

public sealed record StageOpmsImportRequest(Guid ClientRequestId, string SourceFileName, OpmsImportRowRequest[] Rows);
public sealed record OpmsImportRowRequest(
    int SourceRowNumber, string IndicatorNumber, int OrderNumber, string TargetName, string KpiDescription,
    string DepartmentCode, string? UnitCode, string NationalKpa, string MunicipalKpa, string BackToBasicsPillar,
    string StrategicGoal, string StrategicIntervention, string StrategicObjective, string PerformanceObjective,
    decimal Baseline, decimal Weight, string KpiType, string IndicatorType, SaveTargetPeriodValueRequest[] PeriodTargets,
    string? ExistingIndicatorNumber = null);
public sealed record CommitOpmsImportRequest(string Reason, string? ApprovalReference, DateTime? EffectiveAt, string RowVersion);

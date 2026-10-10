namespace FTCERP.Host.API.Responses;

public sealed record PerformanceDashboardResponse(
    int TotalTargets,
    int ActiveTargets,
    int CompletedTargets,
    int OverdueTargets,
    int AtRiskTargets,
    int OutstandingTargets,
    int DraftSubmissions,
    int SubmittedSubmissions,
    int ReturnedSubmissions,
    int ApprovedSubmissions,
    int PendingVerification,
    int PendingApproval,
    Guid? MunicipalityFinancialYearPublicId = null,
    string? FinancialYearCode = null,
    string? FinancialYearName = null,
    Guid? ReportingPeriodPublicId = null,
    string? ReportingPeriodCode = null,
    string? ReportingPeriodName = null,
    string? ReportingWindowState = null,
    DateTime? ReportingWindowOpensAt = null,
    DateTime? ReportingWindowClosesAt = null,
    IReadOnlyCollection<PerformanceDashboardRatingBreakdownResponse>? RatingBreakdown = null,
    IReadOnlyCollection<PerformanceDashboardTeamBreakdownResponse>? TeamBreakdown = null,
    IReadOnlyCollection<PerformanceDashboardPeriodBreakdownResponse>? PeriodBreakdown = null);

public sealed record PerformanceDashboardRatingBreakdownResponse(
    string Label,
    int Count);

public sealed record PerformanceDashboardTeamBreakdownResponse(
    Guid? DepartmentPublicId,
    string DepartmentName,
    int TargetCount,
    int AchievedCount,
    int AtRiskCount);

public sealed record PerformanceDashboardPeriodBreakdownResponse(
    Guid ReportingPeriodPublicId,
    string Code,
    string Name,
    int Sequence,
    string WindowState,
    DateTime? OpensAt,
    DateTime? ClosesAt,
    int SubmissionCount,
    int AchievedCount,
    int AtRiskCount,
    int OutstandingCount);

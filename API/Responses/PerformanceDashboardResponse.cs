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
    int PendingApproval);

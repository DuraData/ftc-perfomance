namespace FTCERP.Host.API.Responses;

public sealed record WorkflowQueueCountsResponse(
    int MySubmissions,
    int Verification,
    int Approval,
    int Pms,
    int Auditor,
    int Returned,
    int MyDrafts,
    int PendingSubmission,
    int MyReturned,
    int UnderVerification,
    int UnderReview,
    int UnderApproval,
    int InternalAuditReturned,
    int ApprovedClosed)
{
    public static WorkflowQueueCountsResponse Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record WorkflowQueueItemResponse(
    string Id,
    Guid PublicId,
    string Kind,
    string TargetId,
    Guid TargetPublicId,
    string TargetName,
    string IndicatorNumber,
    string Quarter,
    DateTime? DueDate,
    string Status,
    Guid? SubmittedByUserPublicId,
    string? SubmittedByName,
    string? VerifierName,
    string? ApproverName,
    DateTime CreatedAt);

public sealed record WorkflowQueueResponse(
    string Queue,
    WorkflowQueueCountsResponse Counts,
    PagedResponse<WorkflowQueueItemResponse> Page);

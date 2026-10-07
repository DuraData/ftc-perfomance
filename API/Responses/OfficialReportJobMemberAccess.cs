namespace FTCERP.Host.API.Responses;

public sealed record OfficialReportJobMemberAccess(
    bool ScheduleRecipientValues,
    bool ScheduleCreatedBy,
    bool JobRequestedBy,
    bool JobLastError,
    bool JobDistributionOutboxPublicId,
    bool JobRecipientUserIds,
    bool JobRetryReason)
{
    public static OfficialReportJobMemberAccess None { get; } = new(false, false, false, false, false, false, false);
    public static OfficialReportJobMemberAccess Full { get; } = new(true, true, true, true, true, true, true);
}

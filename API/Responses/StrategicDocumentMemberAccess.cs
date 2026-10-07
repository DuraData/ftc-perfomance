namespace FTCERP.Host.API.Responses;

public sealed record StrategicDocumentMemberAccess(
    bool CreatedByUserId,
    bool ApprovedByUserId,
    bool PublishedByUserId,
    bool EventActorUserId,
    bool EventReason,
    bool ScannerProvider,
    bool ScannerReference,
    bool ScanDetail)
{
    public static StrategicDocumentMemberAccess Full { get; } = new(true, true, true, true, true, true, true, true);
}

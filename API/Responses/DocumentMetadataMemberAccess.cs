namespace FTCERP.Host.API.Responses;

public sealed record DocumentMetadataMemberAccess(
    bool UploadedByUserId,
    bool UploadedByName,
    bool ScannerProvider,
    bool ScannerReference,
    bool ScanDetail)
{
    public static DocumentMetadataMemberAccess Full { get; } = new(true, true, true, true, true);
}

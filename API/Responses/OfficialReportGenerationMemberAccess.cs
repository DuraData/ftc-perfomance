namespace FTCERP.Host.API.Responses;

public sealed record OfficialReportGenerationMemberAccess(
    bool ScopeJson,
    bool FilterJson,
    bool DataVersionReference,
    bool GeneratedBy)
{
    public static OfficialReportGenerationMemberAccess None { get; } = new(false, false, false, false);
    public static OfficialReportGenerationMemberAccess Full { get; } = new(true, true, true, true);
}

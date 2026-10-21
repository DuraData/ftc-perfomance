namespace FTCERP.Host.API.Responses;

public sealed record RfiQueueCountsResponse(int Open, int Responded, int Overdue, int Closed)
{
    public static RfiQueueCountsResponse Empty { get; } = new(0, 0, 0, 0);
}

public sealed record RfiQueueItemResponse(
    Guid PublicId,
    Guid SubmissionPublicId,
    string Kind,
    Guid TargetPublicId,
    string TargetName,
    string IndicatorNumber,
    string Period,
    Guid? MunicipalityFinancialYearPublicId,
    string? Question,
    DateTime RaisedAt,
    DateTime ResponseDueAt,
    string Status,
    bool CanRespond,
    bool CanClose);

public sealed record RfiQueueResponse(
    RfiQueueCountsResponse Counts,
    PagedResponse<RfiQueueItemResponse> Page);

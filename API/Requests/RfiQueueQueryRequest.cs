using System.ComponentModel.DataAnnotations;

namespace FTCERP.Host.API.Requests;

public sealed class RfiQueueQueryRequest
{
    private const string KindPattern = "^(?i:all|opms|ipms)$";
    private const string StatusPattern = "^(?i:all|open|responded|overdue|closed)$";
    private const string SortPattern = "^(?i:raisedAt|dueAt|targetName|indicatorNumber|period|status)$";
    private const string SortDirectionPattern = "^(?i:asc|desc)$";

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;

    [StringLength(200)]
    public string? Search { get; init; }

    public Guid? MunicipalityFinancialYearPublicId { get; init; }

    [RegularExpression(KindPattern, ErrorMessage = "Framework must be all, OPMS or IPMS.")]
    public string Kind { get; init; } = "all";

    [StringLength(50)]
    public string? Period { get; init; }

    [RegularExpression(StatusPattern, ErrorMessage = "RFI status is not supported.")]
    public string Status { get; init; } = "all";

    [RegularExpression(SortPattern, ErrorMessage = "Sort field is not supported.")]
    public string SortBy { get; init; } = "dueAt";

    [RegularExpression(SortDirectionPattern, ErrorMessage = "Sort direction must be asc or desc.")]
    public string SortDirection { get; init; } = "asc";

    internal int Offset => checked((Page - 1) * PageSize);
    internal string? NormalizedSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    internal string NormalizedKind => Kind.Trim().ToLowerInvariant();
    internal string? NormalizedPeriod => string.IsNullOrWhiteSpace(Period) ? null : Period.Trim();
    internal string NormalizedStatus => Status.Trim().ToLowerInvariant();
    internal string NormalizedSortBy => SortBy.Trim().ToLowerInvariant();
    internal bool SortDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

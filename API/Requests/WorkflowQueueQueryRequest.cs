using System.ComponentModel.DataAnnotations;

namespace FTCERP.Host.API.Requests;

public sealed class WorkflowQueueQueryRequest
{
    private const string QueuePattern = "^(?i:all|my-submissions|verification|approval|pms|auditor|returned|my-drafts|pending-submission|my-returned|under-verification|under-review|under-approval|internal-audit-returned|approved-closed)$";
    private const string KindPattern = "^(?i:all|opms|ipms)$";
    private const string StatusPattern = "^(?i:all|draft|submitted|pending_verification|verified|pending_approval|approved|reviewed|returned_for_info|verify_rejected|rejected|audited|completed|cancelled)$";
    private const string SortPattern = "^(?i:createdAt|dueDate|targetName|indicatorNumber|quarter|status)$";
    private const string SortDirectionPattern = "^(?i:asc|desc)$";

    [Required]
    [RegularExpression(QueuePattern, ErrorMessage = "Queue is not supported.")]
    public string Queue { get; init; } = "all";

    [Range(1, 10_000_000)]
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

    [RegularExpression(StatusPattern, ErrorMessage = "Status is not supported.")]
    public string Status { get; init; } = "all";

    [RegularExpression(SortPattern, ErrorMessage = "Sort field is not supported.")]
    public string SortBy { get; init; } = "createdAt";

    [RegularExpression(SortDirectionPattern, ErrorMessage = "Sort direction must be asc or desc.")]
    public string SortDirection { get; init; } = "desc";

    internal int Offset => checked((Page - 1) * PageSize);
    internal string NormalizedQueue => Queue.Trim().ToLowerInvariant();
    internal string? NormalizedSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    internal string NormalizedKind => Kind.Trim().ToLowerInvariant();
    internal string? NormalizedPeriod => string.IsNullOrWhiteSpace(Period) ? null : Period.Trim();
    internal string NormalizedStatus => Status.Trim().ToLowerInvariant();
    internal string NormalizedSortBy => SortBy.Trim().ToLowerInvariant();
    internal bool SortDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

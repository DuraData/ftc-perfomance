using System.ComponentModel.DataAnnotations;

namespace FTCERP.Host.API.Requests;

public sealed class WorkflowQueueQueryRequest
{
    private const string QueuePattern = "^(?i:all|my-submissions|verification|approval|pms|auditor|returned|my-drafts|pending-submission|my-returned|under-verification|under-review|under-approval|internal-audit-returned|approved-closed)$";

    [Required]
    [RegularExpression(QueuePattern, ErrorMessage = "Queue is not supported.")]
    public string Queue { get; init; } = "all";

    [Range(1, 10_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;

    public int Offset => checked((Page - 1) * PageSize);
    public string NormalizedQueue => Queue.Trim().ToLowerInvariant();
}

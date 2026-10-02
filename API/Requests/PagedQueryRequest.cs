using System.ComponentModel.DataAnnotations;

namespace FTCERP.Host.API.Requests;

public sealed class PagedQueryRequest
{
    [Range(1, 10_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;

    [StringLength(200)]
    public string? Search { get; init; }

    [StringLength(50)]
    public string? SortBy { get; init; }

    [RegularExpression("^(?i:asc|desc)$", ErrorMessage = "SortDirection must be 'asc' or 'desc'.")]
    public string SortDirection { get; init; } = "desc";

    public int Offset => checked((Page - 1) * PageSize);
    public bool Descending => string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
    public string NormalizedSearch => Search?.Trim() ?? string.Empty;
    public string NormalizedSortBy => SortBy?.Trim().ToLowerInvariant() ?? "createdat";
}

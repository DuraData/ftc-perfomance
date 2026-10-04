namespace FTCERP.Host.API.Responses;

public sealed record PagedResponse<T>(
    T[] Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static PagedResponse<T> Empty(int page, int pageSize) => new([], page, pageSize, 0, 0);

    public static PagedResponse<T> Create(IEnumerable<T> items, int page, int pageSize, int totalCount) =>
        new(items.ToArray(), page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
}

public sealed record PerformanceTargetOptionResponse(
    string Id,
    Guid PublicId,
    string IndicatorNumber,
    string TargetName,
    int? DepartmentId,
    string? DepartmentName,
    Guid? RelatedOpmsTargetPublicId = null);

public sealed record TargetLibraryFacetsResponse(
    string[] PrimaryAreas,
    string[] FunctionalAreas,
    string[] Classifications,
    string[] TargetUnitTypes,
    int[] Versions);

using System.ComponentModel.DataAnnotations;

namespace FTCERP.Host.API.Requests;

public sealed class TargetLibraryFilterRequest
{
    [RegularExpression("^(?i:all|active|archived)$", ErrorMessage = "Status must be all, active, or archived.")]
    public string Status { get; init; } = "all";

    [StringLength(120)]
    public string? PrimaryArea { get; init; }

    [StringLength(120)]
    public string? FunctionalArea { get; init; }

    [StringLength(120)]
    public string? Classification { get; init; }

    [StringLength(80)]
    public string? TargetUnitType { get; init; }

    [Range(1, int.MaxValue)]
    public int? Version { get; init; }

    public string NormalizedStatus => Status.Trim().ToLowerInvariant();
    public string NormalizedPrimaryArea => PrimaryArea?.Trim() ?? string.Empty;
    public string NormalizedFunctionalArea => FunctionalArea?.Trim() ?? string.Empty;
    public string NormalizedClassification => Classification?.Trim() ?? string.Empty;
    public string NormalizedTargetUnitType => TargetUnitType?.Trim() ?? string.Empty;
}

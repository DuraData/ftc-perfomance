namespace FTCERP.Host.API.Requests;

public sealed class UploadTidSourceDocumentRequest
{
    public IFormFile File { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
}

public record UpdateTidConfigurationRequest(
    bool TidEnabled,
    bool AllKpisRequired,
    string RowVersion,
    string Reason);

public record CreateTidVersionRequest(
    string IndicatorDefinition,
    string Purpose,
    string DataSource,
    string CollectionMethod,
    string CalculationMethod,
    string? NumeratorDescription,
    string? DenominatorDescription,
    string? Limitations,
    string? Assumptions,
    string VerificationMethod,
    Guid? ResponsibleEmployeePublicId,
    string? Notes,
    DateTime EffectiveFrom,
    string? PreviousVersionRowVersion,
    string Reason);

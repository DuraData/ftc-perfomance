namespace FTCERP.Host.API.Responses;

public record TidConfigurationResponse(
    Guid MunicipalityPublicId,
    bool TidEnabled,
    bool AllKpisRequired,
    int ScopedKpiCount,
    int CurrentTidCount,
    int MissingTidCount,
    string RowVersion);

public record TidRegisterItemResponse(
    Guid TargetPublicId,
    string IndicatorNumber,
    string TargetName,
    string? DepartmentName,
    string? UnitName,
    bool TidRequired,
    TidVersionResponse? CurrentVersion);

public record TidVersionResponse(
    Guid PublicId,
    Guid TargetPublicId,
    int VersionNumber,
    Guid? PreviousVersionPublicId,
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
    string? ResponsibleEmployeeName,
    string? Notes,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsCurrent,
    DateTime CreatedAt,
    string? CreatedByUserId,
    string RowVersion,
    TidSourceDocumentResponse[] SourceDocuments);

public record TidSourceDocumentResponse(
    Guid PublicId,
    string Title,
    string FileName,
    string? ContentType,
    long SizeInBytes,
    string Sha256,
    string ScanStatus,
    bool IsQuarantined,
    DateTime UploadedAt,
    string? UploadedByUserId,
    string? UploadedByName,
    string? ScannerProvider,
    string? ScannerReference,
    string? ScanDetail,
    string ContentUrl);

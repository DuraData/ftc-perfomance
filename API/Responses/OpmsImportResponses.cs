namespace FTCERP.Host.API.Responses;

public sealed record OpmsImportRowResponse(Guid PublicId, int SourceRowNumber, string Reference, string Status,
    string? ExistingValueJson, string? NormalizedJson, string? ErrorCode, string? ErrorPeriod, string? ErrorField,
    string? SuppliedValue, string? ErrorMessage);
public sealed record OpmsImportBatchResponse(Guid PublicId, Guid ClientRequestId, Guid SdbipLayerPublicId,
    string SourceFileName, string SourceSha256, string Status, int TotalRows, int NewRows, int UnchangedRows,
    int ChangedRows, int InvalidRows, DateTime CreatedAt, DateTime? CommittedAt, string RowVersion,
    OpmsImportRowResponse[] Rows);

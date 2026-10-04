namespace FTCERP.Host.Domain.Entities;

public enum OpmsImportBatchStatus { Staged = 0, Committed = 1 }
public enum OpmsImportRowStatus { New = 0, Unchanged = 1, Changed = 2, Invalid = 3 }

public sealed class OpmsImportBatch
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid ClientRequestId { get; set; }
    public long MunicipalityId { get; set; }
    public long SdbipLayerId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string SourceSha256 { get; set; } = string.Empty;
    public OpmsImportBatchStatus Status { get; set; }
    public int TotalRows { get; set; }
    public int NewRows { get; set; }
    public int UnchangedRows { get; set; }
    public int ChangedRows { get; set; }
    public int InvalidRows { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CommittedByUserId { get; set; }
    public DateTime? CommittedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public SdbipLayer SdbipLayer { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? CommittedByUser { get; set; }
    public ICollection<OpmsImportRow> Rows { get; set; } = new List<OpmsImportRow>();
}

public sealed class OpmsImportRow
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long OpmsImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public string Reference { get; set; } = string.Empty;
    public OpmsImportRowStatus Status { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string? NormalizedJson { get; set; }
    public string? ExistingValueJson { get; set; }
    public byte[]? ExpectedEntityRowVersion { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorPeriod { get; set; }
    public string? ErrorField { get; set; }
    public string? SuppliedValue { get; set; }
    public string? ErrorMessage { get; set; }
    public OpmsImportBatch OpmsImportBatch { get; set; } = null!;
}

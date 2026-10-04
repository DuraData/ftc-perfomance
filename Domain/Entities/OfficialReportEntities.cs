namespace FTCERP.Host.Domain.Entities;

public enum OfficialReportFormat
{
    Csv = 1,
    Xlsx = 2,
    Docx = 3,
    Pdf = 4
}

public enum OfficialReportType
{
    QuarterlyPerformance = 1,
    MidTermPerformance = 2,
    AnnualPerformance = 3,
    DepartmentalPerformance = 4,
    UnitPerformance = 5,
    PerformanceSummary = 6,
    WorkflowStatus = 7,
    SubmissionRegister = 8,
    VerificationRegister = 9,
    ApprovalRegister = 10,
    PmsReview = 11,
    InternalAudit = 12,
    OutstandingRfi = 13,
    EvidenceRegister = 14,
    AuditTrail = 15,
    VersionTrail = 16
}

public enum OfficialReportScheduleCadence
{
    Once = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4
}

public enum OfficialReportRecipientKind
{
    User = 1,
    Role = 2
}

public enum OfficialReportJobState
{
    Queued = 1,
    Processing = 2,
    RetryPending = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

/// <summary>
/// An approved municipality report layout. Published versions are immutable; a change creates
/// another row in the same TemplateFamilyPublicId lineage.
/// </summary>
public sealed class OfficialReportTemplate
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid TemplateFamilyPublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long? MunicipalityFinancialYearId { get; set; }
    public long? PreviousVersionId { get; set; }
    public SubmissionKind SubmissionKind { get; set; }
    public OfficialReportType ReportType { get; set; } = OfficialReportType.QuarterlyPerformance;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OfficialReportFormat Format { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string HeadingTemplate { get; set; } = "{FinancialYear} {Period} PERFORMANCE REPORT";
    public string ColumnConfigurationJson { get; set; } = "[]";
    public bool IsCurrent { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string ApprovalReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear? MunicipalityFinancialYear { get; set; }
    public OfficialReportTemplate? PreviousVersion { get; set; }
    public ICollection<OfficialReportTemplate> LaterVersions { get; set; } = new List<OfficialReportTemplate>();
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<OfficialReportGeneration> Generations { get; set; } = new List<OfficialReportGeneration>();
}

/// <summary>An immutable official output and the exact authorization/data snapshot that produced it.</summary>
public sealed class OfficialReportGeneration
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid GenerationFamilyPublicId { get; set; }
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long ReportingPeriodId { get; set; }
    public long ReportTemplateId { get; set; }
    public string EvidenceBlobId { get; set; } = string.Empty;
    public SubmissionKind SubmissionKind { get; set; }
    public OfficialReportType ReportType { get; set; } = OfficialReportType.QuarterlyPerformance;
    public int VersionNumber { get; set; }
    public string ScopeJson { get; set; } = "{}";
    public string FilterJson { get; set; } = "{}";
    public string DataVersionReference { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string GeneratedByUserId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
    public OfficialReportTemplate ReportTemplate { get; set; } = null!;
    public EvidenceBlob Blob { get; set; } = null!;
    public ApplicationUser GeneratedByUser { get; set; } = null!;
}

/// <summary>A versioned, municipality-approved instruction for generating and distributing an official report.</summary>
public sealed class OfficialReportSchedule
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid ScheduleFamilyPublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long ReportTemplateId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long ReportingPeriodId { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public long? PreviousVersionId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OfficialReportScheduleCadence Cadence { get; set; }
    public int Interval { get; set; } = 1;
    public DateTime? NextRunAt { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public OfficialReportRecipientKind RecipientKind { get; set; }
    public string RecipientValuesCsv { get; set; } = string.Empty;
    public string ChannelsCsv { get; set; } = "IN_APP";
    public bool IsMandatory { get; set; } = true;
    public bool IsCurrent { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string ApprovalReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public OfficialReportTemplate ReportTemplate { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
    public Department? Department { get; set; }
    public Unit? Unit { get; set; }
    public OfficialReportSchedule? PreviousVersion { get; set; }
    public ICollection<OfficialReportSchedule> LaterVersions { get; set; } = new List<OfficialReportSchedule>();
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<OfficialReportJob> Jobs { get; set; } = new List<OfficialReportJob>();
}

/// <summary>A durable asynchronous execution record. Completed jobs retain links to generation and delivery evidence.</summary>
public sealed class OfficialReportJob
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long? OfficialReportScheduleId { get; set; }
    public long ReportTemplateId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long ReportingPeriodId { get; set; }
    public int? DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public long? PreviousOfficialReportGenerationId { get; set; }
    public long? OfficialReportGenerationId { get; set; }
    public long? DistributionOutboxId { get; set; }
    public OfficialReportJobState State { get; set; } = OfficialReportJobState.Queued;
    public DateTime ScheduledFor { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string RecipientUserIdsCsv { get; set; } = string.Empty;
    public string ChannelsCsv { get; set; } = string.Empty;
    public bool IsMandatoryDistribution { get; set; }
    public string? RetryReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public OfficialReportSchedule? OfficialReportSchedule { get; set; }
    public OfficialReportTemplate ReportTemplate { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ReportingPeriod ReportingPeriod { get; set; } = null!;
    public Department? Department { get; set; }
    public Unit? Unit { get; set; }
    public OfficialReportGeneration? PreviousOfficialReportGeneration { get; set; }
    public OfficialReportGeneration? OfficialReportGeneration { get; set; }
    public BusinessEventOutbox? DistributionOutbox { get; set; }
    public ApplicationUser RequestedByUser { get; set; } = null!;
}

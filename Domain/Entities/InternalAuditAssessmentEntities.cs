namespace FTCERP.Host.Domain.Entities;

public enum InternalAuditAssessmentModel
{
    Detailed = 1,
    SatisfactoryNotSatisfactory = 2
}

public enum InternalAuditAssessmentOutcome
{
    Achieved = 1,
    NotAchieved = 2,
    Satisfactory = 3,
    NotSatisfactory = 4
}

/// <summary>An immutable, effective-dated tenant/financial-year model selection.</summary>
public sealed class InternalAuditAssessmentConfiguration
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public InternalAuditAssessmentModel Model { get; set; }
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

/// <summary>Append-only Internal Audit assessment and reassessment history.</summary>
public sealed class InternalAuditAssessment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long SubmissionWorkflowInstanceId { get; set; }
    public long ConfigurationId { get; set; }
    public long? PreviousAssessmentId { get; set; }
    public long? PerformanceRfiId { get; set; }
    public InternalAuditAssessmentOutcome Outcome { get; set; }
    public string DetailedObservation { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? Findings { get; set; }
    public string? Recommendation { get; set; }
    public decimal? Score { get; set; }
    public string AssessedByUserId { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public SubmissionWorkflowInstance SubmissionWorkflowInstance { get; set; } = null!;
    public InternalAuditAssessmentConfiguration Configuration { get; set; } = null!;
    public InternalAuditAssessment? PreviousAssessment { get; set; }
    public ICollection<InternalAuditAssessment> Reassessments { get; set; } = new List<InternalAuditAssessment>();
    public PerformanceRfi? PerformanceRfi { get; set; }
    public ApplicationUser AssessedByUser { get; set; } = null!;
}

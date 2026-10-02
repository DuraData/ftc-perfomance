namespace FTCERP.Host.Domain.Entities;

public enum C88CatalogueItemKind
{
    Sector = 1,
    Outcome = 2,
    IndicatorType = 3,
    MunicipalCategory = 4,
    ReadinessTier = 5,
    ReportType = 6,
    ResponseType = 7
}

public enum C88ControlledCalculationOperator
{
    None = 0,
    Sum = 1,
    Average = 2,
    Ratio = 3,
    Percentage = 4,
    Difference = 5
}

public enum C88ValueType { Decimal = 1, Integer = 2, Percentage = 3, Boolean = 4, Text = 5 }
public enum C88AssignmentRole { PrimaryCapturer = 1, Contributor = 2, ReviewerVerifier = 3, FinalSubmitter = 4 }
public enum C88MappingType { Direct = 1, Contributing = 2 }
public enum C88WorkflowStageKind { Capturer = 1, ReviewerVerifier = 2, FinalSubmission = 3 }
public enum C88ReportState { Draft = 1, Submitted = 2, Verified = 3, FinalSubmitted = 4, Rework = 5 }
public enum C88WorkflowActionKind { Created = 1, Submitted = 2, Verified = 3, Returned = 4, FinalSubmitted = 5 }

public class C88CatalogueVersion
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime EditionDate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<C88CatalogueItem> Items { get; set; } = new List<C88CatalogueItem>();
    public ICollection<C88Indicator> Indicators { get; set; } = new List<C88Indicator>();
    public ICollection<C88ComplianceQuestion> ComplianceQuestions { get; set; } = new List<C88ComplianceQuestion>();
}

public class C88MunicipalityConfiguration
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalityFinancialYearId { get; set; }
    public long C88CatalogueVersionId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
    public C88CatalogueVersion CatalogueVersion { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

public class C88CatalogueItem
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88CatalogueVersionId { get; set; }
    public long? ParentItemId { get; set; }
    public C88CatalogueItemKind Kind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88CatalogueVersion CatalogueVersion { get; set; } = null!;
    public C88CatalogueItem? ParentItem { get; set; }
    public ICollection<C88CatalogueItem> ChildItems { get; set; } = new List<C88CatalogueItem>();
}

public class C88Indicator
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88CatalogueVersionId { get; set; }
    public long? SectorItemId { get; set; }
    public long? OutcomeItemId { get; set; }
    public long? IndicatorTypeItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string OfficialTechnicalIndicatorDescription { get; set; } = string.Empty;
    public C88ValueType ValueType { get; set; }
    public C88ControlledCalculationOperator CalculationOperator { get; set; }
    public string? OfficialFormulaText { get; set; }
    public bool RequiresBaseline { get; set; }
    public bool RequiresMediumTermTarget { get; set; }
    public bool RequiresAnnualTarget { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88CatalogueVersion CatalogueVersion { get; set; } = null!;
    public C88CatalogueItem? SectorItem { get; set; }
    public C88CatalogueItem? OutcomeItem { get; set; }
    public C88CatalogueItem? IndicatorTypeItem { get; set; }
    public ICollection<C88DataElement> DataElements { get; set; } = new List<C88DataElement>();
    public ICollection<C88IndicatorApplicability> Applicability { get; set; } = new List<C88IndicatorApplicability>();
}

public class C88DataElement
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88IndicatorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public C88ValueType ValueType { get; set; }
    public bool IsRequired { get; set; }
    public int Sequence { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88Indicator Indicator { get; set; } = null!;
}

public class C88IndicatorApplicability
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88IndicatorId { get; set; }
    public long MunicipalCategoryItemId { get; set; }
    public long? ReadinessTierItemId { get; set; }
    public bool IsApplicable { get; set; } = true;
    public string? Notes { get; set; }

    public Municipality Municipality { get; set; } = null!;
    public C88Indicator Indicator { get; set; } = null!;
    public C88CatalogueItem MunicipalCategoryItem { get; set; } = null!;
    public C88CatalogueItem? ReadinessTierItem { get; set; }
}

public class C88ComplianceQuestion
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88CatalogueVersionId { get; set; }
    public long ReportTypeItemId { get; set; }
    public long ResponseTypeItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88CatalogueVersion CatalogueVersion { get; set; } = null!;
    public C88CatalogueItem ReportTypeItem { get; set; } = null!;
    public C88CatalogueItem ResponseTypeItem { get; set; } = null!;
}

public class C88IndicatorPlan
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88MunicipalityConfigurationId { get; set; }
    public long C88IndicatorId { get; set; }
    public string? BaselineValue { get; set; }
    public string? MediumTermTarget { get; set; }
    public string? AnnualTarget { get; set; }
    public string? MissingDataExplanation { get; set; }
    public DateTime? EstimatedAvailability { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88MunicipalityConfiguration Configuration { get; set; } = null!;
    public C88Indicator Indicator { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

public class C88ReportingCalendar
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88MunicipalityConfigurationId { get; set; }
    public long ReportTypeItemId { get; set; }
    public long? ReportingPeriodId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime DueAt { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88MunicipalityConfiguration Configuration { get; set; } = null!;
    public C88CatalogueItem ReportTypeItem { get; set; } = null!;
    public ReportingPeriod? ReportingPeriod { get; set; }
}

public class C88IndicatorReport
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid ReportFamilyId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88MunicipalityConfigurationId { get; set; }
    public long C88ReportingCalendarId { get; set; }
    public long C88IndicatorId { get; set; }
    public long C88WorkflowDefinitionId { get; set; }
    public long? PreviousVersionId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public C88ReportState State { get; set; } = C88ReportState.Draft;
    public int CurrentStageSequence { get; set; } = 1;
    public string? CalculatedValue { get; set; }
    public string? MissingDataExplanation { get; set; }
    public DateTime? EstimatedAvailability { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime? FinalSubmittedAt { get; set; }
    public string? FinalSubmittedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88MunicipalityConfiguration Configuration { get; set; } = null!;
    public C88ReportingCalendar Calendar { get; set; } = null!;
    public C88Indicator Indicator { get; set; } = null!;
    public C88WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public C88IndicatorReport? PreviousVersion { get; set; }
    public ICollection<C88IndicatorReport> SuccessorVersions { get; set; } = new List<C88IndicatorReport>();
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? FinalSubmittedByUser { get; set; }
    public ICollection<C88DataElementValue> DataElementValues { get; set; } = new List<C88DataElementValue>();
    public ICollection<C88ComplianceResponse> ComplianceResponses { get; set; } = new List<C88ComplianceResponse>();
    public ICollection<C88WorkflowAction> WorkflowActions { get; set; } = new List<C88WorkflowAction>();
}

public class C88DataElementValue
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88IndicatorReportId { get; set; }
    public long C88DataElementId { get; set; }
    public string? Value { get; set; }
    public string? MissingDataExplanation { get; set; }
    public DateTime? EstimatedAvailability { get; set; }

    public Municipality Municipality { get; set; } = null!;
    public C88IndicatorReport IndicatorReport { get; set; } = null!;
    public C88DataElement DataElement { get; set; } = null!;
}

public class C88ComplianceResponse
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88IndicatorReportId { get; set; }
    public long C88ComplianceQuestionId { get; set; }
    public string? Response { get; set; }
    public string? Comment { get; set; }

    public Municipality Municipality { get; set; } = null!;
    public C88IndicatorReport IndicatorReport { get; set; } = null!;
    public C88ComplianceQuestion ComplianceQuestion { get; set; } = null!;
}

public class C88Assignment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88MunicipalityConfigurationId { get; set; }
    public long C88IndicatorId { get; set; }
    public long MunicipalEmployeeId { get; set; }
    public C88AssignmentRole Role { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88MunicipalityConfiguration Configuration { get; set; } = null!;
    public C88Indicator Indicator { get; set; } = null!;
    public MunicipalEmployee MunicipalEmployee { get; set; } = null!;
}

public class C88WorkflowDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88MunicipalityConfigurationId { get; set; }
    public long? PreviousVersionId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88MunicipalityConfiguration Configuration { get; set; } = null!;
    public C88WorkflowDefinition? PreviousVersion { get; set; }
    public ICollection<C88WorkflowDefinition> SuccessorVersions { get; set; } = new List<C88WorkflowDefinition>();
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<C88WorkflowStage> Stages { get; set; } = new List<C88WorkflowStage>();
}

public class C88WorkflowStage
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88WorkflowDefinitionId { get; set; }
    public int Sequence { get; set; }
    public C88WorkflowStageKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public C88AssignmentRole RequiredRole { get; set; }
    public bool IsActive { get; set; } = true;

    public Municipality Municipality { get; set; } = null!;
    public C88WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}

public class C88WorkflowAction
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88IndicatorReportId { get; set; }
    public int FromStageSequence { get; set; }
    public int ToStageSequence { get; set; }
    public C88WorkflowActionKind Action { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string SnapshotJson { get; set; } = string.Empty;

    public Municipality Municipality { get; set; } = null!;
    public C88IndicatorReport IndicatorReport { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}

public class C88OpmsMapping
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long C88MunicipalityConfigurationId { get; set; }
    public long C88IndicatorId { get; set; }
    public string OpmsTargetId { get; set; } = string.Empty;
    public C88MappingType MappingType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];

    public Municipality Municipality { get; set; } = null!;
    public C88MunicipalityConfiguration Configuration { get; set; } = null!;
    public C88Indicator Indicator { get; set; } = null!;
    public OpmsTarget OpmsTarget { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

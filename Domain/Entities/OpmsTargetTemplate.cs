namespace FTCERP.Host.Domain.Entities;

public class OpmsTargetTemplate
{
    public int Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string IndicatorNumber { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string KpiDescription { get; set; } = string.Empty;
    public decimal Baseline { get; set; }
    public decimal AnnualTarget { get; set; }
    public string? AnnualTargetDescription { get; set; }
    public string TargetUnitType { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; }
    public string? NationalKpa { get; set; }
    public string? MunicipalKpa { get; set; }
    public string? StrategicGoal { get; set; }
    public string? StrategicObjective { get; set; }
    public string? PerformanceObjective { get; set; }
    public string? Outcome { get; set; }
    public string? Output { get; set; }
    public string? PriorityIssue { get; set; }
    public string? BudgetSource { get; set; }
    public string? BudgetType { get; set; }
    public decimal Weight { get; set; }
    public string? KpiType { get; set; }
    public string? IndicatorType { get; set; }
    public string? FunctionalArea { get; set; }
    public string? StandardClassification { get; set; }
    public string? IdpReference { get; set; }
    public string? InternalReference { get; set; }
    public string? FmsLink { get; set; }
    public string? DefaultQuarterlyTargetsJson { get; set; }
    public string? DefaultBudgetInformation { get; set; }
    public string? DefaultPoeRequirements { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public int Version { get; set; } = 1;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<OpmsTargetTemplateVersion> Versions { get; set; } = new List<OpmsTargetTemplateVersion>();
}

public class OpmsTargetTemplateVersion
{
    public int Id { get; set; }
    public int OpmsTargetTemplateId { get; set; }
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public OpmsTargetTemplate OpmsTargetTemplate { get; set; } = null!;
}

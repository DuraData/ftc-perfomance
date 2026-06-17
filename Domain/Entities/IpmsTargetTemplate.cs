namespace FTCERP.Host.Domain.Entities;

public class IpmsTargetTemplate
{
    public int Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string KpiDescription { get; set; } = string.Empty;
    public string? PerformanceArea { get; set; }
    public string? EmployeeLevel { get; set; }
    public string? JobGrade { get; set; }
    public string TargetUnitType { get; set; } = string.Empty;
    public string? UnitOfMeasure { get; set; }
    public decimal AnnualTarget { get; set; }
    public string? AnnualTargetDescription { get; set; }
    public decimal Weight { get; set; }
    public string? DefaultRatingMethod { get; set; }
    public string? DefaultScoreScale { get; set; }
    public string? DefaultPoeRequirements { get; set; }
    public string? DefaultTaskTemplatesJson { get; set; }
    public bool LinkedOpmsTargetRequired { get; set; }
    public string? FunctionalArea { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public int Version { get; set; } = 1;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<IpmsTargetTemplateVersion> Versions { get; set; } = new List<IpmsTargetTemplateVersion>();
}

public class IpmsTargetTemplateVersion
{
    public int Id { get; set; }
    public int IpmsTargetTemplateId { get; set; }
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public IpmsTargetTemplate IpmsTargetTemplate { get; set; } = null!;
}

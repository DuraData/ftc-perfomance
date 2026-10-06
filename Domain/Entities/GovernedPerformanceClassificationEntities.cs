namespace FTCERP.Host.Domain.Entities;

public sealed class GovernedKpiType : StrategicPlanningMasterBase;
public sealed class GovernedIndicatorType : StrategicPlanningMasterBase;
public sealed class GovernedFunctionalArea : StrategicPlanningMasterBase;
public sealed class GovernedStandardClassification : StrategicPlanningMasterBase;
public sealed class GovernedKpiUnitOfMeasure : StrategicPlanningMasterBase
{
    public string? Symbol { get; set; }
}

public partial class OpmsTarget
{
    public long? KpiTypeMasterId { get; set; }
    public long? IndicatorTypeMasterId { get; set; }
    public long? FunctionalAreaMasterId { get; set; }
    public long? StandardClassificationMasterId { get; set; }
    public GovernedKpiType? KpiTypeMaster { get; set; }
    public GovernedIndicatorType? IndicatorTypeMaster { get; set; }
    public GovernedFunctionalArea? FunctionalAreaMaster { get; set; }
    public GovernedStandardClassification? StandardClassificationMaster { get; set; }
    public long? KpiUnitOfMeasureMasterId { get; set; }
    public GovernedKpiUnitOfMeasure? KpiUnitOfMeasureMaster { get; set; }
}

public partial class IpmsTarget
{
    public long? KpiTypeMasterId { get; set; }
    public long? IndicatorTypeMasterId { get; set; }
    public long? FunctionalAreaMasterId { get; set; }
    public GovernedKpiType? KpiTypeMaster { get; set; }
    public GovernedIndicatorType? IndicatorTypeMaster { get; set; }
    public GovernedFunctionalArea? FunctionalAreaMaster { get; set; }
    public long? KpiUnitOfMeasureMasterId { get; set; }
    public GovernedKpiUnitOfMeasure? KpiUnitOfMeasureMaster { get; set; }
}

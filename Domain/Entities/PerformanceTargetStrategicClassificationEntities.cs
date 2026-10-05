namespace FTCERP.Host.Domain.Entities;

public partial class OpmsTarget
{
    public long? NationalKpaId { get; set; }
    public long? MunicipalKpaId { get; set; }
    public long? BackToBasicsPillarId { get; set; }
    public long? StrategicGoalMasterId { get; set; }
    public long? StrategicInterventionId { get; set; }
    public long? StrategicObjectiveMasterId { get; set; }
    public long? PerformanceObjectiveId { get; set; }
    public NationalKpa? NationalKpaReference { get; set; }
    public MunicipalKpa? MunicipalKpaReference { get; set; }
    public BackToBasicsPillar? BackToBasicsPillarReference { get; set; }
    public MunicipalStrategicGoal? StrategicGoalMaster { get; set; }
    public StrategicIntervention? StrategicInterventionReference { get; set; }
    public MunicipalStrategicObjective? StrategicObjectiveMaster { get; set; }
    public PerformanceObjective? PerformanceObjectiveReference { get; set; }
}

public partial class IpmsTarget
{
    public long? NationalKpaId { get; set; }
    public long? MunicipalKpaId { get; set; }
    public long? BackToBasicsPillarId { get; set; }
    public long? StrategicGoalMasterId { get; set; }
    public long? StrategicInterventionId { get; set; }
    public long? StrategicObjectiveMasterId { get; set; }
    public long? PerformanceObjectiveId { get; set; }
    public NationalKpa? NationalKpaReference { get; set; }
    public MunicipalKpa? MunicipalKpaReference { get; set; }
    public BackToBasicsPillar? BackToBasicsPillarReference { get; set; }
    public MunicipalStrategicGoal? StrategicGoalMaster { get; set; }
    public StrategicIntervention? StrategicInterventionReference { get; set; }
    public MunicipalStrategicObjective? StrategicObjectiveMaster { get; set; }
    public PerformanceObjective? PerformanceObjectiveReference { get; set; }
}

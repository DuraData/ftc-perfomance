namespace FTCERP.Host.Domain.Entities;

public interface IStrategicPlanningMaster
{
    long Id { get; set; }
    Guid PublicId { get; set; }
    long MunicipalityId { get; set; }
    string? Code { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    long? EffectiveFromFinancialYearId { get; set; }
    long? EffectiveToFinancialYearId { get; set; }
    int DisplayOrder { get; set; }
    bool IsActive { get; set; }
    byte[] RowVersion { get; set; }
    Municipality Municipality { get; set; }
    MunicipalityFinancialYear? EffectiveFromFinancialYear { get; set; }
    MunicipalityFinancialYear? EffectiveToFinancialYear { get; set; }
}

public abstract class StrategicPlanningMasterBase : IStrategicPlanningMaster
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? EffectiveFromFinancialYearId { get; set; }
    public long? EffectiveToFinancialYearId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public MunicipalityFinancialYear? EffectiveFromFinancialYear { get; set; }
    public MunicipalityFinancialYear? EffectiveToFinancialYear { get; set; }
}

public sealed class MunicipalKpa : StrategicPlanningMasterBase;
public sealed class MunicipalStrategicGoal : StrategicPlanningMasterBase;
public sealed class StrategicIntervention : StrategicPlanningMasterBase;
public sealed class MunicipalStrategicObjective : StrategicPlanningMasterBase;
public sealed class PerformanceObjective : StrategicPlanningMasterBase;

public interface IStrategicPlanningRelationship
{
    long Id { get; set; }
    Guid PublicId { get; set; }
    long MunicipalityId { get; set; }
    bool IsActive { get; set; }
    byte[] RowVersion { get; set; }
}

public abstract class StrategicPlanningRelationshipBase : IStrategicPlanningRelationship
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
}

public sealed class MunicipalKpaStrategicGoal : StrategicPlanningRelationshipBase
{
    public long MunicipalKpaId { get; set; }
    public long StrategicGoalId { get; set; }
    public MunicipalKpa MunicipalKpa { get; set; } = null!;
    public MunicipalStrategicGoal StrategicGoal { get; set; } = null!;
}

public sealed class StrategicGoalIntervention : StrategicPlanningRelationshipBase
{
    public long StrategicGoalId { get; set; }
    public long StrategicInterventionId { get; set; }
    public MunicipalStrategicGoal StrategicGoal { get; set; } = null!;
    public StrategicIntervention StrategicIntervention { get; set; } = null!;
}

public sealed class StrategicGoalObjective : StrategicPlanningRelationshipBase
{
    public long StrategicGoalId { get; set; }
    public long StrategicObjectiveId { get; set; }
    public MunicipalStrategicGoal StrategicGoal { get; set; } = null!;
    public MunicipalStrategicObjective StrategicObjective { get; set; } = null!;
}

public sealed class StrategicInterventionObjective : StrategicPlanningRelationshipBase
{
    public long StrategicInterventionId { get; set; }
    public long StrategicObjectiveId { get; set; }
    public StrategicIntervention StrategicIntervention { get; set; } = null!;
    public MunicipalStrategicObjective StrategicObjective { get; set; } = null!;
}

public sealed class StrategicObjectivePerformanceObjective : StrategicPlanningRelationshipBase
{
    public long StrategicObjectiveId { get; set; }
    public long PerformanceObjectiveId { get; set; }
    public MunicipalStrategicObjective StrategicObjective { get; set; } = null!;
    public PerformanceObjective PerformanceObjective { get; set; } = null!;
}

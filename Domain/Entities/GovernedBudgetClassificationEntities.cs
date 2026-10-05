namespace FTCERP.Host.Domain.Entities;

public sealed class GovernedBudgetSource : StrategicPlanningMasterBase
{
}

public sealed class GovernedBudgetType : StrategicPlanningMasterBase
{
    public ICollection<OpmsTarget> OpmsTargets { get; set; } = new List<OpmsTarget>();
    public ICollection<IpmsTarget> IpmsTargets { get; set; } = new List<IpmsTarget>();
}

public abstract class KpiBudgetSourceBase
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long BudgetSourceId { get; set; }
    public decimal? Amount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public GovernedBudgetSource BudgetSource { get; set; } = null!;
}

public sealed class OpmsKpiBudgetSource : KpiBudgetSourceBase
{
    public string OpmsTargetId { get; set; } = string.Empty;
    public OpmsTarget OpmsTarget { get; set; } = null!;
}

public sealed class IpmsKpiBudgetSource : KpiBudgetSourceBase
{
    public string IpmsTargetId { get; set; } = string.Empty;
    public IpmsTarget IpmsTarget { get; set; } = null!;
}

public partial class OpmsTarget
{
    public long? BudgetTypeMasterId { get; set; }
    public GovernedBudgetType? BudgetTypeMaster { get; set; }
    public ICollection<OpmsKpiBudgetSource> GovernedBudgetSources { get; set; } = new List<OpmsKpiBudgetSource>();
}

public partial class IpmsTarget
{
    public long? BudgetTypeMasterId { get; set; }
    public GovernedBudgetType? BudgetTypeMaster { get; set; }
    public ICollection<IpmsKpiBudgetSource> GovernedBudgetSources { get; set; } = new List<IpmsKpiBudgetSource>();
}

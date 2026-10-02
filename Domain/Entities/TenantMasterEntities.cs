namespace FTCERP.Host.Domain.Entities;

public enum ReportingPeriodType
{
    Quarter1 = 1,
    Quarter2 = 2,
    MidTerm = 3,
    Quarter3 = 4,
    Quarter4 = 5,
    Annual = 6
}

public class FinancialYear
{
    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class MunicipalityFinancialYear
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public int FinancialYearId { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public FinancialYear FinancialYear { get; set; } = null!;
    public ICollection<ReportingPeriod> ReportingPeriods { get; set; } = new List<ReportingPeriod>();
}

public class ReportingPeriod
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityFinancialYearId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ReportingPeriodType PeriodType { get; set; }
    public int Sequence { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public MunicipalityFinancialYear MunicipalityFinancialYear { get; set; } = null!;
}

public class MunicipalEmployee
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public string? IdentityUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public ApplicationUser? IdentityUser { get; set; }
    public ICollection<EmployeeAssignment> Assignments { get; set; } = new List<EmployeeAssignment>();
}

public class EmployeeAssignment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long MunicipalEmployeeId { get; set; }
    public int DepartmentId { get; set; }
    public int? UnitId { get; set; }
    public int? PositionId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public MunicipalEmployee MunicipalEmployee { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public Unit? Unit { get; set; }
    public Position? Position { get; set; }
}

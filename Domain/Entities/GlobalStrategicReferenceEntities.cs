namespace FTCERP.Host.Domain.Entities;

public class NationalKpa
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<MunicipalityNationalKpa> MunicipalityMappings { get; set; } = new List<MunicipalityNationalKpa>();
}

public class BackToBasicsPillar
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<MunicipalityBackToBasicsPillar> MunicipalityMappings { get; set; } = new List<MunicipalityBackToBasicsPillar>();
}

public class MunicipalityNationalKpa
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long NationalKpaId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public NationalKpa NationalKpa { get; set; } = null!;
}

public class MunicipalityBackToBasicsPillar
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long MunicipalityId { get; set; }
    public long BackToBasicsPillarId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Municipality Municipality { get; set; } = null!;
    public BackToBasicsPillar BackToBasicsPillar { get; set; } = null!;
}

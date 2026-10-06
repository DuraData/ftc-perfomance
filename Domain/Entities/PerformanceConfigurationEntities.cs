using FTCERP.Host.Domain.Services;

namespace FTCERP.Host.Domain.Entities;

public sealed class PerformanceDirectionDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PerformanceDirection EngineDirection { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class OpmsUnitDefinition
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InputControlType { get; set; } = string.Empty;
    public string ValueDataType { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public int? DecimalPlaces { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool SupportsAutoVariance { get; set; }
    public long? DefaultPerformanceDirectionId { get; set; }
    public bool RequiresComponentUi { get; set; }
    public bool IsQualitative { get; set; }
    public PerformanceUnitKind EngineUnitKind { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public PerformanceDirectionDefinition? DefaultPerformanceDirection { get; set; }
}

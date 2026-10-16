using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<RatioComparisonMethodDefinition> RatioComparisonMethods { get; set; } = null!;
    public DbSet<PerformanceRatioDefinition> PerformanceRatioDefinitions { get; set; } = null!;
    public DbSet<PerformanceRatioSegment> PerformanceRatioSegments { get; set; } = null!;

    private void ConfigureRatioConfiguration(ModelBuilder builder)
    {
        builder.Entity<RatioComparisonMethodDefinition>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<RatioComparisonMethodDefinition>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<RatioComparisonMethodDefinition>().Property(item => item.Code).HasMaxLength(50);
        builder.Entity<RatioComparisonMethodDefinition>().Property(item => item.Name).HasMaxLength(150);
        builder.Entity<RatioComparisonMethodDefinition>().Property(item => item.Description).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<RatioComparisonMethodDefinition>().Property(item => item.RowVersion));
        builder.Entity<RatioComparisonMethodDefinition>().HasData(RatioMethods());

        builder.Entity<PerformanceRatioDefinition>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceRatioDefinition>().HasIndex(item => item.PerformancePeriodTargetId).IsUnique();
        ConfigureRowVersion(builder.Entity<PerformanceRatioDefinition>().Property(item => item.RowVersion));
        builder.Entity<PerformanceRatioDefinition>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioDefinition>().HasOne(item => item.PerformancePeriodTarget).WithOne(item => item.RatioDefinition).HasForeignKey<PerformanceRatioDefinition>(item => item.PerformancePeriodTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioDefinition>().HasOne(item => item.RatioComparisonMethod).WithMany().HasForeignKey(item => item.RatioComparisonMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioDefinition>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioDefinition>().HasOne(item => item.ModifiedByUser).WithMany().HasForeignKey(item => item.ModifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioDefinition>().ToTable(table => table.HasCheckConstraint("CK_PerformanceRatioDefinitions_SegmentCount", "[RatioSegmentCount] BETWEEN 2 AND 4"));
        builder.Entity<PerformanceRatioDefinition>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<PerformanceRatioSegment>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceRatioSegment>().HasIndex(item => new { item.RatioDefinitionId, item.SegmentPosition }).IsUnique();
        builder.Entity<PerformanceRatioSegment>().Property(item => item.SegmentLabel).HasMaxLength(150);
        builder.Entity<PerformanceRatioSegment>().Property(item => item.UnitDescription).HasMaxLength(250);
        builder.Entity<PerformanceRatioSegment>().Property(item => item.MinValue).HasPrecision(19, 6);
        builder.Entity<PerformanceRatioSegment>().Property(item => item.MaxValue).HasPrecision(19, 6);
        ConfigureRowVersion(builder.Entity<PerformanceRatioSegment>().Property(item => item.RowVersion));
        builder.Entity<PerformanceRatioSegment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioSegment>().HasOne(item => item.RatioDefinition).WithMany(item => item.Segments).HasForeignKey(item => item.RatioDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRatioSegment>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_PerformanceRatioSegments_Position", "[SegmentPosition] BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_PerformanceRatioSegments_DecimalPlaces", "[DecimalPlaces] IS NULL OR [DecimalPlaces] BETWEEN 0 AND 6");
            table.HasCheckConstraint("CK_PerformanceRatioSegments_Range", "[MinValue] IS NULL OR [MaxValue] IS NULL OR [MinValue] <= [MaxValue]");
        });
        builder.Entity<PerformanceRatioSegment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }

    private static RatioComparisonMethodDefinition[] RatioMethods() =>
    [
        RatioMethod(1, "NORMALIZED_RATIO", "Normalized ratio", "Normalize a two-segment ratio and apply the performance direction.", true),
        RatioMethod(2, "EXACT_MATCH", "Exact match", "Every actual segment must match its target segment.", true),
        RatioMethod(3, "SEGMENT_BY_SEGMENT", "Segment by segment", "Compare configured segments independently.", true),
        RatioMethod(4, "MANUAL", "Manual", "Do not infer automatic ratio achievement or variance.", false),
    ];

    private static RatioComparisonMethodDefinition RatioMethod(long id, string code, string name, string description, bool supportsAutoCalculation) => new()
    {
        Id = id,
        PublicId = Guid.Parse($"30000000-0000-0000-0000-{id:000000000000}"),
        Code = code,
        Name = name,
        Description = description,
        SupportsAutoCalculation = supportsAutoCalculation,
        IsActive = true
    };
}

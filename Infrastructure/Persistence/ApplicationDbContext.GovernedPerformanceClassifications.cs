using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<GovernedKpiType> GovernedKpiTypes { get; set; } = null!;
    public DbSet<GovernedIndicatorType> GovernedIndicatorTypes { get; set; } = null!;
    public DbSet<GovernedFunctionalArea> GovernedFunctionalAreas { get; set; } = null!;
    public DbSet<GovernedStandardClassification> GovernedStandardClassifications { get; set; } = null!;

    private void ConfigureGovernedPerformanceClassifications(ModelBuilder builder)
    {
        ConfigurePerformanceMaster(builder.Entity<GovernedKpiType>(), "OPMS_KPITypes", "KPITypes");
        ConfigurePerformanceMaster(builder.Entity<GovernedIndicatorType>(), "OPMS_IndicatorTypes", "IndicatorTypes");
        ConfigurePerformanceMaster(builder.Entity<GovernedFunctionalArea>(), "OPMS_FunctionalAreas", "FunctionalAreas");
        ConfigurePerformanceMaster(builder.Entity<GovernedStandardClassification>(), "OPMS_StandardClassifications", "StandardClassifications");

        builder.Entity<OpmsTarget>().HasOne(item => item.KpiTypeMaster).WithMany().HasForeignKey(item => new { item.KpiTypeMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTarget>().HasOne(item => item.IndicatorTypeMaster).WithMany().HasForeignKey(item => new { item.IndicatorTypeMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTarget>().HasOne(item => item.FunctionalAreaMaster).WithMany().HasForeignKey(item => new { item.FunctionalAreaMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTarget>().HasOne(item => item.StandardClassificationMaster).WithMany().HasForeignKey(item => new { item.StandardClassificationMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsTarget>().HasOne(item => item.KpiTypeMaster).WithMany().HasForeignKey(item => new { item.KpiTypeMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsTarget>().HasOne(item => item.IndicatorTypeMaster).WithMany().HasForeignKey(item => new { item.IndicatorTypeMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsTarget>().HasOne(item => item.FunctionalAreaMaster).WithMany().HasForeignKey(item => new { item.FunctionalAreaMasterId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigurePerformanceMaster<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName, string constraintStem)
        where TEntity : StrategicPlanningMasterBase
    {
        entity.ToTable(tableName);
        entity.HasIndex(item => item.PublicId).IsUnique();
        entity.HasIndex(item => new { item.MunicipalityId, item.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
        entity.HasAlternateKey(item => new { item.Id, item.MunicipalityId });
        entity.Property(item => item.Code).HasMaxLength(80);
        entity.Property(item => item.Name).HasMaxLength(500);
        entity.Property(item => item.Description).HasMaxLength(2000);
        entity.ToTable(table => table.HasCheckConstraint($"CK_OPMS_{constraintStem}_DisplayOrder", "[DisplayOrder] >= 0"));
        ConfigureRowVersion(entity.Property(item => item.RowVersion));
        entity.HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.EffectiveFromFinancialYear).WithMany()
            .HasForeignKey(item => new { Id = item.EffectiveFromFinancialYearId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.EffectiveToFinancialYear).WithMany()
            .HasForeignKey(item => new { Id = item.EffectiveToFinancialYearId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}

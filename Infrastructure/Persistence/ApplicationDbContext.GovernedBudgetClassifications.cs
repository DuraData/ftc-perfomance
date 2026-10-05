using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<GovernedBudgetSource> GovernedBudgetSources { get; set; } = null!;
    public DbSet<GovernedBudgetType> GovernedBudgetTypes { get; set; } = null!;
    public DbSet<OpmsKpiBudgetSource> OpmsKpiBudgetSources { get; set; } = null!;
    public DbSet<IpmsKpiBudgetSource> IpmsKpiBudgetSources { get; set; } = null!;

    private void ConfigureGovernedBudgetClassifications(ModelBuilder builder)
    {
        ConfigureBudgetMaster(builder.Entity<GovernedBudgetSource>(), "OPMS_BudgetSources", "BudgetSources");
        ConfigureBudgetMaster(builder.Entity<GovernedBudgetType>(), "OPMS_BudgetTypes", "BudgetTypes");

        builder.Entity<OpmsTarget>().HasOne(item => item.BudgetTypeMaster).WithMany(item => item.OpmsTargets)
            .HasForeignKey(item => new { item.BudgetTypeMasterId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsTarget>().HasOne(item => item.BudgetTypeMaster).WithMany(item => item.IpmsTargets)
            .HasForeignKey(item => new { item.BudgetTypeMasterId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);

        ConfigureBudgetSourceLink(builder.Entity<OpmsKpiBudgetSource>(), "OPMS_KPIBudgetSources");
        builder.Entity<OpmsKpiBudgetSource>().HasIndex(item => new { item.OpmsTargetId, item.BudgetSourceId }).IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<OpmsKpiBudgetSource>().HasOne(item => item.OpmsTarget).WithMany(item => item.GovernedBudgetSources)
            .HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);

        ConfigureBudgetSourceLink(builder.Entity<IpmsKpiBudgetSource>(), "IPMS_KPIBudgetSources");
        builder.Entity<IpmsKpiBudgetSource>().HasIndex(item => new { item.IpmsTargetId, item.BudgetSourceId }).IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<IpmsKpiBudgetSource>().HasOne(item => item.IpmsTarget).WithMany(item => item.GovernedBudgetSources)
            .HasForeignKey(item => item.IpmsTargetId).OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureBudgetMaster<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName, string constraintStem)
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

    private void ConfigureBudgetSourceLink<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : KpiBudgetSourceBase
    {
        entity.ToTable(tableName);
        entity.HasIndex(item => item.PublicId).IsUnique();
        entity.Property(item => item.Amount).HasPrecision(18, 2);
        entity.ToTable(table => table.HasCheckConstraint($"CK_{tableName}_Amount", "[Amount] IS NULL OR [Amount] >= 0"));
        ConfigureRowVersion(entity.Property(item => item.RowVersion));
        entity.HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.BudgetSource).WithMany()
            .HasForeignKey(item => new { item.BudgetSourceId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}

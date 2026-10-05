using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<NationalKpa> NationalKpas { get; set; } = null!;
    public DbSet<BackToBasicsPillar> BackToBasicsPillars { get; set; } = null!;
    public DbSet<MunicipalityNationalKpa> MunicipalityNationalKpas { get; set; } = null!;
    public DbSet<MunicipalityBackToBasicsPillar> MunicipalityBackToBasicsPillars { get; set; } = null!;

    private void ConfigureGlobalStrategicReferences(ModelBuilder builder)
    {
        builder.Entity<NationalKpa>(entity =>
        {
            entity.ToTable("OPMS_NationalKPAs");
            entity.HasIndex(item => item.PublicId).IsUnique();
            entity.HasIndex(item => item.Code).IsUnique();
            entity.Property(item => item.Code).HasMaxLength(60);
            entity.Property(item => item.Name).HasMaxLength(240);
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.ToTable(table => table.HasCheckConstraint("CK_OPMS_NationalKPAs_DisplayOrder", "[DisplayOrder] >= 0"));
            ConfigureRowVersion(entity.Property(item => item.RowVersion));
        });

        builder.Entity<BackToBasicsPillar>(entity =>
        {
            entity.ToTable("OPMS_BackToBasicPillars");
            entity.HasIndex(item => item.PublicId).IsUnique();
            entity.HasIndex(item => item.Code).IsUnique();
            entity.Property(item => item.Code).HasMaxLength(60);
            entity.Property(item => item.Name).HasMaxLength(240);
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.ToTable(table => table.HasCheckConstraint("CK_OPMS_BackToBasicPillars_DisplayOrder", "[DisplayOrder] >= 0"));
            ConfigureRowVersion(entity.Property(item => item.RowVersion));
        });

        builder.Entity<MunicipalityNationalKpa>(entity =>
        {
            entity.ToTable("OPMS_MunicipalityNationalKPAs");
            entity.HasIndex(item => item.PublicId).IsUnique();
            entity.HasIndex(item => new { item.MunicipalityId, item.NationalKpaId }).IsUnique();
            ConfigureRowVersion(entity.Property(item => item.RowVersion));
            entity.HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.NationalKpa).WithMany(item => item.MunicipalityMappings).HasForeignKey(item => item.NationalKpaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        });

        builder.Entity<MunicipalityBackToBasicsPillar>(entity =>
        {
            entity.ToTable("OPMS_MunicipalityBackToBasicPillars");
            entity.HasIndex(item => item.PublicId).IsUnique();
            entity.HasIndex(item => new { item.MunicipalityId, item.BackToBasicsPillarId }).IsUnique();
            ConfigureRowVersion(entity.Property(item => item.RowVersion));
            entity.HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.BackToBasicsPillar).WithMany(item => item.MunicipalityMappings).HasForeignKey(item => item.BackToBasicsPillarId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        });
    }
}

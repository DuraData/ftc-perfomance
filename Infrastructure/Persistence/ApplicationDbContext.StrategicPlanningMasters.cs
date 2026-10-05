using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<MunicipalKpa> MunicipalKpas { get; set; } = null!;
    public DbSet<MunicipalStrategicGoal> MunicipalStrategicGoals { get; set; } = null!;
    public DbSet<StrategicIntervention> StrategicInterventions { get; set; } = null!;
    public DbSet<MunicipalStrategicObjective> MunicipalStrategicObjectives { get; set; } = null!;
    public DbSet<PerformanceObjective> PerformanceObjectives { get; set; } = null!;
    public DbSet<MunicipalKpaStrategicGoal> MunicipalKpaStrategicGoals { get; set; } = null!;
    public DbSet<StrategicGoalIntervention> StrategicGoalInterventions { get; set; } = null!;
    public DbSet<StrategicGoalObjective> StrategicGoalObjectives { get; set; } = null!;
    public DbSet<StrategicInterventionObjective> StrategicInterventionObjectives { get; set; } = null!;
    public DbSet<StrategicObjectivePerformanceObjective> StrategicObjectivePerformanceObjectives { get; set; } = null!;

    private void ConfigureStrategicPlanningMasters(ModelBuilder builder)
    {
        ConfigureStrategicMaster(builder.Entity<MunicipalKpa>(), "OPMS_MunicipalKPAs", "MunicipalKPAs");
        ConfigureStrategicMaster(builder.Entity<MunicipalStrategicGoal>(), "OPMS_StrategicGoals", "StrategicGoals");
        ConfigureStrategicMaster(builder.Entity<StrategicIntervention>(), "OPMS_StrategicInterventions", "StrategicInterventions");
        ConfigureStrategicMaster(builder.Entity<MunicipalStrategicObjective>(), "OPMS_StrategicObjectives", "StrategicObjectives");
        ConfigureStrategicMaster(builder.Entity<PerformanceObjective>(), "OPMS_PerformanceObjectives", "PerformanceObjectives");

        ConfigureRelationship(builder.Entity<MunicipalKpaStrategicGoal>(), "OPMS_MunicipalKPAStrategicGoals");
        builder.Entity<MunicipalKpaStrategicGoal>().HasIndex(item => new { item.MunicipalityId, item.MunicipalKpaId, item.StrategicGoalId }).IsUnique();
        builder.Entity<MunicipalKpaStrategicGoal>().HasOne(item => item.MunicipalKpa).WithMany().HasForeignKey(item => new { item.MunicipalKpaId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalKpaStrategicGoal>().HasOne(item => item.StrategicGoal).WithMany().HasForeignKey(item => new { item.StrategicGoalId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);

        ConfigureRelationship(builder.Entity<StrategicGoalIntervention>(), "OPMS_StrategicGoalInterventions");
        builder.Entity<StrategicGoalIntervention>().HasIndex(item => new { item.MunicipalityId, item.StrategicGoalId, item.StrategicInterventionId }).IsUnique();
        builder.Entity<StrategicGoalIntervention>().HasOne(item => item.StrategicGoal).WithMany().HasForeignKey(item => new { item.StrategicGoalId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicGoalIntervention>().HasOne(item => item.StrategicIntervention).WithMany().HasForeignKey(item => new { item.StrategicInterventionId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);

        ConfigureRelationship(builder.Entity<StrategicGoalObjective>(), "OPMS_StrategicGoalObjectives");
        builder.Entity<StrategicGoalObjective>().HasIndex(item => new { item.MunicipalityId, item.StrategicGoalId, item.StrategicObjectiveId }).IsUnique();
        builder.Entity<StrategicGoalObjective>().HasOne(item => item.StrategicGoal).WithMany().HasForeignKey(item => new { item.StrategicGoalId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicGoalObjective>().HasOne(item => item.StrategicObjective).WithMany().HasForeignKey(item => new { item.StrategicObjectiveId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);

        ConfigureRelationship(builder.Entity<StrategicInterventionObjective>(), "OPMS_StrategicInterventionObjectives");
        builder.Entity<StrategicInterventionObjective>().HasIndex(item => new { item.MunicipalityId, item.StrategicInterventionId, item.StrategicObjectiveId }).IsUnique();
        builder.Entity<StrategicInterventionObjective>().HasOne(item => item.StrategicIntervention).WithMany().HasForeignKey(item => new { item.StrategicInterventionId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicInterventionObjective>().HasOne(item => item.StrategicObjective).WithMany().HasForeignKey(item => new { item.StrategicObjectiveId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);

        ConfigureRelationship(builder.Entity<StrategicObjectivePerformanceObjective>(), "OPMS_StrategicObjectivePerformanceObjectives");
        builder.Entity<StrategicObjectivePerformanceObjective>().HasIndex(item => new { item.MunicipalityId, item.StrategicObjectiveId, item.PerformanceObjectiveId }).IsUnique();
        builder.Entity<StrategicObjectivePerformanceObjective>().HasOne(item => item.StrategicObjective).WithMany().HasForeignKey(item => new { item.StrategicObjectiveId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicObjectivePerformanceObjective>().HasOne(item => item.PerformanceObjective).WithMany().HasForeignKey(item => new { item.PerformanceObjectiveId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureStrategicMaster<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName, string constraintStem) where TEntity : StrategicPlanningMasterBase
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
        entity.HasOne(item => item.EffectiveFromFinancialYear).WithMany().HasForeignKey(item => new { Id = item.EffectiveFromFinancialYearId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.EffectiveToFinancialYear).WithMany().HasForeignKey(item => new { Id = item.EffectiveToFinancialYearId, item.MunicipalityId }).HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }

    private void ConfigureRelationship<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName) where TEntity : StrategicPlanningRelationshipBase
    {
        entity.ToTable(tableName);
        entity.HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(entity.Property(item => item.RowVersion));
        entity.HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}

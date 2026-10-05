using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    private static void ConfigurePerformanceTargetStrategicClassifications(ModelBuilder builder)
    {
        ConfigureTargetStrategicClassifications(builder.Entity<OpmsTarget>());
        ConfigureTargetStrategicClassifications(builder.Entity<IpmsTarget>());
    }

    private static void ConfigureTargetStrategicClassifications<TEntity>(EntityTypeBuilder<TEntity> entity) where TEntity : class
    {
        entity.HasOne(typeof(NationalKpa), nameof(OpmsTarget.NationalKpaReference)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.NationalKpaId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(typeof(BackToBasicsPillar), nameof(OpmsTarget.BackToBasicsPillarReference)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.BackToBasicsPillarId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(typeof(MunicipalKpa), nameof(OpmsTarget.MunicipalKpaReference)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.MunicipalKpaId), nameof(OpmsTarget.MunicipalityId))
            .HasPrincipalKey(nameof(MunicipalKpa.Id), nameof(MunicipalKpa.MunicipalityId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(typeof(MunicipalStrategicGoal), nameof(OpmsTarget.StrategicGoalMaster)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.StrategicGoalMasterId), nameof(OpmsTarget.MunicipalityId))
            .HasPrincipalKey(nameof(MunicipalStrategicGoal.Id), nameof(MunicipalStrategicGoal.MunicipalityId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(typeof(StrategicIntervention), nameof(OpmsTarget.StrategicInterventionReference)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.StrategicInterventionId), nameof(OpmsTarget.MunicipalityId))
            .HasPrincipalKey(nameof(StrategicIntervention.Id), nameof(StrategicIntervention.MunicipalityId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(typeof(MunicipalStrategicObjective), nameof(OpmsTarget.StrategicObjectiveMaster)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.StrategicObjectiveMasterId), nameof(OpmsTarget.MunicipalityId))
            .HasPrincipalKey(nameof(MunicipalStrategicObjective.Id), nameof(MunicipalStrategicObjective.MunicipalityId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(typeof(PerformanceObjective), nameof(OpmsTarget.PerformanceObjectiveReference)).WithMany()
            .HasForeignKey(nameof(OpmsTarget.PerformanceObjectiveId), nameof(OpmsTarget.MunicipalityId))
            .HasPrincipalKey(nameof(PerformanceObjective.Id), nameof(PerformanceObjective.MunicipalityId)).OnDelete(DeleteBehavior.Restrict);
    }
}

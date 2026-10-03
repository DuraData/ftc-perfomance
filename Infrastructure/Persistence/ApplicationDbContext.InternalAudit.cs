using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<InternalAuditAssessmentConfiguration> InternalAuditAssessmentConfigurations { get; set; } = null!;
    public DbSet<InternalAuditAssessment> InternalAuditAssessments { get; set; } = null!;

    private void ConfigureInternalAuditAssessments(ModelBuilder builder)
    {
        builder.Entity<InternalAuditAssessmentConfiguration>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<InternalAuditAssessmentConfiguration>().HasIndex(item => new { item.MunicipalityFinancialYearId, item.Version }).IsUnique();
        builder.Entity<InternalAuditAssessmentConfiguration>().HasIndex(item => new { item.MunicipalityFinancialYearId, item.IsCurrent }).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<InternalAuditAssessmentConfiguration>().Property(item => item.Reason).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<InternalAuditAssessmentConfiguration>().Property(item => item.RowVersion));
        builder.Entity<InternalAuditAssessmentConfiguration>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessmentConfiguration>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessmentConfiguration>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessmentConfiguration>().ToTable(table => table.HasCheckConstraint("CK_InternalAuditAssessmentConfigurations_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        builder.Entity<InternalAuditAssessmentConfiguration>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<InternalAuditAssessment>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<InternalAuditAssessment>().HasIndex(item => new { item.SubmissionWorkflowInstanceId, item.AssessedAt });
        builder.Entity<InternalAuditAssessment>().HasIndex(item => item.PreviousAssessmentId).IsUnique().HasFilter("[PreviousAssessmentId] IS NOT NULL");
        builder.Entity<InternalAuditAssessment>().Property(item => item.DetailedObservation).HasMaxLength(4000);
        builder.Entity<InternalAuditAssessment>().Property(item => item.Comment).HasMaxLength(2000);
        builder.Entity<InternalAuditAssessment>().Property(item => item.Findings).HasMaxLength(4000);
        builder.Entity<InternalAuditAssessment>().Property(item => item.Recommendation).HasMaxLength(4000);
        builder.Entity<InternalAuditAssessment>().Property(item => item.Score).HasPrecision(18, 4);
        builder.Entity<InternalAuditAssessment>().Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Entity<InternalAuditAssessment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessment>().HasOne(item => item.SubmissionWorkflowInstance).WithMany().HasForeignKey(item => item.SubmissionWorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessment>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.ConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessment>().HasOne(item => item.PreviousAssessment).WithMany(item => item.Reassessments).HasForeignKey(item => item.PreviousAssessmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessment>().HasOne(item => item.PerformanceRfi).WithMany().HasForeignKey(item => item.PerformanceRfiId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessment>().HasOne(item => item.AssessedByUser).WithMany().HasForeignKey(item => item.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InternalAuditAssessment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}

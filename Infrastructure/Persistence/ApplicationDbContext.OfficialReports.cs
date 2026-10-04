using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<OfficialReportTemplate> OfficialReportTemplates { get; set; } = null!;
    public DbSet<OfficialReportGeneration> OfficialReportGenerations { get; set; } = null!;
    public DbSet<OfficialReportSchedule> OfficialReportSchedules { get; set; } = null!;
    public DbSet<OfficialReportJob> OfficialReportJobs { get; set; } = null!;

    private void ConfigureOfficialReports(ModelBuilder builder)
    {
        builder.Entity<OfficialReportTemplate>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OfficialReportTemplate>().HasIndex(item => new { item.MunicipalityId, item.TemplateFamilyPublicId, item.VersionNumber }).IsUnique();
        builder.Entity<OfficialReportTemplate>().HasIndex(item => new { item.MunicipalityId, item.TemplateFamilyPublicId, item.IsCurrent }).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<OfficialReportTemplate>().HasIndex(item => item.PreviousVersionId).IsUnique().HasFilter("[PreviousVersionId] IS NOT NULL");
        builder.Entity<OfficialReportTemplate>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<OfficialReportTemplate>().Property(item => item.Name).HasMaxLength(240);
        builder.Entity<OfficialReportTemplate>().Property(item => item.HeadingTemplate).HasMaxLength(500);
        builder.Entity<OfficialReportTemplate>().Property(item => item.ColumnConfigurationJson).HasMaxLength(8000);
        builder.Entity<OfficialReportTemplate>().Property(item => item.ApprovalReference).HasMaxLength(240);
        builder.Entity<OfficialReportTemplate>().Property(item => item.Reason).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<OfficialReportTemplate>().Property(item => item.RowVersion));
        builder.Entity<OfficialReportTemplate>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_OfficialReportTemplates_Version", "[VersionNumber] >= 1");
            table.HasCheckConstraint("CK_OfficialReportTemplates_ReportType", "[ReportType] >= 1 AND [ReportType] <= 16");
            table.HasCheckConstraint("CK_OfficialReportTemplates_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
        builder.Entity<OfficialReportTemplate>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportTemplate>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportTemplate>().HasOne(item => item.PreviousVersion).WithMany(item => item.LaterVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportTemplate>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportTemplate>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<OfficialReportGeneration>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OfficialReportGeneration>().HasIndex(item => new { item.MunicipalityId, item.GenerationFamilyPublicId, item.VersionNumber }).IsUnique();
        builder.Entity<OfficialReportGeneration>().HasIndex(item => new { item.MunicipalityId, item.MunicipalityFinancialYearId, item.ReportingPeriodId, item.GeneratedAt });
        builder.Entity<OfficialReportGeneration>().Property(item => item.ScopeJson).HasMaxLength(8000);
        builder.Entity<OfficialReportGeneration>().Property(item => item.FilterJson).HasMaxLength(8000);
        builder.Entity<OfficialReportGeneration>().Property(item => item.DataVersionReference).HasMaxLength(64);
        builder.Entity<OfficialReportGeneration>().Property(item => item.FileName).HasMaxLength(260);
        builder.Entity<OfficialReportGeneration>().Property(item => item.ContentType).HasMaxLength(160);
        builder.Entity<OfficialReportGeneration>().Property(item => item.Sha256).HasMaxLength(64);
        builder.Entity<OfficialReportGeneration>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_OfficialReportGenerations_Version", "[VersionNumber] >= 1 AND [RowCount] >= 0 AND [SizeInBytes] >= 0");
            table.HasCheckConstraint("CK_OfficialReportGenerations_ReportType", "[ReportType] >= 1 AND [ReportType] <= 16");
        });
        builder.Entity<OfficialReportGeneration>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportGeneration>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportGeneration>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportGeneration>().HasOne(item => item.ReportTemplate).WithMany(item => item.Generations).HasForeignKey(item => item.ReportTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportGeneration>().HasOne(item => item.Blob).WithMany(item => item.OfficialReportGenerations).HasForeignKey(item => item.EvidenceBlobId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportGeneration>().HasOne(item => item.GeneratedByUser).WithMany().HasForeignKey(item => item.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportGeneration>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<OfficialReportSchedule>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OfficialReportSchedule>().HasIndex(item => new { item.MunicipalityId, item.ScheduleFamilyPublicId, item.VersionNumber }).IsUnique();
        builder.Entity<OfficialReportSchedule>().HasIndex(item => new { item.MunicipalityId, item.ScheduleFamilyPublicId, item.IsCurrent }).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<OfficialReportSchedule>().HasIndex(item => item.PreviousVersionId).IsUnique().HasFilter("[PreviousVersionId] IS NOT NULL");
        builder.Entity<OfficialReportSchedule>().HasIndex(item => new { item.IsActive, item.NextRunAt });
        builder.Entity<OfficialReportSchedule>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<OfficialReportSchedule>().Property(item => item.Name).HasMaxLength(240);
        builder.Entity<OfficialReportSchedule>().Property(item => item.RecipientValuesCsv).HasMaxLength(4000);
        builder.Entity<OfficialReportSchedule>().Property(item => item.ChannelsCsv).HasMaxLength(120);
        builder.Entity<OfficialReportSchedule>().Property(item => item.ApprovalReference).HasMaxLength(240);
        builder.Entity<OfficialReportSchedule>().Property(item => item.Reason).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<OfficialReportSchedule>().Property(item => item.RowVersion));
        builder.Entity<OfficialReportSchedule>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_OfficialReportSchedules_VersionInterval", "[VersionNumber] >= 1 AND [Interval] >= 1 AND [Interval] <= 365");
            table.HasCheckConstraint("CK_OfficialReportSchedules_Cadence", "[Cadence] >= 1 AND [Cadence] <= 4");
            table.HasCheckConstraint("CK_OfficialReportSchedules_RecipientKind", "[RecipientKind] >= 1 AND [RecipientKind] <= 2");
        });
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.ReportTemplate).WithMany().HasForeignKey(item => item.ReportTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.Department).WithMany().HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.Unit).WithMany().HasForeignKey(item => item.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.PreviousVersion).WithMany(item => item.LaterVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportSchedule>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<OfficialReportJob>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OfficialReportJob>().HasIndex(item => new { item.OfficialReportScheduleId, item.ScheduledFor }).IsUnique().HasFilter("[OfficialReportScheduleId] IS NOT NULL");
        builder.Entity<OfficialReportJob>().HasIndex(item => new { item.State, item.AvailableAt });
        builder.Entity<OfficialReportJob>().Property(item => item.LastError).HasMaxLength(2000);
        builder.Entity<OfficialReportJob>().Property(item => item.RecipientUserIdsCsv).HasMaxLength(8000);
        builder.Entity<OfficialReportJob>().Property(item => item.ChannelsCsv).HasMaxLength(120);
        builder.Entity<OfficialReportJob>().Property(item => item.RetryReason).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<OfficialReportJob>().Property(item => item.RowVersion));
        builder.Entity<OfficialReportJob>().ToTable(table => table.HasCheckConstraint("CK_OfficialReportJobs_StateAttempts", "[State] >= 1 AND [State] <= 6 AND [AttemptCount] >= 0"));
        builder.Entity<OfficialReportJob>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.OfficialReportSchedule).WithMany(item => item.Jobs).HasForeignKey(item => item.OfficialReportScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.ReportTemplate).WithMany().HasForeignKey(item => item.ReportTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.Department).WithMany().HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.Unit).WithMany().HasForeignKey(item => item.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.PreviousOfficialReportGeneration).WithMany().HasForeignKey(item => item.PreviousOfficialReportGenerationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.OfficialReportGeneration).WithMany().HasForeignKey(item => item.OfficialReportGenerationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.DistributionOutbox).WithMany().HasForeignKey(item => item.DistributionOutboxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasOne(item => item.RequestedByUser).WithMany().HasForeignKey(item => item.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OfficialReportJob>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}

using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    private void ConfigureNotificationPolicies(ModelBuilder builder)
    {
        builder.Entity<NotificationConfiguration>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<NotificationConfiguration>().HasIndex(x => new { x.MunicipalityId, x.FamilyId, x.Version }).IsUnique();
        builder.Entity<NotificationConfiguration>().HasIndex(x => new { x.MunicipalityId, x.MunicipalityFinancialYearId, x.ApplicabilityKey }).IsUnique().HasFilter("[Lifecycle] = 2");
        builder.Entity<NotificationConfiguration>().HasIndex(x => new { x.MunicipalityId, x.MunicipalityFinancialYearId, x.Code, x.Scope, x.WorkflowStageCode, x.ReportingPeriodId, x.Lifecycle });
        builder.Entity<NotificationConfiguration>().Property(x => x.Code).HasMaxLength(80);
        builder.Entity<NotificationConfiguration>().Property(x => x.Name).HasMaxLength(200);
        builder.Entity<NotificationConfiguration>().Property(x => x.ApplicabilityKey).HasMaxLength(300);
        builder.Entity<NotificationConfiguration>().Property(x => x.WorkflowStageCode).HasMaxLength(80);
        builder.Entity<NotificationConfiguration>().Property(x => x.ChannelsCsv).HasMaxLength(120);
        builder.Entity<NotificationConfiguration>().Property(x => x.TitleTemplate).HasMaxLength(240);
        builder.Entity<NotificationConfiguration>().Property(x => x.MessageTemplate).HasMaxLength(2000);
        builder.Entity<NotificationConfiguration>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_NotificationConfigurations_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint("CK_NotificationConfigurations_Scope", "([Scope] = 1 AND [WorkflowStageCode] IS NULL AND [ReportingPeriodId] IS NULL) OR ([Scope] = 2 AND [WorkflowStageCode] IS NOT NULL AND [ReportingPeriodId] IS NULL) OR ([Scope] = 3 AND [ReportingPeriodId] IS NOT NULL)");
        });
        ConfigureRowVersion(builder.Entity<NotificationConfiguration>().Property(x => x.RowVersion));
        builder.Entity<NotificationConfiguration>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationConfiguration>().HasOne(x => x.MunicipalityFinancialYear).WithMany().HasForeignKey(x => x.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationConfiguration>().HasOne(x => x.PreviousVersion).WithMany().HasForeignKey(x => x.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationConfiguration>().HasOne(x => x.ReportingPeriod).WithMany().HasForeignKey(x => x.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationConfiguration>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationConfiguration>().HasOne(x => x.ActivatedByUser).WithMany().HasForeignKey(x => x.ActivatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationConfiguration>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<NotificationScheduleRule>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<NotificationScheduleRule>().HasIndex(x => new { x.NotificationConfigurationId, x.Code }).IsUnique();
        builder.Entity<NotificationScheduleRule>().Property(x => x.Code).HasMaxLength(80);
        builder.Entity<NotificationScheduleRule>().Property(x => x.RecipientValuesCsv).HasMaxLength(2000);
        builder.Entity<NotificationScheduleRule>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationScheduleRule>().HasOne(x => x.NotificationConfiguration).WithMany(x => x.Rules).HasForeignKey(x => x.NotificationConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationScheduleRule>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<WorkingCalendarHoliday>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<WorkingCalendarHoliday>().HasIndex(x => new { x.MunicipalityFinancialYearId, x.Date }).IsUnique();
        builder.Entity<WorkingCalendarHoliday>().Property(x => x.Name).HasMaxLength(200);
        ConfigureRowVersion(builder.Entity<WorkingCalendarHoliday>().Property(x => x.RowVersion));
        builder.Entity<WorkingCalendarHoliday>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkingCalendarHoliday>().HasOne(x => x.MunicipalityFinancialYear).WithMany().HasForeignKey(x => x.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkingCalendarHoliday>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkingCalendarHoliday>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<ScheduledNotification>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<ScheduledNotification>().HasIndex(x => x.LogicalKey).IsUnique();
        builder.Entity<ScheduledNotification>().HasIndex(x => new { x.State, x.ScheduledAt });
        builder.Entity<ScheduledNotification>().Property(x => x.SourceType).HasMaxLength(40);
        builder.Entity<ScheduledNotification>().Property(x => x.SourceId).HasMaxLength(160);
        builder.Entity<ScheduledNotification>().Property(x => x.LogicalKey).HasMaxLength(300);
        builder.Entity<ScheduledNotification>().Property(x => x.ContextHash).HasMaxLength(64);
        builder.Entity<ScheduledNotification>().Property(x => x.StateReason).HasMaxLength(500);
        ConfigureRowVersion(builder.Entity<ScheduledNotification>().Property(x => x.RowVersion));
        builder.Entity<ScheduledNotification>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduledNotification>().HasOne(x => x.NotificationConfiguration).WithMany().HasForeignKey(x => x.NotificationConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduledNotification>().HasOne(x => x.NotificationScheduleRule).WithMany().HasForeignKey(x => x.NotificationScheduleRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduledNotification>().HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduledNotification>().HasOne(x => x.Notification).WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduledNotification>().HasOne(x => x.BusinessEventOutbox).WithMany().HasForeignKey(x => x.BusinessEventOutboxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScheduledNotification>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<NotificationPreference>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<NotificationPreference>().HasIndex(x => new { x.MunicipalityId, x.UserId }).IsUnique();
        ConfigureRowVersion(builder.Entity<NotificationPreference>().Property(x => x.RowVersion));
        builder.Entity<NotificationPreference>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationPreference>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationPreference>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}

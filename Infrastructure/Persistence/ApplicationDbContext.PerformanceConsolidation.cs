using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<PerformanceCalculationTypeDefinition> PerformanceCalculationTypes { get; set; } = null!;
    public DbSet<MunicipalityConsolidationPolicy> MunicipalityConsolidationPolicies { get; set; } = null!;
    public DbSet<PerformanceSuggestionEvent> PerformanceSuggestionEvents { get; set; } = null!;

    private void ConfigurePerformanceConsolidation(ModelBuilder builder)
    {
        builder.Entity<PerformanceCalculationTypeDefinition>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceCalculationTypeDefinition>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<PerformanceCalculationTypeDefinition>().Property(item => item.Code).HasMaxLength(40);
        builder.Entity<PerformanceCalculationTypeDefinition>().Property(item => item.Name).HasMaxLength(120);
        builder.Entity<PerformanceCalculationTypeDefinition>().Property(item => item.Description).HasMaxLength(1000);
        builder.Entity<PerformanceCalculationTypeDefinition>().HasData(CalculationTypes());

        builder.Entity<OpmsTarget>().HasOne(item => item.CalculationType).WithMany().HasForeignKey(item => item.CalculationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsTarget>().HasOne(item => item.CalculationType).WithMany().HasForeignKey(item => item.CalculationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().Property(item => item.DerivedFromPeriods).HasMaxLength(100);
        builder.Entity<PerformancePeriodTarget>().Property(item => item.SystemSuggestedTargetValue).HasMaxLength(1024);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.DerivedCalculationType).WithMany().HasForeignKey(item => item.DerivedCalculationTypeId).OnDelete(DeleteBehavior.Restrict);

        ConfigureSubmissionSuggestion<OpmsSubmission>(builder, "OpmsSubmissions");
        ConfigureSubmissionSuggestion<IpmsSubmission>(builder, "IpmsSubmissions");

        builder.Entity<MunicipalityConsolidationPolicy>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<MunicipalityConsolidationPolicy>().HasIndex(item => new { item.MunicipalityId, item.CalculationTypeId }).IsUnique();
        ConfigureRowVersion(builder.Entity<MunicipalityConsolidationPolicy>().Property(item => item.RowVersion));
        builder.Entity<MunicipalityConsolidationPolicy>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityConsolidationPolicy>().HasOne(item => item.CalculationType).WithMany().HasForeignKey(item => item.CalculationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityConsolidationPolicy>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityConsolidationPolicy>().HasOne(item => item.ModifiedByUser).WithMany().HasForeignKey(item => item.ModifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityConsolidationPolicy>().ToTable(table => table.HasCheckConstraint("CK_MunicipalityConsolidationPolicies_PrimitiveRule", "[ConsolidationRule] IS NULL OR [ConsolidationRule] IN (1,2,3)"));
        builder.Entity<MunicipalityConsolidationPolicy>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<PerformanceSuggestionEvent>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceSuggestionEvent>().HasIndex(item => new { item.MunicipalityId, item.SubmissionKind, item.OpmsSubmissionId, item.IpmsSubmissionId, item.OccurredAt });
        builder.Entity<PerformanceSuggestionEvent>().Property(item => item.SystemSuggestedActualPerformance).HasMaxLength(1024);
        builder.Entity<PerformanceSuggestionEvent>().Property(item => item.ActualPerformance).HasMaxLength(1024);
        builder.Entity<PerformanceSuggestionEvent>().Property(item => item.SourcePeriods).HasMaxLength(100);
        builder.Entity<PerformanceSuggestionEvent>().Property(item => item.Reason).HasMaxLength(2000);
        builder.Entity<PerformanceSuggestionEvent>().Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Entity<PerformanceSuggestionEvent>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceSuggestionEvent>().HasOne(item => item.OpmsSubmission).WithMany(item => item.SuggestionEvents).HasForeignKey(item => item.OpmsSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceSuggestionEvent>().HasOne(item => item.IpmsSubmission).WithMany(item => item.SuggestionEvents).HasForeignKey(item => item.IpmsSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceSuggestionEvent>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceSuggestionEvent>().HasOne(item => item.SuggestionCalculationType).WithMany().HasForeignKey(item => item.SuggestionCalculationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceSuggestionEvent>().HasOne(item => item.ActorUser).WithMany().HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceSuggestionEvent>().ToTable(table => table.HasCheckConstraint("CK_PerformanceSuggestionEvents_OneSubmission", "CASE WHEN [OpmsSubmissionId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsSubmissionId] IS NULL THEN 0 ELSE 1 END = 1"));
        builder.Entity<PerformanceSuggestionEvent>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }

    private static void ConfigureSubmissionSuggestion<TSubmission>(ModelBuilder builder, string tableName) where TSubmission : class
    {
        var entity = builder.Entity<TSubmission>();
        entity.Property<string?>(nameof(OpmsSubmission.SystemSuggestedActualPerformance)).HasMaxLength(1024);
        entity.Property<string?>(nameof(OpmsSubmission.SuggestionEditReason)).HasMaxLength(2000);
        entity.HasOne<PerformanceCalculationTypeDefinition>(nameof(OpmsSubmission.SuggestionCalculationType)).WithMany().HasForeignKey(nameof(OpmsSubmission.SuggestionCalculationTypeId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ApplicationUser>(nameof(OpmsSubmission.SuggestionEditedByUser)).WithMany().HasForeignKey(nameof(OpmsSubmission.SuggestionEditedByUserId)).OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(tableName, table => table.HasCheckConstraint($"CK_{tableName}_SuggestionEditMetadata", "[WasSystemSuggestionEdited] = 0 OR ([SystemSuggestedActualPerformance] IS NOT NULL AND [SuggestionEditedByUserId] IS NOT NULL AND [SuggestionEditedAt] IS NOT NULL)"));
    }

    private static PerformanceCalculationTypeDefinition[] CalculationTypes() =>
    [
        Definition(1, "SUM", "Sum", "Add the valid source-period values."),
        Definition(2, "AVERAGE", "Average", "Average the valid source-period values."),
        Definition(3, "LATEST_VALUE", "Latest value", "Use the latest valid source-period value."),
        Definition(4, "CUMULATIVE", "Cumulative", "Use the latest cumulative position unless configured otherwise."),
        Definition(5, "NON_CUMULATIVE", "Non-cumulative", "Use an explicit configured primitive rule or manual capture."),
        Definition(6, "REVERSE_CUMULATIVE", "Reverse cumulative", "Use the latest reducing position unless configured otherwise."),
        Definition(7, "REVERSE_NON_CUMULATIVE", "Reverse non-cumulative", "Use an explicit configured reverse primitive rule or manual capture."),
        Definition(8, "ZERO_BASED", "Zero-based", "Use an explicit configured zero-based primitive rule or manual capture."),
        Definition(9, "MANUAL", "Manual", "Do not automatically consolidate unless explicitly configured."),
    ];

    private static PerformanceCalculationTypeDefinition Definition(long id, string code, string name, string description) =>
        new() { Id = id, PublicId = Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}"), Code = code, Name = name, Description = description, IsActive = true };
}

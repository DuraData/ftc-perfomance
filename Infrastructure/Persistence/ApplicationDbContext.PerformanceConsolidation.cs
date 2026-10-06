using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<PerformanceCalculationTypeDefinition> PerformanceCalculationTypes { get; set; } = null!;
    public DbSet<OpmsUnitDefinition> OpmsUnitDefinitions { get; set; } = null!;
    public DbSet<PerformanceDirectionDefinition> PerformanceDirectionDefinitions { get; set; } = null!;
    public DbSet<MunicipalityConsolidationPolicy> MunicipalityConsolidationPolicies { get; set; } = null!;
    public DbSet<PerformanceSuggestionEvent> PerformanceSuggestionEvents { get; set; } = null!;

    private void ConfigurePerformanceConsolidation(ModelBuilder builder)
    {
        ConfigurePerformanceConfiguration(builder);
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

    private void ConfigurePerformanceConfiguration(ModelBuilder builder)
    {
        builder.Entity<PerformanceDirectionDefinition>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceDirectionDefinition>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<PerformanceDirectionDefinition>().Property(item => item.Code).HasMaxLength(50);
        builder.Entity<PerformanceDirectionDefinition>().Property(item => item.Name).HasMaxLength(150);
        builder.Entity<PerformanceDirectionDefinition>().Property(item => item.Description).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<PerformanceDirectionDefinition>().Property(item => item.RowVersion));
        builder.Entity<PerformanceDirectionDefinition>().HasData(PerformanceDirections());

        builder.Entity<OpmsUnitDefinition>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsUnitDefinition>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<OpmsUnitDefinition>().Property(item => item.Code).HasMaxLength(50);
        builder.Entity<OpmsUnitDefinition>().Property(item => item.Name).HasMaxLength(150);
        builder.Entity<OpmsUnitDefinition>().Property(item => item.InputControlType).HasMaxLength(50);
        builder.Entity<OpmsUnitDefinition>().Property(item => item.ValueDataType).HasMaxLength(30);
        builder.Entity<OpmsUnitDefinition>().Property(item => item.Symbol).HasMaxLength(20);
        builder.Entity<OpmsUnitDefinition>().Property(item => item.MinValue).HasPrecision(19, 6);
        builder.Entity<OpmsUnitDefinition>().Property(item => item.MaxValue).HasPrecision(19, 6);
        ConfigureRowVersion(builder.Entity<OpmsUnitDefinition>().Property(item => item.RowVersion));
        builder.Entity<OpmsUnitDefinition>().HasOne(item => item.DefaultPerformanceDirection).WithMany()
            .HasForeignKey(item => item.DefaultPerformanceDirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsUnitDefinition>().HasData(OpmsUnits());

        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.OpmsUnit).WithMany()
            .HasForeignKey(item => item.OpmsUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.PerformanceDirectionDefinition).WithMany()
            .HasForeignKey(item => item.PerformanceDirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.RevisedOpmsUnit).WithMany()
            .HasForeignKey(item => item.RevisedOpmsUnitId).OnDelete(DeleteBehavior.Restrict);
    }

    private static PerformanceDirectionDefinition[] PerformanceDirections() =>
    [
        Direction(1, "TARGET_OR_HIGHER", "Target or higher", "Actual at or above target is achieved.", PerformanceDirection.HigherIsBetter),
        Direction(2, "TARGET_OR_LOWER", "Target or lower", "Actual at or below target is achieved.", PerformanceDirection.LowerIsBetter),
        Direction(3, "EXACT", "Exact", "Actual must equal target.", PerformanceDirection.Exact),
        Direction(4, "HIGHER_BETTER", "Higher is better", "Higher normalized performance is favourable.", PerformanceDirection.HigherIsBetter),
        Direction(5, "LOWER_BETTER", "Lower is better", "Lower normalized performance is favourable.", PerformanceDirection.LowerIsBetter),
        Direction(6, "ON_OR_BEFORE_DATE", "On or before date", "Actual date must be on or before the target date.", PerformanceDirection.LowerIsBetter),
        Direction(7, "ON_OR_AFTER_DATE", "On or after date", "Actual date must be on or after the target date.", PerformanceDirection.HigherIsBetter),
        Direction(8, "YES_IS_SUCCESS", "Yes is success", "A Yes actual is achieved.", PerformanceDirection.Exact),
        Direction(9, "NO_IS_SUCCESS", "No is success", "A No actual is achieved.", PerformanceDirection.Exact),
        Direction(10, "MANUAL", "Manual", "The system does not determine achievement.", PerformanceDirection.Exact),
    ];

    private static PerformanceDirectionDefinition Direction(long id, string code, string name, string description, PerformanceDirection engineDirection) =>
        new() { Id = id, PublicId = Guid.Parse($"10000000-0000-0000-0000-{id:000000000000}"), Code = code, Name = name, Description = description, EngineDirection = engineDirection, IsActive = true };

    private static OpmsUnitDefinition[] OpmsUnits() =>
    [
        Unit(1, "NUMBER", "Number", "NUMERIC", "DECIMAL", null, 2, 0, null, true, 1, false, false, PerformanceUnitKind.AbsoluteCount),
        Unit(2, "PERCENT", "Percentage", "NUMERIC", "DECIMAL", "%", 2, 0, 100, true, 1, false, false, PerformanceUnitKind.PercentageBased),
        Unit(3, "FINANCIAL", "Financial", "CURRENCY", "DECIMAL", "R", 2, 0, null, true, 1, false, false, PerformanceUnitKind.Financial),
        Unit(4, "DATE", "Date", "DATE", "DATE", null, null, null, null, true, 6, false, false, PerformanceUnitKind.Date),
        Unit(5, "RATIO", "Ratio", "RATIO", "JSON", null, 6, 0, null, true, 1, true, false, PerformanceUnitKind.Ratios),
        Unit(6, "TIME", "Time", "NUMERIC_UNIT", "DECIMAL", null, 2, 0, null, true, 2, true, false, PerformanceUnitKind.TimeBased),
        Unit(7, "AREA", "Area", "NUMERIC_UNIT", "DECIMAL", null, 2, 0, null, true, 1, true, false, PerformanceUnitKind.AreaBased),
        Unit(8, "VOLUME", "Volume", "NUMERIC_UNIT", "DECIMAL", null, 2, 0, null, true, 1, true, false, PerformanceUnitKind.VolumeBased),
        Unit(9, "YES_NO", "Yes or No", "SELECT", "BOOLEAN", null, null, null, null, true, 8, false, false, PerformanceUnitKind.Binary),
        Unit(10, "SCALE_1_3", "Scale 1 to 3", "SELECT", "INTEGER", null, 0, 1, 3, true, 1, false, false, PerformanceUnitKind.ReadinessScale),
        Unit(11, "ZERO_NUMBER", "Zero number", "NUMERIC", "DECIMAL", null, 2, 0, null, true, 2, false, false, PerformanceUnitKind.ZeroBased),
        Unit(12, "QUALITATIVE", "Qualitative", "TEXT", "TEXT", null, null, null, null, false, 10, false, true, PerformanceUnitKind.QualitativeTargets),
    ];

    private static OpmsUnitDefinition Unit(long id, string code, string name, string inputControlType, string valueDataType, string? symbol,
        int? decimalPlaces, decimal? minValue, decimal? maxValue, bool supportsAutoVariance, long? defaultDirectionId,
        bool requiresComponentUi, bool isQualitative, PerformanceUnitKind engineUnitKind) => new()
    {
        Id = id, PublicId = Guid.Parse($"20000000-0000-0000-0000-{id:000000000000}"), Code = code, Name = name,
        InputControlType = inputControlType, ValueDataType = valueDataType, Symbol = symbol, DecimalPlaces = decimalPlaces,
        MinValue = minValue, MaxValue = maxValue, SupportsAutoVariance = supportsAutoVariance,
        DefaultPerformanceDirectionId = defaultDirectionId, RequiresComponentUi = requiresComponentUi,
        IsQualitative = isQualitative, EngineUnitKind = engineUnitKind, IsActive = true
    };
}

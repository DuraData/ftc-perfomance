using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    private readonly ITenantContext? _tenantContext;
    private bool TenantFilterBypass => _tenantContext == null || (_tenantContext.IsSystem && !_tenantContext.MunicipalityId.HasValue);
    private long? CurrentMunicipalityId => _tenantContext?.MunicipalityId;
    private long CurrentMunicipalityIdOrSentinel => CurrentMunicipalityId ?? long.MinValue;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext? tenantContext = null) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<LoginAuditLog> LoginAuditLogs { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<UserScope> UserScopes { get; set; } = null!;
    public DbSet<UserAssignment> UserAssignments { get; set; } = null!;
    public DbSet<Period> Periods { get; set; } = null!;
    public DbSet<StrategicGoal> StrategicGoals { get; set; } = null!;
    public DbSet<StrategicObjective> StrategicObjectives { get; set; } = null!;
    public DbSet<BudgetSource> BudgetSources { get; set; } = null!;
    public DbSet<BudgetType> BudgetTypes { get; set; } = null!;
    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; } = null!;
    public DbSet<Ward> Wards { get; set; } = null!;
    public DbSet<VoteNumber> VoteNumbers { get; set; } = null!;
    public DbSet<OpmsTarget> OpmsTargets { get; set; } = null!;
    public DbSet<OpmsTargetWard> OpmsTargetWards { get; set; } = null!;
    public DbSet<OpmsTargetAdditionalAssignee> OpmsTargetAdditionalAssignees { get; set; } = null!;
    public DbSet<OpmsTargetVoteNumber> OpmsTargetVoteNumbers { get; set; } = null!;
    public DbSet<IpmsTarget> IpmsTargets { get; set; } = null!;
    public DbSet<OpmsTargetTemplate> OpmsTargetTemplates { get; set; } = null!;
    public DbSet<IpmsTargetTemplate> IpmsTargetTemplates { get; set; } = null!;
    public DbSet<OpmsTargetTemplateVersion> OpmsTargetTemplateVersions { get; set; } = null!;
    public DbSet<IpmsTargetTemplateVersion> IpmsTargetTemplateVersions { get; set; } = null!;
    public DbSet<OpmsSubmission> OpmsSubmissions { get; set; } = null!;
    public DbSet<IpmsSubmission> IpmsSubmissions { get; set; } = null!;
    public DbSet<GovernedRecordLifecycleEvent> GovernedRecordLifecycleEvents { get; set; } = null!;
    public DbSet<EvidenceBlob> EvidenceBlobs { get; set; } = null!;
    public DbSet<PoeFile> PoeFiles { get; set; } = null!;
    public DbSet<PoeEvidenceAssessment> PoeEvidenceAssessments { get; set; } = null!;
    public DbSet<PoeEvidenceReplacement> PoeEvidenceReplacements { get; set; } = null!;
    public DbSet<PoeLegalHoldEvent> PoeLegalHoldEvents { get; set; } = null!;
    public DbSet<PoeDisposalEvent> PoeDisposalEvents { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<AuditTrail> AuditTrails { get; set; } = null!;
    public DbSet<BusinessEventOutbox> BusinessEventOutbox { get; set; } = null!;
    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts { get; set; } = null!;
    public DbSet<IdempotencyRequest> IdempotencyRequests { get; set; } = null!;
    public DbSet<DueDateExtension> DueDateExtensions { get; set; } = null!;
    public DbSet<ReviewComment> ReviewComments { get; set; } = null!;
    public DbSet<AuditFinding> AuditFindings { get; set; } = null!;
    public DbSet<SubmissionScore> SubmissionScores { get; set; } = null!;
    public DbSet<IdpPlan> IdpPlans { get; set; } = null!;
    public DbSet<IdpPlanVersion> IdpPlanVersions { get; set; } = null!;
    public DbSet<IdpChangeLog> IdpChangeLogs { get; set; } = null!;
    public DbSet<IdpStrategicOutcome> IdpStrategicOutcomes { get; set; } = null!;
    public DbSet<IdpStrategicObjective> IdpStrategicObjectives { get; set; } = null!;
    public DbSet<IdpDevelopmentPriority> IdpDevelopmentPriorities { get; set; } = null!;
    public DbSet<IdpProgramme> IdpProgrammes { get; set; } = null!;
    public DbSet<IdpProject> IdpProjects { get; set; } = null!;
    public DbSet<IdpKpi> IdpKpis { get; set; } = null!;
    public DbSet<IdpImportBatch> IdpImportBatches { get; set; } = null!;
    public DbSet<IdpImportRow> IdpImportRows { get; set; } = null!;
    public DbSet<IdpAnnualTarget> IdpAnnualTargets { get; set; } = null!;
    public DbSet<IdpAlignmentLink> IdpAlignmentLinks { get; set; } = null!;
    public DbSet<IdpCommunitySession> IdpCommunitySessions { get; set; } = null!;
    public DbSet<IdpCommunityNeed> IdpCommunityNeeds { get; set; } = null!;
    public DbSet<IdpWardInput> IdpWardInputs { get; set; } = null!;
    public DbSet<IdpStakeholderEngagement> IdpStakeholderEngagements { get; set; } = null!;
    public DbSet<IdpRiskLink> IdpRiskLinks { get; set; } = null!;
    public DbSet<IdpBudgetSnapshot> IdpBudgetSnapshots { get; set; } = null!;
    public DbSet<IdpDocument> IdpDocuments { get; set; } = null!;
    public DbSet<IdpCollaborationComment> IdpCollaborationComments { get; set; } = null!;
    public DbSet<IdpTaskAssignment> IdpTaskAssignments { get; set; } = null!;
    public DbSet<Municipality> Municipalities { get; set; } = null!;
    public DbSet<SecurityResource> SecurityResources { get; set; } = null!;
    public DbSet<SecurityNavigationItem> SecurityNavigationItems { get; set; } = null!;
    public DbSet<SecurityActionDefinition> SecurityActionDefinitions { get; set; } = null!;
    public DbSet<SecurityMemberDefinition> SecurityMemberDefinitions { get; set; } = null!;
    public DbSet<SecurityUserRoleAssignment> SecurityUserRoleAssignments { get; set; } = null!;
    public DbSet<FinancialYear> FinancialYears { get; set; } = null!;
    public DbSet<MunicipalityFinancialYear> MunicipalityFinancialYears { get; set; } = null!;
    public DbSet<ReportingPeriod> ReportingPeriods { get; set; } = null!;
    public DbSet<MunicipalEmployee> MunicipalEmployees { get; set; } = null!;
    public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; } = null!;
    public DbSet<PerformancePeriodTarget> PerformancePeriodTargets { get; set; } = null!;
    public DbSet<PerformanceTargetRevision> PerformanceTargetRevisions { get; set; } = null!;
    public DbSet<LegacySubmissionValueArchive> LegacySubmissionValueArchives { get; set; } = null!;
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = null!;
    public DbSet<WorkflowStageDefinition> WorkflowStageDefinitions { get; set; } = null!;
    public DbSet<SubmissionWorkflowInstance> SubmissionWorkflowInstances { get; set; } = null!;
    public DbSet<SubmissionWorkflowAction> SubmissionWorkflowActions { get; set; } = null!;
    public DbSet<PerformanceRfi> PerformanceRfis { get; set; } = null!;
    public DbSet<PerformanceRfiEvidence> PerformanceRfiEvidenceLinks { get; set; } = null!;
    public DbSet<ReportingWindow> ReportingWindows { get; set; } = null!;
    public DbSet<ReportingWindowException> ReportingWindowExceptions { get; set; } = null!;
    public DbSet<RatingScheme> RatingSchemes { get; set; } = null!;
    public DbSet<RatingSchemeValue> RatingSchemeValues { get; set; } = null!;
    public DbSet<SubmissionStageRating> SubmissionStageRatings { get; set; } = null!;
    public DbSet<TechnicalIndicatorDescription> TechnicalIndicatorDescriptions { get; set; } = null!;
    public DbSet<TidSourceDocument> TidSourceDocuments { get; set; } = null!;
    public DbSet<StrategicDocumentType> StrategicDocumentTypes { get; set; } = null!;
    public DbSet<StrategicDocument> StrategicDocuments { get; set; } = null!;
    public DbSet<StrategicDocumentEvent> StrategicDocumentEvents { get; set; } = null!;
    public DbSet<C88CatalogueVersion> C88CatalogueVersions { get; set; } = null!;
    public DbSet<C88MunicipalityConfiguration> C88MunicipalityConfigurations { get; set; } = null!;
    public DbSet<C88CatalogueItem> C88CatalogueItems { get; set; } = null!;
    public DbSet<C88Indicator> C88Indicators { get; set; } = null!;
    public DbSet<C88DataElement> C88DataElements { get; set; } = null!;
    public DbSet<C88IndicatorApplicability> C88IndicatorApplicabilities { get; set; } = null!;
    public DbSet<C88ComplianceQuestion> C88ComplianceQuestions { get; set; } = null!;
    public DbSet<C88IndicatorPlan> C88IndicatorPlans { get; set; } = null!;
    public DbSet<C88ReportingCalendar> C88ReportingCalendars { get; set; } = null!;
    public DbSet<C88IndicatorReport> C88IndicatorReports { get; set; } = null!;
    public DbSet<C88DataElementValue> C88DataElementValues { get; set; } = null!;
    public DbSet<C88ComplianceResponse> C88ComplianceResponses { get; set; } = null!;
    public DbSet<C88Assignment> C88Assignments { get; set; } = null!;
    public DbSet<C88WorkflowDefinition> C88WorkflowDefinitions { get; set; } = null!;
    public DbSet<C88WorkflowStage> C88WorkflowStages { get; set; } = null!;
    public DbSet<C88WorkflowAction> C88WorkflowActions { get; set; } = null!;
    public DbSet<C88OpmsMapping> C88OpmsMappings { get; set; } = null!;
    public DbSet<AuthenticationConfiguration> AuthenticationConfigurations { get; set; } = null!;
    public DbSet<AuthenticationPolicy> AuthenticationPolicies { get; set; } = null!;
    public DbSet<UserAuthenticator> UserAuthenticators { get; set; } = null!;
    public DbSet<AuthenticationEvent> AuthenticationEvents { get; set; } = null!;
    public DbSet<NotificationConfiguration> NotificationConfigurations { get; set; } = null!;
    public DbSet<NotificationScheduleRule> NotificationScheduleRules { get; set; } = null!;
    public DbSet<WorkingCalendarHoliday> WorkingCalendarHolidays { get; set; } = null!;
    public DbSet<ScheduledNotification> ScheduledNotifications { get; set; } = null!;
    public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigurePerformanceConsolidation(builder);
        ConfigureInternalAuditAssessments(builder);
        ConfigureOfficialReports(builder);
        ConfigureNotificationPolicies(builder);

        builder.Entity<Municipality>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<Municipality>().HasIndex(item => item.Code).IsUnique();
        ConfigureRowVersion(builder.Entity<Municipality>().Property(item => item.RowVersion));

        builder.Entity<ApplicationUser>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<ApplicationUser>().Property(item => item.RowVersion));
        builder.Entity<ApplicationUser>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AuthenticationConfiguration>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<AuthenticationConfiguration>().HasIndex(item => item.MunicipalityId).IsUnique();
        builder.Entity<AuthenticationConfiguration>().Property(item => item.ProviderRegistrationCode).HasMaxLength(40);
        builder.Entity<AuthenticationConfiguration>().Property(item => item.DisplayName).HasMaxLength(160);
        builder.Entity<AuthenticationConfiguration>().ToTable(table => table.HasCheckConstraint("CK_AuthenticationConfigurations_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        ConfigureRowVersion(builder.Entity<AuthenticationConfiguration>().Property(item => item.RowVersion));
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.ModifiedByUser).WithMany().HasForeignKey(item => item.ModifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<AuthenticationPolicy>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<AuthenticationPolicy>().HasIndex(item => item.AuthenticationConfigurationId).IsUnique();
        builder.Entity<AuthenticationPolicy>().HasIndex(item => item.MunicipalityId).IsUnique();
        builder.Entity<AuthenticationPolicy>().ToTable(table => table.HasCheckConstraint("CK_AuthenticationPolicies_Bounds", "[MinimumPasswordLength] BETWEEN 12 AND 128 AND [MaximumFailedAttempts] BETWEEN 1 AND 20 AND [LockoutMinutes] BETWEEN 1 AND 1440 AND [SessionIdleTimeoutMinutes] BETWEEN 5 AND 1440 AND [SessionAbsoluteTimeoutHours] BETWEEN 1 AND 720 AND [MaximumConcurrentSessions] BETWEEN 1 AND 50"));
        ConfigureRowVersion(builder.Entity<AuthenticationPolicy>().Property(item => item.RowVersion));
        builder.Entity<AuthenticationPolicy>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationPolicy>().HasOne(item => item.AuthenticationConfiguration).WithOne(item => item.Policy).HasForeignKey<AuthenticationPolicy>(item => item.AuthenticationConfigurationId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AuthenticationPolicy>().HasOne(item => item.ModifiedByUser).WithMany().HasForeignKey(item => item.ModifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationPolicy>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<UserAuthenticator>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<UserAuthenticator>().HasIndex(item => new { item.MunicipalityId, item.UserId, item.ProviderRegistrationCode }).IsUnique();
        builder.Entity<UserAuthenticator>().HasIndex(item => new { item.MunicipalityId, item.ProviderRegistrationCode, item.ExpectedEmail });
        builder.Entity<UserAuthenticator>().HasIndex(item => item.ExternalIdentityHash).IsUnique().HasFilter("[ExternalIdentityHash] IS NOT NULL");
        builder.Entity<UserAuthenticator>().Property(item => item.ProviderRegistrationCode).HasMaxLength(40);
        builder.Entity<UserAuthenticator>().Property(item => item.ExpectedEmail).HasMaxLength(320);
        builder.Entity<UserAuthenticator>().Property(item => item.Issuer).HasMaxLength(512);
        builder.Entity<UserAuthenticator>().Property(item => item.Subject).HasMaxLength(512);
        builder.Entity<UserAuthenticator>().Property(item => item.ExternalIdentityHash).HasMaxLength(64);
        ConfigureRowVersion(builder.Entity<UserAuthenticator>().Property(item => item.RowVersion));
        builder.Entity<UserAuthenticator>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserAuthenticator>().HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserAuthenticator>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserAuthenticator>().HasOne(item => item.LinkedByUser).WithMany().HasForeignKey(item => item.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserAuthenticator>().HasOne(item => item.DisabledByUser).WithMany().HasForeignKey(item => item.DisabledByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserAuthenticator>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<AuthenticationEvent>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<AuthenticationEvent>().HasIndex(item => new { item.MunicipalityId, item.OccurredAt });
        builder.Entity<AuthenticationEvent>().Property(item => item.ProviderCode).HasMaxLength(40);
        builder.Entity<AuthenticationEvent>().Property(item => item.EventType).HasMaxLength(80);
        builder.Entity<AuthenticationEvent>().Property(item => item.FailureCode).HasMaxLength(160);
        builder.Entity<AuthenticationEvent>().Property(item => item.IpAddress).HasMaxLength(64);
        builder.Entity<AuthenticationEvent>().Property(item => item.UserAgent).HasMaxLength(1024);
        builder.Entity<AuthenticationEvent>().Property(item => item.CorrelationId).HasMaxLength(128);
        builder.Entity<AuthenticationEvent>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationEvent>().HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationEvent>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<FinancialYear>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<FinancialYear>().HasIndex(item => item.Code).IsUnique();
        ConfigureRowVersion(builder.Entity<FinancialYear>().Property(item => item.RowVersion));
        builder.Entity<FinancialYear>().ToTable(table => table.HasCheckConstraint("CK_FinancialYears_DateRange", "[EndDate] >= [StartDate]"));
        builder.Entity<MunicipalityFinancialYear>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<MunicipalityFinancialYear>().HasIndex(item => new { item.MunicipalityId, item.FinancialYearId }).IsUnique();
        ConfigureRowVersion(builder.Entity<MunicipalityFinancialYear>().Property(item => item.RowVersion));
        builder.Entity<MunicipalityFinancialYear>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityFinancialYear>().HasOne(item => item.FinancialYear).WithMany().HasForeignKey(item => item.FinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingPeriod>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<ReportingPeriod>().HasIndex(item => new { item.MunicipalityFinancialYearId, item.Code }).IsUnique();
        ConfigureRowVersion(builder.Entity<ReportingPeriod>().Property(item => item.RowVersion));
        builder.Entity<ReportingPeriod>().HasOne(item => item.MunicipalityFinancialYear).WithMany(item => item.ReportingPeriods).HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingPeriod>().ToTable(table => table.HasCheckConstraint("CK_ReportingPeriods_DateRange", "[EndDate] >= [StartDate]"));

        builder.Entity<MunicipalEmployee>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<MunicipalEmployee>().HasIndex(item => new { item.MunicipalityId, item.EmployeeNumber }).IsUnique();
        builder.Entity<MunicipalEmployee>().HasIndex(item => item.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
        ConfigureRowVersion(builder.Entity<MunicipalEmployee>().Property(item => item.RowVersion));
        builder.Entity<MunicipalEmployee>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalEmployee>().HasOne(item => item.IdentityUser).WithMany().HasForeignKey(item => item.IdentityUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmployeeAssignment>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<EmployeeAssignment>().HasIndex(item => new { item.MunicipalityId, item.MunicipalEmployeeId, item.EffectiveFrom });
        ConfigureRowVersion(builder.Entity<EmployeeAssignment>().Property(item => item.RowVersion));
        builder.Entity<EmployeeAssignment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmployeeAssignment>().HasOne(item => item.MunicipalEmployee).WithMany(item => item.Assignments).HasForeignKey(item => item.MunicipalEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmployeeAssignment>().HasOne(item => item.Department).WithMany().HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmployeeAssignment>().HasOne(item => item.Unit).WithMany().HasForeignKey(item => item.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmployeeAssignment>().HasOne(item => item.Position).WithMany(item => item.EmployeeAssignments).HasForeignKey(item => item.PositionId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PerformancePeriodTarget>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformancePeriodTarget>().HasIndex(item => new { item.OpmsTargetId, item.ReportingPeriodId }).IsUnique().HasFilter("[OpmsTargetId] IS NOT NULL");
        builder.Entity<PerformancePeriodTarget>().HasIndex(item => new { item.IpmsTargetId, item.ReportingPeriodId }).IsUnique().HasFilter("[IpmsTargetId] IS NOT NULL");
        builder.Entity<PerformancePeriodTarget>().Property(item => item.TargetValue).HasMaxLength(1024);
        builder.Entity<PerformancePeriodTarget>().Property(item => item.BudgetValue).HasPrecision(18, 2);
        ConfigureRowVersion(builder.Entity<PerformancePeriodTarget>().Property(item => item.RowVersion));
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.OpmsTarget).WithMany().HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.IpmsTarget).WithMany().HasForeignKey(item => item.IpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().ToTable(table => table.HasCheckConstraint("CK_PerformancePeriodTargets_OneKpi", "CASE WHEN [OpmsTargetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsTargetId] IS NULL THEN 0 ELSE 1 END = 1"));
        builder.Entity<PerformanceTargetRevision>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceTargetRevision>().HasIndex(item => new { item.PerformancePeriodTargetId, item.RecordedAt });
        builder.Entity<PerformanceTargetRevision>().Property(item => item.FieldName).HasMaxLength(100);
        builder.Entity<PerformanceTargetRevision>().Property(item => item.OriginalValue).HasMaxLength(2048);
        builder.Entity<PerformanceTargetRevision>().Property(item => item.RevisedValue).HasMaxLength(2048);
        builder.Entity<PerformanceTargetRevision>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceTargetRevision>().HasOne(item => item.PerformancePeriodTarget).WithMany(item => item.Revisions).HasForeignKey(item => item.PerformancePeriodTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceTargetRevision>().HasOne(item => item.RevisedByUser).WithMany().HasForeignKey(item => item.RevisedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LegacySubmissionValueArchive>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<LegacySubmissionValueArchive>().HasIndex(item => item.OpmsSubmissionId).IsUnique().HasFilter("[OpmsSubmissionId] IS NOT NULL");
        builder.Entity<LegacySubmissionValueArchive>().HasIndex(item => item.IpmsSubmissionId).IsUnique().HasFilter("[IpmsSubmissionId] IS NOT NULL");
        builder.Entity<LegacySubmissionValueArchive>().Property(item => item.LegacyActual).HasPrecision(18, 2);
        builder.Entity<LegacySubmissionValueArchive>().Property(item => item.LegacyActualDescription).HasMaxLength(4000);
        builder.Entity<LegacySubmissionValueArchive>().Property(item => item.LegacyActualPerformanceDescription).HasMaxLength(4000);
        builder.Entity<LegacySubmissionValueArchive>().Property(item => item.CanonicalActualPerformance).HasMaxLength(4000);
        builder.Entity<LegacySubmissionValueArchive>().Property(item => item.ArchiveReason).HasMaxLength(500);
        builder.Entity<LegacySubmissionValueArchive>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionValueArchive>().HasOne(item => item.OpmsSubmission).WithMany().HasForeignKey(item => item.OpmsSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionValueArchive>().HasOne(item => item.IpmsSubmission).WithMany().HasForeignKey(item => item.IpmsSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionValueArchive>().ToTable(table => table.HasCheckConstraint("CK_LegacySubmissionValueArchives_OneSubmission", "CASE WHEN [OpmsSubmissionId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsSubmissionId] IS NULL THEN 0 ELSE 1 END = 1"));

        builder.Entity<WorkflowDefinition>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<WorkflowDefinition>().HasIndex(x => new { x.MunicipalityId, x.MunicipalityFinancialYearId, x.SubmissionKind, x.Code, x.Version }).IsUnique();
        ConfigureRowVersion(builder.Entity<WorkflowDefinition>().Property(x => x.RowVersion));
        builder.Entity<WorkflowDefinition>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkflowDefinition>().HasOne(x => x.MunicipalityFinancialYear).WithMany().HasForeignKey(x => x.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkflowStageDefinition>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<WorkflowStageDefinition>().HasIndex(x => new { x.WorkflowDefinitionId, x.Code }).IsUnique();
        builder.Entity<WorkflowStageDefinition>().HasIndex(x => new { x.WorkflowDefinitionId, x.Sequence }).IsUnique();
        ConfigureRowVersion(builder.Entity<WorkflowStageDefinition>().Property(x => x.RowVersion));
        builder.Entity<WorkflowStageDefinition>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkflowStageDefinition>().HasOne(x => x.WorkflowDefinition).WithMany(x => x.Stages).HasForeignKey(x => x.WorkflowDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkflowStageDefinition>().HasOne(x => x.RatingScheme).WithMany().HasForeignKey(x => x.RatingSchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowInstance>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<SubmissionWorkflowInstance>().HasIndex(x => new { x.MunicipalityId, x.SubmissionKind, x.SubmissionId }).IsUnique();
        ConfigureRowVersion(builder.Entity<SubmissionWorkflowInstance>().Property(x => x.RowVersion));
        builder.Entity<SubmissionWorkflowInstance>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowInstance>().HasOne(x => x.WorkflowDefinition).WithMany().HasForeignKey(x => x.WorkflowDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowInstance>().HasOne(x => x.CurrentStage).WithMany().HasForeignKey(x => x.CurrentStageId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowAction>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<SubmissionWorkflowAction>().HasIndex(x => new { x.SubmissionWorkflowInstanceId, x.Sequence }).IsUnique();
        builder.Entity<SubmissionWorkflowAction>().Property(x => x.RatingValue).HasPrecision(18, 4);
        builder.Entity<SubmissionWorkflowAction>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowAction>().HasOne(x => x.SubmissionWorkflowInstance).WithMany(x => x.Actions).HasForeignKey(x => x.SubmissionWorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowAction>().HasOne(x => x.FromStage).WithMany().HasForeignKey(x => x.FromStageId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowAction>().HasOne(x => x.ToStage).WithMany().HasForeignKey(x => x.ToStageId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionWorkflowAction>().HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRfi>().HasIndex(x => x.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<PerformanceRfi>().Property(x => x.RowVersion));
        builder.Entity<PerformanceRfi>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRfi>().HasOne(x => x.SubmissionWorkflowInstance).WithMany().HasForeignKey(x => x.SubmissionWorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRfiEvidence>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<PerformanceRfiEvidence>().HasIndex(x => new { x.PerformanceRfiId, x.PoeFileId, x.Purpose }).IsUnique();
        builder.Entity<PerformanceRfiEvidence>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<PerformanceRfiEvidence>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRfiEvidence>().HasOne(x => x.PerformanceRfi).WithMany(x => x.EvidenceLinks).HasForeignKey(x => x.PerformanceRfiId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRfiEvidence>().HasOne(x => x.PoeFile).WithMany().HasForeignKey(x => x.PoeFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceRfiEvidence>().HasOne(x => x.LinkedByUser).WithMany().HasForeignKey(x => x.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingWindow>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<ReportingWindow>().HasIndex(x => new { x.MunicipalityId, x.ReportingPeriodId, x.SubmissionKind }).IsUnique();
        ConfigureRowVersion(builder.Entity<ReportingWindow>().Property(x => x.RowVersion));
        builder.Entity<ReportingWindow>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingWindow>().HasOne(x => x.ReportingPeriod).WithMany().HasForeignKey(x => x.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingWindow>().ToTable(table => table.HasCheckConstraint("CK_ReportingWindows_Range", "[ClosesAt] > [OpensAt]"));
        builder.Entity<ReportingWindowException>().HasIndex(x => x.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<ReportingWindowException>().Property(x => x.RowVersion));
        builder.Entity<ReportingWindowException>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingWindowException>().HasOne(x => x.ReportingWindow).WithMany().HasForeignKey(x => x.ReportingWindowId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RatingScheme>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<RatingScheme>().HasIndex(x => new { x.MunicipalityId, x.Code }).IsUnique();
        ConfigureRowVersion(builder.Entity<RatingScheme>().Property(x => x.RowVersion));
        builder.Entity<RatingScheme>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RatingSchemeValue>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<RatingSchemeValue>().HasIndex(x => new { x.RatingSchemeId, x.Value }).IsUnique();
        builder.Entity<RatingSchemeValue>().Property(x => x.Value).HasPrecision(18, 4);
        builder.Entity<RatingSchemeValue>().Property(x => x.MinimumAchievementPercent).HasPrecision(18, 4);
        builder.Entity<RatingSchemeValue>().Property(x => x.MaximumAchievementPercent).HasPrecision(18, 4);
        builder.Entity<RatingSchemeValue>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RatingSchemeValue>().HasOne(x => x.RatingScheme).WithMany(x => x.Values).HasForeignKey(x => x.RatingSchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<SubmissionStageRating>().HasIndex(x => x.SubmissionWorkflowActionId).IsUnique();
        builder.Entity<SubmissionStageRating>().HasIndex(x => new { x.SubmissionWorkflowInstanceId, x.RatedAt });
        builder.Entity<SubmissionStageRating>().Property(x => x.Value).HasPrecision(18, 4);
        builder.Entity<SubmissionStageRating>().Property(x => x.AchievementPercent).HasPrecision(18, 4);
        builder.Entity<SubmissionStageRating>().Property(x => x.LabelSnapshot).HasMaxLength(200);
        builder.Entity<SubmissionStageRating>().Property(x => x.Comment).HasMaxLength(2000);
        builder.Entity<SubmissionStageRating>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.SubmissionWorkflowInstance).WithMany().HasForeignKey(x => x.SubmissionWorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.SubmissionWorkflowAction).WithMany().HasForeignKey(x => x.SubmissionWorkflowActionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.WorkflowStageDefinition).WithMany().HasForeignKey(x => x.WorkflowStageDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.RatingScheme).WithMany().HasForeignKey(x => x.RatingSchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.RatingSchemeValue).WithMany().HasForeignKey(x => x.RatingSchemeValueId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionStageRating>().HasOne(x => x.RatedByUser).WithMany().HasForeignKey(x => x.RatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LoginAuditLog>().HasIndex(item => new { item.MunicipalityId, item.LoggedAt });
        builder.Entity<LoginAuditLog>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LoginAuditLog>().HasOne(item => item.User).WithMany(item => item.LoginAuditLogs).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<LoginAuditLog>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<Municipality>().HasQueryFilter(item => TenantFilterBypass || item.Id == CurrentMunicipalityIdOrSentinel);
        builder.Entity<Department>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<Unit>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<Position>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<Ward>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<VoteNumber>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsTarget>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsTargetWard>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsTargetVoteNumber>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IpmsTarget>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsSubmission>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IpmsSubmission>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<MunicipalEmployee>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<EmployeeAssignment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<MunicipalityFinancialYear>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<ReportingPeriod>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityFinancialYear.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PerformancePeriodTarget>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PerformanceTargetRevision>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<LegacySubmissionValueArchive>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<WorkflowDefinition>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<WorkflowStageDefinition>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<SubmissionWorkflowInstance>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<SubmissionWorkflowAction>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PerformanceRfi>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PerformanceRfiEvidence>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<ReportingWindow>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<ReportingWindowException>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<RatingScheme>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<RatingSchemeValue>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<SubmissionStageRating>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<ApplicationRole>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<ApplicationRole>().HasIndex(item => new { item.MunicipalityId, item.RoleCode }).IsUnique();
        ConfigureRowVersion(builder.Entity<ApplicationRole>().Property(item => item.RowVersion));
        builder.Entity<ApplicationRole>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Permission>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<SecurityResource>().HasIndex(item => item.Code).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityResource>().Property(item => item.RowVersion));
        builder.Entity<SecurityNavigationItem>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<SecurityNavigationItem>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityNavigationItem>().Property(item => item.RowVersion));
        builder.Entity<SecurityNavigationItem>().HasOne(item => item.Parent).WithMany(item => item.Children).HasForeignKey(item => item.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SecurityActionDefinition>().HasIndex(item => item.Code).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityActionDefinition>().Property(item => item.RowVersion));
        builder.Entity<SecurityMemberDefinition>().HasIndex(item => new { item.ResourceCode, item.MemberCode }).IsUnique();

        builder.Entity<SecurityUserRoleAssignment>().HasIndex(item => new { item.UserId, item.RoleId, item.MunicipalityId, item.EffectiveFrom });
        ConfigureRowVersion(builder.Entity<SecurityUserRoleAssignment>().Property(item => item.RowVersion));
        builder.Entity<SecurityUserRoleAssignment>().HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SecurityUserRoleAssignment>().HasOne(item => item.Role).WithMany(item => item.UserAssignments).HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SecurityUserRoleAssignment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RefreshToken>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<RefreshToken>().HasIndex(item => new { item.UserId, item.SessionId, item.CreatedAt });
        builder.Entity<RefreshToken>().HasIndex(item => item.Token).IsUnique();
        builder.Entity<RefreshToken>().Property(item => item.Token).HasMaxLength(128);
        builder.Entity<RefreshToken>().Property(item => item.SecurityStamp).HasMaxLength(256);
        builder.Entity<RefreshToken>().Property(item => item.AuthenticationMethod).HasMaxLength(40);
        builder.Entity<RefreshToken>().Property(item => item.UserAgent).HasMaxLength(1024);
        builder.Entity<RefreshToken>().Property(item => item.RevokedReason).HasMaxLength(500);
        ConfigureRowVersion(builder.Entity<RefreshToken>().Property(item => item.RowVersion));

        // Configure RolePermission
        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
        ConfigureRowVersion(builder.Entity<RolePermission>().Property(item => item.RowVersion));
        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);
        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        // Configure UserPermissionOverride
        builder.Entity<UserPermissionOverride>()
            .HasKey(upo => new { upo.UserId, upo.PermissionId });
        builder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.User)
            .WithMany(u => u.PermissionOverrides)
            .HasForeignKey(upo => upo.UserId);
        builder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.Permission)
            .WithMany(p => p.UserPermissionOverrides)
            .HasForeignKey(upo => upo.PermissionId);

        builder.Entity<Department>()
            .HasIndex(d => new { d.MunicipalityId, d.Code })
            .IsUnique();
        builder.Entity<Department>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<Department>().Property(item => item.RowVersion));
        builder.Entity<Department>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Unit>()
            .HasIndex(u => new { u.MunicipalityId, u.DepartmentId, u.Code })
            .IsUnique();
        builder.Entity<Unit>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<Unit>().Property(item => item.RowVersion));
        builder.Entity<Unit>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Unit>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Units)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Position>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<Position>().HasIndex(item => new { item.MunicipalityId, item.Code }).IsUnique();
        ConfigureRowVersion(builder.Entity<Position>().Property(item => item.RowVersion));
        builder.Entity<Position>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Position>().HasOne(item => item.Department).WithMany(item => item.Positions).HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Position>().HasOne(item => item.Unit).WithMany(item => item.Positions).HasForeignKey(item => item.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Position>().ToTable(table => table.HasCheckConstraint("CK_Positions_DateRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.DepartmentEntity)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.UnitEntity)
            .WithMany(unit => unit.Users)
            .HasForeignKey(u => u.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.ManagerUser)
            .WithMany(u => u.DirectReports)
            .HasForeignKey(u => u.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>()
            .HasOne(us => us.User)
            .WithMany(u => u.Scopes)
            .HasForeignKey(us => us.UserId);

        builder.Entity<UserScope>()
            .HasOne(us => us.Municipality)
            .WithMany()
            .HasForeignKey(us => us.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>()
            .HasOne(us => us.Department)
            .WithMany(d => d.UserScopes)
            .HasForeignKey(us => us.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>()
            .HasOne(us => us.Unit)
            .WithMany(unit => unit.UserScopes)
            .HasForeignKey(us => us.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserAssignment>()
            .HasOne(ua => ua.User)
            .WithMany(u => u.Assignments)
            .HasForeignKey(ua => ua.UserId);

        builder.Entity<UserAssignment>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(ua => ua.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTargetTemplate>()
            .HasIndex(template => template.TemplateCode)
            .IsUnique();

        builder.Entity<IpmsTargetTemplate>()
            .HasIndex(template => template.TemplateCode)
            .IsUnique();

        builder.Entity<OpmsTargetTemplateVersion>()
            .HasOne(version => version.OpmsTargetTemplate)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.OpmsTargetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IpmsTargetTemplateVersion>()
            .HasOne(version => version.IpmsTargetTemplate)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.IpmsTargetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OpmsTarget>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTarget>().Property(item => item.ReasonForWithdrawal).HasMaxLength(1000);
        builder.Entity<OpmsTarget>().ToTable(table => table.HasCheckConstraint(
            "CK_OpmsTargets_WithdrawalMetadata",
            "[IsWithdrawn] = 0 OR ([ReasonForWithdrawal] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<OpmsTarget>().Property(item => item.RowVersion));
        builder.Entity<OpmsTarget>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTargetWard>().HasIndex(item => new { item.OpmsTargetId, item.WardId }).IsUnique();
        builder.Entity<OpmsTargetWard>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasOne(item => item.Target).WithMany(item => item.Wards).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasOne(item => item.Ward).WithMany().HasForeignKey(item => item.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTargetAdditionalAssignee>().HasIndex(item => new { item.OpmsTargetId, item.UserId }).IsUnique();
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.Target).WithMany(item => item.AdditionalAssignees).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTargetVoteNumber>().HasIndex(item => new { item.OpmsTargetId, item.VoteNumberId }).IsUnique();
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.Target).WithMany(item => item.VoteNumbers).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.VoteNumber).WithMany().HasForeignKey(item => item.VoteNumberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsTarget>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<IpmsTarget>().Property(item => item.ReasonForWithdrawal).HasMaxLength(1000);
        builder.Entity<IpmsTarget>().ToTable(table => table.HasCheckConstraint(
            "CK_IpmsTargets_WithdrawalMetadata",
            "[IsWithdrawn] = 0 OR ([ReasonForWithdrawal] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<IpmsTarget>().Property(item => item.RowVersion));
        builder.Entity<IpmsTarget>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(target => target.Department)
            .WithMany()
            .HasForeignKey(target => target.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(target => target.Unit)
            .WithMany()
            .HasForeignKey(target => target.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(target => target.AssignedUser)
            .WithMany()
            .HasForeignKey(target => target.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Weight)
            .HasPrecision(18, 2);

        // Add precision for new OpmsTarget decimal fields
        builder.Entity<OpmsTarget>()
            .Property(target => target.Q1Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q1Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q2Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q2Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.MidTermTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.MidTermBudget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q3Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q3Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q3RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q4Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q4Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q4RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.RevisedAnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.RevisedAnnualBudget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.Department)
            .WithMany()
            .HasForeignKey(target => target.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.Unit)
            .WithMany()
            .HasForeignKey(target => target.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.AssignedUser)
            .WithMany()
            .HasForeignKey(target => target.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.RelatedOpmsTarget)
            .WithMany()
            .HasForeignKey(target => target.RelatedOpmsTargetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .Property(target => target.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Weight)
            .HasPrecision(18, 2);

        // Add precision for new IpmsTarget decimal fields
        builder.Entity<IpmsTarget>()
            .Property(target => target.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q1Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q1Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q2Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q2Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.MidTermTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.MidTermBudget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q3Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q3Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q3RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q4Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q4Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q4RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.RevisedAnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.RevisedAnnualBudget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTargetTemplate>()
            .Property(template => template.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTargetTemplate>()
            .Property(template => template.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTargetTemplate>()
            .Property(template => template.Weight)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTargetTemplate>()
            .Property(template => template.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTargetTemplate>()
            .Property(template => template.Weight)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsSubmission>().Property(item => item.WithdrawalReason).HasMaxLength(1000);
        builder.Entity<OpmsSubmission>().ToTable(table => table.HasCheckConstraint(
            "CK_OpmsSubmissions_WithdrawalMetadata",
            "[IsDisabled] = 0 OR ([WithdrawalReason] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<OpmsSubmission>().Property(item => item.RowVersion));
        builder.Entity<OpmsSubmission>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsSubmission>().Property(item => item.ActualPerformance).HasMaxLength(1024);
        builder.Entity<OpmsSubmission>().Property(item => item.BaseState).HasMaxLength(20);
        builder.Entity<OpmsSubmission>().ToTable(table => table.HasCheckConstraint(
            "CK_OpmsSubmissions_BaseState",
            "[BaseState] IN ('IN_PROGRESS','SUBMITTED')"));
        builder.Entity<OpmsSubmission>().Property(item => item.AchievementPercent).HasPrecision(18, 4);
        builder.Entity<OpmsSubmission>().HasIndex(item => new { item.OpmsTargetId, item.ReportingPeriodId }).IsUnique().HasFilter("[ReportingPeriodId] IS NOT NULL");
        builder.Entity<OpmsSubmission>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsSubmission>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<IpmsSubmission>().Property(item => item.WithdrawalReason).HasMaxLength(1000);
        builder.Entity<IpmsSubmission>().ToTable(table => table.HasCheckConstraint(
            "CK_IpmsSubmissions_WithdrawalMetadata",
            "[IsDisabled] = 0 OR ([WithdrawalReason] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<IpmsSubmission>().Property(item => item.RowVersion));
        builder.Entity<IpmsSubmission>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IpmsSubmission>().Property(item => item.ActualPerformance).HasMaxLength(1024);
        builder.Entity<IpmsSubmission>().Property(item => item.BaseState).HasMaxLength(20);
        builder.Entity<IpmsSubmission>().ToTable(table => table.HasCheckConstraint(
            "CK_IpmsSubmissions_BaseState",
            "[BaseState] IN ('IN_PROGRESS','SUBMITTED')"));
        builder.Entity<IpmsSubmission>().Property(item => item.AchievementPercent).HasPrecision(18, 4);
        builder.Entity<IpmsSubmission>().HasIndex(item => new { item.IpmsTargetId, item.ReportingPeriodId }).IsUnique().HasFilter("[ReportingPeriodId] IS NOT NULL");
        builder.Entity<IpmsSubmission>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GovernedRecordLifecycleEvent>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<GovernedRecordLifecycleEvent>().HasIndex(item => new { item.MunicipalityId, item.AggregateType, item.AggregateId, item.OccurredAt });
        builder.Entity<GovernedRecordLifecycleEvent>().Property(item => item.AggregateType).HasMaxLength(80);
        builder.Entity<GovernedRecordLifecycleEvent>().Property(item => item.AggregateId).HasMaxLength(128);
        builder.Entity<GovernedRecordLifecycleEvent>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<GovernedRecordLifecycleEvent>().Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Entity<GovernedRecordLifecycleEvent>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<GovernedRecordLifecycleEvent>().HasOne(item => item.ActorUser).WithMany().HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<GovernedRecordLifecycleEvent>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<OpmsSubmission>()
            .HasOne(submission => submission.OpmsTarget)
            .WithMany(target => target.Submissions)
            .HasForeignKey(submission => submission.OpmsTargetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OpmsSubmission>()
            .HasOne(submission => submission.SubmittedByUser)
            .WithMany()
            .HasForeignKey(submission => submission.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.ActualExpenditure)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.Variance)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.SubmitterScore)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.VerifierScore)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.ApproverScore)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.PmsScore)
            .HasPrecision(18, 2);

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.AuditorScore)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .HasOne(submission => submission.IpmsTarget)
            .WithMany(target => target.Submissions)
            .HasForeignKey(submission => submission.IpmsTargetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IpmsSubmission>()
            .HasOne(submission => submission.SubmittedByUser)
            .WithMany()
            .HasForeignKey(submission => submission.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.ActualExpenditure)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.Variance)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.SubmitterScore)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.VerifierScore)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.ApproverScore)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.PmsScore)
            .HasPrecision(18, 2);

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.AuditorScore)
            .HasPrecision(18, 2);

        builder.Entity<EvidenceBlob>().HasIndex(blob => blob.PublicId).IsUnique();
        builder.Entity<EvidenceBlob>().HasIndex(blob => new { blob.MunicipalityId, blob.Sha256 });
        builder.Entity<EvidenceBlob>().Property(blob => blob.Id).HasMaxLength(64);
        builder.Entity<EvidenceBlob>().Property(blob => blob.StorageKey).HasMaxLength(1000);
        builder.Entity<EvidenceBlob>().Property(blob => blob.Sha256).HasMaxLength(64);
        builder.Entity<EvidenceBlob>().Property(blob => blob.ScanStatus).HasMaxLength(40);
        builder.Entity<EvidenceBlob>().Property(blob => blob.ScannerProvider).HasMaxLength(120);
        builder.Entity<EvidenceBlob>().Property(blob => blob.ScannerReference).HasMaxLength(240);
        builder.Entity<EvidenceBlob>().Property(blob => blob.ScanDetail).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<EvidenceBlob>().Property(blob => blob.RowVersion));
        builder.Entity<EvidenceBlob>().HasOne(blob => blob.Municipality).WithMany().HasForeignKey(blob => blob.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EvidenceBlob>().HasQueryFilter(blob => TenantFilterBypass || blob.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PoeFile>()
            .HasOne(file => file.UploadedByUser)
            .WithMany()
            .HasForeignKey(file => file.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeFile>().HasIndex(file => file.PublicId).IsUnique();
        builder.Entity<PoeFile>().HasIndex(file => file.EvidenceBlobId);
        ConfigureRowVersion(builder.Entity<PoeFile>().Property(file => file.RowVersion));
        builder.Entity<PoeFile>().HasOne(file => file.Municipality).WithMany().HasForeignKey(file => file.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeFile>().HasOne(file => file.Blob).WithMany(blob => blob.PoeAssociations).HasForeignKey(file => file.EvidenceBlobId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeFile>().HasQueryFilter(file => TenantFilterBypass || file.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PoeEvidenceAssessment>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<PoeEvidenceAssessment>().HasIndex(x => new { x.PoeFileId, x.AssessedAt });
        builder.Entity<PoeEvidenceAssessment>().Property(x => x.Comment).HasMaxLength(2000);
        builder.Entity<PoeEvidenceAssessment>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<PoeEvidenceAssessment>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceAssessment>().HasOne(x => x.PoeFile).WithMany(x => x.Assessments).HasForeignKey(x => x.PoeFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceAssessment>().HasOne(x => x.AssessedByUser).WithMany().HasForeignKey(x => x.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceAssessment>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PoeEvidenceReplacement>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<PoeEvidenceReplacement>().HasIndex(x => x.SupersededPoeFileId).IsUnique();
        builder.Entity<PoeEvidenceReplacement>().HasIndex(x => x.ReplacementPoeFileId).IsUnique();
        builder.Entity<PoeEvidenceReplacement>().Property(x => x.Reason).HasMaxLength(1000);
        builder.Entity<PoeEvidenceReplacement>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<PoeEvidenceReplacement>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceReplacement>().HasOne(x => x.SupersededPoeFile).WithMany(x => x.ReplacementsAsOld).HasForeignKey(x => x.SupersededPoeFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceReplacement>().HasOne(x => x.ReplacementPoeFile).WithOne(x => x.ReplacementAsNew).HasForeignKey<PoeEvidenceReplacement>(x => x.ReplacementPoeFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceReplacement>().HasOne(x => x.ReplacedByUser).WithMany().HasForeignKey(x => x.ReplacedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeEvidenceReplacement>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PoeLegalHoldEvent>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<PoeLegalHoldEvent>().HasIndex(x => new { x.PoeFileId, x.HoldId, x.Action }).IsUnique();
        builder.Entity<PoeLegalHoldEvent>().Property(x => x.HoldReference).HasMaxLength(200);
        builder.Entity<PoeLegalHoldEvent>().Property(x => x.Reason).HasMaxLength(1000);
        builder.Entity<PoeLegalHoldEvent>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<PoeLegalHoldEvent>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeLegalHoldEvent>().HasOne(x => x.PoeFile).WithMany(x => x.LegalHoldEvents).HasForeignKey(x => x.PoeFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeLegalHoldEvent>().HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeLegalHoldEvent>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PoeDisposalEvent>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<PoeDisposalEvent>().HasIndex(x => new { x.PoeFileId, x.DisposalId, x.Action }).IsUnique();
        builder.Entity<PoeDisposalEvent>().Property(x => x.Reason).HasMaxLength(1000);
        builder.Entity<PoeDisposalEvent>().Property(x => x.ApprovalReference).HasMaxLength(200);
        builder.Entity<PoeDisposalEvent>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<PoeDisposalEvent>().Property(x => x.Detail).HasMaxLength(2000);
        builder.Entity<PoeDisposalEvent>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeDisposalEvent>().HasOne(x => x.PoeFile).WithMany(x => x.DisposalEvents).HasForeignKey(x => x.PoeFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeDisposalEvent>().HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PoeDisposalEvent>().HasQueryFilter(x => TenantFilterBypass || x.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<Notification>()
            .HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Notification>().HasIndex(notification => notification.PublicId).IsUnique();
        builder.Entity<Notification>().HasOne(notification => notification.Municipality).WithMany().HasForeignKey(notification => notification.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Notification>().HasQueryFilter(notification => TenantFilterBypass || notification.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<AuditTrail>()
            .HasOne(audit => audit.ChangedByUser)
            .WithMany()
            .HasForeignKey(audit => audit.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuditTrail>().HasIndex(audit => audit.PublicId).IsUnique();
        builder.Entity<AuditTrail>().HasIndex(audit => new { audit.MunicipalityId, audit.ChangedAt });
        builder.Entity<AuditTrail>().Property(audit => audit.CorrelationId).HasMaxLength(100);
        builder.Entity<AuditTrail>().Property(audit => audit.UserAgent).HasMaxLength(512);
        builder.Entity<AuditTrail>().Property(audit => audit.SessionId).HasMaxLength(64);
        builder.Entity<AuditTrail>().Property(audit => audit.IpAddress).HasMaxLength(64);
        builder.Entity<AuditTrail>().Property(audit => audit.Reason).HasMaxLength(1000);
        builder.Entity<AuditTrail>().Property(audit => audit.EntityName).HasMaxLength(160);
        builder.Entity<AuditTrail>().Property(audit => audit.EntityId).HasMaxLength(160);
        builder.Entity<AuditTrail>().Property(audit => audit.Action).HasMaxLength(160);
        builder.Entity<AuditTrail>().HasOne(audit => audit.Municipality).WithMany().HasForeignKey(audit => audit.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuditTrail>().HasQueryFilter(audit => TenantFilterBypass || audit.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<BusinessEventOutbox>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<BusinessEventOutbox>().HasIndex(item => new { item.ProcessedAt, item.AvailableAt });
        builder.Entity<BusinessEventOutbox>().Property(item => item.EventType).HasMaxLength(160);
        builder.Entity<BusinessEventOutbox>().Property(item => item.AggregateType).HasMaxLength(120);
        builder.Entity<BusinessEventOutbox>().Property(item => item.AggregateId).HasMaxLength(160);
        builder.Entity<BusinessEventOutbox>().Property(item => item.CorrelationId).HasMaxLength(100);
        ConfigureRowVersion(builder.Entity<BusinessEventOutbox>().Property(item => item.RowVersion));
        builder.Entity<BusinessEventOutbox>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BusinessEventOutbox>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<NotificationDeliveryAttempt>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<NotificationDeliveryAttempt>().HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.Entity<NotificationDeliveryAttempt>().Property(item => item.Channel).HasMaxLength(40);
        builder.Entity<NotificationDeliveryAttempt>().Property(item => item.Status).HasMaxLength(40);
        builder.Entity<NotificationDeliveryAttempt>().Property(item => item.IdempotencyKey).HasMaxLength(300);
        builder.Entity<NotificationDeliveryAttempt>().Property(item => item.Provider).HasMaxLength(120);
        builder.Entity<NotificationDeliveryAttempt>().Property(item => item.ProviderReference).HasMaxLength(240);
        builder.Entity<NotificationDeliveryAttempt>().Property(item => item.ResponseDetail).HasMaxLength(2000);
        builder.Entity<NotificationDeliveryAttempt>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationDeliveryAttempt>().HasOne(item => item.BusinessEventOutbox).WithMany(item => item.DeliveryAttempts).HasForeignKey(item => item.BusinessEventOutboxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<NotificationDeliveryAttempt>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<IdempotencyRequest>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<IdempotencyRequest>().HasIndex(item => item.IdentityHash).IsUnique();
        builder.Entity<IdempotencyRequest>().HasIndex(item => item.ExpiresAt);
        builder.Entity<IdempotencyRequest>().Property(item => item.ScopeKey).HasMaxLength(64);
        builder.Entity<IdempotencyRequest>().Property(item => item.IdentityHash).HasMaxLength(64);
        builder.Entity<IdempotencyRequest>().Property(item => item.UserId).HasMaxLength(450);
        builder.Entity<IdempotencyRequest>().Property(item => item.Method).HasMaxLength(10);
        builder.Entity<IdempotencyRequest>().Property(item => item.Route).HasMaxLength(600);
        builder.Entity<IdempotencyRequest>().Property(item => item.IdempotencyKey).HasMaxLength(128);
        builder.Entity<IdempotencyRequest>().Property(item => item.RequestHash).HasMaxLength(64);
        builder.Entity<IdempotencyRequest>().Property(item => item.State).HasMaxLength(20);
        builder.Entity<IdempotencyRequest>().Property(item => item.ResponseContentType).HasMaxLength(200);
        builder.Entity<IdempotencyRequest>().Property(item => item.ResponseLocation).HasMaxLength(1000);
        builder.Entity<IdempotencyRequest>().Property(item => item.ResponseETag).HasMaxLength(200);
        builder.Entity<IdempotencyRequest>().Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Entity<IdempotencyRequest>().ToTable(table => table.HasCheckConstraint("CK_IdempotencyRequests_State", "[State] IN ('InProgress','Completed','Failed')"));
        ConfigureRowVersion(builder.Entity<IdempotencyRequest>().Property(item => item.RowVersion));
        builder.Entity<IdempotencyRequest>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdempotencyRequest>().HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdempotencyRequest>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<IdpPlan>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<IdpPlan>().HasIndex(item => new { item.MunicipalityId, item.PlanFamilyId });
        builder.Entity<IdpPlan>().HasIndex(item => new { item.MunicipalityId, item.PlanCode }).IsUnique().HasFilter("[MunicipalityId] IS NOT NULL");
        builder.Entity<IdpPlan>().Property(item => item.PublicationReference).HasMaxLength(240);
        builder.Entity<IdpPlan>().ToTable(table => table.HasCheckConstraint("CK_IdpPlans_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
        ConfigureRowVersion(builder.Entity<IdpPlan>().Property(item => item.RowVersion));
        builder.Entity<IdpPlan>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpPlan>().HasOne(item => item.PredecessorPlan).WithMany(item => item.SuccessorPlans).HasForeignKey(item => item.PredecessorPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpPlan>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpPlanVersion>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<IdpPlanVersion>().Property(item => item.PublicationReference).HasMaxLength(240);
        builder.Entity<IdpPlanVersion>().ToTable(table => table.HasCheckConstraint("CK_IdpPlanVersions_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
        ConfigureRowVersion(builder.Entity<IdpPlanVersion>().Property(item => item.RowVersion));
        builder.Entity<IdpPlanVersion>().HasOne(item => item.PredecessorVersion).WithMany(item => item.SuccessorVersions).HasForeignKey(item => item.PredecessorVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpPlanVersion>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpChangeLog>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlanVersion.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpStrategicOutcome>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpStrategicObjective>().HasQueryFilter(item => TenantFilterBypass || item.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpDevelopmentPriority>().HasQueryFilter(item => TenantFilterBypass || item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpProgramme>().HasQueryFilter(item => TenantFilterBypass || item.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpProject>().HasQueryFilter(item => TenantFilterBypass || item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpKpi>().HasQueryFilter(item => TenantFilterBypass || item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpImportBatch>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpImportRow>().HasQueryFilter(item => TenantFilterBypass || item.IdpImportBatch.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpAnnualTarget>().HasQueryFilter(item => TenantFilterBypass || item.IdpKpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpAlignmentLink>().HasQueryFilter(item => TenantFilterBypass || item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpCommunitySession>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpCommunityNeed>().HasQueryFilter(item => TenantFilterBypass || item.IdpCommunitySession.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpWardInput>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpStakeholderEngagement>().HasQueryFilter(item => TenantFilterBypass || item.IdpCommunitySession.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpRiskLink>().HasQueryFilter(item => TenantFilterBypass ||
            (item.IdpStrategicObjective != null && item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel) ||
            (item.IdpProject != null && item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel) ||
            (item.IdpKpi != null && item.IdpKpi.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel));
        builder.Entity<IdpBudgetSnapshot>().HasQueryFilter(item => TenantFilterBypass ||
            (item.IdpStrategicObjective != null && item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel) ||
            (item.IdpProject != null && item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel));
        builder.Entity<IdpDocument>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpCollaborationComment>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<IdpTaskAssignment>().HasQueryFilter(item => TenantFilterBypass || item.IdpPlan.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<DueDateExtension>()
            .HasOne(item => item.ApprovedByUser)
            .WithMany()
            .HasForeignKey(item => item.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewComment>()
            .HasOne(comment => comment.CommentedByUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommentedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AuditFinding>()
            .HasOne(finding => finding.CreatedByUser)
            .WithMany()
            .HasForeignKey(finding => finding.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionScore>()
            .HasOne(score => score.ScoredByUser)
            .WithMany()
            .HasForeignKey(score => score.ScoredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Period>()
            .HasIndex(p => p.Code)
            .IsUnique();

        builder.Entity<StrategicGoal>()
            .HasIndex(sg => sg.Code)
            .IsUnique();

        builder.Entity<StrategicObjective>()
            .HasIndex(so => so.Code)
            .IsUnique();

        builder.Entity<StrategicObjective>()
            .HasOne(so => so.StrategicGoal)
            .WithMany()
            .HasForeignKey(so => so.StrategicGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BudgetSource>()
            .HasIndex(bs => bs.Code)
            .IsUnique();

        builder.Entity<BudgetType>()
            .HasIndex(bt => bt.Code)
            .IsUnique();

        builder.Entity<UnitOfMeasure>()
            .HasIndex(uom => uom.Code)
            .IsUnique();

        builder.Entity<Ward>().HasIndex(w => w.PublicId).IsUnique();
        builder.Entity<Ward>().HasIndex(w => new { w.MunicipalityId, w.Code }).IsUnique();
        builder.Entity<Ward>().HasOne(w => w.Municipality).WithMany().HasForeignKey(w => w.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<Ward>().Property(w => w.RowVersion));

        builder.Entity<VoteNumber>().HasIndex(vn => vn.PublicId).IsUnique();
        builder.Entity<VoteNumber>().HasIndex(vn => new { vn.MunicipalityId, vn.Code }).IsUnique();
        builder.Entity<VoteNumber>().HasOne(vn => vn.Municipality).WithMany().HasForeignKey(vn => vn.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<VoteNumber>().Property(vn => vn.RowVersion));

        builder.Entity<VoteNumber>()
            .HasOne(vn => vn.Department)
            .WithMany()
            .HasForeignKey(vn => vn.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VoteNumber>()
            .Property(vn => vn.Amount)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.Period)
            .WithMany()
            .HasForeignKey(ot => ot.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.StrategicGoal)
            .WithMany()
            .HasForeignKey(ot => ot.StrategicGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.StrategicObjective)
            .WithMany()
            .HasForeignKey(ot => ot.StrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.BudgetSource)
            .WithMany()
            .HasForeignKey(ot => ot.BudgetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.BudgetType)
            .WithMany()
            .HasForeignKey(ot => ot.BudgetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(ot => ot.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.Period)
            .WithMany()
            .HasForeignKey(it => it.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.StrategicGoal)
            .WithMany()
            .HasForeignKey(it => it.StrategicGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.StrategicObjective)
            .WithMany()
            .HasForeignKey(it => it.StrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.BudgetSource)
            .WithMany()
            .HasForeignKey(it => it.BudgetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.BudgetType)
            .WithMany()
            .HasForeignKey(it => it.BudgetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(it => it.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionScore>()
            .Property(score => score.Score)
            .HasPrecision(18, 2);

        builder.Entity<IdpPlan>()
            .HasOne(plan => plan.CreatedByUser)
            .WithMany()
            .HasForeignKey(plan => plan.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpPlan>()
            .HasOne(plan => plan.ApprovedByUser)
            .WithMany()
            .HasForeignKey(plan => plan.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpPlanVersion>()
            .HasIndex(version => new { version.IdpPlanId, version.VersionNumber })
            .IsUnique();

        builder.Entity<IdpPlanVersion>()
            .HasOne(version => version.IdpPlan)
            .WithMany(plan => plan.Versions)
            .HasForeignKey(version => version.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpPlanVersion>()
            .HasOne(version => version.CreatedByUser)
            .WithMany()
            .HasForeignKey(version => version.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpChangeLog>()
            .HasOne(changeLog => changeLog.IdpPlanVersion)
            .WithMany(version => version.ChangeLogs)
            .HasForeignKey(changeLog => changeLog.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpChangeLog>()
            .HasOne(changeLog => changeLog.ChangedByUser)
            .WithMany()
            .HasForeignKey(changeLog => changeLog.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicOutcome>()
            .HasIndex(outcome => new { outcome.IdpPlanId, outcome.Code })
            .IsUnique();
        builder.Entity<IdpStrategicOutcome>().HasIndex(outcome => outcome.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpStrategicOutcome>().Property(outcome => outcome.RowVersion));

        builder.Entity<IdpStrategicOutcome>()
            .HasOne(outcome => outcome.IdpPlan)
            .WithMany(plan => plan.StrategicOutcomes)
            .HasForeignKey(outcome => outcome.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpStrategicObjective>()
            .HasIndex(objective => new { objective.IdpStrategicOutcomeId, objective.Code })
            .IsUnique();
        builder.Entity<IdpStrategicObjective>().HasIndex(objective => objective.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpStrategicObjective>().Property(objective => objective.RowVersion));

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.IdpStrategicOutcome)
            .WithMany(outcome => outcome.StrategicObjectives)
            .HasForeignKey(objective => objective.IdpStrategicOutcomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.ResponsibleDepartment)
            .WithMany()
            .HasForeignKey(objective => objective.ResponsibleDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.StrategicOwnerUser)
            .WithMany()
            .HasForeignKey(objective => objective.StrategicOwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicObjective>()
            .Property(objective => objective.BaselineValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpStrategicObjective>()
            .Property(objective => objective.TargetValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpStrategicObjective>()
            .Property(objective => objective.BudgetAllocation)
            .HasPrecision(18, 2);

        builder.Entity<IdpDevelopmentPriority>()
            .HasIndex(priority => new { priority.IdpStrategicObjectiveId, priority.PriorityCode })
            .IsUnique();
        builder.Entity<IdpDevelopmentPriority>().HasIndex(priority => priority.PublicId).IsUnique();
        builder.Entity<IdpDevelopmentPriority>().Property(priority => priority.PriorityCode).HasMaxLength(80);
        ConfigureRowVersion(builder.Entity<IdpDevelopmentPriority>().Property(priority => priority.RowVersion));

        builder.Entity<IdpDevelopmentPriority>()
            .HasOne(priority => priority.IdpStrategicObjective)
            .WithMany(objective => objective.DevelopmentPriorities)
            .HasForeignKey(priority => priority.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpProgramme>()
            .HasIndex(programme => new { programme.IdpDevelopmentPriorityId, programme.ProgrammeCode })
            .IsUnique();
        builder.Entity<IdpProgramme>().HasIndex(programme => programme.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpProgramme>().Property(programme => programme.RowVersion));

        builder.Entity<IdpProgramme>()
            .HasOne(programme => programme.IdpDevelopmentPriority)
            .WithMany(priority => priority.Programmes)
            .HasForeignKey(programme => programme.IdpDevelopmentPriorityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpProgramme>()
            .HasOne(programme => programme.ResponsibleDepartment)
            .WithMany()
            .HasForeignKey(programme => programme.ResponsibleDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpProgramme>()
            .Property(programme => programme.PlannedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpProgramme>()
            .Property(programme => programme.ApprovedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpProgramme>()
            .Property(programme => programme.ActualExpenditure)
            .HasPrecision(18, 2);

        builder.Entity<IdpProject>()
            .HasIndex(project => new { project.IdpProgrammeId, project.ProjectCode })
            .IsUnique();
        builder.Entity<IdpProject>().HasIndex(project => project.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpProject>().Property(project => project.RowVersion));

        builder.Entity<IdpProject>()
            .HasOne(project => project.IdpProgramme)
            .WithMany(programme => programme.Projects)
            .HasForeignKey(project => project.IdpProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpProject>()
            .HasOne(project => project.Department)
            .WithMany()
            .HasForeignKey(project => project.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpProject>()
            .Property(project => project.Budget)
            .HasPrecision(18, 2);

        builder.Entity<IdpKpi>()
            .HasIndex(kpi => new { kpi.IdpProjectId, kpi.KpiCode })
            .IsUnique();

        builder.Entity<IdpKpi>().HasIndex(kpi => kpi.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpKpi>().Property(kpi => kpi.RowVersion));

        builder.Entity<IdpKpi>()
            .HasOne(kpi => kpi.IdpProject)
            .WithMany(project => project.Kpis)
            .HasForeignKey(kpi => kpi.IdpProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpKpi>()
            .HasOne(kpi => kpi.ResponsibleDepartment)
            .WithMany()
            .HasForeignKey(kpi => kpi.ResponsibleDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpKpi>()
            .Property(kpi => kpi.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<IdpKpi>()
            .Property(kpi => kpi.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IdpKpi>()
            .Property(kpi => kpi.FiveYearTarget)
            .HasPrecision(18, 2);

        builder.Entity<IdpImportBatch>().HasIndex(batch => batch.PublicId).IsUnique();
        builder.Entity<IdpImportBatch>().HasIndex(batch => new { batch.MunicipalityId, batch.ClientRequestId }).IsUnique();
        builder.Entity<IdpImportBatch>().HasIndex(batch => new { batch.IdpPlanId, batch.CreatedAt });
        builder.Entity<IdpImportBatch>().Property(batch => batch.ImportType).HasMaxLength(40);
        builder.Entity<IdpImportBatch>().Property(batch => batch.SourceFileName).HasMaxLength(260);
        builder.Entity<IdpImportBatch>().Property(batch => batch.SourceSha256).HasMaxLength(64);
        builder.Entity<IdpImportBatch>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_IdpImportBatches_RowCounts", "[TotalRows] > 0 AND [TotalRows] = [NewRows] + [UnchangedRows] + [ChangedRows] + [InvalidRows]");
            table.HasCheckConstraint("CK_IdpImportBatches_CommitMetadata", "[Status] <> 1 OR ([CommittedAt] IS NOT NULL AND [CommittedByUserId] IS NOT NULL)");
        });
        ConfigureRowVersion(builder.Entity<IdpImportBatch>().Property(batch => batch.RowVersion));
        builder.Entity<IdpImportBatch>().HasOne(batch => batch.Municipality).WithMany().HasForeignKey(batch => batch.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpImportBatch>().HasOne(batch => batch.IdpPlan).WithMany(plan => plan.ImportBatches).HasForeignKey(batch => batch.IdpPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpImportBatch>().HasOne(batch => batch.CreatedByUser).WithMany().HasForeignKey(batch => batch.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpImportBatch>().HasOne(batch => batch.CommittedByUser).WithMany().HasForeignKey(batch => batch.CommittedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpImportRow>().HasIndex(row => row.PublicId).IsUnique();
        builder.Entity<IdpImportRow>().HasIndex(row => new { row.IdpImportBatchId, row.SourceRowNumber }).IsUnique();
        builder.Entity<IdpImportRow>().Property(row => row.Reference).HasMaxLength(240);
        builder.Entity<IdpImportRow>().Property(row => row.ErrorCode).HasMaxLength(80);
        builder.Entity<IdpImportRow>().Property(row => row.ErrorField).HasMaxLength(120);
        builder.Entity<IdpImportRow>().Property(row => row.SuppliedValue).HasMaxLength(1000);
        builder.Entity<IdpImportRow>().Property(row => row.ErrorMessage).HasMaxLength(2000);
        builder.Entity<IdpImportRow>().HasOne(row => row.IdpImportBatch).WithMany(batch => batch.Rows).HasForeignKey(row => row.IdpImportBatchId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpAnnualTarget>()
            .HasIndex(annualTarget => new { annualTarget.IdpKpiId, annualTarget.FinancialYear })
            .IsUnique();

        builder.Entity<IdpAnnualTarget>()
            .HasOne(annualTarget => annualTarget.IdpKpi)
            .WithMany(kpi => kpi.AnnualTargets)
            .HasForeignKey(annualTarget => annualTarget.IdpKpiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpAnnualTarget>()
            .Property(annualTarget => annualTarget.TargetValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpAnnualTarget>()
            .Property(annualTarget => annualTarget.ActualValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpAlignmentLink>()
            .HasOne(link => link.IdpStrategicObjective)
            .WithMany(objective => objective.AlignmentLinks)
            .HasForeignKey(link => link.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpCommunitySession>()
            .HasOne(session => session.IdpPlan)
            .WithMany(plan => plan.CommunitySessions)
            .HasForeignKey(session => session.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpCommunitySession>()
            .HasOne(session => session.Ward)
            .WithMany()
            .HasForeignKey(session => session.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCommunityNeed>()
            .HasOne(need => need.IdpCommunitySession)
            .WithMany(session => session.CommunityNeeds)
            .HasForeignKey(need => need.IdpCommunitySessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpWardInput>()
            .HasIndex(wardInput => new { wardInput.IdpPlanId, wardInput.WardId })
            .IsUnique();

        builder.Entity<IdpWardInput>()
            .HasOne(wardInput => wardInput.IdpPlan)
            .WithMany()
            .HasForeignKey(wardInput => wardInput.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpWardInput>()
            .HasOne(wardInput => wardInput.Ward)
            .WithMany()
            .HasForeignKey(wardInput => wardInput.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStakeholderEngagement>()
            .HasOne(engagement => engagement.IdpCommunitySession)
            .WithMany(session => session.StakeholderEngagements)
            .HasForeignKey(engagement => engagement.IdpCommunitySessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpStrategicObjective)
            .WithMany(objective => objective.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpProject)
            .WithMany(project => project.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpKpi)
            .WithMany(kpi => kpi.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpKpiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpBudgetSnapshot>()
            .HasOne(snapshot => snapshot.IdpStrategicObjective)
            .WithMany(objective => objective.BudgetSnapshots)
            .HasForeignKey(snapshot => snapshot.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpBudgetSnapshot>()
            .HasOne(snapshot => snapshot.IdpProject)
            .WithMany(project => project.BudgetSnapshots)
            .HasForeignKey(snapshot => snapshot.IdpProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpBudgetSnapshot>()
            .Property(snapshot => snapshot.PlannedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpBudgetSnapshot>()
            .Property(snapshot => snapshot.ApprovedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpBudgetSnapshot>()
            .Property(snapshot => snapshot.ActualExpenditure)
            .HasPrecision(18, 2);

        builder.Entity<IdpDocument>()
            .HasOne(document => document.IdpPlan)
            .WithMany(plan => plan.Documents)
            .HasForeignKey(document => document.IdpPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpDocument>()
            .HasOne(document => document.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(document => document.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpDocument>()
            .HasOne(document => document.UploadedByUser)
            .WithMany()
            .HasForeignKey(document => document.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpDocument>().HasIndex(document => document.PublicId).IsUnique();
        builder.Entity<IdpDocument>().HasIndex(document => new { document.IdpPlanId, document.EvidenceBlobId });
        builder.Entity<IdpDocument>().HasOne(document => document.Blob).WithMany(blob => blob.IdpDocumentAssociations).HasForeignKey(document => document.EvidenceBlobId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<IdpDocument>().Property(document => document.RowVersion));

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.IdpPlan)
            .WithMany()
            .HasForeignKey(comment => comment.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(comment => comment.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.CommentedByUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommentedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.IdpPlan)
            .WithMany()
            .HasForeignKey(task => task.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(task => task.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.AssignedToUser)
            .WithMany()
            .HasForeignKey(task => task.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.AssignedByUser)
            .WithMany()
            .HasForeignKey(task => task.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TechnicalIndicatorDescription>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<TechnicalIndicatorDescription>().HasIndex(item => new { item.OpmsTargetId, item.VersionNumber }).IsUnique();
        builder.Entity<TechnicalIndicatorDescription>().HasIndex(item => item.OpmsTargetId).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.IndicatorDefinition).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.Purpose).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.DataSource).HasMaxLength(2000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.CollectionMethod).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.CalculationMethod).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.NumeratorDescription).HasMaxLength(2000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.DenominatorDescription).HasMaxLength(2000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.Limitations).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.Assumptions).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.VerificationMethod).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().Property(item => item.Notes).HasMaxLength(4000);
        builder.Entity<TechnicalIndicatorDescription>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_TechnicalIndicatorDescriptions_Version", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_TechnicalIndicatorDescriptions_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint("CK_TechnicalIndicatorDescriptions_Current", "[IsCurrent] = 0 OR [EffectiveTo] IS NULL");
        });
        ConfigureRowVersion(builder.Entity<TechnicalIndicatorDescription>().Property(item => item.RowVersion));
        builder.Entity<TechnicalIndicatorDescription>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TechnicalIndicatorDescription>().HasOne(item => item.OpmsTarget).WithMany(item => item.TechnicalIndicatorDescriptions).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TechnicalIndicatorDescription>().HasOne(item => item.PreviousVersion).WithMany(item => item.SuccessorVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TechnicalIndicatorDescription>().HasOne(item => item.ResponsibleEmployee).WithMany().HasForeignKey(item => item.ResponsibleEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TechnicalIndicatorDescription>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TechnicalIndicatorDescription>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<TidSourceDocument>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<TidSourceDocument>().HasIndex(item => new { item.TechnicalIndicatorDescriptionId, item.EvidenceBlobId }).IsUnique();
        builder.Entity<TidSourceDocument>().Property(item => item.Title).HasMaxLength(240);
        builder.Entity<TidSourceDocument>().Property(item => item.FileName).HasMaxLength(260);
        builder.Entity<TidSourceDocument>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TidSourceDocument>().HasOne(item => item.TechnicalIndicatorDescription).WithMany(item => item.SourceDocuments).HasForeignKey(item => item.TechnicalIndicatorDescriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TidSourceDocument>().HasOne(item => item.Blob).WithMany(item => item.TidSourceDocumentAssociations).HasForeignKey(item => item.EvidenceBlobId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TidSourceDocument>().HasOne(item => item.UploadedByUser).WithMany().HasForeignKey(item => item.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TidSourceDocument>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<StrategicDocumentType>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicDocumentType>().HasIndex(item => new { item.MunicipalityId, item.Code }).IsUnique();
        builder.Entity<StrategicDocumentType>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<StrategicDocumentType>().Property(item => item.Name).HasMaxLength(160);
        builder.Entity<StrategicDocumentType>().Property(item => item.Description).HasMaxLength(1000);
        builder.Entity<StrategicDocumentType>().ToTable(table => table.HasCheckConstraint("CK_StrategicDocumentTypes_DisplayOrder", "[DisplayOrder] >= 0"));
        ConfigureRowVersion(builder.Entity<StrategicDocumentType>().Property(item => item.RowVersion));
        builder.Entity<StrategicDocumentType>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocumentType>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocumentType>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<StrategicDocument>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicDocument>().HasIndex(item => new { item.MunicipalityId, item.DocumentFamilyId, item.VersionNumber }).IsUnique();
        builder.Entity<StrategicDocument>().HasIndex(item => item.DocumentFamilyId).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<StrategicDocument>().HasIndex(item => new { item.MunicipalityFinancialYearId, item.IsActive, item.IsApproved, item.IsPublished });
        builder.Entity<StrategicDocument>().Property(item => item.SdbipLayer).HasMaxLength(120);
        builder.Entity<StrategicDocument>().Property(item => item.Title).HasMaxLength(240);
        builder.Entity<StrategicDocument>().Property(item => item.Description).HasMaxLength(4000);
        builder.Entity<StrategicDocument>().Property(item => item.FileName).HasMaxLength(260);
        builder.Entity<StrategicDocument>().Property(item => item.ExternalUrl).HasMaxLength(2048);
        builder.Entity<StrategicDocument>().Property(item => item.ApprovalReference).HasMaxLength(240);
        builder.Entity<StrategicDocument>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_StrategicDocuments_Version", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_StrategicDocuments_DisplayOrder", "[DisplayOrder] >= 0");
            table.HasCheckConstraint("CK_StrategicDocuments_Content", "([EvidenceBlobId] IS NOT NULL AND [ExternalUrl] IS NULL) OR ([EvidenceBlobId] IS NULL AND [ExternalUrl] IS NOT NULL)");
            table.HasCheckConstraint("CK_StrategicDocuments_Publication", "[IsPublished] = 0 OR [IsApproved] = 1");
        });
        ConfigureRowVersion(builder.Entity<StrategicDocument>().Property(item => item.RowVersion));
        builder.Entity<StrategicDocument>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.DocumentType).WithMany(item => item.Documents).HasForeignKey(item => item.StrategicDocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.PreviousVersion).WithMany(item => item.SuccessorVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.Blob).WithMany(item => item.StrategicDocumentAssociations).HasForeignKey(item => item.EvidenceBlobId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.ApprovedByUser).WithMany().HasForeignKey(item => item.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasOne(item => item.PublishedByUser).WithMany().HasForeignKey(item => item.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocument>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<StrategicDocumentEvent>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicDocumentEvent>().HasIndex(item => new { item.StrategicDocumentId, item.OccurredAt });
        builder.Entity<StrategicDocumentEvent>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<StrategicDocumentEvent>().Property(item => item.SnapshotJson).HasMaxLength(8000);
        builder.Entity<StrategicDocumentEvent>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocumentEvent>().HasOne(item => item.StrategicDocument).WithMany(item => item.Events).HasForeignKey(item => item.StrategicDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocumentEvent>().HasOne(item => item.ActorUser).WithMany().HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicDocumentEvent>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        ConfigureC88Model(builder);
    }

    private void ConfigureC88Model(ModelBuilder builder)
    {
        builder.Entity<C88CatalogueVersion>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88CatalogueVersion>().HasIndex(item => new { item.MunicipalityId, item.Code }).IsUnique();
        builder.Entity<C88CatalogueVersion>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<C88CatalogueVersion>().Property(item => item.Name).HasMaxLength(240);
        builder.Entity<C88CatalogueVersion>().ToTable(table => table.HasCheckConstraint("CK_C88CatalogueVersions_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        ConfigureRowVersion(builder.Entity<C88CatalogueVersion>().Property(item => item.RowVersion));
        builder.Entity<C88CatalogueVersion>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88CatalogueVersion>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88CatalogueVersion>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88MunicipalityConfiguration>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88MunicipalityConfiguration>().HasIndex(item => item.MunicipalityFinancialYearId).IsUnique();
        builder.Entity<C88MunicipalityConfiguration>().ToTable(table => table.HasCheckConstraint("CK_C88MunicipalityConfigurations_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        ConfigureRowVersion(builder.Entity<C88MunicipalityConfiguration>().Property(item => item.RowVersion));
        builder.Entity<C88MunicipalityConfiguration>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88MunicipalityConfiguration>().HasOne(item => item.MunicipalityFinancialYear).WithMany().HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88MunicipalityConfiguration>().HasOne(item => item.CatalogueVersion).WithMany().HasForeignKey(item => item.C88CatalogueVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88MunicipalityConfiguration>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88MunicipalityConfiguration>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88CatalogueItem>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88CatalogueItem>().HasIndex(item => new { item.C88CatalogueVersionId, item.Kind, item.Code }).IsUnique();
        builder.Entity<C88CatalogueItem>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<C88CatalogueItem>().Property(item => item.Name).HasMaxLength(240);
        builder.Entity<C88CatalogueItem>().Property(item => item.Description).HasMaxLength(4000);
        builder.Entity<C88CatalogueItem>().ToTable(table => table.HasCheckConstraint("CK_C88CatalogueItems_DisplayOrder", "[DisplayOrder] >= 0"));
        ConfigureRowVersion(builder.Entity<C88CatalogueItem>().Property(item => item.RowVersion));
        builder.Entity<C88CatalogueItem>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88CatalogueItem>().HasOne(item => item.CatalogueVersion).WithMany(item => item.Items).HasForeignKey(item => item.C88CatalogueVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88CatalogueItem>().HasOne(item => item.ParentItem).WithMany(item => item.ChildItems).HasForeignKey(item => item.ParentItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88CatalogueItem>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88Indicator>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88Indicator>().HasIndex(item => new { item.C88CatalogueVersionId, item.Code }).IsUnique();
        builder.Entity<C88Indicator>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<C88Indicator>().Property(item => item.Name).HasMaxLength(300);
        builder.Entity<C88Indicator>().Property(item => item.Definition).HasMaxLength(8000);
        builder.Entity<C88Indicator>().Property(item => item.OfficialTechnicalIndicatorDescription).HasMaxLength(16000);
        builder.Entity<C88Indicator>().Property(item => item.OfficialFormulaText).HasMaxLength(4000);
        ConfigureRowVersion(builder.Entity<C88Indicator>().Property(item => item.RowVersion));
        builder.Entity<C88Indicator>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Indicator>().HasOne(item => item.CatalogueVersion).WithMany(item => item.Indicators).HasForeignKey(item => item.C88CatalogueVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Indicator>().HasOne(item => item.SectorItem).WithMany().HasForeignKey(item => item.SectorItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Indicator>().HasOne(item => item.OutcomeItem).WithMany().HasForeignKey(item => item.OutcomeItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Indicator>().HasOne(item => item.IndicatorTypeItem).WithMany().HasForeignKey(item => item.IndicatorTypeItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Indicator>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88DataElement>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88DataElement>().HasIndex(item => new { item.C88IndicatorId, item.Code }).IsUnique();
        builder.Entity<C88DataElement>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<C88DataElement>().Property(item => item.Name).HasMaxLength(240);
        builder.Entity<C88DataElement>().Property(item => item.Description).HasMaxLength(4000);
        builder.Entity<C88DataElement>().ToTable(table => table.HasCheckConstraint("CK_C88DataElements_Sequence", "[Sequence] > 0"));
        ConfigureRowVersion(builder.Entity<C88DataElement>().Property(item => item.RowVersion));
        builder.Entity<C88DataElement>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88DataElement>().HasOne(item => item.Indicator).WithMany(item => item.DataElements).HasForeignKey(item => item.C88IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88DataElement>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88IndicatorApplicability>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88IndicatorApplicability>().HasIndex(item => new { item.C88IndicatorId, item.MunicipalCategoryItemId, item.ReadinessTierItemId }).IsUnique();
        builder.Entity<C88IndicatorApplicability>().Property(item => item.Notes).HasMaxLength(2000);
        builder.Entity<C88IndicatorApplicability>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorApplicability>().HasOne(item => item.Indicator).WithMany(item => item.Applicability).HasForeignKey(item => item.C88IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorApplicability>().HasOne(item => item.MunicipalCategoryItem).WithMany().HasForeignKey(item => item.MunicipalCategoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorApplicability>().HasOne(item => item.ReadinessTierItem).WithMany().HasForeignKey(item => item.ReadinessTierItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorApplicability>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88ComplianceQuestion>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88ComplianceQuestion>().HasIndex(item => new { item.C88CatalogueVersionId, item.Code }).IsUnique();
        builder.Entity<C88ComplianceQuestion>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<C88ComplianceQuestion>().Property(item => item.Prompt).HasMaxLength(4000);
        builder.Entity<C88ComplianceQuestion>().ToTable(table => table.HasCheckConstraint("CK_C88ComplianceQuestions_Sequence", "[Sequence] > 0"));
        ConfigureRowVersion(builder.Entity<C88ComplianceQuestion>().Property(item => item.RowVersion));
        builder.Entity<C88ComplianceQuestion>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceQuestion>().HasOne(item => item.CatalogueVersion).WithMany(item => item.ComplianceQuestions).HasForeignKey(item => item.C88CatalogueVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceQuestion>().HasOne(item => item.ReportTypeItem).WithMany().HasForeignKey(item => item.ReportTypeItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceQuestion>().HasOne(item => item.ResponseTypeItem).WithMany().HasForeignKey(item => item.ResponseTypeItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceQuestion>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88IndicatorPlan>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88IndicatorPlan>().HasIndex(item => new { item.C88MunicipalityConfigurationId, item.C88IndicatorId }).IsUnique();
        builder.Entity<C88IndicatorPlan>().Property(item => item.BaselineValue).HasMaxLength(1024);
        builder.Entity<C88IndicatorPlan>().Property(item => item.MediumTermTarget).HasMaxLength(1024);
        builder.Entity<C88IndicatorPlan>().Property(item => item.AnnualTarget).HasMaxLength(1024);
        builder.Entity<C88IndicatorPlan>().Property(item => item.MissingDataExplanation).HasMaxLength(4000);
        ConfigureRowVersion(builder.Entity<C88IndicatorPlan>().Property(item => item.RowVersion));
        builder.Entity<C88IndicatorPlan>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorPlan>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.C88MunicipalityConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorPlan>().HasOne(item => item.Indicator).WithMany().HasForeignKey(item => item.C88IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorPlan>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorPlan>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88ReportingCalendar>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88ReportingCalendar>().HasIndex(item => new { item.C88MunicipalityConfigurationId, item.Code }).IsUnique();
        builder.Entity<C88ReportingCalendar>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<C88ReportingCalendar>().Property(item => item.Name).HasMaxLength(240);
        builder.Entity<C88ReportingCalendar>().ToTable(table => table.HasCheckConstraint("CK_C88ReportingCalendars_Dates", "[ClosesAt] >= [OpensAt] AND [DueAt] >= [OpensAt]"));
        ConfigureRowVersion(builder.Entity<C88ReportingCalendar>().Property(item => item.RowVersion));
        builder.Entity<C88ReportingCalendar>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ReportingCalendar>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.C88MunicipalityConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ReportingCalendar>().HasOne(item => item.ReportTypeItem).WithMany().HasForeignKey(item => item.ReportTypeItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ReportingCalendar>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ReportingCalendar>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88IndicatorReport>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88IndicatorReport>().HasIndex(item => new { item.MunicipalityId, item.ReportFamilyId, item.VersionNumber }).IsUnique();
        builder.Entity<C88IndicatorReport>().HasIndex(item => item.ReportFamilyId).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<C88IndicatorReport>().HasIndex(item => new { item.C88ReportingCalendarId, item.C88IndicatorId, item.IsCurrent }).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<C88IndicatorReport>().Property(item => item.CalculatedValue).HasMaxLength(1024);
        builder.Entity<C88IndicatorReport>().Property(item => item.MissingDataExplanation).HasMaxLength(4000);
        builder.Entity<C88IndicatorReport>().ToTable(table => table.HasCheckConstraint("CK_C88IndicatorReports_Version", "[VersionNumber] > 0"));
        ConfigureRowVersion(builder.Entity<C88IndicatorReport>().Property(item => item.RowVersion));
        builder.Entity<C88IndicatorReport>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.C88MunicipalityConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.Calendar).WithMany().HasForeignKey(item => item.C88ReportingCalendarId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.Indicator).WithMany().HasForeignKey(item => item.C88IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.WorkflowDefinition).WithMany().HasForeignKey(item => item.C88WorkflowDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.PreviousVersion).WithMany(item => item.SuccessorVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasOne(item => item.FinalSubmittedByUser).WithMany().HasForeignKey(item => item.FinalSubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88IndicatorReport>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88DataElementValue>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88DataElementValue>().HasIndex(item => new { item.C88IndicatorReportId, item.C88DataElementId }).IsUnique();
        builder.Entity<C88DataElementValue>().Property(item => item.Value).HasMaxLength(1024);
        builder.Entity<C88DataElementValue>().Property(item => item.MissingDataExplanation).HasMaxLength(4000);
        builder.Entity<C88DataElementValue>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88DataElementValue>().HasOne(item => item.IndicatorReport).WithMany(item => item.DataElementValues).HasForeignKey(item => item.C88IndicatorReportId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88DataElementValue>().HasOne(item => item.DataElement).WithMany().HasForeignKey(item => item.C88DataElementId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88DataElementValue>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88ComplianceResponse>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88ComplianceResponse>().HasIndex(item => new { item.C88IndicatorReportId, item.C88ComplianceQuestionId }).IsUnique();
        builder.Entity<C88ComplianceResponse>().Property(item => item.Response).HasMaxLength(4000);
        builder.Entity<C88ComplianceResponse>().Property(item => item.Comment).HasMaxLength(4000);
        builder.Entity<C88ComplianceResponse>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceResponse>().HasOne(item => item.IndicatorReport).WithMany(item => item.ComplianceResponses).HasForeignKey(item => item.C88IndicatorReportId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceResponse>().HasOne(item => item.ComplianceQuestion).WithMany().HasForeignKey(item => item.C88ComplianceQuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88ComplianceResponse>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88Assignment>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88Assignment>().HasIndex(item => new { item.C88MunicipalityConfigurationId, item.C88IndicatorId, item.MunicipalEmployeeId, item.Role, item.EffectiveFrom }).IsUnique();
        builder.Entity<C88Assignment>().HasIndex(item => new { item.C88MunicipalityConfigurationId, item.C88IndicatorId }).IsUnique().HasFilter("[Role] = 1 AND [IsActive] = 1 AND [EffectiveTo] IS NULL");
        builder.Entity<C88Assignment>().ToTable(table => table.HasCheckConstraint("CK_C88Assignments_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
        ConfigureRowVersion(builder.Entity<C88Assignment>().Property(item => item.RowVersion));
        builder.Entity<C88Assignment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Assignment>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.C88MunicipalityConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Assignment>().HasOne(item => item.Indicator).WithMany().HasForeignKey(item => item.C88IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Assignment>().HasOne(item => item.MunicipalEmployee).WithMany().HasForeignKey(item => item.MunicipalEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88Assignment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88WorkflowDefinition>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88WorkflowDefinition>().HasIndex(item => new { item.C88MunicipalityConfigurationId, item.VersionNumber }).IsUnique();
        builder.Entity<C88WorkflowDefinition>().HasIndex(item => item.C88MunicipalityConfigurationId).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<C88WorkflowDefinition>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_C88WorkflowDefinitions_Version", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_C88WorkflowDefinitions_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
        ConfigureRowVersion(builder.Entity<C88WorkflowDefinition>().Property(item => item.RowVersion));
        builder.Entity<C88WorkflowDefinition>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowDefinition>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.C88MunicipalityConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowDefinition>().HasOne(item => item.PreviousVersion).WithMany(item => item.SuccessorVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowDefinition>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowDefinition>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88WorkflowStage>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88WorkflowStage>().HasIndex(item => new { item.C88WorkflowDefinitionId, item.Sequence }).IsUnique();
        builder.Entity<C88WorkflowStage>().Property(item => item.Name).HasMaxLength(160);
        builder.Entity<C88WorkflowStage>().ToTable(table => table.HasCheckConstraint("CK_C88WorkflowStages_Sequence", "[Sequence] > 0"));
        builder.Entity<C88WorkflowStage>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowStage>().HasOne(item => item.WorkflowDefinition).WithMany(item => item.Stages).HasForeignKey(item => item.C88WorkflowDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowStage>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88WorkflowAction>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88WorkflowAction>().HasIndex(item => new { item.C88IndicatorReportId, item.OccurredAt });
        builder.Entity<C88WorkflowAction>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<C88WorkflowAction>().Property(item => item.SnapshotJson).HasMaxLength(8000);
        builder.Entity<C88WorkflowAction>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowAction>().HasOne(item => item.IndicatorReport).WithMany(item => item.WorkflowActions).HasForeignKey(item => item.C88IndicatorReportId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowAction>().HasOne(item => item.ActorUser).WithMany().HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88WorkflowAction>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<C88OpmsMapping>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<C88OpmsMapping>().HasIndex(item => new { item.C88MunicipalityConfigurationId, item.C88IndicatorId, item.OpmsTargetId }).IsUnique();
        builder.Entity<C88OpmsMapping>().Property(item => item.Reason).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<C88OpmsMapping>().Property(item => item.RowVersion));
        builder.Entity<C88OpmsMapping>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88OpmsMapping>().HasOne(item => item.Configuration).WithMany().HasForeignKey(item => item.C88MunicipalityConfigurationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88OpmsMapping>().HasOne(item => item.Indicator).WithMany().HasForeignKey(item => item.C88IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88OpmsMapping>().HasOne(item => item.OpmsTarget).WithMany().HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88OpmsMapping>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<C88OpmsMapping>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }

    private void ConfigureRowVersion(PropertyBuilder<byte[]> property)
    {
        property.IsConcurrencyToken();
        if (Database.IsSqlServer()) property.IsRowVersion();
        else property.ValueGeneratedNever();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantWrites();
        EnforceAppendOnlyRecords();
        AdvancePortableRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceTenantWrites();
        EnforceAppendOnlyRecords();
        AdvancePortableRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AdvancePortableRowVersions()
    {
        if (Database.IsSqlServer()) return;
        foreach (var entry in ChangeTracker.Entries().Where(item => item.State is EntityState.Added or EntityState.Modified))
        {
            var version = entry.Properties.FirstOrDefault(item => item.Metadata.IsConcurrencyToken && item.Metadata.ClrType == typeof(byte[]));
            if (version != null) version.CurrentValue = Guid.NewGuid().ToByteArray();
        }
    }

    private void EnforceTenantWrites()
    {
        if (_tenantContext == null || (_tenantContext.IsSystem && !_tenantContext.MunicipalityId.HasValue)) return;
        var tenantId = _tenantContext.MunicipalityId;
        var protectedTypes = new HashSet<Type>
        {
            typeof(Department), typeof(Unit), typeof(Position), typeof(Ward), typeof(VoteNumber), typeof(OpmsTarget), typeof(OpmsTargetWard), typeof(OpmsTargetAdditionalAssignee), typeof(OpmsTargetVoteNumber), typeof(IpmsTarget), typeof(OpmsSubmission), typeof(IpmsSubmission),
            typeof(MunicipalEmployee), typeof(EmployeeAssignment), typeof(MunicipalityFinancialYear),
            typeof(PerformancePeriodTarget), typeof(PerformanceTargetRevision), typeof(LegacySubmissionValueArchive)
            , typeof(MunicipalityConsolidationPolicy), typeof(PerformanceSuggestionEvent)
            , typeof(WorkflowDefinition), typeof(WorkflowStageDefinition), typeof(SubmissionWorkflowInstance), typeof(SubmissionWorkflowAction),
            typeof(PerformanceRfi), typeof(PerformanceRfiEvidence), typeof(ReportingWindow), typeof(ReportingWindowException), typeof(RatingScheme), typeof(RatingSchemeValue), typeof(SubmissionStageRating)
            , typeof(EvidenceBlob), typeof(PoeFile), typeof(PoeEvidenceAssessment), typeof(PoeEvidenceReplacement), typeof(PoeLegalHoldEvent), typeof(PoeDisposalEvent), typeof(Notification), typeof(AuditTrail), typeof(BusinessEventOutbox), typeof(NotificationDeliveryAttempt), typeof(IdempotencyRequest), typeof(IdpPlan), typeof(IdpImportBatch), typeof(GovernedRecordLifecycleEvent), typeof(TechnicalIndicatorDescription), typeof(TidSourceDocument), typeof(StrategicDocumentType), typeof(StrategicDocument), typeof(StrategicDocumentEvent),
            typeof(C88CatalogueVersion), typeof(C88MunicipalityConfiguration), typeof(C88CatalogueItem), typeof(C88Indicator), typeof(C88DataElement), typeof(C88IndicatorApplicability), typeof(C88ComplianceQuestion), typeof(C88IndicatorPlan), typeof(C88ReportingCalendar), typeof(C88IndicatorReport), typeof(C88DataElementValue), typeof(C88ComplianceResponse), typeof(C88Assignment), typeof(C88WorkflowDefinition), typeof(C88WorkflowStage), typeof(C88WorkflowAction), typeof(C88OpmsMapping),
            typeof(AuthenticationConfiguration), typeof(AuthenticationPolicy), typeof(UserAuthenticator), typeof(AuthenticationEvent),
            typeof(InternalAuditAssessmentConfiguration), typeof(InternalAuditAssessment), typeof(OfficialReportTemplate), typeof(OfficialReportGeneration),
            typeof(NotificationConfiguration), typeof(NotificationScheduleRule), typeof(WorkingCalendarHoliday), typeof(ScheduledNotification), typeof(NotificationPreference)
        };
        foreach (var entry in ChangeTracker.Entries().Where(item => protectedTypes.Contains(item.Entity.GetType()) && item.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (!tenantId.HasValue || tenantId == long.MinValue) throw new UnauthorizedAccessException("A municipality context is required for this operation.");
            var property = entry.Property("MunicipalityId");
            var current = property.CurrentValue == null ? (long?)null : Convert.ToInt64(property.CurrentValue);
            var original = property.OriginalValue == null ? (long?)null : Convert.ToInt64(property.OriginalValue);
            if (entry.State == EntityState.Added && (!current.HasValue || current.Value == 0))
            {
                property.CurrentValue = tenantId.Value;
                current = tenantId;
            }
            if (current != tenantId || (entry.State != EntityState.Added && original != tenantId))
                throw new UnauthorizedAccessException("Cross-municipality writes are not permitted.");
        }
    }

    private void EnforceAppendOnlyRecords()
    {
        if (ChangeTracker.Entries<PerformanceTargetRevision>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Performance target revision history is append-only.");
        if (ChangeTracker.Entries<PerformanceSuggestionEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Performance suggestion history is append-only.");
        if (ChangeTracker.Entries<SubmissionWorkflowAction>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Submission workflow action history is append-only.");
        if (ChangeTracker.Entries<SubmissionStageRating>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Submission stage rating history is append-only.");
        if (ChangeTracker.Entries<InternalAuditAssessmentConfiguration>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<InternalAuditAssessment>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Internal Audit configuration and assessment history is append-only.");
        if (ChangeTracker.Entries<OfficialReportTemplate>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<OfficialReportGeneration>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Official report templates and generated report history are append-only.");
        if (ChangeTracker.Entries<NotificationConfiguration>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<NotificationScheduleRule>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<WorkingCalendarHoliday>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Notification policy versions, rules, and working-calendar history cannot be deleted or rewritten.");
        if (ChangeTracker.Entries<PerformanceRfiEvidence>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("RFI evidence provenance is append-only.");
        if (ChangeTracker.Entries<PoeEvidenceAssessment>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("POE assessment history is append-only.");
        if (ChangeTracker.Entries<PoeEvidenceReplacement>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("POE replacement provenance is append-only.");
        if (ChangeTracker.Entries<PoeLegalHoldEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("POE legal-hold history is append-only.");
        if (ChangeTracker.Entries<PoeDisposalEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("POE disposal history is append-only.");
        if (ChangeTracker.Entries<AuditTrail>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit history is append-only.");
        if (ChangeTracker.Entries<AuthenticationEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Authentication event history is append-only.");
        if (ChangeTracker.Entries<LoginAuditLog>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Login audit history is append-only.");
        if (ChangeTracker.Entries<IdpImportRow>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("IDP import reconciliation rows are append-only.");
        if (ChangeTracker.Entries<LegacySubmissionValueArchive>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Legacy submission-value archives are append-only.");
        if (ChangeTracker.Entries<GovernedRecordLifecycleEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Governed record lifecycle history is append-only.");
        if (ChangeTracker.Entries<TidSourceDocument>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("TID source-document history is append-only.");
        if (ChangeTracker.Entries<StrategicDocumentEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Strategic-document lifecycle history is append-only.");
        if (ChangeTracker.Entries<C88WorkflowAction>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Circular 88 workflow history is append-only.");
        if (ChangeTracker.Entries<C88DataElementValue>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<C88ComplianceResponse>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Circular 88 report evidence is append-only; create a successor report version.");
        if (ChangeTracker.Entries<C88WorkflowStage>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Circular 88 workflow-stage versions are append-only.");
        var c88GovernedTypes = new HashSet<Type>
        {
            typeof(C88CatalogueVersion), typeof(C88MunicipalityConfiguration), typeof(C88CatalogueItem), typeof(C88Indicator),
            typeof(C88DataElement), typeof(C88IndicatorApplicability), typeof(C88ComplianceQuestion), typeof(C88IndicatorPlan),
            typeof(C88ReportingCalendar), typeof(C88IndicatorReport), typeof(C88Assignment), typeof(C88WorkflowDefinition), typeof(C88OpmsMapping)
        };
        if (ChangeTracker.Entries().Any(entry => entry.State == EntityState.Deleted && c88GovernedTypes.Contains(entry.Entity.GetType())))
            throw new InvalidOperationException("Circular 88 governed records cannot be hard deleted.");
        foreach (var entry in ChangeTracker.Entries<StrategicDocument>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Strategic-document version history is append-only.");
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(StrategicDocument.IsCurrent), nameof(StrategicDocument.IsActive), nameof(StrategicDocument.IsApproved),
                nameof(StrategicDocument.ApprovedAt), nameof(StrategicDocument.ApprovedByUserId), nameof(StrategicDocument.ApprovalReference),
                nameof(StrategicDocument.IsPublished), nameof(StrategicDocument.PublicationDate), nameof(StrategicDocument.PublishedAt),
                nameof(StrategicDocument.PublishedByUserId), nameof(StrategicDocument.RowVersion)
            };
            var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToArray();
            if (changed.Any(property => !allowed.Contains(property))
                || entry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsCurrent)) == false && entry.CurrentValues.GetValue<bool>(nameof(StrategicDocument.IsCurrent))
                || entry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsActive)) == false && entry.CurrentValues.GetValue<bool>(nameof(StrategicDocument.IsActive))
                || entry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsApproved)) && !entry.CurrentValues.GetValue<bool>(nameof(StrategicDocument.IsApproved))
                || entry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsPublished)) && !entry.CurrentValues.GetValue<bool>(nameof(StrategicDocument.IsPublished)))
                throw new InvalidOperationException("Strategic-document versions are append-preserved and lifecycle projections cannot be reversed.");
        }
        foreach (var entry in ChangeTracker.Entries<C88IndicatorReport>().Where(entry => entry.State == EntityState.Modified))
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(C88IndicatorReport.IsCurrent), nameof(C88IndicatorReport.State), nameof(C88IndicatorReport.CurrentStageSequence),
                nameof(C88IndicatorReport.FinalSubmittedAt), nameof(C88IndicatorReport.FinalSubmittedByUserId), nameof(C88IndicatorReport.RowVersion)
            };
            if (entry.Properties.Where(property => property.IsModified).Any(property => !allowed.Contains(property.Metadata.Name))
                || !entry.OriginalValues.GetValue<bool>(nameof(C88IndicatorReport.IsCurrent)) && entry.CurrentValues.GetValue<bool>(nameof(C88IndicatorReport.IsCurrent)))
                throw new InvalidOperationException("Circular 88 report content is append-preserved; create a successor version for content changes.");
        }
        foreach (var entry in ChangeTracker.Entries<TechnicalIndicatorDescription>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("TID version history is append-only.");
            var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToHashSet(StringComparer.Ordinal);
            changed.Remove(nameof(TechnicalIndicatorDescription.IsCurrent));
            changed.Remove(nameof(TechnicalIndicatorDescription.EffectiveTo));
            changed.Remove(nameof(TechnicalIndicatorDescription.RowVersion));
            if (changed.Count > 0 || entry.OriginalValues.GetValue<bool>(nameof(TechnicalIndicatorDescription.IsCurrent)) != true
                || entry.CurrentValues.GetValue<bool>(nameof(TechnicalIndicatorDescription.IsCurrent)) != false
                || entry.OriginalValues.GetValue<DateTime?>(nameof(TechnicalIndicatorDescription.EffectiveTo)).HasValue
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(TechnicalIndicatorDescription.EffectiveTo)).HasValue)
                throw new InvalidOperationException("Published TID versions are append-only; only the current version may be closed by a successor.");
        }
    }
}

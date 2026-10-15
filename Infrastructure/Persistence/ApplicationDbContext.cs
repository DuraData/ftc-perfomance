using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    private readonly ITenantContext? _tenantContext;
    private bool _tenantOwnershipReconciliationActive;
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
    public DbSet<OpmsImportBatch> OpmsImportBatches { get; set; } = null!;
    public DbSet<OpmsImportRow> OpmsImportRows { get; set; } = null!;
    public DbSet<OpmsTargetWard> OpmsTargetWards { get; set; } = null!;
    public DbSet<OpmsTargetAdditionalAssignee> OpmsTargetAdditionalAssignees { get; set; } = null!;
    public DbSet<OpmsTargetVoteNumber> OpmsTargetVoteNumbers { get; set; } = null!;
    public DbSet<StrategicRisk> StrategicRisks { get; set; } = null!;
    public DbSet<OpmsKpiStrategicRisk> OpmsKpiStrategicRisks { get; set; } = null!;
    public DbSet<StrategicRiskAssessment> StrategicRiskAssessments { get; set; } = null!;
    public DbSet<StrategicRiskTreatment> StrategicRiskTreatments { get; set; } = null!;
    public DbSet<StrategicRiskReview> StrategicRiskReviews { get; set; } = null!;
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
    public DbSet<SdbipLayer> SdbipLayers { get; set; } = null!;
    public DbSet<MunicipalEmployee> MunicipalEmployees { get; set; } = null!;
    public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; } = null!;
    public DbSet<PerformancePeriodTarget> PerformancePeriodTargets { get; set; } = null!;
    public DbSet<PerformanceTargetRevision> PerformanceTargetRevisions { get; set; } = null!;
    public DbSet<KpiFieldRevision> KpiFieldRevisions { get; set; } = null!;
    public DbSet<LegacySubmissionValueArchive> LegacySubmissionValueArchives { get; set; } = null!;
    public DbSet<LegacySubmissionPeriodReconciliation> LegacySubmissionPeriodReconciliations { get; set; } = null!;
    public DbSet<LegacyTenantOwnershipReconciliation> LegacyTenantOwnershipReconciliations { get; set; } = null!;
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
    public DbSet<LegacySubmissionRatingReconciliation> LegacySubmissionRatingReconciliations { get; set; } = null!;
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
        ConfigureGlobalStrategicReferences(builder);
        ConfigureStrategicPlanningMasters(builder);
        ConfigurePerformanceTargetStrategicClassifications(builder);
        ConfigureGovernedBudgetClassifications(builder);
        ConfigureGovernedPerformanceClassifications(builder);
        ConfigureIpmsOpmsTargetLinks(builder);

        builder.Entity<Municipality>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<Municipality>().HasIndex(item => item.Code).IsUnique();
        ConfigureRowVersion(builder.Entity<Municipality>().Property(item => item.RowVersion));

        builder.Entity<ApplicationUser>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<ApplicationUser>().Property(item => item.RowVersion));
        builder.Entity<ApplicationUser>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AuthenticationConfiguration>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<AuthenticationConfiguration>().HasIndex(item => new { item.MunicipalityId, item.VersionNumber }).IsUnique();
        builder.Entity<AuthenticationConfiguration>().HasIndex(item => new { item.MunicipalityId, item.IsCurrent }).IsUnique().HasFilter("[IsCurrent] = 1");
        builder.Entity<AuthenticationConfiguration>().HasIndex(item => new { item.ConfigurationFamilyPublicId, item.VersionNumber }).IsUnique();
        builder.Entity<AuthenticationConfiguration>().Property(item => item.ProviderRegistrationCode).HasMaxLength(40);
        builder.Entity<AuthenticationConfiguration>().Property(item => item.DisplayName).HasMaxLength(160);
        builder.Entity<AuthenticationConfiguration>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_AuthenticationConfigurations_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint("CK_AuthenticationConfigurations_Version", "[VersionNumber] >= 1");
        });
        ConfigureRowVersion(builder.Entity<AuthenticationConfiguration>().Property(item => item.RowVersion));
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.ModifiedByUser).WithMany().HasForeignKey(item => item.ModifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasOne(item => item.PreviousVersion).WithMany(item => item.SuccessorVersions).HasForeignKey(item => item.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationConfiguration>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<AuthenticationPolicy>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<AuthenticationPolicy>().HasIndex(item => item.AuthenticationConfigurationId).IsUnique();
        builder.Entity<AuthenticationPolicy>().HasIndex(item => item.MunicipalityId);
        builder.Entity<AuthenticationPolicy>().ToTable(table => table.HasCheckConstraint("CK_AuthenticationPolicies_Bounds", "[MinimumPasswordLength] BETWEEN 12 AND 128 AND [MaximumFailedAttempts] BETWEEN 1 AND 20 AND [LockoutMinutes] BETWEEN 1 AND 1440 AND [SessionIdleTimeoutMinutes] BETWEEN 5 AND 1440 AND [SessionAbsoluteTimeoutHours] BETWEEN 1 AND 720 AND [MaximumConcurrentSessions] BETWEEN 1 AND 50"));
        ConfigureRowVersion(builder.Entity<AuthenticationPolicy>().Property(item => item.RowVersion));
        builder.Entity<AuthenticationPolicy>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuthenticationPolicy>().HasOne(item => item.AuthenticationConfiguration).WithOne(item => item.Policy).HasForeignKey<AuthenticationPolicy>(item => item.AuthenticationConfigurationId).OnDelete(DeleteBehavior.Restrict);
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
        builder.Entity<MunicipalityFinancialYear>().HasAlternateKey(item => new { item.Id, item.MunicipalityId });
        ConfigureRowVersion(builder.Entity<MunicipalityFinancialYear>().Property(item => item.RowVersion));
        builder.Entity<MunicipalityFinancialYear>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityFinancialYear>().HasOne(item => item.FinancialYear).WithMany().HasForeignKey(item => item.FinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingPeriod>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<ReportingPeriod>().HasIndex(item => new { item.MunicipalityFinancialYearId, item.Code }).IsUnique();
        ConfigureRowVersion(builder.Entity<ReportingPeriod>().Property(item => item.RowVersion));
        builder.Entity<ReportingPeriod>().HasOne(item => item.MunicipalityFinancialYear).WithMany(item => item.ReportingPeriods).HasForeignKey(item => item.MunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReportingPeriod>().ToTable(table => table.HasCheckConstraint("CK_ReportingPeriods_DateRange", "[EndDate] >= [StartDate]"));
        builder.Entity<SdbipLayer>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<SdbipLayer>().HasIndex(item => new { item.MunicipalityFinancialYearId, item.Code }).IsUnique();
        builder.Entity<SdbipLayer>().HasIndex(item => new { item.MunicipalityId, item.IsActive, item.DisplayOrder });
        builder.Entity<SdbipLayer>().HasAlternateKey(item => new { item.Id, item.MunicipalityId });
        builder.Entity<SdbipLayer>().Property(item => item.Code).HasMaxLength(80);
        builder.Entity<SdbipLayer>().Property(item => item.Name).HasMaxLength(200);
        builder.Entity<SdbipLayer>().Property(item => item.Description).HasMaxLength(1000);
        ConfigureRowVersion(builder.Entity<SdbipLayer>().Property(item => item.RowVersion));
        builder.Entity<SdbipLayer>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SdbipLayer>().HasOne(item => item.MunicipalityFinancialYear).WithMany(item => item.SdbipLayers)
            .HasForeignKey(item => new { item.MunicipalityFinancialYearId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SdbipLayer>().ToTable(table => table.HasCheckConstraint("CK_SdbipLayers_DisplayOrder", "[DisplayOrder] > 0"));

        builder.Entity<MunicipalEmployee>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<MunicipalEmployee>().HasIndex(item => new { item.MunicipalityId, item.EmployeeNumber }).IsUnique();
        builder.Entity<MunicipalEmployee>().HasIndex(item => item.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
        builder.Entity<MunicipalEmployee>().Property(item => item.SalaryReference).HasMaxLength(120);
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
        builder.Entity<PerformancePeriodTarget>().Property(item => item.RevisedTargetValue).HasMaxLength(1024);
        builder.Entity<PerformancePeriodTarget>().Property(item => item.BudgetValue).HasPrecision(18, 2);
        builder.Entity<PerformancePeriodTarget>().Property(item => item.RevisedBudgetValue).HasPrecision(18, 2);
        ConfigureRowVersion(builder.Entity<PerformancePeriodTarget>().Property(item => item.RowVersion));
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.OpmsTarget).WithMany().HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.IpmsTarget).WithMany().HasForeignKey(item => item.IpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformancePeriodTarget>().ToTable(table => table.HasCheckConstraint("CK_PerformancePeriodTargets_OneKpi", "CASE WHEN [OpmsTargetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsTargetId] IS NULL THEN 0 ELSE 1 END = 1"));
        builder.Entity<OpmsTarget>().HasIndex(item => new { item.MunicipalityId, item.OriginalOrderNumber });
        builder.Entity<OpmsTarget>().HasIndex(item => new { item.MunicipalityId, item.RevisedOrderNumber });
        builder.Entity<OpmsTarget>().Property(item => item.RevisedIndicatorNumber).HasMaxLength(100);
        builder.Entity<OpmsTarget>().Property(item => item.RevisedTargetName).HasMaxLength(500);
        builder.Entity<OpmsTarget>().Property(item => item.RevisedKpiDescription).HasMaxLength(2000);
        builder.Entity<OpmsTarget>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_OpmsTargets_OriginalOrderNumber", "[OriginalOrderNumber] > 0");
            table.HasCheckConstraint("CK_OpmsTargets_RevisedOrderNumber", "[RevisedOrderNumber] > 0");
        });
        builder.Entity<IpmsTarget>().HasIndex(item => new { item.MunicipalityId, item.OriginalOrderNumber });
        builder.Entity<IpmsTarget>().HasIndex(item => new { item.MunicipalityId, item.RevisedOrderNumber });
        builder.Entity<IpmsTarget>().Property(item => item.RevisedIndicatorNumber).HasMaxLength(100);
        builder.Entity<IpmsTarget>().Property(item => item.RevisedTargetName).HasMaxLength(500);
        builder.Entity<IpmsTarget>().Property(item => item.RevisedKpiDescription).HasMaxLength(2000);
        builder.Entity<IpmsTarget>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_IpmsTargets_OriginalOrderNumber", "[OriginalOrderNumber] > 0");
            table.HasCheckConstraint("CK_IpmsTargets_RevisedOrderNumber", "[RevisedOrderNumber] > 0");
        });
        builder.Entity<PerformanceTargetRevision>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<PerformanceTargetRevision>().HasIndex(item => new { item.PerformancePeriodTargetId, item.RecordedAt });
        builder.Entity<PerformanceTargetRevision>().Property(item => item.FieldName).HasMaxLength(100);
        builder.Entity<PerformanceTargetRevision>().Property(item => item.OriginalValue).HasMaxLength(2048);
        builder.Entity<PerformanceTargetRevision>().Property(item => item.RevisedValue).HasMaxLength(2048);
        builder.Entity<PerformanceTargetRevision>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceTargetRevision>().HasOne(item => item.PerformancePeriodTarget).WithMany(item => item.Revisions).HasForeignKey(item => item.PerformancePeriodTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerformanceTargetRevision>().HasOne(item => item.RevisedByUser).WithMany().HasForeignKey(item => item.RevisedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiFieldRevision>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<KpiFieldRevision>().HasIndex(item => new { item.OpmsTargetId, item.RecordedAt });
        builder.Entity<KpiFieldRevision>().HasIndex(item => new { item.IpmsTargetId, item.RecordedAt });
        builder.Entity<KpiFieldRevision>().Property(item => item.FieldName).HasMaxLength(100);
        builder.Entity<KpiFieldRevision>().Property(item => item.OriginalValue).HasMaxLength(2048);
        builder.Entity<KpiFieldRevision>().Property(item => item.RevisedValue).HasMaxLength(2048);
        builder.Entity<KpiFieldRevision>().Property(item => item.Reason).HasMaxLength(2000);
        builder.Entity<KpiFieldRevision>().Property(item => item.ApprovalReference).HasMaxLength(500);
        builder.Entity<KpiFieldRevision>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiFieldRevision>().HasOne(item => item.OpmsTarget).WithMany().HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiFieldRevision>().HasOne(item => item.IpmsTarget).WithMany().HasForeignKey(item => item.IpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiFieldRevision>().HasOne(item => item.RevisedByUser).WithMany().HasForeignKey(item => item.RevisedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiFieldRevision>().ToTable(table => table.HasCheckConstraint("CK_KpiFieldRevisions_OneKpi", "CASE WHEN [OpmsTargetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsTargetId] IS NULL THEN 0 ELSE 1 END = 1"));

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
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasIndex(item => item.OpmsSubmissionId).IsUnique().HasFilter("[OpmsSubmissionId] IS NOT NULL");
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasIndex(item => item.IpmsSubmissionId).IsUnique().HasFilter("[IpmsSubmissionId] IS NOT NULL");
        builder.Entity<LegacySubmissionPeriodReconciliation>().Property(item => item.SourceQuarterSnapshot).HasMaxLength(100);
        builder.Entity<LegacySubmissionPeriodReconciliation>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<LegacySubmissionPeriodReconciliation>().Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasOne(item => item.OpmsSubmission).WithMany().HasForeignKey(item => item.OpmsSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasOne(item => item.IpmsSubmission).WithMany().HasForeignKey(item => item.IpmsSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasOne(item => item.ReportingPeriod).WithMany().HasForeignKey(item => item.ReportingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasOne(item => item.ReconciledByUser).WithMany().HasForeignKey(item => item.ReconciledByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionPeriodReconciliation>().ToTable(table => table.HasCheckConstraint("CK_LegacySubmissionPeriodReconciliations_OneSubmission", "CASE WHEN [OpmsSubmissionId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsSubmissionId] IS NULL THEN 0 ELSE 1 END = 1"));
        builder.Entity<LegacyTenantOwnershipReconciliation>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<LegacyTenantOwnershipReconciliation>().HasIndex(item => new { item.EntityType, item.EntityPublicId }).IsUnique();
        builder.Entity<LegacyTenantOwnershipReconciliation>().HasIndex(item => new { item.MunicipalityId, item.ReconciledAt });
        builder.Entity<LegacyTenantOwnershipReconciliation>().Property(item => item.EntityType).HasMaxLength(80);
        builder.Entity<LegacyTenantOwnershipReconciliation>().Property(item => item.SourceEvidence).HasMaxLength(2000);
        builder.Entity<LegacyTenantOwnershipReconciliation>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<LegacyTenantOwnershipReconciliation>().Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Entity<LegacyTenantOwnershipReconciliation>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacyTenantOwnershipReconciliation>().HasOne(item => item.ReconciledByUser).WithMany().HasForeignKey(item => item.ReconciledByUserId).OnDelete(DeleteBehavior.Restrict);

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
        builder.Entity<LegacySubmissionRatingReconciliation>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<LegacySubmissionRatingReconciliation>().HasIndex(x => new { x.MunicipalityId, x.SourceKey }).IsUnique();
        builder.Entity<LegacySubmissionRatingReconciliation>().HasIndex(x => x.SubmissionStageRatingId);
        builder.Entity<LegacySubmissionRatingReconciliation>().Property(x => x.SourceKey).HasMaxLength(550);
        builder.Entity<LegacySubmissionRatingReconciliation>().Property(x => x.SourceScore).HasPrecision(18, 4);
        builder.Entity<LegacySubmissionRatingReconciliation>().Property(x => x.SourceActorUserIdSnapshot).HasMaxLength(450);
        builder.Entity<LegacySubmissionRatingReconciliation>().Property(x => x.SourceCommentSnapshot).HasMaxLength(2000);
        builder.Entity<LegacySubmissionRatingReconciliation>().Property(x => x.Reason).HasMaxLength(1000);
        builder.Entity<LegacySubmissionRatingReconciliation>().Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.SubmissionWorkflowInstance).WithMany().HasForeignKey(x => x.SubmissionWorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.SubmissionWorkflowAction).WithMany().HasForeignKey(x => x.SubmissionWorkflowActionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.WorkflowStageDefinition).WithMany().HasForeignKey(x => x.WorkflowStageDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.RatingScheme).WithMany().HasForeignKey(x => x.RatingSchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.RatingSchemeValue).WithMany().HasForeignKey(x => x.RatingSchemeValueId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.SubmissionStageRating).WithMany().HasForeignKey(x => x.SubmissionStageRatingId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LegacySubmissionRatingReconciliation>().HasOne(x => x.ReconciledByUser).WithMany().HasForeignKey(x => x.ReconciledByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LoginAuditLog>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<LoginAuditLog>().HasIndex(item => new { item.MunicipalityId, item.LoggedAt });
        builder.Entity<LoginAuditLog>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LoginAuditLog>().HasOne(item => item.User).WithMany(item => item.LoginAuditLogs).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
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
        builder.Entity<DueDateExtension>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<ReviewComment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<AuditFinding>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<SubmissionScore>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<MunicipalEmployee>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<EmployeeAssignment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<MunicipalityFinancialYear>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<ReportingPeriod>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityFinancialYear.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<SdbipLayer>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PerformancePeriodTarget>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<PerformanceTargetRevision>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<KpiFieldRevision>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<LegacySubmissionValueArchive>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<LegacySubmissionPeriodReconciliation>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<LegacyTenantOwnershipReconciliation>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
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
        builder.Entity<LegacySubmissionRatingReconciliation>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);

        builder.Entity<ApplicationRole>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<ApplicationRole>().HasIndex(item => new { item.MunicipalityId, item.RoleCode }).IsUnique();
        ConfigureRowVersion(builder.Entity<ApplicationRole>().Property(item => item.RowVersion));
        builder.Entity<ApplicationRole>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>()
            .HasOne<ApplicationRole>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>()
            .HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>()
            .HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
            .HasOne<ApplicationRole>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
            .HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>()
            .HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Permission>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<SecurityResource>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<SecurityResource>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityResource>().Property(item => item.RowVersion));
        builder.Entity<SecurityNavigationItem>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<SecurityNavigationItem>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityNavigationItem>().Property(item => item.RowVersion));
        builder.Entity<SecurityNavigationItem>().HasOne(item => item.Parent).WithMany(item => item.Children).HasForeignKey(item => item.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SecurityActionDefinition>().HasIndex(item => item.Code).IsUnique();
        builder.Entity<SecurityActionDefinition>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityActionDefinition>().Property(item => item.RowVersion));
        builder.Entity<SecurityMemberDefinition>().HasIndex(item => new { item.ResourceCode, item.MemberCode }).IsUnique();
        builder.Entity<SecurityMemberDefinition>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<SecurityMemberDefinition>().Property(item => item.RowVersion));

        builder.Entity<SecurityUserRoleAssignment>().HasIndex(item => item.PublicId).IsUnique();
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
        builder.Entity<RefreshToken>().HasOne(item => item.User).WithMany(item => item.RefreshTokens)
            .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);

        // Configure RolePermission
        builder.Entity<RolePermission>()
            .HasKey(rp => rp.Id);
        builder.Entity<RolePermission>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<RolePermission>().HasIndex(item => new { item.RoleId, item.PermissionId, item.EffectiveFrom });
        ConfigureRowVersion(builder.Entity<RolePermission>().Property(item => item.RowVersion));
        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure UserPermissionOverride
        builder.Entity<UserPermissionOverride>()
            .HasKey(upo => new { upo.UserId, upo.PermissionId });
        builder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.User)
            .WithMany(u => u.PermissionOverrides)
            .HasForeignKey(upo => upo.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.Permission)
            .WithMany(p => p.UserPermissionOverrides)
            .HasForeignKey(upo => upo.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);

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
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>()
            .HasOne(us => us.Municipality)
            .WithMany()
            .HasForeignKey(us => us.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<UserScope>().Property(item => item.RowVersion));

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
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserAssignment>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(ua => ua.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserAssignment>().HasIndex(item => item.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<UserAssignment>().Property(item => item.RowVersion));

        builder.Entity<OpmsTargetTemplate>()
            .HasIndex(template => template.TemplateCode)
            .IsUnique();
        builder.Entity<OpmsTargetTemplate>().HasIndex(template => template.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<OpmsTargetTemplate>().Property(template => template.RowVersion));

        builder.Entity<IpmsTargetTemplate>()
            .HasIndex(template => template.TemplateCode)
            .IsUnique();
        builder.Entity<IpmsTargetTemplate>().HasIndex(template => template.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IpmsTargetTemplate>().Property(template => template.RowVersion));

        builder.Entity<OpmsTargetTemplateVersion>().HasIndex(version => version.PublicId).IsUnique();
        builder.Entity<OpmsTargetTemplateVersion>()
            .HasIndex(version => new { version.OpmsTargetTemplateId, version.Version })
            .IsUnique();
        ConfigureRowVersion(builder.Entity<OpmsTargetTemplateVersion>().Property(version => version.RowVersion));

        builder.Entity<IpmsTargetTemplateVersion>().HasIndex(version => version.PublicId).IsUnique();
        builder.Entity<IpmsTargetTemplateVersion>()
            .HasIndex(version => new { version.IpmsTargetTemplateId, version.Version })
            .IsUnique();
        ConfigureRowVersion(builder.Entity<IpmsTargetTemplateVersion>().Property(version => version.RowVersion));

        builder.Entity<OpmsTargetTemplateVersion>()
            .HasOne(version => version.OpmsTargetTemplate)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.OpmsTargetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTargetTemplateVersion>()
            .HasOne(version => version.IpmsTargetTemplate)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.IpmsTargetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTarget>().Property(item => item.ReasonForWithdrawal).HasMaxLength(1000);
        builder.Entity<OpmsTarget>().ToTable(table => table.HasCheckConstraint(
            "CK_OpmsTargets_WithdrawalMetadata",
            "[IsWithdrawn] = 0 OR ([ReasonForWithdrawal] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<OpmsTarget>().Property(item => item.RowVersion));
        builder.Entity<OpmsTarget>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTarget>().HasOne(item => item.SdbipLayer).WithMany(item => item.OpmsTargets)
            .HasForeignKey(item => new { item.SdbipLayerId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTargetWard>().HasIndex(item => new { item.OpmsTargetId, item.WardId }).IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<OpmsTargetWard>().Property(item => item.UnlinkReason).HasMaxLength(2000);
        builder.Entity<OpmsTargetWard>().ToTable(table => table.HasCheckConstraint("CK_OpmsTargetWards_UnlinkLifecycle", "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkedByUserId] IS NOT NULL AND [UnlinkReason] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<OpmsTargetWard>().Property(item => item.RowVersion));
        builder.Entity<OpmsTargetWard>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasOne(item => item.Target).WithMany(item => item.Wards).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasOne(item => item.Ward).WithMany().HasForeignKey(item => item.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasOne(item => item.LinkedByUser).WithMany().HasForeignKey(item => item.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetWard>().HasOne(item => item.UnlinkedByUser).WithMany().HasForeignKey(item => item.UnlinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTargetAdditionalAssignee>().HasIndex(item => new { item.OpmsTargetId, item.UserId }).IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<OpmsTargetAdditionalAssignee>().Property(item => item.UnlinkReason).HasMaxLength(2000);
        builder.Entity<OpmsTargetAdditionalAssignee>().ToTable(table => table.HasCheckConstraint("CK_OpmsTargetAdditionalAssignees_UnlinkLifecycle", "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkedByUserId] IS NOT NULL AND [UnlinkReason] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<OpmsTargetAdditionalAssignee>().Property(item => item.RowVersion));
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.Target).WithMany(item => item.AdditionalAssignees).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.LinkedByUser).WithMany().HasForeignKey(item => item.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetAdditionalAssignee>().HasOne(item => item.UnlinkedByUser).WithMany().HasForeignKey(item => item.UnlinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsTargetVoteNumber>().HasIndex(item => new { item.OpmsTargetId, item.VoteNumberId }).IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<OpmsTargetVoteNumber>().Property(item => item.UnlinkReason).HasMaxLength(2000);
        builder.Entity<OpmsTargetVoteNumber>().ToTable(table => table.HasCheckConstraint("CK_OpmsTargetVoteNumbers_UnlinkLifecycle", "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkedByUserId] IS NOT NULL AND [UnlinkReason] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<OpmsTargetVoteNumber>().Property(item => item.RowVersion));
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.Target).WithMany(item => item.VoteNumbers).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.VoteNumber).WithMany().HasForeignKey(item => item.VoteNumberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.LinkedByUser).WithMany().HasForeignKey(item => item.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsTargetVoteNumber>().HasOne(item => item.UnlinkedByUser).WithMany().HasForeignKey(item => item.UnlinkedByUserId).OnDelete(DeleteBehavior.Restrict);
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
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Notification>().HasIndex(notification => notification.PublicId).IsUnique();
        builder.Entity<Notification>().HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedAt });
        builder.Entity<Notification>().ToTable(table => table.HasCheckConstraint("CK_Notifications_ReadLifecycle",
            "([IsRead] = 0 AND [ReadAt] IS NULL AND [ReadByUserId] IS NULL) OR ([IsRead] = 1 AND [ReadAt] IS NOT NULL)"));
        ConfigureRowVersion(builder.Entity<Notification>().Property(notification => notification.RowVersion));
        builder.Entity<Notification>().HasOne(notification => notification.Municipality).WithMany().HasForeignKey(notification => notification.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Notification>().HasOne(notification => notification.ReadByUser).WithMany().HasForeignKey(notification => notification.ReadByUserId).OnDelete(DeleteBehavior.Restrict);
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
        builder.Entity<OpmsImportBatch>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsImportRow>().HasQueryFilter(item => TenantFilterBypass || item.OpmsImportBatch.MunicipalityId == CurrentMunicipalityIdOrSentinel);
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
            .HasOne(item => item.Municipality)
            .WithMany()
            .HasForeignKey(item => item.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<DueDateExtension>()
            .HasOne(item => item.ApprovedByUser)
            .WithMany()
            .HasForeignKey(item => item.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<DueDateExtension>().HasIndex(item => new { item.MunicipalityId, item.SubmissionKind, item.SubmissionId });

        builder.Entity<ReviewComment>()
            .HasOne(item => item.Municipality)
            .WithMany()
            .HasForeignKey(item => item.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReviewComment>()
            .HasOne(comment => comment.CommentedByUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommentedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReviewComment>().HasIndex(item => new { item.MunicipalityId, item.SubmissionKind, item.SubmissionId });

        builder.Entity<AuditFinding>()
            .HasOne(item => item.Municipality)
            .WithMany()
            .HasForeignKey(item => item.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuditFinding>()
            .HasOne(finding => finding.CreatedByUser)
            .WithMany()
            .HasForeignKey(finding => finding.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AuditFinding>().HasIndex(item => new { item.MunicipalityId, item.SubmissionKind, item.SubmissionId });

        builder.Entity<SubmissionScore>()
            .HasOne(item => item.Municipality)
            .WithMany()
            .HasForeignKey(item => item.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionScore>()
            .HasOne(score => score.ScoredByUser)
            .WithMany()
            .HasForeignKey(score => score.ScoredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SubmissionScore>().HasIndex(item => new { item.MunicipalityId, item.SubmissionKind, item.SubmissionId });

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
            .OnDelete(DeleteBehavior.Restrict);

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
        builder.Entity<VoteNumber>().HasIndex(vn => new { vn.MunicipalityId, vn.MunicipalityFinancialYearId, vn.Code }).IsUnique().HasFilter("[MunicipalityFinancialYearId] IS NOT NULL");
        builder.Entity<VoteNumber>().HasOne(vn => vn.Municipality).WithMany().HasForeignKey(vn => vn.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<VoteNumber>().HasOne(vn => vn.MunicipalityFinancialYear).WithMany()
            .HasForeignKey(vn => new { Id = vn.MunicipalityFinancialYearId, vn.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<VoteNumber>().Property(vn => vn.RowVersion));

        builder.Entity<VoteNumber>()
            .HasOne(vn => vn.Department)
            .WithMany()
            .HasForeignKey(vn => vn.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VoteNumber>()
            .Property(vn => vn.Amount)
            .HasPrecision(18, 2);

        builder.Entity<StrategicRisk>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<StrategicRisk>().ToTable("OPMS_StrategicRisks");
        builder.Entity<StrategicRisk>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicRisk>().HasAlternateKey(item => new { item.Id, item.MunicipalityId });
        builder.Entity<StrategicRisk>().HasIndex(item => new { item.MunicipalityId, item.RiskReference }).IsUnique().HasFilter("[RiskReference] IS NOT NULL");
        builder.Entity<StrategicRisk>().Property(item => item.RiskReference).HasMaxLength(100);
        builder.Entity<StrategicRisk>().Property(item => item.RiskTitle).HasMaxLength(500);
        builder.Entity<StrategicRisk>().Property(item => item.RiskDescription).HasMaxLength(2000);
        builder.Entity<StrategicRisk>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRisk>().HasOne(item => item.EffectiveFromMunicipalityFinancialYear).WithMany().HasForeignKey(item => item.EffectiveFromMunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRisk>().HasOne(item => item.EffectiveToMunicipalityFinancialYear).WithMany().HasForeignKey(item => item.EffectiveToMunicipalityFinancialYearId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRisk>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRisk>().HasOne(item => item.UpdatedByUser).WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRisk>().HasOne(item => item.OwnerUser).WithMany().HasForeignKey(item => item.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<StrategicRisk>().Property(item => item.RowVersion));

        builder.Entity<OpmsKpiStrategicRisk>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<OpmsKpiStrategicRisk>().ToTable("OPMS_KPIStrategicRisks");
        builder.Entity<OpmsKpiStrategicRisk>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<OpmsKpiStrategicRisk>().HasIndex(item => new { item.OpmsTargetId, item.StrategicRiskId }).IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<OpmsKpiStrategicRisk>().HasIndex(item => new { item.OpmsTargetId, item.IsPrimary }).IsUnique().HasFilter("[IsActive] = 1 AND [IsPrimary] = 1");
        builder.Entity<OpmsKpiStrategicRisk>().Property(item => item.LinkReason).HasMaxLength(1000);
        builder.Entity<OpmsKpiStrategicRisk>().Property(item => item.UnlinkReason).HasMaxLength(1000);
        builder.Entity<OpmsKpiStrategicRisk>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsKpiStrategicRisk>().HasOne(item => item.OpmsTarget).WithMany().HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsKpiStrategicRisk>().HasOne(item => item.StrategicRisk).WithMany(item => item.KpiLinks).HasForeignKey(item => item.StrategicRiskId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsKpiStrategicRisk>().HasOne(item => item.LinkedByUser).WithMany().HasForeignKey(item => item.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsKpiStrategicRisk>().HasOne(item => item.UnlinkedByUser).WithMany().HasForeignKey(item => item.UnlinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<OpmsKpiStrategicRisk>().Property(item => item.RowVersion));

        builder.Entity<StrategicRiskAssessment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<StrategicRiskAssessment>().ToTable("OPMS_StrategicRiskAssessments", table =>
        {
            table.HasCheckConstraint("CK_OPMS_StrategicRiskAssessments_InherentLikelihood", "[InherentLikelihood] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_OPMS_StrategicRiskAssessments_InherentImpact", "[InherentImpact] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_OPMS_StrategicRiskAssessments_ResidualLikelihood", "[ResidualLikelihood] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_OPMS_StrategicRiskAssessments_ResidualImpact", "[ResidualImpact] BETWEEN 1 AND 5");
        });
        builder.Entity<StrategicRiskAssessment>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicRiskAssessment>().HasIndex(item => new { item.MunicipalityId, item.StrategicRiskId, item.AssessedAt });
        builder.Entity<StrategicRiskAssessment>().Property(item => item.ControlAssessment).HasMaxLength(4000);
        builder.Entity<StrategicRiskAssessment>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<StrategicRiskAssessment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskAssessment>().HasOne(item => item.StrategicRisk).WithMany(item => item.Assessments)
            .HasForeignKey(item => new { item.StrategicRiskId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskAssessment>().HasOne(item => item.AssessedByUser).WithMany().HasForeignKey(item => item.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StrategicRiskTreatment>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<StrategicRiskTreatment>().ToTable("OPMS_StrategicRiskTreatments", table =>
        {
            table.HasCheckConstraint("CK_OPMS_StrategicRiskTreatments_Status", "[Status] IN ('OPEN','IN_PROGRESS','COMPLETED','CANCELLED')");
            table.HasCheckConstraint("CK_OPMS_StrategicRiskTreatments_Progress", "[ProgressPercent] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_OPMS_StrategicRiskTreatments_Completion", "[Status] <> 'COMPLETED' OR [ProgressPercent] = 100");
        });
        builder.Entity<StrategicRiskTreatment>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicRiskTreatment>().HasIndex(item => new { item.MunicipalityId, item.StrategicRiskId, item.Status, item.DueAt });
        builder.Entity<StrategicRiskTreatment>().Property(item => item.Title).HasMaxLength(500);
        builder.Entity<StrategicRiskTreatment>().Property(item => item.Description).HasMaxLength(4000);
        builder.Entity<StrategicRiskTreatment>().Property(item => item.Status).HasMaxLength(20);
        builder.Entity<StrategicRiskTreatment>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskTreatment>().HasOne(item => item.StrategicRisk).WithMany(item => item.Treatments)
            .HasForeignKey(item => new { item.StrategicRiskId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskTreatment>().HasOne(item => item.OwnerUser).WithMany().HasForeignKey(item => item.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskTreatment>().HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskTreatment>().HasOne(item => item.UpdatedByUser).WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder.Entity<StrategicRiskTreatment>().Property(item => item.RowVersion));

        builder.Entity<StrategicRiskReview>().HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
        builder.Entity<StrategicRiskReview>().ToTable("OPMS_StrategicRiskReviews");
        builder.Entity<StrategicRiskReview>().HasIndex(item => item.PublicId).IsUnique();
        builder.Entity<StrategicRiskReview>().HasIndex(item => new { item.MunicipalityId, item.StrategicRiskId, item.ReviewedAt });
        builder.Entity<StrategicRiskReview>().Property(item => item.Outcome).HasMaxLength(4000);
        builder.Entity<StrategicRiskReview>().Property(item => item.Reason).HasMaxLength(1000);
        builder.Entity<StrategicRiskReview>().HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskReview>().HasOne(item => item.StrategicRisk).WithMany(item => item.Reviews)
            .HasForeignKey(item => new { item.StrategicRiskId, item.MunicipalityId })
            .HasPrincipalKey(item => new { item.Id, item.MunicipalityId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StrategicRiskReview>().HasOne(item => item.ReviewedByUser).WithMany().HasForeignKey(item => item.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpPlanVersion>()
            .HasOne(version => version.CreatedByUser)
            .WithMany()
            .HasForeignKey(version => version.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpChangeLog>()
            .HasOne(changeLog => changeLog.IdpPlanVersion)
            .WithMany(version => version.ChangeLogs)
            .HasForeignKey(changeLog => changeLog.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpChangeLog>()
            .HasOne(changeLog => changeLog.ChangedByUser)
            .WithMany()
            .HasForeignKey(changeLog => changeLog.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpChangeLog>().HasIndex(changeLog => changeLog.PublicId).IsUnique();

        builder.Entity<IdpStrategicOutcome>()
            .HasIndex(outcome => new { outcome.IdpPlanId, outcome.Code })
            .IsUnique();
        builder.Entity<IdpStrategicOutcome>().HasIndex(outcome => outcome.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpStrategicOutcome>().Property(outcome => outcome.RowVersion));

        builder.Entity<IdpStrategicOutcome>()
            .HasOne(outcome => outcome.IdpPlan)
            .WithMany(plan => plan.StrategicOutcomes)
            .HasForeignKey(outcome => outcome.IdpPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicObjective>()
            .HasIndex(objective => new { objective.IdpStrategicOutcomeId, objective.Code })
            .IsUnique();
        builder.Entity<IdpStrategicObjective>().HasIndex(objective => objective.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpStrategicObjective>().Property(objective => objective.RowVersion));

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.IdpStrategicOutcome)
            .WithMany(outcome => outcome.StrategicObjectives)
            .HasForeignKey(objective => objective.IdpStrategicOutcomeId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpProgramme>()
            .HasIndex(programme => new { programme.IdpDevelopmentPriorityId, programme.ProgrammeCode })
            .IsUnique();
        builder.Entity<IdpProgramme>().HasIndex(programme => programme.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpProgramme>().Property(programme => programme.RowVersion));

        builder.Entity<IdpProgramme>()
            .HasOne(programme => programme.IdpDevelopmentPriority)
            .WithMany(priority => priority.Programmes)
            .HasForeignKey(programme => programme.IdpDevelopmentPriorityId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);

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
        builder.Entity<IdpImportRow>().HasOne(row => row.IdpImportBatch).WithMany(batch => batch.Rows).HasForeignKey(row => row.IdpImportBatchId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsImportBatch>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<OpmsImportBatch>().HasIndex(x => new { x.MunicipalityId, x.ClientRequestId }).IsUnique();
        builder.Entity<OpmsImportBatch>().HasIndex(x => new { x.SdbipLayerId, x.CreatedAt });
        builder.Entity<OpmsImportBatch>().Property(x => x.SourceFileName).HasMaxLength(260);
        builder.Entity<OpmsImportBatch>().Property(x => x.SourceSha256).HasMaxLength(64);
        builder.Entity<OpmsImportBatch>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_OpmsImportBatches_RowCounts", "[TotalRows] > 0 AND [TotalRows] = [NewRows] + [UnchangedRows] + [ChangedRows] + [InvalidRows]");
            table.HasCheckConstraint("CK_OpmsImportBatches_CommitMetadata", "[Status] <> 1 OR ([CommittedAt] IS NOT NULL AND [CommittedByUserId] IS NOT NULL)");
        });
        ConfigureRowVersion(builder.Entity<OpmsImportBatch>().Property(x => x.RowVersion));
        builder.Entity<OpmsImportBatch>().HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsImportBatch>().HasOne(x => x.SdbipLayer).WithMany().HasForeignKey(x => x.SdbipLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsImportBatch>().HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsImportBatch>().HasOne(x => x.CommittedByUser).WithMany().HasForeignKey(x => x.CommittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OpmsImportRow>().HasIndex(x => x.PublicId).IsUnique();
        builder.Entity<OpmsImportRow>().HasIndex(x => new { x.OpmsImportBatchId, x.SourceRowNumber }).IsUnique();
        builder.Entity<OpmsImportRow>().Property(x => x.Reference).HasMaxLength(240);
        builder.Entity<OpmsImportRow>().Property(x => x.ErrorCode).HasMaxLength(80);
        builder.Entity<OpmsImportRow>().Property(x => x.ErrorPeriod).HasMaxLength(80);
        builder.Entity<OpmsImportRow>().Property(x => x.ErrorField).HasMaxLength(120);
        builder.Entity<OpmsImportRow>().Property(x => x.SuppliedValue).HasMaxLength(1000);
        builder.Entity<OpmsImportRow>().Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Entity<OpmsImportRow>().HasOne(x => x.OpmsImportBatch).WithMany(x => x.Rows).HasForeignKey(x => x.OpmsImportBatchId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpAnnualTarget>()
            .HasIndex(annualTarget => new { annualTarget.IdpKpiId, annualTarget.FinancialYear })
            .IsUnique();
        builder.Entity<IdpAnnualTarget>().HasIndex(annualTarget => annualTarget.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpAnnualTarget>().Property(annualTarget => annualTarget.RowVersion));

        builder.Entity<IdpAnnualTarget>()
            .HasOne(annualTarget => annualTarget.IdpKpi)
            .WithMany(kpi => kpi.AnnualTargets)
            .HasForeignKey(annualTarget => annualTarget.IdpKpiId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpAlignmentLink>().HasIndex(link => link.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpAlignmentLink>().Property(link => link.RowVersion));

        builder.Entity<IdpCommunitySession>()
            .HasOne(session => session.IdpPlan)
            .WithMany(plan => plan.CommunitySessions)
            .HasForeignKey(session => session.IdpPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCommunitySession>()
            .HasOne(session => session.Ward)
            .WithMany()
            .HasForeignKey(session => session.WardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpCommunitySession>().HasIndex(session => session.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpCommunitySession>().Property(session => session.RowVersion));

        builder.Entity<IdpCommunityNeed>()
            .HasOne(need => need.IdpCommunitySession)
            .WithMany(session => session.CommunityNeeds)
            .HasForeignKey(need => need.IdpCommunitySessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpCommunityNeed>().HasIndex(need => need.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpCommunityNeed>().Property(need => need.RowVersion));

        builder.Entity<IdpWardInput>()
            .HasIndex(wardInput => new { wardInput.IdpPlanId, wardInput.WardId })
            .IsUnique();

        builder.Entity<IdpWardInput>()
            .HasOne(wardInput => wardInput.IdpPlan)
            .WithMany()
            .HasForeignKey(wardInput => wardInput.IdpPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpWardInput>()
            .HasOne(wardInput => wardInput.Ward)
            .WithMany()
            .HasForeignKey(wardInput => wardInput.WardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpWardInput>().HasIndex(wardInput => wardInput.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpWardInput>().Property(wardInput => wardInput.RowVersion));

        builder.Entity<IdpStakeholderEngagement>()
            .HasOne(engagement => engagement.IdpCommunitySession)
            .WithMany(session => session.StakeholderEngagements)
            .HasForeignKey(engagement => engagement.IdpCommunitySessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpStakeholderEngagement>().HasIndex(engagement => engagement.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpStakeholderEngagement>().Property(engagement => engagement.RowVersion));

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpStrategicObjective)
            .WithMany(objective => objective.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpRiskLink>().HasIndex(riskLink => riskLink.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpRiskLink>().Property(riskLink => riskLink.RowVersion));

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
        builder.Entity<IdpBudgetSnapshot>().HasIndex(snapshot => snapshot.PublicId).IsUnique();

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
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(comment => comment.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpCollaborationComment>().HasIndex(comment => comment.PublicId).IsUnique();

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.CommentedByUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommentedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.IdpPlan)
            .WithMany()
            .HasForeignKey(task => task.IdpPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(task => task.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdpTaskAssignment>().HasIndex(task => task.PublicId).IsUnique();
        ConfigureRowVersion(builder.Entity<IdpTaskAssignment>().Property(task => task.RowVersion));

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
            typeof(Department), typeof(Unit), typeof(Position), typeof(Ward), typeof(VoteNumber), typeof(OpmsTarget), typeof(OpmsTargetWard), typeof(OpmsTargetAdditionalAssignee), typeof(OpmsTargetVoteNumber), typeof(OpmsKpiBudgetSource), typeof(IpmsTarget), typeof(IpmsTargetOpmsLink), typeof(IpmsKpiBudgetSource), typeof(OpmsSubmission), typeof(IpmsSubmission),
            typeof(DueDateExtension), typeof(ReviewComment), typeof(AuditFinding), typeof(SubmissionScore),
            typeof(MunicipalEmployee), typeof(EmployeeAssignment), typeof(MunicipalityFinancialYear), typeof(SdbipLayer), typeof(OpmsImportBatch),
            typeof(PerformancePeriodTarget), typeof(PerformanceTargetRevision), typeof(KpiFieldRevision), typeof(LegacySubmissionValueArchive), typeof(LegacySubmissionPeriodReconciliation), typeof(LegacyTenantOwnershipReconciliation)
            , typeof(MunicipalityConsolidationPolicy), typeof(PerformanceSuggestionEvent)
            , typeof(WorkflowDefinition), typeof(WorkflowStageDefinition), typeof(SubmissionWorkflowInstance), typeof(SubmissionWorkflowAction),
            typeof(PerformanceRfi), typeof(PerformanceRfiEvidence), typeof(ReportingWindow), typeof(ReportingWindowException), typeof(RatingScheme), typeof(RatingSchemeValue), typeof(SubmissionStageRating), typeof(LegacySubmissionRatingReconciliation)
            , typeof(EvidenceBlob), typeof(PoeFile), typeof(PoeEvidenceAssessment), typeof(PoeEvidenceReplacement), typeof(PoeLegalHoldEvent), typeof(PoeDisposalEvent), typeof(Notification), typeof(AuditTrail), typeof(BusinessEventOutbox), typeof(NotificationDeliveryAttempt), typeof(IdempotencyRequest), typeof(IdpPlan), typeof(IdpImportBatch), typeof(GovernedRecordLifecycleEvent), typeof(TechnicalIndicatorDescription), typeof(TidSourceDocument), typeof(StrategicDocumentType), typeof(StrategicDocument), typeof(StrategicDocumentEvent),
            typeof(C88CatalogueVersion), typeof(C88MunicipalityConfiguration), typeof(C88CatalogueItem), typeof(C88Indicator), typeof(C88DataElement), typeof(C88IndicatorApplicability), typeof(C88ComplianceQuestion), typeof(C88IndicatorPlan), typeof(C88ReportingCalendar), typeof(C88IndicatorReport), typeof(C88DataElementValue), typeof(C88ComplianceResponse), typeof(C88Assignment), typeof(C88WorkflowDefinition), typeof(C88WorkflowStage), typeof(C88WorkflowAction), typeof(C88OpmsMapping),
            typeof(AuthenticationConfiguration), typeof(AuthenticationPolicy), typeof(UserAuthenticator), typeof(AuthenticationEvent),
            typeof(InternalAuditAssessmentConfiguration), typeof(InternalAuditAssessment), typeof(OfficialReportTemplate), typeof(OfficialReportGeneration), typeof(OfficialReportGenerationScopeGrant), typeof(OfficialReportSchedule), typeof(OfficialReportJob),
            typeof(NotificationConfiguration), typeof(NotificationScheduleRule), typeof(WorkingCalendarHoliday), typeof(ScheduledNotification), typeof(NotificationPreference),
            typeof(MunicipalityNationalKpa), typeof(MunicipalityBackToBasicsPillar),
            typeof(MunicipalKpa), typeof(MunicipalStrategicGoal), typeof(StrategicIntervention), typeof(MunicipalStrategicObjective), typeof(PerformanceObjective),
            typeof(MunicipalKpaStrategicGoal), typeof(StrategicGoalIntervention), typeof(StrategicGoalObjective), typeof(StrategicInterventionObjective), typeof(StrategicObjectivePerformanceObjective)
        };
        foreach (var entry in ChangeTracker.Entries().Where(item => protectedTypes.Contains(item.Entity.GetType()) && item.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is AuditTrail { MunicipalityId: null, EntityName: nameof(FinancialYear) } && _tenantContext.IsSystem)
                continue;
            if (!tenantId.HasValue || tenantId == long.MinValue) throw new UnauthorizedAccessException("A municipality context is required for this operation.");
            var property = entry.Property("MunicipalityId");
            var current = property.CurrentValue == null ? (long?)null : Convert.ToInt64(property.CurrentValue);
            var original = property.OriginalValue == null ? (long?)null : Convert.ToInt64(property.OriginalValue);
            if (_tenantOwnershipReconciliationActive && entry.State == EntityState.Modified && !original.HasValue && current == tenantId
                && TenantOwnershipReconciliationTypes.Contains(entry.Entity.GetType())
                && entry.Properties.Where(item => item.IsModified).All(item => item.Metadata.Name == "MunicipalityId" || item.Metadata.IsConcurrencyToken))
                continue;
            if (entry.State == EntityState.Added && (!current.HasValue || current.Value == 0))
            {
                property.CurrentValue = tenantId.Value;
                current = tenantId;
            }
            if (current != tenantId || (entry.State != EntityState.Added && original != tenantId))
                throw new UnauthorizedAccessException("Cross-municipality writes are not permitted.");
        }
    }

    private static readonly HashSet<Type> TenantOwnershipReconciliationTypes =
    [
        typeof(Department), typeof(Unit), typeof(OpmsTarget), typeof(IpmsTarget), typeof(OpmsSubmission), typeof(IpmsSubmission)
    ];

    public IDisposable BeginTenantOwnershipReconciliation()
    {
        if (_tenantContext is not { IsSystem: true, MunicipalityId: > 0 })
            throw new UnauthorizedAccessException("System scope and a selected municipality are required for tenant-ownership reconciliation.");
        if (_tenantOwnershipReconciliationActive)
            throw new InvalidOperationException("A tenant-ownership reconciliation scope is already active.");
        _tenantOwnershipReconciliationActive = true;
        return new ReconciliationScope(this);
    }

    private sealed class ReconciliationScope(ApplicationDbContext context) : IDisposable
    {
        private ApplicationDbContext? _context = context;
        public void Dispose()
        {
            if (_context == null) return;
            _context._tenantOwnershipReconciliationActive = false;
            _context = null;
        }
    }

    private void EnforceAppendOnlyRecords()
    {
        EnforceRetiredLegacyProjectionHistory();
        EnforceRolePermissionHistory();
        EnforceSecurityRoleHistory();
        EnforceSecurityRegistryHistory();
        EnforcePermissionProjectionHistory();
        EnforceMunicipalityRootHistory();
        EnforceApplicationUserRootHistory();
        EnforceTenantCalendarHistory();
        EnforceOrganizationMasterHistory();
        EnforceMunicipalEmployeeHistory();
        EnforceStrategicDocumentTypeHistory();
        EnforceStrategicRiskHistory();
        EnforceStrategicRiskTreatmentHistory();
        EnforceMunicipalityConsolidationPolicyHistory();
        EnforcePerformancePeriodTargetHistory();
        EnforceNotificationHistory();
        EnforceNotificationPreferenceHistory();
        EnforceRefreshTokenHistory();
        EnforceIdempotencyRequestHistory();
        EnforcePerformanceTargetRootHistory();
        EnforcePerformanceSubmissionRootHistory();
        EnforceOpmsTargetRelationshipHistory<OpmsTargetWard>();
        EnforceOpmsTargetRelationshipHistory<OpmsTargetAdditionalAssignee>();
        EnforceOpmsTargetRelationshipHistory<OpmsTargetVoteNumber>();
        EnforceKpiBudgetSourceHistory<OpmsKpiBudgetSource>(nameof(OpmsKpiBudgetSource.OpmsTargetId));
        EnforceKpiBudgetSourceHistory<IpmsKpiBudgetSource>(nameof(IpmsKpiBudgetSource.IpmsTargetId));
        EnforceIpmsOpmsTargetLinkHistory();
        RejectRewrites<PerformanceCalculationTypeDefinition>("Canonical performance-calculation type definitions are migration-managed and immutable.");
        RejectRewrites<OpmsUnitDefinition>("Canonical OPMS unit definitions are migration-managed and immutable.");
        RejectRewrites<PerformanceDirectionDefinition>("Canonical performance-direction definitions are migration-managed and immutable.");
        RejectRewrites<OpmsTargetTemplateVersion>("OPMS target-template version history is append-only.");
        RejectRewrites<IpmsTargetTemplateVersion>("IPMS target-template version history is append-only.");
        EnforceOpmsTargetTemplateHistory();
        EnforceIpmsTargetTemplateHistory();
        EnforceEvidenceLifecycle();
        RejectRewrites<IdpDocument>("IDP document identity, plan/version association, file metadata, retention, approval and upload evidence are immutable; upload a new governed document version instead.");
        EnforceIdpPlanHistory();
        EnforceImportBatchHistory();
        EnforceIdpDefinitionHistory();
        foreach (var entry in ChangeTracker.Entries<AuthenticationConfiguration>().Where(entry => entry.State == EntityState.Added))
        {
            var row = entry.Entity;
            if (row.VersionNumber < 1 || row.ConfigurationFamilyPublicId == Guid.Empty || !row.IsCurrent
                || row.VersionNumber == 1 && row.PreviousVersionId.HasValue
                || row.VersionNumber > 1 && !row.PreviousVersionId.HasValue)
                throw new InvalidOperationException("Authentication configuration versions require a valid family, sequence, current marker, and predecessor lineage.");
            if (row.VersionNumber > 1 && !ChangeTracker.Entries<AuthenticationConfiguration>().Any(candidate =>
                    candidate.State == EntityState.Modified && candidate.Entity.Id == row.PreviousVersionId))
                throw new InvalidOperationException("An authentication configuration successor must close its tracked predecessor in the same unit of work.");
        }
        foreach (var entry in ChangeTracker.Entries<AuthenticationConfiguration>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Authentication configuration-version history cannot be hard deleted.");

            EnsureOnlyProperties(entry,
                [nameof(AuthenticationConfiguration.IsCurrent), nameof(AuthenticationConfiguration.EffectiveTo),
                    nameof(AuthenticationConfiguration.ModifiedByUserId), nameof(AuthenticationConfiguration.ModifiedAt),
                    nameof(AuthenticationConfiguration.RowVersion)],
                "Authentication configuration versions are append-preserved; definition changes require a successor version.");
            var currentEnd = entry.CurrentValues.GetValue<DateTime?>(nameof(AuthenticationConfiguration.EffectiveTo));
            var successors = ChangeTracker.Entries<AuthenticationConfiguration>()
                .Where(candidate => candidate.State == EntityState.Added && candidate.Entity.PreviousVersionId == entry.Entity.Id)
                .Select(candidate => candidate.Entity)
                .ToArray();
            if (!entry.OriginalValues.GetValue<bool>(nameof(AuthenticationConfiguration.IsCurrent))
                || entry.CurrentValues.GetValue<bool>(nameof(AuthenticationConfiguration.IsCurrent))
                || entry.OriginalValues.GetValue<DateTime?>(nameof(AuthenticationConfiguration.EffectiveTo)).HasValue
                || !currentEnd.HasValue
                || successors.Length != 1
                || successors[0].MunicipalityId != entry.OriginalValues.GetValue<long>(nameof(AuthenticationConfiguration.MunicipalityId))
                || successors[0].ConfigurationFamilyPublicId != entry.OriginalValues.GetValue<Guid>(nameof(AuthenticationConfiguration.ConfigurationFamilyPublicId))
                || successors[0].VersionNumber != entry.OriginalValues.GetValue<int>(nameof(AuthenticationConfiguration.VersionNumber)) + 1
                || successors[0].EffectiveFrom != currentEnd.Value.AddTicks(1)
                || successors[0].CreatedByUserId != entry.CurrentValues.GetValue<string?>(nameof(AuthenticationConfiguration.ModifiedByUserId))
                || successors[0].CreatedAt != entry.CurrentValues.GetValue<DateTime?>(nameof(AuthenticationConfiguration.ModifiedAt)))
                throw new InvalidOperationException("An authentication configuration may be closed only by its exact chronological, actor-stamped successor version.");
        }
        if (ChangeTracker.Entries<AuthenticationPolicy>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Authentication policy history is append-only; create a successor configuration version instead.");
        foreach (var entry in ChangeTracker.Entries<UserAuthenticator>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Enterprise-authenticator identity and lifecycle history cannot be hard deleted.");

            EnsureOnlyProperties(entry,
                [nameof(UserAuthenticator.Issuer), nameof(UserAuthenticator.Subject), nameof(UserAuthenticator.ExternalIdentityHash),
                    nameof(UserAuthenticator.LinkedAt), nameof(UserAuthenticator.LinkedByUserId), nameof(UserAuthenticator.IsActive),
                    nameof(UserAuthenticator.LastAuthenticatedAt), nameof(UserAuthenticator.DisabledByUserId), nameof(UserAuthenticator.DisabledAt),
                    nameof(UserAuthenticator.RowVersion)],
                "Enterprise-authenticator municipality, user, provider, expected email and creation evidence are immutable.");
            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name).Where(name => name != nameof(UserAuthenticator.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            var binding = new[] { nameof(UserAuthenticator.Issuer), nameof(UserAuthenticator.Subject), nameof(UserAuthenticator.ExternalIdentityHash),
                nameof(UserAuthenticator.LinkedAt), nameof(UserAuthenticator.LinkedByUserId) };
            var status = new[] { nameof(UserAuthenticator.IsActive), nameof(UserAuthenticator.DisabledByUserId), nameof(UserAuthenticator.DisabledAt) };
            var bindingChanged = binding.Any(changed.Contains);
            var statusChanged = status.Any(changed.Contains);
            var authenticatedChanged = changed.Contains(nameof(UserAuthenticator.LastAuthenticatedAt));

            if (bindingChanged)
            {
                if (statusChanged || !authenticatedChanged || binding.Any(property => !changed.Contains(property))
                    || binding.Any(property => entry.OriginalValues[property] != null)
                    || binding.Any(property => entry.CurrentValues[property] == null)
                    || entry.CurrentValues.GetValue<DateTime?>(nameof(UserAuthenticator.LinkedAt))
                        < entry.OriginalValues.GetValue<DateTime>(nameof(UserAuthenticator.CreatedAt)))
                    throw new InvalidOperationException("An enterprise identity may be bound exactly once, atomically with its first successful authentication evidence.");
            }

            if (authenticatedChanged)
            {
                var currentAuthentication = entry.CurrentValues.GetValue<DateTime?>(nameof(UserAuthenticator.LastAuthenticatedAt));
                var originalAuthentication = entry.OriginalValues.GetValue<DateTime?>(nameof(UserAuthenticator.LastAuthenticatedAt));
                var hasEvent = ChangeTracker.Entries<AuthenticationEvent>().Any(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                    && candidate.Entity.UserId == entry.Entity.UserId
                    && candidate.Entity.ProviderCode == entry.Entity.ProviderRegistrationCode
                    && candidate.Entity.EventType == "ExternalSignIn" && candidate.Entity.Success);
                if (statusChanged || !currentAuthentication.HasValue
                    || originalAuthentication.HasValue && currentAuthentication <= originalAuthentication
                    || !hasEvent)
                    throw new InvalidOperationException("Enterprise-authenticator last-use evidence may advance only with its append-only successful authentication event.");
            }

            if (statusChanged)
            {
                var originalActive = entry.OriginalValues.GetValue<bool>(nameof(UserAuthenticator.IsActive));
                var currentActive = entry.CurrentValues.GetValue<bool>(nameof(UserAuthenticator.IsActive));
                var expectedAction = currentActive ? "EnterpriseIdentityEnabled" : "EnterpriseIdentityDisabled";
                var hasAudit = ChangeTracker.Entries<AuditTrail>().Any(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                    && candidate.Entity.EntityName == "UserAuthenticator"
                    && candidate.Entity.EntityId == entry.Entity.PublicId.ToString()
                    && candidate.Entity.Action == expectedAction);
                if (bindingChanged || authenticatedChanged || originalActive == currentActive
                    || status.Any(property => !changed.Contains(property))
                    || currentActive && (entry.CurrentValues.GetValue<string?>(nameof(UserAuthenticator.DisabledByUserId)) != null
                        || entry.CurrentValues.GetValue<DateTime?>(nameof(UserAuthenticator.DisabledAt)).HasValue)
                    || !currentActive && (string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(UserAuthenticator.DisabledByUserId)))
                        || !entry.CurrentValues.GetValue<DateTime?>(nameof(UserAuthenticator.DisabledAt)).HasValue)
                    || !hasAudit)
                    throw new InvalidOperationException("Enterprise-authenticator status changes require a complete state tuple and append-only governance audit evidence.");
            }

            if (!bindingChanged && !authenticatedChanged && !statusChanged)
                throw new InvalidOperationException("Enterprise-authenticator history cannot be rewritten.");
        }
        RejectRewrites<StrategicRiskAssessment>("Strategic-risk assessment history is append-only.");
        RejectRewrites<StrategicRiskReview>("Strategic-risk review history is append-only.");
        foreach (var entry in ChangeTracker.Entries<OpmsKpiStrategicRisk>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Strategic-risk KPI relationship history cannot be hard deleted.");

            var originalActive = entry.OriginalValues.GetValue<bool>(nameof(OpmsKpiStrategicRisk.IsActive));
            var currentActive = entry.CurrentValues.GetValue<bool>(nameof(OpmsKpiStrategicRisk.IsActive));
            var originalPrimary = entry.OriginalValues.GetValue<bool>(nameof(OpmsKpiStrategicRisk.IsPrimary));
            var currentPrimary = entry.CurrentValues.GetValue<bool>(nameof(OpmsKpiStrategicRisk.IsPrimary));
            if (originalActive && currentActive && originalPrimary && !currentPrimary)
            {
                EnsureOnlyProperties(entry,
                    [nameof(OpmsKpiStrategicRisk.IsPrimary), nameof(OpmsKpiStrategicRisk.RowVersion)],
                    "Strategic-risk KPI relationship identity and provenance are append-preserved.");
                RequireGovernanceAudit(nameof(OpmsKpiStrategicRisk), entry.Entity.PublicId, entry.Entity.MunicipalityId, "DemotePrimary");
                continue;
            }

            EnsureOnlyProperties(entry,
                [nameof(OpmsKpiStrategicRisk.IsActive), nameof(OpmsKpiStrategicRisk.IsPrimary),
                    nameof(OpmsKpiStrategicRisk.UnlinkedByUserId), nameof(OpmsKpiStrategicRisk.UnlinkedAt),
                    nameof(OpmsKpiStrategicRisk.UnlinkReason), nameof(OpmsKpiStrategicRisk.RowVersion)],
                "Strategic-risk KPI relationship identity and provenance are append-preserved.");
            if (!originalActive || currentActive || currentPrimary
                || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(OpmsKpiStrategicRisk.UnlinkedByUserId)))
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsKpiStrategicRisk.UnlinkedAt)).HasValue
                || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(OpmsKpiStrategicRisk.UnlinkReason))))
                throw new InvalidOperationException("A strategic-risk KPI relationship may only advance once to a complete inactive unlink state.");
            RequireGovernanceAudit(nameof(OpmsKpiStrategicRisk), entry.Entity.PublicId, entry.Entity.MunicipalityId, "Unlink");
        }
        var strategicMasterEntries = ChangeTracker.Entries()
            .Where(entry => entry.Entity is StrategicPlanningMasterBase)
            .ToArray();
        if (strategicMasterEntries.Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Strategic-planning master history cannot be hard deleted.");
        foreach (var entry in strategicMasterEntries.Where(entry => entry.State == EntityState.Modified))
        {
            var entity = (StrategicPlanningMasterBase)entry.Entity;
            RequireGovernanceAudit(entry.Metadata.ClrType.Name, entity.PublicId, entity.MunicipalityId);
        }
        if (ChangeTracker.Entries<NationalKpa>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<BackToBasicsPillar>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<MunicipalityNationalKpa>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<MunicipalityBackToBasicsPillar>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Global strategic-reference history cannot be hard deleted.");
        foreach (var entry in ChangeTracker.Entries<NationalKpa>().Where(entry => entry.State == EntityState.Modified))
            RequireGovernanceAudit(nameof(NationalKpa), entry.Entity.PublicId, _tenantContext?.MunicipalityId);
        foreach (var entry in ChangeTracker.Entries<BackToBasicsPillar>().Where(entry => entry.State == EntityState.Modified))
            RequireGovernanceAudit(nameof(BackToBasicsPillar), entry.Entity.PublicId, _tenantContext?.MunicipalityId);
        foreach (var entry in ChangeTracker.Entries<MunicipalityNationalKpa>().Where(entry => entry.State == EntityState.Modified))
            RequireGovernanceAudit(nameof(MunicipalityNationalKpa), entry.Entity.PublicId, entry.Entity.MunicipalityId, "UpdateAvailability");
        foreach (var entry in ChangeTracker.Entries<MunicipalityBackToBasicsPillar>().Where(entry => entry.State == EntityState.Modified))
            RequireGovernanceAudit(nameof(MunicipalityBackToBasicsPillar), entry.Entity.PublicId, entry.Entity.MunicipalityId, "UpdateAvailability");
        if (ChangeTracker.Entries<DueDateExtension>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<ReviewComment>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<AuditFinding>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<SubmissionScore>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Submission extension, review, finding, and score ledgers are append-only.");
        foreach (var group in ChangeTracker.Entries<DueDateExtension>().Where(entry => entry.State == EntityState.Added)
                     .GroupBy(entry => new { entry.Entity.SubmissionKind, entry.Entity.SubmissionId, entry.Entity.MunicipalityId }))
            RequireGovernanceAudit(SubmissionEntityName(group.Key.SubmissionKind), group.Key.SubmissionId,
                group.Key.MunicipalityId, "ExtendDueDate", requireOldValue: true, requiredAuditCount: group.Count());
        foreach (var group in ChangeTracker.Entries<SubmissionScore>().Where(entry => entry.State == EntityState.Added)
                     .GroupBy(entry => new { entry.Entity.SubmissionKind, entry.Entity.SubmissionId, entry.Entity.MunicipalityId }))
            RequireGovernanceAudit(SubmissionEntityName(group.Key.SubmissionKind), group.Key.SubmissionId,
                group.Key.MunicipalityId, "Score", requireOldValue: false, requiredAuditCount: group.Count());
        if (ChangeTracker.Entries<UserScope>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<UserAssignment>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("User scope and operational-assignment history cannot be hard deleted.");
        foreach (var entry in ChangeTracker.Entries<UserScope>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureMonotonicClosure(entry, nameof(UserScope.IsActive), nameof(UserScope.EffectiveTo), nameof(UserScope.EffectiveFrom),
                [nameof(UserScope.IsActive), nameof(UserScope.EffectiveTo), nameof(UserScope.RowVersion)],
                "User scope history is append-preserved; only an active scope may be closed.");
        }
        foreach (var entry in ChangeTracker.Entries<UserAssignment>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureMonotonicClosure(entry, nameof(UserAssignment.IsActive), nameof(UserAssignment.ValidToUtc), nameof(UserAssignment.ValidFromUtc),
                [nameof(UserAssignment.IsActive), nameof(UserAssignment.ValidToUtc), nameof(UserAssignment.RowVersion)],
                "Operational-assignment history is append-preserved; only an active assignment may be closed.", requireEndDate: false);
        }
        foreach (var entry in ChangeTracker.Entries<SecurityUserRoleAssignment>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Role-assignment history cannot be hard deleted.");
            EnsureMonotonicClosure(entry, nameof(SecurityUserRoleAssignment.IsActive), nameof(SecurityUserRoleAssignment.EffectiveTo), nameof(SecurityUserRoleAssignment.EffectiveFrom),
                [nameof(SecurityUserRoleAssignment.IsActive), nameof(SecurityUserRoleAssignment.EffectiveTo), nameof(SecurityUserRoleAssignment.RevokedAt), nameof(SecurityUserRoleAssignment.RevokedBy), nameof(SecurityUserRoleAssignment.RowVersion)],
                "Role-assignment history is append-preserved; only an active assignment may be revoked.");
            if (entry.OriginalValues.GetValue<DateTime?>(nameof(SecurityUserRoleAssignment.RevokedAt)).HasValue
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(SecurityUserRoleAssignment.RevokedAt)).HasValue
                || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(SecurityUserRoleAssignment.RevokedBy))))
                throw new InvalidOperationException("Role-assignment revocation requires an immutable actor and timestamp.");
        }
        foreach (var entry in ChangeTracker.Entries<EmployeeAssignment>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Employee-assignment history cannot be hard deleted.");
            EnsureMonotonicClosure(entry, nameof(EmployeeAssignment.IsActive), nameof(EmployeeAssignment.EffectiveTo), nameof(EmployeeAssignment.EffectiveFrom),
                [nameof(EmployeeAssignment.IsActive), nameof(EmployeeAssignment.EffectiveTo), nameof(EmployeeAssignment.RowVersion)],
                "Employee-assignment history is append-preserved; only an active assignment may be closed.");
        }
        if (ChangeTracker.Entries<PerformanceTargetRevision>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Performance target revision history is append-only.");
        if (ChangeTracker.Entries<KpiFieldRevision>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("KPI field revision history is append-only.");
        if (ChangeTracker.Entries<PerformanceSuggestionEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Performance suggestion history is append-only.");
        if (ChangeTracker.Entries<SubmissionWorkflowAction>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Submission workflow action history is append-only.");
        foreach (var entry in ChangeTracker.Entries<SubmissionWorkflowInstance>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Submission workflow-instance history cannot be hard deleted.");

            EnsureOnlyProperties(entry,
                [nameof(SubmissionWorkflowInstance.CurrentStageId), nameof(SubmissionWorkflowInstance.State),
                    nameof(SubmissionWorkflowInstance.NextSequence), nameof(SubmissionWorkflowInstance.CompletedAt),
                    nameof(SubmissionWorkflowInstance.RowVersion)],
                "Submission workflow identity and pinned definition are immutable; only governed lifecycle state may advance.");

            var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToHashSet(StringComparer.Ordinal);
            var originalSequence = entry.OriginalValues.GetValue<int>(nameof(SubmissionWorkflowInstance.NextSequence));
            var currentSequence = entry.CurrentValues.GetValue<int>(nameof(SubmissionWorkflowInstance.NextSequence));
            var appendedActions = ChangeTracker.Entries<SubmissionWorkflowAction>()
                .Where(action => action.State == EntityState.Added
                    && (ReferenceEquals(action.Entity.SubmissionWorkflowInstance, entry.Entity)
                        || entry.Entity.Id != 0 && action.Entity.SubmissionWorkflowInstanceId == entry.Entity.Id))
                .Select(action => action.Entity)
                .OrderBy(action => action.Sequence)
                .ToArray();

            if (!changed.Contains(nameof(SubmissionWorkflowInstance.NextSequence))
                || currentSequence <= originalSequence
                || currentSequence != originalSequence + appendedActions.Length
                || !appendedActions.Select(action => action.Sequence).SequenceEqual(Enumerable.Range(originalSequence, appendedActions.Length)))
                throw new InvalidOperationException("Workflow action sequence may only advance by the exact append-only actions saved with the instance.");

            var originalState = entry.OriginalValues.GetValue<WorkflowInstanceState>(nameof(SubmissionWorkflowInstance.State));
            var currentState = entry.CurrentValues.GetValue<WorkflowInstanceState>(nameof(SubmissionWorkflowInstance.State));
            var originalStage = entry.OriginalValues.GetValue<long?>(nameof(SubmissionWorkflowInstance.CurrentStageId));
            var currentStage = entry.CurrentValues.GetValue<long?>(nameof(SubmissionWorkflowInstance.CurrentStageId));
            var originalCompletedAt = entry.OriginalValues.GetValue<DateTime?>(nameof(SubmissionWorkflowInstance.CompletedAt));
            var currentCompletedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(SubmissionWorkflowInstance.CompletedAt));
            var lifecycleChanged = changed.Contains(nameof(SubmissionWorkflowInstance.State))
                || changed.Contains(nameof(SubmissionWorkflowInstance.CurrentStageId))
                || changed.Contains(nameof(SubmissionWorkflowInstance.CompletedAt));

            if (originalState is WorkflowInstanceState.Completed or WorkflowInstanceState.Cancelled && lifecycleChanged)
                throw new InvalidOperationException("A terminal workflow instance cannot be reopened or have its completion evidence rewritten.");

            var lastAction = appendedActions[^1];
            if (lifecycleChanged)
            {
                if (lastAction.ToStageId != currentStage
                    || currentState is WorkflowInstanceState.Completed or WorkflowInstanceState.Cancelled
                        && (currentStage.HasValue || !currentCompletedAt.HasValue || currentCompletedAt != lastAction.OccurredAt)
                    || currentState is WorkflowInstanceState.Active or WorkflowInstanceState.Rework
                        && (!currentStage.HasValue || currentCompletedAt.HasValue))
                    throw new InvalidOperationException("Workflow lifecycle state must match the final appended action and cannot be reversed.");
            }
            else if (currentState != originalState || currentStage != originalStage || currentCompletedAt != originalCompletedAt
                || appendedActions.Any(action => action.FromStageId != originalStage || action.ToStageId != originalStage))
            {
                throw new InvalidOperationException("Non-transition workflow actions must remain pinned to the current stage.");
            }
        }
        if (ChangeTracker.Entries<SubmissionStageRating>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Submission stage rating history is append-only.");
        if (ChangeTracker.Entries<LegacySubmissionRatingReconciliation>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Legacy submission rating reconciliation evidence is append-only.");
        if (ChangeTracker.Entries<LegacySubmissionPeriodReconciliation>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Legacy submission period reconciliation evidence is append-only.");
        if (ChangeTracker.Entries<LegacyTenantOwnershipReconciliation>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Legacy tenant-ownership reconciliation evidence is append-only.");
        if (ChangeTracker.Entries<InternalAuditAssessment>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Internal Audit assessment history is append-only.");
        foreach (var entry in ChangeTracker.Entries<InternalAuditAssessmentConfiguration>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Internal Audit configuration history cannot be hard deleted.");
            EnsureMonotonicClosure(entry, nameof(InternalAuditAssessmentConfiguration.IsCurrent), nameof(InternalAuditAssessmentConfiguration.EffectiveTo),
                nameof(InternalAuditAssessmentConfiguration.EffectiveFrom),
                [nameof(InternalAuditAssessmentConfiguration.IsCurrent), nameof(InternalAuditAssessmentConfiguration.EffectiveTo), nameof(InternalAuditAssessmentConfiguration.RowVersion)],
                "Internal Audit configuration versions are append-preserved; only the current predecessor may be closed by a successor.");
        }
        if (ChangeTracker.Entries<WorkflowStageDefinition>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Workflow stage-definition versions are append-only; create a successor workflow version.");
        foreach (var entry in ChangeTracker.Entries<WorkflowDefinition>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Workflow definition history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(WorkflowDefinition.IsActive), nameof(WorkflowDefinition.EffectiveTo), nameof(WorkflowDefinition.RowVersion)],
                "Workflow definitions are append-preserved; definition changes require a successor version.");
            var originalActive = entry.OriginalValues.GetValue<bool>(nameof(WorkflowDefinition.IsActive));
            var currentActive = entry.CurrentValues.GetValue<bool>(nameof(WorkflowDefinition.IsActive));
            var originalEnd = entry.OriginalValues.GetValue<DateTime?>(nameof(WorkflowDefinition.EffectiveTo));
            var currentEnd = entry.CurrentValues.GetValue<DateTime?>(nameof(WorkflowDefinition.EffectiveTo));
            var effectiveFrom = entry.OriginalValues.GetValue<DateTime>(nameof(WorkflowDefinition.EffectiveFrom));
            if (!originalActive || !currentEnd.HasValue || currentEnd < effectiveFrom
                || originalEnd.HasValue && currentEnd > originalEnd
                || !currentActive && !entry.Property(nameof(WorkflowDefinition.IsActive)).IsModified)
                throw new InvalidOperationException("Workflow lifecycle may only close or retire an active version without extending its effective period.");
        }
        if (ChangeTracker.Entries<ReportingWindow>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Reporting-window history is append-only; create a new governed period configuration instead of rewriting evidence.");
        if (ChangeTracker.Entries<ReportingWindowException>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Reporting-window exception approvals are append-only.");
        if (ChangeTracker.Entries<RatingScheme>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Rating-scheme definitions are append-only; create a new scheme instead of rewriting assessment evidence.");
        if (ChangeTracker.Entries<RatingSchemeValue>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Rating-scheme value bands are append-only.");
        foreach (var entry in ChangeTracker.Entries<OfficialReportTemplate>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Official report template-version history cannot be hard deleted.");

            EnsureOnlyProperties(entry,
                [nameof(OfficialReportTemplate.IsCurrent), nameof(OfficialReportTemplate.EffectiveTo), nameof(OfficialReportTemplate.RowVersion)],
                "Official report template versions are append-preserved; definition changes require a successor version.");
            var originalEnd = entry.OriginalValues.GetValue<DateTime?>(nameof(OfficialReportTemplate.EffectiveTo));
            var currentEnd = entry.CurrentValues.GetValue<DateTime?>(nameof(OfficialReportTemplate.EffectiveTo));
            var successors = ChangeTracker.Entries<OfficialReportTemplate>()
                .Where(candidate => candidate.State == EntityState.Added && candidate.Entity.PreviousVersionId == entry.Entity.Id)
                .Select(candidate => candidate.Entity)
                .ToArray();
            if (!entry.OriginalValues.GetValue<bool>(nameof(OfficialReportTemplate.IsCurrent))
                || entry.CurrentValues.GetValue<bool>(nameof(OfficialReportTemplate.IsCurrent))
                || originalEnd.HasValue
                || !currentEnd.HasValue
                || currentEnd < entry.OriginalValues.GetValue<DateTime>(nameof(OfficialReportTemplate.EffectiveFrom))
                || successors.Length != 1
                || successors[0].TemplateFamilyPublicId != entry.OriginalValues.GetValue<Guid>(nameof(OfficialReportTemplate.TemplateFamilyPublicId))
                || successors[0].VersionNumber != entry.OriginalValues.GetValue<int>(nameof(OfficialReportTemplate.VersionNumber)) + 1
                || successors[0].EffectiveFrom != currentEnd.Value.AddTicks(1))
                throw new InvalidOperationException("An official report template may be closed only by its exact chronological successor version.");
        }
        if (ChangeTracker.Entries<OfficialReportGeneration>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<OfficialReportGenerationScopeGrant>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Official report templates and generated report history are append-only.");
        foreach (var entry in ChangeTracker.Entries<OfficialReportSchedule>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Official report schedule versions cannot be deleted.");
            EnsureOnlyProperties(entry,
                [nameof(OfficialReportSchedule.IsCurrent), nameof(OfficialReportSchedule.IsActive), nameof(OfficialReportSchedule.EffectiveTo),
                    nameof(OfficialReportSchedule.NextRunAt), nameof(OfficialReportSchedule.RowVersion)],
                "Official report schedule versions are append-preserved; configuration changes require a successor version.");
            var originalNext = entry.OriginalValues.GetValue<DateTime?>(nameof(OfficialReportSchedule.NextRunAt));
            var currentNext = entry.CurrentValues.GetValue<DateTime?>(nameof(OfficialReportSchedule.NextRunAt));
            var originalEnd = entry.OriginalValues.GetValue<DateTime?>(nameof(OfficialReportSchedule.EffectiveTo));
            var currentEnd = entry.CurrentValues.GetValue<DateTime?>(nameof(OfficialReportSchedule.EffectiveTo));
            if (!entry.OriginalValues.GetValue<bool>(nameof(OfficialReportSchedule.IsCurrent)) && entry.CurrentValues.GetValue<bool>(nameof(OfficialReportSchedule.IsCurrent))
                || !entry.OriginalValues.GetValue<bool>(nameof(OfficialReportSchedule.IsActive)) && entry.CurrentValues.GetValue<bool>(nameof(OfficialReportSchedule.IsActive))
                || originalNext.HasValue && currentNext.HasValue && currentNext < originalNext
                || originalEnd.HasValue && currentEnd != originalEnd)
                throw new InvalidOperationException("Official report schedule lifecycle cannot be reversed or moved backwards.");
        }
        foreach (var entry in ChangeTracker.Entries<OfficialReportJob>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Official report job history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(OfficialReportJob.OfficialReportGenerationId), nameof(OfficialReportJob.DistributionOutboxId), nameof(OfficialReportJob.State),
                    nameof(OfficialReportJob.AvailableAt), nameof(OfficialReportJob.AttemptCount), nameof(OfficialReportJob.StartedAt),
                    nameof(OfficialReportJob.CompletedAt), nameof(OfficialReportJob.LastError), nameof(OfficialReportJob.RetryReason), nameof(OfficialReportJob.RowVersion)],
                "Official report job request identity is immutable; only execution and delivery lifecycle state may change.");
        }
        if (ChangeTracker.Entries<NotificationScheduleRule>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<WorkingCalendarHoliday>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Notification policy rules and working-calendar history cannot be deleted or rewritten.");
        foreach (var entry in ChangeTracker.Entries<NotificationConfiguration>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Notification policy version history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(NotificationConfiguration.Lifecycle), nameof(NotificationConfiguration.DeliveryPaused), nameof(NotificationConfiguration.EffectiveTo),
                    nameof(NotificationConfiguration.ActivatedByUserId), nameof(NotificationConfiguration.ActivatedAt), nameof(NotificationConfiguration.RowVersion)],
                "Notification policy versions are append-preserved; definition changes require a successor version.");

            var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToHashSet(StringComparer.Ordinal);
            var originalLifecycle = entry.OriginalValues.GetValue<NotificationPolicyLifecycle>(nameof(NotificationConfiguration.Lifecycle));
            var currentLifecycle = entry.CurrentValues.GetValue<NotificationPolicyLifecycle>(nameof(NotificationConfiguration.Lifecycle));
            if (originalLifecycle != currentLifecycle)
            {
                var activating = originalLifecycle == NotificationPolicyLifecycle.Draft && currentLifecycle == NotificationPolicyLifecycle.Active;
                var superseding = originalLifecycle == NotificationPolicyLifecycle.Active && currentLifecycle == NotificationPolicyLifecycle.Superseded;
                if (!activating && !superseding)
                    throw new InvalidOperationException("Notification policy lifecycle transitions cannot be reversed or bypassed.");

                if (activating && (entry.OriginalValues.GetValue<DateTime?>(nameof(NotificationConfiguration.ActivatedAt)).HasValue
                    || !entry.CurrentValues.GetValue<DateTime?>(nameof(NotificationConfiguration.ActivatedAt)).HasValue
                    || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(NotificationConfiguration.ActivatedByUserId)))))
                    throw new InvalidOperationException("Notification policy activation requires an immutable actor and timestamp.");

                if (superseding)
                {
                    var originalEnd = entry.OriginalValues.GetValue<DateTime?>(nameof(NotificationConfiguration.EffectiveTo));
                    var currentEnd = entry.CurrentValues.GetValue<DateTime?>(nameof(NotificationConfiguration.EffectiveTo));
                    var effectiveFrom = entry.OriginalValues.GetValue<DateTime>(nameof(NotificationConfiguration.EffectiveFrom));
                    if (!currentEnd.HasValue || currentEnd < effectiveFrom || originalEnd.HasValue && currentEnd > originalEnd)
                        throw new InvalidOperationException("Notification policy supersession may only close the active version without extending its effective period.");
                }
            }
            else if (changed.Contains(nameof(NotificationConfiguration.EffectiveTo))
                || changed.Contains(nameof(NotificationConfiguration.ActivatedAt))
                || changed.Contains(nameof(NotificationConfiguration.ActivatedByUserId)))
                throw new InvalidOperationException("Notification policy lifecycle evidence may only be written during activation or supersession.");

            if (changed.Contains(nameof(NotificationConfiguration.DeliveryPaused))
                && (originalLifecycle != NotificationPolicyLifecycle.Active || currentLifecycle != NotificationPolicyLifecycle.Active))
                throw new InvalidOperationException("Only an active notification policy may change operational delivery state.");
        }
        foreach (var entry in ChangeTracker.Entries<ScheduledNotification>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Scheduled notification history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(ScheduledNotification.State), nameof(ScheduledNotification.QueuedAt), nameof(ScheduledNotification.NotificationId),
                    nameof(ScheduledNotification.BusinessEventOutboxId), nameof(ScheduledNotification.StateReason), nameof(ScheduledNotification.RowVersion)],
                "Scheduled notification source, recipient, deadline, policy, rule and idempotency identity are immutable.");
            var originalState = entry.OriginalValues.GetValue<ScheduledNotificationState>(nameof(ScheduledNotification.State));
            var currentState = entry.CurrentValues.GetValue<ScheduledNotificationState>(nameof(ScheduledNotification.State));
            if (originalState != ScheduledNotificationState.Pending
                || currentState is not (ScheduledNotificationState.Queued or ScheduledNotificationState.Superseded or ScheduledNotificationState.Skipped)
                || currentState == ScheduledNotificationState.Queued && !entry.CurrentValues.GetValue<DateTime?>(nameof(ScheduledNotification.QueuedAt)).HasValue
                || currentState is ScheduledNotificationState.Superseded or ScheduledNotificationState.Skipped
                    && string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(ScheduledNotification.StateReason))))
                throw new InvalidOperationException("Scheduled notification lifecycle can only advance once from pending to queued, superseded, or skipped.");
        }
        if (ChangeTracker.Entries<PerformanceRfiEvidence>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("RFI evidence provenance is append-only.");
        foreach (var entry in ChangeTracker.Entries<PerformanceRfi>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("RFI history cannot be hard deleted.");

            EnsureOnlyProperties(entry,
                [nameof(PerformanceRfi.Response), nameof(PerformanceRfi.RespondedByUserId), nameof(PerformanceRfi.RespondedAt),
                    nameof(PerformanceRfi.ClosedByUserId), nameof(PerformanceRfi.ClosedAt), nameof(PerformanceRfi.RowVersion)],
                "RFI source, scope, question, raiser and due-date evidence are immutable; only response and closure lifecycle state may advance.");

            var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToHashSet(StringComparer.Ordinal);
            var responseProperties = new[] { nameof(PerformanceRfi.Response), nameof(PerformanceRfi.RespondedByUserId), nameof(PerformanceRfi.RespondedAt) };
            var closureProperties = new[] { nameof(PerformanceRfi.ClosedByUserId), nameof(PerformanceRfi.ClosedAt) };
            var responseChanged = responseProperties.Any(changed.Contains);
            var closureChanged = closureProperties.Any(changed.Contains);

            if (responseChanged)
            {
                if (closureChanged
                    || responseProperties.Any(property => !changed.Contains(property))
                    || entry.OriginalValues.GetValue<string?>(nameof(PerformanceRfi.Response)) != null
                    || entry.OriginalValues.GetValue<string?>(nameof(PerformanceRfi.RespondedByUserId)) != null
                    || entry.OriginalValues.GetValue<DateTime?>(nameof(PerformanceRfi.RespondedAt)).HasValue
                    || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(PerformanceRfi.Response)))
                    || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(PerformanceRfi.RespondedByUserId)))
                    || !entry.CurrentValues.GetValue<DateTime?>(nameof(PerformanceRfi.RespondedAt)).HasValue
                    || entry.CurrentValues.GetValue<DateTime?>(nameof(PerformanceRfi.RespondedAt)) < entry.OriginalValues.GetValue<DateTime>(nameof(PerformanceRfi.RaisedAt)))
                    throw new InvalidOperationException("An RFI response may be recorded exactly once with an immutable actor and timestamp.");
            }
            else if (closureChanged)
            {
                if (closureProperties.Any(property => !changed.Contains(property))
                    || !entry.OriginalValues.GetValue<DateTime?>(nameof(PerformanceRfi.RespondedAt)).HasValue
                    || string.IsNullOrWhiteSpace(entry.OriginalValues.GetValue<string?>(nameof(PerformanceRfi.Response)))
                    || entry.OriginalValues.GetValue<string?>(nameof(PerformanceRfi.ClosedByUserId)) != null
                    || entry.OriginalValues.GetValue<DateTime?>(nameof(PerformanceRfi.ClosedAt)).HasValue
                    || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(PerformanceRfi.ClosedByUserId)))
                    || !entry.CurrentValues.GetValue<DateTime?>(nameof(PerformanceRfi.ClosedAt)).HasValue
                    || entry.CurrentValues.GetValue<DateTime?>(nameof(PerformanceRfi.ClosedAt)) < entry.OriginalValues.GetValue<DateTime?>(nameof(PerformanceRfi.RespondedAt)))
                    throw new InvalidOperationException("An RFI may be closed exactly once after response with an immutable actor and timestamp.");
            }
            else
            {
                throw new InvalidOperationException("RFI lifecycle state may only advance through response or closure.");
            }
        }
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
        foreach (var entry in ChangeTracker.Entries<BusinessEventOutbox>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Business-event outbox history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(FTCERP.Host.Domain.Entities.BusinessEventOutbox.AvailableAt), nameof(FTCERP.Host.Domain.Entities.BusinessEventOutbox.ProcessedAt),
                    nameof(FTCERP.Host.Domain.Entities.BusinessEventOutbox.AttemptCount), nameof(FTCERP.Host.Domain.Entities.BusinessEventOutbox.LastError),
                    nameof(FTCERP.Host.Domain.Entities.BusinessEventOutbox.RowVersion)],
                "Business-event identity and payload are immutable; only delivery lifecycle state may change.");
        }
        foreach (var entry in ChangeTracker.Entries<NotificationDeliveryAttempt>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Notification delivery history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(NotificationDeliveryAttempt.Status), nameof(NotificationDeliveryAttempt.AttemptCount), nameof(NotificationDeliveryAttempt.AttemptedAt),
                    nameof(NotificationDeliveryAttempt.DeliveredAt), nameof(NotificationDeliveryAttempt.Error), nameof(NotificationDeliveryAttempt.Provider),
                    nameof(NotificationDeliveryAttempt.ProviderReference), nameof(NotificationDeliveryAttempt.ResponseDetail)],
                "Notification delivery identity is immutable; only delivery-result state may change.");
        }
        if (ChangeTracker.Entries<IdpImportRow>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("IDP import reconciliation rows are append-only.");
        if (ChangeTracker.Entries<IdpChangeLog>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpBudgetSnapshot>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpCollaborationComment>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("IDP change, budget-snapshot, and collaboration-comment history is append-only.");
        if (ChangeTracker.Entries<IdpAnnualTarget>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpAlignmentLink>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpCommunitySession>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpCommunityNeed>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpWardInput>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpStakeholderEngagement>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            || ChangeTracker.Entries<IdpRiskLink>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("IDP annual-target, alignment, participation, stakeholder, and risk-link records are append-only.");
        foreach (var entry in ChangeTracker.Entries<IdpTaskAssignment>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("IDP task history cannot be hard deleted.");
            EnsureOnlyProperties(entry,
                [nameof(IdpTaskAssignment.IsCompleted), nameof(IdpTaskAssignment.CompletedAt), nameof(IdpTaskAssignment.RowVersion)],
                "IDP task identity, assignment, content and due-date evidence are immutable.");
            var originalCompleted = entry.OriginalValues.GetValue<bool>(nameof(IdpTaskAssignment.IsCompleted));
            var currentCompleted = entry.CurrentValues.GetValue<bool>(nameof(IdpTaskAssignment.IsCompleted));
            var originalCompletedAt = entry.OriginalValues.GetValue<DateTime?>(nameof(IdpTaskAssignment.CompletedAt));
            var currentCompletedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(IdpTaskAssignment.CompletedAt));
            if (originalCompleted == currentCompleted
                || currentCompleted && !currentCompletedAt.HasValue
                || !currentCompleted && currentCompletedAt.HasValue
                || originalCompleted && !originalCompletedAt.HasValue)
                throw new InvalidOperationException("IDP task completion must be a complete state transition.");
            RequireGovernanceAudit("IdpTask", entry.Entity.PublicId.ToString(), _tenantContext?.MunicipalityId,
                currentCompleted ? "Complete" : "Reopen", requireOldValue: true);
        }
        if (ChangeTracker.Entries<OpmsImportRow>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("OPMS import reconciliation rows are append-only.");
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
        foreach (var entry in ChangeTracker.Entries<C88MunicipalityConfiguration>().Where(entry => entry.State == EntityState.Modified))
            RequireGovernanceAudit("C88", entry.Entity.PublicId, entry.Entity.MunicipalityId, "Configure");
        foreach (var entry in ChangeTracker.Entries<C88IndicatorPlan>().Where(entry => entry.State == EntityState.Modified))
            RequireGovernanceAudit("C88", entry.Entity.PublicId, entry.Entity.MunicipalityId, "SavePlan");
        if (ChangeTracker.Entries<C88ReportingCalendar>().Any(entry => entry.State == EntityState.Modified)
            || ChangeTracker.Entries<C88Assignment>().Any(entry => entry.State == EntityState.Modified)
            || ChangeTracker.Entries<C88OpmsMapping>().Any(entry => entry.State == EntityState.Modified))
            throw new InvalidOperationException("Circular 88 calendar, assignment, and OPMS-mapping definitions are append-only.");
        foreach (var entry in ChangeTracker.Entries<C88WorkflowDefinition>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(C88WorkflowDefinition.IsCurrent), nameof(C88WorkflowDefinition.IsActive),
                    nameof(C88WorkflowDefinition.EffectiveTo), nameof(C88WorkflowDefinition.RowVersion)],
                "Circular 88 workflow definitions are append-preserved; definition changes require a successor version.");
            var successor = ChangeTracker.Entries<C88WorkflowDefinition>().SingleOrDefault(candidate =>
                candidate.State == EntityState.Added
                && candidate.Entity.PreviousVersionId == entry.Entity.Id
                && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                && candidate.Entity.C88MunicipalityConfigurationId == entry.Entity.C88MunicipalityConfigurationId
                && candidate.Entity.VersionNumber == entry.Entity.VersionNumber + 1
                && candidate.Entity.IsCurrent && candidate.Entity.IsActive);
            var successorAudit = successor == null ? null : ChangeTracker.Entries<AuditTrail>().SingleOrDefault(candidate =>
                candidate.State == EntityState.Added
                && candidate.Entity.EntityName == "C88"
                && candidate.Entity.EntityId == successor.Entity.PublicId.ToString()
                && candidate.Entity.Action == "CreateWorkflowVersion"
                && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && candidate.Entity.NewValue != null);
            if (!entry.OriginalValues.GetValue<bool>(nameof(C88WorkflowDefinition.IsCurrent))
                || !entry.OriginalValues.GetValue<bool>(nameof(C88WorkflowDefinition.IsActive))
                || entry.CurrentValues.GetValue<bool>(nameof(C88WorkflowDefinition.IsCurrent))
                || entry.CurrentValues.GetValue<bool>(nameof(C88WorkflowDefinition.IsActive))
                || successor == null || successorAudit == null
                || successor.Entity.EffectiveFrom <= entry.OriginalValues.GetValue<DateTime>(nameof(C88WorkflowDefinition.EffectiveFrom))
                || entry.CurrentValues.GetValue<DateTime?>(nameof(C88WorkflowDefinition.EffectiveTo)) != successor.Entity.EffectiveFrom)
                throw new InvalidOperationException("A Circular 88 workflow predecessor may only close beside its exact chronological, reason-audited successor version.");
        }
        if (ChangeTracker.Entries<C88CatalogueItem>().Any(entry => entry.State == EntityState.Modified)
            || ChangeTracker.Entries<C88Indicator>().Any(entry => entry.State == EntityState.Modified)
            || ChangeTracker.Entries<C88DataElement>().Any(entry => entry.State == EntityState.Modified)
            || ChangeTracker.Entries<C88IndicatorApplicability>().Any(entry => entry.State == EntityState.Modified)
            || ChangeTracker.Entries<C88ComplianceQuestion>().Any(entry => entry.State == EntityState.Modified))
            throw new InvalidOperationException("Circular 88 catalogue definitions are append-only; create a new catalogue edition.");
        foreach (var entry in ChangeTracker.Entries<C88CatalogueVersion>().Where(entry => entry.State == EntityState.Modified))
        {
            if (entry.OriginalValues.GetValue<bool>(nameof(C88CatalogueVersion.IsPublished)))
                throw new InvalidOperationException("Published Circular 88 catalogue editions are immutable; create a new edition.");
        }
        foreach (var entry in ChangeTracker.Entries<IdpPlanVersion>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("IDP plan-version history is append-only.");
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(IdpPlanVersion.IsActive), nameof(IdpPlanVersion.EffectiveTo), nameof(IdpPlanVersion.RowVersion)
            };
            var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToArray();
            var successor = ChangeTracker.Entries<IdpPlanVersion>().SingleOrDefault(candidate =>
                candidate.State == EntityState.Added
                && (candidate.Entity.IdpPlanId == entry.Entity.IdpPlanId
                    || ReferenceEquals(candidate.Entity.IdpPlan, entry.Entity.IdpPlan))
                && (candidate.Entity.PredecessorVersionId == entry.Entity.Id
                    || ReferenceEquals(candidate.Entity.PredecessorVersion, entry.Entity))
                && candidate.Entity.VersionNumber == entry.Entity.VersionNumber + 1
                && candidate.Entity.IsActive);
            var successorAudit = successor == null ? null : ChangeTracker.Entries<AuditTrail>().SingleOrDefault(candidate =>
                candidate.State == EntityState.Added
                && candidate.Entity.EntityName == nameof(IdpPlanVersion)
                && candidate.Entity.EntityId == successor.Entity.PublicId.ToString()
                && candidate.Entity.Action == "Create"
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && candidate.Entity.NewValue != null);
            if (changed.Any(property => !allowed.Contains(property))
                || !entry.OriginalValues.GetValue<bool>(nameof(IdpPlanVersion.IsActive))
                || entry.CurrentValues.GetValue<bool>(nameof(IdpPlanVersion.IsActive))
                || entry.OriginalValues.GetValue<DateTime?>(nameof(IdpPlanVersion.EffectiveTo)).HasValue
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(IdpPlanVersion.EffectiveTo)).HasValue
                || entry.CurrentValues.GetValue<DateTime?>(nameof(IdpPlanVersion.EffectiveTo))
                    <= entry.OriginalValues.GetValue<DateTime>(nameof(IdpPlanVersion.EffectiveFrom))
                || successor == null
                || successor.Entity.EffectiveFrom != entry.CurrentValues.GetValue<DateTime?>(nameof(IdpPlanVersion.EffectiveTo))
                || successorAudit == null)
                throw new InvalidOperationException("IDP plan versions are append-preserved; an active predecessor may only close beside its exact chronological, reason-audited successor in the same unit of work.");
        }
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

    private void EnforceRetiredLegacyProjectionHistory()
    {
        if (ChangeTracker.Entries<UserPermissionOverride>().Any(entry => entry.State != EntityState.Unchanged))
            throw new InvalidOperationException("Direct user-permission overrides are retired and immutable; use governed, tenant-scoped role assignments and versioned role permissions.");

        RejectRewrites<Period>("Legacy period references are immutable; use governed financial years and reporting periods.");
        RejectRewrites<StrategicGoal>("Legacy strategic-goal references are immutable; use governed municipality strategic-planning masters.");
        RejectRewrites<StrategicObjective>("Legacy strategic-objective references are immutable; use governed municipality strategic-planning masters.");
        RejectRewrites<BudgetSource>("Legacy budget-source references are immutable; use governed municipality budget-source masters.");
        RejectRewrites<BudgetType>("Legacy budget-type references are immutable; use governed municipality budget-type masters.");
        RejectRewrites<UnitOfMeasure>("Legacy unit-of-measure references are immutable; use governed KPI unit and canonical OPMS unit masters.");
    }

    private void EnforceRolePermissionHistory()
    {
        foreach (var entry in ChangeTracker.Entries<RolePermission>())
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                if (row.PublicId == Guid.Empty || string.IsNullOrWhiteSpace(row.RoleId) || row.PermissionId <= 0
                    || row.EffectiveFrom == default || row.EffectiveFrom > DateTime.UtcNow.AddMinutes(5)
                    || row.EffectiveTo.HasValue && row.EffectiveTo < row.EffectiveFrom
                    || !row.IsActive && !row.EffectiveTo.HasValue)
                    throw new InvalidOperationException("Role-permission versions require stable identity, role, permission and an ordered effective interval.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Role-permission history cannot be hard deleted; close the effective version instead.");
            if (entry.State != EntityState.Modified) continue;

            EnsureOnlyProperties(entry,
                [nameof(RolePermission.IsActive), nameof(RolePermission.EffectiveTo), nameof(RolePermission.RowVersion)],
                "Role-permission identity, grant state, scope and effective start are immutable; create a successor version instead.");
            if (!entry.OriginalValues.GetValue<bool>(nameof(RolePermission.IsActive))
                || entry.CurrentValues.GetValue<bool>(nameof(RolePermission.IsActive))
                || entry.OriginalValues.GetValue<DateTime?>(nameof(RolePermission.EffectiveTo)).HasValue
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(RolePermission.EffectiveTo)).HasValue
                || entry.CurrentValues.GetValue<DateTime?>(nameof(RolePermission.EffectiveTo))
                    < entry.OriginalValues.GetValue<DateTime>(nameof(RolePermission.EffectiveFrom)))
                throw new InvalidOperationException("A role-permission version may only close once with an ordered effective end.");
        }
    }

    private void EnforceSecurityRoleHistory()
    {
        foreach (var entry in ChangeTracker.Entries<ApplicationRole>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Dynamic role history cannot be hard deleted; deactivate the role through governed security administration.");

            EnsureOnlyProperties(entry,
                [nameof(ApplicationRole.Name), nameof(ApplicationRole.NormalizedName), nameof(ApplicationRole.Description),
                    nameof(ApplicationRole.IsActive), nameof(ApplicationRole.EffectiveFrom), nameof(ApplicationRole.EffectiveTo),
                    nameof(ApplicationRole.UpdatedAt), nameof(ApplicationRole.ConcurrencyStamp), nameof(ApplicationRole.RowVersion)],
                "Dynamic role identity, tenant ownership, stable code, system classification and creation evidence are immutable.");

            var hasAudit = ChangeTracker.Entries<AuditTrail>().Any(candidate => candidate.State == EntityState.Added
                && candidate.Entity.EntityId == entry.Entity.PublicId.ToString()
                && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && candidate.Entity.NewValue != null
                && ((candidate.Entity.EntityName == "SecurityRole" && candidate.Entity.Action == "Update" && candidate.Entity.OldValue != null)
                    || candidate.Entity.EntityName == "SecurityRolePermissions" && candidate.Entity.Action == "ReplacePermissions"));
            if (!hasAudit)
                throw new InvalidOperationException("Dynamic role changes require same-transaction reasoned audit evidence.");
        }
    }

    private void EnforceSecurityRegistryHistory()
    {
        if (ChangeTracker.Entries<SecurityResource>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<SecurityActionDefinition>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<SecurityMemberDefinition>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<SecurityNavigationItem>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Dynamic security registry history cannot be hard deleted; deactivate the definition instead.");

        foreach (var entry in ChangeTracker.Entries<SecurityResource>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(SecurityResource.Name), nameof(SecurityResource.ResourceType), nameof(SecurityResource.Description),
                    nameof(SecurityResource.SupportsCreate), nameof(SecurityResource.SupportsRead), nameof(SecurityResource.SupportsUpdate),
                    nameof(SecurityResource.SupportsDelete), nameof(SecurityResource.SupportsExport), nameof(SecurityResource.SupportsImport),
                    nameof(SecurityResource.SupportsFieldSecurity), nameof(SecurityResource.SupportsRecordCriteria),
                    nameof(SecurityResource.IsActive), nameof(SecurityResource.RowVersion)],
                "Security-resource public identity, code and implementation bindings are immutable.");
            RequireGovernanceAudit(nameof(SecurityResource), entry.Entity.PublicId, _tenantContext?.MunicipalityId);
        }

        foreach (var entry in ChangeTracker.Entries<SecurityActionDefinition>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(SecurityActionDefinition.Name), nameof(SecurityActionDefinition.Description),
                    nameof(SecurityActionDefinition.IsActive), nameof(SecurityActionDefinition.RowVersion)],
                "Security-action public identity, code and resource binding are immutable.");
            RequireGovernanceAudit(nameof(SecurityActionDefinition), entry.Entity.PublicId, _tenantContext?.MunicipalityId);
        }

        foreach (var entry in ChangeTracker.Entries<SecurityMemberDefinition>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(SecurityMemberDefinition.DisplayName), nameof(SecurityMemberDefinition.IsSensitive),
                    nameof(SecurityMemberDefinition.IsSystemManaged), nameof(SecurityMemberDefinition.IsActive),
                    nameof(SecurityMemberDefinition.RowVersion)],
                "Security-member public identity and resource/member codes are immutable.");
            RequireGovernanceAudit(nameof(SecurityMemberDefinition), entry.Entity.PublicId, _tenantContext?.MunicipalityId);
        }

        foreach (var entry in ChangeTracker.Entries<SecurityNavigationItem>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(SecurityNavigationItem.ParentId), nameof(SecurityNavigationItem.Name), nameof(SecurityNavigationItem.Route),
                    nameof(SecurityNavigationItem.IconKey), nameof(SecurityNavigationItem.DisplayOrder),
                    nameof(SecurityNavigationItem.RequiredPermissionCode), nameof(SecurityNavigationItem.IsActive),
                    nameof(SecurityNavigationItem.RowVersion)],
                "Security-navigation public identity and code are immutable.");
            RequireGovernanceAudit(nameof(SecurityNavigationItem), entry.Entity.PublicId, _tenantContext?.MunicipalityId);
        }
    }

    private void EnforcePermissionProjectionHistory()
    {
        if (ChangeTracker.Entries<Permission>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Permission catalogue history cannot be hard deleted; deactivate the governing security definition instead.");

        foreach (var entry in ChangeTracker.Entries<Permission>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(Permission.Module), nameof(Permission.Feature), nameof(Permission.Action),
                    nameof(Permission.Description), nameof(Permission.ResourceCode), nameof(Permission.Operation),
                    nameof(Permission.MemberCode), nameof(Permission.NavigationCode), nameof(Permission.ActionCode),
                    nameof(Permission.IsActive)],
                "Permission catalogue code and kind are immutable.");

            if (!IsExactTrackedPermissionProjection(entry.Entity))
                throw new InvalidOperationException("Permission catalogue projections may change only to the exact value derived from their governed security registry definition in the same transaction.");
        }
    }

    private bool IsExactTrackedPermissionProjection(Permission permission)
    {
        static bool IsChanged(EntityEntry entry) => entry.State is EntityState.Added or EntityState.Modified;
        var resourceEntry = ChangeTracker.Entries<SecurityResource>().FirstOrDefault(entry => entry.State != EntityState.Deleted
            && entry.Entity.Code == permission.ResourceCode);
        var resourceChanged = resourceEntry != null && IsChanged(resourceEntry);

        return permission.Kind switch
        {
            SecurityPermissionKind.Resource => resourceChanged && IsExactResourcePermission(permission, resourceEntry!.Entity),
            SecurityPermissionKind.Action => ExactActionProjection(permission, resourceEntry?.Entity, resourceChanged),
            SecurityPermissionKind.Member => ExactMemberProjection(permission, resourceEntry?.Entity, resourceChanged),
            SecurityPermissionKind.Navigation => ExactNavigationProjection(permission),
            _ => false
        };
    }

    private bool ExactActionProjection(Permission permission, SecurityResource? resource, bool resourceChanged)
    {
        var actionEntry = ChangeTracker.Entries<SecurityActionDefinition>().FirstOrDefault(entry => entry.State != EntityState.Deleted
            && entry.Entity.Code == (permission.ActionCode ?? permission.Code));
        if (actionEntry == null || resource == null || (!resourceChanged && actionEntry.State is not (EntityState.Added or EntityState.Modified)))
            return false;
        var action = actionEntry.Entity;
        return permission.Code == action.Code && permission.Module == "Action" && permission.Feature == action.ResourceCode
            && permission.Action == action.Code.Split('.').Last() && permission.ResourceCode == action.ResourceCode
            && permission.Operation == SecurityOperation.Execute && permission.MemberCode == null && permission.NavigationCode == null
            && permission.ActionCode == action.Code && permission.Description == (action.Description ?? action.Name)
            && permission.IsActive == (resource.IsActive && action.IsActive);
    }

    private bool ExactMemberProjection(Permission permission, SecurityResource? resource, bool resourceChanged)
    {
        var memberEntry = ChangeTracker.Entries<SecurityMemberDefinition>().FirstOrDefault(entry => entry.State != EntityState.Deleted
            && entry.Entity.ResourceCode == permission.ResourceCode && entry.Entity.MemberCode == permission.MemberCode);
        if (memberEntry == null || resource == null || (!resourceChanged && memberEntry.State is not (EntityState.Added or EntityState.Modified)))
            return false;
        var member = memberEntry.Entity;
        if (permission.Operation is not (SecurityOperation.Read or SecurityOperation.Update)) return false;
        var operation = permission.Operation.Value;
        var expectedActive = resource.IsActive && resource.SupportsFieldSecurity && member.IsActive
            && (operation == SecurityOperation.Read || !member.IsSystemManaged);
        return permission.Code == $"{member.ResourceCode}.{member.MemberCode}.{operation.ToString().ToUpperInvariant()}"
            && permission.Module == "Member" && permission.Feature == member.ResourceCode && permission.Action == operation.ToString()
            && permission.ResourceCode == member.ResourceCode && permission.MemberCode == member.MemberCode
            && permission.NavigationCode == null && permission.ActionCode == null
            && permission.Description == $"{operation} {member.DisplayName}" && permission.IsActive == expectedActive;
    }

    private bool ExactNavigationProjection(Permission permission)
    {
        var navigationEntry = ChangeTracker.Entries<SecurityNavigationItem>().FirstOrDefault(entry => entry.State is EntityState.Added or EntityState.Modified
            && (entry.Entity.Code == permission.NavigationCode || entry.Entity.RequiredPermissionCode == permission.Code));
        if (navigationEntry == null) return false;
        var navigation = navigationEntry.Entity;
        return permission.Code == navigation.RequiredPermissionCode && permission.Module == "Navigation"
            && permission.Feature == navigation.Name && permission.Action == "View" && permission.ResourceCode == null
            && permission.Operation == null && permission.MemberCode == null && permission.NavigationCode == navigation.Code
            && permission.ActionCode == null && permission.Description == $"View {navigation.Name} navigation"
            && permission.IsActive == navigation.IsActive;
    }

    private static bool IsExactResourcePermission(Permission permission, SecurityResource resource)
    {
        if (!permission.Operation.HasValue) return false;
        var operation = permission.Operation.Value;
        var supported = operation switch
        {
            SecurityOperation.Create => resource.SupportsCreate,
            SecurityOperation.Read => resource.SupportsRead,
            SecurityOperation.Update => resource.SupportsUpdate,
            SecurityOperation.Delete => resource.SupportsDelete,
            SecurityOperation.Export => resource.SupportsExport,
            SecurityOperation.Import => resource.SupportsImport,
            _ => false
        };
        return permission.Code == $"{resource.Code}.{operation.ToString().ToUpperInvariant()}"
            && permission.Module == "Resource" && permission.Feature == resource.Code && permission.Action == operation.ToString()
            && permission.ResourceCode == resource.Code && permission.MemberCode == null && permission.NavigationCode == null
            && permission.ActionCode == null && permission.Description == $"{operation} {resource.Name}"
            && permission.IsActive == (resource.IsActive && supported);
    }

    private void EnforceMunicipalityRootHistory()
    {
        if (ChangeTracker.Entries<Municipality>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Municipality tenant-root history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<Municipality>().Where(entry => entry.State == EntityState.Modified))
        {
            var changed = entry.Properties.Where(property => property.IsModified
                    && property.Metadata.Name != nameof(Municipality.RowVersion))
                .Select(property => property.Metadata.Name)
                .ToHashSet(StringComparer.Ordinal);

            if (changed.SetEquals([nameof(Municipality.AuthenticationMode)]))
            {
                var configurations = ChangeTracker.Entries<AuthenticationConfiguration>().Where(candidate =>
                    candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.Id
                    && candidate.Entity.Mode == entry.Entity.AuthenticationMode).ToArray();
                if (configurations.Length != 1)
                    throw new InvalidOperationException("Municipality authentication mode may change only beside its exact reason-audited configuration version.");
                var configuration = configurations[0].Entity;
                var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate =>
                    candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.Id
                    && candidate.Entity.EntityName == "AuthenticationConfiguration"
                    && candidate.Entity.EntityId == configuration.PublicId.ToString()
                    && candidate.Entity.Action is "AuthenticationConfigurationCreated" or "AuthenticationConfigurationVersionCreated"
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && MunicipalityAuthenticationSnapshotMatches(candidate.Entity.NewValue, entry.Entity.AuthenticationMode)).ToArray();
                if (audits.Length != 1)
                    throw new InvalidOperationException("Municipality authentication mode may change only beside its exact reason-audited configuration version.");
                continue;
            }

            if (changed.Count > 0 && changed.All(property => property is nameof(Municipality.TidEnabled) or nameof(Municipality.TidAllKpisRequired)))
            {
                var audit = ChangeTracker.Entries<AuditTrail>().SingleOrDefault(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.Id
                    && candidate.Entity.EntityName == "TidConfiguration"
                    && candidate.Entity.EntityId == entry.Entity.PublicId.ToString()
                    && candidate.Entity.Action == "Update"
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && MunicipalityTidSnapshotMatches(candidate.Entity.OldValue,
                        entry.OriginalValues.GetValue<bool>(nameof(Municipality.TidEnabled)),
                        entry.OriginalValues.GetValue<bool>(nameof(Municipality.TidAllKpisRequired)))
                    && MunicipalityTidSnapshotMatches(candidate.Entity.NewValue,
                        entry.Entity.TidEnabled, entry.Entity.TidAllKpisRequired));
                if (entry.Entity.TidAllKpisRequired && !entry.Entity.TidEnabled || audit == null)
                    throw new InvalidOperationException("Municipality TID settings require a coherent state and exact same-transaction reasoned audit evidence.");
                continue;
            }

            throw new InvalidOperationException("Municipality identity, code, name, activation and effective-date evidence are immutable at the tenant-root persistence boundary.");
        }
    }

    private static bool MunicipalityAuthenticationSnapshotMatches(string? json, AuthenticationMode mode)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!TryGetJsonProperty(document.RootElement, nameof(AuthenticationConfiguration.Mode), out var value)) return false;
            return value.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => value.TryGetInt32(out var numeric) && numeric == (int)mode,
                System.Text.Json.JsonValueKind.String => Enum.TryParse<AuthenticationMode>(value.GetString(), true, out var parsed) && parsed == mode,
                _ => false
            };
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool MunicipalityTidSnapshotMatches(string? json, bool enabled, bool allRequired)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            return TryGetJsonProperty(document.RootElement, nameof(Municipality.TidEnabled), out var enabledValue)
                && enabledValue.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && enabledValue.GetBoolean() == enabled
                && TryGetJsonProperty(document.RootElement, nameof(Municipality.TidAllKpisRequired), out var allRequiredValue)
                && allRequiredValue.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && allRequiredValue.GetBoolean() == allRequired;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool TryGetJsonProperty(System.Text.Json.JsonElement root, string name, out System.Text.Json.JsonElement value)
    {
        if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    private void EnforceApplicationUserRootHistory()
    {
        if (ChangeTracker.Entries<ApplicationUser>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Application-user security-root history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>().Where(entry => entry.State == EntityState.Modified))
        {
            var changed = entry.Properties.Where(property => property.IsModified
                    && property.Metadata.Name != nameof(ApplicationUser.RowVersion)
                    && ApplicationUserValueChanged(property.OriginalValue, property.CurrentValue))
                .Select(property => property.Metadata.Name)
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            var runtimeFields = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(ApplicationUser.PasswordHash), nameof(ApplicationUser.SecurityStamp), nameof(ApplicationUser.ConcurrencyStamp),
                nameof(ApplicationUser.AccessFailedCount), nameof(ApplicationUser.LockoutEnd), nameof(ApplicationUser.LastLoginAt)
            };
            var profileFields = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(ApplicationUser.FirstName), nameof(ApplicationUser.LastName), nameof(ApplicationUser.PhoneNumber), nameof(ApplicationUser.IsActive)
            };
            var metadataFields = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(ApplicationUser.UpdatedAt), nameof(ApplicationUser.UpdatedBy)
            };
            var recognized = runtimeFields.Concat(profileFields).Concat(metadataFields)
                .Append(nameof(ApplicationUser.MustChangePassword)).Append(nameof(ApplicationUser.TwoFactorEnabled))
                .ToHashSet(StringComparer.Ordinal);
            if (!changed.IsSubsetOf(recognized))
                throw new InvalidOperationException("Application-user public identity, tenant, login identity, organisation and creation evidence are immutable.");

            if (changed.Contains(nameof(ApplicationUser.LastLoginAt)))
            {
                var before = entry.OriginalValues.GetValue<DateTime?>(nameof(ApplicationUser.LastLoginAt));
                var after = entry.CurrentValues.GetValue<DateTime?>(nameof(ApplicationUser.LastLoginAt));
                if (!after.HasValue || before.HasValue && after <= before)
                    throw new InvalidOperationException("Application-user last-login evidence may advance only monotonically.");
            }

            var governedProfileFields = changed.Where(profileFields.Contains).ToArray();
            var requiresAdministrationAudit = governedProfileFields.Length > 0 || changed.Any(metadataFields.Contains);
            if (requiresAdministrationAudit)
            {
                var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                    && candidate.Entity.EntityName == nameof(ApplicationUser)
                    && candidate.Entity.EntityId == entry.Entity.PublicId.ToString()
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Action)
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && (!changed.Contains(nameof(ApplicationUser.UpdatedBy))
                        || candidate.Entity.ChangedBy == entry.Entity.UpdatedBy)
                    && (governedProfileFields.Length == 0
                        || ApplicationUserSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues, governedProfileFields)
                            && ApplicationUserSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues, governedProfileFields))).ToArray();
                if (audits.Length != 1)
                    throw new InvalidOperationException("Application-user profile, lifecycle and administration changes require one exact same-transaction reasoned audit.");
            }

            if (changed.Contains(nameof(ApplicationUser.MustChangePassword)))
            {
                var before = entry.OriginalValues.GetValue<bool>(nameof(ApplicationUser.MustChangePassword));
                var after = entry.CurrentValues.GetValue<bool>(nameof(ApplicationUser.MustChangePassword));
                var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                    && candidate.Entity.EntityName == "UserAuthentication"
                    && candidate.Entity.EntityId == entry.Entity.Id
                    && candidate.Entity.Action is "PasswordChange" or "PasswordResetCompleted"
                    && candidate.Entity.ChangedBy == entry.Entity.Id
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && ApplicationUserBooleanSnapshotMatches(candidate.Entity.NewValue, nameof(ApplicationUser.MustChangePassword), after)).ToArray();
                if (!before || after || audits.Length != 1)
                    throw new InvalidOperationException("Application-user password-remediation state may clear only beside its exact authentication audit.");
            }

            if (changed.Contains(nameof(ApplicationUser.TwoFactorEnabled)))
            {
                var after = entry.CurrentValues.GetValue<bool>(nameof(ApplicationUser.TwoFactorEnabled));
                var action = after ? "MfaEnable" : "MfaDisable";
                var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                    && candidate.Entity.EntityName == "UserAuthentication"
                    && candidate.Entity.EntityId == entry.Entity.Id
                    && candidate.Entity.Action == action
                    && candidate.Entity.ChangedBy == entry.Entity.Id
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && ApplicationUserBooleanSnapshotMatches(candidate.Entity.NewValue, nameof(ApplicationUser.TwoFactorEnabled), after)).ToArray();
                if (audits.Length != 1)
                    throw new InvalidOperationException("Application-user MFA state may change only beside its exact authentication audit.");
            }
        }
    }

    private static bool ApplicationUserValueChanged(object? before, object? after) =>
        before is byte[] beforeBytes && after is byte[] afterBytes
            ? !beforeBytes.SequenceEqual(afterBytes)
            : !Equals(before, after);

    private static bool ApplicationUserSnapshotMatches(string? json, PropertyValues values, IEnumerable<string> fields)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            foreach (var field in fields)
            {
                if (!TryGetJsonProperty(document.RootElement, field, out var actual)) return false;
                var expected = values[field];
                if (expected == null)
                {
                    if (actual.ValueKind != System.Text.Json.JsonValueKind.Null) return false;
                }
                else if (expected is bool boolean)
                {
                    if (actual.ValueKind is not (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False)
                        || actual.GetBoolean() != boolean) return false;
                }
                else if (actual.ValueKind != System.Text.Json.JsonValueKind.String
                    || !string.Equals(actual.GetString(), Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool ApplicationUserBooleanSnapshotMatches(string? json, string field, bool expected)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            return TryGetJsonProperty(document.RootElement, field, out var value)
                && value.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && value.GetBoolean() == expected;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private void EnforceTenantCalendarHistory()
    {
        if (ChangeTracker.Entries<FinancialYear>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<MunicipalityFinancialYear>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<ReportingPeriod>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<SdbipLayer>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Tenant calendar and SDBIP master history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<FinancialYear>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(FinancialYear.Code), nameof(FinancialYear.Name), nameof(FinancialYear.StartDate),
                nameof(FinancialYear.EndDate), nameof(FinancialYear.IsActive)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(FinancialYear.RowVersion)],
                "Financial-year identity and public identity are immutable.");
            RequireExactRootAudit(entry, nameof(FinancialYear), entry.OriginalValues.GetValue<Guid>(nameof(FinancialYear.PublicId)),
                null, snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<MunicipalityFinancialYear>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(MunicipalityFinancialYear.FinancialYearId), nameof(MunicipalityFinancialYear.IsCurrent),
                nameof(MunicipalityFinancialYear.IsActive), nameof(MunicipalityFinancialYear.EffectiveFrom),
                nameof(MunicipalityFinancialYear.EffectiveTo)
            };
            EnsureOnlyProperties(entry,
                [nameof(MunicipalityFinancialYear.IsCurrent), nameof(MunicipalityFinancialYear.IsActive),
                    nameof(MunicipalityFinancialYear.EffectiveFrom), nameof(MunicipalityFinancialYear.EffectiveTo),
                    nameof(MunicipalityFinancialYear.RowVersion)],
                "Municipality-financial-year identity, tenant and financial-year association are immutable.");
            RequireExactRootAudit(entry, nameof(MunicipalityFinancialYear),
                entry.OriginalValues.GetValue<Guid>(nameof(MunicipalityFinancialYear.PublicId)),
                entry.OriginalValues.GetValue<long>(nameof(MunicipalityFinancialYear.MunicipalityId)), snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<ReportingPeriod>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(ReportingPeriod.MunicipalityFinancialYearId), nameof(ReportingPeriod.Code), nameof(ReportingPeriod.Name),
                nameof(ReportingPeriod.PeriodType), nameof(ReportingPeriod.Sequence), nameof(ReportingPeriod.StartDate),
                nameof(ReportingPeriod.EndDate), nameof(ReportingPeriod.IsActive)
            };
            EnsureOnlyProperties(entry,
                [nameof(ReportingPeriod.Name), nameof(ReportingPeriod.PeriodType), nameof(ReportingPeriod.Sequence),
                    nameof(ReportingPeriod.StartDate), nameof(ReportingPeriod.EndDate), nameof(ReportingPeriod.IsActive),
                    nameof(ReportingPeriod.RowVersion)],
                "Reporting-period identity, parent financial year and canonical code are immutable.");
            RequireExactRootAudit(entry, nameof(ReportingPeriod),
                entry.OriginalValues.GetValue<Guid>(nameof(ReportingPeriod.PublicId)), _tenantContext?.MunicipalityId, snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<SdbipLayer>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(SdbipLayer.Code), nameof(SdbipLayer.Name), nameof(SdbipLayer.Description),
                nameof(SdbipLayer.DisplayOrder), nameof(SdbipLayer.IsActive), nameof(SdbipLayer.MunicipalityFinancialYearId)
            };
            EnsureOnlyProperties(entry,
                [nameof(SdbipLayer.Code), nameof(SdbipLayer.Name), nameof(SdbipLayer.Description),
                    nameof(SdbipLayer.DisplayOrder), nameof(SdbipLayer.IsActive), nameof(SdbipLayer.UpdatedAt),
                    nameof(SdbipLayer.RowVersion)],
                "SDBIP-layer identity, tenant, financial-year association and creation evidence are immutable.");
            RequireExactRootAudit(entry, nameof(SdbipLayer),
                entry.OriginalValues.GetValue<Guid>(nameof(SdbipLayer.PublicId)),
                entry.OriginalValues.GetValue<long>(nameof(SdbipLayer.MunicipalityId)), snapshotFields);
        }
    }

    private void EnforceOrganizationMasterHistory()
    {
        if (ChangeTracker.Entries<Department>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<Unit>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<Position>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<Ward>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<VoteNumber>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Organization-master history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<Department>().Where(entry => entry.State == EntityState.Modified))
        {
            if (IsExactTenantOwnershipReconciliation(entry, nameof(Department))) continue;
            var snapshotFields = new[]
            {
                nameof(Department.Code), nameof(Department.Name), nameof(Department.Description), nameof(Department.IsActive),
                nameof(Department.EffectiveFrom), nameof(Department.EffectiveTo)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(Department.RowVersion)],
                "Department identity and municipality ownership are immutable outside controlled legacy reconciliation.");
            RequireExactRootAudit(entry, nameof(Department), entry.OriginalValues.GetValue<Guid>(nameof(Department.PublicId)),
                entry.OriginalValues.GetValue<long?>(nameof(Department.MunicipalityId)), snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<Unit>().Where(entry => entry.State == EntityState.Modified))
        {
            if (IsExactTenantOwnershipReconciliation(entry, nameof(Unit))) continue;
            var snapshotFields = new[]
            {
                nameof(Domain.Entities.Unit.DepartmentId), nameof(Domain.Entities.Unit.Code), nameof(Domain.Entities.Unit.Name),
                nameof(Domain.Entities.Unit.IsActive), nameof(Domain.Entities.Unit.EffectiveFrom), nameof(Domain.Entities.Unit.EffectiveTo)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(Domain.Entities.Unit.RowVersion)],
                "Unit identity and municipality ownership are immutable outside controlled legacy reconciliation.");
            RequireExactRootAudit(entry, nameof(Domain.Entities.Unit),
                entry.OriginalValues.GetValue<Guid>(nameof(Domain.Entities.Unit.PublicId)),
                entry.OriginalValues.GetValue<long?>(nameof(Domain.Entities.Unit.MunicipalityId)), snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<Position>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(Position.DepartmentId), nameof(Position.UnitId), nameof(Position.Code), nameof(Position.Name),
                nameof(Position.Grade), nameof(Position.IsActive), nameof(Position.EffectiveFrom), nameof(Position.EffectiveTo)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(Position.RowVersion)],
                "Position identity and municipality ownership are immutable.");
            RequireExactRootAudit(entry, nameof(Position), entry.OriginalValues.GetValue<Guid>(nameof(Position.PublicId)),
                entry.OriginalValues.GetValue<long>(nameof(Position.MunicipalityId)), snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<Ward>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(Ward.Code), nameof(Ward.Name), nameof(Ward.IsActive), nameof(Ward.EffectiveFrom), nameof(Ward.EffectiveTo)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(Ward.RowVersion)],
                "Ward identity, municipality ownership and legacy source evidence are immutable.");
            RequireExactRootAudit(entry, nameof(Ward), entry.OriginalValues.GetValue<Guid>(nameof(Ward.PublicId)),
                entry.OriginalValues.GetValue<long>(nameof(Ward.MunicipalityId)), snapshotFields);
        }

        foreach (var entry in ChangeTracker.Entries<VoteNumber>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(VoteNumber.MunicipalityFinancialYearId), nameof(VoteNumber.DepartmentId), nameof(VoteNumber.Code),
                nameof(VoteNumber.Number), nameof(VoteNumber.Name), nameof(VoteNumber.Amount), nameof(VoteNumber.IsActive),
                nameof(VoteNumber.EffectiveFrom), nameof(VoteNumber.EffectiveTo)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(VoteNumber.RowVersion)],
                "Vote-number identity and municipality ownership are immutable.");
            RequireExactRootAudit(entry, nameof(VoteNumber), entry.OriginalValues.GetValue<Guid>(nameof(VoteNumber.PublicId)),
                entry.OriginalValues.GetValue<long>(nameof(VoteNumber.MunicipalityId)), snapshotFields);
        }
    }

    private void EnforceMunicipalEmployeeHistory()
    {
        if (ChangeTracker.Entries<MunicipalEmployee>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Municipal-employee history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<MunicipalEmployee>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(MunicipalEmployee.EmployeeNumber), nameof(MunicipalEmployee.SalaryReference),
                nameof(MunicipalEmployee.FirstName), nameof(MunicipalEmployee.LastName), nameof(MunicipalEmployee.EmailAddress),
                nameof(MunicipalEmployee.IsActive), nameof(MunicipalEmployee.EffectiveFrom), nameof(MunicipalEmployee.EffectiveTo)
            };
            EnsureOnlyProperties(entry,
                [nameof(MunicipalEmployee.SalaryReference), nameof(MunicipalEmployee.FirstName), nameof(MunicipalEmployee.LastName),
                    nameof(MunicipalEmployee.EmailAddress), nameof(MunicipalEmployee.IdentityUserId), nameof(MunicipalEmployee.IsActive),
                    nameof(MunicipalEmployee.EffectiveFrom), nameof(MunicipalEmployee.EffectiveTo), nameof(MunicipalEmployee.RowVersion)],
                "Municipal-employee identity, employee number and municipality ownership are immutable.");

            var publicId = entry.OriginalValues.GetValue<Guid>(nameof(MunicipalEmployee.PublicId));
            var municipalityId = entry.OriginalValues.GetValue<long>(nameof(MunicipalEmployee.MunicipalityId));
            var originalIdentityPublicId = ResolveIdentityUserPublicId(
                entry.OriginalValues.GetValue<string?>(nameof(MunicipalEmployee.IdentityUserId)));
            var currentIdentityPublicId = ResolveIdentityUserPublicId(
                entry.CurrentValues.GetValue<string?>(nameof(MunicipalEmployee.IdentityUserId)));
            var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                && candidate.Entity.EntityName == nameof(MunicipalEmployee)
                && candidate.Entity.EntityId == publicId.ToString()
                && candidate.Entity.Action == "Update"
                && candidate.Entity.MunicipalityId == municipalityId
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && RootSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues, snapshotFields)
                && RootSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues, snapshotFields)
                && EmployeeIdentitySnapshotMatches(candidate.Entity.OldValue, originalIdentityPublicId)
                && EmployeeIdentitySnapshotMatches(candidate.Entity.NewValue, currentIdentityPublicId)).ToArray();
            if (audits.Length != 1)
                throw new InvalidOperationException("MunicipalEmployee changes require one exact same-transaction reasoned before/after audit.");
        }
    }

    private Guid? ResolveIdentityUserPublicId(string? identityUserId)
    {
        if (string.IsNullOrWhiteSpace(identityUserId)) return null;
        var tracked = ChangeTracker.Entries<ApplicationUser>()
            .Where(candidate => candidate.State is not EntityState.Detached and not EntityState.Deleted
                && candidate.Entity.Id == identityUserId)
            .Select(candidate => (Guid?)candidate.Entity.PublicId).SingleOrDefault();
        return tracked ?? Users.IgnoreQueryFilters().AsNoTracking()
            .Where(candidate => candidate.Id == identityUserId)
            .Select(candidate => (Guid?)candidate.PublicId).SingleOrDefault();
    }

    private static bool EmployeeIdentitySnapshotMatches(string? json, Guid? expectedPublicId)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!TryGetJsonProperty(document.RootElement, "IdentityUserPublicId", out var actual)) return false;
            if (!expectedPublicId.HasValue) return actual.ValueKind == System.Text.Json.JsonValueKind.Null;
            return actual.ValueKind == System.Text.Json.JsonValueKind.String
                && actual.TryGetGuid(out var actualPublicId) && actualPublicId == expectedPublicId.Value;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private void EnforceStrategicDocumentTypeHistory()
    {
        if (ChangeTracker.Entries<StrategicDocumentType>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Strategic-document type history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<StrategicDocumentType>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(StrategicDocumentType.Code), nameof(StrategicDocumentType.Name), nameof(StrategicDocumentType.Description),
                nameof(StrategicDocumentType.AllowsExternalLinks), nameof(StrategicDocumentType.IsActive), nameof(StrategicDocumentType.DisplayOrder)
            };
            EnsureOnlyProperties(entry, [.. snapshotFields, nameof(StrategicDocumentType.RowVersion)],
                "Strategic-document type identity, municipality ownership and creation evidence are immutable.");
            RequireExactRootAudit(entry, nameof(StrategicDocumentType),
                entry.OriginalValues.GetValue<Guid>(nameof(StrategicDocumentType.PublicId)),
                entry.OriginalValues.GetValue<long>(nameof(StrategicDocumentType.MunicipalityId)), snapshotFields);
        }
    }

    private void EnforceStrategicRiskHistory()
    {
        if (ChangeTracker.Entries<StrategicRisk>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Strategic-risk master history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<StrategicRisk>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(StrategicRisk.RiskReference), nameof(StrategicRisk.RiskTitle), nameof(StrategicRisk.RiskDescription),
                nameof(StrategicRisk.IsActive)
            };
            EnsureOnlyProperties(entry,
                [.. snapshotFields, nameof(StrategicRisk.OwnerUserId), nameof(StrategicRisk.EffectiveFromMunicipalityFinancialYearId),
                    nameof(StrategicRisk.EffectiveToMunicipalityFinancialYearId), nameof(StrategicRisk.UpdatedAt),
                    nameof(StrategicRisk.UpdatedByUserId), nameof(StrategicRisk.RowVersion)],
                "Strategic-risk identity, municipality ownership and creation evidence are immutable.");

            var publicId = entry.OriginalValues.GetValue<Guid>(nameof(StrategicRisk.PublicId));
            var municipalityId = entry.OriginalValues.GetValue<long>(nameof(StrategicRisk.MunicipalityId));
            var changedBy = entry.CurrentValues.GetValue<string?>(nameof(StrategicRisk.UpdatedByUserId));
            if (string.IsNullOrWhiteSpace(changedBy)
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(StrategicRisk.UpdatedAt)).HasValue)
                throw new InvalidOperationException("Strategic-risk changes require complete update actor and timestamp evidence.");

            var originalOwnerPublicId = ResolveIdentityUserPublicId(entry.OriginalValues.GetValue<string?>(nameof(StrategicRisk.OwnerUserId)));
            var currentOwnerPublicId = ResolveIdentityUserPublicId(entry.CurrentValues.GetValue<string?>(nameof(StrategicRisk.OwnerUserId)));
            var originalFromYearPublicId = ResolveMunicipalityFinancialYearPublicId(
                entry.OriginalValues.GetValue<long?>(nameof(StrategicRisk.EffectiveFromMunicipalityFinancialYearId)));
            var currentFromYearPublicId = ResolveMunicipalityFinancialYearPublicId(
                entry.CurrentValues.GetValue<long?>(nameof(StrategicRisk.EffectiveFromMunicipalityFinancialYearId)));
            var originalToYearPublicId = ResolveMunicipalityFinancialYearPublicId(
                entry.OriginalValues.GetValue<long?>(nameof(StrategicRisk.EffectiveToMunicipalityFinancialYearId)));
            var currentToYearPublicId = ResolveMunicipalityFinancialYearPublicId(
                entry.CurrentValues.GetValue<long?>(nameof(StrategicRisk.EffectiveToMunicipalityFinancialYearId)));
            var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                && candidate.Entity.EntityName == nameof(StrategicRisk)
                && candidate.Entity.EntityId == publicId.ToString()
                && candidate.Entity.Action == "Update"
                && candidate.Entity.MunicipalityId == municipalityId
                && candidate.Entity.ChangedBy == changedBy
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && RootSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues, snapshotFields)
                && RootSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues, snapshotFields)
                && NullableGuidSnapshotMatches(candidate.Entity.OldValue, "OwnerUserPublicId", originalOwnerPublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.NewValue, "OwnerUserPublicId", currentOwnerPublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.OldValue, "EffectiveFromMunicipalityFinancialYearPublicId", originalFromYearPublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.NewValue, "EffectiveFromMunicipalityFinancialYearPublicId", currentFromYearPublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.OldValue, "EffectiveToMunicipalityFinancialYearPublicId", originalToYearPublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.NewValue, "EffectiveToMunicipalityFinancialYearPublicId", currentToYearPublicId)).ToArray();
            if (audits.Length != 1)
                throw new InvalidOperationException("StrategicRisk changes require one exact same-transaction reasoned before/after audit.");
        }
    }

    private Guid? ResolveMunicipalityFinancialYearPublicId(long? municipalityFinancialYearId)
    {
        if (!municipalityFinancialYearId.HasValue) return null;
        var tracked = ChangeTracker.Entries<MunicipalityFinancialYear>()
            .Where(candidate => candidate.State is not EntityState.Detached and not EntityState.Deleted
                && candidate.Entity.Id == municipalityFinancialYearId.Value)
            .Select(candidate => (Guid?)candidate.Entity.PublicId).SingleOrDefault();
        return tracked ?? MunicipalityFinancialYears.IgnoreQueryFilters().AsNoTracking()
            .Where(candidate => candidate.Id == municipalityFinancialYearId.Value)
            .Select(candidate => (Guid?)candidate.PublicId).SingleOrDefault();
    }

    private static bool NullableGuidSnapshotMatches(string? json, string field, Guid? expected)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!TryGetJsonProperty(document.RootElement, field, out var actual)) return false;
            if (!expected.HasValue) return actual.ValueKind == System.Text.Json.JsonValueKind.Null;
            return actual.ValueKind == System.Text.Json.JsonValueKind.String
                && actual.TryGetGuid(out var actualGuid) && actualGuid == expected.Value;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private void EnforceStrategicRiskTreatmentHistory()
    {
        if (ChangeTracker.Entries<StrategicRiskTreatment>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Strategic-risk treatment history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<StrategicRiskTreatment>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(StrategicRiskTreatment.Title), nameof(StrategicRiskTreatment.Description), nameof(StrategicRiskTreatment.DueAt),
                nameof(StrategicRiskTreatment.Status), nameof(StrategicRiskTreatment.ProgressPercent)
            };
            EnsureOnlyProperties(entry,
                [.. snapshotFields, nameof(StrategicRiskTreatment.OwnerUserId), nameof(StrategicRiskTreatment.UpdatedByUserId),
                    nameof(StrategicRiskTreatment.UpdatedAt), nameof(StrategicRiskTreatment.RowVersion)],
                "Strategic-risk treatment identity, risk association, municipality ownership and creation evidence are immutable.");
            var originalStatus = entry.OriginalValues.GetValue<string>(nameof(StrategicRiskTreatment.Status));
            var currentStatus = entry.CurrentValues.GetValue<string>(nameof(StrategicRiskTreatment.Status));
            var currentProgress = entry.CurrentValues.GetValue<int>(nameof(StrategicRiskTreatment.ProgressPercent));
            if (originalStatus is "COMPLETED" or "CANCELLED")
                throw new InvalidOperationException("A terminal strategic-risk treatment is immutable; create a successor action if more work is required.");
            if (currentStatus == "COMPLETED" && currentProgress != 100)
                throw new InvalidOperationException("A completed strategic-risk treatment must record 100 percent progress.");

            var changedBy = entry.CurrentValues.GetValue<string?>(nameof(StrategicRiskTreatment.UpdatedByUserId));
            if (string.IsNullOrWhiteSpace(changedBy)
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(StrategicRiskTreatment.UpdatedAt)).HasValue)
                throw new InvalidOperationException("Strategic-risk treatment changes require complete update actor and timestamp evidence.");

            var publicId = entry.OriginalValues.GetValue<Guid>(nameof(StrategicRiskTreatment.PublicId));
            var municipalityId = entry.OriginalValues.GetValue<long>(nameof(StrategicRiskTreatment.MunicipalityId));
            var originalOwnerPublicId = ResolveIdentityUserPublicId(
                entry.OriginalValues.GetValue<string?>(nameof(StrategicRiskTreatment.OwnerUserId)));
            var currentOwnerPublicId = ResolveIdentityUserPublicId(
                entry.CurrentValues.GetValue<string?>(nameof(StrategicRiskTreatment.OwnerUserId)));
            var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                && candidate.Entity.EntityName == nameof(StrategicRiskTreatment)
                && candidate.Entity.EntityId == publicId.ToString()
                && candidate.Entity.Action == "Update"
                && candidate.Entity.MunicipalityId == municipalityId
                && candidate.Entity.ChangedBy == changedBy
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && RootSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues, snapshotFields)
                && RootSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues, snapshotFields)
                && NullableGuidSnapshotMatches(candidate.Entity.OldValue, "OwnerUserPublicId", originalOwnerPublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.NewValue, "OwnerUserPublicId", currentOwnerPublicId)).ToArray();
            if (audits.Length != 1)
                throw new InvalidOperationException("StrategicRiskTreatment changes require one exact same-transaction reasoned before/after audit.");
        }
    }

    private void EnforceMunicipalityConsolidationPolicyHistory()
    {
        if (ChangeTracker.Entries<MunicipalityConsolidationPolicy>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Municipality consolidation-policy history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<MunicipalityConsolidationPolicy>().Where(entry => entry.State == EntityState.Modified))
        {
            var snapshotFields = new[]
            {
                nameof(MunicipalityConsolidationPolicy.ConsolidationRule),
                nameof(MunicipalityConsolidationPolicy.MissingValuePolicy),
                nameof(MunicipalityConsolidationPolicy.AllowDerivedTargetOverride),
                nameof(MunicipalityConsolidationPolicy.DeriveAnnualTarget),
                nameof(MunicipalityConsolidationPolicy.IsActive)
            };
            EnsureOnlyProperties(entry,
                [.. snapshotFields, nameof(MunicipalityConsolidationPolicy.ModifiedByUserId),
                    nameof(MunicipalityConsolidationPolicy.ModifiedAt), nameof(MunicipalityConsolidationPolicy.RowVersion)],
                "Municipality consolidation-policy identity, calculation type, municipality ownership and creation evidence are immutable.");

            var changedBy = entry.CurrentValues.GetValue<string?>(nameof(MunicipalityConsolidationPolicy.ModifiedByUserId));
            if (string.IsNullOrWhiteSpace(changedBy)
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(MunicipalityConsolidationPolicy.ModifiedAt)).HasValue)
                throw new InvalidOperationException("Municipality consolidation-policy changes require complete update actor and timestamp evidence.");

            var publicId = entry.OriginalValues.GetValue<Guid>(nameof(MunicipalityConsolidationPolicy.PublicId));
            var municipalityId = entry.OriginalValues.GetValue<long>(nameof(MunicipalityConsolidationPolicy.MunicipalityId));
            var calculationTypePublicId = ResolveCalculationTypePublicId(
                entry.OriginalValues.GetValue<long>(nameof(MunicipalityConsolidationPolicy.CalculationTypeId)));
            var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
                && candidate.Entity.EntityName == nameof(MunicipalityConsolidationPolicy)
                && candidate.Entity.EntityId == publicId.ToString()
                && candidate.Entity.Action == "Edit"
                && candidate.Entity.MunicipalityId == municipalityId
                && candidate.Entity.ChangedBy == changedBy
                && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                && RootSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues, snapshotFields)
                && RootSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues, snapshotFields)
                && NullableGuidSnapshotMatches(candidate.Entity.OldValue, "CalculationTypePublicId", calculationTypePublicId)
                && NullableGuidSnapshotMatches(candidate.Entity.NewValue, "CalculationTypePublicId", calculationTypePublicId)).ToArray();
            if (audits.Length != 1)
                throw new InvalidOperationException("MunicipalityConsolidationPolicy changes require one exact same-transaction reasoned before/after audit.");
        }
    }

    private Guid ResolveCalculationTypePublicId(long calculationTypeId)
    {
        var tracked = ChangeTracker.Entries<PerformanceCalculationTypeDefinition>()
            .Where(candidate => candidate.State is not EntityState.Detached and not EntityState.Deleted
                && candidate.Entity.Id == calculationTypeId)
            .Select(candidate => (Guid?)candidate.Entity.PublicId).SingleOrDefault();
        return tracked ?? PerformanceCalculationTypes.AsNoTracking()
            .Where(candidate => candidate.Id == calculationTypeId)
            .Select(candidate => candidate.PublicId).Single();
    }

    private bool IsExactTenantOwnershipReconciliation<TEntity>(EntityEntry<TEntity> entry, string entityName) where TEntity : class
    {
        if (!_tenantOwnershipReconciliationActive) return false;
        var changed = entry.Properties.Where(property => property.IsModified && !property.Metadata.IsConcurrencyToken)
            .Select(property => property.Metadata.Name).ToArray();
        if (changed.Length != 1 || changed[0] != "MunicipalityId") return false;
        var originalMunicipalityId = entry.OriginalValues["MunicipalityId"] as long?;
        var currentMunicipalityId = entry.CurrentValues["MunicipalityId"] as long?;
        var publicId = entry.OriginalValues.GetValue<Guid>("PublicId");
        if (originalMunicipalityId.HasValue || currentMunicipalityId is not > 0
            || _tenantContext is not { IsSystem: true, MunicipalityId: > 0 }
            || currentMunicipalityId != _tenantContext.MunicipalityId)
            throw new InvalidOperationException("Legacy organization ownership may be assigned only once to the selected municipality.");

        var reconciliations = ChangeTracker.Entries<LegacyTenantOwnershipReconciliation>().Where(candidate =>
            candidate.State == EntityState.Added
            && candidate.Entity.EntityType == entityName
            && candidate.Entity.EntityPublicId == publicId
            && candidate.Entity.MunicipalityId == currentMunicipalityId
            && !string.IsNullOrWhiteSpace(candidate.Entity.SourceEvidence)
            && !string.IsNullOrWhiteSpace(candidate.Entity.ReconciledByUserId)
            && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
            && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId)).ToArray();
        var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
            && candidate.Entity.EntityName == entityName
            && candidate.Entity.EntityId == publicId.ToString()
            && candidate.Entity.Action == "Update"
            && candidate.Entity.MunicipalityId == currentMunicipalityId
            && candidate.Entity.OldValue != null && candidate.Entity.NewValue != null
            && reconciliations.Length == 1
            && candidate.Entity.ChangedBy == reconciliations[0].Entity.ReconciledByUserId
            && candidate.Entity.Reason == reconciliations[0].Entity.Reason).ToArray();
        if (reconciliations.Length != 1 || audits.Length != 1)
            throw new InvalidOperationException("Legacy organization ownership reconciliation requires exact same-transaction provenance and audit evidence.");
        return true;
    }

    private void RequireExactRootAudit<TEntity>(EntityEntry<TEntity> entry, string entityName, Guid publicId,
        long? municipalityId, IReadOnlyCollection<string> snapshotFields) where TEntity : class
    {
        var audits = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
            && candidate.Entity.EntityName == entityName
            && candidate.Entity.EntityId == publicId.ToString()
            && candidate.Entity.Action == "Update"
            && candidate.Entity.MunicipalityId == municipalityId
            && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
            && RootSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues, snapshotFields)
            && RootSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues, snapshotFields)).ToArray();
        if (audits.Length != 1)
            throw new InvalidOperationException($"{entityName} changes require one exact same-transaction reasoned before/after audit.");
    }

    private static bool RootSnapshotMatches(string? json, PropertyValues values, IEnumerable<string> fields)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            foreach (var field in fields)
            {
                if (!TryGetJsonProperty(document.RootElement, field, out var actual)) return false;
                var expectedValue = values[field];
                if (expectedValue == null)
                {
                    if (actual.ValueKind != System.Text.Json.JsonValueKind.Null) return false;
                    continue;
                }

                var expected = System.Text.Json.JsonSerializer.SerializeToElement(expectedValue, expectedValue.GetType());
                if (!System.Text.Json.JsonElement.DeepEquals(actual, expected)) return false;
            }
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private void EnforcePerformancePeriodTargetHistory()
    {
        if (ChangeTracker.Entries<PerformancePeriodTarget>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Canonical performance-period target history cannot be hard deleted; record a governed deactivation revision instead.");

        var allowedProperties = new[]
        {
            nameof(PerformancePeriodTarget.IsTargetRevised), nameof(PerformancePeriodTarget.RevisedTargetValue),
            nameof(PerformancePeriodTarget.RevisedOpmsUnitId), nameof(PerformancePeriodTarget.RevisedUnitKind),
            nameof(PerformancePeriodTarget.PerformanceDirectionId), nameof(PerformancePeriodTarget.Direction),
            nameof(PerformancePeriodTarget.IsBudgetRevised), nameof(PerformancePeriodTarget.RevisedBudgetValue),
            nameof(PerformancePeriodTarget.Description), nameof(PerformancePeriodTarget.IsActive),
            nameof(PerformancePeriodTarget.RowVersion)
        };
        foreach (var entry in ChangeTracker.Entries<PerformancePeriodTarget>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry, allowedProperties,
                "Canonical performance-period target identity, ownership, period, KPI, original values and creation evidence are immutable.");
            foreach (var property in entry.Properties.Where(property => property.IsModified
                         && property.Metadata.Name != nameof(PerformancePeriodTarget.RowVersion)))
            {
                var original = RevisionValue(property.OriginalValue);
                var revised = RevisionValue(property.CurrentValue);
                var matching = ChangeTracker.Entries<PerformanceTargetRevision>().Count(candidate => candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                    && candidate.Entity.PerformancePeriodTargetId == entry.Entity.Id
                    && candidate.Entity.FieldName == property.Metadata.Name
                    && candidate.Entity.OriginalValue == original
                    && candidate.Entity.RevisedValue == revised
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && !string.IsNullOrWhiteSpace(candidate.Entity.ApprovalReference)
                    && !string.IsNullOrWhiteSpace(candidate.Entity.RevisedByUserId));
                if (matching != 1)
                    throw new InvalidOperationException($"Performance-period target field '{property.Metadata.Name}' requires one exact same-transaction revision record.");
            }
        }
    }

    private static string? RevisionValue(object? value) => value switch
    {
        null => null,
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private void EnforceNotificationHistory()
    {
        foreach (var entry in ChangeTracker.Entries<Notification>())
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                var coherentUnread = !row.IsRead && !row.ReadAt.HasValue && row.ReadByUserId == null;
                var coherentRead = row.IsRead && row.ReadAt.HasValue
                    && row.ReadAt >= row.CreatedAt && row.ReadAt <= DateTime.UtcNow.AddMinutes(5)
                    && !string.IsNullOrWhiteSpace(row.ReadByUserId);
                if (string.IsNullOrWhiteSpace(row.Id) || row.PublicId == Guid.Empty || string.IsNullOrWhiteSpace(row.UserId)
                    || string.IsNullOrWhiteSpace(row.Title) || string.IsNullOrWhiteSpace(row.Message)
                    || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                    || (!coherentUnread && !coherentRead))
                    throw new InvalidOperationException("A notification requires immutable recipient, content and creation provenance plus a coherent unread or actor-stamped read lifecycle.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Notification history cannot be hard deleted.");
            if (entry.State != EntityState.Modified) continue;

            EnsureOnlyProperties(entry,
                [nameof(Notification.IsRead), nameof(Notification.ReadAt), nameof(Notification.ReadByUserId), nameof(Notification.RowVersion)],
                "Notification identity, tenant, recipient, content, source and creation evidence are immutable.");
            var readAt = entry.CurrentValues.GetValue<DateTime?>(nameof(Notification.ReadAt));
            var readBy = entry.CurrentValues.GetValue<string?>(nameof(Notification.ReadByUserId));
            if (entry.OriginalValues.GetValue<bool>(nameof(Notification.IsRead))
                || !entry.CurrentValues.GetValue<bool>(nameof(Notification.IsRead))
                || entry.OriginalValues.GetValue<DateTime?>(nameof(Notification.ReadAt)).HasValue
                || entry.OriginalValues.GetValue<string?>(nameof(Notification.ReadByUserId)) != null
                || !readAt.HasValue
                || readAt < entry.OriginalValues.GetValue<DateTime>(nameof(Notification.CreatedAt))
                || readAt > DateTime.UtcNow.AddMinutes(5)
                || string.IsNullOrWhiteSpace(readBy))
                throw new InvalidOperationException("A notification may transition exactly once from unread to read with an ordered timestamp and actor.");
        }
    }

    private void EnforceNotificationPreferenceHistory()
    {
        if (ChangeTracker.Entries<NotificationPreference>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Notification-preference history cannot be hard deleted; save an audited preference update instead.");

        foreach (var entry in ChangeTracker.Entries<NotificationPreference>().Where(entry => entry.State == EntityState.Added))
            RequireNotificationPreferenceAudit(entry, "Create", requireOldSnapshot: false);

        foreach (var entry in ChangeTracker.Entries<NotificationPreference>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(NotificationPreference.EmailEnabled), nameof(NotificationPreference.SmsEnabled),
                    nameof(NotificationPreference.DailyDigestEnabled), nameof(NotificationPreference.WeeklySummaryEnabled),
                    nameof(NotificationPreference.UpdatedAt), nameof(NotificationPreference.RowVersion)],
                "Notification-preference public identity, tenant and owning user are immutable.");
            if (entry.Entity.UpdatedAt <= entry.OriginalValues.GetValue<DateTime>(nameof(NotificationPreference.UpdatedAt)))
                throw new InvalidOperationException("Notification-preference update evidence must advance monotonically.");
            RequireNotificationPreferenceAudit(entry, "Update", requireOldSnapshot: true);
        }
    }

    private void RequireNotificationPreferenceAudit(EntityEntry<NotificationPreference> entry, string action, bool requireOldSnapshot)
    {
        var matches = ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added
            && candidate.Entity.EntityName == nameof(NotificationPreference)
            && candidate.Entity.EntityId == entry.Entity.PublicId.ToString()
            && candidate.Entity.Action == action
            && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
            && candidate.Entity.ChangedBy == entry.Entity.UserId
            && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
            && NotificationPreferenceSnapshotMatches(candidate.Entity.NewValue, entry.CurrentValues)
            && (!requireOldSnapshot || NotificationPreferenceSnapshotMatches(candidate.Entity.OldValue, entry.OriginalValues))
            && (requireOldSnapshot || candidate.Entity.OldValue == null)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"NotificationPreference {action} requires one exact same-transaction reasoned before/after audit record.");
    }

    private static bool NotificationPreferenceSnapshotMatches(string? json, PropertyValues values)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var root = document.RootElement;
            return root.ValueKind == System.Text.Json.JsonValueKind.Object
                && root.TryGetProperty(nameof(NotificationPreference.EmailEnabled), out var email)
                && email.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && email.GetBoolean() == values.GetValue<bool>(nameof(NotificationPreference.EmailEnabled))
                && root.TryGetProperty(nameof(NotificationPreference.SmsEnabled), out var sms)
                && sms.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && sms.GetBoolean() == values.GetValue<bool>(nameof(NotificationPreference.SmsEnabled))
                && root.TryGetProperty(nameof(NotificationPreference.DailyDigestEnabled), out var daily)
                && daily.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && daily.GetBoolean() == values.GetValue<bool>(nameof(NotificationPreference.DailyDigestEnabled))
                && root.TryGetProperty(nameof(NotificationPreference.WeeklySummaryEnabled), out var weekly)
                && weekly.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                && weekly.GetBoolean() == values.GetValue<bool>(nameof(NotificationPreference.WeeklySummaryEnabled));
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private void RejectRewrites<TEntity>(string message) where TEntity : class
    {
        if (ChangeTracker.Entries<TEntity>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException(message);
    }

    private void EnforceRefreshTokenHistory()
    {
        foreach (var entry in ChangeTracker.Entries<RefreshToken>())
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                if (row.PublicId == Guid.Empty || row.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(row.UserId)
                    || row.Token.Length != 64 || row.Token.Any(character => !Uri.IsHexDigit(character))
                    || string.IsNullOrWhiteSpace(row.SecurityStamp) || string.IsNullOrWhiteSpace(row.AuthenticationMethod)
                    || row.ExpiresAt <= row.CreatedAt || row.ExpiresAt > row.AbsoluteExpiresAt
                    || row.LastUsedAt < row.CreatedAt || row.LastUsedAt > row.ExpiresAt
                    || row.RevokedAt.HasValue || row.RevokedByIp != null || row.RevokedReason != null)
                    throw new InvalidOperationException("Refresh-session creation requires a valid immutable identity, digest, security context and bounded lifetime without fabricated revocation evidence.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Refresh-session history cannot be hard deleted.");
            if (entry.State != EntityState.Modified) continue;

            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(RefreshToken.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            var activityProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(RefreshToken.LastUsedAt), nameof(RefreshToken.LastUsedByIp)
            };
            if (changed.IsSubsetOf(activityProperties) && changed.Contains(nameof(RefreshToken.LastUsedAt)))
            {
                if (entry.OriginalValues.GetValue<DateTime?>(nameof(RefreshToken.RevokedAt)).HasValue
                    || entry.CurrentValues.GetValue<DateTime>(nameof(RefreshToken.LastUsedAt))
                        <= entry.OriginalValues.GetValue<DateTime>(nameof(RefreshToken.LastUsedAt))
                    || entry.CurrentValues.GetValue<DateTime>(nameof(RefreshToken.LastUsedAt))
                        > entry.OriginalValues.GetValue<DateTime>(nameof(RefreshToken.ExpiresAt)))
                    throw new InvalidOperationException("Refresh-session activity may only advance monotonically for an unrevoked, unexpired token row.");
                continue;
            }

            var revocationProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(RefreshToken.RevokedAt), nameof(RefreshToken.RevokedByIp), nameof(RefreshToken.RevokedReason)
            };
            if (changed.IsSubsetOf(revocationProperties)
                && changed.Contains(nameof(RefreshToken.RevokedAt))
                && changed.Contains(nameof(RefreshToken.RevokedReason)))
            {
                var revokedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(RefreshToken.RevokedAt));
                if (entry.OriginalValues.GetValue<DateTime?>(nameof(RefreshToken.RevokedAt)).HasValue
                    || !revokedAt.HasValue
                    || revokedAt < entry.OriginalValues.GetValue<DateTime>(nameof(RefreshToken.CreatedAt))
                    || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(RefreshToken.RevokedReason))))
                    throw new InvalidOperationException("Refresh-session revocation is an irreversible actor-context transition with a timestamp and reason.");
                continue;
            }

            throw new InvalidOperationException("Refresh-session identity, token digest, user, session, expiry, security stamp, authentication method and creation evidence are immutable.");
        }
    }

    private void EnforceIdempotencyRequestHistory()
    {
        foreach (var entry in ChangeTracker.Entries<IdempotencyRequest>())
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                var expectedScope = row.MunicipalityId.HasValue ? $"municipality:{row.MunicipalityId.Value}" : "system";
                var expectedIdentityHash = string.IsNullOrWhiteSpace(row.UserId) || string.IsNullOrWhiteSpace(row.Method)
                    || string.IsNullOrWhiteSpace(row.Route) || string.IsNullOrWhiteSpace(row.IdempotencyKey)
                    ? null
                    : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                        $"{expectedScope}\n{row.UserId}\n{row.Method}\n{row.Route}\n{row.IdempotencyKey}")));
                if (row.PublicId == Guid.Empty
                    || row.ScopeKey != expectedScope || row.ScopeKey.Length > 64
                    || row.IdentityHash?.Length != 64 || row.IdentityHash.Any(character => !Uri.IsHexDigit(character))
                    || !string.Equals(row.IdentityHash, expectedIdentityHash, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(row.UserId) || row.UserId.Length > 450
                    || !string.Equals(row.Method, "POST", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(row.Route) || row.Route.Length > 600
                    || string.IsNullOrWhiteSpace(row.IdempotencyKey) || row.IdempotencyKey.Length is < 8 or > 128
                    || row.IdempotencyKey.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.' or ':'))
                    || row.RequestHash?.Length != 64 || row.RequestHash.Any(character => !Uri.IsHexDigit(character))
                    || row.State != IdempotencyRequestStates.InProgress
                    || row.ResponseStatusCode.HasValue
                    || row.ResponseContentType != null
                    || row.ResponseLocation != null
                    || row.ResponseETag != null
                    || row.ResponseBody != null
                    || row.CompletedAt.HasValue
                    || row.ExpiresAt <= row.CreatedAt
                    || string.IsNullOrWhiteSpace(row.CorrelationId) || row.CorrelationId.Length > 100)
                    throw new InvalidOperationException("Idempotency records must begin as a complete, response-free in-progress request identity.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                if (entry.OriginalValues.GetValue<DateTime>(nameof(IdempotencyRequest.ExpiresAt)) > DateTime.UtcNow)
                    throw new InvalidOperationException("Active idempotency replay history cannot be hard deleted before its retention period expires.");
                continue;
            }

            if (entry.State != EntityState.Modified) continue;

            EnsureOnlyProperties(entry,
                [nameof(IdempotencyRequest.State), nameof(IdempotencyRequest.ResponseStatusCode),
                    nameof(IdempotencyRequest.ResponseContentType), nameof(IdempotencyRequest.ResponseLocation),
                    nameof(IdempotencyRequest.ResponseETag), nameof(IdempotencyRequest.ResponseBody),
                    nameof(IdempotencyRequest.CompletedAt), nameof(IdempotencyRequest.RowVersion)],
                "Idempotency request identity, ownership, request digest, retention and correlation evidence are immutable.");

            var originalState = entry.OriginalValues.GetValue<string>(nameof(IdempotencyRequest.State));
            var currentState = entry.CurrentValues.GetValue<string>(nameof(IdempotencyRequest.State));
            var statusCode = entry.CurrentValues.GetValue<int?>(nameof(IdempotencyRequest.ResponseStatusCode));
            var completedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(IdempotencyRequest.CompletedAt));
            var responseBody = entry.CurrentValues.GetValue<byte[]?>(nameof(IdempotencyRequest.ResponseBody));
            if (originalState != IdempotencyRequestStates.InProgress
                || currentState is not (IdempotencyRequestStates.Completed or IdempotencyRequestStates.Failed)
                || statusCode is < 100 or > 599
                || !completedAt.HasValue
                || completedAt.Value < entry.OriginalValues.GetValue<DateTime>(nameof(IdempotencyRequest.CreatedAt))
                || responseBody == null)
                throw new InvalidOperationException("Idempotency request state may advance exactly once from in-progress to a complete persisted response.");

            if (currentState == IdempotencyRequestStates.Failed
                && (statusCode != StatusCodes.Status500InternalServerError
                    || entry.CurrentValues.GetValue<string?>(nameof(IdempotencyRequest.ResponseContentType)) != "application/problem+json"
                    || responseBody.Length == 0
                    || entry.CurrentValues.GetValue<string?>(nameof(IdempotencyRequest.ResponseLocation)) != null
                    || entry.CurrentValues.GetValue<string?>(nameof(IdempotencyRequest.ResponseETag)) != null))
                throw new InvalidOperationException("A failed idempotency request requires its exact terminal problem response evidence.");
        }
    }

    private void EnforcePerformanceTargetRootHistory()
    {
        var entries = ChangeTracker.Entries()
            .Where(entry => entry.Metadata.ClrType == typeof(OpmsTarget) || entry.Metadata.ClrType == typeof(IpmsTarget))
            .ToArray();
        foreach (var entry in entries)
        {
            var entityName = entry.Metadata.ClrType == typeof(OpmsTarget) ? nameof(OpmsTarget) : nameof(IpmsTarget);
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Performance-target history cannot be hard deleted; withdraw the target through its governed lifecycle instead.");
            if (entry.State != EntityState.Modified) continue;

            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(name => name != nameof(OpmsTarget.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            var immutable = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(OpmsTarget.Id), nameof(OpmsTarget.PublicId), nameof(OpmsTarget.MunicipalityId),
                nameof(OpmsTarget.PeriodId), nameof(OpmsTarget.IndicatorNumber), nameof(OpmsTarget.TargetName),
                nameof(OpmsTarget.KpiDescription), nameof(OpmsTarget.AnnualTarget), nameof(OpmsTarget.AnnualTargetDescription),
                nameof(OpmsTarget.TargetUnitType), nameof(OpmsTarget.Q1Target), nameof(OpmsTarget.Q1Description),
                nameof(OpmsTarget.Q1Budget), nameof(OpmsTarget.Q2Target), nameof(OpmsTarget.Q2Description),
                nameof(OpmsTarget.Q2Budget), nameof(OpmsTarget.MidTermTarget), nameof(OpmsTarget.MidTermDescription),
                nameof(OpmsTarget.MidTermBudget), nameof(OpmsTarget.Q3Target), nameof(OpmsTarget.Q3Description),
                nameof(OpmsTarget.Q3Budget), nameof(OpmsTarget.Q3RevisedTarget), nameof(OpmsTarget.Q4Target),
                nameof(OpmsTarget.Q4Description), nameof(OpmsTarget.Q4Budget), nameof(OpmsTarget.Q4RevisedTarget),
                nameof(OpmsTarget.RevisedAnnualTarget), nameof(OpmsTarget.RevisedAnnualBudget), nameof(OpmsTarget.CreatedAt)
            };
            if (entry.Metadata.ClrType == typeof(OpmsTarget))
            {
                immutable.Add(nameof(OpmsTarget.LegacyWardIds));
                immutable.Add(nameof(OpmsTarget.LegacyAdditionalAssigneeIds));
                immutable.Add(nameof(OpmsTarget.LegacyVoteNumberIds));
            }
            if (changed.Overlaps(immutable))
                throw new InvalidOperationException("Performance-target identity, tenant, governed originals and retired wide-period projection are immutable after creation.");

            var originalWithdrawn = entry.OriginalValues.GetValue<bool>(nameof(OpmsTarget.IsWithdrawn));
            if (originalWithdrawn)
                throw new InvalidOperationException("A withdrawn performance target is immutable.");

            var withdrawalProperties = new HashSet<string>(
                [nameof(OpmsTarget.IsWithdrawn), nameof(OpmsTarget.ReasonForWithdrawal), nameof(OpmsTarget.WithdrawnAt), nameof(OpmsTarget.WithdrawnByUserId)],
                StringComparer.Ordinal);
            if (changed.Overlaps(withdrawalProperties))
            {
                if (changed.Except(withdrawalProperties).Any()
                    || !entry.CurrentValues.GetValue<bool>(nameof(OpmsTarget.IsWithdrawn))
                    || entry.OriginalValues.GetValue<string?>(nameof(OpmsTarget.ReasonForWithdrawal)) != null
                    || entry.OriginalValues.GetValue<DateTime?>(nameof(OpmsTarget.WithdrawnAt)).HasValue
                    || entry.OriginalValues.GetValue<string?>(nameof(OpmsTarget.WithdrawnByUserId)) != null)
                    throw new InvalidOperationException("A performance target may be withdrawn exactly once without rewriting its definition.");

                var targetId = entry.OriginalValues.GetValue<string>(nameof(OpmsTarget.Id));
                var municipalityId = entry.OriginalValues.GetValue<long?>(nameof(OpmsTarget.MunicipalityId));
                var reason = entry.CurrentValues.GetValue<string?>(nameof(OpmsTarget.ReasonForWithdrawal));
                var withdrawnAt = entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsTarget.WithdrawnAt));
                var actor = entry.CurrentValues.GetValue<string?>(nameof(OpmsTarget.WithdrawnByUserId));
                var lifecycle = ChangeTracker.Entries<GovernedRecordLifecycleEvent>().Where(candidate =>
                    candidate.State == EntityState.Added
                    && candidate.Entity.AggregateType == entityName
                    && candidate.Entity.AggregateId == targetId
                    && candidate.Entity.MunicipalityId == municipalityId
                    && candidate.Entity.Action == GovernedLifecycleAction.Withdrawn
                    && candidate.Entity.Reason == reason
                    && candidate.Entity.ActorUserId == actor
                    && candidate.Entity.OccurredAt == withdrawnAt
                    && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId)).ToArray();
                if (!municipalityId.HasValue || municipalityId <= 0
                    || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000
                    || string.IsNullOrWhiteSpace(actor) || !withdrawnAt.HasValue
                    || withdrawnAt < entry.OriginalValues.GetValue<DateTime>(nameof(OpmsTarget.CreatedAt))
                    || lifecycle.Length != 1)
                    throw new InvalidOperationException("Performance-target withdrawal requires exact same-transaction tenant, reason, actor, time and lifecycle evidence.");
                continue;
            }

            var definitionProperties = new HashSet<string>(
                [nameof(OpmsTarget.IsIndicatorNumberRevised), nameof(OpmsTarget.RevisedIndicatorNumber),
                    nameof(OpmsTarget.IsTargetNameRevised), nameof(OpmsTarget.RevisedTargetName),
                    nameof(OpmsTarget.IsKpiDescriptionRevised), nameof(OpmsTarget.RevisedKpiDescription)], StringComparer.Ordinal);
            var orderingProperties = new HashSet<string>(
                [nameof(OpmsTarget.OriginalOrderNumber), nameof(OpmsTarget.RevisedOrderNumber)], StringComparer.Ordinal);
            var definitionChanged = changed.Overlaps(definitionProperties);
            var orderingChanged = changed.Overlaps(orderingProperties);
            if (!definitionChanged && !orderingChanged && changed.SetEquals([nameof(OpmsTarget.IsRevised)]))
            {
                var targetId = entry.OriginalValues.GetValue<string>(nameof(OpmsTarget.Id));
                var municipalityId = entry.OriginalValues.GetValue<long?>(nameof(OpmsTarget.MunicipalityId));
                var governedImport = entry.Metadata.ClrType == typeof(OpmsTarget)
                    && ChangeTracker.Entries<OpmsImportBatch>().Any(candidate => candidate.State == EntityState.Modified
                        && candidate.Entity.MunicipalityId == municipalityId
                        && candidate.OriginalValues.GetValue<OpmsImportBatchStatus>(nameof(OpmsImportBatch.Status)) == OpmsImportBatchStatus.Staged
                        && candidate.CurrentValues.GetValue<OpmsImportBatchStatus>(nameof(OpmsImportBatch.Status)) == OpmsImportBatchStatus.Committed);
                var periodIds = PerformancePeriodTargets.IgnoreQueryFilters().AsNoTracking()
                    .Where(period => period.OpmsTargetId == targetId).Select(period => period.Id).ToHashSet();
                var hasPeriodHistory = ChangeTracker.Entries<PerformanceTargetRevision>().Any(candidate => candidate.State == EntityState.Added
                    && (periodIds.Contains(candidate.Entity.PerformancePeriodTargetId)
                        || candidate.Entity.PerformancePeriodTarget?.OpmsTargetId == targetId));
                if (!governedImport || !hasPeriodHistory
                    || entry.OriginalValues.GetValue<bool>(nameof(OpmsTarget.IsRevised))
                    || !entry.CurrentValues.GetValue<bool>(nameof(OpmsTarget.IsRevised)))
                    throw new InvalidOperationException("The KPI revised marker may advance only beside governed import period-revision history.");
                continue;
            }
            if (definitionChanged || orderingChanged)
            {
                var allowed = new HashSet<string>(definitionChanged ? definitionProperties : orderingProperties, StringComparer.Ordinal)
                {
                    nameof(OpmsTarget.IsRevised)
                };
                if (definitionChanged && orderingChanged || changed.Except(allowed).Any())
                    throw new InvalidOperationException("KPI definition and ordering revisions must be recorded as separate governed changes.");
                EnforceExactKpiFieldRevisions(entry, entityName, definitionChanged);
                continue;
            }

            // The parent pointer is governed together with its append-preserved link row below. Let that
            // stricter aggregate invariant validate creation, replacement, unlinking, tenant and actor evidence.
            if (entry.Metadata.ClrType == typeof(IpmsTarget)
                && changed.SetEquals([nameof(IpmsTarget.RelatedOpmsTargetId)]))
                continue;

            RequireGovernanceAudit(entityName, entry.OriginalValues.GetValue<string>(nameof(OpmsTarget.Id)),
                entry.OriginalValues.GetValue<long?>(nameof(OpmsTarget.MunicipalityId)), "Edit", requireOldValue: true);
        }
    }

    private void EnforcePerformanceSubmissionRootHistory()
    {
        var entries = ChangeTracker.Entries()
            .Where(entry => entry.Metadata.ClrType == typeof(OpmsSubmission) || entry.Metadata.ClrType == typeof(IpmsSubmission))
            .ToArray();
        foreach (var entry in entries)
        {
            var isOpms = entry.Metadata.ClrType == typeof(OpmsSubmission);
            var entityName = isOpms ? nameof(OpmsSubmission) : nameof(IpmsSubmission);
            var kind = isOpms ? SubmissionKind.Opms : SubmissionKind.Ipms;
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Performance-submission history cannot be hard deleted; withdraw the submission through its governed lifecycle instead.");
            if (entry.State != EntityState.Modified) continue;

            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(name => name != nameof(OpmsSubmission.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            var submissionId = entry.OriginalValues.GetValue<string>(nameof(OpmsSubmission.Id));
            var originalMunicipalityId = entry.OriginalValues.GetValue<long?>(nameof(OpmsSubmission.MunicipalityId));
            var currentMunicipalityId = entry.CurrentValues.GetValue<long?>(nameof(OpmsSubmission.MunicipalityId));

            if (_tenantOwnershipReconciliationActive
                && changed.SetEquals([nameof(OpmsSubmission.MunicipalityId)])
                && !originalMunicipalityId.HasValue && currentMunicipalityId is > 0
                && ChangeTracker.Entries<LegacyTenantOwnershipReconciliation>().Count(candidate =>
                    candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == currentMunicipalityId
                    && candidate.Entity.EntityType == entityName
                    && candidate.Entity.EntityPublicId == entry.OriginalValues.GetValue<Guid>(nameof(OpmsSubmission.PublicId))
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && !string.IsNullOrWhiteSpace(candidate.Entity.ReconciledByUserId)
                    && candidate.Entity.ReconciledAt != default
                    && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId)) == 1)
                continue;

            var periodChanged = changed.Contains(nameof(OpmsSubmission.ReportingPeriodId));
            if (periodChanged)
            {
                var allowed = new HashSet<string>(
                    [nameof(OpmsSubmission.ReportingPeriodId), nameof(OpmsSubmission.UpdatedBy), nameof(OpmsSubmission.UpdatedOn)],
                    StringComparer.Ordinal);
                var originalPeriodId = entry.OriginalValues.GetValue<long?>(nameof(OpmsSubmission.ReportingPeriodId));
                var currentPeriodId = entry.CurrentValues.GetValue<long?>(nameof(OpmsSubmission.ReportingPeriodId));
                var reconciliations = ChangeTracker.Entries<LegacySubmissionPeriodReconciliation>().Count(candidate =>
                    candidate.State == EntityState.Added
                    && candidate.Entity.MunicipalityId == originalMunicipalityId
                    && candidate.Entity.SubmissionKind == kind
                    && (isOpms ? candidate.Entity.OpmsSubmissionId == submissionId && candidate.Entity.IpmsSubmissionId == null
                        : candidate.Entity.IpmsSubmissionId == submissionId && candidate.Entity.OpmsSubmissionId == null)
                    && candidate.Entity.ReportingPeriodId == currentPeriodId
                    && candidate.Entity.ReconciledByUserId == entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.UpdatedBy))
                    && candidate.Entity.ReconciledAt == entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsSubmission.UpdatedOn))
                    && !string.IsNullOrWhiteSpace(candidate.Entity.SourceQuarterSnapshot)
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                    && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId));
                if (changed.Except(allowed).Any() || originalPeriodId.HasValue || currentPeriodId is not > 0 || reconciliations != 1)
                    throw new InvalidOperationException("A legacy submission reporting period may be assigned exactly once with matching append-only reconciliation evidence.");
                continue;
            }

            var immutable = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(OpmsSubmission.Id), nameof(OpmsSubmission.PublicId), nameof(OpmsSubmission.MunicipalityId),
                nameof(OpmsSubmission.OpmsTargetId), nameof(OpmsSubmission.Quarter), nameof(OpmsSubmission.CreatedBy),
                nameof(OpmsSubmission.CreatedOn), nameof(OpmsSubmission.CreatedAt), nameof(OpmsSubmission.OrganisationId)
            };
            immutable.Add(isOpms ? nameof(OpmsSubmission.OpmsTargetId) : nameof(IpmsSubmission.IpmsTargetId));
            if (changed.Overlaps(immutable))
                throw new InvalidOperationException("Performance-submission identity, tenant, target, legacy period label and creation evidence are immutable after creation.");

            if (entry.OriginalValues.GetValue<bool>(nameof(OpmsSubmission.IsDisabled)))
                throw new InvalidOperationException("A withdrawn performance submission is immutable.");

            var withdrawalProperties = new HashSet<string>(
                [nameof(OpmsSubmission.IsDisabled), nameof(OpmsSubmission.WithdrawalReason),
                    nameof(OpmsSubmission.WithdrawnAt), nameof(OpmsSubmission.WithdrawnByUserId),
                    nameof(OpmsSubmission.UpdatedBy), nameof(OpmsSubmission.UpdatedOn)], StringComparer.Ordinal);
            if (changed.Overlaps(withdrawalProperties.Take(4)))
            {
                var reason = entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.WithdrawalReason));
                var withdrawnAt = entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsSubmission.WithdrawnAt));
                var actor = entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.WithdrawnByUserId));
                var lifecycleCount = ChangeTracker.Entries<GovernedRecordLifecycleEvent>().Count(candidate =>
                    candidate.State == EntityState.Added
                    && candidate.Entity.AggregateType == entityName
                    && candidate.Entity.AggregateId == submissionId
                    && candidate.Entity.MunicipalityId == originalMunicipalityId
                    && candidate.Entity.Action == GovernedLifecycleAction.Withdrawn
                    && candidate.Entity.Reason == reason
                    && candidate.Entity.ActorUserId == actor
                    && candidate.Entity.OccurredAt == withdrawnAt
                    && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId));
                if (changed.Except(withdrawalProperties).Any()
                    || !entry.CurrentValues.GetValue<bool>(nameof(OpmsSubmission.IsDisabled))
                    || entry.OriginalValues.GetValue<string?>(nameof(OpmsSubmission.WithdrawalReason)) != null
                    || entry.OriginalValues.GetValue<DateTime?>(nameof(OpmsSubmission.WithdrawnAt)).HasValue
                    || entry.OriginalValues.GetValue<string?>(nameof(OpmsSubmission.WithdrawnByUserId)) != null
                    || originalMunicipalityId is not > 0 || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000
                    || string.IsNullOrWhiteSpace(actor) || !withdrawnAt.HasValue
                    || withdrawnAt < entry.OriginalValues.GetValue<DateTime>(nameof(OpmsSubmission.CreatedAt))
                    || entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.UpdatedBy)) != actor
                    || entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsSubmission.UpdatedOn)) != withdrawnAt
                    || lifecycleCount != 1)
                    throw new InvalidOperationException("Performance-submission withdrawal requires exact same-transaction tenant, reason, actor, time and lifecycle evidence.");
                continue;
            }

            var dueDateProperties = new HashSet<string>(
                [nameof(OpmsSubmission.DueDate), nameof(OpmsSubmission.ExtendedDueDate), nameof(OpmsSubmission.DueDateExtendedDays),
                    nameof(OpmsSubmission.UpdatedBy), nameof(OpmsSubmission.UpdatedOn)], StringComparer.Ordinal);
            if (changed.Overlaps([nameof(OpmsSubmission.DueDate), nameof(OpmsSubmission.ExtendedDueDate), nameof(OpmsSubmission.DueDateExtendedDays)]))
            {
                var originalDueDate = entry.OriginalValues.GetValue<DateTime?>(nameof(OpmsSubmission.DueDate));
                var currentDueDate = entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsSubmission.DueDate));
                var extensions = ChangeTracker.Entries<DueDateExtension>().Count(candidate =>
                    candidate.State == EntityState.Added && candidate.Entity.MunicipalityId == originalMunicipalityId
                    && candidate.Entity.SubmissionKind == kind && candidate.Entity.SubmissionId == submissionId
                    && candidate.Entity.OriginalDueDate == originalDueDate && candidate.Entity.ExtendedDueDate == currentDueDate
                    && candidate.Entity.ApprovedByUserId == entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.UpdatedBy))
                    && !string.IsNullOrWhiteSpace(candidate.Entity.Reason));
                if (changed.Except(dueDateProperties).Any() || !originalDueDate.HasValue || !currentDueDate.HasValue
                    || currentDueDate <= originalDueDate || extensions != 1)
                    throw new InvalidOperationException("A submission due date may advance only with one exact append-only extension record.");
                RequireGovernanceAudit(entityName, submissionId, originalMunicipalityId, "ExtendDueDate", requireOldValue: true);
                continue;
            }

            var suggestionProperties = new HashSet<string>(
                [nameof(OpmsSubmission.SystemSuggestedActualPerformance), nameof(OpmsSubmission.WasSystemSuggestionEdited),
                    nameof(OpmsSubmission.SuggestionCalculationTypeId), nameof(OpmsSubmission.SuggestionGeneratedDate),
                    nameof(OpmsSubmission.SuggestionEditedByUserId), nameof(OpmsSubmission.SuggestionEditedAt),
                    nameof(OpmsSubmission.SuggestionEditReason), nameof(OpmsSubmission.ActualPerformance),
                    nameof(OpmsSubmission.Variance), nameof(OpmsSubmission.AchievementPercent), nameof(OpmsSubmission.TargetAchieved),
                    nameof(OpmsSubmission.UpdatedBy), nameof(OpmsSubmission.UpdatedOn)], StringComparer.Ordinal);
            var suggestionEvidenceProperties = new HashSet<string>(
                [nameof(OpmsSubmission.SystemSuggestedActualPerformance), nameof(OpmsSubmission.WasSystemSuggestionEdited),
                    nameof(OpmsSubmission.SuggestionCalculationTypeId), nameof(OpmsSubmission.SuggestionGeneratedDate),
                    nameof(OpmsSubmission.SuggestionEditedByUserId), nameof(OpmsSubmission.SuggestionEditedAt),
                    nameof(OpmsSubmission.SuggestionEditReason)], StringComparer.Ordinal);
            var addedSuggestionEvents = ChangeTracker.Entries<PerformanceSuggestionEvent>().Where(candidate =>
                candidate.State == EntityState.Added
                && candidate.Entity.SubmissionKind == kind
                && (isOpms ? candidate.Entity.OpmsSubmissionId == submissionId && candidate.Entity.IpmsSubmissionId == null
                    : candidate.Entity.IpmsSubmissionId == submissionId && candidate.Entity.OpmsSubmissionId == null)).ToArray();
            if (changed.Overlaps(suggestionEvidenceProperties) || addedSuggestionEvents.Length > 0)
            {
                var events = addedSuggestionEvents.Count(candidate =>
                    candidate.Entity.MunicipalityId == originalMunicipalityId
                    && candidate.Entity.SystemSuggestedActualPerformance == entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.SystemSuggestedActualPerformance))
                    && candidate.Entity.ActualPerformance == entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.ActualPerformance))
                    && candidate.Entity.WasSystemSuggestionEdited == entry.CurrentValues.GetValue<bool>(nameof(OpmsSubmission.WasSystemSuggestionEdited))
                    && candidate.Entity.ActorUserId == entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.UpdatedBy))
                    && candidate.Entity.OccurredAt == entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsSubmission.UpdatedOn))
                    && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId));
                if (changed.Except(suggestionProperties).Any() || events != 1)
                    throw new InvalidOperationException("Consolidated submission values may change only with one exact append-only suggestion event.");
                continue;
            }

            var workflowActionCount = ChangeTracker.Entries<SubmissionWorkflowAction>().Count(candidate =>
                candidate.State == EntityState.Added
                && candidate.Entity.MunicipalityId == originalMunicipalityId
                && candidate.Entity.SubmissionWorkflowInstance != null
                && candidate.Entity.SubmissionWorkflowInstance.SubmissionKind == kind
                && candidate.Entity.SubmissionWorkflowInstance.SubmissionId == submissionId
                && candidate.Entity.ActorUserId == entry.CurrentValues.GetValue<string?>(nameof(OpmsSubmission.UpdatedBy))
                && !string.IsNullOrWhiteSpace(candidate.Entity.ActionCode)
                && candidate.Entity.OccurredAt != default
                && !string.IsNullOrWhiteSpace(candidate.Entity.CorrelationId));
            if (workflowActionCount == 1)
            {
                var workflowProperties = new HashSet<string>(
                    [nameof(OpmsSubmission.BaseState), nameof(OpmsSubmission.Status),
                        nameof(OpmsSubmission.SubmitterStatus), nameof(OpmsSubmission.VerifierStatus),
                        nameof(OpmsSubmission.ApproverStatus), nameof(OpmsSubmission.PmsStatus), nameof(OpmsSubmission.AuditorStatus),
                        nameof(OpmsSubmission.SubmittedAt), nameof(OpmsSubmission.SubmittedByUserId),
                        nameof(OpmsSubmission.VerifierUserId), nameof(OpmsSubmission.VerifiedAt),
                        nameof(OpmsSubmission.VerifierComments), nameof(OpmsSubmission.VerifierComment),
                        nameof(OpmsSubmission.ApproverUserId), nameof(OpmsSubmission.ApprovedAt),
                        nameof(OpmsSubmission.ApproverComments), nameof(OpmsSubmission.ApproverComment),
                        nameof(OpmsSubmission.PmsOfficerUserId), nameof(OpmsSubmission.PmsReviewedAt),
                        nameof(OpmsSubmission.PmsComments), nameof(OpmsSubmission.PmsComment),
                        nameof(OpmsSubmission.PmsRecommendation), nameof(OpmsSubmission.PmsResponseDueDate),
                        nameof(OpmsSubmission.PmsRfiComment), nameof(OpmsSubmission.AuditorUserId),
                        nameof(OpmsSubmission.AuditedAt), nameof(OpmsSubmission.AuditorComments),
                        nameof(OpmsSubmission.AuditorComment), nameof(OpmsSubmission.AuditorRecommendation),
                        nameof(OpmsSubmission.AuditorResponseDueDate), nameof(OpmsSubmission.UpdatedBy),
                        nameof(OpmsSubmission.UpdatedOn)], StringComparer.Ordinal);
                if (changed.Except(workflowProperties).Any())
                    throw new InvalidOperationException("A submission workflow action cannot rewrite draft values, calculations, due dates, identity or retired score projections.");
                continue;
            }

            var draftEditProperties = new HashSet<string>(
                [nameof(OpmsSubmission.ActualPerformance), nameof(OpmsSubmission.AchievementPercent),
                    nameof(OpmsSubmission.TargetAchieved), nameof(OpmsSubmission.ActualExpenditure),
                    nameof(OpmsSubmission.Variance), nameof(OpmsSubmission.VarianceReason),
                    nameof(OpmsSubmission.CorrectiveMeasure), nameof(OpmsSubmission.PoeType),
                    nameof(OpmsSubmission.UpdatedBy), nameof(OpmsSubmission.UpdatedOn)], StringComparer.Ordinal);
            if (changed.Except(draftEditProperties).Any()
                || SubmissionBaseStates.Normalize(entry.OriginalValues.GetValue<string>(nameof(OpmsSubmission.BaseState))) != SubmissionBaseStates.InProgress
                || SubmissionBaseStates.Normalize(entry.CurrentValues.GetValue<string>(nameof(OpmsSubmission.BaseState))) != SubmissionBaseStates.InProgress)
                throw new InvalidOperationException("Submission workflow, actor, decision, score, status and terminal projections may change only beside their exact append-only workflow action.");
            RequireGovernanceAudit(entityName, submissionId, originalMunicipalityId, "Edit", requireOldValue: true);
        }
    }

    private void EnforceExactKpiFieldRevisions(EntityEntry entry, string entityName, bool definitionChanged)
    {
        var targetId = entry.OriginalValues.GetValue<string>(nameof(OpmsTarget.Id));
        var municipalityId = entry.OriginalValues.GetValue<long?>(nameof(OpmsTarget.MunicipalityId));
        if (!municipalityId.HasValue)
            throw new InvalidOperationException("A KPI revision requires reconciled municipality ownership.");
        var isOpms = entry.Metadata.ClrType == typeof(OpmsTarget);
        var revisions = ChangeTracker.Entries<KpiFieldRevision>()
            .Where(candidate => candidate.State == EntityState.Added
                && candidate.Entity.MunicipalityId == municipalityId
                && (isOpms ? candidate.Entity.OpmsTargetId == targetId && candidate.Entity.IpmsTargetId == null
                    : candidate.Entity.IpmsTargetId == targetId && candidate.Entity.OpmsTargetId == null))
            .Select(candidate => candidate.Entity).ToList();

        var expected = new List<(string[] Fields, string? Original, string? Revised)>();
        if (definitionChanged)
        {
            AddDefinitionExpectation(nameof(OpmsTarget.IndicatorNumber), nameof(OpmsTarget.IsIndicatorNumberRevised), nameof(OpmsTarget.RevisedIndicatorNumber));
            AddDefinitionExpectation(nameof(OpmsTarget.TargetName), nameof(OpmsTarget.IsTargetNameRevised), nameof(OpmsTarget.RevisedTargetName));
            AddDefinitionExpectation(nameof(OpmsTarget.KpiDescription), nameof(OpmsTarget.IsKpiDescriptionRevised), nameof(OpmsTarget.RevisedKpiDescription));
        }
        else
        {
            AddOrderingExpectation(nameof(OpmsTarget.OriginalOrderNumber), [nameof(OpmsTarget.OriginalOrderNumber)]);
            AddOrderingExpectation(nameof(OpmsTarget.RevisedOrderNumber), [nameof(OpmsTarget.RevisedOrderNumber), "OrderNumber"]);
        }

        if (expected.Count == 0 || revisions.Count != expected.Count)
            throw new InvalidOperationException("Every effective KPI definition or ordering change requires one exact append-only field revision.");
        foreach (var item in expected)
        {
            var matches = revisions.Where(revision => item.Fields.Contains(revision.FieldName, StringComparer.Ordinal)
                && revision.OriginalValue == item.Original && revision.RevisedValue == item.Revised
                && revision.PublicId != Guid.Empty && !string.IsNullOrWhiteSpace(revision.Reason)
                && !string.IsNullOrWhiteSpace(revision.ApprovalReference) && revision.EffectiveAt != default
                && !string.IsNullOrWhiteSpace(revision.RevisedByUserId) && revision.RecordedAt != default).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Every effective KPI definition or ordering change requires one exact append-only field revision.");
            revisions.Remove(matches[0]);
        }

        var action = definitionChanged ? "ReviseDefinitionFields" : "ReviseOrdering";
        var hasDirectAudit = ChangeTracker.Entries<AuditTrail>().Any(candidate => candidate.State == EntityState.Added
            && candidate.Entity.EntityName == entityName && candidate.Entity.EntityId == targetId
            && candidate.Entity.Action == action && candidate.Entity.MunicipalityId == municipalityId
            && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
            && candidate.Entity.OldValue != null && candidate.Entity.NewValue != null);
        var hasImportCommit = isOpms && ChangeTracker.Entries<OpmsImportBatch>().Any(candidate => candidate.State == EntityState.Modified
            && candidate.Entity.MunicipalityId == municipalityId
            && candidate.OriginalValues.GetValue<OpmsImportBatchStatus>(nameof(OpmsImportBatch.Status)) == OpmsImportBatchStatus.Staged
            && candidate.CurrentValues.GetValue<OpmsImportBatchStatus>(nameof(OpmsImportBatch.Status)) == OpmsImportBatchStatus.Committed);
        if (!hasDirectAudit && !hasImportCommit)
            throw new InvalidOperationException("KPI revisions require their reasoned target audit or governed import-commit evidence in the same transaction.");

        void AddDefinitionExpectation(string originalProperty, string flagProperty, string revisedProperty)
        {
            var originalEffective = entry.OriginalValues.GetValue<bool>(flagProperty)
                ? entry.OriginalValues.GetValue<string?>(revisedProperty)
                : entry.OriginalValues.GetValue<string>(originalProperty);
            var currentEffective = entry.CurrentValues.GetValue<bool>(flagProperty)
                ? entry.CurrentValues.GetValue<string?>(revisedProperty)
                : entry.CurrentValues.GetValue<string>(originalProperty);
            if (!string.Equals(originalEffective, currentEffective, StringComparison.Ordinal))
                expected.Add(([originalProperty], originalEffective, currentEffective));
        }

        void AddOrderingExpectation(string property, string[] fieldNames)
        {
            var original = entry.OriginalValues.GetValue<int>(property);
            var current = entry.CurrentValues.GetValue<int>(property);
            if (original != current)
                expected.Add((fieldNames, original.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    current.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    private void EnforceOpmsTargetRelationshipHistory<TEntity>() where TEntity : class, IOpmsTargetRelationship
    {
        foreach (var entry in ChangeTracker.Entries<TEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                if (row.PublicId == Guid.Empty || string.IsNullOrWhiteSpace(row.OpmsTargetId)
                    || !row.MunicipalityId.HasValue || row.MunicipalityId <= 0
                    || string.IsNullOrWhiteSpace(row.LinkedByUserId)
                    || row.LinkedAt == default || row.LinkedAt > DateTime.UtcNow.AddMinutes(5)
                    || !row.IsActive || row.UnlinkedAt.HasValue
                    || row.UnlinkedByUserId != null || row.UnlinkReason != null)
                    throw new InvalidOperationException("A target relationship requires immutable tenant, target, actor and link-time provenance without fabricated unlink evidence.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Target relationship history cannot be hard deleted; record a reasoned unlink transition instead.");
            if (entry.State != EntityState.Modified) continue;

            EnsureOnlyProperties(entry,
                [nameof(IOpmsTargetRelationship.IsActive), nameof(IOpmsTargetRelationship.UnlinkedAt),
                    nameof(IOpmsTargetRelationship.UnlinkedByUserId), nameof(IOpmsTargetRelationship.UnlinkReason),
                    nameof(IOpmsTargetRelationship.RowVersion)],
                "Target relationship identity, tenant, target, linked record, actor and link-time provenance are immutable.");
            var originalActive = entry.OriginalValues.GetValue<bool>(nameof(IOpmsTargetRelationship.IsActive));
            var currentActive = entry.CurrentValues.GetValue<bool>(nameof(IOpmsTargetRelationship.IsActive));
            var originalUnlinkedAt = entry.OriginalValues.GetValue<DateTime?>(nameof(IOpmsTargetRelationship.UnlinkedAt));
            var currentUnlinkedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(IOpmsTargetRelationship.UnlinkedAt));
            var originalUnlinkedBy = entry.OriginalValues.GetValue<string?>(nameof(IOpmsTargetRelationship.UnlinkedByUserId));
            var currentUnlinkedBy = entry.CurrentValues.GetValue<string?>(nameof(IOpmsTargetRelationship.UnlinkedByUserId));
            var originalReason = entry.OriginalValues.GetValue<string?>(nameof(IOpmsTargetRelationship.UnlinkReason));
            var currentReason = entry.CurrentValues.GetValue<string?>(nameof(IOpmsTargetRelationship.UnlinkReason));
            if (!originalActive || currentActive || originalUnlinkedAt.HasValue
                || originalUnlinkedBy != null || originalReason != null
                || !currentUnlinkedAt.HasValue
                || currentUnlinkedAt < entry.OriginalValues.GetValue<DateTime>(nameof(IOpmsTargetRelationship.LinkedAt))
                || currentUnlinkedAt > DateTime.UtcNow.AddMinutes(5)
                || string.IsNullOrWhiteSpace(currentUnlinkedBy)
                || string.IsNullOrWhiteSpace(currentReason) || currentReason.Length > 2000)
                throw new InvalidOperationException("A target relationship may transition exactly once from active to inactive with an ordered timestamp, actor and substantive reason.");
        }
    }

    private void EnforceKpiBudgetSourceHistory<TEntity>(string targetPropertyName) where TEntity : KpiBudgetSourceBase
    {
        foreach (var entry in ChangeTracker.Entries<TEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                var targetId = entry.Property<string>(targetPropertyName).CurrentValue;
                if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || row.BudgetSourceId <= 0
                    || string.IsNullOrWhiteSpace(targetId) || row.Amount < 0
                    || string.IsNullOrWhiteSpace(row.AllocatedByUserId)
                    || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                    || !row.IsActive || row.DeactivatedAt.HasValue
                    || row.DeactivatedByUserId != null || row.DeactivationReason != null)
                    throw new InvalidOperationException("A KPI budget-source allocation requires immutable tenant, target, source, amount, actor and allocation-time provenance without fabricated deactivation evidence.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("KPI budget-source allocation history cannot be hard deleted; record a reasoned deactivation instead.");
            if (entry.State != EntityState.Modified) continue;

            EnsureOnlyProperties(entry,
                [nameof(KpiBudgetSourceBase.IsActive), nameof(KpiBudgetSourceBase.DeactivatedAt),
                    nameof(KpiBudgetSourceBase.DeactivatedByUserId), nameof(KpiBudgetSourceBase.DeactivationReason),
                    nameof(KpiBudgetSourceBase.RowVersion)],
                "KPI budget-source allocation identity, tenant, target, source, amount, actor and allocation-time provenance are immutable.");
            var deactivatedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(KpiBudgetSourceBase.DeactivatedAt));
            var deactivatedBy = entry.CurrentValues.GetValue<string?>(nameof(KpiBudgetSourceBase.DeactivatedByUserId));
            var reason = entry.CurrentValues.GetValue<string?>(nameof(KpiBudgetSourceBase.DeactivationReason));
            if (!entry.OriginalValues.GetValue<bool>(nameof(KpiBudgetSourceBase.IsActive))
                || entry.CurrentValues.GetValue<bool>(nameof(KpiBudgetSourceBase.IsActive))
                || entry.OriginalValues.GetValue<DateTime?>(nameof(KpiBudgetSourceBase.DeactivatedAt)).HasValue
                || entry.OriginalValues.GetValue<string?>(nameof(KpiBudgetSourceBase.DeactivatedByUserId)) != null
                || entry.OriginalValues.GetValue<string?>(nameof(KpiBudgetSourceBase.DeactivationReason)) != null
                || !deactivatedAt.HasValue
                || deactivatedAt < entry.OriginalValues.GetValue<DateTime>(nameof(KpiBudgetSourceBase.CreatedAt))
                || deactivatedAt > DateTime.UtcNow.AddMinutes(5)
                || string.IsNullOrWhiteSpace(deactivatedBy)
                || string.IsNullOrWhiteSpace(reason) || reason.Length > 2000)
                throw new InvalidOperationException("A KPI budget-source allocation may deactivate exactly once with an ordered timestamp, actor and substantive reason.");
        }
    }

    private void EnforceIpmsOpmsTargetLinkHistory()
    {
        var linkEntries = ChangeTracker.Entries<IpmsTargetOpmsLink>().ToArray();
        foreach (var entry in linkEntries)
        {
            if (entry.State == EntityState.Added)
            {
                var row = entry.Entity;
                if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0
                    || string.IsNullOrWhiteSpace(row.IpmsTargetId) || string.IsNullOrWhiteSpace(row.OpmsTargetId)
                    || string.IsNullOrWhiteSpace(row.LinkedByUserId)
                    || row.LinkedAt == default || row.LinkedAt > DateTime.UtcNow.AddMinutes(5)
                    || !row.IsActive || row.UnlinkedAt.HasValue || row.UnlinkedByUserId != null || row.UnlinkReason != null)
                    throw new InvalidOperationException("An IPMS-to-OPMS target link requires immutable tenant, targets, actor and link-time provenance without fabricated unlink evidence.");

                var addedParentEntry = ChangeTracker.Entries<IpmsTarget>().SingleOrDefault(candidate =>
                    ReferenceEquals(candidate.Entity, row.IpmsTarget)
                    || string.Equals(candidate.Entity.Id, row.IpmsTargetId, StringComparison.Ordinal));
                if (addedParentEntry == null || addedParentEntry.State is not (EntityState.Added or EntityState.Modified)
                    || !string.Equals(addedParentEntry.Entity.RelatedOpmsTargetId, row.OpmsTargetId, StringComparison.Ordinal))
                    throw new InvalidOperationException("An IPMS-to-OPMS target link must be appended with the matching parent pointer in the same transaction.");
                var opmsMunicipalityId = ChangeTracker.Entries<OpmsTarget>()
                    .FirstOrDefault(candidate => ReferenceEquals(candidate.Entity, row.OpmsTarget)
                        || string.Equals(candidate.Entity.Id, row.OpmsTargetId, StringComparison.Ordinal))?.Entity.MunicipalityId
                    ?? OpmsTargets.IgnoreQueryFilters().Where(candidate => candidate.Id == row.OpmsTargetId)
                        .Select(candidate => candidate.MunicipalityId).SingleOrDefault();
                if (addedParentEntry.Entity.MunicipalityId != row.MunicipalityId || opmsMunicipalityId != row.MunicipalityId)
                    throw new UnauthorizedAccessException("IPMS-to-OPMS target links cannot cross municipality boundaries.");
                continue;
            }

            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("IPMS-to-OPMS target-link history cannot be hard deleted; record a reasoned unlink instead.");
            if (entry.State != EntityState.Modified) continue;

            EnsureOnlyProperties(entry,
                [nameof(IpmsTargetOpmsLink.IsActive), nameof(IpmsTargetOpmsLink.UnlinkedAt),
                    nameof(IpmsTargetOpmsLink.UnlinkedByUserId), nameof(IpmsTargetOpmsLink.UnlinkReason),
                    nameof(IpmsTargetOpmsLink.RowVersion)],
                "IPMS-to-OPMS target-link identity, tenant, targets, actor and link-time provenance are immutable.");
            var unlinkedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(IpmsTargetOpmsLink.UnlinkedAt));
            var unlinkedBy = entry.CurrentValues.GetValue<string?>(nameof(IpmsTargetOpmsLink.UnlinkedByUserId));
            var reason = entry.CurrentValues.GetValue<string?>(nameof(IpmsTargetOpmsLink.UnlinkReason));
            if (!entry.OriginalValues.GetValue<bool>(nameof(IpmsTargetOpmsLink.IsActive))
                || entry.CurrentValues.GetValue<bool>(nameof(IpmsTargetOpmsLink.IsActive))
                || entry.OriginalValues.GetValue<DateTime?>(nameof(IpmsTargetOpmsLink.UnlinkedAt)).HasValue
                || entry.OriginalValues.GetValue<string?>(nameof(IpmsTargetOpmsLink.UnlinkedByUserId)) != null
                || entry.OriginalValues.GetValue<string?>(nameof(IpmsTargetOpmsLink.UnlinkReason)) != null
                || !unlinkedAt.HasValue
                || unlinkedAt < entry.OriginalValues.GetValue<DateTime>(nameof(IpmsTargetOpmsLink.LinkedAt))
                || unlinkedAt > DateTime.UtcNow.AddMinutes(5)
                || string.IsNullOrWhiteSpace(unlinkedBy)
                || string.IsNullOrWhiteSpace(reason) || reason.Length > 2000)
                throw new InvalidOperationException("An IPMS-to-OPMS target link may transition exactly once from active to inactive with an ordered timestamp, actor and substantive reason.");

            var unlinkParentEntry = ChangeTracker.Entries<IpmsTarget>().SingleOrDefault(candidate =>
                ReferenceEquals(candidate.Entity, entry.Entity.IpmsTarget)
                || string.Equals(candidate.Entity.Id, entry.Entity.IpmsTargetId, StringComparison.Ordinal));
            if (unlinkParentEntry == null || unlinkParentEntry.State != EntityState.Modified
                || !unlinkParentEntry.Property(nameof(IpmsTarget.RelatedOpmsTargetId)).IsModified
                || string.Equals(unlinkParentEntry.Entity.RelatedOpmsTargetId, entry.Entity.OpmsTargetId, StringComparison.Ordinal))
                throw new InvalidOperationException("An IPMS-to-OPMS target unlink must update the matching parent pointer in the same transaction.");
        }

        foreach (var targetEntry in ChangeTracker.Entries<IpmsTarget>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            var target = targetEntry.Entity;
            if (target.MunicipalityId is not > 0) continue;
            var pointer = targetEntry.Property(nameof(IpmsTarget.RelatedOpmsTargetId));
            var originalId = targetEntry.State == EntityState.Added ? null : (string?)pointer.OriginalValue;
            var currentId = (string?)pointer.CurrentValue;
            if (targetEntry.State == EntityState.Modified && !pointer.IsModified) continue;
            if (string.Equals(originalId, currentId, StringComparison.Ordinal)) continue;

            bool MatchesTarget(IpmsTargetOpmsLink link) => ReferenceEquals(link.IpmsTarget, target)
                || string.Equals(link.IpmsTargetId, target.Id, StringComparison.Ordinal);
            if (originalId != null && !linkEntries.Any(entry => entry.State == EntityState.Modified
                    && MatchesTarget(entry.Entity)
                    && string.Equals(entry.Entity.OpmsTargetId, originalId, StringComparison.Ordinal)
                    && !entry.Entity.IsActive))
                throw new InvalidOperationException("Changing an IPMS target's OPMS pointer requires closing its exact active relationship in the same transaction.");
            if (currentId != null && !linkEntries.Any(entry => entry.State == EntityState.Added
                    && MatchesTarget(entry.Entity)
                    && string.Equals(entry.Entity.OpmsTargetId, currentId, StringComparison.Ordinal)
                    && entry.Entity.MunicipalityId == target.MunicipalityId))
                throw new InvalidOperationException("Changing an IPMS target's OPMS pointer requires appending its exact active relationship in the same transaction.");
        }
    }

    private void EnforceOpmsTargetTemplateHistory()
    {
        if (ChangeTracker.Entries<OpmsTargetTemplate>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("OPMS target-template history cannot be hard deleted.");
        foreach (var entry in ChangeTracker.Entries<OpmsTargetTemplate>().Where(entry => entry.State == EntityState.Modified))
        {
            var originalArchived = entry.OriginalValues.GetValue<bool>(nameof(OpmsTargetTemplate.IsArchived));
            var currentArchived = entry.CurrentValues.GetValue<bool>(nameof(OpmsTargetTemplate.IsArchived));
            if (!originalArchived && currentArchived)
            {
                EnsureOnlyProperties(entry,
                    [nameof(OpmsTargetTemplate.IsArchived), nameof(OpmsTargetTemplate.IsActive), nameof(OpmsTargetTemplate.RowVersion)],
                    "OPMS target-template archive state is one-way and all definition changes require a version snapshot.");
                if (entry.CurrentValues.GetValue<bool>(nameof(OpmsTargetTemplate.IsActive)))
                    throw new InvalidOperationException("An archived OPMS target template cannot remain active.");
                RequireGovernanceAudit(nameof(OpmsTargetTemplate), entry.Entity.PublicId, _tenantContext?.MunicipalityId, "Archive");
                continue;
            }

            var originalVersion = entry.OriginalValues.GetValue<int>(nameof(OpmsTargetTemplate.Version));
            var version = ChangeTracker.Entries<OpmsTargetTemplateVersion>().SingleOrDefault(candidate =>
                candidate.State == EntityState.Added
                && (candidate.Entity.OpmsTargetTemplateId == entry.Entity.Id
                    || ReferenceEquals(candidate.Entity.OpmsTargetTemplate, entry.Entity))
                && candidate.Entity.Version == entry.Entity.Version);
            if (originalArchived || currentArchived || entry.Entity.Version != originalVersion + 1
                || version == null || string.IsNullOrWhiteSpace(version.Entity.SnapshotJson)
                || string.IsNullOrWhiteSpace(version.Entity.CreatedBy))
                throw new InvalidOperationException("OPMS target-template edits require the exact next append-only version snapshot in the same unit of work.");
            RequireGovernanceAudit(nameof(OpmsTargetTemplate), entry.Entity.PublicId, _tenantContext?.MunicipalityId, "Edit");
        }
    }

    private void EnforceIpmsTargetTemplateHistory()
    {
        if (ChangeTracker.Entries<IpmsTargetTemplate>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("IPMS target-template history cannot be hard deleted.");
        foreach (var entry in ChangeTracker.Entries<IpmsTargetTemplate>().Where(entry => entry.State == EntityState.Modified))
        {
            var originalArchived = entry.OriginalValues.GetValue<bool>(nameof(IpmsTargetTemplate.IsArchived));
            var currentArchived = entry.CurrentValues.GetValue<bool>(nameof(IpmsTargetTemplate.IsArchived));
            if (!originalArchived && currentArchived)
            {
                EnsureOnlyProperties(entry,
                    [nameof(IpmsTargetTemplate.IsArchived), nameof(IpmsTargetTemplate.IsActive), nameof(IpmsTargetTemplate.RowVersion)],
                    "IPMS target-template archive state is one-way and all definition changes require a version snapshot.");
                if (entry.CurrentValues.GetValue<bool>(nameof(IpmsTargetTemplate.IsActive)))
                    throw new InvalidOperationException("An archived IPMS target template cannot remain active.");
                RequireGovernanceAudit(nameof(IpmsTargetTemplate), entry.Entity.PublicId, _tenantContext?.MunicipalityId, "Archive");
                continue;
            }

            var originalVersion = entry.OriginalValues.GetValue<int>(nameof(IpmsTargetTemplate.Version));
            var version = ChangeTracker.Entries<IpmsTargetTemplateVersion>().SingleOrDefault(candidate =>
                candidate.State == EntityState.Added
                && (candidate.Entity.IpmsTargetTemplateId == entry.Entity.Id
                    || ReferenceEquals(candidate.Entity.IpmsTargetTemplate, entry.Entity))
                && candidate.Entity.Version == entry.Entity.Version);
            if (originalArchived || currentArchived || entry.Entity.Version != originalVersion + 1
                || version == null || string.IsNullOrWhiteSpace(version.Entity.SnapshotJson)
                || string.IsNullOrWhiteSpace(version.Entity.CreatedBy))
                throw new InvalidOperationException("IPMS target-template edits require the exact next append-only version snapshot in the same unit of work.");
            RequireGovernanceAudit(nameof(IpmsTargetTemplate), entry.Entity.PublicId, _tenantContext?.MunicipalityId, "Edit");
        }
    }

    private static void EnsureMonotonicClosure<TEntity>(EntityEntry<TEntity> entry, string activeProperty, string endProperty,
        string startProperty, string[] allowedProperties, string message, bool requireEndDate = true) where TEntity : class
    {
        var changed = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToArray();
        var originalEnd = entry.OriginalValues.GetValue<DateTime?>(endProperty);
        var currentEnd = entry.CurrentValues.GetValue<DateTime?>(endProperty);
        var startValue = entry.OriginalValues[startProperty];
        var start = startValue switch
        {
            DateTime value => value,
            null => (DateTime?)null,
            _ => Convert.ToDateTime(startValue)
        };
        if (changed.Any(property => !allowedProperties.Contains(property))
            || !entry.OriginalValues.GetValue<bool>(activeProperty)
            || entry.CurrentValues.GetValue<bool>(activeProperty)
            || (requireEndDate && !currentEnd.HasValue)
            || (start.HasValue && currentEnd.HasValue && currentEnd < start)
            || (originalEnd.HasValue && currentEnd.HasValue && currentEnd > originalEnd))
            throw new InvalidOperationException(message);
    }

    private void EnforceIdpPlanHistory()
    {
        if (ChangeTracker.Entries<IdpPlan>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("IDP plan history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<IdpPlan>().Where(entry => entry.State == EntityState.Modified))
        {
            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(IdpPlan.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            if (changed.SetEquals([nameof(IdpPlan.CurrentVersionNumber)]))
            {
                var originalVersion = entry.OriginalValues.GetValue<int>(nameof(IdpPlan.CurrentVersionNumber));
                var currentVersion = entry.CurrentValues.GetValue<int>(nameof(IdpPlan.CurrentVersionNumber));
                var successors = ChangeTracker.Entries<IdpPlanVersion>()
                    .Where(candidate => candidate.State == EntityState.Added
                        && (candidate.Entity.IdpPlanId == entry.Entity.Id || ReferenceEquals(candidate.Entity.IdpPlan, entry.Entity))
                        && candidate.Entity.VersionNumber == currentVersion)
                    .Select(candidate => candidate.Entity)
                    .ToArray();
                var audit = successors.Length == 1
                    ? ChangeTracker.Entries<AuditTrail>().SingleOrDefault(candidate => candidate.State == EntityState.Added
                        && candidate.Entity.EntityName == nameof(IdpPlanVersion)
                        && candidate.Entity.EntityId == successors[0].PublicId.ToString()
                        && candidate.Entity.Action == "Create"
                        && candidate.Entity.MunicipalityId == entry.Entity.MunicipalityId
                        && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
                        && candidate.Entity.NewValue != null)
                    : null;
                if (currentVersion != originalVersion + 1 || successors.Length != 1 || audit == null)
                    throw new InvalidOperationException("IDP plan version sequence may advance only by one exact reason-audited successor in the same unit of work.");
                continue;
            }

            EnsureOnlyProperties(entry,
                [nameof(IdpPlan.PlanTitle), nameof(IdpPlan.StartFinancialYear), nameof(IdpPlan.EndFinancialYear),
                    nameof(IdpPlan.Status), nameof(IdpPlan.EffectiveFrom), nameof(IdpPlan.EffectiveTo),
                    nameof(IdpPlan.PublicationReference), nameof(IdpPlan.ApprovedAt), nameof(IdpPlan.ApprovedByUserId),
                    nameof(IdpPlan.PublishedAt), nameof(IdpPlan.RowVersion)],
                "IDP plan family, predecessor, tenant, code and creation identity are immutable.");
            RequireGovernanceAudit(nameof(IdpPlan), entry.Entity.PublicId, entry.Entity.MunicipalityId);

            var originalStatus = entry.OriginalValues.GetValue<IdpPlanStatus>(nameof(IdpPlan.Status));
            var currentStatus = entry.CurrentValues.GetValue<IdpPlanStatus>(nameof(IdpPlan.Status));
            var originalApprovedAt = entry.OriginalValues.GetValue<DateTime?>(nameof(IdpPlan.ApprovedAt));
            var currentApprovedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(IdpPlan.ApprovedAt));
            var originalApprovedBy = entry.OriginalValues.GetValue<string?>(nameof(IdpPlan.ApprovedByUserId));
            var currentApprovedBy = entry.CurrentValues.GetValue<string?>(nameof(IdpPlan.ApprovedByUserId));
            var originalPublishedAt = entry.OriginalValues.GetValue<DateTime?>(nameof(IdpPlan.PublishedAt));
            var currentPublishedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(IdpPlan.PublishedAt));
            var effectiveFrom = entry.CurrentValues.GetValue<DateTime>(nameof(IdpPlan.EffectiveFrom));
            var effectiveTo = entry.CurrentValues.GetValue<DateTime?>(nameof(IdpPlan.EffectiveTo));

            if (originalStatus == IdpPlanStatus.Archived
                || (int)currentStatus < (int)originalStatus
                || originalStatus == IdpPlanStatus.Published && currentStatus != IdpPlanStatus.Archived
                || originalStatus == IdpPlanStatus.Published
                    && changed.Any(property => property != nameof(IdpPlan.Status) && property != nameof(IdpPlan.EffectiveTo))
                || currentStatus == IdpPlanStatus.Archived && !effectiveTo.HasValue
                || effectiveTo.HasValue && effectiveTo <= effectiveFrom
                || originalApprovedAt.HasValue && (currentApprovedAt != originalApprovedAt || currentApprovedBy != originalApprovedBy)
                || (int)currentStatus >= (int)IdpPlanStatus.Approved && (!currentApprovedAt.HasValue || string.IsNullOrWhiteSpace(currentApprovedBy))
                || originalPublishedAt.HasValue && currentPublishedAt != originalPublishedAt
                || (int)currentStatus >= (int)IdpPlanStatus.Published
                    && (!currentPublishedAt.HasValue || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(IdpPlan.PublicationReference)))))
                throw new InvalidOperationException("IDP plan lifecycle may only advance with immutable approval/publication evidence and valid effective dates.");
        }
    }

    private void EnforceImportBatchHistory()
    {
        if (ChangeTracker.Entries<OpmsImportBatch>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<IdpImportBatch>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Import batch history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<OpmsImportBatch>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(OpmsImportBatch.Status), nameof(OpmsImportBatch.CommittedAt), nameof(OpmsImportBatch.CommittedByUserId), nameof(OpmsImportBatch.RowVersion)],
                "OPMS import identity, source, reconciliation counts and creator evidence are immutable.");
            if (entry.OriginalValues.GetValue<OpmsImportBatchStatus>(nameof(OpmsImportBatch.Status)) != OpmsImportBatchStatus.Staged
                || entry.CurrentValues.GetValue<OpmsImportBatchStatus>(nameof(OpmsImportBatch.Status)) != OpmsImportBatchStatus.Committed
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(OpmsImportBatch.CommittedAt)).HasValue
                || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(OpmsImportBatch.CommittedByUserId))))
                throw new InvalidOperationException("An OPMS import may advance only once from Staged to Committed with complete actor and timestamp evidence.");
            RequireGovernanceAudit(nameof(OpmsImportBatch), entry.Entity.PublicId, entry.Entity.MunicipalityId, "Commit");
        }

        foreach (var entry in ChangeTracker.Entries<IdpImportBatch>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpImportBatch.Status), nameof(IdpImportBatch.CommittedAt), nameof(IdpImportBatch.CommittedByUserId), nameof(IdpImportBatch.RowVersion)],
                "IDP import identity, plan, type, source, reconciliation counts and creator evidence are immutable.");
            if (entry.OriginalValues.GetValue<IdpImportBatchStatus>(nameof(IdpImportBatch.Status)) != IdpImportBatchStatus.Staged
                || entry.CurrentValues.GetValue<IdpImportBatchStatus>(nameof(IdpImportBatch.Status)) != IdpImportBatchStatus.Committed
                || !entry.CurrentValues.GetValue<DateTime?>(nameof(IdpImportBatch.CommittedAt)).HasValue
                || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string?>(nameof(IdpImportBatch.CommittedByUserId))))
                throw new InvalidOperationException("An IDP import may advance only once from Staged to Committed with complete actor and timestamp evidence.");
            var action = string.Equals(entry.Entity.ImportType, "HIERARCHY", StringComparison.OrdinalIgnoreCase)
                ? "CommitHierarchy"
                : "Commit";
            RequireGovernanceAudit(nameof(IdpImportBatch), entry.Entity.PublicId, entry.Entity.MunicipalityId, action);
        }
    }

    private void EnforceIdpDefinitionHistory()
    {
        if (ChangeTracker.Entries<IdpStrategicOutcome>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<IdpStrategicObjective>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<IdpDevelopmentPriority>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<IdpProgramme>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<IdpProject>().Any(entry => entry.State == EntityState.Deleted)
            || ChangeTracker.Entries<IdpKpi>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("IDP hierarchy and KPI definition history cannot be hard deleted.");

        foreach (var entry in ChangeTracker.Entries<IdpStrategicOutcome>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpStrategicOutcome.Code), nameof(IdpStrategicOutcome.Name), nameof(IdpStrategicOutcome.Description),
                    nameof(IdpStrategicOutcome.SortOrder), nameof(IdpStrategicOutcome.RowVersion)],
                "IDP outcome identity and plan association are immutable.");
            RequireIdpImportUpdateAudit(nameof(IdpStrategicOutcome), entry.Entity.PublicId, entry.Entity.IdpPlanId, "HIERARCHY");
        }
        foreach (var entry in ChangeTracker.Entries<IdpStrategicObjective>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpStrategicObjective.Code), nameof(IdpStrategicObjective.Name), nameof(IdpStrategicObjective.Description),
                    nameof(IdpStrategicObjective.BaselineValue), nameof(IdpStrategicObjective.TargetValue),
                    nameof(IdpStrategicObjective.ResponsibleDepartmentId), nameof(IdpStrategicObjective.StartDate),
                    nameof(IdpStrategicObjective.EndDate), nameof(IdpStrategicObjective.BudgetAllocation),
                    nameof(IdpStrategicObjective.SortOrder), nameof(IdpStrategicObjective.RowVersion)],
                "IDP objective identity, outcome association and strategic-owner evidence are immutable.");
            RequireIdpImportUpdateAudit(nameof(IdpStrategicObjective), entry.Entity.PublicId,
                entry.Entity.IdpStrategicOutcome?.IdpPlanId ?? int.MinValue, "HIERARCHY");
        }
        foreach (var entry in ChangeTracker.Entries<IdpDevelopmentPriority>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpDevelopmentPriority.PriorityCode), nameof(IdpDevelopmentPriority.Name),
                    nameof(IdpDevelopmentPriority.Description), nameof(IdpDevelopmentPriority.SortOrder),
                    nameof(IdpDevelopmentPriority.RowVersion)],
                "IDP development-priority identity and objective association are immutable.");
            RequireIdpImportUpdateAudit(nameof(IdpDevelopmentPriority), entry.Entity.PublicId,
                entry.Entity.IdpStrategicObjective?.IdpStrategicOutcome?.IdpPlanId ?? int.MinValue, "HIERARCHY");
        }
        foreach (var entry in ChangeTracker.Entries<IdpProgramme>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpProgramme.ProgrammeCode), nameof(IdpProgramme.Name), nameof(IdpProgramme.Description),
                    nameof(IdpProgramme.ResponsibleDepartmentId), nameof(IdpProgramme.PlannedBudget),
                    nameof(IdpProgramme.ApprovedBudget), nameof(IdpProgramme.ActualExpenditure), nameof(IdpProgramme.RowVersion)],
                "IDP programme identity and development-priority association are immutable.");
            RequireIdpImportUpdateAudit(nameof(IdpProgramme), entry.Entity.PublicId,
                entry.Entity.IdpDevelopmentPriority?.IdpStrategicObjective?.IdpStrategicOutcome?.IdpPlanId ?? int.MinValue,
                "HIERARCHY");
        }
        foreach (var entry in ChangeTracker.Entries<IdpProject>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpProject.ProjectCode), nameof(IdpProject.ProjectName), nameof(IdpProject.Description),
                    nameof(IdpProject.Category), nameof(IdpProject.DepartmentId), nameof(IdpProject.Budget),
                    nameof(IdpProject.FundingSource), nameof(IdpProject.StartDate), nameof(IdpProject.EndDate),
                    nameof(IdpProject.Status), nameof(IdpProject.CommunityNeedReference), nameof(IdpProject.RowVersion)],
                "IDP project identity and programme association are immutable.");
            RequireIdpImportUpdateAudit(nameof(IdpProject), entry.Entity.PublicId,
                entry.Entity.IdpProgramme?.IdpDevelopmentPriority?.IdpStrategicObjective?.IdpStrategicOutcome?.IdpPlanId ?? int.MinValue,
                "HIERARCHY");
        }
        foreach (var entry in ChangeTracker.Entries<IdpKpi>().Where(entry => entry.State == EntityState.Modified))
        {
            EnsureOnlyProperties(entry,
                [nameof(IdpKpi.KpiCode), nameof(IdpKpi.KpiName), nameof(IdpKpi.Description), nameof(IdpKpi.Formula),
                    nameof(IdpKpi.Baseline), nameof(IdpKpi.AnnualTarget), nameof(IdpKpi.FiveYearTarget),
                    nameof(IdpKpi.ResponsibleDepartmentId), nameof(IdpKpi.DataSource), nameof(IdpKpi.ReportingFrequency),
                    nameof(IdpKpi.IndicatorType), nameof(IdpKpi.Circular88Linked), nameof(IdpKpi.TreasuryTidLinked),
                    nameof(IdpKpi.RowVersion)],
                "IDP KPI identity and project association are immutable.");
            RequireIdpImportUpdateAudit(nameof(IdpKpi), entry.Entity.PublicId,
                entry.Entity.IdpProject?.IdpProgramme?.IdpDevelopmentPriority?.IdpStrategicObjective?.IdpStrategicOutcome?.IdpPlanId ?? int.MinValue,
                "KPI");
        }
    }

    private void RequireIdpImportUpdateAudit(string entityName, Guid publicId, int planId, string importType)
    {
        var committedBatches = ChangeTracker.Entries<IdpImportBatch>()
            .Where(entry => entry.State == EntityState.Modified
                && entry.OriginalValues.GetValue<IdpImportBatchStatus>(nameof(IdpImportBatch.Status)) == IdpImportBatchStatus.Staged
                && entry.CurrentValues.GetValue<IdpImportBatchStatus>(nameof(IdpImportBatch.Status)) == IdpImportBatchStatus.Committed
                && entry.Entity.IdpPlanId == planId
                && string.Equals(entry.Entity.ImportType, importType, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Entity)
            .ToArray();
        if (committedBatches.Length != 1)
            throw new InvalidOperationException("IDP hierarchy and KPI definitions may be rewritten only by one governed import commit.");
        RequireGovernanceAudit(entityName, publicId, committedBatches[0].MunicipalityId, "ImportUpdate");
    }

    private void EnforceEvidenceLifecycle()
    {
        PoeFile? ResolveFile(string id)
        {
            var tracked = ChangeTracker.Entries<PoeFile>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity)
                .FirstOrDefault();
            return tracked ?? PoeFiles.IgnoreQueryFilters().AsNoTracking().FirstOrDefault(item => item.Id == id);
        }

        foreach (var assessment in ChangeTracker.Entries<PoeEvidenceAssessment>().Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity))
        {
            var file = ResolveFile(assessment.PoeFileId);
            if (file?.MunicipalityId != assessment.MunicipalityId)
                throw new InvalidOperationException("POE assessment evidence must belong to the same municipality as its parent file.");
        }

        foreach (var hold in ChangeTracker.Entries<PoeLegalHoldEvent>().Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity))
        {
            var file = ResolveFile(hold.PoeFileId);
            if (file?.MunicipalityId != hold.MunicipalityId)
                throw new InvalidOperationException("POE legal-hold evidence must belong to the same municipality as its parent file.");
        }

        foreach (var disposal in ChangeTracker.Entries<PoeDisposalEvent>().Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity))
        {
            var file = ResolveFile(disposal.PoeFileId);
            if (file?.MunicipalityId != disposal.MunicipalityId)
                throw new InvalidOperationException("POE disposal evidence must belong to the same municipality as its parent file.");
        }

        var addedReplacements = ChangeTracker.Entries<PoeEvidenceReplacement>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToArray();
        foreach (var ledger in addedReplacements)
        {
            var superseded = ResolveFile(ledger.SupersededPoeFileId);
            var replacement = ResolveFile(ledger.ReplacementPoeFileId);
            if (superseded == null || replacement == null
                || superseded.Id == replacement.Id
                || superseded.MunicipalityId != ledger.MunicipalityId
                || replacement.MunicipalityId != ledger.MunicipalityId
                || superseded.SubmissionKind != replacement.SubmissionKind
                || superseded.SubmissionId != replacement.SubmissionId
                || superseded.IsActive
                || !replacement.IsActive
                || replacement.SupersedesPoeFileId != superseded.Id
                || string.IsNullOrWhiteSpace(ledger.Reason)
                || string.IsNullOrWhiteSpace(ledger.ReplacedByUserId)
                || string.IsNullOrWhiteSpace(ledger.CorrelationId))
                throw new InvalidOperationException("POE replacement evidence requires same-municipality, same-submission records and exact one-way predecessor lineage.");
        }

        foreach (var entry in ChangeTracker.Entries<PoeFile>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("POE file associations are governed evidence and cannot be hard deleted.");

            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(PoeFile.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            if (changed.SetEquals([nameof(PoeFile.IsActive)]))
            {
                var matching = addedReplacements.Where(item => item.SupersededPoeFileId == entry.Entity.Id).ToArray();
                if (!entry.OriginalValues.GetValue<bool>(nameof(PoeFile.IsActive))
                    || entry.CurrentValues.GetValue<bool>(nameof(PoeFile.IsActive))
                    || matching.Length != 1
                    || matching[0].MunicipalityId != entry.OriginalValues.GetValue<long?>(nameof(PoeFile.MunicipalityId)))
                    throw new InvalidOperationException("A POE association may be retired only by one append-only replacement record in the same unit of work.");
                continue;
            }

            if (changed.SetEquals([nameof(PoeFile.SupersedesPoeFileId)]))
            {
                var originalPredecessor = entry.OriginalValues.GetValue<string?>(nameof(PoeFile.SupersedesPoeFileId));
                var currentPredecessor = entry.CurrentValues.GetValue<string?>(nameof(PoeFile.SupersedesPoeFileId));
                var matching = addedReplacements.Where(item => item.ReplacementPoeFileId == entry.Entity.Id).ToArray();
                if (!string.IsNullOrWhiteSpace(originalPredecessor)
                    || string.IsNullOrWhiteSpace(currentPredecessor)
                    || !entry.CurrentValues.GetValue<bool>(nameof(PoeFile.IsActive))
                    || matching.Length != 1
                    || matching[0].SupersededPoeFileId != currentPredecessor
                    || matching[0].MunicipalityId != entry.OriginalValues.GetValue<long?>(nameof(PoeFile.MunicipalityId)))
                    throw new InvalidOperationException("A POE replacement may acquire predecessor lineage only once and with its append-only replacement record.");
                continue;
            }

            throw new InvalidOperationException("POE association identity, ownership, submission, file metadata, retention and provenance are immutable.");
        }

        var scanProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(EvidenceBlob.ScanStatus), nameof(EvidenceBlob.IsQuarantined), nameof(EvidenceBlob.ScannerProvider),
            nameof(EvidenceBlob.ScannerReference), nameof(EvidenceBlob.ScanDetail), nameof(EvidenceBlob.ScannedAt)
        };
        var disposalProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(EvidenceBlob.IsContentDeleted), nameof(EvidenceBlob.ContentDeletedAt)
        };
        var completedDisposals = ChangeTracker.Entries<PoeDisposalEvent>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.Action == PoeDisposalAction.Completed)
            .Select(entry => entry.Entity)
            .ToArray();
        var trackedFiles = ChangeTracker.Entries<PoeFile>().Select(entry => entry.Entity).ToArray();

        foreach (var entry in ChangeTracker.Entries<EvidenceBlob>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Physical evidence metadata cannot be hard deleted; governed disposal preserves its identity and history.");

            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(EvidenceBlob.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            if (changed.Count == 0) continue;

            if (changed.IsSubsetOf(scanProperties))
            {
                var currentScannedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(EvidenceBlob.ScannedAt));
                var originalScannedAt = entry.OriginalValues.GetValue<DateTime?>(nameof(EvidenceBlob.ScannedAt));
                if (entry.OriginalValues.GetValue<bool>(nameof(EvidenceBlob.IsContentDeleted))
                    || string.IsNullOrWhiteSpace(entry.CurrentValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus)))
                    || !currentScannedAt.HasValue
                    || originalScannedAt.HasValue && currentScannedAt < originalScannedAt)
                    throw new InvalidOperationException("Evidence scan state may only advance for available content with a non-empty status and monotonic scan timestamp.");
                continue;
            }

            if (changed.SetEquals(disposalProperties))
            {
                var deletedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(EvidenceBlob.ContentDeletedAt));
                var associatedFileIds = trackedFiles
                    .Where(file => file.EvidenceBlobId == entry.Entity.Id || ReferenceEquals(file.Blob, entry.Entity))
                    .Select(file => file.Id)
                    .ToHashSet(StringComparer.Ordinal);
                var matching = completedDisposals.Where(item => associatedFileIds.Contains(item.PoeFileId)
                    && item.MunicipalityId == entry.OriginalValues.GetValue<long?>(nameof(EvidenceBlob.MunicipalityId))).ToArray();
                if (entry.OriginalValues.GetValue<bool>(nameof(EvidenceBlob.IsContentDeleted))
                    || !entry.CurrentValues.GetValue<bool>(nameof(EvidenceBlob.IsContentDeleted))
                    || entry.OriginalValues.GetValue<DateTime?>(nameof(EvidenceBlob.ContentDeletedAt)).HasValue
                    || !deletedAt.HasValue
                    || matching.Length != 1)
                    throw new InvalidOperationException("Evidence content may be marked deleted only once with one completed governed disposal event in the same unit of work.");
                continue;
            }

            throw new InvalidOperationException("Evidence identity, tenant, storage location, content metadata, signature and creation evidence are immutable.");
        }
    }

    private static void EnsureOnlyProperties<TEntity>(EntityEntry<TEntity> entry, string[] allowedProperties, string message) where TEntity : class
    {
        if (entry.Properties.Where(property => property.IsModified).Any(property => !allowedProperties.Contains(property.Metadata.Name)))
            throw new InvalidOperationException(message);
    }

    private void RequireGovernanceAudit(string entityName, Guid publicId, long? municipalityId, string action = "Update")
        => RequireGovernanceAudit(entityName, publicId.ToString(), municipalityId, action, requireOldValue: true);

    private void RequireGovernanceAudit(string entityName, string entityId, long? municipalityId, string action, bool requireOldValue, int requiredAuditCount = 1)
    {
        var auditCount = ChangeTracker.Entries<AuditTrail>().Count(candidate => candidate.State == EntityState.Added
            && candidate.Entity.EntityName == entityName
            && candidate.Entity.EntityId == entityId
            && candidate.Entity.Action == action
            && candidate.Entity.MunicipalityId == municipalityId
            && !string.IsNullOrWhiteSpace(candidate.Entity.Reason)
            && (!requireOldValue || candidate.Entity.OldValue != null)
            && candidate.Entity.NewValue != null);
        if (auditCount != requiredAuditCount)
            throw new InvalidOperationException($"{entityName} changes require same-transaction reasoned before/after audit evidence.");
    }

    private static string SubmissionEntityName(SubmissionKind kind) => kind switch
    {
        SubmissionKind.Opms => nameof(OpmsSubmission),
        SubmissionKind.Ipms => nameof(IpmsSubmission),
        _ => throw new InvalidOperationException("Submission governance history requires an OPMS or IPMS submission kind.")
    };
}

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

    private void EnforceInternalAuditAndRfiCreation()
    {
        var addedConfigurations = ChangeTracker.Entries<InternalAuditAssessmentConfiguration>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var addedAssessments = ChangeTracker.Entries<InternalAuditAssessment>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var rfiEntries = ChangeTracker.Entries<PerformanceRfi>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified).ToArray();
        if (addedConfigurations.Length == 0 && addedAssessments.Length == 0 && rfiEntries.Length == 0) return;

        ApplicationUser? ResolveActor(string id)
        {
            var tracked = ChangeTracker.Entries<ApplicationUser>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(item => item.Id == id);
        }

        MunicipalityFinancialYear? ResolveYear(InternalAuditAssessmentConfiguration row)
        {
            var tracked = ChangeTracker.Entries<MunicipalityFinancialYear>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (ReferenceEquals(entry.Entity, row.MunicipalityFinancialYear)
                        || row.MunicipalityFinancialYearId != 0 && entry.Entity.Id == row.MunicipalityFinancialYearId))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (row.MunicipalityFinancialYearId == 0 ? null : MunicipalityFinancialYears.IgnoreQueryFilters()
                .AsNoTracking().SingleOrDefault(item => item.Id == row.MunicipalityFinancialYearId));
        }

        SubmissionWorkflowInstance? ResolveInstance(long id, SubmissionWorkflowInstance? navigation = null)
        {
            var tracked = ChangeTracker.Entries<SubmissionWorkflowInstance>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            if (tracked != null)
            {
                if (tracked.WorkflowDefinition is null && tracked.WorkflowDefinitionId != 0)
                    Entry(tracked).Reference(item => item.WorkflowDefinition).Load();
                return tracked;
            }
            return id == 0 ? null : SubmissionWorkflowInstances.IgnoreQueryFilters().AsNoTracking()
                .Include(item => item.WorkflowDefinition).SingleOrDefault(item => item.Id == id);
        }

        InternalAuditAssessmentConfiguration? ResolveConfiguration(InternalAuditAssessment row)
        {
            var tracked = ChangeTracker.Entries<InternalAuditAssessmentConfiguration>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (ReferenceEquals(entry.Entity, row.Configuration)
                        || row.ConfigurationId != 0 && entry.Entity.Id == row.ConfigurationId))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (row.ConfigurationId == 0 ? null : InternalAuditAssessmentConfigurations.IgnoreQueryFilters()
                .AsNoTracking().SingleOrDefault(item => item.Id == row.ConfigurationId));
        }

        InternalAuditAssessment? ResolvePrevious(InternalAuditAssessment row)
        {
            if (!row.PreviousAssessmentId.HasValue && row.PreviousAssessment == null) return null;
            var tracked = ChangeTracker.Entries<InternalAuditAssessment>()
                .Where(entry => entry.State != EntityState.Deleted && !ReferenceEquals(entry.Entity, row)
                    && (ReferenceEquals(entry.Entity, row.PreviousAssessment)
                        || row.PreviousAssessmentId.HasValue && entry.Entity.Id == row.PreviousAssessmentId.Value))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (!row.PreviousAssessmentId.HasValue ? null : InternalAuditAssessments.IgnoreQueryFilters()
                .AsNoTracking().SingleOrDefault(item => item.Id == row.PreviousAssessmentId.Value));
        }

        PerformanceRfi? ResolveRfi(InternalAuditAssessment row)
        {
            if (!row.PerformanceRfiId.HasValue && row.PerformanceRfi == null) return null;
            var tracked = ChangeTracker.Entries<PerformanceRfi>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (ReferenceEquals(entry.Entity, row.PerformanceRfi)
                        || row.PerformanceRfiId.HasValue && entry.Entity.Id == row.PerformanceRfiId.Value))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (!row.PerformanceRfiId.HasValue ? null : PerformanceRfis.IgnoreQueryFilters()
                .AsNoTracking().SingleOrDefault(item => item.Id == row.PerformanceRfiId.Value));
        }

        AuditTrail[] MatchingAudits(string entityName, Guid publicId, long municipalityId, string actorId, DateTime eventTime) =>
            ChangeTracker.Entries<AuditTrail>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity)
                .Where(audit => audit.EntityName == entityName
                    && audit.EntityId == publicId.ToString()
                    && audit.MunicipalityId == municipalityId
                    && audit.ChangedBy == actorId
                    && audit.ChangedAt == eventTime)
                .ToArray();

        static bool AuditValuesMatch(AuditTrail audit, params (string Name, object? Value)[] expected)
        {
            if (string.IsNullOrWhiteSpace(audit.NewValue)) return false;
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(audit.NewValue);
                if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
                foreach (var (name, value) in expected)
                {
                    if (!document.RootElement.TryGetProperty(name, out var actual)) return false;
                    var canonical = System.Text.Json.JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object));
                    if (!System.Text.Json.JsonElement.DeepEquals(actual, canonical)) return false;
                }
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        foreach (var row in addedConfigurations)
        {
            var year = ResolveYear(row);
            var actor = ResolveActor(row.CreatedByUserId);
            var audits = MatchingAudits(nameof(InternalAuditAssessmentConfiguration), row.PublicId, row.MunicipalityId, row.CreatedByUserId, row.CreatedAt);
            var predecessor = ChangeTracker.Entries<InternalAuditAssessmentConfiguration>()
                .SingleOrDefault(entry => entry.State == EntityState.Modified
                    && entry.OriginalValues.GetValue<long>(nameof(InternalAuditAssessmentConfiguration.MunicipalityFinancialYearId)) == row.MunicipalityFinancialYearId
                    && entry.OriginalValues.GetValue<bool>(nameof(InternalAuditAssessmentConfiguration.IsCurrent)));
            var persistedCount = row.MunicipalityFinancialYearId == 0 ? 0 : InternalAuditAssessmentConfigurations.IgnoreQueryFilters()
                .AsNoTracking().Count(item => item.MunicipalityFinancialYearId == row.MunicipalityFinancialYearId);
            var exactFirst = row.Version == 1 && predecessor == null && persistedCount == 0;
            var exactSuccessor = predecessor != null
                && row.Version == predecessor.OriginalValues.GetValue<int>(nameof(InternalAuditAssessmentConfiguration.Version)) + 1
                && predecessor.CurrentValues.GetValue<bool>(nameof(InternalAuditAssessmentConfiguration.IsCurrent)) == false
                && predecessor.CurrentValues.GetValue<DateTime?>(nameof(InternalAuditAssessmentConfiguration.EffectiveTo)) == row.EffectiveFrom
                && row.EffectiveFrom > predecessor.OriginalValues.GetValue<DateTime>(nameof(InternalAuditAssessmentConfiguration.EffectiveFrom));

            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || year == null || year.MunicipalityId != row.MunicipalityId
                || !Enum.IsDefined(row.Model) || row.Version <= 0 || !row.IsCurrent || row.EffectiveFrom == default || row.EffectiveTo.HasValue
                || string.IsNullOrWhiteSpace(row.Reason) || row.Reason.Length is < 10 or > 1000
                || string.IsNullOrWhiteSpace(row.CreatedByUserId) || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || Database.IsRelational() && actor == null
                || actor != null && actor.MunicipalityId.HasValue && actor.MunicipalityId != row.MunicipalityId
                || !exactFirst && !exactSuccessor
                || audits.Length != 1 || audits[0].Action != "SelectModel" || audits[0].Reason != row.Reason
                || !AuditValuesMatch(audits[0], ("Model", row.Model), ("Version", row.Version),
                    ("EffectiveFrom", row.EffectiveFrom), ("Reason", row.Reason)))
                throw new InvalidOperationException("Internal Audit configuration creation requires one chronological tenant/year version with a canonical actor and exact same-time reasoned audit evidence.");
        }

        foreach (var row in addedAssessments)
        {
            var instance = ResolveInstance(row.SubmissionWorkflowInstanceId, row.SubmissionWorkflowInstance);
            var configuration = ResolveConfiguration(row);
            var previous = ResolvePrevious(row);
            var rfi = ResolveRfi(row);
            var actor = ResolveActor(row.AssessedByUserId);
            var expectedPrevious = row.SubmissionWorkflowInstanceId == 0 ? null : InternalAuditAssessments.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.SubmissionWorkflowInstanceId == row.SubmissionWorkflowInstanceId)
                .OrderByDescending(item => item.AssessedAt).ThenByDescending(item => item.Id).FirstOrDefault();
            var expectedActionCode = row.Outcome is InternalAuditAssessmentOutcome.Achieved or InternalAuditAssessmentOutcome.Satisfactory
                ? "AUDIT_ACCEPTED" : "AUDIT_REJECTED";
            var actions = ChangeTracker.Entries<SubmissionWorkflowAction>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity).Where(action =>
                    (ReferenceEquals(action.SubmissionWorkflowInstance, instance)
                        || instance != null && instance.Id != 0 && action.SubmissionWorkflowInstanceId == instance.Id)
                    && action.ActionCode == expectedActionCode && action.Outcome == WorkflowActionOutcome.Complete
                    && action.ActorUserId == row.AssessedByUserId && action.OccurredAt == row.AssessedAt
                    && action.Comment == row.DetailedObservation && action.CorrelationId == row.CorrelationId).ToArray();
            var audits = MatchingAudits(nameof(InternalAuditAssessment), row.PublicId, row.MunicipalityId, row.AssessedByUserId, row.AssessedAt);
            var adverse = row.Outcome is InternalAuditAssessmentOutcome.NotAchieved or InternalAuditAssessmentOutcome.NotSatisfactory;
            var modelMatches = configuration?.Model switch
            {
                InternalAuditAssessmentModel.Detailed => row.Outcome is InternalAuditAssessmentOutcome.Achieved or InternalAuditAssessmentOutcome.NotAchieved,
                InternalAuditAssessmentModel.SatisfactoryNotSatisfactory => row.Outcome is InternalAuditAssessmentOutcome.Satisfactory or InternalAuditAssessmentOutcome.NotSatisfactory
                    && row.Comment == null && row.Findings == null && row.Recommendation == null && !row.Score.HasValue,
                _ => false
            };
            var previousMatches = expectedPrevious == null
                ? previous == null
                : previous != null && previous.Id == expectedPrevious.Id && previous.MunicipalityId == row.MunicipalityId
                    && previous.SubmissionWorkflowInstanceId == row.SubmissionWorkflowInstanceId && previous.AssessedAt < row.AssessedAt;
            var rfiMatches = adverse
                ? rfi != null && Entry(rfi).State == EntityState.Added
                    && rfi.MunicipalityId == row.MunicipalityId
                    && (ReferenceEquals(rfi.SubmissionWorkflowInstance, instance)
                        || instance != null && instance.Id != 0 && rfi.SubmissionWorkflowInstanceId == instance.Id)
                    && rfi.Question == row.DetailedObservation && rfi.RaisedByUserId == row.AssessedByUserId && rfi.RaisedAt == row.AssessedAt
                : rfi == null;

            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || instance == null || configuration == null
                || instance.MunicipalityId != row.MunicipalityId || configuration.MunicipalityId != row.MunicipalityId
                || configuration.MunicipalityFinancialYearId != instance.WorkflowDefinition.MunicipalityFinancialYearId
                || configuration.EffectiveFrom > row.AssessedAt || configuration.EffectiveTo.HasValue && configuration.EffectiveTo <= row.AssessedAt
                || !modelMatches || string.IsNullOrWhiteSpace(row.DetailedObservation) || row.DetailedObservation.Length > 4000
                || row.Comment?.Length > 2000 || row.Findings?.Length > 4000 || row.Recommendation?.Length > 4000
                || string.IsNullOrWhiteSpace(row.AssessedByUserId) || row.AssessedAt == default || row.AssessedAt > DateTime.UtcNow.AddMinutes(5)
                || string.IsNullOrWhiteSpace(row.CorrelationId) || row.CorrelationId.Length > 100
                || Database.IsRelational() && actor == null
                || actor != null && actor.MunicipalityId.HasValue && actor.MunicipalityId != row.MunicipalityId
                || !previousMatches || !rfiMatches || actions.Length != 1 || audits.Length != 1
                || audits[0].Action != (previous == null ? "Assess" : "Reassess")
                || audits[0].CorrelationId != row.CorrelationId
                || !AuditValuesMatch(audits[0], ("Model", configuration?.Model), ("Outcome", row.Outcome),
                    ("DetailedObservation", row.DetailedObservation), ("PreviousAssessmentPublicId", previous?.PublicId)))
                throw new InvalidOperationException("Internal Audit assessment creation requires the effective tenant/year model, exact history predecessor, canonical actor, workflow action, adverse-outcome RFI and same-time audit evidence.");
        }

        foreach (var entry in rfiEntries)
        {
            var row = entry.Entity;
            var instance = ResolveInstance(row.SubmissionWorkflowInstanceId, row.SubmissionWorkflowInstance);
            var isCreation = entry.State == EntityState.Added;
            var actorId = isCreation ? row.RaisedByUserId
                : entry.Property(nameof(PerformanceRfi.Response)).IsModified ? row.RespondedByUserId
                : row.ClosedByUserId;
            var eventTime = isCreation ? row.RaisedAt
                : entry.Property(nameof(PerformanceRfi.Response)).IsModified ? row.RespondedAt
                : row.ClosedAt;
            var outcome = isCreation ? WorkflowActionOutcome.RaiseRfi
                : entry.Property(nameof(PerformanceRfi.Response)).IsModified ? WorkflowActionOutcome.RespondRfi
                : WorkflowActionOutcome.Complete;
            var comment = isCreation ? row.Question
                : entry.Property(nameof(PerformanceRfi.Response)).IsModified ? row.Response
                : null;
            var actor = string.IsNullOrWhiteSpace(actorId) ? null : ResolveActor(actorId);
            var actions = ChangeTracker.Entries<SubmissionWorkflowAction>().Where(candidate => candidate.State == EntityState.Added)
                .Select(candidate => candidate.Entity).Where(action =>
                    (ReferenceEquals(action.SubmissionWorkflowInstance, instance)
                        || instance != null && instance.Id != 0 && action.SubmissionWorkflowInstanceId == instance.Id)
                    && action.Outcome == outcome && action.ActorUserId == actorId && action.OccurredAt == eventTime
                    && (outcome == WorkflowActionOutcome.Complete || action.Comment == comment)).ToArray();
            var audits = eventTime.HasValue && !string.IsNullOrWhiteSpace(actorId)
                ? MatchingAudits(nameof(PerformanceRfi), row.PublicId, row.MunicipalityId, actorId, eventTime.Value)
                : [];

            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || instance == null || instance.MunicipalityId != row.MunicipalityId
                || string.IsNullOrWhiteSpace(actorId) || !eventTime.HasValue || eventTime == default || eventTime > DateTime.UtcNow.AddMinutes(5)
                || Database.IsRelational() && actor == null
                || actor != null && actor.MunicipalityId.HasValue && actor.MunicipalityId != row.MunicipalityId
                || actions.Length != 1 || audits.Length != 1 || audits[0].Action != actions[0].ActionCode
                || audits[0].CorrelationId != actions[0].CorrelationId
                || isCreation && !AuditValuesMatch(audits[0], ("Question", row.Question), ("ResponseDueAt", row.ResponseDueAt))
                || !isCreation && outcome == WorkflowActionOutcome.RespondRfi && !AuditValuesMatch(audits[0], ("Response", row.Response))
                || !isCreation && outcome == WorkflowActionOutcome.Complete && !AuditValuesMatch(audits[0], ("Comment", actions[0].Comment))
                || isCreation && (string.IsNullOrWhiteSpace(row.Question) || row.Question.Length > 4000
                    || row.ResponseDueAt <= row.RaisedAt || row.Response != null || row.RespondedByUserId != null
                    || row.RespondedAt.HasValue || row.ClosedByUserId != null || row.ClosedAt.HasValue)
                || !isCreation && outcome == WorkflowActionOutcome.RespondRfi && (string.IsNullOrWhiteSpace(row.Response) || row.Response.Length > 4000))
                throw new InvalidOperationException("RFI lifecycle creation requires canonical tenant, instance and actor evidence plus one exact same-time workflow action and audit record.");
        }
    }
}

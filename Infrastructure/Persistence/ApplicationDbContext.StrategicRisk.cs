using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    private static readonly HashSet<string> StrategicRiskTreatmentStates =
        ["OPEN", "IN_PROGRESS", "COMPLETED", "CANCELLED"];

    private void EnforceStrategicRiskCreation()
    {
        var risks = ChangeTracker.Entries<StrategicRisk>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var links = ChangeTracker.Entries<OpmsKpiStrategicRisk>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var assessments = ChangeTracker.Entries<StrategicRiskAssessment>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var treatments = ChangeTracker.Entries<StrategicRiskTreatment>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var reviews = ChangeTracker.Entries<StrategicRiskReview>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        if (risks.Length == 0 && links.Length == 0 && assessments.Length == 0
            && treatments.Length == 0 && reviews.Length == 0) return;

        ApplicationUser? ResolveUser(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var tracked = ChangeTracker.Entries<ApplicationUser>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(item => item.Id == id);
        }

        StrategicRisk? ResolveRisk(long id, StrategicRisk? navigation = null)
        {
            var tracked = ChangeTracker.Entries<StrategicRisk>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id == 0 ? null : StrategicRisks.IgnoreQueryFilters().AsNoTracking()
                .Include(item => item.EffectiveFromMunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear)
                .Include(item => item.EffectiveToMunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear)
                .SingleOrDefault(item => item.Id == id));
        }

        OpmsTarget? ResolveTarget(string id, OpmsTarget? navigation = null)
        {
            var tracked = ChangeTracker.Entries<OpmsTarget>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id.Length > 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id.Length == 0 ? null : OpmsTargets.IgnoreQueryFilters().AsNoTracking()
                .Include(item => item.SdbipLayer).ThenInclude(item => item!.MunicipalityFinancialYear)
                    .ThenInclude(item => item.FinancialYear)
                .SingleOrDefault(item => item.Id == id));
        }

        MunicipalityFinancialYear? ResolveYear(long? id, MunicipalityFinancialYear? navigation = null)
        {
            if (!id.HasValue && navigation == null) return null;
            var tracked = ChangeTracker.Entries<MunicipalityFinancialYear>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id.HasValue && entry.Entity.Id == id.Value))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (!id.HasValue ? null : MunicipalityFinancialYears.IgnoreQueryFilters().AsNoTracking()
                .Include(item => item.FinancialYear).SingleOrDefault(item => item.Id == id.Value));
        }

        AuditTrail[] Audits(string entityName, Guid publicId, long municipalityId, string actorId, DateTime changedAt) =>
            ChangeTracker.Entries<AuditTrail>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity)
                .Where(audit => audit.EntityName == entityName && audit.EntityId == publicId.ToString()
                    && audit.MunicipalityId == municipalityId && audit.ChangedBy == actorId
                    && audit.ChangedAt == changedAt && audit.OldValue == null)
                .ToArray();

        static bool ExactValues(AuditTrail audit, params (string Name, object? Value)[] expected)
        {
            if (string.IsNullOrWhiteSpace(audit.NewValue)) return false;
            try
            {
                using var document = JsonDocument.Parse(audit.NewValue);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || document.RootElement.EnumerateObject().Count() != expected.Length) return false;
                foreach (var (name, value) in expected)
                {
                    if (!document.RootElement.TryGetProperty(name, out var actual)) return false;
                    var canonical = JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object));
                    if (!JsonElement.DeepEquals(actual, canonical)) return false;
                }
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        bool CanonicalActor(ApplicationUser? actor, string actorId, long municipalityId) =>
            actor != null && actor.IsActive && GovernanceActorMatches(actorId, _tenantContext?.UserId)
            && (!actor.MunicipalityId.HasValue || actor.MunicipalityId == municipalityId);

        foreach (var row in risks)
        {
            var actor = ResolveUser(row.CreatedByUserId);
            var owner = ResolveUser(row.OwnerUserId);
            var from = ResolveYear(row.EffectiveFromMunicipalityFinancialYearId, row.EffectiveFromMunicipalityFinancialYear);
            var to = ResolveYear(row.EffectiveToMunicipalityFinancialYearId, row.EffectiveToMunicipalityFinancialYear);
            var audits = Audits(nameof(StrategicRisk), row.PublicId, row.MunicipalityId, row.CreatedByUserId, row.CreatedAt);
            var reasonValid = audits.Length == 1 && !string.IsNullOrWhiteSpace(audits[0].Reason)
                && audits[0].Reason!.Length <= 1000;
            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0
                || string.IsNullOrWhiteSpace(row.RiskTitle) || row.RiskTitle.Length > 500
                || row.RiskReference?.Length > 100 || row.RiskDescription?.Length > 2000
                || row.RiskReference != row.RiskReference?.Trim().ToUpperInvariant()
                || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || row.UpdatedAt.HasValue || row.UpdatedByUserId != null
                || !CanonicalActor(actor, row.CreatedByUserId, row.MunicipalityId)
                || owner != null && (!owner.IsActive || owner.MunicipalityId != row.MunicipalityId)
                || row.OwnerUserId != null && owner == null
                || row.EffectiveFromMunicipalityFinancialYearId.HasValue && (from == null || from.MunicipalityId != row.MunicipalityId)
                || row.EffectiveToMunicipalityFinancialYearId.HasValue && (to == null || to.MunicipalityId != row.MunicipalityId)
                || from != null && from.FinancialYear == null || to != null && to.FinancialYear == null
                || from != null && to != null && from.FinancialYear.StartDate > to.FinancialYear.StartDate
                || !reasonValid || audits[0].Action != "Create"
                || !ExactValues(audits[0],
                    ("RiskReference", row.RiskReference), ("RiskTitle", row.RiskTitle),
                    ("RiskDescription", row.RiskDescription), ("OwnerUserPublicId", owner?.PublicId),
                    ("EffectiveFromMunicipalityFinancialYearPublicId", from?.PublicId),
                    ("EffectiveToMunicipalityFinancialYearPublicId", to?.PublicId), ("IsActive", row.IsActive)))
                throw new InvalidOperationException("Strategic-risk creation requires canonical tenant, actor and effective-year relationships plus one exact same-time reasoned audit.");
        }

        foreach (var row in links)
        {
            var risk = ResolveRisk(row.StrategicRiskId, row.StrategicRisk);
            var target = ResolveTarget(row.OpmsTargetId, row.OpmsTarget);
            var actor = ResolveUser(row.LinkedByUserId);
            var audits = Audits(nameof(OpmsKpiStrategicRisk), row.PublicId, row.MunicipalityId, row.LinkedByUserId, row.LinkedAt);
            var targetYear = target?.SdbipLayer?.MunicipalityFinancialYear;
            var riskFrom = risk == null ? null : ResolveYear(risk.EffectiveFromMunicipalityFinancialYearId, risk.EffectiveFromMunicipalityFinancialYear);
            var riskTo = risk == null ? null : ResolveYear(risk.EffectiveToMunicipalityFinancialYearId, risk.EffectiveToMunicipalityFinancialYear);
            var appliesToTargetYear = targetYear?.FinancialYear != null
                && (riskFrom == null || riskFrom.FinancialYear != null)
                && (riskTo == null || riskTo.FinancialYear != null)
                && (riskFrom == null || riskFrom.FinancialYear.StartDate <= targetYear.FinancialYear.StartDate)
                && (riskTo == null || riskTo.FinancialYear.EndDate >= targetYear.FinancialYear.EndDate);
            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || risk == null || target == null
                || risk.MunicipalityId != row.MunicipalityId || target.MunicipalityId != row.MunicipalityId || !risk.IsActive
                || !appliesToTargetYear
                || !row.IsActive || row.UnlinkedAt.HasValue || row.UnlinkedByUserId != null || row.UnlinkReason != null
                || string.IsNullOrWhiteSpace(row.LinkReason) || row.LinkReason.Length > 1000
                || row.LinkedAt == default || row.LinkedAt > DateTime.UtcNow.AddMinutes(5)
                || !CanonicalActor(actor, row.LinkedByUserId, row.MunicipalityId)
                || audits.Length != 1 || audits[0].Action != "Link" || audits[0].Reason != row.LinkReason
                || !ExactValues(audits[0], ("TargetPublicId", target.PublicId),
                    ("StrategicRiskPublicId", risk.PublicId), ("IsPrimary", row.IsPrimary), ("IsActive", true),
                    ("LinkedByUserPublicId", actor?.PublicId), ("LinkedAt", row.LinkedAt), ("LinkReason", row.LinkReason),
                    ("UnlinkedAt", null), ("UnlinkedByUserPublicId", null), ("UnlinkReason", null),
                    ("ReplacedByPublicId", null)))
                throw new InvalidOperationException("Strategic-risk KPI-link creation requires canonical same-tenant records, pristine link state and one exact same-time actor-bound audit.");
        }

        foreach (var row in assessments)
        {
            var risk = ResolveRisk(row.StrategicRiskId, row.StrategicRisk);
            var actor = ResolveUser(row.AssessedByUserId);
            var audits = Audits(nameof(StrategicRiskAssessment), row.PublicId, row.MunicipalityId, row.AssessedByUserId, row.CreatedAt);
            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || risk == null || risk.MunicipalityId != row.MunicipalityId
                || row.AssessedAt == default || row.AssessedAt > DateTime.UtcNow.AddMinutes(5)
                || row.CreatedAt == default || row.CreatedAt < row.AssessedAt || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || row.InherentLikelihood is < 1 or > 5 || row.InherentImpact is < 1 or > 5
                || row.ResidualLikelihood is < 1 or > 5 || row.ResidualImpact is < 1 or > 5
                || row.ControlAssessment?.Length > 4000 || string.IsNullOrWhiteSpace(row.Reason) || row.Reason.Length is < 10 or > 1000
                || !CanonicalActor(actor, row.AssessedByUserId, row.MunicipalityId)
                || audits.Length != 1 || audits[0].Action != "RecordAssessment" || audits[0].Reason != row.Reason
                || !ExactValues(audits[0], ("PublicId", risk.PublicId), ("AssessedAt", row.AssessedAt),
                    ("InherentLikelihood", row.InherentLikelihood), ("InherentImpact", row.InherentImpact),
                    ("ResidualLikelihood", row.ResidualLikelihood), ("ResidualImpact", row.ResidualImpact),
                    ("ControlAssessment", row.ControlAssessment)))
                throw new InvalidOperationException("Strategic-risk assessment creation requires canonical tenant, risk and actor evidence plus one exact same-time reasoned audit.");
        }

        foreach (var row in treatments)
        {
            var risk = ResolveRisk(row.StrategicRiskId, row.StrategicRisk);
            var actor = ResolveUser(row.CreatedByUserId);
            var owner = ResolveUser(row.OwnerUserId);
            var audits = Audits(nameof(StrategicRiskTreatment), row.PublicId, row.MunicipalityId, row.CreatedByUserId, row.CreatedAt);
            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || risk == null || risk.MunicipalityId != row.MunicipalityId
                || row.Title.Length is < 3 or > 500 || row.Description?.Length > 4000
                || row.DueAt == default || !StrategicRiskTreatmentStates.Contains(row.Status) || row.ProgressPercent is < 0 or > 100
                || row.Status is "COMPLETED" or "CANCELLED"
                || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || row.UpdatedAt.HasValue || row.UpdatedByUserId != null
                || !CanonicalActor(actor, row.CreatedByUserId, row.MunicipalityId)
                || owner == null || !owner.IsActive || owner.MunicipalityId != row.MunicipalityId
                || audits.Length != 1 || audits[0].Action != "Create" || string.IsNullOrWhiteSpace(audits[0].Reason)
                || audits[0].Reason!.Length is < 10 or > 1000
                || !ExactValues(audits[0], ("PublicId", risk.PublicId), ("Title", row.Title),
                    ("Description", row.Description), ("OwnerUserPublicId", owner.PublicId), ("DueAt", row.DueAt),
                    ("Status", row.Status), ("ProgressPercent", row.ProgressPercent)))
                throw new InvalidOperationException("Strategic-risk treatment creation requires canonical tenant, risk, owner and actor evidence plus one exact same-time reasoned audit.");
        }

        foreach (var row in reviews)
        {
            var risk = ResolveRisk(row.StrategicRiskId, row.StrategicRisk);
            var actor = ResolveUser(row.ReviewedByUserId);
            var audits = Audits(nameof(StrategicRiskReview), row.PublicId, row.MunicipalityId, row.ReviewedByUserId, row.CreatedAt);
            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0 || risk == null || risk.MunicipalityId != row.MunicipalityId
                || row.ReviewedAt == default || row.ReviewedAt > DateTime.UtcNow.AddMinutes(5)
                || row.NextReviewAt.HasValue && row.NextReviewAt <= row.ReviewedAt
                || string.IsNullOrWhiteSpace(row.Outcome) || row.Outcome.Length is < 3 or > 4000
                || string.IsNullOrWhiteSpace(row.Reason) || row.Reason.Length is < 10 or > 1000
                || row.CreatedAt == default || row.CreatedAt < row.ReviewedAt || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || !CanonicalActor(actor, row.ReviewedByUserId, row.MunicipalityId)
                || audits.Length != 1 || audits[0].Action != "RecordReview" || audits[0].Reason != row.Reason
                || !ExactValues(audits[0], ("PublicId", risk.PublicId), ("ReviewedAt", row.ReviewedAt),
                    ("Outcome", row.Outcome), ("NextReviewAt", row.NextReviewAt)))
                throw new InvalidOperationException("Strategic-risk review creation requires canonical tenant, risk and actor evidence plus one exact same-time reasoned audit.");
        }
    }
}

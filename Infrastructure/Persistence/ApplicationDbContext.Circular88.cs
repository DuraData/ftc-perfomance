using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    private void EnforceC88CatalogueCreationIntegrity()
    {
        var versions = ChangeTracker.Entries<C88CatalogueVersion>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var modifiedVersions = ChangeTracker.Entries<C88CatalogueVersion>()
            .Where(entry => entry.State == EntityState.Modified).ToArray();
        var items = ChangeTracker.Entries<C88CatalogueItem>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var indicators = ChangeTracker.Entries<C88Indicator>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var questions = ChangeTracker.Entries<C88ComplianceQuestion>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var configurations = ChangeTracker.Entries<C88MunicipalityConfiguration>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();

        if (versions.Length == 0 && modifiedVersions.Length == 0 && items.Length == 0 && indicators.Length == 0
            && questions.Length == 0 && configurations.Length == 0) return;

        ApplicationUser? User(string? id) => string.IsNullOrWhiteSpace(id) ? null :
            ChangeTracker.Entries<ApplicationUser>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault()
            ?? Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(row => row.Id == id);

        Municipality? Municipality(long id) => id <= 0 ? null :
            ChangeTracker.Entries<Municipality>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault()
            ?? Municipalities.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(row => row.Id == id);

        C88CatalogueVersion? Version(long id, C88CatalogueVersion? navigation = null) =>
            ChangeTracker.Entries<C88CatalogueVersion>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault()
            ?? (id == 0 ? null : C88CatalogueVersions.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(row => row.Id == id));

        C88CatalogueItem? Item(long? id, C88CatalogueItem? navigation = null) =>
            ChangeTracker.Entries<C88CatalogueItem>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id.HasValue && id.Value != 0 && entry.Entity.Id == id.Value))
                .Select(entry => entry.Entity).SingleOrDefault()
            ?? (!id.HasValue || id.Value == 0 ? null : C88CatalogueItems.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(row => row.Id == id.Value));

        MunicipalityFinancialYear? FinancialYear(long id, MunicipalityFinancialYear? navigation = null) =>
            ChangeTracker.Entries<MunicipalityFinancialYear>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault()
            ?? (id == 0 ? null : MunicipalityFinancialYears.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(row => row.Id == id));

        AuditTrail[] Audits(Guid publicId, long municipalityId, string action) =>
            ChangeTracker.Entries<AuditTrail>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity)
                .Where(audit => audit.EntityName == "C88" && audit.EntityId == publicId.ToString()
                    && audit.Action == action && audit.MunicipalityId == municipalityId
                    && GovernanceActorMatches(audit.ChangedBy, _tenantContext?.UserId)
                    && !string.IsNullOrWhiteSpace(audit.Reason) && audit.Reason!.Length <= 1000
                    && audit.ChangedAt != default && audit.ChangedAt <= DateTime.UtcNow.AddMinutes(5))
                .ToArray();

        static bool Required(string? value, int maximum) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && value == value.Trim();
        static bool Optional(string? value, int maximum) =>
            value == null || value.Length is > 0 && value.Length <= maximum && value == value.Trim();
        static bool Code(string? value) => Required(value, 80) && value == value!.ToUpperInvariant()
            && value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');
        static bool Period(DateTime start, DateTime? end) => start != default && (!end.HasValue || end >= start);
        bool Actor(ApplicationUser? actor, string actorId, long municipalityId) => actor != null && actor.IsActive
            && GovernanceActorMatches(actorId, _tenantContext?.UserId)
            && (!actor.MunicipalityId.HasValue || actor.MunicipalityId == municipalityId);

        foreach (var row in versions)
        {
            var municipality = Municipality(row.MunicipalityId);
            var actor = User(row.CreatedByUserId);
            var audits = Audits(row.PublicId, row.MunicipalityId, "CreateCatalogueVersion");
            var audit = audits.SingleOrDefault();
            if (row.PublicId == Guid.Empty || municipality == null || !municipality.IsActive
                || !Code(row.Code) || !Required(row.Name, 240) || row.EditionDate == default
                || !Period(row.EffectiveFrom, row.EffectiveTo) || row.IsPublished || row.CreatedAt == default
                || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || !Actor(actor, row.CreatedByUserId, row.MunicipalityId)
                || audits.Length != 1 || audit == null || audit.ChangedBy != row.CreatedByUserId
                || audit.ChangedAt != row.CreatedAt || audit.OldValue != null
                || !ExactJsonMatches(audit.NewValue, new C88CatalogueVersionCreationAuditSnapshot(
                    row.Code, row.Name, row.EditionDate, row.EffectiveFrom, row.EffectiveTo,
                    row.IsPublished, row.IsActive, row.CreatedByUserId, audit.Reason!)))
                throw new InvalidOperationException("Circular 88 catalogue-version creation requires canonical tenant and actor evidence plus one exact same-time reasoned audit.");
        }

        foreach (var entry in modifiedVersions)
        {
            EnsureOnlyProperties(entry,
                [nameof(C88CatalogueVersion.Code), nameof(C88CatalogueVersion.Name),
                    nameof(C88CatalogueVersion.EditionDate), nameof(C88CatalogueVersion.EffectiveFrom),
                    nameof(C88CatalogueVersion.EffectiveTo), nameof(C88CatalogueVersion.IsPublished),
                    nameof(C88CatalogueVersion.IsActive), nameof(C88CatalogueVersion.RowVersion)],
                "Circular 88 catalogue-version identity, tenant and creation evidence are immutable.");
            var row = entry.Entity;
            var wasPublished = entry.OriginalValues.GetValue<bool>(nameof(C88CatalogueVersion.IsPublished));
            var publishing = !wasPublished && row.IsPublished;
            var trackedItems = ChangeTracker.Entries<C88CatalogueItem>()
                .Where(candidate => candidate.State != EntityState.Deleted
                    && (candidate.Entity.C88CatalogueVersionId == row.Id
                        || ReferenceEquals(candidate.Entity.CatalogueVersion, row)) && candidate.Entity.IsActive)
                .Select(candidate => candidate.Entity).ToArray();
            var persistedKinds = C88CatalogueItems.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.C88CatalogueVersionId == row.Id && item.IsActive)
                .Select(item => item.Kind).ToArray();
            var kinds = persistedKinds.Concat(trackedItems.Select(item => item.Kind)).Distinct().ToArray();
            var hasIndicator = C88Indicators.IgnoreQueryFilters().AsNoTracking()
                .Any(indicator => indicator.C88CatalogueVersionId == row.Id && indicator.IsActive
                    && indicator.DataElements.Any() && indicator.Applicability.Any())
                || ChangeTracker.Entries<C88Indicator>().Any(candidate => candidate.State != EntityState.Deleted
                    && (candidate.Entity.C88CatalogueVersionId == row.Id
                        || ReferenceEquals(candidate.Entity.CatalogueVersion, row)) && candidate.Entity.IsActive
                    && candidate.Entity.DataElements.Count > 0 && candidate.Entity.Applicability.Count > 0);
            var hasQuestion = C88ComplianceQuestions.IgnoreQueryFilters().AsNoTracking()
                .Any(question => question.C88CatalogueVersionId == row.Id && question.IsActive)
                || ChangeTracker.Entries<C88ComplianceQuestion>().Any(candidate => candidate.State != EntityState.Deleted
                    && (candidate.Entity.C88CatalogueVersionId == row.Id
                        || ReferenceEquals(candidate.Entity.CatalogueVersion, row)) && candidate.Entity.IsActive);
            var audits = Audits(row.PublicId, row.MunicipalityId, "UpdateCatalogueVersion");
            var audit = audits.SingleOrDefault();
            var before = new C88CatalogueVersionStateAuditSnapshot(
                entry.OriginalValues.GetValue<string>(nameof(C88CatalogueVersion.Code)),
                entry.OriginalValues.GetValue<string>(nameof(C88CatalogueVersion.Name)),
                entry.OriginalValues.GetValue<DateTime>(nameof(C88CatalogueVersion.EditionDate)),
                entry.OriginalValues.GetValue<DateTime>(nameof(C88CatalogueVersion.EffectiveFrom)),
                entry.OriginalValues.GetValue<DateTime?>(nameof(C88CatalogueVersion.EffectiveTo)),
                wasPublished,
                entry.OriginalValues.GetValue<bool>(nameof(C88CatalogueVersion.IsActive)));
            var after = new C88CatalogueVersionStateAuditSnapshot(row.Code, row.Name, row.EditionDate,
                row.EffectiveFrom, row.EffectiveTo, row.IsPublished, row.IsActive);
            if (wasPublished || !Code(row.Code) || !Required(row.Name, 240)
                || row.EditionDate == default || !Period(row.EffectiveFrom, row.EffectiveTo)
                || publishing && (!Enum.GetValues<C88CatalogueItemKind>().All(kinds.Contains)
                    || !hasIndicator || !hasQuestion)
                || audits.Length != 1 || audit == null || User(audit.ChangedBy) is not { } actor
                || !Actor(actor, audit.ChangedBy, row.MunicipalityId)
                || !ExactJsonMatches(audit.OldValue, before) || !ExactJsonMatches(audit.NewValue, after))
                throw new InvalidOperationException("Circular 88 draft catalogue changes and publication require complete controlled content plus one exact actor-bound before/after audit; published catalogue editions are immutable.");
        }

        foreach (var row in items)
        {
            var version = Version(row.C88CatalogueVersionId, row.CatalogueVersion);
            var parent = Item(row.ParentItemId, row.ParentItem);
            var audits = Audits(row.PublicId, row.MunicipalityId, "CreateCatalogueItem");
            var audit = audits.SingleOrDefault();
            if (row.PublicId == Guid.Empty || version == null || version.MunicipalityId != row.MunicipalityId
                || version.IsPublished || !version.IsActive || !Enum.IsDefined(row.Kind)
                || !Code(row.Code) || !Required(row.Name, 240) || !Optional(row.Description, 4000)
                || row.DisplayOrder < 0 || row.ParentItemId.HasValue && (parent == null
                    || parent.MunicipalityId != row.MunicipalityId
                    || parent.C88CatalogueVersionId != version.Id)
                || audits.Length != 1 || audit == null || User(audit.ChangedBy) is not { } actor
                || !Actor(actor, audit.ChangedBy, row.MunicipalityId) || audit.OldValue != null
                || !ExactJsonMatches(audit.NewValue, new C88CatalogueItemCreationAuditSnapshot(
                    version.PublicId, parent?.PublicId, row.Kind, row.Code, row.Name, row.Description,
                    row.DisplayOrder, row.IsActive, audit.Reason!)))
                throw new InvalidOperationException("Circular 88 catalogue-item creation requires an editable same-tenant edition, canonical references and one exact actor-bound reasoned audit.");
        }

        foreach (var row in indicators)
        {
            var version = Version(row.C88CatalogueVersionId, row.CatalogueVersion);
            var sector = Item(row.SectorItemId, row.SectorItem);
            var outcome = Item(row.OutcomeItemId, row.OutcomeItem);
            var type = Item(row.IndicatorTypeItemId, row.IndicatorTypeItem);
            var elements = row.DataElements.OrderBy(element => element.Sequence).ToArray();
            var applicability = row.Applicability.OrderBy(item => Item(item.MunicipalCategoryItemId, item.MunicipalCategoryItem)?.PublicId)
                .ThenBy(item => Item(item.ReadinessTierItemId, item.ReadinessTierItem)?.PublicId).ToArray();
            var elementEntries = ChangeTracker.Entries<C88DataElement>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity).ToHashSet();
            var applicabilityEntries = ChangeTracker.Entries<C88IndicatorApplicability>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity).ToHashSet();
            var audits = Audits(row.PublicId, row.MunicipalityId, "CreateIndicator");
            var audit = audits.SingleOrDefault();
            bool Linked(C88CatalogueItem? item, long? id, C88CatalogueItemKind kind) =>
                !id.HasValue && item == null || item != null && item.MunicipalityId == row.MunicipalityId
                    && item.C88CatalogueVersionId == version?.Id && item.Kind == kind && item.IsActive;
            var applicabilityValid = applicability.All(link =>
            {
                var category = Item(link.MunicipalCategoryItemId, link.MunicipalCategoryItem);
                var tier = Item(link.ReadinessTierItemId, link.ReadinessTierItem);
                return applicabilityEntries.Contains(link) && link.PublicId != Guid.Empty
                    && link.MunicipalityId == row.MunicipalityId && category != null && category.IsActive
                    && category.Kind == C88CatalogueItemKind.MunicipalCategory
                    && category.C88CatalogueVersionId == version?.Id
                    && (!link.ReadinessTierItemId.HasValue || tier != null && tier.IsActive
                        && tier.Kind == C88CatalogueItemKind.ReadinessTier
                        && tier.C88CatalogueVersionId == version?.Id)
                    && Optional(link.Notes, 2000);
            });
            var snapshot = version == null ? null : new C88IndicatorCreationAuditSnapshot(
                version.PublicId, row.Code, row.Name, row.Definition, row.OfficialTechnicalIndicatorDescription,
                sector?.PublicId, outcome?.PublicId, type?.PublicId, row.ValueType, row.CalculationOperator,
                row.OfficialFormulaText, row.RequiresBaseline, row.RequiresMediumTermTarget,
                row.RequiresAnnualTarget, row.IsActive,
                elements.Select(element => new C88DataElementCreationAuditSnapshot(element.PublicId,
                    element.Code, element.Name, element.Description, element.ValueType, element.IsRequired,
                    element.Sequence)).ToArray(),
                applicability.Select(link => new C88IndicatorApplicabilityCreationAuditSnapshot(link.PublicId,
                    Item(link.MunicipalCategoryItemId, link.MunicipalCategoryItem)!.PublicId,
                    Item(link.ReadinessTierItemId, link.ReadinessTierItem)?.PublicId,
                    link.IsApplicable, link.Notes)).ToArray(), audit?.Reason ?? string.Empty);
            if (row.PublicId == Guid.Empty || version == null || version.MunicipalityId != row.MunicipalityId
                || version.IsPublished || !version.IsActive || !Code(row.Code) || !Required(row.Name, 300)
                || !Required(row.Definition, 8000) || !Required(row.OfficialTechnicalIndicatorDescription, 16000)
                || !Optional(row.OfficialFormulaText, 4000) || !Enum.IsDefined(row.ValueType)
                || !Enum.IsDefined(row.CalculationOperator)
                || row.ValueType is C88ValueType.Boolean or C88ValueType.Text
                    && row.CalculationOperator != C88ControlledCalculationOperator.None
                || !Linked(sector, row.SectorItemId, C88CatalogueItemKind.Sector)
                || !Linked(outcome, row.OutcomeItemId, C88CatalogueItemKind.Outcome)
                || !Linked(type, row.IndicatorTypeItemId, C88CatalogueItemKind.IndicatorType)
                || elements.Any(element => !elementEntries.Contains(element) || element.PublicId == Guid.Empty
                    || element.MunicipalityId != row.MunicipalityId || !Code(element.Code)
                    || !Required(element.Name, 240) || !Optional(element.Description, 4000)
                    || !Enum.IsDefined(element.ValueType) || element.Sequence <= 0)
                || elements.Select(element => element.Code).Distinct(StringComparer.Ordinal).Count() != elements.Length
                || elements.Select(element => element.Sequence).Distinct().Count() != elements.Length
                || !applicabilityValid || audits.Length != 1 || audit == null
                || User(audit.ChangedBy) is not { } actor || !Actor(actor, audit.ChangedBy, row.MunicipalityId)
                || audit.OldValue != null || snapshot == null || !ExactJsonMatches(audit.NewValue, snapshot))
                throw new InvalidOperationException("Circular 88 indicator creation requires an editable same-tenant edition, canonical child definitions and one exact actor-bound reasoned audit.");
        }

        foreach (var row in questions)
        {
            var version = Version(row.C88CatalogueVersionId, row.CatalogueVersion);
            var reportType = Item(row.ReportTypeItemId, row.ReportTypeItem);
            var responseType = Item(row.ResponseTypeItemId, row.ResponseTypeItem);
            var audits = Audits(row.PublicId, row.MunicipalityId, "CreateComplianceQuestion");
            var audit = audits.SingleOrDefault();
            if (row.PublicId == Guid.Empty || version == null || version.MunicipalityId != row.MunicipalityId
                || version.IsPublished || !version.IsActive || reportType == null || responseType == null
                || reportType.MunicipalityId != row.MunicipalityId || responseType.MunicipalityId != row.MunicipalityId
                || reportType.C88CatalogueVersionId != version.Id || responseType.C88CatalogueVersionId != version.Id
                || reportType.Kind != C88CatalogueItemKind.ReportType || responseType.Kind != C88CatalogueItemKind.ResponseType
                || !reportType.IsActive || !responseType.IsActive || !Code(row.Code) || !Required(row.Prompt, 4000)
                || row.Sequence <= 0 || audits.Length != 1 || audit == null
                || User(audit.ChangedBy) is not { } actor || !Actor(actor, audit.ChangedBy, row.MunicipalityId)
                || audit.OldValue != null || !ExactJsonMatches(audit.NewValue,
                    new C88ComplianceQuestionCreationAuditSnapshot(version.PublicId, reportType.PublicId,
                        responseType.PublicId, row.Code, row.Prompt, row.IsRequired, row.Sequence,
                        row.IsActive, audit.Reason!)))
                throw new InvalidOperationException("Circular 88 compliance-question creation requires canonical same-edition types and one exact actor-bound reasoned audit.");
        }

        foreach (var row in configurations)
        {
            var municipality = Municipality(row.MunicipalityId);
            var year = FinancialYear(row.MunicipalityFinancialYearId, row.MunicipalityFinancialYear);
            var version = Version(row.C88CatalogueVersionId, row.CatalogueVersion);
            var actor = User(row.CreatedByUserId);
            var audits = Audits(row.PublicId, row.MunicipalityId, "Configure");
            var audit = audits.SingleOrDefault();
            if (row.PublicId == Guid.Empty || municipality == null || !municipality.IsActive
                || year == null || year.MunicipalityId != row.MunicipalityId || !year.IsActive
                || version == null || version.MunicipalityId != row.MunicipalityId
                || !version.IsPublished || !version.IsActive || !Period(row.EffectiveFrom, row.EffectiveTo)
                || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || !Actor(actor, row.CreatedByUserId, row.MunicipalityId)
                || audits.Length != 1 || audit == null || audit.ChangedBy != row.CreatedByUserId
                || audit.ChangedAt != row.CreatedAt || audit.OldValue != null
                || !ExactJsonMatches(audit.NewValue, new C88ConfigurationCreationAuditSnapshot(
                    year.PublicId, version.PublicId, row.IsEnabled, row.EffectiveFrom, row.EffectiveTo,
                    row.CreatedByUserId, audit.Reason!)))
                throw new InvalidOperationException("Circular 88 configuration creation requires active same-tenant year/catalogue references, canonical actor evidence and one exact same-time reasoned audit.");
        }
    }
}

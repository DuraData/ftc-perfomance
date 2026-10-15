using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    private static readonly HashSet<string> StrategicDocumentScanProperties =
    [
        nameof(EvidenceBlob.ScanStatus), nameof(EvidenceBlob.IsQuarantined), nameof(EvidenceBlob.ScannerProvider),
        nameof(EvidenceBlob.ScannerReference), nameof(EvidenceBlob.ScanDetail), nameof(EvidenceBlob.ScannedAt)
    ];

    private void EnforceStrategicDocumentCreationAndLifecycle()
    {
        var addedTypes = ChangeTracker.Entries<StrategicDocumentType>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var addedDocuments = ChangeTracker.Entries<StrategicDocument>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var modifiedDocuments = ChangeTracker.Entries<StrategicDocument>()
            .Where(entry => entry.State == EntityState.Modified).ToArray();
        var addedEvents = ChangeTracker.Entries<StrategicDocumentEvent>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var modifiedBlobs = ChangeTracker.Entries<EvidenceBlob>()
            .Where(entry => entry.State == EntityState.Modified).ToArray();

        if (addedTypes.Length == 0 && addedDocuments.Length == 0 && modifiedDocuments.Length == 0
            && addedEvents.Length == 0 && modifiedBlobs.Length == 0) return;

        ApplicationUser? ResolveUser(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return ChangeTracker.Entries<ApplicationUser>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault()
                ?? Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(item => item.Id == id);
        }

        MunicipalityFinancialYear? ResolveYear(long id, MunicipalityFinancialYear? navigation)
        {
            var tracked = ChangeTracker.Entries<MunicipalityFinancialYear>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id == 0 ? null : MunicipalityFinancialYears.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id));
        }

        StrategicDocumentType? ResolveType(long id, StrategicDocumentType? navigation)
        {
            var tracked = ChangeTracker.Entries<StrategicDocumentType>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id == 0 ? null : StrategicDocumentTypes.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id));
        }

        StrategicDocument? ResolveDocument(long id, StrategicDocument? navigation)
        {
            var tracked = ChangeTracker.Entries<StrategicDocument>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id == 0 ? null : StrategicDocuments.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id));
        }

        EvidenceBlob? ResolveBlob(string? id, EvidenceBlob? navigation)
        {
            var tracked = ChangeTracker.Entries<EvidenceBlob>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != null && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id == null ? null : EvidenceBlobs.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id));
        }

        AuditTrail[] Audits(string entityName, Guid publicId, long municipalityId, string actorId, DateTime changedAt) =>
            ChangeTracker.Entries<AuditTrail>().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity)
                .Where(audit => audit.EntityName == entityName && audit.EntityId == publicId.ToString()
                    && audit.MunicipalityId == municipalityId && audit.ChangedBy == actorId
                    && audit.ChangedAt == changedAt)
                .ToArray();

        static bool ExactJson(string? json, params (string Name, object? Value)[] expected)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                using var document = JsonDocument.Parse(json);
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

        static bool ExactAudit(AuditTrail audit, params (string Name, object? Value)[] expected) =>
            ExactJson(audit.NewValue, expected);

        static bool SnapshotMatches(StrategicDocumentEvent lifecycleEvent, StrategicDocument document) =>
            ExactJson(lifecycleEvent.SnapshotJson,
                ("PublicId", document.PublicId), ("DocumentFamilyId", document.DocumentFamilyId),
                ("VersionNumber", document.VersionNumber), ("Title", document.Title),
                ("IsCurrent", document.IsCurrent), ("IsActive", document.IsActive),
                ("IsApproved", document.IsApproved), ("IsPublished", document.IsPublished),
                ("DocumentDate", document.DocumentDate), ("PublicationDate", document.PublicationDate));

        bool CanonicalActor(ApplicationUser? actor, string actorId, long municipalityId) =>
            actor != null && actor.IsActive && GovernanceActorMatches(actorId, _tenantContext?.UserId)
            && (!actor.MunicipalityId.HasValue || actor.MunicipalityId == municipalityId);

        foreach (var row in addedTypes)
        {
            var actor = ResolveUser(row.CreatedByUserId);
            var audits = Audits(nameof(StrategicDocumentType), row.PublicId, row.MunicipalityId, row.CreatedByUserId, row.CreatedAt);
            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0
                || string.IsNullOrWhiteSpace(row.Code) || row.Code.Length > 80 || row.Code != row.Code.Trim().ToUpperInvariant()
                || string.IsNullOrWhiteSpace(row.Name) || row.Name.Length > 160 || row.Name != row.Name.Trim()
                || row.Description?.Length > 1000 || row.DisplayOrder < 0
                || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || !CanonicalActor(actor, row.CreatedByUserId, row.MunicipalityId)
                || audits.Length != 1 || audits[0].Action != "Create" || audits[0].OldValue != null
                || string.IsNullOrWhiteSpace(audits[0].Reason) || audits[0].Reason!.Length > 1000
                || !ExactAudit(audits[0], ("Code", row.Code), ("Name", row.Name),
                    ("Description", row.Description), ("AllowsExternalLinks", row.AllowsExternalLinks), ("IsActive", row.IsActive),
                    ("DisplayOrder", row.DisplayOrder), ("Reason", audits[0].Reason)))
                throw new InvalidOperationException("Strategic-document type creation requires canonical tenant and actor evidence plus one exact same-time reasoned audit.");
        }

        foreach (var row in addedDocuments)
        {
            var actor = ResolveUser(row.CreatedByUserId);
            var year = ResolveYear(row.MunicipalityFinancialYearId, row.MunicipalityFinancialYear);
            var type = ResolveType(row.StrategicDocumentTypeId, row.DocumentType);
            var previous = ResolveDocument(row.PreviousVersionId ?? 0, row.PreviousVersion);
            var blob = ResolveBlob(row.EvidenceBlobId, row.Blob);
            var lifecycleEvents = addedEvents.Where(item => ReferenceEquals(item.StrategicDocument, row)
                || item.StrategicDocumentId != 0 && item.StrategicDocumentId == row.Id).ToArray();
            var versionEvent = lifecycleEvents.Where(item => item.Action == StrategicDocumentEventAction.VersionCreated).ToArray();
            var audits = Audits(nameof(StrategicDocument), row.PublicId, row.MunicipalityId, row.CreatedByUserId, row.CreatedAt);
            var externalValid = row.ExternalUrl == null || Uri.TryCreate(row.ExternalUrl, UriKind.Absolute, out var externalUri)
                && externalUri.Scheme == Uri.UriSchemeHttps;
            var previousEntry = previous == null ? null : ChangeTracker.Entries<StrategicDocument>()
                .SingleOrDefault(entry => ReferenceEquals(entry.Entity, previous));
            var previousWasCurrent = previousEntry?.State == EntityState.Modified
                ? previousEntry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsCurrent))
                : previous?.IsCurrent;
            if (row.PublicId == Guid.Empty || row.DocumentFamilyId == Guid.Empty || row.MunicipalityId <= 0
                || year == null || year.MunicipalityId != row.MunicipalityId || !year.IsActive
                || type == null || type.MunicipalityId != row.MunicipalityId || !type.IsActive
                || string.IsNullOrWhiteSpace(row.Title) || row.Title.Length > 240 || row.Title != row.Title.Trim()
                || row.Description?.Length > 4000 || row.SdbipLayer?.Length > 120 || row.FileName?.Length > 260
                || row.ExternalUrl?.Length > 2048 || !externalValid || row.DisplayOrder < 0 || row.DocumentDate == default
                || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || !CanonicalActor(actor, row.CreatedByUserId, row.MunicipalityId)
                || row.IsCurrent != true || row.IsActive != true || row.IsApproved || row.IsPublished
                || row.ApprovedAt.HasValue || row.ApprovedByUserId != null || row.ApprovalReference != null
                || row.PublicationDate.HasValue || row.PublishedAt.HasValue || row.PublishedByUserId != null
                || blob == null == (row.ExternalUrl == null)
                || blob != null && blob.MunicipalityId != row.MunicipalityId
                || row.ExternalUrl != null && !type.AllowsExternalLinks
                || previous == null && (row.PreviousVersionId.HasValue || row.VersionNumber != 1)
                || previous != null && (previous.MunicipalityId != row.MunicipalityId
                    || previous.MunicipalityFinancialYearId != year.Id || previous.StrategicDocumentTypeId != type.Id
                    || previous.DocumentFamilyId != row.DocumentFamilyId || row.VersionNumber != previous.VersionNumber + 1
                    || previousWasCurrent != true || previous.IsCurrent)
                || lifecycleEvents.Length != 1 || versionEvent.Length != 1
                || versionEvent[0].OccurredAt != row.CreatedAt || string.IsNullOrWhiteSpace(versionEvent[0].Reason)
                || versionEvent[0].Reason.Length > 1000
                || !SnapshotMatches(versionEvent[0], row)
                || audits.Length != 1 || audits[0].Action != "CreateVersion" || audits[0].OldValue != null
                || audits[0].Reason != versionEvent[0].Reason
                || !ExactAudit(audits[0], ("DocumentFamilyId", row.DocumentFamilyId),
                    ("VersionNumber", row.VersionNumber), ("PreviousVersionPublicId", previous?.PublicId),
                    ("MunicipalityFinancialYearPublicId", year.PublicId), ("TypePublicId", type.PublicId),
                    ("SdbipLayer", row.SdbipLayer), ("Title", row.Title), ("Description", row.Description),
                    ("DocumentDate", row.DocumentDate), ("EvidenceBlobPublicId", blob?.PublicId), ("FileName", row.FileName),
                    ("ExternalUrl", row.ExternalUrl), ("DisplayOrder", row.DisplayOrder), ("Reason", versionEvent[0].Reason)))
                throw new InvalidOperationException("Strategic-document version creation requires canonical same-tenant lineage, pristine lifecycle state and one exact same-time actor-bound event and audit.");
        }

        foreach (var lifecycleEvent in addedEvents)
        {
            var document = ResolveDocument(lifecycleEvent.StrategicDocumentId, lifecycleEvent.StrategicDocument)
                ?? throw new InvalidOperationException("Strategic-document events require a canonical document relationship.");
            var actor = ResolveUser(lifecycleEvent.ActorUserId);
            var documentEntry = ChangeTracker.Entries<StrategicDocument>()
                .SingleOrDefault(entry => ReferenceEquals(entry.Entity, document));
            var blobEntry = modifiedBlobs.SingleOrDefault(entry => ReferenceEquals(entry.Entity, document.Blob)
                || document.EvidenceBlobId != null && entry.Entity.Id == document.EvidenceBlobId);
            var expectedAuditAction = lifecycleEvent.Action switch
            {
                StrategicDocumentEventAction.VersionCreated => "CreateVersion",
                StrategicDocumentEventAction.Approved => "Approve",
                StrategicDocumentEventAction.Published => "Publish",
                StrategicDocumentEventAction.Retired => "Retire",
                StrategicDocumentEventAction.MalwareRescanned => "MalwareRescan",
                _ => null
            };
            var audits = expectedAuditAction == null
                ? []
                : Audits(nameof(StrategicDocument), document.PublicId, lifecycleEvent.MunicipalityId,
                    lifecycleEvent.ActorUserId, lifecycleEvent.OccurredAt).Where(item => item.Action == expectedAuditAction).ToArray();
            var transitionValid = lifecycleEvent.Action switch
            {
                StrategicDocumentEventAction.VersionCreated => documentEntry?.State == EntityState.Added
                    && lifecycleEvent.OccurredAt == document.CreatedAt,
                StrategicDocumentEventAction.Approved => documentEntry is { State: EntityState.Modified }
                    && !documentEntry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsApproved))
                    && document.IsApproved && document.ApprovedAt == lifecycleEvent.OccurredAt
                    && document.ApprovedByUserId == lifecycleEvent.ActorUserId
                    && !string.IsNullOrWhiteSpace(document.ApprovalReference)
                    && audits.Length == 1 && audits[0].OldValue == null
                    && ExactAudit(audits[0], ("ApprovalReference", document.ApprovalReference), ("Reason", lifecycleEvent.Reason)),
                StrategicDocumentEventAction.Published => documentEntry is { State: EntityState.Modified }
                    && !documentEntry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsPublished))
                    && document.IsPublished && document.IsApproved && document.PublicationDate.HasValue
                    && document.PublishedAt == lifecycleEvent.OccurredAt && document.PublishedByUserId == lifecycleEvent.ActorUserId
                    && audits.Length == 1 && audits[0].OldValue == null
                    && ExactAudit(audits[0], ("PublicationDate", document.PublicationDate), ("Reason", lifecycleEvent.Reason)),
                StrategicDocumentEventAction.Retired => documentEntry is { State: EntityState.Modified }
                    && documentEntry.OriginalValues.GetValue<bool>(nameof(StrategicDocument.IsActive)) && !document.IsActive
                    && audits.Length == 1 && audits[0].OldValue == null
                    && ExactAudit(audits[0], ("Reason", lifecycleEvent.Reason)),
                StrategicDocumentEventAction.MalwareRescanned => blobEntry is { State: EntityState.Modified }
                    && blobEntry.CurrentValues.GetValue<DateTime?>(nameof(EvidenceBlob.ScannedAt)) == lifecycleEvent.OccurredAt
                    && lifecycleEvent.Reason == (blobEntry.CurrentValues.GetValue<string?>(nameof(EvidenceBlob.ScanDetail))
                        ?? blobEntry.CurrentValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus)))
                    && audits.Length == 1
                    && ExactJson(audits[0].OldValue,
                        ("ScanStatus", blobEntry.OriginalValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus))),
                        ("IsQuarantined", blobEntry.OriginalValues.GetValue<bool>(nameof(EvidenceBlob.IsQuarantined))),
                        ("ScannerReference", blobEntry.OriginalValues.GetValue<string?>(nameof(EvidenceBlob.ScannerReference))))
                    && ExactAudit(audits[0],
                        ("Status", blobEntry.CurrentValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus))),
                        ("IsQuarantined", blobEntry.CurrentValues.GetValue<bool>(nameof(EvidenceBlob.IsQuarantined))),
                        ("ProviderReference", blobEntry.CurrentValues.GetValue<string?>(nameof(EvidenceBlob.ScannerReference))),
                        ("Reason", lifecycleEvent.Reason)),
                _ => false
            };
            if (lifecycleEvent.PublicId == Guid.Empty || lifecycleEvent.MunicipalityId <= 0
                || document.MunicipalityId != lifecycleEvent.MunicipalityId
                || string.IsNullOrWhiteSpace(lifecycleEvent.Reason) || lifecycleEvent.Reason.Length > 1000
                || lifecycleEvent.OccurredAt == default || lifecycleEvent.OccurredAt > DateTime.UtcNow.AddMinutes(5)
                || !CanonicalActor(actor, lifecycleEvent.ActorUserId, lifecycleEvent.MunicipalityId)
                || !SnapshotMatches(lifecycleEvent, document)
                || audits.Length != 1 || audits[0].Reason != lifecycleEvent.Reason || !transitionValid)
                throw new InvalidOperationException("Strategic-document events require one canonical lifecycle transition, exact snapshot and exact same-time actor-bound audit.");
        }

        foreach (var entry in modifiedDocuments)
        {
            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(StrategicDocument.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            var successor = addedDocuments.SingleOrDefault(item => ReferenceEquals(item.PreviousVersion, entry.Entity)
                || item.PreviousVersionId != 0 && item.PreviousVersionId == entry.Entity.Id);
            var expectedAction = changed.SetEquals([nameof(StrategicDocument.IsCurrent)]) ? StrategicDocumentEventAction.VersionCreated
                : changed.SetEquals([nameof(StrategicDocument.IsApproved), nameof(StrategicDocument.ApprovedAt),
                    nameof(StrategicDocument.ApprovedByUserId), nameof(StrategicDocument.ApprovalReference)]) ? StrategicDocumentEventAction.Approved
                : changed.SetEquals([nameof(StrategicDocument.IsPublished), nameof(StrategicDocument.PublicationDate),
                    nameof(StrategicDocument.PublishedAt), nameof(StrategicDocument.PublishedByUserId)]) ? StrategicDocumentEventAction.Published
                : changed.SetEquals([nameof(StrategicDocument.IsActive)]) ? StrategicDocumentEventAction.Retired
                : (StrategicDocumentEventAction?)null;
            var matchingEvents = expectedAction == StrategicDocumentEventAction.VersionCreated && successor != null
                ? addedEvents.Where(item => item.Action == expectedAction
                    && (ReferenceEquals(item.StrategicDocument, successor) || item.StrategicDocumentId == successor.Id)).ToArray()
                : addedEvents.Where(item => item.Action == expectedAction
                    && (ReferenceEquals(item.StrategicDocument, entry.Entity) || item.StrategicDocumentId == entry.Entity.Id)).ToArray();
            if (expectedAction == null || successor == null && expectedAction == StrategicDocumentEventAction.VersionCreated
                || matchingEvents.Length != 1)
                throw new InvalidOperationException("Strategic-document versions are append-preserved; lifecycle projections may change only through one exact same-transaction version, approval, publication or retirement event.");
        }

        foreach (var entry in modifiedBlobs)
        {
            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(EvidenceBlob.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            var documents = ChangeTracker.Entries<StrategicDocument>()
                .Where(candidate => candidate.State != EntityState.Deleted
                    && (ReferenceEquals(candidate.Entity.Blob, entry.Entity)
                        || candidate.Entity.EvidenceBlobId == entry.Entity.Id))
                .Select(candidate => candidate.Entity).ToArray();
            if (documents.Length == 0)
                documents = StrategicDocuments.IgnoreQueryFilters().AsNoTracking()
                    .Where(document => document.EvidenceBlobId == entry.Entity.Id).ToArray();
            if (documents.Length == 0) continue;
            var events = addedEvents.Where(item => item.Action == StrategicDocumentEventAction.MalwareRescanned
                && documents.Any(document => ReferenceEquals(item.StrategicDocument, document)
                    || item.StrategicDocumentId == document.Id)).ToArray();
            if (!changed.IsSubsetOf(StrategicDocumentScanProperties) || changed.Count == 0 || events.Length != 1)
                throw new InvalidOperationException("Strategic-document scan metadata may change only with one exact same-transaction malware-rescan event and audit.");
        }
    }
}

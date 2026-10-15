using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    private static readonly HashSet<string> TidScanProperties =
    [
        nameof(EvidenceBlob.ScanStatus), nameof(EvidenceBlob.IsQuarantined), nameof(EvidenceBlob.ScannerProvider),
        nameof(EvidenceBlob.ScannerReference), nameof(EvidenceBlob.ScanDetail), nameof(EvidenceBlob.ScannedAt)
    ];

    private void EnforceTidCreationAndLifecycle()
    {
        var addedVersions = ChangeTracker.Entries<TechnicalIndicatorDescription>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var modifiedVersions = ChangeTracker.Entries<TechnicalIndicatorDescription>()
            .Where(entry => entry.State == EntityState.Modified).ToArray();
        var addedDocuments = ChangeTracker.Entries<TidSourceDocument>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
        var modifiedBlobs = ChangeTracker.Entries<EvidenceBlob>()
            .Where(entry => entry.State == EntityState.Modified).ToArray();

        if (addedVersions.Length == 0 && modifiedVersions.Length == 0
            && addedDocuments.Length == 0 && modifiedBlobs.Length == 0) return;

        ApplicationUser? ResolveUser(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return ChangeTracker.Entries<ApplicationUser>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault()
                ?? Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(item => item.Id == id);
        }

        Municipality? ResolveMunicipality(long id)
        {
            if (id <= 0) return null;
            return ChangeTracker.Entries<Municipality>()
                .Where(entry => entry.State != EntityState.Deleted && entry.Entity.Id == id)
                .Select(entry => entry.Entity).SingleOrDefault()
                ?? Municipalities.IgnoreQueryFilters().AsNoTracking().SingleOrDefault(item => item.Id == id);
        }

        OpmsTarget? ResolveTarget(string? id, OpmsTarget? navigation)
        {
            var tracked = ChangeTracker.Entries<OpmsTarget>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || !string.IsNullOrWhiteSpace(id) && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (string.IsNullOrWhiteSpace(id) ? null : OpmsTargets.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id));
        }

        MunicipalEmployee? ResolveEmployee(long? id, MunicipalEmployee? navigation)
        {
            if (!id.HasValue && navigation == null) return null;
            var tracked = ChangeTracker.Entries<MunicipalEmployee>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id.HasValue && entry.Entity.Id == id.Value))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (!id.HasValue ? null : MunicipalEmployees.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id.Value));
        }

        TechnicalIndicatorDescription? ResolveVersion(long id, TechnicalIndicatorDescription? navigation)
        {
            var tracked = ChangeTracker.Entries<TechnicalIndicatorDescription>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || id != 0 && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (id == 0 ? null : TechnicalIndicatorDescriptions.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefault(item => item.Id == id));
        }

        EvidenceBlob? ResolveBlob(string? id, EvidenceBlob? navigation)
        {
            var tracked = ChangeTracker.Entries<EvidenceBlob>()
                .Where(entry => entry.State != EntityState.Deleted
                    && (navigation != null && ReferenceEquals(entry.Entity, navigation)
                        || !string.IsNullOrWhiteSpace(id) && entry.Entity.Id == id))
                .Select(entry => entry.Entity).SingleOrDefault();
            return tracked ?? (string.IsNullOrWhiteSpace(id) ? null : EvidenceBlobs.IgnoreQueryFilters().AsNoTracking()
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

        static bool RequiredText(string? value, int maximum) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && value == value.Trim();

        static bool OptionalText(string? value, int maximum) =>
            value == null || value.Length is > 0 && value.Length <= maximum && value == value.Trim();

        static bool HashIsValid(string? value) => value is { Length: 64 }
            && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');

        bool CanonicalActor(ApplicationUser? actor, string actorId, long municipalityId) =>
            actor != null && actor.IsActive && GovernanceActorMatches(actorId, _tenantContext?.UserId)
            && (!actor.MunicipalityId.HasValue || actor.MunicipalityId == municipalityId);

        foreach (var row in addedVersions)
        {
            var municipality = ResolveMunicipality(row.MunicipalityId);
            var target = ResolveTarget(row.OpmsTargetId, row.OpmsTarget);
            var actor = ResolveUser(row.CreatedByUserId);
            var employee = ResolveEmployee(row.ResponsibleEmployeeId, row.ResponsibleEmployee);
            var previous = ResolveVersion(row.PreviousVersionId ?? 0, row.PreviousVersion);
            var previousEntry = previous == null ? null : ChangeTracker.Entries<TechnicalIndicatorDescription>()
                .SingleOrDefault(entry => ReferenceEquals(entry.Entity, previous));
            var previousWasCurrent = previousEntry?.State == EntityState.Modified
                ? previousEntry.OriginalValues.GetValue<bool>(nameof(TechnicalIndicatorDescription.IsCurrent))
                : previous?.IsCurrent;
            var previousEffectiveTo = previousEntry?.State == EntityState.Modified
                ? previousEntry.OriginalValues.GetValue<DateTime?>(nameof(TechnicalIndicatorDescription.EffectiveTo))
                : previous?.EffectiveTo;
            var audits = Audits(nameof(TechnicalIndicatorDescription), row.PublicId, row.MunicipalityId,
                row.CreatedByUserId, row.CreatedAt);

            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0
                || municipality == null || !municipality.IsActive || !municipality.TidEnabled
                || target == null || target.MunicipalityId != row.MunicipalityId || target.IsWithdrawn
                || !RequiredText(row.IndicatorDefinition, 4000) || !RequiredText(row.Purpose, 4000)
                || !RequiredText(row.DataSource, 2000) || !RequiredText(row.CollectionMethod, 4000)
                || !RequiredText(row.CalculationMethod, 4000) || !RequiredText(row.VerificationMethod, 4000)
                || !OptionalText(row.NumeratorDescription, 2000) || !OptionalText(row.DenominatorDescription, 2000)
                || (row.NumeratorDescription == null) != (row.DenominatorDescription == null)
                || !OptionalText(row.Limitations, 4000) || !OptionalText(row.Assumptions, 4000)
                || !OptionalText(row.Notes, 4000)
                || row.ResponsibleEmployeeId.HasValue && (employee == null || !employee.IsActive
                    || employee.MunicipalityId != row.MunicipalityId)
                || row.EffectiveFrom == default || row.CreatedAt == default || row.CreatedAt > DateTime.UtcNow.AddMinutes(5)
                || !row.IsCurrent || row.EffectiveTo.HasValue
                || !CanonicalActor(actor, row.CreatedByUserId, row.MunicipalityId)
                || previous == null && (row.PreviousVersionId.HasValue || row.VersionNumber != 1)
                || previous != null && (previous.MunicipalityId != row.MunicipalityId
                    || previous.OpmsTargetId != row.OpmsTargetId || row.VersionNumber != previous.VersionNumber + 1
                    || previousWasCurrent != true || previousEffectiveTo.HasValue || previous.IsCurrent
                    || previous.EffectiveTo != row.EffectiveFrom.AddTicks(-1))
                || audits.Length != 1 || audits[0].Action != "CreateVersion" || audits[0].OldValue != null
                || string.IsNullOrWhiteSpace(audits[0].Reason) || audits[0].Reason!.Length > 1000
                || !ExactJson(audits[0].NewValue,
                    ("TargetPublicId", target.PublicId), ("VersionNumber", row.VersionNumber),
                    ("PreviousVersionPublicId", previous?.PublicId), ("IndicatorDefinition", row.IndicatorDefinition),
                    ("Purpose", row.Purpose), ("DataSource", row.DataSource),
                    ("CollectionMethod", row.CollectionMethod), ("CalculationMethod", row.CalculationMethod),
                    ("NumeratorDescription", row.NumeratorDescription), ("DenominatorDescription", row.DenominatorDescription),
                    ("Limitations", row.Limitations), ("Assumptions", row.Assumptions),
                    ("VerificationMethod", row.VerificationMethod), ("ResponsibleEmployeePublicId", employee?.PublicId),
                    ("Notes", row.Notes), ("EffectiveFrom", row.EffectiveFrom), ("Reason", audits[0].Reason)))
                throw new InvalidOperationException("TID version creation requires canonical tenant, target, actor and lineage evidence plus one exact same-time reasoned audit.");
        }

        foreach (var entry in modifiedVersions)
        {
            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(TechnicalIndicatorDescription.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            var successor = addedVersions.SingleOrDefault(item => ReferenceEquals(item.PreviousVersion, entry.Entity)
                || item.PreviousVersionId != 0 && item.PreviousVersionId == entry.Entity.Id);
            if (!changed.SetEquals([nameof(TechnicalIndicatorDescription.IsCurrent), nameof(TechnicalIndicatorDescription.EffectiveTo)])
                || successor == null
                || entry.OriginalValues.GetValue<bool>(nameof(TechnicalIndicatorDescription.IsCurrent)) != true
                || entry.CurrentValues.GetValue<bool>(nameof(TechnicalIndicatorDescription.IsCurrent)) != false
                || entry.OriginalValues.GetValue<DateTime?>(nameof(TechnicalIndicatorDescription.EffectiveTo)).HasValue
                || entry.CurrentValues.GetValue<DateTime?>(nameof(TechnicalIndicatorDescription.EffectiveTo))
                    != successor.EffectiveFrom.AddTicks(-1))
                throw new InvalidOperationException("TID versions are append-only; a current predecessor may close only with one exact same-transaction successor.");
        }

        foreach (var row in addedDocuments)
        {
            var version = ResolveVersion(row.TechnicalIndicatorDescriptionId, row.TechnicalIndicatorDescription);
            var blob = ResolveBlob(row.EvidenceBlobId, row.Blob);
            var uploader = ResolveUser(row.UploadedByUserId);
            var blobEntry = blob == null ? null : ChangeTracker.Entries<EvidenceBlob>()
                .SingleOrDefault(entry => ReferenceEquals(entry.Entity, blob));
            var audits = Audits(nameof(TidSourceDocument), row.PublicId, row.MunicipalityId,
                row.UploadedByUserId, row.UploadedAt);
            var cleanStateConsistent = blob != null
                && (string.Equals(blob.ScanStatus, "Clean", StringComparison.OrdinalIgnoreCase) ? !blob.IsQuarantined : blob.IsQuarantined);

            if (row.PublicId == Guid.Empty || row.MunicipalityId <= 0
                || version == null || version.MunicipalityId != row.MunicipalityId
                || blob == null || blob.MunicipalityId != row.MunicipalityId || blobEntry?.State != EntityState.Added
                || string.IsNullOrWhiteSpace(blob.Id) || blob.PublicId == Guid.Empty
                || !RequiredText(row.Title, 240) || !RequiredText(row.FileName, 260)
                || row.FileName != Path.GetFileName(row.FileName) || row.FileName.Contains('/') || row.FileName.Contains('\\')
                || row.UploadedAt == default || row.UploadedAt > DateTime.UtcNow.AddMinutes(5)
                || !CanonicalActor(uploader, row.UploadedByUserId, row.MunicipalityId)
                || !RequiredText(blob.StorageKey, 1000) || string.IsNullOrWhiteSpace(blob.ContentType)
                || blob.SizeInBytes is <= 0 or > 25L * 1024 * 1024 || !HashIsValid(blob.Sha256)
                || !blob.SignatureVerified || !RequiredText(blob.ScanStatus, 40) || !cleanStateConsistent
                || blob.ScannerProvider?.Length > 120 || blob.ScannerReference?.Length > 240 || blob.ScanDetail?.Length > 1000
                || blob.ScannedAt != row.UploadedAt || blob.CreatedAt != row.UploadedAt
                || blob.IsContentDeleted || blob.ContentDeletedAt.HasValue
                || audits.Length != 1 || audits[0].Action != "Upload" || audits[0].OldValue != null
                || string.IsNullOrWhiteSpace(audits[0].Reason) || audits[0].Reason!.Length > 1000
                || !ExactJson(audits[0].NewValue,
                    ("TidPublicId", version.PublicId), ("BlobPublicId", blob.PublicId),
                    ("Title", row.Title), ("FileName", row.FileName), ("ContentType", blob.ContentType),
                    ("SizeInBytes", blob.SizeInBytes), ("Sha256", blob.Sha256),
                    ("ScanStatus", blob.ScanStatus), ("IsQuarantined", blob.IsQuarantined),
                    ("Reason", audits[0].Reason)))
                throw new InvalidOperationException("TID source-document creation requires a new validated same-tenant blob, canonical uploader and one exact same-time reasoned audit.");
        }

        foreach (var entry in modifiedBlobs)
        {
            var changed = entry.Properties.Where(property => property.IsModified)
                .Select(property => property.Metadata.Name)
                .Where(property => property != nameof(EvidenceBlob.RowVersion))
                .ToHashSet(StringComparer.Ordinal);
            var documents = ChangeTracker.Entries<TidSourceDocument>()
                .Where(candidate => candidate.State != EntityState.Deleted
                    && (ReferenceEquals(candidate.Entity.Blob, entry.Entity)
                        || candidate.Entity.EvidenceBlobId == entry.Entity.Id))
                .Select(candidate => candidate.Entity).ToArray();
            if (documents.Length == 0)
                documents = TidSourceDocuments.IgnoreQueryFilters().AsNoTracking()
                    .Where(document => document.EvidenceBlobId == entry.Entity.Id).ToArray();
            if (documents.Length == 0) continue;

            var scannedAt = entry.CurrentValues.GetValue<DateTime?>(nameof(EvidenceBlob.ScannedAt));
            var reason = entry.CurrentValues.GetValue<string?>(nameof(EvidenceBlob.ScanDetail))
                ?? entry.CurrentValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus));
            var validAudits = scannedAt.HasValue
                ? ChangeTracker.Entries<AuditTrail>().Where(candidate => candidate.State == EntityState.Added)
                    .Select(candidate => candidate.Entity)
                    .Where(audit => audit.EntityName == nameof(TidSourceDocument)
                        && documents.Any(document => audit.EntityId == document.PublicId.ToString()
                            && audit.MunicipalityId == document.MunicipalityId)
                        && audit.ChangedAt == scannedAt.Value && audit.Action == "MalwareRescan")
                    .ToArray()
                : [];
            var audit = validAudits.SingleOrDefault();
            var actor = audit == null ? null : ResolveUser(audit.ChangedBy);

            if (!changed.IsSubsetOf(TidScanProperties) || changed.Count == 0 || documents.Length != 1
                || !scannedAt.HasValue || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000
                || validAudits.Length != 1 || audit == null
                || !CanonicalActor(actor, audit.ChangedBy, documents[0].MunicipalityId)
                || audit.Reason != reason || !ExactJson(audit.OldValue,
                    ("ScanStatus", entry.OriginalValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus))),
                    ("IsQuarantined", entry.OriginalValues.GetValue<bool>(nameof(EvidenceBlob.IsQuarantined))),
                    ("ScannerReference", entry.OriginalValues.GetValue<string?>(nameof(EvidenceBlob.ScannerReference))))
                || !ExactJson(audit.NewValue,
                    ("Status", entry.CurrentValues.GetValue<string>(nameof(EvidenceBlob.ScanStatus))),
                    ("IsQuarantined", entry.CurrentValues.GetValue<bool>(nameof(EvidenceBlob.IsQuarantined))),
                    ("ProviderReference", entry.CurrentValues.GetValue<string?>(nameof(EvidenceBlob.ScannerReference))),
                    ("Reason", reason)))
                throw new InvalidOperationException("TID source-document scan metadata may change only with one exact same-time actor-bound malware-rescan audit.");
        }
    }
}

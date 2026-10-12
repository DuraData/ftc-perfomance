using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;

namespace FTCERP.Tests;

public sealed class PoePersistenceGovernanceTests
{
    [Theory]
    [InlineData("assessment")]
    [InlineData("replacement")]
    [InlineData("hold")]
    [InlineData("disposal")]
    public async Task Poe_child_history_cannot_claim_a_different_municipality_than_its_parent(string historyKind)
    {
        await using var context = await CreateContext(includeReplacement: historyKind == "replacement");
        context.Municipalities.Add(new Municipality { Id = 99002, Code = "OTHER", Name = "Other Municipality" });
        await context.SaveChangesAsync();
        var actor = await context.Users.SingleAsync();

        switch (historyKind)
        {
            case "assessment":
                context.PoeEvidenceAssessments.Add(new PoeEvidenceAssessment
                {
                    MunicipalityId = 99002, PoeFileId = "poe-original", Outcome = PoeAssessmentOutcome.Accepted,
                    AssessedByUserId = actor.Id, CorrelationId = "cross-tenant-assessment"
                });
                break;
            case "replacement":
                var rows = await context.PoeFiles.OrderBy(item => item.Id).ToArrayAsync();
                rows[0].IsActive = false;
                rows[1].SupersedesPoeFileId = rows[0].Id;
                context.PoeEvidenceReplacements.Add(new PoeEvidenceReplacement
                {
                    MunicipalityId = 99002, SupersededPoeFileId = rows[0].Id, ReplacementPoeFileId = rows[1].Id,
                    Reason = "Cross-tenant replacement attempt", ReplacedByUserId = actor.Id,
                    CorrelationId = "cross-tenant-replacement"
                });
                break;
            case "hold":
                context.PoeLegalHoldEvents.Add(new PoeLegalHoldEvent
                {
                    HoldId = Guid.NewGuid(), MunicipalityId = 99002, PoeFileId = "poe-original",
                    Action = PoeLegalHoldAction.Placed, HoldReference = "CROSS-TENANT", Reason = "Attempt",
                    ActorUserId = actor.Id, CorrelationId = "cross-tenant-hold"
                });
                break;
            case "disposal":
                context.PoeDisposalEvents.Add(new PoeDisposalEvent
                {
                    DisposalId = Guid.NewGuid(), MunicipalityId = 99002, PoeFileId = "poe-original",
                    Action = PoeDisposalAction.Requested, ApprovalReference = "CROSS-TENANT", Reason = "Attempt",
                    ActorUserId = actor.Id, CorrelationId = "cross-tenant-disposal"
                });
                break;
        }

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*municipality*");
    }

    [Fact]
    public async Task Poe_association_identity_and_file_metadata_cannot_be_rewritten_or_deleted()
    {
        await using var context = await CreateContext();
        var file = await context.PoeFiles.SingleAsync();

        file.FileName = "rewritten.pdf";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*metadata*immutable*");

        context.ChangeTracker.Clear();
        file = await context.PoeFiles.SingleAsync();
        context.PoeFiles.Remove(file);
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Poe_association_retirement_and_predecessor_lineage_require_one_atomic_replacement_record()
    {
        await using var context = await CreateContext(includeReplacement: true);
        var rows = await context.PoeFiles.OrderBy(item => item.Id).ToArrayAsync();
        var superseded = rows[0];
        var replacement = rows[1];

        superseded.IsActive = false;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*replacement record*");

        context.ChangeTracker.Clear();
        rows = await context.PoeFiles.OrderBy(item => item.Id).ToArrayAsync();
        superseded = rows[0];
        replacement = rows[1];
        replacement.SupersedesPoeFileId = superseded.Id;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*replacement record*");

        context.ChangeTracker.Clear();
        rows = await context.PoeFiles.OrderBy(item => item.Id).ToArrayAsync();
        superseded = rows[0];
        replacement = rows[1];
        var actor = await context.Users.SingleAsync();
        superseded.IsActive = false;
        replacement.SupersedesPoeFileId = superseded.Id;
        context.PoeEvidenceReplacements.Add(new PoeEvidenceReplacement
        {
            MunicipalityId = superseded.MunicipalityId!.Value,
            SupersededPoeFileId = superseded.Id,
            ReplacementPoeFileId = replacement.Id,
            Reason = "Corrected signed evidence",
            ReplacedByUserId = actor.Id,
            ReplacedAt = DateTime.UtcNow,
            CorrelationId = "poe-replacement-test"
        });

        await context.SaveChangesAsync();
        (await context.PoeFiles.SingleAsync(item => item.Id == superseded.Id)).IsActive.Should().BeFalse();
        (await context.PoeFiles.SingleAsync(item => item.Id == replacement.Id)).SupersedesPoeFileId.Should().Be(superseded.Id);
        (await context.PoeEvidenceReplacements.CountAsync()).Should().Be(1);

        context.ChangeTracker.Clear();
        superseded = await context.PoeFiles.SingleAsync(item => item.Id == superseded.Id);
        superseded.IsActive = true;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*retired only by one append-only replacement record*");
    }

    [Fact]
    public async Task Evidence_blob_allows_monotonic_rescan_but_rejects_identity_rewrite_and_hard_delete()
    {
        await using var context = await CreateContext();
        var blob = await context.EvidenceBlobs.SingleAsync();
        var firstScan = blob.ScannedAt!.Value;

        blob.ScanStatus = "ThreatDetected";
        blob.IsQuarantined = true;
        blob.ScannerProvider = "GovernedScanner";
        blob.ScannerReference = "scan-2";
        blob.ScanDetail = "Threat signature found";
        blob.ScannedAt = firstScan.AddMinutes(1);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        blob = await context.EvidenceBlobs.SingleAsync();
        blob.StorageKey = "rewritten/storage-key";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*storage location*immutable*");

        context.ChangeTracker.Clear();
        blob = await context.EvidenceBlobs.SingleAsync();
        context.EvidenceBlobs.Remove(blob);
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*cannot be hard deleted*");
    }

    [Fact]
    public async Task Evidence_content_deletion_requires_one_completed_disposal_event_and_cannot_be_reversed()
    {
        await using var context = await CreateContext();
        var file = await context.PoeFiles.Include(item => item.Blob).SingleAsync();
        file.IsActive = false;
        var actor = await context.Users.SingleAsync();
        var replacement = new PoeFile
        {
            Id = "poe-successor", MunicipalityId = file.MunicipalityId, SubmissionKind = file.SubmissionKind,
            SubmissionId = file.SubmissionId, FileName = "successor.pdf", UploadedByUserId = actor.Id,
            SupersedesPoeFileId = file.Id,
            Blob = NewBlob(file.MunicipalityId!.Value, "successor")
        };
        context.PoeFiles.Add(replacement);
        context.PoeEvidenceReplacements.Add(new PoeEvidenceReplacement
        {
            MunicipalityId = file.MunicipalityId.Value, SupersededPoeFileId = file.Id,
            ReplacementPoeFileId = replacement.Id, Reason = "Superseded before retention disposal",
            ReplacedByUserId = actor.Id, CorrelationId = "replacement-before-disposal"
        });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        file = await context.PoeFiles.Include(item => item.Blob).SingleAsync(item => item.Id == "poe-original");
        file.Blob.IsContentDeleted = true;
        file.Blob.ContentDeletedAt = DateTime.UtcNow;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*completed governed disposal event*");

        context.ChangeTracker.Clear();
        file = await context.PoeFiles.Include(item => item.Blob).SingleAsync(item => item.Id == "poe-original");
        actor = await context.Users.SingleAsync();
        var disposalId = Guid.NewGuid();
        context.PoeDisposalEvents.Add(new PoeDisposalEvent
        {
            DisposalId = disposalId, MunicipalityId = file.MunicipalityId!.Value, PoeFileId = file.Id,
            Action = PoeDisposalAction.Requested, Reason = "Retention period expired", ApprovalReference = "COUNCIL-1",
            ActorUserId = actor.Id, CorrelationId = "dispose-request"
        });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        file = await context.PoeFiles.Include(item => item.Blob).SingleAsync(item => item.Id == "poe-original");
        file.Blob.IsContentDeleted = true;
        file.Blob.ContentDeletedAt = DateTime.UtcNow;
        context.PoeDisposalEvents.Add(new PoeDisposalEvent
        {
            DisposalId = disposalId, MunicipalityId = file.MunicipalityId!.Value, PoeFileId = file.Id,
            Action = PoeDisposalAction.Completed, Reason = "Retention period expired", ApprovalReference = "COUNCIL-1",
            ActorUserId = actor.Id, CorrelationId = "dispose-complete", Detail = "Physical content removed"
        });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var deleted = await context.EvidenceBlobs.SingleAsync(item => item.Id == file.EvidenceBlobId);
        deleted.IsContentDeleted.Should().BeTrue();
        deleted.ContentDeletedAt.Should().NotBeNull();
        deleted.IsContentDeleted = false;
        deleted.ContentDeletedAt = null;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should()
            .ThrowAsync<InvalidOperationException>().WithMessage("*only once*");
    }

    private static async Task<ApplicationDbContext> CreateContext(bool includeReplacement = false)
    {
        var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Id = 99001, Code = "POE-GOV", Name = "POE Governance" };
        var actor = IdpTestFixture.CreateUser("poe-governance-actor");
        actor.MunicipalityId = municipality.Id;
        var original = new PoeFile
        {
            Id = "poe-original", MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms,
            SubmissionId = "submission-1", FileName = "original.pdf", RetainUntil = DateTime.UtcNow.AddDays(-1),
            UploadedByUserId = actor.Id, Blob = NewBlob(municipality.Id, "original")
        };
        context.AddRange(municipality, actor, original);
        if (includeReplacement)
        {
            context.PoeFiles.Add(new PoeFile
            {
                Id = "poe-replacement", MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms,
                SubmissionId = "submission-1", FileName = "replacement.pdf", RetainUntil = DateTime.UtcNow.AddYears(7),
                UploadedByUserId = actor.Id, Blob = NewBlob(municipality.Id, "replacement")
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return context;
    }

    private static EvidenceBlob NewBlob(long municipalityId, string suffix) => new()
    {
        Id = $"blob-{suffix}", MunicipalityId = municipalityId, StorageKey = $"poe/{suffix}.pdf",
        ContentType = "application/pdf", SizeInBytes = 128, Sha256 = new string('a', 64),
        SignatureVerified = true, ScanStatus = "Clean", ScannerProvider = "GovernedScanner",
        ScannerReference = $"scan-{suffix}", ScannedAt = DateTime.UtcNow
    };
}

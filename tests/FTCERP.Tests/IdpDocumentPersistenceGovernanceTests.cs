namespace FTCERP.Tests;

public sealed class IdpDocumentPersistenceGovernanceTests
{
    [Fact]
    public async Task Document_metadata_rewrite_is_rejected_at_the_persistence_boundary()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var document = await SeedDocumentAsync(context);
        document.Title = "Rewritten title";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IDP document identity*immutable*new governed document version*");
    }

    [Fact]
    public async Task Document_plan_version_and_blob_associations_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var document = await SeedDocumentAsync(context);
        var replacementBlob = CreateBlob("replacement.pdf", 'b');
        context.EvidenceBlobs.Add(replacementBlob);
        document.Blob = replacementBlob;

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IDP document identity*immutable*new governed document version*");
    }

    [Fact]
    public async Task Document_hard_delete_is_rejected_at_the_persistence_boundary()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var document = await SeedDocumentAsync(context);
        context.IdpDocuments.Remove(document);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IDP document identity*immutable*new governed document version*");
    }

    [Fact]
    public async Task New_document_version_is_added_without_rewriting_prior_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var original = await SeedDocumentAsync(context);
        var successor = new IdpDocument
        {
            IdpPlanId = original.IdpPlanId,
            IdpPlanVersionId = original.IdpPlanVersionId,
            Category = original.Category,
            Title = original.Title,
            FileName = "approved-idp-v2.pdf",
            Blob = CreateBlob("approved-idp-v2.pdf", 'b'),
            RetainUntil = original.RetainUntil?.AddYears(1),
            VersionNumber = 2,
            IsApproved = true,
            UploadedAt = original.UploadedAt.AddDays(1),
            UploadedByUserId = original.UploadedByUserId
        };
        context.IdpDocuments.Add(successor);

        await context.SaveChangesAsync();

        var versions = await context.IdpDocuments.AsNoTracking()
            .OrderBy(item => item.VersionNumber)
            .ToListAsync();
        versions.Should().HaveCount(2);
        versions.Select(item => item.VersionNumber).Should().Equal(1, 2);
        versions[0].FileName.Should().Be("approved-idp-v1.pdf");
        versions[1].FileName.Should().Be("approved-idp-v2.pdf");
        versions.Select(item => item.PublicId).Should().OnlyHaveUniqueItems();
        versions.Select(item => item.EvidenceBlobId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Evidence_rescan_state_can_advance_without_rewriting_document_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var document = await SeedDocumentAsync(context);
        var originalDocumentRowVersion = document.RowVersion.ToArray();
        var scannedAt = document.Blob.ScannedAt!.Value.AddMinutes(5);
        document.Blob.ScanStatus = "Clean";
        document.Blob.IsQuarantined = false;
        document.Blob.ScannerProvider = "GovernedScanner";
        document.Blob.ScannerReference = "scan-2";
        document.Blob.ScanDetail = "Clean rescan";
        document.Blob.ScannedAt = scannedAt;

        await context.SaveChangesAsync();

        context.Entry(document).State.Should().Be(EntityState.Unchanged);
        document.RowVersion.Should().Equal(originalDocumentRowVersion);
        document.Blob.ScanStatus.Should().Be("Clean");
        document.Blob.IsQuarantined.Should().BeFalse();
        document.Blob.ScannedAt.Should().Be(scannedAt);
    }

    private static async Task<IdpDocument> SeedDocumentAsync(ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser($"idp-document-governor-{Guid.NewGuid():N}");
        var effectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "Governed IDP",
            PlanCode = $"IDP-{Guid.NewGuid():N}",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CurrentVersionNumber = 1,
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = user.Id
        };
        var planVersion = new IdpPlanVersion
        {
            IdpPlan = plan,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original",
            EffectiveFrom = effectiveFrom,
            CreatedByUserId = user.Id,
            IsActive = true
        };
        var document = new IdpDocument
        {
            IdpPlan = plan,
            IdpPlanVersion = planVersion,
            Category = IdpDocumentCategory.SignedIdp,
            Title = "Approved IDP",
            FileName = "approved-idp-v1.pdf",
            Blob = CreateBlob("approved-idp-v1.pdf", 'a'),
            RetainUntil = effectiveFrom.AddYears(7),
            VersionNumber = 1,
            IsApproved = false,
            UploadedAt = effectiveFrom.AddDays(1),
            UploadedByUser = user,
            UploadedByUserId = user.Id
        };
        context.AddRange(user, plan, planVersion, document);
        await context.SaveChangesAsync();
        return document;
    }

    private static EvidenceBlob CreateBlob(string storageKey, char hashCharacter) => new()
    {
        StorageKey = $"idp/{storageKey}",
        ContentType = "application/pdf",
        SizeInBytes = 128,
        Sha256 = new string(hashCharacter, 64),
        SignatureVerified = true,
        ScanStatus = "ScannerUnavailable",
        IsQuarantined = true,
        ScannerProvider = "GovernedScanner",
        ScannerReference = "scan-1",
        ScanDetail = "Pending clean rescan",
        ScannedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
        CreatedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc)
    };
}

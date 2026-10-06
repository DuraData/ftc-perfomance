namespace FTCERP.Tests;

public class StrategicDocumentsControllerTests
{
    [Fact]
    public async Task ControlledTypes_AreTenantScopedAuditedAndConcurrencyProtected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = Controller(context, setup.User, setup.Municipality.Id, workflow: workflow);

        var created = Payload(await controller.CreateType(new SaveStrategicDocumentTypeRequest(
            "policy", "Municipal Policy", "Approved policies", true, true, 10, null, "Create controlled type")));
        created.Code.Should().Be("POLICY");
        created.AllowsExternalLinks.Should().BeTrue();

        var updated = Payload(await controller.UpdateType(created.PublicId, new SaveStrategicDocumentTypeRequest(
            "policy", "Council Policy", "Council-approved policies", true, true, 5, created.RowVersion, "Refine type")));
        updated.Name.Should().Be("Council Policy");
        var stale = await controller.UpdateType(created.PublicId, new SaveStrategicDocumentTypeRequest(
            "policy", "Stale", null, true, true, 5, created.RowVersion, "Stale type edit"));
        stale.Result.Should().BeOfType<ConflictObjectResult>();
        workflow.Verify(service => service.QueueAuditTrail(nameof(StrategicDocumentType), created.PublicId.ToString(), "Create",
            null, It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Once);
        workflow.Verify(service => service.QueueAuditTrail(nameof(StrategicDocumentType), created.PublicId.ToString(), "Update",
            It.IsAny<object>(), It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExternalDocument_VersionsRemainIndependentAndPublishedViewUsesLatestPublishedVersion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, allowsExternalLinks: true);
        var manager = Controller(context, setup.User, setup.Municipality.Id);

        var first = Payload(await manager.CreateVersion(ExternalRequest(setup, "Approved IDP", "https://example.gov.za/idp-2026.pdf")));
        first.VersionNumber.Should().Be(1);
        first.IsApproved.Should().BeFalse();
        first.ExternalUrl.Should().Be("https://example.gov.za/idp-2026.pdf");

        var approved = Payload(await manager.Approve(first.PublicId, new ApproveStrategicDocumentRequest(first.RowVersion, "Council 17/2026", "Council approval")));
        var published = Payload(await manager.Publish(first.PublicId, new PublishStrategicDocumentRequest(approved.RowVersion, DateTime.UtcNow.AddMinutes(-1), "Publish approved plan")));
        published.IsPublished.Should().BeTrue();
        published.Events.Select(item => item.Action).Should().Equal("VersionCreated", "Approved", "Published");

        var second = Payload(await manager.CreateVersion(ExternalRequest(setup, "IDP annual review", "https://example.gov.za/idp-2027.pdf", published)));
        second.VersionNumber.Should().Be(2);
        second.PreviousVersionPublicId.Should().Be(first.PublicId);
        var ordinary = Controller(context, setup.User, setup.Municipality.Id, manager: false);
        var whileDraft = Payload(await ordinary.GetDocumentsPage(new PagedQueryRequest { PageSize = 100 }, setup.Year.PublicId));
        whileDraft.Items.Should().ContainSingle().Which.PublicId.Should().Be(first.PublicId);

        var secondApproved = Payload(await manager.Approve(second.PublicId, new ApproveStrategicDocumentRequest(second.RowVersion, "Council 18/2027", "Approve annual review")));
        var secondPublished = Payload(await manager.Publish(second.PublicId, new PublishStrategicDocumentRequest(secondApproved.RowVersion, DateTime.UtcNow.AddMinutes(-1), "Publish annual review")));
        var currentPublished = Payload(await ordinary.GetDocumentsPage(new PagedQueryRequest { PageSize = 100 }, setup.Year.PublicId));
        currentPublished.Items.Should().ContainSingle().Which.PublicId.Should().Be(secondPublished.PublicId);
        var page = Payload(await ordinary.GetDocumentsPage(new PagedQueryRequest
        {
            Page = 1, PageSize = 1, Search = "annual", SortBy = "title", SortDirection = "asc"
        }, setup.Year.PublicId));
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.PublicId.Should().Be(secondPublished.PublicId);

        var persisted = await context.StrategicDocuments.OrderBy(item => item.VersionNumber).ToArrayAsync();
        persisted[0].IsCurrent.Should().BeFalse();
        persisted[1].DocumentFamilyId.Should().Be(persisted[0].DocumentFamilyId);
        persisted[1].PreviousVersionId.Should().Be(persisted[0].Id);
        persisted.Select(item => item.Title).Should().Equal("Approved IDP", "IDP annual review");
        context.ChangeTracker.Clear();
        var historical = await context.StrategicDocuments.SingleAsync(item => item.VersionNumber == 1);
        historical.Title = "Tampered";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-preserved*");
    }

    [Fact]
    public async Task ManagedFile_IsPrivateScannedAndCannotPublishUntilClean()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var bytes = "%PDF-strategic"u8.ToArray();
        var storage = new Mock<IEvidenceBlobStorage>();
        storage.Setup(service => service.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceStorageOperationResult(true, "stored"));
        storage.Setup(service => service.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceStorageReadResult(true, bytes, "read"));
        var inspection = new Mock<IEvidenceInspectionService>();
        inspection.Setup(service => service.Inspect(It.IsAny<byte[]>(), ".pdf"))
            .Returns(new EvidenceInspectionResult(true, new string('b', 64), "SignatureValidated", null));
        var scanner = new Mock<IEvidenceMalwareScanner>();
        scanner.SetupSequence(service => service.ScanAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "application/pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceMalwareScanResult("ThreatDetected", false, "test-scanner", "scan-1", "Threat"))
            .ReturnsAsync(new EvidenceMalwareScanResult("Clean", true, "test-scanner", "scan-2", null));
        var manager = Controller(context, setup.User, setup.Municipality.Id, storage: storage, inspection: inspection, scanner: scanner);
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "strategy.pdf") { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
        var request = FileRequest(setup, "Spatial development framework", file);

        var created = Payload(await manager.CreateVersion(request));
        created.IsQuarantined.Should().BeTrue();
        created.ScanStatus.Should().Be("ThreatDetected");
        (await manager.Download(created.PublicId)).Should().BeOfType<NotFoundResult>();
        var blockedApproval = await manager.Approve(created.PublicId, new ApproveStrategicDocumentRequest(created.RowVersion, "Council 20/2026", "Approve"));
        blockedApproval.Result.Should().BeOfType<ConflictObjectResult>();

        var rescanned = Payload(await manager.Rescan(created.PublicId));
        rescanned.IsQuarantined.Should().BeFalse();
        var approved = Payload(await manager.Approve(created.PublicId, new ApproveStrategicDocumentRequest(rescanned.RowVersion, "Council 20/2026", "Approve clean document")));
        var published = Payload(await manager.Publish(created.PublicId, new PublishStrategicDocumentRequest(approved.RowVersion, DateTime.UtcNow.AddMinutes(-1), "Publish")));
        var ordinary = Controller(context, setup.User, setup.Municipality.Id, manager: false, storage: storage, inspection: inspection, scanner: scanner);
        var rows = Payload(await ordinary.GetDocumentsPage(new PagedQueryRequest { PageSize = 100 }, setup.Year.PublicId));
        rows.Items.Should().ContainSingle().Which.Title.Should().Be("Spatial development framework");
        var download = await ordinary.Download(published.PublicId);
        download.Should().BeOfType<FileContentResult>().Which.FileContents.Should().Equal(bytes);
        (await context.EvidenceBlobs.SingleAsync()).StorageKey.Should().StartWith($"strategic-documents{Path.DirectorySeparatorChar}");
    }

    [Fact]
    public async Task TenantFilterAndDirectApiAuthorization_PreventCrossMunicipalityAccess()
    {
        var database = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options;
        await using (var system = new ApplicationDbContext(options, new FixedTenantContext(null, true)))
        {
            var user = IdpTestFixture.CreateUser("documents-system");
            var a = new Municipality { Id = 8101, Code = "A", Name = "A" };
            var b = new Municipality { Id = 8102, Code = "B", Name = "B" };
            system.AddRange(user, a, b);
            system.StrategicDocumentTypes.AddRange(Type(a.Id, user.Id, "A"), Type(b.Id, user.Id, "B"));
            await system.SaveChangesAsync();
        }
        await using var tenantA = new ApplicationDbContext(options, new FixedTenantContext(8101, false));
        (await tenantA.StrategicDocumentTypes.Select(item => item.Code).ToArrayAsync()).Should().Equal("A");

        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, allowsExternalLinks: true);
        var denied = Controller(context, setup.User, setup.Municipality.Id, allow: false);
        var response = await denied.CreateVersion(ExternalRequest(setup, "Denied", "https://example.gov.za/denied.pdf"));
        response.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await context.StrategicDocuments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Document_page_rejects_unknown_sort_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context);
        var controller = Controller(context, setup.User, setup.Municipality.Id);

        var response = await controller.GetDocumentsPage(new PagedQueryRequest { SortBy = "raw-sql" }, setup.Year.PublicId);

        response.Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetDocuments(setup.Year.PublicId).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static StrategicDocumentsController Controller(
        ApplicationDbContext context,
        ApplicationUser user,
        long municipalityId,
        Mock<IWorkflowGovernanceService>? workflow = null,
        Mock<IEvidenceBlobStorage>? storage = null,
        Mock<IEvidenceInspectionService>? inspection = null,
        Mock<IEvidenceMalwareScanner>? scanner = null,
        bool allow = true,
        bool manager = true)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string permission, AccessScopeContext? _) =>
            {
                var granted = allow && (manager || permission == "STRATEGIC_DOCUMENT.READ");
                return new AccessDecisionResult(granted, granted ? "Allowed" : "Denied", [], [], []);
            });
        storage ??= new Mock<IEvidenceBlobStorage>();
        inspection ??= new Mock<IEvidenceInspectionService>();
        scanner ??= new Mock<IEvidenceMalwareScanner>();
        return new StrategicDocumentsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object,
            (workflow ?? new Mock<IWorkflowGovernanceService>()).Object, new FixedTenantContext(municipalityId, false),
            storage.Object, inspection.Object, scanner.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static CreateStrategicDocumentVersionRequest ExternalRequest(
        (ApplicationUser User, Municipality Municipality, MunicipalityFinancialYear Year, StrategicDocumentType Type) setup,
        string title,
        string url,
        StrategicDocumentResponse? previous = null) => new()
    {
        MunicipalityFinancialYearPublicId = setup.Year.PublicId,
        DocumentTypePublicId = setup.Type.PublicId,
        PreviousVersionPublicId = previous?.PublicId,
        PreviousVersionRowVersion = previous?.RowVersion,
        SdbipLayer = "Top Layer",
        Title = title,
        Description = "Governed strategy",
        DocumentDate = DateTime.UtcNow.Date,
        DisplayOrder = 10,
        ExternalUrl = url,
        Reason = "Council-governed version"
    };

    private static CreateStrategicDocumentVersionRequest FileRequest(
        (ApplicationUser User, Municipality Municipality, MunicipalityFinancialYear Year, StrategicDocumentType Type) setup,
        string title,
        IFormFile file) => new()
    {
        MunicipalityFinancialYearPublicId = setup.Year.PublicId,
        DocumentTypePublicId = setup.Type.PublicId,
        Title = title,
        Description = "Managed strategic document",
        DocumentDate = DateTime.UtcNow.Date,
        DisplayOrder = 20,
        File = file,
        Reason = "Upload governed strategy"
    };

    private static async Task<(ApplicationUser User, Municipality Municipality, MunicipalityFinancialYear Year, StrategicDocumentType Type)> SeedAsync(
        ApplicationDbContext context,
        bool allowsExternalLinks = false)
    {
        var user = IdpTestFixture.CreateUser("strategic-documents-owner");
        var municipality = new Municipality { Id = 8001, PublicId = Guid.NewGuid(), Code = "DOC", Name = "Documents Municipality" };
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) };
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, Municipality = municipality, FinancialYear = financialYear, IsActive = true, IsCurrent = true, EffectiveFrom = financialYear.StartDate };
        var type = Type(municipality.Id, user.Id, "IDP", allowsExternalLinks);
        context.AddRange(user, municipality, financialYear, year, type);
        await context.SaveChangesAsync();
        return (user, municipality, year, type);
    }

    private static StrategicDocumentType Type(long municipalityId, string userId, string code, bool allowsExternalLinks = false) => new()
    {
        MunicipalityId = municipalityId,
        Code = code,
        Name = code + " Documents",
        AllowsExternalLinks = allowsExternalLinks,
        IsActive = true,
        CreatedByUserId = userId
    };

    private static T Payload<T>(ActionResult<ApiResponse<T>> action) =>
        ((action.Result as OkObjectResult)!.Value as ApiResponse<T>)!.Data!;

    private sealed class FixedTenantContext(long? municipalityId, bool isSystem) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => "strategic-documents-test";
    }
}

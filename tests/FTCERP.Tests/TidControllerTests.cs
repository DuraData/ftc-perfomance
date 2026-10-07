namespace FTCERP.Tests;

public class TidControllerTests
{
    [Fact]
    public async Task Configuration_IsOptionalConcurrentAudited_AndReportsMissingKpis()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, enabled: false, allRequired: false);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = Controller(context, setup.User, setup.Municipality.Id, workflow);

        var initial = Payload(await controller.GetConfiguration());
        initial.TidEnabled.Should().BeFalse();
        initial.MissingTidCount.Should().Be(0);

        var updated = Payload(await controller.UpdateConfiguration(new UpdateTidConfigurationRequest(
            true, true, initial.RowVersion, "Council adopted mandatory TIDs")));
        updated.TidEnabled.Should().BeTrue();
        updated.AllKpisRequired.Should().BeTrue();
        updated.ScopedKpiCount.Should().Be(1);
        updated.MissingTidCount.Should().Be(1);
        workflow.Verify(service => service.QueueAuditTrail("TidConfiguration", setup.Municipality.PublicId.ToString(), "Update",
            It.IsAny<object>(), It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Once);

        var stale = await controller.UpdateConfiguration(new UpdateTidConfigurationRequest(false, false, initial.RowVersion, "Stale change"));
        stale.Result.Should().BeOfType<ConflictObjectResult>();
        (await context.Municipalities.SingleAsync()).TidEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Versioning_PreservesLineageAndKpiIndependence_WithRelationalConstraints()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, enabled: true, allRequired: true);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = Controller(context, setup.User, setup.Municipality.Id, workflow);

        var first = Payload(await controller.CreateVersion(setup.Target.PublicId, Request("Initial definition", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc))));
        first.VersionNumber.Should().Be(1);
        first.IsCurrent.Should().BeTrue();
        first.PreviousVersionPublicId.Should().BeNull();

        var second = Payload(await controller.CreateVersion(setup.Target.PublicId,
            Request("Revised definition", new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc), first.RowVersion)));
        second.VersionNumber.Should().Be(2);
        second.PreviousVersionPublicId.Should().Be(first.PublicId);

        var versions = await context.TechnicalIndicatorDescriptions.OrderBy(item => item.VersionNumber).ToArrayAsync();
        versions[0].IsCurrent.Should().BeFalse();
        versions[0].EffectiveTo.Should().Be(new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1));
        versions[1].IsCurrent.Should().BeTrue();
        versions[1].PreviousVersionId.Should().Be(versions[0].Id);
        (await context.OpmsTargets.SingleAsync()).Should().Match<OpmsTarget>(item => item.AnnualTarget == 100 && item.TargetUnitType == "percentage" && item.Submissions.Count == 0);

        var page = Payload(await controller.GetRegisterPage(new PagedQueryRequest
        {
            Page = 1, PageSize = 1, Search = "KPI-1", SortBy = "indicatorNumber", SortDirection = "asc"
        }));
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.CurrentVersion!.IndicatorDefinition.Should().Be("Revised definition");
        controller.GetRegister().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var filteredHistory = Payload(await controller.GetHistoryPage(setup.Target.PublicId, new PagedQueryRequest
        {
            Page = 1, PageSize = 1, Search = "Revised", SortBy = "versionNumber", SortDirection = "desc"
        }));
        filteredHistory.TotalCount.Should().Be(1);
        filteredHistory.Items.Should().ContainSingle().Which.PublicId.Should().Be(second.PublicId);
        var firstHistoryPage = Payload(await controller.GetHistoryPage(setup.Target.PublicId, new PagedQueryRequest
        {
            Page = 1, PageSize = 1, SortBy = "versionNumber", SortDirection = "desc"
        }));
        firstHistoryPage.TotalCount.Should().Be(2);
        firstHistoryPage.TotalPages.Should().Be(2);
        firstHistoryPage.Items.Should().ContainSingle().Which.PublicId.Should().Be(second.PublicId);
        var secondHistoryPage = Payload(await controller.GetHistoryPage(setup.Target.PublicId, new PagedQueryRequest
        {
            Page = 2, PageSize = 1, SortBy = "versionNumber", SortDirection = "desc"
        }));
        secondHistoryPage.Items.Should().ContainSingle().Which.PublicId.Should().Be(first.PublicId);
        (await controller.GetHistoryPage(setup.Target.PublicId, new PagedQueryRequest { SortBy = "raw-sql" })).Result
            .Should().BeOfType<BadRequestObjectResult>();
        controller.GetHistory(setup.Target.PublicId).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var configuration = Payload(await controller.GetConfiguration());
        configuration.MissingTidCount.Should().Be(0);

        context.ChangeTracker.Clear();
        context.TechnicalIndicatorDescriptions.Add(Tid(setup.Municipality.Id, setup.Target.Id, setup.User.Id, 3));
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        var historical = await context.TechnicalIndicatorDescriptions.SingleAsync(item => item.VersionNumber == 1);
        historical.Purpose = "Tampered";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
        workflow.Verify(service => service.QueueAuditTrail("TechnicalIndicatorDescription", It.IsAny<string>(), "CreateVersion",
            null, It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SourceDocument_IsPrivateScannedAppendOnlyAndScoped()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, enabled: true, allRequired: false);
        var storage = new Mock<IEvidenceBlobStorage>();
        storage.Setup(service => service.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceStorageOperationResult(true, "stored"));
        storage.Setup(service => service.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceStorageReadResult(true, "%PDF-source"u8.ToArray(), "read"));
        var inspection = new Mock<IEvidenceInspectionService>();
        inspection.Setup(service => service.Inspect(It.IsAny<byte[]>(), ".pdf"))
            .Returns(new EvidenceInspectionResult(true, new string('a', 64), "SignatureValidated", null));
        var scanner = new Mock<IEvidenceMalwareScanner>();
        scanner.SetupSequence(service => service.ScanAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "application/pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceMalwareScanResult("ThreatDetected", false, "test-scanner", "scan-1", "Threat"))
            .ReturnsAsync(new EvidenceMalwareScanResult("Clean", true, "test-scanner", "scan-2", null));
        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "TID.CREATE", "TID.READ", "TID.UPLOAD_SOURCE", "TID.RESCAN_SOURCE" };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "Allowed" : "Denied", [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(setup.User, It.IsAny<string>()))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], [setup.Municipality.Id]));
        var controller = Controller(context, setup.User, setup.Municipality.Id, storage: storage, inspection: inspection, scanner: scanner, access: access);
        var tid = Payload(await controller.CreateVersion(setup.Target.PublicId, Request("Definition", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc))));
        tid.CreatedByUserId.Should().BeNull();
        var hiddenCreatorSearch = Payload(await controller.GetHistoryPage(setup.Target.PublicId,
            new PagedQueryRequest { Search = setup.User.Id, PageSize = 10 }));
        hiddenCreatorSearch.TotalCount.Should().Be(0);
        allowedCodes.Add("TID.CreatedByUserId.READ");
        var visibleCreatorSearch = Payload(await controller.GetHistoryPage(setup.Target.PublicId,
            new PagedQueryRequest { Search = setup.User.Id, PageSize = 10 }));
        visibleCreatorSearch.Items.Should().ContainSingle().Which.CreatedByUserId.Should().Be(setup.User.Id);
        var bytes = "%PDF-source"u8.ToArray();
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "source.pdf") { Headers = new HeaderDictionary(), ContentType = "application/pdf" };

        var document = Payload(await controller.UploadSourceDocument(tid.PublicId, file, "Approved source methodology"));
        document.ScanStatus.Should().Be("ThreatDetected");
        document.IsQuarantined.Should().BeTrue();
        document.ContentUrl.Should().Contain(tid.PublicId.ToString());
        document.UploadedByUserId.Should().BeNull();
        document.UploadedByName.Should().BeNull();
        document.ScannerProvider.Should().BeNull();
        document.ScannerReference.Should().BeNull();
        document.ScanDetail.Should().BeNull();
        var blob = await context.EvidenceBlobs.SingleAsync();
        blob.StorageKey.Should().StartWith($"tid{Path.DirectorySeparatorChar}");
        blob.Sha256.Should().Be(new string('a', 64));

        foreach (var member in new[] { "UploadedByUserId", "UploadedByName", "ScannerProvider", "ScannerReference", "ScanDetail" })
            allowedCodes.Add($"TID.Source{member}.READ");
        var history = Payload(await controller.GetHistoryPage(setup.Target.PublicId,
            new PagedQueryRequest { Page = 1, PageSize = 10, SortBy = "versionNumber" }));
        var visibleDocument = history.Items.Single().SourceDocuments.Single();
        visibleDocument.UploadedByUserId.Should().Be(setup.User.Id);
        visibleDocument.UploadedByName.Should().Be(setup.User.FullName);
        visibleDocument.ScannerProvider.Should().Be("test-scanner");
        visibleDocument.ScannerReference.Should().Be("scan-1");
        visibleDocument.ScanDetail.Should().Be("Threat");

        (await controller.DownloadSourceDocument(tid.PublicId, document.PublicId)).Should().BeOfType<NotFoundResult>();
        var rescanned = Payload(await controller.RescanSourceDocument(tid.PublicId, document.PublicId));
        rescanned.ScanStatus.Should().Be("Clean");
        rescanned.IsQuarantined.Should().BeFalse();
        access.Verify(service => service.CheckPermissionAsync(setup.User, "TID.RESCAN_SOURCE", It.IsAny<AccessScopeContext?>()), Times.Once);
        var downloaded = await controller.DownloadSourceDocument(tid.PublicId, document.PublicId);
        downloaded.Should().BeOfType<FileContentResult>().Which.FileContents.Should().Equal(bytes);

        context.ChangeTracker.Clear();
        var association = await context.TidSourceDocuments.SingleAsync();
        association.Title = "Tampered";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task Register_page_rejects_unknown_sort_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, enabled: true, allRequired: false);
        var controller = Controller(context, setup.User, setup.Municipality.Id);

        var response = await controller.GetRegisterPage(new PagedQueryRequest { SortBy = "raw-sql" });

        response.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TenantFilter_PreventsCrossMunicipalityTidReads()
    {
        var database = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options;
        await using (var system = new ApplicationDbContext(options, new FixedTenantContext(null, true)))
        {
            var user = IdpTestFixture.CreateUser("tid-system");
            var a = new Municipality { Id = 9101, Code = "A", Name = "A", IsActive = true, TidEnabled = true };
            var b = new Municipality { Id = 9102, Code = "B", Name = "B", IsActive = true, TidEnabled = true };
            system.AddRange(user, a, b);
            var targetA = Target(a.Id, "A");
            var targetB = Target(b.Id, "B");
            system.OpmsTargets.AddRange(targetA, targetB);
            await system.SaveChangesAsync();
            system.TechnicalIndicatorDescriptions.AddRange(Tid(a.Id, targetA.Id, user.Id), Tid(b.Id, targetB.Id, user.Id));
            await system.SaveChangesAsync();
        }

        await using var tenantA = new ApplicationDbContext(options, new FixedTenantContext(9101, false));
        (await tenantA.TechnicalIndicatorDescriptions.Select(item => item.OpmsTarget.IndicatorNumber).ToArrayAsync()).Should().Equal("A");
    }

    [Fact]
    public async Task DirectWrite_IsDeniedWhenDynamicPermissionIsMissing()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedAsync(context, enabled: true, allRequired: false);
        var controller = Controller(context, setup.User, setup.Municipality.Id, allow: false);

        var result = await controller.CreateVersion(setup.Target.PublicId, Request("Denied", DateTime.UtcNow.Date));

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await controller.GetHistoryPage(setup.Target.PublicId, new PagedQueryRequest())).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await context.TechnicalIndicatorDescriptions.CountAsync()).Should().Be(0);
    }

    private static TidsController Controller(
        ApplicationDbContext context,
        ApplicationUser user,
        long municipalityId,
        Mock<IWorkflowGovernanceService>? workflow = null,
        Mock<IEvidenceBlobStorage>? storage = null,
        Mock<IEvidenceInspectionService>? inspection = null,
        Mock<IEvidenceMalwareScanner>? scanner = null,
        bool allow = true,
        Mock<IAccessControlService>? access = null)
    {
        if (access == null)
        {
            access = new Mock<IAccessControlService>();
            access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
                .ReturnsAsync(new AccessDecisionResult(allow, allow ? "Allowed" : "Denied", [], [], []));
            access.Setup(service => service.GetQueryScopeAsync(user, It.IsAny<string>()))
                .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], [municipalityId]));
        }
        storage ??= new Mock<IEvidenceBlobStorage>();
        inspection ??= new Mock<IEvidenceInspectionService>();
        scanner ??= new Mock<IEvidenceMalwareScanner>();
        return new TidsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object,
            (workflow ?? new Mock<IWorkflowGovernanceService>()).Object, new FixedTenantContext(municipalityId, false),
            storage.Object, inspection.Object, scanner.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static CreateTidVersionRequest Request(string definition, DateTime effectiveFrom, string? priorVersion = null) => new(
        definition, "Purpose", "System records", "Monthly extraction", "Actual divided by target", "Actual", "Target",
        "Data latency", "Source systems remain available", "Reconcile to signed source", null, "Governed notes",
        effectiveFrom, priorVersion, "Approved methodology version");

    private static async Task<(ApplicationUser User, Municipality Municipality, OpmsTarget Target)> SeedAsync(ApplicationDbContext context, bool enabled, bool allRequired)
    {
        var user = IdpTestFixture.CreateUser("tid-owner");
        var municipality = new Municipality { Id = 9001, PublicId = Guid.NewGuid(), Code = "TID", Name = "TID Municipality", IsActive = true, TidEnabled = enabled, TidAllKpisRequired = allRequired };
        var target = Target(municipality.Id, "KPI-1");
        context.AddRange(user, municipality, target);
        await context.SaveChangesAsync();
        return (user, municipality, target);
    }

    private static OpmsTarget Target(long municipalityId, string code) => new()
    {
        MunicipalityId = municipalityId,
        IndicatorNumber = code,
        TargetName = $"Target {code}",
        KpiDescription = "KPI",
        AnnualTarget = 100,
        AnnualTargetDescription = "100 percent",
        TargetUnitType = "percentage"
    };

    private static TechnicalIndicatorDescription Tid(long municipalityId, string targetId, string userId, int version = 1) => new()
    {
        MunicipalityId = municipalityId, OpmsTargetId = targetId, VersionNumber = version, IndicatorDefinition = "Definition",
        Purpose = "Purpose", DataSource = "Source", CollectionMethod = "Collect", CalculationMethod = "Calculate",
        VerificationMethod = "Verify", EffectiveFrom = DateTime.UtcNow.Date, CreatedByUserId = userId
    };

    private static T Payload<T>(ActionResult<ApiResponse<T>> action) =>
        ((action.Result as OkObjectResult)!.Value as ApiResponse<T>)!.Data!;

    private sealed class FixedTenantContext(long? municipalityId, bool isSystem) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => "tid-test";
    }
}

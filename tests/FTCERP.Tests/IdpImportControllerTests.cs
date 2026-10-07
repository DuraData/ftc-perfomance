namespace FTCERP.Tests;

public class IdpImportControllerTests
{
    [Fact]
    public async Task StageKpis_ClassifiesEveryRow_AndBlocksInvalidBatchCommit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = CreateController(context, setup.User, workflow.Object);
        var requestId = Guid.NewGuid();
        var rows = new[]
        {
            Row(2, setup.Project.ProjectCode, "NEW-1", "New KPI"),
            Row(3, setup.Project.ProjectCode, "SAME-1", "Unchanged KPI"),
            Row(4, setup.Project.ProjectCode, "CHANGE-1", "Changed KPI", annualTarget: 99),
            Row(5, "MISSING", "BAD-1", "Invalid KPI")
        };

        var stagedAction = await controller.StageKpis(setup.Plan.PublicId, new StageIdpKpiImportRequest(requestId, "idp-kpis.csv", rows));
        var staged = Payload(stagedAction);

        staged.NewRows.Should().Be(1);
        staged.UnchangedRows.Should().Be(1);
        staged.ChangedRows.Should().Be(1);
        staged.InvalidRows.Should().Be(1);
        staged.Rows.Select(item => item.Status).Should().BeEquivalentTo("New", "Unchanged", "Changed", "Invalid");
        staged.Rows.Single(item => item.Status == "Invalid").ErrorCode.Should().Be("PROJECT_NOT_FOUND");

        var commit = await controller.Commit(staged.PublicId, new CommitIdpImportRequest(staged.RowVersion, "Approved reconciliation"));
        commit.Result.Should().BeOfType<BadRequestObjectResult>();
        (await context.IdpImportBatches.SingleAsync()).Status.Should().Be(IdpImportBatchStatus.Staged);
        (await context.IdpKpis.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task StageAndCommit_IsIdempotentAtomicAndAudited()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = CreateController(context, setup.User, workflow.Object);
        var requestId = Guid.NewGuid();
        var request = new StageIdpKpiImportRequest(requestId, "approved-kpis.csv",
        [
            Row(2, setup.Project.ProjectCode, "NEW-2", "Imported KPI"),
            Row(3, setup.Project.ProjectCode, "CHANGE-1", "Changed through import", annualTarget: 77)
        ]);

        var first = Payload(await controller.StageKpis(setup.Plan.PublicId, request));
        var repeated = Payload(await controller.StageKpis(setup.Plan.PublicId, request));
        repeated.PublicId.Should().Be(first.PublicId);
        (await context.IdpImportBatches.CountAsync()).Should().Be(1);

        var committedAction = await controller.Commit(first.PublicId, new CommitIdpImportRequest(first.RowVersion, "Council-approved KPI reconciliation"));
        var committed = Payload(committedAction);
        committed.Status.Should().Be("Committed");
        committed.CommittedAt.Should().NotBeNull();

        var kpis = await context.IdpKpis.OrderBy(item => item.KpiCode).ToListAsync();
        kpis.Should().Contain(item => item.KpiCode == "NEW-2" && item.KpiName == "Imported KPI");
        kpis.Single(item => item.KpiCode == "CHANGE-1").AnnualTarget.Should().Be(77);
        (await context.IdpImportRows.CountAsync()).Should().Be(2);
        workflow.Verify(service => service.WriteAuditTrailAsync(
            "IdpImportBatch", It.IsAny<string>(), "Stage", null, It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Once);
        workflow.Verify(service => service.WriteAuditTrailAsync(
            "IdpImportBatch", It.IsAny<string>(), "Commit", It.IsAny<object>(), It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Commit_RejectsStaleKpiPreview_WithoutPartialWrites()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object);
        var staged = Payload(await controller.StageKpis(setup.Plan.PublicId, new StageIdpKpiImportRequest(
            Guid.NewGuid(), "stale.csv",
            [Row(2, setup.Project.ProjectCode, "NEW-3", "Should not be inserted"), Row(3, setup.Project.ProjectCode, "CHANGE-1", "Preview change", annualTarget: 88)])));

        var existing = await context.IdpKpis.SingleAsync(item => item.KpiCode == "CHANGE-1");
        existing.AnnualTarget = 66;
        await context.SaveChangesAsync();

        var commit = await controller.Commit(staged.PublicId, new CommitIdpImportRequest(staged.RowVersion, "Stale preview"));
        commit.Result.Should().BeOfType<ConflictObjectResult>();
        (await context.IdpKpis.AnyAsync(item => item.KpiCode == "NEW-3")).Should().BeFalse();
        (await context.IdpKpis.SingleAsync(item => item.KpiCode == "CHANGE-1")).AnnualTarget.Should().Be(66);
        (await context.IdpImportBatches.SingleAsync()).Status.Should().Be(IdpImportBatchStatus.Staged);
    }

    [Fact]
    public async Task ImportRows_AreAppendOnly_AndDatabaseCountsRemainConsistent()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object);
        _ = Payload(await controller.StageKpis(setup.Plan.PublicId, new StageIdpKpiImportRequest(
            Guid.NewGuid(), "one.csv", [Row(2, setup.Project.ProjectCode, "NEW-4", "New KPI")])));

        var row = await context.IdpImportRows.SingleAsync();
        row.Reference = "tampered";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");

        context.ChangeTracker.Clear();
        context.IdpImportBatches.Add(new IdpImportBatch
        {
            MunicipalityId = setup.Plan.MunicipalityId!.Value,
            IdpPlanId = setup.Plan.Id,
            ClientRequestId = Guid.NewGuid(),
            SourceFileName = "invalid-counts.csv",
            SourceSha256 = new string('a', 64),
            TotalRows = 2,
            NewRows = 1,
            CreatedByUserId = setup.User.Id
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ImportCommit_RequiresAValidConcurrencyToken_AndSourceRowsAreUniquePerBatch()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object);
        var staged = Payload(await controller.StageKpis(setup.Plan.PublicId, new StageIdpKpiImportRequest(
            Guid.NewGuid(), "validation.csv", [Row(2, setup.Project.ProjectCode, "NEW-5", "New KPI")])));

        var missingVersion = await controller.Commit(staged.PublicId, new CommitIdpImportRequest(string.Empty, "Approved"));
        missingVersion.Result.Should().BeOfType<BadRequestObjectResult>();

        context.ChangeTracker.Clear();
        var duplicateRows = Batch(setup.Plan, setup.User, "duplicates.csv");
        duplicateRows.Rows.Add(new IdpImportRow
        {
            SourceRowNumber = 2,
            Reference = "duplicates.csv/SECOND",
            Status = IdpImportRowStatus.Unchanged,
            PayloadJson = "{}"
        });
        duplicateRows.TotalRows = 2;
        duplicateRows.UnchangedRows = 2;
        context.IdpImportBatches.Add(duplicateRows);

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ImportBatches_AreFilteredByMunicipality()
    {
        var database = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options;
        await using (var system = new ApplicationDbContext(options, new FixedTenantContext(null, true)))
        {
            var user = IdpTestFixture.CreateUser("system-importer");
            var municipalityA = new Municipality { Id = 801, Code = "A", Name = "A", IsActive = true };
            var municipalityB = new Municipality { Id = 802, Code = "B", Name = "B", IsActive = true };
            system.AddRange(user, municipalityA, municipalityB);
            var planA = Plan(municipalityA, user, "A");
            var planB = Plan(municipalityB, user, "B");
            system.IdpPlans.AddRange(planA, planB);
            await system.SaveChangesAsync();
            system.IdpImportBatches.AddRange(Batch(planA, user, "a.csv"), Batch(planB, user, "b.csv"));
            await system.SaveChangesAsync();
        }

        await using var tenantA = new ApplicationDbContext(options, new FixedTenantContext(801, false));
        (await tenantA.IdpImportBatches.Select(item => item.SourceFileName).ToListAsync()).Should().Equal("a.csv");
        (await tenantA.IdpImportRows.Select(item => item.Reference).ToListAsync()).Should().Equal("a.csv/ROW");
    }

    [Fact]
    public async Task ImportBatchPage_FiltersBeforeCount_ExcludesRows_AndRetiresLegacyArray()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        for (var index = 0; index < 31; index++)
        {
            var batch = Batch(setup.Plan, setup.User, $"match-{index:00}.csv");
            batch.ImportType = index % 2 == 0 ? "KPI" : "HIERARCHY";
            context.IdpImportBatches.Add(batch);
        }

        var otherPlan = Plan(await context.Municipalities.SingleAsync(), setup.User, "OTHER");
        context.IdpPlans.Add(otherPlan);
        await context.SaveChangesAsync();
        var otherBatch = Batch(otherPlan, setup.User, "match-outside.csv");
        otherBatch.ImportType = "KPI";
        context.IdpImportBatches.Add(otherBatch);
        await context.SaveChangesAsync();

        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object);
        var action = await controller.GetBatchesPage(setup.Plan.PublicId,
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "fileName", SortDirection = "asc" },
            "Staged", "KPI");
        var page = ((action.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>)!.Data!;

        page.TotalCount.Should().Be(16);
        page.Items.Should().HaveCount(6);
        page.Items.Should().OnlyContain(item => item.ImportType == "KPI" && item.SourceFileName != null && item.SourceFileName.StartsWith("match-"));
        page.Items.Should().NotContain(item => item.SourceFileName == "match-outside.csv");
        page.Items.First().SourceFileName.Should().Be("match-20.csv");

        (await controller.GetBatchesPage(setup.Plan.PublicId,
            new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetBatches(setup.Plan.PublicId)).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task ImportBatchReads_AllowEitherDynamicImportCapability_AndDenyWithoutBoth()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(code == "IDP_PLAN.IMPORT", code == "IDP_PLAN.IMPORT" ? "Allowed" : "Denied", [], [], []));
        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object, access.Object);

        (await controller.GetBatchesPage(setup.Plan.PublicId, new PagedQueryRequest())).Result.Should().BeOfType<OkObjectResult>();

        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        (await controller.GetBatchesPage(setup.Plan.PublicId, new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task ImportHistory_MasksSensitiveMembers_PreventsQueryInference_AndUsesPublicActorIdentity()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IDP_INDICATOR.IMPORT", "IDP_PLAN.IMPORT" };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(setup.User, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowed.Contains(code) && scope?.MunicipalityId == setup.Plan.MunicipalityId,
                    allowed.Contains(code) ? "Allowed" : "Denied", [], [], []));
        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object, access.Object);

        var staged = Payload(await controller.StageKpis(setup.Plan.PublicId, new StageIdpKpiImportRequest(
            Guid.NewGuid(), "SECRET-IDP-IMPORT.csv", [Row(2, "MISSING-PROJECT", "SECRET-KPI", "Protected import row")])));

        staged.ClientRequestId.Should().BeNull();
        staged.SourceFileName.Should().BeNull();
        staged.SourceSha256.Should().BeNull();
        staged.CreatedByUserPublicId.Should().BeNull();
        staged.CreatedByName.Should().BeNull();
        staged.Rows.Should().ContainSingle();
        staged.Rows[0].ExistingValueJson.Should().BeNull();
        staged.Rows[0].NormalizedJson.Should().BeNull();
        staged.Rows[0].SuppliedValue.Should().BeNull();
        staged.Rows[0].ErrorCode.Should().BeNull();
        staged.Rows[0].ErrorField.Should().BeNull();
        staged.Rows[0].ErrorMessage.Should().BeNull();

        var hiddenSearchAction = await controller.GetBatchesPage(setup.Plan.PublicId,
            new PagedQueryRequest { Search = "SECRET-IDP-IMPORT" });
        var hiddenSearch = ((hiddenSearchAction.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>)!.Data!;
        hiddenSearch.TotalCount.Should().Be(0);
        (await controller.GetBatchesPage(setup.Plan.PublicId, new PagedQueryRequest { SortBy = "fileName" })).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        foreach (var member in new[] { "ImportClientRequestId", "ImportSourceFileName", "ImportSourceHash", "ImportActor", "ImportRowPayload", "ImportErrorDetail" })
            allowed.Add($"IDP_PLAN.{member}.READ");

        var visibleAction = await controller.GetBatch(staged.PublicId);
        var visible = Payload(visibleAction);
        visible.ClientRequestId.Should().NotBeNull();
        visible.SourceFileName.Should().Be("SECRET-IDP-IMPORT.csv");
        visible.SourceSha256.Should().HaveLength(64);
        visible.CreatedByUserPublicId.Should().Be(setup.User.PublicId);
        visible.CreatedByName.Should().Be(setup.User.FullName);
        visible.Rows[0].ErrorCode.Should().Be("PROJECT_NOT_FOUND");
        visible.Rows[0].ErrorMessage.Should().NotBeNullOrWhiteSpace();

        var visibleSearchAction = await controller.GetBatchesPage(setup.Plan.PublicId,
            new PagedQueryRequest { Search = "SECRET-IDP-IMPORT", SortBy = "fileName" });
        var visibleSearch = ((visibleSearchAction.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<IdpImportBatchSummaryResponse>>)!.Data!;
        visibleSearch.Items.Should().ContainSingle().Which.CreatedByUserPublicId.Should().Be(setup.User.PublicId);
    }

    [Fact]
    public async Task ManualAndImportedKpis_UseTheSameIndicatorValidationPolicy()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var manual = IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(setup.User).Object, workflow.Object, setup.User.Id);
        var manualResult = await manual.CreateKpi(new CreateIdpKpiRequest(
            setup.Project.Id, "BAD-MANUAL", "Bad manual KPI", "Description", "x", 0, 1, 5,
            null, "System", "Quarterly", "NotAType", false, false));
        manualResult.Result.Should().BeOfType<BadRequestObjectResult>();

        var imports = CreateController(context, setup.User, workflow.Object);
        var invalidImport = Payload(await imports.StageKpis(setup.Plan.PublicId, new StageIdpKpiImportRequest(
            Guid.NewGuid(), "invalid-type.csv",
            [Row(2, setup.Project.ProjectCode, "BAD-IMPORT", "Bad imported KPI") with { IndicatorType = "NotAType" }])));
        invalidImport.InvalidRows.Should().Be(1);
        invalidImport.Rows.Single().ErrorCode.Should().Be("INVALID_INDICATOR_TYPE");
        (await context.IdpKpis.AnyAsync(item => item.KpiCode.StartsWith("BAD"))).Should().BeFalse();
    }

    [Fact]
    public async Task HierarchyImport_CreatesACompleteSharedParentTree_AndUsesItsOwnCommitPermission()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = CreateController(context, setup.User, workflow.Object);
        var rows = new[] { HierarchyRow(2, "PROJECT-A", "Project A"), HierarchyRow(3, "PROJECT-B", "Project B") };

        var staged = Payload(await controller.StageHierarchy(setup.Plan.PublicId, new StageIdpHierarchyImportRequest(
            Guid.NewGuid(), "hierarchy.csv", rows)));

        staged.ImportType.Should().Be("HIERARCHY");
        staged.NewRows.Should().Be(2);
        staged.InvalidRows.Should().Be(0);
        typeof(IdpImportsController).GetMethod(nameof(IdpImportsController.StageHierarchy))!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Policy.Should().Be("Permission:IDP_PLAN.IMPORT");
        typeof(IdpImportsController).GetMethod(nameof(IdpImportsController.CommitHierarchy))!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Policy.Should().Be("Permission:IDP_PLAN.IMPORT");
        (await controller.Commit(staged.PublicId, new CommitIdpImportRequest(staged.RowVersion, "Wrong permission route"))).Result
            .Should().BeOfType<ForbidResult>();

        var committed = Payload(await controller.CommitHierarchy(staged.PublicId, new CommitIdpImportRequest(
            staged.RowVersion, "Council-approved hierarchy reconciliation")));
        committed.Status.Should().Be("Committed");

        var outcome = await context.IdpStrategicOutcomes
            .Include(item => item.StrategicObjectives)
                .ThenInclude(item => item.DevelopmentPriorities)
                    .ThenInclude(item => item.Programmes)
                        .ThenInclude(item => item.Projects)
            .SingleAsync(item => item.Code == "OUT-NEW");
        var objective = outcome.StrategicObjectives.Single();
        var priority = objective.DevelopmentPriorities.Single();
        var programme = priority.Programmes.Single();
        outcome.PublicId.Should().NotBeEmpty();
        objective.PublicId.Should().NotBeEmpty();
        priority.PriorityCode.Should().Be("PRI-NEW");
        programme.Projects.Select(item => item.ProjectCode).Should().BeEquivalentTo("PROJECT-A", "PROJECT-B");
        programme.Projects.Should().OnlyContain(item => item.PublicId != Guid.Empty && item.RowVersion.Length > 0);
        workflow.Verify(service => service.WriteAuditTrailAsync(
            "IdpImportBatch", It.IsAny<string>(), "StageHierarchy", null, It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Once);
        workflow.Verify(service => service.WriteAuditTrailAsync(
            "IdpImportBatch", It.IsAny<string>(), "CommitHierarchy", It.IsAny<object>(), It.IsAny<object>(), setup.User.Id, It.IsAny<string?>()), Times.Once);

        context.IdpDevelopmentPriorities.Add(new IdpDevelopmentPriority
        {
            IdpStrategicObjectiveId = objective.Id,
            PriorityCode = priority.PriorityCode,
            Name = "Duplicate",
            Description = "Must be rejected by the relational business key",
            SortOrder = 2
        });
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task HierarchyImport_RejectsConflictingParents_AndStalePreviews()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var setup = await SeedPlanAsync(context);
        var controller = CreateController(context, setup.User, new Mock<IWorkflowGovernanceService>().Object);
        var conflicting = HierarchyRow(3, "PROJECT-B", "Project B") with { OutcomeName = "A conflicting shared outcome" };

        var invalid = Payload(await controller.StageHierarchy(setup.Plan.PublicId, new StageIdpHierarchyImportRequest(
            Guid.NewGuid(), "conflicting.csv", [HierarchyRow(2, "PROJECT-A", "Project A"), conflicting])));
        invalid.InvalidRows.Should().Be(2);
        invalid.Rows.Should().OnlyContain(item => item.ErrorCode == "CONFLICTING_PARENT_DEFINITION");
        (await controller.CommitHierarchy(invalid.PublicId, new CommitIdpImportRequest(invalid.RowVersion, "Invalid batch"))).Result
            .Should().BeOfType<BadRequestObjectResult>();

        var initial = Payload(await controller.StageHierarchy(setup.Plan.PublicId, new StageIdpHierarchyImportRequest(
            Guid.NewGuid(), "valid.csv", [HierarchyRow(2, "PROJECT-C", "Project C")])));
        _ = Payload(await controller.CommitHierarchy(initial.PublicId, new CommitIdpImportRequest(initial.RowVersion, "Initial import")));
        var unchanged = Payload(await controller.StageHierarchy(setup.Plan.PublicId, new StageIdpHierarchyImportRequest(
            Guid.NewGuid(), "unchanged.csv", [HierarchyRow(2, "PROJECT-C", "Project C")])));
        unchanged.UnchangedRows.Should().Be(1);

        var project = await context.IdpProjects.SingleAsync(item => item.ProjectCode == "PROJECT-C");
        project.Budget = 999999;
        await context.SaveChangesAsync();

        var staleCommit = await controller.CommitHierarchy(unchanged.PublicId, new CommitIdpImportRequest(unchanged.RowVersion, "Stale preview"));
        staleCommit.Result.Should().BeOfType<ConflictObjectResult>();
        (await context.IdpImportBatches.SingleAsync(item => item.PublicId == unchanged.PublicId)).Status.Should().Be(IdpImportBatchStatus.Staged);
    }

    private static IdpImportsController CreateController(
        ApplicationDbContext context,
        ApplicationUser user,
        IWorkflowGovernanceService workflow,
        IAccessControlService? accessControl = null)
    {
        var access = accessControl;
        if (access == null)
        {
            var accessMock = new Mock<IAccessControlService>();
            accessMock.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
                .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => new AccessDecisionResult(true, "Allowed", [code], [], []));
            access = accessMock.Object;
        }
        return new IdpImportsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access,
            workflow)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };
    }

    private static IdpImportBatchResponse Payload(ActionResult<ApiResponse<IdpImportBatchResponse>> action) =>
        ((action.Result as OkObjectResult)!.Value as ApiResponse<IdpImportBatchResponse>)!.Data!;

    private static IdpKpiImportRowRequest Row(
        int sourceRow,
        string projectCode,
        string kpiCode,
        string name,
        decimal annualTarget = 10) => new(
            sourceRow,
            projectCode,
            kpiCode,
            name,
            "Description",
            "Actual / Target",
            1,
            annualTarget,
            50,
            null,
            "System",
            "Quarterly",
            "Output",
            false,
            false);

    private static IdpHierarchyImportRowRequest HierarchyRow(int sourceRow, string projectCode, string projectName) => new(
        sourceRow,
        "out-new", "Outcome", "Outcome description", 1,
        "obj-new", "Objective", "Objective description", 10, 20, null,
        new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc), 1000000, 1,
        "pri-new", "Priority", "Priority description", 1,
        "prog-new", "Programme", "Programme description", null, 900000, 800000, 100000,
        projectCode, projectName, $"{projectName} description", "Capital", null, 500000, "Municipal grant",
        new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2029, 12, 31, 0, 0, 0, DateTimeKind.Utc), "Planned", "NEED-1");

    private static async Task<(ApplicationUser User, IdpPlan Plan, IdpProject Project)> SeedPlanAsync(ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser("importer");
        var municipality = new Municipality { Id = 501, PublicId = Guid.NewGuid(), Code = "IMPORT", Name = "Import Municipality", IsActive = true };
        context.AddRange(user, municipality);
        var plan = new IdpPlan
        {
            MunicipalityId = municipality.Id,
            MunicipalityName = municipality.Name,
            PlanTitle = "Import Plan",
            PlanCode = "IDP-IMPORT",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();
        var outcome = new IdpStrategicOutcome { IdpPlanId = plan.Id, Code = "SO", Name = "Outcome", Description = "Outcome", SortOrder = 1 };
        context.IdpStrategicOutcomes.Add(outcome);
        await context.SaveChangesAsync();
        var objective = new IdpStrategicObjective { IdpStrategicOutcomeId = outcome.Id, Code = "OBJ", Name = "Objective", Description = "Objective", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(5), SortOrder = 1 };
        context.IdpStrategicObjectives.Add(objective);
        await context.SaveChangesAsync();
        var priority = new IdpDevelopmentPriority { IdpStrategicObjectiveId = objective.Id, PriorityCode = "PRI", Name = "Priority", Description = "Priority", SortOrder = 1 };
        context.IdpDevelopmentPriorities.Add(priority);
        await context.SaveChangesAsync();
        var programme = new IdpProgramme { IdpDevelopmentPriorityId = priority.Id, ProgrammeCode = "PRG", Name = "Programme", Description = "Programme" };
        context.IdpProgrammes.Add(programme);
        await context.SaveChangesAsync();
        var project = new IdpProject { IdpProgrammeId = programme.Id, ProjectCode = "PROJ", ProjectName = "Project", Description = "Project", Category = "Service", FundingSource = "Own", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1) };
        context.IdpProjects.Add(project);
        await context.SaveChangesAsync();
        context.IdpKpis.AddRange(
            Kpi(project.Id, "SAME-1", "Unchanged KPI", 10),
            Kpi(project.Id, "CHANGE-1", "Existing KPI", 10));
        await context.SaveChangesAsync();
        return (user, plan, project);
    }

    private static IdpKpi Kpi(int projectId, string code, string name, decimal annualTarget) => new()
    {
        IdpProjectId = projectId,
        KpiCode = code,
        KpiName = name,
        Description = "Description",
        Formula = "Actual / Target",
        Baseline = 1,
        AnnualTarget = annualTarget,
        FiveYearTarget = 50,
        DataSource = "System",
        ReportingFrequency = "Quarterly",
        IndicatorType = IdpKpiIndicatorType.Output
    };

    private static IdpPlan Plan(Municipality municipality, ApplicationUser user, string suffix) => new()
    {
        MunicipalityId = municipality.Id,
        MunicipalityName = municipality.Name,
        PlanTitle = $"Plan {suffix}",
        PlanCode = $"PLAN-{suffix}",
        StartFinancialYear = 2026,
        EndFinancialYear = 2031,
        CreatedByUserId = user.Id
    };

    private static IdpImportBatch Batch(IdpPlan plan, ApplicationUser user, string fileName) => new()
    {
        MunicipalityId = plan.MunicipalityId!.Value,
        IdpPlanId = plan.Id,
        ClientRequestId = Guid.NewGuid(),
        SourceFileName = fileName,
        SourceSha256 = new string('b', 64),
        TotalRows = 1,
        UnchangedRows = 1,
        CreatedByUserId = user.Id,
        Rows = [new IdpImportRow { SourceRowNumber = 2, Reference = $"{fileName}/ROW", Status = IdpImportRowStatus.Unchanged, PayloadJson = "{}" }]
    };

    private sealed class FixedTenantContext(long? municipalityId, bool isSystem) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => "tenant-test";
    }
}

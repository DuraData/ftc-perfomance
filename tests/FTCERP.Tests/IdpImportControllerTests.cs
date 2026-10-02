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

    private static IdpImportsController CreateController(
        ApplicationDbContext context,
        ApplicationUser user,
        IWorkflowGovernanceService workflow) => new(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            workflow)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };

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
        var priority = new IdpDevelopmentPriority { IdpStrategicObjectiveId = objective.Id, Name = "Priority", Description = "Priority", SortOrder = 1 };
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

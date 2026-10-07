namespace FTCERP.Tests;

public class C88ControllerTests
{
    [Fact]
    public async Task VersionedCatalogueAndMunicipalityConfiguration_AreGovernedAndControlled()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);

        var module = await CreateCatalogueAsync(context, controller, seed);
        module.Version.IsPublished.Should().BeTrue();
        module.Indicator.CalculationOperator.Should().Be(C88ControlledCalculationOperator.Percentage);
        module.Indicator.OfficialFormulaText.Should().Be("Numerator divided by denominator times 100");
        module.Indicator.DataElements.Should().HaveCount(2);
        module.Indicator.Applicability.Should().ContainSingle();
        module.Configuration.MunicipalityFinancialYearId.Should().Be(seed.Year.Id);

        var immutable = await controller.CreateCatalogueItem(new SaveC88CatalogueItemRequest(module.Version.PublicId,
            C88CatalogueItemKind.Sector, "LATE", "Late item", null, null, 99, true, "Attempt late edit", null));
        immutable.Result.Should().BeOfType<ConflictObjectResult>();
        (await context.C88CatalogueItems.CountAsync()).Should().Be(7);
    }

    [Fact]
    public async Task PlanningReportingAssignmentsAndIndependentWorkflow_PreserveHistory()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var workflowAudit = new Mock<IWorkflowGovernanceService>();
        var controller = Controller(context, seed.User, seed.Municipality.Id, workflowAudit);
        var module = await CreateCatalogueAsync(context, controller, seed);

        var planId = Payload(await controller.SavePlan(new SaveC88IndicatorPlanRequest(module.Configuration.PublicId,
            module.Indicator.PublicId, "20", "55", "80", null, null, "Approve C88 plan", null)));
        planId.Should().NotBeEmpty();

        var workflowId = Payload(await controller.CreateWorkflow(new SaveC88WorkflowRequest(module.Configuration.PublicId,
            DateTime.UtcNow.AddDays(-1), null,
            [new(1, C88WorkflowStageKind.Capturer, "Capture", C88AssignmentRole.PrimaryCapturer, true),
             new(2, C88WorkflowStageKind.ReviewerVerifier, "Verify", C88AssignmentRole.ReviewerVerifier, true),
             new(3, C88WorkflowStageKind.FinalSubmission, "Final submit", C88AssignmentRole.FinalSubmitter, true)],
            "Configure independent workflow", null, null)));
        workflowId.Should().NotBeEmpty();

        foreach (var role in new[] { C88AssignmentRole.PrimaryCapturer, C88AssignmentRole.ReviewerVerifier, C88AssignmentRole.FinalSubmitter })
            Payload(await controller.CreateAssignment(new SaveC88AssignmentRequest(module.Configuration.PublicId, module.Indicator.PublicId,
                seed.Employee.PublicId, role, DateTime.UtcNow.AddDays(-2), null, true, $"Assign {role}", null))).Should().NotBeEmpty();

        var calendarId = Payload(await controller.CreateCalendar(new SaveC88ReportingCalendarRequest(module.Configuration.PublicId,
            module.ReportType.PublicId, null, "Q1", "Quarter 1", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), true, "Open calendar", null)));
        var calendar = await context.C88ReportingCalendars.SingleAsync(item => item.PublicId == calendarId);
        var reportId = Payload(await controller.CreateReportVersion(new CreateC88ReportVersionRequest(module.Configuration.PublicId,
            calendar.PublicId, module.Indicator.PublicId, null, null, null, null,
            [new(module.Numerator.PublicId, "45", null, null), new(module.Denominator.PublicId, "60", null, null)],
            [new(module.Question.PublicId, "true", "Evidence checked")], "Capture official C88 values")));
        var report = await context.C88IndicatorReports.SingleAsync(item => item.PublicId == reportId);
        report.CalculatedValue.Should().Be("75");
        report.State.Should().Be(C88ReportState.Draft);

        var submitVersion = Convert.ToBase64String(report.RowVersion);
        Payload(await controller.Submit(report.PublicId, new(submitVersion, "Submit for verification"))).Should().Be(report.PublicId);
        var submitted = await context.C88IndicatorReports.SingleAsync(item => item.PublicId == report.PublicId);
        Payload(await controller.Verify(report.PublicId, new(Convert.ToBase64String(submitted.RowVersion), "Verified against source"))).Should().Be(report.PublicId);
        var verified = await context.C88IndicatorReports.SingleAsync(item => item.PublicId == report.PublicId);
        Payload(await controller.FinalSubmit(report.PublicId, new(Convert.ToBase64String(verified.RowVersion), "Final Treasury submission"))).Should().Be(report.PublicId);

        var completed = await context.C88IndicatorReports.Include(item => item.WorkflowActions).SingleAsync(item => item.PublicId == report.PublicId);
        completed.State.Should().Be(C88ReportState.FinalSubmitted);
        completed.WorkflowActions.Select(item => item.Action).Should().Equal(C88WorkflowActionKind.Created, C88WorkflowActionKind.Submitted, C88WorkflowActionKind.Verified, C88WorkflowActionKind.FinalSubmitted);
        completed.CurrentStageSequence.Should().Be(3);
        controller.GetWorkspace().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var planPage = Payload(await controller.GetPlansPage(new PagedQueryRequest { Page = 1, PageSize = 1, SortBy = "indicatorCode", SortDirection = "asc" }, seed.Year.PublicId));
        planPage.TotalCount.Should().Be(1);
        planPage.Items.Should().ContainSingle().Which.PublicId.Should().Be(planId);
        var page = Payload(await controller.GetReportsPage(new PagedQueryRequest { Page = 1, PageSize = 1, SortBy = "indicatorCode", SortDirection = "asc" }, seed.Year.PublicId));
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.IndicatorCode.Should().Be("C88-001");
        context.ChangeTracker.Clear();
        var value = await context.C88DataElementValues.FirstAsync();
        value.Value = "999";
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
        workflowAudit.Verify(service => service.QueueAuditTrail("C88", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object>(), seed.User.Id, It.IsAny<string?>()), Times.AtLeast(10));
    }

    [Fact]
    public async Task OpmsMappingIsAlignmentOnly_AndDisabledC88NeverBlocksCoreOpms()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        var originalTarget = (seed.Target.AnnualTarget, seed.Target.TargetUnitType, seed.Target.KpiDescription);

        Payload(await controller.CreateMapping(new SaveC88MappingRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Target.PublicId, C88MappingType.Contributing, "Alignment only", true, null))).Should().NotBeEmpty();
        var target = await context.OpmsTargets.SingleAsync(item => item.Id == seed.Target.Id);
        (target.AnnualTarget, target.TargetUnitType, target.KpiDescription).Should().Be(originalTarget);

        Payload(await controller.Configure(new ConfigureC88Request(seed.Year.PublicId, module.Version.PublicId, false,
            seed.Year.EffectiveFrom, null, "Municipality disabled optional C88", Convert.ToBase64String(module.Configuration.RowVersion)))).Should().Be(module.Configuration.PublicId);
        var denied = await controller.SavePlan(new SaveC88IndicatorPlanRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            "1", "2", "3", null, null, "Must remain disabled", null));
        denied.Result.Should().BeOfType<BadRequestObjectResult>();
        (await context.OpmsTargets.SingleAsync(item => item.Id == seed.Target.Id)).IsWithdrawn.Should().BeFalse();
    }

    [Fact]
    public async Task TenantFiltersAndPrimaryCapturerConstraint_AreRelationallyEnforced()
    {
        var database = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options;
        await using (var system = new ApplicationDbContext(options, new FixedTenantContext(null, true)))
        {
            var user = IdpTestFixture.CreateUser("c88-system");
            var a = new Municipality { Id = 9401, Code = "A", Name = "A" };
            var b = new Municipality { Id = 9402, Code = "B", Name = "B" };
            system.AddRange(user, a, b,
                new C88CatalogueVersion { MunicipalityId = a.Id, Code = "A-2026", Name = "A", EditionDate = DateTime.UtcNow, EffectiveFrom = DateTime.UtcNow, CreatedByUserId = user.Id },
                new C88CatalogueVersion { MunicipalityId = b.Id, Code = "B-2026", Name = "B", EditionDate = DateTime.UtcNow, EffectiveFrom = DateTime.UtcNow, CreatedByUserId = user.Id });
            await system.SaveChangesAsync();
        }
        await using var tenantA = new ApplicationDbContext(options, new FixedTenantContext(9401, false));
        (await tenantA.C88CatalogueVersions.Select(item => item.Code).ToArrayAsync()).Should().Equal("A-2026");

        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        Payload(await controller.CreateAssignment(new SaveC88AssignmentRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Employee.PublicId, C88AssignmentRole.PrimaryCapturer, DateTime.UtcNow.AddDays(-1), null, true, "Primary one", null))).Should().NotBeEmpty();
        var duplicate = await controller.CreateAssignment(new SaveC88AssignmentRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Employee.PublicId, C88AssignmentRole.PrimaryCapturer, DateTime.UtcNow, null, true, "Primary two", null));
        duplicate.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DirectApiDenialAndAssignmentScope_AreEnforcedBeyondNavigation()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var manager = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, manager, seed);
        Payload(await manager.SavePlan(new SaveC88IndicatorPlanRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            "10", "20", "30", null, null, "Create scoped plan", null))).Should().NotBeEmpty();
        var reader = Controller(context, seed.User, seed.Municipality.Id,
            permissionRule: permission => permission is "C88_INDICATOR.READ" or "C88_REPORT.READ");

        var beforeAssignment = Payload(await reader.GetIndicatorsPage(new PagedQueryRequest(), module.Version.PublicId));
        beforeAssignment.Items.Should().BeEmpty();
        Payload(await reader.GetAssignmentsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Should().BeEmpty();
        Payload(await reader.GetMappingsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Should().BeEmpty();
        Payload(await reader.GetPlansPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Should().BeEmpty();
        var denied = await reader.CreateMapping(new SaveC88MappingRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Target.PublicId, C88MappingType.Direct, "Must be denied", true, null));
        denied.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        Payload(await manager.CreateMapping(new SaveC88MappingRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Target.PublicId, C88MappingType.Direct, "Authorized mapping", true, null))).Should().NotBeEmpty();
        Payload(await manager.CreateAssignment(new SaveC88AssignmentRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Employee.PublicId, C88AssignmentRole.Contributor, DateTime.UtcNow.AddDays(-1), null, true, "Grant scoped contribution", null))).Should().NotBeEmpty();
        var afterAssignment = Payload(await reader.GetIndicatorsPage(new PagedQueryRequest(), module.Version.PublicId));
        afterAssignment.Items.Should().ContainSingle().Which.PublicId.Should().Be(module.Indicator.PublicId);
        var reportOnlyReader = Controller(context, seed.User, seed.Municipality.Id,
            permissionRule: permission => permission == "C88_REPORT.READ");
        Payload(await reportOnlyReader.GetIndicatorsPage(new PagedQueryRequest(), module.Version.PublicId)).Items.Should().ContainSingle()
            .Which.PublicId.Should().Be(module.Indicator.PublicId);
        Payload(await reader.GetAssignmentsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Should().ContainSingle()
            .Which.IndicatorPublicId.Should().Be(module.Indicator.PublicId);
        Payload(await reader.GetMappingsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Should().ContainSingle()
            .Which.IndicatorPublicId.Should().Be(module.Indicator.PublicId);
        Payload(await reader.GetPlansPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Should().ContainSingle()
            .Which.IndicatorPublicId.Should().Be(module.Indicator.PublicId);
    }

    [Fact]
    public async Task Sensitive_members_are_masked_from_reads_query_inference_and_direct_writes()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var manager = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, manager, seed);
        Payload(await manager.SavePlan(new SaveC88IndicatorPlanRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            "10", "20", "30", "plan-secret", DateTime.UtcNow.AddDays(7), "Create protected plan", null))).Should().NotBeEmpty();
        Payload(await manager.CreateMapping(new SaveC88MappingRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Target.PublicId, C88MappingType.Direct, "mapping-secret", true, null))).Should().NotBeEmpty();
        Payload(await manager.CreateWorkflow(new SaveC88WorkflowRequest(module.Configuration.PublicId, DateTime.UtcNow.AddDays(-1), null,
            [new(1, C88WorkflowStageKind.Capturer, "Capture", C88AssignmentRole.PrimaryCapturer, true),
             new(2, C88WorkflowStageKind.ReviewerVerifier, "Verify", C88AssignmentRole.ReviewerVerifier, true),
             new(3, C88WorkflowStageKind.FinalSubmission, "Final", C88AssignmentRole.FinalSubmitter, true)],
            "Create protected workflow", null, null))).Should().NotBeEmpty();
        Payload(await manager.CreateAssignment(new SaveC88AssignmentRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Employee.PublicId, C88AssignmentRole.PrimaryCapturer, DateTime.UtcNow.AddDays(-2), null, true, "Assign capturer", null))).Should().NotBeEmpty();
        var calendarId = Payload(await manager.CreateCalendar(new SaveC88ReportingCalendarRequest(module.Configuration.PublicId,
            module.ReportType.PublicId, null, "SEC", "Protected report", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2), true, "Open protected calendar", null)));
        var reportId = Payload(await manager.CreateReportVersion(new CreateC88ReportVersionRequest(module.Configuration.PublicId,
            calendarId, module.Indicator.PublicId, null, null, "report-missing-secret", DateTime.UtcNow.AddDays(5),
            [new(module.Numerator.PublicId, "45", "element-missing-secret", DateTime.UtcNow.AddDays(4)),
             new(module.Denominator.PublicId, "60", null, null)],
            [new(module.Question.PublicId, "true", "compliance-comment-secret")], "workflow-reason-secret")));

        var denied = Controller(context, seed.User, seed.Municipality.Id,
            permissionRule: permission => permission is "C88_INDICATOR.READ" or "C88_REPORT.READ");
        var deniedPlan = Payload(await denied.GetPlansPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Single();
        deniedPlan.MissingDataExplanation.Should().BeNull();
        var deniedMapping = Payload(await denied.GetMappingsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Single();
        deniedMapping.Reason.Should().BeNull();
        Payload(await denied.GetMappingsPage(new PagedQueryRequest { Search = "mapping-secret" }, seed.Year.PublicId)).TotalCount.Should().Be(0);
        var deniedReport = Payload(await denied.GetReportsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Single();
        deniedReport.PublicId.Should().Be(reportId);
        deniedReport.CalculatedValue.Should().BeNull();
        deniedReport.MissingDataExplanation.Should().BeNull();
        deniedReport.DataElementValues.Should().OnlyContain(item => item.Value == null && item.MissingDataExplanation == null);
        deniedReport.ComplianceResponses.Should().OnlyContain(item => item.Response == null && item.Comment == null);
        deniedReport.WorkflowActions.Should().OnlyContain(item => item.Reason == null && item.ActorUserPublicId == null && item.ActorName == null);

        var allowed = Controller(context, seed.User, seed.Municipality.Id,
            permissionRule: permission => permission.EndsWith(".READ", StringComparison.OrdinalIgnoreCase));
        Payload(await allowed.GetPlansPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Single().MissingDataExplanation.Should().Be("plan-secret");
        Payload(await allowed.GetMappingsPage(new PagedQueryRequest { Search = "mapping-secret" }, seed.Year.PublicId)).Items.Single().Reason.Should().Be("mapping-secret");
        var allowedReport = Payload(await allowed.GetReportsPage(new PagedQueryRequest(), seed.Year.PublicId)).Items.Single();
        allowedReport.CalculatedValue.Should().Be("75");
        allowedReport.MissingDataExplanation.Should().Be("report-missing-secret");
        allowedReport.DataElementValues.Should().Contain(item => item.Value == "45" && item.MissingDataExplanation == "element-missing-secret");
        allowedReport.ComplianceResponses.Should().ContainSingle(item => item.Response == "true" && item.Comment == "compliance-comment-secret");
        allowedReport.WorkflowActions.Should().ContainSingle(item => item.Reason == "workflow-reason-secret"
            && item.ActorUserPublicId == seed.User.PublicId && item.ActorName == seed.User.FullName);

        var writeDenied = Controller(context, seed.User, seed.Municipality.Id, permissionRule: permission =>
            permission is not "C88_INDICATOR.PlanMissingDataExplanation.UPDATE"
                and not "C88_INDICATOR.MappingReason.UPDATE"
                and not "C88_REPORT.WorkflowReason.UPDATE");
        (await writeDenied.SavePlan(new SaveC88IndicatorPlanRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            "11", "21", "31", "changed-secret", DateTime.UtcNow.AddDays(8), "Attempt protected plan edit", deniedPlan.RowVersion))).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await writeDenied.CreateMapping(new SaveC88MappingRequest(module.Configuration.PublicId, module.Indicator.PublicId,
            seed.Target.PublicId, C88MappingType.Contributing, "denied-mapping-secret", true, null))).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await writeDenied.Submit(reportId, new(allowedReport.RowVersion, "denied-workflow-secret"))).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Report_page_rejects_unknown_sort_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);

        var result = await controller.GetReportsPage(new PagedQueryRequest { SortBy = "calculated-sql" }, seed.Year.PublicId);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Assignment_and_mapping_pages_filter_before_count_and_page_stably()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        var effectiveFrom = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            var employee = new MunicipalEmployee
            {
                MunicipalityId = seed.Municipality.Id, EmployeeNumber = $"C88-{index:00}", FirstName = "Paged", LastName = $"Worker {index:00}",
                EffectiveFrom = effectiveFrom, IsActive = true
            };
            var target = new OpmsTarget
            {
                MunicipalityId = seed.Municipality.Id, IndicatorNumber = $"OPMS-{index:00}", KpiDescription = "Mapped KPI", TargetName = "Mapped KPI",
                PerformanceObjective = "Objective", AnnualTargetDescription = "Target", TargetUnitType = "number"
            };
            context.AddRange(employee, target);
            await context.SaveChangesAsync();
            context.AddRange(
                new C88Assignment { MunicipalityId = seed.Municipality.Id, C88MunicipalityConfigurationId = module.Configuration.Id, Configuration = module.Configuration, C88IndicatorId = module.Indicator.Id, Indicator = module.Indicator, MunicipalEmployeeId = employee.Id, MunicipalEmployee = employee, Role = C88AssignmentRole.Contributor, EffectiveFrom = effectiveFrom.AddDays(index), IsActive = true },
                new C88OpmsMapping { MunicipalityId = seed.Municipality.Id, C88MunicipalityConfigurationId = module.Configuration.Id, Configuration = module.Configuration, C88IndicatorId = module.Indicator.Id, Indicator = module.Indicator, OpmsTargetId = target.Id, OpmsTarget = target, MappingType = C88MappingType.Contributing, Reason = $"match mapping {index:00}", IsActive = true, CreatedAt = effectiveFrom.AddMinutes(index), CreatedByUserId = seed.User.Id, CreatedByUser = seed.User });
        }
        await context.SaveChangesAsync();

        var assignments = Payload(await controller.GetAssignmentsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Worker", SortBy = "employeeName", SortDirection = "asc" }, seed.Year.PublicId));
        assignments.TotalCount.Should().Be(11);
        assignments.Items.Select(item => item.EmployeeName).Should().Equal("Paged Worker 03", "Paged Worker 04", "Paged Worker 05");
        var mappings = Payload(await controller.GetMappingsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "opmsIndicator", SortDirection = "asc" }, seed.Year.PublicId));
        mappings.TotalCount.Should().Be(11);
        mappings.Items.Select(item => item.OpmsIndicatorNumber).Should().Equal("OPMS-03", "OPMS-04", "OPMS-05");
        (await controller.GetAssignmentsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetMappingsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Plan_page_filters_before_count_and_pages_stably()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        var createdAt = new DateTime(2035, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            var indicator = new C88Indicator
            {
                MunicipalityId = seed.Municipality.Id, C88CatalogueVersionId = module.Version.Id, CatalogueVersion = module.Version,
                Code = $"PLAN-{index:00}", Name = $"Paged plan {index:00}", Definition = "Definition",
                OfficialTechnicalIndicatorDescription = "Official TID", ValueType = C88ValueType.Decimal,
                CalculationOperator = C88ControlledCalculationOperator.None, IsActive = true
            };
            context.AddRange(indicator, new C88IndicatorPlan
            {
                MunicipalityId = seed.Municipality.Id, C88MunicipalityConfigurationId = module.Configuration.Id, Configuration = module.Configuration,
                Indicator = indicator, BaselineValue = $"baseline {index:00}", AnnualTarget = $"annual {index:00}",
                CreatedAt = createdAt.AddMinutes(index), CreatedByUserId = seed.User.Id, CreatedByUser = seed.User
            });
        }
        await context.SaveChangesAsync();

        var page = Payload(await controller.GetPlansPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "baseline", SortBy = "indicatorCode", SortDirection = "asc" }, seed.Year.PublicId));
        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.IndicatorCode).Should().Equal("PLAN-03", "PLAN-04", "PLAN-05");
        (await controller.GetPlansPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Calendar_and_workflow_pages_filter_before_count_and_page_stably()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        var effectiveFrom = new DateTime(2035, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            var workflow = new C88WorkflowDefinition
            {
                MunicipalityId = seed.Municipality.Id, C88MunicipalityConfigurationId = module.Configuration.Id, Configuration = module.Configuration,
                VersionNumber = index + 1, IsCurrent = index == 10, IsActive = true, EffectiveFrom = effectiveFrom.AddDays(index),
                EffectiveTo = index == 10 ? null : effectiveFrom.AddDays(index + 1), CreatedByUserId = seed.User.Id, CreatedByUser = seed.User
            };
            workflow.Stages.Add(new C88WorkflowStage
            {
                MunicipalityId = seed.Municipality.Id, Sequence = 1, Kind = C88WorkflowStageKind.ReviewerVerifier,
                Name = $"Verify stage {index:00}", RequiredRole = C88AssignmentRole.ReviewerVerifier, IsActive = true
            });
            context.AddRange(
                new C88ReportingCalendar
                {
                    MunicipalityId = seed.Municipality.Id, C88MunicipalityConfigurationId = module.Configuration.Id, Configuration = module.Configuration,
                    ReportTypeItemId = module.ReportType.Id, ReportTypeItem = module.ReportType, Code = $"CAL-{index:00}", Name = $"Paged calendar {index:00}",
                    OpensAt = effectiveFrom.AddDays(index), ClosesAt = effectiveFrom.AddDays(index + 1), DueAt = effectiveFrom.AddDays(index + 2), IsActive = true
                },
                workflow);
        }
        await context.SaveChangesAsync();

        var calendars = Payload(await controller.GetCalendarsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "calendar", SortBy = "code", SortDirection = "asc" }, seed.Year.PublicId));
        calendars.TotalCount.Should().Be(11);
        calendars.Items.Select(item => item.Code).Should().Equal("CAL-03", "CAL-04", "CAL-05");
        var workflows = Payload(await controller.GetWorkflowsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Verify", SortBy = "versionNumber", SortDirection = "asc" }, seed.Year.PublicId));
        workflows.TotalCount.Should().Be(11);
        workflows.Items.Select(item => item.VersionNumber).Should().Equal(4, 5, 6);
        Payload(await controller.GetWorkflowsPage(new PagedQueryRequest(), configurationPublicId: module.Configuration.PublicId, current: true)).Items.Should().ContainSingle().Which.VersionNumber.Should().Be(11);
        (await controller.GetCalendarsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetWorkflowsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();

        var denied = Controller(context, seed.User, seed.Municipality.Id, permissionRule: _ => false);
        (await denied.GetCalendarsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await denied.GetWorkflowsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Compliance_question_page_filters_before_count_and_pages_stably()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        var responseType = await context.C88CatalogueItems.SingleAsync(item => item.C88CatalogueVersionId == module.Version.Id && item.Kind == C88CatalogueItemKind.ResponseType);
        for (var index = 0; index < 11; index++)
            context.C88ComplianceQuestions.Add(new C88ComplianceQuestion
            {
                MunicipalityId = seed.Municipality.Id, C88CatalogueVersionId = module.Version.Id, CatalogueVersion = module.Version,
                ReportTypeItemId = module.ReportType.Id, ReportTypeItem = module.ReportType, ResponseTypeItemId = responseType.Id, ResponseTypeItem = responseType,
                Code = $"PAGE-{index:00}", Prompt = $"Paged prompt {index:00}", IsRequired = index % 2 == 0, Sequence = 100 + index, IsActive = true
            });
        await context.SaveChangesAsync();

        var page = Payload(await controller.GetComplianceQuestionsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Paged prompt", SortBy = "sequence", SortDirection = "asc" }, module.Version.PublicId, module.ReportType.PublicId, true));
        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.Code).Should().Equal("PAGE-03", "PAGE-04", "PAGE-05");
        var required = Payload(await controller.GetComplianceQuestionsPage(new PagedQueryRequest { PageSize = 10 }, module.Version.PublicId, module.ReportType.PublicId, true, true));
        required.TotalCount.Should().Be(7, "the original required question and six even-numbered paged questions match");
        (await controller.GetComplianceQuestionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();

        var denied = Controller(context, seed.User, seed.Municipality.Id, permissionRule: _ => false);
        (await denied.GetComplianceQuestionsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Indicator_page_filters_before_count_and_pages_nested_details_stably()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var module = await CreateCatalogueAsync(context, controller, seed);
        for (var index = 0; index < 11; index++)
        {
            var indicator = new C88Indicator
            {
                MunicipalityId = seed.Municipality.Id, C88CatalogueVersionId = module.Version.Id, CatalogueVersion = module.Version,
                Code = $"PAGE-{index:00}", Name = $"Paged indicator {index:00}", Definition = $"Paged definition {index:00}",
                OfficialTechnicalIndicatorDescription = "Official TID", ValueType = index % 2 == 0 ? C88ValueType.Decimal : C88ValueType.Integer,
                CalculationOperator = C88ControlledCalculationOperator.None, IsActive = true
            };
            indicator.DataElements.Add(new C88DataElement
            {
                MunicipalityId = seed.Municipality.Id, Code = $"VALUE-{index:00}", Name = "Value", ValueType = indicator.ValueType,
                IsRequired = true, Sequence = 1
            });
            context.C88Indicators.Add(indicator);
        }
        await context.SaveChangesAsync();

        var page = Payload(await controller.GetIndicatorsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Paged indicator", SortBy = "code", SortDirection = "asc" }, module.Version.PublicId, active: true));
        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.Code).Should().Equal("PAGE-03", "PAGE-04", "PAGE-05");
        page.Items.Should().OnlyContain(item => item.DataElements.Length == 1);
        var decimals = Payload(await controller.GetIndicatorsPage(new PagedQueryRequest { PageSize = 10 }, module.Version.PublicId, active: true, valueType: C88ValueType.Decimal));
        decimals.TotalCount.Should().Be(6, "the six even-numbered paged indicators are decimal");
        (await controller.GetIndicatorsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();

        var denied = Controller(context, seed.User, seed.Municipality.Id, permissionRule: _ => false);
        (await denied.GetIndicatorsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Configuration_and_catalogue_register_pages_filter_before_count_and_page_stably()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context);
        var controller = Controller(context, seed.User, seed.Municipality.Id);
        var createdAt = new DateTime(2035, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            var year = 2040 + index;
            var financialYear = new FinancialYear
            {
                Code = $"{year}/{(year + 1) % 100:00}", Name = $"Paged year {index:00}",
                StartDate = new DateTime(year, 7, 1), EndDate = new DateTime(year + 1, 6, 30)
            };
            var municipalityYear = new MunicipalityFinancialYear
            {
                MunicipalityId = seed.Municipality.Id, FinancialYear = financialYear, IsActive = true,
                EffectiveFrom = financialYear.StartDate
            };
            var version = new C88CatalogueVersion
            {
                MunicipalityId = seed.Municipality.Id, Code = $"EDITION-{index:00}", Name = $"Paged edition {index:00}",
                EditionDate = createdAt.AddDays(index), EffectiveFrom = municipalityYear.EffectiveFrom, IsPublished = index % 2 == 0,
                IsActive = true, CreatedAt = createdAt.AddMinutes(index), CreatedByUserId = seed.User.Id, CreatedByUser = seed.User
            };
            context.AddRange(financialYear, municipalityYear, version, new C88MunicipalityConfiguration
            {
                MunicipalityId = seed.Municipality.Id, MunicipalityFinancialYear = municipalityYear, CatalogueVersion = version,
                IsEnabled = index % 2 == 0, EffectiveFrom = municipalityYear.EffectiveFrom,
                CreatedAt = createdAt.AddMinutes(index), CreatedByUserId = seed.User.Id, CreatedByUser = seed.User
            });
            context.Add(new C88CatalogueItem
            {
                MunicipalityId = seed.Municipality.Id, Municipality = seed.Municipality, CatalogueVersion = version,
                Kind = C88CatalogueItemKind.ReportType, Code = $"REPORT-{index:00}", Name = $"Paged report type {index:00}",
                Description = $"Searchable item {index:00}", DisplayOrder = index, IsActive = index % 2 == 0
            });
        }
        await context.SaveChangesAsync();

        var configurations = Payload(await controller.GetConfigurationsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Paged edition", SortBy = "catalogueVersion", SortDirection = "asc" }));
        configurations.TotalCount.Should().Be(11);
        configurations.Items.Select(item => item.CatalogueVersionCode).Should().Equal("EDITION-03", "EDITION-04", "EDITION-05");
        Payload(await controller.GetConfigurationsPage(new PagedQueryRequest { PageSize = 1 }, configurationPublicId: configurations.Items[0].PublicId)).Items
            .Should().ContainSingle().Which.PublicId.Should().Be(configurations.Items[0].PublicId);
        var versions = Payload(await controller.GetCatalogueVersionsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Paged edition", SortBy = "code", SortDirection = "asc" }));
        versions.TotalCount.Should().Be(11);
        versions.Items.Select(item => item.Code).Should().Equal("EDITION-03", "EDITION-04", "EDITION-05");
        var items = Payload(await controller.GetCatalogueItemsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "Paged report", SortBy = "displayOrder", SortDirection = "asc" }, kind: C88CatalogueItemKind.ReportType));
        items.TotalCount.Should().Be(11);
        items.Items.Select(item => item.Code).Should().Equal("REPORT-03", "REPORT-04", "REPORT-05");
        Payload(await controller.GetCatalogueItemsPage(new PagedQueryRequest { PageSize = 1 }, catalogueItemPublicId: items.Items[0].PublicId)).Items
            .Should().ContainSingle().Which.PublicId.Should().Be(items.Items[0].PublicId);
        Payload(await controller.GetCatalogueItemsPage(new PagedQueryRequest { PageSize = 10 }, kind: C88CatalogueItemKind.ReportType, active: true)).TotalCount.Should().Be(6);
        Payload(await controller.GetConfigurationsPage(new PagedQueryRequest { PageSize = 10 }, enabled: true)).TotalCount.Should().Be(6);
        Payload(await controller.GetCatalogueVersionsPage(new PagedQueryRequest { PageSize = 10 }, published: true, active: true)).TotalCount.Should().Be(6);
        (await controller.GetConfigurationsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetCatalogueVersionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetCatalogueItemsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();

        var denied = Controller(context, seed.User, seed.Municipality.Id, permissionRule: _ => false);
        (await denied.GetConfigurationsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await denied.GetCatalogueVersionsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await denied.GetCatalogueItemsPage(new PagedQueryRequest())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private static async Task<Module> CreateCatalogueAsync(ApplicationDbContext context, C88Controller controller, Seed seed)
    {
        var versionId = Payload(await controller.CreateCatalogueVersion(new("2026.1", "Treasury C88 2026", DateTime.UtcNow.Date,
            seed.Year.EffectiveFrom, null, false, true, "Create edition", null)));
        async Task<C88CatalogueItem> Item(C88CatalogueItemKind kind, string code, Guid? parent = null)
        {
            var id = Payload(await controller.CreateCatalogueItem(new(versionId, kind, code, code + " name", null, parent, 1, true, "Create catalogue item", null)));
            return await context.C88CatalogueItems.SingleAsync(value => value.PublicId == id);
        }
        var sector = await Item(C88CatalogueItemKind.Sector, "GOV");
        var outcome = await Item(C88CatalogueItemKind.Outcome, "GOV-1", sector.PublicId);
        var type = await Item(C88CatalogueItemKind.IndicatorType, "OUTCOME");
        var category = await Item(C88CatalogueItemKind.MunicipalCategory, "B");
        var tier = await Item(C88CatalogueItemKind.ReadinessTier, "TIER-2");
        var reportType = await Item(C88CatalogueItemKind.ReportType, "QUARTERLY");
        var responseType = await Item(C88CatalogueItemKind.ResponseType, "BOOLEAN");
        var indicatorId = Payload(await controller.CreateIndicator(new(versionId, "C88-001", "Households served", "Official definition", "Official technical indicator description",
            sector.PublicId, outcome.PublicId, type.PublicId, C88ValueType.Percentage, C88ControlledCalculationOperator.Percentage,
            "Numerator divided by denominator times 100", true, true, true, true,
            [new("NUM", "Numerator", null, C88ValueType.Decimal, true, 1), new("DEN", "Denominator", null, C88ValueType.Decimal, true, 2)],
            [new(category.PublicId, tier.PublicId, true, "Applicable")], "Create official indicator")));
        var indicator = await context.C88Indicators.Include(item => item.DataElements).Include(item => item.Applicability).SingleAsync(item => item.PublicId == indicatorId);
        var questionId = Payload(await controller.CreateComplianceQuestion(new(versionId, reportType.PublicId, responseType.PublicId,
            "Q1", "Was the source verified?", true, 1, true, "Create compliance question")));
        var version = await context.C88CatalogueVersions.SingleAsync(item => item.PublicId == versionId);
        Payload(await controller.UpdateCatalogueVersion(versionId, new(version.Code, version.Name, version.EditionDate, version.EffectiveFrom,
            version.EffectiveTo, true, true, "Publish controlled edition", Convert.ToBase64String(version.RowVersion)))).Should().Be(versionId);
        var configurationId = Payload(await controller.Configure(new(seed.Year.PublicId, versionId, true, seed.Year.EffectiveFrom, null, "Enable optional C88", null)));
        return new(await context.C88CatalogueVersions.SingleAsync(item => item.PublicId == versionId),
            await context.C88MunicipalityConfigurations.SingleAsync(item => item.PublicId == configurationId), indicator,
            indicator.DataElements.Single(item => item.Code == "NUM"), indicator.DataElements.Single(item => item.Code == "DEN"),
            reportType, await context.C88ComplianceQuestions.SingleAsync(item => item.PublicId == questionId));
    }

    private static async Task<Seed> SeedAsync(ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser("c88-user");
        var municipality = new Municipality { Id = 9301, Code = "C88", Name = "C88 Municipality" };
        user.MunicipalityId = municipality.Id;
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) };
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYear = financialYear, IsCurrent = true, IsActive = true, EffectiveFrom = financialYear.StartDate };
        var employee = new MunicipalEmployee { MunicipalityId = municipality.Id, EmployeeNumber = "E-1", FirstName = "C88", LastName = "Owner", IdentityUserId = user.Id, IsActive = true, EffectiveFrom = financialYear.StartDate };
        var target = new OpmsTarget { MunicipalityId = municipality.Id, IndicatorNumber = "OPMS-1", KpiDescription = "Core KPI", TargetName = "Core KPI", PerformanceObjective = "Objective", NationalKpa = "KPA", MunicipalKpa = "KPA", AnnualTarget = 80, AnnualTargetDescription = "Eighty", TargetUnitType = "percentage" };
        context.AddRange(user, municipality, financialYear, year, employee, target);
        await context.SaveChangesAsync();
        return new(user, municipality, year, employee, target);
    }

    private static C88Controller Controller(ApplicationDbContext context, ApplicationUser user, long municipalityId, Mock<IWorkflowGovernanceService>? audit = null, Func<string, bool>? permissionRule = null)
    {
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string permission, AccessScopeContext? _) =>
            {
                var allowed = permissionRule?.Invoke(permission) ?? true;
                return new AccessDecisionResult(allowed, allowed ? "Allowed" : "Denied", [], [], []);
            });
        return new C88Controller(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object,
            (audit ?? new Mock<IWorkflowGovernanceService>()).Object, new FixedTenantContext(municipalityId, false))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static T Payload<T>(ActionResult<ApiResponse<T>> action) => ((action.Result as OkObjectResult)!.Value as ApiResponse<T>)!.Data!;
    private sealed record Seed(ApplicationUser User, Municipality Municipality, MunicipalityFinancialYear Year, MunicipalEmployee Employee, OpmsTarget Target);
    private sealed record Module(C88CatalogueVersion Version, C88MunicipalityConfiguration Configuration, C88Indicator Indicator, C88DataElement Numerator, C88DataElement Denominator, C88CatalogueItem ReportType, C88ComplianceQuestion Question);
    private sealed class FixedTenantContext(long? municipalityId, bool isSystem) : ITenantContext { public long? MunicipalityId => municipalityId; public bool IsSystem => isSystem; public string? UserId => "c88-test"; }
}

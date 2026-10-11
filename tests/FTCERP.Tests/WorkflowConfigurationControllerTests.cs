using FTCERP.Host.Application.Services;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class WorkflowConfigurationControllerTests
{
    [Fact]
    public async Task CreateWindow_RequiresReasonAndWritesContextualAudit()
    {
        await using var context = IdpTestFixture.CreateContext();
        var actor = IdpTestFixture.CreateUser("window-admin");
        actor.MunicipalityId = 7;
        var year = new MunicipalityFinancialYear { Id = 610, MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = new DateTime(2035, 7, 1) };
        var period = new ReportingPeriod
        {
            Id = 611,
            MunicipalityFinancialYearId = year.Id,
            MunicipalityFinancialYear = year,
            Code = "Q1",
            Name = "Quarter 1",
            PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1,
            StartDate = new DateTime(2035, 7, 1),
            EndDate = new DateTime(2035, 9, 30)
        };
        context.AddRange(actor, year, period);
        await context.SaveChangesAsync();
        var controller = Controller(context, 7, actor);
        var opensAt = new DateTime(2035, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var closesAt = new DateTime(2035, 7, 31, 23, 59, 59, DateTimeKind.Utc);

        var result = await controller.CreateWindow(new SaveReportingWindowRequest(
            period.PublicId, SubmissionKind.Opms, opensAt, closesAt, "Approved quarterly submission timetable"));

        result.Result.Should().BeOfType<OkObjectResult>();
        var window = await context.ReportingWindows.SingleAsync();
        var audit = await context.AuditTrails.SingleAsync(item => item.EntityName == nameof(ReportingWindow));
        audit.EntityId.Should().Be(window.PublicId.ToString());
        audit.Action.Should().Be("Create");
        audit.Reason.Should().Be("Approved quarterly submission timetable");
        audit.NewValue.Should().Contain(period.PublicId.ToString());
    }

    [Fact]
    public async Task CreateRatingScheme_RequiresReasonAndWritesContextualAudit()
    {
        await using var context = IdpTestFixture.CreateContext();
        var actor = IdpTestFixture.CreateUser("rating-admin");
        actor.MunicipalityId = 7;
        context.Add(actor);
        await context.SaveChangesAsync();
        var controller = Controller(context, 7, actor);
        var values = new[]
        {
            new SaveRatingValueRequest(1m, "Not achieved", 0m, 49.99m, 1),
            new SaveRatingValueRequest(2m, "Achieved", 50m, 100m, 2)
        };

        var denied = await controller.CreateRatingScheme(new SaveRatingSchemeRequest(
            "TWO_POINT", "Two point scale", values, "short"));
        denied.Result.Should().BeOfType<BadRequestObjectResult>();
        context.RatingSchemes.Should().BeEmpty();

        var result = await controller.CreateRatingScheme(new SaveRatingSchemeRequest(
            "TWO_POINT", "Two point scale", values, "Approved assessment rating definition"));

        result.Result.Should().BeOfType<OkObjectResult>();
        var scheme = await context.RatingSchemes.Include(item => item.Values).SingleAsync();
        scheme.Values.Should().HaveCount(2);
        var audit = await context.AuditTrails.SingleAsync(item => item.EntityName == nameof(RatingScheme));
        audit.EntityId.Should().Be(scheme.PublicId.ToString());
        audit.Action.Should().Be("Create");
        audit.Reason.Should().Be("Approved assessment rating definition");
        audit.NewValue.Should().Contain("TWO_POINT").And.Contain("Not achieved");
    }

    [Fact]
    public async Task RetireDefinition_UsesRowVersionAndWritesReasonedAuditWithoutDeletingDefinition()
    {
        await using var context = IdpTestFixture.CreateContext();
        var definition = Definition(1, 101, true, "Original");
        context.AddRange(definition.MunicipalityFinancialYear, definition);
        await context.SaveChangesAsync();
        var controller = Controller(context);

        var result = await controller.RetireDefinition(definition.PublicId, new RetireWorkflowDefinitionRequest("Annual governance retirement", DateTime.UtcNow, Convert.ToBase64String(definition.RowVersion)));

        result.Result.Should().BeOfType<OkObjectResult>();
        definition.IsActive.Should().BeFalse();
        definition.EffectiveTo.Should().NotBeNull();
        context.WorkflowDefinitions.IgnoreQueryFilters().Should().ContainSingle(item => item.PublicId == definition.PublicId);
        var audit = await context.AuditTrails.IgnoreQueryFilters().SingleAsync(item => item.EntityId == definition.PublicId.ToString());
        audit.Action.Should().Be("Retire");
        audit.NewValue.Should().Contain("Annual governance retirement");
    }

    [Fact]
    public async Task CompareDefinitions_ReturnsOnlySameLineageStageDifferences()
    {
        await using var context = IdpTestFixture.CreateContext();
        var prior = Definition(1, 201, false, "Submit");
        var current = Definition(2, 202, true, "Capture and submit");
        current.MunicipalityFinancialYear = prior.MunicipalityFinancialYear;
        current.MunicipalityFinancialYearId = prior.MunicipalityFinancialYearId;
        context.AddRange(prior.MunicipalityFinancialYear, prior, current);
        await context.SaveChangesAsync();
        var controller = Controller(context);

        var result = await controller.CompareDefinitions(prior.PublicId, current.PublicId);

        var response = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<WorkflowDefinitionComparisonDto>>().Subject;
        response.Data!.From.Version.Should().Be(1);
        response.Data.To.Version.Should().Be(2);
        response.Data.StageDifferences.Single().ChangedFields.Should().Contain("Name");
    }

    [Fact]
    public async Task RfiPage_AppliesScopeAndFiltersBeforeCount_AndRetiresLegacyArray()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "RFI-PAGE", Name = "RFI Page Municipality" };
        var user = IdpTestFixture.CreateUser("rfi-reader");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var financialYear = new FinancialYear { Code = "2032/33", Name = "2032/33", StartDate = new(2032, 7, 1), EndDate = new(2033, 6, 30) };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear, EffectiveFrom = financialYear.StartDate };
        var target = new OpmsTarget { MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "RFI-KPI", TargetName = "RFI target", KpiDescription = "RFI target", AnnualTargetDescription = "Target", PerformanceObjective = "Objective" };
        context.AddRange(year, target);
        await context.SaveChangesAsync();
        var submission = new OpmsSubmission { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, OpmsTarget = target, Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "reviewed", SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow };
        var workflow = new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "RFI", Name = "RFI workflow", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        context.AddRange(submission, workflow);
        await context.SaveChangesAsync();
        var instance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id };
        var otherInstance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = "other-submission" };
        context.AddRange(instance, otherInstance);
        await context.SaveChangesAsync();
        var raisedAt = DateTime.UtcNow.AddDays(-10);
        for (var index = 0; index < 31; index++)
        {
            context.PerformanceRfis.Add(new PerformanceRfi
            {
                MunicipalityId = municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                Question = $"match-{index:00}",
                RaisedByUserId = user.Id,
                RaisedAt = raisedAt.AddMinutes(index),
                ResponseDueAt = raisedAt.AddDays(5),
                Response = index % 2 == 0 ? null : "answered",
                RespondedByUserId = index % 2 == 0 ? null : user.Id,
                RespondedAt = index % 2 == 0 ? null : raisedAt.AddMinutes(index + 1)
            });
        }
        context.PerformanceRfis.Add(new PerformanceRfi { MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = otherInstance.Id, SubmissionWorkflowInstance = otherInstance, Question = "match-outside", RaisedByUserId = user.Id, RaisedAt = raisedAt, ResponseDueAt = raisedAt.AddDays(5) });
        await context.SaveChangesAsync();
        var evidenceRfi = await context.PerformanceRfis.SingleAsync(item => item.Question == "match-30");
        var evidenceBlob = new EvidenceBlob { MunicipalityId = municipality.Id, StorageKey = "rfi/secret-evidence", ContentType = "application/pdf", SizeInBytes = 321, Sha256 = new string('a', 64), SignatureVerified = true, ScanStatus = "Clean" };
        var evidence = new PoeFile { Id = "rfi-evidence", MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id, FileName = "secret-evidence.pdf", Blob = evidenceBlob, UploadedByUserId = user.Id, UploadedByUser = user, IsActive = true };
        context.PerformanceRfiEvidenceLinks.Add(new PerformanceRfiEvidence { MunicipalityId = municipality.Id, PerformanceRfi = evidenceRfi, PoeFile = evidence, Purpose = RfiEvidencePurpose.Question, LinkedByUserId = user.Id, LinkedByUser = user, CorrelationId = "rfi-link" });
        await context.SaveChangesAsync();

        var controller = Controller(context, municipality.Id, user);
        var action = await controller.GetRfisPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "raisedAt", SortDirection = "asc" }, "open");
        var page = action.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<PerformanceRfiDto>>>().Subject.Data!;

        page.TotalCount.Should().Be(16);
        page.Items.Should().HaveCount(6);
        page.Items.First().Question.Should().Be("match-20");
        page.Items.Should().NotContain(item => item.Question == "match-outside");
        (await controller.GetRfisPage(SubmissionKind.Opms, submission.PublicId.ToString(), new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetRfis(SubmissionKind.Opms, submission.PublicId.ToString())).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);

        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_RFI.READ", "OPMS_RFI.RAISE", "OPMS_RFI.RESPOND", "OPMS_RFI.CLOSE" };
        var restricted = Controller(context, municipality.Id, user, grants.Contains);
        var masked = (await restricted.GetRfisPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { PageSize = 5, SortBy = "raisedAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<PerformanceRfiDto>>>().Subject.Data!;
        masked.Items.Should().OnlyContain(item => item.Question == null && item.Response == null
            && item.RaisedByUserPublicId == null && item.RaisedByName == null
            && item.RespondedByUserPublicId == null && item.RespondedByName == null
            && item.ClosedByUserPublicId == null && item.ClosedByName == null);
        masked.Items.SelectMany(item => item.Evidence).Should().OnlyContain(item => item.FileName == null
            && item.ContentType == null && item.SizeInBytes == null && item.Sha256 == null && item.Url == null
            && item.LinkedByUserPublicId == null && item.LinkedByName == null);
        var deniedSearch = (await restricted.GetRfisPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = "match-30", SortBy = "raisedAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<PerformanceRfiDto>>>().Subject.Data!;
        deniedSearch.TotalCount.Should().Be(0);
        (await restricted.RaiseRfi(SubmissionKind.Opms, submission.PublicId.ToString(),
            new RaisePerformanceRfiRequest("Protected question", DateTime.UtcNow.AddDays(2)))).Result.Should().BeOfType<ForbidResult>();
        var openRfi = await context.PerformanceRfis.SingleAsync(item => item.Question == "match-00");
        (await restricted.RespondRfi(openRfi.PublicId,
            new RespondPerformanceRfiRequest("Protected response", Convert.ToBase64String(openRfi.RowVersion)))).Result.Should().BeOfType<ForbidResult>();

        foreach (var member in new[] { "Question", "RaisedBy", "Response", "RespondedBy", "ClosedBy", "EvidenceMetadata", "EvidenceLinkedBy" })
            grants.Add($"OPMS_RFI.{member}.READ");
        grants.Add("OPMS_RFI.Question.UPDATE");
        grants.Add("OPMS_RFI.Response.UPDATE");
        var visible = (await restricted.GetRfisPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = "match-30", SortBy = "raisedAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<PerformanceRfiDto>>>().Subject.Data!;
        visible.Items.Should().ContainSingle(item => item.Question == "match-30"
            && item.RaisedByUserPublicId == user.PublicId && item.RaisedByName != null);
        var visibleEvidence = visible.Items.Single().Evidence.Should().ContainSingle().Subject;
        visibleEvidence.FileName.Should().Be("secret-evidence.pdf");
        visibleEvidence.Sha256.Should().Be(new string('a', 64));
        visibleEvidence.LinkedByUserPublicId.Should().Be(user.PublicId);
        var raisedResult = await restricted.RaiseRfi(SubmissionKind.Opms, submission.PublicId.ToString(),
            new RaisePerformanceRfiRequest("Protected question", DateTime.UtcNow.AddDays(2)));
        var raised = raisedResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PerformanceRfiDto>>().Subject.Data!;
        raised.Question.Should().Be("Protected question");
        raised.RaisedByUserPublicId.Should().Be(user.PublicId);
        var responseResult = await restricted.RespondRfi(openRfi.PublicId,
            new RespondPerformanceRfiRequest("Protected response", Convert.ToBase64String(openRfi.RowVersion)));
        var responded = responseResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PerformanceRfiDto>>().Subject.Data!;
        responded.Response.Should().Be("Protected response");
        responded.RespondedByUserPublicId.Should().Be(user.PublicId);
        var closeResult = await restricted.CloseRfi(openRfi.PublicId,
            new ClosePerformanceRfiRequest("Response accepted", responded.RowVersion));
        var closed = closeResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PerformanceRfiDto>>().Subject.Data!;
        closed.ClosedAt.Should().NotBeNull();
        closed.ClosedByUserPublicId.Should().Be(user.PublicId);
    }

    [Fact]
    public async Task GovernancePages_FilterBeforeCount_PageDeterministically_AndRetireLegacyArrays()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WF-GOV", Name = "Workflow Governance Municipality" };
        context.Municipalities.Add(municipality);
        var financialYear = new FinancialYear { Code = "2034/35", Name = "2034/35", StartDate = new(2034, 7, 1), EndDate = new(2035, 6, 30) };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var governanceActor = IdpTestFixture.CreateUser("governance-admin");
        governanceActor.MunicipalityId = municipality.Id;
        context.Users.Add(governanceActor);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear, EffectiveFrom = financialYear.StartDate };
        context.MunicipalityFinancialYears.Add(year);
        await context.SaveChangesAsync();
        var startsAt = new DateTime(2034, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 11; index++)
        {
            var period = new ReportingPeriod { MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, Code = $"MATCH-P{index:00}", Name = $"Match period {index:00}", PeriodType = ReportingPeriodType.Quarter1, Sequence = index + 1, StartDate = startsAt.AddDays(index), EndDate = startsAt.AddDays(index + 1) };
            context.ReportingPeriods.Add(period);
            context.WorkflowDefinitions.Add(new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "MATCH", Name = $"Match workflow {index:00}", Version = index + 1, EffectiveFrom = startsAt.AddDays(index) });
            context.RatingSchemes.Add(new RatingScheme { MunicipalityId = municipality.Id, Code = $"MATCH-{index:00}", Name = $"Match scheme {index:00}" });
            await context.SaveChangesAsync();
            context.ReportingWindows.Add(new ReportingWindow { MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, ReportingPeriod = period, SubmissionKind = SubmissionKind.Opms, OpensAt = startsAt.AddDays(index), ClosesAt = startsAt.AddDays(index + 1) });
        }
        context.WorkflowDefinitions.Add(new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Ipms, Code = "OUTSIDE", Name = "Outside workflow", Version = 1, EffectiveFrom = startsAt });
        context.RatingSchemes.Add(new RatingScheme { MunicipalityId = municipality.Id, Code = "OUTSIDE", Name = "Outside scheme" });
        await context.SaveChangesAsync();
        var exceptionWindow = await context.ReportingWindows.OrderBy(item => item.Id).FirstAsync();
        var otherWindow = await context.ReportingWindows.OrderBy(item => item.Id).Skip(1).FirstAsync();
        var exceptionDepartments = Enumerable.Range(0, 11).Select(index => new Department
        {
            MunicipalityId = municipality.Id,
            Code = $"MATCH-D{index:00}",
            Name = $"Match department {index:00}",
            EffectiveFrom = startsAt
        }).ToArray();
        context.Departments.AddRange(exceptionDepartments);
        await context.SaveChangesAsync();
        for (var index = 0; index < 11; index++)
            context.ReportingWindowExceptions.Add(new ReportingWindowException { MunicipalityId = municipality.Id, ReportingWindowId = exceptionWindow.Id, ReportingWindow = exceptionWindow, DepartmentId = exceptionDepartments[index].Id, ExtendedClosesAt = exceptionWindow.ClosesAt.AddDays(index + 1), Reason = $"match extension {index:00}", ApprovedByUserId = governanceActor.Id, ApprovedAt = startsAt.AddMinutes(index) });
        context.ReportingWindowExceptions.Add(new ReportingWindowException { MunicipalityId = municipality.Id, ReportingWindowId = otherWindow.Id, ReportingWindow = otherWindow, DepartmentId = 99, ExtendedClosesAt = otherWindow.ClosesAt.AddDays(1), Reason = "match outside window", ApprovedByUserId = governanceActor.Id, ApprovedAt = startsAt });
        await context.SaveChangesAsync();

        var controller = Controller(context, municipality.Id, governanceActor);
        var definitionResult = await controller.GetDefinitionsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "version", SortDirection = "asc" }, SubmissionKind.Opms);
        var definitions = definitionResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<WorkflowDefinitionDto>>>().Subject.Data!;
        definitions.TotalCount.Should().Be(11);
        definitions.Items.Select(item => item.Version).Should().Equal(4, 5, 6);

        var windowResult = await controller.GetWindowsPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "period", SortDirection = "asc" }, SubmissionKind.Opms, true);
        var windows = windowResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<ReportingWindowDto>>>().Subject.Data!;
        windows.TotalCount.Should().Be(11);
        windows.Items.Select(item => item.PeriodCode).Should().Equal("MATCH-P03", "MATCH-P04", "MATCH-P05");

        var ratingResult = await controller.GetRatingSchemesPage(new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "code", SortDirection = "asc" }, true);
        var ratings = ratingResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<RatingSchemeDto>>>().Subject.Data!;
        ratings.TotalCount.Should().Be(11);
        ratings.Items.Select(item => item.Code).Should().Equal("MATCH-03", "MATCH-04", "MATCH-05");

        var exceptionResult = await controller.GetWindowExceptionsPage(exceptionWindow.PublicId, new PagedQueryRequest { Page = 2, PageSize = 3, Search = "match", SortBy = "approvedAt", SortDirection = "asc" }, "department");
        var exceptions = exceptionResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiResponse<PagedResponse<ReportingWindowExceptionDto>>>().Subject.Data!;
        exceptions.TotalCount.Should().Be(11);
        exceptions.Items.Select(item => item.ScopeName).Should().Equal("Match department 03", "Match department 04", "Match department 05");
        exceptions.Items.Should().OnlyContain(item => item.ScopeType == "Department" && item.ScopePublicId.HasValue);

        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WORKFLOW.CONFIGURE" };
        var restricted = Controller(context, municipality.Id, governanceActor, grants.Contains);
        var maskedResult = await restricted.GetWindowExceptionsPage(exceptionWindow.PublicId,
            new PagedQueryRequest { PageSize = 3, SortBy = "approvedAt" });
        var masked = maskedResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<ReportingWindowExceptionDto>>>().Subject.Data!;
        masked.Items.Should().OnlyContain(item => item.ScopePublicId == null && item.ScopeName == null
            && item.Reason == null && item.ApprovedByUserPublicId == null && item.ApprovedByName == null);
        var deniedSearch = (await restricted.GetWindowExceptionsPage(exceptionWindow.PublicId,
            new PagedQueryRequest { Search = "match extension 10", SortBy = "approvedAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<ReportingWindowExceptionDto>>>().Subject.Data!;
        deniedSearch.TotalCount.Should().Be(0);
        (await restricted.GetWindowExceptionsPage(exceptionWindow.PublicId,
            new PagedQueryRequest { SortBy = "scope" })).Result.Should().BeOfType<ForbidResult>();
        (await restricted.GetWindowExceptionsPage(exceptionWindow.PublicId,
            new PagedQueryRequest { SortBy = "approvedAt" }, "department")).Result.Should().BeOfType<ForbidResult>();
        (await restricted.CreateWindowException(exceptionWindow.PublicId,
            new SaveReportingWindowExceptionRequest(null, exceptionDepartments[0].PublicId, null,
                exceptionWindow.ClosesAt.AddDays(20), "Controlled extension reason"))).Result.Should().BeOfType<ForbidResult>();

        foreach (var permission in new[] { "OPMS_WORKFLOW.WindowExceptionScope.READ", "OPMS_WORKFLOW.WindowExceptionScope.UPDATE",
                     "OPMS_WORKFLOW.WindowExceptionReason.READ", "OPMS_WORKFLOW.WindowExceptionReason.UPDATE",
                     "OPMS_WORKFLOW.WindowExceptionApprovedBy.READ" })
            grants.Add(permission);
        var grantedSearch = (await restricted.GetWindowExceptionsPage(exceptionWindow.PublicId,
            new PagedQueryRequest { Search = "match extension 10", SortBy = "scope" }, "department")).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<ReportingWindowExceptionDto>>>().Subject.Data!;
        grantedSearch.Items.Should().ContainSingle(item => item.Reason == "match extension 10"
            && item.ScopePublicId == exceptionDepartments[10].PublicId);
        var createResult = await restricted.CreateWindowException(exceptionWindow.PublicId,
            new SaveReportingWindowExceptionRequest(null, exceptionDepartments[0].PublicId, null,
                exceptionWindow.ClosesAt.AddDays(20), "Controlled extension reason"));
        var created = createResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<ReportingWindowExceptionDto>>().Subject.Data!;
        created.ScopePublicId.Should().Be(exceptionDepartments[0].PublicId);
        created.Reason.Should().Be("Controlled extension reason");

        (await controller.GetDefinitionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetWindowExceptionsPage(exceptionWindow.PublicId, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetDefinitions().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        controller.GetWindows().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        controller.GetRatingSchemes().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        (await controller.GetWindowExceptions(exceptionWindow.PublicId)).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task WorkflowEvidencePages_ApplySubmissionScopeBeforeCount_AndRetireLegacyArrays()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WF-EVIDENCE", Name = "Workflow Evidence Municipality" };
        var user = IdpTestFixture.CreateUser("workflow-reader");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var financialYear = new FinancialYear { Code = "2033/34", Name = "2033/34", StartDate = new(2033, 7, 1), EndDate = new(2034, 6, 30) };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var year = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear, EffectiveFrom = financialYear.StartDate };
        var target = new OpmsTarget { MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "WF-KPI", TargetName = "Workflow target", KpiDescription = "Workflow target", AnnualTargetDescription = "Target", PerformanceObjective = "Objective" };
        var scheme = new RatingScheme { MunicipalityId = municipality.Id, Code = "FIVE", Name = "Five point" };
        var value = new RatingSchemeValue { MunicipalityId = municipality.Id, RatingScheme = scheme, Value = 4, Label = "Exceeded", SortOrder = 4 };
        context.AddRange(year, target, scheme, value);
        await context.SaveChangesAsync();
        var submission = new OpmsSubmission { MunicipalityId = municipality.Id, OpmsTargetId = target.Id, OpmsTarget = target, Quarter = "Q1", BaseState = SubmissionBaseStates.Submitted, Status = "reviewed", SubmittedByUserId = user.Id, SubmittedAt = DateTime.UtcNow };
        var workflow = new WorkflowDefinition { MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "EVIDENCE", Name = "Evidence workflow", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var stage = new WorkflowStageDefinition { MunicipalityId = municipality.Id, WorkflowDefinition = workflow, Code = "REVIEW", Name = "Review", Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.REVIEW", RequiredPermissionCode = "OPMS_SUBMISSION.READ", IsTerminal = true, RequiresRating = true, RatingScheme = scheme };
        context.AddRange(submission, workflow, stage);
        await context.SaveChangesAsync();
        var instance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id };
        var otherInstance = new SubmissionWorkflowInstance { MunicipalityId = municipality.Id, WorkflowDefinitionId = workflow.Id, WorkflowDefinition = workflow, SubmissionKind = SubmissionKind.Opms, SubmissionId = "other-submission" };
        context.AddRange(instance, otherInstance);
        await context.SaveChangesAsync();
        var occurredAt = new DateTime(2033, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var actions = new List<SubmissionWorkflowAction>();
        for (var index = 0; index < 21; index++)
        {
            var action = new SubmissionWorkflowAction
            {
                MunicipalityId = municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                Sequence = index + 1,
                ActionCode = $"MATCH-ACTION-{index:00}",
                Outcome = WorkflowActionOutcome.Approve,
                ActorUserId = user.Id,
                Comment = $"match-comment-{index:00}",
                OccurredAt = occurredAt.AddMinutes(index)
            };
            actions.Add(action);
            context.SubmissionWorkflowActions.Add(action);
        }
        var outsideAction = new SubmissionWorkflowAction { MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = otherInstance.Id, SubmissionWorkflowInstance = otherInstance, Sequence = 1, ActionCode = "MATCH-OUTSIDE", Outcome = WorkflowActionOutcome.Approve, ActorUserId = user.Id, OccurredAt = occurredAt };
        context.SubmissionWorkflowActions.Add(outsideAction);
        await context.SaveChangesAsync();
        for (var index = 0; index < actions.Count; index++)
        {
            context.SubmissionStageRatings.Add(new SubmissionStageRating
            {
                MunicipalityId = municipality.Id,
                SubmissionWorkflowInstanceId = instance.Id,
                SubmissionWorkflowInstance = instance,
                SubmissionWorkflowActionId = actions[index].Id,
                SubmissionWorkflowAction = actions[index],
                WorkflowStageDefinitionId = stage.Id,
                WorkflowStageDefinition = stage,
                RatingSchemeId = scheme.Id,
                RatingScheme = scheme,
                RatingSchemeValueId = value.Id,
                RatingSchemeValue = value,
                Value = value.Value,
                LabelSnapshot = $"match-label-{index:00}",
                RatedByUserId = user.Id,
                RatedAt = occurredAt.AddMinutes(index)
            });
        }
        context.SubmissionStageRatings.Add(new SubmissionStageRating { MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = otherInstance.Id, SubmissionWorkflowInstance = otherInstance, SubmissionWorkflowActionId = outsideAction.Id, SubmissionWorkflowAction = outsideAction, WorkflowStageDefinitionId = stage.Id, WorkflowStageDefinition = stage, RatingSchemeId = scheme.Id, RatingScheme = scheme, RatingSchemeValueId = value.Id, RatingSchemeValue = value, Value = value.Value, LabelSnapshot = "match-outside", RatedByUserId = user.Id, RatedAt = occurredAt });
        await context.SaveChangesAsync();

        var controller = Controller(context, municipality.Id, user);
        var actionResult = await controller.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "occurredAt", SortDirection = "asc" });
        var actionPage = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        actionPage.TotalCount.Should().Be(21);
        actionPage.Items.Should().HaveCount(10);
        actionPage.Items.First().ActionCode.Should().Be("MATCH-ACTION-10");
        actionPage.Items.Should().NotContain(item => item.ActionCode == "MATCH-OUTSIDE");

        var ratingResult = await controller.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Page = 2, PageSize = 10, Search = "match", SortBy = "ratedAt", SortDirection = "desc" });
        var ratingPage = ratingResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        ratingPage.TotalCount.Should().Be(21);
        ratingPage.Items.Should().HaveCount(10);
        ratingPage.Items.First().Label.Should().Be("match-label-10");
        ratingPage.Items.Should().NotContain(item => item.Label == "match-outside");

        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_SUBMISSION.READ" };
        var restrictedController = Controller(context, municipality.Id, user, grants.Contains);
        var maskedActions = (await restrictedController.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { PageSize = 10, SortBy = "occurredAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        maskedActions.Items.Should().OnlyContain(item => item.ActorUserPublicId == null && item.ActorName == null && item.Comment == null && item.RatingValue == null);
        var deniedActionSearch = (await restrictedController.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = "match-comment-20", SortBy = "occurredAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        deniedActionSearch.TotalCount.Should().Be(0);
        (await restrictedController.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { SortBy = "actor" })).Result.Should().BeOfType<ForbidResult>();

        var maskedRatings = (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { PageSize = 10, SortBy = "ratedAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        maskedRatings.Items.Should().OnlyContain(item => item.RatingValuePublicId == null && item.Value == null
            && item.Label == null && item.AchievementPercent == null && item.Comment == null
            && item.RatedByUserPublicId == null && item.RatedByName == null);
        var deniedRatingSearch = (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = "match-label-20", SortBy = "ratedAt" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        deniedRatingSearch.TotalCount.Should().Be(0);
        (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { SortBy = "value" })).Result.Should().BeOfType<ForbidResult>();
        (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { SortBy = "actor" })).Result.Should().BeOfType<ForbidResult>();

        foreach (var member in new[] { "ActionActorUserId", "ActionComment", "ActionRatingValue", "StageRatingValue",
                     "StageRatingAchievementPercent", "StageRatingComment", "StageRatingRatedByUserId", "StageRatingRatedByName" })
            grants.Add($"OPMS_WORKFLOW.{member}.READ");
        var grantedActions = (await restrictedController.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = "match-comment-20", SortBy = "actor" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        grantedActions.Items.Should().ContainSingle(item => item.Comment == "match-comment-20"
            && item.ActorUserPublicId == user.PublicId && item.ActorName == user.FullName);
        var grantedRatings = (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = "match-label-20", SortBy = "value" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        grantedRatings.Items.Should().ContainSingle(item => item.Label == "match-label-20"
            && item.RatedByUserPublicId == user.PublicId && item.RatedByName == user.FullName);

        var publicActorActions = (await restrictedController.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = user.PublicId.ToString(), SortBy = "actor" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        publicActorActions.TotalCount.Should().Be(21);
        var publicActorRatings = (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = user.PublicId.ToString(), SortBy = "actor" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        publicActorRatings.TotalCount.Should().Be(21);
        var rawActorActions = (await restrictedController.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = user.Id, SortBy = "actor" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<WorkflowActionDto>>>().Subject.Data!;
        rawActorActions.TotalCount.Should().Be(0);
        var rawActorRatings = (await restrictedController.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(),
            new PagedQueryRequest { Search = user.Id, SortBy = "actor" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<StageRatingDto>>>().Subject.Data!;
        rawActorRatings.TotalCount.Should().Be(0);
        typeof(WorkflowActionDto).GetProperty("ActorUserId").Should().BeNull();
        typeof(StageRatingDto).GetProperty("RatedByUserId").Should().BeNull();

        (await controller.HistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(), new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.RatingHistoryPage(SubmissionKind.Opms, submission.PublicId.ToString(), new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.History(SubmissionKind.Opms, submission.PublicId.ToString())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        (await controller.RatingHistory(SubmissionKind.Opms, submission.PublicId.ToString())).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static WorkflowConfigurationController Controller(
        ApplicationDbContext context,
        long municipalityId = 7,
        ApplicationUser? suppliedUser = null,
        Func<string, bool>? permissionPredicate = null)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(item => item.MunicipalityId).Returns(municipalityId);
        var user = suppliedUser ?? IdpTestFixture.CreateUser("workflow-admin");
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) => permissionPredicate?.Invoke(code) is not false
                ? new AccessDecisionResult(true, "Allowed", [], [], [])
                : new AccessDecisionResult(false, "Denied", [], [], []));
        return new WorkflowConfigurationController(
            context,
            tenant.Object,
            access.Object,
            Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(),
            new WorkflowGovernanceService(context),
            IdpTestFixture.CreateUserManagerMock(user).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };
    }

    private static WorkflowDefinition Definition(int version, long id, bool active, string stageName)
    {
        var year = new MunicipalityFinancialYear { Id = 500, PublicId = Guid.NewGuid(), MunicipalityId = 7, FinancialYearId = 1, EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        var definition = new WorkflowDefinition { Id = id, MunicipalityId = 7, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, SubmissionKind = SubmissionKind.Opms, Code = "DEFAULT", Name = "Default", Version = version, IsActive = active, EffectiveFrom = DateTime.UtcNow.AddDays(-10 + version) };
        definition.Stages.Add(new WorkflowStageDefinition { Id = id * 10, MunicipalityId = 7, Code = "SUBMIT", Name = stageName, Sequence = 1, RequiredActionCode = "OPMS_SUBMISSION.SUBMIT", RequiredPermissionCode = "OPMS_SUBMISSION.SUBMIT", IsTerminal = true });
        return definition;
    }
}

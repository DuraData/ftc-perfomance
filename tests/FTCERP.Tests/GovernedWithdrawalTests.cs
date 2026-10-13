using FTCERP.Host.Application.Services;
using System.Text.Json;

namespace FTCERP.Tests;

public sealed class GovernedWithdrawalTests
{
    [Fact]
    public async Task Due_date_extension_requires_exact_member_updates_and_row_version_and_derives_the_day_count()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "DUE-MEMBER", Name = "Due Date Municipality" };
        var user = IdpTestFixture.CreateUser("due-date-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var originalDueDate = DateTime.UtcNow.Date.AddDays(5);
        var opmsTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "DUE-OPMS",
            TargetName = "OPMS due date", KpiDescription = "OPMS due date", AnnualTargetDescription = "One"
        };
        var ipmsTarget = new IpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "DUE-IPMS",
            TargetName = "IPMS due date", KpiDescription = "IPMS due date", AnnualTargetDescription = "One"
        };
        var opmsSubmission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTarget = opmsTarget, Quarter = "Q1", DueDate = originalDueDate,
            BaseState = SubmissionBaseStates.InProgress, Status = "submitted", CreatedBy = user.Id
        };
        var ipmsSubmission = new IpmsSubmission
        {
            MunicipalityId = municipality.Id, IpmsTarget = ipmsTarget, Quarter = "Q1", DueDate = originalDueDate,
            BaseState = SubmissionBaseStates.InProgress, Status = "submitted", CreatedBy = user.Id
        };
        context.AddRange(opmsTarget, ipmsTarget, opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();

        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OPMS_SUBMISSION.EXTEND_DUE_DATE", "IPMS_SUBMISSION.EXTEND_DUE_DATE"
        };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(grants.Contains(code) && scope?.MunicipalityId == municipality.Id,
                    grants.Contains(code) ? "Allowed" : "Denied", [], [], []));
        access.Setup(service => service.GetEffectiveAccessAsync(user))
            .ReturnsAsync(() => new EffectiveAccessResult([], grants.ToArray(), [], [], [], []));
        var governance = new WorkflowGovernanceService(context,
            tenantContext: new TenantContext(municipality.Id, user.Id));
        var opms = SubmissionController(context, user, access.Object, governance);
        var ipms = IpmsSubmissionController(context, user, access.Object, governance);
        var opmsVersion = Convert.ToBase64String(opmsSubmission.RowVersion);
        var ipmsVersion = Convert.ToBase64String(ipmsSubmission.RowVersion);
        var requestedDate = originalDueDate.AddDays(5);

        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate, "Protected OPMS reason", opmsVersion, 999))).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        grants.Add("OPMS_SUBMISSION.DueDate.UPDATE");
        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate, "Protected OPMS reason", opmsVersion, 999))).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await context.DueDateExtensions.CountAsync()).Should().Be(0);

        grants.UnionWith([
            "OPMS_SUBMISSION.DueDateExtensionReason.UPDATE",
            "IPMS_SUBMISSION.DueDate.UPDATE",
            "IPMS_SUBMISSION.DueDateExtensionReason.UPDATE"
        ]);
        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate, "   ", opmsVersion))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(originalDueDate, "Not an extension", opmsVersion))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate, "Invalid version", "not-base64"))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        (await context.DueDateExtensions.CountAsync()).Should().Be(0);

        var allowedOpms = (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate, "  Protected OPMS reason  ", opmsVersion, 999))).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<OpmsSubmissionResponse>>().Subject.Data!;
        var allowedIpms = (await ipms.ExtendDueDate(ipmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate, "  Protected IPMS reason  ", ipmsVersion, 999))).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<IpmsSubmissionResponse>>().Subject.Data!;

        allowedOpms.DueDate.Should().BeNull();
        allowedIpms.DueDate.Should().BeNull();
        var extensions = await context.DueDateExtensions.OrderBy(item => item.SubmissionKind).ToArrayAsync();
        extensions.Should().HaveCount(2);
        extensions.Should().OnlyContain(item => item.OriginalDueDate == originalDueDate
            && item.ExtendedDueDate == requestedDate && item.ApprovedByUserId == user.Id
            && item.MunicipalityId == municipality.Id);
        extensions.Select(item => item.Reason).Should().BeEquivalentTo("Protected OPMS reason", "Protected IPMS reason");
        var audits = await context.AuditTrails.Where(item => item.Action == "ExtendDueDate").OrderBy(item => item.EntityName).ToArrayAsync();
        audits.Should().HaveCount(2);
        audits.Should().OnlyContain(item => item.MunicipalityId == municipality.Id && !string.IsNullOrWhiteSpace(item.Reason));
        (await context.Notifications.CountAsync(item => item.Type == NotificationType.DueDateExtension)).Should().Be(2);
        opmsSubmission.DueDateExtendedDays.Should().Be(5);
        ipmsSubmission.DueDateExtendedDays.Should().Be(5);

        grants.UnionWith(["OPMS_SUBMISSION.READ", "OPMS_SUBMISSION.DueDate.READ", "IPMS_SUBMISSION.READ", "IPMS_SUBMISSION.DueDate.READ"]);
        var visibleOpms = (await opms.GetSubmission(opmsSubmission.PublicId.ToString())).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<OpmsSubmissionResponse>>().Subject.Data!;
        var visibleIpms = (await ipms.GetSubmission(ipmsSubmission.PublicId.ToString())).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<IpmsSubmissionResponse>>().Subject.Data!;
        visibleOpms.DueDate.Should().Be(requestedDate);
        visibleOpms.DueDateExtendedDays.Should().Be(5);
        visibleIpms.DueDate.Should().Be(requestedDate);
        visibleIpms.DueDateExtendedDays.Should().Be(5);

        var beforeStaleAttempt = await context.DueDateExtensions.CountAsync();
        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(requestedDate.AddDays(3), "Stale extension", opmsVersion))).Result
            .Should().BeOfType<ConflictObjectResult>();
        (await context.DueDateExtensions.CountAsync()).Should().Be(beforeStaleAttempt);
    }

    [Fact]
    public async Task Due_date_extension_uses_canonical_workflow_and_audit_evidence_instead_of_legacy_status()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "DUE-CANONICAL", Name = "Canonical due-date municipality" };
        var user = IdpTestFixture.CreateUser("canonical-due-date-user");
        var financialYear = new FinancialYear
        {
            Code = "2039/40", Name = "2039/2040", StartDate = new(2039, 7, 1), EndDate = new(2040, 6, 30)
        };
        context.AddRange(municipality, user, financialYear);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, EffectiveFrom = financialYear.StartDate
        };
        var opmsTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "DUE-CANONICAL-OPMS",
            TargetName = "Canonical OPMS due date", KpiDescription = "Canonical OPMS due date", AnnualTargetDescription = "One"
        };
        var ipmsTarget = new IpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "DUE-CANONICAL-IPMS",
            TargetName = "Canonical IPMS due date", KpiDescription = "Canonical IPMS due date", AnnualTargetDescription = "One"
        };
        context.AddRange(municipalityYear, opmsTarget, ipmsTarget);
        await context.SaveChangesAsync();
        var dueDate = DateTime.UtcNow.Date.AddDays(5);
        var opmsSubmission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTargetId = opmsTarget.Id, Quarter = "Q1", DueDate = dueDate,
            BaseState = SubmissionBaseStates.Submitted, Status = "draft", CreatedBy = user.Id
        };
        var ipmsSubmission = new IpmsSubmission
        {
            MunicipalityId = municipality.Id, IpmsTargetId = ipmsTarget.Id, Quarter = "Q1", DueDate = dueDate,
            BaseState = SubmissionBaseStates.Submitted, Status = "draft", CreatedBy = user.Id
        };
        var opmsWorkflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id,
            SubmissionKind = SubmissionKind.Opms, Code = "DUE-CANONICAL-OPMS", Name = "Canonical OPMS workflow",
            EffectiveFrom = financialYear.StartDate
        };
        var ipmsWorkflow = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id,
            SubmissionKind = SubmissionKind.Ipms, Code = "DUE-CANONICAL-IPMS", Name = "Canonical IPMS workflow",
            EffectiveFrom = financialYear.StartDate
        };
        var auditConfiguration = new InternalAuditAssessmentConfiguration
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalityYear.Id,
            Model = InternalAuditAssessmentModel.Detailed, EffectiveFrom = financialYear.StartDate,
            Reason = "Approved canonical audit configuration", CreatedByUserId = user.Id
        };
        context.AddRange(opmsSubmission, ipmsSubmission, opmsWorkflow, ipmsWorkflow, auditConfiguration);
        await context.SaveChangesAsync();
        var opmsInstance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinitionId = opmsWorkflow.Id,
            SubmissionKind = SubmissionKind.Opms, SubmissionId = opmsSubmission.Id,
            State = WorkflowInstanceState.Completed, CompletedAt = DateTime.UtcNow
        };
        var ipmsInstance = new SubmissionWorkflowInstance
        {
            MunicipalityId = municipality.Id, WorkflowDefinitionId = ipmsWorkflow.Id,
            SubmissionKind = SubmissionKind.Ipms, SubmissionId = ipmsSubmission.Id,
            State = WorkflowInstanceState.Active
        };
        context.AddRange(opmsInstance, ipmsInstance);
        await context.SaveChangesAsync();
        context.Add(new InternalAuditAssessment
        {
            MunicipalityId = municipality.Id, SubmissionWorkflowInstanceId = ipmsInstance.Id,
            ConfigurationId = auditConfiguration.Id, Outcome = InternalAuditAssessmentOutcome.Achieved,
            DetailedObservation = "Internal Audit completed", AssessedByUserId = user.Id,
            CorrelationId = "canonical-due-date-audit"
        });
        await context.SaveChangesAsync();

        var access = Mock.Of<IAccessControlService>();
        var governance = new WorkflowGovernanceService(context,
            tenantContext: new TenantContext(municipality.Id, user.Id));
        var opms = SubmissionController(context, user, access, governance);
        var ipms = IpmsSubmissionController(context, user, access, governance);

        (await opms.ExtendDueDate(opmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(dueDate.AddDays(2), "Must remain closed", Convert.ToBase64String(opmsSubmission.RowVersion)))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        (await ipms.ExtendDueDate(ipmsSubmission.PublicId.ToString(),
                new DueDateExtensionRequest(dueDate.AddDays(2), "Must remain closed", Convert.ToBase64String(ipmsSubmission.RowVersion)))).Result
            .Should().BeOfType<BadRequestObjectResult>();
        (await context.DueDateExtensions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Due_date_extension_history_is_bounded_kind_scoped_and_masks_searches_sorts_and_fields_by_member_access()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "DUE-HISTORY", Name = "Due Date History Municipality" };
        var user = IdpTestFixture.CreateUser("due-history-user");
        user.FirstName = "History";
        user.LastName = "Approver";
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var target = new OpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "DUE-HISTORY-OPMS",
            TargetName = "Due history", KpiDescription = "Due history", AnnualTargetDescription = "One"
        };
        var submission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTarget = target, Quarter = "Q1",
            DueDate = DateTime.UtcNow.Date.AddDays(10), BaseState = SubmissionBaseStates.InProgress,
            Status = "submitted", CreatedBy = user.Id
        };
        var ipmsTarget = new IpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "DUE-HISTORY-IPMS",
            TargetName = "IPMS due history", KpiDescription = "IPMS due history", AnnualTargetDescription = "One"
        };
        var ipmsSubmission = new IpmsSubmission
        {
            MunicipalityId = municipality.Id, IpmsTarget = ipmsTarget, Quarter = "Q1",
            DueDate = DateTime.UtcNow.Date.AddDays(12), BaseState = SubmissionBaseStates.InProgress,
            Status = "submitted", CreatedBy = user.Id
        };
        context.AddRange(target, submission, ipmsTarget, ipmsSubmission);
        await context.SaveChangesAsync();
        var firstApprovedAt = DateTime.UtcNow.AddMinutes(-2);
        context.DueDateExtensions.AddRange(
            new DueDateExtension
            {
                MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id,
                OriginalDueDate = submission.DueDate!.Value, ExtendedDueDate = submission.DueDate.Value.AddDays(2),
                Reason = "Protected first reason", ApprovedByUserId = user.Id, ApprovedAt = firstApprovedAt
            },
            new DueDateExtension
            {
                MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms, SubmissionId = submission.Id,
                OriginalDueDate = submission.DueDate.Value.AddDays(2), ExtendedDueDate = submission.DueDate.Value.AddDays(4),
                Reason = "Protected second reason", ApprovedByUserId = user.Id, ApprovedAt = firstApprovedAt.AddMinutes(1)
            },
            new DueDateExtension
            {
                MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Ipms, SubmissionId = submission.Id,
                OriginalDueDate = submission.DueDate.Value, ExtendedDueDate = submission.DueDate.Value.AddDays(9),
                Reason = "Wrong submission kind", ApprovedByUserId = user.Id, ApprovedAt = firstApprovedAt.AddMinutes(2)
            },
            new DueDateExtension
            {
                MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Ipms, SubmissionId = ipmsSubmission.Id,
                OriginalDueDate = ipmsSubmission.DueDate!.Value, ExtendedDueDate = ipmsSubmission.DueDate.Value.AddDays(3),
                Reason = "Protected IPMS reason", ApprovedByUserId = user.Id, ApprovedAt = firstApprovedAt.AddMinutes(3)
            });
        QueueSubmissionLedgerAudits(context, user.Id);
        await context.SaveChangesAsync();

        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_SUBMISSION.READ" };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(grants.Contains(code) && scope?.MunicipalityId == municipality.Id,
                    grants.Contains(code) ? "Allowed" : "Denied", [], [], []));
        var controller = SubmissionController(context, user, access.Object, Mock.Of<IWorkflowGovernanceService>());

        var masked = (await controller.GetDueDateExtensionsPage(submission.PublicId.ToString(),
                new PagedQueryRequest { Page = 1, PageSize = 1 })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<DueDateExtensionHistoryResponse>>>().Subject.Data!;
        masked.TotalCount.Should().Be(2, "the IPMS history row must not cross the submission-kind boundary");
        masked.Items.Should().ContainSingle();
        masked.Items[0].OriginalDueDate.Should().BeNull();
        masked.Items[0].ExtendedDueDate.Should().BeNull();
        masked.Items[0].Reason.Should().BeNull();
        masked.Items[0].ApprovedByUserPublicId.Should().BeNull();
        masked.Items[0].ApprovedByName.Should().BeNull();

        var hiddenSearch = (await controller.GetDueDateExtensionsPage(submission.PublicId.ToString(),
                new PagedQueryRequest { Search = "Protected first" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<DueDateExtensionHistoryResponse>>>().Subject.Data!;
        hiddenSearch.TotalCount.Should().Be(0, "search must not infer protected reason or actor values");
        (await controller.GetDueDateExtensionsPage(submission.PublicId.ToString(),
                new PagedQueryRequest { SortBy = "reason" })).Result
            .Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        grants.Add("OPMS_SUBMISSION.DueDateExtensionReason.READ");
        var reasonVisible = (await controller.GetDueDateExtensionsPage(submission.PublicId.ToString(),
                new PagedQueryRequest { Search = "second", SortBy = "reason", SortDirection = "asc" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<DueDateExtensionHistoryResponse>>>().Subject.Data!;
        reasonVisible.Items.Should().ContainSingle().Which.Reason.Should().Be("Protected second reason");
        reasonVisible.Items[0].ApprovedByName.Should().BeNull();

        grants.UnionWith(["OPMS_SUBMISSION.DueDate.READ", "OPMS_SUBMISSION.DueDateExtensionActor.READ"]);
        var fullyVisible = (await controller.GetDueDateExtensionsPage(submission.PublicId.ToString(),
                new PagedQueryRequest { Search = "Approver", SortBy = "actor", SortDirection = "asc" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<DueDateExtensionHistoryResponse>>>().Subject.Data!;
        fullyVisible.TotalCount.Should().Be(2);
        fullyVisible.Items.Should().OnlyContain(item => item.OriginalDueDate.HasValue && item.ExtendedDueDate.HasValue
            && item.ApprovedByUserPublicId == user.PublicId && item.ApprovedByName == "History Approver");

        grants.UnionWith([
            "IPMS_SUBMISSION.READ", "IPMS_SUBMISSION.DueDate.READ",
            "IPMS_SUBMISSION.DueDateExtensionReason.READ", "IPMS_SUBMISSION.DueDateExtensionActor.READ"
        ]);
        var ipmsController = IpmsSubmissionController(context, user, access.Object, Mock.Of<IWorkflowGovernanceService>());
        var ipmsVisible = (await ipmsController.GetDueDateExtensionsPage(ipmsSubmission.PublicId.ToString(),
                new PagedQueryRequest { Search = "IPMS", SortBy = "extendedDueDate", SortDirection = "asc" })).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<DueDateExtensionHistoryResponse>>>().Subject.Data!;
        ipmsVisible.Items.Should().ContainSingle().Which.Reason.Should().Be("Protected IPMS reason");
        ipmsVisible.Items[0].ApprovedByUserPublicId.Should().Be(user.PublicId);
    }

    [Fact]
    public async Task Submission_withdrawal_requires_scoped_reason_update_and_masks_the_update_response()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WD-SUB", Name = "Submission Withdrawal Municipality" };
        var user = IdpTestFixture.CreateUser("submission-withdraw-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        var opmsTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "WD-OPMS",
            TargetName = "OPMS withdrawal", KpiDescription = "OPMS withdrawal", AnnualTargetDescription = "One"
        };
        var ipmsTarget = new IpmsTarget
        {
            MunicipalityId = municipality.Id, AssignedUserId = user.Id, IndicatorNumber = "WD-IPMS",
            TargetName = "IPMS withdrawal", KpiDescription = "IPMS withdrawal", AnnualTargetDescription = "One"
        };
        var opmsSubmission = new OpmsSubmission
        {
            MunicipalityId = municipality.Id, OpmsTarget = opmsTarget, Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress, Status = "draft", CreatedBy = user.Id
        };
        var ipmsSubmission = new IpmsSubmission
        {
            MunicipalityId = municipality.Id, IpmsTarget = ipmsTarget, Quarter = "Q1",
            BaseState = SubmissionBaseStates.InProgress, Status = "draft", CreatedBy = user.Id
        };
        context.AddRange(opmsTarget, ipmsTarget, opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();

        var grants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OPMS_SUBMISSION.WITHDRAW", "IPMS_SUBMISSION.WITHDRAW"
        };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(grants.Contains(code) && scope?.MunicipalityId == municipality.Id,
                    grants.Contains(code) ? "Allowed" : "Denied", [], [], []));
        access.Setup(service => service.GetEffectiveAccessAsync(user))
            .ReturnsAsync(() => new EffectiveAccessResult([], grants.ToArray(), [], [], [], []));
        var audit = new Mock<IWorkflowGovernanceService>();
        audit.Setup(service => service.WriteAuditTrailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var opms = SubmissionController(context, user, access.Object, audit.Object);
        var ipms = IpmsSubmissionController(context, user, access.Object, audit.Object);
        var opmsVersion = Convert.ToBase64String(opmsSubmission.RowVersion);
        var ipmsVersion = Convert.ToBase64String(ipmsSubmission.RowVersion);

        var deniedOpms = await opms.WithdrawSubmission(opmsSubmission.PublicId.ToString(),
            new WithdrawGovernedRecordRequest("Protected OPMS withdrawal reason", opmsVersion));
        deniedOpms.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var deniedIpms = await ipms.WithdrawSubmission(ipmsSubmission.PublicId.ToString(),
            new WithdrawGovernedRecordRequest("Protected IPMS withdrawal reason", ipmsVersion));
        deniedIpms.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        opmsSubmission.IsDisabled.Should().BeFalse();
        ipmsSubmission.IsDisabled.Should().BeFalse();
        context.GovernedRecordLifecycleEvents.Should().BeEmpty();

        grants.UnionWith(["OPMS_SUBMISSION.WithdrawalReason.UPDATE", "IPMS_SUBMISSION.WithdrawalReason.UPDATE"]);
        var allowedOpms = (await opms.WithdrawSubmission(opmsSubmission.PublicId.ToString(),
                new WithdrawGovernedRecordRequest("Protected OPMS withdrawal reason", opmsVersion))).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<OpmsSubmissionResponse>>().Subject.Data!;
        var allowedIpms = (await ipms.WithdrawSubmission(ipmsSubmission.PublicId.ToString(),
                new WithdrawGovernedRecordRequest("Protected IPMS withdrawal reason", ipmsVersion))).Result
            .Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<IpmsSubmissionResponse>>().Subject.Data!;

        allowedOpms.WithdrawalReason.Should().BeNull();
        allowedIpms.WithdrawalReason.Should().BeNull();
        (await context.OpmsSubmissions.SingleAsync()).WithdrawalReason.Should().Be("Protected OPMS withdrawal reason");
        (await context.IpmsSubmissions.SingleAsync()).WithdrawalReason.Should().Be("Protected IPMS withdrawal reason");
        (await context.GovernedRecordLifecycleEvents.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Opms_target_withdrawal_preserves_record_and_appends_immutable_tenant_event()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WD-A", Name = "Withdrawal Municipality" };
        var user = IdpTestFixture.CreateUser("withdraw-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var target = new OpmsTarget
        {
            Id = "opms-withdraw-1",
            MunicipalityId = municipality.Id,
            IndicatorNumber = "1.1",
            TargetName = "Retained target",
            KpiDescription = "Governed target",
            AnnualTargetDescription = "One"
        };
        context.OpmsTargets.Add(target);
        await context.SaveChangesAsync();
        var expectedVersion = Convert.ToBase64String(target.RowVersion);

        var access = AllowAccess("OPMS_KPI.WITHDRAW");
        var audit = new Mock<IWorkflowGovernanceService>();
        audit.Setup(service => service.WriteAuditTrailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            audit.Object,
            new TenantContext(municipality.Id, user.Id),
            new FTCERP.Host.Domain.Services.PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.WithdrawTarget(target.PublicId, new WithdrawGovernedRecordRequest("  Superseded by the approved plan  ", expectedVersion));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<OpmsTargetResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.True(payload.Data!.IsWithdrawn);
        Assert.Equal("Superseded by the approved plan", payload.Data.ReasonForWithdrawal);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.RowVersion));

        context.ChangeTracker.Clear();
        var retained = await context.OpmsTargets.SingleAsync(item => item.Id == target.Id);
        Assert.True(retained.IsWithdrawn);
        Assert.Equal(user.Id, retained.WithdrawnByUserId);
        var lifecycle = await context.GovernedRecordLifecycleEvents.SingleAsync();
        Assert.Equal(GovernedLifecycleAction.Withdrawn, lifecycle.Action);
        Assert.Equal(target.Id, lifecycle.AggregateId);
        Assert.Equal(municipality.Id, lifecycle.MunicipalityId);

        lifecycle.Reason = "Rewritten history";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        audit.Verify(service => service.WriteAuditTrailAsync(
            "OpmsTarget", target.Id, "Withdraw", It.IsAny<object?>(), It.IsAny<object?>(), user.Id, It.IsAny<string?>()), Times.Once);

        var delete = controller.DeleteTarget(target.Id);
        var gone = Assert.IsType<ObjectResult>(delete.Result);
        Assert.Equal(StatusCodes.Status410Gone, gone.StatusCode);
    }

    [Fact]
    public async Task Opms_target_withdrawal_rejects_stale_row_version_without_writing_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "WD-B", Name = "Concurrency Municipality" };
        var user = IdpTestFixture.CreateUser("concurrency-user");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        var target = new OpmsTarget
        {
            Id = "opms-withdraw-stale",
            MunicipalityId = municipality.Id,
            IndicatorNumber = "1.2",
            TargetName = "Concurrent target",
            KpiDescription = "Concurrency target",
            AnnualTargetDescription = "One"
        };
        context.Add(target);
        await context.SaveChangesAsync();

        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            AllowAccess("OPMS_KPI.WITHDRAW").Object,
            Mock.Of<IWorkflowGovernanceService>(),
            new TenantContext(municipality.Id, user.Id),
            new FTCERP.Host.Domain.Services.PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var staleVersion = Convert.ToBase64String(BitConverter.GetBytes(long.MaxValue));
        var result = await controller.WithdrawTarget(target.PublicId, new WithdrawGovernedRecordRequest("Stale request", staleVersion));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        context.ChangeTracker.Clear();
        Assert.False((await context.OpmsTargets.SingleAsync()).IsWithdrawn);
        Assert.Empty(await context.GovernedRecordLifecycleEvents.ToArrayAsync());
    }

    [Fact]
    public async Task Relational_constraints_require_withdrawal_metadata_and_filter_lifecycle_by_tenant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        var user = IdpTestFixture.CreateUser("tenant-lifecycle-user");

        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "LIFE-A", Name = "Lifecycle A" };
            var tenantB = new Municipality { Code = "LIFE-B", Name = "Lifecycle B" };
            setup.AddRange(tenantA, tenantB, user);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            tenantBId = tenantB.Id;

            setup.OpmsTargets.Add(new OpmsTarget
            {
                MunicipalityId = tenantAId,
                IndicatorNumber = "invalid",
                TargetName = "Missing metadata",
                KpiDescription = "Invalid withdrawn target",
                AnnualTargetDescription = "One",
                IsWithdrawn = true
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => setup.SaveChangesAsync());
            setup.ChangeTracker.Clear();

            setup.GovernedRecordLifecycleEvents.AddRange(
                Lifecycle(tenantAId, user.Id, "a"),
                Lifecycle(tenantBId, user.Id, "b"));
            await setup.SaveChangesAsync();
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TenantContext(tenantAId, user.Id));
        var visible = await tenantAContext.GovernedRecordLifecycleEvents.AsNoTracking().ToArrayAsync();
        Assert.Single(visible);
        Assert.Equal("a", visible[0].AggregateId);
    }

    private static GovernedRecordLifecycleEvent Lifecycle(long municipalityId, string userId, string aggregateId) => new()
    {
        MunicipalityId = municipalityId,
        AggregateType = "OpmsTarget",
        AggregateId = aggregateId,
        Action = GovernedLifecycleAction.Withdrawn,
        Reason = "Governed reason",
        ActorUserId = userId,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    private static Mock<IAccessControlService> AllowAccess(string code)
    {
        var mock = new Mock<IAccessControlService>();
        mock.Setup(service => service.CheckPermissionAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string requested, AccessScopeContext? _) =>
            {
                var allowed = requested == code || requested == "OPMS_KPI.WithdrawalReason.UPDATE"
                    || requested is "OPMS_KPI.WithdrawalReason.READ" or "OPMS_KPI.WithdrawalActor.READ"
                    || requested.StartsWith("OPMS_KPI.Period", StringComparison.OrdinalIgnoreCase);
                return new AccessDecisionResult(allowed, allowed ? "Allowed" : "Denied", allowed ? [requested] : [], [], []);
            });
        return mock;
    }

    private static OpmsSubmissionsController SubmissionController(
        ApplicationDbContext context, ApplicationUser user, IAccessControlService access, IWorkflowGovernanceService audit) =>
        new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access, audit,
            Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(), Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        {
            ControllerContext = ControllerContext(user.Id)
        };

    private static IpmsSubmissionsController IpmsSubmissionController(
        ApplicationDbContext context, ApplicationUser user, IAccessControlService access, IWorkflowGovernanceService audit) =>
        new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access, audit,
            Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(), Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        {
            ControllerContext = ControllerContext(user.Id)
        };

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = IdpTestFixture.CreatePrincipal(userId),
            TraceIdentifier = "withdrawal-test"
        }
    };

    private static void QueueSubmissionLedgerAudits(ApplicationDbContext context, string userId)
    {
        foreach (var entry in context.ChangeTracker.Entries<DueDateExtension>().Where(item => item.State == EntityState.Added).ToArray())
        {
            var extension = entry.Entity;
            context.AuditTrails.Add(new AuditTrail
            {
                MunicipalityId = extension.MunicipalityId,
                EntityName = extension.SubmissionKind == SubmissionKind.Opms ? nameof(OpmsSubmission) : nameof(IpmsSubmission),
                EntityId = extension.SubmissionId,
                Action = "ExtendDueDate",
                OldValue = JsonSerializer.Serialize(new { DueDate = extension.OriginalDueDate }),
                NewValue = JsonSerializer.Serialize(new { DueDate = extension.ExtendedDueDate, extension.Reason }),
                Reason = extension.Reason,
                ChangedBy = userId,
                ChangedAt = extension.ApprovedAt
            });
        }
    }

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => userId;
    }

    private sealed class SystemTenantContext : ITenantContext
    {
        public long? MunicipalityId => null;
        public bool IsSystem => true;
        public string? UserId => "system";
    }
}

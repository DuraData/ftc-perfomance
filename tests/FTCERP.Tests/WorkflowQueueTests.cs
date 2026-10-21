using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class WorkflowQueueTests
{
    [Fact]
    public async Task Completed_audit_transition_without_model_assessment_remains_recoverable_in_auditor_queue()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "QUEUE-RECOVERY", Name = "Workflow Queue Recovery Municipality" };
        var financialYear = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        var user = IdpTestFixture.CreateUser("queue-recovery-user", "Queue", "Auditor");
        user.Municipality = municipality;
        context.AddRange(municipality, financialYear, user);
        await context.SaveChangesAsync();

        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, FinancialYear = financialYear,
            EffectiveFrom = financialYear.StartDate, IsCurrent = true
        };
        var definition = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYear = municipalityYear, SubmissionKind = SubmissionKind.Opms,
            Code = "QUEUE-RECOVERY", Name = "Recoverable audit workflow", EffectiveFrom = financialYear.StartDate
        };
        definition.Stages.Add(new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id, Code = "INTERNAL_AUDIT", Name = "Internal audit", Sequence = 1,
            RequiredActionCode = "OPMS_WORKFLOW.INTERNAL_AUDIT", RequiredPermissionCode = "OPMS_WORKFLOW.INTERNAL_AUDIT",
            RequireDifferentActorFromSubmitter = false, RequireDifferentActorFromPreviousStage = false, IsTerminal = true
        });
        var period = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q3-RECOVERY", Name = "Quarter 3",
            StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(10)
        };
        var target = OpmsTarget("opms-audit-recovery", user.Id, municipality.Id);
        var submission = OpmsSubmission("audit-recovery", target.Id, "completed", SubmissionBaseStates.Submitted, user.Id, municipality.Id, DateTime.UtcNow);
        context.AddRange(municipalityYear, definition, period, target, submission);
        await context.SaveChangesAsync();
        var transition = await new ConfigurableWorkflowService(context).PrepareActionAsync(
            SubmissionKind.Opms, submission.Id, period.Id, user.Id, user.Id, "OPMS_WORKFLOW.INTERNAL_AUDIT",
            WorkflowActionOutcome.Approve, "Legacy audit transition", null, "queue-recovery-trace");
        Assert.True(transition.Allowed, transition.Reason);
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ")).ReturnsAsync(Scope([target.Id]));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ")).ReturnsAsync(new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        var controller = new WorkflowQueuesController(context, access.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) } }
        };

        var result = await controller.Get(new WorkflowQueueQueryRequest { Queue = "auditor" });
        var response = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        var item = Assert.Single(response.Page.Items);
        Assert.Equal(submission.PublicId, item.PublicId);
        Assert.Equal("reviewed", item.Status);
        Assert.Equal(1, response.Counts.Auditor);
    }

    [Fact]
    public async Task Combined_queue_counts_and_pages_are_filtered_by_dynamic_scope_before_database_paging()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "QUEUE", Name = "Workflow Queue Municipality" };
        var financialYear = new FinancialYear
        {
            Code = "2025/26", Name = "2025/26", StartDate = new(2025, 7, 1), EndDate = new(2026, 6, 30)
        };
        var user = IdpTestFixture.CreateUser("queue-user", "Queue", "Owner");
        var other = IdpTestFixture.CreateUser("other-user", "Other", "Submitter");
        user.Municipality = municipality;
        other.Municipality = municipality;
        context.AddRange(municipality, financialYear, user, other);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id,
            FinancialYearId = financialYear.Id,
            FinancialYear = financialYear,
            EffectiveFrom = financialYear.StartDate,
            IsCurrent = true
        };
        var opmsDefinition = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id,
            MunicipalityFinancialYear = municipalityYear,
            SubmissionKind = SubmissionKind.Opms,
            Code = "QUEUE-OPMS",
            Name = "OPMS queue workflow",
            EffectiveFrom = financialYear.StartDate
        };
        var verifyStage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id,
            Code = "VERIFY",
            Name = "Verify",
            Sequence = 1,
            RequiredActionCode = "OPMS_SUBMISSION.VERIFY",
            RequiredPermissionCode = "OPMS_SUBMISSION.VERIFY"
        };
        opmsDefinition.Stages.Add(verifyStage);
        var pmsStage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id,
            Code = "PMS_REVIEW",
            Name = "PMS review",
            Sequence = 2,
            RequiredActionCode = "OPMS_WORKFLOW.PMS_REVIEW",
            RequiredPermissionCode = "OPMS_WORKFLOW.PMS_REVIEW"
        };
        var auditStage = new WorkflowStageDefinition
        {
            MunicipalityId = municipality.Id,
            Code = "INTERNAL_AUDIT",
            Name = "Internal audit",
            Sequence = 3,
            RequiredActionCode = "OPMS_WORKFLOW.INTERNAL_AUDIT",
            RequiredPermissionCode = "OPMS_WORKFLOW.INTERNAL_AUDIT"
        };
        opmsDefinition.Stages.Add(pmsStage);
        opmsDefinition.Stages.Add(auditStage);
        var ipmsDefinition = new WorkflowDefinition
        {
            MunicipalityId = municipality.Id,
            MunicipalityFinancialYear = municipalityYear,
            SubmissionKind = SubmissionKind.Ipms,
            Code = "QUEUE-IPMS",
            Name = "IPMS queue workflow",
            EffectiveFrom = financialYear.StartDate
        };
        var reportingPeriod = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear,
            Code = "Q1",
            Name = "Quarter 1",
            PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1,
            StartDate = new DateTime(2025, 7, 1),
            EndDate = new DateTime(2025, 9, 30)
        };
        var opmsDraft = OpmsTarget("opms-draft", user.Id, municipality.Id);
        var opmsVerify = OpmsTarget("opms-verify", other.Id, municipality.Id);
        var opmsPms = OpmsTarget("opms-pms", user.Id, municipality.Id);
        var opmsAudit = OpmsTarget("opms-audit", user.Id, municipality.Id);
        var opmsReturned = OpmsTarget("opms-returned", user.Id, municipality.Id);
        var opmsOutside = OpmsTarget("opms-outside", other.Id, municipality.Id);
        var ipmsApproved = IpmsTarget("ipms-approved", user.Id, municipality.Id);
        var ipmsOutside = IpmsTarget("ipms-outside", other.Id, municipality.Id);
        context.AddRange(municipalityYear, opmsDefinition, ipmsDefinition, reportingPeriod, opmsDraft, opmsVerify, opmsPms, opmsAudit, opmsReturned, opmsOutside, ipmsApproved, ipmsOutside);

        var ownDraft = OpmsSubmission("own-draft", "opms-draft", "completed", SubmissionBaseStates.InProgress, user.Id, municipality.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var verify = OpmsSubmission("verify", "opms-verify", "draft", SubmissionBaseStates.Submitted, other.Id, municipality.Id, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var pms = OpmsSubmission("pms", "opms-pms", "draft", SubmissionBaseStates.Submitted, user.Id, municipality.Id, new DateTime(2026, 1, 2, 1, 0, 0, DateTimeKind.Utc));
        var audit = OpmsSubmission("audit", "opms-audit", "draft", SubmissionBaseStates.Submitted, user.Id, municipality.Id, new DateTime(2026, 1, 2, 2, 0, 0, DateTimeKind.Utc));
        var returned = OpmsSubmission("returned", "opms-returned", "completed", SubmissionBaseStates.Submitted, user.Id, municipality.Id, new DateTime(2026, 1, 2, 3, 0, 0, DateTimeKind.Utc));
        var outsideVerify = OpmsSubmission("outside-verify", "opms-outside", "completed", SubmissionBaseStates.Submitted, other.Id, municipality.Id, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        var ownApproved = IpmsSubmission("own-approved", "ipms-approved", "draft", SubmissionBaseStates.Submitted, user.Id, municipality.Id, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));
        var outsideApproved = IpmsSubmission("outside-approved", "ipms-outside", "draft", SubmissionBaseStates.Submitted, other.Id, municipality.Id, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        foreach (var submission in new[] { ownDraft, verify, pms, audit, returned, outsideVerify }) submission.ReportingPeriod = reportingPeriod;
        foreach (var submission in new[] { ownApproved, outsideApproved }) submission.ReportingPeriod = reportingPeriod;
        context.OpmsSubmissions.AddRange(
            ownDraft, verify, pms, audit, returned, outsideVerify);
        context.IpmsSubmissions.AddRange(ownApproved, outsideApproved);
        await context.SaveChangesAsync();
        context.SubmissionWorkflowInstances.AddRange(
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id,
                WorkflowDefinitionId = opmsDefinition.Id,
                CurrentStageId = verifyStage.Id,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = verify.Id,
                State = WorkflowInstanceState.Active
            },
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id,
                WorkflowDefinitionId = opmsDefinition.Id,
                CurrentStageId = pmsStage.Id,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = pms.Id,
                State = WorkflowInstanceState.Active
            },
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id,
                WorkflowDefinitionId = opmsDefinition.Id,
                CurrentStageId = auditStage.Id,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = audit.Id,
                State = WorkflowInstanceState.Active
            },
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id,
                WorkflowDefinitionId = opmsDefinition.Id,
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = returned.Id,
                State = WorkflowInstanceState.Rework
            },
            new SubmissionWorkflowInstance
            {
                MunicipalityId = municipality.Id,
                WorkflowDefinitionId = ipmsDefinition.Id,
                SubmissionKind = SubmissionKind.Ipms,
                SubmissionId = ownApproved.Id,
                State = WorkflowInstanceState.Completed,
                CompletedAt = new DateTime(2026, 1, 4, 1, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();
        var verificationPublicId = await context.OpmsSubmissions.Where(item => item.Id == "verify").Select(item => item.PublicId).SingleAsync();
        var approvedPublicId = await context.IpmsSubmissions.Where(item => item.Id == "own-approved").Select(item => item.PublicId).SingleAsync();
        var outsideApprovedPublicId = await context.IpmsSubmissions.Where(item => item.Id == "outside-approved").Select(item => item.PublicId).SingleAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"))
            .ReturnsAsync(Scope(["opms-draft", "opms-verify", "opms-pms", "opms-audit", "opms-returned"]));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_SUBMISSION.READ"))
            .ReturnsAsync(Scope(["ipms-approved"]));
        var memberPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext>()))
            .ReturnsAsync((ApplicationUser _, string permission, AccessScopeContext? _) =>
                new AccessDecisionResult(memberPermissions.Contains(permission), memberPermissions.Contains(permission) ? "Allowed" : "Denied", [], [], []));
        var controller = new WorkflowQueuesController(context, access.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };

        var verificationResult = await controller.Get(new WorkflowQueueQueryRequest { Queue = "verification", Page = 1, PageSize = 1 });
        var verification = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(verificationResult.Result).Value).Data!;

        Assert.Equal(1, verification.Page.TotalCount);
        Assert.Equal(1, verification.Page.TotalPages);
        var verificationItem = Assert.Single(verification.Page.Items);
        Assert.Equal(verificationPublicId, verificationItem.PublicId);
        Assert.Equal("opms", verificationItem.Kind);
        Assert.Equal("pending_verification", verificationItem.Status);
        Assert.Null(verificationItem.SubmittedByUserPublicId);
        Assert.Null(verificationItem.SubmittedByName);
        Assert.Equal(5, verification.Counts.MySubmissions);
        Assert.Equal(1, verification.Counts.Verification);
        Assert.Equal(1, verification.Counts.Pms);
        Assert.Equal(1, verification.Counts.Auditor);
        Assert.Equal(1, verification.Counts.Returned);
        Assert.Equal(1, verification.Counts.MyDrafts);
        Assert.Equal(2, verification.Counts.ApprovedClosed);

        memberPermissions.Add("OPMS_SUBMISSION.SubmitterIdentity.READ");
        var visibleVerificationResult = await controller.Get(new WorkflowQueueQueryRequest { Queue = "verification", Page = 1, PageSize = 1 });
        var visibleVerification = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(visibleVerificationResult.Result).Value).Data!;
        Assert.Equal(other.PublicId, Assert.Single(visibleVerification.Page.Items).SubmittedByUserPublicId);
        Assert.Equal(other.FullName, Assert.Single(visibleVerification.Page.Items).SubmittedByName);

        memberPermissions.Add("IPMS_SUBMISSION.SubmitterIdentity.READ");
        var approvedResult = await controller.Get(new WorkflowQueueQueryRequest { Queue = "approved-closed" });
        var approved = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(approvedResult.Result).Value).Data!;
        Assert.Equal(2, approved.Page.TotalCount);
        var approvedItem = Assert.Single(approved.Page.Items, item => item.PublicId == approvedPublicId);
        Assert.Equal(approvedPublicId, approvedItem.PublicId);
        Assert.Equal("ipms", approvedItem.Kind);
        Assert.Equal("completed", approvedItem.Status);
        Assert.Equal(user.PublicId, approvedItem.SubmittedByUserPublicId);
        Assert.DoesNotContain(approved.Page.Items, item => item.PublicId == outsideApprovedPublicId);

        var filteredResult = await controller.Get(new WorkflowQueueQueryRequest
        {
            Queue = "my-submissions",
            Search = "ipms-approved",
            MunicipalityFinancialYearPublicId = municipalityYear.PublicId,
            Kind = "ipms",
            Period = "Q1",
            Status = "completed",
            SortBy = "targetName",
            SortDirection = "asc"
        });
        var filtered = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(filteredResult.Result).Value).Data!;
        var filteredItem = Assert.Single(filtered.Page.Items);
        Assert.Equal(approvedPublicId, filteredItem.PublicId);
        Assert.Equal(1, filtered.Page.TotalCount);
        Assert.Equal(5, filtered.Counts.MySubmissions);

        var protectedActorSearchResult = await controller.Get(new WorkflowQueueQueryRequest
        {
            Queue = "verification",
            Search = other.FullName
        });
        var protectedActorSearch = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(protectedActorSearchResult.Result).Value).Data!;
        Assert.Empty(protectedActorSearch.Page.Items);
        Assert.Equal(0, protectedActorSearch.Page.TotalCount);

        await AssertQueueStatus(controller, "pms", "approved");
        await AssertQueueStatus(controller, "auditor", "reviewed");
        await AssertQueueStatus(controller, "returned", "returned_for_info");
    }

    private static async Task AssertQueueStatus(WorkflowQueuesController controller, string queue, string expectedStatus)
    {
        var result = await controller.Get(new WorkflowQueueQueryRequest { Queue = queue });
        var response = Assert.IsType<ApiResponse<WorkflowQueueResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(expectedStatus, Assert.Single(response.Page.Items).Status);
    }

    private static AccessQueryScopeResult Scope(string[] targetIds) =>
        new(true, false, [], [], [], targetIds, [], []);

    private static OpmsTarget OpmsTarget(string id, string ownerId, long municipalityId) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        AssignedUserId = ownerId,
        IndicatorNumber = id,
        TargetName = id,
        KpiDescription = id,
        AnnualTargetDescription = id
    };

    private static IpmsTarget IpmsTarget(string id, string ownerId, long municipalityId) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        AssignedUserId = ownerId,
        IndicatorNumber = id,
        TargetName = id,
        KpiDescription = id,
        AnnualTargetDescription = id
    };

    private static OpmsSubmission OpmsSubmission(string id, string targetId, string status, string baseState, string submitterId, long municipalityId, DateTime createdAt) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        OpmsTargetId = targetId,
        Quarter = "Q1",
        Status = status,
        BaseState = baseState,
        SubmittedByUserId = submitterId,
        CreatedAt = createdAt
    };

    private static IpmsSubmission IpmsSubmission(string id, string targetId, string status, string baseState, string submitterId, long municipalityId, DateTime createdAt) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        IpmsTargetId = targetId,
        Quarter = "Q1",
        Status = status,
        BaseState = baseState,
        SubmittedByUserId = submitterId,
        CreatedAt = createdAt
    };
}

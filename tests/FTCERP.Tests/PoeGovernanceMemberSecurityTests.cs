using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Tests;

public sealed class PoeGovernanceMemberSecurityTests
{
    [Fact]
    public async Task Direct_poe_governance_writes_require_dynamic_member_update_grants_for_both_registers()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "POE", Name = "POE Security Municipality" };
        var user = IdpTestFixture.CreateUser("poe-member-writer", "POE", "Writer");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var opmsTarget = new OpmsTarget
        {
            Id = "poe-opms-target", MunicipalityId = municipality.Id, IndicatorNumber = "POE-OPMS",
            TargetName = "OPMS evidence", KpiDescription = "OPMS evidence security"
        };
        var ipmsTarget = new IpmsTarget
        {
            Id = "poe-ipms-target", MunicipalityId = municipality.Id, IndicatorNumber = "POE-IPMS",
            TargetName = "IPMS evidence", KpiDescription = "IPMS evidence security"
        };
        var opmsSubmission = new OpmsSubmission
        {
            Id = "poe-opms-submission", MunicipalityId = municipality.Id, OpmsTargetId = opmsTarget.Id,
            OpmsTarget = opmsTarget, Quarter = "Q1", Status = SubmissionBaseStates.InProgress
        };
        var ipmsSubmission = new IpmsSubmission
        {
            Id = "poe-ipms-submission", MunicipalityId = municipality.Id, IpmsTargetId = ipmsTarget.Id,
            IpmsTarget = ipmsTarget, Quarter = "Q1", Status = SubmissionBaseStates.InProgress
        };
        context.AddRange(opmsTarget, ipmsTarget, opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OPMS_POE.ASSESS", "OPMS_POE.REPLACE", "OPMS_POE.PLACE_HOLD", "OPMS_POE.RELEASE_HOLD", "OPMS_POE.DISPOSE",
            "IPMS_POE.ASSESS", "IPMS_POE.REPLACE", "IPMS_POE.PLACE_HOLD", "IPMS_POE.RELEASE_HOLD", "IPMS_POE.DISPOSE"
        };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
            {
                var granted = allowed.Contains(code) && scope?.MunicipalityId == municipality.Id;
                return new AccessDecisionResult(granted, granted ? "Allowed" : $"Missing {code}", [], [], []);
            });

        var opms = CreateOpmsController(context, user, access.Object);
        var ipms = CreateIpmsController(context, user, access.Object);
        var attachmentId = Guid.NewGuid();
        var holdId = Guid.NewGuid();

        AssertForbidden(await opms.AssessAttachment(opmsSubmission.PublicId.ToString(), attachmentId,
            new AssessPoeRequest(PoeAssessmentOutcome.Accepted, "Protected assessment comment")), "AssessmentComment.UPDATE");
        AssertNotFound(await opms.AssessAttachment(opmsSubmission.PublicId.ToString(), attachmentId,
            new AssessPoeRequest(PoeAssessmentOutcome.Accepted, null)));
        AssertForbidden(await opms.ReplaceAttachment(opmsSubmission.PublicId.ToString(), attachmentId,
            new ReplacePoeRequest(Guid.NewGuid(), "Protected replacement reason", "AAAAAAAAAAA=", "AAAAAAAAAAA=")), "ReplacementReason.UPDATE");
        AssertForbidden(await opms.PlaceLegalHold(opmsSubmission.PublicId.ToString(), attachmentId,
            new PlacePoeLegalHoldRequest("CASE-2026-SECURITY", "Protected investigation reason")), "LegalHoldReference.UPDATE");
        AssertForbidden(await opms.ReleaseLegalHold(opmsSubmission.PublicId.ToString(), attachmentId, holdId,
            new ReleasePoeLegalHoldRequest("Protected release reason")), "LegalHoldReason.UPDATE");
        AssertForbidden(await opms.RequestDisposal(opmsSubmission.PublicId.ToString(), attachmentId,
            new RequestPoeDisposalRequest("COUNCIL-SECURITY", "Protected disposal reason", "AAAAAAAAAAA=")), "DisposalApprovalReference.UPDATE");

        AssertForbidden(await ipms.AssessAttachment(ipmsSubmission.PublicId.ToString(), attachmentId,
            new AssessPoeRequest(PoeAssessmentOutcome.Accepted, "Protected assessment comment")), "AssessmentComment.UPDATE");
        AssertNotFound(await ipms.AssessAttachment(ipmsSubmission.PublicId.ToString(), attachmentId,
            new AssessPoeRequest(PoeAssessmentOutcome.Accepted, null)));
        AssertForbidden(await ipms.ReplaceAttachment(ipmsSubmission.PublicId.ToString(), attachmentId,
            new ReplacePoeRequest(Guid.NewGuid(), "Protected replacement reason", "AAAAAAAAAAA=", "AAAAAAAAAAA=")), "ReplacementReason.UPDATE");
        AssertForbidden(await ipms.PlaceLegalHold(ipmsSubmission.PublicId.ToString(), attachmentId,
            new PlacePoeLegalHoldRequest("CASE-2026-SECURITY", "Protected investigation reason")), "LegalHoldReference.UPDATE");
        AssertForbidden(await ipms.ReleaseLegalHold(ipmsSubmission.PublicId.ToString(), attachmentId, holdId,
            new ReleasePoeLegalHoldRequest("Protected release reason")), "LegalHoldReason.UPDATE");
        AssertForbidden(await ipms.RequestDisposal(ipmsSubmission.PublicId.ToString(), attachmentId,
            new RequestPoeDisposalRequest("COUNCIL-SECURITY", "Protected disposal reason", "AAAAAAAAAAA=")), "DisposalApprovalReference.UPDATE");
        Assert.Empty(await context.PoeLegalHoldEvents.ToArrayAsync());
        Assert.Empty(await context.PoeDisposalEvents.ToArrayAsync());
        Assert.Empty(await context.PoeEvidenceAssessments.ToArrayAsync());
        Assert.Empty(await context.PoeEvidenceReplacements.ToArrayAsync());

        foreach (var resource in new[] { "OPMS_POE", "IPMS_POE" })
        foreach (var member in new[] { "AssessmentComment", "ReplacementReason", "LegalHoldReference", "LegalHoldReason", "DisposalApprovalReference", "DisposalReason" })
            allowed.Add($"{resource}.{member}.UPDATE");

        AssertNotFound(await opms.AssessAttachment(opmsSubmission.PublicId.ToString(), attachmentId,
            new AssessPoeRequest(PoeAssessmentOutcome.Accepted, "Protected assessment comment")));
        AssertNotFound(await opms.ReplaceAttachment(opmsSubmission.PublicId.ToString(), attachmentId,
            new ReplacePoeRequest(Guid.NewGuid(), "Protected replacement reason", "AAAAAAAAAAA=", "AAAAAAAAAAA=")));
        AssertNotFound(await opms.PlaceLegalHold(opmsSubmission.PublicId.ToString(), attachmentId,
            new PlacePoeLegalHoldRequest("CASE-2026-SECURITY", "Protected investigation reason")));
        AssertNotFound(await opms.ReleaseLegalHold(opmsSubmission.PublicId.ToString(), attachmentId, holdId,
            new ReleasePoeLegalHoldRequest("Protected release reason")));
        AssertNotFound(await opms.RequestDisposal(opmsSubmission.PublicId.ToString(), attachmentId,
            new RequestPoeDisposalRequest("COUNCIL-SECURITY", "Protected disposal reason", "AAAAAAAAAAA=")));
        AssertNotFound(await ipms.AssessAttachment(ipmsSubmission.PublicId.ToString(), attachmentId,
            new AssessPoeRequest(PoeAssessmentOutcome.Accepted, "Protected assessment comment")));
        AssertNotFound(await ipms.ReplaceAttachment(ipmsSubmission.PublicId.ToString(), attachmentId,
            new ReplacePoeRequest(Guid.NewGuid(), "Protected replacement reason", "AAAAAAAAAAA=", "AAAAAAAAAAA=")));
        AssertNotFound(await ipms.PlaceLegalHold(ipmsSubmission.PublicId.ToString(), attachmentId,
            new PlacePoeLegalHoldRequest("CASE-2026-SECURITY", "Protected investigation reason")));
        AssertNotFound(await ipms.ReleaseLegalHold(ipmsSubmission.PublicId.ToString(), attachmentId, holdId,
            new ReleasePoeLegalHoldRequest("Protected release reason")));
        AssertNotFound(await ipms.RequestDisposal(ipmsSubmission.PublicId.ToString(), attachmentId,
            new RequestPoeDisposalRequest("COUNCIL-SECURITY", "Protected disposal reason", "AAAAAAAAAAA=")));
    }

    private static OpmsSubmissionsController CreateOpmsController(
        ApplicationDbContext context, ApplicationUser user, IAccessControlService access) => new(
        context, IdpTestFixture.CreateUserManagerMock(user).Object, access, Mock.Of<IWorkflowGovernanceService>(),
        Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(), Mock.Of<IConfigurableWorkflowService>(),
        Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
        Mock.Of<IPerformanceSuggestionService>()) { ControllerContext = ControllerContext(user.Id) };

    private static IpmsSubmissionsController CreateIpmsController(
        ApplicationDbContext context, ApplicationUser user, IAccessControlService access) => new(
        context, IdpTestFixture.CreateUserManagerMock(user).Object, access, Mock.Of<IWorkflowGovernanceService>(),
        Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(), Mock.Of<IConfigurableWorkflowService>(),
        Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
        Mock.Of<IPerformanceSuggestionService>()) { ControllerContext = ControllerContext(user.Id) };

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(userId) }
    };

    private static void AssertForbidden(ActionResult<ApiResponse<PoeFileResponse>> result, string missingCode)
    {
        var denied = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, denied.StatusCode);
        Assert.Contains(missingCode, Assert.IsType<ApiResponse<PoeFileResponse>>(denied.Value).Message);
    }

    private static void AssertNotFound(ActionResult<ApiResponse<PoeFileResponse>> result)
        => Assert.IsType<NotFoundObjectResult>(result.Result);
}

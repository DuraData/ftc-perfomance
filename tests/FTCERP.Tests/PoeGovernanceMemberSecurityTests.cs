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
    public async Task Poe_replacement_accepts_provider_neutral_sixteen_byte_row_versions_for_both_registers()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "POE-RV", Name = "POE RowVersion Municipality" };
        var user = IdpTestFixture.CreateUser("poe-rowversion-writer", "POE", "Writer");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var opmsTarget = new OpmsTarget
        {
            Id = "poe-rv-opms-target", MunicipalityId = municipality.Id,
            IndicatorNumber = "POE-RV-OPMS", TargetName = "OPMS replacement", KpiDescription = "OPMS replacement"
        };
        var ipmsTarget = new IpmsTarget
        {
            Id = "poe-rv-ipms-target", MunicipalityId = municipality.Id,
            IndicatorNumber = "POE-RV-IPMS", TargetName = "IPMS replacement", KpiDescription = "IPMS replacement"
        };
        var opmsSubmission = new OpmsSubmission
        {
            Id = "poe-rv-opms-submission", MunicipalityId = municipality.Id,
            OpmsTargetId = opmsTarget.Id, OpmsTarget = opmsTarget, Quarter = "Q1", Status = SubmissionBaseStates.InProgress
        };
        var ipmsSubmission = new IpmsSubmission
        {
            Id = "poe-rv-ipms-submission", MunicipalityId = municipality.Id,
            IpmsTargetId = ipmsTarget.Id, IpmsTarget = ipmsTarget, Quarter = "Q1", Status = SubmissionBaseStates.InProgress
        };
        context.AddRange(opmsTarget, ipmsTarget, opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();

        var files = new[]
        {
            NewEvidence("poe-rv-opms-old", opmsSubmission.Id, SubmissionKind.Opms, "opms-old.pdf", municipality.Id, user.Id),
            NewEvidence("poe-rv-opms-new", opmsSubmission.Id, SubmissionKind.Opms, "opms-new.pdf", municipality.Id, user.Id),
            NewEvidence("poe-rv-ipms-old", ipmsSubmission.Id, SubmissionKind.Ipms, "ipms-old.pdf", municipality.Id, user.Id),
            NewEvidence("poe-rv-ipms-new", ipmsSubmission.Id, SubmissionKind.Ipms, "ipms-new.pdf", municipality.Id, user.Id)
        };
        context.PoeFiles.AddRange(files);
        await context.SaveChangesAsync();
        files.Should().OnlyContain(file => file.RowVersion.Length == 16);

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));

        var opmsResult = await CreateOpmsController(context, user, access.Object).ReplaceAttachment(
            opmsSubmission.PublicId.ToString(), files[0].PublicId,
            new ReplacePoeRequest(files[1].PublicId, "Governed OPMS evidence replacement",
                Convert.ToBase64String(files[0].RowVersion), Convert.ToBase64String(files[1].RowVersion)));
        Assert.IsType<OkObjectResult>(opmsResult.Result);

        var ipmsResult = await CreateIpmsController(context, user, access.Object).ReplaceAttachment(
            ipmsSubmission.PublicId.ToString(), files[2].PublicId,
            new ReplacePoeRequest(files[3].PublicId, "Governed IPMS evidence replacement",
                Convert.ToBase64String(files[2].RowVersion), Convert.ToBase64String(files[3].RowVersion)));
        Assert.IsType<OkObjectResult>(ipmsResult.Result);

        Assert.Equal(2, await context.PoeEvidenceReplacements.CountAsync());
        Assert.False((await context.PoeFiles.FindAsync(files[0].Id))!.IsActive);
        Assert.False((await context.PoeFiles.FindAsync(files[2].Id))!.IsActive);
    }

    [Fact]
    public async Task Direct_poe_download_requires_the_dedicated_evidence_read_permission()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "POE-DOWNLOAD", Name = "POE Download Municipality" };
        var user = IdpTestFixture.CreateUser("poe-download-reader", "POE", "Reader");
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var opmsTarget = new OpmsTarget
        {
            Id = "poe-download-opms-target", MunicipalityId = municipality.Id,
            IndicatorNumber = "POE-DOWNLOAD-OPMS", TargetName = "OPMS evidence", KpiDescription = "OPMS evidence"
        };
        var ipmsTarget = new IpmsTarget
        {
            Id = "poe-download-ipms-target", MunicipalityId = municipality.Id,
            IndicatorNumber = "POE-DOWNLOAD-IPMS", TargetName = "IPMS evidence", KpiDescription = "IPMS evidence"
        };
        var opmsSubmission = new OpmsSubmission
        {
            Id = "poe-download-opms-submission", MunicipalityId = municipality.Id,
            OpmsTargetId = opmsTarget.Id, OpmsTarget = opmsTarget, Quarter = "Q1", Status = SubmissionBaseStates.InProgress
        };
        var ipmsSubmission = new IpmsSubmission
        {
            Id = "poe-download-ipms-submission", MunicipalityId = municipality.Id,
            IpmsTargetId = ipmsTarget.Id, IpmsTarget = ipmsTarget, Quarter = "Q1", Status = SubmissionBaseStates.InProgress
        };
        context.AddRange(opmsTarget, ipmsTarget, opmsSubmission, ipmsSubmission);
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
            {
                var granted = code is "OPMS_SUBMISSION.READ" or "IPMS_SUBMISSION.READ"
                    && scope?.MunicipalityId == municipality.Id;
                return new AccessDecisionResult(granted, granted ? "Allowed" : $"Missing {code}", [], [], []);
            });

        var opmsResult = await CreateOpmsController(context, user, access.Object)
            .DownloadAttachment(opmsSubmission.PublicId.ToString(), Guid.NewGuid());
        var ipmsResult = await CreateIpmsController(context, user, access.Object)
            .DownloadAttachment(ipmsSubmission.PublicId.ToString(), Guid.NewGuid());

        Assert.IsType<ForbidResult>(opmsResult);
        Assert.IsType<ForbidResult>(ipmsResult);
        access.Verify(service => service.CheckPermissionAsync(user, "OPMS_POE.READ", It.IsAny<AccessScopeContext?>()), Times.Once);
        access.Verify(service => service.CheckPermissionAsync(user, "IPMS_POE.READ", It.IsAny<AccessScopeContext?>()), Times.Once);
    }

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

    private static PoeFile NewEvidence(string id, string submissionId, SubmissionKind kind, string fileName, long municipalityId, string userId) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        SubmissionKind = kind,
        SubmissionId = submissionId,
        FileName = fileName,
        UploadedByUserId = userId,
        RetainUntil = DateTime.UtcNow.AddYears(7),
        Blob = new EvidenceBlob
        {
            Id = id + "-blob",
            MunicipalityId = municipalityId,
            StorageKey = "poe/tests/" + fileName,
            ContentType = "application/pdf",
            SizeInBytes = 128,
            Sha256 = new string('a', 64),
            SignatureVerified = true,
            ScanStatus = "Clean",
            IsQuarantined = false
        }
    };
}

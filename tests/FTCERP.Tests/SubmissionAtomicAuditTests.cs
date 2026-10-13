using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Services;

namespace FTCERP.Tests;

public sealed class SubmissionAtomicAuditTests
{
    [Fact]
    public async Task Opms_and_ipms_create_and_edit_persist_distinct_atomic_audit_snapshots()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("atomic-submission-user");
        var municipality = new Municipality { Code = "ATOMIC-SUB", Name = "Atomic Submission Municipality" };
        var financialYear = new FinancialYear
        {
            Code = "2036/37", Name = "2036/2037", StartDate = new(2036, 7, 1), EndDate = new(2037, 6, 30)
        };
        var municipalityYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, EffectiveFrom = financialYear.StartDate
        };
        var period = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q1-ATOMIC", Name = "Atomic quarter one",
            PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = financialYear.StartDate, EndDate = new(2036, 9, 30)
        };
        var opmsTarget = new OpmsTarget
        {
            Municipality = municipality, AssignedUser = user, IndicatorNumber = "ATOMIC-OPMS",
            TargetName = "Atomic OPMS target", KpiDescription = "Atomic OPMS target"
        };
        var ipmsTarget = new IpmsTarget
        {
            Municipality = municipality, AssignedUser = user, IndicatorNumber = "ATOMIC-IPMS",
            TargetName = "Atomic IPMS target", KpiDescription = "Atomic IPMS target"
        };
        context.AddRange(user, municipality, financialYear, municipalityYear, period, opmsTarget, ipmsTarget);
        await context.SaveChangesAsync();
        user.MunicipalityId = municipality.Id;
        context.PerformancePeriodTargets.AddRange(
            new PerformancePeriodTarget
            {
                MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, OpmsTargetId = opmsTarget.Id,
                UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
                TargetValue = "10", CreatedByUserId = user.Id
            },
            new PerformancePeriodTarget
            {
                MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, IpmsTargetId = ipmsTarget.Id,
                UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
                TargetValue = "10", CreatedByUserId = user.Id
            });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        access.Setup(service => service.GetEffectiveAccessAsync(user))
            .ReturnsAsync(new EffectiveAccessResult([], [
                "OPMS_SUBMISSION.ActualPerformance.UPDATE", "OPMS_SUBMISSION.VarianceReason.UPDATE", "OPMS_SUBMISSION.CorrectiveMeasure.UPDATE",
                "IPMS_SUBMISSION.ActualPerformance.UPDATE", "IPMS_SUBMISSION.VarianceReason.UPDATE", "IPMS_SUBMISSION.CorrectiveMeasure.UPDATE"
            ], [], [], [], []));
        var governance = new WorkflowGovernanceService(context,
            tenantContext: new TenantContext(municipality.Id, user.Id));
        var values = new SubmissionValueService(context, new PerformanceUnitEngine());
        var opms = OpmsController(context, user, access.Object, governance, values);
        var ipms = IpmsController(context, user, access.Object, governance, values);

        var createdOpms = Data(await opms.CreateSubmission(new SaveOpmsSubmissionRequest(
            opmsTarget.PublicId, period.PublicId, "12", null, "Initial OPMS reason", "Initial OPMS measure", "Document")));
        var createdIpms = Data(await ipms.CreateSubmission(new SaveIpmsSubmissionRequest(
            ipmsTarget.PublicId, period.PublicId, "13", null, "Initial IPMS reason", "Initial IPMS measure", "Document")));

        Data(await opms.UpdateSubmission(createdOpms.PublicId.ToString(), new SaveOpmsSubmissionRequest(
            opmsTarget.PublicId, period.PublicId, "14", null, "Updated OPMS reason", "Updated OPMS measure", "Register")));
        Data(await ipms.UpdateSubmission(createdIpms.PublicId.ToString(), new SaveIpmsSubmissionRequest(
            ipmsTarget.PublicId, period.PublicId, "15", null, "Updated IPMS reason", "Updated IPMS measure", "Register")));

        var audits = await context.AuditTrails
            .Where(item => item.EntityName == nameof(OpmsSubmission) || item.EntityName == nameof(IpmsSubmission))
            .OrderBy(item => item.EntityName).ThenBy(item => item.ChangedAt).ToArrayAsync();
        audits.Should().HaveCount(4);
        audits.Count(item => item.Action == "Create").Should().Be(2);
        audits.Count(item => item.Action == "Edit").Should().Be(2);
        audits.Should().OnlyContain(item => item.MunicipalityId == municipality.Id && !string.IsNullOrWhiteSpace(item.Reason));
        var opmsEdit = audits.Single(item => item.EntityName == nameof(OpmsSubmission) && item.Action == "Edit");
        opmsEdit.OldValue.Should().Contain("\"ActualPerformance\":\"12\"");
        opmsEdit.NewValue.Should().Contain("\"ActualPerformance\":\"14\"");
        var ipmsEdit = audits.Single(item => item.EntityName == nameof(IpmsSubmission) && item.Action == "Edit");
        ipmsEdit.OldValue.Should().Contain("\"ActualPerformance\":\"13\"");
        ipmsEdit.NewValue.Should().Contain("\"ActualPerformance\":\"15\"");
    }

    private static OpmsSubmissionsController OpmsController(ApplicationDbContext context, ApplicationUser user,
        IAccessControlService access, IWorkflowGovernanceService governance, ISubmissionValueService values) =>
        new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access, governance,
            Mock.Of<IEvidenceBlobStorage>(), values, Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        { ControllerContext = ControllerContext(user.Id) };

    private static IpmsSubmissionsController IpmsController(ApplicationDbContext context, ApplicationUser user,
        IAccessControlService access, IWorkflowGovernanceService governance, ISubmissionValueService values) =>
        new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access, governance,
            Mock.Of<IEvidenceBlobStorage>(), values, Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        { ControllerContext = ControllerContext(user.Id) };

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = IdpTestFixture.CreatePrincipal(userId),
            TraceIdentifier = "atomic-submission-test"
        }
    };

    private static OpmsSubmissionResponse Data(ActionResult<ApiResponse<OpmsSubmissionResponse>> result)
    {
        if (result.Result is not OkObjectResult ok)
            throw new Xunit.Sdk.XunitException($"Expected OPMS success but received {(result.Result as ObjectResult)?.StatusCode}: {(result.Result as ObjectResult)?.Value}");
        return ok.Value.Should().BeOfType<ApiResponse<OpmsSubmissionResponse>>().Subject.Data!;
    }

    private static IpmsSubmissionResponse Data(ActionResult<ApiResponse<IpmsSubmissionResponse>> result)
    {
        if (result.Result is not OkObjectResult ok)
            throw new Xunit.Sdk.XunitException($"Expected IPMS success but received {(result.Result as ObjectResult)?.StatusCode}: {(result.Result as ObjectResult)?.Value}");
        return ok.Value.Should().BeOfType<ApiResponse<IpmsSubmissionResponse>>().Subject.Data!;
    }

    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => userId;
    }
}

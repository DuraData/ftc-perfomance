using FTCERP.Host.Application.Services;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FTCERP.Tests;

public sealed class PerformanceSuggestionServiceTests
{
    [Fact]
    public async Task Midterm_sum_is_persisted_once_and_an_authorised_edit_preserves_the_original_suggestion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeQ2Actual: true, includeAnnual: false);
        var service = CreateService(context);

        var generated = await service.GenerateOpmsAsync(seed.OpmsDestination.Id, seed.User.Id, "generate-midterm");

        generated.Generated.Should().BeTrue();
        generated.SystemSuggestedActualPerformance.Should().Be("50");
        generated.ActualPerformance.Should().Be("50");
        generated.SourcePeriods.Should().Equal("Q1", "Q2");
        var destination = await context.OpmsSubmissions.SingleAsync(item => item.Id == seed.OpmsDestination.Id);
        destination.SystemSuggestedActualPerformance.Should().Be("50");
        destination.ActualPerformance.Should().Be("50");
        destination.Variance.Should().Be(10m);
        destination.AchievementPercent.Should().Be(125m);
        destination.TargetAchieved.Should().BeTrue();
        (await context.PerformanceSuggestionEvents.SingleAsync()).EventType.Should().Be(PerformanceSuggestionEventType.Generated);

        var duplicate = await service.GenerateOpmsAsync(seed.OpmsDestination.Id, seed.User.Id, "duplicate");
        duplicate.Code.Should().Be("SUGGESTION_ALREADY_GENERATED");
        (await context.PerformanceSuggestionEvents.CountAsync()).Should().Be(1);

        var edited = await service.RecordFinalOpmsActualAsync(
            seed.OpmsDestination.Id, "45", "Source evidence was corrected after review.",
            Convert.ToBase64String(destination.RowVersion), seed.User.Id, "edit-midterm");

        edited.Code.Should().Be("SUGGESTION_EDITED");
        destination.SystemSuggestedActualPerformance.Should().Be("50");
        destination.ActualPerformance.Should().Be("45");
        destination.WasSystemSuggestionEdited.Should().BeTrue();
        destination.SuggestionEditReason.Should().Be("Source evidence was corrected after review.");
        destination.Variance.Should().Be(5m);
        destination.AchievementPercent.Should().Be(112.5m);
        (await context.PerformanceSuggestionEvents.OrderBy(item => item.Id).Select(item => item.EventType).ToArrayAsync())
            .Should().Equal(PerformanceSuggestionEventType.Generated, PerformanceSuggestionEventType.Edited);
    }

    [Fact]
    public async Task Missing_required_quarter_fails_closed_without_mutating_the_submission_or_history()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeQ2Actual: false, includeAnnual: false);
        var service = CreateService(context);

        var result = await service.GenerateOpmsAsync(seed.OpmsDestination.Id, seed.User.Id, "missing-q2");

        result.Generated.Should().BeFalse();
        result.ManualRequired.Should().BeTrue();
        result.Code.Should().Be("MISSING_SOURCE_VALUES");
        seed.OpmsDestination.SystemSuggestedActualPerformance.Should().BeNull();
        seed.OpmsDestination.ActualPerformance.Should().BeNull();
        (await context.PerformanceSuggestionEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Annual_ipms_average_uses_all_four_submitted_quarters_and_can_be_accepted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeQ2Actual: true, includeAnnual: true, calculationCode: "AVERAGE");
        var service = CreateService(context);

        var generated = await service.GenerateIpmsAsync(seed.IpmsDestination!.Id, seed.User.Id, "generate-annual");

        generated.SystemSuggestedActualPerformance.Should().Be("25");
        generated.SourcePeriods.Should().Equal("Q1", "Q2", "Q3", "Q4");
        var destination = await context.IpmsSubmissions.SingleAsync(item => item.Id == seed.IpmsDestination.Id);
        var accepted = await service.RecordFinalIpmsActualAsync(
            destination.Id, "25", null, Convert.ToBase64String(destination.RowVersion), seed.User.Id, "accept-annual");

        accepted.Code.Should().Be("SUGGESTION_ACCEPTED");
        destination.WasSystemSuggestionEdited.Should().BeFalse();
        destination.SuggestionEditReason.Should().BeNull();
        (await context.PerformanceSuggestionEvents.OrderBy(item => item.Id).Select(item => item.EventType).ToArrayAsync())
            .Should().Equal(PerformanceSuggestionEventType.Generated, PerformanceSuggestionEventType.Accepted);
    }

    [Fact]
    public async Task Direct_api_call_is_denied_when_actual_performance_member_permission_is_denied()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seed = await SeedAsync(context, includeQ2Actual: true, includeAnnual: false);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(seed.User, "OPMS_SUBMISSION.UPDATE", It.IsAny<AccessScopeContext>()))
            .ReturnsAsync(Decision(true, "Allowed"));
        access.Setup(item => item.CheckPermissionAsync(seed.User, "OPMS_SUBMISSION.ActualPerformance.UPDATE", It.IsAny<AccessScopeContext>()))
            .ReturnsAsync(Decision(false, "Actual Performance is protected by member-level security."));
        var suggestions = new Mock<IPerformanceSuggestionService>();
        var controller = CreateController(context, seed.User, access.Object, suggestions.Object);

        var action = await controller.GenerateConsolidationSuggestion(seed.OpmsDestination.Id);

        var denied = action.Result.Should().BeOfType<ObjectResult>().Subject;
        denied.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        suggestions.Verify(item => item.GenerateOpmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static PerformanceSuggestionService CreateService(ApplicationDbContext context)
    {
        var units = new PerformanceUnitEngine();
        return new PerformanceSuggestionService(context, new PerformanceConsolidationEngine(units), units);
    }

    private static OpmsSubmissionsController CreateController(
        ApplicationDbContext context,
        ApplicationUser user,
        IAccessControlService access,
        IPerformanceSuggestionService suggestions)
    {
        var controller = new OpmsSubmissionsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access,
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<IEvidenceBlobStorage>(),
            Mock.Of<ISubmissionValueService>(),
            Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(),
            Mock.Of<IEvidenceInspectionService>(),
            Mock.Of<IEvidenceMalwareScanner>(),
            suggestions)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id), TraceIdentifier = "api-denial" }
            }
        };
        return controller;
    }

    private static AccessDecisionResult Decision(bool allowed, string reason) => new(allowed, reason, [], [], []);

    private static async Task<SeedResult> SeedAsync(
        ApplicationDbContext context,
        bool includeQ2Actual,
        bool includeAnnual,
        string calculationCode = "SUM")
    {
        var municipality = new Municipality { Code = $"SUG-{Guid.NewGuid():N}"[..20], Name = "Suggestion Municipality" };
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();
        var user = IdpTestFixture.CreateUser($"suggestion-{Guid.NewGuid():N}");
        user.MunicipalityId = municipality.Id;
        context.Users.Add(user);
        var financialYear = new FinancialYear
        {
            Code = $"FY-{Guid.NewGuid():N}"[..20], Name = "2026/2027",
            StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30)
        };
        context.FinancialYears.Add(financialYear);
        await context.SaveChangesAsync();
        var municipalityYear = new MunicipalityFinancialYear
        {
            MunicipalityId = municipality.Id, FinancialYearId = financialYear.Id, IsCurrent = true,
            EffectiveFrom = financialYear.StartDate
        };
        context.MunicipalityFinancialYears.Add(municipalityYear);
        await context.SaveChangesAsync();
        var periods = new[]
        {
            Period(municipalityYear.Id, "Q1", ReportingPeriodType.Quarter1, 1),
            Period(municipalityYear.Id, "Q2", ReportingPeriodType.Quarter2, 2),
            Period(municipalityYear.Id, "MID_TERM", ReportingPeriodType.MidTerm, 3),
            Period(municipalityYear.Id, "Q3", ReportingPeriodType.Quarter3, 4),
            Period(municipalityYear.Id, "Q4", ReportingPeriodType.Quarter4, 5),
            Period(municipalityYear.Id, "ANNUAL", ReportingPeriodType.Annual, 6)
        };
        context.ReportingPeriods.AddRange(periods);
        await context.SaveChangesAsync();
        var calculationTypeId = await context.PerformanceCalculationTypes.Where(item => item.Code == calculationCode).Select(item => item.Id).SingleAsync();

        var opmsTarget = new OpmsTarget
        {
            MunicipalityId = municipality.Id, IndicatorNumber = "OPMS-SUG-1", TargetName = "OPMS suggestion",
            KpiDescription = "Consolidate quarterly actuals", PerformanceObjective = "Consolidation",
            NationalKpa = "Governance", MunicipalKpa = "Governance", AnnualTargetDescription = "40",
            CalculationTypeId = calculationTypeId, Weight = 1, TargetUnitType = "absolute_count"
        };
        context.OpmsTargets.Add(opmsTarget);
        IpmsTarget? ipmsTarget = null;
        if (includeAnnual)
        {
            ipmsTarget = new IpmsTarget
            {
                MunicipalityId = municipality.Id, IndicatorNumber = "IPMS-SUG-1", TargetName = "IPMS suggestion",
                KpiDescription = "Consolidate quarterly actuals", PerformanceObjective = "Consolidation",
                NationalKpa = "Governance", MunicipalKpa = "Governance", AnnualTargetDescription = "20",
                CalculationTypeId = calculationTypeId, Weight = 1, TargetUnitType = "absolute_count"
            };
            context.IpmsTargets.Add(ipmsTarget);
        }
        await context.SaveChangesAsync();

        foreach (var period in periods)
        {
            if (!includeAnnual && period.PeriodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual) continue;
            context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
            {
                MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, OpmsTargetId = opmsTarget.Id,
                UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
                TargetValue = period.PeriodType == ReportingPeriodType.MidTerm ? "40" : "20", CreatedByUserId = user.Id
            });
            if (ipmsTarget != null)
                context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
                {
                    MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, IpmsTargetId = ipmsTarget.Id,
                    UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
                    TargetValue = "20", CreatedByUserId = user.Id
                });
        }

        var opmsDestination = Submission(opmsTarget, periods[2], municipality.Id, null);
        opmsDestination.SubmittedByUserId = user.Id;
        context.OpmsSubmissions.AddRange(
            Submission(opmsTarget, periods[0], municipality.Id, "20"),
            Submission(opmsTarget, periods[1], municipality.Id, includeQ2Actual ? "30" : null),
            opmsDestination);
        IpmsSubmission? ipmsDestination = null;
        if (ipmsTarget != null)
        {
            context.IpmsSubmissions.AddRange(
                Submission(ipmsTarget, periods[0], municipality.Id, "10"),
                Submission(ipmsTarget, periods[1], municipality.Id, "20"),
                Submission(ipmsTarget, periods[3], municipality.Id, "30"),
                Submission(ipmsTarget, periods[4], municipality.Id, "40"));
            ipmsDestination = Submission(ipmsTarget, periods[5], municipality.Id, null);
            ipmsDestination.SubmittedByUserId = user.Id;
            context.IpmsSubmissions.Add(ipmsDestination);
        }
        await context.SaveChangesAsync();
        return new(user, opmsDestination, ipmsDestination);
    }

    private static ReportingPeriod Period(long yearId, string code, ReportingPeriodType type, int sequence) => new()
    {
        MunicipalityFinancialYearId = yearId, Code = code, Name = code, PeriodType = type, Sequence = sequence,
        StartDate = new DateTime(2026, 7, 1).AddMonths(sequence - 1), EndDate = new DateTime(2026, 7, 28).AddMonths(sequence - 1)
    };

    private static OpmsSubmission Submission(OpmsTarget target, ReportingPeriod period, long municipalityId, string? actual) => new()
    {
        MunicipalityId = municipalityId, OpmsTargetId = target.Id, ReportingPeriodId = period.Id, Quarter = period.Code,
        Status = actual == null ? "Draft" : "Submitted", BaseState = actual == null ? SubmissionBaseStates.InProgress : SubmissionBaseStates.Submitted,
        ActualPerformance = actual, SubmittedAt = actual == null ? null : DateTime.UtcNow
    };

    private static IpmsSubmission Submission(IpmsTarget target, ReportingPeriod period, long municipalityId, string? actual) => new()
    {
        MunicipalityId = municipalityId, IpmsTargetId = target.Id, ReportingPeriodId = period.Id, Quarter = period.Code,
        Status = actual == null ? "Draft" : "Submitted", BaseState = actual == null ? SubmissionBaseStates.InProgress : SubmissionBaseStates.Submitted,
        ActualPerformance = actual, SubmittedAt = actual == null ? null : DateTime.UtcNow
    };

    private sealed record SeedResult(ApplicationUser User, OpmsSubmission OpmsDestination, IpmsSubmission? IpmsDestination);
}

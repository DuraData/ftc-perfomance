using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class CanonicalActualPerformanceCutoverTests
{
    [Fact]
    public void Runtime_model_and_public_contract_expose_only_canonical_actual_performance()
    {
        AssertCanonicalContract(typeof(SaveOpmsSubmissionRequest));
        AssertCanonicalContract(typeof(SaveIpmsSubmissionRequest));
        AssertCanonicalContract(typeof(OpmsSubmissionResponse));
        AssertCanonicalContract(typeof(IpmsSubmissionResponse));
        Assert.Null(typeof(SaveOpmsSubmissionRequest).GetProperty("Variance"));
        Assert.Null(typeof(SaveIpmsSubmissionRequest).GetProperty("Variance"));
        Assert.NotNull(typeof(SaveOpmsSubmissionRequest).GetProperty("ReportingPeriodPublicId"));
        Assert.NotNull(typeof(SaveIpmsSubmissionRequest).GetProperty("ReportingPeriodPublicId"));
        Assert.Null(typeof(SaveOpmsSubmissionRequest).GetProperty("Quarter"));
        Assert.Null(typeof(SaveIpmsSubmissionRequest).GetProperty("Quarter"));
        Assert.Null(typeof(OpmsSubmissionResponse).GetProperty("CreatedBy"));
        Assert.Null(typeof(OpmsSubmissionResponse).GetProperty("UpdatedBy"));
        Assert.Null(typeof(IpmsSubmissionResponse).GetProperty("CreatedBy"));
        Assert.Null(typeof(IpmsSubmissionResponse).GetProperty("UpdatedBy"));

        using var context = IdpTestFixture.CreateRelationalContext();
        var opms = context.Model.FindEntityType(typeof(OpmsSubmission))!;
        var ipms = context.Model.FindEntityType(typeof(IpmsSubmission))!;
        Assert.NotNull(opms.FindProperty(nameof(OpmsSubmission.ActualPerformance)));
        Assert.NotNull(ipms.FindProperty(nameof(IpmsSubmission.ActualPerformance)));
        Assert.Null(opms.FindProperty("Actual"));
        Assert.Null(ipms.FindProperty("Actual"));
        Assert.Null(opms.FindProperty("ActualDescription"));
        Assert.Null(ipms.FindProperty("ActualDescription"));
        Assert.Null(opms.FindProperty("ActualPerformanceDescription"));
        Assert.Null(ipms.FindProperty("ActualPerformanceDescription"));
    }

    [Fact]
    public async Task Legacy_value_archive_is_relationally_linked_and_append_only()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = new Municipality { Code = "ARCHIVE", Name = "Archive Municipality" };
        var target = new OpmsTarget
        {
            Municipality = municipality,
            IndicatorNumber = "ARCH-1",
            TargetName = "Archived actual target",
            KpiDescription = "Archive test",
            PerformanceObjective = "Archive test",
            NationalKpa = "Governance",
            MunicipalKpa = "Governance"
        };
        var submission = new OpmsSubmission
        {
            Municipality = municipality,
            OpmsTarget = target,
            Quarter = "Q1",
            ActualPerformance = "42",
            Status = SubmissionBaseStates.InProgress
        };
        context.AddRange(municipality, target, submission);
        await context.SaveChangesAsync();

        var archive = new LegacySubmissionValueArchive
        {
            MunicipalityId = municipality.Id,
            SubmissionKind = SubmissionKind.Opms,
            OpmsSubmissionId = submission.Id,
            LegacyActual = 42m,
            LegacyActualDescription = "Historic narrative",
            CanonicalActualPerformance = "42"
        };
        context.LegacySubmissionValueArchives.Add(archive);
        await context.SaveChangesAsync();

        Assert.Equal(submission.Id, (await context.LegacySubmissionValueArchives.SingleAsync()).OpmsSubmissionId);
        archive.ArchiveReason = "Changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Submission_value_resolution_uses_the_target_scoped_reporting_period_public_id()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("canonical-period-user");
        var municipality = new Municipality { Code = "CANONICAL-PERIOD", Name = "Canonical Period Municipality" };
        var financialYear = new FinancialYear
        {
            Code = "2034/35", Name = "2034/2035", StartDate = new(2034, 7, 1), EndDate = new(2035, 6, 30)
        };
        var municipalityYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = financialYear, IsCurrent = true, EffectiveFrom = financialYear.StartDate
        };
        var period = new ReportingPeriod
        {
            MunicipalityFinancialYear = municipalityYear, Code = "Q1-CANONICAL", Name = "Canonical quarter one",
            PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = financialYear.StartDate, EndDate = new(2034, 9, 30)
        };
        var target = new OpmsTarget
        {
            Id = "canonical-period-target", Municipality = municipality, IndicatorNumber = "CANONICAL-1",
            TargetName = "Canonical period target", KpiDescription = "Canonical period target"
        };
        context.AddRange(user, municipality, financialYear, municipalityYear, period, target);
        await context.SaveChangesAsync();
        context.PerformancePeriodTargets.Add(new PerformancePeriodTarget
        {
            MunicipalityId = municipality.Id, ReportingPeriodId = period.Id, OpmsTargetId = target.Id,
            UnitKind = PerformanceUnitKind.AbsoluteCount, Direction = PerformanceDirection.HigherIsBetter,
            TargetValue = "10", CreatedByUserId = user.Id
        });
        await context.SaveChangesAsync();

        var service = new SubmissionValueService(context, new PerformanceUnitEngine());
        var resolution = await service.ResolveOpmsAsync(target.Id, period.PublicId, "12", null);

        Assert.Equal(period.Id, resolution.Period.Id);
        Assert.Equal("12", resolution.Calculation!.CanonicalActual);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResolveOpmsAsync(target.Id, Guid.NewGuid(), "12", null));
    }

    private static void AssertCanonicalContract(Type type)
    {
        Assert.NotNull(type.GetProperty("ActualPerformance"));
        Assert.Null(type.GetProperty("Actual"));
        Assert.Null(type.GetProperty("ActualDescription"));
        Assert.Null(type.GetProperty("ActualPerformanceDescription"));
    }
}

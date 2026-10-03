using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
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

    private static void AssertCanonicalContract(Type type)
    {
        Assert.NotNull(type.GetProperty("ActualPerformance"));
        Assert.Null(type.GetProperty("Actual"));
        Assert.Null(type.GetProperty("ActualDescription"));
        Assert.Null(type.GetProperty("ActualPerformanceDescription"));
    }
}

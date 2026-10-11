using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;

namespace FTCERP.Tests;

public sealed class OpmsTargetResponseProjectionTests
{
    [Fact]
    public void Projection_preserves_canonical_strategic_and_vote_details()
    {
        var target = new OpmsTarget
        {
            Id = "target-1",
            PublicId = Guid.NewGuid(),
            IndicatorNumber = "OPMS-1",
            NationalKpa = "Basic Service Delivery",
            MunicipalKpa = "Infrastructure",
            PerformanceObjective = "Legacy objective",
            TargetName = "Road target",
            KpiDescription = "Roads resurfaced",
            KpiType = "Quantitative",
            IndicatorType = "Output",
            StrategicGoalMaster = new MunicipalStrategicGoal { PublicId = Guid.NewGuid(), Code = "SG-INFRA", Name = "Reliable Infrastructure Services" },
            StrategicObjectiveMaster = new MunicipalStrategicObjective { PublicId = Guid.NewGuid(), Code = "SO-ROADS", Name = "Improve Road Infrastructure" },
            PerformanceObjectiveReference = new PerformanceObjective { PublicId = Guid.NewGuid(), Code = "PO-ROADS", Name = "Resurface Priority Roads" },
            CanonicalPeriodTargets =
            [
                new PerformancePeriodTarget
                {
                    ReportingPeriod = new ReportingPeriod
                    {
                        MunicipalityFinancialYear = new MunicipalityFinancialYear
                        {
                            PublicId = Guid.NewGuid(),
                            FinancialYear = new FinancialYear { Name = "2024/2025" }
                        }
                    }
                }
            ]
        };
        var vote = new VoteNumber { Id = 12, PublicId = Guid.NewGuid(), Code = "VOTE-01", Number = "001", Name = "Roads Capital Programme", Amount = 12_500_000m };
        target.VoteNumbers.Add(new OpmsTargetVoteNumber { VoteNumberId = vote.Id, VoteNumber = vote });

        var response = target.ToResponse();

        Assert.Equal("SG-INFRA", response.StrategicGoalCode);
        Assert.Equal("Reliable Infrastructure Services", response.StrategicGoalName);
        Assert.Equal("SO-ROADS", response.StrategicObjectiveCode);
        Assert.Equal("Improve Road Infrastructure", response.StrategicObjectiveName);
        Assert.Equal("PO-ROADS", response.PerformanceObjectiveCode);
        Assert.Equal("Resurface Priority Roads", response.PerformanceObjectiveName);
        Assert.Equal("2024/2025", response.MunicipalityFinancialYearName);
        var projectedVote = Assert.Single(response.VoteNumbers);
        Assert.Equal("001", projectedVote.Number);
        Assert.Equal("Roads Capital Programme", projectedVote.Name);
        Assert.Equal(12_500_000m, projectedVote.Amount);

        var masked = target.ToResponse(
            FTCERP.Host.API.Responses.PeriodTargetMemberAccess.Full,
            FTCERP.Host.API.Responses.KpiLifecycleMemberAccess.Full,
            FTCERP.Host.API.Responses.VoteNumberMemberAccess.None);
        var maskedVote = Assert.Single(masked.VoteNumbers);
        Assert.Null(maskedVote.Number);
        Assert.Null(maskedVote.Amount);
        Assert.Equal("VOTE-01", maskedVote.Code);
        Assert.Equal("Roads Capital Programme", maskedVote.Name);
    }

    [Fact]
    public void Live_kpi_projection_masks_sensitive_references_and_budget_amounts_without_hiding_source_identity()
    {
        var source = new GovernedBudgetSource { PublicId = Guid.NewGuid(), Code = "OWN", Name = "Own revenue" };
        var opms = new OpmsTarget
        {
            PublicId = Guid.NewGuid(), IndicatorNumber = "OPMS-SEC", TargetName = "Protected OPMS KPI", KpiDescription = "Description",
            InternalReference = "INTERNAL-SECRET", FmsLink = "FMS-SECRET"
        };
        opms.GovernedBudgetSources.Add(new OpmsKpiBudgetSource { PublicId = Guid.NewGuid(), BudgetSource = source, BudgetSourceId = 1, Amount = 250m });
        var ipms = new IpmsTarget
        {
            PublicId = Guid.NewGuid(), IndicatorNumber = "IPMS-SEC", TargetName = "Protected IPMS KPI", KpiDescription = "Description",
            InternalReference = "EMPLOYEE-SECRET"
        };
        ipms.GovernedBudgetSources.Add(new IpmsKpiBudgetSource { PublicId = Guid.NewGuid(), BudgetSource = source, BudgetSourceId = 1, Amount = 125m });

        var opmsResponse = opms.ToResponse(PeriodTargetMemberAccess.Full, KpiLifecycleMemberAccess.Full,
            VoteNumberMemberAccess.Full, KpiSensitiveMemberAccess.None);
        var ipmsResponse = ipms.ToResponse(null, PeriodTargetMemberAccess.Full, KpiLifecycleMemberAccess.Full,
            KpiSensitiveMemberAccess.None);

        Assert.Null(opmsResponse.InternalReference);
        Assert.Null(opmsResponse.FmsLink);
        Assert.Null(Assert.Single(opmsResponse.BudgetSources).Amount);
        Assert.Equal("OWN", Assert.Single(opmsResponse.BudgetSources).Code);
        Assert.Null(ipmsResponse.InternalReference);
        Assert.Null(Assert.Single(ipmsResponse.BudgetSources).Amount);
        Assert.Equal("Own revenue", Assert.Single(ipmsResponse.BudgetSources).Name);
    }
}

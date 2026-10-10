using FTCERP.Host.API.Controllers;
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
    }
}

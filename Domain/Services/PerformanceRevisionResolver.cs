using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Domain.Services;

public static class PerformanceRevisionResolver
{
    public static bool UsesRevisedValues(ReportingPeriodType periodType) =>
        periodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual;

    public static ReportingPeriodType ResolvePeriodType(ReportingPeriodType? periodType, string? legacyCode) =>
        periodType ?? legacyCode?.Trim().ToUpperInvariant() switch
        {
            "Q2" or "QUARTER2" or "QUARTER 2" => ReportingPeriodType.Quarter2,
            "MID" or "MIDTERM" or "MID-TERM" or "MID TERM" => ReportingPeriodType.MidTerm,
            "Q3" or "QUARTER3" or "QUARTER 3" => ReportingPeriodType.Quarter3,
            "Q4" or "QUARTER4" or "QUARTER 4" => ReportingPeriodType.Quarter4,
            "ANNUAL" or "ANN" or "YEAR" or "YEAR-END" => ReportingPeriodType.Annual,
            _ => ReportingPeriodType.Quarter1
        };

    public static string EffectiveIndicatorNumber(OpmsTarget target, ReportingPeriodType periodType) =>
        UsesRevisedValues(periodType) && target.IsIndicatorNumberRevised && !string.IsNullOrWhiteSpace(target.RevisedIndicatorNumber)
            ? target.RevisedIndicatorNumber
            : target.IndicatorNumber;

    public static string EffectiveIndicatorNumber(IpmsTarget target, ReportingPeriodType periodType) =>
        UsesRevisedValues(periodType) && target.IsIndicatorNumberRevised && !string.IsNullOrWhiteSpace(target.RevisedIndicatorNumber)
            ? target.RevisedIndicatorNumber
            : target.IndicatorNumber;

    public static string EffectiveTargetName(OpmsTarget target, ReportingPeriodType periodType) =>
        UsesRevisedValues(periodType) && target.IsTargetNameRevised && !string.IsNullOrWhiteSpace(target.RevisedTargetName)
            ? target.RevisedTargetName
            : target.TargetName;

    public static string EffectiveTargetName(IpmsTarget target, ReportingPeriodType periodType) =>
        UsesRevisedValues(periodType) && target.IsTargetNameRevised && !string.IsNullOrWhiteSpace(target.RevisedTargetName)
            ? target.RevisedTargetName
            : target.TargetName;

    public static string EffectiveKpiDescription(OpmsTarget target, ReportingPeriodType periodType) =>
        UsesRevisedValues(periodType) && target.IsKpiDescriptionRevised && !string.IsNullOrWhiteSpace(target.RevisedKpiDescription)
            ? target.RevisedKpiDescription
            : target.KpiDescription;

    public static string EffectiveKpiDescription(IpmsTarget target, ReportingPeriodType periodType) =>
        UsesRevisedValues(periodType) && target.IsKpiDescriptionRevised && !string.IsNullOrWhiteSpace(target.RevisedKpiDescription)
            ? target.RevisedKpiDescription
            : target.KpiDescription;

    public static string EffectiveTargetValue(PerformancePeriodTarget target) =>
        UsesRevisedValues(target.ReportingPeriod.PeriodType) && target.IsTargetRevised && !string.IsNullOrWhiteSpace(target.RevisedTargetValue)
            ? target.RevisedTargetValue
            : target.TargetValue;

    public static PerformanceUnitKind EffectiveUnitKind(PerformancePeriodTarget target) =>
        UsesRevisedValues(target.ReportingPeriod.PeriodType) && target.IsTargetRevised && target.RevisedUnitKind.HasValue
            ? target.RevisedUnitKind.Value
            : target.UnitKind;

    public static decimal? EffectiveBudgetValue(PerformancePeriodTarget target) =>
        UsesRevisedValues(target.ReportingPeriod.PeriodType) && target.IsBudgetRevised
            ? target.RevisedBudgetValue
            : target.BudgetValue;
}

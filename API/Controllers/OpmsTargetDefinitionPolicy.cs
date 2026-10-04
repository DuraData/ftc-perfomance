namespace FTCERP.Host.API.Controllers;

internal static class OpmsTargetDefinitionPolicy
{
    public static string? Validate(string? indicatorNumber, int orderNumber, string? targetName, string? kpiDescription,
        string? nationalKpa, string? municipalKpa, string? performanceObjective, decimal weight, string? kpiType, string? indicatorType)
    {
        if (string.IsNullOrWhiteSpace(indicatorNumber) || indicatorNumber.Trim().Length > 120) return "Indicator number must contain 1 to 120 characters.";
        if (orderNumber < 1) return "Original order number must be positive.";
        if (string.IsNullOrWhiteSpace(targetName) || targetName.Trim().Length > 500) return "Target name must contain 1 to 500 characters.";
        if (string.IsNullOrWhiteSpace(kpiDescription) || kpiDescription.Trim().Length > 2000) return "KPI wording must contain 1 to 2000 characters.";
        if (string.IsNullOrWhiteSpace(nationalKpa) || string.IsNullOrWhiteSpace(municipalKpa) || string.IsNullOrWhiteSpace(performanceObjective)) return "National KPA, municipal KPA and performance objective are required.";
        if (weight < 0) return "KPI weight cannot be negative.";
        if (string.IsNullOrWhiteSpace(kpiType) || string.IsNullOrWhiteSpace(indicatorType)) return "KPI type and indicator type are required.";
        return null;
    }
}

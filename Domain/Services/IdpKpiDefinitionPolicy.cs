using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Domain.Services;

public sealed record IdpKpiDefinitionInput(
    int IdpProjectId,
    string KpiCode,
    string KpiName,
    string Description,
    string Formula,
    decimal Baseline,
    decimal AnnualTarget,
    decimal FiveYearTarget,
    int? ResponsibleDepartmentId,
    string DataSource,
    string ReportingFrequency,
    string IndicatorType,
    bool Circular88Linked,
    bool TreasuryTidLinked);

public sealed record NormalizedIdpKpiDefinition(
    int IdpProjectId,
    string KpiCode,
    string KpiName,
    string Description,
    string Formula,
    decimal Baseline,
    decimal AnnualTarget,
    decimal FiveYearTarget,
    int? ResponsibleDepartmentId,
    string DataSource,
    string ReportingFrequency,
    IdpKpiIndicatorType IndicatorType,
    bool Circular88Linked,
    bool TreasuryTidLinked);

public sealed record IdpKpiValidationIssue(string Code, string Field, string? SuppliedValue, string Message);

public static class IdpKpiDefinitionPolicy
{
    public static bool TryNormalize(
        IdpKpiDefinitionInput input,
        out NormalizedIdpKpiDefinition? definition,
        out IdpKpiValidationIssue? issue)
    {
        definition = null;
        issue = Required(input.KpiCode, nameof(input.KpiCode), 100)
            ?? Required(input.KpiName, nameof(input.KpiName), 300)
            ?? Required(input.Description, nameof(input.Description), 4000)
            ?? Required(input.Formula, nameof(input.Formula), 2000)
            ?? Required(input.DataSource, nameof(input.DataSource), 500)
            ?? Required(input.ReportingFrequency, nameof(input.ReportingFrequency), 100);
        if (issue != null) return false;

        if (!TryParseEnum(input.IndicatorType, out IdpKpiIndicatorType indicatorType))
        {
            issue = new("INVALID_INDICATOR_TYPE", nameof(input.IndicatorType), input.IndicatorType, "Indicator type is not supported.");
            return false;
        }

        definition = new(
            input.IdpProjectId,
            input.KpiCode.Trim().ToUpperInvariant(),
            input.KpiName.Trim(),
            input.Description.Trim(),
            input.Formula.Trim(),
            input.Baseline,
            input.AnnualTarget,
            input.FiveYearTarget,
            input.ResponsibleDepartmentId,
            input.DataSource.Trim(),
            input.ReportingFrequency.Trim(),
            indicatorType,
            input.Circular88Linked,
            input.TreasuryTidLinked);
        issue = null;
        return true;
    }

    public static void Apply(IdpKpi entity, NormalizedIdpKpiDefinition definition)
    {
        entity.IdpProjectId = definition.IdpProjectId;
        entity.KpiCode = definition.KpiCode;
        entity.KpiName = definition.KpiName;
        entity.Description = definition.Description;
        entity.Formula = definition.Formula;
        entity.Baseline = definition.Baseline;
        entity.AnnualTarget = definition.AnnualTarget;
        entity.FiveYearTarget = definition.FiveYearTarget;
        entity.ResponsibleDepartmentId = definition.ResponsibleDepartmentId;
        entity.DataSource = definition.DataSource;
        entity.ReportingFrequency = definition.ReportingFrequency;
        entity.IndicatorType = definition.IndicatorType;
        entity.Circular88Linked = definition.Circular88Linked;
        entity.TreasuryTidLinked = definition.TreasuryTidLinked;
    }

    public static bool IsEquivalent(IdpKpi entity, NormalizedIdpKpiDefinition definition) =>
        entity.IdpProjectId == definition.IdpProjectId
        && string.Equals(entity.KpiCode, definition.KpiCode, StringComparison.Ordinal)
        && string.Equals(entity.KpiName, definition.KpiName, StringComparison.Ordinal)
        && string.Equals(entity.Description, definition.Description, StringComparison.Ordinal)
        && string.Equals(entity.Formula, definition.Formula, StringComparison.Ordinal)
        && entity.Baseline == definition.Baseline
        && entity.AnnualTarget == definition.AnnualTarget
        && entity.FiveYearTarget == definition.FiveYearTarget
        && entity.ResponsibleDepartmentId == definition.ResponsibleDepartmentId
        && string.Equals(entity.DataSource, definition.DataSource, StringComparison.Ordinal)
        && string.Equals(entity.ReportingFrequency, definition.ReportingFrequency, StringComparison.Ordinal)
        && entity.IndicatorType == definition.IndicatorType
        && entity.Circular88Linked == definition.Circular88Linked
        && entity.TreasuryTidLinked == definition.TreasuryTidLinked;

    private static IdpKpiValidationIssue? Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return new("REQUIRED", field, value, $"{field} is required.");
        return value.Trim().Length > maxLength
            ? new("MAX_LENGTH", field, value, $"{field} cannot exceed {maxLength} characters.")
            : null;
    }

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed) where TEnum : struct, Enum
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
        foreach (var name in Enum.GetNames<TEnum>())
        {
            var candidate = name.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase)) continue;
            parsed = Enum.Parse<TEnum>(name, true);
            return true;
        }

        parsed = default;
        return false;
    }
}

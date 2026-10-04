using System.Text.Json;
using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Reporting;

public sealed record OfficialReportColumn(string Key, string Heading);
public sealed record OfficialReportDataRow(IReadOnlyDictionary<string, string> Values)
{
    public string Value(string key) => Values.TryGetValue(key, out var value) ? value : string.Empty;
}

public static class OfficialReportCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Headings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["indicator"] = "Indicator", ["targetName"] = "Target", ["department"] = "Department", ["unit"] = "Unit", ["period"] = "Period",
        ["targetValue"] = "Target Value", ["actualPerformance"] = "Actual Performance", ["variance"] = "Variance", ["achievementPercent"] = "Achievement Percent",
        ["targetAchieved"] = "Target Achieved", ["status"] = "Status", ["submissionId"] = "Submission ID", ["submittedBy"] = "Submitted By",
        ["submittedAt"] = "Submitted At", ["workflow"] = "Workflow", ["currentStage"] = "Current Stage", ["startedAt"] = "Started At", ["completedAt"] = "Completed At",
        ["action"] = "Action", ["actor"] = "Actor", ["occurredAt"] = "Occurred At", ["comment"] = "Comment", ["rating"] = "Rating",
        ["outcome"] = "Outcome", ["observation"] = "Observation", ["findings"] = "Findings", ["recommendation"] = "Recommendation", ["score"] = "Score",
        ["rfiId"] = "RFI ID", ["question"] = "Question", ["raisedBy"] = "Raised By", ["raisedAt"] = "Raised At", ["responseDueAt"] = "Response Due At",
        ["response"] = "Response", ["respondedBy"] = "Responded By", ["respondedAt"] = "Responded At", ["closedAt"] = "Closed At",
        ["evidenceId"] = "Evidence ID", ["fileName"] = "File Name", ["contentType"] = "Content Type", ["sizeInBytes"] = "Size (bytes)", ["sha256"] = "SHA-256",
        ["scanStatus"] = "Scan Status", ["uploadedBy"] = "Uploaded By", ["uploadedAt"] = "Uploaded At", ["retainUntil"] = "Retain Until",
        ["entityName"] = "Entity", ["entityId"] = "Entity ID", ["changedBy"] = "Changed By", ["changedAt"] = "Changed At", ["reason"] = "Reason",
        ["correlationId"] = "Correlation ID", ["source"] = "Version Source", ["field"] = "Field", ["originalValue"] = "Original Value", ["revisedValue"] = "Revised Value",
        ["versionNumber"] = "Version", ["effectiveAt"] = "Effective At", ["approvalReference"] = "Approval Reference", ["group"] = "Group",
        ["configuredTargets"] = "Configured Targets", ["submissions"] = "Submissions", ["achieved"] = "Achieved", ["atRisk"] = "At Risk", ["pending"] = "Pending",
        ["averageAchievementPercent"] = "Average Achievement Percent"
    };

    private static readonly IReadOnlyDictionary<OfficialReportType, string[]> Defaults = new Dictionary<OfficialReportType, string[]>
    {
        [OfficialReportType.QuarterlyPerformance] = PerformanceColumns(),
        [OfficialReportType.MidTermPerformance] = PerformanceColumns(),
        [OfficialReportType.AnnualPerformance] = PerformanceColumns(),
        [OfficialReportType.DepartmentalPerformance] = PerformanceColumns(),
        [OfficialReportType.UnitPerformance] = PerformanceColumns(),
        [OfficialReportType.PerformanceSummary] = ["group", "configuredTargets", "submissions", "achieved", "atRisk", "pending", "averageAchievementPercent"],
        [OfficialReportType.WorkflowStatus] = ["submissionId", "indicator", "targetName", "department", "unit", "period", "workflow", "status", "currentStage", "startedAt", "completedAt"],
        [OfficialReportType.SubmissionRegister] = ["submissionId", "indicator", "targetName", "department", "unit", "period", "actualPerformance", "achievementPercent", "submittedBy", "submittedAt", "status"],
        [OfficialReportType.VerificationRegister] = ActionColumns(),
        [OfficialReportType.ApprovalRegister] = ActionColumns(),
        [OfficialReportType.PmsReview] = ActionColumns(),
        [OfficialReportType.InternalAudit] = ["submissionId", "indicator", "targetName", "department", "unit", "period", "outcome", "observation", "findings", "recommendation", "score", "actor", "occurredAt"],
        [OfficialReportType.OutstandingRfi] = ["rfiId", "submissionId", "indicator", "targetName", "department", "unit", "period", "question", "status", "raisedBy", "raisedAt", "responseDueAt", "response", "respondedBy", "respondedAt"],
        [OfficialReportType.EvidenceRegister] = ["evidenceId", "submissionId", "indicator", "targetName", "department", "unit", "period", "fileName", "contentType", "sizeInBytes", "sha256", "scanStatus", "uploadedBy", "uploadedAt", "retainUntil"],
        [OfficialReportType.AuditTrail] = ["entityName", "entityId", "action", "changedBy", "changedAt", "reason", "correlationId"],
        [OfficialReportType.VersionTrail] = ["source", "entityId", "field", "originalValue", "revisedValue", "versionNumber", "actor", "effectiveAt", "reason", "approvalReference"]
    };

    public static string DefaultColumnsJson(OfficialReportType reportType) => JsonSerializer.Serialize(DefaultColumnKeys(reportType));
    public static string[] DefaultColumnKeys(OfficialReportType reportType) => Defaults.TryGetValue(reportType, out var columns) ? columns : throw new ArgumentOutOfRangeException(nameof(reportType));

    public static OfficialReportColumn[] ResolveColumns(OfficialReportType reportType, string? json)
    {
        string[] requested;
        try { requested = JsonSerializer.Deserialize<string[]>(string.IsNullOrWhiteSpace(json) ? DefaultColumnsJson(reportType) : json) ?? []; }
        catch (JsonException exception) { throw new ArgumentException("Column configuration must be a JSON array of supported column names.", nameof(json), exception); }
        if (requested.Length == 0) requested = DefaultColumnKeys(reportType);
        if (requested.Length != requested.Distinct(StringComparer.OrdinalIgnoreCase).Count()) throw new ArgumentException("Column configuration contains duplicate columns.", nameof(json));
        var allowed = DefaultColumnKeys(reportType).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requested.Any(key => !allowed.Contains(key))) throw new ArgumentException("Column configuration contains a column unsupported by the selected report class.", nameof(json));
        return requested.Select(key => new OfficialReportColumn(key, Headings[key])).ToArray();
    }

    public static string[] ValidateColumns(OfficialReportType reportType, string? json) => ResolveColumns(reportType, json).Select(item => item.Key).ToArray();

    private static string[] PerformanceColumns() => ["indicator", "targetName", "department", "unit", "period", "targetValue", "actualPerformance", "variance", "achievementPercent", "targetAchieved", "status"];
    private static string[] ActionColumns() => ["submissionId", "indicator", "targetName", "department", "unit", "period", "action", "actor", "occurredAt", "comment", "rating"];
}

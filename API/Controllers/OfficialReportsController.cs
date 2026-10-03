using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Reporting;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/reports/official")]
[Authorize]
public sealed class OfficialReportsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IAccessControlService accessControl,
    ITenantContext tenantContext,
    IWorkflowGovernanceService governance,
    IEvidenceBlobStorage storage) : ControllerBase
{
    private sealed record StoredScope(bool Unrestricted, int[] DepartmentIds, int[] UnitIds, string[] OwnerUserIds, string[] TargetIds);
    private sealed record TargetProjection(string TargetId, string Indicator, string TargetName, string Department, string Unit, string TargetValue, bool IsWithdrawn);
    private sealed record SubmissionProjection(string TargetId, string? ActualPerformance, decimal? Variance, decimal? AchievementPercent, bool? TargetAchieved, string Status);

    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<OfficialReportTemplateResponse[]>>> Templates([FromQuery] SubmissionKind kind, [FromQuery] bool includeHistory = false)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportTemplateResponse[]>("User not found."));
        if (!await Granted(user, ReadPermission(kind))) return ForbidResponse<OfficialReportTemplateResponse[]>("Official report access is denied.");
        var query = context.OfficialReportTemplates.AsNoTracking().Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear).Where(item => item.SubmissionKind == kind);
        if (!includeHistory)
        {
            var now = DateTime.UtcNow;
            query = query.Where(item => item.IsCurrent && item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo >= now));
        }
        var items = await query.OrderBy(item => item.Code).ThenByDescending(item => item.VersionNumber).ToArrayAsync();
        return Ok(new ApiResponse<OfficialReportTemplateResponse[]>(true, items.Select(Map).ToArray()));
    }

    [HttpPost("templates")]
    public async Task<ActionResult<ApiResponse<OfficialReportTemplateResponse>>> SaveTemplate([FromBody] SaveOfficialReportTemplateRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(Fail<OfficialReportTemplateResponse>("Select a municipality context before configuring reports."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportTemplateResponse>("User not found."));
        if (!await Granted(user, ConfigurePermission(request.SubmissionKind))) return ForbidResponse<OfficialReportTemplateResponse>("Official report template configuration is denied.");
        if (request.SubmissionKind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return BadRequest(Fail<OfficialReportTemplateResponse>("Unsupported performance framework."));
        if (!Enum.IsDefined(request.Format)) return BadRequest(Fail<OfficialReportTemplateResponse>("Unsupported official report format."));
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 80 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 240) return BadRequest(Fail<OfficialReportTemplateResponse>("Code and name are required and must fit their configured limits."));
        if (!string.IsNullOrWhiteSpace(request.HeadingTemplate) && request.HeadingTemplate.Trim().Length > 500) return BadRequest(Fail<OfficialReportTemplateResponse>("Heading template cannot exceed 500 characters."));
        if (string.IsNullOrWhiteSpace(request.ApprovalReference) || string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail<OfficialReportTemplateResponse>("Approval reference and reason are required."));
        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom) return BadRequest(Fail<OfficialReportTemplateResponse>("Effective-to cannot precede effective-from."));
        string columns;
        try { columns = JsonSerializer.Serialize(OfficialReportRenderer.ValidateColumns(JsonSerializer.Serialize(request.Columns ?? []))); }
        catch (ArgumentException exception) { return BadRequest(Fail<OfficialReportTemplateResponse>(exception.Message)); }

        MunicipalityFinancialYear? year = null;
        if (request.MunicipalityFinancialYearPublicId.HasValue)
        {
            year = await context.MunicipalityFinancialYears.SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId && item.IsActive);
            if (year == null) return BadRequest(Fail<OfficialReportTemplateResponse>("Municipality financial year was not found in this tenant."));
        }

        OfficialReportTemplate? previous = null;
        var family = Guid.NewGuid();
        var version = 1;
        if (request.PreviousVersionPublicId.HasValue)
        {
            previous = await context.OfficialReportTemplates.SingleOrDefaultAsync(item => item.PublicId == request.PreviousVersionPublicId);
            if (previous == null || !previous.IsCurrent) return Conflict(Fail<OfficialReportTemplateResponse>("The selected template is no longer the current version."));
            if (previous.SubmissionKind != request.SubmissionKind) return BadRequest(Fail<OfficialReportTemplateResponse>("A template lineage cannot change performance framework."));
            if (!TryRowVersion(request.PreviousVersionRowVersion, out var rowVersion)) return BadRequest(Fail<OfficialReportTemplateResponse>("The prior template RowVersion is required."));
            context.Entry(previous).Property(item => item.RowVersion).OriginalValue = rowVersion;
            previous.IsCurrent = false;
            previous.EffectiveTo ??= request.EffectiveFrom.AddTicks(-1);
            family = previous.TemplateFamilyPublicId;
            version = previous.VersionNumber + 1;
        }
        else if (await context.OfficialReportTemplates.AnyAsync(item => item.IsCurrent && item.SubmissionKind == request.SubmissionKind && item.Code == request.Code.Trim().ToUpper() && item.MunicipalityFinancialYearId == (year == null ? null : year.Id)))
        {
            return Conflict(Fail<OfficialReportTemplateResponse>("A current template with this code and financial-year applicability already exists."));
        }

        var entity = new OfficialReportTemplate
        {
            MunicipalityId = tenantContext.MunicipalityId.Value,
            MunicipalityFinancialYearId = year?.Id,
            PreviousVersionId = previous?.Id,
            TemplateFamilyPublicId = family,
            SubmissionKind = request.SubmissionKind,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Format = request.Format,
            VersionNumber = version,
            HeadingTemplate = string.IsNullOrWhiteSpace(request.HeadingTemplate) ? "{FinancialYear} {Period} PERFORMANCE REPORT" : request.HeadingTemplate.Trim(),
            ColumnConfigurationJson = columns,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            ApprovalReference = request.ApprovalReference.Trim(),
            Reason = request.Reason.Trim(),
            CreatedByUserId = user.Id
        };
        context.OfficialReportTemplates.Add(entity);
        governance.QueueAuditTrail("OfficialReportTemplate", entity.PublicId.ToString(), previous == null ? "Create" : "CreateVersion", null, new { entity.TemplateFamilyPublicId, entity.Code, entity.Format, entity.VersionNumber, entity.ApprovalReference, entity.Reason }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<OfficialReportTemplateResponse>("The prior template changed; refresh before creating another version.")); }
        if (entity.MunicipalityFinancialYearId.HasValue) await context.Entry(entity).Reference(item => item.MunicipalityFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync();
        return Ok(new ApiResponse<OfficialReportTemplateResponse>(true, Map(entity)));
    }

    [HttpGet("generations")]
    public async Task<ActionResult<ApiResponse<OfficialReportGenerationResponse[]>>> Generations([FromQuery] SubmissionKind kind, [FromQuery] Guid? reportingPeriodPublicId = null)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportGenerationResponse[]>("User not found."));
        var scope = await accessControl.GetQueryScopeAsync(user, ReadPermission(kind));
        if (!scope.PermissionGranted) return ForbidResponse<OfficialReportGenerationResponse[]>("Official report access is denied.");
        var query = context.OfficialReportGenerations.AsNoTracking()
            .Include(item => item.ReportTemplate).Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Include(item => item.ReportingPeriod).Include(item => item.GeneratedByUser).Where(item => item.SubmissionKind == kind);
        if (reportingPeriodPublicId.HasValue) query = query.Where(item => item.ReportingPeriod.PublicId == reportingPeriodPublicId);
        var candidates = await query.OrderByDescending(item => item.GeneratedAt).Take(500).ToArrayAsync();
        return Ok(new ApiResponse<OfficialReportGenerationResponse[]>(true, candidates.Where(item => CanReadStoredScope(item.ScopeJson, scope)).Select(Map).ToArray()));
    }

    [HttpPost("generations")]
    public async Task<ActionResult<ApiResponse<OfficialReportGenerationResponse>>> Generate([FromBody] GenerateOfficialReportRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(Fail<OfficialReportGenerationResponse>("Select a municipality context before generating an official report."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<OfficialReportGenerationResponse>("User not found."));
        var template = await context.OfficialReportTemplates.Include(item => item.Municipality).SingleOrDefaultAsync(item => item.PublicId == request.TemplatePublicId && item.IsActive);
        if (template == null) return NotFound(Fail<OfficialReportGenerationResponse>("The official report template was not found."));
        var now = DateTime.UtcNow;
        if (!request.PreviousGenerationPublicId.HasValue && (!template.IsCurrent || template.EffectiveFrom > now || template.EffectiveTo.HasValue && template.EffectiveTo < now))
            return Conflict(Fail<OfficialReportGenerationResponse>("Only the current effective template can start a new official report lineage."));
        var scope = await accessControl.GetQueryScopeAsync(user, GeneratePermission(template.SubmissionKind));
        if (!scope.PermissionGranted) return ForbidResponse<OfficialReportGenerationResponse>("Official report generation is denied.");
        var year = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId && item.IsActive);
        if (year == null) return BadRequest(Fail<OfficialReportGenerationResponse>("Municipality financial year was not found in this tenant."));
        if (template.MunicipalityFinancialYearId.HasValue && template.MunicipalityFinancialYearId != year.Id) return BadRequest(Fail<OfficialReportGenerationResponse>("This template is not approved for the selected financial year."));
        var period = await context.ReportingPeriods.SingleOrDefaultAsync(item => item.PublicId == request.ReportingPeriodPublicId && item.MunicipalityFinancialYearId == year.Id && item.IsActive);
        if (period == null) return BadRequest(Fail<OfficialReportGenerationResponse>("Reporting period was not found in the selected financial year."));

        var rows = await BuildRows(template.SubmissionKind, period, scope);
        var rendered = OfficialReportRenderer.Render(new(template.Municipality.Name, year.FinancialYear.Code, period.Name, template.HeadingTemplate, template.ColumnConfigurationJson, template.Format, rows));
        var storedScope = SerializeScope(scope);
        var filterJson = JsonSerializer.Serialize(new { municipalityFinancialYearPublicId = year.PublicId, reportingPeriodPublicId = period.PublicId });
        var family = Guid.NewGuid();
        var version = 1;
        if (request.PreviousGenerationPublicId.HasValue)
        {
            var previous = await context.OfficialReportGenerations.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == request.PreviousGenerationPublicId);
            if (previous == null) return NotFound(Fail<OfficialReportGenerationResponse>("The prior official generation was not found."));
            if (previous.ReportTemplateId != template.Id || previous.MunicipalityFinancialYearId != year.Id || previous.ReportingPeriodId != period.Id || previous.ScopeJson != storedScope || previous.FilterJson != filterJson)
                return BadRequest(Fail<OfficialReportGenerationResponse>("Regeneration must retain the template, period, financial year, filters and authorization scope of its lineage."));
            family = previous.GenerationFamilyPublicId;
            version = await context.OfficialReportGenerations.Where(item => item.GenerationFamilyPublicId == family).MaxAsync(item => item.VersionNumber) + 1;
        }
        var safeCode = string.Concat(template.Code.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
        var fileName = $"{safeCode}-{year.FinancialYear.Code.Replace('/', '-')}-{period.Code.ToLowerInvariant()}-v{version}.{rendered.Extension}";
        var storageKey = Path.Combine("official-reports", tenantContext.MunicipalityId.Value.ToString(CultureInfo.InvariantCulture), family.ToString("N"), $"v{version}.{rendered.Extension}");
        var stored = await storage.StoreAsync(storageKey, rendered.Content, HttpContext.RequestAborted);
        if (!stored.Succeeded) return StatusCode(StatusCodes.Status503ServiceUnavailable, Fail<OfficialReportGenerationResponse>(stored.Detail));

        var blob = new EvidenceBlob
        {
            MunicipalityId = tenantContext.MunicipalityId.Value, StorageKey = storageKey, ContentType = rendered.ContentType,
            SizeInBytes = rendered.Content.LongLength, Sha256 = rendered.Sha256, SignatureVerified = true,
            ScanStatus = "SystemGenerated", ScannerProvider = "OfficialReportRenderer",
            ScanDetail = "Generated internally from an authorized immutable data snapshot.", ScannedAt = DateTime.UtcNow
        };
        var generation = new OfficialReportGeneration
        {
            GenerationFamilyPublicId = family, MunicipalityId = tenantContext.MunicipalityId.Value,
            MunicipalityFinancialYearId = year.Id, ReportingPeriodId = period.Id, ReportTemplateId = template.Id,
            Blob = blob, SubmissionKind = template.SubmissionKind, VersionNumber = version, ScopeJson = storedScope,
            FilterJson = filterJson, DataVersionReference = rendered.DataVersionReference, FileName = fileName,
            ContentType = rendered.ContentType, SizeInBytes = rendered.Content.LongLength, Sha256 = rendered.Sha256,
            RowCount = rows.Count, GeneratedByUserId = user.Id
        };
        context.OfficialReportGenerations.Add(generation);
        governance.QueueAuditTrail("OfficialReportGeneration", generation.PublicId.ToString(), "Generate", null, new { generation.GenerationFamilyPublicId, generation.VersionNumber, TemplatePublicId = template.PublicId, FinancialYearPublicId = year.PublicId, ReportingPeriodPublicId = period.PublicId, generation.DataVersionReference, generation.Sha256, generation.RowCount }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            await storage.DisposeAsync(storageKey, HttpContext.RequestAborted);
            return Conflict(Fail<OfficialReportGenerationResponse>("A competing generation created this version. Refresh the history before regenerating."));
        }
        generation.ReportTemplate = template; generation.MunicipalityFinancialYear = year; generation.ReportingPeriod = period; generation.GeneratedByUser = user;
        return Ok(new ApiResponse<OfficialReportGenerationResponse>(true, Map(generation)));
    }

    [HttpGet("generations/{publicId:guid}/content")]
    public async Task<IActionResult> Download(Guid publicId)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<object>("User not found."));
        var generation = await context.OfficialReportGenerations.AsNoTracking().Include(item => item.Blob).SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (generation == null) return NotFound(Fail<object>("Official report generation was not found."));
        var scope = await accessControl.GetQueryScopeAsync(user, ReadPermission(generation.SubmissionKind));
        if (!scope.PermissionGranted || !CanReadStoredScope(generation.ScopeJson, scope)) return ForbidResponse<object>("Official report download is denied for the stored generation scope.");
        if (generation.Blob.IsContentDeleted || generation.Blob.IsQuarantined) return Conflict(Fail<object>("The official report content is unavailable."));
        var content = await storage.ReadAsync(generation.Blob.StorageKey, HttpContext.RequestAborted);
        if (!content.Found) return StatusCode(content.Available ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable, Fail<object>(content.Detail));
        var actualHash = Convert.ToHexString(SHA256.HashData(content.Content)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(actualHash), Convert.FromHexString(generation.Sha256)))
        {
            governance.QueueAuditTrail("OfficialReportGeneration", generation.PublicId.ToString(), "IntegrityFailure", new { generation.Sha256 }, new { actualHash }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
            await context.SaveChangesAsync();
            return Conflict(Fail<object>("Stored report integrity verification failed."));
        }
        governance.QueueAuditTrail("OfficialReportGeneration", generation.PublicId.ToString(), "Download", null, new { generation.VersionNumber, generation.Sha256 }, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        await context.SaveChangesAsync();
        return File(content.Content, generation.ContentType, generation.FileName);
    }

    private async Task<List<OfficialPerformanceReportRow>> BuildRows(SubmissionKind kind, ReportingPeriod period, AccessQueryScopeResult scope)
    {
        if (kind == SubmissionKind.Opms)
        {
            var targetQuery = ApplyScope(context.PerformancePeriodTargets.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id && item.OpmsTargetId != null), scope, true);
            var submissionQuery = ApplyScope(context.OpmsSubmissions.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id), scope);
            var targets = await targetQuery.Select(item => new TargetProjection(item.OpmsTargetId!, item.OpmsTarget!.IndicatorNumber, item.OpmsTarget.TargetName, item.OpmsTarget.Department != null ? item.OpmsTarget.Department.Name : "", item.OpmsTarget.Unit != null ? item.OpmsTarget.Unit.Name : "", item.TargetValue, item.OpmsTarget.IsWithdrawn)).ToArrayAsync();
            var submissions = await submissionQuery.Select(item => new SubmissionProjection(item.OpmsTargetId, item.ActualPerformance, item.Variance, item.AchievementPercent, item.TargetAchieved, item.Status)).ToArrayAsync();
            return Merge(targets, submissions, period);
        }
        var ipmsTargets = ApplyScope(context.PerformancePeriodTargets.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id && item.IpmsTargetId != null), scope, false);
        var ipmsSubmissions = ApplyScope(context.IpmsSubmissions.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id), scope);
        var targetRows = await ipmsTargets.Select(item => new TargetProjection(item.IpmsTargetId!, item.IpmsTarget!.IndicatorNumber, item.IpmsTarget.TargetName, item.IpmsTarget.Department != null ? item.IpmsTarget.Department.Name : "", item.IpmsTarget.Unit != null ? item.IpmsTarget.Unit.Name : "", item.TargetValue, item.IpmsTarget.IsWithdrawn)).ToArrayAsync();
        var submissionRows = await ipmsSubmissions.Select(item => new SubmissionProjection(item.IpmsTargetId, item.ActualPerformance, item.Variance, item.AchievementPercent, item.TargetAchieved, item.Status)).ToArrayAsync();
        return Merge(targetRows, submissionRows, period);
    }

    private static List<OfficialPerformanceReportRow> Merge(TargetProjection[] targets, SubmissionProjection[] submissions, ReportingPeriod period)
    {
        var submitted = submissions.ToDictionary(item => item.TargetId, StringComparer.Ordinal);
        var withdrawalApplies = period.PeriodType is ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4 or ReportingPeriodType.Annual;
        return targets.OrderBy(item => item.Indicator, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.TargetId, StringComparer.Ordinal).Select(target =>
        {
            submitted.TryGetValue(target.TargetId, out var submission);
            var withdrawn = target.IsWithdrawn && withdrawalApplies;
            return new OfficialPerformanceReportRow(target.Indicator, target.TargetName, target.Department, target.Unit, period.Code, target.TargetValue,
                withdrawn ? "" : submission?.ActualPerformance ?? "", withdrawn ? "" : Format(submission?.Variance), withdrawn ? "" : Format(submission?.AchievementPercent),
                withdrawn ? "" : submission?.TargetAchieved?.ToString() ?? "", withdrawn ? "Withdrawn" : submission?.Status ?? "Not submitted");
        }).ToList();
    }

    private static string Format(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static IQueryable<PerformancePeriodTarget> ApplyScope(IQueryable<PerformancePeriodTarget> query, AccessQueryScopeResult scope, bool opms) => scope.Unrestricted ? query : opms
        ? query.Where(item => item.OpmsTarget != null && (scope.DepartmentIds.Contains(item.OpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(item.OpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(item.OpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(item.OpmsTargetId!)))
        : query.Where(item => item.IpmsTarget != null && (scope.DepartmentIds.Contains(item.IpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(item.IpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(item.IpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(item.IpmsTargetId!)));
    private static IQueryable<OpmsSubmission> ApplyScope(IQueryable<OpmsSubmission> query, AccessQueryScopeResult scope) => scope.Unrestricted ? query : query.Where(item => scope.DepartmentIds.Contains(item.OpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(item.OpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(item.OpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(item.OpmsTargetId));
    private static IQueryable<IpmsSubmission> ApplyScope(IQueryable<IpmsSubmission> query, AccessQueryScopeResult scope) => scope.Unrestricted ? query : query.Where(item => scope.DepartmentIds.Contains(item.IpmsTarget.DepartmentId ?? -1) || scope.UnitIds.Contains(item.IpmsTarget.UnitId ?? -1) || scope.OwnerUserIds.Contains(item.IpmsTarget.AssignedUserId!) || scope.TargetIds.Contains(item.IpmsTargetId));
    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private async Task<bool> Granted(ApplicationUser user, string permission) => (await accessControl.GetQueryScopeAsync(user, permission)).PermissionGranted;
    private static string ReadPermission(SubmissionKind kind) => kind == SubmissionKind.Opms ? "OPMS_REPORT.READ" : "IPMS_REPORT.READ";
    private static string GeneratePermission(SubmissionKind kind) => kind == SubmissionKind.Opms ? "OPMS_REPORT.GENERATE" : "IPMS_REPORT.GENERATE";
    private static string ConfigurePermission(SubmissionKind kind) => kind == SubmissionKind.Opms ? "OPMS_REPORT.CONFIGURE" : "IPMS_REPORT.CONFIGURE";
    private static string SerializeScope(AccessQueryScopeResult scope) => JsonSerializer.Serialize(new StoredScope(scope.Unrestricted, scope.DepartmentIds.Order().ToArray(), scope.UnitIds.Order().ToArray(), scope.OwnerUserIds.Order(StringComparer.Ordinal).ToArray(), scope.TargetIds.Order(StringComparer.Ordinal).ToArray()));
    private static bool CanReadStoredScope(string json, AccessQueryScopeResult current)
    {
        StoredScope? stored;
        try { stored = JsonSerializer.Deserialize<StoredScope>(json); } catch (JsonException) { return false; }
        if (stored == null) return false;
        if (current.Unrestricted) return true;
        if (stored.Unrestricted) return false;
        return stored.DepartmentIds.All(current.DepartmentIds.Contains) && stored.UnitIds.All(current.UnitIds.Contains) && stored.OwnerUserIds.All(current.OwnerUserIds.Contains) && stored.TargetIds.All(current.TargetIds.Contains);
    }
    private static bool TryRowVersion(string? value, out byte[] bytes) { try { bytes = Convert.FromBase64String(value ?? ""); return bytes.Length > 0; } catch (FormatException) { bytes = []; return false; } }
    private static OfficialReportTemplateResponse Map(OfficialReportTemplate item) => new(item.PublicId, item.TemplateFamilyPublicId, item.MunicipalityFinancialYear?.PublicId, item.MunicipalityFinancialYear?.FinancialYear.Code, item.SubmissionKind, item.Code, item.Name, item.Format, item.VersionNumber, item.HeadingTemplate, OfficialReportRenderer.ValidateColumns(item.ColumnConfigurationJson), item.IsCurrent, item.IsActive, item.EffectiveFrom, item.EffectiveTo, item.ApprovalReference, item.Reason, item.CreatedAt, Convert.ToBase64String(item.RowVersion));
    private static OfficialReportGenerationResponse Map(OfficialReportGeneration item) => new(item.PublicId, item.GenerationFamilyPublicId, item.VersionNumber, item.ReportTemplate.PublicId, item.ReportTemplate.Code, item.ReportTemplate.Name, item.ReportTemplate.VersionNumber, item.ReportTemplate.Format, item.SubmissionKind, item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code, item.ReportingPeriod.PublicId, item.ReportingPeriod.Code, item.ScopeJson, item.FilterJson, item.DataVersionReference, item.FileName, item.ContentType, item.SizeInBytes, item.Sha256, item.RowCount, item.GeneratedByUser.UserName ?? item.GeneratedByUser.Email ?? item.GeneratedByUser.Id, item.GeneratedAt, $"/api/v1/reports/official/generations/{item.PublicId}/content");
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ObjectResult ForbidResponse<T>(string message) => StatusCode(StatusCodes.Status403Forbidden, Fail<T>(message));
}

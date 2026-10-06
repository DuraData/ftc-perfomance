using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Reporting;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
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
    private sealed record SubmissionSubject(string SubmissionId, string TargetId, string Indicator, string TargetName, int? DepartmentId, string Department, int? UnitId, string Unit, string Period, string? ActualPerformance, decimal? AchievementPercent, bool? TargetAchieved, string Status, string SubmittedBy, DateTime? SubmittedAt);

    [HttpGet("templates")]
    public ActionResult<ApiResponse<OfficialReportTemplateResponse[]>> Templates([FromQuery] SubmissionKind kind, [FromQuery] bool includeHistory = false)
    {
        _ = kind;
        _ = includeHistory;
        return StatusCode(StatusCodes.Status410Gone, Fail<OfficialReportTemplateResponse[]>(
            "This fixed-limit route is retired. Use /api/v1/reports/official/templates/page."));
    }

    [HttpGet("templates/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<OfficialReportTemplateResponse>>>> TemplatesPage(
        [FromQuery] SubmissionKind kind,
        [FromQuery] bool includeHistory,
        [FromQuery] Guid? municipalityFinancialYearPublicId,
        [FromQuery] PagedQueryRequest request)
    {
        if (request.NormalizedSortBy is not ("code" or "name" or "reporttype" or "versionnumber" or "effectivefrom" or "createdat"))
            return BadRequest(Fail<PagedResponse<OfficialReportTemplateResponse>>(
                "SortBy must be code, name, reportType, versionNumber, effectiveFrom, or createdAt."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PagedResponse<OfficialReportTemplateResponse>>("User not found."));
        if (!await Granted(user, ReadPermission(kind)))
            return ForbidResponse<PagedResponse<OfficialReportTemplateResponse>>("Official report access is denied.");

        var query = context.OfficialReportTemplates.AsNoTracking().Where(item => item.SubmissionKind == kind);
        if (!includeHistory)
        {
            var now = DateTime.UtcNow;
            query = query.Where(item => item.IsCurrent && item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo >= now));
        }
        if (municipalityFinancialYearPublicId.HasValue)
            query = query.Where(item => !item.MunicipalityFinancialYearId.HasValue || item.MunicipalityFinancialYear!.PublicId == municipalityFinancialYearPublicId);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(search) || item.Name.Contains(search) || item.ApprovalReference.Contains(search)
                || item.MunicipalityFinancialYear != null && item.MunicipalityFinancialYear.FinancialYear.Code.Contains(search));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            ("reporttype", false) => query.OrderBy(item => item.ReportType).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("reporttype", true) => query.OrderByDescending(item => item.ReportType).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("versionnumber", false) => query.OrderBy(item => item.VersionNumber).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("versionnumber", true) => query.OrderByDescending(item => item.VersionNumber).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id),
            ("createdat", false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            ("createdat", true) => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id),
            (_, true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.VersionNumber).ThenByDescending(item => item.Id),
            _ => query.OrderBy(item => item.Code).ThenByDescending(item => item.VersionNumber).ThenBy(item => item.Id)
        };
        var items = await query.Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<OfficialReportTemplateResponse>>(true,
            PagedResponse<OfficialReportTemplateResponse>.Create(items.Select(Map), request.Page, request.PageSize, totalCount)));
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
        if (!Enum.IsDefined(request.ReportType)) return BadRequest(Fail<OfficialReportTemplateResponse>("Unsupported official report class."));
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 80 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 240) return BadRequest(Fail<OfficialReportTemplateResponse>("Code and name are required and must fit their configured limits."));
        if (!string.IsNullOrWhiteSpace(request.HeadingTemplate) && request.HeadingTemplate.Trim().Length > 500) return BadRequest(Fail<OfficialReportTemplateResponse>("Heading template cannot exceed 500 characters."));
        if (string.IsNullOrWhiteSpace(request.ApprovalReference) || string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail<OfficialReportTemplateResponse>("Approval reference and reason are required."));
        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom) return BadRequest(Fail<OfficialReportTemplateResponse>("Effective-to cannot precede effective-from."));
        string columns;
        try { columns = JsonSerializer.Serialize(OfficialReportCatalog.ValidateColumns(request.ReportType, JsonSerializer.Serialize(request.Columns ?? []))); }
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
            if (previous.ReportType != request.ReportType) return BadRequest(Fail<OfficialReportTemplateResponse>("A template lineage cannot change report class."));
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
            ReportType = request.ReportType,
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
    public ActionResult<ApiResponse<OfficialReportGenerationResponse[]>> Generations([FromQuery] SubmissionKind kind, [FromQuery] Guid? reportingPeriodPublicId = null)
    {
        _ = kind;
        _ = reportingPeriodPublicId;
        return StatusCode(StatusCodes.Status410Gone, Fail<OfficialReportGenerationResponse[]>(
            "This fixed-limit route is retired. Use /api/v1/reports/official/generations/page."));
    }

    [HttpGet("generations/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<OfficialReportGenerationResponse>>>> GenerationsPage(
        [FromQuery] SubmissionKind kind,
        [FromQuery] Guid? reportingPeriodPublicId,
        [FromQuery] PagedQueryRequest request)
    {
        if (request.NormalizedSortBy is not ("generatedat" or "templatename" or "versionnumber" or "rowcount" or "financialyear" or "reportingperiod"))
            return BadRequest(Fail<PagedResponse<OfficialReportGenerationResponse>>(
                "SortBy must be generatedAt, templateName, versionNumber, rowCount, financialYear, or reportingPeriod."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PagedResponse<OfficialReportGenerationResponse>>("User not found."));
        var scope = IntersectScopes(
            IntersectScopes(await accessControl.GetQueryScopeAsync(user, ReadPermission(kind)),
                await accessControl.GetQueryScopeAsync(user, kind == SubmissionKind.Opms ? "OPMS_KPI.READ" : "IPMS_KPI.READ")),
            await accessControl.GetQueryScopeAsync(user, kind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ"));
        if (!scope.PermissionGranted)
            return ForbidResponse<PagedResponse<OfficialReportGenerationResponse>>("Official report history requires report, KPI and submission READ permission.");

        var canReadAuditTrail = await Granted(user, "Audit.Trails.View");
        var query = ApplyStoredScope(context.OfficialReportGenerations.AsNoTracking()
            .Where(item => item.SubmissionKind == kind && (item.ReportType != OfficialReportType.AuditTrail || canReadAuditTrail)), scope);
        if (reportingPeriodPublicId.HasValue) query = query.Where(item => item.ReportingPeriod.PublicId == reportingPeriodPublicId);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(item =>
                item.ReportTemplate.Code.Contains(search) ||
                item.ReportTemplate.Name.Contains(search) ||
                item.MunicipalityFinancialYear.FinancialYear.Code.Contains(search) ||
                item.ReportingPeriod.Code.Contains(search) ||
                item.FileName.Contains(search) ||
                (item.GeneratedByUser.UserName != null && item.GeneratedByUser.UserName.Contains(search)) ||
                (item.GeneratedByUser.Email != null && item.GeneratedByUser.Email.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("templatename", false) => query.OrderBy(item => item.ReportTemplate.Name).ThenBy(item => item.Id),
            ("templatename", true) => query.OrderByDescending(item => item.ReportTemplate.Name).ThenByDescending(item => item.Id),
            ("versionnumber", false) => query.OrderBy(item => item.VersionNumber).ThenBy(item => item.Id),
            ("versionnumber", true) => query.OrderByDescending(item => item.VersionNumber).ThenByDescending(item => item.Id),
            ("rowcount", false) => query.OrderBy(item => item.RowCount).ThenBy(item => item.Id),
            ("rowcount", true) => query.OrderByDescending(item => item.RowCount).ThenByDescending(item => item.Id),
            ("financialyear", false) => query.OrderBy(item => item.MunicipalityFinancialYear.FinancialYear.Code).ThenBy(item => item.Id),
            ("financialyear", true) => query.OrderByDescending(item => item.MunicipalityFinancialYear.FinancialYear.Code).ThenByDescending(item => item.Id),
            ("reportingperiod", false) => query.OrderBy(item => item.ReportingPeriod.Code).ThenBy(item => item.Id),
            ("reportingperiod", true) => query.OrderByDescending(item => item.ReportingPeriod.Code).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.GeneratedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.GeneratedAt).ThenByDescending(item => item.Id)
        };
        var items = await query.Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.ReportTemplate).Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Include(item => item.ReportingPeriod).Include(item => item.GeneratedByUser).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<OfficialReportGenerationResponse>>(true,
            PagedResponse<OfficialReportGenerationResponse>.Create(items.Select(Map), request.Page, request.PageSize, totalCount)));
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
        var reportScope = await accessControl.GetQueryScopeAsync(user, GeneratePermission(template.SubmissionKind));
        if (!reportScope.PermissionGranted) return ForbidResponse<OfficialReportGenerationResponse>("Official report generation is denied.");
        var kpiScope = await accessControl.GetQueryScopeAsync(user, template.SubmissionKind == SubmissionKind.Opms ? "OPMS_KPI.READ" : "IPMS_KPI.READ");
        var submissionScope = await accessControl.GetQueryScopeAsync(user, template.SubmissionKind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ");
        var scope = IntersectScopes(IntersectScopes(reportScope, kpiScope), submissionScope);
        if (!scope.PermissionGranted) return ForbidResponse<OfficialReportGenerationResponse>("The report requires both KPI and submission READ permission in addition to report generation permission.");
        if (template.ReportType == OfficialReportType.AuditTrail && !await Granted(user, "Audit.Trails.View"))
            return ForbidResponse<OfficialReportGenerationResponse>("The audit-trail report also requires audit-trail read permission.");
        var year = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId && item.IsActive);
        if (year == null) return BadRequest(Fail<OfficialReportGenerationResponse>("Municipality financial year was not found in this tenant."));
        if (template.MunicipalityFinancialYearId.HasValue && template.MunicipalityFinancialYearId != year.Id) return BadRequest(Fail<OfficialReportGenerationResponse>("This template is not approved for the selected financial year."));
        var period = await context.ReportingPeriods.SingleOrDefaultAsync(item => item.PublicId == request.ReportingPeriodPublicId && item.MunicipalityFinancialYearId == year.Id && item.IsActive);
        if (period == null) return BadRequest(Fail<OfficialReportGenerationResponse>("Reporting period was not found in the selected financial year."));
        var periodError = ValidatePeriod(template.ReportType, period.PeriodType);
        if (periodError != null) return BadRequest(Fail<OfficialReportGenerationResponse>(periodError));
        int? departmentId = null;
        int? unitId = null;
        if (request.DepartmentPublicId.HasValue)
        {
            departmentId = await context.Departments.Where(item => item.PublicId == request.DepartmentPublicId && item.IsActive).Select(item => (int?)item.Id).SingleOrDefaultAsync();
            if (!departmentId.HasValue) return BadRequest(Fail<OfficialReportGenerationResponse>("The selected department was not found in this tenant."));
        }
        if (request.UnitPublicId.HasValue)
        {
            unitId = await context.Units.Where(item => item.PublicId == request.UnitPublicId && item.IsActive).Select(item => (int?)item.Id).SingleOrDefaultAsync();
            if (!unitId.HasValue) return BadRequest(Fail<OfficialReportGenerationResponse>("The selected unit was not found in this tenant."));
        }
        if (template.ReportType == OfficialReportType.DepartmentalPerformance && !departmentId.HasValue)
            return BadRequest(Fail<OfficialReportGenerationResponse>("A department filter is required for a departmental report."));
        if (template.ReportType == OfficialReportType.UnitPerformance && !unitId.HasValue)
            return BadRequest(Fail<OfficialReportGenerationResponse>("A unit filter is required for a unit report."));

        var rows = await BuildRows(template.SubmissionKind, template.ReportType, period, scope, departmentId, unitId);
        var rendered = OfficialReportRenderer.RenderTabular(new(template.Municipality.Name, year.FinancialYear.Code, period.Name, template.HeadingTemplate, template.ColumnConfigurationJson, template.Format, template.ReportType, rows));
        var storedScope = SerializeScope(scope);
        var filterJson = JsonSerializer.Serialize(new { municipalityFinancialYearPublicId = year.PublicId, reportingPeriodPublicId = period.PublicId, reportType = template.ReportType, request.DepartmentPublicId, request.UnitPublicId });
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
            ScopeSchemaVersion = 1, ScopeIsUnrestricted = scope.Unrestricted, ScopeGrants = CreateScopeGrants(tenantContext.MunicipalityId.Value, scope),
            ReportType = template.ReportType,
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
        var scope = IntersectScopes(
            IntersectScopes(await accessControl.GetQueryScopeAsync(user, ReadPermission(generation.SubmissionKind)),
                await accessControl.GetQueryScopeAsync(user, generation.SubmissionKind == SubmissionKind.Opms ? "OPMS_KPI.READ" : "IPMS_KPI.READ")),
            await accessControl.GetQueryScopeAsync(user, generation.SubmissionKind == SubmissionKind.Opms ? "OPMS_SUBMISSION.READ" : "IPMS_SUBMISSION.READ"));
        if (!scope.PermissionGranted || generation.ScopeSchemaVersion != 1 || !CanReadStoredScope(generation.ScopeJson, scope)) return ForbidResponse<object>("Official report download is denied for the stored generation scope.");
        if (generation.ReportType == OfficialReportType.AuditTrail && !await Granted(user, "Audit.Trails.View")) return ForbidResponse<object>("Audit-trail read permission has been revoked.");
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

    private async Task<List<OfficialReportDataRow>> BuildRows(SubmissionKind kind, OfficialReportType reportType, ReportingPeriod period, AccessQueryScopeResult scope, int? departmentId, int? unitId)
    {
        if (reportType is OfficialReportType.QuarterlyPerformance or OfficialReportType.MidTermPerformance or OfficialReportType.AnnualPerformance or OfficialReportType.DepartmentalPerformance or OfficialReportType.UnitPerformance)
            return (await BuildPerformanceRows(kind, period, scope, departmentId, unitId)).Select(PerformanceRow).ToList();

        if (reportType == OfficialReportType.PerformanceSummary)
        {
            var performance = await BuildPerformanceRows(kind, period, scope, departmentId, unitId);
            return performance.GroupBy(item => string.IsNullOrWhiteSpace(item.Department) ? "Unassigned" : item.Department, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => Row(
                    ("group", group.Key), ("configuredTargets", group.Count().ToString(CultureInfo.InvariantCulture)),
                    ("submissions", group.Count(item => item.Status != "Not submitted" && item.Status != "Withdrawn").ToString(CultureInfo.InvariantCulture)),
                    ("achieved", group.Count(item => bool.TryParse(item.TargetAchieved, out var achieved) && achieved).ToString(CultureInfo.InvariantCulture)),
                    ("atRisk", group.Count(item => bool.TryParse(item.TargetAchieved, out var achieved) && !achieved).ToString(CultureInfo.InvariantCulture)),
                    ("pending", group.Count(item => string.IsNullOrWhiteSpace(item.TargetAchieved)).ToString(CultureInfo.InvariantCulture)),
                    ("averageAchievementPercent", Format(group.Select(item => decimal.TryParse(item.AchievementPercent, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? (decimal?)value : null).Where(item => item.HasValue).Select(item => item!.Value).DefaultIfEmpty().Average()))))
                .ToList();
        }

        var subjects = await BuildSubmissionSubjects(kind, period, scope, departmentId, unitId);
        var subjectIds = subjects.Select(item => item.SubmissionId).ToArray();
        var bySubmission = subjects.ToDictionary(item => item.SubmissionId, StringComparer.Ordinal);
        if (reportType == OfficialReportType.SubmissionRegister) return subjects.Select(SubmissionRow).ToList();

        if (reportType == OfficialReportType.WorkflowStatus)
        {
            var instances = await context.SubmissionWorkflowInstances.AsNoTracking()
                .Include(item => item.WorkflowDefinition).Include(item => item.CurrentStage)
                .Where(item => item.SubmissionKind == kind && subjectIds.Contains(item.SubmissionId))
                .OrderBy(item => item.StartedAt).ToArrayAsync();
            return instances.Select(item =>
            {
                var subject = bySubmission[item.SubmissionId];
                return SubjectRow(subject, ("workflow", $"{item.WorkflowDefinition.Name} v{item.WorkflowDefinition.Version}"), ("status", item.State.ToString()),
                    ("currentStage", item.CurrentStage?.Name ?? "Complete"), ("startedAt", Date(item.StartedAt)), ("completedAt", Date(item.CompletedAt)));
            }).ToList();
        }

        if (reportType is OfficialReportType.VerificationRegister or OfficialReportType.ApprovalRegister or OfficialReportType.PmsReview)
        {
            var actions = await context.SubmissionWorkflowActions.AsNoTracking().Include(item => item.SubmissionWorkflowInstance).Include(item => item.ActorUser)
                .Where(item => item.SubmissionWorkflowInstance.SubmissionKind == kind && subjectIds.Contains(item.SubmissionWorkflowInstance.SubmissionId))
                .OrderBy(item => item.OccurredAt).ToArrayAsync();
            actions = actions.Where(item => ActionBelongsTo(reportType, item.ActionCode)).ToArray();
            return actions.Select(item =>
            {
                var subject = bySubmission[item.SubmissionWorkflowInstance.SubmissionId];
                return SubjectRow(subject, ("action", item.ActionCode), ("actor", UserName(item.ActorUser)), ("occurredAt", Date(item.OccurredAt)),
                    ("comment", item.Comment ?? string.Empty), ("rating", Format(item.RatingValue)));
            }).ToList();
        }

        if (reportType == OfficialReportType.InternalAudit)
        {
            var assessments = await context.InternalAuditAssessments.AsNoTracking().Include(item => item.SubmissionWorkflowInstance).Include(item => item.AssessedByUser)
                .Where(item => item.SubmissionWorkflowInstance.SubmissionKind == kind && subjectIds.Contains(item.SubmissionWorkflowInstance.SubmissionId))
                .OrderBy(item => item.AssessedAt).ToArrayAsync();
            return assessments.Select(item =>
            {
                var subject = bySubmission[item.SubmissionWorkflowInstance.SubmissionId];
                return SubjectRow(subject, ("outcome", item.Outcome.ToString()), ("observation", item.DetailedObservation), ("findings", item.Findings ?? string.Empty),
                    ("recommendation", item.Recommendation ?? string.Empty), ("score", Format(item.Score)), ("actor", UserName(item.AssessedByUser)), ("occurredAt", Date(item.AssessedAt)));
            }).ToList();
        }

        if (reportType == OfficialReportType.OutstandingRfi)
        {
            var rfis = await context.PerformanceRfis.AsNoTracking().Include(item => item.SubmissionWorkflowInstance)
                .Where(item => item.SubmissionWorkflowInstance.SubmissionKind == kind && subjectIds.Contains(item.SubmissionWorkflowInstance.SubmissionId) && !item.ClosedAt.HasValue)
                .OrderBy(item => item.ResponseDueAt).ToArrayAsync();
            return rfis.Select(item =>
            {
                var subject = bySubmission[item.SubmissionWorkflowInstance.SubmissionId];
                var status = item.ClosedAt.HasValue ? "Closed" : item.RespondedAt.HasValue ? "Responded" : item.ResponseDueAt < DateTime.UtcNow ? "Overdue" : "Outstanding";
                return SubjectRow(subject, ("rfiId", item.PublicId.ToString()), ("question", item.Question), ("status", status), ("raisedBy", item.RaisedByUserId),
                    ("raisedAt", Date(item.RaisedAt)), ("responseDueAt", Date(item.ResponseDueAt)), ("response", item.Response ?? string.Empty),
                    ("respondedBy", item.RespondedByUserId ?? string.Empty), ("respondedAt", Date(item.RespondedAt)));
            }).ToList();
        }

        if (reportType == OfficialReportType.EvidenceRegister)
        {
            var evidence = await context.PoeFiles.AsNoTracking().Include(item => item.Blob).Include(item => item.UploadedByUser)
                .Where(item => item.SubmissionKind == kind && subjectIds.Contains(item.SubmissionId)).OrderBy(item => item.UploadedAt).ToArrayAsync();
            return evidence.Select(item => SubjectRow(bySubmission[item.SubmissionId], ("evidenceId", item.PublicId.ToString()), ("fileName", item.FileName),
                ("contentType", item.Blob.ContentType ?? string.Empty), ("sizeInBytes", item.Blob.SizeInBytes.ToString(CultureInfo.InvariantCulture)), ("sha256", item.Blob.Sha256),
                ("scanStatus", item.Blob.IsQuarantined ? $"Quarantined ({item.Blob.ScanStatus})" : item.Blob.ScanStatus), ("uploadedBy", UserName(item.UploadedByUser)),
                ("uploadedAt", Date(item.UploadedAt)), ("retainUntil", Date(item.RetainUntil)))).ToList();
        }

        if (reportType == OfficialReportType.AuditTrail)
        {
            var entityIds = subjectIds.Concat(subjects.Select(item => item.TargetId)).Distinct(StringComparer.Ordinal).ToArray();
            return (await context.AuditTrails.AsNoTracking().Where(item => entityIds.Contains(item.EntityId)).OrderBy(item => item.ChangedAt).ToArrayAsync())
                .Select(item => Row(("entityName", item.EntityName), ("entityId", item.EntityId), ("action", item.Action), ("changedBy", item.ChangedBy),
                    ("changedAt", Date(item.ChangedAt)), ("reason", item.Reason ?? string.Empty), ("correlationId", item.CorrelationId ?? string.Empty))).ToList();
        }

        if (reportType == OfficialReportType.VersionTrail)
        {
            var targetIds = subjects.Select(item => item.TargetId).Distinct(StringComparer.Ordinal).ToArray();
            var rows = new List<OfficialReportDataRow>();
            var fields = await context.KpiFieldRevisions.AsNoTracking().Include(item => item.RevisedByUser)
                .Where(item => kind == SubmissionKind.Opms ? item.OpmsTargetId != null && targetIds.Contains(item.OpmsTargetId) : item.IpmsTargetId != null && targetIds.Contains(item.IpmsTargetId))
                .OrderBy(item => item.RecordedAt).ToArrayAsync();
            rows.AddRange(fields.Select(item => Row(("source", "KPI field revision"), ("entityId", item.OpmsTargetId ?? item.IpmsTargetId ?? string.Empty), ("field", item.FieldName),
                ("originalValue", item.OriginalValue ?? string.Empty), ("revisedValue", item.RevisedValue ?? string.Empty), ("versionNumber", string.Empty),
                ("actor", UserName(item.RevisedByUser)), ("effectiveAt", Date(item.EffectiveAt)), ("reason", item.Reason), ("approvalReference", item.ApprovalReference))));
            var periodRevisions = await context.PerformanceTargetRevisions.AsNoTracking().Include(item => item.PerformancePeriodTarget).Include(item => item.RevisedByUser)
                .Where(item => item.PerformancePeriodTarget.ReportingPeriodId == period.Id && (kind == SubmissionKind.Opms ? item.PerformancePeriodTarget.OpmsTargetId != null && targetIds.Contains(item.PerformancePeriodTarget.OpmsTargetId) : item.PerformancePeriodTarget.IpmsTargetId != null && targetIds.Contains(item.PerformancePeriodTarget.IpmsTargetId)))
                .OrderBy(item => item.RecordedAt).ToArrayAsync();
            rows.AddRange(periodRevisions.Select(item => Row(("source", "Period value revision"), ("entityId", item.PerformancePeriodTarget.OpmsTargetId ?? item.PerformancePeriodTarget.IpmsTargetId ?? string.Empty),
                ("field", item.FieldName), ("originalValue", item.OriginalValue ?? string.Empty), ("revisedValue", item.RevisedValue ?? string.Empty), ("versionNumber", string.Empty),
                ("actor", UserName(item.RevisedByUser)), ("effectiveAt", Date(item.EffectiveAt)), ("reason", item.Reason), ("approvalReference", item.ApprovalReference))));
            return rows;
        }

        throw new ArgumentOutOfRangeException(nameof(reportType));
    }

    private async Task<List<OfficialPerformanceReportRow>> BuildPerformanceRows(SubmissionKind kind, ReportingPeriod period, AccessQueryScopeResult scope, int? departmentId, int? unitId)
    {
        var useRevised = PerformanceRevisionResolver.UsesRevisedValues(period.PeriodType);
        if (kind == SubmissionKind.Opms)
        {
            var targetQuery = ApplyScope(context.PerformancePeriodTargets.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id && item.OpmsTargetId != null), scope, true);
            var submissionQuery = ApplyScope(context.OpmsSubmissions.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id), scope);
            if (departmentId.HasValue) { targetQuery = targetQuery.Where(item => item.OpmsTarget!.DepartmentId == departmentId); submissionQuery = submissionQuery.Where(item => item.OpmsTarget.DepartmentId == departmentId); }
            if (unitId.HasValue) { targetQuery = targetQuery.Where(item => item.OpmsTarget!.UnitId == unitId); submissionQuery = submissionQuery.Where(item => item.OpmsTarget.UnitId == unitId); }
            var targets = await targetQuery.Select(item => new TargetProjection(item.OpmsTargetId!,
                useRevised && item.OpmsTarget!.IsIndicatorNumberRevised && item.OpmsTarget.RevisedIndicatorNumber != null ? item.OpmsTarget.RevisedIndicatorNumber : item.OpmsTarget!.IndicatorNumber,
                useRevised && item.OpmsTarget!.IsTargetNameRevised && item.OpmsTarget.RevisedTargetName != null ? item.OpmsTarget.RevisedTargetName : item.OpmsTarget!.TargetName,
                item.OpmsTarget!.Department != null ? item.OpmsTarget.Department.Name : "", item.OpmsTarget.Unit != null ? item.OpmsTarget.Unit.Name : "",
                useRevised && item.IsTargetRevised && item.RevisedTargetValue != null ? item.RevisedTargetValue : item.TargetValue,
                item.OpmsTarget.IsWithdrawn)).ToArrayAsync();
            var submissions = await submissionQuery.Select(item => new SubmissionProjection(item.OpmsTargetId, item.ActualPerformance, item.Variance, item.AchievementPercent, item.TargetAchieved, item.Status)).ToArrayAsync();
            return Merge(targets, submissions, period);
        }
        var ipmsTargets = ApplyScope(context.PerformancePeriodTargets.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id && item.IpmsTargetId != null), scope, false);
        var ipmsSubmissions = ApplyScope(context.IpmsSubmissions.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id), scope);
        if (departmentId.HasValue) { ipmsTargets = ipmsTargets.Where(item => item.IpmsTarget!.DepartmentId == departmentId); ipmsSubmissions = ipmsSubmissions.Where(item => item.IpmsTarget.DepartmentId == departmentId); }
        if (unitId.HasValue) { ipmsTargets = ipmsTargets.Where(item => item.IpmsTarget!.UnitId == unitId); ipmsSubmissions = ipmsSubmissions.Where(item => item.IpmsTarget.UnitId == unitId); }
        var targetRows = await ipmsTargets.Select(item => new TargetProjection(item.IpmsTargetId!,
            useRevised && item.IpmsTarget!.IsIndicatorNumberRevised && item.IpmsTarget.RevisedIndicatorNumber != null ? item.IpmsTarget.RevisedIndicatorNumber : item.IpmsTarget!.IndicatorNumber,
            useRevised && item.IpmsTarget!.IsTargetNameRevised && item.IpmsTarget.RevisedTargetName != null ? item.IpmsTarget.RevisedTargetName : item.IpmsTarget!.TargetName,
            item.IpmsTarget!.Department != null ? item.IpmsTarget.Department.Name : "", item.IpmsTarget.Unit != null ? item.IpmsTarget.Unit.Name : "",
            useRevised && item.IsTargetRevised && item.RevisedTargetValue != null ? item.RevisedTargetValue : item.TargetValue,
            item.IpmsTarget.IsWithdrawn)).ToArrayAsync();
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

    private async Task<SubmissionSubject[]> BuildSubmissionSubjects(SubmissionKind kind, ReportingPeriod period, AccessQueryScopeResult scope, int? departmentId, int? unitId)
    {
        var useRevised = PerformanceRevisionResolver.UsesRevisedValues(period.PeriodType);
        if (kind == SubmissionKind.Opms)
        {
            var query = ApplyScope(context.OpmsSubmissions.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id), scope);
            if (departmentId.HasValue) query = query.Where(item => item.OpmsTarget.DepartmentId == departmentId);
            if (unitId.HasValue) query = query.Where(item => item.OpmsTarget.UnitId == unitId);
            return await query.OrderBy(item => item.OpmsTarget.OriginalOrderNumber).ThenBy(item => item.PublicId).Select(item => new SubmissionSubject(
                item.Id, item.OpmsTargetId,
                useRevised && item.OpmsTarget.IsIndicatorNumberRevised && item.OpmsTarget.RevisedIndicatorNumber != null ? item.OpmsTarget.RevisedIndicatorNumber : item.OpmsTarget.IndicatorNumber,
                useRevised && item.OpmsTarget.IsTargetNameRevised && item.OpmsTarget.RevisedTargetName != null ? item.OpmsTarget.RevisedTargetName : item.OpmsTarget.TargetName,
                item.OpmsTarget.DepartmentId, item.OpmsTarget.Department != null ? item.OpmsTarget.Department.Name : string.Empty,
                item.OpmsTarget.UnitId, item.OpmsTarget.Unit != null ? item.OpmsTarget.Unit.Name : string.Empty, period.Code,
                item.ActualPerformance, item.AchievementPercent, item.TargetAchieved, item.Status,
                item.SubmittedByUser != null ? item.SubmittedByUser.UserName ?? item.SubmittedByUser.Email ?? item.SubmittedByUser.Id : item.SubmittedByUserId ?? string.Empty,
                item.SubmittedAt)).ToArrayAsync();
        }
        var ipms = ApplyScope(context.IpmsSubmissions.AsNoTracking().Where(item => item.ReportingPeriodId == period.Id), scope);
        if (departmentId.HasValue) ipms = ipms.Where(item => item.IpmsTarget.DepartmentId == departmentId);
        if (unitId.HasValue) ipms = ipms.Where(item => item.IpmsTarget.UnitId == unitId);
        return await ipms.OrderBy(item => item.IpmsTarget.OriginalOrderNumber).ThenBy(item => item.PublicId).Select(item => new SubmissionSubject(
            item.Id, item.IpmsTargetId,
            useRevised && item.IpmsTarget.IsIndicatorNumberRevised && item.IpmsTarget.RevisedIndicatorNumber != null ? item.IpmsTarget.RevisedIndicatorNumber : item.IpmsTarget.IndicatorNumber,
            useRevised && item.IpmsTarget.IsTargetNameRevised && item.IpmsTarget.RevisedTargetName != null ? item.IpmsTarget.RevisedTargetName : item.IpmsTarget.TargetName,
            item.IpmsTarget.DepartmentId, item.IpmsTarget.Department != null ? item.IpmsTarget.Department.Name : string.Empty,
            item.IpmsTarget.UnitId, item.IpmsTarget.Unit != null ? item.IpmsTarget.Unit.Name : string.Empty, period.Code,
            item.ActualPerformance, item.AchievementPercent, item.TargetAchieved, item.Status,
            item.SubmittedByUser != null ? item.SubmittedByUser.UserName ?? item.SubmittedByUser.Email ?? item.SubmittedByUser.Id : item.SubmittedByUserId ?? string.Empty,
            item.SubmittedAt)).ToArrayAsync();
    }

    private static OfficialReportDataRow PerformanceRow(OfficialPerformanceReportRow item) => Row(
        ("indicator", item.Indicator), ("targetName", item.TargetName), ("department", item.Department), ("unit", item.Unit), ("period", item.Period),
        ("targetValue", item.TargetValue), ("actualPerformance", item.ActualPerformance), ("variance", item.Variance),
        ("achievementPercent", item.AchievementPercent), ("targetAchieved", item.TargetAchieved), ("status", item.Status));

    private static OfficialReportDataRow SubmissionRow(SubmissionSubject item) => SubjectRow(item,
        ("actualPerformance", item.ActualPerformance ?? string.Empty), ("achievementPercent", Format(item.AchievementPercent)),
        ("targetAchieved", item.TargetAchieved?.ToString() ?? string.Empty), ("submittedBy", item.SubmittedBy), ("submittedAt", Date(item.SubmittedAt)), ("status", item.Status));

    private static OfficialReportDataRow SubjectRow(SubmissionSubject item, params (string Key, string Value)[] extra) => Row(
        [("submissionId", item.SubmissionId), ("indicator", item.Indicator), ("targetName", item.TargetName), ("department", item.Department),
         ("unit", item.Unit), ("period", item.Period), .. extra]);

    private static OfficialReportDataRow Row(params (string Key, string Value)[] values) =>
        new(values.ToDictionary(item => item.Key, item => item.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

    private static bool ActionBelongsTo(OfficialReportType type, string code)
    {
        var normalized = code.ToUpperInvariant();
        return type switch
        {
            OfficialReportType.VerificationRegister => normalized.Contains("VERIFY", StringComparison.Ordinal),
            OfficialReportType.ApprovalRegister => normalized.Contains("APPROV", StringComparison.Ordinal) || normalized.Contains("REJECT", StringComparison.Ordinal),
            OfficialReportType.PmsReview => normalized.Contains("PMS", StringComparison.Ordinal) || normalized.Contains("REVIEW", StringComparison.Ordinal) || normalized.Contains("SCORE", StringComparison.Ordinal),
            _ => false
        };
    }

    private static string? ValidatePeriod(OfficialReportType type, ReportingPeriodType periodType) => type switch
    {
        OfficialReportType.QuarterlyPerformance when periodType is not (ReportingPeriodType.Quarter1 or ReportingPeriodType.Quarter2 or ReportingPeriodType.Quarter3 or ReportingPeriodType.Quarter4) => "A quarterly report requires Q1, Q2, Q3 or Q4.",
        OfficialReportType.MidTermPerformance when periodType != ReportingPeriodType.MidTerm => "A Mid-Term report requires the governed Mid-Term period.",
        OfficialReportType.AnnualPerformance when periodType != ReportingPeriodType.Annual => "An Annual report requires the governed Annual period.",
        _ => null
    };

    private static AccessQueryScopeResult IntersectScopes(AccessQueryScopeResult left, AccessQueryScopeResult right)
    {
        if (!left.PermissionGranted || !right.PermissionGranted) return new(false, false, [], [], [], [], [], []);
        if (left.Unrestricted) return right;
        if (right.Unrestricted) return left;
        return new(true, false,
            left.DepartmentIds.Intersect(right.DepartmentIds).ToArray(), left.UnitIds.Intersect(right.UnitIds).ToArray(),
            left.OwnerUserIds.Intersect(right.OwnerUserIds, StringComparer.OrdinalIgnoreCase).ToArray(),
            left.TargetIds.Intersect(right.TargetIds, StringComparer.OrdinalIgnoreCase).ToArray(),
            left.KpiIds.Intersect(right.KpiIds, StringComparer.OrdinalIgnoreCase).ToArray(), left.MunicipalityIds.Intersect(right.MunicipalityIds).ToArray());
    }

    private static string Date(DateTime value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    private static string Date(DateTime? value) => value.HasValue ? Date(value.Value) : string.Empty;
    private static string UserName(ApplicationUser user) => user.UserName ?? user.Email ?? user.Id;

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
    private static List<OfficialReportGenerationScopeGrant> CreateScopeGrants(long municipalityId, AccessQueryScopeResult scope)
    {
        if (scope.Unrestricted) return [];
        var grants = new List<OfficialReportGenerationScopeGrant>();
        grants.AddRange(scope.DepartmentIds.Distinct().Select(value => NewScopeGrant(municipalityId, OfficialReportScopeDimension.Department, value.ToString(CultureInfo.InvariantCulture))));
        grants.AddRange(scope.UnitIds.Distinct().Select(value => NewScopeGrant(municipalityId, OfficialReportScopeDimension.Unit, value.ToString(CultureInfo.InvariantCulture))));
        grants.AddRange(scope.OwnerUserIds.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToUpperInvariant()).Distinct().Select(value => NewScopeGrant(municipalityId, OfficialReportScopeDimension.OwnerUser, value)));
        grants.AddRange(scope.TargetIds.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToUpperInvariant()).Distinct().Select(value => NewScopeGrant(municipalityId, OfficialReportScopeDimension.Target, value)));
        return grants;
    }
    private static OfficialReportGenerationScopeGrant NewScopeGrant(long municipalityId, OfficialReportScopeDimension dimension, string value) => new() { MunicipalityId = municipalityId, Dimension = dimension, Value = value };
    private static IQueryable<OfficialReportGeneration> ApplyStoredScope(IQueryable<OfficialReportGeneration> query, AccessQueryScopeResult current)
    {
        query = query.Where(item => item.ScopeSchemaVersion == 1);
        if (current.Unrestricted) return query;
        var departmentValues = current.DepartmentIds.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray();
        var unitValues = current.UnitIds.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray();
        var ownerValues = current.OwnerUserIds.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToUpperInvariant()).ToArray();
        var targetValues = current.TargetIds.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToUpperInvariant()).ToArray();
        return query.Where(item => !item.ScopeIsUnrestricted && !item.ScopeGrants.Any(grant =>
            grant.Dimension == OfficialReportScopeDimension.Department && !departmentValues.Contains(grant.Value) ||
            grant.Dimension == OfficialReportScopeDimension.Unit && !unitValues.Contains(grant.Value) ||
            grant.Dimension == OfficialReportScopeDimension.OwnerUser && !ownerValues.Contains(grant.Value) ||
            grant.Dimension == OfficialReportScopeDimension.Target && !targetValues.Contains(grant.Value)));
    }
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
    private static OfficialReportTemplateResponse Map(OfficialReportTemplate item) => new(item.PublicId, item.TemplateFamilyPublicId, item.MunicipalityFinancialYear?.PublicId, item.MunicipalityFinancialYear?.FinancialYear.Code, item.SubmissionKind, item.ReportType, item.Code, item.Name, item.Format, item.VersionNumber, item.HeadingTemplate, OfficialReportCatalog.ValidateColumns(item.ReportType, item.ColumnConfigurationJson), item.IsCurrent, item.IsActive, item.EffectiveFrom, item.EffectiveTo, item.ApprovalReference, item.Reason, item.CreatedAt, Convert.ToBase64String(item.RowVersion));
    private static OfficialReportGenerationResponse Map(OfficialReportGeneration item) => new(item.PublicId, item.GenerationFamilyPublicId, item.VersionNumber, item.ReportTemplate.PublicId, item.ReportTemplate.Code, item.ReportTemplate.Name, item.ReportTemplate.VersionNumber, item.ReportTemplate.Format, item.SubmissionKind, item.ReportType, item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code, item.ReportingPeriod.PublicId, item.ReportingPeriod.Code, item.ScopeJson, item.FilterJson, item.DataVersionReference, item.FileName, item.ContentType, item.SizeInBytes, item.Sha256, item.RowCount, item.GeneratedByUser.UserName ?? item.GeneratedByUser.Email ?? item.GeneratedByUser.Id, item.GeneratedAt, $"/api/v1/reports/official/generations/{item.PublicId}/content");
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ObjectResult ForbidResponse<T>(string message) => StatusCode(StatusCodes.Status403Forbidden, Fail<T>(message));
}

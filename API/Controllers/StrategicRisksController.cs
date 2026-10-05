using System.Security.Claims;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/strategic-risks")]
[Authorize]
public sealed class StrategicRisksController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IAccessControlService accessControl,
    IWorkflowGovernanceService workflow,
    ITenantContext tenantContext) : ControllerBase
{
    private static readonly HashSet<string> RiskSortFields = ["createdat", "reference", "title", "status", "effectivefrom"];
    private static readonly HashSet<string> LinkSortFields = ["linkedat", "indicatornumber", "risktitle", "primary", "status"];

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicRiskDto>>>> GetPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] bool? active = null)
    {
        var session = await SessionAsync<PagedResponse<StrategicRiskDto>>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        if (!RiskSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<StrategicRiskDto>>("SortBy must be createdAt, reference, title, status, or effectiveFrom."));

        var query = RiskQuery();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (municipalityFinancialYearPublicId.HasValue)
        {
            var selected = await context.MunicipalityFinancialYears.AsNoTracking()
                .Include(item => item.FinancialYear)
                .SingleOrDefaultAsync(item => item.PublicId == municipalityFinancialYearPublicId.Value);
            if (selected == null) return NotFound(Fail<PagedResponse<StrategicRiskDto>>("Municipality financial year not found."));
            query = query.Where(item =>
                (!item.EffectiveFromMunicipalityFinancialYearId.HasValue || item.EffectiveFromMunicipalityFinancialYear!.FinancialYear.StartDate <= selected.FinancialYear.StartDate)
                && (!item.EffectiveToMunicipalityFinancialYearId.HasValue || item.EffectiveToMunicipalityFinancialYear!.FinancialYear.EndDate >= selected.FinancialYear.EndDate));
        }
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.RiskTitle.Contains(request.NormalizedSearch)
                || (item.RiskReference != null && item.RiskReference.Contains(request.NormalizedSearch))
                || (item.RiskDescription != null && item.RiskDescription.Contains(request.NormalizedSearch)));

        var totalCount = await query.CountAsync();
        var rows = await OrderRisks(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicRiskDto>>(true,
            PagedResponse<StrategicRiskDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<StrategicRiskSummaryDto>>> GetSummary()
    {
        var session = await SessionAsync<StrategicRiskSummaryDto>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        var risks = context.StrategicRisks.AsNoTracking();
        var links = context.OpmsKpiStrategicRisks.AsNoTracking().Where(item => item.IsActive);
        var total = await risks.CountAsync();
        var active = await risks.CountAsync(item => item.IsActive);
        var linked = await links.Where(item => item.StrategicRisk.IsActive)
            .Select(item => item.StrategicRiskId).Distinct().CountAsync();
        var kpis = await links.Select(item => item.OpmsTargetId).Distinct().CountAsync();
        return Ok(new ApiResponse<StrategicRiskSummaryDto>(true,
            new(total, active, linked, Math.Max(0, active - linked), kpis)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StrategicRiskDto>>> Create([FromBody] SaveStrategicRiskRequest request)
    {
        var session = await SessionAsync<StrategicRiskDto>("STRATEGIC_RISK.CREATE");
        if (session.Error != null) return session.Error;
        if (!TryNormalize(request, out var normalized, out var error)) return BadRequest(Fail<StrategicRiskDto>(error!));
        var years = await ResolveYears(request.EffectiveFromMunicipalityFinancialYearPublicId, request.EffectiveToMunicipalityFinancialYearPublicId);
        if (years.Error != null) return BadRequest(Fail<StrategicRiskDto>(years.Error));
        if (await DuplicateReference(normalized.Reference, null)) return Conflict(Fail<StrategicRiskDto>("A strategic risk with this reference already exists."));

        var entity = new StrategicRisk
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            RiskReference = normalized.Reference,
            RiskTitle = normalized.Title,
            RiskDescription = normalized.Description,
            EffectiveFromMunicipalityFinancialYearId = years.From?.Id,
            EffectiveToMunicipalityFinancialYearId = years.To?.Id,
            IsActive = request.IsActive,
            CreatedByUserId = session.User!.Id
        };
        context.StrategicRisks.Add(entity);
        workflow.QueueAuditTrail(nameof(StrategicRisk), entity.PublicId.ToString(), "Create", null,
            new { entity.RiskReference, entity.RiskTitle, entity.RiskDescription, request.EffectiveFromMunicipalityFinancialYearPublicId, request.EffectiveToMunicipalityFinancialYearPublicId, entity.IsActive },
            session.User.Id, IpAddress(), normalized.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(Fail<StrategicRiskDto>("A strategic risk with this reference already exists.")); }
        entity = await RiskQuery().SingleAsync(item => item.Id == entity.Id);
        return Ok(new ApiResponse<StrategicRiskDto>(true, ToDto(entity)));
    }

    [HttpPut("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<StrategicRiskDto>>> Update(Guid publicId, [FromBody] SaveStrategicRiskRequest request)
    {
        var session = await SessionAsync<StrategicRiskDto>("STRATEGIC_RISK.UPDATE");
        if (session.Error != null) return session.Error;
        if (!TryNormalize(request, out var normalized, out var error)) return BadRequest(Fail<StrategicRiskDto>(error!));
        if (!TryVersion(request.RowVersion, out var expected)) return BadRequest(Fail<StrategicRiskDto>("A valid RowVersion is required."));
        var years = await ResolveYears(request.EffectiveFromMunicipalityFinancialYearPublicId, request.EffectiveToMunicipalityFinancialYearPublicId);
        if (years.Error != null) return BadRequest(Fail<StrategicRiskDto>(years.Error));
        var entity = await context.StrategicRisks.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<StrategicRiskDto>("Strategic risk not found."));
        if (await DuplicateReference(normalized.Reference, entity.Id)) return Conflict(Fail<StrategicRiskDto>("A strategic risk with this reference already exists."));
        var before = new { entity.RiskReference, entity.RiskTitle, entity.RiskDescription, entity.EffectiveFromMunicipalityFinancialYearId, entity.EffectiveToMunicipalityFinancialYearId, entity.IsActive };
        context.Entry(entity).Property(item => item.RowVersion).OriginalValue = expected;
        entity.RiskReference = normalized.Reference;
        entity.RiskTitle = normalized.Title;
        entity.RiskDescription = normalized.Description;
        entity.EffectiveFromMunicipalityFinancialYearId = years.From?.Id;
        entity.EffectiveToMunicipalityFinancialYearId = years.To?.Id;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = session.User!.Id;
        workflow.QueueAuditTrail(nameof(StrategicRisk), entity.PublicId.ToString(), "Update", before,
            new { entity.RiskReference, entity.RiskTitle, entity.RiskDescription, request.EffectiveFromMunicipalityFinancialYearPublicId, request.EffectiveToMunicipalityFinancialYearPublicId, entity.IsActive },
            session.User.Id, IpAddress(), normalized.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicRiskDto>("The strategic risk changed before this update. Reload and retry.")); }
        catch (DbUpdateException) { return Conflict(Fail<StrategicRiskDto>("A strategic risk with this reference already exists.")); }
        entity = await RiskQuery().SingleAsync(item => item.Id == entity.Id);
        return Ok(new ApiResponse<StrategicRiskDto>(true, ToDto(entity)));
    }

    [HttpGet("links/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicRiskKpiLinkDto>>>> GetLinksPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] Guid? strategicRiskPublicId = null,
        [FromQuery] Guid? targetPublicId = null,
        [FromQuery] bool includeInactive = false)
    {
        var session = await SessionAsync<PagedResponse<StrategicRiskKpiLinkDto>>("STRATEGIC_RISK.READ");
        if (session.Error != null) return session.Error;
        if (!LinkSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<StrategicRiskKpiLinkDto>>("SortBy must be linkedAt, indicatorNumber, riskTitle, primary, or status."));
        var targetScope = await accessControl.GetQueryScopeAsync(session.User!, "OPMS_KPI.READ");
        if (!targetScope.PermissionGranted)
            return Ok(new ApiResponse<PagedResponse<StrategicRiskKpiLinkDto>>(true, PagedResponse<StrategicRiskKpiLinkDto>.Empty(request.Page, request.PageSize)));

        var query = LinkQuery();
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (strategicRiskPublicId.HasValue) query = query.Where(item => item.StrategicRisk.PublicId == strategicRiskPublicId.Value);
        if (targetPublicId.HasValue) query = query.Where(item => item.OpmsTarget.PublicId == targetPublicId.Value);
        if (!targetScope.Unrestricted) query = ApplyTargetScope(query, targetScope);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.StrategicRisk.RiskTitle.Contains(request.NormalizedSearch)
                || (item.StrategicRisk.RiskReference != null && item.StrategicRisk.RiskReference.Contains(request.NormalizedSearch))
                || item.OpmsTarget.IndicatorNumber.Contains(request.NormalizedSearch)
                || item.OpmsTarget.TargetName.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var rows = await OrderLinks(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicRiskKpiLinkDto>>(true,
            PagedResponse<StrategicRiskKpiLinkDto>.Create(rows.Select(ToLinkDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("links")]
    public async Task<ActionResult<ApiResponse<StrategicRiskKpiLinkDto>>> LinkKpi([FromBody] LinkStrategicRiskRequest request)
    {
        var session = await SessionAsync<StrategicRiskKpiLinkDto>("STRATEGIC_RISK.LINK_KPI");
        if (session.Error != null) return session.Error;
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail<StrategicRiskKpiLinkDto>("A link reason is required."));
        var risk = await context.StrategicRisks
            .Include(item => item.EffectiveFromMunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear)
            .Include(item => item.EffectiveToMunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear)
            .SingleOrDefaultAsync(item => item.PublicId == request.StrategicRiskPublicId);
        var target = await context.OpmsTargets
            .Include(item => item.SdbipLayer).ThenInclude(item => item!.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.PublicId == request.TargetPublicId);
        if (risk == null || target == null) return NotFound(Fail<StrategicRiskKpiLinkDto>("Strategic risk or OPMS KPI not found."));
        if (!risk.IsActive) return BadRequest(Fail<StrategicRiskKpiLinkDto>("An inactive strategic risk cannot be linked to a KPI."));
        if (target.MunicipalityId != tenantContext.MunicipalityId) return NotFound(Fail<StrategicRiskKpiLinkDto>("OPMS KPI not found."));
        if (!(await accessControl.CheckPermissionAsync(session.User!, "OPMS_KPI.UPDATE", TargetScope(target))).Allowed)
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskKpiLinkDto>("KPI update permission is required for this record."));
        if (!RiskAppliesToTargetYear(risk, target)) return BadRequest(Fail<StrategicRiskKpiLinkDto>("The strategic risk is not valid for the KPI financial year."));
        if (await context.OpmsKpiStrategicRisks.AnyAsync(item => item.OpmsTargetId == target.Id && item.StrategicRiskId == risk.Id && item.IsActive))
            return Conflict(Fail<StrategicRiskKpiLinkDto>("This strategic risk is already linked to the KPI."));

        Guid[] replacedPrimaryLinks = [];
        if (request.IsPrimary)
        {
            var currentPrimary = await context.OpmsKpiStrategicRisks.Where(item => item.OpmsTargetId == target.Id && item.IsActive && item.IsPrimary).ToArrayAsync();
            replacedPrimaryLinks = currentPrimary.Select(item => item.PublicId).ToArray();
            foreach (var item in currentPrimary) item.IsPrimary = false;
        }
        var entity = new OpmsKpiStrategicRisk
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            OpmsTargetId = target.Id,
            StrategicRiskId = risk.Id,
            IsPrimary = request.IsPrimary,
            LinkedByUserId = session.User!.Id,
            LinkReason = request.Reason.Trim()
        };
        context.OpmsKpiStrategicRisks.Add(entity);
        workflow.QueueAuditTrail(nameof(OpmsKpiStrategicRisk), entity.PublicId.ToString(), "Link", null,
            new { TargetPublicId = target.PublicId, StrategicRiskPublicId = risk.PublicId, entity.IsPrimary, ReplacedPrimaryLinks = replacedPrimaryLinks },
            session.User.Id, IpAddress(), entity.LinkReason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(Fail<StrategicRiskKpiLinkDto>("This strategic risk is already linked to the KPI.")); }
        entity = await LinkQuery().SingleAsync(item => item.Id == entity.Id);
        return Ok(new ApiResponse<StrategicRiskKpiLinkDto>(true, ToLinkDto(entity)));
    }

    [HttpPost("links/{publicId:guid}/unlink")]
    public async Task<ActionResult<ApiResponse<StrategicRiskKpiLinkDto>>> UnlinkKpi(Guid publicId, [FromBody] UnlinkStrategicRiskRequest request)
    {
        var session = await SessionAsync<StrategicRiskKpiLinkDto>("STRATEGIC_RISK.UNLINK_KPI");
        if (session.Error != null) return session.Error;
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail<StrategicRiskKpiLinkDto>("An unlink reason is required."));
        if (!TryVersion(request.RowVersion, out var expected)) return BadRequest(Fail<StrategicRiskKpiLinkDto>("A valid RowVersion is required."));
        var entity = await LinkQuery().SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<StrategicRiskKpiLinkDto>("Strategic-risk KPI link not found."));
        if (!entity.IsActive) return Conflict(Fail<StrategicRiskKpiLinkDto>("The strategic-risk KPI link is already inactive."));
        if (!(await accessControl.CheckPermissionAsync(session.User!, "OPMS_KPI.UPDATE", TargetScope(entity.OpmsTarget))).Allowed)
            return StatusCode(StatusCodes.Status403Forbidden, Fail<StrategicRiskKpiLinkDto>("KPI update permission is required for this record."));
        context.Entry(entity).Property(item => item.RowVersion).OriginalValue = expected;
        var wasPrimary = entity.IsPrimary;
        entity.IsActive = false;
        entity.IsPrimary = false;
        entity.UnlinkedAt = DateTime.UtcNow;
        entity.UnlinkedByUserId = session.User!.Id;
        entity.UnlinkReason = request.Reason.Trim();
        workflow.QueueAuditTrail(nameof(OpmsKpiStrategicRisk), entity.PublicId.ToString(), "Unlink",
            new { IsActive = true, WasPrimary = wasPrimary }, new { IsActive = false, entity.UnlinkedAt }, session.User.Id, IpAddress(), entity.UnlinkReason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicRiskKpiLinkDto>("The strategic-risk KPI link changed before this update. Reload and retry.")); }
        return Ok(new ApiResponse<StrategicRiskKpiLinkDto>(true, ToLinkDto(entity)));
    }

    private IQueryable<StrategicRisk> RiskQuery() => context.StrategicRisks.AsNoTracking()
        .Include(item => item.EffectiveFromMunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear)
        .Include(item => item.EffectiveToMunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear)
        .Include(item => item.KpiLinks);

    private IQueryable<OpmsKpiStrategicRisk> LinkQuery() => context.OpmsKpiStrategicRisks
        .Include(item => item.StrategicRisk)
        .Include(item => item.OpmsTarget).ThenInclude(item => item.Department)
        .Include(item => item.OpmsTarget).ThenInclude(item => item.Unit);

    private async Task<(ApplicationUser? User, ActionResult? Error)> SessionAsync<T>(string permission)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId == null ? null : await userManager.FindByIdAsync(userId);
        if (user == null) return (null, Unauthorized(Fail<T>("User not found.")));
        if (!tenantContext.MunicipalityId.HasValue) return (user, BadRequest(Fail<T>("Municipality context is required.")));
        var decision = await accessControl.CheckPermissionAsync(user, permission, new AccessScopeContext(MunicipalityId: tenantContext.MunicipalityId));
        return decision.Allowed ? (user, null) : (user, StatusCode(StatusCodes.Status403Forbidden, Fail<T>(decision.Reason)));
    }

    private async Task<(MunicipalityFinancialYear? From, MunicipalityFinancialYear? To, string? Error)> ResolveYears(Guid? fromId, Guid? toId)
    {
        var ids = new[] { fromId, toId }.Where(item => item.HasValue).Select(item => item!.Value).Distinct().ToArray();
        var years = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).Where(item => ids.Contains(item.PublicId)).ToArrayAsync();
        var from = fromId.HasValue ? years.SingleOrDefault(item => item.PublicId == fromId.Value) : null;
        var to = toId.HasValue ? years.SingleOrDefault(item => item.PublicId == toId.Value) : null;
        if (fromId.HasValue && from == null || toId.HasValue && to == null) return (null, null, "An effective financial year was not found in the selected municipality.");
        if (from != null && to != null && from.FinancialYear.StartDate > to.FinancialYear.StartDate) return (null, null, "Effective-from financial year must not be after effective-to financial year.");
        return (from, to, null);
    }

    private Task<bool> DuplicateReference(string? reference, long? excludedId) => reference == null
        ? Task.FromResult(false)
        : context.StrategicRisks.AnyAsync(item => item.RiskReference == reference && (!excludedId.HasValue || item.Id != excludedId.Value));

    private static bool TryNormalize(SaveStrategicRiskRequest request, out (string? Reference, string Title, string? Description, string Reason) value, out string? error)
    {
        value = (request.RiskReference?.Trim().ToUpperInvariant(), request.RiskTitle?.Trim() ?? string.Empty, request.RiskDescription?.Trim(), request.Reason?.Trim() ?? string.Empty);
        error = value.Title.Length == 0 ? "Risk title is required."
            : value.Title.Length > 500 ? "Risk title cannot exceed 500 characters."
            : value.Reference?.Length > 100 ? "Risk reference cannot exceed 100 characters."
            : value.Description?.Length > 2000 ? "Risk description cannot exceed 2000 characters."
            : value.Reason.Length == 0 ? "An administration reason is required."
            : value.Reason.Length > 1000 ? "Administration reason cannot exceed 1000 characters."
            : null;
        return error == null;
    }

    private static bool TryVersion(string? value, out byte[] version)
    {
        try { version = Convert.FromBase64String(value ?? string.Empty); return version.Length > 0; }
        catch (FormatException) { version = []; return false; }
    }

    private static bool RiskAppliesToTargetYear(StrategicRisk risk, OpmsTarget target)
    {
        var year = target.SdbipLayer?.MunicipalityFinancialYear;
        if (year == null) return false;
        var start = risk.EffectiveFromMunicipalityFinancialYear?.FinancialYear.StartDate;
        var end = risk.EffectiveToMunicipalityFinancialYear?.FinancialYear.EndDate;
        return (!start.HasValue || start <= year.FinancialYear.StartDate) && (!end.HasValue || end >= year.FinancialYear.EndDate);
    }

    private static AccessScopeContext TargetScope(OpmsTarget target) => new(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, KpiId: target.Id, MunicipalityId: target.MunicipalityId);

    private static IQueryable<OpmsKpiStrategicRisk> ApplyTargetScope(IQueryable<OpmsKpiStrategicRisk> query, AccessQueryScopeResult scope) =>
        query.Where(item =>
            item.OpmsTarget.DepartmentId.HasValue && scope.DepartmentIds.Contains(item.OpmsTarget.DepartmentId.Value)
            || item.OpmsTarget.UnitId.HasValue && scope.UnitIds.Contains(item.OpmsTarget.UnitId.Value)
            || item.OpmsTarget.AssignedUserId != null && scope.OwnerUserIds.Contains(item.OpmsTarget.AssignedUserId)
            || scope.TargetIds.Contains(item.OpmsTargetId)
            || scope.KpiIds.Contains(item.OpmsTargetId));

    private static IOrderedQueryable<StrategicRisk> OrderRisks(IQueryable<StrategicRisk> query, string sort, bool desc) => (sort, desc) switch
    {
        ("reference", false) => query.OrderBy(item => item.RiskReference).ThenBy(item => item.PublicId),
        ("reference", true) => query.OrderByDescending(item => item.RiskReference).ThenBy(item => item.PublicId),
        ("title", false) => query.OrderBy(item => item.RiskTitle).ThenBy(item => item.PublicId),
        ("title", true) => query.OrderByDescending(item => item.RiskTitle).ThenBy(item => item.PublicId),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.RiskTitle),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.RiskTitle),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFromMunicipalityFinancialYear!.FinancialYear.StartDate).ThenBy(item => item.PublicId),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFromMunicipalityFinancialYear!.FinancialYear.StartDate).ThenBy(item => item.PublicId),
        (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.PublicId),
        _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.PublicId)
    };

    private static IOrderedQueryable<OpmsKpiStrategicRisk> OrderLinks(IQueryable<OpmsKpiStrategicRisk> query, string sort, bool desc) => (sort, desc) switch
    {
        ("indicatornumber", false) => query.OrderBy(item => item.OpmsTarget.IndicatorNumber).ThenBy(item => item.PublicId),
        ("indicatornumber", true) => query.OrderByDescending(item => item.OpmsTarget.IndicatorNumber).ThenBy(item => item.PublicId),
        ("risktitle", false) => query.OrderBy(item => item.StrategicRisk.RiskTitle).ThenBy(item => item.PublicId),
        ("risktitle", true) => query.OrderByDescending(item => item.StrategicRisk.RiskTitle).ThenBy(item => item.PublicId),
        ("primary", false) => query.OrderBy(item => item.IsPrimary).ThenBy(item => item.PublicId),
        ("primary", true) => query.OrderByDescending(item => item.IsPrimary).ThenBy(item => item.PublicId),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.PublicId),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.PublicId),
        (_, false) => query.OrderBy(item => item.LinkedAt).ThenBy(item => item.PublicId),
        _ => query.OrderByDescending(item => item.LinkedAt).ThenBy(item => item.PublicId)
    };

    private static StrategicRiskDto ToDto(StrategicRisk item) => new(
        item.PublicId, item.RiskReference, item.RiskTitle, item.RiskDescription,
        item.EffectiveFromMunicipalityFinancialYear?.PublicId, item.EffectiveFromMunicipalityFinancialYear?.FinancialYear.Code,
        item.EffectiveToMunicipalityFinancialYear?.PublicId, item.EffectiveToMunicipalityFinancialYear?.FinancialYear.Code,
        item.IsActive, item.KpiLinks.Count(link => link.IsActive), item.CreatedAt, item.UpdatedAt, Convert.ToBase64String(item.RowVersion));

    private static StrategicRiskKpiLinkDto ToLinkDto(OpmsKpiStrategicRisk item) => new(
        item.PublicId, item.StrategicRisk.PublicId, item.StrategicRisk.RiskReference, item.StrategicRisk.RiskTitle,
        item.OpmsTarget.PublicId, item.OpmsTarget.IndicatorNumber, item.OpmsTarget.TargetName,
        item.OpmsTarget.Department?.Name, item.OpmsTarget.Unit?.Name, item.IsPrimary, item.IsActive,
        item.LinkedAt, item.LinkReason, item.UnlinkedAt, item.UnlinkReason, Convert.ToBase64String(item.RowVersion));

    private string? IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
}

public sealed record StrategicRiskDto(Guid PublicId, string? RiskReference, string RiskTitle, string? RiskDescription,
    Guid? EffectiveFromMunicipalityFinancialYearPublicId, string? EffectiveFromFinancialYear,
    Guid? EffectiveToMunicipalityFinancialYearPublicId, string? EffectiveToFinancialYear,
    bool IsActive, int ActiveKpiLinks, DateTime CreatedAt, DateTime? UpdatedAt, string RowVersion);

public sealed record StrategicRiskKpiLinkDto(Guid PublicId, Guid StrategicRiskPublicId, string? RiskReference, string RiskTitle,
    Guid TargetPublicId, string IndicatorNumber, string TargetName, string? DepartmentName, string? UnitName,
    bool IsPrimary, bool IsActive, DateTime LinkedAt, string LinkReason, DateTime? UnlinkedAt, string? UnlinkReason, string RowVersion);

public sealed record StrategicRiskSummaryDto(int TotalRisks, int ActiveRisks, int LinkedRisks, int UnlinkedActiveRisks, int LinkedKpis);
public sealed record SaveStrategicRiskRequest(string? RiskReference, string RiskTitle, string? RiskDescription,
    Guid? EffectiveFromMunicipalityFinancialYearPublicId, Guid? EffectiveToMunicipalityFinancialYearPublicId,
    bool IsActive, string Reason, string? RowVersion);
public sealed record LinkStrategicRiskRequest(Guid StrategicRiskPublicId, Guid TargetPublicId, bool IsPrimary, string Reason);
public sealed record UnlinkStrategicRiskRequest(string Reason, string RowVersion);

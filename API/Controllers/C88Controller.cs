using System.Globalization;
using System.Text.Json;
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
[Route("api/v1/c88")]
[Authorize]
public sealed class C88Controller : ControllerBase
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IAccessControlService accessControl;
    private readonly IWorkflowGovernanceService workflow;
    private readonly ITenantContext tenantContext;

    public C88Controller(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAccessControlService accessControl, IWorkflowGovernanceService workflow, ITenantContext tenantContext)
    {
        this.context = context;
        this.userManager = userManager;
        this.accessControl = accessControl;
        this.workflow = workflow;
        this.tenantContext = tenantContext;
    }

    [HttpGet("workspace")]
    public async Task<ActionResult<ApiResponse<C88WorkspaceResponse>>> GetWorkspace([FromQuery] Guid? municipalityFinancialYearPublicId = null)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized(Fail<C88WorkspaceResponse>("User not found."));
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest(Fail<C88WorkspaceResponse>("Municipality context is required."));
        var indicatorRead = await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.READ", Scope());
        var reportRead = await accessControl.CheckPermissionAsync(user, "C88_REPORT.READ", Scope());
        if (!indicatorRead.Allowed && !reportRead.Allowed) return Forbidden<C88WorkspaceResponse>(indicatorRead.Reason);
        var configurations = context.C88MunicipalityConfigurations.AsNoTracking()
            .Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Include(item => item.CatalogueVersion).AsQueryable();
        if (municipalityFinancialYearPublicId.HasValue)
            configurations = configurations.Where(item => item.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        var configRows = await configurations.OrderByDescending(item => item.MunicipalityFinancialYear.FinancialYear.StartDate).ToArrayAsync();
        var versionIds = configRows.Select(item => item.C88CatalogueVersionId).Distinct().ToArray();
        if (!municipalityFinancialYearPublicId.HasValue)
            versionIds = await context.C88CatalogueVersions.AsNoTracking().Select(item => item.Id).ToArrayAsync();

        var items = await context.C88CatalogueItems.AsNoTracking().Include(item => item.CatalogueVersion).Include(item => item.ParentItem).Where(item => versionIds.Contains(item.C88CatalogueVersionId)).OrderBy(item => item.Kind).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Code).ToArrayAsync();
        return Ok(new ApiResponse<C88WorkspaceResponse>(true, new C88WorkspaceResponse(
            items.Select(ToResponse).ToArray(), [])));
    }

    [HttpGet("configurations/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88ConfigurationResponse>>>> GetConfigurationsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? configurationPublicId = null,
        [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] Guid? catalogueVersionPublicId = null, [FromQuery] bool? enabled = null)
    {
        var readError = await WorkspaceRead<C88ConfigurationResponse>();
        if (readError != null) return readError;
        if (request.NormalizedSortBy is not ("createdat" or "effectivefrom" or "financialyear" or "catalogueversion" or "enabled"))
            return BadRequest(Fail<PagedResponse<C88ConfigurationResponse>>("SortBy must be createdAt, effectiveFrom, financialYear, catalogueVersion, or enabled."));
        var query = context.C88MunicipalityConfigurations.AsNoTracking().AsQueryable();
        if (configurationPublicId.HasValue) query = query.Where(item => item.PublicId == configurationPublicId.Value);
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (catalogueVersionPublicId.HasValue) query = query.Where(item => item.CatalogueVersion.PublicId == catalogueVersionPublicId.Value);
        if (enabled.HasValue) query = query.Where(item => item.IsEnabled == enabled.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.MunicipalityFinancialYear.FinancialYear.Code.Contains(request.NormalizedSearch)
            || item.CatalogueVersion.Code.Contains(request.NormalizedSearch) || item.CatalogueVersion.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id),
            ("financialyear", false) => query.OrderBy(item => item.MunicipalityFinancialYear.FinancialYear.StartDate).ThenBy(item => item.Id),
            ("financialyear", true) => query.OrderByDescending(item => item.MunicipalityFinancialYear.FinancialYear.StartDate).ThenByDescending(item => item.Id),
            ("catalogueversion", false) => query.OrderBy(item => item.CatalogueVersion.Code).ThenBy(item => item.Id),
            ("catalogueversion", true) => query.OrderByDescending(item => item.CatalogueVersion.Code).ThenByDescending(item => item.Id),
            ("enabled", false) => query.OrderBy(item => item.IsEnabled).ThenBy(item => item.Id),
            ("enabled", true) => query.OrderByDescending(item => item.IsEnabled).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .Include(item => item.CatalogueVersion).Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88ConfigurationResponse>>(true,
            PagedResponse<C88ConfigurationResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("catalogue-versions/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88CatalogueVersionResponse>>>> GetCatalogueVersionsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? catalogueVersionPublicId = null,
        [FromQuery] bool? published = null, [FromQuery] bool? active = null)
    {
        var readError = await WorkspaceRead<C88CatalogueVersionResponse>();
        if (readError != null) return readError;
        if (request.NormalizedSortBy is not ("createdat" or "editiondate" or "effectivefrom" or "code" or "name" or "published"))
            return BadRequest(Fail<PagedResponse<C88CatalogueVersionResponse>>("SortBy must be createdAt, editionDate, effectiveFrom, code, name, or published."));
        var query = context.C88CatalogueVersions.AsNoTracking().AsQueryable();
        if (catalogueVersionPublicId.HasValue) query = query.Where(item => item.PublicId == catalogueVersionPublicId.Value);
        if (published.HasValue) query = query.Where(item => item.IsPublished == published.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Code.Contains(request.NormalizedSearch) || item.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("editiondate", false) => query.OrderBy(item => item.EditionDate).ThenBy(item => item.Id),
            ("editiondate", true) => query.OrderByDescending(item => item.EditionDate).ThenByDescending(item => item.Id),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id),
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Id),
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            ("published", false) => query.OrderBy(item => item.IsPublished).ThenBy(item => item.EditionDate).ThenBy(item => item.Id),
            ("published", true) => query.OrderByDescending(item => item.IsPublished).ThenByDescending(item => item.EditionDate).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88CatalogueVersionResponse>>(true,
            PagedResponse<C88CatalogueVersionResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("reports/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88IndicatorReportResponse>>>> GetReportsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] Guid? municipalityFinancialYearPublicId = null)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized(Fail<PagedResponse<C88IndicatorReportResponse>>("User not found."));
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest(Fail<PagedResponse<C88IndicatorReportResponse>>("Municipality context is required."));
        if (!C88ReportSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<C88IndicatorReportResponse>>("SortBy must be createdAt, indicatorCode, state, or versionNumber."));
        var reportRead = await accessControl.CheckPermissionAsync(user, "C88_REPORT.READ", Scope());
        if (!reportRead.Allowed) return Forbidden<PagedResponse<C88IndicatorReportResponse>>(reportRead.Reason);
        var manager = (await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.UPDATE", Scope())).Allowed;
        long[] scopedIndicatorIds = [];
        if (!manager)
        {
            var employeeId = await context.MunicipalEmployees.Where(item => item.IdentityUserId == user.Id && item.IsActive).Select(item => (long?)item.Id).SingleOrDefaultAsync();
            if (employeeId.HasValue)
            {
                var now = DateTime.UtcNow;
                scopedIndicatorIds = await context.C88Assignments.Where(item => item.MunicipalEmployeeId == employeeId.Value && item.IsActive
                        && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo >= now))
                    .Select(item => item.C88IndicatorId).Distinct().ToArrayAsync();
            }
        }

        var query = ReportQuery().AsNoTracking().Where(item => item.IsCurrent && (manager || scopedIndicatorIds.Contains(item.C88IndicatorId)));
        if (municipalityFinancialYearPublicId.HasValue)
            query = query.Where(item => item.Configuration.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Indicator.Code.Contains(request.NormalizedSearch) || item.Indicator.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var rows = await ApplyC88ReportOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88IndicatorReportResponse>>(true,
            PagedResponse<C88IndicatorReportResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> C88ReportSortFields = ["createdat", "indicatorcode", "state", "versionnumber"];

    private static IOrderedQueryable<C88IndicatorReport> ApplyC88ReportOrdering(IQueryable<C88IndicatorReport> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("indicatorcode", false) => query.OrderBy(item => item.Indicator.Code).ThenBy(item => item.PublicId),
            ("indicatorcode", true) => query.OrderByDescending(item => item.Indicator.Code).ThenBy(item => item.PublicId),
            ("state", false) => query.OrderBy(item => item.State).ThenBy(item => item.PublicId),
            ("state", true) => query.OrderByDescending(item => item.State).ThenBy(item => item.PublicId),
            ("versionnumber", false) => query.OrderBy(item => item.VersionNumber).ThenBy(item => item.PublicId),
            ("versionnumber", true) => query.OrderByDescending(item => item.VersionNumber).ThenBy(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.PublicId)
        };

    [HttpGet("indicators/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88IndicatorResponse>>>> GetIndicatorsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? catalogueVersionPublicId = null,
        [FromQuery] Guid? indicatorPublicId = null, [FromQuery] bool? active = null, [FromQuery] C88ValueType? valueType = null)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized(Fail<PagedResponse<C88IndicatorResponse>>("User not found."));
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest(Fail<PagedResponse<C88IndicatorResponse>>("Municipality context is required."));
        var indicatorRead = await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.READ", Scope());
        var reportRead = await accessControl.CheckPermissionAsync(user, "C88_REPORT.READ", Scope());
        if (!indicatorRead.Allowed && !reportRead.Allowed) return Forbidden<PagedResponse<C88IndicatorResponse>>(indicatorRead.Reason);
        if (request.NormalizedSortBy is not ("createdat" or "code" or "name" or "valuetype"))
            return BadRequest(Fail<PagedResponse<C88IndicatorResponse>>("SortBy must be createdAt, code, name, or valueType."));

        var manager = (await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.UPDATE", Scope())).Allowed;
        long[] scopedIndicatorIds = [];
        if (!manager)
        {
            var employeeId = await context.MunicipalEmployees.Where(item => item.IdentityUserId == user.Id && item.IsActive)
                .Select(item => (long?)item.Id).SingleOrDefaultAsync();
            if (employeeId.HasValue)
            {
                var now = DateTime.UtcNow;
                scopedIndicatorIds = await context.C88Assignments.Where(item => item.MunicipalEmployeeId == employeeId.Value && item.IsActive
                        && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo >= now))
                    .Select(item => item.C88IndicatorId).Distinct().ToArrayAsync();
            }
        }

        var query = context.C88Indicators.AsNoTracking().AsQueryable();
        if (!manager) query = query.Where(item => scopedIndicatorIds.Contains(item.Id));
        if (catalogueVersionPublicId.HasValue) query = query.Where(item => item.CatalogueVersion.PublicId == catalogueVersionPublicId.Value);
        if (indicatorPublicId.HasValue) query = query.Where(item => item.PublicId == indicatorPublicId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (valueType.HasValue) query = query.Where(item => item.ValueType == valueType.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Code.Contains(request.NormalizedSearch)
            || item.Name.Contains(request.NormalizedSearch) || item.Definition.Contains(request.NormalizedSearch)
            || item.OfficialTechnicalIndicatorDescription.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Id),
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            ("valuetype", false) => query.OrderBy(item => item.ValueType).ThenBy(item => item.Code).ThenBy(item => item.Id),
            ("valuetype", true) => query.OrderByDescending(item => item.ValueType).ThenByDescending(item => item.Code).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.CatalogueVersion).Include(item => item.SectorItem).Include(item => item.OutcomeItem)
            .Include(item => item.IndicatorTypeItem).Include(item => item.DataElements).Include(item => item.Applicability)
            .ThenInclude(item => item.MunicipalCategoryItem).Include(item => item.Applicability).ThenInclude(item => item.ReadinessTierItem)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88IndicatorResponse>>(true,
            PagedResponse<C88IndicatorResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("plans/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88IndicatorPlanResponse>>>> GetPlansPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] Guid? configurationPublicId = null, [FromQuery] Guid? indicatorPublicId = null)
    {
        var visibility = await IndicatorVisibility<C88IndicatorPlanResponse>();
        if (visibility.Error != null) return visibility.Error;
        if (request.NormalizedSortBy is not ("createdat" or "indicatorcode" or "baseline" or "annualtarget"))
            return BadRequest(Fail<PagedResponse<C88IndicatorPlanResponse>>("SortBy must be createdAt, indicatorCode, baseline, or annualTarget."));
        var query = context.C88IndicatorPlans.AsNoTracking().AsQueryable();
        if (!visibility.Manager) query = query.Where(item => visibility.IndicatorIds.Contains(item.C88IndicatorId));
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.Configuration.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (configurationPublicId.HasValue) query = query.Where(item => item.Configuration.PublicId == configurationPublicId.Value);
        if (indicatorPublicId.HasValue) query = query.Where(item => item.Indicator.PublicId == indicatorPublicId.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Indicator.Code.Contains(request.NormalizedSearch)
            || item.Indicator.Name.Contains(request.NormalizedSearch)
            || item.BaselineValue != null && item.BaselineValue.Contains(request.NormalizedSearch)
            || item.AnnualTarget != null && item.AnnualTarget.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("indicatorcode", false) => query.OrderBy(item => item.Indicator.Code).ThenBy(item => item.Id),
            ("indicatorcode", true) => query.OrderByDescending(item => item.Indicator.Code).ThenByDescending(item => item.Id),
            ("baseline", false) => query.OrderBy(item => item.BaselineValue).ThenBy(item => item.Id),
            ("baseline", true) => query.OrderByDescending(item => item.BaselineValue).ThenByDescending(item => item.Id),
            ("annualtarget", false) => query.OrderBy(item => item.AnnualTarget).ThenBy(item => item.Id),
            ("annualtarget", true) => query.OrderByDescending(item => item.AnnualTarget).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.Configuration).Include(item => item.Indicator)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88IndicatorPlanResponse>>(true,
            PagedResponse<C88IndicatorPlanResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("compliance-questions/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88ComplianceQuestionResponse>>>> GetComplianceQuestionsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? catalogueVersionPublicId = null,
        [FromQuery] Guid? reportTypePublicId = null, [FromQuery] bool? active = null, [FromQuery] bool? required = null)
    {
        var readError = await WorkspaceRead<C88ComplianceQuestionResponse>();
        if (readError != null) return readError;
        if (request.NormalizedSortBy is not ("createdat" or "sequence" or "code" or "reporttype" or "required"))
            return BadRequest(Fail<PagedResponse<C88ComplianceQuestionResponse>>("SortBy must be sequence, code, reportType, or required."));
        var query = context.C88ComplianceQuestions.AsNoTracking().AsQueryable();
        if (catalogueVersionPublicId.HasValue) query = query.Where(item => item.CatalogueVersion.PublicId == catalogueVersionPublicId.Value);
        if (reportTypePublicId.HasValue) query = query.Where(item => item.ReportTypeItem.PublicId == reportTypePublicId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (required.HasValue) query = query.Where(item => item.IsRequired == required.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Code.Contains(request.NormalizedSearch)
            || item.Prompt.Contains(request.NormalizedSearch) || item.ReportTypeItem.Code.Contains(request.NormalizedSearch)
            || item.ReportTypeItem.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Id),
            ("reporttype", false) => query.OrderBy(item => item.ReportTypeItem.Code).ThenBy(item => item.Sequence).ThenBy(item => item.Id),
            ("reporttype", true) => query.OrderByDescending(item => item.ReportTypeItem.Code).ThenByDescending(item => item.Sequence).ThenByDescending(item => item.Id),
            ("required", false) => query.OrderBy(item => item.IsRequired).ThenBy(item => item.Sequence).ThenBy(item => item.Id),
            ("required", true) => query.OrderByDescending(item => item.IsRequired).ThenBy(item => item.Sequence).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.Sequence).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.Sequence).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.CatalogueVersion).Include(item => item.ReportTypeItem).Include(item => item.ResponseTypeItem)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88ComplianceQuestionResponse>>(true,
            PagedResponse<C88ComplianceQuestionResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("calendars/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88ReportingCalendarResponse>>>> GetCalendarsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] Guid? configurationPublicId = null, [FromQuery] bool? active = null)
    {
        var readError = await WorkspaceRead<C88ReportingCalendarResponse>();
        if (readError != null) return readError;
        if (request.NormalizedSortBy is not ("createdat" or "opensat" or "dueat" or "code" or "name"))
            return BadRequest(Fail<PagedResponse<C88ReportingCalendarResponse>>("SortBy must be opensAt, dueAt, code, or name."));
        var query = context.C88ReportingCalendars.AsNoTracking().AsQueryable();
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.Configuration.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (configurationPublicId.HasValue) query = query.Where(item => item.Configuration.PublicId == configurationPublicId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Code.Contains(request.NormalizedSearch)
            || item.Name.Contains(request.NormalizedSearch) || item.ReportTypeItem.Code.Contains(request.NormalizedSearch)
            || item.ReportTypeItem.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("dueat", false) => query.OrderBy(item => item.DueAt).ThenBy(item => item.Id),
            ("dueat", true) => query.OrderByDescending(item => item.DueAt).ThenByDescending(item => item.Id),
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Id),
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.OpensAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.OpensAt).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.Configuration).Include(item => item.ReportTypeItem).Include(item => item.ReportingPeriod)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88ReportingCalendarResponse>>(true,
            PagedResponse<C88ReportingCalendarResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("workflows/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88WorkflowResponse>>>> GetWorkflowsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearPublicId = null,
        [FromQuery] Guid? configurationPublicId = null, [FromQuery] bool? current = null, [FromQuery] bool? active = null)
    {
        var readError = await WorkspaceRead<C88WorkflowResponse>();
        if (readError != null) return readError;
        if (request.NormalizedSortBy is not ("createdat" or "effectivefrom" or "versionnumber" or "current"))
            return BadRequest(Fail<PagedResponse<C88WorkflowResponse>>("SortBy must be effectiveFrom, versionNumber, or current."));
        var query = context.C88WorkflowDefinitions.AsNoTracking().AsQueryable();
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.Configuration.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (configurationPublicId.HasValue) query = query.Where(item => item.Configuration.PublicId == configurationPublicId.Value);
        if (current.HasValue) query = query.Where(item => item.IsCurrent == current.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Stages.Any(stage => stage.Name.Contains(request.NormalizedSearch)));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("versionnumber", false) => query.OrderBy(item => item.VersionNumber).ThenBy(item => item.Id),
            ("versionnumber", true) => query.OrderByDescending(item => item.VersionNumber).ThenByDescending(item => item.Id),
            ("current", false) => query.OrderBy(item => item.IsCurrent).ThenBy(item => item.Id),
            ("current", true) => query.OrderByDescending(item => item.IsCurrent).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.Configuration).Include(item => item.Stages)
            .Skip(request.Offset).Take(request.PageSize).AsSplitQuery().ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88WorkflowResponse>>(true,
            PagedResponse<C88WorkflowResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("assignments/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88AssignmentResponse>>>> GetAssignmentsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearPublicId = null, [FromQuery] Guid? indicatorPublicId = null, [FromQuery] bool? active = null)
    {
        var visibility = await IndicatorVisibility<C88AssignmentResponse>();
        if (visibility.Error != null) return visibility.Error;
        if (request.NormalizedSortBy is not ("createdat" or "effectivefrom" or "indicatorcode" or "employeename" or "role"))
            return BadRequest(Fail<PagedResponse<C88AssignmentResponse>>("SortBy must be effectiveFrom, indicatorCode, employeeName, or role."));
        var query = context.C88Assignments.AsNoTracking().AsQueryable();
        if (!visibility.Manager) query = query.Where(item => visibility.IndicatorIds.Contains(item.C88IndicatorId));
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.Configuration.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (indicatorPublicId.HasValue) query = query.Where(item => item.Indicator.PublicId == indicatorPublicId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Indicator.Code.Contains(request.NormalizedSearch)
            || item.MunicipalEmployee.FirstName.Contains(request.NormalizedSearch) || item.MunicipalEmployee.LastName.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("indicatorcode", false) => query.OrderBy(item => item.Indicator.Code).ThenBy(item => item.Id),
            ("indicatorcode", true) => query.OrderByDescending(item => item.Indicator.Code).ThenByDescending(item => item.Id),
            ("employeename", false) => query.OrderBy(item => item.MunicipalEmployee.LastName).ThenBy(item => item.MunicipalEmployee.FirstName).ThenBy(item => item.Id),
            ("employeename", true) => query.OrderByDescending(item => item.MunicipalEmployee.LastName).ThenByDescending(item => item.MunicipalEmployee.FirstName).ThenByDescending(item => item.Id),
            ("role", false) => query.OrderBy(item => item.Role).ThenBy(item => item.Id),
            ("role", true) => query.OrderByDescending(item => item.Role).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.Configuration).Include(item => item.Indicator).Include(item => item.MunicipalEmployee)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88AssignmentResponse>>(true, PagedResponse<C88AssignmentResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("mappings/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<C88MappingResponse>>>> GetMappingsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearPublicId = null, [FromQuery] Guid? indicatorPublicId = null, [FromQuery] bool? active = null)
    {
        var visibility = await IndicatorVisibility<C88MappingResponse>();
        if (visibility.Error != null) return visibility.Error;
        if (request.NormalizedSortBy is not ("createdat" or "indicatorcode" or "opmsindicator" or "mappingtype"))
            return BadRequest(Fail<PagedResponse<C88MappingResponse>>("SortBy must be createdAt, indicatorCode, opmsIndicator, or mappingType."));
        var query = context.C88OpmsMappings.AsNoTracking().AsQueryable();
        if (!visibility.Manager) query = query.Where(item => visibility.IndicatorIds.Contains(item.C88IndicatorId));
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.Configuration.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (indicatorPublicId.HasValue) query = query.Where(item => item.Indicator.PublicId == indicatorPublicId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Indicator.Code.Contains(request.NormalizedSearch)
            || item.OpmsTarget.IndicatorNumber.Contains(request.NormalizedSearch) || item.Reason.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("indicatorcode", false) => query.OrderBy(item => item.Indicator.Code).ThenBy(item => item.Id),
            ("indicatorcode", true) => query.OrderByDescending(item => item.Indicator.Code).ThenByDescending(item => item.Id),
            ("opmsindicator", false) => query.OrderBy(item => item.OpmsTarget.IndicatorNumber).ThenBy(item => item.Id),
            ("opmsindicator", true) => query.OrderByDescending(item => item.OpmsTarget.IndicatorNumber).ThenByDescending(item => item.Id),
            ("mappingtype", false) => query.OrderBy(item => item.MappingType).ThenBy(item => item.Id),
            ("mappingtype", true) => query.OrderByDescending(item => item.MappingType).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        };
        var rows = await ordered.Include(item => item.Configuration).Include(item => item.Indicator).Include(item => item.OpmsTarget)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<C88MappingResponse>>(true, PagedResponse<C88MappingResponse>.Create(rows.Select(ToResponse), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("catalogue-versions")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCatalogueVersion([FromBody] SaveC88CatalogueVersionRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_CATALOGUE");
        if (session.Error != null) return session.Error;
        if (!ValidateCatalogueVersion(request, out var code, out var name, out var reason, out var error)) return BadRequest(Fail<Guid>(error!));
        if (await context.C88CatalogueVersions.AnyAsync(item => item.Code == code)) return Conflict(Fail<Guid>("A Circular 88 catalogue version with this code already exists."));
        var entity = new C88CatalogueVersion
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, Code = code, Name = name, EditionDate = Utc(request.EditionDate),
            EffectiveFrom = Utc(request.EffectiveFrom), EffectiveTo = NullableUtc(request.EffectiveTo), IsPublished = request.IsPublished,
            IsActive = request.IsActive, CreatedByUserId = session.User!.Id
        };
        context.C88CatalogueVersions.Add(entity);
        Audit(entity.PublicId, "CreateCatalogueVersion", null, new { entity.Code, entity.Name, entity.EditionDate, entity.IsPublished, Reason = reason }, session.User.Id);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(Fail<Guid>("The catalogue version conflicts with an existing record.")); }
        return OkId(entity.PublicId);
    }

    [HttpPut("catalogue-versions/{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateCatalogueVersion(Guid publicId, [FromBody] SaveC88CatalogueVersionRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_CATALOGUE");
        if (session.Error != null) return session.Error;
        if (!ValidateCatalogueVersion(request, out var code, out var name, out var reason, out var error) || !TryVersion(request.RowVersion, out var rowVersion))
            return BadRequest(Fail<Guid>(error ?? "A valid RowVersion is required."));
        var entity = await context.C88CatalogueVersions.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<Guid>("Catalogue version not found."));
        if (entity.IsPublished) return Conflict(Fail<Guid>("Published catalogue versions are immutable. Create a new edition."));
        if (request.IsPublished)
        {
            var kinds = await context.C88CatalogueItems.Where(item => item.C88CatalogueVersionId == entity.Id && item.IsActive).Select(item => item.Kind).Distinct().ToArrayAsync();
            var complete = Enum.GetValues<C88CatalogueItemKind>().All(kinds.Contains)
                && await context.C88Indicators.AnyAsync(item => item.C88CatalogueVersionId == entity.Id && item.IsActive && item.DataElements.Any() && item.Applicability.Any())
                && await context.C88ComplianceQuestions.AnyAsync(item => item.C88CatalogueVersionId == entity.Id && item.IsActive);
            if (!complete) return Conflict(Fail<Guid>("A published C88 edition requires every catalogue classification, at least one indicator with data elements/applicability, and at least one compliance question."));
        }
        context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var before = new { entity.Code, entity.Name, entity.EditionDate, entity.EffectiveFrom, entity.EffectiveTo, entity.IsPublished, entity.IsActive };
        entity.Code = code; entity.Name = name; entity.EditionDate = Utc(request.EditionDate); entity.EffectiveFrom = Utc(request.EffectiveFrom);
        entity.EffectiveTo = NullableUtc(request.EffectiveTo); entity.IsPublished = request.IsPublished; entity.IsActive = request.IsActive;
        Audit(entity.PublicId, "UpdateCatalogueVersion", before, new { entity.Code, entity.Name, entity.EditionDate, entity.EffectiveFrom, entity.EffectiveTo, entity.IsPublished, entity.IsActive, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The catalogue version changed before this update. Reload and retry.");
    }

    [HttpPost("catalogue-items")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCatalogueItem([FromBody] SaveC88CatalogueItemRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_CATALOGUE");
        if (session.Error != null) return session.Error;
        if (!TryCode(request.Code, out var code) || !TryText(request.Name, 240, out var name) || !TryReason(request.Reason, out var reason))
            return BadRequest(Fail<Guid>("Code, name and reason are required and must fit the allowed lengths."));
        if (request.DisplayOrder < 0 || !Enum.IsDefined(request.Kind)) return BadRequest(Fail<Guid>("Kind and non-negative display order are required."));
        var version = await EditableVersion(request.CatalogueVersionPublicId);
        if (version == null) return Conflict(Fail<Guid>("The catalogue version is missing or already published."));
        C88CatalogueItem? parent = null;
        if (request.ParentItemPublicId.HasValue)
        {
            parent = await context.C88CatalogueItems.SingleOrDefaultAsync(item => item.PublicId == request.ParentItemPublicId && item.C88CatalogueVersionId == version.Id);
            if (parent == null) return BadRequest(Fail<Guid>("Parent catalogue item must belong to the same edition."));
        }
        var entity = new C88CatalogueItem
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, C88CatalogueVersionId = version.Id, ParentItemId = parent?.Id,
            Kind = request.Kind, Code = code, Name = name, Description = Clean(request.Description, 4000), DisplayOrder = request.DisplayOrder, IsActive = request.IsActive
        };
        context.C88CatalogueItems.Add(entity);
        Audit(entity.PublicId, "CreateCatalogueItem", null, new { version.PublicId, entity.Kind, entity.Code, entity.Name, ParentPublicId = parent?.PublicId, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The catalogue item conflicts with an existing code.");
    }

    [HttpPost("indicators")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateIndicator([FromBody] SaveC88IndicatorRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_CATALOGUE");
        if (session.Error != null) return session.Error;
        if (!TryCode(request.Code, out var code) || !TryText(request.Name, 300, out var name) || !TryText(request.Definition, 8000, out var definition)
            || !TryText(request.OfficialTechnicalIndicatorDescription, 16000, out var tid) || !TryReason(request.Reason, out var reason))
            return BadRequest(Fail<Guid>("Code, name, definition, official TID and reason are required."));
        if (!Enum.IsDefined(request.ValueType) || !Enum.IsDefined(request.CalculationOperator)) return BadRequest(Fail<Guid>("Value type and controlled calculation operator are required."));
        if (request.ValueType is C88ValueType.Boolean or C88ValueType.Text && request.CalculationOperator != C88ControlledCalculationOperator.None)
            return BadRequest(Fail<Guid>("Text and Boolean indicators cannot use numeric calculation operators."));
        if (request.CalculationOperator is C88ControlledCalculationOperator.Ratio or C88ControlledCalculationOperator.Percentage or C88ControlledCalculationOperator.Difference && request.DataElements.Length < 2)
            return BadRequest(Fail<Guid>("The selected calculation operator requires at least two data elements."));
        var version = await EditableVersion(request.CatalogueVersionPublicId);
        if (version == null) return Conflict(Fail<Guid>("The catalogue version is missing or already published."));
        var linked = await ResolveItems(version.Id, request.SectorPublicId, request.OutcomePublicId, request.IndicatorTypePublicId);
        if (linked.Error != null) return BadRequest(Fail<Guid>(linked.Error));
        var normalizedElements = new List<C88DataElementInput>();
        foreach (var input in request.DataElements ?? [])
        {
            if (!TryCode(input.Code, out var elementCode) || !TryText(input.Name, 240, out var elementName) || input.Sequence <= 0 || !Enum.IsDefined(input.ValueType))
                return BadRequest(Fail<Guid>("Every data element requires a unique code, name, positive sequence and valid value type."));
            normalizedElements.Add(input with { Code = elementCode, Name = elementName, Description = Clean(input.Description, 4000) });
        }
        if (normalizedElements.Select(item => item.Code).Distinct(StringComparer.Ordinal).Count() != normalizedElements.Count
            || normalizedElements.Select(item => item.Sequence).Distinct().Count() != normalizedElements.Count)
            return BadRequest(Fail<Guid>("Data-element codes and sequences must be unique within an indicator."));
        var applicability = new List<(C88ApplicabilityInput Input, C88CatalogueItem Category, C88CatalogueItem? Tier)>();
        foreach (var input in request.Applicability ?? [])
        {
            var category = await ItemOfKind(version.Id, input.MunicipalCategoryPublicId, C88CatalogueItemKind.MunicipalCategory);
            var tier = input.ReadinessTierPublicId.HasValue ? await ItemOfKind(version.Id, input.ReadinessTierPublicId.Value, C88CatalogueItemKind.ReadinessTier) : null;
            if (category == null || input.ReadinessTierPublicId.HasValue && tier == null) return BadRequest(Fail<Guid>("Applicability references must use municipality-category/readiness-tier items from this edition."));
            applicability.Add((input, category, tier));
        }
        var entity = new C88Indicator
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, C88CatalogueVersionId = version.Id, Code = code, Name = name,
            Definition = definition, OfficialTechnicalIndicatorDescription = tid, SectorItemId = linked.Sector?.Id, OutcomeItemId = linked.Outcome?.Id,
            IndicatorTypeItemId = linked.Type?.Id, ValueType = request.ValueType, CalculationOperator = request.CalculationOperator,
            OfficialFormulaText = Clean(request.OfficialFormulaText, 4000), RequiresBaseline = request.RequiresBaseline,
            RequiresMediumTermTarget = request.RequiresMediumTermTarget, RequiresAnnualTarget = request.RequiresAnnualTarget, IsActive = request.IsActive
        };
        foreach (var item in normalizedElements) entity.DataElements.Add(new C88DataElement { MunicipalityId = entity.MunicipalityId, Code = item.Code, Name = item.Name, Description = item.Description, ValueType = item.ValueType, IsRequired = item.IsRequired, Sequence = item.Sequence });
        foreach (var item in applicability) entity.Applicability.Add(new C88IndicatorApplicability { MunicipalityId = entity.MunicipalityId, MunicipalCategoryItemId = item.Category.Id, ReadinessTierItemId = item.Tier?.Id, IsApplicable = item.Input.IsApplicable, Notes = Clean(item.Input.Notes, 2000) });
        context.C88Indicators.Add(entity);
        Audit(entity.PublicId, "CreateIndicator", null, new { version.PublicId, entity.Code, entity.Name, entity.ValueType, entity.CalculationOperator, DataElements = normalizedElements.Count, Applicability = applicability.Count, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The indicator conflicts with existing catalogue content.");
    }

    [HttpPost("compliance-questions")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateComplianceQuestion([FromBody] SaveC88ComplianceQuestionRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_CATALOGUE");
        if (session.Error != null) return session.Error;
        if (!TryCode(request.Code, out var code) || !TryText(request.Prompt, 4000, out var prompt) || !TryReason(request.Reason, out var reason) || request.Sequence <= 0)
            return BadRequest(Fail<Guid>("Code, prompt, positive sequence and reason are required."));
        var version = await EditableVersion(request.CatalogueVersionPublicId);
        if (version == null) return Conflict(Fail<Guid>("The catalogue version is missing or already published."));
        var reportType = await ItemOfKind(version.Id, request.ReportTypePublicId, C88CatalogueItemKind.ReportType);
        var responseType = await ItemOfKind(version.Id, request.ResponseTypePublicId, C88CatalogueItemKind.ResponseType);
        if (reportType == null || responseType == null) return BadRequest(Fail<Guid>("Report and response types must belong to the selected edition."));
        var entity = new C88ComplianceQuestion { MunicipalityId = tenantContext.MunicipalityId!.Value, C88CatalogueVersionId = version.Id, ReportTypeItemId = reportType.Id, ResponseTypeItemId = responseType.Id, Code = code, Prompt = prompt, IsRequired = request.IsRequired, Sequence = request.Sequence, IsActive = request.IsActive };
        context.C88ComplianceQuestions.Add(entity);
        Audit(entity.PublicId, "CreateComplianceQuestion", null, new { CatalogueVersionPublicId = version.PublicId, ReportTypePublicId = reportType.PublicId, ResponseTypePublicId = responseType.PublicId, entity.Code, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The compliance question conflicts with existing catalogue content.");
    }

    [HttpPut("configurations")]
    public async Task<ActionResult<ApiResponse<Guid>>> Configure([FromBody] ConfigureC88Request request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.CONFIGURE");
        if (session.Error != null) return session.Error;
        if (!TryReason(request.Reason, out var reason) || request.EffectiveFrom == default || request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            return BadRequest(Fail<Guid>("A valid effective period and reason are required."));
        var year = await context.MunicipalityFinancialYears.SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId);
        var version = await context.C88CatalogueVersions.SingleOrDefaultAsync(item => item.PublicId == request.CatalogueVersionPublicId && item.IsPublished && item.IsActive);
        if (year == null || version == null) return BadRequest(Fail<Guid>("The municipality financial year and a published active catalogue edition are required."));
        var entity = await context.C88MunicipalityConfigurations.SingleOrDefaultAsync(item => item.MunicipalityFinancialYearId == year.Id);
        object? before = null;
        if (entity == null)
        {
            if (!string.IsNullOrWhiteSpace(request.RowVersion)) return Conflict(Fail<Guid>("Configuration does not yet exist; reload and retry."));
            entity = new C88MunicipalityConfiguration { MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalityFinancialYearId = year.Id, CreatedByUserId = session.User!.Id };
            context.C88MunicipalityConfigurations.Add(entity);
        }
        else
        {
            if (!TryVersion(request.RowVersion, out var rowVersion)) return BadRequest(Fail<Guid>("A valid RowVersion is required."));
            context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
            before = new { entity.C88CatalogueVersionId, entity.IsEnabled, entity.EffectiveFrom, entity.EffectiveTo };
        }
        entity.C88CatalogueVersionId = version.Id; entity.IsEnabled = request.IsEnabled; entity.EffectiveFrom = Utc(request.EffectiveFrom); entity.EffectiveTo = NullableUtc(request.EffectiveTo);
        Audit(entity.PublicId, "Configure", before, new { FinancialYearPublicId = year.PublicId, CatalogueVersionPublicId = version.PublicId, entity.IsEnabled, entity.EffectiveFrom, entity.EffectiveTo, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The Circular 88 configuration changed before this update. Reload and retry.");
    }

    [HttpPut("plans")]
    public async Task<ActionResult<ApiResponse<Guid>>> SavePlan([FromBody] SaveC88IndicatorPlanRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.UPDATE");
        if (session.Error != null) return session.Error;
        if (!TryReason(request.Reason, out var reason)) return BadRequest(Fail<Guid>("Reason is required."));
        var pair = await ConfigurationIndicator(request.ConfigurationPublicId, request.IndicatorPublicId, requireEnabled: true);
        if (pair.Error != null) return BadRequest(Fail<Guid>(pair.Error));
        var validation = ValidatePlan(pair.Indicator!, request);
        if (validation != null) return BadRequest(Fail<Guid>(validation));
        var entity = await context.C88IndicatorPlans.SingleOrDefaultAsync(item => item.C88MunicipalityConfigurationId == pair.Configuration!.Id && item.C88IndicatorId == pair.Indicator!.Id);
        object? before = null;
        if (entity == null)
        {
            if (!string.IsNullOrWhiteSpace(request.RowVersion)) return Conflict(Fail<Guid>("Plan does not yet exist; reload and retry."));
            entity = new C88IndicatorPlan { MunicipalityId = tenantContext.MunicipalityId!.Value, C88MunicipalityConfigurationId = pair.Configuration!.Id, C88IndicatorId = pair.Indicator!.Id, CreatedByUserId = session.User!.Id };
            context.C88IndicatorPlans.Add(entity);
        }
        else
        {
            if (!TryVersion(request.RowVersion, out var rowVersion)) return BadRequest(Fail<Guid>("A valid RowVersion is required."));
            context.Entry(entity).Property(item => item.RowVersion).OriginalValue = rowVersion;
            before = new { entity.BaselineValue, entity.MediumTermTarget, entity.AnnualTarget, entity.MissingDataExplanation, entity.EstimatedAvailability };
        }
        entity.BaselineValue = Clean(request.BaselineValue, 1024); entity.MediumTermTarget = Clean(request.MediumTermTarget, 1024);
        entity.AnnualTarget = Clean(request.AnnualTarget, 1024); entity.MissingDataExplanation = Clean(request.MissingDataExplanation, 4000); entity.EstimatedAvailability = NullableUtc(request.EstimatedAvailability);
        Audit(entity.PublicId, "SavePlan", before, new { pair.Indicator!.PublicId, entity.BaselineValue, entity.MediumTermTarget, entity.AnnualTarget, entity.MissingDataExplanation, entity.EstimatedAvailability, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The plan changed before this update. Reload and retry.");
    }

    [HttpPost("calendars")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCalendar([FromBody] SaveC88ReportingCalendarRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_WORKFLOW");
        if (session.Error != null) return session.Error;
        if (!TryCode(request.Code, out var code) || !TryText(request.Name, 240, out var name) || !TryReason(request.Reason, out var reason)
            || request.OpensAt == default || request.ClosesAt < request.OpensAt || request.DueAt < request.OpensAt)
            return BadRequest(Fail<Guid>("Code, name, reason and a valid reporting window are required."));
        var configuration = await context.C88MunicipalityConfigurations.SingleOrDefaultAsync(item => item.PublicId == request.ConfigurationPublicId);
        if (configuration == null) return BadRequest(Fail<Guid>("Configuration not found."));
        var reportType = await ItemOfKind(configuration.C88CatalogueVersionId, request.ReportTypePublicId, C88CatalogueItemKind.ReportType);
        if (reportType == null) return BadRequest(Fail<Guid>("Report type must belong to the configured edition."));
        ReportingPeriod? period = null;
        if (request.ReportingPeriodPublicId.HasValue)
        {
            period = await context.ReportingPeriods.SingleOrDefaultAsync(item => item.PublicId == request.ReportingPeriodPublicId && item.MunicipalityFinancialYearId == configuration.MunicipalityFinancialYearId);
            if (period == null) return BadRequest(Fail<Guid>("Reporting period must belong to the configured financial year."));
        }
        var entity = new C88ReportingCalendar { MunicipalityId = tenantContext.MunicipalityId!.Value, C88MunicipalityConfigurationId = configuration.Id, ReportTypeItemId = reportType.Id, ReportingPeriodId = period?.Id, Code = code, Name = name, OpensAt = Utc(request.OpensAt), ClosesAt = Utc(request.ClosesAt), DueAt = Utc(request.DueAt), IsActive = request.IsActive };
        context.C88ReportingCalendars.Add(entity);
        Audit(entity.PublicId, "CreateCalendar", null, new { ConfigurationPublicId = configuration.PublicId, ReportTypePublicId = reportType.PublicId, ReportingPeriodPublicId = period?.PublicId, entity.Code, entity.OpensAt, entity.ClosesAt, entity.DueAt, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The reporting calendar conflicts with an existing code.");
    }

    [HttpPost("assignments")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAssignment([FromBody] SaveC88AssignmentRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_ASSIGNMENTS");
        if (session.Error != null) return session.Error;
        if (!TryReason(request.Reason, out var reason) || !Enum.IsDefined(request.Role) || request.EffectiveFrom == default || request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            return BadRequest(Fail<Guid>("Role, valid effective period and reason are required."));
        var pair = await ConfigurationIndicator(request.ConfigurationPublicId, request.IndicatorPublicId, requireEnabled: false);
        if (pair.Error != null) return BadRequest(Fail<Guid>(pair.Error));
        var employee = await context.MunicipalEmployees.SingleOrDefaultAsync(item => item.PublicId == request.EmployeePublicId && item.IsActive);
        if (employee == null) return BadRequest(Fail<Guid>("Active municipal employee not found."));
        if (request.Role == C88AssignmentRole.PrimaryCapturer && request.IsActive && !request.EffectiveTo.HasValue
            && await context.C88Assignments.AnyAsync(item => item.C88MunicipalityConfigurationId == pair.Configuration!.Id && item.C88IndicatorId == pair.Indicator!.Id && item.Role == C88AssignmentRole.PrimaryCapturer && item.IsActive && item.EffectiveTo == null))
            return Conflict(Fail<Guid>("Only one active Primary Capturer is allowed for this financial-year indicator context."));
        var entity = new C88Assignment { MunicipalityId = tenantContext.MunicipalityId!.Value, C88MunicipalityConfigurationId = pair.Configuration!.Id, C88IndicatorId = pair.Indicator!.Id, MunicipalEmployeeId = employee.Id, Role = request.Role, EffectiveFrom = Utc(request.EffectiveFrom), EffectiveTo = NullableUtc(request.EffectiveTo), IsActive = request.IsActive };
        context.C88Assignments.Add(entity);
        Audit(entity.PublicId, "CreateAssignment", null, new { ConfigurationPublicId = pair.Configuration.PublicId, IndicatorPublicId = pair.Indicator.PublicId, EmployeePublicId = employee.PublicId, entity.Role, entity.EffectiveFrom, entity.EffectiveTo, entity.IsActive, Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The assignment conflicts with an existing active assignment.");
    }

    [HttpPost("workflows")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateWorkflow([FromBody] SaveC88WorkflowRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_WORKFLOW");
        if (session.Error != null) return session.Error;
        if (!TryReason(request.Reason, out var reason) || request.EffectiveFrom == default || request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            return BadRequest(Fail<Guid>("A valid effective period and reason are required."));
        var stages = request.Stages ?? [];
        if (stages.Length != 3 || stages.Select(item => item.Sequence).Distinct().Count() != 3 || stages.Any(item => item.Sequence <= 0 || !item.IsActive || !TryText(item.Name, 160, out _))
            || stages.SingleOrDefault(item => item.Kind == C88WorkflowStageKind.Capturer)?.RequiredRole != C88AssignmentRole.PrimaryCapturer
            || stages.SingleOrDefault(item => item.Kind == C88WorkflowStageKind.ReviewerVerifier)?.RequiredRole != C88AssignmentRole.ReviewerVerifier
            || stages.SingleOrDefault(item => item.Kind == C88WorkflowStageKind.FinalSubmission)?.RequiredRole != C88AssignmentRole.FinalSubmitter)
            return BadRequest(Fail<Guid>("Workflow requires one active Capturer, Reviewer/Verifier and Final Submission stage with the corresponding assignment roles."));
        var ordered = stages.OrderBy(item => item.Sequence).ToArray();
        if (ordered[0].Kind != C88WorkflowStageKind.Capturer || ordered[1].Kind != C88WorkflowStageKind.ReviewerVerifier || ordered[2].Kind != C88WorkflowStageKind.FinalSubmission)
            return BadRequest(Fail<Guid>("Workflow stages must progress from Capturer to Reviewer/Verifier to Final Submission."));
        var configuration = await context.C88MunicipalityConfigurations.SingleOrDefaultAsync(item => item.PublicId == request.ConfigurationPublicId);
        if (configuration == null) return BadRequest(Fail<Guid>("Configuration not found."));
        C88WorkflowDefinition? previous = null;
        if (request.PreviousWorkflowPublicId.HasValue)
        {
            previous = await context.C88WorkflowDefinitions.SingleOrDefaultAsync(item => item.PublicId == request.PreviousWorkflowPublicId && item.C88MunicipalityConfigurationId == configuration.Id && item.IsCurrent);
            if (previous == null || !TryVersion(request.PreviousWorkflowRowVersion, out var previousVersion)) return Conflict(Fail<Guid>("The current workflow or RowVersion is stale."));
            context.Entry(previous).Property(item => item.RowVersion).OriginalValue = previousVersion;
        }
        else if (await context.C88WorkflowDefinitions.AnyAsync(item => item.C88MunicipalityConfigurationId == configuration.Id && item.IsCurrent))
            return Conflict(Fail<Guid>("A current workflow exists; create a successor using its PublicId and RowVersion."));
        var entity = new C88WorkflowDefinition { MunicipalityId = tenantContext.MunicipalityId!.Value, C88MunicipalityConfigurationId = configuration.Id, PreviousVersionId = previous?.Id, VersionNumber = (previous?.VersionNumber ?? 0) + 1, EffectiveFrom = Utc(request.EffectiveFrom), EffectiveTo = NullableUtc(request.EffectiveTo), CreatedByUserId = session.User!.Id };
        foreach (var stage in ordered) entity.Stages.Add(new C88WorkflowStage { MunicipalityId = entity.MunicipalityId, Sequence = stage.Sequence, Kind = stage.Kind, Name = stage.Name.Trim(), RequiredRole = stage.RequiredRole, IsActive = true });
        if (previous != null) { previous.IsCurrent = false; previous.IsActive = false; previous.EffectiveTo ??= entity.EffectiveFrom; }
        context.C88WorkflowDefinitions.Add(entity);
        Audit(entity.PublicId, "CreateWorkflowVersion", null, new { configuration.PublicId, entity.VersionNumber, PreviousPublicId = previous?.PublicId, Stages = ordered.Select(item => new { item.Sequence, item.Kind, item.Name, item.RequiredRole }), Reason = reason }, session.User!.Id);
        return await SaveId(entity.PublicId, "The workflow changed before this successor was saved.");
    }

    [HttpPost("mappings")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateMapping([FromBody] SaveC88MappingRequest request)
    {
        var session = await SessionAsync<Guid>("C88_INDICATOR.MANAGE_MAPPING");
        if (session.Error != null) return session.Error;
        if (!TryReason(request.Reason, out var reason) || !Enum.IsDefined(request.MappingType)) return BadRequest(Fail<Guid>("Mapping type and reason are required."));
        var pair = await ConfigurationIndicator(request.ConfigurationPublicId, request.IndicatorPublicId, requireEnabled: false);
        if (pair.Error != null) return BadRequest(Fail<Guid>(pair.Error));
        var target = await context.OpmsTargets.SingleOrDefaultAsync(item => item.PublicId == request.OpmsTargetPublicId && !item.IsWithdrawn);
        if (target == null) return BadRequest(Fail<Guid>("OPMS target not found in this municipality."));
        var entity = new C88OpmsMapping { MunicipalityId = tenantContext.MunicipalityId!.Value, C88MunicipalityConfigurationId = pair.Configuration!.Id, C88IndicatorId = pair.Indicator!.Id, OpmsTargetId = target.Id, MappingType = request.MappingType, Reason = reason, IsActive = request.IsActive, CreatedByUserId = session.User!.Id };
        context.C88OpmsMappings.Add(entity);
        Audit(entity.PublicId, "CreateMapping", null, new { ConfigurationPublicId = pair.Configuration.PublicId, IndicatorPublicId = pair.Indicator.PublicId, OpmsTargetPublicId = target.PublicId, entity.MappingType, entity.IsActive, Reason = reason }, session.User.Id);
        return await SaveId(entity.PublicId, "This OPMS/Circular 88 mapping already exists.");
    }

    [HttpPost("reports")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateReportVersion([FromBody] CreateC88ReportVersionRequest request)
    {
        var session = await SessionAsync<Guid>("C88_REPORT.CREATE");
        if (session.Error != null) return session.Error;
        if (!TryReason(request.Reason, out var reason)) return BadRequest(Fail<Guid>("Reason is required."));
        var pair = await ConfigurationIndicator(request.ConfigurationPublicId, request.IndicatorPublicId, requireEnabled: true);
        if (pair.Error != null) return BadRequest(Fail<Guid>(pair.Error));
        if (!await HasAssignment(session.User!, pair.Configuration!.Id, pair.Indicator!.Id, C88AssignmentRole.PrimaryCapturer, C88AssignmentRole.Contributor))
            return Forbidden<Guid>("An effective Circular 88 Capturer or Contributor assignment is required.");
        var calendar = await context.C88ReportingCalendars.SingleOrDefaultAsync(item => item.PublicId == request.CalendarPublicId && item.C88MunicipalityConfigurationId == pair.Configuration.Id && item.IsActive);
        if (calendar == null) return BadRequest(Fail<Guid>("Active reporting calendar not found for this configuration."));
        var now = DateTime.UtcNow;
        if (now < calendar.OpensAt || now > calendar.ClosesAt) return Conflict(Fail<Guid>("The Circular 88 reporting window is closed."));
        var workflowDefinition = await context.C88WorkflowDefinitions.Include(item => item.Stages).SingleOrDefaultAsync(item => item.C88MunicipalityConfigurationId == pair.Configuration.Id && item.IsCurrent && item.IsActive);
        if (workflowDefinition == null) return Conflict(Fail<Guid>("Configure an active Circular 88 workflow before capturing reports."));
        var firstStage = workflowDefinition.Stages.Where(item => item.IsActive).OrderBy(item => item.Sequence).FirstOrDefault();
        if (firstStage?.Kind != C88WorkflowStageKind.Capturer) return Conflict(Fail<Guid>("The active Circular 88 workflow has no Capturer stage."));

        C88IndicatorReport? previous = null;
        if (request.PreviousReportPublicId.HasValue)
        {
            previous = await context.C88IndicatorReports.SingleOrDefaultAsync(item => item.PublicId == request.PreviousReportPublicId && item.IsCurrent);
            if (previous == null || previous.C88MunicipalityConfigurationId != pair.Configuration.Id || previous.C88ReportingCalendarId != calendar.Id || previous.C88IndicatorId != pair.Indicator.Id || !TryVersion(request.PreviousReportRowVersion, out var previousVersion))
                return Conflict(Fail<Guid>("The current report version or RowVersion is stale."));
            if (previous.State is C88ReportState.Submitted or C88ReportState.Verified) return Conflict(Fail<Guid>("Return the in-flight report to rework before creating a successor version."));
            context.Entry(previous).Property(item => item.RowVersion).OriginalValue = previousVersion;
        }
        else if (await context.C88IndicatorReports.AnyAsync(item => item.C88ReportingCalendarId == calendar.Id && item.C88IndicatorId == pair.Indicator.Id && item.IsCurrent))
            return Conflict(Fail<Guid>("A current report exists; create a successor using its PublicId and RowVersion."));

        var elements = await context.C88DataElements.Where(item => item.C88IndicatorId == pair.Indicator.Id).OrderBy(item => item.Sequence).ToArrayAsync();
        var valueInputs = request.DataElementValues ?? [];
        var suppliedValues = valueInputs.GroupBy(item => item.DataElementPublicId).ToDictionary(group => group.Key, group => group.ToArray());
        if (suppliedValues.Any(item => item.Value.Length != 1) || suppliedValues.Keys.Any(key => elements.All(item => item.PublicId != key)))
            return BadRequest(Fail<Guid>("Data-element values must be unique and belong to the selected indicator."));
        foreach (var element in elements)
        {
            suppliedValues.TryGetValue(element.PublicId, out var values);
            var value = values?.SingleOrDefault();
            if (element.IsRequired && (value == null || string.IsNullOrWhiteSpace(value.Value)) && (value == null || string.IsNullOrWhiteSpace(value.MissingDataExplanation) || !value.EstimatedAvailability.HasValue))
                return BadRequest(Fail<Guid>($"{element.Code} requires a value or both a missing-data explanation and estimated availability."));
            if (value != null && !string.IsNullOrWhiteSpace(value.Value) && !ValueIsValid(value.Value, element.ValueType))
                return BadRequest(Fail<Guid>($"{element.Code} is not valid for {element.ValueType}."));
        }
        var questions = await context.C88ComplianceQuestions.Include(item => item.ResponseTypeItem).Where(item => item.C88CatalogueVersionId == pair.Configuration.C88CatalogueVersionId && item.ReportTypeItemId == calendar.ReportTypeItemId && item.IsActive).ToArrayAsync();
        var responseInputs = request.ComplianceResponses ?? [];
        var suppliedResponses = responseInputs.GroupBy(item => item.QuestionPublicId).ToDictionary(group => group.Key, group => group.ToArray());
        if (suppliedResponses.Any(item => item.Value.Length != 1) || suppliedResponses.Keys.Any(key => questions.All(item => item.PublicId != key)))
            return BadRequest(Fail<Guid>("Compliance responses must be unique and belong to the selected report type."));
        foreach (var question in questions.Where(item => item.IsRequired))
            if (!suppliedResponses.TryGetValue(question.PublicId, out var responses) || string.IsNullOrWhiteSpace(responses[0].Response))
                return BadRequest(Fail<Guid>($"A response is required for {question.Code}."));
        foreach (var question in questions)
            if (suppliedResponses.TryGetValue(question.PublicId, out var typedResponses) && !string.IsNullOrWhiteSpace(typedResponses[0].Response)
                && !ResponseIsValid(typedResponses[0].Response!, question.ResponseTypeItem.Code))
                return BadRequest(Fail<Guid>($"The response for {question.Code} is not valid for response type {question.ResponseTypeItem.Code}."));

        var calculated = Calculate(pair.Indicator, elements, valueInputs, out var calculationError);
        if (calculationError != null) return BadRequest(Fail<Guid>(calculationError));
        var report = new C88IndicatorReport
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, ReportFamilyId = previous?.ReportFamilyId ?? Guid.NewGuid(),
            C88MunicipalityConfigurationId = pair.Configuration.Id, C88ReportingCalendarId = calendar.Id, C88IndicatorId = pair.Indicator.Id,
            C88WorkflowDefinitionId = workflowDefinition.Id,
            PreviousVersionId = previous?.Id, VersionNumber = (previous?.VersionNumber ?? 0) + 1, State = C88ReportState.Draft,
            CurrentStageSequence = firstStage.Sequence, CalculatedValue = calculated, MissingDataExplanation = Clean(request.MissingDataExplanation, 4000),
            EstimatedAvailability = NullableUtc(request.EstimatedAvailability), CreatedByUserId = session.User!.Id
        };
        foreach (var element in elements)
        {
            var input = valueInputs.FirstOrDefault(item => item.DataElementPublicId == element.PublicId);
            if (input != null) report.DataElementValues.Add(new C88DataElementValue { MunicipalityId = report.MunicipalityId, C88DataElementId = element.Id, Value = Clean(input.Value, 1024), MissingDataExplanation = Clean(input.MissingDataExplanation, 4000), EstimatedAvailability = NullableUtc(input.EstimatedAvailability) });
        }
        foreach (var question in questions)
        {
            var input = responseInputs.FirstOrDefault(item => item.QuestionPublicId == question.PublicId);
            if (input != null) report.ComplianceResponses.Add(new C88ComplianceResponse { MunicipalityId = report.MunicipalityId, C88ComplianceQuestionId = question.Id, Response = Clean(input.Response, 4000), Comment = Clean(input.Comment, 4000) });
        }
        report.WorkflowActions.Add(NewAction(report, firstStage.Sequence, firstStage.Sequence, C88WorkflowActionKind.Created, reason, session.User.Id));
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (previous != null) previous.IsCurrent = false;
            context.C88IndicatorReports.Add(report);
            Audit(report.PublicId, "CreateReportVersion", null, new { report.ReportFamilyId, report.VersionNumber, PreviousPublicId = previous?.PublicId, ConfigurationPublicId = pair.Configuration.PublicId, CalendarPublicId = calendar.PublicId, IndicatorPublicId = pair.Indicator.PublicId, report.CalculatedValue, Values = report.DataElementValues.Count, Responses = report.ComplianceResponses.Count, Reason = reason }, session.User.Id);
            await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(); return Conflict(Fail<Guid>("The report changed before this successor was saved.")); }
        catch (DbUpdateException) { await transaction.RollbackAsync(); return Conflict(Fail<Guid>("The report conflicts with an existing current version.")); }
        return OkId(report.PublicId);
    }

    [HttpPost("reports/{publicId:guid}/submit")]
    public Task<ActionResult<ApiResponse<Guid>>> Submit(Guid publicId, [FromBody] C88WorkflowCommandRequest request) =>
        ExecuteWorkflow(publicId, request, "C88_REPORT.SUBMIT", C88AssignmentRole.PrimaryCapturer, C88ReportState.Draft, C88ReportState.Submitted, C88WorkflowActionKind.Submitted, C88WorkflowStageKind.ReviewerVerifier);

    [HttpPost("reports/{publicId:guid}/verify")]
    public Task<ActionResult<ApiResponse<Guid>>> Verify(Guid publicId, [FromBody] C88WorkflowCommandRequest request) =>
        ExecuteWorkflow(publicId, request, "C88_REPORT.VERIFY", C88AssignmentRole.ReviewerVerifier, C88ReportState.Submitted, C88ReportState.Verified, C88WorkflowActionKind.Verified, C88WorkflowStageKind.FinalSubmission);

    [HttpPost("reports/{publicId:guid}/return")]
    public async Task<ActionResult<ApiResponse<Guid>>> Return(Guid publicId, [FromBody] C88WorkflowCommandRequest request)
    {
        var loaded = await LoadReportCommand(publicId, request, "C88_REPORT.RETURN");
        if (loaded.Error != null) return loaded.Error;
        var report = loaded.Report!;
        var requiredRole = report.State == C88ReportState.Submitted ? C88AssignmentRole.ReviewerVerifier : C88AssignmentRole.FinalSubmitter;
        if (report.State is not (C88ReportState.Submitted or C88ReportState.Verified)) return Conflict(Fail<Guid>("Only submitted or verified reports can be returned."));
        if (!await HasAssignment(loaded.User!, report.C88MunicipalityConfigurationId, report.C88IndicatorId, requiredRole)) return Forbidden<Guid>("The effective workflow assignment is required.");
        var first = await ReportStage(report.C88WorkflowDefinitionId, C88WorkflowStageKind.Capturer);
        if (first == null) return Conflict(Fail<Guid>("The current workflow has no Capturer stage."));
        var from = report.CurrentStageSequence; report.State = C88ReportState.Rework; report.CurrentStageSequence = first.Sequence;
        report.WorkflowActions.Add(NewAction(report, from, first.Sequence, C88WorkflowActionKind.Returned, loaded.Reason!, loaded.User!.Id));
        Audit(report.PublicId, "Return", new { State = loaded.OriginalState, Stage = from }, new { report.State, Stage = first.Sequence, Reason = loaded.Reason }, loaded.User.Id);
        return await SaveId(report.PublicId, "The report changed before it was returned.");
    }

    [HttpPost("reports/{publicId:guid}/final-submit")]
    public async Task<ActionResult<ApiResponse<Guid>>> FinalSubmit(Guid publicId, [FromBody] C88WorkflowCommandRequest request)
    {
        var result = await ExecuteWorkflow(publicId, request, "C88_REPORT.FINAL_SUBMIT", C88AssignmentRole.FinalSubmitter, C88ReportState.Verified, C88ReportState.FinalSubmitted, C88WorkflowActionKind.FinalSubmitted, null);
        return result;
    }

    private async Task<ActionResult<ApiResponse<Guid>>> ExecuteWorkflow(Guid publicId, C88WorkflowCommandRequest request, string permission, C88AssignmentRole role, C88ReportState expected, C88ReportState next, C88WorkflowActionKind action, C88WorkflowStageKind? nextStageKind)
    {
        var loaded = await LoadReportCommand(publicId, request, permission);
        if (loaded.Error != null) return loaded.Error;
        var report = loaded.Report!;
        if (report.State != expected && !(action == C88WorkflowActionKind.Submitted && report.State == C88ReportState.Rework)) return Conflict(Fail<Guid>($"Report state must be {expected}."));
        if (!await HasAssignment(loaded.User!, report.C88MunicipalityConfigurationId, report.C88IndicatorId, role)) return Forbidden<Guid>("The effective Circular 88 workflow assignment is required.");
        var from = report.CurrentStageSequence;
        var to = from;
        if (nextStageKind.HasValue)
        {
            var stage = await ReportStage(report.C88WorkflowDefinitionId, nextStageKind.Value);
            if (stage == null) return Conflict(Fail<Guid>("The active Circular 88 workflow is incomplete."));
            to = stage.Sequence;
        }
        report.State = next; report.CurrentStageSequence = to;
        if (next == C88ReportState.FinalSubmitted) { report.FinalSubmittedAt = DateTime.UtcNow; report.FinalSubmittedByUserId = loaded.User!.Id; }
        report.WorkflowActions.Add(NewAction(report, from, to, action, loaded.Reason!, loaded.User!.Id));
        Audit(report.PublicId, action.ToString(), new { State = loaded.OriginalState, Stage = from }, new { report.State, Stage = to, Reason = loaded.Reason }, loaded.User.Id);
        return await SaveId(report.PublicId, "The report changed before this workflow action was saved.");
    }

    private IQueryable<C88IndicatorReport> ReportQuery() => context.C88IndicatorReports
        .Include(item => item.Configuration).Include(item => item.Calendar).Include(item => item.Indicator)
        .Include(item => item.DataElementValues).ThenInclude(item => item.DataElement)
        .Include(item => item.ComplianceResponses).ThenInclude(item => item.ComplianceQuestion)
        .Include(item => item.WorkflowActions);

    private async Task<(ApplicationUser? User, C88IndicatorReport? Report, C88ReportState OriginalState, string? Reason, ActionResult<ApiResponse<Guid>>? Error)> LoadReportCommand(Guid publicId, C88WorkflowCommandRequest request, string permission)
    {
        var session = await SessionAsync<Guid>(permission);
        if (session.Error != null) return (session.User, null, default, null, session.Error);
        if (!TryVersion(request.RowVersion, out var rowVersion) || !TryReason(request.Reason, out var reason))
            return (session.User, null, default, null, BadRequest(Fail<Guid>("A valid RowVersion and reason are required.")));
        var report = await ReportQuery().SingleOrDefaultAsync(item => item.PublicId == publicId && item.IsCurrent);
        if (report == null) return (session.User, null, default, null, NotFound(Fail<Guid>("Current Circular 88 report not found.")));
        if (!report.Configuration.IsEnabled) return (session.User, null, default, null, Conflict(Fail<Guid>("Circular 88 is disabled for this municipality financial year.")));
        context.Entry(report).Property(item => item.RowVersion).OriginalValue = rowVersion;
        return (session.User, report, report.State, reason, null);
    }

    private async Task<C88WorkflowStage?> ReportStage(long workflowDefinitionId, C88WorkflowStageKind kind) =>
        await context.C88WorkflowStages.SingleOrDefaultAsync(item => item.C88WorkflowDefinitionId == workflowDefinitionId && item.IsActive && item.Kind == kind);

    private async Task<bool> HasAssignment(ApplicationUser user, long configurationId, long indicatorId, params C88AssignmentRole[] roles)
    {
        var employeeId = await context.MunicipalEmployees.Where(item => item.IdentityUserId == user.Id && item.IsActive).Select(item => (long?)item.Id).SingleOrDefaultAsync();
        if (!employeeId.HasValue) return false;
        var today = DateTime.UtcNow;
        return await context.C88Assignments.AnyAsync(item => item.C88MunicipalityConfigurationId == configurationId && item.C88IndicatorId == indicatorId
            && item.MunicipalEmployeeId == employeeId && roles.Contains(item.Role) && item.IsActive && item.EffectiveFrom <= today && (!item.EffectiveTo.HasValue || item.EffectiveTo >= today));
    }

    private async Task<(C88MunicipalityConfiguration? Configuration, C88Indicator? Indicator, string? Error)> ConfigurationIndicator(Guid configurationPublicId, Guid indicatorPublicId, bool requireEnabled)
    {
        var configuration = await context.C88MunicipalityConfigurations.SingleOrDefaultAsync(item => item.PublicId == configurationPublicId);
        if (configuration == null || requireEnabled && !configuration.IsEnabled) return (configuration, null, requireEnabled ? "Circular 88 is not enabled for this municipality financial year." : "Configuration not found.");
        var indicator = await context.C88Indicators.SingleOrDefaultAsync(item => item.PublicId == indicatorPublicId && item.C88CatalogueVersionId == configuration.C88CatalogueVersionId && item.IsActive);
        return indicator == null ? (configuration, null, "Indicator must be active in the configured catalogue edition.") : (configuration, indicator, null);
    }

    private async Task<C88CatalogueVersion?> EditableVersion(Guid publicId) =>
        await context.C88CatalogueVersions.SingleOrDefaultAsync(item => item.PublicId == publicId && !item.IsPublished && item.IsActive);

    private async Task<C88CatalogueItem?> ItemOfKind(long versionId, Guid publicId, C88CatalogueItemKind kind) =>
        await context.C88CatalogueItems.SingleOrDefaultAsync(item => item.C88CatalogueVersionId == versionId && item.PublicId == publicId && item.Kind == kind && item.IsActive);

    private async Task<(C88CatalogueItem? Sector, C88CatalogueItem? Outcome, C88CatalogueItem? Type, string? Error)> ResolveItems(long versionId, Guid? sectorId, Guid? outcomeId, Guid? typeId)
    {
        var sector = sectorId.HasValue ? await ItemOfKind(versionId, sectorId.Value, C88CatalogueItemKind.Sector) : null;
        var outcome = outcomeId.HasValue ? await ItemOfKind(versionId, outcomeId.Value, C88CatalogueItemKind.Outcome) : null;
        var type = typeId.HasValue ? await ItemOfKind(versionId, typeId.Value, C88CatalogueItemKind.IndicatorType) : null;
        if (sectorId.HasValue && sector == null || outcomeId.HasValue && outcome == null || typeId.HasValue && type == null)
            return (sector, outcome, type, "Sector, outcome and indicator type must belong to the selected catalogue edition.");
        if (outcome?.ParentItemId.HasValue == true && outcome.ParentItemId != sector?.Id)
            return (sector, outcome, type, "The selected outcome does not belong to the selected sector.");
        return (sector, outcome, type, null);
    }

    private static string? ValidatePlan(C88Indicator indicator, SaveC88IndicatorPlanRequest request)
    {
        var baseline = Clean(request.BaselineValue, 1024); var medium = Clean(request.MediumTermTarget, 1024); var annual = Clean(request.AnnualTarget, 1024);
        var missing = Clean(request.MissingDataExplanation, 4000);
        if ((indicator.RequiresBaseline && baseline == null || indicator.RequiresMediumTermTarget && medium == null || indicator.RequiresAnnualTarget && annual == null)
            && (missing == null || !request.EstimatedAvailability.HasValue)) return "Required planning values need either a valid value or both a missing-data explanation and estimated availability.";
        foreach (var value in new[] { baseline, medium, annual }.Where(value => value != null))
            if (!ValueIsValid(value!, indicator.ValueType)) return $"Planning value is not valid for {indicator.ValueType}.";
        return null;
    }

    private static string? Calculate(C88Indicator indicator, C88DataElement[] elements, C88DataElementValueInput[] inputs, out string? error)
    {
        error = null;
        var ordered = elements.OrderBy(item => item.Sequence).Select(item => inputs.FirstOrDefault(input => input.DataElementPublicId == item.PublicId)?.Value).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (indicator.CalculationOperator == C88ControlledCalculationOperator.None) return ordered.Length == 1 ? ordered[0]!.Trim() : null;
        var values = new List<decimal>();
        foreach (var value in ordered)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) { error = "Controlled calculation inputs must be numeric."; return null; }
            values.Add(parsed);
        }
        if (values.Count == 0) return null;
        decimal result;
        switch (indicator.CalculationOperator)
        {
            case C88ControlledCalculationOperator.Sum: result = values.Sum(); break;
            case C88ControlledCalculationOperator.Average: result = values.Average(); break;
            case C88ControlledCalculationOperator.Difference when values.Count >= 2: result = values[0] - values[1]; break;
            case C88ControlledCalculationOperator.Ratio when values.Count >= 2 && values[1] != 0: result = values[0] / values[1]; break;
            case C88ControlledCalculationOperator.Percentage when values.Count >= 2 && values[1] != 0: result = values[0] / values[1] * 100m; break;
            default: error = "The controlled calculation requires enough non-zero numeric operands."; return null;
        }
        return result.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    private static bool ValueIsValid(string value, C88ValueType type) => type switch
    {
        C88ValueType.Decimal => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
        C88ValueType.Integer => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        C88ValueType.Percentage => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) && number is >= 0 and <= 100,
        C88ValueType.Boolean => bool.TryParse(value, out _),
        C88ValueType.Text => value.Trim().Length is > 0 and <= 1024,
        _ => false
    };

    private static bool ResponseIsValid(string value, string responseTypeCode) => responseTypeCode.Trim().ToUpperInvariant() switch
    {
        "BOOLEAN" => bool.TryParse(value, out _),
        "YES_NO" or "YESNO" => value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Trim().Equals("no", StringComparison.OrdinalIgnoreCase),
        "INTEGER" => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        "DECIMAL" or "NUMBER" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
        "DATE" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _),
        _ => value.Trim().Length is > 0 and <= 4000
    };

    private static C88WorkflowAction NewAction(C88IndicatorReport report, int from, int to, C88WorkflowActionKind action, string reason, string userId) => new()
    {
        MunicipalityId = report.MunicipalityId, FromStageSequence = from, ToStageSequence = to, Action = action, Reason = reason, ActorUserId = userId,
        SnapshotJson = JsonSerializer.Serialize(new { report.PublicId, report.ReportFamilyId, report.VersionNumber, report.State, report.CurrentStageSequence, report.CalculatedValue })
    };

    private async Task<(ApplicationUser? User, ActionResult<ApiResponse<T>>? Error)> SessionAsync<T>(string permission)
    {
        var user = await CurrentUserAsync();
        if (user == null) return (null, Unauthorized(Fail<T>("User not found.")));
        if (!tenantContext.MunicipalityId.HasValue) return (user, BadRequest(Fail<T>("Municipality context is required.")));
        var decision = await accessControl.CheckPermissionAsync(user, permission, Scope());
        return decision.Allowed ? (user, null) : (user, Forbidden<T>(decision.Reason));
    }

    private Task<ApplicationUser?> CurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(userId);
    }

    private AccessScopeContext Scope() => new(MunicipalityId: tenantContext.MunicipalityId);
    private void Audit(Guid publicId, string action, object? before, object after, string userId) => workflow.QueueAuditTrail("C88", publicId.ToString(), action, before, after, userId, PerformanceApiSupport.GetIpAddress(HttpContext));
    private ActionResult<ApiResponse<Guid>> OkId(Guid id) => Ok(new ApiResponse<Guid>(true, id));

    private async Task<ActionResult<ApiResponse<Guid>>> SaveId(Guid id, string conflict)
    {
        try { await context.SaveChangesAsync(); return OkId(id); }
        catch (DbUpdateConcurrencyException) { context.ChangeTracker.Clear(); return Conflict(Fail<Guid>(conflict)); }
        catch (DbUpdateException) { context.ChangeTracker.Clear(); return Conflict(Fail<Guid>(conflict)); }
    }

    private static bool ValidateCatalogueVersion(SaveC88CatalogueVersionRequest request, out string code, out string name, out string reason, out string? error)
    {
        code = string.Empty; name = string.Empty; reason = string.Empty; error = null;
        if (!TryCode(request.Code, out code) || !TryText(request.Name, 240, out name) || !TryReason(request.Reason, out reason)) { error = "Code, name and reason are required."; return false; }
        if (request.EditionDate == default || request.EffectiveFrom == default || request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom) { error = "A valid edition and effective period are required."; return false; }
        return true;
    }

    private static bool TryCode(string? value, out string normalized)
    {
        normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length is > 0 and <= 80 && normalized.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.');
    }
    private static bool TryText(string? value, int maximum, out string normalized) { normalized = value?.Trim() ?? string.Empty; return normalized.Length is > 0 && normalized.Length <= maximum; }
    private static bool TryReason(string? value, out string normalized) { normalized = value?.Trim() ?? string.Empty; return normalized.Length is > 0 and <= 1000; }
    private static string? Clean(string? value, int maximum) { var cleaned = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); return cleaned?.Length > maximum ? cleaned[..maximum] : cleaned; }
    private static bool TryVersion(string? value, out byte[] bytes) { bytes = []; try { if (string.IsNullOrWhiteSpace(value)) return false; bytes = Convert.FromBase64String(value); return bytes.Length > 0; } catch (FormatException) { return false; } }
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static DateTime? NullableUtc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
    private async Task<(bool Manager, long[] IndicatorIds, ActionResult<ApiResponse<PagedResponse<T>>>? Error)> IndicatorVisibility<T>()
    {
        var user = await CurrentUserAsync();
        if (user == null) return (false, [], Unauthorized(Fail<PagedResponse<T>>("User not found.")));
        if (!tenantContext.MunicipalityId.HasValue) return (false, [], BadRequest(Fail<PagedResponse<T>>("Municipality context is required.")));
        var read = await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.READ", Scope());
        if (!read.Allowed) return (false, [], Forbidden<PagedResponse<T>>(read.Reason));
        var manager = (await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.UPDATE", Scope())).Allowed;
        if (manager) return (true, [], null);
        var employeeId = await context.MunicipalEmployees.Where(item => item.IdentityUserId == user.Id && item.IsActive)
            .Select(item => (long?)item.Id).SingleOrDefaultAsync();
        if (!employeeId.HasValue) return (false, [], null);
        var now = DateTime.UtcNow;
        var indicatorIds = await context.C88Assignments.Where(item => item.MunicipalEmployeeId == employeeId.Value && item.IsActive
                && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo >= now))
            .Select(item => item.C88IndicatorId).Distinct().ToArrayAsync();
        return (false, indicatorIds, null);
    }
    private async Task<ActionResult<ApiResponse<PagedResponse<T>>>?> WorkspaceRead<T>()
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized(Fail<PagedResponse<T>>("User not found."));
        if (!tenantContext.MunicipalityId.HasValue) return BadRequest(Fail<PagedResponse<T>>("Municipality context is required."));
        var indicatorRead = await accessControl.CheckPermissionAsync(user, "C88_INDICATOR.READ", Scope());
        var reportRead = await accessControl.CheckPermissionAsync(user, "C88_REPORT.READ", Scope());
        return indicatorRead.Allowed || reportRead.Allowed ? null : Forbidden<PagedResponse<T>>(indicatorRead.Reason);
    }
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> Forbidden<T>(string? message) => StatusCode(StatusCodes.Status403Forbidden, Fail<T>(message ?? "Permission denied."));

    private static C88ConfigurationResponse ToResponse(C88MunicipalityConfiguration item) => new(item.PublicId, item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear.FinancialYear.Code, item.CatalogueVersion.PublicId, item.CatalogueVersion.Code, item.IsEnabled, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion));
    private static C88CatalogueVersionResponse ToResponse(C88CatalogueVersion item) => new(item.PublicId, item.Code, item.Name, item.EditionDate, item.EffectiveFrom, item.EffectiveTo, item.IsPublished, item.IsActive, Convert.ToBase64String(item.RowVersion));
    private static C88CatalogueItemResponse ToResponse(C88CatalogueItem item) => new(item.PublicId, item.CatalogueVersion.PublicId, item.Kind, item.Code, item.Name, item.Description, item.ParentItem?.PublicId, item.DisplayOrder, item.IsActive, Convert.ToBase64String(item.RowVersion));
    private static C88IndicatorResponse ToResponse(C88Indicator item) => new(item.PublicId, item.CatalogueVersion.PublicId, item.Code, item.Name, item.Definition, item.OfficialTechnicalIndicatorDescription, item.SectorItem?.PublicId, item.OutcomeItem?.PublicId, item.IndicatorTypeItem?.PublicId, item.ValueType, item.CalculationOperator, item.OfficialFormulaText, item.RequiresBaseline, item.RequiresMediumTermTarget, item.RequiresAnnualTarget, item.IsActive, Convert.ToBase64String(item.RowVersion), item.DataElements.OrderBy(value => value.Sequence).Select(value => new C88DataElementResponse(value.PublicId, value.Code, value.Name, value.Description, value.ValueType, value.IsRequired, value.Sequence, Convert.ToBase64String(value.RowVersion))).ToArray(), item.Applicability.Select(value => new C88ApplicabilityResponse(value.PublicId, value.MunicipalCategoryItem.PublicId, value.ReadinessTierItem?.PublicId, value.IsApplicable, value.Notes)).ToArray());
    private static C88ComplianceQuestionResponse ToResponse(C88ComplianceQuestion item) => new(item.PublicId, item.CatalogueVersion.PublicId, item.ReportTypeItem.PublicId, item.ResponseTypeItem.PublicId, item.Code, item.Prompt, item.IsRequired, item.Sequence, item.IsActive, Convert.ToBase64String(item.RowVersion));
    private static C88IndicatorPlanResponse ToResponse(C88IndicatorPlan item) => new(item.PublicId, item.Configuration.PublicId, item.Indicator.PublicId, item.Indicator.Code, item.BaselineValue, item.MediumTermTarget, item.AnnualTarget, item.MissingDataExplanation, item.EstimatedAvailability, Convert.ToBase64String(item.RowVersion));
    private static C88ReportingCalendarResponse ToResponse(C88ReportingCalendar item) => new(item.PublicId, item.Configuration.PublicId, item.ReportTypeItem.PublicId, item.ReportingPeriod?.PublicId, item.Code, item.Name, item.OpensAt, item.ClosesAt, item.DueAt, item.IsActive, Convert.ToBase64String(item.RowVersion));
    private static C88IndicatorReportResponse ToResponse(C88IndicatorReport item) => new(item.PublicId, item.ReportFamilyId, item.VersionNumber, item.IsCurrent, item.Configuration.PublicId, item.Calendar.PublicId, item.Indicator.PublicId, item.Indicator.Code, item.State, item.CurrentStageSequence, item.CalculatedValue, item.MissingDataExplanation, item.EstimatedAvailability, item.CreatedAt, item.FinalSubmittedAt, Convert.ToBase64String(item.RowVersion), item.DataElementValues.Select(value => new C88DataElementValueResponse(value.PublicId, value.DataElement.PublicId, value.Value, value.MissingDataExplanation, value.EstimatedAvailability)).ToArray(), item.ComplianceResponses.Select(value => new C88ComplianceResponseResponse(value.PublicId, value.ComplianceQuestion.PublicId, value.Response, value.Comment)).ToArray(), item.WorkflowActions.OrderBy(value => value.OccurredAt).Select(value => new C88WorkflowActionResponse(value.PublicId, value.FromStageSequence, value.ToStageSequence, value.Action, value.Reason, value.OccurredAt)).ToArray());
    private static C88AssignmentResponse ToResponse(C88Assignment item) => new(item.PublicId, item.Configuration.PublicId, item.Indicator.PublicId, item.MunicipalEmployee.PublicId, $"{item.MunicipalEmployee.FirstName} {item.MunicipalEmployee.LastName}".Trim(), item.Role, item.EffectiveFrom, item.EffectiveTo, item.IsActive, Convert.ToBase64String(item.RowVersion));
    private static C88WorkflowResponse ToResponse(C88WorkflowDefinition item) => new(item.PublicId, item.Configuration.PublicId, item.VersionNumber, item.IsCurrent, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion), item.Stages.OrderBy(value => value.Sequence).Select(value => new C88WorkflowStageResponse(value.PublicId, value.Sequence, value.Kind, value.Name, value.RequiredRole, value.IsActive)).ToArray());
    private static C88MappingResponse ToResponse(C88OpmsMapping item) => new(item.PublicId, item.Configuration.PublicId, item.Indicator.PublicId, item.OpmsTarget.PublicId, item.OpmsTarget.IndicatorNumber, item.MappingType, item.Reason, item.IsActive, Convert.ToBase64String(item.RowVersion));
}

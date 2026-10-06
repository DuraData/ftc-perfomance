using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/masters")]
[Authorize]
public sealed class TenantMastersController(ApplicationDbContext context, ITenantContext tenantContext, IAccessControlService? accessControl = null) : ControllerBase
{
    private static readonly HashSet<string> FinancialYearSortFields = new(StringComparer.OrdinalIgnoreCase) { "createdat", "code", "name", "startdate", "enddate", "status" };
    private static readonly HashSet<string> MunicipalityYearSortFields = new(StringComparer.OrdinalIgnoreCase) { "createdat", "code", "name", "startdate", "effectivefrom", "status", "current" };
    private static readonly HashSet<string> PeriodSortFields = new(StringComparer.OrdinalIgnoreCase) { "createdat", "code", "name", "sequence", "startdate", "enddate", "status" };
    private static readonly HashSet<string> LayerSortFields = new(StringComparer.OrdinalIgnoreCase) { "createdat", "code", "name", "displayorder", "financialyear", "status" };
    private static readonly HashSet<string> AssignmentSortFields = new(StringComparer.OrdinalIgnoreCase) { "effectivefrom", "effectiveto", "position", "department", "unit", "status", "primary" };

    [HttpGet("financial-years")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.READ")]
    public ActionResult<ApiResponse<FinancialYearDto[]>> GetFinancialYears() =>
        StatusCode(StatusCodes.Status410Gone, Fail<FinancialYearDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/financial-years/page."));

    [HttpGet("financial-years/page")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<FinancialYearDto>>>> GetFinancialYearsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null)
    {
        if (!FinancialYearSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<FinancialYearDto>("createdAt, code, name, startDate, endDate, or status");
        var query = context.FinancialYears.AsNoTracking().AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderFinancialYears(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new FinancialYearDto(item.PublicId, item.Code, item.Name, item.StartDate, item.EndDate, item.IsActive, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<FinancialYearDto>>(true, PagedResponse<FinancialYearDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("financial-years")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.CREATE")]
    public async Task<ActionResult<ApiResponse<FinancialYearDto>>> CreateFinancialYear(SaveFinancialYearRequest request)
    {
        if (!tenantContext.IsSystem) return Forbid();
        if (!ValidDates(request.StartDate, request.EndDate)) return BadRequest(Fail<FinancialYearDto>("End date must be on or after start date."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.FinancialYears.AnyAsync(x => x.Code == code)) return Conflict(Fail<FinancialYearDto>("Financial year code already exists."));
        var entity = new FinancialYear { Code = code, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate };
        context.FinancialYears.Add(entity);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<FinancialYearDto>(true, ToDto(entity)));
    }

    [HttpPut("financial-years/{publicId:guid}")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.UPDATE")]
    public async Task<ActionResult<ApiResponse<FinancialYearDto>>> UpdateFinancialYear(Guid publicId, SaveFinancialYearRequest request)
    {
        if (!tenantContext.IsSystem) return Forbid();
        if (!ValidDates(request.StartDate, request.EndDate)) return BadRequest(Fail<FinancialYearDto>("End date must be on or after start date."));
        var entity = await context.FinancialYears.SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<FinancialYearDto>("Financial year not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<FinancialYearDto>("A valid row version is required."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.FinancialYears.AnyAsync(x => x.Id != entity.Id && x.Code == code)) return Conflict(Fail<FinancialYearDto>("Financial year code already exists."));
        entity.Code = code; entity.Name = request.Name.Trim(); entity.StartDate = request.StartDate; entity.EndDate = request.EndDate; entity.IsActive = request.IsActive;
        return await SaveVersioned(entity, ToDto, "Financial year was changed by another user.");
    }

    [HttpGet("municipality-financial-years")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.READ")]
    public ActionResult<ApiResponse<MunicipalityFinancialYearDto[]>> GetMunicipalityFinancialYears() =>
        StatusCode(StatusCodes.Status410Gone, Fail<MunicipalityFinancialYearDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/municipality-financial-years/page."));

    [HttpGet("municipality-financial-years/page")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<MunicipalityFinancialYearDto>>>> GetMunicipalityFinancialYearsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null, [FromQuery] bool? current = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<MunicipalityFinancialYearDto>>();
        if (!MunicipalityYearSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<MunicipalityFinancialYearDto>("createdAt, code, name, startDate, effectiveFrom, status, or current");
        var query = context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear).AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (current.HasValue) query = query.Where(item => item.IsCurrent == current.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.FinancialYear.Code.Contains(term) || item.FinancialYear.Name.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderMunicipalityYears(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new MunicipalityFinancialYearDto(item.PublicId, item.FinancialYear.PublicId, item.FinancialYear.Code, item.FinancialYear.Name, item.IsCurrent, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<MunicipalityFinancialYearDto>>(true, PagedResponse<MunicipalityFinancialYearDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("municipality-financial-years")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.CREATE")]
    public async Task<ActionResult<ApiResponse<MunicipalityFinancialYearDto>>> CreateMunicipalityFinancialYear(SaveMunicipalityFinancialYearRequest request)
    {
        if (!HasTenant()) return TenantRequired<MunicipalityFinancialYearDto>();
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<MunicipalityFinancialYearDto>("Effective-to must be on or after effective-from."));
        var year = await context.FinancialYears.SingleOrDefaultAsync(x => x.PublicId == request.FinancialYearPublicId && x.IsActive);
        if (year == null) return BadRequest(Fail<MunicipalityFinancialYearDto>("Financial year not found or inactive."));
        if (await context.MunicipalityFinancialYears.AnyAsync(x => x.FinancialYearId == year.Id)) return Conflict(Fail<MunicipalityFinancialYearDto>("Financial year is already configured for this municipality."));
        if (request.IsCurrent) await ClearCurrentFinancialYear();
        var entity = new MunicipalityFinancialYear { MunicipalityId = tenantContext.MunicipalityId!.Value, FinancialYearId = year.Id, FinancialYear = year, IsCurrent = request.IsCurrent, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo };
        context.MunicipalityFinancialYears.Add(entity);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<MunicipalityFinancialYearDto>(true, ToDto(entity)));
    }

    [HttpPut("municipality-financial-years/{publicId:guid}")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.UPDATE")]
    public async Task<ActionResult<ApiResponse<MunicipalityFinancialYearDto>>> UpdateMunicipalityFinancialYear(Guid publicId, UpdateMunicipalityFinancialYearRequest request)
    {
        if (!HasTenant()) return TenantRequired<MunicipalityFinancialYearDto>();
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<MunicipalityFinancialYearDto>("Effective-to must be on or after effective-from."));
        var entity = await context.MunicipalityFinancialYears.Include(x => x.FinancialYear).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<MunicipalityFinancialYearDto>("Municipality financial year not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<MunicipalityFinancialYearDto>("A valid row version is required."));
        if (request.IsCurrent) await ClearCurrentFinancialYear(entity.Id);
        entity.IsCurrent = request.IsCurrent; entity.IsActive = request.IsActive; entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo;
        return await SaveVersioned(entity, ToDto, "Municipality financial year was changed by another user.");
    }

    [HttpGet("reporting-periods")]
    [Authorize(Policy = "Permission:REPORTING_PERIOD.READ")]
    public ActionResult<ApiResponse<ReportingPeriodDto[]>> GetReportingPeriods([FromQuery] Guid? municipalityFinancialYearId = null)
    {
        _ = municipalityFinancialYearId;
        return StatusCode(StatusCodes.Status410Gone, Fail<ReportingPeriodDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/reporting-periods/page."));
    }

    [HttpGet("reporting-periods/page")]
    [Authorize(Policy = "Permission:REPORTING_PERIOD.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportingPeriodDto>>>> GetReportingPeriodsPage([FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearId = null, [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<ReportingPeriodDto>>();
        if (!PeriodSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<ReportingPeriodDto>("createdAt, code, name, sequence, startDate, endDate, or status");
        var query = context.ReportingPeriods.AsNoTracking().Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear).AsQueryable();
        if (municipalityFinancialYearId.HasValue) query = query.Where(item => item.MunicipalityFinancialYear.PublicId == municipalityFinancialYearId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.ReportingPeriodType.HasValue) query = query.Where(item => item.PeriodType == request.ReportingPeriodType.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term) || item.MunicipalityFinancialYear.FinancialYear.Code.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderReportingPeriods(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new ReportingPeriodDto(item.PublicId, item.MunicipalityFinancialYear.PublicId, item.Code, item.Name, item.PeriodType, item.Sequence, item.StartDate, item.EndDate, item.IsActive, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<ReportingPeriodDto>>(true, PagedResponse<ReportingPeriodDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("reporting-periods")]
    [Authorize(Policy = "Permission:REPORTING_PERIOD.CREATE")]
    public async Task<ActionResult<ApiResponse<ReportingPeriodDto>>> CreateReportingPeriod(SaveReportingPeriodRequest request)
    {
        if (!HasTenant()) return TenantRequired<ReportingPeriodDto>();
        if (!ValidDates(request.StartDate, request.EndDate) || request.Sequence < 1) return BadRequest(Fail<ReportingPeriodDto>("Valid dates and a positive sequence are required."));
        var parent = await context.MunicipalityFinancialYears.Include(x => x.FinancialYear).SingleOrDefaultAsync(x => x.PublicId == request.MunicipalityFinancialYearPublicId);
        if (parent == null) return BadRequest(Fail<ReportingPeriodDto>("Municipality financial year not found."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (request.StartDate < parent.FinancialYear.StartDate || request.EndDate > parent.FinancialYear.EndDate) return BadRequest(Fail<ReportingPeriodDto>("Reporting period must fall within the financial year."));
        if (await context.ReportingPeriods.AnyAsync(x => x.MunicipalityFinancialYearId == parent.Id && (x.Code == code || x.Sequence == request.Sequence))) return Conflict(Fail<ReportingPeriodDto>("Reporting-period code or sequence already exists."));
        var entity = new ReportingPeriod { MunicipalityFinancialYearId = parent.Id, MunicipalityFinancialYear = parent, Code = code, Name = request.Name.Trim(), PeriodType = request.PeriodType, Sequence = request.Sequence, StartDate = request.StartDate, EndDate = request.EndDate };
        context.ReportingPeriods.Add(entity); await context.SaveChangesAsync();
        return Ok(new ApiResponse<ReportingPeriodDto>(true, ToDto(entity)));
    }

    [HttpPut("reporting-periods/{publicId:guid}")]
    [Authorize(Policy = "Permission:REPORTING_PERIOD.UPDATE")]
    public async Task<ActionResult<ApiResponse<ReportingPeriodDto>>> UpdateReportingPeriod(Guid publicId, UpdateReportingPeriodRequest request)
    {
        if (!HasTenant()) return TenantRequired<ReportingPeriodDto>();
        var entity = await context.ReportingPeriods.Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<ReportingPeriodDto>("Reporting period not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<ReportingPeriodDto>("A valid row version is required."));
        if (!ValidDates(request.StartDate, request.EndDate) || request.StartDate < entity.MunicipalityFinancialYear.FinancialYear.StartDate || request.EndDate > entity.MunicipalityFinancialYear.FinancialYear.EndDate) return BadRequest(Fail<ReportingPeriodDto>("Reporting period dates must fall within the financial year."));
        entity.Name = request.Name.Trim(); entity.PeriodType = request.PeriodType; entity.Sequence = request.Sequence; entity.StartDate = request.StartDate; entity.EndDate = request.EndDate; entity.IsActive = request.IsActive;
        return await SaveVersioned(entity, ToDto, "Reporting period was changed by another user.");
    }

    [HttpGet("sdbip-layers")]
    [Authorize(Policy = "Permission:SDBIP_LAYER.READ")]
    public ActionResult<ApiResponse<SdbipLayerDto[]>> GetSdbipLayers([FromQuery] Guid? municipalityFinancialYearId = null, [FromQuery] bool includeInactive = false)
    {
        _ = municipalityFinancialYearId;
        _ = includeInactive;
        return StatusCode(StatusCodes.Status410Gone, Fail<SdbipLayerDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/sdbip-layers/page."));
    }

    [HttpGet("sdbip-layers/page")]
    [Authorize(Policy = "Permission:SDBIP_LAYER.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<SdbipLayerDto>>>> GetSdbipLayersPage([FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityFinancialYearId = null, [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<SdbipLayerDto>>();
        if (!LayerSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<SdbipLayerDto>("createdAt, code, name, displayOrder, financialYear, or status");
        var query = context.SdbipLayers.AsNoTracking().Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear).AsQueryable();
        if (municipalityFinancialYearId.HasValue) query = query.Where(item => item.MunicipalityFinancialYear.PublicId == municipalityFinancialYearId.Value);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term) || (item.Description != null && item.Description.Contains(term))
                || item.MunicipalityFinancialYear.FinancialYear.Code.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderSdbipLayers(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item => new SdbipLayerDto(item.PublicId, item.MunicipalityFinancialYear.PublicId,
            item.MunicipalityFinancialYear.FinancialYear.Code, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<SdbipLayerDto>>(true, PagedResponse<SdbipLayerDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("sdbip-layers")]
    [Authorize(Policy = "Permission:SDBIP_LAYER.CREATE")]
    public async Task<ActionResult<ApiResponse<SdbipLayerDto>>> CreateSdbipLayer(SaveSdbipLayerRequest request)
    {
        if (!HasTenant()) return TenantRequired<SdbipLayerDto>();
        var validation = ValidateSdbipLayer(request.Code, request.Name, request.Description, request.DisplayOrder, request.Reason);
        if (validation != null) return BadRequest(Fail<SdbipLayerDto>(validation));
        var parent = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId && item.IsActive);
        if (parent == null) return BadRequest(Fail<SdbipLayerDto>("An active municipality financial year is required."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.SdbipLayers.AnyAsync(item => item.MunicipalityFinancialYearId == parent.Id && item.Code == code))
            return Conflict(Fail<SdbipLayerDto>("The SDBIP layer code already exists for this municipality financial year."));
        var entity = new SdbipLayer
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalityFinancialYearId = parent.Id, MunicipalityFinancialYear = parent,
            Code = code, Name = request.Name.Trim(), Description = Normalize(request.Description), DisplayOrder = request.DisplayOrder
        };
        context.SdbipLayers.Add(entity);
        QueueSdbipLayerAudit(entity, "Create", null, request.Reason);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<SdbipLayerDto>(true, ToDto(entity)));
    }

    [HttpPut("sdbip-layers/{publicId:guid}")]
    [Authorize(Policy = "Permission:SDBIP_LAYER.UPDATE")]
    public async Task<ActionResult<ApiResponse<SdbipLayerDto>>> UpdateSdbipLayer(Guid publicId, UpdateSdbipLayerRequest request)
    {
        if (!HasTenant()) return TenantRequired<SdbipLayerDto>();
        var validation = ValidateSdbipLayer(request.Code, request.Name, request.Description, request.DisplayOrder, request.Reason);
        if (validation != null) return BadRequest(Fail<SdbipLayerDto>(validation));
        var entity = await context.SdbipLayers.Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item.FinancialYear)
            .SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<SdbipLayerDto>("SDBIP layer not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<SdbipLayerDto>("A valid row version is required."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.SdbipLayers.AnyAsync(item => item.Id != entity.Id && item.MunicipalityFinancialYearId == entity.MunicipalityFinancialYearId && item.Code == code))
            return Conflict(Fail<SdbipLayerDto>("The SDBIP layer code already exists for this municipality financial year."));
        if (!request.IsActive && await context.OpmsTargets.AnyAsync(item => item.SdbipLayerId == entity.Id && !item.IsWithdrawn))
            return Conflict(Fail<SdbipLayerDto>("Withdraw or reclassify active KPIs before deactivating this SDBIP layer."));
        var before = new { entity.Code, entity.Name, entity.Description, entity.DisplayOrder, entity.IsActive };
        entity.Code = code; entity.Name = request.Name.Trim(); entity.Description = Normalize(request.Description); entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTime.UtcNow;
        QueueSdbipLayerAudit(entity, "Update", before, request.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<SdbipLayerDto>("The SDBIP layer changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<SdbipLayerDto>(true, ToDto(entity)));
    }

    [HttpGet("employees")]
    [Authorize(Policy = "Permission:EMPLOYEE.READ")]
    public ActionResult<ApiResponse<EmployeeDto[]>> GetEmployees() =>
        StatusCode(StatusCodes.Status410Gone, Fail<EmployeeDto[]>(
            "This fixed-limit route is retired. Use /api/v1/masters/employees/page."));

    [HttpGet("employees/page")]
    [Authorize(Policy = "Permission:EMPLOYEE.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<EmployeeDto>>>> GetEmployeesPage([FromQuery] PagedQueryRequest request, [FromQuery] bool activeOnly = false)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<EmployeeDto>>();
        var canReadNumber = await CanAccessEmployeeMemberAsync("EmployeeNumber", SecurityOperation.Read);
        var canReadSalary = await CanAccessEmployeeMemberAsync("SalaryReference", SecurityOperation.Read);
        var canReadEmail = await CanAccessEmployeeMemberAsync("EmailAddress", SecurityOperation.Read);
        var canReadIdentity = await CanAccessEmployeeMemberAsync("IdentityUserId", SecurityOperation.Read);
        if (!EmployeeSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(Fail<PagedResponse<EmployeeDto>>("SortBy must be createdAt, name, employeeNumber, salaryReference, email, status, or effectiveFrom."));
        if (!canReadNumber && request.NormalizedSortBy == "employeenumber") return Forbid();
        if (!canReadSalary && request.NormalizedSortBy == "salaryreference") return Forbid();
        if (!canReadEmail && request.NormalizedSortBy == "email") return Forbid();

        var query = context.MunicipalEmployees.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            var now = DateTime.UtcNow;
            query = query.Where(item => item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now));
        }
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.FirstName.Contains(term) || item.LastName.Contains(term)
                || (canReadNumber && item.EmployeeNumber.Contains(term))
                || (canReadSalary && item.SalaryReference != null && item.SalaryReference.Contains(term))
                || (canReadEmail && item.EmailAddress != null && item.EmailAddress.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("name", false) => query.OrderBy(item => item.LastName).ThenBy(item => item.FirstName).ThenBy(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.LastName).ThenByDescending(item => item.FirstName).ThenBy(item => item.Id),
            ("employeenumber", false) => query.OrderBy(item => item.EmployeeNumber).ThenBy(item => item.Id),
            ("employeenumber", true) => query.OrderByDescending(item => item.EmployeeNumber).ThenBy(item => item.Id),
            ("salaryreference", false) => query.OrderBy(item => item.SalaryReference).ThenBy(item => item.Id),
            ("salaryreference", true) => query.OrderByDescending(item => item.SalaryReference).ThenBy(item => item.Id),
            ("email", false) => query.OrderBy(item => item.EmailAddress).ThenBy(item => item.Id),
            ("email", true) => query.OrderByDescending(item => item.EmailAddress).ThenBy(item => item.Id),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.LastName).ThenBy(item => item.Id),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.LastName).ThenBy(item => item.Id),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.Id),
            _ => query.OrderByDescending(item => item.Id)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(item => new EmployeeDto(item.PublicId, canReadNumber ? item.EmployeeNumber : null, canReadSalary ? item.SalaryReference : null, item.FirstName, item.LastName,
                canReadEmail ? item.EmailAddress : null, canReadIdentity ? item.IdentityUserId : null, item.IsActive, item.EffectiveFrom,
                item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<EmployeeDto>>(true,
            PagedResponse<EmployeeDto>.Create(rows, request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> EmployeeSortFields = ["createdat", "name", "employeenumber", "salaryreference", "email", "status", "effectivefrom"];

    [HttpPost("employees")]
    [Authorize(Policy = "Permission:EMPLOYEE.CREATE")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(SaveEmployeeRequest request)
    {
        if (!HasTenant()) return TenantRequired<EmployeeDto>();
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<EmployeeDto>("Effective-to must be on or after effective-from."));
        var salaryReference = Normalize(request.SalaryReference);
        if (salaryReference?.Length > 120) return BadRequest(Fail<EmployeeDto>("Salary reference cannot exceed 120 characters."));
        if (!await CanAccessEmployeeMemberAsync("EmployeeNumber", SecurityOperation.Update)) return Forbid();
        if (salaryReference != null && !await CanAccessEmployeeMemberAsync("SalaryReference", SecurityOperation.Update)) return Forbid();
        if (!string.IsNullOrWhiteSpace(request.EmailAddress) && !await CanAccessEmployeeMemberAsync("EmailAddress", SecurityOperation.Update)) return Forbid();
        if (!string.IsNullOrWhiteSpace(request.IdentityUserId) && !await CanAccessEmployeeMemberAsync("IdentityUserId", SecurityOperation.Update)) return Forbid();
        var identityUserId = Normalize(request.IdentityUserId);
        if (identityUserId != null && !await IsActiveTenantUserAsync(identityUserId))
            return BadRequest(Fail<EmployeeDto>("Linked login must be an active user in the selected municipality."));
        var number = request.EmployeeNumber.Trim().ToUpperInvariant();
        if (await context.MunicipalEmployees.AnyAsync(x => x.EmployeeNumber == number)) return Conflict(Fail<EmployeeDto>("Employee number already exists."));
        var entity = new MunicipalEmployee { MunicipalityId = tenantContext.MunicipalityId!.Value, EmployeeNumber = number, SalaryReference = salaryReference, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), EmailAddress = request.EmailAddress?.Trim(), IdentityUserId = identityUserId, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo };
        context.MunicipalEmployees.Add(entity); await context.SaveChangesAsync();
        return Ok(new ApiResponse<EmployeeDto>(true, ToDto(entity,
            await CanAccessEmployeeMemberAsync("EmployeeNumber", SecurityOperation.Read),
            await CanAccessEmployeeMemberAsync("SalaryReference", SecurityOperation.Read),
            await CanAccessEmployeeMemberAsync("EmailAddress", SecurityOperation.Read),
            await CanAccessEmployeeMemberAsync("IdentityUserId", SecurityOperation.Read))));
    }

    [HttpPut("employees/{publicId:guid}")]
    [Authorize(Policy = "Permission:EMPLOYEE.UPDATE")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(Guid publicId, UpdateEmployeeRequest request)
    {
        if (!HasTenant()) return TenantRequired<EmployeeDto>();
        var entity = await context.MunicipalEmployees.SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<EmployeeDto>("Employee not found."));
        var salaryReference = Normalize(request.SalaryReference);
        if (request.SalaryReferenceSpecified && salaryReference?.Length > 120)
            return BadRequest(Fail<EmployeeDto>("Salary reference cannot exceed 120 characters."));
        if (request.SalaryReferenceSpecified
            && !string.Equals(entity.SalaryReference, salaryReference, StringComparison.OrdinalIgnoreCase)
            && !await CanAccessEmployeeMemberAsync("SalaryReference", SecurityOperation.Update)) return Forbid();
        if (request.EmailAddressSpecified
            && !string.Equals(entity.EmailAddress?.Trim(), request.EmailAddress?.Trim(), StringComparison.OrdinalIgnoreCase)
            && !await CanAccessEmployeeMemberAsync("EmailAddress", SecurityOperation.Update)) return Forbid();
        var identityUserId = Normalize(request.IdentityUserId);
        if (request.IdentityUserIdSpecified
            && !string.Equals(entity.IdentityUserId, identityUserId, StringComparison.Ordinal)
            && !await CanAccessEmployeeMemberAsync("IdentityUserId", SecurityOperation.Update)) return Forbid();
        if (request.IdentityUserIdSpecified && identityUserId != null && !await IsActiveTenantUserAsync(identityUserId))
            return BadRequest(Fail<EmployeeDto>("Linked login must be an active user in the selected municipality."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<EmployeeDto>("A valid row version is required."));
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<EmployeeDto>("Effective-to must be on or after effective-from."));
        if (!request.IsActive && await context.EmployeeAssignments.AnyAsync(x => x.MunicipalEmployeeId == entity.Id && x.IsActive))
            return Conflict(Fail<EmployeeDto>("End all active employee assignments before deactivating the employee."));
        entity.FirstName = request.FirstName.Trim(); entity.LastName = request.LastName.Trim(); if (request.SalaryReferenceSpecified) entity.SalaryReference = salaryReference; if (request.EmailAddressSpecified) entity.EmailAddress = request.EmailAddress?.Trim(); if (request.IdentityUserIdSpecified) entity.IdentityUserId = identityUserId; entity.IsActive = request.IsActive; entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo;
        var canReadNumber = await CanAccessEmployeeMemberAsync("EmployeeNumber", SecurityOperation.Read);
        var canReadSalary = await CanAccessEmployeeMemberAsync("SalaryReference", SecurityOperation.Read);
        var canReadEmail = await CanAccessEmployeeMemberAsync("EmailAddress", SecurityOperation.Read);
        var canReadIdentity = await CanAccessEmployeeMemberAsync("IdentityUserId", SecurityOperation.Read);
        return await SaveVersioned(entity, item => ToDto(item, canReadNumber, canReadSalary, canReadEmail, canReadIdentity), "Employee was changed by another user.");
    }

    [HttpGet("employees/{employeePublicId:guid}/assignments")]
    [Authorize(Policy = "Permission:EMPLOYEE_ASSIGNMENT.READ")]
    public ActionResult<ApiResponse<EmployeeAssignmentDto[]>> GetAssignments(Guid employeePublicId) =>
        StatusCode(StatusCodes.Status410Gone, Fail<EmployeeAssignmentDto[]>(
            $"This unbounded employee placement-history route is retired. Use /api/v1/masters/employees/{employeePublicId}/assignments/page."));

    [HttpGet("employees/{employeePublicId:guid}/assignments/page")]
    [Authorize(Policy = "Permission:EMPLOYEE_ASSIGNMENT.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<EmployeeAssignmentDto>>>> GetAssignmentsPage(
        Guid employeePublicId,
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<EmployeeAssignmentDto>>();
        var sortBy = request.SortBy == null ? "effectivefrom" : request.NormalizedSortBy;
        if (!AssignmentSortFields.Contains(sortBy)) return InvalidSort<EmployeeAssignmentDto>("effectiveFrom, effectiveTo, position, department, unit, status, or primary");
        if (!await context.MunicipalEmployees.AsNoTracking().AnyAsync(item => item.PublicId == employeePublicId))
            return NotFound(Fail<PagedResponse<EmployeeAssignmentDto>>("Employee not found."));
        var query = context.EmployeeAssignments.AsNoTracking()
            .Include(item => item.MunicipalEmployee).Include(item => item.Department).Include(item => item.Unit).Include(item => item.Position)
            .Where(item => item.MunicipalEmployee.PublicId == employeePublicId);
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.PositionCode.Contains(term) || item.PositionName.Contains(term)
                || item.Department.Code.Contains(term) || item.Department.Name.Contains(term)
                || (item.Unit != null && (item.Unit.Code.Contains(term) || item.Unit.Name.Contains(term))));
        }
        var total = await query.CountAsync();
        var ordered = (sortBy, request.Descending) switch
        {
            ("effectiveto", false) => query.OrderBy(item => item.EffectiveTo).ThenBy(item => item.PublicId),
            ("effectiveto", true) => query.OrderByDescending(item => item.EffectiveTo).ThenByDescending(item => item.PublicId),
            ("position", false) => query.OrderBy(item => item.PositionName).ThenBy(item => item.PositionCode).ThenBy(item => item.PublicId),
            ("position", true) => query.OrderByDescending(item => item.PositionName).ThenByDescending(item => item.PositionCode).ThenByDescending(item => item.PublicId),
            ("department", false) => query.OrderBy(item => item.Department.Name).ThenBy(item => item.PublicId),
            ("department", true) => query.OrderByDescending(item => item.Department.Name).ThenByDescending(item => item.PublicId),
            ("unit", false) => query.OrderBy(item => item.Unit == null ? null : item.Unit.Name).ThenBy(item => item.PublicId),
            ("unit", true) => query.OrderByDescending(item => item.Unit == null ? null : item.Unit.Name).ThenByDescending(item => item.PublicId),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.PublicId),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenByDescending(item => item.PublicId),
            ("primary", false) => query.OrderBy(item => item.IsPrimary).ThenBy(item => item.PublicId),
            ("primary", true) => query.OrderByDescending(item => item.IsPrimary).ThenByDescending(item => item.PublicId),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.PublicId)
        };
        var rows = await ordered.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new EmployeeAssignmentDto(item.PublicId, item.MunicipalEmployee.PublicId, item.Department.PublicId, item.Department.Name,
                item.Unit == null ? null : item.Unit.PublicId, item.Unit == null ? null : item.Unit.Name, item.PositionCode, item.PositionName,
                item.EffectiveFrom, item.EffectiveTo, item.IsPrimary, item.IsActive, Convert.ToBase64String(item.RowVersion),
                item.Position == null ? null : item.Position.PublicId)).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<EmployeeAssignmentDto>>(true,
            PagedResponse<EmployeeAssignmentDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("employee-assignments")]
    [Authorize(Policy = "Permission:EMPLOYEE_ASSIGNMENT.CREATE")]
    public async Task<ActionResult<ApiResponse<EmployeeAssignmentDto>>> CreateAssignment(SaveEmployeeAssignmentRequest request)
    {
        if (!HasTenant()) return TenantRequired<EmployeeAssignmentDto>();
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<EmployeeAssignmentDto>("Effective-to must be after effective-from."));
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var employee = await context.MunicipalEmployees.SingleOrDefaultAsync(x => x.PublicId == request.EmployeePublicId);
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId);
        var unit = request.UnitPublicId.HasValue ? await context.Units.SingleOrDefaultAsync(x => x.PublicId == request.UnitPublicId.Value) : null;
        var position = request.PositionPublicId.HasValue ? await context.Positions.SingleOrDefaultAsync(x => x.PublicId == request.PositionPublicId.Value && x.IsActive) : null;
        if (employee == null || department == null || (request.UnitPublicId.HasValue && unit == null) || (unit != null && unit.DepartmentId != department.Id)) return BadRequest(Fail<EmployeeAssignmentDto>("Employee, department, or unit is invalid for the selected municipality."));
        if (request.PositionPublicId.HasValue && (position == null || position.DepartmentId != department.Id || position.UnitId != unit?.Id)) return BadRequest(Fail<EmployeeAssignmentDto>("Position is invalid for the selected department and unit."));
        if (position == null && (string.IsNullOrWhiteSpace(request.PositionCode) || string.IsNullOrWhiteSpace(request.PositionName))) return BadRequest(Fail<EmployeeAssignmentDto>("Select a governed position."));
        if (await HasAssignmentOverlap(employee.Id, request.EffectiveFrom, request.EffectiveTo)) return Conflict(Fail<EmployeeAssignmentDto>("Employee assignment dates overlap an existing assignment."));
        var entity = new EmployeeAssignment { MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalEmployeeId = employee.Id, MunicipalEmployee = employee, DepartmentId = department.Id, Department = department, UnitId = unit?.Id, Unit = unit, PositionId = position?.Id, Position = position, PositionCode = position?.Code ?? request.PositionCode!.Trim().ToUpperInvariant(), PositionName = position?.Name ?? request.PositionName!.Trim(), EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, IsPrimary = request.IsPrimary };
        context.EmployeeAssignments.Add(entity); await context.SaveChangesAsync(); await transaction.CommitAsync();
        return Ok(new ApiResponse<EmployeeAssignmentDto>(true, ToDto(entity)));
    }

    [HttpPut("employee-assignments/{publicId:guid}/close")]
    [Authorize(Policy = "Permission:EMPLOYEE_ASSIGNMENT.UPDATE")]
    public async Task<ActionResult<ApiResponse<EmployeeAssignmentDto>>> CloseAssignment(Guid publicId, CloseEmployeeAssignmentRequest request)
    {
        if (!HasTenant()) return TenantRequired<EmployeeAssignmentDto>();
        var reason = request.Reason.Trim();
        if (reason.Length is < 10 or > 1000) return BadRequest(Fail<EmployeeAssignmentDto>("A closure reason between 10 and 1000 characters is required."));
        var entity = await context.EmployeeAssignments.Include(x => x.MunicipalEmployee).Include(x => x.Department).Include(x => x.Unit).Include(x => x.Position)
            .SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<EmployeeAssignmentDto>("Employee assignment not found."));
        if (!entity.IsActive) return Conflict(Fail<EmployeeAssignmentDto>("Employee assignment is already closed."));
        var actorId = tenantContext.UserId;
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(Fail<EmployeeAssignmentDto>("An authenticated actor is required."));
        if (request.EffectiveTo < entity.EffectiveFrom || request.EffectiveTo > DateTime.UtcNow)
            return BadRequest(Fail<EmployeeAssignmentDto>("Effective-to must be on or after effective-from and cannot be in the future."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<EmployeeAssignmentDto>("A valid row version is required."));

        var before = new { entity.EffectiveTo, entity.IsActive, entity.IsPrimary };
        entity.EffectiveTo = request.EffectiveTo;
        entity.IsActive = false;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = tenantContext.MunicipalityId,
            EntityName = nameof(EmployeeAssignment),
            EntityId = entity.PublicId.ToString(),
            Action = "Close",
            OldValue = JsonSerializer.Serialize(before),
            NewValue = JsonSerializer.Serialize(new { entity.EffectiveTo, entity.IsActive, entity.IsPrimary }),
            ChangedBy = actorId,
            ChangedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = HttpContext.TraceIdentifier,
            Reason = reason,
            UserAgent = Request.Headers.UserAgent.ToString()
        });
        return await SaveVersioned(entity, ToDto, "Employee assignment was changed by another user.");
    }

    private ActionResult<ApiResponse<PagedResponse<T>>> InvalidSort<T>(string fields) =>
        BadRequest(Fail<PagedResponse<T>>($"SortBy must be {fields}."));

    private static IQueryable<FinancialYear> OrderFinancialYears(IQueryable<FinancialYear> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("startdate", false) => query.OrderBy(item => item.StartDate).ThenBy(item => item.Id),
        ("startdate", true) => query.OrderByDescending(item => item.StartDate).ThenBy(item => item.Id),
        ("enddate", false) => query.OrderBy(item => item.EndDate).ThenBy(item => item.Id),
        ("enddate", true) => query.OrderByDescending(item => item.EndDate).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenByDescending(item => item.StartDate).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenByDescending(item => item.StartDate).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<MunicipalityFinancialYear> OrderMunicipalityYears(IQueryable<MunicipalityFinancialYear> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.FinancialYear.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.FinancialYear.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.FinancialYear.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.FinancialYear.Name).ThenBy(item => item.Id),
        ("startdate", false) => query.OrderBy(item => item.FinancialYear.StartDate).ThenBy(item => item.Id),
        ("startdate", true) => query.OrderByDescending(item => item.FinancialYear.StartDate).ThenBy(item => item.Id),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenByDescending(item => item.FinancialYear.StartDate).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenByDescending(item => item.FinancialYear.StartDate).ThenBy(item => item.Id),
        ("current", false) => query.OrderBy(item => item.IsCurrent).ThenByDescending(item => item.FinancialYear.StartDate).ThenBy(item => item.Id),
        ("current", true) => query.OrderByDescending(item => item.IsCurrent).ThenByDescending(item => item.FinancialYear.StartDate).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<ReportingPeriod> OrderReportingPeriods(IQueryable<ReportingPeriod> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("sequence", false) => query.OrderBy(item => item.Sequence).ThenBy(item => item.Id),
        ("sequence", true) => query.OrderByDescending(item => item.Sequence).ThenBy(item => item.Id),
        ("startdate", false) => query.OrderBy(item => item.StartDate).ThenBy(item => item.Id),
        ("startdate", true) => query.OrderByDescending(item => item.StartDate).ThenBy(item => item.Id),
        ("enddate", false) => query.OrderBy(item => item.EndDate).ThenBy(item => item.Id),
        ("enddate", true) => query.OrderByDescending(item => item.EndDate).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Sequence).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Sequence).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<SdbipLayer> OrderSdbipLayers(IQueryable<SdbipLayer> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("displayorder", false) => query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("displayorder", true) => query.OrderByDescending(item => item.DisplayOrder).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("financialyear", false) => query.OrderBy(item => item.MunicipalityFinancialYear.FinancialYear.StartDate).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Id),
        ("financialyear", true) => query.OrderByDescending(item => item.MunicipalityFinancialYear.FinancialYear.StartDate).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private Task<bool> IsActiveTenantUserAsync(string userId) => context.Users
        .AnyAsync(item => item.Id == userId && item.MunicipalityId == tenantContext.MunicipalityId && item.IsActive);
    private async Task<bool> CanAccessEmployeeMemberAsync(string memberCode, SecurityOperation operation)
    {
        if (accessControl == null || string.IsNullOrWhiteSpace(tenantContext.UserId)) return false;
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == tenantContext.UserId);
        if (user == null) return false;
        var decision = await accessControl.CheckPermissionAsync(
            user,
            $"EMPLOYEE.{memberCode}.{operation.ToString().ToUpperInvariant()}",
            new AccessScopeContext(MunicipalityId: tenantContext.MunicipalityId));
        return decision.Allowed;
    }
    private static bool ValidDates(DateTime from, DateTime? to) => !to.HasValue || to.Value >= from;
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before using tenant master data."));
    private async Task<bool> HasAssignmentOverlap(long employeeId, DateTime from, DateTime? to) => await context.EmployeeAssignments.AnyAsync(x => x.MunicipalEmployeeId == employeeId && x.IsActive && (!x.EffectiveTo.HasValue || from < x.EffectiveTo.Value) && (!to.HasValue || x.EffectiveFrom < to.Value));
    private async Task ClearCurrentFinancialYear(long? exceptId = null) { var rows = await context.MunicipalityFinancialYears.Where(x => x.IsCurrent && (!exceptId.HasValue || x.Id != exceptId.Value)).ToArrayAsync(); foreach (var row in rows) row.IsCurrent = false; }
    private bool TrySetVersion<TEntity>(TEntity entity, string? value) where TEntity : class { if (string.IsNullOrWhiteSpace(value)) return false; try { context.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); return true; } catch (FormatException) { return false; } }
    private async Task<ActionResult<ApiResponse<TDto>>> SaveVersioned<TEntity, TDto>(TEntity entity, Func<TEntity, TDto> map, string conflict) where TEntity : class { try { await context.SaveChangesAsync(); return Ok(new ApiResponse<TDto>(true, map(entity))); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<TDto>(conflict)); } }
    private static FinancialYearDto ToDto(FinancialYear x) => new(x.PublicId, x.Code, x.Name, x.StartDate, x.EndDate, x.IsActive, Convert.ToBase64String(x.RowVersion));
    private static MunicipalityFinancialYearDto ToDto(MunicipalityFinancialYear x) => new(x.PublicId, x.FinancialYear.PublicId, x.FinancialYear.Code, x.FinancialYear.Name, x.IsCurrent, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static ReportingPeriodDto ToDto(ReportingPeriod x) => new(x.PublicId, x.MunicipalityFinancialYear.PublicId, x.Code, x.Name, x.PeriodType, x.Sequence, x.StartDate, x.EndDate, x.IsActive, Convert.ToBase64String(x.RowVersion));
    private static SdbipLayerDto ToDto(SdbipLayer x) => new(x.PublicId, x.MunicipalityFinancialYear.PublicId, x.MunicipalityFinancialYear.FinancialYear.Code, x.Code, x.Name, x.Description, x.DisplayOrder, x.IsActive, Convert.ToBase64String(x.RowVersion));
    private static EmployeeDto ToDto(MunicipalEmployee x, bool includeNumber = false, bool includeSalary = false, bool includeEmail = false, bool includeIdentity = false) => new(x.PublicId, includeNumber ? x.EmployeeNumber : null, includeSalary ? x.SalaryReference : null, x.FirstName, x.LastName, includeEmail ? x.EmailAddress : null, includeIdentity ? x.IdentityUserId : null, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static EmployeeAssignmentDto ToDto(EmployeeAssignment x) => new(x.PublicId, x.MunicipalEmployee.PublicId, x.Department.PublicId, x.Department.Name, x.Unit?.PublicId, x.Unit?.Name, x.PositionCode, x.PositionName, x.EffectiveFrom, x.EffectiveTo, x.IsPrimary, x.IsActive, Convert.ToBase64String(x.RowVersion), x.Position?.PublicId);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? ValidateSdbipLayer(string? code, string? name, string? description, int displayOrder, string? reason)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 80) return "A code of at most 80 characters is required.";
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return "A name of at most 200 characters is required.";
        if (description?.Trim().Length > 1000) return "Description cannot exceed 1000 characters.";
        if (displayOrder < 1) return "Display order must be positive.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10 || reason.Trim().Length > 1000) return "A governance reason between 10 and 1000 characters is required.";
        return null;
    }
    private void QueueSdbipLayerAudit(SdbipLayer entity, string action, object? oldValue, string reason)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) throw new UnauthorizedAccessException("An authenticated actor is required for SDBIP layer governance.");
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = entity.MunicipalityId, EntityName = nameof(SdbipLayer), EntityId = entity.PublicId.ToString(), Action = action,
            OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(new { entity.Code, entity.Name, entity.Description, entity.DisplayOrder, entity.IsActive, entity.MunicipalityFinancialYearId }),
            ChangedBy = actor, ChangedAt = DateTime.UtcNow, Reason = reason.Trim(), CorrelationId = HttpContext.TraceIdentifier
        });
    }
}

public sealed record FinancialYearDto(Guid PublicId, string Code, string Name, DateTime StartDate, DateTime EndDate, bool IsActive, string RowVersion);
public sealed record SaveFinancialYearRequest(string Code, string Name, DateTime StartDate, DateTime EndDate, bool IsActive = true, string? RowVersion = null);
public sealed record MunicipalityFinancialYearDto(Guid PublicId, Guid FinancialYearPublicId, string Code, string Name, bool IsCurrent, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveMunicipalityFinancialYearRequest(Guid FinancialYearPublicId, bool IsCurrent, DateTime EffectiveFrom, DateTime? EffectiveTo);
public sealed record UpdateMunicipalityFinancialYearRequest(bool IsCurrent, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record ReportingPeriodDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, string Code, string Name, ReportingPeriodType PeriodType, int Sequence, DateTime StartDate, DateTime EndDate, bool IsActive, string RowVersion);
public sealed record SaveReportingPeriodRequest(Guid MunicipalityFinancialYearPublicId, string Code, string Name, ReportingPeriodType PeriodType, int Sequence, DateTime StartDate, DateTime EndDate);
public sealed record UpdateReportingPeriodRequest(string Name, ReportingPeriodType PeriodType, int Sequence, DateTime StartDate, DateTime EndDate, bool IsActive, string RowVersion);
public sealed record SdbipLayerDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, string FinancialYearCode, string Code, string Name, string? Description, int DisplayOrder, bool IsActive, string RowVersion);
public sealed record SaveSdbipLayerRequest(Guid MunicipalityFinancialYearPublicId, string Code, string Name, string? Description, int DisplayOrder, string Reason);
public sealed record UpdateSdbipLayerRequest(string Code, string Name, string? Description, int DisplayOrder, bool IsActive, string Reason, string RowVersion);
public sealed record EmployeeDto(Guid PublicId, string? EmployeeNumber, string? SalaryReference, string FirstName, string LastName, string? EmailAddress, string? IdentityUserId, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveEmployeeRequest(string EmployeeNumber, string FirstName, string LastName, string? EmailAddress, string? IdentityUserId, DateTime EffectiveFrom, DateTime? EffectiveTo, string? SalaryReference = null);
public sealed record UpdateEmployeeRequest(string FirstName, string LastName, string? EmailAddress, string? IdentityUserId, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion, bool EmailAddressSpecified = true, bool IdentityUserIdSpecified = true, string? SalaryReference = null, bool SalaryReferenceSpecified = false);
public sealed record EmployeeAssignmentDto(Guid PublicId, Guid EmployeePublicId, Guid DepartmentPublicId, string DepartmentName, Guid? UnitPublicId, string? UnitName, string PositionCode, string PositionName, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPrimary, bool IsActive, string RowVersion, Guid? PositionPublicId = null);
public sealed record SaveEmployeeAssignmentRequest(Guid EmployeePublicId, Guid DepartmentPublicId, Guid? UnitPublicId, string? PositionCode, string? PositionName, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPrimary, Guid? PositionPublicId = null);
public sealed record CloseEmployeeAssignmentRequest(DateTime EffectiveTo, string Reason, string RowVersion);

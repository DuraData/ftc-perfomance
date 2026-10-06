using System.Text.Json;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/masters")]
[Authorize]
public sealed class OrganizationMastersController(ApplicationDbContext context, ITenantContext tenantContext) : ControllerBase
{
    private static readonly HashSet<string> CommonSortFields = new(StringComparer.OrdinalIgnoreCase) { "createdat", "code", "name", "status", "effectivefrom" };
    private static readonly HashSet<string> PositionSortFields = new(CommonSortFields, StringComparer.OrdinalIgnoreCase) { "department", "unit", "grade" };
    private static readonly HashSet<string> VoteSortFields = new(CommonSortFields, StringComparer.OrdinalIgnoreCase) { "department", "number", "amount" };

    [HttpGet("departments")]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public ActionResult<ApiResponse<DepartmentMasterDto[]>> GetDepartments() =>
        StatusCode(StatusCodes.Status410Gone, Fail<DepartmentMasterDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/departments/page."));

    [HttpGet("departments/page")]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<DepartmentMasterDto>>>> GetDepartmentsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<DepartmentMasterDto>>();
        if (!CommonSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<DepartmentMasterDto>("createdAt, code, name, status, or effectiveFrom");
        var query = context.Departments.AsNoTracking().AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term) || (item.Description != null && item.Description.Contains(term)));
        }
        var total = await query.CountAsync();
        query = OrderDepartments(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new DepartmentMasterDto(item.PublicId, item.Code, item.Name, item.Description, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<DepartmentMasterDto>>(true, PagedResponse<DepartmentMasterDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("departments")]
    [Authorize(Policy = "Permission:DEPARTMENT.CREATE")]
    public async Task<ActionResult<ApiResponse<DepartmentMasterDto>>> CreateDepartment(SaveDepartmentMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<DepartmentMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<DepartmentMasterDto>(validation));
        var code = NormalizeCode(request.Code);
        if (await context.Departments.AnyAsync(x => x.Code == code)) return Conflict(Fail<DepartmentMasterDto>("Department code already exists in this municipality."));
        var entity = new Department { MunicipalityId = tenantContext.MunicipalityId, Code = code, Name = request.Name.Trim(), Description = Clean(request.Description), EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, IsActive = request.IsActive };
        context.Departments.Add(entity);
        AddAudit(nameof(Department), entity.PublicId, "Create", null, Snapshot(entity), request.Reason);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<DepartmentMasterDto>(true, ToDto(entity)));
    }

    [HttpPut("departments/{publicId:guid}")]
    [Authorize(Policy = "Permission:DEPARTMENT.UPDATE")]
    public async Task<ActionResult<ApiResponse<DepartmentMasterDto>>> UpdateDepartment(Guid publicId, SaveDepartmentMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<DepartmentMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<DepartmentMasterDto>(validation));
        var entity = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<DepartmentMasterDto>("Department not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<DepartmentMasterDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.Departments.AnyAsync(x => x.Id != entity.Id && x.Code == code)) return Conflict(Fail<DepartmentMasterDto>("Department code already exists in this municipality."));
        if (!request.IsActive && await context.EmployeeAssignments.AnyAsync(x => x.DepartmentId == entity.Id && x.IsActive)) return Conflict(Fail<DepartmentMasterDto>("Close active employee assignments before deactivating the department."));
        var before = Snapshot(entity);
        entity.Code = code; entity.Name = request.Name.Trim(); entity.Description = Clean(request.Description); entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo; entity.IsActive = request.IsActive;
        AddAudit(nameof(Department), entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        return await SaveVersioned(entity, ToDto, "Department was changed by another user.");
    }

    [HttpGet("units")]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public ActionResult<ApiResponse<UnitMasterDto[]>> GetUnits() =>
        StatusCode(StatusCodes.Status410Gone, Fail<UnitMasterDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/units/page."));

    [HttpGet("units/page")]
    [Authorize(Policy = "Permission:UNIT.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UnitMasterDto>>>> GetUnitsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null, [FromQuery] Guid? departmentPublicId = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<UnitMasterDto>>();
        if (!PositionSortFields.Contains(request.NormalizedSortBy) || request.NormalizedSortBy is "unit" or "grade") return InvalidSort<UnitMasterDto>("createdAt, code, name, department, status, or effectiveFrom");
        var query = context.Units.AsNoTracking().Include(item => item.Department).AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (departmentPublicId.HasValue) query = query.Where(item => item.Department.PublicId == departmentPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term) || item.Department.Code.Contains(term) || item.Department.Name.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderUnits(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new UnitMasterDto(item.PublicId, item.Department.PublicId, item.Department.Name, item.Code, item.Name, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<UnitMasterDto>>(true, PagedResponse<UnitMasterDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("units")]
    [Authorize(Policy = "Permission:UNIT.CREATE")]
    public async Task<ActionResult<ApiResponse<UnitMasterDto>>> CreateUnit(SaveUnitMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<UnitMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<UnitMasterDto>(validation));
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId && x.IsActive);
        if (department == null) return BadRequest(Fail<UnitMasterDto>("Select an active department in this municipality."));
        var code = NormalizeCode(request.Code);
        if (await context.Units.AnyAsync(x => x.DepartmentId == department.Id && x.Code == code)) return Conflict(Fail<UnitMasterDto>("Unit code already exists in this department."));
        var entity = new Unit { MunicipalityId = tenantContext.MunicipalityId, DepartmentId = department.Id, Department = department, Code = code, Name = request.Name.Trim(), EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, IsActive = request.IsActive };
        context.Units.Add(entity); AddAudit(nameof(Unit), entity.PublicId, "Create", null, Snapshot(entity), request.Reason); await context.SaveChangesAsync();
        return Ok(new ApiResponse<UnitMasterDto>(true, ToDto(entity)));
    }

    [HttpPut("units/{publicId:guid}")]
    [Authorize(Policy = "Permission:UNIT.UPDATE")]
    public async Task<ActionResult<ApiResponse<UnitMasterDto>>> UpdateUnit(Guid publicId, SaveUnitMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<UnitMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<UnitMasterDto>(validation));
        var entity = await context.Units.Include(x => x.Department).SingleOrDefaultAsync(x => x.PublicId == publicId);
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId && x.IsActive);
        if (entity == null) return NotFound(Fail<UnitMasterDto>("Unit not found."));
        if (department == null) return BadRequest(Fail<UnitMasterDto>("Select an active department in this municipality."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<UnitMasterDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.Units.AnyAsync(x => x.Id != entity.Id && x.DepartmentId == department.Id && x.Code == code)) return Conflict(Fail<UnitMasterDto>("Unit code already exists in this department."));
        if (!request.IsActive && await context.EmployeeAssignments.AnyAsync(x => x.UnitId == entity.Id && x.IsActive)) return Conflict(Fail<UnitMasterDto>("Close active employee assignments before deactivating the unit."));
        var before = Snapshot(entity);
        entity.DepartmentId = department.Id; entity.Department = department; entity.Code = code; entity.Name = request.Name.Trim(); entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo; entity.IsActive = request.IsActive;
        AddAudit(nameof(Unit), entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        return await SaveVersioned(entity, ToDto, "Unit was changed by another user.");
    }

    [HttpGet("positions")]
    [Authorize(Policy = "Permission:POSITION.READ")]
    public ActionResult<ApiResponse<PositionMasterDto[]>> GetPositions() =>
        StatusCode(StatusCodes.Status410Gone, Fail<PositionMasterDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/positions/page."));

    [HttpGet("positions/page")]
    [Authorize(Policy = "Permission:POSITION.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PositionMasterDto>>>> GetPositionsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null, [FromQuery] Guid? departmentPublicId = null, [FromQuery] Guid? unitPublicId = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<PositionMasterDto>>();
        if (!PositionSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<PositionMasterDto>("createdAt, code, name, department, unit, grade, status, or effectiveFrom");
        var query = context.Positions.AsNoTracking().Include(item => item.Department).Include(item => item.Unit).AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (departmentPublicId.HasValue) query = query.Where(item => item.Department.PublicId == departmentPublicId.Value);
        if (unitPublicId.HasValue) query = query.Where(item => item.Unit != null && item.Unit.PublicId == unitPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term) || (item.Grade != null && item.Grade.Contains(term))
                || item.Department.Code.Contains(term) || item.Department.Name.Contains(term) || (item.Unit != null && (item.Unit.Code.Contains(term) || item.Unit.Name.Contains(term))));
        }
        var total = await query.CountAsync();
        query = OrderPositions(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new PositionMasterDto(item.PublicId, item.Department.PublicId, item.Department.Name, item.Unit == null ? null : item.Unit.PublicId, item.Unit == null ? null : item.Unit.Name, item.Code, item.Name, item.Grade, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<PositionMasterDto>>(true, PagedResponse<PositionMasterDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("positions")]
    [Authorize(Policy = "Permission:POSITION.CREATE")]
    public async Task<ActionResult<ApiResponse<PositionMasterDto>>> CreatePosition(SavePositionMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<PositionMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<PositionMasterDto>(validation));
        var resolved = await ResolveOrganization(request.DepartmentPublicId, request.UnitPublicId);
        if (resolved.Error != null) return BadRequest(Fail<PositionMasterDto>(resolved.Error));
        var code = NormalizeCode(request.Code);
        if (await context.Positions.AnyAsync(x => x.Code == code)) return Conflict(Fail<PositionMasterDto>("Position code already exists in this municipality."));
        var entity = new Position { MunicipalityId = tenantContext.MunicipalityId!.Value, DepartmentId = resolved.Department!.Id, Department = resolved.Department, UnitId = resolved.Unit?.Id, Unit = resolved.Unit, Code = code, Name = request.Name.Trim(), Grade = Clean(request.Grade), EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, IsActive = request.IsActive };
        context.Positions.Add(entity); AddAudit(nameof(Position), entity.PublicId, "Create", null, Snapshot(entity), request.Reason); await context.SaveChangesAsync();
        return Ok(new ApiResponse<PositionMasterDto>(true, ToDto(entity)));
    }

    [HttpPut("positions/{publicId:guid}")]
    [Authorize(Policy = "Permission:POSITION.UPDATE")]
    public async Task<ActionResult<ApiResponse<PositionMasterDto>>> UpdatePosition(Guid publicId, SavePositionMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<PositionMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<PositionMasterDto>(validation));
        var entity = await context.Positions.Include(x => x.Department).Include(x => x.Unit).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<PositionMasterDto>("Position not found."));
        var resolved = await ResolveOrganization(request.DepartmentPublicId, request.UnitPublicId);
        if (resolved.Error != null) return BadRequest(Fail<PositionMasterDto>(resolved.Error));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<PositionMasterDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.Positions.AnyAsync(x => x.Id != entity.Id && x.Code == code)) return Conflict(Fail<PositionMasterDto>("Position code already exists in this municipality."));
        if (!request.IsActive && await context.EmployeeAssignments.AnyAsync(x => x.PositionId == entity.Id && x.IsActive)) return Conflict(Fail<PositionMasterDto>("Close active employee assignments before deactivating the position."));
        var before = Snapshot(entity);
        entity.DepartmentId = resolved.Department!.Id; entity.Department = resolved.Department; entity.UnitId = resolved.Unit?.Id; entity.Unit = resolved.Unit; entity.Code = code; entity.Name = request.Name.Trim(); entity.Grade = Clean(request.Grade); entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo; entity.IsActive = request.IsActive;
        AddAudit(nameof(Position), entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        return await SaveVersioned(entity, ToDto, "Position was changed by another user.");
    }

    [HttpGet("wards")]
    [Authorize(Policy = "Permission:WARD.READ")]
    public ActionResult<ApiResponse<WardMasterDto[]>> GetWards() =>
        StatusCode(StatusCodes.Status410Gone, Fail<WardMasterDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/wards/page."));

    [HttpGet("wards/page")]
    [Authorize(Policy = "Permission:WARD.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<WardMasterDto>>>> GetWardsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<WardMasterDto>>();
        if (!CommonSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<WardMasterDto>("createdAt, code, name, status, or effectiveFrom");
        var query = context.Wards.AsNoTracking().AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderWards(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new WardMasterDto(item.PublicId, item.Id, item.Code, item.Name, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<WardMasterDto>>(true, PagedResponse<WardMasterDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("wards")]
    [Authorize(Policy = "Permission:WARD.CREATE")]
    public async Task<ActionResult<ApiResponse<WardMasterDto>>> CreateWard(SaveWardMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<WardMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<WardMasterDto>(validation));
        var code = NormalizeCode(request.Code);
        if (await context.Wards.AnyAsync(x => x.Code == code)) return Conflict(Fail<WardMasterDto>("Ward code already exists in this municipality."));
        var municipality = await context.Municipalities.AsNoTracking().SingleAsync(x => x.Id == tenantContext.MunicipalityId!.Value);
        var entity = new Ward { MunicipalityId = municipality.Id, LegacyMunicipality = municipality.Name, Code = code, Name = request.Name.Trim(), IsActive = request.IsActive, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo };
        context.Wards.Add(entity); AddAudit(nameof(Ward), entity.PublicId, "Create", null, Snapshot(entity), request.Reason); await context.SaveChangesAsync();
        return Ok(new ApiResponse<WardMasterDto>(true, ToDto(entity)));
    }

    [HttpPut("wards/{publicId:guid}")]
    [Authorize(Policy = "Permission:WARD.UPDATE")]
    public async Task<ActionResult<ApiResponse<WardMasterDto>>> UpdateWard(Guid publicId, SaveWardMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<WardMasterDto>();
        var validation = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (validation != null) return BadRequest(Fail<WardMasterDto>(validation));
        var entity = await context.Wards.SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<WardMasterDto>("Ward not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<WardMasterDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.Wards.AnyAsync(x => x.Id != entity.Id && x.Code == code)) return Conflict(Fail<WardMasterDto>("Ward code already exists in this municipality."));
        if (!request.IsActive && (await context.OpmsTargetWards.AnyAsync(x => x.WardId == entity.Id) || await context.IdpCommunitySessions.AnyAsync(x => x.WardId == entity.Id) || await context.IdpWardInputs.AnyAsync(x => x.WardId == entity.Id)))
            return Conflict(Fail<WardMasterDto>("Ward is referenced by performance or IDP records and cannot be deactivated."));
        var before = Snapshot(entity);
        entity.Code = code; entity.Name = request.Name.Trim(); entity.IsActive = request.IsActive; entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo;
        AddAudit(nameof(Ward), entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        return await SaveVersioned(entity, ToDto, "Ward was changed by another user.");
    }

    [HttpGet("vote-numbers")]
    [Authorize(Policy = "Permission:VOTE_NUMBER.READ")]
    public ActionResult<ApiResponse<VoteNumberMasterDto[]>> GetVoteNumbers() =>
        StatusCode(StatusCodes.Status410Gone, Fail<VoteNumberMasterDto[]>(
            "This unbounded route is retired. Use /api/v1/masters/vote-numbers/page."));

    [HttpGet("vote-numbers/page")]
    [Authorize(Policy = "Permission:VOTE_NUMBER.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<VoteNumberMasterDto>>>> GetVoteNumbersPage([FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null, [FromQuery] Guid? departmentPublicId = null, [FromQuery] Guid? municipalityFinancialYearPublicId = null)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<VoteNumberMasterDto>>();
        if (!VoteSortFields.Contains(request.NormalizedSortBy)) return InvalidSort<VoteNumberMasterDto>("createdAt, code, name, department, number, amount, status, or effectiveFrom");
        var query = VoteNumberQuery();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (departmentPublicId.HasValue) query = query.Where(item => item.Department.PublicId == departmentPublicId.Value);
        if (municipalityFinancialYearPublicId.HasValue) query = query.Where(item => item.MunicipalityFinancialYear != null && item.MunicipalityFinancialYear.PublicId == municipalityFinancialYearPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
        {
            var term = request.NormalizedSearch;
            query = query.Where(item => item.Code.Contains(term) || item.Number.Contains(term) || item.Name.Contains(term)
                || item.Department.Code.Contains(term) || item.Department.Name.Contains(term));
        }
        var total = await query.CountAsync();
        query = OrderVoteNumbers(query, request.NormalizedSortBy, request.Descending);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item =>
            new VoteNumberMasterDto(item.PublicId, item.Id, item.Department.PublicId, item.Department.Name, item.MunicipalityFinancialYear == null ? null : item.MunicipalityFinancialYear.PublicId, item.MunicipalityFinancialYear == null ? null : item.MunicipalityFinancialYear.FinancialYear.Code, item.Code, item.Number, item.Name, item.Amount, item.IsActive, item.EffectiveFrom, item.EffectiveTo, Convert.ToBase64String(item.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<VoteNumberMasterDto>>(true, PagedResponse<VoteNumberMasterDto>.Create(rows, request.Page, request.PageSize, total)));
    }

    [HttpPost("vote-numbers")]
    [Authorize(Policy = "Permission:VOTE_NUMBER.CREATE")]
    public async Task<ActionResult<ApiResponse<VoteNumberMasterDto>>> CreateVoteNumber(SaveVoteNumberMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<VoteNumberMasterDto>();
        var validation = ValidateVote(request);
        if (validation != null) return BadRequest(Fail<VoteNumberMasterDto>(validation));
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId && x.IsActive);
        if (department == null) return BadRequest(Fail<VoteNumberMasterDto>("Select an active department in this municipality."));
        var year = await ResolveVoteYear(request.MunicipalityFinancialYearPublicId);
        if (year == null) return BadRequest(Fail<VoteNumberMasterDto>("Select an active municipality financial year."));
        var code = NormalizeCode(request.Code);
        if (await context.VoteNumbers.AnyAsync(x => x.MunicipalityFinancialYearId == year.Id && x.Code == code)) return Conflict(Fail<VoteNumberMasterDto>("Vote-number code already exists for this municipality financial year."));
        var entity = new VoteNumber { MunicipalityId = tenantContext.MunicipalityId!.Value, MunicipalityFinancialYearId = year.Id, MunicipalityFinancialYear = year, DepartmentId = department.Id, Department = department, Code = code, Number = request.Number.Trim().ToUpperInvariant(), Name = request.Name.Trim(), Amount = request.Amount, IsActive = request.IsActive, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo };
        context.VoteNumbers.Add(entity); AddAudit(nameof(VoteNumber), entity.PublicId, "Create", null, Snapshot(entity), request.Reason); await context.SaveChangesAsync();
        return Ok(new ApiResponse<VoteNumberMasterDto>(true, ToDto(entity)));
    }

    [HttpPut("vote-numbers/{publicId:guid}")]
    [Authorize(Policy = "Permission:VOTE_NUMBER.UPDATE")]
    public async Task<ActionResult<ApiResponse<VoteNumberMasterDto>>> UpdateVoteNumber(Guid publicId, SaveVoteNumberMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<VoteNumberMasterDto>();
        var validation = ValidateVote(request);
        if (validation != null) return BadRequest(Fail<VoteNumberMasterDto>(validation));
        var entity = await VoteNumberQuery(tracking: true).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<VoteNumberMasterDto>("Vote number not found."));
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId && x.IsActive);
        if (department == null) return BadRequest(Fail<VoteNumberMasterDto>("Select an active department in this municipality."));
        var year = await ResolveVoteYear(request.MunicipalityFinancialYearPublicId, entity.MunicipalityFinancialYearId);
        if (year == null) return BadRequest(Fail<VoteNumberMasterDto>("Select an active municipality financial year."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<VoteNumberMasterDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.VoteNumbers.AnyAsync(x => x.Id != entity.Id && x.MunicipalityFinancialYearId == year.Id && x.Code == code)) return Conflict(Fail<VoteNumberMasterDto>("Vote-number code already exists for this municipality financial year."));
        if (!request.IsActive && await context.OpmsTargetVoteNumbers.AnyAsync(x => x.VoteNumberId == entity.Id)) return Conflict(Fail<VoteNumberMasterDto>("Vote number is referenced by a performance target and cannot be deactivated."));
        var before = Snapshot(entity);
        entity.MunicipalityFinancialYearId = year.Id; entity.MunicipalityFinancialYear = year; entity.DepartmentId = department.Id; entity.Department = department; entity.Code = code; entity.Number = request.Number.Trim().ToUpperInvariant(); entity.Name = request.Name.Trim(); entity.Amount = request.Amount; entity.IsActive = request.IsActive; entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo;
        AddAudit(nameof(VoteNumber), entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        return await SaveVersioned(entity, ToDto, "Vote number was changed by another user.");
    }

    private ActionResult<ApiResponse<PagedResponse<T>>> InvalidSort<T>(string fields) =>
        BadRequest(Fail<PagedResponse<T>>($"SortBy must be {fields}."));

    private static IQueryable<Department> OrderDepartments(IQueryable<Department> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<Unit> OrderUnits(IQueryable<Unit> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("department", false) => query.OrderBy(item => item.Department.Name).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("department", true) => query.OrderByDescending(item => item.Department.Name).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<Position> OrderPositions(IQueryable<Position> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("department", false) => query.OrderBy(item => item.Department.Name).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("department", true) => query.OrderByDescending(item => item.Department.Name).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("unit", false) => query.OrderBy(item => item.Unit == null ? "" : item.Unit.Name).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("unit", true) => query.OrderByDescending(item => item.Unit == null ? "" : item.Unit.Name).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("grade", false) => query.OrderBy(item => item.Grade).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("grade", true) => query.OrderByDescending(item => item.Grade).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name).ThenBy(item => item.Id),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<Ward> OrderWards(IQueryable<Ward> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private static IQueryable<VoteNumber> OrderVoteNumbers(IQueryable<VoteNumber> query, string sortBy, bool descending) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenBy(item => item.Id),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenBy(item => item.Id),
        ("department", false) => query.OrderBy(item => item.Department.Name).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("department", true) => query.OrderByDescending(item => item.Department.Name).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("number", false) => query.OrderBy(item => item.Number).ThenBy(item => item.Id),
        ("number", true) => query.OrderByDescending(item => item.Number).ThenBy(item => item.Id),
        ("amount", false) => query.OrderBy(item => (double)item.Amount).ThenBy(item => item.Id),
        ("amount", true) => query.OrderByDescending(item => (double)item.Amount).ThenBy(item => item.Id),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Code).ThenBy(item => item.Id),
        ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
        ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenBy(item => item.Id),
        (_, false) => query.OrderBy(item => item.Id),
        _ => query.OrderByDescending(item => item.Id)
    };

    private async Task<(Department? Department, Unit? Unit, string? Error)> ResolveOrganization(Guid departmentPublicId, Guid? unitPublicId)
    {
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == departmentPublicId && x.IsActive);
        var unit = unitPublicId.HasValue ? await context.Units.SingleOrDefaultAsync(x => x.PublicId == unitPublicId && x.IsActive) : null;
        if (department == null) return (null, null, "Select an active department in this municipality.");
        if (unitPublicId.HasValue && unit == null) return (null, null, "Select an active unit in this municipality.");
        if (unit != null && unit.DepartmentId != department.Id) return (null, null, "The selected unit does not belong to the selected department.");
        return (department, unit, null);
    }

    private IQueryable<VoteNumber> VoteNumberQuery(bool tracking = false)
    {
        var query = context.VoteNumbers.Include(item => item.Department).Include(item => item.MunicipalityFinancialYear).ThenInclude(item => item!.FinancialYear);
        return tracking ? query : query.AsNoTracking();
    }

    private async Task<MunicipalityFinancialYear?> ResolveVoteYear(Guid publicId, long? existingId = null)
    {
        var year = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == publicId);
        return year != null && (year.IsActive || year.Id == existingId) ? year : null;
    }

    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? ValidateCommon(string code, string name, DateTime from, DateTime? to, string reason)
    {
        if (code.Trim().Length is < 2 or > 50) return "Code must contain between 2 and 50 characters.";
        if (name.Trim().Length is < 2 or > 200) return "Name must contain between 2 and 200 characters.";
        if (to.HasValue && to.Value < from) return "Effective-to must be on or after effective-from.";
        if (reason.Trim().Length is < 10 or > 1000) return "A governance reason between 10 and 1000 characters is required.";
        return null;
    }
    private static string? ValidateVote(SaveVoteNumberMasterRequest request)
    {
        var common = ValidateCommon(request.Code, request.Name, request.EffectiveFrom, request.EffectiveTo, request.Reason);
        if (common != null) return common;
        if (request.Number.Trim().Length is < 1 or > 50) return "Vote number must contain between 1 and 50 characters.";
        if (request.Amount < 0) return "Vote amount cannot be negative.";
        return null;
    }
    private bool TrySetVersion<TEntity>(TEntity entity, string? value) where TEntity : class { if (string.IsNullOrWhiteSpace(value)) return false; try { context.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); return true; } catch (FormatException) { return false; } }
    private async Task<ActionResult<ApiResponse<TDto>>> SaveVersioned<TEntity, TDto>(TEntity entity, Func<TEntity, TDto> map, string conflict) where TEntity : class { try { await context.SaveChangesAsync(); return Ok(new ApiResponse<TDto>(true, map(entity))); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<TDto>(conflict)); } }
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before using tenant master data."));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private void AddAudit(string entityName, Guid publicId, string action, object? oldValue, object newValue, string reason) => context.AuditTrails.Add(new AuditTrail { MunicipalityId = tenantContext.MunicipalityId, EntityName = entityName, EntityId = publicId.ToString(), Action = action, OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(newValue), ChangedBy = tenantContext.UserId ?? "UNKNOWN", ChangedAt = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CorrelationId = HttpContext.TraceIdentifier, Reason = reason.Trim(), UserAgent = Request.Headers.UserAgent.ToString() });
    private static object Snapshot(Department x) => new { x.Code, x.Name, x.Description, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static object Snapshot(Unit x) => new { x.DepartmentId, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static object Snapshot(Position x) => new { x.DepartmentId, x.UnitId, x.Code, x.Name, x.Grade, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static object Snapshot(Ward x) => new { x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static object Snapshot(VoteNumber x) => new { x.MunicipalityFinancialYearId, x.DepartmentId, x.Code, x.Number, x.Name, x.Amount, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static DepartmentMasterDto ToDto(Department x) => new(x.PublicId, x.Code, x.Name, x.Description, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static UnitMasterDto ToDto(Unit x) => new(x.PublicId, x.Department.PublicId, x.Department.Name, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static PositionMasterDto ToDto(Position x) => new(x.PublicId, x.Department.PublicId, x.Department.Name, x.Unit?.PublicId, x.Unit?.Name, x.Code, x.Name, x.Grade, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static WardMasterDto ToDto(Ward x) => new(x.PublicId, x.Id, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static VoteNumberMasterDto ToDto(VoteNumber x) => new(x.PublicId, x.Id, x.Department.PublicId, x.Department.Name, x.MunicipalityFinancialYear?.PublicId, x.MunicipalityFinancialYear?.FinancialYear.Code, x.Code, x.Number, x.Name, x.Amount, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
}

public sealed record DepartmentMasterDto(Guid PublicId, string Code, string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record UnitMasterDto(Guid PublicId, Guid DepartmentPublicId, string DepartmentName, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record PositionMasterDto(Guid PublicId, Guid DepartmentPublicId, string DepartmentName, Guid? UnitPublicId, string? UnitName, string Code, string Name, string? Grade, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record WardMasterDto(Guid PublicId, int Id, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record VoteNumberMasterDto(Guid PublicId, int Id, Guid DepartmentPublicId, string DepartmentName, Guid? MunicipalityFinancialYearPublicId, string? FinancialYearCode, string Code, string Number, string Name, decimal Amount, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveDepartmentMasterRequest(string Code, string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveUnitMasterRequest(Guid DepartmentPublicId, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SavePositionMasterRequest(Guid DepartmentPublicId, Guid? UnitPublicId, string Code, string Name, string? Grade, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveWardMasterRequest(string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveVoteNumberMasterRequest(Guid DepartmentPublicId, Guid MunicipalityFinancialYearPublicId, string Code, string Number, string Name, decimal Amount, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);

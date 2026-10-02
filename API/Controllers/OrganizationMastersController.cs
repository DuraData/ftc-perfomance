using System.Text.Json;
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
    [HttpGet("departments")]
    [Authorize(Policy = "Permission:DEPARTMENT.READ")]
    public async Task<ActionResult<ApiResponse<DepartmentMasterDto[]>>> GetDepartments() =>
        Ok(new ApiResponse<DepartmentMasterDto[]>(true, await context.Departments.AsNoTracking().OrderBy(x => x.Name).Select(x =>
            new DepartmentMasterDto(x.PublicId, x.Code, x.Name, x.Description, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync()));

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
    public async Task<ActionResult<ApiResponse<UnitMasterDto[]>>> GetUnits() =>
        Ok(new ApiResponse<UnitMasterDto[]>(true, await context.Units.AsNoTracking().Include(x => x.Department).OrderBy(x => x.Department.Name).ThenBy(x => x.Name).Select(x =>
            new UnitMasterDto(x.PublicId, x.Department.PublicId, x.Department.Name, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync()));

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
    public async Task<ActionResult<ApiResponse<PositionMasterDto[]>>> GetPositions() =>
        Ok(new ApiResponse<PositionMasterDto[]>(true, await context.Positions.AsNoTracking().Include(x => x.Department).Include(x => x.Unit).OrderBy(x => x.Name).Select(x =>
            new PositionMasterDto(x.PublicId, x.Department.PublicId, x.Department.Name, x.Unit == null ? null : x.Unit.PublicId, x.Unit == null ? null : x.Unit.Name, x.Code, x.Name, x.Grade, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync()));

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
    public async Task<ActionResult<ApiResponse<WardMasterDto[]>>> GetWards() =>
        Ok(new ApiResponse<WardMasterDto[]>(true, await context.Wards.AsNoTracking().OrderBy(x => x.Code).Select(x =>
            new WardMasterDto(x.PublicId, x.Id, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync()));

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
    public async Task<ActionResult<ApiResponse<VoteNumberMasterDto[]>>> GetVoteNumbers() =>
        Ok(new ApiResponse<VoteNumberMasterDto[]>(true, await context.VoteNumbers.AsNoTracking().Include(x => x.Department).OrderBy(x => x.Code).Select(x =>
            new VoteNumberMasterDto(x.PublicId, x.Id, x.Department.PublicId, x.Department.Name, x.Code, x.Number, x.Name, x.Amount, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync()));

    [HttpPost("vote-numbers")]
    [Authorize(Policy = "Permission:VOTE_NUMBER.CREATE")]
    public async Task<ActionResult<ApiResponse<VoteNumberMasterDto>>> CreateVoteNumber(SaveVoteNumberMasterRequest request)
    {
        if (!HasTenant()) return TenantRequired<VoteNumberMasterDto>();
        var validation = ValidateVote(request);
        if (validation != null) return BadRequest(Fail<VoteNumberMasterDto>(validation));
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId && x.IsActive);
        if (department == null) return BadRequest(Fail<VoteNumberMasterDto>("Select an active department in this municipality."));
        var code = NormalizeCode(request.Code);
        if (await context.VoteNumbers.AnyAsync(x => x.Code == code)) return Conflict(Fail<VoteNumberMasterDto>("Vote-number code already exists in this municipality."));
        var entity = new VoteNumber { MunicipalityId = tenantContext.MunicipalityId!.Value, DepartmentId = department.Id, Department = department, Code = code, Number = request.Number.Trim().ToUpperInvariant(), Name = request.Name.Trim(), Amount = request.Amount, IsActive = request.IsActive, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo };
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
        var entity = await context.VoteNumbers.Include(x => x.Department).SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<VoteNumberMasterDto>("Vote number not found."));
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == request.DepartmentPublicId && x.IsActive);
        if (department == null) return BadRequest(Fail<VoteNumberMasterDto>("Select an active department in this municipality."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<VoteNumberMasterDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.VoteNumbers.AnyAsync(x => x.Id != entity.Id && x.Code == code)) return Conflict(Fail<VoteNumberMasterDto>("Vote-number code already exists in this municipality."));
        if (!request.IsActive && await context.OpmsTargetVoteNumbers.AnyAsync(x => x.VoteNumberId == entity.Id)) return Conflict(Fail<VoteNumberMasterDto>("Vote number is referenced by a performance target and cannot be deactivated."));
        var before = Snapshot(entity);
        entity.DepartmentId = department.Id; entity.Department = department; entity.Code = code; entity.Number = request.Number.Trim().ToUpperInvariant(); entity.Name = request.Name.Trim(); entity.Amount = request.Amount; entity.IsActive = request.IsActive; entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo;
        AddAudit(nameof(VoteNumber), entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        return await SaveVersioned(entity, ToDto, "Vote number was changed by another user.");
    }

    private async Task<(Department? Department, Unit? Unit, string? Error)> ResolveOrganization(Guid departmentPublicId, Guid? unitPublicId)
    {
        var department = await context.Departments.SingleOrDefaultAsync(x => x.PublicId == departmentPublicId && x.IsActive);
        var unit = unitPublicId.HasValue ? await context.Units.SingleOrDefaultAsync(x => x.PublicId == unitPublicId && x.IsActive) : null;
        if (department == null) return (null, null, "Select an active department in this municipality.");
        if (unitPublicId.HasValue && unit == null) return (null, null, "Select an active unit in this municipality.");
        if (unit != null && unit.DepartmentId != department.Id) return (null, null, "The selected unit does not belong to the selected department.");
        return (department, unit, null);
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
    private static object Snapshot(VoteNumber x) => new { x.DepartmentId, x.Code, x.Number, x.Name, x.Amount, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static DepartmentMasterDto ToDto(Department x) => new(x.PublicId, x.Code, x.Name, x.Description, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static UnitMasterDto ToDto(Unit x) => new(x.PublicId, x.Department.PublicId, x.Department.Name, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static PositionMasterDto ToDto(Position x) => new(x.PublicId, x.Department.PublicId, x.Department.Name, x.Unit?.PublicId, x.Unit?.Name, x.Code, x.Name, x.Grade, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static WardMasterDto ToDto(Ward x) => new(x.PublicId, x.Id, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static VoteNumberMasterDto ToDto(VoteNumber x) => new(x.PublicId, x.Id, x.Department.PublicId, x.Department.Name, x.Code, x.Number, x.Name, x.Amount, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
}

public sealed record DepartmentMasterDto(Guid PublicId, string Code, string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record UnitMasterDto(Guid PublicId, Guid DepartmentPublicId, string DepartmentName, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record PositionMasterDto(Guid PublicId, Guid DepartmentPublicId, string DepartmentName, Guid? UnitPublicId, string? UnitName, string Code, string Name, string? Grade, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record WardMasterDto(Guid PublicId, int Id, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record VoteNumberMasterDto(Guid PublicId, int Id, Guid DepartmentPublicId, string DepartmentName, string Code, string Number, string Name, decimal Amount, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveDepartmentMasterRequest(string Code, string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveUnitMasterRequest(Guid DepartmentPublicId, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SavePositionMasterRequest(Guid DepartmentPublicId, Guid? UnitPublicId, string Code, string Name, string? Grade, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveWardMasterRequest(string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveVoteNumberMasterRequest(Guid DepartmentPublicId, string Code, string Number, string Name, decimal Amount, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);

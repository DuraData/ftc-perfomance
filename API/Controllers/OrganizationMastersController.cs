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
    private bool TrySetVersion<TEntity>(TEntity entity, string? value) where TEntity : class { if (string.IsNullOrWhiteSpace(value)) return false; try { context.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); return true; } catch (FormatException) { return false; } }
    private async Task<ActionResult<ApiResponse<TDto>>> SaveVersioned<TEntity, TDto>(TEntity entity, Func<TEntity, TDto> map, string conflict) where TEntity : class { try { await context.SaveChangesAsync(); return Ok(new ApiResponse<TDto>(true, map(entity))); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<TDto>(conflict)); } }
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before using tenant master data."));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private void AddAudit(string entityName, Guid publicId, string action, object? oldValue, object newValue, string reason) => context.AuditTrails.Add(new AuditTrail { MunicipalityId = tenantContext.MunicipalityId, EntityName = entityName, EntityId = publicId.ToString(), Action = action, OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(newValue), ChangedBy = tenantContext.UserId ?? "UNKNOWN", ChangedAt = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CorrelationId = HttpContext.TraceIdentifier, Reason = reason.Trim(), UserAgent = Request.Headers.UserAgent.ToString() });
    private static object Snapshot(Department x) => new { x.Code, x.Name, x.Description, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static object Snapshot(Unit x) => new { x.DepartmentId, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static object Snapshot(Position x) => new { x.DepartmentId, x.UnitId, x.Code, x.Name, x.Grade, x.IsActive, x.EffectiveFrom, x.EffectiveTo };
    private static DepartmentMasterDto ToDto(Department x) => new(x.PublicId, x.Code, x.Name, x.Description, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static UnitMasterDto ToDto(Unit x) => new(x.PublicId, x.Department.PublicId, x.Department.Name, x.Code, x.Name, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static PositionMasterDto ToDto(Position x) => new(x.PublicId, x.Department.PublicId, x.Department.Name, x.Unit?.PublicId, x.Unit?.Name, x.Code, x.Name, x.Grade, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
}

public sealed record DepartmentMasterDto(Guid PublicId, string Code, string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record UnitMasterDto(Guid PublicId, Guid DepartmentPublicId, string DepartmentName, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record PositionMasterDto(Guid PublicId, Guid DepartmentPublicId, string DepartmentName, Guid? UnitPublicId, string? UnitName, string Code, string Name, string? Grade, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveDepartmentMasterRequest(string Code, string Name, string? Description, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SaveUnitMasterRequest(Guid DepartmentPublicId, string Code, string Name, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);
public sealed record SavePositionMasterRequest(Guid DepartmentPublicId, Guid? UnitPublicId, string Code, string Name, string? Grade, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string Reason, string? RowVersion = null);

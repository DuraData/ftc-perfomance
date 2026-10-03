using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/masters")]
[Authorize]
public sealed class TenantMastersController(ApplicationDbContext context, ITenantContext tenantContext, IAccessControlService? accessControl = null) : ControllerBase
{
    [HttpGet("financial-years")]
    [Authorize(Policy = "Permission:FINANCIAL_YEAR.READ")]
    public async Task<ActionResult<ApiResponse<FinancialYearDto[]>>> GetFinancialYears() =>
        Ok(new ApiResponse<FinancialYearDto[]>(true, await context.FinancialYears.AsNoTracking().OrderByDescending(x => x.StartDate)
            .Select(x => new FinancialYearDto(x.PublicId, x.Code, x.Name, x.StartDate, x.EndDate, x.IsActive, Convert.ToBase64String(x.RowVersion))).ToArrayAsync()));

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
    public async Task<ActionResult<ApiResponse<MunicipalityFinancialYearDto[]>>> GetMunicipalityFinancialYears()
    {
        if (!HasTenant()) return TenantRequired<MunicipalityFinancialYearDto[]>();
        var rows = await context.MunicipalityFinancialYears.AsNoTracking().Include(x => x.FinancialYear).OrderByDescending(x => x.FinancialYear.StartDate)
            .Select(x => new MunicipalityFinancialYearDto(x.PublicId, x.FinancialYear.PublicId, x.FinancialYear.Code, x.FinancialYear.Name, x.IsCurrent, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<MunicipalityFinancialYearDto[]>(true, rows));
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
    public async Task<ActionResult<ApiResponse<ReportingPeriodDto[]>>> GetReportingPeriods([FromQuery] Guid? municipalityFinancialYearId = null)
    {
        if (!HasTenant()) return TenantRequired<ReportingPeriodDto[]>();
        var query = context.ReportingPeriods.AsNoTracking().Include(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).AsQueryable();
        if (municipalityFinancialYearId.HasValue) query = query.Where(x => x.MunicipalityFinancialYear.PublicId == municipalityFinancialYearId.Value);
        var rows = await query.OrderByDescending(x => x.MunicipalityFinancialYear.FinancialYear.StartDate).ThenBy(x => x.Sequence)
            .Select(x => new ReportingPeriodDto(x.PublicId, x.MunicipalityFinancialYear.PublicId, x.Code, x.Name, x.PeriodType, x.Sequence, x.StartDate, x.EndDate, x.IsActive, Convert.ToBase64String(x.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<ReportingPeriodDto[]>(true, rows));
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

    [HttpGet("employees")]
    [Authorize(Policy = "Permission:EMPLOYEE.READ")]
    public async Task<ActionResult<ApiResponse<EmployeeDto[]>>> GetEmployees()
    {
        if (!HasTenant()) return TenantRequired<EmployeeDto[]>();
        var canReadEmail = await CanAccessEmployeeEmailAsync(SecurityOperation.Read);
        var rows = await context.MunicipalEmployees.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new EmployeeDto(x.PublicId, x.EmployeeNumber, x.FirstName, x.LastName, canReadEmail ? x.EmailAddress : null, x.IdentityUserId, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion))).ToArrayAsync();
        return Ok(new ApiResponse<EmployeeDto[]>(true, rows));
    }

    [HttpPost("employees")]
    [Authorize(Policy = "Permission:EMPLOYEE.CREATE")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(SaveEmployeeRequest request)
    {
        if (!HasTenant()) return TenantRequired<EmployeeDto>();
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<EmployeeDto>("Effective-to must be on or after effective-from."));
        if (!string.IsNullOrWhiteSpace(request.EmailAddress) && !await CanAccessEmployeeEmailAsync(SecurityOperation.Update)) return Forbid();
        var number = request.EmployeeNumber.Trim().ToUpperInvariant();
        if (await context.MunicipalEmployees.AnyAsync(x => x.EmployeeNumber == number)) return Conflict(Fail<EmployeeDto>("Employee number already exists."));
        var entity = new MunicipalEmployee { MunicipalityId = tenantContext.MunicipalityId!.Value, EmployeeNumber = number, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), EmailAddress = request.EmailAddress?.Trim(), IdentityUserId = request.IdentityUserId, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo };
        context.MunicipalEmployees.Add(entity); await context.SaveChangesAsync();
        return Ok(new ApiResponse<EmployeeDto>(true, ToDto(entity, await CanAccessEmployeeEmailAsync(SecurityOperation.Read))));
    }

    [HttpPut("employees/{publicId:guid}")]
    [Authorize(Policy = "Permission:EMPLOYEE.UPDATE")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(Guid publicId, UpdateEmployeeRequest request)
    {
        if (!HasTenant()) return TenantRequired<EmployeeDto>();
        var entity = await context.MunicipalEmployees.SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<EmployeeDto>("Employee not found."));
        if (request.EmailAddressSpecified
            && !string.Equals(entity.EmailAddress?.Trim(), request.EmailAddress?.Trim(), StringComparison.OrdinalIgnoreCase)
            && !await CanAccessEmployeeEmailAsync(SecurityOperation.Update)) return Forbid();
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<EmployeeDto>("A valid row version is required."));
        if (!ValidDates(request.EffectiveFrom, request.EffectiveTo)) return BadRequest(Fail<EmployeeDto>("Effective-to must be on or after effective-from."));
        if (!request.IsActive && await context.EmployeeAssignments.AnyAsync(x => x.MunicipalEmployeeId == entity.Id && x.IsActive))
            return Conflict(Fail<EmployeeDto>("End all active employee assignments before deactivating the employee."));
        entity.FirstName = request.FirstName.Trim(); entity.LastName = request.LastName.Trim(); if (request.EmailAddressSpecified) entity.EmailAddress = request.EmailAddress?.Trim(); entity.IdentityUserId = request.IdentityUserId; entity.IsActive = request.IsActive; entity.EffectiveFrom = request.EffectiveFrom; entity.EffectiveTo = request.EffectiveTo;
        var canReadEmail = await CanAccessEmployeeEmailAsync(SecurityOperation.Read);
        return await SaveVersioned(entity, item => ToDto(item, canReadEmail), "Employee was changed by another user.");
    }

    [HttpGet("employees/{employeePublicId:guid}/assignments")]
    [Authorize(Policy = "Permission:EMPLOYEE_ASSIGNMENT.READ")]
    public async Task<ActionResult<ApiResponse<EmployeeAssignmentDto[]>>> GetAssignments(Guid employeePublicId)
    {
        if (!HasTenant()) return TenantRequired<EmployeeAssignmentDto[]>();
        var rows = await context.EmployeeAssignments.AsNoTracking().Include(x => x.MunicipalEmployee).Include(x => x.Department).Include(x => x.Unit).Include(x => x.Position)
            .Where(x => x.MunicipalEmployee.PublicId == employeePublicId).OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new EmployeeAssignmentDto(x.PublicId, x.MunicipalEmployee.PublicId, x.Department.PublicId, x.Department.Name, x.Unit == null ? null : x.Unit.PublicId, x.Unit == null ? null : x.Unit.Name, x.PositionCode, x.PositionName, x.EffectiveFrom, x.EffectiveTo, x.IsPrimary, x.IsActive, Convert.ToBase64String(x.RowVersion), x.Position == null ? null : x.Position.PublicId)).ToArrayAsync();
        return Ok(new ApiResponse<EmployeeAssignmentDto[]>(true, rows));
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

    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private async Task<bool> CanAccessEmployeeEmailAsync(SecurityOperation operation)
    {
        if (accessControl == null || string.IsNullOrWhiteSpace(tenantContext.UserId)) return false;
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == tenantContext.UserId);
        if (user == null) return false;
        var decision = await accessControl.CheckPermissionAsync(
            user,
            $"EMPLOYEE.EmailAddress.{operation.ToString().ToUpperInvariant()}",
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
    private static EmployeeDto ToDto(MunicipalEmployee x, bool includeEmail = false) => new(x.PublicId, x.EmployeeNumber, x.FirstName, x.LastName, includeEmail ? x.EmailAddress : null, x.IdentityUserId, x.IsActive, x.EffectiveFrom, x.EffectiveTo, Convert.ToBase64String(x.RowVersion));
    private static EmployeeAssignmentDto ToDto(EmployeeAssignment x) => new(x.PublicId, x.MunicipalEmployee.PublicId, x.Department.PublicId, x.Department.Name, x.Unit?.PublicId, x.Unit?.Name, x.PositionCode, x.PositionName, x.EffectiveFrom, x.EffectiveTo, x.IsPrimary, x.IsActive, Convert.ToBase64String(x.RowVersion), x.Position?.PublicId);
}

public sealed record FinancialYearDto(Guid PublicId, string Code, string Name, DateTime StartDate, DateTime EndDate, bool IsActive, string RowVersion);
public sealed record SaveFinancialYearRequest(string Code, string Name, DateTime StartDate, DateTime EndDate, bool IsActive = true, string? RowVersion = null);
public sealed record MunicipalityFinancialYearDto(Guid PublicId, Guid FinancialYearPublicId, string Code, string Name, bool IsCurrent, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveMunicipalityFinancialYearRequest(Guid FinancialYearPublicId, bool IsCurrent, DateTime EffectiveFrom, DateTime? EffectiveTo);
public sealed record UpdateMunicipalityFinancialYearRequest(bool IsCurrent, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record ReportingPeriodDto(Guid PublicId, Guid MunicipalityFinancialYearPublicId, string Code, string Name, ReportingPeriodType PeriodType, int Sequence, DateTime StartDate, DateTime EndDate, bool IsActive, string RowVersion);
public sealed record SaveReportingPeriodRequest(Guid MunicipalityFinancialYearPublicId, string Code, string Name, ReportingPeriodType PeriodType, int Sequence, DateTime StartDate, DateTime EndDate);
public sealed record UpdateReportingPeriodRequest(string Name, ReportingPeriodType PeriodType, int Sequence, DateTime StartDate, DateTime EndDate, bool IsActive, string RowVersion);
public sealed record EmployeeDto(Guid PublicId, string EmployeeNumber, string FirstName, string LastName, string? EmailAddress, string? IdentityUserId, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion);
public sealed record SaveEmployeeRequest(string EmployeeNumber, string FirstName, string LastName, string? EmailAddress, string? IdentityUserId, DateTime EffectiveFrom, DateTime? EffectiveTo);
public sealed record UpdateEmployeeRequest(string FirstName, string LastName, string? EmailAddress, string? IdentityUserId, bool IsActive, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion, bool EmailAddressSpecified = true);
public sealed record EmployeeAssignmentDto(Guid PublicId, Guid EmployeePublicId, Guid DepartmentPublicId, string DepartmentName, Guid? UnitPublicId, string? UnitName, string PositionCode, string PositionName, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPrimary, bool IsActive, string RowVersion, Guid? PositionPublicId = null);
public sealed record SaveEmployeeAssignmentRequest(Guid EmployeePublicId, Guid DepartmentPublicId, Guid? UnitPublicId, string? PositionCode, string? PositionName, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsPrimary, Guid? PositionPublicId = null);
public sealed record CloseEmployeeAssignmentRequest(DateTime EffectiveTo, string Reason, string RowVersion);

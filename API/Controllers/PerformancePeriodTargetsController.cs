using System.Globalization;
using System.Security.Claims;
using FTCERP.Host.API.Responses;
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
[Route("api/v1/performance-period-targets")]
[Authorize]
public sealed class PerformancePeriodTargetsController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    IPerformanceUnitEngine unitEngine,
    IAccessControlService accessControl,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PerformancePeriodTargetDto[]>>> Get([FromQuery] Guid? opmsTargetId, [FromQuery] Guid? ipmsTargetId)
    {
        if (!HasTenant()) return TenantRequired<PerformancePeriodTargetDto[]>();
        if (opmsTargetId.HasValue == ipmsTargetId.HasValue) return BadRequest(Fail<PerformancePeriodTargetDto[]>("Provide exactly one OPMS or IPMS target public id."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformancePeriodTargetDto[]>("User not found."));
        var query = Query();
        if (opmsTargetId.HasValue)
        {
            var target = await context.OpmsTargets.SingleOrDefaultAsync(x => x.PublicId == opmsTargetId.Value);
            if (target == null) return NotFound(Fail<PerformancePeriodTargetDto[]>("OPMS target not found."));
            var decision = await accessControl.CheckPermissionAsync(user, "OPMS_KPI.READ", new AccessScopeContext(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, MunicipalityId: target.MunicipalityId));
            if (!decision.Allowed) return Forbid();
            query = query.Where(x => x.OpmsTargetId == target.Id);
        }
        else
        {
            var target = await context.IpmsTargets.SingleOrDefaultAsync(x => x.PublicId == ipmsTargetId!.Value);
            if (target == null) return NotFound(Fail<PerformancePeriodTargetDto[]>("IPMS target not found."));
            var decision = await accessControl.CheckPermissionAsync(user, "IPMS_KPI.READ", new AccessScopeContext(target.DepartmentId, target.UnitId, target.AssignedUserId, TargetId: target.Id, MunicipalityId: target.MunicipalityId));
            if (!decision.Allowed) return Forbid();
            query = query.Where(x => x.IpmsTargetId == target.Id);
        }
        var entities = await query.OrderBy(x => x.ReportingPeriod.Sequence).ToArrayAsync();
        var rows = entities.Select(ToDto).ToArray();
        return Ok(new ApiResponse<PerformancePeriodTargetDto[]>(true, rows));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PerformancePeriodTargetDto>>> Create(SavePerformancePeriodTargetRequest request)
    {
        if (!HasTenant()) return TenantRequired<PerformancePeriodTargetDto>();
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformancePeriodTargetDto>("User not found."));
        var period = await context.ReportingPeriods.Include(x => x.MunicipalityFinancialYear).SingleOrDefaultAsync(x => x.PublicId == request.ReportingPeriodPublicId && x.IsActive);
        if (period == null) return BadRequest(Fail<PerformancePeriodTargetDto>("Reporting period not found or inactive."));
        var normalized = unitEngine.Normalize(request.UnitKind, request.TargetValue);
        if (!normalized.IsValid) return BadRequest(Fail<PerformancePeriodTargetDto>(normalized.Error!));

        OpmsTarget? opms = null; IpmsTarget? ipms = null; string permission;
        if (request.TargetKind == SubmissionKind.Opms)
        {
            opms = await context.OpmsTargets.SingleOrDefaultAsync(x => x.PublicId == request.TargetPublicId);
            if (opms == null) return BadRequest(Fail<PerformancePeriodTargetDto>("OPMS target not found."));
            permission = "OPMS_KPI.UPDATE";
        }
        else if (request.TargetKind == SubmissionKind.Ipms)
        {
            ipms = await context.IpmsTargets.SingleOrDefaultAsync(x => x.PublicId == request.TargetPublicId);
            if (ipms == null) return BadRequest(Fail<PerformancePeriodTargetDto>("IPMS target not found."));
            permission = "IPMS_KPI.UPDATE";
        }
        else return BadRequest(Fail<PerformancePeriodTargetDto>("Target kind must be OPMS or IPMS."));
        var targetDepartment = opms?.DepartmentId ?? ipms?.DepartmentId;
        var targetUnit = opms?.UnitId ?? ipms?.UnitId;
        var targetOwner = opms?.AssignedUserId ?? ipms?.AssignedUserId;
        var targetId = opms?.Id ?? ipms!.Id;
        var decision = await accessControl.CheckPermissionAsync(user, permission, new AccessScopeContext(targetDepartment, targetUnit, targetOwner, TargetId: targetId, MunicipalityId: tenantContext.MunicipalityId));
        if (!decision.Allowed) return Forbid();
        var duplicate = opms != null
            ? await context.PerformancePeriodTargets.AnyAsync(x => x.ReportingPeriodId == period.Id && x.OpmsTargetId == opms.Id)
            : await context.PerformancePeriodTargets.AnyAsync(x => x.ReportingPeriodId == period.Id && x.IpmsTargetId == ipms!.Id);
        if (duplicate)
            return Conflict(Fail<PerformancePeriodTargetDto>("A target value already exists for this KPI and reporting period."));

        var entity = new PerformancePeriodTarget
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value, ReportingPeriodId = period.Id, ReportingPeriod = period,
            OpmsTargetId = opms?.Id, OpmsTarget = opms, IpmsTargetId = ipms?.Id, IpmsTarget = ipms,
            UnitKind = request.UnitKind, Direction = request.Direction, TargetValue = normalized.CanonicalValue!,
            BudgetValue = request.BudgetValue, Description = request.Description?.Trim(), CreatedByUserId = user.Id, CreatedByUser = user
        };
        context.PerformancePeriodTargets.Add(entity);
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<PerformancePeriodTargetDto>(true, ToDto(entity)));
    }

    [HttpPut("{publicId:guid}")]
    public async Task<ActionResult<ApiResponse<PerformancePeriodTargetDto>>> Revise(Guid publicId, RevisePerformancePeriodTargetRequest request)
    {
        if (!HasTenant()) return TenantRequired<PerformancePeriodTargetDto>();
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.ApprovalReference)) return BadRequest(Fail<PerformancePeriodTargetDto>("Revision reason and approval reference are required."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformancePeriodTargetDto>("User not found."));
        var entity = await Query().SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<PerformancePeriodTargetDto>("Performance period target not found."));
        var permission = entity.OpmsTargetId != null ? "OPMS_KPI.UPDATE" : "IPMS_KPI.UPDATE";
        var target = (object?)entity.OpmsTarget ?? entity.IpmsTarget!;
        var departmentId = target is OpmsTarget opms ? opms.DepartmentId : ((IpmsTarget)target).DepartmentId;
        var unitId = target is OpmsTarget opmsTarget ? opmsTarget.UnitId : ((IpmsTarget)target).UnitId;
        var ownerId = target is OpmsTarget opmsOwner ? opmsOwner.AssignedUserId : ((IpmsTarget)target).AssignedUserId;
        var targetId = target is OpmsTarget opmsId ? opmsId.Id : ((IpmsTarget)target).Id;
        if (!(await accessControl.CheckPermissionAsync(user, permission, new AccessScopeContext(departmentId, unitId, ownerId, TargetId: targetId, MunicipalityId: entity.MunicipalityId))).Allowed) return Forbid();
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<PerformancePeriodTargetDto>("A valid row version is required."));
        var normalized = unitEngine.Normalize(request.UnitKind, request.TargetValue);
        if (!normalized.IsValid) return BadRequest(Fail<PerformancePeriodTargetDto>(normalized.Error!));
        var changes = new Dictionary<string, (string? Old, string? New)>
        {
            [nameof(entity.UnitKind)] = (entity.UnitKind.ToString(), request.UnitKind.ToString()),
            [nameof(entity.Direction)] = (entity.Direction.ToString(), request.Direction.ToString()),
            [nameof(entity.TargetValue)] = (entity.TargetValue, normalized.CanonicalValue),
            [nameof(entity.BudgetValue)] = (entity.BudgetValue?.ToString(CultureInfo.InvariantCulture), request.BudgetValue?.ToString(CultureInfo.InvariantCulture)),
            [nameof(entity.Description)] = (entity.Description, request.Description?.Trim()),
            [nameof(entity.IsActive)] = (entity.IsActive.ToString(), request.IsActive.ToString())
        };
        foreach (var change in changes.Where(x => x.Value.Old != x.Value.New))
            context.PerformanceTargetRevisions.Add(new PerformanceTargetRevision { MunicipalityId = entity.MunicipalityId, PerformancePeriodTargetId = entity.Id, FieldName = change.Key, OriginalValue = change.Value.Old, RevisedValue = change.Value.New, Reason = request.Reason.Trim(), ApprovalReference = request.ApprovalReference.Trim(), EffectiveAt = request.EffectiveAt, RevisedByUserId = user.Id });
        entity.UnitKind = request.UnitKind; entity.Direction = request.Direction; entity.TargetValue = normalized.CanonicalValue!; entity.BudgetValue = request.BudgetValue; entity.Description = request.Description?.Trim(); entity.IsActive = request.IsActive;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<PerformancePeriodTargetDto>("Target values changed since they were loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<PerformancePeriodTargetDto>(true, ToDto(entity)));
    }

    [HttpGet("{publicId:guid}/revisions")]
    public async Task<ActionResult<ApiResponse<PerformanceTargetRevisionDto[]>>> Revisions(Guid publicId)
    {
        if (!HasTenant()) return TenantRequired<PerformanceTargetRevisionDto[]>();
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<PerformanceTargetRevisionDto[]>("User not found."));
        var entity = await Query().AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == publicId);
        if (entity == null) return NotFound(Fail<PerformanceTargetRevisionDto[]>("Performance period target not found."));
        var permission = entity.OpmsTargetId != null ? "OPMS_KPI.READ" : "IPMS_KPI.READ";
        var departmentId = entity.OpmsTarget?.DepartmentId ?? entity.IpmsTarget?.DepartmentId;
        var unitId = entity.OpmsTarget?.UnitId ?? entity.IpmsTarget?.UnitId;
        var ownerId = entity.OpmsTarget?.AssignedUserId ?? entity.IpmsTarget?.AssignedUserId;
        var targetId = entity.OpmsTargetId ?? entity.IpmsTargetId;
        if (!(await accessControl.CheckPermissionAsync(user, permission, new AccessScopeContext(departmentId, unitId, ownerId, TargetId: targetId, MunicipalityId: entity.MunicipalityId))).Allowed) return Forbid();
        var rows = await context.PerformanceTargetRevisions.AsNoTracking().Where(x => x.PerformancePeriodTargetId == entity.Id).OrderBy(x => x.RecordedAt)
            .Select(x => new PerformanceTargetRevisionDto(x.PublicId, x.FieldName, x.OriginalValue, x.RevisedValue, x.Reason, x.ApprovalReference, x.EffectiveAt, x.RevisedByUserId, x.RecordedAt)).ToArrayAsync();
        return Ok(new ApiResponse<PerformanceTargetRevisionDto[]>(true, rows));
    }

    private IQueryable<PerformancePeriodTarget> Query() => context.PerformancePeriodTargets.Include(x => x.ReportingPeriod).Include(x => x.OpmsTarget).Include(x => x.IpmsTarget);
    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before using performance targets."));
    private bool TrySetVersion(object entity, string value) { try { context.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); return true; } catch (FormatException) { return false; } }
    private static PerformancePeriodTargetDto ToDto(PerformancePeriodTarget x) => new(x.PublicId, x.ReportingPeriod.PublicId, x.ReportingPeriod.Code, x.UnitKind, x.Direction, x.TargetValue, x.BudgetValue, x.Description, x.IsActive, Convert.ToBase64String(x.RowVersion));
}

public sealed record PerformancePeriodTargetDto(Guid PublicId, Guid ReportingPeriodPublicId, string PeriodCode, PerformanceUnitKind UnitKind, PerformanceDirection Direction, string TargetValue, decimal? BudgetValue, string? Description, bool IsActive, string RowVersion);
public sealed record SavePerformancePeriodTargetRequest(SubmissionKind TargetKind, Guid TargetPublicId, Guid ReportingPeriodPublicId, PerformanceUnitKind UnitKind, PerformanceDirection Direction, string TargetValue, decimal? BudgetValue, string? Description);
public sealed record RevisePerformancePeriodTargetRequest(PerformanceUnitKind UnitKind, PerformanceDirection Direction, string TargetValue, decimal? BudgetValue, string? Description, bool IsActive, string Reason, string ApprovalReference, DateTime EffectiveAt, string RowVersion);
public sealed record PerformanceTargetRevisionDto(Guid PublicId, string FieldName, string? OriginalValue, string? RevisedValue, string Reason, string ApprovalReference, DateTime EffectiveAt, string RevisedByUserId, DateTime RecordedAt);

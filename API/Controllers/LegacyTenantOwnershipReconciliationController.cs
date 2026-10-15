using System.Security.Claims;
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
[Route("api/v1/security/tenant-ownership-reconciliation")]
[Authorize]
[Authorize(Policy = "Permission:SECURITY.RECONCILE_TENANT_OWNERSHIP")]
public sealed class LegacyTenantOwnershipReconciliationController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IWorkflowGovernanceService governance) : ControllerBase
{
    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>>> Preview(
        [FromQuery] LegacyTenantEntityType entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (!CanReconcile()) return SystemScopeRequired<PagedResponse<LegacyTenantOwnershipPreviewDto>>();
        if (!ValidType(entityType)) return InvalidType<PagedResponse<LegacyTenantOwnershipPreviewDto>>();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var selectedMunicipalityId = tenantContext.MunicipalityId!.Value;

        return entityType switch
        {
            LegacyTenantEntityType.Department => Ok(await PreviewDepartments(page, pageSize, selectedMunicipalityId)),
            LegacyTenantEntityType.Unit => Ok(await PreviewUnits(page, pageSize, selectedMunicipalityId)),
            LegacyTenantEntityType.OpmsTarget => Ok(await PreviewOpmsTargets(page, pageSize, selectedMunicipalityId)),
            LegacyTenantEntityType.IpmsTarget => Ok(await PreviewIpmsTargets(page, pageSize, selectedMunicipalityId)),
            LegacyTenantEntityType.OpmsSubmission => Ok(await PreviewOpmsSubmissions(page, pageSize, selectedMunicipalityId)),
            LegacyTenantEntityType.IpmsSubmission => Ok(await PreviewIpmsSubmissions(page, pageSize, selectedMunicipalityId)),
            _ => InvalidType<PagedResponse<LegacyTenantOwnershipPreviewDto>>()
        };
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<LegacyTenantOwnershipResultDto>>> Execute(LegacyTenantOwnershipRequest request)
    {
        if (!CanReconcile()) return SystemScopeRequired<LegacyTenantOwnershipResultDto>();
        if (!ValidType(request.EntityType)) return InvalidType<LegacyTenantOwnershipResultDto>();
        if (request.EntityPublicId == Guid.Empty)
            return BadRequest(Fail<LegacyTenantOwnershipResultDto>("An entity public identifier is required."));
        if (!TryVersion(request.RowVersion, out var expectedVersion))
            return BadRequest(Fail<LegacyTenantOwnershipResultDto>("A valid RowVersion is required."));
        var reason = request.Reason?.Trim();
        if (reason is not { Length: >= 10 and <= 1000 })
            return BadRequest(Fail<LegacyTenantOwnershipResultDto>("A reconciliation reason of 10 to 1000 characters is required."));
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actor = actorId == null ? null : await userManager.FindByIdAsync(actorId);
        if (actor == null) return Unauthorized(Fail<LegacyTenantOwnershipResultDto>("User not found."));

        var selectedMunicipalityId = tenantContext.MunicipalityId!.Value;
        var selectedMunicipalityPublicId = await context.Municipalities.IgnoreQueryFilters()
            .Where(x => x.Id == selectedMunicipalityId).Select(x => x.PublicId).SingleAsync();
        var loaded = await Load(request.EntityType, request.EntityPublicId);
        if (loaded == null)
            return NotFound(Fail<LegacyTenantOwnershipResultDto>("The legacy record was not found."));
        if (loaded.MunicipalityId.HasValue)
            return Conflict(Fail<LegacyTenantOwnershipResultDto>("The record already has municipality ownership."));
        var analysis = await Analyze(request.EntityType, loaded.Entity, loaded.Label, selectedMunicipalityId, request.RowVersion);
        if (analysis.Status != "Ready")
            return Conflict(Fail<LegacyTenantOwnershipResultDto>(analysis.Message));

        context.Entry(loaded.Entity).Property("RowVersion").OriginalValue = expectedVersion;
        context.Entry(loaded.Entity).Property("MunicipalityId").CurrentValue = selectedMunicipalityId;
        var reconciliation = new LegacyTenantOwnershipReconciliation
        {
            MunicipalityId = selectedMunicipalityId,
            EntityType = request.EntityType.ToString(),
            EntityPublicId = request.EntityPublicId,
            SourceEvidence = analysis.SourceEvidence,
            ReconciledByUserId = actor.Id,
            ReconciledAt = DateTime.UtcNow,
            Reason = reason,
            CorrelationId = Bounded(HttpContext.TraceIdentifier, 100)
        };

        await using var transaction = await context.Database.BeginTransactionAsync();
        using var reconciliationScope = context.BeginTenantOwnershipReconciliation();
        context.LegacyTenantOwnershipReconciliations.Add(reconciliation);
        governance.QueueAuditTrail(request.EntityType.ToString(), request.EntityPublicId.ToString(), "Update",
            new { request.EntityPublicId, MunicipalityPublicId = (Guid?)null },
            new { request.EntityPublicId, MunicipalityPublicId = selectedMunicipalityPublicId },
            actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext), reason);
        governance.QueueAuditTrail(nameof(LegacyTenantOwnershipReconciliation), reconciliation.PublicId.ToString(), "Reconcile",
            new { EntityType = request.EntityType.ToString(), request.EntityPublicId, MunicipalityPublicId = (Guid?)null },
            new { EntityType = request.EntityType.ToString(), request.EntityPublicId, MunicipalityPublicId = selectedMunicipalityPublicId, Evidence = analysis.SourceEvidence },
            actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext), reason);
        try
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(Fail<LegacyTenantOwnershipResultDto>("The record changed concurrently. Refresh the preview."));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(Fail<LegacyTenantOwnershipResultDto>("The record was reconciled concurrently or its ownership evidence is no longer valid."));
        }

        return Ok(new ApiResponse<LegacyTenantOwnershipResultDto>(true,
            new(reconciliation.PublicId, request.EntityType, request.EntityPublicId)));
    }

    private async Task<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>> PreviewDepartments(int page, int size, long selected)
    {
        var query = context.Departments.IgnoreQueryFilters().AsNoTracking().Where(x => x.MunicipalityId == null);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.Code).ThenBy(x => x.PublicId).Skip((page - 1) * size).Take(size).ToArrayAsync();
        var items = new List<LegacyTenantOwnershipPreviewDto>(rows.Length);
        foreach (var row in rows) items.Add(await Analyze(LegacyTenantEntityType.Department, row, $"{row.Code} · {row.Name}", selected, Version(row.RowVersion)));
        return Page(items, page, size, total);
    }

    private async Task<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>> PreviewUnits(int page, int size, long selected)
    {
        var query = context.Units.IgnoreQueryFilters().AsNoTracking().Where(x => x.MunicipalityId == null);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.Code).ThenBy(x => x.PublicId).Skip((page - 1) * size).Take(size).ToArrayAsync();
        var items = new List<LegacyTenantOwnershipPreviewDto>(rows.Length);
        foreach (var row in rows) items.Add(await Analyze(LegacyTenantEntityType.Unit, row, $"{row.Code} · {row.Name}", selected, Version(row.RowVersion)));
        return Page(items, page, size, total);
    }

    private async Task<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>> PreviewOpmsTargets(int page, int size, long selected)
    {
        var query = context.OpmsTargets.IgnoreQueryFilters().AsNoTracking().Where(x => x.MunicipalityId == null);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.IndicatorNumber).ThenBy(x => x.PublicId).Skip((page - 1) * size).Take(size).ToArrayAsync();
        var items = new List<LegacyTenantOwnershipPreviewDto>(rows.Length);
        foreach (var row in rows) items.Add(await Analyze(LegacyTenantEntityType.OpmsTarget, row, $"{row.IndicatorNumber} · {row.TargetName}", selected, Version(row.RowVersion)));
        return Page(items, page, size, total);
    }

    private async Task<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>> PreviewIpmsTargets(int page, int size, long selected)
    {
        var query = context.IpmsTargets.IgnoreQueryFilters().AsNoTracking().Where(x => x.MunicipalityId == null);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.IndicatorNumber).ThenBy(x => x.PublicId).Skip((page - 1) * size).Take(size).ToArrayAsync();
        var items = new List<LegacyTenantOwnershipPreviewDto>(rows.Length);
        foreach (var row in rows) items.Add(await Analyze(LegacyTenantEntityType.IpmsTarget, row, $"{row.IndicatorNumber} · {row.TargetName}", selected, Version(row.RowVersion)));
        return Page(items, page, size, total);
    }

    private async Task<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>> PreviewOpmsSubmissions(int page, int size, long selected)
    {
        var query = context.OpmsSubmissions.IgnoreQueryFilters().AsNoTracking().Where(x => x.MunicipalityId == null);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.CreatedOn).ThenBy(x => x.PublicId).Skip((page - 1) * size).Take(size).ToArrayAsync();
        var items = new List<LegacyTenantOwnershipPreviewDto>(rows.Length);
        foreach (var row in rows) items.Add(await Analyze(LegacyTenantEntityType.OpmsSubmission, row, $"OPMS submission · {row.Quarter}", selected, Version(row.RowVersion)));
        return Page(items, page, size, total);
    }

    private async Task<ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>>> PreviewIpmsSubmissions(int page, int size, long selected)
    {
        var query = context.IpmsSubmissions.IgnoreQueryFilters().AsNoTracking().Where(x => x.MunicipalityId == null);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.CreatedOn).ThenBy(x => x.PublicId).Skip((page - 1) * size).Take(size).ToArrayAsync();
        var items = new List<LegacyTenantOwnershipPreviewDto>(rows.Length);
        foreach (var row in rows) items.Add(await Analyze(LegacyTenantEntityType.IpmsSubmission, row, $"IPMS submission · {row.Quarter}", selected, Version(row.RowVersion)));
        return Page(items, page, size, total);
    }

    private async Task<LegacyTenantOwnershipPreviewDto> Analyze(LegacyTenantEntityType type, object entity, string label, long selected, string rowVersion)
    {
        var evidence = new Dictionary<long, HashSet<string>>();
        void Add(long? municipalityId, string source)
        {
            if (municipalityId is not > 0) return;
            if (!evidence.TryGetValue(municipalityId.Value, out var sources)) evidence[municipalityId.Value] = sources = [];
            sources.Add(source);
        }

        switch (type)
        {
            case LegacyTenantEntityType.Department:
            {
                var row = (Department)entity;
                foreach (var value in await context.Positions.IgnoreQueryFilters().Where(x => x.DepartmentId == row.Id).Select(x => x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "position.department");
                foreach (var value in await context.EmployeeAssignments.IgnoreQueryFilters().Where(x => x.DepartmentId == row.Id).Select(x => x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "employee-assignment.department");
                foreach (var value in await context.Users.IgnoreQueryFilters().Where(x => x.DepartmentId == row.Id).Select(x => x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "user.department");
                foreach (var value in await context.Units.IgnoreQueryFilters().Where(x => x.DepartmentId == row.Id).Select(x => x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "unit.department");
                break;
            }
            case LegacyTenantEntityType.Unit:
            {
                var row = (Unit)entity;
                Add(await context.Departments.IgnoreQueryFilters().Where(x => x.Id == row.DepartmentId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "unit.department");
                foreach (var value in await context.Positions.IgnoreQueryFilters().Where(x => x.UnitId == row.Id).Select(x => (long?)x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "position.unit");
                foreach (var value in await context.EmployeeAssignments.IgnoreQueryFilters().Where(x => x.UnitId == row.Id).Select(x => (long?)x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "employee-assignment.unit");
                foreach (var value in await context.Users.IgnoreQueryFilters().Where(x => x.UnitId == row.Id).Select(x => x.MunicipalityId).Distinct().ToArrayAsync()) Add(value, "user.unit");
                break;
            }
            case LegacyTenantEntityType.OpmsTarget:
            {
                var row = (OpmsTarget)entity;
                if (row.DepartmentId.HasValue) Add(await context.Departments.IgnoreQueryFilters().Where(x => x.Id == row.DepartmentId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.department");
                if (row.UnitId.HasValue) Add(await context.Units.IgnoreQueryFilters().Where(x => x.Id == row.UnitId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.unit");
                if (row.SdbipLayerId.HasValue) Add(await context.SdbipLayers.IgnoreQueryFilters().Where(x => x.Id == row.SdbipLayerId).Select(x => (long?)x.MunicipalityId).SingleOrDefaultAsync(), "target.sdbip-layer");
                if (row.AssignedUserId != null) Add(await context.Users.IgnoreQueryFilters().Where(x => x.Id == row.AssignedUserId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.assignee");
                break;
            }
            case LegacyTenantEntityType.IpmsTarget:
            {
                var row = (IpmsTarget)entity;
                if (row.DepartmentId.HasValue) Add(await context.Departments.IgnoreQueryFilters().Where(x => x.Id == row.DepartmentId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.department");
                if (row.UnitId.HasValue) Add(await context.Units.IgnoreQueryFilters().Where(x => x.Id == row.UnitId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.unit");
                if (row.AssignedUserId != null) Add(await context.Users.IgnoreQueryFilters().Where(x => x.Id == row.AssignedUserId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.assignee");
                if (row.SupervisorId != null) Add(await context.Users.IgnoreQueryFilters().Where(x => x.Id == row.SupervisorId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.supervisor");
                if (row.RelatedOpmsTargetId != null) Add(await context.OpmsTargets.IgnoreQueryFilters().Where(x => x.Id == row.RelatedOpmsTargetId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "target.related-opms-target");
                break;
            }
            case LegacyTenantEntityType.OpmsSubmission:
            {
                var row = (OpmsSubmission)entity;
                Add(await context.OpmsTargets.IgnoreQueryFilters().Where(x => x.Id == row.OpmsTargetId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "submission.target");
                if (row.SubmittedByUserId != null) Add(await context.Users.IgnoreQueryFilters().Where(x => x.Id == row.SubmittedByUserId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "submission.submitter");
                break;
            }
            case LegacyTenantEntityType.IpmsSubmission:
            {
                var row = (IpmsSubmission)entity;
                Add(await context.IpmsTargets.IgnoreQueryFilters().Where(x => x.Id == row.IpmsTargetId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "submission.target");
                if (row.SubmittedByUserId != null) Add(await context.Users.IgnoreQueryFilters().Where(x => x.Id == row.SubmittedByUserId).Select(x => x.MunicipalityId).SingleOrDefaultAsync(), "submission.submitter");
                break;
            }
        }

        var publicId = (Guid)context.Entry(entity).Property("PublicId").CurrentValue!;
        var sources = string.Join(", ", evidence.SelectMany(x => x.Value).Distinct(StringComparer.Ordinal).OrderBy(value => value));
        if (evidence.Count == 0)
            return new(type, publicId, label, "Blocked", "No authoritative tenant-owned relationship proves municipality ownership.", string.Empty, rowVersion);
        if (evidence.Count > 1)
            return new(type, publicId, label, "Ambiguous", "Authoritative relationships point to more than one municipality. Resolve the conflicting data before reconciliation.", sources, rowVersion);
        if (evidence.Keys.Single() != selected)
            return new(type, publicId, label, "DifferentMunicipality", "The authoritative relationship belongs to a different municipality than the selected context.", sources, rowVersion);
        return new(type, publicId, label, "Ready", "Exactly one municipality is proven by authoritative relationships.", sources, rowVersion);
    }

    private async Task<LoadedRecord?> Load(LegacyTenantEntityType type, Guid publicId) => type switch
    {
        LegacyTenantEntityType.Department => Wrap(await context.Departments.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PublicId == publicId), x => x.MunicipalityId, x => $"{x.Code} · {x.Name}"),
        LegacyTenantEntityType.Unit => Wrap(await context.Units.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PublicId == publicId), x => x.MunicipalityId, x => $"{x.Code} · {x.Name}"),
        LegacyTenantEntityType.OpmsTarget => Wrap(await context.OpmsTargets.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PublicId == publicId), x => x.MunicipalityId, x => $"{x.IndicatorNumber} · {x.TargetName}"),
        LegacyTenantEntityType.IpmsTarget => Wrap(await context.IpmsTargets.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PublicId == publicId), x => x.MunicipalityId, x => $"{x.IndicatorNumber} · {x.TargetName}"),
        LegacyTenantEntityType.OpmsSubmission => Wrap(await context.OpmsSubmissions.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PublicId == publicId), x => x.MunicipalityId, x => $"OPMS submission · {x.Quarter}"),
        LegacyTenantEntityType.IpmsSubmission => Wrap(await context.IpmsSubmissions.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.PublicId == publicId), x => x.MunicipalityId, x => $"IPMS submission · {x.Quarter}"),
        _ => null
    };

    private static LoadedRecord? Wrap<T>(T? entity, Func<T, long?> municipality, Func<T, string> label) where T : class =>
        entity == null ? null : new(entity, municipality(entity), label(entity));
    private static ApiResponse<PagedResponse<LegacyTenantOwnershipPreviewDto>> Page(IReadOnlyList<LegacyTenantOwnershipPreviewDto> items, int page, int size, int total) =>
        new(true, PagedResponse<LegacyTenantOwnershipPreviewDto>.Create(items, page, size, total));
    private bool CanReconcile() => tenantContext is { IsSystem: true, MunicipalityId: > 0 };
    private static bool ValidType(LegacyTenantEntityType type) => type is >= LegacyTenantEntityType.Department and <= LegacyTenantEntityType.IpmsSubmission;
    private static string Version(byte[] value) => Convert.ToBase64String(value);
    private static bool TryVersion(string? value, out byte[] version)
    {
        version = [];
        try { version = Convert.FromBase64String(value ?? string.Empty); return version.Length > 0; }
        catch (FormatException) { return false; }
    }
    private static string Bounded(string? value, int length) => string.IsNullOrWhiteSpace(value) ? "tenant-ownership-reconciliation" : value[..Math.Min(length, value.Length)];
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> SystemScopeRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("System scope and a selected municipality are required for tenant-ownership reconciliation."));
    private ActionResult<ApiResponse<T>> InvalidType<T>() => BadRequest(Fail<T>("Entity type must be Department, Unit, OPMS/IPMS target, or OPMS/IPMS submission."));
    private sealed record LoadedRecord(object Entity, long? MunicipalityId, string Label);
}

public enum LegacyTenantEntityType
{
    Department = 1,
    Unit = 2,
    OpmsTarget = 3,
    IpmsTarget = 4,
    OpmsSubmission = 5,
    IpmsSubmission = 6
}

public sealed record LegacyTenantOwnershipPreviewDto(LegacyTenantEntityType EntityType, Guid EntityPublicId,
    string Label, string Status, string Message, string SourceEvidence, string RowVersion);
public sealed record LegacyTenantOwnershipRequest(LegacyTenantEntityType EntityType, Guid EntityPublicId,
    string RowVersion, string Reason);
public sealed record LegacyTenantOwnershipResultDto(Guid ReconciliationPublicId,
    LegacyTenantEntityType EntityType, Guid EntityPublicId);

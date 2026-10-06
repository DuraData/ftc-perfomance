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
public sealed class GlobalStrategicReferencesController(ApplicationDbContext context, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("national-kpas")]
    [Authorize(Policy = "Permission:NATIONAL_KPA.READ")]
    public ActionResult<ApiResponse<GlobalStrategicReferenceDto[]>> GetNationalKpas([FromQuery] bool activeOnly = false)
    {
        _ = activeOnly;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<GlobalStrategicReferenceDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/masters/national-kpas/page."));
    }

    [HttpGet("national-kpas/page")]
    [Authorize(Policy = "Permission:NATIONAL_KPA.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>>> GetNationalKpasPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null, [FromQuery] bool? enabledForMunicipality = null)
    {
        var tenantId = tenantContext.MunicipalityId;
        var sortBy = request.SortBy == null ? "displayorder" : request.NormalizedSortBy;
        if (sortBy is not ("displayorder" or "code" or "name" or "status" or "availability"))
            return InvalidGlobalSort();
        var query = context.NationalKpas.AsNoTracking().AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (enabledForMunicipality.HasValue)
            query = query.Where(item => (!tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled)) == enabledForMunicipality.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Code.Contains(request.NormalizedSearch) || item.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        query = ApplyNationalKpaOrdering(query, sortBy, request.Descending, tenantId);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item => new GlobalStrategicReferenceDto(
            item.PublicId, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive,
            !tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled),
            item.MunicipalityMappings.Where(mapping => tenantId.HasValue && mapping.MunicipalityId == tenantId.Value).Select(mapping => (Guid?)mapping.PublicId).FirstOrDefault(),
            item.MunicipalityMappings.Where(mapping => tenantId.HasValue && mapping.MunicipalityId == tenantId.Value).Select(mapping => mapping.RowVersion).FirstOrDefault(), item.RowVersion)).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>(true,
            PagedResponse<GlobalStrategicReferenceDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpGet("back-to-basics-pillars")]
    [Authorize(Policy = "Permission:BACK_TO_BASICS_PILLAR.READ")]
    public ActionResult<ApiResponse<GlobalStrategicReferenceDto[]>> GetBackToBasicsPillars([FromQuery] bool activeOnly = false)
    {
        _ = activeOnly;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<GlobalStrategicReferenceDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/masters/back-to-basics-pillars/page."));
    }

    [HttpGet("back-to-basics-pillars/page")]
    [Authorize(Policy = "Permission:BACK_TO_BASICS_PILLAR.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>>> GetBackToBasicsPillarsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] bool? active = null, [FromQuery] bool? enabledForMunicipality = null)
    {
        var tenantId = tenantContext.MunicipalityId;
        var sortBy = request.SortBy == null ? "displayorder" : request.NormalizedSortBy;
        if (sortBy is not ("displayorder" or "code" or "name" or "status" or "availability"))
            return InvalidGlobalSort();
        var query = context.BackToBasicsPillars.AsNoTracking().AsQueryable();
        if (active.HasValue) query = query.Where(item => item.IsActive == active.Value);
        if (enabledForMunicipality.HasValue)
            query = query.Where(item => (!tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled)) == enabledForMunicipality.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Code.Contains(request.NormalizedSearch) || item.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        query = ApplyBackToBasicsOrdering(query, sortBy, request.Descending, tenantId);
        var rows = await query.Skip(request.Offset).Take(request.PageSize).Select(item => new GlobalStrategicReferenceDto(
            item.PublicId, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive,
            !tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled),
            item.MunicipalityMappings.Where(mapping => tenantId.HasValue && mapping.MunicipalityId == tenantId.Value).Select(mapping => (Guid?)mapping.PublicId).FirstOrDefault(),
            item.MunicipalityMappings.Where(mapping => tenantId.HasValue && mapping.MunicipalityId == tenantId.Value).Select(mapping => mapping.RowVersion).FirstOrDefault(), item.RowVersion)).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>(true,
            PagedResponse<GlobalStrategicReferenceDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, totalCount)));
    }

    [HttpPost("national-kpas")]
    [Authorize(Policy = "Permission:NATIONAL_KPA.CREATE")]
    public async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> CreateNationalKpa(SaveGlobalStrategicReferenceRequest request)
    {
        if (!tenantContext.IsSystem) return SystemScopeRequired<GlobalStrategicReferenceDto>();
        var error = Validate(request); if (error != null) return BadRequest(Fail<GlobalStrategicReferenceDto>(error));
        var code = NormalizeCode(request.Code);
        if (await context.NationalKpas.AnyAsync(item => item.Code == code)) return Conflict(Fail<GlobalStrategicReferenceDto>("National KPA code already exists."));
        var entity = new NationalKpa { Code = code, Name = request.Name.Trim(), Description = Clean(request.Description), DisplayOrder = request.DisplayOrder, IsActive = request.IsActive };
        context.NationalKpas.Add(entity); AddAudit(nameof(NationalKpa), entity.PublicId, "Create", null, Snapshot(entity), request.Reason, null); await context.SaveChangesAsync();
        return Ok(new ApiResponse<GlobalStrategicReferenceDto>(true, ToDto(entity)));
    }

    [HttpPut("national-kpas/{publicId:guid}")]
    [Authorize(Policy = "Permission:NATIONAL_KPA.UPDATE")]
    public async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> UpdateNationalKpa(Guid publicId, SaveGlobalStrategicReferenceRequest request)
    {
        if (!tenantContext.IsSystem) return SystemScopeRequired<GlobalStrategicReferenceDto>();
        var error = Validate(request); if (error != null) return BadRequest(Fail<GlobalStrategicReferenceDto>(error));
        var entity = await context.NationalKpas.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<GlobalStrategicReferenceDto>("National KPA not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<GlobalStrategicReferenceDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.NationalKpas.AnyAsync(item => item.Id != entity.Id && item.Code == code)) return Conflict(Fail<GlobalStrategicReferenceDto>("National KPA code already exists."));
        var before = Snapshot(entity); entity.Code = code; entity.Name = request.Name.Trim(); entity.Description = Clean(request.Description); entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive;
        AddAudit(nameof(NationalKpa), entity.PublicId, "Update", before, Snapshot(entity), request.Reason, null);
        return await SaveVersioned(entity, ToDto, "National KPA was changed by another user.");
    }

    [HttpPost("back-to-basics-pillars")]
    [Authorize(Policy = "Permission:BACK_TO_BASICS_PILLAR.CREATE")]
    public async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> CreateBackToBasicsPillar(SaveGlobalStrategicReferenceRequest request)
    {
        if (!tenantContext.IsSystem) return SystemScopeRequired<GlobalStrategicReferenceDto>();
        var error = Validate(request); if (error != null) return BadRequest(Fail<GlobalStrategicReferenceDto>(error));
        var code = NormalizeCode(request.Code);
        if (await context.BackToBasicsPillars.AnyAsync(item => item.Code == code)) return Conflict(Fail<GlobalStrategicReferenceDto>("Back-to-Basics pillar code already exists."));
        var entity = new BackToBasicsPillar { Code = code, Name = request.Name.Trim(), Description = Clean(request.Description), DisplayOrder = request.DisplayOrder, IsActive = request.IsActive };
        context.BackToBasicsPillars.Add(entity); AddAudit(nameof(BackToBasicsPillar), entity.PublicId, "Create", null, Snapshot(entity), request.Reason, null); await context.SaveChangesAsync();
        return Ok(new ApiResponse<GlobalStrategicReferenceDto>(true, ToDto(entity)));
    }

    [HttpPut("back-to-basics-pillars/{publicId:guid}")]
    [Authorize(Policy = "Permission:BACK_TO_BASICS_PILLAR.UPDATE")]
    public async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> UpdateBackToBasicsPillar(Guid publicId, SaveGlobalStrategicReferenceRequest request)
    {
        if (!tenantContext.IsSystem) return SystemScopeRequired<GlobalStrategicReferenceDto>();
        var error = Validate(request); if (error != null) return BadRequest(Fail<GlobalStrategicReferenceDto>(error));
        var entity = await context.BackToBasicsPillars.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<GlobalStrategicReferenceDto>("Back-to-Basics pillar not found."));
        if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<GlobalStrategicReferenceDto>("A valid row version is required."));
        var code = NormalizeCode(request.Code);
        if (await context.BackToBasicsPillars.AnyAsync(item => item.Id != entity.Id && item.Code == code)) return Conflict(Fail<GlobalStrategicReferenceDto>("Back-to-Basics pillar code already exists."));
        var before = Snapshot(entity); entity.Code = code; entity.Name = request.Name.Trim(); entity.Description = Clean(request.Description); entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive;
        AddAudit(nameof(BackToBasicsPillar), entity.PublicId, "Update", before, Snapshot(entity), request.Reason, null);
        return await SaveVersioned(entity, ToDto, "Back-to-Basics pillar was changed by another user.");
    }

    [HttpPut("national-kpas/{publicId:guid}/municipality-availability")]
    [Authorize(Policy = "Permission:NATIONAL_KPA.UPDATE")]
    public async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> SetNationalKpaAvailability(Guid publicId, SaveMunicipalityReferenceAvailabilityRequest request)
    {
        if (!tenantContext.MunicipalityId.HasValue) return TenantRequired<GlobalStrategicReferenceDto>();
        var error = ValidateReason(request.Reason); if (error != null) return BadRequest(Fail<GlobalStrategicReferenceDto>(error));
        var entity = await context.NationalKpas.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<GlobalStrategicReferenceDto>("National KPA not found."));
        var mapping = await context.MunicipalityNationalKpas.SingleOrDefaultAsync(item => item.NationalKpaId == entity.Id);
        object? before = mapping == null ? null : new { mapping.IsEnabled };
        if (mapping == null)
        {
            mapping = new MunicipalityNationalKpa { MunicipalityId = tenantContext.MunicipalityId.Value, NationalKpaId = entity.Id, IsEnabled = request.IsEnabled };
            context.MunicipalityNationalKpas.Add(mapping);
        }
        else
        {
            if (!TrySetVersion(mapping, request.RowVersion)) return BadRequest(Fail<GlobalStrategicReferenceDto>("A valid availability row version is required."));
            mapping.IsEnabled = request.IsEnabled;
        }
        AddAudit(nameof(MunicipalityNationalKpa), mapping.PublicId, before == null ? "CreateAvailability" : "UpdateAvailability", before, new { entity.PublicId, request.IsEnabled }, request.Reason, tenantContext.MunicipalityId);
        try { await context.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<GlobalStrategicReferenceDto>("National KPA availability was changed by another user.")); }
        return Ok(new ApiResponse<GlobalStrategicReferenceDto>(true, ToDto(entity, mapping)));
    }

    [HttpPut("back-to-basics-pillars/{publicId:guid}/municipality-availability")]
    [Authorize(Policy = "Permission:BACK_TO_BASICS_PILLAR.UPDATE")]
    public async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> SetBackToBasicsPillarAvailability(Guid publicId, SaveMunicipalityReferenceAvailabilityRequest request)
    {
        if (!tenantContext.MunicipalityId.HasValue) return TenantRequired<GlobalStrategicReferenceDto>();
        var error = ValidateReason(request.Reason); if (error != null) return BadRequest(Fail<GlobalStrategicReferenceDto>(error));
        var entity = await context.BackToBasicsPillars.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<GlobalStrategicReferenceDto>("Back-to-Basics pillar not found."));
        var mapping = await context.MunicipalityBackToBasicsPillars.SingleOrDefaultAsync(item => item.BackToBasicsPillarId == entity.Id);
        object? before = mapping == null ? null : new { mapping.IsEnabled };
        if (mapping == null)
        {
            mapping = new MunicipalityBackToBasicsPillar { MunicipalityId = tenantContext.MunicipalityId.Value, BackToBasicsPillarId = entity.Id, IsEnabled = request.IsEnabled };
            context.MunicipalityBackToBasicsPillars.Add(mapping);
        }
        else
        {
            if (!TrySetVersion(mapping, request.RowVersion)) return BadRequest(Fail<GlobalStrategicReferenceDto>("A valid availability row version is required."));
            mapping.IsEnabled = request.IsEnabled;
        }
        AddAudit(nameof(MunicipalityBackToBasicsPillar), mapping.PublicId, before == null ? "CreateAvailability" : "UpdateAvailability", before, new { entity.PublicId, request.IsEnabled }, request.Reason, tenantContext.MunicipalityId);
        try { await context.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<GlobalStrategicReferenceDto>("Back-to-Basics pillar availability was changed by another user.")); }
        return Ok(new ApiResponse<GlobalStrategicReferenceDto>(true, ToDto(entity, mapping)));
    }

    private static IQueryable<NationalKpa> ApplyNationalKpaOrdering(IQueryable<NationalKpa> query, string sortBy, bool descending, long? tenantId) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.PublicId),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.PublicId),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.PublicId),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.PublicId),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        ("availability", false) => query.OrderBy(item => !tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled)).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        ("availability", true) => query.OrderByDescending(item => !tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled)).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        (_, false) => query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.PublicId),
        _ => query.OrderByDescending(item => item.DisplayOrder).ThenByDescending(item => item.Name).ThenByDescending(item => item.PublicId)
    };

    private static IQueryable<BackToBasicsPillar> ApplyBackToBasicsOrdering(IQueryable<BackToBasicsPillar> query, string sortBy, bool descending, long? tenantId) => (sortBy, descending) switch
    {
        ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.PublicId),
        ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.PublicId),
        ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.PublicId),
        ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.PublicId),
        ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        ("availability", false) => query.OrderBy(item => !tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled)).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        ("availability", true) => query.OrderByDescending(item => !tenantId.HasValue || !item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value) || item.MunicipalityMappings.Any(mapping => mapping.MunicipalityId == tenantId.Value && mapping.IsEnabled)).ThenBy(item => item.DisplayOrder).ThenBy(item => item.PublicId),
        (_, false) => query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.PublicId),
        _ => query.OrderByDescending(item => item.DisplayOrder).ThenByDescending(item => item.Name).ThenByDescending(item => item.PublicId)
    };

    private ActionResult<ApiResponse<PagedResponse<GlobalStrategicReferenceDto>>> InvalidGlobalSort() =>
        BadRequest(Fail<PagedResponse<GlobalStrategicReferenceDto>>("SortBy must be displayOrder, code, name, status, or availability."));

    private static string? Validate(SaveGlobalStrategicReferenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 60) return "Code is required and may not exceed 60 characters.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 240) return "Name is required and may not exceed 240 characters.";
        if (request.Description?.Trim().Length > 2000) return "Description may not exceed 2000 characters.";
        if (request.DisplayOrder < 0) return "Display order must be zero or greater.";
        return ValidateReason(request.Reason);
    }

    private static string? ValidateReason(string? reason) => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10 || reason.Trim().Length > 1000 ? "A governance reason between 10 and 1000 characters is required." : null;
    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private bool TrySetVersion<TEntity>(TEntity entity, string? value) where TEntity : class { if (string.IsNullOrWhiteSpace(value)) return false; try { context.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); return true; } catch (FormatException) { return false; } }
    private async Task<ActionResult<ApiResponse<GlobalStrategicReferenceDto>>> SaveVersioned<TEntity>(TEntity entity, Func<TEntity, GlobalStrategicReferenceDto> map, string conflict) where TEntity : class { try { await context.SaveChangesAsync(); return Ok(new ApiResponse<GlobalStrategicReferenceDto>(true, map(entity))); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<GlobalStrategicReferenceDto>(conflict)); } }
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => BadRequest(Fail<T>("Select a municipality context before changing availability."));
    private ActionResult<ApiResponse<T>> SystemScopeRequired<T>() => StatusCode(StatusCodes.Status403Forbidden, Fail<T>("System scope is required to maintain global reference records."));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private void AddAudit(string entityName, Guid publicId, string action, object? oldValue, object newValue, string reason, long? municipalityId) => context.AuditTrails.Add(new AuditTrail { MunicipalityId = municipalityId, EntityName = entityName, EntityId = publicId.ToString(), Action = action, OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(newValue), ChangedBy = tenantContext.UserId ?? "UNKNOWN", ChangedAt = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CorrelationId = HttpContext.TraceIdentifier, Reason = reason.Trim(), UserAgent = Request.Headers.UserAgent.ToString() });
    private static object Snapshot(NationalKpa item) => new { item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive };
    private static object Snapshot(BackToBasicsPillar item) => new { item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive };
    private static GlobalStrategicReferenceDto ToDto(NationalKpa item) => new(item.PublicId, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive, true, null, null, Convert.ToBase64String(item.RowVersion));
    private static GlobalStrategicReferenceDto ToDto(BackToBasicsPillar item) => new(item.PublicId, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive, true, null, null, Convert.ToBase64String(item.RowVersion));
    private static GlobalStrategicReferenceDto ToDto(NationalKpa item, MunicipalityNationalKpa mapping) => new(item.PublicId, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive, mapping.IsEnabled, mapping.PublicId, Convert.ToBase64String(mapping.RowVersion), Convert.ToBase64String(item.RowVersion));
    private static GlobalStrategicReferenceDto ToDto(BackToBasicsPillar item, MunicipalityBackToBasicsPillar mapping) => new(item.PublicId, item.Code, item.Name, item.Description, item.DisplayOrder, item.IsActive, mapping.IsEnabled, mapping.PublicId, Convert.ToBase64String(mapping.RowVersion), Convert.ToBase64String(item.RowVersion));
    private static GlobalStrategicReferenceDto ToDto(GlobalStrategicReferenceDto item) => item with { RowVersion = Convert.ToBase64String(item.RowVersionBytes ?? []), AvailabilityRowVersion = item.AvailabilityRowVersionBytes == null ? null : Convert.ToBase64String(item.AvailabilityRowVersionBytes), RowVersionBytes = null, AvailabilityRowVersionBytes = null };
}

public sealed record GlobalStrategicReferenceDto(Guid PublicId, string Code, string Name, string? Description, int DisplayOrder, bool IsActive, bool IsEnabledForMunicipality, Guid? AvailabilityPublicId, string? AvailabilityRowVersion, string RowVersion)
{
    internal GlobalStrategicReferenceDto(Guid publicId, string code, string name, string? description, int displayOrder, bool isActive, bool isEnabledForMunicipality, Guid? availabilityPublicId, byte[]? availabilityRowVersionBytes, byte[] rowVersionBytes)
        : this(publicId, code, name, description, displayOrder, isActive, isEnabledForMunicipality, availabilityPublicId, null, string.Empty) { AvailabilityRowVersionBytes = availabilityRowVersionBytes; RowVersionBytes = rowVersionBytes; }
    internal byte[]? AvailabilityRowVersionBytes { get; init; }
    internal byte[]? RowVersionBytes { get; init; }
}

public sealed record SaveGlobalStrategicReferenceRequest(string Code, string Name, string? Description, int DisplayOrder, bool IsActive, string Reason, string? RowVersion = null);
public sealed record SaveMunicipalityReferenceAvailabilityRequest(bool IsEnabled, string Reason, string? RowVersion = null);

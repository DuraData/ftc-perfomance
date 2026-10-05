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
[Route("api/v1/strategic-planning")]
[Authorize]
public sealed class StrategicPlanningMastersController(ApplicationDbContext context, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("municipal-kpas/page"), Authorize(Policy = "Permission:MUNICIPAL_KPA.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetMunicipalKpas([FromQuery] StrategicPlanningPageRequest request) => Page(context.MunicipalKpas, request);

    [HttpGet("strategic-goals/page"), Authorize(Policy = "Permission:STRATEGIC_GOAL.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetStrategicGoals([FromQuery] StrategicPlanningPageRequest request) => Page(context.MunicipalStrategicGoals, request);

    [HttpGet("strategic-interventions/page"), Authorize(Policy = "Permission:STRATEGIC_INTERVENTION.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetStrategicInterventions([FromQuery] StrategicPlanningPageRequest request) => Page(context.StrategicInterventions, request);

    [HttpGet("strategic-objectives/page"), Authorize(Policy = "Permission:STRATEGIC_OBJECTIVE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetStrategicObjectives([FromQuery] StrategicPlanningPageRequest request) => Page(context.MunicipalStrategicObjectives, request);

    [HttpGet("performance-objectives/page"), Authorize(Policy = "Permission:PERFORMANCE_OBJECTIVE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetPerformanceObjectives([FromQuery] StrategicPlanningPageRequest request) => Page(context.PerformanceObjectives, request);

    [HttpPost("municipal-kpas"), Authorize(Policy = "Permission:MUNICIPAL_KPA.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateMunicipalKpa(SaveStrategicPlanningMasterRequest request) => Create(context.MunicipalKpas, () => new MunicipalKpa(), nameof(MunicipalKpa), request);

    [HttpPost("strategic-goals"), Authorize(Policy = "Permission:STRATEGIC_GOAL.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateStrategicGoal(SaveStrategicPlanningMasterRequest request) => Create(context.MunicipalStrategicGoals, () => new MunicipalStrategicGoal(), nameof(MunicipalStrategicGoal), request);

    [HttpPost("strategic-interventions"), Authorize(Policy = "Permission:STRATEGIC_INTERVENTION.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateStrategicIntervention(SaveStrategicPlanningMasterRequest request) => Create(context.StrategicInterventions, () => new StrategicIntervention(), nameof(StrategicIntervention), request);

    [HttpPost("strategic-objectives"), Authorize(Policy = "Permission:STRATEGIC_OBJECTIVE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateStrategicObjective(SaveStrategicPlanningMasterRequest request) => Create(context.MunicipalStrategicObjectives, () => new MunicipalStrategicObjective(), nameof(MunicipalStrategicObjective), request);

    [HttpPost("performance-objectives"), Authorize(Policy = "Permission:PERFORMANCE_OBJECTIVE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreatePerformanceObjective(SaveStrategicPlanningMasterRequest request) => Create(context.PerformanceObjectives, () => new PerformanceObjective(), nameof(PerformanceObjective), request);

    [HttpPut("municipal-kpas/{publicId:guid}"), Authorize(Policy = "Permission:MUNICIPAL_KPA.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateMunicipalKpa(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.MunicipalKpas, publicId, nameof(MunicipalKpa), request);

    [HttpPut("strategic-goals/{publicId:guid}"), Authorize(Policy = "Permission:STRATEGIC_GOAL.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateStrategicGoal(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.MunicipalStrategicGoals, publicId, nameof(MunicipalStrategicGoal), request);

    [HttpPut("strategic-interventions/{publicId:guid}"), Authorize(Policy = "Permission:STRATEGIC_INTERVENTION.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateStrategicIntervention(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.StrategicInterventions, publicId, nameof(StrategicIntervention), request);

    [HttpPut("strategic-objectives/{publicId:guid}"), Authorize(Policy = "Permission:STRATEGIC_OBJECTIVE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateStrategicObjective(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.MunicipalStrategicObjectives, publicId, nameof(MunicipalStrategicObjective), request);

    [HttpPut("performance-objectives/{publicId:guid}"), Authorize(Policy = "Permission:PERFORMANCE_OBJECTIVE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdatePerformanceObjective(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.PerformanceObjectives, publicId, nameof(PerformanceObjective), request);

    [HttpGet("relationships"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.READ")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto[]>>> GetRelationships([FromQuery] bool includeInactive = false)
    {
        if (!TenantSelected()) return TenantRequired<StrategicPlanningRelationshipDto[]>();
        var result = new List<StrategicPlanningRelationshipDto>();
        result.AddRange(await RelationshipRows(context.MunicipalKpaStrategicGoals, "municipal-kpa-strategic-goal", item => item.MunicipalKpa, item => item.StrategicGoal, includeInactive));
        result.AddRange(await RelationshipRows(context.StrategicGoalInterventions, "strategic-goal-intervention", item => item.StrategicGoal, item => item.StrategicIntervention, includeInactive));
        result.AddRange(await RelationshipRows(context.StrategicGoalObjectives, "strategic-goal-objective", item => item.StrategicGoal, item => item.StrategicObjective, includeInactive));
        result.AddRange(await RelationshipRows(context.StrategicInterventionObjectives, "strategic-intervention-objective", item => item.StrategicIntervention, item => item.StrategicObjective, includeInactive));
        result.AddRange(await RelationshipRows(context.StrategicObjectivePerformanceObjectives, "strategic-objective-performance-objective", item => item.StrategicObjective, item => item.PerformanceObjective, includeInactive));
        return Ok(new ApiResponse<StrategicPlanningRelationshipDto[]>(true, result.OrderBy(item => item.RelationshipType).ThenBy(item => item.ParentName).ThenBy(item => item.ChildName).ToArray()));
    }

    [HttpPost("relationships/municipal-kpa-strategic-goal"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> LinkMunicipalKpaToGoal(LinkStrategicPlanningRequest request)
    {
        var parents = await ResolvePair(context.MunicipalKpas, context.MunicipalStrategicGoals, request); if (parents.Error != null) return parents.Error;
        return await Link(context.MunicipalKpaStrategicGoals, () => new MunicipalKpaStrategicGoal { MunicipalKpaId = parents.Parent!.Id, StrategicGoalId = parents.Child!.Id }, "municipal-kpa-strategic-goal", parents.Parent!, parents.Child!, request);
    }

    [HttpPost("relationships/strategic-goal-intervention"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> LinkGoalToIntervention(LinkStrategicPlanningRequest request)
    {
        var pair = await ResolvePair(context.MunicipalStrategicGoals, context.StrategicInterventions, request); if (pair.Error != null) return pair.Error;
        return await Link(context.StrategicGoalInterventions, () => new StrategicGoalIntervention { StrategicGoalId = pair.Parent!.Id, StrategicInterventionId = pair.Child!.Id }, "strategic-goal-intervention", pair.Parent!, pair.Child!, request);
    }

    [HttpPost("relationships/strategic-goal-objective"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> LinkGoalToObjective(LinkStrategicPlanningRequest request)
    {
        var pair = await ResolvePair(context.MunicipalStrategicGoals, context.MunicipalStrategicObjectives, request); if (pair.Error != null) return pair.Error;
        return await Link(context.StrategicGoalObjectives, () => new StrategicGoalObjective { StrategicGoalId = pair.Parent!.Id, StrategicObjectiveId = pair.Child!.Id }, "strategic-goal-objective", pair.Parent!, pair.Child!, request);
    }

    [HttpPost("relationships/strategic-intervention-objective"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> LinkInterventionToObjective(LinkStrategicPlanningRequest request)
    {
        var pair = await ResolvePair(context.StrategicInterventions, context.MunicipalStrategicObjectives, request); if (pair.Error != null) return pair.Error;
        return await Link(context.StrategicInterventionObjectives, () => new StrategicInterventionObjective { StrategicInterventionId = pair.Parent!.Id, StrategicObjectiveId = pair.Child!.Id }, "strategic-intervention-objective", pair.Parent!, pair.Child!, request);
    }

    [HttpPost("relationships/strategic-objective-performance-objective"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.CREATE")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> LinkObjectiveToPerformanceObjective(LinkStrategicPlanningRequest request)
    {
        var pair = await ResolvePair(context.MunicipalStrategicObjectives, context.PerformanceObjectives, request); if (pair.Error != null) return pair.Error;
        return await Link(context.StrategicObjectivePerformanceObjectives, () => new StrategicObjectivePerformanceObjective { StrategicObjectiveId = pair.Parent!.Id, PerformanceObjectiveId = pair.Child!.Id }, "strategic-objective-performance-objective", pair.Parent!, pair.Child!, request);
    }

    [HttpPost("relationships/{publicId:guid}/disable"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.UPDATE")]
    public async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> DisableRelationship(Guid publicId, DisableStrategicPlanningRelationshipRequest request)
    {
        if (!TenantSelected()) return TenantRequired<StrategicPlanningRelationshipDto>();
        var error = ValidateReason(request.Reason); if (error != null) return BadRequest(Fail<StrategicPlanningRelationshipDto>(error));
        if (!TryVersion(request.RowVersion, out var expected)) return BadRequest(Fail<StrategicPlanningRelationshipDto>("A valid row version is required."));
        IStrategicPlanningRelationship? entity = await context.MunicipalKpaStrategicGoals.SingleOrDefaultAsync(item => item.PublicId == publicId)
            ?? (IStrategicPlanningRelationship?)await context.StrategicGoalInterventions.SingleOrDefaultAsync(item => item.PublicId == publicId)
            ?? (IStrategicPlanningRelationship?)await context.StrategicGoalObjectives.SingleOrDefaultAsync(item => item.PublicId == publicId)
            ?? (IStrategicPlanningRelationship?)await context.StrategicInterventionObjectives.SingleOrDefaultAsync(item => item.PublicId == publicId)
            ?? (IStrategicPlanningRelationship?)await context.StrategicObjectivePerformanceObjectives.SingleOrDefaultAsync(item => item.PublicId == publicId);
        if (entity == null) return NotFound(Fail<StrategicPlanningRelationshipDto>("Strategic relationship not found."));
        if (!entity.IsActive) return Conflict(Fail<StrategicPlanningRelationshipDto>("Strategic relationship is already inactive."));
        context.Entry((object)entity).Property(nameof(entity.RowVersion)).OriginalValue = expected;
        entity.IsActive = false;
        AddAudit(entity.GetType().Name, entity.PublicId, "DisableRelationship", new { IsActive = true }, new { IsActive = false }, request.Reason);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicPlanningRelationshipDto>("The relationship changed before this update. Reload and retry.")); }
        return Ok(new ApiResponse<StrategicPlanningRelationshipDto>(true, new(entity.PublicId, entity.GetType().Name, Guid.Empty, string.Empty, Guid.Empty, string.Empty, false, Convert.ToBase64String(entity.RowVersion))));
    }

    private async Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> Page<TEntity>(DbSet<TEntity> set, StrategicPlanningPageRequest request) where TEntity : StrategicPlanningMasterBase
    {
        if (!TenantSelected()) return TenantRequired<PagedResponse<StrategicPlanningMasterDto>>();
        var query = set.AsNoTracking().Include(item => item.EffectiveFromFinancialYear).ThenInclude(item => item!.FinancialYear).Include(item => item.EffectiveToFinancialYear).ThenInclude(item => item!.FinancialYear).AsQueryable();
        if (!request.IncludeInactive) query = query.Where(item => item.IsActive);
        if (request.MunicipalityFinancialYearPublicId.HasValue)
        {
            var selected = await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == request.MunicipalityFinancialYearPublicId);
            if (selected == null) return NotFound(Fail<PagedResponse<StrategicPlanningMasterDto>>("Municipality financial year not found."));
            query = query.Where(item => (!item.EffectiveFromFinancialYearId.HasValue || item.EffectiveFromFinancialYear!.FinancialYear.StartDate <= selected.FinancialYear.StartDate) && (!item.EffectiveToFinancialYearId.HasValue || item.EffectiveToFinancialYear!.FinancialYear.EndDate >= selected.FinancialYear.EndDate));
        }
        if (request.NormalizedSearch.Length > 0) query = query.Where(item => item.Name.Contains(request.NormalizedSearch) || (item.Code != null && item.Code.Contains(request.NormalizedSearch)) || (item.Description != null && item.Description.Contains(request.NormalizedSearch)));
        var total = await query.CountAsync();
        query = request.NormalizedSortBy switch
        {
            "code" => request.Descending ? query.OrderByDescending(item => item.Code) : query.OrderBy(item => item.Code),
            "name" => request.Descending ? query.OrderByDescending(item => item.Name) : query.OrderBy(item => item.Name),
            _ => request.Descending ? query.OrderByDescending(item => item.DisplayOrder).ThenByDescending(item => item.Name) : query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicPlanningMasterDto>>(true, PagedResponse<StrategicPlanningMasterDto>.Create(rows.Select(ToDto), request.Page, request.PageSize, total)));
    }

    private async Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> Create<TEntity>(DbSet<TEntity> set, Func<TEntity> factory, string entityName, SaveStrategicPlanningMasterRequest request) where TEntity : StrategicPlanningMasterBase
    {
        if (!TenantSelected()) return TenantRequired<StrategicPlanningMasterDto>();
        var normalized = await ValidateMaster(set, request, null); if (normalized.Error != null) return BadRequest(Fail<StrategicPlanningMasterDto>(normalized.Error));
        var entity = factory(); Apply(entity, request, normalized.Code, normalized.From, normalized.To); entity.MunicipalityId = tenantContext.MunicipalityId!.Value;
        set.Add(entity); AddAudit(entityName, entity.PublicId, "Create", null, Snapshot(entity), request.Reason);
        try { await context.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(Fail<StrategicPlanningMasterDto>("A record with this code already exists.")); }
        await LoadYears(entity); return Ok(new ApiResponse<StrategicPlanningMasterDto>(true, ToDto(entity)));
    }

    private async Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> Update<TEntity>(DbSet<TEntity> set, Guid publicId, string entityName, SaveStrategicPlanningMasterRequest request) where TEntity : StrategicPlanningMasterBase
    {
        if (!TenantSelected()) return TenantRequired<StrategicPlanningMasterDto>();
        if (!TryVersion(request.RowVersion, out var expected)) return BadRequest(Fail<StrategicPlanningMasterDto>("A valid row version is required."));
        var entity = await set.SingleOrDefaultAsync(item => item.PublicId == publicId); if (entity == null) return NotFound(Fail<StrategicPlanningMasterDto>("Strategic planning record not found."));
        var normalized = await ValidateMaster(set, request, entity.Id); if (normalized.Error != null) return BadRequest(Fail<StrategicPlanningMasterDto>(normalized.Error));
        var before = Snapshot(entity); context.Entry(entity).Property(item => item.RowVersion).OriginalValue = expected; Apply(entity, request, normalized.Code, normalized.From, normalized.To); AddAudit(entityName, entity.PublicId, "Update", before, Snapshot(entity), request.Reason);
        try { await context.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicPlanningMasterDto>("The record changed before this update. Reload and retry.")); } catch (DbUpdateException) { return Conflict(Fail<StrategicPlanningMasterDto>("A record with this code already exists.")); }
        await LoadYears(entity); return Ok(new ApiResponse<StrategicPlanningMasterDto>(true, ToDto(entity)));
    }

    private async Task<(string? Code, MunicipalityFinancialYear? From, MunicipalityFinancialYear? To, string? Error)> ValidateMaster<TEntity>(DbSet<TEntity> set, SaveStrategicPlanningMasterRequest request, long? excludeId) where TEntity : StrategicPlanningMasterBase
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 500) return (null, null, null, "Name is required and may not exceed 500 characters.");
        if (request.Code?.Trim().Length > 80 || request.Description?.Trim().Length > 2000 || request.DisplayOrder < 0) return (null, null, null, "Code, description, or display order is invalid.");
        var reasonError = ValidateReason(request.Reason); if (reasonError != null) return (null, null, null, reasonError);
        var code = Clean(request.Code)?.ToUpperInvariant();
        if (code != null && await set.AnyAsync(item => item.Code == code && (!excludeId.HasValue || item.Id != excludeId.Value))) return (null, null, null, "A record with this code already exists.");
        var years = await ResolveYears(request.EffectiveFromFinancialYearPublicId, request.EffectiveToFinancialYearPublicId); if (years.Error != null) return (null, null, null, years.Error);
        return (code, years.From, years.To, null);
    }

    private async Task<(MunicipalityFinancialYear? From, MunicipalityFinancialYear? To, string? Error)> ResolveYears(Guid? fromId, Guid? toId)
    {
        MunicipalityFinancialYear? from = null, to = null;
        if (fromId.HasValue) from = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == fromId);
        if (toId.HasValue) to = await context.MunicipalityFinancialYears.Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == toId);
        if (fromId.HasValue && from == null || toId.HasValue && to == null) return (null, null, "An effective financial year was not found in this municipality.");
        if (from != null && to != null && from.FinancialYear.StartDate > to.FinancialYear.StartDate) return (null, null, "Effective-from financial year must not follow effective-to financial year.");
        return (from, to, null);
    }

    private async Task<(TParent? Parent, TChild? Child, ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>? Error)> ResolvePair<TParent, TChild>(DbSet<TParent> parents, DbSet<TChild> children, LinkStrategicPlanningRequest request) where TParent : StrategicPlanningMasterBase where TChild : StrategicPlanningMasterBase
    {
        if (!TenantSelected()) return (null, null, TenantRequired<StrategicPlanningRelationshipDto>());
        var reasonError = ValidateReason(request.Reason); if (reasonError != null) return (null, null, BadRequest(Fail<StrategicPlanningRelationshipDto>(reasonError)));
        var parent = await parents.Include(item => item.EffectiveFromFinancialYear).ThenInclude(item => item!.FinancialYear).Include(item => item.EffectiveToFinancialYear).ThenInclude(item => item!.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == request.ParentPublicId);
        var child = await children.Include(item => item.EffectiveFromFinancialYear).ThenInclude(item => item!.FinancialYear).Include(item => item.EffectiveToFinancialYear).ThenInclude(item => item!.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == request.ChildPublicId);
        if (parent == null || child == null) return (null, null, NotFound(Fail<StrategicPlanningRelationshipDto>("Parent or child record was not found in this municipality.")));
        if (!parent.IsActive || !child.IsActive) return (null, null, BadRequest(Fail<StrategicPlanningRelationshipDto>("Only active strategic records can be linked.")));
        var parentStart = parent.EffectiveFromFinancialYear?.FinancialYear.StartDate ?? DateTime.MinValue; var parentEnd = parent.EffectiveToFinancialYear?.FinancialYear.EndDate ?? DateTime.MaxValue;
        var childStart = child.EffectiveFromFinancialYear?.FinancialYear.StartDate ?? DateTime.MinValue; var childEnd = child.EffectiveToFinancialYear?.FinancialYear.EndDate ?? DateTime.MaxValue;
        if (parentStart > childEnd || childStart > parentEnd) return (null, null, BadRequest(Fail<StrategicPlanningRelationshipDto>("The parent and child effective financial-year ranges do not overlap.")));
        return (parent, child, null);
    }

    private async Task<ActionResult<ApiResponse<StrategicPlanningRelationshipDto>>> Link<TEntity>(DbSet<TEntity> set, Func<TEntity> factory, string type, StrategicPlanningMasterBase parent, StrategicPlanningMasterBase child, LinkStrategicPlanningRequest request) where TEntity : StrategicPlanningRelationshipBase
    {
        var existing = await set.SingleOrDefaultAsync(item => EF.Property<long>(item, ParentKey<TEntity>()) == parent.Id && EF.Property<long>(item, ChildKey<TEntity>()) == child.Id);
        if (existing?.IsActive == true) return Conflict(Fail<StrategicPlanningRelationshipDto>("This strategic relationship already exists."));
        TEntity entity;
        if (existing == null) { entity = factory(); entity.MunicipalityId = tenantContext.MunicipalityId!.Value; set.Add(entity); }
        else
        {
            if (!TryVersion(request.RowVersion, out var expected)) return Conflict(Fail<StrategicPlanningRelationshipDto>("The inactive relationship already exists. Supply its current row version to reactivate it."));
            context.Entry(existing).Property(item => item.RowVersion).OriginalValue = expected;
            entity = existing; entity.IsActive = true;
        }
        AddAudit(typeof(TEntity).Name, entity.PublicId, existing == null ? "CreateRelationship" : "ReactivateRelationship", null, new { ParentPublicId = parent.PublicId, ChildPublicId = child.PublicId, IsActive = true }, request.Reason);
        try { await context.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(Fail<StrategicPlanningRelationshipDto>("The relationship changed before reactivation. Reload and retry.")); } catch (DbUpdateException) { return Conflict(Fail<StrategicPlanningRelationshipDto>("This strategic relationship already exists.")); }
        return Ok(new ApiResponse<StrategicPlanningRelationshipDto>(true, new(entity.PublicId, type, parent.PublicId, parent.Name, child.PublicId, child.Name, true, Convert.ToBase64String(entity.RowVersion))));
    }

    private static string ParentKey<TEntity>() => typeof(TEntity) == typeof(MunicipalKpaStrategicGoal) ? nameof(MunicipalKpaStrategicGoal.MunicipalKpaId) : typeof(TEntity) == typeof(StrategicGoalIntervention) || typeof(TEntity) == typeof(StrategicGoalObjective) ? nameof(StrategicGoalIntervention.StrategicGoalId) : typeof(TEntity) == typeof(StrategicInterventionObjective) ? nameof(StrategicInterventionObjective.StrategicInterventionId) : nameof(StrategicObjectivePerformanceObjective.StrategicObjectiveId);
    private static string ChildKey<TEntity>() => typeof(TEntity) == typeof(MunicipalKpaStrategicGoal) ? nameof(MunicipalKpaStrategicGoal.StrategicGoalId) : typeof(TEntity) == typeof(StrategicGoalIntervention) ? nameof(StrategicGoalIntervention.StrategicInterventionId) : typeof(TEntity) == typeof(StrategicGoalObjective) || typeof(TEntity) == typeof(StrategicInterventionObjective) ? nameof(StrategicGoalObjective.StrategicObjectiveId) : nameof(StrategicObjectivePerformanceObjective.PerformanceObjectiveId);

    private static async Task<StrategicPlanningRelationshipDto[]> RelationshipRows<TEntity, TParent, TChild>(DbSet<TEntity> set, string type, System.Linq.Expressions.Expression<Func<TEntity, TParent>> parent, System.Linq.Expressions.Expression<Func<TEntity, TChild>> child, bool includeInactive) where TEntity : StrategicPlanningRelationshipBase where TParent : StrategicPlanningMasterBase where TChild : StrategicPlanningMasterBase
    {
        var query = set.AsNoTracking().Include(parent).Include(child).AsQueryable(); if (!includeInactive) query = query.Where(item => item.IsActive);
        var rows = await query.ToArrayAsync();
        var getParent = parent.Compile(); var getChild = child.Compile();
        return rows.Select(item => { var p = getParent(item); var c = getChild(item); return new StrategicPlanningRelationshipDto(item.PublicId, type, p.PublicId, p.Name, c.PublicId, c.Name, item.IsActive, Convert.ToBase64String(item.RowVersion)); }).ToArray();
    }

    private void Apply(StrategicPlanningMasterBase entity, SaveStrategicPlanningMasterRequest request, string? code, MunicipalityFinancialYear? from, MunicipalityFinancialYear? to) { entity.Code = code; entity.Name = request.Name.Trim(); entity.Description = Clean(request.Description); entity.EffectiveFromFinancialYearId = from?.Id; entity.EffectiveToFinancialYearId = to?.Id; entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive; }
    private async Task LoadYears(StrategicPlanningMasterBase entity) { if (entity.EffectiveFromFinancialYearId.HasValue) await context.Entry(entity).Reference(item => item.EffectiveFromFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync(); if (entity.EffectiveToFinancialYearId.HasValue) await context.Entry(entity).Reference(item => item.EffectiveToFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync(); }
    private static StrategicPlanningMasterDto ToDto(StrategicPlanningMasterBase item) => new(item.PublicId, item.Code, item.Name, item.Description, item.EffectiveFromFinancialYear?.PublicId, item.EffectiveFromFinancialYear?.FinancialYear.Code, item.EffectiveToFinancialYear?.PublicId, item.EffectiveToFinancialYear?.FinancialYear.Code, item.DisplayOrder, item.IsActive, Convert.ToBase64String(item.RowVersion));
    private static object Snapshot(StrategicPlanningMasterBase item) => new { item.Code, item.Name, item.Description, item.EffectiveFromFinancialYearId, item.EffectiveToFinancialYearId, item.DisplayOrder, item.IsActive };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? ValidateReason(string? reason) => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10 || reason.Trim().Length > 1000 ? "A governance reason between 10 and 1000 characters is required." : null;
    private static bool TryVersion(string? value, out byte[] version) { version = []; if (string.IsNullOrWhiteSpace(value)) return false; try { version = Convert.FromBase64String(value); return version.Length > 0; } catch (FormatException) { return false; } }
    private bool TenantSelected() => tenantContext.MunicipalityId.HasValue;
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => BadRequest(Fail<T>("Select a municipality context before maintaining strategic planning records."));
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private void AddAudit(string entityName, Guid publicId, string action, object? oldValue, object newValue, string reason) => context.AuditTrails.Add(new AuditTrail { MunicipalityId = tenantContext.MunicipalityId, EntityName = entityName, EntityId = publicId.ToString(), Action = action, OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = JsonSerializer.Serialize(newValue), ChangedBy = tenantContext.UserId ?? "UNKNOWN", ChangedAt = DateTime.UtcNow, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CorrelationId = HttpContext.TraceIdentifier, Reason = reason.Trim(), UserAgent = Request.Headers.UserAgent.ToString() });
}

public sealed class StrategicPlanningPageRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, 10_000_000)] public int Page { get; init; } = 1;
    [System.ComponentModel.DataAnnotations.Range(1, 100)] public int PageSize { get; init; } = 25;
    [System.ComponentModel.DataAnnotations.StringLength(200)] public string? Search { get; init; }
    [System.ComponentModel.DataAnnotations.StringLength(50)] public string? SortBy { get; init; }
    [System.ComponentModel.DataAnnotations.RegularExpression("^(?i:asc|desc)$")] public string SortDirection { get; init; } = "asc";
    public Guid? MunicipalityFinancialYearPublicId { get; init; }
    public bool IncludeInactive { get; init; }
    public int Offset => checked((Page - 1) * PageSize);
    public bool Descending => string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
    public string NormalizedSearch => Search?.Trim() ?? string.Empty;
    public string NormalizedSortBy => SortBy?.Trim().ToLowerInvariant() ?? "displayorder";
}

public sealed record StrategicPlanningMasterDto(Guid PublicId, string? Code, string Name, string? Description, Guid? EffectiveFromFinancialYearPublicId, string? EffectiveFromFinancialYearCode, Guid? EffectiveToFinancialYearPublicId, string? EffectiveToFinancialYearCode, int DisplayOrder, bool IsActive, string RowVersion);
public sealed record SaveStrategicPlanningMasterRequest(string? Code, string Name, string? Description, Guid? EffectiveFromFinancialYearPublicId, Guid? EffectiveToFinancialYearPublicId, int DisplayOrder, bool IsActive, string Reason, string? RowVersion = null);
public sealed record StrategicPlanningRelationshipDto(Guid PublicId, string RelationshipType, Guid ParentPublicId, string ParentName, Guid ChildPublicId, string ChildName, bool IsActive, string RowVersion);
public sealed record LinkStrategicPlanningRequest(Guid ParentPublicId, Guid ChildPublicId, string Reason, string? RowVersion = null);
public sealed record DisableStrategicPlanningRelationshipRequest(string Reason, string RowVersion);

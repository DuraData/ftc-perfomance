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
    [HttpGet("catalogue/opms"), Authorize(Policy = "Permission:OPMS_KPI.READ")]
    public ActionResult<ApiResponse<StrategicClassificationCatalogueDto>> GetOpmsCatalogue([FromQuery] Guid municipalityFinancialYearPublicId)
    {
        _ = municipalityFinancialYearPublicId;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<StrategicClassificationCatalogueDto>(false, null,
            "This aggregate catalogue route is retired. Use /api/v1/strategic-planning/catalogue/opms/page with classificationKind and bounded paging."));
    }

    [HttpGet("catalogue/ipms"), Authorize(Policy = "Permission:IPMS_KPI.READ")]
    public ActionResult<ApiResponse<StrategicClassificationCatalogueDto>> GetIpmsCatalogue([FromQuery] Guid municipalityFinancialYearPublicId)
    {
        _ = municipalityFinancialYearPublicId;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<StrategicClassificationCatalogueDto>(false, null,
            "This aggregate catalogue route is retired. Use /api/v1/strategic-planning/catalogue/ipms/page with classificationKind and bounded paging."));
    }

    [HttpGet("catalogue/opms/page"), Authorize(Policy = "Permission:OPMS_KPI.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicCatalogueItemDto>>>> GetOpmsCataloguePage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] Guid municipalityFinancialYearPublicId,
        [FromQuery] string classificationKind,
        [FromQuery] Guid? parentPublicId = null,
        [FromQuery] string? relationshipType = null) =>
        CataloguePage(request, municipalityFinancialYearPublicId, classificationKind, parentPublicId, relationshipType);

    [HttpGet("catalogue/ipms/page"), Authorize(Policy = "Permission:IPMS_KPI.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicCatalogueItemDto>>>> GetIpmsCataloguePage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] Guid municipalityFinancialYearPublicId,
        [FromQuery] string classificationKind,
        [FromQuery] Guid? parentPublicId = null,
        [FromQuery] string? relationshipType = null) =>
        CataloguePage(request, municipalityFinancialYearPublicId, classificationKind, parentPublicId, relationshipType);

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

    [HttpGet("budget-sources/page"), Authorize(Policy = "Permission:BUDGET_SOURCE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetBudgetSources([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedBudgetSources, request);

    [HttpGet("budget-types/page"), Authorize(Policy = "Permission:BUDGET_TYPE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetBudgetTypes([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedBudgetTypes, request);

    [HttpGet("kpi-types/page"), Authorize(Policy = "Permission:KPI_TYPE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetKpiTypes([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedKpiTypes, request);
    [HttpGet("indicator-types/page"), Authorize(Policy = "Permission:INDICATOR_TYPE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetIndicatorTypes([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedIndicatorTypes, request);
    [HttpGet("functional-areas/page"), Authorize(Policy = "Permission:FUNCTIONAL_AREA.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetFunctionalAreas([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedFunctionalAreas, request);
    [HttpGet("standard-classifications/page"), Authorize(Policy = "Permission:STANDARD_CLASSIFICATION.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetStandardClassifications([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedStandardClassifications, request);
    [HttpGet("kpi-units-of-measure/page"), Authorize(Policy = "Permission:KPI_UNIT_OF_MEASURE.READ")]
    public Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningMasterDto>>>> GetKpiUnitsOfMeasure([FromQuery] StrategicPlanningPageRequest request) => Page(context.GovernedKpiUnitOfMeasures, request);

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

    [HttpPost("budget-sources"), Authorize(Policy = "Permission:BUDGET_SOURCE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateBudgetSource(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedBudgetSources, () => new GovernedBudgetSource(), nameof(GovernedBudgetSource), request);

    [HttpPost("budget-types"), Authorize(Policy = "Permission:BUDGET_TYPE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateBudgetType(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedBudgetTypes, () => new GovernedBudgetType(), nameof(GovernedBudgetType), request);

    [HttpPost("kpi-types"), Authorize(Policy = "Permission:KPI_TYPE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateKpiType(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedKpiTypes, () => new GovernedKpiType(), nameof(GovernedKpiType), request);
    [HttpPost("indicator-types"), Authorize(Policy = "Permission:INDICATOR_TYPE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateIndicatorType(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedIndicatorTypes, () => new GovernedIndicatorType(), nameof(GovernedIndicatorType), request);
    [HttpPost("functional-areas"), Authorize(Policy = "Permission:FUNCTIONAL_AREA.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateFunctionalArea(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedFunctionalAreas, () => new GovernedFunctionalArea(), nameof(GovernedFunctionalArea), request);
    [HttpPost("standard-classifications"), Authorize(Policy = "Permission:STANDARD_CLASSIFICATION.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateStandardClassification(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedStandardClassifications, () => new GovernedStandardClassification(), nameof(GovernedStandardClassification), request);
    [HttpPost("kpi-units-of-measure"), Authorize(Policy = "Permission:KPI_UNIT_OF_MEASURE.CREATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> CreateKpiUnitOfMeasure(SaveStrategicPlanningMasterRequest request) => Create(context.GovernedKpiUnitOfMeasures, () => new GovernedKpiUnitOfMeasure(), nameof(GovernedKpiUnitOfMeasure), request);

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

    [HttpPut("budget-sources/{publicId:guid}"), Authorize(Policy = "Permission:BUDGET_SOURCE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateBudgetSource(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedBudgetSources, publicId, nameof(GovernedBudgetSource), request);

    [HttpPut("budget-types/{publicId:guid}"), Authorize(Policy = "Permission:BUDGET_TYPE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateBudgetType(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedBudgetTypes, publicId, nameof(GovernedBudgetType), request);

    [HttpPut("kpi-types/{publicId:guid}"), Authorize(Policy = "Permission:KPI_TYPE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateKpiType(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedKpiTypes, publicId, nameof(GovernedKpiType), request);
    [HttpPut("indicator-types/{publicId:guid}"), Authorize(Policy = "Permission:INDICATOR_TYPE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateIndicatorType(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedIndicatorTypes, publicId, nameof(GovernedIndicatorType), request);
    [HttpPut("functional-areas/{publicId:guid}"), Authorize(Policy = "Permission:FUNCTIONAL_AREA.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateFunctionalArea(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedFunctionalAreas, publicId, nameof(GovernedFunctionalArea), request);
    [HttpPut("standard-classifications/{publicId:guid}"), Authorize(Policy = "Permission:STANDARD_CLASSIFICATION.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateStandardClassification(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedStandardClassifications, publicId, nameof(GovernedStandardClassification), request);
    [HttpPut("kpi-units-of-measure/{publicId:guid}"), Authorize(Policy = "Permission:KPI_UNIT_OF_MEASURE.UPDATE")]
    public Task<ActionResult<ApiResponse<StrategicPlanningMasterDto>>> UpdateKpiUnitOfMeasure(Guid publicId, SaveStrategicPlanningMasterRequest request) => Update(context.GovernedKpiUnitOfMeasures, publicId, nameof(GovernedKpiUnitOfMeasure), request);

    [HttpGet("relationships"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.READ")]
    public ActionResult<ApiResponse<StrategicPlanningRelationshipDto[]>> GetRelationships([FromQuery] bool includeInactive = false)
    {
        _ = includeInactive;
        return StatusCode(StatusCodes.Status410Gone, new ApiResponse<StrategicPlanningRelationshipDto[]>(false, null,
            "This unbounded route is retired. Use /api/v1/strategic-planning/relationships/page."));
    }

    [HttpGet("relationships/page"), Authorize(Policy = "Permission:STRATEGIC_HIERARCHY.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StrategicPlanningRelationshipDto>>>> GetRelationshipsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? relationshipType,
        [FromQuery] bool includeInactive = false,
        [FromQuery] Guid? parentPublicId = null,
        [FromQuery] Guid? childPublicId = null)
    {
        if (!TenantSelected()) return TenantRequired<PagedResponse<StrategicPlanningRelationshipDto>>();
        var type = relationshipType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!RelationshipTypes.Contains(type))
            return BadRequest(Fail<PagedResponse<StrategicPlanningRelationshipDto>>("RelationshipType is invalid."));
        var sortBy = request.SortBy == null ? "parentname" : request.NormalizedSortBy;
        if (sortBy is not ("parentname" or "childname" or "status"))
            return BadRequest(Fail<PagedResponse<StrategicPlanningRelationshipDto>>("SortBy must be parentName, childName, or status."));

        var query = RelationshipQuery(type);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (parentPublicId.HasValue) query = query.Where(item => item.ParentPublicId == parentPublicId.Value);
        if (childPublicId.HasValue) query = query.Where(item => item.ChildPublicId == childPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.ParentName.Contains(request.NormalizedSearch) || item.ChildName.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        query = (sortBy, request.Descending) switch
        {
            ("childname", false) => query.OrderBy(item => item.ChildName).ThenBy(item => item.ParentName).ThenBy(item => item.PublicId),
            ("childname", true) => query.OrderByDescending(item => item.ChildName).ThenByDescending(item => item.ParentName).ThenByDescending(item => item.PublicId),
            ("status", false) => query.OrderBy(item => item.IsActive).ThenBy(item => item.ParentName).ThenBy(item => item.ChildName).ThenBy(item => item.PublicId),
            ("status", true) => query.OrderByDescending(item => item.IsActive).ThenBy(item => item.ParentName).ThenBy(item => item.ChildName).ThenBy(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.ParentName).ThenBy(item => item.ChildName).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.ParentName).ThenByDescending(item => item.ChildName).ThenByDescending(item => item.PublicId)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicPlanningRelationshipDto>>(true,
            PagedResponse<StrategicPlanningRelationshipDto>.Create(rows.Select(item => new StrategicPlanningRelationshipDto(
                item.PublicId, item.RelationshipType, item.ParentPublicId, item.ParentName, item.ChildPublicId, item.ChildName,
                item.IsActive, Convert.ToBase64String(item.RowVersion))), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> RelationshipTypes =
    [
        "municipal-kpa-strategic-goal", "strategic-goal-intervention", "strategic-goal-objective",
        "strategic-intervention-objective", "strategic-objective-performance-objective"
    ];

    private IQueryable<StrategicPlanningRelationshipPageRow> RelationshipQuery(string type) => type switch
    {
        "municipal-kpa-strategic-goal" => context.MunicipalKpaStrategicGoals.AsNoTracking().Select(item => new StrategicPlanningRelationshipPageRow
        {
            PublicId = item.PublicId, RelationshipType = type, ParentPublicId = item.MunicipalKpa.PublicId, ParentName = item.MunicipalKpa.Name,
            ChildPublicId = item.StrategicGoal.PublicId, ChildName = item.StrategicGoal.Name, IsActive = item.IsActive, RowVersion = item.RowVersion
        }),
        "strategic-goal-intervention" => context.StrategicGoalInterventions.AsNoTracking().Select(item => new StrategicPlanningRelationshipPageRow
        {
            PublicId = item.PublicId, RelationshipType = type, ParentPublicId = item.StrategicGoal.PublicId, ParentName = item.StrategicGoal.Name,
            ChildPublicId = item.StrategicIntervention.PublicId, ChildName = item.StrategicIntervention.Name, IsActive = item.IsActive, RowVersion = item.RowVersion
        }),
        "strategic-goal-objective" => context.StrategicGoalObjectives.AsNoTracking().Select(item => new StrategicPlanningRelationshipPageRow
        {
            PublicId = item.PublicId, RelationshipType = type, ParentPublicId = item.StrategicGoal.PublicId, ParentName = item.StrategicGoal.Name,
            ChildPublicId = item.StrategicObjective.PublicId, ChildName = item.StrategicObjective.Name, IsActive = item.IsActive, RowVersion = item.RowVersion
        }),
        "strategic-intervention-objective" => context.StrategicInterventionObjectives.AsNoTracking().Select(item => new StrategicPlanningRelationshipPageRow
        {
            PublicId = item.PublicId, RelationshipType = type, ParentPublicId = item.StrategicIntervention.PublicId, ParentName = item.StrategicIntervention.Name,
            ChildPublicId = item.StrategicObjective.PublicId, ChildName = item.StrategicObjective.Name, IsActive = item.IsActive, RowVersion = item.RowVersion
        }),
        _ => context.StrategicObjectivePerformanceObjectives.AsNoTracking().Select(item => new StrategicPlanningRelationshipPageRow
        {
            PublicId = item.PublicId, RelationshipType = type, ParentPublicId = item.StrategicObjective.PublicId, ParentName = item.StrategicObjective.Name,
            ChildPublicId = item.PerformanceObjective.PublicId, ChildName = item.PerformanceObjective.Name, IsActive = item.IsActive, RowVersion = item.RowVersion
        })
    };

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

    private static readonly HashSet<string> CatalogueKinds =
    [
        "national-kpas", "municipal-kpas", "back-to-basics-pillars", "strategic-goals", "strategic-interventions",
        "strategic-objectives", "performance-objectives", "budget-sources", "budget-types", "kpi-types",
        "indicator-types", "functional-areas", "standard-classifications", "kpi-units-of-measure"
    ];

    private static readonly IReadOnlyDictionary<string, HashSet<string>> CatalogueRelationshipKinds =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["strategic-goals"] = ["municipal-kpa-strategic-goal"],
            ["strategic-interventions"] = ["strategic-goal-intervention"],
            ["strategic-objectives"] = ["strategic-goal-objective", "strategic-intervention-objective"],
            ["performance-objectives"] = ["strategic-objective-performance-objective"]
        };

    private async Task<ActionResult<ApiResponse<PagedResponse<StrategicCatalogueItemDto>>>> CataloguePage(
        PagedQueryRequest request,
        Guid municipalityFinancialYearPublicId,
        string classificationKind,
        Guid? parentPublicId,
        string? relationshipType)
    {
        if (!TenantSelected()) return TenantRequired<PagedResponse<StrategicCatalogueItemDto>>();
        var kind = classificationKind.Trim().ToLowerInvariant();
        if (!CatalogueKinds.Contains(kind))
            return BadRequest(Fail<PagedResponse<StrategicCatalogueItemDto>>("ClassificationKind is invalid."));
        var sortBy = request.SortBy == null ? "displayorder" : request.NormalizedSortBy;
        if (sortBy is not ("displayorder" or "code" or "name"))
            return BadRequest(Fail<PagedResponse<StrategicCatalogueItemDto>>("SortBy must be displayOrder, code, or name."));
        var descending = request.SortBy != null && request.Descending;
        var year = await context.MunicipalityFinancialYears.AsNoTracking().Include(item => item.FinancialYear).SingleOrDefaultAsync(item => item.PublicId == municipalityFinancialYearPublicId);
        if (year == null) return NotFound(Fail<PagedResponse<StrategicCatalogueItemDto>>("Municipality financial year not found."));

        IQueryable<StrategicCataloguePageRow> query = kind switch
        {
            "national-kpas" => context.NationalKpas.AsNoTracking().Where(item => item.IsActive
                    && (!item.MunicipalityMappings.Any() || item.MunicipalityMappings.Any(mapping => mapping.IsEnabled)))
                .Select(item => new StrategicCataloguePageRow { PublicId = item.PublicId, Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder }),
            "back-to-basics-pillars" => context.BackToBasicsPillars.AsNoTracking().Where(item => item.IsActive
                    && (!item.MunicipalityMappings.Any() || item.MunicipalityMappings.Any(mapping => mapping.IsEnabled)))
                .Select(item => new StrategicCataloguePageRow { PublicId = item.PublicId, Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder }),
            "municipal-kpas" => CatalogueQuery(context.MunicipalKpas, year),
            "strategic-goals" => CatalogueQuery(context.MunicipalStrategicGoals, year),
            "strategic-interventions" => CatalogueQuery(context.StrategicInterventions, year),
            "strategic-objectives" => CatalogueQuery(context.MunicipalStrategicObjectives, year),
            "performance-objectives" => CatalogueQuery(context.PerformanceObjectives, year),
            "budget-sources" => CatalogueQuery(context.GovernedBudgetSources, year),
            "budget-types" => CatalogueQuery(context.GovernedBudgetTypes, year),
            "kpi-types" => CatalogueQuery(context.GovernedKpiTypes, year),
            "indicator-types" => CatalogueQuery(context.GovernedIndicatorTypes, year),
            "functional-areas" => CatalogueQuery(context.GovernedFunctionalAreas, year),
            "standard-classifications" => CatalogueQuery(context.GovernedStandardClassifications, year),
            _ => context.GovernedKpiUnitOfMeasures.AsNoTracking().Where(item => item.IsActive
                    && (!item.EffectiveFromFinancialYearId.HasValue || item.EffectiveFromFinancialYear!.FinancialYear.StartDate <= year.FinancialYear.StartDate)
                    && (!item.EffectiveToFinancialYearId.HasValue || item.EffectiveToFinancialYear!.FinancialYear.EndDate >= year.FinancialYear.EndDate))
                .Select(item => new StrategicCataloguePageRow { PublicId = item.PublicId, Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder, Symbol = item.Symbol })
        };

        if (parentPublicId.HasValue)
        {
            var relationship = relationshipType?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!CatalogueRelationshipKinds.TryGetValue(kind, out var supportedRelationships) || !supportedRelationships.Contains(relationship))
                return BadRequest(Fail<PagedResponse<StrategicCatalogueItemDto>>("RelationshipType is invalid for this classification kind."));
            var configuredRelationships = RelationshipQuery(relationship)
                .Where(item => item.IsActive && item.ParentPublicId == parentPublicId.Value);
            if (await configuredRelationships.AnyAsync())
            {
                var childIds = configuredRelationships.Select(item => item.ChildPublicId);
                query = query.Where(item => childIds.Contains(item.PublicId));
            }
        }
        else if (!string.IsNullOrWhiteSpace(relationshipType))
        {
            return BadRequest(Fail<PagedResponse<StrategicCatalogueItemDto>>("ParentPublicId is required when RelationshipType is supplied."));
        }

        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Name.Contains(request.NormalizedSearch) || (item.Code != null && item.Code.Contains(request.NormalizedSearch)));
        var totalCount = await query.CountAsync();
        query = (sortBy, descending) switch
        {
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Name).ThenBy(item => item.PublicId),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Name).ThenByDescending(item => item.PublicId),
            ("name", false) => query.OrderBy(item => item.Name).ThenBy(item => item.PublicId),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.PublicId),
            (_, false) => query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.DisplayOrder).ThenByDescending(item => item.Name).ThenByDescending(item => item.PublicId)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<StrategicCatalogueItemDto>>(true,
            PagedResponse<StrategicCatalogueItemDto>.Create(
                rows.Select(item => new StrategicCatalogueItemDto(item.PublicId, item.Code, item.Name, item.DisplayOrder, item.Symbol)),
                request.Page, request.PageSize, totalCount)));
    }

    private static IQueryable<StrategicCataloguePageRow> CatalogueQuery<TEntity>(DbSet<TEntity> set, MunicipalityFinancialYear year) where TEntity : StrategicPlanningMasterBase =>
        set.AsNoTracking().Where(item => item.IsActive
                && (!item.EffectiveFromFinancialYearId.HasValue || item.EffectiveFromFinancialYear!.FinancialYear.StartDate <= year.FinancialYear.StartDate)
                && (!item.EffectiveToFinancialYearId.HasValue || item.EffectiveToFinancialYear!.FinancialYear.EndDate >= year.FinancialYear.EndDate))
            .Select(item => new StrategicCataloguePageRow { PublicId = item.PublicId, Code = item.Code, Name = item.Name, DisplayOrder = item.DisplayOrder });

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
        if (request.Code?.Trim().Length > 80 || request.Description?.Trim().Length > 2000 || request.Symbol?.Trim().Length > 40 || request.DisplayOrder < 0) return (null, null, null, "Code, description, symbol, or display order is invalid.");
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

    private void Apply(StrategicPlanningMasterBase entity, SaveStrategicPlanningMasterRequest request, string? code, MunicipalityFinancialYear? from, MunicipalityFinancialYear? to) { entity.Code = code; entity.Name = request.Name.Trim(); entity.Description = Clean(request.Description); entity.EffectiveFromFinancialYearId = from?.Id; entity.EffectiveToFinancialYearId = to?.Id; entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive; if (entity is GovernedKpiUnitOfMeasure unit) unit.Symbol = Clean(request.Symbol); }
    private async Task LoadYears(StrategicPlanningMasterBase entity) { if (entity.EffectiveFromFinancialYearId.HasValue) await context.Entry(entity).Reference(item => item.EffectiveFromFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync(); if (entity.EffectiveToFinancialYearId.HasValue) await context.Entry(entity).Reference(item => item.EffectiveToFinancialYear).Query().Include(item => item.FinancialYear).LoadAsync(); }
    private static StrategicPlanningMasterDto ToDto(StrategicPlanningMasterBase item) => new(item.PublicId, item.Code, item.Name, item.Description, item.EffectiveFromFinancialYear?.PublicId, item.EffectiveFromFinancialYear?.FinancialYear.Code, item.EffectiveToFinancialYear?.PublicId, item.EffectiveToFinancialYear?.FinancialYear.Code, item.DisplayOrder, item.IsActive, Convert.ToBase64String(item.RowVersion), (item as GovernedKpiUnitOfMeasure)?.Symbol);
    private static object Snapshot(StrategicPlanningMasterBase item) => new { item.Code, item.Name, item.Description, Symbol = (item as GovernedKpiUnitOfMeasure)?.Symbol, item.EffectiveFromFinancialYearId, item.EffectiveToFinancialYearId, item.DisplayOrder, item.IsActive };
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

public sealed record StrategicPlanningMasterDto(Guid PublicId, string? Code, string Name, string? Description, Guid? EffectiveFromFinancialYearPublicId, string? EffectiveFromFinancialYearCode, Guid? EffectiveToFinancialYearPublicId, string? EffectiveToFinancialYearCode, int DisplayOrder, bool IsActive, string RowVersion, string? Symbol = null);
public sealed record SaveStrategicPlanningMasterRequest(string? Code, string Name, string? Description, Guid? EffectiveFromFinancialYearPublicId, Guid? EffectiveToFinancialYearPublicId, int DisplayOrder, bool IsActive, string Reason, string? RowVersion = null, string? Symbol = null);
public sealed record StrategicPlanningRelationshipDto(Guid PublicId, string RelationshipType, Guid ParentPublicId, string ParentName, Guid ChildPublicId, string ChildName, bool IsActive, string RowVersion);
internal sealed class StrategicPlanningRelationshipPageRow
{
    public Guid PublicId { get; init; }
    public string RelationshipType { get; init; } = string.Empty;
    public Guid ParentPublicId { get; init; }
    public string ParentName { get; init; } = string.Empty;
    public Guid ChildPublicId { get; init; }
    public string ChildName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public byte[] RowVersion { get; init; } = [];
}
internal sealed class StrategicCataloguePageRow
{
    public Guid PublicId { get; init; }
    public string? Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public string? Symbol { get; init; }
}
public sealed record LinkStrategicPlanningRequest(Guid ParentPublicId, Guid ChildPublicId, string Reason, string? RowVersion = null);
public sealed record DisableStrategicPlanningRelationshipRequest(string Reason, string RowVersion);
public sealed record StrategicCatalogueItemDto(Guid PublicId, string? Code, string Name, int DisplayOrder, string? Symbol = null);
public sealed record StrategicCatalogueRelationshipDto(string RelationshipType, Guid ParentPublicId, Guid ChildPublicId);
public sealed record StrategicClassificationCatalogueDto(
    StrategicCatalogueItemDto[] NationalKpas,
    StrategicCatalogueItemDto[] MunicipalKpas,
    StrategicCatalogueItemDto[] BackToBasicsPillars,
    StrategicCatalogueItemDto[] StrategicGoals,
    StrategicCatalogueItemDto[] StrategicInterventions,
    StrategicCatalogueItemDto[] StrategicObjectives,
    StrategicCatalogueItemDto[] PerformanceObjectives,
    StrategicCatalogueItemDto[] BudgetSources,
    StrategicCatalogueItemDto[] BudgetTypes,
    StrategicCatalogueItemDto[] KpiTypes,
    StrategicCatalogueItemDto[] IndicatorTypes,
    StrategicCatalogueItemDto[] FunctionalAreas,
    StrategicCatalogueItemDto[] StandardClassifications,
    StrategicCatalogueItemDto[] KpiUnitsOfMeasure,
    StrategicCatalogueRelationshipDto[] Relationships);

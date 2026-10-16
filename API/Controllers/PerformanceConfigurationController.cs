using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/performance-configuration")]
[Authorize]
public sealed class PerformanceConfigurationController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("catalogue")]
    public async Task<ActionResult<ApiResponse<PerformanceConfigurationCatalogueDto>>> Catalogue()
    {
        var units = await context.OpmsUnitDefinitions.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Id)
            .Select(item => new OpmsUnitDefinitionDto(item.PublicId, item.Code, item.Name, item.InputControlType, item.ValueDataType,
                item.Symbol, item.DecimalPlaces, item.MinValue, item.MaxValue, item.SupportsAutoVariance,
                item.DefaultPerformanceDirection == null ? null : item.DefaultPerformanceDirection.PublicId,
                item.RequiresComponentUi, item.IsQualitative, (int)item.EngineUnitKind, item.IsActive))
            .ToArrayAsync();
        var directions = await context.PerformanceDirectionDefinitions.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Id)
            .Select(item => new PerformanceDirectionDefinitionDto(item.PublicId, item.Code, item.Name, item.Description, (int)item.EngineDirection, item.IsActive))
            .ToArrayAsync();
        var ratioMethods = await context.RatioComparisonMethods.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Id)
            .Select(item => new RatioComparisonMethodDefinitionDto(item.PublicId, item.Code, item.Name, item.Description, item.SupportsAutoCalculation, item.IsActive))
            .ToArrayAsync();
        return Ok(new ApiResponse<PerformanceConfigurationCatalogueDto>(true, new(units, directions, ratioMethods)));
    }
}

public sealed record PerformanceConfigurationCatalogueDto(OpmsUnitDefinitionDto[] OpmsUnits, PerformanceDirectionDefinitionDto[] PerformanceDirections,
    RatioComparisonMethodDefinitionDto[] RatioComparisonMethods);
public sealed record OpmsUnitDefinitionDto(Guid PublicId, string Code, string Name, string InputControlType, string ValueDataType,
    string? Symbol, int? DecimalPlaces, decimal? MinValue, decimal? MaxValue, bool SupportsAutoVariance,
    Guid? DefaultPerformanceDirectionPublicId, bool RequiresComponentUi, bool IsQualitative, int EngineUnitKind, bool IsActive);
public sealed record PerformanceDirectionDefinitionDto(Guid PublicId, string Code, string Name, string Description,
    int EngineDirection, bool IsActive);
public sealed record RatioComparisonMethodDefinitionDto(Guid PublicId, string Code, string Name, string Description,
    bool SupportsAutoCalculation, bool IsActive);

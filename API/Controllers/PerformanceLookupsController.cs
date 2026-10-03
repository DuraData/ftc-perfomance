using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/performance-lookups")]
[Authorize]
public sealed class PerformanceLookupsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IAccessControlService accessControl) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PerformanceLookupsDto>>> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId == null ? null : await userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized(new ApiResponse<PerformanceLookupsDto>(false, null, "User not found"));

        var opms = await accessControl.GetQueryScopeAsync(user, "OPMS_KPI.READ");
        var ipms = await accessControl.GetQueryScopeAsync(user, "IPMS_KPI.READ");
        if (!opms.PermissionGranted && !ipms.PermissionGranted)
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PerformanceLookupsDto>(false, null, "Performance lookup access requires KPI read permission."));

        var periods = await context.Periods.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.StartDate)
            .Select(item => new PeriodLookupDto(item.Id, item.Code, item.Name, item.StartDate, item.EndDate, item.FiscalYear)).ToArrayAsync();
        var goals = await context.StrategicGoals.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Code, item.Name)).ToArrayAsync();
        var objectives = await context.StrategicObjectives.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new StrategicObjectiveLookupDto(item.Id, item.Code, item.Name, item.StrategicGoalId)).ToArrayAsync();
        var budgetSources = await context.BudgetSources.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Code, item.Name)).ToArrayAsync();
        var budgetTypes = await context.BudgetTypes.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new LookupItemDto(item.Id, item.Code, item.Name)).ToArrayAsync();
        var unitsOfMeasure = await context.UnitOfMeasures.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new UnitOfMeasureLookupDto(item.Id, item.Code, item.Name, item.Symbol)).ToArrayAsync();

        return Ok(new ApiResponse<PerformanceLookupsDto>(true, new PerformanceLookupsDto(
            periods, goals, objectives, budgetSources, budgetTypes, unitsOfMeasure)));
    }
}

public sealed record LookupItemDto(int Id, string Code, string Name);
public sealed record PeriodLookupDto(int Id, string Code, string Name, DateTime StartDate, DateTime EndDate, string FiscalYear);
public sealed record StrategicObjectiveLookupDto(int Id, string Code, string Name, int StrategicGoalId);
public sealed record UnitOfMeasureLookupDto(int Id, string Code, string Name, string? Symbol);
public sealed record PerformanceLookupsDto(
    PeriodLookupDto[] Periods,
    LookupItemDto[] StrategicGoals,
    StrategicObjectiveLookupDto[] StrategicObjectives,
    LookupItemDto[] BudgetSources,
    LookupItemDto[] BudgetTypes,
    UnitOfMeasureLookupDto[] UnitsOfMeasure);

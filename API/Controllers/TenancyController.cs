using FTCERP.Host.API.Responses;
using FTCERP.Host.API.Requests;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/tenancy")]
[Authorize]
public sealed class TenancyController(ApplicationDbContext context, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("my-contexts")]
    public ActionResult<ApiResponse<TenantContextDto[]>> GetMyContexts() => StatusCode(StatusCodes.Status410Gone,
        new ApiResponse<TenantContextDto[]>(false, null, "This unbounded route is retired. Use /api/v1/tenancy/my-contexts/page."));

    [HttpGet("my-contexts/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<TenantContextDto>>>> GetMyContextsPage(
        [FromQuery] PagedQueryRequest request, [FromQuery] Guid? municipalityPublicId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse<PagedResponse<TenantContextDto>>(false, null, "User not found"));
        if (request.NormalizedSortBy is not ("createdat" or "effectivefrom" or "code" or "name"))
            return BadRequest(new ApiResponse<PagedResponse<TenantContextDto>>(false, null, "SortBy must be createdAt, effectiveFrom, code, or name."));
        var query = context.Municipalities.IgnoreQueryFilters().AsNoTracking().Where(item => item.IsActive);
        if (!tenantContext.IsSystem)
        {
            var now = DateTime.UtcNow;
            query = query.Where(municipality => context.SecurityUserRoleAssignments.AsNoTracking().Any(assignment =>
                assignment.UserId == userId && assignment.MunicipalityId == municipality.Id && assignment.IsActive
                && !assignment.RevokedAt.HasValue && assignment.EffectiveFrom <= now
                && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo > now)));
        }
        if (municipalityPublicId.HasValue) query = query.Where(item => item.PublicId == municipalityPublicId.Value);
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Code.Contains(request.NormalizedSearch) || item.Name.Contains(request.NormalizedSearch));
        var totalCount = await query.CountAsync();
        var ordered = (request.NormalizedSortBy, request.Descending) switch
        {
            ("code", false) => query.OrderBy(item => item.Code).ThenBy(item => item.Id),
            ("code", true) => query.OrderByDescending(item => item.Code).ThenByDescending(item => item.Id),
            ("name", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            ("effectivefrom", false) => query.OrderBy(item => item.EffectiveFrom).ThenBy(item => item.Id),
            ("effectivefrom", true) => query.OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id),
            _ => query.OrderBy(item => item.Name).ThenBy(item => item.Id)
        };
        var municipalities = await ordered.Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        var rows = municipalities.Select(item => new TenantContextDto(item.PublicId, item.Code, item.Name, tenantContext.MunicipalityId == item.Id));
        return Ok(new ApiResponse<PagedResponse<TenantContextDto>>(true,
            PagedResponse<TenantContextDto>.Create(rows, request.Page, request.PageSize, totalCount)));
    }
}

public sealed record TenantContextDto(Guid PublicId, string Code, string Name, bool IsCurrent);

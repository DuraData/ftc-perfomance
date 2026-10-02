using FTCERP.Host.API.Responses;
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
    public async Task<ActionResult<ApiResponse<TenantContextDto[]>>> GetMyContexts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse<TenantContextDto[]>(false, null, "User not found"));
        long[] municipalityIds;
        if (tenantContext.IsSystem)
            municipalityIds = await context.Municipalities.IgnoreQueryFilters().Where(item => item.IsActive).Select(item => item.Id).ToArrayAsync();
        else
            municipalityIds = await context.SecurityUserRoleAssignments.AsNoTracking().Where(item => item.UserId == userId && item.IsActive && !item.RevokedAt.HasValue && item.MunicipalityId.HasValue).Select(item => item.MunicipalityId!.Value).Distinct().ToArrayAsync();
        var municipalities = await context.Municipalities.IgnoreQueryFilters().Where(item => municipalityIds.Contains(item.Id) && item.IsActive).OrderBy(item => item.Name).ToArrayAsync();
        return Ok(new ApiResponse<TenantContextDto[]>(true, municipalities.Select(item => new TenantContextDto(item.Id, item.PublicId, item.Code, item.Name, tenantContext.MunicipalityId == item.Id)).ToArray()));
    }
}

public sealed record TenantContextDto(long Id, Guid PublicId, string Code, string Name, bool IsCurrent);

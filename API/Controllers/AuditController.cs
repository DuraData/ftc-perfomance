using FTCERP.Host.API.Responses;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("login-logs")]
    [Authorize(Policy = "Permission:Audit.LoginLogs.View")]
    public async Task<ActionResult<ApiResponse<LoginAuditLogResponse[]>>> GetLoginLogs([FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 1000);

        var logs = await _context.LoginAuditLogs.AsNoTracking()
            .OrderByDescending(l => l.LoggedAt)
            .Take(take)
            .Select(l => new LoginAuditLogResponse(l.Id, l.UserId, l.Email, l.IpAddress, l.UserAgent, l.Success, l.FailureReason, l.LoggedAt))
            .ToArrayAsync();

        return Ok(new ApiResponse<LoginAuditLogResponse[]>(true, logs));
    }

    [HttpGet("security-events")]
    [Authorize(Policy = "Permission:Audit.View")]
    public ActionResult<ApiResponse<object[]>> GetSecurityEvents()
    {
        return Ok(new ApiResponse<object[]>(true, Array.Empty<object>()));
    }

    [HttpGet("trails")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuditTrailEntryResponse[]>>> GetAuditTrails([FromQuery] string? entityName = null, [FromQuery] string? entityId = null, [FromQuery] int take = 500)
    {
        take = Math.Clamp(take, 1, 1000);
        var query = _context.AuditTrails.AsNoTracking().OrderByDescending(item => item.ChangedAt).AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(item => item.EntityName == entityName);
        }
        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(item => item.EntityId == entityId);
        }

        var rows = await query.Take(take).ToArrayAsync();
        return Ok(new ApiResponse<AuditTrailEntryResponse[]>(true, rows.Select(item => item.ToResponse()).ToArray()));
    }
}

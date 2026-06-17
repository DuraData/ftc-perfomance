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
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;

    public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAccessControlService accessControlService)
    {
        _context = context;
        _userManager = userManager;
        _accessControlService = accessControlService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<NotificationResponse[]>>> GetNotifications([FromQuery] bool includeAll = false)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<NotificationResponse[]>(false, null, "User not found"));

        var permissionCode = includeAll ? "Notifications.Manage" : "Notifications.View";
        var decision = await _accessControlService.CheckPermissionAsync(user, permissionCode);
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<NotificationResponse[]>(false, null, decision.Reason));

        var query = _context.Notifications.AsNoTracking().OrderByDescending(item => item.CreatedAt).AsQueryable();
        if (!includeAll)
        {
            query = query.Where(item => item.UserId == user.Id);
        }

        var items = await query.Take(500).ToArrayAsync();
        return Ok(new ApiResponse<NotificationResponse[]>(true, items.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpPatch("{id}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkRead(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));

        var notification = await _context.Notifications.FirstOrDefaultAsync(item => item.Id == id);
        if (notification == null) return NotFound(new ApiResponse<bool>(false, false, "Notification not found"));

        if (!string.Equals(notification.UserId, user.Id, StringComparison.OrdinalIgnoreCase))
        {
            var decision = await _accessControlService.CheckPermissionAsync(user, "Notifications.Manage");
            if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<bool>(true, true));
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }
}

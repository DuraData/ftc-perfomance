using FTCERP.Host.API.Requests;
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
[Route("api/v1/notifications")]
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

    [HttpGet("page")]
    public async Task<ActionResult<ApiResponse<NotificationPageResponse>>> GetNotificationsPage([FromQuery] PagedQueryRequest request, [FromQuery] bool includeAll = false)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<NotificationPageResponse>(false, null, "User not found"));
        if (!NotificationSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<NotificationPageResponse>(false, null, "SortBy must be createdAt, type, or title."));

        var permissionCode = includeAll ? "Notifications.Manage" : "Notifications.View";
        var decision = await _accessControlService.CheckPermissionAsync(user, permissionCode);
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<NotificationPageResponse>(false, null, decision.Reason));

        var permittedQuery = _context.Notifications.AsNoTracking().AsQueryable();
        if (!includeAll) permittedQuery = permittedQuery.Where(item => item.UserId == user.Id);
        var unreadCount = await permittedQuery.CountAsync(item => !item.IsRead);
        var query = permittedQuery;
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.Title.Contains(request.NormalizedSearch) || item.Message.Contains(request.NormalizedSearch) || (item.EntityName != null && item.EntityName.Contains(request.NormalizedSearch)));

        var totalCount = await query.CountAsync();
        var items = await ApplyNotificationOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize).ToArrayAsync();
        return Ok(new ApiResponse<NotificationPageResponse>(true,
            NotificationPageResponse.Create(items.Select(item => item.ToResponse()), request.Page, request.PageSize, totalCount, unreadCount)));
    }

    private static readonly HashSet<string> NotificationSortFields = ["createdat", "type", "title"];

    private static IOrderedQueryable<Notification> ApplyNotificationOrdering(IQueryable<Notification> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("type", false) => query.OrderBy(item => item.Type).ThenBy(item => item.Id),
            ("type", true) => query.OrderByDescending(item => item.Type).ThenBy(item => item.Id),
            ("title", false) => query.OrderBy(item => item.Title).ThenBy(item => item.Id),
            ("title", true) => query.OrderByDescending(item => item.Title).ThenBy(item => item.Id),
            (_, false) => query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
        };

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

using System.Security.Claims;
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
[Route("api/v1/notification-operations")]
[Authorize(Policy = "Permission:NOTIFICATION_DELIVERY.READ")]
public sealed class NotificationOperationsController(ApplicationDbContext context, ITenantContext tenantContext, IAccessControlService accessControl) : ControllerBase
{
    [HttpGet("pending")]
    public ActionResult<ApiResponse<NotificationOutboxItemDto[]>> GetPending() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<NotificationOutboxItemDto[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/notification-operations/pending/page."));

    [HttpGet("pending/page")]
    public async Task<ActionResult<ApiResponse<PagedResponse<NotificationOutboxItemDto>>>> GetPendingPage([FromQuery] PagedQueryRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<PagedResponse<NotificationOutboxItemDto>>(false, null, "Select a municipality context."));
        if (!PendingSortFields.Contains(request.NormalizedSortBy))
            return BadRequest(new ApiResponse<PagedResponse<NotificationOutboxItemDto>>(false, null, "SortBy must be createdAt, availableAt, attemptCount, or eventType."));
        var access = await GetMemberAccessAsync();
        if (access == null) return Unauthorized();

        var query = context.BusinessEventOutbox.AsNoTracking()
            .Where(item => item.ProcessedAt == null && item.EventType.StartsWith("Notification."));
        if (request.NormalizedSearch.Length > 0)
            query = query.Where(item => item.EventType.Contains(request.NormalizedSearch) || item.AggregateType.Contains(request.NormalizedSearch)
                || access.AggregateId && item.AggregateId.Contains(request.NormalizedSearch)
                || access.LastError && item.LastError != null && item.LastError.Contains(request.NormalizedSearch));

        var totalCount = await query.CountAsync();
        var rows = await ApplyPendingOrdering(query, request.NormalizedSortBy, request.Descending)
            .Skip(request.Offset).Take(request.PageSize)
            .Include(item => item.DeliveryAttempts)
            .AsSplitQuery()
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<NotificationOutboxItemDto>>(true,
            PagedResponse<NotificationOutboxItemDto>.Create(rows.Select(item => ToDto(item, access)), request.Page, request.PageSize, totalCount)));
    }

    private static readonly HashSet<string> PendingSortFields = ["createdat", "availableat", "attemptcount", "eventtype"];

    private static IOrderedQueryable<BusinessEventOutbox> ApplyPendingOrdering(IQueryable<BusinessEventOutbox> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("availableat", false) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenBy(item => item.AvailableAt).ThenBy(item => item.PublicId),
            ("availableat", true) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenByDescending(item => item.AvailableAt).ThenBy(item => item.PublicId),
            ("attemptcount", false) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenBy(item => item.AttemptCount).ThenBy(item => item.PublicId),
            ("attemptcount", true) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenByDescending(item => item.AttemptCount).ThenBy(item => item.PublicId),
            ("eventtype", false) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenBy(item => item.EventType).ThenBy(item => item.PublicId),
            ("eventtype", true) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenByDescending(item => item.EventType).ThenBy(item => item.PublicId),
            (_, false) => query.OrderByDescending(item => item.AttemptCount >= 10).ThenBy(item => item.OccurredAt).ThenBy(item => item.PublicId),
            _ => query.OrderByDescending(item => item.AttemptCount >= 10).ThenByDescending(item => item.OccurredAt).ThenBy(item => item.PublicId)
        };

    [HttpPost("{publicId:guid}/retry")]
    [Authorize(Policy = "Permission:NOTIFICATION_DELIVERY.RETRY")]
    public async Task<ActionResult<ApiResponse<NotificationOutboxItemDto>>> Retry(Guid publicId, RetryNotificationOutboxRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<NotificationOutboxItemDto>(false, null, "Select a municipality context."));
        if (request.Reason.Trim().Length is < 5 or > 500) return BadRequest(new ApiResponse<NotificationOutboxItemDto>(false, null, "A retry reason of 5-500 characters is required."));
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(new ApiResponse<NotificationOutboxItemDto>(false, null, "User not found."));
        var access = await GetMemberAccessAsync();
        if (access == null) return Unauthorized(new ApiResponse<NotificationOutboxItemDto>(false, null, "User not found."));
        var row = await context.BusinessEventOutbox.Include(item => item.DeliveryAttempts).SingleOrDefaultAsync(item => item.PublicId == publicId && item.ProcessedAt == null && item.EventType.StartsWith("Notification."));
        if (row == null) return NotFound(new ApiResponse<NotificationOutboxItemDto>(false, null, "Pending notification event not found."));
        try { context.Entry(row).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return BadRequest(new ApiResponse<NotificationOutboxItemDto>(false, null, "RowVersion is invalid.")); }

        var before = new { row.AttemptCount, row.AvailableAt, row.LastError };
        row.AttemptCount = 0;
        row.AvailableAt = DateTime.UtcNow;
        row.LastError = null;
        context.AuditTrails.Add(new AuditTrail
        {
            EntityName = nameof(BusinessEventOutbox), EntityId = row.PublicId.ToString(), Action = "RetryNotificationDelivery",
            OldValue = JsonSerializer.Serialize(before), NewValue = JsonSerializer.Serialize(new { row.AttemptCount, row.AvailableAt }),
            ChangedBy = actorId, ChangedAt = DateTime.UtcNow, Reason = request.Reason.Trim(), CorrelationId = HttpContext.TraceIdentifier,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString()
        });
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiResponse<NotificationOutboxItemDto>(false, null, "The delivery event changed since it was loaded. Refresh and try again.")); }
        return Ok(new ApiResponse<NotificationOutboxItemDto>(true, ToDto(row, access), "Delivery event queued for retry. Successful channel receipts remain idempotently preserved."));
    }

    private async Task<NotificationDeliveryMemberAccess?> GetMemberAccessAsync()
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return null;
        var actor = await context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == actorId);
        if (actor == null) return null;
        async Task<bool> Read(string member) => (await accessControl.CheckPermissionAsync(actor, $"NOTIFICATION_DELIVERY.{member}.READ",
            new AccessScopeContext(MunicipalityId: tenantContext.MunicipalityId))).Allowed;
        return new NotificationDeliveryMemberAccess(
            await Read("AggregateId"), await Read("LastError"), await Read("RecipientUserId"),
            await Read("ProviderReference"), await Read("Error"), await Read("ResponseDetail"));
    }

    private static NotificationOutboxItemDto ToDto(BusinessEventOutbox item, NotificationDeliveryMemberAccess access) => new(
        item.PublicId, item.EventType, item.AggregateType, access.AggregateId ? item.AggregateId : null, item.OccurredAt, item.AvailableAt, item.AttemptCount,
        access.LastError ? item.LastError : null, item.AttemptCount >= 10, Convert.ToBase64String(item.RowVersion),
        item.DeliveryAttempts.OrderBy(value => value.Channel).ThenBy(value => value.RecipientUserId).Select(value => new NotificationDeliveryAttemptDto(
            value.PublicId, access.RecipientUserId ? value.RecipientUserId : null, value.Channel, value.Status, value.AttemptCount, value.AttemptedAt, value.DeliveredAt,
            value.Provider, access.ProviderReference ? value.ProviderReference : null, access.Error ? value.Error : null,
            access.ResponseDetail ? value.ResponseDetail : null)).ToArray());

    private sealed record NotificationDeliveryMemberAccess(bool AggregateId, bool LastError, bool RecipientUserId,
        bool ProviderReference, bool Error, bool ResponseDetail);
}

public sealed record RetryNotificationOutboxRequest(string Reason, string RowVersion);
public sealed record NotificationDeliveryAttemptDto(Guid PublicId, string? RecipientUserId, string Channel, string Status, int AttemptCount, DateTime AttemptedAt, DateTime? DeliveredAt, string? Provider, string? ProviderReference, string? Error, string? ResponseDetail);
public sealed record NotificationOutboxItemDto(Guid PublicId, string EventType, string AggregateType, string? AggregateId, DateTime OccurredAt, DateTime AvailableAt, int AttemptCount, string? LastError, bool IsDeadLetter, string RowVersion, NotificationDeliveryAttemptDto[] Deliveries);

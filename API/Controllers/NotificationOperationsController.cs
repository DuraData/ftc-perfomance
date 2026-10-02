using System.Security.Claims;
using System.Text.Json;
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
[Authorize(Policy = "Permission:WORKFLOW.CONFIGURE")]
public sealed class NotificationOperationsController(ApplicationDbContext context, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<NotificationOutboxItemDto[]>>> GetPending()
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<NotificationOutboxItemDto[]>(false, null, "Select a municipality context."));
        var rows = await context.BusinessEventOutbox.AsNoTracking()
            .Include(item => item.DeliveryAttempts)
            .Where(item => item.ProcessedAt == null && item.EventType.StartsWith("Notification."))
            .OrderByDescending(item => item.AttemptCount >= 10)
            .ThenBy(item => item.AvailableAt)
            .Take(200)
            .ToArrayAsync();
        return Ok(new ApiResponse<NotificationOutboxItemDto[]>(true, rows.Select(ToDto).ToArray()));
    }

    [HttpPost("{publicId:guid}/retry")]
    public async Task<ActionResult<ApiResponse<NotificationOutboxItemDto>>> Retry(Guid publicId, RetryNotificationOutboxRequest request)
    {
        if (tenantContext.MunicipalityId is not > 0) return Conflict(new ApiResponse<NotificationOutboxItemDto>(false, null, "Select a municipality context."));
        if (request.Reason.Trim().Length is < 5 or > 500) return BadRequest(new ApiResponse<NotificationOutboxItemDto>(false, null, "A retry reason of 5-500 characters is required."));
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Unauthorized(new ApiResponse<NotificationOutboxItemDto>(false, null, "User not found."));
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
        return Ok(new ApiResponse<NotificationOutboxItemDto>(true, ToDto(row), "Delivery event queued for retry. Successful channel receipts remain idempotently preserved."));
    }

    private static NotificationOutboxItemDto ToDto(BusinessEventOutbox item) => new(
        item.PublicId, item.EventType, item.AggregateType, item.AggregateId, item.OccurredAt, item.AvailableAt, item.AttemptCount,
        item.LastError, item.AttemptCount >= 10, Convert.ToBase64String(item.RowVersion),
        item.DeliveryAttempts.OrderBy(value => value.Channel).ThenBy(value => value.RecipientUserId).Select(value => new NotificationDeliveryAttemptDto(
            value.PublicId, value.RecipientUserId, value.Channel, value.Status, value.AttemptCount, value.AttemptedAt, value.DeliveredAt,
            value.Provider, value.ProviderReference, value.Error, value.ResponseDetail)).ToArray());
}

public sealed record RetryNotificationOutboxRequest(string Reason, string RowVersion);
public sealed record NotificationDeliveryAttemptDto(Guid PublicId, string RecipientUserId, string Channel, string Status, int AttemptCount, DateTime AttemptedAt, DateTime? DeliveredAt, string? Provider, string? ProviderReference, string? Error, string? ResponseDetail);
public sealed record NotificationOutboxItemDto(Guid PublicId, string EventType, string AggregateType, string AggregateId, DateTime OccurredAt, DateTime AvailableAt, int AttemptCount, string? LastError, bool IsDeadLetter, string RowVersion, NotificationDeliveryAttemptDto[] Deliveries);

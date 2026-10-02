using System.Text.Json;
using System.Diagnostics;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using System.Security.Claims;

namespace FTCERP.Host.Infrastructure.Security;

public interface IWorkflowGovernanceService
{
    void QueueAuditTrail(string entityName, string entityId, string action, object? oldValue, object? newValue, string changedBy, string? ipAddress, string? reason = null);
    void QueueNotification(string userId, NotificationType type, string title, string message, string? entityName, string? entityId);
    void QueueWorkflowNotifications(IEnumerable<string> userIds, NotificationType type, string title, string message, string? entityName, string? entityId);
    Task WriteAuditTrailAsync(string entityName, string entityId, string action, object? oldValue, object? newValue, string changedBy, string? ipAddress, string? reason = null);
    Task CreateNotificationAsync(string userId, NotificationType type, string title, string message, string? entityName, string? entityId);
    Task CreateWorkflowNotificationsAsync(IEnumerable<string> userIds, NotificationType type, string title, string message, string? entityName, string? entityId);
}

public class WorkflowGovernanceService : IWorkflowGovernanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ITenantContext? _tenantContext;

    public WorkflowGovernanceService(ApplicationDbContext context, IHttpContextAccessor? httpContextAccessor = null, ITenantContext? tenantContext = null)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public async Task WriteAuditTrailAsync(string entityName, string entityId, string action, object? oldValue, object? newValue, string changedBy, string? ipAddress, string? reason = null)
    {
        QueueAuditTrail(entityName, entityId, action, oldValue, newValue, changedBy, ipAddress, reason);
        await _context.SaveChangesAsync();
    }

    public void QueueAuditTrail(string entityName, string entityId, string action, object? oldValue, object? newValue, string changedBy, string? ipAddress, string? reason = null)
    {
        var http = _httpContextAccessor?.HttpContext;
        _context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = _tenantContext?.MunicipalityId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValue = Serialize(oldValue),
            NewValue = Serialize(newValue),
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress ?? http?.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = http?.TraceIdentifier ?? Activity.Current?.TraceId.ToString(),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            UserAgent = http?.Request.Headers.UserAgent.ToString(),
            SessionId = http?.User.FindFirstValue("sid")
        });
    }

    public async Task CreateNotificationAsync(string userId, NotificationType type, string title, string message, string? entityName, string? entityId)
    {
        QueueNotification(userId, type, title, message, entityName, entityId);
        await _context.SaveChangesAsync();
    }

    public void QueueNotification(string userId, NotificationType type, string title, string message, string? entityName, string? entityId)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            EntityName = entityName,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        QueueOutbox(type, title, message, entityName, entityId, [userId]);
    }

    public async Task CreateWorkflowNotificationsAsync(IEnumerable<string> userIds, NotificationType type, string title, string message, string? entityName, string? entityId)
    {
        QueueWorkflowNotifications(userIds, type, title, message, entityName, entityId);
        await _context.SaveChangesAsync();
    }

    public void QueueWorkflowNotifications(IEnumerable<string> userIds, NotificationType type, string title, string message, string? entityName, string? entityId)
    {
        var distinctUserIds = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (distinctUserIds.Length == 0)
        {
            return;
        }

        foreach (var userId in distinctUserIds)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                EntityName = entityName,
                EntityId = entityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        QueueOutbox(type, title, message, entityName, entityId, distinctUserIds);
    }

    private void QueueOutbox(NotificationType type, string title, string message, string? entityName, string? entityId, string[] recipients)
    {
        _context.BusinessEventOutbox.Add(new BusinessEventOutbox
        {
            EventType = $"Notification.{type}",
            AggregateType = entityName ?? "Notification",
            AggregateId = entityId ?? Guid.NewGuid().ToString("N"),
            Payload = JsonSerializer.Serialize(new { type, title, message, entityName, entityId, recipients }),
            CorrelationId = Activity.Current?.TraceId.ToString(),
            OccurredAt = DateTime.UtcNow,
            AvailableAt = DateTime.UtcNow
        });
    }

    private static string? Serialize(object? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value);
    }
}

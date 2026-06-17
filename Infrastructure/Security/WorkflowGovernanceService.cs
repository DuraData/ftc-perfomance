using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;

namespace FTCERP.Host.Infrastructure.Security;

public interface IWorkflowGovernanceService
{
    Task WriteAuditTrailAsync(string entityName, string entityId, string action, object? oldValue, object? newValue, string changedBy, string? ipAddress);
    Task CreateNotificationAsync(string userId, NotificationType type, string title, string message, string? entityName, string? entityId);
    Task CreateWorkflowNotificationsAsync(IEnumerable<string> userIds, NotificationType type, string title, string message, string? entityName, string? entityId);
}

public class WorkflowGovernanceService : IWorkflowGovernanceService
{
    private readonly ApplicationDbContext _context;

    public WorkflowGovernanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task WriteAuditTrailAsync(string entityName, string entityId, string action, object? oldValue, object? newValue, string changedBy, string? ipAddress)
    {
        _context.AuditTrails.Add(new AuditTrail
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValue = Serialize(oldValue),
            NewValue = Serialize(newValue),
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        });
        await _context.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(string userId, NotificationType type, string title, string message, string? entityName, string? entityId)
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
        await _context.SaveChangesAsync();
    }

    public async Task CreateWorkflowNotificationsAsync(IEnumerable<string> userIds, NotificationType type, string title, string message, string? entityName, string? entityId)
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

        await _context.SaveChangesAsync();
    }

    private static string? Serialize(object? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value);
    }
}

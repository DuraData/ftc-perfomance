using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Security;

public sealed class NotificationOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<NotificationOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = Math.Clamp(configuration.GetValue("Outbox:PollSeconds", 10), 2, 300);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        do
        {
            try { await ProcessBatch(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Notification outbox batch failed"); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> ProcessBatch(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var senders = scope.ServiceProvider.GetServices<INotificationChannelSender>().ToDictionary(item => item.Channel, StringComparer.OrdinalIgnoreCase);
        var defaultChannels = (configuration["Notifications:Channels"] ?? "IN_APP").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(item => item.ToUpperInvariant()).Distinct().ToArray();
        if (defaultChannels.Length == 0) defaultChannels = ["IN_APP"];
        var now = DateTime.UtcNow;
        var rows = await context.BusinessEventOutbox.IgnoreQueryFilters()
            .Where(item => item.ProcessedAt == null && item.AvailableAt <= now && item.AttemptCount < 10 && item.EventType.StartsWith("Notification."))
            .OrderBy(item => item.OccurredAt)
            .Take(25)
            .ToArrayAsync(cancellationToken);
        foreach (var row in rows)
        {
            try
            {
                var payload = ReadPayload(row.Payload);
                var channels = payload.Channels.Length == 0 ? defaultChannels : payload.Channels;
                var users = await context.Users.IgnoreQueryFilters().Where(item => payload.Recipients.Contains(item.Id)).ToDictionaryAsync(item => item.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
                var preferences = await context.NotificationPreferences.IgnoreQueryFilters().Where(item => payload.Recipients.Contains(item.UserId)).ToDictionaryAsync(item => item.UserId, StringComparer.OrdinalIgnoreCase, cancellationToken);
                var failures = new List<string>();
                foreach (var recipient in payload.Recipients)
                {
                    foreach (var channel in channels)
                    {
                        var idempotencyKey = $"{row.PublicId:N}:{channel}:{recipient}";
                        var attempt = await context.NotificationDeliveryAttempts.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
                        if (attempt?.Status == "Delivered") continue;
                        if (attempt == null)
                        {
                            attempt = new NotificationDeliveryAttempt { MunicipalityId = row.MunicipalityId, BusinessEventOutboxId = row.Id, RecipientUserId = recipient, Channel = channel, Status = "Pending", IdempotencyKey = idempotencyKey };
                            context.NotificationDeliveryAttempts.Add(attempt);
                        }

                        NotificationChannelResult result;
                        if (channel == "IN_APP") result = new(true, "ApplicationDatabase", row.PublicId.ToString(), "In-app notification persisted transactionally with the outbox event.");
                        else if (!payload.Mandatory && preferences.TryGetValue(recipient, out var preference) && ((channel == "EMAIL" && !preference.EmailEnabled) || (channel == "SMS" && !preference.SmsEnabled)))
                            result = NotificationChannelResult.Suppressed($"Recipient disabled optional {channel} notifications.");
                        else if (!senders.TryGetValue(channel, out var sender)) result = NotificationChannelResult.Failed("Unconfigured", $"No sender is registered for channel '{channel}'.");
                        else
                        {
                            var address = users.TryGetValue(recipient, out var user) ? channel switch { "EMAIL" => user.Email, "SMS" => user.PhoneNumber, _ => null } : null;
                            result = string.IsNullOrWhiteSpace(address)
                                ? NotificationChannelResult.NotDeliverable(sender.GetType().Name, $"Recipient has no deliverable address for channel '{channel}'.")
                                : await sender.SendAsync(new NotificationChannelMessage(recipient, address, payload.Title, payload.Message, payload.EntityName, payload.EntityId, idempotencyKey, row.CorrelationId), cancellationToken);
                        }
                        attempt.AttemptCount++;
                        attempt.AttemptedAt = DateTime.UtcNow;
                        attempt.Status = result.Delivered ? "Delivered" : result.Retryable ? "Failed" : result.TerminalStatus ?? "NotDeliverable";
                        attempt.DeliveredAt = result.Delivered ? attempt.AttemptedAt : null;
                        attempt.Error = result.Delivered ? null : Limit(result.Detail);
                        attempt.Provider = result.Provider;
                        attempt.ProviderReference = result.ProviderReference;
                        attempt.ResponseDetail = Limit(result.Detail);
                        if (!result.Delivered && result.Retryable) failures.Add($"{channel}/{recipient}: {result.Detail}");
                    }
                }
                row.AttemptCount++;
                row.ProcessedAt = failures.Count == 0 ? DateTime.UtcNow : null;
                row.LastError = failures.Count == 0 ? null : Limit(string.Join(" | ", failures));
                if (failures.Count > 0) row.AvailableAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, row.AttemptCount)));
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                foreach (var pendingDelivery in context.ChangeTracker.Entries<NotificationDeliveryAttempt>().Where(entry => entry.State == EntityState.Added)) pendingDelivery.State = EntityState.Detached;
                row.AttemptCount++;
                row.LastError = exception.Message.Length > 2000 ? exception.Message[..2000] : exception.Message;
                row.AvailableAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, row.AttemptCount)));
                try { await context.SaveChangesAsync(cancellationToken); }
                catch (Exception saveException) { logger.LogError(saveException, "Could not persist outbox retry state for {OutboxId}", row.PublicId); }
                logger.LogWarning(exception, "Notification outbox event {OutboxId} will be retried", row.PublicId);
            }
        }
        return rows.Length;
    }

    private static NotificationPayload ReadPayload(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var recipients = root.TryGetProperty("recipients", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
        return new NotificationPayload(
            recipients,
            ReadString(root, "title") ?? "OPMS notification",
            ReadString(root, "message") ?? string.Empty,
            ReadString(root, "entityName"),
            ReadString(root, "entityId"),
            root.TryGetProperty("channels", out var channels) && channels.ValueKind == JsonValueKind.Array
                ? channels.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()?.ToUpperInvariant()).Where(item => item is "IN_APP" or "EMAIL" or "SMS").Select(item => item!).Distinct().ToArray()
                : [],
            !root.TryGetProperty("mandatory", out var mandatory) || mandatory.ValueKind != JsonValueKind.False);
    }

    private static string? ReadString(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string? Limit(string? value) => string.IsNullOrWhiteSpace(value) ? value : value.Length > 2000 ? value[..2000] : value;
    private sealed record NotificationPayload(string[] Recipients, string Title, string Message, string? EntityName, string? EntityId, string[] Channels, bool Mandatory);
}

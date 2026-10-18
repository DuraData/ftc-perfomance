using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Host.Infrastructure.Observability;

public sealed record OperationalAlertDeliveryConfiguration(
    bool MonitorEnabled,
    int PollSeconds,
    bool RecipientsConfigured,
    string[] Channels);

public sealed record OperationalAlertHistoryItem(
    Guid PublicId,
    Guid IncidentPublicId,
    string Lifecycle,
    string HealthStatus,
    string[] CheckNames,
    DateTime OccurredAt,
    int TotalDeliveries,
    int DeliveredDeliveries,
    int FailedDeliveries,
    int TerminalDeliveries);

public sealed record OperationalAlertHistory(
    bool HistoryAvailable,
    OperationalAlertDeliveryConfiguration Configuration,
    OperationalAlertHistoryItem[] Items);

public interface IOperationalAlertStore
{
    Task ObserveAsync(HealthReport report, CancellationToken cancellationToken = default);
    Task<OperationalAlertHistory> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default);
}

public sealed class OperationalAlertStore(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OperationalAlertStore> logger) : IOperationalAlertStore, IDisposable
{
    private const string EventPrefix = "Notification.OperationalAlert";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;
    private ActiveIncident? _activeIncident;

    public async Task ObserveAsync(HealthReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!_initialized)
            {
                _activeIncident = await LoadActiveIncidentAsync(context, cancellationToken);
                _initialized = true;
            }

            var status = report.Status.ToString();
            var failedChecks = report.Entries
                .Where(item => item.Value.Status != HealthStatus.Healthy)
                .Select(item => item.Key)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var healthy = report.Status == HealthStatus.Healthy;
            if (healthy && _activeIncident == null) return;
            if (!healthy && _activeIncident != null
                && string.Equals(_activeIncident.HealthStatus, status, StringComparison.Ordinal)
                && _activeIncident.CheckNames.SequenceEqual(failedChecks, StringComparer.Ordinal)) return;

            var lifecycle = healthy ? "Recovered" : _activeIncident == null ? "Raised" : "Changed";
            var incidentPublicId = _activeIncident?.PublicId ?? Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;
            var recipientPublicIds = ParseGuidList(configuration["Observability:Alerts:RecipientUserPublicIds"]);
            var recipientUserIds = recipientPublicIds.Length == 0
                ? []
                : await context.Users.IgnoreQueryFilters().AsNoTracking()
                    .Where(item => item.IsActive && recipientPublicIds.Contains(item.PublicId))
                    .Select(item => item.Id)
                    .ToArrayAsync(cancellationToken);
            if (recipientPublicIds.Length != recipientUserIds.Length)
                logger.LogWarning("One or more configured operational-alert recipients are missing or inactive");

            var channels = GetChannels();
            var title = lifecycle == "Recovered" ? "OPMS operational health recovered" : $"OPMS operational health {status.ToLowerInvariant()}";
            var message = lifecycle == "Recovered"
                ? "All monitored OPMS readiness checks have recovered."
                : $"OPMS readiness is {status.ToLowerInvariant()}. Affected checks: {string.Join(", ", failedChecks)}.";
            var payload = JsonSerializer.Serialize(new
            {
                recipients = recipientUserIds,
                title,
                message,
                entityName = "SystemHealth",
                entityId = incidentPublicId.ToString("N"),
                channels,
                mandatory = true,
                lifecycle,
                healthStatus = status,
                checkNames = failedChecks,
                incidentPublicId,
                generatedAt = occurredAt
            }, JsonOptions);

            context.BusinessEventOutbox.Add(new BusinessEventOutbox
            {
                EventType = $"{EventPrefix}{lifecycle}",
                AggregateType = "SystemHealth",
                AggregateId = incidentPublicId.ToString("N"),
                Payload = payload,
                CorrelationId = incidentPublicId.ToString("N"),
                OccurredAt = occurredAt,
                AvailableAt = occurredAt
            });
            await context.SaveChangesAsync(cancellationToken);

            _activeIncident = healthy ? null : new ActiveIncident(incidentPublicId, status, failedChecks);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationalAlertHistory> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        var deliveryConfiguration = GetDeliveryConfiguration();
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rows = await context.BusinessEventOutbox.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.EventType.StartsWith(EventPrefix))
                .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
                .Take(Math.Clamp(take, 1, 50))
                .Include(item => item.DeliveryAttempts)
                .ToArrayAsync(cancellationToken);
            var items = rows.Select(ToHistoryItem).Where(item => item != null).Select(item => item!).ToArray();
            return new OperationalAlertHistory(true, deliveryConfiguration, items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Operational-alert history is temporarily unavailable");
            return new OperationalAlertHistory(false, deliveryConfiguration, []);
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task<ActiveIncident?> LoadActiveIncidentAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var latest = await context.BusinessEventOutbox.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.EventType.StartsWith(EventPrefix))
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Select(item => item.Payload)
            .FirstOrDefaultAsync(cancellationToken);
        var alert = ParsePayload(latest);
        return alert == null || string.Equals(alert.Lifecycle, "Recovered", StringComparison.OrdinalIgnoreCase)
            ? null
            : new ActiveIncident(alert.IncidentPublicId, alert.HealthStatus, alert.CheckNames);
    }

    private OperationalAlertDeliveryConfiguration GetDeliveryConfiguration() => new(
        configuration.GetValue("Observability:Alerts:MonitorEnabled", false),
        Math.Clamp(configuration.GetValue("Observability:Alerts:PollSeconds", 60), 10, 3_600),
        ParseGuidList(configuration["Observability:Alerts:RecipientUserPublicIds"]).Length > 0,
        GetChannels());

    private string[] GetChannels()
    {
        var channels = (configuration["Observability:Alerts:Channels"] ?? "IN_APP")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.ToUpperInvariant())
            .Where(item => item is "IN_APP" or "EMAIL" or "SMS")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return channels.Length == 0 ? ["IN_APP"] : channels;
    }

    private static Guid[] ParseGuidList(string? value) => (value ?? string.Empty)
        .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(item => Guid.TryParse(item, out var publicId) ? publicId : Guid.Empty)
        .Where(item => item != Guid.Empty)
        .Distinct()
        .ToArray();

    private static OperationalAlertHistoryItem? ToHistoryItem(BusinessEventOutbox row)
    {
        var payload = ParsePayload(row.Payload);
        if (payload == null) return null;
        return new OperationalAlertHistoryItem(
            row.PublicId,
            payload.IncidentPublicId,
            payload.Lifecycle,
            payload.HealthStatus,
            payload.CheckNames,
            row.OccurredAt,
            row.DeliveryAttempts.Count,
            row.DeliveryAttempts.Count(item => item.Status == "Delivered"),
            row.DeliveryAttempts.Count(item => item.Status == "Failed"),
            row.DeliveryAttempts.Count(item => item.Status is "Suppressed" or "NotDeliverable"));
    }

    private static AlertPayload? ParsePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (!root.TryGetProperty("lifecycle", out var lifecycleElement)
                || !root.TryGetProperty("healthStatus", out var statusElement)
                || !root.TryGetProperty("incidentPublicId", out var incidentElement)
                || !Guid.TryParse(incidentElement.GetString(), out var incidentPublicId)) return null;
            var checkNames = root.TryGetProperty("checkNames", out var checksElement) && checksElement.ValueKind == JsonValueKind.Array
                ? checksElement.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).Order(StringComparer.Ordinal).ToArray()
                : [];
            return new AlertPayload(lifecycleElement.GetString() ?? string.Empty, statusElement.GetString() ?? string.Empty, checkNames, incidentPublicId);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ActiveIncident(Guid PublicId, string HealthStatus, string[] CheckNames);
    private sealed record AlertPayload(string Lifecycle, string HealthStatus, string[] CheckNames, Guid IncidentPublicId);
}

public sealed class OperationalAlertMonitor(
    HealthCheckService healthChecks,
    IOperationalAlertStore store,
    IConfiguration configuration,
    ILogger<OperationalAlertMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Observability:Alerts:MonitorEnabled", false))
        {
            logger.LogInformation("Operational alert monitoring is disabled");
            return;
        }

        var pollSeconds = Math.Clamp(configuration.GetValue("Observability:Alerts:PollSeconds", 60), 10, 3_600);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollSeconds));
        do
        {
            try
            {
                var report = await healthChecks.CheckHealthAsync(stoppingToken);
                await store.ObserveAsync(report, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Operational alert monitoring cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

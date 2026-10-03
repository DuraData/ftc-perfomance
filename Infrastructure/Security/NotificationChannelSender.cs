using System.Net.Http.Json;

namespace FTCERP.Host.Infrastructure.Security;

public sealed record NotificationChannelMessage(string RecipientUserId, string Address, string Title, string Message, string? EntityName, string? EntityId, string IdempotencyKey, string? CorrelationId);
public sealed record NotificationChannelResult(bool Delivered, string Provider, string? ProviderReference, string? Detail, bool Retryable = true, string? TerminalStatus = null)
{
    public static NotificationChannelResult Failed(string provider, string detail) => new(false, provider, null, detail);
    public static NotificationChannelResult NotDeliverable(string provider, string detail) => new(false, provider, null, detail, false, "NotDeliverable");
    public static NotificationChannelResult Suppressed(string detail) => new(false, "UserPreference", null, detail, false, "Suppressed");
}

public sealed class HttpSmsNotificationSender(HttpClient client, IConfiguration configuration, ILogger<HttpSmsNotificationSender> logger) : INotificationChannelSender
{
    public string Channel => "SMS";

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["Notifications:Sms:Endpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return NotificationChannelResult.Failed("Unconfigured", "SMS notification endpoint is not configured.");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(new { to = message.Address, text = message.Message, title = message.Title, message.EntityName, message.EntityId, message.CorrelationId })
            };
            request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey);
            var apiKey = configuration["Notifications:Sms:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.TryAddWithoutValidation("X-Notification-Api-Key", apiKey);
            using var response = await client.SendAsync(request, cancellationToken);
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) return NotificationChannelResult.Failed(uri.Host, $"Provider returned HTTP {(int)response.StatusCode}: {Limit(detail)}");
            var reference = response.Headers.TryGetValues("X-Message-Id", out var values) ? values.FirstOrDefault() : null;
            return new(true, uri.Host, reference, Limit(detail));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "SMS notification delivery failed for user {UserId}", message.RecipientUserId);
            return NotificationChannelResult.Failed("Http", "SMS notification provider could not be reached.");
        }
    }

    private static string? Limit(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length > 2000 ? value[..2000] : value;
}

public interface INotificationChannelSender
{
    string Channel { get; }
    Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default);
}

public sealed class HttpEmailNotificationSender(HttpClient client, IConfiguration configuration, ILogger<HttpEmailNotificationSender> logger) : INotificationChannelSender
{
    public string Channel => "EMAIL";

    public async Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["Notifications:Email:Endpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return NotificationChannelResult.Failed("Unconfigured", "Email notification endpoint is not configured.");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(new { to = message.Address, subject = message.Title, text = message.Message, message.EntityName, message.EntityId, message.CorrelationId })
            };
            request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey);
            var apiKey = configuration["Notifications:Email:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.TryAddWithoutValidation("X-Notification-Api-Key", apiKey);
            using var response = await client.SendAsync(request, cancellationToken);
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) return NotificationChannelResult.Failed(uri.Host, $"Provider returned HTTP {(int)response.StatusCode}: {Limit(detail)}");
            ProviderResponse? provider = null;
            try { provider = string.IsNullOrWhiteSpace(detail) ? null : System.Text.Json.JsonSerializer.Deserialize<ProviderResponse>(detail, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch (System.Text.Json.JsonException) { }
            var reference = provider?.Reference;
            if (string.IsNullOrWhiteSpace(reference) && response.Headers.TryGetValues("X-Message-Id", out var values)) reference = values.FirstOrDefault();
            return new(true, provider?.Provider ?? uri.Host, reference, Limit(provider?.Detail ?? detail));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Email notification delivery failed for user {UserId}", message.RecipientUserId);
            return NotificationChannelResult.Failed("Http", "Email notification provider could not be reached.");
        }
    }

    private static string? Limit(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length > 2000 ? value[..2000] : value;
    private sealed record ProviderResponse(string? Provider, string? Reference, string? Detail);
}

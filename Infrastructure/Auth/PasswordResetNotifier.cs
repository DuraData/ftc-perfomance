using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Host.Infrastructure.Auth;

public sealed record PasswordResetDeliveryResult(bool Delivered, string Provider);

public interface IPasswordResetNotifier
{
    Task<PasswordResetDeliveryResult> SendAsync(ApplicationUser user, string token, string correlationId, CancellationToken cancellationToken = default);
}

public sealed class PasswordResetNotifier(
    IEnumerable<INotificationChannelSender> senders,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<PasswordResetNotifier> logger) : IPasswordResetNotifier
{
    public async Task<PasswordResetDeliveryResult> SendAsync(ApplicationUser user, string token, string correlationId, CancellationToken cancellationToken = default)
    {
        var sender = senders.FirstOrDefault(item => string.Equals(item.Channel, "EMAIL", StringComparison.OrdinalIgnoreCase));
        var configuredBaseUrl = configuration["Authentication:PasswordReset:PublicBaseUrl"]?.Trim().TrimEnd('/');
        if (sender == null || string.IsNullOrWhiteSpace(user.Email) || !Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri))
            return new(false, "Unconfigured");
        if (!environment.IsDevelopment() && baseUri.Scheme != Uri.UriSchemeHttps)
            return new(false, "InvalidConfiguration");

        var lifetimeMinutes = Math.Clamp(configuration.GetValue("Authentication:PasswordReset:TokenLifetimeMinutes", 30), 5, 1440);
        // Keep reset material in the URL fragment so browsers never send it to the web server or access logs.
        var resetUrl = $"{configuredBaseUrl}/reset-password#email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
        var message = $"A password reset was requested for your OPMS account. Use this link within {lifetimeMinutes} minutes: {resetUrl}\n\nIf you did not request this change, ignore this message.";
        var idempotencyKey = $"password-reset:{user.Id}:{Guid.NewGuid():N}";
        var result = await sender.SendAsync(new NotificationChannelMessage(
            user.Id,
            user.Email,
            "Reset your OPMS password",
            message,
            "UserAuthentication",
            user.Id,
            idempotencyKey,
            correlationId), cancellationToken);
        if (!result.Delivered)
            logger.LogWarning("Password reset delivery failed for user {UserId} through provider {Provider}", user.Id, result.Provider);
        return new(result.Delivered, result.Provider);
    }
}

using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace FTCERP.Host.Infrastructure.Auth;

public enum PasswordBreachStatus
{
    Disabled,
    Safe,
    Compromised,
    Unavailable
}

public sealed record PasswordBreachResult(PasswordBreachStatus Status, int Occurrences = 0);

public interface ICompromisedPasswordLookup
{
    Task<PasswordBreachResult> CheckAsync(string password, CancellationToken cancellationToken = default);
}

public sealed class PwnedPasswordLookup(
    HttpClient client,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<PwnedPasswordLookup> logger) : ICompromisedPasswordLookup
{
    private const int MaximumResponseCharacters = 2_000_000;

    public async Task<PasswordBreachResult> CheckAsync(string password, CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Authentication:PasswordProtection:CompromisedCheckEnabled", false))
            return new(PasswordBreachStatus.Disabled);

        var endpoint = configuration["Authentication:PasswordProtection:Endpoint"]?.Trim().TrimEnd('/');
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)
            return new(PasswordBreachStatus.Unavailable);

        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var prefix = hash[..5];
        var suffix = hash[5..];
        try
        {
            var range = await GetRangeAsync(endpoint, prefix, cancellationToken);
            if (range == null) return new(PasswordBreachStatus.Unavailable);
            var minimum = Math.Clamp(configuration.GetValue("Authentication:PasswordProtection:MinimumBreachCount", 1), 1, int.MaxValue);
            return range.TryGetValue(suffix, out var occurrences) && occurrences >= minimum
                ? new(PasswordBreachStatus.Compromised, occurrences)
                : new(PasswordBreachStatus.Safe);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "The compromised-password range service is unavailable");
            return new(PasswordBreachStatus.Unavailable);
        }
    }

    private async Task<IReadOnlyDictionary<string, int>?> GetRangeAsync(string endpoint, string prefix, CancellationToken cancellationToken)
    {
        var cacheKey = $"pwned-password-range:{prefix}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, int>? cached)) return cached;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/{prefix}");
        request.Headers.TryAddWithoutValidation("Add-Padding", "true");
        request.Headers.UserAgent.ParseAdd("OPMS-Password-Screening/1.0");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaximumResponseCharacters) return null;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > MaximumResponseCharacters) return null;

        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':');
            if (separator != 35 || !int.TryParse(line[(separator + 1)..], out var count) || count <= 0) continue;
            var candidate = line[..separator];
            if (candidate.All(Uri.IsHexDigit)) values[candidate] = count;
        }

        var minutes = Math.Clamp(configuration.GetValue("Authentication:PasswordProtection:CacheMinutes", 60), 1, 1440);
        cache.Set(cacheKey, values, TimeSpan.FromMinutes(minutes));
        return values;
    }
}

public sealed class CompromisedPasswordValidator(
    ICompromisedPasswordLookup lookup,
    IConfiguration configuration) : IPasswordValidator<ApplicationUser>
{
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password1", "password123", "qwerty", "qwerty123", "123456", "12345678",
        "123456789", "admin", "administrator", "letmein", "welcome", "welcome1", "changeme",
        "passw0rd", "p@ssw0rd", "municipality", "opms", "performance"
    };

    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || CommonPasswords.Contains(password.Trim()))
            return Rejected("PasswordCompromised", "Choose a password that is not commonly used or known to be compromised.");

        var result = await lookup.CheckAsync(password);
        if (result.Status == PasswordBreachStatus.Compromised)
            return Rejected("PasswordCompromised", "This password appears in a known breach. Choose a different password.");
        if (result.Status == PasswordBreachStatus.Unavailable
            && configuration.GetValue("Authentication:PasswordProtection:FailClosed", false))
            return Rejected("PasswordScreeningUnavailable", "Password security screening is temporarily unavailable. Try again later.");
        return IdentityResult.Success;
    }

    private static IdentityResult Rejected(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}

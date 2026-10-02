using System.Text.RegularExpressions;

namespace FTCERP.Host.Infrastructure.Auth;

public static class EnterpriseProviderKinds
{
    public const string MicrosoftEntraId = "MICROSOFT_ENTRA_ID";
    public const string ActiveDirectory = "ACTIVE_DIRECTORY";
}

public sealed class EnterpriseAuthenticationOptions
{
    public const string SectionName = "EnterpriseAuthentication";
    public EnterpriseProviderRegistration[] Providers { get; set; } = [];
    public string PostLoginPath { get; set; } = "/login?enterprise=complete";
    public string FailurePath { get; set; } = "/login?enterprise=failed";
}

public sealed class EnterpriseProviderRegistration
{
    public string Code { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = [];
}

public interface IEnterpriseProviderRegistry
{
    IReadOnlyCollection<EnterpriseProviderRegistration> Providers { get; }
    bool TryGet(string code, out EnterpriseProviderRegistration provider);
}

public sealed class EnterpriseProviderRegistry : IEnterpriseProviderRegistry
{
    private static readonly Regex SafeCode = new("^[A-Z0-9][A-Z0-9_-]{1,39}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly IReadOnlyDictionary<string, EnterpriseProviderRegistration> _providers;
    private readonly IReadOnlyCollection<EnterpriseProviderRegistration> _providerList;

    public EnterpriseProviderRegistry(IEnumerable<EnterpriseProviderRegistration> providers)
    {
        var normalized = providers.Select(Validate).ToArray();
        if (normalized.Select(item => item.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
            throw new InvalidOperationException("Enterprise authentication provider codes must be unique.");
        if (normalized.Select(item => item.CallbackPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
            throw new InvalidOperationException("Enterprise authentication callback paths must be unique.");
        _providers = normalized.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);
        _providerList = normalized;
    }

    public IReadOnlyCollection<EnterpriseProviderRegistration> Providers => _providerList;
    public bool TryGet(string code, out EnterpriseProviderRegistration provider) => _providers.TryGetValue(code, out provider!);

    public static string Scheme(string providerCode) => $"Enterprise:{providerCode.ToUpperInvariant()}";

    private static EnterpriseProviderRegistration Validate(EnterpriseProviderRegistration provider)
    {
        provider.Code = provider.Code.Trim().ToUpperInvariant();
        provider.Kind = provider.Kind.Trim().ToUpperInvariant();
        provider.DisplayName = provider.DisplayName.Trim();
        provider.Authority = provider.Authority.Trim().TrimEnd('/');
        provider.ClientId = provider.ClientId.Trim();
        provider.CallbackPath = provider.CallbackPath.Trim();
        if (!SafeCode.IsMatch(provider.Code)) throw new InvalidOperationException("Enterprise provider Code must contain 2-40 uppercase letters, numbers, underscores, or hyphens.");
        if (provider.Kind is not EnterpriseProviderKinds.MicrosoftEntraId and not EnterpriseProviderKinds.ActiveDirectory)
            throw new InvalidOperationException($"Enterprise provider '{provider.Code}' has unsupported Kind '{provider.Kind}'.");
        if (!Uri.TryCreate(provider.Authority, UriKind.Absolute, out var authority) || authority.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"Enterprise provider '{provider.Code}' Authority must be an absolute HTTPS URI.");
        if (string.IsNullOrWhiteSpace(provider.ClientId) || string.IsNullOrWhiteSpace(provider.ClientSecret))
            throw new InvalidOperationException($"Enterprise provider '{provider.Code}' requires ClientId and ClientSecret from deployment secrets.");
        if (!provider.CallbackPath.StartsWith('/') || provider.CallbackPath.Length > 120 || provider.CallbackPath.Contains('?', StringComparison.Ordinal))
            throw new InvalidOperationException($"Enterprise provider '{provider.Code}' CallbackPath must be a local path without a query string.");
        if (string.IsNullOrWhiteSpace(provider.DisplayName)) provider.DisplayName = provider.Code;
        provider.Scopes = provider.Scopes.Select(scope => scope.Trim()).Where(scope => scope.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return provider;
    }
}

namespace FTCERP.Host.Infrastructure.Auth;

internal static class AuthenticationCodeNormalizer
{
    internal static string Authenticator(string? code) =>
        string.Concat((code ?? string.Empty).Where(character =>
            !char.IsWhiteSpace(character) && character != '-'));

    internal static string Recovery(string? code) =>
        string.Concat((code ?? string.Empty).Where(character =>
            !char.IsWhiteSpace(character)));
}

using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("authentication")]
[Route("api/v1/auth/enterprise")]
public sealed class EnterpriseAuthController(
    ApplicationDbContext context,
    IEnterpriseProviderRegistry providers,
    IEnterpriseAuthenticationService enterpriseAuthentication,
    IJwtService jwtService,
    IOptions<EnterpriseAuthenticationOptions> options,
    IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    public const string ExternalCookieScheme = "Enterprise.External";

    [HttpGet("options/{municipalityCode}")]
    public async Task<ActionResult<ApiResponse<EnterpriseSignInOptionsResponse>>> GetOptions(string municipalityCode, CancellationToken cancellationToken)
    {
        var municipality = await context.Municipalities.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == municipalityCode && item.IsActive, cancellationToken);
        if (municipality == null) return NotFound(new ApiResponse<EnterpriseSignInOptionsResponse>(false, null, "Municipality not found."));
        var configuration = await ActiveConfiguration(municipality.Id, cancellationToken);
        var localEnabled = configuration == null
            ? municipality.AuthenticationMode is AuthenticationMode.Local or AuthenticationMode.Hybrid
            : configuration.Mode is AuthenticationMode.Local or AuthenticationMode.Hybrid;
        EnterpriseProviderRegistration? provider = null;
        if (configuration?.ProviderRegistrationCode != null
            && providers.TryGet(configuration.ProviderRegistrationCode, out var registered)
            && EnterpriseAuthenticationService.ModeAllows(configuration.Mode, registered.Kind))
            provider = registered;
        var enterpriseProviders = provider == null
            ? []
            : new[] { new EnterpriseProviderResponse(provider.Code, provider.DisplayName, provider.Kind) };
        return Ok(new ApiResponse<EnterpriseSignInOptionsResponse>(true,
            new EnterpriseSignInOptionsResponse(municipality.Code, municipality.Name, localEnabled, enterpriseProviders)));
    }

    [HttpGet("challenge/{municipalityCode}/{providerCode}")]
    public async Task<IActionResult> ChallengeProvider(string municipalityCode, string providerCode, CancellationToken cancellationToken)
    {
        var municipality = await context.Municipalities.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == municipalityCode && item.IsActive, cancellationToken);
        if (municipality == null || !providers.TryGet(providerCode, out var provider)) return NotFound();
        var configuration = await ActiveConfiguration(municipality.Id, cancellationToken);
        if (configuration == null || !string.Equals(configuration.ProviderRegistrationCode, provider.Code, StringComparison.OrdinalIgnoreCase)
            || !EnterpriseAuthenticationService.ModeAllows(configuration.Mode, provider.Kind)) return NotFound();
        var properties = new AuthenticationProperties { RedirectUri = "/api/v1/auth/enterprise/complete" };
        properties.Items["municipalityId"] = municipality.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        properties.Items["providerCode"] = provider.Code;
        return Challenge(properties, EnterpriseProviderRegistry.Scheme(provider.Code));
    }

    [HttpGet("complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var ticket = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
        string? municipalityValue = null;
        string? providerValue = null;
        var hasMunicipality = ticket.Properties?.Items.TryGetValue("municipalityId", out municipalityValue) == true;
        var hasProvider = ticket.Properties?.Items.TryGetValue("providerCode", out providerValue) == true;
        if (!ticket.Succeeded || ticket.Principal == null || !hasMunicipality
            || !long.TryParse(municipalityValue, out var municipalityId) || !hasProvider || string.IsNullOrWhiteSpace(providerValue))
            return RedirectFailure();
        var providerCode = providerValue!;
        var resolved = await enterpriseAuthentication.ResolveAsync(municipalityId, providerCode, ticket.Principal, HttpContext, cancellationToken);
        await HttpContext.SignOutAsync(ExternalCookieScheme);
        if (!resolved.Succeeded || resolved.User == null) return RedirectFailure();
        var (accessToken, refreshToken, expiresAt) = await jwtService.GenerateTokensAsync(resolved.User,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        SetSessionCookies(accessToken, refreshToken, expiresAt);
        return LocalRedirect(options.Value.PostLoginPath);
    }

    private async Task<AuthenticationConfiguration?> ActiveConfiguration(long municipalityId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await context.AuthenticationConfigurations.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item => item.MunicipalityId == municipalityId && item.IsActive && item.EffectiveFrom <= now
                && (!item.EffectiveTo.HasValue || item.EffectiveTo > now), cancellationToken);
    }

    private IActionResult RedirectFailure() => LocalRedirect(options.Value.FailurePath);

    private void SetSessionCookies(string accessToken, string refreshToken, DateTime accessExpiresAt)
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        Response.Cookies.Append(AuthCookiePolicy.AccessCookieName, accessToken, AuthCookiePolicy.Create(environment, AuthCookiePolicy.AccessPath, accessExpiresAt - DateTime.UtcNow));
        Response.Cookies.Append(AuthCookiePolicy.RefreshCookieName, refreshToken, AuthCookiePolicy.Create(environment, AuthCookiePolicy.RefreshPath,
            TimeSpan.FromDays(Math.Clamp(jwtSettings.Value.RefreshTokenExpiryDays, 1, 90))));
    }
}

public sealed record EnterpriseProviderResponse(string Code, string DisplayName, string Kind);
public sealed record EnterpriseSignInOptionsResponse(string MunicipalityCode, string MunicipalityName, bool LocalEnabled, EnterpriseProviderResponse[] Providers);

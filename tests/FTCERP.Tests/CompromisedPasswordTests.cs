using System.Net;
using System.Security.Cryptography;
using System.Text;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Health;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace FTCERP.Tests;

public sealed class CompromisedPasswordTests
{
    [Fact]
    public async Task Range_lookup_sends_only_five_hash_characters_with_padding_and_caches_the_range()
    {
        const string password = "Unique-Test-Password9!";
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{hash[5..]}:42\r\n{new string('0', 35)}:0\r\n")
        });
        var lookup = new PwnedPasswordLookup(
            new HttpClient(handler),
            Configuration(enabled: true),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PwnedPasswordLookup>.Instance);

        var first = await lookup.CheckAsync(password);
        var second = await lookup.CheckAsync(password);

        Assert.Equal(PasswordBreachStatus.Compromised, first.Status);
        Assert.Equal(42, first.Occurrences);
        Assert.Equal(PasswordBreachStatus.Compromised, second.Status);
        Assert.Equal(1, handler.CallCount);
        Assert.EndsWith($"/range/{hash[..5]}", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("true", handler.Request.Headers.GetValues("Add-Padding").Single());
        Assert.Contains("OPMS-Password-Screening/1.0", handler.Request.Headers.UserAgent.ToString());
        Assert.DoesNotContain(password, handler.Request.RequestUri.ToString());
        Assert.DoesNotContain(hash, handler.Request.RequestUri.ToString());
    }

    [Fact]
    public async Task Validator_rejects_local_and_remote_compromised_passwords_and_can_fail_closed()
    {
        var lookup = new Mock<ICompromisedPasswordLookup>();
        var user = IdpTestFixture.CreateUser("password-policy-user");
        var validator = new CompromisedPasswordValidator(lookup.Object, Configuration(enabled: true, failClosed: true));

        var local = await validator.ValidateAsync(null!, user, "Password123");
        lookup.Setup(service => service.CheckAsync("Breach-Listed-Password9!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordBreachResult(PasswordBreachStatus.Compromised, 3));
        var remote = await validator.ValidateAsync(null!, user, "Breach-Listed-Password9!");
        lookup.Setup(service => service.CheckAsync("Unavailable-Password9!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordBreachResult(PasswordBreachStatus.Unavailable));
        var unavailable = await validator.ValidateAsync(null!, user, "Unavailable-Password9!");

        Assert.False(local.Succeeded);
        Assert.Contains(local.Errors, error => error.Code == "PasswordCompromised");
        Assert.False(remote.Succeeded);
        Assert.Contains(remote.Errors, error => error.Code == "PasswordCompromised");
        Assert.False(unavailable.Succeeded);
        Assert.Contains(unavailable.Errors, error => error.Code == "PasswordScreeningUnavailable");
        lookup.Verify(service => service.CheckAsync("Password123", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Disabled_external_screening_still_enforces_the_local_denylist()
    {
        var lookup = new Mock<ICompromisedPasswordLookup>();
        lookup.Setup(service => service.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordBreachResult(PasswordBreachStatus.Disabled));
        var validator = new CompromisedPasswordValidator(lookup.Object, Configuration(enabled: false));

        Assert.False((await validator.ValidateAsync(null!, IdpTestFixture.CreateUser(), "Welcome1")).Succeeded);
        Assert.True((await validator.ValidateAsync(null!, IdpTestFixture.CreateUser(), "Locally-Unique-Password9!")).Succeeded);
    }

    [Fact]
    public async Task Readiness_fails_when_fail_closed_screening_cannot_reach_the_range_service()
    {
        var lookup = new Mock<ICompromisedPasswordLookup>();
        lookup.Setup(service => service.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordBreachResult(PasswordBreachStatus.Unavailable));
        var check = new CompromisedPasswordHealthCheck(Configuration(enabled: true, failClosed: true), lookup.Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private static IConfiguration Configuration(bool enabled, bool failClosed = false) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:PasswordProtection:CompromisedCheckEnabled"] = enabled.ToString(),
            ["Authentication:PasswordProtection:Endpoint"] = "https://api.pwnedpasswords.com/range",
            ["Authentication:PasswordProtection:MinimumBreachCount"] = "1",
            ["Authentication:PasswordProtection:FailClosed"] = failClosed.ToString(),
            ["Authentication:PasswordProtection:CacheMinutes"] = "60"
        }).Build();

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;
            return Task.FromResult(response);
        }
    }
}

using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace FTCERP.Tests;

public sealed class PasswordResetNotifierTests
{
    [Fact]
    public async Task Reset_link_is_encoded_and_delivery_metadata_does_not_contain_the_token()
    {
        var sender = new CapturingSender();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:PasswordReset:PublicBaseUrl"] = "https://opms.example.test",
            ["Authentication:PasswordReset:TokenLifetimeMinutes"] = "30"
        }).Build();
        var notifier = new PasswordResetNotifier([sender], configuration, new TestEnvironment(Environments.Production), NullLogger<PasswordResetNotifier>.Instance);
        var user = IdpTestFixture.CreateUser("reset-user");

        var result = await notifier.SendAsync(user, "sensitive+/= token", "correlation-1");

        Assert.True(result.Delivered);
        Assert.NotNull(sender.Message);
        Assert.Contains("https://opms.example.test/reset-password#", sender.Message!.Message);
        Assert.Contains("sensitive%2B%2F%3D%20token", sender.Message.Message);
        Assert.DoesNotContain("sensitive+/= token", sender.Message.IdempotencyKey);
        Assert.Equal("correlation-1", sender.Message.CorrelationId);
    }

    [Fact]
    public async Task Production_rejects_non_https_reset_origins()
    {
        var sender = new CapturingSender();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:PasswordReset:PublicBaseUrl"] = "http://opms.example.test"
        }).Build();
        var notifier = new PasswordResetNotifier([sender], configuration, new TestEnvironment(Environments.Production), NullLogger<PasswordResetNotifier>.Instance);

        var result = await notifier.SendAsync(IdpTestFixture.CreateUser("reset-user"), "token", "correlation-1");

        Assert.False(result.Delivered);
        Assert.Null(sender.Message);
    }

    private sealed class CapturingSender : INotificationChannelSender
    {
        public string Channel => "EMAIL";
        public NotificationChannelMessage? Message { get; private set; }
        public Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default)
        {
            Message = message;
            return Task.FromResult(new NotificationChannelResult(true, "Test", "message-1", null));
        }
    }

    private sealed class TestEnvironment(string name) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "FTCERP.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

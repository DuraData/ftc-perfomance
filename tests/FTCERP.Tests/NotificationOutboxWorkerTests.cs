using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FTCERP.Host.Infrastructure.Health;

namespace FTCERP.Tests;

public sealed class NotificationOutboxWorkerTests
{
    [Fact]
    public async Task Worker_RecordsIdempotentInAppDeliveryAndCompletesEvent()
    {
        var database = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        await using var provider = services.BuildServiceProvider();
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.BusinessEventOutbox.Add(new BusinessEventOutbox
            {
                MunicipalityId = 7,
                EventType = "Notification.Approval",
                AggregateType = "OpmsSubmission",
                AggregateId = "submission-1",
                Payload = "{\"recipients\":[\"user-1\",\"user-1\"]}"
            });
            await context.SaveChangesAsync();
        }
        var worker = new NotificationOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(), new ConfigurationBuilder().Build(), NullLogger<NotificationOutboxWorker>.Instance);

        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(1);
        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(0);

        await using var verifyScope = provider.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var delivery = await verify.NotificationDeliveryAttempts.IgnoreQueryFilters().SingleAsync();
        delivery.Status.Should().Be("Delivered");
        delivery.RecipientUserId.Should().Be("user-1");
        (await verify.BusinessEventOutbox.IgnoreQueryFilters().SingleAsync()).ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Worker_RetriesFailedEmailAndPreservesIdempotentProviderReceipt()
    {
        var database = Guid.NewGuid().ToString();
        var sender = new SequencedEmailSender();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        services.AddSingleton<INotificationChannelSender>(sender);
        await using var provider = services.BuildServiceProvider();
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(IdpTestFixture.CreateUser("user-1"));
            context.BusinessEventOutbox.Add(new BusinessEventOutbox
            {
                MunicipalityId = 7, EventType = "Notification.Approval", AggregateType = "OpmsSubmission", AggregateId = "submission-1",
                Payload = "{\"title\":\"Approved\",\"message\":\"Submission approved\",\"recipients\":[\"user-1\"]}"
            });
            await context.SaveChangesAsync();
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Notifications:Channels"] = "IN_APP,EMAIL" }).Build();
        var worker = new NotificationOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration, NullLogger<NotificationOutboxWorker>.Instance);

        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(1);
        await using (var retryScope = provider.CreateAsyncScope())
        {
            var context = retryScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var row = await context.BusinessEventOutbox.IgnoreQueryFilters().SingleAsync();
            row.ProcessedAt.Should().BeNull();
            row.AvailableAt = DateTime.UtcNow.AddSeconds(-1);
            await context.SaveChangesAsync();
        }
        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(1);

        await using var verifyScope = provider.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attempts = await verify.NotificationDeliveryAttempts.IgnoreQueryFilters().OrderBy(item => item.Channel).ToArrayAsync();
        attempts.Should().HaveCount(2);
        attempts.Single(item => item.Channel == "EMAIL").AttemptCount.Should().Be(2);
        attempts.Single(item => item.Channel == "EMAIL").ProviderReference.Should().Be("provider-message-1");
        attempts.Single(item => item.Channel == "IN_APP").AttemptCount.Should().Be(1);
        (await verify.BusinessEventOutbox.IgnoreQueryFilters().SingleAsync()).ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task NotificationHealth_FailsClosedWhenEnabledEmailEndpointIsMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Notifications:Channels"] = "IN_APP,EMAIL" }).Build();
        var sender = new SequencedEmailSender();

        var result = await new NotificationChannelHealthCheck(configuration, [sender]).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("endpoint");
    }

    [Fact]
    public async Task NotificationHealth_FailsClosedWhenEnabledSmsEndpointIsMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Notifications:Channels"] = "IN_APP,SMS" }).Build();
        var result = await new NotificationChannelHealthCheck(configuration, [new NeverCalledSmsSender()]).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("SMS").And.Contain("endpoint");
    }

    [Fact]
    public async Task Worker_MarksMissingSmsAddressNotDeliverableWithoutRetryingOutbox()
    {
        var database = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        services.AddSingleton<INotificationChannelSender>(new NeverCalledSmsSender());
        await using var provider = services.BuildServiceProvider();
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = IdpTestFixture.CreateUser("sms-user");
            user.PhoneNumber = null;
            context.Users.Add(user);
            context.BusinessEventOutbox.Add(new BusinessEventOutbox
            {
                MunicipalityId = 7, EventType = "Notification.DeadlineReminder", AggregateType = "ReportingWindow", AggregateId = "window-1",
                Payload = "{\"recipients\":[\"sms-user\"],\"channels\":[\"IN_APP\",\"SMS\"]}"
            });
            await context.SaveChangesAsync();
        }
        var worker = new NotificationOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(), new ConfigurationBuilder().Build(), NullLogger<NotificationOutboxWorker>.Instance);

        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(1);

        await using var verifyScope = provider.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attempts = await verify.NotificationDeliveryAttempts.IgnoreQueryFilters().OrderBy(x => x.Channel).ToArrayAsync();
        attempts.Single(x => x.Channel == "SMS").Status.Should().Be("NotDeliverable");
        (await verify.BusinessEventOutbox.IgnoreQueryFilters().SingleAsync()).ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Worker_SuppressesOptionalSmsButMandatorySmsBypassesPreference()
    {
        var database = Guid.NewGuid().ToString();
        var sender = new CountingSmsSender();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        services.AddSingleton<INotificationChannelSender>(sender);
        await using var provider = services.BuildServiceProvider();
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = IdpTestFixture.CreateUser("preference-user"); user.PhoneNumber = "+27110000001";
            context.Users.Add(user);
            context.NotificationPreferences.Add(new NotificationPreference { MunicipalityId = 7, UserId = user.Id, SmsEnabled = false });
            context.BusinessEventOutbox.AddRange(
                new BusinessEventOutbox { MunicipalityId = 7, EventType = "Notification.Optional", AggregateType = "Test", AggregateId = "optional", Payload = "{\"recipients\":[\"preference-user\"],\"channels\":[\"SMS\"],\"mandatory\":false}" },
                new BusinessEventOutbox { MunicipalityId = 7, EventType = "Notification.Mandatory", AggregateType = "Test", AggregateId = "mandatory", Payload = "{\"recipients\":[\"preference-user\"],\"channels\":[\"SMS\"],\"mandatory\":true}" });
            await context.SaveChangesAsync();
        }
        var worker = new NotificationOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(), new ConfigurationBuilder().Build(), NullLogger<NotificationOutboxWorker>.Instance);

        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(2);

        sender.Calls.Should().Be(1);
        await using var verifyScope = provider.CreateAsyncScope();
        var attempts = await verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().NotificationDeliveryAttempts.IgnoreQueryFilters().Include(x => x.BusinessEventOutbox).ToArrayAsync();
        attempts.Single(x => x.BusinessEventOutbox.AggregateId == "optional").Status.Should().Be("Suppressed");
        attempts.Single(x => x.BusinessEventOutbox.AggregateId == "mandatory").Status.Should().Be("Delivered");
    }

    private sealed class SequencedEmailSender : INotificationChannelSender
    {
        private int _calls;
        public string Channel => "EMAIL";
        public Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default)
        {
            _calls++;
            return Task.FromResult(_calls == 1
                ? NotificationChannelResult.Failed("TestProvider", "Temporary provider failure")
                : new NotificationChannelResult(true, "TestProvider", "provider-message-1", "Accepted"));
        }
    }

    private sealed class NeverCalledSmsSender : INotificationChannelSender
    {
        public string Channel => "SMS";
        public Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Sender must not be called without a phone number.");
    }

    private sealed class CountingSmsSender : INotificationChannelSender
    {
        public int Calls { get; private set; }
        public string Channel => "SMS";
        public Task<NotificationChannelResult> SendAsync(NotificationChannelMessage message, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new NotificationChannelResult(true, "TestSms", "sms-1", "Accepted"));
        }
    }
}

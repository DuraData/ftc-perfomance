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
}

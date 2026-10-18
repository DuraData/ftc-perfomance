using System.Text.Json;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Observability;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace FTCERP.Tests;

public sealed class OperationalAlertMonitorTests
{
    [Fact]
    public async Task Store_PersistsOnlyTransitions_RestoresIncidentAndHandsOffThroughNotificationOutbox()
    {
        var database = Guid.NewGuid().ToString();
        var recipient = IdpTestFixture.CreateUser("operations-alert-recipient");
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        await using var provider = services.BuildServiceProvider();
        await using (var seedScope = provider.CreateAsyncScope())
        {
            seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.Add(recipient);
            await seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:Alerts:MonitorEnabled"] = "true",
            ["Observability:Alerts:PollSeconds"] = "30",
            ["Observability:Alerts:RecipientUserPublicIds"] = recipient.PublicId.ToString(),
            ["Observability:Alerts:Channels"] = "IN_APP"
        }).Build();
        var store = CreateStore(provider, configuration);
        var degraded = Report(("database", HealthStatus.Healthy), ("outbox", HealthStatus.Degraded));
        var unhealthy = Report(("database", HealthStatus.Unhealthy), ("outbox", HealthStatus.Degraded));

        await store.ObserveAsync(degraded);
        await store.ObserveAsync(degraded);
        await store.ObserveAsync(unhealthy);

        var restartedStore = CreateStore(provider, configuration);
        await restartedStore.ObserveAsync(unhealthy);
        await restartedStore.ObserveAsync(Report(("database", HealthStatus.Healthy), ("outbox", HealthStatus.Healthy)));

        await using (var verifyScope = provider.CreateAsyncScope())
        {
            var rows = await verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().BusinessEventOutbox
                .IgnoreQueryFilters().OrderBy(item => item.OccurredAt).ThenBy(item => item.Id).ToArrayAsync();
            rows.Should().HaveCount(3);
            rows.Select(item => item.EventType).Should().Equal(
                "Notification.OperationalAlertRaised",
                "Notification.OperationalAlertChanged",
                "Notification.OperationalAlertRecovered");
            rows.Select(item => item.AggregateId).Distinct().Should().ContainSingle();
            rows.Should().OnlyContain(item => item.MunicipalityId == null && item.AggregateType == "SystemHealth");
            var combinedPayload = string.Join('\n', rows.Select(item => item.Payload));
            combinedPayload.Should().NotContain("Server=private").And.NotContain("Password=secret")
                .And.NotContain("private provider detail").And.NotContain("connectionString");
            using var firstPayload = JsonDocument.Parse(rows[0].Payload);
            firstPayload.RootElement.GetProperty("recipients")[0].GetString().Should().Be(recipient.Id);
            firstPayload.RootElement.GetProperty("checkNames")[0].GetString().Should().Be("outbox");
        }

        var worker = new NotificationOutboxWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            NullLogger<NotificationOutboxWorker>.Instance);
        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(3);

        var history = await restartedStore.GetHistoryAsync();
        history.HistoryAvailable.Should().BeTrue();
        history.Configuration.Should().BeEquivalentTo(new OperationalAlertDeliveryConfiguration(true, 30, true, ["IN_APP"]));
        history.Items.Should().HaveCount(3);
        history.Items.Select(item => item.Lifecycle).Should().Equal("Recovered", "Changed", "Raised");
        history.Items.Should().OnlyContain(item => item.TotalDeliveries == 1 && item.DeliveredDeliveries == 1);
        history.Items.Select(item => item.IncidentPublicId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task Store_PersistsHistoryWithoutConfiguredRecipients()
    {
        var database = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        await using var provider = services.BuildServiceProvider();
        await using (var initializationScope = provider.CreateAsyncScope())
        {
            var context = initializationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(IdpTestFixture.CreateUser("unconfigured-operations-user"));
            await context.SaveChangesAsync();
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:Alerts:MonitorEnabled"] = "true",
            ["Observability:Alerts:Channels"] = "unsupported"
        }).Build();
        var store = CreateStore(provider, configuration);

        var degraded = Report(("database", HealthStatus.Degraded));
        degraded.Status.Should().Be(HealthStatus.Degraded);
        await store.ObserveAsync(degraded);
        await using (var eventScope = provider.CreateAsyncScope())
        {
            var eventContext = eventScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await eventContext.BusinessEventOutbox.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        }
        var worker = new NotificationOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(), configuration, NullLogger<NotificationOutboxWorker>.Instance);
        (await worker.ProcessBatch(CancellationToken.None)).Should().Be(1);

        var history = await store.GetHistoryAsync(500);
        history.Configuration.RecipientsConfigured.Should().BeFalse();
        history.Configuration.Channels.Should().Equal("IN_APP");
        history.Items.Should().ContainSingle(item => item.TotalDeliveries == 0);
    }

    private static OperationalAlertStore CreateStore(IServiceProvider provider, IConfiguration configuration) => new(
        provider.GetRequiredService<IServiceScopeFactory>(),
        configuration,
        NullLogger<OperationalAlertStore>.Instance);

    private static HealthReport Report(params (string Name, HealthStatus Status)[] checks)
    {
        var entries = checks.ToDictionary(
            item => item.Name,
            item => new HealthReportEntry(
                item.Status,
                item.Status == HealthStatus.Healthy ? "ok" : "private provider detail Server=private;Password=secret",
                TimeSpan.FromMilliseconds(2),
                null,
                new Dictionary<string, object> { ["connectionString"] = "sensitive" }));
        return new HealthReport(entries, TimeSpan.FromMilliseconds(5));
    }
}

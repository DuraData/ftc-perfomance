using System.Net;
using FTCERP.Host.Infrastructure.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FTCERP.Tests;

public sealed class EvidenceStorageTests
{
    [Fact]
    public async Task HttpProvider_UsesOpaqueKeysAndSupportsWriteReadDeleteAndHealth()
    {
        var handler = new ObjectStoreHandler();
        var storage = new HttpEvidenceBlobStorage(new HttpClient(handler), Configuration());

        var write = await storage.StoreAsync(Path.Combine("tenant-7", "proof.pdf"), "evidence"u8.ToArray(), CancellationToken.None);
        var read = await storage.ReadAsync(Path.Combine("tenant-7", "proof.pdf"), CancellationToken.None);
        var delete = await storage.DisposeAsync(Path.Combine("tenant-7", "proof.pdf"), CancellationToken.None);
        var health = await new EvidenceStorageHealthCheck(storage).CheckHealthAsync(new HealthCheckContext());

        write.Succeeded.Should().BeTrue();
        read.Found.Should().BeTrue();
        read.Content.Should().Equal("evidence"u8.ToArray());
        delete.Succeeded.Should().BeTrue();
        health.Status.Should().Be(HealthStatus.Healthy);
        handler.Calls.Select(item => item.Method).Should().Equal("PUT", "GET", "DELETE", "GET");
        handler.Calls.Take(3).Should().OnlyContain(item => item.ObjectKey == "tenant-7/proof.pdf");
        handler.Calls.Should().OnlyContain(item => item.ApiKey == "test-secret");
    }

    [Fact]
    public async Task HttpProvider_FailsClosedWhenConfigurationIsMissing()
    {
        var handler = new ObjectStoreHandler();
        var storage = new HttpEvidenceBlobStorage(new HttpClient(handler), new ConfigurationBuilder().Build());

        (await storage.StoreAsync("proof.pdf", "evidence"u8.ToArray(), CancellationToken.None)).Succeeded.Should().BeFalse();
        (await storage.ReadAsync("proof.pdf", CancellationToken.None)).Available.Should().BeFalse();
        (await storage.DisposeAsync("proof.pdf", CancellationToken.None)).Succeeded.Should().BeFalse();
        (await new EvidenceStorageHealthCheck(storage).CheckHealthAsync(new HealthCheckContext())).Status.Should().Be(HealthStatus.Unhealthy);
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task HttpProvider_RejectsTraversalWithoutCallingProvider()
    {
        var handler = new ObjectStoreHandler();
        var storage = new HttpEvidenceBlobStorage(new HttpClient(handler), Configuration());

        (await storage.StoreAsync(Path.Combine("..", "outside.pdf"), "evidence"u8.ToArray(), CancellationToken.None)).Succeeded.Should().BeFalse();
        (await storage.ReadAsync(Path.Combine("..", "outside.pdf"), CancellationToken.None)).Found.Should().BeFalse();
        (await storage.DisposeAsync(Path.Combine("..", "outside.pdf"), CancellationToken.None)).Succeeded.Should().BeFalse();
        handler.Calls.Should().BeEmpty();
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["EvidenceStorage:Endpoint"] = "https://storage.test/objects",
        ["EvidenceStorage:HealthEndpoint"] = "https://storage.test/health",
        ["EvidenceStorage:ApiKey"] = "test-secret"
    }).Build();

    private sealed class ObjectStoreHandler : HttpMessageHandler
    {
        public List<Call> Calls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var objectKey = request.Headers.TryGetValues("X-Object-Key", out var keys) ? keys.Single() : null;
            var apiKey = request.Headers.TryGetValues("X-API-Key", out var apiKeys) ? apiKeys.Single() : null;
            Calls.Add(new(request.Method.Method, request.RequestUri!.AbsoluteUri, objectKey, apiKey));
            var response = request.Method == HttpMethod.Get && objectKey != null
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("evidence"u8.ToArray()) }
                : new HttpResponseMessage(request.Method == HttpMethod.Put ? HttpStatusCode.Created : HttpStatusCode.OK);
            return Task.FromResult(response);
        }
    }

    private sealed record Call(string Method, string Uri, string? ObjectKey, string? ApiKey);
}

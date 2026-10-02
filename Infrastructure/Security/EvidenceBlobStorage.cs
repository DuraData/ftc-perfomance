using System.Net;
using System.Net.Http.Headers;

namespace FTCERP.Host.Infrastructure.Security;

public sealed record EvidenceStorageOperationResult(bool Succeeded, string Detail);
public sealed record EvidenceStorageReadResult(bool Found, byte[] Content, string Detail, bool Available = true);
public sealed record EvidenceStorageHealthResult(bool Available, string Provider, string Detail);

public interface IEvidenceBlobStorage
{
    Task<EvidenceStorageOperationResult> StoreAsync(string storageKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
    Task<EvidenceStorageReadResult> ReadAsync(string storageKey, CancellationToken cancellationToken);
    Task<EvidenceStorageOperationResult> DisposeAsync(string storageKey, CancellationToken cancellationToken);
    Task<EvidenceStorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken);
}

public sealed class FileSystemEvidenceBlobStorage(IWebHostEnvironment environment, IConfiguration configuration) : IEvidenceBlobStorage
{
    public async Task<EvidenceStorageOperationResult> StoreAsync(string storageKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        if (path == null) return new(false, "Storage key failed canonical-root validation.");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken);
            return new(true, "Evidence content was written to private filesystem storage.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(false, Limit(exception.Message));
        }
    }

    public async Task<EvidenceStorageReadResult> ReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        if (path == null) return new(false, [], "Storage key failed canonical-root validation.");
        if (!File.Exists(path)) return new(false, [], "Evidence content was not found.");
        try { return new(true, await File.ReadAllBytesAsync(path, cancellationToken), "Evidence content was read from private filesystem storage."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return new(false, [], Limit(exception.Message), false); }
    }

    public Task<EvidenceStorageOperationResult> DisposeAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(storageKey);
        if (path == null) return Task.FromResult(new EvidenceStorageOperationResult(false, "Storage key failed canonical-root validation."));
        if (!File.Exists(path)) return Task.FromResult(new EvidenceStorageOperationResult(true, "Evidence content was already absent; disposal is idempotently complete."));
        try { File.Delete(path); return Task.FromResult(new EvidenceStorageOperationResult(true, "Evidence content was deleted from private filesystem storage.")); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return Task.FromResult(new EvidenceStorageOperationResult(false, Limit(exception.Message))); }
    }

    public async Task<EvidenceStorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var key = Path.Combine(".health", Guid.NewGuid().ToString("N") + ".probe");
        var write = await StoreAsync(key, "OPMS"u8.ToArray(), cancellationToken);
        if (!write.Succeeded) return new(false, "FileSystem", write.Detail);
        var read = await ReadAsync(key, cancellationToken);
        var delete = await DisposeAsync(key, cancellationToken);
        return read.Found && delete.Succeeded
            ? new(true, "FileSystem", "Private filesystem storage passed write/read/delete verification.")
            : new(false, "FileSystem", $"Storage probe failed: read={read.Detail}; delete={delete.Detail}");
    }

    private string? Resolve(string storageKey)
    {
        if (!EvidenceStorageKey.IsValid(storageKey)) return null;
        var configuredRoot = configuration["EvidenceStorage:LocalRoot"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(environment.ContentRootPath, "secure-files")
            : Path.IsPathRooted(configuredRoot) ? configuredRoot : Path.Combine(environment.ContentRootPath, configuredRoot);
        root = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine(root, storageKey));
        return candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    private static string Limit(string value) => value.Length <= 2000 ? value : value[..2000];
}

public sealed class HttpEvidenceBlobStorage(HttpClient client, IConfiguration configuration) : IEvidenceBlobStorage
{
    private const int MaximumReadBytes = 26 * 1024 * 1024;

    public async Task<EvidenceStorageOperationResult> StoreAsync(string storageKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        if (!TryCreateRequest(HttpMethod.Put, storageKey, false, out var request, out var error)) return new(false, error);
        using (request)
        {
            request.Content = new ByteArrayContent(content.ToArray());
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return response.IsSuccessStatusCode ? new(true, "Evidence content was written to the configured object store.") : new(false, await Failure(response, cancellationToken));
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                return new(false, "Object-storage write failed: " + Limit(exception.Message));
            }
        }
    }

    public async Task<EvidenceStorageReadResult> ReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (!TryCreateRequest(HttpMethod.Get, storageKey, false, out var request, out var error)) return new(false, [], error, false);
        using (request)
        {
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound) return new(false, [], "Evidence content was not found in object storage.");
                if (!response.IsSuccessStatusCode) return new(false, [], await Failure(response, cancellationToken), false);
                if (response.Content.Headers.ContentLength is > MaximumReadBytes) return new(false, [], "Object-store response exceeded the governed evidence-size limit.", false);
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var buffer = new MemoryStream();
                var chunk = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(chunk.AsMemory(), cancellationToken)) > 0)
                {
                    if (buffer.Length + read > MaximumReadBytes) return new(false, [], "Object-store response exceeded the governed evidence-size limit.", false);
                    await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
                }
                return new(true, buffer.ToArray(), "Evidence content was read from the configured object store.");
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                return new(false, [], "Object-storage read failed: " + Limit(exception.Message), false);
            }
        }
    }

    public async Task<EvidenceStorageOperationResult> DisposeAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (!TryCreateRequest(HttpMethod.Delete, storageKey, false, out var request, out var error)) return new(false, error);
        using (request)
        {
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound
                    ? new(true, response.StatusCode == HttpStatusCode.NotFound ? "Evidence content was already absent; disposal is idempotently complete." : "Evidence content was deleted from object storage.")
                    : new(false, await Failure(response, cancellationToken));
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                return new(false, "Object-storage disposal failed: " + Limit(exception.Message));
            }
        }
    }

    public async Task<EvidenceStorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        if (!TryCreateRequest(HttpMethod.Get, string.Empty, true, out var request, out var error)) return new(false, "HttpObjectStore", error);
        using (request)
        {
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return response.IsSuccessStatusCode
                    ? new(true, "HttpObjectStore", "Configured object-storage provider is reachable.")
                    : new(false, "HttpObjectStore", await Failure(response, cancellationToken));
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                return new(false, "HttpObjectStore", "Object-storage health request failed: " + exception.Message);
            }
        }
    }

    private bool TryCreateRequest(HttpMethod method, string storageKey, bool health, out HttpRequestMessage request, out string error)
    {
        request = null!; error = string.Empty;
        var endpointValue = health ? configuration["EvidenceStorage:HealthEndpoint"] : configuration["EvidenceStorage:Endpoint"];
        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint)) { error = health ? "Object-storage health endpoint is not configured." : "Object-storage endpoint is not configured."; return false; }
        if (!health && !EvidenceStorageKey.IsValid(storageKey)) { error = "Storage key failed validation."; return false; }
        var apiKey = configuration["EvidenceStorage:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) { error = "Object-storage API key is not configured."; return false; }
        request = new HttpRequestMessage(method, endpoint);
        request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        if (!health) request.Headers.TryAddWithoutValidation("X-Object-Key", storageKey.Replace('\\', '/'));
        return true;
    }

    private static async Task<string> Failure(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (detail.Length > 1000) detail = detail[..1000];
        return $"Object-storage provider returned {(int)response.StatusCode} {response.ReasonPhrase}: {detail}";
    }

    private static string Limit(string value) => value.Length <= 2000 ? value : value[..2000];
}

internal static class EvidenceStorageKey
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1000 || Path.IsPathRooted(value) || value.Contains(':') || value.Any(char.IsControl)) return false;
        return !value.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment is "." or "..");
    }
}

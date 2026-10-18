extern alias HostApp;

using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: WebApplicationFactoryContentRoot(
    "FTCERP.Host, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    "../../../../..",
    "FTCERP.Host.csproj",
    "0")]

return await OpenApiExporter.RunAsync(args);

internal static class OpenApiExporter
{
    public static async Task<int> RunAsync(string[] args)
    {
        var check = args.Contains("--check", StringComparer.OrdinalIgnoreCase);
        var outputIndex = Array.FindIndex(args, argument => argument.Equals("--output", StringComparison.OrdinalIgnoreCase));
        if (outputIndex < 0 || outputIndex == args.Length - 1)
        {
            Console.Error.WriteLine("Usage: FTCERP.OpenApiExporter --output <path> [--check]");
            return 2;
        }

        var outputPath = Path.GetFullPath(args[outputIndex + 1]);
        await using var factory = new OpenApiApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var source = await response.Content.ReadAsStringAsync();
        var canonical = Canonicalize(source);

        using var document = JsonDocument.Parse(canonical);
        var root = document.RootElement;
        ValidateContract(root);
        var pathCount = root.GetProperty("paths").EnumerateObject().Count();
        var operationCount = root.GetProperty("paths").EnumerateObject()
            .Sum(path => path.Value.EnumerateObject().Count(operation => IsHttpMethod(operation.Name)));
        var schemaCount = root.GetProperty("components").GetProperty("schemas").EnumerateObject().Count();

        if (check)
        {
            if (!File.Exists(outputPath))
            {
                Console.Error.WriteLine($"Version-controlled OpenAPI contract is missing: {outputPath}");
                return 1;
            }

            var committed = NormalizeNewLines(await File.ReadAllTextAsync(outputPath));
            if (!string.Equals(committed, canonical, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("The version-controlled OpenAPI contract is stale. Regenerate it with:");
                Console.Error.WriteLine("dotnet run --project tools/FTCERP.OpenApiExporter/FTCERP.OpenApiExporter.csproj -- --output docs/api/openapi-v1.json");
                return 1;
            }

            Console.WriteLine($"OpenAPI contract is current: {pathCount} paths, {operationCount} operations, {schemaCount} schemas.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, canonical);
        Console.WriteLine($"Wrote OpenAPI contract: {pathCount} paths, {operationCount} operations, {schemaCount} schemas.");
        return 0;
    }

    private static string Canonicalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        return NormalizeNewLines(JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
        {
            WriteIndented = true
        })) + "\n";
    }

    private static string NormalizeNewLines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static bool IsHttpMethod(string value) => value is "get" or "put" or "post" or "delete" or "options" or "head" or "patch" or "trace";

    private static void ValidateContract(JsonElement root)
    {
        if (!root.GetProperty("openapi").GetString()!.StartsWith("3.", StringComparison.Ordinal))
            throw new InvalidOperationException("The generated document is not OpenAPI 3.x.");

        var bearerDefined = root.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _);
        var bearerRequired = root.GetProperty("security").EnumerateArray()
            .Any(requirement => requirement.TryGetProperty("Bearer", out _));
        if (!bearerDefined || !bearerRequired)
            throw new InvalidOperationException("The generated document does not define and require the Bearer scheme by default.");

        var expectedAnonymousOperations = new HashSet<string>(StringComparer.Ordinal)
        {
            "POST /api/v1/auth/login",
            "POST /api/v1/auth/password/forgot",
            "POST /api/v1/auth/password/reset",
            "POST /api/v1/auth/refresh-token",
            "POST /api/v1/auth/enterprise/options",
            "GET /api/v1/auth/enterprise/options/{municipalityCode}",
            "GET /api/v1/auth/enterprise/challenge/{municipalityCode}/{providerCode}",
            "GET /api/v1/auth/enterprise/complete"
        };
        var actualAnonymousOperations = new HashSet<string>(StringComparer.Ordinal);
        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        var requiredFailureResponses = new[] { "400", "401", "403", "404", "409", "429", "500" };

        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            if (path.Name.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                && !path.Name.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unversioned API path remained in OpenAPI: {path.Name}");

            foreach (var method in path.Value.EnumerateObject().Where(item => IsHttpMethod(item.Name)))
            {
                var operation = method.Value;
                if (!operation.TryGetProperty("operationId", out var operationIdElement)
                    || string.IsNullOrWhiteSpace(operationIdElement.GetString()))
                    throw new InvalidOperationException($"OpenAPI operation has no stable operationId: {method.Name.ToUpperInvariant()} {path.Name}");
                if (!operationIds.Add(operationIdElement.GetString()!))
                    throw new InvalidOperationException($"Duplicate OpenAPI operationId: {operationIdElement.GetString()}");

                var responses = operation.GetProperty("responses");
                foreach (var status in requiredFailureResponses)
                    if (!responses.TryGetProperty(status, out _))
                        throw new InvalidOperationException($"OpenAPI operation is missing standard {status} response: {method.Name.ToUpperInvariant()} {path.Name}");

                if (operation.TryGetProperty("security", out var security) && security.ValueKind == JsonValueKind.Array
                    && security.GetArrayLength() == 0)
                    actualAnonymousOperations.Add($"{method.Name.ToUpperInvariant()} {path.Name}");
            }
        }

        if (!actualAnonymousOperations.SetEquals(expectedAnonymousOperations))
            throw new InvalidOperationException(
                $"OpenAPI anonymous-operation inventory differs from the reviewed allowlist. Actual: {string.Join(", ", actualAnonymousOperations.Order())}");
    }
}

internal sealed class OpenApiApplicationFactory : WebApplicationFactory<HostApp::Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"opms-openapi-{Guid.NewGuid():N}.db");
    private readonly string evidencePath = Path.Combine(Path.GetTempPath(), $"opms-openapi-evidence-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:EnsureCreated"] = "true",
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}",
            ["SeedData:Enabled"] = "false",
            ["EvidenceStorage:Provider"] = "FileSystem",
            ["EvidenceStorage:LocalRoot"] = evidencePath,
            ["JwtSettings:Secret"] = "openapi-exporter-local-test-signing-material"
        }));
        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(databasePath)) File.Delete(databasePath);
        if (Directory.Exists(evidencePath)) Directory.Delete(evidencePath, recursive: true);
    }
}

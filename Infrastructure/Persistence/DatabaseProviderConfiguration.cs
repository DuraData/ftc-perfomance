using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public static class DatabaseProviderConfiguration
{
    public const string SqlServer = "SqlServer";
    public const string Sqlite = "Sqlite";
    public const string DefaultConnectionName = "DefaultConnection";
    public const string SqlServerConnectionName = "SqlServerConnection";
    public const string SqliteConnectionName = "SqliteConnection";

    public static DbContextOptionsBuilder UseConfiguredDatabase(
        this DbContextOptionsBuilder options,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.Trim();
        if (string.IsNullOrWhiteSpace(provider)) provider = SqlServer;

        return provider.ToUpperInvariant() switch
        {
            "SQLSERVER" => options.UseSqlServer(GetRequiredConnectionString(
                configuration,
                SqlServer,
                SqlServerConnectionName)),
            "SQLITE" => options.UseSqlite(GetRequiredConnectionString(
                configuration,
                Sqlite,
                SqliteConnectionName)),
            _ => throw new InvalidOperationException(
                $"Unsupported database provider '{provider}'. Supported values are '{SqlServer}' and '{Sqlite}'.")
        };
    }

    private static string GetRequiredConnectionString(
        IConfiguration configuration,
        string provider,
        string providerSpecificName)
    {
        var fallback = configuration.GetConnectionString(DefaultConnectionName);
        if (provider == Sqlite && IsValidSqliteConnectionString(fallback)) return fallback!;

        var providerSpecific = configuration.GetConnectionString(providerSpecificName);
        if (!string.IsNullOrWhiteSpace(providerSpecific)) return providerSpecific;

        if (!string.IsNullOrWhiteSpace(fallback)) return fallback;

        throw new InvalidOperationException(
            $"ConnectionStrings:{providerSpecificName} or ConnectionStrings:{DefaultConnectionName} " +
            $"must be configured when Database:Provider is {provider}.");
    }

    private static bool IsValidSqliteConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        try
        {
            _ = new SqliteConnectionStringBuilder(connectionString);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

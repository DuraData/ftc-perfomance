using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public static class DatabaseProviderConfiguration
{
    public const string SqlServer = "SqlServer";
    public const string Sqlite = "Sqlite";

    public static DbContextOptionsBuilder UseConfiguredDatabase(
        this DbContextOptionsBuilder options,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.Trim();
        if (string.IsNullOrWhiteSpace(provider)) provider = SqlServer;

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        return provider.ToUpperInvariant() switch
        {
            "SQLSERVER" => options.UseSqlServer(connectionString),
            "SQLITE" when !string.IsNullOrWhiteSpace(connectionString) => options.UseSqlite(connectionString),
            "SQLITE" => throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured when Database:Provider is Sqlite."),
            _ => throw new InvalidOperationException(
                $"Unsupported database provider '{provider}'. Supported values are '{SqlServer}' and '{Sqlite}'.")
        };
    }
}

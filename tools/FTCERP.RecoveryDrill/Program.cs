using System.Text.Json;
using FTCERP.RecoveryDrill;

if (args.Contains("--validate-only", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(JsonSerializer.Serialize(SqliteRecoveryDrill.ValidationContract, SqliteRecoveryDrill.JsonOptions));
    return 0;
}

try
{
    var options = SqliteRecoveryDrillOptions.Parse(args);
    var evidence = await SqliteRecoveryDrill.RunAsync(options);
    Console.WriteLine($"Recovery drill evidence: {evidence.EvidencePath}");
    return evidence.Succeeded ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

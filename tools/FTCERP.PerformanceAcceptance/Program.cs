using System.Text.Json;

namespace FTCERP.PerformanceAcceptance.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--validate-only", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(PerformanceAcceptanceRunner.ValidationContract,
                PerformanceAcceptanceRunner.JsonOptions));
            return 0;
        }

        try
        {
            var options = PerformanceAcceptanceOptions.Parse(args);
            var evidence = await PerformanceAcceptanceRunner.RunAsync(options);
            Console.WriteLine(JsonSerializer.Serialize(evidence, PerformanceAcceptanceRunner.JsonOptions));
            return evidence.Succeeded ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}

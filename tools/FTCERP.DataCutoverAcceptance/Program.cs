using System.Text.Json;

namespace FTCERP.DataCutoverAcceptance;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--validate-only", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(DataCutoverAcceptanceRunner.ValidationContract,
                DataCutoverAcceptanceRunner.JsonOptions));
            return 0;
        }

        try
        {
            var options = DataCutoverAcceptanceOptions.Parse(args);
            var evidence = await DataCutoverAcceptanceRunner.RunAsync(options);
            Console.WriteLine(JsonSerializer.Serialize(evidence, DataCutoverAcceptanceRunner.JsonOptions));
            return evidence.Succeeded ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}

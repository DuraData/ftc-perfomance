using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FTCERP.Host.Infrastructure.OpenApi;

public sealed class ProblemDetailsOperationFilter : IOperationFilter
{
    private static readonly int[] StandardFailureStatuses = [400, 401, 403, 404, 409, 413, 415, 422, 429, 500];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
        operation.Responses ??= new OpenApiResponses();
        foreach (var status in StandardFailureStatuses)
        {
            var key = status.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (operation.Responses.ContainsKey(key)) continue;
            operation.Responses[key] = new OpenApiResponse
            {
                Description = ReasonPhrases.GetReasonPhrase(status),
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/problem+json"] = new() { Schema = schema }
                }
            };
        }
    }
}

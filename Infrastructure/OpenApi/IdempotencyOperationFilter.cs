using FTCERP.Host.API.Controllers;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FTCERP.Host.Infrastructure.OpenApi;

public sealed class IdempotencyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.HttpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
            || context.MethodInfo.DeclaringType == typeof(AuthController))
            return;

        operation.Parameters ??= [];
        if (operation.Parameters.Any(parameter => string.Equals(parameter.Name, IdempotencyMiddleware.HeaderName, StringComparison.OrdinalIgnoreCase)))
            return;
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotencyMiddleware.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "A caller-generated 8-128 character key. Reuse the same key only when retrying the identical request."
        });
    }
}

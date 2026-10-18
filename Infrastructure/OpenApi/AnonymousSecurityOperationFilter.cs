using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FTCERP.Host.Infrastructure.OpenApi;

/// <summary>
/// Overrides the document-level Bearer requirement for explicitly anonymous actions.
/// An empty operation security array is the OpenAPI 3.x representation of an
/// operation that does not inherit the document security requirement.
/// </summary>
public sealed class AnonymousSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [];
        }
    }
}

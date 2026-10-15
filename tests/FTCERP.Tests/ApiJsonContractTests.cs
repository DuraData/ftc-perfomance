using System.Text.Json;
using FTCERP.Host.API.Requests;
using FTCERP.Host.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FTCERP.Tests;

public sealed class ApiJsonContractTests
{
    [Fact]
    public void C88_enum_names_round_trip_through_the_public_api_json_contract()
    {
        using var factory = new TenantApplicationFactory("json-contract-user");
        var options = factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        var versionId = Guid.NewGuid();
        var json = $$"""
            {
              "catalogueVersionPublicId": "{{versionId}}",
              "kind": "Sector",
              "code": "SEC-VIS",
              "name": "Visual verification sector",
              "description": null,
              "parentItemPublicId": null,
              "displayOrder": 0,
              "isActive": true,
              "reason": "Verify the browser API contract",
              "rowVersion": null
            }
            """;

        var request = JsonSerializer.Deserialize<SaveC88CatalogueItemRequest>(json, options);

        Assert.NotNull(request);
        Assert.Equal(versionId, request.CatalogueVersionPublicId);
        Assert.Equal(C88CatalogueItemKind.Sector, request.Kind);
        Assert.Equal("\"Sector\"", JsonSerializer.Serialize(request.Kind, options));
    }
}

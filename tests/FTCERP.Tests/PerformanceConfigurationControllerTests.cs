using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Tests;

public sealed class PerformanceConfigurationControllerTests
{
    [Fact]
    public async Task Catalogue_exposes_the_complete_active_OPMS_unit_and_direction_masters()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var controller = new PerformanceConfigurationController(context);

        var action = await controller.Catalogue();
        var response = ((OkObjectResult)action.Result!).Value.Should().BeOfType<ApiResponse<PerformanceConfigurationCatalogueDto>>().Which;
        response.Data.Should().NotBeNull();
        var catalogue = response.Data!;

        catalogue.OpmsUnits.Select(item => item.Code).Should().BeEquivalentTo(
            "NUMBER", "PERCENT", "FINANCIAL", "DATE", "RATIO", "TIME", "AREA", "VOLUME", "YES_NO", "SCALE_1_3", "ZERO_NUMBER", "QUALITATIVE");
        catalogue.PerformanceDirections.Select(item => item.Code).Should().BeEquivalentTo(
            "TARGET_OR_HIGHER", "TARGET_OR_LOWER", "EXACT", "HIGHER_BETTER", "LOWER_BETTER", "ON_OR_BEFORE_DATE", "ON_OR_AFTER_DATE", "YES_IS_SUCCESS", "NO_IS_SUCCESS", "MANUAL");

        var date = catalogue.OpmsUnits.Single(item => item.Code == "DATE");
        var onOrBefore = catalogue.PerformanceDirections.Single(item => item.Code == "ON_OR_BEFORE_DATE");
        date.InputControlType.Should().Be("DATE");
        date.DefaultPerformanceDirectionPublicId.Should().Be(onOrBefore.PublicId);
        onOrBefore.EngineDirection.Should().Be(PerformanceDirection.LowerIsBetter);
    }
}

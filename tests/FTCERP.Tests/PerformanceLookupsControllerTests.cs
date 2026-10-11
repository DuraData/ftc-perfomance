using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Tests;

public sealed class PerformanceLookupsControllerTests
{
    [Fact]
    public void Legacy_unbounded_private_key_catalogue_is_retired()
    {
        var result = new PerformanceLookupsController().Get();
        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status410Gone, response.StatusCode);
        Assert.Contains("bounded", Assert.IsType<ApiResponse<object>>(response.Value).Message, StringComparison.OrdinalIgnoreCase);
    }
}

using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/navigation")]
[Authorize]
public sealed class NavigationController(IAccessControlService accessControlService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("~/api/navigation/my-menu")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<object>> GetLegacyMyMenu() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>(false, null,
            "This unversioned navigation contract is retired. Use GET /api/v1/navigation/my-menu."));

    [HttpGet("my-menu")]
    public async Task<ActionResult<ApiResponse<MenuItemResponse[]>>> GetMyMenu()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId == null ? null : await userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized(new ApiResponse<MenuItemResponse[]>(false, null, "User not found"));
        var menu = await accessControlService.GetAuthorizedNavigationAsync(user);
        return Ok(new ApiResponse<MenuItemResponse[]>(true, menu));
    }
}

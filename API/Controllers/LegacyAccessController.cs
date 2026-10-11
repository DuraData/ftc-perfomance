using FTCERP.Host.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/access")]
[Authorize]
public sealed class LegacyAccessController : ControllerBase
{
    [HttpGet("my-permissions")]
    public ActionResult<ApiResponse<object>> GetMyPermissions() => Gone("my-permissions");

    [HttpPost("check")]
    public ActionResult<ApiResponse<object>> Check() => Gone("check");

    [HttpPost("simulate")]
    public ActionResult<ApiResponse<object>> Simulate() => Gone("simulate");

    [HttpGet("role-access-matrix/page")]
    public ActionResult<ApiResponse<object>> GetRoleAccessMatrixPage() => Gone("role-access-matrix/page");

    [HttpGet("role-access-matrix")]
    public ActionResult<ApiResponse<object>> GetRoleAccessMatrix() => Gone("role-access-matrix/page");

    [HttpGet("system-coverage-audit")]
    public ActionResult<ApiResponse<object>> GetSystemCoverageAudit() => Gone("system-coverage-audit");

    private ActionResult<ApiResponse<object>> Gone(string path) => StatusCode(
        StatusCodes.Status410Gone,
        new ApiResponse<object>(false, null, $"This unversioned access-governance route is retired. Use /api/v1/access/{path}."));
}

using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/access")]
[Authorize]
public class AccessController : ControllerBase
{
    private readonly IAccessControlService _accessControlService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessController(IAccessControlService accessControlService, UserManager<ApplicationUser> userManager)
    {
        _accessControlService = accessControlService;
        _userManager = userManager;
    }

    [HttpGet("my-permissions")]
    [Authorize(Policy = "Permission:Access.MyPermissions.View")]
    public ActionResult<ApiResponse<string[]>> GetMyPermissions()
    {
        var permissions = User.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        return Ok(new ApiResponse<string[]>(true, permissions));
    }

    [HttpPost("check")]
    public async Task<ActionResult<ApiResponse<bool>>> Check([FromBody] CheckPermissionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ApiResponse<bool>(false, false, "Invalid user context"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));
        }

        var result = await _accessControlService.CheckPermissionAsync(user, request.PermissionCode);
        return Ok(new ApiResponse<bool>(true, result.Allowed, result.Reason));
    }

    [HttpPost("simulate")]
    [Authorize(Policy = "Permission:Admin.Users.Manage")]
    public async Task<ActionResult<ApiResponse<AccessSimulationResponse>>> Simulate([FromBody] SimulateAccessRequest request)
    {
        ApplicationUser? subject = null;
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            subject = await _userManager.FindByIdAsync(request.UserId);
        }

        if (subject == null)
        {
            return NotFound(new ApiResponse<AccessSimulationResponse>(false, null, "Simulation user not found"));
        }

        var result = await _accessControlService.CheckPermissionAsync(
            subject,
            request.PermissionCode,
            new AccessScopeContext(
                request.DepartmentId,
                request.UnitId,
                request.TargetId,
                request.KpiId,
                request.ProjectId,
                request.TaskId));

        return Ok(new ApiResponse<AccessSimulationResponse>(
            true,
            new AccessSimulationResponse(result.Allowed, result.Reason, result.EffectivePermissions, result.MatchedScopes, result.MatchedAssignments)));
    }

    [HttpGet("role-access-matrix")]
    [Authorize(Policy = "Permission:RoleImplementationAudit.View")]
    public async Task<ActionResult<ApiResponse<RoleAccessMatrixResponse[]>>> GetRoleAccessMatrix()
    {
        var rows = await _accessControlService.BuildRoleAccessMatrixAsync();
        return Ok(new ApiResponse<RoleAccessMatrixResponse[]>(true, rows));
    }

    [HttpGet("system-coverage-audit")]
    [Authorize(Policy = "Permission:RoleImplementationAudit.View")]
    public async Task<ActionResult<ApiResponse<SystemCoverageAuditResponse[]>>> GetSystemCoverageAudit()
    {
        var rows = await _accessControlService.BuildSystemCoverageAuditAsync();
        return Ok(new ApiResponse<SystemCoverageAuditResponse[]>(true, rows));
    }
}

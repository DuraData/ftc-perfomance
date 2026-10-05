using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
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
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AccessController(IAccessControlService accessControlService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, ITenantContext tenantContext)
    {
        _accessControlService = accessControlService;
        _userManager = userManager;
        _context = context;
        _tenantContext = tenantContext;
    }

    [HttpGet("my-permissions")]
    [Authorize(Policy = "Permission:Access.MyPermissions.View")]
    public async Task<ActionResult<ApiResponse<string[]>>> GetMyPermissions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse<string[]>(false, null, "Invalid user context"));
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized(new ApiResponse<string[]>(false, null, "User not found"));
        var access = await _accessControlService.GetEffectiveAccessAsync(user);
        var permissions = access.EffectivePermissions.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToArray();

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
        if (_tenantContext.MunicipalityId is not > 0)
            return Conflict(new ApiResponse<AccessSimulationResponse>(false, null, "Select a municipality context before simulating access"));

        ApplicationUser? subject = null;
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            subject = await _userManager.FindByIdAsync(request.UserId);
        }

        if (subject == null || subject.MunicipalityId != _tenantContext.MunicipalityId)
        {
            return NotFound(new ApiResponse<AccessSimulationResponse>(false, null, "Simulation user not found"));
        }

        var organization = await PerformanceApiSupport.ResolveOrganizationScopeAsync(
            _context,
            _tenantContext.MunicipalityId,
            request.DepartmentId,
            request.UnitId,
            request.DepartmentPublicId,
            request.UnitPublicId);
        if (organization.Error != null)
            return BadRequest(new ApiResponse<AccessSimulationResponse>(false, null, organization.Error));

        var result = await _accessControlService.CheckPermissionAsync(
            subject,
            request.PermissionCode,
            new AccessScopeContext(
                organization.DepartmentId,
                organization.UnitId,
                request.OwnerUserId,
                request.DelegatorUserId,
                request.TargetId,
                request.KpiId,
                request.ProjectId,
                request.TaskId));

        return Ok(new ApiResponse<AccessSimulationResponse>(
            true,
            new AccessSimulationResponse(result.Allowed, result.Reason, result.EffectivePermissions, result.MatchedScopes, result.MatchedAssignments)));
    }

    [HttpGet("role-access-matrix/page")]
    [Authorize(Policy = "Permission:RoleImplementationAudit.View")]
    public async Task<ActionResult<ApiResponse<PagedResponse<RoleAccessMatrixResponse>>>> GetRoleAccessMatrixPage([FromQuery] PagedQueryRequest request)
    {
        if (request.NormalizedSortBy is not ("name" or "code" or "createdat"))
            return BadRequest(new ApiResponse<PagedResponse<RoleAccessMatrixResponse>>(false, null, "SortBy must be name, code, or createdAt."));
        var page = await _accessControlService.BuildRoleAccessMatrixPageAsync(request);
        return Ok(new ApiResponse<PagedResponse<RoleAccessMatrixResponse>>(true, page));
    }

    [HttpGet("role-access-matrix")]
    [Authorize(Policy = "Permission:RoleImplementationAudit.View")]
    public ActionResult<ApiResponse<RoleAccessMatrixResponse[]>> GetRoleAccessMatrix()
    {
        return StatusCode(StatusCodes.Status410Gone,
            new ApiResponse<RoleAccessMatrixResponse[]>(false, null, "This unbounded route is retired. Use the /role-access-matrix/page endpoint."));
    }

    [HttpGet("system-coverage-audit")]
    [Authorize(Policy = "Permission:RoleImplementationAudit.View")]
    public async Task<ActionResult<ApiResponse<SystemCoverageAuditResponse[]>>> GetSystemCoverageAudit()
    {
        var rows = await _accessControlService.BuildSystemCoverageAuditAsync();
        return Ok(new ApiResponse<SystemCoverageAuditResponse[]>(true, rows));
    }
}

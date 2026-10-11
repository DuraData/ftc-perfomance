using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/access")]
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

        var subject = await _context.Users.SingleOrDefaultAsync(item => item.PublicId == request.UserPublicId);

        if (subject == null || subject.MunicipalityId != _tenantContext.MunicipalityId)
        {
            return NotFound(new ApiResponse<AccessSimulationResponse>(false, null, "Simulation user not found"));
        }

        var organization = await PerformanceApiSupport.ResolveOrganizationScopeAsync(
            _context,
            _tenantContext.MunicipalityId,
            null,
            null,
            request.DepartmentPublicId,
            request.UnitPublicId);
        if (organization.Error != null)
            return BadRequest(new ApiResponse<AccessSimulationResponse>(false, null, organization.Error));

        var referencedUsers = new[] { request.OwnerUserPublicId, request.DelegatorUserPublicId }
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var userIdsByPublicId = referencedUsers.Length == 0
            ? new Dictionary<Guid, string>()
            : await _context.Users.AsNoTracking()
                .Where(item => item.MunicipalityId == _tenantContext.MunicipalityId && referencedUsers.Contains(item.PublicId))
                .ToDictionaryAsync(item => item.PublicId, item => item.Id);
        if (userIdsByPublicId.Count != referencedUsers.Length)
            return BadRequest(new ApiResponse<AccessSimulationResponse>(false, null, "Owner and delegator users must belong to the selected municipality."));

        var recordScope = await ResolveRecordScopeAsync(request);
        if (recordScope.Error != null)
            return BadRequest(new ApiResponse<AccessSimulationResponse>(false, null, recordScope.Error));

        var result = await _accessControlService.CheckPermissionAsync(
            subject,
            request.PermissionCode,
            new AccessScopeContext(
                organization.DepartmentId,
                organization.UnitId,
                request.OwnerUserPublicId.HasValue ? userIdsByPublicId[request.OwnerUserPublicId.Value] : null,
                request.DelegatorUserPublicId.HasValue ? userIdsByPublicId[request.DelegatorUserPublicId.Value] : null,
                recordScope.TargetId,
                recordScope.KpiId,
                recordScope.ProjectId,
                recordScope.TaskId,
                _tenantContext.MunicipalityId));

        return Ok(new ApiResponse<AccessSimulationResponse>(
            true,
            new AccessSimulationResponse(result.Allowed, result.Reason, result.EffectivePermissions, result.MatchedScopes, result.MatchedAssignments)));
    }

    private async Task<(string? TargetId, string? KpiId, string? ProjectId, string? TaskId, string? Error)> ResolveRecordScopeAsync(SimulateAccessRequest request)
    {
        var municipalityId = _tenantContext.MunicipalityId!.Value;
        string? targetId = null;
        if (request.TargetPublicId.HasValue)
        {
            var targetCandidates = await _context.OpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && item.PublicId == request.TargetPublicId.Value)
                .Select(item => item.Id)
                .Concat(_context.IpmsTargets.AsNoTracking()
                    .Where(item => item.MunicipalityId == municipalityId && item.PublicId == request.TargetPublicId.Value)
                    .Select(item => item.Id))
                .Take(2)
                .ToArrayAsync();
            if (targetCandidates.Length != 1)
                return (null, null, null, null, "Target public identifier was not found uniquely in the selected municipality.");
            targetId = targetCandidates[0];
        }

        string? kpiId = null;
        if (request.KpiPublicId.HasValue)
        {
            var performanceCandidates = await _context.OpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && item.PublicId == request.KpiPublicId.Value)
                .Select(item => item.Id)
                .Concat(_context.IpmsTargets.AsNoTracking()
                    .Where(item => item.MunicipalityId == municipalityId && item.PublicId == request.KpiPublicId.Value)
                    .Select(item => item.Id))
                .Take(2)
                .ToArrayAsync();
            var idpCandidates = await _context.IdpKpis.AsNoTracking()
                .Where(item => item.PublicId == request.KpiPublicId.Value
                    && item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            var candidates = performanceCandidates.Concat(idpCandidates.Select(item => item.ToString(CultureInfo.InvariantCulture))).Take(2).ToArray();
            if (candidates.Length != 1)
                return (null, null, null, null, "KPI public identifier was not found uniquely in the selected municipality.");
            kpiId = candidates[0];
        }

        string? projectId = null;
        if (request.ProjectPublicId.HasValue)
        {
            var internalProjectIds = await _context.IdpProjects.AsNoTracking()
                .Where(item => item.PublicId == request.ProjectPublicId.Value
                    && item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            if (internalProjectIds.Length != 1)
                return (null, null, null, null, "Project public identifier was not found uniquely in the selected municipality.");
            projectId = internalProjectIds[0].ToString(CultureInfo.InvariantCulture);
        }

        string? taskId = null;
        if (request.TaskPublicId.HasValue)
        {
            var internalTaskIds = await _context.IdpTaskAssignments.AsNoTracking()
                .Where(item => item.PublicId == request.TaskPublicId.Value && item.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            if (internalTaskIds.Length != 1)
                return (null, null, null, null, "Task public identifier was not found uniquely in the selected municipality.");
            taskId = internalTaskIds[0].ToString(CultureInfo.InvariantCulture);
        }

        return (targetId, kpiId, projectId, taskId, null);
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

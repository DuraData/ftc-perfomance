using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAccessControlService _accessControl;
    private readonly ITenantContext _tenantContext;

    public AuditController(ApplicationDbContext context, IAccessControlService accessControl, ITenantContext tenantContext)
    {
        _context = context;
        _accessControl = accessControl;
        _tenantContext = tenantContext;
    }

    [HttpGet("login-logs")]
    [Authorize(Policy = "Permission:LOGIN_AUDIT.READ")]
    public ActionResult<ApiResponse<LoginAuditLogResponse[]>> GetLoginLogs() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<LoginAuditLogResponse[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/audit/login-logs/page."));

    [HttpGet("/api/v1/audit/login-logs/page")]
    [Authorize(Policy = "Permission:LOGIN_AUDIT.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LoginAuditLogResponse>>>> GetLoginLogsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] bool failuresOnly = false)
    {
        var actor = await GetActorAsync();
        if (actor == null) return Unauthorized();

        if (request.NormalizedSortBy is not ("createdat" or "email" or "success"))
            return BadRequest(new ApiResponse<PagedResponse<LoginAuditLogResponse>>(false, null, "SortBy must be createdAt, email, or success."));

        var canReadUserId = await CanReadLoginMemberAsync(actor, "UserId");
        var canReadEmail = await CanReadLoginMemberAsync(actor, "Email");
        var canReadIpAddress = await CanReadLoginMemberAsync(actor, "IpAddress");
        var canReadUserAgent = await CanReadLoginMemberAsync(actor, "UserAgent");
        var canReadFailureReason = await CanReadLoginMemberAsync(actor, "FailureReason");
        if (request.NormalizedSortBy == "email" && !canReadEmail) return Forbid();

        var query = _context.LoginAuditLogs.AsNoTracking().AsQueryable();
        if (failuresOnly) query = query.Where(item => !item.Success);
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(item => canReadEmail && item.Email.Contains(search)
                || canReadUserId && item.UserId != null && item.UserId.Contains(search)
                || canReadIpAddress && item.IpAddress != null && item.IpAddress.Contains(search)
                || canReadFailureReason && item.FailureReason != null && item.FailureReason.Contains(search)
                || canReadUserAgent && item.UserAgent != null && item.UserAgent.Contains(search));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("email", false) => query.OrderBy(item => item.Email).ThenBy(item => item.Id),
            ("email", true) => query.OrderByDescending(item => item.Email).ThenByDescending(item => item.Id),
            ("success", false) => query.OrderBy(item => item.Success).ThenByDescending(item => item.LoggedAt).ThenByDescending(item => item.Id),
            ("success", true) => query.OrderByDescending(item => item.Success).ThenByDescending(item => item.LoggedAt).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.LoggedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.LoggedAt).ThenByDescending(item => item.Id)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(item => new LoginAuditLogResponse(item.PublicId,
                canReadUserId ? item.UserId : null,
                canReadEmail ? item.Email : null,
                canReadIpAddress ? item.IpAddress : null,
                canReadUserAgent ? item.UserAgent : null,
                item.Success,
                canReadFailureReason ? item.FailureReason : null,
                item.LoggedAt))
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<LoginAuditLogResponse>>(true,
            PagedResponse<LoginAuditLogResponse>.Create(rows, request.Page, request.PageSize, totalCount)));
    }

    private async Task<bool> CanReadLoginMemberAsync(ApplicationUser actor, string memberCode)
    {
        var decision = await _accessControl.CheckPermissionAsync(actor, $"LOGIN_AUDIT.{memberCode}.READ",
            new AccessScopeContext(MunicipalityId: _tenantContext.MunicipalityId));
        return decision.Allowed;
    }

    private async Task<bool> CanReadAuditTrailMemberAsync(ApplicationUser actor, string memberCode)
    {
        var decision = await _accessControl.CheckPermissionAsync(actor, $"AUDIT_TRAIL.{memberCode}.READ",
            new AccessScopeContext(MunicipalityId: _tenantContext.MunicipalityId));
        return decision.Allowed;
    }

    private Task<ApplicationUser?> GetActorAsync()
    {
        var actorId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(actorId)
            ? Task.FromResult<ApplicationUser?>(null)
            : _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == actorId);
    }

    [HttpGet("security-events")]
    [Authorize(Policy = "Permission:Audit.View")]
    public ActionResult<ApiResponse<object[]>> GetSecurityEvents() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<object[]>(false, null,
            "This placeholder route is retired. Use /api/v1/audit/trails/page for governed security and business audit events."));

    [HttpGet("trails")]
    [HttpGet("/api/v1/audit/trails")]
    [Authorize(Policy = "Permission:AUDIT_TRAIL.READ")]
    public ActionResult<ApiResponse<AuditTrailEntryResponse[]>> GetAuditTrails() =>
        StatusCode(StatusCodes.Status410Gone, new ApiResponse<AuditTrailEntryResponse[]>(false, null,
            "This fixed-limit route is retired. Use /api/v1/audit/trails/page."));

    [HttpGet("/api/v1/audit/trails/page")]
    [Authorize(Policy = "Permission:AUDIT_TRAIL.READ")]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditTrailEntryResponse>>>> GetAuditTrailsPage(
        [FromQuery] PagedQueryRequest request,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null)
    {
        var actor = await GetActorAsync();
        if (actor == null) return Unauthorized();
        if (request.NormalizedSortBy is not ("createdat" or "entityname" or "action" or "changedby"))
            return BadRequest(new ApiResponse<PagedResponse<AuditTrailEntryResponse>>(false, null, "SortBy must be createdAt, entityName, action, or changedBy."));

        var canReadEntityId = await CanReadAuditTrailMemberAsync(actor, "EntityId");
        var canReadOldValue = await CanReadAuditTrailMemberAsync(actor, "OldValue");
        var canReadNewValue = await CanReadAuditTrailMemberAsync(actor, "NewValue");
        var canReadChangedBy = await CanReadAuditTrailMemberAsync(actor, "ChangedBy");
        var canReadIpAddress = await CanReadAuditTrailMemberAsync(actor, "IpAddress");
        var canReadCorrelationId = await CanReadAuditTrailMemberAsync(actor, "CorrelationId");
        var canReadReason = await CanReadAuditTrailMemberAsync(actor, "Reason");
        var canReadUserAgent = await CanReadAuditTrailMemberAsync(actor, "UserAgent");
        var canReadSessionId = await CanReadAuditTrailMemberAsync(actor, "SessionId");
        if (request.NormalizedSortBy == "changedby" && !canReadChangedBy) return Forbid();

        var query = _context.AuditTrails.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(item => item.EntityName == entityName.Trim());
        if (!string.IsNullOrWhiteSpace(entityId))
        {
            if (!canReadEntityId) return Forbid();
            query = query.Where(item => item.EntityId == entityId.Trim());
        }
        if (request.NormalizedSearch.Length > 0)
        {
            var search = request.NormalizedSearch;
            query = query.Where(item => item.EntityName.Contains(search) || item.Action.Contains(search)
                || canReadEntityId && item.EntityId.Contains(search)
                || canReadChangedBy && item.ChangedBy.Contains(search)
                || canReadReason && item.Reason != null && item.Reason.Contains(search)
                || canReadCorrelationId && item.CorrelationId != null && item.CorrelationId.Contains(search));
        }

        var totalCount = await query.CountAsync();
        query = (request.NormalizedSortBy, request.Descending) switch
        {
            ("entityname", false) => query.OrderBy(item => item.EntityName).ThenBy(item => item.Id),
            ("entityname", true) => query.OrderByDescending(item => item.EntityName).ThenByDescending(item => item.Id),
            ("action", false) => query.OrderBy(item => item.Action).ThenBy(item => item.Id),
            ("action", true) => query.OrderByDescending(item => item.Action).ThenByDescending(item => item.Id),
            ("changedby", false) => query.OrderBy(item => item.ChangedBy).ThenBy(item => item.Id),
            ("changedby", true) => query.OrderByDescending(item => item.ChangedBy).ThenByDescending(item => item.Id),
            (_, false) => query.OrderBy(item => item.ChangedAt).ThenBy(item => item.Id),
            _ => query.OrderByDescending(item => item.ChangedAt).ThenByDescending(item => item.Id)
        };
        var rows = await query.Skip(request.Offset).Take(request.PageSize)
            .Select(item => new AuditTrailEntryResponse(item.PublicId, item.MunicipalityId, item.EntityName,
                canReadEntityId ? item.EntityId : null, item.Action,
                canReadOldValue ? item.OldValue : null,
                canReadNewValue ? item.NewValue : null,
                canReadChangedBy ? item.ChangedBy : null,
                item.ChangedAt,
                canReadIpAddress ? item.IpAddress : null,
                canReadCorrelationId ? item.CorrelationId : null,
                canReadReason ? item.Reason : null,
                canReadUserAgent ? item.UserAgent : null,
                canReadSessionId ? item.SessionId : null))
            .ToArrayAsync();
        return Ok(new ApiResponse<PagedResponse<AuditTrailEntryResponse>>(true,
            PagedResponse<AuditTrailEntryResponse>.Create(rows, request.Page, request.PageSize, totalCount)));
    }
}

using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/performance-consolidation")]
[Authorize]
public sealed class PerformanceConsolidationController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    IAccessControlService accessControl,
    UserManager<ApplicationUser> userManager,
    IWorkflowGovernanceService workflowGovernance) : ControllerBase
{
    [HttpGet("calculation-types")]
    public async Task<ActionResult<ApiResponse<PerformanceCalculationTypeDto[]>>> GetCalculationTypes()
    {
        var authorization = await Authorize("OPMS_KPI.READ");
        if (authorization.Error != null) return authorization.Error;
        var rows = await context.PerformanceCalculationTypes.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new PerformanceCalculationTypeDto(item.PublicId, item.Code, item.Name, item.Description, item.IsActive))
            .ToArrayAsync();
        return Ok(new ApiResponse<PerformanceCalculationTypeDto[]>(true, rows));
    }

    [HttpGet("policies")]
    public async Task<ActionResult<ApiResponse<ConsolidationPolicyDto[]>>> GetPolicies()
    {
        var authorization = await Authorize("OPMS_KPI.READ");
        if (authorization.Error != null) return authorization.Error;
        var policies = await context.MunicipalityConsolidationPolicies.AsNoTracking().ToDictionaryAsync(item => item.CalculationTypeId);
        var definitions = await context.PerformanceCalculationTypes.AsNoTracking().OrderBy(item => item.Id).ToArrayAsync();
        return Ok(new ApiResponse<ConsolidationPolicyDto[]>(true, definitions.Select(item => ToDto(item, policies.GetValueOrDefault(item.Id))).ToArray()));
    }

    [HttpPut("policies/{calculationTypePublicId:guid}")]
    public async Task<ActionResult<ApiResponse<ConsolidationPolicyDto>>> SavePolicy(Guid calculationTypePublicId, SaveConsolidationPolicyRequest request)
    {
        var authorization = await Authorize("OPMS_KPI.CONFIGURE_CONSOLIDATION");
        if (authorization.Error != null) return authorization.Error;
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Fail<ConsolidationPolicyDto>("A governance reason is required."));
        if (!Enum.IsDefined(request.MissingValuePolicy)) return BadRequest(Fail<ConsolidationPolicyDto>("Select a valid missing-value policy."));
        if (request.ConsolidationRule is not null && request.ConsolidationRule is not PerformanceCalculationType.Sum and not PerformanceCalculationType.Average and not PerformanceCalculationType.LatestValue)
            return BadRequest(Fail<ConsolidationPolicyDto>("Configured consolidation overrides are limited to SUM, AVERAGE or LATEST_VALUE."));

        var definition = await context.PerformanceCalculationTypes.SingleOrDefaultAsync(item => item.PublicId == calculationTypePublicId && item.IsActive);
        if (definition == null) return NotFound(Fail<ConsolidationPolicyDto>("Calculation type not found or inactive."));
        var entity = await context.MunicipalityConsolidationPolicies.SingleOrDefaultAsync(item => item.CalculationTypeId == definition.Id);
        ConsolidationPolicyDto? before = null;
        var now = DateTime.UtcNow;
        if (entity == null)
        {
            if (!string.IsNullOrWhiteSpace(request.RowVersion)) return Conflict(Fail<ConsolidationPolicyDto>("The policy no longer exists in the expected state. Refresh and try again."));
            entity = new MunicipalityConsolidationPolicy
            {
                MunicipalityId = tenantContext.MunicipalityId!.Value,
                CalculationTypeId = definition.Id,
                CreatedByUserId = authorization.User!.Id,
                CreatedAt = now
            };
            context.MunicipalityConsolidationPolicies.Add(entity);
        }
        else
        {
            before = ToDto(definition, entity);
            if (!TrySetVersion(entity, request.RowVersion)) return BadRequest(Fail<ConsolidationPolicyDto>("A valid RowVersion is required when changing an existing policy."));
            entity.ModifiedByUserId = authorization.User!.Id;
            entity.ModifiedAt = now;
        }

        entity.ConsolidationRule = request.ConsolidationRule;
        entity.MissingValuePolicy = request.MissingValuePolicy;
        entity.AllowDerivedTargetOverride = request.AllowDerivedTargetOverride;
        entity.DeriveAnnualTarget = request.DeriveAnnualTarget;
        entity.IsActive = request.IsActive;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(Fail<ConsolidationPolicyDto>("The consolidation policy changed since it was loaded. Refresh and try again.")); }
        catch (DbUpdateException) { return Conflict(Fail<ConsolidationPolicyDto>("The consolidation policy conflicts with another current policy.")); }

        var after = ToDto(definition, entity);
        await workflowGovernance.WriteAuditTrailAsync(
            nameof(MunicipalityConsolidationPolicy), entity.PublicId.ToString(), before == null ? "Create" : "Edit",
            before, new { Policy = after, Reason = request.Reason.Trim() }, authorization.User!.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<ConsolidationPolicyDto>(true, after));
    }

    private async Task<(ApplicationUser? User, ActionResult? Error)> Authorize(string permission)
    {
        if (tenantContext.MunicipalityId is not > 0)
            return (null, BadRequest(Fail<object>("A municipality context is required.")));
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? tenantContext.UserId;
        var user = string.IsNullOrWhiteSpace(userId) ? null : await userManager.FindByIdAsync(userId);
        if (user == null) return (null, Unauthorized(Fail<object>("User not found.")));
        var decision = await accessControl.CheckPermissionAsync(user, permission, new AccessScopeContext(MunicipalityId: tenantContext.MunicipalityId));
        return decision.Allowed ? (user, null) : (user, Forbid());
    }

    private bool TrySetVersion(MunicipalityConsolidationPolicy entity, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length == 0) return false;
            context.Entry(entity).Property(item => item.RowVersion).OriginalValue = bytes;
            return true;
        }
        catch (FormatException) { return false; }
    }

    private static ConsolidationPolicyDto ToDto(PerformanceCalculationTypeDefinition definition, MunicipalityConsolidationPolicy? policy) =>
        new(definition.PublicId, definition.Code, definition.Name, definition.Description, definition.IsActive,
            policy?.PublicId, policy?.ConsolidationRule, policy?.MissingValuePolicy ?? ConsolidationMissingValuePolicy.Block,
            policy?.AllowDerivedTargetOverride ?? false, policy?.DeriveAnnualTarget ?? false, policy?.IsActive ?? false,
            policy == null ? null : Convert.ToBase64String(policy.RowVersion));

    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
}

public sealed record PerformanceCalculationTypeDto(Guid PublicId, string Code, string Name, string Description, bool IsActive);
public sealed record ConsolidationPolicyDto(
    Guid CalculationTypePublicId, string CalculationTypeCode, string CalculationTypeName, string CalculationTypeDescription,
    bool CalculationTypeIsActive, Guid? PolicyPublicId, PerformanceCalculationType? ConsolidationRule,
    ConsolidationMissingValuePolicy MissingValuePolicy, bool AllowDerivedTargetOverride, bool DeriveAnnualTarget,
    bool IsActive, string? RowVersion);
public sealed record SaveConsolidationPolicyRequest(
    PerformanceCalculationType? ConsolidationRule, ConsolidationMissingValuePolicy MissingValuePolicy,
    bool AllowDerivedTargetOverride, bool DeriveAnnualTarget, bool IsActive, string Reason, string? RowVersion);

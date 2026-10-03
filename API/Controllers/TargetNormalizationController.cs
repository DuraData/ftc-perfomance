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
[Route("api/v1/target-normalization")]
[Authorize]
[Authorize(Policy = "Permission:OPMS_KPI.NORMALIZE_LEGACY")]
public sealed class TargetNormalizationController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    IPerformanceUnitEngine unitEngine,
    UserManager<ApplicationUser> userManager,
    IWorkflowGovernanceService workflowGovernance) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<TargetNormalizationPreviewDto>>>> Preview(
        [FromQuery] SubmissionKind targetKind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<TargetNormalizationPreviewDto>>();
        if (targetKind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return InvalidKind<PagedResponse<TargetNormalizationPreviewDto>>();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = targetKind == SubmissionKind.Opms
            ? await context.OpmsTargets.CountAsync(item => item.MunicipalityId == tenantContext.MunicipalityId)
            : await context.IpmsTargets.CountAsync(item => item.MunicipalityId == tenantContext.MunicipalityId);

        var items = targetKind == SubmissionKind.Opms
            ? await PreviewOpms(page, pageSize)
            : await PreviewIpms(page, pageSize);
        return Ok(new ApiResponse<PagedResponse<TargetNormalizationPreviewDto>>(true,
            PagedResponse<TargetNormalizationPreviewDto>.Create(items, page, pageSize, total)));
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<TargetNormalizationResultDto>>> Execute(TargetNormalizationRequest request)
    {
        if (!HasTenant()) return TenantRequired<TargetNormalizationResultDto>();
        if (request.TargetKind is not (SubmissionKind.Opms or SubmissionKind.Ipms)) return InvalidKind<TargetNormalizationResultDto>();
        if (request.TargetPublicIds is not { Length: > 0 and <= 100 })
            return BadRequest(Fail<TargetNormalizationResultDto>("Select between 1 and 100 target public IDs."));
        var ids = request.TargetPublicIds.Distinct().ToArray();
        if (ids.Length != request.TargetPublicIds.Length)
            return BadRequest(Fail<TargetNormalizationResultDto>("Target public IDs must be unique."));
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            return BadRequest(Fail<TargetNormalizationResultDto>("A reconciliation reason between 1 and 1000 characters is required."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<TargetNormalizationResultDto>("User not found."));

        var rows = new List<(string Id, Guid PublicId, string IndicatorNumber, TargetPeriodPlan Plan, string? OpmsId, string? IpmsId)>();
        if (request.TargetKind == SubmissionKind.Opms)
        {
            var targets = await context.OpmsTargets.Where(item => ids.Contains(item.PublicId) && item.MunicipalityId == tenantContext.MunicipalityId).ToArrayAsync();
            if (targets.Length != ids.Length) return NotFound(Fail<TargetNormalizationResultDto>("One or more OPMS targets were not found in the selected municipality."));
            foreach (var target in targets)
            {
                var validation = ValidateLegacyRevisionState(target.Q3RevisedTarget, target.Q4RevisedTarget, target.RevisedAnnualTarget, target.RevisedAnnualBudget, target.IndicatorNumber);
                if (validation != null) return Conflict(Fail<TargetNormalizationResultDto>(validation));
                var plan = await Plan(target);
                if (!plan.IsValid) return Conflict(Fail<TargetNormalizationResultDto>($"{target.IndicatorNumber}: {plan.Error}"));
                rows.Add((target.Id, target.PublicId, target.IndicatorNumber, plan, target.Id, null));
            }
        }
        else
        {
            var targets = await context.IpmsTargets.Where(item => ids.Contains(item.PublicId) && item.MunicipalityId == tenantContext.MunicipalityId).ToArrayAsync();
            if (targets.Length != ids.Length) return NotFound(Fail<TargetNormalizationResultDto>("One or more IPMS targets were not found in the selected municipality."));
            foreach (var target in targets)
            {
                var validation = ValidateLegacyRevisionState(target.Q3RevisedTarget, target.Q4RevisedTarget, target.RevisedAnnualTarget, target.RevisedAnnualBudget, target.IndicatorNumber);
                if (validation != null) return Conflict(Fail<TargetNormalizationResultDto>(validation));
                var plan = await Plan(target);
                if (!plan.IsValid) return Conflict(Fail<TargetNormalizationResultDto>($"{target.IndicatorNumber}: {plan.Error}"));
                rows.Add((target.Id, target.PublicId, target.IndicatorNumber, plan, null, target.Id));
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        var additions = new List<(string Id, Guid PublicId, string IndicatorNumber, int Count)>();
        foreach (var row in rows)
        {
            var reconciliation = await TargetPeriodCutover.ReconcileAsync(context, row.Plan, row.OpmsId, row.IpmsId);
            if (!reconciliation.IsValid)
            {
                await transaction.RollbackAsync();
                return Conflict(Fail<TargetNormalizationResultDto>($"{row.IndicatorNumber}: {reconciliation.Error}"));
            }
            TargetPeriodCutover.AddNewRows(context, new TargetPeriodPlan(reconciliation.MissingRows, null), tenantContext.MunicipalityId!.Value, user.Id, row.OpmsId, row.IpmsId);
            additions.Add((row.Id, row.PublicId, row.IndicatorNumber, reconciliation.MissingRows.Count));
        }
        await context.SaveChangesAsync();
        foreach (var addition in additions.Where(item => item.Count > 0))
            await workflowGovernance.WriteAuditTrailAsync(
                request.TargetKind == SubmissionKind.Opms ? "OpmsTarget" : "IpmsTarget",
                addition.Id,
                "NormalizeLegacyPeriodTargets",
                null,
                new { addition.PublicId, addition.IndicatorNumber, AddedPeriodTargets = addition.Count, Reason = reason },
                user.Id,
                PerformanceApiSupport.GetIpAddress(HttpContext));
        await transaction.CommitAsync();

        return Ok(new ApiResponse<TargetNormalizationResultDto>(true,
            new(ids.Length, additions.Sum(item => item.Count), additions.Count(item => item.Count == 0))));
    }

    private async Task<TargetNormalizationPreviewDto[]> PreviewOpms(int page, int pageSize)
    {
        var targets = await context.OpmsTargets.AsNoTracking().Where(item => item.MunicipalityId == tenantContext.MunicipalityId)
            .OrderBy(item => item.IndicatorNumber).ThenBy(item => item.PublicId).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
        var results = new List<TargetNormalizationPreviewDto>(targets.Length);
        foreach (var target in targets)
            results.Add(await Preview(target.PublicId, target.IndicatorNumber, target.TargetName,
                ValidateLegacyRevisionState(target.Q3RevisedTarget, target.Q4RevisedTarget, target.RevisedAnnualTarget, target.RevisedAnnualBudget, target.IndicatorNumber),
                await Plan(target), target.Id, null));
        return results.ToArray();
    }

    private async Task<TargetNormalizationPreviewDto[]> PreviewIpms(int page, int pageSize)
    {
        var targets = await context.IpmsTargets.AsNoTracking().Where(item => item.MunicipalityId == tenantContext.MunicipalityId)
            .OrderBy(item => item.IndicatorNumber).ThenBy(item => item.PublicId).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
        var results = new List<TargetNormalizationPreviewDto>(targets.Length);
        foreach (var target in targets)
            results.Add(await Preview(target.PublicId, target.IndicatorNumber, target.TargetName,
                ValidateLegacyRevisionState(target.Q3RevisedTarget, target.Q4RevisedTarget, target.RevisedAnnualTarget, target.RevisedAnnualBudget, target.IndicatorNumber),
                await Plan(target), null, target.Id));
        return results.ToArray();
    }

    private async Task<TargetNormalizationPreviewDto> Preview(Guid publicId, string indicatorNumber, string targetName, string? legacyError, TargetPeriodPlan plan, string? opmsId, string? ipmsId)
    {
        if (legacyError != null) return new(publicId, indicatorNumber, targetName, "Blocked", 0, legacyError);
        if (!plan.IsValid) return new(publicId, indicatorNumber, targetName, "Blocked", 0, plan.Error);
        var reconciliation = await TargetPeriodCutover.ReconcileAsync(context, plan, opmsId, ipmsId);
        if (!reconciliation.IsValid) return new(publicId, indicatorNumber, targetName, "Blocked", 0, reconciliation.Error);
        return new(publicId, indicatorNumber, targetName, reconciliation.MissingRows.Count == 0 ? "Normalized" : "Ready", reconciliation.MissingRows.Count, null);
    }

    private Task<TargetPeriodPlan> Plan(OpmsTarget target) => TargetPeriodCutover.BuildPlanAsync(context, unitEngine, target.MunicipalityId, target.PeriodId, target.TargetUnitType,
        TargetPeriodCutover.Values(target.AnnualTarget, target.AnnualTargetDescription, target.Q1Target, target.Q1Description, target.Q1Budget, target.Q2Target, target.Q2Description, target.Q2Budget, target.MidTermTarget, target.MidTermDescription, target.MidTermBudget, target.Q3Target, target.Q3Description, target.Q3Budget, target.Q4Target, target.Q4Description, target.Q4Budget));

    private Task<TargetPeriodPlan> Plan(IpmsTarget target) => TargetPeriodCutover.BuildPlanAsync(context, unitEngine, target.MunicipalityId, target.PeriodId, target.TargetUnitType,
        TargetPeriodCutover.Values(target.AnnualTarget, target.AnnualTargetDescription, target.Q1Target, target.Q1Description, target.Q1Budget, target.Q2Target, target.Q2Description, target.Q2Budget, target.MidTermTarget, target.MidTermDescription, target.MidTermBudget, target.Q3Target, target.Q3Description, target.Q3Budget, target.Q4Target, target.Q4Description, target.Q4Budget));

    private static string? ValidateLegacyRevisionState(decimal? q3, decimal? q4, decimal? annual, decimal? budget, string indicatorNumber) =>
        TargetPeriodCutover.ValidateNoLegacyRevisionValues(q3, q4, annual, budget) is { } error ? $"{indicatorNumber}: {error}" : null;
    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before reconciling targets."));
    private ActionResult<ApiResponse<T>> InvalidKind<T>() => BadRequest(Fail<T>("Target kind must be OPMS or IPMS."));
}

public sealed record TargetNormalizationPreviewDto(Guid TargetPublicId, string IndicatorNumber, string TargetName, string Status, int MissingPeriodTargets, string? Error);
public sealed record TargetNormalizationRequest(SubmissionKind TargetKind, Guid[] TargetPublicIds, string Reason);
public sealed record TargetNormalizationResultDto(int SelectedTargets, int AddedPeriodTargets, int AlreadyNormalizedTargets);

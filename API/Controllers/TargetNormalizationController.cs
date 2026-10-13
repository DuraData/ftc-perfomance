using System.Security.Claims;
using System.Data;
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
        if (request.Targets is not { Length: > 0 and <= 100 })
            return BadRequest(Fail<TargetNormalizationResultDto>("Select between 1 and 100 versioned targets."));
        if (request.Targets.Any(item => item == null || item.TargetPublicId == Guid.Empty || !TryVersion(item.RowVersion, out _)))
            return BadRequest(Fail<TargetNormalizationResultDto>("Every selected target requires a public identifier and valid RowVersion."));
        var ids = request.Targets.Select(item => item.TargetPublicId).Distinct().ToArray();
        if (ids.Length != request.Targets.Length)
            return BadRequest(Fail<TargetNormalizationResultDto>("Target public IDs must be unique."));
        var reason = request.Reason?.Trim();
        if (reason is not { Length: >= 10 and <= 1000 })
            return BadRequest(Fail<TargetNormalizationResultDto>("A reconciliation reason between 10 and 1000 characters is required."));
        var user = await CurrentUser();
        if (user == null) return Unauthorized(Fail<TargetNormalizationResultDto>("User not found."));

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var expected = request.Targets.ToDictionary(item => item.TargetPublicId, item => Convert.FromBase64String(item.RowVersion));
        var rows = new List<(Guid PublicId, string IndicatorNumber, TargetPeriodPlan Plan, string? OpmsId, string? IpmsId)>();
        if (request.TargetKind == SubmissionKind.Opms)
        {
            var targets = await context.OpmsTargets.Where(item => ids.Contains(item.PublicId) && item.MunicipalityId == tenantContext.MunicipalityId).ToArrayAsync();
            if (targets.Length != ids.Length) return NotFound(Fail<TargetNormalizationResultDto>("One or more OPMS targets were not found in the selected municipality."));
            foreach (var target in targets)
            {
                if (!target.RowVersion.SequenceEqual(expected[target.PublicId])) { await transaction.RollbackAsync(); return Conflict(Fail<TargetNormalizationResultDto>($"{target.IndicatorNumber}: the target changed concurrently. Refresh the preview.")); }
                var validation = ValidateLegacyRevisionState(target.Q3RevisedTarget, target.Q4RevisedTarget, target.RevisedAnnualTarget, target.RevisedAnnualBudget, target.IndicatorNumber);
                if (validation != null) return Conflict(Fail<TargetNormalizationResultDto>(validation));
                var plan = await Plan(target);
                if (!plan.IsValid) return Conflict(Fail<TargetNormalizationResultDto>($"{target.IndicatorNumber}: {plan.Error}"));
                rows.Add((target.PublicId, target.IndicatorNumber, plan, target.Id, null));
            }
        }
        else
        {
            var targets = await context.IpmsTargets.Where(item => ids.Contains(item.PublicId) && item.MunicipalityId == tenantContext.MunicipalityId).ToArrayAsync();
            if (targets.Length != ids.Length) return NotFound(Fail<TargetNormalizationResultDto>("One or more IPMS targets were not found in the selected municipality."));
            foreach (var target in targets)
            {
                if (!target.RowVersion.SequenceEqual(expected[target.PublicId])) { await transaction.RollbackAsync(); return Conflict(Fail<TargetNormalizationResultDto>($"{target.IndicatorNumber}: the target changed concurrently. Refresh the preview.")); }
                var validation = ValidateLegacyRevisionState(target.Q3RevisedTarget, target.Q4RevisedTarget, target.RevisedAnnualTarget, target.RevisedAnnualBudget, target.IndicatorNumber);
                if (validation != null) return Conflict(Fail<TargetNormalizationResultDto>(validation));
                var plan = await Plan(target);
                if (!plan.IsValid) return Conflict(Fail<TargetNormalizationResultDto>($"{target.IndicatorNumber}: {plan.Error}"));
                rows.Add((target.PublicId, target.IndicatorNumber, plan, null, target.Id));
            }
        }

        var additions = new List<(Guid PublicId, string IndicatorNumber, int Count)>();
        foreach (var row in rows)
        {
            var reconciliation = await TargetPeriodCutover.ReconcileAsync(context, row.Plan, row.OpmsId, row.IpmsId);
            if (!reconciliation.IsValid)
            {
                await transaction.RollbackAsync();
                return Conflict(Fail<TargetNormalizationResultDto>($"{row.IndicatorNumber}: {reconciliation.Error}"));
            }
            TargetPeriodCutover.AddNewRows(context, new TargetPeriodPlan(reconciliation.MissingRows, null), tenantContext.MunicipalityId!.Value, user.Id, row.OpmsId, row.IpmsId);
            additions.Add((row.PublicId, row.IndicatorNumber, reconciliation.MissingRows.Count));
        }
        foreach (var addition in additions.Where(item => item.Count > 0))
            workflowGovernance.QueueAuditTrail(
                request.TargetKind == SubmissionKind.Opms ? "OpmsTarget" : "IpmsTarget",
                addition.PublicId.ToString(),
                "NormalizeLegacyPeriodTargets",
                null,
                new { addition.PublicId, addition.IndicatorNumber, AddedPeriodTargets = addition.Count, Reason = reason },
                user.Id,
                PerformanceApiSupport.GetIpAddress(HttpContext), reason);
        await context.SaveChangesAsync();
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
                await Plan(target), target.Id, null, Version(target.RowVersion)));
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
                await Plan(target), null, target.Id, Version(target.RowVersion)));
        return results.ToArray();
    }

    private async Task<TargetNormalizationPreviewDto> Preview(Guid publicId, string indicatorNumber, string targetName, string? legacyError, TargetPeriodPlan plan, string? opmsId, string? ipmsId, string rowVersion)
    {
        if (legacyError != null) return new(publicId, indicatorNumber, targetName, "Blocked", 0, legacyError, rowVersion);
        if (!plan.IsValid) return new(publicId, indicatorNumber, targetName, "Blocked", 0, plan.Error, rowVersion);
        var reconciliation = await TargetPeriodCutover.ReconcileAsync(context, plan, opmsId, ipmsId);
        if (!reconciliation.IsValid) return new(publicId, indicatorNumber, targetName, "Blocked", 0, reconciliation.Error, rowVersion);
        return new(publicId, indicatorNumber, targetName, reconciliation.MissingRows.Count == 0 ? "Normalized" : "Ready", reconciliation.MissingRows.Count, null, rowVersion);
    }

    private Task<TargetPeriodPlan> Plan(OpmsTarget target) => TargetPeriodCutover.BuildLegacyPlanAsync(context, unitEngine, target.MunicipalityId, target.PeriodId, target.TargetUnitType,
        TargetPeriodCutover.LegacyValues(target.AnnualTarget, target.AnnualTargetDescription, target.Q1Target, target.Q1Description, target.Q1Budget, target.Q2Target, target.Q2Description, target.Q2Budget, target.MidTermTarget, target.MidTermDescription, target.MidTermBudget, target.Q3Target, target.Q3Description, target.Q3Budget, target.Q4Target, target.Q4Description, target.Q4Budget));

    private Task<TargetPeriodPlan> Plan(IpmsTarget target) => TargetPeriodCutover.BuildLegacyPlanAsync(context, unitEngine, target.MunicipalityId, target.PeriodId, target.TargetUnitType,
        TargetPeriodCutover.LegacyValues(target.AnnualTarget, target.AnnualTargetDescription, target.Q1Target, target.Q1Description, target.Q1Budget, target.Q2Target, target.Q2Description, target.Q2Budget, target.MidTermTarget, target.MidTermDescription, target.MidTermBudget, target.Q3Target, target.Q3Description, target.Q3Budget, target.Q4Target, target.Q4Description, target.Q4Budget));

    private static string? ValidateLegacyRevisionState(decimal? q3, decimal? q4, decimal? annual, decimal? budget, string indicatorNumber) =>
        TargetPeriodCutover.ValidateLegacyRevisionValues(q3, q4, annual, budget) is { } error ? $"{indicatorNumber}: {error}" : null;
    private async Task<ApplicationUser?> CurrentUser() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return id == null ? null : await userManager.FindByIdAsync(id); }
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private static string Version(byte[] value) => Convert.ToBase64String(value);
    private static bool TryVersion(string? value, out byte[] version) { version = []; try { version = Convert.FromBase64String(value ?? string.Empty); return version.Length > 0; } catch (FormatException) { return false; } }
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before reconciling targets."));
    private ActionResult<ApiResponse<T>> InvalidKind<T>() => BadRequest(Fail<T>("Target kind must be OPMS or IPMS."));
}

public sealed record TargetNormalizationPreviewDto(Guid TargetPublicId, string IndicatorNumber, string TargetName, string Status, int MissingPeriodTargets, string? Error, string RowVersion);
public sealed record TargetNormalizationSelection(Guid TargetPublicId, string RowVersion);
public sealed record TargetNormalizationRequest(SubmissionKind TargetKind, TargetNormalizationSelection[] Targets, string Reason);
public sealed record TargetNormalizationResultDto(int SelectedTargets, int AddedPeriodTargets, int AlreadyNormalizedTargets);

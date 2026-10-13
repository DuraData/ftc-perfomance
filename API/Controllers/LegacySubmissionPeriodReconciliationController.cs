using System.Security.Claims;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/v1/workflow/period-reconciliation")]
[Authorize]
[Authorize(Policy = "Permission:WORKFLOW.RECONCILE_LEGACY_PERIODS")]
public sealed class LegacySubmissionPeriodReconciliationController(
    ApplicationDbContext context,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IWorkflowGovernanceService governance) : ControllerBase
{
    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>>>> Preview(
        [FromQuery] SubmissionKind submissionKind,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!HasTenant()) return TenantRequired<PagedResponse<LegacySubmissionPeriodPreviewDto>>();
        if (!ValidKind(submissionKind)) return InvalidKind<PagedResponse<LegacySubmissionPeriodPreviewDto>>();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (submissionKind == SubmissionKind.Opms)
        {
            var query = context.OpmsSubmissions.AsNoTracking()
                .Where(x => x.ReportingPeriodId == null && x.MunicipalityId == tenantContext.MunicipalityId);
            var total = await query.CountAsync();
            var rows = await query.Include(x => x.OpmsTarget).ThenInclude(x => x.Period)
                .OrderBy(x => x.CreatedOn).ThenBy(x => x.PublicId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
            var items = new List<LegacySubmissionPeriodPreviewDto>(rows.Length);
            foreach (var row in rows)
                items.Add(await Analyze(SubmissionKind.Opms, row.PublicId, row.Id, row.OpmsTargetId,
                    row.OpmsTarget.IndicatorNumber, row.OpmsTarget.TargetName, row.OpmsTarget.Period?.FiscalYear,
                    row.Quarter, Convert.ToBase64String(row.RowVersion)));
            return Ok(Page(items, page, pageSize, total));
        }

        {
            var query = context.IpmsSubmissions.AsNoTracking()
                .Where(x => x.ReportingPeriodId == null && x.MunicipalityId == tenantContext.MunicipalityId);
            var total = await query.CountAsync();
            var rows = await query.Include(x => x.IpmsTarget).ThenInclude(x => x.Period)
                .OrderBy(x => x.CreatedOn).ThenBy(x => x.PublicId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();
            var items = new List<LegacySubmissionPeriodPreviewDto>(rows.Length);
            foreach (var row in rows)
                items.Add(await Analyze(SubmissionKind.Ipms, row.PublicId, row.Id, row.IpmsTargetId,
                    row.IpmsTarget.IndicatorNumber, row.IpmsTarget.TargetName, row.IpmsTarget.Period?.FiscalYear,
                    row.Quarter, Convert.ToBase64String(row.RowVersion)));
            return Ok(Page(items, page, pageSize, total));
        }
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<LegacySubmissionPeriodResultDto>>> Execute(LegacySubmissionPeriodRequest request)
    {
        if (!HasTenant()) return TenantRequired<LegacySubmissionPeriodResultDto>();
        if (!ValidKind(request.SubmissionKind)) return InvalidKind<LegacySubmissionPeriodResultDto>();
        if (request.SubmissionPublicId == Guid.Empty || request.ReportingPeriodPublicId == Guid.Empty)
            return BadRequest(Fail<LegacySubmissionPeriodResultDto>("Submission and reporting-period public identifiers are required."));
        var reason = request.Reason?.Trim();
        if (reason is not { Length: >= 10 and <= 1000 })
            return BadRequest(Fail<LegacySubmissionPeriodResultDto>("A reconciliation reason of 10 to 1000 characters is required."));
        if (!TryVersion(request.RowVersion, out var expectedVersion))
            return BadRequest(Fail<LegacySubmissionPeriodResultDto>("A valid submission RowVersion is required."));
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actor = actorId == null ? null : await userManager.FindByIdAsync(actorId);
        if (actor == null) return Unauthorized(Fail<LegacySubmissionPeriodResultDto>("User not found."));

        string submissionId;
        string targetId;
        string indicator;
        string targetName;
        string quarter;
        string? fiscalYear;
        OpmsSubmission? opms = null;
        IpmsSubmission? ipms = null;
        if (request.SubmissionKind == SubmissionKind.Opms)
        {
            opms = await context.OpmsSubmissions.Include(x => x.OpmsTarget).ThenInclude(x => x.Period)
                .SingleOrDefaultAsync(x => x.PublicId == request.SubmissionPublicId && x.MunicipalityId == tenantContext.MunicipalityId);
            if (opms == null) return NotFound(Fail<LegacySubmissionPeriodResultDto>("The OPMS submission was not found in the selected municipality."));
            if (opms.ReportingPeriodId.HasValue) return Conflict(Fail<LegacySubmissionPeriodResultDto>("The submission already has a canonical reporting period."));
            context.Entry(opms).Property(x => x.RowVersion).OriginalValue = expectedVersion;
            (submissionId, targetId, indicator, targetName, fiscalYear, quarter) =
                (opms.Id, opms.OpmsTargetId, opms.OpmsTarget.IndicatorNumber, opms.OpmsTarget.TargetName, opms.OpmsTarget.Period?.FiscalYear, opms.Quarter);
        }
        else
        {
            ipms = await context.IpmsSubmissions.Include(x => x.IpmsTarget).ThenInclude(x => x.Period)
                .SingleOrDefaultAsync(x => x.PublicId == request.SubmissionPublicId && x.MunicipalityId == tenantContext.MunicipalityId);
            if (ipms == null) return NotFound(Fail<LegacySubmissionPeriodResultDto>("The IPMS submission was not found in the selected municipality."));
            if (ipms.ReportingPeriodId.HasValue) return Conflict(Fail<LegacySubmissionPeriodResultDto>("The submission already has a canonical reporting period."));
            context.Entry(ipms).Property(x => x.RowVersion).OriginalValue = expectedVersion;
            (submissionId, targetId, indicator, targetName, fiscalYear, quarter) =
                (ipms.Id, ipms.IpmsTargetId, ipms.IpmsTarget.IndicatorNumber, ipms.IpmsTarget.TargetName, ipms.IpmsTarget.Period?.FiscalYear, ipms.Quarter);
        }

        var preview = await Analyze(request.SubmissionKind, request.SubmissionPublicId, submissionId, targetId,
            indicator, targetName, fiscalYear, quarter, request.RowVersion);
        var selected = preview.Candidates.SingleOrDefault(x => x.ReportingPeriodPublicId == request.ReportingPeriodPublicId && !x.HasSubmissionConflict);
        if (selected == null)
            return Conflict(Fail<LegacySubmissionPeriodResultDto>("The selected period is not an eligible target-scoped match for the preserved legacy quarter."));
        var period = await context.ReportingPeriods.SingleAsync(x => x.PublicId == selected.ReportingPeriodPublicId);

        var reconciliation = new LegacySubmissionPeriodReconciliation
        {
            MunicipalityId = tenantContext.MunicipalityId!.Value,
            SubmissionKind = request.SubmissionKind,
            OpmsSubmissionId = opms?.Id,
            OpmsSubmission = opms,
            IpmsSubmissionId = ipms?.Id,
            IpmsSubmission = ipms,
            SourceQuarterSnapshot = quarter,
            ReportingPeriodId = period.Id,
            ReportingPeriod = period,
            ReconciledByUserId = actor.Id,
            ReconciledByUser = actor,
            ReconciledAt = DateTime.UtcNow,
            Reason = reason,
            CorrelationId = Bounded(HttpContext.TraceIdentifier, 100)
        };

        await using var transaction = await context.Database.BeginTransactionAsync();
        if (opms != null)
        {
            opms.ReportingPeriodId = period.Id;
            opms.UpdatedBy = actor.Id;
            opms.UpdatedOn = reconciliation.ReconciledAt;
        }
        else
        {
            ipms!.ReportingPeriodId = period.Id;
            ipms.UpdatedBy = actor.Id;
            ipms.UpdatedOn = reconciliation.ReconciledAt;
        }
        context.LegacySubmissionPeriodReconciliations.Add(reconciliation);
        governance.QueueAuditTrail(nameof(LegacySubmissionPeriodReconciliation), reconciliation.PublicId.ToString(), "Reconcile",
            new { SubmissionPublicId = request.SubmissionPublicId, LegacyQuarter = quarter, ReportingPeriodPublicId = (Guid?)null },
            new { SubmissionPublicId = request.SubmissionPublicId, LegacyQuarter = quarter, ReportingPeriodPublicId = period.PublicId, period.Code },
            actor.Id, PerformanceApiSupport.GetIpAddress(HttpContext), reason);
        try
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(Fail<LegacySubmissionPeriodResultDto>("The submission changed concurrently. Refresh the preview."));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(Fail<LegacySubmissionPeriodResultDto>("The submission period was reconciled concurrently or conflicts with another logical submission."));
        }

        return Ok(new ApiResponse<LegacySubmissionPeriodResultDto>(true,
            new(reconciliation.PublicId, request.SubmissionPublicId, period.PublicId, period.Code)));
    }

    private async Task<LegacySubmissionPeriodPreviewDto> Analyze(
        SubmissionKind kind, Guid publicId, string submissionId, string targetId, string indicator,
        string targetName, string? fiscalYear, string quarter, string rowVersion)
    {
        if (!TryPeriodType(quarter, out var periodType))
            return Preview(kind, publicId, indicator, targetName, quarter, "Blocked",
                "The preserved quarter is blank or cannot be mapped to a canonical reporting-period type.", [], rowVersion);

        var targetPeriods = kind == SubmissionKind.Opms
            ? await context.PerformancePeriodTargets.AsNoTracking()
                .Where(x => x.OpmsTargetId == targetId && x.IsActive && x.ReportingPeriod.PeriodType == periodType)
                .Include(x => x.ReportingPeriod).ThenInclude(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).ToArrayAsync()
            : await context.PerformancePeriodTargets.AsNoTracking()
                .Where(x => x.IpmsTargetId == targetId && x.IsActive && x.ReportingPeriod.PeriodType == periodType)
                .Include(x => x.ReportingPeriod).ThenInclude(x => x.MunicipalityFinancialYear).ThenInclude(x => x.FinancialYear).ToArrayAsync();
        if (!string.IsNullOrWhiteSpace(fiscalYear))
            targetPeriods = targetPeriods.Where(x => string.Equals(x.ReportingPeriod.MunicipalityFinancialYear.FinancialYear.Code, fiscalYear, StringComparison.OrdinalIgnoreCase)).ToArray();

        var candidates = new List<LegacySubmissionPeriodCandidateDto>();
        foreach (var row in targetPeriods.GroupBy(x => x.ReportingPeriodId).Select(x => x.First()).OrderBy(x => x.ReportingPeriod.Sequence))
        {
            var conflict = kind == SubmissionKind.Opms
                ? await context.OpmsSubmissions.AsNoTracking().AnyAsync(x => x.Id != submissionId && x.OpmsTargetId == targetId && x.ReportingPeriodId == row.ReportingPeriodId)
                : await context.IpmsSubmissions.AsNoTracking().AnyAsync(x => x.Id != submissionId && x.IpmsTargetId == targetId && x.ReportingPeriodId == row.ReportingPeriodId);
            candidates.Add(new(row.ReportingPeriod.PublicId, row.ReportingPeriod.Code, row.ReportingPeriod.Name,
                row.ReportingPeriod.PeriodType, row.ReportingPeriod.MunicipalityFinancialYear.FinancialYear.Code, conflict));
        }

        var eligible = candidates.Count(x => !x.HasSubmissionConflict);
        if (candidates.Count == 0)
            return Preview(kind, publicId, indicator, targetName, quarter, "Blocked",
                "No active target-scoped canonical period matches the preserved quarter and financial year.", candidates, rowVersion);
        if (eligible == 0)
            return Preview(kind, publicId, indicator, targetName, quarter, "Blocked",
                "Every matching period already has another logical submission for this target.", candidates, rowVersion);
        return Preview(kind, publicId, indicator, targetName, quarter, eligible == 1 ? "Ready" : "RequiresMapping",
            eligible == 1 ? "One exact target-scoped period is available." : "Multiple target-scoped periods match; select the evidenced period explicitly.", candidates, rowVersion);
    }

    private static bool TryPeriodType(string? value, out ReportingPeriodType type)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        type = normalized switch
        {
            "1" or "Q1" or "QUARTER1" or "QUARTERONE" => ReportingPeriodType.Quarter1,
            "2" or "Q2" or "QUARTER2" or "QUARTERTWO" => ReportingPeriodType.Quarter2,
            "MIDTERM" or "MIDYEAR" or "HALFYEAR" or "HALFYEARLY" => ReportingPeriodType.MidTerm,
            "3" or "Q3" or "QUARTER3" or "QUARTERTHREE" => ReportingPeriodType.Quarter3,
            "4" or "Q4" or "QUARTER4" or "QUARTERFOUR" => ReportingPeriodType.Quarter4,
            "ANNUAL" or "YEAR" or "YEAREND" or "FULLYEAR" => ReportingPeriodType.Annual,
            _ => 0
        };
        return type != 0;
    }

    private static LegacySubmissionPeriodPreviewDto Preview(SubmissionKind kind, Guid publicId, string indicator,
        string targetName, string quarter, string status, string message,
        IReadOnlyList<LegacySubmissionPeriodCandidateDto> candidates, string rowVersion) =>
        new(kind, publicId, indicator, targetName, quarter, status, message, candidates, rowVersion);

    private static ApiResponse<PagedResponse<LegacySubmissionPeriodPreviewDto>> Page(
        IReadOnlyList<LegacySubmissionPeriodPreviewDto> items, int page, int pageSize, int total) =>
        new(true, PagedResponse<LegacySubmissionPeriodPreviewDto>.Create(items, page, pageSize, total));
    private bool HasTenant() => tenantContext.MunicipalityId is > 0;
    private static bool ValidKind(SubmissionKind kind) => kind is SubmissionKind.Opms or SubmissionKind.Ipms;
    private static bool TryVersion(string? value, out byte[] version)
    {
        version = [];
        try { version = Convert.FromBase64String(value ?? string.Empty); return version.Length > 0; }
        catch (FormatException) { return false; }
    }
    private static string Bounded(string? value, int length) => string.IsNullOrWhiteSpace(value) ? "legacy-period-reconciliation" : value[..Math.Min(length, value.Length)];
    private static ApiResponse<T> Fail<T>(string message) => new(false, default, message);
    private ActionResult<ApiResponse<T>> TenantRequired<T>() => StatusCode(StatusCodes.Status409Conflict, Fail<T>("Select a municipality context before reconciling submission periods."));
    private ActionResult<ApiResponse<T>> InvalidKind<T>() => BadRequest(Fail<T>("Submission kind must be OPMS or IPMS."));
}

public sealed record LegacySubmissionPeriodCandidateDto(Guid ReportingPeriodPublicId, string Code, string Name,
    ReportingPeriodType PeriodType, string FinancialYearCode, bool HasSubmissionConflict);
public sealed record LegacySubmissionPeriodPreviewDto(SubmissionKind SubmissionKind, Guid SubmissionPublicId,
    string IndicatorNumber, string TargetName, string LegacyQuarter, string Status, string Message,
    IReadOnlyList<LegacySubmissionPeriodCandidateDto> Candidates, string RowVersion);
public sealed record LegacySubmissionPeriodRequest(SubmissionKind SubmissionKind, Guid SubmissionPublicId,
    Guid ReportingPeriodPublicId, string Reason, string RowVersion);
public sealed record LegacySubmissionPeriodResultDto(Guid ReconciliationPublicId, Guid SubmissionPublicId,
    Guid ReportingPeriodPublicId, string ReportingPeriodCode);

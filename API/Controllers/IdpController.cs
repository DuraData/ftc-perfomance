using FTCERP.Host.API.Requests;
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
[Route("api/idp")]
[Authorize]
public class IdpController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;

    public IdpController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWorkflowGovernanceService workflowGovernanceService)
    {
        _context = context;
        _userManager = userManager;
        _workflowGovernanceService = workflowGovernanceService;
    }

    [HttpGet("plans")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public async Task<ActionResult<ApiResponse<IdpPlanSummaryResponse[]>>> GetPlans()
    {
        var plans = await _context.IdpPlans
            .AsNoTracking()
            .OrderByDescending(plan => plan.CreatedAt)
            .Select(plan => ToSummaryResponse(plan))
            .ToArrayAsync();

        return Ok(new ApiResponse<IdpPlanSummaryResponse[]>(true, plans));
    }

    [HttpPost("plans")]
    [Authorize(Policy = "Permission:IDP.Plan.Manage")]
    public async Task<ActionResult<ApiResponse<IdpPlanSummaryResponse>>> CreatePlan([FromBody] CreateIdpPlanRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new ApiResponse<IdpPlanSummaryResponse>(false, null, "User not found"));
        }

        var entity = new IdpPlan
        {
            MunicipalityName = request.MunicipalityName.Trim(),
            PlanTitle = request.PlanTitle.Trim(),
            PlanCode = request.PlanCode.Trim(),
            StartFinancialYear = request.StartFinancialYear,
            EndFinancialYear = request.EndFinancialYear,
            Status = IdpPlanStatus.Draft,
            CurrentVersionNumber = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        };

        _context.IdpPlans.Add(entity);
        await _context.SaveChangesAsync();

        var version = new IdpPlanVersion
        {
            IdpPlanId = entity.Id,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original Approved IDP",
            ReviewYear = null,
            SummaryOfChanges = "Original approved five-year IDP version.",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            IsActive = true
        };

        _context.IdpPlanVersions.Add(version);
        await _context.SaveChangesAsync();

        await _workflowGovernanceService.WriteAuditTrailAsync(
            "IdpPlan",
            entity.Id.ToString(),
            "Create",
            null,
            ToSummaryResponse(entity),
            user.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpPlanSummaryResponse>(true, ToSummaryResponse(entity)));
    }

    [HttpPut("plans/{id:int}")]
    [Authorize(Policy = "Permission:IDP.Plan.Manage")]
    public async Task<ActionResult<ApiResponse<IdpPlanSummaryResponse>>> UpdatePlan(int id, [FromBody] UpdateIdpPlanRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new ApiResponse<IdpPlanSummaryResponse>(false, null, "User not found"));
        }

        var entity = await _context.IdpPlans.FirstOrDefaultAsync(plan => plan.Id == id);
        if (entity == null)
        {
            return NotFound(new ApiResponse<IdpPlanSummaryResponse>(false, null, "IDP plan not found"));
        }

        var before = ToSummaryResponse(entity);
        entity.PlanTitle = request.PlanTitle.Trim();
        entity.StartFinancialYear = request.StartFinancialYear;
        entity.EndFinancialYear = request.EndFinancialYear;
        if (TryParseEnum(request.Status, out IdpPlanStatus status))
        {
            entity.Status = status;
            if (status == IdpPlanStatus.Approved || status == IdpPlanStatus.Published)
            {
                entity.ApprovedAt = DateTime.UtcNow;
                entity.ApprovedByUserId = user.Id;
            }
        }

        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync(
            "IdpPlan",
            entity.Id.ToString(),
            "Update",
            before,
            ToSummaryResponse(entity),
            user.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpPlanSummaryResponse>(true, ToSummaryResponse(entity)));
    }

    [HttpPost("plans/{id:int}/versions")]
    [Authorize(Policy = "Permission:IDP.Version.Manage")]
    public async Task<ActionResult<ApiResponse<IdpPlanVersionResponse>>> CreatePlanVersion(int id, [FromBody] CreateIdpPlanVersionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new ApiResponse<IdpPlanVersionResponse>(false, null, "User not found"));
        }

        var plan = await _context.IdpPlans.FirstOrDefaultAsync(item => item.Id == id);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpPlanVersionResponse>(false, null, "IDP plan not found"));
        }

        if (!TryParseEnum(request.VersionType, out IdpVersionType versionType))
        {
            return BadRequest(new ApiResponse<IdpPlanVersionResponse>(false, null, "Invalid version type"));
        }

        var nextVersion = plan.CurrentVersionNumber + 1;
        var entity = new IdpPlanVersion
        {
            IdpPlanId = id,
            VersionNumber = nextVersion,
            VersionType = versionType,
            VersionLabel = request.VersionLabel.Trim(),
            ReviewYear = request.ReviewYear,
            SummaryOfChanges = request.SummaryOfChanges,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id
        };

        var previousActive = await _context.IdpPlanVersions.Where(item => item.IdpPlanId == id && item.IsActive).ToListAsync();
        foreach (var existing in previousActive)
        {
            existing.IsActive = false;
        }

        _context.IdpPlanVersions.Add(entity);
        plan.CurrentVersionNumber = nextVersion;
        await _context.SaveChangesAsync();

        await _workflowGovernanceService.WriteAuditTrailAsync(
            "IdpPlanVersion",
            entity.Id.ToString(),
            "Create",
            null,
            ToVersionResponse(entity),
            user.Id,
            PerformanceApiSupport.GetIpAddress(HttpContext));

        return Ok(new ApiResponse<IdpPlanVersionResponse>(true, ToVersionResponse(entity)));
    }

    [HttpGet("plans/{id:int}/hierarchy")]
    [Authorize(Policy = "Permission:IDP.Plan.View")]
    public async Task<ActionResult<ApiResponse<IdpHierarchyResponse>>> GetHierarchy(int id)
    {
        var plan = await _context.IdpPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpHierarchyResponse>(false, null, "IDP plan not found"));
        }

        var versions = await _context.IdpPlanVersions
            .AsNoTracking()
            .Where(item => item.IdpPlanId == id)
            .OrderByDescending(item => item.VersionNumber)
            .ToArrayAsync();

        var outcomes = await _context.IdpStrategicOutcomes
            .AsNoTracking()
            .Where(item => item.IdpPlanId == id)
            .OrderBy(item => item.SortOrder)
            .ToArrayAsync();

        var outcomeIds = outcomes.Select(item => item.Id).ToArray();

        var objectives = await _context.IdpStrategicObjectives
            .AsNoTracking()
            .Include(item => item.ResponsibleDepartment)
            .Include(item => item.StrategicOwnerUser)
            .Where(item => outcomeIds.Contains(item.IdpStrategicOutcomeId))
            .OrderBy(item => item.SortOrder)
            .ToArrayAsync();

        var objectiveIds = objectives.Select(item => item.Id).ToArray();

        var priorities = await _context.IdpDevelopmentPriorities
            .AsNoTracking()
            .Where(item => objectiveIds.Contains(item.IdpStrategicObjectiveId))
            .OrderBy(item => item.SortOrder)
            .ToArrayAsync();

        var priorityIds = priorities.Select(item => item.Id).ToArray();

        var programmes = await _context.IdpProgrammes
            .AsNoTracking()
            .Include(item => item.ResponsibleDepartment)
            .Where(item => priorityIds.Contains(item.IdpDevelopmentPriorityId))
            .ToArrayAsync();

        var programmeIds = programmes.Select(item => item.Id).ToArray();

        var projects = await _context.IdpProjects
            .AsNoTracking()
            .Include(item => item.Department)
            .Where(item => programmeIds.Contains(item.IdpProgrammeId))
            .ToArrayAsync();

        var projectIds = projects.Select(item => item.Id).ToArray();

        var kpis = await _context.IdpKpis
            .AsNoTracking()
            .Include(item => item.ResponsibleDepartment)
            .Where(item => projectIds.Contains(item.IdpProjectId))
            .ToArrayAsync();

        var kpiIds = kpis.Select(item => item.Id).ToArray();

        var annualTargets = await _context.IdpAnnualTargets
            .AsNoTracking()
            .Where(item => kpiIds.Contains(item.IdpKpiId))
            .ToArrayAsync();

        var alignmentLinks = await _context.IdpAlignmentLinks
            .AsNoTracking()
            .Where(item => objectiveIds.Contains(item.IdpStrategicObjectiveId))
            .ToArrayAsync();

        var riskLinks = await _context.IdpRiskLinks
            .AsNoTracking()
            .Where(item => (item.IdpStrategicObjectiveId.HasValue && objectiveIds.Contains(item.IdpStrategicObjectiveId.Value))
                || (item.IdpProjectId.HasValue && projectIds.Contains(item.IdpProjectId.Value))
                || (item.IdpKpiId.HasValue && kpiIds.Contains(item.IdpKpiId.Value)))
            .ToArrayAsync();

        var budgetSnapshots = await _context.IdpBudgetSnapshots
            .AsNoTracking()
            .Where(item => (item.IdpStrategicObjectiveId.HasValue && objectiveIds.Contains(item.IdpStrategicObjectiveId.Value))
                || (item.IdpProjectId.HasValue && projectIds.Contains(item.IdpProjectId.Value)))
            .ToArrayAsync();

        var response = new IdpHierarchyResponse(
            ToSummaryResponse(plan),
            versions.Select(ToVersionResponse).ToArray(),
            outcomes.Select(ToOutcomeResponse).ToArray(),
            objectives.Select(ToObjectiveResponse).ToArray(),
            priorities.Select(ToPriorityResponse).ToArray(),
            programmes.Select(ToProgrammeResponse).ToArray(),
            projects.Select(ToProjectResponse).ToArray(),
            kpis.Select(ToKpiResponse).ToArray(),
            annualTargets.Select(ToAnnualTargetResponse).ToArray(),
            alignmentLinks.Select(ToAlignmentResponse).ToArray(),
            riskLinks.Select(ToRiskResponse).ToArray(),
            budgetSnapshots.Select(ToBudgetSnapshotResponse).ToArray());

        return Ok(new ApiResponse<IdpHierarchyResponse>(true, response));
    }

    [HttpGet("plans/{id:int}/dashboard")]
    [Authorize(Policy = "Permission:IDP.Dashboard.View")]
    public async Task<ActionResult<ApiResponse<IdpDashboardResponse>>> GetDashboard(int id)
    {
        var plan = await _context.IdpPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpDashboardResponse>(false, null, "IDP plan not found"));
        }

        var outcomes = await _context.IdpStrategicOutcomes.CountAsync(item => item.IdpPlanId == id);

        var objectiveIds = await _context.IdpStrategicObjectives
            .Where(item => item.IdpStrategicOutcome.IdpPlanId == id)
            .Select(item => item.Id)
            .ToArrayAsync();

        var projectIds = await _context.IdpProjects
            .Where(item => item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == id)
            .Select(item => item.Id)
            .ToArrayAsync();

        var kpiIds = await _context.IdpKpis
            .Where(item => projectIds.Contains(item.IdpProjectId))
            .Select(item => item.Id)
            .ToArrayAsync();

        var annualTargets = await _context.IdpAnnualTargets
            .Where(item => kpiIds.Contains(item.IdpKpiId))
            .ToArrayAsync();

        var achievedCount = annualTargets.Count(item => item.ActualValue.HasValue && item.ActualValue.Value >= item.TargetValue);
        var kpiAchievementRate = annualTargets.Length == 0 ? 0m : (decimal)achievedCount / annualTargets.Length * 100m;

        var budgetSnapshots = await _context.IdpBudgetSnapshots
            .Where(item => (item.IdpStrategicObjectiveId.HasValue && objectiveIds.Contains(item.IdpStrategicObjectiveId.Value))
                || (item.IdpProjectId.HasValue && projectIds.Contains(item.IdpProjectId.Value)))
            .ToArrayAsync();

        var topRiskTitles = await _context.IdpRiskLinks
            .Where(item => (item.IdpStrategicObjectiveId.HasValue && objectiveIds.Contains(item.IdpStrategicObjectiveId.Value))
                || (item.IdpProjectId.HasValue && projectIds.Contains(item.IdpProjectId.Value))
                || (item.IdpKpiId.HasValue && kpiIds.Contains(item.IdpKpiId.Value)))
            .OrderByDescending(item => item.RiskLevel)
            .Select(item => item.RiskTitle)
            .Distinct()
            .Take(5)
            .ToArrayAsync();

        var sessions = await _context.IdpCommunitySessions
            .AsNoTracking()
            .Where(item => item.IdpPlanId == id && item.WardId.HasValue)
            .Select(item => new
            {
                item.Id,
                item.WardId,
                WardName = item.Ward != null ? item.Ward.Name : "Unknown Ward",
                item.ParticipantsCount
            })
            .ToListAsync();

        var sessionIds = sessions.Select(item => item.Id).ToArray();
        var needsBySession = await _context.IdpCommunityNeeds
            .AsNoTracking()
            .Where(item => sessionIds.Contains(item.IdpCommunitySessionId))
            .GroupBy(item => item.IdpCommunitySessionId)
            .Select(group => new { SessionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.SessionId, item => item.Count);

        var wardParticipation = sessions
            .GroupBy(item => new { item.WardId, item.WardName })
            .Select(group => new IdpWardParticipationResponse(
                group.Key.WardId ?? 0,
                group.Key.WardName,
                group.Count(),
                group.Sum(item => item.ParticipantsCount),
                group.Sum(item => needsBySession.TryGetValue(item.Id, out var count) ? count : 0)))
            .OrderByDescending(item => item.ParticipantsCount)
            .ToArray();

        var matrix = await BuildAlignmentMatrixAsync(id);

        var response = new IdpDashboardResponse(
            id,
            plan.PlanTitle,
            outcomes,
            objectiveIds.Length,
            projectIds.Length,
            kpiIds.Length,
            await _context.IdpCommunitySessions.CountAsync(item => item.IdpPlanId == id),
            topRiskTitles.Length,
            budgetSnapshots.Sum(item => item.PlannedBudget),
            budgetSnapshots.Sum(item => item.ApprovedBudget),
            budgetSnapshots.Sum(item => item.ActualExpenditure),
            decimal.Round(kpiAchievementRate, 2),
            topRiskTitles,
            wardParticipation,
            matrix);

        return Ok(new ApiResponse<IdpDashboardResponse>(true, response));
    }

    [HttpGet("plans/{id:int}/alignment-matrix")]
    [Authorize(Policy = "Permission:IDP.Alignment.View")]
    public async Task<ActionResult<ApiResponse<IdpAlignmentMatrixItemResponse[]>>> GetAlignmentMatrix(int id)
    {
        var matrix = await BuildAlignmentMatrixAsync(id);
        return Ok(new ApiResponse<IdpAlignmentMatrixItemResponse[]>(true, matrix));
    }

    [HttpGet("plans/{id:int}/reports/{reportType}")]
    [Authorize(Policy = "Permission:IDP.Reports.Generate")]
    public async Task<ActionResult<ApiResponse<IdpReportDocumentResponse>>> GenerateReport(int id, string reportType, [FromQuery] string format = "pdf")
    {
        var plan = await _context.IdpPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (plan == null)
        {
            return NotFound(new ApiResponse<IdpReportDocumentResponse>(false, null, "IDP plan not found"));
        }

        var normalizedFormat = (format ?? "pdf").Trim().ToLowerInvariant();
        var contentType = normalizedFormat switch
        {
            "xlsx" or "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "docx" or "word" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/pdf"
        };

        var reportName = $"{reportType.ToUpperInvariant()} - {plan.PlanCode} ({DateTime.UtcNow:yyyy-MM-dd})";
        var reportText = $"EPMS IDP REPORT\nName: {reportName}\nPlan: {plan.PlanTitle}\nMunicipality: {plan.MunicipalityName}\nGenerated UTC: {DateTime.UtcNow:O}";
        var fileName = $"{plan.PlanCode}_{reportType}_{DateTime.UtcNow:yyyyMMddHHmmss}.{(normalizedFormat == "word" ? "docx" : normalizedFormat == "excel" ? "xlsx" : normalizedFormat)}";

        var response = new IdpReportDocumentResponse(reportName, contentType, fileName, System.Text.Encoding.UTF8.GetBytes(reportText));
        return Ok(new ApiResponse<IdpReportDocumentResponse>(true, response));
    }

    [HttpPost("outcomes")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpStrategicOutcomeResponse>>> CreateOutcome([FromBody] CreateIdpStrategicOutcomeRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpStrategicOutcomeResponse>(false, null, "User not found"));

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpStrategicOutcomeResponse>(false, null, "IDP plan not found"));

        var entity = new IdpStrategicOutcome
        {
            IdpPlanId = request.IdpPlanId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            SortOrder = request.SortOrder
        };

        _context.IdpStrategicOutcomes.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpStrategicOutcome", entity.Id.ToString(), "Create", null, ToOutcomeResponse(entity));

        return Ok(new ApiResponse<IdpStrategicOutcomeResponse>(true, ToOutcomeResponse(entity)));
    }

    [HttpPost("objectives")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpStrategicObjectiveResponse>>> CreateObjective([FromBody] CreateIdpStrategicObjectiveRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpStrategicObjectiveResponse>(false, null, "User not found"));

        var outcomeExists = await _context.IdpStrategicOutcomes.AnyAsync(item => item.Id == request.IdpStrategicOutcomeId);
        if (!outcomeExists) return NotFound(new ApiResponse<IdpStrategicObjectiveResponse>(false, null, "Strategic outcome not found"));

        var entity = new IdpStrategicObjective
        {
            IdpStrategicOutcomeId = request.IdpStrategicOutcomeId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            BaselineValue = request.BaselineValue,
            TargetValue = request.TargetValue,
            ResponsibleDepartmentId = request.ResponsibleDepartmentId,
            StrategicOwnerUserId = request.StrategicOwnerUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BudgetAllocation = request.BudgetAllocation,
            SortOrder = request.SortOrder
        };

        _context.IdpStrategicObjectives.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpStrategicObjectives
            .Include(item => item.ResponsibleDepartment)
            .Include(item => item.StrategicOwnerUser)
            .FirstAsync(item => item.Id == entity.Id);

        await WriteIdpAudit(user.Id, "IdpStrategicObjective", entity.Id.ToString(), "Create", null, ToObjectiveResponse(entity));
        return Ok(new ApiResponse<IdpStrategicObjectiveResponse>(true, ToObjectiveResponse(entity)));
    }

    [HttpPost("priorities")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpDevelopmentPriorityResponse>>> CreatePriority([FromBody] CreateIdpDevelopmentPriorityRequest request)
    {
        return Ok(new ApiResponse<IdpDevelopmentPriorityResponse>(true, ToPriorityResponse(await CreateEntity(request))));
    }

    [HttpPost("programmes")]
    [Authorize(Policy = "Permission:IDP.Hierarchy.Manage")]
    public async Task<ActionResult<ApiResponse<IdpProgrammeResponse>>> CreateProgramme([FromBody] CreateIdpProgrammeRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpProgrammeResponse>(false, null, "User not found"));

        var priorityExists = await _context.IdpDevelopmentPriorities.AnyAsync(item => item.Id == request.IdpDevelopmentPriorityId);
        if (!priorityExists) return NotFound(new ApiResponse<IdpProgrammeResponse>(false, null, "Development priority not found"));

        var entity = new IdpProgramme
        {
            IdpDevelopmentPriorityId = request.IdpDevelopmentPriorityId,
            ProgrammeCode = request.ProgrammeCode.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            ResponsibleDepartmentId = request.ResponsibleDepartmentId,
            PlannedBudget = request.PlannedBudget,
            ApprovedBudget = request.ApprovedBudget,
            ActualExpenditure = request.ActualExpenditure
        };

        _context.IdpProgrammes.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpProgrammes.Include(item => item.ResponsibleDepartment).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpProgramme", entity.Id.ToString(), "Create", null, ToProgrammeResponse(entity));
        return Ok(new ApiResponse<IdpProgrammeResponse>(true, ToProgrammeResponse(entity)));
    }

    [HttpPost("projects")]
    [Authorize(Policy = "Permission:IDP.Project.Manage")]
    public async Task<ActionResult<ApiResponse<IdpProjectResponse>>> CreateProject([FromBody] CreateIdpProjectRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpProjectResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.Status, out IdpProjectStatus status))
        {
            return BadRequest(new ApiResponse<IdpProjectResponse>(false, null, "Invalid project status"));
        }

        var programmeExists = await _context.IdpProgrammes.AnyAsync(item => item.Id == request.IdpProgrammeId);
        if (!programmeExists) return NotFound(new ApiResponse<IdpProjectResponse>(false, null, "Programme not found"));

        var entity = new IdpProject
        {
            IdpProgrammeId = request.IdpProgrammeId,
            ProjectCode = request.ProjectCode.Trim(),
            ProjectName = request.ProjectName.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            DepartmentId = request.DepartmentId,
            Budget = request.Budget,
            FundingSource = request.FundingSource.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = status,
            CommunityNeedReference = request.CommunityNeedReference
        };

        _context.IdpProjects.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpProjects.Include(item => item.Department).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpProject", entity.Id.ToString(), "Create", null, ToProjectResponse(entity));
        return Ok(new ApiResponse<IdpProjectResponse>(true, ToProjectResponse(entity)));
    }

    [HttpPost("kpis")]
    [Authorize(Policy = "Permission:IDP.Kpi.Manage")]
    public async Task<ActionResult<ApiResponse<IdpKpiResponse>>> CreateKpi([FromBody] CreateIdpKpiRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpKpiResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.IndicatorType, out IdpKpiIndicatorType indicatorType))
        {
            return BadRequest(new ApiResponse<IdpKpiResponse>(false, null, "Invalid KPI indicator type"));
        }

        var projectExists = await _context.IdpProjects.AnyAsync(item => item.Id == request.IdpProjectId);
        if (!projectExists) return NotFound(new ApiResponse<IdpKpiResponse>(false, null, "Project not found"));

        var entity = new IdpKpi
        {
            IdpProjectId = request.IdpProjectId,
            KpiCode = request.KpiCode.Trim(),
            KpiName = request.KpiName.Trim(),
            Description = request.Description.Trim(),
            Formula = request.Formula.Trim(),
            Baseline = request.Baseline,
            AnnualTarget = request.AnnualTarget,
            FiveYearTarget = request.FiveYearTarget,
            ResponsibleDepartmentId = request.ResponsibleDepartmentId,
            DataSource = request.DataSource.Trim(),
            ReportingFrequency = request.ReportingFrequency.Trim(),
            IndicatorType = indicatorType,
            Circular88Linked = request.Circular88Linked,
            TreasuryTidLinked = request.TreasuryTidLinked
        };

        _context.IdpKpis.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpKpis.Include(item => item.ResponsibleDepartment).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpKpi", entity.Id.ToString(), "Create", null, ToKpiResponse(entity));
        return Ok(new ApiResponse<IdpKpiResponse>(true, ToKpiResponse(entity)));
    }

    [HttpPost("annual-targets")]
    [Authorize(Policy = "Permission:IDP.Kpi.Manage")]
    public async Task<ActionResult<ApiResponse<IdpAnnualTargetResponse>>> CreateAnnualTarget([FromBody] CreateIdpAnnualTargetRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpAnnualTargetResponse>(false, null, "User not found"));

        var kpiExists = await _context.IdpKpis.AnyAsync(item => item.Id == request.IdpKpiId);
        if (!kpiExists) return NotFound(new ApiResponse<IdpAnnualTargetResponse>(false, null, "KPI not found"));

        var entity = new IdpAnnualTarget
        {
            IdpKpiId = request.IdpKpiId,
            FinancialYear = request.FinancialYear,
            TargetValue = request.TargetValue,
            ActualValue = request.ActualValue,
            ProgressComment = request.ProgressComment
        };

        _context.IdpAnnualTargets.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpAnnualTarget", entity.Id.ToString(), "Create", null, ToAnnualTargetResponse(entity));

        return Ok(new ApiResponse<IdpAnnualTargetResponse>(true, ToAnnualTargetResponse(entity)));
    }

    [HttpPost("alignment-links")]
    [Authorize(Policy = "Permission:IDP.Alignment.Manage")]
    public async Task<ActionResult<ApiResponse<IdpAlignmentLinkResponse>>> CreateAlignmentLink([FromBody] CreateIdpAlignmentLinkRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpAlignmentLinkResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.FrameworkType, out AlignmentFrameworkType frameworkType))
        {
            return BadRequest(new ApiResponse<IdpAlignmentLinkResponse>(false, null, "Invalid framework type"));
        }

        var objectiveExists = await _context.IdpStrategicObjectives.AnyAsync(item => item.Id == request.IdpStrategicObjectiveId);
        if (!objectiveExists) return NotFound(new ApiResponse<IdpAlignmentLinkResponse>(false, null, "Strategic objective not found"));

        var entity = new IdpAlignmentLink
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            FrameworkType = frameworkType,
            FrameworkReferenceCode = request.FrameworkReferenceCode.Trim(),
            FrameworkReferenceTitle = request.FrameworkReferenceTitle.Trim(),
            Notes = request.Notes
        };

        _context.IdpAlignmentLinks.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpAlignmentLink", entity.Id.ToString(), "Create", null, ToAlignmentResponse(entity));

        return Ok(new ApiResponse<IdpAlignmentLinkResponse>(true, ToAlignmentResponse(entity)));
    }

    [HttpPost("community-sessions")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpCommunitySessionResponse>>> CreateCommunitySession([FromBody] CreateIdpCommunitySessionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpCommunitySessionResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.ParticipationType, out IdpParticipationType participationType))
        {
            return BadRequest(new ApiResponse<IdpCommunitySessionResponse>(false, null, "Invalid participation type"));
        }

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpCommunitySessionResponse>(false, null, "IDP plan not found"));

        var entity = new IdpCommunitySession
        {
            IdpPlanId = request.IdpPlanId,
            ParticipationType = participationType,
            SessionDate = request.SessionDate,
            Venue = request.Venue.Trim(),
            WardId = request.WardId,
            ParticipantsCount = request.ParticipantsCount,
            AttendanceRegisterPath = request.AttendanceRegisterPath,
            MinutesPath = request.MinutesPath
        };

        _context.IdpCommunitySessions.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.IdpCommunitySessions.Include(item => item.Ward).FirstAsync(item => item.Id == entity.Id);
        await WriteIdpAudit(user.Id, "IdpCommunitySession", entity.Id.ToString(), "Create", null, ToCommunitySessionResponse(entity));

        return Ok(new ApiResponse<IdpCommunitySessionResponse>(true, ToCommunitySessionResponse(entity)));
    }

    [HttpPost("community-needs")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpCommunityNeedResponse>>> CreateCommunityNeed([FromBody] CreateIdpCommunityNeedRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpCommunityNeedResponse>(false, null, "User not found"));

        var sessionExists = await _context.IdpCommunitySessions.AnyAsync(item => item.Id == request.IdpCommunitySessionId);
        if (!sessionExists) return NotFound(new ApiResponse<IdpCommunityNeedResponse>(false, null, "Community session not found"));

        var entity = new IdpCommunityNeed
        {
            IdpCommunitySessionId = request.IdpCommunitySessionId,
            IssueCategory = request.IssueCategory.Trim(),
            Description = request.Description.Trim(),
            PriorityLevel = request.PriorityLevel.Trim(),
            ProposedIntervention = request.ProposedIntervention
        };

        _context.IdpCommunityNeeds.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpCommunityNeed", entity.Id.ToString(), "Create", null, ToCommunityNeedResponse(entity));

        return Ok(new ApiResponse<IdpCommunityNeedResponse>(true, ToCommunityNeedResponse(entity)));
    }

    [HttpPost("ward-inputs")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpWardInputResponse>>> CreateWardInput([FromBody] CreateIdpWardInputRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpWardInputResponse>(false, null, "User not found"));

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpWardInputResponse>(false, null, "IDP plan not found"));

        var ward = await _context.Wards.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.WardId);
        if (ward == null) return NotFound(new ApiResponse<IdpWardInputResponse>(false, null, "Ward not found"));

        var entity = new IdpWardInput
        {
            IdpPlanId = request.IdpPlanId,
            WardId = request.WardId,
            WardPlanSummary = request.WardPlanSummary.Trim(),
            WardPriorities = request.WardPriorities.Trim(),
            WardProjects = request.WardProjects.Trim()
        };

        _context.IdpWardInputs.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpWardInput", entity.Id.ToString(), "Create", null, ToWardInputResponse(entity, ward.Name));

        return Ok(new ApiResponse<IdpWardInputResponse>(true, ToWardInputResponse(entity, ward.Name)));
    }

    [HttpPost("stakeholder-engagements")]
    [Authorize(Policy = "Permission:IDP.Participation.Manage")]
    public async Task<ActionResult<ApiResponse<IdpStakeholderEngagementResponse>>> CreateStakeholderEngagement([FromBody] CreateIdpStakeholderEngagementRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpStakeholderEngagementResponse>(false, null, "User not found"));

        var sessionExists = await _context.IdpCommunitySessions.AnyAsync(item => item.Id == request.IdpCommunitySessionId);
        if (!sessionExists) return NotFound(new ApiResponse<IdpStakeholderEngagementResponse>(false, null, "Community session not found"));

        var entity = new IdpStakeholderEngagement
        {
            IdpCommunitySessionId = request.IdpCommunitySessionId,
            StakeholderType = request.StakeholderType.Trim(),
            StakeholderName = request.StakeholderName.Trim(),
            ContactPerson = request.ContactPerson,
            ContactEmail = request.ContactEmail,
            KeyInput = request.KeyInput
        };

        _context.IdpStakeholderEngagements.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpStakeholderEngagement", entity.Id.ToString(), "Create", null, ToStakeholderResponse(entity));

        return Ok(new ApiResponse<IdpStakeholderEngagementResponse>(true, ToStakeholderResponse(entity)));
    }

    [HttpPost("risk-links")]
    [Authorize(Policy = "Permission:IDP.Risk.Manage")]
    public async Task<ActionResult<ApiResponse<IdpRiskLinkResponse>>> CreateRiskLink([FromBody] CreateIdpRiskLinkRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpRiskLinkResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.RiskLevel, out IdpRiskLevel riskLevel))
        {
            return BadRequest(new ApiResponse<IdpRiskLinkResponse>(false, null, "Invalid risk level"));
        }

        var entity = new IdpRiskLink
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            IdpProjectId = request.IdpProjectId,
            IdpKpiId = request.IdpKpiId,
            RiskReference = request.RiskReference.Trim(),
            RiskTitle = request.RiskTitle.Trim(),
            MitigationPlan = request.MitigationPlan,
            RiskLevel = riskLevel
        };

        _context.IdpRiskLinks.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpRiskLink", entity.Id.ToString(), "Create", null, ToRiskResponse(entity));

        return Ok(new ApiResponse<IdpRiskLinkResponse>(true, ToRiskResponse(entity)));
    }

    [HttpPost("budget-snapshots")]
    [Authorize(Policy = "Permission:IDP.Budget.View")]
    public async Task<ActionResult<ApiResponse<IdpBudgetSnapshotResponse>>> CreateBudgetSnapshot([FromBody] CreateIdpBudgetSnapshotRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpBudgetSnapshotResponse>(false, null, "User not found"));

        var entity = new IdpBudgetSnapshot
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            IdpProjectId = request.IdpProjectId,
            FinancialYear = request.FinancialYear,
            PlannedBudget = request.PlannedBudget,
            ApprovedBudget = request.ApprovedBudget,
            ActualExpenditure = request.ActualExpenditure,
            SourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "FMS" : request.SourceSystem.Trim(),
            CapturedAt = DateTime.UtcNow
        };

        _context.IdpBudgetSnapshots.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpBudgetSnapshot", entity.Id.ToString(), "Create", null, ToBudgetSnapshotResponse(entity));

        return Ok(new ApiResponse<IdpBudgetSnapshotResponse>(true, ToBudgetSnapshotResponse(entity)));
    }

    [HttpPost("documents")]
    [Authorize(Policy = "Permission:IDP.Documents.Manage")]
    public async Task<ActionResult<ApiResponse<IdpDocumentResponse>>> CreateDocument([FromBody] CreateIdpDocumentRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpDocumentResponse>(false, null, "User not found"));

        if (!TryParseEnum(request.Category, out IdpDocumentCategory category))
        {
            return BadRequest(new ApiResponse<IdpDocumentResponse>(false, null, "Invalid document category"));
        }

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpDocumentResponse>(false, null, "IDP plan not found"));

        var entity = new IdpDocument
        {
            IdpPlanId = request.IdpPlanId,
            IdpPlanVersionId = request.IdpPlanVersionId,
            Category = category,
            Title = request.Title.Trim(),
            FileName = request.FileName.Trim(),
            StoragePath = request.StoragePath.Trim(),
            ContentType = request.ContentType,
            SizeInBytes = request.SizeInBytes,
            VersionNumber = request.VersionNumber,
            IsApproved = request.IsApproved,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = user.Id
        };

        _context.IdpDocuments.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpDocument", entity.Id.ToString(), "Create", null, ToDocumentResponse(entity, user.FullName));

        return Ok(new ApiResponse<IdpDocumentResponse>(true, ToDocumentResponse(entity, user.FullName)));
    }

    [HttpPost("comments")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public async Task<ActionResult<ApiResponse<IdpCommentResponse>>> CreateComment([FromBody] CreateIdpCommentRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpCommentResponse>(false, null, "User not found"));

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpCommentResponse>(false, null, "IDP plan not found"));

        var entity = new IdpCollaborationComment
        {
            IdpPlanId = request.IdpPlanId,
            IdpPlanVersionId = request.IdpPlanVersionId,
            EntityName = request.EntityName.Trim(),
            EntityId = request.EntityId.Trim(),
            Comment = request.Comment.Trim(),
            CommentedByUserId = user.Id,
            CommentedAt = DateTime.UtcNow
        };

        _context.IdpCollaborationComments.Add(entity);
        await _context.SaveChangesAsync();
        await WriteIdpAudit(user.Id, "IdpComment", entity.Id.ToString(), "Create", null, ToCommentResponse(entity, user.FullName));

        return Ok(new ApiResponse<IdpCommentResponse>(true, ToCommentResponse(entity, user.FullName)));
    }

    [HttpPost("tasks")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public async Task<ActionResult<ApiResponse<IdpTaskResponse>>> CreateTask([FromBody] CreateIdpTaskRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpTaskResponse>(false, null, "User not found"));

        var planExists = await _context.IdpPlans.AnyAsync(item => item.Id == request.IdpPlanId);
        if (!planExists) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "IDP plan not found"));

        var assignee = await _userManager.FindByIdAsync(request.AssignedToUserId);
        if (assignee == null) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "Assignee not found"));

        var entity = new IdpTaskAssignment
        {
            IdpPlanId = request.IdpPlanId,
            IdpPlanVersionId = request.IdpPlanVersionId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = user.Id,
            DueDate = request.DueDate,
            IsCompleted = false
        };

        _context.IdpTaskAssignments.Add(entity);
        await _context.SaveChangesAsync();

        await _workflowGovernanceService.CreateNotificationAsync(
            assignee.Id,
            NotificationType.Submission,
            "IDP task assigned",
            $"You were assigned IDP task '{entity.Title}'.",
            "IdpTask",
            entity.Id.ToString());

        await WriteIdpAudit(user.Id, "IdpTask", entity.Id.ToString(), "Create", null, ToTaskResponse(entity, assignee.FullName, user.FullName));
        return Ok(new ApiResponse<IdpTaskResponse>(true, ToTaskResponse(entity, assignee.FullName, user.FullName)));
    }

    [HttpPatch("tasks/{id:long}/complete")]
    [Authorize(Policy = "Permission:IDP.Collaboration.Manage")]
    public async Task<ActionResult<ApiResponse<IdpTaskResponse>>> CompleteTask(long id, [FromBody] CompleteIdpTaskRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<IdpTaskResponse>(false, null, "User not found"));

        var entity = await _context.IdpTaskAssignments.FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<IdpTaskResponse>(false, null, "Task not found"));

        entity.IsCompleted = request.IsCompleted;
        entity.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();

        var assignee = await _userManager.FindByIdAsync(entity.AssignedToUserId);
        var assigner = await _userManager.FindByIdAsync(entity.AssignedByUserId);

        await WriteIdpAudit(user.Id, "IdpTask", entity.Id.ToString(), "Complete", null, ToTaskResponse(entity, assignee?.FullName, assigner?.FullName));
        return Ok(new ApiResponse<IdpTaskResponse>(true, ToTaskResponse(entity, assignee?.FullName, assigner?.FullName)));
    }

    private async Task<IdpDevelopmentPriority> CreateEntity(CreateIdpDevelopmentPriorityRequest request)
    {
        var entity = new IdpDevelopmentPriority
        {
            IdpStrategicObjectiveId = request.IdpStrategicObjectiveId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            SortOrder = request.SortOrder
        };

        _context.IdpDevelopmentPriorities.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    private async Task<IdpAlignmentMatrixItemResponse[]> BuildAlignmentMatrixAsync(int planId)
    {
        return await _context.IdpAlignmentLinks
            .AsNoTracking()
            .Include(item => item.IdpStrategicObjective)
            .ThenInclude(item => item.IdpStrategicOutcome)
            .Where(item => item.IdpStrategicObjective.IdpStrategicOutcome.IdpPlanId == planId)
            .OrderBy(item => item.IdpStrategicObjective.IdpStrategicOutcome.SortOrder)
            .ThenBy(item => item.IdpStrategicObjective.SortOrder)
            .Select(item => new IdpAlignmentMatrixItemResponse(
                item.IdpStrategicObjective.IdpStrategicOutcome.Code,
                item.IdpStrategicObjective.IdpStrategicOutcome.Name,
                item.IdpStrategicObjective.Code,
                item.IdpStrategicObjective.Name,
                item.FrameworkType.ToString(),
                item.FrameworkReferenceCode,
                item.FrameworkReferenceTitle))
            .ToArrayAsync();
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private Task WriteIdpAudit(string changedBy, string entityName, string entityId, string action, object? before, object? after)
    {
        return _workflowGovernanceService.WriteAuditTrailAsync(entityName, entityId, action, before, after, changedBy, PerformanceApiSupport.GetIpAddress(HttpContext));
    }

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed) where TEnum : struct, Enum
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);

        foreach (var name in Enum.GetNames(typeof(TEnum)))
        {
            var candidate = name.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
            {
                parsed = Enum.Parse<TEnum>(name, true);
                return true;
            }
        }

        parsed = default;
        return false;
    }

    private static IdpPlanSummaryResponse ToSummaryResponse(IdpPlan plan) =>
        new(plan.Id, plan.MunicipalityName, plan.PlanTitle, plan.PlanCode, plan.StartFinancialYear, plan.EndFinancialYear, plan.Status.ToString(), plan.CurrentVersionNumber, plan.CreatedAt, plan.ApprovedAt);

    private static IdpPlanVersionResponse ToVersionResponse(IdpPlanVersion version) =>
        new(version.Id, version.IdpPlanId, version.VersionNumber, version.VersionType.ToString(), version.VersionLabel, version.ReviewYear, version.SummaryOfChanges, version.IsActive, version.CreatedAt, version.CreatedByUserId);

    private static IdpStrategicOutcomeResponse ToOutcomeResponse(IdpStrategicOutcome outcome) =>
        new(outcome.Id, outcome.IdpPlanId, outcome.Code, outcome.Name, outcome.Description, outcome.SortOrder);

    private static IdpStrategicObjectiveResponse ToObjectiveResponse(IdpStrategicObjective objective) =>
        new(
            objective.Id,
            objective.IdpStrategicOutcomeId,
            objective.Code,
            objective.Name,
            objective.Description,
            objective.BaselineValue,
            objective.TargetValue,
            objective.ResponsibleDepartmentId,
            objective.ResponsibleDepartment?.Name,
            objective.StrategicOwnerUserId,
            objective.StrategicOwnerUser?.FullName,
            objective.StartDate,
            objective.EndDate,
            objective.BudgetAllocation,
            objective.SortOrder);

    private static IdpDevelopmentPriorityResponse ToPriorityResponse(IdpDevelopmentPriority priority) =>
        new(priority.Id, priority.IdpStrategicObjectiveId, priority.Name, priority.Description, priority.SortOrder);

    private static IdpProgrammeResponse ToProgrammeResponse(IdpProgramme programme) =>
        new(
            programme.Id,
            programme.IdpDevelopmentPriorityId,
            programme.ProgrammeCode,
            programme.Name,
            programme.Description,
            programme.ResponsibleDepartmentId,
            programme.ResponsibleDepartment?.Name,
            programme.PlannedBudget,
            programme.ApprovedBudget,
            programme.ActualExpenditure);

    private static IdpProjectResponse ToProjectResponse(IdpProject project) =>
        new(
            project.Id,
            project.IdpProgrammeId,
            project.ProjectCode,
            project.ProjectName,
            project.Description,
            project.Category,
            project.DepartmentId,
            project.Department?.Name,
            project.Budget,
            project.FundingSource,
            project.StartDate,
            project.EndDate,
            project.Status.ToString(),
            project.CommunityNeedReference);

    private static IdpKpiResponse ToKpiResponse(IdpKpi kpi) =>
        new(
            kpi.Id,
            kpi.IdpProjectId,
            kpi.KpiCode,
            kpi.KpiName,
            kpi.Description,
            kpi.Formula,
            kpi.Baseline,
            kpi.AnnualTarget,
            kpi.FiveYearTarget,
            kpi.ResponsibleDepartmentId,
            kpi.ResponsibleDepartment?.Name,
            kpi.DataSource,
            kpi.ReportingFrequency,
            kpi.IndicatorType.ToString(),
            kpi.Circular88Linked,
            kpi.TreasuryTidLinked);

    private static IdpAnnualTargetResponse ToAnnualTargetResponse(IdpAnnualTarget annualTarget) =>
        new(annualTarget.Id, annualTarget.IdpKpiId, annualTarget.FinancialYear, annualTarget.TargetValue, annualTarget.ActualValue, annualTarget.ProgressComment);

    private static IdpAlignmentLinkResponse ToAlignmentResponse(IdpAlignmentLink link) =>
        new(link.Id, link.IdpStrategicObjectiveId, link.FrameworkType.ToString(), link.FrameworkReferenceCode, link.FrameworkReferenceTitle, link.Notes);

    private static IdpCommunitySessionResponse ToCommunitySessionResponse(IdpCommunitySession session) =>
        new(session.Id, session.IdpPlanId, session.ParticipationType.ToString(), session.SessionDate, session.Venue, session.WardId, session.Ward?.Name, session.ParticipantsCount, session.AttendanceRegisterPath, session.MinutesPath);

    private static IdpCommunityNeedResponse ToCommunityNeedResponse(IdpCommunityNeed need) =>
        new(need.Id, need.IdpCommunitySessionId, need.IssueCategory, need.Description, need.PriorityLevel, need.ProposedIntervention);

    private static IdpWardInputResponse ToWardInputResponse(IdpWardInput wardInput, string wardName) =>
        new(wardInput.Id, wardInput.IdpPlanId, wardInput.WardId, wardName, wardInput.WardPlanSummary, wardInput.WardPriorities, wardInput.WardProjects);

    private static IdpStakeholderEngagementResponse ToStakeholderResponse(IdpStakeholderEngagement stakeholder) =>
        new(stakeholder.Id, stakeholder.IdpCommunitySessionId, stakeholder.StakeholderType, stakeholder.StakeholderName, stakeholder.ContactPerson, stakeholder.ContactEmail, stakeholder.KeyInput);

    private static IdpRiskLinkResponse ToRiskResponse(IdpRiskLink risk) =>
        new(risk.Id, risk.IdpStrategicObjectiveId, risk.IdpProjectId, risk.IdpKpiId, risk.RiskReference, risk.RiskTitle, risk.MitigationPlan, risk.RiskLevel.ToString());

    private static IdpBudgetSnapshotResponse ToBudgetSnapshotResponse(IdpBudgetSnapshot snapshot) =>
        new(snapshot.Id, snapshot.IdpStrategicObjectiveId, snapshot.IdpProjectId, snapshot.FinancialYear, snapshot.PlannedBudget, snapshot.ApprovedBudget, snapshot.ActualExpenditure, snapshot.SourceSystem, snapshot.CapturedAt);

    private static IdpDocumentResponse ToDocumentResponse(IdpDocument document, string? uploadedByName) =>
        new(document.Id, document.IdpPlanId, document.IdpPlanVersionId, document.Category.ToString(), document.Title, document.FileName, document.StoragePath, document.ContentType, document.SizeInBytes, document.VersionNumber, document.IsApproved, document.UploadedAt, document.UploadedByUserId, uploadedByName);

    private static IdpCommentResponse ToCommentResponse(IdpCollaborationComment comment, string? commentedByName) =>
        new(comment.Id, comment.IdpPlanId, comment.IdpPlanVersionId, comment.EntityName, comment.EntityId, comment.Comment, comment.CommentedByUserId, commentedByName, comment.CommentedAt);

    private static IdpTaskResponse ToTaskResponse(IdpTaskAssignment task, string? assignedToName, string? assignedByName) =>
        new(task.Id, task.IdpPlanId, task.IdpPlanVersionId, task.Title, task.Description, task.AssignedToUserId, assignedToName, task.AssignedByUserId, assignedByName, task.DueDate, task.IsCompleted, task.CompletedAt);
}

using System.Globalization;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Security;

public sealed record SecurityScopeIdentityResolution(
    int? DepartmentId,
    int? UnitId,
    string? TargetId,
    string? KpiId,
    string? ProjectId,
    string? TaskId,
    string? Error)
{
    public bool Succeeded => Error == null;
}

public sealed record SecurityScopePublicIdentityMaps(
    IReadOnlyDictionary<string, Guid> Targets,
    IReadOnlyDictionary<string, Guid> Kpis,
    IReadOnlyDictionary<string, Guid> Projects,
    IReadOnlyDictionary<string, Guid> Tasks);

public static class SecurityScopeIdentityResolver
{
    public static async Task<SecurityScopePublicIdentityMaps> ResolvePublicIdsAsync(
        ApplicationDbContext context,
        long municipalityId,
        IEnumerable<string?> targetIds,
        IEnumerable<string?> kpiIds,
        IEnumerable<string?> projectIds,
        IEnumerable<string?> taskIds)
    {
        var targetKeys = Keys(targetIds);
        var opmsTargetRows = targetKeys.Length == 0
            ? []
            : await context.OpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && targetKeys.Contains(item.Id))
                .Select(item => new InternalPublicIdentity(item.Id, item.PublicId))
                .ToArrayAsync();
        var ipmsTargetRows = targetKeys.Length == 0
            ? []
            : await context.IpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && targetKeys.Contains(item.Id))
                .Select(item => new InternalPublicIdentity(item.Id, item.PublicId))
                .ToArrayAsync();
        var targets = UniqueMap(opmsTargetRows.Concat(ipmsTargetRows));

        var kpiKeys = Keys(kpiIds);
        var opmsKpis = kpiKeys.Length == 0
            ? []
            : await context.OpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && kpiKeys.Contains(item.Id))
                .Select(item => new InternalPublicIdentity(item.Id, item.PublicId))
                .ToArrayAsync();
        var ipmsKpis = kpiKeys.Length == 0
            ? []
            : await context.IpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && kpiKeys.Contains(item.Id))
                .Select(item => new InternalPublicIdentity(item.Id, item.PublicId))
                .ToArrayAsync();
        var idpKpiIds = kpiKeys.Select(item => int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? (int?)id : null)
            .Where(item => item.HasValue).Select(item => item!.Value).Distinct().ToArray();
        var idpKpis = idpKpiIds.Length == 0
            ? []
            : await context.IdpKpis.AsNoTracking()
                .Where(item => idpKpiIds.Contains(item.Id)
                    && item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => new { item.Id, item.PublicId })
                .ToArrayAsync();
        var kpis = UniqueMap(opmsKpis.Concat(ipmsKpis).Concat(idpKpis.Select(item =>
            new InternalPublicIdentity(item.Id.ToString(CultureInfo.InvariantCulture), item.PublicId))));

        var projectKeys = Keys(projectIds);
        var internalProjectIds = projectKeys.Select(item => int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? (int?)id : null)
            .Where(item => item.HasValue).Select(item => item!.Value).Distinct().ToArray();
        var projects = internalProjectIds.Length == 0
            ? new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            : (await context.IdpProjects.AsNoTracking()
                .Where(item => internalProjectIds.Contains(item.Id)
                    && item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => new { item.Id, item.PublicId })
                .ToArrayAsync())
                .ToDictionary(item => item.Id.ToString(CultureInfo.InvariantCulture), item => item.PublicId, StringComparer.OrdinalIgnoreCase);

        var taskKeys = Keys(taskIds);
        var internalTaskIds = taskKeys.Select(item => long.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? (long?)id : null)
            .Where(item => item.HasValue).Select(item => item!.Value).Distinct().ToArray();
        var tasks = internalTaskIds.Length == 0
            ? new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            : (await context.IdpTaskAssignments.AsNoTracking()
                .Where(item => internalTaskIds.Contains(item.Id) && item.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => new { item.Id, item.PublicId })
                .ToArrayAsync())
                .ToDictionary(item => item.Id.ToString(CultureInfo.InvariantCulture), item => item.PublicId, StringComparer.OrdinalIgnoreCase);

        return new(targets, kpis, projects, tasks);
    }

    public static async Task<SecurityScopeIdentityResolution> ResolveAsync(
        ApplicationDbContext context,
        long municipalityId,
        Guid? departmentPublicId,
        Guid? unitPublicId,
        Guid? targetPublicId,
        Guid? kpiPublicId,
        Guid? projectPublicId,
        Guid? taskPublicId)
    {
        int? departmentId = null;
        if (departmentPublicId.HasValue)
        {
            departmentId = await context.Departments.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && item.PublicId == departmentPublicId.Value && item.IsActive)
                .Select(item => (int?)item.Id)
                .SingleOrDefaultAsync();
            if (!departmentId.HasValue)
                return Failed("Department public identifier was not found or active in the selected municipality.");
        }

        int? unitId = null;
        if (unitPublicId.HasValue)
        {
            var unit = await context.Units.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && item.PublicId == unitPublicId.Value && item.IsActive)
                .Select(item => new { item.Id, item.DepartmentId })
                .SingleOrDefaultAsync();
            if (unit == null)
                return Failed("Unit public identifier was not found or active in the selected municipality.");
            if (departmentId.HasValue && unit.DepartmentId != departmentId.Value)
                return Failed("Unit does not belong to the selected department.");
            unitId = unit.Id;
            departmentId ??= unit.DepartmentId;
        }

        var targetId = await ResolvePerformanceTargetAsync(context, municipalityId, targetPublicId, "Target");
        if (targetId.Error != null) return Failed(targetId.Error);

        string? kpiId = null;
        if (kpiPublicId.HasValue)
        {
            var performanceCandidates = await ResolvePerformanceCandidatesAsync(context, municipalityId, kpiPublicId.Value);
            var idpCandidates = await context.IdpKpis.AsNoTracking()
                .Where(item => item.PublicId == kpiPublicId.Value
                    && item.IdpProject.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            var candidates = performanceCandidates
                .Concat(idpCandidates.Select(item => item.ToString(CultureInfo.InvariantCulture)))
                .Take(2)
                .ToArray();
            if (candidates.Length != 1)
                return Failed("KPI public identifier was not found uniquely in the selected municipality.");
            kpiId = candidates[0];
        }

        string? projectId = null;
        if (projectPublicId.HasValue)
        {
            var ids = await context.IdpProjects.AsNoTracking()
                .Where(item => item.PublicId == projectPublicId.Value
                    && item.IdpProgramme.IdpDevelopmentPriority.IdpStrategicObjective.IdpStrategicOutcome.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            if (ids.Length != 1)
                return Failed("Project public identifier was not found uniquely in the selected municipality.");
            projectId = ids[0].ToString(CultureInfo.InvariantCulture);
        }

        string? taskId = null;
        if (taskPublicId.HasValue)
        {
            var ids = await context.IdpTaskAssignments.AsNoTracking()
                .Where(item => item.PublicId == taskPublicId.Value && item.IdpPlan.MunicipalityId == municipalityId)
                .Select(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            if (ids.Length != 1)
                return Failed("Task public identifier was not found uniquely in the selected municipality.");
            taskId = ids[0].ToString(CultureInfo.InvariantCulture);
        }

        return new(departmentId, unitId, targetId.Value, kpiId, projectId, taskId, null);
    }

    private static async Task<(string? Value, string? Error)> ResolvePerformanceTargetAsync(
        ApplicationDbContext context, long municipalityId, Guid? publicId, string label)
    {
        if (!publicId.HasValue) return (null, null);
        var candidates = await ResolvePerformanceCandidatesAsync(context, municipalityId, publicId.Value);
        return candidates.Length == 1
            ? (candidates[0], null)
            : (null, $"{label} public identifier was not found uniquely in the selected municipality.");
    }

    private static Task<string[]> ResolvePerformanceCandidatesAsync(ApplicationDbContext context, long municipalityId, Guid publicId) =>
        context.OpmsTargets.AsNoTracking()
            .Where(item => item.MunicipalityId == municipalityId && item.PublicId == publicId)
            .Select(item => item.Id)
            .Concat(context.IpmsTargets.AsNoTracking()
                .Where(item => item.MunicipalityId == municipalityId && item.PublicId == publicId)
                .Select(item => item.Id))
            .Take(2)
            .ToArrayAsync();

    private static SecurityScopeIdentityResolution Failed(string error) => new(null, null, null, null, null, null, error);

    private static string[] Keys(IEnumerable<string?> values) => values
        .Where(item => !string.IsNullOrWhiteSpace(item))
        .Select(item => item!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static Dictionary<string, Guid> UniqueMap(IEnumerable<InternalPublicIdentity> rows) => rows
        .GroupBy(item => item.InternalId, StringComparer.OrdinalIgnoreCase)
        .Where(group => group.Select(item => item.PublicId).Distinct().Count() == 1)
        .ToDictionary(group => group.Key, group => group.First().PublicId, StringComparer.OrdinalIgnoreCase);

    private sealed record InternalPublicIdentity(string InternalId, Guid PublicId);
}

using System.Security.Claims;
using System.Text.Json;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Security;

public sealed class OfficialReportJobWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OfficialReportJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = Math.Clamp(configuration.GetValue("OfficialReports:JobPollSeconds", 15), 2, 300);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        do
        {
            try { await ProcessBatch(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Official report job batch failed"); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> ProcessBatch(CancellationToken cancellationToken)
    {
        await MaterializeDueSchedules(cancellationToken);
        Guid[] publicIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stale = DateTime.UtcNow.AddMinutes(-15);
            var abandoned = await context.OfficialReportJobs.IgnoreQueryFilters()
                .Where(item => item.State == OfficialReportJobState.Processing && item.StartedAt < stale && item.AttemptCount < 5).Take(25).ToArrayAsync(cancellationToken);
            foreach (var item in abandoned) { item.State = OfficialReportJobState.RetryPending; item.AvailableAt = DateTime.UtcNow; item.LastError = "Recovered after an interrupted worker lease."; }
            if (abandoned.Length > 0) await context.SaveChangesAsync(cancellationToken);
            publicIds = await context.OfficialReportJobs.IgnoreQueryFilters().AsNoTracking()
                .Where(item => (item.State == OfficialReportJobState.Queued || item.State == OfficialReportJobState.RetryPending) && item.AvailableAt <= DateTime.UtcNow && item.AttemptCount < 5)
                .OrderBy(item => item.AvailableAt).ThenBy(item => item.RequestedAt).Select(item => item.PublicId).Take(10).ToArrayAsync(cancellationToken);
        }
        var processed = 0;
        foreach (var publicId in publicIds)
        {
            if (await ProcessJob(publicId, cancellationToken)) processed++;
        }
        return processed;
    }

    private async Task MaterializeDueSchedules(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var schedules = await context.OfficialReportSchedules.IgnoreQueryFilters()
            .Include(item => item.ReportTemplate).Include(item => item.MunicipalityFinancialYear).Include(item => item.ReportingPeriod)
            .Include(item => item.Department).Include(item => item.Unit)
            .Where(item => item.IsCurrent && item.IsActive && item.NextRunAt != null && item.NextRunAt <= now)
            .OrderBy(item => item.NextRunAt).Take(50).ToArrayAsync(cancellationToken);
        foreach (var schedule in schedules)
        {
            var occurrence = schedule.NextRunAt!.Value;
            if (schedule.EffectiveTo.HasValue && occurrence > schedule.EffectiveTo.Value)
            {
                schedule.IsActive = false; schedule.NextRunAt = null;
                continue;
            }
            if (!await context.OfficialReportJobs.IgnoreQueryFilters().AnyAsync(item => item.OfficialReportScheduleId == schedule.Id && item.ScheduledFor == occurrence, cancellationToken))
            {
                var recipients = await ResolveRecipients(context, schedule, now, cancellationToken);
                context.OfficialReportJobs.Add(new OfficialReportJob
                {
                    MunicipalityId = schedule.MunicipalityId, OfficialReportScheduleId = schedule.Id, ReportTemplateId = schedule.ReportTemplateId,
                    MunicipalityFinancialYearId = schedule.MunicipalityFinancialYearId, ReportingPeriodId = schedule.ReportingPeriodId,
                    DepartmentId = schedule.DepartmentId, UnitId = schedule.UnitId, State = recipients.Length == 0 ? OfficialReportJobState.Failed : OfficialReportJobState.Queued,
                    ScheduledFor = occurrence, AvailableAt = now, RequestedByUserId = schedule.CreatedByUserId, RequestedAt = now,
                    RecipientUserIdsCsv = string.Join(',', recipients), ChannelsCsv = schedule.ChannelsCsv, IsMandatoryDistribution = schedule.IsMandatory,
                    LastError = recipients.Length == 0 ? "The schedule resolved no active tenant recipients at materialization time." : null,
                    CompletedAt = recipients.Length == 0 ? now : null
                });
                context.AuditTrails.Add(new AuditTrail
                {
                    MunicipalityId = schedule.MunicipalityId, EntityName = nameof(OfficialReportSchedule), EntityId = schedule.PublicId.ToString(), Action = "MaterializeJob",
                    NewValue = JsonSerializer.Serialize(new { ScheduledFor = occurrence, RecipientCount = recipients.Length }), ChangedBy = schedule.CreatedByUserId,
                    ChangedAt = now, Reason = recipients.Length == 0 ? "Scheduled execution failed closed because no active recipients resolved." : "Materialized approved report schedule."
                });
            }
            schedule.NextRunAt = NextOccurrence(schedule, occurrence);
            if (!schedule.NextRunAt.HasValue || schedule.EffectiveTo.HasValue && schedule.NextRunAt > schedule.EffectiveTo)
            {
                schedule.IsActive = false;
                schedule.NextRunAt = null;
            }
        }
        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> ProcessJob(Guid publicId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = null;
        var systemContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identity = await systemContext.OfficialReportJobs.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.PublicId == publicId).Select(item => new { item.MunicipalityId, item.RequestedByUserId }).SingleOrDefaultAsync(cancellationToken);
        if (identity == null) return false;

        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, identity.RequestedByUserId)], "official-report-worker"));
        http.Items[TenantResolutionMiddleware.MunicipalityItem] = identity.MunicipalityId;
        http.Items[TenantResolutionMiddleware.SystemItem] = false;
        http.TraceIdentifier = $"report-job-{publicId:N}";
        accessor.HttpContext = http;

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = await context.OfficialReportJobs
            .Include(item => item.ReportTemplate).Include(item => item.MunicipalityFinancialYear).Include(item => item.ReportingPeriod)
            .Include(item => item.Department).Include(item => item.Unit).Include(item => item.PreviousOfficialReportGeneration)
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);
        if (job == null || job.State is not (OfficialReportJobState.Queued or OfficialReportJobState.RetryPending) || job.AvailableAt > DateTime.UtcNow) return false;
        job.State = OfficialReportJobState.Processing;
        job.StartedAt = DateTime.UtcNow;
        job.AttemptCount++;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return false; }

        try
        {
            var controller = new OfficialReportsController(context,
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<IAccessControlService>(),
                scope.ServiceProvider.GetRequiredService<ITenantContext>(),
                scope.ServiceProvider.GetRequiredService<IWorkflowGovernanceService>(),
                scope.ServiceProvider.GetRequiredService<IEvidenceBlobStorage>())
            { ControllerContext = new ControllerContext { HttpContext = http } };
            var result = await controller.Generate(new GenerateOfficialReportRequest(
                job.ReportTemplate.PublicId, job.MunicipalityFinancialYear.PublicId, job.ReportingPeriod.PublicId,
                job.PreviousOfficialReportGeneration?.PublicId, job.Department?.PublicId, job.Unit?.PublicId));
            if (result.Result is ObjectResult objectResult && objectResult.StatusCode is >= 200 and < 300
                && objectResult.Value is ApiResponse<OfficialReportGenerationResponse> { Success: true, Data: not null } response)
            {
                var generation = await context.OfficialReportGenerations.SingleAsync(item => item.PublicId == response.Data.PublicId, cancellationToken);
                job.OfficialReportGenerationId = generation.Id;
                job.State = OfficialReportJobState.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.LastError = null;
                QueueDistribution(context, job, generation);
                context.AuditTrails.Add(Audit(job, "Complete", new { GenerationPublicId = generation.PublicId, generation.FileName }, "Asynchronous report generation completed."));
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            var status = (result.Result as ObjectResult)?.StatusCode ?? StatusCodes.Status500InternalServerError;
            var message = ((result.Result as ObjectResult)?.Value as IApiResponse)?.Message ?? "Official report generation did not return a successful result.";
            await FailOrRetry(context, job, status >= 500, message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Official report job {JobPublicId} failed", publicId);
            await FailOrRetry(context, job, true, ExceptionDetail(exception), cancellationToken);
        }
        return true;
    }

    private static void QueueDistribution(ApplicationDbContext context, OfficialReportJob job, OfficialReportGeneration generation)
    {
        var recipients = Split(job.RecipientUserIdsCsv);
        if (recipients.Length == 0) return;
        var title = $"Official report ready: {job.ReportTemplate.Name}";
        var message = $"{generation.FileName} is ready. Download it from the protected official report history. Generation {generation.PublicId}.";
        foreach (var recipient in recipients)
            context.Notifications.Add(new Notification { MunicipalityId = job.MunicipalityId, UserId = recipient, Type = NotificationType.ReportReady, Title = title, Message = message, EntityName = nameof(OfficialReportGeneration), EntityId = generation.PublicId.ToString() });
        var outbox = new BusinessEventOutbox
        {
            MunicipalityId = job.MunicipalityId, EventType = "Notification.ReportReady", AggregateType = nameof(OfficialReportGeneration), AggregateId = generation.PublicId.ToString(),
            Payload = JsonSerializer.Serialize(new { type = NotificationType.ReportReady, title, message, entityName = nameof(OfficialReportGeneration), entityId = generation.PublicId.ToString(), recipients, channels = Split(job.ChannelsCsv), mandatory = job.IsMandatoryDistribution }),
            CorrelationId = $"report-job-{job.PublicId:N}", OccurredAt = DateTime.UtcNow, AvailableAt = DateTime.UtcNow
        };
        context.BusinessEventOutbox.Add(outbox);
        job.DistributionOutbox = outbox;
    }

    private static async Task FailOrRetry(ApplicationDbContext context, OfficialReportJob job, bool retryable, string detail, CancellationToken cancellationToken)
    {
        var publicId = job.PublicId;
        context.ChangeTracker.Clear();
        job = await context.OfficialReportJobs.SingleAsync(item => item.PublicId == publicId, cancellationToken);
        job.LastError = Limit(detail);
        job.CompletedAt = DateTime.UtcNow;
        if (retryable && job.AttemptCount < 5)
        {
            job.State = OfficialReportJobState.RetryPending;
            job.AvailableAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, job.AttemptCount)));
        }
        else job.State = OfficialReportJobState.Failed;
        context.AuditTrails.Add(Audit(job, job.State == OfficialReportJobState.Failed ? "Fail" : "RetryPending", new { job.AttemptCount, job.AvailableAt, job.LastError }, retryable ? "Report job execution failed with a retryable error." : "Report job execution was denied or invalid."));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static AuditTrail Audit(OfficialReportJob job, string action, object value, string reason) => new()
    {
        MunicipalityId = job.MunicipalityId, EntityName = nameof(OfficialReportJob), EntityId = job.PublicId.ToString(), Action = action,
        NewValue = JsonSerializer.Serialize(value), ChangedBy = job.RequestedByUserId, ChangedAt = DateTime.UtcNow,
        CorrelationId = $"report-job-{job.PublicId:N}", Reason = reason
    };

    private static async Task<string[]> ResolveRecipients(ApplicationDbContext context, OfficialReportSchedule schedule, DateTime now, CancellationToken cancellationToken)
    {
        var values = Split(schedule.RecipientValuesCsv);
        if (schedule.RecipientKind == OfficialReportRecipientKind.User)
            return await context.Users.IgnoreQueryFilters().Where(item => item.MunicipalityId == schedule.MunicipalityId && item.IsActive && values.Contains(item.Id)).Select(item => item.Id).Distinct().ToArrayAsync(cancellationToken);
        return await context.SecurityUserRoleAssignments.IgnoreQueryFilters()
            .Where(item => item.MunicipalityId == schedule.MunicipalityId && item.IsActive && item.EffectiveFrom <= now && (!item.EffectiveTo.HasValue || item.EffectiveTo > now)
                && item.User.IsActive && values.Contains(item.Role.RoleCode))
            .Select(item => item.UserId).Distinct().ToArrayAsync(cancellationToken);
    }

    private static DateTime? NextOccurrence(OfficialReportSchedule schedule, DateTime current) => schedule.Cadence switch
    {
        OfficialReportScheduleCadence.Once => null,
        OfficialReportScheduleCadence.Daily => current.AddDays(schedule.Interval),
        OfficialReportScheduleCadence.Weekly => current.AddDays(schedule.Interval * 7),
        OfficialReportScheduleCadence.Monthly => current.AddMonths(schedule.Interval),
        _ => null
    };
    private static string[] Split(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static string Limit(string value) => value.Length > 2000 ? value[..2000] : value;
    private static string ExceptionDetail(Exception exception) => exception.InnerException == null ? exception.Message : $"{exception.Message} | {ExceptionDetail(exception.InnerException)}";
}

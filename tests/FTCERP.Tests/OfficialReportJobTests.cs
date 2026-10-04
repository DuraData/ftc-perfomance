using FluentAssertions;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.Application.Reporting;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FTCERP.Tests;

public sealed class OfficialReportJobTests
{
    [Fact]
    public async Task DirectQueueCall_IsDeniedWithoutUnderlyingResourceReadPermission()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var seeded = await SeedDueSchedule(options);
        var tenant = new FixedTenantContext(seeded.Schedule.MunicipalityId, seeded.User.Id);
        await using var context = new ApplicationDbContext(options, tenant);
        var schedule = await context.OfficialReportSchedules.Include(item => item.ReportTemplate).Include(item => item.MunicipalityFinancialYear).Include(item => item.ReportingPeriod).SingleAsync();
        var granted = new AccessQueryScopeResult(true, true, [], [], [], [], [], []);
        var denied = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), "OPMS_REPORT.GENERATE")).ReturnsAsync(granted);
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), "OPMS_KPI.READ")).ReturnsAsync(denied);
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), "OPMS_SUBMISSION.READ")).ReturnsAsync(granted);
        var controller = new OfficialReportJobsController(context, IdpTestFixture.CreateUserManagerMock(seeded.User).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(seeded.User.Id) } }
        };

        var result = await controller.Queue(new QueueOfficialReportJobRequest(schedule.ReportTemplate.PublicId, schedule.MunicipalityFinancialYear.PublicId, schedule.ReportingPeriod.PublicId));

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await context.OfficialReportJobs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ScheduleChange_CreatesRowVersionProtectedSuccessorWithoutRewritingPriorVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var seeded = await SeedDueSchedule(options);
        var tenant = new FixedTenantContext(seeded.Schedule.MunicipalityId, seeded.User.Id);
        await using var context = new ApplicationDbContext(options, tenant);
        var schedule = await context.OfficialReportSchedules.Include(item => item.ReportTemplate).Include(item => item.MunicipalityFinancialYear).Include(item => item.ReportingPeriod).SingleAsync();
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        var controller = new OfficialReportJobsController(context, IdpTestFixture.CreateUserManagerMock(seeded.User).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(seeded.User.Id) } }
        };
        var request = new SaveOfficialReportScheduleRequest(schedule.PublicId, Convert.ToBase64String(schedule.RowVersion), schedule.ReportTemplate.PublicId,
            schedule.MunicipalityFinancialYear.PublicId, schedule.ReportingPeriod.PublicId, null, null, schedule.Code, "Revised distribution",
            OfficialReportScheduleCadence.Weekly, 2, DateTime.UtcNow.AddDays(1), null, OfficialReportRecipientKind.User, [seeded.User.Id], ["IN_APP", "EMAIL"], true, true, "Council-2026-2", "Approved revised cadence");

        var result = await controller.SaveSchedule(request);

        result.Result.Should().BeOfType<OkObjectResult>();
        var versions = await context.OfficialReportSchedules.OrderBy(item => item.VersionNumber).ToArrayAsync();
        versions.Should().HaveCount(2);
        versions[0].IsCurrent.Should().BeFalse();
        versions[0].Name.Should().Be("Q1 governed distribution");
        versions[1].IsCurrent.Should().BeTrue();
        versions[1].VersionNumber.Should().Be(2);
        versions[1].PreviousVersionId.Should().Be(versions[0].Id);
        versions[1].ChannelsCsv.Should().Be("IN_APP,EMAIL");
    }

    [Fact]
    public async Task DueSchedule_MaterializesCompletesAndQueuesDistributionEvidence()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var seeded = await SeedDueSchedule(options);

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        var storage = new Mock<IEvidenceBlobStorage>();
        storage.Setup(service => service.StoreAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceStorageOperationResult(true, "stored"));

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped(provider => new ApplicationDbContext(options, provider.GetRequiredService<ITenantContext>()));
        services.AddScoped<UserManager<ApplicationUser>>(provider =>
            IdpTestFixture.CreateUserManagerMock(provider.GetRequiredService<ApplicationDbContext>().Users.Single(item => item.Id == seeded.User.Id)).Object);
        services.AddSingleton(access.Object);
        services.AddSingleton(storage.Object);
        services.AddScoped<IWorkflowGovernanceService, WorkflowGovernanceService>();
        await using var provider = services.BuildServiceProvider();
        var worker = new OfficialReportJobWorker(provider.GetRequiredService<IServiceScopeFactory>(), new ConfigurationBuilder().Build(), NullLogger<OfficialReportJobWorker>.Instance);

        var processed = await worker.ProcessBatch(CancellationToken.None);

        processed.Should().Be(1);
        await using var verification = new ApplicationDbContext(options, new SystemTenantContext());
        var schedule = await verification.OfficialReportSchedules.IgnoreQueryFilters().SingleAsync();
        schedule.IsActive.Should().BeFalse();
        schedule.NextRunAt.Should().BeNull();
        var job = await verification.OfficialReportJobs.IgnoreQueryFilters().Include(item => item.OfficialReportGeneration).Include(item => item.DistributionOutbox).SingleAsync();
        job.State.Should().Be(OfficialReportJobState.Completed, job.LastError);
        job.AttemptCount.Should().Be(1);
        job.OfficialReportGeneration.Should().NotBeNull();
        job.DistributionOutbox.Should().NotBeNull();
        job.DistributionOutbox!.EventType.Should().Be("Notification.ReportReady");
        job.DistributionOutbox.Payload.Should().Contain(seeded.User.Id).And.Contain("IN_APP");
        (await verification.Notifications.IgnoreQueryFilters().SingleAsync()).Type.Should().Be(NotificationType.ReportReady);
        (await verification.AuditTrails.IgnoreQueryFilters().AnyAsync(item => item.EntityName == nameof(OfficialReportJob) && item.Action == "Complete")).Should().BeTrue();
    }

    [Fact]
    public async Task DueSchedule_WithNoResolvedRecipients_FailsClosedWithoutGenerating()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var seeded = await SeedDueSchedule(options);
        await using (var update = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            var schedule = await update.OfficialReportSchedules.SingleAsync();
            schedule.RecipientValuesCsv = "missing-user";
            await update.SaveChangesAsync();
        }
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped(provider => new ApplicationDbContext(options, provider.GetRequiredService<ITenantContext>()));
        await using var provider = services.BuildServiceProvider();
        var worker = new OfficialReportJobWorker(provider.GetRequiredService<IServiceScopeFactory>(), new ConfigurationBuilder().Build(), NullLogger<OfficialReportJobWorker>.Instance);

        var processed = await worker.ProcessBatch(CancellationToken.None);

        processed.Should().Be(0);
        await using var verification = new ApplicationDbContext(options, new SystemTenantContext());
        var job = await verification.OfficialReportJobs.IgnoreQueryFilters().SingleAsync();
        job.State.Should().Be(OfficialReportJobState.Failed);
        job.LastError.Should().Contain("no active tenant recipients");
        (await verification.OfficialReportGenerations.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    private static async Task<(ApplicationUser User, OfficialReportSchedule Schedule)> SeedDueSchedule(DbContextOptions<ApplicationDbContext> options)
    {
        await using var context = new ApplicationDbContext(options, new SystemTenantContext());
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Code = "ASYNC", Name = "Async Municipality" };
        var user = new ApplicationUser { Id = "report-scheduler", UserName = "report-scheduler", NormalizedUserName = "REPORT-SCHEDULER", Email = "reports@example.test", NormalizedEmail = "REPORTS@EXAMPLE.TEST", FirstName = "Report", LastName = "Scheduler", Municipality = municipality, IsActive = true };
        var year = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        context.AddRange(municipality, user, year);
        await context.SaveChangesAsync();
        var municipalYear = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = year.Id, IsActive = true, IsCurrent = true, EffectiveFrom = year.StartDate };
        context.Add(municipalYear); await context.SaveChangesAsync();
        var period = new ReportingPeriod { MunicipalityFinancialYearId = municipalYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new(2026, 7, 1), EndDate = new(2026, 9, 30), IsActive = true };
        context.Add(period); await context.SaveChangesAsync();
        var template = new OfficialReportTemplate
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalYear.Id, SubmissionKind = SubmissionKind.Opms,
            ReportType = OfficialReportType.QuarterlyPerformance, Code = "QUARTERLY", Name = "Quarterly report", Format = OfficialReportFormat.Csv,
            ColumnConfigurationJson = OfficialReportCatalog.DefaultColumnsJson(OfficialReportType.QuarterlyPerformance), EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            ApprovalReference = "Council-2026", Reason = "Approved", CreatedByUserId = user.Id
        };
        context.Add(template); await context.SaveChangesAsync();
        var schedule = new OfficialReportSchedule
        {
            MunicipalityId = municipality.Id, ReportTemplateId = template.Id, MunicipalityFinancialYearId = municipalYear.Id, ReportingPeriodId = period.Id,
            Code = "Q1-DISTRIBUTION", Name = "Q1 governed distribution", Cadence = OfficialReportScheduleCadence.Once, Interval = 1,
            NextRunAt = DateTime.UtcNow.AddMinutes(-1), RecipientKind = OfficialReportRecipientKind.User, RecipientValuesCsv = user.Id,
            ChannelsCsv = "IN_APP", IsMandatory = true, IsCurrent = true, IsActive = true, ApprovalReference = "Council-2026", Reason = "Approved distribution", CreatedByUserId = user.Id
        };
        context.Add(schedule); await context.SaveChangesAsync();
        return (new ApplicationUser
        {
            Id = user.Id, UserName = user.UserName, NormalizedUserName = user.NormalizedUserName, Email = user.Email,
            NormalizedEmail = user.NormalizedEmail, FirstName = user.FirstName, LastName = user.LastName,
            MunicipalityId = municipality.Id, IsActive = true
        }, schedule);
    }

    private sealed class SystemTenantContext : ITenantContext { public long? MunicipalityId => null; public bool IsSystem => true; public string? UserId => "system"; }
    private sealed class FixedTenantContext(long municipalityId, string userId) : ITenantContext { public long? MunicipalityId => municipalityId; public bool IsSystem => false; public string? UserId => userId; }
}

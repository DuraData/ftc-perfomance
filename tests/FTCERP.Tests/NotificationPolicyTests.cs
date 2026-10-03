using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class NotificationPolicyTests
{
    [Fact]
    public void AdministrationEndpoints_RequireDynamicWorkflowConfigurationPermission()
    {
        foreach (var methodName in new[] { nameof(NotificationPoliciesController.GetAll), nameof(NotificationPoliciesController.CreateDraft), nameof(NotificationPoliciesController.Activate), nameof(NotificationPoliciesController.CopyToFinancialYear), nameof(NotificationPoliciesController.SetDeliveryState), nameof(NotificationPoliciesController.QueueTest), nameof(NotificationPoliciesController.RunDue), nameof(NotificationPoliciesController.AddHoliday) })
        {
            var method = typeof(NotificationPoliciesController).GetMethod(methodName)!;
            var attribute = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single();
            attribute.Policy.Should().Be("Permission:WORKFLOW.CONFIGURE");
        }
    }

    [Fact]
    public void WorkingCalendar_SkipsWeekendsAndConfiguredHolidays()
    {
        var service = new WorkingCalendarService();
        var holidays = new HashSet<DateOnly> { new(2026, 4, 6) };

        service.AddWorkingDays(new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), -1, holidays)
            .Should().Be(new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc));
        service.AddWorkingDays(new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc), 1, holidays)
            .Should().Be(new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TemplateValidation_IsControlledAndMandatoryPolicyAlwaysIncludesInApp()
    {
        var service = CreateService(null!);
        service.ValidateTemplate("{Item} due", "Submit by {DueDate} for {Period}").Should().BeNull();
        service.ValidateTemplate("{Unsafe}", "Message").Should().Contain("not supported");
        var policy = new NotificationConfiguration { IsMandatory = true, ChannelsCsv = "SMS", TitleTemplate = "{Item}", MessageTemplate = "{Municipality}: {Days}" };

        var preview = service.Preview(policy, new("Submission", "Q1", "Metro", DateTime.UtcNow, -3));

        preview.Channels.Should().Equal("IN_APP", "SMS");
        preview.Title.Should().Be("Submission");
        preview.Message.Should().Be("Metro: 3");
    }

    [Fact]
    public async Task Scheduler_QueuesCatchUpOnce_AndPinsPolicyChannels()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var fixture = await Seed(context, deliveryPaused: false);
        var service = CreateService(context);
        var now = fixture.Deadline.AddDays(5);

        (await service.ProcessDueAsync(now)).Should().Be(1);
        (await service.ProcessDueAsync(now.AddMinutes(1))).Should().Be(0);

        var scheduled = await context.ScheduledNotifications.IgnoreQueryFilters().SingleAsync();
        scheduled.State.Should().Be(ScheduledNotificationState.Queued);
        scheduled.ScheduledAt.Should().Be(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
        var outbox = await context.BusinessEventOutbox.IgnoreQueryFilters().SingleAsync();
        outbox.Payload.Should().Contain("IN_APP").And.Contain("SMS");
        (await context.Notifications.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Scheduler_PauseDoesNotChangeDeadline_AndResumeCatchesUp()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var fixture = await Seed(context, deliveryPaused: true);
        var service = CreateService(context);

        (await service.ProcessDueAsync(fixture.Deadline.AddDays(1))).Should().Be(0);
        var pending = await context.ScheduledNotifications.IgnoreQueryFilters().SingleAsync();
        pending.State.Should().Be(ScheduledNotificationState.Pending);
        pending.DeadlineAt.Should().Be(fixture.Deadline);
        var policy = await context.NotificationConfigurations.IgnoreQueryFilters().SingleAsync();
        policy.DeliveryPaused = false;
        await context.SaveChangesAsync();

        (await service.ProcessDueAsync(fixture.Deadline.AddDays(2))).Should().Be(1);
        (await context.ScheduledNotifications.IgnoreQueryFilters().SingleAsync()).State.Should().Be(ScheduledNotificationState.Queued);
    }

    [Fact]
    public async Task Scheduler_PeriodOverrideReplacesMunicipalityDefaultForSamePolicyCode()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var fixture = await Seed(context, deliveryPaused: false);
        var period = await context.ReportingPeriods.Include(x => x.MunicipalityFinancialYear).SingleAsync();
        var municipality = await context.Municipalities.SingleAsync();
        var user = await context.Users.SingleAsync();
        var policy = new NotificationConfiguration
        {
            MunicipalityId = municipality.Id, MunicipalityFinancialYearId = period.MunicipalityFinancialYearId, FamilyId = Guid.NewGuid(), Version = 1,
            Code = fixture.Policy.Code, Name = "Quarter override", ApplicabilityKey = $"SUBMISSION_DUE|1|Opms|3|-|{period.Id}",
            Scope = NotificationPolicyScope.ReportingPeriodOverride, Source = NotificationScheduleSource.ReportingWindow, SubmissionKind = SubmissionKind.Opms,
            ReportingPeriodId = period.Id, Lifecycle = NotificationPolicyLifecycle.Active, IsMandatory = true, ChannelsCsv = "IN_APP",
            TitleTemplate = "Override for {Period}", MessageTemplate = "Override due {DueDate}", EffectiveFrom = fixture.Deadline.AddMonths(-1),
            CreatedByUserId = user.Id, ActivatedByUserId = user.Id, ActivatedAt = fixture.Deadline.AddMonths(-1)
        };
        policy.Rules.Add(new NotificationScheduleRule { MunicipalityId = municipality.Id, Code = "THREE_DAYS", WorkingDayOffset = -3, RecipientKind = NotificationRecipientKind.User, RecipientValuesCsv = user.Id });
        context.NotificationConfigurations.Add(policy);
        await context.SaveChangesAsync();

        (await CreateService(context).ProcessDueAsync(fixture.Deadline.AddDays(1))).Should().Be(1);

        (await context.Notifications.IgnoreQueryFilters().SingleAsync()).Title.Should().Be("Override for Quarter 1");
        (await context.ScheduledNotifications.IgnoreQueryFilters().SingleAsync()).NotificationConfigurationId.Should().Be(policy.Id);
    }

    private static NotificationPolicyService CreateService(ApplicationDbContext context) => new(context, new WorkingCalendarService());

    private static async Task<(DateTime Deadline, NotificationConfiguration Policy)> Seed(ApplicationDbContext context, bool deliveryPaused)
    {
        var municipality = new Municipality { Code = $"M-{Guid.NewGuid():N}", Name = "Test Metro", EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        var user = IdpTestFixture.CreateUser("notify-user");
        user.Municipality = municipality; user.MunicipalityId = municipality.Id; user.PhoneNumber = "+27110000000";
        var year = new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        var municipalYear = new MunicipalityFinancialYear { Municipality = municipality, FinancialYear = year, EffectiveFrom = new(2026, 7, 1), IsCurrent = true };
        var period = new ReportingPeriod { MunicipalityFinancialYear = municipalYear, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new(2026, 7, 1), EndDate = new(2026, 9, 30) };
        var deadline = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        var window = new ReportingWindow { Municipality = municipality, ReportingPeriod = period, SubmissionKind = SubmissionKind.Opms, OpensAt = deadline.AddDays(-14), ClosesAt = deadline };
        var policy = new NotificationConfiguration
        {
            Municipality = municipality, MunicipalityFinancialYear = municipalYear, FamilyId = Guid.NewGuid(), Version = 1, Code = "SUBMISSION_DUE", Name = "Submission due",
            ApplicabilityKey = "SUBMISSION_DUE|1|Opms|1|-|-", Scope = NotificationPolicyScope.MunicipalityDefault, Source = NotificationScheduleSource.ReportingWindow,
            SubmissionKind = SubmissionKind.Opms, Lifecycle = NotificationPolicyLifecycle.Active, IsMandatory = true, DeliveryPaused = deliveryPaused,
            ChannelsCsv = "SMS", TitleTemplate = "{Item} due", MessageTemplate = "{Period} is due {DueDate}", EffectiveFrom = deadline.AddMonths(-1), CreatedByUserId = user.Id, ActivatedByUserId = user.Id, ActivatedAt = deadline.AddMonths(-1)
        };
        policy.Rules.Add(new NotificationScheduleRule { Municipality = municipality, Code = "THREE_DAYS", WorkingDayOffset = -3, RecipientKind = NotificationRecipientKind.User, RecipientValuesCsv = user.Id });
        context.AddRange(municipality, user, year, municipalYear, period, window, policy);
        await context.SaveChangesAsync();
        return (deadline, policy);
    }
}

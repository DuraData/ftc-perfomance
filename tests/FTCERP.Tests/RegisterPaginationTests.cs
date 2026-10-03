using System.ComponentModel.DataAnnotations;
using FTCERP.Host.Application.Services;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class RegisterPaginationTests
{
    [Fact]
    public async Task Opms_submission_page_filters_by_target_before_count_and_projects_target_identity()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("submission-page-user");
        var first = Target("target-a", "KPI-001", "First", Guid.NewGuid());
        var second = Target("target-b", "KPI-002", "Second", Guid.NewGuid());
        context.AddRange(user, first, second);
        context.OpmsSubmissions.AddRange(
            new OpmsSubmission { Id = "submission-a", OpmsTargetId = first.Id, Quarter = "Q1", Status = "draft" },
            new OpmsSubmission { Id = "submission-b", OpmsTargetId = second.Id, Quarter = "Q1", Status = "draft" });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        access.Setup(service => service.GetEffectiveAccessAsync(user))
            .ReturnsAsync(new EffectiveAccessResult([], [], [], [], [], []));
        var controller = new OpmsSubmissionsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<IEvidenceBlobStorage>(),
            Mock.Of<ISubmissionValueService>(),
            Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(),
            Mock.Of<IEvidenceInspectionService>(),
            Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.GetSubmissionsPage(new PagedQueryRequest
        {
            Page = 1,
            PageSize = 25,
            TargetPublicId = first.PublicId
        });

        var envelope = Assert.IsType<ApiResponse<PagedResponse<OpmsSubmissionResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(1, envelope.Data!.TotalCount);
        var item = Assert.Single(envelope.Data.Items);
        Assert.Equal(first.Id, item.OpmsTargetId);
        Assert.Equal("KPI-001", item.TargetIndicatorNumber);

        var retired = Assert.IsType<ObjectResult>(controller.GetSubmissions().Result);
        Assert.Equal(StatusCodes.Status410Gone, retired.StatusCode);
    }

    [Fact]
    public async Task Opms_target_page_applies_authorized_scope_before_count_and_uses_stable_server_paging()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("page-user");
        context.Users.Add(user);
        context.OpmsTargets.AddRange(
            Target("a", "003", "Gamma", new Guid("00000000-0000-0000-0000-000000000003")),
            Target("b", "001", "Alpha", new Guid("00000000-0000-0000-0000-000000000001")),
            Target("c", "002", "Beta", new Guid("00000000-0000-0000-0000-000000000002")),
            Target("outside", "000", "Outside scope", new Guid("00000000-0000-0000-0000-000000000004")));
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, false, [], [], [], ["a", "b", "c"], [], []));
        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<ITenantContext>(),
            new PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.GetTargetsPage(new PagedQueryRequest
        {
            Page = 1,
            PageSize = 2,
            SortBy = "targetName",
            SortDirection = "asc"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<PagedResponse<OpmsTargetResponse>>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Equal(3, envelope.Data!.TotalCount);
        Assert.Equal(2, envelope.Data.TotalPages);
        Assert.Equal(["Alpha", "Beta"], envelope.Data.Items.Select(item => item.TargetName));
        Assert.DoesNotContain(envelope.Data.Items, item => item.Id == "outside");
    }

    [Fact]
    public async Task Opms_target_page_rejects_unknown_sort_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("sort-user");
        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            Mock.Of<IAccessControlService>(),
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<ITenantContext>(),
            new PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.GetTargetsPage(new PagedQueryRequest { SortBy = "raw-sql-field" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Opms_target_options_are_lightweight_searchable_bounded_and_exclude_withdrawn_records()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("option-user");
        var available = Target("available", "KPI-001", "Water reliability", Guid.NewGuid());
        var withdrawn = Target("withdrawn", "KPI-002", "Water legacy", Guid.NewGuid());
        withdrawn.IsWithdrawn = true;
        withdrawn.ReasonForWithdrawal = "Superseded target";
        withdrawn.WithdrawnAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var outside = Target("outside-option", "KPI-003", "Water outside", Guid.NewGuid());
        context.AddRange(user, available, withdrawn, outside);
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, false, [], [], [], [available.Id, withdrawn.Id], [], []));
        var controller = new OpmsTargetsController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object,
            Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<ITenantContext>(),
            new PerformanceUnitEngine())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.GetTargetOptions(new PagedQueryRequest
        {
            Page = 1,
            PageSize = 1,
            Search = "Water",
            SortBy = "indicatorNumber",
            SortDirection = "asc"
        });

        var envelope = Assert.IsType<ApiResponse<PagedResponse<PerformanceTargetOptionResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(1, envelope.Data!.TotalCount);
        Assert.Equal(1, envelope.Data.PageSize);
        var option = Assert.Single(envelope.Data.Items);
        Assert.Equal(available.Id, option.Id);
        Assert.Equal(available.PublicId, option.PublicId);
        Assert.Equal("KPI-001", option.IndicatorNumber);
        Assert.DoesNotContain(envelope.Data.Items, item => item.Id == withdrawn.Id || item.Id == outside.Id);

        var retired = Assert.IsType<ObjectResult>(controller.GetTargets().Result);
        Assert.Equal(StatusCodes.Status410Gone, retired.StatusCode);
    }

    [Fact]
    public async Task Notification_delivery_page_is_bounded_searchable_and_prioritizes_dead_letters()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.BusinessEventOutbox.AddRange(
            NotificationEvent("Notification.Zeta", "record-z", 10, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
            NotificationEvent("Notification.Alpha", "record-a", 1, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            NotificationEvent("Notification.Beta", "record-b", 2, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            NotificationEvent("Integration.Unrelated", "record-x", 10, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(value => value.MunicipalityId).Returns(7);
        var controller = new NotificationOperationsController(context, tenant.Object);

        var result = await controller.GetPendingPage(new PagedQueryRequest
        {
            Page = 1,
            PageSize = 2,
            SortBy = "eventType",
            SortDirection = "asc"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<PagedResponse<NotificationOutboxItemDto>>>(ok.Value);
        Assert.Equal(3, envelope.Data!.TotalCount);
        Assert.Equal(2, envelope.Data.TotalPages);
        Assert.Equal(["Notification.Zeta", "Notification.Alpha"], envelope.Data.Items.Select(item => item.EventType));
        Assert.True(envelope.Data.Items[0].IsDeadLetter);
    }

    [Fact]
    public async Task Notification_delivery_page_rejects_unknown_sort_fields()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(value => value.MunicipalityId).Returns(7);
        var controller = new NotificationOperationsController(context, tenant.Object);

        var result = await controller.GetPendingPage(new PagedQueryRequest { SortBy = "payload" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task User_notification_page_reports_authoritative_unread_count_and_excludes_other_users()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("notification-user");
        var other = IdpTestFixture.CreateUser("other-notification-user");
        context.Users.AddRange(user, other);
        context.Notifications.AddRange(
            UserNotification("n-1", user.Id, "Zulu", false, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
            UserNotification("n-2", user.Id, "Alpha", true, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            UserNotification("n-3", user.Id, "Beta", false, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            UserNotification("outside", other.Id, "Outside", false, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, "Notifications.View", null))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        var controller = new NotificationsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object)
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.GetNotificationsPage(new PagedQueryRequest
        {
            Page = 1,
            PageSize = 2,
            SortBy = "title",
            SortDirection = "asc"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<NotificationPageResponse>>(ok.Value);
        Assert.Equal(3, envelope.Data!.TotalCount);
        Assert.Equal(2, envelope.Data.UnreadCount);
        Assert.Equal(["Alpha", "Beta"], envelope.Data.Items.Select(item => item.Title));
        Assert.DoesNotContain(envelope.Data.Items, item => item.Id == "outside");
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Page_contract_rejects_unbounded_or_invalid_ranges(int page, int pageSize)
    {
        var request = new PagedQueryRequest { Page = page, PageSize = pageSize };
        var validationResults = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true);

        Assert.False(valid);
        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public void Page_response_calculates_metadata_without_provider_specific_behavior()
    {
        var response = PagedResponse<string>.Create(["one", "two"], 2, 2, 5);

        Assert.Equal(3, response.TotalPages);
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
    }

    private static OpmsTarget Target(string id, string indicator, string name, Guid publicId) => new()
    {
        Id = id,
        PublicId = publicId,
        IndicatorNumber = indicator,
        TargetName = name,
        KpiDescription = $"{name} description",
        AnnualTargetDescription = "One",
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static BusinessEventOutbox NotificationEvent(string eventType, string aggregateId, int attemptCount, DateTime occurredAt) => new()
    {
        EventType = eventType,
        AggregateType = "OpmsSubmission",
        AggregateId = aggregateId,
        Payload = "{}",
        AttemptCount = attemptCount,
        OccurredAt = occurredAt,
        AvailableAt = occurredAt
    };

    private static Notification UserNotification(string id, string userId, string title, bool isRead, DateTime createdAt) => new()
    {
        Id = id,
        UserId = userId,
        Type = NotificationType.Submission,
        Title = title,
        Message = $"{title} message",
        IsRead = isRead,
        CreatedAt = createdAt
    };

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(userId) }
    };
}

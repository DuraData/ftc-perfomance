using System.ComponentModel.DataAnnotations;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;

namespace FTCERP.Tests;

public sealed class RegisterPaginationTests
{
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
            Mock.Of<ITenantContext>())
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
            Mock.Of<ITenantContext>())
        {
            ControllerContext = ControllerContext(user.Id)
        };

        var result = await controller.GetTargetsPage(new PagedQueryRequest { SortBy = "raw-sql-field" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
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

    private static ControllerContext ControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(userId) }
    };
}

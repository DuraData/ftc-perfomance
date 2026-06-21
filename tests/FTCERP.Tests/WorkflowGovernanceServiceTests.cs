namespace FTCERP.Tests;

public class WorkflowGovernanceServiceTests
{
    [Fact]
    public async Task WriteAuditTrailAsync_ShouldPersistAuditRecord()
    {
        await using var context = IdpTestFixture.CreateContext();
        var service = new WorkflowGovernanceService(context);

        await service.WriteAuditTrailAsync(
            entityName: "IdpPlan",
            entityId: "1",
            action: "Create",
            oldValue: null,
            newValue: new { Name = "Plan" },
            changedBy: "tester",
            ipAddress: "127.0.0.1");

        var audit = await context.AuditTrails.SingleAsync();
        audit.EntityName.Should().Be("IdpPlan");
        audit.EntityId.Should().Be("1");
        audit.Action.Should().Be("Create");
        audit.ChangedBy.Should().Be("tester");
        audit.IpAddress.Should().Be("127.0.0.1");
        audit.NewValue.Should().Contain("Plan");
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldPersistNotification()
    {
        await using var context = IdpTestFixture.CreateContext();
        var service = new WorkflowGovernanceService(context);

        await service.CreateNotificationAsync("user-1", NotificationType.Submission, "Title", "Message", "Entity", "1");

        var note = await context.Notifications.SingleAsync();
        note.UserId.Should().Be("user-1");
        note.Type.Should().Be(NotificationType.Submission);
        note.Title.Should().Be("Title");
        note.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWorkflowNotificationsAsync_ShouldCreateDistinctNotifications()
    {
        await using var context = IdpTestFixture.CreateContext();
        var service = new WorkflowGovernanceService(context);

        await service.CreateWorkflowNotificationsAsync(
            new[] { "user-1", "user-1", "user-2", "" },
            NotificationType.Approval,
            "Approval",
            "Review completed",
            "Entity",
            "2");

        var notes = await context.Notifications.OrderBy(n => n.UserId).ToListAsync();
        notes.Should().HaveCount(2);
        notes.Select(n => n.UserId).Should().Equal("user-1", "user-2");
    }
}

using System.Security.Claims;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace FTCERP.Tests;

public sealed class AuditGovernanceTests
{
    [Fact]
    public async Task Workflow_audit_captures_tenant_request_session_and_reason_context()
    {
        var tenant = new AuditTenantContext(7);
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        context.Users.Add(new ApplicationUser { Id = "actor", UserName = "actor@test", Email = "actor@test", FirstName = "Audit", LastName = "Actor", IsActive = true });
        await context.SaveChangesAsync();
        var http = new DefaultHttpContext { TraceIdentifier = "correlation-123" };
        http.Request.Headers.UserAgent = "Audit Test Agent";
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sid", "session-123")], "test"));
        var service = new WorkflowGovernanceService(context, new HttpContextAccessor { HttpContext = http }, tenant);

        service.QueueAuditTrail("OpmsSubmission", "submission-1", "Approve", new { State = "Review" }, new { State = "Approved" }, "actor", null, "Approved after verification");
        await context.SaveChangesAsync();

        var audit = await context.AuditTrails.SingleAsync();
        audit.MunicipalityId.Should().Be(7);
        audit.CorrelationId.Should().Be("correlation-123");
        audit.SessionId.Should().Be("session-123");
        audit.UserAgent.Should().Be("Audit Test Agent");
        audit.IpAddress.Should().Be("127.0.0.1");
        audit.Reason.Should().Be("Approved after verification");
        audit.OldValue.Should().Contain("Review");
        audit.NewValue.Should().Contain("Approved");
    }

    [Fact]
    public void Audit_trail_endpoint_requires_the_dynamic_view_permission()
    {
        var method = typeof(AuditController).GetMethod(nameof(AuditController.GetAuditTrails))!;
        var authorization = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        authorization.Policy.Should().Be("Permission:Audit.Trails.View");
        method.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Select(item => item.Template)
            .Should().Contain("/api/v1/audit/trails");
    }

    private sealed class AuditTenantContext(long municipalityId) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => false;
        public string? UserId => "actor";
    }
}

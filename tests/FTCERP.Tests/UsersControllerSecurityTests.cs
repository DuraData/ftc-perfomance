using System.Security.Claims;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class UsersControllerSecurityTests
{
    [Fact]
    public async Task Security_registry_idempotently_catalogues_user_contact_members_and_soft_delete()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new FixedTenantContext(null, "system", true));
        await context.Database.EnsureCreatedAsync();

        await SecurityRegistrySeeder.SeedAsync(context);
        await SecurityRegistrySeeder.SeedAsync(context);

        var members = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "USER")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(["Email", "PhoneNumber"], members.Select(item => item.MemberCode).ToArray());
        Assert.All(members, item => Assert.True(item.IsSensitive));
        Assert.Equal(4, await context.Permissions.CountAsync(item => item.ResourceCode == "USER" && item.MemberCode != null));
        Assert.Single(await context.Permissions.Where(item => item.Code == "USER.DELETE").ToArrayAsync());

        var employeeMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "EMPLOYEE")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        employeeMembers.Select(item => item.MemberCode).Should().Contain(["EmployeeNumber", "EmailAddress", "IdentityUserId"]);
        Assert.All(employeeMembers.Where(item => item.MemberCode is "EmployeeNumber" or "EmailAddress" or "IdentityUserId"), item => Assert.True(item.IsSensitive));
        Assert.Equal(6, await context.Permissions.CountAsync(item => item.ResourceCode == "EMPLOYEE"
            && new[] { "EmployeeNumber", "EmailAddress", "IdentityUserId" }.Contains(item.MemberCode)));

        foreach (var resourceCode in new[] { "OPMS_SUBMISSION", "IPMS_SUBMISSION" })
        {
            var submissionMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode)
                .Select(item => item.MemberCode)
                .ToArrayAsync();
            submissionMembers.Should().Contain(["ActualPerformance", "Variance", "VarianceReason", "CorrectiveMeasure", "SubmittedDate", "InternalAuditObservation"]);
            (await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode && item.MemberCode != null)).Should().Be(10);
        }
    }

    [Fact]
    public async Task User_administration_is_tenant_scoped_and_redacts_denied_members()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(101, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var municipalityA = new Municipality { Id = 101, Code = "M101", Name = "Municipality 101" };
        var municipalityB = new Municipality { Id = 102, Code = "M102", Name = "Municipality 102" };
        var actor = User("actor", 101, "actor@example.test", "0111111111");
        var sameTenant = User("same", 101, "same@example.test", "0222222222");
        var otherTenant = User("other", 102, "other@example.test", "0333333333");
        context.AddRange(municipalityA, municipalityB, actor, sameTenant, otherTenant);
        await context.SaveChangesAsync();

        var access = Access(actor, "USER.READ");
        var controller = Controller(context, tenant, actor, new Dictionary<string, ApplicationUser>
        {
            [actor.Id] = actor,
            [sameTenant.Id] = sameTenant,
            [otherTenant.Id] = otherTenant
        }, access.Object);

        var list = await controller.GetUsersPage(new PagedQueryRequest { PageSize = 100, SortBy = "name" });
        var envelope = Assert.IsType<ApiResponse<PagedResponse<UserDetailResponse>>>(Assert.IsType<OkObjectResult>(list.Result).Value);
        Assert.Equal(2, envelope.Data!.TotalCount);
        Assert.All(envelope.Data.Items, item =>
        {
            Assert.Null(item.User.Email);
            Assert.Null(item.User.PhoneNumber);
        });
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetUsers().Result).StatusCode);

        var crossTenant = await controller.GetUser(otherTenant.Id);
        Assert.IsType<NotFoundObjectResult>(crossTenant.Result);
    }

    [Fact]
    public async Task Direct_user_update_cannot_change_phone_when_member_update_is_denied()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(201, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Id = 201, Code = "M201", Name = "Municipality 201" };
        var actor = User("actor", 201, "actor@example.test", "0111111111");
        var target = User("target", 201, "target@example.test", "0222222222");
        context.AddRange(municipality, actor, target);
        await context.SaveChangesAsync();

        var access = Access(actor, "USER.UPDATE", "USER.Email.READ", "USER.PhoneNumber.READ");
        var controller = Controller(context, tenant, actor, new Dictionary<string, ApplicationUser>
        {
            [actor.Id] = actor,
            [target.Id] = target
        }, access.Object);

        var response = await controller.UpdateUser(target.Id, new UpdateUserRequest("Changed", "Name", "0999999999", true));

        Assert.IsType<ForbidResult>(response.Result);
        Assert.Equal("0222222222", (await context.Users.SingleAsync(item => item.Id == target.Id)).PhoneNumber);
    }

    [Fact]
    public async Task User_directory_page_is_bounded_searchable_and_tenant_scoped()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(301, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        context.AddRange(new Municipality { Id = 301, Code = "M301", Name = "Municipality 301" },
            new Municipality { Id = 302, Code = "M302", Name = "Municipality 302" });
        var actor = User("actor", 301, "actor@example.test", "0111111111");
        var users = Enumerable.Range(1, 31).Select(index =>
            User($"tenant-{index:00}", 301, $"person{index:00}@example.test", $"02{index:00000000}")).ToArray();
        var other = User("other", 302, "person99@example.test", "0399999999");
        context.Add(actor); context.AddRange(users); context.Add(other);
        await context.SaveChangesAsync();
        var directory = users.Append(actor).Append(other).ToDictionary(item => item.Id);
        var controller = Controller(context, tenant, actor, directory,
            Access(actor, "USER.READ", "USER.Email.READ", "USER.PhoneNumber.READ").Object);

        var result = await controller.GetUsersPage(new PagedQueryRequest
        {
            Page = 2, PageSize = 10, SortBy = "email", SortDirection = "asc", Search = "example.test"
        });

        var envelope = Assert.IsType<ApiResponse<PagedResponse<UserDetailResponse>>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(32, envelope.Data!.TotalCount);
        Assert.Equal(10, envelope.Data.Items.Length);
        Assert.Equal(2, envelope.Data.Page);
        Assert.Equal(4, envelope.Data.TotalPages);
        Assert.DoesNotContain(envelope.Data.Items, item => item.User.Id == other.Id);
    }

    private static ApplicationUser User(string id, long municipalityId, string email, string phone) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        PhoneNumber = phone,
        FirstName = id,
        LastName = "User",
        SecurityStamp = Guid.NewGuid().ToString()
    };

    private static Mock<IAccessControlService> Access(ApplicationUser actor, params string[] allowed)
    {
        var allowedSet = allowed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(allowedSet.Contains(code), allowedSet.Contains(code) ? "Allowed" : "Denied", [], [], []));
        return access;
    }

    private static UsersController Controller(ApplicationDbContext context, ITenantContext tenant, ApplicationUser actor,
        Dictionary<string, ApplicationUser> users, IAccessControlService access)
    {
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roles = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, Array.Empty<IRoleValidator<ApplicationRole>>(), null!, null!, null!);
        var controller = new UsersController(context, IdpTestFixture.CreateUserManagerMock(actor, users).Object, roles.Object, access, tenant);
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.Id)], "test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private sealed class FixedTenantContext(long? municipalityId, string? userId, bool isSystem = false) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => userId;
    }
}

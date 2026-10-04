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

        var list = await controller.GetUsers();
        var envelope = Assert.IsType<ApiResponse<UserDetailResponse[]>>(Assert.IsType<OkObjectResult>(list.Result).Value);
        Assert.Equal(2, envelope.Data!.Length);
        Assert.All(envelope.Data, item =>
        {
            Assert.Null(item.User.Email);
            Assert.Null(item.User.PhoneNumber);
        });

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

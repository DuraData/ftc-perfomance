namespace FTCERP.Tests;

public class TenancyControllerTests
{
    [Fact]
    public async Task Municipality_context_directory_pages_searches_and_retires_the_unbounded_route()
    {
        var tenant = new FixedTenantContext(null, true, "system-user");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        context.Municipalities.AddRange(Enumerable.Range(0, 11).Select(index => new Municipality
        {
            Code = $"MUN-{index:00}", Name = $"Paged Municipality {index:00}", IsActive = true,
            EffectiveFrom = new DateTime(2030, 1, 1).AddDays(index)
        }));
        await context.SaveChangesAsync();
        var controller = Controller(context, tenant, tenant.UserId!);

        var page = Payload(await controller.GetMyContextsPage(new PagedQueryRequest
            { Page = 2, PageSize = 3, Search = "Paged Municipality", SortBy = "code", SortDirection = "asc" }));

        page.TotalCount.Should().Be(11);
        page.Items.Select(item => item.Code).Should().Equal("MUN-03", "MUN-04", "MUN-05");
        Payload(await controller.GetMyContextsPage(new PagedQueryRequest { PageSize = 1 }, page.Items[0].PublicId)).Items
            .Should().ContainSingle().Which.PublicId.Should().Be(page.Items[0].PublicId);
        (await controller.GetMyContextsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetMyContexts().Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task Municipality_context_directory_exposes_only_current_effective_assignments()
    {
        var tenant = new FixedTenantContext(null, false, "tenant-user");
        await using var context = IdpTestFixture.CreateRelationalContext(tenant);
        var now = DateTime.UtcNow;
        var municipalities = Enumerable.Range(0, 4).Select(index => new Municipality
            { Code = $"SCOPE-{index}", Name = $"Scoped Municipality {index}", IsActive = true }).ToArray();
        var user = IdpTestFixture.CreateUser(tenant.UserId!, "Tenant", "User");
        var role = new ApplicationRole
            { Id = "tenant-role", Name = "Tenant role", NormalizedName = "TENANT ROLE", RoleCode = "TENANT_ROLE", IsActive = true };
        context.AddRange(municipalities);
        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.AddRange(
            Assignment(user, role, municipalities[0], now.AddDays(-1), null),
            Assignment(user, role, municipalities[1], now.AddDays(1), null),
            Assignment(user, role, municipalities[2], now.AddDays(-2), now.AddDays(-1)),
            Assignment(user, role, municipalities[3], now.AddDays(-2), null, now.AddHours(-1)));
        await context.SaveChangesAsync();
        var controller = Controller(context, tenant, user.Id);

        var page = Payload(await controller.GetMyContextsPage(new PagedQueryRequest { PageSize = 25, SortBy = "name", SortDirection = "asc" }));

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.Code.Should().Be("SCOPE-0");
    }

    private static SecurityUserRoleAssignment Assignment(ApplicationUser user, ApplicationRole role, Municipality municipality,
        DateTime effectiveFrom, DateTime? effectiveTo, DateTime? revokedAt = null) => new()
    {
        User = user, Role = role, Municipality = municipality, MunicipalityId = municipality.Id,
        EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo, IsActive = true,
        AssignedAt = effectiveFrom, AssignedBy = user.Id, RevokedAt = revokedAt, RevokedBy = revokedAt.HasValue ? user.Id : null
    };

    private static TenancyController Controller(ApplicationDbContext context, ITenantContext tenant, string userId)
    {
        var controller = new TenancyController(context, tenant);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private static PagedResponse<TenantContextDto> Payload(ActionResult<ApiResponse<PagedResponse<TenantContextDto>>> result) =>
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ApiResponse<PagedResponse<TenantContextDto>>>().Which.Data!;

    private sealed class FixedTenantContext(long? municipalityId, bool isSystem, string userId) : ITenantContext
    {
        public long? MunicipalityId { get; } = municipalityId;
        public bool IsSystem { get; } = isSystem;
        public string? UserId { get; } = userId;
    }
}

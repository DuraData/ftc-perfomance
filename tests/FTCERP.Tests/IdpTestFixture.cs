namespace FTCERP.Tests;

public static class IdpTestFixture
{
    public static ApplicationDbContext CreateContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static ApplicationDbContext CreateRelationalContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static ApplicationUser CreateUser(string id = "user-1", string firstName = "Test", string lastName = "User")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@local",
            NormalizedUserName = $"{id}@local".ToUpperInvariant(),
            Email = $"{id}@local.test",
            NormalizedEmail = $"{id}@local.test".ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true
        };
    }

    public static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser? currentUser = null, Dictionary<string, ApplicationUser>? byIdUsers = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        manager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) =>
            {
                if (byIdUsers != null && byIdUsers.TryGetValue(id, out var found))
                {
                    return found;
                }

                return currentUser != null && string.Equals(currentUser.Id, id, StringComparison.OrdinalIgnoreCase)
                    ? currentUser
                    : null;
            });

        return manager;
    }

    public static ClaimsPrincipal CreatePrincipal(string userId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            authenticationType: "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    public static IdpController CreateController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWorkflowGovernanceService workflow,
        string userId)
    {
        var controller = new IdpController(context, userManager, workflow)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(userId)
                }
            }
        };

        return controller;
    }
}

using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace FTCERP.Tests;

public sealed class PasswordResetControllerTests
{
    [Fact]
    public async Task Forgot_and_reset_are_enumeration_safe_audited_and_revoke_sessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton<IWebHostEnvironment>(new TestEnvironment());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
        }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = IdpTestFixture.CreateUser("password-reset-user");
        user.MustChangePassword = true;
        Assert.True((await users.CreateAsync(user, "Original-Password9!")).Succeeded);
        var originalStamp = user.SecurityStamp;

        var jwt = new Mock<IJwtService>();
        jwt.Setup(service => service.RevokeAllSessionsAsync(user.Id, It.IsAny<string?>(), "Password reset")).ReturnsAsync(2);
        var notifier = new Mock<IPasswordResetNotifier>();
        notifier.Setup(service => service.SendAsync(user, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetDeliveryResult(true, "TestEmail"));
        var controller = CreateController(users, jwt.Object, context, notifier.Object, scope.ServiceProvider);

        var known = await controller.ForgotPassword(new ForgotPasswordRequest(user.Email!), CancellationToken.None);
        var unknown = await controller.ForgotPassword(new ForgotPasswordRequest("absent@example.test"), CancellationToken.None);

        Assert.Equal(Message(known), Message(unknown));
        notifier.Verify(service => service.SendAsync(user, It.IsAny<string>(), "password-reset-test", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(await context.AuditTrails.Where(item => item.Action == "PasswordResetRequested").ToArrayAsync());

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var reset = await controller.ResetPassword(new ResetPasswordRequest(user.Email!, token, "Replacement-Password8@"));

        var response = Assert.IsType<OkObjectResult>(reset.Result);
        Assert.True(Assert.IsType<ApiResponse<bool>>(response.Value).Success);
        Assert.True(await users.CheckPasswordAsync(user, "Replacement-Password8@"));
        Assert.False(user.MustChangePassword);
        Assert.NotEqual(originalStamp, user.SecurityStamp);
        jwt.Verify(service => service.RevokeAllSessionsAsync(user.Id, It.IsAny<string?>(), "Password reset"), Times.Once);
        Assert.Single(await context.AuditTrails.Where(item => item.Action == "PasswordResetCompleted").ToArrayAsync());
    }

    private static string? Message(ActionResult<ApiResponse<bool>> result) =>
        Assert.IsType<ApiResponse<bool>>(Assert.IsType<OkObjectResult>(result.Result).Value).Message;

    private static AuthController CreateController(
        UserManager<ApplicationUser> users,
        IJwtService jwt,
        ApplicationDbContext context,
        IPasswordResetNotifier notifier,
        IServiceProvider services) => new(users, null!, jwt, null!, context, Options.Create(new JwtSettings()), notifier,
            Mock.Of<IAuthenticationPolicyResolver>(), Mock.Of<ITenantContext>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services,
                    TraceIdentifier = "password-reset-test"
                }
            }
        };

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "FTCERP.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

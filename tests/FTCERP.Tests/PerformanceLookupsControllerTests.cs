using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Tests;

public sealed class PerformanceLookupsControllerTests
{
    [Fact]
    public async Task Get_returns_only_active_database_catalogue_values_for_authorized_kpi_reader()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("lookup-reader");
        context.Periods.AddRange(
            new Period { Code = "Q1", Name = "Quarter 1", FiscalYear = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 9, 30), IsActive = true },
            new Period { Code = "OLD", Name = "Retired period", FiscalYear = "2025/26", StartDate = new DateTime(2025, 7, 1), EndDate = new DateTime(2025, 9, 30), IsActive = false });
        var goal = new StrategicGoal { Code = "GOAL", Name = "Live strategic goal", IsActive = true };
        context.StrategicGoals.Add(goal);
        await context.SaveChangesAsync();
        context.StrategicObjectives.Add(new StrategicObjective { Code = "OBJ", Name = "Live objective", StrategicGoalId = goal.Id, IsActive = true });
        context.BudgetSources.Add(new BudgetSource { Code = "OWN", Name = "Own revenue", IsActive = true });
        context.BudgetTypes.Add(new BudgetType { Code = "CAPEX", Name = "Capital", IsActive = true });
        context.UnitOfMeasures.Add(new UnitOfMeasure { Code = "PCT", Name = "Percentage", Symbol = "%", IsActive = true });
        await context.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(true, true, [], [], [], [], [], []));
        access.Setup(service => service.GetQueryScopeAsync(user, "IPMS_KPI.READ"))
            .ReturnsAsync(new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var controller = Controller(context, user, access.Object);

        var response = await controller.Get();

        var envelope = Assert.IsType<ApiResponse<PerformanceLookupsDto>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal("Q1", Assert.Single(envelope.Data!.Periods).Code);
        Assert.Equal("Live strategic goal", Assert.Single(envelope.Data.StrategicGoals).Name);
        Assert.Equal(goal.Id, Assert.Single(envelope.Data.StrategicObjectives).StrategicGoalId);
        Assert.Equal("Own revenue", Assert.Single(envelope.Data.BudgetSources).Name);
        Assert.Equal("Capital", Assert.Single(envelope.Data.BudgetTypes).Name);
        Assert.Equal("%", Assert.Single(envelope.Data.UnitsOfMeasure).Symbol);
    }

    [Fact]
    public async Task Get_denies_authenticated_user_without_kpi_read_permission()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("lookup-denied");
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, It.IsAny<string>()))
            .ReturnsAsync(new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var controller = Controller(context, user, access.Object);

        var response = await controller.Get();

        Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(403, ((ObjectResult)response.Result!).StatusCode);
    }

    private static PerformanceLookupsController Controller(
        FTCERP.Host.Infrastructure.Persistence.ApplicationDbContext context,
        ApplicationUser user,
        IAccessControlService access) => new(context, IdpTestFixture.CreateUserManagerMock(user).Object, access)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = IdpTestFixture.CreatePrincipal(user.Id) }
            }
        };
}

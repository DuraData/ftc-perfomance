using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Tests;

public sealed class TenantMastersControllerTests
{
    [Fact]
    public async Task EmployeeEmail_member_permission_redacts_reads_and_rejects_direct_updates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new TestTenantContext(81, "employee-reader");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Id = 81, Code = "M81", Name = "Municipality 81" };
        var user = IdpTestFixture.CreateUser(tenant.UserId!);
        user.MunicipalityId = municipality.Id;
        var employee = new MunicipalEmployee { MunicipalityId = municipality.Id, EmployeeNumber = "E081", FirstName = "Protected", LastName = "Employee", EmailAddress = "private@example.test", EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        context.AddRange(municipality, user, employee);
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.EmailAddress.READ", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.EmailAddress.UPDATE", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        var controller = new TenantMastersController(context, tenant, access.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var readResult = await controller.GetEmployees();
        var readEnvelope = Assert.IsType<ApiResponse<EmployeeDto[]>>(Assert.IsType<OkObjectResult>(readResult.Result).Value);
        Assert.Null(Assert.Single(readEnvelope.Data!).EmailAddress);

        var updateResult = await controller.UpdateEmployee(employee.PublicId, new UpdateEmployeeRequest(
            employee.FirstName, employee.LastName, "exfiltration@example.test", null, true,
            employee.EffectiveFrom, null, Convert.ToBase64String(employee.RowVersion)));

        Assert.IsType<ForbidResult>(updateResult.Result);
        Assert.Equal("private@example.test", (await context.MunicipalEmployees.SingleAsync()).EmailAddress);
    }

    [Fact]
    public async Task Sqlite_relational_constraints_and_tenant_filters_match_provider_neutral_contract()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var system = new TestTenantContext(null, "system", true);
        long tenantAId;
        long tenantBId;
        await using (var setup = new ApplicationDbContext(options, system))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "SQLITE-A", Name = "SQLite Tenant A", RowVersion = [1] };
            var tenantB = new Municipality { Code = "SQLITE-B", Name = "SQLite Tenant B", RowVersion = [1] };
            setup.Municipalities.AddRange(tenantA, tenantB);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            tenantBId = tenantB.Id;
            setup.MunicipalEmployees.AddRange(
                new MunicipalEmployee { MunicipalityId = tenantAId, EmployeeNumber = "E-SQLITE", FirstName = "Ada", LastName = "A", EffectiveFrom = DateTime.UtcNow, RowVersion = [1] },
                new MunicipalEmployee { MunicipalityId = tenantBId, EmployeeNumber = "E-SQLITE", FirstName = "Ben", LastName = "B", EffectiveFrom = DateTime.UtcNow, RowVersion = [1] });
            await setup.SaveChangesAsync();
            setup.MunicipalEmployees.Add(new MunicipalEmployee { MunicipalityId = tenantAId, EmployeeNumber = "E-SQLITE", FirstName = "Duplicate", LastName = "A", EffectiveFrom = DateTime.UtcNow, RowVersion = [1] });
            await Assert.ThrowsAsync<DbUpdateException>(() => setup.SaveChangesAsync());
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TestTenantContext(tenantAId, "tenant-a"));
        await using var tenantBContext = new ApplicationDbContext(options, new TestTenantContext(tenantBId, "tenant-b"));
        Assert.Single(await tenantAContext.MunicipalEmployees.AsNoTracking().ToArrayAsync());
        Assert.Single(await tenantBContext.MunicipalEmployees.AsNoTracking().ToArrayAsync());
        Assert.NotEqual((await tenantAContext.MunicipalEmployees.SingleAsync()).MunicipalityId, (await tenantBContext.MunicipalEmployees.SingleAsync()).MunicipalityId);
    }

    [Fact]
    public async Task CloseAssignment_preserves_row_and_writes_reasoned_audit()
    {
        var tenant = new TestTenantContext(41, "administrator-1");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = 41, Code = "M41", Name = "Municipality 41" };
        var department = new Department { Id = 7, MunicipalityId = 41, Code = "FIN", Name = "Finance" };
        var employee = new MunicipalEmployee
        {
            Id = 11,
            MunicipalityId = 41,
            EmployeeNumber = "E001",
            FirstName = "Ada",
            LastName = "Mokoena",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        };
        var assignment = new EmployeeAssignment
        {
            Id = 13,
            MunicipalityId = 41,
            MunicipalEmployee = employee,
            MunicipalEmployeeId = employee.Id,
            Department = department,
            DepartmentId = department.Id,
            PositionCode = "CFO",
            PositionName = "Chief Financial Officer",
            EffectiveFrom = DateTime.UtcNow.AddMonths(-3),
            RowVersion = [1]
        };
        context.AddRange(municipality, department, employee, assignment);
        await context.SaveChangesAsync();

        var controller = CreateController(context, tenant);
        var effectiveTo = DateTime.UtcNow.AddDays(-1);
        var response = await controller.CloseAssignment(
            assignment.PublicId,
            new CloseEmployeeAssignmentRequest(effectiveTo, "Employee transferred to another directorate", Convert.ToBase64String(assignment.RowVersion)));

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var envelope = Assert.IsType<ApiResponse<EmployeeAssignmentDto>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.False(envelope.Data!.IsActive);
        Assert.Equal(effectiveTo, envelope.Data.EffectiveTo);
        Assert.Single(await context.EmployeeAssignments.IgnoreQueryFilters().Where(x => x.Id == assignment.Id).ToArrayAsync());
        var audit = Assert.Single(await context.AuditTrails.IgnoreQueryFilters().Where(x => x.EntityName == nameof(EmployeeAssignment)).ToArrayAsync());
        Assert.Equal("Close", audit.Action);
        Assert.Equal("Employee transferred to another directorate", audit.Reason);
        Assert.Equal(41, audit.MunicipalityId);
    }

    [Fact]
    public async Task DeactivateEmployee_rejects_active_assignments()
    {
        var tenant = new TestTenantContext(51, "administrator-2");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = 51, Code = "M51", Name = "Municipality 51" };
        var department = new Department { Id = 17, MunicipalityId = 51, Code = "CORP", Name = "Corporate Services" };
        var employee = new MunicipalEmployee { Id = 21, MunicipalityId = 51, EmployeeNumber = "E002", FirstName = "Sam", LastName = "Ndlovu", EffectiveFrom = DateTime.UtcNow.AddYears(-1), RowVersion = [2] };
        context.AddRange(municipality, department, employee, new EmployeeAssignment { MunicipalityId = 51, MunicipalEmployee = employee, Department = department, PositionCode = "DIR", PositionName = "Director", EffectiveFrom = DateTime.UtcNow.AddMonths(-2) });
        await context.SaveChangesAsync();

        var controller = CreateController(context, tenant);
        var response = await controller.UpdateEmployee(employee.PublicId, new UpdateEmployeeRequest(employee.FirstName, employee.LastName, null, null, false, employee.EffectiveFrom, DateTime.UtcNow, Convert.ToBase64String(employee.RowVersion)));

        var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
        var envelope = Assert.IsType<ApiResponse<EmployeeDto>>(conflict.Value);
        Assert.Contains("End all active employee assignments", envelope.Message);
        Assert.True((await context.MunicipalEmployees.IgnoreQueryFilters().SingleAsync(x => x.Id == employee.Id)).IsActive);
    }

    private static TenantMastersController CreateController(ApplicationDbContext context, ITenantContext tenant) => new(context, tenant)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private sealed class TestTenantContext(long? municipalityId, string userId, bool isSystem = false) : ITenantContext
    {
        public long? MunicipalityId => municipalityId;
        public bool IsSystem => isSystem;
        public string? UserId => userId;
    }
}

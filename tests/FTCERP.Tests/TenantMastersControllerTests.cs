using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
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
    public async Task Calendar_master_pages_filter_before_count_and_stay_tenant_scoped()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        FinancialYear[] years;
        MunicipalityFinancialYear[] municipalYears;
        await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "system", true)))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Municipalities.AddRange(new Municipality { Id = 111, Code = "CAL-A", Name = "Calendar A" }, new Municipality { Id = 112, Code = "CAL-B", Name = "Calendar B" });
            years = Enumerable.Range(1, 31).Select(index => new FinancialYear
            {
                Code = $"FY{index:000}", Name = $"Financial Year {index:000}", StartDate = new DateTime(2000 + index, 7, 1), EndDate = new DateTime(2001 + index, 6, 30), IsActive = index != 30
            }).ToArray();
            setup.FinancialYears.AddRange(years);
            await setup.SaveChangesAsync();
            municipalYears = years.Select((year, index) => new MunicipalityFinancialYear
            {
                MunicipalityId = 111, FinancialYearId = year.Id, FinancialYear = year, EffectiveFrom = year.StartDate, IsCurrent = index == 30, IsActive = index != 29
            }).ToArray();
            setup.MunicipalityFinancialYears.AddRange(municipalYears);
            setup.MunicipalityFinancialYears.Add(new MunicipalityFinancialYear { MunicipalityId = 112, FinancialYearId = years[0].Id, FinancialYear = years[0], EffectiveFrom = years[0].StartDate });
            await setup.SaveChangesAsync();
            setup.ReportingPeriods.AddRange(municipalYears.Select((year, index) => new ReportingPeriod
            {
                MunicipalityFinancialYearId = year.Id, Code = "Q1", Name = $"Quarter One {index + 1:000}", PeriodType = ReportingPeriodType.Quarter1,
                Sequence = 1, StartDate = year.FinancialYear.StartDate, EndDate = year.FinancialYear.StartDate.AddMonths(3).AddDays(-1), IsActive = index != 29
            }));
            setup.SdbipLayers.AddRange(municipalYears.Select((year, index) => new SdbipLayer
            {
                MunicipalityId = 111, MunicipalityFinancialYearId = year.Id, Code = "TOP", Name = $"Top Layer {index + 1:000}", Description = index == 6 ? "Searchable layer" : null,
                DisplayOrder = index + 1, IsActive = index != 29
            }));
            await setup.SaveChangesAsync();
        }

        var tenant = new TestTenantContext(111, "calendar-reader");
        await using var context = new ApplicationDbContext(options, tenant);
        var controller = CreateController(context, tenant);
        var yearsResult = await controller.GetFinancialYearsPage(new PagedQueryRequest { Page = 2, PageSize = 10, Search = "Financial Year", SortBy = "code", SortDirection = "asc" });
        var yearsPage = Assert.IsType<ApiResponse<PagedResponse<FinancialYearDto>>>(Assert.IsType<OkObjectResult>(yearsResult.Result).Value).Data!;
        Assert.Equal(31, yearsPage.TotalCount);
        Assert.Equal("FY011", yearsPage.Items[0].Code);

        var municipalResult = await controller.GetMunicipalityFinancialYearsPage(new PagedQueryRequest { PageSize = 10, Search = "Financial Year", SortBy = "startDate", SortDirection = "desc" });
        var municipalPage = Assert.IsType<ApiResponse<PagedResponse<MunicipalityFinancialYearDto>>>(Assert.IsType<OkObjectResult>(municipalResult.Result).Value).Data!;
        Assert.Equal(31, municipalPage.TotalCount);
        Assert.Equal("FY031", municipalPage.Items[0].Code);
        var inactiveMunicipal = await controller.GetMunicipalityFinancialYearsPage(new PagedQueryRequest { SortBy = "code", SortDirection = "asc" }, false);
        Assert.Equal(1, Assert.IsType<ApiResponse<PagedResponse<MunicipalityFinancialYearDto>>>(Assert.IsType<OkObjectResult>(inactiveMunicipal.Result).Value).Data!.TotalCount);

        var periodResult = await controller.GetReportingPeriodsPage(new PagedQueryRequest { Search = "Quarter One", SortBy = "startDate", SortDirection = "desc", ReportingPeriodType = ReportingPeriodType.Quarter1 }, municipalYears[6].PublicId, true);
        var periodPage = Assert.IsType<ApiResponse<PagedResponse<ReportingPeriodDto>>>(Assert.IsType<OkObjectResult>(periodResult.Result).Value).Data!;
        Assert.Equal(1, periodPage.TotalCount);
        Assert.Equal(years[6].StartDate, Assert.Single(periodPage.Items).StartDate);
        var layerResult = await controller.GetSdbipLayersPage(new PagedQueryRequest { Search = "Searchable", SortBy = "displayOrder", SortDirection = "asc" });
        var layerPage = Assert.IsType<ApiResponse<PagedResponse<SdbipLayerDto>>>(Assert.IsType<OkObjectResult>(layerResult.Result).Value).Data!;
        Assert.Equal(1, layerPage.TotalCount);
        Assert.Equal("Top Layer 007", Assert.Single(layerPage.Items).Name);
    }

    [Fact]
    public async Task Calendar_master_pages_reject_unknown_sorts()
    {
        var tenant = new TestTenantContext(113, "calendar-reader");
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        var controller = CreateController(context, tenant);
        var request = new PagedQueryRequest { SortBy = "unsafe" };

        Assert.IsType<BadRequestObjectResult>((await controller.GetFinancialYearsPage(request)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetMunicipalityFinancialYearsPage(request)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetReportingPeriodsPage(request)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetSdbipLayersPage(request)).Result);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetFinancialYears().Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetMunicipalityFinancialYears().Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetReportingPeriods().Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetSdbipLayers().Result).StatusCode);
    }

    [Fact]
    public async Task Employee_sensitive_member_permissions_redact_reads_and_reject_direct_updates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new TestTenantContext(81, "employee-reader");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Id = 81, Code = "M81", Name = "Municipality 81" };
        var otherMunicipality = new Municipality { Id = 82, Code = "M82", Name = "Municipality 82" };
        var user = IdpTestFixture.CreateUser(tenant.UserId!);
        user.MunicipalityId = municipality.Id;
        var foreignUser = IdpTestFixture.CreateUser("foreign-login");
        foreignUser.MunicipalityId = otherMunicipality.Id;
        var employee = new MunicipalEmployee { MunicipalityId = municipality.Id, EmployeeNumber = "E081", FirstName = "Protected", LastName = "Employee", EmailAddress = "private@example.test", IdentityUserId = user.Id, EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        context.AddRange(municipality, otherMunicipality, user, foreignUser, employee);
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.EmailAddress.READ", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.EmailAddress.UPDATE", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.EmployeeNumber.READ", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.EmployeeNumber.UPDATE", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.IdentityUserId.READ", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.IdentityUserId.UPDATE", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        var controller = new TenantMastersController(context, tenant, access.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var pageResult = await controller.GetEmployeesPage(new PagedQueryRequest { Page = 1, PageSize = 10, SortBy = "name", SortDirection = "asc" });
        var pageEnvelope = Assert.IsType<ApiResponse<PagedResponse<EmployeeDto>>>(Assert.IsType<OkObjectResult>(pageResult.Result).Value);
        var protectedEmployee = Assert.Single(pageEnvelope.Data!.Items);
        Assert.Null(protectedEmployee.EmployeeNumber);
        Assert.Null(protectedEmployee.EmailAddress);
        Assert.Null(protectedEmployee.IdentityUserId);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetEmployees().Result).StatusCode);
        var hiddenEmailSearch = await controller.GetEmployeesPage(new PagedQueryRequest { Search = "private@example.test", SortBy = "name", SortDirection = "asc" });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<EmployeeDto>>>(Assert.IsType<OkObjectResult>(hiddenEmailSearch.Result).Value).Data!.TotalCount);
        var hiddenNumberSearch = await controller.GetEmployeesPage(new PagedQueryRequest { Search = "E081", SortBy = "name", SortDirection = "asc" });
        Assert.Equal(0, Assert.IsType<ApiResponse<PagedResponse<EmployeeDto>>>(Assert.IsType<OkObjectResult>(hiddenNumberSearch.Result).Value).Data!.TotalCount);
        Assert.IsType<ForbidResult>((await controller.GetEmployeesPage(new PagedQueryRequest { SortBy = "employeeNumber" })).Result);
        Assert.IsType<ForbidResult>((await controller.GetEmployeesPage(new PagedQueryRequest { SortBy = "email" })).Result);

        var updateResult = await controller.UpdateEmployee(employee.PublicId, new UpdateEmployeeRequest(
            employee.FirstName, employee.LastName, "exfiltration@example.test", null, true,
            employee.EffectiveFrom, null, Convert.ToBase64String(employee.RowVersion)));

        Assert.IsType<ForbidResult>(updateResult.Result);
        Assert.Equal("private@example.test", (await context.MunicipalEmployees.SingleAsync()).EmailAddress);

        var identityUpdate = await controller.UpdateEmployee(employee.PublicId, new UpdateEmployeeRequest(
            employee.FirstName, employee.LastName, null, "different-login", true,
            employee.EffectiveFrom, null, Convert.ToBase64String(employee.RowVersion), false, true));
        Assert.IsType<ForbidResult>(identityUpdate.Result);
        Assert.Equal(user.Id, (await context.MunicipalEmployees.SingleAsync()).IdentityUserId);

        access.Setup(item => item.CheckPermissionAsync(user, "EMPLOYEE.IdentityUserId.UPDATE", It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        var crossTenantLink = await controller.UpdateEmployee(employee.PublicId, new UpdateEmployeeRequest(
            employee.FirstName, employee.LastName, null, foreignUser.Id, true,
            employee.EffectiveFrom, null, Convert.ToBase64String(employee.RowVersion), false, true));
        Assert.IsType<BadRequestObjectResult>(crossTenantLink.Result);
        Assert.Equal(user.Id, (await context.MunicipalEmployees.SingleAsync()).IdentityUserId);
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
    public async Task Employee_directory_page_is_bounded_searchable_and_tenant_scoped()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using (var setup = new ApplicationDbContext(options, new TestTenantContext(null, "system", true)))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Municipalities.AddRange(new Municipality { Id = 91, Code = "M91", Name = "Municipality 91" }, new Municipality { Id = 92, Code = "M92", Name = "Municipality 92" });
            setup.MunicipalEmployees.AddRange(Enumerable.Range(1, 31).Select(index => new MunicipalEmployee
            {
                MunicipalityId = 91, EmployeeNumber = $"E{index:000}", FirstName = $"Person{index:000}", LastName = "Local",
                EmailAddress = $"person{index:000}@example.test", EffectiveFrom = DateTime.UtcNow.AddYears(-1)
            }));
            setup.MunicipalEmployees.AddRange(
                new MunicipalEmployee { MunicipalityId = 91, EmployeeNumber = "EXPIRED", FirstName = "Expired", LastName = "NotCurrent", EffectiveFrom = DateTime.UtcNow.AddYears(-1), EffectiveTo = DateTime.UtcNow.AddDays(-1), IsActive = true },
                new MunicipalEmployee { MunicipalityId = 91, EmployeeNumber = "FUTURE", FirstName = "Future", LastName = "NotCurrent", EffectiveFrom = DateTime.UtcNow.AddDays(1), IsActive = true });
            setup.MunicipalEmployees.Add(new MunicipalEmployee { MunicipalityId = 92, EmployeeNumber = "FOREIGN", FirstName = "Foreign", LastName = "Employee", EffectiveFrom = DateTime.UtcNow });
            await setup.SaveChangesAsync();
        }
        var tenant = new TestTenantContext(91, "directory-reader");
        await using var context = new ApplicationDbContext(options, tenant);
        context.Users.Add(IdpTestFixture.CreateUser(tenant.UserId!));
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        var controller = new TenantMastersController(context, tenant, access.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetEmployeesPage(new PagedQueryRequest { Page = 2, PageSize = 10, Search = "Local", SortBy = "employeeNumber", SortDirection = "asc" });

        var payload = Assert.IsType<ApiResponse<PagedResponse<EmployeeDto>>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(31, payload.TotalCount);
        Assert.Equal(10, payload.Items.Length);
        Assert.Equal(4, payload.TotalPages);
        Assert.DoesNotContain(payload.Items, item => item.EmployeeNumber == "FOREIGN");
        var activeResult = await controller.GetEmployeesPage(new PagedQueryRequest { PageSize = 100, SortBy = "name", SortDirection = "asc" }, true);
        var activePayload = Assert.IsType<ApiResponse<PagedResponse<EmployeeDto>>>(Assert.IsType<OkObjectResult>(activeResult.Result).Value).Data!;
        Assert.Equal(31, activePayload.TotalCount);
        Assert.DoesNotContain(activePayload.Items, item => item.EmployeeNumber is "EXPIRED" or "FUTURE");
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
        var historicalAssignment = new EmployeeAssignment
        {
            Id = 14,
            MunicipalityId = 41,
            MunicipalEmployee = employee,
            MunicipalEmployeeId = employee.Id,
            Department = department,
            DepartmentId = department.Id,
            PositionCode = "ANL",
            PositionName = "Financial Analyst",
            EffectiveFrom = DateTime.UtcNow.AddYears(-1),
            EffectiveTo = DateTime.UtcNow.AddMonths(-4),
            IsActive = false,
            RowVersion = [2]
        };
        context.AddRange(municipality, department, employee, assignment, historicalAssignment);
        await context.SaveChangesAsync();

        var controller = CreateController(context, tenant);
        var firstPageResult = await controller.GetAssignmentsPage(employee.PublicId, new PagedQueryRequest
        {
            Page = 1, PageSize = 1, SortBy = "effectiveFrom", SortDirection = "desc"
        });
        var firstPage = Assert.IsType<ApiResponse<PagedResponse<EmployeeAssignmentDto>>>(Assert.IsType<OkObjectResult>(firstPageResult.Result).Value).Data!;
        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(assignment.PublicId, Assert.Single(firstPage.Items).PublicId);
        var filteredResult = await controller.GetAssignmentsPage(employee.PublicId, new PagedQueryRequest
        {
            Page = 1, PageSize = 10, Search = "Analyst", SortBy = "position", SortDirection = "asc"
        });
        var filtered = Assert.IsType<ApiResponse<PagedResponse<EmployeeAssignmentDto>>>(Assert.IsType<OkObjectResult>(filteredResult.Result).Value).Data!;
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal(historicalAssignment.PublicId, Assert.Single(filtered.Items).PublicId);
        Assert.IsType<BadRequestObjectResult>((await controller.GetAssignmentsPage(employee.PublicId, new PagedQueryRequest { SortBy = "raw-sql" })).Result);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetAssignments(employee.PublicId).Result).StatusCode);
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

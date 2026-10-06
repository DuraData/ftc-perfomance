using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;

namespace FTCERP.Tests;

public sealed class OrganizationMastersControllerTests
{
    [Fact]
    public async Task Organization_and_reference_pages_filter_before_count_and_stay_tenant_scoped()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        Guid scopedYearPublicId;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Municipalities.AddRange(new Municipality { Id = 101, Code = "PAGE-A", Name = "Paging A" }, new Municipality { Id = 102, Code = "PAGE-B", Name = "Paging B" });
            await setup.SaveChangesAsync();
            var financialYears = new[]
            {
                new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) },
                new FinancialYear { Code = "2027/28", Name = "2027/28", StartDate = new DateTime(2027, 7, 1), EndDate = new DateTime(2028, 6, 30) }
            };
            setup.FinancialYears.AddRange(financialYears);
            await setup.SaveChangesAsync();
            var municipalityYears = new[]
            {
                new MunicipalityFinancialYear { MunicipalityId = 101, FinancialYearId = financialYears[0].Id, IsActive = true, IsCurrent = true, EffectiveFrom = financialYears[0].StartDate },
                new MunicipalityFinancialYear { MunicipalityId = 101, FinancialYearId = financialYears[1].Id, IsActive = true, EffectiveFrom = financialYears[1].StartDate }
            };
            setup.MunicipalityFinancialYears.AddRange(municipalityYears);
            await setup.SaveChangesAsync();
            scopedYearPublicId = municipalityYears[0].PublicId;
            var departments = Enumerable.Range(1, 31).Select(index => new Department
            {
                MunicipalityId = 101, Code = $"D{index:000}", Name = $"Department {index:000}", IsActive = index != 30, EffectiveFrom = DateTime.UtcNow.AddYears(-1)
            }).ToArray();
            setup.Departments.AddRange(departments);
            setup.Departments.Add(new Department { MunicipalityId = 102, Code = "FOREIGN", Name = "Foreign department", EffectiveFrom = DateTime.UtcNow.AddYears(-1) });
            await setup.SaveChangesAsync();
            var parent = departments[0];
            setup.Units.AddRange(Enumerable.Range(1, 31).Select(index => new Unit { MunicipalityId = 101, DepartmentId = parent.Id, Code = $"U{index:000}", Name = $"Unit {index:000}", EffectiveFrom = DateTime.UtcNow.AddYears(-1) }));
            setup.Positions.AddRange(Enumerable.Range(1, 31).Select(index => new Position { MunicipalityId = 101, DepartmentId = parent.Id, Code = $"P{index:000}", Name = $"Position {index:000}", Grade = index == 7 ? "SEARCH-GRADE" : null, EffectiveFrom = DateTime.UtcNow.AddYears(-1) }));
            setup.Wards.AddRange(Enumerable.Range(1, 31).Select(index => new Ward { MunicipalityId = 101, LegacyMunicipality = "Paging A", Code = $"W{index:000}", Name = $"Ward {index:000}", EffectiveFrom = DateTime.UtcNow.AddYears(-1) }));
            setup.VoteNumbers.AddRange(Enumerable.Range(1, 31).Select(index => new VoteNumber { MunicipalityId = 101, MunicipalityFinancialYearId = index <= 15 ? municipalityYears[0].Id : municipalityYears[1].Id, DepartmentId = parent.Id, Code = $"V{index:000}", Number = $"{index:000}", Name = $"Vote {index:000}", Amount = index, EffectiveFrom = DateTime.UtcNow.AddYears(-1) }));
            await setup.SaveChangesAsync();
        }

        var tenant = new TenantContext(101, "master-reader");
        await using var context = new ApplicationDbContext(options, tenant);
        var controller = CreateController(context, tenant);
        var departmentsResult = await controller.GetDepartmentsPage(new PagedQueryRequest { Page = 2, PageSize = 10, Search = "Department", SortBy = "code", SortDirection = "asc" });
        var departmentsPage = Assert.IsType<ApiResponse<PagedResponse<DepartmentMasterDto>>>(Assert.IsType<OkObjectResult>(departmentsResult.Result).Value).Data!;
        Assert.Equal(31, departmentsPage.TotalCount);
        Assert.Equal(10, departmentsPage.Items.Length);
        Assert.Equal("D011", departmentsPage.Items[0].Code);
        Assert.DoesNotContain(departmentsPage.Items, item => item.Code == "FOREIGN");

        var inactiveResult = await controller.GetDepartmentsPage(new PagedQueryRequest { SortBy = "name", SortDirection = "asc" }, false);
        var inactivePage = Assert.IsType<ApiResponse<PagedResponse<DepartmentMasterDto>>>(Assert.IsType<OkObjectResult>(inactiveResult.Result).Value).Data!;
        Assert.Equal(1, inactivePage.TotalCount);
        Assert.Equal("D030", Assert.Single(inactivePage.Items).Code);

        var unitsResult = await controller.GetUnitsPage(new PagedQueryRequest { PageSize = 5, Search = "Unit", SortBy = "department", SortDirection = "asc" }, null, departmentsPage.Items[0].PublicId);
        var unitsPage = Assert.IsType<ApiResponse<PagedResponse<UnitMasterDto>>>(Assert.IsType<OkObjectResult>(unitsResult.Result).Value).Data!;
        Assert.Equal(0, unitsPage.TotalCount);
        var positionsResult = await controller.GetPositionsPage(new PagedQueryRequest { Search = "SEARCH-GRADE", SortBy = "grade", SortDirection = "asc" });
        Assert.Equal(1, Assert.IsType<ApiResponse<PagedResponse<PositionMasterDto>>>(Assert.IsType<OkObjectResult>(positionsResult.Result).Value).Data!.TotalCount);
        var wardsResult = await controller.GetWardsPage(new PagedQueryRequest { PageSize = 7, Search = "Ward", SortBy = "code", SortDirection = "desc" });
        Assert.Equal(31, Assert.IsType<ApiResponse<PagedResponse<WardMasterDto>>>(Assert.IsType<OkObjectResult>(wardsResult.Result).Value).Data!.TotalCount);
        var votesResult = await controller.GetVoteNumbersPage(new PagedQueryRequest { PageSize = 5, SortBy = "amount", SortDirection = "desc" });
        var votesPage = Assert.IsType<ApiResponse<PagedResponse<VoteNumberMasterDto>>>(Assert.IsType<OkObjectResult>(votesResult.Result).Value).Data!;
        Assert.Equal(31m, votesPage.Items[0].Amount);
        Assert.Equal(31, votesPage.TotalCount);
        var yearVotesResult = await controller.GetVoteNumbersPage(new PagedQueryRequest { PageSize = 25, SortBy = "code" }, municipalityFinancialYearPublicId: scopedYearPublicId);
        var yearVotesPage = Assert.IsType<ApiResponse<PagedResponse<VoteNumberMasterDto>>>(Assert.IsType<OkObjectResult>(yearVotesResult.Result).Value).Data!;
        Assert.Equal(15, yearVotesPage.TotalCount);
        Assert.All(yearVotesPage.Items, item => Assert.Equal(scopedYearPublicId, item.MunicipalityFinancialYearPublicId));
    }

    [Fact]
    public async Task Organization_pages_reject_unknown_sort_and_compatibility_reads_preserve_existing_consumers()
    {
        var tenant = new TenantContext(103, "master-reader");
        await using var context = NewContext(tenant);
        context.Municipalities.Add(new Municipality { Id = 103, Code = "CAP", Name = "Capped" });
        context.Wards.AddRange(Enumerable.Range(1, 105).Select(index => new Ward { MunicipalityId = 103, LegacyMunicipality = "Capped", Code = $"W{index:000}", Name = $"Ward {index:000}" }));
        await context.SaveChangesAsync();
        var controller = CreateController(context, tenant);

        Assert.IsType<BadRequestObjectResult>((await controller.GetDepartmentsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetUnitsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetPositionsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetWardsPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetVoteNumbersPage(new PagedQueryRequest { SortBy = "unsafe" })).Result);
        var legacy = Assert.IsType<ApiResponse<WardMasterDto[]>>(Assert.IsType<OkObjectResult>((await controller.GetWards()).Result).Value).Data!;
        Assert.Equal(105, legacy.Length);
    }

    [Fact]
    public async Task Sqlite_enforces_tenant_scoped_ward_and_vote_number_uniqueness_and_filters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "REF-A", Name = "Reference A", RowVersion = [1] };
            var tenantB = new Municipality { Code = "REF-B", Name = "Reference B", RowVersion = [1] };
            setup.AddRange(tenantA, tenantB); await setup.SaveChangesAsync(); tenantAId = tenantA.Id; tenantBId = tenantB.Id;
            var departmentA = new Department { MunicipalityId = tenantAId, Code = "FIN", Name = "Finance A", RowVersion = [1] };
            var departmentB = new Department { MunicipalityId = tenantBId, Code = "FIN", Name = "Finance B", RowVersion = [1] };
            setup.AddRange(departmentA, departmentB); await setup.SaveChangesAsync();
            setup.Wards.AddRange(
                new Ward { MunicipalityId = tenantAId, LegacyMunicipality = tenantA.Name, Code = "W01", Name = "Ward A", RowVersion = [1] },
                new Ward { MunicipalityId = tenantBId, LegacyMunicipality = tenantB.Name, Code = "W01", Name = "Ward B", RowVersion = [1] });
            setup.VoteNumbers.AddRange(
                new VoteNumber { MunicipalityId = tenantAId, DepartmentId = departmentA.Id, Code = "V01", Number = "1", Name = "Vote A", RowVersion = [1] },
                new VoteNumber { MunicipalityId = tenantBId, DepartmentId = departmentB.Id, Code = "V01", Number = "1", Name = "Vote B", RowVersion = [1] });
            await setup.SaveChangesAsync();
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TenantContext(tenantAId, "tenant-a"));
        await using var tenantBContext = new ApplicationDbContext(options, new TenantContext(tenantBId, "tenant-b"));
        Assert.Equal("Ward A", (await tenantAContext.Wards.AsNoTracking().SingleAsync()).Name);
        Assert.Equal("Ward B", (await tenantBContext.Wards.AsNoTracking().SingleAsync()).Name);
        Assert.Equal("Vote A", (await tenantAContext.VoteNumbers.AsNoTracking().SingleAsync()).Name);
        Assert.Equal("Vote B", (await tenantBContext.VoteNumbers.AsNoTracking().SingleAsync()).Name);

        await using var staleContext = new ApplicationDbContext(options, new TenantContext(tenantAId, "stale-editor"));
        var currentWard = await tenantAContext.Wards.SingleAsync();
        var staleWard = await staleContext.Wards.SingleAsync();
        currentWard.Name = "Ward A updated";
        await tenantAContext.SaveChangesAsync();
        staleWard.Name = "Stale overwrite";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());

        tenantAContext.Wards.Add(new Ward { MunicipalityId = tenantAId, LegacyMunicipality = "Reference A", Code = "W01", Name = "Duplicate", RowVersion = [1] });
        await Assert.ThrowsAsync<DbUpdateException>(() => tenantAContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CreateWard_and_vote_number_persist_tenant_scope_and_reasoned_audit()
    {
        var tenant = new TenantContext(75, "reference-admin");
        await using var context = NewContext(tenant);
        var municipality = new Municipality { Id = 75, Code = "M75", Name = "Municipality 75" };
        var department = new Department { Id = 11, MunicipalityId = 75, Code = "FIN", Name = "Finance" };
        var financialYear = new FinancialYear { Id = 751, Code = "2026-2027", Name = "2026/2027", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) };
        var municipalityYear = new MunicipalityFinancialYear { Id = 752, MunicipalityId = 75, Municipality = municipality, FinancialYearId = financialYear.Id, FinancialYear = financialYear, IsActive = true, EffectiveFrom = financialYear.StartDate };
        context.AddRange(municipality, department, financialYear, municipalityYear); await context.SaveChangesAsync();
        var controller = CreateController(context, tenant);

        var wardResponse = await controller.CreateWard(new SaveWardMasterRequest(" w01 ", "Ward One", true, DateTime.UtcNow.Date, null, "Approved municipal demarcation"));
        var voteResponse = await controller.CreateVoteNumber(new SaveVoteNumberMasterRequest(department.PublicId, municipalityYear.PublicId, " v01 ", "001", "Operating Vote", 1250m, true, DateTime.UtcNow.Date, null, "Approved annual budget structure"));

        Assert.Equal("W01", Assert.IsType<ApiResponse<WardMasterDto>>(Assert.IsType<OkObjectResult>(wardResponse.Result).Value).Data!.Code);
        var vote = Assert.IsType<ApiResponse<VoteNumberMasterDto>>(Assert.IsType<OkObjectResult>(voteResponse.Result).Value).Data!;
        Assert.Equal("V01", vote.Code);
        Assert.Equal(municipalityYear.PublicId, vote.MunicipalityFinancialYearPublicId);
        Assert.Equal("2026-2027", vote.FinancialYearCode);
        Assert.All(await context.AuditTrails.ToArrayAsync(), row => Assert.Equal(75, row.MunicipalityId));
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), row => row.EntityName == nameof(Ward) && row.Reason == "Approved municipal demarcation");
        Assert.Contains(await context.AuditTrails.ToArrayAsync(), row => row.EntityName == nameof(VoteNumber) && row.Reason == "Approved annual budget structure");
    }

    [Fact]
    public async Task Sqlite_enforces_vote_number_uniqueness_per_municipality_financial_year_and_tenant_year_fk()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        int departmentId;
        long[] municipalityYearIds;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var department = new Department { MunicipalityId = 201, Code = "FIN", Name = "Finance" };
            var years = new[]
            {
                new FinancialYear { Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) },
                new FinancialYear { Code = "2027/28", Name = "2027/28", StartDate = new DateTime(2027, 7, 1), EndDate = new DateTime(2028, 6, 30) }
            };
            setup.AddRange(new Municipality { Id = 201, Code = "VOTE-A", Name = "Vote Municipality A" }, new Municipality { Id = 202, Code = "VOTE-B", Name = "Vote Municipality B" }, department);
            setup.FinancialYears.AddRange(years);
            await setup.SaveChangesAsync();
            var municipalityYears = new[]
            {
                new MunicipalityFinancialYear { MunicipalityId = 201, FinancialYearId = years[0].Id, IsActive = true, EffectiveFrom = years[0].StartDate },
                new MunicipalityFinancialYear { MunicipalityId = 201, FinancialYearId = years[1].Id, IsActive = true, EffectiveFrom = years[1].StartDate },
                new MunicipalityFinancialYear { MunicipalityId = 202, FinancialYearId = years[0].Id, IsActive = true, EffectiveFrom = years[0].StartDate }
            };
            setup.MunicipalityFinancialYears.AddRange(municipalityYears);
            await setup.SaveChangesAsync();
            departmentId = department.Id;
            municipalityYearIds = municipalityYears.Select(item => item.Id).ToArray();
        }

        await using var context = new ApplicationDbContext(options, new TenantContext(201, "reference-admin"));

        context.VoteNumbers.AddRange(
            new VoteNumber { MunicipalityId = 201, MunicipalityFinancialYearId = municipalityYearIds[0], DepartmentId = departmentId, Code = "V01", Number = "001", Name = "Vote 2026" },
            new VoteNumber { MunicipalityId = 201, MunicipalityFinancialYearId = municipalityYearIds[1], DepartmentId = departmentId, Code = "V01", Number = "001", Name = "Vote 2027" });
        await context.SaveChangesAsync();

        var duplicate = new VoteNumber { MunicipalityId = 201, MunicipalityFinancialYearId = municipalityYearIds[0], DepartmentId = departmentId, Code = "V01", Number = "009", Name = "Duplicate" };
        context.VoteNumbers.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.Entry(duplicate).State = EntityState.Detached;

        var crossTenantYear = new VoteNumber { MunicipalityId = 201, MunicipalityFinancialYearId = municipalityYearIds[2], DepartmentId = departmentId, Code = "V02", Number = "002", Name = "Wrong tenant year" };
        context.VoteNumbers.Add(crossTenantYear);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Sqlite_enforces_position_uniqueness_and_tenant_filters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var system = new SystemTenantContext();
        long tenantAId;
        long tenantBId;
        await using (var setup = new ApplicationDbContext(options, system))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Code = "ORG-A", Name = "Organization A", RowVersion = [1] };
            var tenantB = new Municipality { Code = "ORG-B", Name = "Organization B", RowVersion = [1] };
            setup.AddRange(tenantA, tenantB); await setup.SaveChangesAsync(); tenantAId = tenantA.Id; tenantBId = tenantB.Id;
            var departmentA = new Department { MunicipalityId = tenantAId, Code = "FIN", Name = "Finance A", RowVersion = [1] };
            var departmentB = new Department { MunicipalityId = tenantBId, Code = "FIN", Name = "Finance B", RowVersion = [1] };
            setup.AddRange(departmentA, departmentB); await setup.SaveChangesAsync();
            setup.Positions.AddRange(
                new Position { MunicipalityId = tenantAId, DepartmentId = departmentA.Id, Code = "CFO", Name = "CFO A", RowVersion = [1] },
                new Position { MunicipalityId = tenantBId, DepartmentId = departmentB.Id, Code = "CFO", Name = "CFO B", RowVersion = [1] });
            await setup.SaveChangesAsync();
            setup.Positions.Add(new Position { MunicipalityId = tenantAId, DepartmentId = departmentA.Id, Code = "CFO", Name = "Duplicate", RowVersion = [1] });
            await Assert.ThrowsAsync<DbUpdateException>(() => setup.SaveChangesAsync());
        }

        await using var tenantAContext = new ApplicationDbContext(options, new TenantContext(tenantAId, "tenant-a"));
        await using var tenantBContext = new ApplicationDbContext(options, new TenantContext(tenantBId, "tenant-b"));
        Assert.Equal("CFO A", (await tenantAContext.Positions.AsNoTracking().SingleAsync()).Name);
        Assert.Equal("CFO B", (await tenantBContext.Positions.AsNoTracking().SingleAsync()).Name);
    }

    [Fact]
    public async Task CreatePosition_persists_tenant_scope_relationships_and_reasoned_audit()
    {
        var tenant = new TenantContext(71, "org-admin");
        await using var context = NewContext(tenant);
        var municipality = new Municipality { Id = 71, Code = "M71", Name = "Municipality 71" };
        var department = new Department { Id = 3, MunicipalityId = 71, Code = "FIN", Name = "Finance" };
        var unit = new Unit { Id = 4, MunicipalityId = 71, Department = department, DepartmentId = 3, Code = "BUD", Name = "Budget" };
        context.AddRange(municipality, department, unit);
        await context.SaveChangesAsync();

        var controller = CreateController(context, tenant);
        var response = await controller.CreatePosition(new SavePositionMasterRequest(department.PublicId, unit.PublicId, " cfo ", "Chief Financial Officer", "T20", true, DateTime.UtcNow.Date, null, "Approved organization establishment"));

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var envelope = Assert.IsType<ApiResponse<PositionMasterDto>>(ok.Value);
        Assert.Equal("CFO", envelope.Data!.Code);
        var position = await context.Positions.Include(x => x.Department).Include(x => x.Unit).SingleAsync();
        Assert.Equal(71, position.MunicipalityId);
        Assert.Equal(department.Id, position.DepartmentId);
        Assert.Equal(unit.Id, position.UnitId);
        var audit = await context.AuditTrails.SingleAsync(x => x.EntityName == nameof(Position));
        Assert.Equal("Approved organization establishment", audit.Reason);
        Assert.Equal(position.PublicId.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CreatePosition_rejects_a_unit_from_another_department()
    {
        var tenant = new TenantContext(72, "org-admin");
        await using var context = NewContext(tenant);
        var municipality = new Municipality { Id = 72, Code = "M72", Name = "Municipality 72" };
        var finance = new Department { Id = 5, MunicipalityId = 72, Code = "FIN", Name = "Finance" };
        var corporate = new Department { Id = 6, MunicipalityId = 72, Code = "CORP", Name = "Corporate" };
        var hr = new Unit { Id = 7, MunicipalityId = 72, Department = corporate, DepartmentId = corporate.Id, Code = "HR", Name = "Human Resources" };
        context.AddRange(municipality, finance, corporate, hr);
        await context.SaveChangesAsync();

        var response = await CreateController(context, tenant).CreatePosition(new SavePositionMasterRequest(finance.PublicId, hr.PublicId, "CFO", "Chief Financial Officer", null, true, DateTime.UtcNow.Date, null, "Approved organization establishment"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var envelope = Assert.IsType<ApiResponse<PositionMasterDto>>(badRequest.Value);
        Assert.Contains("does not belong", envelope.Message);
        Assert.Empty(await context.Positions.ToArrayAsync());
    }

    [Fact]
    public async Task CreateAssignment_uses_governed_position_and_preserves_its_snapshot()
    {
        var tenant = new TenantContext(73, "org-admin");
        await using var context = NewContext(tenant);
        var municipality = new Municipality { Id = 73, Code = "M73", Name = "Municipality 73" };
        var department = new Department { Id = 8, MunicipalityId = 73, Code = "FIN", Name = "Finance" };
        var position = new Position { Id = 9, MunicipalityId = 73, Department = department, DepartmentId = department.Id, Code = "CFO", Name = "Chief Financial Officer", EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        var employee = new MunicipalEmployee { Id = 10, MunicipalityId = 73, EmployeeNumber = "E73", FirstName = "Ada", LastName = "Mokoena", EffectiveFrom = DateTime.UtcNow.AddYears(-1) };
        context.AddRange(municipality, department, position, employee);
        await context.SaveChangesAsync();

        var controller = new TenantMastersController(context, tenant) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var response = await controller.CreateAssignment(new SaveEmployeeAssignmentRequest(employee.PublicId, department.PublicId, null, null, null, DateTime.UtcNow.Date, null, true, position.PublicId));

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var envelope = Assert.IsType<ApiResponse<EmployeeAssignmentDto>>(ok.Value);
        Assert.Equal(position.PublicId, envelope.Data!.PositionPublicId);
        Assert.Equal("CFO", envelope.Data.PositionCode);
        Assert.Equal("Chief Financial Officer", envelope.Data.PositionName);
        var assignment = await context.EmployeeAssignments.SingleAsync();
        Assert.Equal(position.Id, assignment.PositionId);
    }

    [Fact]
    public void Legacy_integer_mutation_routes_are_retired()
    {
        var tenant = new TenantContext(74, "org-admin");
        using var context = NewContext(tenant);
        var departmentResult = new DepartmentsController(context).UpdateDepartment(1, new FTCERP.Host.API.Requests.UpdateDepartmentRequest("FIN", "Finance", null));
        var unitResult = new UnitsController(context).DeleteUnit(1);

        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(departmentResult.Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(unitResult.Result).StatusCode);
    }

    private static ApplicationDbContext NewContext(ITenantContext tenant) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options, tenant);
    private static OrganizationMastersController CreateController(ApplicationDbContext context, ITenantContext tenant) => new(context, tenant) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    private sealed class TenantContext(long municipalityId, string userId) : ITenantContext { public long? MunicipalityId => municipalityId; public bool IsSystem => false; public string? UserId => userId; }
    private sealed class SystemTenantContext : ITenantContext { public long? MunicipalityId => null; public bool IsSystem => true; public string? UserId => "system"; }
}

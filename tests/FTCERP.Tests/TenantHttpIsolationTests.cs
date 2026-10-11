using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FTCERP.Tests;

public sealed class TenantHttpIsolationTests : IAsyncLifetime
{
    private const string UserId = "tenant-a-http-user";
    private readonly TenantApplicationFactory _factory = new(UserId);
    private HttpClient _client = null!;
    private SeededIds _ids = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _client.DefaultRequestHeaders.Add(CsrfProtectionMiddleware.HeaderName, CsrfProtectionMiddleware.HeaderValue);
        _client.DefaultRequestHeaders.Add(IdempotencyMiddleware.HeaderName, "tenant-isolation-test");
        _ids = await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task TenantPipeline_DeniesCrossMunicipalityListFetchEditApproveUploadDownloadReportAndAudit()
    {
        var list = await _client.GetAsync("/api/v1/opms-targets/page?page=1&pageSize=50&sortBy=createdAt");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await list.Content.ReadAsStringAsync();
        listJson.Should().Contain(_ids.TenantATargetPublicId.ToString());
        listJson.Should().NotContain(_ids.TenantBTargetPublicId.ToString());
        listJson.Should().NotContain("Tenant B KPI");

        (await _client.GetAsync($"/api/v1/opms-targets/{_ids.TenantBTargetPublicId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/v1/opms-targets/{_ids.TenantBTargetId}"))
            .StatusCode.Should().Be(HttpStatusCode.Gone);

        using var edit = JsonContent(new
        {
            opmsTargetId = _ids.TenantBTargetId,
            quarter = "Q1",
            actualPerformance = "99",
            actualExpenditure = (decimal?)null,
            varianceReason = "cross-tenant attempt",
            correctiveMeasure = "none",
            submitterScore = (decimal?)null,
            poeType = "Document"
        });
        (await _client.PutAsync($"/api/v1/opms-submissions/{_ids.TenantBSubmissionPublicId}", edit))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/v1/opms-submissions/{_ids.TenantBSubmissionId}"))
            .StatusCode.Should().Be(HttpStatusCode.Gone);

        using var approve = JsonContent(new { comment = "cross-tenant attempt" });
        (await _client.PostAsync($"/api/v1/opms-submissions/{_ids.TenantBSubmissionPublicId}/approve", approve))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var upload = new MultipartFormDataContent();
        var pdf = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF"));
        pdf.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        upload.Add(pdf, "file", "attempt.pdf");
        (await _client.PostAsync($"/api/v1/opms-submissions/{_ids.TenantBSubmissionPublicId}/attachments", upload))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await _client.GetAsync($"/api/v1/opms-submissions/{_ids.TenantBSubmissionPublicId}/attachments/{_ids.TenantBEvidencePublicId}/content"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/v1/opms-submissions/{_ids.TenantBSubmissionPublicId}/attachments/{_ids.TenantBEvidenceId}/content"))
            .StatusCode.Should().Be(HttpStatusCode.Gone);

        var report = await _client.GetAsync("/api/v1/reports/performance-summary?kind=Opms");
        report.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var reportJson = JsonDocument.Parse(await report.Content.ReadAsStringAsync()))
        {
            var data = reportJson.RootElement.GetProperty("data");
            data.GetProperty("targetCount").GetInt32().Should().Be(1);
            data.GetProperty("submissionCount").GetInt32().Should().Be(1);
            data.GetProperty("departments").EnumerateArray().Select(item => item.GetProperty("department").GetString())
                .Should().Equal("Tenant A Department");
        }

        var audit = await _client.GetAsync("/api/v1/audit/trails/page?page=1&pageSize=50&sortBy=createdAt");
        audit.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditJson = await audit.Content.ReadAsStringAsync();
        auditJson.Should().Contain("tenant-a-audit");
        auditJson.Should().NotContain("tenant-b-audit");
    }

    [Fact]
    public async Task TenantHeader_CannotSelectAnUnassignedMunicipality()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/opms-targets/page?page=1&pageSize=10&sortBy=createdAt");
        request.Headers.Add(TenantResolutionMiddleware.HeaderName, _ids.TenantBId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().Contain("TENANT_CONTEXT_DENIED");
    }

    [Fact]
    public async Task DynamicRoleChange_HidesNavigationAndRevokesDirectHttpReadWithoutRestart()
    {
        var initialMenu = await _client.GetAsync("/api/navigation/my-menu");
        initialMenu.StatusCode.Should().Be(HttpStatusCode.OK);
        (await initialMenu.Content.ReadAsStringAsync()).Should().Contain("NAV.HTTP.OPMS");

        var initialPage = await _client.GetAsync("/api/v1/opms-targets/page?page=1&pageSize=10&sortBy=createdAt");
        initialPage.StatusCode.Should().Be(HttpStatusCode.OK);
        (await initialPage.Content.ReadAsStringAsync()).Should().Contain(_ids.TenantATargetPublicId.ToString());

        await SetRolePermissionAsync(isAllowed: false);

        var deniedMenu = await _client.GetAsync("/api/navigation/my-menu");
        deniedMenu.StatusCode.Should().Be(HttpStatusCode.OK);
        (await deniedMenu.Content.ReadAsStringAsync()).Should().NotContain("NAV.HTTP.OPMS");

        using (var checkRequest = JsonContent(new { permissionCode = "OPMS_KPI.READ" }))
        {
            var check = await _client.PostAsync("/api/v1/access/check", checkRequest);
            check.StatusCode.Should().Be(HttpStatusCode.OK);
            using var checkJson = JsonDocument.Parse(await check.Content.ReadAsStringAsync());
            checkJson.RootElement.GetProperty("data").GetBoolean().Should().BeFalse();
        }

        var deniedPage = await _client.GetAsync("/api/v1/opms-targets/page?page=1&pageSize=10&sortBy=createdAt");
        deniedPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var deniedJson = await deniedPage.Content.ReadAsStringAsync();
        deniedJson.Should().NotContain(_ids.TenantATargetPublicId.ToString());
        deniedJson.Should().NotContain("Tenant A KPI");

        await SetRolePermissionAsync(isAllowed: true);

        var restoredMenu = await _client.GetAsync("/api/navigation/my-menu");
        restoredMenu.StatusCode.Should().Be(HttpStatusCode.OK);
        (await restoredMenu.Content.ReadAsStringAsync()).Should().Contain("NAV.HTTP.OPMS");

        var restoredPage = await _client.GetAsync("/api/v1/opms-targets/page?page=1&pageSize=10&sortBy=createdAt");
        restoredPage.StatusCode.Should().Be(HttpStatusCode.OK);
        (await restoredPage.Content.ReadAsStringAsync()).Should().Contain(_ids.TenantATargetPublicId.ToString());
    }

    private async Task SetRolePermissionAsync(bool isAllowed)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rolePermission = await context.RolePermissions.SingleAsync(item =>
            item.RoleId == _ids.TenantARoleId && item.PermissionId == _ids.OpmsReadPermissionId);
        rolePermission.IsAllowed = isAllowed;
        await context.SaveChangesAsync();
    }

    private async Task<SeededIds> SeedAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var tenantA = new Municipality { Id = 7001, Code = "HTTP-A", Name = "HTTP Tenant A" };
        var tenantB = new Municipality { Id = 7002, Code = "HTTP-B", Name = "HTTP Tenant B" };
        var userA = IdpTestFixture.CreateUser(UserId, "Tenant", "A"); userA.MunicipalityId = tenantA.Id;
        var userB = IdpTestFixture.CreateUser("tenant-b-http-user", "Tenant", "B"); userB.MunicipalityId = tenantB.Id;
        var role = new ApplicationRole
        {
            Id = "tenant-a-http-role",
            Name = "HTTP_TENANT_A_OPERATOR",
            NormalizedName = "HTTP_TENANT_A_OPERATOR",
            RoleCode = "HTTP_TENANT_A_OPERATOR",
            MunicipalityId = tenantA.Id,
            IsActive = true,
            EffectiveFrom = now.AddDays(-1)
        };
        context.AddRange(tenantA, tenantB, userA, userB, role);
        await context.SaveChangesAsync();

        var permissionCodes = new[]
        {
            "OPMS_KPI.READ", "OPMS_SUBMISSION.READ", "OPMS_SUBMISSION.UPDATE",
            "OPMS_SUBMISSION.APPROVE", "OPMS_POE.UPLOAD", "OPMS_REPORT.GENERATE",
            "AUDIT_TRAIL.READ", "AUDIT_TRAIL.EntityId.READ"
        };
        var permissions = await context.Permissions.Where(item => permissionCodes.Contains(item.Code)).ToListAsync();
        foreach (var missingCode in permissionCodes.Except(permissions.Select(item => item.Code), StringComparer.OrdinalIgnoreCase))
        {
            var segments = missingCode.Split('.');
            var permission = new Permission
            {
                Code = missingCode,
                Module = segments[0],
                Feature = segments.Length > 2 ? segments[1] : segments[0],
                Action = segments[^1],
                Kind = SecurityPermissionKind.Action,
                IsActive = true
            };
            permissions.Add(permission);
            context.Permissions.Add(permission);
        }
        await context.SaveChangesAsync();
        context.SecurityUserRoleAssignments.Add(new SecurityUserRoleAssignment
        {
            UserId = userA.Id,
            RoleId = role.Id,
            MunicipalityId = tenantA.Id,
            AssignedBy = "http-integration-test",
            AssignedAt = now,
            EffectiveFrom = now.AddDays(-1),
            IsActive = true
        });
        context.RolePermissions.AddRange(permissions.Select(permission => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsAllowed = true,
            IsActive = true,
            EffectiveFrom = now.AddDays(-1)
        }));
        context.SecurityNavigationItems.Add(new SecurityNavigationItem
        {
            Code = "NAV.HTTP.OPMS",
            Name = "HTTP OPMS register",
            Route = "/opms/targets",
            DisplayOrder = 1,
            RequiredPermissionCode = "OPMS_KPI.READ",
            IsActive = true
        });

        var departmentA = new Department { MunicipalityId = tenantA.Id, Code = "A-DEP", Name = "Tenant A Department" };
        var departmentB = new Department { MunicipalityId = tenantB.Id, Code = "B-DEP", Name = "Tenant B Department" };
        context.Departments.AddRange(departmentA, departmentB);
        await context.SaveChangesAsync();

        var targetA = Target(tenantA.Id, departmentA.Id, "tenant-a-target", "Tenant A KPI", userA.Id);
        var targetB = Target(tenantB.Id, departmentB.Id, "tenant-b-target", "Tenant B KPI", userB.Id);
        context.OpmsTargets.AddRange(targetA, targetB);
        var submissionA = Submission(tenantA.Id, targetA.Id, "tenant-a-submission", userA.Id);
        var submissionB = Submission(tenantB.Id, targetB.Id, "tenant-b-submission", userB.Id);
        context.OpmsSubmissions.AddRange(submissionA, submissionB);
        context.PerformancePeriodTargets.AddRange(
            new PerformancePeriodTarget { MunicipalityId = tenantA.Id, OpmsTargetId = targetA.Id, ReportingPeriodId = 7101, TargetValue = "10", CreatedByUserId = userA.Id },
            new PerformancePeriodTarget { MunicipalityId = tenantB.Id, OpmsTargetId = targetB.Id, ReportingPeriodId = 7201, TargetValue = "20", CreatedByUserId = userB.Id });

        var year = new FinancialYear { Id = 7001, Code = "2026/27", Name = "2026/27", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2027, 6, 30) };
        var municipalYearA = new MunicipalityFinancialYear { Id = 7001, MunicipalityId = tenantA.Id, FinancialYearId = year.Id, IsCurrent = true, IsActive = true, EffectiveFrom = now.AddDays(-1) };
        var municipalYearB = new MunicipalityFinancialYear { Id = 7002, MunicipalityId = tenantB.Id, FinancialYearId = year.Id, IsCurrent = true, IsActive = true, EffectiveFrom = now.AddDays(-1) };
        var periodA = new ReportingPeriod { Id = 7101, MunicipalityFinancialYearId = municipalYearA.Id, Code = "Q1-A", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 9, 30) };
        var periodB = new ReportingPeriod { Id = 7201, MunicipalityFinancialYearId = municipalYearB.Id, Code = "Q1-B", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 9, 30) };
        context.AddRange(year, municipalYearA, municipalYearB, periodA, periodB);
        submissionA.ReportingPeriodId = periodA.Id;
        submissionB.ReportingPeriodId = periodB.Id;

        var blobB = new EvidenceBlob
        {
            Id = "tenant-b-blob",
            MunicipalityId = tenantB.Id,
            StorageKey = "poe/opms/tenant-b-submission/evidence.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 14,
            Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF"))).ToLowerInvariant(),
            SignatureVerified = true,
            ScanStatus = "Clean"
        };
        var evidenceB = new PoeFile
        {
            Id = "tenant-b-evidence",
            MunicipalityId = tenantB.Id,
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = submissionB.Id,
            FileName = "evidence.pdf",
            Blob = blobB,
            UploadedByUserId = userB.Id
        };
        context.PoeFiles.Add(evidenceB);
        context.AuditTrails.AddRange(
            new AuditTrail { MunicipalityId = tenantA.Id, EntityName = "Test", EntityId = "tenant-a-audit", Action = "Seed", ChangedBy = userA.Id },
            new AuditTrail { MunicipalityId = tenantB.Id, EntityName = "Test", EntityId = "tenant-b-audit", Action = "Seed", ChangedBy = userB.Id });
        await context.SaveChangesAsync();

        var opmsReadPermissionId = permissions.Single(item => item.Code == "OPMS_KPI.READ").Id;
        return new SeededIds(tenantB.Id, role.Id, opmsReadPermissionId, targetA.PublicId, targetB.PublicId, targetB.Id, submissionB.PublicId, submissionB.Id, evidenceB.PublicId, evidenceB.Id);
    }

    private static OpmsTarget Target(long municipalityId, int departmentId, string id, string name, string ownerId) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        DepartmentId = departmentId,
        AssignedUserId = ownerId,
        IndicatorNumber = id,
        TargetName = name,
        KpiDescription = name,
        NationalKpa = "Governance",
        MunicipalKpa = "Governance",
        PerformanceObjective = "Test tenant isolation",
        AnnualTargetDescription = "10",
        AnnualTarget = 10,
        Weight = 100,
        KpiType = "Output",
        IndicatorType = "Quantitative"
    };

    private static OpmsSubmission Submission(long municipalityId, string targetId, string id, string userId) => new()
    {
        Id = id,
        MunicipalityId = municipalityId,
        OpmsTargetId = targetId,
        Quarter = "Q1",
        BaseState = SubmissionBaseStates.Submitted,
        Status = "verified",
        ActualPerformance = "8",
        SubmittedByUserId = userId,
        CreatedBy = userId
    };

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private sealed record SeededIds(long TenantBId, string TenantARoleId, int OpmsReadPermissionId, Guid TenantATargetPublicId, Guid TenantBTargetPublicId, string TenantBTargetId, Guid TenantBSubmissionPublicId, string TenantBSubmissionId, Guid TenantBEvidencePublicId, string TenantBEvidenceId);
}

internal sealed class TenantApplicationFactory(string userId) : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"opms-http-{Guid.NewGuid():N}.db");
    private readonly string _evidencePath = Path.Combine(Path.GetTempPath(), $"opms-http-evidence-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:EnsureCreated"] = "true",
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}",
            ["SeedData:Enabled"] = "false",
            ["EvidenceStorage:Provider"] = "FileSystem",
            ["EvidenceStorage:LocalRoot"] = _evidencePath,
            ["JwtSettings:Secret"] = "test-only-secret-that-is-long-enough-for-hmac-signing"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthenticationHandler.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
            services.AddSingleton(new TestAuthenticationIdentity(userId));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        if (Directory.Exists(_evidencePath)) Directory.Delete(_evidencePath, recursive: true);
    }
}

internal sealed record TestAuthenticationIdentity(string UserId);

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TestAuthenticationIdentity identity) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "TenantIntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, identity.UserId), new Claim(ClaimTypes.Name, identity.UserId)], AuthenticationScheme));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, AuthenticationScheme)));
    }
}

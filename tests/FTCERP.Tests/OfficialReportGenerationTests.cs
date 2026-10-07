using System.IO.Compression;
using FluentAssertions;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Reporting;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace FTCERP.Tests;

public sealed class OfficialReportGenerationTests
{
    private static readonly OfficialPerformanceReportRow[] Rows =
    [
        new("KPI-001", "Revenue collected", "Finance", "Revenue", "Q1", "95%", "90%", "-5", "94.74", "False", "Submitted"),
        new("=FORMULA", "Formula-safe title", "Corporate", "Governance", "Q1", "1", "1", "0", "100", "True", "Approved")
    ];

    [Theory]
    [InlineData(OfficialReportFormat.Csv, "csv")]
    [InlineData(OfficialReportFormat.Xlsx, "xlsx")]
    [InlineData(OfficialReportFormat.Docx, "docx")]
    [InlineData(OfficialReportFormat.Pdf, "pdf")]
    public void Renderer_ProducesEveryApprovedFormatFromTheSameDeterministicDataset(OfficialReportFormat format, string extension)
    {
        var request = new OfficialReportRenderRequest("Example Municipality", "2026/27", "Quarter 1", "{Municipality} · {FinancialYear} {Period}", OfficialReportRenderer.DefaultColumnsJson, format, Rows);

        var first = OfficialReportRenderer.Render(request);
        var second = OfficialReportRenderer.Render(request);

        first.Extension.Should().Be(extension);
        first.Content.Should().NotBeEmpty();
        first.DataVersionReference.Should().HaveLength(64).And.Be(second.DataVersionReference);
        first.Sha256.Should().HaveLength(64).And.Be(second.Sha256);
        first.Content.Should().Equal(second.Content);
        if (format == OfficialReportFormat.Pdf) first.Content[..5].Should().Equal("%PDF-"u8.ToArray());
        if (format is OfficialReportFormat.Xlsx or OfficialReportFormat.Docx)
        {
            using var archive = new ZipArchive(new MemoryStream(first.Content), ZipArchiveMode.Read);
            archive.GetEntry("[Content_Types].xml").Should().NotBeNull();
        }
        if (format == OfficialReportFormat.Csv)
            System.Text.Encoding.UTF8.GetString(first.Content).Should().Contain("\"'=FORMULA\"");
    }

    [Fact]
    public void Renderer_RejectsUnknownAndDuplicateColumns()
    {
        var unknown = () => OfficialReportRenderer.ValidateColumns("[\"indicator\",\"secretField\"]");
        var duplicate = () => OfficialReportRenderer.ValidateColumns("[\"indicator\",\"INDICATOR\"]");
        unknown.Should().Throw<ArgumentException>();
        duplicate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Catalog_DefinesAndRestrictsEveryRequiredReportClass()
    {
        var reportTypes = Enum.GetValues<OfficialReportType>();

        reportTypes.Should().HaveCount(16);
        foreach (var reportType in reportTypes)
        {
            var defaults = OfficialReportCatalog.DefaultColumnKeys(reportType);
            defaults.Should().NotBeEmpty($"{reportType} must have an approved default layout");
            OfficialReportCatalog.ValidateColumns(reportType, OfficialReportCatalog.DefaultColumnsJson(reportType)).Should().Equal(defaults);
        }

        var invalid = () => OfficialReportCatalog.ValidateColumns(OfficialReportType.AuditTrail, "[\"indicator\"]");
        invalid.Should().Throw<ArgumentException>().WithMessage("*unsupported by the selected report class*");
    }

    [Theory]
    [InlineData(OfficialReportType.QuarterlyPerformance, "OPMS_SUBMISSION.ActualPerformance.READ")]
    [InlineData(OfficialReportType.VerificationRegister, "OPMS_WORKFLOW.ActionComment.READ")]
    [InlineData(OfficialReportType.InternalAudit, "OPMS_SUBMISSION.InternalAuditObservation.READ")]
    [InlineData(OfficialReportType.OutstandingRfi, "OPMS_RFI.Response.READ")]
    [InlineData(OfficialReportType.EvidenceRegister, "OPMS_POE.UploadedByName.READ")]
    [InlineData(OfficialReportType.AuditTrail, "AUDIT_TRAIL.CorrelationId.READ")]
    [InlineData(OfficialReportType.VersionTrail, "OPMS_KPI.RevisionActor.READ")]
    public void ReportContentPermissions_CoverProtectedConfiguredColumns(OfficialReportType reportType, string expectedPermission)
    {
        var permissions = OfficialReportsController.ReportContentReadPermissions(
            SubmissionKind.Opms, reportType, OfficialReportCatalog.DefaultColumnsJson(reportType));

        permissions.Should().Contain(expectedPermission);
    }

    [Fact]
    public void ReportContentPermissions_DoNotRequireOmittedSensitiveAuditMembers()
    {
        var permissions = OfficialReportsController.ReportContentReadPermissions(
            SubmissionKind.Opms, OfficialReportType.AuditTrail, "[\"entityName\",\"action\",\"changedAt\"]");

        permissions.Should().Equal("AUDIT_TRAIL.READ");
    }

    [Fact]
    public void TabularRenderer_UsesTheSelectedReportClassHeadingsAndStableSnapshot()
    {
        var rows = new[]
        {
            new OfficialReportDataRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["entityName"] = "OpmsSubmission",
                ["entityId"] = "submission-1",
                ["action"] = "Approve",
                ["changedBy"] = "auditor",
                ["changedAt"] = "2026-10-04T10:00:00.0000000Z",
                ["reason"] = "Governed approval",
                ["correlationId"] = "correlation-1"
            })
        };
        var request = new OfficialTabularReportRenderRequest("Example Municipality", "2026/27", "Quarter 1", "Audit trail", OfficialReportCatalog.DefaultColumnsJson(OfficialReportType.AuditTrail), OfficialReportFormat.Csv, OfficialReportType.AuditTrail, rows);

        var first = OfficialReportRenderer.RenderTabular(request);
        var second = OfficialReportRenderer.RenderTabular(request);
        var csv = System.Text.Encoding.UTF8.GetString(first.Content);

        csv.Should().Contain("Entity").And.Contain("Correlation ID").And.Contain("OpmsSubmission");
        first.DataVersionReference.Should().Be(second.DataVersionReference);
        first.Content.Should().Equal(second.Content);
    }

    [Fact]
    public void PdfRenderer_PaginatesWithoutDroppingOfficialRows()
    {
        var rows = Enumerable.Range(1, 120).Select(index => Rows[0] with { Indicator = $"KPI-{index:D3}" }).ToArray();
        var result = OfficialReportRenderer.Render(new("Example Municipality", "2026/27", "Annual", "Annual report", OfficialReportRenderer.DefaultColumnsJson, OfficialReportFormat.Pdf, rows));
        var pdf = System.Text.Encoding.ASCII.GetString(result.Content);
        pdf.Should().Contain("/Count 3");
        pdf.Should().Contain("KPI-120");
    }

    [Fact]
    public async Task OfficialGenerations_AreTenantFilteredVersionUniqueAndAppendOnly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        long tenantBId;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Name = "Tenant A", Code = "TA" };
            var tenantB = new Municipality { Name = "Tenant B", Code = "TB" };
            setup.AddRange(tenantA, tenantB);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id; tenantBId = tenantB.Id;
            await SeedGeneration(setup, tenantA, "reporter-a");
            await SeedGeneration(setup, tenantB, "reporter-b");
        }

        await using var tenantAContext = new ApplicationDbContext(options, new FixedTenantContext(tenantAId));
        (await tenantAContext.OfficialReportGenerations.CountAsync()).Should().Be(1);
        (await tenantAContext.OfficialReportGenerationScopeGrants.CountAsync()).Should().Be(1);
        (await tenantAContext.OfficialReportTemplates.CountAsync()).Should().Be(1);
        var generation = await tenantAContext.OfficialReportGenerations.SingleAsync();
        generation.VersionNumber = 2;
        var update = () => tenantAContext.SaveChangesAsync();
        await update.Should().ThrowAsync<InvalidOperationException>().WithMessage("*generated report history*append-only*");
        tenantAContext.Entry(generation).State = EntityState.Unchanged;

        var scopeGrant = await tenantAContext.OfficialReportGenerationScopeGrants.SingleAsync();
        scopeGrant.Value = "2";
        var rewriteScope = () => tenantAContext.SaveChangesAsync();
        await rewriteScope.Should().ThrowAsync<InvalidOperationException>().WithMessage("*generated report history*append-only*");
        tenantAContext.Entry(scopeGrant).State = EntityState.Unchanged;

        var crossTenant = new OfficialReportGeneration { MunicipalityId = tenantBId };
        tenantAContext.OfficialReportGenerations.Add(crossTenant);
        var write = () => tenantAContext.SaveChangesAsync();
        await write.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Cross-municipality writes*");
    }

    [Fact]
    public async Task TemplatesPage_IsTenantFilteredSearchableAndStablyPaged()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        long tenantAId;
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            var tenantA = new Municipality { Name = "Template Tenant A", Code = "TTA" };
            var tenantB = new Municipality { Name = "Template Tenant B", Code = "TTB" };
            setup.AddRange(tenantA, tenantB);
            await setup.SaveChangesAsync();
            tenantAId = tenantA.Id;
            await SeedGeneration(setup, tenantA, "template-reporter-a");
            await SeedGeneration(setup, tenantB, "template-reporter-b");
        }

        var tenant = new FixedTenantContext(tenantAId);
        await using var context = new ApplicationDbContext(options, tenant);
        var user = await context.Users.SingleAsync(item => item.Id == "template-reporter-a");
        var year = await context.MunicipalityFinancialYears.SingleAsync();
        context.OfficialReportTemplates.AddRange(Enumerable.Range(1, 30).Select(index => new OfficialReportTemplate
        {
            MunicipalityId = tenantAId, MunicipalityFinancialYearId = year.Id, SubmissionKind = SubmissionKind.Opms,
            ReportType = OfficialReportType.QuarterlyPerformance, Code = $"TPL-{index:00}", Name = $"Quarterly template {index:00}",
            Format = OfficialReportFormat.Pdf, EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            ApprovalReference = $"Council-{index:00}", Reason = "Approved template", CreatedByUserId = user.Id,
            ColumnConfigurationJson = OfficialReportRenderer.DefaultColumnsJson
        }));
        await context.SaveChangesAsync();
        var granted = new AccessQueryScopeResult(true, true, [], [], [], [], [], []);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), "OPMS_REPORT.READ")).ReturnsAsync(granted);
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test")) } }
        };

        var result = await controller.TemplatesPage(SubmissionKind.Opms, false, year.PublicId, new PagedQueryRequest
        {
            Page = 2, PageSize = 10, Search = "Quarterly", SortBy = "code", SortDirection = "asc"
        });

        var page = Assert.IsType<ApiResponse<PagedResponse<OfficialReportTemplateResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        page.TotalCount.Should().Be(31);
        page.TotalPages.Should().Be(4);
        page.Items.Should().HaveCount(10);
        page.Items[0].Code.Should().Be("TPL-10");
    }

    [Fact]
    public async Task TemplatesPage_RejectsUnknownSort()
    {
        await using var context = IdpTestFixture.CreateContext();
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock().Object, Mock.Of<IAccessControlService>(), IdpTestFixture.Tenant(1, "reader"), Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>());

        var result = await controller.TemplatesPage(SubmissionKind.Opms, false, null, new PagedQueryRequest { SortBy = "raw-sql" });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        controller.Templates(SubmissionKind.Opms).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task DirectGenerationCall_IsDeniedWhenTheDynamicActionPermissionIsHidden()
    {
        var municipalityId = 701L;
        var tenant = new FixedTenantContext(municipalityId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = municipalityId, Code = "RPT", Name = "Reporting Municipality" };
        var user = new ApplicationUser { Id = "restricted-reporter", UserName = "restricted-reporter", FirstName = "Restricted", LastName = "Reporter", MunicipalityId = municipalityId };
        var template = new OfficialReportTemplate { MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Opms, Code = "QUARTERLY", Name = "Quarterly", Format = OfficialReportFormat.Pdf, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ApprovalReference = "Council", Reason = "Approved", CreatedByUserId = user.Id, ColumnConfigurationJson = OfficialReportRenderer.DefaultColumnsJson };
        context.AddRange(municipality, user, template);
        await context.SaveChangesAsync();
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_REPORT.GENERATE")).ReturnsAsync(new AccessQueryScopeResult(false, false, [], [], [], [], [], []));
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test")) } }
        };

        var response = await controller.Generate(new GenerateOfficialReportRequest(template.PublicId, Guid.NewGuid(), Guid.NewGuid(), null));

        response.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task DirectGenerationCall_IsDeniedWhenUnderlyingResourceReadPermissionIsMissing()
    {
        var municipalityId = 702L;
        var tenant = new FixedTenantContext(municipalityId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = municipalityId, Code = "RPR", Name = "Resource Permission Municipality" };
        var user = new ApplicationUser { Id = "resource-reporter", UserName = "resource-reporter", FirstName = "Resource", LastName = "Reporter", MunicipalityId = municipalityId };
        var template = new OfficialReportTemplate { MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Opms, ReportType = OfficialReportType.QuarterlyPerformance, Code = "QUARTERLY", Name = "Quarterly", Format = OfficialReportFormat.Pdf, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ApprovalReference = "Council", Reason = "Approved", CreatedByUserId = user.Id, ColumnConfigurationJson = OfficialReportCatalog.DefaultColumnsJson(OfficialReportType.QuarterlyPerformance) };
        context.AddRange(municipality, user, template);
        await context.SaveChangesAsync();
        var granted = new AccessQueryScopeResult(true, true, [], [], [], [], [], []);
        var denied = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_REPORT.GENERATE")).ReturnsAsync(granted);
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_KPI.READ")).ReturnsAsync(denied);
        access.Setup(service => service.GetQueryScopeAsync(user, "OPMS_SUBMISSION.READ")).ReturnsAsync(granted);
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test")) } }
        };

        var response = await controller.Generate(new GenerateOfficialReportRequest(template.PublicId, Guid.NewGuid(), Guid.NewGuid(), null));

        response.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task OutstandingRfiGeneration_IsDeniedWhenRfiMemberPermissionIsMissing()
    {
        var municipalityId = 703L;
        var tenant = new FixedTenantContext(municipalityId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = municipalityId, Code = "RFI-RPT", Name = "RFI Report Municipality" };
        var user = new ApplicationUser { Id = "rfi-reporter", UserName = "rfi-reporter", FirstName = "RFI", LastName = "Reporter", MunicipalityId = municipalityId };
        var template = new OfficialReportTemplate { MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Opms, ReportType = OfficialReportType.OutstandingRfi, Code = "RFI", Name = "Outstanding RFI", Format = OfficialReportFormat.Csv, EffectiveFrom = DateTime.UtcNow.AddDays(-1), ApprovalReference = "Council", Reason = "Approved", CreatedByUserId = user.Id, ColumnConfigurationJson = OfficialReportCatalog.DefaultColumnsJson(OfficialReportType.OutstandingRfi) };
        context.AddRange(municipality, user, template);
        await context.SaveChangesAsync();
        var granted = new AccessQueryScopeResult(true, true, [], [], [], [], [], []);
        var denied = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var basePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPMS_REPORT.GENERATE", "OPMS_KPI.READ", "OPMS_SUBMISSION.READ" };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string permission) => basePermissions.Contains(permission) ? granted : denied);
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test")) } }
        };

        var response = await controller.Generate(new GenerateOfficialReportRequest(template.PublicId, Guid.NewGuid(), Guid.NewGuid(), null));

        response.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        access.Verify(service => service.GetQueryScopeAsync(user, "OPMS_RFI.READ"), Times.Once);
    }

    [Theory]
    [InlineData(OfficialReportType.QuarterlyPerformance, "OPMS_SUBMISSION.ActualPerformance.READ")]
    [InlineData(OfficialReportType.VerificationRegister, "OPMS_WORKFLOW.ActionActorUserId.READ")]
    [InlineData(OfficialReportType.InternalAudit, "OPMS_SUBMISSION.InternalAuditFindings.READ")]
    [InlineData(OfficialReportType.EvidenceRegister, "OPMS_POE.READ")]
    [InlineData(OfficialReportType.AuditTrail, "AUDIT_TRAIL.Reason.READ")]
    [InlineData(OfficialReportType.VersionTrail, "OPMS_KPI.RevisionReason.READ")]
    public async Task DirectGenerationCall_IsDeniedWhenAConfiguredSourcePermissionIsMissing(
        OfficialReportType reportType, string deniedPermission)
    {
        var municipalityId = 704L + (long)reportType;
        var tenant = new FixedTenantContext(municipalityId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = municipalityId, Code = $"SRC-{(int)reportType}", Name = "Source Permission Municipality" };
        var user = new ApplicationUser { Id = $"source-reporter-{(int)reportType}", UserName = "source-reporter", FirstName = "Source", LastName = "Reporter", MunicipalityId = municipalityId };
        var template = new OfficialReportTemplate
        {
            MunicipalityId = municipalityId, SubmissionKind = SubmissionKind.Opms, ReportType = reportType,
            Code = $"SOURCE-{(int)reportType}", Name = "Protected source report", Format = OfficialReportFormat.Csv,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1), ApprovalReference = "Council", Reason = "Approved",
            CreatedByUserId = user.Id, ColumnConfigurationJson = OfficialReportCatalog.DefaultColumnsJson(reportType)
        };
        context.AddRange(municipality, user, template);
        await context.SaveChangesAsync();
        var granted = new AccessQueryScopeResult(true, true, [], [], [], [], [], []);
        var denied = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string permission) =>
                permission.Equals(deniedPermission, StringComparison.OrdinalIgnoreCase) ? denied : granted);
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test"))
                }
            }
        };

        var response = await controller.Generate(new GenerateOfficialReportRequest(template.PublicId, Guid.NewGuid(), Guid.NewGuid(), null));

        response.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        access.Verify(service => service.GetQueryScopeAsync(user, deniedPermission), Times.Once);
    }

    [Fact]
    public async Task Download_ReevaluatesConfiguredSourceMemberPermissionsAndRevokesStoredContent()
    {
        var municipalityId = 799L;
        var tenant = new FixedTenantContext(municipalityId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        var municipality = new Municipality { Id = municipalityId, Code = "REV", Name = "Revocation Municipality" };
        var user = new ApplicationUser { Id = "revoked-reporter", UserName = "revoked-reporter", MunicipalityId = municipalityId };
        var financialYear = new FinancialYear { Code = "REV-2026", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        context.AddRange(municipality, user, financialYear);
        await context.SaveChangesAsync();
        var municipalYear = new MunicipalityFinancialYear { MunicipalityId = municipalityId, FinancialYearId = financialYear.Id, IsActive = true, IsCurrent = true, EffectiveFrom = financialYear.StartDate };
        context.Add(municipalYear);
        await context.SaveChangesAsync();
        var period = new ReportingPeriod { MunicipalityFinancialYearId = municipalYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new(2026, 7, 1), EndDate = new(2026, 9, 30) };
        var template = new OfficialReportTemplate
        {
            MunicipalityId = municipalityId, MunicipalityFinancialYearId = municipalYear.Id, SubmissionKind = SubmissionKind.Opms,
            ReportType = OfficialReportType.InternalAudit, Code = "IA", Name = "Internal Audit", Format = OfficialReportFormat.Csv,
            EffectiveFrom = financialYear.StartDate, ApprovalReference = "Council", Reason = "Approved", CreatedByUserId = user.Id,
            ColumnConfigurationJson = OfficialReportCatalog.DefaultColumnsJson(OfficialReportType.InternalAudit)
        };
        var blob = new EvidenceBlob { MunicipalityId = municipalityId, StorageKey = "official-reports/revoked.csv", ContentType = "text/csv", SizeInBytes = 4, Sha256 = new string('a', 64), SignatureVerified = true, ScanStatus = "SystemGenerated" };
        context.AddRange(period, template, blob);
        await context.SaveChangesAsync();
        var generation = new OfficialReportGeneration
        {
            MunicipalityId = municipalityId, MunicipalityFinancialYearId = municipalYear.Id, ReportingPeriodId = period.Id,
            ReportTemplateId = template.Id, EvidenceBlobId = blob.Id, SubmissionKind = SubmissionKind.Opms,
            ReportType = OfficialReportType.InternalAudit, VersionNumber = 1, ScopeSchemaVersion = 1, ScopeIsUnrestricted = true,
            ScopeJson = "{\"Unrestricted\":true,\"DepartmentIds\":[],\"UnitIds\":[],\"OwnerUserIds\":[],\"TargetIds\":[]}",
            FilterJson = "{}", DataVersionReference = new string('b', 64), FileName = "internal-audit.csv", ContentType = "text/csv",
            SizeInBytes = 4, Sha256 = blob.Sha256, GeneratedByUserId = user.Id
        };
        context.Add(generation);
        await context.SaveChangesAsync();

        var granted = new AccessQueryScopeResult(true, true, [], [], [], [], [], []);
        var denied = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var revokedPermission = "OPMS_SUBMISSION.InternalAuditScore.READ";
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(user, It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string permission) =>
                permission.Equals(revokedPermission, StringComparison.OrdinalIgnoreCase) ? denied : granted);
        var storage = new Mock<IEvidenceBlobStorage>();
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object,
            access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), storage.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test"))
                }
            }
        };

        var response = await controller.Download(generation.PublicId);

        response.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        access.Verify(service => service.GetQueryScopeAsync(user, revokedPermission), Times.Once);
        storage.Verify(service => service.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerationsPage_AppliesNormalizedStoredScopeBeforeCountAndPaging()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var municipality = new Municipality { Name = "Paged Municipality", Code = "PAGE" };
        await using (var setup = new ApplicationDbContext(options, new SystemTenantContext()))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Add(municipality);
            await setup.SaveChangesAsync();
            await SeedGeneration(setup, municipality, "paged-reporter");
        }

        var tenant = new FixedTenantContext(municipality.Id);
        await using var context = new ApplicationDbContext(options, tenant);
        var template = await context.OfficialReportTemplates.SingleAsync();
        var year = await context.MunicipalityFinancialYears.SingleAsync();
        var period = await context.ReportingPeriods.SingleAsync();
        var blob = await context.EvidenceBlobs.SingleAsync();
        var user = await context.Users.SingleAsync();
        var start = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var departmentPublicId = Guid.NewGuid();
        var accessible = Enumerable.Range(1, 31).Select(index => NewScopedGeneration(index, "10", 1, start.AddMinutes(index))).ToArray();
        var denied = Enumerable.Range(32, 2).Select(index => NewScopedGeneration(index, "20", 1, start.AddMinutes(index))).ToArray();
        var unnormalized = NewScopedGeneration(34, "10", 0, start.AddMinutes(34));
        context.AddRange(accessible.Concat(denied).Append(unnormalized));
        await context.SaveChangesAsync();

        var permittedScope = new AccessQueryScopeResult(true, false, [10], [], [], [], [], []);
        var deniedScope = new AccessQueryScopeResult(false, false, [], [], [], [], [], []);
        var allowedMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var basePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OPMS_REPORT.READ", "OPMS_KPI.READ", "OPMS_SUBMISSION.READ"
        };
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.GetQueryScopeAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser _, string permission) =>
                basePermissions.Contains(permission) || allowedMembers.Contains(permission) ? permittedScope : deniedScope);
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock(user).Object, access.Object, tenant, Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test")) } }
        };

        var result = await controller.GenerationsPage(SubmissionKind.Opms, null, new PagedQueryRequest
        {
            Page = 2,
            PageSize = 10,
            Search = "Quarterly",
            SortBy = "generatedAt",
            SortDirection = "asc"
        });

        var page = Assert.IsType<ApiResponse<PagedResponse<OfficialReportGenerationResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        page.TotalCount.Should().Be(31);
        page.TotalPages.Should().Be(4);
        page.Items.Should().HaveCount(10);
        page.Items[0].VersionNumber.Should().Be(11);
        page.Items.Should().OnlyContain(item => item.VersionNumber >= 11 && item.VersionNumber <= 20);
        page.Items.Should().OnlyContain(item => item.ScopeJson == null && item.FilterJson == null
            && item.DataVersionReference == null && item.GeneratedBy == null);
        page.Items.Should().OnlyContain(item => item.DepartmentPublicId == departmentPublicId && item.UnitPublicId == null);
        var deniedGeneratorSearch = await controller.GenerationsPage(SubmissionKind.Opms, null,
            new PagedQueryRequest { Search = "paged-reporter", SortBy = "generatedAt" });
        Assert.IsType<ApiResponse<PagedResponse<OfficialReportGenerationResponse>>>(
            Assert.IsType<OkObjectResult>(deniedGeneratorSearch.Result).Value).Data!.TotalCount.Should().Be(0);

        foreach (var member in new[]
                 {
                     "OPMS_REPORT.GenerationScopeJson.READ", "OPMS_REPORT.GenerationFilterJson.READ",
                     "OPMS_REPORT.GenerationDataVersionReference.READ", "OPMS_REPORT.GenerationGeneratedBy.READ"
                 })
            allowedMembers.Add(member);

        var grantedResult = await controller.GenerationsPage(SubmissionKind.Opms, null,
            new PagedQueryRequest { Search = "paged-reporter", SortBy = "generatedAt" });
        var grantedPage = Assert.IsType<ApiResponse<PagedResponse<OfficialReportGenerationResponse>>>(
            Assert.IsType<OkObjectResult>(grantedResult.Result).Value).Data!;
        grantedPage.TotalCount.Should().Be(31);
        grantedPage.Items.Should().OnlyContain(item => item.ScopeJson != null && item.FilterJson != null
            && item.DataVersionReference == new string('c', 64) && item.GeneratedBy == "paged-reporter");

        OfficialReportGeneration NewScopedGeneration(int index, string departmentId, int scopeSchemaVersion, DateTime generatedAt) => new()
        {
            GenerationFamilyPublicId = Guid.NewGuid(), MunicipalityId = municipality.Id, MunicipalityFinancialYearId = year.Id,
            ReportingPeriodId = period.Id, ReportTemplateId = template.Id, EvidenceBlobId = blob.Id, SubmissionKind = SubmissionKind.Opms,
            ReportType = OfficialReportType.QuarterlyPerformance, VersionNumber = index, ScopeSchemaVersion = scopeSchemaVersion,
            ScopeJson = $"{{\"Unrestricted\":false,\"DepartmentIds\":[{departmentId}],\"UnitIds\":[],\"OwnerUserIds\":[],\"TargetIds\":[]}}",
            FilterJson = $"{{\"departmentPublicId\":\"{departmentPublicId}\",\"internal\":\"FILTER-SECRET\"}}", DataVersionReference = new string('c', 64), FileName = $"report-{index}.pdf", ContentType = "application/pdf",
            SizeInBytes = 4, Sha256 = blob.Sha256, RowCount = index, GeneratedByUserId = user.Id, GeneratedAt = generatedAt,
            ScopeGrants = [new OfficialReportGenerationScopeGrant { MunicipalityId = municipality.Id, Dimension = OfficialReportScopeDimension.Department, Value = departmentId }]
        };
    }

    [Fact]
    public async Task GenerationsPage_RejectsUnknownSort()
    {
        await using var context = IdpTestFixture.CreateContext();
        var controller = new OfficialReportsController(context, IdpTestFixture.CreateUserManagerMock().Object, Mock.Of<IAccessControlService>(), IdpTestFixture.Tenant(1, "reader"), Mock.Of<IWorkflowGovernanceService>(), Mock.Of<IEvidenceBlobStorage>());

        var result = await controller.GenerationsPage(SubmissionKind.Opms, null, new PagedQueryRequest { SortBy = "raw-sql" });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        controller.Generations(SubmissionKind.Opms).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static async Task SeedGeneration(ApplicationDbContext context, Municipality municipality, string userId)
    {
        var user = new ApplicationUser { Id = userId, UserName = userId, NormalizedUserName = userId.ToUpperInvariant(), Email = userId + "@example.test", NormalizedEmail = (userId + "@example.test").ToUpperInvariant(), FirstName = "Report", LastName = "User", MunicipalityId = municipality.Id };
        var year = new FinancialYear { Code = municipality.Code + "-2026", Name = "2026/27", StartDate = new(2026, 7, 1), EndDate = new(2027, 6, 30) };
        context.AddRange(user, year); await context.SaveChangesAsync();
        var municipalYear = new MunicipalityFinancialYear { MunicipalityId = municipality.Id, FinancialYearId = year.Id, IsActive = true, IsCurrent = true, EffectiveFrom = year.StartDate };
        context.Add(municipalYear); await context.SaveChangesAsync();
        var period = new ReportingPeriod { MunicipalityFinancialYearId = municipalYear.Id, Code = "Q1", Name = "Quarter 1", PeriodType = ReportingPeriodType.Quarter1, Sequence = 1, StartDate = new(2026, 7, 1), EndDate = new(2026, 9, 30) };
        context.Add(period); await context.SaveChangesAsync();
        var template = new OfficialReportTemplate { MunicipalityId = municipality.Id, SubmissionKind = SubmissionKind.Opms, Code = "QUARTERLY", Name = "Quarterly report", Format = OfficialReportFormat.Pdf, EffectiveFrom = year.StartDate, ApprovalReference = "Council-1", Reason = "Approved layout", CreatedByUserId = user.Id, ColumnConfigurationJson = OfficialReportRenderer.DefaultColumnsJson };
        var blob = new EvidenceBlob { MunicipalityId = municipality.Id, StorageKey = $"official-reports/{municipality.Id}/one/v1.pdf", ContentType = "application/pdf", SizeInBytes = 4, Sha256 = new string('a', 64), SignatureVerified = true, ScanStatus = "SystemGenerated" };
        context.AddRange(template, blob); await context.SaveChangesAsync();
        context.Add(new OfficialReportGeneration { GenerationFamilyPublicId = Guid.NewGuid(), MunicipalityId = municipality.Id, MunicipalityFinancialYearId = municipalYear.Id, ReportingPeriodId = period.Id, ReportTemplateId = template.Id, EvidenceBlobId = blob.Id, SubmissionKind = SubmissionKind.Opms, VersionNumber = 1, ScopeJson = "{\"Unrestricted\":false,\"DepartmentIds\":[1],\"UnitIds\":[],\"OwnerUserIds\":[],\"TargetIds\":[]}", ScopeSchemaVersion = 1, ScopeIsUnrestricted = false, FilterJson = "{}", DataVersionReference = new string('b', 64), FileName = "report-v1.pdf", ContentType = "application/pdf", SizeInBytes = 4, Sha256 = blob.Sha256, RowCount = 1, GeneratedByUserId = user.Id, ScopeGrants = [new OfficialReportGenerationScopeGrant { MunicipalityId = municipality.Id, Dimension = OfficialReportScopeDimension.Department, Value = "1" }] });
        await context.SaveChangesAsync();
    }

    private sealed class FixedTenantContext(long municipalityId) : ITenantContext { public long? MunicipalityId => municipalityId; public bool IsSystem => false; public string? UserId => "test"; }
    private sealed class SystemTenantContext : ITenantContext { public long? MunicipalityId => null; public bool IsSystem => true; public string? UserId => "system"; }
}

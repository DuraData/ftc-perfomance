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
    public async Task Security_history_model_has_unique_public_identity_and_portable_concurrency()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, new FixedTenantContext(null, "system", true));
        await context.Database.EnsureCreatedAsync();

        foreach (var type in new[] { typeof(LoginAuditLog), typeof(SecurityUserRoleAssignment), typeof(UserScope), typeof(UserAssignment) })
        {
            var entity = context.Model.FindEntityType(type)!;
            entity.GetIndexes().Should().ContainSingle(index => index.IsUnique
                && index.Properties.Count == 1 && index.Properties[0].Name == "PublicId");
        }

        foreach (var type in new[] { typeof(UserScope), typeof(UserAssignment) })
        {
            var property = context.Model.FindEntityType(type)!.FindProperty("RowVersion")!;
            property.IsConcurrencyToken.Should().BeTrue();
        }
    }

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

        var employeeMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "EMPLOYEE")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        employeeMembers.Select(item => item.MemberCode).Should().Contain(["EmployeeNumber", "EmailAddress", "IdentityUserId"]);
        Assert.All(employeeMembers.Where(item => item.MemberCode is "EmployeeNumber" or "EmailAddress" or "IdentityUserId"), item => Assert.True(item.IsSensitive));
        Assert.Equal(6, await context.Permissions.CountAsync(item => item.ResourceCode == "EMPLOYEE"
            && new[] { "EmployeeNumber", "EmailAddress", "IdentityUserId" }.Contains(item.MemberCode)));

        foreach (var resourceCode in new[] { "OPMS_SUBMISSION", "IPMS_SUBMISSION" })
        {
            var submissionMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode)
                .Select(item => item.MemberCode)
                .ToArrayAsync();
            submissionMembers.Should().Contain(["ActualPerformance", "Variance", "VarianceReason", "CorrectiveMeasure", "SubmittedDate",
                "InternalAuditObservation", "InternalAuditComment", "InternalAuditFindings", "InternalAuditRecommendation",
                "InternalAuditScore", "InternalAuditAssessedBy", "InternalAuditRfi", "SuggestionActor", "SuggestionReason", "SuggestionCorrelationId",
                "SubmitterIdentity", "SubmitterScore", "VerifierIdentity", "VerifierComment", "VerifierScore",
                "ApproverIdentity", "ApproverComment", "ApproverScore", "PmsIdentity", "PmsComment", "PmsRecommendation", "PmsScore", "PmsRfi",
                "WithdrawalReason", "WithdrawalActor"]);
            var internalAuditMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode && item.MemberCode.StartsWith("InternalAudit"))
                .ToArrayAsync();
            internalAuditMembers.Should().HaveCount(7);
            internalAuditMembers.Should().OnlyContain(item => item.IsSensitive);
            (await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode && item.MemberCode != null)).Should().Be(38);
        }

        foreach (var resourceCode in new[] { "OPMS_KPI", "IPMS_KPI" })
        {
            var revisionMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode && item.MemberCode.StartsWith("Revision"))
                .OrderBy(item => item.MemberCode)
                .ToArrayAsync();
            revisionMembers.Select(item => item.MemberCode).Should().Equal(
                "RevisionActor", "RevisionApprovalReference", "RevisionOriginalValue", "RevisionReason", "RevisionRevisedValue");
            revisionMembers.Should().OnlyContain(item => item.IsSensitive && item.IsSystemManaged);
            (await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode
                && item.MemberCode != null && item.MemberCode.StartsWith("Revision"))).Should().Be(5);
        }

        foreach (var resourceCode in new[] { "OPMS_WORKFLOW", "IPMS_WORKFLOW" })
        {
            var workflowMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode)
                .OrderBy(item => item.MemberCode)
                .ToArrayAsync();
            workflowMembers.Select(item => item.MemberCode).Should().Equal(
                "ActionActorUserId", "ActionComment", "ActionRatingValue", "StageRatingAchievementPercent",
                "StageRatingComment", "StageRatingRatedByName", "StageRatingRatedByUserId", "StageRatingValue",
                "WindowExceptionApprovedBy", "WindowExceptionReason", "WindowExceptionScope");
            workflowMembers.Should().OnlyContain(item => item.IsSensitive);
            workflowMembers.Where(item => item.MemberCode is not ("WindowExceptionReason" or "WindowExceptionScope"))
                .Should().OnlyContain(item => item.IsSystemManaged);
            workflowMembers.Where(item => item.MemberCode is "WindowExceptionReason" or "WindowExceptionScope")
                .Should().OnlyContain(item => !item.IsSystemManaged);
            (await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode && item.MemberCode != null)).Should().Be(13);
            (await context.SecurityResources.SingleAsync(item => item.Code == resourceCode)).SupportsFieldSecurity.Should().BeTrue();
        }

        foreach (var resourceCode in new[] { "OPMS_RFI", "IPMS_RFI" })
        {
            var rfiMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode)
                .OrderBy(item => item.MemberCode)
                .ToArrayAsync();
            rfiMembers.Select(item => item.MemberCode).Should().Equal(
                "ClosedBy", "EvidenceLinkedBy", "EvidenceMetadata", "Question", "RaisedBy", "RespondedBy", "Response");
            rfiMembers.Should().OnlyContain(item => item.IsSensitive);
            rfiMembers.Where(item => item.MemberCode is not ("Question" or "Response"))
                .Should().OnlyContain(item => item.IsSystemManaged);
            rfiMembers.Where(item => item.MemberCode is "Question" or "Response")
                .Should().OnlyContain(item => !item.IsSystemManaged);
            (await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode && item.MemberCode != null)).Should().Be(9);
            (await context.SecurityResources.SingleAsync(item => item.Code == resourceCode)).SupportsFieldSecurity.Should().BeTrue();
        }

        var authenticationMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "AUTHENTICATION")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        authenticationMembers.Select(item => item.MemberCode).Should().Equal(
            "EventIpAddress", "EventUserId", "ExpectedEmail", "Issuer", "Subject", "UserEmail");
        Assert.All(authenticationMembers, item => Assert.True(item.IsSensitive));
        Assert.Equal(9, await context.Permissions.CountAsync(item => item.ResourceCode == "AUTHENTICATION" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "AUTHENTICATION")).SupportsFieldSecurity);

        var stakeholderMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "IDP_STAKEHOLDER")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        stakeholderMembers.Select(item => item.MemberCode).Should().Equal("ContactEmail", "ContactPerson");
        Assert.All(stakeholderMembers, item => Assert.True(item.IsSensitive));
        Assert.Equal(4, await context.Permissions.CountAsync(item => item.ResourceCode == "IDP_STAKEHOLDER" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "IDP_STAKEHOLDER")).SupportsFieldSecurity);
        var loginAuditMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "LOGIN_AUDIT")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "Email", "FailureReason", "IpAddress", "UserAgent", "UserId" }, loginAuditMembers.Select(item => item.MemberCode));
        Assert.All(loginAuditMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(5, await context.Permissions.CountAsync(item => item.ResourceCode == "LOGIN_AUDIT" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "LOGIN_AUDIT")).SupportsFieldSecurity);

        var auditTrailMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "AUDIT_TRAIL")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "ChangedBy", "CorrelationId", "EntityId", "IpAddress", "NewValue", "OldValue", "Reason", "SessionId", "UserAgent" },
            auditTrailMembers.Select(item => item.MemberCode));
        Assert.All(auditTrailMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(9, await context.Permissions.CountAsync(item => item.ResourceCode == "AUDIT_TRAIL" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "AUDIT_TRAIL")).SupportsFieldSecurity);

        var notificationDeliveryMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "NOTIFICATION_DELIVERY")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "AggregateId", "Error", "LastError", "ProviderReference", "RecipientUserId", "ResponseDetail" },
            notificationDeliveryMembers.Select(item => item.MemberCode));
        Assert.All(notificationDeliveryMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(6, await context.Permissions.CountAsync(item => item.ResourceCode == "NOTIFICATION_DELIVERY" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "NOTIFICATION_DELIVERY")).SupportsFieldSecurity);
        Assert.Single(await context.Permissions.Where(item => item.Code == "NOTIFICATION_DELIVERY.RETRY").ToArrayAsync());

        var notificationPolicyMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "NOTIFICATION_POLICY")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "MessageTemplate", "RecipientValues", "TitleTemplate" },
            notificationPolicyMembers.Select(item => item.MemberCode));
        Assert.All(notificationPolicyMembers, item => { Assert.True(item.IsSensitive); Assert.False(item.IsSystemManaged); });
        Assert.Equal(6, await context.Permissions.CountAsync(item => item.ResourceCode == "NOTIFICATION_POLICY" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "NOTIFICATION_POLICY")).SupportsFieldSecurity);
        Assert.Equal(6, await context.SecurityActionDefinitions.CountAsync(item => item.ResourceCode == "NOTIFICATION_POLICY"));

        foreach (var resourceCode in new[] { "OPMS_POE", "IPMS_POE" })
        {
            var evidenceMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode)
                .OrderBy(item => item.MemberCode)
                .ToArrayAsync();
            Assert.Equal(new[]
                {
                    "AssessedByName", "AssessedByUserId", "AssessmentComment", "AssessmentCorrelationId",
                    "DisposalDetail", "DisposalRequestedByName", "DisposalRequestedByUserId",
                    "LegalHoldActorName", "LegalHoldActorUserId", "ReplacedByName", "ReplacedByUserId",
                    "ReplacementCorrelationId", "ScanDetail", "ScannerProvider", "ScannerReference",
                    "UploadedByName", "UploadedByUserId"
                },
                evidenceMembers.Select(item => item.MemberCode));
            Assert.All(evidenceMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
            Assert.Equal(17, await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode && item.MemberCode != null));
            Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == resourceCode)).SupportsFieldSecurity);
        }

        foreach (var resourceCode in new[] { "OPMS_REPORT", "IPMS_REPORT" })
        {
            var reportMembers = await context.SecurityMemberDefinitions
                .Where(item => item.ResourceCode == resourceCode)
                .OrderBy(item => item.MemberCode)
                .ToArrayAsync();
            Assert.Equal(new[]
                {
                    "GenerationDataVersionReference", "GenerationFilterJson", "GenerationGeneratedBy", "GenerationScopeJson",
                    "JobDistributionOutboxPublicId", "JobLastError", "JobRecipientUserIds", "JobRequestedBy", "JobRetryReason",
                    "ScheduleCreatedBy", "ScheduleRecipientValues"
                },
                reportMembers.Select(item => item.MemberCode));
            Assert.All(reportMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
            Assert.Equal(11, await context.Permissions.CountAsync(item => item.ResourceCode == resourceCode && item.MemberCode != null));
            Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == resourceCode)).SupportsFieldSecurity);
        }

        var idpDocumentMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "IDP_DOCUMENT")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "ScanDetail", "ScannerProvider", "ScannerReference", "UploadedByName", "UploadedByUserId" },
            idpDocumentMembers.Select(item => item.MemberCode));
        Assert.All(idpDocumentMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(5, await context.Permissions.CountAsync(item => item.ResourceCode == "IDP_DOCUMENT" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "IDP_DOCUMENT")).SupportsFieldSecurity);
        Assert.Single(await context.Permissions.Where(item => item.Code == "IDP_DOCUMENT.RESCAN").ToArrayAsync());

        var opmsImportMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "OPMS_KPI" && item.MemberCode.StartsWith("Import"))
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "ImportActor", "ImportClientRequestId", "ImportErrorDetail", "ImportRowPayload", "ImportSourceFileName", "ImportSourceHash" },
            opmsImportMembers.Select(item => item.MemberCode));
        Assert.All(opmsImportMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(6, await context.Permissions.CountAsync(item => item.ResourceCode == "OPMS_KPI" && item.MemberCode != null && item.MemberCode.StartsWith("Import")));

        var idpImportMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "IDP_PLAN" && item.MemberCode.StartsWith("Import"))
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "ImportActor", "ImportClientRequestId", "ImportErrorDetail", "ImportRowPayload", "ImportSourceFileName", "ImportSourceHash" },
            idpImportMembers.Select(item => item.MemberCode));
        Assert.All(idpImportMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(6, await context.Permissions.CountAsync(item => item.ResourceCode == "IDP_PLAN" && item.MemberCode != null && item.MemberCode.StartsWith("Import")));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "IDP_PLAN")).SupportsFieldSecurity);

        var idpCollaborationMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "IDP_PLAN" && !item.MemberCode.StartsWith("Import"))
            .OrderBy(item => item.MemberCode).ToArrayAsync();
        idpCollaborationMembers.Select(item => item.MemberCode).Should().Equal(
            "CollaborationActor", "CollaborationComment", "ObjectiveBudgetAllocation", "ObjectiveStrategicOwner",
            "TaskAssignee", "TaskAssigner", "TaskContent", "VersionCreator", "VersionSummary");
        idpCollaborationMembers.Should().OnlyContain(item => item.IsSensitive);
        idpCollaborationMembers.Where(item => item.MemberCode is "CollaborationActor" or "TaskAssigner" or "VersionCreator")
            .Should().OnlyContain(item => item.IsSystemManaged);
        idpCollaborationMembers.Where(item => item.MemberCode is not ("CollaborationActor" or "TaskAssigner" or "VersionCreator"))
            .Should().OnlyContain(item => !item.IsSystemManaged);
        (await context.Permissions.CountAsync(item => item.ResourceCode == "IDP_PLAN" && item.MemberCode != null && !item.MemberCode.StartsWith("Import"))).Should().Be(15);

        var idpIndicatorMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "IDP_INDICATOR").OrderBy(item => item.MemberCode).ToArrayAsync();
        idpIndicatorMembers.Select(item => item.MemberCode).Should().Equal("AnnualActualValue", "AnnualProgressComment", "AnnualTargetValue");
        idpIndicatorMembers.Should().OnlyContain(item => item.IsSensitive && !item.IsSystemManaged);
        (await context.Permissions.CountAsync(item => item.ResourceCode == "IDP_INDICATOR" && item.MemberCode != null)).Should().Be(6);

        var idpProjectMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "IDP_PROJECT").OrderBy(item => item.MemberCode).ToArrayAsync();
        idpProjectMembers.Select(item => item.MemberCode).Should().Equal(
            "BudgetSnapshotActual", "BudgetSnapshotApproved", "BudgetSnapshotPlanned", "BudgetSnapshotSource",
            "ProgrammeActualExpenditure", "ProgrammeApprovedBudget", "ProgrammePlannedBudget", "ProjectBudget", "ProjectFundingSource");
        idpProjectMembers.Should().OnlyContain(item => item.IsSensitive && !item.IsSystemManaged);
        (await context.Permissions.CountAsync(item => item.ResourceCode == "IDP_PROJECT" && item.MemberCode != null)).Should().Be(18);

        var tidSourceMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "TID" && item.MemberCode.StartsWith("Source"))
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "SourceScanDetail", "SourceScannerProvider", "SourceScannerReference", "SourceUploadedByName", "SourceUploadedByUserId" },
            tidSourceMembers.Select(item => item.MemberCode));
        Assert.All(tidSourceMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(5, await context.Permissions.CountAsync(item => item.ResourceCode == "TID" && item.MemberCode != null && item.MemberCode.StartsWith("Source")));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "TID")).SupportsFieldSecurity);
        Assert.Single(await context.Permissions.Where(item => item.Code == "TID.RESCAN_SOURCE").ToArrayAsync());
        var tidCreator = await context.SecurityMemberDefinitions.SingleAsync(item => item.ResourceCode == "TID" && item.MemberCode == "CreatedByUserId");
        Assert.True(tidCreator.IsSensitive);
        Assert.True(tidCreator.IsSystemManaged);
        Assert.Single(await context.Permissions.Where(item => item.Code == "TID.CreatedByUserId.READ").ToArrayAsync());

        var strategicDocumentMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "STRATEGIC_DOCUMENT")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        Assert.Equal(new[] { "ApprovedByUserId", "CreatedByUserId", "EventActorUserId", "EventReason", "PublishedByUserId", "ScanDetail", "ScannerProvider", "ScannerReference" },
            strategicDocumentMembers.Select(item => item.MemberCode));
        Assert.All(strategicDocumentMembers, item => { Assert.True(item.IsSensitive); Assert.True(item.IsSystemManaged); });
        Assert.Equal(8, await context.Permissions.CountAsync(item => item.ResourceCode == "STRATEGIC_DOCUMENT" && item.MemberCode != null));
        Assert.True((await context.SecurityResources.SingleAsync(item => item.Code == "STRATEGIC_DOCUMENT")).SupportsFieldSecurity);
        Assert.Single(await context.Permissions.Where(item => item.Code == "STRATEGIC_DOCUMENT.RESCAN").ToArrayAsync());

        var strategicRiskMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "STRATEGIC_RISK")
            .OrderBy(item => item.MemberCode)
            .ToArrayAsync();
        strategicRiskMembers.Select(item => item.MemberCode).Should().Equal("LinkReason", "RiskDescription", "UnlinkReason");
        strategicRiskMembers.Should().OnlyContain(item => item.IsSensitive);
        strategicRiskMembers.Where(item => item.MemberCode is "LinkReason" or "UnlinkReason")
            .Should().OnlyContain(item => item.IsSystemManaged);
        strategicRiskMembers.Single(item => item.MemberCode == "RiskDescription").IsSystemManaged.Should().BeFalse();
        (await context.Permissions.CountAsync(item => item.ResourceCode == "STRATEGIC_RISK" && item.MemberCode != null)).Should().Be(4);
        (await context.SecurityResources.SingleAsync(item => item.Code == "STRATEGIC_RISK")).SupportsFieldSecurity.Should().BeTrue();

        var c88IndicatorMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "C88_INDICATOR").OrderBy(item => item.MemberCode).ToArrayAsync();
        c88IndicatorMembers.Select(item => item.MemberCode).Should().Equal("MappingReason", "PlanMissingDataExplanation");
        c88IndicatorMembers.Should().OnlyContain(item => item.IsSensitive && !item.IsSystemManaged);
        (await context.Permissions.CountAsync(item => item.ResourceCode == "C88_INDICATOR" && item.MemberCode != null)).Should().Be(4);

        var c88ReportMembers = await context.SecurityMemberDefinitions
            .Where(item => item.ResourceCode == "C88_REPORT").OrderBy(item => item.MemberCode).ToArrayAsync();
        c88ReportMembers.Select(item => item.MemberCode).Should().Equal("CalculatedValue", "ComplianceComment", "ComplianceResponse",
            "DataElementMissingDataExplanation", "DataElementValue", "MissingDataExplanation", "WorkflowActor", "WorkflowReason");
        c88ReportMembers.Should().OnlyContain(item => item.IsSensitive);
        c88ReportMembers.Where(item => item.MemberCode is "CalculatedValue" or "WorkflowActor").Should().OnlyContain(item => item.IsSystemManaged);
        c88ReportMembers.Where(item => item.MemberCode is not ("CalculatedValue" or "WorkflowActor")).Should().OnlyContain(item => !item.IsSystemManaged);
        (await context.Permissions.CountAsync(item => item.ResourceCode == "C88_REPORT" && item.MemberCode != null)).Should().Be(14);
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

        var list = await controller.GetUsersPage(new PagedQueryRequest { PageSize = 100, SortBy = "name" });
        var envelope = Assert.IsType<ApiResponse<PagedResponse<UserDetailResponse>>>(Assert.IsType<OkObjectResult>(list.Result).Value);
        Assert.Equal(2, envelope.Data!.TotalCount);
        Assert.All(envelope.Data.Items, item =>
        {
            Assert.Null(item.User.Email);
            Assert.Null(item.User.PhoneNumber);
        });
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetUsers().Result).StatusCode);

        var crossTenant = await controller.GetUser(otherTenant.PublicId);
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

        var response = await controller.UpdateUser(target.PublicId, new UpdateUserRequest("Changed", "Name", "0999999999", true));

        Assert.IsType<ForbidResult>(response.Result);
        Assert.Equal("0222222222", (await context.Users.SingleAsync(item => item.Id == target.Id)).PhoneNumber);
    }

    [Fact]
    public async Task User_directory_page_is_bounded_searchable_and_tenant_scoped()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(301, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        context.AddRange(new Municipality { Id = 301, Code = "M301", Name = "Municipality 301" },
            new Municipality { Id = 302, Code = "M302", Name = "Municipality 302" });
        var actor = User("actor", 301, "actor@example.test", "0111111111");
        var users = Enumerable.Range(1, 31).Select(index =>
            User($"tenant-{index:00}", 301, $"person{index:00}@example.test", $"02{index:00000000}")).ToArray();
        var other = User("other", 302, "person99@example.test", "0399999999");
        context.Add(actor); context.AddRange(users); context.Add(other);
        await context.SaveChangesAsync();
        var directory = users.Append(actor).Append(other).ToDictionary(item => item.Id);
        var controller = Controller(context, tenant, actor, directory,
            Access(actor, "USER.READ", "USER.Email.READ", "USER.PhoneNumber.READ").Object);

        var result = await controller.GetUsersPage(new PagedQueryRequest
        {
            Page = 2, PageSize = 10, SortBy = "email", SortDirection = "asc", Search = "example.test"
        });

        var envelope = Assert.IsType<ApiResponse<PagedResponse<UserDetailResponse>>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(32, envelope.Data!.TotalCount);
        Assert.Equal(10, envelope.Data.Items.Length);
        Assert.Equal(2, envelope.Data.Page);
        Assert.Equal(4, envelope.Data.TotalPages);
        Assert.DoesNotContain(envelope.Data.Items, item => item.User.PublicId == other.PublicId);
    }

    [Fact]
    public async Task User_scope_and_assignment_histories_are_tenant_authorized_searchable_pages()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(401, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        context.AddRange(new Municipality { Id = 401, Code = "M401", Name = "Municipality 401" },
            new Municipality { Id = 402, Code = "M402", Name = "Municipality 402" });
        var actor = User("actor", 401, "actor@example.test", "0111111111");
        var target = User("target", 401, "target@example.test", "0222222222");
        var other = User("other", 402, "other@example.test", "0333333333");
        context.AddRange(actor, target, other);
        await context.SaveChangesAsync();
        for (var index = 1; index <= 12; index++)
        {
            context.UserScopes.Add(new UserScope
            {
                UserId = target.Id, MunicipalityId = 401, ScopeType = ScopeType.AssignedTargetScope,
                TargetId = $"target-{index:00}", EffectiveFrom = DateTime.UtcNow.AddDays(-index), IsActive = true
            });
            context.UserAssignments.Add(new UserAssignment
            {
                UserId = target.Id, AssignmentType = AssignmentType.AdditionalSubmitterAssignment,
                TargetId = $"target-{index:00}", ValidFromUtc = DateTime.UtcNow.AddDays(-index), IsActive = index % 2 == 0
            });
        }
        context.UserScopes.Add(new UserScope { UserId = other.Id, MunicipalityId = 402, ScopeType = ScopeType.System, IsActive = true });
        context.UserAssignments.Add(new UserAssignment { UserId = other.Id, AssignmentType = AssignmentType.DelegatedAssignment, IsActive = true });
        await context.SaveChangesAsync();
        var directory = new[] { actor, target, other }.ToDictionary(item => item.Id);
        var controller = Controller(context, tenant, actor, directory, Access(actor, "SECURITY.VIEW_EFFECTIVE").Object);

        var scopesResult = await controller.GetUserScopesPage(target.PublicId, new PagedQueryRequest
        {
            Page = 2, PageSize = 5, SortBy = "effectiveFrom", SortDirection = "desc"
        });
        var scopes = Assert.IsType<ApiResponse<PagedResponse<UserScopeResponse>>>(Assert.IsType<OkObjectResult>(scopesResult.Result).Value).Data!;
        Assert.Equal(12, scopes.TotalCount);
        Assert.Equal(5, scopes.Items.Length);
        Assert.Equal(3, scopes.TotalPages);
        Assert.All(scopes.Items, item =>
        {
            Assert.NotEqual(Guid.Empty, item.PublicId);
            Assert.False(string.IsNullOrWhiteSpace(item.RowVersion));
        });

        var assignmentsResult = await controller.GetUserAssignmentsPage(target.PublicId, new PagedQueryRequest
        {
            Page = 1, PageSize = 10, Search = "target-12", SortBy = "validFrom", SortDirection = "asc"
        });
        var assignments = Assert.IsType<ApiResponse<PagedResponse<UserAssignmentResponse>>>(Assert.IsType<OkObjectResult>(assignmentsResult.Result).Value).Data!;
        Assert.Equal(1, assignments.TotalCount);
        var assignment = Assert.Single(assignments.Items);
        Assert.Equal("target-12", assignment.TargetId);
        Assert.NotEqual(Guid.Empty, assignment.PublicId);
        Assert.False(string.IsNullOrWhiteSpace(assignment.RowVersion));

        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetUserScopes(target.PublicId).Result).StatusCode);
        Assert.Equal(StatusCodes.Status410Gone, Assert.IsType<ObjectResult>(controller.GetUserAssignments(target.PublicId).Result).StatusCode);
        Assert.IsType<NotFoundObjectResult>((await controller.GetUserScopesPage(other.PublicId, new PagedQueryRequest())).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetUserAssignmentsPage(target.PublicId, new PagedQueryRequest { SortBy = "unsafe" })).Result);
    }

    [Fact]
    public async Task Scope_change_requires_reason_and_user_concurrency_and_preserves_stable_history()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(451, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        var municipality = new Municipality { Id = 451, Code = "M451", Name = "Municipality 451" };
        var actor = User("actor", 451, "actor@example.test", "0111111111");
        var target = User("target", 451, "target@example.test", "0222222222");
        var prior = new UserScope { UserId = target.Id, MunicipalityId = 451, ScopeType = ScopeType.InstitutionScope, IsActive = true };
        context.AddRange(municipality, actor, target, prior);
        await context.SaveChangesAsync();
        var originalUserVersion = Convert.ToBase64String(target.RowVersion);
        var priorPublicId = prior.PublicId;
        context.ChangeTracker.Clear();

        var controller = Controller(context, tenant, actor, new[] { actor, target }.ToDictionary(item => item.Id),
            Access(actor, "SECURITY.ASSIGN_ROLES").Object);
        var response = await controller.SetUserScopes(target.PublicId, new UpdateUserScopesRequest(
            [new UserScopeItemRequest(nameof(ScopeType.AssignedTargetScope), null, null, "target-451", null, null, null)],
            originalUserVersion, "Approved target responsibility scope"));

        Assert.IsType<OkObjectResult>(response.Result);
        var history = await context.UserScopes.OrderBy(item => item.Id).ToArrayAsync();
        Assert.Equal(2, history.Length);
        Assert.Equal(priorPublicId, history[0].PublicId);
        Assert.False(history[0].IsActive);
        Assert.NotNull(history[0].EffectiveTo);
        Assert.NotEqual(Guid.Empty, history[1].PublicId);
        Assert.True(history[1].IsActive);
        Assert.All(history, item => Assert.NotEmpty(item.RowVersion));
        Assert.Equal("Approved target responsibility scope", (await context.AuditTrails.SingleAsync()).Reason);

        context.ChangeTracker.Clear();
        var stale = await controller.SetUserScopes(target.PublicId, new UpdateUserScopesRequest(
            [new UserScopeItemRequest(nameof(ScopeType.System), null, null, null, null, null, null)],
            originalUserVersion, "Attempt stale scope replacement"));
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.Equal(2, await context.UserScopes.CountAsync());
    }

    [Fact]
    public async Task Assignment_change_preserves_history_validates_tenant_and_advances_user_concurrency()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var tenant = new FixedTenantContext(501, "actor");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options, tenant);
        await context.Database.EnsureCreatedAsync();
        context.AddRange(new Municipality { Id = 501, Code = "M501", Name = "Municipality 501" },
            new Municipality { Id = 502, Code = "M502", Name = "Municipality 502" });
        var actor = User("actor", 501, "actor@example.test", "0111111111");
        var target = User("target", 501, "target@example.test", "0222222222");
        var otherTenant = User("other", 502, "other@example.test", "0333333333");
        context.AddRange(actor, target, otherTenant);
        var prior = new UserAssignment
        {
            UserId = target.Id,
            AssignmentType = AssignmentType.AdditionalSubmitterAssignment,
            TargetId = "prior-target",
            ValidFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };
        context.UserAssignments.Add(prior);
        await context.SaveChangesAsync();
        var originalVersion = Convert.ToBase64String(target.RowVersion);
        context.ChangeTracker.Clear();

        var directory = new[] { actor, target, otherTenant }.ToDictionary(item => item.Id);
        var controller = Controller(context, tenant, actor, directory, Access(actor, "SECURITY.ASSIGN_ROLES").Object);
        var now = DateTime.UtcNow;
        var response = await controller.SetUserAssignments(target.PublicId, new UpdateUserAssignmentsRequest(
            [new UserAssignmentItemRequest(nameof(AssignmentType.DelegatedAssignment), actor.PublicId, true,
                now.AddMinutes(-1), now.AddDays(1), "delegated-target", null, null, null)],
            originalVersion, "Approved temporary submission delegation"));

        Assert.IsType<OkObjectResult>(response.Result);
        var rows = await context.UserAssignments.OrderBy(item => item.Id).ToArrayAsync();
        Assert.Equal(2, rows.Length);
        Assert.False(rows[0].IsActive);
        Assert.NotNull(rows[0].ValidToUtc);
        Assert.True(rows[1].IsActive);
        Assert.Equal(actor.Id, rows[1].DelegatorUserId);
        Assert.NotEqual(originalVersion, Convert.ToBase64String((await context.Users.SingleAsync(item => item.Id == target.Id)).RowVersion));
        Assert.Equal("Approved temporary submission delegation", (await context.AuditTrails.SingleAsync()).Reason);

        context.ChangeTracker.Clear();
        var refreshedTarget = await context.Users.SingleAsync(item => item.Id == target.Id);
        var invalid = await controller.SetUserAssignments(target.PublicId, new UpdateUserAssignmentsRequest(
            [new UserAssignmentItemRequest(nameof(AssignmentType.DelegatedAssignment), otherTenant.PublicId, true,
                now, now.AddDays(1), "cross-tenant-target", null, null, null)],
            Convert.ToBase64String(refreshedTarget.RowVersion), "Attempt cross tenant delegation"));
        Assert.IsType<BadRequestObjectResult>(invalid.Result);
        Assert.Equal(2, await context.UserAssignments.CountAsync());

        context.ChangeTracker.Clear();
        var stale = await controller.SetUserAssignments(target.PublicId, new UpdateUserAssignmentsRequest(
            [new UserAssignmentItemRequest(nameof(AssignmentType.TaskAssignee), null, true,
                now, now.AddDays(2), null, null, null, "stale-task")],
            originalVersion, "Attempt stale assignment update"));
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.Equal(2, await context.UserAssignments.CountAsync());
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

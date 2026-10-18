using FTCERP.Host.Application.Services;

namespace FTCERP.Tests;

public sealed class ThreeMunicipalityAcceptanceMatrixTests
{
    [Fact]
    public async Task Three_differently_configured_municipalities_execute_their_own_governed_profiles_without_cross_tenant_leakage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var systemContext = new ApplicationDbContext(options))
        {
            await systemContext.Database.EnsureCreatedAsync();
            var matrix = await SeedMatrixAsync(systemContext);
            var workflow = new ConfigurableWorkflowService(systemContext);

            var localCompleted = await workflow.PrepareActionAsync(
                SubmissionKind.Opms, matrix.Local.SubmissionId, matrix.Local.Period.Id,
                matrix.Local.Submitter.Id, matrix.Local.Submitter.Id,
                "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit,
                "Local profile submission", null, "matrix-local-submit");
            localCompleted.Allowed.Should().BeTrue(localCompleted.Reason);
            localCompleted.Instance!.State.Should().Be(WorkflowInstanceState.Completed);
            await systemContext.SaveChangesAsync();

            var enterpriseSubmitted = await workflow.PrepareActionAsync(
                SubmissionKind.Opms, matrix.Enterprise.SubmissionId, matrix.Enterprise.Period.Id,
                matrix.Enterprise.Submitter.Id, matrix.Enterprise.Submitter.Id,
                "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit,
                "Enterprise profile submission", 4m, "matrix-enterprise-submit");
            enterpriseSubmitted.Allowed.Should().BeTrue(enterpriseSubmitted.Reason);
            enterpriseSubmitted.Instance!.State.Should().Be(WorkflowInstanceState.Active);
            await systemContext.SaveChangesAsync();

            var enterpriseVerified = await workflow.PrepareActionAsync(
                SubmissionKind.Opms, matrix.Enterprise.SubmissionId, matrix.Enterprise.Period.Id,
                matrix.Enterprise.Submitter.Id, matrix.Enterprise.Reviewer.Id,
                "OPMS_SUBMISSION.VERIFY", WorkflowActionOutcome.Approve,
                "Enterprise profile verification", 2m, "matrix-enterprise-verify");
            enterpriseVerified.Allowed.Should().BeTrue(enterpriseVerified.Reason);
            enterpriseVerified.Instance!.State.Should().Be(WorkflowInstanceState.Completed);
            await systemContext.SaveChangesAsync();

            var hybridSubmitted = await workflow.PrepareActionAsync(
                SubmissionKind.Opms, matrix.Hybrid.SubmissionId, matrix.Hybrid.Period.Id,
                matrix.Hybrid.Submitter.Id, matrix.Hybrid.Submitter.Id,
                "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit,
                "Hybrid profile submission", null, "matrix-hybrid-submit");
            hybridSubmitted.Allowed.Should().BeTrue(hybridSubmitted.Reason);
            await systemContext.SaveChangesAsync();

            var hybridProjection = await workflow.GetProjectionAsync(
                SubmissionKind.Opms, matrix.Hybrid.SubmissionId, matrix.Hybrid.Period.Id,
                matrix.Hybrid.Submitter.Id, matrix.Hybrid.Reviewer.Id);
            hybridProjection.CurrentStageCode.Should().Be("VERIFY");
            hybridProjection.ActorEligibleForBypass.Should().BeTrue();

            var hybridBypassed = await workflow.PrepareActionAsync(
                SubmissionKind.Opms, matrix.Hybrid.SubmissionId, matrix.Hybrid.Period.Id,
                matrix.Hybrid.Submitter.Id, matrix.Hybrid.Reviewer.Id,
                "OPMS_SUBMISSION.APPROVE_BYPASS", WorkflowActionOutcome.Bypass,
                "Optional verification bypassed by approver", 0m, "matrix-hybrid-bypass");
            hybridBypassed.Allowed.Should().BeTrue(hybridBypassed.Reason);
            hybridBypassed.Instance!.State.Should().Be(WorkflowInstanceState.Completed);
            hybridBypassed.Action!.Outcome.Should().Be(WorkflowActionOutcome.Bypass);
            await systemContext.SaveChangesAsync();

            var persistedProfiles = await systemContext.Municipalities.AsNoTracking()
                .OrderBy(item => item.Code)
                .Select(item => new
                {
                    item.Code,
                    item.AuthenticationMode,
                    item.TidEnabled,
                    item.TidAllKpisRequired
                })
                .ToArrayAsync();
            persistedProfiles.Should().BeEquivalentTo(new[]
            {
                new { Code = "MATRIX-A", AuthenticationMode = AuthenticationMode.Local, TidEnabled = false, TidAllKpisRequired = false },
                new { Code = "MATRIX-B", AuthenticationMode = AuthenticationMode.MicrosoftEntraId, TidEnabled = true, TidAllKpisRequired = false },
                new { Code = "MATRIX-C", AuthenticationMode = AuthenticationMode.Hybrid, TidEnabled = true, TidAllKpisRequired = true }
            });

            var hybridRating = await systemContext.SubmissionStageRatings.AsNoTracking()
                .SingleAsync(item => item.MunicipalityId == matrix.Hybrid.Municipality.Id);
            hybridRating.Value.Should().Be(0m);
            hybridRating.LabelSnapshot.Should().Be("Target met");
            (await systemContext.RatingSchemeValues.AsNoTracking()
                    .SingleAsync(item => item.MunicipalityId == matrix.Hybrid.Municipality.Id && item.Value == 1m))
                .Label.Should().Be("Target not met");
        }

        await AssertTenantViewAsync(options, 11001, "matrix-a-submitter", "MATRIX-A", 1, 1, 0);
        await AssertTenantViewAsync(options, 11002, "matrix-b-submitter", "MATRIX-B", 2, 2, 2);
        await AssertTenantViewAsync(options, 11003, "matrix-c-submitter", "MATRIX-C", 3, 2, 1);

        await using var localTenant = new ApplicationDbContext(
            options, IdpTestFixture.Tenant(11001, "matrix-a-submitter"));
        var deniedCrossTenantAction = await new ConfigurableWorkflowService(localTenant).PrepareActionAsync(
            SubmissionKind.Opms, "matrix-b-foreign-attempt", 12002,
            "matrix-a-submitter", "matrix-a-submitter",
            "OPMS_SUBMISSION.SUBMIT", WorkflowActionOutcome.Submit,
            null, null, "matrix-cross-tenant-attempt");
        deniedCrossTenantAction.Allowed.Should().BeFalse();
        deniedCrossTenantAction.Reason.Should().Be("Reporting period not found.");
        localTenant.ChangeTracker.Entries<SubmissionWorkflowInstance>().Should().BeEmpty();
        localTenant.ChangeTracker.Entries<SubmissionWorkflowAction>().Should().BeEmpty();
    }

    private static async Task AssertTenantViewAsync(
        DbContextOptions<ApplicationDbContext> options,
        long municipalityId,
        string userId,
        string expectedMunicipalityCode,
        int expectedStageCount,
        int expectedActionCount,
        int expectedRatingCount)
    {
        await using var context = new ApplicationDbContext(options, IdpTestFixture.Tenant(municipalityId, userId));

        (await context.Municipalities.AsNoTracking().Select(item => item.Code).SingleAsync())
            .Should().Be(expectedMunicipalityCode);
        (await context.WorkflowDefinitions.AsNoTracking().CountAsync()).Should().Be(1);
        (await context.WorkflowStageDefinitions.AsNoTracking().CountAsync()).Should().Be(expectedStageCount);
        (await context.SubmissionWorkflowInstances.AsNoTracking().CountAsync()).Should().Be(1);
        (await context.SubmissionWorkflowActions.AsNoTracking().CountAsync()).Should().Be(expectedActionCount);
        (await context.SubmissionStageRatings.AsNoTracking().CountAsync()).Should().Be(expectedRatingCount);

        (await context.WorkflowDefinitions.AsNoTracking().Select(item => item.MunicipalityId).Distinct().SingleAsync())
            .Should().Be(municipalityId);
        (await context.SubmissionWorkflowActions.AsNoTracking().Select(item => item.MunicipalityId).Distinct().SingleAsync())
            .Should().Be(municipalityId);
    }

    private static async Task<AcceptanceMatrix> SeedMatrixAsync(ApplicationDbContext context)
    {
        var effectiveFrom = DateTime.UtcNow.AddDays(-1);
        var financialYear = new FinancialYear
        {
            Id = 10001,
            Code = "2040/41",
            Name = "2040/41",
            StartDate = new DateTime(2040, 7, 1),
            EndDate = new DateTime(2041, 6, 30)
        };

        var local = CreateProfile(
            11001, 11501, 12001, "MATRIX-A", AuthenticationMode.Local,
            tidEnabled: false, tidAllKpisRequired: false,
            "matrix-a-submitter", "matrix-a-reviewer", "matrix-a-submission");
        local.Definition.Stages.Add(Stage(local, 13001, "SUBMIT", 1, "OPMS_SUBMISSION.SUBMIT", terminal: true));

        var enterprise = CreateProfile(
            11002, 11502, 12002, "MATRIX-B", AuthenticationMode.MicrosoftEntraId,
            tidEnabled: true, tidAllKpisRequired: false,
            "matrix-b-submitter", "matrix-b-reviewer", "matrix-b-submission");
        var enterpriseScheme = Scheme(enterprise, 14002, "FIVE_POINT",
            (14021, 2m, "Partially achieved"),
            (14022, 4m, "Exceeded"));
        enterprise.Definition.Stages.Add(Stage(enterprise, 13002, "SUBMIT", 1, "OPMS_SUBMISSION.SUBMIT",
            requiresRating: true, scheme: enterpriseScheme));
        enterprise.Definition.Stages.Add(Stage(enterprise, 13003, "VERIFY", 2, "OPMS_SUBMISSION.VERIFY",
            terminal: true, requiresRating: true, scheme: enterpriseScheme, separateFromSubmitter: true));

        var hybrid = CreateProfile(
            11003, 11503, 12003, "MATRIX-C", AuthenticationMode.Hybrid,
            tidEnabled: true, tidAllKpisRequired: true,
            "matrix-c-submitter", "matrix-c-approver", "matrix-c-submission");
        var binaryScheme = Scheme(hybrid, 14003, "BINARY",
            (14031, 0m, "Target met"),
            (14032, 1m, "Target not met"));
        hybrid.Definition.Stages.Add(Stage(hybrid, 13004, "SUBMIT", 1, "OPMS_SUBMISSION.SUBMIT"));
        hybrid.Definition.Stages.Add(Stage(hybrid, 13005, "VERIFY", 2, "OPMS_SUBMISSION.VERIFY",
            optional: true, allowBypass: true));
        hybrid.Definition.Stages.Add(Stage(hybrid, 13006, "APPROVE", 3, "OPMS_SUBMISSION.APPROVE",
            terminal: true, requiresRating: true, scheme: binaryScheme, separateFromSubmitter: true));

        context.Add(financialYear);
        foreach (var profile in new[] { local, enterprise, hybrid })
        {
            profile.Year.FinancialYear = financialYear;
            context.AddRange(profile.Municipality, profile.Year, profile.Period, profile.Submitter, profile.Reviewer, profile.Definition);
        }
        context.AddRange(enterpriseScheme, binaryScheme);
        await context.SaveChangesAsync();
        return new(local, enterprise, hybrid);
    }

    private static MunicipalityProfile CreateProfile(
        long municipalityId,
        long municipalityYearId,
        long periodId,
        string code,
        AuthenticationMode authenticationMode,
        bool tidEnabled,
        bool tidAllKpisRequired,
        string submitterId,
        string reviewerId,
        string submissionId)
    {
        var municipality = new Municipality
        {
            Id = municipalityId,
            Code = code,
            Name = $"{code} Municipality",
            AuthenticationMode = authenticationMode,
            TidEnabled = tidEnabled,
            TidAllKpisRequired = tidAllKpisRequired,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        var year = new MunicipalityFinancialYear
        {
            Id = municipalityYearId,
            Municipality = municipality,
            FinancialYearId = 10001,
            IsCurrent = true,
            EffectiveFrom = new DateTime(2040, 7, 1)
        };
        var period = new ReportingPeriod
        {
            Id = periodId,
            MunicipalityFinancialYear = year,
            Code = "Q1",
            Name = "Quarter 1",
            PeriodType = ReportingPeriodType.Quarter1,
            Sequence = 1,
            StartDate = new DateTime(2040, 7, 1),
            EndDate = new DateTime(2040, 9, 30)
        };
        var submitter = IdpTestFixture.CreateUser(submitterId);
        submitter.Municipality = municipality;
        var reviewer = IdpTestFixture.CreateUser(reviewerId);
        reviewer.Municipality = municipality;
        var definition = new WorkflowDefinition
        {
            Id = municipalityId + 1000,
            Municipality = municipality,
            MunicipalityFinancialYear = year,
            SubmissionKind = SubmissionKind.Opms,
            Code = "PROFILE",
            Name = $"{code} workflow",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        return new(municipality, year, period, submitter, reviewer, definition, submissionId);
    }

    private static WorkflowStageDefinition Stage(
        MunicipalityProfile profile,
        long id,
        string code,
        int sequence,
        string actionCode,
        bool terminal = false,
        bool requiresRating = false,
        RatingScheme? scheme = null,
        bool separateFromSubmitter = false,
        bool optional = false,
        bool allowBypass = false) => new()
    {
        Id = id,
        Municipality = profile.Municipality,
        WorkflowDefinition = profile.Definition,
        Code = code,
        Name = code,
        Sequence = sequence,
        RequiredActionCode = actionCode,
        RequiredPermissionCode = actionCode,
        RequireDifferentActorFromSubmitter = separateFromSubmitter,
        IsOptional = optional,
        AllowBypass = allowBypass,
        IsTerminal = terminal,
        RequiresRating = requiresRating,
        RatingScheme = scheme
    };

    private static RatingScheme Scheme(
        MunicipalityProfile profile,
        long id,
        string code,
        params (long Id, decimal Value, string Label)[] values)
    {
        var scheme = new RatingScheme
        {
            Id = id,
            Municipality = profile.Municipality,
            Code = code,
            Name = $"{code} scheme"
        };
        foreach (var value in values)
        {
            scheme.Values.Add(new RatingSchemeValue
            {
                Id = value.Id,
                Municipality = profile.Municipality,
                RatingScheme = scheme,
                Value = value.Value,
                Label = value.Label,
                SortOrder = scheme.Values.Count + 1
            });
        }
        return scheme;
    }

    private sealed record MunicipalityProfile(
        Municipality Municipality,
        MunicipalityFinancialYear Year,
        ReportingPeriod Period,
        ApplicationUser Submitter,
        ApplicationUser Reviewer,
        WorkflowDefinition Definition,
        string SubmissionId);

    private sealed record AcceptanceMatrix(
        MunicipalityProfile Local,
        MunicipalityProfile Enterprise,
        MunicipalityProfile Hybrid);
}

using System.Reflection;
using FTCERP.Host.API.Controllers;
using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace FTCERP.Tests;

public sealed class LegacySubmissionScoreRetirementTests
{
    [Fact]
    public void Submission_write_contracts_do_not_accept_legacy_submitter_scores()
    {
        Assert.Null(typeof(SaveOpmsSubmissionRequest).GetProperty("SubmitterScore"));
        Assert.Null(typeof(SaveIpmsSubmissionRequest).GetProperty("SubmitterScore"));
        Assert.NotNull(typeof(SaveOpmsSubmissionRequest).GetProperty("PoeType"));
        Assert.NotNull(typeof(SaveIpmsSubmissionRequest).GetProperty("PoeType"));
    }

    [Fact]
    public void Standalone_score_routes_are_hidden_and_return_gone_without_writing()
    {
        using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("retired-score-user");
        var users = IdpTestFixture.CreateUserManagerMock(user).Object;
        var opms = new OpmsSubmissionsController(
            context, users, Mock.Of<IAccessControlService>(), Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(), Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>());
        var ipms = new IpmsSubmissionsController(
            context, users, Mock.Of<IAccessControlService>(), Mock.Of<IWorkflowGovernanceService>(),
            Mock.Of<IEvidenceBlobStorage>(), Mock.Of<ISubmissionValueService>(), Mock.Of<IConfigurableWorkflowService>(),
            Mock.Of<IReportingWindowService>(), Mock.Of<IEvidenceInspectionService>(), Mock.Of<IEvidenceMalwareScanner>(),
            Mock.Of<IPerformanceSuggestionService>());

        AssertGone(opms.Score(Guid.NewGuid().ToString()).Result);
        AssertGone(ipms.Score(Guid.NewGuid().ToString()).Result);
        Assert.Empty(context.SubmissionScores);

        AssertHiddenScoreAction(typeof(OpmsSubmissionsController));
        AssertHiddenScoreAction(typeof(IpmsSubmissionsController));
    }

    [Fact]
    public void Production_sources_cannot_reintroduce_legacy_score_writes_or_the_standalone_client_action()
    {
        var root = FindRepositoryRoot();
        var controllerSources = new[] { "OpmsSubmissionsController.cs", "IpmsSubmissionsController.cs" }
            .Select(file => File.ReadAllText(Path.Combine(root.FullName, "API", "Controllers", file)))
            .ToArray();
        var combinedControllers = string.Join(Environment.NewLine, controllerSources);
        var seedSource = File.ReadAllText(Path.Combine(root.FullName, "Infrastructure", "Persistence", "Seed", "DbInitializer.cs"));

        foreach (var forbiddenWrite in new[]
        {
            "_context.SubmissionScores.Add", "SubmitterScore = request.SubmitterScore",
            "VerifierScore = request.Score", "ApproverScore = request.Score",
            "PmsScore = request.Score", "AuditorScore = request.Score"
        })
        {
            Assert.DoesNotContain(forbiddenWrite, combinedControllers, StringComparison.Ordinal);
        }
        Assert.DoesNotContain("SubmitterScore = submissionSeed.SubmitterScore", seedSource, StringComparison.Ordinal);

        var workspace = File.ReadAllText(Path.Combine(root.FullName, "ClientApp", "src", "components", "submissions", "SubmissionWorkspace.tsx"));
        var api = File.ReadAllText(Path.Combine(root.FullName, "ClientApp", "src", "api", "api.ts"));
        Assert.DoesNotContain("Save Score", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("| 'score'", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("| 'score'", api, StringComparison.Ordinal);
    }

    private static void AssertGone(IActionResult? actionResult)
    {
        var result = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status410Gone, result.StatusCode);
        var response = Assert.IsAssignableFrom<IApiResponse>(result.Value);
        Assert.False(response.Success);
        Assert.Contains("configured workflow transition", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertHiddenScoreAction(Type controllerType)
    {
        var method = controllerType.GetMethod("Score", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.True(method.GetCustomAttribute<ApiExplorerSettingsAttribute>()?.IgnoreApi);
        Assert.Equal("{id:guid}/score", method.GetCustomAttribute<HttpPostAttribute>()?.Template);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "API", "Controllers"))) return directory;
        }

        throw new DirectoryNotFoundException("Could not locate API/Controllers from the test output directory.");
    }
}

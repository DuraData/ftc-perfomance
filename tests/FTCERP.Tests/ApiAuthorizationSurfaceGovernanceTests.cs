using System.Reflection;
using FTCERP.Host.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace FTCERP.Tests;

public sealed class ApiAuthorizationSurfaceGovernanceTests
{
    private static readonly HashSet<string> AllowedAnonymousActions = new(StringComparer.Ordinal)
    {
        ActionKey<AuthController>(nameof(AuthController.Login)),
        ActionKey<AuthController>(nameof(AuthController.ForgotPassword)),
        ActionKey<AuthController>(nameof(AuthController.ResetPassword)),
        ActionKey<AuthController>(nameof(AuthController.RefreshToken)),
        ActionKey<EnterpriseAuthController>(nameof(EnterpriseAuthController.DiscoverOptions)),
        ActionKey<EnterpriseAuthController>(nameof(EnterpriseAuthController.GetOptions)),
        ActionKey<EnterpriseAuthController>(nameof(EnterpriseAuthController.ChallengeProvider)),
        ActionKey<EnterpriseAuthController>(nameof(EnterpriseAuthController.Complete)),
        ActionKey<LegacyAuthController>(nameof(LegacyAuthController.Retired))
    };

    [Fact]
    public void Every_routed_API_action_is_explicitly_authorized_or_is_an_approved_anonymous_authentication_action()
    {
        var actions = DiscoverActions();
        actions.Should().HaveCountGreaterThan(250,
            "the inventory must cover the complete production controller surface rather than a small hand-picked subset");

        var failures = new List<string>();
        var actualAnonymousActions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (controller, action) in actions)
        {
            var key = ActionKey(controller, action);
            var anonymous = controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                || action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            var authorized = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()
                || action.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();

            if (anonymous)
            {
                actualAnonymousActions.Add(key);
                if (!AllowedAnonymousActions.Contains(key))
                    failures.Add($"Unexpected anonymous action: {key}");
            }
            else if (!authorized)
            {
                failures.Add($"Action has no explicit authorization boundary: {key}");
            }
        }

        failures.Should().BeEmpty(
            "every production API action must fail closed unless its anonymous authentication role is explicitly reviewed");
        actualAnonymousActions.Should().BeEquivalentTo(AllowedAnonymousActions,
            "removing or adding an anonymous route must be an intentional reviewable contract change");
    }

    [Fact]
    public void Approved_anonymous_authentication_actions_are_rate_limited_except_for_the_retired_tombstone()
    {
        var actions = DiscoverActions().ToLookup(item => ActionKey(item.Controller, item.Action));
        var failures = AllowedAnonymousActions
            .Where(key => key != ActionKey<LegacyAuthController>(nameof(LegacyAuthController.Retired)))
            .Where(key =>
            {
                var matches = actions[key].ToArray();
                return matches.Length != 1 || matches.Any(match =>
                    !match.Controller.IsDefined(typeof(EnableRateLimitingAttribute), inherit: true)
                    && !match.Action.IsDefined(typeof(EnableRateLimitingAttribute), inherit: true));
            })
            .Select(key => $"Anonymous authentication action is not rate limited: {key}")
            .ToArray();

        failures.Should().BeEmpty();
    }

    private static IReadOnlyList<(Type Controller, MethodInfo Action)> DiscoverActions() =>
        typeof(AuthController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(action => !action.IsSpecialName
                    && !action.IsDefined(typeof(NonActionAttribute), inherit: true)
                    && action.GetCustomAttributes(inherit: true).OfType<IRouteTemplateProvider>().Any())
                .Select(action => (Controller: controller, Action: action)))
            .OrderBy(item => item.Controller.FullName, StringComparer.Ordinal)
            .ThenBy(item => item.Action.Name, StringComparer.Ordinal)
            .ToArray();

    private static string ActionKey<TController>(string action) => ActionKey(typeof(TController), action);

    private static string ActionKey(Type controller, MethodInfo action) => ActionKey(controller, action.Name);

    private static string ActionKey(Type controller, string action) => $"{controller.FullName}.{action}";
}

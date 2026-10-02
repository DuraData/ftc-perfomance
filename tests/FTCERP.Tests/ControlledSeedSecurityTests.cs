using FTCERP.Host.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;

namespace FTCERP.Tests;

public sealed class ControlledSeedSecurityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void RequiredSeedSecret_RejectsMissingAndWeakFallbacks(string? value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Password"] = value
        }).Build();

        var action = () => DbInitializer.GetRequiredSeedSecret(configuration, "Admin:Password");

        action.Should().Throw<InvalidOperationException>().WithMessage("*local/deployment secret*");
    }

    [Fact]
    public void RequiredSeedSecret_ReturnsExplicitStrongConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Password"] = "local-only-Strong9!"
        }).Build();

        DbInitializer.GetRequiredSeedSecret(configuration, "Admin:Password").Should().Be("local-only-Strong9!");
    }
}

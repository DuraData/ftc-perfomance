namespace FTCERP.Tests;

public sealed class SubmissionBaseStateTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("IN_PROGRESS")]
    [InlineData("draft")]
    [InlineData("verify_rejected")]
    [InlineData("rejected")]
    public void Normalize_MapsEditableAndLegacyReturnedStatesToInProgress(string? state)
    {
        SubmissionBaseStates.Normalize(state).Should().Be(SubmissionBaseStates.InProgress);
    }

    [Theory]
    [InlineData("SUBMITTED")]
    [InlineData("verified")]
    [InlineData("approved")]
    [InlineData("audited")]
    public void Normalize_MapsPostSubmissionStagesToSubmitted(string state)
    {
        SubmissionBaseStates.Normalize(state).Should().Be(SubmissionBaseStates.Submitted);
    }
}

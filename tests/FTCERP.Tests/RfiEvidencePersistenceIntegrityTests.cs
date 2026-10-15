namespace FTCERP.Tests;

public sealed class RfiEvidencePersistenceIntegrityTests
{
    [Fact]
    public async Task Question_evidence_requires_exact_same_transaction_lifecycle_and_released_submission_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "valid");
        var rfi = NewRfi(fixture);
        var link = NewLink(fixture, rfi, fixture.File, fixture.Actor);
        context.AddRange(rfi, link);

        await context.SaveChangesAsync();

        (await context.PerformanceRfiEvidenceLinks.SingleAsync()).PublicId.Should().Be(link.PublicId);
    }

    [Fact]
    public async Task Orphan_or_later_question_evidence_is_rejected()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "orphan");
        context.PerformanceRfiEvidenceLinks.Add(new PerformanceRfiEvidence
        {
            MunicipalityId = fixture.Municipality.Id,
            PerformanceRfiId = 999999,
            PoeFileId = fixture.File.Id,
            Purpose = RfiEvidencePurpose.Question,
            LinkedByUserId = fixture.Actor.Id,
            CorrelationId = "trace-orphan"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RFI evidence creation requires an exact same-transaction lifecycle action, actor, tenant, submission and released governed POE record.*");
    }

    [Theory]
    [InlineData("cross-submission")]
    [InlineData("quarantined")]
    [InlineData("wrong-actor")]
    public async Task Cross_submission_unreleased_or_wrong_actor_evidence_is_rejected(string defect)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, defect);

        var rfi = NewRfi(fixture);
        var actor = defect == "wrong-actor" ? fixture.OtherActor : fixture.Actor;
        context.AddRange(rfi, NewLink(fixture, rfi, fixture.File, actor));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RFI evidence creation requires an exact same-transaction lifecycle action, actor, tenant, submission and released governed POE record.*");
    }

    [Fact]
    public async Task Response_evidence_requires_the_exact_response_actor_and_lifecycle_write()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var fixture = await SeedAsync(context, "response");
        var rfi = NewRfi(fixture);
        context.PerformanceRfis.Add(rfi);
        await context.SaveChangesAsync();

        var respondedAt = DateTime.UtcNow;
        rfi.Response = "The requested clarification is attached.";
        rfi.RespondedByUserId = fixture.Actor.Id;
        rfi.RespondedAt = respondedAt;
        var link = NewLink(fixture, rfi, fixture.File, fixture.Actor, RfiEvidencePurpose.Response);
        link.LinkedAt = respondedAt;
        context.PerformanceRfiEvidenceLinks.Add(link);

        await context.SaveChangesAsync();

        (await context.PerformanceRfiEvidenceLinks.SingleAsync()).Purpose.Should().Be(RfiEvidencePurpose.Response);
    }

    private static PerformanceRfi NewRfi(Fixture fixture) => new()
    {
        MunicipalityId = fixture.Municipality.Id,
        SubmissionWorkflowInstanceId = fixture.Instance.Id,
        SubmissionWorkflowInstance = fixture.Instance,
        Question = "Provide the signed source schedule.",
        RaisedByUserId = fixture.Actor.Id,
        RaisedAt = DateTime.UtcNow,
        ResponseDueAt = DateTime.UtcNow.AddDays(5)
    };

    private static PerformanceRfiEvidence NewLink(
        Fixture fixture,
        PerformanceRfi rfi,
        PoeFile file,
        ApplicationUser actor,
        RfiEvidencePurpose purpose = RfiEvidencePurpose.Question) => new()
    {
        MunicipalityId = fixture.Municipality.Id,
        PerformanceRfi = rfi,
        PoeFileId = file.Id,
        PoeFile = file,
        Purpose = purpose,
        LinkedByUserId = actor.Id,
        LinkedByUser = actor,
        LinkedAt = DateTime.UtcNow,
        CorrelationId = $"trace-{purpose.ToString().ToLowerInvariant()}"
    };

    private static async Task<Fixture> SeedAsync(ApplicationDbContext context, string suffix)
    {
        var municipality = new Municipality { Code = $"RFI-{suffix}", Name = $"RFI {suffix}" };
        var actor = new ApplicationUser
        {
            Id = $"actor-{suffix}", UserName = $"actor-{suffix}@test.local", NormalizedUserName = $"ACTOR-{suffix.ToUpperInvariant()}@TEST.LOCAL",
            Email = $"actor-{suffix}@test.local", NormalizedEmail = $"ACTOR-{suffix.ToUpperInvariant()}@TEST.LOCAL", Municipality = municipality
        };
        var otherActor = new ApplicationUser
        {
            Id = $"other-{suffix}", UserName = $"other-{suffix}@test.local", NormalizedUserName = $"OTHER-{suffix.ToUpperInvariant()}@TEST.LOCAL",
            Email = $"other-{suffix}@test.local", NormalizedEmail = $"OTHER-{suffix.ToUpperInvariant()}@TEST.LOCAL", Municipality = municipality
        };
        var year = new FinancialYear
        {
            Code = $"30{suffix.Length:D2}/31{suffix.Length:D2}", Name = $"RFI year {suffix}",
            StartDate = new DateTime(2030, 7, 1), EndDate = new DateTime(2031, 6, 30)
        };
        var municipalYear = new MunicipalityFinancialYear
        {
            Municipality = municipality, FinancialYear = year, EffectiveFrom = year.StartDate
        };
        var definition = new WorkflowDefinition
        {
            Municipality = municipality, MunicipalityFinancialYear = municipalYear,
            SubmissionKind = SubmissionKind.Opms, Code = $"RFI-{suffix}", Name = "RFI workflow", EffectiveFrom = year.StartDate
        };
        var instance = new SubmissionWorkflowInstance
        {
            Municipality = municipality, WorkflowDefinition = definition,
            SubmissionKind = SubmissionKind.Opms, SubmissionId = $"submission-{suffix}"
        };
        var blob = new EvidenceBlob
        {
            Municipality = municipality, StorageKey = $"rfi/{suffix}", ContentType = "application/pdf", SizeInBytes = 128,
            Sha256 = new string('a', 64), SignatureVerified = true, ScanStatus = "Clean"
        };
        var file = new PoeFile
        {
            Id = $"poe-{suffix}", Municipality = municipality, SubmissionKind = SubmissionKind.Opms,
            SubmissionId = suffix == "cross-submission" ? "another-submission" : instance.SubmissionId,
            FileName = "source.pdf", Blob = blob,
            UploadedByUser = actor, IsActive = true
        };
        blob.IsQuarantined = suffix == "quarantined";
        context.AddRange(municipality, actor, otherActor, year, municipalYear, definition, instance, blob, file);
        await context.SaveChangesAsync();
        return new(municipality, actor, otherActor, instance, file, blob);
    }

    private sealed record Fixture(
        Municipality Municipality,
        ApplicationUser Actor,
        ApplicationUser OtherActor,
        SubmissionWorkflowInstance Instance,
        PoeFile File,
        EvidenceBlob Blob);
}

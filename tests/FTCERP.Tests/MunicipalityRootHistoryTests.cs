namespace FTCERP.Tests;

public sealed class MunicipalityRootHistoryTests
{
    [Fact]
    public async Task Municipality_tenant_root_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();

        context.Municipalities.Remove(municipality);

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality tenant-root history cannot be hard deleted*");
    }

    [Fact]
    public async Task Municipality_identity_and_profile_cannot_be_rewritten_directly()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();

        municipality.Name = "Rewritten Municipality";

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality identity, code, name, activation and effective-date evidence are immutable*");
    }

    [Fact]
    public async Task Municipality_authentication_mode_requires_exact_version_and_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();

        municipality.AuthenticationMode = AuthenticationMode.Hybrid;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality authentication mode may change only beside its exact reason-audited configuration version*");
    }

    [Fact]
    public async Task Municipality_authentication_mode_rejects_forged_configuration_audit_snapshot()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var actor = IdpTestFixture.CreateUser("municipality-auth-forged-actor");
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();

        municipality.AuthenticationMode = AuthenticationMode.Hybrid;
        var configuration = new AuthenticationConfiguration
        {
            MunicipalityId = municipality.Id,
            Mode = AuthenticationMode.Hybrid,
            ProviderRegistrationCode = "ENTRA",
            DisplayName = "Forged audit attempt",
            EffectiveFrom = DateTime.UtcNow,
            CreatedByUserId = actor.Id
        };
        configuration.Policy = new AuthenticationPolicy
        {
            MunicipalityId = municipality.Id,
            AuthenticationConfiguration = configuration,
            ModifiedByUserId = actor.Id
        };
        context.AuthenticationConfigurations.Add(configuration);
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "AuthenticationConfiguration",
            EntityId = configuration.PublicId.ToString(),
            Action = "AuthenticationConfigurationCreated",
            NewValue = "{\"mode\":0}",
            ChangedBy = actor.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Fabricated mode evidence must not authorize a tenant-root rewrite."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality authentication mode may change only beside its exact reason-audited configuration version*");
    }

    [Fact]
    public async Task Municipality_tid_projection_requires_coherent_state_and_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        context.Municipalities.Add(municipality);
        await context.SaveChangesAsync();

        municipality.TidAllKpisRequired = true;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality TID settings require a coherent state and exact same-transaction reasoned audit evidence*");
    }

    [Fact]
    public async Task Municipality_tid_projection_accepts_exact_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var actor = IdpTestFixture.CreateUser("municipality-tid-actor");
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();

        municipality.TidEnabled = true;
        municipality.TidAllKpisRequired = true;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "TidConfiguration",
            EntityId = municipality.PublicId.ToString(),
            Action = "Update",
            OldValue = "{\"tidEnabled\":false,\"tidAllKpisRequired\":false}",
            NewValue = "{\"tidEnabled\":true,\"tidAllKpisRequired\":true}",
            ChangedBy = actor.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Council approved mandatory technical indicator descriptions."
        });

        await context.SaveChangesAsync();

        var stored = await context.Municipalities.AsNoTracking().SingleAsync(item => item.Id == municipality.Id);
        stored.TidEnabled.Should().BeTrue();
        stored.TidAllKpisRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Municipality_tid_projection_rejects_forged_audit_snapshots()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var actor = IdpTestFixture.CreateUser("municipality-tid-forged-actor");
        context.AddRange(municipality, actor);
        await context.SaveChangesAsync();

        municipality.TidEnabled = true;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "TidConfiguration",
            EntityId = municipality.PublicId.ToString(),
            Action = "Update",
            OldValue = "{\"tidEnabled\":true,\"tidAllKpisRequired\":false}",
            NewValue = "{\"tidEnabled\":false,\"tidAllKpisRequired\":false}",
            ChangedBy = actor.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Fabricated snapshots must not authorize a tenant-root rewrite."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality TID settings require a coherent state and exact same-transaction reasoned audit evidence*");
    }

    [Fact]
    public async Task Municipality_tid_projection_rejects_exact_audit_from_a_different_actor()
    {
        const long municipalityId = 9150;
        var municipality = Municipality();
        municipality.Id = municipalityId;
        var actor = IdpTestFixture.CreateUser("municipality-tid-context-actor");
        var forgedActor = IdpTestFixture.CreateUser("municipality-tid-different-actor");
        actor.MunicipalityId = municipalityId;
        forgedActor.MunicipalityId = municipalityId;
        await using var context = IdpTestFixture.CreateRelationalContext(
            IdpTestFixture.Tenant(municipalityId, actor.Id));
        context.AddRange(municipality, actor, forgedActor);
        await context.SaveChangesAsync();

        municipality.TidEnabled = true;
        municipality.TidAllKpisRequired = true;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "TidConfiguration",
            EntityId = municipality.PublicId.ToString(),
            Action = "Update",
            OldValue = "{\"tidEnabled\":false,\"tidAllKpisRequired\":false}",
            NewValue = "{\"tidEnabled\":true,\"tidAllKpisRequired\":true}",
            ChangedBy = forgedActor.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Exact snapshots must not authorize another actor."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Municipality TID settings require a coherent state and exact same-transaction reasoned audit evidence*");
    }

    private static Municipality Municipality() => new()
    {
        Code = $"ROOT-{Guid.NewGuid():N}"[..20],
        Name = "Governed Municipality",
        EffectiveFrom = DateTime.UtcNow.AddYears(-1)
    };
}

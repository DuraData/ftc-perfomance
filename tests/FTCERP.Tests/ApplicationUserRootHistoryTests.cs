namespace FTCERP.Tests;

public sealed class ApplicationUserRootHistoryTests
{
    [Fact]
    public async Task Application_user_security_root_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("delete-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        context.Users.Remove(user);

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user security-root history cannot be hard deleted*");
    }

    [Fact]
    public async Task Application_user_login_identity_and_tenant_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("identity-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.Email = "rewritten@example.test";

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user public identity, tenant, login identity, organisation and creation evidence are immutable*");
    }

    [Fact]
    public async Task Application_user_profile_requires_exact_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("profile-direct-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.FirstName = "Direct rewrite";
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = user.Id;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user profile, lifecycle and administration changes require one exact same-transaction reasoned audit*");
    }

    [Fact]
    public async Task Application_user_profile_rejects_forged_audit_snapshot()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var actor = User("profile-forged-actor", municipality);
        var user = User("profile-forged-user", municipality);
        context.AddRange(municipality, actor, user);
        await context.SaveChangesAsync();

        var before = user.FirstName;
        user.FirstName = "Governed value";
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actor.Id;
        context.AuditTrails.Add(Audit(user, actor, "Update", new { FirstName = before }, new { FirstName = "Different value" }));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user profile, lifecycle and administration changes require one exact same-transaction reasoned audit*");
    }

    [Fact]
    public async Task Application_user_profile_accepts_exact_reasoned_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var actor = User("profile-actor", municipality);
        var user = User("profile-user", municipality);
        context.AddRange(municipality, actor, user);
        await context.SaveChangesAsync();

        var before = new { user.FirstName, user.LastName, user.PhoneNumber, user.IsActive };
        user.FirstName = "Governed";
        user.LastName = "Profile";
        user.PhoneNumber = "0123456789";
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actor.Id;
        context.AuditTrails.Add(Audit(user, actor, "Update", before,
            new { user.FirstName, user.LastName, user.PhoneNumber, user.IsActive }));

        await context.SaveChangesAsync();

        var stored = await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        stored.FirstName.Should().Be("Governed");
        stored.LastName.Should().Be("Profile");
        stored.PhoneNumber.Should().Be("0123456789");
        stored.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Application_user_password_remediation_requires_exact_authentication_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("password-direct-user", municipality);
        user.MustChangePassword = true;
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.MustChangePassword = false;

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user password-remediation state may clear only beside its exact authentication audit*");
    }

    [Fact]
    public async Task Application_user_password_remediation_accepts_exact_authentication_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("password-governed-user", municipality);
        user.MustChangePassword = true;
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.MustChangePassword = false;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = "PasswordChange",
            NewValue = "{\"mustChangePassword\":false}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "User completed a governed password change."
        });

        await context.SaveChangesAsync();
        (await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Application_user_mfa_state_rejects_missing_or_forged_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("mfa-direct-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.TwoFactorEnabled = true;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = "MfaEnable",
            NewValue = "{\"twoFactorEnabled\":false}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Forged MFA state must not authorize a security-root rewrite."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user MFA state may change only beside its exact authentication audit*");
    }

    [Fact]
    public async Task Application_user_last_login_may_only_advance()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("login-evidence-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        user.LastLoginAt = user.LastLoginAt.Value.AddMinutes(-1);

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user last-login evidence may advance only monotonically*");
    }

    private static Municipality Municipality() => new()
    {
        Code = $"USER-{Guid.NewGuid():N}"[..20],
        Name = "Governed User Municipality",
        EffectiveFrom = DateTime.UtcNow.AddYears(-1)
    };

    private static ApplicationUser User(string id, Municipality municipality) => new()
    {
        Id = id,
        UserName = $"{id}@example.test",
        NormalizedUserName = $"{id}@example.test".ToUpperInvariant(),
        Email = $"{id}@example.test",
        NormalizedEmail = $"{id}@example.test".ToUpperInvariant(),
        EmailConfirmed = true,
        FirstName = "Original",
        LastName = "User",
        Municipality = municipality,
        IsActive = true,
        SecurityStamp = $"stamp-{id}"
    };

    private static AuditTrail Audit(ApplicationUser user, ApplicationUser actor, string action, object oldValue, object newValue) => new()
    {
        MunicipalityId = user.MunicipalityId,
        EntityName = nameof(ApplicationUser),
        EntityId = user.PublicId.ToString(),
        Action = action,
        OldValue = System.Text.Json.JsonSerializer.Serialize(oldValue),
        NewValue = System.Text.Json.JsonSerializer.Serialize(newValue),
        ChangedBy = actor.Id,
        ChangedAt = DateTime.UtcNow,
        Reason = "Authorized user administration change."
    };
}

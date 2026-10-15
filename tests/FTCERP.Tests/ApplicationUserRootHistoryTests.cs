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
    public async Task Application_user_profile_rejects_an_exact_audit_from_a_different_context_actor()
    {
        var municipality = Municipality();
        var contextActor = User("profile-context-actor", municipality);
        await using var context = IdpTestFixture.CreateRelationalContext(
            IdpTestFixture.Tenant(null, contextActor.Id, true));
        var forgedActor = User("profile-different-actor", municipality);
        var user = User("profile-context-target", municipality);
        context.AddRange(municipality, contextActor, forgedActor, user);
        await context.SaveChangesAsync();

        var before = user.FirstName;
        user.FirstName = "Governed value";
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = forgedActor.Id;
        context.AuditTrails.Add(Audit(user, forgedActor, "Update", new { FirstName = before }, new { user.FirstName }));

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user profile, lifecycle and administration changes require one exact same-transaction reasoned audit*");
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
            OldValue = "{\"mustChangePassword\":true}",
            NewValue = "{\"mustChangePassword\":false}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "User completed a governed password change."
        });

        await context.SaveChangesAsync();
        (await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Application_user_password_remediation_rejects_exact_audit_from_a_different_context_actor()
    {
        var municipality = Municipality();
        var user = User("password-context-user", municipality);
        await using var context = IdpTestFixture.CreateRelationalContext(
            IdpTestFixture.Tenant(null, "different-authenticated-user", true));
        context.AddRange(municipality, user);
        user.MustChangePassword = true;
        await context.SaveChangesAsync();

        user.MustChangePassword = false;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = "PasswordChange",
            OldValue = "{\"mustChangePassword\":true}",
            NewValue = "{\"mustChangePassword\":false}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Exact state evidence attributed outside the authenticated context."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user password-remediation state may clear only beside its exact authentication audit*");
    }

    [Fact]
    public async Task Application_user_password_remediation_rejects_future_dated_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("password-future-audit-user", municipality);
        user.MustChangePassword = true;
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.MustChangePassword = false;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = "PasswordResetCompleted",
            OldValue = "{\"mustChangePassword\":true}",
            NewValue = "{\"mustChangePassword\":false}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow.AddMinutes(10),
            Reason = "Future-dated authentication evidence must not authorize a state change."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user password-remediation state may clear only beside its exact authentication audit*");
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
            OldValue = "{\"twoFactorEnabled\":false}",
            NewValue = "{\"twoFactorEnabled\":false}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Forged MFA state must not authorize a security-root rewrite."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user MFA state may change only beside its exact authentication audit*");
    }

    [Fact]
    public async Task Application_user_mfa_state_accepts_exact_actor_bound_audit()
    {
        var municipality = Municipality();
        var user = User("mfa-governed-user", municipality);
        await using var context = IdpTestFixture.CreateRelationalContext(
            IdpTestFixture.Tenant(null, user.Id, true));
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.TwoFactorEnabled = true;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = "MfaEnable",
            OldValue = "{\"twoFactorEnabled\":false}",
            NewValue = "{\"twoFactorEnabled\":true}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "User enabled authenticator MFA."
        });

        await context.SaveChangesAsync();
        (await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Application_user_mfa_state_rejects_exact_audit_from_a_different_context_actor()
    {
        var municipality = Municipality();
        var user = User("mfa-context-user", municipality);
        await using var context = IdpTestFixture.CreateRelationalContext(
            IdpTestFixture.Tenant(null, "different-authenticated-user", true));
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        user.TwoFactorEnabled = true;
        context.AuditTrails.Add(new AuditTrail
        {
            MunicipalityId = municipality.Id,
            EntityName = "UserAuthentication",
            EntityId = user.Id,
            Action = "MfaEnable",
            OldValue = "{\"twoFactorEnabled\":false}",
            NewValue = "{\"twoFactorEnabled\":true}",
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = "Exact state evidence attributed outside the authenticated context."
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user MFA state may change only beside its exact authentication audit*");
    }

    [Fact]
    public async Task Application_user_last_login_may_only_advance_with_exact_authentication_ledgers()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("login-evidence-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var authenticatedAt = DateTime.UtcNow;
        user.LastLoginAt = authenticatedAt;
        context.AuthenticationEvents.Add(new AuthenticationEvent
        {
            MunicipalityId = municipality.Id,
            UserId = user.Id,
            ProviderCode = "LOCAL",
            EventType = "LocalSignIn",
            Success = true,
            OccurredAt = authenticatedAt,
            CorrelationId = "login-evidence"
        });
        context.LoginAuditLogs.Add(new LoginAuditLog
        {
            MunicipalityId = municipality.Id,
            UserId = user.Id,
            Email = user.Email!,
            Success = true,
            LoggedAt = authenticatedAt
        });
        await context.SaveChangesAsync();
        user.LastLoginAt = user.LastLoginAt.Value.AddMinutes(-1);

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application-user last-login evidence may advance only with its exact append-only authentication ledgers*");
    }

    [Fact]
    public async Task Application_user_last_login_rejects_future_exact_local_ledgers_atomically()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var municipality = Municipality();
        var user = User("future-login-evidence-user", municipality);
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();

        var future = DateTime.UtcNow.AddHours(1);
        user.LastLoginAt = future;
        context.AuthenticationEvents.Add(new AuthenticationEvent
        {
            MunicipalityId = municipality.Id,
            UserId = user.Id,
            ProviderCode = "LOCAL",
            EventType = "LocalSignIn",
            Success = true,
            OccurredAt = future,
            CorrelationId = "future-login-evidence"
        });
        context.LoginAuditLogs.Add(new LoginAuditLog
        {
            MunicipalityId = municipality.Id,
            UserId = user.Id,
            Email = user.Email!,
            Success = true,
            LoggedAt = future
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>();
        context.ChangeTracker.Clear();
        (await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).LastLoginAt.Should().BeNull();
        (await context.AuthenticationEvents.AsNoTracking().CountAsync()).Should().Be(0);
        (await context.LoginAuditLogs.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Authentication_ledgers_reject_failure_rows_without_failure_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        context.AuthenticationEvents.Add(new AuthenticationEvent
        {
            ProviderCode = "LOCAL",
            EventType = "LocalSignIn",
            Success = false,
            CorrelationId = "missing-authentication-failure"
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Authentication events require complete, chronologically valid success or failure evidence*");
        context.ChangeTracker.Clear();
        context.LoginAuditLogs.Add(new LoginAuditLog
        {
            Email = "failed@example.test",
            Success = false
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Login audit records require complete, chronologically valid success or failure evidence*");
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
        ChangedAt = user.UpdatedAt ?? DateTime.UtcNow,
        Reason = "Authorized user administration change."
    };
}

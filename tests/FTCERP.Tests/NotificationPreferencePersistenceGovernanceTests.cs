namespace FTCERP.Tests;

public sealed class NotificationPreferencePersistenceGovernanceTests
{
    [Fact]
    public async Task Preference_create_requires_exact_same_transaction_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedOwnerAsync(context);
        context.NotificationPreferences.Add(new NotificationPreference
        {
            MunicipalityId = seeded.Municipality.Id, UserId = seeded.User.Id
        });

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("NotificationPreference Create requires one exact same-transaction reasoned before/after audit record.");
    }

    [Fact]
    public async Task Preference_update_rejects_missing_or_fabricated_snapshot_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedPreferenceAsync(context);
        seeded.Preference.SmsEnabled = true;
        seeded.Preference.UpdatedAt = seeded.Preference.UpdatedAt.AddMinutes(1);
        context.AuditTrails.Add(Audit(seeded, "Update", Snapshot(seeded.Preference, smsEnabled: false), Snapshot(seeded.Preference, smsEnabled: false)));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("NotificationPreference Update requires one exact same-transaction reasoned before/after audit record.");
    }

    [Fact]
    public async Task Preference_owner_and_identity_are_immutable_even_with_audit()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedPreferenceAsync(context);
        seeded.Preference.UserId = "another-user";
        seeded.Preference.UpdatedAt = seeded.Preference.UpdatedAt.AddMinutes(1);
        context.AuditTrails.Add(Audit(seeded, "Update", Snapshot(seeded.Preference), Snapshot(seeded.Preference)));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Notification-preference public identity, tenant and owning user are immutable.");
    }

    [Fact]
    public async Task Preference_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedPreferenceAsync(context);
        context.NotificationPreferences.Remove(seeded.Preference);

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Notification-preference history cannot be hard deleted*");
    }

    [Fact]
    public async Task Exact_atomic_preference_audit_allows_update_and_advances_rowversion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var seeded = await SeedPreferenceAsync(context);
        var oldVersion = Convert.ToBase64String(seeded.Preference.RowVersion);
        var before = Snapshot(seeded.Preference);
        seeded.Preference.SmsEnabled = true;
        seeded.Preference.WeeklySummaryEnabled = true;
        seeded.Preference.UpdatedAt = seeded.Preference.UpdatedAt.AddMinutes(1);
        context.AuditTrails.Add(Audit(seeded, "Update", before, Snapshot(seeded.Preference)));

        await context.SaveChangesAsync();

        Convert.ToBase64String(seeded.Preference.RowVersion).Should().NotBe(oldVersion);
        (await context.AuditTrails.CountAsync(item => item.EntityName == nameof(NotificationPreference))).Should().Be(2);
    }

    private static async Task<Seeded> SeedOwnerAsync(ApplicationDbContext context)
    {
        var municipality = new Municipality { Code = "PREF-GOV", Name = "Preference Governance" };
        var user = IdpTestFixture.CreateUser("preference-governance-user");
        user.Municipality = municipality;
        context.AddRange(municipality, user);
        await context.SaveChangesAsync();
        return new Seeded(municipality, user, null!);
    }

    private static async Task<Seeded> SeedPreferenceAsync(ApplicationDbContext context)
    {
        var seeded = await SeedOwnerAsync(context);
        var preference = new NotificationPreference
        {
            MunicipalityId = seeded.Municipality.Id, UserId = seeded.User.Id,
            EmailEnabled = true, SmsEnabled = false, DailyDigestEnabled = true,
            WeeklySummaryEnabled = false, UpdatedAt = new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc)
        };
        context.NotificationPreferences.Add(preference);
        var complete = seeded with { Preference = preference };
        context.AuditTrails.Add(Audit(complete, "Create", null, Snapshot(preference)));
        await context.SaveChangesAsync();
        return complete;
    }

    private static AuditTrail Audit(Seeded seeded, string action, object? before, object after) => new()
    {
        MunicipalityId = seeded.Municipality.Id,
        EntityName = nameof(NotificationPreference),
        EntityId = seeded.Preference.PublicId.ToString(),
        Action = action,
        OldValue = before == null ? null : System.Text.Json.JsonSerializer.Serialize(before),
        NewValue = System.Text.Json.JsonSerializer.Serialize(after),
        ChangedBy = seeded.User.Id,
        Reason = "User notification preference update; mandatory operational notifications remain enabled."
    };

    private static object Snapshot(NotificationPreference preference, bool? smsEnabled = null) => new
    {
        preference.EmailEnabled,
        SmsEnabled = smsEnabled ?? preference.SmsEnabled,
        preference.DailyDigestEnabled,
        preference.WeeklySummaryEnabled
    };

    private sealed record Seeded(Municipality Municipality, ApplicationUser User, NotificationPreference Preference);
}

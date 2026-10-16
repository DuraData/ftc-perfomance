using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FTCERP.Tests;

public sealed class NotificationPersistenceGovernanceTests
{
    [Fact]
    public async Task Sqlite_upgrade_preserves_notifications_and_backfills_legacy_read_time_without_inventing_an_actor()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-notification-history-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var user = await SeedUserAsync(context);
            var read = UnreadNotification(user);
            read.Id = "legacy-read";
            read.IsRead = true;
            read.ReadAt = read.CreatedAt;
            read.ReadByUserId = user.Id;
            var unread = UnreadNotification(user);
            unread.Id = "legacy-unread";
            context.Notifications.AddRange(read, unread);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "Notifications_Legacy" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY,
                    "CreatedAt" TEXT NOT NULL,
                    "EntityId" TEXT NULL,
                    "EntityName" TEXT NULL,
                    "IsRead" INTEGER NOT NULL,
                    "Message" TEXT NOT NULL,
                    "MunicipalityId" INTEGER NULL,
                    "PublicId" TEXT NOT NULL,
                    "Title" TEXT NOT NULL,
                    "Type" INTEGER NOT NULL,
                    "UserId" TEXT NOT NULL,
                    CONSTRAINT "FK_Notifications_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Notifications_Municipalities_MunicipalityId" FOREIGN KEY ("MunicipalityId") REFERENCES "Municipalities" ("Id") ON DELETE RESTRICT
                );
                INSERT INTO "Notifications_Legacy" ("Id", "CreatedAt", "EntityId", "EntityName", "IsRead", "Message", "MunicipalityId", "PublicId", "Title", "Type", "UserId")
                SELECT "Id", "CreatedAt", "EntityId", "EntityName", "IsRead", "Message", "MunicipalityId", "PublicId", "Title", "Type", "UserId" FROM "Notifications";
                DROP TABLE "Notifications";
                ALTER TABLE "Notifications_Legacy" RENAME TO "Notifications";
                CREATE INDEX "IX_Notifications_MunicipalityId" ON "Notifications" ("MunicipalityId");
                CREATE UNIQUE INDEX "IX_Notifications_PublicId" ON "Notifications" ("PublicId");
                CREATE INDEX "IX_Notifications_UserId" ON "Notifications" ("UserId");
                """);
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            const string latestMigration = "20261013131200_V39GovernedNotificationReadLifecycle";
            foreach (var migration in context.Database.GetMigrations().TakeWhile(item => item != latestMigration))
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migration, "10.0.0");

            await context.GetService<IMigrator>().MigrateAsync(latestMigration);

            var preservedRead = await context.Notifications.AsNoTracking().SingleAsync(item => item.Id == "legacy-read");
            preservedRead.IsRead.Should().BeTrue();
            preservedRead.ReadAt.Should().Be(preservedRead.CreatedAt);
            preservedRead.ReadByUserId.Should().BeNull("migration evidence must not invent a legacy reader");
            preservedRead.RowVersion.Should().HaveCount(16);
            var preservedUnread = await context.Notifications.AsNoTracking().SingleAsync(item => item.Id == "legacy-unread");
            preservedUnread.IsRead.Should().BeFalse();
            preservedUnread.ReadAt.Should().BeNull();
            preservedUnread.ReadByUserId.Should().BeNull();
            preservedUnread.RowVersion.Should().HaveCount(16);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Notification_creation_requires_a_coherent_read_lifecycle()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = await SeedUserAsync(context);
        var notification = UnreadNotification(user);
        notification.IsRead = true;
        context.Notifications.Add(notification);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A notification requires immutable recipient, content and creation provenance plus a coherent unread or actor-stamped read lifecycle.");
    }

    [Fact]
    public async Task Notification_identity_and_content_are_immutable()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var notification = await SeedNotificationAsync(context);
        notification.Message = "Rewritten message";

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Notification identity, tenant, recipient, content, source and creation evidence are immutable.");
    }

    [Fact]
    public async Task Notification_history_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var notification = await SeedNotificationAsync(context);
        context.Notifications.Remove(notification);

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Notification history cannot be hard deleted.");
    }

    [Fact]
    public async Task Exact_read_transition_records_actor_and_time_and_advances_rowversion()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var notification = await SeedNotificationAsync(context);
        var originalVersion = notification.RowVersion.ToArray();
        var readAt = notification.CreatedAt.AddMinutes(1);
        notification.IsRead = true;
        notification.ReadAt = readAt;
        notification.ReadByUserId = notification.UserId;

        await context.SaveChangesAsync();

        notification.ReadAt.Should().Be(readAt);
        notification.ReadByUserId.Should().Be(notification.UserId);
        notification.RowVersion.Should().NotEqual(originalVersion);
    }

    [Fact]
    public async Task Read_transition_is_irreversible_and_attribution_cannot_be_rewritten()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var notification = await SeedNotificationAsync(context);
        notification.IsRead = true;
        notification.ReadAt = notification.CreatedAt.AddMinutes(1);
        notification.ReadByUserId = notification.UserId;
        await context.SaveChangesAsync();

        notification.ReadAt = notification.ReadAt.Value.AddMinutes(1);
        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A notification may transition exactly once from unread to read with an ordered timestamp and actor.");
    }

    [Fact]
    public async Task Concurrent_read_transition_rejects_the_stale_writer()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"opms-notification-concurrency-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            string notificationId;
            await using (var seedContext = new ApplicationDbContext(options))
            {
                await seedContext.Database.EnsureCreatedAsync();
                var notification = await SeedNotificationAsync(seedContext);
                notificationId = notification.Id;
            }

            await using var winnerContext = new ApplicationDbContext(options);
            await using var staleContext = new ApplicationDbContext(options);
            var winner = await winnerContext.Notifications.SingleAsync(item => item.Id == notificationId);
            var stale = await staleContext.Notifications.SingleAsync(item => item.Id == notificationId);
            winner.IsRead = true;
            winner.ReadAt = winner.CreatedAt.AddMinutes(1);
            winner.ReadByUserId = winner.UserId;
            stale.IsRead = true;
            stale.ReadAt = stale.CreatedAt.AddMinutes(2);
            stale.ReadByUserId = stale.UserId;

            await winnerContext.SaveChangesAsync();
            await FluentActions.Invoking(() => staleContext.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext context)
    {
        var user = IdpTestFixture.CreateUser($"notification-governance-{Guid.NewGuid():N}");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Notification> SeedNotificationAsync(ApplicationDbContext context)
    {
        var user = await SeedUserAsync(context);
        var notification = UnreadNotification(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification;
    }

    private static Notification UnreadNotification(ApplicationUser user) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        UserId = user.Id,
        Type = NotificationType.Submission,
        Title = "Submission requires attention",
        Message = "A governed performance submission requires attention.",
        CreatedAt = new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc)
    };
}

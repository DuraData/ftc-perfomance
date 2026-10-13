using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedNotificationReadLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                UpSqlite(migrationBuilder);
                return;
            }

            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Notifications",
                type: sqlServer ? "datetime2" : "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadByUserId",
                table: "Notifications",
                type: sqlServer ? "nvarchar(450)" : "TEXT",
                nullable: true);

            if (sqlServer)
            {
                migrationBuilder.AddColumn<byte[]>(
                    name: "RowVersion",
                    table: "Notifications",
                    type: "rowversion",
                    rowVersion: true,
                    nullable: false);
            }
            else
            {
                migrationBuilder.AddColumn<byte[]>(
                    name: "RowVersion",
                    table: "Notifications",
                    type: "BLOB",
                    nullable: false,
                    defaultValue: new byte[0]);
            }

            migrationBuilder.Sql("""
                UPDATE Notifications
                SET ReadAt = CreatedAt
                WHERE IsRead = 1 AND ReadAt IS NULL;
                """);

            if (!sqlServer)
            {
                migrationBuilder.Sql("""
                    UPDATE Notifications
                    SET RowVersion = randomblob(16)
                    WHERE length(RowVersion) = 0;
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReadByUserId",
                table: "Notifications",
                column: "ReadByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_ReadLifecycle",
                table: "Notifications",
                sql: "([IsRead] = 0 AND [ReadAt] IS NULL AND [ReadByUserId] IS NULL) OR ([IsRead] = 1 AND [ReadAt] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_ReadByUserId",
                table: "Notifications",
                column: "ReadByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                DownSqlite(migrationBuilder);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_ReadByUserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReadByUserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_ReadLifecycle",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadByUserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void UpSqlite(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA defer_foreign_keys = ON;");
            migrationBuilder.Sql("""
                CREATE TABLE "Notifications_Governed" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY,
                    "CreatedAt" TEXT NOT NULL,
                    "EntityId" TEXT NULL,
                    "EntityName" TEXT NULL,
                    "IsRead" INTEGER NOT NULL,
                    "Message" TEXT NOT NULL,
                    "MunicipalityId" INTEGER NULL,
                    "PublicId" TEXT NOT NULL,
                    "ReadAt" TEXT NULL,
                    "ReadByUserId" TEXT NULL,
                    "RowVersion" BLOB NOT NULL,
                    "Title" TEXT NOT NULL,
                    "Type" INTEGER NOT NULL,
                    "UserId" TEXT NOT NULL,
                    CONSTRAINT "CK_Notifications_ReadLifecycle" CHECK (("IsRead" = 0 AND "ReadAt" IS NULL AND "ReadByUserId" IS NULL) OR ("IsRead" = 1 AND "ReadAt" IS NOT NULL)),
                    CONSTRAINT "FK_Notifications_AspNetUsers_ReadByUserId" FOREIGN KEY ("ReadByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Notifications_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Notifications_Municipalities_MunicipalityId" FOREIGN KEY ("MunicipalityId") REFERENCES "Municipalities" ("Id") ON DELETE RESTRICT
                );
                INSERT INTO "Notifications_Governed"
                    ("Id", "CreatedAt", "EntityId", "EntityName", "IsRead", "Message", "MunicipalityId", "PublicId",
                     "ReadAt", "ReadByUserId", "RowVersion", "Title", "Type", "UserId")
                SELECT "Id", "CreatedAt", "EntityId", "EntityName", "IsRead", "Message", "MunicipalityId", "PublicId",
                       CASE WHEN "IsRead" = 1 THEN "CreatedAt" ELSE NULL END, NULL, randomblob(16), "Title", "Type", "UserId"
                FROM "Notifications";
                DROP TABLE "Notifications";
                ALTER TABLE "Notifications_Governed" RENAME TO "Notifications";
                CREATE INDEX "IX_Notifications_MunicipalityId" ON "Notifications" ("MunicipalityId");
                CREATE UNIQUE INDEX "IX_Notifications_PublicId" ON "Notifications" ("PublicId");
                CREATE INDEX "IX_Notifications_ReadByUserId" ON "Notifications" ("ReadByUserId");
                CREATE INDEX "IX_Notifications_UserId_IsRead_CreatedAt" ON "Notifications" ("UserId", "IsRead", "CreatedAt");
                """);
        }

        private static void DownSqlite(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA defer_foreign_keys = ON;");
            migrationBuilder.Sql("""
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
                INSERT INTO "Notifications_Legacy"
                    ("Id", "CreatedAt", "EntityId", "EntityName", "IsRead", "Message", "MunicipalityId", "PublicId", "Title", "Type", "UserId")
                SELECT "Id", "CreatedAt", "EntityId", "EntityName", "IsRead", "Message", "MunicipalityId", "PublicId", "Title", "Type", "UserId"
                FROM "Notifications";
                DROP TABLE "Notifications";
                ALTER TABLE "Notifications_Legacy" RENAME TO "Notifications";
                CREATE INDEX "IX_Notifications_MunicipalityId" ON "Notifications" ("MunicipalityId");
                CREATE UNIQUE INDEX "IX_Notifications_PublicId" ON "Notifications" ("PublicId");
                CREATE INDEX "IX_Notifications_UserId" ON "Notifications" ("UserId");
                """);
        }
    }
}

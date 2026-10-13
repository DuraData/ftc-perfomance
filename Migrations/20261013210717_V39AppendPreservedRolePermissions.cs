using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39AppendPreservedRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                RebuildSqliteWithHistoryIdentity(migrationBuilder);
                return;
            }

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "RolePermissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "RolePermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PublicId",
                table: "RolePermissions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId_EffectiveFrom",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                RebuildSqliteWithCompositeIdentity(migrationBuilder);
                return;
            }

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [RolePermissions]
                    GROUP BY [RoleId], [PermissionId]
                    HAVING COUNT_BIG(*) > 1)
                    THROW 51000, 'Cannot downgrade append-preserved role permissions after successor history exists.', 1;
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PublicId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PermissionId_EffectiveFrom",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "RolePermissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" });
        }

        private static void RebuildSqliteWithHistoryIdentity(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "RolePermissions", newName: "RolePermissions_Legacy");
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAllowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScopeType = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", item => item.Id);
                    table.ForeignKey("FK_RolePermissions_AspNetRoles_RoleId", item => item.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_RolePermissions_Permissions_PermissionId", item => item.PermissionId, "Permissions", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.Sql("""
                INSERT INTO "RolePermissions"
                    ("PublicId", "RoleId", "PermissionId", "IsAllowed", "ScopeType", "IsActive", "EffectiveFrom", "EffectiveTo", "RowVersion")
                SELECT
                    lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))), 2) || '-' ||
                    substr('89ab', abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))),
                    "RoleId", "PermissionId", "IsAllowed", "ScopeType", "IsActive", "EffectiveFrom", "EffectiveTo", "RowVersion"
                FROM "RolePermissions_Legacy";
                """);
            migrationBuilder.DropTable(name: "RolePermissions_Legacy");
            migrationBuilder.CreateIndex(name: "IX_RolePermissions_PermissionId", table: "RolePermissions", column: "PermissionId");
            migrationBuilder.CreateIndex(name: "IX_RolePermissions_PublicId", table: "RolePermissions", column: "PublicId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_RolePermissions_RoleId_PermissionId_EffectiveFrom", table: "RolePermissions", columns: new[] { "RoleId", "PermissionId", "EffectiveFrom" });
        }

        private static void RebuildSqliteWithCompositeIdentity(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "RolePermissions", newName: "RolePermissions_History");
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAllowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScopeType = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", item => new { item.RoleId, item.PermissionId });
                    table.ForeignKey("FK_RolePermissions_AspNetRoles_RoleId", item => item.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_RolePermissions_Permissions_PermissionId", item => item.PermissionId, "Permissions", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.Sql("""
                INSERT INTO "RolePermissions"
                    ("RoleId", "PermissionId", "IsAllowed", "ScopeType", "IsActive", "EffectiveFrom", "EffectiveTo", "RowVersion")
                SELECT "RoleId", "PermissionId", "IsAllowed", "ScopeType", "IsActive", "EffectiveFrom", "EffectiveTo", "RowVersion"
                FROM "RolePermissions_History";
                """);
            migrationBuilder.DropTable(name: "RolePermissions_History");
            migrationBuilder.CreateIndex(name: "IX_RolePermissions_PermissionId", table: "RolePermissions", column: "PermissionId");
        }
    }
}

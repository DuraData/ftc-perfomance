using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedTargetTemplateDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                RebuildSqliteVersionTable(migrationBuilder, "OpmsTargetTemplateVersions", "OpmsTargetTemplates", "OpmsTargetTemplateId", cascade: false);
                RebuildSqliteVersionTable(migrationBuilder, "IpmsTargetTemplateVersions", "IpmsTargetTemplates", "IpmsTargetTemplateId", cascade: false);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargetTemplateVersions_IpmsTargetTemplates_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetTemplateVersions_OpmsTargetTemplates_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargetTemplateVersions_IpmsTargetTemplates_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions",
                column: "IpmsTargetTemplateId",
                principalTable: "IpmsTargetTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetTemplateVersions_OpmsTargetTemplates_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions",
                column: "OpmsTargetTemplateId",
                principalTable: "OpmsTargetTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                RebuildSqliteVersionTable(migrationBuilder, "OpmsTargetTemplateVersions", "OpmsTargetTemplates", "OpmsTargetTemplateId", cascade: true);
                RebuildSqliteVersionTable(migrationBuilder, "IpmsTargetTemplateVersions", "IpmsTargetTemplates", "IpmsTargetTemplateId", cascade: true);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargetTemplateVersions_IpmsTargetTemplates_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetTemplateVersions_OpmsTargetTemplates_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargetTemplateVersions_IpmsTargetTemplates_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions",
                column: "IpmsTargetTemplateId",
                principalTable: "IpmsTargetTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetTemplateVersions_OpmsTargetTemplates_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions",
                column: "OpmsTargetTemplateId",
                principalTable: "OpmsTargetTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void RebuildSqliteVersionTable(
            MigrationBuilder migrationBuilder,
            string table,
            string parentTable,
            string parentColumn,
            bool cascade)
        {
            var replacementTable = $"{table}_GovernedDeletionPolicy";
            var deleteAction = cascade ? "CASCADE" : "RESTRICT";
            migrationBuilder.Sql($$"""
                PRAGMA foreign_keys = OFF;
                BEGIN IMMEDIATE;
                CREATE TABLE "{{replacementTable}}" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_{{table}}" PRIMARY KEY AUTOINCREMENT,
                    "CreatedBy" TEXT NULL,
                    "CreatedDate" TEXT NOT NULL,
                    "{{parentColumn}}" INTEGER NOT NULL,
                    "PublicId" TEXT NOT NULL,
                    "RowVersion" BLOB NOT NULL,
                    "SnapshotJson" TEXT NOT NULL,
                    "Version" INTEGER NOT NULL,
                    CONSTRAINT "FK_{{table}}_{{parentTable}}_{{parentColumn}}" FOREIGN KEY ("{{parentColumn}}") REFERENCES "{{parentTable}}" ("Id") ON DELETE {{deleteAction}}
                );
                INSERT INTO "{{replacementTable}}" ("Id", "CreatedBy", "CreatedDate", "{{parentColumn}}", "PublicId", "RowVersion", "SnapshotJson", "Version")
                SELECT "Id", "CreatedBy", "CreatedDate", "{{parentColumn}}", "PublicId", "RowVersion", "SnapshotJson", "Version" FROM "{{table}}";
                DROP TABLE "{{table}}";
                ALTER TABLE "{{replacementTable}}" RENAME TO "{{table}}";
                CREATE UNIQUE INDEX "IX_{{table}}_PublicId" ON "{{table}}" ("PublicId");
                CREATE UNIQUE INDEX "IX_{{table}}_{{parentColumn}}_Version" ON "{{table}}" ("{{parentColumn}}", "Version");
                COMMIT;
                PRAGMA foreign_keys = ON;
                """, suppressTransaction: true);
        }
    }
}

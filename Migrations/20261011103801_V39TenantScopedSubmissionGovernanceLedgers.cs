using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39TenantScopedSubmissionGovernanceLedgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "SubmissionScores",
                    type: "nvarchar(450)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)");
            }

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "SubmissionScores",
                type: "bigint",
                nullable: true);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "ReviewComments",
                    type: "nvarchar(450)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)");
            }

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "ReviewComments",
                type: "bigint",
                nullable: true);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "DueDateExtensions",
                    type: "nvarchar(450)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)");
            }

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "DueDateExtensions",
                type: "bigint",
                nullable: true);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "AuditFindings",
                    type: "nvarchar(450)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)");
            }

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "AuditFindings",
                type: "bigint",
                nullable: true);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(BackfillSqlServer("SubmissionScores"));
                migrationBuilder.Sql(BackfillSqlServer("ReviewComments"));
                migrationBuilder.Sql(BackfillSqlServer("DueDateExtensions"));
                migrationBuilder.Sql(BackfillSqlServer("AuditFindings"));
            }
            else
            {
                migrationBuilder.Sql(BackfillPortable("SubmissionScores"));
                migrationBuilder.Sql(BackfillPortable("ReviewComments"));
                migrationBuilder.Sql(BackfillPortable("DueDateExtensions"));
                migrationBuilder.Sql(BackfillPortable("AuditFindings"));
            }

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionScores_MunicipalityId_SubmissionKind_SubmissionId",
                table: "SubmissionScores",
                columns: new[] { "MunicipalityId", "SubmissionKind", "SubmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_MunicipalityId_SubmissionKind_SubmissionId",
                table: "ReviewComments",
                columns: new[] { "MunicipalityId", "SubmissionKind", "SubmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DueDateExtensions_MunicipalityId_SubmissionKind_SubmissionId",
                table: "DueDateExtensions",
                columns: new[] { "MunicipalityId", "SubmissionKind", "SubmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_MunicipalityId_SubmissionKind_SubmissionId",
                table: "AuditFindings",
                columns: new[] { "MunicipalityId", "SubmissionKind", "SubmissionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditFindings_Municipalities_MunicipalityId",
                table: "AuditFindings",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DueDateExtensions_Municipalities_MunicipalityId",
                table: "DueDateExtensions",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewComments_Municipalities_MunicipalityId",
                table: "ReviewComments",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionScores_Municipalities_MunicipalityId",
                table: "SubmissionScores",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditFindings_Municipalities_MunicipalityId",
                table: "AuditFindings");

            migrationBuilder.DropForeignKey(
                name: "FK_DueDateExtensions_Municipalities_MunicipalityId",
                table: "DueDateExtensions");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewComments_Municipalities_MunicipalityId",
                table: "ReviewComments");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionScores_Municipalities_MunicipalityId",
                table: "SubmissionScores");

            migrationBuilder.DropIndex(
                name: "IX_SubmissionScores_MunicipalityId_SubmissionKind_SubmissionId",
                table: "SubmissionScores");

            migrationBuilder.DropIndex(
                name: "IX_ReviewComments_MunicipalityId_SubmissionKind_SubmissionId",
                table: "ReviewComments");

            migrationBuilder.DropIndex(
                name: "IX_DueDateExtensions_MunicipalityId_SubmissionKind_SubmissionId",
                table: "DueDateExtensions");

            migrationBuilder.DropIndex(
                name: "IX_AuditFindings_MunicipalityId_SubmissionKind_SubmissionId",
                table: "AuditFindings");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "SubmissionScores");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "ReviewComments");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "DueDateExtensions");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "AuditFindings");

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "SubmissionScores",
                    type: "nvarchar(max)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(450)");

                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "ReviewComments",
                    type: "nvarchar(max)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(450)");

                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "DueDateExtensions",
                    type: "nvarchar(max)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(450)");

                migrationBuilder.AlterColumn<string>(
                    name: "SubmissionId",
                    table: "AuditFindings",
                    type: "nvarchar(max)",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(450)");
            }
        }

        private static string BackfillSqlServer(string table) => $"""
            UPDATE ledger
            SET [MunicipalityId] = CASE
                WHEN ledger.[SubmissionKind] = 1 THEN opms.[MunicipalityId]
                WHEN ledger.[SubmissionKind] = 2 THEN ipms.[MunicipalityId]
                ELSE NULL
            END
            FROM [{table}] AS ledger
            LEFT JOIN [OpmsSubmissions] AS opms
                ON ledger.[SubmissionKind] = 1 AND opms.[Id] = ledger.[SubmissionId]
            LEFT JOIN [IpmsSubmissions] AS ipms
                ON ledger.[SubmissionKind] = 2 AND ipms.[Id] = ledger.[SubmissionId]
            WHERE ledger.[MunicipalityId] IS NULL;
            """;

        private static string BackfillPortable(string table) => $"""
            UPDATE "{table}"
            SET "MunicipalityId" = CASE
                WHEN "SubmissionKind" = 1 THEN (
                    SELECT "MunicipalityId" FROM "OpmsSubmissions"
                    WHERE "OpmsSubmissions"."Id" = "{table}"."SubmissionId")
                WHEN "SubmissionKind" = 2 THEN (
                    SELECT "MunicipalityId" FROM "IpmsSubmissions"
                    WHERE "IpmsSubmissions"."Id" = "{table}"."SubmissionId")
                ELSE NULL
            END
            WHERE "MunicipalityId" IS NULL;
            """;
    }
}

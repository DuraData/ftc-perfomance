using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedIdpSecondaryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            var guidType = sqlServer ? "uniqueidentifier" : "TEXT";
            string[] publicIdentityTables =
            [
                "IdpWardInputs", "IdpTaskAssignments", "IdpStakeholderEngagements", "IdpRiskLinks",
                "IdpCommunitySessions", "IdpCommunityNeeds", "IdpCollaborationComments", "IdpChangeLogs",
                "IdpBudgetSnapshots", "IdpAnnualTargets", "IdpAlignmentLinks"
            ];
            string[] mutableTables =
            [
                "IdpWardInputs", "IdpTaskAssignments", "IdpStakeholderEngagements", "IdpRiskLinks",
                "IdpCommunitySessions", "IdpCommunityNeeds", "IdpAnnualTargets", "IdpAlignmentLinks"
            ];

            foreach (var table in publicIdentityTables)
                migrationBuilder.AddColumn<Guid>(name: "PublicId", table: table, type: guidType, nullable: true);

            foreach (var table in mutableTables)
            {
                if (sqlServer)
                    migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: table, type: "rowversion", rowVersion: true, nullable: false, defaultValue: Array.Empty<byte>());
                else
                    migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: table, type: "BLOB", nullable: false, defaultValue: Array.Empty<byte>());
            }

            if (sqlServer)
            {
                foreach (var table in publicIdentityTables)
                    migrationBuilder.Sql($"UPDATE [{table}] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
            }
            else
            {
                const string sqliteGuid = "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";
                foreach (var table in publicIdentityTables)
                    migrationBuilder.Sql($"UPDATE [{table}] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
            }

            foreach (var table in publicIdentityTables)
                migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: table, type: guidType, nullable: false, oldClrType: typeof(Guid), oldType: guidType, oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpWardInputs_PublicId",
                table: "IdpWardInputs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpTaskAssignments_PublicId",
                table: "IdpTaskAssignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpStakeholderEngagements_PublicId",
                table: "IdpStakeholderEngagements",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpRiskLinks_PublicId",
                table: "IdpRiskLinks",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpCommunitySessions_PublicId",
                table: "IdpCommunitySessions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpCommunityNeeds_PublicId",
                table: "IdpCommunityNeeds",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpCollaborationComments_PublicId",
                table: "IdpCollaborationComments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpChangeLogs_PublicId",
                table: "IdpChangeLogs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpBudgetSnapshots_PublicId",
                table: "IdpBudgetSnapshots",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpAnnualTargets_PublicId",
                table: "IdpAnnualTargets",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpAlignmentLinks_PublicId",
                table: "IdpAlignmentLinks",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdpWardInputs_PublicId",
                table: "IdpWardInputs");

            migrationBuilder.DropIndex(
                name: "IX_IdpTaskAssignments_PublicId",
                table: "IdpTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_IdpStakeholderEngagements_PublicId",
                table: "IdpStakeholderEngagements");

            migrationBuilder.DropIndex(
                name: "IX_IdpRiskLinks_PublicId",
                table: "IdpRiskLinks");

            migrationBuilder.DropIndex(
                name: "IX_IdpCommunitySessions_PublicId",
                table: "IdpCommunitySessions");

            migrationBuilder.DropIndex(
                name: "IX_IdpCommunityNeeds_PublicId",
                table: "IdpCommunityNeeds");

            migrationBuilder.DropIndex(
                name: "IX_IdpCollaborationComments_PublicId",
                table: "IdpCollaborationComments");

            migrationBuilder.DropIndex(
                name: "IX_IdpChangeLogs_PublicId",
                table: "IdpChangeLogs");

            migrationBuilder.DropIndex(
                name: "IX_IdpBudgetSnapshots_PublicId",
                table: "IdpBudgetSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_IdpAnnualTargets_PublicId",
                table: "IdpAnnualTargets");

            migrationBuilder.DropIndex(
                name: "IX_IdpAlignmentLinks_PublicId",
                table: "IdpAlignmentLinks");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpWardInputs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpWardInputs");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpTaskAssignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpTaskAssignments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpStakeholderEngagements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpStakeholderEngagements");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpRiskLinks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpRiskLinks");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpCommunitySessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpCommunitySessions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpCommunityNeeds");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpCommunityNeeds");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpCollaborationComments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpChangeLogs");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpBudgetSnapshots");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpAnnualTargets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpAnnualTargets");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpAlignmentLinks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpAlignmentLinks");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39IdpPlanLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "IdpPlanVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "IdpPlanVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredecessorVersionId",
                table: "IdpPlanVersions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpPlanVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicationReference",
                table: "IdpPlanVersions",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "IdpPlanVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpPlanVersions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "IdpPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "IdpPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanFamilyId",
                table: "IdpPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredecessorPlanId",
                table: "IdpPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicationReference",
                table: "IdpPlans",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "IdpPlans",
                type: "datetime2",
                nullable: true);

            if (ActiveProvider.Contains("SqlServer"))
            {
                migrationBuilder.Sql("""
                    UPDATE [IdpPlans]
                    SET [PlanFamilyId] = NEWID(), [EffectiveFrom] = [CreatedAt]
                    WHERE [PlanFamilyId] IS NULL OR [EffectiveFrom] IS NULL;

                    UPDATE [IdpPlanVersions]
                    SET [PublicId] = NEWID(), [EffectiveFrom] = [CreatedAt]
                    WHERE [PublicId] IS NULL OR [EffectiveFrom] IS NULL;

                    UPDATE currentVersion
                    SET [PredecessorVersionId] = previousVersion.[Id]
                    FROM [IdpPlanVersions] currentVersion
                    OUTER APPLY (
                        SELECT TOP (1) candidate.[Id]
                        FROM [IdpPlanVersions] candidate
                        WHERE candidate.[IdpPlanId] = currentVersion.[IdpPlanId]
                          AND candidate.[VersionNumber] < currentVersion.[VersionNumber]
                        ORDER BY candidate.[VersionNumber] DESC
                    ) previousVersion
                    WHERE currentVersion.[PredecessorVersionId] IS NULL;

                    UPDATE currentVersion
                    SET [EffectiveTo] = nextVersion.[EffectiveFrom]
                    FROM [IdpPlanVersions] currentVersion
                    OUTER APPLY (
                        SELECT TOP (1) candidate.[EffectiveFrom]
                        FROM [IdpPlanVersions] candidate
                        WHERE candidate.[IdpPlanId] = currentVersion.[IdpPlanId]
                          AND candidate.[VersionNumber] > currentVersion.[VersionNumber]
                        ORDER BY candidate.[VersionNumber]
                    ) nextVersion
                    WHERE currentVersion.[IsActive] = 0 AND nextVersion.[EffectiveFrom] IS NOT NULL;
                    """);
            }
            else
            {
                const string sqliteGuid = "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(6)))";
                migrationBuilder.Sql($"UPDATE \"IdpPlans\" SET \"PlanFamilyId\" = {sqliteGuid}, \"EffectiveFrom\" = \"CreatedAt\" WHERE \"PlanFamilyId\" IS NULL OR \"EffectiveFrom\" IS NULL;");
                migrationBuilder.Sql($"UPDATE \"IdpPlanVersions\" SET \"PublicId\" = {sqliteGuid}, \"EffectiveFrom\" = \"CreatedAt\" WHERE \"PublicId\" IS NULL OR \"EffectiveFrom\" IS NULL;");
                migrationBuilder.Sql("UPDATE \"IdpPlanVersions\" SET \"PredecessorVersionId\" = (SELECT previous.\"Id\" FROM \"IdpPlanVersions\" previous WHERE previous.\"IdpPlanId\" = \"IdpPlanVersions\".\"IdpPlanId\" AND previous.\"VersionNumber\" < \"IdpPlanVersions\".\"VersionNumber\" ORDER BY previous.\"VersionNumber\" DESC LIMIT 1);");
                migrationBuilder.Sql("UPDATE \"IdpPlanVersions\" SET \"EffectiveTo\" = (SELECT following.\"EffectiveFrom\" FROM \"IdpPlanVersions\" following WHERE following.\"IdpPlanId\" = \"IdpPlanVersions\".\"IdpPlanId\" AND following.\"VersionNumber\" > \"IdpPlanVersions\".\"VersionNumber\" ORDER BY following.\"VersionNumber\" LIMIT 1) WHERE \"IsActive\" = 0;");
            }

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveFrom",
                table: "IdpPlanVersions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "IdpPlanVersions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveFrom",
                table: "IdpPlans",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PlanFamilyId",
                table: "IdpPlans",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlanVersions_PredecessorVersionId",
                table: "IdpPlanVersions",
                column: "PredecessorVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlanVersions_PublicId",
                table: "IdpPlanVersions",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdpPlanVersions_EffectiveDates",
                table: "IdpPlanVersions",
                sql: "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_MunicipalityId_PlanFamilyId",
                table: "IdpPlans",
                columns: new[] { "MunicipalityId", "PlanFamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_PredecessorPlanId",
                table: "IdpPlans",
                column: "PredecessorPlanId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdpPlans_EffectiveDates",
                table: "IdpPlans",
                sql: "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");

            migrationBuilder.AddForeignKey(
                name: "FK_IdpPlans_IdpPlans_PredecessorPlanId",
                table: "IdpPlans",
                column: "PredecessorPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpPlanVersions_IdpPlanVersions_PredecessorVersionId",
                table: "IdpPlanVersions",
                column: "PredecessorVersionId",
                principalTable: "IdpPlanVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdpPlans_IdpPlans_PredecessorPlanId",
                table: "IdpPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpPlanVersions_IdpPlanVersions_PredecessorVersionId",
                table: "IdpPlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_IdpPlanVersions_PredecessorVersionId",
                table: "IdpPlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_IdpPlanVersions_PublicId",
                table: "IdpPlanVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdpPlanVersions_EffectiveDates",
                table: "IdpPlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_IdpPlans_MunicipalityId_PlanFamilyId",
                table: "IdpPlans");

            migrationBuilder.DropIndex(
                name: "IX_IdpPlans_PredecessorPlanId",
                table: "IdpPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdpPlans_EffectiveDates",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "PredecessorVersionId",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "PublicationReference",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpPlanVersions");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "PlanFamilyId",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "PredecessorPlanId",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "PublicationReference",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "IdpPlans");
        }
    }
}

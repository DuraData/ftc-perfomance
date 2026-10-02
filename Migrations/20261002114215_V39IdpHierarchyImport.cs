using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39IdpHierarchyImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdpDevelopmentPriorities_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpStrategicOutcomes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpStrategicOutcomes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpStrategicObjectives",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpStrategicObjectives",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpProjects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpProjects",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpProgrammes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpProgrammes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "PriorityCode",
                table: "IdpDevelopmentPriorities",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpDevelopmentPriorities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpDevelopmentPriorities",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("UPDATE [IdpStrategicOutcomes] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [IdpStrategicObjectives] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [IdpDevelopmentPriorities] SET [PublicId] = NEWID(), [PriorityCode] = CONCAT('PRIORITY-', [Id]) WHERE [PublicId] IS NULL OR [PriorityCode] IS NULL;");
                migrationBuilder.Sql("UPDATE [IdpProgrammes] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [IdpProjects] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
            }
            else
            {
                const string sqliteGuid = "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";
                migrationBuilder.Sql($"UPDATE [IdpStrategicOutcomes] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [IdpStrategicObjectives] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [IdpDevelopmentPriorities] SET [PublicId] = {sqliteGuid}, [PriorityCode] = 'PRIORITY-' || [Id] WHERE [PublicId] IS NULL OR [PriorityCode] IS NULL;");
                migrationBuilder.Sql($"UPDATE [IdpProgrammes] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [IdpProjects] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
            }

            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IdpStrategicOutcomes", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IdpStrategicObjectives", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IdpDevelopmentPriorities", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "PriorityCode", table: "IdpDevelopmentPriorities", type: "nvarchar(80)", maxLength: 80, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(80)", oldMaxLength: 80, oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IdpProgrammes", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IdpProjects", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpStrategicOutcomes_PublicId",
                table: "IdpStrategicOutcomes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpStrategicObjectives_PublicId",
                table: "IdpStrategicObjectives",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpProjects_PublicId",
                table: "IdpProjects",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpProgrammes_PublicId",
                table: "IdpProgrammes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpDevelopmentPriorities_IdpStrategicObjectiveId_PriorityCode",
                table: "IdpDevelopmentPriorities",
                columns: new[] { "IdpStrategicObjectiveId", "PriorityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpDevelopmentPriorities_PublicId",
                table: "IdpDevelopmentPriorities",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdpStrategicOutcomes_PublicId",
                table: "IdpStrategicOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_IdpStrategicObjectives_PublicId",
                table: "IdpStrategicObjectives");

            migrationBuilder.DropIndex(
                name: "IX_IdpProjects_PublicId",
                table: "IdpProjects");

            migrationBuilder.DropIndex(
                name: "IX_IdpProgrammes_PublicId",
                table: "IdpProgrammes");

            migrationBuilder.DropIndex(
                name: "IX_IdpDevelopmentPriorities_IdpStrategicObjectiveId_PriorityCode",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.DropIndex(
                name: "IX_IdpDevelopmentPriorities_PublicId",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpStrategicOutcomes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpStrategicOutcomes");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpStrategicObjectives");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpStrategicObjectives");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpProjects");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpProjects");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpProgrammes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpProgrammes");

            migrationBuilder.DropColumn(
                name: "PriorityCode",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.CreateIndex(
                name: "IX_IdpDevelopmentPriorities_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities",
                column: "IdpStrategicObjectiveId");
        }
    }
}

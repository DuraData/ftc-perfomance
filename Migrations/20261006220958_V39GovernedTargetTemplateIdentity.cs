using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedTargetTemplateIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetTemplateVersions_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargetTemplateVersions_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "OpmsTargetTemplateVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsTargetTemplateVersions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "OpmsTargetTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsTargetTemplates",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IpmsTargetTemplateVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IpmsTargetTemplateVersions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IpmsTargetTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IpmsTargetTemplates",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("UPDATE [OpmsTargetTemplates] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [OpmsTargetTemplateVersions] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [IpmsTargetTemplates] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [IpmsTargetTemplateVersions] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
            }
            else
            {
                const string sqliteGuid = "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";
                migrationBuilder.Sql($"UPDATE [OpmsTargetTemplates] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [OpmsTargetTemplateVersions] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [IpmsTargetTemplates] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [IpmsTargetTemplateVersions] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
            }

            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "OpmsTargetTemplates", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "OpmsTargetTemplateVersions", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IpmsTargetTemplates", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "IpmsTargetTemplateVersions", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetTemplateVersions_OpmsTargetTemplateId_Version",
                table: "OpmsTargetTemplateVersions",
                columns: new[] { "OpmsTargetTemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetTemplateVersions_PublicId",
                table: "OpmsTargetTemplateVersions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetTemplates_PublicId",
                table: "OpmsTargetTemplates",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargetTemplateVersions_IpmsTargetTemplateId_Version",
                table: "IpmsTargetTemplateVersions",
                columns: new[] { "IpmsTargetTemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargetTemplateVersions_PublicId",
                table: "IpmsTargetTemplateVersions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargetTemplates_PublicId",
                table: "IpmsTargetTemplates",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetTemplateVersions_OpmsTargetTemplateId_Version",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetTemplateVersions_PublicId",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetTemplates_PublicId",
                table: "OpmsTargetTemplates");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargetTemplateVersions_IpmsTargetTemplateId_Version",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargetTemplateVersions_PublicId",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargetTemplates_PublicId",
                table: "IpmsTargetTemplates");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsTargetTemplateVersions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "OpmsTargetTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsTargetTemplates");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IpmsTargetTemplateVersions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IpmsTargetTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IpmsTargetTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetTemplateVersions_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions",
                column: "OpmsTargetTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargetTemplateVersions_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions",
                column: "IpmsTargetTemplateId");
        }
    }
}

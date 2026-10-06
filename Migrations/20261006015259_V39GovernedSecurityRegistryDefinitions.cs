using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedSecurityRegistryDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "SecurityResources",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "SecurityMemberDefinitions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SecurityMemberDefinitions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "SecurityActionDefinitions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityResources_PublicId",
                table: "SecurityResources",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityMemberDefinitions_PublicId",
                table: "SecurityMemberDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityActionDefinitions_PublicId",
                table: "SecurityActionDefinitions",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityResources_PublicId",
                table: "SecurityResources");

            migrationBuilder.DropIndex(
                name: "IX_SecurityMemberDefinitions_PublicId",
                table: "SecurityMemberDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_SecurityActionDefinitions_PublicId",
                table: "SecurityActionDefinitions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "SecurityResources");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "SecurityMemberDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SecurityMemberDefinitions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "SecurityActionDefinitions");
        }
    }
}

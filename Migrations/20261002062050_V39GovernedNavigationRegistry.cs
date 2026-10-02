using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedNavigationRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "SecurityNavigationItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [SecurityNavigationItems] SET [PublicId] = NEWID();");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityNavigationItems_PublicId",
                table: "SecurityNavigationItems",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityNavigationItems_PublicId",
                table: "SecurityNavigationItems");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "SecurityNavigationItems");
        }
    }
}

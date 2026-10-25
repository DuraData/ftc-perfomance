using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39DeterministicNavigationOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Route",
                table: "SecurityNavigationItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchMode",
                table: "SecurityNavigationItems",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql("UPDATE SecurityNavigationItems SET Route = '/poe', MatchMode = 2 WHERE Code = 'NAV.POE' AND LOWER(Route) = '/opms/submissions';");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityNavigationItems_Route_MatchMode",
                table: "SecurityNavigationItems",
                columns: new[] { "Route", "MatchMode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE SecurityNavigationItems SET Route = '/opms/submissions' WHERE Code = 'NAV.POE' AND LOWER(Route) = '/poe';");
            migrationBuilder.DropIndex(
                name: "IX_SecurityNavigationItems_Route_MatchMode",
                table: "SecurityNavigationItems");

            migrationBuilder.DropColumn(
                name: "MatchMode",
                table: "SecurityNavigationItems");

            migrationBuilder.AlterColumn<string>(
                name: "Route",
                table: "SecurityNavigationItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}

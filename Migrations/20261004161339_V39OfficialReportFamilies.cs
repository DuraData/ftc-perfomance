using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39OfficialReportFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReportType",
                table: "OfficialReportTemplates",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ReportType",
                table: "OfficialReportGenerations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OfficialReportTemplates_ReportType",
                table: "OfficialReportTemplates",
                sql: "[ReportType] >= 1 AND [ReportType] <= 16");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OfficialReportGenerations_ReportType",
                table: "OfficialReportGenerations",
                sql: "[ReportType] >= 1 AND [ReportType] <= 16");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OfficialReportTemplates_ReportType",
                table: "OfficialReportTemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OfficialReportGenerations_ReportType",
                table: "OfficialReportGenerations");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "OfficialReportTemplates");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "OfficialReportGenerations");
        }
    }
}

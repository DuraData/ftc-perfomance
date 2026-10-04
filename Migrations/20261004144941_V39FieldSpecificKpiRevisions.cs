using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39FieldSpecificKpiRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBudgetRevised",
                table: "PerformancePeriodTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTargetRevised",
                table: "PerformancePeriodTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RevisedBudgetValue",
                table: "PerformancePeriodTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisedTargetValue",
                table: "PerformancePeriodTargets",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisedUnitKind",
                table: "PerformancePeriodTargets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIndicatorNumberRevised",
                table: "OpmsTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsKpiDescriptionRevised",
                table: "OpmsTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTargetNameRevised",
                table: "OpmsTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RevisedIndicatorNumber",
                table: "OpmsTargets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisedKpiDescription",
                table: "OpmsTargets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisedTargetName",
                table: "OpmsTargets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIndicatorNumberRevised",
                table: "IpmsTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsKpiDescriptionRevised",
                table: "IpmsTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTargetNameRevised",
                table: "IpmsTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RevisedIndicatorNumber",
                table: "IpmsTargets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisedKpiDescription",
                table: "IpmsTargets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisedTargetName",
                table: "IpmsTargets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBudgetRevised",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "IsTargetRevised",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "RevisedBudgetValue",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "RevisedTargetValue",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "RevisedUnitKind",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "IsIndicatorNumberRevised",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "IsKpiDescriptionRevised",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "IsTargetNameRevised",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedIndicatorNumber",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedKpiDescription",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedTargetName",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "IsIndicatorNumberRevised",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "IsKpiDescriptionRevised",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "IsTargetNameRevised",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedIndicatorNumber",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedKpiDescription",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedTargetName",
                table: "IpmsTargets");
        }
    }
}

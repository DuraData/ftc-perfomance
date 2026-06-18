using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddQuarterlyTargetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns to IpmsTargets
            migrationBuilder.AddColumn<string>(
                name: "Q1Description",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Q1Budget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Q2Description",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Q2Budget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MidTermDescription",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MidTermBudget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Q3Description",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Q3Budget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Q3RevisedTarget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Q4Description",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Q4Budget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Q4RevisedTarget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RevisedAnnualTarget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RevisedAnnualBudget",
                table: "IpmsTargets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove columns from IpmsTargets
            migrationBuilder.DropColumn(
                name: "Q1Description",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q1Budget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q2Description",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q2Budget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "MidTermDescription",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "MidTermBudget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q3Description",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q3Budget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q3RevisedTarget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q4Description",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q4Budget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "Q4RevisedTarget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedAnnualTarget",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedAnnualBudget",
                table: "IpmsTargets");
        }
    }
}

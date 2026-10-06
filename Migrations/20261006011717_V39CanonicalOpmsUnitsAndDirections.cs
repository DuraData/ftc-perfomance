using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39CanonicalOpmsUnitsAndDirections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OpmsUnitId",
                table: "PerformancePeriodTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PerformanceDirectionId",
                table: "PerformancePeriodTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RevisedOpmsUnitId",
                table: "PerformancePeriodTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerformanceDirectionDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EngineDirection = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceDirectionDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpmsUnitDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    InputControlType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValueDataType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    SupportsAutoVariance = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPerformanceDirectionId = table.Column<long>(type: "bigint", nullable: true),
                    RequiresComponentUi = table.Column<bool>(type: "bit", nullable: false),
                    IsQualitative = table.Column<bool>(type: "bit", nullable: false),
                    EngineUnitKind = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsUnitDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsUnitDefinitions_PerformanceDirectionDefinitions_DefaultPerformanceDirectionId",
                        column: x => x.DefaultPerformanceDirectionId,
                        principalTable: "PerformanceDirectionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PerformanceDirectionDefinitions",
                columns: new[] { "Id", "Code", "Description", "EngineDirection", "IsActive", "Name", "PublicId" },
                values: new object[,]
                {
                    { 1L, "TARGET_OR_HIGHER", "Actual at or above target is achieved.", 1, true, "Target or higher", new Guid("10000000-0000-0000-0000-000000000001") },
                    { 2L, "TARGET_OR_LOWER", "Actual at or below target is achieved.", 2, true, "Target or lower", new Guid("10000000-0000-0000-0000-000000000002") },
                    { 3L, "EXACT", "Actual must equal target.", 3, true, "Exact", new Guid("10000000-0000-0000-0000-000000000003") },
                    { 4L, "HIGHER_BETTER", "Higher normalized performance is favourable.", 1, true, "Higher is better", new Guid("10000000-0000-0000-0000-000000000004") },
                    { 5L, "LOWER_BETTER", "Lower normalized performance is favourable.", 2, true, "Lower is better", new Guid("10000000-0000-0000-0000-000000000005") },
                    { 6L, "ON_OR_BEFORE_DATE", "Actual date must be on or before the target date.", 2, true, "On or before date", new Guid("10000000-0000-0000-0000-000000000006") },
                    { 7L, "ON_OR_AFTER_DATE", "Actual date must be on or after the target date.", 1, true, "On or after date", new Guid("10000000-0000-0000-0000-000000000007") },
                    { 8L, "YES_IS_SUCCESS", "A Yes actual is achieved.", 3, true, "Yes is success", new Guid("10000000-0000-0000-0000-000000000008") },
                    { 9L, "NO_IS_SUCCESS", "A No actual is achieved.", 3, true, "No is success", new Guid("10000000-0000-0000-0000-000000000009") },
                    { 10L, "MANUAL", "The system does not determine achievement.", 3, true, "Manual", new Guid("10000000-0000-0000-0000-000000000010") }
                });

            migrationBuilder.InsertData(
                table: "OpmsUnitDefinitions",
                columns: new[] { "Id", "Code", "DecimalPlaces", "DefaultPerformanceDirectionId", "EngineUnitKind", "InputControlType", "IsActive", "IsQualitative", "MaxValue", "MinValue", "Name", "PublicId", "RequiresComponentUi", "SupportsAutoVariance", "Symbol", "ValueDataType" },
                values: new object[,]
                {
                    { 1L, "NUMBER", 2, 1L, 2, "NUMERIC", true, false, null, 0m, "Number", new Guid("20000000-0000-0000-0000-000000000001"), false, true, null, "DECIMAL" },
                    { 2L, "PERCENT", 2, 1L, 1, "NUMERIC", true, false, 100m, 0m, "Percentage", new Guid("20000000-0000-0000-0000-000000000002"), false, true, "%", "DECIMAL" },
                    { 3L, "FINANCIAL", 2, 1L, 3, "CURRENCY", true, false, null, 0m, "Financial", new Guid("20000000-0000-0000-0000-000000000003"), false, true, "R", "DECIMAL" },
                    { 4L, "DATE", null, 6L, 10, "DATE", true, false, null, null, "Date", new Guid("20000000-0000-0000-0000-000000000004"), false, true, null, "DATE" },
                    { 5L, "RATIO", 6, 1L, 8, "RATIO", true, false, null, 0m, "Ratio", new Guid("20000000-0000-0000-0000-000000000005"), true, true, null, "JSON" },
                    { 6L, "TIME", 2, 2L, 4, "NUMERIC_UNIT", true, false, null, 0m, "Time", new Guid("20000000-0000-0000-0000-000000000006"), true, true, null, "DECIMAL" },
                    { 7L, "AREA", 2, 1L, 5, "NUMERIC_UNIT", true, false, null, 0m, "Area", new Guid("20000000-0000-0000-0000-000000000007"), true, true, null, "DECIMAL" },
                    { 8L, "VOLUME", 2, 1L, 6, "NUMERIC_UNIT", true, false, null, 0m, "Volume", new Guid("20000000-0000-0000-0000-000000000008"), true, true, null, "DECIMAL" },
                    { 9L, "YES_NO", null, 8L, 9, "SELECT", true, false, null, null, "Yes or No", new Guid("20000000-0000-0000-0000-000000000009"), false, true, null, "BOOLEAN" },
                    { 10L, "SCALE_1_3", 0, 1L, 11, "SELECT", true, false, 3m, 1m, "Scale 1 to 3", new Guid("20000000-0000-0000-0000-000000000010"), false, true, null, "INTEGER" },
                    { 11L, "ZERO_NUMBER", 2, 2L, 14, "NUMERIC", true, false, null, 0m, "Zero number", new Guid("20000000-0000-0000-0000-000000000011"), false, true, null, "DECIMAL" },
                    { 12L, "QUALITATIVE", null, 10L, 13, "TEXT", true, true, null, null, "Qualitative", new Guid("20000000-0000-0000-0000-000000000012"), false, false, null, "TEXT" }
                });

            migrationBuilder.Sql(
                """
                UPDATE [PerformancePeriodTargets]
                SET [OpmsUnitId] = CASE [UnitKind]
                    WHEN 0 THEN 12 WHEN 1 THEN 2 WHEN 2 THEN 1 WHEN 3 THEN 3
                    WHEN 4 THEN 6 WHEN 5 THEN 7 WHEN 6 THEN 8 WHEN 7 THEN 10
                    WHEN 8 THEN 5 WHEN 9 THEN 9 WHEN 10 THEN 4 WHEN 11 THEN 10
                    WHEN 12 THEN 9 WHEN 13 THEN 12 WHEN 14 THEN 11 WHEN 15 THEN 1
                    WHEN 16 THEN 1 ELSE NULL END,
                    [PerformanceDirectionId] = CASE [Direction]
                    WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 3 ELSE NULL END,
                    [RevisedOpmsUnitId] = CASE [RevisedUnitKind]
                    WHEN 0 THEN 12 WHEN 1 THEN 2 WHEN 2 THEN 1 WHEN 3 THEN 3
                    WHEN 4 THEN 6 WHEN 5 THEN 7 WHEN 6 THEN 8 WHEN 7 THEN 10
                    WHEN 8 THEN 5 WHEN 9 THEN 9 WHEN 10 THEN 4 WHEN 11 THEN 10
                    WHEN 12 THEN 9 WHEN 13 THEN 12 WHEN 14 THEN 11 WHEN 15 THEN 1
                    WHEN 16 THEN 1 ELSE NULL END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_OpmsUnitId",
                table: "PerformancePeriodTargets",
                column: "OpmsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_PerformanceDirectionId",
                table: "PerformancePeriodTargets",
                column: "PerformanceDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_RevisedOpmsUnitId",
                table: "PerformancePeriodTargets",
                column: "RevisedOpmsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsUnitDefinitions_Code",
                table: "OpmsUnitDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsUnitDefinitions_DefaultPerformanceDirectionId",
                table: "OpmsUnitDefinitions",
                column: "DefaultPerformanceDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsUnitDefinitions_PublicId",
                table: "OpmsUnitDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceDirectionDefinitions_Code",
                table: "PerformanceDirectionDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceDirectionDefinitions_PublicId",
                table: "PerformanceDirectionDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformancePeriodTargets_OpmsUnitDefinitions_OpmsUnitId",
                table: "PerformancePeriodTargets",
                column: "OpmsUnitId",
                principalTable: "OpmsUnitDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformancePeriodTargets_OpmsUnitDefinitions_RevisedOpmsUnitId",
                table: "PerformancePeriodTargets",
                column: "RevisedOpmsUnitId",
                principalTable: "OpmsUnitDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformancePeriodTargets_PerformanceDirectionDefinitions_PerformanceDirectionId",
                table: "PerformancePeriodTargets",
                column: "PerformanceDirectionId",
                principalTable: "PerformanceDirectionDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformancePeriodTargets_OpmsUnitDefinitions_OpmsUnitId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformancePeriodTargets_OpmsUnitDefinitions_RevisedOpmsUnitId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformancePeriodTargets_PerformanceDirectionDefinitions_PerformanceDirectionId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropTable(
                name: "OpmsUnitDefinitions");

            migrationBuilder.DropTable(
                name: "PerformanceDirectionDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_PerformancePeriodTargets_OpmsUnitId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropIndex(
                name: "IX_PerformancePeriodTargets_PerformanceDirectionId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropIndex(
                name: "IX_PerformancePeriodTargets_RevisedOpmsUnitId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "OpmsUnitId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "PerformanceDirectionId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "RevisedOpmsUnitId",
                table: "PerformancePeriodTargets");
        }
    }
}

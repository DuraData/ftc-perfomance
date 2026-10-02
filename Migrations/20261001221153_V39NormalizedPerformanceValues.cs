using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39NormalizedPerformanceValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_OpmsTargetId",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_IpmsTargetId",
                table: "IpmsSubmissions");

            migrationBuilder.AddColumn<decimal>(
                name: "AchievementPercent",
                table: "OpmsSubmissions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualPerformance",
                table: "OpmsSubmissions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReportingPeriodId",
                table: "OpmsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TargetAchieved",
                table: "OpmsSubmissions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AchievementPercent",
                table: "IpmsSubmissions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualPerformance",
                table: "IpmsSubmissions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReportingPeriodId",
                table: "IpmsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TargetAchieved",
                table: "IpmsSubmissions",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerformancePeriodTargets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UnitKind = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    BudgetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformancePeriodTargets", x => x.Id);
                    table.CheckConstraint("CK_PerformancePeriodTargets_OneKpi", "CASE WHEN [OpmsTargetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsTargetId] IS NULL THEN 0 ELSE 1 END = 1");
                    table.ForeignKey(
                        name: "FK_PerformancePeriodTargets_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformancePeriodTargets_IpmsTargets_IpmsTargetId",
                        column: x => x.IpmsTargetId,
                        principalTable: "IpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformancePeriodTargets_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformancePeriodTargets_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformancePeriodTargets_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceTargetRevisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    PerformancePeriodTargetId = table.Column<long>(type: "bigint", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RevisedValue = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceTargetRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceTargetRevisions_AspNetUsers_RevisedByUserId",
                        column: x => x.RevisedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceTargetRevisions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceTargetRevisions_PerformancePeriodTargets_PerformancePeriodTargetId",
                        column: x => x.PerformancePeriodTargetId,
                        principalTable: "PerformancePeriodTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_OpmsTargetId_ReportingPeriodId",
                table: "OpmsSubmissions",
                columns: new[] { "OpmsTargetId", "ReportingPeriodId" },
                unique: true,
                filter: "[ReportingPeriodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_ReportingPeriodId",
                table: "OpmsSubmissions",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_IpmsTargetId_ReportingPeriodId",
                table: "IpmsSubmissions",
                columns: new[] { "IpmsTargetId", "ReportingPeriodId" },
                unique: true,
                filter: "[ReportingPeriodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_ReportingPeriodId",
                table: "IpmsSubmissions",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_CreatedByUserId",
                table: "PerformancePeriodTargets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_IpmsTargetId_ReportingPeriodId",
                table: "PerformancePeriodTargets",
                columns: new[] { "IpmsTargetId", "ReportingPeriodId" },
                unique: true,
                filter: "[IpmsTargetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_MunicipalityId",
                table: "PerformancePeriodTargets",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_OpmsTargetId_ReportingPeriodId",
                table: "PerformancePeriodTargets",
                columns: new[] { "OpmsTargetId", "ReportingPeriodId" },
                unique: true,
                filter: "[OpmsTargetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_PublicId",
                table: "PerformancePeriodTargets",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_ReportingPeriodId",
                table: "PerformancePeriodTargets",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceTargetRevisions_MunicipalityId",
                table: "PerformanceTargetRevisions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceTargetRevisions_PerformancePeriodTargetId_RecordedAt",
                table: "PerformanceTargetRevisions",
                columns: new[] { "PerformancePeriodTargetId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceTargetRevisions_PublicId",
                table: "PerformanceTargetRevisions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceTargetRevisions_RevisedByUserId",
                table: "PerformanceTargetRevisions",
                column: "RevisedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsSubmissions_ReportingPeriods_ReportingPeriodId",
                table: "IpmsSubmissions",
                column: "ReportingPeriodId",
                principalTable: "ReportingPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsSubmissions_ReportingPeriods_ReportingPeriodId",
                table: "OpmsSubmissions",
                column: "ReportingPeriodId",
                principalTable: "ReportingPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IpmsSubmissions_ReportingPeriods_ReportingPeriodId",
                table: "IpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsSubmissions_ReportingPeriods_ReportingPeriodId",
                table: "OpmsSubmissions");

            migrationBuilder.DropTable(
                name: "PerformanceTargetRevisions");

            migrationBuilder.DropTable(
                name: "PerformancePeriodTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_OpmsTargetId_ReportingPeriodId",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_ReportingPeriodId",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_IpmsTargetId_ReportingPeriodId",
                table: "IpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_ReportingPeriodId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AchievementPercent",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ActualPerformance",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ReportingPeriodId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "TargetAchieved",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AchievementPercent",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ActualPerformance",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ReportingPeriodId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "TargetAchieved",
                table: "IpmsSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_OpmsTargetId",
                table: "OpmsSubmissions",
                column: "OpmsTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_IpmsTargetId",
                table: "IpmsSubmissions",
                column: "IpmsTargetId");
        }
    }
}

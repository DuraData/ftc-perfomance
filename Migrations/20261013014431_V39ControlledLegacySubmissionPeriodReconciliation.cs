using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ControlledLegacySubmissionPeriodReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            var idType = sqlServer ? "bigint" : "INTEGER";
            var guidType = sqlServer ? "uniqueidentifier" : "TEXT";
            var integerType = sqlServer ? "int" : "INTEGER";
            var userIdType = sqlServer ? "nvarchar(450)" : "TEXT";
            var shortTextType = sqlServer ? "nvarchar(100)" : "TEXT";
            var reasonType = sqlServer ? "nvarchar(1000)" : "TEXT";
            var dateTimeType = sqlServer ? "datetime2" : "TEXT";

            migrationBuilder.CreateTable(
                name: "LegacySubmissionPeriodReconciliations",
                columns: table => new
                {
                    Id = table.Column<long>(type: idType, nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: guidType, nullable: false),
                    MunicipalityId = table.Column<long>(type: idType, nullable: false),
                    SubmissionKind = table.Column<int>(type: integerType, nullable: false),
                    OpmsSubmissionId = table.Column<string>(type: userIdType, nullable: true),
                    IpmsSubmissionId = table.Column<string>(type: userIdType, nullable: true),
                    SourceQuarterSnapshot = table.Column<string>(type: shortTextType, maxLength: 100, nullable: false),
                    ReportingPeriodId = table.Column<long>(type: idType, nullable: false),
                    ReconciledByUserId = table.Column<string>(type: userIdType, nullable: false),
                    ReconciledAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    Reason = table.Column<string>(type: reasonType, maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: shortTextType, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacySubmissionPeriodReconciliations", x => x.Id);
                    table.CheckConstraint("CK_LegacySubmissionPeriodReconciliations_OneSubmission", "CASE WHEN [OpmsSubmissionId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsSubmissionId] IS NULL THEN 0 ELSE 1 END = 1");
                    table.ForeignKey(
                        name: "FK_LegacySubmissionPeriodReconciliations_AspNetUsers_ReconciledByUserId",
                        column: x => x.ReconciledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionPeriodReconciliations_IpmsSubmissions_IpmsSubmissionId",
                        column: x => x.IpmsSubmissionId,
                        principalTable: "IpmsSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionPeriodReconciliations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionPeriodReconciliations_OpmsSubmissions_OpmsSubmissionId",
                        column: x => x.OpmsSubmissionId,
                        principalTable: "OpmsSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionPeriodReconciliations_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionPeriodReconciliations_IpmsSubmissionId",
                table: "LegacySubmissionPeriodReconciliations",
                column: "IpmsSubmissionId",
                unique: true,
                filter: "[IpmsSubmissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionPeriodReconciliations_MunicipalityId",
                table: "LegacySubmissionPeriodReconciliations",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionPeriodReconciliations_OpmsSubmissionId",
                table: "LegacySubmissionPeriodReconciliations",
                column: "OpmsSubmissionId",
                unique: true,
                filter: "[OpmsSubmissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionPeriodReconciliations_PublicId",
                table: "LegacySubmissionPeriodReconciliations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionPeriodReconciliations_ReconciledByUserId",
                table: "LegacySubmissionPeriodReconciliations",
                column: "ReconciledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionPeriodReconciliations_ReportingPeriodId",
                table: "LegacySubmissionPeriodReconciliations",
                column: "ReportingPeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacySubmissionPeriodReconciliations");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39InternalAuditAssessmentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternalAuditAssessmentConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    Model = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalAuditAssessmentConfigurations", x => x.Id);
                    table.CheckConstraint("CK_InternalAuditAssessmentConfigurations_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessmentConfigurations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessmentConfigurations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessmentConfigurations_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InternalAuditAssessments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionWorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    ConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousAssessmentId = table.Column<long>(type: "bigint", nullable: true),
                    PerformanceRfiId = table.Column<long>(type: "bigint", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    DetailedObservation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Score = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AssessedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalAuditAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessments_AspNetUsers_AssessedByUserId",
                        column: x => x.AssessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessments_InternalAuditAssessmentConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "InternalAuditAssessmentConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessments_InternalAuditAssessments_PreviousAssessmentId",
                        column: x => x.PreviousAssessmentId,
                        principalTable: "InternalAuditAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessments_PerformanceRfis_PerformanceRfiId",
                        column: x => x.PerformanceRfiId,
                        principalTable: "PerformanceRfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalAuditAssessments_SubmissionWorkflowInstances_SubmissionWorkflowInstanceId",
                        column: x => x.SubmissionWorkflowInstanceId,
                        principalTable: "SubmissionWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessmentConfigurations_CreatedByUserId",
                table: "InternalAuditAssessmentConfigurations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessmentConfigurations_MunicipalityFinancialYearId_IsCurrent",
                table: "InternalAuditAssessmentConfigurations",
                columns: new[] { "MunicipalityFinancialYearId", "IsCurrent" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessmentConfigurations_MunicipalityFinancialYearId_Version",
                table: "InternalAuditAssessmentConfigurations",
                columns: new[] { "MunicipalityFinancialYearId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessmentConfigurations_MunicipalityId",
                table: "InternalAuditAssessmentConfigurations",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessmentConfigurations_PublicId",
                table: "InternalAuditAssessmentConfigurations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_AssessedByUserId",
                table: "InternalAuditAssessments",
                column: "AssessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_ConfigurationId",
                table: "InternalAuditAssessments",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_MunicipalityId",
                table: "InternalAuditAssessments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_PerformanceRfiId",
                table: "InternalAuditAssessments",
                column: "PerformanceRfiId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_PreviousAssessmentId",
                table: "InternalAuditAssessments",
                column: "PreviousAssessmentId",
                unique: true,
                filter: "[PreviousAssessmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_PublicId",
                table: "InternalAuditAssessments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternalAuditAssessments_SubmissionWorkflowInstanceId_AssessedAt",
                table: "InternalAuditAssessments",
                columns: new[] { "SubmissionWorkflowInstanceId", "AssessedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternalAuditAssessments");

            migrationBuilder.DropTable(
                name: "InternalAuditAssessmentConfigurations");
        }
    }
}

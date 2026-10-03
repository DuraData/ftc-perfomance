using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39OfficialReportGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficialReportTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateFamilyPublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    HeadingTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ColumnConfigurationJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialReportTemplates", x => x.Id);
                    table.CheckConstraint("CK_OfficialReportTemplates_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_OfficialReportTemplates_Version", "[VersionNumber] >= 1");
                    table.ForeignKey(
                        name: "FK_OfficialReportTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportTemplates_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportTemplates_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportTemplates_OfficialReportTemplates_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "OfficialReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficialReportGenerations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GenerationFamilyPublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    ReportTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    EvidenceBlobId = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ScopeJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    DataVersionReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialReportGenerations", x => x.Id);
                    table.CheckConstraint("CK_OfficialReportGenerations_Version", "[VersionNumber] >= 1 AND [RowCount] >= 0 AND [SizeInBytes] >= 0");
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerations_AspNetUsers_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerations_EvidenceBlobs_EvidenceBlobId",
                        column: x => x.EvidenceBlobId,
                        principalTable: "EvidenceBlobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerations_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerations_OfficialReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "OfficialReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerations_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_EvidenceBlobId",
                table: "OfficialReportGenerations",
                column: "EvidenceBlobId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_GeneratedByUserId",
                table: "OfficialReportGenerations",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_MunicipalityFinancialYearId",
                table: "OfficialReportGenerations",
                column: "MunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_MunicipalityId_GenerationFamilyPublicId_VersionNumber",
                table: "OfficialReportGenerations",
                columns: new[] { "MunicipalityId", "GenerationFamilyPublicId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_MunicipalityId_MunicipalityFinancialYearId_ReportingPeriodId_GeneratedAt",
                table: "OfficialReportGenerations",
                columns: new[] { "MunicipalityId", "MunicipalityFinancialYearId", "ReportingPeriodId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_PublicId",
                table: "OfficialReportGenerations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_ReportingPeriodId",
                table: "OfficialReportGenerations",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerations_ReportTemplateId",
                table: "OfficialReportGenerations",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportTemplates_CreatedByUserId",
                table: "OfficialReportTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportTemplates_MunicipalityFinancialYearId",
                table: "OfficialReportTemplates",
                column: "MunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportTemplates_MunicipalityId_TemplateFamilyPublicId_IsCurrent",
                table: "OfficialReportTemplates",
                columns: new[] { "MunicipalityId", "TemplateFamilyPublicId", "IsCurrent" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportTemplates_MunicipalityId_TemplateFamilyPublicId_VersionNumber",
                table: "OfficialReportTemplates",
                columns: new[] { "MunicipalityId", "TemplateFamilyPublicId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportTemplates_PreviousVersionId",
                table: "OfficialReportTemplates",
                column: "PreviousVersionId",
                unique: true,
                filter: "[PreviousVersionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportTemplates_PublicId",
                table: "OfficialReportTemplates",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficialReportGenerations");

            migrationBuilder.DropTable(
                name: "OfficialReportTemplates");
        }
    }
}

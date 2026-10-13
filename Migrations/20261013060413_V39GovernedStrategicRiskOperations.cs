using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedStrategicRiskOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "OPMS_StrategicRisks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_OPMS_StrategicRisks_Id_MunicipalityId",
                table: "OPMS_StrategicRisks",
                columns: new[] { "Id", "MunicipalityId" });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicRiskAssessments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicRiskId = table.Column<long>(type: "bigint", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InherentLikelihood = table.Column<int>(type: "int", nullable: false),
                    InherentImpact = table.Column<int>(type: "int", nullable: false),
                    ResidualLikelihood = table.Column<int>(type: "int", nullable: false),
                    ResidualImpact = table.Column<int>(type: "int", nullable: false),
                    ControlAssessment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AssessedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicRiskAssessments", x => x.Id);
                    table.CheckConstraint("CK_OPMS_StrategicRiskAssessments_InherentImpact", "[InherentImpact] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_OPMS_StrategicRiskAssessments_InherentLikelihood", "[InherentLikelihood] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_OPMS_StrategicRiskAssessments_ResidualImpact", "[ResidualImpact] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_OPMS_StrategicRiskAssessments_ResidualLikelihood", "[ResidualLikelihood] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskAssessments_AspNetUsers_AssessedByUserId",
                        column: x => x.AssessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskAssessments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskAssessments_OPMS_StrategicRisks_StrategicRiskId_MunicipalityId",
                        columns: x => new { x.StrategicRiskId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicRisks",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicRiskReviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicRiskId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NextReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicRiskReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskReviews_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskReviews_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskReviews_OPMS_StrategicRisks_StrategicRiskId_MunicipalityId",
                        columns: x => new { x.StrategicRiskId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicRisks",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicRiskTreatments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicRiskId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicRiskTreatments", x => x.Id);
                    table.CheckConstraint("CK_OPMS_StrategicRiskTreatments_Completion", "[Status] <> 'COMPLETED' OR [ProgressPercent] = 100");
                    table.CheckConstraint("CK_OPMS_StrategicRiskTreatments_Progress", "[ProgressPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_OPMS_StrategicRiskTreatments_Status", "[Status] IN ('OPEN','IN_PROGRESS','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskTreatments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskTreatments_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskTreatments_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskTreatments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRiskTreatments_OPMS_StrategicRisks_StrategicRiskId_MunicipalityId",
                        columns: x => new { x.StrategicRiskId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicRisks",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_OwnerUserId",
                table: "OPMS_StrategicRisks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskAssessments_AssessedByUserId",
                table: "OPMS_StrategicRiskAssessments",
                column: "AssessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskAssessments_MunicipalityId_StrategicRiskId_AssessedAt",
                table: "OPMS_StrategicRiskAssessments",
                columns: new[] { "MunicipalityId", "StrategicRiskId", "AssessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskAssessments_PublicId",
                table: "OPMS_StrategicRiskAssessments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskAssessments_StrategicRiskId_MunicipalityId",
                table: "OPMS_StrategicRiskAssessments",
                columns: new[] { "StrategicRiskId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskReviews_MunicipalityId_StrategicRiskId_ReviewedAt",
                table: "OPMS_StrategicRiskReviews",
                columns: new[] { "MunicipalityId", "StrategicRiskId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskReviews_PublicId",
                table: "OPMS_StrategicRiskReviews",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskReviews_ReviewedByUserId",
                table: "OPMS_StrategicRiskReviews",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskReviews_StrategicRiskId_MunicipalityId",
                table: "OPMS_StrategicRiskReviews",
                columns: new[] { "StrategicRiskId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskTreatments_CreatedByUserId",
                table: "OPMS_StrategicRiskTreatments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskTreatments_MunicipalityId_StrategicRiskId_Status_DueAt",
                table: "OPMS_StrategicRiskTreatments",
                columns: new[] { "MunicipalityId", "StrategicRiskId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskTreatments_OwnerUserId",
                table: "OPMS_StrategicRiskTreatments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskTreatments_PublicId",
                table: "OPMS_StrategicRiskTreatments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskTreatments_StrategicRiskId_MunicipalityId",
                table: "OPMS_StrategicRiskTreatments",
                columns: new[] { "StrategicRiskId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRiskTreatments_UpdatedByUserId",
                table: "OPMS_StrategicRiskTreatments",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OPMS_StrategicRisks_AspNetUsers_OwnerUserId",
                table: "OPMS_StrategicRisks",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OPMS_StrategicRisks_AspNetUsers_OwnerUserId",
                table: "OPMS_StrategicRisks");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicRiskAssessments");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicRiskReviews");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicRiskTreatments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_OPMS_StrategicRisks_Id_MunicipalityId",
                table: "OPMS_StrategicRisks");

            migrationBuilder.DropIndex(
                name: "IX_OPMS_StrategicRisks_OwnerUserId",
                table: "OPMS_StrategicRisks");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "OPMS_StrategicRisks");
        }
    }
}

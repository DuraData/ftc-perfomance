using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ImmutablePoeAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoeEvidenceAssessments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    PoeFileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssessedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoeEvidenceAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceAssessments_AspNetUsers_AssessedByUserId",
                        column: x => x.AssessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceAssessments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceAssessments_PoeFiles_PoeFileId",
                        column: x => x.PoeFileId,
                        principalTable: "PoeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceAssessments_AssessedByUserId",
                table: "PoeEvidenceAssessments",
                column: "AssessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceAssessments_MunicipalityId",
                table: "PoeEvidenceAssessments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceAssessments_PoeFileId_AssessedAt",
                table: "PoeEvidenceAssessments",
                columns: new[] { "PoeFileId", "AssessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceAssessments_PublicId",
                table: "PoeEvidenceAssessments",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoeEvidenceAssessments");
        }
    }
}

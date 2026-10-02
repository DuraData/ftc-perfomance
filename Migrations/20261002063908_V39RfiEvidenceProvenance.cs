using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39RfiEvidenceProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceRfiEvidenceLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    PerformanceRfiId = table.Column<long>(type: "bigint", nullable: false),
                    PoeFileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    LinkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceRfiEvidenceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceRfiEvidenceLinks_AspNetUsers_LinkedByUserId",
                        column: x => x.LinkedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRfiEvidenceLinks_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRfiEvidenceLinks_PerformanceRfis_PerformanceRfiId",
                        column: x => x.PerformanceRfiId,
                        principalTable: "PerformanceRfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRfiEvidenceLinks_PoeFiles_PoeFileId",
                        column: x => x.PoeFileId,
                        principalTable: "PoeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfiEvidenceLinks_LinkedByUserId",
                table: "PerformanceRfiEvidenceLinks",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfiEvidenceLinks_MunicipalityId",
                table: "PerformanceRfiEvidenceLinks",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfiEvidenceLinks_PerformanceRfiId_PoeFileId_Purpose",
                table: "PerformanceRfiEvidenceLinks",
                columns: new[] { "PerformanceRfiId", "PoeFileId", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfiEvidenceLinks_PoeFileId",
                table: "PerformanceRfiEvidenceLinks",
                column: "PoeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfiEvidenceLinks_PublicId",
                table: "PerformanceRfiEvidenceLinks",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceRfiEvidenceLinks");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ImmutablePoeReplacementProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoeEvidenceReplacements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SupersededPoeFileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReplacementPoeFileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReplacedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReplacedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoeEvidenceReplacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceReplacements_AspNetUsers_ReplacedByUserId",
                        column: x => x.ReplacedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceReplacements_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceReplacements_PoeFiles_ReplacementPoeFileId",
                        column: x => x.ReplacementPoeFileId,
                        principalTable: "PoeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeEvidenceReplacements_PoeFiles_SupersededPoeFileId",
                        column: x => x.SupersededPoeFileId,
                        principalTable: "PoeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceReplacements_MunicipalityId",
                table: "PoeEvidenceReplacements",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceReplacements_PublicId",
                table: "PoeEvidenceReplacements",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceReplacements_ReplacedByUserId",
                table: "PoeEvidenceReplacements",
                column: "ReplacedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceReplacements_ReplacementPoeFileId",
                table: "PoeEvidenceReplacements",
                column: "ReplacementPoeFileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoeEvidenceReplacements_SupersededPoeFileId",
                table: "PoeEvidenceReplacements",
                column: "SupersededPoeFileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoeEvidenceReplacements");
        }
    }
}

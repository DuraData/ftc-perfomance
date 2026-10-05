using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GlobalStrategicReferenceMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OPMS_BackToBasicPillars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_BackToBasicPillars", x => x.Id);
                    table.CheckConstraint("CK_OPMS_BackToBasicPillars_DisplayOrder", "[DisplayOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "OPMS_NationalKPAs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_NationalKPAs", x => x.Id);
                    table.CheckConstraint("CK_OPMS_NationalKPAs_DisplayOrder", "[DisplayOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "OPMS_MunicipalityBackToBasicPillars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    BackToBasicsPillarId = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_MunicipalityBackToBasicPillars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalityBackToBasicPillars_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalityBackToBasicPillars_OPMS_BackToBasicPillars_BackToBasicsPillarId",
                        column: x => x.BackToBasicsPillarId,
                        principalTable: "OPMS_BackToBasicPillars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_MunicipalityNationalKPAs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    NationalKpaId = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_MunicipalityNationalKPAs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalityNationalKPAs_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalityNationalKPAs_OPMS_NationalKPAs_NationalKpaId",
                        column: x => x.NationalKpaId,
                        principalTable: "OPMS_NationalKPAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BackToBasicPillars_Code",
                table: "OPMS_BackToBasicPillars",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BackToBasicPillars_PublicId",
                table: "OPMS_BackToBasicPillars",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalityBackToBasicPillars_BackToBasicsPillarId",
                table: "OPMS_MunicipalityBackToBasicPillars",
                column: "BackToBasicsPillarId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalityBackToBasicPillars_MunicipalityId_BackToBasicsPillarId",
                table: "OPMS_MunicipalityBackToBasicPillars",
                columns: new[] { "MunicipalityId", "BackToBasicsPillarId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalityBackToBasicPillars_PublicId",
                table: "OPMS_MunicipalityBackToBasicPillars",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalityNationalKPAs_MunicipalityId_NationalKpaId",
                table: "OPMS_MunicipalityNationalKPAs",
                columns: new[] { "MunicipalityId", "NationalKpaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalityNationalKPAs_NationalKpaId",
                table: "OPMS_MunicipalityNationalKPAs",
                column: "NationalKpaId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalityNationalKPAs_PublicId",
                table: "OPMS_MunicipalityNationalKPAs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_NationalKPAs_Code",
                table: "OPMS_NationalKPAs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_NationalKPAs_PublicId",
                table: "OPMS_NationalKPAs",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OPMS_MunicipalityBackToBasicPillars");

            migrationBuilder.DropTable(
                name: "OPMS_MunicipalityNationalKPAs");

            migrationBuilder.DropTable(
                name: "OPMS_BackToBasicPillars");

            migrationBuilder.DropTable(
                name: "OPMS_NationalKPAs");
        }
    }
}

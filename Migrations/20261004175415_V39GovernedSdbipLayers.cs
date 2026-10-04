using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedSdbipLayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SdbipLayerId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MunicipalityFinancialYears_Id_MunicipalityId",
                table: "MunicipalityFinancialYears",
                columns: new[] { "Id", "MunicipalityId" });

            migrationBuilder.CreateTable(
                name: "SdbipLayers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdbipLayers", x => x.Id);
                    table.UniqueConstraint("AK_SdbipLayers_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_SdbipLayers_DisplayOrder", "[DisplayOrder] > 0");
                    table.ForeignKey(
                        name: "FK_SdbipLayers_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SdbipLayers_MunicipalityFinancialYears_MunicipalityFinancialYearId_MunicipalityId",
                        columns: x => new { x.MunicipalityFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_SdbipLayerId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "SdbipLayerId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SdbipLayers_MunicipalityFinancialYearId_Code",
                table: "SdbipLayers",
                columns: new[] { "MunicipalityFinancialYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SdbipLayers_MunicipalityFinancialYearId_MunicipalityId",
                table: "SdbipLayers",
                columns: new[] { "MunicipalityFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SdbipLayers_MunicipalityId_IsActive_DisplayOrder",
                table: "SdbipLayers",
                columns: new[] { "MunicipalityId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SdbipLayers_PublicId",
                table: "SdbipLayers",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_SdbipLayers_SdbipLayerId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "SdbipLayerId", "MunicipalityId" },
                principalTable: "SdbipLayers",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_SdbipLayers_SdbipLayerId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropTable(
                name: "SdbipLayers");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_SdbipLayerId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MunicipalityFinancialYears_Id_MunicipalityId",
                table: "MunicipalityFinancialYears");

            migrationBuilder.DropColumn(
                name: "SdbipLayerId",
                table: "OpmsTargets");
        }
    }
}

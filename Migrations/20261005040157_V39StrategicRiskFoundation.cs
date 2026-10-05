using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39StrategicRiskFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OPMS_StrategicRisks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    RiskReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RiskTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RiskDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromMunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToMunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRisks_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRisks_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRisks_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRisks_MunicipalityFinancialYears_EffectiveFromMunicipalityFinancialYearId",
                        column: x => x.EffectiveFromMunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicRisks_MunicipalityFinancialYears_EffectiveToMunicipalityFinancialYearId",
                        column: x => x.EffectiveToMunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_KPIStrategicRisks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategicRiskId = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LinkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UnlinkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UnlinkedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlinkReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_KPIStrategicRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIStrategicRisks_AspNetUsers_LinkedByUserId",
                        column: x => x.LinkedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIStrategicRisks_AspNetUsers_UnlinkedByUserId",
                        column: x => x.UnlinkedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIStrategicRisks_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIStrategicRisks_OPMS_StrategicRisks_StrategicRiskId",
                        column: x => x.StrategicRiskId,
                        principalTable: "OPMS_StrategicRisks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIStrategicRisks_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_LinkedByUserId",
                table: "OPMS_KPIStrategicRisks",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_MunicipalityId",
                table: "OPMS_KPIStrategicRisks",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_OpmsTargetId_IsPrimary",
                table: "OPMS_KPIStrategicRisks",
                columns: new[] { "OpmsTargetId", "IsPrimary" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_OpmsTargetId_StrategicRiskId",
                table: "OPMS_KPIStrategicRisks",
                columns: new[] { "OpmsTargetId", "StrategicRiskId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_PublicId",
                table: "OPMS_KPIStrategicRisks",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_StrategicRiskId",
                table: "OPMS_KPIStrategicRisks",
                column: "StrategicRiskId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIStrategicRisks_UnlinkedByUserId",
                table: "OPMS_KPIStrategicRisks",
                column: "UnlinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_CreatedByUserId",
                table: "OPMS_StrategicRisks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_EffectiveFromMunicipalityFinancialYearId",
                table: "OPMS_StrategicRisks",
                column: "EffectiveFromMunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_EffectiveToMunicipalityFinancialYearId",
                table: "OPMS_StrategicRisks",
                column: "EffectiveToMunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_MunicipalityId_RiskReference",
                table: "OPMS_StrategicRisks",
                columns: new[] { "MunicipalityId", "RiskReference" },
                unique: true,
                filter: "[RiskReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_PublicId",
                table: "OPMS_StrategicRisks",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicRisks_UpdatedByUserId",
                table: "OPMS_StrategicRisks",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OPMS_KPIStrategicRisks");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicRisks");
        }
    }
}

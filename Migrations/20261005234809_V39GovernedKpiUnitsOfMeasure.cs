using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedKpiUnitsOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "KpiUnitOfMeasureMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KpiUnitOfMeasureMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OPMS_KpiUnitOfMeasures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_KpiUnitOfMeasures", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_KpiUnitOfMeasures_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_KpiUnitOfMeasures_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_KpiUnitOfMeasures_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KpiUnitOfMeasures_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KpiUnitOfMeasures_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [OPMS_KpiUnitOfMeasures]
                    ([PublicId], [MunicipalityId], [Code], [Name], [Description], [Symbol], [DisplayOrder], [IsActive])
                SELECT NEWID(), municipalities.[Id],
                    CASE WHEN NULLIF(LTRIM(RTRIM(units.[Code])), '') IS NULL
                        THEN CONCAT('LEGACY-UOM-', units.[Id])
                        ELSE LEFT(LTRIM(RTRIM(units.[Code])), 80) END,
                    LEFT(LTRIM(RTRIM(units.[Name])), 500),
                    'Migrated from the legacy UnitOfMeasures master.',
                    LEFT(NULLIF(LTRIM(RTRIM(units.[Symbol])), ''), 40),
                    units.[Id], units.[IsActive]
                FROM [Municipalities] municipalities
                CROSS JOIN [UnitOfMeasures] units;

                UPDATE targets
                SET [KpiUnitOfMeasureMasterId] = governed.[Id]
                FROM [OpmsTargets] targets
                INNER JOIN [OPMS_KpiUnitOfMeasures] governed
                    ON governed.[MunicipalityId] = targets.[MunicipalityId]
                    AND governed.[DisplayOrder] = targets.[UnitOfMeasureId]
                WHERE targets.[UnitOfMeasureId] IS NOT NULL;

                UPDATE targets
                SET [KpiUnitOfMeasureMasterId] = governed.[Id]
                FROM [IpmsTargets] targets
                INNER JOIN [OPMS_KpiUnitOfMeasures] governed
                    ON governed.[MunicipalityId] = targets.[MunicipalityId]
                    AND governed.[DisplayOrder] = targets.[UnitOfMeasureId]
                WHERE targets.[UnitOfMeasureId] IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "KpiUnitOfMeasureMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "KpiUnitOfMeasureMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KpiUnitOfMeasures_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_KpiUnitOfMeasures",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KpiUnitOfMeasures_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_KpiUnitOfMeasures",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KpiUnitOfMeasures_MunicipalityId_Code",
                table: "OPMS_KpiUnitOfMeasures",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KpiUnitOfMeasures_PublicId",
                table: "OPMS_KpiUnitOfMeasures",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_KpiUnitOfMeasures_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "KpiUnitOfMeasureMasterId", "MunicipalityId" },
                principalTable: "OPMS_KpiUnitOfMeasures",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_KpiUnitOfMeasures_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "KpiUnitOfMeasureMasterId", "MunicipalityId" },
                principalTable: "OPMS_KpiUnitOfMeasures",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_KpiUnitOfMeasures_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_KpiUnitOfMeasures_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropTable(
                name: "OPMS_KpiUnitOfMeasures");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_KpiUnitOfMeasureMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "KpiUnitOfMeasureMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "KpiUnitOfMeasureMasterId",
                table: "IpmsTargets");
        }
    }
}

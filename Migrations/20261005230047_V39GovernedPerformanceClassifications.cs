using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedPerformanceClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FunctionalAreaMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IndicatorTypeMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KpiTypeMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StandardClassificationMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FunctionalAreaMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IndicatorTypeMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "KpiTypeMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OPMS_FunctionalAreas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_OPMS_FunctionalAreas", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_FunctionalAreas_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_FunctionalAreas_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_FunctionalAreas_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_FunctionalAreas_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_FunctionalAreas_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_IndicatorTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_OPMS_IndicatorTypes", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_IndicatorTypes_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_IndicatorTypes_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_IndicatorTypes_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_IndicatorTypes_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_IndicatorTypes_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_KPITypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_OPMS_KPITypes", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_KPITypes_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_KPITypes_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_KPITypes_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPITypes_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPITypes_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StandardClassifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_OPMS_StandardClassifications", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_StandardClassifications_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_StandardClassifications_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_StandardClassifications_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StandardClassifications_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StandardClassifications_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_FunctionalAreaMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "FunctionalAreaMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_IndicatorTypeMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "IndicatorTypeMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_KpiTypeMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "KpiTypeMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_StandardClassificationMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StandardClassificationMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_FunctionalAreaMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "FunctionalAreaMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_IndicatorTypeMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "IndicatorTypeMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_KpiTypeMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "KpiTypeMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_FunctionalAreas_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_FunctionalAreas",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_FunctionalAreas_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_FunctionalAreas",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_FunctionalAreas_MunicipalityId_Code",
                table: "OPMS_FunctionalAreas",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_FunctionalAreas_PublicId",
                table: "OPMS_FunctionalAreas",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_IndicatorTypes_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_IndicatorTypes",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_IndicatorTypes_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_IndicatorTypes",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_IndicatorTypes_MunicipalityId_Code",
                table: "OPMS_IndicatorTypes",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_IndicatorTypes_PublicId",
                table: "OPMS_IndicatorTypes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPITypes_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_KPITypes",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPITypes_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_KPITypes",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPITypes_MunicipalityId_Code",
                table: "OPMS_KPITypes",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPITypes_PublicId",
                table: "OPMS_KPITypes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StandardClassifications_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_StandardClassifications",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StandardClassifications_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_StandardClassifications",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StandardClassifications_MunicipalityId_Code",
                table: "OPMS_StandardClassifications",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StandardClassifications_PublicId",
                table: "OPMS_StandardClassifications",
                column: "PublicId",
                unique: true);

            migrationBuilder.Sql("""
                WITH ValuesToSeed AS (
                    SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(KpiType)) AS [Name] FROM OpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(KpiType)), '') IS NOT NULL
                    UNION SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(KpiType)) FROM IpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(KpiType)), '') IS NOT NULL
                )
                INSERT INTO OPMS_KPITypes (PublicId, MunicipalityId, Code, Name, DisplayOrder, IsActive)
                SELECT NEWID(), MunicipalityId, LEFT(UPPER(REPLACE(REPLACE([Name], ' ', '_'), '-', '_')), 60) + '_' + LEFT(CONVERT(varchar(64), HASHBYTES('SHA2_256', [Name]), 2), 8), [Name], ROW_NUMBER() OVER (PARTITION BY MunicipalityId ORDER BY [Name]) * 10, 1 FROM ValuesToSeed;

                WITH ValuesToSeed AS (
                    SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(IndicatorType)) AS [Name] FROM OpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(IndicatorType)), '') IS NOT NULL
                    UNION SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(IndicatorType)) FROM IpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(IndicatorType)), '') IS NOT NULL
                )
                INSERT INTO OPMS_IndicatorTypes (PublicId, MunicipalityId, Code, Name, DisplayOrder, IsActive)
                SELECT NEWID(), MunicipalityId, LEFT(UPPER(REPLACE(REPLACE([Name], ' ', '_'), '-', '_')), 60) + '_' + LEFT(CONVERT(varchar(64), HASHBYTES('SHA2_256', [Name]), 2), 8), [Name], ROW_NUMBER() OVER (PARTITION BY MunicipalityId ORDER BY [Name]) * 10, 1 FROM ValuesToSeed;

                WITH ValuesToSeed AS (
                    SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(FunctionalArea)) AS [Name] FROM OpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(FunctionalArea)), '') IS NOT NULL
                    UNION SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(FunctionalArea)) FROM IpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(FunctionalArea)), '') IS NOT NULL
                )
                INSERT INTO OPMS_FunctionalAreas (PublicId, MunicipalityId, Code, Name, DisplayOrder, IsActive)
                SELECT NEWID(), MunicipalityId, LEFT(UPPER(REPLACE(REPLACE([Name], ' ', '_'), '-', '_')), 60) + '_' + LEFT(CONVERT(varchar(64), HASHBYTES('SHA2_256', [Name]), 2), 8), [Name], ROW_NUMBER() OVER (PARTITION BY MunicipalityId ORDER BY [Name]) * 10, 1 FROM ValuesToSeed;

                WITH ValuesToSeed AS (
                    SELECT DISTINCT MunicipalityId, LTRIM(RTRIM(StandardClassification)) AS [Name] FROM OpmsTargets WHERE MunicipalityId IS NOT NULL AND NULLIF(LTRIM(RTRIM(StandardClassification)), '') IS NOT NULL
                )
                INSERT INTO OPMS_StandardClassifications (PublicId, MunicipalityId, Code, Name, DisplayOrder, IsActive)
                SELECT NEWID(), MunicipalityId, LEFT(UPPER(REPLACE(REPLACE([Name], ' ', '_'), '-', '_')), 60) + '_' + LEFT(CONVERT(varchar(64), HASHBYTES('SHA2_256', [Name]), 2), 8), [Name], ROW_NUMBER() OVER (PARTITION BY MunicipalityId ORDER BY [Name]) * 10, 1 FROM ValuesToSeed;

                UPDATE target SET KpiTypeMasterId = master.Id FROM OpmsTargets target INNER JOIN OPMS_KPITypes master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.KpiType));
                UPDATE target SET IndicatorTypeMasterId = master.Id FROM OpmsTargets target INNER JOIN OPMS_IndicatorTypes master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.IndicatorType));
                UPDATE target SET FunctionalAreaMasterId = master.Id FROM OpmsTargets target INNER JOIN OPMS_FunctionalAreas master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.FunctionalArea));
                UPDATE target SET StandardClassificationMasterId = master.Id FROM OpmsTargets target INNER JOIN OPMS_StandardClassifications master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.StandardClassification));
                UPDATE target SET KpiTypeMasterId = master.Id FROM IpmsTargets target INNER JOIN OPMS_KPITypes master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.KpiType));
                UPDATE target SET IndicatorTypeMasterId = master.Id FROM IpmsTargets target INNER JOIN OPMS_IndicatorTypes master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.IndicatorType));
                UPDATE target SET FunctionalAreaMasterId = master.Id FROM IpmsTargets target INNER JOIN OPMS_FunctionalAreas master ON master.MunicipalityId = target.MunicipalityId AND master.Name = LTRIM(RTRIM(target.FunctionalArea));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_FunctionalAreas_FunctionalAreaMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "FunctionalAreaMasterId", "MunicipalityId" },
                principalTable: "OPMS_FunctionalAreas",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_IndicatorTypes_IndicatorTypeMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "IndicatorTypeMasterId", "MunicipalityId" },
                principalTable: "OPMS_IndicatorTypes",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_KPITypes_KpiTypeMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "KpiTypeMasterId", "MunicipalityId" },
                principalTable: "OPMS_KPITypes",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_FunctionalAreas_FunctionalAreaMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "FunctionalAreaMasterId", "MunicipalityId" },
                principalTable: "OPMS_FunctionalAreas",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_IndicatorTypes_IndicatorTypeMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "IndicatorTypeMasterId", "MunicipalityId" },
                principalTable: "OPMS_IndicatorTypes",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_KPITypes_KpiTypeMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "KpiTypeMasterId", "MunicipalityId" },
                principalTable: "OPMS_KPITypes",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_StandardClassifications_StandardClassificationMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StandardClassificationMasterId", "MunicipalityId" },
                principalTable: "OPMS_StandardClassifications",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_FunctionalAreas_FunctionalAreaMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_IndicatorTypes_IndicatorTypeMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_KPITypes_KpiTypeMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_FunctionalAreas_FunctionalAreaMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_IndicatorTypes_IndicatorTypeMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_KPITypes_KpiTypeMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_StandardClassifications_StandardClassificationMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropTable(
                name: "OPMS_FunctionalAreas");

            migrationBuilder.DropTable(
                name: "OPMS_IndicatorTypes");

            migrationBuilder.DropTable(
                name: "OPMS_KPITypes");

            migrationBuilder.DropTable(
                name: "OPMS_StandardClassifications");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_FunctionalAreaMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_IndicatorTypeMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_KpiTypeMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_StandardClassificationMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_FunctionalAreaMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_IndicatorTypeMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_KpiTypeMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "FunctionalAreaMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "IndicatorTypeMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "KpiTypeMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "StandardClassificationMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "FunctionalAreaMasterId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "IndicatorTypeMasterId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "KpiTypeMasterId",
                table: "IpmsTargets");
        }
    }
}

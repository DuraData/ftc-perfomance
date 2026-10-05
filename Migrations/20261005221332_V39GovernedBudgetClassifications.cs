using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedBudgetClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BudgetTypeMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BudgetTypeMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OPMS_BudgetSources",
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
                    table.PrimaryKey("PK_OPMS_BudgetSources", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_BudgetSources_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_BudgetSources_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_BudgetSources_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_BudgetSources_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_BudgetSources_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_BudgetTypes",
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
                    table.PrimaryKey("PK_OPMS_BudgetTypes", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_BudgetTypes_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_BudgetTypes_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_BudgetTypes_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_BudgetTypes_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_BudgetTypes_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IPMS_KPIBudgetSources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    BudgetSourceId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPMS_KPIBudgetSources", x => x.Id);
                    table.CheckConstraint("CK_IPMS_KPIBudgetSources_Amount", "[Amount] IS NULL OR [Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_IPMS_KPIBudgetSources_IpmsTargets_IpmsTargetId",
                        column: x => x.IpmsTargetId,
                        principalTable: "IpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IPMS_KPIBudgetSources_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IPMS_KPIBudgetSources_OPMS_BudgetSources_BudgetSourceId_MunicipalityId",
                        columns: x => new { x.BudgetSourceId, x.MunicipalityId },
                        principalTable: "OPMS_BudgetSources",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_KPIBudgetSources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    BudgetSourceId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_KPIBudgetSources", x => x.Id);
                    table.CheckConstraint("CK_OPMS_KPIBudgetSources_Amount", "[Amount] IS NULL OR [Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_KPIBudgetSources_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIBudgetSources_OPMS_BudgetSources_BudgetSourceId_MunicipalityId",
                        columns: x => new { x.BudgetSourceId, x.MunicipalityId },
                        principalTable: "OPMS_BudgetSources",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_KPIBudgetSources_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [OPMS_BudgetSources] ([PublicId], [MunicipalityId], [Code], [Name], [Description], [EffectiveFromFinancialYearId], [EffectiveToFinancialYearId], [DisplayOrder], [IsActive])
                SELECT NEWID(), source.[MunicipalityId], source.[Code], source.[Name], NULL, NULL, NULL,
                       ROW_NUMBER() OVER (PARTITION BY source.[MunicipalityId] ORDER BY source.[Code]) * 10, 1
                FROM (
                    SELECT DISTINCT target.[MunicipalityId], legacy.[Code], legacy.[Name]
                    FROM [OpmsTargets] target INNER JOIN [BudgetSources] legacy ON legacy.[Id] = target.[BudgetSourceId]
                    WHERE target.[MunicipalityId] IS NOT NULL
                    UNION
                    SELECT DISTINCT target.[MunicipalityId], legacy.[Code], legacy.[Name]
                    FROM [IpmsTargets] target INNER JOIN [BudgetSources] legacy ON legacy.[Id] = target.[BudgetSourceId]
                    WHERE target.[MunicipalityId] IS NOT NULL
                ) source
                WHERE NOT EXISTS (
                    SELECT 1 FROM [OPMS_BudgetSources] governed
                    WHERE governed.[MunicipalityId] = source.[MunicipalityId] AND governed.[Code] = source.[Code]
                );

                INSERT INTO [OPMS_BudgetTypes] ([PublicId], [MunicipalityId], [Code], [Name], [Description], [EffectiveFromFinancialYearId], [EffectiveToFinancialYearId], [DisplayOrder], [IsActive])
                SELECT NEWID(), source.[MunicipalityId], source.[Code], source.[Name], NULL, NULL, NULL,
                       ROW_NUMBER() OVER (PARTITION BY source.[MunicipalityId] ORDER BY source.[Code]) * 10, 1
                FROM (
                    SELECT DISTINCT target.[MunicipalityId], legacy.[Code], legacy.[Name]
                    FROM [OpmsTargets] target INNER JOIN [BudgetTypes] legacy ON legacy.[Id] = target.[BudgetTypeId]
                    WHERE target.[MunicipalityId] IS NOT NULL
                    UNION
                    SELECT DISTINCT target.[MunicipalityId], legacy.[Code], legacy.[Name]
                    FROM [IpmsTargets] target INNER JOIN [BudgetTypes] legacy ON legacy.[Id] = target.[BudgetTypeId]
                    WHERE target.[MunicipalityId] IS NOT NULL
                ) source
                WHERE NOT EXISTS (
                    SELECT 1 FROM [OPMS_BudgetTypes] governed
                    WHERE governed.[MunicipalityId] = source.[MunicipalityId] AND governed.[Code] = source.[Code]
                );

                UPDATE target SET [BudgetTypeMasterId] = governed.[Id]
                FROM [OpmsTargets] target
                INNER JOIN [BudgetTypes] legacy ON legacy.[Id] = target.[BudgetTypeId]
                INNER JOIN [OPMS_BudgetTypes] governed ON governed.[MunicipalityId] = target.[MunicipalityId] AND governed.[Code] = legacy.[Code]
                WHERE target.[BudgetTypeId] IS NOT NULL AND target.[MunicipalityId] IS NOT NULL;

                UPDATE target SET [BudgetTypeMasterId] = governed.[Id]
                FROM [IpmsTargets] target
                INNER JOIN [BudgetTypes] legacy ON legacy.[Id] = target.[BudgetTypeId]
                INNER JOIN [OPMS_BudgetTypes] governed ON governed.[MunicipalityId] = target.[MunicipalityId] AND governed.[Code] = legacy.[Code]
                WHERE target.[BudgetTypeId] IS NOT NULL AND target.[MunicipalityId] IS NOT NULL;

                INSERT INTO [OPMS_KPIBudgetSources] ([PublicId], [OpmsTargetId], [MunicipalityId], [BudgetSourceId], [Amount], [IsActive], [CreatedAt])
                SELECT NEWID(), target.[Id], target.[MunicipalityId], governed.[Id], NULL, 1, SYSUTCDATETIME()
                FROM [OpmsTargets] target
                INNER JOIN [BudgetSources] legacy ON legacy.[Id] = target.[BudgetSourceId]
                INNER JOIN [OPMS_BudgetSources] governed ON governed.[MunicipalityId] = target.[MunicipalityId] AND governed.[Code] = legacy.[Code]
                WHERE target.[BudgetSourceId] IS NOT NULL AND target.[MunicipalityId] IS NOT NULL;

                INSERT INTO [IPMS_KPIBudgetSources] ([PublicId], [IpmsTargetId], [MunicipalityId], [BudgetSourceId], [Amount], [IsActive], [CreatedAt])
                SELECT NEWID(), target.[Id], target.[MunicipalityId], governed.[Id], NULL, 1, SYSUTCDATETIME()
                FROM [IpmsTargets] target
                INNER JOIN [BudgetSources] legacy ON legacy.[Id] = target.[BudgetSourceId]
                INNER JOIN [OPMS_BudgetSources] governed ON governed.[MunicipalityId] = target.[MunicipalityId] AND governed.[Code] = legacy.[Code]
                WHERE target.[BudgetSourceId] IS NOT NULL AND target.[MunicipalityId] IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_BudgetTypeMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "BudgetTypeMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_BudgetTypeMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "BudgetTypeMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_KPIBudgetSources_BudgetSourceId_MunicipalityId",
                table: "IPMS_KPIBudgetSources",
                columns: new[] { "BudgetSourceId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_KPIBudgetSources_IpmsTargetId_BudgetSourceId",
                table: "IPMS_KPIBudgetSources",
                columns: new[] { "IpmsTargetId", "BudgetSourceId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_KPIBudgetSources_MunicipalityId",
                table: "IPMS_KPIBudgetSources",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_KPIBudgetSources_PublicId",
                table: "IPMS_KPIBudgetSources",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetSources_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_BudgetSources",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetSources_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_BudgetSources",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetSources_MunicipalityId_Code",
                table: "OPMS_BudgetSources",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetSources_PublicId",
                table: "OPMS_BudgetSources",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetTypes_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_BudgetTypes",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetTypes_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_BudgetTypes",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetTypes_MunicipalityId_Code",
                table: "OPMS_BudgetTypes",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_BudgetTypes_PublicId",
                table: "OPMS_BudgetTypes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIBudgetSources_BudgetSourceId_MunicipalityId",
                table: "OPMS_KPIBudgetSources",
                columns: new[] { "BudgetSourceId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIBudgetSources_MunicipalityId",
                table: "OPMS_KPIBudgetSources",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIBudgetSources_OpmsTargetId_BudgetSourceId",
                table: "OPMS_KPIBudgetSources",
                columns: new[] { "OpmsTargetId", "BudgetSourceId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIBudgetSources_PublicId",
                table: "OPMS_KPIBudgetSources",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_BudgetTypes_BudgetTypeMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "BudgetTypeMasterId", "MunicipalityId" },
                principalTable: "OPMS_BudgetTypes",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_BudgetTypes_BudgetTypeMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "BudgetTypeMasterId", "MunicipalityId" },
                principalTable: "OPMS_BudgetTypes",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_BudgetTypes_BudgetTypeMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_BudgetTypes_BudgetTypeMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropTable(
                name: "IPMS_KPIBudgetSources");

            migrationBuilder.DropTable(
                name: "OPMS_BudgetTypes");

            migrationBuilder.DropTable(
                name: "OPMS_KPIBudgetSources");

            migrationBuilder.DropTable(
                name: "OPMS_BudgetSources");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_BudgetTypeMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_BudgetTypeMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "BudgetTypeMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "BudgetTypeMasterId",
                table: "IpmsTargets");
        }
    }
}

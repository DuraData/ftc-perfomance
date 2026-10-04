using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39NormalizedOfficialReportGenerationScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ScopeIsUnrestricted",
                table: "OfficialReportGenerations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ScopeSchemaVersion",
                table: "OfficialReportGenerations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OfficialReportGenerationScopeGrants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    OfficialReportGenerationId = table.Column<long>(type: "bigint", nullable: false),
                    Dimension = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialReportGenerationScopeGrants", x => x.Id);
                    table.CheckConstraint("CK_OfficialReportGenerationScopeGrants_Dimension", "[Dimension] >= 1 AND [Dimension] <= 4");
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerationScopeGrants_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportGenerationScopeGrants_OfficialReportGenerations_OfficialReportGenerationId",
                        column: x => x.OfficialReportGenerationId,
                        principalTable: "OfficialReportGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [OfficialReportGenerationScopeGrants] ([PublicId], [MunicipalityId], [OfficialReportGenerationId], [Dimension], [Value])
                SELECT NEWID(), source.[MunicipalityId], source.[Id], 1, source.[Value]
                FROM (
                    SELECT DISTINCT generation.[MunicipalityId], generation.[Id], CONVERT(nvarchar(450), TRY_CONVERT(int, value.[value])) AS [Value]
                    FROM [OfficialReportGenerations] AS generation
                    CROSS APPLY OPENJSON(CASE WHEN ISJSON(generation.[ScopeJson]) = 1 THEN generation.[ScopeJson] ELSE N'{}' END, '$.DepartmentIds') AS value
                    WHERE TRY_CONVERT(int, value.[value]) IS NOT NULL
                ) AS source;

                INSERT INTO [OfficialReportGenerationScopeGrants] ([PublicId], [MunicipalityId], [OfficialReportGenerationId], [Dimension], [Value])
                SELECT NEWID(), source.[MunicipalityId], source.[Id], 2, source.[Value]
                FROM (
                    SELECT DISTINCT generation.[MunicipalityId], generation.[Id], CONVERT(nvarchar(450), TRY_CONVERT(int, value.[value])) AS [Value]
                    FROM [OfficialReportGenerations] AS generation
                    CROSS APPLY OPENJSON(CASE WHEN ISJSON(generation.[ScopeJson]) = 1 THEN generation.[ScopeJson] ELSE N'{}' END, '$.UnitIds') AS value
                    WHERE TRY_CONVERT(int, value.[value]) IS NOT NULL
                ) AS source;

                INSERT INTO [OfficialReportGenerationScopeGrants] ([PublicId], [MunicipalityId], [OfficialReportGenerationId], [Dimension], [Value])
                SELECT NEWID(), source.[MunicipalityId], source.[Id], 3, source.[Value]
                FROM (
                    SELECT DISTINCT generation.[MunicipalityId], generation.[Id], UPPER(LTRIM(RTRIM(CONVERT(nvarchar(450), value.[value])))) AS [Value]
                    FROM [OfficialReportGenerations] AS generation
                    CROSS APPLY OPENJSON(CASE WHEN ISJSON(generation.[ScopeJson]) = 1 THEN generation.[ScopeJson] ELSE N'{}' END, '$.OwnerUserIds') AS value
                    WHERE LEN(LTRIM(RTRIM(CONVERT(nvarchar(450), value.[value])))) > 0
                ) AS source;

                INSERT INTO [OfficialReportGenerationScopeGrants] ([PublicId], [MunicipalityId], [OfficialReportGenerationId], [Dimension], [Value])
                SELECT NEWID(), source.[MunicipalityId], source.[Id], 4, source.[Value]
                FROM (
                    SELECT DISTINCT generation.[MunicipalityId], generation.[Id], UPPER(LTRIM(RTRIM(CONVERT(nvarchar(450), value.[value])))) AS [Value]
                    FROM [OfficialReportGenerations] AS generation
                    CROSS APPLY OPENJSON(CASE WHEN ISJSON(generation.[ScopeJson]) = 1 THEN generation.[ScopeJson] ELSE N'{}' END, '$.TargetIds') AS value
                    WHERE LEN(LTRIM(RTRIM(CONVERT(nvarchar(450), value.[value])))) > 0
                ) AS source;

                UPDATE [OfficialReportGenerations]
                SET [ScopeIsUnrestricted] = CASE WHEN JSON_VALUE(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END, '$.Unrestricted') = N'true' THEN 1 ELSE 0 END,
                    [ScopeSchemaVersion] = 1
                WHERE ISJSON([ScopeJson]) = 1
                  AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END) WHERE [key] = N'Unrestricted' AND [type] = 3)
                  AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END) WHERE [key] = N'DepartmentIds' AND [type] = 4)
                  AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END) WHERE [key] = N'UnitIds' AND [type] = 4)
                  AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END) WHERE [key] = N'OwnerUserIds' AND [type] = 4)
                  AND EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END) WHERE [key] = N'TargetIds' AND [type] = 4)
                  AND NOT EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END, '$.DepartmentIds') WHERE TRY_CONVERT(int, [value]) IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END, '$.UnitIds') WHERE TRY_CONVERT(int, [value]) IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END, '$.OwnerUserIds') WHERE [type] <> 1 OR LEN(LTRIM(RTRIM(CONVERT(nvarchar(max), [value])))) NOT BETWEEN 1 AND 450)
                  AND NOT EXISTS (SELECT 1 FROM OPENJSON(CASE WHEN ISJSON([ScopeJson]) = 1 THEN [ScopeJson] ELSE N'{}' END, '$.TargetIds') WHERE [type] <> 1 OR LEN(LTRIM(RTRIM(CONVERT(nvarchar(max), [value])))) NOT BETWEEN 1 AND 450);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OfficialReportGenerations_ScopeSchema",
                table: "OfficialReportGenerations",
                sql: "[ScopeSchemaVersion] >= 0 AND [ScopeSchemaVersion] <= 1");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerationScopeGrants_MunicipalityId_OfficialReportGenerationId_Dimension_Value",
                table: "OfficialReportGenerationScopeGrants",
                columns: new[] { "MunicipalityId", "OfficialReportGenerationId", "Dimension", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerationScopeGrants_OfficialReportGenerationId",
                table: "OfficialReportGenerationScopeGrants",
                column: "OfficialReportGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportGenerationScopeGrants_PublicId",
                table: "OfficialReportGenerationScopeGrants",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficialReportGenerationScopeGrants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OfficialReportGenerations_ScopeSchema",
                table: "OfficialReportGenerations");

            migrationBuilder.DropColumn(
                name: "ScopeIsUnrestricted",
                table: "OfficialReportGenerations");

            migrationBuilder.DropColumn(
                name: "ScopeSchemaVersion",
                table: "OfficialReportGenerations");
        }
    }
}

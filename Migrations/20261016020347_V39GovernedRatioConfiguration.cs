using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedRatioConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RatioComparisonMethods",
                columns: table => new
                {
                    Id = table.Column<long>(type: ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ? "INTEGER" : "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SupportsAutoCalculation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatioComparisonMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceRatioDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ? "INTEGER" : "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    PerformancePeriodTargetId = table.Column<long>(type: "bigint", nullable: false),
                    RatioSegmentCount = table.Column<byte>(type: "tinyint", nullable: false),
                    RatioComparisonMethodId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceRatioDefinitions", x => x.Id);
                    table.CheckConstraint("CK_PerformanceRatioDefinitions_SegmentCount", "[RatioSegmentCount] BETWEEN 2 AND 4");
                    table.ForeignKey(
                        name: "FK_PerformanceRatioDefinitions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRatioDefinitions_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRatioDefinitions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRatioDefinitions_PerformancePeriodTargets_PerformancePeriodTargetId",
                        column: x => x.PerformancePeriodTargetId,
                        principalTable: "PerformancePeriodTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRatioDefinitions_RatioComparisonMethods_RatioComparisonMethodId",
                        column: x => x.RatioComparisonMethodId,
                        principalTable: "RatioComparisonMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceRatioSegments",
                columns: table => new
                {
                    Id = table.Column<long>(type: ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ? "INTEGER" : "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    RatioDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    SegmentPosition = table.Column<byte>(type: "tinyint", nullable: false),
                    SegmentLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "tinyint", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    UnitDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceRatioSegments", x => x.Id);
                    table.CheckConstraint("CK_PerformanceRatioSegments_DecimalPlaces", "[DecimalPlaces] IS NULL OR [DecimalPlaces] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_PerformanceRatioSegments_Position", "[SegmentPosition] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_PerformanceRatioSegments_Range", "[MinValue] IS NULL OR [MaxValue] IS NULL OR [MinValue] <= [MaxValue]");
                    table.ForeignKey(
                        name: "FK_PerformanceRatioSegments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRatioSegments_PerformanceRatioDefinitions_RatioDefinitionId",
                        column: x => x.RatioDefinitionId,
                        principalTable: "PerformanceRatioDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "OpmsUnitDefinitions",
                keyColumn: "Id",
                keyValue: 5L,
                column: "ValueDataType",
                value: "TEXT");

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    SET IDENTITY_INSERT [RatioComparisonMethods] ON;
                    INSERT INTO [RatioComparisonMethods]
                        ([Id], [PublicId], [Code], [Name], [Description], [SupportsAutoCalculation], [IsActive])
                    VALUES
                        (1, '30000000-0000-0000-0000-000000000001', 'NORMALIZED_RATIO', 'Normalized ratio', 'Normalize a two-segment ratio and apply the performance direction.', 1, 1),
                        (2, '30000000-0000-0000-0000-000000000002', 'EXACT_MATCH', 'Exact match', 'Every actual segment must match its target segment.', 1, 1),
                        (3, '30000000-0000-0000-0000-000000000003', 'SEGMENT_BY_SEGMENT', 'Segment by segment', 'Compare configured segments independently.', 1, 1),
                        (4, '30000000-0000-0000-0000-000000000004', 'MANUAL', 'Manual', 'Do not infer automatic ratio achievement or variance.', 0, 1);
                    SET IDENTITY_INSERT [RatioComparisonMethods] OFF;
                    """);
                migrationBuilder.Sql("""
                    INSERT INTO [PerformanceRatioDefinitions]
                        ([PublicId], [MunicipalityId], [PerformancePeriodTargetId], [RatioSegmentCount], [RatioComparisonMethodId], [IsActive], [CreatedAt], [CreatedByUserId])
                    SELECT NEWID(), target.[MunicipalityId], target.[Id],
                        CAST(LEN(source.[RatioValue]) - LEN(REPLACE(source.[RatioValue], ':', '')) + 1 AS tinyint),
                        4, 1, SYSUTCDATETIME(), target.[CreatedByUserId]
                    FROM [PerformancePeriodTargets] target
                    CROSS APPLY (SELECT CASE WHEN target.[RevisedUnitKind] = 8 AND target.[RevisedTargetValue] IS NOT NULL
                        THEN target.[RevisedTargetValue] ELSE target.[TargetValue] END AS [RatioValue]) source
                    WHERE (target.[UnitKind] = 8 OR target.[RevisedUnitKind] = 8)
                      AND LEN(source.[RatioValue]) - LEN(REPLACE(source.[RatioValue], ':', '')) + 1 BETWEEN 2 AND 4
                      AND NOT EXISTS (SELECT 1 FROM [PerformanceRatioDefinitions] existing WHERE existing.[PerformancePeriodTargetId] = target.[Id]);

                    INSERT INTO [PerformanceRatioSegments]
                        ([PublicId], [MunicipalityId], [RatioDefinitionId], [SegmentPosition], [SegmentLabel], [DecimalPlaces], [MinValue], [MaxValue], [UnitDescription], [IsActive])
                    SELECT NEWID(), definition.[MunicipalityId], definition.[Id], positions.[Position],
                        CONCAT('Segment ', positions.[Position]), 6, 0, NULL, NULL, 1
                    FROM [PerformanceRatioDefinitions] definition
                    CROSS JOIN (VALUES (CAST(1 AS tinyint)), (CAST(2 AS tinyint)), (CAST(3 AS tinyint)), (CAST(4 AS tinyint))) positions([Position])
                    WHERE positions.[Position] <= definition.[RatioSegmentCount]
                      AND NOT EXISTS (SELECT 1 FROM [PerformanceRatioSegments] existing
                          WHERE existing.[RatioDefinitionId] = definition.[Id] AND existing.[SegmentPosition] = positions.[Position]);
                    """);
            }
            else
            {
                migrationBuilder.Sql("""
                    INSERT INTO "RatioComparisonMethods"
                        ("Id", "PublicId", "Code", "Name", "Description", "SupportsAutoCalculation", "IsActive", "RowVersion")
                    VALUES
                        (1, '30000000-0000-0000-0000-000000000001', 'NORMALIZED_RATIO', 'Normalized ratio', 'Normalize a two-segment ratio and apply the performance direction.', 1, 1, randomblob(16)),
                        (2, '30000000-0000-0000-0000-000000000002', 'EXACT_MATCH', 'Exact match', 'Every actual segment must match its target segment.', 1, 1, randomblob(16)),
                        (3, '30000000-0000-0000-0000-000000000003', 'SEGMENT_BY_SEGMENT', 'Segment by segment', 'Compare configured segments independently.', 1, 1, randomblob(16)),
                        (4, '30000000-0000-0000-0000-000000000004', 'MANUAL', 'Manual', 'Do not infer automatic ratio achievement or variance.', 0, 1, randomblob(16));
                    """);
                migrationBuilder.Sql("""
                    INSERT INTO "PerformanceRatioDefinitions"
                        ("PublicId", "MunicipalityId", "PerformancePeriodTargetId", "RatioSegmentCount", "RatioComparisonMethodId", "IsActive", "CreatedAt", "CreatedByUserId", "RowVersion")
                    SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-a' || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
                        target."MunicipalityId", target."Id",
                        length(CASE WHEN target."RevisedUnitKind" = 8 AND target."RevisedTargetValue" IS NOT NULL THEN target."RevisedTargetValue" ELSE target."TargetValue" END)
                            - length(replace(CASE WHEN target."RevisedUnitKind" = 8 AND target."RevisedTargetValue" IS NOT NULL THEN target."RevisedTargetValue" ELSE target."TargetValue" END, ':', '')) + 1,
                        4, 1, CURRENT_TIMESTAMP, target."CreatedByUserId", randomblob(16)
                    FROM "PerformancePeriodTargets" target
                    WHERE (target."UnitKind" = 8 OR target."RevisedUnitKind" = 8)
                      AND length(CASE WHEN target."RevisedUnitKind" = 8 AND target."RevisedTargetValue" IS NOT NULL THEN target."RevisedTargetValue" ELSE target."TargetValue" END)
                          - length(replace(CASE WHEN target."RevisedUnitKind" = 8 AND target."RevisedTargetValue" IS NOT NULL THEN target."RevisedTargetValue" ELSE target."TargetValue" END, ':', '')) + 1 BETWEEN 2 AND 4
                      AND NOT EXISTS (SELECT 1 FROM "PerformanceRatioDefinitions" existing WHERE existing."PerformancePeriodTargetId" = target."Id");
                    """);
                migrationBuilder.Sql("""
                    INSERT INTO "PerformanceRatioSegments"
                        ("PublicId", "MunicipalityId", "RatioDefinitionId", "SegmentPosition", "SegmentLabel", "DecimalPlaces", "MinValue", "MaxValue", "UnitDescription", "IsActive", "RowVersion")
                    SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-a' || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
                        definition."MunicipalityId", definition."Id", positions."Position",
                        'Segment ' || positions."Position", 6, 0, NULL, NULL, 1, randomblob(16)
                    FROM "PerformanceRatioDefinitions" definition
                    CROSS JOIN (SELECT 1 AS "Position" UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4) positions
                    WHERE positions."Position" <= definition."RatioSegmentCount"
                      AND NOT EXISTS (SELECT 1 FROM "PerformanceRatioSegments" existing
                          WHERE existing."RatioDefinitionId" = definition."Id" AND existing."SegmentPosition" = positions."Position");
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioDefinitions_CreatedByUserId",
                table: "PerformanceRatioDefinitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioDefinitions_ModifiedByUserId",
                table: "PerformanceRatioDefinitions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioDefinitions_MunicipalityId",
                table: "PerformanceRatioDefinitions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioDefinitions_PerformancePeriodTargetId",
                table: "PerformanceRatioDefinitions",
                column: "PerformancePeriodTargetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioDefinitions_PublicId",
                table: "PerformanceRatioDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioDefinitions_RatioComparisonMethodId",
                table: "PerformanceRatioDefinitions",
                column: "RatioComparisonMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioSegments_MunicipalityId",
                table: "PerformanceRatioSegments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioSegments_PublicId",
                table: "PerformanceRatioSegments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRatioSegments_RatioDefinitionId_SegmentPosition",
                table: "PerformanceRatioSegments",
                columns: new[] { "RatioDefinitionId", "SegmentPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatioComparisonMethods_Code",
                table: "RatioComparisonMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatioComparisonMethods_PublicId",
                table: "RatioComparisonMethods",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceRatioSegments");

            migrationBuilder.DropTable(
                name: "PerformanceRatioDefinitions");

            migrationBuilder.DropTable(
                name: "RatioComparisonMethods");

            migrationBuilder.UpdateData(
                table: "OpmsUnitDefinitions",
                keyColumn: "Id",
                keyValue: 5L,
                column: "ValueDataType",
                value: "JSON");
        }
    }
}

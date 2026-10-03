using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39CanonicalActualPerformanceCutover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacySubmissionValueArchives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    OpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LegacyActual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LegacyActualDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LegacyActualPerformanceDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CanonicalActualPerformance = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchiveReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacySubmissionValueArchives", x => x.Id);
                    table.CheckConstraint("CK_LegacySubmissionValueArchives_OneSubmission", "CASE WHEN [OpmsSubmissionId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsSubmissionId] IS NULL THEN 0 ELSE 1 END = 1");
                    table.ForeignKey(
                        name: "FK_LegacySubmissionValueArchives_IpmsSubmissions_IpmsSubmissionId",
                        column: x => x.IpmsSubmissionId,
                        principalTable: "IpmsSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionValueArchives_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionValueArchives_OpmsSubmissions_OpmsSubmissionId",
                        column: x => x.OpmsSubmissionId,
                        principalTable: "OpmsSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionValueArchives_IpmsSubmissionId",
                table: "LegacySubmissionValueArchives",
                column: "IpmsSubmissionId",
                unique: true,
                filter: "[IpmsSubmissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionValueArchives_MunicipalityId",
                table: "LegacySubmissionValueArchives",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionValueArchives_OpmsSubmissionId",
                table: "LegacySubmissionValueArchives",
                column: "OpmsSubmissionId",
                unique: true,
                filter: "[OpmsSubmissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionValueArchives_PublicId",
                table: "LegacySubmissionValueArchives",
                column: "PublicId",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [LegacySubmissionValueArchives]
                    ([PublicId], [MunicipalityId], [SubmissionKind], [OpmsSubmissionId], [IpmsSubmissionId], [LegacyActual], [LegacyActualDescription], [LegacyActualPerformanceDescription], [CanonicalActualPerformance], [ArchivedAt], [ArchiveReason])
                SELECT NEWID(), COALESCE(s.[MunicipalityId], t.[MunicipalityId]), 1, s.[Id], NULL,
                    s.[Actual], s.[ActualDescription], s.[ActualPerformanceDescription],
                    COALESCE(s.[ActualPerformance], CASE WHEN s.[Actual] IS NULL THEN NULL ELSE CONVERT(nvarchar(128), s.[Actual]) END),
                    SYSUTCDATETIME(), N'V3.9 canonical ActualPerformance cutover'
                FROM [OpmsSubmissions] s
                LEFT JOIN [OpmsTargets] t ON t.[Id] = s.[OpmsTargetId]
                WHERE s.[Actual] IS NOT NULL OR s.[ActualDescription] IS NOT NULL OR s.[ActualPerformanceDescription] IS NOT NULL;

                INSERT INTO [LegacySubmissionValueArchives]
                    ([PublicId], [MunicipalityId], [SubmissionKind], [OpmsSubmissionId], [IpmsSubmissionId], [LegacyActual], [LegacyActualDescription], [LegacyActualPerformanceDescription], [CanonicalActualPerformance], [ArchivedAt], [ArchiveReason])
                SELECT NEWID(), COALESCE(s.[MunicipalityId], t.[MunicipalityId]), 2, NULL, s.[Id],
                    s.[Actual], s.[ActualDescription], s.[ActualPerformanceDescription],
                    COALESCE(s.[ActualPerformance], CASE WHEN s.[Actual] IS NULL THEN NULL ELSE CONVERT(nvarchar(128), s.[Actual]) END),
                    SYSUTCDATETIME(), N'V3.9 canonical ActualPerformance cutover'
                FROM [IpmsSubmissions] s
                LEFT JOIN [IpmsTargets] t ON t.[Id] = s.[IpmsTargetId]
                WHERE s.[Actual] IS NOT NULL OR s.[ActualDescription] IS NOT NULL OR s.[ActualPerformanceDescription] IS NOT NULL;

                UPDATE [OpmsSubmissions]
                SET [ActualPerformance] = CONVERT(nvarchar(128), [Actual])
                WHERE [ActualPerformance] IS NULL AND [Actual] IS NOT NULL;

                UPDATE [IpmsSubmissions]
                SET [ActualPerformance] = CONVERT(nvarchar(128), [Actual])
                WHERE [ActualPerformance] IS NULL AND [Actual] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(name: "Actual", table: "OpmsSubmissions");
            migrationBuilder.DropColumn(name: "ActualDescription", table: "OpmsSubmissions");
            migrationBuilder.DropColumn(name: "ActualPerformanceDescription", table: "OpmsSubmissions");
            migrationBuilder.DropColumn(name: "Actual", table: "IpmsSubmissions");
            migrationBuilder.DropColumn(name: "ActualDescription", table: "IpmsSubmissions");
            migrationBuilder.DropColumn(name: "ActualPerformanceDescription", table: "IpmsSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Actual",
                table: "OpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualDescription",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualPerformanceDescription",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Actual",
                table: "IpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualDescription",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualPerformanceDescription",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE s SET
                    s.[Actual] = a.[LegacyActual],
                    s.[ActualDescription] = a.[LegacyActualDescription],
                    s.[ActualPerformanceDescription] = a.[LegacyActualPerformanceDescription]
                FROM [OpmsSubmissions] s
                INNER JOIN [LegacySubmissionValueArchives] a ON a.[OpmsSubmissionId] = s.[Id];

                UPDATE s SET
                    s.[Actual] = a.[LegacyActual],
                    s.[ActualDescription] = a.[LegacyActualDescription],
                    s.[ActualPerformanceDescription] = a.[LegacyActualPerformanceDescription]
                FROM [IpmsSubmissions] s
                INNER JOIN [LegacySubmissionValueArchives] a ON a.[IpmsSubmissionId] = s.[Id];
                """);

            migrationBuilder.DropTable(name: "LegacySubmissionValueArchives");
        }
    }
}

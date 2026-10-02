using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39RelationalOpmsTargetMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpmsTargetAdditionalAssignees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsTargetAdditionalAssignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsTargetAdditionalAssignees_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargetAdditionalAssignees_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargetAdditionalAssignees_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpmsTargetVoteNumbers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoteNumberId = table.Column<int>(type: "int", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsTargetVoteNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsTargetVoteNumbers_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargetVoteNumbers_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargetVoteNumbers_VoteNumbers_VoteNumberId",
                        column: x => x.VoteNumberId,
                        principalTable: "VoteNumbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpmsTargetWards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WardId = table.Column<int>(type: "int", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsTargetWards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsTargetWards_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargetWards_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargetWards_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    INSERT INTO [OpmsTargetWards] ([PublicId], [MunicipalityId], [OpmsTargetId], [WardId], [LinkedAt])
                    SELECT NEWID(), mapped.[MunicipalityId], mapped.[Id], mapped.[WardId], SYSUTCDATETIME()
                    FROM (
                    SELECT DISTINCT t.[MunicipalityId], t.[Id], w.[Id] AS [WardId]
                    FROM [OpmsTargets] t
                    CROSS APPLY STRING_SPLIT(t.[WardIds], ',') value
                    INNER JOIN [Wards] w ON w.[Id] = TRY_CONVERT(int, LTRIM(RTRIM(value.[value]))) AND w.[IsActive] = 1
                    INNER JOIN [Municipalities] m ON m.[Id] = t.[MunicipalityId] AND (UPPER(w.[Municipality]) = UPPER(m.[Code]) OR UPPER(w.[Municipality]) = UPPER(m.[Name]))
                    WHERE t.[MunicipalityId] IS NOT NULL AND NULLIF(LTRIM(RTRIM(t.[WardIds])), '') IS NOT NULL) mapped;

                    INSERT INTO [OpmsTargetAdditionalAssignees] ([PublicId], [MunicipalityId], [OpmsTargetId], [UserId], [LinkedAt])
                    SELECT NEWID(), mapped.[MunicipalityId], mapped.[Id], mapped.[UserId], SYSUTCDATETIME()
                    FROM (
                    SELECT DISTINCT t.[MunicipalityId], t.[Id], u.[Id] AS [UserId]
                    FROM [OpmsTargets] t
                    CROSS APPLY STRING_SPLIT(t.[AdditionalAssigneeIds], ',') value
                    INNER JOIN [AspNetUsers] u ON u.[Id] = LTRIM(RTRIM(value.[value])) AND u.[IsActive] = 1
                    WHERE t.[MunicipalityId] IS NOT NULL AND NULLIF(LTRIM(RTRIM(t.[AdditionalAssigneeIds])), '') IS NOT NULL
                      AND (u.[MunicipalityId] = t.[MunicipalityId] OR EXISTS (
                        SELECT 1 FROM [SecurityUserRoleAssignments] a
                        WHERE a.[UserId] = u.[Id] AND a.[MunicipalityId] = t.[MunicipalityId] AND a.[IsActive] = 1
                          AND a.[EffectiveFrom] <= SYSUTCDATETIME() AND (a.[EffectiveTo] IS NULL OR a.[EffectiveTo] > SYSUTCDATETIME())))) mapped;

                    INSERT INTO [OpmsTargetVoteNumbers] ([PublicId], [MunicipalityId], [OpmsTargetId], [VoteNumberId], [LinkedAt])
                    SELECT NEWID(), mapped.[MunicipalityId], mapped.[Id], mapped.[VoteNumberId], SYSUTCDATETIME()
                    FROM (
                    SELECT DISTINCT t.[MunicipalityId], t.[Id], v.[Id] AS [VoteNumberId]
                    FROM [OpmsTargets] t
                    CROSS APPLY STRING_SPLIT(t.[VoteNumberIds], ',') value
                    INNER JOIN [VoteNumbers] v ON v.[Id] = TRY_CONVERT(int, LTRIM(RTRIM(value.[value]))) AND v.[IsActive] = 1
                    INNER JOIN [Departments] d ON d.[Id] = v.[DepartmentId] AND d.[MunicipalityId] = t.[MunicipalityId]
                    WHERE t.[MunicipalityId] IS NOT NULL AND NULLIF(LTRIM(RTRIM(t.[VoteNumberIds])), '') IS NOT NULL) mapped;
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_MunicipalityId",
                table: "OpmsTargetAdditionalAssignees",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_OpmsTargetId_UserId",
                table: "OpmsTargetAdditionalAssignees",
                columns: new[] { "OpmsTargetId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_PublicId",
                table: "OpmsTargetAdditionalAssignees",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_UserId",
                table: "OpmsTargetAdditionalAssignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_MunicipalityId",
                table: "OpmsTargetVoteNumbers",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_OpmsTargetId_VoteNumberId",
                table: "OpmsTargetVoteNumbers",
                columns: new[] { "OpmsTargetId", "VoteNumberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_PublicId",
                table: "OpmsTargetVoteNumbers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_VoteNumberId",
                table: "OpmsTargetVoteNumbers",
                column: "VoteNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_MunicipalityId",
                table: "OpmsTargetWards",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_OpmsTargetId_WardId",
                table: "OpmsTargetWards",
                columns: new[] { "OpmsTargetId", "WardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_PublicId",
                table: "OpmsTargetWards",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_WardId",
                table: "OpmsTargetWards",
                column: "WardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropTable(
                name: "OpmsTargetVoteNumbers");

            migrationBuilder.DropTable(
                name: "OpmsTargetWards");
        }
    }
}

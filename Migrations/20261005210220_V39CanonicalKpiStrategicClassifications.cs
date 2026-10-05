using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39CanonicalKpiStrategicClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BackToBasicsPillarId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalKpaId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NationalKpaId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PerformanceObjectiveId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StrategicGoalMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StrategicInterventionId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StrategicObjectiveMasterId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BackToBasicsPillarId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalKpaId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NationalKpaId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PerformanceObjectiveId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StrategicGoalMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StrategicInterventionId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StrategicObjectiveMasterId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            // Only deterministic legacy text is reconciled automatically. Missing Back-to-Basics,
            // goal, intervention and strategic-objective references remain null for governed review.
            migrationBuilder.Sql(UniqueBackfillSql("OpmsTargets", "OPMS_NationalKPAs", "NationalKpa", "NationalKpaId", false));
            migrationBuilder.Sql(UniqueBackfillSql("OpmsTargets", "OPMS_MunicipalKPAs", "MunicipalKpa", "MunicipalKpaId", true));
            migrationBuilder.Sql(UniqueBackfillSql("OpmsTargets", "OPMS_PerformanceObjectives", "PerformanceObjective", "PerformanceObjectiveId", true));
            migrationBuilder.Sql(UniqueBackfillSql("IpmsTargets", "OPMS_NationalKPAs", "NationalKpa", "NationalKpaId", false));
            migrationBuilder.Sql(UniqueBackfillSql("IpmsTargets", "OPMS_MunicipalKPAs", "MunicipalKpa", "MunicipalKpaId", true));
            migrationBuilder.Sql(UniqueBackfillSql("IpmsTargets", "OPMS_PerformanceObjectives", "PerformanceObjective", "PerformanceObjectiveId", true));

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_BackToBasicsPillarId",
                table: "OpmsTargets",
                column: "BackToBasicsPillarId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_MunicipalKpaId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "MunicipalKpaId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_NationalKpaId",
                table: "OpmsTargets",
                column: "NationalKpaId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_PerformanceObjectiveId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "PerformanceObjectiveId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_StrategicGoalMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StrategicGoalMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_StrategicInterventionId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StrategicInterventionId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_StrategicObjectiveMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StrategicObjectiveMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_BackToBasicsPillarId",
                table: "IpmsTargets",
                column: "BackToBasicsPillarId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_MunicipalKpaId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "MunicipalKpaId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_NationalKpaId",
                table: "IpmsTargets",
                column: "NationalKpaId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_PerformanceObjectiveId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "PerformanceObjectiveId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_StrategicGoalMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "StrategicGoalMasterId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_StrategicInterventionId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "StrategicInterventionId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_StrategicObjectiveMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "StrategicObjectiveMasterId", "MunicipalityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_BackToBasicPillars_BackToBasicsPillarId",
                table: "IpmsTargets",
                column: "BackToBasicsPillarId",
                principalTable: "OPMS_BackToBasicPillars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_MunicipalKPAs_MunicipalKpaId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "MunicipalKpaId", "MunicipalityId" },
                principalTable: "OPMS_MunicipalKPAs",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_NationalKPAs_NationalKpaId",
                table: "IpmsTargets",
                column: "NationalKpaId",
                principalTable: "OPMS_NationalKPAs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_PerformanceObjectives_PerformanceObjectiveId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "PerformanceObjectiveId", "MunicipalityId" },
                principalTable: "OPMS_PerformanceObjectives",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_StrategicGoals_StrategicGoalMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "StrategicGoalMasterId", "MunicipalityId" },
                principalTable: "OPMS_StrategicGoals",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_StrategicInterventions_StrategicInterventionId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "StrategicInterventionId", "MunicipalityId" },
                principalTable: "OPMS_StrategicInterventions",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_OPMS_StrategicObjectives_StrategicObjectiveMasterId_MunicipalityId",
                table: "IpmsTargets",
                columns: new[] { "StrategicObjectiveMasterId", "MunicipalityId" },
                principalTable: "OPMS_StrategicObjectives",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_BackToBasicPillars_BackToBasicsPillarId",
                table: "OpmsTargets",
                column: "BackToBasicsPillarId",
                principalTable: "OPMS_BackToBasicPillars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_MunicipalKPAs_MunicipalKpaId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "MunicipalKpaId", "MunicipalityId" },
                principalTable: "OPMS_MunicipalKPAs",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_NationalKPAs_NationalKpaId",
                table: "OpmsTargets",
                column: "NationalKpaId",
                principalTable: "OPMS_NationalKPAs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_PerformanceObjectives_PerformanceObjectiveId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "PerformanceObjectiveId", "MunicipalityId" },
                principalTable: "OPMS_PerformanceObjectives",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_StrategicGoals_StrategicGoalMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StrategicGoalMasterId", "MunicipalityId" },
                principalTable: "OPMS_StrategicGoals",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_StrategicInterventions_StrategicInterventionId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StrategicInterventionId", "MunicipalityId" },
                principalTable: "OPMS_StrategicInterventions",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_OPMS_StrategicObjectives_StrategicObjectiveMasterId_MunicipalityId",
                table: "OpmsTargets",
                columns: new[] { "StrategicObjectiveMasterId", "MunicipalityId" },
                principalTable: "OPMS_StrategicObjectives",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_BackToBasicPillars_BackToBasicsPillarId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_MunicipalKPAs_MunicipalKpaId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_NationalKPAs_NationalKpaId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_PerformanceObjectives_PerformanceObjectiveId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_StrategicGoals_StrategicGoalMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_StrategicInterventions_StrategicInterventionId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_OPMS_StrategicObjectives_StrategicObjectiveMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_BackToBasicPillars_BackToBasicsPillarId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_MunicipalKPAs_MunicipalKpaId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_NationalKPAs_NationalKpaId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_PerformanceObjectives_PerformanceObjectiveId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_StrategicGoals_StrategicGoalMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_StrategicInterventions_StrategicInterventionId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_OPMS_StrategicObjectives_StrategicObjectiveMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_BackToBasicsPillarId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_MunicipalKpaId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_NationalKpaId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_PerformanceObjectiveId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_StrategicGoalMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_StrategicInterventionId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_StrategicObjectiveMasterId_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_BackToBasicsPillarId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_MunicipalKpaId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_NationalKpaId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_PerformanceObjectiveId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_StrategicGoalMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_StrategicInterventionId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_StrategicObjectiveMasterId_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "BackToBasicsPillarId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "MunicipalKpaId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "NationalKpaId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "PerformanceObjectiveId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "StrategicGoalMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "StrategicInterventionId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "StrategicObjectiveMasterId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "BackToBasicsPillarId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "MunicipalKpaId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "NationalKpaId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "PerformanceObjectiveId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "StrategicGoalMasterId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "StrategicInterventionId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "StrategicObjectiveMasterId",
                table: "IpmsTargets");
        }

        private static string UniqueBackfillSql(string targetTable, string masterTable, string legacyColumn, string foreignKeyColumn, bool tenantScoped) => $"""
            ;WITH [UniqueMatches] AS
            (
                SELECT [target].[Id] AS [TargetId], MIN([master].[Id]) AS [MasterId]
                FROM [{targetTable}] AS [target]
                INNER JOIN [{masterTable}] AS [master]
                    ON (UPPER(LTRIM(RTRIM([target].[{legacyColumn}]))) = UPPER(LTRIM(RTRIM([master].[Code])))
                        OR UPPER(LTRIM(RTRIM([target].[{legacyColumn}]))) = UPPER(LTRIM(RTRIM([master].[Name]))))
                    {(tenantScoped ? "AND [target].[MunicipalityId] = [master].[MunicipalityId]" : string.Empty)}
                WHERE NULLIF(LTRIM(RTRIM([target].[{legacyColumn}])), '') IS NOT NULL
                GROUP BY [target].[Id]
                HAVING COUNT_BIG(*) = 1
            )
            UPDATE [target]
            SET [{foreignKeyColumn}] = [matches].[MasterId]
            FROM [{targetTable}] AS [target]
            INNER JOIN [UniqueMatches] AS [matches] ON [matches].[TargetId] = [target].[Id];
            """;
    }
}

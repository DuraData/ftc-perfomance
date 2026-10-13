using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39RestrictiveIdpAggregateHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                CreateSqliteRestrictTriggers(migrationBuilder);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_IdpAlignmentLinks_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpAlignmentLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpAnnualTargets_IdpKpis_IdpKpiId",
                table: "IdpAnnualTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpChangeLogs_IdpPlanVersions_IdpPlanVersionId",
                table: "IdpChangeLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpCollaborationComments_IdpPlans_IdpPlanId",
                table: "IdpCollaborationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpCommunityNeeds_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpCommunityNeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpCommunitySessions_IdpPlans_IdpPlanId",
                table: "IdpCommunitySessions");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpDevelopmentPriorities_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpImportRows_IdpImportBatches_IdpImportBatchId",
                table: "IdpImportRows");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpKpis_IdpProjects_IdpProjectId",
                table: "IdpKpis");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpPlanVersions_IdpPlans_IdpPlanId",
                table: "IdpPlanVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpProgrammes_IdpDevelopmentPriorities_IdpDevelopmentPriorityId",
                table: "IdpProgrammes");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpProjects_IdpProgrammes_IdpProgrammeId",
                table: "IdpProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpStakeholderEngagements_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpStakeholderEngagements");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpStrategicObjectives_IdpStrategicOutcomes_IdpStrategicOutcomeId",
                table: "IdpStrategicObjectives");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpStrategicOutcomes_IdpPlans_IdpPlanId",
                table: "IdpStrategicOutcomes");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpTaskAssignments_IdpPlans_IdpPlanId",
                table: "IdpTaskAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpWardInputs_IdpPlans_IdpPlanId",
                table: "IdpWardInputs");

            migrationBuilder.AddForeignKey(
                name: "FK_IdpAlignmentLinks_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpAlignmentLinks",
                column: "IdpStrategicObjectiveId",
                principalTable: "IdpStrategicObjectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpAnnualTargets_IdpKpis_IdpKpiId",
                table: "IdpAnnualTargets",
                column: "IdpKpiId",
                principalTable: "IdpKpis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpChangeLogs_IdpPlanVersions_IdpPlanVersionId",
                table: "IdpChangeLogs",
                column: "IdpPlanVersionId",
                principalTable: "IdpPlanVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpCollaborationComments_IdpPlans_IdpPlanId",
                table: "IdpCollaborationComments",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpCommunityNeeds_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpCommunityNeeds",
                column: "IdpCommunitySessionId",
                principalTable: "IdpCommunitySessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpCommunitySessions_IdpPlans_IdpPlanId",
                table: "IdpCommunitySessions",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpDevelopmentPriorities_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities",
                column: "IdpStrategicObjectiveId",
                principalTable: "IdpStrategicObjectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpImportRows_IdpImportBatches_IdpImportBatchId",
                table: "IdpImportRows",
                column: "IdpImportBatchId",
                principalTable: "IdpImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpKpis_IdpProjects_IdpProjectId",
                table: "IdpKpis",
                column: "IdpProjectId",
                principalTable: "IdpProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpPlanVersions_IdpPlans_IdpPlanId",
                table: "IdpPlanVersions",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpProgrammes_IdpDevelopmentPriorities_IdpDevelopmentPriorityId",
                table: "IdpProgrammes",
                column: "IdpDevelopmentPriorityId",
                principalTable: "IdpDevelopmentPriorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpProjects_IdpProgrammes_IdpProgrammeId",
                table: "IdpProjects",
                column: "IdpProgrammeId",
                principalTable: "IdpProgrammes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpStakeholderEngagements_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpStakeholderEngagements",
                column: "IdpCommunitySessionId",
                principalTable: "IdpCommunitySessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpStrategicObjectives_IdpStrategicOutcomes_IdpStrategicOutcomeId",
                table: "IdpStrategicObjectives",
                column: "IdpStrategicOutcomeId",
                principalTable: "IdpStrategicOutcomes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpStrategicOutcomes_IdpPlans_IdpPlanId",
                table: "IdpStrategicOutcomes",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpTaskAssignments_IdpPlans_IdpPlanId",
                table: "IdpTaskAssignments",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpWardInputs_IdpPlans_IdpPlanId",
                table: "IdpWardInputs",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                DropSqliteRestrictTriggers(migrationBuilder);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_IdpAlignmentLinks_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpAlignmentLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpAnnualTargets_IdpKpis_IdpKpiId",
                table: "IdpAnnualTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpChangeLogs_IdpPlanVersions_IdpPlanVersionId",
                table: "IdpChangeLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpCollaborationComments_IdpPlans_IdpPlanId",
                table: "IdpCollaborationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpCommunityNeeds_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpCommunityNeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpCommunitySessions_IdpPlans_IdpPlanId",
                table: "IdpCommunitySessions");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpDevelopmentPriorities_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpImportRows_IdpImportBatches_IdpImportBatchId",
                table: "IdpImportRows");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpKpis_IdpProjects_IdpProjectId",
                table: "IdpKpis");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpPlanVersions_IdpPlans_IdpPlanId",
                table: "IdpPlanVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpProgrammes_IdpDevelopmentPriorities_IdpDevelopmentPriorityId",
                table: "IdpProgrammes");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpProjects_IdpProgrammes_IdpProgrammeId",
                table: "IdpProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpStakeholderEngagements_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpStakeholderEngagements");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpStrategicObjectives_IdpStrategicOutcomes_IdpStrategicOutcomeId",
                table: "IdpStrategicObjectives");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpStrategicOutcomes_IdpPlans_IdpPlanId",
                table: "IdpStrategicOutcomes");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpTaskAssignments_IdpPlans_IdpPlanId",
                table: "IdpTaskAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_IdpWardInputs_IdpPlans_IdpPlanId",
                table: "IdpWardInputs");

            migrationBuilder.AddForeignKey(
                name: "FK_IdpAlignmentLinks_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpAlignmentLinks",
                column: "IdpStrategicObjectiveId",
                principalTable: "IdpStrategicObjectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpAnnualTargets_IdpKpis_IdpKpiId",
                table: "IdpAnnualTargets",
                column: "IdpKpiId",
                principalTable: "IdpKpis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpChangeLogs_IdpPlanVersions_IdpPlanVersionId",
                table: "IdpChangeLogs",
                column: "IdpPlanVersionId",
                principalTable: "IdpPlanVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpCollaborationComments_IdpPlans_IdpPlanId",
                table: "IdpCollaborationComments",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpCommunityNeeds_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpCommunityNeeds",
                column: "IdpCommunitySessionId",
                principalTable: "IdpCommunitySessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpCommunitySessions_IdpPlans_IdpPlanId",
                table: "IdpCommunitySessions",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpDevelopmentPriorities_IdpStrategicObjectives_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities",
                column: "IdpStrategicObjectiveId",
                principalTable: "IdpStrategicObjectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpImportRows_IdpImportBatches_IdpImportBatchId",
                table: "IdpImportRows",
                column: "IdpImportBatchId",
                principalTable: "IdpImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpKpis_IdpProjects_IdpProjectId",
                table: "IdpKpis",
                column: "IdpProjectId",
                principalTable: "IdpProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpPlanVersions_IdpPlans_IdpPlanId",
                table: "IdpPlanVersions",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpProgrammes_IdpDevelopmentPriorities_IdpDevelopmentPriorityId",
                table: "IdpProgrammes",
                column: "IdpDevelopmentPriorityId",
                principalTable: "IdpDevelopmentPriorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpProjects_IdpProgrammes_IdpProgrammeId",
                table: "IdpProjects",
                column: "IdpProgrammeId",
                principalTable: "IdpProgrammes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpStakeholderEngagements_IdpCommunitySessions_IdpCommunitySessionId",
                table: "IdpStakeholderEngagements",
                column: "IdpCommunitySessionId",
                principalTable: "IdpCommunitySessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpStrategicObjectives_IdpStrategicOutcomes_IdpStrategicOutcomeId",
                table: "IdpStrategicObjectives",
                column: "IdpStrategicOutcomeId",
                principalTable: "IdpStrategicOutcomes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpStrategicOutcomes_IdpPlans_IdpPlanId",
                table: "IdpStrategicOutcomes",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpTaskAssignments_IdpPlans_IdpPlanId",
                table: "IdpTaskAssignments",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpWardInputs_IdpPlans_IdpPlanId",
                table: "IdpWardInputs",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void CreateSqliteRestrictTriggers(MigrationBuilder migrationBuilder)
        {
            foreach (var relationship in IdpHistoryRelationships())
            {
                migrationBuilder.Sql($$"""
                    CREATE TRIGGER "{{relationship.TriggerName}}"
                    BEFORE DELETE ON "{{relationship.ParentTable}}"
                    FOR EACH ROW
                    WHEN EXISTS (
                        SELECT 1 FROM "{{relationship.ChildTable}}"
                        WHERE "{{relationship.ForeignKey}}" = OLD."Id"
                    )
                    BEGIN
                        SELECT RAISE(ABORT, 'IDP governed history restricts parent deletion.');
                    END;
                    """);
            }
        }

        private static void DropSqliteRestrictTriggers(MigrationBuilder migrationBuilder)
        {
            foreach (var relationship in IdpHistoryRelationships())
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS \"{relationship.TriggerName}\";");
        }

        private static (string TriggerName, string ChildTable, string ParentTable, string ForeignKey)[] IdpHistoryRelationships() =>
        [
            ("TRG_IdpPlans_IdpPlanVersions_RestrictDelete", "IdpPlanVersions", "IdpPlans", "IdpPlanId"),
            ("TRG_IdpPlanVersions_IdpChangeLogs_RestrictDelete", "IdpChangeLogs", "IdpPlanVersions", "IdpPlanVersionId"),
            ("TRG_IdpPlans_IdpStrategicOutcomes_RestrictDelete", "IdpStrategicOutcomes", "IdpPlans", "IdpPlanId"),
            ("TRG_IdpStrategicOutcomes_IdpStrategicObjectives_RestrictDelete", "IdpStrategicObjectives", "IdpStrategicOutcomes", "IdpStrategicOutcomeId"),
            ("TRG_IdpStrategicObjectives_IdpDevelopmentPriorities_RestrictDelete", "IdpDevelopmentPriorities", "IdpStrategicObjectives", "IdpStrategicObjectiveId"),
            ("TRG_IdpDevelopmentPriorities_IdpProgrammes_RestrictDelete", "IdpProgrammes", "IdpDevelopmentPriorities", "IdpDevelopmentPriorityId"),
            ("TRG_IdpProgrammes_IdpProjects_RestrictDelete", "IdpProjects", "IdpProgrammes", "IdpProgrammeId"),
            ("TRG_IdpProjects_IdpKpis_RestrictDelete", "IdpKpis", "IdpProjects", "IdpProjectId"),
            ("TRG_IdpImportBatches_IdpImportRows_RestrictDelete", "IdpImportRows", "IdpImportBatches", "IdpImportBatchId"),
            ("TRG_IdpKpis_IdpAnnualTargets_RestrictDelete", "IdpAnnualTargets", "IdpKpis", "IdpKpiId"),
            ("TRG_IdpStrategicObjectives_IdpAlignmentLinks_RestrictDelete", "IdpAlignmentLinks", "IdpStrategicObjectives", "IdpStrategicObjectiveId"),
            ("TRG_IdpPlans_IdpCommunitySessions_RestrictDelete", "IdpCommunitySessions", "IdpPlans", "IdpPlanId"),
            ("TRG_IdpCommunitySessions_IdpCommunityNeeds_RestrictDelete", "IdpCommunityNeeds", "IdpCommunitySessions", "IdpCommunitySessionId"),
            ("TRG_IdpPlans_IdpWardInputs_RestrictDelete", "IdpWardInputs", "IdpPlans", "IdpPlanId"),
            ("TRG_IdpCommunitySessions_IdpStakeholderEngagements_RestrictDelete", "IdpStakeholderEngagements", "IdpCommunitySessions", "IdpCommunitySessionId"),
            ("TRG_IdpPlans_IdpCollaborationComments_RestrictDelete", "IdpCollaborationComments", "IdpPlans", "IdpPlanId"),
            ("TRG_IdpPlans_IdpTaskAssignments_RestrictDelete", "IdpTaskAssignments", "IdpPlans", "IdpPlanId")
        ];
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ControlledLegacyRatingReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            var idType = sqlServer ? "bigint" : "INTEGER";
            var guidType = sqlServer ? "uniqueidentifier" : "TEXT";
            var integerType = sqlServer ? "int" : "INTEGER";
            var textType = sqlServer ? "nvarchar(max)" : "TEXT";
            var sourceKeyType = sqlServer ? "nvarchar(550)" : "TEXT";
            var userIdType = sqlServer ? "nvarchar(450)" : "TEXT";
            var commentType = sqlServer ? "nvarchar(2000)" : "TEXT";
            var reasonType = sqlServer ? "nvarchar(1000)" : "TEXT";
            var correlationType = sqlServer ? "nvarchar(100)" : "TEXT";
            var decimalType = sqlServer ? "decimal(18,4)" : "TEXT";
            var dateTimeType = sqlServer ? "datetime2" : "TEXT";
            var booleanType = sqlServer ? "bit" : "INTEGER";

            migrationBuilder.CreateTable(
                name: "LegacySubmissionRatingReconciliations",
                columns: table => new
                {
                    Id = table.Column<long>(type: idType, nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: guidType, nullable: false),
                    MunicipalityId = table.Column<long>(type: idType, nullable: false),
                    SubmissionKind = table.Column<int>(type: integerType, nullable: false),
                    SubmissionId = table.Column<string>(type: textType, nullable: false),
                    SourceKind = table.Column<int>(type: integerType, nullable: false),
                    SourceKey = table.Column<string>(type: sourceKeyType, maxLength: 550, nullable: false),
                    SourceScore = table.Column<decimal>(type: decimalType, precision: 18, scale: 4, nullable: false),
                    SourceActorUserIdSnapshot = table.Column<string>(type: userIdType, maxLength: 450, nullable: false),
                    SourceOccurredAtSnapshot = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    SourceCommentSnapshot = table.Column<string>(type: commentType, maxLength: 2000, nullable: true),
                    SubmissionWorkflowInstanceId = table.Column<long>(type: idType, nullable: false),
                    SubmissionWorkflowActionId = table.Column<long>(type: idType, nullable: false),
                    WorkflowStageDefinitionId = table.Column<long>(type: idType, nullable: false),
                    RatingSchemeId = table.Column<long>(type: idType, nullable: false),
                    RatingSchemeValueId = table.Column<long>(type: idType, nullable: false),
                    SubmissionStageRatingId = table.Column<long>(type: idType, nullable: false),
                    UsedExistingRating = table.Column<bool>(type: booleanType, nullable: false),
                    ReconciledByUserId = table.Column<string>(type: userIdType, nullable: false),
                    ReconciledAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    Reason = table.Column<string>(type: reasonType, maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: correlationType, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacySubmissionRatingReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_AspNetUsers_ReconciledByUserId",
                        column: x => x.ReconciledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_RatingSchemeValues_RatingSchemeValueId",
                        column: x => x.RatingSchemeValueId,
                        principalTable: "RatingSchemeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_RatingSchemes_RatingSchemeId",
                        column: x => x.RatingSchemeId,
                        principalTable: "RatingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_SubmissionStageRatings_SubmissionStageRatingId",
                        column: x => x.SubmissionStageRatingId,
                        principalTable: "SubmissionStageRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_SubmissionWorkflowActions_SubmissionWorkflowActionId",
                        column: x => x.SubmissionWorkflowActionId,
                        principalTable: "SubmissionWorkflowActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_SubmissionWorkflowInstances_SubmissionWorkflowInstanceId",
                        column: x => x.SubmissionWorkflowInstanceId,
                        principalTable: "SubmissionWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacySubmissionRatingReconciliations_WorkflowStageDefinitions_WorkflowStageDefinitionId",
                        column: x => x.WorkflowStageDefinitionId,
                        principalTable: "WorkflowStageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_MunicipalityId_SourceKey",
                table: "LegacySubmissionRatingReconciliations",
                columns: new[] { "MunicipalityId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_PublicId",
                table: "LegacySubmissionRatingReconciliations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_RatingSchemeId",
                table: "LegacySubmissionRatingReconciliations",
                column: "RatingSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_RatingSchemeValueId",
                table: "LegacySubmissionRatingReconciliations",
                column: "RatingSchemeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_ReconciledByUserId",
                table: "LegacySubmissionRatingReconciliations",
                column: "ReconciledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_SubmissionStageRatingId",
                table: "LegacySubmissionRatingReconciliations",
                column: "SubmissionStageRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_SubmissionWorkflowActionId",
                table: "LegacySubmissionRatingReconciliations",
                column: "SubmissionWorkflowActionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_SubmissionWorkflowInstanceId",
                table: "LegacySubmissionRatingReconciliations",
                column: "SubmissionWorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubmissionRatingReconciliations_WorkflowStageDefinitionId",
                table: "LegacySubmissionRatingReconciliations",
                column: "WorkflowStageDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacySubmissionRatingReconciliations");
        }
    }
}

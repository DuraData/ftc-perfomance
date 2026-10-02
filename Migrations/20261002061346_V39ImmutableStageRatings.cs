using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ImmutableStageRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RatingSchemeId",
                table: "WorkflowStageDefinitions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresRating",
                table: "WorkflowStageDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SubmissionStageRatings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionWorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionWorkflowActionId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowStageDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    RatingSchemeId = table.Column<long>(type: "bigint", nullable: false),
                    RatingSchemeValueId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LabelSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AchievementPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionStageRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_AspNetUsers_RatedByUserId",
                        column: x => x.RatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_RatingSchemeValues_RatingSchemeValueId",
                        column: x => x.RatingSchemeValueId,
                        principalTable: "RatingSchemeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_RatingSchemes_RatingSchemeId",
                        column: x => x.RatingSchemeId,
                        principalTable: "RatingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_SubmissionWorkflowActions_SubmissionWorkflowActionId",
                        column: x => x.SubmissionWorkflowActionId,
                        principalTable: "SubmissionWorkflowActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_SubmissionWorkflowInstances_SubmissionWorkflowInstanceId",
                        column: x => x.SubmissionWorkflowInstanceId,
                        principalTable: "SubmissionWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionStageRatings_WorkflowStageDefinitions_WorkflowStageDefinitionId",
                        column: x => x.WorkflowStageDefinitionId,
                        principalTable: "WorkflowStageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStageDefinitions_RatingSchemeId",
                table: "WorkflowStageDefinitions",
                column: "RatingSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_MunicipalityId",
                table: "SubmissionStageRatings",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_PublicId",
                table: "SubmissionStageRatings",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_RatedByUserId",
                table: "SubmissionStageRatings",
                column: "RatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_RatingSchemeId",
                table: "SubmissionStageRatings",
                column: "RatingSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_RatingSchemeValueId",
                table: "SubmissionStageRatings",
                column: "RatingSchemeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_SubmissionWorkflowActionId",
                table: "SubmissionStageRatings",
                column: "SubmissionWorkflowActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_SubmissionWorkflowInstanceId_RatedAt",
                table: "SubmissionStageRatings",
                columns: new[] { "SubmissionWorkflowInstanceId", "RatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionStageRatings_WorkflowStageDefinitionId",
                table: "SubmissionStageRatings",
                column: "WorkflowStageDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowStageDefinitions_RatingSchemes_RatingSchemeId",
                table: "WorkflowStageDefinitions",
                column: "RatingSchemeId",
                principalTable: "RatingSchemes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowStageDefinitions_RatingSchemes_RatingSchemeId",
                table: "WorkflowStageDefinitions");

            migrationBuilder.DropTable(
                name: "SubmissionStageRatings");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStageDefinitions_RatingSchemeId",
                table: "WorkflowStageDefinitions");

            migrationBuilder.DropColumn(
                name: "RatingSchemeId",
                table: "WorkflowStageDefinitions");

            migrationBuilder.DropColumn(
                name: "RequiresRating",
                table: "WorkflowStageDefinitions");
        }
    }
}

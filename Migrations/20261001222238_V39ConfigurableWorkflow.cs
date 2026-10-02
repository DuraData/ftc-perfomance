using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ConfigurableWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RatingSchemes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingSchemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingSchemes_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportingWindows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingWindows", x => x.Id);
                    table.CheckConstraint("CK_ReportingWindows_Range", "[ClosesAt] > [OpensAt]");
                    table.ForeignKey(
                        name: "FK_ReportingWindows_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportingWindows_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowDefinitions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowDefinitions_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatingSchemeValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    RatingSchemeId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumAchievementPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaximumAchievementPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingSchemeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingSchemeValues_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RatingSchemeValues_RatingSchemes_RatingSchemeId",
                        column: x => x.RatingSchemeId,
                        principalTable: "RatingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportingWindowExceptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingWindowId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    ExtendedClosesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingWindowExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportingWindowExceptions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportingWindowExceptions_ReportingWindows_ReportingWindowId",
                        column: x => x.ReportingWindowId,
                        principalTable: "ReportingWindows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStageDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RequiredActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredPermissionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    AllowBypass = table.Column<bool>(type: "bit", nullable: false),
                    RequireDifferentActorFromSubmitter = table.Column<bool>(type: "bit", nullable: false),
                    RequireDifferentActorFromPreviousStage = table.Column<bool>(type: "bit", nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    RejectionStageCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStageDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStageDefinitions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStageDefinitions_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionWorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentStageId = table.Column<long>(type: "bigint", nullable: true),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    NextSequence = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionWorkflowInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowInstances_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowInstances_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowInstances_WorkflowStageDefinitions_CurrentStageId",
                        column: x => x.CurrentStageId,
                        principalTable: "WorkflowStageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceRfis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionWorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaisedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaisedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseDueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceRfis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceRfis_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceRfis_SubmissionWorkflowInstances_SubmissionWorkflowInstanceId",
                        column: x => x.SubmissionWorkflowInstanceId,
                        principalTable: "SubmissionWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionWorkflowActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionWorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    FromStageId = table.Column<long>(type: "bigint", nullable: true),
                    ToStageId = table.Column<long>(type: "bigint", nullable: true),
                    ActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RatingValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionWorkflowActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowActions_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowActions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowActions_SubmissionWorkflowInstances_SubmissionWorkflowInstanceId",
                        column: x => x.SubmissionWorkflowInstanceId,
                        principalTable: "SubmissionWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowActions_WorkflowStageDefinitions_FromStageId",
                        column: x => x.FromStageId,
                        principalTable: "WorkflowStageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionWorkflowActions_WorkflowStageDefinitions_ToStageId",
                        column: x => x.ToStageId,
                        principalTable: "WorkflowStageDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfis_MunicipalityId",
                table: "PerformanceRfis",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfis_PublicId",
                table: "PerformanceRfis",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRfis_SubmissionWorkflowInstanceId",
                table: "PerformanceRfis",
                column: "SubmissionWorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingSchemes_MunicipalityId_Code",
                table: "RatingSchemes",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingSchemes_PublicId",
                table: "RatingSchemes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingSchemeValues_MunicipalityId",
                table: "RatingSchemeValues",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingSchemeValues_PublicId",
                table: "RatingSchemeValues",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingSchemeValues_RatingSchemeId_Value",
                table: "RatingSchemeValues",
                columns: new[] { "RatingSchemeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingWindowExceptions_MunicipalityId",
                table: "ReportingWindowExceptions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingWindowExceptions_PublicId",
                table: "ReportingWindowExceptions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingWindowExceptions_ReportingWindowId",
                table: "ReportingWindowExceptions",
                column: "ReportingWindowId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingWindows_MunicipalityId_ReportingPeriodId_SubmissionKind",
                table: "ReportingWindows",
                columns: new[] { "MunicipalityId", "ReportingPeriodId", "SubmissionKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingWindows_PublicId",
                table: "ReportingWindows",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingWindows_ReportingPeriodId",
                table: "ReportingWindows",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowActions_ActorUserId",
                table: "SubmissionWorkflowActions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowActions_FromStageId",
                table: "SubmissionWorkflowActions",
                column: "FromStageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowActions_MunicipalityId",
                table: "SubmissionWorkflowActions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowActions_PublicId",
                table: "SubmissionWorkflowActions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowActions_SubmissionWorkflowInstanceId_Sequence",
                table: "SubmissionWorkflowActions",
                columns: new[] { "SubmissionWorkflowInstanceId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowActions_ToStageId",
                table: "SubmissionWorkflowActions",
                column: "ToStageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowInstances_CurrentStageId",
                table: "SubmissionWorkflowInstances",
                column: "CurrentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowInstances_MunicipalityId_SubmissionKind_SubmissionId",
                table: "SubmissionWorkflowInstances",
                columns: new[] { "MunicipalityId", "SubmissionKind", "SubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowInstances_PublicId",
                table: "SubmissionWorkflowInstances",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionWorkflowInstances_WorkflowDefinitionId",
                table: "SubmissionWorkflowInstances",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_MunicipalityFinancialYearId",
                table: "WorkflowDefinitions",
                column: "MunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_MunicipalityId_MunicipalityFinancialYearId_SubmissionKind_Code_Version",
                table: "WorkflowDefinitions",
                columns: new[] { "MunicipalityId", "MunicipalityFinancialYearId", "SubmissionKind", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_PublicId",
                table: "WorkflowDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStageDefinitions_MunicipalityId",
                table: "WorkflowStageDefinitions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStageDefinitions_PublicId",
                table: "WorkflowStageDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStageDefinitions_WorkflowDefinitionId_Code",
                table: "WorkflowStageDefinitions",
                columns: new[] { "WorkflowDefinitionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStageDefinitions_WorkflowDefinitionId_Sequence",
                table: "WorkflowStageDefinitions",
                columns: new[] { "WorkflowDefinitionId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceRfis");

            migrationBuilder.DropTable(
                name: "RatingSchemeValues");

            migrationBuilder.DropTable(
                name: "ReportingWindowExceptions");

            migrationBuilder.DropTable(
                name: "SubmissionWorkflowActions");

            migrationBuilder.DropTable(
                name: "RatingSchemes");

            migrationBuilder.DropTable(
                name: "ReportingWindows");

            migrationBuilder.DropTable(
                name: "SubmissionWorkflowInstances");

            migrationBuilder.DropTable(
                name: "WorkflowStageDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");
        }
    }
}

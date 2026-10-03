using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39PerformanceConsolidationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DerivedCalculationTypeId",
                table: "PerformancePeriodTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DerivedFromPeriods",
                table: "PerformancePeriodTargets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemDerivedTarget",
                table: "PerformancePeriodTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SystemSuggestedTargetValue",
                table: "PerformancePeriodTargets",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CalculationTypeId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SuggestionCalculationTypeId",
                table: "OpmsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionEditReason",
                table: "OpmsSubmissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestionEditedAt",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionEditedByUserId",
                table: "OpmsSubmissions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestionGeneratedDate",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemSuggestedActualPerformance",
                table: "OpmsSubmissions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasSystemSuggestionEdited",
                table: "OpmsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "CalculationTypeId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SuggestionCalculationTypeId",
                table: "IpmsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionEditReason",
                table: "IpmsSubmissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestionEditedAt",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionEditedByUserId",
                table: "IpmsSubmissions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestionGeneratedDate",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemSuggestedActualPerformance",
                table: "IpmsSubmissions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasSystemSuggestionEdited",
                table: "IpmsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PerformanceCalculationTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceCalculationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MunicipalityConsolidationPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    CalculationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    ConsolidationRule = table.Column<int>(type: "int", nullable: true),
                    MissingValuePolicy = table.Column<int>(type: "int", nullable: false),
                    AllowDerivedTargetOverride = table.Column<bool>(type: "bit", nullable: false),
                    DeriveAnnualTarget = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityConsolidationPolicies", x => x.Id);
                    table.CheckConstraint("CK_MunicipalityConsolidationPolicies_PrimitiveRule", "[ConsolidationRule] IS NULL OR [ConsolidationRule] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_MunicipalityConsolidationPolicies_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalityConsolidationPolicies_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalityConsolidationPolicies_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalityConsolidationPolicies_PerformanceCalculationTypes_CalculationTypeId",
                        column: x => x.CalculationTypeId,
                        principalTable: "PerformanceCalculationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceSuggestionEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    OpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    SystemSuggestedActualPerformance = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ActualPerformance = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    WasSystemSuggestionEdited = table.Column<bool>(type: "bit", nullable: false),
                    SuggestionCalculationTypeId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveCalculationType = table.Column<int>(type: "int", nullable: true),
                    SourcePeriods = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceSuggestionEvents", x => x.Id);
                    table.CheckConstraint("CK_PerformanceSuggestionEvents_OneSubmission", "CASE WHEN [OpmsSubmissionId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsSubmissionId] IS NULL THEN 0 ELSE 1 END = 1");
                    table.ForeignKey(
                        name: "FK_PerformanceSuggestionEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceSuggestionEvents_IpmsSubmissions_IpmsSubmissionId",
                        column: x => x.IpmsSubmissionId,
                        principalTable: "IpmsSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceSuggestionEvents_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceSuggestionEvents_OpmsSubmissions_OpmsSubmissionId",
                        column: x => x.OpmsSubmissionId,
                        principalTable: "OpmsSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceSuggestionEvents_PerformanceCalculationTypes_SuggestionCalculationTypeId",
                        column: x => x.SuggestionCalculationTypeId,
                        principalTable: "PerformanceCalculationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceSuggestionEvents_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PerformanceCalculationTypes",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name", "PublicId" },
                values: new object[,]
                {
                    { 1L, "SUM", "Add the valid source-period values.", true, "Sum", new Guid("00000000-0000-0000-0000-000000000001") },
                    { 2L, "AVERAGE", "Average the valid source-period values.", true, "Average", new Guid("00000000-0000-0000-0000-000000000002") },
                    { 3L, "LATEST_VALUE", "Use the latest valid source-period value.", true, "Latest value", new Guid("00000000-0000-0000-0000-000000000003") },
                    { 4L, "CUMULATIVE", "Use the latest cumulative position unless configured otherwise.", true, "Cumulative", new Guid("00000000-0000-0000-0000-000000000004") },
                    { 5L, "NON_CUMULATIVE", "Use an explicit configured primitive rule or manual capture.", true, "Non-cumulative", new Guid("00000000-0000-0000-0000-000000000005") },
                    { 6L, "REVERSE_CUMULATIVE", "Use the latest reducing position unless configured otherwise.", true, "Reverse cumulative", new Guid("00000000-0000-0000-0000-000000000006") },
                    { 7L, "REVERSE_NON_CUMULATIVE", "Use an explicit configured reverse primitive rule or manual capture.", true, "Reverse non-cumulative", new Guid("00000000-0000-0000-0000-000000000007") },
                    { 8L, "ZERO_BASED", "Use an explicit configured zero-based primitive rule or manual capture.", true, "Zero-based", new Guid("00000000-0000-0000-0000-000000000008") },
                    { 9L, "MANUAL", "Do not automatically consolidate unless explicitly configured.", true, "Manual", new Guid("00000000-0000-0000-0000-000000000009") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePeriodTargets_DerivedCalculationTypeId",
                table: "PerformancePeriodTargets",
                column: "DerivedCalculationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_CalculationTypeId",
                table: "OpmsTargets",
                column: "CalculationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_SuggestionCalculationTypeId",
                table: "OpmsSubmissions",
                column: "SuggestionCalculationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_SuggestionEditedByUserId",
                table: "OpmsSubmissions",
                column: "SuggestionEditedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsSubmissions_SuggestionEditMetadata",
                table: "OpmsSubmissions",
                sql: "[WasSystemSuggestionEdited] = 0 OR ([SystemSuggestedActualPerformance] IS NOT NULL AND [SuggestionEditedByUserId] IS NOT NULL AND [SuggestionEditedAt] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_CalculationTypeId",
                table: "IpmsTargets",
                column: "CalculationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_SuggestionCalculationTypeId",
                table: "IpmsSubmissions",
                column: "SuggestionCalculationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_SuggestionEditedByUserId",
                table: "IpmsSubmissions",
                column: "SuggestionEditedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpmsSubmissions_SuggestionEditMetadata",
                table: "IpmsSubmissions",
                sql: "[WasSystemSuggestionEdited] = 0 OR ([SystemSuggestedActualPerformance] IS NOT NULL AND [SuggestionEditedByUserId] IS NOT NULL AND [SuggestionEditedAt] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityConsolidationPolicies_CalculationTypeId",
                table: "MunicipalityConsolidationPolicies",
                column: "CalculationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityConsolidationPolicies_CreatedByUserId",
                table: "MunicipalityConsolidationPolicies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityConsolidationPolicies_ModifiedByUserId",
                table: "MunicipalityConsolidationPolicies",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityConsolidationPolicies_MunicipalityId_CalculationTypeId",
                table: "MunicipalityConsolidationPolicies",
                columns: new[] { "MunicipalityId", "CalculationTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityConsolidationPolicies_PublicId",
                table: "MunicipalityConsolidationPolicies",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceCalculationTypes_Code",
                table: "PerformanceCalculationTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceCalculationTypes_PublicId",
                table: "PerformanceCalculationTypes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_ActorUserId",
                table: "PerformanceSuggestionEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_IpmsSubmissionId",
                table: "PerformanceSuggestionEvents",
                column: "IpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_MunicipalityId_SubmissionKind_OpmsSubmissionId_IpmsSubmissionId_OccurredAt",
                table: "PerformanceSuggestionEvents",
                columns: new[] { "MunicipalityId", "SubmissionKind", "OpmsSubmissionId", "IpmsSubmissionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_OpmsSubmissionId",
                table: "PerformanceSuggestionEvents",
                column: "OpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_PublicId",
                table: "PerformanceSuggestionEvents",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_ReportingPeriodId",
                table: "PerformanceSuggestionEvents",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSuggestionEvents_SuggestionCalculationTypeId",
                table: "PerformanceSuggestionEvents",
                column: "SuggestionCalculationTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsSubmissions_AspNetUsers_SuggestionEditedByUserId",
                table: "IpmsSubmissions",
                column: "SuggestionEditedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsSubmissions_PerformanceCalculationTypes_SuggestionCalculationTypeId",
                table: "IpmsSubmissions",
                column: "SuggestionCalculationTypeId",
                principalTable: "PerformanceCalculationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_PerformanceCalculationTypes_CalculationTypeId",
                table: "IpmsTargets",
                column: "CalculationTypeId",
                principalTable: "PerformanceCalculationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsSubmissions_AspNetUsers_SuggestionEditedByUserId",
                table: "OpmsSubmissions",
                column: "SuggestionEditedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsSubmissions_PerformanceCalculationTypes_SuggestionCalculationTypeId",
                table: "OpmsSubmissions",
                column: "SuggestionCalculationTypeId",
                principalTable: "PerformanceCalculationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_PerformanceCalculationTypes_CalculationTypeId",
                table: "OpmsTargets",
                column: "CalculationTypeId",
                principalTable: "PerformanceCalculationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformancePeriodTargets_PerformanceCalculationTypes_DerivedCalculationTypeId",
                table: "PerformancePeriodTargets",
                column: "DerivedCalculationTypeId",
                principalTable: "PerformanceCalculationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IpmsSubmissions_AspNetUsers_SuggestionEditedByUserId",
                table: "IpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsSubmissions_PerformanceCalculationTypes_SuggestionCalculationTypeId",
                table: "IpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_PerformanceCalculationTypes_CalculationTypeId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsSubmissions_AspNetUsers_SuggestionEditedByUserId",
                table: "OpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsSubmissions_PerformanceCalculationTypes_SuggestionCalculationTypeId",
                table: "OpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_PerformanceCalculationTypes_CalculationTypeId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformancePeriodTargets_PerformanceCalculationTypes_DerivedCalculationTypeId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropTable(
                name: "MunicipalityConsolidationPolicies");

            migrationBuilder.DropTable(
                name: "PerformanceSuggestionEvents");

            migrationBuilder.DropTable(
                name: "PerformanceCalculationTypes");

            migrationBuilder.DropIndex(
                name: "IX_PerformancePeriodTargets_DerivedCalculationTypeId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_CalculationTypeId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_SuggestionCalculationTypeId",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_SuggestionEditedByUserId",
                table: "OpmsSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsSubmissions_SuggestionEditMetadata",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_CalculationTypeId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_SuggestionCalculationTypeId",
                table: "IpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_SuggestionEditedByUserId",
                table: "IpmsSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpmsSubmissions_SuggestionEditMetadata",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "DerivedCalculationTypeId",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "DerivedFromPeriods",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "IsSystemDerivedTarget",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "SystemSuggestedTargetValue",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "CalculationTypeId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "SuggestionCalculationTypeId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionEditReason",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionEditedAt",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionEditedByUserId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionGeneratedDate",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SystemSuggestedActualPerformance",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WasSystemSuggestionEdited",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "CalculationTypeId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "SuggestionCalculationTypeId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionEditReason",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionEditedAt",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionEditedByUserId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SuggestionGeneratedDate",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SystemSuggestedActualPerformance",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WasSystemSuggestionEdited",
                table: "IpmsSubmissions");
        }
    }
}

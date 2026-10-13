using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedPerformanceTargetDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                RebuildSqliteSubmissionTable(migrationBuilder, "OpmsSubmissions", "OpmsTargets", "OpmsTargetId", cascade: false);
                RebuildSqliteSubmissionTable(migrationBuilder, "IpmsSubmissions", "IpmsTargets", "IpmsTargetId", cascade: false);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsSubmissions_IpmsTargets_IpmsTargetId",
                table: "IpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsSubmissions_OpmsTargets_OpmsTargetId",
                table: "OpmsSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsSubmissions_IpmsTargets_IpmsTargetId",
                table: "IpmsSubmissions",
                column: "IpmsTargetId",
                principalTable: "IpmsTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsSubmissions_OpmsTargets_OpmsTargetId",
                table: "OpmsSubmissions",
                column: "OpmsTargetId",
                principalTable: "OpmsTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                RebuildSqliteSubmissionTable(migrationBuilder, "OpmsSubmissions", "OpmsTargets", "OpmsTargetId", cascade: true);
                RebuildSqliteSubmissionTable(migrationBuilder, "IpmsSubmissions", "IpmsTargets", "IpmsTargetId", cascade: true);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsSubmissions_IpmsTargets_IpmsTargetId",
                table: "IpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsSubmissions_OpmsTargets_OpmsTargetId",
                table: "OpmsSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsSubmissions_IpmsTargets_IpmsTargetId",
                table: "IpmsSubmissions",
                column: "IpmsTargetId",
                principalTable: "IpmsTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsSubmissions_OpmsTargets_OpmsTargetId",
                table: "OpmsSubmissions",
                column: "OpmsTargetId",
                principalTable: "OpmsTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void RebuildSqliteSubmissionTable(
            MigrationBuilder migrationBuilder,
            string table,
            string targetTable,
            string targetColumn,
            bool cascade)
        {
            var replacementTable = $"{table}_GovernedDeletionPolicy";
            var deleteAction = cascade ? "CASCADE" : "RESTRICT";
            migrationBuilder.Sql($$"""
                PRAGMA foreign_keys = OFF;
                BEGIN IMMEDIATE;
                CREATE TABLE "{{replacementTable}}" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_{{table}}" PRIMARY KEY,
                    "SystemSuggestedActualPerformance" TEXT NULL,
                    "WasSystemSuggestionEdited" INTEGER NOT NULL,
                    "SuggestionCalculationTypeId" INTEGER NULL,
                    "SuggestionGeneratedDate" TEXT NULL,
                    "SuggestionEditedByUserId" TEXT NULL,
                    "SuggestionEditedAt" TEXT NULL,
                    "SuggestionEditReason" TEXT NULL,
                    "PublicId" TEXT NOT NULL,
                    "MunicipalityId" INTEGER NULL,
                    "ReportingPeriodId" INTEGER NULL,
                    "{{targetColumn}}" TEXT NOT NULL,
                    "Quarter" TEXT NOT NULL,
                    "BaseState" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    "SubmitterStatus" TEXT NOT NULL,
                    "VerifierStatus" TEXT NOT NULL,
                    "ApproverStatus" TEXT NOT NULL,
                    "PmsStatus" TEXT NOT NULL,
                    "AuditorStatus" TEXT NOT NULL,
                    "ActualPerformance" TEXT NULL,
                    "AchievementPercent" TEXT NULL,
                    "TargetAchieved" INTEGER NULL,
                    "ActualExpenditure" TEXT NULL,
                    "Variance" TEXT NULL,
                    "VarianceReason" TEXT NULL,
                    "CorrectiveMeasure" TEXT NULL,
                    "SubmitterScore" TEXT NULL,
                    "SubmittedAt" TEXT NULL,
                    "SubmittedByUserId" TEXT NULL,
                    "VerifierUserId" TEXT NULL,
                    "VerifiedAt" TEXT NULL,
                    "VerifierComments" TEXT NULL,
                    "VerifierComment" TEXT NULL,
                    "VerifierScore" TEXT NULL,
                    "ApproverUserId" TEXT NULL,
                    "ApprovedAt" TEXT NULL,
                    "ApproverComments" TEXT NULL,
                    "ApproverComment" TEXT NULL,
                    "ApproverScore" TEXT NULL,
                    "PmsOfficerUserId" TEXT NULL,
                    "PmsReviewedAt" TEXT NULL,
                    "PmsComments" TEXT NULL,
                    "PmsComment" TEXT NULL,
                    "PmsRecommendation" TEXT NULL,
                    "PmsScore" TEXT NULL,
                    "PmsResponseDueDate" TEXT NULL,
                    "PmsRfiComment" TEXT NULL,
                    "AuditorUserId" TEXT NULL,
                    "AuditedAt" TEXT NULL,
                    "AuditorComments" TEXT NULL,
                    "AuditorComment" TEXT NULL,
                    "AuditorRecommendation" TEXT NULL,
                    "AuditorScore" TEXT NULL,
                    "AuditorResponseDueDate" TEXT NULL,
                    "DueDate" TEXT NULL,
                    "ExtendedDueDate" TEXT NULL,
                    "DueDateExtendedDays" INTEGER NULL,
                    "PoeType" TEXT NULL,
                    "IsDisabled" INTEGER NOT NULL,
                    "WithdrawalReason" TEXT NULL,
                    "WithdrawnAt" TEXT NULL,
                    "WithdrawnByUserId" TEXT NULL,
                    "CreatedBy" TEXT NULL,
                    "CreatedOn" TEXT NOT NULL,
                    "UpdatedBy" TEXT NULL,
                    "UpdatedOn" TEXT NULL,
                    "OrganisationId" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "RowVersion" BLOB NOT NULL,
                    CONSTRAINT "CK_{{table}}_BaseState" CHECK ([BaseState] IN ('IN_PROGRESS','SUBMITTED')),
                    CONSTRAINT "CK_{{table}}_SuggestionEditMetadata" CHECK ([WasSystemSuggestionEdited] = 0 OR ([SystemSuggestedActualPerformance] IS NOT NULL AND [SuggestionEditedByUserId] IS NOT NULL AND [SuggestionEditedAt] IS NOT NULL)),
                    CONSTRAINT "CK_{{table}}_WithdrawalMetadata" CHECK ([IsDisabled] = 0 OR ([WithdrawalReason] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)),
                    CONSTRAINT "FK_{{table}}_AspNetUsers_ApproverUserId" FOREIGN KEY ("ApproverUserId") REFERENCES "AspNetUsers" ("Id"),
                    CONSTRAINT "FK_{{table}}_AspNetUsers_AuditorUserId" FOREIGN KEY ("AuditorUserId") REFERENCES "AspNetUsers" ("Id"),
                    CONSTRAINT "FK_{{table}}_AspNetUsers_PmsOfficerUserId" FOREIGN KEY ("PmsOfficerUserId") REFERENCES "AspNetUsers" ("Id"),
                    CONSTRAINT "FK_{{table}}_AspNetUsers_SubmittedByUserId" FOREIGN KEY ("SubmittedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_{{table}}_AspNetUsers_SuggestionEditedByUserId" FOREIGN KEY ("SuggestionEditedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_{{table}}_AspNetUsers_VerifierUserId" FOREIGN KEY ("VerifierUserId") REFERENCES "AspNetUsers" ("Id"),
                    CONSTRAINT "FK_{{table}}_Municipalities_MunicipalityId" FOREIGN KEY ("MunicipalityId") REFERENCES "Municipalities" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_{{table}}_{{targetTable}}_{{targetColumn}}" FOREIGN KEY ("{{targetColumn}}") REFERENCES "{{targetTable}}" ("Id") ON DELETE {{deleteAction}},
                    CONSTRAINT "FK_{{table}}_PerformanceCalculationTypes_SuggestionCalculationTypeId" FOREIGN KEY ("SuggestionCalculationTypeId") REFERENCES "PerformanceCalculationTypes" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_{{table}}_ReportingPeriods_ReportingPeriodId" FOREIGN KEY ("ReportingPeriodId") REFERENCES "ReportingPeriods" ("Id") ON DELETE RESTRICT
                );
                INSERT INTO "{{replacementTable}}" SELECT * FROM "{{table}}";
                DROP TABLE "{{table}}";
                ALTER TABLE "{{replacementTable}}" RENAME TO "{{table}}";
                CREATE INDEX "IX_{{table}}_ApproverUserId" ON "{{table}}" ("ApproverUserId");
                CREATE INDEX "IX_{{table}}_AuditorUserId" ON "{{table}}" ("AuditorUserId");
                CREATE UNIQUE INDEX "IX_{{table}}_{{targetColumn}}_ReportingPeriodId" ON "{{table}}" ("{{targetColumn}}", "ReportingPeriodId") WHERE [ReportingPeriodId] IS NOT NULL;
                CREATE INDEX "IX_{{table}}_MunicipalityId" ON "{{table}}" ("MunicipalityId");
                CREATE INDEX "IX_{{table}}_PmsOfficerUserId" ON "{{table}}" ("PmsOfficerUserId");
                CREATE UNIQUE INDEX "IX_{{table}}_PublicId" ON "{{table}}" ("PublicId");
                CREATE INDEX "IX_{{table}}_ReportingPeriodId" ON "{{table}}" ("ReportingPeriodId");
                CREATE INDEX "IX_{{table}}_SubmittedByUserId" ON "{{table}}" ("SubmittedByUserId");
                CREATE INDEX "IX_{{table}}_SuggestionCalculationTypeId" ON "{{table}}" ("SuggestionCalculationTypeId");
                CREATE INDEX "IX_{{table}}_SuggestionEditedByUserId" ON "{{table}}" ("SuggestionEditedByUserId");
                CREATE INDEX "IX_{{table}}_VerifierUserId" ON "{{table}}" ("VerifierUserId");
                COMMIT;
                PRAGMA foreign_keys = ON;
                """, suppressTransaction: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39NotificationPoliciesAndScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicabilityKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: true),
                    WorkflowStageCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: true),
                    Lifecycle = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    DeliveryPaused = table.Column<bool>(type: "bit", nullable: false),
                    ChannelsCsv = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationConfigurations", x => x.Id);
                    table.CheckConstraint("CK_NotificationConfigurations_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_NotificationConfigurations_Scope", "([Scope] = 1 AND [WorkflowStageCode] IS NULL AND [ReportingPeriodId] IS NULL) OR ([Scope] = 2 AND [WorkflowStageCode] IS NOT NULL AND [ReportingPeriodId] IS NULL) OR ([Scope] = 3 AND [ReportingPeriodId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_NotificationConfigurations_AspNetUsers_ActivatedByUserId",
                        column: x => x.ActivatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationConfigurations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationConfigurations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationConfigurations_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationConfigurations_NotificationConfigurations_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "NotificationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationConfigurations_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkingCalendarHolidays",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingCalendarHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingCalendarHolidays_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkingCalendarHolidays_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkingCalendarHolidays_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationScheduleRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    NotificationConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    WorkingDayOffset = table.Column<int>(type: "int", nullable: false),
                    RecipientKind = table.Column<int>(type: "int", nullable: false),
                    RecipientValuesCsv = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationScheduleRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationScheduleRules_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationScheduleRules_NotificationConfigurations_NotificationConfigurationId",
                        column: x => x.NotificationConfigurationId,
                        principalTable: "NotificationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    NotificationConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    NotificationScheduleRuleId = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RecipientUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogicalKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    MaterializedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BusinessEventOutboxId = table.Column<long>(type: "bigint", nullable: true),
                    StateReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_BusinessEventOutbox_BusinessEventOutboxId",
                        column: x => x.BusinessEventOutboxId,
                        principalTable: "BusinessEventOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_NotificationConfigurations_NotificationConfigurationId",
                        column: x => x.NotificationConfigurationId,
                        principalTable: "NotificationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_NotificationScheduleRules_NotificationScheduleRuleId",
                        column: x => x.NotificationScheduleRuleId,
                        principalTable: "NotificationScheduleRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledNotifications_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_ActivatedByUserId",
                table: "NotificationConfigurations",
                column: "ActivatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_CreatedByUserId",
                table: "NotificationConfigurations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_MunicipalityFinancialYearId",
                table: "NotificationConfigurations",
                column: "MunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_MunicipalityId_FamilyId_Version",
                table: "NotificationConfigurations",
                columns: new[] { "MunicipalityId", "FamilyId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_MunicipalityId_MunicipalityFinancialYearId_ApplicabilityKey",
                table: "NotificationConfigurations",
                columns: new[] { "MunicipalityId", "MunicipalityFinancialYearId", "ApplicabilityKey" },
                unique: true,
                filter: "[Lifecycle] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_MunicipalityId_MunicipalityFinancialYearId_Code_Scope_WorkflowStageCode_ReportingPeriodId_Lifecyc~",
                table: "NotificationConfigurations",
                columns: new[] { "MunicipalityId", "MunicipalityFinancialYearId", "Code", "Scope", "WorkflowStageCode", "ReportingPeriodId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_PreviousVersionId",
                table: "NotificationConfigurations",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_PublicId",
                table: "NotificationConfigurations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigurations_ReportingPeriodId",
                table: "NotificationConfigurations",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationScheduleRules_MunicipalityId",
                table: "NotificationScheduleRules",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationScheduleRules_NotificationConfigurationId_Code",
                table: "NotificationScheduleRules",
                columns: new[] { "NotificationConfigurationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationScheduleRules_PublicId",
                table: "NotificationScheduleRules",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_BusinessEventOutboxId",
                table: "ScheduledNotifications",
                column: "BusinessEventOutboxId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_LogicalKey",
                table: "ScheduledNotifications",
                column: "LogicalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_MunicipalityId",
                table: "ScheduledNotifications",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_NotificationConfigurationId",
                table: "ScheduledNotifications",
                column: "NotificationConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_NotificationId",
                table: "ScheduledNotifications",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_NotificationScheduleRuleId",
                table: "ScheduledNotifications",
                column: "NotificationScheduleRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_PublicId",
                table: "ScheduledNotifications",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_RecipientUserId",
                table: "ScheduledNotifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_State_ScheduledAt",
                table: "ScheduledNotifications",
                columns: new[] { "State", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkingCalendarHolidays_CreatedByUserId",
                table: "WorkingCalendarHolidays",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingCalendarHolidays_MunicipalityFinancialYearId_Date",
                table: "WorkingCalendarHolidays",
                columns: new[] { "MunicipalityFinancialYearId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkingCalendarHolidays_MunicipalityId",
                table: "WorkingCalendarHolidays",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingCalendarHolidays_PublicId",
                table: "WorkingCalendarHolidays",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledNotifications");

            migrationBuilder.DropTable(
                name: "WorkingCalendarHolidays");

            migrationBuilder.DropTable(
                name: "NotificationScheduleRules");

            migrationBuilder.DropTable(
                name: "NotificationConfigurations");
        }
    }
}

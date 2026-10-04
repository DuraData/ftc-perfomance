using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39OfficialReportJobsAndDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficialReportSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleFamilyPublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    ReportTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Cadence = table.Column<int>(type: "int", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientKind = table.Column<int>(type: "int", nullable: false),
                    RecipientValuesCsv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ChannelsCsv = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialReportSchedules", x => x.Id);
                    table.CheckConstraint("CK_OfficialReportSchedules_Cadence", "[Cadence] >= 1 AND [Cadence] <= 4");
                    table.CheckConstraint("CK_OfficialReportSchedules_RecipientKind", "[RecipientKind] >= 1 AND [RecipientKind] <= 2");
                    table.CheckConstraint("CK_OfficialReportSchedules_VersionInterval", "[VersionNumber] >= 1 AND [Interval] >= 1 AND [Interval] <= 365");
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_OfficialReportSchedules_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "OfficialReportSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_OfficialReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "OfficialReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportSchedules_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficialReportJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    OfficialReportScheduleId = table.Column<long>(type: "bigint", nullable: true),
                    ReportTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    PreviousOfficialReportGenerationId = table.Column<long>(type: "bigint", nullable: true),
                    OfficialReportGenerationId = table.Column<long>(type: "bigint", nullable: true),
                    DistributionOutboxId = table.Column<long>(type: "bigint", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientUserIdsCsv = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ChannelsCsv = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsMandatoryDistribution = table.Column<bool>(type: "bit", nullable: false),
                    RetryReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialReportJobs", x => x.Id);
                    table.CheckConstraint("CK_OfficialReportJobs_StateAttempts", "[State] >= 1 AND [State] <= 6 AND [AttemptCount] >= 0");
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_BusinessEventOutbox_DistributionOutboxId",
                        column: x => x.DistributionOutboxId,
                        principalTable: "BusinessEventOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_OfficialReportGenerations_OfficialReportGenerationId",
                        column: x => x.OfficialReportGenerationId,
                        principalTable: "OfficialReportGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_OfficialReportGenerations_PreviousOfficialReportGenerationId",
                        column: x => x.PreviousOfficialReportGenerationId,
                        principalTable: "OfficialReportGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_OfficialReportSchedules_OfficialReportScheduleId",
                        column: x => x.OfficialReportScheduleId,
                        principalTable: "OfficialReportSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_OfficialReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "OfficialReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialReportJobs_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_DepartmentId",
                table: "OfficialReportJobs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_DistributionOutboxId",
                table: "OfficialReportJobs",
                column: "DistributionOutboxId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_MunicipalityFinancialYearId",
                table: "OfficialReportJobs",
                column: "MunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_MunicipalityId",
                table: "OfficialReportJobs",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_OfficialReportGenerationId",
                table: "OfficialReportJobs",
                column: "OfficialReportGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_OfficialReportScheduleId_ScheduledFor",
                table: "OfficialReportJobs",
                columns: new[] { "OfficialReportScheduleId", "ScheduledFor" },
                unique: true,
                filter: "[OfficialReportScheduleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_PreviousOfficialReportGenerationId",
                table: "OfficialReportJobs",
                column: "PreviousOfficialReportGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_PublicId",
                table: "OfficialReportJobs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_ReportingPeriodId",
                table: "OfficialReportJobs",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_ReportTemplateId",
                table: "OfficialReportJobs",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_RequestedByUserId",
                table: "OfficialReportJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_State_AvailableAt",
                table: "OfficialReportJobs",
                columns: new[] { "State", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportJobs_UnitId",
                table: "OfficialReportJobs",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_CreatedByUserId",
                table: "OfficialReportSchedules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_DepartmentId",
                table: "OfficialReportSchedules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_IsActive_NextRunAt",
                table: "OfficialReportSchedules",
                columns: new[] { "IsActive", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_MunicipalityFinancialYearId",
                table: "OfficialReportSchedules",
                column: "MunicipalityFinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_MunicipalityId_ScheduleFamilyPublicId_IsCurrent",
                table: "OfficialReportSchedules",
                columns: new[] { "MunicipalityId", "ScheduleFamilyPublicId", "IsCurrent" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_MunicipalityId_ScheduleFamilyPublicId_VersionNumber",
                table: "OfficialReportSchedules",
                columns: new[] { "MunicipalityId", "ScheduleFamilyPublicId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_PreviousVersionId",
                table: "OfficialReportSchedules",
                column: "PreviousVersionId",
                unique: true,
                filter: "[PreviousVersionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_PublicId",
                table: "OfficialReportSchedules",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_ReportingPeriodId",
                table: "OfficialReportSchedules",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_ReportTemplateId",
                table: "OfficialReportSchedules",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialReportSchedules_UnitId",
                table: "OfficialReportSchedules",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficialReportJobs");

            migrationBuilder.DropTable(
                name: "OfficialReportSchedules");
        }
    }
}

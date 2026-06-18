using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupTablesAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Finding = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditFindings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditTrails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditTrails_AspNetUsers_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DueDateExtensions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtendedDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DueDateExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DueDateExtensions_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IpmsTargetTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KpiDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformanceArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobGrade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetUnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnualTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualTargetDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultRatingMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultScoreScale = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultPoeRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultTaskTemplatesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedOpmsTargetRequired = table.Column<bool>(type: "bit", nullable: false),
                    FunctionalArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpmsTargetTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpmsTargetTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KpiDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Baseline = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualTargetDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetUnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationalKpa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MunicipalKpa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrategicGoal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrategicObjective = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceObjective = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriorityIssue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BudgetSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BudgetType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KpiType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndicatorType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionalArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdpReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FmsLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultQuarterlyTargetsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultBudgetInformation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultPoeRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsTargetTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Periods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FiscalYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoteNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteNumbers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Municipality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpmsTargetTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpmsTargetTemplateId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpmsTargetTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpmsTargetTemplateVersions_IpmsTargetTemplates_IpmsTargetTemplateId",
                        column: x => x.IpmsTargetTemplateId,
                        principalTable: "IpmsTargetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpmsTargetTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpmsTargetTemplateId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsTargetTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsTargetTemplateVersions_OpmsTargetTemplates_OpmsTargetTemplateId",
                        column: x => x.OpmsTargetTemplateId,
                        principalTable: "OpmsTargetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategicGoalId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicObjectives_StrategicGoals_StrategicGoalId",
                        column: x => x.StrategicGoalId,
                        principalTable: "StrategicGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpmsTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceTemplateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceTemplateVersion = table.Column<int>(type: "int", nullable: true),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WardIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalAssigneeIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoteNumberIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndicatorNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalKpa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MunicipalKpa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrategicGoalId = table.Column<int>(type: "int", nullable: true),
                    StrategicObjectiveId = table.Column<int>(type: "int", nullable: true),
                    PerformanceObjective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KpiDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Baseline = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaselineDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnnualTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualTargetDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BudgetSourceId = table.Column<int>(type: "int", nullable: true),
                    BudgetTypeId = table.Column<int>(type: "int", nullable: true),
                    UnitOfMeasureId = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KpiType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FunctionalArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdpReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FmsLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRevised = table.Column<bool>(type: "bit", nullable: false),
                    IsWithdrawn = table.Column<bool>(type: "bit", nullable: false),
                    ReasonForWithdrawal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetUnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Q1Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q1Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Q1Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q2Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q2Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Q2Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MidTermTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MidTermDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MidTermBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q3Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q3Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Q3Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q3RevisedTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q4Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q4Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Q4Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q4RevisedTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RevisedAnnualTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RevisedAnnualBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_AspNetUsers_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_BudgetSources_BudgetSourceId",
                        column: x => x.BudgetSourceId,
                        principalTable: "BudgetSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_BudgetTypes_BudgetTypeId",
                        column: x => x.BudgetTypeId,
                        principalTable: "BudgetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_StrategicGoals_StrategicGoalId",
                        column: x => x.StrategicGoalId,
                        principalTable: "StrategicGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_StrategicObjectives_StrategicObjectiveId",
                        column: x => x.StrategicObjectiveId,
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsTargets_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IpmsTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceTemplateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceTemplateVersion = table.Column<int>(type: "int", nullable: true),
                    RelatedOpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SupervisorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndicatorNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalKpa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MunicipalKpa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrategicGoalId = table.Column<int>(type: "int", nullable: true),
                    StrategicObjectiveId = table.Column<int>(type: "int", nullable: true),
                    PerformanceObjective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KpiDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Baseline = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualTargetDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BudgetSourceId = table.Column<int>(type: "int", nullable: true),
                    BudgetTypeId = table.Column<int>(type: "int", nullable: true),
                    UnitOfMeasureId = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KpiType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FunctionalArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdpReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRevised = table.Column<bool>(type: "bit", nullable: false),
                    TargetUnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Q1Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q2Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MidTermTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q3Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Q4Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpmsTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_AspNetUsers_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_BudgetSources_BudgetSourceId",
                        column: x => x.BudgetSourceId,
                        principalTable: "BudgetSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_BudgetTypes_BudgetTypeId",
                        column: x => x.BudgetTypeId,
                        principalTable: "BudgetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_OpmsTargets_RelatedOpmsTargetId",
                        column: x => x.RelatedOpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_StrategicGoals_StrategicGoalId",
                        column: x => x.StrategicGoalId,
                        principalTable: "StrategicGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_StrategicObjectives_StrategicObjectiveId",
                        column: x => x.StrategicObjectiveId,
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_UnitOfMeasures_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsTargets_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpmsSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Quarter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualExpenditure = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VarianceReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectiveMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmitterScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VerifierUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifierComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproverUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PmsOfficerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PmsReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PmsComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AuditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuditorComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtendedDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsSubmissions_AspNetUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpmsSubmissions_AspNetUsers_AuditorUserId",
                        column: x => x.AuditorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpmsSubmissions_AspNetUsers_PmsOfficerUserId",
                        column: x => x.PmsOfficerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpmsSubmissions_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsSubmissions_AspNetUsers_VerifierUserId",
                        column: x => x.VerifierUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpmsSubmissions_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IpmsSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Quarter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Actual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualExpenditure = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VarianceReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectiveMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmitterScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VerifierUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifierComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproverUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PmsOfficerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PmsReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PmsComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AuditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuditorComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtendedDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpmsSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpmsSubmissions_AspNetUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IpmsSubmissions_AspNetUsers_AuditorUserId",
                        column: x => x.AuditorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IpmsSubmissions_AspNetUsers_PmsOfficerUserId",
                        column: x => x.PmsOfficerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IpmsSubmissions_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpmsSubmissions_AspNetUsers_VerifierUserId",
                        column: x => x.VerifierUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IpmsSubmissions_IpmsTargets_IpmsTargetId",
                        column: x => x.IpmsTargetId,
                        principalTable: "IpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoeFiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoeFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoeFiles_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeFiles_IpmsSubmissions_IpmsSubmissionId",
                        column: x => x.IpmsSubmissionId,
                        principalTable: "IpmsSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PoeFiles_OpmsSubmissions_OpmsSubmissionId",
                        column: x => x.OpmsSubmissionId,
                        principalTable: "OpmsSubmissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CommentedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewComments_AspNetUsers_CommentedByUserId",
                        column: x => x.CommentedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewComments_IpmsSubmissions_IpmsSubmissionId",
                        column: x => x.IpmsSubmissionId,
                        principalTable: "IpmsSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewComments_OpmsSubmissions_OpmsSubmissionId",
                        column: x => x.OpmsSubmissionId,
                        principalTable: "OpmsSubmissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubmissionScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmissionKind = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScoredByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OpmsSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionScores_AspNetUsers_ScoredByUserId",
                        column: x => x.ScoredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionScores_IpmsSubmissions_IpmsSubmissionId",
                        column: x => x.IpmsSubmissionId,
                        principalTable: "IpmsSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmissionScores_OpmsSubmissions_OpmsSubmissionId",
                        column: x => x.OpmsSubmissionId,
                        principalTable: "OpmsSubmissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_CreatedByUserId",
                table: "AuditFindings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_ChangedBy",
                table: "AuditTrails",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSources_Code",
                table: "BudgetSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTypes_Code",
                table: "BudgetTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DueDateExtensions_ApprovedByUserId",
                table: "DueDateExtensions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_ApproverUserId",
                table: "IpmsSubmissions",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_AuditorUserId",
                table: "IpmsSubmissions",
                column: "AuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_IpmsTargetId",
                table: "IpmsSubmissions",
                column: "IpmsTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_PmsOfficerUserId",
                table: "IpmsSubmissions",
                column: "PmsOfficerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_SubmittedByUserId",
                table: "IpmsSubmissions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_VerifierUserId",
                table: "IpmsSubmissions",
                column: "VerifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_AssignedUserId",
                table: "IpmsTargets",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_BudgetSourceId",
                table: "IpmsTargets",
                column: "BudgetSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_BudgetTypeId",
                table: "IpmsTargets",
                column: "BudgetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_DepartmentId",
                table: "IpmsTargets",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_PeriodId",
                table: "IpmsTargets",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_RelatedOpmsTargetId",
                table: "IpmsTargets",
                column: "RelatedOpmsTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_StrategicGoalId",
                table: "IpmsTargets",
                column: "StrategicGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_StrategicObjectiveId",
                table: "IpmsTargets",
                column: "StrategicObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_UnitId",
                table: "IpmsTargets",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_UnitOfMeasureId",
                table: "IpmsTargets",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargetTemplates_TemplateCode",
                table: "IpmsTargetTemplates",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargetTemplateVersions_IpmsTargetTemplateId",
                table: "IpmsTargetTemplateVersions",
                column: "IpmsTargetTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_ApproverUserId",
                table: "OpmsSubmissions",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_AuditorUserId",
                table: "OpmsSubmissions",
                column: "AuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_OpmsTargetId",
                table: "OpmsSubmissions",
                column: "OpmsTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_PmsOfficerUserId",
                table: "OpmsSubmissions",
                column: "PmsOfficerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_SubmittedByUserId",
                table: "OpmsSubmissions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_VerifierUserId",
                table: "OpmsSubmissions",
                column: "VerifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_AssignedUserId",
                table: "OpmsTargets",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_BudgetSourceId",
                table: "OpmsTargets",
                column: "BudgetSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_BudgetTypeId",
                table: "OpmsTargets",
                column: "BudgetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_DepartmentId",
                table: "OpmsTargets",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_PeriodId",
                table: "OpmsTargets",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_StrategicGoalId",
                table: "OpmsTargets",
                column: "StrategicGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_StrategicObjectiveId",
                table: "OpmsTargets",
                column: "StrategicObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_UnitId",
                table: "OpmsTargets",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_UnitOfMeasureId",
                table: "OpmsTargets",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetTemplates_TemplateCode",
                table: "OpmsTargetTemplates",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetTemplateVersions_OpmsTargetTemplateId",
                table: "OpmsTargetTemplateVersions",
                column: "OpmsTargetTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Periods_Code",
                table: "Periods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoeFiles_IpmsSubmissionId",
                table: "PoeFiles",
                column: "IpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeFiles_OpmsSubmissionId",
                table: "PoeFiles",
                column: "OpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeFiles_UploadedByUserId",
                table: "PoeFiles",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_CommentedByUserId",
                table: "ReviewComments",
                column: "CommentedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_IpmsSubmissionId",
                table: "ReviewComments",
                column: "IpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_OpmsSubmissionId",
                table: "ReviewComments",
                column: "OpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicGoals_Code",
                table: "StrategicGoals",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_Code",
                table: "StrategicObjectives",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_StrategicGoalId",
                table: "StrategicObjectives",
                column: "StrategicGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionScores_IpmsSubmissionId",
                table: "SubmissionScores",
                column: "IpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionScores_OpmsSubmissionId",
                table: "SubmissionScores",
                column: "OpmsSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionScores_ScoredByUserId",
                table: "SubmissionScores",
                column: "ScoredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_Code",
                table: "UnitOfMeasures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_Code",
                table: "VoteNumbers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_DepartmentId",
                table: "VoteNumbers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_Code",
                table: "Wards",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "AuditTrails");

            migrationBuilder.DropTable(
                name: "DueDateExtensions");

            migrationBuilder.DropTable(
                name: "IpmsTargetTemplateVersions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OpmsTargetTemplateVersions");

            migrationBuilder.DropTable(
                name: "PoeFiles");

            migrationBuilder.DropTable(
                name: "ReviewComments");

            migrationBuilder.DropTable(
                name: "SubmissionScores");

            migrationBuilder.DropTable(
                name: "VoteNumbers");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "IpmsTargetTemplates");

            migrationBuilder.DropTable(
                name: "OpmsTargetTemplates");

            migrationBuilder.DropTable(
                name: "IpmsSubmissions");

            migrationBuilder.DropTable(
                name: "OpmsSubmissions");

            migrationBuilder.DropTable(
                name: "IpmsTargets");

            migrationBuilder.DropTable(
                name: "OpmsTargets");

            migrationBuilder.DropTable(
                name: "BudgetSources");

            migrationBuilder.DropTable(
                name: "BudgetTypes");

            migrationBuilder.DropTable(
                name: "Periods");

            migrationBuilder.DropTable(
                name: "StrategicObjectives");

            migrationBuilder.DropTable(
                name: "UnitOfMeasures");

            migrationBuilder.DropTable(
                name: "StrategicGoals");
        }
    }
}

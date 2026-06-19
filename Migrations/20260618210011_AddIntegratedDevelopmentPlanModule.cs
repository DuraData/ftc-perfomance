using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegratedDevelopmentPlanModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdpPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MunicipalityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartFinancialYear = table.Column<int>(type: "int", nullable: false),
                    EndFinancialYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpPlans_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpPlans_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdpCommunitySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    ParticipationType = table.Column<int>(type: "int", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardId = table.Column<int>(type: "int", nullable: true),
                    ParticipantsCount = table.Column<int>(type: "int", nullable: false),
                    AttendanceRegisterPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinutesPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpCommunitySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpCommunitySessions_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdpCommunitySessions_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdpPlanVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    VersionType = table.Column<int>(type: "int", nullable: false),
                    VersionLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummaryOfChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpPlanVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpPlanVersions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpPlanVersions_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpStrategicOutcomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpStrategicOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpStrategicOutcomes_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpWardInputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    WardId = table.Column<int>(type: "int", nullable: false),
                    WardPlanSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardPriorities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WardProjects = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpWardInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpWardInputs_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdpWardInputs_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdpCommunityNeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpCommunitySessionId = table.Column<int>(type: "int", nullable: false),
                    IssueCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriorityLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedIntervention = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpCommunityNeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpCommunityNeeds_IdpCommunitySessions_IdpCommunitySessionId",
                        column: x => x.IdpCommunitySessionId,
                        principalTable: "IdpCommunitySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpStakeholderEngagements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpCommunitySessionId = table.Column<int>(type: "int", nullable: false),
                    StakeholderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StakeholderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeyInput = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpStakeholderEngagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpStakeholderEngagements_IdpCommunitySessions_IdpCommunitySessionId",
                        column: x => x.IdpCommunitySessionId,
                        principalTable: "IdpCommunitySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpChangeLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanVersionId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpChangeLogs_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpChangeLogs_IdpPlanVersions_IdpPlanVersionId",
                        column: x => x.IdpPlanVersionId,
                        principalTable: "IdpPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpCollaborationComments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    IdpPlanVersionId = table.Column<int>(type: "int", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CommentedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpCollaborationComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpCollaborationComments_AspNetUsers_CommentedByUserId",
                        column: x => x.CommentedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpCollaborationComments_IdpPlanVersions_IdpPlanVersionId",
                        column: x => x.IdpPlanVersionId,
                        principalTable: "IdpPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpCollaborationComments_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    IdpPlanVersionId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpDocuments_IdpPlanVersions_IdpPlanVersionId",
                        column: x => x.IdpPlanVersionId,
                        principalTable: "IdpPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpDocuments_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpTaskAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    IdpPlanVersionId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpTaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpTaskAssignments_AspNetUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpTaskAssignments_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpTaskAssignments_IdpPlanVersions_IdpPlanVersionId",
                        column: x => x.IdpPlanVersionId,
                        principalTable: "IdpPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpTaskAssignments_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpStrategicObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpStrategicOutcomeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaselineValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResponsibleDepartmentId = table.Column<int>(type: "int", nullable: true),
                    StrategicOwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BudgetAllocation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpStrategicObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpStrategicObjectives_AspNetUsers_StrategicOwnerUserId",
                        column: x => x.StrategicOwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpStrategicObjectives_Departments_ResponsibleDepartmentId",
                        column: x => x.ResponsibleDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpStrategicObjectives_IdpStrategicOutcomes_IdpStrategicOutcomeId",
                        column: x => x.IdpStrategicOutcomeId,
                        principalTable: "IdpStrategicOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpAlignmentLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpStrategicObjectiveId = table.Column<int>(type: "int", nullable: false),
                    FrameworkType = table.Column<int>(type: "int", nullable: false),
                    FrameworkReferenceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrameworkReferenceTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpAlignmentLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpAlignmentLinks_IdpStrategicObjectives_IdpStrategicObjectiveId",
                        column: x => x.IdpStrategicObjectiveId,
                        principalTable: "IdpStrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpDevelopmentPriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpStrategicObjectiveId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpDevelopmentPriorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpDevelopmentPriorities_IdpStrategicObjectives_IdpStrategicObjectiveId",
                        column: x => x.IdpStrategicObjectiveId,
                        principalTable: "IdpStrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpProgrammes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpDevelopmentPriorityId = table.Column<int>(type: "int", nullable: false),
                    ProgrammeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsibleDepartmentId = table.Column<int>(type: "int", nullable: true),
                    PlannedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualExpenditure = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpProgrammes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpProgrammes_Departments_ResponsibleDepartmentId",
                        column: x => x.ResponsibleDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpProgrammes_IdpDevelopmentPriorities_IdpDevelopmentPriorityId",
                        column: x => x.IdpDevelopmentPriorityId,
                        principalTable: "IdpDevelopmentPriorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpProgrammeId = table.Column<int>(type: "int", nullable: false),
                    ProjectCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FundingSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CommunityNeedReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpProjects_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpProjects_IdpProgrammes_IdpProgrammeId",
                        column: x => x.IdpProgrammeId,
                        principalTable: "IdpProgrammes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpBudgetSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpStrategicObjectiveId = table.Column<int>(type: "int", nullable: true),
                    IdpProjectId = table.Column<int>(type: "int", nullable: true),
                    FinancialYear = table.Column<int>(type: "int", nullable: false),
                    PlannedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualExpenditure = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpBudgetSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpBudgetSnapshots_IdpProjects_IdpProjectId",
                        column: x => x.IdpProjectId,
                        principalTable: "IdpProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpBudgetSnapshots_IdpStrategicObjectives_IdpStrategicObjectiveId",
                        column: x => x.IdpStrategicObjectiveId,
                        principalTable: "IdpStrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdpKpis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpProjectId = table.Column<int>(type: "int", nullable: false),
                    KpiCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KpiName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Baseline = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FiveYearTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResponsibleDepartmentId = table.Column<int>(type: "int", nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportingFrequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorType = table.Column<int>(type: "int", nullable: false),
                    Circular88Linked = table.Column<bool>(type: "bit", nullable: false),
                    TreasuryTidLinked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpKpis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpKpis_Departments_ResponsibleDepartmentId",
                        column: x => x.ResponsibleDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpKpis_IdpProjects_IdpProjectId",
                        column: x => x.IdpProjectId,
                        principalTable: "IdpProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpAnnualTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpKpiId = table.Column<int>(type: "int", nullable: false),
                    FinancialYear = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProgressComment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpAnnualTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpAnnualTargets_IdpKpis_IdpKpiId",
                        column: x => x.IdpKpiId,
                        principalTable: "IdpKpis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdpRiskLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdpStrategicObjectiveId = table.Column<int>(type: "int", nullable: true),
                    IdpProjectId = table.Column<int>(type: "int", nullable: true),
                    IdpKpiId = table.Column<int>(type: "int", nullable: true),
                    RiskReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MitigationPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpRiskLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpRiskLinks_IdpKpis_IdpKpiId",
                        column: x => x.IdpKpiId,
                        principalTable: "IdpKpis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpRiskLinks_IdpProjects_IdpProjectId",
                        column: x => x.IdpProjectId,
                        principalTable: "IdpProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpRiskLinks_IdpStrategicObjectives_IdpStrategicObjectiveId",
                        column: x => x.IdpStrategicObjectiveId,
                        principalTable: "IdpStrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdpAlignmentLinks_IdpStrategicObjectiveId",
                table: "IdpAlignmentLinks",
                column: "IdpStrategicObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpAnnualTargets_IdpKpiId_FinancialYear",
                table: "IdpAnnualTargets",
                columns: new[] { "IdpKpiId", "FinancialYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpBudgetSnapshots_IdpProjectId",
                table: "IdpBudgetSnapshots",
                column: "IdpProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpBudgetSnapshots_IdpStrategicObjectiveId",
                table: "IdpBudgetSnapshots",
                column: "IdpStrategicObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpChangeLogs_ChangedByUserId",
                table: "IdpChangeLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpChangeLogs_IdpPlanVersionId",
                table: "IdpChangeLogs",
                column: "IdpPlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpCollaborationComments_CommentedByUserId",
                table: "IdpCollaborationComments",
                column: "CommentedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpCollaborationComments_IdpPlanId",
                table: "IdpCollaborationComments",
                column: "IdpPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpCollaborationComments_IdpPlanVersionId",
                table: "IdpCollaborationComments",
                column: "IdpPlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpCommunityNeeds_IdpCommunitySessionId",
                table: "IdpCommunityNeeds",
                column: "IdpCommunitySessionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpCommunitySessions_IdpPlanId",
                table: "IdpCommunitySessions",
                column: "IdpPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpCommunitySessions_WardId",
                table: "IdpCommunitySessions",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpDevelopmentPriorities_IdpStrategicObjectiveId",
                table: "IdpDevelopmentPriorities",
                column: "IdpStrategicObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpDocuments_IdpPlanId",
                table: "IdpDocuments",
                column: "IdpPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpDocuments_IdpPlanVersionId",
                table: "IdpDocuments",
                column: "IdpPlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpDocuments_UploadedByUserId",
                table: "IdpDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpKpis_IdpProjectId_KpiCode",
                table: "IdpKpis",
                columns: new[] { "IdpProjectId", "KpiCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpKpis_ResponsibleDepartmentId",
                table: "IdpKpis",
                column: "ResponsibleDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_ApprovedByUserId",
                table: "IdpPlans",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_CreatedByUserId",
                table: "IdpPlans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_PlanCode",
                table: "IdpPlans",
                column: "PlanCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlanVersions_CreatedByUserId",
                table: "IdpPlanVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlanVersions_IdpPlanId_VersionNumber",
                table: "IdpPlanVersions",
                columns: new[] { "IdpPlanId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpProgrammes_IdpDevelopmentPriorityId_ProgrammeCode",
                table: "IdpProgrammes",
                columns: new[] { "IdpDevelopmentPriorityId", "ProgrammeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpProgrammes_ResponsibleDepartmentId",
                table: "IdpProgrammes",
                column: "ResponsibleDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpProjects_DepartmentId",
                table: "IdpProjects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpProjects_IdpProgrammeId_ProjectCode",
                table: "IdpProjects",
                columns: new[] { "IdpProgrammeId", "ProjectCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpRiskLinks_IdpKpiId",
                table: "IdpRiskLinks",
                column: "IdpKpiId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpRiskLinks_IdpProjectId",
                table: "IdpRiskLinks",
                column: "IdpProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpRiskLinks_IdpStrategicObjectiveId",
                table: "IdpRiskLinks",
                column: "IdpStrategicObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpStakeholderEngagements_IdpCommunitySessionId",
                table: "IdpStakeholderEngagements",
                column: "IdpCommunitySessionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpStrategicObjectives_IdpStrategicOutcomeId_Code",
                table: "IdpStrategicObjectives",
                columns: new[] { "IdpStrategicOutcomeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpStrategicObjectives_ResponsibleDepartmentId",
                table: "IdpStrategicObjectives",
                column: "ResponsibleDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpStrategicObjectives_StrategicOwnerUserId",
                table: "IdpStrategicObjectives",
                column: "StrategicOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpStrategicOutcomes_IdpPlanId_Code",
                table: "IdpStrategicOutcomes",
                columns: new[] { "IdpPlanId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpTaskAssignments_AssignedByUserId",
                table: "IdpTaskAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpTaskAssignments_AssignedToUserId",
                table: "IdpTaskAssignments",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpTaskAssignments_IdpPlanId",
                table: "IdpTaskAssignments",
                column: "IdpPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpTaskAssignments_IdpPlanVersionId",
                table: "IdpTaskAssignments",
                column: "IdpPlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpWardInputs_IdpPlanId_WardId",
                table: "IdpWardInputs",
                columns: new[] { "IdpPlanId", "WardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpWardInputs_WardId",
                table: "IdpWardInputs",
                column: "WardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdpAlignmentLinks");

            migrationBuilder.DropTable(
                name: "IdpAnnualTargets");

            migrationBuilder.DropTable(
                name: "IdpBudgetSnapshots");

            migrationBuilder.DropTable(
                name: "IdpChangeLogs");

            migrationBuilder.DropTable(
                name: "IdpCollaborationComments");

            migrationBuilder.DropTable(
                name: "IdpCommunityNeeds");

            migrationBuilder.DropTable(
                name: "IdpDocuments");

            migrationBuilder.DropTable(
                name: "IdpRiskLinks");

            migrationBuilder.DropTable(
                name: "IdpStakeholderEngagements");

            migrationBuilder.DropTable(
                name: "IdpTaskAssignments");

            migrationBuilder.DropTable(
                name: "IdpWardInputs");

            migrationBuilder.DropTable(
                name: "IdpKpis");

            migrationBuilder.DropTable(
                name: "IdpCommunitySessions");

            migrationBuilder.DropTable(
                name: "IdpPlanVersions");

            migrationBuilder.DropTable(
                name: "IdpProjects");

            migrationBuilder.DropTable(
                name: "IdpProgrammes");

            migrationBuilder.DropTable(
                name: "IdpDevelopmentPriorities");

            migrationBuilder.DropTable(
                name: "IdpStrategicObjectives");

            migrationBuilder.DropTable(
                name: "IdpStrategicOutcomes");

            migrationBuilder.DropTable(
                name: "IdpPlans");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39StrategicPlanningMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OPMS_MunicipalKPAs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_MunicipalKPAs", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_MunicipalKPAs_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_MunicipalKPAs_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalKPAs_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalKPAs_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalKPAs_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_PerformanceObjectives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_PerformanceObjectives", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_PerformanceObjectives_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_PerformanceObjectives_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_PerformanceObjectives_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_PerformanceObjectives_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_PerformanceObjectives_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicGoals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicGoals", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_StrategicGoals_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_StrategicGoals_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoals_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoals_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoals_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicInterventions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicInterventions", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_StrategicInterventions_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_StrategicInterventions_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicInterventions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicInterventions_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicInterventions_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicObjectives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFromFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveToFinancialYearId = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicObjectives", x => x.Id);
                    table.UniqueConstraint("AK_OPMS_StrategicObjectives_Id_MunicipalityId", x => new { x.Id, x.MunicipalityId });
                    table.CheckConstraint("CK_OPMS_StrategicObjectives_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicObjectives_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicObjectives_MunicipalityFinancialYears_EffectiveFromFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveFromFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicObjectives_MunicipalityFinancialYears_EffectiveToFinancialYearId_MunicipalityId",
                        columns: x => new { x.EffectiveToFinancialYearId, x.MunicipalityId },
                        principalTable: "MunicipalityFinancialYears",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_MunicipalKPAStrategicGoals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MunicipalKpaId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicGoalId = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_MunicipalKPAStrategicGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalKPAStrategicGoals_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalKPAStrategicGoals_OPMS_MunicipalKPAs_MunicipalKpaId_MunicipalityId",
                        columns: x => new { x.MunicipalKpaId, x.MunicipalityId },
                        principalTable: "OPMS_MunicipalKPAs",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_MunicipalKPAStrategicGoals_OPMS_StrategicGoals_StrategicGoalId_MunicipalityId",
                        columns: x => new { x.StrategicGoalId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicGoals",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicGoalInterventions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategicGoalId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicInterventionId = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicGoalInterventions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoalInterventions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoalInterventions_OPMS_StrategicGoals_StrategicGoalId_MunicipalityId",
                        columns: x => new { x.StrategicGoalId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicGoals",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoalInterventions_OPMS_StrategicInterventions_StrategicInterventionId_MunicipalityId",
                        columns: x => new { x.StrategicInterventionId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicInterventions",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicGoalObjectives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategicGoalId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicObjectiveId = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicGoalObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoalObjectives_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoalObjectives_OPMS_StrategicGoals_StrategicGoalId_MunicipalityId",
                        columns: x => new { x.StrategicGoalId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicGoals",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicGoalObjectives_OPMS_StrategicObjectives_StrategicObjectiveId_MunicipalityId",
                        columns: x => new { x.StrategicObjectiveId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicObjectives",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicInterventionObjectives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategicInterventionId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicObjectiveId = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicInterventionObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicInterventionObjectives_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicInterventionObjectives_OPMS_StrategicInterventions_StrategicInterventionId_MunicipalityId",
                        columns: x => new { x.StrategicInterventionId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicInterventions",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicInterventionObjectives_OPMS_StrategicObjectives_StrategicObjectiveId_MunicipalityId",
                        columns: x => new { x.StrategicObjectiveId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicObjectives",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPMS_StrategicObjectivePerformanceObjectives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategicObjectiveId = table.Column<long>(type: "bigint", nullable: false),
                    PerformanceObjectiveId = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPMS_StrategicObjectivePerformanceObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicObjectivePerformanceObjectives_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicObjectivePerformanceObjectives_OPMS_PerformanceObjectives_PerformanceObjectiveId_MunicipalityId",
                        columns: x => new { x.PerformanceObjectiveId, x.MunicipalityId },
                        principalTable: "OPMS_PerformanceObjectives",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPMS_StrategicObjectivePerformanceObjectives_OPMS_StrategicObjectives_StrategicObjectiveId_MunicipalityId",
                        columns: x => new { x.StrategicObjectiveId, x.MunicipalityId },
                        principalTable: "OPMS_StrategicObjectives",
                        principalColumns: new[] { "Id", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAs_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_MunicipalKPAs",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAs_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_MunicipalKPAs",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAs_MunicipalityId_Code",
                table: "OPMS_MunicipalKPAs",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAs_PublicId",
                table: "OPMS_MunicipalKPAs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAStrategicGoals_MunicipalityId_MunicipalKpaId_StrategicGoalId",
                table: "OPMS_MunicipalKPAStrategicGoals",
                columns: new[] { "MunicipalityId", "MunicipalKpaId", "StrategicGoalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAStrategicGoals_MunicipalKpaId_MunicipalityId",
                table: "OPMS_MunicipalKPAStrategicGoals",
                columns: new[] { "MunicipalKpaId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAStrategicGoals_PublicId",
                table: "OPMS_MunicipalKPAStrategicGoals",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_MunicipalKPAStrategicGoals_StrategicGoalId_MunicipalityId",
                table: "OPMS_MunicipalKPAStrategicGoals",
                columns: new[] { "StrategicGoalId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_PerformanceObjectives_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_PerformanceObjectives",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_PerformanceObjectives_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_PerformanceObjectives",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_PerformanceObjectives_MunicipalityId_Code",
                table: "OPMS_PerformanceObjectives",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_PerformanceObjectives_PublicId",
                table: "OPMS_PerformanceObjectives",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalInterventions_MunicipalityId_StrategicGoalId_StrategicInterventionId",
                table: "OPMS_StrategicGoalInterventions",
                columns: new[] { "MunicipalityId", "StrategicGoalId", "StrategicInterventionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalInterventions_PublicId",
                table: "OPMS_StrategicGoalInterventions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalInterventions_StrategicGoalId_MunicipalityId",
                table: "OPMS_StrategicGoalInterventions",
                columns: new[] { "StrategicGoalId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalInterventions_StrategicInterventionId_MunicipalityId",
                table: "OPMS_StrategicGoalInterventions",
                columns: new[] { "StrategicInterventionId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalObjectives_MunicipalityId_StrategicGoalId_StrategicObjectiveId",
                table: "OPMS_StrategicGoalObjectives",
                columns: new[] { "MunicipalityId", "StrategicGoalId", "StrategicObjectiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalObjectives_PublicId",
                table: "OPMS_StrategicGoalObjectives",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalObjectives_StrategicGoalId_MunicipalityId",
                table: "OPMS_StrategicGoalObjectives",
                columns: new[] { "StrategicGoalId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoalObjectives_StrategicObjectiveId_MunicipalityId",
                table: "OPMS_StrategicGoalObjectives",
                columns: new[] { "StrategicObjectiveId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoals_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_StrategicGoals",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoals_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_StrategicGoals",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoals_MunicipalityId_Code",
                table: "OPMS_StrategicGoals",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicGoals_PublicId",
                table: "OPMS_StrategicGoals",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventionObjectives_MunicipalityId_StrategicInterventionId_StrategicObjectiveId",
                table: "OPMS_StrategicInterventionObjectives",
                columns: new[] { "MunicipalityId", "StrategicInterventionId", "StrategicObjectiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventionObjectives_PublicId",
                table: "OPMS_StrategicInterventionObjectives",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventionObjectives_StrategicInterventionId_MunicipalityId",
                table: "OPMS_StrategicInterventionObjectives",
                columns: new[] { "StrategicInterventionId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventionObjectives_StrategicObjectiveId_MunicipalityId",
                table: "OPMS_StrategicInterventionObjectives",
                columns: new[] { "StrategicObjectiveId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventions_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_StrategicInterventions",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventions_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_StrategicInterventions",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventions_MunicipalityId_Code",
                table: "OPMS_StrategicInterventions",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicInterventions_PublicId",
                table: "OPMS_StrategicInterventions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectivePerformanceObjectives_MunicipalityId_StrategicObjectiveId_PerformanceObjectiveId",
                table: "OPMS_StrategicObjectivePerformanceObjectives",
                columns: new[] { "MunicipalityId", "StrategicObjectiveId", "PerformanceObjectiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectivePerformanceObjectives_PerformanceObjectiveId_MunicipalityId",
                table: "OPMS_StrategicObjectivePerformanceObjectives",
                columns: new[] { "PerformanceObjectiveId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectivePerformanceObjectives_PublicId",
                table: "OPMS_StrategicObjectivePerformanceObjectives",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectivePerformanceObjectives_StrategicObjectiveId_MunicipalityId",
                table: "OPMS_StrategicObjectivePerformanceObjectives",
                columns: new[] { "StrategicObjectiveId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectives_EffectiveFromFinancialYearId_MunicipalityId",
                table: "OPMS_StrategicObjectives",
                columns: new[] { "EffectiveFromFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectives_EffectiveToFinancialYearId_MunicipalityId",
                table: "OPMS_StrategicObjectives",
                columns: new[] { "EffectiveToFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectives_MunicipalityId_Code",
                table: "OPMS_StrategicObjectives",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_StrategicObjectives_PublicId",
                table: "OPMS_StrategicObjectives",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OPMS_MunicipalKPAStrategicGoals");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicGoalInterventions");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicGoalObjectives");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicInterventionObjectives");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicObjectivePerformanceObjectives");

            migrationBuilder.DropTable(
                name: "OPMS_MunicipalKPAs");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicGoals");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicInterventions");

            migrationBuilder.DropTable(
                name: "OPMS_PerformanceObjectives");

            migrationBuilder.DropTable(
                name: "OPMS_StrategicObjectives");
        }
    }
}

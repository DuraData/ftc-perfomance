using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39Circular88 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "C88CatalogueVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    EditionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88CatalogueVersions", x => x.Id);
                    table.CheckConstraint("CK_C88CatalogueVersions_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_C88CatalogueVersions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88CatalogueVersions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88CatalogueItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88CatalogueVersionId = table.Column<long>(type: "bigint", nullable: false),
                    ParentItemId = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88CatalogueItems", x => x.Id);
                    table.CheckConstraint("CK_C88CatalogueItems_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_C88CatalogueItems_C88CatalogueItems_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88CatalogueItems_C88CatalogueVersions_C88CatalogueVersionId",
                        column: x => x.C88CatalogueVersionId,
                        principalTable: "C88CatalogueVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88CatalogueItems_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88MunicipalityConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    C88CatalogueVersionId = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88MunicipalityConfigurations", x => x.Id);
                    table.CheckConstraint("CK_C88MunicipalityConfigurations_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_C88MunicipalityConfigurations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88MunicipalityConfigurations_C88CatalogueVersions_C88CatalogueVersionId",
                        column: x => x.C88CatalogueVersionId,
                        principalTable: "C88CatalogueVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88MunicipalityConfigurations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88MunicipalityConfigurations_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88ComplianceQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88CatalogueVersionId = table.Column<long>(type: "bigint", nullable: false),
                    ReportTypeItemId = table.Column<long>(type: "bigint", nullable: false),
                    ResponseTypeItemId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88ComplianceQuestions", x => x.Id);
                    table.CheckConstraint("CK_C88ComplianceQuestions_Sequence", "[Sequence] > 0");
                    table.ForeignKey(
                        name: "FK_C88ComplianceQuestions_C88CatalogueItems_ReportTypeItemId",
                        column: x => x.ReportTypeItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ComplianceQuestions_C88CatalogueItems_ResponseTypeItemId",
                        column: x => x.ResponseTypeItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ComplianceQuestions_C88CatalogueVersions_C88CatalogueVersionId",
                        column: x => x.C88CatalogueVersionId,
                        principalTable: "C88CatalogueVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ComplianceQuestions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88Indicators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88CatalogueVersionId = table.Column<long>(type: "bigint", nullable: false),
                    SectorItemId = table.Column<long>(type: "bigint", nullable: true),
                    OutcomeItemId = table.Column<long>(type: "bigint", nullable: true),
                    IndicatorTypeItemId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    OfficialTechnicalIndicatorDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    CalculationOperator = table.Column<int>(type: "int", nullable: false),
                    OfficialFormulaText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequiresBaseline = table.Column<bool>(type: "bit", nullable: false),
                    RequiresMediumTermTarget = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAnnualTarget = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88Indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88Indicators_C88CatalogueItems_IndicatorTypeItemId",
                        column: x => x.IndicatorTypeItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Indicators_C88CatalogueItems_OutcomeItemId",
                        column: x => x.OutcomeItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Indicators_C88CatalogueItems_SectorItemId",
                        column: x => x.SectorItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Indicators_C88CatalogueVersions_C88CatalogueVersionId",
                        column: x => x.C88CatalogueVersionId,
                        principalTable: "C88CatalogueVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Indicators_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88ReportingCalendars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88MunicipalityConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    ReportTypeItemId = table.Column<long>(type: "bigint", nullable: false),
                    ReportingPeriodId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    OpensAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88ReportingCalendars", x => x.Id);
                    table.CheckConstraint("CK_C88ReportingCalendars_Dates", "[ClosesAt] >= [OpensAt] AND [DueAt] >= [OpensAt]");
                    table.ForeignKey(
                        name: "FK_C88ReportingCalendars_C88CatalogueItems_ReportTypeItemId",
                        column: x => x.ReportTypeItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ReportingCalendars_C88MunicipalityConfigurations_C88MunicipalityConfigurationId",
                        column: x => x.C88MunicipalityConfigurationId,
                        principalTable: "C88MunicipalityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ReportingCalendars_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ReportingCalendars_ReportingPeriods_ReportingPeriodId",
                        column: x => x.ReportingPeriodId,
                        principalTable: "ReportingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88MunicipalityConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88WorkflowDefinitions", x => x.Id);
                    table.CheckConstraint("CK_C88WorkflowDefinitions_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_C88WorkflowDefinitions_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_C88WorkflowDefinitions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88WorkflowDefinitions_C88MunicipalityConfigurations_C88MunicipalityConfigurationId",
                        column: x => x.C88MunicipalityConfigurationId,
                        principalTable: "C88MunicipalityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88WorkflowDefinitions_C88WorkflowDefinitions_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "C88WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88WorkflowDefinitions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88Assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88MunicipalityConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88Assignments", x => x.Id);
                    table.CheckConstraint("CK_C88Assignments_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_C88Assignments_C88Indicators_C88IndicatorId",
                        column: x => x.C88IndicatorId,
                        principalTable: "C88Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Assignments_C88MunicipalityConfigurations_C88MunicipalityConfigurationId",
                        column: x => x.C88MunicipalityConfigurationId,
                        principalTable: "C88MunicipalityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Assignments_MunicipalEmployees_MunicipalEmployeeId",
                        column: x => x.MunicipalEmployeeId,
                        principalTable: "MunicipalEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88Assignments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88DataElements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88DataElements", x => x.Id);
                    table.CheckConstraint("CK_C88DataElements_Sequence", "[Sequence] > 0");
                    table.ForeignKey(
                        name: "FK_C88DataElements_C88Indicators_C88IndicatorId",
                        column: x => x.C88IndicatorId,
                        principalTable: "C88Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88DataElements_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88IndicatorApplicabilities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalCategoryItemId = table.Column<long>(type: "bigint", nullable: false),
                    ReadinessTierItemId = table.Column<long>(type: "bigint", nullable: true),
                    IsApplicable = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88IndicatorApplicabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88IndicatorApplicabilities_C88CatalogueItems_MunicipalCategoryItemId",
                        column: x => x.MunicipalCategoryItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorApplicabilities_C88CatalogueItems_ReadinessTierItemId",
                        column: x => x.ReadinessTierItemId,
                        principalTable: "C88CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorApplicabilities_C88Indicators_C88IndicatorId",
                        column: x => x.C88IndicatorId,
                        principalTable: "C88Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorApplicabilities_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88IndicatorPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88MunicipalityConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    BaselineValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MediumTermTarget = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AnnualTarget = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MissingDataExplanation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EstimatedAvailability = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88IndicatorPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88IndicatorPlans_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorPlans_C88Indicators_C88IndicatorId",
                        column: x => x.C88IndicatorId,
                        principalTable: "C88Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorPlans_C88MunicipalityConfigurations_C88MunicipalityConfigurationId",
                        column: x => x.C88MunicipalityConfigurationId,
                        principalTable: "C88MunicipalityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorPlans_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88OpmsMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88MunicipalityConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MappingType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88OpmsMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88OpmsMappings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88OpmsMappings_C88Indicators_C88IndicatorId",
                        column: x => x.C88IndicatorId,
                        principalTable: "C88Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88OpmsMappings_C88MunicipalityConfigurations_C88MunicipalityConfigurationId",
                        column: x => x.C88MunicipalityConfigurationId,
                        principalTable: "C88MunicipalityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88OpmsMappings_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88OpmsMappings_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88IndicatorReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88MunicipalityConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    C88ReportingCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorId = table.Column<long>(type: "bigint", nullable: false),
                    C88WorkflowDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CurrentStageSequence = table.Column<int>(type: "int", nullable: false),
                    CalculatedValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MissingDataExplanation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EstimatedAvailability = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FinalSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalSubmittedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88IndicatorReports", x => x.Id);
                    table.CheckConstraint("CK_C88IndicatorReports_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_AspNetUsers_FinalSubmittedByUserId",
                        column: x => x.FinalSubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_C88IndicatorReports_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "C88IndicatorReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_C88Indicators_C88IndicatorId",
                        column: x => x.C88IndicatorId,
                        principalTable: "C88Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_C88MunicipalityConfigurations_C88MunicipalityConfigurationId",
                        column: x => x.C88MunicipalityConfigurationId,
                        principalTable: "C88MunicipalityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_C88ReportingCalendars_C88ReportingCalendarId",
                        column: x => x.C88ReportingCalendarId,
                        principalTable: "C88ReportingCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_C88WorkflowDefinitions_C88WorkflowDefinitionId",
                        column: x => x.C88WorkflowDefinitionId,
                        principalTable: "C88WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88IndicatorReports_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88WorkflowStages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88WorkflowDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RequiredRole = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88WorkflowStages", x => x.Id);
                    table.CheckConstraint("CK_C88WorkflowStages_Sequence", "[Sequence] > 0");
                    table.ForeignKey(
                        name: "FK_C88WorkflowStages_C88WorkflowDefinitions_C88WorkflowDefinitionId",
                        column: x => x.C88WorkflowDefinitionId,
                        principalTable: "C88WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88WorkflowStages_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88ComplianceResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorReportId = table.Column<long>(type: "bigint", nullable: false),
                    C88ComplianceQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88ComplianceResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88ComplianceResponses_C88ComplianceQuestions_C88ComplianceQuestionId",
                        column: x => x.C88ComplianceQuestionId,
                        principalTable: "C88ComplianceQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ComplianceResponses_C88IndicatorReports_C88IndicatorReportId",
                        column: x => x.C88IndicatorReportId,
                        principalTable: "C88IndicatorReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88ComplianceResponses_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88DataElementValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorReportId = table.Column<long>(type: "bigint", nullable: false),
                    C88DataElementId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MissingDataExplanation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EstimatedAvailability = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88DataElementValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88DataElementValues_C88DataElements_C88DataElementId",
                        column: x => x.C88DataElementId,
                        principalTable: "C88DataElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88DataElementValues_C88IndicatorReports_C88IndicatorReportId",
                        column: x => x.C88IndicatorReportId,
                        principalTable: "C88IndicatorReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88DataElementValues_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "C88WorkflowActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    C88IndicatorReportId = table.Column<long>(type: "bigint", nullable: false),
                    FromStageSequence = table.Column<int>(type: "int", nullable: false),
                    ToStageSequence = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_C88WorkflowActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_C88WorkflowActions_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88WorkflowActions_C88IndicatorReports_C88IndicatorReportId",
                        column: x => x.C88IndicatorReportId,
                        principalTable: "C88IndicatorReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_C88WorkflowActions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_C88Assignments_C88IndicatorId",
                table: "C88Assignments",
                column: "C88IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_C88Assignments_C88MunicipalityConfigurationId_C88IndicatorId",
                table: "C88Assignments",
                columns: new[] { "C88MunicipalityConfigurationId", "C88IndicatorId" },
                unique: true,
                filter: "[Role] = 1 AND [IsActive] = 1 AND [EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_C88Assignments_C88MunicipalityConfigurationId_C88IndicatorId_MunicipalEmployeeId_Role_EffectiveFrom",
                table: "C88Assignments",
                columns: new[] { "C88MunicipalityConfigurationId", "C88IndicatorId", "MunicipalEmployeeId", "Role", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88Assignments_MunicipalEmployeeId",
                table: "C88Assignments",
                column: "MunicipalEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_C88Assignments_MunicipalityId",
                table: "C88Assignments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88Assignments_PublicId",
                table: "C88Assignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueItems_C88CatalogueVersionId_Kind_Code",
                table: "C88CatalogueItems",
                columns: new[] { "C88CatalogueVersionId", "Kind", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueItems_MunicipalityId",
                table: "C88CatalogueItems",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueItems_ParentItemId",
                table: "C88CatalogueItems",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueItems_PublicId",
                table: "C88CatalogueItems",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueVersions_CreatedByUserId",
                table: "C88CatalogueVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueVersions_MunicipalityId_Code",
                table: "C88CatalogueVersions",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88CatalogueVersions_PublicId",
                table: "C88CatalogueVersions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceQuestions_C88CatalogueVersionId_Code",
                table: "C88ComplianceQuestions",
                columns: new[] { "C88CatalogueVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceQuestions_MunicipalityId",
                table: "C88ComplianceQuestions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceQuestions_PublicId",
                table: "C88ComplianceQuestions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceQuestions_ReportTypeItemId",
                table: "C88ComplianceQuestions",
                column: "ReportTypeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceQuestions_ResponseTypeItemId",
                table: "C88ComplianceQuestions",
                column: "ResponseTypeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceResponses_C88ComplianceQuestionId",
                table: "C88ComplianceResponses",
                column: "C88ComplianceQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceResponses_C88IndicatorReportId_C88ComplianceQuestionId",
                table: "C88ComplianceResponses",
                columns: new[] { "C88IndicatorReportId", "C88ComplianceQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceResponses_MunicipalityId",
                table: "C88ComplianceResponses",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ComplianceResponses_PublicId",
                table: "C88ComplianceResponses",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElements_C88IndicatorId_Code",
                table: "C88DataElements",
                columns: new[] { "C88IndicatorId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElements_MunicipalityId",
                table: "C88DataElements",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElements_PublicId",
                table: "C88DataElements",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElementValues_C88DataElementId",
                table: "C88DataElementValues",
                column: "C88DataElementId");

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElementValues_C88IndicatorReportId_C88DataElementId",
                table: "C88DataElementValues",
                columns: new[] { "C88IndicatorReportId", "C88DataElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElementValues_MunicipalityId",
                table: "C88DataElementValues",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88DataElementValues_PublicId",
                table: "C88DataElementValues",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorApplicabilities_C88IndicatorId_MunicipalCategoryItemId_ReadinessTierItemId",
                table: "C88IndicatorApplicabilities",
                columns: new[] { "C88IndicatorId", "MunicipalCategoryItemId", "ReadinessTierItemId" },
                unique: true,
                filter: "[ReadinessTierItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorApplicabilities_MunicipalCategoryItemId",
                table: "C88IndicatorApplicabilities",
                column: "MunicipalCategoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorApplicabilities_MunicipalityId",
                table: "C88IndicatorApplicabilities",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorApplicabilities_PublicId",
                table: "C88IndicatorApplicabilities",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorApplicabilities_ReadinessTierItemId",
                table: "C88IndicatorApplicabilities",
                column: "ReadinessTierItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorPlans_C88IndicatorId",
                table: "C88IndicatorPlans",
                column: "C88IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorPlans_C88MunicipalityConfigurationId_C88IndicatorId",
                table: "C88IndicatorPlans",
                columns: new[] { "C88MunicipalityConfigurationId", "C88IndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorPlans_CreatedByUserId",
                table: "C88IndicatorPlans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorPlans_MunicipalityId",
                table: "C88IndicatorPlans",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorPlans_PublicId",
                table: "C88IndicatorPlans",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_C88IndicatorId",
                table: "C88IndicatorReports",
                column: "C88IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_C88MunicipalityConfigurationId",
                table: "C88IndicatorReports",
                column: "C88MunicipalityConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_C88ReportingCalendarId_C88IndicatorId_IsCurrent",
                table: "C88IndicatorReports",
                columns: new[] { "C88ReportingCalendarId", "C88IndicatorId", "IsCurrent" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_C88WorkflowDefinitionId",
                table: "C88IndicatorReports",
                column: "C88WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_CreatedByUserId",
                table: "C88IndicatorReports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_FinalSubmittedByUserId",
                table: "C88IndicatorReports",
                column: "FinalSubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_MunicipalityId_ReportFamilyId_VersionNumber",
                table: "C88IndicatorReports",
                columns: new[] { "MunicipalityId", "ReportFamilyId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_PreviousVersionId",
                table: "C88IndicatorReports",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_PublicId",
                table: "C88IndicatorReports",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88IndicatorReports_ReportFamilyId",
                table: "C88IndicatorReports",
                column: "ReportFamilyId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_C88Indicators_C88CatalogueVersionId_Code",
                table: "C88Indicators",
                columns: new[] { "C88CatalogueVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88Indicators_IndicatorTypeItemId",
                table: "C88Indicators",
                column: "IndicatorTypeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88Indicators_MunicipalityId",
                table: "C88Indicators",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88Indicators_OutcomeItemId",
                table: "C88Indicators",
                column: "OutcomeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88Indicators_PublicId",
                table: "C88Indicators",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88Indicators_SectorItemId",
                table: "C88Indicators",
                column: "SectorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88MunicipalityConfigurations_C88CatalogueVersionId",
                table: "C88MunicipalityConfigurations",
                column: "C88CatalogueVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_C88MunicipalityConfigurations_CreatedByUserId",
                table: "C88MunicipalityConfigurations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88MunicipalityConfigurations_MunicipalityFinancialYearId",
                table: "C88MunicipalityConfigurations",
                column: "MunicipalityFinancialYearId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88MunicipalityConfigurations_MunicipalityId",
                table: "C88MunicipalityConfigurations",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88MunicipalityConfigurations_PublicId",
                table: "C88MunicipalityConfigurations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88OpmsMappings_C88IndicatorId",
                table: "C88OpmsMappings",
                column: "C88IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_C88OpmsMappings_C88MunicipalityConfigurationId_C88IndicatorId_OpmsTargetId",
                table: "C88OpmsMappings",
                columns: new[] { "C88MunicipalityConfigurationId", "C88IndicatorId", "OpmsTargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88OpmsMappings_CreatedByUserId",
                table: "C88OpmsMappings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88OpmsMappings_MunicipalityId",
                table: "C88OpmsMappings",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88OpmsMappings_OpmsTargetId",
                table: "C88OpmsMappings",
                column: "OpmsTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_C88OpmsMappings_PublicId",
                table: "C88OpmsMappings",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ReportingCalendars_C88MunicipalityConfigurationId_Code",
                table: "C88ReportingCalendars",
                columns: new[] { "C88MunicipalityConfigurationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ReportingCalendars_MunicipalityId",
                table: "C88ReportingCalendars",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ReportingCalendars_PublicId",
                table: "C88ReportingCalendars",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88ReportingCalendars_ReportingPeriodId",
                table: "C88ReportingCalendars",
                column: "ReportingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_C88ReportingCalendars_ReportTypeItemId",
                table: "C88ReportingCalendars",
                column: "ReportTypeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowActions_ActorUserId",
                table: "C88WorkflowActions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowActions_C88IndicatorReportId_OccurredAt",
                table: "C88WorkflowActions",
                columns: new[] { "C88IndicatorReportId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowActions_MunicipalityId",
                table: "C88WorkflowActions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowActions_PublicId",
                table: "C88WorkflowActions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowDefinitions_C88MunicipalityConfigurationId",
                table: "C88WorkflowDefinitions",
                column: "C88MunicipalityConfigurationId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowDefinitions_C88MunicipalityConfigurationId_VersionNumber",
                table: "C88WorkflowDefinitions",
                columns: new[] { "C88MunicipalityConfigurationId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowDefinitions_CreatedByUserId",
                table: "C88WorkflowDefinitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowDefinitions_MunicipalityId",
                table: "C88WorkflowDefinitions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowDefinitions_PreviousVersionId",
                table: "C88WorkflowDefinitions",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowDefinitions_PublicId",
                table: "C88WorkflowDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowStages_C88WorkflowDefinitionId_Sequence",
                table: "C88WorkflowStages",
                columns: new[] { "C88WorkflowDefinitionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowStages_MunicipalityId",
                table: "C88WorkflowStages",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_C88WorkflowStages_PublicId",
                table: "C88WorkflowStages",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "C88Assignments");

            migrationBuilder.DropTable(
                name: "C88ComplianceResponses");

            migrationBuilder.DropTable(
                name: "C88DataElementValues");

            migrationBuilder.DropTable(
                name: "C88IndicatorApplicabilities");

            migrationBuilder.DropTable(
                name: "C88IndicatorPlans");

            migrationBuilder.DropTable(
                name: "C88OpmsMappings");

            migrationBuilder.DropTable(
                name: "C88WorkflowActions");

            migrationBuilder.DropTable(
                name: "C88WorkflowStages");

            migrationBuilder.DropTable(
                name: "C88ComplianceQuestions");

            migrationBuilder.DropTable(
                name: "C88DataElements");

            migrationBuilder.DropTable(
                name: "C88IndicatorReports");

            migrationBuilder.DropTable(
                name: "C88Indicators");

            migrationBuilder.DropTable(
                name: "C88ReportingCalendars");

            migrationBuilder.DropTable(
                name: "C88WorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "C88CatalogueItems");

            migrationBuilder.DropTable(
                name: "C88MunicipalityConfigurations");

            migrationBuilder.DropTable(
                name: "C88CatalogueVersions");
        }
    }
}

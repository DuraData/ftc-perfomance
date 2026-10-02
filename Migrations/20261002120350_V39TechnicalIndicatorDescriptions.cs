using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39TechnicalIndicatorDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TidAllKpisRequired",
                table: "Municipalities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TidEnabled",
                table: "Municipalities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TechnicalIndicatorDescriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    IndicatorDefinition = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CollectionMethod = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CalculationMethod = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NumeratorDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenominatorDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Limitations = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Assumptions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    VerificationMethod = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ResponsibleEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalIndicatorDescriptions", x => x.Id);
                    table.CheckConstraint("CK_TechnicalIndicatorDescriptions_Current", "[IsCurrent] = 0 OR [EffectiveTo] IS NULL");
                    table.CheckConstraint("CK_TechnicalIndicatorDescriptions_EffectivePeriod", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_TechnicalIndicatorDescriptions_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_TechnicalIndicatorDescriptions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalIndicatorDescriptions_MunicipalEmployees_ResponsibleEmployeeId",
                        column: x => x.ResponsibleEmployeeId,
                        principalTable: "MunicipalEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalIndicatorDescriptions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalIndicatorDescriptions_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalIndicatorDescriptions_TechnicalIndicatorDescriptions_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "TechnicalIndicatorDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TidSourceDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    TechnicalIndicatorDescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    EvidenceBlobId = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TidSourceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TidSourceDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TidSourceDocuments_EvidenceBlobs_EvidenceBlobId",
                        column: x => x.EvidenceBlobId,
                        principalTable: "EvidenceBlobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TidSourceDocuments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TidSourceDocuments_TechnicalIndicatorDescriptions_TechnicalIndicatorDescriptionId",
                        column: x => x.TechnicalIndicatorDescriptionId,
                        principalTable: "TechnicalIndicatorDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_CreatedByUserId",
                table: "TechnicalIndicatorDescriptions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_MunicipalityId",
                table: "TechnicalIndicatorDescriptions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_OpmsTargetId",
                table: "TechnicalIndicatorDescriptions",
                column: "OpmsTargetId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_OpmsTargetId_VersionNumber",
                table: "TechnicalIndicatorDescriptions",
                columns: new[] { "OpmsTargetId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_PreviousVersionId",
                table: "TechnicalIndicatorDescriptions",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_PublicId",
                table: "TechnicalIndicatorDescriptions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIndicatorDescriptions_ResponsibleEmployeeId",
                table: "TechnicalIndicatorDescriptions",
                column: "ResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TidSourceDocuments_EvidenceBlobId",
                table: "TidSourceDocuments",
                column: "EvidenceBlobId");

            migrationBuilder.CreateIndex(
                name: "IX_TidSourceDocuments_MunicipalityId",
                table: "TidSourceDocuments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_TidSourceDocuments_PublicId",
                table: "TidSourceDocuments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TidSourceDocuments_TechnicalIndicatorDescriptionId_EvidenceBlobId",
                table: "TidSourceDocuments",
                columns: new[] { "TechnicalIndicatorDescriptionId", "EvidenceBlobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TidSourceDocuments_UploadedByUserId",
                table: "TidSourceDocuments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TidSourceDocuments");

            migrationBuilder.DropTable(
                name: "TechnicalIndicatorDescriptions");

            migrationBuilder.DropColumn(
                name: "TidAllKpisRequired",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "TidEnabled",
                table: "Municipalities");
        }
    }
}

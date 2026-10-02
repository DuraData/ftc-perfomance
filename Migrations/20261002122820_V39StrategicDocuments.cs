using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39StrategicDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StrategicDocumentTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AllowsExternalLinks = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicDocumentTypes", x => x.Id);
                    table.CheckConstraint("CK_StrategicDocumentTypes_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_StrategicDocumentTypes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocumentTypes_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategicDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicDocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousVersionId = table.Column<long>(type: "bigint", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    SdbipLayer = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvidenceBlobId = table.Column<string>(type: "nvarchar(64)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovalReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicDocuments", x => x.Id);
                    table.CheckConstraint("CK_StrategicDocuments_Content", "([EvidenceBlobId] IS NOT NULL AND [ExternalUrl] IS NULL) OR ([EvidenceBlobId] IS NULL AND [ExternalUrl] IS NOT NULL)");
                    table.CheckConstraint("CK_StrategicDocuments_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_StrategicDocuments_Publication", "[IsPublished] = 0 OR [IsApproved] = 1");
                    table.CheckConstraint("CK_StrategicDocuments_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_AspNetUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_EvidenceBlobs_EvidenceBlobId",
                        column: x => x.EvidenceBlobId,
                        principalTable: "EvidenceBlobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_StrategicDocumentTypes_StrategicDocumentTypeId",
                        column: x => x.StrategicDocumentTypeId,
                        principalTable: "StrategicDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocuments_StrategicDocuments_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "StrategicDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategicDocumentEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    StrategicDocumentId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicDocumentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicDocumentEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocumentEvents_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategicDocumentEvents_StrategicDocuments_StrategicDocumentId",
                        column: x => x.StrategicDocumentId,
                        principalTable: "StrategicDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentEvents_ActorUserId",
                table: "StrategicDocumentEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentEvents_MunicipalityId",
                table: "StrategicDocumentEvents",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentEvents_PublicId",
                table: "StrategicDocumentEvents",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentEvents_StrategicDocumentId_OccurredAt",
                table: "StrategicDocumentEvents",
                columns: new[] { "StrategicDocumentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_ApprovedByUserId",
                table: "StrategicDocuments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_CreatedByUserId",
                table: "StrategicDocuments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_DocumentFamilyId",
                table: "StrategicDocuments",
                column: "DocumentFamilyId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_EvidenceBlobId",
                table: "StrategicDocuments",
                column: "EvidenceBlobId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_MunicipalityFinancialYearId_IsActive_IsApproved_IsPublished",
                table: "StrategicDocuments",
                columns: new[] { "MunicipalityFinancialYearId", "IsActive", "IsApproved", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_MunicipalityId_DocumentFamilyId_VersionNumber",
                table: "StrategicDocuments",
                columns: new[] { "MunicipalityId", "DocumentFamilyId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_PreviousVersionId",
                table: "StrategicDocuments",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_PublicId",
                table: "StrategicDocuments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_PublishedByUserId",
                table: "StrategicDocuments",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocuments_StrategicDocumentTypeId",
                table: "StrategicDocuments",
                column: "StrategicDocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentTypes_CreatedByUserId",
                table: "StrategicDocumentTypes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentTypes_MunicipalityId_Code",
                table: "StrategicDocumentTypes",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicDocumentTypes_PublicId",
                table: "StrategicDocumentTypes",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrategicDocumentEvents");

            migrationBuilder.DropTable(
                name: "StrategicDocuments");

            migrationBuilder.DropTable(
                name: "StrategicDocumentTypes");
        }
    }
}

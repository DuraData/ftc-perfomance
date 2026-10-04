using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedSdbipImportReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpmsImportBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    SdbipLayerId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    NewRows = table.Column<int>(type: "int", nullable: false),
                    UnchangedRows = table.Column<int>(type: "int", nullable: false),
                    ChangedRows = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommittedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CommittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsImportBatches", x => x.Id);
                    table.CheckConstraint("CK_OpmsImportBatches_CommitMetadata", "[Status] <> 1 OR ([CommittedAt] IS NOT NULL AND [CommittedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_OpmsImportBatches_RowCounts", "[TotalRows] > 0 AND [TotalRows] = [NewRows] + [UnchangedRows] + [ChangedRows] + [InvalidRows]");
                    table.ForeignKey(
                        name: "FK_OpmsImportBatches_AspNetUsers_CommittedByUserId",
                        column: x => x.CommittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsImportBatches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsImportBatches_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpmsImportBatches_SdbipLayers_SdbipLayerId",
                        column: x => x.SdbipLayerId,
                        principalTable: "SdbipLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpmsImportRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpmsImportBatchId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedEntityRowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ErrorPeriod = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ErrorField = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SuppliedValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpmsImportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpmsImportRows_OpmsImportBatches_OpmsImportBatchId",
                        column: x => x.OpmsImportBatchId,
                        principalTable: "OpmsImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportBatches_CommittedByUserId",
                table: "OpmsImportBatches",
                column: "CommittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportBatches_CreatedByUserId",
                table: "OpmsImportBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportBatches_MunicipalityId_ClientRequestId",
                table: "OpmsImportBatches",
                columns: new[] { "MunicipalityId", "ClientRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportBatches_PublicId",
                table: "OpmsImportBatches",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportBatches_SdbipLayerId_CreatedAt",
                table: "OpmsImportBatches",
                columns: new[] { "SdbipLayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportRows_OpmsImportBatchId_SourceRowNumber",
                table: "OpmsImportRows",
                columns: new[] { "OpmsImportBatchId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsImportRows_PublicId",
                table: "OpmsImportRows",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpmsImportRows");

            migrationBuilder.DropTable(
                name: "OpmsImportBatches");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39IdpKpiImportReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpKpis",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpKpis",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "IdpImportBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IdpPlanId = table.Column<int>(type: "int", nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_IdpImportBatches", x => x.Id);
                    table.CheckConstraint("CK_IdpImportBatches_CommitMetadata", "[Status] <> 1 OR ([CommittedAt] IS NOT NULL AND [CommittedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_IdpImportBatches_RowCounts", "[TotalRows] > 0 AND [TotalRows] = [NewRows] + [UnchangedRows] + [ChangedRows] + [InvalidRows]");
                    table.ForeignKey(
                        name: "FK_IdpImportBatches_AspNetUsers_CommittedByUserId",
                        column: x => x.CommittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpImportBatches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpImportBatches_IdpPlans_IdpPlanId",
                        column: x => x.IdpPlanId,
                        principalTable: "IdpPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdpImportBatches_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdpImportRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdpImportBatchId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedEntityRowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ErrorField = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SuppliedValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpImportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdpImportRows_IdpImportBatches_IdpImportBatchId",
                        column: x => x.IdpImportBatchId,
                        principalTable: "IdpImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            if (ActiveProvider.Contains("SqlServer"))
            {
                migrationBuilder.Sql("UPDATE [IdpKpis] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
            }
            else
            {
                migrationBuilder.Sql("UPDATE \"IdpKpis\" SET \"PublicId\" = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(6))) WHERE \"PublicId\" IS NULL;");
            }

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "IdpKpis",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpKpis_PublicId",
                table: "IdpKpis",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportBatches_CommittedByUserId",
                table: "IdpImportBatches",
                column: "CommittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportBatches_CreatedByUserId",
                table: "IdpImportBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportBatches_IdpPlanId_CreatedAt",
                table: "IdpImportBatches",
                columns: new[] { "IdpPlanId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportBatches_MunicipalityId_ClientRequestId",
                table: "IdpImportBatches",
                columns: new[] { "MunicipalityId", "ClientRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportBatches_PublicId",
                table: "IdpImportBatches",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportRows_IdpImportBatchId_SourceRowNumber",
                table: "IdpImportRows",
                columns: new[] { "IdpImportBatchId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpImportRows_PublicId",
                table: "IdpImportRows",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdpImportRows");

            migrationBuilder.DropTable(
                name: "IdpImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_IdpKpis_PublicId",
                table: "IdpKpis");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpKpis");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpKpis");
        }
    }
}

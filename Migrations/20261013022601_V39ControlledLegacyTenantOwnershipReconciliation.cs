using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39ControlledLegacyTenantOwnershipReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            var idType = sqlServer ? "bigint" : "INTEGER";
            var guidType = sqlServer ? "uniqueidentifier" : "TEXT";
            var userIdType = sqlServer ? "nvarchar(450)" : "TEXT";
            var entityType = sqlServer ? "nvarchar(80)" : "TEXT";
            var evidenceType = sqlServer ? "nvarchar(2000)" : "TEXT";
            var reasonType = sqlServer ? "nvarchar(1000)" : "TEXT";
            var correlationType = sqlServer ? "nvarchar(100)" : "TEXT";
            var dateTimeType = sqlServer ? "datetime2" : "TEXT";

            migrationBuilder.CreateTable(
                name: "LegacyTenantOwnershipReconciliations",
                columns: table => new
                {
                    Id = table.Column<long>(type: idType, nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    PublicId = table.Column<Guid>(type: guidType, nullable: false),
                    MunicipalityId = table.Column<long>(type: idType, nullable: false),
                    EntityType = table.Column<string>(type: entityType, maxLength: 80, nullable: false),
                    EntityPublicId = table.Column<Guid>(type: guidType, nullable: false),
                    SourceEvidence = table.Column<string>(type: evidenceType, maxLength: 2000, nullable: false),
                    ReconciledByUserId = table.Column<string>(type: userIdType, nullable: false),
                    ReconciledAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    Reason = table.Column<string>(type: reasonType, maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: correlationType, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyTenantOwnershipReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyTenantOwnershipReconciliations_AspNetUsers_ReconciledByUserId",
                        column: x => x.ReconciledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegacyTenantOwnershipReconciliations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTenantOwnershipReconciliations_EntityType_EntityPublicId",
                table: "LegacyTenantOwnershipReconciliations",
                columns: new[] { "EntityType", "EntityPublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTenantOwnershipReconciliations_MunicipalityId_ReconciledAt",
                table: "LegacyTenantOwnershipReconciliations",
                columns: new[] { "MunicipalityId", "ReconciledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTenantOwnershipReconciliations_PublicId",
                table: "LegacyTenantOwnershipReconciliations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTenantOwnershipReconciliations_ReconciledByUserId",
                table: "LegacyTenantOwnershipReconciliations",
                column: "ReconciledByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyTenantOwnershipReconciliations");
        }
    }
}

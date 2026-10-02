using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedIdpDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdpDocuments_IdpPlans_IdpPlanId",
                table: "IdpDocuments");

            migrationBuilder.DropIndex(
                name: "IX_IdpDocuments_IdpPlanId",
                table: "IdpDocuments");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "IdpDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuarantined",
                table: "IdpDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpDocuments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "RetainUntil",
                table: "IdpDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpDocuments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ScanDetail",
                table: "IdpDocuments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                table: "IdpDocuments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScannedAt",
                table: "IdpDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannerProvider",
                table: "IdpDocuments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannerReference",
                table: "IdpDocuments",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "IdpDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SignatureVerified",
                table: "IdpDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Existing metadata-only rows have no independently verified content. Give each row
            // a non-enumerable identifier, retain it, and fail closed until an authorized rescan.
            migrationBuilder.Sql(
                """
                UPDATE [IdpDocuments]
                SET [PublicId] = NEWID(),
                    [IsActive] = 1,
                    [IsQuarantined] = 1,
                    [SignatureVerified] = 0,
                    [ScanStatus] = N'LegacyUnverified',
                    [ScanDetail] = N'Legacy document metadata requires governed content validation and malware scanning.',
                    [RetainUntil] = DATEADD(year, 7, [UploadedAt]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_IdpDocuments_IdpPlanId_Sha256",
                table: "IdpDocuments",
                columns: new[] { "IdpPlanId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_IdpDocuments_PublicId",
                table: "IdpDocuments",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpDocuments_IdpPlans_IdpPlanId",
                table: "IdpDocuments",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdpDocuments_IdpPlans_IdpPlanId",
                table: "IdpDocuments");

            migrationBuilder.DropIndex(
                name: "IX_IdpDocuments_IdpPlanId_Sha256",
                table: "IdpDocuments");

            migrationBuilder.DropIndex(
                name: "IX_IdpDocuments_PublicId",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "IsQuarantined",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "RetainUntil",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "ScanDetail",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "ScannedAt",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "ScannerProvider",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "ScannerReference",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "IdpDocuments");

            migrationBuilder.DropColumn(
                name: "SignatureVerified",
                table: "IdpDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_IdpDocuments_IdpPlanId",
                table: "IdpDocuments",
                column: "IdpPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_IdpDocuments_IdpPlans_IdpPlanId",
                table: "IdpDocuments",
                column: "IdpPlanId",
                principalTable: "IdpPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39EvidenceIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PoeFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuarantined",
                table: "PoeFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "PoeFiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "PoeFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "RetainUntil",
                table: "PoeFiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PoeFiles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                table: "PoeFiles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "PoeFiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SignatureVerified",
                table: "PoeFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SupersedesPoeFileId",
                table: "PoeFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [PoeFiles]
                SET [PublicId] = NEWID(),
                    [IsActive] = 1,
                    [IsQuarantined] = 1,
                    [ScanStatus] = N'LegacyUnverified',
                    [Sha256] = LEFT(CONCAT(N'legacy-', [Id]), 64),
                    [RetainUntil] = DATEADD(year, 7, [UploadedAt]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PoeFiles_MunicipalityId_Sha256",
                table: "PoeFiles",
                columns: new[] { "MunicipalityId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_PoeFiles_PublicId",
                table: "PoeFiles",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PoeFiles_Municipalities_MunicipalityId",
                table: "PoeFiles",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PoeFiles_Municipalities_MunicipalityId",
                table: "PoeFiles");

            migrationBuilder.DropIndex(
                name: "IX_PoeFiles_MunicipalityId_Sha256",
                table: "PoeFiles");

            migrationBuilder.DropIndex(
                name: "IX_PoeFiles_PublicId",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "IsQuarantined",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "RetainUntil",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "SignatureVerified",
                table: "PoeFiles");

            migrationBuilder.DropColumn(
                name: "SupersedesPoeFileId",
                table: "PoeFiles");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39IdpTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdpPlans_PlanCode",
                table: "IdpPlans");

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "IdpPlans",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IdpPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IdpPlans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql(
                """
                UPDATE p
                SET p.[PublicId] = NEWID(),
                    p.[MunicipalityId] = u.[MunicipalityId]
                FROM [IdpPlans] p
                INNER JOIN [AspNetUsers] u ON u.[Id] = p.[CreatedByUserId];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_MunicipalityId_PlanCode",
                table: "IdpPlans",
                columns: new[] { "MunicipalityId", "PlanCode" },
                unique: true,
                filter: "[MunicipalityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_PublicId",
                table: "IdpPlans",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IdpPlans_Municipalities_MunicipalityId",
                table: "IdpPlans",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdpPlans_Municipalities_MunicipalityId",
                table: "IdpPlans");

            migrationBuilder.DropIndex(
                name: "IX_IdpPlans_MunicipalityId_PlanCode",
                table: "IdpPlans");

            migrationBuilder.DropIndex(
                name: "IX_IdpPlans_PublicId",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IdpPlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdpPlans");

            migrationBuilder.CreateIndex(
                name: "IX_IdpPlans_PlanCode",
                table: "IdpPlans",
                column: "PlanCode",
                unique: true);
        }
    }
}

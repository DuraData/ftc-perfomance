using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39AppendPreservedKpiBudgetSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllocatedByUserId",
                table: "OPMS_KPIBudgetSources",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "OPMS_KPIBudgetSources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "OPMS_KPIBudgetSources",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "OPMS_KPIBudgetSources",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "IpmsTargets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllocatedByUserId",
                table: "IPMS_KPIBudgetSources",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "IPMS_KPIBudgetSources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "IPMS_KPIBudgetSources",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "IPMS_KPIBudgetSources",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [OPMS_KPIBudgetSources]
                SET [DeactivatedAt] = [CreatedAt],
                    [DeactivationReason] = 'Legacy inactive allocation migrated without historic actor evidence.'
                WHERE [IsActive] = 0;

                UPDATE [IPMS_KPIBudgetSources]
                SET [DeactivatedAt] = [CreatedAt],
                    [DeactivationReason] = 'Legacy inactive allocation migrated without historic actor evidence.'
                WHERE [IsActive] = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIBudgetSources_AllocatedByUserId",
                table: "OPMS_KPIBudgetSources",
                column: "AllocatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OPMS_KPIBudgetSources_DeactivatedByUserId",
                table: "OPMS_KPIBudgetSources",
                column: "DeactivatedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OPMS_KPIBudgetSources_DeactivationLifecycle",
                table: "OPMS_KPIBudgetSources",
                sql: "([IsActive] = 1 AND [DeactivatedAt] IS NULL AND [DeactivatedByUserId] IS NULL AND [DeactivationReason] IS NULL) OR ([IsActive] = 0 AND [DeactivatedAt] IS NOT NULL AND [DeactivationReason] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_KPIBudgetSources_AllocatedByUserId",
                table: "IPMS_KPIBudgetSources",
                column: "AllocatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_KPIBudgetSources_DeactivatedByUserId",
                table: "IPMS_KPIBudgetSources",
                column: "DeactivatedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IPMS_KPIBudgetSources_DeactivationLifecycle",
                table: "IPMS_KPIBudgetSources",
                sql: "([IsActive] = 1 AND [DeactivatedAt] IS NULL AND [DeactivatedByUserId] IS NULL AND [DeactivationReason] IS NULL) OR ([IsActive] = 0 AND [DeactivatedAt] IS NOT NULL AND [DeactivationReason] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_IPMS_KPIBudgetSources_AspNetUsers_AllocatedByUserId",
                table: "IPMS_KPIBudgetSources",
                column: "AllocatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IPMS_KPIBudgetSources_AspNetUsers_DeactivatedByUserId",
                table: "IPMS_KPIBudgetSources",
                column: "DeactivatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OPMS_KPIBudgetSources_AspNetUsers_AllocatedByUserId",
                table: "OPMS_KPIBudgetSources",
                column: "AllocatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OPMS_KPIBudgetSources_AspNetUsers_DeactivatedByUserId",
                table: "OPMS_KPIBudgetSources",
                column: "DeactivatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IPMS_KPIBudgetSources_AspNetUsers_AllocatedByUserId",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropForeignKey(
                name: "FK_IPMS_KPIBudgetSources_AspNetUsers_DeactivatedByUserId",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropForeignKey(
                name: "FK_OPMS_KPIBudgetSources_AspNetUsers_AllocatedByUserId",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropForeignKey(
                name: "FK_OPMS_KPIBudgetSources_AspNetUsers_DeactivatedByUserId",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropIndex(
                name: "IX_OPMS_KPIBudgetSources_AllocatedByUserId",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropIndex(
                name: "IX_OPMS_KPIBudgetSources_DeactivatedByUserId",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OPMS_KPIBudgetSources_DeactivationLifecycle",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropIndex(
                name: "IX_IPMS_KPIBudgetSources_AllocatedByUserId",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropIndex(
                name: "IX_IPMS_KPIBudgetSources_DeactivatedByUserId",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IPMS_KPIBudgetSources_DeactivationLifecycle",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "AllocatedByUserId",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "OPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "AllocatedByUserId",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "IPMS_KPIBudgetSources");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "IPMS_KPIBudgetSources");
        }
    }
}

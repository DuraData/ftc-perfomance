using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39TenantScopedPagedAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs");

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "LoginAuditLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE loginLog
                SET MunicipalityId = tenantUser.MunicipalityId
                FROM LoginAuditLogs AS loginLog
                INNER JOIN AspNetUsers AS tenantUser ON tenantUser.Id = loginLog.UserId
                WHERE loginLog.MunicipalityId IS NULL
                  AND tenantUser.MunicipalityId IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAuditLogs_MunicipalityId_LoggedAt",
                table: "LoginAuditLogs",
                columns: new[] { "MunicipalityId", "LoggedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginAuditLogs_Municipalities_MunicipalityId",
                table: "LoginAuditLogs",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_LoginAuditLogs_Municipalities_MunicipalityId",
                table: "LoginAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_LoginAuditLogs_MunicipalityId_LoggedAt",
                table: "LoginAuditLogs");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "LoginAuditLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

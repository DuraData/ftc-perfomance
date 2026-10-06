using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedSecurityHistoryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            var guidType = sqlServer ? "uniqueidentifier" : "TEXT";

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "UserScopes",
                type: guidType,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "UserAssignments",
                type: guidType,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "SecurityUserRoleAssignments",
                type: guidType,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "LoginAuditLogs",
                type: guidType,
                nullable: true);

            if (sqlServer)
            {
                migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "UserScopes", type: "rowversion", rowVersion: true, nullable: false, defaultValue: new byte[0]);
                migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "UserAssignments", type: "rowversion", rowVersion: true, nullable: false, defaultValue: new byte[0]);
                migrationBuilder.Sql("UPDATE [UserScopes] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [UserAssignments] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [SecurityUserRoleAssignments] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql("UPDATE [LoginAuditLogs] SET [PublicId] = NEWID() WHERE [PublicId] IS NULL;");
            }
            else
            {
                migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "UserScopes", type: "BLOB", nullable: false, defaultValue: Array.Empty<byte>());
                migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "UserAssignments", type: "BLOB", nullable: false, defaultValue: Array.Empty<byte>());
                const string sqliteGuid = "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";
                migrationBuilder.Sql($"UPDATE [UserScopes] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [UserAssignments] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [SecurityUserRoleAssignments] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
                migrationBuilder.Sql($"UPDATE [LoginAuditLogs] SET [PublicId] = {sqliteGuid} WHERE [PublicId] IS NULL;");
            }

            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "UserScopes", type: guidType, nullable: false, oldClrType: typeof(Guid), oldType: guidType, oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "UserAssignments", type: guidType, nullable: false, oldClrType: typeof(Guid), oldType: guidType, oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "SecurityUserRoleAssignments", type: guidType, nullable: false, oldClrType: typeof(Guid), oldType: guidType, oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PublicId", table: "LoginAuditLogs", type: guidType, nullable: false, oldClrType: typeof(Guid), oldType: guidType, oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_PublicId",
                table: "UserScopes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignments_PublicId",
                table: "UserAssignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUserRoleAssignments_PublicId",
                table: "SecurityUserRoleAssignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAuditLogs_PublicId",
                table: "LoginAuditLogs",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserScopes_PublicId",
                table: "UserScopes");

            migrationBuilder.DropIndex(
                name: "IX_UserAssignments_PublicId",
                table: "UserAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SecurityUserRoleAssignments_PublicId",
                table: "SecurityUserRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_LoginAuditLogs_PublicId",
                table: "LoginAuditLogs");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "UserScopes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserScopes");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "UserAssignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserAssignments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "SecurityUserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "LoginAuditLogs");
        }
    }
}

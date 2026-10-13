using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39RestrictiveRemainingAggregateHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                CreateSqliteRestrictTriggers(migrationBuilder);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationPolicies_AuthenticationConfigurations_AuthenticationConfigurationId",
                table: "AuthenticationPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsImportRows_OpmsImportBatches_OpmsImportBatchId",
                table: "OpmsImportRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_AspNetRoles_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_StrategicObjectives_StrategicGoals_StrategicGoalId",
                table: "StrategicObjectives");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Departments_DepartmentId",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAssignments_AspNetUsers_UserId",
                table: "UserAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissionOverrides_AspNetUsers_UserId",
                table: "UserPermissionOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissionOverrides_Permissions_PermissionId",
                table: "UserPermissionOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_UserScopes_AspNetUsers_UserId",
                table: "UserScopes");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationPolicies_AuthenticationConfigurations_AuthenticationConfigurationId",
                table: "AuthenticationPolicies",
                column: "AuthenticationConfigurationId",
                principalTable: "AuthenticationConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsImportRows_OpmsImportBatches_OpmsImportBatchId",
                table: "OpmsImportRows",
                column: "OpmsImportBatchId",
                principalTable: "OpmsImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_AspNetRoles_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StrategicObjectives_StrategicGoals_StrategicGoalId",
                table: "StrategicObjectives",
                column: "StrategicGoalId",
                principalTable: "StrategicGoals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Departments_DepartmentId",
                table: "Units",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAssignments_AspNetUsers_UserId",
                table: "UserAssignments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissionOverrides_AspNetUsers_UserId",
                table: "UserPermissionOverrides",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissionOverrides_Permissions_PermissionId",
                table: "UserPermissionOverrides",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserScopes_AspNetUsers_UserId",
                table: "UserScopes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", System.StringComparison.OrdinalIgnoreCase))
            {
                DropSqliteRestrictTriggers(migrationBuilder);
                return;
            }

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationPolicies_AuthenticationConfigurations_AuthenticationConfigurationId",
                table: "AuthenticationPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsImportRows_OpmsImportBatches_OpmsImportBatchId",
                table: "OpmsImportRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_AspNetRoles_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_StrategicObjectives_StrategicGoals_StrategicGoalId",
                table: "StrategicObjectives");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Departments_DepartmentId",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAssignments_AspNetUsers_UserId",
                table: "UserAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissionOverrides_AspNetUsers_UserId",
                table: "UserPermissionOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissionOverrides_Permissions_PermissionId",
                table: "UserPermissionOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_UserScopes_AspNetUsers_UserId",
                table: "UserScopes");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationPolicies_AuthenticationConfigurations_AuthenticationConfigurationId",
                table: "AuthenticationPolicies",
                column: "AuthenticationConfigurationId",
                principalTable: "AuthenticationConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginAuditLogs_AspNetUsers_UserId",
                table: "LoginAuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsImportRows_OpmsImportBatches_OpmsImportBatchId",
                table: "OpmsImportRows",
                column: "OpmsImportBatchId",
                principalTable: "OpmsImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_AspNetRoles_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StrategicObjectives_StrategicGoals_StrategicGoalId",
                table: "StrategicObjectives",
                column: "StrategicGoalId",
                principalTable: "StrategicGoals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Departments_DepartmentId",
                table: "Units",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAssignments_AspNetUsers_UserId",
                table: "UserAssignments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissionOverrides_AspNetUsers_UserId",
                table: "UserPermissionOverrides",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissionOverrides_Permissions_PermissionId",
                table: "UserPermissionOverrides",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserScopes_AspNetUsers_UserId",
                table: "UserScopes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void CreateSqliteRestrictTriggers(MigrationBuilder migrationBuilder)
        {
            foreach (var relationship in GovernedRelationships())
            {
                migrationBuilder.Sql($$"""
                    CREATE TRIGGER "{{relationship.TriggerName}}"
                    BEFORE DELETE ON "{{relationship.ParentTable}}"
                    FOR EACH ROW
                    WHEN EXISTS (
                        SELECT 1 FROM "{{relationship.ChildTable}}"
                        WHERE "{{relationship.ForeignKey}}" = OLD."Id"
                    )
                    BEGIN
                        SELECT RAISE(ABORT, 'Governed history restricts parent deletion.');
                    END;
                    """);
            }
        }

        private static void DropSqliteRestrictTriggers(MigrationBuilder migrationBuilder)
        {
            foreach (var relationship in GovernedRelationships())
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS \"{relationship.TriggerName}\";");
        }

        private static (string TriggerName, string ChildTable, string ParentTable, string ForeignKey)[] GovernedRelationships() =>
        [
            ("TRG_AspNetRoles_AspNetRoleClaims_RestrictDelete", "AspNetRoleClaims", "AspNetRoles", "RoleId"),
            ("TRG_AspNetUsers_AspNetUserClaims_RestrictDelete", "AspNetUserClaims", "AspNetUsers", "UserId"),
            ("TRG_AspNetUsers_AspNetUserLogins_RestrictDelete", "AspNetUserLogins", "AspNetUsers", "UserId"),
            ("TRG_AspNetRoles_AspNetUserRoles_RestrictDelete", "AspNetUserRoles", "AspNetRoles", "RoleId"),
            ("TRG_AspNetUsers_AspNetUserRoles_RestrictDelete", "AspNetUserRoles", "AspNetUsers", "UserId"),
            ("TRG_AspNetUsers_AspNetUserTokens_RestrictDelete", "AspNetUserTokens", "AspNetUsers", "UserId"),
            ("TRG_AuthenticationConfigurations_AuthenticationPolicies_RestrictDelete", "AuthenticationPolicies", "AuthenticationConfigurations", "AuthenticationConfigurationId"),
            ("TRG_AspNetUsers_LoginAuditLogs_RestrictDelete", "LoginAuditLogs", "AspNetUsers", "UserId"),
            ("TRG_OpmsImportBatches_OpmsImportRows_RestrictDelete", "OpmsImportRows", "OpmsImportBatches", "OpmsImportBatchId"),
            ("TRG_AspNetUsers_RefreshTokens_RestrictDelete", "RefreshTokens", "AspNetUsers", "UserId"),
            ("TRG_AspNetRoles_RolePermissions_RestrictDelete", "RolePermissions", "AspNetRoles", "RoleId"),
            ("TRG_Permissions_RolePermissions_RestrictDelete", "RolePermissions", "Permissions", "PermissionId"),
            ("TRG_StrategicGoals_StrategicObjectives_RestrictDelete", "StrategicObjectives", "StrategicGoals", "StrategicGoalId"),
            ("TRG_Departments_Units_RestrictDelete", "Units", "Departments", "DepartmentId"),
            ("TRG_AspNetUsers_UserAssignments_RestrictDelete", "UserAssignments", "AspNetUsers", "UserId"),
            ("TRG_AspNetUsers_UserPermissionOverrides_RestrictDelete", "UserPermissionOverrides", "AspNetUsers", "UserId"),
            ("TRG_Permissions_UserPermissionOverrides_RestrictDelete", "UserPermissionOverrides", "Permissions", "PermissionId"),
            ("TRG_AspNetUsers_UserScopes_RestrictDelete", "UserScopes", "AspNetUsers", "UserId")
        ];
    }
}

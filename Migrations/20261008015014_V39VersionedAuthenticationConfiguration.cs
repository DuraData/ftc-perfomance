using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39VersionedAuthenticationConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuthenticationPolicies_MunicipalityId",
                table: "AuthenticationPolicies");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId",
                table: "AuthenticationConfigurations");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfigurationFamilyPublicId",
                table: "AuthenticationConfigurations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "AuthenticationConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PreviousVersionId",
                table: "AuthenticationConfigurations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "AuthenticationConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE [AuthenticationConfigurations]
                SET [ConfigurationFamilyPublicId] = [PublicId],
                    [VersionNumber] = 1,
                    [IsCurrent] = 1;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationConfigurations_Version",
                table: "AuthenticationConfigurations",
                sql: "[VersionNumber] >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationPolicies_MunicipalityId",
                table: "AuthenticationPolicies",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_ConfigurationFamilyPublicId_VersionNumber",
                table: "AuthenticationConfigurations",
                columns: new[] { "ConfigurationFamilyPublicId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId_IsCurrent",
                table: "AuthenticationConfigurations",
                columns: new[] { "MunicipalityId", "IsCurrent" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId_VersionNumber",
                table: "AuthenticationConfigurations",
                columns: new[] { "MunicipalityId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_PreviousVersionId",
                table: "AuthenticationConfigurations",
                column: "PreviousVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationConfigurations_AuthenticationConfigurations_PreviousVersionId",
                table: "AuthenticationConfigurations",
                column: "PreviousVersionId",
                principalTable: "AuthenticationConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationConfigurations_Version",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationConfigurations_AuthenticationConfigurations_PreviousVersionId",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationPolicies_MunicipalityId",
                table: "AuthenticationPolicies");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationConfigurations_ConfigurationFamilyPublicId_VersionNumber",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId_IsCurrent",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId_VersionNumber",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationConfigurations_PreviousVersionId",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropColumn(
                name: "ConfigurationFamilyPublicId",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropColumn(
                name: "PreviousVersionId",
                table: "AuthenticationConfigurations");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "AuthenticationConfigurations");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationPolicies_MunicipalityId",
                table: "AuthenticationPolicies",
                column: "MunicipalityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId",
                table: "AuthenticationConfigurations",
                column: "MunicipalityId",
                unique: true);
        }
    }
}
